using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Desktop.Parsers;

/// <summary>
/// Parses survey HTML clipboard data into Survey model objects.
/// Simplified port from the WinForms SurveyParser — no System.Windows.Forms dependency.
/// </summary>
public sealed class SurveyParser
{
    private readonly ILogger<SurveyParser> _logger;

    public SurveyParser(ILogger<SurveyParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parses an HTML fragment and returns a populated Survey.
    /// </summary>
    public Survey? ParseSurveyHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return null;
        }

        var survey = new Survey(string.Empty);
        ProcessHtml(survey, html);
        return survey;
    }

    /// <summary>
    /// Parses the description line to extract DateTime and ScannedBy.
    /// </summary>
    internal static void ParseDescription(Survey survey, string descText)
    {
        var match = Regex.Match(
            descText,
            @"(?:taken|generated)\s+on\s+(.+?)\s+by\s+(.+)$",
            RegexOptions.IgnoreCase);
        if (match.Success)
        {
            string rawDate = match.Groups[1].Value.Trim();
            if (SurveyDateTimeParser.TryParseGameFormat(rawDate, out DateTime parsed))
            {
                survey.DateTime = SurveyDateTimeParser.ToIsoString(parsed);
            }
            else
            {
                survey.DateTime = rawDate;
            }

            survey.ScannedBy = match.Groups[2].Value.Trim();
        }
    }

    /// <summary>
    /// Parses the title line to extract PlanetName, SystemName, and SurveyID.
    /// </summary>
    internal static void ParseTitle(Survey survey, string titleText)
    {
        if (string.IsNullOrEmpty(titleText))
        {
            return;
        }

        var m = Regex.Match(titleText, @"^(.+?),\s*(.+?)\s*\((.+?)\)\s*$");
        if (m.Success)
        {
            survey.PlanetName = m.Groups[1].Value.Trim();
            survey.SystemName = m.Groups[2].Value.Trim();
            survey.SurveyID = m.Groups[3].Value.Trim();
            return;
        }

        survey.PlanetName = titleText;
    }

    /// <summary>
    /// Parses a resource name and detail into a SurveyResource.
    /// </summary>
    internal static void ParseResource(Survey survey, string rawName, string rawDetail)
    {
        if (rawName.Contains("Unknown") || rawDetail.Contains("?"))
        {
            return;
        }

        string resourceName = rawName;
        string purity = string.Empty;
        var m = Regex.Match(rawName, @"^(.+?)\s*\((.+?)\)\s*$");
        if (m.Success)
        {
            resourceName = m.Groups[1].Value.Trim();
            purity = m.Groups[2].Value.Trim();
            if (purity.EndsWith(" Purity", StringComparison.OrdinalIgnoreCase))
            {
                purity = purity.Substring(0, purity.Length - " Purity".Length).Trim();
            }

            purity = NormalizePurity(purity);
        }

        if (rawDetail.Contains("/cycle", StringComparison.OrdinalIgnoreCase))
        {
            survey.SurveyType = SurveyType.Asteroid;
        }

        string amount = new string(rawDetail.Where(c => char.IsDigit(c) || c == '.').ToArray());
        if (string.IsNullOrEmpty(amount))
        {
            amount = rawDetail;
        }

        var resource = new SurveyResource(resourceName, purity, amount);
        survey.Resources[resourceName] = resource;
    }

    /// <summary>
    /// Parses max reserve values for asteroid surveys.
    /// </summary>
    internal static void ParseMaxReserves(
        Survey survey, XmlNodeList maxReserveNodes, XmlNodeList? nameNodes)
    {
        survey.ParsedMaxReserves = new Dictionary<string, int>();
        int reserveCount = Math.Min(maxReserveNodes.Count, nameNodes?.Count ?? 0);
        for (int i = 0; i < reserveCount; i++)
        {
            var reserveNode = maxReserveNodes[i];
            var nameNode = nameNodes?[i];
            if (reserveNode is null || nameNode is null)
            {
                continue;
            }

            string rawResName = nameNode.InnerText.Trim();
            var resMatch = Regex.Match(rawResName, @"^(.+?)\s*\((.+?)\)\s*$");
            string resName = resMatch.Success ? resMatch.Groups[1].Value.Trim() : rawResName;

            string rawReserve = reserveNode.InnerText.Trim();
            string cleaned = Regex.Replace(rawReserve, @"^[^0-9]*", string.Empty)
                .Replace(",", string.Empty).Trim();
            if (int.TryParse(cleaned, out int maxReserve))
            {
                survey.ParsedMaxReserves[resName] = maxReserve;
            }
        }
    }

    /// <summary>
    /// Normalizes purity abbreviations to standard values.
    /// </summary>
    internal static string NormalizePurity(string purity)
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

    /// <summary>
    /// Parses an HTML fragment and populates the given Survey object.
    /// </summary>
    private void ProcessHtml(Survey survey, string htmlFragment)
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

            // Extract description line
            var descNode = doc.SelectSingleNode(
                "//div[contains(@class,'SmallSlideOut_Form_Row_Description')]");
            if (descNode is not null)
            {
                ParseDescription(survey, descNode.InnerText.Trim());
            }

            // Extract title (planet name, system, survey ID)
            var titleNode = doc.SelectSingleNode(
                "//div[contains(@class,'SmallSlideOut_Form_Row_Text_Bold')]");
            if (titleNode is not null)
            {
                ParseTitle(survey, titleNode.InnerText.Trim());
            }

            // Extract resource rows
            var nameNodes = doc.SelectNodes(
                "//div[contains(@class,'ScanDetailOutputResourceName')]");
            var detailNodes = doc.SelectNodes(
                "//div[contains(@class,'ScanDetailOutputResourceDetail')]");

            int count = Math.Min(nameNodes?.Count ?? 0, detailNodes?.Count ?? 0);
            for (int i = 0; i < count; i++)
            {
                var nameNode = nameNodes?[i];
                var detailNode = detailNodes?[i];
                if (nameNode is null || detailNode is null)
                {
                    continue;
                }

                string rawName = nameNode.InnerText.Trim();
                string rawDetail = detailNode.InnerText.Trim();
                ParseResource(survey, rawName, rawDetail);
            }

            // Detect asteroid survey from max reserve nodes
            var maxReserveNodes = doc.SelectNodes(
                "//div[contains(@class,'ScanDetailOutputMaxReserve')]");
            if (maxReserveNodes is not null && maxReserveNodes.Count > 0)
            {
                survey.SurveyType = SurveyType.Asteroid;
                ParseMaxReserves(survey, maxReserveNodes, nameNodes);
            }

            _logger.LogInformation(
                "ProcessHtml: Planet={Planet}, System={System}, SurveyID={SurveyID}, Resources={Count}",
                survey.PlanetName ?? "(null)",
                survey.SystemName ?? "(null)",
                survey.SurveyID ?? "(null)",
                survey.Resources.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing survey HTML fragment");
        }
    }
}
