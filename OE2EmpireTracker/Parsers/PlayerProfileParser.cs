using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Parsers
{
    public class PlayerProfileParser
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Parses an HTML fragment from the game's profile panel clipboard data
        /// and populates the given PlayerProfile with extracted data.
        /// </summary>
        public void ProcessHtml(PlayerProfile profile, string htmlFragment)
        {
            try
            {
                var doc = ColonyParser.ParseHtmlToXml(htmlFragment);
                if (doc == null) return;

                ParseCharacterIdentity(profile, doc);
                ParseCredits(profile, doc);
                ParseHeadlineFields(profile, doc);
                ParseRankTracks(profile, doc);
                ParseSkillPoints(profile, doc);
                ParseSkillGroups(profile, doc);
                ParseSkills(profile, doc);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error parsing player profile HTML fragment");
            }
        }

        /// <summary>
        /// Reads HTML from the clipboard and processes it into the given profile.
        /// </summary>
        public void ProcessClipboard(PlayerProfile profile)
        {
            if (Clipboard.ContainsText(TextDataFormat.Html))
            {
                string clipboardData = Clipboard.GetText(TextDataFormat.Html);
                Log.Info("Player profile clipboard data length: {0}", clipboardData.Length);
                string html = ClipboardHelper.ExtractHtmlFragment(clipboardData);
                ProcessHtml(profile, html);
            }
        }

        /// <summary>
        /// Extracts character name and faction from the top bar ui_character_detail section.
        /// </summary>
        internal static void ParseCharacterIdentity(PlayerProfile profile, XmlDocument doc)
        {
            var charDetail = doc.SelectSingleNode("//div[@id='ui_character_detail']");
            if (charDetail == null)
            {
                Log.Error("ui_character_detail element not found in clipboard HTML");
                return;
            }

            // Extract name from ui_text_white divs — combine first + last name
            var nameNodes = charDetail.SelectNodes(".//div[contains(@class,'ui_text_white')]");
            if (nameNodes != null && nameNodes.Count > 0)
            {
                var nameParts = nameNodes.Cast<XmlNode>()
                    .Select(n => NormalizeWhitespace(n.InnerText))
                    .Where(s => !string.IsNullOrEmpty(s));
                string fullName = string.Join(" ", nameParts);
                if (!string.IsNullOrEmpty(fullName))
                    profile.Name = fullName;
            }

            // Extract faction from ui_text_purple div — strip brackets
            var factionNode = charDetail.SelectSingleNode(".//div[contains(@class,'ui_text_purple')]");
            if (factionNode != null)
            {
                string factionText = NormalizeWhitespace(factionNode.InnerText);
                // Strip surrounding brackets like [NEC]
                factionText = factionText.Trim('[', ']');
                if (!string.IsNullOrEmpty(factionText))
                    profile.Faction = factionText;
            }
        }

        /// <summary>
        /// Extracts total credits from the ui_credit_detail tooltip attribute.
        /// </summary>
        internal static void ParseCredits(PlayerProfile profile, XmlDocument doc)
        {
            var creditNode = doc.SelectSingleNode("//div[@id='ui_credit_detail']//div[@data-ui-tooltip]");
            if (creditNode == null)
            {
                Log.Warn("ui_credit_detail tooltip element not found in clipboard HTML");
                return;
            }

            string tooltip = creditNode.Attributes["data-ui-tooltip"]?.Value;
            if (string.IsNullOrEmpty(tooltip))
            {
                Log.Warn("data-ui-tooltip attribute is empty on credit element");
                return;
            }

            // Tooltip format is like "#11,982,019.28" — strip # and commas, parse as decimal
            string cleaned = tooltip.Replace("#", "").Replace(",", "").Trim();
            if (decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal credits))
            {
                profile.TotalCredits = credits;
            }
            else
            {
                Log.Warn("Failed to parse credit value from tooltip: {0}", tooltip);
            }
        }

        /// <summary>
        /// Extracts CitizenId, RegistrationDate, and ActiveTime from ProfileHeadlineRow elements.
        /// </summary>
        internal static void ParseHeadlineFields(PlayerProfile profile, XmlDocument doc)
        {
            var rows = doc.SelectNodes("//div[contains(@class,'ProfileHeadlineRow')]");
            if (rows == null || rows.Count == 0)
            {
                Log.Warn("No ProfileHeadlineRow elements found in clipboard HTML");
                return;
            }

            foreach (XmlNode row in rows)
            {
                var titleNode = row.SelectSingleNode(".//div[contains(@class,'ProfileHeadline_Title')]");
                var textNode = row.SelectSingleNode(".//div[contains(@class,'ProfileHeadline_Text')]");
                if (titleNode == null || textNode == null) continue;

                string label = NormalizeWhitespace(titleNode.InnerText).TrimEnd(':');
                string value = NormalizeWhitespace(textNode.InnerText);

                switch (label)
                {
                    case "Citizen ID":
                        profile.CitizenId = value;
                        break;
                    case "Registration Date":
                        profile.RegistrationDate = value;
                        break;
                    case "Active Time":
                        profile.ActiveTime = value;
                        break;
                }
            }
        }

        /// <summary>
        /// Extracts rank level, title, current XP, and next XP for Public/Private/Military tracks.
        /// </summary>
        internal static void ParseRankTracks(PlayerProfile profile, XmlDocument doc)
        {
            var sections = doc.SelectNodes("//div[contains(@class,'Profile_TrackInformation_Section')]");
            if (sections == null || sections.Count == 0)
            {
                Log.Warn("No Profile_TrackInformation_Section elements found in clipboard HTML");
                return;
            }

            foreach (XmlNode section in sections)
            {
                // Determine which track this is by looking for Bar_Public, Bar_Private, or Bar_Military
                var barNode = section.SelectSingleNode(".//div[contains(@class,'Profile_TrackInformation_Section_LevelTrack_Bar_Public')]");
                PlayerRank rank = null;
                if (barNode != null)
                {
                    rank = profile.Public;
                }
                else
                {
                    barNode = section.SelectSingleNode(".//div[contains(@class,'Profile_TrackInformation_Section_LevelTrack_Bar_Private')]");
                    if (barNode != null)
                    {
                        rank = profile.Private;
                    }
                    else
                    {
                        barNode = section.SelectSingleNode(".//div[contains(@class,'Profile_TrackInformation_Section_LevelTrack_Bar_Military')]");
                        if (barNode != null)
                        {
                            rank = profile.Military;
                        }
                    }
                }

                if (rank == null)
                {
                    Log.Warn("Could not identify rank track type for a Profile_TrackInformation_Section");
                    continue;
                }

                // Extract rank title from the first ui_text_white div_block in the section
                // The structure has: icon, grey label ("Public Rank:"), white title ("Under Secretary (Grade 3)")
                var titleNodes = section.SelectNodes(".//div[contains(@class,'ui_text_white') and contains(@class,'div_block')]");
                if (titleNodes != null && titleNodes.Count > 0)
                {
                    string title = NormalizeWhitespace(titleNodes[0].InnerText);
                    if (!string.IsNullOrEmpty(title))
                        rank.Title = title;
                }

                // Extract rank level from LevelTrack_LevelNumber — text is like "Rank 42"
                var levelNode = section.SelectSingleNode(".//div[contains(@class,'Profile_TrackInformation_Section_LevelTrack_LevelNumber')]");
                if (levelNode != null)
                {
                    string levelText = NormalizeWhitespace(levelNode.InnerText);
                    // Strip "Rank " prefix and parse the number
                    string numPart = levelText.Replace("Rank", "").Trim();
                    if (int.TryParse(numPart, out int level))
                        rank.Rank = level;
                }

                // Extract XP from Bar_Text — format "1,010,379 / 3,063,750"
                var xpNode = section.SelectSingleNode(".//div[contains(@class,'Profile_TrackInformation_Section_LevelTrack_Bar_Text')]");
                if (xpNode != null)
                {
                    string xpText = NormalizeWhitespace(xpNode.InnerText);
                    string[] parts = xpText.Split('/');
                    if (parts.Length == 2)
                    {
                        rank.CurrentXP = ParseFormattedNumber(parts[0].Trim());
                        rank.NextXP = ParseFormattedNumber(parts[1].Trim());
                    }
                }
            }
        }

        /// <summary>
        /// Extracts available skill points from the Profile_Skills_BankedContainer_Available element.
        /// </summary>
        internal static void ParseSkillPoints(PlayerProfile profile, XmlDocument doc)
        {
            var spNode = doc.SelectSingleNode("//div[contains(@class,'Profile_Skills_BankedContainer_Available')]");
            if (spNode == null)
            {
                Log.Warn("Profile_Skills_BankedContainer_Available element not found in clipboard HTML");
                return;
            }

            string text = NormalizeWhitespace(spNode.InnerText);
            // Text is like "42 SP" — strip non-digit characters and parse
            string cleaned = new string(text.Where(c => char.IsDigit(c)).ToArray());
            if (!string.IsNullOrEmpty(cleaned) && int.TryParse(cleaned, out int sp))
            {
                profile.SkillPoints = sp;
            }
            else
            {
                Log.Warn("Failed to parse skill points from text: {0}", text);
            }
        }

        /// <summary>
        /// Extracts skill group names and locked/unlocked states.
        /// </summary>
        internal static void ParseSkillGroups(PlayerProfile profile, XmlDocument doc)
        {
            // Stub — implemented in task 4.1
        }

        /// <summary>
        /// Extracts individual skill levels and training status within each skill group.
        /// </summary>
        internal static void ParseSkills(PlayerProfile profile, XmlDocument doc)
        {
            // Stub — implemented in task 4.2
        }

        /// <summary>
        /// Strips non-numeric characters (except '.') from a formatted number string and parses to long.
        /// Handles formats like "1,234,567" or "#11,982,019.28".
        /// </summary>
        internal static long ParseFormattedNumber(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            // Keep only digits and '.'
            string cleaned = new string(text.Where(c => char.IsDigit(c) || c == '.').ToArray());

            if (string.IsNullOrEmpty(cleaned))
                return 0;

            // If there's a decimal point, parse as decimal first then truncate to long
            if (cleaned.Contains("."))
            {
                if (decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal decVal))
                    return (long)decVal;
                return 0;
            }

            if (long.TryParse(cleaned, out long result))
                return result;

            return 0;
        }

        /// <summary>
        /// Collapses whitespace runs (spaces, tabs, newlines) to a single space and trims.
        /// </summary>
        internal static string NormalizeWhitespace(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return Regex.Replace(text, @"\s+", " ").Trim();
        }
    }
}
