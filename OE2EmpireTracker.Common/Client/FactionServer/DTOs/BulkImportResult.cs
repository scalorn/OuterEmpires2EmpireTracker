// <copyright file="BulkImportResult.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Common.Client.FactionServer
{
    /// <summary>
    /// Response from the bulk import endpoint on success.
    /// </summary>
    public class BulkImportResult
    {
        /// <summary>
        /// Gets or sets the dictionary of collection names to imported entity counts.
        /// </summary>
        [JsonProperty("imported")]
        public Dictionary<string, int> Imported { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Gets or sets the total number of entities imported.
        /// </summary>
        [JsonProperty("total")]
        public int Total { get; set; }
    }
}
