using Amazon.Runtime.Internal.Transform;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace OE2EmpireTracker.Forms.Blueprint
{
    public class BlueprintScanner
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static Dictionary<string, string> PropertyRemap = new Dictionary<string, string>()
        {
            { "Manufacture Run Time", "ManufactureTime" },
            { "Cargo Volume Size", "CargoVolumeSize" },
            { "Power Generated", "PowerGenerated" },
            { "Health (Hitpoints)", "Health" },
            { "Eng. Capacity Required", "EngCapacityRequired" },
            { "Power regeneration rate", "PowerRegenerationRate" },
            { "Wear and Tear Rate", "WearAndTearRate" },
            { "Maximum Damage Repair %", "MaximumDamageRepairRate" }
        };

        private static Dictionary<string, string> BPTypeImageRemap = new Dictionary<string, string>()
        {
            { "0f3e805217c98030f0c5.png", "Reactor" }
        };

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
                Log.Info(output);
                //string html = ExtractHtmlFragmentFromClipboardData(returnHtmlText);
                processHtml(blueprint, returnHtmlText);
            }
        }

        public void processHtml(Data.Blueprint blueprint, string htmlFragment)
        {
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
                XmlNode iconBaseNode = doc.SelectSingleNode("//div[contains(@class,'ui_icon_base')]");
                XmlNode titleNode = doc.SelectSingleNode("//div[contains(@class,'SmallSlideOut_Form_Row_Text_Bold')]");
                XmlNode evoNode = doc.SelectSingleNode("//div[contains(@class,'EvolutionNumber')]");
                XmlNode descNode = doc.SelectSingleNode("//div[contains(@class,'SmallSlideOut_Form_Row_Description')]");

                XmlNodeList nameNodes = doc.SelectNodes("//div[contains(@class,'ScanDetailOutputResourceName')]");
                XmlNodeList detailNodes = doc.SelectNodes("//div[contains(@class,'ScanDetailOutputResourceDetail')]");

                // Parse stats/properties from the statistics page
                XmlNodeList propNodes = doc.SelectNodes("//div[contains(@class,'ShipComponentProperty')]");

                // Populate blueprint name, evolution, techlevel and description if available
                try
                {
                    /*
                    if (evoLeftNode != null)
                    {
                        string bpTypeImage = "";
                        foreach (XmlAttribute attr in evoLeftNode.Attributes)
                        {
                            //Log.Info("attr.innerText = " + attr.InnerText);
                            //Log.Info("attr.innerXml = " + attr.InnerXml);
                            string innerXml = attr.InnerXml;
                            Log.Info("Background Inner XML = " + innerXml);
                            var match = Regex.Match(innerXml, @"background:\s*url\([""']?([^""')]+)[""']?\)");
                            if (match.Success)
                            {
                                bpTypeImage = match.Groups[1].Value;
                            }
                        }
                        string bpType;
                        if(BPTypeImageRemap.TryGetValue(bpTypeImage, out bpType))
                        {
                            blueprint.BluePrintType = bpType;
                        } else
                        {
                            Log.Info("Unknown Background Type Image = " + bpTypeImage);
                        }
                    }
                    */

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
                    Log.Error("Error extracting blueprint metadata: " + ex.Message);
                }

                // Populate blueprint properties from propNodes
                try
                {
                    if (propNodes != null && propNodes.Count > 0)
                    {
                        if (blueprint.Properties == null)
                        {
                            blueprint.Properties = new Data.PropertyBag();
                        }
                        else
                        {
                            //blueprint.Properties.Clear();
                        }

                        foreach (XmlNode prop in propNodes)
                        {
                            // Each ShipComponentProperty contains two divs: label and value
                            XmlNode labelNode = prop.SelectSingleNode(".//div[contains(@class,'CargoInfoDialogue')]");
                            XmlNode valueNode = prop.SelectSingleNode(".//div[contains(@class,'div_block') and contains(@class,'ui_text_blue_light')]");
                            if (labelNode == null || valueNode == null)
                            {
                                // Fallback: try first and second child divs
                                var childDivs = prop.SelectNodes(".//div");
                                if (childDivs != null && childDivs.Count >= 2)
                                {
                                    labelNode = childDivs[0];
                                    valueNode = childDivs[1];
                                }
                            }

                            if (labelNode != null && valueNode != null)
                            {
                                string key = labelNode.InnerText.Trim();
                                string rawValue = valueNode.InnerText.Trim();
                                // Normalize whitespace and remove any embedded arrows or parentheses content used for delta indicators
                                rawValue = Regex.Replace(rawValue, "\\s+", " ").Trim();
                                // Remove inline delta text like "(▲ 435)" or "(▼ -9)"
                                rawValue = Regex.Replace(rawValue, "\\(.*?\\)", "").Trim();

                                string remapKey = key;
                                if (!PropertyRemap.TryGetValue(key, out remapKey)) {
                                    // If no remap defined, use original key with whitespace removed for consistency
                                    remapKey = key;
                                }

                                blueprint.Properties.setProperty(remapKey, rawValue);
                                Log.Info($"Extracted property: {remapKey} = {rawValue}");
                            }
                        }

                        // Remap properties.
                        string equipClass;
                        blueprint.Properties.getString("Class", null, out equipClass);
                        if (equipClass != null)
                        {
                            blueprint.Class = int.Parse(equipClass);
                        }

                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error extracting blueprint properties: " + ex.Message);
                }

                if (blueprint.Resources == null)
                {
                    blueprint.Resources = new System.Collections.Generic.Dictionary<string, string>();
                }
                else
                {
                    //blueprint.Resources.Clear();
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
                    Log.Info($"Extracted resource: {name} = {qtyNormalized}");
                }
            }
            catch (Exception ex)
            {
                Log.Info("Error parsing blueprint HTML fragment: " + ex.Message);
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
                Log.Info("T = " + item.InnerText);
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
                Log.Info("C" + depth + " = " + item.InnerText);
                if (item.HasChildNodes)
                {
                    children((depth + 1), item.ChildNodes);
                }
            }
        }
    }
}
