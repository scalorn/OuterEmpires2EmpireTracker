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
        /// Builds a dictionary mapping building design names (e.g. "Mining Rig")
        /// to their flatpack blueprint UUIDs from the global blueprint list.
        /// </summary>
        public static Dictionary<string, string> BuildFlatpackLookup(EmpireContext empireContext)
        {
            var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (empireContext?.GlobalBlueprintList == null) return lookup;

            foreach (var bp in empireContext.GlobalBlueprintList)
            {
                if (bp.BluePrintType != null && bp.BluePrintType.IsFlatpack())
                {
                    // Use OutputItemName: "Mining Rig Flatpack" -> "Mining Rig"
                    string designName = bp.OutputItemName;

                    // Commodity factory flatpacks use the building name directly
                    // (e.g. "Administration Block" -> "Administration Block")
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

                var purityMatch = Regex.Match(
                    qualifier,
                    @"(Low|Medium|High|Med|Hi|Lo)\s*Purity",
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
                string html = ClipboardHelper.ExtractHtmlFragment(clipboardData);
                ProcessHtml(colony, html, empireContext);
            }
        }

        /// <summary>
        /// Parses clipboard HTML into a new temporary Colony object without mutating any existing colony.
        /// Returns null if the clipboard does not contain HTML.
        /// </summary>
        public Colony ParseClipboardToTemp(EmpireContext empireContext, out string extractedHtml)
        {
            extractedHtml = null;
            if (!Clipboard.ContainsText(TextDataFormat.Html))
                return null;

            string clipboardData = Clipboard.GetText(TextDataFormat.Html);
            Log.Info("Colony clipboard data length (temp parse): {0}", clipboardData.Length);
            extractedHtml = ClipboardHelper.ExtractHtmlFragment(clipboardData);

            var tempColony = new Colony();
            ProcessHtml(tempColony, extractedHtml, empireContext);
            return tempColony;
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

            Log.Info(
                "ParsePlanetOverview: PlanetName='{0}', SystemName='{1}', ColonyName='{2}'",
                colony.PlanetName ?? "(null)",
                colony.SystemName ?? "(null)",
                colony.ColonyName ?? "(null)");

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
                Log.Info("ParseColonyBuildings: using JSON path (colony-buildings attribute found)");
                ParseColonyBuildingsFromJson(colony, jsonEncoded, empireContext);
                return;
            }

            // Fallback: extract building names from colony-workers workforce detail (non-local colonies)
            Log.Info("ParseColonyBuildings: using workers fallback (colony-buildings empty)");
            ParseColonyBuildingsFromWorkers(colony, doc, empireContext);
        }

        /// <summary>
        /// Parses the full colony-buildings JSON into structures.
        /// Uses a two-pass approach: first parse all buildings and assign DisplaySequence
        /// per type, then merge into existing structures using compound key
        /// FlatpackBlueprintUUID + DisplaySequence. Commodity factory types use positional
        /// matching within each sub-type instead of DisplaySequence.
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

                // === First pass: parse all buildings into a list ===
                var parsedBuildings = new List<ColonyStructure>();
                var parsedMaxRates = new Dictionary<int, decimal>(); // index -> maxRate
                int parsedIndex = 0;
                foreach (var building in buildings)
                {
                    var parsed = ParseBuilding(building, flatpackLookup, out decimal maxRate);
                    if (parsed != null)
                    {
                        // For refineries, the game JSON uses resourceName for the refining resource
                        // but ParseMiningResource puts it on MiningSurveyResource. Copy it to
                        // RefiningResource so RefinerySetupHelper can find it.
                        if (!string.IsNullOrEmpty(parsed.MiningSurveyResource) &&
                            !string.IsNullOrEmpty(parsed.FlatpackBlueprintUUID))
                        {
                            var bp = empireContext?.FindGlobalBlueprint(parsed.FlatpackBlueprintUUID);
                            if (bp != null && bp.BluePrintType == BlueprintTypes.Refinery)
                            {
                                parsed.RefiningResource = parsed.MiningSurveyResource;
                                Log.Debug(
                                    "Refinery {0}: set RefiningResource='{1}' from MiningSurveyResource",
                                    parsed.FlatpackBlueprintUUID,
                                    parsed.RefiningResource);
                            }
                            else if (bp != null && bp.BluePrintType != BlueprintTypes.MiningRig)
                            {
                                // Game JSON includes resourceName on all structure types
                                // (e.g. the item being manufactured). Only mining rigs and
                                // refineries should retain it as MiningSurveyResource.
                                parsed.MiningSurveyResource = null;
                                parsed.RefiningResourcePurity = null;
                            }
                        }

                        parsedMaxRates[parsedBuildings.Count] = maxRate;
                        parsedBuildings.Add(parsed);
                    }

                    parsedIndex++;
                }

                // Assign DisplaySequence per FlatpackBlueprintUUID type
                // (first Mining Rig = 1, second Mining Rig = 2, etc.)
                var typeCounters = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var parsed in parsedBuildings)
                {
                    string key = parsed.FlatpackBlueprintUUID ?? string.Empty;
                    if (!typeCounters.ContainsKey(key))
                        typeCounters[key] = 0;
                    typeCounters[key]++;
                    parsed.DisplaySequence = typeCounters[key];
                }

                // === Build merge lookups from existing colony structures ===

                // Determine which FlatpackBlueprintUUIDs are commodity factory types
                var commodityFactoryUUIDs = new HashSet<string>(StringComparer.Ordinal);
                foreach (var s in colony.Structures)
                {
                    if (string.IsNullOrEmpty(s.FlatpackBlueprintUUID)) continue;
                    if (commodityFactoryUUIDs.Contains(s.FlatpackBlueprintUUID)) continue;
                    var bp = empireContext?.FindGlobalBlueprint(s.FlatpackBlueprintUUID);
                    if (bp != null && bp.BluePrintType.IsCommodityFactory())
                        commodityFactoryUUIDs.Add(s.FlatpackBlueprintUUID);
                }

                // Also check parsed buildings for commodity factory types
                foreach (var p in parsedBuildings)
                {
                    if (string.IsNullOrEmpty(p.FlatpackBlueprintUUID)) continue;
                    if (commodityFactoryUUIDs.Contains(p.FlatpackBlueprintUUID)) continue;
                    var bp = empireContext?.FindGlobalBlueprint(p.FlatpackBlueprintUUID);
                    if (bp != null && bp.BluePrintType.IsCommodityFactory())
                        commodityFactoryUUIDs.Add(p.FlatpackBlueprintUUID);
                }

                // Assign DisplaySequence to existing structures that have DisplaySequence=0
                // (manually-added structures). This ensures they get a meaningful compound key
                // that can match parsed buildings.
                var existingTypeCounters = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var s in colony.Structures)
                {
                    string bpUUID = s.FlatpackBlueprintUUID ?? string.Empty;
                    if (!existingTypeCounters.ContainsKey(bpUUID))
                        existingTypeCounters[bpUUID] = 0;
                    existingTypeCounters[bpUUID]++;

                    if (s.DisplaySequence == 0)
                        s.DisplaySequence = existingTypeCounters[bpUUID];
                }

                // For non-commodity-factory types: compound key lookup
                var existingByCompoundKey = new Dictionary<string, ColonyStructure>(StringComparer.Ordinal);
                // For commodity factory types: positional lookup per sub-type (FlatpackBlueprintUUID)
                var existingCommodityByType = new Dictionary<string, List<ColonyStructure>>(StringComparer.Ordinal);

                foreach (var s in colony.Structures)
                {
                    if (string.IsNullOrEmpty(s.FlatpackBlueprintUUID)) continue;

                    if (commodityFactoryUUIDs.Contains(s.FlatpackBlueprintUUID))
                    {
                        // Commodity factory: add to positional list (preserving list order)
                        if (!existingCommodityByType.ContainsKey(s.FlatpackBlueprintUUID))
                            existingCommodityByType[s.FlatpackBlueprintUUID] = new List<ColonyStructure>();
                        existingCommodityByType[s.FlatpackBlueprintUUID].Add(s);
                    }
                    else
                    {
                        // Non-commodity: compound key = FlatpackBlueprintUUID + ":" + DisplaySequence
                        string compoundKey = s.FlatpackBlueprintUUID + ":" + s.DisplaySequence;
                        if (!existingByCompoundKey.ContainsKey(compoundKey))
                            existingByCompoundKey[compoundKey] = s;
                    }
                }

                // Track positional index per commodity factory sub-type during merge
                var commodityPositionCounters = new Dictionary<string, int>(StringComparer.Ordinal);

                // === Second pass: merge parsed buildings into existing structures ===
                var maxRates = new Dictionary<string, decimal>();
                int added = 0, updated = 0;
                int mergeIndex = 0;
                foreach (var parsed in parsedBuildings)
                {
                    ColonyStructure existing = null;

                    if (!string.IsNullOrEmpty(parsed.FlatpackBlueprintUUID) &&
                        commodityFactoryUUIDs.Contains(parsed.FlatpackBlueprintUUID))
                    {
                        // Commodity factory: positional matching within sub-type
                        if (!commodityPositionCounters.ContainsKey(parsed.FlatpackBlueprintUUID))
                            commodityPositionCounters[parsed.FlatpackBlueprintUUID] = 0;

                        int pos = commodityPositionCounters[parsed.FlatpackBlueprintUUID];
                        commodityPositionCounters[parsed.FlatpackBlueprintUUID]++;

                        if (existingCommodityByType.TryGetValue(parsed.FlatpackBlueprintUUID, out var list) &&
                            pos < list.Count)
                        {
                            existing = list[pos];
                        }
                    }
                    else
                    {
                        // Non-commodity: compound key lookup
                        string compoundKey = (parsed.FlatpackBlueprintUUID ?? string.Empty) + ":" + parsed.DisplaySequence;
                        existingByCompoundKey.TryGetValue(compoundKey, out existing);
                    }

                    if (existing != null)
                    {
                        MergeStructure(existing, parsed);
                        // Collect maxRate keyed by the merged structure's UUID
                        if (parsedMaxRates.TryGetValue(mergeIndex, out decimal mr) && mr > 0m)
                            maxRates[existing.UUID] = mr;
                        updated++;
                    }
                    else
                    {
                        colony.Structures.Add(parsed);
                        // Collect maxRate keyed by the new structure's UUID
                        if (parsedMaxRates.TryGetValue(mergeIndex, out decimal mr) && mr > 0m)
                            maxRates[parsed.UUID] = mr;
                        added++;
                    }

                    mergeIndex++;
                }

                Log.Info(
                    "Colony structures merge: {0} updated, {1} added (total: {2})",
                    updated,
                    added,
                    colony.Structures.Count);

                // Only run setup helpers for real imports (colony has an OwnerUUID).
                // Temp parses (ParseClipboardToTemp) create colonies with no OwnerUUID
                // and must not have side effects on PlayerContext.SurveyList.
                if (!string.IsNullOrEmpty(colony.OwnerUUID))
                {
                    MinerSetupHelper.SetupMiners(colony, empireContext, maxRates);
                    RefinerySetupHelper.SetupRefineries(colony, empireContext);
                }
                else
                {
                    Log.Info("Skipping miner/refinery setup for temp parse (no OwnerUUID)");
                }
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
            existing.BuildingID = parsed.BuildingID;
            existing.DisplaySequence = parsed.DisplaySequence;

            // Update online/built status from game
            if (parsed.Properties.ContainsKey(GameConstants.PropBuilt))
            {
                parsed.Properties.GetBoolean(GameConstants.PropBuilt, false, out bool built);
                existing.Properties.SetProperty(GameConstants.PropBuilt, built);
            }

            if (parsed.Properties.ContainsKey(GameConstants.PropOnline))
            {
                parsed.Properties.GetBoolean(GameConstants.PropOnline, false, out bool online);
                existing.Properties.SetProperty(GameConstants.PropOnline, online);
            }

            // Merge building attributes (overwrite with game values)
            foreach (var kvp in parsed.Properties.Properties)
            {
                if (kvp.Key != GameConstants.PropBuilt &&
                    kvp.Key != GameConstants.PropOnline &&
                    kvp.Key != GameConstants.PropStaged)
                {
                    existing.Properties.SetProperty(kvp.Key, kvp.Value);
                }
            }

            // Update mining resource from game (game is authoritative)
            if (!string.IsNullOrEmpty(parsed.MiningSurveyResource))
            {
                existing.MiningSurveyResource = parsed.MiningSurveyResource;
                existing.RefiningResourcePurity = parsed.RefiningResourcePurity;
            }

            // Update refining resource from game (set by parser for refinery structures)
            if (!string.IsNullOrEmpty(parsed.RefiningResource))
            {
                Log.Info(
                    "MergeStructure: carrying RefiningResource='{0}' from parsed to existing structure {1}",
                    parsed.RefiningResource,
                    existing.UUID);
                existing.RefiningResource = parsed.RefiningResource;
            }

            // Reconcile manufacturing state from game.
            // Game JSON reports remaining runs (manufactureNumber). The tracker stores
            // total (ManufacturingQuantity) and completed (ManufacturingCompleted).
            // Derive completed from: total - gameRemaining.
            int gameRemaining = parsed.ManufacturingQuantity;
            if (gameRemaining > 0 && existing.ManufacturingQuantity > 0)
            {
                if (gameRemaining > existing.ManufacturingQuantity)
                {
                    // User added more runs in-game — update total to match
                    existing.ManufacturingQuantity = gameRemaining;
                    existing.ManufacturingCompleted = 0;
                }
                else
                {
                    // Normal progress — derive completed from total minus remaining
                    existing.ManufacturingCompleted = existing.ManufacturingQuantity - gameRemaining;
                }
            }
            else if (gameRemaining == 0 && existing.ManufacturingQuantity > 0)
            {
                // Manufacturing finished or cancelled in-game — clear state
                existing.ManufacturingBlueprintUUID = null;
                existing.ManufacturingCommodityName = null;
                existing.ManufacturingQuantity = 0;
                existing.ManufacturingCompleted = 0;
                existing.StagingResources = false;
                existing.ProcessCompletionTime = null;
            }

            // Update worker assignments from game
            if (parsed.AssignedWorkers.Properties.Count > 0)
            {
                foreach (var kvp in parsed.AssignedWorkers.Properties)
                {
                    existing.AssignedWorkers.SetProperty(kvp.Key, kvp.Value);
                }
            }
        }

        /// <summary>
        /// Parses a single building JSON object into a ColonyStructure.
        /// The out parameter maxRate receives the building's maxRate from the game JSON (0 if absent).
        /// </summary>
        internal static ColonyStructure ParseBuilding(JToken building, Dictionary<string, string> flatpackLookup, out decimal maxRate)
        {
            maxRate = building["maxRate"]?.Value<decimal>() ?? 0m;

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

            // Store the game's unique building identifier
            int buildingId = building["buildingID"]?.Value<int>() ?? 0;
            structure.BuildingID = buildingId;
            // DisplaySequence is NOT set here -- it will be calculated per-type
            // in ParseColonyBuildingsFromJson after all buildings are parsed

            // Online/Built status
            bool online = building["buildingOnline"]?.Value<bool>() ?? false;
            structure.Properties.SetProperty(GameConstants.PropBuilt, true);
            structure.Properties.SetProperty(GameConstants.PropOnline, online);

            // Building attributes -> Properties
            var attributes = building["buildingAttributes"] as JArray;
            if (attributes != null)
            {
                foreach (var attr in attributes)
                {
                    string propName = attr["propertyName"]?.ToString();
                    var propValue = attr["propertyValue"];
                    if (!string.IsNullOrEmpty(propName) && propValue != null)
                    {
                        structure.Properties.SetProperty(propName, propValue.ToString());
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

            // Worker details -> AssignedWorkers
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
                        structure.AssignedWorkers.SetProperty(key, workerID > 0);
                    }
                }
            }

            Log.Debug(
                "ParseBuilding: designName='{0}', BuildingID={1}, online={2}, maxRate={3}, " + "MiningSurveyResource='{4}', RefiningResourcePurity='{5}', RefiningResource='{6}', FlatpackBP='{7}'",
                designName,
                structure.BuildingID,
                structure.Properties.ContainsKey(GameConstants.PropOnline) ? structure.Properties.Properties[GameConstants.PropOnline] : "?",
                maxRate,
                structure.MiningSurveyResource ?? "(null)",
                structure.RefiningResourcePurity ?? "(null)",
                structure.RefiningResource ?? "(null)",
                structure.FlatpackBlueprintUUID ?? "(null)");

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
                // Workers fallback doesn't have DisplaySequence, so we match by blueprint
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
                        structure.Properties.SetProperty(GameConstants.PropBuilt, true);
                        structure.Properties.SetProperty(GameConstants.PropOnline, true);
                        colony.Structures.Add(structure);
                        added++;
                    }
                }

                Log.Info(
                    "Colony structures merge from workers: {0} added (total: {1})",
                    added,
                    colony.Structures.Count);
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

                // Build list of existing commodities for merge.
                // Multiple requests for the same commodity are allowed (e.g. one fulfilled, one open).
                // Match strategy: for each incoming demand, find an existing request with the same name
                // AND same fulfilled status. If multiple match, prefer the one with the same amount.
                // This handles the common case of one fulfilled + one open request for the same commodity.

                int added = 0, updated = 0;
                var matchedIndices = new HashSet<int>();

                foreach (var demand in demands)
                {
                    string name = demand["typeName"]?.ToString();
                    int amount = demand["amount"]?.Value<int>() ?? 0;
                    bool fulfilled = demand["fulfilled"]?.Value<bool>() ?? false;
                    string requiredByStr = demand["requiredBy"]?.ToString();

                    DateTime needBy = DateTime.MinValue;
                    if (!string.IsNullOrEmpty(requiredByStr))
                    {
                        DateTime.TryParse(
                            requiredByStr,
                            null,
                            System.Globalization.DateTimeStyles.RoundtripKind,
                            out needBy);
                    }

                    if (string.IsNullOrEmpty(name)) continue;

                    // Find best unmatched existing request: same name + same fulfilled status preferred,
                    // then same amount as tiebreaker
                    int bestIdx = -1;
                    int bestScore = -1;
                    for (int i = 0; i < colony.Commodities.Count; i++)
                    {
                        if (matchedIndices.Contains(i)) continue;
                        var c = colony.Commodities[i];
                        if (!string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)) continue;

                        int score = 0;
                        if (c.Fulfilled == fulfilled) score += 2;
                        if (c.Requested == amount) score += 1;

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestIdx = i;
                        }
                    }

                    if (bestIdx >= 0)
                    {
                        var existing = colony.Commodities[bestIdx];
                        matchedIndices.Add(bestIdx);
                        existing.Requested = amount;
                        existing.NeedBy = needBy;
                        existing.Fulfilled = fulfilled;
                        // Preserve Delivered -- that's locally tracked
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

                Log.Info(
                    "Colony commodity demands merge: {0} updated, {1} added (total: {2})",
                    updated,
                    added,
                    colony.Commodities.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to parse colony-workers JSON for commodity demands");
            }
        }
    }
}
