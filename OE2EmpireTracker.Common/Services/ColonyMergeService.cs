// <copyright file="ColonyMergeService.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;

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
        /// Processes a single API colony: finds a local match or creates a new colony.
        /// </summary>
        private static void ProcessSingleColony(
            GameApiColonyListItem apiColony,
            List<Colony> localColonies,
            string ownerUUID,
            ColonyMergeResult result)
        {
            // Find local match by PlanetName + SystemName (case-insensitive, same owner)
            var match = localColonies.FirstOrDefault(c =>
                string.Equals(c.OwnerUUID, ownerUUID, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.PlanetName, apiColony.SystemObjectName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.SystemName, apiColony.SystemName, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
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
                var newColony = CreateColonyFromApi(apiColony, ownerUUID);
                localColonies.Add(newColony);
                result.ColonyIdToUUIDMap[apiColony.ColonyId] = newColony.UUID;
                result.Created++;
            }
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
        /// Stub implementation — full field merge logic will be added in task 15.2.
        /// </summary>
        /// <param name="local">The local colony to update.</param>
        /// <param name="apiColony">The API colony data.</param>
        /// <returns>True if any field was changed; otherwise false.</returns>
        private static bool MergeAllColonyFields(Colony local, GameApiColonyListItem apiColony)
        {
            // Stub: full implementation in task 15.2
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
