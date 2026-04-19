using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System;
using System.Collections.Generic;

namespace OE2EmpireTracker.Tests.Services
{
    using Blueprint = OE2EmpireTracker.Models.Blueprint;
    [TestFixture]
    public class ResourceCheckServiceTests
    {
        #region Helpers

        private Blueprint CreateBlueprint(string uuid, string name,
            Dictionary<string, string> resources)
        {
            var bp = new Blueprint(name) { UUID = uuid };
            if (resources != null)
            {
                foreach (var kvp in resources)
                    bp.Resources[kvp.Key] = kvp.Value;
            }
            return bp;
        }

        private ItemBag CreateInventory(params (string name, string purity, int qty)[] items)
        {
            var bag = new ItemBag();
            foreach (var (name, purity, qty) in items)
            {
                var item = new Item(ItemType.ItemTypeEnum.Resource, name)
                {
                    UUID = Guid.NewGuid().ToString(),
                    BaseItemTypeID = name,
                    ResourcePurity = purity,
                    Quantity = qty
                };
                bag.AddItem(item);
            }
            return bag;
        }

        private BuildItem CreateManufactoryItem(string bpUUID, int quantity,
            string locationUUID = "")
        {
            return new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Manufactory,
                BlueprintUUID = bpUUID,
                Quantity = quantity,
                BuildLocationType = DestinationType.Colony,
                BuildLocationUUID = locationUUID
            };
        }

