using NUnit.Framework;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using OE2EmpireTracker.Tests;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class ColonyInactivityCollectorTests
    {
        private static readonly Random Rng = new Random(42);

        private static readonly string[] ProductionBlueprintTypes = new[]
        {
            BlueprintTypes.MiningRig,
            BlueprintTypes.Refinery,
            BlueprintTypes.ResearchLaboratory,
            BlueprintTypes.Manufactory,
            BlueprintTypes.CommodityFactoryPrefix + "Agridome"
        };

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            TestHelper.SetEmpireFilePath();
            EmpireContext.Reset();
        }

        [SetUp]
        public void SetUp()
        {
            PlayerContext.Reset();
            EmpireContext.Reset();
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private OE2EmpireTracker.Models.Blueprint CreateBlueprint(string bpType, string name = null, int evolution = 0)
        {
            var bp = new OE2EmpireTracker.Models.Blueprint(name ?? "TestBP_" + Guid.NewGuid().ToString().Substring(0, 6));
            bp.UUID = Guid.NewGuid().ToString();
            bp.BluePrintType = bpType;
            bp.Evolution = evolution;
            PlayerContext.getInstance().blueprintList.Add(bp);
            return bp;
        }

        private static ColonyStructure MakeStructure(string blueprintUUID, int gameSeq,
            bool built = true, bool online = true)
        {
            var structure = new ColonyStructure();
            structure.UUID = Guid.NewGuid().ToString();
            structure.FlatpackBlueprintUUID = blueprintUUID;
            structure.displaySequence = gameSeq;
            if (built)
                structure.Properties.setProperty(GameConstants.PropBuilt, true);
            if (online)
                structure.Properties.setProperty(GameConstants.PropOnline, true);
            return structure;
        }

        private static Colony MakeColony(string systemName = "TestSystem", string colonyName = "TestColony")
        {
            return new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                SystemName = systemName,
                ColonyName = colonyName
            };
        }

        private static CountDownTime MakeActiveTimer(long secondsRemaining)
        {
            var timer = new CountDownTime();
            timer.TimeRemaining = secondsRemaining;
            return timer;
        }

        private static CountDownTime MakeActiveRepeatingTimer(long intervalSeconds)
        {
            var timer = new CountDownTime();
            timer.StartRepeating(intervalSeconds);
            return timer;
        }

        // -----------------------------------------------------------------------
        // Property 1: Idle structure detection
        // Feature: activity-inactivity-mode, Property 1: Idle structure detection
        // **Validates: Requirements 3.1, 3.2, 4.1, 4.2, 6.1, 6.2, 7.1, 7.2, 8.1, 8.2**
        // -----------------------------------------------------------------------

        [Test]
        public void Property1_IdleStructureDetection()
        {
            var pc = PlayerContext.getInstance();

            for (int iteration = 0; iteration < 100; iteration++)
            {
                // Reset blueprints each iteration to avoid accumulation
                pc.blueprintList.Clear();

                var colony = MakeColony("Sys_" + iteration, "Col_" + iteration);
                int expectedIdleCount = 0;

                int structureCount = Rng.Next(1, 6);
                for (int s = 0; s < structureCount; s++)
                {
                    string bpType = ProductionBlueprintTypes[Rng.Next(ProductionBlueprintTypes.Length)];
                    var bp = CreateBlueprint(bpType);

                    // Random state: 0=built+online+no timer (idle), 1=built+online+active timer (active),
                    // 2=not built, 3=not online, 4=built+online+work item+no timer (idle with work)
                    int state = Rng.Next(5);

                    bool built = state != 2;
                    bool online = state != 3;
                    var structure = MakeStructure(bp.UUID, s + 1, built, online);

                    if (state == 1)
                    {
                        // Active — has a running process timer
                        structure.ProcessCompletionTime = MakeActiveRepeatingTimer(3600);
                        // Also assign a work item so it's truly active
                        AssignWorkItem(structure, bpType);
                    }
                    else if (state == 4)
                    {
                        // Idle with work item assigned but no timer
                        AssignWorkItem(structure, bpType);
                    }
                    // state 0: no work item, no timer — idle
                    // state 2: not built — should not appear
                    // state 3: not online — should not appear

                    colony.Structures.Add(structure);

                    // Only built+online structures without active process should appear
                    if (built && online && state != 1)
                    {
                        expectedIdleCount++;
                    }
                }

                var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

                Assert.That(rows.Count, Is.EqualTo(expectedIdleCount),
                    $"Iteration {iteration}: expected {expectedIdleCount} idle rows, got {rows.Count}");

                // Verify no active structures appear
                foreach (var row in rows)
                {
                    Assert.That(row.CountDown, Is.Null,
                        $"Iteration {iteration}: inactivity row should have null CountDown");
                }
            }
        }

        private void AssignWorkItem(ColonyStructure structure, string bpType)
        {
            if (bpType == BlueprintTypes.MiningRig)
                structure.MiningSurveyResource = "TestOre";
            else if (bpType == BlueprintTypes.Refinery)
                structure.RefiningResource = "TestMineral";
            else if (bpType == BlueprintTypes.ResearchLaboratory)
                structure.ResearchingBlueprintUUID = Guid.NewGuid().ToString();
            else if (bpType == BlueprintTypes.Manufactory)
                structure.ManufacturingBlueprintUUID = Guid.NewGuid().ToString();
            else if (bpType.IsCommodityFactory())
                structure.ManufacturingCommodityName = "TestCommodity";
        }

        // -----------------------------------------------------------------------
        // Property 2: Idle structure ProcessDetails correctness
        // Feature: activity-inactivity-mode, Property 2: Idle structure ProcessDetails correctness
        // **Validates: Requirements 3.3, 3.4, 4.3, 4.4, 6.3, 6.4, 7.3, 7.4, 8.3, 8.4**
        // -----------------------------------------------------------------------

        [Test]
        public void Property2_IdleStructureProcessDetailsCorrectness()
        {
            var pc = PlayerContext.getInstance();

            // Test each structure type with both "no work item" and "work item but no timer"
            var testCases = new[]
            {
                new { BpType = BlueprintTypes.MiningRig, NoWorkDetails = "No survey assigned", IdleDetails = "Idle" },
                new { BpType = BlueprintTypes.Refinery, NoWorkDetails = "No resource assigned", IdleDetails = "Idle" },
                new { BpType = BlueprintTypes.ResearchLaboratory, NoWorkDetails = "No blueprint assigned", IdleDetails = "Idle" },
                new { BpType = BlueprintTypes.Manufactory, NoWorkDetails = "No blueprint assigned", IdleDetails = "Idle" },
                new { BpType = BlueprintTypes.CommodityFactoryPrefix + "Agridome", NoWorkDetails = "No commodity assigned", IdleDetails = "Idle" }
            };

            for (int iteration = 0; iteration < 100; iteration++)
            {
                pc.blueprintList.Clear();

                var testCase = testCases[iteration % testCases.Length];
                bool hasWorkItem = iteration % 2 == 0;

                var bp = CreateBlueprint(testCase.BpType, "BP_" + iteration);
                var colony = MakeColony();
                var structure = MakeStructure(bp.UUID, iteration + 1);

                if (hasWorkItem)
                {
                    AssignWorkItem(structure, testCase.BpType);
                }

                colony.Structures.Add(structure);

                var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

                Assert.That(rows.Count, Is.EqualTo(1),
                    $"Iteration {iteration}: expected 1 row for {testCase.BpType}");

                string expectedDetails = hasWorkItem ? testCase.IdleDetails : testCase.NoWorkDetails;
                Assert.That(rows[0].ProcessDetails, Is.EqualTo(expectedDetails),
                    $"Iteration {iteration}: ProcessDetails mismatch for {testCase.BpType} (hasWork={hasWorkItem})");
            }
        }

        // -----------------------------------------------------------------------
        // Property 3: Inactivity row metadata format
        // Feature: activity-inactivity-mode, Property 3: Inactivity row metadata format
        // **Validates: Requirements 9.1, 9.2, 9.3, 9.4**
        // -----------------------------------------------------------------------

        [Test]
        public void Property3_InactivityRowMetadataFormat()
        {
            var pc = PlayerContext.getInstance();

            var typeMap = new Dictionary<string, ActivityType>
            {
                { BlueprintTypes.MiningRig, ActivityType.Mining },
                { BlueprintTypes.Refinery, ActivityType.Refining },
                { BlueprintTypes.ResearchLaboratory, ActivityType.Research },
                { BlueprintTypes.Manufactory, ActivityType.Manufacturing },
                { BlueprintTypes.CommodityFactoryPrefix + "Agridome", ActivityType.CommodityManufacturing }
            };

            for (int iteration = 0; iteration < 100; iteration++)
            {
                pc.blueprintList.Clear();

                string bpType = ProductionBlueprintTypes[iteration % ProductionBlueprintTypes.Length];
                int gameSeq = Rng.Next(1, 50);
                int evo = Rng.Next(0, 10);
                string bpName = "MetaBP_" + iteration;
                var bp = CreateBlueprint(bpType, bpName, evo);

                string systemName = "MetaSys_" + iteration;
                string colonyName = "MetaCol_" + iteration;
                var colony = MakeColony(systemName, colonyName);
                var structure = MakeStructure(bp.UUID, gameSeq);
                colony.Structures.Add(structure);

                var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

                Assert.That(rows.Count, Is.EqualTo(1),
                    $"Iteration {iteration}: expected 1 row");

                var row = rows[0];

                // Type must map correctly
                Assert.That(row.Type, Is.EqualTo(typeMap[bpType]),
                    $"Iteration {iteration}: Type mismatch for {bpType}");

                // SystemName and ColonyName must match
                Assert.That(row.SystemName, Is.EqualTo(systemName),
                    $"Iteration {iteration}: SystemName mismatch");
                Assert.That(row.ColonyName, Is.EqualTo(colonyName),
                    $"Iteration {iteration}: ColonyName mismatch");

                // SourceName must follow "#{displaySequence} {blueprint.ExtendedName}" format
                Assert.That(row.SourceName, Is.EqualTo($"#{gameSeq} {bp.ExtendedName}"),
                    $"Iteration {iteration}: SourceName mismatch");

                // CountDown must be null
                Assert.That(row.CountDown, Is.Null,
                    $"Iteration {iteration}: CountDown should be null");
            }
        }

        // -----------------------------------------------------------------------
        // Unit Tests — Edge Cases
        // **Validates: Requirements 3.1–3.4, 4.1–4.4, 6.1–6.4, 7.1–7.4, 8.1–8.4, 9.1–9.4**
        // -----------------------------------------------------------------------

        [Test]
        public void EmptyColony_ReturnsEmptyList()
        {
            var pc = PlayerContext.getInstance();
            var colony = MakeColony();
            // Colony has no structures

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

            Assert.That(rows.Count, Is.EqualTo(0));
        }

        [Test]
        public void EmptyColonyList_ReturnsEmptyList()
        {
            var pc = PlayerContext.getInstance();

            var rows = ColonyInactivityCollector.CollectInactivities(new List<Colony>(), pc);

            Assert.That(rows.Count, Is.EqualTo(0));
        }

        [Test]
        public void NullBlueprint_SkipsStructureSilently()
        {
            var pc = PlayerContext.getInstance();
            var colony = MakeColony();

            // Structure with a UUID that doesn't match any blueprint
            var structure = MakeStructure(Guid.NewGuid().ToString(), 1);
            colony.Structures.Add(structure);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

            Assert.That(rows.Count, Is.EqualTo(0));
        }

        [Test]
        public void NonProductionStructureType_IsIgnored()
        {
            var pc = PlayerContext.getInstance();
            // Use a non-production blueprint type (e.g., "Flatpacks/Agridome")
            var bp = CreateBlueprint("Flatpacks/Agridome", "TestAgridome");
            var colony = MakeColony();
            var structure = MakeStructure(bp.UUID, 1);
            colony.Structures.Add(structure);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

            Assert.That(rows.Count, Is.EqualTo(0));
        }

        [Test]
        public void NotBuiltStructure_DoesNotAppear()
        {
            var pc = PlayerContext.getInstance();
            var bp = CreateBlueprint(BlueprintTypes.MiningRig, "UnbuiltMiner");
            var colony = MakeColony();
            var structure = MakeStructure(bp.UUID, 1, built: false, online: true);
            colony.Structures.Add(structure);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

            Assert.That(rows.Count, Is.EqualTo(0));
        }

        [Test]
        public void NotOnlineStructure_DoesNotAppear()
        {
            var pc = PlayerContext.getInstance();
            var bp = CreateBlueprint(BlueprintTypes.Refinery, "OfflineRefinery");
            var colony = MakeColony();
            var structure = MakeStructure(bp.UUID, 1, built: true, online: false);
            colony.Structures.Add(structure);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

            Assert.That(rows.Count, Is.EqualTo(0));
        }

        [Test]
        public void ActiveStructure_DoesNotAppear()
        {
            var pc = PlayerContext.getInstance();
            var bp = CreateBlueprint(BlueprintTypes.MiningRig, "ActiveMiner");
            var colony = MakeColony();
            var structure = MakeStructure(bp.UUID, 1);
            structure.ProcessCompletionTime = MakeActiveRepeatingTimer(3600);
            structure.MiningSurveyResource = "Iron";
            colony.Structures.Add(structure);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

            Assert.That(rows.Count, Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // Per-type idle detection tests
        // -----------------------------------------------------------------------

        [Test]
        public void IdleMiner_NoSurvey_ReturnsNoSurveyAssigned()
        {
            var pc = PlayerContext.getInstance();
            var bp = CreateBlueprint(BlueprintTypes.MiningRig, "IdleMiner");
            var colony = MakeColony("MineSys", "MineCol");
            var structure = MakeStructure(bp.UUID, 3);
            colony.Structures.Add(structure);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].Type, Is.EqualTo(ActivityType.Mining));
            Assert.That(rows[0].ProcessDetails, Is.EqualTo("No survey assigned"));
            Assert.That(rows[0].SourceName, Is.EqualTo($"#3 {bp.ExtendedName}"));
            Assert.That(rows[0].SystemName, Is.EqualTo("MineSys"));
            Assert.That(rows[0].ColonyName, Is.EqualTo("MineCol"));
        }

        [Test]
        public void IdleMiner_SurveyAssignedNoTimer_ReturnsIdle()
        {
            var pc = PlayerContext.getInstance();
            var bp = CreateBlueprint(BlueprintTypes.MiningRig, "IdleMinerWithSurvey");
            var colony = MakeColony();
            var structure = MakeStructure(bp.UUID, 2);
            structure.MiningSurveyResource = "Iron";
            colony.Structures.Add(structure);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].ProcessDetails, Is.EqualTo("Idle"));
        }

        [Test]
        public void IdleRefiner_NoResource_ReturnsNoResourceAssigned()
        {
            var pc = PlayerContext.getInstance();
            var bp = CreateBlueprint(BlueprintTypes.Refinery, "IdleRefiner");
            var colony = MakeColony();
            var structure = MakeStructure(bp.UUID, 1);
            colony.Structures.Add(structure);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].Type, Is.EqualTo(ActivityType.Refining));
            Assert.That(rows[0].ProcessDetails, Is.EqualTo("No resource assigned"));
        }

        [Test]
        public void IdleRefiner_ResourceAssignedNoTimer_ReturnsIdle()
        {
            var pc = PlayerContext.getInstance();
            var bp = CreateBlueprint(BlueprintTypes.Refinery, "IdleRefinerWithResource");
            var colony = MakeColony();
            var structure = MakeStructure(bp.UUID, 1);
            structure.RefiningResource = "Copper";
            colony.Structures.Add(structure);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].ProcessDetails, Is.EqualTo("Idle"));
        }

        [Test]
        public void IdleResearchLab_NoBlueprint_ReturnsNoBlueprintAssigned()
        {
            var pc = PlayerContext.getInstance();
            var bp = CreateBlueprint(BlueprintTypes.ResearchLaboratory, "IdleLab");
            var colony = MakeColony();
            var structure = MakeStructure(bp.UUID, 1);
            colony.Structures.Add(structure);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].Type, Is.EqualTo(ActivityType.Research));
            Assert.That(rows[0].ProcessDetails, Is.EqualTo("No blueprint assigned"));
        }

        [Test]
        public void IdleResearchLab_BlueprintAssignedNoTimer_ReturnsIdle()
        {
            var pc = PlayerContext.getInstance();
            var bp = CreateBlueprint(BlueprintTypes.ResearchLaboratory, "IdleLabWithBP");
            var colony = MakeColony();
            var structure = MakeStructure(bp.UUID, 1);
            structure.ResearchingBlueprintUUID = Guid.NewGuid().ToString();
            colony.Structures.Add(structure);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].ProcessDetails, Is.EqualTo("Idle"));
        }

        [Test]
        public void IdleManufactory_NoBlueprint_ReturnsNoBlueprintAssigned()
        {
            var pc = PlayerContext.getInstance();
            var bp = CreateBlueprint(BlueprintTypes.Manufactory, "IdleFactory");
            var colony = MakeColony();
            var structure = MakeStructure(bp.UUID, 1);
            colony.Structures.Add(structure);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].Type, Is.EqualTo(ActivityType.Manufacturing));
            Assert.That(rows[0].ProcessDetails, Is.EqualTo("No blueprint assigned"));
        }

        [Test]
        public void IdleManufactory_BlueprintAssignedNoTimer_ReturnsIdle()
        {
            var pc = PlayerContext.getInstance();
            var bp = CreateBlueprint(BlueprintTypes.Manufactory, "IdleFactoryWithBP");
            var colony = MakeColony();
            var structure = MakeStructure(bp.UUID, 1);
            structure.ManufacturingBlueprintUUID = Guid.NewGuid().ToString();
            colony.Structures.Add(structure);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].ProcessDetails, Is.EqualTo("Idle"));
        }

        [Test]
        public void IdleCommodityFactory_NoCommodity_ReturnsNoCommodityAssigned()
        {
            var pc = PlayerContext.getInstance();
            var bp = CreateBlueprint(BlueprintTypes.CommodityFactoryPrefix + "Agridome", "IdleCommodity");
            var colony = MakeColony();
            var structure = MakeStructure(bp.UUID, 1);
            colony.Structures.Add(structure);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].Type, Is.EqualTo(ActivityType.CommodityManufacturing));
            Assert.That(rows[0].ProcessDetails, Is.EqualTo("No commodity assigned"));
        }

        [Test]
        public void IdleCommodityFactory_CommodityAssignedNoTimer_ReturnsIdle()
        {
            var pc = PlayerContext.getInstance();
            var bp = CreateBlueprint(BlueprintTypes.CommodityFactoryPrefix + "Agridome", "IdleCommodityWithWork");
            var colony = MakeColony();
            var structure = MakeStructure(bp.UUID, 1);
            structure.ManufacturingCommodityName = "Electronics";
            colony.Structures.Add(structure);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].ProcessDetails, Is.EqualTo("Idle"));
        }

        [Test]
        public void MixedActiveAndIdle_OnlyIdleAppear()
        {
            var pc = PlayerContext.getInstance();
            var colony = MakeColony();

            // Active miner (should NOT appear)
            var activeBp = CreateBlueprint(BlueprintTypes.MiningRig, "ActiveMiner");
            var activeStructure = MakeStructure(activeBp.UUID, 1);
            activeStructure.ProcessCompletionTime = MakeActiveRepeatingTimer(3600);
            activeStructure.MiningSurveyResource = "Iron";
            colony.Structures.Add(activeStructure);

            // Idle refiner (should appear)
            var idleBp = CreateBlueprint(BlueprintTypes.Refinery, "IdleRefiner");
            var idleStructure = MakeStructure(idleBp.UUID, 2);
            colony.Structures.Add(idleStructure);

            // Not built manufactory (should NOT appear)
            var unbuiltBp = CreateBlueprint(BlueprintTypes.Manufactory, "UnbuiltFactory");
            var unbuiltStructure = MakeStructure(unbuiltBp.UUID, 3, built: false);
            colony.Structures.Add(unbuiltStructure);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(rows[0].Type, Is.EqualTo(ActivityType.Refining));
            Assert.That(rows[0].ProcessDetails, Is.EqualTo("No resource assigned"));
        }

        [Test]
        public void MultipleColonies_CollectsFromAll()
        {
            var pc = PlayerContext.getInstance();

            var colony1 = MakeColony("Sys1", "Col1");
            var bp1 = CreateBlueprint(BlueprintTypes.MiningRig, "Miner1");
            colony1.Structures.Add(MakeStructure(bp1.UUID, 1));

            var colony2 = MakeColony("Sys2", "Col2");
            var bp2 = CreateBlueprint(BlueprintTypes.Refinery, "Refiner2");
            colony2.Structures.Add(MakeStructure(bp2.UUID, 1));

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony1, colony2 }, pc);

            Assert.That(rows.Count, Is.EqualTo(2));
            Assert.That(rows.Any(r => r.SystemName == "Sys1"), Is.True);
            Assert.That(rows.Any(r => r.SystemName == "Sys2"), Is.True);
        }

        // -----------------------------------------------------------------------
        // Helpers for underutilized refiner tests
        // -----------------------------------------------------------------------

        private PlayerProfile CreateOwnerProfile(string ownerUUID, int extractionFocusLevel = 0)
        {
            var pc = PlayerContext.getInstance();
            var profile = new PlayerProfile
            {
                UUID = ownerUUID,
                Name = "TestOwner_" + ownerUUID.Substring(0, 6)
            };
            if (extractionFocusLevel > 0)
            {
                profile.GetSkill(SkillName.ExtractionFocus).Level = extractionFocusLevel;
            }
            pc.playerProfileList.Add(profile);
            return profile;
        }

        private Survey CreateSurvey(string resource, string purity, string amount)
        {
            var pc = PlayerContext.getInstance();
            var survey = new Survey("TestSurvey_" + Guid.NewGuid().ToString().Substring(0, 6));
            survey.UUID = Guid.NewGuid().ToString();
            survey.Resources[resource] = new SurveyResource(resource, purity, amount);
            pc.surveyList.Add(survey);
            return survey;
        }

        private ColonyStructure MakeActiveMiner(string blueprintUUID, int gameSeq,
            string surveyUUID, string surveyResource)
        {
            var structure = MakeStructure(blueprintUUID, gameSeq);
            structure.ProcessCompletionTime = MakeActiveRepeatingTimer(3600);
            structure.MiningSurvey = surveyUUID;
            structure.MiningSurveyResource = surveyResource;
            return structure;
        }

        private ColonyStructure MakeActiveRefiner(string blueprintUUID, int gameSeq,
            string resource, string purity)
        {
            var structure = MakeStructure(blueprintUUID, gameSeq);
            structure.ProcessCompletionTime = MakeActiveRepeatingTimer(3600);
            structure.RefiningResource = resource;
            structure.RefiningResourcePurity = purity;
            return structure;
        }

        private static void AddWarehouseResource(Colony colony, string resource, string purity, int quantity)
        {
            var item = new Item(ItemType.ItemTypeEnum.Resource, resource);
            item.UUID = Guid.NewGuid().ToString();
            item.BaseItemTypeID = resource;
            item.ResourcePurity = purity;
            item.Quantity = quantity;
            colony.Items.AddItem(item);
        }

        // -----------------------------------------------------------------------
        // Property 4: Underutilized refiner detection
        // Feature: activity-inactivity-mode, Property 4: Underutilized refiner detection
        // **Validates: Requirements 5.1, 5.2, 5.3, 5.5**
        // -----------------------------------------------------------------------

        [Test]
        public void Property4_UnderutilizedRefinerDetection()
        {
            // 1 miner (10/h Low) + 2 refiners (25/cycle each)
            // Total consumption (50) > mining output (10), so second refiner (highest gameSeq) flagged
            var pc = PlayerContext.getInstance();
            string ownerUUID = Guid.NewGuid().ToString();
            CreateOwnerProfile(ownerUUID);

            var minerBp = CreateBlueprint(BlueprintTypes.MiningRig, "Miner");
            var refinerBp = CreateBlueprint(BlueprintTypes.Refinery, "Refiner");

            var survey = CreateSurvey("Iron", "Low", "10");

            var colony = MakeColony("Sys", "Col");
            colony.OwnerUUID = ownerUUID;

            // Active miner mining Iron (Low) at 10/h
            var miner = MakeActiveMiner(minerBp.UUID, 1, survey.UUID, "Iron");
            colony.Structures.Add(miner);

            // Two active refiners refining Iron (Low) at 25/cycle each
            var refiner1 = MakeActiveRefiner(refinerBp.UUID, 2, "Iron", "Low");
            colony.Structures.Add(refiner1);
            var refiner2 = MakeActiveRefiner(refinerBp.UUID, 3, "Iron", "Low");
            colony.Structures.Add(refiner2);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);

            // Only underutilized rows (idle structures are not expected since all are active)
            var underutilized = rows.Where(r => r.ProcessDetails.StartsWith("Underutilized")).ToList();
            Assert.That(underutilized.Count, Is.EqualTo(2), "Expected 2 underutilized refiners");

            // Refiner #2 (gameSeq=2, lowest priority after #1 doesn't exist) gets 10/25
            // Refiner #3 (gameSeq=3, highest) gets 0/25
            var refiner2Row = underutilized.FirstOrDefault(r => r.SourceName.Contains("#2"));
            var refiner3Row = underutilized.FirstOrDefault(r => r.SourceName.Contains("#3"));

            Assert.That(refiner3Row, Is.Not.Null, "Refiner #3 should be underutilized");
            Assert.That(refiner3Row.ProcessDetails, Is.EqualTo("Underutilized: 0/25 per cycle"));
            Assert.That(refiner3Row.Type, Is.EqualTo(ActivityType.Refining));

            Assert.That(refiner2Row, Is.Not.Null, "Refiner #2 should be underutilized");
            Assert.That(refiner2Row.ProcessDetails, Is.EqualTo("Underutilized: 10/25 per cycle"));
        }

        [Test]
        public void UnderutilizedRefiner_FirstRefinerGetsPartialSupply()
        {
            // 1 miner (10/h Low) + 2 refiners (25/cycle each)
            // Refiner #2 (gameSeq=2) gets 10, refiner #3 (gameSeq=3) gets 0
            var pc = PlayerContext.getInstance();
            string ownerUUID = Guid.NewGuid().ToString();
            CreateOwnerProfile(ownerUUID);

            var minerBp = CreateBlueprint(BlueprintTypes.MiningRig, "Miner");
            var refinerBp = CreateBlueprint(BlueprintTypes.Refinery, "Refiner");
            var survey = CreateSurvey("Iron", "Low", "10");

            var colony = MakeColony();
            colony.OwnerUUID = ownerUUID;

            colony.Structures.Add(MakeActiveMiner(minerBp.UUID, 1, survey.UUID, "Iron"));
            colony.Structures.Add(MakeActiveRefiner(refinerBp.UUID, 2, "Iron", "Low"));
            colony.Structures.Add(MakeActiveRefiner(refinerBp.UUID, 3, "Iron", "Low"));

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);
            var underutilized = rows.Where(r => r.ProcessDetails.StartsWith("Underutilized")).ToList();

            // Both refiners are underutilized: #2 gets 10/25, #3 gets 0/25
            Assert.That(underutilized.Count, Is.EqualTo(2));

            var refiner2Row = underutilized.FirstOrDefault(r => r.SourceName.Contains("#2"));
            var refiner3Row = underutilized.FirstOrDefault(r => r.SourceName.Contains("#3"));

            Assert.That(refiner2Row, Is.Not.Null, "Refiner #2 should be underutilized");
            Assert.That(refiner3Row, Is.Not.Null, "Refiner #3 should be underutilized");
            Assert.That(refiner2Row.ProcessDetails, Is.EqualTo("Underutilized: 10/25 per cycle"));
            Assert.That(refiner3Row.ProcessDetails, Is.EqualTo("Underutilized: 0/25 per cycle"));
        }

        [Test]
        public void UnderutilizedRefiner_SufficientMiningSupply_NoneFlag()
        {
            // 1 miner (60/h Low) + 2 refiners (25/cycle each) = 50 total consumption <= 60 output
            var pc = PlayerContext.getInstance();
            string ownerUUID = Guid.NewGuid().ToString();
            CreateOwnerProfile(ownerUUID);

            var minerBp = CreateBlueprint(BlueprintTypes.MiningRig, "Miner");
            var refinerBp = CreateBlueprint(BlueprintTypes.Refinery, "Refiner");
            var survey = CreateSurvey("Iron", "Low", "60");

            var colony = MakeColony();
            colony.OwnerUUID = ownerUUID;

            colony.Structures.Add(MakeActiveMiner(minerBp.UUID, 1, survey.UUID, "Iron"));
            colony.Structures.Add(MakeActiveRefiner(refinerBp.UUID, 2, "Iron", "Low"));
            colony.Structures.Add(MakeActiveRefiner(refinerBp.UUID, 3, "Iron", "Low"));

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);
            var underutilized = rows.Where(r => r.ProcessDetails.StartsWith("Underutilized")).ToList();

            Assert.That(underutilized.Count, Is.EqualTo(0), "No refiners should be underutilized");
        }

        [Test]
        public void UnderutilizedRefiner_SyntheticRecipe_UsesRecipeConsumeRate()
        {
            // Synthetic refiner consuming Lanthanides (Refined) at 1250/cycle
            var pc = PlayerContext.getInstance();
            string ownerUUID = Guid.NewGuid().ToString();
            CreateOwnerProfile(ownerUUID);

            var minerBp = CreateBlueprint(BlueprintTypes.MiningRig, "Miner");
            var refinerBp = CreateBlueprint(BlueprintTypes.Refinery, "SyntheticRefiner");

            // Miner producing 100/h of Lanthanides (Refined) — way less than 1250
            var survey = CreateSurvey("Lanthanides", GameConstants.PurityRefined, "100");

            var colony = MakeColony();
            colony.OwnerUUID = ownerUUID;

            colony.Structures.Add(MakeActiveMiner(minerBp.UUID, 1, survey.UUID, "Lanthanides"));

            // Synthetic refiner: Lanthanides + Refined triggers RefiningRecipes.FindByInput
            var synRefiner = MakeActiveRefiner(refinerBp.UUID, 2, "Lanthanides", GameConstants.PurityRefined);
            colony.Structures.Add(synRefiner);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);
            var underutilized = rows.Where(r => r.ProcessDetails.StartsWith("Underutilized")).ToList();

            Assert.That(underutilized.Count, Is.EqualTo(1));
            Assert.That(underutilized[0].ProcessDetails, Is.EqualTo("Underutilized: 100/1250 per cycle"));
        }

        [Test]
        public void UnderutilizedRefiner_ExtractionFocusBonus_AppliedToMiningRate()
        {
            // Miner at 10/h with ExtractionFocus level 5 => 10 * 1.05 = 10.5
            // 1 refiner at 25/cycle => underutilized with 10/25 (floor of 10.5)
            var pc = PlayerContext.getInstance();
            string ownerUUID = Guid.NewGuid().ToString();
            CreateOwnerProfile(ownerUUID, extractionFocusLevel: 5);

            var minerBp = CreateBlueprint(BlueprintTypes.MiningRig, "Miner");
            var refinerBp = CreateBlueprint(BlueprintTypes.Refinery, "Refiner");
            var survey = CreateSurvey("Iron", "Low", "10");

            var colony = MakeColony();
            colony.OwnerUUID = ownerUUID;

            colony.Structures.Add(MakeActiveMiner(minerBp.UUID, 1, survey.UUID, "Iron"));
            colony.Structures.Add(MakeActiveRefiner(refinerBp.UUID, 2, "Iron", "Low"));

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);
            var underutilized = rows.Where(r => r.ProcessDetails.StartsWith("Underutilized")).ToList();

            // 10 * 1.05 = 10.5, floor = 10
            Assert.That(underutilized.Count, Is.EqualTo(1));
            Assert.That(underutilized[0].ProcessDetails, Is.EqualTo("Underutilized: 10/25 per cycle"));
        }

        [Test]
        public void UnderutilizedRefiner_MultipleResources_HandledIndependently()
        {
            // Two resources: Iron (Low) and Copper (Low), each with 1 miner + 1 refiner
            // Iron miner at 30/h, Copper miner at 10/h
            // Iron refiner at 25/cycle (sufficient), Copper refiner at 25/cycle (underutilized)
            var pc = PlayerContext.getInstance();
            string ownerUUID = Guid.NewGuid().ToString();
            CreateOwnerProfile(ownerUUID);

            var minerBp = CreateBlueprint(BlueprintTypes.MiningRig, "Miner");
            var refinerBp = CreateBlueprint(BlueprintTypes.Refinery, "Refiner");

            var ironSurvey = CreateSurvey("Iron", "Low", "30");
            var copperSurvey = CreateSurvey("Copper", "Low", "10");

            var colony = MakeColony();
            colony.OwnerUUID = ownerUUID;

            // Iron: sufficient
            colony.Structures.Add(MakeActiveMiner(minerBp.UUID, 1, ironSurvey.UUID, "Iron"));
            colony.Structures.Add(MakeActiveRefiner(refinerBp.UUID, 2, "Iron", "Low"));

            // Copper: underutilized
            colony.Structures.Add(MakeActiveMiner(minerBp.UUID, 3, copperSurvey.UUID, "Copper"));
            colony.Structures.Add(MakeActiveRefiner(refinerBp.UUID, 4, "Copper", "Low"));

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);
            var underutilized = rows.Where(r => r.ProcessDetails.StartsWith("Underutilized")).ToList();

            Assert.That(underutilized.Count, Is.EqualTo(1), "Only Copper refiner should be underutilized");
            Assert.That(underutilized[0].SourceName, Does.Contain("#4"));
            Assert.That(underutilized[0].ProcessDetails, Is.EqualTo("Underutilized: 10/25 per cycle"));
        }

        // -----------------------------------------------------------------------
        // Property 5: Warehouse stockpile exemption
        // Feature: activity-inactivity-mode, Property 5: Warehouse stockpile exemption
        // **Validates: Requirements 5.4**
        // -----------------------------------------------------------------------

        [Test]
        public void Property5_WarehouseStockpileExemption()
        {
            // Same as Property4 test but with 25+ units in warehouse — no underutilized flag
            var pc = PlayerContext.getInstance();
            string ownerUUID = Guid.NewGuid().ToString();
            CreateOwnerProfile(ownerUUID);

            var minerBp = CreateBlueprint(BlueprintTypes.MiningRig, "Miner");
            var refinerBp = CreateBlueprint(BlueprintTypes.Refinery, "Refiner");
            var survey = CreateSurvey("Iron", "Low", "10");

            var colony = MakeColony();
            colony.OwnerUUID = ownerUUID;

            colony.Structures.Add(MakeActiveMiner(minerBp.UUID, 1, survey.UUID, "Iron"));
            colony.Structures.Add(MakeActiveRefiner(refinerBp.UUID, 2, "Iron", "Low"));
            colony.Structures.Add(MakeActiveRefiner(refinerBp.UUID, 3, "Iron", "Low"));

            // Add 25 units of Iron (Low) to warehouse — enough for one cycle
            AddWarehouseResource(colony, "Iron", "Low", 25);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);
            var underutilized = rows.Where(r => r.ProcessDetails.StartsWith("Underutilized")).ToList();

            Assert.That(underutilized.Count, Is.EqualTo(0),
                "No refiners should be underutilized when warehouse has sufficient stockpile");
        }

        [Test]
        public void WarehouseExemption_InsufficientStockpile_StillFlagged()
        {
            // Warehouse has 24 units (less than 25 per cycle) — refiner still flagged
            var pc = PlayerContext.getInstance();
            string ownerUUID = Guid.NewGuid().ToString();
            CreateOwnerProfile(ownerUUID);

            var minerBp = CreateBlueprint(BlueprintTypes.MiningRig, "Miner");
            var refinerBp = CreateBlueprint(BlueprintTypes.Refinery, "Refiner");
            var survey = CreateSurvey("Iron", "Low", "10");

            var colony = MakeColony();
            colony.OwnerUUID = ownerUUID;

            colony.Structures.Add(MakeActiveMiner(minerBp.UUID, 1, survey.UUID, "Iron"));
            colony.Structures.Add(MakeActiveRefiner(refinerBp.UUID, 2, "Iron", "Low"));
            colony.Structures.Add(MakeActiveRefiner(refinerBp.UUID, 3, "Iron", "Low"));

            // Only 24 units — not enough
            AddWarehouseResource(colony, "Iron", "Low", 24);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);
            var underutilized = rows.Where(r => r.ProcessDetails.StartsWith("Underutilized")).ToList();

            Assert.That(underutilized.Count, Is.GreaterThan(0),
                "Refiners should still be flagged when warehouse has insufficient stockpile");
        }

        [Test]
        public void WarehouseExemption_SyntheticRefiner_NeedsRecipeConsumeRate()
        {
            // Synthetic refiner needs 1250 units in warehouse to be exempt
            var pc = PlayerContext.getInstance();
            string ownerUUID = Guid.NewGuid().ToString();
            CreateOwnerProfile(ownerUUID);

            var minerBp = CreateBlueprint(BlueprintTypes.MiningRig, "Miner");
            var refinerBp = CreateBlueprint(BlueprintTypes.Refinery, "SynRefiner");
            var survey = CreateSurvey("Lanthanides", GameConstants.PurityRefined, "100");

            var colony = MakeColony();
            colony.OwnerUUID = ownerUUID;

            colony.Structures.Add(MakeActiveMiner(minerBp.UUID, 1, survey.UUID, "Lanthanides"));
            colony.Structures.Add(MakeActiveRefiner(refinerBp.UUID, 2, "Lanthanides", GameConstants.PurityRefined));

            // Add 1250 units — exactly enough for synthetic recipe
            AddWarehouseResource(colony, "Lanthanides", GameConstants.PurityRefined, 1250);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);
            var underutilized = rows.Where(r => r.ProcessDetails.StartsWith("Underutilized")).ToList();

            Assert.That(underutilized.Count, Is.EqualTo(0),
                "Synthetic refiner should be exempt with 1250 units in warehouse");
        }

        [Test]
        public void WarehouseExemption_SyntheticRefiner_InsufficientStockpile()
        {
            // Synthetic refiner with only 1249 units — still flagged
            var pc = PlayerContext.getInstance();
            string ownerUUID = Guid.NewGuid().ToString();
            CreateOwnerProfile(ownerUUID);

            var minerBp = CreateBlueprint(BlueprintTypes.MiningRig, "Miner");
            var refinerBp = CreateBlueprint(BlueprintTypes.Refinery, "SynRefiner");
            var survey = CreateSurvey("Lanthanides", GameConstants.PurityRefined, "100");

            var colony = MakeColony();
            colony.OwnerUUID = ownerUUID;

            colony.Structures.Add(MakeActiveMiner(minerBp.UUID, 1, survey.UUID, "Lanthanides"));
            colony.Structures.Add(MakeActiveRefiner(refinerBp.UUID, 2, "Lanthanides", GameConstants.PurityRefined));

            AddWarehouseResource(colony, "Lanthanides", GameConstants.PurityRefined, 1249);

            var rows = ColonyInactivityCollector.CollectInactivities(new[] { colony }, pc);
            var underutilized = rows.Where(r => r.ProcessDetails.StartsWith("Underutilized")).ToList();

            Assert.That(underutilized.Count, Is.EqualTo(1),
                "Synthetic refiner should still be flagged with insufficient warehouse stockpile");
        }
    }
}
