using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;

namespace OE2EmpireTracker.Server.Storage;

/// <summary>
/// PostgreSQL-based implementation of <see cref="IStorageBackend"/>.
/// Stores data as JSON blobs in tables with ACID guarantees.
/// Suitable for multi-server deployments with shared database.
/// </summary>
public class PostgresStorageBackend : IStorageBackend
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
    private readonly ILogger<PostgresStorageBackend> _logger;

    public PostgresStorageBackend(IConfiguration configuration, ILogger<PostgresStorageBackend> logger)
    {
        _connectionString = configuration["Storage:ConnectionString"]
            ?? "Host=localhost;Database=oe2empiretracker;Username=postgres;Password=postgres";
        _logger = logger;
    }

    // --- Lifecycle ---

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        using var cmd = new NpgsqlCommand(CreateTablesScript, conn);
        await cmd.ExecuteNonQueryAsync(ct);

        _logger.LogInformation("PostgresStorageBackend initialized");
    }

    public async Task<bool> ValidateConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PostgreSQL storage validation failed");
            return false;
        }
    }

    // --- Factions ---

    public async Task<ServerFaction?> GetFactionAsync(string uuid)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand("SELECT Data FROM Factions WHERE UUID = @uuid", conn);
        cmd.Parameters.AddWithValue("@uuid", uuid);

        var result = await cmd.ExecuteScalarAsync();
        return result is string json
            ? JsonConvert.DeserializeObject<ServerFaction>(json)
            : null;
    }

    public async Task<IReadOnlyList<ServerFaction>> GetAllFactionsAsync()
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand("SELECT Data FROM Factions", conn);

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
        using var cmd = new NpgsqlCommand(@"INSERT INTO Factions (UUID, Data) VALUES (@uuid, @data)
            ON CONFLICT (UUID) DO UPDATE SET Data = @data", conn);
        cmd.Parameters.AddWithValue("@uuid", faction.UUID);
        cmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(faction, SerializerSettings));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteFactionAsync(string uuid)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand("DELETE FROM Factions WHERE UUID = @uuid", conn);
        cmd.Parameters.AddWithValue("@uuid", uuid);
        await cmd.ExecuteNonQueryAsync();
    }

    // --- Characters ---

    public async Task<ServerCharacter?> GetCharacterAsync(string uuid)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand("SELECT Data FROM Characters WHERE UUID = @uuid", conn);
        cmd.Parameters.AddWithValue("@uuid", uuid);

        var result = await cmd.ExecuteScalarAsync();
        return result is string json
            ? JsonConvert.DeserializeObject<ServerCharacter>(json)
            : null;
    }

    public async Task<IReadOnlyList<ServerCharacter>> GetAllCharactersAsync()
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand("SELECT Data FROM Characters", conn);

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
        using var cmd = new NpgsqlCommand(@"INSERT INTO Characters (UUID, Data) VALUES (@uuid, @data)
            ON CONFLICT (UUID) DO UPDATE SET Data = @data", conn);
        cmd.Parameters.AddWithValue("@uuid", character.UUID);
        cmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(character, SerializerSettings));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteCharacterAsync(string uuid)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand("DELETE FROM Characters WHERE UUID = @uuid", conn);
        cmd.Parameters.AddWithValue("@uuid", uuid);
        await cmd.ExecuteNonQueryAsync();
    }

    // --- Character Data (raw JSON) ---

    public async Task<string?> GetCharacterDataAsync(string characterUUID, string dataType)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand(
            "SELECT Data FROM CharacterCollections WHERE CharacterUUID = @charUuid AND DataType = @dataType", conn);
        cmd.Parameters.AddWithValue("@charUuid", characterUUID);
        cmd.Parameters.AddWithValue("@dataType", dataType);

        var result = await cmd.ExecuteScalarAsync();
        return result as string;
    }

    public async Task<string?> GetCharacterEntityAsync(string characterUUID, string dataType, string entityUUID)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand(
            "SELECT Data FROM CharacterData WHERE CharacterUUID = @charUuid AND DataType = @dataType AND EntityUUID = @entityUuid", conn);
        cmd.Parameters.AddWithValue("@charUuid", characterUUID);
        cmd.Parameters.AddWithValue("@dataType", dataType);
        cmd.Parameters.AddWithValue("@entityUuid", entityUUID);

        var result = await cmd.ExecuteScalarAsync();
        return result as string;
    }

    public async Task UpsertCharacterDataAsync(string characterUUID, string dataType, string json)
    {
        using var conn = await OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync();

        using var upsertCmd = new NpgsqlCommand(@"INSERT INTO CharacterCollections (CharacterUUID, DataType, Data)
            VALUES (@charUuid, @dataType, @data)
            ON CONFLICT (CharacterUUID, DataType) DO UPDATE SET Data = @data", conn, transaction);
        upsertCmd.Parameters.AddWithValue("@charUuid", characterUUID);
        upsertCmd.Parameters.AddWithValue("@dataType", dataType);
        upsertCmd.Parameters.AddWithValue("@data", json);
        await upsertCmd.ExecuteNonQueryAsync();

        using var deleteCmd = new NpgsqlCommand(
            "DELETE FROM CharacterData WHERE CharacterUUID = @charUuid AND DataType = @dataType", conn, transaction);
        deleteCmd.Parameters.AddWithValue("@charUuid", characterUUID);
        deleteCmd.Parameters.AddWithValue("@dataType", dataType);
        await deleteCmd.ExecuteNonQueryAsync();

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

                    using var insertCmd = new NpgsqlCommand(@"INSERT INTO CharacterData (CharacterUUID, DataType, EntityUUID, Data)
                        VALUES (@charUuid, @dataType, @entityUuid, @entityData)", conn, transaction);
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

        await transaction.CommitAsync();
    }

    public async Task UpsertCharacterEntityAsync(string characterUUID, string dataType, string entityUUID, string json)
    {
        using var conn = await OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync();

        using var entityCmd = new NpgsqlCommand(@"INSERT INTO CharacterData (CharacterUUID, DataType, EntityUUID, Data)
            VALUES (@charUuid, @dataType, @entityUuid, @data)
            ON CONFLICT (CharacterUUID, DataType, EntityUUID) DO UPDATE SET Data = @data", conn, transaction);
        entityCmd.Parameters.AddWithValue("@charUuid", characterUUID);
        entityCmd.Parameters.AddWithValue("@dataType", dataType);
        entityCmd.Parameters.AddWithValue("@entityUuid", entityUUID);
        entityCmd.Parameters.AddWithValue("@data", json);
        await entityCmd.ExecuteNonQueryAsync();

        await RebuildCollectionBlobAsync(conn, transaction, characterUUID, dataType);
        await transaction.CommitAsync();
    }

    public async Task DeleteCharacterEntityAsync(string characterUUID, string dataType, string entityUUID)
    {
        using var conn = await OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync();

        using var cmd = new NpgsqlCommand(
            "DELETE FROM CharacterData WHERE CharacterUUID = @charUuid AND DataType = @dataType AND EntityUUID = @entityUuid",
            conn, transaction);
        cmd.Parameters.AddWithValue("@charUuid", characterUUID);
        cmd.Parameters.AddWithValue("@dataType", dataType);
        cmd.Parameters.AddWithValue("@entityUuid", entityUUID);
        await cmd.ExecuteNonQueryAsync();

        await RebuildCollectionBlobAsync(conn, transaction, characterUUID, dataType);
        await transaction.CommitAsync();
    }

    public async Task<string?> GetAllCharacterDataAsync(string characterUUID)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand(
            "SELECT DataType, Data FROM CharacterCollections WHERE CharacterUUID = @charUuid", conn);
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
        using var cmd = new NpgsqlCommand("SELECT Data FROM GlobalData WHERE DataType = @dataType", conn);
        cmd.Parameters.AddWithValue("@dataType", dataType);

        var result = await cmd.ExecuteScalarAsync();
        return result as string;
    }

    public async Task UpsertGlobalDataAsync(string dataType, string json)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand(@"INSERT INTO GlobalData (DataType, Data) VALUES (@dataType, @data)
            ON CONFLICT (DataType) DO UPDATE SET Data = @data", conn);
        cmd.Parameters.AddWithValue("@dataType", dataType);
        cmd.Parameters.AddWithValue("@data", json);
        await cmd.ExecuteNonQueryAsync();
    }

    // --- Tokens ---

    public async Task<ApiToken?> FindTokenByHashAsync(string tokenHash)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand("SELECT Data FROM Tokens", conn);

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
        using var cmd = new NpgsqlCommand("SELECT Data FROM Tokens", conn);

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
        using var cmd = new NpgsqlCommand(@"INSERT INTO Tokens (Id, Data) VALUES (@id, @data)
            ON CONFLICT (Id) DO UPDATE SET Data = @data", conn);
        cmd.Parameters.AddWithValue("@id", token.Id);
        cmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(token, SerializerSettings));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteTokenAsync(string id)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand("DELETE FROM Tokens WHERE Id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    // --- Membership Actions ---

    public async Task<IReadOnlyList<MembershipAction>> GetFactionActionsAsync(string factionUUID)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand(
            "SELECT Data FROM MembershipActions WHERE FactionUUID = @factionUuid", conn);
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
        using var cmd = new NpgsqlCommand(@"INSERT INTO MembershipActions (Id, FactionUUID, ExpiresUtc, Data)
            VALUES (@id, @factionUuid, @expiresUtc, @data)
            ON CONFLICT (Id) DO UPDATE SET FactionUUID = @factionUuid, ExpiresUtc = @expiresUtc, Data = @data", conn);
        cmd.Parameters.AddWithValue("@id", action.Id);
        cmd.Parameters.AddWithValue("@factionUuid", action.FactionUUID);
        cmd.Parameters.AddWithValue("@expiresUtc", action.ExpiresUtc.ToString("O"));
        cmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(action, SerializerSettings));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteMembershipActionAsync(string id)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand("DELETE FROM MembershipActions WHERE Id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteExpiredActionsAsync(DateTime cutoff)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand("DELETE FROM MembershipActions WHERE ExpiresUtc <= @cutoff", conn);
        cmd.Parameters.AddWithValue("@cutoff", cutoff.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }

    // --- Sharing Rules ---

    public async Task<IReadOnlyList<SharingRule>> GetSharingRulesForCharacterAsync(string characterUUID)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand("SELECT Data FROM SharingRules WHERE CharacterUUID = @charUuid", conn);
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
        using var cmd = new NpgsqlCommand(@"INSERT INTO SharingRules (CharacterUUID, Data) VALUES (@charUuid, @data)
            ON CONFLICT (CharacterUUID) DO UPDATE SET Data = @data", conn);
        cmd.Parameters.AddWithValue("@charUuid", characterUUID);
        cmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(rules, SerializerSettings));
        await cmd.ExecuteNonQueryAsync();
    }

    // --- Character Preferences ---

    public async Task<CharacterPreferences?> GetCharacterPreferencesAsync(string characterUUID)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand("SELECT Data FROM CharacterPreferences WHERE CharacterUUID = @charUuid", conn);
        cmd.Parameters.AddWithValue("@charUuid", characterUUID);

        var result = await cmd.ExecuteScalarAsync();
        return result is string json
            ? JsonConvert.DeserializeObject<CharacterPreferences>(json)
            : null;
    }

    public async Task UpsertCharacterPreferencesAsync(CharacterPreferences prefs)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand(@"INSERT INTO CharacterPreferences (CharacterUUID, Data) VALUES (@charUuid, @data)
            ON CONFLICT (CharacterUUID) DO UPDATE SET Data = @data", conn);
        cmd.Parameters.AddWithValue("@charUuid", prefs.CharacterUUID);
        cmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(prefs, SerializerSettings));
        await cmd.ExecuteNonQueryAsync();
    }

    // --- Private Helpers ---

    private static async Task RebuildCollectionBlobAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction transaction,
        string characterUUID,
        string dataType)
    {
        using var selectCmd = new NpgsqlCommand(
            "SELECT Data FROM CharacterData WHERE CharacterUUID = @charUuid AND DataType = @dataType",
            conn, transaction);
        selectCmd.Parameters.AddWithValue("@charUuid", characterUUID);
        selectCmd.Parameters.AddWithValue("@dataType", dataType);

        var array = new JArray();
        using var reader = await selectCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var entityJson = reader.GetString(0);
            array.Add(JToken.Parse(entityJson));
        }

        using var upsertCmd = new NpgsqlCommand(@"INSERT INTO CharacterCollections (CharacterUUID, DataType, Data)
            VALUES (@charUuid, @dataType, @data)
            ON CONFLICT (CharacterUUID, DataType) DO UPDATE SET Data = @data", conn, transaction);
        upsertCmd.Parameters.AddWithValue("@charUuid", characterUUID);
        upsertCmd.Parameters.AddWithValue("@dataType", dataType);
        upsertCmd.Parameters.AddWithValue("@data", array.ToString(Formatting.Indented));
        await upsertCmd.ExecuteNonQueryAsync();
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }
}
