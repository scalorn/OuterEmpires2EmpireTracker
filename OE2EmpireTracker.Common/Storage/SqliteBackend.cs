// -----------------------------------------------------------------------
// <copyright file="SqliteBackend.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NLog;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Common.Storage
{
    /// <summary>
    /// IStorageBackend implementation that persists all data in a single SQLite
    /// database file using a fully normalized relational schema.
    /// </summary>
    internal class SqliteBackend : IStorageBackend
    {
        private const int CurrentSchemaVersion = 1;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Schema DDL concatenated from tasks 5.2-5.11. ExecuteSchema runs this
        /// against a fresh database.
        /// </summary>
        private static readonly string SchemaDdl = ColonySchema + ItemsBlueprintSchema + SurveyPlayerProfileSchema + DeliveryRouteShipSchema + DeliveryPlanMarketSchema + PricingBuildStockSchema + RemainingPlayerEntitySchema + ServerGlobalSchema + PermissionSchema + IntelAuditBaselineSchema;

        private readonly string _connectionString;
        private readonly string _databasePath;

        private const string ColonySchema = @"
CREATE TABLE IF NOT EXISTS Colonies (
    UUID TEXT PRIMARY KEY,
    OwnerUUID TEXT NOT NULL,
    LegacyUUID TEXT,
    PlanetName TEXT,
    SystemName TEXT NOT NULL DEFAULT '',
    ColonyName TEXT,
    LastImportDateTime TEXT,
    ColonyId INTEGER NOT NULL DEFAULT 0,
    SystemId INTEGER NOT NULL DEFAULT 0,
    ColonySize INTEGER NOT NULL DEFAULT 0,
    Distance REAL NOT NULL DEFAULT 0,
    SurfaceVariation INTEGER NOT NULL DEFAULT 0,
    AtmosVariation INTEGER NOT NULL DEFAULT 0,
    HexValue TEXT NOT NULL DEFAULT '',
    SystemObjectTypeName TEXT NOT NULL DEFAULT '',
    ImagePreFix TEXT NOT NULL DEFAULT '',
    ManufacturingBlocked INTEGER NOT NULL DEFAULT 0,
    WorkerCurrentAttitude INTEGER NOT NULL DEFAULT 0,
    ContentmentIndex INTEGER NOT NULL DEFAULT 0,
    BlueCollarAllocated INTEGER NOT NULL DEFAULT 0,
    BlueCollarUnallocated INTEGER NOT NULL DEFAULT 0,
    WhiteCollarAllocated INTEGER NOT NULL DEFAULT 0,
    WhiteCollarUnallocated INTEGER NOT NULL DEFAULT 0,
    SpecialistAllocated INTEGER NOT NULL DEFAULT 0,
    SpecialistUnallocated INTEGER NOT NULL DEFAULT 0,
    WageLevel INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS ColonyStructures (
    UUID TEXT PRIMARY KEY,
    ColonyUUID TEXT NOT NULL REFERENCES Colonies(UUID) ON DELETE CASCADE,
    Sequence INTEGER NOT NULL DEFAULT 0,
    FlatpackBlueprintUUID TEXT,
    DisplaySequence INTEGER NOT NULL DEFAULT 0,
    BuildingID INTEGER NOT NULL DEFAULT 0,
    BuildQueueSequence INTEGER NOT NULL DEFAULT 0,
    MiningSurvey TEXT,
    MiningSurveyResource TEXT,
    MiningLeftOvers REAL NOT NULL DEFAULT 0,
    RefiningResource TEXT,
    RefiningResourcePurity TEXT,
    ResearchingBlueprintUUID TEXT,
    ManufacturingBlueprintUUID TEXT,
    ManufacturingCommodityName TEXT,
    ManufacturingQuantity INTEGER NOT NULL DEFAULT 0,
    ManufacturingCompleted INTEGER NOT NULL DEFAULT 0,
    StagingResources INTEGER NOT NULL DEFAULT 0,
    ColonyBuildingTypeId INTEGER NOT NULL DEFAULT 0,
    ResourceId INTEGER NOT NULL DEFAULT 0,
    ResourceIcon TEXT NOT NULL DEFAULT '',
    ManufactureAmountPerRun INTEGER NOT NULL DEFAULT 0,
    DurabilityCurrent REAL NOT NULL DEFAULT 0,
    DurabilityMax REAL NOT NULL DEFAULT 0,
    WageLevel INTEGER NOT NULL DEFAULT 0,
    BuildCompletion_StartTime TEXT,
    BuildCompletion_RepeatIntervalSeconds INTEGER,
    BuildCompletion_IsRepeating INTEGER,
    ProcessCompletion_StartTime TEXT,
    ProcessCompletion_RepeatIntervalSeconds INTEGER,
    ProcessCompletion_IsRepeating INTEGER
);

CREATE TABLE IF NOT EXISTS ColonyStructureProperties (
    StructureUUID TEXT NOT NULL REFERENCES ColonyStructures(UUID) ON DELETE CASCADE,
    Key TEXT NOT NULL,
    Value TEXT NOT NULL,
    PRIMARY KEY (StructureUUID, Key)
);

CREATE TABLE IF NOT EXISTS ColonyStructureWorkers (
    StructureUUID TEXT NOT NULL REFERENCES ColonyStructures(UUID) ON DELETE CASCADE,
    Key TEXT NOT NULL,
    Value TEXT NOT NULL,
    PRIMARY KEY (StructureUUID, Key)
);
";

        private const string ItemsBlueprintSchema = @"
-- UNIFIED ITEMS TABLE
CREATE TABLE IF NOT EXISTS Items (
    UUID TEXT PRIMARY KEY,
    ParentUUID TEXT NOT NULL,
    ParentType TEXT NOT NULL,
    ItemType TEXT NOT NULL,
    BaseItemTypeID TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL DEFAULT '',
    NickName TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT '',
    Quantity INTEGER NOT NULL DEFAULT 0,
    ResourcePurity TEXT NOT NULL DEFAULT '',
    Volume REAL NOT NULL DEFAULT 0,
    CurrentHP INTEGER NOT NULL DEFAULT 0,
    MaxHP INTEGER NOT NULL DEFAULT 0,
    MaxRepairPercent REAL NOT NULL DEFAULT 0,
    Mass REAL,
    GameItemId INTEGER,
    JobRef INTEGER,
    JobDeliveryLoc INTEGER,
    HealthPercentage REAL,
    LastRepairHealthPercentage REAL,
    Evolution INTEGER,
    ShipPartType TEXT NOT NULL DEFAULT '',
    JobName TEXT NOT NULL DEFAULT '',
    JobTrack TEXT NOT NULL DEFAULT ''
);

CREATE INDEX IF NOT EXISTS IX_Items_Parent ON Items (ParentUUID, ParentType);

-- BLUEPRINT
CREATE TABLE IF NOT EXISTS Blueprints (
    UUID TEXT PRIMARY KEY,
    OwnerUUID TEXT NOT NULL DEFAULT '',
    BaseBlueprintUUID TEXT,
    LegacyUUID TEXT,
    BluePrintType TEXT,
    TechLevel TEXT,
    Class INTEGER NOT NULL DEFAULT 0,
    Evolution INTEGER NOT NULL DEFAULT 0,
    CopyCost INTEGER NOT NULL DEFAULT 0,
    ItemType TEXT NOT NULL,
    BaseItemTypeID TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL DEFAULT '',
    NickName TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT '',
    Quantity INTEGER NOT NULL DEFAULT 0,
    Volume REAL NOT NULL DEFAULT 0,
    GameApiBlueprintId INTEGER,
    LastDetailImportUtc TEXT
);

CREATE TABLE IF NOT EXISTS BlueprintProperties (
    BlueprintUUID TEXT NOT NULL REFERENCES Blueprints(UUID) ON DELETE CASCADE,
    Key TEXT NOT NULL,
    Value TEXT NOT NULL,
    PRIMARY KEY (BlueprintUUID, Key)
);

CREATE TABLE IF NOT EXISTS BlueprintResources (
    BlueprintUUID TEXT NOT NULL REFERENCES Blueprints(UUID) ON DELETE CASCADE,
    ResourceName TEXT NOT NULL,
    Amount INTEGER NOT NULL,
    PRIMARY KEY (BlueprintUUID, ResourceName)
);
";

        private const string SurveyPlayerProfileSchema = @"
-- SURVEY
CREATE TABLE IF NOT EXISTS Surveys (
    UUID TEXT PRIMARY KEY,
    OwnerUUID TEXT NOT NULL DEFAULT '',
    ItemType TEXT NOT NULL,
    BaseItemTypeID TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL DEFAULT '',
    NickName TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT '',
    Quantity INTEGER NOT NULL DEFAULT 0,
    Volume REAL NOT NULL DEFAULT 0,
    ScannedBy TEXT,
    DateTime TEXT,
    PlanetName TEXT,
    SystemName TEXT NOT NULL DEFAULT '',
    SurveyID TEXT,
    ScannerBlueprintUUID TEXT,
    SurveyType TEXT NOT NULL DEFAULT 'Planet',
    AsteroidUUID TEXT NOT NULL DEFAULT '',
    SystemObjectId INTEGER NOT NULL DEFAULT 0,
    GameApiSurveyId INTEGER,
    LastDetailImportUtc TEXT
);

CREATE TABLE IF NOT EXISTS SurveyProperties (
    SurveyUUID TEXT NOT NULL REFERENCES Surveys(UUID) ON DELETE CASCADE,
    Key TEXT NOT NULL,
    Value TEXT NOT NULL,
    PRIMARY KEY (SurveyUUID, Key)
);

CREATE TABLE IF NOT EXISTS SurveyResources (
    SurveyUUID TEXT NOT NULL REFERENCES Surveys(UUID) ON DELETE CASCADE,
    ResourceKey TEXT NOT NULL,
    Resource TEXT NOT NULL DEFAULT '',
    Purity TEXT NOT NULL DEFAULT '',
    Amount INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (SurveyUUID, ResourceKey)
);

-- PLAYER PROFILE
CREATE TABLE IF NOT EXISTS PlayerProfiles (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    Faction TEXT NOT NULL DEFAULT '',
    FactionUUID TEXT NOT NULL DEFAULT '',
    TotalCredits REAL NOT NULL DEFAULT 0,
    SkillPoints INTEGER NOT NULL DEFAULT 0,
    CitizenId TEXT NOT NULL DEFAULT '',
    RegistrationDate TEXT NOT NULL DEFAULT '',
    ActiveTime TEXT NOT NULL DEFAULT '',
    CharacterId INTEGER NOT NULL DEFAULT 0,
    FirstName TEXT NOT NULL DEFAULT '',
    LastName TEXT NOT NULL DEFAULT '',
    ActiveTimeMinutes INTEGER NOT NULL DEFAULT 0,
    PublicRank_Rank INTEGER NOT NULL DEFAULT 0,
    PublicRank_CurrentXp INTEGER NOT NULL DEFAULT 0,
    PublicRank_XpToNextLevel INTEGER NOT NULL DEFAULT 0,
    PublicRank_RankName TEXT NOT NULL DEFAULT '',
    PrivateRank_Rank INTEGER NOT NULL DEFAULT 0,
    PrivateRank_CurrentXp INTEGER NOT NULL DEFAULT 0,
    PrivateRank_XpToNextLevel INTEGER NOT NULL DEFAULT 0,
    PrivateRank_RankName TEXT NOT NULL DEFAULT '',
    MilitaryRank_Rank INTEGER NOT NULL DEFAULT 0,
    MilitaryRank_CurrentXp INTEGER NOT NULL DEFAULT 0,
    MilitaryRank_XpToNextLevel INTEGER NOT NULL DEFAULT 0,
    MilitaryRank_RankName TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS PlayerSkills (
    PlayerUUID TEXT NOT NULL REFERENCES PlayerProfiles(UUID) ON DELETE CASCADE,
    SkillName TEXT NOT NULL,
    Level INTEGER NOT NULL DEFAULT 0,
    TrainingStarted INTEGER NOT NULL DEFAULT 0,
    SkillId INTEGER NOT NULL DEFAULT 0,
    EffectDescription TEXT NOT NULL DEFAULT '',
    AmountPerLevel INTEGER NOT NULL DEFAULT 0,
    SkillGroupName TEXT NOT NULL DEFAULT '',
    IsUnlocked INTEGER NOT NULL DEFAULT 0,
    TargetLevel INTEGER NOT NULL DEFAULT 0,
    TrainingPercentageComplete INTEGER NOT NULL DEFAULT 0,
    RemainingMinutes INTEGER NOT NULL DEFAULT 0,
    Completion_StartTime TEXT,
    Completion_RepeatIntervalSeconds INTEGER,
    Completion_IsRepeating INTEGER,
    PRIMARY KEY (PlayerUUID, SkillName)
);
";

        private const string DeliveryRouteShipSchema = @"
-- DELIVERY ROUTE
CREATE TABLE IF NOT EXISTS DeliveryRoutes (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    OwnerUUID TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS DeliveryRouteStops (
    DeliveryRouteUUID TEXT NOT NULL REFERENCES DeliveryRoutes(UUID) ON DELETE CASCADE,
    Sequence INTEGER NOT NULL,
    ColonyUUID TEXT NOT NULL DEFAULT '',
    DestinationType TEXT NOT NULL DEFAULT 'Colony',
    DestinationUUID TEXT NOT NULL DEFAULT '',
    Purpose TEXT NOT NULL DEFAULT 'Cargo',
    FuelEstimate REAL NOT NULL DEFAULT 0,
    PRIMARY KEY (DeliveryRouteUUID, Sequence)
);

-- SHIP
CREATE TABLE IF NOT EXISTS Ships (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    OwnerUUID TEXT NOT NULL DEFAULT '',
    TemplateUUID TEXT NOT NULL DEFAULT '',
    HullBlueprintUUID TEXT NOT NULL DEFAULT '',
    LocationType TEXT NOT NULL DEFAULT 'Station',
    LocationUUID TEXT NOT NULL DEFAULT '',
    GameLocationId INTEGER,
    HullCurrentHP INTEGER NOT NULL DEFAULT 0,
    HullMaxHP INTEGER NOT NULL DEFAULT 0,
    HullMaxRepairPercent REAL NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS ShipComponents (
    ShipUUID TEXT NOT NULL REFERENCES Ships(UUID) ON DELETE CASCADE,
    Sequence INTEGER NOT NULL,
    SlotType TEXT NOT NULL DEFAULT '',
    BlueprintUUID TEXT NOT NULL DEFAULT '',
    CurrentHP INTEGER NOT NULL DEFAULT 0,
    MaxHP INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (ShipUUID, Sequence)
);

-- SHIP TEMPLATE
CREATE TABLE IF NOT EXISTS ShipTemplates (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    OwnerUUID TEXT NOT NULL DEFAULT '',
    HullBlueprintUUID TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS ShipTemplateComponents (
    ShipTemplateUUID TEXT NOT NULL REFERENCES ShipTemplates(UUID) ON DELETE CASCADE,
    Sequence INTEGER NOT NULL,
    SlotType TEXT NOT NULL DEFAULT '',
    SlotIndex INTEGER NOT NULL DEFAULT 0,
    BlueprintUUID TEXT NOT NULL DEFAULT '',
    CurrentHP INTEGER NOT NULL DEFAULT 0,
    MaxHP INTEGER NOT NULL DEFAULT 0,
    MaxRepairPercent REAL NOT NULL DEFAULT 0,
    PRIMARY KEY (ShipTemplateUUID, Sequence)
);
";

        private const string DeliveryPlanMarketSchema = @"
-- DELIVERY PLAN
CREATE TABLE IF NOT EXISTS DeliveryPlans (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    OwnerUUID TEXT NOT NULL DEFAULT '',
    RouteUUID TEXT NOT NULL DEFAULT '',
    ShipUUID TEXT NOT NULL DEFAULT '',
    Completed INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS DeliveryPlanStops (
    DeliveryPlanUUID TEXT NOT NULL REFERENCES DeliveryPlans(UUID) ON DELETE CASCADE,
    Sequence INTEGER NOT NULL,
    ColonyUUID TEXT NOT NULL DEFAULT '',
    StopCompleted INTEGER NOT NULL DEFAULT 0,
    DestinationType TEXT NOT NULL DEFAULT 'Colony',
    DestinationUUID TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (DeliveryPlanUUID, Sequence)
);

CREATE TABLE IF NOT EXISTS DeliveryPlanItems (
    DeliveryPlanUUID TEXT NOT NULL,
    StopSequence INTEGER NOT NULL,
    Direction TEXT NOT NULL,
    Sequence INTEGER NOT NULL,
    ItemType TEXT NOT NULL DEFAULT 'None',
    BaseItemTypeID TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL DEFAULT '',
    ResourcePurity TEXT NOT NULL DEFAULT '',
    Quantity INTEGER NOT NULL DEFAULT 0,
    Delivered INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (DeliveryPlanUUID, StopSequence, Direction, Sequence),
    FOREIGN KEY (DeliveryPlanUUID, StopSequence) REFERENCES DeliveryPlanStops(DeliveryPlanUUID, Sequence) ON DELETE CASCADE
);

-- MARKET
CREATE TABLE IF NOT EXISTS MarketListings (
    UUID TEXT PRIMARY KEY,
    OwnerUUID TEXT NOT NULL DEFAULT '',
    StationUUID TEXT NOT NULL DEFAULT '',
    ItemType TEXT NOT NULL DEFAULT 'None',
    ItemReferenceID TEXT NOT NULL DEFAULT '',
    ItemName TEXT NOT NULL DEFAULT '',
    Quantity INTEGER NOT NULL DEFAULT 0,
    PricePerUnit REAL NOT NULL DEFAULT 0,
    CurrentHP INTEGER NOT NULL DEFAULT 0,
    MaxHP INTEGER NOT NULL DEFAULT 0,
    MaxRepairPercent REAL NOT NULL DEFAULT 0,
    MarketId INTEGER,
    BuyOrder INTEGER NOT NULL DEFAULT 0,
    BaseItemTypeID TEXT NOT NULL DEFAULT '',
    ResourcePurity TEXT NOT NULL DEFAULT '',
    GameTypeCode TEXT NOT NULL DEFAULT '',
    GameTypeId INTEGER,
    GameSubTypeId TEXT NOT NULL DEFAULT '',
    LocationName TEXT NOT NULL DEFAULT '',
    SystemId INTEGER,
    SystemName TEXT NOT NULL DEFAULT '',
    GameLocationId INTEGER,
    AmountRemaining INTEGER,
    AmountOriginal INTEGER,
    AmountSold INTEGER,
    EscrowRemaining REAL,
    SalesTaxEstimate REAL,
    ValueRemaining REAL,
    Evolution INTEGER,
    HealthPercentage REAL,
    SellerName TEXT NOT NULL DEFAULT '',
    SellerFactionTag TEXT NOT NULL DEFAULT '',
    PrivateSale INTEGER NOT NULL DEFAULT 0,
    BuyerName TEXT NOT NULL DEFAULT '',
    BuyerFactionTag TEXT NOT NULL DEFAULT '',
    IsOutbid INTEGER NOT NULL DEFAULT 0,
    IsUndercut INTEGER NOT NULL DEFAULT 0,
    PlacedDT TEXT NOT NULL DEFAULT '',
    ExpiresDT TEXT NOT NULL DEFAULT '',
    CompetitorForMarketId INTEGER,
    SyncedByCharacterUUID TEXT NOT NULL DEFAULT '',
    SyncTimestamp TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS MarketTransactions (
    UUID TEXT PRIMARY KEY,
    OwnerUUID TEXT NOT NULL DEFAULT '',
    TransactionType TEXT NOT NULL DEFAULT 'Buy',
    ItemType TEXT NOT NULL DEFAULT 'None',
    ItemReferenceID TEXT NOT NULL DEFAULT '',
    ItemName TEXT NOT NULL DEFAULT '',
    Quantity INTEGER NOT NULL DEFAULT 0,
    PricePerUnit REAL NOT NULL DEFAULT 0,
    TotalPrice REAL NOT NULL DEFAULT 0,
    Counterparty TEXT NOT NULL DEFAULT '',
    CounterpartyFaction TEXT NOT NULL DEFAULT '',
    StationUUID TEXT NOT NULL DEFAULT '',
    Timestamp TEXT NOT NULL DEFAULT '',
    Notes TEXT NOT NULL DEFAULT '',
    ListingUUID TEXT NOT NULL DEFAULT '',
    CurrentHP INTEGER NOT NULL DEFAULT 0,
    MaxHP INTEGER NOT NULL DEFAULT 0,
    MaxRepairPercent REAL NOT NULL DEFAULT 0
);
";

        private const string PricingBuildStockSchema = @"
-- PRICING PLAN
CREATE TABLE IF NOT EXISTS PricingPlans (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    OwnerUUID TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT '',
    FixedCostPerItem REAL NOT NULL DEFAULT 0,
    HourlyCostRate REAL NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS PricingPlanPrices (
    PricingPlanUUID TEXT NOT NULL REFERENCES PricingPlans(UUID) ON DELETE CASCADE,
    ResourceName TEXT NOT NULL,
    Price REAL NOT NULL DEFAULT 0,
    PRIMARY KEY (PricingPlanUUID, ResourceName)
);

-- BUILD PLAN
CREATE TABLE IF NOT EXISTS BuildPlans (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    OwnerUUID TEXT NOT NULL DEFAULT '',
    ColonyUUID TEXT NOT NULL DEFAULT '',
    BlueprintUUID TEXT NOT NULL DEFAULT '',
    Quantity INTEGER NOT NULL DEFAULT 0,
    Priority INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS BuildItems (
    BuildPlanUUID TEXT NOT NULL REFERENCES BuildPlans(UUID) ON DELETE CASCADE,
    Sequence INTEGER NOT NULL,
    ResourceName TEXT NOT NULL DEFAULT '',
    Quantity INTEGER NOT NULL DEFAULT 0,
    Fulfilled INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (BuildPlanUUID, Sequence)
);

-- STOCK PLAN
CREATE TABLE IF NOT EXISTS StockPlans (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    OwnerUUID TEXT NOT NULL DEFAULT '',
    ColonyUUID TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS StockTargets (
    StockPlanUUID TEXT NOT NULL REFERENCES StockPlans(UUID) ON DELETE CASCADE,
    Sequence INTEGER NOT NULL,
    ResourceName TEXT NOT NULL DEFAULT '',
    TargetQuantity INTEGER NOT NULL DEFAULT 0,
    Priority INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (StockPlanUUID, Sequence)
);

-- STOCK PROFILE
CREATE TABLE IF NOT EXISTS StockProfiles (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    OwnerUUID TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS StockProfileEntries (
    StockProfileUUID TEXT NOT NULL REFERENCES StockProfiles(UUID) ON DELETE CASCADE,
    Sequence INTEGER NOT NULL,
    ResourceName TEXT NOT NULL DEFAULT '',
    MinQuantity INTEGER NOT NULL DEFAULT 0,
    MaxQuantity INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (StockProfileUUID, Sequence)
);
";

        private const string RemainingPlayerEntitySchema = @"
-- SUPPLY CHAIN
CREATE TABLE IF NOT EXISTS SupplyChains (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    OwnerUUID TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS SupplyChainStages (
    SupplyChainUUID TEXT NOT NULL REFERENCES SupplyChains(UUID) ON DELETE CASCADE,
    Sequence INTEGER NOT NULL,
    ColonyUUID TEXT NOT NULL DEFAULT '',
    BlueprintUUID TEXT NOT NULL DEFAULT '',
    OutputItemType TEXT NOT NULL DEFAULT '',
    OutputQuantity INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (SupplyChainUUID, Sequence)
);

-- ASTEROID
CREATE TABLE IF NOT EXISTS Asteroids (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    OwnerUUID TEXT NOT NULL DEFAULT '',
    SystemName TEXT NOT NULL DEFAULT '',
    SystemId INTEGER NOT NULL DEFAULT 0,
    SystemObjectId INTEGER NOT NULL DEFAULT 0,
    GameApiAsteroidId INTEGER
);

CREATE TABLE IF NOT EXISTS AsteroidReserves (
    AsteroidUUID TEXT NOT NULL REFERENCES Asteroids(UUID) ON DELETE CASCADE,
    Sequence INTEGER NOT NULL,
    ResourceName TEXT NOT NULL DEFAULT '',
    Purity TEXT NOT NULL DEFAULT '',
    MaxReserve INTEGER NOT NULL DEFAULT 0,
    CurrentReserve INTEGER,
    ResetTimestamp TEXT,
    PRIMARY KEY (AsteroidUUID, Sequence)
);

-- STATION
CREATE TABLE IF NOT EXISTS Stations (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    OwnerUUID TEXT NOT NULL DEFAULT '',
    SystemName TEXT NOT NULL DEFAULT '',
    SystemId INTEGER NOT NULL DEFAULT 0,
    SystemObjectId INTEGER NOT NULL DEFAULT 0,
    GameLocationId INTEGER
);

CREATE TABLE IF NOT EXISTS StationComponents (
    StationUUID TEXT NOT NULL REFERENCES Stations(UUID) ON DELETE CASCADE,
    Sequence INTEGER NOT NULL,
    SlotType TEXT NOT NULL DEFAULT '',
    BlueprintUUID TEXT NOT NULL DEFAULT '',
    CurrentHP INTEGER NOT NULL DEFAULT 0,
    MaxHP INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (StationUUID, Sequence)
);

-- FACTION (per-character contacts)
CREATE TABLE IF NOT EXISTS Factions (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    OwnerUUID TEXT NOT NULL DEFAULT '',
    Tag TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT ''
);

-- EXTERNAL CHARACTER (contacts)
CREATE TABLE IF NOT EXISTS ExternalCharacters (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    OwnerUUID TEXT NOT NULL DEFAULT '',
    FactionUUID TEXT NOT NULL DEFAULT '',
    FactionName TEXT NOT NULL DEFAULT '',
    CharacterId INTEGER NOT NULL DEFAULT 0,
    Notes TEXT NOT NULL DEFAULT ''
);

-- WAREHOUSE OVERFLOW RULES
CREATE TABLE IF NOT EXISTS WarehouseOverflowRules (
    UUID TEXT PRIMARY KEY,
    OwnerUUID TEXT NOT NULL DEFAULT '',
    ColonyUUID TEXT NOT NULL DEFAULT '',
    ResourceName TEXT NOT NULL DEFAULT '',
    RuleType INTEGER NOT NULL DEFAULT 0,
    Threshold INTEGER NOT NULL DEFAULT 0,
    DestinationColonyUUID TEXT NOT NULL DEFAULT ''
);

-- MAIL
CREATE TABLE IF NOT EXISTS MailMessages (
    MailId INTEGER NOT NULL,
    OwnerUUID TEXT NOT NULL DEFAULT '',
    CharacterIdFrom INTEGER NOT NULL DEFAULT 0,
    FromName TEXT NOT NULL DEFAULT '',
    CharacterIdTo INTEGER NOT NULL DEFAULT 0,
    ToName TEXT NOT NULL DEFAULT '',
    SentTime TEXT NOT NULL DEFAULT '',
    Subject TEXT NOT NULL DEFAULT '',
    MailRead INTEGER NOT NULL DEFAULT 0,
    MailType TEXT,
    MailContent TEXT NOT NULL DEFAULT '',
    LocalRead INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (OwnerUUID, MailId)
);

-- BANKING
CREATE TABLE IF NOT EXISTS BankingTransactions (
    UUID TEXT PRIMARY KEY,
    OwnerUUID TEXT NOT NULL DEFAULT '',
    TransactionDateTime TEXT NOT NULL DEFAULT '',
    CreditChange REAL NOT NULL DEFAULT 0,
    OldBalance REAL NOT NULL DEFAULT 0,
    NewBalance REAL NOT NULL DEFAULT 0,
    TransactionType INTEGER NOT NULL DEFAULT 0,
    Detail TEXT NOT NULL DEFAULT '',
    CharacterId INTEGER,
    SystemObjectId INTEGER,
    SystemId INTEGER,
    IsManualEntry INTEGER NOT NULL DEFAULT 0
);
";

        private const string ServerGlobalSchema = @"
-- SERVER FACTIONS
CREATE TABLE IF NOT EXISTS ServerFactions (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT '',
    Metadata_LastModifiedUtc TEXT,
    Metadata_ModifiedByTokenId TEXT
);

CREATE TABLE IF NOT EXISTS ServerFactionLeaders (
    FactionUUID TEXT NOT NULL REFERENCES ServerFactions(UUID) ON DELETE CASCADE,
    CharacterUUID TEXT NOT NULL,
    PRIMARY KEY (FactionUUID, CharacterUUID)
);

-- SERVER CHARACTERS
CREATE TABLE IF NOT EXISTS ServerCharacters (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    FactionUUID TEXT,
    Metadata_LastModifiedUtc TEXT,
    Metadata_ModifiedByTokenId TEXT
);

-- API TOKENS
CREATE TABLE IF NOT EXISTS ApiTokens (
    Id TEXT PRIMARY KEY,
    TokenHash TEXT NOT NULL DEFAULT '',
    CharacterUUID TEXT,
    Role INTEGER NOT NULL DEFAULT 0,
    FactionUUID TEXT,
    CreatedUtc TEXT NOT NULL DEFAULT '',
    LastUsedUtc TEXT,
    IsRevoked INTEGER NOT NULL DEFAULT 0,
    RateLimits_RequestsPerMinute INTEGER NOT NULL DEFAULT 300
);

-- MEMBERSHIP ACTIONS
CREATE TABLE IF NOT EXISTS MembershipActions (
    Id TEXT PRIMARY KEY,
    FactionUUID TEXT NOT NULL DEFAULT '',
    CharacterUUID TEXT NOT NULL DEFAULT '',
    Type INTEGER NOT NULL DEFAULT 0,
    CreatedUtc TEXT NOT NULL DEFAULT '',
    ExpiresUtc TEXT NOT NULL DEFAULT ''
);

-- STAR SYSTEMS
CREATE TABLE IF NOT EXISTS StarSystems (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    X REAL NOT NULL DEFAULT 0,
    Y REAL NOT NULL DEFAULT 0,
    Quadrant INTEGER NOT NULL DEFAULT 0,
    Sector INTEGER NOT NULL DEFAULT 0,
    Region INTEGER NOT NULL DEFAULT 0,
    Locality INTEGER NOT NULL DEFAULT 0,
    SpectralClass TEXT NOT NULL DEFAULT '',
    FactionId INTEGER NOT NULL DEFAULT 0,
    FactionName TEXT NOT NULL DEFAULT '',
    FactionColor TEXT NOT NULL DEFAULT '',
    HasOrbital INTEGER NOT NULL DEFAULT 0,
    HasSpaceport INTEGER NOT NULL DEFAULT 0,
    HasStarbase INTEGER NOT NULL DEFAULT 0
);

-- SHARING RULES
CREATE TABLE IF NOT EXISTS SharingRules (
    Id TEXT PRIMARY KEY,
    OwnerCharacterUUID TEXT NOT NULL DEFAULT '',
    TargetUUID TEXT NOT NULL DEFAULT '',
    TargetType INTEGER NOT NULL DEFAULT 0,
    DataType TEXT,
    EntityUUID TEXT
);

-- CHARACTER PREFERENCES
CREATE TABLE IF NOT EXISTS CharacterPreferences (
    CharacterUUID TEXT PRIMARY KEY,
    ServerProcessing INTEGER NOT NULL DEFAULT 0
);
";

        private const string PermissionSchema = @"
-- FACTION CAPABILITIES
CREATE TABLE IF NOT EXISTS FactionCapabilities (
    UUID TEXT PRIMARY KEY,
    FactionUUID TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT ''
);

-- FACTION CLEARANCE LEVELS
CREATE TABLE IF NOT EXISTS FactionClearanceLevels (
    UUID TEXT PRIMARY KEY,
    FactionUUID TEXT NOT NULL DEFAULT '',
    Level INTEGER NOT NULL DEFAULT 0,
    Name TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT ''
);

-- FACTION PERMISSION GROUPS
CREATE TABLE IF NOT EXISTS FactionPermissionGroups (
    UUID TEXT PRIMARY KEY,
    FactionUUID TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT '',
    DefaultClearanceLevelUUID TEXT NOT NULL DEFAULT ''
);

-- FACTION GROUP CAPABILITIES (junction)
CREATE TABLE IF NOT EXISTS FactionGroupCapabilities (
    GroupUUID TEXT NOT NULL,
    CapabilityUUID TEXT NOT NULL,
    PRIMARY KEY (GroupUUID, CapabilityUUID)
);

-- FACTION GROUP SHARING RULES
CREATE TABLE IF NOT EXISTS FactionGroupSharingRules (
    UUID TEXT PRIMARY KEY,
    GroupUUID TEXT NOT NULL DEFAULT '',
    DataType TEXT,
    EntityUUID TEXT,
    MinClearanceLevelUUID TEXT NOT NULL DEFAULT ''
);

-- FACTION MEMBER PERMISSIONS
CREATE TABLE IF NOT EXISTS FactionMemberPermissions (
    FactionUUID TEXT NOT NULL,
    CharacterUUID TEXT NOT NULL,
    GroupUUID TEXT,
    ClearanceLevelUUID TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (FactionUUID, CharacterUUID)
);

-- FACTION MEMBER CAPABILITIES (junction)
CREATE TABLE IF NOT EXISTS FactionMemberCapabilities (
    FactionUUID TEXT NOT NULL,
    CharacterUUID TEXT NOT NULL,
    CapabilityUUID TEXT NOT NULL,
    PRIMARY KEY (FactionUUID, CharacterUUID, CapabilityUUID)
);

-- CHARACTER CAPABILITIES
CREATE TABLE IF NOT EXISTS CharacterCapabilities (
    UUID TEXT PRIMARY KEY,
    OwnerCharacterUUID TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT ''
);

-- CHARACTER CLEARANCE LEVELS
CREATE TABLE IF NOT EXISTS CharacterClearanceLevels (
    UUID TEXT PRIMARY KEY,
    OwnerCharacterUUID TEXT NOT NULL DEFAULT '',
    Level INTEGER NOT NULL DEFAULT 0,
    Name TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT ''
);

-- CHARACTER PERMISSION GROUPS
CREATE TABLE IF NOT EXISTS CharacterPermissionGroups (
    UUID TEXT PRIMARY KEY,
    OwnerCharacterUUID TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT '',
    DefaultClearanceLevelUUID TEXT NOT NULL DEFAULT ''
);

-- CHARACTER GROUP CAPABILITIES (junction)
CREATE TABLE IF NOT EXISTS CharacterGroupCapabilities (
    GroupUUID TEXT NOT NULL,
    CapabilityUUID TEXT NOT NULL,
    PRIMARY KEY (GroupUUID, CapabilityUUID)
);

-- CHARACTER GROUP SHARING RULES
CREATE TABLE IF NOT EXISTS CharacterGroupSharingRules (
    UUID TEXT PRIMARY KEY,
    GroupUUID TEXT NOT NULL DEFAULT '',
    DataType TEXT,
    EntityUUID TEXT
);

-- CHARACTER GRANTEE PERMISSIONS
CREATE TABLE IF NOT EXISTS CharacterGranteePermissions (
    OwnerCharacterUUID TEXT NOT NULL,
    GranteeType INTEGER NOT NULL DEFAULT 0,
    GranteeUUID TEXT NOT NULL,
    GroupUUID TEXT,
    ClearanceLevelUUID TEXT,
    PRIMARY KEY (OwnerCharacterUUID, GranteeUUID)
);

-- CHARACTER GRANTEE CAPABILITIES (junction)
CREATE TABLE IF NOT EXISTS CharacterGranteeCapabilities (
    OwnerCharacterUUID TEXT NOT NULL,
    GranteeUUID TEXT NOT NULL,
    CapabilityUUID TEXT NOT NULL,
    PRIMARY KEY (OwnerCharacterUUID, GranteeUUID, CapabilityUUID)
);
";

        private const string IntelAuditBaselineSchema = @"
-- INTEL COMMENTS
CREATE TABLE IF NOT EXISTS IntelComments (
    UUID TEXT PRIMARY KEY,
    TargetCharacterUUID TEXT NOT NULL DEFAULT '',
    SubmitterCharacterUUID TEXT NOT NULL DEFAULT '',
    Text TEXT NOT NULL DEFAULT '',
    CreatedUtc TEXT NOT NULL DEFAULT ''
);

-- INTEL COMMENT FACTION SHARES
CREATE TABLE IF NOT EXISTS IntelCommentFactionShares (
    UUID TEXT PRIMARY KEY,
    IntelCommentUUID TEXT NOT NULL REFERENCES IntelComments(UUID) ON DELETE CASCADE,
    FactionUUID TEXT NOT NULL DEFAULT '',
    ClassificationLevelUUID TEXT,
    ClassifiedByCharacterUUID TEXT,
    SharedUtc TEXT NOT NULL DEFAULT '',
    ClassifiedUtc TEXT
);

-- PERMISSION AUDIT ENTRIES
CREATE TABLE IF NOT EXISTS PermissionAuditEntries (
    UUID TEXT PRIMARY KEY,
    Timestamp TEXT NOT NULL DEFAULT '',
    ActorCharacterUUID TEXT NOT NULL DEFAULT '',
    TargetCharacterUUID TEXT NOT NULL DEFAULT '',
    ActionType INTEGER NOT NULL DEFAULT 0,
    OldValue TEXT NOT NULL DEFAULT '',
    NewValue TEXT NOT NULL DEFAULT ''
);

-- BASELINE GAME CONSTANTS
CREATE TABLE IF NOT EXISTS BaselineGameConstants (
    Id INTEGER PRIMARY KEY DEFAULT 1,
    DataVersion INTEGER NOT NULL DEFAULT 0,
    LastUpdatedUtc TEXT
);

-- BLUEPRINT TYPES
CREATE TABLE IF NOT EXISTS BlueprintTypes (
    Name TEXT PRIMARY KEY,
    Category TEXT NOT NULL DEFAULT '',
    TechLevel TEXT NOT NULL DEFAULT '',
    BaseVolume REAL NOT NULL DEFAULT 0,
    BaseMass REAL NOT NULL DEFAULT 0
);

-- BLUEPRINT TYPE PROPERTIES (default properties for a blueprint type)
CREATE TABLE IF NOT EXISTS BlueprintTypeProperties (
    BlueprintTypeName TEXT NOT NULL REFERENCES BlueprintTypes(Name) ON DELETE CASCADE,
    Key TEXT NOT NULL,
    DefaultValue TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (BlueprintTypeName, Key)
);

-- BLUEPRINT TYPE RESEARCHABLE PROPERTIES
CREATE TABLE IF NOT EXISTS BlueprintTypeResearchableProperties (
    BlueprintTypeName TEXT NOT NULL REFERENCES BlueprintTypes(Name) ON DELETE CASCADE,
    Key TEXT NOT NULL,
    MinValue TEXT NOT NULL DEFAULT '',
    MaxValue TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (BlueprintTypeName, Key)
);

-- SHIP CLASSES
CREATE TABLE IF NOT EXISTS ShipClasses (
    Name TEXT PRIMARY KEY,
    HullType TEXT NOT NULL DEFAULT '',
    CargoCapacity INTEGER NOT NULL DEFAULT 0,
    HopperCapacity INTEGER NOT NULL DEFAULT 0,
    ComponentSlots INTEGER NOT NULL DEFAULT 0,
    BaseHP INTEGER NOT NULL DEFAULT 0
);

-- TECH LEVELS
CREATE TABLE IF NOT EXISTS TechLevels (
    Name TEXT PRIMARY KEY,
    Level INTEGER NOT NULL DEFAULT 0,
    Description TEXT NOT NULL DEFAULT ''
);

-- COMMODITIES
CREATE TABLE IF NOT EXISTS Commodities (
    Name TEXT PRIMARY KEY,
    Category TEXT NOT NULL DEFAULT '',
    BaseVolume REAL NOT NULL DEFAULT 0,
    BaseMass REAL NOT NULL DEFAULT 0,
    BaseValue REAL NOT NULL DEFAULT 0
);

-- COMMODITY RESOURCES (construction recipe)
CREATE TABLE IF NOT EXISTS CommodityResources (
    CommodityName TEXT NOT NULL REFERENCES Commodities(Name) ON DELETE CASCADE,
    ResourceName TEXT NOT NULL,
    Quantity INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (CommodityName, ResourceName)
);

-- REFINING RECIPES
CREATE TABLE IF NOT EXISTS RefiningRecipes (
    Id TEXT PRIMARY KEY,
    InputResource TEXT NOT NULL DEFAULT '',
    InputPurity TEXT NOT NULL DEFAULT '',
    OutputResource TEXT NOT NULL DEFAULT '',
    OutputPurity TEXT NOT NULL DEFAULT '',
    OutputQuantity INTEGER NOT NULL DEFAULT 0,
    ProcessingTime INTEGER NOT NULL DEFAULT 0
);

-- RESEARCH TIMES
CREATE TABLE IF NOT EXISTS ResearchTimes (
    Id TEXT PRIMARY KEY,
    BlueprintType TEXT NOT NULL DEFAULT '',
    TechLevel TEXT NOT NULL DEFAULT '',
    PropertyKey TEXT NOT NULL DEFAULT '',
    BaseTimeMinutes INTEGER NOT NULL DEFAULT 0
);

-- PROPERTY TYPE DEFINITIONS
CREATE TABLE IF NOT EXISTS PropertyTypeDefinitions (
    Key TEXT PRIMARY KEY,
    DisplayName TEXT NOT NULL DEFAULT '',
    Category TEXT NOT NULL DEFAULT '',
    DataType TEXT NOT NULL DEFAULT 'string',
    Unit TEXT NOT NULL DEFAULT ''
);
";

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteBackend"/> class.
        /// </summary>
        /// <param name="config">Configuration containing the database file path.</param>
        public SqliteBackend(StorageBackendConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _databasePath = config.ConnectionString ?? string.Empty;
            _connectionString = $"Data Source={_databasePath}";
        }

        // ═══════════════════════════════════════════════════════════
        // Lifecycle
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task InitializeAsync(CancellationToken ct = default)
        {
            using (var conn = OpenConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "CREATE TABLE IF NOT EXISTS _metadata (Key TEXT PRIMARY KEY, Value TEXT NOT NULL);";
                    cmd.ExecuteNonQuery();
                }

                int version = GetSchemaVersion(conn);
                if (version == 0)
                {
                    ExecuteSchema(conn);
                    SetSchemaVersion(conn, CurrentSchemaVersion);
                    Log.Info("SQLite database initialized with schema version {0}", CurrentSchemaVersion);
                }
                else
                {
                    Log.Debug("SQLite database already at schema version {0}", version);
                }
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> ValidateConnectionAsync(CancellationToken ct = default)
        {
            try
            {
                using (var conn = OpenConnection())
                {
                    return Task.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "SQLite connection validation failed");
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc/>
        public StorageInfo GetStorageInfo()
        {
            return new StorageInfo
            {
                BackendType = "Sqlite",
                Location = _databasePath,
            };
        }

        // ═══════════════════════════════════════════════════════════
        // Server Factions
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<ServerFaction> GetFactionAsync(string uuid)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT UUID, Name, Description, Metadata_LastModifiedUtc, Metadata_ModifiedByTokenId FROM ServerFactions WHERE UUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", uuid);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var faction = ReadServerFaction(reader);
                        faction.LeaderCharacterUUIDs = ReadFactionLeaders(conn, uuid);
                        return Task.FromResult(faction);
                    }
                }
            }

            return Task.FromResult<ServerFaction>(null);
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ServerFaction>> GetAllFactionsAsync()
        {
            var results = new List<ServerFaction>();
            using (var conn = OpenConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT UUID, Name, Description, Metadata_LastModifiedUtc, Metadata_ModifiedByTokenId FROM ServerFactions";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadServerFaction(reader));
                        }
                    }
                }

                foreach (var faction in results)
                {
                    faction.LeaderCharacterUUIDs = ReadFactionLeaders(conn, faction.UUID);
                }
            }

            return Task.FromResult<IReadOnlyList<ServerFaction>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertFactionAsync(ServerFaction faction)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT OR REPLACE INTO ServerFactions (UUID, Name, Description, Metadata_LastModifiedUtc, Metadata_ModifiedByTokenId)
                                       VALUES (@uuid, @name, @desc, @modUtc, @modBy)";
                    cmd.Parameters.AddWithValue("@uuid", faction.UUID);
                    cmd.Parameters.AddWithValue("@name", faction.Name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@desc", faction.Description ?? string.Empty);
                    cmd.Parameters.AddWithValue("@modUtc", faction.Metadata?.LastModifiedUtc.ToString("O") ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@modBy", (object)faction.Metadata?.ModifiedByTokenId ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                using (var delCmd = conn.CreateCommand())
                {
                    delCmd.Transaction = tx;
                    delCmd.CommandText = "DELETE FROM ServerFactionLeaders WHERE FactionUUID = @uuid";
                    delCmd.Parameters.AddWithValue("@uuid", faction.UUID);
                    delCmd.ExecuteNonQuery();
                }

                if (faction.LeaderCharacterUUIDs != null)
                {
                    foreach (var leaderUUID in faction.LeaderCharacterUUIDs)
                    {
                        using (var insCmd = conn.CreateCommand())
                        {
                            insCmd.Transaction = tx;
                            insCmd.CommandText = "INSERT INTO ServerFactionLeaders (FactionUUID, CharacterUUID) VALUES (@fid, @cid)";
                            insCmd.Parameters.AddWithValue("@fid", faction.UUID);
                            insCmd.Parameters.AddWithValue("@cid", leaderUUID);
                            insCmd.ExecuteNonQuery();
                        }
                    }
                }

                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteFactionAsync(string uuid)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM ServerFactions WHERE UUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", uuid);
                cmd.ExecuteNonQuery();
            }

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Server Characters
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<ServerCharacter> GetCharacterAsync(string uuid)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT UUID, Name, FactionUUID, Metadata_LastModifiedUtc, Metadata_ModifiedByTokenId FROM ServerCharacters WHERE UUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", uuid);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return Task.FromResult(ReadServerCharacter(reader));
                    }
                }
            }

            return Task.FromResult<ServerCharacter>(null);
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ServerCharacter>> GetAllCharactersAsync()
        {
            var results = new List<ServerCharacter>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT UUID, Name, FactionUUID, Metadata_LastModifiedUtc, Metadata_ModifiedByTokenId FROM ServerCharacters";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(ReadServerCharacter(reader));
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<ServerCharacter>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertCharacterAsync(ServerCharacter character)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO ServerCharacters (UUID, Name, FactionUUID, Metadata_LastModifiedUtc, Metadata_ModifiedByTokenId)
                                   VALUES (@uuid, @name, @factionUUID, @modUtc, @modBy)";
                cmd.Parameters.AddWithValue("@uuid", character.UUID);
                cmd.Parameters.AddWithValue("@name", character.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@factionUUID", (object)character.FactionUUID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@modUtc", character.Metadata?.LastModifiedUtc.ToString("O") ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@modBy", (object)character.Metadata?.ModifiedByTokenId ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteCharacterAsync(string uuid)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM ServerCharacters WHERE UUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", uuid);
                cmd.ExecuteNonQuery();
            }

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Global Data
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<string> GetGlobalDataAsync(string dataType)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Value FROM _metadata WHERE Key = @key";
                cmd.Parameters.AddWithValue("@key", "global_" + dataType);
                var result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    return Task.FromResult<string>(null);
                }

                return Task.FromResult(result.ToString());
            }
        }

        /// <inheritdoc/>
        public Task UpsertGlobalDataAsync(string dataType, string json)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT OR REPLACE INTO _metadata (Key, Value) VALUES (@key, @val)";
                cmd.Parameters.AddWithValue("@key", "global_" + dataType);
                cmd.Parameters.AddWithValue("@val", json ?? string.Empty);
                cmd.ExecuteNonQuery();
            }

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Star Systems
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<StarSystem>> GetAllStarSystemsAsync()
        {
            var results = new List<StarSystem>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, Name, X, Y, Quadrant, Sector, Region, Locality, SpectralClass, FactionId, FactionName, FactionColor, HasOrbital, HasSpaceport, HasStarbase FROM StarSystems";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(ReadStarSystem(reader));
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<StarSystem>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertStarSystemsAsync(IReadOnlyList<StarSystem> systems)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                using (var delCmd = conn.CreateCommand())
                {
                    delCmd.Transaction = tx;
                    delCmd.CommandText = "DELETE FROM StarSystems";
                    delCmd.ExecuteNonQuery();
                }

                foreach (var system in systems)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = @"INSERT INTO StarSystems (Id, Name, X, Y, Quadrant, Sector, Region, Locality, SpectralClass, FactionId, FactionName, FactionColor, HasOrbital, HasSpaceport, HasStarbase)
                                           VALUES (@id, @name, @x, @y, @quadrant, @sector, @region, @locality, @spectral, @factionId, @factionName, @factionColor, @hasOrbital, @hasSpaceport, @hasStarbase)";
                        cmd.Parameters.AddWithValue("@id", system.Id);
                        cmd.Parameters.AddWithValue("@name", system.Name ?? string.Empty);
                        cmd.Parameters.AddWithValue("@x", (double)system.X);
                        cmd.Parameters.AddWithValue("@y", (double)system.Y);
                        cmd.Parameters.AddWithValue("@quadrant", system.Quadrant);
                        cmd.Parameters.AddWithValue("@sector", system.Sector);
                        cmd.Parameters.AddWithValue("@region", system.Region);
                        cmd.Parameters.AddWithValue("@locality", system.Locality);
                        cmd.Parameters.AddWithValue("@spectral", system.SpectralClass ?? string.Empty);
                        cmd.Parameters.AddWithValue("@factionId", system.FactionId);
                        cmd.Parameters.AddWithValue("@factionName", system.FactionName ?? string.Empty);
                        cmd.Parameters.AddWithValue("@factionColor", system.FactionColor ?? string.Empty);
                        cmd.Parameters.AddWithValue("@hasOrbital", system.HasOrbital ? 1 : 0);
                        cmd.Parameters.AddWithValue("@hasSpaceport", system.HasSpaceport ? 1 : 0);
                        cmd.Parameters.AddWithValue("@hasStarbase", system.HasStarbase ? 1 : 0);
                        cmd.ExecuteNonQuery();
                    }
                }

                tx.Commit();
            }

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Colony Summaries
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<ColonySummary>> GetColonySummariesForSystemAsync(int systemId)
        {
            var results = new List<ColonySummary>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ColonyName, ColonySize, PlanetName FROM Colonies WHERE SystemId = @systemId";
                cmd.Parameters.AddWithValue("@systemId", systemId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new ColonySummary
                        {
                            ColonyName = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                            Size = reader.GetInt32(1),
                            PlanetName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<ColonySummary>>(results);
        }

        // ═══════════════════════════════════════════════════════════
        // API Tokens
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<ApiToken> FindTokenByHashAsync(string tokenHash)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, TokenHash, CharacterUUID, Role, FactionUUID, CreatedUtc, LastUsedUtc, IsRevoked, RateLimits_RequestsPerMinute FROM ApiTokens WHERE TokenHash = @hash";
                cmd.Parameters.AddWithValue("@hash", tokenHash);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return Task.FromResult(ReadApiToken(reader));
                    }
                }
            }

            return Task.FromResult<ApiToken>(null);
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ApiToken>> GetAllTokensAsync()
        {
            var results = new List<ApiToken>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, TokenHash, CharacterUUID, Role, FactionUUID, CreatedUtc, LastUsedUtc, IsRevoked, RateLimits_RequestsPerMinute FROM ApiTokens";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(ReadApiToken(reader));
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<ApiToken>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertTokenAsync(ApiToken token)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO ApiTokens (Id, TokenHash, CharacterUUID, Role, FactionUUID, CreatedUtc, LastUsedUtc, IsRevoked, RateLimits_RequestsPerMinute)
                                   VALUES (@id, @hash, @charUUID, @role, @factionUUID, @created, @lastUsed, @revoked, @rpm)";
                cmd.Parameters.AddWithValue("@id", token.Id);
                cmd.Parameters.AddWithValue("@hash", token.TokenHash ?? string.Empty);
                cmd.Parameters.AddWithValue("@charUUID", (object)token.CharacterUUID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@role", (int)token.Role);
                cmd.Parameters.AddWithValue("@factionUUID", (object)token.FactionUUID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@created", token.CreatedUtc.ToString("O"));
                cmd.Parameters.AddWithValue("@lastUsed", token.LastUsedUtc.HasValue ? (object)token.LastUsedUtc.Value.ToString("O") : DBNull.Value);
                cmd.Parameters.AddWithValue("@revoked", token.IsRevoked ? 1 : 0);
                cmd.Parameters.AddWithValue("@rpm", token.RateLimits?.RequestsPerMinute ?? 300);
                cmd.ExecuteNonQuery();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteTokenAsync(string id)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM ApiTokens WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Membership Actions
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<MembershipAction>> GetFactionActionsAsync(string factionUUID)
        {
            var results = new List<MembershipAction>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, FactionUUID, CharacterUUID, Type, CreatedUtc, ExpiresUtc FROM MembershipActions WHERE FactionUUID = @fid";
                cmd.Parameters.AddWithValue("@fid", factionUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(ReadMembershipAction(reader));
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<MembershipAction>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertMembershipActionAsync(MembershipAction action)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO MembershipActions (Id, FactionUUID, CharacterUUID, Type, CreatedUtc, ExpiresUtc)
                                   VALUES (@id, @fid, @cid, @type, @created, @expires)";
                cmd.Parameters.AddWithValue("@id", action.Id);
                cmd.Parameters.AddWithValue("@fid", action.FactionUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@cid", action.CharacterUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@type", (int)action.Type);
                cmd.Parameters.AddWithValue("@created", action.CreatedUtc.ToString("O"));
                cmd.Parameters.AddWithValue("@expires", action.ExpiresUtc.ToString("O"));
                cmd.ExecuteNonQuery();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteMembershipActionAsync(string id)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM MembershipActions WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteExpiredActionsAsync(DateTime cutoff)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM MembershipActions WHERE ExpiresUtc < @cutoff";
                cmd.Parameters.AddWithValue("@cutoff", cutoff.ToString("O"));
                cmd.ExecuteNonQuery();
            }

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Sharing Rules
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<SharingRule>> GetSharingRulesForCharacterAsync(string characterUUID)
        {
            var results = new List<SharingRule>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, OwnerCharacterUUID, TargetUUID, TargetType, DataType, EntityUUID FROM SharingRules WHERE OwnerCharacterUUID = @cid";
                cmd.Parameters.AddWithValue("@cid", characterUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(ReadSharingRule(reader));
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<SharingRule>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertSharingRulesAsync(string characterUUID, IReadOnlyList<SharingRule> rules)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                using (var delCmd = conn.CreateCommand())
                {
                    delCmd.Transaction = tx;
                    delCmd.CommandText = "DELETE FROM SharingRules WHERE OwnerCharacterUUID = @cid";
                    delCmd.Parameters.AddWithValue("@cid", characterUUID);
                    delCmd.ExecuteNonQuery();
                }

                foreach (var rule in rules)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = @"INSERT INTO SharingRules (Id, OwnerCharacterUUID, TargetUUID, TargetType, DataType, EntityUUID)
                                           VALUES (@id, @cid, @target, @targetType, @dataType, @entityUUID)";
                        cmd.Parameters.AddWithValue("@id", rule.Id);
                        cmd.Parameters.AddWithValue("@cid", characterUUID);
                        cmd.Parameters.AddWithValue("@target", rule.TargetUUID ?? string.Empty);
                        cmd.Parameters.AddWithValue("@targetType", (int)rule.TargetType);
                        cmd.Parameters.AddWithValue("@dataType", (object)rule.DataType ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@entityUUID", (object)rule.EntityUUID ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

                tx.Commit();
            }

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Character Preferences
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<CharacterPreferences> GetCharacterPreferencesAsync(string characterUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT CharacterUUID, ServerProcessing FROM CharacterPreferences WHERE CharacterUUID = @cid";
                cmd.Parameters.AddWithValue("@cid", characterUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return Task.FromResult(new CharacterPreferences
                        {
                            CharacterUUID = reader.GetString(0),
                            ServerProcessing = reader.GetInt32(1) != 0,
                        });
                    }
                }
            }

            return Task.FromResult<CharacterPreferences>(null);
        }

        /// <inheritdoc/>
        public Task UpsertCharacterPreferencesAsync(CharacterPreferences prefs)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO CharacterPreferences (CharacterUUID, ServerProcessing)
                                   VALUES (@cid, @processing)";
                cmd.Parameters.AddWithValue("@cid", prefs.CharacterUUID);
                cmd.Parameters.AddWithValue("@processing", prefs.ServerProcessing ? 1 : 0);
                cmd.ExecuteNonQuery();
            }

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Colony (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Colony>> GetAllColoniesAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<Colony> GetColonyAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertColonyAsync(string characterUUID, Colony entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteColonyAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Blueprint (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Blueprint>> GetAllBlueprintsAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<Blueprint> GetBlueprintAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertBlueprintAsync(string characterUUID, Blueprint entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteBlueprintAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Survey (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Survey>> GetAllSurveysAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<Survey> GetSurveyAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertSurveyAsync(string characterUUID, Survey entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteSurveyAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — PlayerProfile (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<PlayerProfile>> GetAllPlayerProfilesAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<PlayerProfile> GetPlayerProfileAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertPlayerProfileAsync(string characterUUID, PlayerProfile entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeletePlayerProfileAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — DeliveryRoute (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<DeliveryRoute>> GetAllDeliveryRoutesAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<DeliveryRoute> GetDeliveryRouteAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertDeliveryRouteAsync(string characterUUID, DeliveryRoute entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteDeliveryRouteAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — DeliveryPlan (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<DeliveryPlan>> GetAllDeliveryPlansAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<DeliveryPlan> GetDeliveryPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertDeliveryPlanAsync(string characterUUID, DeliveryPlan entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteDeliveryPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Ship (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Ship>> GetAllShipsAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<Ship> GetShipAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertShipAsync(string characterUUID, Ship entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteShipAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — ShipTemplate (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<ShipTemplate>> GetAllShipTemplatesAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<ShipTemplate> GetShipTemplateAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertShipTemplateAsync(string characterUUID, ShipTemplate entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteShipTemplateAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — MarketListing (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<MarketListing>> GetAllMarketListingsAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<MarketListing> GetMarketListingAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertMarketListingAsync(string characterUUID, MarketListing entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteMarketListingAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — MarketTransaction (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<MarketTransaction>> GetAllMarketTransactionsAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<MarketTransaction> GetMarketTransactionAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertMarketTransactionAsync(string characterUUID, MarketTransaction entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteMarketTransactionAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — PricingPlan (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<PricingPlan>> GetAllPricingPlansAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<PricingPlan> GetPricingPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertPricingPlanAsync(string characterUUID, PricingPlan entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeletePricingPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — StockPlan (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<StockPlan>> GetAllStockPlansAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<StockPlan> GetStockPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertStockPlanAsync(string characterUUID, StockPlan entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteStockPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — StockProfile (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<StockProfile>> GetAllStockProfilesAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<StockProfile> GetStockProfileAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertStockProfileAsync(string characterUUID, StockProfile entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteStockProfileAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — BuildPlan (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<BuildPlan>> GetAllBuildPlansAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<BuildPlan> GetBuildPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertBuildPlanAsync(string characterUUID, BuildPlan entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteBuildPlanAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — SupplyChain (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<SupplyChain>> GetAllSupplyChainsAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<SupplyChain> GetSupplyChainAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertSupplyChainAsync(string characterUUID, SupplyChain entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteSupplyChainAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Asteroid (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Asteroid>> GetAllAsteroidsAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<Asteroid> GetAsteroidAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertAsteroidAsync(string characterUUID, Asteroid entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteAsteroidAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Station (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Station>> GetAllStationsAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<Station> GetStationAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertStationAsync(string characterUUID, Station entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteStationAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Faction contacts (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Faction>> GetAllFactionsForCharacterAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<Faction> GetFactionForCharacterAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertFactionForCharacterAsync(string characterUUID, Faction entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteFactionForCharacterAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — ExternalCharacter (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<ExternalCharacter>> GetAllExternalCharactersAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<ExternalCharacter> GetExternalCharacterAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertExternalCharacterAsync(string characterUUID, ExternalCharacter entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteExternalCharacterAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — WarehouseOverflowRule (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<WarehouseOverflowRule>> GetAllWarehouseOverflowRulesAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<WarehouseOverflowRule> GetWarehouseOverflowRuleAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertWarehouseOverflowRuleAsync(string characterUUID, WarehouseOverflowRule entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteWarehouseOverflowRuleAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — MailMessage (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<MailMessage>> GetAllMailMessagesAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<MailMessage> GetMailMessageAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertMailMessageAsync(string characterUUID, MailMessage entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteMailMessageAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — BankingTransaction (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<BankingTransaction>> GetAllBankingTransactionsAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<BankingTransaction> GetBankingTransactionAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertBankingTransactionAsync(string characterUUID, BankingTransaction entity) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteBankingTransactionAsync(string characterUUID, string entityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Faction Permission Entities (stubs — Phase 7)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionCapability>> GetFactionCapabilitiesAsync(string factionUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertFactionCapabilityAsync(FactionCapability capability) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteFactionCapabilityAsync(string factionUUID, string capabilityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionClearanceLevel>> GetFactionClearanceLevelsAsync(string factionUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertFactionClearanceLevelAsync(FactionClearanceLevel level) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteFactionClearanceLevelAsync(string factionUUID, string levelUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionPermissionGroup>> GetFactionGroupsAsync(string factionUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<FactionPermissionGroup> GetFactionGroupAsync(string factionUUID, string groupUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertFactionGroupAsync(FactionPermissionGroup group) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteFactionGroupAsync(string factionUUID, string groupUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionGroupCapability>> GetFactionGroupCapabilitiesAsync(string groupUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task AddFactionGroupCapabilityAsync(FactionGroupCapability item) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task RemoveFactionGroupCapabilityAsync(string groupUUID, string capabilityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionGroupSharingRule>> GetFactionGroupSharingRulesAsync(string groupUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertFactionGroupSharingRuleAsync(FactionGroupSharingRule rule) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteFactionGroupSharingRuleAsync(string groupUUID, string ruleUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<FactionMemberPermissions> GetFactionMemberPermissionsAsync(string factionUUID, string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertFactionMemberPermissionsAsync(FactionMemberPermissions perms) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionMemberPermissions>> GetAllFactionMembersPermissionsAsync(string factionUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionMemberCapability>> GetFactionMemberCapabilitiesAsync(string factionUUID, string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task AddFactionMemberCapabilityAsync(FactionMemberCapability item) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task RemoveFactionMemberCapabilityAsync(string factionUUID, string characterUUID, string capabilityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Character Permission Entities (stubs — Phase 7)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterCapability>> GetCharacterCapabilitiesAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertCharacterCapabilityAsync(CharacterCapability capability) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteCharacterCapabilityAsync(string characterUUID, string capabilityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterClearanceLevel>> GetCharacterClearanceLevelsAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertCharacterClearanceLevelAsync(CharacterClearanceLevel level) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteCharacterClearanceLevelAsync(string characterUUID, string levelUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterPermissionGroup>> GetCharacterGroupsAsync(string characterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<CharacterPermissionGroup> GetCharacterGroupAsync(string characterUUID, string groupUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertCharacterGroupAsync(CharacterPermissionGroup group) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteCharacterGroupAsync(string characterUUID, string groupUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGroupCapability>> GetCharacterGroupCapabilitiesAsync(string groupUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task AddCharacterGroupCapabilityAsync(CharacterGroupCapability item) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task RemoveCharacterGroupCapabilityAsync(string groupUUID, string capabilityUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGroupSharingRule>> GetCharacterGroupSharingRulesAsync(string groupUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertCharacterGroupSharingRuleAsync(CharacterGroupSharingRule rule) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteCharacterGroupSharingRuleAsync(string groupUUID, string ruleUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGranteePermissions>> GetCharacterGranteesAsync(string ownerCharacterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertCharacterGranteePermissionsAsync(CharacterGranteePermissions perms) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteCharacterGranteePermissionsAsync(string ownerCharacterUUID, string granteeUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGranteeCapability>> GetCharacterGranteeCapabilitiesAsync(string ownerCharacterUUID, string granteeUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task AddCharacterGranteeCapabilityAsync(CharacterGranteeCapability item) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task RemoveCharacterGranteeCapabilityAsync(string ownerCharacterUUID, string granteeUUID, string capabilityUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Intel (stubs — Phase 7)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelComment>> GetIntelCommentsForTargetAsync(string targetCharacterUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IntelComment> GetIntelCommentAsync(string commentUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertIntelCommentAsync(IntelComment comment) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteIntelCommentAsync(string commentUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForCommentAsync(string commentUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForFactionAsync(string factionUUID) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertIntelShareAsync(IntelCommentFactionShare share) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteIntelShareAsync(string shareUUID) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Audit (stubs — Phase 7)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<PermissionAuditEntry>> GetPermissionAuditEntriesAsync(DateTime? startDate = null, DateTime? endDate = null, PermissionActionType? actionType = null, string actorUUID = null, string targetUUID = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task AppendPermissionAuditEntryAsync(PermissionAuditEntry entry) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task DeleteExpiredAuditEntriesAsync(DateTime cutoff) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Baseline / Global Lookup Data (stubs — Phase 6)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<BaselineGameConstants> GetBaselineGameConstantsAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertBaselineGameConstantsAsync(BaselineGameConstants constants) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<BlueprintType>> GetAllBlueprintTypesAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertBlueprintTypesAsync(IReadOnlyList<BlueprintType> types) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<ShipClass>> GetAllShipClassesAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertShipClassesAsync(IReadOnlyList<ShipClass> classes) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<TechLevel>> GetAllTechLevelsAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertTechLevelsAsync(IReadOnlyList<TechLevel> levels) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<Commodity>> GetAllCommoditiesAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertCommoditiesAsync(IReadOnlyList<Commodity> commodities) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<RefiningRecipe>> GetAllRefiningRecipesAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertRefiningRecipesAsync(IReadOnlyList<RefiningRecipe> recipes) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<ResearchTimeEntry>> GetAllResearchTimesAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertResearchTimesAsync(IReadOnlyList<ResearchTimeEntry> entries) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyList<PropertyTypeDefinition>> GetAllPropertyTypeDefinitionsAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UpsertPropertyTypeDefinitionsAsync(IReadOnlyList<PropertyTypeDefinition> definitions) => throw new NotImplementedException();

        // ═══════════════════════════════════════════════════════════
        // Private Helpers
        // ═══════════════════════════════════════════════════════════

        private static ServerFaction ReadServerFaction(SqliteDataReader reader)
        {
            var faction = new ServerFaction
            {
                UUID = reader.GetString(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                Metadata = new EntityMetadata(),
            };

            if (!reader.IsDBNull(3))
            {
                faction.Metadata.LastModifiedUtc = DateTime.Parse(reader.GetString(3));
            }

            if (!reader.IsDBNull(4))
            {
                faction.Metadata.ModifiedByTokenId = reader.GetString(4);
            }

            return faction;
        }

        private static List<string> ReadFactionLeaders(SqliteConnection conn, string factionUUID)
        {
            var leaders = new List<string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT CharacterUUID FROM ServerFactionLeaders WHERE FactionUUID = @fid";
                cmd.Parameters.AddWithValue("@fid", factionUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        leaders.Add(reader.GetString(0));
                    }
                }
            }

            return leaders;
        }

        private static ServerCharacter ReadServerCharacter(SqliteDataReader reader)
        {
            var character = new ServerCharacter
            {
                UUID = reader.GetString(0),
                Name = reader.GetString(1),
                Metadata = new EntityMetadata(),
            };

            if (!reader.IsDBNull(2))
            {
                character.FactionUUID = reader.GetString(2);
            }

            if (!reader.IsDBNull(3))
            {
                character.Metadata.LastModifiedUtc = DateTime.Parse(reader.GetString(3));
            }

            if (!reader.IsDBNull(4))
            {
                character.Metadata.ModifiedByTokenId = reader.GetString(4);
            }

            return character;
        }

        private static ApiToken ReadApiToken(SqliteDataReader reader)
        {
            var token = new ApiToken
            {
                Id = reader.GetString(0),
                TokenHash = reader.GetString(1),
                Role = (TokenRole)reader.GetInt32(3),
                CreatedUtc = DateTime.Parse(reader.GetString(5)),
                IsRevoked = reader.GetInt32(7) != 0,
                RateLimits = new RateLimitConfig { RequestsPerMinute = reader.GetInt32(8) },
            };

            if (!reader.IsDBNull(2))
            {
                token.CharacterUUID = reader.GetString(2);
            }

            if (!reader.IsDBNull(4))
            {
                token.FactionUUID = reader.GetString(4);
            }

            if (!reader.IsDBNull(6))
            {
                token.LastUsedUtc = DateTime.Parse(reader.GetString(6));
            }

            return token;
        }

        private static MembershipAction ReadMembershipAction(SqliteDataReader reader)
        {
            return new MembershipAction
            {
                Id = reader.GetString(0),
                FactionUUID = reader.GetString(1),
                CharacterUUID = reader.GetString(2),
                Type = (MembershipActionType)reader.GetInt32(3),
                CreatedUtc = DateTime.Parse(reader.GetString(4)),
                ExpiresUtc = DateTime.Parse(reader.GetString(5)),
            };
        }

        private static SharingRule ReadSharingRule(SqliteDataReader reader)
        {
            var rule = new SharingRule
            {
                Id = reader.GetString(0),
                OwnerCharacterUUID = reader.GetString(1),
                TargetUUID = reader.GetString(2),
                TargetType = (SharingTargetType)reader.GetInt32(3),
            };

            if (!reader.IsDBNull(4))
            {
                rule.DataType = reader.GetString(4);
            }

            if (!reader.IsDBNull(5))
            {
                rule.EntityUUID = reader.GetString(5);
            }

            return rule;
        }

        private static StarSystem ReadStarSystem(SqliteDataReader reader)
        {
            return new StarSystem
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                X = (decimal)reader.GetDouble(2),
                Y = (decimal)reader.GetDouble(3),
                Quadrant = reader.GetInt32(4),
                Sector = reader.GetInt32(5),
                Region = reader.GetInt32(6),
                Locality = reader.GetInt32(7),
                SpectralClass = reader.GetString(8),
                FactionId = reader.GetInt32(9),
                FactionName = reader.GetString(10),
                FactionColor = reader.GetString(11),
                HasOrbital = reader.GetInt32(12) != 0,
                HasSpaceport = reader.GetInt32(13) != 0,
                HasStarbase = reader.GetInt32(14) != 0,
            };
        }

        private static int GetSchemaVersion(SqliteConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Value FROM _metadata WHERE Key = 'schema_version';";
                var result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    return 0;
                }

                return int.TryParse(result.ToString(), out int v) ? v : 0;
            }
        }

        private static void SetSchemaVersion(SqliteConnection conn, int version)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT OR REPLACE INTO _metadata (Key, Value) VALUES ('schema_version', @v);";
                cmd.Parameters.AddWithValue("@v", version.ToString());
                cmd.ExecuteNonQuery();
            }
        }

        private SqliteConnection OpenConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA journal_mode=WAL;";
                cmd.ExecuteNonQuery();
            }

            return conn;
        }

        private void ExecuteSchema(SqliteConnection conn)
        {
            if (string.IsNullOrWhiteSpace(SchemaDdl))
            {
                Log.Debug("No schema DDL to execute (will be populated in tasks 5.2-5.11)");
                return;
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = SchemaDdl;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
