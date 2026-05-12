using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Interfaces;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Tests;

namespace OE2EmpireTracker.Tests.Models
{
    [TestFixture]
    public class ColonyProcessingTests
    {
        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            TestHelper.SetEmpireFilePath();
            EmpireContext.Reset();
        }

        // -----------------------------------------------------------------------
        // ProcessCommodityFactory -- single cycle produces 10 commodities
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessCommodityFactory_SingleCycle_Produces10Commodities()
        {
            // "Advanced Biolubricants" requires: Alkali Organics x2, Strong Acidic Inorganics x2
            var colony = MakeCommodityFactoryColony(
                "Advanced Biolubricants",
                manufacturingQuantity: 5,
                manufacturingCompleted: 0,
                intervalsPassed: 1,
                warehouseResources: new Dictionary<string, int>
                {
                    { "Alkali Organics", 100 },
                    { "Strong Acidic Inorganics", 100 }
                });

            colony.ProcessColony(MakeContext());

            var commodities = colony.Items.FindByType(ItemType.ItemTypeEnum.Commodity, "Advanced Biolubricants");
            Assert.That(commodities.Count, Is.EqualTo(1));
            Assert.That(commodities[0].Quantity, Is.EqualTo(GameConstants.CommoditiesPerCycle));
        }

        // -----------------------------------------------------------------------
        // ProcessCommodityFactory -- multi-cycle produces correct total
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessCommodityFactory_ThreeCycles_Produces30Commodities()
        {
            var colony = MakeCommodityFactoryColony(
                "Advanced Biolubricants",
                manufacturingQuantity: 5,
                manufacturingCompleted: 0,
                intervalsPassed: 3,
                warehouseResources: new Dictionary<string, int>
                {
                    { "Alkali Organics", 100 },
                    { "Strong Acidic Inorganics", 100 }
                });

            colony.ProcessColony(MakeContext());

            var commodities = colony.Items.FindByType(ItemType.ItemTypeEnum.Commodity, "Advanced Biolubricants");
            Assert.That(commodities.Count, Is.EqualTo(1));
            Assert.That(commodities[0].Quantity, Is.EqualTo(3 * GameConstants.CommoditiesPerCycle));
        }

        // -----------------------------------------------------------------------
        // ProcessCommodityFactory -- commodities stack with existing items
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessCommodityFactory_StacksWithExistingCommodities()
        {
            var colony = MakeCommodityFactoryColony(
                "Advanced Biolubricants",
                manufacturingQuantity: 5,
                manufacturingCompleted: 0,
                intervalsPassed: 1,
                warehouseResources: new Dictionary<string, int>
                {
                    { "Alkali Organics", 100 },
                    { "Strong Acidic Inorganics", 100 }
                });

            // Pre-add 5 existing commodities
            var existing = new Item(ItemType.ItemTypeEnum.Commodity, "Advanced Biolubricants");
            existing.UUID = Guid.NewGuid().ToString();
            existing.BaseItemTypeID = "Advanced Biolubricants";
            existing.Quantity = 5;
            existing.Volume = 10;
            colony.Items.AddItem(existing);

            colony.ProcessColony(MakeContext());

            var commodities = colony.Items.FindByType(ItemType.ItemTypeEnum.Commodity, "Advanced Biolubricants");
            Assert.That(commodities.Count, Is.EqualTo(1));
            Assert.That(commodities[0].Quantity, Is.EqualTo(5 + GameConstants.CommoditiesPerCycle));
        }

        // -----------------------------------------------------------------------
        // ProcessCommodityFactory -- construction resources consumed per cycle
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessCommodityFactory_ConsumesResourcesPerCycle()
        {
            // Advanced Biolubricants: Alkali Organics x2, Strong Acidic Inorganics x2 per cycle
            var colony = MakeCommodityFactoryColony(
                "Advanced Biolubricants",
                manufacturingQuantity: 5,
                manufacturingCompleted: 0,
                intervalsPassed: 1,
                warehouseResources: new Dictionary<string, int>
                {
                    { "Alkali Organics", 100 },
                    { "Strong Acidic Inorganics", 100 }
                });

            colony.ProcessColony(MakeContext());

            var alkali = colony.Items.FindResource("Alkali Organics", GameConstants.PurityRefined);
            Assert.That(alkali.Count, Is.EqualTo(1));
            Assert.That(alkali[0].Quantity, Is.EqualTo(98)); // 100 - 2

            var acidic = colony.Items.FindResource("Strong Acidic Inorganics", GameConstants.PurityRefined);
            Assert.That(acidic.Count, Is.EqualTo(1));
            Assert.That(acidic[0].Quantity, Is.EqualTo(98)); // 100 - 2
        }

