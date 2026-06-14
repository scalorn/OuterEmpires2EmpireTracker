// <copyright file="CrateContentImporter.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Common.Client.Generated;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Result of a crate content import operation, containing statistics
    /// and any nested crate IDs discovered for cascade processing.
    /// </summary>
    public class CrateContentImportResult
    {
        /// <summary>
        /// Gets or sets the total number of cargo items in the API response.
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Gets or sets the number of items successfully imported.
        /// </summary>
        public int Imported { get; set; }

        /// <summary>
        /// Gets or sets the number of items that failed to import.
        /// </summary>
        public int Failed { get; set; }

        /// <summary>
        /// Gets or sets the number of blueprints linked to the master list.
        /// </summary>
        public int BlueprintsLinked { get; set; }

        /// <summary>
        /// Gets or sets the list of nested crate GameItemIds for cascade work items.
        /// </summary>
        public List<int> NestedCrateIds { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets the per-type item count breakdown.
        /// </summary>
        public Dictionary<ItemType.ItemTypeEnum, int> CountsByType { get; set; }
            = new Dictionary<ItemType.ItemTypeEnum, int>();

        /// <summary>
        /// Gets or sets the list of errors encountered during import.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether the import completed successfully.
        /// </summary>
        public bool Success { get; set; } = true;
    }

    /// <summary>
    /// Imports crate contents from a raw JSON response into the local data model.
    /// Parses the game API crate detail response and populates the crate Item's
    /// Contents bag with all item types found inside the crate.
    /// </summary>
    public class CrateContentImporter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;
        private readonly EmpireContext _empireContext;
        private readonly BlueprintLinkageService _blueprintLinkageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CrateContentImporter"/> class.
        /// </summary>
        /// <param name="playerContext">The player context for persistence.</param>
        /// <param name="empireContext">The empire context for shared game data.</param>
        /// <param name="blueprintLinkageService">The blueprint linkage service for dual-tracking.</param>
        public CrateContentImporter(
            PlayerContext playerContext,
            EmpireContext empireContext,
            BlueprintLinkageService blueprintLinkageService)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
            _empireContext = empireContext ?? throw new ArgumentNullException(nameof(empireContext));
            _blueprintLinkageService = blueprintLinkageService ?? throw new ArgumentNullException(nameof(blueprintLinkageService));
        }

        /// <summary>
        /// Imports crate contents from a typed <see cref="AssetCrateContents"/> DTO
        /// into the local data model. Bypasses JSON deserialization since the typed
        /// client already provides the unwrapped DTO.
        /// </summary>
        /// <param name="crateContents">The typed crate contents DTO from the game API.</param>
        /// <param name="crateGameItemId">The GameItemId of the parent crate item.</param>
        /// <param name="parentBag">The ItemBag containing the crate item.</param>
        /// <param name="ownerUUID">Owner UUID for blueprint routing.</param>
        /// <param name="visitedCrateIds">Set of already-visited crate IDs for cycle detection.</param>
        /// <returns>Result containing nested crate IDs to cascade and import statistics.</returns>
        public CrateContentImportResult Import(
            AssetCrateContents crateContents,
            int crateGameItemId,
            ItemBag parentBag,
            string ownerUUID,
            HashSet<int> visitedCrateIds)
        {
            var result = new CrateContentImportResult();

            // Mark self as visited before processing contents (cycle detection)
            visitedCrateIds.Add(crateGameItemId);

            if (crateContents?.Cargo == null)
            {
                Log.Info("CrateContentImporter: crate GameItemId={0} has no cargo (null DTO or cargo).", crateGameItemId);
                return result;
            }

            var cargoList = crateContents.Cargo.ToList();
            result.TotalItems = cargoList.Count;
            Log.Info("CrateContentImporter: starting import for crate GameItemId={0}, cargo count={1}", crateGameItemId, cargoList.Count);

            var createdItems = new List<Item>();

            for (int i = 0; i < cargoList.Count; i++)
            {
                try
                {
                    var cargoItem = cargoList[i];
                    var mappedType = AssetMergeService.MapAssetTypeC(cargoItem.TypeC);
                    Log.Debug("CrateContentImporter: item[{0}] TypeC='{1}' mapped to ItemType={2}", i, cargoItem.TypeC, mappedType);

                    if (mappedType == ItemType.ItemTypeEnum.None)
                    {
                        Log.Warn("CrateContentImporter: unrecognized TypeC='{0}' for item[{1}] '{2}' in crate GameItemId={3}, assigning ItemType.None", cargoItem.TypeC, i, cargoItem.ResourceName, crateGameItemId);
                    }

                    var item = AssetMergeService.CreateAssetItem(cargoItem, mappedType);

                    // Blueprint dual-tracking: link to master blueprint list
                    if (string.Equals(cargoItem.TypeC, AssetTypeCodes.Blueprint, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            bool linked = _blueprintLinkageService.ProcessItem(cargoItem, item, ownerUUID);
                            if (linked)
                            {
                                result.BlueprintsLinked++;
                            }
                        }
                        catch (Exception bpEx)
                        {
                            Log.Warn("CrateContentImporter: blueprint linkage failed for item[{0}] '{1}': {2}", i, cargoItem.ResourceName, bpEx.Message);
                        }
                    }

                    // Nested crate detection: record for cascade, check for cycles
                    if (string.Equals(cargoItem.TypeC, AssetTypeCodes.Crate, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            int nestedCrateId = cargoItem.Id;
                            if (visitedCrateIds.Contains(nestedCrateId))
                            {
                                Log.Warn("CrateContentImporter: cycle detected — nested crate GameItemId={0} already visited, skipping cascade", nestedCrateId);
                            }
                            else
                            {
                                result.NestedCrateIds.Add(nestedCrateId);
                            }
                        }
                        catch (Exception nestedEx)
                        {
                            Log.Warn("CrateContentImporter: nested crate processing failed for item[{0}] '{1}': {2}", i, cargoItem.ResourceName, nestedEx.Message);
                        }
                    }

                    createdItems.Add(item);
                    result.Imported++;

                    if (result.CountsByType.ContainsKey(mappedType))
                    {
                        result.CountsByType[mappedType]++;
                    }
                    else
                    {
                        result.CountsByType[mappedType] = 1;
                    }
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    try
                    {
                        string itemName = (i < cargoList.Count) ? cargoList[i]?.ResourceName ?? "(unknown)" : "(unknown)";
                        Log.Error("CrateContentImporter: failed to process item[{0}] '{1}' in crate GameItemId={2}: {3}", i, itemName, crateGameItemId, ex.Message);
                        result.Errors.Add($"Item[{i}] '{itemName}': {ex.Message}");
                    }
                    catch
                    {
                        // Req 9 AC6: if error logging itself fails, continue processing without interruption
                    }
                }
            }

            // Locate or create the crate Item in the parent bag
            Item crateItem = null;
            foreach (var kvp in parentBag.Items)
            {
                if (kvp.Value.GameItemId == crateGameItemId)
                {
                    crateItem = kvp.Value;
                    break;
                }
            }

            if (crateItem == null)
            {
                crateItem = new Item(ItemType.ItemTypeEnum.Crate, "Crate")
                {
                    UUID = Guid.NewGuid().ToString(),
                    GameItemId = crateGameItemId,
                };
                parentBag.AddItem(crateItem);
                Log.Info("CrateContentImporter: created new crate Item UUID={0} for GameItemId={1}", crateItem.UUID, crateGameItemId);
            }

            // Replace Contents bag with new ItemBag containing all parsed items
            try
            {
                var contentsBag = new ItemBag();
                foreach (var item in createdItems)
                {
                    contentsBag.AddItem(item);
                }

                crateItem.Contents = contentsBag;
            }
            catch (Exception ex)
            {
                Log.Error("CrateContentImporter: failed to replace Contents bag for crate GameItemId={0}: {1}", crateGameItemId, ex.Message);
            }

            // Persist updated context after successful Contents population
            try
            {
                _playerContext.WriteContext();
            }
            catch (Exception writeEx)
            {
                Log.Error("CrateContentImporter: WriteContext failed for crate GameItemId={0}: {1} — in-memory state retained, will retry on next sync", crateGameItemId, writeEx.Message);
            }

            // Fire BlueprintDataChanged if any blueprints were linked
            if (result.BlueprintsLinked > 0)
            {
                _playerContext.OnBlueprintDataChanged(null);
            }

            // Completion summary logging (Req 9 AC2)
            try
            {
                var typeBreakdown = string.Join(", ", result.CountsByType.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                Log.Info(
                    "CrateContentImporter: completed import for crate GameItemId={0} — imported={1}, failed={2}, blueprints={3}, nested={4}, types=[{5}]",
                    crateGameItemId,
                    result.Imported,
                    result.Failed,
                    result.BlueprintsLinked,
                    result.NestedCrateIds.Count,
                    typeBreakdown);

                if (result.Errors.Count > 0)
                {
                    Log.Warn("CrateContentImporter: {0} error(s) during import of crate GameItemId={1}", result.Errors.Count, crateGameItemId);
                }
            }
            catch
            {
                // Req 9 AC6: if summary logging fails, do not halt processing
            }

            return result;
        }
    }
}
