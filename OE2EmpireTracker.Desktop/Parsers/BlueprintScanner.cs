using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Parsers;

/// <summary>
/// Parses blueprint HTML clipboard data into Blueprint model objects.
/// Port of the WinForms BlueprintScanner using SgmlReader for HTML parsing.
/// </summary>
public sealed class BlueprintScanner
{
    private static readonly Dictionary<string, string> PropertyRemap =
        new (StringComparer.OrdinalIgnoreCase)
        {
            ["Health (Hitpoints)"] = BlueprintPropertyKeys.Health,
            ["Maximum Damage Repair %"] = "Maximum Damage Repair",
            ["The number of crew supported"] = "Crew Supported",
            ["Eng. Capacity Required"] = "Eng Capacity Required",
            ["Eng. Capacity Available"] = "Eng Capacity Available",
            ["Power regeneration rate"] = "Power Regeneration Rate",
            ["Blue Collar Detail(s)"] = GameConstants.PropBlueCollarDetail,
            ["Unassigned White Collar Detail(s)"] = GameConstants.PropUnassignedWhiteCollarDetail,
            ["Unassigned Specialist Detail(s)"] = GameConstants.PropUnassignedSpecialistDetail,
            ["Specialist Detail(s)"] = GameConstants.PropSpecialistDetail,
            ["White Collar Detail(s)"] = GameConstants.PropWhiteCollarDetail,
            ["Warehousing Capacity"] = GameConstants.PropWarehouseCapacity,
        };

    private readonly ILogger<BlueprintScanner> _logger;

    public BlueprintScanner(ILogger<BlueprintScanner> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Processes HTML clipboard data and returns a populated Blueprint.
    /// Returns null if the HTML is empty or unparseable.
    /// </summary>
    public Blueprint? ProcessHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return null;
        }

