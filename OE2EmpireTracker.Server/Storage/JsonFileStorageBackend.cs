using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

    // --- Character Data (raw JSON) ---

    public async Task<string?> GetCharacterDataAsync(string characterUUID, string dataType)
    {
        var path = CharacterDataPath(characterUUID, dataType);
        return await ReadRawAsync(path);
    }

    public async Task<string?> GetCharacterEntityAsync(string characterUUID, string dataType, string entityUUID)
    {
        var json = await GetCharacterDataAsync(characterUUID, dataType);
        if (json == null)
        {
            return null;
        }

        var array = JArray.Parse(json);
        var entity = array.FirstOrDefault(t => t["UUID"]?.ToString() == entityUUID);
        return entity?.ToString(Formatting.Indented);
    }

    public async Task UpsertCharacterDataAsync(string characterUUID, string dataType, string json)
    {
        var path = CharacterDataPath(characterUUID, dataType);
        EnsureCharacterDirectory(characterUUID);
        await WriteAtomicAsync(path, json);
    }

    public async Task UpsertCharacterEntityAsync(string characterUUID, string dataType, string entityUUID, string json)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, dataType);
            EnsureCharacterDirectory(characterUUID);

            var existing = await ReadRawUnlockedAsync(path);
            var array = string.IsNullOrEmpty(existing) ? new JArray() : JArray.Parse(existing);
            var newEntity = JObject.Parse(json);

            var index = FindEntityIndex(array, entityUUID);
            if (index >= 0)
            {
                array[index] = newEntity;
            }
            else
            {
                array.Add(newEntity);
            }

            await WriteAtomicUnlockedAsync(path, array.ToString(Formatting.Indented));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteCharacterEntityAsync(string characterUUID, string dataType, string entityUUID)
    {
        await _lock.WaitAsync();
        try
        {
            var path = CharacterDataPath(characterUUID, dataType);
            var existing = await ReadRawUnlockedAsync(path);
            if (existing == null)
            {
                return;
            }

            var array = JArray.Parse(existing);
            var index = FindEntityIndex(array, entityUUID);
            if (index >= 0)
            {
                array.RemoveAt(index);
                await WriteAtomicUnlockedAsync(path, array.ToString(Formatting.Indented));
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string?> GetAllCharacterDataAsync(string characterUUID)
    {
        var dir = Path.Combine(_dataPath, "characters", characterUUID);
        if (!Directory.Exists(dir))
        {
            return null;
        }

        var result = new JObject();
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (name == "sharing" || name == "preferences")
            {
                continue;
            }

            var content = await File.ReadAllTextAsync(file);
            result[name] = JToken.Parse(content);
        }

        return result.ToString(Formatting.Indented);
    }

    public async Task PutAllCharacterDataAsync(string characterUUID, string json)
    {
        EnsureCharacterDirectory(characterUUID);
        var obj = JObject.Parse(json);
        foreach (var prop in obj.Properties())
        {
            var path = CharacterDataPath(characterUUID, prop.Name);
            await WriteAtomicAsync(path, prop.Value.ToString(Formatting.Indented));
        }
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

    // --- Private Static Helpers ---

    private static void EnsureFile(string path, string defaultContent)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, defaultContent);
        }
    }

    private static int FindEntityIndex(JArray array, string entityUUID)
    {
        for (int i = 0; i < array.Count; i++)
        {
            if (array[i]["UUID"]?.ToString() == entityUUID)
            {
                return i;
            }
        }

        return -1;
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
}
