using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class DeliveryGenerationServiceTests
    {
        private PlayerContext _playerContext;

        [SetUp]
        public void SetUp()
        {
            EmpireContext.Reset();
            PlayerContext.Reset();
            TestHelper.SetAllFilePaths();
            _playerContext = PlayerContext.GetInstance();
            EmpireContext.PlayerContext = _playerContext;
        }

        [TearDown]
        public void TearDown()
        {
            PlayerContext.Reset();
            EmpireContext.Reset();
        }

        #region Helpers

        private DeliveryRoute CreateRoute(string ownerUUID, params string[] colonyUUIDs)
        {
            var route = new DeliveryRoute
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "Test Route",
                OwnerUUID = ownerUUID
            };

            for (int i = 0; i < colonyUUIDs.Length; i++)
            {
                route.Stops.Add(new RouteStop
                {
                    ColonyUUID = colonyUUIDs[i],
                    DestinationUUID = colonyUUIDs[i],
                    DestinationType = DestinationType.Colony,
                    Sequence = i + 1
                });
            }

            return route;
        }

        private BuildPlan CreateBuildPlan(string name, params BuildItem[] items)
        {
            var plan = new BuildPlan
            {
                UUID = Guid.NewGuid().ToString(),
                Name = name,
                OwnerUUID = "player-1"
            };

            plan.Items.AddRange(items);
            return plan;
        }

        private BuildItem CreateManufactoryItem(string bpUUID, int qty,
            string locationUUID, string notes = "")
        {
            return new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Manufactory,
                BlueprintUUID = bpUUID,
                ItemName = "Test Item",
                Quantity = qty,
                BuildLocationType = DestinationType.Colony,
                BuildLocationUUID = locationUUID,
                Notes = notes
            };
        }

        #endregion

        #region GenerateDeliveryPlan - Null Arguments

        [Test]
        public void GenerateDeliveryPlan_NullBuildPlan_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                DeliveryGenerationService.GenerateDeliveryPlan(
                    null, new DeliveryRoute(),
                    new Dictionary<string, Dictionary<string, int>>(),
                    id => null, _playerContext));
        }

        [Test]
        public void GenerateDeliveryPlan_NullRoute_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                DeliveryGenerationService.GenerateDeliveryPlan(
                    new BuildPlan(), null,
                    new Dictionary<string, Dictionary<string, int>>(),
                    id => null, _playerContext));
        }

        [Test]
        public void GenerateDeliveryPlan_NullShortfalls_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                DeliveryGenerationService.GenerateDeliveryPlan(
                    new BuildPlan(), new DeliveryRoute(), null,
                    id => null, _playerContext));
        }

        #endregion

        #region GenerateDeliveryPlan - Basic Behavior

        [Test]
        public void GenerateDeliveryPlan_CreatesNewPlan_WithCorrectName()
        {
            var route = CreateRoute("player-1", "colony-1");
            var item = CreateManufactoryItem("bp-1", 1, "colony-1");
            var buildPlan = CreateBuildPlan("Reactor Build", item);

            var shortfalls = new Dictionary<string, Dictionary<string, int>>
            {
                { item.UUID, new Dictionary<string, int> { { "Trans-Metals", 100 } } }
            };

            var result = DeliveryGenerationService.GenerateDeliveryPlan(
                buildPlan, route, shortfalls, id => null, _playerContext);

            Assert.That(result.Name, Is.EqualTo("Build: Reactor Build"));
            Assert.That(result.RouteUUID, Is.EqualTo(route.UUID));
            Assert.That(buildPlan.DeliveryPlanUUID, Is.EqualTo(result.UUID));
        }

        [Test]
        public void GenerateDeliveryPlan_CreatesDropOffStops()
        {
            var route = CreateRoute("player-1", "colony-1");
            var item = CreateManufactoryItem("bp-1", 1, "colony-1");
            var buildPlan = CreateBuildPlan("Test", item);

            var shortfalls = new Dictionary<string, Dictionary<string, int>>
            {
                { item.UUID, new Dictionary<string, int>
                    {
                        { "Trans-Metals", 100 },
                        { "Metallics", 50 }
                    }
                }
            };

            var result = DeliveryGenerationService.GenerateDeliveryPlan(
                buildPlan, route, shortfalls, id => null, _playerContext);

            Assert.That(result.Stops.Count, Is.EqualTo(1));
            Assert.That(result.Stops[0].DropOff.Count, Is.EqualTo(2));
        }

        [Test]
        public void GenerateDeliveryPlan_UpdatesItemStatusToDelivering()
        {
            var route = CreateRoute("player-1", "colony-1");
            var item = CreateManufactoryItem("bp-1", 1, "colony-1");
            var buildPlan = CreateBuildPlan("Test", item);

            var shortfalls = new Dictionary<string, Dictionary<string, int>>
            {
                { item.UUID, new Dictionary<string, int> { { "Trans-Metals", 100 } } }
            };

            DeliveryGenerationService.GenerateDeliveryPlan(
                buildPlan, route, shortfalls, id => null, _playerContext);

            Assert.That(item.Status, Is.EqualTo(BuildItemStatus.Delivering));
        }

        [Test]
        public void GenerateDeliveryPlan_EmptyShortfalls_CreatesEmptyPlan()
        {
            var route = CreateRoute("player-1", "colony-1");
            var buildPlan = CreateBuildPlan("Test");

            var result = DeliveryGenerationService.GenerateDeliveryPlan(
                buildPlan, route,
                new Dictionary<string, Dictionary<string, int>>(),
                id => null, _playerContext);

            Assert.That(result.Stops.Count, Is.EqualTo(0));
        }

        [Test]
        public void GenerateDeliveryPlan_UpdatesExistingPlan()
        {
            var route = CreateRoute("player-1", "colony-1");
            var item = CreateManufactoryItem("bp-1", 1, "colony-1");
            var buildPlan = CreateBuildPlan("Test", item);

            var shortfalls = new Dictionary<string, Dictionary<string, int>>
            {
                { item.UUID, new Dictionary<string, int> { { "Trans-Metals", 100 } } }
            };

            // First call creates the plan
            var plan1 = DeliveryGenerationService.GenerateDeliveryPlan(
                buildPlan, route, shortfalls, id => null, _playerContext);
            string planUUID = plan1.UUID;

            // Second call should update the same plan
            var plan2 = DeliveryGenerationService.GenerateDeliveryPlan(
                buildPlan, route, shortfalls, id => null, _playerContext);

            Assert.That(plan2.UUID, Is.EqualTo(planUUID));
        }

        [Test]
        public void GenerateDeliveryPlan_MultipleLocations_CreatesMultipleStops()
        {
            var route = CreateRoute("player-1", "colony-1", "colony-2");
            var item1 = CreateManufactoryItem("bp-1", 1, "colony-1");
            var item2 = CreateManufactoryItem("bp-2", 1, "colony-2");
            var buildPlan = CreateBuildPlan("Test", item1, item2);

            var shortfalls = new Dictionary<string, Dictionary<string, int>>
            {
                { item1.UUID, new Dictionary<string, int> { { "Trans-Metals", 100 } } },
                { item2.UUID, new Dictionary<string, int> { { "Metallics", 50 } } }
            };

            var result = DeliveryGenerationService.GenerateDeliveryPlan(
                buildPlan, route, shortfalls, id => null, _playerContext);

            Assert.That(result.Stops.Count, Is.EqualTo(2));
        }

        #endregion

        #region GenerateConsolidatedDeliveryPlan

        [Test]
        public void GenerateConsolidatedDeliveryPlan_NullPlans_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                DeliveryGenerationService.GenerateConsolidatedDeliveryPlan(
                    null, new DeliveryRoute(), p => null,
                    id => null, _playerContext, "Test"));
        }

        [Test]
        public void GenerateConsolidatedDeliveryPlan_MergesAcrossPlans()
        {
            var route = CreateRoute("player-1", "colony-1");
            var item1 = CreateManufactoryItem("bp-1", 1, "colony-1");
            var item2 = CreateManufactoryItem("bp-2", 1, "colony-1");
            var plan1 = CreateBuildPlan("Plan A", item1);
            var plan2 = CreateBuildPlan("Plan B", item2);

            Func<BuildPlan, Dictionary<string, Dictionary<string, int>>> provider = p =>
            {
                if (p == plan1)
                    return new Dictionary<string, Dictionary<string, int>>
                    {
                        { item1.UUID, new Dictionary<string, int> { { "Trans-Metals", 500 } } }
                    };

                if (p == plan2)
                    return new Dictionary<string, Dictionary<string, int>>
                    {
                        { item2.UUID, new Dictionary<string, int> { { "Trans-Metals", 300 } } }
                    };

                return new Dictionary<string, Dictionary<string, int>>();
            };

            var result = DeliveryGenerationService.GenerateConsolidatedDeliveryPlan(
                new[] { plan1, plan2 }, route, provider, id => null, _playerContext,
                "Consolidated");

            Assert.That(result.Name, Is.EqualTo("Consolidated"));
            Assert.That(result.Stops.Count, Is.EqualTo(1));
            // Should merge: 500 + 300 = 800 Trans-Metals
            var dropOff = result.Stops[0].DropOff.First(d => d.Name == "Trans-Metals");
            Assert.That(dropOff.Quantity, Is.EqualTo(800));
        }

        [Test]
        public void GenerateConsolidatedDeliveryPlan_EmptyPlans_CreatesEmptyPlan()
        {
            var route = CreateRoute("player-1", "colony-1");

            var result = DeliveryGenerationService.GenerateConsolidatedDeliveryPlan(
                new BuildPlan[0], route,
                p => new Dictionary<string, Dictionary<string, int>>(),
                id => null, _playerContext, "Empty");

            Assert.That(result.Stops.Count, Is.EqualTo(0));
        }

        #endregion

        #region GenerateFlatpackDeliveryPlan

        [Test]
        public void GenerateFlatpackDeliveryPlan_NullPlans_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                DeliveryGenerationService.GenerateFlatpackDeliveryPlan(
                    null, new DeliveryRoute(), id => null, id => null,
                    _playerContext, "Test"));
        }

        [Test]
        public void GenerateFlatpackDeliveryPlan_CollectsCompletedItems()
        {
            var route = CreateRoute("player-1", "colony-dest");
            var completedItem = CreateManufactoryItem("bp-1", 1, "colony-mfg",
                "For colony: Alpha (colony-dest)");
            completedItem.Status = BuildItemStatus.Completed;
            completedItem.ItemName = "Reactor Flatpack";

            var stagedItem = CreateManufactoryItem("bp-2", 1, "colony-mfg",
                "For colony: Alpha (colony-dest)");
            stagedItem.Status = BuildItemStatus.Staged;

            var plan = CreateBuildPlan("Test", completedItem, stagedItem);

            var result = DeliveryGenerationService.GenerateFlatpackDeliveryPlan(
                new[] { plan }, route, id => null, id => null,
                _playerContext, "Flatpack Delivery");

            Assert.That(result.Stops.Count, Is.EqualTo(1));
            Assert.That(result.Stops[0].DropOff.Count, Is.EqualTo(1));
            Assert.That(result.Stops[0].DropOff[0].Name, Is.EqualTo("Reactor Flatpack"));
        }

        [Test]
        public void GenerateFlatpackDeliveryPlan_NoCompletedItems_EmptyPlan()
        {
            var route = CreateRoute("player-1", "colony-dest");
            var item = CreateManufactoryItem("bp-1", 1, "colony-mfg",
                "For colony: Alpha (colony-dest)");
            item.Status = BuildItemStatus.InProgress;

            var plan = CreateBuildPlan("Test", item);

            var result = DeliveryGenerationService.GenerateFlatpackDeliveryPlan(
                new[] { plan }, route, id => null, id => null,
                _playerContext, "Flatpack");

            Assert.That(result.Stops.Count, Is.EqualTo(0));
        }

        [Test]
        public void GenerateFlatpackDeliveryPlan_GroupsByDestination()
        {
            var route = CreateRoute("player-1", "colony-a", "colony-b");

            var item1 = CreateManufactoryItem("bp-1", 1, "colony-mfg",
                "For colony: Alpha (colony-a)");
            item1.Status = BuildItemStatus.Completed;
            item1.ItemName = "Flatpack A";

            var item2 = CreateManufactoryItem("bp-2", 1, "colony-mfg",
                "For colony: Beta (colony-b)");
            item2.Status = BuildItemStatus.Completed;
            item2.ItemName = "Flatpack B";

            var plan = CreateBuildPlan("Test", item1, item2);

            var result = DeliveryGenerationService.GenerateFlatpackDeliveryPlan(
                new[] { plan }, route, id => null, id => null,
                _playerContext, "Flatpack");

            Assert.That(result.Stops.Count, Is.EqualTo(2));
        }

        [Test]
        public void GenerateFlatpackDeliveryPlan_NoNotesUUID_SkipsItem()
        {
            var route = CreateRoute("player-1", "colony-dest");
            var item = CreateManufactoryItem("bp-1", 1, "colony-mfg", "No UUID here");
            item.Status = BuildItemStatus.Completed;

            var plan = CreateBuildPlan("Test", item);

            var result = DeliveryGenerationService.GenerateFlatpackDeliveryPlan(
                new[] { plan }, route, id => null, id => null,
                _playerContext, "Flatpack");

            Assert.That(result.Stops.Count, Is.EqualTo(0));
        }

        #endregion
    }
}
