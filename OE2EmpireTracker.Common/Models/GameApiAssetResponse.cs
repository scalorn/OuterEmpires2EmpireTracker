// <copyright file="GameApiAssetResponse.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// DTO representing the asset locations list response from the game API.
    /// </summary>
    public class GameApiAssetLocationsResponse
    {
        /// <summary>
        /// Gets or sets the list of asset locations.
        /// </summary>
        [JsonProperty("locations")]
        public List<GameApiAssetLocationEntry> Locations { get; set; } = new List<GameApiAssetLocationEntry>();
    }

    /// <summary>
    /// DTO representing a single asset location entry from the game API.
    /// </summary>
    public class GameApiAssetLocationEntry
    {
        /// <summary>
        /// Gets or sets the location identifier.
        /// </summary>
        [JsonProperty("locationId")]
        public int LocationId { get; set; }

        /// <summary>
        /// Gets or sets the location type code (e.g. "Co", "St", "Sh").
        /// </summary>
        [JsonProperty("locationType")]
        public string LocationType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the location name.
        /// </summary>
        [JsonProperty("locationName")]
        public string LocationName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the star system name.
        /// </summary>
        [JsonProperty("systemName")]
        public string SystemName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the star system identifier.
        /// </summary>
        [JsonProperty("systemId")]
        public int SystemId { get; set; }

        /// <summary>
        /// Gets or sets the number of assets at this location.
        /// </summary>
        [JsonProperty("assetCount")]
        public int AssetCount { get; set; }
    }

    /// <summary>
    /// DTO representing the asset location detail response from the game API.
    /// </summary>
    public class GameApiAssetDetailResponse
    {
        /// <summary>
        /// Gets or sets the list of cargo items at this location.
        /// </summary>
        [JsonProperty("cargo")]
        public List<GameApiAssetCargoItem> Cargo { get; set; } = new List<GameApiAssetCargoItem>();
    }

    /// <summary>
    /// DTO representing a single cargo item from the asset detail API response.
    /// </summary>
    public class GameApiAssetCargoItem
    {
        /// <summary>
        /// Gets or sets the cargo item identifier.
        /// </summary>
        [JsonProperty("cargoItemId")]
        public int CargoItemId { get; set; }

        /// <summary>
        /// Gets or sets the item type identifier.
        /// </summary>
        [JsonProperty("typeId")]
        public int TypeId { get; set; }

        /// <summary>
        /// Gets or sets the quantity of this item.
        /// </summary>
        [JsonProperty("amount")]
        public int Amount { get; set; }

        /// <summary>
        /// Gets or sets the job reference identifier.
        /// </summary>
        [JsonProperty("jobRef")]
        public int? JobRef { get; set; }

        /// <summary>
        /// Gets or sets the job delivery location identifier.
        /// </summary>
        [JsonProperty("jobDeliveryLoc")]
        public int? JobDeliveryLoc { get; set; }

        /// <summary>
        /// Gets or sets the health percentage of the item.
        /// </summary>
        [JsonProperty("healthPercentage")]
        public double? HealthPercentage { get; set; }

        /// <summary>
        /// Gets or sets the last repair health percentage.
        /// </summary>
        [JsonProperty("lastRepairHealthPercentage")]
        public double? LastRepairHealthPercentage { get; set; }

        /// <summary>
        /// Gets or sets the resource name.
        /// </summary>
        [JsonProperty("resourceName")]
        public string ResourceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the evolution level of the item.
        /// </summary>
        [JsonProperty("evolution")]
        public int? Evolution { get; set; }

        /// <summary>
        /// Gets or sets the icon path for this item.
        /// </summary>
        [JsonProperty("icon")]
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type code (e.g. "R", "C", "Bp", "S").
        /// </summary>
        [JsonProperty("typeC")]
        public string TypeC { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the mass of the item.
        /// </summary>
        [JsonProperty("mass")]
        public double? Mass { get; set; }

        /// <summary>
        /// Gets or sets the volume of the item.
        /// </summary>
        [JsonProperty("volume")]
        public double? Volume { get; set; }

        /// <summary>
        /// Gets or sets the item properties (modifications, stats).
        /// </summary>
        [JsonProperty("properties")]
        public List<GameApiAssetItemProperty> Properties { get; set; } = new List<GameApiAssetItemProperty>();

        /// <summary>
        /// Gets or sets the job name.
        /// </summary>
        [JsonProperty("jobName")]
        public string JobName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the job track.
        /// </summary>
        [JsonProperty("jobTrack")]
        public string JobTrack { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ship part type.
        /// </summary>
        [JsonProperty("shipPartType")]
        public string ShipPartType { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO representing a property on a cargo item from the asset detail API response.
    /// </summary>
    public class GameApiAssetItemProperty
    {
        /// <summary>
        /// Gets or sets the modification type identifier.
        /// </summary>
        [JsonProperty("modTypeId")]
        public int ModTypeId { get; set; }

        /// <summary>
        /// Gets or sets the property name.
        /// </summary>
        [JsonProperty("propertyName")]
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the friendly (display) property name.
        /// </summary>
        [JsonProperty("friendlyPropertyName")]
        public string FriendlyPropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the property value.
        /// </summary>
        [JsonProperty("propertyValue")]
        public decimal PropertyValue { get; set; }

        /// <summary>
        /// Gets or sets the unit of measurement.
        /// </summary>
        [JsonProperty("unit")]
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the evolution level.
        /// </summary>
        [JsonProperty("evolution")]
        public int Evolution { get; set; }

        /// <summary>
        /// Gets or sets the original (base) property value before research modifications.
        /// </summary>
        [JsonProperty("originalPropertyValue")]
        public decimal OriginalPropertyValue { get; set; }

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
