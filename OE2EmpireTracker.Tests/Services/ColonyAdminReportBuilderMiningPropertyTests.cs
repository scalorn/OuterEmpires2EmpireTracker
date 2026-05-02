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
    /// Feature: colony-admin-summary, Property 9: Mining aggregation -- one row per resource+purity
    /// </summary>
    [TestFixture]
    public class ColonyAdminReportBuilderMiningPropertyTests
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

        /// <summary>
        /// Feature: colony-admin-summary, Property 9: Mining aggregation -- one row per resource+purity.
        /// For any colony with N active miners on the same (Resource, Purity) combination,
        /// the activity section SHALL contain exactly one mining summary row for that combination,
        /// and the displayed rate SHALL equal the sum of individual miner rates.
        /// **Validates: Requirements 4.6**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property MiningAggregation_OneRowPerResourcePurity()
        {
            var minerCountGen = Gen.Choose(1, 5);
            var amountGen = Gen.Choose(10, 200);

            var gen = from minerCount in minerCountGen
                      from amount in amountGen
                      select new { MinerCount = minerCount, Amount = amount };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var pc = PlayerContext.GetInstance();
                foreach (var item in pc.BlueprintList.ToList()) pc.RemoveBlueprint(item);
                foreach (var item in pc.SurveyList.ToList()) pc.RemoveSurvey(item);

                string resource = "Halogen";
                string purity = "High";
                var survey = CreateSurvey(resource, purity, data.Amount.ToString());

                var colony = MakeColony();
                for (int i = 0; i < data.MinerCount; i++)
                {
                    var bp = CreateBlueprint(BlueprintTypes.MiningRig, "Miner" + i);
                    colony.Structures.Add(MakeActiveMiner(bp.UUID, i + 1, survey.UUID, resource));
                }

                string rtf = ColonyAdminReportBuilder.BuildReport(colony, pc);

                // Count occurrences of the resource+purity pattern in the Mining section
                // The pattern is "Resource (Purity)" in the mining summary
                string pattern = $"{resource} ({purity})";
                int occurrences = CountOccurrences(rtf, pattern);

                if (occurrences != 1)
                    return false.Label($"Expected 1 occurrence of '{pattern}', found {occurrences}");

                // Verify the aggregated rate
                decimal expectedRate = (decimal)data.Amount * data.MinerCount;
                string ratePattern = $"{expectedRate:F2}/h";
                if (!rtf.Contains(ratePattern))
                    return false.Label($"Expected rate '{ratePattern}' not found in report");

                return true.Label("Mining aggregation correct");
            });
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

        private OE2EmpireTracker.Models.Blueprint CreateBlueprint(string bpType, string name)
        {
            var bp = new OE2EmpireTracker.Models.Blueprint(name);
            bp.UUID = Guid.NewGuid().ToString();
            bp.BluePrintType = bpType;
            PlayerContext.GetInstance().AddBlueprint(bp);
            return bp;
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
    }
}
