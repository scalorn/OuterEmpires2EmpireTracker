// <copyright file="GameApiColonyResponse.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// DTO representing the colony list response from the game API.
    /// </summary>
    public class GameApiColonyListResponse
    {
        /// <summary>
        /// Gets or sets the list of colonies returned by the API.
        /// </summary>
        [JsonProperty("colonies")]
        public List<GameApiColonyListItem> Colonies { get; set; } = new List<GameApiColonyListItem>();
    }

    /// <summary>
    /// DTO representing a single colony in the colony list API response.
    /// </summary>
    public class GameApiColonyListItem
    {
        /// <summary>
        /// Gets or sets the colony identifier.
        /// </summary>
        [JsonProperty("colonyId")]
        public int ColonyId { get; set; }

        /// <summary>
        /// Gets or sets the colony name.
        /// </summary>
        [JsonProperty("colonyName")]
        public string ColonyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the system object (planet/moon) name.
        /// </summary>
        [JsonProperty("systemObjectName")]
        public string SystemObjectName { get; set; } = string.Empty;

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
        /// Gets or sets the colony size.
        /// </summary>
        [JsonProperty("colonySize")]
        public int ColonySize { get; set; }

        /// <summary>
        /// Gets or sets the remote access level.
        /// </summary>
        [JsonProperty("remoteAccess")]
        public int RemoteAccess { get; set; }

        /// <summary>
        /// Gets or sets the distance from the player's current location.
        /// </summary>
        [JsonProperty("distance")]
        public double Distance { get; set; }

        /// <summary>
        /// Gets or sets the surface variation value.
        /// </summary>
        [JsonProperty("surfaceVariation")]
        public int SurfaceVariation { get; set; }

        /// <summary>
        /// Gets or sets the atmospheric variation value.
        /// </summary>
        [JsonProperty("atmosVariation")]
        public int AtmosVariation { get; set; }

        /// <summary>
        /// Gets or sets the hex color value for the colony.
        /// </summary>
        [JsonProperty("hexValue")]
        public string HexValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the system object type name (e.g. planet, moon).
        /// </summary>
        [JsonProperty("systemObjectTypeName")]
        public string SystemObjectTypeName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the image prefix for the colony's visual representation.
        /// </summary>
        [JsonProperty("imagePreFix")]
        public string ImagePreFix { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the colony has manufacturing capability.
        /// </summary>
        [JsonProperty("hasManufacturing")]
        public int HasManufacturing { get; set; }

        /// <summary>
        /// Gets or sets whether manufacturing is currently in progress.
        /// </summary>
        [JsonProperty("manufacturingInProgress")]
        public int ManufacturingInProgress { get; set; }

        /// <summary>
        /// Gets or sets whether the colony has mining capability.
        /// </summary>
        [JsonProperty("hasMining")]
        public int HasMining { get; set; }

        /// <summary>
        /// Gets or sets whether mining is currently in progress.
        /// </summary>
        [JsonProperty("miningInProgress")]
        public int MiningInProgress { get; set; }

        /// <summary>
        /// Gets or sets whether the colony has refining capability.
        /// </summary>
        [JsonProperty("hasRefining")]
        public int HasRefining { get; set; }

        /// <summary>
        /// Gets or sets whether refining is currently in progress.
        /// </summary>
        [JsonProperty("refiningInProgress")]
        public int RefiningInProgress { get; set; }

        /// <summary>
        /// Gets or sets whether the colony has research capability.
        /// </summary>
        [JsonProperty("hasResearch")]
        public int HasResearch { get; set; }

        /// <summary>
        /// Gets or sets whether research is currently in progress.
        /// </summary>
        [JsonProperty("researchInProgress")]
        public int ResearchInProgress { get; set; }

        /// <summary>
        /// Gets or sets whether manufacturing is blocked.
        /// </summary>
        [JsonProperty("manufacturingBlocked")]
        public int ManufacturingBlocked { get; set; }

        /// <summary>
        /// Gets or sets the current worker attitude value.
        /// </summary>
        [JsonProperty("workerCurrentAttitude")]
        public int WorkerCurrentAttitude { get; set; }

        /// <summary>
        /// Gets or sets the contentment index value.
        /// </summary>
        [JsonProperty("contentmentIndex")]
        public int ContentmentIndex { get; set; }
    }

    /// <summary>
    /// DTO representing the colony buildings response from the game API.
    /// </summary>
    public class GameApiColonyBuildingsResponse
    {
        /// <summary>
        /// Gets or sets the list of buildings in the colony.
        /// </summary>
        [JsonProperty("buildings")]
        public List<GameApiColonyBuilding> Buildings { get; set; } = new List<GameApiColonyBuilding>();

        /// <summary>
        /// Gets or sets the summary object for the colony buildings.
        /// </summary>
        [JsonProperty("summary")]
        public object Summary { get; set; }

        /// <summary>
        /// Gets or sets the colony capacities object.
        /// </summary>
        [JsonProperty("colonyCapacities")]
        public object ColonyCapacities { get; set; }
    }

    /// <summary>
    /// DTO representing a single building in the colony buildings API response.
    /// </summary>
    public class GameApiColonyBuilding
    {
        /// <summary>
        /// Gets or sets the building identifier.
        /// </summary>
        [JsonProperty("buildingId")]
        public int BuildingId { get; set; }

        /// <summary>
        /// Gets or sets the colony building type identifier.
        /// </summary>
        [JsonProperty("colonyBuildingTypeId")]
        public int ColonyBuildingTypeId { get; set; }

        /// <summary>
        /// Gets or sets the blueprint design name.
        /// </summary>
        [JsonProperty("blueprintDesignName")]
        public string BlueprintDesignName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the building is online.
        /// </summary>
        [JsonProperty("buildingOnline")]
        public bool BuildingOnline { get; set; }

        /// <summary>
        /// Gets or sets the status identifier.
        /// </summary>
        [JsonProperty("statusId")]
        public int StatusId { get; set; }

        /// <summary>
        /// Gets or sets the construction finish time, if under construction.
        /// </summary>
        [JsonProperty("constructingBuildingFinish")]
        public DateTime? ConstructingBuildingFinish { get; set; }

        /// <summary>
        /// Gets or sets the resource identifier.
        /// </summary>
        [JsonProperty("resourceId")]
        public int ResourceId { get; set; }

        /// <summary>
        /// Gets or sets the resource icon path.
        /// </summary>
        [JsonProperty("resourceIcon")]
        public string ResourceIcon { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the resource name.
        /// </summary>
        [JsonProperty("resourceName")]
        public string ResourceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum production rate.
        /// </summary>
        [JsonProperty("maxRate")]
        public double MaxRate { get; set; }

        /// <summary>
        /// Gets or sets the next finish time for the current operation.
        /// </summary>
        [JsonProperty("nextFinish")]
        public DateTime? NextFinish { get; set; }

        /// <summary>
        /// Gets or sets the manufacture number.
        /// </summary>
        [JsonProperty("manufactureNumber")]
        public int ManufactureNumber { get; set; }

        /// <summary>
        /// Gets or sets the manufacture amount per run.
        /// </summary>
        [JsonProperty("manufactureAmountPerRun")]
        public int ManufactureAmountPerRun { get; set; }

        /// <summary>
        /// Gets or sets the current durability value.
        /// </summary>
        [JsonProperty("durabilityCurrent")]
        public double DurabilityCurrent { get; set; }

        /// <summary>
        /// Gets or sets the maximum durability value.
        /// </summary>
        [JsonProperty("durabilityMax")]
        public double DurabilityMax { get; set; }

        /// <summary>
        /// Gets or sets the operational status effects on this building.
        /// </summary>
        [JsonProperty("opsStatusEffects")]
        public List<GameApiBuildingStatusEffect> OpsStatusEffects { get; set; } = new List<GameApiBuildingStatusEffect>();

        /// <summary>
        /// Gets or sets the industries associated with this building.
        /// </summary>
        [JsonProperty("industries")]
        public List<GameApiBuildingIndustry> Industries { get; set; } = new List<GameApiBuildingIndustry>();

        /// <summary>
        /// Gets or sets the detail requirements for this building.
        /// </summary>
        [JsonProperty("detailsRequired")]
        public List<GameApiBuildingDetailRequirement> DetailsRequired { get; set; } = new List<GameApiBuildingDetailRequirement>();

        /// <summary>
        /// Gets or sets the support detail requirements for this building.
        /// </summary>
        [JsonProperty("supportDetailsRequired")]
        public List<GameApiBuildingDetailRequirement> SupportDetailsRequired { get; set; } = new List<GameApiBuildingDetailRequirement>();

        /// <summary>
        /// Gets or sets the building attributes.
        /// </summary>
        [JsonProperty("buildingAttributes")]
        public List<GameApiBuildingAttribute> BuildingAttributes { get; set; } = new List<GameApiBuildingAttribute>();

        /// <summary>
        /// Gets or sets the extra properties for this building.
        /// </summary>
        [JsonProperty("extraProperties")]
        public List<GameApiBuildingExtraProperty> ExtraProperties { get; set; } = new List<GameApiBuildingExtraProperty>();
    }

    /// <summary>
    /// DTO representing a building status effect from the game API.
    /// </summary>
    public class GameApiBuildingStatusEffect
    {
        /// <summary>
        /// Gets or sets the status identifier.
        /// </summary>
        [JsonProperty("statusId")]
        public int StatusId { get; set; }

        /// <summary>
        /// Gets or sets the modification type identifier.
        /// </summary>
        [JsonProperty("modTypeId")]
        public int ModTypeId { get; set; }

        /// <summary>
        /// Gets or sets the change value.
        /// </summary>
        [JsonProperty("change")]
        public double Change { get; set; }
    }

    /// <summary>
    /// DTO representing a building industry from the game API.
    /// </summary>
    public class GameApiBuildingIndustry
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
    /// DTO representing a building detail requirement from the game API.
    /// </summary>
    public class GameApiBuildingDetailRequirement
    {
        /// <summary>
        /// Gets or sets the requirement name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the worker identifier.
        /// </summary>
        [JsonProperty("workerID")]
        public int WorkerID { get; set; }
    }

    /// <summary>
    /// DTO representing a building attribute from the game API.
    /// </summary>
    public class GameApiBuildingAttribute
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
        /// Gets or sets the friendly property name.
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
    /// DTO representing a building extra property from the game API.
    /// </summary>
    public class GameApiBuildingExtraProperty
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

    /// <summary>
    /// DTO representing the colony warehouse response from the game API.
    /// </summary>
    public class GameApiColonyWarehouseResponse
    {
        /// <summary>
        /// Gets or sets the list of items in the colony warehouse.
        /// </summary>
        [JsonProperty("contents")]
        public List<GameApiAssetCargoItem> Contents { get; set; } = new List<GameApiAssetCargoItem>();

        /// <summary>
        /// Gets or sets the warehouse capacity.
        /// </summary>
        [JsonProperty("warehouseCapacity")]
        public int WarehouseCapacity { get; set; }
    }

    /// <summary>
    /// DTO representing a single item in the colony warehouse (assetCargoItem schema).
    /// </summary>
    public class GameApiWarehouseItem
    {
        /// <summary>
        /// Gets or sets the item identifier (nullable, may not be present for all items).
        /// </summary>
        [JsonProperty("Id")]
        public int? Id { get; set; }

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
        /// Gets or sets the resource name.
        /// </summary>
        [JsonProperty("resourceName")]
        public string ResourceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type code (e.g. "R", "C", "Bp", "Sh").
        /// </summary>
        [JsonProperty("typeC")]
        public string TypeC { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the icon path for this item.
        /// </summary>
        [JsonProperty("icon")]
        public string Icon { get; set; } = string.Empty;

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
        /// Gets or sets the evolution level of the item.
        /// </summary>
        [JsonProperty("evolution")]
        public int? Evolution { get; set; }

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
        public List<GameApiItemProperty> Properties { get; set; } = new List<GameApiItemProperty>();

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
    /// DTO representing an item property from the game API warehouse response.
    /// </summary>
    public class GameApiItemProperty
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
        public string PropertyValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unit of measurement.
        /// </summary>
        [JsonProperty("unit")]
        public string Unit { get; set; } = string.Empty;
    }
}
