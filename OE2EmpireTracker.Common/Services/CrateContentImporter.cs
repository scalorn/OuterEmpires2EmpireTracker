// <copyright file="CrateContentImporter.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NLog;
using OE2EmpireTracker.Client;
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
        /// Imports crate contents from a raw JSON response into the local data model.
        /// Returns work items for any nested crates discovered.
        /// </summary>
        /// <param name="json">Raw JSON from GetAssetCrateAsync (GameApiAssetDetailResponse shape).</param>
        /// <param name="crateGameItemId">The GameItemId of the parent crate item.</param>
        /// <param name="parentBag">The ItemBag containing the crate item (ship cargo, station hold, etc.).</param>
        /// <param name="ownerUUID">Owner UUID for blueprint routing.</param>
        /// <param name="visitedCrateIds">Set of already-visited crate IDs for cycle detection.</param>
        /// <returns>Result containing nested crate IDs to cascade and import statistics.</returns>
        public CrateContentImportResult Import(
            string json,
            int crateGameItemId,
            ItemBag parentBag,
            string ownerUUID,
            HashSet<int> visitedCrateIds)
        {
            var result = new CrateContentImportResult();

            GameApiAssetDetailResponse response;
            try
            {
                response = JsonConvert.DeserializeObject<GameApiAssetDetailResponse>(json);
            }
            catch (JsonException ex)
            {
                Log.Error("CrateContentImporter: malformed JSON for crate GameItemId={0}: {1}", crateGameItemId, ex.Message);
                result.Success = false;
                result.Errors.Add($"Malformed JSON: {ex.Message}");
                return result;
            }

            if (response?.Cargo == null)
            {
                Log.Info("CrateContentImporter: crate GameItemId={0} has no cargo (null response or cargo).", crateGameItemId);
                return result;
            }

            result.TotalItems = response.Cargo.Count;
            Log.Info("CrateContentImporter: starting import for crate GameItemId={0}, cargo count={1}", crateGameItemId, response.Cargo.Count);

            var createdItems = new List<Item>();

            for (int i = 0; i < response.Cargo.Count; i++)
            {
                try
                {
                    var cargoItem = response.Cargo[i];
                    var mappedType = AssetMergeService.MapAssetTypeC(cargoItem.TypeC);
                    Log.Debug("CrateContentImporter: item[{0}] TypeC='{1}' mapped to ItemType={2}", i, cargoItem.TypeC, mappedType);

                    if (mappedType == ItemType.ItemTypeEnum.None)
                    {
                        Log.Warn("CrateContentImporter: unrecognized TypeC='{0}' for item[{1}] '{2}' in crate GameItemId={3}, assigning ItemType.None", cargoItem.TypeC, i, cargoItem.ResourceName, crateGameItemId);
                    }

                    var item = AssetMergeService.CreateAssetItem(cargoItem, mappedType);
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
                    string itemName = (i < response.Cargo.Count) ? response.Cargo[i]?.ResourceName ?? "(unknown)" : "(unknown)";
                    Log.Error("CrateContentImporter: failed to process item[{0}] '{1}' in crate GameItemId={2}: {3}", i, itemName, crateGameItemId, ex.Message);
                    result.Errors.Add($"Item[{i}] '{itemName}': {ex.Message}");
                }
            }

            return result;
        }
    }
}
