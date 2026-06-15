using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for stock target Market scope shortfall calculation.
    /// Feature: market-integration
    /// </summary>
    [TestFixture]
    public class StockTargetMarketScopePropertyTests
    {
        // -------------------------------------------------------------------
        // 9.7 Stock Target Shortfall Non-Negative Property
        // **Validates: Requirements 7.3, 7.4**
        // Adjusted shortfall is always >= 0.
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property StockTargetShortfall_AlwaysNonNegative()
        {
            var targetQtyArb = Arb.From(Gen.Choose(0, 500));
            var sellQtyArb = Arb.From(Gen.Choose(0, 1000));
            var orderCountArb = Arb.From(Gen.Choose(0, 5));

            return Prop.ForAll(
                targetQtyArb,
                sellQtyArb,
                orderCountArb,
                (targetQty, sellQtyPerOrder, orderCount) =>
                {
                    string playerUUID = "player-uuid";
                    string itemRef = "Noble Gases";

                    // Build market listings — own sell orders
                    var listings = new List<MarketListing>();
                    for (int i = 0; i < orderCount; i++)
                    {
                        listings.Add(new MarketListing
                        {
                            UUID = Guid.NewGuid().ToString(),
                            MarketId = 1000 + i,
                            OwnerUUID = playerUUID,
                            BuyOrder = false,
                            ItemType = ItemType.ItemTypeEnum.Resource,
                            BaseItemTypeID = itemRef,
                            ResourcePurity = "High",
                            AmountRemaining = sellQtyPerOrder,
                        });
                    }

                    var target = new StockTarget
                    {
                        UUID = Guid.NewGuid().ToString(),
                        ItemType = ItemType.ItemTypeEnum.Resource,
                        ItemReferenceID = itemRef,
                        ItemName = "Noble Gases (High)",
                        TargetQuantity = targetQty,
                        Scope = StockTargetScope.Market,
                    };

                    var plan = new StockPlan
                    {
                        UUID = Guid.NewGuid().ToString(),
                        IsActive = true,
                        Targets = new List<StockTarget> { target },
                    };

                    // Act
                    var shortfalls = StockTargetService.CheckTargets(
                        new List<StockPlan> { plan },
                        playerUUID,
                        uuid => null,
                        uuid => null,
                        uuid => null,
                        uuid => null,
                        new List<Colony>(),
                        new List<Station>(),
                        listings);

                    // Assert: shortfall quantity is always >= 0
                    // (CheckTargets only returns entries with shortfall > 0)
                    return shortfalls.All(s => s.ShortfallQuantity >= 0);
                });
        }
    }
}
