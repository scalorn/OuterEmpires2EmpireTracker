// -----------------------------------------------------------------------
// <copyright file="StubStorageBackend.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Minimal stub of <see cref="IStorageBackend"/> for unit testing AuthorizationHelper.
/// Only the methods used by AuthorizationHelper are implemented; all others throw.
/// </summary>
internal class StubStorageBackend : IStorageBackend
{
    /// <summary>
    /// Gets the grantee lookup keyed by owner character UUID.
    /// </summary>
    public Dictionary<string, List<CharacterGranteePermissions>> Grantees { get; } = new();

    /// <summary>
    /// Gets the faction member lookup keyed by (factionUUID, characterUUID).
    /// </summary>
    public Dictionary<(string FactionUUID, string CharacterUUID), FactionMemberPermissions> FactionMembers { get; } = new();

    /// <summary>
    /// Gets the sharing rules lookup keyed by owner character UUID.
    /// </summary>
    public Dictionary<string, List<SharingRule>> SharingRules { get; } = new();

    /// <inheritdoc/>
    public Task<IReadOnlyList<CharacterGranteePermissions>> GetCharacterGranteesAsync(string ownerCharacterUUID)
    {
        if (Grantees.TryGetValue(ownerCharacterUUID, out var list))
        {
            return Task.FromResult<IReadOnlyList<CharacterGranteePermissions>>(list);
        }

        return Task.FromResult<IReadOnlyList<CharacterGranteePermissions>>(
            Array.Empty<CharacterGranteePermissions>());
    }

