// <copyright file="RecordingStorageBackend.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;
using Bp = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Records upsert/delete calls for verification.
    /// </summary>
    public class UpsertRecord
    {
        public UpsertRecord(Type entityType, string entityUUID)
        {
            EntityType = entityType;
            EntityUUID = entityUUID;
        }

        public Type EntityType { get; }

        public string EntityUUID { get; }
    }

    /// <summary>
    /// Test stub for IStorageBackend that records method calls and can
    /// throw exceptions on demand. Returns configurable data for load operations.
    /// </summary>
    public class RecordingStorageBackend : IStorageBackend
    {
        public bool ThrowOnUpsert { get; set; }

        public bool ThrowOnLoad { get; set; }

        public List<UpsertRecord> UpsertCalls { get; } = new List<UpsertRecord>();

        public List<Colony> ColoniesToReturn { get; set; } = new List<Colony>();

        public List<Bp> BlueprintsToReturn { get; set; } = new List<Bp>();

        // Lifecycle
        public Task InitializeAsync(CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<bool> ValidateConnectionAsync(CancellationToken ct = default)
            => Task.FromResult(true);

        public StorageInfo GetStorageInfo()
            => new StorageInfo { BackendType = "Test", Location = "memory://test" };

        public Task<IReadOnlyList<string>> GetAllCharacterUUIDsAsync()
            => Task.FromResult<IReadOnlyList<string>>(new List<string>());

        // Server Factions
        public Task<ServerFaction> GetFactionAsync(string uuid) => Task.FromResult<ServerFaction>(null);
        public Task<IReadOnlyList<ServerFaction>> GetAllFactionsAsync() => Task.FromResult<IReadOnlyList<ServerFaction>>(new List<ServerFaction>());
        public Task UpsertFactionAsync(ServerFaction faction) => Task.CompletedTask;
        public Task DeleteFactionAsync(string uuid) => Task.CompletedTask;

        // Server Characters
        public Task<ServerCharacter> GetCharacterAsync(string uuid) => Task.FromResult<ServerCharacter>(null);
        public Task<IReadOnlyList<ServerCharacter>> GetAllCharactersAsync() => Task.FromResult<IReadOnlyList<ServerCharacter>>(new List<ServerCharacter>());
        public Task UpsertCharacterAsync(ServerCharacter character) => Task.CompletedTask;
        public Task DeleteCharacterAsync(string uuid) => Task.CompletedTask;

        // Global Data
        public Task<string> GetGlobalDataAsync(string dataType) => Task.FromResult<string>(null);
        public Task UpsertGlobalDataAsync(string dataType, string json) => Task.CompletedTask;

        // Star Systems
        public Task<IReadOnlyList<StarSystem>> GetAllStarSystemsAsync() => Task.FromResult<IReadOnlyList<StarSystem>>(new List<StarSystem>());
        public Task UpsertStarSystemsAsync(IReadOnlyList<StarSystem> systems) => Task.CompletedTask;

        // Colony Summaries
        public Task<IReadOnlyList<ColonySummary>> GetColonySummariesForSystemAsync(int systemId) => Task.FromResult<IReadOnlyList<ColonySummary>>(new List<ColonySummary>());

        // API Tokens
        public Task<ApiToken> FindTokenByHashAsync(string tokenHash) => Task.FromResult<ApiToken>(null);
        public Task<IReadOnlyList<ApiToken>> GetAllTokensAsync() => Task.FromResult<IReadOnlyList<ApiToken>>(new List<ApiToken>());
        public Task UpsertTokenAsync(ApiToken token) => Task.CompletedTask;
        public Task DeleteTokenAsync(string id) => Task.CompletedTask;

        // Membership Actions
        public Task<IReadOnlyList<MembershipAction>> GetFactionActionsAsync(string factionUUID) => Task.FromResult<IReadOnlyList<MembershipAction>>(new List<MembershipAction>());
        public Task UpsertMembershipActionAsync(MembershipAction action) => Task.CompletedTask;
        public Task DeleteMembershipActionAsync(string id) => Task.CompletedTask;
        public Task DeleteExpiredActionsAsync(DateTime cutoff) => Task.CompletedTask;

        // Sharing Rules
        public Task<IReadOnlyList<SharingRule>> GetSharingRulesForCharacterAsync(string characterUUID) => Task.FromResult<IReadOnlyList<SharingRule>>(new List<SharingRule>());
        public Task UpsertSharingRulesAsync(string characterUUID, IReadOnlyList<SharingRule> rules) => Task.CompletedTask;

        // Character Preferences
        public Task<CharacterPreferences> GetCharacterPreferencesAsync(string characterUUID) => Task.FromResult<CharacterPreferences>(null);
        public Task UpsertCharacterPreferencesAsync(CharacterPreferences prefs) => Task.CompletedTask;

        // Per-Character: Colony
        public Task<IReadOnlyList<Colony>> GetAllColoniesAsync(string characterUUID)
        {
            if (ThrowOnLoad) throw new StorageLoadException("Test load failure");
            return Task.FromResult<IReadOnlyList<Colony>>(ColoniesToReturn);
        }

        public Task<Colony> GetColonyAsync(string characterUUID, string entityUUID) => Task.FromResult<Colony>(null);

        public Task UpsertColonyAsync(string characterUUID, Colony entity)
        {
            if (ThrowOnUpsert) throw new StorageWriteException("Test write failure");
            UpsertCalls.Add(new UpsertRecord(typeof(Colony), entity.UUID));
            return Task.CompletedTask;
        }

        public Task DeleteColonyAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: Blueprint
        public Task<IReadOnlyList<Bp>> GetAllBlueprintsAsync(string characterUUID)
        {
            if (ThrowOnLoad) throw new StorageLoadException("Test load failure");
            return Task.FromResult<IReadOnlyList<Bp>>(BlueprintsToReturn);
        }

        public Task<Bp> GetBlueprintAsync(string characterUUID, string entityUUID) => Task.FromResult<Bp>(null);

        public Task UpsertBlueprintAsync(string characterUUID, Bp entity)
        {
            if (ThrowOnUpsert) throw new StorageWriteException("Test write failure");
            UpsertCalls.Add(new UpsertRecord(typeof(Bp), entity.UUID));
            return Task.CompletedTask;
        }

        public Task DeleteBlueprintAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: Survey
        public Task<IReadOnlyList<Survey>> GetAllSurveysAsync(string characterUUID)
        {
            if (ThrowOnLoad) throw new StorageLoadException("Test load failure");
            return Task.FromResult<IReadOnlyList<Survey>>(new List<Survey>());
        }

        public Task<Survey> GetSurveyAsync(string characterUUID, string entityUUID) => Task.FromResult<Survey>(null);
        public Task UpsertSurveyAsync(string characterUUID, Survey entity) => DoUpsert(typeof(Survey), entity.UUID);
        public Task DeleteSurveyAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: PlayerProfile
        public Task<IReadOnlyList<PlayerProfile>> GetAllPlayerProfilesAsync(string characterUUID)
        {
            if (ThrowOnLoad) throw new StorageLoadException("Test load failure");
            return Task.FromResult<IReadOnlyList<PlayerProfile>>(new List<PlayerProfile>());
        }

        public Task<PlayerProfile> GetPlayerProfileAsync(string characterUUID, string entityUUID) => Task.FromResult<PlayerProfile>(null);
        public Task UpsertPlayerProfileAsync(string characterUUID, PlayerProfile entity) => DoUpsert(typeof(PlayerProfile), entity.UUID);
        public Task DeletePlayerProfileAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: DeliveryRoute
        public Task<IReadOnlyList<DeliveryRoute>> GetAllDeliveryRoutesAsync(string characterUUID) => EmptyList<DeliveryRoute>();
        public Task<DeliveryRoute> GetDeliveryRouteAsync(string characterUUID, string entityUUID) => Task.FromResult<DeliveryRoute>(null);
        public Task UpsertDeliveryRouteAsync(string characterUUID, DeliveryRoute entity) => DoUpsert(typeof(DeliveryRoute), entity.UUID);
        public Task DeleteDeliveryRouteAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: DeliveryPlan
        public Task<IReadOnlyList<DeliveryPlan>> GetAllDeliveryPlansAsync(string characterUUID) => EmptyList<DeliveryPlan>();
        public Task<DeliveryPlan> GetDeliveryPlanAsync(string characterUUID, string entityUUID) => Task.FromResult<DeliveryPlan>(null);
        public Task UpsertDeliveryPlanAsync(string characterUUID, DeliveryPlan entity) => DoUpsert(typeof(DeliveryPlan), entity.UUID);
        public Task DeleteDeliveryPlanAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: Ship
        public Task<IReadOnlyList<Ship>> GetAllShipsAsync(string characterUUID) => EmptyList<Ship>();
        public Task<Ship> GetShipAsync(string characterUUID, string entityUUID) => Task.FromResult<Ship>(null);
        public Task UpsertShipAsync(string characterUUID, Ship entity) => DoUpsert(typeof(Ship), entity.UUID);
        public Task DeleteShipAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: ShipTemplate
        public Task<IReadOnlyList<ShipTemplate>> GetAllShipTemplatesAsync(string characterUUID) => EmptyList<ShipTemplate>();
        public Task<ShipTemplate> GetShipTemplateAsync(string characterUUID, string entityUUID) => Task.FromResult<ShipTemplate>(null);
        public Task UpsertShipTemplateAsync(string characterUUID, ShipTemplate entity) => DoUpsert(typeof(ShipTemplate), entity.UUID);
        public Task DeleteShipTemplateAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: Station
        public Task<IReadOnlyList<Station>> GetAllStationsAsync(string characterUUID) => EmptyList<Station>();
        public Task<Station> GetStationAsync(string characterUUID, string entityUUID) => Task.FromResult<Station>(null);
        public Task UpsertStationAsync(string characterUUID, Station entity) => DoUpsert(typeof(Station), entity.UUID);
        public Task DeleteStationAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: MarketListing
        public Task<IReadOnlyList<MarketListing>> GetAllMarketListingsAsync(string characterUUID) => EmptyList<MarketListing>();
        public Task<MarketListing> GetMarketListingAsync(string characterUUID, string entityUUID) => Task.FromResult<MarketListing>(null);
        public Task UpsertMarketListingAsync(string characterUUID, MarketListing entity) => DoUpsert(typeof(MarketListing), entity.UUID);
        public Task DeleteMarketListingAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: MarketTransaction
        public Task<IReadOnlyList<MarketTransaction>> GetAllMarketTransactionsAsync(string characterUUID) => EmptyList<MarketTransaction>();
        public Task<MarketTransaction> GetMarketTransactionAsync(string characterUUID, string entityUUID) => Task.FromResult<MarketTransaction>(null);
        public Task UpsertMarketTransactionAsync(string characterUUID, MarketTransaction entity) => DoUpsert(typeof(MarketTransaction), entity.UUID);
        public Task DeleteMarketTransactionAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: PricingPlan
        public Task<IReadOnlyList<PricingPlan>> GetAllPricingPlansAsync(string characterUUID) => EmptyList<PricingPlan>();
        public Task<PricingPlan> GetPricingPlanAsync(string characterUUID, string entityUUID) => Task.FromResult<PricingPlan>(null);
        public Task UpsertPricingPlanAsync(string characterUUID, PricingPlan entity) => DoUpsert(typeof(PricingPlan), entity.UUID);
        public Task DeletePricingPlanAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: StockPlan
        public Task<IReadOnlyList<StockPlan>> GetAllStockPlansAsync(string characterUUID) => EmptyList<StockPlan>();
        public Task<StockPlan> GetStockPlanAsync(string characterUUID, string entityUUID) => Task.FromResult<StockPlan>(null);
        public Task UpsertStockPlanAsync(string characterUUID, StockPlan entity) => DoUpsert(typeof(StockPlan), entity.UUID);
        public Task DeleteStockPlanAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: StockProfile
        public Task<IReadOnlyList<StockProfile>> GetAllStockProfilesAsync(string characterUUID) => EmptyList<StockProfile>();
        public Task<StockProfile> GetStockProfileAsync(string characterUUID, string entityUUID) => Task.FromResult<StockProfile>(null);
        public Task UpsertStockProfileAsync(string characterUUID, StockProfile entity) => DoUpsert(typeof(StockProfile), entity.UUID);
        public Task DeleteStockProfileAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: BuildPlan
        public Task<IReadOnlyList<BuildPlan>> GetAllBuildPlansAsync(string characterUUID) => EmptyList<BuildPlan>();
        public Task<BuildPlan> GetBuildPlanAsync(string characterUUID, string entityUUID) => Task.FromResult<BuildPlan>(null);
        public Task UpsertBuildPlanAsync(string characterUUID, BuildPlan entity) => DoUpsert(typeof(BuildPlan), entity.UUID);
        public Task DeleteBuildPlanAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: SupplyChain
        public Task<IReadOnlyList<SupplyChain>> GetAllSupplyChainsAsync(string characterUUID) => EmptyList<SupplyChain>();
        public Task<SupplyChain> GetSupplyChainAsync(string characterUUID, string entityUUID) => Task.FromResult<SupplyChain>(null);
        public Task UpsertSupplyChainAsync(string characterUUID, SupplyChain entity) => DoUpsert(typeof(SupplyChain), entity.UUID);
        public Task DeleteSupplyChainAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: Asteroid
        public Task<IReadOnlyList<Asteroid>> GetAllAsteroidsAsync(string characterUUID) => EmptyList<Asteroid>();
        public Task<Asteroid> GetAsteroidAsync(string characterUUID, string entityUUID) => Task.FromResult<Asteroid>(null);
        public Task UpsertAsteroidAsync(string characterUUID, Asteroid entity) => DoUpsert(typeof(Asteroid), entity.UUID);
        public Task DeleteAsteroidAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: Faction (contacts)
        public Task<IReadOnlyList<Faction>> GetAllFactionsForCharacterAsync(string characterUUID) => EmptyList<Faction>();
        public Task<Faction> GetFactionForCharacterAsync(string characterUUID, string entityUUID) => Task.FromResult<Faction>(null);
        public Task UpsertFactionForCharacterAsync(string characterUUID, Faction entity) => DoUpsert(typeof(Faction), entity.UUID);
        public Task DeleteFactionForCharacterAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: ExternalCharacter
        public Task<IReadOnlyList<ExternalCharacter>> GetAllExternalCharactersAsync(string characterUUID) => EmptyList<ExternalCharacter>();
        public Task<ExternalCharacter> GetExternalCharacterAsync(string characterUUID, string entityUUID) => Task.FromResult<ExternalCharacter>(null);
        public Task UpsertExternalCharacterAsync(string characterUUID, ExternalCharacter entity) => DoUpsert(typeof(ExternalCharacter), entity.UUID);
        public Task DeleteExternalCharacterAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: WarehouseOverflowRule
        public Task<IReadOnlyList<WarehouseOverflowRule>> GetAllWarehouseOverflowRulesAsync(string characterUUID) => EmptyList<WarehouseOverflowRule>();
        public Task<WarehouseOverflowRule> GetWarehouseOverflowRuleAsync(string characterUUID, string entityUUID) => Task.FromResult<WarehouseOverflowRule>(null);
        public Task UpsertWarehouseOverflowRuleAsync(string characterUUID, WarehouseOverflowRule entity) => DoUpsert(typeof(WarehouseOverflowRule), entity.UUID);
        public Task DeleteWarehouseOverflowRuleAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: MailMessage
        public Task<IReadOnlyList<MailMessage>> GetAllMailMessagesAsync(string characterUUID) => EmptyList<MailMessage>();
        public Task<MailMessage> GetMailMessageAsync(string characterUUID, string entityUUID) => Task.FromResult<MailMessage>(null);
        public Task UpsertMailMessageAsync(string characterUUID, MailMessage entity) => DoUpsert(typeof(MailMessage), entity.UUID);
        public Task DeleteMailMessageAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Per-Character: BankingTransaction
        public Task<IReadOnlyList<BankingTransaction>> GetAllBankingTransactionsAsync(string characterUUID) => EmptyList<BankingTransaction>();
        public Task<BankingTransaction> GetBankingTransactionAsync(string characterUUID, string entityUUID) => Task.FromResult<BankingTransaction>(null);
        public Task UpsertBankingTransactionAsync(string characterUUID, BankingTransaction entity) => DoUpsert(typeof(BankingTransaction), entity.UUID);
        public Task DeleteBankingTransactionAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Faction Permission Entities
        public Task<IReadOnlyList<FactionCapability>> GetFactionCapabilitiesAsync(string factionUUID) => EmptyList<FactionCapability>();
        public Task UpsertFactionCapabilityAsync(FactionCapability capability) => Task.CompletedTask;
        public Task DeleteFactionCapabilityAsync(string factionUUID, string capabilityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<FactionClearanceLevel>> GetFactionClearanceLevelsAsync(string factionUUID) => EmptyList<FactionClearanceLevel>();
        public Task UpsertFactionClearanceLevelAsync(FactionClearanceLevel level) => Task.CompletedTask;
        public Task DeleteFactionClearanceLevelAsync(string factionUUID, string levelUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<FactionPermissionGroup>> GetFactionGroupsAsync(string factionUUID) => EmptyList<FactionPermissionGroup>();
        public Task<FactionPermissionGroup> GetFactionGroupAsync(string factionUUID, string groupUUID) => Task.FromResult<FactionPermissionGroup>(null);
        public Task UpsertFactionGroupAsync(FactionPermissionGroup group) => Task.CompletedTask;
        public Task DeleteFactionGroupAsync(string factionUUID, string groupUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<FactionGroupCapability>> GetFactionGroupCapabilitiesAsync(string groupUUID) => EmptyList<FactionGroupCapability>();
        public Task AddFactionGroupCapabilityAsync(FactionGroupCapability item) => Task.CompletedTask;
        public Task RemoveFactionGroupCapabilityAsync(string groupUUID, string capabilityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<FactionGroupSharingRule>> GetFactionGroupSharingRulesAsync(string groupUUID) => EmptyList<FactionGroupSharingRule>();
        public Task UpsertFactionGroupSharingRuleAsync(FactionGroupSharingRule rule) => Task.CompletedTask;
        public Task DeleteFactionGroupSharingRuleAsync(string groupUUID, string ruleUUID) => Task.CompletedTask;
        public Task<FactionMemberPermissions> GetFactionMemberPermissionsAsync(string factionUUID, string characterUUID) => Task.FromResult<FactionMemberPermissions>(null);
        public Task UpsertFactionMemberPermissionsAsync(FactionMemberPermissions perms) => Task.CompletedTask;
        public Task<IReadOnlyList<FactionMemberPermissions>> GetAllFactionMembersPermissionsAsync(string factionUUID) => EmptyList<FactionMemberPermissions>();
        public Task<IReadOnlyList<FactionMemberCapability>> GetFactionMemberCapabilitiesAsync(string factionUUID, string characterUUID) => EmptyList<FactionMemberCapability>();
        public Task AddFactionMemberCapabilityAsync(FactionMemberCapability item) => Task.CompletedTask;
        public Task RemoveFactionMemberCapabilityAsync(string factionUUID, string characterUUID, string capabilityUUID) => Task.CompletedTask;

        // Character Permission Entities
        public Task<IReadOnlyList<CharacterCapability>> GetCharacterCapabilitiesAsync(string characterUUID) => EmptyList<CharacterCapability>();
        public Task UpsertCharacterCapabilityAsync(CharacterCapability capability) => Task.CompletedTask;
        public Task DeleteCharacterCapabilityAsync(string characterUUID, string capabilityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<CharacterClearanceLevel>> GetCharacterClearanceLevelsAsync(string characterUUID) => EmptyList<CharacterClearanceLevel>();
        public Task UpsertCharacterClearanceLevelAsync(CharacterClearanceLevel level) => Task.CompletedTask;
        public Task DeleteCharacterClearanceLevelAsync(string characterUUID, string levelUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<CharacterPermissionGroup>> GetCharacterGroupsAsync(string characterUUID) => EmptyList<CharacterPermissionGroup>();
        public Task<CharacterPermissionGroup> GetCharacterGroupAsync(string characterUUID, string groupUUID) => Task.FromResult<CharacterPermissionGroup>(null);
        public Task UpsertCharacterGroupAsync(CharacterPermissionGroup group) => Task.CompletedTask;
        public Task DeleteCharacterGroupAsync(string characterUUID, string groupUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<CharacterGroupCapability>> GetCharacterGroupCapabilitiesAsync(string groupUUID) => EmptyList<CharacterGroupCapability>();
        public Task AddCharacterGroupCapabilityAsync(CharacterGroupCapability item) => Task.CompletedTask;
        public Task RemoveCharacterGroupCapabilityAsync(string groupUUID, string capabilityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<CharacterGroupSharingRule>> GetCharacterGroupSharingRulesAsync(string groupUUID) => EmptyList<CharacterGroupSharingRule>();
        public Task UpsertCharacterGroupSharingRuleAsync(CharacterGroupSharingRule rule) => Task.CompletedTask;
        public Task DeleteCharacterGroupSharingRuleAsync(string groupUUID, string ruleUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<CharacterGranteePermissions>> GetCharacterGranteesAsync(string ownerCharacterUUID) => EmptyList<CharacterGranteePermissions>();
        public Task UpsertCharacterGranteePermissionsAsync(CharacterGranteePermissions perms) => Task.CompletedTask;
        public Task DeleteCharacterGranteePermissionsAsync(string ownerCharacterUUID, string granteeUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<CharacterGranteeCapability>> GetCharacterGranteeCapabilitiesAsync(string ownerCharacterUUID, string granteeUUID) => EmptyList<CharacterGranteeCapability>();
        public Task AddCharacterGranteeCapabilityAsync(CharacterGranteeCapability item) => Task.CompletedTask;
        public Task RemoveCharacterGranteeCapabilityAsync(string ownerCharacterUUID, string granteeUUID, string capabilityUUID) => Task.CompletedTask;

        // Intel
        public Task<IReadOnlyList<IntelComment>> GetIntelCommentsForTargetAsync(string targetCharacterUUID) => EmptyList<IntelComment>();
        public Task<IntelComment> GetIntelCommentAsync(string commentUUID) => Task.FromResult<IntelComment>(null);
        public Task UpsertIntelCommentAsync(IntelComment comment) => Task.CompletedTask;
        public Task DeleteIntelCommentAsync(string commentUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForCommentAsync(string commentUUID) => EmptyList<IntelCommentFactionShare>();
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForFactionAsync(string factionUUID) => EmptyList<IntelCommentFactionShare>();
        public Task UpsertIntelShareAsync(IntelCommentFactionShare share) => Task.CompletedTask;
        public Task DeleteIntelShareAsync(string shareUUID) => Task.CompletedTask;

        // Audit
        public Task<IReadOnlyList<PermissionAuditEntry>> GetPermissionAuditEntriesAsync(DateTime? startDate = null, DateTime? endDate = null, PermissionActionType? actionType = null, string actorUUID = null, string targetUUID = null) => EmptyList<PermissionAuditEntry>();
        public Task AppendPermissionAuditEntryAsync(PermissionAuditEntry entry) => Task.CompletedTask;
        public Task DeleteExpiredAuditEntriesAsync(DateTime cutoff) => Task.CompletedTask;

        // Baseline / Global Lookup Data
        public Task<BaselineGameConstants> GetBaselineGameConstantsAsync() => Task.FromResult<BaselineGameConstants>(null);
        public Task UpsertBaselineGameConstantsAsync(BaselineGameConstants constants) => Task.CompletedTask;
        public Task<IReadOnlyList<BlueprintType>> GetAllBlueprintTypesAsync() => EmptyList<BlueprintType>();
        public Task UpsertBlueprintTypesAsync(IReadOnlyList<BlueprintType> types) => Task.CompletedTask;
        public Task<IReadOnlyList<ShipClass>> GetAllShipClassesAsync() => EmptyList<ShipClass>();
        public Task UpsertShipClassesAsync(IReadOnlyList<ShipClass> classes) => Task.CompletedTask;
        public Task<IReadOnlyList<TechLevel>> GetAllTechLevelsAsync() => EmptyList<TechLevel>();
        public Task UpsertTechLevelsAsync(IReadOnlyList<TechLevel> levels) => Task.CompletedTask;
        public Task<IReadOnlyList<Commodity>> GetAllCommoditiesAsync() => EmptyList<Commodity>();
        public Task UpsertCommoditiesAsync(IReadOnlyList<Commodity> commodities) => Task.CompletedTask;
        public Task<IReadOnlyList<RefiningRecipe>> GetAllRefiningRecipesAsync() => EmptyList<RefiningRecipe>();
        public Task UpsertRefiningRecipesAsync(IReadOnlyList<RefiningRecipe> recipes) => Task.CompletedTask;
        public Task<IReadOnlyList<ResearchTimeEntry>> GetAllResearchTimesAsync() => EmptyList<ResearchTimeEntry>();
        public Task UpsertResearchTimesAsync(IReadOnlyList<ResearchTimeEntry> entries) => Task.CompletedTask;
        public Task<IReadOnlyList<PropertyTypeDefinition>> GetAllPropertyTypeDefinitionsAsync() => EmptyList<PropertyTypeDefinition>();
        public Task UpsertPropertyTypeDefinitionsAsync(IReadOnlyList<PropertyTypeDefinition> definitions) => Task.CompletedTask;

        // Helper methods
        private Task DoUpsert(Type entityType, string uuid)
        {
            if (ThrowOnUpsert)
            {
                throw new StorageWriteException("Test write failure");
            }

            UpsertCalls.Add(new UpsertRecord(entityType, uuid));
            return Task.CompletedTask;
        }

        private static Task<IReadOnlyList<T>> EmptyList<T>()
            => Task.FromResult<IReadOnlyList<T>>(new List<T>());
    }
}
