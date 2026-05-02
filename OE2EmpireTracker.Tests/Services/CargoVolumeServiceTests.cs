using System;
using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class CargoVolumeServiceTests
    {
        [Test]
        public void ComputeLoadVolume_EmptyList_ReturnsZero()
        {
            var result = CargoVolumeService.ComputeLoadVolume(
                new List<DeliveryItem>(), _ => null);
            Assert.That(result.TotalVolume, Is.EqualTo(0m));
            Assert.That(result.TotalMass, Is.EqualTo(0m));
        }

        [Test]
        public void ComputeLoadVolume_NullList_ReturnsZero()
        {
            var result = CargoVolumeService.ComputeLoadVolume(null, _ => null);
            Assert.That(result.TotalVolume, Is.EqualTo(0m));
            Assert.That(result.TotalMass, Is.EqualTo(0m));
        }

        [Test]
        public void ComputeLoadVolume_Resources_VolumeIs1Each()
        {
            var items = new List<DeliveryItem>
            {
                new DeliveryItem
                {
                    ItemType = ItemType.ItemTypeEnum.Resource,
                    BaseItemTypeID = "Iron",
                    Name = "Iron",
                    Quantity = 100
                }
            };

            var result = CargoVolumeService.ComputeLoadVolume(items, _ => null);
            Assert.That(result.TotalVolume, Is.EqualTo(100m));
            Assert.That(result.TotalMass, Is.EqualTo(100m));
        }

        [Test]
        public void ComputeLoadVolume_Commodities_VolumeIs10Each()
        {
            var items = new List<DeliveryItem>
            {
                new DeliveryItem
                {
                    ItemType = ItemType.ItemTypeEnum.Commodity,
                    BaseItemTypeID = "Steel",
                    Name = "Steel",
                    Quantity = 50
                }
            };

            var result = CargoVolumeService.ComputeLoadVolume(items, _ => null);
            Assert.That(result.TotalVolume, Is.EqualTo(500m));
            Assert.That(result.TotalMass, Is.EqualTo(250m));
        }

        [Test]
        public void ComputeLoadVolume_Workers_VolumeIs50Each()
        {
            var items = new List<DeliveryItem>
            {
                new DeliveryItem
                {
                    ItemType = ItemType.ItemTypeEnum.WorkDetail,
                    BaseItemTypeID = "Miner",
                    Name = "Miner",
                    Quantity = 10
                }
            };

            var result = CargoVolumeService.ComputeLoadVolume(items, _ => null);
            Assert.That(result.TotalVolume, Is.EqualTo(500m));
            Assert.That(result.TotalMass, Is.EqualTo(100m));
        }

        [Test]
        public void ComputeLoadVolume_Blueprints_VolumeIsZero()
        {
            var items = new List<DeliveryItem>
            {
                new DeliveryItem
                {
                    ItemType = ItemType.ItemTypeEnum.Blueprint,
                    BaseItemTypeID = "bp1",
                    Name = "Some BP",
                    Quantity = 5
                }
            };

            var result = CargoVolumeService.ComputeLoadVolume(items, _ => null);
            Assert.That(result.TotalVolume, Is.EqualTo(0m));
            Assert.That(result.TotalMass, Is.EqualTo(0m));
        }

        [Test]
        public void ComputeLoadVolume_ManufacturedItem_UsesBlueprint()
        {
            var bp = MakeBlueprint(
                "reactor1",
                "Reactor",
                cargoVolumeSize: 25m,
                mass: 100m);
            var finder = MakeFinder(bp);

            var items = new List<DeliveryItem>
            {
                new DeliveryItem
                {
                    ItemType = ItemType.ItemTypeEnum.None,
                    BaseItemTypeID = "reactor1",
                    Name = "Reactor",
                    Quantity = 4
                }
            };

            var result = CargoVolumeService.ComputeLoadVolume(items, finder);
            Assert.That(result.TotalVolume, Is.EqualTo(100m));
            Assert.That(result.TotalMass, Is.EqualTo(400m));
        }

        [Test]
        public void ComputeLoadVolume_MixedItems_SumsCorrectly()
        {
            var bp = MakeBlueprint(
                "flatpack1",
                "Flatpack",
                cargoVolumeSize: 200m,
                mass: 500m);
            var finder = MakeFinder(bp);

            var items = new List<DeliveryItem>
            {
                new DeliveryItem
                {
                    ItemType = ItemType.ItemTypeEnum.Resource,
                    BaseItemTypeID = "Iron",
                    Name = "Iron",
                    Quantity = 100
                },
                new DeliveryItem
                {
                    ItemType = ItemType.ItemTypeEnum.Commodity,
                    BaseItemTypeID = "Steel",
                    Name = "Steel",
                    Quantity = 20
                },
                new DeliveryItem
                {
                    ItemType = ItemType.ItemTypeEnum.None,
                    BaseItemTypeID = "flatpack1",
                    Name = "Flatpack",
                    Quantity = 2
                }
            };

            var result = CargoVolumeService.ComputeLoadVolume(items, finder);
            // 100*1 + 20*10 + 2*200 = 100 + 200 + 400 = 700
            Assert.That(result.TotalVolume, Is.EqualTo(700m));
            // 100*1 + 20*5 + 2*500 = 100 + 100 + 1000 = 1200
            Assert.That(result.TotalMass, Is.EqualTo(1200m));
        }

        // --- SplitIntoTrips tests ---

        [Test]
        public void SplitIntoTrips_FitsInOneTrip_ReturnsSingleTrip()
        {
            var items = new List<DeliveryItem>
            {
                new DeliveryItem
                {
                    ItemType = ItemType.ItemTypeEnum.Resource,
                    BaseItemTypeID = "Iron",
                    Name = "Iron",
                    Quantity = 50
                }
            };

            var trips = CargoVolumeService.SplitIntoTrips(
                items, 100m, _ => null);
            Assert.That(trips.Count, Is.EqualTo(1));
            Assert.That(trips[0].Count, Is.EqualTo(1));
            Assert.That(trips[0][0].Quantity, Is.EqualTo(50));
        }

        [Test]
        public void SplitIntoTrips_ExceedsCapacity_SplitsIntoMultiple()
        {
            var items = new List<DeliveryItem>
            {
                new DeliveryItem
                {
                    ItemType = ItemType.ItemTypeEnum.Resource,
                    BaseItemTypeID = "Iron",
                    Name = "Iron",
                    Quantity = 250
                }
            };

            // Capacity 100, each resource = 1 vol, so 100 per trip
            var trips = CargoVolumeService.SplitIntoTrips(
                items, 100m, _ => null);
            Assert.That(trips.Count, Is.EqualTo(3));
            Assert.That(trips[0][0].Quantity, Is.EqualTo(100));
            Assert.That(trips[1][0].Quantity, Is.EqualTo(100));
            Assert.That(trips[2][0].Quantity, Is.EqualTo(50));
        }

        [Test]
        public void SplitIntoTrips_EmptyList_ReturnsEmpty()
        {
            var trips = CargoVolumeService.SplitIntoTrips(
                new List<DeliveryItem>(), 100m, _ => null);
            Assert.That(trips.Count, Is.EqualTo(0));
        }

        [Test]
        public void SplitIntoTrips_ZeroCapacity_ReturnsSingleTrip()
        {
            var items = new List<DeliveryItem>
            {
                new DeliveryItem
                {
                    ItemType = ItemType.ItemTypeEnum.Resource,
                    BaseItemTypeID = "Iron",
                    Name = "Iron",
                    Quantity = 50
                }
            };

            var trips = CargoVolumeService.SplitIntoTrips(
                items, 0m, _ => null);
            Assert.That(trips.Count, Is.EqualTo(1));
        }

        [Test]
        public void SplitIntoTrips_ZeroVolumeItems_AllFitInOneTrip()
        {
            var items = new List<DeliveryItem>
            {
                new DeliveryItem
                {
                    ItemType = ItemType.ItemTypeEnum.Blueprint,
                    BaseItemTypeID = "bp1",
                    Name = "Blueprint",
                    Quantity = 1000
                }
            };

            var trips = CargoVolumeService.SplitIntoTrips(
                items, 10m, _ => null);
            Assert.That(trips.Count, Is.EqualTo(1));
            Assert.That(trips[0][0].Quantity, Is.EqualTo(1000));
        }

        [Test]
        public void SplitIntoTrips_MultipleItemTypes_SplitsCorrectly()
        {
            var items = new List<DeliveryItem>
            {
                new DeliveryItem
                {
                    ItemType = ItemType.ItemTypeEnum.Resource,
                    BaseItemTypeID = "Iron",
                    Name = "Iron",
                    Quantity = 80
                },
                new DeliveryItem
                {
                    ItemType = ItemType.ItemTypeEnum.Commodity,
                    BaseItemTypeID = "Steel",
                    Name = "Steel",
                    Quantity = 5
                }
            };

            // Capacity 100: Iron=80vol, Steel=5*10=50vol, total=130
            // Trip 1: 80 Iron (80vol) + 2 Steel (20vol) = 100
            // Trip 2: 3 Steel (30vol)
            var trips = CargoVolumeService.SplitIntoTrips(
                items, 100m, _ => null);
            Assert.That(trips.Count, Is.EqualTo(2));

            // Verify total quantities preserved
            int totalIron = 0, totalSteel = 0;
            foreach (var trip in trips)
            {
                foreach (var item in trip)
                {
                    if (item.BaseItemTypeID == "Iron") totalIron += item.Quantity;
                    if (item.BaseItemTypeID == "Steel") totalSteel += item.Quantity;
                }
            }

            Assert.That(totalIron, Is.EqualTo(80));
            Assert.That(totalSteel, Is.EqualTo(5));
        }

        [Test]
        public void SplitIntoTrips_OversizedItem_GetsOwnTrip()
        {
            var bp = MakeBlueprint(
                "bigpart1",
                "Big Part",
                cargoVolumeSize: 200m,
                mass: 1000m);
            var finder = MakeFinder(bp);

            var items = new List<DeliveryItem>
            {
                new DeliveryItem
                {
                    ItemType = ItemType.ItemTypeEnum.None,
                    BaseItemTypeID = "bigpart1",
                    Name = "Big Part",
                    Quantity = 3
                }
            };

            // Capacity 150, each item is 200 vol (oversized), so each gets its own trip
            var trips = CargoVolumeService.SplitIntoTrips(
                items, 150m, finder);
            Assert.That(trips.Count, Is.EqualTo(3));
            Assert.That(trips[0][0].Quantity, Is.EqualTo(1));
            Assert.That(trips[1][0].Quantity, Is.EqualTo(1));
            Assert.That(trips[2][0].Quantity, Is.EqualTo(1));
        }

        [Test]
        public void ComputeLoadVolume_SurveyItems_VolumeIsZero()
        {
            var items = new List<DeliveryItem>
            {
                new DeliveryItem
                {
                    ItemType = ItemType.ItemTypeEnum.Survey,
                    BaseItemTypeID = "survey1",
                    Name = "Planet Survey",
                    Quantity = 3
                }
            };

            var result = CargoVolumeService.ComputeLoadVolume(items, _ => null);
            Assert.That(result.TotalVolume, Is.EqualTo(0m));
        }

        [Test]
        public void GetItemVolume_CrateType_UsesCrateVolume()
        {
            var crateBp = MakeBlueprint(
                "crate1",
                "Storage Crate",
                cargoVolumeSize: 500m,
                mass: 50m,
                bpType: "Crate");
            var finder = MakeFinder(crateBp);

            var item = new DeliveryItem
            {
                ItemType = ItemType.ItemTypeEnum.None,
                BaseItemTypeID = "crate1",
                Name = "Storage Crate",
                Quantity = 1
            };

            decimal vol = CargoVolumeService.GetItemVolume(item, finder);
            Assert.That(vol, Is.EqualTo(500m));
        }

        private ReadOnlyBlueprint MakeBlueprint(
            string uuid,
            string name,
            decimal cargoVolumeSize = 0m,
            decimal mass = 0m,
            string bpType = "Component")
        {
            var bp = new OE2EmpireTracker.Models.Blueprint(name)
            {
                UUID = uuid,
                BluePrintType = bpType
            };

            if (cargoVolumeSize > 0)
                bp.Properties.SetProperty("Cargo Volume Size", cargoVolumeSize);
            if (mass > 0)
                bp.Properties.SetProperty("Mass", mass);
            return new ReadOnlyBlueprint(bp);
        }

        private Func<string, ReadOnlyBlueprint> MakeFinder(
            params ReadOnlyBlueprint[] blueprints)
        {
            var dict = new Dictionary<string, ReadOnlyBlueprint>();
            foreach (var bp in blueprints)
                dict[bp.UUID] = bp;
            return uuid =>
            {
                dict.TryGetValue(uuid, out var found);
                return found;
            };
        }
    }
}
