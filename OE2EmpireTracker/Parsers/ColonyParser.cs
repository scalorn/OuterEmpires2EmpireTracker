using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Parsers
{
    public class ColonyParser
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Parses an HTML fragment from the game's colony Administration tab clipboard data
        /// and populates the given Colony object with extracted data.
        /// </summary>
        public void ProcessHtml(Colony colony, string htmlFragment, EmpireContext empireContext)
        {
            try
            {
                var doc = ParseHtmlToXml(htmlFragment);
                if (doc == null) return;

                ParsePlanetOverview(colony, doc);
                ParseColonyBuildings(colony, doc, empireContext);
                ParseCommodityDemands(colony, doc);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error parsing colony HTML fragment");
            }
        }

        /// <summary>
        /// Reads HTML from the clipboard and processes it into the given colony.
        /// </summary>
        public void ProcessClipboard(Colony colony, EmpireContext empireContext)
        {
            if (Clipboard.ContainsText(TextDataFormat.Html))
            {
                string clipboardData = Clipboard.GetText(TextDataFormat.Html);
                Log.Info("Colony clipboard data length: {0}", clipboardData.Length);
                string html = Forms.Blueprint.BlueprintScanner
                    .ExtractHtmlFragmentFromClipboardData(clipboardData);
                ProcessHtml(colony, html, empireContext);
            }
        }

        /// <summary>
        /// Parses the HTML string into an XmlDocument using SgmlReader.
        /// </summary>
        internal static XmlDocument ParseHtmlToXml(string htmlFragment)
        {
            try
            {
                var reader = new StringReader(htmlFragment);
                var sgmlReader = new Sgml.SgmlReader()
                {
                    DocType = "HTML",
                    WhitespaceHandling = WhitespaceHandling.All,
                    CaseFolding = Sgml.CaseFolding.ToLower,
                    InputStream = reader
                };

                var doc = new XmlDocument()
                {
                    PreserveWhitespace = true,
                    XmlResolver = null
                };
                doc.Load(sgmlReader);
                return doc;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to parse HTML fragment");
                return null;
            }
        }

        /// <summary>
        /// Extracts colony name, planet name, system name, and owner from the
        /// ColonyInformation_PlanetOverview section (local colonies only).
        /// </summary>
        internal static void ParsePlanetOverview(Colony colony, XmlDocument doc)
        {
            // Colony name from the large header
            var colonyNameNode = doc.SelectSingleNode(
                "//div[contains(@class,'ColonyInformation_ColonyName')]");
            if (colonyNameNode != null)
            {
                string colonyName = colonyNameNode.InnerText.Trim();
                colony.ColonyName = colonyName;
                // Planet name is the same as colony name from this element
                colony.PlanetName = colonyName;
            }

            // Stat info lines: Planet, System, Owner, Colony Size
            var labelNodes = doc.SelectNodes(
                "//div[contains(@class,'ColonyInformation_PlanetOverview_StatInformation_Label')]");
            var infoNodes = doc.SelectNodes(
                "//div[contains(@class,'ColonyInformation_PlanetOverview_StatInformation_Info')]");

            if (labelNodes != null && infoNodes != null)
            {
                int count = Math.Min(labelNodes.Count, infoNodes.Count);
                for (int i = 0; i < count; i++)
                {
                    string label = labelNodes[i].InnerText.Trim().TrimEnd(':');
                    string value = infoNodes[i].InnerText.Trim();

                    switch (label)
                    {
                        case "Planet":
                            colony.PlanetName = value;
                            break;
                        case "System":
                            colony.SystemName = value;
                            break;
                        case "Colony Size":
                            // Colony size is informational; not stored on Colony directly
                            break;
                    }
                }
            }

            // Fallback: system name from the top location bar
            if (string.IsNullOrEmpty(colony.SystemName))
            {
                var locationNode = doc.SelectSingleNode(
                    "//span[contains(@class,'location_name')]");
                if (locationNode != null)
                {
                    colony.SystemName = locationNode.InnerText.Trim();
                }
            }
        }

        /// <summary>
        /// Extracts the colony-buildings JSON from the ui-colony-headline-structures element
        /// and populates the colony's structure list. Falls back to colony-workers workforce
        /// detail for non-local colonies where colony-buildings is empty.
        /// </summary>
        internal static void ParseColonyBuildings(Colony colony, XmlDocument doc, EmpireContext empireContext)
        {
            // Try the colony-buildings JSON first (local colonies)
            var headlineNode = doc.SelectSingleNode(
                "//ui-colony-headline-structures[@colony-buildings]");

            string jsonEncoded = headlineNode?.Attributes["colony-buildings"]?.Value;

            if (!string.IsNullOrEmpty(jsonEncoded))
            {
                ParseColonyBuildingsFromJson(colony, jsonEncoded, empireContext);
                return;
            }

            // Fallback: extract building names from colony-workers workforce detail (non-local colonies)
            Log.Debug("colony-buildings empty, falling back to colony-workers workforce detail");
            ParseColonyBuildingsFromWorkers(colony, doc, empireContext);
        }

        /// <summary>
        /// Parses the full colony-buildings JSON into structures.
        /// </summary>
        internal static void ParseColonyBuildingsFromJson(Colony colony, string jsonEncoded, EmpireContext empireContext)
        {

            try
            {
                var root = JObject.Parse(jsonEncoded);
                var buildings = root["buildings"] as JArray;
                if (buildings == null)
                {
                    Log.Debug("No buildings array in colony-buildings JSON");
                    return;
                }

                var flatpackLookup = BuildFlatpackLookup(empireContext);

                // Index existing structures by gameSequence for merge
                var existingByGameSeq = new Dictionary<int, ColonyStructure>();
                foreach (var s in colony.Structures)
                {
                    if (s.gameSequence > 0 && !existingByGameSeq.ContainsKey(s.gameSequence))
                        existingByGameSeq[s.gameSequence] = s;
                }

                int added = 0, updated = 0;
                foreach (var building in buildings)
                {
                    var parsed = ParseBuilding(building, flatpackLookup);
                    if (parsed == null) continue;

                    if (existingByGameSeq.TryGetValue(parsed.gameSequence, out var existing))
                    {
                        // Merge into existing: update fields from HTML but preserve UUID and local state
                        MergeStructure(existing, parsed);
                        updated++;
                    }
                    else
                    {
                        colony.Structures.Add(parsed);
                        added++;
                    }
                }

                Log.Info("Colony structures merge: {0} updated, {1} added (total: {2})",
                    updated, added, colony.Structures.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to parse colony-buildings JSON");
            }
        }

        /// <summary>
        /// Merges parsed structure data into an existing structure, preserving UUID
        /// and locally-configured state (mining survey, refining, manufacturing assignments).
        /// </summary>
        internal static void MergeStructure(ColonyStructure existing, ColonyStructure parsed)
        {
            existing.FlatpackBlueprintUUID = parsed.FlatpackBlueprintUUID ?? existing.FlatpackBlueprintUUID;
            existing.gameSequence = parsed.gameSequence;

            // Update online/built status from game
            if (parsed.Properties.ContainsKey(GameConstants.PropBuilt))
            {
                parsed.Properties.getBoolean(GameConstants.PropBuilt, false, out bool built);
                existing.Properties.setProperty(GameConstants.PropBuilt, built);
            }
            if (parsed.Properties.ContainsKey(GameConstants.PropOnline))
            {
                parsed.Properties.getBoolean(GameConstants.PropOnline, false, out bool online);
                existing.Properties.setProperty(GameConstants.PropOnline, online);
            }

            // Merge building attributes (overwrite with game values)
            foreach (var kvp in parsed.Properties.Properties)
            {
                if (kvp.Key != GameConstants.PropBuilt &&
                    kvp.Key != GameConstants.PropOnline &&
                    kvp.Key != GameConstants.PropStaged)
                {
                    existing.Properties.setProperty(kvp.Key, kvp.Value);
                }
            }

            // Update mining resource if parsed has one and existing doesn't
            if (!string.IsNullOrEmpty(parsed.MiningSurveyResource) &&
                string.IsNullOrEmpty(existing.MiningSurveyResource))
            {
                existing.MiningSurveyResource = parsed.MiningSurveyResource;
                existing.RefiningResourcePurity = parsed.RefiningResourcePurity;
            }

            // Update manufacturing quantity from game
            if (parsed.ManufacturingQuantity > 0)
            {
                existing.ManufacturingQuantity = parsed.ManufacturingQuantity;
            }

            // Update worker assignments from game
            if (parsed.AssignedWorkers.Properties.Count > 0)
            {
                foreach (var kvp in parsed.AssignedWorkers.Properties)
                {
                    existing.AssignedWorkers.setProperty(kvp.Key, kvp.Value);
                }
            }
        }

        /// <summary>
        /// Builds a dictionary mapping building design names (e.g. "Mining Rig")
        /// to their flatpack blueprint UUIDs from the global blueprint list.
        /// </summary>
        public static Dictionary<string, string> BuildFlatpackLookup(EmpireContext empireContext)
        {
            var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (empireContext?.globalBlueprintList == null) return lookup;

            foreach (var bp in empireContext.globalBlueprintList)
            {
                if (bp.BluePrintType != null && bp.BluePrintType.IsFlatpack())
                {
                    // Standard flatpacks: "Mining Rig Flatpack" → "Mining Rig"
                    string designName = bp.Name;
                    if (designName.EndsWith(" Flatpack", StringComparison.OrdinalIgnoreCase))
                    {
                        designName = designName.Substring(0, designName.Length - " Flatpack".Length);
                    }

                    // Commodity factory flatpacks use the building name directly
                    // (e.g. "Administration Block" → "Administration Block")
                    if (!lookup.ContainsKey(designName))
                    {
                        lookup[designName] = bp.UUID;
                    }

                    // Also register the full name with " Flatpack" suffix removed
                    // so both "Mining Rig" and "Mining Rig Flatpack" resolve
                    if (!lookup.ContainsKey(bp.Name))
                    {
                        lookup[bp.Name] = bp.UUID;
                    }
                }
            }

            return lookup;
        }

        /// <summary>
        /// Parses a single building JSON object into a ColonyStructure.
        /// </summary>
        internal static ColonyStructure ParseBuilding(JToken building, Dictionary<string, string> flatpackLookup)
        {
            string designName = building["blueprintDesignName"]?.ToString();
            if (string.IsNullOrEmpty(designName)) return null;

            var structure = new ColonyStructure();
            structure.UUID = Guid.NewGuid().ToString();

            // Match to flatpack blueprint UUID
            if (flatpackLookup.TryGetValue(designName, out string flatpackUUID))
            {
                structure.FlatpackBlueprintUUID = flatpackUUID;
            }
            else
            {
                Log.Warn("No flatpack blueprint found for building design: {0}", designName);
            }

            // Game sequence from buildingID
            int buildingID = building["buildingID"]?.Value<int>() ?? 0;
            structure.gameSequence = buildingID;

            // Online/Built status
            bool online = building["buildingOnline"]?.Value<bool>() ?? false;
            structure.Properties.setProperty(GameConstants.PropBuilt, true);
            structure.Properties.setProperty(GameConstants.PropOnline, online);

            // Building attributes → Properties
            var attributes = building["buildingAttributes"] as JArray;
            if (attributes != null)
            {
                foreach (var attr in attributes)
                {
                    string propName = attr["propertyName"]?.ToString();
                    var propValue = attr["propertyValue"];
                    if (!string.IsNullOrEmpty(propName) && propValue != null)
                    {
                        structure.Properties.setProperty(propName, propValue.ToString());
                    }
                }
            }

            // Mining-specific fields
            string resourceName = building["resourceName"]?.ToString();
            if (!string.IsNullOrEmpty(resourceName))
            {
                ParseMiningResource(structure, resourceName);
            }

            // Manufacturing fields
            int mfgNumber = building["manufactureNumber"]?.Value<int>() ?? 0;
            int mfgPerRun = building["manufactureAmountPerRun"]?.Value<int>() ?? 0;
            if (mfgNumber > 0 || mfgPerRun > 0)
            {
                structure.ManufacturingQuantity = mfgNumber;
            }

            // Worker details → AssignedWorkers
            // The UI expects keys like "BlueCollar1", "WhiteCollar1" with boolean values.
            // The JSON provides detail names like "Blue Collar Detail(s)" with workerIDs.
            var details = building["detailsRequired"] as JArray;
            if (details != null)
            {
                // Track how many of each worker type we've seen to build the index suffix
                var workerCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (var detail in details)
                {
                    string detailName = detail["name"]?.ToString();
                    int workerID = detail["workerID"]?.Value<int>() ?? 0;
                    if (string.IsNullOrEmpty(detailName)) continue;

                    // Map detail name to WorkerPrefix
                    string prefix = MapDetailNameToWorkerPrefix(detailName);
                    if (prefix != null)
                    {
                        if (!workerCounts.ContainsKey(prefix)) workerCounts[prefix] = 0;
                        workerCounts[prefix]++;
                        string key = prefix + workerCounts[prefix];
                        structure.AssignedWorkers.setProperty(key, workerID > 0);
                    }
                }
            }

            return structure;
        }

        /// <summary>
        /// Maps a JSON detail name like "Blue Collar Detail(s)" to the WorkerPrefix
        /// used by the UI (e.g. "BlueCollar"). Returns null if no match.
        /// </summary>
        internal static string MapDetailNameToWorkerPrefix(string detailName)
        {
            if (detailName.IndexOf("Blue Collar", StringComparison.OrdinalIgnoreCase) >= 0) return "BlueCollar";
            if (detailName.IndexOf("White Collar", StringComparison.OrdinalIgnoreCase) >= 0) return "WhiteCollar";
            if (detailName.IndexOf("Specialist", StringComparison.OrdinalIgnoreCase) >= 0) return "Specialist";
            return null;
        }

        /// <summary>
        /// Parses a resource name like "Post-Trans Metals (Unrefined, High Purity)"
        /// into MiningSurveyResource and RefiningResourcePurity on the structure.
        /// </summary>
        public static void ParseMiningResource(ColonyStructure structure, string resourceName)
        {
            // Format: "ResourceName (Qualifier, Purity)"
            // e.g. "Post-Trans Metals (Unrefined, High Purity)"
            var match = Regex.Match(resourceName, @"^(.+?)\s*\((.+)\)\s*$");
            if (match.Success)
            {
                structure.MiningSurveyResource = match.Groups[1].Value.Trim();
                string qualifier = match.Groups[2].Value.Trim();

                var purityMatch = Regex.Match(qualifier, @"(Low|Medium|High|Med|Hi|Lo)\s*Purity",
                    RegexOptions.IgnoreCase);
                if (purityMatch.Success)
                {
                    structure.RefiningResourcePurity = SurveyParser.NormalizePurity(
                        purityMatch.Groups[1].Value.Trim());
                }
            }
            else
            {
                structure.MiningSurveyResource = resourceName.Trim();
            }
        }

        /// <summary>
        /// Extracts building names from the colony-workers workforce detail for non-local
        /// colonies where colony-buildings is empty. Creates one structure per unique
        /// worker assignment (multiple workers on the same building type create multiple structures).
        /// </summary>
        internal static void ParseColonyBuildingsFromWorkers(Colony colony, XmlDocument doc, EmpireContext empireContext)
        {
            var workersNode = doc.SelectSingleNode(
                "//ui-colony-headline-workers[@colony-workers]");
            if (workersNode == null) return;

            string jsonEncoded = workersNode.Attributes["colony-workers"]?.Value;
            if (string.IsNullOrEmpty(jsonEncoded)) return;

            try
            {
                var root = JObject.Parse(jsonEncoded);
                var details = root["workforceDetail"] as JArray;
                if (details == null || details.Count == 0) return;

                var flatpackLookup = BuildFlatpackLookup(empireContext);

                // Index existing structures by FlatpackBlueprintUUID for merge
                // Workers fallback doesn't have gameSequence, so we match by blueprint
                var existingByBlueprint = new Dictionary<string, List<ColonyStructure>>();
                foreach (var s in colony.Structures)
                {
                    if (!string.IsNullOrEmpty(s.FlatpackBlueprintUUID))
                    {
                        if (!existingByBlueprint.ContainsKey(s.FlatpackBlueprintUUID))
                            existingByBlueprint[s.FlatpackBlueprintUUID] = new List<ColonyStructure>();
                        existingByBlueprint[s.FlatpackBlueprintUUID].Add(s);
                    }
                }

                // Each worker detail entry represents one worker on one building.
                // Group by workerID to get unique building instances.
                var buildingsByWorker = new Dictionary<int, JToken>();
                foreach (var detail in details)
                {
                    int workerId = detail["workerId"]?.Value<int>() ?? 0;
                    if (workerId > 0 && !buildingsByWorker.ContainsKey(workerId))
                    {
                        buildingsByWorker[workerId] = detail;
                    }
                }

                // Count how many of each blueprint type we need from the HTML
                var parsedCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var parsedDetails = new List<(string buildingName, string flatpackUUID)>();

                foreach (var kvp in buildingsByWorker)
                {
                    string buildingName = kvp.Value["name"]?.ToString();
                    if (string.IsNullOrEmpty(buildingName)) continue;

                    flatpackLookup.TryGetValue(buildingName, out string flatpackUUID);
                    parsedDetails.Add((buildingName, flatpackUUID));

                    string key = flatpackUUID ?? buildingName;
                    if (!parsedCounts.ContainsKey(key)) parsedCounts[key] = 0;
                    parsedCounts[key]++;
                }

                int added = 0;
                foreach (var group in parsedCounts)
                {
                    string key = group.Key;
                    int needed = group.Value;
                    int existingCount = 0;

                    if (existingByBlueprint.ContainsKey(key))
                        existingCount = existingByBlueprint[key].Count;

                    // Add structures only if we have fewer than the HTML shows
                    int toAdd = needed - existingCount;
                    for (int i = 0; i < toAdd; i++)
                    {
                        var structure = new ColonyStructure();
                        structure.UUID = Guid.NewGuid().ToString();
                        structure.FlatpackBlueprintUUID = key.Contains("-") ? key : null; // UUID has dashes
                        structure.Properties.setProperty(GameConstants.PropBuilt, true);
                        structure.Properties.setProperty(GameConstants.PropOnline, true);
                        colony.Structures.Add(structure);
                        added++;
                    }
                }

                Log.Info("Colony structures merge from workers: {0} added (total: {1})",
                    added, colony.Structures.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to parse colony-workers JSON for building names");
            }
        }

        /// <summary>
        /// Extracts workforce commodity demands from the colony-workers JSON attribute
        /// on the ui-colony-headline-workers element and populates colony.Commodities.
        /// </summary>
        internal static void ParseCommodityDemands(Colony colony, XmlDocument doc)
        {
            var workersNode = doc.SelectSingleNode(
                "//ui-colony-headline-workers[@colony-workers]");
            if (workersNode == null)
            {
                Log.Debug("No colony-workers attribute found in HTML");
                return;
            }

            string jsonEncoded = workersNode.Attributes["colony-workers"]?.Value;
            if (string.IsNullOrEmpty(jsonEncoded))
            {
                Log.Debug("colony-workers attribute is empty");
                return;
            }

            try
            {
                var root = JObject.Parse(jsonEncoded);
                var demands = root["workforceCommodityDemands"] as JArray;
                if (demands == null || demands.Count == 0)
                {
                    Log.Debug("No workforceCommodityDemands in colony-workers JSON");
                    return;
                }

                // Index existing commodities by name for merge
                var existingByName = new Dictionary<string, CommodityRequested>(StringComparer.OrdinalIgnoreCase);
                foreach (var c in colony.Commodities)
                {
                    if (!string.IsNullOrEmpty(c.Name) && !existingByName.ContainsKey(c.Name))
                        existingByName[c.Name] = c;
                }

                int added = 0, updated = 0;
                foreach (var demand in demands)
                {
                    string name = demand["typeName"]?.ToString();
                    int amount = demand["amount"]?.Value<int>() ?? 0;
                    bool fulfilled = demand["fulfilled"]?.Value<bool>() ?? false;
                    string requiredByStr = demand["requiredBy"]?.ToString();

                    DateTime needBy = DateTime.MinValue;
                    if (!string.IsNullOrEmpty(requiredByStr))
                    {
                        DateTime.TryParse(requiredByStr, null,
                            System.Globalization.DateTimeStyles.RoundtripKind, out needBy);
                    }

                    if (!string.IsNullOrEmpty(name))
                    {
                        if (existingByName.TryGetValue(name, out var existing))
                        {
                            // Update existing: refresh amount, deadline, fulfilled from game
                            existing.Requested = amount;
                            existing.NeedBy = needBy;
                            existing.Fulfilled = fulfilled;
                            // Preserve Delivered — that's locally tracked
                            updated++;
                        }
                        else
                        {
                            colony.Commodities.Add(new CommodityRequested
                            {
                                Name = name,
                                Requested = amount,
                                Delivered = 0,
                                NeedBy = needBy,
                                Fulfilled = fulfilled
                            });
                            added++;
                        }
                    }
                }

                Log.Info("Colony commodity demands merge: {0} updated, {1} added (total: {2})",
                    updated, added, colony.Commodities.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to parse colony-workers JSON for commodity demands");
            }
        }
    }
}
