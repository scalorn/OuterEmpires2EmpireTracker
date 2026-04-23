using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Parsers
{
    /// <summary>
    /// Unit tests for MinerSetupHelper.FindBestSurvey.
    /// Validates: Requirements 1.1, 1.2, 1.3, 1.6
    /// </summary>
    [TestFixture]
    public class MinerSetupHelperFindBestSurveyTests
    {
        private PlayerContext _playerContext;

        [SetUp]
        public void SetUp()
        {
            EmpireContext.Reset();
            TestHelper.SetAllFilePaths();
            EmpireContext.GetInstance();
            _playerContext = EmpireContext.PlayerContext;

            // Clear existing surveys so tests start clean
            foreach (var item in _playerContext.SurveyList.ToList()) _playerContext.RemoveSurvey(item);
        }

        [TearDown]
        public void TearDown()
        {
            EmpireContext.Reset();
        }

        /// <summary>
        /// Helper to create a survey with a single resource entry.
        /// </summary>
        private Survey CreateSurvey(
            string uuid,
            string planetName,
            string surveyId,
            string resourceName,
            string purity,
            string amount)
        {
            var survey = new Survey("Test Survey")
            {
                UUID = uuid,
                PlanetName = planetName,
                SurveyID = surveyId,
                Resources = new Dictionary<string, SurveyResource>
                {
                    [resourceName] = new SurveyResource(resourceName, purity, amount)
                }
            };

            return survey;
        }

        [Test]
        public void FindBestSurvey_SelectsClosestMatch_WhenMaxRateGreaterThanZero()
        {
            // Arrange: two real surveys with different amounts
            var survey1 = CreateSurvey("s1", "Alpha Prime", "SURV-001", "Iron", "Medium", "100");
            var survey2 = CreateSurvey("s2", "Alpha Prime", "SURV-002", "Iron", "Medium", "150");
            _playerContext.AddSurvey(survey1);
            _playerContext.AddSurvey(survey2);

            // Act: maxRate 140 is closer to 150 than to 100
            var result = MinerSetupHelper.FindBestSurvey("Alpha Prime", "Iron", "Medium", 140m, _playerContext);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.UUID, Is.EqualTo("s2"));
        }

        [Test]
        public void FindBestSurvey_SelectsHighestAmount_WhenMaxRateIsZero()
        {
            // Arrange
            var survey1 = CreateSurvey("s1", "Beta Colony", "SURV-010", "Copper", "High", "200");
            var survey2 = CreateSurvey("s2", "Beta Colony", "SURV-011", "Copper", "High", "350");
            var survey3 = CreateSurvey("s3", "Beta Colony", "SURV-012", "Copper", "High", "275");
            _playerContext.AddSurvey(survey1);
            _playerContext.AddSurvey(survey2);
            _playerContext.AddSurvey(survey3);

            // Act: maxRate 0 means select highest
            var result = MinerSetupHelper.FindBestSurvey("Beta Colony", "Copper", "High", 0m, _playerContext);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.UUID, Is.EqualTo("s2"));
        }

        [Test]
        public void FindBestSurvey_ReturnsNull_WhenNoMatchingSurveyExists()
        {
            // Arrange: survey for a different planet
            var survey = CreateSurvey("s1", "Gamma World", "SURV-020", "Gold", "Low", "50");
            _playerContext.AddSurvey(survey);

            // Act: search for a planet with no surveys
            var result = MinerSetupHelper.FindBestSurvey("Delta Station", "Gold", "Low", 50m, _playerContext);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindBestSurvey_ExcludesDefaultSurveys()
        {
            // Arrange: one DEFAULT survey and one real survey
            var defaultSurvey = CreateSurvey("d1", "Epsilon", "DEFAULT", "Silver", "Medium", "500");
            var realSurvey = CreateSurvey("r1", "Epsilon", "SURV-030", "Silver", "Medium", "100");
            _playerContext.AddSurvey(defaultSurvey);
            _playerContext.AddSurvey(realSurvey);

            // Act: even though DEFAULT has amount 500 closer to maxRate 480, it should be excluded
            var result = MinerSetupHelper.FindBestSurvey("Epsilon", "Silver", "Medium", 480m, _playerContext);

            // Assert: should pick the real survey, not the default
            Assert.That(result, Is.Not.Null);
            Assert.That(result.UUID, Is.EqualTo("r1"));
        }

        [Test]
        public void FindBestSurvey_ReturnsNull_WhenOnlyDefaultSurveysExist()
        {
            // Arrange: only a DEFAULT survey
            var defaultSurvey = CreateSurvey("d1", "Zeta", "DEFAULT", "Titanium", "High", "300");
            _playerContext.AddSurvey(defaultSurvey);

            // Act
            var result = MinerSetupHelper.FindBestSurvey("Zeta", "Titanium", "High", 300m, _playerContext);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindBestSurvey_IsCaseInsensitive_ForPlanetName()
        {
            // Arrange
            var survey = CreateSurvey("s1", "Alpha Prime", "SURV-040", "Iron", "Low", "80");
            _playerContext.AddSurvey(survey);

            // Act: search with different casing
            var result = MinerSetupHelper.FindBestSurvey("alpha prime", "Iron", "Low", 80m, _playerContext);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.UUID, Is.EqualTo("s1"));
        }

        [Test]
        public void FindBestSurvey_IsCaseInsensitive_ForResourceAndPurity()
        {
            // Arrange
            var survey = CreateSurvey("s1", "Theta", "SURV-050", "Iron", "Medium", "120");
            _playerContext.AddSurvey(survey);

            // Act: search with different casing for resource and purity
            var result = MinerSetupHelper.FindBestSurvey("Theta", "iron", "medium", 120m, _playerContext);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.UUID, Is.EqualTo("s1"));
        }

        [Test]
        public void FindBestSurvey_ReturnsNull_WhenResourceDoesNotMatch()
        {
            // Arrange: survey has Iron but we search for Copper
            var survey = CreateSurvey("s1", "Kappa", "SURV-060", "Iron", "High", "200");
            _playerContext.AddSurvey(survey);

            // Act
            var result = MinerSetupHelper.FindBestSurvey("Kappa", "Copper", "High", 200m, _playerContext);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindBestSurvey_ReturnsNull_WhenPurityDoesNotMatch()
        {
            // Arrange: survey has Medium purity but we search for High
            var survey = CreateSurvey("s1", "Lambda", "SURV-070", "Iron", "Medium", "200");
            _playerContext.AddSurvey(survey);

            // Act
            var result = MinerSetupHelper.FindBestSurvey("Lambda", "Iron", "High", 200m, _playerContext);

            // Assert
            Assert.That(result, Is.Null);
        }
    }
}
