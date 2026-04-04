using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace OE2EmpireTracker.Baseline
{
    public class SurveyParser
    {
        /// <summary>
        /// Parses an HTML fragment from the game's survey clipboard data and populates
        /// the given Survey object with extracted data.
        /// </summary>
        public void processHtml(Survey survey, string htmlFragment)
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

                // Extract the description line: "A detailed survey report taken on {date} by {name}"
                XmlNode descNode = doc.SelectSingleNode(
                    "//div[contains(@class,'SmallSlideOut_Form_Row_Description')]");
                if (descNode != null)
                {
                    string descText = descNode.InnerText.Trim();
                    ParseDescription(survey, descText);
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
            }
            catch (Exception ex)
            {
                Debug.Print("Error parsing survey HTML fragment: " + ex.Message);
            }
        }

        /// <summary>
        /// Parses the description line to extract DateTime and ScannedBy.
        /// Expected format: "A detailed survey report taken on {date} by {name}"
        /// </summary>
        public static void ParseDescription(Survey survey, string descText)
        {
            // Pattern: "...taken on {date} by {name}"
            var match = Regex.Match(descText, @"taken\s+on\s+(.+?)\s+by\s+(.+)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                survey.DateTime = match.Groups[1].Value.Trim();
                survey.ScannedBy = match.Groups[2].Value.Trim();
            }
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
            string purity = "";
            var m = Regex.Match(rawName, @"^(.+?)\s*\((.+?)\)\s*$");
            if (m.Success)
            {
                resourceName = m.Groups[1].Value.Trim();
                purity = m.Groups[2].Value.Trim();
            }

            // Extract numeric amount from "41/hour"
            string amount = new string(rawDetail.Where(c => char.IsDigit(c)).ToArray());
            if (string.IsNullOrEmpty(amount))
                amount = rawDetail;

            var resource = new SurveyResource(resourceName, purity, amount);
            survey.Resources[resourceName] = resource;
        }

        /// <summary>
        /// Reads HTML from the clipboard and processes it into the given survey.
        /// </summary>
        public void processClipboard(Survey survey)
        {
            if (Clipboard.ContainsText(TextDataFormat.Html))
            {
                string clipboardData = Clipboard.GetText(TextDataFormat.Html);
                string html = OE2EmpireTracker.Forms.Blueprint.BlueprintScanner
                    .ExtractHtmlFragmentFromClipboardData(clipboardData);
                processHtml(survey, html);
            }
        }
    }
}
