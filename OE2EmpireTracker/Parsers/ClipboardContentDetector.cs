using System;

namespace OE2EmpireTracker.Parsers
{
    /// <summary>
    /// Detects the content type of game HTML on the clipboard by sniffing for
    /// distinctive CSS class markers. Used to prevent cross-type imports
    /// (e.g. importing a blueprint as a survey).
    /// </summary>
    public static class ClipboardContentDetector
    {
        public enum ContentType
        {
            Unknown,
            Colony,
            Survey,
            Blueprint,
            PlayerProfile,
            MarketListing
        }

        /// <summary>
        /// Sniffs an HTML fragment for distinctive game UI markers and returns
        /// the detected content type.
        /// 
        /// Content-specific markers (Colony, Survey, Market, Blueprint) are checked
        /// before page-chrome markers (PlayerProfile) because the game UI may include
        /// ui_character_detail in the page chrome of non-profile pages.
        /// </summary>
        public static ContentType Detect(string html)
        {
            if (string.IsNullOrEmpty(html))
                return ContentType.Unknown;

            // Colony: ColonyInformation_PlanetOverview -- content-specific marker
            if (html.IndexOf("ColonyInformation_PlanetOverview", StringComparison.Ordinal) >= 0)
                return ContentType.Colony;

            // Market listing: Market_ShipComponentProperty or ScanDetailOutputResourceName_MarketListing
            // Checked BEFORE Survey because market HTML contains ScanDetailOutputResourceName_MarketListing
            // which would match the Survey check's substring search for ScanDetailOutputResourceName.
            if (html.IndexOf("Market_ShipComponentProperty", StringComparison.Ordinal) >= 0 ||
                html.IndexOf("ScanDetailOutputResourceName_MarketListing", StringComparison.Ordinal) >= 0)
                return ContentType.MarketListing;

            // Survey: ScanDetailOutputResourceName -- content-specific marker
            // Must come AFTER MarketListing check to avoid false positives from market resource rows.
            if (html.IndexOf("ScanDetailOutputResourceName", StringComparison.Ordinal) >= 0)
                return ContentType.Survey;

            // Blueprint (individual): ShipComponentProperty or SmallSlideOut_Form_Row_Description
            if (html.IndexOf("ShipComponentProperty", StringComparison.Ordinal) >= 0 ||
                html.IndexOf("SmallSlideOut_Form_Row_Description", StringComparison.Ordinal) >= 0)
                return ContentType.Blueprint;

            // Player profile: ui_character_detail or Profile_Skill_Group
            // Checked last because ui_character_detail may appear in page chrome on any game page
            if (html.IndexOf("ui_character_detail", StringComparison.Ordinal) >= 0 ||
                html.IndexOf("Profile_Skill_Group", StringComparison.Ordinal) >= 0)
                return ContentType.PlayerProfile;

            return ContentType.Unknown;
        }

        /// <summary>
        /// Returns a user-friendly description of what was detected on the clipboard.
        /// </summary>
        public static string GetDescription(ContentType type)
        {
            switch (type)
            {
                case ContentType.Colony: return "colony data";
                case ContentType.Survey: return "survey data";
                case ContentType.Blueprint: return "blueprint data";
                case ContentType.PlayerProfile: return "player profile data";
                case ContentType.MarketListing: return "market listing data";
                default: return "unrecognized content";
            }
        }
    }
}
