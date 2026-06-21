// -----------------------------------------------------------------------
// <copyright file="PostgresBackend.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Npgsql;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using Polly;
using Polly.Retry;

namespace OE2EmpireTracker.Common.Storage
{
    /// <summary>
    /// IStorageBackend implementation that persists all data in a PostgreSQL
    /// database using Npgsql with Polly retry policies for transient fault handling.
    /// </summary>
    internal class PostgresBackend : IStorageBackend
    {
        private const int CurrentSchemaVersion = 1;
        private const int MaxRetries = 3;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Schema DDL concatenated from tasks 7.2-7.3. ExecuteSchema runs this
        /// against a fresh database.
        /// </summary>
        private static readonly string SchemaDdl = string.Empty;

        private readonly string _connectionString;
        private readonly RetryPolicy _retryPolicy;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresBackend"/> class.
        /// </summary>
        /// <param name="config">Configuration containing the Postgres connection string.</param>
        public PostgresBackend(StorageBackendConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _connectionString = config.ConnectionString ?? string.Empty;

            _retryPolicy = Policy
                .Handle<NpgsqlException>()
                .Or<TimeoutException>()
                .WaitAndRetry(
                    MaxRetries,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        Log.Warn(
                            "Postgres operation failed (attempt {0}/{1}), retrying in {2}s: {3}",
                            retryCount,
                            MaxRetries,
                            timeSpan.TotalSeconds,
                            exception.Message);
                    });
        }

