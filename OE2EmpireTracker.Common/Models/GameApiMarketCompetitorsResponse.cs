// <copyright file="GameApiMarketCompetitorsResponse.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// DTO representing the market competitor orders response from the game API.
    /// Used for both buy and sell competitor endpoints.
    /// </summary>
    public class GameApiMarketCompetitorOrdersResponse
    {
        /// <summary>
        /// Gets or sets the list of orders with their competitors.
        /// </summary>
        [JsonProperty("orders")]
        public List<GameApiMarketOrderCompetitors> Orders { get; set; } = new List<GameApiMarketOrderCompetitors>();
    }

    /// <summary>
    /// DTO representing competitors for a single market order from the game API.
    /// </summary>
    public class GameApiMarketOrderCompetitors
    {
        /// <summary>
        /// Gets or sets the market order identifier.
        /// </summary>
        [JsonProperty("marketId")]
        public long MarketId { get; set; }

        /// <summary>
        /// Gets or sets the list of competing orders.
        /// </summary>
        [JsonProperty("competitors")]
        public List<GameApiMarketCompetitor> Competitors { get; set; } = new List<GameApiMarketCompetitor>();
    }

    /// <summary>
    /// DTO representing a single competitor order from the game API.
    /// </summary>
    public class GameApiMarketCompetitor
    {
        /// <summary>
        /// Gets or sets the competitor's market order identifier.
        /// </summary>
        [JsonProperty("marketId")]
        public long MarketId { get; set; }

        /// <summary>
        /// Gets or sets the competitor's order price.
        /// </summary>
        [JsonProperty("price")]
        public double Price { get; set; }

        /// <summary>
        /// Gets or sets the remaining amount on the competitor's order.
        /// </summary>
        [JsonProperty("amountRemaining")]
        public int AmountRemaining { get; set; }

        /// <summary>
        /// Gets or sets the location name where the competitor's order is placed.
        /// </summary>
        [JsonProperty("locationName")]
        public string LocationName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the pilot name of the competitor.
        /// </summary>
        [JsonProperty("pilotName")]
        public string PilotName { get; set; } = string.Empty;
    }
}
