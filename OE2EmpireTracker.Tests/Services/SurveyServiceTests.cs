using System;
using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for SurveyService edge cases and event firing.
    /// Feature: bl-110-survey-readonly
    /// Validates: Requirements 13.7, 13.8, 13.9, 14.2, 14.3, 14.7, 15.4, 15.5, 16.2, 16.3, 16.4
    /// </summary>
    [TestFixture]
    public class SurveyServiceTests
    {
        private PlayerContext playerContext;
        private SurveyService service;

        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
            playerContext = PlayerContext.GetInstance();
            playerContext.CurrentPlayerUUID = "test-player-uuid";
            service = new SurveyService(playerContext);
        }

        // -------------------------------------------------------------------
        // Requirement 13.9: Update throws InvalidOperationException on unknown UUID
        // -------------------------------------------------------------------

        [Test]
        public void Update_NonExistentUUID_ThrowsInvalidOperationException()
        {
            var request = new SurveyUpdateRequest
            {
                PlanetName = "Test",
                SystemName = string.Empty,
                SurveyID = string.Empty,
                NickName = string.Empty,
                ScannedBy = string.Empty,
                DateTime = string.Empty,
                ScannerBlueprintUUID = string.Empty,
                AsteroidUUID = string.Empty,
                SurveyType = SurveyType.Planet,
                Resources = new Dictionary<string, SurveyResource>(),
                Properties = new Dictionary<string, string>(),
            };

            Assert.Throws<InvalidOperationException>(
                () => service.Update("nonexistent-uuid", request));
        }

        // -------------------------------------------------------------------
        // Requirement 15.5: Delete with empty UUID returns without error
        // -------------------------------------------------------------------

        [Test]
        public void Delete_EmptyUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => service.Delete(string.Empty));
        }

        // -------------------------------------------------------------------
        // Requirement 15.5: Delete with non-existent UUID returns without error
        // -------------------------------------------------------------------

        [Test]
        public void Delete_NonExistentUUID_ReturnsWithoutError()
        {
            Assert.DoesNotThrow(() => service.Delete("nonexistent-uuid"));
        }

        // -------------------------------------------------------------------
        // Requirement 14.2: Create assigns non-empty UUID
        // -------------------------------------------------------------------

        [Test]
        public void Create_AssignsNonEmptyUUID()
        {
            var request = new SurveyCreateRequest
            {
                PlanetName = "NewPlanet",
                SystemName = string.Empty,
                SurveyID = "S001",
                NickName = string.Empty,
                ScannedBy = string.Empty,
                DateTime = string.Empty,
                ScannerBlueprintUUID = string.Empty,
                AsteroidUUID = string.Empty,
                SurveyType = SurveyType.Planet,
                Resources = new Dictionary<string, SurveyResource>(),
                Properties = new Dictionary<string, string>(),
            };

            var result = service.Create(request);

            Assert.That(result.UUID, Is.Not.Null.And.Not.Empty);
        }

        // -------------------------------------------------------------------
        // Requirement 14.3: Create sets OwnerUUID to current player UUID
        // -------------------------------------------------------------------

        [Test]
        public void Create_SetsOwnerUUID_ToCurrentPlayerUUID()
        {
            var request = new SurveyCreateRequest
            {
                PlanetName = "OwnerTest",
                SystemName = string.Empty,
                SurveyID = "S002",
                NickName = string.Empty,
                ScannedBy = string.Empty,
                DateTime = string.Empty,
                ScannerBlueprintUUID = string.Empty,
                AsteroidUUID = string.Empty,
                SurveyType = SurveyType.Planet,
                Resources = new Dictionary<string, SurveyResource>(),
                Properties = new Dictionary<string, string>(),
            };

            var result = service.Create(request);

            Assert.That(result.OwnerUUID, Is.EqualTo("test-player-uuid"));
        }

        // -------------------------------------------------------------------
        // Requirement 13.7: Update fires SurveyDataChanged event
        // -------------------------------------------------------------------

        [Test]
        public void Update_FiresSurveyDataChangedEvent()
        {
            var survey = new Survey
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                PlanetName = "EventTest",
            };
            playerContext.AddSurvey(survey);

            bool eventFired = false;
            playerContext.SurveyDataChanged += (s, e) => eventFired = true;

            var request = new SurveyUpdateRequest
            {
                PlanetName = "Updated",
                SystemName = string.Empty,
                SurveyID = string.Empty,
                NickName = string.Empty,
                ScannedBy = string.Empty,
                DateTime = string.Empty,
                ScannerBlueprintUUID = string.Empty,
                AsteroidUUID = string.Empty,
                SurveyType = SurveyType.Planet,
                Resources = new Dictionary<string, SurveyResource>(),
                Properties = new Dictionary<string, string>(),
            };
            service.Update(survey.UUID, request);

            Assert.That(eventFired, Is.True);
        }

        // -------------------------------------------------------------------
        // Requirement 14.7: Create fires SurveyDataChanged event
        // -------------------------------------------------------------------

        [Test]
        public void Create_FiresSurveyDataChangedEvent()
        {
            bool eventFired = false;
            playerContext.SurveyDataChanged += (s, e) => eventFired = true;

            var request = new SurveyCreateRequest
            {
                PlanetName = "NewPlan",
                SystemName = string.Empty,
                SurveyID = "S003",
                NickName = string.Empty,
                ScannedBy = string.Empty,
                DateTime = string.Empty,
                ScannerBlueprintUUID = string.Empty,
                AsteroidUUID = string.Empty,
                SurveyType = SurveyType.Planet,
                Resources = new Dictionary<string, SurveyResource>(),
                Properties = new Dictionary<string, string>(),
            };
            service.Create(request);

            Assert.That(eventFired, Is.True);
        }

        // -------------------------------------------------------------------
        // Requirement 15.4: Delete fires SurveyDataChanged event
        // -------------------------------------------------------------------

        [Test]
        public void Delete_FiresSurveyDataChangedEvent()
        {
            var survey = new Survey
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                PlanetName = "DeleteEventTest",
            };
            playerContext.AddSurvey(survey);

            bool eventFired = false;
            playerContext.SurveyDataChanged += (s, e) => eventFired = true;

            service.Delete(survey.UUID);

            Assert.That(eventFired, Is.True);
        }

        // -------------------------------------------------------------------
        // Requirement 16.2: Import with matching survey merges data
        // -------------------------------------------------------------------

        [Test]
        public void Import_MatchingSurvey_PreservesUUIDAndNickName()
        {
            var existing = new Survey
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                PlanetName = "MergePlanet",
                SurveyID = "MERGE-001",
                NickName = "MyNick",
                ScannedBy = "OldScanner",
                SurveyType = SurveyType.Planet,
            };
            playerContext.AddSurvey(existing);

            var tempSurvey = new Survey
            {
                PlanetName = "MergePlanet",
                SurveyID = "MERGE-001",
                ScannedBy = "NewScanner",
                DateTime = "2025-06-01T12:00:00Z",
                SurveyType = SurveyType.Planet,
                AsteroidUUID = string.Empty,
            };

            var result = service.Import(tempSurvey);

            Assert.That(result.UUID, Is.EqualTo(existing.UUID));
            Assert.That(result.NickName, Is.EqualTo("MyNick"));
            Assert.That(result.ScannedBy, Is.EqualTo("NewScanner"));
        }

        // -------------------------------------------------------------------
        // Requirement 16.3: Import with no match creates new survey
        // -------------------------------------------------------------------

        [Test]
        public void Import_NoMatch_CreatesNewSurvey()
        {
            var tempSurvey = new Survey
            {
                PlanetName = "BrandNewPlanet",
                SurveyID = "NEW-001",
                SystemName = "TestSystem",
                ScannedBy = "Scanner",
                DateTime = "2025-06-01T12:00:00Z",
                SurveyType = SurveyType.Planet,
                AsteroidUUID = string.Empty,
            };

            var result = service.Import(tempSurvey);

            Assert.That(result.UUID, Is.Not.Null.And.Not.Empty);
            Assert.That(result.PlanetName, Is.EqualTo("BrandNewPlanet"));
            Assert.That(result.SurveyID, Is.EqualTo("NEW-001"));
        }

        // -------------------------------------------------------------------
        // Requirement 16.4: Import with asteroid survey calls LinkOrCreateAsteroid
        // -------------------------------------------------------------------

        [Test]
        public void Import_AsteroidSurvey_LinksOrCreatesAsteroid()
        {
            var tempSurvey = new Survey
            {
                PlanetName = "AsteroidAlpha",
                SurveyID = "AST-001",
                SystemName = "AsteroidSystem",
                ScannedBy = "Scanner",
                DateTime = "2025-06-01T12:00:00Z",
                SurveyType = SurveyType.Asteroid,
                AsteroidUUID = string.Empty,
            };

            var result = service.Import(tempSurvey);

            Assert.That(result.UUID, Is.Not.Null.And.Not.Empty);
            Assert.That(result.AsteroidUUID, Is.Not.Null.And.Not.Empty);
            Assert.That(result.SurveyType, Is.EqualTo(SurveyType.Asteroid));
        }
    }
}