        // ═══════════════════════════════════════════════════════════
        // Lifecycle
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task InitializeAsync(CancellationToken ct = default)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "CREATE TABLE IF NOT EXISTS _metadata (Key TEXT PRIMARY KEY, Value TEXT NOT NULL);";
                        cmd.ExecuteNonQuery();
                    }

                    int version = GetSchemaVersion(conn);
                    if (version == 0)
                    {
                        ExecuteSchema(conn);
                        SetSchemaVersion(conn, CurrentSchemaVersion);
                        Log.Info("Postgres database initialized with schema version {0}", CurrentSchemaVersion);
                    }
                    else
                    {
                        Log.Debug("Postgres database already at schema version {0}", version);
                    }
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> ValidateConnectionAsync(CancellationToken ct = default)
        {
            try
            {
                _retryPolicy.Execute(() =>
                {
                    using (var conn = OpenConnection())
                    {
                    }
                });

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Postgres connection validation failed");
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc/>
        public StorageInfo GetStorageInfo()
        {
            return new StorageInfo
            {
                BackendType = "Postgres",
                Location = _connectionString,
            };
        }

        // ═══════════════════════════════════════════════════════════
        // Server Factions
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<ServerFaction> GetFactionAsync(string uuid)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ServerFaction>> GetAllFactionsAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertFactionAsync(ServerFaction faction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteFactionAsync(string uuid)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Server Characters
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<ServerCharacter> GetCharacterAsync(string uuid)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ServerCharacter>> GetAllCharactersAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertCharacterAsync(ServerCharacter character)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteCharacterAsync(string uuid)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Global Data
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<string> GetGlobalDataAsync(string dataType)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertGlobalDataAsync(string dataType, string json)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Star Systems
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<StarSystem>> GetAllStarSystemsAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertStarSystemsAsync(IReadOnlyList<StarSystem> systems)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Colony Summaries
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<ColonySummary>> GetColonySummariesForSystemAsync(int systemId)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // API Tokens
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<ApiToken> FindTokenByHashAsync(string tokenHash)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ApiToken>> GetAllTokensAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertTokenAsync(ApiToken token)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteTokenAsync(string id)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Membership Actions
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<MembershipAction>> GetFactionActionsAsync(string factionUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertMembershipActionAsync(MembershipAction action)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteMembershipActionAsync(string id)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteExpiredActionsAsync(DateTime cutoff)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Sharing Rules
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<SharingRule>> GetSharingRulesForCharacterAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertSharingRulesAsync(string characterUUID, IReadOnlyList<SharingRule> rules)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Character Preferences
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<CharacterPreferences> GetCharacterPreferencesAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertCharacterPreferencesAsync(CharacterPreferences prefs)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Colony
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Colony>> GetAllColoniesAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Colony> GetColonyAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertColonyAsync(string characterUUID, Colony entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteColonyAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Blueprint
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Blueprint>> GetAllBlueprintsAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Blueprint> GetBlueprintAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertBlueprintAsync(string characterUUID, Blueprint entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteBlueprintAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Survey
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Survey>> GetAllSurveysAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Survey> GetSurveyAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertSurveyAsync(string characterUUID, Survey entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteSurveyAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — PlayerProfile
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<PlayerProfile>> GetAllPlayerProfilesAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<PlayerProfile> GetPlayerProfileAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertPlayerProfileAsync(string characterUUID, PlayerProfile entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeletePlayerProfileAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — DeliveryRoute
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<DeliveryRoute>> GetAllDeliveryRoutesAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<DeliveryRoute> GetDeliveryRouteAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertDeliveryRouteAsync(string characterUUID, DeliveryRoute entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteDeliveryRouteAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — DeliveryPlan
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<DeliveryPlan>> GetAllDeliveryPlansAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<DeliveryPlan> GetDeliveryPlanAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertDeliveryPlanAsync(string characterUUID, DeliveryPlan entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteDeliveryPlanAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Ship
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Ship>> GetAllShipsAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Ship> GetShipAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertShipAsync(string characterUUID, Ship entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteShipAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — ShipTemplate
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<ShipTemplate>> GetAllShipTemplatesAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<ShipTemplate> GetShipTemplateAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertShipTemplateAsync(string characterUUID, ShipTemplate entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteShipTemplateAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — MarketListing
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<MarketListing>> GetAllMarketListingsAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<MarketListing> GetMarketListingAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertMarketListingAsync(string characterUUID, MarketListing entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteMarketListingAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — MarketTransaction
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<MarketTransaction>> GetAllMarketTransactionsAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<MarketTransaction> GetMarketTransactionAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertMarketTransactionAsync(string characterUUID, MarketTransaction entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteMarketTransactionAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — PricingPlan
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<PricingPlan>> GetAllPricingPlansAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<PricingPlan> GetPricingPlanAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertPricingPlanAsync(string characterUUID, PricingPlan entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeletePricingPlanAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — StockPlan
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<StockPlan>> GetAllStockPlansAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<StockPlan> GetStockPlanAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertStockPlanAsync(string characterUUID, StockPlan entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteStockPlanAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — StockProfile
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<StockProfile>> GetAllStockProfilesAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<StockProfile> GetStockProfileAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertStockProfileAsync(string characterUUID, StockProfile entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteStockProfileAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — BuildPlan
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<BuildPlan>> GetAllBuildPlansAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<BuildPlan> GetBuildPlanAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertBuildPlanAsync(string characterUUID, BuildPlan entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteBuildPlanAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — SupplyChain
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<SupplyChain>> GetAllSupplyChainsAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<SupplyChain> GetSupplyChainAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertSupplyChainAsync(string characterUUID, SupplyChain entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteSupplyChainAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Asteroid
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Asteroid>> GetAllAsteroidsAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Asteroid> GetAsteroidAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertAsteroidAsync(string characterUUID, Asteroid entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteAsteroidAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Station
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Station>> GetAllStationsAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Station> GetStationAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertStationAsync(string characterUUID, Station entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteStationAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Faction (contacts)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Faction>> GetAllFactionsForCharacterAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Faction> GetFactionForCharacterAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertFactionForCharacterAsync(string characterUUID, Faction entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteFactionForCharacterAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — ExternalCharacter
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<ExternalCharacter>> GetAllExternalCharactersAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<ExternalCharacter> GetExternalCharacterAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertExternalCharacterAsync(string characterUUID, ExternalCharacter entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteExternalCharacterAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — WarehouseOverflowRule
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<WarehouseOverflowRule>> GetAllWarehouseOverflowRulesAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<WarehouseOverflowRule> GetWarehouseOverflowRuleAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertWarehouseOverflowRuleAsync(string characterUUID, WarehouseOverflowRule entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteWarehouseOverflowRuleAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — MailMessage
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<MailMessage>> GetAllMailMessagesAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<MailMessage> GetMailMessageAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertMailMessageAsync(string characterUUID, MailMessage entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteMailMessageAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — BankingTransaction
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<BankingTransaction>> GetAllBankingTransactionsAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<BankingTransaction> GetBankingTransactionAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertBankingTransactionAsync(string characterUUID, BankingTransaction entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteBankingTransactionAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Faction Permission Entities
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionCapability>> GetFactionCapabilitiesAsync(string factionUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertFactionCapabilityAsync(FactionCapability capability)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteFactionCapabilityAsync(string factionUUID, string capabilityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionClearanceLevel>> GetFactionClearanceLevelsAsync(string factionUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertFactionClearanceLevelAsync(FactionClearanceLevel level)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteFactionClearanceLevelAsync(string factionUUID, string levelUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionPermissionGroup>> GetFactionGroupsAsync(string factionUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<FactionPermissionGroup> GetFactionGroupAsync(string factionUUID, string groupUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertFactionGroupAsync(FactionPermissionGroup group)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteFactionGroupAsync(string factionUUID, string groupUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionGroupCapability>> GetFactionGroupCapabilitiesAsync(string groupUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task AddFactionGroupCapabilityAsync(FactionGroupCapability item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task RemoveFactionGroupCapabilityAsync(string groupUUID, string capabilityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionGroupSharingRule>> GetFactionGroupSharingRulesAsync(string groupUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertFactionGroupSharingRuleAsync(FactionGroupSharingRule rule)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteFactionGroupSharingRuleAsync(string groupUUID, string ruleUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<FactionMemberPermissions> GetFactionMemberPermissionsAsync(string factionUUID, string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertFactionMemberPermissionsAsync(FactionMemberPermissions perms)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionMemberPermissions>> GetAllFactionMembersPermissionsAsync(string factionUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionMemberCapability>> GetFactionMemberCapabilitiesAsync(string factionUUID, string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task AddFactionMemberCapabilityAsync(FactionMemberCapability item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task RemoveFactionMemberCapabilityAsync(string factionUUID, string characterUUID, string capabilityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Character Permission Entities
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterCapability>> GetCharacterCapabilitiesAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertCharacterCapabilityAsync(CharacterCapability capability)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteCharacterCapabilityAsync(string characterUUID, string capabilityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterClearanceLevel>> GetCharacterClearanceLevelsAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertCharacterClearanceLevelAsync(CharacterClearanceLevel level)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteCharacterClearanceLevelAsync(string characterUUID, string levelUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterPermissionGroup>> GetCharacterGroupsAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CharacterPermissionGroup> GetCharacterGroupAsync(string characterUUID, string groupUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertCharacterGroupAsync(CharacterPermissionGroup group)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteCharacterGroupAsync(string characterUUID, string groupUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGroupCapability>> GetCharacterGroupCapabilitiesAsync(string groupUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task AddCharacterGroupCapabilityAsync(CharacterGroupCapability item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task RemoveCharacterGroupCapabilityAsync(string groupUUID, string capabilityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGroupSharingRule>> GetCharacterGroupSharingRulesAsync(string groupUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertCharacterGroupSharingRuleAsync(CharacterGroupSharingRule rule)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteCharacterGroupSharingRuleAsync(string groupUUID, string ruleUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGranteePermissions>> GetCharacterGranteesAsync(string ownerCharacterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertCharacterGranteePermissionsAsync(CharacterGranteePermissions perms)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteCharacterGranteePermissionsAsync(string ownerCharacterUUID, string granteeUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGranteeCapability>> GetCharacterGranteeCapabilitiesAsync(string ownerCharacterUUID, string granteeUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task AddCharacterGranteeCapabilityAsync(CharacterGranteeCapability item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task RemoveCharacterGranteeCapabilityAsync(string ownerCharacterUUID, string granteeUUID, string capabilityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Intel
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelComment>> GetIntelCommentsForTargetAsync(string targetCharacterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IntelComment> GetIntelCommentAsync(string commentUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertIntelCommentAsync(IntelComment comment)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteIntelCommentAsync(string commentUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForCommentAsync(string commentUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForFactionAsync(string factionUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertIntelShareAsync(IntelCommentFactionShare share)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteIntelShareAsync(string shareUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Audit
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<PermissionAuditEntry>> GetPermissionAuditEntriesAsync(DateTime? startDate = null, DateTime? endDate = null, PermissionActionType? actionType = null, string actorUUID = null, string targetUUID = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task AppendPermissionAuditEntryAsync(PermissionAuditEntry entry)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteExpiredAuditEntriesAsync(DateTime cutoff)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Baseline / Global Lookup Data
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<BaselineGameConstants> GetBaselineGameConstantsAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertBaselineGameConstantsAsync(BaselineGameConstants constants)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<BlueprintType>> GetAllBlueprintTypesAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertBlueprintTypesAsync(IReadOnlyList<BlueprintType> types)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ShipClass>> GetAllShipClassesAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertShipClassesAsync(IReadOnlyList<ShipClass> classes)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<TechLevel>> GetAllTechLevelsAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertTechLevelsAsync(IReadOnlyList<TechLevel> levels)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<Commodity>> GetAllCommoditiesAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertCommoditiesAsync(IReadOnlyList<Commodity> commodities)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<RefiningRecipe>> GetAllRefiningRecipesAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertRefiningRecipesAsync(IReadOnlyList<RefiningRecipe> recipes)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ResearchTimeEntry>> GetAllResearchTimesAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertResearchTimesAsync(IReadOnlyList<ResearchTimeEntry> entries)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<PropertyTypeDefinition>> GetAllPropertyTypeDefinitionsAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertPropertyTypeDefinitionsAsync(IReadOnlyList<PropertyTypeDefinition> definitions)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Private Helpers
        // ═══════════════════════════════════════════════════════════

        private static int GetSchemaVersion(NpgsqlConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT \"Value\" FROM _metadata WHERE \"Key\" = 'schema_version';";
                var result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    return 0;
                }

                return int.TryParse(result.ToString(), out int v) ? v : 0;
            }
        }

        private static void SetSchemaVersion(NpgsqlConnection conn, int version)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO _metadata (\"Key\", \"Value\") VALUES ('schema_version', @v) ON CONFLICT (\"Key\") DO UPDATE SET \"Value\" = @v;";
                cmd.Parameters.AddWithValue("@v", version.ToString());
                cmd.ExecuteNonQuery();
            }
        }

        private NpgsqlConnection OpenConnection()
        {
            var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        private void ExecuteSchema(NpgsqlConnection conn)
        {
            if (string.IsNullOrWhiteSpace(SchemaDdl))
            {
                Log.Debug("No schema DDL to execute (will be populated in tasks 7.2-7.3)");
                return;
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = SchemaDdl;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
