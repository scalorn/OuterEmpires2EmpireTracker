using System.Collections.Generic;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Maps form type names to their corresponding help documentation filenames
    /// and provides an ordered list of all available help topics.
    /// </summary>
    public static class HelpTopicRegistry
    {
        private static readonly Dictionary<string, string> FormTopicMap = new Dictionary<string, string>
        {
            { "FormColony", "colonies.md" },
            { "FormBlueprint", "blueprints.md" },
            { "FormSurvey", "surveys.md" },
            { "FormDeliveryRoute", "delivery-routes.md" },
            { "FormDeliveryExecution", "delivery-routes.md" },
            { "FormAutoFill", "delivery-routes.md" },
            { "FormPlayerProfile", "player-profiles.md" },
            { "FormColonyActivity", "colonies.md" },
            { "FormColonyDailyBuild", "colonies.md" },
            { "FormPreferences", "preferences.md" }
        };

        private static readonly IReadOnlyList<(string DisplayName, string FileName)> Topics =
            new List<(string, string)>
            {
                ("Table of Contents", "README.md"),
                ("Getting Started", "getting-started.md"),
                ("Colonies", "colonies.md"),
                ("Blueprints", "blueprints.md"),
                ("Surveys", "surveys.md"),
                ("Delivery Routes", "delivery-routes.md"),
                ("Player Profiles", "player-profiles.md"),
                ("Background Processing", "background-processing.md"),
                ("Window State", "window-state.md"),
                ("Preferences", "preferences.md")
            };

        /// <summary>
        /// Returns the documentation filename for a given form type name,
        /// or "README.md" if no mapping exists.
        /// </summary>
        public static string GetTopicForForm(string formTypeName)
        {
            if (formTypeName != null && FormTopicMap.TryGetValue(formTypeName, out string fileName))
                return fileName;
            return "README.md";
        }

        /// <summary>
        /// Returns all help topics in display order as (DisplayName, FileName) tuples.
        /// </summary>
        public static IReadOnlyList<(string DisplayName, string FileName)> GetAllTopics()
        {
            return Topics;
        }
    }
}
