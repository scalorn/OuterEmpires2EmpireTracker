// <copyright file="EmpireContextBackendTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using Bp = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for EmpireContext backend integration (LoadBaselineFromBackend and WriteContext).
    /// Satisfies: Req 3, Criteria 1-9.
    /// </summary>
    [TestFixture]
    public class EmpireContextBackendTests
    {
        [SetUp]
        public void SetUp()
        {
            EmpireContext.Reset();
            EmpireContext.RunMigrations = null;
            PlayerContext.WritesBlocked = false;
        }

        [TearDown]
        public void TearDown()
        {
            EmpireContext.Reset();
            EmpireContext.RunMigrations = null;
            PlayerContext.WritesBlocked = false;
        }

        [Test]
        public void LoadFromBackend_JsonBackend_UsesGetGlobalDataAsync()
        {
            var baselineRoot = new BaselineRoot
            {
                GameConstants = new BaselineGameConstants(),
                BlueprintType = new[] { new BlueprintType { Id = "bt-1", Name = "Laser" } },
                ShipClass = new ShipClass[0],
                TechLevel = new TechLevel[0],
            };
            string json = JsonConvert.SerializeObject(baselineRoot);

            var backend = new EmpireTestBackend { GlobalDataJson = json };
            var ctx = CreateContext();
            ctx.StorageBackend = backend;
            ctx.StorageBackendType = StorageBackendType.JsonSingleFile;

            ctx.LoadBaselineFromBackend();

            Assert.That(backend.GetGlobalDataCalled, Is.True,
                "Should call GetGlobalDataAsync for JSON backend");
            Assert.That(ctx.BlueprintTypeList, Has.Count.EqualTo(1));
            Assert.That(ctx.BlueprintTypeList[0].Name, Is.EqualTo("Laser"));
        }

        [Test]
        public void LoadFromBackend_RelationalBackend_UsesTypedMethods()
        {
            var backend = new EmpireTestBackend
            {
                BaselineConstantsToReturn = new BaselineGameConstants(),
                BlueprintTypesToReturn = new List<BlueprintType>
                {
                    new BlueprintType { Id = "bt-1", Name = "Shield" },
                    new BlueprintType { Id = "bt-2", Name = "Armor" },
                },
                ShipClassesToReturn = new List<ShipClass>
                {
                    new ShipClass { Id = 1, Name = "Frigate" },
                },
            };
            var ctx = CreateContext();
            ctx.StorageBackend = backend;
            ctx.StorageBackendType = StorageBackendType.Sqlite;

            ctx.LoadBaselineFromBackend();

            Assert.That(backend.GetBaselineConstantsCalled, Is.True,
                "Should call GetBaselineGameConstantsAsync for relational backend");
            Assert.That(backend.GetAllBlueprintTypesCalled, Is.True,
                "Should call GetAllBlueprintTypesAsync for relational backend");
            Assert.That(ctx.BlueprintTypeList, Has.Count.EqualTo(2));
            Assert.That(ctx.ShipClassList, Has.Count.EqualTo(1));
            Assert.That(ctx.ShipClassList[0].Name, Is.EqualTo("Frigate"));
        }

        [Test]
        public void LoadFromBackend_EmptyBackend_SeedsFromBaselineDataJson()
        {
            string baselinePath = TestHelper.TestDataPath("BaselineData.json");
            EmpireContext.FilePath = baselinePath;
            var backend = new EmpireTestBackend();
            var ctx = CreateContext();

            // Set type first (no trigger since backend is null), then set backend
            ctx.StorageBackendType = StorageBackendType.JsonSingleFile;
            ctx.StorageBackend = backend;

            // After seed, WriteContext should have called UpsertGlobalDataAsync
            Assert.That(backend.UpsertGlobalDataCalled, Is.True,
                "Empty backend should trigger seed and call UpsertGlobalDataAsync");
            Assert.That(ctx.BlueprintTypeList.Count, Is.GreaterThan(0),
                "Seed from file should populate blueprint types");
        }

        [Test]
        public void WriteContext_JsonBackend_CallsUpsertGlobalDataAsync()
        {
            var baselineRoot = new BaselineRoot
            {
                GameConstants = new BaselineGameConstants(),
                BlueprintType = new[] { new BlueprintType { Id = "bt-1", Name = "Laser" } },
                ShipClass = new ShipClass[0],
                TechLevel = new TechLevel[0],
            };
            string json = JsonConvert.SerializeObject(baselineRoot);

            var backend = new EmpireTestBackend { GlobalDataJson = json };
            var ctx = CreateContext();
            ctx.StorageBackendType = StorageBackendType.JsonSingleFile;
            ctx.StorageBackend = backend;

            // Reset tracking flags that were set during auto-reload
            backend.UpsertGlobalDataCalled = false;
            backend.UpsertGlobalDataKey = null;

            ctx.WriteContext();

            Assert.That(backend.UpsertGlobalDataCalled, Is.True,
                "WriteContext should call UpsertGlobalDataAsync for JSON backend");
            Assert.That(backend.UpsertGlobalDataKey, Is.EqualTo("BaselineRoot"));
        }

        [Test]
        public void WriteContext_RelationalBackend_CallsTypedUpsertMethods()
        {
            var backend = new EmpireTestBackend
            {
                BaselineConstantsToReturn = new BaselineGameConstants(),
                BlueprintTypesToReturn = new List<BlueprintType>
                {
                    new BlueprintType { Id = "bt-1", Name = "Laser" },
                },
                ShipClassesToReturn = new List<ShipClass>
                {
                    new ShipClass { Id = 1, Name = "Frigate" },
                },
            };
            var ctx = CreateContext();
            ctx.StorageBackendType = StorageBackendType.Sqlite;
            ctx.StorageBackend = backend;

            // Reset tracking flags that were set during auto-reload
            backend.UpsertBaselineConstantsCalled = false;
            backend.UpsertBlueprintTypesCalled = false;
            backend.UpsertShipClassesCalled = false;
            backend.UpsertTechLevelsCalled = false;
            backend.UpsertCommoditiesCalled = false;
            backend.UpsertRefiningRecipesCalled = false;
            backend.UpsertResearchTimesCalled = false;
            backend.UpsertPropertyTypesCalled = false;

            ctx.WriteContext();

            Assert.That(backend.UpsertBaselineConstantsCalled, Is.True,
                "Should call UpsertBaselineGameConstantsAsync");
            Assert.That(backend.UpsertBlueprintTypesCalled, Is.True,
                "Should call UpsertBlueprintTypesAsync");
            Assert.That(backend.UpsertShipClassesCalled, Is.True,
                "Should call UpsertShipClassesAsync");
            Assert.That(backend.UpsertTechLevelsCalled, Is.True,
                "Should call UpsertTechLevelsAsync");
            Assert.That(backend.UpsertCommoditiesCalled, Is.True,
                "Should call UpsertCommoditiesAsync");
            Assert.That(backend.UpsertRefiningRecipesCalled, Is.True,
                "Should call UpsertRefiningRecipesAsync");
            Assert.That(backend.UpsertResearchTimesCalled, Is.True,
                "Should call UpsertResearchTimesAsync");
            Assert.That(backend.UpsertPropertyTypesCalled, Is.True,
                "Should call UpsertPropertyTypeDefinitionsAsync");
        }

        [Test]
        public void WriteContext_NoBackend_LegacyFileWritePreserved()
        {
            string tempFile = Path.Combine(
                Path.GetTempPath(), Guid.NewGuid() + ".json");
            try
            {
                var ctx = CreateContext();
                ctx.StorageBackend = null;
                ctx.StorageBackendType = null;
                EmpireContext.FilePath = tempFile;

                ctx.WriteContext();

                Assert.That(File.Exists(tempFile), Is.True,
                    "Legacy file write should occur when no backend configured");
                string content = File.ReadAllText(tempFile);
                Assert.That(content, Does.Contain("GameConstants"));
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Test]
        public void LoadFromBackend_StorageLoadException_Propagated_NoPartialInit()
        {
            var backend = new EmpireTestBackend { ThrowOnGlobalDataRead = true };
            var ctx = CreateContext();
            ctx.StorageBackendType = StorageBackendType.JsonSingleFile;

            // Setting StorageBackend triggers LoadBaselineFromBackend when type is set
            Assert.Throws<StorageLoadException>(() => { ctx.StorageBackend = backend; },
                "StorageLoadException should propagate without being caught");
        }

        /// <summary>
        /// Creates an EmpireContext via the internal constructor with minimal baseline.
        /// </summary>
        private static EmpireContext CreateContext()
        {
            var baselineRoot = new BaselineRoot
            {
                GameConstants = new BaselineGameConstants(),
                BlueprintType = new BlueprintType[0],
                ShipClass = new ShipClass[0],
                TechLevel = new TechLevel[0],
            };
            var playerRoot = new PlayerRoot();
            return new EmpireContext(baselineRoot, playerRoot);
        }
    }

    /// <summary>
    /// Test backend that implements IStorageBackend with configurable baseline
    /// data returns and call tracking for EmpireContext tests.
    /// Only the baseline and global-data methods have meaningful implementations;
    /// per-character and permission methods return empty defaults.
    /// </summary>
    internal class EmpireTestBackend : IStorageBackend
    {
        public string GlobalDataJson { get; set; }

        public bool ThrowOnGlobalDataRead { get; set; }

        public BaselineGameConstants BaselineConstantsToReturn { get; set; }

        public List<BlueprintType> BlueprintTypesToReturn { get; set; }

        public List<ShipClass> ShipClassesToReturn { get; set; }

        public List<TechLevel> TechLevelsToReturn { get; set; }

        public List<Commodity> CommoditiesToReturn { get; set; }

        public List<RefiningRecipe> RefiningRecipesToReturn { get; set; }

        public List<ResearchTimeEntry> ResearchTimesToReturn { get; set; }

        public List<PropertyTypeDefinition> PropertyTypesToReturn { get; set; }

        // Call tracking flags
        public bool GetGlobalDataCalled { get; set; }

        public bool GetBaselineConstantsCalled { get; set; }

        public bool GetAllBlueprintTypesCalled { get; set; }

        public bool UpsertGlobalDataCalled { get; set; }

        public string UpsertGlobalDataKey { get; set; }

        public bool UpsertBaselineConstantsCalled { get; set; }

        public bool UpsertBlueprintTypesCalled { get; set; }

        public bool UpsertShipClassesCalled { get; set; }

        public bool UpsertTechLevelsCalled { get; set; }

        public bool UpsertCommoditiesCalled { get; set; }

        public bool UpsertRefiningRecipesCalled { get; set; }

        public bool UpsertResearchTimesCalled { get; set; }

        public bool UpsertPropertyTypesCalled { get; set; }

        // Lifecycle
        public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<bool> ValidateConnectionAsync(CancellationToken ct = default) => Task.FromResult(true);

        public StorageInfo GetStorageInfo() => new StorageInfo { BackendType = "Test", Location = "memory://test" };

        public Task<IReadOnlyList<string>> GetAllCharacterUUIDsAsync() => Task.FromResult<IReadOnlyList<string>>(new List<string>());

        // Global Data — tracked
        public Task<string> GetGlobalDataAsync(string dataType)
        {
            if (ThrowOnGlobalDataRead) throw new StorageLoadException("Test load failure");
            GetGlobalDataCalled = true;
            return Task.FromResult(GlobalDataJson);
        }

        public Task UpsertGlobalDataAsync(string dataType, string json)
        {
            UpsertGlobalDataCalled = true;
            UpsertGlobalDataKey = dataType;
            return Task.CompletedTask;
        }

        // Baseline reads — tracked
        public Task<BaselineGameConstants> GetBaselineGameConstantsAsync()
        {
            if (ThrowOnGlobalDataRead) throw new StorageLoadException("Test load failure");
            GetBaselineConstantsCalled = true;
            return Task.FromResult(BaselineConstantsToReturn);
        }

        public Task<IReadOnlyList<BlueprintType>> GetAllBlueprintTypesAsync()
        {
            GetAllBlueprintTypesCalled = true;
            return Task.FromResult<IReadOnlyList<BlueprintType>>(BlueprintTypesToReturn ?? new List<BlueprintType>());
        }

        public Task<IReadOnlyList<ShipClass>> GetAllShipClassesAsync()
            => Task.FromResult<IReadOnlyList<ShipClass>>(ShipClassesToReturn ?? new List<ShipClass>());

        public Task<IReadOnlyList<TechLevel>> GetAllTechLevelsAsync()
            => Task.FromResult<IReadOnlyList<TechLevel>>(TechLevelsToReturn ?? new List<TechLevel>());

        public Task<IReadOnlyList<Commodity>> GetAllCommoditiesAsync()
            => Task.FromResult<IReadOnlyList<Commodity>>(CommoditiesToReturn ?? new List<Commodity>());

        public Task<IReadOnlyList<RefiningRecipe>> GetAllRefiningRecipesAsync()
            => Task.FromResult<IReadOnlyList<RefiningRecipe>>(RefiningRecipesToReturn ?? new List<RefiningRecipe>());

        public Task<IReadOnlyList<ResearchTimeEntry>> GetAllResearchTimesAsync()
            => Task.FromResult<IReadOnlyList<ResearchTimeEntry>>(ResearchTimesToReturn ?? new List<ResearchTimeEntry>());

        public Task<IReadOnlyList<PropertyTypeDefinition>> GetAllPropertyTypeDefinitionsAsync()
            => Task.FromResult<IReadOnlyList<PropertyTypeDefinition>>(PropertyTypesToReturn ?? new List<PropertyTypeDefinition>());

        // Baseline writes — tracked
        public Task UpsertBaselineGameConstantsAsync(BaselineGameConstants constants)
        {
            UpsertBaselineConstantsCalled = true;
            return Task.CompletedTask;
        }

        public Task UpsertBlueprintTypesAsync(IReadOnlyList<BlueprintType> types)
        {
            UpsertBlueprintTypesCalled = true;
            return Task.CompletedTask;
        }

        public Task UpsertShipClassesAsync(IReadOnlyList<ShipClass> classes)
        {
            UpsertShipClassesCalled = true;
            return Task.CompletedTask;
        }

        public Task UpsertTechLevelsAsync(IReadOnlyList<TechLevel> levels)
        {
            UpsertTechLevelsCalled = true;
            return Task.CompletedTask;
        }

        public Task UpsertCommoditiesAsync(IReadOnlyList<Commodity> commodities)
        {
            UpsertCommoditiesCalled = true;
            return Task.CompletedTask;
        }

        public Task UpsertRefiningRecipesAsync(IReadOnlyList<RefiningRecipe> recipes)
        {
            UpsertRefiningRecipesCalled = true;
            return Task.CompletedTask;
        }

        public Task UpsertResearchTimesAsync(IReadOnlyList<ResearchTimeEntry> entries)
        {
            UpsertResearchTimesCalled = true;
            return Task.CompletedTask;
        }

        public Task UpsertPropertyTypeDefinitionsAsync(IReadOnlyList<PropertyTypeDefinition> definitions)
        {
            UpsertPropertyTypesCalled = true;
            return Task.CompletedTask;
        }

        // Server Factions — not used by EmpireContext tests
        public Task<ServerFaction> GetFactionAsync(string uuid) => Task.FromResult<ServerFaction>(null);
        public Task<IReadOnlyList<ServerFaction>> GetAllFactionsAsync() => Empty<ServerFaction>();
        public Task UpsertFactionAsync(ServerFaction faction) => Task.CompletedTask;
        public Task DeleteFactionAsync(string uuid) => Task.CompletedTask;

        // Server Characters
        public Task<ServerCharacter> GetCharacterAsync(string uuid) => Task.FromResult<ServerCharacter>(null);
        public Task<IReadOnlyList<ServerCharacter>> GetAllCharactersAsync() => Empty<ServerCharacter>();
        public Task UpsertCharacterAsync(ServerCharacter character) => Task.CompletedTask;
        public Task DeleteCharacterAsync(string uuid) => Task.CompletedTask;

        // Star Systems
        public Task<IReadOnlyList<StarSystem>> GetAllStarSystemsAsync() => Empty<StarSystem>();
        public Task UpsertStarSystemsAsync(IReadOnlyList<StarSystem> systems) => Task.CompletedTask;

        // Colony Summaries
        public Task<IReadOnlyList<ColonySummary>> GetColonySummariesForSystemAsync(int systemId) => Empty<ColonySummary>();

        // API Tokens
        public Task<ApiToken> FindTokenByHashAsync(string tokenHash) => Task.FromResult<ApiToken>(null);
        public Task<IReadOnlyList<ApiToken>> GetAllTokensAsync() => Empty<ApiToken>();
        public Task UpsertTokenAsync(ApiToken token) => Task.CompletedTask;
        public Task DeleteTokenAsync(string id) => Task.CompletedTask;

        // Membership Actions
        public Task<IReadOnlyList<MembershipAction>> GetFactionActionsAsync(string factionUUID) => Empty<MembershipAction>();
        public Task UpsertMembershipActionAsync(MembershipAction action) => Task.CompletedTask;
        public Task DeleteMembershipActionAsync(string id) => Task.CompletedTask;
        public Task DeleteExpiredActionsAsync(DateTime cutoff) => Task.CompletedTask;

        // Sharing Rules
        public Task<IReadOnlyList<SharingRule>> GetSharingRulesForCharacterAsync(string characterUUID) => Empty<SharingRule>();
        public Task UpsertSharingRulesAsync(string characterUUID, IReadOnlyList<SharingRule> rules) => Task.CompletedTask;

        // Character Preferences
        public Task<CharacterPreferences> GetCharacterPreferencesAsync(string characterUUID) => Task.FromResult<CharacterPreferences>(null);
        public Task UpsertCharacterPreferencesAsync(CharacterPreferences prefs) => Task.CompletedTask;

        // Per-Character entities — all return empty
        public Task<IReadOnlyList<Colony>> GetAllColoniesAsync(string characterUUID) => Empty<Colony>();
        public Task<Colony> GetColonyAsync(string characterUUID, string entityUUID) => Task.FromResult<Colony>(null);
        public Task UpsertColonyAsync(string characterUUID, Colony entity) => Task.CompletedTask;
        public Task DeleteColonyAsync(string characterUUID, string entityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<Bp>> GetAllBlueprintsAsync(string characterUUID) => Empty<Bp>();
        public Task<Bp> GetBlueprintAsync(string characterUUID, string entityUUID) => Task.FromResult<Bp>(null);
        public Task UpsertBlueprintAsync(string characterUUID, Bp entity) => Task.CompletedTask;
        public Task DeleteBlueprintAsync(string characterUUID, string entityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<Survey>> GetAllSurveysAsync(string characterUUID) => Empty<Survey>();
        public Task<Survey> GetSurveyAsync(string characterUUID, string entityUUID) => Task.FromResult<Survey>(null);
        public Task UpsertSurveyAsync(string characterUUID, Survey entity) => Task.CompletedTask;
        public Task DeleteSurveyAsync(string characterUUID, string entityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<PlayerProfile>> GetAllPlayerProfilesAsync(string characterUUID) => Empty<PlayerProfile>();
        public Task<PlayerProfile> GetPlayerProfileAsync(string characterUUID, string entityUUID) => Task.FromResult<PlayerProfile>(null);
        public Task UpsertPlayerProfileAsync(string characterUUID, PlayerProfile entity) => Task.CompletedTask;
        public Task DeletePlayerProfileAsync(string characterUUID, string entityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<DeliveryRoute>> GetAllDeliveryRoutesAsync(string characterUUID) => Empty<DeliveryRoute>();
        public Task<DeliveryRoute> GetDeliveryRouteAsync(string characterUUID, string entityUUID) => Task.FromResult<DeliveryRoute>(null);
        public Task UpsertDeliveryRouteAsync(string characterUUID, DeliveryRoute entity) => Task.CompletedTask;
        public Task DeleteDeliveryRouteAsync(string characterUUID, string entityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<DeliveryPlan>> GetAllDeliveryPlansAsync(string characterUUID) => Empty<DeliveryPlan>();
        public Task<DeliveryPlan> GetDeliveryPlanAsync(string characterUUID, string entityUUID) => Task.FromResult<DeliveryPlan>(null);
        public Task UpsertDeliveryPlanAsync(string characterUUID, DeliveryPlan entity) => Task.CompletedTask;
        public Task DeleteDeliveryPlanAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        public Task<IReadOnlyList<Ship>> GetAllShipsAsync(string characterUUID) => Empty<Ship>();
        public Task<Ship> GetShipAsync(string characterUUID, string entityUUID) => Task.FromResult<Ship>(null);
        public Task UpsertShipAsync(string characterUUID, Ship entity) => Task.CompletedTask;
        public Task DeleteShipAsync(string characterUUID, string entityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<ShipTemplate>> GetAllShipTemplatesAsync(string characterUUID) => Empty<ShipTemplate>();
        public Task<ShipTemplate> GetShipTemplateAsync(string characterUUID, string entityUUID) => Task.FromResult<ShipTemplate>(null);
        public Task UpsertShipTemplateAsync(string characterUUID, ShipTemplate entity) => Task.CompletedTask;
        public Task DeleteShipTemplateAsync(string characterUUID, string entityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<Station>> GetAllStationsAsync(string characterUUID) => Empty<Station>();
        public Task<Station> GetStationAsync(string characterUUID, string entityUUID) => Task.FromResult<Station>(null);
        public Task UpsertStationAsync(string characterUUID, Station entity) => Task.CompletedTask;
        public Task DeleteStationAsync(string characterUUID, string entityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<MarketListing>> GetAllMarketListingsAsync(string characterUUID) => Empty<MarketListing>();
        public Task<MarketListing> GetMarketListingAsync(string characterUUID, string entityUUID) => Task.FromResult<MarketListing>(null);
        public Task UpsertMarketListingAsync(string characterUUID, MarketListing entity) => Task.CompletedTask;
        public Task DeleteMarketListingAsync(string characterUUID, string entityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<MarketTransaction>> GetAllMarketTransactionsAsync(string characterUUID) => Empty<MarketTransaction>();
        public Task<MarketTransaction> GetMarketTransactionAsync(string characterUUID, string entityUUID) => Task.FromResult<MarketTransaction>(null);
        public Task UpsertMarketTransactionAsync(string characterUUID, MarketTransaction entity) => Task.CompletedTask;
        public Task DeleteMarketTransactionAsync(string characterUUID, string entityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<PricingPlan>> GetAllPricingPlansAsync(string characterUUID) => Empty<PricingPlan>();
        public Task<PricingPlan> GetPricingPlanAsync(string characterUUID, string entityUUID) => Task.FromResult<PricingPlan>(null);
        public Task UpsertPricingPlanAsync(string characterUUID, PricingPlan entity) => Task.CompletedTask;
        public Task DeletePricingPlanAsync(string characterUUID, string entityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<StockPlan>> GetAllStockPlansAsync(string characterUUID) => Empty<StockPlan>();
        public Task<StockPlan> GetStockPlanAsync(string characterUUID, string entityUUID) => Task.FromResult<StockPlan>(null);
        public Task UpsertStockPlanAsync(string characterUUID, StockPlan entity) => Task.CompletedTask;
        public Task DeleteStockPlanAsync(string characterUUID, string entityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<StockProfile>> GetAllStockProfilesAsync(string characterUUID) => Empty<StockProfile>();
        public Task<StockProfile> GetStockProfileAsync(string characterUUID, string entityUUID) => Task.FromResult<StockProfile>(null);
        public Task UpsertStockProfileAsync(string characterUUID, StockProfile entity) => Task.CompletedTask;
        public Task DeleteStockProfileAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        public Task<IReadOnlyList<BuildPlan>> GetAllBuildPlansAsync(string characterUUID) => Empty<BuildPlan>();
        public Task<BuildPlan> GetBuildPlanAsync(string characterUUID, string entityUUID) => Task.FromResult<BuildPlan>(null);
        public Task UpsertBuildPlanAsync(string characterUUID, BuildPlan entity) => Task.CompletedTask;
        public Task DeleteBuildPlanAsync(string characterUUID, string entityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<SupplyChain>> GetAllSupplyChainsAsync(string characterUUID) => Empty<SupplyChain>();
        public Task<SupplyChain> GetSupplyChainAsync(string characterUUID, string entityUUID) => Task.FromResult<SupplyChain>(null);
        public Task UpsertSupplyChainAsync(string characterUUID, SupplyChain entity) => Task.CompletedTask;
        public Task DeleteSupplyChainAsync(string characterUUID, string entityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<Asteroid>> GetAllAsteroidsAsync(string characterUUID) => Empty<Asteroid>();
        public Task<Asteroid> GetAsteroidAsync(string characterUUID, string entityUUID) => Task.FromResult<Asteroid>(null);
        public Task UpsertAsteroidAsync(string characterUUID, Asteroid entity) => Task.CompletedTask;
        public Task DeleteAsteroidAsync(string characterUUID, string entityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<Faction>> GetAllFactionsForCharacterAsync(string characterUUID) => Empty<Faction>();
        public Task<Faction> GetFactionForCharacterAsync(string characterUUID, string entityUUID) => Task.FromResult<Faction>(null);
        public Task UpsertFactionForCharacterAsync(string characterUUID, Faction entity) => Task.CompletedTask;
        public Task DeleteFactionForCharacterAsync(string characterUUID, string entityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<ExternalCharacter>> GetAllExternalCharactersAsync(string characterUUID) => Empty<ExternalCharacter>();
        public Task<ExternalCharacter> GetExternalCharacterAsync(string characterUUID, string entityUUID) => Task.FromResult<ExternalCharacter>(null);
        public Task UpsertExternalCharacterAsync(string characterUUID, ExternalCharacter entity) => Task.CompletedTask;
        public Task DeleteExternalCharacterAsync(string characterUUID, string entityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<WarehouseOverflowRule>> GetAllWarehouseOverflowRulesAsync(string characterUUID) => Empty<WarehouseOverflowRule>();
        public Task<WarehouseOverflowRule> GetWarehouseOverflowRuleAsync(string characterUUID, string entityUUID) => Task.FromResult<WarehouseOverflowRule>(null);
        public Task UpsertWarehouseOverflowRuleAsync(string characterUUID, WarehouseOverflowRule entity) => Task.CompletedTask;
        public Task DeleteWarehouseOverflowRuleAsync(string characterUUID, string entityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<MailMessage>> GetAllMailMessagesAsync(string characterUUID) => Empty<MailMessage>();
        public Task<MailMessage> GetMailMessageAsync(string characterUUID, string entityUUID) => Task.FromResult<MailMessage>(null);
        public Task UpsertMailMessageAsync(string characterUUID, MailMessage entity) => Task.CompletedTask;
        public Task DeleteMailMessageAsync(string characterUUID, string entityUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<BankingTransaction>> GetAllBankingTransactionsAsync(string characterUUID) => Empty<BankingTransaction>();
        public Task<BankingTransaction> GetBankingTransactionAsync(string characterUUID, string entityUUID) => Task.FromResult<BankingTransaction>(null);
        public Task UpsertBankingTransactionAsync(string characterUUID, BankingTransaction entity) => Task.CompletedTask;
        public Task DeleteBankingTransactionAsync(string characterUUID, string entityUUID) => Task.CompletedTask;

        // Faction Permission Entities — not used
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

        // Character Permission Entities — not used
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

        // Intel — not used
        public Task<IReadOnlyList<IntelComment>> GetIntelCommentsForTargetAsync(string targetCharacterUUID) => Empty<IntelComment>();
        public Task<IntelComment> GetIntelCommentAsync(string commentUUID) => Task.FromResult<IntelComment>(null);
        public Task UpsertIntelCommentAsync(IntelComment comment) => Task.CompletedTask;
        public Task DeleteIntelCommentAsync(string commentUUID) => Task.CompletedTask;
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForCommentAsync(string commentUUID) => Empty<IntelCommentFactionShare>();
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForFactionAsync(string factionUUID) => Empty<IntelCommentFactionShare>();
        public Task UpsertIntelShareAsync(IntelCommentFactionShare share) => Task.CompletedTask;
        public Task DeleteIntelShareAsync(string shareUUID) => Task.CompletedTask;

        // Audit — not used
        public Task<IReadOnlyList<PermissionAuditEntry>> GetPermissionAuditEntriesAsync(DateTime? startDate = null, DateTime? endDate = null, PermissionActionType? actionType = null, string actorUUID = null, string targetUUID = null) => Empty<PermissionAuditEntry>();
        public Task AppendPermissionAuditEntryAsync(PermissionAuditEntry entry) => Task.CompletedTask;
        public Task DeleteExpiredAuditEntriesAsync(DateTime cutoff) => Task.CompletedTask;

        private static Task<IReadOnlyList<T>> Empty<T>() => Task.FromResult<IReadOnlyList<T>>(new List<T>());
    }
}
