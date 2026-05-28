// <copyright file="BlueprintLinkageServiceTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using Blueprint = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for BlueprintLinkageService.
    /// Validates: Requirements 1.1, 1.2, 1.3, 1.7, 5.1.
    /// </summary>
    [TestFixture]
    public class BlueprintLinkageServiceTests
    {
        private PlayerContext playerContext;
        private EmpireContext empireContext;
        private BlueprintLinkageService service;

        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
            playerContext = PlayerContext.GetInstance();
            empireContext = EmpireContext.GetInstance();
            playerContext.CurrentPlayerUUID = "test-player-uuid";
            service = new BlueprintLinkageService(playerContext, empireContext);
        }

        // -------------------------------------------------------------------
        // Test 1: ProcessItem with blueprint properties creates new blueprint
        // Validates: Requirement 1.1
        // -------------------------------------------------------------------

        [Test]
        public void ProcessItem_WithBlueprintProperties_CreatesNewBlueprint()
        {
            var apiItem = new GameApiAssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Test Blueprint",
                Evolution = 1,
                ShipPartType = "Sh",
                Properties = new List<GameApiAssetItemProperty>
                {
                    new GameApiAssetItemProperty
                    {
                        ModTypeId = 1,
                        PropertyName = "defence",
                        FriendlyPropertyName = "Defence",
                        PropertyValue = 45.5m,
                        OriginalPropertyValue = 30.0m,
                        Unit = "%",
                        ResearchPositive = true,
                        CanResearch = true,
                    },
                },
            };
            var localItem = new Item { UUID = "local-item-001", Name = "Test Blueprint" };

            bool result = service.ProcessItem(apiItem, localItem, "test-player-uuid");

            Assert.That(result, Is.True);
            var blueprints = playerContext.GetCurrentPlayerBlueprints();
            var created = blueprints.FirstOrDefault(b => b.Name == "Test Blueprint");
            Assert.That(created, Is.Not.Null);
            Assert.That(created.Evolution, Is.EqualTo(1));
            Assert.That(created.UUID, Is.Not.Null.And.Not.Empty);
        }

        // -------------------------------------------------------------------
        // Test 2: ProcessItem with existing match updates properties
        // Validates: Requirement 1.3
        // -------------------------------------------------------------------

        [Test]
        public void ProcessItem_WithExistingMatch_UpdatesProperties()
        {
            // Pre-add a blueprint with matching dedup key
            var existing = new OE2EmpireTracker.Models.Blueprint
            {
                UUID = "existing-bp-uuid",
                Name = "Test Blueprint",
                Evolution = 1,
                BluePrintType = "Shield",
                OwnerUUID = "test-player-uuid",
            };
            existing.Properties.SetProperty("Defence", "20.0");
            playerContext.AddBlueprint(existing);

            var apiItem = new GameApiAssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Test Blueprint",
                Evolution = 1,
                ShipPartType = "Sh",
                Properties = new List<GameApiAssetItemProperty>
                {
                    new GameApiAssetItemProperty
                    {
                        ModTypeId = 1,
                        PropertyName = "defence",
                        FriendlyPropertyName = "Defence",
                        PropertyValue = 55.0m,
                        OriginalPropertyValue = 30.0m,
                        Unit = "%",
                        ResearchPositive = true,
                        CanResearch = true,
                    },
                },
            };
            var localItem = new Item { UUID = "local-item-002", Name = "Test Blueprint" };

            bool result = service.ProcessItem(apiItem, localItem, "test-player-uuid");

            Assert.That(result, Is.True);
            Assert.That(existing.Properties.ContainsKey("Defence"), Is.True);
            existing.Properties.GetDecimal("Defence", 0, out decimal val);
            Assert.That(val, Is.EqualTo(55.0m));
        }

        // -------------------------------------------------------------------
        // Test 3: ProcessItem with empty properties returns false, no creation
        // Validates: Requirement 1.7
        // -------------------------------------------------------------------

        [Test]
        public void ProcessItem_WithEmptyProperties_ReturnsFalseNoCreation()
        {
            var apiItem = new GameApiAssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Empty Blueprint",
                Evolution = 1,
                ShipPartType = "Sh",
                Properties = new List<GameApiAssetItemProperty>(),
            };
            var localItem = new Item { UUID = "local-item-003", Name = "Empty Blueprint" };

            int countBefore = playerContext.GetCurrentPlayerBlueprints().Count;

            bool result = service.ProcessItem(apiItem, localItem, "test-player-uuid");

            Assert.That(result, Is.False);
            int countAfter = playerContext.GetCurrentPlayerBlueprints().Count;
            Assert.That(countAfter, Is.EqualTo(countBefore));
        }

        // -------------------------------------------------------------------
        // Test 4: ProcessItem sets BaseItemTypeID to created blueprint UUID
        // Validates: Requirement 5.1
        // -------------------------------------------------------------------

        [Test]
        public void ProcessItem_SetsBaseItemTypeID_ToCreatedBlueprintUUID()
        {
            var apiItem = new GameApiAssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Linked Blueprint",
                Evolution = 1,
                ShipPartType = "Re",
                Properties = new List<GameApiAssetItemProperty>
                {
                    new GameApiAssetItemProperty
                    {
                        ModTypeId = 2,
                        PropertyName = "power",
                        FriendlyPropertyName = "Power",
                        PropertyValue = 100.0m,
                        OriginalPropertyValue = 80.0m,
                        Unit = "MW",
                        ResearchPositive = true,
                        CanResearch = true,
                    },
                },
            };
            var localItem = new Item { UUID = "local-item-004", Name = "Linked Blueprint" };

            service.ProcessItem(apiItem, localItem, "test-player-uuid");

            var blueprints = playerContext.GetCurrentPlayerBlueprints();
            var created = blueprints.FirstOrDefault(b => b.Name == "Linked Blueprint");
            Assert.That(created, Is.Not.Null);
            Assert.That(localItem.BaseItemTypeID, Is.EqualTo(created.UUID));
        }
    }
}
