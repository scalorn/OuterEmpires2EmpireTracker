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
    /// Feature: colony-admin-summary, Property 10: Refining aggregation -- one row per resource+purity
    /// </summary>
    [TestFixture]
    public class ColonyAdminReportBuilderRefiningPropertyTests
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
            PlayerContext.GetInstance().AddBlueprint(bp);
            return bp;
        }

        private static ColonyStructure MakeActiveRefiner(string blueprintUUID, int gameSeq,
            string resource, string purity)
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

        private static readonly string[] Purities = { "Low", "Medium", "High" };
        private static readonly int[] PurityMultipliers = { 1, 3, 5 };

        /// <summary>
        /// Feature: colony-admin-summary, Property 10: Refining aggregation -- one row per resource+purity.
        /// For any colony with N active refiners on the same (InputResource, InputPurity) combination,
        /// the activity section SHALL contain exactly one refining summary row for that combination,
        /// and the displayed consume and produce rates SHALL equal the sums of individual refiner rates.
        /// **Validates: Requirements 4.7**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property RefiningAggregation_OneRowPerResourcePurity()
        {
            var refinerCountGen = Gen.Choose(1, 5);
            var purityIndexGen = Gen.Choose(0, 2);

            var gen = from refinerCount in refinerCountGen
                      from purityIdx in purityIndexGen
                      select new { RefinerCount = refinerCount, PurityIndex = purityIdx };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var pc = PlayerContext.GetInstance();
                foreach (var item in pc.BlueprintList.ToList()) pc.RemoveBlueprint(item);

                string resource = "Calcium";
                string purity = Purities[data.PurityIndex];

                var colony = MakeColony();
                for (int i = 0; i < data.RefinerCount; i++)
                {
                    var bp = CreateBlueprint(BlueprintTypes.Refinery, "Refiner" + i);
                    colony.Structures.Add(MakeActiveRefiner(bp.UUID, i + 1, resource, purity));
                }

                string rtf = ColonyAdminReportBuilder.BuildReport(colony, pc);

                // Count occurrences of the resource+purity pattern
                string pattern = $"{resource} ({purity})";
                int occurrences = CountOccurrences(rtf, pattern);

                if (occurrences != 1)
                    return false.Label($"Expected 1 occurrence of '{pattern}', found {occurrences}");

                // Verify aggregated rates
                int baseRate = GameConstants.RefiningBaseRate;
                int expectedConsume = baseRate * data.RefinerCount;
                int expectedProduce = baseRate * PurityMultipliers[data.PurityIndex] * data.RefinerCount;

                string ratePattern = $"{expectedConsume:F2}:{expectedProduce:F2}";
                if (!rtf.Contains(ratePattern))
                    return false.Label($"Expected rate '{ratePattern}' not found in report");

                // Verify count prefix
                string countPattern = $"{data.RefinerCount}x";
                if (!rtf.Contains(countPattern))
                    return false.Label($"Expected count '{countPattern}' not found in report");

                return true.Label("Refining aggregation correct");
            });
        }

        private static int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
            {
                count++;
                index += pattern.Length;
            }

            return count;
        }
    }
}
