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
            // ── Per-character entity migration ──
            foreach (var charUUID in uuids)
            {
                ct.ThrowIfCancellationRequested();
                string phase = $"Character {charUUID}";

                // Colonies
                var colonies = await source.GetAllColoniesAsync(charUUID).ConfigureAwait(false);
                foreach (var entity in colonies)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertColonyAsync(charUUID, entity).ConfigureAwait(false);
                    Report(phase, "Colony");
                }

                // Blueprints
                var blueprints = await source.GetAllBlueprintsAsync(charUUID).ConfigureAwait(false);
                foreach (var entity in blueprints)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertBlueprintAsync(charUUID, entity).ConfigureAwait(false);
                    Report(phase, "Blueprint");
                }

                // Surveys
                var surveys = await source.GetAllSurveysAsync(charUUID).ConfigureAwait(false);
                foreach (var entity in surveys)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertSurveyAsync(charUUID, entity).ConfigureAwait(false);
                    Report(phase, "Survey");
                }

                // PlayerProfiles
                var profiles = await source.GetAllPlayerProfilesAsync(charUUID).ConfigureAwait(false);
                foreach (var entity in profiles)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertPlayerProfileAsync(charUUID, entity).ConfigureAwait(false);
                    Report(phase, "PlayerProfile");
                }

                // DeliveryRoutes
                var routes = await source.GetAllDeliveryRoutesAsync(charUUID).ConfigureAwait(false);
                foreach (var entity in routes)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertDeliveryRouteAsync(charUUID, entity).ConfigureAwait(false);
                    Report(phase, "DeliveryRoute");
                }

                // DeliveryPlans
                var plans = await source.GetAllDeliveryPlansAsync(charUUID).ConfigureAwait(false);
                foreach (var entity in plans)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertDeliveryPlanAsync(charUUID, entity).ConfigureAwait(false);
                    Report(phase, "DeliveryPlan");
                }

                // PricingPlans
                var pricingPlans = await source.GetAllPricingPlansAsync(charUUID).ConfigureAwait(false);
                foreach (var entity in pricingPlans)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertPricingPlanAsync(charUUID, entity).ConfigureAwait(false);
                    Report(phase, "PricingPlan");
                }

                // BuildPlans
                var buildPlans = await source.GetAllBuildPlansAsync(charUUID).ConfigureAwait(false);
                foreach (var entity in buildPlans)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertBuildPlanAsync(charUUID, entity).ConfigureAwait(false);
                    Report(phase, "BuildPlan");
                }

                // ShipTemplates
                var shipTemplates = await source.GetAllShipTemplatesAsync(charUUID).ConfigureAwait(false);
                foreach (var entity in shipTemplates)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertShipTemplateAsync(charUUID, entity).ConfigureAwait(false);
                    Report(phase, "ShipTemplate");
                }

                // Ships
                var ships = await source.GetAllShipsAsync(charUUID).ConfigureAwait(false);
                foreach (var entity in ships)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertShipAsync(charUUID, entity).ConfigureAwait(false);
                    Report(phase, "Ship");
                }

                // Stations
                var stations = await source.GetAllStationsAsync(charUUID).ConfigureAwait(false);
                foreach (var entity in stations)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertStationAsync(charUUID, entity).ConfigureAwait(false);
                    Report(phase, "Station");
                }

                // MarketListings
                var marketListings = await source.GetAllMarketListingsAsync(charUUID).ConfigureAwait(false);
                foreach (var entity in marketListings)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertMarketListingAsync(charUUID, entity).ConfigureAwait(false);
                    Report(phase, "MarketListing");
                }

                // MarketTransactions
                var marketTransactions = await source.GetAllMarketTransactionsAsync(charUUID).ConfigureAwait(false);
                foreach (var entity in marketTransactions)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertMarketTransactionAsync(charUUID, entity).ConfigureAwait(false);
                    Report(phase, "MarketTransaction");
                }

                // StockPlans
                var stockPlans = await source.GetAllStockPlansAsync(charUUID).ConfigureAwait(false);
                foreach (var entity in stockPlans)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertStockPlanAsync(charUUID, entity).ConfigureAwait(false);
                    Report(phase, "StockPlan");
                }

                // StockProfiles
                var stockProfiles = await source.GetAllStockProfilesAsync(charUUID).ConfigureAwait(false);
                foreach (var entity in stockProfiles)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertStockProfileAsync(charUUID, entity).ConfigureAwait(false);
                    Report(phase, "StockProfile");
                }

                // SupplyChains
                var supplyChains = await source.GetAllSupplyChainsAsync(charUUID).ConfigureAwait(false);
                foreach (var entity in supplyChains)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertSupplyChainAsync(charUUID, entity).ConfigureAwait(false);
                    Report(phase, "SupplyChain");
                }

                // WarehouseOverflowRules
                var overflowRules = await source.GetAllWarehouseOverflowRulesAsync(charUUID).ConfigureAwait(false);
                foreach (var entity in overflowRules)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertWarehouseOverflowRuleAsync(charUUID, entity).ConfigureAwait(false);
                    Report(phase, "WarehouseOverflowRule");
                }

                // Asteroids
                var asteroids = await source.GetAllAsteroidsAsync(charUUID).ConfigureAwait(false);
                foreach (var entity in asteroids)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertAsteroidAsync(charUUID, entity).ConfigureAwait(false);
                    Report(phase, "Asteroid");
                }

                // BankingTransactions
                var bankingTransactions = await source.GetAllBankingTransactionsAsync(charUUID).ConfigureAwait(false);
                foreach (var entity in bankingTransactions)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertBankingTransactionAsync(charUUID, entity).ConfigureAwait(false);
                    Report(phase, "BankingTransaction");
                }

                // MailMessages
                var mailMessages = await source.GetAllMailMessagesAsync(charUUID).ConfigureAwait(false);
                foreach (var entity in mailMessages)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertMailMessageAsync(charUUID, entity).ConfigureAwait(false);
                    Report(phase, "MailMessage");
                }

                // SharingRules (bulk: read all, upsert all)
                var sharingRules = await source.GetSharingRulesForCharacterAsync(charUUID).ConfigureAwait(false);
                if (sharingRules.Count > 0)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertSharingRulesAsync(charUUID, sharingRules).ConfigureAwait(false);
                    Report(phase, "SharingRules");
                }

                // CharacterPreferences (single object)
                var prefs = await source.GetCharacterPreferencesAsync(charUUID).ConfigureAwait(false);
                if (prefs != null)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.UpsertCharacterPreferencesAsync(prefs).ConfigureAwait(false);
                    Report(phase, "CharacterPreferences");
                }
            }

            // TODO: Task 18.6 — permissions/intel/audit migration
            // TODO: Task 18.7 — count validation

            Log.Info("Migration complete — {0} entities processed", totalProcessed);
        }
    }
}