        // -----------------------------------------------------------------------
        // ProcessCommodityFactory -- resources depleted to 0 are removed
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessCommodityFactory_DepletedResources_RemovedFromWarehouse()
        {
            // Give exactly 2 of each resource -- one cycle will deplete them to 0
            var colony = MakeCommodityFactoryColony(
                "Advanced Biolubricants",
                manufacturingQuantity: 5,
                manufacturingCompleted: 0,
                intervalsPassed: 1,
                warehouseResources: new Dictionary<string, int>
                {
                    { "Alkali Organics", 2 },
                    { "Strong Acidic Inorganics", 2 }
                });

            colony.ProcessColony(MakeContext());

            var alkali = colony.Items.FindResource("Alkali Organics", GameConstants.PurityRefined);
            Assert.That(
                alkali.Count,
                Is.EqualTo(0),
                "Depleted resource should be removed from warehouse");

            var acidic = colony.Items.FindResource("Strong Acidic Inorganics", GameConstants.PurityRefined);
            Assert.That(
                acidic.Count,
                Is.EqualTo(0),
                "Depleted resource should be removed from warehouse");
        }

        // -----------------------------------------------------------------------
        // ProcessCommodityFactory -- ManufacturingCompleted increments per cycle
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessCommodityFactory_IncrementsManufacturingCompleted()
        {
            var colony = MakeCommodityFactoryColony(
                "Advanced Biolubricants",
                manufacturingQuantity: 5,
                manufacturingCompleted: 0,
                intervalsPassed: 2,
                warehouseResources: new Dictionary<string, int>
                {
                    { "Alkali Organics", 100 },
                    { "Strong Acidic Inorganics", 100 }
                });

            colony.ProcessColony(MakeContext());

            Assert.That(colony.Structures[0].ManufacturingCompleted, Is.EqualTo(2));
        }

