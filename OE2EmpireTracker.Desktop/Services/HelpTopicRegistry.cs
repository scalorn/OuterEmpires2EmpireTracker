using System.Collections.Generic;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Maps document type names to help file paths in docs/.
/// Provides topic lookup for context-sensitive help and the help topic list.
/// </summary>
public sealed class HelpTopicRegistry
{
    private static readonly Dictionary<string, (string DisplayName, string FileName)> TopicMap = new ()
    {
        ["ColonyList"] = ("Colonies", "colonies.md"),
        ["BlueprintList"] = ("Blueprints", "blueprints.md"),
        ["SurveyList"] = ("Surveys", "surveys.md"),
        ["ShipList"] = ("Ships", "ships.md"),
        ["ShipTemplateList"] = ("Ships", "ships.md"),
        ["DeliveryList"] = ("Delivery Routes", "delivery-routes.md"),
        ["DeliveryExecution"] = ("Delivery Execution", "delivery-execution.md"),
        ["MarketList"] = ("Market", "market.md"),
        ["Profile"] = ("Player Profiles", "player-profiles.md"),
        ["SystemList"] = ("Systems", "systems.md"),
        ["StationList"] = ("Stations", "stations.md"),
        ["BuildPlanList"] = ("Build Planner", "build-planner.md"),
        ["StockTargets"] = ("Stock Targets", "stock-targets.md"),
        ["SupplyChainList"] = ("Supply Chains", "supply-chains.md"),
        ["ContactsList"] = ("Contacts", "contacts.md"),
        ["PricingPlanList"] = ("Pricing Plans", "pricing-plans.md"),
        ["Preferences"] = ("Preferences", "preferences.md"),
        ["ColonyActivity"] = ("Colony Activity", "colony-activity.md"),
        ["AsteroidList"] = ("Asteroids", "asteroids.md"),
    };

    /// <summary>
    /// Gets the help file name for a given document type.
    /// </summary>
    /// <param name="documentType">The document type identifier.</param>
    /// <returns>The file name in docs/, or null if no topic is mapped.</returns>
    public string? GetTopicForDocumentType(string documentType)
    {
        return TopicMap.TryGetValue(documentType, out var entry) ? entry.FileName : null;
    }

    /// <summary>
    /// Gets all available help topics as (DisplayName, FileName) tuples.
    /// </summary>
    public IReadOnlyList<(string DisplayName, string FileName)> GetAllTopics()
    {
        var topics = new List<(string DisplayName, string FileName)>();
        var seen = new HashSet<string>();

        foreach (var kvp in TopicMap)
        {
            if (seen.Add(kvp.Value.FileName))
            {
                topics.Add(kvp.Value);
            }
        }

        topics.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.Ordinal));
        return topics;
    }
}
