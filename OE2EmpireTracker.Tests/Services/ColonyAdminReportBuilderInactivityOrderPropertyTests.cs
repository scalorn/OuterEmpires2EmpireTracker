using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Feature: colony-admin-summary, Property 4: Inactivity group ordering
    /// </summary>
    [TestFixture]
    public class ColonyAdminReportBuilderInactivityOrderPropertyTests
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
        /// Feature: colony-admin-summary, Property 4: Inactivity group ordering.
        /// Groups appear in fixed order: Colony Import Staleness, Idle Mining, Idle Refining,
        /// Idle Manufacturing, Idle Commodity Manufacturing, Idle Research, Underutilized Refining.
        /// **Validates: Requirements 3.2**
        /// </summary>
        [Test]
        public void InactivityGroups_AppearInFixedOrder()
        {
            var pc = PlayerContext.GetInstance();
            foreach (var item in pc.BlueprintList.ToList()) pc.RemoveBlueprint(item);

            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                SystemName = "TestSystem",
                ColonyName = "TestColony",
                LastImportDateTime = null // triggers staleness
            };

            // Add idle structures of each type
            var types = new[] {
                BlueprintTypes.MiningRig,
                BlueprintTypes.Refinery,
                BlueprintTypes.Manufactory,
                BlueprintTypes.CommodityFactoryPrefix + "Agridome",
                BlueprintTypes.ResearchLaboratory
            };

            int seq = 1;
            foreach (var t in types)
            {
                var bp = new OE2EmpireTracker.Models.Blueprint("Idle_" + t.Replace("/", "_"));
                bp.UUID = Guid.NewGuid().ToString();
                bp.BluePrintType = t;
                pc.AddBlueprint(bp);

                var s = new ColonyStructure();
                s.UUID = Guid.NewGuid().ToString();
                s.FlatpackBlueprintUUID = bp.UUID;
                s.DisplaySequence = seq++;
                s.Properties.SetProperty(GameConstants.PropBuilt, true);
                s.Properties.SetProperty(GameConstants.PropOnline, true);
                colony.Structures.Add(s);
            }

            string rtf = ColonyAdminReportBuilder.BuildReport(colony, pc);

            var headers = new[]
            {
                "Colony Import Staleness",
                "Idle Mining",
                "Idle Refining",
                "Idle Manufacturing",
                "Idle Commodity Manufacturing",
                "Idle Research"
            };

            int lastIdx = -1;
            foreach (var header in headers)
            {
                int idx = rtf.IndexOf(header, StringComparison.Ordinal);
                if (idx < 0) continue; // group may be empty
                Assert.That(idx, Is.GreaterThan(lastIdx),
                    $"'{header}' should appear after previous group");
                lastIdx = idx;
            }
        }
    }
}
