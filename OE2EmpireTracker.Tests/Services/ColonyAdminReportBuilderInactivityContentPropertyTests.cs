using System;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Feature: colony-admin-summary, Property 5: Inactivity rows contain header and details
    /// </summary>
    [TestFixture]
    public class ColonyAdminReportBuilderInactivityContentPropertyTests
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
        /// Feature: colony-admin-summary, Property 5: Inactivity rows contain header and details.
        /// Each inactivity group with rows SHALL have a header and each row SHALL contain
        /// the source name and process details.
        /// **Validates: Requirements 3.3, 3.4**
        /// </summary>
        [Test]
        public void InactivityRows_ContainHeaderAndDetails()
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

            // Idle miner with no survey
            var bp = new OE2EmpireTracker.Models.Blueprint("TestMiner");
            bp.UUID = Guid.NewGuid().ToString();
            bp.BluePrintType = BlueprintTypes.MiningRig;
            pc.BlueprintList.Add(bp);

            var s = new ColonyStructure();
            s.UUID = Guid.NewGuid().ToString();
            s.FlatpackBlueprintUUID = bp.UUID;
            s.displaySequence = 5;
            s.Properties.setProperty(GameConstants.PropBuilt, true);
            s.Properties.setProperty(GameConstants.PropOnline, true);
            colony.Structures.Add(s);

            string rtf = ColonyAdminReportBuilder.BuildReport(colony, pc);

            // Should contain the group header
            Assert.That(rtf.Contains("Idle Mining"), Is.True, "Missing 'Idle Mining' header");

            // Should contain the source name
            string expectedSource = $"#{s.displaySequence} {bp.ExtendedName}";
            Assert.That(rtf.Contains(expectedSource), Is.True, $"Missing source name '{expectedSource}'");

            // Should contain process details
            Assert.That(rtf.Contains("No survey assigned"), Is.True, "Missing process details");
        }
    }
}
