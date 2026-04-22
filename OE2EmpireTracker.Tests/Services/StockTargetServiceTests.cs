using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class StockTargetServiceTests
    {
        private const string PlayerUUID = "player-1";

        private Colony MakeColony(string uuid, params (ItemType.ItemTypeEnum type, string refId, int qty)[] items)
        {
            var colony = new Colony { UUID = uuid, Items = new ItemBag() };
            foreach (var (type, refId, qty) in items)
            {
                colony.Items.AddItem(new Item
                {
                    UUID = Guid.NewGuid().ToString(),
                    ItemType = type,
                    BaseItemTypeID = refId,
                    Quantity = qty
                });
            }

            return colony;
        }

        private Station MakeStation(string uuid, string playerUUID, params (ItemType.ItemTypeEnum type, string refId, int qty)[] items)
        {
            var station = new Station { UUID = uuid };
            var hold = new ItemBag();
            foreach (var (type, refId, qty) in items)
            {
                hold.AddItem(new Item
                {
                    UUID = Guid.NewGuid().ToString(),
                    ItemType = type,
                    BaseItemTypeID = refId,
                    Quantity = qty
                });
            }

            station.Holds[playerUUID] = hold;
            return station;
        }

        // --- CheckTargets ---

        [Test]
        public void CheckTargets_EmptyPlans_ReturnsEmptyShortfalls()
        {
            var result = StockTargetService.CheckTargets(
                Enumerable.Empty<StockPlan>(),
                PlayerUUID,
                _ => null, _ => null, _ => null, _ => null,
                Enumerable.Empty<Colony>(),
                Enumerable.Empty<Station>());

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void CheckTargets_InactivePlan_IsSkipped()
        {
            var plan = new StockPlan
            {
                UUID = "plan-1",
                IsActive = false,
                Targets = new List<StockTarget>
                {
                    new StockTarget
                    {
                        ItemType = ItemType.ItemTypeEnum.Resource,
                        ItemReferenceID = "Iron",
                        TargetQuantity = 100,
                        Scope = StockTargetScope.EmpireWide
                    }
                }
            };

            var result = StockTargetService.CheckTargets(
                new[] { plan },
                PlayerUUID,
                _ => null, _ => null, _ => null, _ => null,
                Enumerable.Empty<Colony>(),
                Enumerable.Empty<Station>());

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void CheckTargets_SufficientInventory_NoShortfall()
        {
            var colony = MakeColony("col-1", (ItemType.ItemTypeEnum.Resource, "Iron", 200));
            var plan = new StockPlan
            {
                UUID = "plan-1",
                IsActive = true,
                Targets = new List<StockTarget>
                {
                    new StockTarget
                    {
                        ItemType = ItemType.ItemTypeEnum.Resource,
                        ItemReferenceID = "Iron",
                        TargetQuantity = 100,
                        Scope = StockTargetScope.EmpireWide
                    }
                }
            };

            var result = StockTargetService.CheckTargets(
                new[] { plan },
                PlayerUUID,
                _ => null, _ => null, _ => null, _ => null,
                new[] { colony },
                Enumerable.Empty<Station>());

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void CheckTargets_InsufficientInventory_ReturnsShortfallWithCorrectQuantity()
        {
            var colony = MakeColony("col-1", (ItemType.ItemTypeEnum.Resource, "Iron", 30));
            var plan = new StockPlan
            {
                UUID = "plan-1",
                IsActive = true,
                Targets = new List<StockTarget>
                {
                    new StockTarget
                    {
                        ItemType = ItemType.ItemTypeEnum.Resource,
                        ItemReferenceID = "Iron",
                        TargetQuantity = 100,
                        CriticalThreshold = 50,
                        Scope = StockTargetScope.EmpireWide
                    }
                }
            };

            var result = StockTargetService.CheckTargets(
                new[] { plan },
                PlayerUUID,
                _ => null, _ => null, _ => null, _ => null,
                new[] { colony },
                Enumerable.Empty<Station>());

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].ShortfallQuantity, Is.EqualTo(70));
            Assert.That(result[0].CurrentQuantity, Is.EqualTo(30));
            Assert.That(result[0].IsCritical, Is.True);
        }

        [Test]
        public void CheckTargets_ColonyScope_ChecksOnlySpecifiedColony()
        {
            var colonyA = MakeColony("col-A", (ItemType.ItemTypeEnum.Resource, "Iron", 10));
            var colonyB = MakeColony("col-B", (ItemType.ItemTypeEnum.Resource, "Iron", 200));
            var plan = new StockPlan
            {
                UUID = "plan-1",
                IsActive = true,
                Targets = new List<StockTarget>
                {
                    new StockTarget
                    {
                        ItemType = ItemType.ItemTypeEnum.Resource,
                        ItemReferenceID = "Iron",
                        TargetQuantity = 50,
                        Scope = StockTargetScope.Colony,
                        LocationUUID = "col-A"
                    }
                }
            };

            var result = StockTargetService.CheckTargets(
                new[] { plan },
                PlayerUUID,
                uuid => uuid == "col-A" ? colonyA : uuid == "col-B" ? colonyB : null,
                _ => null, _ => null, _ => null,
                new[] { colonyA, colonyB },
                Enumerable.Empty<Station>());

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].ShortfallQuantity, Is.EqualTo(40));
        }

        [Test]
        public void CheckTargets_StationScope_ChecksOnlySpecifiedStationHold()
        {
            var station = MakeStation("sta-1", PlayerUUID, (ItemType.ItemTypeEnum.Commodity, "Steel", 20));
            var plan = new StockPlan
            {
                UUID = "plan-1",
                IsActive = true,
                Targets = new List<StockTarget>
                {
                    new StockTarget
                    {
                        ItemType = ItemType.ItemTypeEnum.Commodity,
                        ItemReferenceID = "Steel",
                        TargetQuantity = 50,
                        Scope = StockTargetScope.Station,
                        LocationUUID = "sta-1"
                    }
                }
            };

            var result = StockTargetService.CheckTargets(
                new[] { plan },
                PlayerUUID,
                _ => null,
                uuid => uuid == "sta-1" ? station : null,
                _ => null, _ => null,
                Enumerable.Empty<Colony>(),
                new[] { station });

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].ShortfallQuantity, Is.EqualTo(30));
        }

        [Test]
        public void CheckTargets_EmpireWideScope_SumsAcrossAllColoniesAndStations()
        {
            var colony = MakeColony("col-1", (ItemType.ItemTypeEnum.Resource, "Iron", 40));
            var station = MakeStation("sta-1", PlayerUUID, (ItemType.ItemTypeEnum.Resource, "Iron", 30));
            var plan = new StockPlan
            {
                UUID = "plan-1",
                IsActive = true,
                Targets = new List<StockTarget>
                {
                    new StockTarget
                    {
                        ItemType = ItemType.ItemTypeEnum.Resource,
                        ItemReferenceID = "Iron",
                        TargetQuantity = 100,
                        Scope = StockTargetScope.EmpireWide
                    }
                }
            };

            var result = StockTargetService.CheckTargets(
                new[] { plan },
                PlayerUUID,
                _ => null, _ => null, _ => null, _ => null,
                new[] { colony },
                new[] { station });

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].CurrentQuantity, Is.EqualTo(70));
            Assert.That(result[0].ShortfallQuantity, Is.EqualTo(30));
        }

        [Test]
        public void CheckTargets_ShipTemplateTarget_ReturnsMinAcrossComponents()
        {
            var colony = MakeColony("col-1",
                (ItemType.ItemTypeEnum.ShipHull, "hull-bp", 5),
                (ItemType.ItemTypeEnum.ShipPart, "reactor-bp", 3),
                (ItemType.ItemTypeEnum.ShipPart, "drive-bp", 10));

            var template = new ShipTemplate
            {
                UUID = "tmpl-1",
                HullBlueprintUUID = "hull-bp",
                Components = new List<ShipComponentSlot>
                {
                    new ShipComponentSlot { BlueprintUUID = "reactor-bp" },
                    new ShipComponentSlot { BlueprintUUID = "drive-bp" }
                }
            };

            var plan = new StockPlan
            {
                UUID = "plan-1",
                IsActive = true,
                Targets = new List<StockTarget>
                {
                    new StockTarget
                    {
                        ShipTemplateUUID = "tmpl-1",
                        TargetQuantity = 5,
                        Scope = StockTargetScope.EmpireWide
                    }
                }
            };

            var result = StockTargetService.CheckTargets(
                new[] { plan },
                PlayerUUID,
                _ => null, _ => null,
                uuid => uuid == "tmpl-1" ? template : null,
                _ => null,
                new[] { colony },
                Enumerable.Empty<Station>());

            // Min across hull(5), reactor(3), drive(10) = 3; shortfall = 5 - 3 = 2
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].CurrentQuantity, Is.EqualTo(3));
            Assert.That(result[0].ShortfallQuantity, Is.EqualTo(2));
        }

        [Test]
        public void CheckTargets_NullPlans_ReturnsEmpty()
        {
            var result = StockTargetService.CheckTargets(
                null,
                PlayerUUID,
                _ => null, _ => null, _ => null, _ => null,
                Enumerable.Empty<Colony>(),
                Enumerable.Empty<Station>());

            Assert.That(result, Is.Empty);
        }

        // --- GenerateReplenishmentItems ---

        [Test]
        public void GenerateReplenishmentItems_EmptyShortfalls_ReturnsEmpty()
        {
            var result = StockTargetService.GenerateReplenishmentItems(
                new List<StockShortfall>(),
                Enumerable.Empty<BuildPlan>());

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GenerateReplenishmentItems_GeneratesBuildItemsForShortfalls()
        {
            var shortfalls = new List<StockShortfall>
            {
                new StockShortfall
                {
                    Target = new StockTarget
                    {
                        ItemType = ItemType.ItemTypeEnum.Resource,
                        ItemReferenceID = "Iron",
                        ItemName = "Iron"
                    },
                    ShortfallQuantity = 50
                }
            };

            var result = StockTargetService.GenerateReplenishmentItems(
                shortfalls, Enumerable.Empty<BuildPlan>());

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].ItemName, Is.EqualTo("Iron"));
            Assert.That(result[0].Quantity, Is.EqualTo(50));
            Assert.That(result[0].Status, Is.EqualTo(BuildItemStatus.Staged));
        }

        [Test]
        public void GenerateReplenishmentItems_SkipsExistingBuildPlanItems()
        {
            var shortfalls = new List<StockShortfall>
            {
                new StockShortfall
                {
                    Target = new StockTarget
                    {
                        ItemType = ItemType.ItemTypeEnum.Commodity,
                        ItemReferenceID = "Steel",
                        ItemName = "Steel"
                    },
                    ShortfallQuantity = 50
                }
            };

            var existingPlan = new BuildPlan
            {
                UUID = "bp-1",
                Items = new List<BuildItem>
                {
                    new BuildItem
                    {
                        ItemType = BuildItemType.Commodity,
                        BlueprintUUID = "Steel",
                        Status = BuildItemStatus.Staged
                    }
                }
            };

            var result = StockTargetService.GenerateReplenishmentItems(
                shortfalls, new[] { existingPlan });

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GenerateReplenishmentItems_NullShortfalls_ReturnsEmpty()
        {
            var result = StockTargetService.GenerateReplenishmentItems(
                null, Enumerable.Empty<BuildPlan>());

            Assert.That(result, Is.Empty);
        }
    }
}