        // -----------------------------------------------------------------------
        // ProcessCommodityFactory -- does nothing when ManufacturingCommodityName is empty
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessCommodityFactory_EmptyCommodityName_DoesNothing()
        {
            var colony = MakeCommodityFactoryColony(
                string.Empty,
                manufacturingQuantity: 5,
                manufacturingCompleted: 0,
                intervalsPassed: 1,
                warehouseResources: new Dictionary<string, int>
                {
                    { "Alkali Organics", 100 }
                });

            colony.ProcessColony(MakeContext());

            // No commodities should be produced
            Assert.That(colony.Structures[0].ManufacturingCompleted, Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // ProcessCommodityFactory -- does nothing when commodity not found
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessCommodityFactory_UnknownCommodity_DoesNothing()
        {
            var colony = MakeCommodityFactoryColony(
                "NonExistentCommodityXYZ",
                manufacturingQuantity: 5,
                manufacturingCompleted: 0,
                intervalsPassed: 1,
                warehouseResources: new Dictionary<string, int>
                {
                    { "Alkali Organics", 100 }
                });

            colony.ProcessColony(MakeContext());

            Assert.That(colony.Structures[0].ManufacturingCompleted, Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // LockCommodityFactoryResources
        // -----------------------------------------------------------------------

        [Test]
        public void LockCommodityFactoryResources_LocksCorrectQuantity()
        {
            // Advanced Biolubricants: Alkali Organics x2, Strong Acidic Inorganics x2 per cycle
            // 3 remaining cycles, lock (3-1)=2 future cycles => lock 4 of each
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();

            var bp = new OE2EmpireTracker.Models.Blueprint("TestCF");
            bp.UUID = Guid.NewGuid().ToString();
            bp.BluePrintType = BlueprintTypes.CommodityFactoryPrefix + "Agridome";
            PlayerContext.GetInstance().AddBlueprint(bp);

            var structure = new ColonyStructure();
            structure.UUID = Guid.NewGuid().ToString();
            structure.FlatpackBlueprintUUID = bp.UUID;
            structure.ManufacturingCommodityName = "Advanced Biolubricants";
            structure.ManufacturingQuantity = 5;
            structure.ManufacturingCompleted = 2; // 3 remaining
            structure.Properties.SetProperty("Built", true);
            structure.Properties.SetProperty("Online", true);

            var timer = new CountDownTime();
            timer.StartRepeating(GameConstants.CommodityCycleSeconds);
            structure.ProcessCompletionTime = timer;

            colony.Structures.Add(structure);

            // Add resources to warehouse
            var alkali = new Item(ItemType.ItemTypeEnum.Resource, "Alkali Organics");
            alkali.UUID = Guid.NewGuid().ToString();
            alkali.BaseItemTypeID = "Alkali Organics";
            alkali.ResourcePurity = GameConstants.PurityRefined;
            alkali.Quantity = 50;
            alkali.Volume = 1;
            colony.Items.AddItem(alkali);

            var acidic = new Item(ItemType.ItemTypeEnum.Resource, "Strong Acidic Inorganics");
            acidic.UUID = Guid.NewGuid().ToString();
            acidic.BaseItemTypeID = "Strong Acidic Inorganics";
            acidic.ResourcePurity = GameConstants.PurityRefined;
            acidic.Quantity = 50;
            acidic.Volume = 1;
            colony.Items.AddItem(acidic);

            var calc = new ColonyStatusCalculator(colony);
            calc.CalculateBuilt();

            // Remaining = 5 - 2 = 3 cycles. Lock (3-1)=2 future cycles. Each costs 2 per resource => lock 4
            int lockedAlkali = colony.Locks.GetLockedQuantity(
                ItemType.ItemTypeEnum.Resource, "Alkali Organics|Refined");
            int lockedAcidic = colony.Locks.GetLockedQuantity(
                ItemType.ItemTypeEnum.Resource, "Strong Acidic Inorganics|Refined");

            Assert.That(lockedAlkali, Is.EqualTo(4));
            Assert.That(lockedAcidic, Is.EqualTo(4));
        }

        [Test]
        public void LockCommodityFactoryResources_CreatesResourceItemsWhenNotInWarehouse()
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();

            var bp = new OE2EmpireTracker.Models.Blueprint("TestCF2");
            bp.UUID = Guid.NewGuid().ToString();
            bp.BluePrintType = BlueprintTypes.CommodityFactoryPrefix + "Agridome";
            PlayerContext.GetInstance().AddBlueprint(bp);

            var structure = new ColonyStructure();
            structure.UUID = Guid.NewGuid().ToString();
            structure.FlatpackBlueprintUUID = bp.UUID;
            structure.ManufacturingCommodityName = "Advanced Biolubricants";
            structure.ManufacturingQuantity = 2;
            structure.ManufacturingCompleted = 0;
            structure.Properties.SetProperty("Built", true);
            structure.Properties.SetProperty("Online", true);

            var timer = new CountDownTime();
            timer.StartRepeating(GameConstants.CommodityCycleSeconds);
            structure.ProcessCompletionTime = timer;

            colony.Structures.Add(structure);
            // No resources in warehouse

            var calc = new ColonyStatusCalculator(colony);
            calc.CalculateBuilt();

            // Resources should be created with 0 quantity (locking 1 future cycle creates items)
            var alkali = colony.Items.FindResource("Alkali Organics", GameConstants.PurityRefined);
            Assert.That(
                alkali.Count,
                Is.EqualTo(1),
                "Resource item should be created");
            Assert.That(alkali[0].Quantity, Is.EqualTo(0));
        }

        [Test]
        public void LockCommodityFactoryResources_NoLocks_WhenCommodityNameEmpty()
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();

            var bp = new OE2EmpireTracker.Models.Blueprint("TestCF3");
            bp.UUID = Guid.NewGuid().ToString();
            bp.BluePrintType = BlueprintTypes.CommodityFactoryPrefix + "Agridome";
            PlayerContext.GetInstance().AddBlueprint(bp);

            var structure = new ColonyStructure();
            structure.UUID = Guid.NewGuid().ToString();
            structure.FlatpackBlueprintUUID = bp.UUID;
            structure.ManufacturingCommodityName = string.Empty;
            structure.ManufacturingQuantity = 5;
            structure.ManufacturingCompleted = 0;
            structure.Properties.SetProperty("Built", true);
            structure.Properties.SetProperty("Online", true);

            var timer = new CountDownTime();
            timer.StartRepeating(GameConstants.CommodityCycleSeconds);
            structure.ProcessCompletionTime = timer;

            colony.Structures.Add(structure);

            var calc = new ColonyStatusCalculator(colony);
            calc.CalculateBuilt();

            var locks = colony.Locks.GetLocksForProcess(structure.UUID);
            Assert.That(locks.Count, Is.EqualTo(0));
        }

        [Test]
        public void LockCommodityFactoryResources_NoLocks_WhenCompletedEqualsQuantity()
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();

            var bp = new OE2EmpireTracker.Models.Blueprint("TestCF4");
            bp.UUID = Guid.NewGuid().ToString();
            bp.BluePrintType = BlueprintTypes.CommodityFactoryPrefix + "Agridome";
            PlayerContext.GetInstance().AddBlueprint(bp);

            var structure = new ColonyStructure();
            structure.UUID = Guid.NewGuid().ToString();
            structure.FlatpackBlueprintUUID = bp.UUID;
            structure.ManufacturingCommodityName = "Advanced Biolubricants";
            structure.ManufacturingQuantity = 3;
            structure.ManufacturingCompleted = 3; // All done
            structure.Properties.SetProperty("Built", true);
            structure.Properties.SetProperty("Online", true);

            var timer = new CountDownTime();
            timer.StartRepeating(GameConstants.CommodityCycleSeconds);
            structure.ProcessCompletionTime = timer;

            colony.Structures.Add(structure);

            var calc = new ColonyStatusCalculator(colony);
            calc.CalculateBuilt();

            int lockedAlkali = colony.Locks.GetLockedQuantity(
                ItemType.ItemTypeEnum.Resource, "Alkali Organics|Refined");
            Assert.That(lockedAlkali, Is.EqualTo(0));
        }

        [Test]
        public void ExtractionFocus_Level10_IncreasesMiningOutputBy10Percent()
        {
            var profile = CreatePlayerWithSkills(new Dictionary<SkillName, int>
            {
                { SkillName.ExtractionFocus, 10 }
            });

            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();
            colony.OwnerUUID = profile.UUID;

            // Create a survey with a known resource amount
            var survey = new Survey();
            survey.UUID = Guid.NewGuid().ToString();
            survey.PlanetName = "TestPlanet";
            survey.SurveyID = "S1";
            survey.Resources = new Dictionary<string, SurveyResource>
            {
                { "TestOre", new SurveyResource { Resource = "TestOre", Purity = "Low", Amount = "100" } }
            };

            PlayerContext.GetInstance().AddSurvey(survey);

            // Create a MiningRig blueprint
            var bp = new OE2EmpireTracker.Models.Blueprint("TestMiningRig");
            bp.UUID = Guid.NewGuid().ToString();
            bp.BluePrintType = BlueprintTypes.MiningRig;
            PlayerContext.GetInstance().AddBlueprint(bp);

            var structure = new ColonyStructure();
            structure.UUID = Guid.NewGuid().ToString();
            structure.FlatpackBlueprintUUID = bp.UUID;
            structure.MiningSurvey = survey.UUID;
            structure.MiningSurveyResource = "TestOre";
            structure.Properties.SetProperty("Built", true);
            structure.Properties.SetProperty("Online", true);

            var timer = new CountDownTime();
            timer.StartRepeating(3600);
            timer.StartTime = DateTime.UtcNow.AddSeconds(-3600); // 1 interval passed
            structure.ProcessCompletionTime = timer;

            colony.Structures.Add(structure);
            colony.ProcessColony(MakeContext());

            // Base output = 100, with 10% bonus = 110
            var items = colony.Items.FindResource("TestOre", "Low");
            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items[0].Quantity, Is.EqualTo(110));
        }

