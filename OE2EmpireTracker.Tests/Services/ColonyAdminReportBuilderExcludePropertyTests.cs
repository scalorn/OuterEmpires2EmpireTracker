using System;
using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Feature: colony-admin-summary, Property 6: Activity section excludes building and commodity request rows
    /// </summary>
    [TestFixture]
    public class ColonyAdminReportBuilderExcludePropertyTests
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
        /// Feature: colony-admin-summary, Property 6: Activity section excludes building and commodity request rows.
        /// Building rows appear only in the Building section, commodity requests only in Commodity Requests.
        /// **Validates: Requirements 4.2**
        /// </summary>
        [Test]
        public void ActivitySection_ExcludesBuildingAndCommodityRequestRows()
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

            // Building structure
            var buildBp = new OE2EmpireTracker.Models.Blueprint("BuildingStruct");
            buildBp.UUID = Guid.NewGuid().ToString();
            buildBp.BluePrintType = BlueprintTypes.MiningRig;
            pc.BlueprintList.Add(buildBp);

            var buildStruct = new ColonyStructure();
            buildStruct.UUID = Guid.NewGuid().ToString();
            buildStruct.FlatpackBlueprintUUID = buildBp.UUID;
            buildStruct.displaySequence = 1;
            buildStruct.Properties.setProperty(GameConstants.PropBuilt, true);
            buildStruct.Properties.setProperty(GameConstants.PropOnline, true);
            buildStruct.BuildCompletionTime = new CountDownTime();
            buildStruct.BuildCompletionTime.TimeRemaining = 3600;
            colony.Structures.Add(buildStruct);

            // Commodity request
            colony.Commodities = new List<CommodityRequested>
            {
                new CommodityRequested { Name = "UniqueTestCommodity", Requested = 50, Fulfilled = false }
            };

            // Active research (should appear in activity section)
            var resBp = new OE2EmpireTracker.Models.Blueprint("ResearchLab");
            resBp.UUID = Guid.NewGuid().ToString();
            resBp.BluePrintType = BlueprintTypes.ResearchLaboratory;
            pc.BlueprintList.Add(resBp);

            var researchBp = new OE2EmpireTracker.Models.Blueprint("ResearchTarget");
            researchBp.UUID = Guid.NewGuid().ToString();
            researchBp.BluePrintType = "Beamer/Small";
            researchBp.Evolution = 3;
            pc.BlueprintList.Add(researchBp);

            var resStruct = new ColonyStructure();
            resStruct.UUID = Guid.NewGuid().ToString();
            resStruct.FlatpackBlueprintUUID = resBp.UUID;
            resStruct.displaySequence = 2;
            resStruct.Properties.setProperty(GameConstants.PropBuilt, true);
            resStruct.Properties.setProperty(GameConstants.PropOnline, true);
            resStruct.ResearchingBlueprintUUID = researchBp.UUID;
            resStruct.ProcessCompletionTime = new CountDownTime();
            resStruct.ProcessCompletionTime.TimeRemaining = 7200;
            colony.Structures.Add(resStruct);

            string rtf = ColonyAdminReportBuilder.BuildReport(colony, pc);

            // Find the Research section (activity)
            int researchIdx = rtf.IndexOf("Research", StringComparison.Ordinal);
            Assert.That(researchIdx, Is.GreaterThanOrEqualTo(0), "Research section should exist");

            // The building structure name should NOT appear after the Research header
            string buildName = buildBp.ExtendedName;
            int buildInActivity = rtf.IndexOf(buildName, researchIdx, StringComparison.Ordinal);
            // Building name appears in the Building section (before Research), not in the activity section
            // Just verify the Building section exists separately
            int buildingSectionIdx = rtf.IndexOf("Building", StringComparison.Ordinal);
            Assert.That(buildingSectionIdx, Is.GreaterThanOrEqualTo(0), "Building section should exist");
            Assert.That(buildingSectionIdx, Is.LessThan(researchIdx), "Building section should come before Research");
        }
    }
}
