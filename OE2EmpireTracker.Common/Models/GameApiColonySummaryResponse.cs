// <copyright file="GameApiColonySummaryResponse.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// DTO representing the colony summary response from the game API.
    /// </summary>
    public class GameApiColonySummaryResponse
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
        /// Gets or sets a value indicating whether the colony can be landed on.
        /// </summary>
        [JsonProperty("canLand")]
        public bool CanLand { get; set; }

        /// <summary>
        /// Gets or sets the character name of the colony owner.
        /// </summary>
        [JsonProperty("characterName")]
        public string CharacterName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the colony size.
        /// </summary>
        [JsonProperty("colonySize")]
        public int ColonySize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is the player's own colony.
        /// </summary>
        [JsonProperty("isOwnColony")]
        public bool IsOwnColony { get; set; }

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
        /// Gets or sets the total asset value of the colony.
        /// </summary>
        [JsonProperty("assetValue")]
        public int AssetValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the colony has a command center.
        /// </summary>
        [JsonProperty("hasCommandCenter")]
        public bool HasCommandCenter { get; set; }

        /// <summary>
        /// Gets or sets the seconds until the first building completes construction.
        /// </summary>
        [JsonProperty("timeToFirstBuildingComplete")]
        public int TimeToFirstBuildingComplete { get; set; }

        /// <summary>
        /// Gets or sets the date/time by which the claim must be solidified.
        /// </summary>
        [JsonProperty("mustSolidifyClaim")]
        public DateTime? MustSolidifyClaim { get; set; }

        /// <summary>
        /// Gets or sets the image prefix for the colony's visual representation.
        /// </summary>
        [JsonProperty("imagePreFix")]
        public string ImagePreFix { get; set; } = string.Empty;

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
        /// Gets or sets the colony notices.
        /// </summary>
        [JsonProperty("notices")]
        public List<GameApiColonyNotice> Notices { get; set; } = new List<GameApiColonyNotice>();

        /// <summary>
        /// Gets or sets the industries present at the colony.
        /// </summary>
        [JsonProperty("industriesPresent")]
        public List<GameApiColonyIndustry> IndustriesPresent { get; set; } = new List<GameApiColonyIndustry>();

        /// <summary>
        /// Gets or sets the operations present at the colony.
        /// </summary>
        [JsonProperty("operationsPresent")]
        public List<GameApiColonyOperation> OperationsPresent { get; set; } = new List<GameApiColonyOperation>();

        /// <summary>
        /// Gets or sets the colony durability information.
        /// </summary>
        [JsonProperty("colonyDurability")]
        public GameApiColonyDurability ColonyDurability { get; set; }

        /// <summary>
        /// Gets or sets the operational efficiencies for the colony.
        /// </summary>
        [JsonProperty("operationalEfficiencies")]
        public List<GameApiColonyOperationalEfficiency> OperationalEfficiencies { get; set; } = new List<GameApiColonyOperationalEfficiency>();
    }

    /// <summary>
    /// DTO representing a colony notice from the game API.
    /// </summary>
    public class GameApiColonyNotice
    {
        /// <summary>
        /// Gets or sets the notice text.
        /// </summary>
        [JsonProperty("noticeText")]
        public string NoticeText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date/time the notice was created.
        /// </summary>
        [JsonProperty("noticeDt")]
        public DateTime NoticeDt { get; set; }
    }

    /// <summary>
    /// DTO representing a colony industry from the game API.
    /// </summary>
    public class GameApiColonyIndustry
    {
        /// <summary>
        /// Gets or sets the sum of industry buildings.
        /// </summary>
        [JsonProperty("sum")]
        public int Sum { get; set; }

        /// <summary>
        /// Gets or sets the industry identifier.
        /// </summary>
        [JsonProperty("industryId")]
        public int IndustryId { get; set; }

        /// <summary>
        /// Gets or sets the industry name.
        /// </summary>
        [JsonProperty("industryName")]
        public string IndustryName { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO representing a colony operation from the game API.
    /// </summary>
    public class GameApiColonyOperation
    {
        /// <summary>
        /// Gets or sets the operation type name.
        /// </summary>
        [JsonProperty("operationType")]
        public string OperationType { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO representing the colony durability from the game API.
    /// </summary>
    public class GameApiColonyDurability
    {
        /// <summary>
        /// Gets or sets the maximum colony durability.
        /// </summary>
        [JsonProperty("maxColonyDurability")]
        public int MaxColonyDurability { get; set; }

        /// <summary>
        /// Gets or sets the current colony durability.
        /// </summary>
        [JsonProperty("currentColonyDurability")]
        public int CurrentColonyDurability { get; set; }
    }

    /// <summary>
    /// DTO representing a colony operational efficiency from the game API.
    /// </summary>
    public class GameApiColonyOperationalEfficiency
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
        public double PropertyValue { get; set; }

        /// <summary>
        /// Gets or sets the unit of measurement.
        /// </summary>
        [JsonProperty("unit")]
        public string Unit { get; set; } = string.Empty;
    }
}
