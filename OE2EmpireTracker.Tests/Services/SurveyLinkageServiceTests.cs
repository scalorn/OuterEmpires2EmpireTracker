// <copyright file="SurveyLinkageServiceTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for SurveyLinkageService.
    /// Validates: Requirements 4.1, 4.2, 4.3, 4.4, 4.5, 11.2.
    /// </summary>
    [TestFixture]
    public class SurveyLinkageServiceTests
    {
        private PlayerContext playerContext;
        private SurveyLinkageService service;

        [SetUp]
        public void Setup()
        {
            TestHelper.ResetWithCachedData();
            this.playerContext = PlayerContext.GetInstance();
            this.playerContext.CurrentPlayerUUID = "test-player-uuid";
            this.service = new SurveyLinkageService(this.playerContext);
        }

        // -------------------------------------------------------------------
        // Requirement 4.1: Matches existing survey by planet name, links by UUID
        // -------------------------------------------------------------------

        [Test]
        public void ProcessItem_MatchesExistingSurvey_LinksByUUID()
        {
            var existingSurvey = new Survey
            {
                UUID = Guid.NewGuid().ToString(),
                PlanetName = "Alpha Prime",
                SurveyID = "111111",
                OwnerUUID = "test-player-uuid",
            };
            this.playerContext.AddSurvey(existingSurvey);

            var apiItem = new GameApiAssetCargoItem
            {
                TypeC = "Sc",
                ResourceName = "Survey Report: Alpha Prime (ABC123)",
            };
            var localItem = new Item { UUID = Guid.NewGuid().ToString() };

            bool result = this.service.ProcessItem(apiItem, localItem, "test-player-uuid");

            Assert.That(result, Is.True);
            Assert.That(localItem.BaseItemTypeID, Is.EqualTo(existingSurvey.UUID));
        }

        // -------------------------------------------------------------------
        // Requirement 4.2: No existing survey creates a stub
        // -------------------------------------------------------------------

        [Test]
        public void ProcessItem_NoExistingSurvey_CreatesStub()
        {
            var apiItem = new GameApiAssetCargoItem
            {
                TypeC = "Sc",
                ResourceName = "Survey Report: Beta Colony (DEF456)",
            };
            var localItem = new Item { UUID = Guid.NewGuid().ToString() };

            bool result = this.service.ProcessItem(apiItem, localItem, "test-player-uuid");

            Assert.That(result, Is.True);

            var surveys = this.playerContext.GetCurrentPlayerSurveys();
            var stub = surveys.FirstOrDefault(s => s.PlanetName == "Beta Colony");
            Assert.That(stub, Is.Not.Null);
            Assert.That(stub.SurveyID, Is.EqualTo("DEF456"));
            Assert.That(stub.OwnerUUID, Is.EqualTo("test-player-uuid"));
        }

        // -------------------------------------------------------------------
        // Requirement 4.3: Malformed name skips and returns false
        // -------------------------------------------------------------------

        [Test]
        public void ProcessItem_MalformedName_SkipsAndLogsWarning()
        {
            var apiItem = new GameApiAssetCargoItem
            {
                TypeC = "Sc",
                ResourceName = "Some Random Item",
            };
            var localItem = new Item { UUID = Guid.NewGuid().ToString() };

            bool result = this.service.ProcessItem(apiItem, localItem, "test-player-uuid");

            Assert.That(result, Is.False);
            Assert.That(localItem.BaseItemTypeID, Is.EqualTo(string.Empty));
        }

        // -------------------------------------------------------------------
        // Requirement 4.4: Case-insensitive match finds existing survey
        // -------------------------------------------------------------------

        [Test]
        public void ProcessItem_CaseInsensitiveMatch_FindsExisting()
        {
            var existingSurvey = new Survey
            {
                UUID = Guid.NewGuid().ToString(),
                PlanetName = "alpha prime",
                SurveyID = "999999",
                OwnerUUID = "test-player-uuid",
            };
            this.playerContext.AddSurvey(existingSurvey);

            var apiItem = new GameApiAssetCargoItem
            {
                TypeC = "Sc",
                ResourceName = "Survey Report: Alpha Prime (ABC123)",
            };
            var localItem = new Item { UUID = Guid.NewGuid().ToString() };

            bool result = this.service.ProcessItem(apiItem, localItem, "test-player-uuid");

            Assert.That(result, Is.True);
            Assert.That(localItem.BaseItemTypeID, Is.EqualTo(existingSurvey.UUID));
        }

        // -------------------------------------------------------------------
        // Requirement 4.5: BaseItemTypeID set to survey UUID after stub creation
        // -------------------------------------------------------------------

        [Test]
        public void ProcessItem_SetsBaseItemTypeID_ToSurveyUUID()
        {
            var apiItem = new GameApiAssetCargoItem
            {
                TypeC = "Sc",
                ResourceName = "Survey Report: Gamma World (AABB11)",
            };
            var localItem = new Item { UUID = Guid.NewGuid().ToString() };

            this.service.ProcessItem(apiItem, localItem, "test-player-uuid");

            var surveys = this.playerContext.GetCurrentPlayerSurveys();
            var stub = surveys.FirstOrDefault(s => s.PlanetName == "Gamma World");
            Assert.That(stub, Is.Not.Null);
            Assert.That(localItem.BaseItemTypeID, Is.EqualTo(stub.UUID));
        }

        // -------------------------------------------------------------------
        // Requirement 11.2: Stub survey SurveyID set to hex code from name
        // -------------------------------------------------------------------

        [Test]
        public void ProcessItem_StubSurvey_SetsSurveyIDToHexCode()
        {
            var apiItem = new GameApiAssetCargoItem
            {
                TypeC = "Sc",
                ResourceName = "Survey Report: Planet X (FF00AA)",
            };
            var localItem = new Item { UUID = Guid.NewGuid().ToString() };

            this.service.ProcessItem(apiItem, localItem, "test-player-uuid");

            var surveys = this.playerContext.GetCurrentPlayerSurveys();
            var stub = surveys.FirstOrDefault(s => s.PlanetName == "Planet X");
            Assert.That(stub, Is.Not.Null);
            Assert.That(stub.SurveyID, Is.EqualTo("FF00AA"));
        }

        // -------------------------------------------------------------------
        // Property 2: Survey Linkage Uniqueness
        // Validates: Requirements 11.2
        // -------------------------------------------------------------------

        /// <summary>
        /// Processing the same survey item N times produces exactly one
        /// survey per planet name. No duplicate stubs are created.
        /// </summary>
        [Test]
        public void SurveyIdempotency_ProcessSameItemNTimes_ExactlyOneSurveyPerPlanet()
        {
            Prop.ForAll<PositiveInt>(repeatCount =>
            {
            TestHelper.ResetWithCachedData();
            var context = PlayerContext.GetInstance();
            context.CurrentPlayerUUID = "test-player-uuid";
            var svc = new SurveyLinkageService(context);

            var apiItem = new GameApiAssetCargoItem
            {
                TypeC = "Sc",
                ResourceName = "Survey Report: Test Planet (AABB11)",
            };

            int n = repeatCount.Get;
            for (int i = 0; i < n; i++)
            {
                var localItem = new Item { UUID = Guid.NewGuid().ToString() };
                svc.ProcessItem(apiItem, localItem, "test-player-uuid");
            }

            var surveys = context.GetCurrentPlayerSurveys();
            var matching = surveys.Where(s =>
                string.Equals(s.PlanetName, "Test Planet", StringComparison.OrdinalIgnoreCase)).ToList();
            Assert.That(matching.Count, Is.EqualTo(1));
            }).QuickCheckThrowOnFailure();
        }
    }
}
