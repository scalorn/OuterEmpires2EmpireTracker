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

    // Faction Permission Entities

    // FactionCapability
    Task<IReadOnlyList<FactionCapability>> GetFactionCapabilitiesAsync(string factionUUID);
    Task UpsertFactionCapabilityAsync(FactionCapability capability);
    Task DeleteFactionCapabilityAsync(string factionUUID, string capabilityUUID);

    // FactionClearanceLevel
    Task<IReadOnlyList<FactionClearanceLevel>> GetFactionClearanceLevelsAsync(string factionUUID);
    Task UpsertFactionClearanceLevelAsync(FactionClearanceLevel level);
    Task DeleteFactionClearanceLevelAsync(string factionUUID, string levelUUID);

    // FactionPermissionGroup
    Task<IReadOnlyList<FactionPermissionGroup>> GetFactionGroupsAsync(string factionUUID);
    Task<FactionPermissionGroup?> GetFactionGroupAsync(string factionUUID, string groupUUID);
    Task UpsertFactionGroupAsync(FactionPermissionGroup group);
    Task DeleteFactionGroupAsync(string factionUUID, string groupUUID);

    // FactionGroupCapability
    Task<IReadOnlyList<FactionGroupCapability>> GetFactionGroupCapabilitiesAsync(string groupUUID);
    Task AddFactionGroupCapabilityAsync(FactionGroupCapability item);
    Task RemoveFactionGroupCapabilityAsync(string groupUUID, string capabilityUUID);

    // FactionGroupSharingRule
    Task<IReadOnlyList<FactionGroupSharingRule>> GetFactionGroupSharingRulesAsync(string groupUUID);
    Task UpsertFactionGroupSharingRuleAsync(FactionGroupSharingRule rule);
    Task DeleteFactionGroupSharingRuleAsync(string groupUUID, string ruleUUID);

    // FactionMemberPermissions
    Task<FactionMemberPermissions?> GetFactionMemberPermissionsAsync(string factionUUID, string characterUUID);
    Task UpsertFactionMemberPermissionsAsync(FactionMemberPermissions perms);
    Task<IReadOnlyList<FactionMemberPermissions>> GetAllFactionMembersPermissionsAsync(string factionUUID);

    // FactionMemberCapability
    Task<IReadOnlyList<FactionMemberCapability>> GetFactionMemberCapabilitiesAsync(string factionUUID, string characterUUID);
    Task AddFactionMemberCapabilityAsync(FactionMemberCapability item);
    Task RemoveFactionMemberCapabilityAsync(string factionUUID, string characterUUID, string capabilityUUID);

    // Character Permission Entities

    // CharacterCapability
    Task<IReadOnlyList<CharacterCapability>> GetCharacterCapabilitiesAsync(string characterUUID);
    Task UpsertCharacterCapabilityAsync(CharacterCapability capability);
    Task DeleteCharacterCapabilityAsync(string characterUUID, string capabilityUUID);

    // CharacterClearanceLevel
    Task<IReadOnlyList<CharacterClearanceLevel>> GetCharacterClearanceLevelsAsync(string characterUUID);
    Task UpsertCharacterClearanceLevelAsync(CharacterClearanceLevel level);
    Task DeleteCharacterClearanceLevelAsync(string characterUUID, string levelUUID);

    // CharacterPermissionGroup
    Task<IReadOnlyList<CharacterPermissionGroup>> GetCharacterGroupsAsync(string characterUUID);
    Task<CharacterPermissionGroup?> GetCharacterGroupAsync(string characterUUID, string groupUUID);
    Task UpsertCharacterGroupAsync(CharacterPermissionGroup group);
    Task DeleteCharacterGroupAsync(string characterUUID, string groupUUID);

    // CharacterGroupCapability
    Task<IReadOnlyList<CharacterGroupCapability>> GetCharacterGroupCapabilitiesAsync(string groupUUID);
    Task AddCharacterGroupCapabilityAsync(CharacterGroupCapability item);
    Task RemoveCharacterGroupCapabilityAsync(string groupUUID, string capabilityUUID);

    // CharacterGroupSharingRule
    Task<IReadOnlyList<CharacterGroupSharingRule>> GetCharacterGroupSharingRulesAsync(string groupUUID);
    Task UpsertCharacterGroupSharingRuleAsync(CharacterGroupSharingRule rule);
    Task DeleteCharacterGroupSharingRuleAsync(string groupUUID, string ruleUUID);

    // CharacterGranteePermissions
    Task<IReadOnlyList<CharacterGranteePermissions>> GetCharacterGranteesAsync(string ownerCharacterUUID);
    Task UpsertCharacterGranteePermissionsAsync(CharacterGranteePermissions perms);
    Task DeleteCharacterGranteePermissionsAsync(string ownerCharacterUUID, string granteeUUID);

    // CharacterGranteeCapability
    Task<IReadOnlyList<CharacterGranteeCapability>> GetCharacterGranteeCapabilitiesAsync(string ownerCharacterUUID, string granteeUUID);
    Task AddCharacterGranteeCapabilityAsync(CharacterGranteeCapability item);
    Task RemoveCharacterGranteeCapabilityAsync(string ownerCharacterUUID, string granteeUUID, string capabilityUUID);

    // Intel and Audit

    // IntelComment
    Task<IReadOnlyList<IntelComment>> GetIntelCommentsForTargetAsync(string targetCharacterUUID);
    Task<IntelComment?> GetIntelCommentAsync(string commentUUID);
    Task UpsertIntelCommentAsync(IntelComment comment);
    Task DeleteIntelCommentAsync(string commentUUID);

    // IntelCommentFactionShare
    Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForCommentAsync(string commentUUID);
    Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForFactionAsync(string factionUUID);
    Task UpsertIntelShareAsync(IntelCommentFactionShare share);
    Task DeleteIntelShareAsync(string shareUUID);

    // PermissionAuditEntry
    Task<IReadOnlyList<PermissionAuditEntry>> GetPermissionAuditEntriesAsync(DateTime? startDate = null, DateTime? endDate = null, PermissionActionType? actionType = null, string? actorUUID = null, string? targetUUID = null);
    Task AppendPermissionAuditEntryAsync(PermissionAuditEntry entry);
    Task DeleteExpiredAuditEntriesAsync(DateTime cutoff);
}
