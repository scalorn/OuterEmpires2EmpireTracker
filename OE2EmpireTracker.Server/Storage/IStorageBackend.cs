namespace OE2EmpireTracker.Server.Storage;

/// <summary>
/// Abstraction for server-side data persistence.
/// All backends implement this interface so the service is storage-agnostic.
/// </summary>
public interface IStorageBackend
{
    // Lifecycle
    Task InitializeAsync(CancellationToken ct = default);
    Task<bool> ValidateConnectionAsync(CancellationToken ct = default);

    // Factions
    Task<ServerFaction?> GetFactionAsync(string uuid);
    Task<IReadOnlyList<ServerFaction>> GetAllFactionsAsync();
    Task UpsertFactionAsync(ServerFaction faction);
    Task DeleteFactionAsync(string uuid);

    // Characters
    Task<ServerCharacter?> GetCharacterAsync(string uuid);
    Task<IReadOnlyList<ServerCharacter>> GetAllCharactersAsync();
    Task UpsertCharacterAsync(ServerCharacter character);
    Task DeleteCharacterAsync(string uuid);

    // Character Data (per data type — stored as raw JSON)
    Task<string?> GetCharacterDataAsync(string characterUUID, string dataType);
    Task<string?> GetCharacterEntityAsync(string characterUUID, string dataType, string entityUUID);
    Task UpsertCharacterDataAsync(string characterUUID, string dataType, string json);
    Task UpsertCharacterEntityAsync(string characterUUID, string dataType, string entityUUID, string json);
    Task DeleteCharacterEntityAsync(string characterUUID, string dataType, string entityUUID);
    Task<string?> GetAllCharacterDataAsync(string characterUUID);
    Task PutAllCharacterDataAsync(string characterUUID, string json);

    // Global/Baseline Data
    Task<string?> GetGlobalDataAsync(string dataType);
    Task UpsertGlobalDataAsync(string dataType, string json);

    // Tokens
    Task<ApiToken?> FindTokenByHashAsync(string tokenHash);
    Task<IReadOnlyList<ApiToken>> GetAllTokensAsync();
    Task UpsertTokenAsync(ApiToken token);
    Task DeleteTokenAsync(string id);

    // Membership Actions
    Task<IReadOnlyList<MembershipAction>> GetFactionActionsAsync(string factionUUID);
    Task UpsertMembershipActionAsync(MembershipAction action);
    Task DeleteMembershipActionAsync(string id);
    Task DeleteExpiredActionsAsync(DateTime cutoff);

    // Sharing Rules
    Task<IReadOnlyList<SharingRule>> GetSharingRulesForCharacterAsync(string characterUUID);
    Task UpsertSharingRulesAsync(string characterUUID, IReadOnlyList<SharingRule> rules);

    // Character Preferences
    Task<CharacterPreferences?> GetCharacterPreferencesAsync(string characterUUID);
    Task UpsertCharacterPreferencesAsync(CharacterPreferences prefs);
}
