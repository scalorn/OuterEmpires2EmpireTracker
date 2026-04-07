using Amazon.Runtime.Internal.Transform;
using NLog;
using OE2EmpireTracker.Services;
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
            // Meaningful remaps — clean up game labels
            { "Health (Hitpoints)", "Health" },
            { "Maximum Damage Repair %", "Maximum Damage Repair" },
            { "The number of crew supported", "Crew Supported" },
            { "Eng. Capacity Required", "Eng Capacity Required" },
            { "Eng. Capacity Available", "Eng Capacity Available" },
            { "Power regeneration rate", "Power Regeneration Rate" },
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
        public void processClipboard(Models.Blueprint blueprint)
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
                ProcessHtml(blueprint, returnHtmlText);
            }
        }

        public void ProcessHtml(Models.Blueprint blueprint, string htmlFragment)
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
                            blueprint.Properties = new Models.PropertyBag();
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
                                // Remove inline delta text like "(? 435)" or "(? -9)"
                                rawValue = Regex.Replace(rawValue, "\\(.*?\\)", "").Trim();

                                string remapKey = key;
                                if (!PropertyRemap.TryGetValue(key, out remapKey)) {
                                    // If no remap defined, use original key with whitespace removed for consistency
                                    remapKey = key;
                                    Log.Warn("No property remap defined for: '{0}'", key);
                                }

                                blueprint.Properties.setProperty(remapKey, NormalizePropertyValue(remapKey, rawValue));
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
        /// Parses market listing HTML and extracts multiple blueprints.
        /// Market HTML uses different CSS classes than the individual blueprint page.
        /// Each expanded listing contains stats and resources for one blueprint.
        /// </summary>
        public List<Models.Blueprint> ProcessMarketHtml(string htmlFragment)
        {
            var blueprints = new List<Models.Blueprint>();

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
                XmlDocument doc = new XmlDocument() { PreserveWhitespace = true, XmlResolver = null };
                doc.Load(sgmlReader);

                // Market HTML has pairs of <tr> rows:
                // 1. MarketListingRow — contains name, evolution, price
                // 2. MarketListingRowDetail — contains expanded stats and resources
                // They are siblings in the table, not nested.

                XmlNodeList allRows = doc.SelectNodes("//tr");
                if (allRows == null) return blueprints;

                for (int i = 0; i < allRows.Count; i++)
                {
                    XmlNode row = allRows[i];
                    string rowClass = row.Attributes?["class"]?.Value ?? "";
                    if (!rowClass.Contains("MarketListingRow") || rowClass.Contains("MarketListingRowDetail"))
                        continue;

                    // This is a listing row — extract name and evolution
                    XmlNode nameNode = row.SelectSingleNode(".//div[contains(@class,'MarketListingRowDetailDescription')]");
                    if (nameNode == null) continue;

                    // Name is the direct text of the div, not including nested spans (which contain seller info like "Government")
                    string name = "";
                    foreach (XmlNode child in nameNode.ChildNodes)
                    {
                        if (child.NodeType == XmlNodeType.Text)
                        {
                            name = child.InnerText.Trim();
                            break;
                        }
                    }
                    if (string.IsNullOrEmpty(name))
                        name = nameNode.InnerText.Trim(); // fallback
                    if (string.IsNullOrEmpty(name)) continue;

                    var bp = new Models.Blueprint(name);
                    bp.UUID = System.Guid.NewGuid().ToString();

                    XmlNode evoNode = row.SelectSingleNode(".//div[contains(@class,'EvolutionNumber')]");
                    if (evoNode != null && int.TryParse(evoNode.InnerText.Trim(), out int evo))
                    {
                        bp.Evolution = evo;
                    }

                    // Look for the next sibling row which should be MarketListingRowDetail
                    XmlNode detailRow = (i + 1 < allRows.Count) ? allRows[i + 1] : null;
                    string detailClass = detailRow?.Attributes?["class"]?.Value ?? "";
                    if (detailRow != null && detailClass.Contains("MarketListingRowDetail"))
                    {
                        // Extract properties from Market_ShipComponentProperty divs
                        XmlNodeList propNodes = detailRow.SelectNodes(".//div[contains(@class,'Market_ShipComponentProperty')]");
                        if (propNodes != null)
                        {
                            foreach (XmlNode prop in propNodes)
                            {
                                XmlNode labelNode = prop.SelectSingleNode(".//div[contains(@class,'Market_ShipComponentProperty_Label')]");
                                XmlNode valueNode = prop.SelectSingleNode(".//div[contains(@class,'ui_text_blue_light')]");
                                if (labelNode == null || valueNode == null) continue;

                                string key = labelNode.InnerText.Trim();
                                string rawValue = valueNode.InnerText.Trim();
                                rawValue = Regex.Replace(rawValue, "\\s+", " ").Trim();
                                rawValue = Regex.Replace(rawValue, "\\(.*?\\)", "").Trim();

                                string remapKey;
                                if (!PropertyRemap.TryGetValue(key, out remapKey))
                                {
                                    remapKey = key;
                                }

                                bp.Properties.setProperty(remapKey, NormalizePropertyValue(remapKey, rawValue));
                            }

                            string equipClass;
                            bp.Properties.getString("Class", null, out equipClass);
                            if (equipClass != null && int.TryParse(equipClass, out int cls))
                            {
                                bp.Class = cls;
                            }
                        }

                        // Extract resources
                        XmlNodeList resNameNodes = detailRow.SelectNodes(".//div[contains(@class,'ScanDetailOutputResourceName_MarketListing')]");
                        XmlNodeList resDetailNodes = detailRow.SelectNodes(".//div[contains(@class,'ScanDetailOutputResourceDetail')]");
                        int resCount = Math.Min(resNameNodes?.Count ?? 0, resDetailNodes?.Count ?? 0);
                        for (int r = 0; r < resCount; r++)
                        {
                            string resName = resNameNodes[r].InnerText.Trim();
                            string qtyText = resDetailNodes[r].InnerText.Trim();
                            string qtyNormalized = new string(qtyText.Where(c => char.IsDigit(c)).ToArray());
                            if (string.IsNullOrEmpty(qtyNormalized)) qtyNormalized = qtyText;
                            bp.Resources[resName] = qtyNormalized;
                        }

                        // Extract blueprint type icon — sprite position from ui_icon_base background
                        XmlNode iconNode = detailRow.SelectSingleNode(".//div[contains(@class,'MarketListingRowDetailIcon')]//div[contains(@class,'ui_icon_base')]");
                        if (iconNode != null)
                        {
                            string style = iconNode.Attributes?["style"]?.Value ?? "";
                            var bgMatch = Regex.Match(style, @"background:\s*url\([""']?([^""')]+)[""']?\)\s*(-?\d+px)\s*(-?\d+px)");
                            if (bgMatch.Success)
                            {
                                string iconPosition = bgMatch.Groups[2].Value + " " + bgMatch.Groups[3].Value;
                                bp.Properties.setProperty("_IconPosition", iconPosition);

                                // Resolve icon to BlueprintType via BaselineData
                                var ec = EmpireContext.getInstance();
                                var bpType = ec?.FindBlueprintTypeByIcon(iconPosition);
                                if (bpType != null)
                                {
                                    bp.BluePrintType = bpType.Id;
                                    Log.Info($"  Icon {iconPosition} -> {bpType.Id}");
                                }
                                else
                                {
                                    Log.Warn($"  Unknown icon position: {iconPosition} for '{bp.Name}'");
                                }
                            }
                        }
                    }

                    blueprints.Add(bp);
                    Log.Info($"Market import: {bp.Name} (Ev{bp.Evolution}) — {bp.Properties.Count} properties, {bp.Resources.Count} resources");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error parsing market HTML: " + ex.Message);
            }

            return blueprints;
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
        public static string ExtractHtmlFragmentFromClipboardData(string htmlDataString)
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
        /// Normalizes property values based on the property key.
        /// Time properties like ManufactureTime get "hours" -> "h", "minutes" -> "m" etc.
        /// </summary>
        private static string NormalizePropertyValue(string key, string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            var propType = Constants.BlueprintPropertyValidation.GetPropertyType(key);

            switch (propType)
            {
                case Constants.PropertyValueType.Time:
                    // "9 hours" -> "9h", "30 minutes" -> "30m"
                    value = Regex.Replace(value, @"\s*hours?\s*", "h ", RegexOptions.IgnoreCase);
                    value = Regex.Replace(value, @"\s*minutes?\s*", "m ", RegexOptions.IgnoreCase);
                    value = Regex.Replace(value, @"\s*seconds?\s*", "s ", RegexOptions.IgnoreCase);
                    value = Regex.Replace(value, @"\s*days?\s*", "d ", RegexOptions.IgnoreCase);
                    return value.Trim();

                case Constants.PropertyValueType.Decimal:
                    // Strip units: "31.5MW/s" -> "31.5", "2.959%" -> "2.959"
                    var decMatch = Regex.Match(value, @"[+-]?\d+(\.\d+)?");
                    return decMatch.Success ? decMatch.Value : value;

                case Constants.PropertyValueType.Integer:
                    // Strip any non-digit characters except leading +/-
                    var intMatch = Regex.Match(value, @"[+-]?\d+");
                    return intMatch.Success ? intMatch.Value : value;

                default:
                    Log.Warn("No normalization rule for property: '{0}' (type: Unknown)", key);
                    return value;
            }
        }

        /// <summary>
        /// Processes HTML content by parsing with SgmlReader and debugging child nodes.
        /// </summary>
        /// <param name="inputText">The HTML string to parse.</param>
        /// <remarks>
        /// Currently used for debugging - prints inner text of each node to Debug window.
        /// Uses SgmlReader for HTML parsing with whitespace handling preserved.
        /// </remarks>
        private void ProcessHTML(string inputText)
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
        /// Used by ProcessHTML() to traverse and debug HTML node structure.
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
