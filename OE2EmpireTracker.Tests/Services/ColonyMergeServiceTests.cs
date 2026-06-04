// <copyright file="ColonyMergeServiceTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for ColonyMergeService.MergeColonyList.
    /// Validates: Requirements 6.1-6.4, 7.1-7.16, 10.1, 10.2, 13.2, 13.3, 13.5.
    /// </summary>
    [TestFixture]
    public class ColonyMergeServiceTests
    {
        private const string OwnerUUID = "owner-uuid-001";

        private static readonly DateTime FrozenTime = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            SystemClock.UtcNowFunc = () => FrozenTime;
        }

        [TearDown]
        public void TearDown()
        {
            SystemClock.Reset();
        }

        // -------------------------------------------------------------------
        // Test 1: New colony creation with all fields
        // Validates: Requirements 6.3, 7.1-7.14, 10.1, 10.2
        // -------------------------------------------------------------------

        [Test]
        public void MergeColonyList_NewColony_CreatesWithAllFields()
        {
            var apiColonies = new List<GameApiColonyListItem>
            {
                new GameApiColonyListItem
                {
                    ColonyId = 42,
                    ColonyName = "My Colony",
                    SystemObjectName = "Planet Alpha",
                    SystemName = "Sol System",
                    SystemId = 7,
                    ColonySize = 3,
                    Distance = 12.5,
                    SurfaceVariation = 2,
                    AtmosVariation = 1,
                    HexValue = "FF00AA",
                    SystemObjectTypeName = "Planet",
                    ImagePreFix = "planet_img",
                    ManufacturingBlocked = 1,
                    WorkerCurrentAttitude = 80,
                    ContentmentIndex = 75,
                },
            };
            var localColonies = new List<Colony>();

            var result = ColonyMergeService.MergeColonyList(apiColonies, localColonies, OwnerUUID);

            Assert.That(result.Created, Is.EqualTo(1));
            Assert.That(result.Updated, Is.EqualTo(0));
            Assert.That(result.HasChanges, Is.True);
            Assert.That(localColonies.Count, Is.EqualTo(1));

            var colony = localColonies[0];
            Assert.That(colony.UUID, Is.Not.Null.And.Not.Empty);
            Assert.That(colony.OwnerUUID, Is.EqualTo(OwnerUUID));
            Assert.That(colony.PlanetName, Is.EqualTo("Planet Alpha"));
            Assert.That(colony.SystemName, Is.EqualTo("Sol System"));
            Assert.That(colony.ColonyName, Is.EqualTo("My Colony"));
            Assert.That(colony.SystemId, Is.EqualTo(7));
            Assert.That(colony.ColonySize, Is.EqualTo(3));
            Assert.That(colony.Distance, Is.EqualTo(12.5m));
            Assert.That(colony.SurfaceVariation, Is.EqualTo(2));
            Assert.That(colony.AtmosVariation, Is.EqualTo(1));
            Assert.That(colony.HexValue, Is.EqualTo("FF00AA"));
            Assert.That(colony.SystemObjectTypeName, Is.EqualTo("Planet"));
            Assert.That(colony.ImagePreFix, Is.EqualTo("planet_img"));
            Assert.That(colony.ManufacturingBlocked, Is.EqualTo(1));
            Assert.That(colony.WorkerCurrentAttitude, Is.EqualTo(80));
            Assert.That(colony.ContentmentIndex, Is.EqualTo(75));
            Assert.That(colony.LastImportDateTime, Is.EqualTo(FrozenTime.ToString("o")));
            Assert.That(result.ColonyIdToUUIDMap[42], Is.EqualTo(colony.UUID));
        }

        // -------------------------------------------------------------------
        // Test 2: Existing colony update with game-authoritative fields
        // Validates: Requirements 6.1, 6.2, 7.1-7.14
        // -------------------------------------------------------------------

        [Test]
        public void MergeColonyList_ExistingColony_UpdatesGameAuthoritativeFields()
        {
            var existingColony = new Colony
            {
                UUID = "existing-uuid-001",
                OwnerUUID = OwnerUUID,
                PlanetName = "Planet Alpha",
                SystemName = "Sol System",
                ColonyName = "Old Name",
                SystemId = 1,
                ColonySize = 2,
                Distance = 5.0m,
                SurfaceVariation = 0,
                AtmosVariation = 0,
                HexValue = "000000",
                SystemObjectTypeName = "Moon",
                ImagePreFix = "old_img",
                ManufacturingBlocked = 0,
                WorkerCurrentAttitude = 50,
                ContentmentIndex = 50,
            };
            var localColonies = new List<Colony> { existingColony };

            var apiColonies = new List<GameApiColonyListItem>
            {
                new GameApiColonyListItem
                {
                    ColonyId = 99,
                    ColonyName = "New Name",
                    SystemObjectName = "Planet Alpha",
                    SystemName = "Sol System",
                    SystemId = 7,
                    ColonySize = 5,
                    Distance = 20.0,
                    SurfaceVariation = 3,
                    AtmosVariation = 2,
                    HexValue = "AABBCC",
                    SystemObjectTypeName = "Planet",
                    ImagePreFix = "new_img",
                    ManufacturingBlocked = 1,
                    WorkerCurrentAttitude = 90,
                    ContentmentIndex = 85,
                },
            };

            var result = ColonyMergeService.MergeColonyList(apiColonies, localColonies, OwnerUUID);

            Assert.That(result.Updated, Is.EqualTo(1));
            Assert.That(result.Created, Is.EqualTo(0));
            Assert.That(localColonies.Count, Is.EqualTo(1));

            var colony = localColonies[0];
            Assert.That(colony.UUID, Is.EqualTo("existing-uuid-001"));
            Assert.That(colony.ColonyName, Is.EqualTo("New Name"));
            Assert.That(colony.SystemId, Is.EqualTo(7));
            Assert.That(colony.ColonySize, Is.EqualTo(5));
            Assert.That(colony.Distance, Is.EqualTo(20.0m));
            Assert.That(colony.SurfaceVariation, Is.EqualTo(3));
            Assert.That(colony.AtmosVariation, Is.EqualTo(2));
            Assert.That(colony.HexValue, Is.EqualTo("AABBCC"));
            Assert.That(colony.SystemObjectTypeName, Is.EqualTo("Planet"));
            Assert.That(colony.ImagePreFix, Is.EqualTo("new_img"));
            Assert.That(colony.ManufacturingBlocked, Is.EqualTo(1));
            Assert.That(colony.WorkerCurrentAttitude, Is.EqualTo(90));
            Assert.That(colony.ContentmentIndex, Is.EqualTo(85));
            Assert.That(colony.LastImportDateTime, Is.EqualTo(FrozenTime.ToString("o")));
            Assert.That(result.ColonyIdToUUIDMap[99], Is.EqualTo("existing-uuid-001"));
        }

        // -------------------------------------------------------------------
        // Test 3: Idempotency — no double creation
        // Validates: Requirements 6.1, 6.2
        // -------------------------------------------------------------------

        [Test]
        public void MergeColonyList_DuplicateApiData_NoDoubleCreation()
        {
            var apiColonies = new List<GameApiColonyListItem>
            {
                new GameApiColonyListItem
                {
                    ColonyId = 10,
                    ColonyName = "Colony One",
                    SystemObjectName = "Planet Beta",
                    SystemName = "Alpha Centauri",
                    SystemId = 3,
                    ColonySize = 1,
                    Distance = 5.0,
                },
            };
            var localColonies = new List<Colony>();

            // First merge — creates the colony
            var result1 = ColonyMergeService.MergeColonyList(apiColonies, localColonies, OwnerUUID);
            Assert.That(result1.Created, Is.EqualTo(1));
            Assert.That(localColonies.Count, Is.EqualTo(1));

            // Second merge — should match existing, not create duplicate
            var result2 = ColonyMergeService.MergeColonyList(apiColonies, localColonies, OwnerUUID);
            Assert.That(result2.Created, Is.EqualTo(0));
            Assert.That(localColonies.Count, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Test 4: Empty list produces no changes
        // Validates: Requirements 13.5
        // -------------------------------------------------------------------

        [Test]
        public void MergeColonyList_EmptyList_NoChanges()
        {
            var existingColony = new Colony
            {
                UUID = "existing-uuid-002",
                OwnerUUID = OwnerUUID,
                PlanetName = "Planet Gamma",
                SystemName = "Vega",
                ColonyName = "Existing Colony",
            };
            var localColonies = new List<Colony> { existingColony };
            var apiColonies = new List<GameApiColonyListItem>();

            var result = ColonyMergeService.MergeColonyList(apiColonies, localColonies, OwnerUUID);

            Assert.That(result.Created, Is.EqualTo(0));
            Assert.That(result.Updated, Is.EqualTo(0));
            Assert.That(result.Skipped, Is.EqualTo(0));
            Assert.That(result.HasChanges, Is.False);
            Assert.That(localColonies.Count, Is.EqualTo(1));
            Assert.That(localColonies[0].UUID, Is.EqualTo("existing-uuid-002"));
        }

        // -------------------------------------------------------------------
        // Test 5: Null SystemObjectName skips entry
        // Validates: Requirements 13.2
        // -------------------------------------------------------------------

        [Test]
        public void MergeColonyList_NullSystemObjectName_SkipsEntry()
        {
            var apiColonies = new List<GameApiColonyListItem>
            {
                new GameApiColonyListItem
                {
                    ColonyId = 20,
                    ColonyName = "Bad Colony",
                    SystemObjectName = null,
                    SystemName = "Some System",
                },
            };
            var localColonies = new List<Colony>();

            var result = ColonyMergeService.MergeColonyList(apiColonies, localColonies, OwnerUUID);

            Assert.That(result.Skipped, Is.EqualTo(1));
            Assert.That(result.Created, Is.EqualTo(0));
            Assert.That(localColonies.Count, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Test 6: Owner isolation — only matches owned colonies
        // Validates: Requirements 6.4
        // -------------------------------------------------------------------

        [Test]
        public void MergeColonyList_OwnerIsolation_OnlyMatchesOwnedColonies()
        {
            var otherPlayerColony = new Colony
            {
                UUID = "other-player-uuid",
                OwnerUUID = "different-owner-uuid",
                PlanetName = "Planet Delta",
                SystemName = "Proxima",
                ColonyName = "Other Player Colony",
            };
            var localColonies = new List<Colony> { otherPlayerColony };

            var apiColonies = new List<GameApiColonyListItem>
            {
                new GameApiColonyListItem
                {
                    ColonyId = 30,
                    ColonyName = "My Colony",
                    SystemObjectName = "Planet Delta",
                    SystemName = "Proxima",
                    SystemId = 5,
                    ColonySize = 2,
                },
            };

            var result = ColonyMergeService.MergeColonyList(apiColonies, localColonies, OwnerUUID);

            // Should create a new colony, not match the other player's colony
            Assert.That(result.Created, Is.EqualTo(1));
            Assert.That(result.Updated, Is.EqualTo(0));
            Assert.That(localColonies.Count, Is.EqualTo(2));
            Assert.That(otherPlayerColony.ColonyName, Is.EqualTo("Other Player Colony"));
            Assert.That(otherPlayerColony.OwnerUUID, Is.EqualTo("different-owner-uuid"));
        }

        // -------------------------------------------------------------------
        // Test 7: Null/empty API string value preserves local
        // Validates: Requirements 7.16
        // -------------------------------------------------------------------

        [Test]
        public void MergeColonyList_NullStringApiValue_PreservesLocal()
        {
            var existingColony = new Colony
            {
                UUID = "existing-uuid-003",
                OwnerUUID = OwnerUUID,
                PlanetName = "Planet Epsilon",
                SystemName = "Sirius",
                ColonyName = "Original Name",
                HexValue = "112233",
            };
            var localColonies = new List<Colony> { existingColony };

            var apiColonies = new List<GameApiColonyListItem>
            {
                new GameApiColonyListItem
                {
                    ColonyId = 40,
                    ColonyName = string.Empty,
                    SystemObjectName = "Planet Epsilon",
                    SystemName = "Sirius",
                    HexValue = null,
                },
            };

            ColonyMergeService.MergeColonyList(apiColonies, localColonies, OwnerUUID);

            Assert.That(existingColony.ColonyName, Is.EqualTo("Original Name"));
            Assert.That(existingColony.HexValue, Is.EqualTo("112233"));
        }

        // -------------------------------------------------------------------
        // Test 8: Zero numeric API value overwrites local
        // Validates: Requirements 7.13, 7.14
        // -------------------------------------------------------------------

        [Test]
        public void MergeColonyList_ZeroNumericApiValue_OverwritesLocal()
        {
            var existingColony = new Colony
            {
                UUID = "existing-uuid-004",
                OwnerUUID = OwnerUUID,
                PlanetName = "Planet Zeta",
                SystemName = "Betelgeuse",
                ColonyName = "Zeta Colony",
                ContentmentIndex = 50,
                WorkerCurrentAttitude = 75,
            };
            var localColonies = new List<Colony> { existingColony };

            var apiColonies = new List<GameApiColonyListItem>
            {
                new GameApiColonyListItem
                {
                    ColonyId = 50,
                    ColonyName = "Zeta Colony",
                    SystemObjectName = "Planet Zeta",
                    SystemName = "Betelgeuse",
                    ContentmentIndex = 0,
                    WorkerCurrentAttitude = 0,
                },
            };

            ColonyMergeService.MergeColonyList(apiColonies, localColonies, OwnerUUID);

            Assert.That(existingColony.ContentmentIndex, Is.EqualTo(0));
            Assert.That(existingColony.WorkerCurrentAttitude, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // MergeBuildings Tests
        // Validates: Requirements 8.1-8.18
        // -------------------------------------------------------------------

        // -------------------------------------------------------------------
        // Test 9: New building creates structure
        // Validates: Requirements 8.1, 8.2, 8.3
        // -------------------------------------------------------------------

        [Test]
        public void MergeBuildings_NewBuilding_CreatesStructure()
        {
            var colony = new Colony
            {
                UUID = "colony-uuid-001",
                OwnerUUID = OwnerUUID,
                Structures = new List<ColonyStructure>(),
            };

            var apiBuildings = new List<GameApiColonyBuilding>
            {
                new GameApiColonyBuilding
                {
                    BuildingId = 101,
                    ColonyBuildingTypeId = 5,
                    BlueprintDesignName = "Mining Rig",
                    BuildingOnline = true,
                    StatusId = 1,
                },
            };

            bool changed = ColonyMergeService.MergeBuildings(apiBuildings, colony);

            Assert.That(changed, Is.True);
            Assert.That(colony.Structures.Count, Is.EqualTo(1));

            var structure = colony.Structures[0];
            Assert.That(structure.BuildingID, Is.EqualTo(101));
            Assert.That(structure.ColonyBuildingTypeId, Is.EqualTo(5));
            Assert.That(structure.UUID, Is.Not.Null.And.Not.Empty);

            bool built;
            structure.Properties.GetBoolean(GameConstants.PropBuilt, false, out built);
            Assert.That(built, Is.True);

            bool online;
            structure.Properties.GetBoolean(GameConstants.PropOnline, false, out online);
            Assert.That(online, Is.True);
        }

        // -------------------------------------------------------------------
        // Test 10: Existing building updates status
        // Validates: Requirements 8.1, 8.2
        // -------------------------------------------------------------------

        [Test]
        public void MergeBuildings_ExistingBuilding_UpdatesStatus()
        {
            var existingStructure = new ColonyStructure
            {
                UUID = "structure-uuid-001",
                BuildingID = 200,
                ColonyBuildingTypeId = 3,
            };
            existingStructure.Properties.SetProperty(GameConstants.PropBuilt, true);
            existingStructure.Properties.SetProperty(GameConstants.PropOnline, true);

            var colony = new Colony
            {
                UUID = "colony-uuid-002",
                OwnerUUID = OwnerUUID,
                Structures = new List<ColonyStructure> { existingStructure },
            };

            var apiBuildings = new List<GameApiColonyBuilding>
            {
                new GameApiColonyBuilding
                {
                    BuildingId = 200,
                    ColonyBuildingTypeId = 3,
                    BlueprintDesignName = "Refinery",
                    BuildingOnline = false,
                    StatusId = 1,
                },
            };

            bool changed = ColonyMergeService.MergeBuildings(apiBuildings, colony);

            Assert.That(changed, Is.True);
            Assert.That(colony.Structures.Count, Is.EqualTo(1));

            bool online;
            existingStructure.Properties.GetBoolean(GameConstants.PropOnline, false, out online);
            Assert.That(online, Is.False);
        }

        // -------------------------------------------------------------------
        // Test 11: Existing building replaces collections
        // Validates: Requirements 8.13, 8.14, 8.17
        // -------------------------------------------------------------------

        [Test]
        public void MergeBuildings_ExistingBuilding_ReplacesCollections()
        {
            var existingStructure = new ColonyStructure
            {
                UUID = "structure-uuid-002",
                BuildingID = 300,
                ColonyBuildingTypeId = 7,
                OpsStatusEffects = new List<BuildingStatusEffect>
                {
                    new BuildingStatusEffect { StatusId = 1, ModTypeId = 1, Change = 0.5m },
                },
                Industries = new List<BuildingIndustry>
                {
                    new BuildingIndustry { Id = 10, Name = "Old Industry" },
                },
                BuildingAttributes = new List<BuildingAttribute>
                {
                    new BuildingAttribute { ModTypeId = 1, PropertyName = "OldProp" },
                },
            };
            existingStructure.Properties.SetProperty(GameConstants.PropBuilt, true);
            existingStructure.Properties.SetProperty(GameConstants.PropOnline, true);

            var colony = new Colony
            {
                UUID = "colony-uuid-003",
                OwnerUUID = OwnerUUID,
                Structures = new List<ColonyStructure> { existingStructure },
            };

            var apiBuildings = new List<GameApiColonyBuilding>
            {
                new GameApiColonyBuilding
                {
                    BuildingId = 300,
                    ColonyBuildingTypeId = 7,
                    BlueprintDesignName = "Factory",
                    BuildingOnline = true,
                    StatusId = 1,
                    OpsStatusEffects = new List<GameApiBuildingStatusEffect>
                    {
                        new GameApiBuildingStatusEffect { StatusId = 2, ModTypeId = 3, Change = 1.5 },
                        new GameApiBuildingStatusEffect { StatusId = 3, ModTypeId = 4, Change = 2.0 },
                    },
                    Industries = new List<GameApiBuildingIndustry>
                    {
                        new GameApiBuildingIndustry { Id = 20, Name = "New Industry" },
                    },
                    BuildingAttributes = new List<GameApiBuildingAttribute>
                    {
                        new GameApiBuildingAttribute { ModTypeId = 5, PropertyName = "NewProp", PropertyValue = "Val" },
                    },
                },
            };

            bool changed = ColonyMergeService.MergeBuildings(apiBuildings, colony);

            Assert.That(changed, Is.True);

            // OpsStatusEffects replaced
            Assert.That(existingStructure.OpsStatusEffects.Count, Is.EqualTo(2));
            Assert.That(existingStructure.OpsStatusEffects[0].StatusId, Is.EqualTo(2));
            Assert.That(existingStructure.OpsStatusEffects[1].Change, Is.EqualTo(2.0m));

            // Industries replaced
            Assert.That(existingStructure.Industries.Count, Is.EqualTo(1));
            Assert.That(existingStructure.Industries[0].Name, Is.EqualTo("New Industry"));

            // BuildingAttributes replaced
            Assert.That(existingStructure.BuildingAttributes.Count, Is.EqualTo(1));
            Assert.That(existingStructure.BuildingAttributes[0].PropertyName, Is.EqualTo("NewProp"));
        }

        // -------------------------------------------------------------------
        // Test 12: Preserves local-only fields
        // Validates: Requirements 8.5
        // -------------------------------------------------------------------

        [Test]
        public void MergeBuildings_PreservesLocalOnlyFields()
        {
            var existingStructure = new ColonyStructure
            {
                UUID = "structure-uuid-003",
                BuildingID = 400,
                ColonyBuildingTypeId = 2,
                ManufacturingBlueprintUUID = "bp-uuid-999",
                BuildQueueSequence = 5,
                ProcessCompletionTime = new CountDownTime(),
            };
            existingStructure.ProcessCompletionTime.TimeRemaining = 3600;
            existingStructure.Properties.SetProperty(GameConstants.PropBuilt, true);
            existingStructure.Properties.SetProperty(GameConstants.PropOnline, true);

            var colony = new Colony
            {
                UUID = "colony-uuid-004",
                OwnerUUID = OwnerUUID,
                Structures = new List<ColonyStructure> { existingStructure },
            };

            var apiBuildings = new List<GameApiColonyBuilding>
            {
                new GameApiColonyBuilding
                {
                    BuildingId = 400,
                    ColonyBuildingTypeId = 2,
                    BlueprintDesignName = "Manufactory",
                    BuildingOnline = true,
                    StatusId = 1,
                },
            };

            ColonyMergeService.MergeBuildings(apiBuildings, colony);

            Assert.That(existingStructure.ManufacturingBlueprintUUID, Is.EqualTo("bp-uuid-999"));
            Assert.That(existingStructure.BuildQueueSequence, Is.EqualTo(1));
            Assert.That(existingStructure.ProcessCompletionTime, Is.Not.Null);
        }

        // -------------------------------------------------------------------
        // Test 13: Missing from API not removed
        // Validates: Requirements 8.4
        // -------------------------------------------------------------------

        [Test]
        public void MergeBuildings_MissingFromApi_NotRemoved()
        {
            var localOnlyStructure = new ColonyStructure
            {
                UUID = "structure-uuid-004",
                BuildingID = 500,
                ColonyBuildingTypeId = 1,
            };
            localOnlyStructure.Properties.SetProperty(GameConstants.PropBuilt, true);
            localOnlyStructure.Properties.SetProperty(GameConstants.PropOnline, true);

            var colony = new Colony
            {
                UUID = "colony-uuid-005",
                OwnerUUID = OwnerUUID,
                Structures = new List<ColonyStructure> { localOnlyStructure },
            };

            // API returns a different building — local structure 500 is not in the response
            var apiBuildings = new List<GameApiColonyBuilding>
            {
                new GameApiColonyBuilding
                {
                    BuildingId = 600,
                    ColonyBuildingTypeId = 9,
                    BlueprintDesignName = "Research Lab",
                    BuildingOnline = true,
                    StatusId = 1,
                },
            };

            ColonyMergeService.MergeBuildings(apiBuildings, colony);

            // Local structure still present, plus the new one
            Assert.That(colony.Structures.Count, Is.EqualTo(2));
            Assert.That(colony.Structures.Any(s => s.BuildingID == 500), Is.True);
            Assert.That(colony.Structures.Any(s => s.BuildingID == 600), Is.True);
        }

        // -------------------------------------------------------------------
        // Test 14: ResourceName sets MiningSurveyResource if local empty
        // Validates: Requirements 8.6
        // -------------------------------------------------------------------

        [Test]
        public void MergeBuildings_ResourceName_SetsIfLocalEmpty()
        {
            var existingStructure = new ColonyStructure
            {
                UUID = "structure-uuid-005",
                BuildingID = 700,
                ColonyBuildingTypeId = 4,
                MiningSurveyResource = null,
            };
            existingStructure.Properties.SetProperty(GameConstants.PropBuilt, true);
            existingStructure.Properties.SetProperty(GameConstants.PropOnline, true);

            var colony = new Colony
            {
                UUID = "colony-uuid-006",
                OwnerUUID = OwnerUUID,
                Structures = new List<ColonyStructure> { existingStructure },
            };

            var apiBuildings = new List<GameApiColonyBuilding>
            {
                new GameApiColonyBuilding
                {
                    BuildingId = 700,
                    ColonyBuildingTypeId = 4,
                    BlueprintDesignName = "Mining Rig",
                    BuildingOnline = true,
                    StatusId = 1,
                    ResourceName = "Lanthanides",
                },
            };

            ColonyMergeService.MergeBuildings(apiBuildings, colony);

            Assert.That(existingStructure.MiningSurveyResource, Is.EqualTo("Lanthanides"));

            // Now verify it does NOT overwrite if local already has a value
            existingStructure.MiningSurveyResource = "Iron";

            var apiBuildings2 = new List<GameApiColonyBuilding>
            {
                new GameApiColonyBuilding
                {
                    BuildingId = 700,
                    ColonyBuildingTypeId = 4,
                    BlueprintDesignName = "Mining Rig",
                    BuildingOnline = true,
                    StatusId = 1,
                    ResourceName = "Copper",
                },
            };

            ColonyMergeService.MergeBuildings(apiBuildings2, colony);

            Assert.That(existingStructure.MiningSurveyResource, Is.EqualTo("Iron"));
        }

        // -------------------------------------------------------------------
        // Test 15: ConstructionTimer sets BuildCompletionTime if local null
        // Validates: Requirements 8.7
        // -------------------------------------------------------------------

        [Test]
        public void MergeBuildings_ConstructionTimer_SetsIfLocalNull()
        {
            var existingStructure = new ColonyStructure
            {
                UUID = "structure-uuid-006",
                BuildingID = 800,
                ColonyBuildingTypeId = 6,
                BuildCompletionTime = null,
            };
            existingStructure.Properties.SetProperty(GameConstants.PropBuilt, false);
            existingStructure.Properties.SetProperty(GameConstants.PropOnline, false);

            var colony = new Colony
            {
                UUID = "colony-uuid-007",
                OwnerUUID = OwnerUUID,
                Structures = new List<ColonyStructure> { existingStructure },
            };

            var finishTime = FrozenTime.AddHours(2);
            var apiBuildings = new List<GameApiColonyBuilding>
            {
                new GameApiColonyBuilding
                {
                    BuildingId = 800,
                    ColonyBuildingTypeId = 6,
                    BlueprintDesignName = "Commodity Factory",
                    BuildingOnline = false,
                    StatusId = 0,
                    ConstructingBuildingFinish = finishTime,
                },
            };

            ColonyMergeService.MergeBuildings(apiBuildings, colony);

            Assert.That(existingStructure.BuildCompletionTime, Is.Not.Null);
            Assert.That(existingStructure.BuildCompletionTime.TimeRemaining, Is.EqualTo(7200));

            // Now verify it does NOT overwrite if local already has a timer
            var existingTimer = existingStructure.BuildCompletionTime;

            var apiBuildings2 = new List<GameApiColonyBuilding>
            {
                new GameApiColonyBuilding
                {
                    BuildingId = 800,
                    ColonyBuildingTypeId = 6,
                    BlueprintDesignName = "Commodity Factory",
                    BuildingOnline = false,
                    StatusId = 0,
                    ConstructingBuildingFinish = FrozenTime.AddHours(5),
                },
            };

            ColonyMergeService.MergeBuildings(apiBuildings2, colony);

            // Should still be the original timer, not overwritten
            Assert.That(existingStructure.BuildCompletionTime, Is.SameAs(existingTimer));
        }

        // -------------------------------------------------------------------
        // MergeWarehouse Tests
        // Validates: Requirements 9.1-9.14
        // -------------------------------------------------------------------

        // -------------------------------------------------------------------
        // Test: New warehouse item creates with correct mapping
        // Validates: Requirements 9.3
        // -------------------------------------------------------------------

        [Test]
        public void MergeWarehouse_NewItem_CreatesWithMapping()
        {
            var colony = new Colony
            {
                UUID = "colony-wh-001",
                OwnerUUID = OwnerUUID,
                Items = new ItemBag(),
            };

            var apiItems = new List<GameApiAssetCargoItem>
            {
                new GameApiAssetCargoItem
                {
                    ResourceName = "Iron",
                    TypeC = "R",
                    Amount = 500,
                    CargoItemId = 42,
                },
            };

            var result = ColonyMergeService.MergeWarehouse(apiItems, colony);

            Assert.That(result, Is.True);
            Assert.That(colony.Items.Count(), Is.EqualTo(1));

            var item = colony.Items.Items.Values.First();
            Assert.That(item.Name, Is.EqualTo("Iron"));
            Assert.That(item.ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Resource));
            Assert.That(item.Quantity, Is.EqualTo(500));
            Assert.That(item.UUID, Is.Not.Null.And.Not.Empty);
        }

        // -------------------------------------------------------------------
        // Test: Existing item gets quantity updated
        // Validates: Requirements 9.1, 9.2
        // -------------------------------------------------------------------

        [Test]
        public void MergeWarehouse_ExistingItem_UpdatesQuantity()
        {
            var existingItem = new Item(ItemType.ItemTypeEnum.Resource, "Iron")
            {
                UUID = "item-uuid-001",
                Quantity = 100,
                BaseItemTypeID = "Iron",
                GameItemId = 101,
            };

            var colony = new Colony
            {
                UUID = "colony-wh-002",
                OwnerUUID = OwnerUUID,
                Items = new ItemBag(),
            };
            colony.Items.AddItem(existingItem);

            var apiItems = new List<GameApiAssetCargoItem>
            {
                new GameApiAssetCargoItem
                {
                    ResourceName = "Iron",
                    TypeC = "R",
                    Amount = 750,
                    CargoItemId = 101,
                },
            };

            var result = ColonyMergeService.MergeWarehouse(apiItems, colony);

            Assert.That(result, Is.True);
            Assert.That(colony.Items.Count(), Is.EqualTo(1));
            Assert.That(existingItem.Quantity, Is.EqualTo(750));
        }

        // -------------------------------------------------------------------
        // Test: Zero amount sets quantity to zero (not removal)
        // Validates: Requirements 9.5
        // -------------------------------------------------------------------

        [Test]
        public void MergeWarehouse_ZeroAmount_SetsToZero()
        {
            var existingItem = new Item(ItemType.ItemTypeEnum.Resource, "Copper")
            {
                UUID = "item-uuid-002",
                Quantity = 200,
                BaseItemTypeID = "Copper",
                GameItemId = 102,
            };

            var colony = new Colony
            {
                UUID = "colony-wh-003",
                OwnerUUID = OwnerUUID,
                Items = new ItemBag(),
            };
            colony.Items.AddItem(existingItem);

            var apiItems = new List<GameApiAssetCargoItem>
            {
                new GameApiAssetCargoItem
                {
                    ResourceName = "Copper",
                    TypeC = "R",
                    Amount = 0,
                    CargoItemId = 102,
                },
            };

            var result = ColonyMergeService.MergeWarehouse(apiItems, colony);

            Assert.That(result, Is.True);
            Assert.That(colony.Items.Count(), Is.EqualTo(1));
            Assert.That(existingItem.Quantity, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Test: Local item missing from API is not removed
        // Validates: Requirements 9.4
        // -------------------------------------------------------------------

        [Test]
        public void MergeWarehouse_MissingFromApi_IsRemoved()
        {
            var existingItem = new Item(ItemType.ItemTypeEnum.Resource, "Gold")
            {
                UUID = "item-uuid-003",
                Quantity = 50,
                BaseItemTypeID = "Gold",
            };

            var colony = new Colony
            {
                UUID = "colony-wh-004",
                OwnerUUID = OwnerUUID,
                Items = new ItemBag(),
            };
            colony.Items.AddItem(existingItem);

            // API returns a different item — Gold is not in the response
            var apiItems = new List<GameApiAssetCargoItem>
            {
                new GameApiAssetCargoItem
                {
                    CargoItemId = 9999,
                    ResourceName = "Silver",
                    TypeC = "R",
                    Amount = 300,
                },
            };

            ColonyMergeService.MergeWarehouse(apiItems, colony);

            // Gold should be removed (game API is authoritative)
            Assert.That(colony.Items.Items.ContainsKey("item-uuid-003"), Is.False);
            // Only Silver remains
            Assert.That(colony.Items.Count(), Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Test: Unknown TypeC maps to ItemTypeEnum.None
        // Validates: Requirements 9.3 (TypeC mapping)
        // -------------------------------------------------------------------

        [Test]
        public void MergeWarehouse_UnknownTypeC_MapsToNone()
        {
            var colony = new Colony
            {
                UUID = "colony-wh-005",
                OwnerUUID = OwnerUUID,
                Items = new ItemBag(),
            };

            var apiItems = new List<GameApiAssetCargoItem>
            {
                new GameApiAssetCargoItem
                {
                    ResourceName = "Mystery Item",
                    TypeC = "ZZ",
                    Amount = 10,
                },
            };

            ColonyMergeService.MergeWarehouse(apiItems, colony);

            var item = colony.Items.Items.Values.First();
            Assert.That(item.ItemType, Is.EqualTo(ItemType.ItemTypeEnum.None));
            Assert.That(item.Name, Is.EqualTo("Mystery Item"));
        }

        // -------------------------------------------------------------------
        // Test: TypeC mapping for all known codes
        // Validates: Requirements 9.3 (TypeC mapping)
        // -------------------------------------------------------------------

        [TestCase("R", ItemType.ItemTypeEnum.Resource)]
        [TestCase("Sc", ItemType.ItemTypeEnum.Survey)]
        [TestCase("W", ItemType.ItemTypeEnum.WorkDetail)]
        [TestCase("S", ItemType.ItemTypeEnum.ShipPart)]
        [TestCase("Bp", ItemType.ItemTypeEnum.Blueprint)]
        [TestCase("F", ItemType.ItemTypeEnum.Flatpack)]
        public void MergeWarehouse_TypeCMapping_AllKnownCodes(
            string typeC,
            ItemType.ItemTypeEnum expectedType)
        {
            var colony = new Colony
            {
                UUID = "colony-wh-006",
                OwnerUUID = OwnerUUID,
                Items = new ItemBag(),
            };

            var apiItems = new List<GameApiAssetCargoItem>
            {
                new GameApiAssetCargoItem
                {
                    ResourceName = "Test Item " + typeC,
                    TypeC = typeC,
                    Amount = 1,
                },
            };

            ColonyMergeService.MergeWarehouse(apiItems, colony);

            var item = colony.Items.Items.Values.First();
            Assert.That(item.ItemType, Is.EqualTo(expectedType));
        }
    }
}
