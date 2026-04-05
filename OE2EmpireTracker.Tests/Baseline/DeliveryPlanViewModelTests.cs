using NUnit.Framework;
using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Data;
using OE2EmpireTracker.ViewModels;
using System.IO;
using System.Linq;

namespace OE2EmpireTracker.Tests.Baseline
{
    [TestFixture]
    public class DeliveryPlanViewModelTests
    {
        private PlayerContext playerContext;

        [SetUp]
        public void SetUp()
        {
            PlayerContext.Reset();
            EmpireContext.FilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"..\..\..\..\OE2EmpireTracker\BaselineData.json");
            PlayerContext.FilePath = "nonexistent_player_data.json";
            playerContext = PlayerContext.getInstance();
        }

        private DeliveryPlanViewModel CreateViewModel()
        {
            var plan = new DeliveryPlan { UUID = "plan-1", Name = "Test Plan", RouteUUID = "r1" };
            return new DeliveryPlanViewModel(plan, playerContext);
        }

        // -----------------------------------------------------------------------
        // GetOrCreateStop
        // -----------------------------------------------------------------------

        [Test]
        public void GetOrCreateStop_NewColony_CreatesStop()
        {
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            Assert.IsNotNull(stop);
            Assert.AreEqual("c1", stop.ColonyUUID);
            Assert.AreEqual(0, stop.Sequence);
            Assert.AreEqual(1, vm.Data.Stops.Count);
        }

        [Test]
        public void GetOrCreateStop_ExistingColony_ReturnsSameStop()
        {
            var vm = CreateViewModel();
            var stop1 = vm.GetOrCreateStop("c1", 0);
            var stop2 = vm.GetOrCreateStop("c1", 0);
            Assert.AreSame(stop1, stop2);
            Assert.AreEqual(1, vm.Data.Stops.Count);
        }

        [Test]
        public void GetOrCreateStop_DifferentColonies_CreatesSeparateStops()
        {
            var vm = CreateViewModel();
            vm.GetOrCreateStop("c1", 0);
            vm.GetOrCreateStop("c2", 1);
            Assert.AreEqual(2, vm.Data.Stops.Count);
        }

        // -----------------------------------------------------------------------
        // AddDropOffItem / AddPickUpItem
        // -----------------------------------------------------------------------

        [Test]
        public void AddDropOffItem_AddsToStopDropOffList()
        {
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddDropOffItem(stop, ItemType.ItemTypeEnum.Commodity, "Steel", "Steel Plates", 50);
            Assert.AreEqual(1, stop.DropOff.Count);
            Assert.AreEqual("Steel", stop.DropOff[0].BaseItemTypeID);
            Assert.AreEqual("Steel Plates", stop.DropOff[0].Name);
            Assert.AreEqual(50, stop.DropOff[0].Quantity);
            Assert.AreEqual(ItemType.ItemTypeEnum.Commodity, stop.DropOff[0].ItemType);
        }

        [Test]
        public void AddDropOffItem_WithPurity_SetsPurity()
        {
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddDropOffItem(stop, ItemType.ItemTypeEnum.Resource, "Iron", "Iron", 100, "High");
            Assert.AreEqual("High", stop.DropOff[0].ResourcePurity);
        }

        [Test]
        public void AddDropOffItem_NoPurity_EmptyString()
        {
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddDropOffItem(stop, ItemType.ItemTypeEnum.Commodity, "Steel", "Steel", 10);
            Assert.AreEqual(string.Empty, stop.DropOff[0].ResourcePurity);
        }

        [Test]
        public void AddPickUpItem_AddsToStopPickUpList()
        {
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddPickUpItem(stop, ItemType.ItemTypeEnum.Resource, "Iron", "Iron", 200, "Low");
            Assert.AreEqual(1, stop.PickUp.Count);
            Assert.AreEqual("Iron", stop.PickUp[0].BaseItemTypeID);
            Assert.AreEqual(200, stop.PickUp[0].Quantity);
            Assert.AreEqual("Low", stop.PickUp[0].ResourcePurity);
        }

        [Test]
        public void AddMultipleItems_AllAdded()
        {
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddDropOffItem(stop, ItemType.ItemTypeEnum.Commodity, "A", "A", 1);
            vm.AddDropOffItem(stop, ItemType.ItemTypeEnum.Commodity, "B", "B", 2);
            vm.AddPickUpItem(stop, ItemType.ItemTypeEnum.Resource, "C", "C", 3);
            Assert.AreEqual(2, stop.DropOff.Count);
            Assert.AreEqual(1, stop.PickUp.Count);
        }

        // -----------------------------------------------------------------------
        // RemoveDropOffItems / RemovePickUpItems
        // -----------------------------------------------------------------------

