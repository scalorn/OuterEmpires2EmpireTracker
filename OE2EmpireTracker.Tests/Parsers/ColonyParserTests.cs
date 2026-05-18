using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Tests;

namespace OE2EmpireTracker.Tests.Parsers
{
    [TestFixture]
    public class ColonyParserTests
    {
        private ColonyParser _parser;

        private EmpireContext _empireContext;

        private string _originalEmpireFilePath;

        private string _originalPlayerFilePath;

        [SetUp]
        public void SetUp()
        {
            _parser = new ColonyParser();
            _originalEmpireFilePath = EmpireContext.FilePath;
            _originalPlayerFilePath = PlayerContext.FilePath;

            EmpireContext.Reset();
            TestHelper.SetEmpireFilePath();
            PlayerContext.FilePath = "nonexistent_player_data.json";
            _empireContext = EmpireContext.GetInstance();
        }

        [TearDown]
        public void TearDown()
        {
            EmpireContext.Reset();
            EmpireContext.FilePath = _originalEmpireFilePath;
            PlayerContext.FilePath = _originalPlayerFilePath;
        }

        [Test]
        public void M1_ParsesPlanetName()
        {
            var colony = ParseM1();
            Assert.That(colony.PlanetName, Is.EqualTo("Zeh Vazoran II M1"));
        }

        [Test]
        public void M1_ParsesSystemName()
        {
            var colony = ParseM1();
            Assert.That(colony.SystemName, Is.EqualTo("Zeh Vazoran"));
        }

