// <copyright file="ManualRequestEndpoint.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Forms.GameApiStatus
{
    /// <summary>
    /// Categories of endpoints based on their parameter requirements.
    /// </summary>
    public enum EndpointCategory
    {
        /// <summary>No inputs needed.</summary>
        Parameterless,

        /// <summary>One numeric ID field.</summary>
        SingleId,

        /// <summary>ID plus location type dropdown.</summary>
        LocationDetail,

        /// <summary>View (required) plus optional market filters.</summary>
        MarketView,

        /// <summary>Market IDs (comma-separated).</summary>
        MarketCompetitors,
    }

    /// <summary>
    /// Metadata for a single manual-request endpoint.
    /// </summary>
    public class ManualRequestEndpoint
    {
        /// <summary>
        /// All available endpoints in display order.
        /// </summary>
        public static readonly IReadOnlyList<ManualRequestEndpoint> All = new List<ManualRequestEndpoint>
        {
            // Parameterless endpoints
            new ManualRequestEndpoint("Character", EndpointCategory.Parameterless, null),
            new ManualRequestEndpoint("Character Skills", EndpointCategory.Parameterless, null),
            new ManualRequestEndpoint("Colony List", EndpointCategory.Parameterless, null),
            new ManualRequestEndpoint("Banking Balance", EndpointCategory.Parameterless, null),
            new ManualRequestEndpoint("Banking Transactions", EndpointCategory.Parameterless, null),
            new ManualRequestEndpoint("Accepted Jobs", EndpointCategory.Parameterless, null),
            new ManualRequestEndpoint("Asset Locations", EndpointCategory.Parameterless, null),
            new ManualRequestEndpoint("Kill Mail List", EndpointCategory.Parameterless, null),
            new ManualRequestEndpoint("Ship Configuration", EndpointCategory.Parameterless, null),
            new ManualRequestEndpoint("Ship Cargo", EndpointCategory.Parameterless, null),
            new ManualRequestEndpoint("Mail List", EndpointCategory.Parameterless, null),
            new ManualRequestEndpoint("Market Buy Orders", EndpointCategory.Parameterless, null),
            new ManualRequestEndpoint("Market Sell Orders", EndpointCategory.Parameterless, null),

            // Single-ID endpoints
            new ManualRequestEndpoint("Colony Buildings", EndpointCategory.SingleId, "Colony ID"),
            new ManualRequestEndpoint("Colony Warehouse", EndpointCategory.SingleId, "Colony ID"),
            new ManualRequestEndpoint("Colony Workers", EndpointCategory.SingleId, "Colony ID"),
            new ManualRequestEndpoint("Colony Summary", EndpointCategory.SingleId, "Colony ID"),
            new ManualRequestEndpoint("Kill Mail Detail", EndpointCategory.SingleId, "Kill Mail ID"),
            new ManualRequestEndpoint("Mail Detail", EndpointCategory.SingleId, "Mail ID"),
            new ManualRequestEndpoint("Blueprint", EndpointCategory.SingleId, "Blueprint ID"),
            new ManualRequestEndpoint("Survey", EndpointCategory.SingleId, "Survey ID"),
            new ManualRequestEndpoint("Crate", EndpointCategory.SingleId, "Crate ID"),
            new ManualRequestEndpoint("Market Ship Components", EndpointCategory.SingleId, "Market ID"),

            // Special endpoints
            new ManualRequestEndpoint("Location Detail", EndpointCategory.LocationDetail, "Location ID"),
            new ManualRequestEndpoint("Market Listings", EndpointCategory.MarketView, null),
            new ManualRequestEndpoint("Market Prices", EndpointCategory.MarketView, null),
            new ManualRequestEndpoint("Market Items", EndpointCategory.MarketView, null),
            new ManualRequestEndpoint("Market Buy Competitors", EndpointCategory.MarketCompetitors, null),
            new ManualRequestEndpoint("Market Sell Competitors", EndpointCategory.MarketCompetitors, null),
        }.AsReadOnly();

        private ManualRequestEndpoint(string displayName, EndpointCategory category, string idLabel)
        {
            this.DisplayName = displayName;
            this.Category = category;
            this.IdLabel = idLabel;
        }

        /// <summary>
        /// Gets the display name shown in the endpoint dropdown.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the category determining which input controls are visible.
        /// </summary>
        public EndpointCategory Category { get; }

        /// <summary>
        /// Gets the label for the ID field (e.g. "Colony ID", "Blueprint ID"), or null if not applicable.
        /// </summary>
        public string IdLabel { get; }

        /// <summary>
        /// Finds an endpoint by its display name.
        /// </summary>
        /// <param name="name">The display name to search for.</param>
        /// <returns>The matching endpoint, or null if not found.</returns>
        public static ManualRequestEndpoint FindByDisplayName(string name)
        {
            return All.FirstOrDefault(e => e.DisplayName == name);
        }
    }
}
