using Newtonsoft.Json;
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
    [Category("Integration")]
    public class SerializationSorterIntegrationTests
    {
        [Test]
        public void PlayerRoot_SortAndSerialize_ProducesSortedJson()
        {
            // Build a PlayerRoot with entities in reverse UUID order
            var root = new PlayerRoot
            {
                DataVersion = 1,
                CurrentPlayerUUID = "player-1",
                PlayerProfile = new[] { new PlayerProfile { UUID = "z-prof" }, new PlayerProfile { UUID = "a-prof" } },
                Blueprint = new[] { new Bp { UUID = "z-bp" }, new Bp { UUID = "a-bp" } },
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

            // Sort, serialize, deserialize
            var sorted = SerializationSorter.SortPlayerRoot(root);
            var json = JsonConvert.SerializeObject(sorted, JsonSettings.SerializerSettings);
            var deserialized = JsonConvert.DeserializeObject<PlayerRoot>(json);

            // Verify all top-level arrays are sorted by UUID
            Assert.That(deserialized.PlayerProfile.Select(x => x.UUID), Is.EqualTo(new[] { "a-prof", "z-prof" }));
            Assert.That(deserialized.Blueprint.Select(x => x.UUID), Is.EqualTo(new[] { "a-bp", "z-bp" }));
            Assert.That(deserialized.Survey.Select(x => x.UUID), Is.EqualTo(new[] { "a-sv", "z-sv" }));
            Assert.That(deserialized.Colony.Select(x => x.UUID), Is.EqualTo(new[] { "a-col", "z-col" }));
            Assert.That(deserialized.DeliveryRoute.Select(x => x.UUID), Is.EqualTo(new[] { "a-dr", "z-dr" }));
            Assert.That(deserialized.DeliveryPlan.Select(x => x.UUID), Is.EqualTo(new[] { "a-dp", "z-dp" }));
            Assert.That(deserialized.PricingPlan.Select(x => x.UUID), Is.EqualTo(new[] { "a-pp", "z-pp" }));
            Assert.That(deserialized.BuildPlan.Select(x => x.UUID), Is.EqualTo(new[] { "a-bpl", "z-bpl" }));
            Assert.That(deserialized.ShipTemplate.Select(x => x.UUID), Is.EqualTo(new[] { "a-st", "z-st" }));
            Assert.That(deserialized.Ship.Select(x => x.UUID), Is.EqualTo(new[] { "a-sh", "z-sh" }));
            Assert.That(deserialized.Station.Select(x => x.UUID), Is.EqualTo(new[] { "a-stn", "z-stn" }));
            Assert.That(deserialized.MarketListing.Select(x => x.UUID), Is.EqualTo(new[] { "a-ml", "z-ml" }));
            Assert.That(deserialized.MarketTransaction.Select(x => x.UUID), Is.EqualTo(new[] { "a-mt", "z-mt" }));
            Assert.That(deserialized.StockPlan.Select(x => x.UUID), Is.EqualTo(new[] { "a-sp", "z-sp" }));
            Assert.That(deserialized.StockProfile.Select(x => x.UUID), Is.EqualTo(new[] { "a-spf", "z-spf" }));
            Assert.That(deserialized.SupplyChain.Select(x => x.UUID), Is.EqualTo(new[] { "a-sc", "z-sc" }));
            Assert.That(deserialized.WarehouseOverflowRule.Select(x => x.UUID), Is.EqualTo(new[] { "a-wor", "z-wor" }));
            Assert.That(deserialized.Faction.Select(x => x.UUID), Is.EqualTo(new[] { "a-fac", "z-fac" }));
            Assert.That(deserialized.ExternalCharacter.Select(x => x.UUID), Is.EqualTo(new[] { "a-ec", "z-ec" }));
            Assert.That(deserialized.Asteroid.Select(x => x.UUID), Is.EqualTo(new[] { "a-ast", "z-ast" }));

            // Verify scalar properties preserved
            Assert.That(deserialized.DataVersion, Is.EqualTo(1));
            Assert.That(deserialized.CurrentPlayerUUID, Is.EqualTo("player-1"));
        }

        [Test]
        public void BaselineRoot_SortAndSerialize_ProducesSortedJson()
        {
            // Build a BaselineRoot with entities in reverse order
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

            // Sort, serialize, deserialize
            var sorted = SerializationSorter.SortBaselineRoot(root);
            var json = JsonConvert.SerializeObject(sorted, JsonSettings.SerializerSettings);
            var deserialized = JsonConvert.DeserializeObject<BaselineRoot>(json);

            // Verify all arrays are sorted by their primary key
            Assert.That(deserialized.BlueprintType.Select(x => x.Id), Is.EqualTo(new[] { "a-bt", "z-bt" }));
            Assert.That(deserialized.Blueprint.Select(x => x.UUID), Is.EqualTo(new[] { "a-bp", "z-bp" }));
            Assert.That(deserialized.ShipClass.Select(x => x.Id), Is.EqualTo(new[] { 1, 9 }));
            Assert.That(deserialized.TechLevel.Select(x => x.Name), Is.EqualTo(new[] { "a-tl", "z-tl" }));
            Assert.That(deserialized.Commodity.Select(x => x.ID), Is.EqualTo(new[] { "a-com", "z-com" }));
            Assert.That(deserialized.RefiningRecipe.Select(x => x.OutputResource), Is.EqualTo(new[] { "a-rr", "z-rr" }));
            Assert.That(deserialized.ResearchTime.Select(x => x.Evolution), Is.EqualTo(new[] { 1, 5 }));

            // Verify scalar properties preserved
            Assert.That(deserialized.DataVersion, Is.EqualTo(2));
            Assert.That(deserialized.GameConstants, Is.Not.Null);
        }

        [Test]
        public void PlayerRoot_SortPreservesOriginalArrayOrder()
        {
            var colZ = new Colony { UUID = "z-col", Structures = new List<ColonyStructure>(), Commodities = new List<CommodityRequested>() };
            var colA = new Colony { UUID = "a-col", Structures = new List<ColonyStructure>(), Commodities = new List<CommodityRequested>() };
            var bpZ = new Bp { UUID = "z-bp" };
            var bpA = new Bp { UUID = "a-bp" };
            var svZ = new Survey("s") { UUID = "z-sv" };
            var svA = new Survey("s") { UUID = "a-sv" };

            var root = new PlayerRoot
            {
                DataVersion = 1,
                CurrentPlayerUUID = "player-1",
                Colony = new[] { colZ, colA },
                Blueprint = new[] { bpZ, bpA },
                Survey = new[] { svZ, svA }
            };

            // Snapshot original order
            var originalColonyOrder = root.Colony.Select(c => c.UUID).ToArray();
            var originalBlueprintOrder = root.Blueprint.Select(b => b.UUID).ToArray();
            var originalSurveyOrder = root.Survey.Select(s => s.UUID).ToArray();

            // Sort
            var sorted = SerializationSorter.SortPlayerRoot(root);

            // Verify original root's arrays are unchanged
            Assert.That(root.Colony.Select(c => c.UUID).ToArray(), Is.EqualTo(originalColonyOrder));
            Assert.That(root.Blueprint.Select(b => b.UUID).ToArray(), Is.EqualTo(originalBlueprintOrder));
            Assert.That(root.Survey.Select(s => s.UUID).ToArray(), Is.EqualTo(originalSurveyOrder));

            // Verify sorted arrays are different instances
            Assert.That(sorted.Colony, Is.Not.SameAs(root.Colony));
            Assert.That(sorted.Blueprint, Is.Not.SameAs(root.Blueprint));
            Assert.That(sorted.Survey, Is.Not.SameAs(root.Survey));

            // Verify sorted arrays are actually sorted
            Assert.That(sorted.Colony.Select(c => c.UUID), Is.EqualTo(new[] { "a-col", "z-col" }));
            Assert.That(sorted.Blueprint.Select(b => b.UUID), Is.EqualTo(new[] { "a-bp", "z-bp" }));
            Assert.That(sorted.Survey.Select(s => s.UUID), Is.EqualTo(new[] { "a-sv", "z-sv" }));
        }

        [Test]
        public void BaselineRoot_SortPreservesOriginalArrayOrder()
        {
            var btZ = new BlueprintType { Id = "z-bt" };
            var btA = new BlueprintType { Id = "a-bt" };
            var bpZ = new Bp { UUID = "z-bp" };
            var bpA = new Bp { UUID = "a-bp" };
            var scHeavy = new ShipClass { Id = 9, Name = "Heavy" };
            var scLight = new ShipClass { Id = 1, Name = "Light" };

            var root = new BaselineRoot
            {
                DataVersion = 2,
                GameConstants = new BaselineGameConstants(),
                BlueprintType = new[] { btZ, btA },
                Blueprint = new[] { bpZ, bpA },
                ShipClass = new[] { scHeavy, scLight },
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

            // Snapshot original order
            var originalBlueprintTypeOrder = root.BlueprintType.Select(x => x.Id).ToArray();
            var originalBlueprintOrder = root.Blueprint.Select(x => x.UUID).ToArray();
            var originalShipClassOrder = root.ShipClass.Select(x => x.Id).ToArray();

            // Sort
            var sorted = SerializationSorter.SortBaselineRoot(root);

            // Verify original root's arrays are unchanged
            Assert.That(root.BlueprintType.Select(x => x.Id).ToArray(), Is.EqualTo(originalBlueprintTypeOrder));
            Assert.That(root.Blueprint.Select(x => x.UUID).ToArray(), Is.EqualTo(originalBlueprintOrder));
            Assert.That(root.ShipClass.Select(x => x.Id).ToArray(), Is.EqualTo(originalShipClassOrder));

            // Verify sorted arrays are different instances
            Assert.That(sorted.BlueprintType, Is.Not.SameAs(root.BlueprintType));
            Assert.That(sorted.Blueprint, Is.Not.SameAs(root.Blueprint));
            Assert.That(sorted.ShipClass, Is.Not.SameAs(root.ShipClass));

            // Verify sorted arrays are actually sorted
            Assert.That(sorted.BlueprintType.Select(x => x.Id), Is.EqualTo(new[] { "a-bt", "z-bt" }));
            Assert.That(sorted.Blueprint.Select(x => x.UUID), Is.EqualTo(new[] { "a-bp", "z-bp" }));
            Assert.That(sorted.ShipClass.Select(x => x.Id), Is.EqualTo(new[] { 1, 9 }));
        }
    }
}