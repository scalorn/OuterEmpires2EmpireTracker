// <copyright file="GameApiMarketPriceStatsResponse.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// DTO representing market price statistics from the game API.
    /// </summary>
    public class GameApiMarketPriceStatsResponse
    {
        /// <summary>
        /// Gets or sets the lowest price found in the search results.
        /// </summary>
        [JsonProperty("lowPrice")]
        public double? LowPrice { get; set; }

        /// <summary>
        /// Gets or sets the average price found in the search results.
        /// </summary>
        [JsonProperty("avgPrice")]
        public double? AvgPrice { get; set; }

        /// <summary>
        /// Gets or sets the highest price found in the search results.
        /// </summary>
        [JsonProperty("highPrice")]
        public double? HighPrice { get; set; }

        /// <summary>
        /// Gets or sets the number of samples used to compute the price statistics.
        /// </summary>
        [JsonProperty("sampleCount")]
        public int SampleCount { get; set; }

        /// <summary>
        /// Gets or sets the search radius used for the price query.
        /// </summary>
        [JsonProperty("searchRadius")]
        public string SearchRadius { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of days searched for price data.
        /// </summary>
        [JsonProperty("daysSearched")]
        public int DaysSearched { get; set; }

        /// <summary>
        /// Gets or sets the order type (e.g. "buy" or "sell").
        /// </summary>
        [JsonProperty("orderType")]
        public string OrderType { get; set; } = string.Empty;
    }
}
