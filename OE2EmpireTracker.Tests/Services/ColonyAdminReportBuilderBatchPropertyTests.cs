using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Feature: colony-admin-summary, Property 8: Multi-quantity manufacturing shows next-item and batch completion
    /// </summary>
    [TestFixture]
    public class ColonyAdminReportBuilderBatchPropertyTests
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

        private OE2EmpireTracker.Models.Blueprint CreateBlueprint(string bpType, string name)
        {
            var bp = new OE2EmpireTracker.Models.Blueprint(name);
            bp.UUID = Guid.NewGuid().ToString();
            bp.BluePrintType = bpType;
            PlayerContext.GetInstance().BlueprintList.Add(bp);
            return bp;
        }

        private static ColonyStructure MakeStructure(string blueprintUUID, int gameSeq)
        {
            var structure = new ColonyStructure();
            structure.UUID = Guid.NewGuid().ToString();
            structure.FlatpackBlueprintUUID = blueprintUUID;
            structure.displaySequence = gameSeq;
            structure.Properties.setProperty(GameConstants.PropBuilt, true);
            structure.Properties.setProperty(GameConstants.PropOnline, true);
            return structure;
        }

        private static Colony MakeColony()
        {
            return new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                SystemName = "TestSystem",
                ColonyName = "TestColony",
                LastImportDateTime = SurveyDateTimeParser.ToIsoString(DateTime.UtcNow)
            };
        }

        /// <summary>
        /// Feature: colony-admin-summary, Property 8: Multi-quantity manufacturing shows next-item and batch completion.
        /// For any Manufacturing or CommodityManufacturing structure with ManufacturingQuantity > 1
        /// and ManufacturingCompleted &lt; ManufacturingQuantity - 1, the report SHALL contain both
        /// a next-item completion time and a full-batch completion time.
        /// **Validates: Requirements 4.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MultiQuantityManufacturing_ShowsBothNextAndBatchCompletion()
        {
            var quantityGen = Gen.Choose(2, 20);
            var intervalGen = Gen.Choose(60, 7200);
            var remainingGen = Gen.Choose(10, 3600);
            var isCommodityGen = Arb.Default.Bool().Generator;

            var gen = from qty in quantityGen
                      from interval in intervalGen
                      from remaining in remainingGen
                      from isCommodity in isCommodityGen
                      // Ensure completed < qty - 1 so there are remaining cycles
                      from completed in Gen.Choose(0, Math.Max(0, qty - 2))
                      select new { Quantity = qty, Interval = interval, Remaining = remaining, IsCommodity = isCommodity, Completed = completed };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var pc = PlayerContext.GetInstance();
                pc.BlueprintList.Clear();

                string bpType = data.IsCommodity
                    ? BlueprintTypes.CommodityFactoryPrefix + "Agridome"
                    : BlueprintTypes.Manufactory;
                var bp = CreateBlueprint(bpType, "TestMfg");

                var colony = MakeColony();
                var structure = MakeStructure(bp.UUID, 1);
                structure.ManufacturingQuantity = data.Quantity;
                structure.ManufacturingCompleted = data.Completed;
                structure.ManufacturingBlueprintUUID = data.IsCommodity ? null : Guid.NewGuid().ToString();
                structure.ManufacturingCommodityName = data.IsCommodity ? "TestCommodity" : null;

                var timer = new CountDownTime();
                timer.RepeatIntervalSeconds = data.Interval;
                timer.TimeRemaining = data.Remaining;
                structure.ProcessCompletionTime = timer;

                colony.Structures.Add(structure);

                string rtf = ColonyAdminReportBuilder.BuildReport(colony, pc);

                // Should contain "Next:" for next-item completion
                bool hasNext = rtf.Contains("Next:");
                if (!hasNext)
                    return false.Label("Missing 'Next:' in report");

                // Should contain "Batch:" for batch completion
                bool hasBatch = rtf.Contains("Batch:");
                if (!hasBatch)
                    return false.Label("Missing 'Batch:' in report");

                return true.Label("Both Next and Batch present");
            });
        }

        /// <summary>
        /// Single-item manufacturing (ManufacturingQuantity == 1) should NOT show batch line.
        /// </summary>
        [Test]
        public void SingleItemManufacturing_NoBatchLine()
        {
            var pc = PlayerContext.GetInstance();
            pc.BlueprintList.Clear();

            var bp = CreateBlueprint(BlueprintTypes.Manufactory, "SingleMfg");
            var colony = MakeColony();
            var structure = MakeStructure(bp.UUID, 1);
            structure.ManufacturingQuantity = 1;
            structure.ManufacturingCompleted = 0;
            structure.ManufacturingBlueprintUUID = Guid.NewGuid().ToString();

            var timer = new CountDownTime();
            timer.TimeRemaining = 600;
            structure.ProcessCompletionTime = timer;

            colony.Structures.Add(structure);

            string rtf = ColonyAdminReportBuilder.BuildReport(colony, pc);

            Assert.That(rtf.Contains("Next:"), Is.True, "Should have Next: line");
            Assert.That(rtf.Contains("Batch:"), Is.False, "Should NOT have Batch: line for single item");
        }

        /// <summary>
        /// Last cycle manufacturing (ManufacturingCompleted == ManufacturingQuantity - 1) should NOT show batch line.
        /// </summary>
        [Test]
        public void LastCycleManufacturing_NoBatchLine()
        {
            var pc = PlayerContext.GetInstance();
            pc.BlueprintList.Clear();

            var bp = CreateBlueprint(BlueprintTypes.Manufactory, "LastCycleMfg");
            var colony = MakeColony();
            var structure = MakeStructure(bp.UUID, 1);
            structure.ManufacturingQuantity = 5;
            structure.ManufacturingCompleted = 4;
            structure.ManufacturingBlueprintUUID = Guid.NewGuid().ToString();

            var timer = new CountDownTime();
            timer.RepeatIntervalSeconds = 600;
            timer.TimeRemaining = 300;
            structure.ProcessCompletionTime = timer;

            colony.Structures.Add(structure);

            string rtf = ColonyAdminReportBuilder.BuildReport(colony, pc);

            Assert.That(rtf.Contains("Next:"), Is.True, "Should have Next: line");
            Assert.That(rtf.Contains("Batch:"), Is.False, "Should NOT have Batch: line on last cycle");
        }
    }
}
