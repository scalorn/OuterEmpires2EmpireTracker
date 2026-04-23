using System;
using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class BuildPlanServiceTests
    {
        [Test]
        public void ValidatePlanName_ValidName_ReturnsTrue()
        {
            Assert.That(BuildPlanService.ValidatePlanName("My Build Plan"), Is.True);
        }

        [Test]
        public void ValidatePlanName_Null_ReturnsFalse()
        {
            Assert.That(BuildPlanService.ValidatePlanName(null), Is.False);
        }

        [Test]
        public void ValidatePlanName_Empty_ReturnsFalse()
        {
            Assert.That(BuildPlanService.ValidatePlanName(string.Empty), Is.False);
        }

        [Test]
        public void ValidatePlanName_WhitespaceOnly_ReturnsFalse()
        {
            Assert.That(BuildPlanService.ValidatePlanName("   "), Is.False);
        }

        [Test]
        public void ValidatePlanName_TabsOnly_ReturnsFalse()
        {
            Assert.That(BuildPlanService.ValidatePlanName("\t\t"), Is.False);
        }

        [Test]
        public void ValidatePlanName_SingleChar_ReturnsTrue()
        {
            Assert.That(BuildPlanService.ValidatePlanName("A"), Is.True);
        }

        [Test]
        public void ValidateBuildItem_Null_ReturnsFalse()
        {
            Assert.That(BuildPlanService.ValidateBuildItem(null), Is.False);
        }

        [Test]
        public void ValidateBuildItem_ZeroQuantity_ReturnsFalse()
        {
            var item = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Manufactory,
                BlueprintUUID = "bp-1",
                Quantity = 0
            };

            Assert.That(BuildPlanService.ValidateBuildItem(item), Is.False);
        }

        [Test]
        public void ValidateBuildItem_NegativeQuantity_ReturnsFalse()
        {
            var item = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Manufactory,
                BlueprintUUID = "bp-1",
                Quantity = -5
            };

            Assert.That(BuildPlanService.ValidateBuildItem(item), Is.False);
        }

        [Test]
        public void ValidateBuildItem_ValidManufactory_ReturnsTrue()
        {
            var item = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Manufactory,
                BlueprintUUID = "bp-1",
                Quantity = 3
            };

            Assert.That(BuildPlanService.ValidateBuildItem(item), Is.True);
        }

        [Test]
        public void ValidateBuildItem_ManufactoryMissingBlueprint_ReturnsFalse()
        {
            var item = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Manufactory,
                Quantity = 1
            };

            Assert.That(BuildPlanService.ValidateBuildItem(item), Is.False);
        }

        [Test]
        public void ValidateBuildItem_ValidCommodity_ReturnsTrue()
        {
            var item = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Commodity,
                CommodityName = "Electronics",
                Quantity = 10
            };

            Assert.That(BuildPlanService.ValidateBuildItem(item), Is.True);
        }

        [Test]
        public void ValidateBuildItem_CommodityMissingName_ReturnsFalse()
        {
            var item = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Commodity,
                Quantity = 1
            };

            Assert.That(BuildPlanService.ValidateBuildItem(item), Is.False);
        }

        [Test]
        public void ValidateBuildItem_ValidShipTemplate_ReturnsTrue()
        {
            var item = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.ShipTemplate,
                ShipTemplateUUID = "st-1",
                Quantity = 2
            };

            Assert.That(BuildPlanService.ValidateBuildItem(item), Is.True);
        }

        [Test]
        public void ValidateBuildItem_ShipTemplateMissingUUID_ReturnsFalse()
        {
            var item = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.ShipTemplate,
                Quantity = 1
            };

            Assert.That(BuildPlanService.ValidateBuildItem(item), Is.False);
        }

        [Test]
        public void ValidateBuildItem_ValidMining_ReturnsTrue()
        {
            var item = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Mining,
                MiningResource = "Iron",
                Quantity = 1
            };

            Assert.That(BuildPlanService.ValidateBuildItem(item), Is.True);
        }

        [Test]
        public void ValidateBuildItem_MiningMissingResource_ReturnsFalse()
        {
            var item = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Mining,
                Quantity = 1
            };

            Assert.That(BuildPlanService.ValidateBuildItem(item), Is.False);
        }

        [Test]
        public void ValidateBuildItem_ValidRefining_ReturnsTrue()
        {
            var item = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Refining,
                RefiningResource = "Iron",
                Quantity = 1
            };

            Assert.That(BuildPlanService.ValidateBuildItem(item), Is.True);
        }

        [Test]
        public void ValidateBuildItem_RefiningMissingResource_ReturnsFalse()
        {
            var item = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Refining,
                Quantity = 1
            };

            Assert.That(BuildPlanService.ValidateBuildItem(item), Is.False);
        }

        [Test]
        public void ValidateBuildItem_ValidResearch_ReturnsTrue()
        {
            var item = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Research,
                BlueprintUUID = "bp-research",
                Quantity = 1
            };

            Assert.That(BuildPlanService.ValidateBuildItem(item), Is.True);
        }

        [Test]
        public void ValidateBuildItem_ResearchMissingBlueprint_ReturnsFalse()
        {
            var item = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Research,
                Quantity = 1
            };

            Assert.That(BuildPlanService.ValidateBuildItem(item), Is.False);
        }

        private Colony CreateTestColony(string name, params ColonyStructure[] structures)
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                ColonyName = name
            };

            colony.Structures.AddRange(structures);
            return colony;
        }

        private ColonyStructure CreateStructure(string flatpackUUID, bool staged, bool built)
        {
            var s = new ColonyStructure
            {
                UUID = Guid.NewGuid().ToString(),
                FlatpackBlueprintUUID = flatpackUUID
            };

            if (staged) s.Properties.SetProperty(GameConstants.PropStaged, true);
            if (built) s.Properties.SetProperty(GameConstants.PropBuilt, true);
            return s;
        }

        private OE2EmpireTracker.Models.Blueprint CreateBlueprint(string uuid, string name)
        {
            return new OE2EmpireTracker.Models.Blueprint(name) { UUID = uuid };
        }

        [Test]
        public void GenerateColonyBuildItems_NullColony_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                BuildPlanService.GenerateColonyBuildItems(null, new BuildPlan(), id => null));
        }

        [Test]
        public void GenerateColonyBuildItems_NullPlan_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                BuildPlanService.GenerateColonyBuildItems(new Colony(), null, id => null));
        }

        [Test]
        public void GenerateColonyBuildItems_NullFinder_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                BuildPlanService.GenerateColonyBuildItems(new Colony(), new BuildPlan(), null));
        }

        [Test]
        public void GenerateColonyBuildItems_UnstagedStructures_CreatesItems()
        {
            var bp1 = CreateBlueprint("bp-1", "Reactor Flatpack");
            var bp2 = CreateBlueprint("bp-2", "Refinery Flatpack");
            var blueprints = new Dictionary<string, OE2EmpireTracker.Models.Blueprint>
            {
                { bp1.UUID, bp1 },
                { bp2.UUID, bp2 }
            };

            var colony = CreateTestColony(
                "Alpha",
                CreateStructure("bp-1", false, false),
                CreateStructure("bp-2", false, false));

            var plan = new BuildPlan { UUID = Guid.NewGuid().ToString() };

            int added = BuildPlanService.GenerateColonyBuildItems(
                colony, plan, id => blueprints.ContainsKey(id) ? blueprints[id] : null);

            Assert.That(added, Is.EqualTo(2));
            Assert.That(plan.Items.Count, Is.EqualTo(2));
            Assert.That(plan.Items[0].ItemType, Is.EqualTo(BuildItemType.Manufactory));
            Assert.That(plan.Items[0].BlueprintUUID, Is.EqualTo("bp-1"));
            Assert.That(plan.Items[0].ItemName, Is.EqualTo("Reactor Flatpack"));
            Assert.That(plan.Items[0].Quantity, Is.EqualTo(1));
            Assert.That(plan.Items[0].Status, Is.EqualTo(BuildItemStatus.Staged));
            Assert.That(plan.Items[0].BuildLocationUUID, Is.EqualTo(string.Empty));
            Assert.That(plan.Items[0].Notes, Does.Contain(colony.UUID));
        }

        [Test]
        public void GenerateColonyBuildItems_StagedStructure_Skipped()
        {
            var colony = CreateTestColony(
                "Beta",
                CreateStructure("bp-1", true, false));

            var plan = new BuildPlan { UUID = Guid.NewGuid().ToString() };

            int added = BuildPlanService.GenerateColonyBuildItems(
                colony, plan, id => null);

            Assert.That(added, Is.EqualTo(0));
            Assert.That(plan.Items.Count, Is.EqualTo(0));
        }

        [Test]
        public void GenerateColonyBuildItems_BuiltStructure_Skipped()
        {
            var colony = CreateTestColony(
                "Gamma",
                CreateStructure("bp-1", false, true));

            var plan = new BuildPlan { UUID = Guid.NewGuid().ToString() };

            int added = BuildPlanService.GenerateColonyBuildItems(
                colony, plan, id => null);

            Assert.That(added, Is.EqualTo(0));
            Assert.That(plan.Items.Count, Is.EqualTo(0));
        }

        [Test]
        public void GenerateColonyBuildItems_DuplicateBlueprint_SkippedOnSecondRun()
        {
            var bp1 = CreateBlueprint("bp-1", "Reactor Flatpack");
            var colony = CreateTestColony(
                "Delta",
                CreateStructure("bp-1", false, false));

            var plan = new BuildPlan { UUID = Guid.NewGuid().ToString() };

            // First run adds the item
            int added1 = BuildPlanService.GenerateColonyBuildItems(
                colony, plan, id => bp1);
            Assert.That(added1, Is.EqualTo(1));

            // Second run skips it (dedup by blueprint + colony UUID in Notes)
            int added2 = BuildPlanService.GenerateColonyBuildItems(
                colony, plan, id => bp1);
            Assert.That(added2, Is.EqualTo(0));
            Assert.That(plan.Items.Count, Is.EqualTo(1));
        }

        [Test]
        public void GenerateColonyBuildItems_NoFlatpackUUID_Skipped()
        {
            var structure = new ColonyStructure
            {
                UUID = Guid.NewGuid().ToString(),
                FlatpackBlueprintUUID = null
            };

            var colony = CreateTestColony("Epsilon", structure);
            var plan = new BuildPlan { UUID = Guid.NewGuid().ToString() };

            int added = BuildPlanService.GenerateColonyBuildItems(
                colony, plan, id => null);

            Assert.That(added, Is.EqualTo(0));
        }

        [Test]
        public void GenerateColonyBuildItems_BlueprintNotFound_UsesUnknownName()
        {
            var colony = CreateTestColony(
                "Zeta",
                CreateStructure("bp-missing", false, false));

            var plan = new BuildPlan { UUID = Guid.NewGuid().ToString() };

            int added = BuildPlanService.GenerateColonyBuildItems(
                colony, plan, id => null);

            Assert.That(added, Is.EqualTo(1));
            Assert.That(plan.Items[0].ItemName, Is.EqualTo("Unknown Blueprint"));
        }

        [Test]
        public void GenerateColonyBuildItems_MixedStructures_OnlyUnstagedUnbuilt()
        {
            var bp = CreateBlueprint("bp-1", "Flatpack A");
            var colony = CreateTestColony(
                "Eta",
                CreateStructure("bp-1", false, false),
                CreateStructure("bp-2", true, false),
                CreateStructure("bp-3", false, true),
                CreateStructure("bp-4", true, true));    // both -> skipped

            var plan = new BuildPlan { UUID = Guid.NewGuid().ToString() };

            int added = BuildPlanService.GenerateColonyBuildItems(
                colony, plan, id => id == "bp-1" ? bp : null);

            Assert.That(added, Is.EqualTo(1));
            Assert.That(plan.Items[0].BlueprintUUID, Is.EqualTo("bp-1"));
        }

        [Test]
        public void GenerateColonyBuildItems_EmptyColony_ReturnsZero()
        {
            var colony = CreateTestColony("Theta");
            var plan = new BuildPlan { UUID = Guid.NewGuid().ToString() };

            int added = BuildPlanService.GenerateColonyBuildItems(
                colony, plan, id => null);

            Assert.That(added, Is.EqualTo(0));
        }
    }
}
