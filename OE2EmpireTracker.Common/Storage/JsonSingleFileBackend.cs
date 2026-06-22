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
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Common.Storage
{
    /// <summary>
    /// IStorageBackend implementation that persists all player data in a single
    /// monolithic JSON file (PlayerData.json / Alpha3.json) and baseline data
    /// in a separate BaselineData.json file. Server-global entities, permissions,
    /// intel, and audit data are stored in a ServerData.json file alongside
    /// the player data.
    /// </summary>
    internal class JsonSingleFileBackend : IStorageBackend
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly string _playerDataPath;
        private readonly string _baselineDataPath;
        private readonly string _serverDataPath;
        private readonly object _writeLock = new object();

        private PlayerRoot _playerRoot;
        private BaselineRoot _baselineRoot;
        private ServerDataStore _serverStore;

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
            _serverDataPath = Path.Combine(directory, "ServerData.json");
        }

        /// <inheritdoc/>
        public Task InitializeAsync(CancellationToken ct = default)
        {
            _playerRoot = LoadRoot<PlayerRoot>(_playerDataPath);
            _baselineRoot = LoadRoot<BaselineRoot>(_baselineDataPath);
            _serverStore = LoadRoot<ServerDataStore>(_serverDataPath);

            // DataVersion migration check
            int playerVersion = _playerRoot.DataVersion;
            if (playerVersion < MigrationRunner.CurrentVersion)
            {
                Log.Info(
                    "Player data version {0} is below current version {1}; migration may be needed",
                    playerVersion,
                    MigrationRunner.CurrentVersion);
            }

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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Server-Global Entities
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<ServerFaction> GetFactionAsync(string uuid)
        {
            var result = _serverStore.ServerFactions.FirstOrDefault(f => f.UUID == uuid);
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ServerFaction>> GetAllFactionsAsync()
        {
            IReadOnlyList<ServerFaction> result = _serverStore.ServerFactions.ToList();
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertFactionAsync(ServerFaction faction)
        {
            _serverStore.ServerFactions.RemoveAll(f => f.UUID == faction.UUID);
            _serverStore.ServerFactions.Add(faction);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteFactionAsync(string uuid)
        {
            _serverStore.ServerFactions.RemoveAll(f => f.UUID == uuid);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<ServerCharacter> GetCharacterAsync(string uuid)
        {
            var result = _serverStore.ServerCharacters.FirstOrDefault(c => c.UUID == uuid);
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ServerCharacter>> GetAllCharactersAsync()
        {
            IReadOnlyList<ServerCharacter> result = _serverStore.ServerCharacters.ToList();
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertCharacterAsync(ServerCharacter character)
        {
            _serverStore.ServerCharacters.RemoveAll(c => c.UUID == character.UUID);
            _serverStore.ServerCharacters.Add(character);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteCharacterAsync(string uuid)
        {
            _serverStore.ServerCharacters.RemoveAll(c => c.UUID == uuid);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<string> GetGlobalDataAsync(string dataType)
        {
            _serverStore.GlobalData.TryGetValue(dataType, out string value);
            return Task.FromResult(value);
        }

        /// <inheritdoc/>
        public Task UpsertGlobalDataAsync(string dataType, string json)
        {
            _serverStore.GlobalData[dataType] = json;
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<StarSystem>> GetAllStarSystemsAsync()
        {
            IReadOnlyList<StarSystem> result = _serverStore.StarSystems.ToList();
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertStarSystemsAsync(IReadOnlyList<StarSystem> systems)
        {
            foreach (var system in systems)
            {
                _serverStore.StarSystems.RemoveAll(s => s.Id == system.Id);
                _serverStore.StarSystems.Add(system);
            }

            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ColonySummary>> GetColonySummariesForSystemAsync(int systemId)
        {
            var summaries = _playerRoot.Colony
                .Where(c => c.SystemId == systemId)
                .Select(c => new ColonySummary
                {
                    ColonyName = c.ColonyName ?? string.Empty,
                    Size = c.ColonySize,
                    PlanetName = c.PlanetName ?? string.Empty,
                })
                .ToList();
            IReadOnlyList<ColonySummary> result = summaries;
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task<ApiToken> FindTokenByHashAsync(string tokenHash)
        {
            var result = _serverStore.Tokens.FirstOrDefault(t => t.TokenHash == tokenHash);
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ApiToken>> GetAllTokensAsync()
        {
            IReadOnlyList<ApiToken> result = _serverStore.Tokens.ToList();
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertTokenAsync(ApiToken token)
        {
            _serverStore.Tokens.RemoveAll(t => t.Id == token.Id);
            _serverStore.Tokens.Add(token);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteTokenAsync(string id)
        {
            _serverStore.Tokens.RemoveAll(t => t.Id == id);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<MembershipAction>> GetFactionActionsAsync(string factionUUID)
        {
            var results = _serverStore.MembershipActions
                .Where(a => a.FactionUUID == factionUUID)
                .ToList();
            IReadOnlyList<MembershipAction> result = results;
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertMembershipActionAsync(MembershipAction action)
        {
            _serverStore.MembershipActions.RemoveAll(a => a.Id == action.Id);
            _serverStore.MembershipActions.Add(action);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteMembershipActionAsync(string id)
        {
            _serverStore.MembershipActions.RemoveAll(a => a.Id == id);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteExpiredActionsAsync(DateTime cutoff)
        {
            _serverStore.MembershipActions.RemoveAll(a => a.ExpiresUtc <= cutoff);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<SharingRule>> GetSharingRulesForCharacterAsync(string characterUUID)
        {
            if (!_serverStore.SharingRules.TryGetValue(characterUUID, out var rules))
            {
                rules = new List<SharingRule>();
            }

            IReadOnlyList<SharingRule> result = rules.ToList();
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertSharingRulesAsync(string characterUUID, IReadOnlyList<SharingRule> rules)
        {
            _serverStore.SharingRules[characterUUID] = rules.ToList();
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<CharacterPreferences> GetCharacterPreferencesAsync(string characterUUID)
        {
            _serverStore.CharacterPreferences.TryGetValue(characterUUID, out var prefs);
            return Task.FromResult(prefs);
        }

        /// <inheritdoc/>
        public Task UpsertCharacterPreferencesAsync(CharacterPreferences prefs)
        {
            _serverStore.CharacterPreferences[prefs.CharacterUUID] = prefs;
            SaveServerData();
            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Character Discovery
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<string>> GetAllCharacterUUIDsAsync()
        {
            var uuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var e in _playerRoot.Colony)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            foreach (var e in _playerRoot.Blueprint)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            foreach (var e in _playerRoot.Survey)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            foreach (var e in _playerRoot.PlayerProfile)
            {
                if (!string.IsNullOrEmpty(e.UUID))
                {
                    uuids.Add(e.UUID);
                }
            }

            foreach (var e in _playerRoot.DeliveryRoute)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            foreach (var e in _playerRoot.DeliveryPlan)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            foreach (var e in _playerRoot.Ship)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            foreach (var e in _playerRoot.ShipTemplate)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            foreach (var e in _playerRoot.MarketListing)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            foreach (var e in _playerRoot.MarketTransaction)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            foreach (var e in _playerRoot.PricingPlan)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            foreach (var e in _playerRoot.StockPlan)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            foreach (var e in _playerRoot.StockProfile)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            foreach (var e in _playerRoot.BuildPlan)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            foreach (var e in _playerRoot.SupplyChain)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            foreach (var e in _playerRoot.Asteroid)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            foreach (var e in _playerRoot.Station)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            foreach (var e in _playerRoot.Faction)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            foreach (var e in _playerRoot.ExternalCharacter)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            foreach (var e in _playerRoot.WarehouseOverflowRule)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            foreach (var e in _playerRoot.BankingTransaction)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            foreach (var e in _playerRoot.MailMessage)
            {
                if (!string.IsNullOrEmpty(e.OwnerUUID))
                {
                    uuids.Add(e.OwnerUUID);
                }
            }

            IReadOnlyList<string> result = uuids.ToList();
            return Task.FromResult(result);
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Faction Permission Entities
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionCapability>> GetFactionCapabilitiesAsync(string factionUUID)
        {
            var results = _serverStore.FactionCapabilities
                .Where(c => c.FactionUUID == factionUUID)
                .ToList();
            IReadOnlyList<FactionCapability> result = results;
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertFactionCapabilityAsync(FactionCapability capability)
        {
            _serverStore.FactionCapabilities.RemoveAll(c => c.UUID == capability.UUID);
            _serverStore.FactionCapabilities.Add(capability);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteFactionCapabilityAsync(string factionUUID, string capabilityUUID)
        {
            _serverStore.FactionCapabilities.RemoveAll(
                c => c.FactionUUID == factionUUID && c.UUID == capabilityUUID);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionClearanceLevel>> GetFactionClearanceLevelsAsync(string factionUUID)
        {
            var results = _serverStore.FactionClearanceLevels
                .Where(l => l.FactionUUID == factionUUID)
                .ToList();
            IReadOnlyList<FactionClearanceLevel> result = results;
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertFactionClearanceLevelAsync(FactionClearanceLevel level)
        {
            _serverStore.FactionClearanceLevels.RemoveAll(l => l.UUID == level.UUID);
            _serverStore.FactionClearanceLevels.Add(level);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteFactionClearanceLevelAsync(string factionUUID, string levelUUID)
        {
            _serverStore.FactionClearanceLevels.RemoveAll(
                l => l.FactionUUID == factionUUID && l.UUID == levelUUID);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionPermissionGroup>> GetFactionGroupsAsync(string factionUUID)
        {
            var results = _serverStore.FactionGroups
                .Where(g => g.FactionUUID == factionUUID)
                .ToList();
            IReadOnlyList<FactionPermissionGroup> result = results;
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task<FactionPermissionGroup> GetFactionGroupAsync(string factionUUID, string groupUUID)
        {
            var result = _serverStore.FactionGroups
                .FirstOrDefault(g => g.FactionUUID == factionUUID && g.UUID == groupUUID);
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertFactionGroupAsync(FactionPermissionGroup group)
        {
            _serverStore.FactionGroups.RemoveAll(g => g.UUID == group.UUID);
            _serverStore.FactionGroups.Add(group);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteFactionGroupAsync(string factionUUID, string groupUUID)
        {
            _serverStore.FactionGroups.RemoveAll(
                g => g.FactionUUID == factionUUID && g.UUID == groupUUID);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionGroupCapability>> GetFactionGroupCapabilitiesAsync(string groupUUID)
        {
            var results = _serverStore.FactionGroupCapabilities
                .Where(c => c.GroupUUID == groupUUID)
                .ToList();
            IReadOnlyList<FactionGroupCapability> result = results;
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task AddFactionGroupCapabilityAsync(FactionGroupCapability item)
        {
            _serverStore.FactionGroupCapabilities.RemoveAll(
                c => c.GroupUUID == item.GroupUUID && c.CapabilityUUID == item.CapabilityUUID);
            _serverStore.FactionGroupCapabilities.Add(item);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RemoveFactionGroupCapabilityAsync(string groupUUID, string capabilityUUID)
        {
            _serverStore.FactionGroupCapabilities.RemoveAll(
                c => c.GroupUUID == groupUUID && c.CapabilityUUID == capabilityUUID);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionGroupSharingRule>> GetFactionGroupSharingRulesAsync(string groupUUID)
        {
            var results = _serverStore.FactionGroupSharingRules
                .Where(r => r.GroupUUID == groupUUID)
                .ToList();
            IReadOnlyList<FactionGroupSharingRule> result = results;
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertFactionGroupSharingRuleAsync(FactionGroupSharingRule rule)
        {
            _serverStore.FactionGroupSharingRules.RemoveAll(r => r.UUID == rule.UUID);
            _serverStore.FactionGroupSharingRules.Add(rule);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteFactionGroupSharingRuleAsync(string groupUUID, string ruleUUID)
        {
            _serverStore.FactionGroupSharingRules.RemoveAll(
                r => r.GroupUUID == groupUUID && r.UUID == ruleUUID);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<FactionMemberPermissions> GetFactionMemberPermissionsAsync(string factionUUID, string characterUUID)
        {
            var result = _serverStore.FactionMemberPermissions
                .FirstOrDefault(p => p.FactionUUID == factionUUID && p.CharacterUUID == characterUUID);
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertFactionMemberPermissionsAsync(FactionMemberPermissions perms)
        {
            _serverStore.FactionMemberPermissions.RemoveAll(
                p => p.FactionUUID == perms.FactionUUID && p.CharacterUUID == perms.CharacterUUID);
            _serverStore.FactionMemberPermissions.Add(perms);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionMemberPermissions>> GetAllFactionMembersPermissionsAsync(string factionUUID)
        {
            var results = _serverStore.FactionMemberPermissions
                .Where(p => p.FactionUUID == factionUUID)
                .ToList();
            IReadOnlyList<FactionMemberPermissions> result = results;
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionMemberCapability>> GetFactionMemberCapabilitiesAsync(string factionUUID, string characterUUID)
        {
            var results = _serverStore.FactionMemberCapabilities
                .Where(c => c.FactionUUID == factionUUID && c.CharacterUUID == characterUUID)
                .ToList();
            IReadOnlyList<FactionMemberCapability> result = results;
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task AddFactionMemberCapabilityAsync(FactionMemberCapability item)
        {
            _serverStore.FactionMemberCapabilities.RemoveAll(
                c => c.FactionUUID == item.FactionUUID
                     && c.CharacterUUID == item.CharacterUUID
                     && c.CapabilityUUID == item.CapabilityUUID);
            _serverStore.FactionMemberCapabilities.Add(item);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RemoveFactionMemberCapabilityAsync(string factionUUID, string characterUUID, string capabilityUUID)
        {
            _serverStore.FactionMemberCapabilities.RemoveAll(
                c => c.FactionUUID == factionUUID
                     && c.CharacterUUID == characterUUID
                     && c.CapabilityUUID == capabilityUUID);
            SaveServerData();
            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Character Permission Entities
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterCapability>> GetCharacterCapabilitiesAsync(string characterUUID)
        {
            var results = _serverStore.CharacterCapabilities
                .Where(c => c.OwnerCharacterUUID == characterUUID)
                .ToList();
            IReadOnlyList<CharacterCapability> result = results;
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertCharacterCapabilityAsync(CharacterCapability capability)
        {
            _serverStore.CharacterCapabilities.RemoveAll(c => c.UUID == capability.UUID);
            _serverStore.CharacterCapabilities.Add(capability);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteCharacterCapabilityAsync(string characterUUID, string capabilityUUID)
        {
            _serverStore.CharacterCapabilities.RemoveAll(
                c => c.OwnerCharacterUUID == characterUUID && c.UUID == capabilityUUID);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterClearanceLevel>> GetCharacterClearanceLevelsAsync(string characterUUID)
        {
            var results = _serverStore.CharacterClearanceLevels
                .Where(l => l.OwnerCharacterUUID == characterUUID)
                .ToList();
            IReadOnlyList<CharacterClearanceLevel> result = results;
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertCharacterClearanceLevelAsync(CharacterClearanceLevel level)
        {
            _serverStore.CharacterClearanceLevels.RemoveAll(l => l.UUID == level.UUID);
            _serverStore.CharacterClearanceLevels.Add(level);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteCharacterClearanceLevelAsync(string characterUUID, string levelUUID)
        {
            _serverStore.CharacterClearanceLevels.RemoveAll(
                l => l.OwnerCharacterUUID == characterUUID && l.UUID == levelUUID);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterPermissionGroup>> GetCharacterGroupsAsync(string characterUUID)
        {
            var results = _serverStore.CharacterGroups
                .Where(g => g.OwnerCharacterUUID == characterUUID)
                .ToList();
            IReadOnlyList<CharacterPermissionGroup> result = results;
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task<CharacterPermissionGroup> GetCharacterGroupAsync(string characterUUID, string groupUUID)
        {
            var result = _serverStore.CharacterGroups
                .FirstOrDefault(g => g.OwnerCharacterUUID == characterUUID && g.UUID == groupUUID);
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertCharacterGroupAsync(CharacterPermissionGroup group)
        {
            _serverStore.CharacterGroups.RemoveAll(g => g.UUID == group.UUID);
            _serverStore.CharacterGroups.Add(group);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteCharacterGroupAsync(string characterUUID, string groupUUID)
        {
            _serverStore.CharacterGroups.RemoveAll(
                g => g.OwnerCharacterUUID == characterUUID && g.UUID == groupUUID);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGroupCapability>> GetCharacterGroupCapabilitiesAsync(string groupUUID)
        {
            var results = _serverStore.CharacterGroupCapabilities
                .Where(c => c.GroupUUID == groupUUID)
                .ToList();
            IReadOnlyList<CharacterGroupCapability> result = results;
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task AddCharacterGroupCapabilityAsync(CharacterGroupCapability item)
        {
            _serverStore.CharacterGroupCapabilities.RemoveAll(
                c => c.GroupUUID == item.GroupUUID && c.CapabilityUUID == item.CapabilityUUID);
            _serverStore.CharacterGroupCapabilities.Add(item);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RemoveCharacterGroupCapabilityAsync(string groupUUID, string capabilityUUID)
        {
            _serverStore.CharacterGroupCapabilities.RemoveAll(
                c => c.GroupUUID == groupUUID && c.CapabilityUUID == capabilityUUID);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGroupSharingRule>> GetCharacterGroupSharingRulesAsync(string groupUUID)
        {
            var results = _serverStore.CharacterGroupSharingRules
                .Where(r => r.GroupUUID == groupUUID)
                .ToList();
            IReadOnlyList<CharacterGroupSharingRule> result = results;
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertCharacterGroupSharingRuleAsync(CharacterGroupSharingRule rule)
        {
            _serverStore.CharacterGroupSharingRules.RemoveAll(r => r.UUID == rule.UUID);
            _serverStore.CharacterGroupSharingRules.Add(rule);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteCharacterGroupSharingRuleAsync(string groupUUID, string ruleUUID)
        {
            _serverStore.CharacterGroupSharingRules.RemoveAll(
                r => r.GroupUUID == groupUUID && r.UUID == ruleUUID);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGranteePermissions>> GetCharacterGranteesAsync(string ownerCharacterUUID)
        {
            var results = _serverStore.CharacterGranteePermissions
                .Where(p => p.OwnerCharacterUUID == ownerCharacterUUID)
                .ToList();
            IReadOnlyList<CharacterGranteePermissions> result = results;
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertCharacterGranteePermissionsAsync(CharacterGranteePermissions perms)
        {
            _serverStore.CharacterGranteePermissions.RemoveAll(
                p => p.OwnerCharacterUUID == perms.OwnerCharacterUUID
                     && p.GranteeUUID == perms.GranteeUUID);
            _serverStore.CharacterGranteePermissions.Add(perms);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteCharacterGranteePermissionsAsync(string ownerCharacterUUID, string granteeUUID)
        {
            _serverStore.CharacterGranteePermissions.RemoveAll(
                p => p.OwnerCharacterUUID == ownerCharacterUUID && p.GranteeUUID == granteeUUID);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGranteeCapability>> GetCharacterGranteeCapabilitiesAsync(string ownerCharacterUUID, string granteeUUID)
        {
            var results = _serverStore.CharacterGranteeCapabilities
                .Where(c => c.OwnerCharacterUUID == ownerCharacterUUID && c.GranteeUUID == granteeUUID)
                .ToList();
            IReadOnlyList<CharacterGranteeCapability> result = results;
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task AddCharacterGranteeCapabilityAsync(CharacterGranteeCapability item)
        {
            _serverStore.CharacterGranteeCapabilities.RemoveAll(
                c => c.OwnerCharacterUUID == item.OwnerCharacterUUID
                     && c.GranteeUUID == item.GranteeUUID
                     && c.CapabilityUUID == item.CapabilityUUID);
            _serverStore.CharacterGranteeCapabilities.Add(item);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RemoveCharacterGranteeCapabilityAsync(string ownerCharacterUUID, string granteeUUID, string capabilityUUID)
        {
            _serverStore.CharacterGranteeCapabilities.RemoveAll(
                c => c.OwnerCharacterUUID == ownerCharacterUUID
                     && c.GranteeUUID == granteeUUID
                     && c.CapabilityUUID == capabilityUUID);
            SaveServerData();
            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Intel
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelComment>> GetIntelCommentsForTargetAsync(string targetCharacterUUID)
        {
            var results = _serverStore.IntelComments
                .Where(c => c.TargetCharacterUUID == targetCharacterUUID)
                .ToList();
            IReadOnlyList<IntelComment> result = results;
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task<IntelComment> GetIntelCommentAsync(string commentUUID)
        {
            var result = _serverStore.IntelComments.FirstOrDefault(c => c.UUID == commentUUID);
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertIntelCommentAsync(IntelComment comment)
        {
            _serverStore.IntelComments.RemoveAll(c => c.UUID == comment.UUID);
            _serverStore.IntelComments.Add(comment);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteIntelCommentAsync(string commentUUID)
        {
            _serverStore.IntelComments.RemoveAll(c => c.UUID == commentUUID);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForCommentAsync(string commentUUID)
        {
            var results = _serverStore.IntelShares
                .Where(s => s.IntelCommentUUID == commentUUID)
                .ToList();
            IReadOnlyList<IntelCommentFactionShare> result = results;
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForFactionAsync(string factionUUID)
        {
            var results = _serverStore.IntelShares
                .Where(s => s.FactionUUID == factionUUID)
                .ToList();
            IReadOnlyList<IntelCommentFactionShare> result = results;
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertIntelShareAsync(IntelCommentFactionShare share)
        {
            _serverStore.IntelShares.RemoveAll(s => s.UUID == share.UUID);
            _serverStore.IntelShares.Add(share);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteIntelShareAsync(string shareUUID)
        {
            _serverStore.IntelShares.RemoveAll(s => s.UUID == shareUUID);
            SaveServerData();
            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Audit
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<PermissionAuditEntry>> GetPermissionAuditEntriesAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            PermissionActionType? actionType = null,
            string actorUUID = null,
            string targetUUID = null)
        {
            IEnumerable<PermissionAuditEntry> query = _serverStore.AuditEntries;

            if (startDate.HasValue)
            {
                query = query.Where(e => e.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(e => e.Timestamp <= endDate.Value);
            }

            if (actionType.HasValue)
            {
                query = query.Where(e => e.ActionType == actionType.Value);
            }

            if (!string.IsNullOrEmpty(actorUUID))
            {
                query = query.Where(e => e.ActorCharacterUUID == actorUUID);
            }

            if (!string.IsNullOrEmpty(targetUUID))
            {
                query = query.Where(e => e.TargetCharacterUUID == targetUUID);
            }

            IReadOnlyList<PermissionAuditEntry> result = query.ToList();
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task AppendPermissionAuditEntryAsync(PermissionAuditEntry entry)
        {
            _serverStore.AuditEntries.Add(entry);
            SaveServerData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteExpiredAuditEntriesAsync(DateTime cutoff)
        {
            _serverStore.AuditEntries.RemoveAll(e => e.Timestamp < cutoff);
            SaveServerData();
            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Baseline / Global Lookup Data
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<BaselineGameConstants> GetBaselineGameConstantsAsync()
        {
            return Task.FromResult(_baselineRoot.GameConstants);
        }

        /// <inheritdoc/>
        public Task UpsertBaselineGameConstantsAsync(BaselineGameConstants constants)
        {
            _baselineRoot.GameConstants = constants;
            SaveBaselineData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<BlueprintType>> GetAllBlueprintTypesAsync()
        {
            IReadOnlyList<BlueprintType> result = _baselineRoot.BlueprintType ?? Array.Empty<BlueprintType>();
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertBlueprintTypesAsync(IReadOnlyList<BlueprintType> types)
        {
            _baselineRoot.BlueprintType = types.ToArray();
            SaveBaselineData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ShipClass>> GetAllShipClassesAsync()
        {
            IReadOnlyList<ShipClass> result = _baselineRoot.ShipClass ?? Array.Empty<ShipClass>();
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertShipClassesAsync(IReadOnlyList<ShipClass> classes)
        {
            _baselineRoot.ShipClass = classes.ToArray();
            SaveBaselineData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<TechLevel>> GetAllTechLevelsAsync()
        {
            IReadOnlyList<TechLevel> result = _baselineRoot.TechLevel ?? Array.Empty<TechLevel>();
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertTechLevelsAsync(IReadOnlyList<TechLevel> levels)
        {
            _baselineRoot.TechLevel = levels.ToArray();
            SaveBaselineData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<Commodity>> GetAllCommoditiesAsync()
        {
            IReadOnlyList<Commodity> result = _baselineRoot.Commodity ?? Array.Empty<Commodity>();
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertCommoditiesAsync(IReadOnlyList<Commodity> commodities)
        {
            _baselineRoot.Commodity = commodities.ToArray();
            SaveBaselineData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<RefiningRecipe>> GetAllRefiningRecipesAsync()
        {
            IReadOnlyList<RefiningRecipe> result = _baselineRoot.RefiningRecipe ?? Array.Empty<RefiningRecipe>();
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertRefiningRecipesAsync(IReadOnlyList<RefiningRecipe> recipes)
        {
            _baselineRoot.RefiningRecipe = recipes.ToArray();
            SaveBaselineData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ResearchTimeEntry>> GetAllResearchTimesAsync()
        {
            IReadOnlyList<ResearchTimeEntry> result = _baselineRoot.ResearchTime ?? Array.Empty<ResearchTimeEntry>();
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertResearchTimesAsync(IReadOnlyList<ResearchTimeEntry> entries)
        {
            _baselineRoot.ResearchTime = entries.ToArray();
            SaveBaselineData();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<PropertyTypeDefinition>> GetAllPropertyTypeDefinitionsAsync()
        {
            IReadOnlyList<PropertyTypeDefinition> result = _baselineRoot.PropertyType ?? Array.Empty<PropertyTypeDefinition>();
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task UpsertPropertyTypeDefinitionsAsync(IReadOnlyList<PropertyTypeDefinition> definitions)
        {
            _baselineRoot.PropertyType = definitions.ToArray();
            SaveBaselineData();
            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Private Helpers â€” Load / Save
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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

        private void SaveServerData()
        {
            lock (_writeLock)
            {
                string json = JsonConvert.SerializeObject(_serverStore, JsonSettings.SerializerSettings);
                SafeFileWriter.WriteAllText(_serverDataPath, json);
                Log.Debug("Server data saved to {0}", _serverDataPath);
            }
        }
    }
}
