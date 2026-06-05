// <copyright file="GameApiCrateContentsResponse.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// DTO representing the crate contents response from the game API.
    /// </summary>
    public class GameApiCrateContentsResponse
    {
        /// <summary>
        /// Gets or sets the list of cargo items inside the crate.
        /// </summary>
        [JsonProperty("cargo")]
        public List<GameApiAssetCargoItem> Cargo { get; set; } = new List<GameApiAssetCargoItem>();
    }
}
