using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Parsers;
using System.Collections.Generic;

namespace OE2EmpireTracker.Tests.Parsers
{
    /// <summary>
    /// Tests that ParseBuilding correctly extracts the maxRate field from building JSON.
    /// Validates: Requirements 6.7
    /// </summary>
    [TestFixture]
    public class ColonyParserMaxRateTests
    {
        private Dictionary<string, string> _emptyLookup;

        [SetUp]
        public void SetUp()
        {
            _emptyLookup = new Dictionary<string, string>();
        }

        [Test]
        public void ParseBuilding_ExtractsMaxRate_WhenPresent()
        {
            var building = new JObject
            {
                ["blueprintDesignName"] = "Mining Rig",
                ["buildingID"] = 1,
                ["buildingOnline"] = true,
                ["maxRate"] = 123.45m
            };

            var structure = ColonyParser.ParseBuilding(building, _emptyLookup, out decimal maxRate);

            Assert.That(structure, Is.Not.Null);
            Assert.That(maxRate, Is.EqualTo(123.45m));
        }

        [Test]
        public void ParseBuilding_ReturnsZeroMaxRate_WhenFieldMissing()
        {
            var building = new JObject
            {
                ["blueprintDesignName"] = "Warehouse",
                ["buildingID"] = 2,
                ["buildingOnline"] = true
            };

            var structure = ColonyParser.ParseBuilding(building, _emptyLookup, out decimal maxRate);

            Assert.That(structure, Is.Not.Null);
            Assert.That(maxRate, Is.EqualTo(0m));
        }

        [Test]
        public void ParseBuilding_ReturnsZeroMaxRate_WhenFieldIsZero()
        {
            var building = new JObject
            {
                ["blueprintDesignName"] = "Mining Rig",
                ["buildingID"] = 3,
                ["buildingOnline"] = false,
                ["maxRate"] = 0m
            };

            var structure = ColonyParser.ParseBuilding(building, _emptyLookup, out decimal maxRate);

            Assert.That(structure, Is.Not.Null);
            Assert.That(maxRate, Is.EqualTo(0m));
        }

        [Test]
        public void ParseBuilding_ReturnsNullStructure_WhenDesignNameEmpty()
        {
            var building = new JObject
            {
                ["blueprintDesignName"] = "",
                ["maxRate"] = 50.0m
            };

            var structure = ColonyParser.ParseBuilding(building, _emptyLookup, out decimal maxRate);

            Assert.That(structure, Is.Null);
            // maxRate is still extracted even when structure is null
            Assert.That(maxRate, Is.EqualTo(50.0m));
        }

        [Test]
        public void ParseBuilding_ExtractsDecimalMaxRate_WithHighPrecision()
        {
            var building = new JObject
            {
                ["blueprintDesignName"] = "Mining Rig",
                ["buildingID"] = 4,
                ["buildingOnline"] = true,
                ["maxRate"] = 99.9876m
            };

            var structure = ColonyParser.ParseBuilding(building, _emptyLookup, out decimal maxRate);

            Assert.That(structure, Is.Not.Null);
            Assert.That(maxRate, Is.EqualTo(99.9876m));
        }
    }
}