        // -----------------------------------------------------------------------
        // Skill multiplier tests -- RefiningFocus
        // -----------------------------------------------------------------------

        [Test]
        public void RefiningFocus_Level5_IncreasesRefiningOutputBy10Percent()
        {
            var profile = CreatePlayerWithSkills(new Dictionary<SkillName, int>
            {
                { SkillName.RefiningFocus, 5 }
            });

            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();
            colony.OwnerUUID = profile.UUID;

            // Create a Refinery blueprint
            var bp = new OE2EmpireTracker.Models.Blueprint("TestRefinery");
            bp.UUID = Guid.NewGuid().ToString();
            bp.BluePrintType = BlueprintTypes.Refinery;
            PlayerContext.GetInstance().AddBlueprint(bp);

            // Add source resource (Low purity, multiplier = 1)
            var sourceItem = new Item(ItemType.ItemTypeEnum.Resource, "TestMineral");
            sourceItem.UUID = Guid.NewGuid().ToString();
            sourceItem.BaseItemTypeID = "TestMineral";
            sourceItem.ResourcePurity = "Low";
            sourceItem.Quantity = 100;
            sourceItem.Volume = 1;
            colony.Items.AddItem(sourceItem);

            var structure = new ColonyStructure();
            structure.UUID = Guid.NewGuid().ToString();
            structure.FlatpackBlueprintUUID = bp.UUID;
            structure.RefiningResource = "TestMineral";
            structure.RefiningResourcePurity = "Low";
            structure.Properties.SetProperty("Built", true);
            structure.Properties.SetProperty("Online", true);

            var timer = new CountDownTime();
            timer.StartRepeating(3600);
            timer.StartTime = DateTime.UtcNow.AddSeconds(-3600); // 1 interval passed
            structure.ProcessCompletionTime = timer;

            colony.Structures.Add(structure);
            colony.ProcessColony(MakeContext());

            // Base: consume 25, Low multiplier = 1, produce 25. With 10% bonus = (int)(25 * 1 * 1.10) = 27
            var refined = colony.Items.FindResource("TestMineral", GameConstants.PurityRefined);
            Assert.That(refined.Count, Is.EqualTo(1));
            Assert.That(refined[0].Quantity, Is.EqualTo(27));
        }

