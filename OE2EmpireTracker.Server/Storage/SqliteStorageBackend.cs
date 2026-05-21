using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Server.Storage;

/// <summary>
/// SQLite-based implementation of <see cref="IStorageBackend"/>.
/// Stores data as JSON blobs in a single .db file with ACID guarantees.
/// Suitable for single-server deployments with no external dependencies.
/// </summary>
public class SqliteStorageBackend : IStorageBackend
{
    private const string CreateTablesScript = @"
CREATE TABLE IF NOT EXISTS Factions (
    UUID TEXT PRIMARY KEY,
    Data TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Characters (
    UUID TEXT PRIMARY KEY,
    Data TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Tokens (
    Id TEXT PRIMARY KEY,
    Data TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS MembershipActions (
    Id TEXT PRIMARY KEY,
    FactionUUID TEXT,
    ExpiresUtc TEXT,
    Data TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS CharacterData (
    CharacterUUID TEXT,
    DataType TEXT,
    EntityUUID TEXT,
    Data TEXT NOT NULL,
    PRIMARY KEY (CharacterUUID, DataType, EntityUUID)
);

CREATE TABLE IF NOT EXISTS CharacterCollections (
    CharacterUUID TEXT,
    DataType TEXT,
    Data TEXT NOT NULL,
    PRIMARY KEY (CharacterUUID, DataType)
);

CREATE TABLE IF NOT EXISTS GlobalData (
    DataType TEXT PRIMARY KEY,
    Data TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS SharingRules (
    CharacterUUID TEXT PRIMARY KEY,
    Data TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS CharacterPreferences (
    CharacterUUID TEXT PRIMARY KEY,
    Data TEXT NOT NULL
);
";

    private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
    };

    private readonly string _connectionString;
    private readonly ILogger<SqliteStorageBackend> _logger;

    public SqliteStorageBackend(IConfiguration configuration, ILogger<SqliteStorageBackend> logger)
    {
        _connectionString = configuration["Storage:ConnectionString"]
            ?? "Data Source=./data/oe2server.db";
        _logger = logger;
    }

    // --- Lifecycle ---

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        // Ensure the directory for the database file exists
        var connBuilder = new SqliteConnectionStringBuilder(_connectionString);
        var dbPath = connBuilder.DataSource;
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = CreateTablesScript;
        await cmd.ExecuteNonQueryAsync(ct);

        _logger.LogInformation("SqliteStorageBackend initialized at {Path}", dbPath);
    }

    public async Task<bool> ValidateConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SQLite storage validation failed");
            return false;
        }
    }

    // --- Factions ---

    public async Task<ServerFaction?> GetFactionAsync(string uuid)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Data FROM Factions WHERE UUID = @uuid";
        cmd.Parameters.AddWithValue("@uuid", uuid);

        var result = await cmd.ExecuteScalarAsync();
        return result is string json
            ? JsonConvert.DeserializeObject<ServerFaction>(json)
            : null;
    }

    public async Task<IReadOnlyList<ServerFaction>> GetAllFactionsAsync()
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Data FROM Factions";

        var list = new List<ServerFaction>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var json = reader.GetString(0);
            var faction = JsonConvert.DeserializeObject<ServerFaction>(json);
            if (faction != null)
            {
                list.Add(faction);
            }
        }

