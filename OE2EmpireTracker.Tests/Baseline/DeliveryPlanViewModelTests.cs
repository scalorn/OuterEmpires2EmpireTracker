using NUnit.Framework;
using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;
using System.Collections.Generic;
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
            // Set file paths before any initialization
            EmpireContext.FilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"..\..\..\..\OE2EmpireTracker\BaselineData.json");
            PlayerContext.FilePath = "nonexistent_player_data.json";
            // Ensure EmpireContext singleton exists (ColonyStatusCalculator depends on it)
            // Must be done before PlayerContext.Reset() to avoid stale references
            var ec = EmpireContext.getInstance();
            PlayerContext.Reset();
            playerContext = PlayerContext.getInstance();
            // Update EmpireContext's PlayerContext reference
            EmpireContext.PlayerContext = playerContext;
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
            Assert.That(stop, Is.Not.Null);
            Assert.That(stop.ColonyUUID, Is.EqualTo("c1"));
            Assert.That(stop.Sequence, Is.EqualTo(0));
            Assert.That(vm.Data.Stops.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetOrCreateStop_ExistingColony_ReturnsSameStop()
        {
            var vm = CreateViewModel();
            var stop1 = vm.GetOrCreateStop("c1", 0);
            var stop2 = vm.GetOrCreateStop("c1", 0);
            Assert.That(stop2, Is.SameAs(stop1));
            Assert.That(vm.Data.Stops.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetOrCreateStop_DifferentColonies_CreatesSeparateStops()
        {
            var vm = CreateViewModel();
            vm.GetOrCreateStop("c1", 0);
            vm.GetOrCreateStop("c2", 1);
            Assert.That(vm.Data.Stops.Count, Is.EqualTo(2));
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
            Assert.That(stop.DropOff.Count, Is.EqualTo(1));
            Assert.That(stop.DropOff[0].BaseItemTypeID, Is.EqualTo("Steel"));
            Assert.That(stop.DropOff[0].Name, Is.EqualTo("Steel Plates"));
            Assert.That(stop.DropOff[0].Quantity, Is.EqualTo(50));
            Assert.That(stop.DropOff[0].ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Commodity));
        }

        [Test]
        public void AddDropOffItem_WithPurity_SetsPurity()
        {
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddDropOffItem(stop, ItemType.ItemTypeEnum.Resource, "Iron", "Iron", 100, "High");
            Assert.That(stop.DropOff[0].ResourcePurity, Is.EqualTo("High"));
        }

        [Test]
        public void AddDropOffItem_NoPurity_EmptyString()
        {
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddDropOffItem(stop, ItemType.ItemTypeEnum.Commodity, "Steel", "Steel", 10);
            Assert.That(stop.DropOff[0].ResourcePurity, Is.EqualTo(string.Empty));
        }

        [Test]
        public void AddPickUpItem_AddsToStopPickUpList()
        {
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddPickUpItem(stop, ItemType.ItemTypeEnum.Resource, "Iron", "Iron", 200, "Low");
            Assert.That(stop.PickUp.Count, Is.EqualTo(1));
            Assert.That(stop.PickUp[0].BaseItemTypeID, Is.EqualTo("Iron"));
            Assert.That(stop.PickUp[0].Quantity, Is.EqualTo(200));
            Assert.That(stop.PickUp[0].ResourcePurity, Is.EqualTo("Low"));
        }

        [Test]
        public void AddMultipleItems_AllAdded()
        {
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddDropOffItem(stop, ItemType.ItemTypeEnum.Commodity, "A", "A", 1);
            vm.AddDropOffItem(stop, ItemType.ItemTypeEnum.Commodity, "B", "B", 2);
            vm.AddPickUpItem(stop, ItemType.ItemTypeEnum.Resource, "C", "C", 3);
            Assert.That(stop.DropOff.Count, Is.EqualTo(2));
            Assert.That(stop.PickUp.Count, Is.EqualTo(1));
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
            Assert.That(stop.DropOff.Count, Is.EqualTo(1));
            Assert.That(stop.DropOff[0].BaseItemTypeID, Is.EqualTo("B"));
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
            Assert.That(stop.DropOff.Count, Is.EqualTo(1));
            Assert.That(stop.DropOff[0].BaseItemTypeID, Is.EqualTo("B"));
        }

        [Test]
        public void RemoveDropOffItems_InvalidIndex_Ignored()
        {
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddDropOffItem(stop, ItemType.ItemTypeEnum.Commodity, "A", "A", 1);
            vm.RemoveDropOffItems(stop, new[] { 5 });
            Assert.That(stop.DropOff.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemovePickUpItems_SingleIndex_Removes()
        {
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddPickUpItem(stop, ItemType.ItemTypeEnum.Resource, "A", "A", 1);
            vm.AddPickUpItem(stop, ItemType.ItemTypeEnum.Resource, "B", "B", 2);
            vm.RemovePickUpItems(stop, new[] { 1 });
            Assert.That(stop.PickUp.Count, Is.EqualTo(1));
            Assert.That(stop.PickUp[0].BaseItemTypeID, Is.EqualTo("A"));
        }

        [Test]
        public void RemovePickUpItems_NegativeIndex_Ignored()
        {
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddPickUpItem(stop, ItemType.ItemTypeEnum.Resource, "A", "A", 1);
            vm.RemovePickUpItems(stop, new[] { -1 });
            Assert.That(stop.PickUp.Count, Is.EqualTo(1));
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
            Assert.That(vm.UUID, Is.EqualTo("plan-1"));
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

            Assert.That(added, Is.EqualTo(1));
            var stop = vm.Data.Stops.First(s => s.ColonyUUID == "c1");
            Assert.That(stop.DropOff.Count, Is.EqualTo(1));
            Assert.That(stop.DropOff[0].Name, Is.EqualTo("Steel Plates"));
            Assert.That(stop.DropOff[0].BaseItemTypeID, Is.EqualTo("Steel Plates"));
            Assert.That(stop.DropOff[0].Quantity, Is.EqualTo(50));
            Assert.That(stop.DropOff[0].ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Commodity));
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
            Assert.That(stop.DropOff[0].Quantity, Is.EqualTo(70));
        }

        [Test]
        public void AutoFillCommodities_FulfilledCommodity_Skipped()
        {
            var vm = CreateViewModel();
            var colony = CreateColonyWithCommodities("c1",
                new CommodityRequested { Name = "Steel Plates", Requested = 50, Delivered = 50, Fulfilled = true });
            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillCommodities(stops, uuid => uuid == "c1" ? colony : null);

            Assert.That(added, Is.EqualTo(0));
        }

        [Test]
        public void AutoFillCommodities_ZeroShortfall_Skipped()
        {
            var vm = CreateViewModel();
            var colony = CreateColonyWithCommodities("c1",
                new CommodityRequested { Name = "Steel Plates", Requested = 50, Delivered = 50, Fulfilled = false });
            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillCommodities(stops, uuid => uuid == "c1" ? colony : null);

            Assert.That(added, Is.EqualTo(0));
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

            Assert.That(added, Is.EqualTo(1));
            Assert.That(vm.Data.Stops.Any(s => s.ColonyUUID == "c1"), Is.False);
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

            Assert.That(stop.DropOff.Count, Is.EqualTo(2));
            Assert.That(stop.DropOff[0].BaseItemTypeID, Is.EqualTo("Iron"));
            Assert.That(stop.DropOff[0].ResourcePurity, Is.EqualTo("High"));
            Assert.That(stop.DropOff[1].Name, Is.EqualTo("Steel Plates"));
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

            Assert.That(stop.PickUp.Count, Is.EqualTo(1));
            Assert.That(stop.PickUp[0].BaseItemTypeID, Is.EqualTo("Iron"));
        }

        [Test]
        public void AutoFillCommodities_EmptyCommoditiesList_AddsNothing()
        {
            var vm = CreateViewModel();
            var colony = new Colony { UUID = "c1" };
            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillCommodities(stops, uuid => uuid == "c1" ? colony : null);

            Assert.That(added, Is.EqualTo(0));
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

            Assert.That(added, Is.EqualTo(2));
            var stop1 = vm.Data.Stops.First(s => s.ColonyUUID == "c1");
            Assert.That(stop1.DropOff.Count, Is.EqualTo(1));
            Assert.That(stop1.DropOff[0].Name, Is.EqualTo("Steel Plates"));
            Assert.That(stop1.DropOff[0].Quantity, Is.EqualTo(50));

            var stop2 = vm.Data.Stops.First(s => s.ColonyUUID == "c2");
            Assert.That(stop2.DropOff.Count, Is.EqualTo(1));
            Assert.That(stop2.DropOff[0].Name, Is.EqualTo("Glass Panels"));
            Assert.That(stop2.DropOff[0].Quantity, Is.EqualTo(15));
        }

        // -----------------------------------------------------------------------
        // AutoFillFlatpacks — Property 1
        // Validates: Requirements 2.1, 2.2, 2.3
        // -----------------------------------------------------------------------

        private OE2EmpireTracker.Models.Blueprint CreateTestBlueprint(string uuid, string name, string bpType = "Flatpacks/Habitat")
        {
            var bp = new OE2EmpireTracker.Models.Blueprint(name) { UUID = uuid, BluePrintType = bpType };
            playerContext.blueprintList.Add(bp);
            return bp;
        }

        private Colony CreateColonyWithStructures(string uuid, params ColonyStructure[] structures)
        {
            var colony = new Colony { UUID = uuid };
            colony.Structures.AddRange(structures);
            return colony;
        }

        private ColonyStructure MakeStructure(string bpUUID, bool built, bool staged)
        {
            var s = new ColonyStructure { UUID = System.Guid.NewGuid().ToString(), FlatpackBlueprintUUID = bpUUID };
            if (built) s.Properties.setProperty("Built", true);
            if (staged) s.Properties.setProperty("Staged", true);
            return s;
        }

        [Test]
        public void AutoFillFlatpacks_NoStructures_ReturnsZero()
        {
            var vm = CreateViewModel();
            var colony = new Colony { UUID = "c1" };
            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillFlatpacks(stops, uuid => uuid == "c1" ? colony : null);

            Assert.That(added, Is.EqualTo(0));
        }

        [Test]
        public void AutoFillFlatpacks_AllBuilt_ReturnsZero()
        {
            var bp = CreateTestBlueprint("bp1", "Habitat");
            var vm = CreateViewModel();
            var colony = CreateColonyWithStructures("c1",
                MakeStructure("bp1", built: true, staged: false));
            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillFlatpacks(stops, uuid => uuid == "c1" ? colony : null);

            Assert.That(added, Is.EqualTo(0));
        }

        [Test]
        public void AutoFillFlatpacks_AllStaged_ReturnsZero()
        {
            var bp = CreateTestBlueprint("bp2", "Refinery");
            var vm = CreateViewModel();
            var colony = CreateColonyWithStructures("c1",
                MakeStructure("bp2", built: false, staged: true));
            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillFlatpacks(stops, uuid => uuid == "c1" ? colony : null);

            Assert.That(added, Is.EqualTo(0));
        }

        [Test]
        public void AutoFillFlatpacks_UnbuiltUnstaged_AddsFlatpackItem()
        {
            var bp = CreateTestBlueprint("bp-unbuilt", "Habitat");
            var vm = CreateViewModel();
            var colony = CreateColonyWithStructures("c1",
                MakeStructure("bp-unbuilt", built: false, staged: false));
            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillFlatpacks(stops, uuid => uuid == "c1" ? colony : null);

            Assert.That(added, Is.EqualTo(1));
            var stop = vm.Data.Stops.First(s => s.ColonyUUID == "c1");
            Assert.That(stop.DropOff.Count, Is.EqualTo(1));
            Assert.That(stop.DropOff[0].ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Flatpack));
            Assert.That(stop.DropOff[0].BaseItemTypeID, Is.EqualTo("bp-unbuilt"));
            Assert.That(stop.DropOff[0].Quantity, Is.EqualTo(1));
        }

        [Test]
        public void AutoFillFlatpacks_StacksSameBlueprintUUID()
        {
            CreateTestBlueprint("bp-stack", "Refinery");
            var vm = CreateViewModel();
            var colony = CreateColonyWithStructures("c1",
                MakeStructure("bp-stack", built: false, staged: false),
                MakeStructure("bp-stack", built: false, staged: false),
                MakeStructure("bp-stack", built: false, staged: false));
            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillFlatpacks(stops, uuid => uuid == "c1" ? colony : null);

            Assert.That(added, Is.EqualTo(1)); // 1 aggregated item, not 3
            var stop = vm.Data.Stops.First(s => s.ColonyUUID == "c1");
            Assert.That(stop.DropOff.Count, Is.EqualTo(1));
            Assert.That(stop.DropOff[0].BaseItemTypeID, Is.EqualTo("bp-stack"));
            Assert.That(stop.DropOff[0].Quantity, Is.EqualTo(3));
        }

        [Test]
        public void AutoFillFlatpacks_MixOfStates_CorrectCount()
        {
            CreateTestBlueprint("bp-a", "Mining Rig");
            CreateTestBlueprint("bp-b", "Refinery");
            CreateTestBlueprint("bp-c", "Habitat");
            var vm = CreateViewModel();
            var colony = CreateColonyWithStructures("c1",
                MakeStructure("bp-a", built: true, staged: false),   // built — skip
                MakeStructure("bp-b", built: false, staged: true),   // staged — skip
                MakeStructure("bp-c", built: false, staged: false)); // unbuilt+unstaged — add
            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillFlatpacks(stops, uuid => uuid == "c1" ? colony : null);

            Assert.That(added, Is.EqualTo(1));
            var stop = vm.Data.Stops.First(s => s.ColonyUUID == "c1");
            Assert.That(stop.DropOff.Count, Is.EqualTo(1));
            Assert.That(stop.DropOff[0].ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Flatpack));
            Assert.That(stop.DropOff[0].BaseItemTypeID, Is.EqualTo("bp-c"));
            Assert.That(stop.DropOff[0].Quantity, Is.EqualTo(1));
        }

        [Test]
        public void AutoFillFlatpacks_PreservesExistingItems()
        {
            CreateTestBlueprint("bp-d", "Power Plant");
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddDropOffItem(stop, ItemType.ItemTypeEnum.Resource, "Iron", "Iron", 100, "High");

            var colony = CreateColonyWithStructures("c1",
                MakeStructure("bp-d", built: false, staged: false));
            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            vm.AutoFillFlatpacks(stops, uuid => uuid == "c1" ? colony : null);

            Assert.That(stop.DropOff.Count, Is.EqualTo(2));
            Assert.That(stop.DropOff[0].BaseItemTypeID, Is.EqualTo("Iron"));
            Assert.That(stop.DropOff[1].ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Flatpack));
        }

        [Test]
        public void AutoFillFlatpacks_MissingColony_SkipsStop()
        {
            var vm = CreateViewModel();
            var stops = new[] { new RouteStop { ColonyUUID = "missing", Sequence = 0 } };

            int added = vm.AutoFillFlatpacks(stops, uuid => null);

            Assert.That(added, Is.EqualTo(0));
        }

        [Test]
        public void AutoFillFlatpacks_MissingBlueprint_SkipsStructure()
        {
            var vm = CreateViewModel();
            var colony = CreateColonyWithStructures("c1",
                MakeStructure("nonexistent-bp", built: false, staged: false));
            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillFlatpacks(stops, uuid => uuid == "c1" ? colony : null);

            Assert.That(added, Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // AutoFillManufacturingResources — Property 3
        // Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5, 8.1, 8.2, 8.3, 8.4
        // -----------------------------------------------------------------------

        private OE2EmpireTracker.Models.Blueprint CreateManufactoryBlueprint(string uuid, string name, Dictionary<string, string> resources)
        {
            var bp = new OE2EmpireTracker.Models.Blueprint(name) { UUID = uuid, BluePrintType = "Flatpacks/Manufactory", Resources = resources };
            bp.Properties.setProperty("ManufactureTime", "1h");
            bp.Properties.setProperty("CanManufacture", true);
            playerContext.blueprintList.Add(bp);
            return bp;
        }

        private ColonyStructure MakeStagingManufactory(string flatpackBpUUID, string mfgBpUUID, int qty)
        {
            var s = new ColonyStructure
            {
                UUID = System.Guid.NewGuid().ToString(),
                FlatpackBlueprintUUID = flatpackBpUUID,
                ManufacturingBlueprintUUID = mfgBpUUID,
                ManufacturingQuantity = qty,
                StagingResources = true
            };
            s.Properties.setProperty("Built", true);
            s.Properties.setProperty("Online", true);
            return s;
        }

        private ColonyStructure MakeStagingCommodityFactory(string flatpackBpUUID, string commodityName, int qty)
        {
            var s = new ColonyStructure
            {
                UUID = System.Guid.NewGuid().ToString(),
                FlatpackBlueprintUUID = flatpackBpUUID,
                ManufacturingCommodityName = commodityName,
                ManufacturingQuantity = qty,
                StagingResources = true
            };
            s.Properties.setProperty("Built", true);
            s.Properties.setProperty("Online", true);
            return s;
        }

        [Test]
        public void AutoFillManufacturingResources_NoStagingStructures_ReturnsZero()
        {
            var vm = CreateViewModel();
            var colony = new Colony { UUID = "c1" };
            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillManufacturingResources(stops,
                uuid => uuid == "c1" ? colony : null,
                uuid => playerContext.FindBlueprint(uuid));

            Assert.That(added, Is.EqualTo(0));
        }

        [Test]
        public void AutoFillManufacturingResources_StagingManufactory_AddsResourceShortfall()
        {
            var flatpackBp = new OE2EmpireTracker.Models.Blueprint("Manufactory") { UUID = "fp-mfg-shortfall", BluePrintType = "Flatpacks/Manufactory" };
            playerContext.blueprintList.Add(flatpackBp);

            CreateManufactoryBlueprint("mfg-bp-shortfall", "Widget",
                new Dictionary<string, string> { { "Iron", "5" }, { "Copper", "3" } });

            var vm = CreateViewModel();
            var structure = MakeStagingManufactory("fp-mfg-shortfall", "mfg-bp-shortfall", 2); // needs 10 Iron, 6 Copper
            var colony = CreateColonyWithStructures("c1", structure);

            // Add 4 Refined Iron to warehouse (shortfall = 6), no Copper (shortfall = 6)
            var ironItem = new Item(ItemType.ItemTypeEnum.Resource, "Iron")
            {
                UUID = System.Guid.NewGuid().ToString(),
                BaseItemTypeID = "Iron",
                ResourcePurity = "Refined",
                Quantity = 4
            };
            colony.Items.AddItem(ironItem);

            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillManufacturingResources(stops,
                uuid => uuid == "c1" ? colony : null,
                uuid => playerContext.FindBlueprint(uuid));

            Assert.That(added, Is.EqualTo(2));
            var stop = vm.Data.Stops.First(s => s.ColonyUUID == "c1");
            var ironDrop = stop.DropOff.FirstOrDefault(d => d.BaseItemTypeID == "Iron");
            var copperDrop = stop.DropOff.FirstOrDefault(d => d.BaseItemTypeID == "Copper");
            Assert.That(ironDrop, Is.Not.Null);
            Assert.That(ironDrop.Quantity, Is.EqualTo(6));
            Assert.That(ironDrop.ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Resource));
            Assert.That(copperDrop, Is.Not.Null);
            Assert.That(copperDrop.Quantity, Is.EqualTo(6));
        }

        [Test]
        public void AutoFillManufacturingResources_ResourcePurityIsRefined()
        {
            var flatpackBp = new OE2EmpireTracker.Models.Blueprint("Manufactory") { UUID = "fp-mfg-purity", BluePrintType = "Flatpacks/Manufactory" };
            playerContext.blueprintList.Add(flatpackBp);

            CreateManufactoryBlueprint("mfg-bp-purity", "Gadget",
                new Dictionary<string, string> { { "Iron", "3" }, { "Copper", "2" } });

            var vm = CreateViewModel();
            var structure = MakeStagingManufactory("fp-mfg-purity", "mfg-bp-purity", 1);
            var colony = CreateColonyWithStructures("c1", structure);

            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillManufacturingResources(stops,
                uuid => uuid == "c1" ? colony : null,
                uuid => playerContext.FindBlueprint(uuid));

            Assert.That(added, Is.EqualTo(2));
            var stop = vm.Data.Stops.First(s => s.ColonyUUID == "c1");
            foreach (var item in stop.DropOff)
            {
                Assert.That(item.ResourcePurity, Is.EqualTo("Refined"),
                    $"Resource '{item.BaseItemTypeID}' should have Refined purity");
                Assert.That(item.ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Resource));
            }
        }

        [Test]
        public void AutoFillManufacturingResources_WarehouseFullyStocked_ReturnsZero()
        {
            // Create flatpack blueprint for the manufactory structure
            var flatpackBp = new OE2EmpireTracker.Models.Blueprint("Manufactory") { UUID = "fp-mfg1", BluePrintType = "Flatpacks/Manufactory" };
            playerContext.blueprintList.Add(flatpackBp);

            // Create the manufacturing target blueprint with resources
            var mfgBp = CreateManufactoryBlueprint("mfg-bp1", "Widget",
                new Dictionary<string, string> { { "Iron", "5" } });

            var vm = CreateViewModel();
            var structure = MakeStagingManufactory("fp-mfg1", "mfg-bp1", 2); // needs 10 Iron
            var colony = CreateColonyWithStructures("c1", structure);

            // Add 10 Refined Iron to warehouse
            var item = new Item(ItemType.ItemTypeEnum.Resource, "Iron")
            {
                UUID = System.Guid.NewGuid().ToString(),
                BaseItemTypeID = "Iron",
                ResourcePurity = "Refined",
                Quantity = 10
            };
            colony.Items.AddItem(item);

            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillManufacturingResources(stops,
                uuid => uuid == "c1" ? colony : null,
                uuid => playerContext.FindBlueprint(uuid));

            Assert.That(added, Is.EqualTo(0));
        }

        [Test]
        public void AutoFillManufacturingResources_PartialWarehouse_CorrectShortfall()
        {
            var flatpackBp = new OE2EmpireTracker.Models.Blueprint("Manufactory") { UUID = "fp-mfg2", BluePrintType = "Flatpacks/Manufactory" };
            playerContext.blueprintList.Add(flatpackBp);

            CreateManufactoryBlueprint("mfg-bp2", "Gadget",
                new Dictionary<string, string> { { "Iron", "5" }, { "Copper", "3" } });

            var vm = CreateViewModel();
            var structure = MakeStagingManufactory("fp-mfg2", "mfg-bp2", 2); // needs 10 Iron, 6 Copper
            var colony = CreateColonyWithStructures("c1", structure);

            // Add 4 Refined Iron (shortfall = 6) and 0 Copper (shortfall = 6)
            var ironItem = new Item(ItemType.ItemTypeEnum.Resource, "Iron")
            {
                UUID = System.Guid.NewGuid().ToString(),
                BaseItemTypeID = "Iron",
                ResourcePurity = "Refined",
                Quantity = 4
            };
            colony.Items.AddItem(ironItem);

            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillManufacturingResources(stops,
                uuid => uuid == "c1" ? colony : null,
                uuid => playerContext.FindBlueprint(uuid));

            Assert.That(added, Is.EqualTo(2));
            var stop = vm.Data.Stops.First(s => s.ColonyUUID == "c1");
            var ironDrop = stop.DropOff.FirstOrDefault(d => d.BaseItemTypeID == "Iron");
            var copperDrop = stop.DropOff.FirstOrDefault(d => d.BaseItemTypeID == "Copper");
            Assert.That(ironDrop, Is.Not.Null);
            Assert.That(ironDrop.Quantity, Is.EqualTo(6));
            Assert.That(ironDrop.ResourcePurity, Is.EqualTo("Refined"));
            Assert.That(ironDrop.ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Resource));
            Assert.That(copperDrop, Is.Not.Null);
            Assert.That(copperDrop.Quantity, Is.EqualTo(6));
        }

        [Test]
        public void AutoFillManufacturingResources_MultipleStagingStructures_AggregatesNeeds()
        {
            var flatpackBp = new OE2EmpireTracker.Models.Blueprint("Manufactory") { UUID = "fp-mfg3", BluePrintType = "Flatpacks/Manufactory" };
            playerContext.blueprintList.Add(flatpackBp);

            CreateManufactoryBlueprint("mfg-bp3", "Part",
                new Dictionary<string, string> { { "Iron", "2" } });

            var vm = CreateViewModel();
            var s1 = MakeStagingManufactory("fp-mfg3", "mfg-bp3", 3); // needs 6 Iron
            var s2 = MakeStagingManufactory("fp-mfg3", "mfg-bp3", 2); // needs 4 Iron
            var colony = CreateColonyWithStructures("c1", s1, s2);     // total = 10 Iron

            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillManufacturingResources(stops,
                uuid => uuid == "c1" ? colony : null,
                uuid => playerContext.FindBlueprint(uuid));

            Assert.That(added, Is.EqualTo(1));
            var stop = vm.Data.Stops.First(s => s.ColonyUUID == "c1");
            Assert.That(stop.DropOff[0].Quantity, Is.EqualTo(10));
        }

        [Test]
        public void AutoFillManufacturingResources_CommodityFactory_CorrectShortfall()
        {
            var flatpackBp = new OE2EmpireTracker.Models.Blueprint("CommodityFactory") { UUID = "fp-cf1", BluePrintType = "Flatpacks/CommodityFactory" };
            playerContext.blueprintList.Add(flatpackBp);

            // Use a real commodity — "Advanced Biolubricants" needs Alkali Organics (2) and Strong Acidic Inorganics (2)
            var vm = CreateViewModel();
            var structure = MakeStagingCommodityFactory("fp-cf1", "Advanced Biolubricants", 3);
            var colony = CreateColonyWithStructures("c1", structure);

            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillManufacturingResources(stops,
                uuid => uuid == "c1" ? colony : null,
                uuid => playerContext.FindBlueprint(uuid));

            Assert.That(added, Is.EqualTo(2)); // 2 resource types
            var stop = vm.Data.Stops.First(s => s.ColonyUUID == "c1");
            var alkali = stop.DropOff.FirstOrDefault(d => d.BaseItemTypeID == "Alkali Organics");
            var acidic = stop.DropOff.FirstOrDefault(d => d.BaseItemTypeID == "Strong Acidic Inorganics");
            Assert.That(alkali, Is.Not.Null);
            Assert.That(alkali.Quantity, Is.EqualTo(6)); // 2 * 3
            Assert.That(acidic, Is.Not.Null);
            Assert.That(acidic.Quantity, Is.EqualTo(6)); // 2 * 3
        }

        [Test]
        public void AutoFillManufacturingResources_ZeroQuantity_Skipped()
        {
            var flatpackBp = new OE2EmpireTracker.Models.Blueprint("Manufactory") { UUID = "fp-mfg4", BluePrintType = "Flatpacks/Manufactory" };
            playerContext.blueprintList.Add(flatpackBp);

            CreateManufactoryBlueprint("mfg-bp4", "Thing",
                new Dictionary<string, string> { { "Iron", "5" } });

            var vm = CreateViewModel();
            var structure = MakeStagingManufactory("fp-mfg4", "mfg-bp4", 0); // qty = 0
            var colony = CreateColonyWithStructures("c1", structure);

            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillManufacturingResources(stops,
                uuid => uuid == "c1" ? colony : null,
                uuid => playerContext.FindBlueprint(uuid));

            Assert.That(added, Is.EqualTo(0));
        }

        [Test]
        public void AutoFillManufacturingResources_PreservesExistingItems()
        {
            var flatpackBp = new OE2EmpireTracker.Models.Blueprint("Manufactory") { UUID = "fp-mfg5", BluePrintType = "Flatpacks/Manufactory" };
            playerContext.blueprintList.Add(flatpackBp);

            CreateManufactoryBlueprint("mfg-bp5", "Gizmo",
                new Dictionary<string, string> { { "Iron", "1" } });

            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddDropOffItem(stop, ItemType.ItemTypeEnum.Commodity, "Steel", "Steel", 50);

            var structure = MakeStagingManufactory("fp-mfg5", "mfg-bp5", 1);
            var colony = CreateColonyWithStructures("c1", structure);

            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            vm.AutoFillManufacturingResources(stops,
                uuid => uuid == "c1" ? colony : null,
                uuid => playerContext.FindBlueprint(uuid));

            Assert.That(stop.DropOff.Count, Is.EqualTo(2));
            Assert.That(stop.DropOff[0].BaseItemTypeID, Is.EqualTo("Steel"));
        }

        // -----------------------------------------------------------------------
        // AutoFillWorkers — Property 4
        // Validates: Requirements 10.1, 10.2, 10.3, 10.4
        // -----------------------------------------------------------------------

        private OE2EmpireTracker.Models.Blueprint CreateBlueprintWithWorkers(string uuid, string name, string bpType,
            int blueCollar = 0, int whiteCollar = 0, int specialist = 0)
        {
            var bp = new OE2EmpireTracker.Models.Blueprint(name) { UUID = uuid, BluePrintType = bpType };
            if (blueCollar > 0) bp.Properties.setProperty("BlueCollarDetail", blueCollar.ToString());
            if (whiteCollar > 0) bp.Properties.setProperty("WhiteCollarDetail", whiteCollar.ToString());
            if (specialist > 0) bp.Properties.setProperty("SpecialistDetail", specialist.ToString());
            playerContext.blueprintList.Add(bp);
            return bp;
        }

        private ColonyStructure MakeBuiltOnlineStructure(string bpUUID, int blueAssigned = 0, int whiteAssigned = 0, int specAssigned = 0)
        {
            var s = new ColonyStructure
            {
                UUID = System.Guid.NewGuid().ToString(),
                FlatpackBlueprintUUID = bpUUID
            };
            s.Properties.setProperty("Built", true);
            s.Properties.setProperty("Online", true);
            for (int i = 1; i <= blueAssigned; i++)
                s.AssignedWorkers.setProperty("BlueCollar" + i, true);
            for (int i = 1; i <= whiteAssigned; i++)
                s.AssignedWorkers.setProperty("WhiteCollar" + i, true);
            for (int i = 1; i <= specAssigned; i++)
                s.AssignedWorkers.setProperty("Specialist" + i, true);
            return s;
        }

        [Test]
        public void AutoFillWorkers_FullyStaffed_ReturnsZero()
        {
            var bp = CreateBlueprintWithWorkers("bp-w1", "Habitat", "Flatpacks/Habitat", blueCollar: 1);
            var vm = CreateViewModel();
            var structure = MakeBuiltOnlineStructure("bp-w1", blueAssigned: 1);
            var colony = CreateColonyWithStructures("c1", structure);

            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillWorkers(stops,
                uuid => uuid == "c1" ? colony : null, playerContext);

            Assert.That(added, Is.EqualTo(0));
        }

        [Test]
        public void AutoFillWorkers_PartialStaffing_CorrectGap()
        {
            var bp = CreateBlueprintWithWorkers("bp-w2", "Factory", "Flatpacks/Factory",
                blueCollar: 2, whiteCollar: 1);
            var vm = CreateViewModel();
            // Assign 1 of 2 blue collar, 0 of 1 white collar
            var structure = MakeBuiltOnlineStructure("bp-w2", blueAssigned: 1, whiteAssigned: 0);
            var colony = CreateColonyWithStructures("c1", structure);

            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillWorkers(stops,
                uuid => uuid == "c1" ? colony : null, playerContext);

            Assert.That(added, Is.EqualTo(2)); // 1 BlueCollar gap + 1 WhiteCollar gap
            var stop = vm.Data.Stops.First(s => s.ColonyUUID == "c1");
            var blueDrop = stop.DropOff.FirstOrDefault(d => d.BaseItemTypeID == "BlueCollarDetail");
            var whiteDrop = stop.DropOff.FirstOrDefault(d => d.BaseItemTypeID == "WhiteCollarDetail");
            Assert.That(blueDrop, Is.Not.Null);
            Assert.That(blueDrop.Quantity, Is.EqualTo(1));
            Assert.That(blueDrop.ItemType, Is.EqualTo(ItemType.ItemTypeEnum.WorkDetail));
            Assert.That(blueDrop.Name, Is.EqualTo("Blue Collar Detail"));
            Assert.That(whiteDrop, Is.Not.Null);
            Assert.That(whiteDrop.Quantity, Is.EqualTo(1));
        }

        [Test]
        public void AutoFillWorkers_NoStructures_ReturnsZero()
        {
            var vm = CreateViewModel();
            var colony = new Colony { UUID = "c1" };
            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            int added = vm.AutoFillWorkers(stops,
                uuid => uuid == "c1" ? colony : null, playerContext);

            Assert.That(added, Is.EqualTo(0));
        }

        [Test]
        public void AutoFillWorkers_PreservesExistingItems()
        {
            var bp = CreateBlueprintWithWorkers("bp-w3", "Lab", "Flatpacks/Lab", blueCollar: 1);
            var vm = CreateViewModel();
            var stop = vm.GetOrCreateStop("c1", 0);
            vm.AddDropOffItem(stop, ItemType.ItemTypeEnum.Resource, "Iron", "Iron", 100);

            var structure = MakeBuiltOnlineStructure("bp-w3", blueAssigned: 0);
            var colony = CreateColonyWithStructures("c1", structure);

            var stops = new[] { new RouteStop { ColonyUUID = "c1", Sequence = 0 } };

            vm.AutoFillWorkers(stops,
                uuid => uuid == "c1" ? colony : null, playerContext);

            Assert.That(stop.DropOff[0].BaseItemTypeID, Is.EqualTo("Iron"));
            Assert.That(stop.DropOff.Count >= 2, Is.True);
        }
    }
}