    /// <inheritdoc/>
    public Task<FactionMemberPermissions?> GetFactionMemberPermissionsAsync(
        string factionUUID,
        string characterUUID)
    {
        FactionMembers.TryGetValue((factionUUID, characterUUID), out var perms);
        return Task.FromResult(perms);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SharingRule>> GetSharingRulesForCharacterAsync(string characterUUID)
    {
        if (SharingRules.TryGetValue(characterUUID, out var list))
        {
            return Task.FromResult<IReadOnlyList<SharingRule>>(list);
        }

        return Task.FromResult<IReadOnlyList<SharingRule>>(Array.Empty<SharingRule>());
    }

    // ===== Not implemented — not used by AuthorizationHelper =====

    public Task<IReadOnlyList<StarSystem>> GetAllStarSystemsAsync() => throw new NotImplementedException();

    public Task UpsertStarSystemsAsync(IReadOnlyList<StarSystem> systems) => throw new NotImplementedException();

    public Task<IReadOnlyList<ColonySummary>> GetColonySummariesForSystemAsync(int systemId) => throw new NotImplementedException();

    public Task InitializeAsync(CancellationToken ct = default) => throw new NotImplementedException();

    public Task<bool> ValidateConnectionAsync(CancellationToken ct = default) => throw new NotImplementedException();

    public Task<ServerFaction?> GetFactionAsync(string uuid) => throw new NotImplementedException();

    public Task<IReadOnlyList<ServerFaction>> GetAllFactionsAsync() => throw new NotImplementedException();

    public Task UpsertFactionAsync(ServerFaction faction) => throw new NotImplementedException();

    public Task DeleteFactionAsync(string uuid) => throw new NotImplementedException();

    public Task<ServerCharacter?> GetCharacterAsync(string uuid) => throw new NotImplementedException();

    public Task<IReadOnlyList<ServerCharacter>> GetAllCharactersAsync() => throw new NotImplementedException();

    public Task UpsertCharacterAsync(ServerCharacter character) => throw new NotImplementedException();

    public Task DeleteCharacterAsync(string uuid) => throw new NotImplementedException();

    public Task<string?> GetGlobalDataAsync(string dataType) => throw new NotImplementedException();

    public Task UpsertGlobalDataAsync(string dataType, string json) => throw new NotImplementedException();

    public Task<ApiToken?> FindTokenByHashAsync(string tokenHash) => throw new NotImplementedException();

    public Task<IReadOnlyList<ApiToken>> GetAllTokensAsync() => throw new NotImplementedException();

    public Task UpsertTokenAsync(ApiToken token) => throw new NotImplementedException();

    public Task DeleteTokenAsync(string id) => throw new NotImplementedException();

    public Task<IReadOnlyList<MembershipAction>> GetFactionActionsAsync(string factionUUID) => throw new NotImplementedException();

    public Task UpsertMembershipActionAsync(MembershipAction action) => throw new NotImplementedException();

    public Task DeleteMembershipActionAsync(string id) => throw new NotImplementedException();

    public Task DeleteExpiredActionsAsync(DateTime cutoff) => throw new NotImplementedException();

    public Task UpsertSharingRulesAsync(string characterUUID, IReadOnlyList<SharingRule> rules) => throw new NotImplementedException();

    public Task<CharacterPreferences?> GetCharacterPreferencesAsync(string characterUUID) => throw new NotImplementedException();

    public Task UpsertCharacterPreferencesAsync(CharacterPreferences prefs) => throw new NotImplementedException();

    public Task<IReadOnlyList<FactionCapability>> GetFactionCapabilitiesAsync(string factionUUID) => throw new NotImplementedException();

    public Task UpsertFactionCapabilityAsync(FactionCapability capability) => throw new NotImplementedException();

    public Task DeleteFactionCapabilityAsync(string factionUUID, string capabilityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<FactionClearanceLevel>> GetFactionClearanceLevelsAsync(string factionUUID) => throw new NotImplementedException();

    public Task UpsertFactionClearanceLevelAsync(FactionClearanceLevel level) => throw new NotImplementedException();

    public Task DeleteFactionClearanceLevelAsync(string factionUUID, string levelUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<FactionPermissionGroup>> GetFactionGroupsAsync(string factionUUID) => throw new NotImplementedException();

    public Task<FactionPermissionGroup?> GetFactionGroupAsync(string factionUUID, string groupUUID) => throw new NotImplementedException();

    public Task UpsertFactionGroupAsync(FactionPermissionGroup group) => throw new NotImplementedException();

    public Task DeleteFactionGroupAsync(string factionUUID, string groupUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<FactionGroupCapability>> GetFactionGroupCapabilitiesAsync(string groupUUID) => throw new NotImplementedException();

    public Task AddFactionGroupCapabilityAsync(FactionGroupCapability item) => throw new NotImplementedException();

    public Task RemoveFactionGroupCapabilityAsync(string groupUUID, string capabilityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<FactionGroupSharingRule>> GetFactionGroupSharingRulesAsync(string groupUUID) => throw new NotImplementedException();

    public Task UpsertFactionGroupSharingRuleAsync(FactionGroupSharingRule rule) => throw new NotImplementedException();

    public Task DeleteFactionGroupSharingRuleAsync(string groupUUID, string ruleUUID) => throw new NotImplementedException();

    public Task UpsertFactionMemberPermissionsAsync(FactionMemberPermissions perms) => throw new NotImplementedException();

    public Task<IReadOnlyList<FactionMemberPermissions>> GetAllFactionMembersPermissionsAsync(string factionUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<FactionMemberCapability>> GetFactionMemberCapabilitiesAsync(string factionUUID, string characterUUID) => throw new NotImplementedException();

    public Task AddFactionMemberCapabilityAsync(FactionMemberCapability item) => throw new NotImplementedException();

    public Task RemoveFactionMemberCapabilityAsync(string factionUUID, string characterUUID, string capabilityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<CharacterCapability>> GetCharacterCapabilitiesAsync(string characterUUID) => throw new NotImplementedException();

    public Task UpsertCharacterCapabilityAsync(CharacterCapability capability) => throw new NotImplementedException();

    public Task DeleteCharacterCapabilityAsync(string characterUUID, string capabilityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<CharacterClearanceLevel>> GetCharacterClearanceLevelsAsync(string characterUUID) => throw new NotImplementedException();

    public Task UpsertCharacterClearanceLevelAsync(CharacterClearanceLevel level) => throw new NotImplementedException();

    public Task DeleteCharacterClearanceLevelAsync(string characterUUID, string levelUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<CharacterPermissionGroup>> GetCharacterGroupsAsync(string characterUUID) => throw new NotImplementedException();

    public Task<CharacterPermissionGroup?> GetCharacterGroupAsync(string characterUUID, string groupUUID) => throw new NotImplementedException();

    public Task UpsertCharacterGroupAsync(CharacterPermissionGroup group) => throw new NotImplementedException();

    public Task DeleteCharacterGroupAsync(string characterUUID, string groupUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<CharacterGroupCapability>> GetCharacterGroupCapabilitiesAsync(string groupUUID) => throw new NotImplementedException();

    public Task AddCharacterGroupCapabilityAsync(CharacterGroupCapability item) => throw new NotImplementedException();

    public Task RemoveCharacterGroupCapabilityAsync(string groupUUID, string capabilityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<CharacterGroupSharingRule>> GetCharacterGroupSharingRulesAsync(string groupUUID) => throw new NotImplementedException();

    public Task UpsertCharacterGroupSharingRuleAsync(CharacterGroupSharingRule rule) => throw new NotImplementedException();

    public Task DeleteCharacterGroupSharingRuleAsync(string groupUUID, string ruleUUID) => throw new NotImplementedException();

    public Task UpsertCharacterGranteePermissionsAsync(CharacterGranteePermissions perms) => throw new NotImplementedException();

    public Task DeleteCharacterGranteePermissionsAsync(string ownerCharacterUUID, string granteeUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<CharacterGranteeCapability>> GetCharacterGranteeCapabilitiesAsync(string ownerCharacterUUID, string granteeUUID) => throw new NotImplementedException();

    public Task AddCharacterGranteeCapabilityAsync(CharacterGranteeCapability item) => throw new NotImplementedException();

    public Task RemoveCharacterGranteeCapabilityAsync(string ownerCharacterUUID, string granteeUUID, string capabilityUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<IntelComment>> GetIntelCommentsForTargetAsync(string targetCharacterUUID) => throw new NotImplementedException();

    public Task<IntelComment?> GetIntelCommentAsync(string commentUUID) => throw new NotImplementedException();

    public Task UpsertIntelCommentAsync(IntelComment comment) => throw new NotImplementedException();

    public Task DeleteIntelCommentAsync(string commentUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForCommentAsync(string commentUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForFactionAsync(string factionUUID) => throw new NotImplementedException();

    public Task UpsertIntelShareAsync(IntelCommentFactionShare share) => throw new NotImplementedException();

    public Task DeleteIntelShareAsync(string shareUUID) => throw new NotImplementedException();

    public Task<IReadOnlyList<PermissionAuditEntry>> GetPermissionAuditEntriesAsync(DateTime? startDate = null, DateTime? endDate = null, PermissionActionType? actionType = null, string? actorUUID = null, string? targetUUID = null) => throw new NotImplementedException();

    public Task AppendPermissionAuditEntryAsync(PermissionAuditEntry entry) => throw new NotImplementedException();

    public Task DeleteExpiredAuditEntriesAsync(DateTime cutoff) => throw new NotImplementedException();

    // Typed entity methods

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
}
