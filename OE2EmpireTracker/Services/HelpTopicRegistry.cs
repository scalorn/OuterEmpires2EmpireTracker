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
            { "FormBlueprintV2", "blueprints.md" },
            { "FormSurvey", "surveys.md" },
            { "FormDeliveryRoute", "delivery-routes.md" },
            { "FormDeliveryExecution", "delivery-execution.md" },
            { "FormAutoFill", "delivery-routes.md" },
            { "FormPlayerProfile", "player-profiles.md" },
            { "FormColonyActivity", "colony-activity.md" },
            { "FormColonyDailyBuild", "colony-daily-build.md" },
            { "FormAsteroid", "asteroids.md" },
            { "FormPreferences", "preferences.md" },
            { "FormBuildPlanner", "build-planner.md" },
            { "FormPricingPlan", "pricing-plans.md" },
            { "FormSupplyChain", "supply-chains.md" },
            { "FormShipTemplate", "ships.md" },
            { "FormShipInstance", "ships.md" },
            { "FormStation", "stations.md" },
            { "FormMarket", "market.md" },
            { "FormStockTargets", "stock-targets.md" },
            { "FormContacts", "contacts.md" }
        };

        private static readonly IReadOnlyList<(string DisplayName, string FileName)> Topics =
            new List<(string, string)>
            {
                ("Table of Contents", "README.md"),
                ("Getting Started", "getting-started.md"),
                ("Colonies", "colonies.md"),
                ("Colony Activity", "colony-activity.md"),
                ("Colony Daily Build", "colony-daily-build.md"),
                ("Blueprints", "blueprints.md"),
                ("Surveys", "surveys.md"),
                ("Asteroids", "asteroids.md"),
                ("Delivery Routes", "delivery-routes.md"),
                ("Delivery Execution", "delivery-execution.md"),
                ("Build Planner", "build-planner.md"),
                ("Pricing Plans", "pricing-plans.md"),
                ("Supply Chains", "supply-chains.md"),
                ("Ships", "ships.md"),
                ("Stations", "stations.md"),
                ("Market", "market.md"),
                ("Stock Targets", "stock-targets.md"),
                ("Contacts", "contacts.md"),
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
