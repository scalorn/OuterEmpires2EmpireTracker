using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;
using System.Linq;

namespace OE2EmpireTracker.Tests.Parsers
{
    /// <summary>
    /// Tests that ParseColonyBuildingsFromJson correctly populates RefiningResource
    /// on refinery structures from the game JSON resourceName field.
    /// </summary>
    [TestFixture]
    public class ColonyParserRefineryResourceTests
    {
        private EmpireContext _empireContext;

        [SetUp]
        public void SetUp()
        {
            EmpireContext.Reset();
            TestHelper.SetAllFilePaths();
            _empireContext = EmpireContext.GetInstance();
        }

        [TearDown]
        public void TearDown()
        {
            EmpireContext.Reset();
        }

        private string FindRefineryFlatpackUUID()
        {
            return _empireContext.GlobalBlueprintList
                .First(bp => bp.BluePrintType == BlueprintTypes.Refinery)
                .UUID;
        }

        private string FindRefineryDesignName()
        {
            var bp = _empireContext.GlobalBlueprintList
                .First(b => b.BluePrintType == BlueprintTypes.Refinery);
            return bp.OutputItemName;
        }

        private string FindMiningRigDesignName()
        {
            var bp = _empireContext.GlobalBlueprintList
                .First(b => b.BluePrintType == BlueprintTypes.MiningRig);
            return bp.OutputItemName;
        }

        private string BuildJson(params JObject[] buildings)
        {
            var root = new JObject
            {
                ["buildings"] = new JArray(buildings)
            };
            return root.ToString();
        }

        [Test]
        public void ParseColonyBuildingsFromJson_SetsRefiningResource_ForRefineryWithResource()
        {
            var colony = new Colony { PlanetName = "TestPlanet" };
            string refineryName = FindRefineryDesignName();

            var building = new JObject
            {
                ["blueprintDesignName"] = refineryName,
                ["buildingID"] = 1,
                ["buildingOnline"] = true,
                ["resourceName"] = "Alkaline Earth Metals (Unrefined, High Purity)",
                ["maxRate"] = 0m
            };

            string json = BuildJson(building);
            ColonyParser.ParseColonyBuildingsFromJson(colony, json, _empireContext);

            var refinery = colony.Structures.FirstOrDefault();
            Assert.That(refinery, Is.Not.Null);
            Assert.That(refinery.RefiningResource, Is.EqualTo("Alkaline Earth Metals"));
            Assert.That(refinery.RefiningResourcePurity, Is.EqualTo("High"));
            Assert.That(refinery.MiningSurveyResource, Is.EqualTo("Alkaline Earth Metals"));
        }

        [Test]
        public void ParseColonyBuildingsFromJson_DoesNotSetRefiningResource_ForMiningRig()
        {
            var colony = new Colony { PlanetName = "TestPlanet" };
            string minerName = FindMiningRigDesignName();

            var building = new JObject
            {
                ["blueprintDesignName"] = minerName,
                ["buildingID"] = 2,
                ["buildingOnline"] = true,
                ["resourceName"] = "Iron Ore (Unrefined, Medium Purity)",
                ["maxRate"] = 50m
            };

            string json = BuildJson(building);
            ColonyParser.ParseColonyBuildingsFromJson(colony, json, _empireContext);

            var miner = colony.Structures.FirstOrDefault();
            Assert.That(miner, Is.Not.Null);
            Assert.That(miner.RefiningResource, Is.Null);
            Assert.That(miner.MiningSurveyResource, Is.EqualTo("Iron Ore"));
        }

        [Test]
        public void ParseColonyBuildingsFromJson_RefiningResource_SurvivesReimportMerge()
        {
            var colony = new Colony { PlanetName = "TestPlanet" };
            string refineryName = FindRefineryDesignName();

            var building = new JObject
            {
                ["blueprintDesignName"] = refineryName,
                ["buildingID"] = 1,
                ["buildingOnline"] = true,
                ["resourceName"] = "Copper (Unrefined, Low Purity)",
                ["maxRate"] = 0m
            };

            // First import
            string json = BuildJson(building);
            ColonyParser.ParseColonyBuildingsFromJson(colony, json, _empireContext);
            Assert.That(colony.Structures.Count, Is.EqualTo(1));
            Assert.That(colony.Structures[0].RefiningResource, Is.EqualTo("Copper"));

            // Reimport — should merge and preserve RefiningResource
            ColonyParser.ParseColonyBuildingsFromJson(colony, json, _empireContext);
            Assert.That(colony.Structures.Count, Is.EqualTo(1));
            Assert.That(colony.Structures[0].RefiningResource, Is.EqualTo("Copper"));
        }
    }
}
