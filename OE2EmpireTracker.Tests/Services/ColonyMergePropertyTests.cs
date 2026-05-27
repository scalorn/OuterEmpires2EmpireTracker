// <copyright file="ColonyMergePropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for ColonyMergeService.
    /// Feature: colony-api-sync
    /// </summary>
    [TestFixture]
    public class ColonyMergePropertyTests
    {
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

        // ---------------------------------------------------------------
        // Generators
        // ---------------------------------------------------------------

        private static Gen<string> NonEmptyNameGen()
        {
            return from prefix in Gen.Elements("Alpha", "Beta", "Gamma", "Delta", "Epsilon")
                   from suffix in Gen.Choose(1, 9999)
                   select prefix + suffix;
        }

        private static Gen<GameApiColonyListItem> ColonyListItemGen()
        {
            return from colonyId in Gen.Choose(1, 100000)
                   from colonyName in NonEmptyNameGen()
                   from planetName in NonEmptyNameGen()
                   from systemName in NonEmptyNameGen()
                   from systemId in Gen.Choose(1, 500)
                   from colonySize in Gen.Choose(1, 10)
                   from distance in Gen.Choose(1, 1000)
                   from surfVar in Gen.Choose(0, 5)
                   from atmosVar in Gen.Choose(0, 5)
                   select new GameApiColonyListItem
                   {
                       ColonyId = colonyId,
                       ColonyName = colonyName,
                       SystemObjectName = planetName,
                       SystemName = systemName,
                       SystemId = systemId,
                       ColonySize = colonySize,
                       Distance = distance,
                       SurfaceVariation = surfVar,
                       AtmosVariation = atmosVar,
                       HexValue = "AABB00",
                       SystemObjectTypeName = "Planet",
                       ImagePreFix = "planet_img",
                   };
        }

        private static Gen<List<GameApiColonyListItem>> ColonyListGen()
        {
            return from count in Gen.Choose(1, 5)
                   from items in Gen.ListOf(count, ColonyListItemGen())
                   select items.ToList();
        }

        // ---------------------------------------------------------------
        // Property 1: Dedup Idempotency
        // Validates: Requirements 6.1, 6.2, 6.3
        // merge(merge(state, data), data) == merge(state, data)
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 1: Dedup Idempotency.
        /// Running MergeColonyList twice with the same data produces no
        /// additional colonies on the second run.
        /// Validates: Requirements 6.1, 6.2, 6.3
        /// </summary>
        [Test]
        public void MergeColonyList_Idempotent_NoDuplicatesOnRepeatedSync()
        {
            Prop.ForAll(ColonyListGen().ToArbitrary(), (apiColonies) =>
            {
                var ownerUUID = "owner-prop-001";
                var localColonies = new List<Colony>();

                // First merge
                ColonyMergeService.MergeColonyList(apiColonies, localColonies, ownerUUID);
                int countAfterFirst = localColonies.Count;

                // Second merge with same data
                ColonyMergeService.MergeColonyList(apiColonies, localColonies, ownerUUID);
                int countAfterSecond = localColonies.Count;

                return (countAfterSecond == countAfterFirst).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 2: Local-Only Field Preservation
        // Validates: Requirements 8.5
        // For any structure existing before and after merge,
        // local-only fields are unchanged.
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 2: Local-Only Field Preservation.
        /// MergeBuildings never overwrites ManufacturingBlueprintUUID,
        /// BuildQueueSequence, or ManufacturingQuantity on existing structures.
        /// Validates: Requirements 8.5
        /// </summary>
        [Test]
        public void MergeBuildings_PreservesLocalOnlyFields_ForAllInputs()
        {
            var localFieldGen =
                from bpUuid in NonEmptyNameGen()
                from queueSeq in Gen.Choose(1, 100)
                from mfgQty in Gen.Choose(1, 500)
                from buildingId in Gen.Choose(1, 10000)
                from typeId in Gen.Choose(1, 20)
                select new { BpUuid = bpUuid, QueueSeq = queueSeq, MfgQty = mfgQty, BuildingId = buildingId, TypeId = typeId };

            Prop.ForAll(localFieldGen.ToArbitrary(), (data) =>
            {
                var existingStructure = new ColonyStructure
                {
                    UUID = Guid.NewGuid().ToString(),
                    BuildingID = data.BuildingId,
                    ColonyBuildingTypeId = data.TypeId,
                    ManufacturingBlueprintUUID = data.BpUuid,
                    BuildQueueSequence = data.QueueSeq,
                    ManufacturingQuantity = data.MfgQty,
                };
                existingStructure.Properties.SetProperty(GameConstants.PropBuilt, true);
                existingStructure.Properties.SetProperty(GameConstants.PropOnline, true);

                var colony = new Colony
                {
                    UUID = "colony-prop-001",
                    OwnerUUID = "owner-prop-001",
                    Structures = new List<ColonyStructure> { existingStructure },
                };

                var apiBuildings = new List<GameApiColonyBuilding>
                {
                    new GameApiColonyBuilding
                    {
                        BuildingId = data.BuildingId,
                        ColonyBuildingTypeId = data.TypeId,
                        BlueprintDesignName = "SomeBuilding",
                        BuildingOnline = true,
                        StatusId = 1,
                    },
                };

                ColonyMergeService.MergeBuildings(apiBuildings, colony);

                return (existingStructure.ManufacturingBlueprintUUID == data.BpUuid
                    && existingStructure.BuildQueueSequence == data.QueueSeq
                    && existingStructure.ManufacturingQuantity == data.MfgQty).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 3: No Data Loss on Empty Response
        // Validates: Requirements 8.4, 9.4, 13.5
        // Empty API lists never reduce local collection sizes.
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 3: No Data Loss on Empty Response.
        /// Merging empty API lists never reduces local colony, structure,
        /// or item counts.
        /// Validates: Requirements 8.4, 9.4, 13.5
        /// </summary>
        [Test]
        public void MergeWithEmptyLists_NeverReducesLocalCounts()
        {
            var scenarioGen =
                from colonyCount in Gen.Choose(1, 5)
                from structureCount in Gen.Choose(1, 5)
                from itemCount in Gen.Choose(1, 5)
                select new { ColonyCount = colonyCount, StructureCount = structureCount, ItemCount = itemCount };

            Prop.ForAll(scenarioGen.ToArbitrary(), (scenario) =>
            {
                var ownerUUID = "owner-prop-003";

                // Build local colonies with structures and items
                var localColonies = new List<Colony>();
                for (int i = 0; i < scenario.ColonyCount; i++)
                {
                    var colony = new Colony
                    {
                        UUID = Guid.NewGuid().ToString(),
                        OwnerUUID = ownerUUID,
                        PlanetName = "Planet" + i,
                        SystemName = "System" + i,
                        ColonyName = "Colony" + i,
                        Structures = new List<ColonyStructure>(),
                        Items = new ItemBag(),
                    };

                    for (int s = 0; s < scenario.StructureCount; s++)
                    {
                        var structure = new ColonyStructure
                        {
                            UUID = Guid.NewGuid().ToString(),
                            BuildingID = (i * 100) + s,
                            ColonyBuildingTypeId = 1,
                        };
                        structure.Properties.SetProperty(GameConstants.PropBuilt, true);
                        colony.Structures.Add(structure);
                    }

                    for (int t = 0; t < scenario.ItemCount; t++)
                    {
                        var item = new Item
                        {
                            UUID = Guid.NewGuid().ToString(),
                            Name = "Item" + t + "_" + i,
                            Quantity = 10,
                        };
                        colony.Items.AddItem(item);
                    }

                    localColonies.Add(colony);
                }

                int colonyCountBefore = localColonies.Count;
                int structureCountBefore = localColonies.Sum(c => c.Structures.Count);
                int itemCountBefore = localColonies.Sum(c => c.Items.Count());

                // Merge with empty colony list
                ColonyMergeService.MergeColonyList(
                    new List<GameApiColonyListItem>(), localColonies, ownerUUID);

                // Merge with empty buildings for each colony
                foreach (var colony in localColonies)
                {
                    ColonyMergeService.MergeBuildings(
                        new List<GameApiColonyBuilding>(), colony);
                }

                // Merge with empty warehouse for each colony
                foreach (var colony in localColonies)
                {
                    ColonyMergeService.MergeWarehouse(
                        new List<GameApiWarehouseItem>(), colony);
                }

                int colonyCountAfter = localColonies.Count;
                int structureCountAfter = localColonies.Sum(c => c.Structures.Count);
                int itemCountAfter = localColonies.Sum(c => c.Items.Count());

                return (colonyCountAfter >= colonyCountBefore
                    && structureCountAfter >= structureCountBefore
                    && itemCountAfter >= itemCountBefore).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        // ---------------------------------------------------------------
        // Property 4: Owner Isolation
        // Validates: Requirements 6.4
        // Syncing player B never modifies player A's colonies.
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 4: Owner Isolation.
        /// Syncing one owner's data never modifies another owner's colonies.
        /// Validates: Requirements 6.4
        /// </summary>
        [Test]
        public void MergeColonyList_OwnerIsolation_NeverModifiesOtherOwner()
        {
            var scenarioGen =
                from ownerACount in Gen.Choose(1, 4)
                from ownerBCount in Gen.Choose(1, 4)
                select new { OwnerACount = ownerACount, OwnerBCount = ownerBCount };

            Prop.ForAll(scenarioGen.ToArbitrary(), ColonyListGen().ToArbitrary(), (scenario, apiColonies) =>
            {
                var ownerA = "owner-A-uuid";
                var ownerB = "owner-B-uuid";

                // Create owner A's local colonies
                var localColonies = new List<Colony>();
                var ownerAColonyNames = new List<string>();
                for (int i = 0; i < scenario.OwnerACount; i++)
                {
                    var name = "OwnerA_Colony_" + i;
                    ownerAColonyNames.Add(name);
                    localColonies.Add(new Colony
                    {
                        UUID = Guid.NewGuid().ToString(),
                        OwnerUUID = ownerA,
                        PlanetName = "PlanetA" + i,
                        SystemName = "SystemA" + i,
                        ColonyName = name,
                        SystemId = i + 1,
                        ColonySize = 3,
                    });
                }

                // Create owner B's local colonies
                for (int i = 0; i < scenario.OwnerBCount; i++)
                {
                    localColonies.Add(new Colony
                    {
                        UUID = Guid.NewGuid().ToString(),
                        OwnerUUID = ownerB,
                        PlanetName = "PlanetB" + i,
                        SystemName = "SystemB" + i,
                        ColonyName = "OwnerB_Colony_" + i,
                        SystemId = i + 100,
                        ColonySize = 2,
                    });
                }

                // Snapshot owner A's colonies before sync
                var ownerABefore = localColonies
                    .Where(c => c.OwnerUUID == ownerA)
                    .Select(c => new { c.UUID, c.ColonyName, c.PlanetName, c.SystemName, c.SystemId, c.ColonySize })
                    .ToList();

                // Sync owner B's data
                ColonyMergeService.MergeColonyList(apiColonies, localColonies, ownerB);

                // Verify owner A's colonies are unchanged
                var ownerAAfter = localColonies
                    .Where(c => c.OwnerUUID == ownerA)
                    .Select(c => new { c.UUID, c.ColonyName, c.PlanetName, c.SystemName, c.SystemId, c.ColonySize })
                    .ToList();

                if (ownerABefore.Count != ownerAAfter.Count) return false.ToProperty();

                for (int i = 0; i < ownerABefore.Count; i++)
                {
                    var before = ownerABefore[i];
                    var after = ownerAAfter[i];
                    if (before.UUID != after.UUID
                        || before.ColonyName != after.ColonyName
                        || before.PlanetName != after.PlanetName
                        || before.SystemName != after.SystemName
                        || before.SystemId != after.SystemId
                        || before.ColonySize != after.ColonySize)
                    {
                        return false.ToProperty();
                    }
                }

                return true.ToProperty();
            }).QuickCheckThrowOnFailure();
        }
    }
}
