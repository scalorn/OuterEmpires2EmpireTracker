using System;
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
    /// Feature: colony-admin-summary, Property 2: Completion time dual display
    /// </summary>
    [TestFixture]
    public class ColonyAdminReportBuilderTimePropertyTests
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
        /// Feature: colony-admin-summary, Property 2: Completion time dual display.
        /// For any non-repeating activity row with positive TimeRemaining, the output
        /// SHALL contain both a relative countdown string and a local timezone time string.
        /// **Validates: Requirements 2.2, 4.5, 4.10**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property CompletionTimeDualDisplay_ContainsBothCountdownAndLocalTime()
        {
            var secondsGen = Gen.Choose(60, 86400);

            return Prop.ForAll(secondsGen.ToArbitrary(), seconds =>
            {
                var pc = PlayerContext.GetInstance();
                foreach (var item in pc.BlueprintList.ToList()) pc.RemoveBlueprint(item);

                var bp = CreateBlueprint(BlueprintTypes.MiningRig, "Builder");
                var colony = MakeColony();
                var structure = MakeStructure(bp.UUID, 1);
                structure.BuildCompletionTime = new CountDownTime();
                structure.BuildCompletionTime.TimeRemaining = seconds;
                colony.Structures.Add(structure);

                string rtf = ColonyAdminReportBuilder.BuildReport(colony, pc);

                // Should contain a countdown (e.g. "1h 30m 0s" or similar)
                bool hasCountdown = rtf.Contains("m") || rtf.Contains("h") || rtf.Contains("s");
                if (!hasCountdown)
                    return false.Label("Missing countdown string");

                // Should contain a local time (HH:mm format)
                DateTime expectedLocal = structure.BuildCompletionTime.EndTime.ToLocalTime();
                string expectedTimeStr = expectedLocal.ToString("HH:mm");
                if (!rtf.Contains(expectedTimeStr))
                    return false.Label($"Missing local time '{expectedTimeStr}'");

                return true.Label("Both countdown and local time present");
            });
        }

        private static ColonyStructure MakeStructure(string blueprintUUID, int gameSeq)
        {
            var s = new ColonyStructure();
            s.UUID = Guid.NewGuid().ToString();
            s.FlatpackBlueprintUUID = blueprintUUID;
            s.DisplaySequence = gameSeq;
            s.Properties.SetProperty(GameConstants.PropBuilt, true);
            s.Properties.SetProperty(GameConstants.PropOnline, true);
            return s;
        }

        private static Colony MakeColony()
        {
            return new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                SystemName = "TestSystem",
                ColonyName = "TestColony",
                LastImportDateTime = SurveyDateTimeParser.ToIsoString(SystemClock.UtcNow)
            };
        }

        private OE2EmpireTracker.Models.Blueprint CreateBlueprint(string bpType, string name)
        {
            var bp = new OE2EmpireTracker.Models.Blueprint(name);
            bp.UUID = Guid.NewGuid().ToString();
            bp.BluePrintType = bpType;
            PlayerContext.GetInstance().AddBlueprint(bp);
            return bp;
        }
    }
}
