// -----------------------------------------------------------------------
// <copyright file="JsonSingleFileBackend.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Persistence;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Common.Storage
{
    /// <summary>
    /// IStorageBackend implementation that persists all player data in a single
    /// monolithic JSON file (PlayerData.json / Alpha3.json) and baseline data
    /// in a separate BaselineData.json file.
    /// </summary>
    internal class JsonSingleFileBackend : IStorageBackend
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly string _playerDataPath;
        private readonly string _baselineDataPath;
        private readonly object _writeLock = new object();

        private PlayerRoot _playerRoot;
        private BaselineRoot _baselineRoot;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSingleFileBackend"/> class.
        /// </summary>
        /// <param name="config">Configuration containing the path to PlayerData.json or its directory.</param>
        public JsonSingleFileBackend(StorageBackendConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            string connectionString = config.ConnectionString ?? string.Empty;

            // If the connection string points to a directory, assume PlayerData.json inside it.
            if (Directory.Exists(connectionString))
            {
                _playerDataPath = Path.Combine(connectionString, "PlayerData.json");
            }
            else
            {
                _playerDataPath = connectionString;
            }

            // Baseline data lives alongside player data.
            string directory = Path.GetDirectoryName(_playerDataPath) ?? string.Empty;
            _baselineDataPath = Path.Combine(directory, "BaselineData.json");
        }

        /// <inheritdoc/>
        public Task InitializeAsync(CancellationToken ct = default)
        {
            _playerRoot = LoadRoot<PlayerRoot>(_playerDataPath);
            _baselineRoot = LoadRoot<BaselineRoot>(_baselineDataPath);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> ValidateConnectionAsync(CancellationToken ct = default)
        {
            string directory = Path.GetDirectoryName(_playerDataPath);
            if (string.IsNullOrEmpty(directory))
            {
                return Task.FromResult(false);
            }

            if (!Directory.Exists(directory))
            {
                return Task.FromResult(false);
            }

            // Check writability by verifying we can access the directory.
            try
            {
                string testFile = Path.Combine(directory, ".write_test_" + Guid.NewGuid().ToString("N"));
                File.WriteAllText(testFile, string.Empty);
                File.Delete(testFile);
                return Task.FromResult(true);
            }
            catch (Exception)
            {
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc/>
        public StorageInfo GetStorageInfo()
        {
            return new StorageInfo
            {
                BackendType = "JsonSingleFile",
                Location = _playerDataPath
            };
        }


        // ═══════════════════════════════════════════════════════════
        // Private Helpers — Load / Save
        // ═══════════════════════════════════════════════════════════

        private T LoadRoot<T>(string filePath)
            where T : new()
        {
            if (!File.Exists(filePath))
            {
                Log.Debug("File not found, initializing empty root: {0}", filePath);
                return new T();
            }

            string content = File.ReadAllText(filePath);

            if (string.IsNullOrWhiteSpace(content))
            {
                Log.Debug("File is empty, initializing empty root: {0}", filePath);
                return new T();
            }

            try
            {
                T result = JsonConvert.DeserializeObject<T>(content, JsonSettings.SerializerSettings);
                return result ?? new T();
            }
            catch (JsonReaderException ex)
            {
                throw new StorageLoadException(
                    "JsonSingleFile",
                    filePath,
                    $"Malformed JSON in file: {filePath}",
                    ex);
            }
        }

        private void SavePlayerData()
        {
            lock (_writeLock)
            {
                PlayerRoot sorted = SerializationSorter.SortPlayerRoot(_playerRoot);
                string json = JsonConvert.SerializeObject(sorted, JsonSettings.SerializerSettings);
                SafeFileWriter.WriteAllText(_playerDataPath, json);
                Log.Debug("Player data saved to {0}", _playerDataPath);
            }
        }

        private void SaveBaselineData()
        {
            lock (_writeLock)
            {
                BaselineRoot sorted = SerializationSorter.SortBaselineRoot(_baselineRoot);
                string json = JsonConvert.SerializeObject(sorted, JsonSettings.SerializerSettings);
                SafeFileWriter.WriteAllText(_baselineDataPath, json);
                Log.Debug("Baseline data saved to {0}", _baselineDataPath);
            }
        }


        // ═══════════════════════════════════════════════════════════
        // STUBS — Server-global entities (Tasks 4.2-4.4 will implement)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<ServerFaction> GetFactionAsync(string uuid) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<ServerFaction>> GetAllFactionsAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertFactionAsync(ServerFaction faction) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteFactionAsync(string uuid) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<ServerCharacter> GetCharacterAsync(string uuid) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<ServerCharacter>> GetAllCharactersAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertCharacterAsync(ServerCharacter character) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteCharacterAsync(string uuid) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<string> GetGlobalDataAsync(string dataType) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertGlobalDataAsync(string dataType, string json) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<StarSystem>> GetAllStarSystemsAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertStarSystemsAsync(IReadOnlyList<StarSystem> systems) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<ColonySummary>> GetColonySummariesForSystemAsync(int systemId) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<ApiToken> FindTokenByHashAsync(string tokenHash) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<ApiToken>> GetAllTokensAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertTokenAsync(ApiToken token) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteTokenAsync(string id) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<MembershipAction>> GetFactionActionsAsync(string factionUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertMembershipActionAsync(MembershipAction action) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteMembershipActionAsync(string id) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteExpiredActionsAsync(DateTime cutoff) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<SharingRule>> GetSharingRulesForCharacterAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertSharingRulesAsync(string characterUUID, IReadOnlyList<SharingRule> rules) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<CharacterPreferences> GetCharacterPreferencesAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertCharacterPreferencesAsync(CharacterPreferences prefs) => throw new NotImplementedException();


        // ═══════════════════════════════════════════════════════════
        // STUBS — Per-Character Entity CRUD (Tasks 4.2-4.3 will implement)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Colony>> GetAllColoniesAsync(string characterUUID)
        {
            var results = _playerRoot.Colony
                .Where(c => c.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<Colony>>(results);
        }

        /// <inheritdoc/>
        public Task<Colony> GetColonyAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.Colony
                .FirstOrDefault(c => c.UUID == entityUUID && c.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertColonyAsync(string characterUUID, Colony entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.Colony.ToList();
            var index = list.FindIndex(c => c.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.Colony = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteColonyAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.Colony = _playerRoot.Colony
                .Where(c => !(c.UUID == entityUUID && c.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<Blueprint>> GetAllBlueprintsAsync(string characterUUID)
        {
            var results = _playerRoot.Blueprint
                .Where(b => b.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<Blueprint>>(results);
        }

        /// <inheritdoc/>
        public Task<Blueprint> GetBlueprintAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.Blueprint
                .FirstOrDefault(b => b.UUID == entityUUID && b.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertBlueprintAsync(string characterUUID, Blueprint entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.Blueprint.ToList();
            var index = list.FindIndex(b => b.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.Blueprint = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteBlueprintAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.Blueprint = _playerRoot.Blueprint
                .Where(b => !(b.UUID == entityUUID && b.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<Survey>> GetAllSurveysAsync(string characterUUID)
        {
            var results = _playerRoot.Survey
                .Where(s => s.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<Survey>>(results);
        }

        /// <inheritdoc/>
        public Task<Survey> GetSurveyAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.Survey
                .FirstOrDefault(s => s.SurveyID == entityUUID && s.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertSurveyAsync(string characterUUID, Survey entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.Survey.ToList();
            var index = list.FindIndex(s => s.SurveyID == entity.SurveyID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.Survey = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteSurveyAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.Survey = _playerRoot.Survey
                .Where(s => !(s.SurveyID == entityUUID && s.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<PlayerProfile>> GetAllPlayerProfilesAsync(string characterUUID)
        {
            var results = _playerRoot.PlayerProfile
                .Where(p => p.UUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<PlayerProfile>>(results);
        }

        /// <inheritdoc/>
        public Task<PlayerProfile> GetPlayerProfileAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.PlayerProfile
                .FirstOrDefault(p => p.UUID == entityUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertPlayerProfileAsync(string characterUUID, PlayerProfile entity)
        {
            var list = _playerRoot.PlayerProfile.ToList();
            var index = list.FindIndex(p => p.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.PlayerProfile = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeletePlayerProfileAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.PlayerProfile = _playerRoot.PlayerProfile
                .Where(p => p.UUID != entityUUID)
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<DeliveryRoute>> GetAllDeliveryRoutesAsync(string characterUUID)
        {
            var results = _playerRoot.DeliveryRoute
                .Where(r => r.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<DeliveryRoute>>(results);
        }

        /// <inheritdoc/>
        public Task<DeliveryRoute> GetDeliveryRouteAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.DeliveryRoute
                .FirstOrDefault(r => r.UUID == entityUUID && r.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertDeliveryRouteAsync(string characterUUID, DeliveryRoute entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.DeliveryRoute.ToList();
            var index = list.FindIndex(r => r.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.DeliveryRoute = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteDeliveryRouteAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.DeliveryRoute = _playerRoot.DeliveryRoute
                .Where(r => !(r.UUID == entityUUID && r.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<DeliveryPlan>> GetAllDeliveryPlansAsync(string characterUUID)
        {
            var results = _playerRoot.DeliveryPlan
                .Where(p => p.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<DeliveryPlan>>(results);
        }

        /// <inheritdoc/>
        public Task<DeliveryPlan> GetDeliveryPlanAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.DeliveryPlan
                .FirstOrDefault(p => p.UUID == entityUUID && p.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertDeliveryPlanAsync(string characterUUID, DeliveryPlan entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.DeliveryPlan.ToList();
            var index = list.FindIndex(p => p.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.DeliveryPlan = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteDeliveryPlanAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.DeliveryPlan = _playerRoot.DeliveryPlan
                .Where(p => !(p.UUID == entityUUID && p.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }


        /// <inheritdoc/>
        public Task<IReadOnlyList<Ship>> GetAllShipsAsync(string characterUUID)
        {
            var results = _playerRoot.Ship
                .Where(e => e.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<Ship>>(results);
        }

        /// <inheritdoc/>
        public Task<Ship> GetShipAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.Ship
                .FirstOrDefault(e => e.UUID == entityUUID && e.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertShipAsync(string characterUUID, Ship entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.Ship.ToList();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.Ship = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteShipAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.Ship = _playerRoot.Ship
                .Where(e => !(e.UUID == entityUUID && e.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ShipTemplate>> GetAllShipTemplatesAsync(string characterUUID)
        {
            var results = _playerRoot.ShipTemplate
                .Where(e => e.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<ShipTemplate>>(results);
        }

        /// <inheritdoc/>
        public Task<ShipTemplate> GetShipTemplateAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.ShipTemplate
                .FirstOrDefault(e => e.UUID == entityUUID && e.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertShipTemplateAsync(string characterUUID, ShipTemplate entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.ShipTemplate.ToList();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.ShipTemplate = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteShipTemplateAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.ShipTemplate = _playerRoot.ShipTemplate
                .Where(e => !(e.UUID == entityUUID && e.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<MarketListing>> GetAllMarketListingsAsync(string characterUUID)
        {
            var results = _playerRoot.MarketListing
                .Where(e => e.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<MarketListing>>(results);
        }

        /// <inheritdoc/>
        public Task<MarketListing> GetMarketListingAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.MarketListing
                .FirstOrDefault(e => e.UUID == entityUUID && e.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertMarketListingAsync(string characterUUID, MarketListing entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.MarketListing.ToList();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.MarketListing = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteMarketListingAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.MarketListing = _playerRoot.MarketListing
                .Where(e => !(e.UUID == entityUUID && e.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<MarketTransaction>> GetAllMarketTransactionsAsync(string characterUUID)
        {
            var results = _playerRoot.MarketTransaction
                .Where(e => e.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<MarketTransaction>>(results);
        }

        /// <inheritdoc/>
        public Task<MarketTransaction> GetMarketTransactionAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.MarketTransaction
                .FirstOrDefault(e => e.UUID == entityUUID && e.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertMarketTransactionAsync(string characterUUID, MarketTransaction entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.MarketTransaction.ToList();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.MarketTransaction = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteMarketTransactionAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.MarketTransaction = _playerRoot.MarketTransaction
                .Where(e => !(e.UUID == entityUUID && e.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<PricingPlan>> GetAllPricingPlansAsync(string characterUUID)
        {
            var results = _playerRoot.PricingPlan
                .Where(e => e.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<PricingPlan>>(results);
        }

        /// <inheritdoc/>
        public Task<PricingPlan> GetPricingPlanAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.PricingPlan
                .FirstOrDefault(e => e.UUID == entityUUID && e.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertPricingPlanAsync(string characterUUID, PricingPlan entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.PricingPlan.ToList();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.PricingPlan = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeletePricingPlanAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.PricingPlan = _playerRoot.PricingPlan
                .Where(e => !(e.UUID == entityUUID && e.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<StockPlan>> GetAllStockPlansAsync(string characterUUID)
        {
            var results = _playerRoot.StockPlan
                .Where(e => e.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<StockPlan>>(results);
        }

        /// <inheritdoc/>
        public Task<StockPlan> GetStockPlanAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.StockPlan
                .FirstOrDefault(e => e.UUID == entityUUID && e.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertStockPlanAsync(string characterUUID, StockPlan entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.StockPlan.ToList();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.StockPlan = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteStockPlanAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.StockPlan = _playerRoot.StockPlan
                .Where(e => !(e.UUID == entityUUID && e.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<StockProfile>> GetAllStockProfilesAsync(string characterUUID)
        {
            var results = _playerRoot.StockProfile
                .Where(e => e.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<StockProfile>>(results);
        }

        /// <inheritdoc/>
        public Task<StockProfile> GetStockProfileAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.StockProfile
                .FirstOrDefault(e => e.UUID == entityUUID && e.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertStockProfileAsync(string characterUUID, StockProfile entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.StockProfile.ToList();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.StockProfile = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteStockProfileAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.StockProfile = _playerRoot.StockProfile
                .Where(e => !(e.UUID == entityUUID && e.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }


        /// <inheritdoc/>
        public Task<IReadOnlyList<BuildPlan>> GetAllBuildPlansAsync(string characterUUID)
        {
            var results = _playerRoot.BuildPlan
                .Where(e => e.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<BuildPlan>>(results);
        }

        /// <inheritdoc/>
        public Task<BuildPlan> GetBuildPlanAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.BuildPlan
                .FirstOrDefault(e => e.UUID == entityUUID && e.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertBuildPlanAsync(string characterUUID, BuildPlan entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.BuildPlan.ToList();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.BuildPlan = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteBuildPlanAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.BuildPlan = _playerRoot.BuildPlan
                .Where(e => !(e.UUID == entityUUID && e.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<SupplyChain>> GetAllSupplyChainsAsync(string characterUUID)
        {
            var results = _playerRoot.SupplyChain
                .Where(e => e.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<SupplyChain>>(results);
        }

        /// <inheritdoc/>
        public Task<SupplyChain> GetSupplyChainAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.SupplyChain
                .FirstOrDefault(e => e.UUID == entityUUID && e.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertSupplyChainAsync(string characterUUID, SupplyChain entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.SupplyChain.ToList();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.SupplyChain = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteSupplyChainAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.SupplyChain = _playerRoot.SupplyChain
                .Where(e => !(e.UUID == entityUUID && e.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<Asteroid>> GetAllAsteroidsAsync(string characterUUID)
        {
            var results = _playerRoot.Asteroid
                .Where(e => e.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<Asteroid>>(results);
        }

        /// <inheritdoc/>
        public Task<Asteroid> GetAsteroidAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.Asteroid
                .FirstOrDefault(e => e.UUID == entityUUID && e.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertAsteroidAsync(string characterUUID, Asteroid entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.Asteroid.ToList();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.Asteroid = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteAsteroidAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.Asteroid = _playerRoot.Asteroid
                .Where(e => !(e.UUID == entityUUID && e.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<Station>> GetAllStationsAsync(string characterUUID)
        {
            var results = _playerRoot.Station
                .Where(e => e.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<Station>>(results);
        }

        /// <inheritdoc/>
        public Task<Station> GetStationAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.Station
                .FirstOrDefault(e => e.UUID == entityUUID && e.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertStationAsync(string characterUUID, Station entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.Station.ToList();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.Station = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteStationAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.Station = _playerRoot.Station
                .Where(e => !(e.UUID == entityUUID && e.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<Faction>> GetAllFactionsForCharacterAsync(string characterUUID)
        {
            var results = _playerRoot.Faction
                .Where(e => e.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<Faction>>(results);
        }

        /// <inheritdoc/>
        public Task<Faction> GetFactionForCharacterAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.Faction
                .FirstOrDefault(e => e.UUID == entityUUID && e.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertFactionForCharacterAsync(string characterUUID, Faction entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.Faction.ToList();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.Faction = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteFactionForCharacterAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.Faction = _playerRoot.Faction
                .Where(e => !(e.UUID == entityUUID && e.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ExternalCharacter>> GetAllExternalCharactersAsync(string characterUUID)
        {
            var results = _playerRoot.ExternalCharacter
                .Where(e => e.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<ExternalCharacter>>(results);
        }

        /// <inheritdoc/>
        public Task<ExternalCharacter> GetExternalCharacterAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.ExternalCharacter
                .FirstOrDefault(e => e.UUID == entityUUID && e.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertExternalCharacterAsync(string characterUUID, ExternalCharacter entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.ExternalCharacter.ToList();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.ExternalCharacter = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteExternalCharacterAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.ExternalCharacter = _playerRoot.ExternalCharacter
                .Where(e => !(e.UUID == entityUUID && e.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<WarehouseOverflowRule>> GetAllWarehouseOverflowRulesAsync(string characterUUID)
        {
            var results = _playerRoot.WarehouseOverflowRule
                .Where(e => e.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<WarehouseOverflowRule>>(results);
        }

        /// <inheritdoc/>
        public Task<WarehouseOverflowRule> GetWarehouseOverflowRuleAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.WarehouseOverflowRule
                .FirstOrDefault(e => e.UUID == entityUUID && e.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertWarehouseOverflowRuleAsync(string characterUUID, WarehouseOverflowRule entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.WarehouseOverflowRule.ToList();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.WarehouseOverflowRule = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteWarehouseOverflowRuleAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.WarehouseOverflowRule = _playerRoot.WarehouseOverflowRule
                .Where(e => !(e.UUID == entityUUID && e.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<MailMessage>> GetAllMailMessagesAsync(string characterUUID)
        {
            var results = _playerRoot.MailMessage
                .Where(e => e.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<MailMessage>>(results);
        }

        /// <inheritdoc/>
        public Task<MailMessage> GetMailMessageAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.MailMessage
                .FirstOrDefault(e => e.UUID == entityUUID && e.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertMailMessageAsync(string characterUUID, MailMessage entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.MailMessage.ToList();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.MailMessage = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteMailMessageAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.MailMessage = _playerRoot.MailMessage
                .Where(e => !(e.UUID == entityUUID && e.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<BankingTransaction>> GetAllBankingTransactionsAsync(string characterUUID)
        {
            var results = _playerRoot.BankingTransaction
                .Where(e => e.OwnerUUID == characterUUID)
                .ToList();
            return Task.FromResult<IReadOnlyList<BankingTransaction>>(results);
        }

        /// <inheritdoc/>
        public Task<BankingTransaction> GetBankingTransactionAsync(string characterUUID, string entityUUID)
        {
            var entity = _playerRoot.BankingTransaction
                .FirstOrDefault(e => e.UUID == entityUUID && e.OwnerUUID == characterUUID);
            return Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public Task UpsertBankingTransactionAsync(string characterUUID, BankingTransaction entity)
        {
            entity.OwnerUUID = characterUUID;
            var list = _playerRoot.BankingTransaction.ToList();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0)
            {
                list[index] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _playerRoot.BankingTransaction = list.ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteBankingTransactionAsync(string characterUUID, string entityUUID)
        {
            _playerRoot.BankingTransaction = _playerRoot.BankingTransaction
                .Where(e => !(e.UUID == entityUUID && e.OwnerUUID == characterUUID))
                .ToArray();
            SavePlayerData();
            return Task.CompletedTask;
        }


        // ═══════════════════════════════════════════════════════════
        // STUBS — Faction Permission Entities (Task 4.4 will implement)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionCapability>> GetFactionCapabilitiesAsync(string factionUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertFactionCapabilityAsync(FactionCapability capability) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteFactionCapabilityAsync(string factionUUID, string capabilityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionClearanceLevel>> GetFactionClearanceLevelsAsync(string factionUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertFactionClearanceLevelAsync(FactionClearanceLevel level) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteFactionClearanceLevelAsync(string factionUUID, string levelUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionPermissionGroup>> GetFactionGroupsAsync(string factionUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<FactionPermissionGroup> GetFactionGroupAsync(string factionUUID, string groupUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertFactionGroupAsync(FactionPermissionGroup group) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteFactionGroupAsync(string factionUUID, string groupUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionGroupCapability>> GetFactionGroupCapabilitiesAsync(string groupUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task AddFactionGroupCapabilityAsync(FactionGroupCapability item) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task RemoveFactionGroupCapabilityAsync(string groupUUID, string capabilityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionGroupSharingRule>> GetFactionGroupSharingRulesAsync(string groupUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertFactionGroupSharingRuleAsync(FactionGroupSharingRule rule) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteFactionGroupSharingRuleAsync(string groupUUID, string ruleUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<FactionMemberPermissions> GetFactionMemberPermissionsAsync(string factionUUID, string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertFactionMemberPermissionsAsync(FactionMemberPermissions perms) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionMemberPermissions>> GetAllFactionMembersPermissionsAsync(string factionUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionMemberCapability>> GetFactionMemberCapabilitiesAsync(string factionUUID, string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task AddFactionMemberCapabilityAsync(FactionMemberCapability item) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task RemoveFactionMemberCapabilityAsync(string factionUUID, string characterUUID, string capabilityUUID) => throw new NotImplementedException();


        // ═══════════════════════════════════════════════════════════
        // STUBS — Character Permission Entities (Task 4.4 will implement)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterCapability>> GetCharacterCapabilitiesAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertCharacterCapabilityAsync(CharacterCapability capability) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteCharacterCapabilityAsync(string characterUUID, string capabilityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterClearanceLevel>> GetCharacterClearanceLevelsAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertCharacterClearanceLevelAsync(CharacterClearanceLevel level) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteCharacterClearanceLevelAsync(string characterUUID, string levelUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterPermissionGroup>> GetCharacterGroupsAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<CharacterPermissionGroup> GetCharacterGroupAsync(string characterUUID, string groupUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertCharacterGroupAsync(CharacterPermissionGroup group) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteCharacterGroupAsync(string characterUUID, string groupUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGroupCapability>> GetCharacterGroupCapabilitiesAsync(string groupUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task AddCharacterGroupCapabilityAsync(CharacterGroupCapability item) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task RemoveCharacterGroupCapabilityAsync(string groupUUID, string capabilityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGroupSharingRule>> GetCharacterGroupSharingRulesAsync(string groupUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertCharacterGroupSharingRuleAsync(CharacterGroupSharingRule rule) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteCharacterGroupSharingRuleAsync(string groupUUID, string ruleUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGranteePermissions>> GetCharacterGranteesAsync(string ownerCharacterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertCharacterGranteePermissionsAsync(CharacterGranteePermissions perms) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteCharacterGranteePermissionsAsync(string ownerCharacterUUID, string granteeUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGranteeCapability>> GetCharacterGranteeCapabilitiesAsync(string ownerCharacterUUID, string granteeUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task AddCharacterGranteeCapabilityAsync(CharacterGranteeCapability item) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task RemoveCharacterGranteeCapabilityAsync(string ownerCharacterUUID, string granteeUUID, string capabilityUUID) => throw new NotImplementedException();


        // ═══════════════════════════════════════════════════════════
        // STUBS — Intel (Task 4.4 will implement)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelComment>> GetIntelCommentsForTargetAsync(string targetCharacterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IntelComment> GetIntelCommentAsync(string commentUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertIntelCommentAsync(IntelComment comment) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteIntelCommentAsync(string commentUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForCommentAsync(string commentUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForFactionAsync(string factionUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertIntelShareAsync(IntelCommentFactionShare share) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteIntelShareAsync(string shareUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // STUBS — Audit (Task 4.4 will implement)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<PermissionAuditEntry>> GetPermissionAuditEntriesAsync(DateTime? startDate = null, DateTime? endDate = null, PermissionActionType? actionType = null, string actorUUID = null, string targetUUID = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task AppendPermissionAuditEntryAsync(PermissionAuditEntry entry) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteExpiredAuditEntriesAsync(DateTime cutoff) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // STUBS — Baseline / Global Lookup Data (Task 4.4 will implement)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<BaselineGameConstants> GetBaselineGameConstantsAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertBaselineGameConstantsAsync(BaselineGameConstants constants) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<BlueprintType>> GetAllBlueprintTypesAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertBlueprintTypesAsync(IReadOnlyList<BlueprintType> types) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<ShipClass>> GetAllShipClassesAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertShipClassesAsync(IReadOnlyList<ShipClass> classes) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<TechLevel>> GetAllTechLevelsAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertTechLevelsAsync(IReadOnlyList<TechLevel> levels) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<Commodity>> GetAllCommoditiesAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertCommoditiesAsync(IReadOnlyList<Commodity> commodities) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<RefiningRecipe>> GetAllRefiningRecipesAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertRefiningRecipesAsync(IReadOnlyList<RefiningRecipe> recipes) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<ResearchTimeEntry>> GetAllResearchTimesAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertResearchTimesAsync(IReadOnlyList<ResearchTimeEntry> entries) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<PropertyTypeDefinition>> GetAllPropertyTypeDefinitionsAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertPropertyTypeDefinitionsAsync(IReadOnlyList<PropertyTypeDefinition> definitions) => throw new NotImplementedException();
    }
}
