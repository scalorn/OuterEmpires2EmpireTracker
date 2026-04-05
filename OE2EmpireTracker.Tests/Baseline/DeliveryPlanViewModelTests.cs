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
    }
}
