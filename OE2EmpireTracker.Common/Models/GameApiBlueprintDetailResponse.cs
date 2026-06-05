// <copyright file="GameApiBlueprintDetailResponse.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// DTO representing the blueprint detail response from the game API.
    /// </summary>
    public class GameApiBlueprintDetailResponse
    {
        /// <summary>
        /// Gets or sets the blueprint information.
        /// </summary>
        [JsonProperty("blueprint")]
        public GameApiBlueprintInfo Blueprint { get; set; }

        /// <summary>
        /// Gets or sets the list of resources required to manufacture this blueprint.
        /// </summary>
        [JsonProperty("resourcesRequired")]
        public List<GameApiBlueprintResource> ResourcesRequired { get; set; } = new List<GameApiBlueprintResource>();

        /// <summary>
        /// Gets or sets the list of blueprint properties (researchable stats).
        /// </summary>
        [JsonProperty("blueprintProperties")]
        public List<GameApiAssetItemProperty> BlueprintProperties { get; set; } = new List<GameApiAssetItemProperty>();
    }

    /// <summary>
    /// DTO representing the blueprint information within the blueprint detail response.
    /// </summary>
    public class GameApiBlueprintInfo
    {
        /// <summary>
        /// Gets or sets the blueprint identifier.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the blueprint name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the blueprint type (e.g. "Weapon", "Engine").
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the evolution level of the blueprint.
        /// </summary>
        [JsonProperty("evolution")]
        public int Evolution { get; set; }

        /// <summary>
        /// Gets or sets the part type icon identifier.
        /// </summary>
        [JsonProperty("partTypeIcon")]
        public string PartTypeIcon { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the blueprint description.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the manufacture time in seconds.
        /// </summary>
        [JsonProperty("manufactureTime")]
        public int ManufactureTime { get; set; }

        /// <summary>
        /// Gets or sets the number of items produced per manufacture cycle.
        /// </summary>
        [JsonProperty("manufactureAmount")]
        public int ManufactureAmount { get; set; }
    }

    /// <summary>
    /// DTO representing a resource required by a blueprint from the game API.
    /// </summary>
    public class GameApiBlueprintResource
    {
        /// <summary>
        /// Gets or sets the resource identifier.
        /// </summary>
        [JsonProperty("resourceId")]
        public int ResourceId { get; set; }

        /// <summary>
        /// Gets or sets the resource name.
        /// </summary>
        [JsonProperty("resourceName")]
        public string ResourceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the resource icon path.
        /// </summary>
        [JsonProperty("resourceIcon")]
        public string ResourceIcon { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the amount of this resource required.
        /// </summary>
        [JsonProperty("resourceAmount")]
        public int ResourceAmount { get; set; }

        /// <summary>
        /// Gets or sets the rarity classification of this resource.
        /// </summary>
        [JsonProperty("rarityClassification")]
        public string RarityClassification { get; set; } = string.Empty;
    }
}
