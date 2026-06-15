// <copyright file="ColonyMergeBuildingsFixCheckTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Common.Client.Generated;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Fix-checking property tests for ColonyMergeService.MergeBuildings.
    /// These tests validate the FIXED three-phase algorithm on random colony
    /// configurations, asserting post-merge invariants hold for all inputs.
    /// Spec: colony-import-fix
    /// </summary>
    [TestFixture]
    public class ColonyMergeBuildingsFixCheckTests
    {
        private static readonly DateTime FrozenTime =
            new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

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

        /// <summary>
        /// Generates a ColonyBuildingTypeId in a realistic range (1-5 to create type collisions).
        /// </summary>
        private static Gen<int> BuildingTypeIdGen()
        {
            return Gen.Choose(1, 5);
        }

        /// <summary>
        /// Generates a random colony configuration with 1-10 structures of varying types,
        /// 0-5 API buildings, and 0-3 warehouse flatpacks.
        /// Returns a tuple of (colony, apiBuildings).
        /// </summary>
        private static Gen<ColonyTestInput> ColonyConfigGen()
        {
            var gen =
                from structureCount in Gen.Choose(1, 10)
                from apiCount in Gen.Choose(0, 5)
                from warehouseCount in Gen.Choose(0, 3)
                from typeIds in Gen.ArrayOf(
                    structureCount,
                    Gen.Choose(1, 5))
                from apiBuildingTypeIndices in Gen.ArrayOf(
                    apiCount,
                    Gen.Choose(0, Math.Max(0, structureCount - 1)))
                from warehouseTypeIndices in Gen.ArrayOf(
                    warehouseCount,
                    Gen.Choose(0, Math.Max(0, structureCount - 1)))
                from warehouseQuantities in Gen.ArrayOf(
                    warehouseCount,
                    Gen.Choose(1, 3))
                from hasFutureBuilding in Gen.Elements(true, false)
                select BuildTestInput(
                    structureCount,
                    apiCount,
                    typeIds,
                    apiBuildingTypeIndices,
                    warehouseTypeIndices,
                    warehouseQuantities,
                    hasFutureBuilding);

            return gen;
        }

        /// <summary>
        /// Builds a test input from generated random values.
        /// </summary>
        private static ColonyTestInput BuildTestInput(
            int structureCount,
            int apiCount,
            int[] typeIds,
            int[] apiBuildingTypeIndices,
            int[] warehouseTypeIndices,
            int[] warehouseQuantities,
            bool hasFutureBuilding)
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "owner-1",
                PlanetName = "TestPlanet",
                SystemName = "TestSystem",
            };

            // Create pool structures with varying types
            var blueprintUUIDs = new Dictionary<int, string>();
            for (int i = 0; i < structureCount; i++)
            {
                int typeId = typeIds[i];
                if (!blueprintUUIDs.ContainsKey(typeId))
                {
                    blueprintUUIDs[typeId] = "bp-uuid-type-" + typeId;
                }

                var s = new ColonyStructure
                {
                    UUID = Guid.NewGuid().ToString(),
                    BuildingID = 0,
                    ColonyBuildingTypeId = typeId,
                    FlatpackBlueprintUUID = blueprintUUIDs[typeId],
                    DisplaySequence = i + 1,
                    BuildQueueSequence = i + 1,
                };
                s.Properties.SetProperty(GameConstants.PropBuilt, false);
                s.Properties.SetProperty(GameConstants.PropStaged, false);
                colony.Structures.Add(s);
            }

            // Create API buildings matching pool types by index
            var apiBuildingsList = new List<ColonyBuilding>();
            int effectiveApiCount = Math.Min(apiCount, structureCount);
            for (int i = 0; i < effectiveApiCount; i++)
            {
                int structIdx = apiBuildingTypeIndices[i] % structureCount;
                int typeId = typeIds[structIdx];

                bool isFuture = hasFutureBuilding && i == effectiveApiCount - 1;
                DateTime completionDate = isFuture
                    ? FrozenTime.AddHours(2)
                    : FrozenTime.AddHours(-(effectiveApiCount - i));

                apiBuildingsList.Add(new ColonyBuilding
                {
                    BuildingId = 100 + i,
                    ColonyBuildingTypeId = typeId,
                    BlueprintDesignName = "Structure-Type-" + typeId,
                    StatusId = 1,
                    BuildingOnline = !isFuture,
                    ConstructingBuildingFinish = new DateTimeOffset(completionDate, TimeSpan.Zero),
                    DurabilityCurrent = 100.0,
                    DurabilityMax = 100.0,
                });
            }

            // Add warehouse flatpacks
            for (int i = 0; i < warehouseTypeIndices.Length; i++)
            {
                int structIdx = warehouseTypeIndices[i] % structureCount;
                int typeId = typeIds[structIdx];
                string bpUUID = blueprintUUIDs[typeId];
                int qty = warehouseQuantities[i];

                // Avoid adding duplicate flatpacks for same blueprint
                bool alreadyExists = colony.Items.Items.Values
                    .Any(it => string.Equals(
                        it.BaseItemTypeID, bpUUID, StringComparison.Ordinal));
                if (!alreadyExists)
                {
                    var item = new Item(
                        ItemType.ItemTypeEnum.Flatpack,
                        "Flatpack-" + bpUUID)
                    {
                        UUID = Guid.NewGuid().ToString(),
                        BaseItemTypeID = bpUUID,
                        Quantity = qty,
                    };
                    colony.Items.AddItem(item);
                }
            }

            return new ColonyTestInput
            {
                Colony = colony,
                ApiBuildings = new ColonyBuildings { Buildings = apiBuildingsList },
            };
        }

        // ---------------------------------------------------------------
        // Property 1: At-Most-One Building Invariant
        // At most 1 structure has Built=false AND BuildCompletionTime != null.
        // Validates: Requirements 2.1
        // ---------------------------------------------------------------

        /// <summary>
        /// Property: After MergeBuildings, at most 1 structure in the colony
        /// has Built=false AND BuildCompletionTime != null (the "building" state).
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property AtMostOneBuilding()
        {
            return Prop.ForAll(ColonyConfigGen().ToArbitrary(), input =>
            {
                ColonyMergeService.MergeBuildings(input.ApiBuildings, input.Colony);

                int buildingCount = 0;
                foreach (var s in input.Colony.Structures)
                {
                    bool built;
                    s.Properties.GetBoolean(GameConstants.PropBuilt, false, out built);
                    if (!built && s.BuildCompletionTime != null)
                    {
                        buildingCount++;
                    }
                }

                return (buildingCount <= 1)
                    .Label($"Expected at most 1 'building' structure but found {buildingCount}");
            });
        }

        // ---------------------------------------------------------------
        // Property 2: Unique Consumption — No BuildingID Collision
        // All matched structures have unique BuildingIDs (no consumption collision).
        // Validates: Requirements 2.2
        // ---------------------------------------------------------------

        /// <summary>
        /// Property: After MergeBuildings, all structures with BuildingID > 0
        /// have unique BuildingIDs — no two pool entries consumed the same API building.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property UniqueConsumption()
        {
            return Prop.ForAll(ColonyConfigGen().ToArbitrary(), input =>
            {
                ColonyMergeService.MergeBuildings(input.ApiBuildings, input.Colony);

                var assignedIds = input.Colony.Structures
                    .Where(s => s.BuildingID > 0)
                    .Select(s => s.BuildingID)
                    .ToList();

                var uniqueIds = assignedIds.Distinct().ToList();

                return (assignedIds.Count == uniqueIds.Count)
                    .Label($"BuildingID collision: [{string.Join(", ", assignedIds)}]");
            });
        }

        // ---------------------------------------------------------------
        // Property 3: Staged Count Limit
        // Staged count per type <= warehouse flatpack quantity for that type.
        // Validates: Requirements 2.3, 2.4
        // ---------------------------------------------------------------

        /// <summary>
        /// Property: After MergeBuildings, for each blueprint type, the count of
        /// staged structures does not exceed the warehouse flatpack quantity.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property StagedCountLimit()
        {
            return Prop.ForAll(ColonyConfigGen().ToArbitrary(), input =>
            {
                ColonyMergeService.MergeBuildings(input.ApiBuildings, input.Colony);

                // Build warehouse quantity lookup by BaseItemTypeID
                var warehouseQtyByType = new Dictionary<string, int>(
                    StringComparer.Ordinal);
                foreach (var item in input.Colony.Items.Items.Values)
                {
                    if (item.ItemType == ItemType.ItemTypeEnum.Flatpack
                        && item.Quantity > 0
                        && !string.IsNullOrEmpty(item.BaseItemTypeID))
                    {
                        warehouseQtyByType[item.BaseItemTypeID] = item.Quantity;
                    }
                }

                // Count staged structures by FlatpackBlueprintUUID
                var stagedByType = new Dictionary<string, int>(
                    StringComparer.Ordinal);
                foreach (var s in input.Colony.Structures)
                {
                    bool staged;
                    s.Properties.GetBoolean(
                        GameConstants.PropStaged, false, out staged);
                    if (staged && !string.IsNullOrEmpty(s.FlatpackBlueprintUUID))
                    {
                        if (!stagedByType.ContainsKey(s.FlatpackBlueprintUUID))
                        {
                            stagedByType[s.FlatpackBlueprintUUID] = 0;
                        }

                        stagedByType[s.FlatpackBlueprintUUID]++;
                    }
                }

                // Assert: staged count <= warehouse quantity for each type
                var violations = new List<string>();
                foreach (var kvp in stagedByType)
                {
                    int warehouseQty = 0;
                    warehouseQtyByType.TryGetValue(kvp.Key, out warehouseQty);
                    if (kvp.Value > warehouseQty)
                    {
                        violations.Add(
                            $"type={kvp.Key}: staged={kvp.Value} > warehouse={warehouseQty}");
                    }
                }

                return (violations.Count == 0)
                    .Label($"Staged exceeds warehouse: [{string.Join("; ", violations)}]");
            });
        }

        // ---------------------------------------------------------------
        // Property 4: Planned Normalization
        // All unmatched entries without warehouse match have
        // Built=false, Staged=false, BuildCompletionTime=null.
        // Validates: Requirements 2.5
        // ---------------------------------------------------------------

        /// <summary>
        /// Property: After MergeBuildings, all structures with BuildingID=0
        /// (never matched) that are not staged must be in planned state:
        /// Built=false, Staged=false, BuildCompletionTime=null.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property PlannedNormalization()
        {
            return Prop.ForAll(ColonyConfigGen().ToArbitrary(), input =>
            {
                ColonyMergeService.MergeBuildings(input.ApiBuildings, input.Colony);

                var violations = new List<string>();
                foreach (var s in input.Colony.Structures)
                {
                    // Only check unmatched entries (BuildingID still 0)
                    if (s.BuildingID != 0)
                    {
                        continue;
                    }

                    bool staged;
                    s.Properties.GetBoolean(
                        GameConstants.PropStaged, false, out staged);
                    if (staged)
                    {
                        // Staged entries are OK — they were matched by warehouse
                        continue;
                    }

                    // This is a planned entry — verify normalization
                    bool built;
                    s.Properties.GetBoolean(
                        GameConstants.PropBuilt, false, out built);

                    if (built || s.BuildCompletionTime != null)
                    {
                        violations.Add(
                            $"UUID={s.UUID}: Built={built}, " +
                            $"BCT={s.BuildCompletionTime?.TimeRemaining}");
                    }
                }

                return (violations.Count == 0)
                    .Label($"Planned entries not normalized: " +
                           $"[{string.Join("; ", violations)}]");
            });
        }

        // ---------------------------------------------------------------
        // Property 5: BuildQueueSequence Contiguous 1..N Assignment
        // Built/building first, then staged, then planned.
        // Validates: Requirements 2.7
        // ---------------------------------------------------------------

        /// <summary>
        /// Property: After MergeBuildings, structures that participated in Phase 3
        /// (consumed pool entries + remaining pool entries) have contiguous
        /// BuildQueueSequence values starting at 1, with built/building before
        /// staged before planned. Newly created structures (not from pool) are
        /// excluded since they retain their assigned BQS from Phase 1.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property BuildQueueSequenceContiguous()
        {
            return Prop.ForAll(ColonyConfigGen().ToArbitrary(), input =>
            {
                // Track original pool UUIDs before merge (these participate in Phase 3)
                var originalPoolUUIDs = new HashSet<string>(
                    input.Colony.Structures.Select(s => s.UUID),
                    StringComparer.Ordinal);

                ColonyMergeService.MergeBuildings(input.ApiBuildings, input.Colony);

                // Only check structures that were in the original pool
                // (Phase 3 reassigns BQS for consumed + remaining pool entries)
                var phase3Structures = input.Colony.Structures
                    .Where(s => originalPoolUUIDs.Contains(s.UUID))
                    .OrderBy(s => s.BuildQueueSequence)
                    .ToList();

                if (phase3Structures.Count == 0)
                {
                    return true.Label("No pool structures to check (trivial)");
                }

                // Classify Phase 3 structures
                var builtOrBuilding = new List<ColonyStructure>();
                var stagedList = new List<ColonyStructure>();
                var plannedList = new List<ColonyStructure>();

                foreach (var s in phase3Structures)
                {
                    bool built;
                    s.Properties.GetBoolean(
                        GameConstants.PropBuilt, false, out built);
                    bool staged;
                    s.Properties.GetBoolean(
                        GameConstants.PropStaged, false, out staged);

                    bool isBuilding = !built && s.BuildCompletionTime != null;

                    if (built || isBuilding)
                    {
                        builtOrBuilding.Add(s);
                    }
                    else if (staged)
                    {
                        stagedList.Add(s);
                    }
                    else
                    {
                        plannedList.Add(s);
                    }
                }

                // Check contiguity: Phase 3 structures should have 1, 2, 3...N
                bool contiguous = true;
                for (int i = 0; i < phase3Structures.Count; i++)
                {
                    if (phase3Structures[i].BuildQueueSequence != i + 1)
                    {
                        contiguous = false;
                        break;
                    }
                }

                // Check ordering: built/building before staged before planned
                bool orderCorrect = true;
                int maxBuiltSeq = builtOrBuilding.Count > 0
                    ? builtOrBuilding.Max(s => s.BuildQueueSequence)
                    : 0;
                int minStagedSeq = stagedList.Count > 0
                    ? stagedList.Min(s => s.BuildQueueSequence)
                    : int.MaxValue;
                int maxStagedSeq = stagedList.Count > 0
                    ? stagedList.Max(s => s.BuildQueueSequence)
                    : 0;
                int minPlannedSeq = plannedList.Count > 0
                    ? plannedList.Min(s => s.BuildQueueSequence)
                    : int.MaxValue;

                if (builtOrBuilding.Count > 0 && stagedList.Count > 0)
                {
                    orderCorrect &= maxBuiltSeq < minStagedSeq;
                }

                if (stagedList.Count > 0 && plannedList.Count > 0)
                {
                    orderCorrect &= maxStagedSeq < minPlannedSeq;
                }

                if (builtOrBuilding.Count > 0 && plannedList.Count > 0
                    && stagedList.Count == 0)
                {
                    orderCorrect &= maxBuiltSeq < minPlannedSeq;
                }

                return (contiguous && orderCorrect)
                    .Label($"Contiguous={contiguous}, OrderCorrect={orderCorrect}. " +
                           $"Seqs=[{string.Join(",", phase3Structures.Select(s => s.BuildQueueSequence))}] " +
                           $"Built/Building={builtOrBuilding.Count} Staged={stagedList.Count} " +
                           $"Planned={plannedList.Count}");
            });
        }

        // ---------------------------------------------------------------
        // Helper Types
        // ---------------------------------------------------------------

        /// <summary>
        /// Encapsulates generated test input for property tests.
        /// </summary>
        private class ColonyTestInput
        {
            public Colony Colony { get; set; }

            public ColonyBuildings ApiBuildings { get; set; }
        }
    }
}