        // -----------------------------------------------------------------------
        // Skill multiplier tests -- zero skill level produces base output
        // -----------------------------------------------------------------------

        [Test]
        public void ZeroSkillLevel_ProducesBaseOutput()
        {
            var profile = CreatePlayerWithSkills(new Dictionary<SkillName, int>
            {
                { SkillName.RefiningFocus, 0 }
            });

            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();
            colony.OwnerUUID = profile.UUID;

            var bp = new OE2EmpireTracker.Models.Blueprint("TestRefinery0");
            bp.UUID = Guid.NewGuid().ToString();
            bp.BluePrintType = BlueprintTypes.Refinery;
            PlayerContext.GetInstance().AddBlueprint(bp);

            var sourceItem = new Item(ItemType.ItemTypeEnum.Resource, "TestMineral2");
            sourceItem.UUID = Guid.NewGuid().ToString();
            sourceItem.BaseItemTypeID = "TestMineral2";
            sourceItem.ResourcePurity = "Low";
            sourceItem.Quantity = 100;
            sourceItem.Volume = 1;
            colony.Items.AddItem(sourceItem);

            var structure = new ColonyStructure();
            structure.UUID = Guid.NewGuid().ToString();
            structure.FlatpackBlueprintUUID = bp.UUID;
            structure.RefiningResource = "TestMineral2";
            structure.RefiningResourcePurity = "Low";
            structure.Properties.SetProperty("Built", true);
            structure.Properties.SetProperty("Online", true);

            var timer = new CountDownTime();
            timer.StartRepeating(3600);
            timer.StartTime = DateTime.UtcNow.AddSeconds(-3600);
            structure.ProcessCompletionTime = timer;

            colony.Structures.Add(structure);
            colony.ProcessColony(MakeContext());

            // Base: consume 25, Low multiplier = 1, produce 25. Multiplier = 1.0 (0 skill)
            var refined = colony.Items.FindResource("TestMineral2", GameConstants.PurityRefined);
            Assert.That(refined.Count, Is.EqualTo(1));
            Assert.That(refined[0].Quantity, Is.EqualTo(25));
        }

        // -----------------------------------------------------------------------
        // Skill multiplier tests -- colony with no OwnerUUID uses base rates
        // -----------------------------------------------------------------------

        [Test]
        public void NoOwnerUUID_UsesBaseRates()
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();
            colony.OwnerUUID = string.Empty; // No owner

            var bp = new OE2EmpireTracker.Models.Blueprint("TestRefineryNoOwner");
            bp.UUID = Guid.NewGuid().ToString();
            bp.BluePrintType = BlueprintTypes.Refinery;
            PlayerContext.GetInstance().AddBlueprint(bp);

