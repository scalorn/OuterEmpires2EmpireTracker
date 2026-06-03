// <copyright file="ColonyMergeBuildingsBugConditionTests.cs" company="OE2EmpireTracker">
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
    /// Bug condition exploration tests for ColonyMergeService.MergeBuildings.
    /// These tests encode the EXPECTED (correct) behavior and are expected to
    /// FAIL on unfixed code, confirming the bug exists.
    /// Spec: colony-import-fix
    /// </summary>
    [TestFixture]
    public class ColonyMergeBuildingsBugConditionTests
    {
        private static readonly DateTime FrozenTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

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
        /// Generates a ColonyBuildingTypeId in a realistic range.
        /// </summary>
        private static Gen<int> BuildingTypeIdGen()
        {
            return Gen.Choose(1, 10);
        }

        /// <summary>
        /// Creates a colony with N pool entries of the specified ColonyBuildingTypeId.
        /// All entries have BuildingID=0 (unassigned) to trigger fallback matching.
        /// </summary>
        private static Colony CreateColonyWithPool(
            int count,
            int colonyBuildingTypeId,
            string flatpackBlueprintUUID = "bp-uuid-001")
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "owner-1",
                PlanetName = "TestPlanet",
                SystemName = "TestSystem",
            };

            for (int i = 0; i < count; i++)
            {
                var s = new ColonyStructure
                {
                    UUID = Guid.NewGuid().ToString(),
                    BuildingID = 0,
                    ColonyBuildingTypeId = colonyBuildingTypeId,
                    FlatpackBlueprintUUID = flatpackBlueprintUUID,
                    DisplaySequence = i + 1,
                    BuildQueueSequence = i + 1,
                };
                s.Properties.SetProperty(GameConstants.PropBuilt, false);
                s.Properties.SetProperty(GameConstants.PropStaged, false);
                colony.Structures.Add(s);
            }

            return colony;
        }

        /// <summary>
        /// Creates a list of API buildings of the specified type with past completion dates.
        /// Each building gets a unique BuildingId and a past ConstructingBuildingFinish.
        /// </summary>
        private static List<GameApiColonyBuilding> CreateApiBuildings(
            int count,
            int colonyBuildingTypeId,
            int startBuildingId = 100)
        {
            var buildings = new List<GameApiColonyBuilding>();
            for (int i = 0; i < count; i++)
            {
                buildings.Add(new GameApiColonyBuilding
                {
                    BuildingId = startBuildingId + i,
                    ColonyBuildingTypeId = colonyBuildingTypeId,
                    BlueprintDesignName = "Structure-Type-" + colonyBuildingTypeId,
                    StatusId = 1,
                    BuildingOnline = true,
                    ConstructingBuildingFinish = FrozenTime.AddHours(-(count - i)),
                    DurabilityCurrent = 100.0,
                    DurabilityMax = 100.0,
                });
            }

            return buildings;
        }

        /// <summary>
        /// Adds flatpack items to the colony warehouse for staged assignment testing.
        /// </summary>
        private static void AddWarehouseFlatpacks(
            Colony colony,
            string flatpackBlueprintUUID,
            int quantity)
        {
            var item = new Item(ItemType.ItemTypeEnum.Flatpack, "Flatpack-" + flatpackBlueprintUUID)
            {
                UUID = Guid.NewGuid().ToString(),
                BaseItemTypeID = flatpackBlueprintUUID,
                Quantity = quantity,
            };
            colony.Items.AddItem(item);
        }

        // ---------------------------------------------------------------
        // Case A: Unique Pool Consumption
        // Colony with 2+ pool entries of same ColonyBuildingTypeId (both BuildingID=0),
        // API returns 2+ buildings of that type.
        // Assert: each pool entry consumed at most once (unique BuildingIDs assigned).
        // Validates: Requirements 1.2, 2.2
        // ---------------------------------------------------------------

        /// <summary>
        /// Property: When multiple unassigned pool entries of the same type exist
        /// and the API returns multiple buildings of that type, each pool entry must
        /// be consumed at most once — resulting in unique BuildingIDs across matched entries.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CaseA_UniquePoolConsumption_EachEntryConsumedAtMostOnce()
        {
            var gen =
                from poolCount in Gen.Choose(2, 5)
                from apiCount in Gen.Choose(2, 5).Select(c => Math.Min(c, poolCount))
                from typeId in BuildingTypeIdGen()
                select new { PoolCount = poolCount, ApiCount = apiCount, TypeId = typeId };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var colony = CreateColonyWithPool(data.PoolCount, data.TypeId);
                var apiBuildings = CreateApiBuildings(data.ApiCount, data.TypeId);

                ColonyMergeService.MergeBuildings(apiBuildings, colony);

                // After merge, structures with BuildingID > 0 must all have unique BuildingIDs
                var assignedIds = colony.Structures
                    .Where(s => s.BuildingID > 0)
                    .Select(s => s.BuildingID)
                    .ToList();

                var uniqueIds = assignedIds.Distinct().ToList();

                return (assignedIds.Count == uniqueIds.Count)
                    .Label($"Expected unique BuildingIDs but got duplicates: [{string.Join(", ", assignedIds)}]");
            });
        }

        // ---------------------------------------------------------------
        // Case B: Staged Assignment From Warehouse
        // Colony with 3+ pool entries, API returns structures where warehouse
        // has flatpacks matching remaining entries.
        // Assert: remaining entries have Staged=true.
        // Validates: Requirements 1.3, 1.4, 2.3, 2.4
        // ---------------------------------------------------------------

        /// <summary>
        /// Property: When the API matches some pool entries and the warehouse contains
        /// flatpacks matching remaining entries' FlatpackBlueprintUUID, those remaining
        /// entries must be marked Staged=true (up to warehouse quantity).
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CaseB_StagedFromWarehouse_RemainingEntriesMarkedStaged()
        {
            var gen =
                from poolCount in Gen.Choose(3, 6)
                from apiCount in Gen.Choose(1, 3).Select(c => Math.Min(c, poolCount - 1))
                from warehouseQty in Gen.Choose(1, 3).Select(q => Math.Min(q, poolCount - apiCount))
                from typeId in BuildingTypeIdGen()
                select new
                {
                    PoolCount = poolCount,
                    ApiCount = apiCount,
                    WarehouseQty = warehouseQty,
                    TypeId = typeId,
                };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                string bpUUID = "bp-uuid-" + data.TypeId;
                var colony = CreateColonyWithPool(data.PoolCount, data.TypeId, bpUUID);
                var apiBuildings = CreateApiBuildings(data.ApiCount, data.TypeId);
                AddWarehouseFlatpacks(colony, bpUUID, data.WarehouseQty);

                ColonyMergeService.MergeBuildings(apiBuildings, colony);

                // Count entries that are staged (remaining entries matched by warehouse)
                int stagedCount = 0;
                foreach (var s in colony.Structures)
                {
                    bool staged;
                    s.Properties.GetBoolean(GameConstants.PropStaged, false, out staged);
                    if (staged)
                    {
                        stagedCount++;
                    }
                }

                // Expected: at least min(warehouseQty, remaining) entries staged
                int expectedRemaining = data.PoolCount - data.ApiCount;
                int expectedStaged = Math.Min(data.WarehouseQty, expectedRemaining);

                return (stagedCount >= expectedStaged)
                    .Label($"Expected at least {expectedStaged} staged entries but got {stagedCount}. " +
                           $"Pool={data.PoolCount}, API={data.ApiCount}, Warehouse={data.WarehouseQty}");
            });
        }

        // ---------------------------------------------------------------
        // Case C: Planned Normalization
        // Colony with pool entries having stale state (Built=true from prior mismatch),
        // API returns fewer buildings.
        // Assert: unmatched entries reset to planned (Built=false, Staged=false,
        // BuildCompletionTime=null).
        // Validates: Requirements 1.5, 2.5
        // ---------------------------------------------------------------

        /// <summary>
        /// Property: When pool entries have stale Built=true state and the API returns
        /// fewer buildings (leaving some entries unmatched and no warehouse flatpacks),
        /// those unmatched entries must be normalized to planned state.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CaseC_PlannedNormalization_UnmatchedEntriesResetToPlanned()
        {
            var gen =
                from poolCount in Gen.Choose(3, 6)
                from apiCount in Gen.Choose(1, 3).Select(c => Math.Min(c, poolCount - 1))
                from typeId in BuildingTypeIdGen()
                select new { PoolCount = poolCount, ApiCount = apiCount, TypeId = typeId };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                string bpUUID = "bp-uuid-" + data.TypeId;
                var colony = CreateColonyWithPool(data.PoolCount, data.TypeId, bpUUID);

                // Corrupt the pool entries with stale state (simulate prior incorrect merge)
                foreach (var s in colony.Structures)
                {
                    s.Properties.SetProperty(GameConstants.PropBuilt, true);
                    s.BuildCompletionTime = new CountDownTime();
                    s.BuildCompletionTime.TimeRemaining = 3600;
                }

                // API returns fewer buildings — some entries will be unmatched
                var apiBuildings = CreateApiBuildings(data.ApiCount, data.TypeId);

                // No warehouse flatpacks — unmatched entries should become planned
                ColonyMergeService.MergeBuildings(apiBuildings, colony);

                // Check unmatched entries (those still with BuildingID=0 after merge)
                var unmatched = colony.Structures
                    .Where(s => s.BuildingID == 0)
                    .ToList();

                // Each unmatched entry must be in planned state
                bool allPlanned = true;
                var violations = new List<string>();
                foreach (var s in unmatched)
                {
                    bool built;
                    s.Properties.GetBoolean(GameConstants.PropBuilt, false, out built);
                    bool staged;
                    s.Properties.GetBoolean(GameConstants.PropStaged, false, out staged);

                    if (built || staged || s.BuildCompletionTime != null)
                    {
                        allPlanned = false;
                        violations.Add(
                            $"UUID={s.UUID}: Built={built}, Staged={staged}, " +
                            $"BuildCompletionTime={s.BuildCompletionTime?.TimeRemaining}");
                    }
                }

                return allPlanned
                    .Label($"Unmatched entries not in planned state. " +
                           $"Violations: [{string.Join("; ", violations)}]");
            });
        }
    }
}
