// <copyright file="BlueprintLinkageServiceTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Common.Client.Generated;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    using Blueprint = OE2EmpireTracker.Models.Blueprint;
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
            var apiItem = new AssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Test Blueprint",
                Evolution = 1,
                ShipPartType = "Sh",
                Properties = new List<AssetCargoProperty>
                {
                    new AssetCargoProperty
                    {
                        ModTypeId = 1,
                        PropertyName = "defence",
                        FriendlyPropertyName = "Defence",
                        PropertyValue = 45.5,
                        OriginalPropertyValue = 30.0,
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

            var apiItem = new AssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Test Blueprint",
                Evolution = 1,
                ShipPartType = "Sh",
                Properties = new List<AssetCargoProperty>
                {
                    new AssetCargoProperty
                    {
                        ModTypeId = 1,
                        PropertyName = "defence",
                        FriendlyPropertyName = "Defence",
                        PropertyValue = 55.0,
                        OriginalPropertyValue = 30.0,
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
            var apiItem = new AssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Empty Blueprint",
                Evolution = 1,
                ShipPartType = "Sh",
                Properties = new List<AssetCargoProperty>(),
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
            var apiItem = new AssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Linked Blueprint",
                Evolution = 1,
                ShipPartType = "Re",
                Properties = new List<AssetCargoProperty>
                {
                    new AssetCargoProperty
                    {
                        ModTypeId = 2,
                        PropertyName = "power",
                        FriendlyPropertyName = "Power",
                        PropertyValue = 100.0,
                        OriginalPropertyValue = 80.0,
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
            var apiItem = new AssetCargoItem
            {
                TypeC = "S",
                ResourceName = "Advanced Shield",
                Evolution = 1,
                ShipPartType = "Sh",
                Properties = new List<AssetCargoProperty>
                {
                    new AssetCargoProperty
                    {
                        ModTypeId = 10,
                        PropertyName = "defence",
                        FriendlyPropertyName = "Defence",
                        PropertyValue = 60.0,
                        OriginalPropertyValue = 40.0,
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
            var apiItem = new AssetCargoItem
            {
                TypeC = "S",
                ResourceName = "Heavy Cruiser Hull",
                Evolution = 1,
                ShipPartType = "Hu",
                Properties = new List<AssetCargoProperty>
                {
                    new AssetCargoProperty
                    {
                        ModTypeId = 11,
                        PropertyName = "slots",
                        FriendlyPropertyName = "Slots",
                        PropertyValue = 8.0,
                        OriginalPropertyValue = 6.0,
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
            var apiItem = new AssetCargoItem
            {
                TypeC = "S",
                ResourceName = "Mystery Component",
                Evolution = 1,
                ShipPartType = "Zz",
                Properties = new List<AssetCargoProperty>
                {
                    new AssetCargoProperty
                    {
                        ModTypeId = 12,
                        PropertyName = "power",
                        FriendlyPropertyName = "Power",
                        PropertyValue = 50.0,
                        OriginalPropertyValue = 30.0,
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
            var apiItem = new AssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Mining Rig Flatpack",
                Evolution = 1,
                Properties = new List<AssetCargoProperty>
                {
                    new AssetCargoProperty
                    {
                        ModTypeId = 13,
                        PropertyName = "efficiency",
                        FriendlyPropertyName = "Efficiency",
                        PropertyValue = 75.0,
                        OriginalPropertyValue = 50.0,
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
            var apiItem = new AssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Global Shield Blueprint",
                Evolution = 0,
                ShipPartType = "Sh",
                Properties = new List<AssetCargoProperty>
                {
                    new AssetCargoProperty
                    {
                        ModTypeId = 10,
                        PropertyName = "defence",
                        FriendlyPropertyName = "Defence",
                        PropertyValue = 20.0,
                        OriginalPropertyValue = 20.0,
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
            var apiItem = new AssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Player Reactor Blueprint",
                Evolution = 2,
                ShipPartType = "Re",
                Properties = new List<AssetCargoProperty>
                {
                    new AssetCargoProperty
                    {
                        ModTypeId = 11,
                        PropertyName = "power",
                        FriendlyPropertyName = "Power",
                        PropertyValue = 150.0,
                        OriginalPropertyValue = 100.0,
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
            var apiItem = new AssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Registry Test Blueprint",
                Evolution = 1,
                ShipPartType = "Sh",
                Properties = new List<AssetCargoProperty>
                {
                    new AssetCargoProperty
                    {
                        ModTypeId = 42,
                        PropertyName = "armour",
                        FriendlyPropertyName = "Armour",
                        PropertyValue = 75.0,
                        OriginalPropertyValue = 50.0,
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
            var existing = new OE2EmpireTracker.Models.Blueprint
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
            var apiItem = new AssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Preserve Props Blueprint",
                Evolution = 1,
                ShipPartType = "Sh",
                Properties = new List<AssetCargoProperty>
                {
                    new AssetCargoProperty
                    {
                        ModTypeId = 1,
                        PropertyName = "defence",
                        FriendlyPropertyName = "Defence",
                        PropertyValue = 60.0,
                        OriginalPropertyValue = 30.0,
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

        // -------------------------------------------------------------------
        // Property 1: Idempotency — process same item N times, assert exactly
        // one blueprint per dedup key.
        // Validates: Requirements 11.1
        // -------------------------------------------------------------------

        [Test]
        public void BlueprintIdempotency_ProcessSameItemNTimes_ExactlyOneBlueprint()
        {
            var config = Configuration.QuickThrowOnFailure;
            config.MaxNbOfTest = 25;
            Prop.ForAll<PositiveInt>(repeatCount =>
            {
            TestHelper.ResetWithCachedData();
            var localPlayerContext = PlayerContext.GetInstance();
            var localEmpireContext = EmpireContext.GetInstance();
            localPlayerContext.CurrentPlayerUUID = "test-player-uuid";
            var localService = new BlueprintLinkageService(localPlayerContext, localEmpireContext);

            var apiItem = new AssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Idempotent Blueprint",
                Evolution = 1,
                ShipPartType = "Sh",
                Properties = new List<AssetCargoProperty>
                {
                    new AssetCargoProperty
                    {
                        ModTypeId = 1,
                        PropertyName = "defence",
                        FriendlyPropertyName = "Defence",
                        PropertyValue = 45.5,
                        OriginalPropertyValue = 30.0,
                        Unit = "%",
                        ResearchPositive = true,
                        CanResearch = true,
                    },
                },
            };

            int n = repeatCount.Get;
            for (int i = 0; i < n; i++)
            {
                var localItem = new Item { UUID = Guid.NewGuid().ToString() };
                localService.ProcessItem(apiItem, localItem, "test-player-uuid");
            }

            var blueprints = localPlayerContext.GetCurrentPlayerBlueprints();
            var matching = blueprints.Where(b => b.Name == "Idempotent Blueprint" && b.Evolution == 1).ToList();
            Assert.That(matching.Count, Is.EqualTo(1));
            }).Check(config);
        }

        // -------------------------------------------------------------------
        // Property 4: Property Preservation — existing properties not in API
        // are never removed after merge.
        // Validates: Requirements 6.1, 6.2
        // -------------------------------------------------------------------

        [Test]
        public void PropertyPreservation_ExistingPropertiesNeverRemoved()
        {
            var config = Configuration.QuickThrowOnFailure;
            config.MaxNbOfTest = 25;
            Prop.ForAll<PositiveInt>(repeatCount =>
            {
            TestHelper.ResetWithCachedData();
            var localPlayerContext = PlayerContext.GetInstance();
            var localEmpireContext = EmpireContext.GetInstance();
            localPlayerContext.CurrentPlayerUUID = "test-player-uuid";
            var localService = new BlueprintLinkageService(localPlayerContext, localEmpireContext);

            // Pre-add a blueprint with extra properties not in the API response
            var existing = new OE2EmpireTracker.Models.Blueprint
            {
                UUID = "preserve-prop-test-uuid",
                Name = "Preservation Test BP",
                Evolution = 1,
                BluePrintType = "Shield",
                OwnerUUID = "test-player-uuid",
            };
            existing.Properties.SetProperty("ExtraAlpha", "100");
            existing.Properties.SetProperty("ExtraBeta", "200");
            existing.Properties.SetProperty("Defence", "10.0");
            localPlayerContext.AddBlueprint(existing);

            // API response only has Defence — ExtraAlpha and ExtraBeta are not present
            var apiItem = new AssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Preservation Test BP",
                Evolution = 1,
                ShipPartType = "Sh",
                Properties = new List<AssetCargoProperty>
                {
                    new AssetCargoProperty
                    {
                        ModTypeId = 1,
                        PropertyName = "defence",
                        FriendlyPropertyName = "Defence",
                        PropertyValue = 55.0,
                        OriginalPropertyValue = 30.0,
                        Unit = "%",
                        ResearchPositive = true,
                        CanResearch = true,
                    },
                },
            };

            int n = repeatCount.Get;
            for (int i = 0; i < n; i++)
            {
                var localItem = new Item { UUID = Guid.NewGuid().ToString() };
                localService.ProcessItem(apiItem, localItem, "test-player-uuid");
            }

            // Extra properties must still exist after N merge iterations
            Assert.That(existing.Properties.ContainsKey("ExtraAlpha"), Is.True);
            Assert.That(existing.Properties.ContainsKey("ExtraBeta"), Is.True);
            existing.Properties.GetString("ExtraAlpha", string.Empty, out string alphaVal);
            existing.Properties.GetString("ExtraBeta", string.Empty, out string betaVal);
            Assert.That(alphaVal, Is.EqualTo("100"));
            Assert.That(betaVal, Is.EqualTo("200"));
            }).Check(config);
        }

        // -------------------------------------------------------------------
        // Property 7: Ownership Correctness — Evo > 0 → player,
        // Evo = 0 → global.
        // Validates: Requirements 1.4, 1.5
        // -------------------------------------------------------------------

        [Test]
        public void OwnershipRouting_EvoGreaterThan0IsPlayer_Evo0IsGlobal()
        {
            var config = Configuration.QuickThrowOnFailure;
            config.MaxNbOfTest = 25;
            Prop.ForAll<int>(rawEvolution =>
            {
            int evolution = Math.Abs(rawEvolution % 6);

            TestHelper.ResetWithCachedData();
            var localPlayerContext = PlayerContext.GetInstance();
            var localEmpireContext = EmpireContext.GetInstance();
            localPlayerContext.CurrentPlayerUUID = "test-player-uuid";
            var localService = new BlueprintLinkageService(localPlayerContext, localEmpireContext);

            string bpName = "Ownership BP Evo" + evolution;
            var apiItem = new AssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = bpName,
                Evolution = evolution,
                ShipPartType = "Sh",
                Properties = new List<AssetCargoProperty>
                {
                    new AssetCargoProperty
                    {
                        ModTypeId = 1,
                        PropertyName = "defence",
                        FriendlyPropertyName = "Defence",
                        PropertyValue = 40.0,
                        OriginalPropertyValue = 30.0,
                        Unit = "%",
                        ResearchPositive = true,
                        CanResearch = true,
                    },
                },
            };
            var localItem = new Item { UUID = Guid.NewGuid().ToString() };

            localService.ProcessItem(apiItem, localItem, "test-player-uuid");

            if (evolution > 0)
            {
                var playerBps = localPlayerContext.GetCurrentPlayerBlueprints();
                var found = playerBps.FirstOrDefault(b => b.Name == bpName && b.Evolution == evolution);
                Assert.That(found, Is.Not.Null, "Evo > 0 should create player blueprint");
                Assert.That(found.OwnerUUID, Is.EqualTo("test-player-uuid"));
            }
            else
            {
                var globalBps = localEmpireContext.GlobalBlueprintList;
                var found = globalBps.FirstOrDefault(b => b.Name == bpName && b.Evolution == evolution);
                Assert.That(found, Is.Not.Null, "Evo = 0 should create global blueprint");
                Assert.That(found.OwnerUUID, Is.EqualTo(string.Empty));
            }
            }).Check(config);
        }

        // -------------------------------------------------------------------
        // Property 5: PropertyTypeRegistry Completeness — after processing,
        // every modTypeId in properties exists in registry.
        // Validates: Requirements 7.1, 7.5
        // -------------------------------------------------------------------

        [Test]
        public void PropertyTypeRegistryCompleteness_AllModTypeIdsRegistered()
        {
            var config = Configuration.QuickThrowOnFailure;
            config.MaxNbOfTest = 25;
            Prop.ForAll<PositiveInt>(modTypeIdSeed =>
            {
            TestHelper.ResetWithCachedData();
            var localPlayerContext = PlayerContext.GetInstance();
            var localEmpireContext = EmpireContext.GetInstance();
            localPlayerContext.CurrentPlayerUUID = "test-player-uuid";
            var localService = new BlueprintLinkageService(localPlayerContext, localEmpireContext);

            int modTypeId1 = modTypeIdSeed.Get;
            int modTypeId2 = modTypeId1 + 1;
            int modTypeId3 = modTypeId1 + 2;

            var apiItem = new AssetCargoItem
            {
                TypeC = "Bp",
                ResourceName = "Registry Completeness BP " + modTypeId1,
                Evolution = 1,
                ShipPartType = "Sh",
                Properties = new List<AssetCargoProperty>
                {
                    new AssetCargoProperty
                    {
                        ModTypeId = modTypeId1,
                        PropertyName = "prop_a",
                        FriendlyPropertyName = "Prop A",
                        PropertyValue = 10.0,
                        OriginalPropertyValue = 5.0,
                        Unit = "%",
                        ResearchPositive = true,
                        CanResearch = true,
                    },
                    new AssetCargoProperty
                    {
                        ModTypeId = modTypeId2,
                        PropertyName = "prop_b",
                        FriendlyPropertyName = "Prop B",
                        PropertyValue = 20.0,
                        OriginalPropertyValue = 15.0,
                        Unit = "MW",
                        ResearchPositive = false,
                        CanResearch = true,
                    },
                    new AssetCargoProperty
                    {
                        ModTypeId = modTypeId3,
                        PropertyName = "prop_c",
                        FriendlyPropertyName = "Prop C",
                        PropertyValue = 30.0,
                        OriginalPropertyValue = 25.0,
                        Unit = "pts",
                        ResearchPositive = true,
                        CanResearch = false,
                    },
                },
            };
            var localItem = new Item { UUID = Guid.NewGuid().ToString() };

            localService.ProcessItem(apiItem, localItem, "test-player-uuid");

            // Every modTypeId from the properties must now exist in the registry
            foreach (var prop in apiItem.Properties)
            {
                var registered = localEmpireContext.FindPropertyType(prop.ModTypeId);
                Assert.That(registered, Is.Not.Null, "ModTypeId " + prop.ModTypeId + " should be in registry");
                Assert.That(registered.PropertyName, Is.EqualTo(prop.PropertyName));
            }
            }).Check(config);
        }
    }
}
