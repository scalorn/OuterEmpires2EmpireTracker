using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Parsers
{
    /// <summary>
    /// Bug condition exploration tests for asteroid survey max reserve extraction.
    /// These tests encode the EXPECTED behavior — they are expected to FAIL on unfixed code,
    /// proving the bug exists: SurveyParser.ProcessHtml detects ScanDetailOutputMaxReserve
    /// nodes but never extracts their values, and LinkOrCreateAsteroid creates asteroids
    /// with empty Reserves.
    ///
    /// Validates: Requirements 1.1, 2.1
    /// </summary>
    [TestFixture]
    public class SurveyParserMaxReserveTests
    {
        private SurveyParser _parser;
        private PlayerContext _playerContext;
        private string _originalPlayerFilePath;

        [SetUp]
        public void SetUp()
        {
            _parser = new SurveyParser();
            _originalPlayerFilePath = PlayerContext.FilePath;
            PlayerContext.FilePath = "nonexistent_player_data.json";
            PlayerContext.Reset();
            _playerContext = PlayerContext.GetInstance();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerContext.Reset();
            PlayerContext.FilePath = _originalPlayerFilePath;
        }

        private static string LoadTestData(string filename)
        {
            string baseDir = TestContext.CurrentContext.TestDirectory;
            string path = Path.Combine(baseDir, "TestData", filename);
            return File.ReadAllText(path);
        }

        private static string ExtractFragment(string clipboardData)
        {
            return BlueprintScanner.ExtractHtmlFragmentFromClipboardData(clipboardData);
        }

        private Survey ParseAsteroidSurvey()
        {
            string clipboardData = LoadTestData("AsteroidSurveySample.html");
            string html = ExtractFragment(clipboardData);
            var survey = new Survey();
            _parser.ProcessHtml(survey, html);
            return survey;
        }

        // -----------------------------------------------------------------
        // Bug Condition: Parser does not extract max reserve values
        // isBugCondition: maxReserveNodes.Count > 0 AND resourceNodes.Count > 0
        //                 AND survey.SurveyType == Asteroid
        //                 AND linkedAsteroid.Reserves.Count == 0
        // -----------------------------------------------------------------

        /// <summary>
        /// ProcessHtml with asteroid survey HTML containing ScanDetailOutputMaxReserve
        /// nodes should populate ParsedMaxReserves on the survey.
        /// Bug: Survey.ParsedMaxReserves property does not exist yet.
        /// </summary>
        [Test]
        public void ProcessHtml_AsteroidSurvey_ParsedMaxReservesIsNotNull()
        {
            var survey = ParseAsteroidSurvey();
            Assert.That(survey.ParsedMaxReserves, Is.Not.Null,
                "ParsedMaxReserves should be populated when HTML contains ScanDetailOutputMaxReserve nodes");
        }

        /// <summary>
        /// The parsed max reserves should contain entries for each resource that has
        /// a max reserve value in the HTML (Noble Gases=7123, Heavy Noble Gases=6998,
        /// Alkali Organics=6420, Strong Alkali Organics=6269).
        /// </summary>
        [Test]
        public void ProcessHtml_AsteroidSurvey_ParsedMaxReservesHasCorrectCount()
        {
            var survey = ParseAsteroidSurvey();
            Assert.That(survey.ParsedMaxReserves, Is.Not.Null);
            // The HTML has 5 MaxReserve nodes but only 4 have numeric values;
            // the 5th (Superheavy Exotics) has an empty value
            Assert.That(survey.ParsedMaxReserves.Count, Is.GreaterThanOrEqualTo(4),
                "Should have at least 4 parsed max reserve entries");
        }

        /// <summary>
        /// Each parsed max reserve value should match the known values from the HTML fixture.
        /// </summary>
        [Test]
        public void ProcessHtml_AsteroidSurvey_ParsedMaxReserveValuesCorrect()
        {
            var survey = ParseAsteroidSurvey();
            Assert.That(survey.ParsedMaxReserves, Is.Not.Null);
            Assert.That(survey.ParsedMaxReserves.ContainsKey("Noble Gases"), Is.True);
            Assert.That(survey.ParsedMaxReserves["Noble Gases"], Is.EqualTo(7123));
            Assert.That(survey.ParsedMaxReserves["Heavy Noble Gases"], Is.EqualTo(6998));
            Assert.That(survey.ParsedMaxReserves["Alkali Organics"], Is.EqualTo(6420));
            Assert.That(survey.ParsedMaxReserves["Strong Alkali Organics"], Is.EqualTo(6269));
        }

        // -----------------------------------------------------------------
        // Downstream: LinkOrCreateAsteroid should populate Reserves
        // expectedBehavior: asteroid.Reserves.Count == survey.ParsedMaxReserves.Count
        //                   AND each reserve.MaxReserve matches parsed value
        // -----------------------------------------------------------------

        /// <summary>
        /// Given a survey with ParsedMaxReserves populated, LinkOrCreateAsteroid should
        /// create an asteroid with non-empty Reserves matching the parsed data.
        /// Bug: LinkOrCreateAsteroid creates asteroid with empty Reserves.
        /// </summary>
        [Test]
        public void LinkOrCreateAsteroid_WithParsedMaxReserves_PopulatesReserves()
        {
            // Build a survey that simulates what the fixed parser would produce
            var survey = new Survey("Test Asteroid Survey");
            survey.PlanetName = "AST-TEST-01";
            survey.SystemName = "Test System";
            survey.SurveyType = SurveyType.Asteroid;
            survey.Resources["Noble Gases"] = new SurveyResource("Noble Gases", "High", "23.1");
            survey.Resources["Heavy Noble Gases"] = new SurveyResource("Heavy Noble Gases", "High", "23.1");
            survey.Resources["Alkali Organics"] = new SurveyResource("Alkali Organics", "High", "19.8");

            // Simulate parsed max reserves (what the fixed parser would produce)
            survey.ParsedMaxReserves = new Dictionary<string, int>
            {
                { "Noble Gases", 15000 },
                { "Heavy Noble Gases", 8500 },
                { "Alkali Organics", 22000 }
            };

            SurveyImportHelper.LinkOrCreateAsteroid(survey, _playerContext);

            // Find the created asteroid
            var asteroid = _playerContext.AsteroidList.FirstOrDefault(a => a.UUID == survey.AsteroidUUID);
            Assert.That(asteroid, Is.Not.Null, "Asteroid should be created");
            Assert.That(asteroid.Reserves, Is.Not.Empty,
                "Asteroid.Reserves should be populated from survey.ParsedMaxReserves");
            Assert.That(asteroid.Reserves.Count, Is.EqualTo(3),
                "Asteroid should have one reserve per parsed max reserve entry");
        }

        /// <summary>
        /// Each AsteroidReserve should have the correct MaxReserve value matching
        /// the parsed data from the survey.
        /// </summary>
        [Test]
        public void LinkOrCreateAsteroid_WithParsedMaxReserves_ReserveValuesCorrect()
        {
            var survey = new Survey("Test Asteroid Survey");
            survey.PlanetName = "AST-TEST-02";
            survey.SystemName = "Test System";
            survey.SurveyType = SurveyType.Asteroid;
            survey.Resources["Noble Gases"] = new SurveyResource("Noble Gases", "High", "23.1");
            survey.Resources["Heavy Noble Gases"] = new SurveyResource("Heavy Noble Gases", "High", "23.1");

            survey.ParsedMaxReserves = new Dictionary<string, int>
            {
                { "Noble Gases", 15000 },
                { "Heavy Noble Gases", 8500 }
            };

            SurveyImportHelper.LinkOrCreateAsteroid(survey, _playerContext);

            var asteroid = _playerContext.AsteroidList.FirstOrDefault(a => a.UUID == survey.AsteroidUUID);
            Assert.That(asteroid, Is.Not.Null);

            var nobleGasReserve = asteroid.Reserves.FirstOrDefault(r => r.ResourceName == "Noble Gases");
            Assert.That(nobleGasReserve, Is.Not.Null, "Should have a reserve for Noble Gases");
            Assert.That(nobleGasReserve.MaxReserve, Is.EqualTo(15000));
            Assert.That(nobleGasReserve.Purity, Is.EqualTo("High"));

            var heavyNobleReserve = asteroid.Reserves.FirstOrDefault(r => r.ResourceName == "Heavy Noble Gases");
            Assert.That(heavyNobleReserve, Is.Not.Null, "Should have a reserve for Heavy Noble Gases");
            Assert.That(heavyNobleReserve.MaxReserve, Is.EqualTo(8500));
        }
    }
}