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
            try
            {
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
                    foreach (var system in starSystems)
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
            }
            catch (NotSupportedException)
            {
                Log.Info("Source backend does not support server-global entities — skipping");
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

                // SharingRules and CharacterPreferences
                try
                {
                    var sharingRules = await source.GetSharingRulesForCharacterAsync(charUUID).ConfigureAwait(false);
                    if (sharingRules.Count > 0)
                    {
                        ct.ThrowIfCancellationRequested();
                        await destination.UpsertSharingRulesAsync(charUUID, sharingRules).ConfigureAwait(false);
                        Report(phase, "SharingRules");
                    }

                    var prefs = await source.GetCharacterPreferencesAsync(charUUID).ConfigureAwait(false);
                    if (prefs != null)
                    {
                        ct.ThrowIfCancellationRequested();
                        await destination.UpsertCharacterPreferencesAsync(prefs).ConfigureAwait(false);
                        Report(phase, "CharacterPreferences");
                    }
                }
                catch (NotSupportedException)
                {
                    Log.Info("Source backend does not support sharing rules/character preferences for {0} — skipping", charUUID);
                }
            }

            // ── Faction permission entities ──
            try
            {
                var permFactions = await source.GetAllFactionsAsync().ConfigureAwait(false);
                foreach (var faction in permFactions)
                {
                    ct.ThrowIfCancellationRequested();
                    string factionPhase = $"FactionPerms {faction.UUID}";

                    var factionCaps = await source.GetFactionCapabilitiesAsync(faction.UUID).ConfigureAwait(false);
                    foreach (var cap in factionCaps)
                    {
                        ct.ThrowIfCancellationRequested();
                        await destination.UpsertFactionCapabilityAsync(cap).ConfigureAwait(false);
                        Report(factionPhase, "FactionCapability");
                    }

                    var clearanceLevels = await source.GetFactionClearanceLevelsAsync(faction.UUID).ConfigureAwait(false);
                    foreach (var level in clearanceLevels)
                    {
                        ct.ThrowIfCancellationRequested();
                        await destination.UpsertFactionClearanceLevelAsync(level).ConfigureAwait(false);
                        Report(factionPhase, "FactionClearanceLevel");
                    }

                    var groups = await source.GetFactionGroupsAsync(faction.UUID).ConfigureAwait(false);
                    foreach (var group in groups)
                    {
                        ct.ThrowIfCancellationRequested();
                        await destination.UpsertFactionGroupAsync(group).ConfigureAwait(false);
                        Report(factionPhase, "FactionPermissionGroup");

                        var groupCaps = await source.GetFactionGroupCapabilitiesAsync(group.UUID).ConfigureAwait(false);
                        foreach (var gc in groupCaps)
                        {
                            ct.ThrowIfCancellationRequested();
                            await destination.AddFactionGroupCapabilityAsync(gc).ConfigureAwait(false);
                            Report(factionPhase, "FactionGroupCapability");
                        }

                        var groupRules = await source.GetFactionGroupSharingRulesAsync(group.UUID).ConfigureAwait(false);
                        foreach (var rule in groupRules)
                        {
                            ct.ThrowIfCancellationRequested();
                            await destination.UpsertFactionGroupSharingRuleAsync(rule).ConfigureAwait(false);
                            Report(factionPhase, "FactionGroupSharingRule");
                        }
                    }

                    var memberPermsList = await source.GetAllFactionMembersPermissionsAsync(faction.UUID).ConfigureAwait(false);
                    foreach (var memberPerms in memberPermsList)
                    {
                        ct.ThrowIfCancellationRequested();
                        await destination.UpsertFactionMemberPermissionsAsync(memberPerms).ConfigureAwait(false);
                        Report(factionPhase, "FactionMemberPermissions");

                        var memberCaps = await source.GetFactionMemberCapabilitiesAsync(faction.UUID, memberPerms.CharacterUUID).ConfigureAwait(false);
                        foreach (var mc in memberCaps)
                        {
                            ct.ThrowIfCancellationRequested();
                            await destination.AddFactionMemberCapabilityAsync(mc).ConfigureAwait(false);
                            Report(factionPhase, "FactionMemberCapability");
                        }
                    }
                }

                // ── Character permission entities ──
                foreach (var charUUID in uuids)
                {
                    ct.ThrowIfCancellationRequested();
                    string charPermPhase = $"CharPerms {charUUID}";

                    var charCaps = await source.GetCharacterCapabilitiesAsync(charUUID).ConfigureAwait(false);
                    foreach (var cap in charCaps)
                    {
                        ct.ThrowIfCancellationRequested();
                        await destination.UpsertCharacterCapabilityAsync(cap).ConfigureAwait(false);
                        Report(charPermPhase, "CharacterCapability");
                    }

                    var charLevels = await source.GetCharacterClearanceLevelsAsync(charUUID).ConfigureAwait(false);
                    foreach (var level in charLevels)
                    {
                        ct.ThrowIfCancellationRequested();
                        await destination.UpsertCharacterClearanceLevelAsync(level).ConfigureAwait(false);
                        Report(charPermPhase, "CharacterClearanceLevel");
                    }

                    var charGroups = await source.GetCharacterGroupsAsync(charUUID).ConfigureAwait(false);
                    foreach (var cg in charGroups)
                    {
                        ct.ThrowIfCancellationRequested();
                        await destination.UpsertCharacterGroupAsync(cg).ConfigureAwait(false);
                        Report(charPermPhase, "CharacterPermissionGroup");

                        var cgCaps = await source.GetCharacterGroupCapabilitiesAsync(cg.UUID).ConfigureAwait(false);
                        foreach (var cgc in cgCaps)
                        {
                            ct.ThrowIfCancellationRequested();
                            await destination.AddCharacterGroupCapabilityAsync(cgc).ConfigureAwait(false);
                            Report(charPermPhase, "CharacterGroupCapability");
                        }

                        var cgRules = await source.GetCharacterGroupSharingRulesAsync(cg.UUID).ConfigureAwait(false);
                        foreach (var cgr in cgRules)
                        {
                            ct.ThrowIfCancellationRequested();
                            await destination.UpsertCharacterGroupSharingRuleAsync(cgr).ConfigureAwait(false);
                            Report(charPermPhase, "CharacterGroupSharingRule");
                        }
                    }

                    var grantees = await source.GetCharacterGranteesAsync(charUUID).ConfigureAwait(false);
                    foreach (var grantee in grantees)
                    {
                        ct.ThrowIfCancellationRequested();
                        await destination.UpsertCharacterGranteePermissionsAsync(grantee).ConfigureAwait(false);
                        Report(charPermPhase, "CharacterGranteePermissions");

                        var granteeCaps = await source.GetCharacterGranteeCapabilitiesAsync(charUUID, grantee.GranteeUUID).ConfigureAwait(false);
                        foreach (var gtc in granteeCaps)
                        {
                            ct.ThrowIfCancellationRequested();
                            await destination.AddCharacterGranteeCapabilityAsync(gtc).ConfigureAwait(false);
                            Report(charPermPhase, "CharacterGranteeCapability");
                        }
                    }
                }

                // ── Intel migration ──
                foreach (var charUUID in uuids)
                {
                    ct.ThrowIfCancellationRequested();
                    string intelPhase = $"Intel {charUUID}";

                    var comments = await source.GetIntelCommentsForTargetAsync(charUUID).ConfigureAwait(false);
                    foreach (var comment in comments)
                    {
                        ct.ThrowIfCancellationRequested();
                        await destination.UpsertIntelCommentAsync(comment).ConfigureAwait(false);
                        Report(intelPhase, "IntelComment");

                        var shares = await source.GetIntelSharesForCommentAsync(comment.UUID).ConfigureAwait(false);
                        foreach (var share in shares)
                        {
                            ct.ThrowIfCancellationRequested();
                            await destination.UpsertIntelShareAsync(share).ConfigureAwait(false);
                            Report(intelPhase, "IntelCommentFactionShare");
                        }
                    }
                }

                // ── Audit entries ──
                var auditEntries = await source.GetPermissionAuditEntriesAsync().ConfigureAwait(false);
                foreach (var entry in auditEntries)
                {
                    ct.ThrowIfCancellationRequested();
                    await destination.AppendPermissionAuditEntryAsync(entry).ConfigureAwait(false);
                    Report("Audit", "PermissionAuditEntry");
                }
            }
            catch (NotSupportedException)
            {
                Log.Info("Source backend does not support permissions/intel/audit entities — skipping");
            }

            try
            {
                await ValidateCountsAsync(source, destination, uuids, ct).ConfigureAwait(false);
            }
            catch (NotSupportedException)
            {
                Log.Info("Count validation skipped — source backend does not support all entity queries");
            }

            Log.Info("Migration complete — {0} entities processed", totalProcessed);
        }

        /// <summary>
        /// Validates that entity counts match between source and destination after migration.
        /// </summary>
        /// <param name="source">Source backend to read from.</param>
        /// <param name="destination">Destination backend to compare against.</param>
        /// <param name="characterUUIDs">Character UUIDs that were migrated.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task representing the asynchronous validation operation.</returns>
        private async Task ValidateCountsAsync(
            IStorageBackend source,
            IStorageBackend destination,
            IReadOnlyList<string> characterUUIDs,
            CancellationToken ct)
        {
            var mismatches = new Dictionary<string, (int Expected, int Actual)>();

            // ── Global entity counts ──
            ct.ThrowIfCancellationRequested();
            int srcFactions = (await source.GetAllFactionsAsync().ConfigureAwait(false)).Count;
            int dstFactions = (await destination.GetAllFactionsAsync().ConfigureAwait(false)).Count;
            if (srcFactions != dstFactions)
            {
                mismatches["ServerFaction"] = (srcFactions, dstFactions);
            }

            ct.ThrowIfCancellationRequested();
            int srcSystems = (await source.GetAllStarSystemsAsync().ConfigureAwait(false)).Count;
            int dstSystems = (await destination.GetAllStarSystemsAsync().ConfigureAwait(false)).Count;
            if (srcSystems != dstSystems)
            {
                mismatches["StarSystem"] = (srcSystems, dstSystems);
            }

            ct.ThrowIfCancellationRequested();
            int srcTokens = (await source.GetAllTokensAsync().ConfigureAwait(false)).Count;
            int dstTokens = (await destination.GetAllTokensAsync().ConfigureAwait(false)).Count;
            if (srcTokens != dstTokens)
            {
                mismatches["ApiToken"] = (srcTokens, dstTokens);
            }

            // ── Per-character entity counts (summed across all characters) ──
            int srcColonies = 0, dstColonies = 0;
            int srcBlueprints = 0, dstBlueprints = 0;
            int srcBanking = 0, dstBanking = 0;
            int srcMail = 0, dstMail = 0;
            int srcShips = 0, dstShips = 0;

            foreach (var uuid in characterUUIDs)
            {
                ct.ThrowIfCancellationRequested();
                srcColonies += (await source.GetAllColoniesAsync(uuid).ConfigureAwait(false)).Count;
                dstColonies += (await destination.GetAllColoniesAsync(uuid).ConfigureAwait(false)).Count;

                srcBlueprints += (await source.GetAllBlueprintsAsync(uuid).ConfigureAwait(false)).Count;
                dstBlueprints += (await destination.GetAllBlueprintsAsync(uuid).ConfigureAwait(false)).Count;

                srcBanking += (await source.GetAllBankingTransactionsAsync(uuid).ConfigureAwait(false)).Count;
                dstBanking += (await destination.GetAllBankingTransactionsAsync(uuid).ConfigureAwait(false)).Count;

                srcMail += (await source.GetAllMailMessagesAsync(uuid).ConfigureAwait(false)).Count;
                dstMail += (await destination.GetAllMailMessagesAsync(uuid).ConfigureAwait(false)).Count;

                srcShips += (await source.GetAllShipsAsync(uuid).ConfigureAwait(false)).Count;
                dstShips += (await destination.GetAllShipsAsync(uuid).ConfigureAwait(false)).Count;
            }

            if (srcColonies != dstColonies)
            {
                mismatches["Colony"] = (srcColonies, dstColonies);
            }

            if (srcBlueprints != dstBlueprints)
            {
                mismatches["Blueprint"] = (srcBlueprints, dstBlueprints);
            }

            if (srcBanking != dstBanking)
            {
                mismatches["BankingTransaction"] = (srcBanking, dstBanking);
            }

            if (srcMail != dstMail)
            {
                mismatches["MailMessage"] = (srcMail, dstMail);
            }

            if (srcShips != dstShips)
            {
                mismatches["Ship"] = (srcShips, dstShips);
            }

            if (mismatches.Count > 0)
            {
                throw new MigrationValidationException(mismatches);
            }
        }
    }
}
