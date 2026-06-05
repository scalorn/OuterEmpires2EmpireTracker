// <copyright file="GameApiMarketShipComponentsResponse.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// DTO representing the market ship components response from the game API.
    /// </summary>
    public class GameApiMarketShipComponentsResponse
    {
        /// <summary>
        /// Gets or sets the list of ship components fitted to the listed ship.
        /// </summary>
        [JsonProperty("components")]
        public List<GameApiMarketShipComponent> Components { get; set; } = new List<GameApiMarketShipComponent>();
    }

    /// <summary>
    /// DTO representing a single ship component from the market ship components API response.
    /// </summary>
    public class GameApiMarketShipComponent
    {
        /// <summary>
        /// Gets or sets the component name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the blueprint type of the component.
        /// </summary>
        [JsonProperty("blueprintType")]
        public string BlueprintType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the component identifier.
        /// </summary>
        [JsonProperty("Id")]
        public long? Id { get; set; }

        /// <summary>
        /// Gets or sets the health percentage of the component.
        /// </summary>
        [JsonProperty("healthPercentage")]
        public double? HealthPercentage { get; set; }

        /// <summary>
        /// Gets or sets the evolution level of the component.
        /// </summary>
        [JsonProperty("evolution")]
        public int Evolution { get; set; }

        /// <summary>
        /// Gets or sets the last repair health percentage of the component.
        /// </summary>
        [JsonProperty("lastRepairHealthPercentage")]
        public double LastRepairHealthPercentage { get; set; }

        /// <summary>
        /// Gets or sets the list of component properties.
        /// </summary>
        [JsonProperty("properties")]
        public List<GameApiMarketListingProperty> Properties { get; set; } = new List<GameApiMarketListingProperty>();
    }
}
