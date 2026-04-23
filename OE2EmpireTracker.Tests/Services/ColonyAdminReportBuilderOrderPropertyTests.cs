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
    /// Feature: colony-admin-summary, Property 1: Report section ordering
    /// </summary>
    [TestFixture]
    public class ColonyAdminReportBuilderOrderPropertyTests
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
        /// Feature: colony-admin-summary, Property 1: Report section ordering.
        /// Building appears before Commodity Requests, before Inactivity, before Activity.
        /// **Validates: Requirements 2.1, 2a.1, 3.1, 4.1**
        /// </summary>
        [Test]
        public void SectionOrdering_BuildingBeforeCommodityRequestsBeforeInactivityBeforeActivity()
        {
            var pc = PlayerContext.GetInstance();
            foreach (var item in pc.BlueprintList.ToList()) pc.RemoveBlueprint(item);

            var colony = MakeColony();

            // Building structure
            var buildBp = CreateBlueprint(BlueprintTypes.MiningRig, "BuildingMiner");
            var buildStruct = MakeStructure(buildBp.UUID, 1);
            buildStruct.BuildCompletionTime = new CountDownTime();
            buildStruct.BuildCompletionTime.TimeRemaining = 3600;
            colony.Structures.Add(buildStruct);

            // Commodity request
            colony.Commodities = new List<CommodityRequested>
            {
                new CommodityRequested { Name = "Joybots", Requested = 100, Fulfilled = false, NeedBy = DateTime.MinValue }
            };

            // Idle structure (inactivity)
            var idleBp = CreateBlueprint(BlueprintTypes.Refinery, "IdleRefiner");
            colony.Structures.Add(MakeStructure(idleBp.UUID, 2));

            // Active manufacturing (activity)
            var mfgBp = CreateBlueprint(BlueprintTypes.Manufactory, "ActiveMfg");
            var mfgStruct = MakeStructure(mfgBp.UUID, 3);
            mfgStruct.ManufacturingBlueprintUUID = Guid.NewGuid().ToString();
            mfgStruct.ManufacturingQuantity = 1;
            mfgStruct.ProcessCompletionTime = new CountDownTime();
            mfgStruct.ProcessCompletionTime.TimeRemaining = 1800;
            colony.Structures.Add(mfgStruct);

            // Need a blueprint for the manufacturing details
            var targetBp = new OE2EmpireTracker.Models.Blueprint("TestWeapon");
            targetBp.UUID = mfgStruct.ManufacturingBlueprintUUID;
            targetBp.BluePrintType = "Beamer/Small";
            pc.AddBlueprint(targetBp);

            string rtf = ColonyAdminReportBuilder.BuildReport(colony, pc);

            int buildingIdx = rtf.IndexOf("Building", StringComparison.Ordinal);
            int commodityIdx = rtf.IndexOf("Commodity Requests", StringComparison.Ordinal);
            int idleIdx = rtf.IndexOf("Idle Refining", StringComparison.Ordinal);
            int mfgIdx = rtf.IndexOf("Manufacturing", StringComparison.Ordinal);

            Assert.That(buildingIdx, Is.GreaterThanOrEqualTo(0), "Building section missing");
            Assert.That(commodityIdx, Is.GreaterThan(buildingIdx), "Commodity Requests should come after Building");
            Assert.That(idleIdx, Is.GreaterThan(commodityIdx), "Inactivity should come after Commodity Requests");
            // Manufacturing section appears after the idle section
            // Find the Manufacturing header that comes after the idle section
            int mfgHeaderIdx = rtf.IndexOf("Manufacturing", idleIdx, StringComparison.Ordinal);
            Assert.That(mfgHeaderIdx, Is.GreaterThan(idleIdx), "Activity should come after Inactivity");
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
                LastImportDateTime = SurveyDateTimeParser.ToIsoString(DateTime.UtcNow)
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