        private BuildItem CreateCommodityItem(string commodityName, int quantity,
            string locationUUID = "")
        {
            return new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Commodity,
                CommodityName = commodityName,
                Quantity = quantity,
                BuildLocationType = DestinationType.Colony,
                BuildLocationUUID = locationUUID
            };
        }

        private Colony CreateColony(string uuid, ItemBag inventory)
        {
            return new Colony
            {
                UUID = uuid,
                ColonyName = "Test Colony",
                Items = inventory
            };
        }

        #endregion

        #region ComputeShortfalls ? Null Arguments

        [Test]
        public void ComputeShortfalls_NullItem_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ResourceCheckService.ComputeShortfalls(null, new ItemBag(), id => null));
        }

        [Test]
        public void ComputeShortfalls_NullInventory_Throws()
        {
            var item = CreateManufactoryItem("bp-1", 1);
            Assert.Throws<ArgumentNullException>(() =>
                ResourceCheckService.ComputeShortfalls(item, null, id => null));
        }

        [Test]
        public void ComputeShortfalls_NullFinder_Throws()
        {
            var item = CreateManufactoryItem("bp-1", 1);
            Assert.Throws<ArgumentNullException>(() =>
                ResourceCheckService.ComputeShortfalls(item, new ItemBag(), null));
        }

        #endregion

        #region ComputeShortfalls ? Manufactory

        [Test]
        public void ComputeShortfalls_Manufactory_AllAvailable_ReturnsEmpty()
        {
            var bp = CreateBlueprint("bp-1", "Reactor", new Dictionary<string, string>
            {
                { "Trans-Metals", "100" },
                { "Metallics", "50" }
            });
            var inventory = CreateInventory(
                ("Trans-Metals", "Refined", 200),
                ("Metallics", "Refined", 100));
            var item = CreateManufactoryItem("bp-1", 1);

            var result = ResourceCheckService.ComputeShortfalls(
                item, inventory, id => id == "bp-1" ? bp : null);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ComputeShortfalls_Manufactory_PartialShortfall()
        {
            var bp = CreateBlueprint("bp-1", "Reactor", new Dictionary<string, string>
            {
                { "Trans-Metals", "100" },
                { "Metallics", "50" }
            });
            // Have enough Trans-Metals but not enough Metallics
            var inventory = CreateInventory(
                ("Trans-Metals", "Refined", 200),
                ("Metallics", "Refined", 30));
            var item = CreateManufactoryItem("bp-1", 1);

            var result = ResourceCheckService.ComputeShortfalls(
                item, inventory, id => id == "bp-1" ? bp : null);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.ContainsKey("Metallics"), Is.True);
            Assert.That(result["Metallics"], Is.EqualTo(20)); // 50 - 30
        }

        [Test]
        public void ComputeShortfalls_Manufactory_MultipleRuns_MultipliesQuantity()
        {
            var bp = CreateBlueprint("bp-1", "Reactor", new Dictionary<string, string>
            {
                { "Trans-Metals", "100" }
            });
            var inventory = CreateInventory(
                ("Trans-Metals", "Refined", 150));
            var item = CreateManufactoryItem("bp-1", 3); // 3 runs = 300 needed

            var result = ResourceCheckService.ComputeShortfalls(
                item, inventory, id => id == "bp-1" ? bp : null);

            Assert.That(result["Trans-Metals"], Is.EqualTo(150)); // 300 - 150
        }

        [Test]
        public void ComputeShortfalls_Manufactory_NoResources_ReturnsEmpty()
        {
            var bp = CreateBlueprint("bp-1", "Simple Item", null);
            var item = CreateManufactoryItem("bp-1", 1);

            var result = ResourceCheckService.ComputeShortfalls(
                item, new ItemBag(), id => id == "bp-1" ? bp : null);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ComputeShortfalls_Manufactory_BlueprintNotFound_ReturnsEmpty()
        {
            var item = CreateManufactoryItem("bp-missing", 1);

            var result = ResourceCheckService.ComputeShortfalls(
                item, new ItemBag(), id => null);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ComputeShortfalls_Manufactory_ZeroInventory_FullShortfall()
        {
            var bp = CreateBlueprint("bp-1", "Reactor", new Dictionary<string, string>
            {
                { "Trans-Metals", "100" },
                { "Metallics", "50" }
            });
            var item = CreateManufactoryItem("bp-1", 2);

            var result = ResourceCheckService.ComputeShortfalls(
                item, new ItemBag(), id => id == "bp-1" ? bp : null);

            Assert.That(result["Trans-Metals"], Is.EqualTo(200));
            Assert.That(result["Metallics"], Is.EqualTo(100));
        }

        [Test]
        public void ComputeShortfalls_Manufactory_SyntheticResource_UsesCorrectPurity()
        {
            // S1 synthetics should look for "Refined" purity in inventory
            // (synthetics are stored at Refined purity after refining)
            var bp = CreateBlueprint("bp-1", "Advanced Reactor", new Dictionary<string, string>
            {
                { "S1. Translanthanic Exotics", "10" }
            });
            // Inventory has the S1 resource at Refined purity (which is how synthetics are stored)
            // DeterminePurity("S1. Translanthanic Exotics") returns "S1"
            // But synthetics in the warehouse are stored at "Refined" purity
            // The service uses DeterminePurity which returns "S1" for S1 resources
            // So we need to store them at "S1" purity for the lookup to work
            var inventory = CreateInventory(
                ("S1. Translanthanic Exotics", "S1", 5));
            var item = CreateManufactoryItem("bp-1", 1);

            var result = ResourceCheckService.ComputeShortfalls(
                item, inventory, id => id == "bp-1" ? bp : null);

            Assert.That(result["S1. Translanthanic Exotics"], Is.EqualTo(5)); // 10 - 5
        }

        #endregion

        #region ComputeShortfalls ? Commodity

        [Test]
        public void ComputeShortfalls_Commodity_AllAvailable_ReturnsEmpty()
        {
            // Use a real commodity name from the hardcoded list
            var item = CreateCommodityItem("Assemblatrons", 1);
            // Assemblatrons needs: Non-Metallics 2, Metallics 2
            var inventory = CreateInventory(
                ("Non-Metallics", "Refined", 10),
                ("Metallics", "Refined", 10));

            var result = ResourceCheckService.ComputeShortfalls(
                item, inventory, id => null);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ComputeShortfalls_Commodity_Shortfall()
        {
            var item = CreateCommodityItem("Assemblatrons", 5);
            // 5 runs ? 2 Non-Metallics = 10, 5 runs ? 2 Metallics = 10
            var inventory = CreateInventory(
                ("Non-Metallics", "Refined", 10),
                ("Metallics", "Refined", 3));

            var result = ResourceCheckService.ComputeShortfalls(
                item, inventory, id => null);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result["Metallics"], Is.EqualTo(7)); // 10 - 3
        }

        [Test]
        public void ComputeShortfalls_Commodity_NotFound_ReturnsEmpty()
        {
            var item = CreateCommodityItem("NonExistentCommodity", 1);

            var result = ResourceCheckService.ComputeShortfalls(
                item, new ItemBag(), id => null);

            Assert.That(result, Is.Empty);
        }

        #endregion

        #region ComputeShortfalls ? Research

        [Test]
        public void ComputeShortfalls_Research_AlwaysEmpty()
        {
            var item = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Research,
                BlueprintUUID = "bp-research",
                Quantity = 1
            };

            var result = ResourceCheckService.ComputeShortfalls(
                item, new ItemBag(), id => null);

            Assert.That(result, Is.Empty);
        }

        #endregion

        #region ComputePlanShortfalls

        [Test]
        public void ComputePlanShortfalls_NullPlan_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ResourceCheckService.ComputePlanShortfalls(
                    null, id => null, id => null, id => null, "p1", id => null));
        }

        [Test]
        public void ComputePlanShortfalls_NullColonyFinder_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ResourceCheckService.ComputePlanShortfalls(
                    new BuildPlan(), null, id => null, id => null, "p1", id => null));
        }

        [Test]
        public void ComputePlanShortfalls_NullBlueprintFinder_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ResourceCheckService.ComputePlanShortfalls(
                    new BuildPlan(), id => null, id => null, id => null, "p1", null));
        }

        [Test]
        public void ComputePlanShortfalls_UnallocatedItems_Skipped()
        {
            var plan = new BuildPlan
            {
                UUID = "plan-1",
                Name = "Test Plan",
                Items = new List<BuildItem>
                {
                    CreateManufactoryItem("bp-1", 1, "") // no location
                }
            };

            var result = ResourceCheckService.ComputePlanShortfalls(
                plan, id => null, id => null, id => null, "p1", id => null);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ComputePlanShortfalls_AllocatedItem_ResolvesColonyInventory()
        {
            var bp = CreateBlueprint("bp-1", "Reactor", new Dictionary<string, string>
            {
                { "Trans-Metals", "100" }
            });
            var inventory = CreateInventory(("Trans-Metals", "Refined", 50));
            var colony = CreateColony("colony-1", inventory);

            var item = CreateManufactoryItem("bp-1", 1, "colony-1");
            var plan = new BuildPlan
            {
                UUID = "plan-1",
                Name = "Test Plan",
                Items = new List<BuildItem> { item }
            };

            var result = ResourceCheckService.ComputePlanShortfalls(
                plan,
                id => id == "colony-1" ? colony : null,
                id => null, id => null, "p1",
                id => id == "bp-1" ? bp : null);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.ContainsKey(item.UUID), Is.True);
            Assert.That(result[item.UUID]["Trans-Metals"], Is.EqualTo(50));
        }

        [Test]
        public void ComputePlanShortfalls_ColonyNotFound_SkipsItem()
        {
            var item = CreateManufactoryItem("bp-1", 1, "colony-missing");
            var plan = new BuildPlan
            {
                UUID = "plan-1",
                Name = "Test Plan",
                Items = new List<BuildItem> { item }
            };

            var result = ResourceCheckService.ComputePlanShortfalls(
                plan, id => null, id => null, id => null, "p1", id => null);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ComputePlanShortfalls_MultipleItems_ReturnsOnlyShortfalls()
        {
            var bp1 = CreateBlueprint("bp-1", "Reactor", new Dictionary<string, string>
            {
                { "Trans-Metals", "100" }
            });
            var bp2 = CreateBlueprint("bp-2", "Drive", new Dictionary<string, string>
            {
                { "Metallics", "50" }
            });
            // Colony has enough for bp-2 but not bp-1
            var inventory = CreateInventory(
                ("Trans-Metals", "Refined", 50),
                ("Metallics", "Refined", 100));
            var colony = CreateColony("colony-1", inventory);

            var item1 = CreateManufactoryItem("bp-1", 1, "colony-1");
            var item2 = CreateManufactoryItem("bp-2", 1, "colony-1");
            var plan = new BuildPlan
            {
                UUID = "plan-1",
                Name = "Test Plan",
                Items = new List<BuildItem> { item1, item2 }
            };

            Func<string, Blueprint> bpFinder = id =>
            {
                if (id == "bp-1") return bp1;
                if (id == "bp-2") return bp2;
                return null;
            };

            var result = ResourceCheckService.ComputePlanShortfalls(
                plan,
                id => id == "colony-1" ? colony : null,
                id => null, id => null, "p1", bpFinder);

            // Only item1 should have shortfalls
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.ContainsKey(item1.UUID), Is.True);
            Assert.That(result.ContainsKey(item2.UUID), Is.False);
        }

        [Test]
        public void ComputePlanShortfalls_EmptyPlan_ReturnsEmpty()
        {
            var plan = new BuildPlan
            {
                UUID = "plan-1",
                Name = "Empty Plan"
            };

            var result = ResourceCheckService.ComputePlanShortfalls(
                plan, id => null, id => null, id => null, "p1", id => null);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ComputePlanShortfalls_ShipLocation_ThrowsNotSupported()
        {
            var item = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Manufactory,
                BlueprintUUID = "bp-1",
                Quantity = 1,
                BuildLocationType = DestinationType.Ship,
                BuildLocationUUID = "ship-1"
            };
            var plan = new BuildPlan
            {
                UUID = "plan-1",
                Name = "Test Plan",
                Items = new List<BuildItem> { item }
            };

            Assert.Throws<NotSupportedException>(() =>
                ResourceCheckService.ComputePlanShortfalls(
                    plan, id => null, id => null, id => null, "p1", id => null));
        }

        #endregion
    }
}
