// -----------------------------------------------------------------------
// <copyright file="JsonMultiFileBackend.cs" company="OE2EmpireTracker">
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

namespace OE2EmpireTracker.Common.Storage
{
    /// <summary>
    /// JSON multi-file-based implementation of <see cref="IStorageBackend"/>.
    /// Stores data as JSON files in a configurable directory structure.
    /// Thread-safe via SemaphoreSlim; atomic writes via temp-file + rename.
    /// </summary>
    public class JsonMultiFileBackend : IStorageBackend
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
        };

        private readonly string _dataPath;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonMultiFileBackend"/> class.
        /// </summary>
        /// <param name="dataPath">Root directory for JSON file storage.</param>
        public JsonMultiFileBackend(string dataPath)
        {
            _dataPath = dataPath ?? "./data";
        }

        // ═══════════════════════════════════════════════════════════
        // Lifecycle
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task InitializeAsync(CancellationToken ct = default)
        {
            await _lock.WaitAsync(ct);
            try
            {
                Directory.CreateDirectory(_dataPath);
                Directory.CreateDirectory(Path.Combine(_dataPath, "global"));
                Directory.CreateDirectory(Path.Combine(_dataPath, "characters"));

                EnsureFile(Path.Combine(_dataPath, "factions.json"), "[]");
                EnsureFile(Path.Combine(_dataPath, "characters.json"), "[]");
                EnsureFile(Path.Combine(_dataPath, "tokens.json"), "[]");
                EnsureFile(Path.Combine(_dataPath, "membership-actions.json"), "[]");

                Log.Info("JsonMultiFileBackend initialized at {0}", _dataPath);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public Task<bool> ValidateConnectionAsync(CancellationToken ct = default)
        {
            try
            {
                if (!Directory.Exists(_dataPath))
                {
                    return Task.FromResult(false);
                }

                var testFile = Path.Combine(_dataPath, ".write-test");
                File.WriteAllText(testFile, "ok");
                File.Delete(testFile);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Storage validation failed");
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc/>
        public StorageInfo GetStorageInfo()
        {
            return new StorageInfo
            {
                BackendType = "JsonMultiFile",
                Location = _dataPath,
            };
        }

        // ═══════════════════════════════════════════════════════════
        // Server Factions
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<ServerFaction> GetFactionAsync(string uuid)
        {
            var factions = await ReadListAsync<ServerFaction>("factions.json");
            return factions.FirstOrDefault(f => f.UUID == uuid);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ServerFaction>> GetAllFactionsAsync()
        {
            return await ReadListAsync<ServerFaction>("factions.json");
        }

        /// <inheritdoc/>
        public async Task UpsertFactionAsync(ServerFaction faction)
        {
            await UpsertInListAsync<ServerFaction>("factions.json", faction, f => f.UUID == faction.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteFactionAsync(string uuid)
        {
            await DeleteFromListAsync<ServerFaction>("factions.json", f => f.UUID == uuid);
        }

        // ═══════════════════════════════════════════════════════════
        // Server Characters
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<ServerCharacter> GetCharacterAsync(string uuid)
        {
            var characters = await ReadListAsync<ServerCharacter>("characters.json");
            return characters.FirstOrDefault(c => c.UUID == uuid);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ServerCharacter>> GetAllCharactersAsync()
        {
            return await ReadListAsync<ServerCharacter>("characters.json");
        }

        /// <inheritdoc/>
        public async Task UpsertCharacterAsync(ServerCharacter character)
        {
            await UpsertInListAsync<ServerCharacter>("characters.json", character, c => c.UUID == character.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteCharacterAsync(string uuid)
        {
            await DeleteFromListAsync<ServerCharacter>("characters.json", c => c.UUID == uuid);
        }

        // ═══════════════════════════════════════════════════════════
        // Global Data
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<string> GetGlobalDataAsync(string dataType)
        {
            var path = Path.Combine(_dataPath, "global", $"{dataType}.json");
            return await ReadRawAsync(path);
        }

        /// <inheritdoc/>
        public async Task UpsertGlobalDataAsync(string dataType, string json)
        {
            var path = Path.Combine(_dataPath, "global", $"{dataType}.json");
            await WriteAtomicAsync(path, json);
        }

        // ═══════════════════════════════════════════════════════════
        // Star Systems
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<IReadOnlyList<StarSystem>> GetAllStarSystemsAsync()
        {
            var path = Path.Combine(_dataPath, "global", "star-systems.json");
            await _lock.WaitAsync();
            try
            {
                return await ReadListUnlockedAsync<StarSystem>(path);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task UpsertStarSystemsAsync(IReadOnlyList<StarSystem> systems)
        {
            var path = Path.Combine(_dataPath, "global", "star-systems.json");
            var json = JsonConvert.SerializeObject(systems, SerializerSettings);
            await WriteAtomicAsync(path, json);
        }

        // ═══════════════════════════════════════════════════════════
        // Colony Summaries
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ColonySummary>> GetColonySummariesForSystemAsync(int systemId)
        {
            await _lock.WaitAsync();
            try
            {
                var systemsPath = Path.Combine(_dataPath, "global", "star-systems.json");
                var systems = await ReadListUnlockedAsync<StarSystem>(systemsPath);
                var system = systems.FirstOrDefault(s => s.Id == systemId);
                if (system == null)
                {
                    return Array.Empty<ColonySummary>();
                }

                var systemName = system.Name;
                var results = new List<ColonySummary>();

                var charsDir = Path.Combine(_dataPath, "characters");
                if (!Directory.Exists(charsDir))
                {
                    return results;
                }

                foreach (var dir in Directory.GetDirectories(charsDir))
                {
                    var coloniesPath = Path.Combine(dir, "colonies.json");
                    var colonies = await ReadListUnlockedAsync<Colony>(coloniesPath);
                    foreach (var colony in colonies)
                    {
                        if (string.Equals(colony.SystemName, systemName, StringComparison.OrdinalIgnoreCase))
                        {
                            results.Add(new ColonySummary
                            {
                                ColonyName = colony.ColonyName ?? string.Empty,
                                Size = colony.Structures?.Count ?? 0,
                                PlanetName = colony.PlanetName ?? string.Empty,
                            });
                        }
                    }
                }

                return results;
            }
            finally
            {
                _lock.Release();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // API Tokens
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<ApiToken> FindTokenByHashAsync(string tokenHash)
        {
            var tokens = await ReadListAsync<ApiToken>("tokens.json");
            return tokens.FirstOrDefault(t => t.TokenHash == tokenHash);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ApiToken>> GetAllTokensAsync()
        {
            return await ReadListAsync<ApiToken>("tokens.json");
        }

        /// <inheritdoc/>
        public async Task UpsertTokenAsync(ApiToken token)
        {
            await UpsertInListAsync<ApiToken>("tokens.json", token, t => t.Id == token.Id);
        }

        /// <inheritdoc/>
        public async Task DeleteTokenAsync(string id)
        {
            await DeleteFromListAsync<ApiToken>("tokens.json", t => t.Id == id);
        }

        // ═══════════════════════════════════════════════════════════
        // Membership Actions
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<IReadOnlyList<MembershipAction>> GetFactionActionsAsync(string factionUUID)
        {
            var actions = await ReadListAsync<MembershipAction>("membership-actions.json");
            return actions.Where(a => a.FactionUUID == factionUUID).ToList();
        }

        /// <inheritdoc/>
        public async Task UpsertMembershipActionAsync(MembershipAction action)
        {
            await UpsertInListAsync<MembershipAction>("membership-actions.json", action, a => a.Id == action.Id);
        }

        /// <inheritdoc/>
        public async Task DeleteMembershipActionAsync(string id)
        {
            await DeleteFromListAsync<MembershipAction>("membership-actions.json", a => a.Id == id);
        }

        /// <inheritdoc/>
        public async Task DeleteExpiredActionsAsync(DateTime cutoff)
        {
            await _lock.WaitAsync();
            try
            {
                var path = DataFilePath("membership-actions.json");
                var list = await ReadListUnlockedAsync<MembershipAction>(path);
                var filtered = list.Where(a => a.ExpiresUtc > cutoff).ToList();
                var json = JsonConvert.SerializeObject(filtered, SerializerSettings);
                await WriteAtomicUnlockedAsync(path, json);
            }
            finally
            {
                _lock.Release();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // Sharing Rules
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<IReadOnlyList<SharingRule>> GetSharingRulesForCharacterAsync(string characterUUID)
        {
            var path = Path.Combine(_dataPath, "characters", characterUUID, "sharing.json");
            var json = await ReadRawAsync(path);
            if (json == null)
            {
                return Array.Empty<SharingRule>();
            }

            return JsonConvert.DeserializeObject<List<SharingRule>>(json) ?? new List<SharingRule>();
        }

        /// <inheritdoc/>
        public async Task UpsertSharingRulesAsync(string characterUUID, IReadOnlyList<SharingRule> rules)
        {
            EnsureCharacterDirectory(characterUUID);
            var path = Path.Combine(_dataPath, "characters", characterUUID, "sharing.json");
            var json = JsonConvert.SerializeObject(rules, SerializerSettings);
            await WriteAtomicAsync(path, json);
        }

        // ═══════════════════════════════════════════════════════════
        // Character Preferences
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<CharacterPreferences> GetCharacterPreferencesAsync(string characterUUID)
        {
            var path = Path.Combine(_dataPath, "characters", characterUUID, "preferences.json");
            var json = await ReadRawAsync(path);
            if (json == null)
            {
                return null;
            }

            return JsonConvert.DeserializeObject<CharacterPreferences>(json);
        }

        /// <inheritdoc/>
        public async Task UpsertCharacterPreferencesAsync(CharacterPreferences prefs)
        {
            EnsureCharacterDirectory(prefs.CharacterUUID);
            var path = Path.Combine(_dataPath, "characters", prefs.CharacterUUID, "preferences.json");
            var json = JsonConvert.SerializeObject(prefs, SerializerSettings);
            await WriteAtomicAsync(path, json);
        }

        // ═══════════════════════════════════════════════════════════
        // Faction Permission Entities
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<IReadOnlyList<FactionCapability>> GetFactionCapabilitiesAsync(string factionUUID)
        {
            return await ReadFactionListAsync<FactionCapability>(factionUUID, "capabilities.json");
        }

        /// <inheritdoc/>
        public async Task UpsertFactionCapabilityAsync(FactionCapability capability)
        {
            await _lock.WaitAsync();
            try
            {
                EnsureFactionDirectory(capability.FactionUUID);
                var path = FactionDataPath(capability.FactionUUID, "capabilities.json");
                var list = await ReadListUnlockedAsync<FactionCapability>(path);
                var index = list.FindIndex(c => c.UUID == capability.UUID);
                if (index >= 0)
                {
                    list[index] = capability;
                }
                else
                {
                    list.Add(capability);
                }

                var json = JsonConvert.SerializeObject(list, SerializerSettings);
                await WriteAtomicUnlockedAsync(path, json);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task DeleteFactionCapabilityAsync(string factionUUID, string capabilityUUID)
        {
            await _lock.WaitAsync();
            try
            {
                var path = FactionDataPath(factionUUID, "capabilities.json");
                var list = await ReadListUnlockedAsync<FactionCapability>(path);
                var removed = list.RemoveAll(c => c.UUID == capabilityUUID);
                if (removed > 0)
                {
                    var json = JsonConvert.SerializeObject(list, SerializerSettings);
                    await WriteAtomicUnlockedAsync(path, json);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<FactionClearanceLevel>> GetFactionClearanceLevelsAsync(string factionUUID)
        {
            return await ReadFactionListAsync<FactionClearanceLevel>(factionUUID, "clearance-levels.json");
        }

        /// <inheritdoc/>
        public async Task UpsertFactionClearanceLevelAsync(FactionClearanceLevel level)
        {
            await _lock.WaitAsync();
            try
            {
                EnsureFactionDirectory(level.FactionUUID);
                var path = FactionDataPath(level.FactionUUID, "clearance-levels.json");
                var list = await ReadListUnlockedAsync<FactionClearanceLevel>(path);
                var index = list.FindIndex(l => l.UUID == level.UUID);
                if (index >= 0)
                {
                    list[index] = level;
                }
                else
                {
                    list.Add(level);
                }

                var json = JsonConvert.SerializeObject(list, SerializerSettings);
                await WriteAtomicUnlockedAsync(path, json);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task DeleteFactionClearanceLevelAsync(string factionUUID, string levelUUID)
        {
            await _lock.WaitAsync();
            try
            {
                var path = FactionDataPath(factionUUID, "clearance-levels.json");
                var list = await ReadListUnlockedAsync<FactionClearanceLevel>(path);
                var removed = list.RemoveAll(l => l.UUID == levelUUID);
                if (removed > 0)
                {
                    var json = JsonConvert.SerializeObject(list, SerializerSettings);
                    await WriteAtomicUnlockedAsync(path, json);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<FactionPermissionGroup>> GetFactionGroupsAsync(string factionUUID)
        {
            return await ReadFactionListAsync<FactionPermissionGroup>(factionUUID, "groups.json");
        }

        /// <inheritdoc/>
        public async Task<FactionPermissionGroup> GetFactionGroupAsync(string factionUUID, string groupUUID)
        {
            var groups = await ReadFactionListAsync<FactionPermissionGroup>(factionUUID, "groups.json");
            return groups.FirstOrDefault(g => g.UUID == groupUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertFactionGroupAsync(FactionPermissionGroup group)
        {
            await _lock.WaitAsync();
            try
            {
                EnsureFactionDirectory(group.FactionUUID);
                var path = FactionDataPath(group.FactionUUID, "groups.json");
                var list = await ReadListUnlockedAsync<FactionPermissionGroup>(path);
                var index = list.FindIndex(g => g.UUID == group.UUID);
                if (index >= 0)
                {
                    list[index] = group;
                }
                else
                {
                    list.Add(group);
                }

                var json = JsonConvert.SerializeObject(list, SerializerSettings);
                await WriteAtomicUnlockedAsync(path, json);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task DeleteFactionGroupAsync(string factionUUID, string groupUUID)
        {
            await _lock.WaitAsync();
            try
            {
                var path = FactionDataPath(factionUUID, "groups.json");
                var list = await ReadListUnlockedAsync<FactionPermissionGroup>(path);
                var removed = list.RemoveAll(g => g.UUID == groupUUID);
                if (removed > 0)
                {
                    var json = JsonConvert.SerializeObject(list, SerializerSettings);
                    await WriteAtomicUnlockedAsync(path, json);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<FactionGroupCapability>> GetFactionGroupCapabilitiesAsync(string groupUUID)
        {
            await _lock.WaitAsync();
            try
            {
                var allFiles = FindFactionFilesForGroup("group-capabilities.json");
                var results = new List<FactionGroupCapability>();
                foreach (var file in allFiles)
                {
                    var list = await ReadListUnlockedAsync<FactionGroupCapability>(file);
                    results.AddRange(list.Where(c => c.GroupUUID == groupUUID));
                }

                return results;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task AddFactionGroupCapabilityAsync(FactionGroupCapability item)
        {
            await _lock.WaitAsync();
            try
            {
                var factionUUID = await FindFactionForGroupUnlockedAsync(item.GroupUUID);
                if (factionUUID == null)
                {
                    return;
                }

                EnsureFactionDirectory(factionUUID);
                var path = FactionDataPath(factionUUID, "group-capabilities.json");
                var list = await ReadListUnlockedAsync<FactionGroupCapability>(path);
                if (!list.Any(c => c.GroupUUID == item.GroupUUID && c.CapabilityUUID == item.CapabilityUUID))
                {
                    list.Add(item);
                    var json = JsonConvert.SerializeObject(list, SerializerSettings);
                    await WriteAtomicUnlockedAsync(path, json);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task RemoveFactionGroupCapabilityAsync(string groupUUID, string capabilityUUID)
        {
            await _lock.WaitAsync();
            try
            {
                var factionUUID = await FindFactionForGroupUnlockedAsync(groupUUID);
                if (factionUUID == null)
                {
                    return;
                }

                var path = FactionDataPath(factionUUID, "group-capabilities.json");
                var list = await ReadListUnlockedAsync<FactionGroupCapability>(path);
                var removed = list.RemoveAll(c => c.GroupUUID == groupUUID && c.CapabilityUUID == capabilityUUID);
                if (removed > 0)
                {
                    var json = JsonConvert.SerializeObject(list, SerializerSettings);
                    await WriteAtomicUnlockedAsync(path, json);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<FactionGroupSharingRule>> GetFactionGroupSharingRulesAsync(string groupUUID)
        {
            await _lock.WaitAsync();
            try
            {
                var allFiles = FindFactionFilesForGroup("group-sharing-rules.json");
                var results = new List<FactionGroupSharingRule>();
                foreach (var file in allFiles)
                {
                    var list = await ReadListUnlockedAsync<FactionGroupSharingRule>(file);
                    results.AddRange(list.Where(r => r.GroupUUID == groupUUID));
                }

                return results;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task UpsertFactionGroupSharingRuleAsync(FactionGroupSharingRule rule)
        {
            await _lock.WaitAsync();
            try
            {
                var factionUUID = await FindFactionForGroupUnlockedAsync(rule.GroupUUID);
                if (factionUUID == null)
                {
                    return;
                }

                EnsureFactionDirectory(factionUUID);
                var path = FactionDataPath(factionUUID, "group-sharing-rules.json");
                var list = await ReadListUnlockedAsync<FactionGroupSharingRule>(path);
                var index = list.FindIndex(r => r.UUID == rule.UUID);
                if (index >= 0)
                {
                    list[index] = rule;
                }
                else
                {
                    list.Add(rule);
                }

                var json = JsonConvert.SerializeObject(list, SerializerSettings);
                await WriteAtomicUnlockedAsync(path, json);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task DeleteFactionGroupSharingRuleAsync(string groupUUID, string ruleUUID)
        {
            await _lock.WaitAsync();
            try
            {
                var factionUUID = await FindFactionForGroupUnlockedAsync(groupUUID);
                if (factionUUID == null)
                {
                    return;
                }

                var path = FactionDataPath(factionUUID, "group-sharing-rules.json");
                var list = await ReadListUnlockedAsync<FactionGroupSharingRule>(path);
                var removed = list.RemoveAll(r => r.GroupUUID == groupUUID && r.UUID == ruleUUID);
                if (removed > 0)
                {
                    var json = JsonConvert.SerializeObject(list, SerializerSettings);
                    await WriteAtomicUnlockedAsync(path, json);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<FactionMemberPermissions> GetFactionMemberPermissionsAsync(string factionUUID, string characterUUID)
        {
            var list = await ReadFactionListAsync<FactionMemberPermissions>(factionUUID, "member-permissions.json");
            return list.FirstOrDefault(p => p.CharacterUUID == characterUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertFactionMemberPermissionsAsync(FactionMemberPermissions perms)
        {
            await _lock.WaitAsync();
            try
            {
                EnsureFactionDirectory(perms.FactionUUID);
                var path = FactionDataPath(perms.FactionUUID, "member-permissions.json");
                var list = await ReadListUnlockedAsync<FactionMemberPermissions>(path);
                var index = list.FindIndex(p => p.CharacterUUID == perms.CharacterUUID);
                if (index >= 0)
                {
                    list[index] = perms;
                }
                else
                {
                    list.Add(perms);
                }

                var json = JsonConvert.SerializeObject(list, SerializerSettings);
                await WriteAtomicUnlockedAsync(path, json);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<FactionMemberPermissions>> GetAllFactionMembersPermissionsAsync(string factionUUID)
        {
            return await ReadFactionListAsync<FactionMemberPermissions>(factionUUID, "member-permissions.json");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<FactionMemberCapability>> GetFactionMemberCapabilitiesAsync(string factionUUID, string characterUUID)
        {
            var list = await ReadFactionListAsync<FactionMemberCapability>(factionUUID, "member-capabilities.json");
            return list.Where(c => c.CharacterUUID == characterUUID).ToList();
        }

        /// <inheritdoc/>
        public async Task AddFactionMemberCapabilityAsync(FactionMemberCapability item)
        {
            await _lock.WaitAsync();
            try
            {
                EnsureFactionDirectory(item.FactionUUID);
                var path = FactionDataPath(item.FactionUUID, "member-capabilities.json");
                var list = await ReadListUnlockedAsync<FactionMemberCapability>(path);
                if (!list.Any(c => c.CharacterUUID == item.CharacterUUID && c.CapabilityUUID == item.CapabilityUUID))
                {
                    list.Add(item);
                    var json = JsonConvert.SerializeObject(list, SerializerSettings);
                    await WriteAtomicUnlockedAsync(path, json);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task RemoveFactionMemberCapabilityAsync(string factionUUID, string characterUUID, string capabilityUUID)
        {
            await _lock.WaitAsync();
            try
            {
                var path = FactionDataPath(factionUUID, "member-capabilities.json");
                var list = await ReadListUnlockedAsync<FactionMemberCapability>(path);
                var removed = list.RemoveAll(c => c.CharacterUUID == characterUUID && c.CapabilityUUID == capabilityUUID);
                if (removed > 0)
                {
                    var json = JsonConvert.SerializeObject(list, SerializerSettings);
                    await WriteAtomicUnlockedAsync(path, json);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // Character Permission Entities
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<IReadOnlyList<CharacterCapability>> GetCharacterCapabilitiesAsync(string characterUUID)
        {
            return await ReadCharacterPermListAsync<CharacterCapability>(characterUUID, "perm-capabilities.json");
        }

        /// <inheritdoc/>
        public async Task UpsertCharacterCapabilityAsync(CharacterCapability capability)
        {
            await _lock.WaitAsync();
            try
            {
                EnsureCharacterDirectory(capability.OwnerCharacterUUID);
                var path = CharacterPermPath(capability.OwnerCharacterUUID, "perm-capabilities.json");
                var list = await ReadListUnlockedAsync<CharacterCapability>(path);
                var index = list.FindIndex(c => c.UUID == capability.UUID);
                if (index >= 0)
                {
                    list[index] = capability;
                }
                else
                {
                    list.Add(capability);
                }

                var json = JsonConvert.SerializeObject(list, SerializerSettings);
                await WriteAtomicUnlockedAsync(path, json);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task DeleteCharacterCapabilityAsync(string characterUUID, string capabilityUUID)
        {
            await _lock.WaitAsync();
            try
            {
                var path = CharacterPermPath(characterUUID, "perm-capabilities.json");
                var list = await ReadListUnlockedAsync<CharacterCapability>(path);
                var removed = list.RemoveAll(c => c.UUID == capabilityUUID);
                if (removed > 0)
                {
                    var json = JsonConvert.SerializeObject(list, SerializerSettings);
                    await WriteAtomicUnlockedAsync(path, json);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<CharacterClearanceLevel>> GetCharacterClearanceLevelsAsync(string characterUUID)
        {
            return await ReadCharacterPermListAsync<CharacterClearanceLevel>(characterUUID, "perm-clearance-levels.json");
        }

        /// <inheritdoc/>
        public async Task UpsertCharacterClearanceLevelAsync(CharacterClearanceLevel level)
        {
            await _lock.WaitAsync();
            try
            {
                EnsureCharacterDirectory(level.OwnerCharacterUUID);
                var path = CharacterPermPath(level.OwnerCharacterUUID, "perm-clearance-levels.json");
                var list = await ReadListUnlockedAsync<CharacterClearanceLevel>(path);
                var index = list.FindIndex(l => l.UUID == level.UUID);
                if (index >= 0)
                {
                    list[index] = level;
                }
                else
                {
                    list.Add(level);
                }

                var json = JsonConvert.SerializeObject(list, SerializerSettings);
                await WriteAtomicUnlockedAsync(path, json);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task DeleteCharacterClearanceLevelAsync(string characterUUID, string levelUUID)
        {
            await _lock.WaitAsync();
            try
            {
                var path = CharacterPermPath(characterUUID, "perm-clearance-levels.json");
                var list = await ReadListUnlockedAsync<CharacterClearanceLevel>(path);
                var removed = list.RemoveAll(l => l.UUID == levelUUID);
                if (removed > 0)
                {
                    var json = JsonConvert.SerializeObject(list, SerializerSettings);
                    await WriteAtomicUnlockedAsync(path, json);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<CharacterPermissionGroup>> GetCharacterGroupsAsync(string characterUUID)
        {
            return await ReadCharacterPermListAsync<CharacterPermissionGroup>(characterUUID, "perm-groups.json");
        }

        /// <inheritdoc/>
        public async Task<CharacterPermissionGroup> GetCharacterGroupAsync(string characterUUID, string groupUUID)
        {
            var groups = await ReadCharacterPermListAsync<CharacterPermissionGroup>(characterUUID, "perm-groups.json");
            return groups.FirstOrDefault(g => g.UUID == groupUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertCharacterGroupAsync(CharacterPermissionGroup group)
        {
            await _lock.WaitAsync();
            try
            {
                EnsureCharacterDirectory(group.OwnerCharacterUUID);
                var path = CharacterPermPath(group.OwnerCharacterUUID, "perm-groups.json");
                var list = await ReadListUnlockedAsync<CharacterPermissionGroup>(path);
                var index = list.FindIndex(g => g.UUID == group.UUID);
                if (index >= 0)
                {
                    list[index] = group;
                }
                else
                {
                    list.Add(group);
                }

                var json = JsonConvert.SerializeObject(list, SerializerSettings);
                await WriteAtomicUnlockedAsync(path, json);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task DeleteCharacterGroupAsync(string characterUUID, string groupUUID)
        {
            await _lock.WaitAsync();
            try
            {
                var path = CharacterPermPath(characterUUID, "perm-groups.json");
                var list = await ReadListUnlockedAsync<CharacterPermissionGroup>(path);
                var removed = list.RemoveAll(g => g.UUID == groupUUID);
                if (removed > 0)
                {
                    var json = JsonConvert.SerializeObject(list, SerializerSettings);
                    await WriteAtomicUnlockedAsync(path, json);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<CharacterGroupCapability>> GetCharacterGroupCapabilitiesAsync(string groupUUID)
        {
            await _lock.WaitAsync();
            try
            {
                var allFiles = FindCharacterFilesForGroup("perm-group-capabilities.json");
                var results = new List<CharacterGroupCapability>();
                foreach (var file in allFiles)
                {
                    var list = await ReadListUnlockedAsync<CharacterGroupCapability>(file);
                    results.AddRange(list.Where(c => c.GroupUUID == groupUUID));
                }

                return results;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task AddCharacterGroupCapabilityAsync(CharacterGroupCapability item)
        {
            await _lock.WaitAsync();
            try
            {
                var ownerUUID = await FindCharacterForGroupUnlockedAsync(item.GroupUUID);
                if (ownerUUID == null)
                {
                    return;
                }

                EnsureCharacterDirectory(ownerUUID);
                var path = CharacterPermPath(ownerUUID, "perm-group-capabilities.json");
                var list = await ReadListUnlockedAsync<CharacterGroupCapability>(path);
                if (!list.Any(c => c.GroupUUID == item.GroupUUID && c.CapabilityUUID == item.CapabilityUUID))
                {
                    list.Add(item);
                    var json = JsonConvert.SerializeObject(list, SerializerSettings);
                    await WriteAtomicUnlockedAsync(path, json);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task RemoveCharacterGroupCapabilityAsync(string groupUUID, string capabilityUUID)
        {
            await _lock.WaitAsync();
            try
            {
                var ownerUUID = await FindCharacterForGroupUnlockedAsync(groupUUID);
                if (ownerUUID == null)
                {
                    return;
                }

                var path = CharacterPermPath(ownerUUID, "perm-group-capabilities.json");
                var list = await ReadListUnlockedAsync<CharacterGroupCapability>(path);
                var removed = list.RemoveAll(c => c.GroupUUID == groupUUID && c.CapabilityUUID == capabilityUUID);
                if (removed > 0)
                {
                    var json = JsonConvert.SerializeObject(list, SerializerSettings);
                    await WriteAtomicUnlockedAsync(path, json);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<CharacterGroupSharingRule>> GetCharacterGroupSharingRulesAsync(string groupUUID)
        {
            await _lock.WaitAsync();
            try
            {
                var allFiles = FindCharacterFilesForGroup("perm-group-sharing-rules.json");
                var results = new List<CharacterGroupSharingRule>();
                foreach (var file in allFiles)
                {
                    var list = await ReadListUnlockedAsync<CharacterGroupSharingRule>(file);
                    results.AddRange(list.Where(r => r.GroupUUID == groupUUID));
                }

                return results;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task UpsertCharacterGroupSharingRuleAsync(CharacterGroupSharingRule rule)
        {
            await _lock.WaitAsync();
            try
            {
                var ownerUUID = await FindCharacterForGroupUnlockedAsync(rule.GroupUUID);
                if (ownerUUID == null)
                {
                    return;
                }

                EnsureCharacterDirectory(ownerUUID);
                var path = CharacterPermPath(ownerUUID, "perm-group-sharing-rules.json");
                var list = await ReadListUnlockedAsync<CharacterGroupSharingRule>(path);
                var index = list.FindIndex(r => r.UUID == rule.UUID);
                if (index >= 0)
                {
                    list[index] = rule;
                }
                else
                {
                    list.Add(rule);
                }

                var json = JsonConvert.SerializeObject(list, SerializerSettings);
                await WriteAtomicUnlockedAsync(path, json);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task DeleteCharacterGroupSharingRuleAsync(string groupUUID, string ruleUUID)
        {
            await _lock.WaitAsync();
            try
            {
                var ownerUUID = await FindCharacterForGroupUnlockedAsync(groupUUID);
                if (ownerUUID == null)
                {
                    return;
                }

                var path = CharacterPermPath(ownerUUID, "perm-group-sharing-rules.json");
                var list = await ReadListUnlockedAsync<CharacterGroupSharingRule>(path);
                var removed = list.RemoveAll(r => r.GroupUUID == groupUUID && r.UUID == ruleUUID);
                if (removed > 0)
                {
                    var json = JsonConvert.SerializeObject(list, SerializerSettings);
                    await WriteAtomicUnlockedAsync(path, json);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<CharacterGranteePermissions>> GetCharacterGranteesAsync(string ownerCharacterUUID)
        {
            return await ReadCharacterPermListAsync<CharacterGranteePermissions>(ownerCharacterUUID, "perm-grantees.json");
        }

        /// <inheritdoc/>
        public async Task UpsertCharacterGranteePermissionsAsync(CharacterGranteePermissions perms)
        {
            await _lock.WaitAsync();
            try
            {
                EnsureCharacterDirectory(perms.OwnerCharacterUUID);
                var path = CharacterPermPath(perms.OwnerCharacterUUID, "perm-grantees.json");
                var list = await ReadListUnlockedAsync<CharacterGranteePermissions>(path);
                var index = list.FindIndex(p => p.GranteeUUID == perms.GranteeUUID);
                if (index >= 0)
                {
                    list[index] = perms;
                }
                else
                {
                    list.Add(perms);
                }

                var json = JsonConvert.SerializeObject(list, SerializerSettings);
                await WriteAtomicUnlockedAsync(path, json);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task DeleteCharacterGranteePermissionsAsync(string ownerCharacterUUID, string granteeUUID)
        {
            await _lock.WaitAsync();
            try
            {
                var path = CharacterPermPath(ownerCharacterUUID, "perm-grantees.json");
                var list = await ReadListUnlockedAsync<CharacterGranteePermissions>(path);
                var removed = list.RemoveAll(p => p.GranteeUUID == granteeUUID);
                if (removed > 0)
                {
                    var json = JsonConvert.SerializeObject(list, SerializerSettings);
                    await WriteAtomicUnlockedAsync(path, json);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<CharacterGranteeCapability>> GetCharacterGranteeCapabilitiesAsync(string ownerCharacterUUID, string granteeUUID)
        {
            var list = await ReadCharacterPermListAsync<CharacterGranteeCapability>(ownerCharacterUUID, "perm-grantee-capabilities.json");
            return list.Where(c => c.GranteeUUID == granteeUUID).ToList();
        }

        /// <inheritdoc/>
        public async Task AddCharacterGranteeCapabilityAsync(CharacterGranteeCapability item)
        {
            await _lock.WaitAsync();
            try
            {
                EnsureCharacterDirectory(item.OwnerCharacterUUID);
                var path = CharacterPermPath(item.OwnerCharacterUUID, "perm-grantee-capabilities.json");
                var list = await ReadListUnlockedAsync<CharacterGranteeCapability>(path);
                if (!list.Any(c => c.GranteeUUID == item.GranteeUUID && c.CapabilityUUID == item.CapabilityUUID))
                {
                    list.Add(item);
                    var json = JsonConvert.SerializeObject(list, SerializerSettings);
                    await WriteAtomicUnlockedAsync(path, json);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task RemoveCharacterGranteeCapabilityAsync(string ownerCharacterUUID, string granteeUUID, string capabilityUUID)
        {
            await _lock.WaitAsync();
            try
            {
                var path = CharacterPermPath(ownerCharacterUUID, "perm-grantee-capabilities.json");
                var list = await ReadListUnlockedAsync<CharacterGranteeCapability>(path);
                var removed = list.RemoveAll(c => c.GranteeUUID == granteeUUID && c.CapabilityUUID == capabilityUUID);
                if (removed > 0)
                {
                    var json = JsonConvert.SerializeObject(list, SerializerSettings);
                    await WriteAtomicUnlockedAsync(path, json);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // Intel and Audit
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<IReadOnlyList<IntelComment>> GetIntelCommentsForTargetAsync(string targetCharacterUUID)
        {
            var list = await ReadListAsync<IntelComment>("intel-comments.json");
            return list.Where(c => c.TargetCharacterUUID == targetCharacterUUID).ToList();
        }

        /// <inheritdoc/>
        public async Task<IntelComment> GetIntelCommentAsync(string commentUUID)
        {
            var list = await ReadListAsync<IntelComment>("intel-comments.json");
            return list.FirstOrDefault(c => c.UUID == commentUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertIntelCommentAsync(IntelComment comment)
        {
            await UpsertInListAsync<IntelComment>("intel-comments.json", comment, c => c.UUID == comment.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteIntelCommentAsync(string commentUUID)
        {
            await DeleteFromListAsync<IntelComment>("intel-comments.json", c => c.UUID == commentUUID);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForCommentAsync(string commentUUID)
        {
            var list = await ReadListAsync<IntelCommentFactionShare>("intel-shares.json");
            return list.Where(s => s.IntelCommentUUID == commentUUID).ToList();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForFactionAsync(string factionUUID)
        {
            var list = await ReadListAsync<IntelCommentFactionShare>("intel-shares.json");
            return list.Where(s => s.FactionUUID == factionUUID).ToList();
        }

        /// <inheritdoc/>
        public async Task UpsertIntelShareAsync(IntelCommentFactionShare share)
        {
            await UpsertInListAsync<IntelCommentFactionShare>("intel-shares.json", share, s => s.UUID == share.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteIntelShareAsync(string shareUUID)
        {
            await DeleteFromListAsync<IntelCommentFactionShare>("intel-shares.json", s => s.UUID == shareUUID);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<PermissionAuditEntry>> GetPermissionAuditEntriesAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            PermissionActionType? actionType = null,
            string actorUUID = null,
            string targetUUID = null)
        {
            var list = await ReadListAsync<PermissionAuditEntry>("permission-audit.json");
            IEnumerable<PermissionAuditEntry> filtered = list;

            if (startDate.HasValue)
            {
                filtered = filtered.Where(e => e.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                filtered = filtered.Where(e => e.Timestamp <= endDate.Value);
            }

            if (actionType.HasValue)
            {
                filtered = filtered.Where(e => e.ActionType == actionType.Value);
            }

            if (actorUUID != null)
            {
                filtered = filtered.Where(e => e.ActorCharacterUUID == actorUUID);
            }

            if (targetUUID != null)
            {
                filtered = filtered.Where(e => e.TargetCharacterUUID == targetUUID);
            }

            return filtered.ToList();
        }

        /// <inheritdoc/>
        public async Task AppendPermissionAuditEntryAsync(PermissionAuditEntry entry)
        {
            await _lock.WaitAsync();
            try
            {
                var path = DataFilePath("permission-audit.json");
                var list = await ReadListUnlockedAsync<PermissionAuditEntry>(path);
                list.Add(entry);
                var json = JsonConvert.SerializeObject(list, SerializerSettings);
                await WriteAtomicUnlockedAsync(path, json);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task DeleteExpiredAuditEntriesAsync(DateTime cutoff)
        {
            await _lock.WaitAsync();
            try
            {
                var path = DataFilePath("permission-audit.json");
                var list = await ReadListUnlockedAsync<PermissionAuditEntry>(path);
                var filtered = list.Where(e => e.Timestamp > cutoff).ToList();
                var json = JsonConvert.SerializeObject(filtered, SerializerSettings);
                await WriteAtomicUnlockedAsync(path, json);
            }
            finally
            {
                _lock.Release();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD
        // ═══════════════════════════════════════════════════════════

        // Colony

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Colony>> GetAllColoniesAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<Colony>(characterUUID, "colonies");
        }

        /// <inheritdoc/>
        public async Task<Colony> GetColonyAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllColoniesAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertColonyAsync(string characterUUID, Colony entity)
        {
            await UpsertEntityAsync(characterUUID, "colonies", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteColonyAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<Colony>(characterUUID, "colonies", e => e.UUID == entityUUID);
        }

        // Blueprint

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Blueprint>> GetAllBlueprintsAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<Blueprint>(characterUUID, "blueprints");
        }

        /// <inheritdoc/>
        public async Task<Blueprint> GetBlueprintAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllBlueprintsAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertBlueprintAsync(string characterUUID, Blueprint entity)
        {
            await UpsertEntityAsync(characterUUID, "blueprints", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteBlueprintAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<Blueprint>(characterUUID, "blueprints", e => e.UUID == entityUUID);
        }

        // Survey

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Survey>> GetAllSurveysAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<Survey>(characterUUID, "surveys");
        }

        /// <inheritdoc/>
        public async Task<Survey> GetSurveyAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllSurveysAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertSurveyAsync(string characterUUID, Survey entity)
        {
            await UpsertEntityAsync(characterUUID, "surveys", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteSurveyAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<Survey>(characterUUID, "surveys", e => e.UUID == entityUUID);
        }

        // PlayerProfile

        /// <inheritdoc/>
        public async Task<IReadOnlyList<PlayerProfile>> GetAllPlayerProfilesAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<PlayerProfile>(characterUUID, "playerprofile");
        }

        /// <inheritdoc/>
        public async Task<PlayerProfile> GetPlayerProfileAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllPlayerProfilesAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertPlayerProfileAsync(string characterUUID, PlayerProfile entity)
        {
            await UpsertEntityAsync(characterUUID, "playerprofile", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeletePlayerProfileAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<PlayerProfile>(characterUUID, "playerprofile", e => e.UUID == entityUUID);
        }

        // DeliveryRoute

        /// <inheritdoc/>
        public async Task<IReadOnlyList<DeliveryRoute>> GetAllDeliveryRoutesAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<DeliveryRoute>(characterUUID, "deliveryroutes");
        }

        /// <inheritdoc/>
        public async Task<DeliveryRoute> GetDeliveryRouteAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllDeliveryRoutesAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertDeliveryRouteAsync(string characterUUID, DeliveryRoute entity)
        {
            await UpsertEntityAsync(characterUUID, "deliveryroutes", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteDeliveryRouteAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<DeliveryRoute>(characterUUID, "deliveryroutes", e => e.UUID == entityUUID);
        }

        // DeliveryPlan

        /// <inheritdoc/>
        public async Task<IReadOnlyList<DeliveryPlan>> GetAllDeliveryPlansAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<DeliveryPlan>(characterUUID, "deliveryplans");
        }

        /// <inheritdoc/>
        public async Task<DeliveryPlan> GetDeliveryPlanAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllDeliveryPlansAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertDeliveryPlanAsync(string characterUUID, DeliveryPlan entity)
        {
            await UpsertEntityAsync(characterUUID, "deliveryplans", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteDeliveryPlanAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<DeliveryPlan>(characterUUID, "deliveryplans", e => e.UUID == entityUUID);
        }

        // Ship

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Ship>> GetAllShipsAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<Ship>(characterUUID, "ships");
        }

        /// <inheritdoc/>
        public async Task<Ship> GetShipAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllShipsAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertShipAsync(string characterUUID, Ship entity)
        {
            await UpsertEntityAsync(characterUUID, "ships", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteShipAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<Ship>(characterUUID, "ships", e => e.UUID == entityUUID);
        }

        // ShipTemplate

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ShipTemplate>> GetAllShipTemplatesAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<ShipTemplate>(characterUUID, "shiptemplates");
        }

        /// <inheritdoc/>
        public async Task<ShipTemplate> GetShipTemplateAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllShipTemplatesAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertShipTemplateAsync(string characterUUID, ShipTemplate entity)
        {
            await UpsertEntityAsync(characterUUID, "shiptemplates", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteShipTemplateAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<ShipTemplate>(characterUUID, "shiptemplates", e => e.UUID == entityUUID);
        }

        // MarketListing

        /// <inheritdoc/>
        public async Task<IReadOnlyList<MarketListing>> GetAllMarketListingsAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<MarketListing>(characterUUID, "marketlistings");
        }

        /// <inheritdoc/>
        public async Task<MarketListing> GetMarketListingAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllMarketListingsAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertMarketListingAsync(string characterUUID, MarketListing entity)
        {
            await UpsertEntityAsync(characterUUID, "marketlistings", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteMarketListingAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<MarketListing>(characterUUID, "marketlistings", e => e.UUID == entityUUID);
        }

        // MarketTransaction

        /// <inheritdoc/>
        public async Task<IReadOnlyList<MarketTransaction>> GetAllMarketTransactionsAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<MarketTransaction>(characterUUID, "markettransactions");
        }

        /// <inheritdoc/>
        public async Task<MarketTransaction> GetMarketTransactionAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllMarketTransactionsAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertMarketTransactionAsync(string characterUUID, MarketTransaction entity)
        {
            await UpsertEntityAsync(characterUUID, "markettransactions", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteMarketTransactionAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<MarketTransaction>(characterUUID, "markettransactions", e => e.UUID == entityUUID);
        }

        // PricingPlan

        /// <inheritdoc/>
        public async Task<IReadOnlyList<PricingPlan>> GetAllPricingPlansAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<PricingPlan>(characterUUID, "pricingplans");
        }

        /// <inheritdoc/>
        public async Task<PricingPlan> GetPricingPlanAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllPricingPlansAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertPricingPlanAsync(string characterUUID, PricingPlan entity)
        {
            await UpsertEntityAsync(characterUUID, "pricingplans", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeletePricingPlanAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<PricingPlan>(characterUUID, "pricingplans", e => e.UUID == entityUUID);
        }

        // StockPlan

        /// <inheritdoc/>
        public async Task<IReadOnlyList<StockPlan>> GetAllStockPlansAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<StockPlan>(characterUUID, "stockplans");
        }

        /// <inheritdoc/>
        public async Task<StockPlan> GetStockPlanAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllStockPlansAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertStockPlanAsync(string characterUUID, StockPlan entity)
        {
            await UpsertEntityAsync(characterUUID, "stockplans", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteStockPlanAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<StockPlan>(characterUUID, "stockplans", e => e.UUID == entityUUID);
        }

        // StockProfile

        /// <inheritdoc/>
        public async Task<IReadOnlyList<StockProfile>> GetAllStockProfilesAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<StockProfile>(characterUUID, "stockprofiles");
        }

        /// <inheritdoc/>
        public async Task<StockProfile> GetStockProfileAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllStockProfilesAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertStockProfileAsync(string characterUUID, StockProfile entity)
        {
            await UpsertEntityAsync(characterUUID, "stockprofiles", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteStockProfileAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<StockProfile>(characterUUID, "stockprofiles", e => e.UUID == entityUUID);
        }

        // BuildPlan

        /// <inheritdoc/>
        public async Task<IReadOnlyList<BuildPlan>> GetAllBuildPlansAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<BuildPlan>(characterUUID, "buildplans");
        }

        /// <inheritdoc/>
        public async Task<BuildPlan> GetBuildPlanAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllBuildPlansAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertBuildPlanAsync(string characterUUID, BuildPlan entity)
        {
            await UpsertEntityAsync(characterUUID, "buildplans", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteBuildPlanAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<BuildPlan>(characterUUID, "buildplans", e => e.UUID == entityUUID);
        }

        // SupplyChain

        /// <inheritdoc/>
        public async Task<IReadOnlyList<SupplyChain>> GetAllSupplyChainsAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<SupplyChain>(characterUUID, "supplychains");
        }

        /// <inheritdoc/>
        public async Task<SupplyChain> GetSupplyChainAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllSupplyChainsAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertSupplyChainAsync(string characterUUID, SupplyChain entity)
        {
            await UpsertEntityAsync(characterUUID, "supplychains", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteSupplyChainAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<SupplyChain>(characterUUID, "supplychains", e => e.UUID == entityUUID);
        }

        // Asteroid

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Asteroid>> GetAllAsteroidsAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<Asteroid>(characterUUID, "asteroids");
        }

        /// <inheritdoc/>
        public async Task<Asteroid> GetAsteroidAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllAsteroidsAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertAsteroidAsync(string characterUUID, Asteroid entity)
        {
            await UpsertEntityAsync(characterUUID, "asteroids", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteAsteroidAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<Asteroid>(characterUUID, "asteroids", e => e.UUID == entityUUID);
        }

        // Station

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Station>> GetAllStationsAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<Station>(characterUUID, "stations");
        }

        /// <inheritdoc/>
        public async Task<Station> GetStationAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllStationsAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertStationAsync(string characterUUID, Station entity)
        {
            await UpsertEntityAsync(characterUUID, "stations", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteStationAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<Station>(characterUUID, "stations", e => e.UUID == entityUUID);
        }

        // Faction (per-character contacts)

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Faction>> GetAllFactionsForCharacterAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<Faction>(characterUUID, "faction");
        }

        /// <inheritdoc/>
        public async Task<Faction> GetFactionForCharacterAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllFactionsForCharacterAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertFactionForCharacterAsync(string characterUUID, Faction entity)
        {
            await UpsertEntityAsync(characterUUID, "faction", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteFactionForCharacterAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<Faction>(characterUUID, "faction", e => e.UUID == entityUUID);
        }

        // ExternalCharacter

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ExternalCharacter>> GetAllExternalCharactersAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<ExternalCharacter>(characterUUID, "externalcharacter");
        }

        /// <inheritdoc/>
        public async Task<ExternalCharacter> GetExternalCharacterAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllExternalCharactersAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertExternalCharacterAsync(string characterUUID, ExternalCharacter entity)
        {
            await UpsertEntityAsync(characterUUID, "externalcharacter", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteExternalCharacterAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<ExternalCharacter>(characterUUID, "externalcharacter", e => e.UUID == entityUUID);
        }

        // WarehouseOverflowRule

        /// <inheritdoc/>
        public async Task<IReadOnlyList<WarehouseOverflowRule>> GetAllWarehouseOverflowRulesAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<WarehouseOverflowRule>(characterUUID, "warehouseoverflowrules");
        }

        /// <inheritdoc/>
        public async Task<WarehouseOverflowRule> GetWarehouseOverflowRuleAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllWarehouseOverflowRulesAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertWarehouseOverflowRuleAsync(string characterUUID, WarehouseOverflowRule entity)
        {
            await UpsertEntityAsync(characterUUID, "warehouseoverflowrules", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteWarehouseOverflowRuleAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<WarehouseOverflowRule>(characterUUID, "warehouseoverflowrules", e => e.UUID == entityUUID);
        }

        // MailMessage

        /// <inheritdoc/>
        public async Task<IReadOnlyList<MailMessage>> GetAllMailMessagesAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<MailMessage>(characterUUID, "mailmessages");
        }

        /// <inheritdoc/>
        public async Task<MailMessage> GetMailMessageAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllMailMessagesAsync(characterUUID);
            return all.FirstOrDefault(e => e.MailId.ToString() == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertMailMessageAsync(string characterUUID, MailMessage entity)
        {
            await UpsertEntityAsync(characterUUID, "mailmessages", entity, e => e.MailId == entity.MailId);
        }

        /// <inheritdoc/>
        public async Task DeleteMailMessageAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<MailMessage>(characterUUID, "mailmessages", e => e.MailId.ToString() == entityUUID);
        }

        // BankingTransaction

        /// <inheritdoc/>
        public async Task<IReadOnlyList<BankingTransaction>> GetAllBankingTransactionsAsync(string characterUUID)
        {
            return await GetAllEntitiesAsync<BankingTransaction>(characterUUID, "bankingtransactions");
        }

        /// <inheritdoc/>
        public async Task<BankingTransaction> GetBankingTransactionAsync(string characterUUID, string entityUUID)
        {
            var all = await GetAllBankingTransactionsAsync(characterUUID);
            return all.FirstOrDefault(e => e.UUID == entityUUID);
        }

        /// <inheritdoc/>
        public async Task UpsertBankingTransactionAsync(string characterUUID, BankingTransaction entity)
        {
            await UpsertEntityAsync(characterUUID, "bankingtransactions", entity, e => e.UUID == entity.UUID);
        }

        /// <inheritdoc/>
        public async Task DeleteBankingTransactionAsync(string characterUUID, string entityUUID)
        {
            await DeleteEntityAsync<BankingTransaction>(characterUUID, "bankingtransactions", e => e.UUID == entityUUID);
        }

        // ═══════════════════════════════════════════════════════════
        // Baseline / Global Lookup Data
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<BaselineGameConstants> GetBaselineGameConstantsAsync()
        {
            var path = Path.Combine(_dataPath, "global", "game-constants.json");
            var json = await ReadRawAsync(path);
            if (json == null)
            {
                return null;
            }

            return JsonConvert.DeserializeObject<BaselineGameConstants>(json, SerializerSettings);
        }

        /// <inheritdoc/>
        public async Task UpsertBaselineGameConstantsAsync(BaselineGameConstants constants)
        {
            var path = Path.Combine(_dataPath, "global", "game-constants.json");
            var json = JsonConvert.SerializeObject(constants, SerializerSettings);
            await WriteAtomicAsync(path, json);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<BlueprintType>> GetAllBlueprintTypesAsync()
        {
            var path = Path.Combine(_dataPath, "global", "blueprint-types.json");
            await _lock.WaitAsync();
            try
            {
                return await ReadListUnlockedAsync<BlueprintType>(path);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task UpsertBlueprintTypesAsync(IReadOnlyList<BlueprintType> types)
        {
            var path = Path.Combine(_dataPath, "global", "blueprint-types.json");
            var json = JsonConvert.SerializeObject(types, SerializerSettings);
            await WriteAtomicAsync(path, json);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ShipClass>> GetAllShipClassesAsync()
        {
            var path = Path.Combine(_dataPath, "global", "ship-classes.json");
            await _lock.WaitAsync();
            try
            {
                return await ReadListUnlockedAsync<ShipClass>(path);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task UpsertShipClassesAsync(IReadOnlyList<ShipClass> classes)
        {
            var path = Path.Combine(_dataPath, "global", "ship-classes.json");
            var json = JsonConvert.SerializeObject(classes, SerializerSettings);
            await WriteAtomicAsync(path, json);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TechLevel>> GetAllTechLevelsAsync()
        {
            var path = Path.Combine(_dataPath, "global", "tech-levels.json");
            await _lock.WaitAsync();
            try
            {
                return await ReadListUnlockedAsync<TechLevel>(path);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task UpsertTechLevelsAsync(IReadOnlyList<TechLevel> levels)
        {
            var path = Path.Combine(_dataPath, "global", "tech-levels.json");
            var json = JsonConvert.SerializeObject(levels, SerializerSettings);
            await WriteAtomicAsync(path, json);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Commodity>> GetAllCommoditiesAsync()
        {
            var path = Path.Combine(_dataPath, "global", "commodities.json");
            await _lock.WaitAsync();
            try
            {
                return await ReadListUnlockedAsync<Commodity>(path);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task UpsertCommoditiesAsync(IReadOnlyList<Commodity> commodities)
        {
            var path = Path.Combine(_dataPath, "global", "commodities.json");
            var json = JsonConvert.SerializeObject(commodities, SerializerSettings);
            await WriteAtomicAsync(path, json);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<RefiningRecipe>> GetAllRefiningRecipesAsync()
        {
            var path = Path.Combine(_dataPath, "global", "refining-recipes.json");
            await _lock.WaitAsync();
            try
            {
                return await ReadListUnlockedAsync<RefiningRecipe>(path);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task UpsertRefiningRecipesAsync(IReadOnlyList<RefiningRecipe> recipes)
        {
            var path = Path.Combine(_dataPath, "global", "refining-recipes.json");
            var json = JsonConvert.SerializeObject(recipes, SerializerSettings);
            await WriteAtomicAsync(path, json);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ResearchTimeEntry>> GetAllResearchTimesAsync()
        {
            var path = Path.Combine(_dataPath, "global", "research-times.json");
            await _lock.WaitAsync();
            try
            {
                return await ReadListUnlockedAsync<ResearchTimeEntry>(path);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task UpsertResearchTimesAsync(IReadOnlyList<ResearchTimeEntry> entries)
        {
            var path = Path.Combine(_dataPath, "global", "research-times.json");
            var json = JsonConvert.SerializeObject(entries, SerializerSettings);
            await WriteAtomicAsync(path, json);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<PropertyTypeDefinition>> GetAllPropertyTypeDefinitionsAsync()
        {
            var path = Path.Combine(_dataPath, "global", "property-type-definitions.json");
            await _lock.WaitAsync();
            try
            {
                return await ReadListUnlockedAsync<PropertyTypeDefinition>(path);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task UpsertPropertyTypeDefinitionsAsync(IReadOnlyList<PropertyTypeDefinition> definitions)
        {
            var path = Path.Combine(_dataPath, "global", "property-type-definitions.json");
            var json = JsonConvert.SerializeObject(definitions, SerializerSettings);
            await WriteAtomicAsync(path, json);
        }

        // ═══════════════════════════════════════════════════════════
        // Private Helpers
        // ═══════════════════════════════════════════════════════════

        private static void EnsureFile(string path, string defaultContent)
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, defaultContent);
            }
        }

        private static Task<string> ReadRawUnlockedAsync(string path)
        {
            if (!File.Exists(path))
            {
                return Task.FromResult<string>(null);
            }

            var content = File.ReadAllText(path);
            return Task.FromResult(content);
        }

        private static Task WriteAtomicUnlockedAsync(string path, string content)
        {
            var dir = Path.GetDirectoryName(path);
            if (dir != null)
            {
                Directory.CreateDirectory(dir);
            }

            var tempPath = path + ".tmp";
            File.WriteAllText(tempPath, content);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tempPath, path);
            return Task.CompletedTask;
        }

        private static Task<List<T>> ReadListUnlockedAsync<T>(string path)
        {
            if (!File.Exists(path))
            {
                return Task.FromResult(new List<T>());
            }

            var json = File.ReadAllText(path);
            try
            {
                var list = JsonConvert.DeserializeObject<List<T>>(json) ?? new List<T>();
                return Task.FromResult(list);
            }
            catch (JsonException ex)
            {
                throw new StorageLoadException(
                    "JsonMultiFile",
                    path,
                    $"Failed to deserialize JSON from {path}",
                    ex);
            }
        }

        private string DataFilePath(string filename) => Path.Combine(_dataPath, filename);

        private string CharacterDataPath(string characterUUID, string dataType) =>
            Path.Combine(_dataPath, "characters", characterUUID, $"{dataType}.json");

        private string CharacterPermPath(string characterUUID, string filename) =>
            Path.Combine(_dataPath, "characters", characterUUID, filename);

        private string FactionDataPath(string factionUUID, string filename) =>
            Path.Combine(_dataPath, "factions", factionUUID, filename);

        private void EnsureCharacterDirectory(string characterUUID)
        {
            Directory.CreateDirectory(Path.Combine(_dataPath, "characters", characterUUID));
        }

        private void EnsureFactionDirectory(string factionUUID)
        {
            Directory.CreateDirectory(Path.Combine(_dataPath, "factions", factionUUID));
        }

        private async Task<string> ReadRawAsync(string path)
        {
            await _lock.WaitAsync();
            try
            {
                return await ReadRawUnlockedAsync(path);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task WriteAtomicAsync(string path, string content)
        {
            await _lock.WaitAsync();
            try
            {
                await WriteAtomicUnlockedAsync(path, content);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<List<T>> ReadListAsync<T>(string filename)
        {
            await _lock.WaitAsync();
            try
            {
                var path = DataFilePath(filename);
                return await ReadListUnlockedAsync<T>(path);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task UpsertInListAsync<T>(string filename, T item, Func<T, bool> predicate)
        {
            await _lock.WaitAsync();
            try
            {
                var path = DataFilePath(filename);
                var list = await ReadListUnlockedAsync<T>(path);
                var index = list.FindIndex(x => predicate(x));
                if (index >= 0)
                {
                    list[index] = item;
                }
                else
                {
                    list.Add(item);
                }

                var json = JsonConvert.SerializeObject(list, SerializerSettings);
                await WriteAtomicUnlockedAsync(path, json);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task DeleteFromListAsync<T>(string filename, Func<T, bool> predicate)
        {
            await _lock.WaitAsync();
            try
            {
                var path = DataFilePath(filename);
                var list = await ReadListUnlockedAsync<T>(path);
                var removed = list.RemoveAll(x => predicate(x));
                if (removed > 0)
                {
                    var json = JsonConvert.SerializeObject(list, SerializerSettings);
                    await WriteAtomicUnlockedAsync(path, json);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<IReadOnlyList<T>> ReadFactionListAsync<T>(string factionUUID, string filename)
        {
            await _lock.WaitAsync();
            try
            {
                var path = FactionDataPath(factionUUID, filename);
                return await ReadListUnlockedAsync<T>(path);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<IReadOnlyList<T>> ReadCharacterPermListAsync<T>(string characterUUID, string filename)
        {
            await _lock.WaitAsync();
            try
            {
                var path = CharacterPermPath(characterUUID, filename);
                return await ReadListUnlockedAsync<T>(path);
            }
            finally
            {
                _lock.Release();
            }
        }

        private List<string> FindFactionFilesForGroup(string filename)
        {
            var factionsDir = Path.Combine(_dataPath, "factions");
            if (!Directory.Exists(factionsDir))
            {
                return new List<string>();
            }

            return Directory.GetDirectories(factionsDir)
                .Select(d => Path.Combine(d, filename))
                .Where(File.Exists)
                .ToList();
        }

        private List<string> FindCharacterFilesForGroup(string filename)
        {
            var charsDir = Path.Combine(_dataPath, "characters");
            if (!Directory.Exists(charsDir))
            {
                return new List<string>();
            }

            return Directory.GetDirectories(charsDir)
                .Select(d => Path.Combine(d, filename))
                .Where(File.Exists)
                .ToList();
        }

        private async Task<string> FindFactionForGroupUnlockedAsync(string groupUUID)
        {
            var factionsDir = Path.Combine(_dataPath, "factions");
            if (!Directory.Exists(factionsDir))
            {
                return null;
            }

            foreach (var dir in Directory.GetDirectories(factionsDir))
            {
                var groupsPath = Path.Combine(dir, "groups.json");
                var groups = await ReadListUnlockedAsync<FactionPermissionGroup>(groupsPath);
                if (groups.Any(g => g.UUID == groupUUID))
                {
                    return Path.GetFileName(dir);
                }
            }

            return null;
        }

        private async Task<string> FindCharacterForGroupUnlockedAsync(string groupUUID)
        {
            var charsDir = Path.Combine(_dataPath, "characters");
            if (!Directory.Exists(charsDir))
            {
                return null;
            }

            foreach (var dir in Directory.GetDirectories(charsDir))
            {
                var groupsPath = Path.Combine(dir, "perm-groups.json");
                var groups = await ReadListUnlockedAsync<CharacterPermissionGroup>(groupsPath);
                if (groups.Any(g => g.UUID == groupUUID))
                {
                    return Path.GetFileName(dir);
                }
            }

            return null;
        }

        // --- Generic Entity CRUD helpers ---

        private async Task<IReadOnlyList<T>> GetAllEntitiesAsync<T>(string characterUUID, string dataType)
        {
            var path = CharacterDataPath(characterUUID, dataType);
            var json = await ReadRawAsync(path);
            if (json == null)
            {
                return Array.Empty<T>();
            }

            try
            {
                return JsonConvert.DeserializeObject<List<T>>(json, SerializerSettings) ?? new List<T>();
            }
            catch (JsonException ex)
            {
                throw new StorageLoadException(
                    "JsonMultiFile",
                    path,
                    $"Failed to deserialize JSON from {path}",
                    ex);
            }
        }

        private async Task UpsertEntityAsync<T>(string characterUUID, string dataType, T entity, Func<T, bool> predicate)
        {
            await _lock.WaitAsync();
            try
            {
                var path = CharacterDataPath(characterUUID, dataType);
                EnsureCharacterDirectory(characterUUID);
                var json = await ReadRawUnlockedAsync(path);
                var list = string.IsNullOrEmpty(json)
                    ? new List<T>()
                    : JsonConvert.DeserializeObject<List<T>>(json, SerializerSettings) ?? new List<T>();
                var index = list.FindIndex(x => predicate(x));
                if (index >= 0)
                {
                    list[index] = entity;
                }
                else
                {
                    list.Add(entity);
                }

                await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task DeleteEntityAsync<T>(string characterUUID, string dataType, Func<T, bool> predicate)
        {
            await _lock.WaitAsync();
            try
            {
                var path = CharacterDataPath(characterUUID, dataType);
                var json = await ReadRawUnlockedAsync(path);
                if (json == null)
                {
                    return;
                }

                var list = JsonConvert.DeserializeObject<List<T>>(json, SerializerSettings) ?? new List<T>();
                list.RemoveAll(x => predicate(x));
                await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
