// <copyright file="GameApiMarketOrdersResponse.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// DTO representing the buy orders response from the game API.
    /// </summary>
    public class GameApiMarketBuyOrdersResponse
    {
        /// <summary>
        /// Gets or sets the list of open buy orders.
        /// </summary>
        [JsonProperty("orders")]
        public List<GameApiMarketBuyOrder> Orders { get; set; } = new List<GameApiMarketBuyOrder>();
    }

    /// <summary>
    /// DTO representing a single buy order from the game API.
    /// </summary>
    public class GameApiMarketBuyOrder
    {
        /// <summary>
        /// Gets or sets the market order identifier.
        /// </summary>
        [JsonProperty("marketId")]
        public long MarketId { get; set; }

        /// <summary>
        /// Gets or sets the type code (e.g. "R", "C", "Bp").
        /// </summary>
        [JsonProperty("typeC")]
        public string TypeC { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the item type identifier.
        /// </summary>
        [JsonProperty("typeId")]
        public long TypeId { get; set; }

        /// <summary>
        /// Gets or sets the item name.
        /// </summary>
        [JsonProperty("itemName")]
        public string ItemName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the quantity remaining to be filled.
        /// </summary>
        [JsonProperty("amountRemaining")]
        public int AmountRemaining { get; set; }

        /// <summary>
        /// Gets or sets the original order quantity.
        /// </summary>
        [JsonProperty("amountOriginal")]
        public int AmountOriginal { get; set; }

        /// <summary>
        /// Gets or sets the quantity already filled.
        /// </summary>
        [JsonProperty("amountFilled")]
        public int AmountFilled { get; set; }

        /// <summary>
        /// Gets or sets the bid price per unit.
        /// </summary>
        [JsonProperty("price")]
        public double Price { get; set; }

        /// <summary>
        /// Gets or sets the remaining escrow amount.
        /// </summary>
        [JsonProperty("escrowRemaining")]
        public double EscrowRemaining { get; set; }

        /// <summary>
        /// Gets or sets the minimum evolution level accepted.
        /// </summary>
        [JsonProperty("minEvolution")]
        public int MinEvolution { get; set; }

        /// <summary>
        /// Gets or sets the system object identifier where the order is placed.
        /// </summary>
        [JsonProperty("systemObjectId")]
        public long SystemObjectId { get; set; }

        /// <summary>
        /// Gets or sets the location name where the order is placed.
        /// </summary>
        [JsonProperty("locationName")]
        public string LocationName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the star system identifier.
        /// </summary>
        [JsonProperty("systemId")]
        public long SystemId { get; set; }

        /// <summary>
        /// Gets or sets the star system name.
        /// </summary>
        [JsonProperty("systemName")]
        public string SystemName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time the order was placed.
        /// </summary>
        [JsonProperty("placedDT")]
        public DateTime PlacedDT { get; set; }

        /// <summary>
        /// Gets or sets the date and time the order expires.
        /// </summary>
        [JsonProperty("expiresDT")]
        public DateTime ExpiresDT { get; set; }

        /// <summary>
        /// Gets or sets the minutes remaining until the order expires.
        /// </summary>
        [JsonProperty("minutesRemaining")]
        public int MinutesRemaining { get; set; }

        /// <summary>
        /// Gets or sets the distance to the order location in jumps, or null if unknown.
        /// </summary>
        [JsonProperty("distance")]
        public int? Distance { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the order is within the character's range.
        /// </summary>
        [JsonProperty("isInRange")]
        public bool IsInRange { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the order has been outbid by a competitor.
        /// </summary>
        [JsonProperty("isOutbid")]
        public bool IsOutbid { get; set; }
    }

    /// <summary>
    /// DTO representing the sell orders response from the game API.
    /// </summary>
    public class GameApiMarketSellOrdersResponse
    {
        /// <summary>
        /// Gets or sets the list of open sell orders.
        /// </summary>
        [JsonProperty("orders")]
        public List<GameApiMarketSellOrder> Orders { get; set; } = new List<GameApiMarketSellOrder>();
    }

    /// <summary>
    /// DTO representing a single sell order from the game API.
    /// </summary>
    public class GameApiMarketSellOrder
    {
        /// <summary>
        /// Gets or sets the market order identifier.
        /// </summary>
        [JsonProperty("marketId")]
        public long MarketId { get; set; }

        /// <summary>
        /// Gets or sets the type code (e.g. "R", "C", "Bp").
        /// </summary>
        [JsonProperty("typeC")]
        public string TypeC { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the item type identifier.
        /// </summary>
        [JsonProperty("typeId")]
        public long TypeId { get; set; }

        /// <summary>
        /// Gets or sets the item name.
        /// </summary>
        [JsonProperty("itemName")]
        public string ItemName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the evolution level of the item.
        /// </summary>
        [JsonProperty("evolution")]
        public int Evolution { get; set; }

        /// <summary>
        /// Gets or sets the quantity remaining.
        /// </summary>
        [JsonProperty("amountRemaining")]
        public int AmountRemaining { get; set; }

        /// <summary>
        /// Gets or sets the original order quantity.
        /// </summary>
        [JsonProperty("amountOriginal")]
        public int AmountOriginal { get; set; }

        /// <summary>
        /// Gets or sets the quantity sold so far.
        /// </summary>
        [JsonProperty("amountSold")]
        public int AmountSold { get; set; }

        /// <summary>
        /// Gets or sets the sell price per unit.
        /// </summary>
        [JsonProperty("price")]
        public double Price { get; set; }

        /// <summary>
        /// Gets or sets the remaining value of unsold items.
        /// </summary>
        [JsonProperty("valueRemaining")]
        public double? ValueRemaining { get; set; }

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
        /// Gets or sets the character identifier for private sale recipient.
        /// </summary>
        [JsonProperty("characterIdTo")]
        public int? CharacterIdTo { get; set; }

        /// <summary>
        /// Gets or sets the name of the private sale recipient.
        /// </summary>
        [JsonProperty("privateSaleToName")]
        public string PrivateSaleToName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the estimated sales tax.
        /// </summary>
        [JsonProperty("salesTaxEstimate")]
        public double? SalesTaxEstimate { get; set; }

        /// <summary>
        /// Gets or sets the system object identifier where the order is placed.
        /// </summary>
        [JsonProperty("systemObjectId")]
        public long SystemObjectId { get; set; }

        /// <summary>
        /// Gets or sets the location name where the order is placed.
        /// </summary>
        [JsonProperty("locationName")]
        public string LocationName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the star system identifier.
        /// </summary>
        [JsonProperty("systemId")]
        public long SystemId { get; set; }

        /// <summary>
        /// Gets or sets the star system name.
        /// </summary>
        [JsonProperty("systemName")]
        public string SystemName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time the order was placed.
        /// </summary>
        [JsonProperty("placedDT")]
        public DateTime PlacedDT { get; set; }

        /// <summary>
        /// Gets or sets the date and time the order expires.
        /// </summary>
        [JsonProperty("expiresDT")]
        public DateTime ExpiresDT { get; set; }

        /// <summary>
        /// Gets or sets the minutes remaining until the order expires.
        /// </summary>
        [JsonProperty("minutesRemaining")]
        public int MinutesRemaining { get; set; }

        /// <summary>
        /// Gets or sets the distance to the order location in jumps, or null if unknown.
        /// </summary>
        [JsonProperty("distance")]
        public int? Distance { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the order is within the character's range.
        /// </summary>
        [JsonProperty("isInRange")]
        public bool IsInRange { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the order has been undercut by a competitor.
        /// </summary>
        [JsonProperty("isUndercut")]
        public bool IsUndercut { get; set; }
    }
}
