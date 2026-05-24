using Newtonsoft.Json;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Server.Storage;

/// <summary>
/// JSON file-based implementation of <see cref="IStorageBackend"/>.
/// Stores data as JSON files in a configurable directory structure.
/// Thread-safe via SemaphoreSlim; atomic writes via temp-file + rename.
/// </summary>
public class JsonFileStorageBackend : IStorageBackend
{
    private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
    };

    private readonly string _dataPath;
    private readonly ILogger<JsonFileStorageBackend> _logger;
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    public JsonFileStorageBackend(IConfiguration configuration, ILogger<JsonFileStorageBackend> logger)
    {
        _dataPath = configuration["Storage:DataPath"] ?? "./data";
        _logger = logger;
    }

    // --- Lifecycle ---

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

            _logger.LogInformation("JsonFileStorageBackend initialized at {Path}", _dataPath);
        }
        finally
        {
            _lock.Release();
        }
    }

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
            _logger.LogWarning(ex, "Storage validation failed");
            return Task.FromResult(false);
        }
    }

    // --- Factions ---

    public async Task<ServerFaction?> GetFactionAsync(string uuid)
    {
        var factions = await ReadListAsync<ServerFaction>("factions.json");
        return factions.FirstOrDefault(f => f.UUID == uuid);
    }

    public async Task<IReadOnlyList<ServerFaction>> GetAllFactionsAsync()
    {
        return await ReadListAsync<ServerFaction>("factions.json");
    }

    public async Task UpsertFactionAsync(ServerFaction faction)
    {
        await UpsertInListAsync<ServerFaction>("factions.json", faction, f => f.UUID == faction.UUID);
    }

    public async Task DeleteFactionAsync(string uuid)
    {
        await DeleteFromListAsync<ServerFaction>("factions.json", f => f.UUID == uuid);
    }

    // --- Characters ---

    public async Task<ServerCharacter?> GetCharacterAsync(string uuid)
    {
        var characters = await ReadListAsync<ServerCharacter>("characters.json");
        return characters.FirstOrDefault(c => c.UUID == uuid);
    }

    public async Task<IReadOnlyList<ServerCharacter>> GetAllCharactersAsync()
    {
        return await ReadListAsync<ServerCharacter>("characters.json");
    }

    public async Task UpsertCharacterAsync(ServerCharacter character)
    {
        await UpsertInListAsync<ServerCharacter>("characters.json", character, c => c.UUID == character.UUID);
    }

    public async Task DeleteCharacterAsync(string uuid)
    {
        await DeleteFromListAsync<ServerCharacter>("characters.json", c => c.UUID == uuid);
    }

    // --- Global/Baseline Data ---

    public async Task<string?> GetGlobalDataAsync(string dataType)
    {
        var path = Path.Combine(_dataPath, "global", $"{dataType}.json");
        return await ReadRawAsync(path);
    }

    public async Task UpsertGlobalDataAsync(string dataType, string json)
    {
        var path = Path.Combine(_dataPath, "global", $"{dataType}.json");
        await WriteAtomicAsync(path, json);
    }

    // --- Star Systems ---

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

    public async Task UpsertStarSystemsAsync(IReadOnlyList<StarSystem> systems)
    {
        var path = Path.Combine(_dataPath, "global", "star-systems.json");
        var json = JsonConvert.SerializeObject(systems, SerializerSettings);
        await WriteAtomicAsync(path, json);
    }

    // --- Colony Summaries ---

    public Task<IReadOnlyList<ColonySummary>> GetColonySummariesForSystemAsync(int systemId)
    {
        throw new NotImplementedException();
    }

    // --- Tokens ---

    public async Task<ApiToken?> FindTokenByHashAsync(string tokenHash)
    {
        var tokens = await ReadListAsync<ApiToken>("tokens.json");
        return tokens.FirstOrDefault(t => t.TokenHash == tokenHash);
    }

    public async Task<IReadOnlyList<ApiToken>> GetAllTokensAsync()
    {
        return await ReadListAsync<ApiToken>("tokens.json");
    }

    public async Task UpsertTokenAsync(ApiToken token)
    {
        await UpsertInListAsync<ApiToken>("tokens.json", token, t => t.Id == token.Id);
    }

    public async Task DeleteTokenAsync(string id)
    {
        await DeleteFromListAsync<ApiToken>("tokens.json", t => t.Id == id);
    }

    // --- Membership Actions ---

    public async Task<IReadOnlyList<MembershipAction>> GetFactionActionsAsync(string factionUUID)
    {
        var actions = await ReadListAsync<MembershipAction>("membership-actions.json");
        return actions.Where(a => a.FactionUUID == factionUUID).ToList();
    }

    public async Task UpsertMembershipActionAsync(MembershipAction action)
    {
        await UpsertInListAsync<MembershipAction>("membership-actions.json", action, a => a.Id == action.Id);
    }

    public async Task DeleteMembershipActionAsync(string id)
    {
        await DeleteFromListAsync<MembershipAction>("membership-actions.json", a => a.Id == id);
    }

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

    // --- Sharing Rules ---

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

    public async Task UpsertSharingRulesAsync(string characterUUID, IReadOnlyList<SharingRule> rules)
    {
        EnsureCharacterDirectory(characterUUID);
        var path = Path.Combine(_dataPath, "characters", characterUUID, "sharing.json");
        var json = JsonConvert.SerializeObject(rules, SerializerSettings);
        await WriteAtomicAsync(path, json);
    }

    // --- Character Preferences ---

    public async Task<CharacterPreferences?> GetCharacterPreferencesAsync(string characterUUID)
    {
        var path = Path.Combine(_dataPath, "characters", characterUUID, "preferences.json");
        var json = await ReadRawAsync(path);
        if (json == null)
        {
            return null;
        }

        return JsonConvert.DeserializeObject<CharacterPreferences>(json);
    }

    public async Task UpsertCharacterPreferencesAsync(CharacterPreferences prefs)
    {
        EnsureCharacterDirectory(prefs.CharacterUUID);
        var path = Path.Combine(_dataPath, "characters", prefs.CharacterUUID, "preferences.json");
        var json = JsonConvert.SerializeObject(prefs, SerializerSettings);
        await WriteAtomicAsync(path, json);
    }

    // --- Faction Permission Entities ---

    public async Task<IReadOnlyList<FactionCapability>> GetFactionCapabilitiesAsync(string factionUUID)
    {
        return await ReadFactionListAsync<FactionCapability>(factionUUID, "capabilities.json");
    }

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

    public async Task<IReadOnlyList<FactionClearanceLevel>> GetFactionClearanceLevelsAsync(string factionUUID)
    {
        return await ReadFactionListAsync<FactionClearanceLevel>(factionUUID, "clearance-levels.json");
    }

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

    public async Task<IReadOnlyList<FactionPermissionGroup>> GetFactionGroupsAsync(string factionUUID)
    {
        return await ReadFactionListAsync<FactionPermissionGroup>(factionUUID, "groups.json");
    }

    public async Task<FactionPermissionGroup?> GetFactionGroupAsync(string factionUUID, string groupUUID)
    {
        var groups = await ReadFactionListAsync<FactionPermissionGroup>(factionUUID, "groups.json");
        return groups.FirstOrDefault(g => g.UUID == groupUUID);
    }

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

    public async Task<FactionMemberPermissions?> GetFactionMemberPermissionsAsync(string factionUUID, string characterUUID)
    {
        var list = await ReadFactionListAsync<FactionMemberPermissions>(factionUUID, "member-permissions.json");
        return list.FirstOrDefault(p => p.CharacterUUID == characterUUID);
    }

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

    public async Task<IReadOnlyList<FactionMemberPermissions>> GetAllFactionMembersPermissionsAsync(string factionUUID)
    {
        return await ReadFactionListAsync<FactionMemberPermissions>(factionUUID, "member-permissions.json");
    }

    public async Task<IReadOnlyList<FactionMemberCapability>> GetFactionMemberCapabilitiesAsync(string factionUUID, string characterUUID)
    {
        var list = await ReadFactionListAsync<FactionMemberCapability>(factionUUID, "member-capabilities.json");
        return list.Where(c => c.CharacterUUID == characterUUID).ToList();
    }

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

    // --- Character Permission Entities ---

    public async Task<IReadOnlyList<CharacterCapability>> GetCharacterCapabilitiesAsync(string characterUUID)
    {
        return await ReadCharacterPermListAsync<CharacterCapability>(characterUUID, "perm-capabilities.json");
    }

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

    public async Task<IReadOnlyList<CharacterClearanceLevel>> GetCharacterClearanceLevelsAsync(string characterUUID)
    {
        return await ReadCharacterPermListAsync<CharacterClearanceLevel>(characterUUID, "perm-clearance-levels.json");
    }

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

    public async Task<IReadOnlyList<CharacterPermissionGroup>> GetCharacterGroupsAsync(string characterUUID)
    {
        return await ReadCharacterPermListAsync<CharacterPermissionGroup>(characterUUID, "perm-groups.json");
    }

    public async Task<CharacterPermissionGroup?> GetCharacterGroupAsync(string characterUUID, string groupUUID)
    {
        var groups = await ReadCharacterPermListAsync<CharacterPermissionGroup>(characterUUID, "perm-groups.json");
        return groups.FirstOrDefault(g => g.UUID == groupUUID);
    }

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

    public async Task<IReadOnlyList<CharacterGranteePermissions>> GetCharacterGranteesAsync(string ownerCharacterUUID)
    {
        return await ReadCharacterPermListAsync<CharacterGranteePermissions>(ownerCharacterUUID, "perm-grantees.json");
    }

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

    public async Task<IReadOnlyList<CharacterGranteeCapability>> GetCharacterGranteeCapabilitiesAsync(string ownerCharacterUUID, string granteeUUID)
    {
        var list = await ReadCharacterPermListAsync<CharacterGranteeCapability>(ownerCharacterUUID, "perm-grantee-capabilities.json");
        return list.Where(c => c.GranteeUUID == granteeUUID).ToList();
    }

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

    // --- Intel and Audit ---

    public async Task<IReadOnlyList<IntelComment>> GetIntelCommentsForTargetAsync(string targetCharacterUUID)
    {
        var list = await ReadListAsync<IntelComment>("intel-comments.json");
        return list.Where(c => c.TargetCharacterUUID == targetCharacterUUID).ToList();
    }

    public async Task<IntelComment?> GetIntelCommentAsync(string commentUUID)
    {
        var list = await ReadListAsync<IntelComment>("intel-comments.json");
        return list.FirstOrDefault(c => c.UUID == commentUUID);
    }

    public async Task UpsertIntelCommentAsync(IntelComment comment)
    {
        await UpsertInListAsync<IntelComment>("intel-comments.json", comment, c => c.UUID == comment.UUID);
    }

    public async Task DeleteIntelCommentAsync(string commentUUID)
    {
        await DeleteFromListAsync<IntelComment>("intel-comments.json", c => c.UUID == commentUUID);
    }

    public async Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForCommentAsync(string commentUUID)
    {
        var list = await ReadListAsync<IntelCommentFactionShare>("intel-shares.json");
        return list.Where(s => s.IntelCommentUUID == commentUUID).ToList();
    }

    public async Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForFactionAsync(string factionUUID)
    {
        var list = await ReadListAsync<IntelCommentFactionShare>("intel-shares.json");
        return list.Where(s => s.FactionUUID == factionUUID).ToList();
    }

    public async Task UpsertIntelShareAsync(IntelCommentFactionShare share)
    {
        await UpsertInListAsync<IntelCommentFactionShare>("intel-shares.json", share, s => s.UUID == share.UUID);
    }

    public async Task DeleteIntelShareAsync(string shareUUID)
    {
        await DeleteFromListAsync<IntelCommentFactionShare>("intel-shares.json", s => s.UUID == shareUUID);
    }

    public async Task<IReadOnlyList<PermissionAuditEntry>> GetPermissionAuditEntriesAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        PermissionActionType? actionType = null,
        string? actorUUID = null,
        string? targetUUID = null)
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

    // --- Typed Entity CRUD (per-character domain entities) ---

    // Colony

    public async Task<IReadOnlyList<Colony>> GetAllColoniesAsync(string characterUUID)
    {
        var path = CharacterDataPath(characterUUID, "colonies");
        var json = await ReadRawAsync(path);
        if (json == null) return Array.Empty<Colony>();
        return JsonConvert.DeserializeObject<List<Colony>>(json, SerializerSettings) ?? new List<Colony>();
    }

    public async Task<Colony?> GetColonyAsync(string characterUUID, string entityUUID)
    {
        var all = await GetAllColoniesAsync(characterUUID);
        return all.FirstOrDefault(e => e.UUID == entityUUID);
    }

    public async Task UpsertColonyAsync(string characterUUID, Colony entity)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "colonies");
            EnsureCharacterDirectory(characterUUID);
            var json = await ReadRawUnlockedAsync(path);
            var list = string.IsNullOrEmpty(json)
                ? new List<Colony>()
                : JsonConvert.DeserializeObject<List<Colony>>(json, SerializerSettings) ?? new List<Colony>();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0) list[index] = entity;
            else list.Add(entity);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteColonyAsync(string characterUUID, string entityUUID)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "colonies");
            var json = await ReadRawUnlockedAsync(path);
            if (json == null) return;
            var list = JsonConvert.DeserializeObject<List<Colony>>(json, SerializerSettings) ?? new List<Colony>();
            list.RemoveAll(e => e.UUID == entityUUID);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    // Blueprint

    public async Task<IReadOnlyList<Blueprint>> GetAllBlueprintsAsync(string characterUUID)
    {
        var path = CharacterDataPath(characterUUID, "blueprints");
        var json = await ReadRawAsync(path);
        if (json == null) return Array.Empty<Blueprint>();
        return JsonConvert.DeserializeObject<List<Blueprint>>(json, SerializerSettings) ?? new List<Blueprint>();
    }

    public async Task<Blueprint?> GetBlueprintAsync(string characterUUID, string entityUUID)
    {
        var all = await GetAllBlueprintsAsync(characterUUID);
        return all.FirstOrDefault(e => e.UUID == entityUUID);
    }

    public async Task UpsertBlueprintAsync(string characterUUID, Blueprint entity)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "blueprints");
            EnsureCharacterDirectory(characterUUID);
            var json = await ReadRawUnlockedAsync(path);
            var list = string.IsNullOrEmpty(json)
                ? new List<Blueprint>()
                : JsonConvert.DeserializeObject<List<Blueprint>>(json, SerializerSettings) ?? new List<Blueprint>();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0) list[index] = entity;
            else list.Add(entity);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteBlueprintAsync(string characterUUID, string entityUUID)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "blueprints");
            var json = await ReadRawUnlockedAsync(path);
            if (json == null) return;
            var list = JsonConvert.DeserializeObject<List<Blueprint>>(json, SerializerSettings) ?? new List<Blueprint>();
            list.RemoveAll(e => e.UUID == entityUUID);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    // Survey

    public async Task<IReadOnlyList<Survey>> GetAllSurveysAsync(string characterUUID)
    {
        var path = CharacterDataPath(characterUUID, "surveys");
        var json = await ReadRawAsync(path);
        if (json == null) return Array.Empty<Survey>();
        return JsonConvert.DeserializeObject<List<Survey>>(json, SerializerSettings) ?? new List<Survey>();
    }

    public async Task<Survey?> GetSurveyAsync(string characterUUID, string entityUUID)
    {
        var all = await GetAllSurveysAsync(characterUUID);
        return all.FirstOrDefault(e => e.UUID == entityUUID);
    }

    public async Task UpsertSurveyAsync(string characterUUID, Survey entity)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "surveys");
            EnsureCharacterDirectory(characterUUID);
            var json = await ReadRawUnlockedAsync(path);
            var list = string.IsNullOrEmpty(json)
                ? new List<Survey>()
                : JsonConvert.DeserializeObject<List<Survey>>(json, SerializerSettings) ?? new List<Survey>();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0) list[index] = entity;
            else list.Add(entity);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteSurveyAsync(string characterUUID, string entityUUID)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "surveys");
            var json = await ReadRawUnlockedAsync(path);
            if (json == null) return;
            var list = JsonConvert.DeserializeObject<List<Survey>>(json, SerializerSettings) ?? new List<Survey>();
            list.RemoveAll(e => e.UUID == entityUUID);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    // PlayerProfile

    public async Task<IReadOnlyList<PlayerProfile>> GetAllPlayerProfilesAsync(string characterUUID)
    {
        var path = CharacterDataPath(characterUUID, "playerprofile");
        var json = await ReadRawAsync(path);
        if (json == null) return Array.Empty<PlayerProfile>();
        return JsonConvert.DeserializeObject<List<PlayerProfile>>(json, SerializerSettings) ?? new List<PlayerProfile>();
    }

    public async Task<PlayerProfile?> GetPlayerProfileAsync(string characterUUID, string entityUUID)
    {
        var all = await GetAllPlayerProfilesAsync(characterUUID);
        return all.FirstOrDefault(e => e.UUID == entityUUID);
    }

    public async Task UpsertPlayerProfileAsync(string characterUUID, PlayerProfile entity)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "playerprofile");
            EnsureCharacterDirectory(characterUUID);
            var json = await ReadRawUnlockedAsync(path);
            var list = string.IsNullOrEmpty(json)
                ? new List<PlayerProfile>()
                : JsonConvert.DeserializeObject<List<PlayerProfile>>(json, SerializerSettings) ?? new List<PlayerProfile>();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0) list[index] = entity;
            else list.Add(entity);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeletePlayerProfileAsync(string characterUUID, string entityUUID)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "playerprofile");
            var json = await ReadRawUnlockedAsync(path);
            if (json == null) return;
            var list = JsonConvert.DeserializeObject<List<PlayerProfile>>(json, SerializerSettings) ?? new List<PlayerProfile>();
            list.RemoveAll(e => e.UUID == entityUUID);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    // DeliveryRoute

    public async Task<IReadOnlyList<DeliveryRoute>> GetAllDeliveryRoutesAsync(string characterUUID)
    {
        var path = CharacterDataPath(characterUUID, "deliveryroutes");
        var json = await ReadRawAsync(path);
        if (json == null) return Array.Empty<DeliveryRoute>();
        return JsonConvert.DeserializeObject<List<DeliveryRoute>>(json, SerializerSettings) ?? new List<DeliveryRoute>();
    }

    public async Task<DeliveryRoute?> GetDeliveryRouteAsync(string characterUUID, string entityUUID)
    {
        var all = await GetAllDeliveryRoutesAsync(characterUUID);
        return all.FirstOrDefault(e => e.UUID == entityUUID);
    }

    public async Task UpsertDeliveryRouteAsync(string characterUUID, DeliveryRoute entity)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "deliveryroutes");
            EnsureCharacterDirectory(characterUUID);
            var json = await ReadRawUnlockedAsync(path);
            var list = string.IsNullOrEmpty(json)
                ? new List<DeliveryRoute>()
                : JsonConvert.DeserializeObject<List<DeliveryRoute>>(json, SerializerSettings) ?? new List<DeliveryRoute>();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0) list[index] = entity;
            else list.Add(entity);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteDeliveryRouteAsync(string characterUUID, string entityUUID)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "deliveryroutes");
            var json = await ReadRawUnlockedAsync(path);
            if (json == null) return;
            var list = JsonConvert.DeserializeObject<List<DeliveryRoute>>(json, SerializerSettings) ?? new List<DeliveryRoute>();
            list.RemoveAll(e => e.UUID == entityUUID);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    // DeliveryPlan

    public async Task<IReadOnlyList<DeliveryPlan>> GetAllDeliveryPlansAsync(string characterUUID)
    {
        var path = CharacterDataPath(characterUUID, "deliveryplans");
        var json = await ReadRawAsync(path);
        if (json == null) return Array.Empty<DeliveryPlan>();
        return JsonConvert.DeserializeObject<List<DeliveryPlan>>(json, SerializerSettings) ?? new List<DeliveryPlan>();
    }

    public async Task<DeliveryPlan?> GetDeliveryPlanAsync(string characterUUID, string entityUUID)
    {
        var all = await GetAllDeliveryPlansAsync(characterUUID);
        return all.FirstOrDefault(e => e.UUID == entityUUID);
    }

    public async Task UpsertDeliveryPlanAsync(string characterUUID, DeliveryPlan entity)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "deliveryplans");
            EnsureCharacterDirectory(characterUUID);
            var json = await ReadRawUnlockedAsync(path);
            var list = string.IsNullOrEmpty(json)
                ? new List<DeliveryPlan>()
                : JsonConvert.DeserializeObject<List<DeliveryPlan>>(json, SerializerSettings) ?? new List<DeliveryPlan>();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0) list[index] = entity;
            else list.Add(entity);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteDeliveryPlanAsync(string characterUUID, string entityUUID)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "deliveryplans");
            var json = await ReadRawUnlockedAsync(path);
            if (json == null) return;
            var list = JsonConvert.DeserializeObject<List<DeliveryPlan>>(json, SerializerSettings) ?? new List<DeliveryPlan>();
            list.RemoveAll(e => e.UUID == entityUUID);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    // Ship

    public async Task<IReadOnlyList<Ship>> GetAllShipsAsync(string characterUUID)
    {
        var path = CharacterDataPath(characterUUID, "ships");
        var json = await ReadRawAsync(path);
        if (json == null) return Array.Empty<Ship>();
        return JsonConvert.DeserializeObject<List<Ship>>(json, SerializerSettings) ?? new List<Ship>();
    }

    public async Task<Ship?> GetShipAsync(string characterUUID, string entityUUID)
    {
        var all = await GetAllShipsAsync(characterUUID);
        return all.FirstOrDefault(e => e.UUID == entityUUID);
    }

    public async Task UpsertShipAsync(string characterUUID, Ship entity)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "ships");
            EnsureCharacterDirectory(characterUUID);
            var json = await ReadRawUnlockedAsync(path);
            var list = string.IsNullOrEmpty(json)
                ? new List<Ship>()
                : JsonConvert.DeserializeObject<List<Ship>>(json, SerializerSettings) ?? new List<Ship>();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0) list[index] = entity;
            else list.Add(entity);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteShipAsync(string characterUUID, string entityUUID)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "ships");
            var json = await ReadRawUnlockedAsync(path);
            if (json == null) return;
            var list = JsonConvert.DeserializeObject<List<Ship>>(json, SerializerSettings) ?? new List<Ship>();
            list.RemoveAll(e => e.UUID == entityUUID);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    // ShipTemplate

    public async Task<IReadOnlyList<ShipTemplate>> GetAllShipTemplatesAsync(string characterUUID)
    {
        var path = CharacterDataPath(characterUUID, "shiptemplates");
        var json = await ReadRawAsync(path);
        if (json == null) return Array.Empty<ShipTemplate>();
        return JsonConvert.DeserializeObject<List<ShipTemplate>>(json, SerializerSettings) ?? new List<ShipTemplate>();
    }

    public async Task<ShipTemplate?> GetShipTemplateAsync(string characterUUID, string entityUUID)
    {
        var all = await GetAllShipTemplatesAsync(characterUUID);
        return all.FirstOrDefault(e => e.UUID == entityUUID);
    }

    public async Task UpsertShipTemplateAsync(string characterUUID, ShipTemplate entity)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "shiptemplates");
            EnsureCharacterDirectory(characterUUID);
            var json = await ReadRawUnlockedAsync(path);
            var list = string.IsNullOrEmpty(json)
                ? new List<ShipTemplate>()
                : JsonConvert.DeserializeObject<List<ShipTemplate>>(json, SerializerSettings) ?? new List<ShipTemplate>();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0) list[index] = entity;
            else list.Add(entity);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteShipTemplateAsync(string characterUUID, string entityUUID)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "shiptemplates");
            var json = await ReadRawUnlockedAsync(path);
            if (json == null) return;
            var list = JsonConvert.DeserializeObject<List<ShipTemplate>>(json, SerializerSettings) ?? new List<ShipTemplate>();
            list.RemoveAll(e => e.UUID == entityUUID);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    // MarketListing

    public async Task<IReadOnlyList<MarketListing>> GetAllMarketListingsAsync(string characterUUID)
    {
        var path = CharacterDataPath(characterUUID, "marketlistings");
        var json = await ReadRawAsync(path);
        if (json == null) return Array.Empty<MarketListing>();
        return JsonConvert.DeserializeObject<List<MarketListing>>(json, SerializerSettings) ?? new List<MarketListing>();
    }

    public async Task<MarketListing?> GetMarketListingAsync(string characterUUID, string entityUUID)
    {
        var all = await GetAllMarketListingsAsync(characterUUID);
        return all.FirstOrDefault(e => e.UUID == entityUUID);
    }

    public async Task UpsertMarketListingAsync(string characterUUID, MarketListing entity)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "marketlistings");
            EnsureCharacterDirectory(characterUUID);
            var json = await ReadRawUnlockedAsync(path);
            var list = string.IsNullOrEmpty(json)
                ? new List<MarketListing>()
                : JsonConvert.DeserializeObject<List<MarketListing>>(json, SerializerSettings) ?? new List<MarketListing>();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0) list[index] = entity;
            else list.Add(entity);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteMarketListingAsync(string characterUUID, string entityUUID)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "marketlistings");
            var json = await ReadRawUnlockedAsync(path);
            if (json == null) return;
            var list = JsonConvert.DeserializeObject<List<MarketListing>>(json, SerializerSettings) ?? new List<MarketListing>();
            list.RemoveAll(e => e.UUID == entityUUID);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    // MarketTransaction

    public async Task<IReadOnlyList<MarketTransaction>> GetAllMarketTransactionsAsync(string characterUUID)
    {
        var path = CharacterDataPath(characterUUID, "markettransactions");
        var json = await ReadRawAsync(path);
        if (json == null) return Array.Empty<MarketTransaction>();
        return JsonConvert.DeserializeObject<List<MarketTransaction>>(json, SerializerSettings) ?? new List<MarketTransaction>();
    }

    public async Task<MarketTransaction?> GetMarketTransactionAsync(string characterUUID, string entityUUID)
    {
        var all = await GetAllMarketTransactionsAsync(characterUUID);
        return all.FirstOrDefault(e => e.UUID == entityUUID);
    }

    public async Task UpsertMarketTransactionAsync(string characterUUID, MarketTransaction entity)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "markettransactions");
            EnsureCharacterDirectory(characterUUID);
            var json = await ReadRawUnlockedAsync(path);
            var list = string.IsNullOrEmpty(json)
                ? new List<MarketTransaction>()
                : JsonConvert.DeserializeObject<List<MarketTransaction>>(json, SerializerSettings) ?? new List<MarketTransaction>();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0) list[index] = entity;
            else list.Add(entity);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteMarketTransactionAsync(string characterUUID, string entityUUID)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "markettransactions");
            var json = await ReadRawUnlockedAsync(path);
            if (json == null) return;
            var list = JsonConvert.DeserializeObject<List<MarketTransaction>>(json, SerializerSettings) ?? new List<MarketTransaction>();
            list.RemoveAll(e => e.UUID == entityUUID);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    // PricingPlan

    public async Task<IReadOnlyList<PricingPlan>> GetAllPricingPlansAsync(string characterUUID)
    {
        var path = CharacterDataPath(characterUUID, "pricingplans");
        var json = await ReadRawAsync(path);
        if (json == null) return Array.Empty<PricingPlan>();
        return JsonConvert.DeserializeObject<List<PricingPlan>>(json, SerializerSettings) ?? new List<PricingPlan>();
    }

    public async Task<PricingPlan?> GetPricingPlanAsync(string characterUUID, string entityUUID)
    {
        var all = await GetAllPricingPlansAsync(characterUUID);
        return all.FirstOrDefault(e => e.UUID == entityUUID);
    }

    public async Task UpsertPricingPlanAsync(string characterUUID, PricingPlan entity)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "pricingplans");
            EnsureCharacterDirectory(characterUUID);
            var json = await ReadRawUnlockedAsync(path);
            var list = string.IsNullOrEmpty(json)
                ? new List<PricingPlan>()
                : JsonConvert.DeserializeObject<List<PricingPlan>>(json, SerializerSettings) ?? new List<PricingPlan>();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0) list[index] = entity;
            else list.Add(entity);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeletePricingPlanAsync(string characterUUID, string entityUUID)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "pricingplans");
            var json = await ReadRawUnlockedAsync(path);
            if (json == null) return;
            var list = JsonConvert.DeserializeObject<List<PricingPlan>>(json, SerializerSettings) ?? new List<PricingPlan>();
            list.RemoveAll(e => e.UUID == entityUUID);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    // StockPlan

    public async Task<IReadOnlyList<StockPlan>> GetAllStockPlansAsync(string characterUUID)
    {
        var path = CharacterDataPath(characterUUID, "stockplans");
        var json = await ReadRawAsync(path);
        if (json == null) return Array.Empty<StockPlan>();
        return JsonConvert.DeserializeObject<List<StockPlan>>(json, SerializerSettings) ?? new List<StockPlan>();
    }

    public async Task<StockPlan?> GetStockPlanAsync(string characterUUID, string entityUUID)
    {
        var all = await GetAllStockPlansAsync(characterUUID);
        return all.FirstOrDefault(e => e.UUID == entityUUID);
    }

    public async Task UpsertStockPlanAsync(string characterUUID, StockPlan entity)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "stockplans");
            EnsureCharacterDirectory(characterUUID);
            var json = await ReadRawUnlockedAsync(path);
            var list = string.IsNullOrEmpty(json)
                ? new List<StockPlan>()
                : JsonConvert.DeserializeObject<List<StockPlan>>(json, SerializerSettings) ?? new List<StockPlan>();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0) list[index] = entity;
            else list.Add(entity);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteStockPlanAsync(string characterUUID, string entityUUID)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "stockplans");
            var json = await ReadRawUnlockedAsync(path);
            if (json == null) return;
            var list = JsonConvert.DeserializeObject<List<StockPlan>>(json, SerializerSettings) ?? new List<StockPlan>();
            list.RemoveAll(e => e.UUID == entityUUID);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    // StockProfile

    public async Task<IReadOnlyList<StockProfile>> GetAllStockProfilesAsync(string characterUUID)
    {
        var path = CharacterDataPath(characterUUID, "stockprofiles");
        var json = await ReadRawAsync(path);
        if (json == null) return Array.Empty<StockProfile>();
        return JsonConvert.DeserializeObject<List<StockProfile>>(json, SerializerSettings) ?? new List<StockProfile>();
    }

    public async Task<StockProfile?> GetStockProfileAsync(string characterUUID, string entityUUID)
    {
        var all = await GetAllStockProfilesAsync(characterUUID);
        return all.FirstOrDefault(e => e.UUID == entityUUID);
    }

    public async Task UpsertStockProfileAsync(string characterUUID, StockProfile entity)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "stockprofiles");
            EnsureCharacterDirectory(characterUUID);
            var json = await ReadRawUnlockedAsync(path);
            var list = string.IsNullOrEmpty(json)
                ? new List<StockProfile>()
                : JsonConvert.DeserializeObject<List<StockProfile>>(json, SerializerSettings) ?? new List<StockProfile>();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0) list[index] = entity;
            else list.Add(entity);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteStockProfileAsync(string characterUUID, string entityUUID)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "stockprofiles");
            var json = await ReadRawUnlockedAsync(path);
            if (json == null) return;
            var list = JsonConvert.DeserializeObject<List<StockProfile>>(json, SerializerSettings) ?? new List<StockProfile>();
            list.RemoveAll(e => e.UUID == entityUUID);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    // BuildPlan

    public async Task<IReadOnlyList<BuildPlan>> GetAllBuildPlansAsync(string characterUUID)
    {
        var path = CharacterDataPath(characterUUID, "buildplans");
        var json = await ReadRawAsync(path);
        if (json == null) return Array.Empty<BuildPlan>();
        return JsonConvert.DeserializeObject<List<BuildPlan>>(json, SerializerSettings) ?? new List<BuildPlan>();
    }

    public async Task<BuildPlan?> GetBuildPlanAsync(string characterUUID, string entityUUID)
    {
        var all = await GetAllBuildPlansAsync(characterUUID);
        return all.FirstOrDefault(e => e.UUID == entityUUID);
    }

    public async Task UpsertBuildPlanAsync(string characterUUID, BuildPlan entity)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "buildplans");
            EnsureCharacterDirectory(characterUUID);
            var json = await ReadRawUnlockedAsync(path);
            var list = string.IsNullOrEmpty(json)
                ? new List<BuildPlan>()
                : JsonConvert.DeserializeObject<List<BuildPlan>>(json, SerializerSettings) ?? new List<BuildPlan>();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0) list[index] = entity;
            else list.Add(entity);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteBuildPlanAsync(string characterUUID, string entityUUID)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "buildplans");
            var json = await ReadRawUnlockedAsync(path);
            if (json == null) return;
            var list = JsonConvert.DeserializeObject<List<BuildPlan>>(json, SerializerSettings) ?? new List<BuildPlan>();
            list.RemoveAll(e => e.UUID == entityUUID);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    // SupplyChain

    public async Task<IReadOnlyList<SupplyChain>> GetAllSupplyChainsAsync(string characterUUID)
    {
        var path = CharacterDataPath(characterUUID, "supplychains");
        var json = await ReadRawAsync(path);
        if (json == null) return Array.Empty<SupplyChain>();
        return JsonConvert.DeserializeObject<List<SupplyChain>>(json, SerializerSettings) ?? new List<SupplyChain>();
    }

    public async Task<SupplyChain?> GetSupplyChainAsync(string characterUUID, string entityUUID)
    {
        var all = await GetAllSupplyChainsAsync(characterUUID);
        return all.FirstOrDefault(e => e.UUID == entityUUID);
    }

    public async Task UpsertSupplyChainAsync(string characterUUID, SupplyChain entity)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "supplychains");
            EnsureCharacterDirectory(characterUUID);
            var json = await ReadRawUnlockedAsync(path);
            var list = string.IsNullOrEmpty(json)
                ? new List<SupplyChain>()
                : JsonConvert.DeserializeObject<List<SupplyChain>>(json, SerializerSettings) ?? new List<SupplyChain>();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0) list[index] = entity;
            else list.Add(entity);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteSupplyChainAsync(string characterUUID, string entityUUID)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "supplychains");
            var json = await ReadRawUnlockedAsync(path);
            if (json == null) return;
            var list = JsonConvert.DeserializeObject<List<SupplyChain>>(json, SerializerSettings) ?? new List<SupplyChain>();
            list.RemoveAll(e => e.UUID == entityUUID);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    // Asteroid

    public async Task<IReadOnlyList<Asteroid>> GetAllAsteroidsAsync(string characterUUID)
    {
        var path = CharacterDataPath(characterUUID, "asteroids");
        var json = await ReadRawAsync(path);
        if (json == null) return Array.Empty<Asteroid>();
        return JsonConvert.DeserializeObject<List<Asteroid>>(json, SerializerSettings) ?? new List<Asteroid>();
    }

    public async Task<Asteroid?> GetAsteroidAsync(string characterUUID, string entityUUID)
    {
        var all = await GetAllAsteroidsAsync(characterUUID);
        return all.FirstOrDefault(e => e.UUID == entityUUID);
    }

    public async Task UpsertAsteroidAsync(string characterUUID, Asteroid entity)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "asteroids");
            EnsureCharacterDirectory(characterUUID);
            var json = await ReadRawUnlockedAsync(path);
            var list = string.IsNullOrEmpty(json)
                ? new List<Asteroid>()
                : JsonConvert.DeserializeObject<List<Asteroid>>(json, SerializerSettings) ?? new List<Asteroid>();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0) list[index] = entity;
            else list.Add(entity);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteAsteroidAsync(string characterUUID, string entityUUID)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "asteroids");
            var json = await ReadRawUnlockedAsync(path);
            if (json == null) return;
            var list = JsonConvert.DeserializeObject<List<Asteroid>>(json, SerializerSettings) ?? new List<Asteroid>();
            list.RemoveAll(e => e.UUID == entityUUID);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    // Station

    public async Task<IReadOnlyList<Station>> GetAllStationsAsync(string characterUUID)
    {
        var path = CharacterDataPath(characterUUID, "stations");
        var json = await ReadRawAsync(path);
        if (json == null) return Array.Empty<Station>();
        return JsonConvert.DeserializeObject<List<Station>>(json, SerializerSettings) ?? new List<Station>();
    }

    public async Task<Station?> GetStationAsync(string characterUUID, string entityUUID)
    {
        var all = await GetAllStationsAsync(characterUUID);
        return all.FirstOrDefault(e => e.UUID == entityUUID);
    }

    public async Task UpsertStationAsync(string characterUUID, Station entity)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "stations");
            EnsureCharacterDirectory(characterUUID);
            var json = await ReadRawUnlockedAsync(path);
            var list = string.IsNullOrEmpty(json)
                ? new List<Station>()
                : JsonConvert.DeserializeObject<List<Station>>(json, SerializerSettings) ?? new List<Station>();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0) list[index] = entity;
            else list.Add(entity);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteStationAsync(string characterUUID, string entityUUID)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "stations");
            var json = await ReadRawUnlockedAsync(path);
            if (json == null) return;
            var list = JsonConvert.DeserializeObject<List<Station>>(json, SerializerSettings) ?? new List<Station>();
            list.RemoveAll(e => e.UUID == entityUUID);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    // Faction (per-character faction contacts)

    public async Task<IReadOnlyList<Faction>> GetAllFactionsForCharacterAsync(string characterUUID)
    {
        var path = CharacterDataPath(characterUUID, "faction");
        var json = await ReadRawAsync(path);
        if (json == null) return Array.Empty<Faction>();
        return JsonConvert.DeserializeObject<List<Faction>>(json, SerializerSettings) ?? new List<Faction>();
    }

    public async Task<Faction?> GetFactionForCharacterAsync(string characterUUID, string entityUUID)
    {
        var all = await GetAllFactionsForCharacterAsync(characterUUID);
        return all.FirstOrDefault(e => e.UUID == entityUUID);
    }

    public async Task UpsertFactionForCharacterAsync(string characterUUID, Faction entity)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "faction");
            EnsureCharacterDirectory(characterUUID);
            var json = await ReadRawUnlockedAsync(path);
            var list = string.IsNullOrEmpty(json)
                ? new List<Faction>()
                : JsonConvert.DeserializeObject<List<Faction>>(json, SerializerSettings) ?? new List<Faction>();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0) list[index] = entity;
            else list.Add(entity);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteFactionForCharacterAsync(string characterUUID, string entityUUID)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "faction");
            var json = await ReadRawUnlockedAsync(path);
            if (json == null) return;
            var list = JsonConvert.DeserializeObject<List<Faction>>(json, SerializerSettings) ?? new List<Faction>();
            list.RemoveAll(e => e.UUID == entityUUID);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    // ExternalCharacter

    public async Task<IReadOnlyList<ExternalCharacter>> GetAllExternalCharactersAsync(string characterUUID)
    {
        var path = CharacterDataPath(characterUUID, "externalcharacter");
        var json = await ReadRawAsync(path);
        if (json == null) return Array.Empty<ExternalCharacter>();
        return JsonConvert.DeserializeObject<List<ExternalCharacter>>(json, SerializerSettings) ?? new List<ExternalCharacter>();
    }

    public async Task<ExternalCharacter?> GetExternalCharacterAsync(string characterUUID, string entityUUID)
    {
        var all = await GetAllExternalCharactersAsync(characterUUID);
        return all.FirstOrDefault(e => e.UUID == entityUUID);
    }

    public async Task UpsertExternalCharacterAsync(string characterUUID, ExternalCharacter entity)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "externalcharacter");
            EnsureCharacterDirectory(characterUUID);
            var json = await ReadRawUnlockedAsync(path);
            var list = string.IsNullOrEmpty(json)
                ? new List<ExternalCharacter>()
                : JsonConvert.DeserializeObject<List<ExternalCharacter>>(json, SerializerSettings) ?? new List<ExternalCharacter>();
            var index = list.FindIndex(e => e.UUID == entity.UUID);
            if (index >= 0) list[index] = entity;
            else list.Add(entity);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteExternalCharacterAsync(string characterUUID, string entityUUID)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, "externalcharacter");
            var json = await ReadRawUnlockedAsync(path);
            if (json == null) return;
            var list = JsonConvert.DeserializeObject<List<ExternalCharacter>>(json, SerializerSettings) ?? new List<ExternalCharacter>();
            list.RemoveAll(e => e.UUID == entityUUID);
            await WriteAtomicUnlockedAsync(path, JsonConvert.SerializeObject(list, SerializerSettings));
        }
        finally
        {
            _lock.Release();
        }
    }

    // --- Private Static Helpers ---

    private static void EnsureFile(string path, string defaultContent)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, defaultContent);
        }
    }

    private static async Task<string?> ReadRawUnlockedAsync(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        return await File.ReadAllTextAsync(path);
    }

    private static async Task WriteAtomicUnlockedAsync(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (dir != null)
        {
            Directory.CreateDirectory(dir);
        }

        var tempPath = path + ".tmp";
        await File.WriteAllTextAsync(tempPath, content);
        File.Move(tempPath, path, overwrite: true);
    }

    private static async Task<List<T>> ReadListUnlockedAsync<T>(string path)
    {
        if (!File.Exists(path))
        {
            return new List<T>();
        }

        var json = await File.ReadAllTextAsync(path);
        return JsonConvert.DeserializeObject<List<T>>(json) ?? new List<T>();
    }

    // --- Private Instance Helpers ---

    private string DataFilePath(string filename) => Path.Combine(_dataPath, filename);

    private string CharacterDataPath(string characterUUID, string dataType) =>
        Path.Combine(_dataPath, "characters", characterUUID, $"{dataType}.json");

    private void EnsureCharacterDirectory(string characterUUID)
    {
        Directory.CreateDirectory(Path.Combine(_dataPath, "characters", characterUUID));
    }

    private async Task<string?> ReadRawAsync(string path)
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

    private string FactionDataPath(string factionUUID, string filename) =>
        Path.Combine(_dataPath, "factions", factionUUID, filename);

    private void EnsureFactionDirectory(string factionUUID)
    {
        Directory.CreateDirectory(Path.Combine(_dataPath, "factions", factionUUID));
    }

    private async Task<List<T>> ReadFactionListAsync<T>(string factionUUID, string filename)
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

    private async Task<string?> FindFactionForGroupUnlockedAsync(string groupUUID)
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

    private string CharacterPermPath(string characterUUID, string filename) =>
        Path.Combine(_dataPath, "characters", characterUUID, filename);

    private async Task<List<T>> ReadCharacterPermListAsync<T>(string characterUUID, string filename)
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

    private async Task<string?> FindCharacterForGroupUnlockedAsync(string groupUUID)
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
}