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

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Schema DDL concatenated from tasks 5.2-5.11. ExecuteSchema runs this
        /// against a fresh database.
        /// </summary>
        private static readonly string SchemaDdl = ColonySchema + ItemsBlueprintSchema + SurveyPlayerProfileSchema + DeliveryRouteShipSchema + DeliveryPlanMarketSchema + PricingBuildStockSchema + RemainingPlayerEntitySchema + ServerGlobalSchema + PermissionSchema + IntelAuditBaselineSchema;

        private readonly string _connectionString;
        private readonly string _databasePath;

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
        // Per-Character Entity CRUD — Colony
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Colony>> GetAllColoniesAsync(string characterUUID)
        {
            var results = new List<Colony>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Colonies WHERE OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(ReadColonyParent(reader));
                    }
                }

                foreach (var colony in results)
                {
                    colony.Structures = LoadColonyStructures(conn, colony.UUID);
                    colony.Items = LoadItems(conn, colony.UUID, "Colony");
                }
            }

            return Task.FromResult<IReadOnlyList<Colony>>(results);
        }

        /// <inheritdoc/>
        public Task<Colony> GetColonyAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Colonies WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var colony = ReadColonyParent(reader);
                        colony.Structures = LoadColonyStructures(conn, colony.UUID);
                        colony.Items = LoadItems(conn, colony.UUID, "Colony");
                        return Task.FromResult(colony);
                    }
                }
            }

            return Task.FromResult<Colony>(null);
        }

        /// <inheritdoc/>
        public Task UpsertColonyAsync(string characterUUID, Colony entity)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                UpsertColonyParent(conn, tx, characterUUID, entity);
                DeleteColonyChildren(conn, tx, entity.UUID);
                InsertColonyStructures(conn, tx, entity);
                InsertItems(conn, tx, entity.UUID, "Colony", entity.Items);
                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteColonyAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            {
                // Delete items first (polymorphic FK, no CASCADE)
                using (var delItems = conn.CreateCommand())
                {
                    delItems.CommandText = "DELETE FROM Items WHERE ParentUUID = @uuid AND ParentType = 'Colony'";
                    delItems.Parameters.AddWithValue("@uuid", entityUUID);
                    delItems.ExecuteNonQuery();
                }

                // CASCADE handles ColonyStructures, ColonyStructureProperties, ColonyStructureWorkers
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Colonies WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            }

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Blueprint
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Blueprint>> GetAllBlueprintsAsync(string characterUUID)
        {
            var results = new List<Blueprint>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Blueprints WHERE OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(ReadBlueprintParent(reader));
                    }
                }

                foreach (var bp in results)
                {
                    bp.Properties = LoadPropertyBag(conn, "BlueprintProperties", "BlueprintUUID", bp.UUID);
                    bp.Resources = LoadBlueprintResources(conn, bp.UUID);
                }
            }

            return Task.FromResult<IReadOnlyList<Blueprint>>(results);
        }

        /// <inheritdoc/>
        public Task<Blueprint> GetBlueprintAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Blueprints WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var bp = ReadBlueprintParent(reader);
                        bp.Properties = LoadPropertyBag(conn, "BlueprintProperties", "BlueprintUUID", bp.UUID);
                        bp.Resources = LoadBlueprintResources(conn, bp.UUID);
                        return Task.FromResult(bp);
                    }
                }
            }

            return Task.FromResult<Blueprint>(null);
        }

        /// <inheritdoc/>
        public Task UpsertBlueprintAsync(string characterUUID, Blueprint entity)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                UpsertBlueprintParent(conn, tx, characterUUID, entity);
                DeleteBlueprintChildren(conn, tx, entity.UUID);
                InsertBlueprintChildren(conn, tx, entity);
                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteBlueprintAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                // CASCADE handles BlueprintProperties and BlueprintResources
                cmd.CommandText = "DELETE FROM Blueprints WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                cmd.ExecuteNonQuery();
            }

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Survey
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Survey>> GetAllSurveysAsync(string characterUUID)
        {
            var results = new List<Survey>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Surveys WHERE OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(ReadSurveyParent(reader));
                    }
                }

                foreach (var survey in results)
                {
                    survey.Properties = LoadSurveyProperties(conn, survey.UUID);
                    survey.Resources = LoadSurveyResources(conn, survey.UUID);
                }
            }

            return Task.FromResult<IReadOnlyList<Survey>>(results);
        }

        /// <inheritdoc/>
        public Task<Survey> GetSurveyAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Surveys WHERE SurveyID = @surveyId AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@surveyId", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var survey = ReadSurveyParent(reader);
                        survey.Properties = LoadSurveyProperties(conn, survey.UUID);
                        survey.Resources = LoadSurveyResources(conn, survey.UUID);
                        return Task.FromResult(survey);
                    }
                }
            }

            return Task.FromResult<Survey>(null);
        }

        /// <inheritdoc/>
        public Task UpsertSurveyAsync(string characterUUID, Survey entity)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                UpsertSurveyParent(conn, tx, characterUUID, entity);
                DeleteSurveyChildren(conn, tx, entity.UUID);
                InsertSurveyChildren(conn, tx, entity);
                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteSurveyAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                // CASCADE handles SurveyProperties and SurveyResources
                cmd.CommandText = "DELETE FROM Surveys WHERE SurveyID = @surveyId AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@surveyId", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                cmd.ExecuteNonQuery();
            }

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — PlayerProfile
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<PlayerProfile>> GetAllPlayerProfilesAsync(string characterUUID)
        {
            var results = new List<PlayerProfile>();
            using (var conn = OpenConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM PlayerProfiles WHERE UUID = @uuid";
                    cmd.Parameters.AddWithValue("@uuid", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadPlayerProfileParent(reader));
                        }
                    }
                }

                foreach (var profile in results)
                {
                    profile.Skills = LoadPlayerSkills(conn, profile.UUID);
                }
            }

            return Task.FromResult<IReadOnlyList<PlayerProfile>>(results);
        }

        /// <inheritdoc/>
        public Task<PlayerProfile> GetPlayerProfileAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM PlayerProfiles WHERE UUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var profile = ReadPlayerProfileParent(reader);
                        profile.Skills = LoadPlayerSkills(conn, profile.UUID);
                        return Task.FromResult(profile);
                    }
                }
            }

            return Task.FromResult<PlayerProfile>(null);
        }

        /// <inheritdoc/>
        public Task UpsertPlayerProfileAsync(string characterUUID, PlayerProfile entity)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                UpsertPlayerProfileParent(conn, tx, entity);
                DeletePlayerProfileChildren(conn, tx, entity.UUID);
                InsertPlayerSkills(conn, tx, entity);
                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeletePlayerProfileAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                // CASCADE handles PlayerSkills
                cmd.CommandText = "DELETE FROM PlayerProfiles WHERE UUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.ExecuteNonQuery();
            }

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — DeliveryRoute
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<DeliveryRoute>> GetAllDeliveryRoutesAsync(string characterUUID)
        {
            var results = new List<DeliveryRoute>();
            using (var conn = OpenConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM DeliveryRoutes WHERE OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadDeliveryRouteParent(reader));
                        }
                    }
                }

                foreach (var route in results)
                {
                    route.Stops = LoadDeliveryRouteStops(conn, route.UUID);
                }
            }

            return Task.FromResult<IReadOnlyList<DeliveryRoute>>(results);
        }

        /// <inheritdoc/>
        public Task<DeliveryRoute> GetDeliveryRouteAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM DeliveryRoutes WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var route = ReadDeliveryRouteParent(reader);
                        route.Stops = LoadDeliveryRouteStops(conn, route.UUID);
                        return Task.FromResult(route);
                    }
                }
            }

            return Task.FromResult<DeliveryRoute>(null);
        }

        /// <inheritdoc/>
        public Task UpsertDeliveryRouteAsync(string characterUUID, DeliveryRoute entity)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                UpsertDeliveryRouteParent(conn, tx, characterUUID, entity);
                DeleteDeliveryRouteChildren(conn, tx, entity.UUID);
                InsertDeliveryRouteStops(conn, tx, entity);
                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteDeliveryRouteAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                // CASCADE handles DeliveryRouteStops
                cmd.CommandText = "DELETE FROM DeliveryRoutes WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                cmd.ExecuteNonQuery();
            }

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — DeliveryPlan
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<DeliveryPlan>> GetAllDeliveryPlansAsync(string characterUUID)
        {
            var results = new List<DeliveryPlan>();
            using (var conn = OpenConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM DeliveryPlans WHERE OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadDeliveryPlanParent(reader));
                        }
                    }
                }

                foreach (var plan in results)
                {
                    plan.Stops = LoadDeliveryPlanStops(conn, plan.UUID);
                }
            }

            return Task.FromResult<IReadOnlyList<DeliveryPlan>>(results);
        }

        /// <inheritdoc/>
        public Task<DeliveryPlan> GetDeliveryPlanAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM DeliveryPlans WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var plan = ReadDeliveryPlanParent(reader);
                        plan.Stops = LoadDeliveryPlanStops(conn, plan.UUID);
                        return Task.FromResult(plan);
                    }
                }
            }

            return Task.FromResult<DeliveryPlan>(null);
        }

        /// <inheritdoc/>
        public Task UpsertDeliveryPlanAsync(string characterUUID, DeliveryPlan entity)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                UpsertDeliveryPlanParent(conn, tx, characterUUID, entity);
                DeleteDeliveryPlanChildren(conn, tx, entity.UUID);
                InsertDeliveryPlanChildren(conn, tx, entity);
                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteDeliveryPlanAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                // CASCADE handles DeliveryPlanStops and DeliveryPlanItems
                cmd.CommandText = "DELETE FROM DeliveryPlans WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                cmd.ExecuteNonQuery();
            }

            return Task.CompletedTask;
        }

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

        private static Colony ReadColonyParent(SqliteDataReader reader)
        {
            var colony = new Colony
            {
                UUID = reader["UUID"] as string,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                LegacyUUID = reader["LegacyUUID"] as string,
                PlanetName = reader["PlanetName"] as string,
                SystemName = reader["SystemName"] as string ?? string.Empty,
                ColonyName = reader["ColonyName"] as string,
                LastImportDateTime = reader["LastImportDateTime"] as string,
                ColonyId = Convert.ToInt32(reader["ColonyId"]),
                SystemId = Convert.ToInt32(reader["SystemId"]),
                ColonySize = Convert.ToInt32(reader["ColonySize"]),
                Distance = Convert.ToDecimal(reader["Distance"]),
                SurfaceVariation = Convert.ToInt32(reader["SurfaceVariation"]),
                AtmosVariation = Convert.ToInt32(reader["AtmosVariation"]),
                HexValue = reader["HexValue"] as string ?? string.Empty,
                SystemObjectTypeName = reader["SystemObjectTypeName"] as string ?? string.Empty,
                ImagePreFix = reader["ImagePreFix"] as string ?? string.Empty,
                ManufacturingBlocked = Convert.ToInt32(reader["ManufacturingBlocked"]),
                WorkerCurrentAttitude = Convert.ToInt32(reader["WorkerCurrentAttitude"]),
                ContentmentIndex = Convert.ToInt32(reader["ContentmentIndex"]),
                BlueCollarAllocated = Convert.ToInt32(reader["BlueCollarAllocated"]),
                BlueCollarUnallocated = Convert.ToInt32(reader["BlueCollarUnallocated"]),
                WhiteCollarAllocated = Convert.ToInt32(reader["WhiteCollarAllocated"]),
                WhiteCollarUnallocated = Convert.ToInt32(reader["WhiteCollarUnallocated"]),
                SpecialistAllocated = Convert.ToInt32(reader["SpecialistAllocated"]),
                SpecialistUnallocated = Convert.ToInt32(reader["SpecialistUnallocated"]),
                WageLevel = Convert.ToInt32(reader["WageLevel"]),
            };
            return colony;
        }

        private static List<ColonyStructure> LoadColonyStructures(SqliteConnection conn, string colonyUUID)
        {
            var structures = new List<ColonyStructure>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM ColonyStructures WHERE ColonyUUID = @cid ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@cid", colonyUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var s = new ColonyStructure
                        {
                            UUID = reader["UUID"] as string,
                            FlatpackBlueprintUUID = reader["FlatpackBlueprintUUID"] as string,
                            DisplaySequence = Convert.ToInt32(reader["DisplaySequence"]),
                            BuildingID = Convert.ToInt32(reader["BuildingID"]),
                            BuildQueueSequence = Convert.ToInt32(reader["BuildQueueSequence"]),
                            MiningSurvey = reader["MiningSurvey"] as string,
                            MiningSurveyResource = reader["MiningSurveyResource"] as string,
                            MiningLeftOvers = Convert.ToDecimal(reader["MiningLeftOvers"]),
                            RefiningResource = reader["RefiningResource"] as string,
                            RefiningResourcePurity = reader["RefiningResourcePurity"] as string,
                            ResearchingBlueprintUUID = reader["ResearchingBlueprintUUID"] as string,
                            ManufacturingBlueprintUUID = reader["ManufacturingBlueprintUUID"] as string,
                            ManufacturingCommodityName = reader["ManufacturingCommodityName"] as string,
                            ManufacturingQuantity = Convert.ToInt32(reader["ManufacturingQuantity"]),
                            ManufacturingCompleted = Convert.ToInt32(reader["ManufacturingCompleted"]),
                            StagingResources = Convert.ToInt32(reader["StagingResources"]) != 0,
                            ColonyBuildingTypeId = Convert.ToInt32(reader["ColonyBuildingTypeId"]),
                            ResourceId = Convert.ToInt32(reader["ResourceId"]),
                            ResourceIcon = reader["ResourceIcon"] as string ?? string.Empty,
                            ManufactureAmountPerRun = Convert.ToInt32(reader["ManufactureAmountPerRun"]),
                            DurabilityCurrent = Convert.ToDecimal(reader["DurabilityCurrent"]),
                            DurabilityMax = Convert.ToDecimal(reader["DurabilityMax"]),
                            WageLevel = Convert.ToInt32(reader["WageLevel"]),
                        };

                        // BuildCompletionTime
                        var buildStart = reader["BuildCompletion_StartTime"] as string;
                        if (buildStart != null)
                        {
                            s.BuildCompletionTime = new CountDownTime
                            {
                                StartTime = DateTime.Parse(buildStart),
                                RepeatIntervalSeconds = reader["BuildCompletion_RepeatIntervalSeconds"] == DBNull.Value ? 0 : Convert.ToInt64(reader["BuildCompletion_RepeatIntervalSeconds"]),
                            };
                        }

                        // ProcessCompletionTime
                        var procStart = reader["ProcessCompletion_StartTime"] as string;
                        if (procStart != null)
                        {
                            s.ProcessCompletionTime = new CountDownTime
                            {
                                StartTime = DateTime.Parse(procStart),
                                RepeatIntervalSeconds = reader["ProcessCompletion_RepeatIntervalSeconds"] == DBNull.Value ? 0 : Convert.ToInt64(reader["ProcessCompletion_RepeatIntervalSeconds"]),
                            };
                        }

                        // Load Properties and AssignedWorkers
                        s.Properties = LoadPropertyBag(conn, "ColonyStructureProperties", "StructureUUID", s.UUID);
                        s.AssignedWorkers = LoadPropertyBag(conn, "ColonyStructureWorkers", "StructureUUID", s.UUID);

                        structures.Add(s);
                    }
                }
            }

            return structures;
        }

        private static PropertyBag LoadPropertyBag(SqliteConnection conn, string tableName, string fkColumn, string fkValue)
        {
            var bag = new PropertyBag();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT Key, Value FROM {tableName} WHERE {fkColumn} = @fk";
                cmd.Parameters.AddWithValue("@fk", fkValue);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bag.Properties[reader.GetString(0)] = reader.GetString(1);
                    }
                }
            }

            return bag;
        }

        private static ItemBag LoadItems(SqliteConnection conn, string parentUUID, string parentType)
        {
            var bag = new ItemBag();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Items WHERE ParentUUID = @pid AND ParentType = @pt";
                cmd.Parameters.AddWithValue("@pid", parentUUID);
                cmd.Parameters.AddWithValue("@pt", parentType);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = new Item
                        {
                            UUID = reader["UUID"] as string,
                            ItemType = Enum.TryParse<ItemType.ItemTypeEnum>(reader["ItemType"] as string, true, out var it) ? it : ItemType.ItemTypeEnum.None,
                            BaseItemTypeID = reader["BaseItemTypeID"] as string ?? string.Empty,
                            Name = reader["Name"] as string ?? string.Empty,
                            NickName = reader["NickName"] as string ?? string.Empty,
                            Description = reader["Description"] as string ?? string.Empty,
                            Quantity = Convert.ToInt32(reader["Quantity"]),
                            ResourcePurity = reader["ResourcePurity"] as string ?? string.Empty,
                            Volume = Convert.ToDecimal(reader["Volume"]),
                            CurrentHP = Convert.ToInt32(reader["CurrentHP"]),
                            MaxHP = Convert.ToInt32(reader["MaxHP"]),
                            MaxRepairPercent = Convert.ToDecimal(reader["MaxRepairPercent"]),
                            ShipPartType = reader["ShipPartType"] as string ?? string.Empty,
                            JobName = reader["JobName"] as string ?? string.Empty,
                            JobTrack = reader["JobTrack"] as string ?? string.Empty,
                        };

                        if (reader["Mass"] != DBNull.Value)
                        {
                            item.Mass = Convert.ToDecimal(reader["Mass"]);
                        }

                        if (reader["GameItemId"] != DBNull.Value)
                        {
                            item.GameItemId = Convert.ToInt32(reader["GameItemId"]);
                        }

                        if (reader["JobRef"] != DBNull.Value)
                        {
                            item.JobRef = Convert.ToInt32(reader["JobRef"]);
                        }

                        if (reader["JobDeliveryLoc"] != DBNull.Value)
                        {
                            item.JobDeliveryLoc = Convert.ToInt32(reader["JobDeliveryLoc"]);
                        }

                        if (reader["HealthPercentage"] != DBNull.Value)
                        {
                            item.HealthPercentage = Convert.ToDecimal(reader["HealthPercentage"]);
                        }

                        if (reader["LastRepairHealthPercentage"] != DBNull.Value)
                        {
                            item.LastRepairHealthPercentage = Convert.ToDecimal(reader["LastRepairHealthPercentage"]);
                        }

                        if (reader["Evolution"] != DBNull.Value)
                        {
                            item.Evolution = Convert.ToInt32(reader["Evolution"]);
                        }

                        bag.Items[item.UUID] = item;
                    }
                }
            }

            return bag;
        }

        private static void UpsertColonyParent(SqliteConnection conn, SqliteTransaction tx, string characterUUID, Colony entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO Colonies (
                    UUID, OwnerUUID, LegacyUUID, PlanetName, SystemName, ColonyName,
                    LastImportDateTime, ColonyId, SystemId, ColonySize, Distance,
                    SurfaceVariation, AtmosVariation, HexValue, SystemObjectTypeName,
                    ImagePreFix, ManufacturingBlocked, WorkerCurrentAttitude, ContentmentIndex,
                    BlueCollarAllocated, BlueCollarUnallocated, WhiteCollarAllocated,
                    WhiteCollarUnallocated, SpecialistAllocated, SpecialistUnallocated, WageLevel
                ) VALUES (
                    @uuid, @owner, @legacy, @planet, @system, @colName,
                    @lastImport, @colId, @sysId, @colSize, @distance,
                    @surfVar, @atmosVar, @hex, @sysObjType,
                    @imgPre, @mfgBlocked, @attitude, @contentment,
                    @bcAlloc, @bcUnalloc, @wcAlloc,
                    @wcUnalloc, @specAlloc, @specUnalloc, @wage
                )";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@legacy", (object)entity.LegacyUUID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@planet", (object)entity.PlanetName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@system", entity.SystemName ?? string.Empty);
                cmd.Parameters.AddWithValue("@colName", (object)entity.ColonyName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@lastImport", (object)entity.LastImportDateTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@colId", entity.ColonyId);
                cmd.Parameters.AddWithValue("@sysId", entity.SystemId);
                cmd.Parameters.AddWithValue("@colSize", entity.ColonySize);
                cmd.Parameters.AddWithValue("@distance", (double)entity.Distance);
                cmd.Parameters.AddWithValue("@surfVar", entity.SurfaceVariation);
                cmd.Parameters.AddWithValue("@atmosVar", entity.AtmosVariation);
                cmd.Parameters.AddWithValue("@hex", entity.HexValue ?? string.Empty);
                cmd.Parameters.AddWithValue("@sysObjType", entity.SystemObjectTypeName ?? string.Empty);
                cmd.Parameters.AddWithValue("@imgPre", entity.ImagePreFix ?? string.Empty);
                cmd.Parameters.AddWithValue("@mfgBlocked", entity.ManufacturingBlocked);
                cmd.Parameters.AddWithValue("@attitude", entity.WorkerCurrentAttitude);
                cmd.Parameters.AddWithValue("@contentment", entity.ContentmentIndex);
                cmd.Parameters.AddWithValue("@bcAlloc", entity.BlueCollarAllocated);
                cmd.Parameters.AddWithValue("@bcUnalloc", entity.BlueCollarUnallocated);
                cmd.Parameters.AddWithValue("@wcAlloc", entity.WhiteCollarAllocated);
                cmd.Parameters.AddWithValue("@wcUnalloc", entity.WhiteCollarUnallocated);
                cmd.Parameters.AddWithValue("@specAlloc", entity.SpecialistAllocated);
                cmd.Parameters.AddWithValue("@specUnalloc", entity.SpecialistUnallocated);
                cmd.Parameters.AddWithValue("@wage", entity.WageLevel);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteColonyChildren(SqliteConnection conn, SqliteTransaction tx, string colonyUUID)
        {
            // Delete items (polymorphic FK, no CASCADE from Colonies)
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM Items WHERE ParentUUID = @uuid AND ParentType = 'Colony'";
                cmd.Parameters.AddWithValue("@uuid", colonyUUID);
                cmd.ExecuteNonQuery();
            }

            // Delete structure items
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"DELETE FROM Items WHERE ParentType = 'ColonyStructure' AND ParentUUID IN
                    (SELECT UUID FROM ColonyStructures WHERE ColonyUUID = @uuid)";
                cmd.Parameters.AddWithValue("@uuid", colonyUUID);
                cmd.ExecuteNonQuery();
            }

            // CASCADE handles ColonyStructureProperties and ColonyStructureWorkers
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM ColonyStructures WHERE ColonyUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", colonyUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertColonyStructures(SqliteConnection conn, SqliteTransaction tx, Colony entity)
        {
            for (int i = 0; i < entity.Structures.Count; i++)
            {
                var s = entity.Structures[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO ColonyStructures (
                        UUID, ColonyUUID, Sequence, FlatpackBlueprintUUID, DisplaySequence,
                        BuildingID, BuildQueueSequence, MiningSurvey, MiningSurveyResource,
                        MiningLeftOvers, RefiningResource, RefiningResourcePurity,
                        ResearchingBlueprintUUID, ManufacturingBlueprintUUID,
                        ManufacturingCommodityName, ManufacturingQuantity, ManufacturingCompleted,
                        StagingResources, ColonyBuildingTypeId, ResourceId, ResourceIcon,
                        ManufactureAmountPerRun, DurabilityCurrent, DurabilityMax, WageLevel,
                        BuildCompletion_StartTime, BuildCompletion_RepeatIntervalSeconds, BuildCompletion_IsRepeating,
                        ProcessCompletion_StartTime, ProcessCompletion_RepeatIntervalSeconds, ProcessCompletion_IsRepeating
                    ) VALUES (
                        @uuid, @colUUID, @seq, @flatpack, @dispSeq,
                        @buildId, @bqSeq, @minSurvey, @minRes,
                        @minLeft, @refRes, @refPurity,
                        @resBp, @mfgBp,
                        @mfgComm, @mfgQty, @mfgDone,
                        @staging, @cbTypeId, @resId, @resIcon,
                        @mfgPerRun, @durCur, @durMax, @wage,
                        @bcStart, @bcInterval, @bcRepeat,
                        @pcStart, @pcInterval, @pcRepeat
                    )";
                    cmd.Parameters.AddWithValue("@uuid", s.UUID);
                    cmd.Parameters.AddWithValue("@colUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", i);
                    cmd.Parameters.AddWithValue("@flatpack", (object)s.FlatpackBlueprintUUID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@dispSeq", s.DisplaySequence);
                    cmd.Parameters.AddWithValue("@buildId", s.BuildingID);
                    cmd.Parameters.AddWithValue("@bqSeq", s.BuildQueueSequence);
                    cmd.Parameters.AddWithValue("@minSurvey", (object)s.MiningSurvey ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@minRes", (object)s.MiningSurveyResource ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@minLeft", (double)s.MiningLeftOvers);
                    cmd.Parameters.AddWithValue("@refRes", (object)s.RefiningResource ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@refPurity", (object)s.RefiningResourcePurity ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@resBp", (object)s.ResearchingBlueprintUUID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@mfgBp", (object)s.ManufacturingBlueprintUUID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@mfgComm", (object)s.ManufacturingCommodityName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@mfgQty", s.ManufacturingQuantity);
                    cmd.Parameters.AddWithValue("@mfgDone", s.ManufacturingCompleted);
                    cmd.Parameters.AddWithValue("@staging", s.StagingResources ? 1 : 0);
                    cmd.Parameters.AddWithValue("@cbTypeId", s.ColonyBuildingTypeId);
                    cmd.Parameters.AddWithValue("@resId", s.ResourceId);
                    cmd.Parameters.AddWithValue("@resIcon", s.ResourceIcon ?? string.Empty);
                    cmd.Parameters.AddWithValue("@mfgPerRun", s.ManufactureAmountPerRun);
                    cmd.Parameters.AddWithValue("@durCur", (double)s.DurabilityCurrent);
                    cmd.Parameters.AddWithValue("@durMax", (double)s.DurabilityMax);
                    cmd.Parameters.AddWithValue("@wage", s.WageLevel);

                    // BuildCompletionTime
                    if (s.BuildCompletionTime != null)
                    {
                        cmd.Parameters.AddWithValue("@bcStart", s.BuildCompletionTime.StartTime.ToString("O"));
                        cmd.Parameters.AddWithValue("@bcInterval", s.BuildCompletionTime.RepeatIntervalSeconds);
                        cmd.Parameters.AddWithValue("@bcRepeat", s.BuildCompletionTime.IsRepeating ? 1 : 0);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@bcStart", DBNull.Value);
                        cmd.Parameters.AddWithValue("@bcInterval", DBNull.Value);
                        cmd.Parameters.AddWithValue("@bcRepeat", DBNull.Value);
                    }

                    // ProcessCompletionTime
                    if (s.ProcessCompletionTime != null)
                    {
                        cmd.Parameters.AddWithValue("@pcStart", s.ProcessCompletionTime.StartTime.ToString("O"));
                        cmd.Parameters.AddWithValue("@pcInterval", s.ProcessCompletionTime.RepeatIntervalSeconds);
                        cmd.Parameters.AddWithValue("@pcRepeat", s.ProcessCompletionTime.IsRepeating ? 1 : 0);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@pcStart", DBNull.Value);
                        cmd.Parameters.AddWithValue("@pcInterval", DBNull.Value);
                        cmd.Parameters.AddWithValue("@pcRepeat", DBNull.Value);
                    }

                    cmd.ExecuteNonQuery();
                }

                // Insert properties
                foreach (var kvp in s.Properties.Properties)
                {
                    using (var propCmd = conn.CreateCommand())
                    {
                        propCmd.Transaction = tx;
                        propCmd.CommandText = "INSERT INTO ColonyStructureProperties (StructureUUID, Key, Value) VALUES (@sid, @key, @val)";
                        propCmd.Parameters.AddWithValue("@sid", s.UUID);
                        propCmd.Parameters.AddWithValue("@key", kvp.Key);
                        propCmd.Parameters.AddWithValue("@val", kvp.Value);
                        propCmd.ExecuteNonQuery();
                    }
                }

                // Insert assigned workers
                foreach (var kvp in s.AssignedWorkers.Properties)
                {
                    using (var wCmd = conn.CreateCommand())
                    {
                        wCmd.Transaction = tx;
                        wCmd.CommandText = "INSERT INTO ColonyStructureWorkers (StructureUUID, Key, Value) VALUES (@sid, @key, @val)";
                        wCmd.Parameters.AddWithValue("@sid", s.UUID);
                        wCmd.Parameters.AddWithValue("@key", kvp.Key);
                        wCmd.Parameters.AddWithValue("@val", kvp.Value);
                        wCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static void InsertItems(SqliteConnection conn, SqliteTransaction tx, string parentUUID, string parentType, ItemBag items)
        {
            if (items == null)
            {
                return;
            }

            foreach (var kvp in items.Items)
            {
                var item = kvp.Value;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO Items (
                        UUID, ParentUUID, ParentType, ItemType, BaseItemTypeID, Name, NickName,
                        Description, Quantity, ResourcePurity, Volume, CurrentHP, MaxHP,
                        MaxRepairPercent, Mass, GameItemId, JobRef, JobDeliveryLoc,
                        HealthPercentage, LastRepairHealthPercentage, Evolution,
                        ShipPartType, JobName, JobTrack
                    ) VALUES (
                        @uuid, @parent, @ptype, @itype, @baseId, @name, @nick,
                        @desc, @qty, @purity, @vol, @curHp, @maxHp,
                        @maxRepair, @mass, @gameId, @jobRef, @jobDel,
                        @health, @lastRepair, @evo,
                        @shipPart, @jobName, @jobTrack
                    )";
                    cmd.Parameters.AddWithValue("@uuid", item.UUID);
                    cmd.Parameters.AddWithValue("@parent", parentUUID);
                    cmd.Parameters.AddWithValue("@ptype", parentType);
                    cmd.Parameters.AddWithValue("@itype", item.ItemType.ToString());
                    cmd.Parameters.AddWithValue("@baseId", item.BaseItemTypeID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@name", item.Name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@nick", item.NickName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@desc", item.Description ?? string.Empty);
                    cmd.Parameters.AddWithValue("@qty", item.Quantity);
                    cmd.Parameters.AddWithValue("@purity", item.ResourcePurity ?? string.Empty);
                    cmd.Parameters.AddWithValue("@vol", (double)item.Volume);
                    cmd.Parameters.AddWithValue("@curHp", item.CurrentHP);
                    cmd.Parameters.AddWithValue("@maxHp", item.MaxHP);
                    cmd.Parameters.AddWithValue("@maxRepair", (double)item.MaxRepairPercent);
                    cmd.Parameters.AddWithValue("@mass", item.Mass.HasValue ? (object)(double)item.Mass.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@gameId", item.GameItemId.HasValue ? (object)item.GameItemId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@jobRef", item.JobRef.HasValue ? (object)item.JobRef.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@jobDel", item.JobDeliveryLoc.HasValue ? (object)item.JobDeliveryLoc.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@health", item.HealthPercentage.HasValue ? (object)(double)item.HealthPercentage.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@lastRepair", item.LastRepairHealthPercentage.HasValue ? (object)(double)item.LastRepairHealthPercentage.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@evo", item.Evolution.HasValue ? (object)item.Evolution.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@shipPart", item.ShipPartType ?? string.Empty);
                    cmd.Parameters.AddWithValue("@jobName", item.JobName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@jobTrack", item.JobTrack ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // Blueprint Helpers
        // ═══════════════════════════════════════════════════════════

        private static Blueprint ReadBlueprintParent(SqliteDataReader reader)
        {
            var bp = new Blueprint
            {
                UUID = reader["UUID"] as string,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                BaseBlueprintUUID = reader["BaseBlueprintUUID"] as string,
                LegacyUUID = reader["LegacyUUID"] as string,
                BluePrintType = reader["BluePrintType"] as string,
                TechLevel = reader["TechLevel"] as string,
                Class = Convert.ToInt32(reader["Class"]),
                Evolution = Convert.ToInt32(reader["Evolution"]),
                CopyCost = Convert.ToInt32(reader["CopyCost"]),
                BaseItemTypeID = reader["BaseItemTypeID"] as string ?? string.Empty,
                Name = reader["Name"] as string ?? string.Empty,
                NickName = reader["NickName"] as string ?? string.Empty,
                Description = reader["Description"] as string ?? string.Empty,
                Quantity = Convert.ToInt32(reader["Quantity"]),
                Volume = Convert.ToDecimal(reader["Volume"]),
            };

            var itemTypeStr = reader["ItemType"] as string;
            if (Enum.TryParse<ItemType.ItemTypeEnum>(itemTypeStr, true, out var it))
            {
                bp.ItemType = it;
            }

            if (reader["GameApiBlueprintId"] != DBNull.Value)
            {
                bp.GameApiBlueprintId = Convert.ToInt32(reader["GameApiBlueprintId"]);
            }

            if (reader["LastDetailImportUtc"] != DBNull.Value)
            {
                var dtStr = reader["LastDetailImportUtc"] as string;
                if (dtStr != null)
                {
                    bp.LastDetailImportUtc = DateTime.Parse(dtStr);
                }
            }

            return bp;
        }

        private static Dictionary<string, string> LoadBlueprintResources(SqliteConnection conn, string blueprintUUID)
        {
            var resources = new Dictionary<string, string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ResourceName, Amount FROM BlueprintResources WHERE BlueprintUUID = @bpUUID";
                cmd.Parameters.AddWithValue("@bpUUID", blueprintUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = reader.GetString(0);
                        var amount = Convert.ToInt32(reader[1]);
                        resources[name] = amount.ToString();
                    }
                }
            }

            return resources;
        }

        private static void UpsertBlueprintParent(SqliteConnection conn, SqliteTransaction tx, string characterUUID, Blueprint entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO Blueprints (
                    UUID, OwnerUUID, BaseBlueprintUUID, LegacyUUID, BluePrintType,
                    TechLevel, Class, Evolution, CopyCost, ItemType,
                    BaseItemTypeID, Name, NickName, Description, Quantity, Volume,
                    GameApiBlueprintId, LastDetailImportUtc
                ) VALUES (
                    @uuid, @owner, @baseBp, @legacy, @bpType,
                    @tech, @class, @evo, @copyCost, @itemType,
                    @baseId, @name, @nick, @desc, @qty, @vol,
                    @gameApiId, @lastImport
                )";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@baseBp", (object)entity.BaseBlueprintUUID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@legacy", (object)entity.LegacyUUID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@bpType", (object)entity.BluePrintType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@tech", (object)entity.TechLevel ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@class", entity.Class);
                cmd.Parameters.AddWithValue("@evo", entity.Evolution);
                cmd.Parameters.AddWithValue("@copyCost", entity.CopyCost);
                cmd.Parameters.AddWithValue("@itemType", entity.ItemType.ToString());
                cmd.Parameters.AddWithValue("@baseId", entity.BaseItemTypeID ?? string.Empty);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@nick", entity.NickName ?? string.Empty);
                cmd.Parameters.AddWithValue("@desc", entity.Description ?? string.Empty);
                cmd.Parameters.AddWithValue("@qty", entity.Quantity);
                cmd.Parameters.AddWithValue("@vol", (double)entity.Volume);
                cmd.Parameters.AddWithValue("@gameApiId", entity.GameApiBlueprintId.HasValue ? (object)entity.GameApiBlueprintId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@lastImport", entity.LastDetailImportUtc.HasValue ? (object)entity.LastDetailImportUtc.Value.ToString("O") : DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteBlueprintChildren(SqliteConnection conn, SqliteTransaction tx, string blueprintUUID)
        {
            // CASCADE handles these, but explicit delete within transaction is cleaner for INSERT OR REPLACE
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM BlueprintProperties WHERE BlueprintUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", blueprintUUID);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM BlueprintResources WHERE BlueprintUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", blueprintUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertBlueprintChildren(SqliteConnection conn, SqliteTransaction tx, Blueprint entity)
        {
            if (entity.Properties != null)
            {
                foreach (var kvp in entity.Properties.Properties)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "INSERT INTO BlueprintProperties (BlueprintUUID, Key, Value) VALUES (@bpUUID, @key, @val)";
                        cmd.Parameters.AddWithValue("@bpUUID", entity.UUID);
                        cmd.Parameters.AddWithValue("@key", kvp.Key);
                        cmd.Parameters.AddWithValue("@val", kvp.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            if (entity.Resources != null)
            {
                foreach (var kvp in entity.Resources)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "INSERT INTO BlueprintResources (BlueprintUUID, ResourceName, Amount) VALUES (@bpUUID, @resName, @amount)";
                        cmd.Parameters.AddWithValue("@bpUUID", entity.UUID);
                        cmd.Parameters.AddWithValue("@resName", kvp.Key);
                        cmd.Parameters.AddWithValue("@amount", int.TryParse(kvp.Value, out var amt) ? amt : 0);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // Survey Helpers
        // ═══════════════════════════════════════════════════════════

        private static Survey ReadSurveyParent(SqliteDataReader reader)
        {
            var survey = new Survey
            {
                UUID = reader["UUID"] as string,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                BaseItemTypeID = reader["BaseItemTypeID"] as string ?? string.Empty,
                Name = reader["Name"] as string ?? string.Empty,
                NickName = reader["NickName"] as string ?? string.Empty,
                Description = reader["Description"] as string ?? string.Empty,
                Quantity = Convert.ToInt32(reader["Quantity"]),
                Volume = Convert.ToDecimal(reader["Volume"]),
                ScannedBy = reader["ScannedBy"] as string,
                DateTime = reader["DateTime"] as string,
                PlanetName = reader["PlanetName"] as string,
                SystemName = reader["SystemName"] as string ?? string.Empty,
                SurveyID = reader["SurveyID"] as string,
                ScannerBlueprintUUID = reader["ScannerBlueprintUUID"] as string,
                AsteroidUUID = reader["AsteroidUUID"] as string ?? string.Empty,
                SystemObjectId = Convert.ToInt32(reader["SystemObjectId"]),
            };

            var itemTypeStr = reader["ItemType"] as string;
            if (Enum.TryParse<ItemType.ItemTypeEnum>(itemTypeStr, true, out var it))
            {
                survey.ItemType = it;
            }

            var surveyTypeStr = reader["SurveyType"] as string;
            if (Enum.TryParse<SurveyType>(surveyTypeStr, true, out var st))
            {
                survey.SurveyType = st;
            }

            if (reader["GameApiSurveyId"] != DBNull.Value)
            {
                survey.GameApiSurveyId = Convert.ToInt32(reader["GameApiSurveyId"]);
            }

            if (reader["LastDetailImportUtc"] != DBNull.Value)
            {
                var dtStr = reader["LastDetailImportUtc"] as string;
                if (dtStr != null)
                {
                    survey.LastDetailImportUtc = DateTime.Parse(dtStr);
                }
            }

            return survey;
        }

        private static Dictionary<string, string> LoadSurveyProperties(SqliteConnection conn, string surveyUUID)
        {
            var props = new Dictionary<string, string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Key, Value FROM SurveyProperties WHERE SurveyUUID = @sUUID";
                cmd.Parameters.AddWithValue("@sUUID", surveyUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        props[reader.GetString(0)] = reader.GetString(1);
                    }
                }
            }

            return props;
        }

        private static Dictionary<string, SurveyResource> LoadSurveyResources(SqliteConnection conn, string surveyUUID)
        {
            var resources = new Dictionary<string, SurveyResource>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ResourceKey, Resource, Purity, Amount FROM SurveyResources WHERE SurveyUUID = @sUUID";
                cmd.Parameters.AddWithValue("@sUUID", surveyUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var key = reader.GetString(0);
                        resources[key] = new SurveyResource
                        {
                            Resource = reader["Resource"] as string ?? string.Empty,
                            Purity = reader["Purity"] as string ?? string.Empty,
                            Amount = Convert.ToInt32(reader["Amount"]).ToString(),
                        };
                    }
                }
            }

            return resources;
        }

        private static void UpsertSurveyParent(SqliteConnection conn, SqliteTransaction tx, string characterUUID, Survey entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO Surveys (
                    UUID, OwnerUUID, ItemType, BaseItemTypeID, Name, NickName,
                    Description, Quantity, Volume, ScannedBy, DateTime,
                    PlanetName, SystemName, SurveyID, ScannerBlueprintUUID,
                    SurveyType, AsteroidUUID, SystemObjectId,
                    GameApiSurveyId, LastDetailImportUtc
                ) VALUES (
                    @uuid, @owner, @itemType, @baseId, @name, @nick,
                    @desc, @qty, @vol, @scannedBy, @dateTime,
                    @planet, @system, @surveyId, @scannerBp,
                    @surveyType, @asteroidUUID, @sysObjId,
                    @gameApiId, @lastImport
                )";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@itemType", entity.ItemType.ToString());
                cmd.Parameters.AddWithValue("@baseId", entity.BaseItemTypeID ?? string.Empty);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@nick", entity.NickName ?? string.Empty);
                cmd.Parameters.AddWithValue("@desc", entity.Description ?? string.Empty);
                cmd.Parameters.AddWithValue("@qty", entity.Quantity);
                cmd.Parameters.AddWithValue("@vol", (double)entity.Volume);
                cmd.Parameters.AddWithValue("@scannedBy", (object)entity.ScannedBy ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dateTime", (object)entity.DateTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@planet", (object)entity.PlanetName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@system", entity.SystemName ?? string.Empty);
                cmd.Parameters.AddWithValue("@surveyId", (object)entity.SurveyID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@scannerBp", (object)entity.ScannerBlueprintUUID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@surveyType", entity.SurveyType.ToString());
                cmd.Parameters.AddWithValue("@asteroidUUID", entity.AsteroidUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@sysObjId", entity.SystemObjectId);
                cmd.Parameters.AddWithValue("@gameApiId", entity.GameApiSurveyId.HasValue ? (object)entity.GameApiSurveyId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@lastImport", entity.LastDetailImportUtc.HasValue ? (object)entity.LastDetailImportUtc.Value.ToString("O") : DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteSurveyChildren(SqliteConnection conn, SqliteTransaction tx, string surveyUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM SurveyProperties WHERE SurveyUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", surveyUUID);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM SurveyResources WHERE SurveyUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", surveyUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertSurveyChildren(SqliteConnection conn, SqliteTransaction tx, Survey entity)
        {
            if (entity.Properties != null)
            {
                foreach (var kvp in entity.Properties)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "INSERT INTO SurveyProperties (SurveyUUID, Key, Value) VALUES (@sUUID, @key, @val)";
                        cmd.Parameters.AddWithValue("@sUUID", entity.UUID);
                        cmd.Parameters.AddWithValue("@key", kvp.Key);
                        cmd.Parameters.AddWithValue("@val", kvp.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            if (entity.Resources != null)
            {
                foreach (var kvp in entity.Resources)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "INSERT INTO SurveyResources (SurveyUUID, ResourceKey, Resource, Purity, Amount) VALUES (@sUUID, @resKey, @res, @purity, @amount)";
                        cmd.Parameters.AddWithValue("@sUUID", entity.UUID);
                        cmd.Parameters.AddWithValue("@resKey", kvp.Key);
                        cmd.Parameters.AddWithValue("@res", kvp.Value.Resource ?? string.Empty);
                        cmd.Parameters.AddWithValue("@purity", kvp.Value.Purity ?? string.Empty);
                        cmd.Parameters.AddWithValue("@amount", int.TryParse(kvp.Value.Amount, out var amt) ? amt : 0);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // PlayerProfile Helpers
        // ═══════════════════════════════════════════════════════════

        private static PlayerProfile ReadPlayerProfileParent(SqliteDataReader reader)
        {
            var profile = new PlayerProfile
            {
                UUID = reader["UUID"] as string ?? string.Empty,
                Name = reader["Name"] as string ?? string.Empty,
                Faction = reader["Faction"] as string ?? string.Empty,
                FactionUUID = reader["FactionUUID"] as string ?? string.Empty,
                TotalCredits = Convert.ToDecimal(reader["TotalCredits"]),
                SkillPoints = Convert.ToInt32(reader["SkillPoints"]),
                CitizenId = reader["CitizenId"] as string ?? string.Empty,
                RegistrationDate = reader["RegistrationDate"] as string ?? string.Empty,
                ActiveTime = reader["ActiveTime"] as string ?? string.Empty,
                CharacterId = Convert.ToInt32(reader["CharacterId"]),
                FirstName = reader["FirstName"] as string ?? string.Empty,
                LastName = reader["LastName"] as string ?? string.Empty,
                ActiveTimeMinutes = Convert.ToInt32(reader["ActiveTimeMinutes"]),
                Public = new PlayerRank
                {
                    Rank = Convert.ToInt32(reader["PublicRank_Rank"]),
                    CurrentXp = Convert.ToInt64(reader["PublicRank_CurrentXp"]),
                    XpToNextLevel = Convert.ToInt64(reader["PublicRank_XpToNextLevel"]),
                    RankName = reader["PublicRank_RankName"] as string ?? string.Empty,
                },
                Private = new PlayerRank
                {
                    Rank = Convert.ToInt32(reader["PrivateRank_Rank"]),
                    CurrentXp = Convert.ToInt64(reader["PrivateRank_CurrentXp"]),
                    XpToNextLevel = Convert.ToInt64(reader["PrivateRank_XpToNextLevel"]),
                    RankName = reader["PrivateRank_RankName"] as string ?? string.Empty,
                },
                Military = new PlayerRank
                {
                    Rank = Convert.ToInt32(reader["MilitaryRank_Rank"]),
                    CurrentXp = Convert.ToInt64(reader["MilitaryRank_CurrentXp"]),
                    XpToNextLevel = Convert.ToInt64(reader["MilitaryRank_XpToNextLevel"]),
                    RankName = reader["MilitaryRank_RankName"] as string ?? string.Empty,
                },
            };

            return profile;
        }

        private static Dictionary<string, PlayerSkill> LoadPlayerSkills(SqliteConnection conn, string playerUUID)
        {
            var skills = new Dictionary<string, PlayerSkill>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM PlayerSkills WHERE PlayerUUID = @pUUID";
                cmd.Parameters.AddWithValue("@pUUID", playerUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var skillName = reader["SkillName"] as string ?? string.Empty;
                        var skill = new PlayerSkill
                        {
                            Level = Convert.ToInt32(reader["Level"]),
                            TrainingStarted = Convert.ToInt32(reader["TrainingStarted"]) != 0,
                            SkillId = Convert.ToInt32(reader["SkillId"]),
                            EffectDescription = reader["EffectDescription"] as string ?? string.Empty,
                            AmountPerLevel = Convert.ToInt32(reader["AmountPerLevel"]),
                            SkillGroupName = reader["SkillGroupName"] as string ?? string.Empty,
                            IsUnlocked = Convert.ToInt32(reader["IsUnlocked"]) != 0,
                            TargetLevel = Convert.ToInt32(reader["TargetLevel"]),
                            TrainingPercentageComplete = Convert.ToInt32(reader["TrainingPercentageComplete"]),
                            RemainingMinutes = Convert.ToInt32(reader["RemainingMinutes"]),
                        };

                        var completionStart = reader["Completion_StartTime"] as string;
                        if (completionStart != null)
                        {
                            skill.CompletionTime = new CountDownTime
                            {
                                StartTime = DateTime.Parse(completionStart),
                                RepeatIntervalSeconds = reader["Completion_RepeatIntervalSeconds"] == DBNull.Value ? 0 : Convert.ToInt64(reader["Completion_RepeatIntervalSeconds"]),
                            };
                        }

                        skills[skillName] = skill;
                    }
                }
            }

            return skills;
        }

        private static void UpsertPlayerProfileParent(SqliteConnection conn, SqliteTransaction tx, PlayerProfile entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO PlayerProfiles (
                    UUID, Name, Faction, FactionUUID, TotalCredits, SkillPoints,
                    CitizenId, RegistrationDate, ActiveTime, CharacterId,
                    FirstName, LastName, ActiveTimeMinutes,
                    PublicRank_Rank, PublicRank_CurrentXp, PublicRank_XpToNextLevel, PublicRank_RankName,
                    PrivateRank_Rank, PrivateRank_CurrentXp, PrivateRank_XpToNextLevel, PrivateRank_RankName,
                    MilitaryRank_Rank, MilitaryRank_CurrentXp, MilitaryRank_XpToNextLevel, MilitaryRank_RankName
                ) VALUES (
                    @uuid, @name, @faction, @factionUUID, @credits, @skillPts,
                    @citizenId, @regDate, @activeTime, @charId,
                    @firstName, @lastName, @activeMin,
                    @pubRank, @pubXp, @pubNext, @pubName,
                    @privRank, @privXp, @privNext, @privName,
                    @milRank, @milXp, @milNext, @milName
                )";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@faction", entity.Faction ?? string.Empty);
                cmd.Parameters.AddWithValue("@factionUUID", entity.FactionUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@credits", (double)entity.TotalCredits);
                cmd.Parameters.AddWithValue("@skillPts", entity.SkillPoints);
                cmd.Parameters.AddWithValue("@citizenId", entity.CitizenId ?? string.Empty);
                cmd.Parameters.AddWithValue("@regDate", entity.RegistrationDate ?? string.Empty);
                cmd.Parameters.AddWithValue("@activeTime", entity.ActiveTime ?? string.Empty);
                cmd.Parameters.AddWithValue("@charId", entity.CharacterId);
                cmd.Parameters.AddWithValue("@firstName", entity.FirstName ?? string.Empty);
                cmd.Parameters.AddWithValue("@lastName", entity.LastName ?? string.Empty);
                cmd.Parameters.AddWithValue("@activeMin", entity.ActiveTimeMinutes);
                cmd.Parameters.AddWithValue("@pubRank", entity.Public?.Rank ?? 0);
                cmd.Parameters.AddWithValue("@pubXp", entity.Public?.CurrentXp ?? 0L);
                cmd.Parameters.AddWithValue("@pubNext", entity.Public?.XpToNextLevel ?? 0L);
                cmd.Parameters.AddWithValue("@pubName", entity.Public?.RankName ?? string.Empty);
                cmd.Parameters.AddWithValue("@privRank", entity.Private?.Rank ?? 0);
                cmd.Parameters.AddWithValue("@privXp", entity.Private?.CurrentXp ?? 0L);
                cmd.Parameters.AddWithValue("@privNext", entity.Private?.XpToNextLevel ?? 0L);
                cmd.Parameters.AddWithValue("@privName", entity.Private?.RankName ?? string.Empty);
                cmd.Parameters.AddWithValue("@milRank", entity.Military?.Rank ?? 0);
                cmd.Parameters.AddWithValue("@milXp", entity.Military?.CurrentXp ?? 0L);
                cmd.Parameters.AddWithValue("@milNext", entity.Military?.XpToNextLevel ?? 0L);
                cmd.Parameters.AddWithValue("@milName", entity.Military?.RankName ?? string.Empty);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeletePlayerProfileChildren(SqliteConnection conn, SqliteTransaction tx, string playerUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM PlayerSkills WHERE PlayerUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", playerUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertPlayerSkills(SqliteConnection conn, SqliteTransaction tx, PlayerProfile entity)
        {
            if (entity.Skills == null)
            {
                return;
            }

            foreach (var kvp in entity.Skills)
            {
                var skillName = kvp.Key;
                var skill = kvp.Value;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO PlayerSkills (
                        PlayerUUID, SkillName, Level, TrainingStarted, SkillId,
                        EffectDescription, AmountPerLevel, SkillGroupName, IsUnlocked,
                        TargetLevel, TrainingPercentageComplete, RemainingMinutes,
                        Completion_StartTime, Completion_RepeatIntervalSeconds, Completion_IsRepeating
                    ) VALUES (
                        @pUUID, @skillName, @level, @training, @skillId,
                        @effect, @amount, @group, @unlocked,
                        @target, @pct, @remaining,
                        @cStart, @cInterval, @cRepeat
                    )";
                    cmd.Parameters.AddWithValue("@pUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@skillName", skillName);
                    cmd.Parameters.AddWithValue("@level", skill.Level);
                    cmd.Parameters.AddWithValue("@training", skill.TrainingStarted ? 1 : 0);
                    cmd.Parameters.AddWithValue("@skillId", skill.SkillId);
                    cmd.Parameters.AddWithValue("@effect", skill.EffectDescription ?? string.Empty);
                    cmd.Parameters.AddWithValue("@amount", skill.AmountPerLevel);
                    cmd.Parameters.AddWithValue("@group", skill.SkillGroupName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@unlocked", skill.IsUnlocked ? 1 : 0);
                    cmd.Parameters.AddWithValue("@target", skill.TargetLevel);
                    cmd.Parameters.AddWithValue("@pct", skill.TrainingPercentageComplete);
                    cmd.Parameters.AddWithValue("@remaining", skill.RemainingMinutes);

                    if (skill.CompletionTime != null && skill.CompletionTime.StartTime != DateTime.MinValue)
                    {
                        cmd.Parameters.AddWithValue("@cStart", skill.CompletionTime.StartTime.ToString("O"));
                        cmd.Parameters.AddWithValue("@cInterval", skill.CompletionTime.RepeatIntervalSeconds);
                        cmd.Parameters.AddWithValue("@cRepeat", skill.CompletionTime.IsRepeating ? 1 : 0);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@cStart", DBNull.Value);
                        cmd.Parameters.AddWithValue("@cInterval", DBNull.Value);
                        cmd.Parameters.AddWithValue("@cRepeat", DBNull.Value);
                    }

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // DeliveryRoute Helpers
        // ═══════════════════════════════════════════════════════════

        private static DeliveryRoute ReadDeliveryRouteParent(SqliteDataReader reader)
        {
            return new DeliveryRoute
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
            };
        }

        private static List<RouteStop> LoadDeliveryRouteStops(SqliteConnection conn, string routeUUID)
        {
            var stops = new List<RouteStop>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM DeliveryRouteStops WHERE DeliveryRouteUUID = @rUUID ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@rUUID", routeUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var stop = new RouteStop
                        {
                            Sequence = Convert.ToInt32(reader["Sequence"]),
                            ColonyUUID = reader["ColonyUUID"] as string ?? string.Empty,
                            DestinationUUID = reader["DestinationUUID"] as string ?? string.Empty,
                            FuelEstimate = Convert.ToDecimal(reader["FuelEstimate"]),
                        };

                        var destTypeStr = reader["DestinationType"] as string;
                        if (Enum.TryParse<DestinationType>(destTypeStr, true, out var dt))
                        {
                            stop.DestinationType = dt;
                        }

                        var purposeStr = reader["Purpose"] as string;
                        if (Enum.TryParse<RouteStopPurpose>(purposeStr, true, out var purpose))
                        {
                            stop.Purpose = purpose;
                        }

                        stops.Add(stop);
                    }
                }
            }

            return stops;
        }

        private static void UpsertDeliveryRouteParent(SqliteConnection conn, SqliteTransaction tx, string characterUUID, DeliveryRoute entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO DeliveryRoutes (UUID, Name, OwnerUUID)
                                   VALUES (@uuid, @name, @owner)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteDeliveryRouteChildren(SqliteConnection conn, SqliteTransaction tx, string routeUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM DeliveryRouteStops WHERE DeliveryRouteUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", routeUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertDeliveryRouteStops(SqliteConnection conn, SqliteTransaction tx, DeliveryRoute entity)
        {
            if (entity.Stops == null)
            {
                return;
            }

            for (int i = 0; i < entity.Stops.Count; i++)
            {
                var stop = entity.Stops[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO DeliveryRouteStops (
                        DeliveryRouteUUID, Sequence, ColonyUUID, DestinationType, DestinationUUID, Purpose, FuelEstimate
                    ) VALUES (
                        @rUUID, @seq, @colUUID, @destType, @destUUID, @purpose, @fuel
                    )";
                    cmd.Parameters.AddWithValue("@rUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", stop.Sequence);
                    cmd.Parameters.AddWithValue("@colUUID", stop.ColonyUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@destType", stop.DestinationType.ToString());
                    cmd.Parameters.AddWithValue("@destUUID", stop.DestinationUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@purpose", stop.Purpose.ToString());
                    cmd.Parameters.AddWithValue("@fuel", (double)stop.FuelEstimate);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // DeliveryPlan Helpers
        // ═══════════════════════════════════════════════════════════

        private static DeliveryPlan ReadDeliveryPlanParent(SqliteDataReader reader)
        {
            return new DeliveryPlan
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                RouteUUID = reader["RouteUUID"] as string ?? string.Empty,
                ShipUUID = reader["ShipUUID"] as string ?? string.Empty,
                Completed = Convert.ToInt32(reader["Completed"]) != 0,
            };
        }

        private static List<DeliveryPlanStop> LoadDeliveryPlanStops(SqliteConnection conn, string planUUID)
        {
            var stops = new List<DeliveryPlanStop>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM DeliveryPlanStops WHERE DeliveryPlanUUID = @pUUID ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@pUUID", planUUID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var stop = new DeliveryPlanStop
                        {
                            Sequence = Convert.ToInt32(reader["Sequence"]),
                            ColonyUUID = reader["ColonyUUID"] as string ?? string.Empty,
                            StopCompleted = Convert.ToInt32(reader["StopCompleted"]) != 0,
                            DestinationUUID = reader["DestinationUUID"] as string ?? string.Empty,
                        };

                        var destTypeStr = reader["DestinationType"] as string;
                        if (Enum.TryParse<DestinationType>(destTypeStr, true, out var dt))
                        {
                            stop.DestinationType = dt;
                        }

                        stops.Add(stop);
                    }
                }
            }

            // Load items for each stop
            foreach (var stop in stops)
            {
                LoadDeliveryPlanItems(conn, planUUID, stop);
            }

            return stops;
        }

        private static void LoadDeliveryPlanItems(SqliteConnection conn, string planUUID, DeliveryPlanStop stop)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM DeliveryPlanItems WHERE DeliveryPlanUUID = @pUUID AND StopSequence = @seq ORDER BY Direction, Sequence";
                cmd.Parameters.AddWithValue("@pUUID", planUUID);
                cmd.Parameters.AddWithValue("@seq", stop.Sequence);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = new DeliveryItem
                        {
                            BaseItemTypeID = reader["BaseItemTypeID"] as string ?? string.Empty,
                            Name = reader["Name"] as string ?? string.Empty,
                            ResourcePurity = reader["ResourcePurity"] as string ?? string.Empty,
                            Quantity = Convert.ToInt32(reader["Quantity"]),
                            Delivered = Convert.ToInt32(reader["Delivered"]) != 0,
                        };

                        var itemTypeStr = reader["ItemType"] as string;
                        if (Enum.TryParse<ItemType.ItemTypeEnum>(itemTypeStr, true, out var it))
                        {
                            item.ItemType = it;
                        }

                        var direction = reader["Direction"] as string;
                        if (string.Equals(direction, "DropOff", StringComparison.OrdinalIgnoreCase))
                        {
                            stop.DropOff.Add(item);
                        }
                        else
                        {
                            stop.PickUp.Add(item);
                        }
                    }
                }
            }
        }

        private static void UpsertDeliveryPlanParent(SqliteConnection conn, SqliteTransaction tx, string characterUUID, DeliveryPlan entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO DeliveryPlans (UUID, Name, OwnerUUID, RouteUUID, ShipUUID, Completed)
                                   VALUES (@uuid, @name, @owner, @route, @ship, @completed)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@route", entity.RouteUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@ship", entity.ShipUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@completed", entity.Completed ? 1 : 0);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeleteDeliveryPlanChildren(SqliteConnection conn, SqliteTransaction tx, string planUUID)
        {
            // Delete items first (they reference stops)
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM DeliveryPlanItems WHERE DeliveryPlanUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", planUUID);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM DeliveryPlanStops WHERE DeliveryPlanUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", planUUID);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertDeliveryPlanChildren(SqliteConnection conn, SqliteTransaction tx, DeliveryPlan entity)
        {
            if (entity.Stops == null)
            {
                return;
            }

            foreach (var stop in entity.Stops)
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO DeliveryPlanStops (
                        DeliveryPlanUUID, Sequence, ColonyUUID, StopCompleted, DestinationType, DestinationUUID
                    ) VALUES (
                        @pUUID, @seq, @colUUID, @completed, @destType, @destUUID
                    )";
                    cmd.Parameters.AddWithValue("@pUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", stop.Sequence);
                    cmd.Parameters.AddWithValue("@colUUID", stop.ColonyUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@completed", stop.StopCompleted ? 1 : 0);
                    cmd.Parameters.AddWithValue("@destType", stop.DestinationType.ToString());
                    cmd.Parameters.AddWithValue("@destUUID", stop.DestinationUUID ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }

                // Insert drop-off items
                InsertDeliveryPlanItemList(conn, tx, entity.UUID, stop.Sequence, "DropOff", stop.DropOff);

                // Insert pick-up items
                InsertDeliveryPlanItemList(conn, tx, entity.UUID, stop.Sequence, "PickUp", stop.PickUp);
            }
        }

        private static void InsertDeliveryPlanItemList(SqliteConnection conn, SqliteTransaction tx, string planUUID, int stopSequence, string direction, List<DeliveryItem> items)
        {
            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO DeliveryPlanItems (
                        DeliveryPlanUUID, StopSequence, Direction, Sequence,
                        ItemType, BaseItemTypeID, Name, ResourcePurity, Quantity, Delivered
                    ) VALUES (
                        @pUUID, @stopSeq, @dir, @seq,
                        @itemType, @baseId, @name, @purity, @qty, @delivered
                    )";
                    cmd.Parameters.AddWithValue("@pUUID", planUUID);
                    cmd.Parameters.AddWithValue("@stopSeq", stopSequence);
                    cmd.Parameters.AddWithValue("@dir", direction);
                    cmd.Parameters.AddWithValue("@seq", i);
                    cmd.Parameters.AddWithValue("@itemType", item.ItemType.ToString());
                    cmd.Parameters.AddWithValue("@baseId", item.BaseItemTypeID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@name", item.Name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@purity", item.ResourcePurity ?? string.Empty);
                    cmd.Parameters.AddWithValue("@qty", item.Quantity);
                    cmd.Parameters.AddWithValue("@delivered", item.Delivered ? 1 : 0);
                    cmd.ExecuteNonQuery();
                }
            }
        }

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
