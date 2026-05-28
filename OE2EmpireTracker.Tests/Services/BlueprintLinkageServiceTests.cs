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
    /// Validates: Requirements 1.1, 1.2, 1.3, 1.7, 2.1, 2.2, 3.1, 3.2, 3.3, 3.4, 5.1.
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

        // -------------------------------------------------------------------
        // Test 5: ProcessItem with ship part maps ShipPartType correctly
        // Validates: Requirements 2.1, 2.2, 3.2
        // -------------------------------------------------------------------

        [Test]
        public void ProcessItem_ShipPart_MapsShipPartTypeCorrectly()
        {
            var apiItem = new GameApiAssetCargoItem
            {
                TypeC = "S",
                ResourceName = "Advanced Shield",
                Evolution = 1,
                ShipPartType = "Sh",
                Properties = new List<GameApiAssetItemProperty>
                {
                    new GameApiAssetItemProperty
                    {
                        ModTypeId = 10,
                        PropertyName = "defence",
                        FriendlyPropertyName = "Defence",
                        PropertyValue = 60.0m,
                        OriginalPropertyValue = 40.0m,
                        Unit = "%",
                        ResearchPositive = true,
                        CanResearch = true,
                    },
                },
            };
            var localItem = new Item { UUID = "local-item-005", Name = "Advanced Shield" };

            bool result = service.ProcessItem(apiItem, localItem, "test-player-uuid");

            Assert.That(result, Is.True);
            var blueprints = playerContext.GetCurrentPlayerBlueprints();
            var created = blueprints.FirstOrDefault(b => b.Name == "Advanced Shield");
            Assert.That(created, Is.Not.Null);
            Assert.That(created.BluePrintType, Is.EqualTo("Shield"));
        }

        // -------------------------------------------------------------------
        // Test 6: ProcessItem with hull classifies as Hull type
        // Validates: Requirements 2.1, 3.1
        // -------------------------------------------------------------------

        [Test]
        public void ProcessItem_Hull_ClassifiesAsHullType()
        {
            var apiItem = new GameApiAssetCargoItem
            {
                TypeC = "S",
                ResourceName = "Heavy Cruiser Hull",
                Evolution = 1,
                ShipPartType = "Hu",
                Properties = new List<GameApiAssetItemProperty>
                {
                    new GameApiAssetItemProperty
                    {
                        ModTypeId = 11,
                        PropertyName = "slots",
                        FriendlyPropertyName = "Slots",
                        PropertyValue = 8.0m,
                        OriginalPropertyValue = 6.0m,
                        Unit = string.Empty,
                        ResearchPositive = true,
                        CanResearch = true,
                    },
                },
            };
            var localItem = new Item { UUID = "local-item-006", Name = "Heavy Cruiser Hull" };

            bool result = service.ProcessItem(apiItem, localItem, "test-player-uuid");

            Assert.That(result, Is.True);
            var blueprints = playerContext.GetCurrentPlayerBlueprints();
            var created = blueprints.FirstOrDefault(b => b.Name == "Heavy Cruiser Hull");
            Assert.That(created, Is.Not.Null);
            Assert.That(created.BluePrintType, Is.EqualTo("Hull"));
        }

        // -------------------------------------------------------------------
        // Test 7: ProcessItem with unknown ShipPartType uses raw value
        // Validates: Requirements 3.4
        // -------------------------------------------------------------------

        [Test]
        public void ProcessItem_UnknownShipPartType_UsesRawValueAndLogsWarning()
        {
            var apiItem = new GameApiAssetCargoItem
            {
                TypeC = "S",
                ResourceName = "Mystery Component",
                Evolution = 1,
                ShipPartType = "Zz",
                Properties = new List<GameApiAssetItemProperty>
                {
                    new GameApiAssetItemProperty
                    {
                        ModTypeId = 12,
                        PropertyName = "power",
                        FriendlyPropertyName = "Power",
                        PropertyValue = 50.0m,
                        OriginalPropertyValue = 30.0m,
                        Unit = "MW",
                        ResearchPositive = true,
                        CanResearch = true,
                    },
                },
            };
            var localItem = new Item { UUID = "local-item-007", Name = "Mystery Component" };

            bool result = service.ProcessItem(apiItem, localItem, "test-player-uuid");

            Assert.That(result, Is.True);
            var blueprints = playerContext.GetCurrentPlayerBlueprints();
            var created = blueprints.FirstOrDefault(b => b.Name == "Mystery Component");
            Assert.That(created, Is.Not.Null);
            Assert.That(created.BluePrintType, Is.EqualTo("Zz"));
        }

        // -------------------------------------------------------------------
        // Test 8: ProcessItem with Flatpack classifies as Flatpack type
        // Validates: Requirements 3.3
        // -------------------------------------------------------------------

        [Test]
        public void ProcessItem_Flatpack_ClassifiesAsFlatpackType()
        {
            var apiItem = new GameApiAssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Mining Rig Flatpack",
                Evolution = 1,
                Properties = new List<GameApiAssetItemProperty>
                {
                    new GameApiAssetItemProperty
                    {
                        ModTypeId = 13,
                        PropertyName = "efficiency",
                        FriendlyPropertyName = "Efficiency",
                        PropertyValue = 75.0m,
                        OriginalPropertyValue = 50.0m,
                        Unit = "%",
                        ResearchPositive = true,
                        CanResearch = true,
                    },
                },
            };
            var localItem = new Item { UUID = "local-item-008", Name = "Mining Rig Flatpack" };

            bool result = service.ProcessItem(apiItem, localItem, "test-player-uuid");

            Assert.That(result, Is.True);
            var blueprints = playerContext.GetCurrentPlayerBlueprints();
            var created = blueprints.FirstOrDefault(b => b.Name == "Mining Rig Flatpack");
            Assert.That(created, Is.Not.Null);
            Assert.That(created.BluePrintType, Does.StartWith("Flatpacks/"));
        }

        // -------------------------------------------------------------------
        // Test 9: ProcessItem with Evo 0 creates global blueprint
        // Validates: Requirements 1.4, 1.5
        // -------------------------------------------------------------------

        [Test]
        public void ProcessItem_Evo0_CreatesGlobalBlueprint()
        {
            var apiItem = new GameApiAssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Global Shield Blueprint",
                Evolution = 0,
                ShipPartType = "Sh",
                Properties = new List<GameApiAssetItemProperty>
                {
                    new GameApiAssetItemProperty
                    {
                        ModTypeId = 10,
                        PropertyName = "defence",
                        FriendlyPropertyName = "Defence",
                        PropertyValue = 20.0m,
                        OriginalPropertyValue = 20.0m,
                        Unit = "%",
                        ResearchPositive = true,
                        CanResearch = true,
                    },
                },
            };
            var localItem = new Item { UUID = "local-item-010", Name = "Global Shield Blueprint" };

            int globalCountBefore = empireContext.GlobalBlueprintList.Count;
            int playerCountBefore = playerContext.GetCurrentPlayerBlueprints().Count;

            bool result = service.ProcessItem(apiItem, localItem, "test-player-uuid");

            Assert.That(result, Is.True);
            Assert.That(empireContext.GlobalBlueprintList.Count, Is.EqualTo(globalCountBefore + 1));
            Assert.That(playerContext.GetCurrentPlayerBlueprints().Count, Is.EqualTo(playerCountBefore));

            var created = empireContext.GlobalBlueprintList.FirstOrDefault(
                b => b.Name == "Global Shield Blueprint");
            Assert.That(created, Is.Not.Null);
            Assert.That(created.OwnerUUID, Is.EqualTo(string.Empty));
        }

        // -------------------------------------------------------------------
        // Test 10: ProcessItem with Evo > 0 creates player blueprint
        // Validates: Requirements 1.4, 1.5
        // -------------------------------------------------------------------

        [Test]
        public void ProcessItem_EvoGreaterThan0_CreatesPlayerBlueprint()
        {
            var apiItem = new GameApiAssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Player Reactor Blueprint",
                Evolution = 2,
                ShipPartType = "Re",
                Properties = new List<GameApiAssetItemProperty>
                {
                    new GameApiAssetItemProperty
                    {
                        ModTypeId = 11,
                        PropertyName = "power",
                        FriendlyPropertyName = "Power",
                        PropertyValue = 150.0m,
                        OriginalPropertyValue = 100.0m,
                        Unit = "MW",
                        ResearchPositive = true,
                        CanResearch = true,
                    },
                },
            };
            var localItem = new Item { UUID = "local-item-011", Name = "Player Reactor Blueprint" };

            int playerCountBefore = playerContext.GetCurrentPlayerBlueprints().Count;

            bool result = service.ProcessItem(apiItem, localItem, "test-player-uuid");

            Assert.That(result, Is.True);
            Assert.That(
                playerContext.GetCurrentPlayerBlueprints().Count,
                Is.EqualTo(playerCountBefore + 1));

            var created = playerContext.GetCurrentPlayerBlueprints().FirstOrDefault(
                b => b.Name == "Player Reactor Blueprint");
            Assert.That(created, Is.Not.Null);
            Assert.That(created.OwnerUUID, Is.EqualTo("test-player-uuid"));
        }

        // -------------------------------------------------------------------
        // Test 11: ProcessItem registers property types in registry
        // Validates: Requirements 6.1, 6.2, 7.1, 7.5
        // -------------------------------------------------------------------

        [Test]
        public void ProcessItem_RegistersPropertyTypes_InRegistry()
        {
            var apiItem = new GameApiAssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Registry Test Blueprint",
                Evolution = 1,
                ShipPartType = "Sh",
                Properties = new List<GameApiAssetItemProperty>
                {
                    new GameApiAssetItemProperty
                    {
                        ModTypeId = 42,
                        PropertyName = "armour",
                        FriendlyPropertyName = "Armour",
                        PropertyValue = 75.0m,
                        OriginalPropertyValue = 50.0m,
                        Unit = "pts",
                        ResearchPositive = true,
                        CanResearch = false,
                    },
                },
            };
            var localItem = new Item { UUID = "local-item-012", Name = "Registry Test Blueprint" };

            service.ProcessItem(apiItem, localItem, "test-player-uuid");

            var propType = empireContext.FindPropertyType(42);
            Assert.That(propType, Is.Not.Null);
            Assert.That(propType.PropertyName, Is.EqualTo("armour"));
            Assert.That(propType.FriendlyPropertyName, Is.EqualTo("Armour"));
            Assert.That(propType.Unit, Is.EqualTo("pts"));
            Assert.That(propType.ResearchPositive, Is.True);
            Assert.That(propType.CanResearch, Is.False);
        }

        // -------------------------------------------------------------------
        // Test 12: ProcessItem preserves existing properties not in API response
        // Validates: Requirements 6.1, 6.2
        // -------------------------------------------------------------------

        [Test]
        public void ProcessItem_PreservesExistingProperties_NotInApiResponse()
        {
            // Pre-add a blueprint with a custom property
            var existing = new Blueprint
            {
                UUID = "existing-preserve-uuid",
                Name = "Preserve Props Blueprint",
                Evolution = 1,
                BluePrintType = "Shield",
                OwnerUUID = "test-player-uuid",
            };
            existing.Properties.SetProperty("CustomProp", "999");
            existing.Properties.SetProperty("Defence", "10.0");
            playerContext.AddBlueprint(existing);

            // API response only has Defence, not CustomProp
            var apiItem = new GameApiAssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Preserve Props Blueprint",
                Evolution = 1,
                ShipPartType = "Sh",
                Properties = new List<GameApiAssetItemProperty>
                {
                    new GameApiAssetItemProperty
                    {
                        ModTypeId = 1,
                        PropertyName = "defence",
                        FriendlyPropertyName = "Defence",
                        PropertyValue = 60.0m,
                        OriginalPropertyValue = 30.0m,
                        Unit = "%",
                        ResearchPositive = true,
                        CanResearch = true,
                    },
                },
            };
            var localItem = new Item { UUID = "local-item-013", Name = "Preserve Props Blueprint" };

            service.ProcessItem(apiItem, localItem, "test-player-uuid");

            // CustomProp should still exist (additive merge, no removal)
            Assert.That(existing.Properties.ContainsKey("CustomProp"), Is.True);
            existing.Properties.GetString("CustomProp", string.Empty, out string customVal);
            Assert.That(customVal, Is.EqualTo("999"));

            // Defence should be updated to new value
            existing.Properties.GetDecimal("Defence", 0, out decimal defVal);
            Assert.That(defVal, Is.EqualTo(60.0m));
        }
    }
}