        return list;
    }

    public async Task UpsertFactionAsync(ServerFaction faction)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Factions (UUID, Data) VALUES (@uuid, @data)
            ON CONFLICT(UUID) DO UPDATE SET Data = @data";
        cmd.Parameters.AddWithValue("@uuid", faction.UUID);
        cmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(faction, SerializerSettings));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteFactionAsync(string uuid)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Factions WHERE UUID = @uuid";
        cmd.Parameters.AddWithValue("@uuid", uuid);
        await cmd.ExecuteNonQueryAsync();
    }

    // --- Characters ---

    public async Task<ServerCharacter?> GetCharacterAsync(string uuid)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Data FROM Characters WHERE UUID = @uuid";
        cmd.Parameters.AddWithValue("@uuid", uuid);

        var result = await cmd.ExecuteScalarAsync();
        return result is string json
            ? JsonConvert.DeserializeObject<ServerCharacter>(json)
            : null;
    }

    public async Task<IReadOnlyList<ServerCharacter>> GetAllCharactersAsync()
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Data FROM Characters";

        var list = new List<ServerCharacter>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var json = reader.GetString(0);
            var character = JsonConvert.DeserializeObject<ServerCharacter>(json);
            if (character != null)
            {
                list.Add(character);
            }
        }

        return list;
    }

    public async Task UpsertCharacterAsync(ServerCharacter character)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Characters (UUID, Data) VALUES (@uuid, @data)
            ON CONFLICT(UUID) DO UPDATE SET Data = @data";
        cmd.Parameters.AddWithValue("@uuid", character.UUID);
        cmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(character, SerializerSettings));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteCharacterAsync(string uuid)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Characters WHERE UUID = @uuid";
        cmd.Parameters.AddWithValue("@uuid", uuid);
        await cmd.ExecuteNonQueryAsync();
    }

    // --- Character Data (raw JSON) ---

    public async Task<string?> GetCharacterDataAsync(string characterUUID, string dataType)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Data FROM CharacterCollections WHERE CharacterUUID = @charUuid AND DataType = @dataType";
        cmd.Parameters.AddWithValue("@charUuid", characterUUID);
        cmd.Parameters.AddWithValue("@dataType", dataType);

        var result = await cmd.ExecuteScalarAsync();
        return result as string;
    }

    public async Task<string?> GetCharacterEntityAsync(string characterUUID, string dataType, string entityUUID)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Data FROM CharacterData WHERE CharacterUUID = @charUuid AND DataType = @dataType AND EntityUUID = @entityUuid";
        cmd.Parameters.AddWithValue("@charUuid", characterUUID);
        cmd.Parameters.AddWithValue("@dataType", dataType);
        cmd.Parameters.AddWithValue("@entityUuid", entityUUID);

        var result = await cmd.ExecuteScalarAsync();
        return result as string;
    }

    public async Task UpsertCharacterDataAsync(string characterUUID, string dataType, string json)
    {
        using var conn = await OpenConnectionAsync();
        using var transaction = conn.BeginTransaction();

        // Replace the collection blob
        using var upsertCmd = conn.CreateCommand();
        upsertCmd.CommandText = @"INSERT INTO CharacterCollections (CharacterUUID, DataType, Data)
            VALUES (@charUuid, @dataType, @data)
            ON CONFLICT(CharacterUUID, DataType) DO UPDATE SET Data = @data";
        upsertCmd.Parameters.AddWithValue("@charUuid", characterUUID);
        upsertCmd.Parameters.AddWithValue("@dataType", dataType);
        upsertCmd.Parameters.AddWithValue("@data", json);
        await upsertCmd.ExecuteNonQueryAsync();

        // Also sync individual entities in CharacterData table
        using var deleteCmd = conn.CreateCommand();
        deleteCmd.CommandText = "DELETE FROM CharacterData WHERE CharacterUUID = @charUuid AND DataType = @dataType";
        deleteCmd.Parameters.AddWithValue("@charUuid", characterUUID);
        deleteCmd.Parameters.AddWithValue("@dataType", dataType);
        await deleteCmd.ExecuteNonQueryAsync();

        // Parse the JSON array and insert individual entities
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var array = JArray.Parse(json);
                foreach (var item in array)
                {
                    var entityUuid = item["UUID"]?.ToString();
                    if (string.IsNullOrEmpty(entityUuid))
                    {
                        continue;
                    }

                    using var insertCmd = conn.CreateCommand();
                    insertCmd.CommandText = @"INSERT INTO CharacterData (CharacterUUID, DataType, EntityUUID, Data)
                        VALUES (@charUuid, @dataType, @entityUuid, @entityData)";
                    insertCmd.Parameters.AddWithValue("@charUuid", characterUUID);
                    insertCmd.Parameters.AddWithValue("@dataType", dataType);
                    insertCmd.Parameters.AddWithValue("@entityUuid", entityUuid);
                    insertCmd.Parameters.AddWithValue("@entityData", item.ToString(Formatting.Indented));
                    await insertCmd.ExecuteNonQueryAsync();
                }
            }
            catch (JsonReaderException)
            {
                // Not a JSON array — just store as collection blob only
            }
        }

        transaction.Commit();
    }

    public async Task UpsertCharacterEntityAsync(string characterUUID, string dataType, string entityUUID, string json)
    {
        using var conn = await OpenConnectionAsync();
        using var transaction = conn.BeginTransaction();

        // Upsert in the individual entity table
        using var entityCmd = conn.CreateCommand();
        entityCmd.CommandText = @"INSERT INTO CharacterData (CharacterUUID, DataType, EntityUUID, Data)
            VALUES (@charUuid, @dataType, @entityUuid, @data)
            ON CONFLICT(CharacterUUID, DataType, EntityUUID) DO UPDATE SET Data = @data";
        entityCmd.Parameters.AddWithValue("@charUuid", characterUUID);
        entityCmd.Parameters.AddWithValue("@dataType", dataType);
        entityCmd.Parameters.AddWithValue("@entityUuid", entityUUID);
        entityCmd.Parameters.AddWithValue("@data", json);
        await entityCmd.ExecuteNonQueryAsync();

        // Rebuild the collection blob from all entities
        await RebuildCollectionBlobAsync(conn, characterUUID, dataType);

        transaction.Commit();
    }

    public async Task DeleteCharacterEntityAsync(string characterUUID, string dataType, string entityUUID)
    {
        using var conn = await OpenConnectionAsync();
        using var transaction = conn.BeginTransaction();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM CharacterData WHERE CharacterUUID = @charUuid AND DataType = @dataType AND EntityUUID = @entityUuid";
        cmd.Parameters.AddWithValue("@charUuid", characterUUID);
        cmd.Parameters.AddWithValue("@dataType", dataType);
        cmd.Parameters.AddWithValue("@entityUuid", entityUUID);
        await cmd.ExecuteNonQueryAsync();

        // Rebuild the collection blob from remaining entities
        await RebuildCollectionBlobAsync(conn, characterUUID, dataType);

        transaction.Commit();
    }

    public async Task<string?> GetAllCharacterDataAsync(string characterUUID)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT DataType, Data FROM CharacterCollections WHERE CharacterUUID = @charUuid";
        cmd.Parameters.AddWithValue("@charUuid", characterUUID);

        var result = new JObject();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var dataType = reader.GetString(0);
            var data = reader.GetString(1);
            result[dataType] = JToken.Parse(data);
        }

        return result.HasValues ? result.ToString(Formatting.Indented) : null;
    }

    public async Task PutAllCharacterDataAsync(string characterUUID, string json)
    {
        var obj = JObject.Parse(json);
        foreach (var prop in obj.Properties())
        {
            await UpsertCharacterDataAsync(characterUUID, prop.Name, prop.Value.ToString(Formatting.Indented));
        }
    }

    // --- Global/Baseline Data ---

    public async Task<string?> GetGlobalDataAsync(string dataType)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Data FROM GlobalData WHERE DataType = @dataType";
        cmd.Parameters.AddWithValue("@dataType", dataType);

        var result = await cmd.ExecuteScalarAsync();
        return result as string;
    }

    public async Task UpsertGlobalDataAsync(string dataType, string json)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO GlobalData (DataType, Data) VALUES (@dataType, @data)
            ON CONFLICT(DataType) DO UPDATE SET Data = @data";
        cmd.Parameters.AddWithValue("@dataType", dataType);
        cmd.Parameters.AddWithValue("@data", json);
        await cmd.ExecuteNonQueryAsync();
    }

    // --- Tokens ---

    public async Task<ApiToken?> FindTokenByHashAsync(string tokenHash)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Data FROM Tokens";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var json = reader.GetString(0);
            var token = JsonConvert.DeserializeObject<ApiToken>(json);
            if (token?.TokenHash == tokenHash)
            {
                return token;
            }
        }

        return null;
    }

    public async Task<IReadOnlyList<ApiToken>> GetAllTokensAsync()
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Data FROM Tokens";

        var list = new List<ApiToken>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var json = reader.GetString(0);
            var token = JsonConvert.DeserializeObject<ApiToken>(json);
            if (token != null)
            {
                list.Add(token);
            }
        }

        return list;
    }

    public async Task UpsertTokenAsync(ApiToken token)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Tokens (Id, Data) VALUES (@id, @data)
            ON CONFLICT(Id) DO UPDATE SET Data = @data";
        cmd.Parameters.AddWithValue("@id", token.Id);
        cmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(token, SerializerSettings));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteTokenAsync(string id)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Tokens WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    // --- Membership Actions ---

    public async Task<IReadOnlyList<MembershipAction>> GetFactionActionsAsync(string factionUUID)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Data FROM MembershipActions WHERE FactionUUID = @factionUuid";
        cmd.Parameters.AddWithValue("@factionUuid", factionUUID);

        var list = new List<MembershipAction>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var json = reader.GetString(0);
            var action = JsonConvert.DeserializeObject<MembershipAction>(json);
            if (action != null)
            {
                list.Add(action);
            }
        }

        return list;
    }

    public async Task UpsertMembershipActionAsync(MembershipAction action)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO MembershipActions (Id, FactionUUID, ExpiresUtc, Data)
            VALUES (@id, @factionUuid, @expiresUtc, @data)
            ON CONFLICT(Id) DO UPDATE SET FactionUUID = @factionUuid, ExpiresUtc = @expiresUtc, Data = @data";
        cmd.Parameters.AddWithValue("@id", action.Id);
        cmd.Parameters.AddWithValue("@factionUuid", action.FactionUUID);
        cmd.Parameters.AddWithValue("@expiresUtc", action.ExpiresUtc.ToString("O"));
        cmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(action, SerializerSettings));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteMembershipActionAsync(string id)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM MembershipActions WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteExpiredActionsAsync(DateTime cutoff)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM MembershipActions WHERE ExpiresUtc <= @cutoff";
        cmd.Parameters.AddWithValue("@cutoff", cutoff.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }

    // --- Sharing Rules ---

    public async Task<IReadOnlyList<SharingRule>> GetSharingRulesForCharacterAsync(string characterUUID)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Data FROM SharingRules WHERE CharacterUUID = @charUuid";
        cmd.Parameters.AddWithValue("@charUuid", characterUUID);

        var result = await cmd.ExecuteScalarAsync();
        if (result is string json)
        {
            return JsonConvert.DeserializeObject<List<SharingRule>>(json) ?? new List<SharingRule>();
        }

        return Array.Empty<SharingRule>();
    }

    public async Task UpsertSharingRulesAsync(string characterUUID, IReadOnlyList<SharingRule> rules)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO SharingRules (CharacterUUID, Data) VALUES (@charUuid, @data)
            ON CONFLICT(CharacterUUID) DO UPDATE SET Data = @data";
        cmd.Parameters.AddWithValue("@charUuid", characterUUID);
        cmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(rules, SerializerSettings));
        await cmd.ExecuteNonQueryAsync();
    }

    // --- Character Preferences ---

    public async Task<CharacterPreferences?> GetCharacterPreferencesAsync(string characterUUID)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Data FROM CharacterPreferences WHERE CharacterUUID = @charUuid";
        cmd.Parameters.AddWithValue("@charUuid", characterUUID);

        var result = await cmd.ExecuteScalarAsync();
        return result is string json
            ? JsonConvert.DeserializeObject<CharacterPreferences>(json)
            : null;
    }

    public async Task UpsertCharacterPreferencesAsync(CharacterPreferences prefs)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO CharacterPreferences (CharacterUUID, Data) VALUES (@charUuid, @data)
            ON CONFLICT(CharacterUUID) DO UPDATE SET Data = @data";
        cmd.Parameters.AddWithValue("@charUuid", prefs.CharacterUUID);
        cmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(prefs, SerializerSettings));
        await cmd.ExecuteNonQueryAsync();
    }

    // --- Faction Permission Entities ---

    public Task<IReadOnlyList<FactionCapability>> GetFactionCapabilitiesAsync(string factionUUID)
        => throw new NotImplementedException();

    public Task UpsertFactionCapabilityAsync(FactionCapability capability)
        => throw new NotImplementedException();

    public Task DeleteFactionCapabilityAsync(string factionUUID, string capabilityUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<FactionClearanceLevel>> GetFactionClearanceLevelsAsync(string factionUUID)
        => throw new NotImplementedException();

    public Task UpsertFactionClearanceLevelAsync(FactionClearanceLevel level)
        => throw new NotImplementedException();

    public Task DeleteFactionClearanceLevelAsync(string factionUUID, string levelUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<FactionPermissionGroup>> GetFactionGroupsAsync(string factionUUID)
        => throw new NotImplementedException();

    public Task<FactionPermissionGroup?> GetFactionGroupAsync(string factionUUID, string groupUUID)
        => throw new NotImplementedException();

    public Task UpsertFactionGroupAsync(FactionPermissionGroup group)
        => throw new NotImplementedException();

    public Task DeleteFactionGroupAsync(string factionUUID, string groupUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<FactionGroupCapability>> GetFactionGroupCapabilitiesAsync(string groupUUID)
        => throw new NotImplementedException();

    public Task AddFactionGroupCapabilityAsync(FactionGroupCapability item)
        => throw new NotImplementedException();

    public Task RemoveFactionGroupCapabilityAsync(string groupUUID, string capabilityUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<FactionGroupSharingRule>> GetFactionGroupSharingRulesAsync(string groupUUID)
        => throw new NotImplementedException();

    public Task UpsertFactionGroupSharingRuleAsync(FactionGroupSharingRule rule)
        => throw new NotImplementedException();

    public Task DeleteFactionGroupSharingRuleAsync(string groupUUID, string ruleUUID)
        => throw new NotImplementedException();

    public Task<FactionMemberPermissions?> GetFactionMemberPermissionsAsync(string factionUUID, string characterUUID)
        => throw new NotImplementedException();

    public Task UpsertFactionMemberPermissionsAsync(FactionMemberPermissions perms)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<FactionMemberPermissions>> GetAllFactionMembersPermissionsAsync(string factionUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<FactionMemberCapability>> GetFactionMemberCapabilitiesAsync(string factionUUID, string characterUUID)
        => throw new NotImplementedException();

    public Task AddFactionMemberCapabilityAsync(FactionMemberCapability item)
        => throw new NotImplementedException();

    public Task RemoveFactionMemberCapabilityAsync(string factionUUID, string characterUUID, string capabilityUUID)
        => throw new NotImplementedException();

    // --- Character Permission Entities ---

    public Task<IReadOnlyList<CharacterCapability>> GetCharacterCapabilitiesAsync(string characterUUID)
        => throw new NotImplementedException();

    public Task UpsertCharacterCapabilityAsync(CharacterCapability capability)
        => throw new NotImplementedException();

    public Task DeleteCharacterCapabilityAsync(string characterUUID, string capabilityUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<CharacterClearanceLevel>> GetCharacterClearanceLevelsAsync(string characterUUID)
        => throw new NotImplementedException();

    public Task UpsertCharacterClearanceLevelAsync(CharacterClearanceLevel level)
        => throw new NotImplementedException();

    public Task DeleteCharacterClearanceLevelAsync(string characterUUID, string levelUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<CharacterPermissionGroup>> GetCharacterGroupsAsync(string characterUUID)
        => throw new NotImplementedException();

    public Task<CharacterPermissionGroup?> GetCharacterGroupAsync(string characterUUID, string groupUUID)
        => throw new NotImplementedException();

    public Task UpsertCharacterGroupAsync(CharacterPermissionGroup group)
        => throw new NotImplementedException();

    public Task DeleteCharacterGroupAsync(string characterUUID, string groupUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<CharacterGroupCapability>> GetCharacterGroupCapabilitiesAsync(string groupUUID)
        => throw new NotImplementedException();

    public Task AddCharacterGroupCapabilityAsync(CharacterGroupCapability item)
        => throw new NotImplementedException();

    public Task RemoveCharacterGroupCapabilityAsync(string groupUUID, string capabilityUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<CharacterGroupSharingRule>> GetCharacterGroupSharingRulesAsync(string groupUUID)
        => throw new NotImplementedException();

    public Task UpsertCharacterGroupSharingRuleAsync(CharacterGroupSharingRule rule)
        => throw new NotImplementedException();

    public Task DeleteCharacterGroupSharingRuleAsync(string groupUUID, string ruleUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<CharacterGranteePermissions>> GetCharacterGranteesAsync(string ownerCharacterUUID)
        => throw new NotImplementedException();

    public Task UpsertCharacterGranteePermissionsAsync(CharacterGranteePermissions perms)
        => throw new NotImplementedException();

    public Task DeleteCharacterGranteePermissionsAsync(string ownerCharacterUUID, string granteeUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<CharacterGranteeCapability>> GetCharacterGranteeCapabilitiesAsync(string ownerCharacterUUID, string granteeUUID)
        => throw new NotImplementedException();

    public Task AddCharacterGranteeCapabilityAsync(CharacterGranteeCapability item)
        => throw new NotImplementedException();

    public Task RemoveCharacterGranteeCapabilityAsync(string ownerCharacterUUID, string granteeUUID, string capabilityUUID)
        => throw new NotImplementedException();

    // --- Intel and Audit ---

    public Task<IReadOnlyList<IntelComment>> GetIntelCommentsForTargetAsync(string targetCharacterUUID)
        => throw new NotImplementedException();

    public Task<IntelComment?> GetIntelCommentAsync(string commentUUID)
        => throw new NotImplementedException();

    public Task UpsertIntelCommentAsync(IntelComment comment)
        => throw new NotImplementedException();

    public Task DeleteIntelCommentAsync(string commentUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForCommentAsync(string commentUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForFactionAsync(string factionUUID)
        => throw new NotImplementedException();

    public Task UpsertIntelShareAsync(IntelCommentFactionShare share)
        => throw new NotImplementedException();

    public Task DeleteIntelShareAsync(string shareUUID)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<PermissionAuditEntry>> GetPermissionAuditEntriesAsync(DateTime? startDate = null, DateTime? endDate = null, PermissionActionType? actionType = null, string? actorUUID = null, string? targetUUID = null)
        => throw new NotImplementedException();

    public Task AppendPermissionAuditEntryAsync(PermissionAuditEntry entry)
        => throw new NotImplementedException();

    public Task DeleteExpiredAuditEntriesAsync(DateTime cutoff)
        => throw new NotImplementedException();

    // --- Typed Entity CRUD (per-character domain entities) --- NOT YET IMPLEMENTED ---

    public Task<IReadOnlyList<Colony>> GetAllColoniesAsync(string characterUUID) => throw new NotImplementedException();
    public Task<Colony?> GetColonyAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertColonyAsync(string characterUUID, Colony entity) => throw new NotImplementedException();
    public Task DeleteColonyAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<Blueprint>> GetAllBlueprintsAsync(string characterUUID) => throw new NotImplementedException();
    public Task<Blueprint?> GetBlueprintAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertBlueprintAsync(string characterUUID, Blueprint entity) => throw new NotImplementedException();
    public Task DeleteBlueprintAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<Survey>> GetAllSurveysAsync(string characterUUID) => throw new NotImplementedException();
    public Task<Survey?> GetSurveyAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertSurveyAsync(string characterUUID, Survey entity) => throw new NotImplementedException();
    public Task DeleteSurveyAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<PlayerProfile>> GetAllPlayerProfilesAsync(string characterUUID) => throw new NotImplementedException();
    public Task<PlayerProfile?> GetPlayerProfileAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertPlayerProfileAsync(string characterUUID, PlayerProfile entity) => throw new NotImplementedException();
    public Task DeletePlayerProfileAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<DeliveryRoute>> GetAllDeliveryRoutesAsync(string characterUUID) => throw new NotImplementedException();
    public Task<DeliveryRoute?> GetDeliveryRouteAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertDeliveryRouteAsync(string characterUUID, DeliveryRoute entity) => throw new NotImplementedException();
    public Task DeleteDeliveryRouteAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<DeliveryPlan>> GetAllDeliveryPlansAsync(string characterUUID) => throw new NotImplementedException();
    public Task<DeliveryPlan?> GetDeliveryPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertDeliveryPlanAsync(string characterUUID, DeliveryPlan entity) => throw new NotImplementedException();
    public Task DeleteDeliveryPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<Ship>> GetAllShipsAsync(string characterUUID) => throw new NotImplementedException();
    public Task<Ship?> GetShipAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertShipAsync(string characterUUID, Ship entity) => throw new NotImplementedException();
    public Task DeleteShipAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<ShipTemplate>> GetAllShipTemplatesAsync(string characterUUID) => throw new NotImplementedException();
    public Task<ShipTemplate?> GetShipTemplateAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertShipTemplateAsync(string characterUUID, ShipTemplate entity) => throw new NotImplementedException();
    public Task DeleteShipTemplateAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<MarketListing>> GetAllMarketListingsAsync(string characterUUID) => throw new NotImplementedException();
    public Task<MarketListing?> GetMarketListingAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertMarketListingAsync(string characterUUID, MarketListing entity) => throw new NotImplementedException();
    public Task DeleteMarketListingAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<MarketTransaction>> GetAllMarketTransactionsAsync(string characterUUID) => throw new NotImplementedException();
    public Task<MarketTransaction?> GetMarketTransactionAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertMarketTransactionAsync(string characterUUID, MarketTransaction entity) => throw new NotImplementedException();
    public Task DeleteMarketTransactionAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<PricingPlan>> GetAllPricingPlansAsync(string characterUUID) => throw new NotImplementedException();
    public Task<PricingPlan?> GetPricingPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertPricingPlanAsync(string characterUUID, PricingPlan entity) => throw new NotImplementedException();
    public Task DeletePricingPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<StockPlan>> GetAllStockPlansAsync(string characterUUID) => throw new NotImplementedException();
    public Task<StockPlan?> GetStockPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertStockPlanAsync(string characterUUID, StockPlan entity) => throw new NotImplementedException();
    public Task DeleteStockPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<StockProfile>> GetAllStockProfilesAsync(string characterUUID) => throw new NotImplementedException();
    public Task<StockProfile?> GetStockProfileAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertStockProfileAsync(string characterUUID, StockProfile entity) => throw new NotImplementedException();
    public Task DeleteStockProfileAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<BuildPlan>> GetAllBuildPlansAsync(string characterUUID) => throw new NotImplementedException();
    public Task<BuildPlan?> GetBuildPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertBuildPlanAsync(string characterUUID, BuildPlan entity) => throw new NotImplementedException();
    public Task DeleteBuildPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<SupplyChain>> GetAllSupplyChainsAsync(string characterUUID) => throw new NotImplementedException();
    public Task<SupplyChain?> GetSupplyChainAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertSupplyChainAsync(string characterUUID, SupplyChain entity) => throw new NotImplementedException();
    public Task DeleteSupplyChainAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<Asteroid>> GetAllAsteroidsAsync(string characterUUID) => throw new NotImplementedException();
    public Task<Asteroid?> GetAsteroidAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertAsteroidAsync(string characterUUID, Asteroid entity) => throw new NotImplementedException();
    public Task DeleteAsteroidAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<Station>> GetAllStationsAsync(string characterUUID) => throw new NotImplementedException();
    public Task<Station?> GetStationAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertStationAsync(string characterUUID, Station entity) => throw new NotImplementedException();
    public Task DeleteStationAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<Faction>> GetAllFactionsForCharacterAsync(string characterUUID) => throw new NotImplementedException();
    public Task<Faction?> GetFactionForCharacterAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertFactionForCharacterAsync(string characterUUID, Faction entity) => throw new NotImplementedException();
    public Task DeleteFactionForCharacterAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<ExternalCharacter>> GetAllExternalCharactersAsync(string characterUUID) => throw new NotImplementedException();
    public Task<ExternalCharacter?> GetExternalCharacterAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();
    public Task UpsertExternalCharacterAsync(string characterUUID, ExternalCharacter entity) => throw new NotImplementedException();
    public Task DeleteExternalCharacterAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

    // --- Private Helpers ---

    private static async Task RebuildCollectionBlobAsync(SqliteConnection conn, string characterUUID, string dataType)
    {
        using var selectCmd = conn.CreateCommand();
        selectCmd.CommandText = "SELECT Data FROM CharacterData WHERE CharacterUUID = @charUuid AND DataType = @dataType";
        selectCmd.Parameters.AddWithValue("@charUuid", characterUUID);
        selectCmd.Parameters.AddWithValue("@dataType", dataType);

        var array = new JArray();
        using var reader = await selectCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var entityJson = reader.GetString(0);
            array.Add(JToken.Parse(entityJson));
        }

        using var upsertCmd = conn.CreateCommand();
        upsertCmd.CommandText = @"INSERT INTO CharacterCollections (CharacterUUID, DataType, Data)
            VALUES (@charUuid, @dataType, @data)
            ON CONFLICT(CharacterUUID, DataType) DO UPDATE SET Data = @data";
        upsertCmd.Parameters.AddWithValue("@charUuid", characterUUID);
        upsertCmd.Parameters.AddWithValue("@dataType", dataType);
        upsertCmd.Parameters.AddWithValue("@data", array.ToString(Formatting.Indented));
        await upsertCmd.ExecuteNonQueryAsync();
    }

    private async Task<SqliteConnection> OpenConnectionAsync()
    {
        var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }
}