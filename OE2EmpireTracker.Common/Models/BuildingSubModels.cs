// <copyright file="BuildingSubModels.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace OE2EmpireTracker.Common.Models
{
    /// <summary>
    /// Represents a status effect applied to a building's operations.
    /// </summary>
    public class BuildingStatusEffect
    {
        /// <summary>
        /// Gets or sets the status identifier.
        /// </summary>
        [JsonProperty("statusId")]
        public int StatusId { get; set; }

        /// <summary>
        /// Gets or sets the modifier type identifier.
        /// </summary>
        [JsonProperty("modTypeId")]
        public int ModTypeId { get; set; }

        /// <summary>
        /// Gets or sets the change value applied by this effect.
        /// </summary>
        [JsonProperty("change")]
        public decimal Change { get; set; }
    }

    /// <summary>
    /// Represents an industry associated with a building.
    /// </summary>
    public class BuildingIndustry
    {
        /// <summary>
        /// Gets or sets the industry identifier.
        /// </summary>
        [JsonProperty("Id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the industry name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a worker detail requirement for a building.
    /// Maps to the colonyBuildingDetailRequirement swagger schema.
    /// </summary>
    public class BuildingDetailRequirement
    {
        /// <summary>
        /// Gets or sets the detail name (e.g. "Blue Collar Detail(s)").
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the worker identifier assigned to this detail.
        /// </summary>
        [JsonProperty("workerID")]
        public int WorkerID { get; set; }
    }

    /// <summary>
    /// Represents a building attribute with modifier information.
    /// </summary>
    public class BuildingAttribute
    {
        /// <summary>
        /// Gets or sets the modifier type identifier.
        /// </summary>
        [JsonProperty("modTypeId")]
        public int ModTypeId { get; set; }

        /// <summary>
        /// Gets or sets the property name.
        /// </summary>
        [JsonProperty("propertyName")]
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the friendly display name for the property.
        /// </summary>
        [JsonProperty("friendlyPropertyName")]
        public string FriendlyPropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the property value.
        /// </summary>
        [JsonProperty("propertyValue")]
        public string PropertyValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unit of measurement.
        /// </summary>
        [JsonProperty("unit")]
        public string Unit { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents extra properties associated with a building.
    /// </summary>
    public class BuildingExtraProperty
    {
        /// <summary>
        /// Gets or sets the first info field.
        /// </summary>
        [JsonProperty("info1")]
        public string Info1 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the second info field.
        /// </summary>
        [JsonProperty("info2")]
        public string Info2 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the third info field.
        /// </summary>
        [JsonProperty("info3")]
        public string Info3 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the fourth info field.
        /// </summary>
        [JsonProperty("info4")]
        public string Info4 { get; set; } = string.Empty;
    }
}
