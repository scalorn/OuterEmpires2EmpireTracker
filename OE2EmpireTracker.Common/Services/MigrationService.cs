// <copyright file="MigrationService.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using OE2EmpireTracker.Common.Interfaces;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Migrates all entity data from any source <see cref="IStorageBackend"/> to any destination.
    /// </summary>
    public class MigrationService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Migrates all data from source to destination.
        /// </summary>
        /// <param name="source">Source backend to read from.</param>
        /// <param name="destination">Destination backend to write to.</param>
        /// <param name="progress">Progress callback (invoked at least once per entity type).</param>
        /// <param name="characterUUIDs">
        /// Optional explicit list of character UUIDs to migrate.
        /// If null, discovers all characters from source via GetAllCharacterUUIDsAsync.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task representing the asynchronous migration operation.</returns>
        public async Task MigrateAsync(
            IStorageBackend source,
            IStorageBackend destination,
            IProgress<MigrationProgress> progress = null,
            IReadOnlyList<string> characterUUIDs = null,
            CancellationToken ct = default)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            // Discover characters
            var uuids = characterUUIDs ?? await source.GetAllCharacterUUIDsAsync().ConfigureAwait(false);
            Log.Info("Migration starting — {0} character(s) to migrate", uuids.Count);

            int totalProcessed = 0;

            void Report(string phase, string entityType)
            {
                totalProcessed++;
                progress?.Report(new MigrationProgress
                {
                    Phase = phase,
                    CurrentEntityType = entityType,
                    EntitiesProcessed = totalProcessed,
                });
            }

            // ── Server-global data migration ──

            // Factions
            var factions = await source.GetAllFactionsAsync().ConfigureAwait(false);
            foreach (var faction in factions)
            {
                ct.ThrowIfCancellationRequested();
                await destination.UpsertFactionAsync(faction).ConfigureAwait(false);
                Report("Global", "ServerFaction");
            }

            // Characters (server-level)
            var characters = await source.GetAllCharactersAsync().ConfigureAwait(false);
            foreach (var character in characters)
            {
                ct.ThrowIfCancellationRequested();
                await destination.UpsertCharacterAsync(character).ConfigureAwait(false);
                Report("Global", "ServerCharacter");
            }

            // Star Systems (bulk upsert)
            var starSystems = await source.GetAllStarSystemsAsync().ConfigureAwait(false);
            if (starSystems.Count > 0)
            {
                ct.ThrowIfCancellationRequested();
                await destination.UpsertStarSystemsAsync(starSystems).ConfigureAwait(false);
                foreach (var _ in starSystems)
                {
                    Report("Global", "StarSystem");
                }
            }

            // API Tokens
            var tokens = await source.GetAllTokensAsync().ConfigureAwait(false);
            foreach (var token in tokens)
            {
                ct.ThrowIfCancellationRequested();
                await destination.UpsertTokenAsync(token).ConfigureAwait(false);
                Report("Global", "ApiToken");
            }

            // Membership Actions (per faction)
            foreach (var faction in factions)
            {
                ct.ThrowIfCancellationRequested();
                var actions = await source.GetFactionActionsAsync(faction.UUID).ConfigureAwait(false);
                foreach (var action in actions)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertMembershipActionAsync(action).ConfigureAwait(false);
                    Report("Global", "MembershipAction");
                }
            }

            // Baseline data
            var constants = await source.GetBaselineGameConstantsAsync().ConfigureAwait(false);
            if (constants != null)
            {
                await destination.UpsertBaselineGameConstantsAsync(constants).ConfigureAwait(false);
                Report("Baseline", "GameConstants");
            }

            var blueprintTypes = await source.GetAllBlueprintTypesAsync().ConfigureAwait(false);
            await destination.UpsertBlueprintTypesAsync(blueprintTypes).ConfigureAwait(false);
            Report("Baseline", "BlueprintTypes");

            var shipClasses = await source.GetAllShipClassesAsync().ConfigureAwait(false);
            await destination.UpsertShipClassesAsync(shipClasses).ConfigureAwait(false);
            Report("Baseline", "ShipClasses");

            var techLevels = await source.GetAllTechLevelsAsync().ConfigureAwait(false);
            await destination.UpsertTechLevelsAsync(techLevels).ConfigureAwait(false);
            Report("Baseline", "TechLevels");

            var commodities = await source.GetAllCommoditiesAsync().ConfigureAwait(false);
            await destination.UpsertCommoditiesAsync(commodities).ConfigureAwait(false);
            Report("Baseline", "Commodities");

            var recipes = await source.GetAllRefiningRecipesAsync().ConfigureAwait(false);
            await destination.UpsertRefiningRecipesAsync(recipes).ConfigureAwait(false);
            Report("Baseline", "RefiningRecipes");

            var researchTimes = await source.GetAllResearchTimesAsync().ConfigureAwait(false);
            await destination.UpsertResearchTimesAsync(researchTimes).ConfigureAwait(false);
            Report("Baseline", "ResearchTimes");

            var propertyTypes = await source.GetAllPropertyTypeDefinitionsAsync().ConfigureAwait(false);
            await destination.UpsertPropertyTypeDefinitionsAsync(propertyTypes).ConfigureAwait(false);
            Report("Baseline", "PropertyTypeDefinitions");
            // TODO: Task 18.4/18.5 — per-character entity migration
            // TODO: Task 18.6 — permissions/intel/audit migration
            // TODO: Task 18.7 — count validation

            Log.Info("Migration complete — {0} entities processed", totalProcessed);
        }
    }
}
