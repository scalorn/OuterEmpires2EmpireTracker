// <copyright file="InMemoryMigrationBackend.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;
using Bp = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// In-memory IStorageBackend that stores written entities and returns them on read.
    /// Used by MigrationServiceTests to verify data round-trip through migration.
    /// </summary>
    internal class InMemoryMigrationBackend : IStorageBackend
    {
        private readonly List<string> characterUUIDs;
        private readonly Dictionary<string, List<Colony>> colonies = new Dictionary<string, List<Colony>>();
        private readonly Dictionary<string, List<Bp>> blueprints = new Dictionary<string, List<Bp>>();
        private readonly Dictionary<string, List<Ship>> ships = new Dictionary<string, List<Ship>>();
        private readonly Dictionary<string, List<BankingTransaction>> banking = new Dictionary<string, List<BankingTransaction>>();
        private readonly Dictionary<string, List<MailMessage>> mail = new Dictionary<string, List<MailMessage>>();
        private readonly Dictionary<string, List<Survey>> surveys = new Dictionary<string, List<Survey>>();
        private readonly List<ServerFaction> factions = new List<ServerFaction>();
        private readonly List<StarSystem> starSystems = new List<StarSystem>();
        private readonly List<ApiToken> tokens = new List<ApiToken>();

        public InMemoryMigrationBackend(List<string> characterUUIDs = null)
        {
            this.characterUUIDs = characterUUIDs ?? new List<string>();
        }

        /// <summary>Gets the list of entity type names that received upsert calls.</summary>
        public List<string> UpsertedEntityTypes { get; } = new List<string>();

        // ── Lifecycle ──
        public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<bool> ValidateConnectionAsync(CancellationToken ct = default)
            => Task.FromResult(true);

        public StorageInfo GetStorageInfo()
            => new StorageInfo { BackendType = "InMemory", Location = "memory://migration-test" };

        public Task<IReadOnlyList<string>> GetAllCharacterUUIDsAsync()
            => Task.FromResult<IReadOnlyList<string>>(characterUUIDs);

        // ── Server-global entities ──
        public Task<ServerFaction> GetFactionAsync(string uuid) => Task.FromResult<ServerFaction>(null);

        public Task<IReadOnlyList<ServerFaction>> GetAllFactionsAsync()
            => Task.FromResult<IReadOnlyList<ServerFaction>>(factions);

        public Task UpsertFactionAsync(ServerFaction faction)
        {
            factions.RemoveAll(f => f.UUID == faction.UUID);
            factions.Add(faction);
            UpsertedEntityTypes.Add("ServerFaction");
            return Task.CompletedTask;
        }

        public Task DeleteFactionAsync(string uuid) => Task.CompletedTask;

        public Task<ServerCharacter> GetCharacterAsync(string uuid) => Task.FromResult<ServerCharacter>(null);

        public Task<IReadOnlyList<ServerCharacter>> GetAllCharactersAsync()
            => Task.FromResult<IReadOnlyList<ServerCharacter>>(new List<ServerCharacter>());

        public Task UpsertCharacterAsync(ServerCharacter character)
        {
            UpsertedEntityTypes.Add("ServerCharacter");
            return Task.CompletedTask;
        }

        public Task DeleteCharacterAsync(string uuid) => Task.CompletedTask;

        public Task<string> GetGlobalDataAsync(string dataType) => Task.FromResult<string>(null);

        public Task UpsertGlobalDataAsync(string dataType, string json) => Task.CompletedTask;

        public Task<IReadOnlyList<StarSystem>> GetAllStarSystemsAsync()
            => Task.FromResult<IReadOnlyList<StarSystem>>(starSystems);

        public Task UpsertStarSystemsAsync(IReadOnlyList<StarSystem> systems)
        {
            starSystems.Clear();
            starSystems.AddRange(systems);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ColonySummary>> GetColonySummariesForSystemAsync(int systemId)
            => Task.FromResult<IReadOnlyList<ColonySummary>>(new List<ColonySummary>());

        public Task<ApiToken> FindTokenByHashAsync(string tokenHash) => Task.FromResult<ApiToken>(null);

        public Task<IReadOnlyList<ApiToken>> GetAllTokensAsync()
            => Task.FromResult<IReadOnlyList<ApiToken>>(tokens);

        public Task UpsertTokenAsync(ApiToken token)
        {
            tokens.RemoveAll(t => t.Id == token.Id);
            tokens.Add(token);
            return Task.CompletedTask;
        }

        public Task DeleteTokenAsync(string id) => Task.CompletedTask;

        public Task<IReadOnlyList<MembershipAction>> GetFactionActionsAsync(string factionUUID)
            => Task.FromResult<IReadOnlyList<MembershipAction>>(new List<MembershipAction>());

        public Task UpsertMembershipActionAsync(MembershipAction action) => Task.CompletedTask;

        public Task DeleteMembershipActionAsync(string id) => Task.CompletedTask;

        public Task DeleteExpiredActionsAsync(DateTime cutoff) => Task.CompletedTask;

        // ── Sharing / Prefs ──
        public Task<IReadOnlyList<SharingRule>> GetSharingRulesForCharacterAsync(string characterUUID)
            => Task.FromResult<IReadOnlyList<SharingRule>>(new List<SharingRule>());

        public Task UpsertSharingRulesAsync(string characterUUID, IReadOnlyList<SharingRule> rules)
            => Task.CompletedTask;

        public Task<CharacterPreferences> GetCharacterPreferencesAsync(string characterUUID)
            => Task.FromResult<CharacterPreferences>(null);

        public Task UpsertCharacterPreferencesAsync(CharacterPreferences prefs) => Task.CompletedTask;

        // ── Per-Character: Colony ──
        public Task<IReadOnlyList<Colony>> GetAllColoniesAsync(string characterUUID)
        {
            if (!colonies.TryGetValue(characterUUID, out var list))
            {
                list = new List<Colony>();
            }

            return Task.FromResult<IReadOnlyList<Colony>>(list);
        }

        public Task<Colony> GetColonyAsync(string characterUUID, string entityUUID)
            => Task.FromResult<Colony>(null);

        public Task UpsertColonyAsync(string characterUUID, Colony entity)
        {
            if (!colonies.ContainsKey(characterUUID))
            {
                colonies[characterUUID] = new List<Colony>();
            }

            colonies[characterUUID].RemoveAll(c => c.UUID == entity.UUID);
            colonies[characterUUID].Add(entity);
            UpsertedEntityTypes.Add("Colony");
            return Task.CompletedTask;
        }

        public Task DeleteColonyAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // ── Per-Character: Blueprint ──
        public Task<IReadOnlyList<Bp>> GetAllBlueprintsAsync(string characterUUID)
        {
            if (!blueprints.TryGetValue(characterUUID, out var list))
            {
                list = new List<Bp>();
            }

            return Task.FromResult<IReadOnlyList<Bp>>(list);
        }

        public Task<Bp> GetBlueprintAsync(string characterUUID, string entityUUID)
            => Task.FromResult<Bp>(null);

        public Task UpsertBlueprintAsync(string characterUUID, Bp entity)
        {
            if (!blueprints.ContainsKey(characterUUID))
            {
                blueprints[characterUUID] = new List<Bp>();
            }

            blueprints[characterUUID].RemoveAll(b => b.UUID == entity.UUID);
            blueprints[characterUUID].Add(entity);
            UpsertedEntityTypes.Add("Blueprint");
            return Task.CompletedTask;
        }

        public Task DeleteBlueprintAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // ── Per-Character: Ship ──
        public Task<IReadOnlyList<Ship>> GetAllShipsAsync(string characterUUID)
        {
            if (!ships.TryGetValue(characterUUID, out var list))
            {
                list = new List<Ship>();
            }

            return Task.FromResult<IReadOnlyList<Ship>>(list);
        }

        public Task<Ship> GetShipAsync(string characterUUID, string entityUUID)
            => Task.FromResult<Ship>(null);

        public Task UpsertShipAsync(string characterUUID, Ship entity)
        {
            if (!ships.ContainsKey(characterUUID))
            {
                ships[characterUUID] = new List<Ship>();
            }

            ships[characterUUID].RemoveAll(s => s.UUID == entity.UUID);
            ships[characterUUID].Add(entity);
            UpsertedEntityTypes.Add("Ship");
            return Task.CompletedTask;
        }

        public Task DeleteShipAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // ── Per-Character: BankingTransaction ──
        public Task<IReadOnlyList<BankingTransaction>> GetAllBankingTransactionsAsync(string characterUUID)
        {
            if (!banking.TryGetValue(characterUUID, out var list))
            {
                list = new List<BankingTransaction>();
            }

            return Task.FromResult<IReadOnlyList<BankingTransaction>>(list);
        }

        public Task<BankingTransaction> GetBankingTransactionAsync(string characterUUID, string entityUUID)
            => Task.FromResult<BankingTransaction>(null);

        public Task UpsertBankingTransactionAsync(string characterUUID, BankingTransaction entity)
        {
            if (!banking.ContainsKey(characterUUID))
            {
                banking[characterUUID] = new List<BankingTransaction>();
            }

            banking[characterUUID].RemoveAll(b => b.UUID == entity.UUID);
            banking[characterUUID].Add(entity);
            UpsertedEntityTypes.Add("BankingTransaction");
            return Task.CompletedTask;
        }

        public Task DeleteBankingTransactionAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // ── Per-Character: MailMessage ──
        public Task<IReadOnlyList<MailMessage>> GetAllMailMessagesAsync(string characterUUID)
        {
            if (!mail.TryGetValue(characterUUID, out var list))
            {
                list = new List<MailMessage>();
            }

            return Task.FromResult<IReadOnlyList<MailMessage>>(list);
        }

        public Task<MailMessage> GetMailMessageAsync(string characterUUID, string entityUUID)
            => Task.FromResult<MailMessage>(null);

        public Task UpsertMailMessageAsync(string characterUUID, MailMessage entity)
        {
            if (!mail.ContainsKey(characterUUID))
            {
                mail[characterUUID] = new List<MailMessage>();
            }

            mail[characterUUID].RemoveAll(m => m.UUID == entity.UUID);
            mail[characterUUID].Add(entity);
            UpsertedEntityTypes.Add("MailMessage");
            return Task.CompletedTask;
        }

        public Task DeleteMailMessageAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // ── Per-Character: Survey ──
        public Task<IReadOnlyList<Survey>> GetAllSurveysAsync(string characterUUID)
        {
            if (!surveys.TryGetValue(characterUUID, out var list))
            {
                list = new List<Survey>();
            }

            return Task.FromResult<IReadOnlyList<Survey>>(list);
        }

        public Task<Survey> GetSurveyAsync(string characterUUID, string entityUUID)
            => Task.FromResult<Survey>(null);

        public Task UpsertSurveyAsync(string characterUUID, Survey entity)
        {
            if (!surveys.ContainsKey(characterUUID))
            {
                surveys[characterUUID] = new List<Survey>();
            }

            surveys[characterUUID].RemoveAll(s => s.UUID == entity.UUID);
            surveys[characterUUID].Add(entity);
            UpsertedEntityTypes.Add("Survey");
            return Task.CompletedTask;
        }

        public Task DeleteSurveyAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // ── Per-Character: PlayerProfile ──
        public Task<IReadOnlyList<PlayerProfile>> GetAllPlayerProfilesAsync(string characterUUID)
            => Empty<PlayerProfile>();

        public Task<PlayerProfile> GetPlayerProfileAsync(string characterUUID, string entityUUID)
            => Task.FromResult<PlayerProfile>(null);

        public Task UpsertPlayerProfileAsync(string characterUUID, PlayerProfile entity)
        {
            UpsertedEntityTypes.Add("PlayerProfile");
            return Task.CompletedTask;
        }

        public Task DeletePlayerProfileAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // ── Per-Character: remaining entity types (no-op store) ──
        public Task<IReadOnlyList<DeliveryRoute>> GetAllDeliveryRoutesAsync(string characterUUID) => Empty<DeliveryRoute>();
        public Task<DeliveryRoute> GetDeliveryRouteAsync(string characterUUID, string entityUUID) => Task.FromResult<DeliveryRoute>(null);
        public Task UpsertDeliveryRouteAsync(string characterUUID, DeliveryRoute entity) => Track("DeliveryRoute");
        public Task DeleteDeliveryRouteAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        public Task<IReadOnlyList<DeliveryPlan>> GetAllDeliveryPlansAsync(string characterUUID) => Empty<DeliveryPlan>();
        public Task<DeliveryPlan> GetDeliveryPlanAsync(string characterUUID, string entityUUID) => Task.FromResult<DeliveryPlan>(null);
        public Task UpsertDeliveryPlanAsync(string characterUUID, DeliveryPlan entity) => Track("DeliveryPlan");
        public Task DeleteDeliveryPlanAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        public Task<IReadOnlyList<ShipTemplate>> GetAllShipTemplatesAsync(string characterUUID) => Empty<ShipTemplate>();
        public Task<ShipTemplate> GetShipTemplateAsync(string characterUUID, string entityUUID) => Task.FromResult<ShipTemplate>(null);
        public Task UpsertShipTemplateAsync(string characterUUID, ShipTemplate entity) => Track("ShipTemplate");
        public Task DeleteShipTemplateAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        public Task<IReadOnlyList<Station>> GetAllStationsAsync(string characterUUID) => Empty<Station>();
        public Task<Station> GetStationAsync(string characterUUID, string entityUUID) => Task.FromResult<Station>(null);
        public Task UpsertStationAsync(string characterUUID, Station entity) => Track("Station");
        public Task DeleteStationAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        public Task<IReadOnlyList<MarketListing>> GetAllMarketListingsAsync(string characterUUID) => Empty<MarketListing>();
        public Task<MarketListing> GetMarketListingAsync(string characterUUID, string entityUUID) => Task.FromResult<MarketListing>(null);
        public Task UpsertMarketListingAsync(string characterUUID, MarketListing entity) => Track("MarketListing");
        public Task DeleteMarketListingAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        public Task<IReadOnlyList<MarketTransaction>> GetAllMarketTransactionsAsync(string characterUUID) => Empty<MarketTransaction>();
        public Task<MarketTransaction> GetMarketTransactionAsync(string characterUUID, string entityUUID) => Task.FromResult<MarketTransaction>(null);
        public Task UpsertMarketTransactionAsync(string characterUUID, MarketTransaction entity) => Track("MarketTransaction");
        public Task DeleteMarketTransactionAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        public Task<IReadOnlyList<PricingPlan>> GetAllPricingPlansAsync(string characterUUID) => Empty<PricingPlan>();
        public Task<PricingPlan> GetPricingPlanAsync(string characterUUID, string entityUUID) => Task.FromResult<PricingPlan>(null);
        public Task UpsertPricingPlanAsync(string characterUUID, PricingPlan entity) => Track("PricingPlan");
        public Task DeletePricingPlanAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        public Task<IReadOnlyList<StockPlan>> GetAllStockPlansAsync(string characterUUID) => Empty<StockPlan>();
        public Task<StockPlan> GetStockPlanAsync(string characterUUID, string entityUUID) => Task.FromResult<StockPlan>(null);
        public Task UpsertStockPlanAsync(string characterUUID, StockPlan entity) => Track("StockPlan");
        public Task DeleteStockPlanAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        public Task<IReadOnlyList<StockProfile>> GetAllStockProfilesAsync(string characterUUID) => Empty<StockProfile>();
        public Task<StockProfile> GetStockProfileAsync(string characterUUID, string entityUUID) => Task.FromResult<StockProfile>(null);
        public Task UpsertStockProfileAsync(string characterUUID, StockProfile entity) => Track("StockProfile");
        public Task DeleteStockProfileAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        public Task<IReadOnlyList<BuildPlan>> GetAllBuildPlansAsync(string characterUUID) => Empty<BuildPlan>();
        public Task<BuildPlan> GetBuildPlanAsync(string characterUUID, string entityUUID) => Task.FromResult<BuildPlan>(null);
        public Task UpsertBuildPlanAsync(string characterUUID, BuildPlan entity) => Track("BuildPlan");
        public Task DeleteBuildPlanAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        public Task<IReadOnlyList<SupplyChain>> GetAllSupplyChainsAsync(string characterUUID) => Empty<SupplyChain>();
        public Task<SupplyChain> GetSupplyChainAsync(string characterUUID, string entityUUID) => Task.FromResult<SupplyChain>(null);
        public Task UpsertSupplyChainAsync(string characterUUID, SupplyChain entity) => Track("SupplyChain");
        public Task DeleteSupplyChainAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        public Task<IReadOnlyList<Asteroid>> GetAllAsteroidsAsync(string characterUUID) => Empty<Asteroid>();
        public Task<Asteroid> GetAsteroidAsync(string characterUUID, string entityUUID) => Task.FromResult<Asteroid>(null);
        public Task UpsertAsteroidAsync(string characterUUID, Asteroid entity) => Track("Asteroid");
        public Task DeleteAsteroidAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        public Task<IReadOnlyList<WarehouseOverflowRule>> GetAllWarehouseOverflowRulesAsync(string characterUUID) => Empty<WarehouseOverflowRule>();
        public Task<WarehouseOverflowRule> GetWarehouseOverflowRuleAsync(string characterUUID, string entityUUID) => Task.FromResult<WarehouseOverflowRule>(null);
        public Task UpsertWarehouseOverflowRuleAsync(string characterUUID, WarehouseOverflowRule entity) => Track("WarehouseOverflowRule");
        public Task DeleteWarehouseOverflowRuleAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // ── Faction Permission Entities ──
        public Task<IReadOnlyList<FactionCapability>> GetFactionCapabilitiesAsync(string factionUUID) => Empty<FactionCapability>();
        public Task UpsertFactionCapabilityAsync(FactionCapability capability) => Task.CompletedTask;
        public Task DeleteFactionCapabilityAsync(string factionUUID, string capabilityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<FactionClearanceLevel>> GetFactionClearanceLevelsAsync(string factionUUID) => Empty<FactionClearanceLevel>();
        public Task UpsertFactionClearanceLevelAsync(FactionClearanceLevel level) => Task.CompletedTask;
        public Task DeleteFactionClearanceLevelAsync(string factionUUID, string levelUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<FactionPermissionGroup>> GetFactionGroupsAsync(string factionUUID) => Empty<FactionPermissionGroup>();
        public Task<FactionPermissionGroup> GetFactionGroupAsync(string factionUUID, string groupUUID) => Task.FromResult<FactionPermissionGroup>(null);
        public Task UpsertFactionGroupAsync(FactionPermissionGroup group) => Task.CompletedTask;
        public Task DeleteFactionGroupAsync(string factionUUID, string groupUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<FactionGroupCapability>> GetFactionGroupCapabilitiesAsync(string groupUUID) => Empty<FactionGroupCapability>();
        public Task AddFactionGroupCapabilityAsync(FactionGroupCapability item) => Task.CompletedTask;
        public Task RemoveFactionGroupCapabilityAsync(string groupUUID, string capabilityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<FactionGroupSharingRule>> GetFactionGroupSharingRulesAsync(string groupUUID) => Empty<FactionGroupSharingRule>();
        public Task UpsertFactionGroupSharingRuleAsync(FactionGroupSharingRule rule) => Task.CompletedTask;
        public Task DeleteFactionGroupSharingRuleAsync(string groupUUID, string ruleUUID) => Task.CompletedTask;
        public Task<FactionMemberPermissions> GetFactionMemberPermissionsAsync(string factionUUID, string characterUUID) => Task.FromResult<FactionMemberPermissions>(null);
        public Task UpsertFactionMemberPermissionsAsync(FactionMemberPermissions perms) => Task.CompletedTask;
        public Task<IReadOnlyList<FactionMemberPermissions>> GetAllFactionMembersPermissionsAsync(string factionUUID) => Empty<FactionMemberPermissions>();
        public Task<IReadOnlyList<FactionMemberCapability>> GetFactionMemberCapabilitiesAsync(string factionUUID, string characterUUID) => Empty<FactionMemberCapability>();
        public Task AddFactionMemberCapabilityAsync(FactionMemberCapability item) => Task.CompletedTask;
        public Task RemoveFactionMemberCapabilityAsync(string factionUUID, string characterUUID, string capabilityUUID) => Task.CompletedTask;

        // ── Character Permission Entities ──
        public Task<IReadOnlyList<CharacterCapability>> GetCharacterCapabilitiesAsync(string characterUUID) => Empty<CharacterCapability>();
        public Task UpsertCharacterCapabilityAsync(CharacterCapability capability) => Task.CompletedTask;
        public Task DeleteCharacterCapabilityAsync(string characterUUID, string capabilityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<CharacterClearanceLevel>> GetCharacterClearanceLevelsAsync(string characterUUID) => Empty<CharacterClearanceLevel>();
        public Task UpsertCharacterClearanceLevelAsync(CharacterClearanceLevel level) => Task.CompletedTask;
        public Task DeleteCharacterClearanceLevelAsync(string characterUUID, string levelUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<CharacterPermissionGroup>> GetCharacterGroupsAsync(string characterUUID) => Empty<CharacterPermissionGroup>();
        public Task<CharacterPermissionGroup> GetCharacterGroupAsync(string characterUUID, string groupUUID) => Task.FromResult<CharacterPermissionGroup>(null);
        public Task UpsertCharacterGroupAsync(CharacterPermissionGroup group) => Task.CompletedTask;
        public Task DeleteCharacterGroupAsync(string characterUUID, string groupUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<CharacterGroupCapability>> GetCharacterGroupCapabilitiesAsync(string groupUUID) => Empty<CharacterGroupCapability>();
        public Task AddCharacterGroupCapabilityAsync(CharacterGroupCapability item) => Task.CompletedTask;
        public Task RemoveCharacterGroupCapabilityAsync(string groupUUID, string capabilityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<CharacterGroupSharingRule>> GetCharacterGroupSharingRulesAsync(string groupUUID) => Empty<CharacterGroupSharingRule>();
        public Task UpsertCharacterGroupSharingRuleAsync(CharacterGroupSharingRule rule) => Task.CompletedTask;
        public Task DeleteCharacterGroupSharingRuleAsync(string groupUUID, string ruleUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<CharacterGranteePermissions>> GetCharacterGranteesAsync(string ownerCharacterUUID) => Empty<CharacterGranteePermissions>();
        public Task UpsertCharacterGranteePermissionsAsync(CharacterGranteePermissions perms) => Task.CompletedTask;
        public Task DeleteCharacterGranteePermissionsAsync(string ownerCharacterUUID, string granteeUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<CharacterGranteeCapability>> GetCharacterGranteeCapabilitiesAsync(string ownerCharacterUUID, string granteeUUID) => Empty<CharacterGranteeCapability>();
        public Task AddCharacterGranteeCapabilityAsync(CharacterGranteeCapability item) => Task.CompletedTask;
        public Task RemoveCharacterGranteeCapabilityAsync(string ownerCharacterUUID, string granteeUUID, string capabilityUUID) => Task.CompletedTask;

        // ── Intel ──
        public Task<IReadOnlyList<IntelComment>> GetIntelCommentsForTargetAsync(string targetCharacterUUID) => Empty<IntelComment>();
        public Task<IntelComment> GetIntelCommentAsync(string commentUUID) => Task.FromResult<IntelComment>(null);
        public Task UpsertIntelCommentAsync(IntelComment comment) => Task.CompletedTask;
        public Task DeleteIntelCommentAsync(string commentUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForCommentAsync(string commentUUID) => Empty<IntelCommentFactionShare>();
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForFactionAsync(string factionUUID) => Empty<IntelCommentFactionShare>();
        public Task UpsertIntelShareAsync(IntelCommentFactionShare share) => Task.CompletedTask;
        public Task DeleteIntelShareAsync(string shareUUID) => Task.CompletedTask;

        // ── Audit ──
        public Task<IReadOnlyList<PermissionAuditEntry>> GetPermissionAuditEntriesAsync(DateTime? startDate = null, DateTime? endDate = null, PermissionActionType? actionType = null, string actorUUID = null, string targetUUID = null)
            => Empty<PermissionAuditEntry>();

        public Task AppendPermissionAuditEntryAsync(PermissionAuditEntry entry) => Task.CompletedTask;

        public Task DeleteExpiredAuditEntriesAsync(DateTime cutoff) => Task.CompletedTask;

        // ── Baseline / Global Lookup Data ──
        public Task<BaselineGameConstants> GetBaselineGameConstantsAsync() => Task.FromResult<BaselineGameConstants>(null);
        public Task UpsertBaselineGameConstantsAsync(BaselineGameConstants constants) => Task.CompletedTask;
        public Task<IReadOnlyList<BlueprintType>> GetAllBlueprintTypesAsync() => Empty<BlueprintType>();
        public Task UpsertBlueprintTypesAsync(IReadOnlyList<BlueprintType> types) => Track("BlueprintTypes");
        public Task<IReadOnlyList<ShipClass>> GetAllShipClassesAsync() => Empty<ShipClass>();
        public Task UpsertShipClassesAsync(IReadOnlyList<ShipClass> classes) => Track("ShipClasses");
        public Task<IReadOnlyList<TechLevel>> GetAllTechLevelsAsync() => Empty<TechLevel>();
        public Task UpsertTechLevelsAsync(IReadOnlyList<TechLevel> levels) => Track("TechLevels");
        public Task<IReadOnlyList<Commodity>> GetAllCommoditiesAsync() => Empty<Commodity>();
        public Task UpsertCommoditiesAsync(IReadOnlyList<Commodity> commodities) => Track("Commodities");
        public Task<IReadOnlyList<RefiningRecipe>> GetAllRefiningRecipesAsync() => Empty<RefiningRecipe>();
        public Task UpsertRefiningRecipesAsync(IReadOnlyList<RefiningRecipe> recipes) => Track("RefiningRecipes");
        public Task<IReadOnlyList<ResearchTimeEntry>> GetAllResearchTimesAsync() => Empty<ResearchTimeEntry>();
        public Task UpsertResearchTimesAsync(IReadOnlyList<ResearchTimeEntry> entries) => Track("ResearchTimes");
        public Task<IReadOnlyList<PropertyTypeDefinition>> GetAllPropertyTypeDefinitionsAsync() => Empty<PropertyTypeDefinition>();
        public Task UpsertPropertyTypeDefinitionsAsync(IReadOnlyList<PropertyTypeDefinition> definitions) => Track("PropertyTypeDefinitions");
        public Task<IReadOnlyList<Bp>> GetAllGlobalBlueprintsAsync() => Empty<Bp>();
        public Task UpsertGlobalBlueprintsAsync(IReadOnlyList<Bp> blueprints) => Track("GlobalBlueprints");

        // ── Contacts (per-character) — not used by migration validation ──
        public Task<IReadOnlyList<Faction>> GetAllFactionsForCharacterAsync(string characterUUID) => Empty<Faction>();
        public Task<Faction> GetFactionForCharacterAsync(string characterUUID, string entityUUID) => Task.FromResult<Faction>(null);
        public Task UpsertFactionForCharacterAsync(string characterUUID, Faction entity) => Task.CompletedTask;
        public Task DeleteFactionForCharacterAsync(string characterUUID, string entityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<ExternalCharacter>> GetAllExternalCharactersAsync(string characterUUID) => Empty<ExternalCharacter>();
        public Task<ExternalCharacter> GetExternalCharacterAsync(string characterUUID, string entityUUID) => Task.FromResult<ExternalCharacter>(null);
        public Task UpsertExternalCharacterAsync(string characterUUID, ExternalCharacter entity) => Task.CompletedTask;
        public Task DeleteExternalCharacterAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // ── Helper ──
        private static Task<IReadOnlyList<T>> Empty<T>()
            => Task.FromResult<IReadOnlyList<T>>(new List<T>());

        private Task Track(string entityType)
        {
            UpsertedEntityTypes.Add(entityType);
            return Task.CompletedTask;
        }
    }
}
