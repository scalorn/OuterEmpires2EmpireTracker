using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace OE2EmpireTracker.Forms.Blueprint
{
    public class BlueprintScanner
    {
        /// <summary>
        /// Handles the click event for the Import button.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        /// <remarks>
        /// Checks if clipboard contains HTML text and retrieves it for import operations.
        /// The HTML fragment is extracted from clipboard data which typically includes
        // start/end fragment markers. Currently commented out - can be re-enabled when needed.
        /// </remarks>
        public void processClipboard(Data.Blueprint blueprint)
        {
            String returnHtmlText = null;
            if (Clipboard.ContainsText(TextDataFormat.Html))
            {
                returnHtmlText = Clipboard.GetText(TextDataFormat.Html);
                string output = $@"@""{returnHtmlText.Replace("\"", "\"\"")}""";
                //output = $@"@""{output.Replace("\n", "\"\n")}""";
                //output = $@"@""{output.Replace("\r", "\"\r")}""";
                Debug.Print(output);
                //string html = ExtractHtmlFragmentFromClipboardData(returnHtmlText);
                processHtml(blueprint, returnHtmlText);
            }
        }

        public void processHtml(Data.Blueprint blueprint, string htmlFragment)
        {
/*
            String htmlFragment = @"Version:0.9
StartHTML:0000000155
EndHTML:0000010490
StartFragment:0000000191
EndFragment:0000010454
SourceURL:https://game.dev.outerempires.net/game
<html>
<body>
<!--StartFragment--><div class=""SmallSlideOut_FormSection"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(221, 221, 221); font-family: BarlowSemiCondensed-Light; font-size: medium; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; white-space: normal; background-color: rgba(0, 0, 0, 0.8); text-decoration-thickness: initial; text-decoration-style: initial; text-decoration-color: initial;""><br class=""Apple-interchange-newline""><span> </span><div class=""SmallSlideOut_Form_Row_NameOfItem_Section"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block; vertical-align: top; padding-left: 10px; width: 298.797px;""><div class=""SmallSlideOut_Form_Row_Text_Bold"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); font-family: BarlowSemiCondensed-Bold;""><div class=""EvolutionLeft"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background: url(&quot;0f3e805217c98030f0c5.png&quot;) -558px -718px no-repeat; width: 5px; height: 14px; display: inline-block;""></div><span> </span><div class=""EvolutionNumber"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block; font-family: BarlowSemiCondensed-Bold; font-size: 14px; color: rgb(238, 160, 34); position: relative; top: -1px;"">5</div><span> </span><div class=""EvolutionRight"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background: url(&quot;0f3e805217c98030f0c5.png&quot;) -571px -718px no-repeat; width: 5px; height: 14px; display: inline-block;""></div><span> </span>AMX-LL Reactor Core (MilSpec)</div><div class=""SmallSlideOut_Form_Row_Description SmallSlideOut_Form_Row_Description_Small"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); max-width: 300px; font-size: 13px;"">Reactor that generates power for the ship</div></div></div><div class=""LeftSlideout_SectionContent_Divider"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); margin-top: 3px; margin-bottom: 3px; width: 356.391px; height: 2px; background-image: linear-gradient(to right, rgb(0, 54, 1), rgb(0, 83, 9), rgb(0, 113, 15), rgb(0, 145, 22), rgb(0, 178, 29), rgb(0, 178, 29), rgb(0, 178, 29), rgb(0, 178, 29), rgb(0, 145, 22), rgb(0, 113, 15), rgb(0, 83, 9), rgb(0, 54, 1)); color: rgb(221, 221, 221); font-family: BarlowSemiCondensed-Light; font-size: medium; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; white-space: normal; background-color: rgba(0, 0, 0, 0.8); text-decoration-thickness: initial; text-decoration-style: initial; text-decoration-color: initial;""></div><div id=""BlueprintTabs_3KHBP"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(221, 221, 221); font-family: BarlowSemiCondensed-Light; font-size: medium; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; white-space: normal; background-color: rgba(0, 0, 0, 0.8); text-decoration-thickness: initial; text-decoration-style: initial; text-decoration-color: initial;""><div class=""BlueprintTab"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block;""><div class=""BlueprintTab_Selector BlueprintTab_Content"" id=""BlueprintStatsTab_3KHBP"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background-color: rgba(0, 237, 162, 0.17); color: rgb(0, 237, 162); display: inline-block; padding-left: 2px; padding-right: 2px; clip-path: polygon(6% 0%, 95% 0%, 100% 100%, 0% 100%);""><div class=""BlueprintTab_Text"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-left: 5px; padding-right: 5px;"">Statistics</div></div></div><span> </span><div class=""BlueprintTab"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: inline-block;""><div class=""BlueprintTab_Selector BlueprintTab_Content_Selected"" id=""BlueprintRecipeTab_3KHBP"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); background-color: rgb(0, 237, 162); color: black; display: inline-block; padding-left: 2px; padding-right: 2px; clip-path: polygon(6% 0%, 95% 0%, 100% 100%, 0% 100%);""><div class=""BlueprintTab_Text"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-left: 5px; padding-right: 5px;"">Required Resources</div></div></div></div><div class=""ScrollingArea BlueprintMainContentContainer"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); height: 325px; overflow: hidden auto; color: rgb(221, 221, 221); font-family: BarlowSemiCondensed-Light; font-size: medium; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; white-space: normal; background-color: rgba(0, 0, 0, 0.8); text-decoration-thickness: initial; text-decoration-style: initial; text-decoration-color: initial;""><div class=""SmallSlideOut_FormSection BlueprintContentSection BlueprintHiddenFirst BlueprintResourcePaddingLeft"" id=""BlueprintRecipe_3KHBP"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); display: block; padding-left: 20px;""><div class=""SmallSlideOut_Form_Row"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-bottom: 5px; padding-right: 10px;""><div class="""" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35);""><div class=""ScanRarityTypeRow"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-top: 8px;"">Common<span> </span>Elements<span> </span>Required:</div><div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(167, 167, 167); display: inline-block; width: 225px;"">Alkaline Earth Metals</div><span> </span><div class=""div_block ui_text_blue_light ScanDetailOutputResourceDetail"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block; text-align: left; width: 40px;"">9,366</div><span> </span><div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(167, 167, 167); display: inline-block; width: 225px;"">Acidic Inorganics</div><span> </span><div class=""div_block ui_text_blue_light ScanDetailOutputResourceDetail"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block; text-align: left; width: 40px;"">1,927</div><div class=""ScanRarityTypeRow"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-top: 8px;"">Uncommon<span> </span>Elements<span> </span>Required:</div><div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(167, 167, 167); display: inline-block; width: 225px;"">Heavy Trans-Metals</div><span> </span><div class=""div_block ui_text_blue_light ScanDetailOutputResourceDetail"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block; text-align: left; width: 40px;"">2,121</div><span> </span><div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(167, 167, 167); display: inline-block; width: 225px;"">Complex Non-Metallics</div><span> </span><div class=""div_block ui_text_blue_light ScanDetailOutputResourceDetail"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block; text-align: left; width: 40px;"">2,036</div><div class=""ScanRarityTypeRow"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-top: 8px;"">Rare<span> </span>Elements<span> </span>Required:</div><div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(167, 167, 167); display: inline-block; width: 225px;"">Heavy Alkaline Earth Metals</div><span> </span><div class=""div_block ui_text_blue_light ScanDetailOutputResourceDetail"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block; text-align: left; width: 40px;"">2,440</div><div class=""ScanRarityTypeRow"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); padding-top: 8px;"">Very Rare<span> </span>Elements<span> </span>Required:</div><div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(167, 167, 167); display: inline-block; width: 225px;"">S1. Translivermoric Exotics</div><span> </span><div class=""div_block ui_text_blue_light ScanDetailOutputResourceDetail"" style=""scrollbar-width: thin; scrollbar-color: rgb(67, 236, 161) rgba(0, 0, 0, 0.35); color: rgb(127, 149, 251); display: inline-block; text-align: left; width: 40px;"">699</div></div></div></div></div><!--EndFragment-->
</body>
</html>";
*/

            // Convert html fragment into Resources on the blueprint.
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

                // Extract title / evolution / tech level / description
                XmlNode titleNode = doc.SelectSingleNode("//div[contains(@class,'SmallSlideOut_Form_Row_Text_Bold')]");
                XmlNode evoNode = doc.SelectSingleNode("//div[contains(@class,'EvolutionNumber')]");
                XmlNode descNode = doc.SelectSingleNode("//div[contains(@class,'SmallSlideOut_Form_Row_Description')]");

                XmlNodeList nameNodes = doc.SelectNodes("//div[contains(@class,'ScanDetailOutputResourceName')]");
                XmlNodeList detailNodes = doc.SelectNodes("//div[contains(@class,'ScanDetailOutputResourceDetail')]");

                // Populate blueprint name, evolution, techlevel and description if available
                if (blueprint != null)
                {
                    try
                    {
                        // Evolution
                        if (evoNode != null && int.TryParse(evoNode.InnerText.Trim(), out int evo))
                        {
                            blueprint.Evolution = evo;
                        }

                        // Title contains the name and possibly tech level in parentheses
                        if (titleNode != null)
                        {
                            string titleFull = titleNode.InnerText.Trim();
                            // Remove evolution number text if present
                            if (evoNode != null)
                            {
                                titleFull = titleFull.Replace(evoNode.InnerText, "").Trim();
                            }

                            // Extract tech level in parentheses at end, e.g. "Name (MilSpec)"
                            var m = Regex.Match(titleFull, "^(.*)\\((.*)\\)$");
                            if (m.Success)
                            {
                                string nameOnly = m.Groups[1].Value.Trim();
                                string tech = m.Groups[2].Value.Trim();
                                blueprint.Name = nameOnly;
                                blueprint.TechLevel = tech;
                            }
                            else
                            {
                                blueprint.Name = titleFull;
                            }
                        }

                        if (descNode != null)
                        {
                            blueprint.Description = descNode.InnerText.Trim();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Print("Error extracting blueprint metadata: " + ex.Message);
                    }
                }

                if (blueprint.Resources == null)
                {
                    blueprint.Resources = new System.Collections.Generic.Dictionary<string, string>();
                }
                else
                {
                    blueprint.Resources.Clear();
                }

                int count = Math.Min(nameNodes?.Count ?? 0, detailNodes?.Count ?? 0);
                for (int i = 0; i < count; i++)
                {
                    string name = nameNodes[i].InnerText.Trim();
                    string qtyText = detailNodes[i].InnerText.Trim();

                    // Normalize quantity by removing any non-digit characters (commas, spaces, etc.)
                    string qtyNormalized = new string(qtyText.Where(c => char.IsDigit(c)).ToArray());
                    if (string.IsNullOrEmpty(qtyNormalized))
                    {
                        // Fallback to raw text if no digits found
                        qtyNormalized = qtyText;
                    }

                    // Store into blueprint resource dictionary. Key = resource name, Value = quantity string
                    blueprint.Resources[name] = qtyNormalized;
                }
            }
            catch (Exception ex)
            {
                Debug.Print("Error parsing blueprint HTML fragment: " + ex.Message);
            }
        }

        /// <summary>
        /// Extracts selected HTML fragment string from clipboard data by parsing header information.
        /// </summary>
        /// <param name="htmlDataString">String representing HTML clipboard data. This includes HTML header.</param>
        /// <returns>String containing only the HTML selection part of htmlDataString, without header. Returns error message if parsing fails.</returns>
        /// <remarks>
        /// Uses Microsoft's standard clipboard HTML format which wraps fragments with:
        /// - <!--StartFragment--> marker followed by byte count to fragment start
        /// - <!--EndFragment--> marker followed by byte count to fragment end
        /// 
        /// The method extracts the content between these markers to isolate just the selected fragment.
        /// 
        /// Reference: https://msdn.microsoft.com/en-us/library/aa767917(v=vs.85).aspx
        /// 
        /// TODO: Current implementation assumes 10-digit indices which may be brittle for non-standard cases.
        /// More flexible parsing should be implemented to handle edge cases.
        /// </remarks>
        internal static string ExtractHtmlFragmentFromClipboardData(string htmlDataString)
        {
            // HTML Clipboard Format:
            // (https://msdn.microsoft.com/en-us/library/aa767917(v=vs.85).aspx)
            // - Fragment contains valid HTML representing the selected area
            // - Includes opening tags and attributes for elements with end tags within selection
            // - End tags that match included opening tags
            // - Wrapped with <!--StartFragment--> and <!--EndFragment--> markers

            // Byte count from beginning of clipboard to start of fragment
            int startFragmentIndex = htmlDataString.IndexOf("StartFragment:");
            if (startFragmentIndex < 0)
            {
                return "ERROR: Unrecognized html header";
            }

            // Parse the byte offset for fragment start
            startFragmentIndex = Int32.Parse(htmlDataString.Substring(startFragmentIndex + "StartFragment:".Length, 10));
            if (startFragmentIndex < 0 || startFragmentIndex > htmlDataString.Length)
            {
                return "ERROR: Unrecognized html header";
            }

            // Byte count from beginning of clipboard to end of fragment
            int endFragmentIndex = htmlDataString.IndexOf("EndFragment:");
            if (endFragmentIndex < 0)
            {
                return "ERROR: Unrecognized html header";
            }

            // Parse the byte offset for fragment end
            endFragmentIndex = Int32.Parse(htmlDataString.Substring(endFragmentIndex + "EndFragment:".Length, 10));
            if (endFragmentIndex > htmlDataString.Length)
            {
                endFragmentIndex = htmlDataString.Length;
            }

            // Convert bytes to string using UTF-8 encoding
            byte[] bytes = Encoding.UTF8.GetBytes(htmlDataString);
            return Encoding.UTF8.GetString(bytes, startFragmentIndex, endFragmentIndex - startFragmentIndex);
        }

        /// <summary>
        /// Processes HTML content by parsing with SgmlReader and debugging child nodes.
        /// </summary>
        /// <param name="inputText">The HTML string to parse.</param>
        /// <remarks>
        /// Currently used for debugging - prints inner text of each node to Debug window.
        /// Uses SgmlReader for HTML parsing with whitespace handling preserved.
        /// </remarks>
        private void processHTML(string inputText)
        {
            StringReader reader = new StringReader(inputText);

            // Setup SgmlReader with HTML document type and settings
            Sgml.SgmlReader sgmlReader = new Sgml.SgmlReader()
            {
                DocType = "HTML",
                WhitespaceHandling = WhitespaceHandling.All,
                CaseFolding = Sgml.CaseFolding.ToLower,
                InputStream = reader
            };

            // Create document with whitespace preservation
            XmlDocument doc = new XmlDocument()
            {
                PreserveWhitespace = true,
                XmlResolver = null
            };
            doc.Load(sgmlReader);

            // Debug: Print inner text of each node
            foreach (XmlNode item in doc)
            {
                Debug.Print("T = " + item.InnerText);
                if (item.HasChildNodes)
                {
                    children(0, item.ChildNodes);
                }
            }
        }

        /// <summary>
        /// Recursively processes child nodes and prints their inner text to debug output.
        /// </summary>
        /// <param name="depth">Current recursion depth for indentation.</param>
        /// <param name="nodes">List of child nodes to process.</param>
        /// <remarks>
        /// Used by processHTML() to traverse and debug HTML node structure.
        /// Increments depth parameter for recursive calls to show nesting level.
        /// </remarks>
        private void children(int depth, XmlNodeList nodes)
        {
            foreach (XmlNode item in nodes)
            {
                Debug.Print("C" + depth + " = " + item.InnerText);
                if (item.HasChildNodes)
                {
                    children((depth + 1), item.ChildNodes);
                }
            }
        }
    }
}