        [Test]
        public void RemoveDropOffItems_SingleIndex_Removes()
        {
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddDropOffItem(stop, ItemType.ItemTypeEnum.Commodity, "A", "A", 1);
            vm.AddDropOffItem(stop, ItemType.ItemTypeEnum.Commodity, "B", "B", 2);
            vm.RemoveDropOffItems(stop, new[] { 0 });
            Assert.AreEqual(1, stop.DropOff.Count);
            Assert.AreEqual("B", stop.DropOff[0].BaseItemTypeID);
        }

        [Test]
        public void RemoveDropOffItems_MultipleIndices_RemovesCorrectly()
        {
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddDropOffItem(stop, ItemType.ItemTypeEnum.Commodity, "A", "A", 1);
            vm.AddDropOffItem(stop, ItemType.ItemTypeEnum.Commodity, "B", "B", 2);
            vm.AddDropOffItem(stop, ItemType.ItemTypeEnum.Commodity, "C", "C", 3);
            vm.RemoveDropOffItems(stop, new[] { 0, 2 });
            Assert.AreEqual(1, stop.DropOff.Count);
            Assert.AreEqual("B", stop.DropOff[0].BaseItemTypeID);
        }

        [Test]
        public void RemoveDropOffItems_InvalidIndex_Ignored()
        {
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddDropOffItem(stop, ItemType.ItemTypeEnum.Commodity, "A", "A", 1);
            vm.RemoveDropOffItems(stop, new[] { 5 });
            Assert.AreEqual(1, stop.DropOff.Count);
        }

        [Test]
        public void RemovePickUpItems_SingleIndex_Removes()
        {
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddPickUpItem(stop, ItemType.ItemTypeEnum.Resource, "A", "A", 1);
            vm.AddPickUpItem(stop, ItemType.ItemTypeEnum.Resource, "B", "B", 2);
            vm.RemovePickUpItems(stop, new[] { 1 });
            Assert.AreEqual(1, stop.PickUp.Count);
            Assert.AreEqual("A", stop.PickUp[0].BaseItemTypeID);
        }

        [Test]
        public void RemovePickUpItems_NegativeIndex_Ignored()
        {
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddPickUpItem(stop, ItemType.ItemTypeEnum.Resource, "A", "A", 1);
            vm.RemovePickUpItems(stop, new[] { -1 });
            Assert.AreEqual(1, stop.PickUp.Count);
        }

        // -----------------------------------------------------------------------
        // Constructor Validation
        // -----------------------------------------------------------------------

