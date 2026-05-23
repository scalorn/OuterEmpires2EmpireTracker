// <copyright file="SharingRuleDto.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// Data transfer object representing a sharing rule returned from the server API.
    /// </summary>
    public class SharingRuleDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the sharing rule.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the UUID of the character who owns the shared data.
        /// </summary>
        [JsonProperty("ownerCharacterUUID")]
        public string OwnerCharacterUUID { get; set; }

        /// <summary>
        /// Gets or sets the UUID of the target (character or faction) the data is shared with.
        /// </summary>
        [JsonProperty("targetUUID")]
        public string TargetUUID { get; set; }

        /// <summary>
        /// Gets or sets the target type (e.g. Character, Faction, Public).
        /// </summary>
        [JsonProperty("targetType")]
        public string TargetType { get; set; }

        /// <summary>
        /// Gets or sets the data type being shared (e.g. colonies, blueprints, surveys).
        /// </summary>
        [JsonProperty("dataType")]
        public string DataType { get; set; }

        /// <summary>
        /// Gets or sets the UUID of the specific entity being shared.
        /// </summary>
        [JsonProperty("entityUUID")]
        public string EntityUUID { get; set; }
    }
}