            var sourceItem = new Item(ItemType.ItemTypeEnum.Resource, "TestMineral3");
            sourceItem.UUID = Guid.NewGuid().ToString();
            sourceItem.BaseItemTypeID = "TestMineral3";
            sourceItem.ResourcePurity = "Low";
            sourceItem.Quantity = 100;
            sourceItem.Volume = 1;
            colony.Items.AddItem(sourceItem);

            var structure = new ColonyStructure();
            structure.UUID = Guid.NewGuid().ToString();
            structure.FlatpackBlueprintUUID = bp.UUID;
            structure.RefiningResource = "TestMineral3";
            structure.RefiningResourcePurity = "Low";
            structure.Properties.SetProperty("Built", true);
            structure.Properties.SetProperty("Online", true);

            var timer = new CountDownTime();
            timer.StartRepeating(3600);
            timer.StartTime = DateTime.UtcNow.AddSeconds(-3600);
            structure.ProcessCompletionTime = timer;

            colony.Structures.Add(structure);
            colony.ProcessColony(MakeContext());

            // No owner => skill level 0 => multiplier 1.0 => base output 25
            var refined = colony.Items.FindResource("TestMineral3", GameConstants.PurityRefined);
            Assert.That(refined.Count, Is.EqualTo(1));
            Assert.That(refined[0].Quantity, Is.EqualTo(25));
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        /// <summary>
        /// Creates a colony with a CommodityFactory structure configured for the given commodity.
        /// Adds required construction resources to the warehouse.
        /// </summary>
        private static Colony MakeCommodityFactoryColony(
            string commodityName,
            int manufacturingQuantity,
            int manufacturingCompleted,
            int intervalsPassed,
            Dictionary<string, int> warehouseResources = null)
        {
            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();

            // Create a CommodityFactory blueprint in the player context
            var bp = new OE2EmpireTracker.Models.Blueprint("TestCommodityFactory");
            bp.UUID = Guid.NewGuid().ToString();
            bp.BluePrintType = BlueprintTypes.CommodityFactoryPrefix + "Agridome";
            PlayerContext.GetInstance().AddBlueprint(bp);

            var structure = new ColonyStructure();
            structure.UUID = Guid.NewGuid().ToString();
            structure.FlatpackBlueprintUUID = bp.UUID;
            structure.ManufacturingCommodityName = commodityName;
            structure.ManufacturingQuantity = manufacturingQuantity;
            structure.ManufacturingCompleted = manufacturingCompleted;
            structure.Properties.SetProperty("Built", true);
            structure.Properties.SetProperty("Online", true);

            // Set up a repeating timer with the specified intervals already passed
            var timer = new CountDownTime();
            timer.StartRepeating(GameConstants.CommodityCycleSeconds);
            // Move StartTime back so IntervalsPassed returns the desired count
            timer.StartTime = DateTime.UtcNow.AddSeconds(-intervalsPassed * GameConstants.CommodityCycleSeconds);
            structure.ProcessCompletionTime = timer;

            colony.Structures.Add(structure);

            // Add warehouse resources
            if (warehouseResources != null)
            {
                foreach (var kvp in warehouseResources)
                {
                    var item = new Item(ItemType.ItemTypeEnum.Resource, kvp.Key);
                    item.UUID = Guid.NewGuid().ToString();
                    item.BaseItemTypeID = kvp.Key;
                    item.ResourcePurity = GameConstants.PurityRefined;
                    item.Quantity = kvp.Value;
                    item.Volume = 1;
                    colony.Items.AddItem(item);
                }
            }

            return colony;
        }

        // -----------------------------------------------------------------------
        // Skill multiplier tests -- ExtractionFocus (mining)
        // -----------------------------------------------------------------------

        private PlayerProfile CreatePlayerWithSkills(Dictionary<SkillName, int> skills)
        {
            var profile = new PlayerProfile();
            profile.UUID = Guid.NewGuid().ToString();
            profile.Name = "TestPlayer_" + Guid.NewGuid().ToString().Substring(0, 8);
            foreach (var kvp in skills)
            {
                profile.GetSkill(kvp.Key).Level = kvp.Value;
            }

            PlayerContext.GetInstance().AddPlayerProfile(profile);
            return profile;
        }

        private static IColonyProcessingContext MakeContext()
        {
            return new ColonyProcessingContextAdapter(
                PlayerContext.GetInstance(), EmpireContext.GetInstance());
        }
    }
}