        [Test]
        public void M1_ParsesColonyName()
        {
            var colony = ParseM1();
            Assert.That(colony.ColonyName, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void M1_ParsesStructures()
        {
            var colony = ParseM1();
            Assert.That(colony.Structures.Count, Is.EqualTo(43));
        }

        [Test]
        public void M1_StructuresHaveFlatpackUUIDs()
        {
            var colony = ParseM1();
            int matched = colony.Structures.Count(s => !string.IsNullOrEmpty(s.FlatpackBlueprintUUID));
            Assert.That(
                matched,
                Is.EqualTo(colony.Structures.Count),
                "All structures should have a matching flatpack blueprint UUID");
        }

        [Test]
        public void M1_StructuresAreBuiltAndOnline()
        {
            var colony = ParseM1();
            foreach (var s in colony.Structures)
            {
                s.Properties.GetBoolean(GameConstants.PropBuilt, false, out bool built);
                Assert.That(built, Is.True, "Structure should be marked as built");
            }
        }

        [Test]
        public void M1_ContainsExpectedBuildingTypes()
        {
            var colony = ParseM1();
            var names = colony.Structures
                .Select(s => _empireContext.FindGlobalBlueprint(s.FlatpackBlueprintUUID)?.Name)
                .Where(n => n != null)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            Assert.That(names, Does.Contain("Colony Command Centre Flatpack"));
            Assert.That(names, Does.Contain("Reactor Core Flatpack"));
            Assert.That(names, Does.Contain("Mining Rig Flatpack"));
            Assert.That(names, Does.Contain("Refinery Flatpack"));
            Assert.That(names, Does.Contain("Manufactory Flatpack"));
            Assert.That(names, Does.Contain("Warehouse Flatpack"));
            Assert.That(names, Does.Contain("Habitation Block Flatpack"));
            Assert.That(names, Does.Contain("Hydroponics Bay Flatpack"));
            Assert.That(names, Does.Contain("Entertainment Centre Flatpack"));
            Assert.That(names, Does.Contain("Research Laboratory Flatpack"));
            Assert.That(names, Does.Contain("Remote Operations Array Flatpack"));
        }

        [Test]
        public void M1_MiningRigHasResourceInfo()
        {
            var colony = ParseM1();
            var miningRig = colony.Structures.FirstOrDefault(s =>
            {
                var bp = _empireContext.FindGlobalBlueprint(s.FlatpackBlueprintUUID);
                return bp?.Name == "Mining Rig Flatpack";
            });

            Assert.That(miningRig, Is.Not.Null, "Should have a Mining Rig");
            Assert.That(
                miningRig.MiningSurveyResource,
                Is.Not.Null.And.Not.Empty,
                "Mining Rig should have a resource assigned");
        }

        [Test]
        public void M1_StructuresHaveDisplaySequence()
        {
            var colony = ParseM1();
            foreach (var s in colony.Structures)
            {
                Assert.That(
                    s.DisplaySequence,
                    Is.GreaterThan(0),
                    "Each structure should have a non-zero game sequence (BuildingID)");
            }
        }

        [Test]
        public void M1_StructuresHaveBuildingAttributes()
        {
            var colony = ParseM1();
            // Every structure should have at least some properties from building attributes
            foreach (var s in colony.Structures)
            {
                // Built and Online are always set, plus building attributes
                Assert.That(
                    s.Properties.Count,
                    Is.GreaterThan(2),
                    "Structure should have building attributes beyond Built/Online");
            }
        }

        [Test]
        public void M2_2_ParsesCommodityDemands()
        {
            var colony = ParseM2_2();
            Assert.That(
                colony.Commodities.Count,
                Is.GreaterThan(0),
                "Should have at least one commodity demand");
        }

        [Test]
        public void M2_2_CommodityDemandHasDockmasterDrones()
        {
            var colony = ParseM2_2();
            var drones = colony.Commodities.FirstOrDefault(c => c.Name == "Dockmaster Drones");
            Assert.That(drones, Is.Not.Null, "Should have Dockmaster Drones demand");
            Assert.That(drones.Requested, Is.EqualTo(38));
            Assert.That(drones.Fulfilled, Is.False);
            Assert.That(drones.NeedBy, Is.GreaterThan(DateTime.MinValue));
        }

        [Test]
        public void M2_SystemNameFromLocationBar()
        {
            var colony = ParseM2();
            // Non-local colony should still get system name from the location bar
            Assert.That(
                colony.SystemName,
                Is.Not.Null.And.Not.Empty,
                "System name should be extracted from location bar for non-local colonies");
        }

        [Test]
        public void VI1_ParsesStructuresFromWorkersFallback()
        {
            var colony = ParseVI1();
            Assert.That(
                colony.Structures.Count,
                Is.GreaterThan(0),
                "Should parse structures from colony-workers fallback");
        }

        [Test]
        public void VI1_ContainsCommodityFactoryBuildings()
        {
            var colony = ParseVI1();
            var names = colony.Structures
                .Where(s => !string.IsNullOrEmpty(s.FlatpackBlueprintUUID))
                .Select(s => _empireContext.FindGlobalBlueprint(s.FlatpackBlueprintUUID)?.Name)
                .Where(n => n != null)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            Assert.That(names, Does.Contain("Administration Block Flatpack"));
            Assert.That(names, Does.Contain("Agridome Flatpack"));
            Assert.That(names, Does.Contain("Logistics Centre Flatpack"));
            Assert.That(names, Does.Contain("Engineering Block Flatpack"));
        }

        [Test]
        public void VI1_AllStructuresHaveFlatpackUUIDs()
        {
            var colony = ParseVI1();
            int matched = colony.Structures.Count(s => !string.IsNullOrEmpty(s.FlatpackBlueprintUUID));
            Assert.That(
                matched,
                Is.EqualTo(colony.Structures.Count),
                "All structures from workers fallback should have flatpack UUIDs");
        }

        [Test]
        public void VI1_ParsesCommodityDemands()
        {
            var colony = ParseVI1();
            var demand = colony.Commodities.FirstOrDefault(c => c.Name == "Advanced Materials Simulators");
            Assert.That(demand, Is.Not.Null, "Should have Advanced Materials Simulators demand");
            Assert.That(demand.Requested, Is.EqualTo(46));
        }

        // -------------------------------------------------------------------
        // BuildFlatpackLookup
        // -------------------------------------------------------------------

        [Test]
        public void BuildFlatpackLookup_ContainsStandardFlatpacks()
        {
            var lookup = ColonyParser.BuildFlatpackLookup(_empireContext);
            Assert.That(lookup.ContainsKey("Mining Rig"), Is.True);
            Assert.That(lookup.ContainsKey("Reactor Core"), Is.True);
            Assert.That(lookup.ContainsKey("Warehouse"), Is.True);
        }

        [Test]
        public void BuildFlatpackLookup_ContainsCommodityFactories()
        {
            var lookup = ColonyParser.BuildFlatpackLookup(_empireContext);
            Assert.That(lookup.ContainsKey("Administration Block"), Is.True);
            Assert.That(lookup.ContainsKey("Agridome"), Is.True);
            Assert.That(lookup.ContainsKey("Technology Institute"), Is.True);
        }

        // -------------------------------------------------------------------
        // ParseMiningResource
        // -------------------------------------------------------------------

        [Test]
        public void ParseMiningResource_ExtractsNameAndPurity()
        {
            var structure = new ColonyStructure();
            ColonyParser.ParseMiningResource(structure, "Post-Trans Metals (Unrefined, High Purity)");
            Assert.That(structure.MiningSurveyResource, Is.EqualTo("Post-Trans Metals"));
            Assert.That(structure.RefiningResourcePurity, Is.EqualTo("High"));
        }

        [Test]
        public void ParseMiningResource_PlainName()
        {
            var structure = new ColonyStructure();
            ColonyParser.ParseMiningResource(structure, "Iron Ore");
            Assert.That(structure.MiningSurveyResource, Is.EqualTo("Iron Ore"));
            Assert.That(structure.RefiningResourcePurity, Is.Null);
        }

        private static string LoadTestData(string filename)
        {
            string baseDir = TestContext.CurrentContext.TestDirectory;
            return File.ReadAllText(Path.Combine(baseDir, "TestData", filename));
        }

        private static string ExtractFragment(string clipboardData)
        {
            return OE2EmpireTracker.Parsers.ClipboardHelper
                .ExtractHtmlFragment(clipboardData);
        }

        // -------------------------------------------------------------------
        // M1 (local colony) -- full planet overview + colony-buildings JSON
        // -------------------------------------------------------------------

        private Colony ParseM1()
        {
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranIIM1.html");
            string html = ExtractFragment(clipboardData);
            var colony = new Colony();
            _parser.ProcessHtml(colony, html, _empireContext);
            return colony;
        }

        // -------------------------------------------------------------------
        // M2-2 (non-local colony with commodity demands)
        // -------------------------------------------------------------------

        private Colony ParseM2_2()
        {
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranIIM2-2.html");
            string html = ExtractFragment(clipboardData);
            var colony = new Colony();
            _parser.ProcessHtml(colony, html, _empireContext);
            return colony;
        }

        // -------------------------------------------------------------------
        // M2 (non-local colony, no planet overview)
        // -------------------------------------------------------------------

        private Colony ParseM2()
        {
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranIIM2.html");
            string html = ExtractFragment(clipboardData);
            var colony = new Colony();
            _parser.ProcessHtml(colony, html, _empireContext);
            return colony;
        }

        // -------------------------------------------------------------------
        // VI-1 (non-local colony with commodity factory buildings)
        // -------------------------------------------------------------------

        private Colony ParseVI1()
        {
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranVI-1.html");
            string html = ExtractFragment(clipboardData);
            var colony = new Colony();
            _parser.ProcessHtml(colony, html, _empireContext);
            return colony;
        }
    }
}