        [Test]
        public void Constructor_NullPlan_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new DeliveryPlanViewModel(null, playerContext));
        }

        [Test]
        public void Constructor_NullPlayerContext_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new DeliveryPlanViewModel(new DeliveryPlan(), null));
        }

        [Test]
        public void UUID_ReturnsPlansUUID()
        {
            var vm = CreateViewModel();
            Assert.AreEqual("plan-1", vm.UUID);
        }

        // -----------------------------------------------------------------------
        // AutoFillCommodities
        // -----------------------------------------------------------------------

        private Colony CreateColonyWithCommodities(string uuid, params CommodityRequested[] commodities)
        {
            var colony = new Colony { UUID = uuid };
            colony.Commodities.AddRange(commodities);
            return colony;
        }

        [Test]
        public void AutoFillCommodities_UnfulfilledCommodity_AddsDropOffItem()
        {
            var vm = CreateViewModel();
            var colony = CreateColonyWithCommodities("c1",
                new CommodityRequested { Name = "Steel Plates", Requested = 50, Delivered = 0, Fulfilled = false });
            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillCommodities(stops, uuid => uuid == "c1" ? colony : null);

            Assert.AreEqual(1, added);
            var stop = vm.Data.Stops.First(s => s.ColonyUUID == "c1");
            Assert.AreEqual(1, stop.DropOff.Count);
            Assert.AreEqual("Steel Plates", stop.DropOff[0].Name);
            Assert.AreEqual("Steel Plates", stop.DropOff[0].BaseItemTypeID);
            Assert.AreEqual(50, stop.DropOff[0].Quantity);
            Assert.AreEqual(ItemType.ItemTypeEnum.Commodity, stop.DropOff[0].ItemType);
        }

        [Test]
        public void AutoFillCommodities_PartiallyDelivered_AddsShortfall()
        {
            var vm = CreateViewModel();
            var colony = CreateColonyWithCommodities("c1",
                new CommodityRequested { Name = "Steel Plates", Requested = 100, Delivered = 30, Fulfilled = false });
            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            vm.AutoFillCommodities(stops, uuid => uuid == "c1" ? colony : null);

            var stop = vm.Data.Stops.First(s => s.ColonyUUID == "c1");
            Assert.AreEqual(70, stop.DropOff[0].Quantity);
        }

        [Test]
        public void AutoFillCommodities_FulfilledCommodity_Skipped()
        {
            var vm = CreateViewModel();
            var colony = CreateColonyWithCommodities("c1",
                new CommodityRequested { Name = "Steel Plates", Requested = 50, Delivered = 50, Fulfilled = true });
            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillCommodities(stops, uuid => uuid == "c1" ? colony : null);

            Assert.AreEqual(0, added);
        }

        [Test]
        public void AutoFillCommodities_ZeroShortfall_Skipped()
        {
            var vm = CreateViewModel();
            var colony = CreateColonyWithCommodities("c1",
                new CommodityRequested { Name = "Steel Plates", Requested = 50, Delivered = 50, Fulfilled = false });
            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillCommodities(stops, uuid => uuid == "c1" ? colony : null);

            Assert.AreEqual(0, added);
        }

        [Test]
        public void AutoFillCommodities_MissingColony_SkipsStop()
        {
            var vm = CreateViewModel();
            var colony = CreateColonyWithCommodities("c2",
                new CommodityRequested { Name = "Steel Plates", Requested = 50, Delivered = 0, Fulfilled = false });
            var stops = new[]
            {
                new RouteStop { ColonyUUID = "c1", Sequence = 0 },
                new RouteStop { ColonyUUID = "c2", Sequence = 1 }
            };

            int added = vm.AutoFillCommodities(stops, uuid => uuid == "c2" ? colony : null);

            Assert.AreEqual(1, added);
            Assert.IsFalse(vm.Data.Stops.Any(s => s.ColonyUUID == "c1"));
        }

        [Test]
        public void AutoFillCommodities_PreservesExistingDropOffItems()
        {
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddDropOffItem(stop, ItemType.ItemTypeEnum.Resource, "Iron", "Iron", 100, "High");

            var colony = CreateColonyWithCommodities("c1",
                new CommodityRequested { Name = "Steel Plates", Requested = 50, Delivered = 0, Fulfilled = false });
            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            vm.AutoFillCommodities(stops, uuid => uuid == "c1" ? colony : null);

            Assert.AreEqual(2, stop.DropOff.Count);
            Assert.AreEqual("Iron", stop.DropOff[0].BaseItemTypeID);
            Assert.AreEqual("High", stop.DropOff[0].ResourcePurity);
            Assert.AreEqual("Steel Plates", stop.DropOff[1].Name);
        }

        [Test]
        public void AutoFillCommodities_DoesNotModifyPickUpList()
        {
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddPickUpItem(stop, ItemType.ItemTypeEnum.Resource, "Iron", "Iron", 200);

            var colony = CreateColonyWithCommodities("c1",
                new CommodityRequested { Name = "Steel Plates", Requested = 50, Delivered = 0, Fulfilled = false });
            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            vm.AutoFillCommodities(stops, uuid => uuid == "c1" ? colony : null);

            Assert.AreEqual(1, stop.PickUp.Count);
            Assert.AreEqual("Iron", stop.PickUp[0].BaseItemTypeID);
        }

        [Test]
        public void AutoFillCommodities_EmptyCommoditiesList_AddsNothing()
        {
            var vm = CreateViewModel();
            var colony = new Colony { UUID = "c1" };
            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillCommodities(stops, uuid => uuid == "c1" ? colony : null);

            Assert.AreEqual(0, added);
        }

        [Test]
        public void AutoFillCommodities_MultipleStops_MixedCommodities()
        {
            var vm = CreateViewModel();
            var colony1 = CreateColonyWithCommodities("c1",
                new CommodityRequested { Name = "Steel Plates", Requested = 50, Delivered = 0, Fulfilled = false },
                new CommodityRequested { Name = "Copper Wire", Requested = 30, Delivered = 30, Fulfilled = true });
            var colony2 = CreateColonyWithCommodities("c2",
                new CommodityRequested { Name = "Glass Panels", Requested = 20, Delivered = 5, Fulfilled = false });
            var stops = new[]
            {
                new RouteStop { ColonyUUID = "c1", Sequence = 0 },
                new RouteStop { ColonyUUID = "c2", Sequence = 1 }
            };

            int added = vm.AutoFillCommodities(stops, uuid =>
            {
                if (uuid == "c1") return colony1;
                if (uuid == "c2") return colony2;
                return null;
            });

            Assert.AreEqual(2, added);
            var stop1 = vm.Data.Stops.First(s => s.ColonyUUID == "c1");
            Assert.AreEqual(1, stop1.DropOff.Count);
            Assert.AreEqual("Steel Plates", stop1.DropOff[0].Name);
            Assert.AreEqual(50, stop1.DropOff[0].Quantity);

            var stop2 = vm.Data.Stops.First(s => s.ColonyUUID == "c2");
            Assert.AreEqual(1, stop2.DropOff.Count);
            Assert.AreEqual("Glass Panels", stop2.DropOff[0].Name);
            Assert.AreEqual(15, stop2.DropOff[0].Quantity);
        }
    }
}
