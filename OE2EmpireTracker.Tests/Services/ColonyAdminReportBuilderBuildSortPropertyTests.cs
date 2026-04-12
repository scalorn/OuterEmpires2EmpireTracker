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
    /// Feature: colony-admin-summary, Property 3: Building rows sorted by soonest completion
    /// </summary>
    [TestFixture]
    public class ColonyAdminReportBuilderBuildSortPropertyTests
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
        /// Feature: colony-admin-summary, Property 3: Building rows sorted by soonest completion.
        /// **Validates: Requirements 2.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property BuildingRows_SortedBySoonestCompletion()
        {
            var countGen = Gen.Choose(2, 5);
            var secondsGen = Gen.Choose(60, 86400);

            var gen = from count in countGen
                      from seconds in Gen.ListOf(count, secondsGen)
                      select new { Count = count, Seconds = seconds.ToList() };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var pc = PlayerContext.GetInstance();
                pc.BlueprintList.Clear();

                var colony = new Colony
                {
                    UUID = Guid.NewGuid().ToString(),
                    SystemName = "TestSystem",
                    ColonyName = "TestColony",
                    LastImportDateTime = SurveyDateTimeParser.ToIsoString(DateTime.UtcNow)
                };

                var names = new List<string>();
                for (int i = 0; i < data.Count; i++)
                {
                    string name = $"Struct{i}";
                    names.Add(name);
                    var bp = new OE2EmpireTracker.Models.Blueprint(name);
                    bp.UUID = Guid.NewGuid().ToString();
                    bp.BluePrintType = BlueprintTypes.MiningRig;
                    pc.BlueprintList.Add(bp);

                    var s = new ColonyStructure();
                    s.UUID = Guid.NewGuid().ToString();
                    s.FlatpackBlueprintUUID = bp.UUID;
                    s.displaySequence = i + 1;
                    s.Properties.setProperty(GameConstants.PropBuilt, true);
                    s.Properties.setProperty(GameConstants.PropOnline, true);
                    s.BuildCompletionTime = new CountDownTime();
                    s.BuildCompletionTime.TimeRemaining = data.Seconds[i];
                    colony.Structures.Add(s);
                }

                string rtf = ColonyAdminReportBuilder.BuildReport(colony, pc);

                // Verify names appear in order of ascending seconds
                var sortedPairs = names.Zip(data.Seconds, (n, s) => new { Name = n, Secs = s })
                    .OrderBy(p => p.Secs).ToList();

                int lastIdx = -1;
                foreach (var pair in sortedPairs)
                {
                    int idx = rtf.IndexOf(pair.Name, StringComparison.Ordinal);
                    if (idx < lastIdx)
                        return false.Label($"'{pair.Name}' ({pair.Secs}s) appears before a shorter timer");
                    lastIdx = idx;
                }

                return true.Label("Building rows in ascending order");
            });
        }
    }
}
