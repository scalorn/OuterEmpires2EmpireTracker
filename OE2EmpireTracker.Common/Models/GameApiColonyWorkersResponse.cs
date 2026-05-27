// <copyright file="GameApiColonyWorkersResponse.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// DTO representing the colony workers response from the game API.
    /// </summary>
    public class GameApiColonyWorkersResponse
    {
        /// <summary>
        /// Gets or sets the current worker attitude value.
        /// </summary>
        [JsonProperty("workerCurrentAttitude")]
        public int WorkerCurrentAttitude { get; set; }

        /// <summary>
        /// Gets or sets the list of colony modifiers.
        /// </summary>
        [JsonProperty("colonyModifiers")]
        public List<GameApiColonyModifier> ColonyModifiers { get; set; } = new List<GameApiColonyModifier>();

        /// <summary>
        /// Gets or sets the colony capacities.
        /// </summary>
        [JsonProperty("colonyCapacities")]
        public GameApiColonyCapacities ColonyCapacities { get; set; }

        /// <summary>
        /// Gets or sets the workforce commodity demands.
        /// </summary>
        [JsonProperty("workforceCommodityDemands")]
        public List<GameApiCommodityDemand> WorkforceCommodityDemands { get; set; } = new List<GameApiCommodityDemand>();

        /// <summary>
        /// Gets or sets the workforce overview.
        /// </summary>
        [JsonProperty("workforceOverview")]
        public GameApiWorkforceOverview WorkforceOverview { get; set; }

        /// <summary>
        /// Gets or sets the workforce detail list.
        /// </summary>
        [JsonProperty("workforceDetail")]
        public List<GameApiColonyWorkerDetail> WorkforceDetail { get; set; } = new List<GameApiColonyWorkerDetail>();

        /// <summary>
        /// Gets or sets the colony wages information.
        /// </summary>
        [JsonProperty("wages")]
        public GameApiColonyWages Wages { get; set; }
    }

    /// <summary>
    /// DTO representing a commodity demand from the workforce.
    /// </summary>
    public class GameApiCommodityDemand
    {
        /// <summary>
        /// Gets or sets the demand identifier.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the type category.
        /// </summary>
        [JsonProperty("typeC")]
        public string TypeC { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the commodity type.
        /// </summary>
        [JsonProperty("commodityType")]
        public string CommodityType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type identifier.
        /// </summary>
        [JsonProperty("typeId")]
        public int TypeId { get; set; }

        /// <summary>
        /// Gets or sets the type name.
        /// </summary>
        [JsonProperty("typeName")]
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the amount demanded.
        /// </summary>
        [JsonProperty("amount")]
        public int Amount { get; set; }

        /// <summary>
        /// Gets or sets the date by which the commodity is required.
        /// </summary>
        [JsonProperty("requiredBy")]
        public DateTime RequiredBy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the demand has been fulfilled.
        /// </summary>
        [JsonProperty("fulfilled")]
        public bool Fulfilled { get; set; }
    }

    /// <summary>
    /// DTO representing the workforce overview with allocation counts.
    /// </summary>
    public class GameApiWorkforceOverview
    {
        /// <summary>
        /// Gets or sets the number of allocated blue collar workers.
        /// </summary>
        [JsonProperty("blueCollarAllocated")]
        public int BlueCollarAllocated { get; set; }

        /// <summary>
        /// Gets or sets the number of unallocated blue collar workers.
        /// </summary>
        [JsonProperty("blueCollarUnallocated")]
        public int BlueCollarUnallocated { get; set; }

        /// <summary>
        /// Gets or sets the number of allocated white collar workers.
        /// </summary>
        [JsonProperty("whiteCollarAllocated")]
        public int WhiteCollarAllocated { get; set; }

        /// <summary>
        /// Gets or sets the number of unallocated white collar workers.
        /// </summary>
        [JsonProperty("whiteCollarUnallocated")]
        public int WhiteCollarUnallocated { get; set; }

        /// <summary>
        /// Gets or sets the number of allocated specialist workers.
        /// </summary>
        [JsonProperty("specialistAllocated")]
        public int SpecialistAllocated { get; set; }

        /// <summary>
        /// Gets or sets the number of unallocated specialist workers.
        /// </summary>
        [JsonProperty("specialistUnallocated")]
        public int SpecialistUnallocated { get; set; }
    }

    /// <summary>
    /// DTO representing a single worker detail entry.
    /// </summary>
    public class GameApiColonyWorkerDetail
    {
        /// <summary>
        /// Gets or sets the worker identifier.
        /// </summary>
        [JsonProperty("workerId")]
        public int WorkerId { get; set; }

        /// <summary>
        /// Gets or sets the worker type identifier.
        /// </summary>
        [JsonProperty("workerTypeId")]
        public int WorkerTypeId { get; set; }

        /// <summary>
        /// Gets or sets the building type identifier the worker is assigned to.
        /// </summary>
        [JsonProperty("buildingTypeId")]
        public int BuildingTypeId { get; set; }

        /// <summary>
        /// Gets or sets the worker name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the down tools status.
        /// </summary>
        [JsonProperty("downTools")]
        public int DownTools { get; set; }
    }

    /// <summary>
    /// DTO representing colony wages information.
    /// </summary>
    public class GameApiColonyWages
    {
        /// <summary>
        /// Gets or sets the current wage percentage.
        /// </summary>
        [JsonProperty("currentWagePercentage")]
        public int CurrentWagePercentage { get; set; }

        /// <summary>
        /// Gets or sets the galactic wage standard.
        /// </summary>
        [JsonProperty("galacticWageStandard")]
        public int GalacticWageStandard { get; set; }

        /// <summary>
        /// Gets or sets the current wage bill per cycle.
        /// </summary>
        [JsonProperty("currentWageBillPerCycle")]
        public int CurrentWageBillPerCycle { get; set; }

        /// <summary>
        /// Gets or sets the date of the last wage change.
        /// </summary>
        [JsonProperty("lastWageChange")]
        public DateTime? LastWageChange { get; set; }
    }

    /// <summary>
    /// DTO representing a colony modifier.
    /// </summary>
    public class GameApiColonyModifier
    {
        /// <summary>
        /// Gets or sets the modifier identifier.
        /// </summary>
        [JsonProperty("Id")]
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the modifier description.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the modifier number.
        /// </summary>
        [JsonProperty("modifierNumber")]
        public int ModifierNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the modifier is positive.
        /// </summary>
        [JsonProperty("positive")]
        public bool Positive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the modifier is temporary.
        /// </summary>
        [JsonProperty("temporary")]
        public bool Temporary { get; set; }
    }

    /// <summary>
    /// DTO representing colony capacity values.
    /// </summary>
    public class GameApiColonyCapacities
    {
        /// <summary>
        /// Gets or sets the power draw.
        /// </summary>
        [JsonProperty("powerDraw")]
        public double PowerDraw { get; set; }

        /// <summary>
        /// Gets or sets the power generated.
        /// </summary>
        [JsonProperty("powerGenerated")]
        public int PowerGenerated { get; set; }

        /// <summary>
        /// Gets or sets the luxuries needed.
        /// </summary>
        [JsonProperty("luxuriesNeeded")]
        public int LuxuriesNeeded { get; set; }

        /// <summary>
        /// Gets or sets the luxuries available.
        /// </summary>
        [JsonProperty("luxuriesAvailable")]
        public int LuxuriesAvailable { get; set; }

        /// <summary>
        /// Gets or sets the habitation needed.
        /// </summary>
        [JsonProperty("habitationNeeded")]
        public int HabitationNeeded { get; set; }

        /// <summary>
        /// Gets or sets the habitation available.
        /// </summary>
        [JsonProperty("habitationAvailable")]
        public int HabitationAvailable { get; set; }

        /// <summary>
        /// Gets or sets the warehouse space used.
        /// </summary>
        [JsonProperty("warehouseUsed")]
        public int WarehouseUsed { get; set; }

        /// <summary>
        /// Gets or sets the warehouse capacity.
        /// </summary>
        [JsonProperty("warehouseCapacity")]
        public int WarehouseCapacity { get; set; }

        /// <summary>
        /// Gets or sets the food needed.
        /// </summary>
        [JsonProperty("foodNeeded")]
        public int FoodNeeded { get; set; }

        /// <summary>
        /// Gets or sets the food available.
        /// </summary>
        [JsonProperty("foodAvailable")]
        public int FoodAvailable { get; set; }
    }
}
