// <copyright file="GameApiShipCargoResponse.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// DTO representing the ship cargo hold contents response from the game API.
    /// </summary>
    public class GameApiShipCargoResponse
    {
        /// <summary>
        /// Gets or sets the list of cargo items in the active ship's hold.
        /// </summary>
        [JsonProperty("cargo")]
        public List<GameApiAssetCargoItem> Cargo { get; set; } = new List<GameApiAssetCargoItem>();
    }
}
