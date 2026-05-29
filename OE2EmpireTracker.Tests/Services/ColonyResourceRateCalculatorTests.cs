using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests and property-based tests for ColonyResourceRateCalculator.
    /// **Validates: Requirements 1.4, 1.5**
    /// </summary>
    [TestFixture]
    public class ColonyResourceRateCalculatorTests
    {
        [SetUp]
        public void SetUp()
        {
            TestHelper.SetEmpireFilePath();
            EmpireContext.Reset();
            EmpireContext.GetInstance();
            PlayerContext.Reset();
            PlayerContext.FilePath = "nonexistent_player_data.json";
        }

        [TearDown]
        public void TearDown()
        {
            PlayerContext.Reset();
            EmpireContext.Reset();
        }

        // -------------------------------------------------------------------
        // ComputeWarehouseVolume Unit Tests
        // -------------------------------------------------------------------

        [Test]
        public void ComputeWarehouseVolume_EmptyColony_ReturnsZero()
        {
            var colony = MakeColony();

            decimal volume = ColonyResourceRateCalculator.ComputeWarehouseVolume(colony);

            Assert.That(volume, Is.EqualTo(0m));
        }

        [Test]
        public void ComputeWarehouseVolume_NullColony_ReturnsZero()
        {
            decimal volume = ColonyResourceRateCalculator.ComputeWarehouseVolume(null);

            Assert.That(volume, Is.EqualTo(0m));
        }

        [Test]
        public void ComputeWarehouseVolume_ResourcesOnly_UsesVolumeResource()
        {
            var colony = MakeColony();
            AddItem(colony, ItemType.ItemTypeEnum.Resource, 100);

            decimal volume = ColonyResourceRateCalculator.ComputeWarehouseVolume(colony);

            Assert.That(volume, Is.EqualTo(100m * GameConstants.VolumeResource));
        }

        [Test]
        public void ComputeWarehouseVolume_CommoditiesOnly_UsesVolumeCommodity()
        {
            var colony = MakeColony();
            AddItem(colony, ItemType.ItemTypeEnum.Commodity, 50);

            decimal volume = ColonyResourceRateCalculator.ComputeWarehouseVolume(colony);

            Assert.That(volume, Is.EqualTo(50m * GameConstants.VolumeCommodity));
        }

        [Test]
        public void ComputeWarehouseVolume_WorkersOnly_UsesVolumeWorkDetail()
        {
            var colony = MakeColony();
            AddItem(colony, ItemType.ItemTypeEnum.WorkDetail, 10);

            decimal volume = ColonyResourceRateCalculator.ComputeWarehouseVolume(colony);

            Assert.That(volume, Is.EqualTo(10m * GameConstants.VolumeWorkDetail));
        }

        [Test]
        public void ComputeWarehouseVolume_BlueprintsOnly_ReturnsZero()
        {
            var colony = MakeColony();
            AddItem(colony, ItemType.ItemTypeEnum.Blueprint, 25);

            decimal volume = ColonyResourceRateCalculator.ComputeWarehouseVolume(colony);

            Assert.That(volume, Is.EqualTo(0m));
        }

        [Test]
        public void ComputeWarehouseVolume_SurveysOnly_ReturnsZero()
        {
            var colony = MakeColony();
            AddItem(colony, ItemType.ItemTypeEnum.Survey, 5);

            decimal volume = ColonyResourceRateCalculator.ComputeWarehouseVolume(colony);

            Assert.That(volume, Is.EqualTo(0m));
        }

        [Test]
        public void ComputeWarehouseVolume_MixedItemTypes_SumsCorrectly()
        {
            var colony = MakeColony();
            AddItem(colony, ItemType.ItemTypeEnum.Resource, 200);
            AddItem(colony, ItemType.ItemTypeEnum.Commodity, 30);
            AddItem(colony, ItemType.ItemTypeEnum.WorkDetail, 5);
            AddItem(colony, ItemType.ItemTypeEnum.Blueprint, 10);
            AddItem(colony, ItemType.ItemTypeEnum.Survey, 3);

            decimal volume = ColonyResourceRateCalculator.ComputeWarehouseVolume(colony);

            decimal expected = (200m * GameConstants.VolumeResource)
                + (30m * GameConstants.VolumeCommodity)
                + (5m * GameConstants.VolumeWorkDetail)
                + (10m * GameConstants.VolumeBlueprint)
                + (3m * GameConstants.VolumeSurvey);
            Assert.That(volume, Is.EqualTo(expected));
        }

        [Test]
        public void ComputeWarehouseVolume_OtherItemType_UsesItemVolumeProperty()
        {
            var colony = MakeColony();
            var item = new Item(ItemType.ItemTypeEnum.ShipHull, "TestHull");
            item.UUID = Guid.NewGuid().ToString();
            item.Quantity = 2;
            item.Volume = 75m;
            colony.Items.AddItem(item);

            decimal volume = ColonyResourceRateCalculator.ComputeWarehouseVolume(colony);

            Assert.That(volume, Is.EqualTo(2m * 75m));
        }

        // -------------------------------------------------------------------
        // GetNetHourlyRate Unit Tests
        // -------------------------------------------------------------------

        [Test]
        public void GetNetHourlyRate_EmptyColony_ReturnsZero()
        {
            var pc = PlayerContext.GetInstance();
            var colony = MakeColony();

            decimal rate = ColonyResourceRateCalculator.GetNetHourlyRate(
                colony, pc, "Iron", "Low");

            Assert.That(rate, Is.EqualTo(0m));
        }

        [Test]
        public void GetNetHourlyRate_NullColony_ReturnsZero()
        {
            var pc = PlayerContext.GetInstance();

            decimal rate = ColonyResourceRateCalculator.GetNetHourlyRate(
                null, pc, "Iron", "Low");

            Assert.That(rate, Is.EqualTo(0m));
        }

        [Test]
        public void GetNetHourlyRate_OneMiner_NoRefiners_ReturnsMiningRate()
        {
            var pc = PlayerContext.GetInstance();
            string ownerUUID = Guid.NewGuid().ToString();
            CreateOwnerProfile(ownerUUID);

            var minerBp = CreateBlueprint(BlueprintTypes.MiningRig, "Miner");
            var survey = CreateSurvey("Iron", "Low", "50");

            var colony = MakeColony();
            colony.OwnerUUID = ownerUUID;
            colony.Structures.Add(MakeActiveMiner(minerBp.UUID, 1, survey.UUID, "Iron"));

            decimal rate = ColonyResourceRateCalculator.GetNetHourlyRate(
                colony, pc, "Iron", "Low");

            Assert.That(rate, Is.EqualTo(50m));
        }

        [Test]
        public void GetNetHourlyRate_NoMiners_OneRefiner_ReturnsNegativeConsumption()
        {
            var pc = PlayerContext.GetInstance();
            string ownerUUID = Guid.NewGuid().ToString();
            CreateOwnerProfile(ownerUUID);

            var refinerBp = CreateBlueprint(BlueprintTypes.Refinery, "Refiner");

            var colony = MakeColony();
            colony.OwnerUUID = ownerUUID;
            colony.Structures.Add(MakeActiveRefiner(refinerBp.UUID, 1, "Iron", "Low"));

            decimal rate = ColonyResourceRateCalculator.GetNetHourlyRate(
                colony, pc, "Iron", "Low");

            Assert.That(rate, Is.EqualTo(-GameConstants.RefiningBaseRate));
        }

        [Test]
        public void GetNetHourlyRate_MultipleMinersMixedPurities_OnlyMatchingPurityCounted()
        {
            var pc = PlayerContext.GetInstance();
            string ownerUUID = Guid.NewGuid().ToString();
            CreateOwnerProfile(ownerUUID);

            var minerBp = CreateBlueprint(BlueprintTypes.MiningRig, "Miner");
            var surveyLow = CreateSurvey("Iron", "Low", "30");
            var surveyHigh = CreateSurvey("Iron", "High", "80");

            var colony = MakeColony();
            colony.OwnerUUID = ownerUUID;
            colony.Structures.Add(MakeActiveMiner(minerBp.UUID, 1, surveyLow.UUID, "Iron"));
            colony.Structures.Add(MakeActiveMiner(minerBp.UUID, 2, surveyHigh.UUID, "Iron"));

            decimal rateLow = ColonyResourceRateCalculator.GetNetHourlyRate(
                colony, pc, "Iron", "Low");
            decimal rateHigh = ColonyResourceRateCalculator.GetNetHourlyRate(
                colony, pc, "Iron", "High");

            Assert.That(rateLow, Is.EqualTo(30m));
            Assert.That(rateHigh, Is.EqualTo(80m));
        }

        [Test]
        public void GetNetHourlyRate_MultipleMinersAndRefiners_ReturnsNetRate()
        {
            var pc = PlayerContext.GetInstance();
            string ownerUUID = Guid.NewGuid().ToString();
            CreateOwnerProfile(ownerUUID);

            var minerBp = CreateBlueprint(BlueprintTypes.MiningRig, "Miner");
            var refinerBp = CreateBlueprint(BlueprintTypes.Refinery, "Refiner");
            var survey = CreateSurvey("Iron", "Low", "40");

            var colony = MakeColony();
            colony.OwnerUUID = ownerUUID;

            // 2 miners at 40/h each = 80/h total mining
            colony.Structures.Add(MakeActiveMiner(minerBp.UUID, 1, survey.UUID, "Iron"));
            colony.Structures.Add(MakeActiveMiner(minerBp.UUID, 2, survey.UUID, "Iron"));

            // 3 refiners at 25/cycle each = 75/h total consumption
            colony.Structures.Add(MakeActiveRefiner(refinerBp.UUID, 3, "Iron", "Low"));
            colony.Structures.Add(MakeActiveRefiner(refinerBp.UUID, 4, "Iron", "Low"));
            colony.Structures.Add(MakeActiveRefiner(refinerBp.UUID, 5, "Iron", "Low"));

            decimal rate = ColonyResourceRateCalculator.GetNetHourlyRate(
                colony, pc, "Iron", "Low");

            // Net = 80 - 75 = 5
            Assert.That(rate, Is.EqualTo(5m));
        }

        // -------------------------------------------------------------------
        // GetTotalMiningRate Unit Tests
        // -------------------------------------------------------------------

        [Test]
        public void GetTotalMiningRate_ZeroMiners_ReturnsZero()
        {
            var pc = PlayerContext.GetInstance();
            var colony = MakeColony();

            decimal rate = ColonyResourceRateCalculator.GetTotalMiningRate(
                colony, pc, "Iron", "Low");

            Assert.That(rate, Is.EqualTo(0m));
        }

        [Test]
        public void GetTotalMiningRate_WithExtractionFocusBonus_AppliesMultiplier()
        {
            var pc = PlayerContext.GetInstance();
            string ownerUUID = Guid.NewGuid().ToString();
            CreateOwnerProfile(ownerUUID, extractionFocusLevel: 5);

            var minerBp = CreateBlueprint(BlueprintTypes.MiningRig, "Miner");
            var survey = CreateSurvey("Iron", "Low", "100");

            var colony = MakeColony();
            colony.OwnerUUID = ownerUUID;
            colony.Structures.Add(MakeActiveMiner(minerBp.UUID, 1, survey.UUID, "Iron"));

            decimal rate = ColonyResourceRateCalculator.GetTotalMiningRate(
                colony, pc, "Iron", "Low");

            // ExtractionFocus level 5 = 1.0 + (5 * 0.01) = 1.05 multiplier
            decimal expected = 100m * 1.05m;
            Assert.That(rate, Is.EqualTo(expected));
        }

        [Test]
        public void GetTotalMiningRate_MultipleMiners_SumsRates()
        {
            var pc = PlayerContext.GetInstance();
            string ownerUUID = Guid.NewGuid().ToString();
            CreateOwnerProfile(ownerUUID);

            var minerBp = CreateBlueprint(BlueprintTypes.MiningRig, "Miner");
            var survey1 = CreateSurvey("Iron", "Low", "30");
            var survey2 = CreateSurvey("Iron", "Low", "45");

            var colony = MakeColony();
            colony.OwnerUUID = ownerUUID;
            colony.Structures.Add(MakeActiveMiner(minerBp.UUID, 1, survey1.UUID, "Iron"));
            colony.Structures.Add(MakeActiveMiner(minerBp.UUID, 2, survey2.UUID, "Iron"));

            decimal rate = ColonyResourceRateCalculator.GetTotalMiningRate(
                colony, pc, "Iron", "Low");

            Assert.That(rate, Is.EqualTo(75m));
        }

        [Test]
        public void GetTotalMiningRate_InactiveMiner_NotCounted()
        {
            var pc = PlayerContext.GetInstance();
            string ownerUUID = Guid.NewGuid().ToString();
            CreateOwnerProfile(ownerUUID);

            var minerBp = CreateBlueprint(BlueprintTypes.MiningRig, "Miner");
            var survey = CreateSurvey("Iron", "Low", "50");

            var colony = MakeColony();
            colony.OwnerUUID = ownerUUID;

            // Miner without active process timer
            var structure = new ColonyStructure();
            structure.UUID = Guid.NewGuid().ToString();
            structure.FlatpackBlueprintUUID = minerBp.UUID;
            structure.DisplaySequence = 1;
            structure.Properties.SetProperty(GameConstants.PropBuilt, true);
            structure.Properties.SetProperty(GameConstants.PropOnline, true);
            structure.MiningSurvey = survey.UUID;
            structure.MiningSurveyResource = "Iron";
            colony.Structures.Add(structure);

            decimal rate = ColonyResourceRateCalculator.GetTotalMiningRate(
                colony, pc, "Iron", "Low");

            Assert.That(rate, Is.EqualTo(0m));
        }

        // -------------------------------------------------------------------
        // GetTotalRefiningConsumption Unit Tests
        // -------------------------------------------------------------------

        [Test]
        public void GetTotalRefiningConsumption_ZeroRefiners_ReturnsZero()
        {
            var pc = PlayerContext.GetInstance();
            var colony = MakeColony();

            decimal rate = ColonyResourceRateCalculator.GetTotalRefiningConsumption(
                colony, pc, "Iron", "Low");

            Assert.That(rate, Is.EqualTo(0m));
        }

        [Test]
        public void GetTotalRefiningConsumption_MultipleRefiners_SumsConsumption()
        {
            var pc = PlayerContext.GetInstance();
            string ownerUUID = Guid.NewGuid().ToString();
            CreateOwnerProfile(ownerUUID);

            var refinerBp = CreateBlueprint(BlueprintTypes.Refinery, "Refiner");

            var colony = MakeColony();
            colony.OwnerUUID = ownerUUID;
            colony.Structures.Add(MakeActiveRefiner(refinerBp.UUID, 1, "Iron", "Low"));
            colony.Structures.Add(MakeActiveRefiner(refinerBp.UUID, 2, "Iron", "Low"));

            decimal rate = ColonyResourceRateCalculator.GetTotalRefiningConsumption(
                colony, pc, "Iron", "Low");

            Assert.That(rate, Is.EqualTo(GameConstants.RefiningBaseRate * 2));
        }

        [Test]
        public void GetTotalRefiningConsumption_InactiveRefiner_NotCounted()
        {
            var pc = PlayerContext.GetInstance();
            string ownerUUID = Guid.NewGuid().ToString();
            CreateOwnerProfile(ownerUUID);

            var refinerBp = CreateBlueprint(BlueprintTypes.Refinery, "Refiner");

            var colony = MakeColony();
            colony.OwnerUUID = ownerUUID;

            // Refiner without active process timer
            var structure = new ColonyStructure();
            structure.UUID = Guid.NewGuid().ToString();
            structure.FlatpackBlueprintUUID = refinerBp.UUID;
            structure.DisplaySequence = 1;
            structure.Properties.SetProperty(GameConstants.PropBuilt, true);
            structure.Properties.SetProperty(GameConstants.PropOnline, true);
            structure.RefiningResource = "Iron";
            structure.RefiningResourcePurity = "Low";
            colony.Structures.Add(structure);

            decimal rate = ColonyResourceRateCalculator.GetTotalRefiningConsumption(
                colony, pc, "Iron", "Low");

            Assert.That(rate, Is.EqualTo(0m));
        }

        // -------------------------------------------------------------------
        // Property 1: Warehouse Volume Accuracy
        // Feature: colony-admin-warehouse-utilization
        // For any colony C with items I1...In:
        //   ComputeWarehouseVolume(C) == Sum(Ii.Quantity * VolumeForType(Ii.ItemType))
        // **Validates: Requirements 1.4, 1.5**
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Property1_WarehouseVolumeAccuracy()
        {
            var countGen = Gen.Choose(0, 10);
            var quantityGen = Gen.Choose(0, 1000);
            var itemTypeIndexGen = Gen.Choose(0, 5);
            var volumeGen = Gen.Choose(1, 100);

            var itemTypes = new[]
            {
                ItemType.ItemTypeEnum.Resource,
                ItemType.ItemTypeEnum.Commodity,
                ItemType.ItemTypeEnum.WorkDetail,
                ItemType.ItemTypeEnum.Blueprint,
                ItemType.ItemTypeEnum.Survey,
                ItemType.ItemTypeEnum.ShipHull
            };

            var gen = from count in countGen
                      from qty in quantityGen
                      from typeIdx in itemTypeIndexGen
                      from vol in volumeGen
                      select new { Count = count, Quantity = qty, TypeIdx = typeIdx, Volume = vol };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var colony = MakeColony();

                decimal expectedVolume = 0m;
                for (int i = 0; i < data.Count; i++)
                {
                    var itemType = itemTypes[data.TypeIdx];
                    decimal vol = (decimal)data.Volume;

                    var item = new Item(itemType, "Item_" + i);
                    item.UUID = Guid.NewGuid().ToString();
                    item.Quantity = data.Quantity;
                    item.Volume = vol;
                    colony.Items.AddItem(item);

                    decimal volumePerUnit = GetExpectedVolumePerUnit(itemType, vol);
                    expectedVolume += data.Quantity * volumePerUnit;
                }

                decimal actualVolume = ColonyResourceRateCalculator.ComputeWarehouseVolume(colony);

                if (actualVolume != expectedVolume)
                {
                    return false.Label(
                        $"Expected {expectedVolume}, got {actualVolume} for {data.Count} items");
                }

                return true.Label("Volume matches formula");
            });
        }

        // -------------------------------------------------------------------
        // Helper Methods
        // -------------------------------------------------------------------

        private static decimal GetExpectedVolumePerUnit(ItemType.ItemTypeEnum itemType, decimal itemVolume)
        {
            switch (itemType)
            {
                case ItemType.ItemTypeEnum.Resource:
                    return GameConstants.VolumeResource;
                case ItemType.ItemTypeEnum.Commodity:
                    return GameConstants.VolumeCommodity;
                case ItemType.ItemTypeEnum.WorkDetail:
                    return GameConstants.VolumeWorkDetail;
                case ItemType.ItemTypeEnum.Blueprint:
                    return GameConstants.VolumeBlueprint;
                case ItemType.ItemTypeEnum.Survey:
                    return GameConstants.VolumeSurvey;
                default:
                    return itemVolume;
            }
        }

        private static Colony MakeColony(string systemName = "TestSystem", string colonyName = "TestColony")
        {
            return new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                SystemName = systemName,
                ColonyName = colonyName,
                LastImportDateTime = SurveyDateTimeParser.ToIsoString(SystemClock.UtcNow)
            };
        }

        private static void AddItem(Colony colony, ItemType.ItemTypeEnum itemType, int quantity)
        {
            var item = new Item(itemType, "TestItem_" + Guid.NewGuid().ToString().Substring(0, 6));
            item.UUID = Guid.NewGuid().ToString();
            item.Quantity = quantity;
            colony.Items.AddItem(item);
        }

        private OE2EmpireTracker.Models.Blueprint CreateBlueprint(string bpType, string name)
        {
            var bp = new OE2EmpireTracker.Models.Blueprint(name);
            bp.UUID = Guid.NewGuid().ToString();
            bp.BluePrintType = bpType;
            PlayerContext.GetInstance().AddBlueprint(bp);
            return bp;
        }

        private PlayerProfile CreateOwnerProfile(string ownerUUID, int extractionFocusLevel = 0)
        {
            var pc = PlayerContext.GetInstance();
            var profile = new PlayerProfile
            {
                UUID = ownerUUID,
                Name = "TestOwner_" + ownerUUID.Substring(0, 6)
            };

            if (extractionFocusLevel > 0)
            {
                profile.GetSkill(SkillName.ExtractionFocus).Level = extractionFocusLevel;
            }

            pc.AddPlayerProfile(profile);
            return profile;
        }

        private Survey CreateSurvey(string resource, string purity, string amount)
        {
            var pc = PlayerContext.GetInstance();
            var survey = new Survey("TestSurvey_" + Guid.NewGuid().ToString().Substring(0, 6));
            survey.UUID = Guid.NewGuid().ToString();
            survey.Resources[resource] = new SurveyResource(resource, purity, amount);
            pc.AddSurvey(survey);
            return survey;
        }

        private static ColonyStructure MakeActiveMiner(
            string blueprintUUID,
            int gameSeq,
            string surveyUUID,
            string surveyResource)
        {
            var structure = new ColonyStructure();
            structure.UUID = Guid.NewGuid().ToString();
            structure.FlatpackBlueprintUUID = blueprintUUID;
            structure.DisplaySequence = gameSeq;
            structure.Properties.SetProperty(GameConstants.PropBuilt, true);
            structure.Properties.SetProperty(GameConstants.PropOnline, true);
            var timer = new CountDownTime();
            timer.StartRepeating(3600);
            structure.ProcessCompletionTime = timer;
            structure.MiningSurvey = surveyUUID;
            structure.MiningSurveyResource = surveyResource;
            return structure;
        }

        private static ColonyStructure MakeActiveRefiner(
            string blueprintUUID,
            int gameSeq,
            string resource,
            string purity)
        {
            var structure = new ColonyStructure();
            structure.UUID = Guid.NewGuid().ToString();
            structure.FlatpackBlueprintUUID = blueprintUUID;
            structure.DisplaySequence = gameSeq;
            structure.Properties.SetProperty(GameConstants.PropBuilt, true);
            structure.Properties.SetProperty(GameConstants.PropOnline, true);
            var timer = new CountDownTime();
            timer.StartRepeating(3600);
            structure.ProcessCompletionTime = timer;
            structure.RefiningResource = resource;
            structure.RefiningResourcePurity = purity;
            return structure;
        }
    }
}
