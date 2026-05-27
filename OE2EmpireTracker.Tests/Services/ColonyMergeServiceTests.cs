// <copyright file="ColonyMergeServiceTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Client;
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
    }
}
