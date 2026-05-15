using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Parsers;

/// <summary>
/// Parses colony HTML clipboard data into Colony model objects.
/// Simplified port from the WinForms ColonyParser — no System.Windows.Forms dependency.
/// </summary>
public sealed class ColonyParser
{
    private readonly ILogger<ColonyParser> _logger;

    public ColonyParser(ILogger<ColonyParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Builds a dictionary mapping building design names to flatpack blueprint UUIDs.
    /// </summary>
    public static Dictionary<string, string> BuildFlatpackLookup(DataService dataService)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var bp in dataService.Blueprints)
        {
            if (bp.BluePrintType is not null && bp.BluePrintType.IsFlatpack())
            {
                string designName = bp.OutputItemName;
                if (!string.IsNullOrEmpty(designName) && !lookup.ContainsKey(designName))
                {
                    lookup[designName] = bp.UUID;
                }

                if (!string.IsNullOrEmpty(bp.Name) && !lookup.ContainsKey(bp.Name))
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
                structure.RefiningResourcePurity = NormalizePurity(
                    purityMatch.Groups[1].Value.Trim());
            }
        }
        else
        {
            structure.MiningSurveyResource = resourceName.Trim();
        }
    }

    /// <summary>
    /// Parses an HTML fragment and populates the given colony with extracted data.
    /// </summary>
    public void ProcessHtml(Colony colony, string htmlFragment, DataService dataService)
    {
        try
        {
            var doc = ParseHtmlToXml(htmlFragment);
            if (doc is null)
            {
                return;
            }

            ParsePlanetOverview(colony, doc);
            ParseColonyBuildings(colony, doc, dataService);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing colony HTML fragment");
        }
    }

    /// <summary>
    /// Merges parsed structure data into an existing structure.
    /// </summary>
    internal static void MergeStructure(ColonyStructure existing, ColonyStructure parsed)
    {
        existing.FlatpackBlueprintUUID = parsed.FlatpackBlueprintUUID ?? existing.FlatpackBlueprintUUID;
        existing.BuildingID = parsed.BuildingID;
        existing.DisplaySequence = parsed.DisplaySequence;

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

        if (!string.IsNullOrEmpty(parsed.MiningSurveyResource))
        {
            existing.MiningSurveyResource = parsed.MiningSurveyResource;
            existing.RefiningResourcePurity = parsed.RefiningResourcePurity;
        }

        if (!string.IsNullOrEmpty(parsed.RefiningResource))
        {
            existing.RefiningResource = parsed.RefiningResource;
        }
    }

    /// <summary>
    /// Parses a single building JSON object into a ColonyStructure.
    /// </summary>
    internal static ColonyStructure? ParseBuilding(
        JToken building, Dictionary<string, string> flatpackLookup)
    {
        string? designName = building["blueprintDesignName"]?.ToString();
        if (string.IsNullOrEmpty(designName))
        {
            return null;
        }

        var structure = new ColonyStructure
        {
            UUID = Guid.NewGuid().ToString(),
        };

        if (flatpackLookup.TryGetValue(designName, out string? flatpackUUID))
        {
            structure.FlatpackBlueprintUUID = flatpackUUID;
        }

        structure.BuildingID = building["buildingID"]?.Value<int>() ?? 0;

        bool online = building["buildingOnline"]?.Value<bool>() ?? false;
        structure.Properties.SetProperty(GameConstants.PropBuilt, true);
        structure.Properties.SetProperty(GameConstants.PropOnline, online);

        // Building attributes
        var attributes = building["buildingAttributes"] as JArray;
        if (attributes is not null)
        {
            foreach (var attr in attributes)
            {
                string? propName = attr["propertyName"]?.ToString();
                var propValue = attr["propertyValue"];
                if (!string.IsNullOrEmpty(propName) && propValue is not null)
                {
                    structure.Properties.SetProperty(propName, propValue.ToString());
                }
            }
        }

        // Mining resource
        string? resourceName = building["resourceName"]?.ToString();
        if (!string.IsNullOrEmpty(resourceName))
        {
            ParseMiningResource(structure, resourceName);
        }

        // Manufacturing fields
        int mfgNumber = building["manufactureNumber"]?.Value<int>() ?? 0;
        if (mfgNumber > 0)
        {
            structure.ManufacturingQuantity = mfgNumber;
        }

        ParseWorkerDetails(structure, building);

        return structure;
    }

    /// <summary>
    /// Maps a JSON detail name to the worker prefix used by the UI.
    /// </summary>
    internal static string? MapDetailNameToWorkerPrefix(string detailName)
    {
        if (detailName.Contains("Blue Collar", StringComparison.OrdinalIgnoreCase))
        {
            return "BlueCollar";
        }

        if (detailName.Contains("White Collar", StringComparison.OrdinalIgnoreCase))
        {
            return "WhiteCollar";
        }

        if (detailName.Contains("Specialist", StringComparison.OrdinalIgnoreCase))
        {
            return "Specialist";
        }

        return null;
    }

    /// <summary>
    /// Converts an HTML string to an XmlDocument using SgmlReader.
    /// </summary>
    internal XmlDocument? ParseHtmlToXml(string htmlFragment)
    {
        try
        {
            using var reader = new StringReader(htmlFragment);
            var sgmlReader = new Sgml.SgmlReader()
            {
                DocType = "HTML",
                WhitespaceHandling = WhitespaceHandling.All,
                CaseFolding = Sgml.CaseFolding.ToLower,
                InputStream = reader,
            };

            var doc = new XmlDocument()
            {
                PreserveWhitespace = true,
                XmlResolver = null,
            };

            doc.Load(sgmlReader);
            return doc;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse HTML fragment via SgmlReader");
            return null;
        }
    }

    /// <summary>
    /// Extracts colony name, planet name, and system name from the overview section.
    /// </summary>
    internal void ParsePlanetOverview(Colony colony, XmlDocument doc)
    {
        var colonyNameNode = doc.SelectSingleNode(
            "//div[contains(@class,'ColonyInformation_ColonyName')]");
        if (colonyNameNode is not null)
        {
            string colonyName = colonyNameNode.InnerText.Trim();
            colony.ColonyName = colonyName;
            colony.PlanetName = colonyName;
        }

        var labelNodes = doc.SelectNodes(
            "//div[contains(@class,'ColonyInformation_PlanetOverview_StatInformation_Label')]");
        var infoNodes = doc.SelectNodes(
            "//div[contains(@class,'ColonyInformation_PlanetOverview_StatInformation_Info')]");

        if (labelNodes is not null && infoNodes is not null)
        {
            int count = Math.Min(labelNodes.Count, infoNodes.Count);
            for (int i = 0; i < count; i++)
            {
                var labelNode = labelNodes[i];
                var infoNode = infoNodes[i];
                if (labelNode is null || infoNode is null)
                {
                    continue;
                }

                string label = labelNode.InnerText.Trim().TrimEnd(':');
                string value = infoNode.InnerText.Trim();

                switch (label)
                {
                    case "Planet":
                        colony.PlanetName = value;
                        break;
                    case "System":
                        colony.SystemName = value;
                        break;
                }
            }
        }

        _logger.LogInformation(
            "ParsePlanetOverview: Planet={Planet}, System={System}, Colony={Colony}",
            colony.PlanetName ?? "(null)",
            colony.SystemName ?? "(null)",
            colony.ColonyName ?? "(null)");

        // Fallback: system name from location bar
        if (string.IsNullOrEmpty(colony.SystemName))
        {
            var locationNode = doc.SelectSingleNode(
                "//span[contains(@class,'location_name')]");
            if (locationNode is not null)
            {
                colony.SystemName = locationNode.InnerText.Trim();
            }
        }
    }

    /// <summary>
    /// Extracts colony buildings from the JSON attribute on the headline structures element.
    /// </summary>
    internal void ParseColonyBuildings(Colony colony, XmlDocument doc, DataService dataService)
    {
        var headlineNode = doc.SelectSingleNode(
            "//ui-colony-headline-structures[@colony-buildings]");

        string? jsonEncoded = headlineNode?.Attributes?["colony-buildings"]?.Value;
        if (string.IsNullOrEmpty(jsonEncoded))
        {
            _logger.LogInformation("No colony-buildings JSON found in HTML");
            return;
        }

        ParseColonyBuildingsFromJson(colony, jsonEncoded, dataService);
    }

    /// <summary>
    /// Parses the colony-buildings JSON and merges structures into the colony.
    /// </summary>
    internal void ParseColonyBuildingsFromJson(
        Colony colony, string jsonEncoded, DataService dataService)
    {
        try
        {
            var root = JObject.Parse(jsonEncoded);
            var buildings = root["buildings"] as JArray;
            if (buildings is null)
            {
                _logger.LogDebug("No buildings array in colony-buildings JSON");
                return;
            }

            var flatpackLookup = BuildFlatpackLookup(dataService);

            // First pass: parse all buildings
            var parsedBuildings = new List<ColonyStructure>();
            foreach (var building in buildings)
            {
                var parsed = ParseBuilding(building, flatpackLookup);
                if (parsed is not null)
                {
                    parsedBuildings.Add(parsed);
                }
            }

            // Assign DisplaySequence per type
            var typeCounters = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var parsed in parsedBuildings)
            {
                string key = parsed.FlatpackBlueprintUUID ?? string.Empty;
                if (!typeCounters.ContainsKey(key))
                {
                    typeCounters[key] = 0;
                }

                typeCounters[key]++;
                parsed.DisplaySequence = typeCounters[key];
            }

            MergeParsedBuildings(colony, parsedBuildings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse colony-buildings JSON");
        }
    }

    private static void ParseWorkerDetails(ColonyStructure structure, JToken building)
    {
        var details = building["detailsRequired"] as JArray;
        if (details is null)
        {
            return;
        }

        var workerCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var detail in details)
        {
            string? detailName = detail["name"]?.ToString();
            int workerID = detail["workerID"]?.Value<int>() ?? 0;
            if (string.IsNullOrEmpty(detailName))
            {
                continue;
            }

            string? prefix = MapDetailNameToWorkerPrefix(detailName);
            if (prefix is not null)
            {
                if (!workerCounts.ContainsKey(prefix))
                {
                    workerCounts[prefix] = 0;
                }

                workerCounts[prefix]++;
                string key = prefix + workerCounts[prefix];
                structure.AssignedWorkers.SetProperty(key, workerID > 0);
            }
        }
    }

    private static string NormalizePurity(string purity)
    {
        if (string.IsNullOrEmpty(purity))
        {
            return purity;
        }

        return purity.ToLowerInvariant() switch
        {
            "med" => GameConstants.PurityMedium,
            "hi" => GameConstants.PurityHigh,
            "lo" => GameConstants.PurityLow,
            _ => purity,
        };
    }

    private void MergeParsedBuildings(Colony colony, List<ColonyStructure> parsedBuildings)
    {
        // Build merge lookup from existing structures
        var existingByCompoundKey = new Dictionary<string, ColonyStructure>(StringComparer.Ordinal);
        var existingTypeCounters = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var s in colony.Structures)
        {
            string bpUUID = s.FlatpackBlueprintUUID ?? string.Empty;
            if (!existingTypeCounters.ContainsKey(bpUUID))
            {
                existingTypeCounters[bpUUID] = 0;
            }

            existingTypeCounters[bpUUID]++;
            if (s.DisplaySequence == 0)
            {
                s.DisplaySequence = existingTypeCounters[bpUUID];
            }

            if (!string.IsNullOrEmpty(s.FlatpackBlueprintUUID))
            {
                string compoundKey = s.FlatpackBlueprintUUID + ":" + s.DisplaySequence;
                if (!existingByCompoundKey.ContainsKey(compoundKey))
                {
                    existingByCompoundKey[compoundKey] = s;
                }
            }
        }

        // Merge parsed buildings into existing structures
        int added = 0;
        int updated = 0;
        foreach (var parsed in parsedBuildings)
        {
            ColonyStructure? existing = null;
            if (!string.IsNullOrEmpty(parsed.FlatpackBlueprintUUID))
            {
                string compoundKey = parsed.FlatpackBlueprintUUID + ":" + parsed.DisplaySequence;
                existingByCompoundKey.TryGetValue(compoundKey, out existing);
            }

            if (existing is not null)
            {
                MergeStructure(existing, parsed);
                updated++;
            }
            else
            {
                colony.Structures.Add(parsed);
                added++;
            }
        }

        _logger.LogInformation(
            "Colony structures merge: {Updated} updated, {Added} added (total: {Total})",
            updated, added, colony.Structures.Count);
    }
}
