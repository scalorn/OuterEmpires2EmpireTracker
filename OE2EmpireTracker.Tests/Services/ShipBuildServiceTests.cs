using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class ShipBuildServiceTests
    {
        // --- GenerateShipBuildItems ---

        [Test]
        public void GenerateShipBuildItems_NullTemplate_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ShipBuildService.GenerateShipBuildItems(
                    null,
                    1,
                    DestinationType.Station,
                    "s1",
                    id => null,
                    null));
        }

        [Test]
        public void GenerateShipBuildItems_CreatesHullAndComponents()
        {
            var template = CreateTemplate("hull-1", "reactor-1", "drive-1");
            var items = ShipBuildService.GenerateShipBuildItems(
                template,
                1,
                DestinationType.Station,
                "station-1",
                id => new OE2EmpireTracker.Models.Blueprint(id) { UUID = id },
                id => 0);

            Assert.That(items.Count, Is.EqualTo(3)); // hull + 2 components
            Assert.That(items.All(i => i.ItemType == BuildItemType.Manufactory), Is.True);
        }

        [Test]
        public void GenerateShipBuildItems_SkipsInStockItems()
        {
            var template = CreateTemplate("hull-1", "reactor-1");
            var items = ShipBuildService.GenerateShipBuildItems(
                template,
                1,
                DestinationType.Station,
                "station-1",
                id => new OE2EmpireTracker.Models.Blueprint(id) { UUID = id },
                id => id == "reactor-1" ? 5 : 0); // reactor in stock

            Assert.That(items.Count, Is.EqualTo(1)); // only hull
            Assert.That(items[0].BlueprintUUID, Is.EqualTo("hull-1"));
        }

        [Test]
        public void GenerateShipBuildItems_MultipleShips_MultipliesItems()
        {
            var template = CreateTemplate("hull-1", "reactor-1");
            var items = ShipBuildService.GenerateShipBuildItems(
                template,
                3,
                DestinationType.Station,
                "station-1",
                id => new OE2EmpireTracker.Models.Blueprint(id) { UUID = id },
                id => 0);

            Assert.That(items.Count, Is.EqualTo(6)); // 3 x (hull + reactor)
        }

        // --- ValidateAssemblyLocation ---

        [Test]
        public void ValidateAssemblyLocation_Class5_AnyStation_Valid()
        {
            Assert.That(ShipBuildService.ValidateAssemblyLocation(5, StationType.Outpost), Is.Null);
            Assert.That(ShipBuildService.ValidateAssemblyLocation(5, StationType.Station), Is.Null);
            Assert.That(ShipBuildService.ValidateAssemblyLocation(5, StationType.Starbase), Is.Null);
        }

        [Test]
        public void ValidateAssemblyLocation_Class6_OutpostInvalid()
        {
            Assert.That(ShipBuildService.ValidateAssemblyLocation(6, StationType.Outpost), Is.Not.Null);
            Assert.That(ShipBuildService.ValidateAssemblyLocation(6, StationType.Station), Is.Null);
            Assert.That(ShipBuildService.ValidateAssemblyLocation(6, StationType.Starbase), Is.Null);
        }

        [Test]
        public void ValidateAssemblyLocation_Class7_StarbaseOnly()
        {
            Assert.That(ShipBuildService.ValidateAssemblyLocation(7, StationType.Outpost), Is.Not.Null);
            Assert.That(ShipBuildService.ValidateAssemblyLocation(7, StationType.Station), Is.Not.Null);
            Assert.That(ShipBuildService.ValidateAssemblyLocation(7, StationType.Starbase), Is.Null);
        }

        [Test]
        public void ValidateAssemblyLocation_Class8_StarbaseOnly()
        {
            Assert.That(ShipBuildService.ValidateAssemblyLocation(8, StationType.Outpost), Is.Not.Null);
            Assert.That(ShipBuildService.ValidateAssemblyLocation(8, StationType.Station), Is.Not.Null);
            Assert.That(ShipBuildService.ValidateAssemblyLocation(8, StationType.Starbase), Is.Null);
        }

        // --- ComputeStats ---

        [Test]
        public void ComputeStats_NullHull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ShipBuildService.ComputeStats(null, null, id => null));
        }

        [Test]
        public void ComputeStats_HullOnly_ReturnsHullStats()
        {
            var hull = CreateBlueprint("hull-1", "Clipper", new Dictionary<string, decimal>
            {
                { "Mass", 100m },
                { "Health", 500m },
                { "Cargo Capacity", 50m }
            });

            var stats = ShipBuildService.ComputeStats(hull, null, id => null);

            Assert.That(stats.TotalMass, Is.EqualTo(100m));
            Assert.That(stats.TotalHealth, Is.EqualTo(500m));
            Assert.That(stats.CargoCapacity, Is.EqualTo(50m));
        }

        [Test]
        public void ComputeStats_HullPlusComponents_SumsValues()
        {
            var hull = CreateBlueprint("hull-1", "Clipper", new Dictionary<string, decimal>
            {
                { "Mass", 100m },
                { "Health", 500m },
                { "Power Generated", 0m }
            });
            var reactor = CreateBlueprint("reactor-1", "Reactor", new Dictionary<string, decimal>
            {
                { "Mass", 20m },
                { "Power Generated", 200m },
                { "Power Consumed", 0m }
            });

            var components = new List<ShipComponentSlot>
            {
                new ShipComponentSlot { SlotType = "Reactor", BlueprintUUID = "reactor-1" }
            };

            var stats = ShipBuildService.ComputeStats(
                hull,
                components,
                id => id == "reactor-1" ? reactor : null);

            Assert.That(stats.TotalMass, Is.EqualTo(120m));
            Assert.That(stats.PowerGenerated, Is.EqualTo(200m));
            Assert.That(stats.PowerBalance, Is.EqualTo(200m));
        }

        // --- ComputeStationStats ---

        [Test]
        public void ComputeStationStats_NullBlueprint_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ShipBuildService.ComputeStationStats(null, null, id => null));
        }

        [Test]
        public void ComputeStationStats_ReturnsDefenceStats()
        {
            var stationBp = CreateBlueprint("station-bp", "Outpost", new Dictionary<string, decimal>
            {
                { "Health", 1000m },
                { "Energy Defence", 50m }
            });

            var stats = ShipBuildService.ComputeStationStats(stationBp, null, id => null);

            Assert.That(stats.TotalHealth, Is.EqualTo(1000m));
            Assert.That(stats.EnergyDefence, Is.EqualTo(50m));
        }

        private OE2EmpireTracker.Models.Blueprint CreateBlueprint(
            string uuid,
            string name,
            Dictionary<string,
            decimal> props = null)
        {
            var bp = new OE2EmpireTracker.Models.Blueprint(name) { UUID = uuid, Properties = new PropertyBag() };
            if (props != null)
            {
                foreach (var kv in props)
                {
                    bp.Properties.SetProperty(kv.Key, kv.Value);
                }
            }

            return bp;
        }

        private ShipTemplate CreateTemplate(string hullUUID, params string[] componentUUIDs)
        {
            var t = new ShipTemplate
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "TestTemplate",
                HullBlueprintUUID = hullUUID
            };

            int idx = 0;
            foreach (var uuid in componentUUIDs)
            {
                t.Components.Add(new ShipComponentSlot
                {
                    SlotType = "Reactor",
                    SlotIndex = idx++,
                    BlueprintUUID = uuid
                });
            }

            return t;
        }
    }
}
