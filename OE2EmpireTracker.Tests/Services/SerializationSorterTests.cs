using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System.Collections.Generic;
using System.Linq;
using Bp = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    [Category("Feature: json-deterministic-order")]
    public class SerializationSorterTests
    {
        [Test]
        public void SortPlayerRoot_SortsAllTopLevelArraysByUUID()
        {
            var root = new PlayerRoot
            {
                DataVersion = 1,
                CurrentPlayerUUID = "player-1",
                PlayerProfile = new[] { new PlayerProfile { UUID = "zzz" }, new PlayerProfile { UUID = "aaa" }, new PlayerProfile { UUID = "mmm" } },
                Blueprint = new[] { new Bp { UUID = "c-bp" }, new Bp { UUID = "a-bp" }, new Bp { UUID = "b-bp" } },
                Survey = new[] { new Survey("s") { UUID = "z-sv" }, new Survey("s") { UUID = "a-sv" } },
                Colony = new[]
                {
                    new Colony { UUID = "z-col", Structures = new List<ColonyStructure>(), Commodities = new List<CommodityRequested>() },
                    new Colony { UUID = "a-col", Structures = new List<ColonyStructure>(), Commodities = new List<CommodityRequested>() }
                },
                DeliveryRoute = new[] { new DeliveryRoute { UUID = "z-dr" }, new DeliveryRoute { UUID = "a-dr" } },
                DeliveryPlan = new[] { new DeliveryPlan { UUID = "z-dp" }, new DeliveryPlan { UUID = "a-dp" } },
                PricingPlan = new[] { new PricingPlan { UUID = "z-pp" }, new PricingPlan { UUID = "a-pp" } },
                BuildPlan = new[] { new BuildPlan { UUID = "z-bpl" }, new BuildPlan { UUID = "a-bpl" } },
                ShipTemplate = new[] { new ShipTemplate { UUID = "z-st" }, new ShipTemplate { UUID = "a-st" } },
                Ship = new[] { new Ship { UUID = "z-sh" }, new Ship { UUID = "a-sh" } },
                Station = new[] { new Station { UUID = "z-stn" }, new Station { UUID = "a-stn" } },
                MarketListing = new[] { new MarketListing { UUID = "z-ml" }, new MarketListing { UUID = "a-ml" } },
                MarketTransaction = new[] { new MarketTransaction { UUID = "z-mt" }, new MarketTransaction { UUID = "a-mt" } },
                StockPlan = new[] { new StockPlan { UUID = "z-sp" }, new StockPlan { UUID = "a-sp" } },
                StockProfile = new[] { new StockProfile { UUID = "z-spf" }, new StockProfile { UUID = "a-spf" } },
                SupplyChain = new[] { new SupplyChain { UUID = "z-sc" }, new SupplyChain { UUID = "a-sc" } },
                WarehouseOverflowRule = new[] { new WarehouseOverflowRule { UUID = "z-wor" }, new WarehouseOverflowRule { UUID = "a-wor" } },
                Faction = new[] { new Faction { UUID = "z-fac" }, new Faction { UUID = "a-fac" } },
                ExternalCharacter = new[] { new ExternalCharacter { UUID = "z-ec" }, new ExternalCharacter { UUID = "a-ec" } },
                Asteroid = new[] { new Asteroid { UUID = "z-ast" }, new Asteroid { UUID = "a-ast" } }
            };

            var sorted = SerializationSorter.SortPlayerRoot(root);

            Assert.That(sorted.PlayerProfile.Select(x => x.UUID), Is.EqualTo(new[] { "aaa", "mmm", "zzz" }));
            Assert.That(sorted.Blueprint.Select(x => x.UUID), Is.EqualTo(new[] { "a-bp", "b-bp", "c-bp" }));
            Assert.That(sorted.Survey.Select(x => x.UUID), Is.EqualTo(new[] { "a-sv", "z-sv" }));
            Assert.That(sorted.Colony.Select(x => x.UUID), Is.EqualTo(new[] { "a-col", "z-col" }));
            Assert.That(sorted.DeliveryRoute.Select(x => x.UUID), Is.EqualTo(new[] { "a-dr", "z-dr" }));
            Assert.That(sorted.DeliveryPlan.Select(x => x.UUID), Is.EqualTo(new[] { "a-dp", "z-dp" }));
            Assert.That(sorted.PricingPlan.Select(x => x.UUID), Is.EqualTo(new[] { "a-pp", "z-pp" }));
            Assert.That(sorted.BuildPlan.Select(x => x.UUID), Is.EqualTo(new[] { "a-bpl", "z-bpl" }));
            Assert.That(sorted.ShipTemplate.Select(x => x.UUID), Is.EqualTo(new[] { "a-st", "z-st" }));
            Assert.That(sorted.Ship.Select(x => x.UUID), Is.EqualTo(new[] { "a-sh", "z-sh" }));
            Assert.That(sorted.Station.Select(x => x.UUID), Is.EqualTo(new[] { "a-stn", "z-stn" }));
            Assert.That(sorted.MarketListing.Select(x => x.UUID), Is.EqualTo(new[] { "a-ml", "z-ml" }));
            Assert.That(sorted.MarketTransaction.Select(x => x.UUID), Is.EqualTo(new[] { "a-mt", "z-mt" }));
            Assert.That(sorted.StockPlan.Select(x => x.UUID), Is.EqualTo(new[] { "a-sp", "z-sp" }));
            Assert.That(sorted.StockProfile.Select(x => x.UUID), Is.EqualTo(new[] { "a-spf", "z-spf" }));
            Assert.That(sorted.SupplyChain.Select(x => x.UUID), Is.EqualTo(new[] { "a-sc", "z-sc" }));
            Assert.That(sorted.WarehouseOverflowRule.Select(x => x.UUID), Is.EqualTo(new[] { "a-wor", "z-wor" }));
            Assert.That(sorted.Faction.Select(x => x.UUID), Is.EqualTo(new[] { "a-fac", "z-fac" }));
            Assert.That(sorted.ExternalCharacter.Select(x => x.UUID), Is.EqualTo(new[] { "a-ec", "z-ec" }));
            Assert.That(sorted.Asteroid.Select(x => x.UUID), Is.EqualTo(new[] { "a-ast", "z-ast" }));

            Assert.That(sorted.DataVersion, Is.EqualTo(1));
            Assert.That(sorted.CurrentPlayerUUID, Is.EqualTo("player-1"));
        }

        [Test]
        public void SortBaselineRoot_SortsAllArraysByPrimaryKey()
        {
            var root = new BaselineRoot
            {
                DataVersion = 2,
                GameConstants = new BaselineGameConstants(),
                BlueprintType = new[] { new BlueprintType { Id = "z-bt" }, new BlueprintType { Id = "a-bt" } },
                Blueprint = new[] { new Bp { UUID = "z-bp" }, new Bp { UUID = "a-bp" } },
                ShipClass = new[] { new ShipClass { Id = 9, Name = "Heavy" }, new ShipClass { Id = 1, Name = "Light" } },
                TechLevel = new[] { new TechLevel { Name = "z-tl" }, new TechLevel { Name = "a-tl" } },
                Commodity = new[]
                {
                    new Commodity { ID = "z-com", Name = "Zeta" },
                    new Commodity { ID = "a-com", Name = "Alpha" }
                },
                RefiningRecipe = new[]
                {
                    new RefiningRecipe { OutputResource = "z-rr" },
                    new RefiningRecipe { OutputResource = "a-rr" }
                },
                ResearchTime = new[]
                {
                    new ResearchTimeEntry { Evolution = 5, ResearchTimeSeconds = 500 },
                    new ResearchTimeEntry { Evolution = 1, ResearchTimeSeconds = 100 }
                }
            };

            var sorted = SerializationSorter.SortBaselineRoot(root);

            Assert.That(sorted.BlueprintType.Select(x => x.Id), Is.EqualTo(new[] { "a-bt", "z-bt" }));
            Assert.That(sorted.Blueprint.Select(x => x.UUID), Is.EqualTo(new[] { "a-bp", "z-bp" }));
            Assert.That(sorted.ShipClass.Select(x => x.Id), Is.EqualTo(new[] { 1, 9 }));
            Assert.That(sorted.TechLevel.Select(x => x.Name), Is.EqualTo(new[] { "a-tl", "z-tl" }));
            Assert.That(sorted.Commodity.Select(x => x.ID), Is.EqualTo(new[] { "a-com", "z-com" }));
            Assert.That(sorted.RefiningRecipe.Select(x => x.OutputResource), Is.EqualTo(new[] { "a-rr", "z-rr" }));
            Assert.That(sorted.ResearchTime.Select(x => x.Evolution), Is.EqualTo(new[] { 1, 5 }));

            Assert.That(sorted.DataVersion, Is.EqualTo(2));
            Assert.That(sorted.GameConstants, Is.Not.Null);
        }

        [Test]
        public void SortPlayerRoot_SortsNestedColonyArrays()
        {
            var colony = new Colony
            {
                UUID = "col-1",
                Structures = new List<ColonyStructure>
                {
                    new ColonyStructure { UUID = "z-struct" },
                    new ColonyStructure { UUID = "a-struct" },
                    new ColonyStructure { UUID = "m-struct" }
                },
                Commodities = new List<CommodityRequested>
                {
                    new CommodityRequested { Name = "Zeta Commodity" },
                    new CommodityRequested { Name = "Alpha Commodity" }
                }
            };

            var root = BuildMinimalPlayerRoot();
            root.Colony = new[] { colony };

            var sorted = SerializationSorter.SortPlayerRoot(root);

            Assert.That(sorted.Colony[0].Structures.Select(x => x.UUID),
                Is.EqualTo(new[] { "a-struct", "m-struct", "z-struct" }));
            Assert.That(sorted.Colony[0].Commodities.Select(x => x.Name),
                Is.EqualTo(new[] { "Alpha Commodity", "Zeta Commodity" }));
        }

        [Test]
        public void SortPlayerRoot_SortsNestedDeliveryRouteStops()
        {
            var route = new DeliveryRoute
            {
                UUID = "route-1",
                Stops = new List<RouteStop>
                {
                    new RouteStop { Sequence = 3, ColonyUUID = "col-c" },
                    new RouteStop { Sequence = 1, ColonyUUID = "col-a" },
                    new RouteStop { Sequence = 2, ColonyUUID = "col-b" }
                }
            };

            var root = BuildMinimalPlayerRoot();
            root.DeliveryRoute = new[] { route };

            var sorted = SerializationSorter.SortPlayerRoot(root);

            Assert.That(sorted.DeliveryRoute[0].Stops.Select(x => x.Sequence),
                Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void SortPlayerRoot_SortsNestedDeliveryPlanArrays()
        {
            var plan = new DeliveryPlan
            {
                UUID = "plan-1",
                Stops = new List<DeliveryPlanStop>
                {
                    new DeliveryPlanStop
                    {
                        Sequence = 2,
                        ColonyUUID = "col-b",
                        DropOff = new List<DeliveryItem>
                        {
                            new DeliveryItem { Name = "Zeta" },
                            new DeliveryItem { Name = "Alpha" }
                        },
                        PickUp = new List<DeliveryItem>
                        {
                            new DeliveryItem { Name = "Omega" },
                            new DeliveryItem { Name = "Beta" }
                        }
                    },
                    new DeliveryPlanStop
                    {
                        Sequence = 1,
                        ColonyUUID = "col-a",
                        DropOff = new List<DeliveryItem>(),
                        PickUp = new List<DeliveryItem>()
                    }
                }
            };

            var root = BuildMinimalPlayerRoot();
            root.DeliveryPlan = new[] { plan };

            var sorted = SerializationSorter.SortPlayerRoot(root);

            Assert.That(sorted.DeliveryPlan[0].Stops.Select(x => x.Sequence),
                Is.EqualTo(new[] { 1, 2 }));

            var stop2 = sorted.DeliveryPlan[0].Stops.First(s => s.Sequence == 2);
            Assert.That(stop2.DropOff.Select(x => x.Name), Is.EqualTo(new[] { "Alpha", "Zeta" }));
            Assert.That(stop2.PickUp.Select(x => x.Name), Is.EqualTo(new[] { "Beta", "Omega" }));
        }

        [Test]
        public void SortPlayerRoot_SortsNestedShipComponents()
        {
            var components = new List<ShipComponentSlot>
            {
                new ShipComponentSlot { SlotType = "Weapon", SlotIndex = 2 },
                new ShipComponentSlot { SlotType = "Drive", SlotIndex = 1 },
                new ShipComponentSlot { SlotType = "Weapon", SlotIndex = 1 },
                new ShipComponentSlot { SlotType = "Drive", SlotIndex = 0 }
            };

            var root = BuildMinimalPlayerRoot();
            root.ShipTemplate = new[]
            {
                new ShipTemplate { UUID = "st-1", Components = new List<ShipComponentSlot>(components) }
            };
            root.Ship = new[]
            {
                new Ship { UUID = "sh-1", Components = new List<ShipComponentSlot>(components) }
            };
            root.Station = new[]
            {
                new Station { UUID = "stn-1", Components = new List<ShipComponentSlot>(components) }
            };

            var sorted = SerializationSorter.SortPlayerRoot(root);

            var expectedOrder = new[] { ("Drive", 0), ("Drive", 1), ("Weapon", 1), ("Weapon", 2) };

            Assert.That(
                sorted.ShipTemplate[0].Components.Select(c => (c.SlotType, c.SlotIndex)),
                Is.EqualTo(expectedOrder));
            Assert.That(
                sorted.Ship[0].Components.Select(c => (c.SlotType, c.SlotIndex)),
                Is.EqualTo(expectedOrder));
            Assert.That(
                sorted.Station[0].Components.Select(c => (c.SlotType, c.SlotIndex)),
                Is.EqualTo(expectedOrder));
        }

        [Test]
        public void SortPlayerRoot_SortsNestedAsteroidReserves()
        {
            var asteroid = new Asteroid
            {
                UUID = "ast-1",
                Reserves = new List<AsteroidReserve>
                {
                    new AsteroidReserve { ResourceName = "Iron", Purity = "Medium" },
                    new AsteroidReserve { ResourceName = "Gold", Purity = "High" },
                    new AsteroidReserve { ResourceName = "Iron", Purity = "High" },
                    new AsteroidReserve { ResourceName = "Gold", Purity = "Low" }
                }
            };

            var root = BuildMinimalPlayerRoot();
            root.Asteroid = new[] { asteroid };

            var sorted = SerializationSorter.SortPlayerRoot(root);

            var expected = new[]
            {
                ("Gold", "High"),
                ("Gold", "Low"),
                ("Iron", "High"),
                ("Iron", "Medium")
            };

            Assert.That(
                sorted.Asteroid[0].Reserves.Select(r => (r.ResourceName, r.Purity)),
                Is.EqualTo(expected));
        }

        [Test]
        public void SortPlayerRoot_NullInput_ReturnsNull()
        {
            Assert.That(SerializationSorter.SortPlayerRoot(null), Is.Null);
            Assert.That(SerializationSorter.SortBaselineRoot(null), Is.Null);
        }

        private static PlayerRoot BuildMinimalPlayerRoot()
        {
            return new PlayerRoot
            {
                DataVersion = 1,
                CurrentPlayerUUID = "test-player"
            };
        }
    }
}
