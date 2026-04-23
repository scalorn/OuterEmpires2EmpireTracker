using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Parsers
{
    public class SurveyParser
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Parses the description line to extract DateTime and ScannedBy.
        /// Expected format: "A detailed survey report taken on {date} by {name}"
        /// </summary>
        public static void ParseDescription(Survey survey, string descText)
        {
            // Matches both "taken on {date} by {name}" and "generated on {date} by {name}"
            var match = Regex.Match(descText, @"(?:taken|generated)\s+on\s+(.+?)\s+by\s+(.+)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string rawDate = match.Groups[1].Value.Trim();
                if (SurveyDateTimeParser.TryParseGameFormat(rawDate, out DateTime parsed))
                    survey.DateTime = SurveyDateTimeParser.ToIsoString(parsed);
                else
                    survey.DateTime = rawDate; // preserve unparseable values
                survey.ScannedBy = match.Groups[2].Value.Trim();
            }
        }

        /// <summary>
        /// Parses the title line to extract PlanetName, SystemName, and optionally SurveyID.
        /// Expected format: "PlanetName, SystemName (SurveyID)" or just "PlanetName"
        /// </summary>
        public static void ParseTitle(Survey survey, string titleText)
        {
            if (string.IsNullOrEmpty(titleText)) return;

            // Try "PlanetName, SystemName (SurveyID)"
            var m = Regex.Match(titleText, @"^(.+?),\s*(.+?)\s*\((.+?)\)\s*$");
            if (m.Success)
            {
                survey.PlanetName = m.Groups[1].Value.Trim();
                survey.SystemName = m.Groups[2].Value.Trim();
                survey.SurveyID = m.Groups[3].Value.Trim();
                return;
            }

            // Fallback: use the whole title as planet name
            survey.PlanetName = titleText;
        }

        /// <summary>
        /// Parses a resource name like "Post-Trans Metals (Low Purity)" and a detail
        /// like "41/hour" into a SurveyResource and adds it to the survey.
        /// </summary>
        public static void ParseResource(Survey survey, string rawName, string rawDetail)
        {
            // Skip unknown/trace entries
            if (rawName.Contains("Unknown") || rawDetail.Contains("?"))
                return;

            // Extract resource name and purity from "ResourceName (Purity)"
            string resourceName = rawName;
            string purity = string.Empty;
            var m = Regex.Match(rawName, @"^(.+?)\s*\((.+?)\)\s*$");
            if (m.Success)
            {
                resourceName = m.Groups[1].Value.Trim();
                purity = m.Groups[2].Value.Trim();
                // Normalize purity: "High Purity" -> "High", "Low Purity" -> "Low", "Med Purity" -> "Medium"
                if (purity.EndsWith(" Purity", StringComparison.OrdinalIgnoreCase))
                {
                    purity = purity.Substring(0, purity.Length - " Purity".Length).Trim();
                }

                // Normalize abbreviations
                purity = NormalizePurity(purity);
            }

            // Detect asteroid survey by "/cycle" vs "/hour"
            if (rawDetail.IndexOf("/cycle", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                survey.SurveyType = SurveyType.Asteroid;
            }

            // Extract numeric amount from "41/hour" or "36.3/cycle"
            string amount = new string(rawDetail.Where(c => char.IsDigit(c) || c == '.').ToArray());
            if (string.IsNullOrEmpty(amount))
                amount = rawDetail;

            var resource = new SurveyResource(resourceName, purity, amount);
            survey.Resources[resourceName] = resource;
        }

        /// <summary>
        /// Normalizes purity abbreviations to match ResourcePurity.Name values.
        /// </summary>
        public static string NormalizePurity(string purity)
        {
            if (string.IsNullOrEmpty(purity)) return purity;
            switch (purity.ToLowerInvariant())
            {
                case "med": return GameConstants.PurityMedium;
                case "hi": return GameConstants.PurityHigh;
                case "lo": return GameConstants.PurityLow;
                default: return purity;
            }
        }

        /// <summary>
        /// Parses an HTML fragment from the game's survey clipboard data and populates
        /// the given Survey object with extracted data.
        /// </summary>
        public void ProcessHtml(Survey survey, string htmlFragment)
        {
            try
            {
                StringReader reader = new StringReader(htmlFragment);

                Sgml.SgmlReader sgmlReader = new Sgml.SgmlReader()
                {
                    DocType = "HTML",
                    WhitespaceHandling = WhitespaceHandling.All,
                    CaseFolding = Sgml.CaseFolding.ToLower,
                    InputStream = reader
                };

                XmlDocument doc = new XmlDocument()
                {
                    PreserveWhitespace = true,
                    XmlResolver = null
                };

                doc.Load(sgmlReader);

                // Extract the description line
                XmlNode descNode = doc.SelectSingleNode(
                    "//div[contains(@class,'SmallSlideOut_Form_Row_Description')]");
                if (descNode != null)
                {
                    string descText = descNode.InnerText.Trim();
                    ParseDescription(survey, descText);
                }

                // Extract planet name from the title node (e.g. "Zeh Vazoran II M2, Zeh Vazoran (B465873)")
                XmlNode titleNode = doc.SelectSingleNode(
                    "//div[contains(@class,'SmallSlideOut_Form_Row_Text_Bold')]");
                if (titleNode != null)
                {
                    string titleText = titleNode.InnerText.Trim();
                    ParseTitle(survey, titleText);
                }

                // Extract resource rows
                XmlNodeList nameNodes = doc.SelectNodes(
                    "//div[contains(@class,'ScanDetailOutputResourceName')]");
                XmlNodeList detailNodes = doc.SelectNodes(
                    "//div[contains(@class,'ScanDetailOutputResourceDetail')]");

                int count = Math.Min(nameNodes?.Count ?? 0, detailNodes?.Count ?? 0);
                for (int i = 0; i < count; i++)
                {
                    string rawName = nameNodes[i].InnerText.Trim();
                    string rawDetail = detailNodes[i].InnerText.Trim();

                    ParseResource(survey, rawName, rawDetail);
                }

                // Extract max reserve values for asteroid surveys
                XmlNodeList maxReserveNodes = doc.SelectNodes(
                    "//div[contains(@class,'ScanDetailOutputMaxReserve')]");
                if (maxReserveNodes != null && maxReserveNodes.Count > 0)
                {
                    // Max reserve presence confirms this is an asteroid survey
                    survey.SurveyType = SurveyType.Asteroid;
                    Log.Info("Detected asteroid survey (MaxReserve nodes found: {0})", maxReserveNodes.Count);

                    // Extract max reserve values, iterating in parallel with resource name nodes
                    survey.ParsedMaxReserves = new Dictionary<string, int>();
                    int reserveCount = Math.Min(maxReserveNodes.Count, nameNodes?.Count ?? 0);
                    for (int i = 0; i < reserveCount; i++)
                    {
                        string rawResName = nameNodes[i].InnerText.Trim();
                        // Extract just the resource name (before purity parentheses), matching ParseResource logic
                        var resMatch = Regex.Match(rawResName, @"^(.+?)\s*\((.+?)\)\s*$");
                        string resName = resMatch.Success ? resMatch.Groups[1].Value.Trim() : rawResName;

                        string rawReserve = maxReserveNodes[i].InnerText.Trim();
                        // Strip "Max Reserve:" label prefix, commas, and whitespace, then parse to int
                        string cleaned = Regex.Replace(rawReserve, @"^[^0-9]*", string.Empty).Replace(",", string.Empty).Trim();
                        if (int.TryParse(cleaned, out int maxReserve))
                        {
                            survey.ParsedMaxReserves[resName] = maxReserve;
                            Log.Debug("  MaxReserve: {0} = {1}", resName, maxReserve);
                        }
                        else
                        {
                            Log.Warn("  Could not parse max reserve value '{0}' for resource '{1}'", rawReserve, resName);
                        }
                    }
                }

                Log.Info(
                    "ProcessHtml: PlanetName='{0}', SystemName='{1}', SurveyID='{2}', resources extracted={3}",
                    survey.PlanetName ?? "(null)",
                    survey.SystemName ?? "(null)",
                    survey.SurveyID ?? "(null)",
                    survey.Resources.Count);
                foreach (var kvp in survey.Resources)
                {
                    var r = kvp.Value;
                    Log.Debug("  Resource: {0}, Purity={1}, Amount={2}", r.Resource, r.Purity ?? "(null)", r.Amount ?? "(null)");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error parsing survey HTML fragment");
            }
        }

        /// <summary>
        /// Parses clipboard HTML into a new temporary Survey object without mutating any existing survey.
        /// Returns null if the clipboard does not contain HTML.
        /// Also returns the extracted HTML via the out parameter for reuse.
        /// </summary>
        public Survey ParseClipboardToTemp(out string extractedHtml)
        {
            extractedHtml = null;
            if (!Clipboard.ContainsText(TextDataFormat.Html))
                return null;

            string clipboardData = Clipboard.GetText(TextDataFormat.Html);
            Log.Info("Survey clipboard data length (temp parse): {0}", clipboardData.Length);
            extractedHtml = ClipboardHelper.ExtractHtmlFragment(clipboardData);

            var tempSurvey = new Survey();
            ProcessHtml(tempSurvey, extractedHtml);
            return tempSurvey;
        }

        /// <summary>
        /// Reads HTML from the clipboard and processes it into the given survey.
        /// </summary>
        public void ProcessClipboard(Survey survey)
        {
            if (Clipboard.ContainsText(TextDataFormat.Html))
            {
                string clipboardData = Clipboard.GetText(TextDataFormat.Html);
                string output = $@"@""{clipboardData.Replace("\"", "\"\"")}""";
                Log.Info(output);
                string html = ClipboardHelper.ExtractHtmlFragment(clipboardData);
                ProcessHtml(survey, html);
            }
        }
    }
}