        var blueprint = new Blueprint();
        try
        {
            var doc = ParseHtmlToXml(html);
            if (doc is null)
            {
                return null;
            }

            ParseTitleAndEvolution(blueprint, doc);
            ParseProperties(blueprint, doc);
            ParseResources(blueprint, doc);

            _logger.LogInformation(
                "BlueprintScanner: name='{Name}' evo={Evo} tech='{Tech}' class={Class} props={Props} resources={Res}",
                blueprint.Name ?? "(null)",
                blueprint.Evolution,
                blueprint.TechLevel ?? "(null)",
                blueprint.Class,
                blueprint.Properties?.Count ?? 0,
                blueprint.Resources?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing blueprint HTML fragment");
            return null;
        }

        return string.IsNullOrEmpty(blueprint.Name) ? null : blueprint;
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
    /// Extracts blueprint name, evolution number, and tech level from the title area.
    /// </summary>
    internal void ParseTitleAndEvolution(Blueprint blueprint, XmlDocument doc)
    {
        // Extract evolution number
        var titleNode = doc.SelectSingleNode(
            "//div[contains(@class,'SmallSlideOut_Form_Row_Text_Bold')]");

        // Scope EvolutionNumber search to inside the title node first
        var evoNode = titleNode?.SelectSingleNode(
            ".//div[contains(@class,'EvolutionNumber')]")
            ?? doc.SelectSingleNode("//div[contains(@class,'EvolutionNumber')]");

        if (evoNode is not null && int.TryParse(evoNode.InnerText.Trim(), out int evo))
        {
            blueprint.Evolution = evo;
        }

        if (titleNode is not null)
        {
            // Build title text excluding the EvolutionNumber div
            var titleBuilder = new StringBuilder();
            foreach (XmlNode child in titleNode.ChildNodes)
            {
                if (child == evoNode)
                {
                    continue;
                }

                // Skip nested divs that contain the evo node
                if (child.NodeType == XmlNodeType.Element && evoNode is not null
                    && child.SelectSingleNode(
                        ".//div[contains(@class,'EvolutionNumber')]") is not null)
                {
                    continue;
                }

                titleBuilder.Append(child.InnerText);
            }

            string titleFull = titleBuilder.ToString().Trim();

            // Extract tech level from parentheses at end: "Name (TechLevel)"
            var m = Regex.Match(titleFull, @"^(.*)\((.+)\)\s*$");
            if (m.Success)
            {
                blueprint.Name = m.Groups[1].Value.Trim();
                blueprint.TechLevel = m.Groups[2].Value.Trim();
            }
            else
            {
                blueprint.Name = titleFull;
            }
        }

        // Extract description if present
        var descNode = doc.SelectSingleNode(
            "//div[contains(@class,'SmallSlideOut_Form_Row_Description')]");
        if (descNode is not null)
        {
            blueprint.Description = descNode.InnerText.Trim();
        }
    }

    /// <summary>
    /// Extracts properties from the ShipComponentProperty divs.
    /// Applies property remapping and strips delta indicators.
    /// </summary>
    internal void ParseProperties(Blueprint blueprint, XmlDocument doc)
    {
        var propNodes = doc.SelectNodes(
            "//div[contains(@class,'ShipComponentProperty')]");
        if (propNodes is null || propNodes.Count == 0)
        {
            return;
        }

        foreach (XmlNode prop in propNodes)
        {
            // Each ShipComponentProperty contains label and value divs
            var labelNode = prop.SelectSingleNode(
                ".//div[contains(@class,'CargoInfoDialogue')]");
            var valueNode = prop.SelectSingleNode(
                ".//div[contains(@class,'div_block') and contains(@class,'ui_text_blue_light')]");

            if (labelNode is null || valueNode is null)
            {
                // Fallback: try first and second child divs
                var childDivs = prop.SelectNodes(".//div");
                if (childDivs is not null && childDivs.Count >= 2)
                {
                    labelNode = childDivs[0];
                    valueNode = childDivs[1];
                }
            }

            if (labelNode is null || valueNode is null)
            {
                continue;
            }

            string key = labelNode.InnerText.Trim();
            string rawValue = valueNode.InnerText.Trim();

            // Normalize whitespace
            rawValue = Regex.Replace(rawValue, @"\s+", " ").Trim();

            // Remove inline delta text like "(▲ 435)" or "(▼ -9)"
            rawValue = Regex.Replace(rawValue, @"\(.*?\)", string.Empty).Trim();

            // Apply property remap
            if (!PropertyRemap.TryGetValue(key, out string? remapKey))
            {
                remapKey = key;
            }

            string normalizedValue = NormalizePropertyValue(remapKey, rawValue);
            blueprint.Properties.SetProperty(remapKey, normalizedValue);
        }

        // Extract Class from properties and move to blueprint field
        blueprint.Properties.GetString("Class", null, out string? equipClass);
        if (equipClass is not null && int.TryParse(equipClass, out int cls))
        {
            blueprint.Class = cls;
            blueprint.Properties.Remove("Class");
        }
    }

    /// <summary>
    /// Extracts resources from the resource name/detail div pairs.
    /// </summary>
    internal void ParseResources(Blueprint blueprint, XmlDocument doc)
    {
        var nameNodes = doc.SelectNodes(
            "//div[contains(@class,'ScanDetailOutputResourceName')]");
        var detailNodes = doc.SelectNodes(
            "//div[contains(@class,'ScanDetailOutputResourceDetail')]");

        int count = Math.Min(nameNodes?.Count ?? 0, detailNodes?.Count ?? 0);
        for (int i = 0; i < count; i++)
        {
            var nameNode = nameNodes![i];
            var detailNode = detailNodes![i];
            if (nameNode is null || detailNode is null)
            {
                continue;
            }

            string name = nameNode.InnerText.Trim();
            string qtyText = detailNode.InnerText.Trim();

            // Normalize quantity by keeping only digits
            string qtyNormalized = new string(
                qtyText.Where(c => char.IsDigit(c)).ToArray());
            if (string.IsNullOrEmpty(qtyNormalized))
            {
                qtyNormalized = qtyText;
            }

            blueprint.Resources[name] = qtyNormalized;
        }
    }

    /// <summary>
    /// Normalizes property values based on the property key type.
    /// Time properties get abbreviated, numeric properties get units stripped.
    /// </summary>
    private static string NormalizePropertyValue(string key, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var propType = BlueprintPropertyValidation.GetPropertyType(key);

        switch (propType)
        {
            case PropertyValueType.Time:
                value = Regex.Replace(value, @"\s*hours?\s*", "h ", RegexOptions.IgnoreCase);
                value = Regex.Replace(value, @"\s*minutes?\s*", "m ", RegexOptions.IgnoreCase);
                value = Regex.Replace(value, @"\s*seconds?\s*", "s ", RegexOptions.IgnoreCase);
                value = Regex.Replace(value, @"\s*days?\s*", "d ", RegexOptions.IgnoreCase);
                return value.Trim();

            case PropertyValueType.Decimal:
                var decMatch = Regex.Match(value, @"[+-]?\d+(\.\d+)?");
                return decMatch.Success ? decMatch.Value : value;

            case PropertyValueType.Integer:
                var intMatch = Regex.Match(value, @"[+-]?\d+");
                return intMatch.Success ? intMatch.Value : value;

            default:
                return value;
        }
    }
}
