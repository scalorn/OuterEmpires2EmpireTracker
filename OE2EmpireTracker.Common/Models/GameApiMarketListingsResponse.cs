// <copyright file="GameApiMarketListingsResponse.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// DTO representing the market listings response from the game API.
    /// </summary>
    public class GameApiMarketListingsResponse
    {
        /// <summary>
        /// Gets or sets the list of market listings.
        /// </summary>
        [JsonProperty("listings")]
        public List<GameApiMarketListing> Listings { get; set; } = new List<GameApiMarketListing>();
    }

    /// <summary>
    /// DTO representing a single market listing from the game API.
    /// </summary>
    public class GameApiMarketListing
    {
        /// <summary>
        /// Gets or sets the market listing identifier.
        /// </summary>
        [JsonProperty("marketId")]
        public long MarketId { get; set; }

        /// <summary>
        /// Gets or sets the location name where the listing is posted.
        /// </summary>
        [JsonProperty("locationName")]
        public string LocationName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the distance to the listing location.
        /// </summary>
        [JsonProperty("distance")]
        public string Distance { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this is a buy order.
        /// </summary>
        [JsonProperty("buyOrder")]
        public bool BuyOrder { get; set; }

        /// <summary>
        /// Gets or sets the item type name.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the item type identifier.
        /// </summary>
        [JsonProperty("typeId")]
        public long TypeId { get; set; }

        /// <summary>
        /// Gets or sets the item sub-type identifier.
        /// </summary>
        [JsonProperty("subTypeId")]
        public string SubTypeId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the group A classification.
        /// </summary>
        [JsonProperty("groupA")]
        public string GroupA { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the amount remaining in the listing.
        /// </summary>
        [JsonProperty("amountRemaining")]
        public string AmountRemaining { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the listing price.
        /// </summary>
        [JsonProperty("price")]
        public string Price { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this is the player's own order.
        /// </summary>
        [JsonProperty("ownOrder")]
        public bool OwnOrder { get; set; }

        /// <summary>
        /// Gets or sets the item description.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the item icon path.
        /// </summary>
        [JsonProperty("icon")]
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the evolution level of the item.
        /// </summary>
        [JsonProperty("evolution")]
        public string Evolution { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this is a private sale.
        /// </summary>
        [JsonProperty("privateSale")]
        public bool PrivateSale { get; set; }

        /// <summary>
        /// Gets or sets the seller's character name.
        /// </summary>
        [JsonProperty("sellerName")]
        public string SellerName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the seller's faction tag.
        /// </summary>
        [JsonProperty("sellerFactionTag")]
        public string SellerFactionTag { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the character name the private sale is directed to.
        /// </summary>
        [JsonProperty("privateSaleTo")]
        public string PrivateSaleTo { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the health percentage of the item.
        /// </summary>
        [JsonProperty("healthPercentage")]
        public double? HealthPercentage { get; set; }

        /// <summary>
        /// Gets or sets the last repair health percentage.
        /// </summary>
        [JsonProperty("lastRepairHealth")]
        public double? LastRepairHealth { get; set; }

        /// <summary>
        /// Gets or sets the base item type identifier.
        /// </summary>
        [JsonProperty("baseItemTypeId")]
        public string BaseItemTypeId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of item properties (modifications, stats).
        /// </summary>
        [JsonProperty("properties")]
        public List<GameApiMarketListingProperty> Properties { get; set; } = new List<GameApiMarketListingProperty>();

        /// <summary>
        /// Gets or sets the list of resources required for this item.
        /// </summary>
        [JsonProperty("resourcesRequired")]
        public List<GameApiMarketListingResource> ResourcesRequired { get; set; } = new List<GameApiMarketListingResource>();

        /// <summary>
        /// Gets or sets the list of blueprint properties for this item.
        /// </summary>
        [JsonProperty("blueprintProperties")]
        public List<GameApiAssetItemProperty> BlueprintProperties { get; set; } = new List<GameApiAssetItemProperty>();
    }

    /// <summary>
    /// DTO representing a property on a market listing item from the game API.
    /// </summary>
    public class GameApiMarketListingProperty
    {
        /// <summary>
        /// Gets or sets the modification type identifier.
        /// </summary>
        [JsonProperty("modTypeId")]
        public int? ModTypeId { get; set; }

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

    /// <summary>
    /// DTO representing a resource required by a market listing item from the game API.
    /// </summary>
    public class GameApiMarketListingResource
    {
        /// <summary>
        /// Gets or sets the resource identifier.
        /// </summary>
        [JsonProperty("resourceId")]
        public string ResourceId { get; set; } = string.Empty;

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
        public string ResourceAmount { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the rarity classification of this resource.
        /// </summary>
        [JsonProperty("rarityClassification")]
        public string RarityClassification { get; set; } = string.Empty;
    }
}
