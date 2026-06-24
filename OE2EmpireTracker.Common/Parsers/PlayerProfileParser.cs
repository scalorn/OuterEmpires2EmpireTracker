using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

            // Extract name from ui_text_white divs -- combine first + last name
            var nameNodes = charDetail.SelectNodes(".//div[contains(@class,'ui_text_white')]");
            if (nameNodes != null && nameNodes.Count > 0)
            {
                var nameParts = nameNodes.Cast<XmlNode>()
                    .Select(n => NormalizeWhitespace(n.InnerText))
                    .Where(s => !string.IsNullOrEmpty(s));
                string fullName = string.Join(" ", nameParts);
                if (!string.IsNullOrEmpty(fullName))
                {
                    profile.Name = fullName;
                    var parts = fullName.Split(new[] { ' ' }, 2);
                    profile.FirstName = parts[0];
                    profile.LastName = parts.Length > 1 ? parts[1] : string.Empty;
                }
            }

            // Extract faction from ui_text_purple div -- strip brackets
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

            // Tooltip format is like "#11,982,019.28" -- strip # and commas, parse as decimal
            string cleaned = tooltip.Replace("#", string.Empty).Replace(",", string.Empty).Trim();
            if (decimal.TryParse(
                cleaned,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal credits))
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
                    case "Reg. Date":
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
                        rank.RankName = title;
                }

                // Extract rank level from LevelTrack_LevelNumber -- text is like "Rank 42"
                var levelNode = section.SelectSingleNode(".//div[contains(@class,'Profile_TrackInformation_Section_LevelTrack_LevelNumber')]");
                if (levelNode != null)
                {
                    string levelText = NormalizeWhitespace(levelNode.InnerText);
                    // Strip "Rank " prefix and parse the number
                    string numPart = levelText.Replace("Rank", string.Empty).Trim();
                    if (int.TryParse(numPart, out int level))
                        rank.Rank = level;
                }

                // Extract XP from Bar_Text -- format "1,010,379 / 3,063,750"
                var xpNode = section.SelectSingleNode(".//div[contains(@class,'Profile_TrackInformation_Section_LevelTrack_Bar_Text')]");
                if (xpNode != null)
                {
                    string xpText = NormalizeWhitespace(xpNode.InnerText);
                    string[] parts = xpText.Split('/');
                    if (parts.Length == 2)
                    {
                        rank.CurrentXp = ParseFormattedNumber(parts[0].Trim());
                        rank.XpToNextLevel = ParseFormattedNumber(parts[1].Trim());
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
            // Text is like "42 SP" -- strip non-digit characters and parse
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
            // Build reverse lookup: display name -> SkillGroupName enum value
            var groupLookup = new Dictionary<string, SkillGroupName>();
            foreach (SkillGroupName g in Enum.GetValues(typeof(SkillGroupName)))
            {
                groupLookup[g.ToDisplayName()] = g;
            }

            var groups = doc.SelectNodes("//div[contains(@class,'Profile_Skill_Group')]");
            if (groups == null || groups.Count == 0)
            {
                Log.Warn("No Profile_Skill_Group elements found in clipboard HTML");
                return;
            }

            foreach (XmlNode group in groups)
            {
                var nameNode = group.SelectSingleNode(".//div[contains(@class,'Profile_Skill_Group_Name')]");
                if (nameNode == null) continue;

                string rawText = NormalizeWhitespace(nameNode.InnerText);
                if (string.IsNullOrEmpty(rawText)) continue;

                // The group name div may contain trailing "..." and "Unlock for NNsp" text.
                // Strip everything from the first "..." onward to get the clean group name.
                string displayName = rawText;
                int ellipsisIdx = displayName.IndexOf("...");
                if (ellipsisIdx > 0)
                    displayName = displayName.Substring(0, ellipsisIdx).Trim();

                if (!groupLookup.TryGetValue(displayName, out SkillGroupName enumValue))
                {
                    Log.Warn("Unknown skill group display name: {0}", displayName);
                    continue;
                }

                // If the disabled div is present, the group is locked (false); otherwise unlocked (true)
                var disabledNode = group.SelectSingleNode(".//div[contains(@class,'Profile_Skill_Group_Disabled')]");
                bool isUnlocked = disabledNode == null;

                profile.SetSkillGroup(enumValue, isUnlocked);
            }
        }

        /// <summary>
        /// Extracts individual skill levels and training status within each skill group.
        /// </summary>
        internal static void ParseSkills(PlayerProfile profile, XmlDocument doc)
        {
            // Build reverse lookup: display name -> SkillName enum value
            var skillLookup = new Dictionary<string, SkillName>();
            foreach (SkillName s in Enum.GetValues(typeof(SkillName)))
            {
                skillLookup[s.ToDisplayName()] = s;
            }

            var groups = doc.SelectNodes("//div[contains(@class,'Profile_Skill_Group')]");
            if (groups == null || groups.Count == 0) return;

            foreach (XmlNode group in groups)
            {
                var skillNodes = group.SelectNodes(".//div[contains(@class,'Profile_Skill_Group_Skills_Skill')]");
                if (skillNodes == null) continue;

                foreach (XmlNode skillNode in skillNodes)
                {
                    var nameNode = skillNode.SelectSingleNode(".//div[contains(@class,'Profile_Skill_Group_Skills_Skill_Name')]");
                    if (nameNode == null) continue;

                    string displayName = NormalizeWhitespace(nameNode.InnerText);
                    if (string.IsNullOrEmpty(displayName)) continue;

                    if (!skillLookup.TryGetValue(displayName, out SkillName enumValue))
                    {
                        Log.Warn("Unknown skill display name: {0}", displayName);
                        continue;
                    }

                    var skill = profile.GetSkill(enumValue);

                    // Level = count of completed level boxes (these are <td> elements in the HTML)
                    var completedBoxes = skillNode.SelectNodes(".//*[contains(@class,'Profile_Skill_Group_Skills_Skill_Level_Box_Complete')]");
                    skill.Level = completedBoxes?.Count ?? 0;

                    // Training in progress = presence of training box (also a <td> element)
                    var trainingBox = skillNode.SelectSingleNode(".//*[contains(@class,'Profile_Skill_Group_Skills_Skill_Level_Box_Training')]");
                    skill.TrainingStarted = trainingBox != null;

                    // Training time remaining
                    if (skill.TrainingStarted)
                    {
                        // The training time is in a div whose class is exactly
                        // "Profile_Skill_Group_Skills_Skill_Level_Training" (no suffix).
                        // Use concat trick to avoid matching _Container, _Bar, _Description.
                        var timeNode = skillNode.SelectSingleNode(
                            ".//*[contains(concat(' ',@class,' '),' Profile_Skill_Group_Skills_Skill_Level_Training ')]");
                        if (timeNode != null)
                        {
                            string timeText = NormalizeWhitespace(timeNode.InnerText);
                            long totalSeconds = ParseTrainingTime(timeText);
                            if (totalSeconds > 0)
                            {
                                skill.CompletionTime.TimeRemaining = totalSeconds;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Parses a training time string like "22 days, 9 hours" or "5 hours" into total seconds.
        /// </summary>
        internal static long ParseTrainingTime(string timeText)
        {
            if (string.IsNullOrEmpty(timeText)) return 0;

            int days = 0;
            int hours = 0;

            var dayMatch = Regex.Match(timeText, @"(\d+)\s*days?");
            if (dayMatch.Success)
            {
                int.TryParse(dayMatch.Groups[1].Value, out days);
            }

            var hourMatch = Regex.Match(timeText, @"(\d+)\s*hours?");
            if (hourMatch.Success)
            {
                int.TryParse(hourMatch.Groups[1].Value, out hours);
            }

            return (((long)days * 24) + hours) * 3600;
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
                if (decimal.TryParse(
                    cleaned,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal decVal))
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
