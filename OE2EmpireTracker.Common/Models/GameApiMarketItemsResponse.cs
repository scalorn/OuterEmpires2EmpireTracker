// <copyright file="GameApiMarketItemsResponse.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// DTO representing the market item catalog search response from the game API.
    /// </summary>
    public class GameApiMarketItemsResponse
    {
        /// <summary>
        /// Gets or sets the list of market items matching the search criteria.
        /// </summary>
        [JsonProperty("items")]
        public List<GameApiMarketItem> Items { get; set; } = new List<GameApiMarketItem>();
    }

    /// <summary>
    /// DTO representing a single item in the market item catalog from the game API.
    /// </summary>
    public class GameApiMarketItem
    {
        /// <summary>
        /// Gets or sets the type code (e.g. "R", "C", "Bp", "S").
        /// </summary>
        [JsonProperty("typeC")]
        public string TypeC { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type identifier.
        /// </summary>
        [JsonProperty("typeId")]
        public long TypeId { get; set; }

        /// <summary>
        /// Gets or sets the item name.
        /// </summary>
        [JsonProperty("itemName")]
        public string ItemName { get; set; } = string.Empty;
    }
}
