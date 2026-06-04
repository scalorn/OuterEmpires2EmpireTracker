// <copyright file="ColonyMergeService.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Provides merge logic for synchronizing API colony data with local colony data.
    /// Implements dedup matching by PlanetName + SystemName (case-insensitive, same owner)
    /// and field-level merge with "API wins" strategy.
    /// </summary>
    public static class ColonyMergeService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Merges a list of API colonies into the local colony list.
        /// Deduplicates by PlanetName + SystemName (case-insensitive) within the same owner.
        /// Skips entries with null/empty SystemObjectName. Per-colony errors are caught and logged.
        /// </summary>
        /// <param name="apiColonies">The colonies returned by the game API.</param>
        /// <param name="localColonies">The local colony list to merge into (may be modified).</param>
        /// <param name="ownerUUID">The UUID of the player who owns these colonies.</param>
        /// <returns>A <see cref="ColonyMergeResult"/> summarizing the merge outcome.</returns>
        public static ColonyMergeResult MergeColonyList(
            List<GameApiColonyListItem> apiColonies,
            List<Colony> localColonies,
            string ownerUUID)
        {
            var result = new ColonyMergeResult();

            if (apiColonies == null || localColonies == null)
            {
                Log.Warn("MergeColonyList: apiColonies or localColonies is null, returning empty result");
                return result;
            }

            foreach (var apiColony in apiColonies)
            {
                if (string.IsNullOrEmpty(apiColony.SystemObjectName))
                {
                    Log.Warn(
                        "MergeColonyList: skipping colony with ColonyId={0} — SystemObjectName is null/empty",
                        apiColony.ColonyId);
                    result.Skipped++;
                    continue;
                }

                try
                {
                    ProcessSingleColony(apiColony, localColonies, ownerUUID, result);
                }
                catch (Exception ex)
                {
                    Log.Error(
                        ex,
                        "MergeColonyList: error processing colony ColonyId={0} Name='{1}', skipping",
                        apiColony.ColonyId,
                        apiColony.ColonyName);
                    result.Skipped++;
                }
            }

            Log.Info(
                "MergeColonyList: completed — Created={0} Updated={1} Skipped={2}",
                result.Created,
                result.Updated,
                result.Skipped);

            return result;
        }

        /// <summary>
        /// Merges a list of API buildings into a colony's structure list.
        /// Implements a three-phase algorithm:
        ///   Phase 1: Ordered matching with pool consumption tracking.
        ///   Phase 2: Warehouse-based staged assignment (task 3.2).
        ///   Phase 3: BuildQueueSequence reassignment (task 3.3).
        /// Never removes local structures absent from the API response.
        /// Preserves local-only fields on existing structures.
        /// </summary>
        /// <param name="apiBuildings">The buildings returned by the game API.</param>
        /// <param name="colony">The local colony whose structures are being merged.</param>
        /// <returns>True if any structures were created or updated; otherwise false.</returns>
        public static bool MergeBuildings(
            List<GameApiColonyBuilding> apiBuildings,
            Colony colony)
        {
            if (apiBuildings == null || colony == null)
            {
                Log.Warn("MergeBuildings: apiBuildings or colony is null, returning false");
                return false;
            }

            bool hasChanges = false;

            // Sort API buildings by constructingBuildingFinish ascending.
            // Buildings constructed first have earlier dates; the one currently being built has the latest.
            var sortedApiBuildings = apiBuildings
                .OrderBy(b => b.ConstructingBuildingFinish ?? DateTime.MaxValue)
                .ToList();

            // Phase 1: Ordered matching with pool consumption tracking.
            // Walk API buildings in completion-date order, consuming pool entries one-by-one.
            var consumed = new HashSet<string>(StringComparer.Ordinal);

            int sequenceCounter = 0;
            foreach (var apiBuilding in sortedApiBuildings)
            {
                sequenceCounter++;
                var match = FindLocalStructure(apiBuilding, colony, consumed);

                if (match != null)
                {
                    // Mark this pool entry as consumed so it cannot be matched again
                    consumed.Add(match.UUID);

                    // Update DisplaySequence to match build order if not already set
                    if (match.DisplaySequence == 0)
                    {
                        match.DisplaySequence = sequenceCounter;
                        hasChanges = true;
                    }

                    bool updated = MergeExistingStructure(match, apiBuilding);
                    hasChanges |= updated;
                }
                else
                {
                    var newStructure = CreateStructureFromApi(apiBuilding);
                    newStructure.DisplaySequence = sequenceCounter;

                    // Assign BuildQueueSequence at end of list so it displays last
                    int maxBuildQueueSeq = colony.Structures.Count > 0
                        ? colony.Structures.Max(s => s.BuildQueueSequence)
                        : 0;
                    newStructure.BuildQueueSequence = maxBuildQueueSeq + 1;

                    colony.Structures.Add(newStructure);
                    hasChanges = true;

                    Log.Info(
                        "MergeBuildings: created new structure UUID={0} BuildingID={1} Name='{2}' seq={3} bqSeq={4} for colony {5}",
                        newStructure.UUID,
                        newStructure.BuildingID,
                        apiBuilding.BlueprintDesignName,
                        newStructure.DisplaySequence,
                        newStructure.BuildQueueSequence,
                        colony.UUID);
                }
            }

            // Phase 2: Warehouse-based staged assignment (task 3.2)
            // Only consider genuine pool entries (BuildingID == 0, never synced) as remaining.
            // Structures with BuildingID > 0 that weren't consumed are previously-synced structures
            // not present in this API response — they retain their existing state.
            var remaining = colony.Structures
                .Where(s => !consumed.Contains(s.UUID) && s.BuildingID == 0)
                .ToList();

            if (remaining.Count > 0)
            {
                var stagedUUIDs = new HashSet<string>(StringComparer.Ordinal);

                // Only attempt warehouse matching if Items is available
                if (colony.Items != null)
                {
                    // Log warehouse state for diagnostics
                    int flatpackTypeCount = colony.Items.Items.Values
                        .Count(i => i.ItemType == ItemType.ItemTypeEnum.Flatpack && i.Quantity > 0);

                    // Log actual warehouse flatpack BaseItemTypeIDs for diagnostic comparison
                    var warehouseFlatpackIds = colony.Items.Items.Values
                        .Where(i => i.ItemType == ItemType.ItemTypeEnum.Flatpack && i.Quantity > 0)
                        .Select(i => i.BaseItemTypeID + " (qty=" + i.Quantity + ", name='" + i.Name + "')")
                        .ToList();

                    Log.Debug(
                        "MergeBuildings Phase 2: colony {0} has {1} remaining pool entries, {2} flatpack item types in warehouse: [{3}]",
                        colony.UUID,
                        remaining.Count,
                        flatpackTypeCount,
                        string.Join(", ", warehouseFlatpackIds));
                    // Group remaining entries by FlatpackBlueprintUUID
                    var entriesWithBlueprint = remaining
                        .Where(s => !string.IsNullOrEmpty(s.FlatpackBlueprintUUID))
                        .ToList();
                    var entriesWithoutBlueprint = remaining.Count - entriesWithBlueprint.Count;

                    if (entriesWithoutBlueprint > 0)
                    {
                        Log.Debug(
                            "MergeBuildings Phase 2: colony {0}: {1} remaining entries have no FlatpackBlueprintUUID (cannot match warehouse)",
                            colony.UUID,
                            entriesWithoutBlueprint);
                    }

                    var groupedByBlueprint = entriesWithBlueprint
                        .GroupBy(s => s.FlatpackBlueprintUUID, StringComparer.Ordinal);

                    foreach (var group in groupedByBlueprint)
                    {
                        // Sum quantities across all matching warehouse flatpack entries.
                        // Flatpacks are stored as individual Item entries (Quantity=1 each),
                        // not as a single stack. Use CountByType which sums across all stacks.
                        int stagedLimit = colony.Items.CountByType(
                            ItemType.ItemTypeEnum.Flatpack,
                            group.Key);

                        Log.Debug(
                            "MergeBuildings Phase 2: blueprint={0}, groupCount={1}, warehouseQty={2}",
                            group.Key,
                            group.Count(),
                            stagedLimit);

                        if (stagedLimit == 0)
                        {
                            continue;
                        }

                        // Order entries by BuildQueueSequence ascending, mark first N as staged
                        int stagedCount = 0;

                        foreach (var entry in group.OrderBy(s => s.BuildQueueSequence))
                        {
                            if (stagedCount >= stagedLimit)
                            {
                                break;
                            }

                            entry.Properties.SetProperty(GameConstants.PropStaged, true);
                            entry.Properties.SetProperty(GameConstants.PropBuilt, false);
                            entry.BuildCompletionTime = null;
                            stagedUUIDs.Add(entry.UUID);
                            stagedCount++;
                            hasChanges = true;

                            Log.Debug(
                                "MergeBuildings Phase 2: staged UUID={0} (blueprint={1}, warehouseQty={2})",
                                entry.UUID,
                                group.Key,
                                stagedLimit);
                        }
                    }
                }

                // Planned normalization: remaining entries with no warehouse match
                foreach (var entry in remaining)
                {
                    if (stagedUUIDs.Contains(entry.UUID))
                    {
                        continue;
                    }

                    entry.Properties.SetProperty(GameConstants.PropBuilt, false);
                    entry.Properties.SetProperty(GameConstants.PropStaged, false);
                    entry.BuildCompletionTime = null;
                    hasChanges = true;

                    Log.Debug(
                        "MergeBuildings Phase 2: planned UUID={0} (no warehouse match)",
                        entry.UUID);
                }
            }

            // Phase 3: BuildQueueSequence reassignment (task 3.3)
            // Only reassign BQS for structures actively involved in this merge cycle:
            // - Consumed structures (matched in Phase 1): ordered by API completion date
            // - Remaining pool entries (staged/planned from Phase 2): preserve relative order
            // Previously-synced structures not in this API (BuildingID > 0, not consumed)
            // and newly-created structures (already given maxBQS+1) keep their BQS.

            // Build a lookup from BuildingID to the API sort position for ordering.
            var apiOrderLookup = new Dictionary<int, int>();
            for (int i = 0; i < sortedApiBuildings.Count; i++)
            {
                apiOrderLookup[sortedApiBuildings[i].BuildingId] = i;
            }

            // Consumed structures ordered by API completion date
            var consumedOrdered = colony.Structures
                .Where(s => consumed.Contains(s.UUID))
                .OrderBy(s => apiOrderLookup.ContainsKey(s.BuildingID)
                    ? apiOrderLookup[s.BuildingID]
                    : int.MaxValue)
                .ToList();

            // Remaining pool entries from Phase 2 (BuildingID == 0, not consumed)
            // These are staged or planned structures.
            bool staged;
            var stagedStructures = remaining
                .Where(s =>
                {
                    s.Properties.GetBoolean(GameConstants.PropStaged, false, out staged);
                    return staged;
                })
                .OrderBy(s => s.BuildQueueSequence)
                .ToList();

            var stagedStructureUUIDs = new HashSet<string>(
                stagedStructures.Select(s => s.UUID),
                StringComparer.Ordinal);

            var plannedStructures = remaining
                .Where(s => !stagedStructureUUIDs.Contains(s.UUID))
                .OrderBy(s => s.BuildQueueSequence)
                .ToList();

            // Assign contiguous BuildQueueSequence starting at 1:
            // consumed (API order), then staged (user order), then planned (user order).
            int bqSeq = 0;
            foreach (var s in consumedOrdered)
            {
                bqSeq++;
                if (s.BuildQueueSequence != bqSeq)
                {
                    s.BuildQueueSequence = bqSeq;
                    hasChanges = true;
                }
            }

            foreach (var s in stagedStructures)
            {
                bqSeq++;
                if (s.BuildQueueSequence != bqSeq)
                {
                    s.BuildQueueSequence = bqSeq;
                    hasChanges = true;
                }
            }

            foreach (var s in plannedStructures)
            {
                bqSeq++;
                if (s.BuildQueueSequence != bqSeq)
                {
                    s.BuildQueueSequence = bqSeq;
                    hasChanges = true;
                }
            }

            // Defensive invariant: at most 1 structure with Built=false AND BuildCompletionTime != null
            bool builtValue;
            int buildingCount = colony.Structures.Count(s =>
            {
                s.Properties.GetBoolean(GameConstants.PropBuilt, false, out builtValue);
                return !builtValue && s.BuildCompletionTime != null;
            });

            if (buildingCount > 1)
            {
                Log.Error(
                    "MergeBuildings Phase 3 INVARIANT VIOLATION: colony {0} has {1} structures with Built=false AND BuildCompletionTime != null (expected at most 1)",
                    colony.UUID,
                    buildingCount);
            }

            Log.Info(
                "MergeBuildings: completed for colony {0} — {1} API buildings processed, hasChanges={2}",
                colony.UUID,
                apiBuildings.Count,
                hasChanges);

            return hasChanges;
        }

        /// <summary>
        /// Merges a list of API warehouse items into the colony's ItemBag.
        /// Delegates to AssetMergeService.ProcessSingleAssetItem for each item,
        /// using the same DTO and processing code as the asset sync.
        /// Optionally invokes blueprint and survey linkage services for qualifying items.
        /// </summary>
        /// <param name="apiItems">The warehouse items returned by the game API.</param>
        /// <param name="colony">The local colony whose Items bag will be updated.</param>
        /// <param name="blueprintLinkage">Optional blueprint linkage service for Bp/S items with properties.</param>
        /// <param name="surveyLinkage">Optional survey linkage service for Sc items.</param>
        /// <returns>True if any item was created or updated; otherwise false.</returns>
        public static bool MergeWarehouse(
            List<GameApiAssetCargoItem> apiItems,
            Colony colony,
            BlueprintLinkageService blueprintLinkage = null,
            SurveyLinkageService surveyLinkage = null)
        {
            if (apiItems == null || colony == null)
            {
                Log.Warn("MergeWarehouse: apiItems or colony is null, returning false");
                return false;
            }

            if (colony.Items == null)
            {
                colony.Items = new ItemBag();
            }

            bool hasChanges = false;
            var touchedUUIDs = new HashSet<string>();

            foreach (var apiItem in apiItems)
            {
                try
                {
                    string touchedUUID = AssetMergeService.ProcessSingleAssetItemAndReturnUUID(apiItem, colony.Items);
                    if (touchedUUID != null)
                    {
                        touchedUUIDs.Add(touchedUUID);

                        // Blueprint linkage for Bp/S items with properties
                        if (blueprintLinkage != null && HasBlueprintProperties(apiItem))
                        {
                            Item localItem;
                            if (colony.Items.Items.TryGetValue(touchedUUID, out localItem))
                            {
                                blueprintLinkage.ProcessItem(apiItem, localItem, colony.OwnerUUID);
                            }
                        }

                        // Survey linkage for Sc items
                        if (surveyLinkage != null && IsSurveyItem(apiItem))
                        {
                            Item localItem;
                            if (colony.Items.Items.TryGetValue(touchedUUID, out localItem))
                            {
                                surveyLinkage.ProcessItem(apiItem, localItem, colony.OwnerUUID);
                            }
                        }
                    }

                    hasChanges = true;
                }
                catch (Exception ex)
                {
                    Log.Error(
                        ex,
                        "MergeWarehouse: error processing item ResourceName='{0}' TypeC='{1}', skipping",
                        apiItem.ResourceName,
                        apiItem.TypeC);
                }
            }

            // Remove or zero items that were NOT touched by this sync.
            // Game API is authoritative — anything not in the response doesn't exist in game.
            var allLocalItems = colony.Items.Items.Values.ToList();
            Log.Debug(
                "MergeWarehouse: colony {0} — touched {1} items, local has {2} total",
                colony.UUID,
                touchedUUIDs.Count,
                allLocalItems.Count);

            var untouchedItems = allLocalItems
                .Where(i => !touchedUUIDs.Contains(i.UUID))
                .ToList();

            foreach (var item in untouchedItems)
            {
                bool hasLock = colony.Locks != null &&
                    colony.Locks.GetLockedQuantity(item.ItemType, GetLockKey(item)) > 0;

                if (hasLock)
                {
                    // Locked item: zero the quantity but keep it (staging reservation)
                    if (item.Quantity != 0)
                    {
                        Log.Info(
                            "MergeWarehouse: zeroing locked item UUID={0} Name='{1}' Qty={2}→0 (has lock, not in API)",
                            item.UUID,
                            item.Name,
                            item.Quantity);
                        item.Quantity = 0;
                        hasChanges = true;
                    }
                }
                else
                {
                    // No lock: remove entirely
                    colony.Items.Remove(item.UUID);
                    hasChanges = true;
                    Log.Info(
                        "MergeWarehouse: removed item UUID={0} Name='{1}' GameItemId={2} Qty={3} (not in API, no lock)",
                        item.UUID,
                        item.Name,
                        item.GameItemId,
                        item.Quantity);
                }
            }

            return hasChanges;
        }

        /// <summary>
        /// Merges worker data from the game API into the colony.
        /// Updates workforce allocation fields, wages, attitude, and commodity demands.
        /// </summary>
        /// <param name="apiWorkers">The workers response from the game API.</param>
        /// <param name="colony">The local colony to update.</param>
        /// <returns>True if any data changed; otherwise false.</returns>
        public static bool MergeWorkers(GameApiColonyWorkersResponse apiWorkers, Colony colony)
        {
            if (apiWorkers == null || colony == null)
            {
                Log.Warn("MergeWorkers: apiWorkers or colony is null, returning false");
                return false;
            }

            bool changed = false;

            // 1. Update WorkerCurrentAttitude
            if (colony.WorkerCurrentAttitude != apiWorkers.WorkerCurrentAttitude)
            {
                Log.Debug(
                    "MergeWorkers: colony {0} WorkerCurrentAttitude: {1} -> {2}",
                    colony.UUID,
                    colony.WorkerCurrentAttitude,
                    apiWorkers.WorkerCurrentAttitude);
                colony.WorkerCurrentAttitude = apiWorkers.WorkerCurrentAttitude;
                changed = true;
            }

            // 2. Update workforce allocation fields from WorkforceOverview
            if (apiWorkers.WorkforceOverview != null)
            {
                changed |= MergeWorkforceField(colony, "BlueCollarAllocated", colony.BlueCollarAllocated, apiWorkers.WorkforceOverview.BlueCollarAllocated, v => colony.BlueCollarAllocated = v);
                changed |= MergeWorkforceField(colony, "BlueCollarUnallocated", colony.BlueCollarUnallocated, apiWorkers.WorkforceOverview.BlueCollarUnallocated, v => colony.BlueCollarUnallocated = v);
                changed |= MergeWorkforceField(colony, "WhiteCollarAllocated", colony.WhiteCollarAllocated, apiWorkers.WorkforceOverview.WhiteCollarAllocated, v => colony.WhiteCollarAllocated = v);
                changed |= MergeWorkforceField(colony, "WhiteCollarUnallocated", colony.WhiteCollarUnallocated, apiWorkers.WorkforceOverview.WhiteCollarUnallocated, v => colony.WhiteCollarUnallocated = v);
                changed |= MergeWorkforceField(colony, "SpecialistAllocated", colony.SpecialistAllocated, apiWorkers.WorkforceOverview.SpecialistAllocated, v => colony.SpecialistAllocated = v);
                changed |= MergeWorkforceField(colony, "SpecialistUnallocated", colony.SpecialistUnallocated, apiWorkers.WorkforceOverview.SpecialistUnallocated, v => colony.SpecialistUnallocated = v);
            }

            // 3. Update WageLevel from Wages
            if (apiWorkers.Wages != null)
            {
                if (colony.WageLevel != apiWorkers.Wages.CurrentWagePercentage)
                {
                    Log.Debug(
                        "MergeWorkers: colony {0} WageLevel: {1} -> {2}",
                        colony.UUID,
                        colony.WageLevel,
                        apiWorkers.Wages.CurrentWagePercentage);
                    colony.WageLevel = apiWorkers.Wages.CurrentWagePercentage;
                    changed = true;
                }
            }

            // 4. Map commodity demands
            var newCommodities = MapCommodityDemands(apiWorkers.WorkforceCommodityDemands);

            // 5. Compare old vs new commodities list
            int oldCount = colony.Commodities?.Count ?? 0;
            int oldFulfilled = colony.Commodities?.Count(c => c.Fulfilled) ?? 0;
            int oldUnfulfilled = oldCount - oldFulfilled;
            int newFulfilled = newCommodities.Count(c => c.Fulfilled);
            int newUnfulfilled = newCommodities.Count - newFulfilled;

            Log.Debug(
                "MergeWorkers: colony {0} commodity comparison — old: {1} total ({2} fulfilled, {3} unfulfilled), new: {4} total ({5} fulfilled, {6} unfulfilled)",
                colony.UUID,
                oldCount,
                oldFulfilled,
                oldUnfulfilled,
                newCommodities.Count,
                newFulfilled,
                newUnfulfilled);

            if (!CommodityListsEqual(colony.Commodities, newCommodities))
            {
                Log.Debug(
                    "MergeWorkers: colony {0} Commodities REPLACING list — old names: [{1}], new names: [{2}]",
                    colony.UUID,
                    colony.Commodities != null ? string.Join(", ", colony.Commodities.Select(c => c.Name + (c.Fulfilled ? "(F)" : "(U)"))) : "null",
                    string.Join(", ", newCommodities.Select(c => c.Name + (c.Fulfilled ? "(F)" : "(U)"))));

                colony.Commodities = newCommodities;
                changed = true;
                Log.Debug(
                    "MergeWorkers: colony {0} Commodities updated ({1} demands)",
                    colony.UUID,
                    newCommodities.Count);
            }
            else
            {
                Log.Debug(
                    "MergeWorkers: colony {0} Commodities unchanged (lists equal, {1} demands)",
                    colony.UUID,
                    newCommodities.Count);
            }

            Log.Info(
                "MergeWorkers: completed for colony {0} — hasChanges={1}",
                colony.UUID,
                changed);

            return changed;
        }

        /// <summary>
        /// Gets the lock key for an item (Name + "|" + ResourcePurity for resources, just Name otherwise).
        /// </summary>
        private static string GetLockKey(Item item)
        {
            if (item.ItemType == ItemType.ItemTypeEnum.Resource && !string.IsNullOrEmpty(item.ResourcePurity))
            {
                return item.Name + "|" + item.ResourcePurity;
            }

            return item.Name ?? string.Empty;
        }

        /// <summary>
        /// Determines whether an API item qualifies for blueprint linkage.
        /// Returns true if typeC is "Bp" or "S" and the item has non-empty properties.
        /// </summary>
        private static bool HasBlueprintProperties(GameApiAssetCargoItem apiItem)
        {
            var typeC = apiItem.TypeC?.Trim();
            if (string.IsNullOrEmpty(typeC))
            {
                return false;
            }

            bool isBlueprintType = string.Equals(typeC, "Bp", StringComparison.OrdinalIgnoreCase)
                || string.Equals(typeC, "S", StringComparison.OrdinalIgnoreCase);

            return isBlueprintType && apiItem.Properties != null && apiItem.Properties.Count > 0;
        }

        /// <summary>
        /// Determines whether an API item is a survey item (typeC = "Sc").
        /// </summary>
        private static bool IsSurveyItem(GameApiAssetCargoItem apiItem)
        {
            return string.Equals(apiItem.TypeC?.Trim(), "Sc", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Maps API commodity demands to local CommodityRequested list.
        /// </summary>
        private static List<CommodityRequested> MapCommodityDemands(List<GameApiCommodityDemand> demands)
        {
            if (demands == null || demands.Count == 0)
            {
                Log.Debug("MapCommodityDemands: no demands to map (null or empty)");
                return new List<CommodityRequested>();
            }

            var result = new List<CommodityRequested>();
            foreach (var d in demands)
            {
                var mapped = new CommodityRequested
                {
                    Name = d.TypeName,
                    Requested = d.Amount,
                    NeedBy = d.RequiredBy,
                    Fulfilled = d.Fulfilled,
                    Delivered = d.Fulfilled ? d.Amount : 0,
                };
                result.Add(mapped);

                Log.Debug(
                    "MapCommodityDemands: id={0} name='{1}' amount={2} requiredBy={3:o} fulfilled={4} → Delivered={5}",
                    d.Id,
                    d.TypeName,
                    d.Amount,
                    d.RequiredBy,
                    d.Fulfilled,
                    mapped.Delivered);
            }

            return result;
        }

        /// <summary>
        /// Compares two CommodityRequested lists for equality.
        /// </summary>
        private static bool CommodityListsEqual(List<CommodityRequested> local, List<CommodityRequested> mapped)
        {
            if (local == null && mapped == null)
            {
                return true;
            }

            if (local == null || mapped == null)
            {
                return false;
            }

            if (local.Count != mapped.Count)
            {
                return false;
            }

            for (int i = 0; i < local.Count; i++)
            {
                if (!string.Equals(local[i].Name, mapped[i].Name, StringComparison.Ordinal) ||
                    local[i].Requested != mapped[i].Requested ||
                    local[i].NeedBy != mapped[i].NeedBy ||
                    local[i].Fulfilled != mapped[i].Fulfilled ||
                    local[i].Delivered != mapped[i].Delivered)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Merges a single workforce integer field. Returns true if changed.
        /// </summary>
        private static bool MergeWorkforceField(Colony colony, string fieldName, int localValue, int apiValue, Action<int> setter)
        {
            if (localValue != apiValue)
            {
                Log.Debug(
                    "MergeWorkers: colony {0} {1}: {2} -> {3}",
                    colony.UUID,
                    fieldName,
                    localValue,
                    apiValue);
                setter(apiValue);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Finds a local structure matching the API building, excluding already-consumed entries.
        /// First tries matching by BuildingID (for previously synced structures),
        /// then falls back to matching unassigned structures of the same type by DisplaySequence order.
        /// Consumed UUIDs are excluded from both primary and fallback matching to prevent
        /// the same pool entry from being matched by multiple API buildings.
        /// </summary>
        /// <param name="apiBuilding">The API building to find a local match for.</param>
        /// <param name="colony">The colony whose structures are searched.</param>
        /// <param name="consumed">Set of pool entry UUIDs already matched; excluded from candidates.</param>
        /// <returns>The matched local structure, or null if no match found.</returns>
        private static ColonyStructure FindLocalStructure(
            GameApiColonyBuilding apiBuilding,
            Colony colony,
            HashSet<string> consumed)
        {
            // Primary match: by BuildingID (structures that were previously synced)
            var match = colony.Structures.FirstOrDefault(s =>
                s.BuildingID > 0 &&
                s.BuildingID == apiBuilding.BuildingId &&
                !consumed.Contains(s.UUID));

            if (match != null)
            {
                return match;
            }

            // Fallback: match unassigned structures of the same ColonyBuildingTypeId by DisplaySequence.
            // "Unassigned" means BuildingID == 0 (never synced with API before).
            if (apiBuilding.ColonyBuildingTypeId > 0)
            {
                var unassigned = colony.Structures
                    .Where(s => s.BuildingID == 0 &&
                                s.ColonyBuildingTypeId == apiBuilding.ColonyBuildingTypeId &&
                                !consumed.Contains(s.UUID))
                    .OrderBy(s => s.DisplaySequence)
                    .FirstOrDefault();

                if (unassigned != null)
                {
                    Log.Debug(
                        "FindLocalStructure: fallback match by ColonyBuildingTypeId={0} — API buildingId={1} -> local UUID={2} (seq={3})",
                        apiBuilding.ColonyBuildingTypeId,
                        apiBuilding.BuildingId,
                        unassigned.UUID,
                        unassigned.DisplaySequence);
                    return unassigned;
                }
            }

            return null;
        }

        /// <summary>
        /// Merges API building data into an existing local structure.
        /// Updates game-authoritative fields while preserving local-only fields.
        /// </summary>
        private static bool MergeExistingStructure(
            ColonyStructure local,
            GameApiColonyBuilding apiBuilding)
        {
            bool changed = false;

            // Update BuildingID for correlation (Req 8.1)
            if (local.BuildingID != apiBuilding.BuildingId)
            {
                local.BuildingID = apiBuilding.BuildingId;
                changed = true;
            }

            // Update Built/Online status from API (Req 8.2)
            // If constructingBuildingFinish is in the future, the building is still under construction
            bool isCurrentlyBuilding = apiBuilding.ConstructingBuildingFinish != null &&
                apiBuilding.ConstructingBuildingFinish.Value > SystemClock.UtcNow;
            bool apiBuildingBuilt = apiBuilding.StatusId > 0 && !isCurrentlyBuilding;
            bool apiOnline = apiBuildingBuilt && apiBuilding.BuildingOnline;

            bool currentBuilt;
            local.Properties.GetBoolean(GameConstants.PropBuilt, false, out currentBuilt);
            if (currentBuilt != apiBuildingBuilt)
            {
                local.Properties.SetProperty(GameConstants.PropBuilt, apiBuildingBuilt);
                changed = true;
                Log.Debug(
                    "MergeBuildings: structure {0} Built: {1} -> {2} (API wins, isCurrentlyBuilding={3})",
                    local.UUID,
                    currentBuilt,
                    apiBuildingBuilt,
                    isCurrentlyBuilding);
            }

            bool currentOnline;
            local.Properties.GetBoolean(GameConstants.PropOnline, false, out currentOnline);
            if (currentOnline != apiOnline)
            {
                local.Properties.SetProperty(GameConstants.PropOnline, apiOnline);
                changed = true;
                Log.Debug(
                    "MergeBuildings: structure {0} Online: {1} -> {2} (API wins)",
                    local.UUID,
                    currentOnline,
                    apiOnline);
            }

            // Update ColonyBuildingTypeId (Req 8.8)
            if (local.ColonyBuildingTypeId != apiBuilding.ColonyBuildingTypeId)
            {
                Log.Debug(
                    "MergeBuildings: structure {0} ColonyBuildingTypeId: {1} -> {2} (API wins)",
                    local.UUID,
                    local.ColonyBuildingTypeId,
                    apiBuilding.ColonyBuildingTypeId);
                local.ColonyBuildingTypeId = apiBuilding.ColonyBuildingTypeId;
                changed = true;
            }

            // Update ResourceId (Req 8.9)
            if (local.ResourceId != apiBuilding.ResourceId)
            {
                Log.Debug(
                    "MergeBuildings: structure {0} ResourceId: {1} -> {2} (API wins)",
                    local.UUID,
                    local.ResourceId,
                    apiBuilding.ResourceId);
                local.ResourceId = apiBuilding.ResourceId;
                changed = true;
            }

            // Update ResourceIcon (Req 8.10)
            if (!string.Equals(local.ResourceIcon, apiBuilding.ResourceIcon, StringComparison.Ordinal))
            {
                Log.Debug(
                    "MergeBuildings: structure {0} ResourceIcon: '{1}' -> '{2}' (API wins)",
                    local.UUID,
                    local.ResourceIcon,
                    apiBuilding.ResourceIcon);
                local.ResourceIcon = apiBuilding.ResourceIcon ?? string.Empty;
                changed = true;
            }

            // Update ManufactureAmountPerRun (Req 8.11)
            if (local.ManufactureAmountPerRun != apiBuilding.ManufactureAmountPerRun)
            {
                Log.Debug(
                    "MergeBuildings: structure {0} ManufactureAmountPerRun: {1} -> {2} (API wins)",
                    local.UUID,
                    local.ManufactureAmountPerRun,
                    apiBuilding.ManufactureAmountPerRun);
                local.ManufactureAmountPerRun = apiBuilding.ManufactureAmountPerRun;
                changed = true;
            }

            // Update DurabilityCurrent and DurabilityMax (Req 8.12)
            decimal apiDurabilityCurrent = (decimal)apiBuilding.DurabilityCurrent;
            if (local.DurabilityCurrent != apiDurabilityCurrent)
            {
                Log.Debug(
                    "MergeBuildings: structure {0} DurabilityCurrent: {1} -> {2} (API wins)",
                    local.UUID,
                    local.DurabilityCurrent,
                    apiDurabilityCurrent);
                local.DurabilityCurrent = apiDurabilityCurrent;
                changed = true;
            }

            decimal apiDurabilityMax = (decimal)apiBuilding.DurabilityMax;
            if (local.DurabilityMax != apiDurabilityMax)
            {
                Log.Debug(
                    "MergeBuildings: structure {0} DurabilityMax: {1} -> {2} (API wins)",
                    local.UUID,
                    local.DurabilityMax,
                    apiDurabilityMax);
                local.DurabilityMax = apiDurabilityMax;
                changed = true;
            }

            // Replace collections (Req 8.13-8.18)
            changed |= ReplaceOpsStatusEffects(local, apiBuilding);
            changed |= ReplaceIndustries(local, apiBuilding);
            changed |= ReplaceDetailsRequired(local, apiBuilding);
            changed |= ReplaceSupportDetailsRequired(local, apiBuilding);
            changed |= ReplaceBuildingAttributes(local, apiBuilding);
            changed |= ReplaceExtraProperties(local, apiBuilding);

            // Conditional: MiningSurveyResource if local empty (Req 8.6)
            if (!string.IsNullOrEmpty(apiBuilding.ResourceName) &&
                string.IsNullOrEmpty(local.MiningSurveyResource))
            {
                Log.Debug(
                    "MergeBuildings: structure {0} MiningSurveyResource: (empty) -> '{1}' (API conditional)",
                    local.UUID,
                    apiBuilding.ResourceName);
                local.MiningSurveyResource = apiBuilding.ResourceName;
                changed = true;
            }

            // Conditional: BuildCompletionTime if local null (Req 8.7)
            Log.Debug(
                "MergeBuildings: structure {0} BuildCompletionTime check: apiFinish={1}, localTimer={2}, isCurrentlyBuilding={3}",
                local.UUID,
                apiBuilding.ConstructingBuildingFinish?.ToString("O") ?? "(null)",
                local.BuildCompletionTime != null ? $"exists(TR={local.BuildCompletionTime.TimeRemaining})" : "null",
                isCurrentlyBuilding);

            if (apiBuilding.ConstructingBuildingFinish != null && local.BuildCompletionTime == null)
            {
                var finishTime = apiBuilding.ConstructingBuildingFinish.Value;
                var now = SystemClock.UtcNow;
                long remainingSeconds = (long)(finishTime - now).TotalSeconds;
                if (remainingSeconds < 0)
                {
                    remainingSeconds = 0;
                }

                local.BuildCompletionTime = new CountDownTime();
                local.BuildCompletionTime.TimeRemaining = remainingSeconds;

                Log.Debug(
                    "MergeBuildings: structure {0} BuildCompletionTime set from API (finish={1:O}, remaining={2}s)",
                    local.UUID,
                    finishTime,
                    remainingSeconds);
                changed = true;
            }

            // Conditional: FlatpackBlueprintUUID if local empty and API has BlueprintDesignName
            if (string.IsNullOrEmpty(local.FlatpackBlueprintUUID) &&
                !string.IsNullOrEmpty(apiBuilding.BlueprintDesignName))
            {
                var empireContext = EmpireContext.GetInstanceIfLoaded();
                if (empireContext != null)
                {
                    var flatpackLookup = ColonyParser.BuildFlatpackLookup(empireContext);
                    if (flatpackLookup.TryGetValue(apiBuilding.BlueprintDesignName, out string flatpackUuid))
                    {
                        local.FlatpackBlueprintUUID = flatpackUuid;
                        changed = true;
                        Log.Debug(
                            "MergeBuildings: structure {0} FlatpackBlueprintUUID mapped from BlueprintDesignName '{1}' -> {2}",
                            local.UUID,
                            apiBuilding.BlueprintDesignName,
                            flatpackUuid);
                    }
                    else
                    {
                        Log.Warn(
                            "MergeBuildings: structure {0} no flatpack blueprint found for BlueprintDesignName '{1}'",
                            local.UUID,
                            apiBuilding.BlueprintDesignName);
                    }
                }
            }

            return changed;
        }

        /// <summary>
        /// Creates a new ColonyStructure from API building data.
        /// </summary>
        private static ColonyStructure CreateStructureFromApi(GameApiColonyBuilding apiBuilding)
        {
            var structure = new ColonyStructure
            {
                UUID = Guid.NewGuid().ToString(),
                BuildingID = apiBuilding.BuildingId,
                ColonyBuildingTypeId = apiBuilding.ColonyBuildingTypeId,
                ResourceId = apiBuilding.ResourceId,
                ResourceIcon = apiBuilding.ResourceIcon ?? string.Empty,
                ManufactureAmountPerRun = apiBuilding.ManufactureAmountPerRun,
                DurabilityCurrent = (decimal)apiBuilding.DurabilityCurrent,
                DurabilityMax = (decimal)apiBuilding.DurabilityMax,
            };

            // Set Built/Online status (Req 8.3)
            // If constructingBuildingFinish is in the future, the building is still under construction
            bool isCurrentlyBuilding = apiBuilding.ConstructingBuildingFinish != null &&
                apiBuilding.ConstructingBuildingFinish.Value > SystemClock.UtcNow;
            bool isBuilt = apiBuilding.StatusId > 0 && !isCurrentlyBuilding;
            structure.Properties.SetProperty(GameConstants.PropBuilt, isBuilt);
            structure.Properties.SetProperty(GameConstants.PropOnline, isBuilt && apiBuilding.BuildingOnline);

            // Map collections from API DTOs to local model types
            structure.OpsStatusEffects = MapOpsStatusEffects(apiBuilding.OpsStatusEffects);
            structure.Industries = MapIndustries(apiBuilding.Industries);
            structure.DetailsRequired = MapDetailsRequired(apiBuilding.DetailsRequired);
            structure.SupportDetailsRequired = MapDetailsRequired(apiBuilding.SupportDetailsRequired);
            structure.BuildingAttributes = MapBuildingAttributes(apiBuilding.BuildingAttributes);
            structure.ExtraProperties = MapExtraProperties(apiBuilding.ExtraProperties);

            // Conditional: MiningSurveyResource from ResourceName
            if (!string.IsNullOrEmpty(apiBuilding.ResourceName))
            {
                structure.MiningSurveyResource = apiBuilding.ResourceName;
            }

            // Conditional: BuildCompletionTime from ConstructingBuildingFinish
            if (apiBuilding.ConstructingBuildingFinish != null)
            {
                var finishTime = apiBuilding.ConstructingBuildingFinish.Value;
                var now = SystemClock.UtcNow;
                long remainingSeconds = (long)(finishTime - now).TotalSeconds;
                if (remainingSeconds < 0)
                {
                    remainingSeconds = 0;
                }

                structure.BuildCompletionTime = new CountDownTime();
                structure.BuildCompletionTime.TimeRemaining = remainingSeconds;
            }

            // Map FlatpackBlueprintUUID from BlueprintDesignName via flatpack lookup
            if (!string.IsNullOrEmpty(apiBuilding.BlueprintDesignName))
            {
                var empireContext = EmpireContext.GetInstanceIfLoaded();
                if (empireContext != null)
                {
                    var flatpackLookup = ColonyParser.BuildFlatpackLookup(empireContext);
                    if (flatpackLookup.TryGetValue(apiBuilding.BlueprintDesignName, out string flatpackUuid))
                    {
                        structure.FlatpackBlueprintUUID = flatpackUuid;
                        Log.Debug(
                            "MergeBuildings: mapped BlueprintDesignName '{0}' to FlatpackBlueprintUUID={1} for new structure",
                            apiBuilding.BlueprintDesignName,
                            flatpackUuid);
                    }
                    else
                    {
                        Log.Warn(
                            "MergeBuildings: no flatpack blueprint found for BlueprintDesignName '{0}' on new structure",
                            apiBuilding.BlueprintDesignName);
                    }
                }
                else
                {
                    Log.Debug(
                        "MergeBuildings: EmpireContext not loaded, cannot resolve FlatpackBlueprintUUID for '{0}'",
                        apiBuilding.BlueprintDesignName);
                }
            }

            return structure;
        }

        /// <summary>
        /// Replaces OpsStatusEffects on the local structure with mapped API data.
        /// </summary>
        private static bool ReplaceOpsStatusEffects(
            ColonyStructure local,
            GameApiColonyBuilding apiBuilding)
        {
            var mapped = MapOpsStatusEffects(apiBuilding.OpsStatusEffects);
            local.OpsStatusEffects = mapped;
            return true;
        }

        /// <summary>
        /// Replaces Industries on the local structure with mapped API data.
        /// </summary>
        private static bool ReplaceIndustries(
            ColonyStructure local,
            GameApiColonyBuilding apiBuilding)
        {
            var mapped = MapIndustries(apiBuilding.Industries);
            local.Industries = mapped;
            return true;
        }

        /// <summary>
        /// Replaces DetailsRequired on the local structure with mapped API data.
        /// </summary>
        private static bool ReplaceDetailsRequired(
            ColonyStructure local,
            GameApiColonyBuilding apiBuilding)
        {
            var mapped = MapDetailsRequired(apiBuilding.DetailsRequired);
            local.DetailsRequired = mapped;
            return true;
        }

        /// <summary>
        /// Replaces SupportDetailsRequired on the local structure with mapped API data.
        /// </summary>
        private static bool ReplaceSupportDetailsRequired(
            ColonyStructure local,
            GameApiColonyBuilding apiBuilding)
        {
            var mapped = MapDetailsRequired(apiBuilding.SupportDetailsRequired);
            local.SupportDetailsRequired = mapped;
            return true;
        }

        /// <summary>
        /// Replaces BuildingAttributes on the local structure with mapped API data.
        /// </summary>
        private static bool ReplaceBuildingAttributes(
            ColonyStructure local,
            GameApiColonyBuilding apiBuilding)
        {
            var mapped = MapBuildingAttributes(apiBuilding.BuildingAttributes);
            local.BuildingAttributes = mapped;
            return true;
        }

        /// <summary>
        /// Replaces ExtraProperties on the local structure with mapped API data.
        /// </summary>
        private static bool ReplaceExtraProperties(
            ColonyStructure local,
            GameApiColonyBuilding apiBuilding)
        {
            var mapped = MapExtraProperties(apiBuilding.ExtraProperties);
            local.ExtraProperties = mapped;
            return true;
        }

        /// <summary>
        /// Maps API status effects to local model types.
        /// </summary>
        private static List<BuildingStatusEffect> MapOpsStatusEffects(
            List<GameApiBuildingStatusEffect> apiEffects)
        {
            if (apiEffects == null)
            {
                return new List<BuildingStatusEffect>();
            }

            return apiEffects.Select(e => new BuildingStatusEffect
            {
                StatusId = e.StatusId,
                ModTypeId = e.ModTypeId,
                Change = (decimal)e.Change,
            }).ToList();
        }

        /// <summary>
        /// Maps API industries to local model types.
        /// </summary>
        private static List<BuildingIndustry> MapIndustries(
            List<GameApiBuildingIndustry> apiIndustries)
        {
            if (apiIndustries == null)
            {
                return new List<BuildingIndustry>();
            }

            return apiIndustries.Select(i => new BuildingIndustry
            {
                Id = i.Id,
                Name = i.Name ?? string.Empty,
            }).ToList();
        }

        /// <summary>
        /// Maps API detail requirements to local model types.
        /// </summary>
        private static List<BuildingDetailRequirement> MapDetailsRequired(
            List<GameApiBuildingDetailRequirement> apiDetails)
        {
            if (apiDetails == null)
            {
                return new List<BuildingDetailRequirement>();
            }

            return apiDetails.Select(d => new BuildingDetailRequirement
            {
                Name = d.Name ?? string.Empty,
                WorkerID = d.WorkerID,
            }).ToList();
        }

        /// <summary>
        /// Maps API building attributes to local model types.
        /// </summary>
        private static List<BuildingAttribute> MapBuildingAttributes(
            List<GameApiBuildingAttribute> apiAttributes)
        {
            if (apiAttributes == null)
            {
                return new List<BuildingAttribute>();
            }

            return apiAttributes.Select(a => new BuildingAttribute
            {
                ModTypeId = a.ModTypeId,
                PropertyName = a.PropertyName ?? string.Empty,
                FriendlyPropertyName = a.FriendlyPropertyName ?? string.Empty,
                PropertyValue = a.PropertyValue ?? string.Empty,
                Unit = a.Unit ?? string.Empty,
            }).ToList();
        }

        /// <summary>
        /// Maps API extra properties to local model types.
        /// </summary>
        private static List<BuildingExtraProperty> MapExtraProperties(
            List<GameApiBuildingExtraProperty> apiProperties)
        {
            if (apiProperties == null)
            {
                return new List<BuildingExtraProperty>();
            }

            return apiProperties.Select(p => new BuildingExtraProperty
            {
                Info1 = p.Info1 ?? string.Empty,
                Info2 = p.Info2 ?? string.Empty,
                Info3 = p.Info3 ?? string.Empty,
                Info4 = p.Info4 ?? string.Empty,
            }).ToList();
        }

        /// <summary>
        /// Processes a single API colony: finds a local match or creates a new colony.
        /// </summary>
        private static void ProcessSingleColony(
            GameApiColonyListItem apiColony,
            List<Colony> localColonies,
            string ownerUUID,
            ColonyMergeResult result)
        {
            Log.Debug(
                "MergeColonyList: matching API colony ColonyId={0} Planet='{1}' System='{2}' Name='{3}' against {4} local colonies for owner {5}",
                apiColony.ColonyId,
                apiColony.SystemObjectName,
                apiColony.SystemName,
                apiColony.ColonyName,
                localColonies.Count,
                ownerUUID);

            // Find local match by PlanetName + SystemName (case-insensitive, same owner)
            var match = localColonies.FirstOrDefault(c =>
                string.Equals(c.OwnerUUID, ownerUUID, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.PlanetName, apiColony.SystemObjectName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.SystemName, apiColony.SystemName, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                Log.Debug(
                    "MergeColonyList: matched API ColonyId={0} to local UUID={1} (Planet='{2}' System='{3}')",
                    apiColony.ColonyId,
                    match.UUID,
                    match.PlanetName,
                    match.SystemName);

                bool changed = MergeAllColonyFields(match, apiColony);
                match.LastImportDateTime = SystemClock.UtcNow.ToString("o");
                result.ColonyIdToUUIDMap[apiColony.ColonyId] = match.UUID;

                if (changed)
                {
                    result.Updated++;
                }
            }
            else
            {
                Log.Info(
                    "MergeColonyList: no local colony matched API ColonyId={0} Planet='{1}' System='{2}' — creating new. Local candidates: [{3}]",
                    apiColony.ColonyId,
                    apiColony.SystemObjectName,
                    apiColony.SystemName,
                    FormatLocalColonyCandidates(localColonies, ownerUUID));

                var newColony = CreateColonyFromApi(apiColony, ownerUUID);
                localColonies.Add(newColony);
                result.ColonyIdToUUIDMap[apiColony.ColonyId] = newColony.UUID;
                result.Created++;
            }
        }

        /// <summary>
        /// Formats a summary of local colonies for the given owner, for diagnostic logging.
        /// </summary>
        private static string FormatLocalColonyCandidates(List<Colony> localColonies, string ownerUUID)
        {
            var candidates = localColonies
                .Where(c => string.Equals(c.OwnerUUID, ownerUUID, StringComparison.OrdinalIgnoreCase))
                .Select(c => "'" + c.PlanetName + "' @ '" + c.SystemName + "'");
            return string.Join(", ", candidates);
        }

        /// <summary>
        /// Creates a new Colony from API data with a new UUID.
        /// </summary>
        private static Colony CreateColonyFromApi(GameApiColonyListItem apiColony, string ownerUUID)
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = ownerUUID,
                PlanetName = apiColony.SystemObjectName,
                SystemName = apiColony.SystemName,
                ColonyName = apiColony.ColonyName,
                ColonyId = apiColony.ColonyId,
                SystemId = apiColony.SystemId,
                ColonySize = apiColony.ColonySize,
                Distance = (decimal)apiColony.Distance,
                SurfaceVariation = apiColony.SurfaceVariation,
                AtmosVariation = apiColony.AtmosVariation,
                HexValue = apiColony.HexValue ?? string.Empty,
                SystemObjectTypeName = apiColony.SystemObjectTypeName ?? string.Empty,
                ImagePreFix = apiColony.ImagePreFix ?? string.Empty,
                ManufacturingBlocked = apiColony.ManufacturingBlocked,
                WorkerCurrentAttitude = apiColony.WorkerCurrentAttitude,
                ContentmentIndex = apiColony.ContentmentIndex,
                LastImportDateTime = SystemClock.UtcNow.ToString("o"),
            };

            Log.Info(
                "MergeColonyList: created new colony UUID={0} Planet='{1}' System='{2}' Name='{3}'",
                colony.UUID,
                colony.PlanetName,
                colony.SystemName,
                colony.ColonyName);

            return colony;
        }

        /// <summary>
        /// Merges all 14 game-authoritative fields from the API colony into the local colony.
        /// String fields: skip if API value is null/empty (preserve local).
        /// Numeric fields: always overwrite (0 is a valid game state).
        /// </summary>
        /// <param name="local">The local colony to update.</param>
        /// <param name="apiColony">The API colony data.</param>
        /// <returns>True if any field was changed; otherwise false.</returns>
        private static bool MergeAllColonyFields(Colony local, GameApiColonyListItem apiColony)
        {
            bool changed = false;

            // String fields: skip if API value is null/empty (preserve local)
            changed |= MergeStringField(local, "ColonyName", local.ColonyName, apiColony.ColonyName, v => local.ColonyName = v);
            changed |= MergeStringField(local, "PlanetName", local.PlanetName, apiColony.SystemObjectName, v => local.PlanetName = v);
            changed |= MergeStringField(local, "SystemName", local.SystemName, apiColony.SystemName, v => local.SystemName = v);
            changed |= MergeStringField(local, "HexValue", local.HexValue, apiColony.HexValue, v => local.HexValue = v);
            changed |= MergeStringField(local, "SystemObjectTypeName", local.SystemObjectTypeName, apiColony.SystemObjectTypeName, v => local.SystemObjectTypeName = v);
            changed |= MergeStringField(local, "ImagePreFix", local.ImagePreFix, apiColony.ImagePreFix, v => local.ImagePreFix = v);

            // Numeric fields: always overwrite (0 is valid game state)
            changed |= MergeNumericField(local, "ColonyId", local.ColonyId, apiColony.ColonyId, v => local.ColonyId = v);
            changed |= MergeNumericField(local, "SystemId", local.SystemId, apiColony.SystemId, v => local.SystemId = v);
            changed |= MergeNumericField(local, "ColonySize", local.ColonySize, apiColony.ColonySize, v => local.ColonySize = v);
            changed |= MergeDecimalField(local, "Distance", local.Distance, (decimal)apiColony.Distance, v => local.Distance = v);
            changed |= MergeNumericField(local, "SurfaceVariation", local.SurfaceVariation, apiColony.SurfaceVariation, v => local.SurfaceVariation = v);
            changed |= MergeNumericField(local, "AtmosVariation", local.AtmosVariation, apiColony.AtmosVariation, v => local.AtmosVariation = v);
            changed |= MergeNumericField(local, "ManufacturingBlocked", local.ManufacturingBlocked, apiColony.ManufacturingBlocked, v => local.ManufacturingBlocked = v);
            changed |= MergeNumericField(local, "WorkerCurrentAttitude", local.WorkerCurrentAttitude, apiColony.WorkerCurrentAttitude, v => local.WorkerCurrentAttitude = v);
            changed |= MergeNumericField(local, "ContentmentIndex", local.ContentmentIndex, apiColony.ContentmentIndex, v => local.ContentmentIndex = v);

            return changed;
        }

        /// <summary>
        /// Merges a string field from the API into the local colony.
        /// Skips if the API value is null or empty (preserves local value).
        /// </summary>
        private static bool MergeStringField(Colony local, string fieldName, string localValue, string apiValue, Action<string> setter)
        {
            if (string.IsNullOrEmpty(apiValue))
            {
                return false;
            }

            if (!string.Equals(localValue, apiValue, StringComparison.Ordinal))
            {
                Log.Debug(
                    "Colony {0} field {1}: '{2}' -> '{3}' (API wins)",
                    local.UUID,
                    fieldName,
                    localValue,
                    apiValue);
                setter(apiValue);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Merges an integer field from the API into the local colony.
        /// Always overwrites (0 is a valid game state).
        /// </summary>
        private static bool MergeNumericField(Colony local, string fieldName, int localValue, int apiValue, Action<int> setter)
        {
            if (localValue != apiValue)
            {
                Log.Debug(
                    "Colony {0} field {1}: '{2}' -> '{3}' (API wins)",
                    local.UUID,
                    fieldName,
                    localValue,
                    apiValue);
                setter(apiValue);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Merges a decimal field from the API into the local colony.
        /// Always overwrites (0 is a valid game state).
        /// </summary>
        private static bool MergeDecimalField(Colony local, string fieldName, decimal localValue, decimal apiValue, Action<decimal> setter)
        {
            if (localValue != apiValue)
            {
                Log.Debug(
                    "Colony {0} field {1}: '{2}' -> '{3}' (API wins)",
                    local.UUID,
                    fieldName,
                    localValue,
                    apiValue);
                setter(apiValue);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Result of a colony list merge operation.
    /// </summary>
    public class ColonyMergeResult
    {
        /// <summary>
        /// Gets or sets the number of new colonies created.
        /// </summary>
        public int Created { get; set; }

        /// <summary>
        /// Gets or sets the number of existing colonies updated.
        /// </summary>
        public int Updated { get; set; }

        /// <summary>
        /// Gets or sets the number of colonies skipped (errors or invalid data).
        /// </summary>
        public int Skipped { get; set; }

        /// <summary>
        /// Gets a value indicating whether any colonies were created or updated.
        /// </summary>
        public bool HasChanges => Created > 0 || Updated > 0;

        /// <summary>
        /// Gets or sets the mapping of API ColonyId to local Colony UUID.
        /// </summary>
        public Dictionary<int, string> ColonyIdToUUIDMap { get; set; } = new Dictionary<int, string>();
    }
}
