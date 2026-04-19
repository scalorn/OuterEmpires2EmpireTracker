using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class AutoAssignServiceTests
    {
        #region Helpers

        private Colony CreateColony(string uuid, string name,
            params ColonyStructure[] structures)
        {
            var colony = new Colony
            {
                UUID = uuid,
                ColonyName = name
            };
            colony.Structures.AddRange(structures);
            return colony;
        }

        private ColonyStructure CreateManufactory(string uuid, string flatpackBpUUID,
            bool idle = true)
        {
            var s = new ColonyStructure { UUID = uuid, FlatpackBlueprintUUID = flatpackBpUUID };
            s.Properties.setProperty(GameConstants.PropBuilt, true);
            s.Properties.setProperty(GameConstants.PropOnline, true);
            if (!idle)
                s.ManufacturingBlueprintUUID = "some-bp";
            return s;
        }

        private ColonyStructure CreateCommodityFactory(string uuid, string flatpackBpUUID,
            bool idle = true)
        {
            var s = new ColonyStructure { UUID = uuid, FlatpackBlueprintUUID = flatpackBpUUID };
            s.Properties.setProperty(GameConstants.PropBuilt, true);
            s.Properties.setProperty(GameConstants.PropOnline, true);
            if (!idle)
                s.ManufacturingCommodityName = "some-commodity";
            return s;
        }

        private Blueprint CreateBlueprint(string uuid, string name, string bpType,
            int quantity = 1)
        {
            return new Blueprint(name)
            {
                UUID = uuid,
                BluePrintType = bpType,
                Quantity = quantity
            };
        }

        private DeliveryRoute CreateRoute(params string[] colonyUUIDs)
        {
            var route = new DeliveryRoute
            {
                UUID = "route-1",
                Name = "Test Route"
            };
            int seq = 0;
            foreach (var uuid in colonyUUIDs)
            {
                route.Stops.Add(new RouteStop
                {
                    DestinationType = DestinationType.Colony,
                    DestinationUUID = uuid,
                    ColonyUUID = uuid,
                    Sequence = seq++
                });
            }
            return route;
        }

        private BuildItem CreateManufactoryItem(string bpUUID, int quantity)
        {
            return new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Manufactory,
                BlueprintUUID = bpUUID,
                Quantity = quantity,
                BuildLocationType = DestinationType.Colony,
                BuildLocationUUID = string.Empty,
                StructureUUID = string.Empty
            };
        }

        private BuildItem CreateCommodityItem(string commodityName, int quantity)
        {
            return new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Commodity,
                CommodityName = commodityName,
                Quantity = quantity,
                BuildLocationType = DestinationType.Colony,
                BuildLocationUUID = string.Empty,
                StructureUUID = string.Empty
            };
        }

        private BuildItem CreateAllocatedItem(string bpUUID, int quantity,
            string locationUUID, string structureUUID)
        {
            return new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Manufactory,
                BlueprintUUID = bpUUID,
                Quantity = quantity,
                BuildLocationType = DestinationType.Colony,
                BuildLocationUUID = locationUUID,
                StructureUUID = structureUUID
            };
        }

        #endregion

        #region Null Arguments

        [Test]
        public void ProposeAssignments_NullPlan_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                AutoAssignService.ProposeAssignments(
                    null, new DeliveryRoute(), id => null, id => null, id => null, id => null));
        }

        [Test]
        public void ProposeAssignments_NullRoute_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                AutoAssignService.ProposeAssignments(
                    new BuildPlan(), null, id => null, id => null, id => null, id => null));
        }

        [Test]
        public void ProposeAssignments_NullColonyFinder_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                AutoAssignService.ProposeAssignments(
                    new BuildPlan(), new DeliveryRoute(), null, id => null, id => null, id => null));
        }

        [Test]
        public void ProposeAssignments_NullBlueprintFinder_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                AutoAssignService.ProposeAssignments(
                    new BuildPlan(), new DeliveryRoute(), id => null, id => null, id => null, null));
        }

        #endregion

        #region Empty / No Items

        [Test]
        public void ProposeAssignments_EmptyPlan_ReturnsEmpty()
        {
            var plan = new BuildPlan { UUID = "plan-1", Name = "Empty" };
            var route = CreateRoute("col-1");

            var result = AutoAssignService.ProposeAssignments(
                plan, route, id => null, id => null, id => null, id => null);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ProposeAssignments_AllItemsAllocated_ReturnsEmpty()
        {
            var plan = new BuildPlan { UUID = "plan-1", Name = "Allocated" };
            plan.Items.Add(CreateAllocatedItem("bp-1", 5, "col-1", "struct-1"));

            var route = CreateRoute("col-1");

            var result = AutoAssignService.ProposeAssignments(
                plan, route, id => null, id => null, id => null, id => null);

            Assert.That(result, Is.Empty);
        }

        #endregion

        #region Manufactory Assignment

        [Test]
        public void ProposeAssignments_SingleMfgItem_SingleStructure_AssignsCorrectly()
        {
            var mfgBp = CreateBlueprint("mfg-bp", "Manufactory", BlueprintTypes.Manufactory);
            var itemBp = CreateBlueprint("bp-1", "Reactor", BlueprintTypes.Manufactory, quantity: 2);

            var structure = CreateManufactory("struct-1", "mfg-bp");
            var colony = CreateColony("col-1", "Alpha", structure);
            var route = CreateRoute("col-1");

            var plan = new BuildPlan { UUID = "plan-1", Name = "Test" };
            plan.Items.Add(CreateManufactoryItem("bp-1", 5));

            Func<string, Blueprint> bpFinder = id =>
            {
                if (id == "mfg-bp") return mfgBp;
                if (id == "bp-1") return itemBp;
                return null;
            };

            var result = AutoAssignService.ProposeAssignments(
                plan, route,
                id => id == "col-1" ? colony : null,
                id => null, id => null, bpFinder);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].BuildLocationUUID, Is.EqualTo("col-1"));
            Assert.That(result[0].StructureUUID, Is.EqualTo("struct-1"));
            Assert.That(result[0].SequenceInStructure, Is.EqualTo(0));
        }

        [Test]
        public void ProposeAssignments_TwoMfgItems_TwoStructures_DistributesEvenly()
        {
            var mfgBp = CreateBlueprint("mfg-bp", "Manufactory", BlueprintTypes.Manufactory);
            var itemBp = CreateBlueprint("bp-1", "Reactor", BlueprintTypes.Manufactory, quantity: 5);

            var s1 = CreateManufactory("struct-1", "mfg-bp");
            var s2 = CreateManufactory("struct-2", "mfg-bp");
            var colony = CreateColony("col-1", "Alpha", s1, s2);
            var route = CreateRoute("col-1");

            var plan = new BuildPlan { UUID = "plan-1", Name = "Test" };
            plan.Items.Add(CreateManufactoryItem("bp-1", 5));
            plan.Items.Add(CreateManufactoryItem("bp-1", 3));

            Func<string, Blueprint> bpFinder = id =>
            {
                if (id == "mfg-bp") return mfgBp;
                if (id == "bp-1") return itemBp;
                return null;
            };

            var result = AutoAssignService.ProposeAssignments(
                plan, route,
                id => id == "col-1" ? colony : null,
                id => null, id => null, bpFinder);

            Assert.That(result.Count, Is.EqualTo(2));
            // Items should be on different structures
            var structureUUIDs = result.Select(r => r.StructureUUID).Distinct().ToList();
            Assert.That(structureUUIDs.Count, Is.EqualTo(2));
        }

        [Test]
        public void ProposeAssignments_CopyConstraint_LimitsParallelism()
        {
            // Blueprint has only 1 copy, so only 1 structure can be used
            var mfgBp = CreateBlueprint("mfg-bp", "Manufactory", BlueprintTypes.Manufactory);
            var itemBp = CreateBlueprint("bp-1", "Reactor", BlueprintTypes.Manufactory, quantity: 1);

            var s1 = CreateManufactory("struct-1", "mfg-bp");
            var s2 = CreateManufactory("struct-2", "mfg-bp");
            var colony = CreateColony("col-1", "Alpha", s1, s2);
            var route = CreateRoute("col-1");

            var plan = new BuildPlan { UUID = "plan-1", Name = "Test" };
            plan.Items.Add(CreateManufactoryItem("bp-1", 5));
            plan.Items.Add(CreateManufactoryItem("bp-1", 3));

            Func<string, Blueprint> bpFinder = id =>
            {
                if (id == "mfg-bp") return mfgBp;
                if (id == "bp-1") return itemBp;
                return null;
            };

            var result = AutoAssignService.ProposeAssignments(
                plan, route,
                id => id == "col-1" ? colony : null,
                id => null, id => null, bpFinder);

            Assert.That(result.Count, Is.EqualTo(2));
            // Both items should be on the same structure (copy limit = 1)
            var structureUUIDs = result.Select(r => r.StructureUUID).Distinct().ToList();
            Assert.That(structureUUIDs.Count, Is.EqualTo(1));
            // Second item should have SequenceInStructure = 1
            Assert.That(result[1].SequenceInStructure, Is.EqualTo(1));
        }

        [Test]
        public void ProposeAssignments_BusyStructures_Skipped()
        {
            var mfgBp = CreateBlueprint("mfg-bp", "Manufactory", BlueprintTypes.Manufactory);
            var itemBp = CreateBlueprint("bp-1", "Reactor", BlueprintTypes.Manufactory, quantity: 2);

            var s1 = CreateManufactory("struct-1", "mfg-bp", idle: false);
            var s2 = CreateManufactory("struct-2", "mfg-bp", idle: true);
            var colony = CreateColony("col-1", "Alpha", s1, s2);
            var route = CreateRoute("col-1");

            var plan = new BuildPlan { UUID = "plan-1", Name = "Test" };
            plan.Items.Add(CreateManufactoryItem("bp-1", 5));

            Func<string, Blueprint> bpFinder = id =>
            {
                if (id == "mfg-bp") return mfgBp;
                if (id == "bp-1") return itemBp;
                return null;
            };

            var result = AutoAssignService.ProposeAssignments(
                plan, route,
                id => id == "col-1" ? colony : null,
                id => null, id => null, bpFinder);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].StructureUUID, Is.EqualTo("struct-2"));
        }

        [Test]
        public void ProposeAssignments_NoEligibleStructures_ReturnsEmpty()
        {
            var plan = new BuildPlan { UUID = "plan-1", Name = "Test" };
            plan.Items.Add(CreateManufactoryItem("bp-1", 5));

            var colony = CreateColony("col-1", "Alpha"); // no structures
            var route = CreateRoute("col-1");

            var result = AutoAssignService.ProposeAssignments(
                plan, route,
                id => id == "col-1" ? colony : null,
                id => null, id => null, id => null);

            Assert.That(result, Is.Empty);
        }

        #endregion

        #region Commodity Assignment

        [Test]
        public void ProposeAssignments_CommodityItem_AssignsToFactory()
        {
            var factoryBp = CreateBlueprint("cf-bp", "Agridome Factory",
                "Flatpacks/CommodityFactory/Agridome");

            var structure = CreateCommodityFactory("cf-1", "cf-bp");
            var colony = CreateColony("col-1", "Alpha", structure);
            var route = CreateRoute("col-1");

            var plan = new BuildPlan { UUID = "plan-1", Name = "Test" };
            plan.Items.Add(CreateCommodityItem("Grain", 10));

            var result = AutoAssignService.ProposeAssignments(
                plan, route,
                id => id == "col-1" ? colony : null,
                id => null, id => null,
                id => id == "cf-bp" ? factoryBp : null);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].StructureUUID, Is.EqualTo("cf-1"));
            Assert.That(result[0].BuildLocationUUID, Is.EqualTo("col-1"));
        }

        [Test]
        public void ProposeAssignments_CommodityItems_NoCopyConstraint_DistributesAll()
        {
            var factoryBp = CreateBlueprint("cf-bp", "Agridome Factory",
                "Flatpacks/CommodityFactory/Agridome");

            var s1 = CreateCommodityFactory("cf-1", "cf-bp");
            var s2 = CreateCommodityFactory("cf-2", "cf-bp");
            var colony = CreateColony("col-1", "Alpha", s1, s2);
            var route = CreateRoute("col-1");

            var plan = new BuildPlan { UUID = "plan-1", Name = "Test" };
            plan.Items.Add(CreateCommodityItem("Grain", 10));
            plan.Items.Add(CreateCommodityItem("Grain", 20));

            var result = AutoAssignService.ProposeAssignments(
                plan, route,
                id => id == "col-1" ? colony : null,
                id => null, id => null,
                id => id == "cf-bp" ? factoryBp : null);

            Assert.That(result.Count, Is.EqualTo(2));
            // Should distribute across both factories
            var structureUUIDs = result.Select(r => r.StructureUUID).Distinct().ToList();
            Assert.That(structureUUIDs.Count, Is.EqualTo(2));
        }

        #endregion

        #region Mixed Items

        [Test]
        public void ProposeAssignments_MixedItems_AssignsBothTypes()
        {
            var mfgBp = CreateBlueprint("mfg-bp", "Manufactory", BlueprintTypes.Manufactory);
            var itemBp = CreateBlueprint("bp-1", "Reactor", BlueprintTypes.Manufactory, quantity: 3);
            var factoryBp = CreateBlueprint("cf-bp", "Agridome Factory",
                "Flatpacks/CommodityFactory/Agridome");

            var mfgStruct = CreateManufactory("struct-1", "mfg-bp");
            var cfStruct = CreateCommodityFactory("cf-1", "cf-bp");
            var colony = CreateColony("col-1", "Alpha", mfgStruct, cfStruct);
            var route = CreateRoute("col-1");

            var plan = new BuildPlan { UUID = "plan-1", Name = "Test" };
            plan.Items.Add(CreateManufactoryItem("bp-1", 5));
            plan.Items.Add(CreateCommodityItem("Grain", 10));

            Func<string, Blueprint> bpFinder = id =>
            {
                if (id == "mfg-bp") return mfgBp;
                if (id == "bp-1") return itemBp;
                if (id == "cf-bp") return factoryBp;
                return null;
            };

            var result = AutoAssignService.ProposeAssignments(
                plan, route,
                id => id == "col-1" ? colony : null,
                id => null, id => null, bpFinder);

            Assert.That(result.Count, Is.EqualTo(2));
            var mfgProposal = result.First(r => r.StructureUUID == "struct-1");
            var cfProposal = result.First(r => r.StructureUUID == "cf-1");
            Assert.That(mfgProposal, Is.Not.Null);
            Assert.That(cfProposal, Is.Not.Null);
        }

        #endregion

        #region Multi-Colony Route

        [Test]
        public void ProposeAssignments_MultiColonyRoute_CollectsFromAllStops()
        {
            var mfgBp = CreateBlueprint("mfg-bp", "Manufactory", BlueprintTypes.Manufactory);
            var itemBp = CreateBlueprint("bp-1", "Reactor", BlueprintTypes.Manufactory, quantity: 5);

            var s1 = CreateManufactory("struct-1", "mfg-bp");
            var s2 = CreateManufactory("struct-2", "mfg-bp");
            var colony1 = CreateColony("col-1", "Alpha", s1);
            var colony2 = CreateColony("col-2", "Beta", s2);
            var route = CreateRoute("col-1", "col-2");

            var plan = new BuildPlan { UUID = "plan-1", Name = "Test" };
            plan.Items.Add(CreateManufactoryItem("bp-1", 5));
            plan.Items.Add(CreateManufactoryItem("bp-1", 3));

            Func<string, Colony> colFinder = id =>
            {
                if (id == "col-1") return colony1;
                if (id == "col-2") return colony2;
                return null;
            };

            Func<string, Blueprint> bpFinder = id =>
            {
                if (id == "mfg-bp") return mfgBp;
                if (id == "bp-1") return itemBp;
                return null;
            };

            var result = AutoAssignService.ProposeAssignments(
                plan, route, colFinder, id => null, id => null, bpFinder);

            Assert.That(result.Count, Is.EqualTo(2));
            // Items should be distributed across colonies
            var colonyUUIDs = result.Select(r => r.BuildLocationUUID).Distinct().ToList();
            Assert.That(colonyUUIDs.Count, Is.EqualTo(2));
        }

        #endregion

        #region Edge Cases

        [Test]
        public void ProposeAssignments_UnbuiltStructure_Skipped()
        {
            var mfgBp = CreateBlueprint("mfg-bp", "Manufactory", BlueprintTypes.Manufactory);
            var itemBp = CreateBlueprint("bp-1", "Reactor", BlueprintTypes.Manufactory, quantity: 2);

            // Structure is not built
            var structure = new ColonyStructure
            {
                UUID = "struct-1",
                FlatpackBlueprintUUID = "mfg-bp"
            };
            structure.Properties.setProperty(GameConstants.PropBuilt, false);
            structure.Properties.setProperty(GameConstants.PropOnline, false);

            var colony = CreateColony("col-1", "Alpha", structure);
            var route = CreateRoute("col-1");

            var plan = new BuildPlan { UUID = "plan-1", Name = "Test" };
            plan.Items.Add(CreateManufactoryItem("bp-1", 5));

            Func<string, Blueprint> bpFinder = id =>
            {
                if (id == "mfg-bp") return mfgBp;
                if (id == "bp-1") return itemBp;
                return null;
            };

            var result = AutoAssignService.ProposeAssignments(
                plan, route,
                id => id == "col-1" ? colony : null,
                id => null, id => null, bpFinder);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ProposeAssignments_NonColonyStop_Skipped()
        {
            var route = new DeliveryRoute { UUID = "route-1", Name = "Test" };
            route.Stops.Add(new RouteStop
            {
                DestinationType = DestinationType.Station,
                DestinationUUID = "station-1",
                Sequence = 0
            });

            var plan = new BuildPlan { UUID = "plan-1", Name = "Test" };
            plan.Items.Add(CreateManufactoryItem("bp-1", 5));

            var result = AutoAssignService.ProposeAssignments(
                plan, route, id => null, id => null, id => null, id => null);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ProposeAssignments_StructureStacking_IncrementsSequence()
        {
            var mfgBp = CreateBlueprint("mfg-bp", "Manufactory", BlueprintTypes.Manufactory);
            var itemBp = CreateBlueprint("bp-1", "Reactor", BlueprintTypes.Manufactory, quantity: 1);

            // Only 1 structure, 1 copy ? all items stack
            var structure = CreateManufactory("struct-1", "mfg-bp");
            var colony = CreateColony("col-1", "Alpha", structure);
            var route = CreateRoute("col-1");

            var plan = new BuildPlan { UUID = "plan-1", Name = "Test" };
            plan.Items.Add(CreateManufactoryItem("bp-1", 5));
            plan.Items.Add(CreateManufactoryItem("bp-1", 3));
            plan.Items.Add(CreateManufactoryItem("bp-1", 2));

            Func<string, Blueprint> bpFinder = id =>
            {
                if (id == "mfg-bp") return mfgBp;
                if (id == "bp-1") return itemBp;
                return null;
            };

            var result = AutoAssignService.ProposeAssignments(
                plan, route,
                id => id == "col-1" ? colony : null,
                id => null, id => null, bpFinder);

            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result[0].SequenceInStructure, Is.EqualTo(0));
            Assert.That(result[1].SequenceInStructure, Is.EqualTo(1));
            Assert.That(result[2].SequenceInStructure, Is.EqualTo(2));
            // All on same structure
            Assert.That(result.All(r => r.StructureUUID == "struct-1"), Is.True);
        }

        [Test]
        public void ProposeAssignments_ProposalHasCorrectBuildLocationType()
        {
            var mfgBp = CreateBlueprint("mfg-bp", "Manufactory", BlueprintTypes.Manufactory);
            var itemBp = CreateBlueprint("bp-1", "Reactor", BlueprintTypes.Manufactory, quantity: 2);

            var structure = CreateManufactory("struct-1", "mfg-bp");
            var colony = CreateColony("col-1", "Alpha", structure);
            var route = CreateRoute("col-1");

            var plan = new BuildPlan { UUID = "plan-1", Name = "Test" };
            plan.Items.Add(CreateManufactoryItem("bp-1", 5));

            Func<string, Blueprint> bpFinder = id =>
            {
                if (id == "mfg-bp") return mfgBp;
                if (id == "bp-1") return itemBp;
                return null;
            };

            var result = AutoAssignService.ProposeAssignments(
                plan, route,
                id => id == "col-1" ? colony : null,
                id => null, id => null, bpFinder);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].BuildLocationType, Is.EqualTo(DestinationType.Colony));
        }

        #endregion
    }
}
