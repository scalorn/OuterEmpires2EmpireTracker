// <copyright file="SyncResponse.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;
using OE2EmpireTracker.Common.Models;

namespace OE2EmpireTracker.Common.Client.FactionServer
{
    /// <summary>
    /// Response from the /api/v1/sync/snapshot endpoint.
    /// Contains server-level factions, characters, and a timestamp.
    /// </summary>
    public class SyncResponse
    {
        /// <summary>
        /// Gets or sets the factions on the server.
        /// </summary>
        [JsonProperty("factions")]
        public ServerFaction[] Factions { get; set; }

        /// <summary>
        /// Gets or sets the characters on the server.
        /// </summary>
        [JsonProperty("characters")]
        public ServerCharacter[] Characters { get; set; }

        /// <summary>
        /// Gets or sets the server timestamp for sync divergence detection.
        /// </summary>
        [JsonProperty("serverTimestamp")]
        public DateTime ServerTimestamp { get; set; }
    }
}
