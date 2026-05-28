// <copyright file="PropertyTypeDefinition.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Represents property type metadata learned from the game API.
    /// Stored in BaselineData.json and keyed by ModTypeId in the runtime cache.
    /// </summary>
    public class PropertyTypeDefinition
    {
        /// <summary>
        /// Gets or sets the mod type identifier that uniquely identifies this property type.
        /// </summary>
        [JsonProperty("modTypeId")]
        public int ModTypeId { get; set; }

        /// <summary>
        /// Gets or sets the internal property name.
        /// </summary>
        [JsonProperty("propertyName")]
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the friendly (display) property name.
        /// </summary>
        [JsonProperty("friendlyPropertyName")]
        public string FriendlyPropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unit of measurement for this property.
        /// </summary>
        [JsonProperty("unit")]
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether research improves this property (positive direction).
        /// </summary>
        [JsonProperty("researchPositive")]
        public bool ResearchPositive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this property can be researched.
        /// </summary>
        [JsonProperty("canResearch")]
        public bool CanResearch { get; set; }
    }
}
