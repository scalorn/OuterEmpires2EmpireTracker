// -----------------------------------------------------------------------
// <copyright file="SqliteBackend.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using NLog;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Common.Storage
{
    /// <summary>
    /// IStorageBackend implementation that persists all data in a single SQLite
    /// database file using a fully normalized relational schema.
    /// </summary>
    internal class SqliteBackend : IStorageBackend
    {
        private const int CurrentSchemaVersion = 3;

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
    PricePerUnit TEXT NOT NULL DEFAULT '0',
    TotalPrice TEXT NOT NULL DEFAULT '0',
    Counterparty TEXT NOT NULL DEFAULT '',
    CounterpartyFaction TEXT NOT NULL DEFAULT '',
    StationUUID TEXT NOT NULL DEFAULT '',
    Timestamp TEXT NOT NULL DEFAULT '',
    Notes TEXT NOT NULL DEFAULT '',
    ListingUUID TEXT NOT NULL DEFAULT '',
    CurrentHP INTEGER NOT NULL DEFAULT 0,
    MaxHP INTEGER NOT NULL DEFAULT 0,
    MaxRepairPercent TEXT NOT NULL DEFAULT '0'
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
    CreditChange TEXT NOT NULL DEFAULT '0',
    OldBalance TEXT NOT NULL DEFAULT '0',
    NewBalance TEXT NOT NULL DEFAULT '0',
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

        /// <summary>
        /// Ordered list of migration actions. Each entry migrates from version N to
        /// version N+1 (i.e. Migrations[0] migrates v1 â†’ v2). Currently empty because
        /// the schema is at version 1 with no prior versions to migrate from.
        /// </summary>
        private static readonly List<Action<SqliteConnection, SqliteTransaction>> Migrations = new List<Action<SqliteConnection, SqliteTransaction>>
        {
            // Version 1 â†’ 2: BankingTransactions decimal columns REAL â†’ TEXT
            MigrateBankingTransactionsDecimalToText,

            // Version 2 â†’ 3: MarketTransactions decimal columns REAL â†’ TEXT
            MigrateMarketTransactionsDecimalToText,
        };

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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Lifecycle
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task InitializeAsync(CancellationToken ct = default)
        {
            try
            {
                using (var conn = OpenConnection())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "CREATE TABLE IF NOT EXISTS _metadata (Key TEXT PRIMARY KEY, Value TEXT NOT NULL);";
                        ExecuteNonQueryLogged(cmd);
                    }

                    int version = GetSchemaVersion(conn);
                    if (version == 0)
                    {
                        ExecuteSchema(conn);
                        SetSchemaVersion(conn, CurrentSchemaVersion);
                        Log.Info("SQLite database initialized with schema version {0}", CurrentSchemaVersion);
                    }
                    else if (version < CurrentSchemaVersion)
                    {
                        RunMigrations(conn, version, _databasePath);
                    }
                    else
                    {
                        Log.Debug("SQLite database already at schema version {0}", version);
                    }

                    if (DetectLegacySchema(conn))
                    {
                        MigrateLegacyData(conn);
                    }
                }
            }
            catch (StorageLoadException)
            {
                throw;
            }
            catch (StorageCorruptionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new StorageLoadException(
                    "Sqlite",
                    _databasePath,
                    "Failed to initialize SQLite database",
                    ex);
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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Server Factions
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<ServerFaction> GetFactionAsync(string uuid)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT UUID, Name, Description, Metadata_LastModifiedUtc, Metadata_ModifiedByTokenId FROM ServerFactions WHERE UUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", uuid);
                using (var reader = ExecuteReaderLogged(cmd))
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
                    using (var reader = ExecuteReaderLogged(cmd))
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
                    ExecuteNonQueryLogged(cmd);
                }

                using (var delCmd = conn.CreateCommand())
                {
                    delCmd.Transaction = tx;
                    delCmd.CommandText = "DELETE FROM ServerFactionLeaders WHERE FactionUUID = @uuid";
                    delCmd.Parameters.AddWithValue("@uuid", faction.UUID);
                    ExecuteNonQueryLogged(delCmd);
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
                            ExecuteNonQueryLogged(insCmd);
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
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Server Characters
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<ServerCharacter> GetCharacterAsync(string uuid)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT UUID, Name, FactionUUID, Metadata_LastModifiedUtc, Metadata_ModifiedByTokenId FROM ServerCharacters WHERE UUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", uuid);
                using (var reader = ExecuteReaderLogged(cmd))
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
                using (var reader = ExecuteReaderLogged(cmd))
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
                ExecuteNonQueryLogged(cmd);
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
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Global Data
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<string> GetGlobalDataAsync(string dataType)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Value FROM _metadata WHERE Key = @key";
                cmd.Parameters.AddWithValue("@key", "global_" + dataType);
                var result = ExecuteScalarLogged(cmd);
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
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Star Systems
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<StarSystem>> GetAllStarSystemsAsync()
        {
            var results = new List<StarSystem>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, Name, X, Y, Quadrant, Sector, Region, Locality, SpectralClass, FactionId, FactionName, FactionColor, HasOrbital, HasSpaceport, HasStarbase FROM StarSystems";
                using (var reader = ExecuteReaderLogged(cmd))
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
                    ExecuteNonQueryLogged(delCmd);
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
                        ExecuteNonQueryLogged(cmd);
                    }
                }

                tx.Commit();
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Colony Summaries
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<ColonySummary>> GetColonySummariesForSystemAsync(int systemId)
        {
            var results = new List<ColonySummary>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ColonyName, ColonySize, PlanetName FROM Colonies WHERE SystemId = @systemId";
                cmd.Parameters.AddWithValue("@systemId", systemId);
                using (var reader = ExecuteReaderLogged(cmd))
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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // API Tokens
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<ApiToken> FindTokenByHashAsync(string tokenHash)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, TokenHash, CharacterUUID, Role, FactionUUID, CreatedUtc, LastUsedUtc, IsRevoked, RateLimits_RequestsPerMinute FROM ApiTokens WHERE TokenHash = @hash";
                cmd.Parameters.AddWithValue("@hash", tokenHash);
                using (var reader = ExecuteReaderLogged(cmd))
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
                using (var reader = ExecuteReaderLogged(cmd))
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
                ExecuteNonQueryLogged(cmd);
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
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Membership Actions
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<MembershipAction>> GetFactionActionsAsync(string factionUUID)
        {
            var results = new List<MembershipAction>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, FactionUUID, CharacterUUID, Type, CreatedUtc, ExpiresUtc FROM MembershipActions WHERE FactionUUID = @fid";
                cmd.Parameters.AddWithValue("@fid", factionUUID);
                using (var reader = ExecuteReaderLogged(cmd))
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
                ExecuteNonQueryLogged(cmd);
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
                ExecuteNonQueryLogged(cmd);
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
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Sharing Rules
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<SharingRule>> GetSharingRulesForCharacterAsync(string characterUUID)
        {
            var results = new List<SharingRule>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, OwnerCharacterUUID, TargetUUID, TargetType, DataType, EntityUUID FROM SharingRules WHERE OwnerCharacterUUID = @cid";
                cmd.Parameters.AddWithValue("@cid", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
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
                    ExecuteNonQueryLogged(delCmd);
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
                        ExecuteNonQueryLogged(cmd);
                    }
                }

                tx.Commit();
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Character Preferences
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<CharacterPreferences> GetCharacterPreferencesAsync(string characterUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT CharacterUUID, ServerProcessing FROM CharacterPreferences WHERE CharacterUUID = @cid";
                cmd.Parameters.AddWithValue("@cid", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
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
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Character Discovery
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<string>> GetAllCharacterUUIDsAsync()
        {
            var results = new List<string>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT DISTINCT OwnerUUID FROM Colonies
UNION SELECT DISTINCT OwnerUUID FROM Blueprints
UNION SELECT DISTINCT OwnerUUID FROM Surveys
UNION SELECT DISTINCT UUID FROM PlayerProfiles
UNION SELECT DISTINCT OwnerUUID FROM DeliveryRoutes
UNION SELECT DISTINCT OwnerUUID FROM DeliveryPlans
UNION SELECT DISTINCT OwnerUUID FROM Ships
UNION SELECT DISTINCT OwnerUUID FROM ShipTemplates
UNION SELECT DISTINCT OwnerUUID FROM MarketListings
UNION SELECT DISTINCT OwnerUUID FROM MarketTransactions
UNION SELECT DISTINCT OwnerUUID FROM PricingPlans
UNION SELECT DISTINCT OwnerUUID FROM StockPlans
UNION SELECT DISTINCT OwnerUUID FROM StockProfiles
UNION SELECT DISTINCT OwnerUUID FROM BuildPlans
UNION SELECT DISTINCT OwnerUUID FROM SupplyChains
UNION SELECT DISTINCT OwnerUUID FROM Asteroids
UNION SELECT DISTINCT OwnerUUID FROM Stations
UNION SELECT DISTINCT OwnerUUID FROM Factions
UNION SELECT DISTINCT OwnerUUID FROM ExternalCharacters
UNION SELECT DISTINCT OwnerUUID FROM WarehouseOverflowRules
UNION SELECT DISTINCT OwnerUUID FROM MailMessages
UNION SELECT DISTINCT OwnerUUID FROM BankingTransactions";

                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        var uuid = reader.GetString(0);
                        if (!string.IsNullOrEmpty(uuid))
                        {
                            results.Add(uuid);
                        }
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<string>>(results);
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” Colony
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<Colony>> GetAllColoniesAsync(string characterUUID)
        {
            var results = new List<Colony>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Colonies WHERE OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
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
                using (var reader = ExecuteReaderLogged(cmd))
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
                    ExecuteNonQueryLogged(delItems);
                }

                // CASCADE handles ColonyStructures, ColonyStructureProperties, ColonyStructureWorkers
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Colonies WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    ExecuteNonQueryLogged(cmd);
                }
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” Blueprint
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<Blueprint>> GetAllBlueprintsAsync(string characterUUID)
        {
            var results = new List<Blueprint>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Blueprints WHERE OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
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
                using (var reader = ExecuteReaderLogged(cmd))
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
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” Survey
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<Survey>> GetAllSurveysAsync(string characterUUID)
        {
            var results = new List<Survey>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Surveys WHERE OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
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
                using (var reader = ExecuteReaderLogged(cmd))
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
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” PlayerProfile
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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
                    using (var reader = ExecuteReaderLogged(cmd))
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
                using (var reader = ExecuteReaderLogged(cmd))
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
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” DeliveryRoute
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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
                    using (var reader = ExecuteReaderLogged(cmd))
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
                using (var reader = ExecuteReaderLogged(cmd))
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
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” DeliveryPlan
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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
                    using (var reader = ExecuteReaderLogged(cmd))
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
                using (var reader = ExecuteReaderLogged(cmd))
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
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” Ship
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<Ship>> GetAllShipsAsync(string characterUUID)
        {
            var results = new List<Ship>();
            using (var conn = OpenConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Ships WHERE OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = ExecuteReaderLogged(cmd))
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadShipParent(reader));
                        }
                    }
                }

                foreach (var ship in results)
                {
                    ship.Components = LoadShipComponents(conn, ship.UUID);
                    ship.Cargo = LoadItems(conn, ship.UUID, "ShipCargo");
                    ship.Hopper = LoadItems(conn, ship.UUID, "ShipHopper");
                }
            }

            return Task.FromResult<IReadOnlyList<Ship>>(results);
        }

        /// <inheritdoc/>
        public Task<Ship> GetShipAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Ships WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        var ship = ReadShipParent(reader);
                        ship.Components = LoadShipComponents(conn, ship.UUID);
                        ship.Cargo = LoadItems(conn, ship.UUID, "ShipCargo");
                        ship.Hopper = LoadItems(conn, ship.UUID, "ShipHopper");
                        return Task.FromResult(ship);
                    }
                }
            }

            return Task.FromResult<Ship>(null);
        }

        /// <inheritdoc/>
        public Task UpsertShipAsync(string characterUUID, Ship entity)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                UpsertShipParent(conn, tx, characterUUID, entity);
                DeleteShipChildren(conn, tx, entity.UUID);
                InsertShipComponents(conn, tx, entity);
                InsertItems(conn, tx, entity.UUID, "ShipCargo", entity.Cargo);
                InsertItems(conn, tx, entity.UUID, "ShipHopper", entity.Hopper);
                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteShipAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            {
                // Delete items first (polymorphic FK, no CASCADE)
                using (var delItems = conn.CreateCommand())
                {
                    delItems.CommandText = "DELETE FROM Items WHERE ParentUUID = @uuid AND (ParentType = 'ShipCargo' OR ParentType = 'ShipHopper')";
                    delItems.Parameters.AddWithValue("@uuid", entityUUID);
                    ExecuteNonQueryLogged(delItems);
                }

                // CASCADE handles ShipComponents
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Ships WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    ExecuteNonQueryLogged(cmd);
                }
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” ShipTemplate
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<ShipTemplate>> GetAllShipTemplatesAsync(string characterUUID)
        {
            var results = new List<ShipTemplate>();
            using (var conn = OpenConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM ShipTemplates WHERE OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = ExecuteReaderLogged(cmd))
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadShipTemplateParent(reader));
                        }
                    }
                }

                foreach (var template in results)
                {
                    template.Components = LoadShipTemplateComponents(conn, template.UUID);
                }
            }

            return Task.FromResult<IReadOnlyList<ShipTemplate>>(results);
        }

        /// <inheritdoc/>
        public Task<ShipTemplate> GetShipTemplateAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM ShipTemplates WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        var template = ReadShipTemplateParent(reader);
                        template.Components = LoadShipTemplateComponents(conn, template.UUID);
                        return Task.FromResult(template);
                    }
                }
            }

            return Task.FromResult<ShipTemplate>(null);
        }

        /// <inheritdoc/>
        public Task UpsertShipTemplateAsync(string characterUUID, ShipTemplate entity)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                UpsertShipTemplateParent(conn, tx, characterUUID, entity);
                DeleteShipTemplateChildren(conn, tx, entity.UUID);
                InsertShipTemplateComponents(conn, tx, entity);
                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteShipTemplateAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                // CASCADE handles ShipTemplateComponents
                cmd.CommandText = "DELETE FROM ShipTemplates WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” MarketListing
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<MarketListing>> GetAllMarketListingsAsync(string characterUUID)
        {
            var results = new List<MarketListing>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM MarketListings WHERE OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(ReadMarketListing(reader));
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<MarketListing>>(results);
        }

        /// <inheritdoc/>
        public Task<MarketListing> GetMarketListingAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM MarketListings WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        return Task.FromResult(ReadMarketListing(reader));
                    }
                }
            }

            return Task.FromResult<MarketListing>(null);
        }

        /// <inheritdoc/>
        public Task UpsertMarketListingAsync(string characterUUID, MarketListing entity)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO MarketListings (
                    UUID, OwnerUUID, StationUUID, ItemType, ItemReferenceID, ItemName,
                    Quantity, PricePerUnit, CurrentHP, MaxHP, MaxRepairPercent,
                    MarketId, BuyOrder, BaseItemTypeID, ResourcePurity,
                    GameTypeCode, GameTypeId, GameSubTypeId,
                    LocationName, SystemId, SystemName, GameLocationId,
                    AmountRemaining, AmountOriginal, AmountSold,
                    EscrowRemaining, SalesTaxEstimate, ValueRemaining,
                    Evolution, HealthPercentage,
                    SellerName, SellerFactionTag, PrivateSale,
                    BuyerName, BuyerFactionTag,
                    IsOutbid, IsUndercut,
                    PlacedDT, ExpiresDT,
                    CompetitorForMarketId,
                    SyncedByCharacterUUID, SyncTimestamp
                ) VALUES (
                    @uuid, @owner, @stationUUID, @itemType, @itemRefId, @itemName,
                    @qty, @price, @curHp, @maxHp, @maxRepair,
                    @marketId, @buyOrder, @baseItemTypeId, @resPurity,
                    @gameTypeCode, @gameTypeId, @gameSubTypeId,
                    @locName, @systemId, @systemName, @gameLocId,
                    @amtRemain, @amtOrig, @amtSold,
                    @escrow, @salesTax, @valueRemain,
                    @evo, @healthPct,
                    @sellerName, @sellerFaction, @privateSale,
                    @buyerName, @buyerFaction,
                    @isOutbid, @isUndercut,
                    @placedDT, @expiresDT,
                    @competitorId,
                    @syncBy, @syncTs
                )";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@stationUUID", entity.StationUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@itemType", entity.ItemType.ToString());
                cmd.Parameters.AddWithValue("@itemRefId", entity.ItemReferenceID ?? string.Empty);
                cmd.Parameters.AddWithValue("@itemName", entity.ItemName ?? string.Empty);
                cmd.Parameters.AddWithValue("@qty", entity.Quantity);
                cmd.Parameters.AddWithValue("@price", (double)entity.PricePerUnit);
                cmd.Parameters.AddWithValue("@curHp", entity.CurrentHP);
                cmd.Parameters.AddWithValue("@maxHp", entity.MaxHP);
                cmd.Parameters.AddWithValue("@maxRepair", (double)entity.MaxRepairPercent);
                cmd.Parameters.AddWithValue("@marketId", entity.MarketId.HasValue ? (object)entity.MarketId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@buyOrder", entity.BuyOrder ? 1 : 0);
                cmd.Parameters.AddWithValue("@baseItemTypeId", entity.BaseItemTypeID ?? string.Empty);
                cmd.Parameters.AddWithValue("@resPurity", entity.ResourcePurity ?? string.Empty);
                cmd.Parameters.AddWithValue("@gameTypeCode", entity.GameTypeCode ?? string.Empty);
                cmd.Parameters.AddWithValue("@gameTypeId", entity.GameTypeId.HasValue ? (object)entity.GameTypeId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@gameSubTypeId", entity.GameSubTypeId ?? string.Empty);
                cmd.Parameters.AddWithValue("@locName", entity.LocationName ?? string.Empty);
                cmd.Parameters.AddWithValue("@systemId", entity.SystemId.HasValue ? (object)entity.SystemId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@systemName", entity.SystemName ?? string.Empty);
                cmd.Parameters.AddWithValue("@gameLocId", entity.GameLocationId.HasValue ? (object)entity.GameLocationId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@amtRemain", entity.AmountRemaining.HasValue ? (object)entity.AmountRemaining.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@amtOrig", entity.AmountOriginal.HasValue ? (object)entity.AmountOriginal.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@amtSold", entity.AmountSold.HasValue ? (object)entity.AmountSold.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@escrow", entity.EscrowRemaining.HasValue ? (object)(double)entity.EscrowRemaining.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@salesTax", entity.SalesTaxEstimate.HasValue ? (object)(double)entity.SalesTaxEstimate.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@valueRemain", entity.ValueRemaining.HasValue ? (object)(double)entity.ValueRemaining.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@evo", entity.Evolution.HasValue ? (object)entity.Evolution.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@healthPct", entity.HealthPercentage.HasValue ? (object)entity.HealthPercentage.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@sellerName", entity.SellerName ?? string.Empty);
                cmd.Parameters.AddWithValue("@sellerFaction", entity.SellerFactionTag ?? string.Empty);
                cmd.Parameters.AddWithValue("@privateSale", entity.PrivateSale ? 1 : 0);
                cmd.Parameters.AddWithValue("@buyerName", entity.BuyerName ?? string.Empty);
                cmd.Parameters.AddWithValue("@buyerFaction", entity.BuyerFactionTag ?? string.Empty);
                cmd.Parameters.AddWithValue("@isOutbid", entity.IsOutbid ? 1 : 0);
                cmd.Parameters.AddWithValue("@isUndercut", entity.IsUndercut ? 1 : 0);
                cmd.Parameters.AddWithValue("@placedDT", entity.PlacedDT ?? string.Empty);
                cmd.Parameters.AddWithValue("@expiresDT", entity.ExpiresDT ?? string.Empty);
                cmd.Parameters.AddWithValue("@competitorId", entity.CompetitorForMarketId.HasValue ? (object)entity.CompetitorForMarketId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@syncBy", entity.SyncedByCharacterUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@syncTs", entity.SyncTimestamp ?? string.Empty);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteMarketListingAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM MarketListings WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” MarketTransaction
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<MarketTransaction>> GetAllMarketTransactionsAsync(string characterUUID)
        {
            var results = new List<MarketTransaction>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM MarketTransactions WHERE OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(ReadMarketTransaction(reader));
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<MarketTransaction>>(results);
        }

        /// <inheritdoc/>
        public Task<MarketTransaction> GetMarketTransactionAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM MarketTransactions WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        return Task.FromResult(ReadMarketTransaction(reader));
                    }
                }
            }

            return Task.FromResult<MarketTransaction>(null);
        }

        /// <inheritdoc/>
        public Task UpsertMarketTransactionAsync(string characterUUID, MarketTransaction entity)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO MarketTransactions (
                    UUID, OwnerUUID, TransactionType, ItemType, ItemReferenceID, ItemName,
                    Quantity, PricePerUnit, TotalPrice,
                    Counterparty, CounterpartyFaction, StationUUID,
                    Timestamp, Notes, ListingUUID,
                    CurrentHP, MaxHP, MaxRepairPercent
                ) VALUES (
                    @uuid, @owner, @txType, @itemType, @itemRefId, @itemName,
                    @qty, @price, @total,
                    @counterparty, @counterpartyFaction, @stationUUID,
                    @timestamp, @notes, @listingUUID,
                    @curHp, @maxHp, @maxRepair
                )";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@txType", entity.TransactionType.ToString());
                cmd.Parameters.AddWithValue("@itemType", entity.ItemType.ToString());
                cmd.Parameters.AddWithValue("@itemRefId", entity.ItemReferenceID ?? string.Empty);
                cmd.Parameters.AddWithValue("@itemName", entity.ItemName ?? string.Empty);
                cmd.Parameters.AddWithValue("@qty", entity.Quantity);
                cmd.Parameters.AddWithValue("@price", entity.PricePerUnit.ToString("G"));
                cmd.Parameters.AddWithValue("@total", entity.TotalPrice.ToString("G"));
                cmd.Parameters.AddWithValue("@counterparty", entity.Counterparty ?? string.Empty);
                cmd.Parameters.AddWithValue("@counterpartyFaction", entity.CounterpartyFaction ?? string.Empty);
                cmd.Parameters.AddWithValue("@stationUUID", entity.StationUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@timestamp", entity.Timestamp ?? string.Empty);
                cmd.Parameters.AddWithValue("@notes", entity.Notes ?? string.Empty);
                cmd.Parameters.AddWithValue("@listingUUID", entity.ListingUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@curHp", entity.CurrentHP);
                cmd.Parameters.AddWithValue("@maxHp", entity.MaxHP);
                cmd.Parameters.AddWithValue("@maxRepair", entity.MaxRepairPercent.ToString("G"));
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteMarketTransactionAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM MarketTransactions WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” PricingPlan
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<PricingPlan>> GetAllPricingPlansAsync(string characterUUID)
        {
            var results = new List<PricingPlan>();
            using (var conn = OpenConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM PricingPlans WHERE OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = ExecuteReaderLogged(cmd))
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadPricingPlanParent(reader));
                        }
                    }
                }

                foreach (var plan in results)
                {
                    plan.ResourcePrices = LoadPricingPlanPrices(conn, plan.UUID);
                }
            }

            return Task.FromResult<IReadOnlyList<PricingPlan>>(results);
        }

        /// <inheritdoc/>
        public Task<PricingPlan> GetPricingPlanAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM PricingPlans WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        var plan = ReadPricingPlanParent(reader);
                        plan.ResourcePrices = LoadPricingPlanPrices(conn, plan.UUID);
                        return Task.FromResult(plan);
                    }
                }
            }

            return Task.FromResult<PricingPlan>(null);
        }

        /// <inheritdoc/>
        public Task UpsertPricingPlanAsync(string characterUUID, PricingPlan entity)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                UpsertPricingPlanParent(conn, tx, characterUUID, entity);
                DeletePricingPlanChildren(conn, tx, entity.UUID);
                InsertPricingPlanPrices(conn, tx, entity);
                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeletePricingPlanAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                // CASCADE handles PricingPlanPrices
                cmd.CommandText = "DELETE FROM PricingPlans WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” StockPlan
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<StockPlan>> GetAllStockPlansAsync(string characterUUID)
        {
            var results = new List<StockPlan>();
            using (var conn = OpenConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM StockPlans WHERE OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = ExecuteReaderLogged(cmd))
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadStockPlanParent(reader));
                        }
                    }
                }

                foreach (var plan in results)
                {
                    plan.Targets = LoadStockTargets(conn, plan.UUID);
                }
            }

            return Task.FromResult<IReadOnlyList<StockPlan>>(results);
        }

        /// <inheritdoc/>
        public Task<StockPlan> GetStockPlanAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM StockPlans WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        var plan = ReadStockPlanParent(reader);
                        plan.Targets = LoadStockTargets(conn, plan.UUID);
                        return Task.FromResult(plan);
                    }
                }
            }

            return Task.FromResult<StockPlan>(null);
        }

        /// <inheritdoc/>
        public Task UpsertStockPlanAsync(string characterUUID, StockPlan entity)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                UpsertStockPlanParent(conn, tx, characterUUID, entity);
                DeleteStockPlanChildren(conn, tx, entity.UUID);
                InsertStockTargets(conn, tx, entity);
                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteStockPlanAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                // CASCADE handles StockTargets
                cmd.CommandText = "DELETE FROM StockPlans WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” StockProfile
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<StockProfile>> GetAllStockProfilesAsync(string characterUUID)
        {
            var results = new List<StockProfile>();
            using (var conn = OpenConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM StockProfiles WHERE OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = ExecuteReaderLogged(cmd))
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadStockProfileParent(reader));
                        }
                    }
                }

                foreach (var profile in results)
                {
                    profile.Entries = LoadStockProfileEntries(conn, profile.UUID);
                }
            }

            return Task.FromResult<IReadOnlyList<StockProfile>>(results);
        }

        /// <inheritdoc/>
        public Task<StockProfile> GetStockProfileAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM StockProfiles WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        var profile = ReadStockProfileParent(reader);
                        profile.Entries = LoadStockProfileEntries(conn, profile.UUID);
                        return Task.FromResult(profile);
                    }
                }
            }

            return Task.FromResult<StockProfile>(null);
        }

        /// <inheritdoc/>
        public Task UpsertStockProfileAsync(string characterUUID, StockProfile entity)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                UpsertStockProfileParent(conn, tx, characterUUID, entity);
                DeleteStockProfileChildren(conn, tx, entity.UUID);
                InsertStockProfileEntries(conn, tx, entity);
                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteStockProfileAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                // CASCADE handles StockProfileEntries
                cmd.CommandText = "DELETE FROM StockProfiles WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” BuildPlan
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<BuildPlan>> GetAllBuildPlansAsync(string characterUUID)
        {
            var results = new List<BuildPlan>();
            using (var conn = OpenConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM BuildPlans WHERE OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = ExecuteReaderLogged(cmd))
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadBuildPlanParent(reader));
                        }
                    }
                }

                foreach (var plan in results)
                {
                    plan.Items = LoadBuildItems(conn, plan.UUID);
                }
            }

            return Task.FromResult<IReadOnlyList<BuildPlan>>(results);
        }

        /// <inheritdoc/>
        public Task<BuildPlan> GetBuildPlanAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM BuildPlans WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        var plan = ReadBuildPlanParent(reader);
                        plan.Items = LoadBuildItems(conn, plan.UUID);
                        return Task.FromResult(plan);
                    }
                }
            }

            return Task.FromResult<BuildPlan>(null);
        }

        /// <inheritdoc/>
        public Task UpsertBuildPlanAsync(string characterUUID, BuildPlan entity)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                UpsertBuildPlanParent(conn, tx, characterUUID, entity);
                DeleteBuildPlanChildren(conn, tx, entity.UUID);
                InsertBuildItems(conn, tx, entity);
                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteBuildPlanAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                // CASCADE handles BuildItems
                cmd.CommandText = "DELETE FROM BuildPlans WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” SupplyChain
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<SupplyChain>> GetAllSupplyChainsAsync(string characterUUID)
        {
            var results = new List<SupplyChain>();
            using (var conn = OpenConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM SupplyChains WHERE OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = ExecuteReaderLogged(cmd))
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadSupplyChainParent(reader));
                        }
                    }
                }

                foreach (var chain in results)
                {
                    chain.Stages = LoadSupplyChainStages(conn, chain.UUID);
                }
            }

            return Task.FromResult<IReadOnlyList<SupplyChain>>(results);
        }

        /// <inheritdoc/>
        public Task<SupplyChain> GetSupplyChainAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM SupplyChains WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        var chain = ReadSupplyChainParent(reader);
                        chain.Stages = LoadSupplyChainStages(conn, chain.UUID);
                        return Task.FromResult(chain);
                    }
                }
            }

            return Task.FromResult<SupplyChain>(null);
        }

        /// <inheritdoc/>
        public Task UpsertSupplyChainAsync(string characterUUID, SupplyChain entity)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                UpsertSupplyChainParent(conn, tx, characterUUID, entity);
                DeleteSupplyChainChildren(conn, tx, entity.UUID);
                InsertSupplyChainStages(conn, tx, entity);
                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteSupplyChainAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                // CASCADE handles SupplyChainStages
                cmd.CommandText = "DELETE FROM SupplyChains WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” Asteroid
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<Asteroid>> GetAllAsteroidsAsync(string characterUUID)
        {
            var results = new List<Asteroid>();
            using (var conn = OpenConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Asteroids WHERE OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = ExecuteReaderLogged(cmd))
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadAsteroidParent(reader));
                        }
                    }
                }

                foreach (var asteroid in results)
                {
                    asteroid.Reserves = LoadAsteroidReserves(conn, asteroid.UUID);
                }
            }

            return Task.FromResult<IReadOnlyList<Asteroid>>(results);
        }

        /// <inheritdoc/>
        public Task<Asteroid> GetAsteroidAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Asteroids WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        var asteroid = ReadAsteroidParent(reader);
                        asteroid.Reserves = LoadAsteroidReserves(conn, asteroid.UUID);
                        return Task.FromResult(asteroid);
                    }
                }
            }

            return Task.FromResult<Asteroid>(null);
        }

        /// <inheritdoc/>
        public Task UpsertAsteroidAsync(string characterUUID, Asteroid entity)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                UpsertAsteroidParent(conn, tx, characterUUID, entity);
                DeleteAsteroidChildren(conn, tx, entity.UUID);
                InsertAsteroidReserves(conn, tx, entity);
                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteAsteroidAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                // CASCADE handles AsteroidReserves
                cmd.CommandText = "DELETE FROM Asteroids WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” Station
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<Station>> GetAllStationsAsync(string characterUUID)
        {
            var results = new List<Station>();
            using (var conn = OpenConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Stations WHERE OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = ExecuteReaderLogged(cmd))
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadStationParent(reader));
                        }
                    }
                }

                foreach (var station in results)
                {
                    station.Components = LoadStationComponents(conn, station.UUID);
                    LoadStationItems(conn, station);
                }
            }

            return Task.FromResult<IReadOnlyList<Station>>(results);
        }

        /// <inheritdoc/>
        public Task<Station> GetStationAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Stations WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        var station = ReadStationParent(reader);
                        station.Components = LoadStationComponents(conn, station.UUID);
                        LoadStationItems(conn, station);
                        return Task.FromResult(station);
                    }
                }
            }

            return Task.FromResult<Station>(null);
        }

        /// <inheritdoc/>
        public Task UpsertStationAsync(string characterUUID, Station entity)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                UpsertStationParent(conn, tx, characterUUID, entity);
                DeleteStationChildren(conn, tx, entity.UUID);
                InsertStationComponents(conn, tx, entity);
                InsertStationItems(conn, tx, entity);
                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteStationAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            {
                // Delete items first (polymorphic FK, no CASCADE)
                using (var delItems = conn.CreateCommand())
                {
                    delItems.CommandText = "DELETE FROM Items WHERE ParentUUID = @uuid AND (ParentType = 'StationMunitions' OR ParentType LIKE 'StationHold:%')";
                    delItems.Parameters.AddWithValue("@uuid", entityUUID);
                    ExecuteNonQueryLogged(delItems);
                }

                // CASCADE handles StationComponents
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Stations WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    ExecuteNonQueryLogged(cmd);
                }
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” Faction contacts
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<Faction>> GetAllFactionsForCharacterAsync(string characterUUID)
        {
            var results = new List<Faction>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Factions WHERE OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new Faction
                        {
                            UUID = reader["UUID"] as string,
                            OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                            Name = reader["Name"] as string ?? string.Empty,
                            Description = reader["Description"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<Faction>>(results);
        }

        /// <inheritdoc/>
        public Task<Faction> GetFactionForCharacterAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Factions WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        return Task.FromResult(new Faction
                        {
                            UUID = reader["UUID"] as string,
                            OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                            Name = reader["Name"] as string ?? string.Empty,
                            Description = reader["Description"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<Faction>(null);
        }

        /// <inheritdoc/>
        public Task UpsertFactionForCharacterAsync(string characterUUID, Faction entity)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO Factions (UUID, Name, OwnerUUID, Tag, Description)
                                   VALUES (@uuid, @name, @owner, @tag, @desc)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@tag", string.Empty);
                cmd.Parameters.AddWithValue("@desc", entity.Description ?? string.Empty);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteFactionForCharacterAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM Factions WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” ExternalCharacter
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<ExternalCharacter>> GetAllExternalCharactersAsync(string characterUUID)
        {
            var results = new List<ExternalCharacter>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM ExternalCharacters WHERE OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new ExternalCharacter
                        {
                            UUID = reader["UUID"] as string,
                            OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                            Name = reader["Name"] as string ?? string.Empty,
                            FactionUUID = reader["FactionUUID"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<ExternalCharacter>>(results);
        }

        /// <inheritdoc/>
        public Task<ExternalCharacter> GetExternalCharacterAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM ExternalCharacters WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        return Task.FromResult(new ExternalCharacter
                        {
                            UUID = reader["UUID"] as string,
                            OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                            Name = reader["Name"] as string ?? string.Empty,
                            FactionUUID = reader["FactionUUID"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<ExternalCharacter>(null);
        }

        /// <inheritdoc/>
        public Task UpsertExternalCharacterAsync(string characterUUID, ExternalCharacter entity)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO ExternalCharacters (UUID, Name, OwnerUUID, FactionUUID, FactionName, CharacterId, Notes)
                                   VALUES (@uuid, @name, @owner, @factionUUID, @factionName, @charId, @notes)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@factionUUID", entity.FactionUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@factionName", string.Empty);
                cmd.Parameters.AddWithValue("@charId", 0);
                cmd.Parameters.AddWithValue("@notes", string.Empty);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteExternalCharacterAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM ExternalCharacters WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” WarehouseOverflowRule
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<WarehouseOverflowRule>> GetAllWarehouseOverflowRulesAsync(string characterUUID)
        {
            var results = new List<WarehouseOverflowRule>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM WarehouseOverflowRules WHERE OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(ReadWarehouseOverflowRule(reader));
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<WarehouseOverflowRule>>(results);
        }

        /// <inheritdoc/>
        public Task<WarehouseOverflowRule> GetWarehouseOverflowRuleAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM WarehouseOverflowRules WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        return Task.FromResult(ReadWarehouseOverflowRule(reader));
                    }
                }
            }

            return Task.FromResult<WarehouseOverflowRule>(null);
        }

        /// <inheritdoc/>
        public Task UpsertWarehouseOverflowRuleAsync(string characterUUID, WarehouseOverflowRule entity)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO WarehouseOverflowRules (UUID, OwnerUUID, ColonyUUID, ResourceName, RuleType, Threshold, DestinationColonyUUID)
                                   VALUES (@uuid, @owner, @colony, @resource, @ruleType, @threshold, @dest)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@colony", entity.ColonyUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@resource", entity.ResourceName ?? string.Empty);
                cmd.Parameters.AddWithValue("@ruleType", (int)entity.RuleType);
                cmd.Parameters.AddWithValue("@threshold", (int)entity.TriggerThreshold);
                cmd.Parameters.AddWithValue("@dest", entity.DestinationUUID ?? string.Empty);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteWarehouseOverflowRuleAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM WarehouseOverflowRules WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” MailMessage
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<MailMessage>> GetAllMailMessagesAsync(string characterUUID)
        {
            var results = new List<MailMessage>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM MailMessages WHERE OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(ReadMailMessage(reader));
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<MailMessage>>(results);
        }

        /// <inheritdoc/>
        public Task<MailMessage> GetMailMessageAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM MailMessages WHERE OwnerUUID = @ownerUUID AND MailId = CAST(@mailId AS INTEGER)";
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                cmd.Parameters.AddWithValue("@mailId", entityUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        return Task.FromResult(ReadMailMessage(reader));
                    }
                }
            }

            return Task.FromResult<MailMessage>(null);
        }

        /// <inheritdoc/>
        public Task UpsertMailMessageAsync(string characterUUID, MailMessage entity)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO MailMessages (MailId, OwnerUUID, CharacterIdFrom, FromName, CharacterIdTo, ToName, SentTime, Subject, MailRead, MailType, MailContent, LocalRead)
                                   VALUES (@mailId, @owner, @fromId, @fromName, @toId, @toName, @sent, @subject, @mailRead, @mailType, @content, @localRead)";
                cmd.Parameters.AddWithValue("@mailId", entity.MailId);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@fromId", entity.CharacterIdFrom);
                cmd.Parameters.AddWithValue("@fromName", entity.FromName ?? string.Empty);
                cmd.Parameters.AddWithValue("@toId", entity.CharacterIdTo);
                cmd.Parameters.AddWithValue("@toName", entity.ToName ?? string.Empty);
                cmd.Parameters.AddWithValue("@sent", entity.SentTime ?? string.Empty);
                cmd.Parameters.AddWithValue("@subject", entity.Subject ?? string.Empty);
                cmd.Parameters.AddWithValue("@mailRead", entity.MailRead ? 1 : 0);
                cmd.Parameters.AddWithValue("@mailType", (object)entity.MailType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@content", entity.MailContent ?? string.Empty);
                cmd.Parameters.AddWithValue("@localRead", entity.LocalRead ? 1 : 0);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteMailMessageAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM MailMessages WHERE OwnerUUID = @ownerUUID AND MailId = CAST(@mailId AS INTEGER)";
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                cmd.Parameters.AddWithValue("@mailId", entityUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Per-Character Entity CRUD â€” BankingTransaction
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<BankingTransaction>> GetAllBankingTransactionsAsync(string characterUUID)
        {
            var results = new List<BankingTransaction>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM BankingTransactions WHERE OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(ReadBankingTransaction(reader));
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<BankingTransaction>>(results);
        }

        /// <inheritdoc/>
        public Task<BankingTransaction> GetBankingTransactionAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM BankingTransactions WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        return Task.FromResult(ReadBankingTransaction(reader));
                    }
                }
            }

            return Task.FromResult<BankingTransaction>(null);
        }

        /// <inheritdoc/>
        public Task UpsertBankingTransactionAsync(string characterUUID, BankingTransaction entity)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO BankingTransactions (UUID, OwnerUUID, TransactionDateTime, CreditChange, OldBalance, NewBalance, TransactionType, Detail, CharacterId, SystemObjectId, SystemId, IsManualEntry)
                                   VALUES (@uuid, @owner, @txnDate, @credit, @oldBal, @newBal, @txnType, @detail, @charId, @sysObjId, @sysId, @manual)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@txnDate", entity.TransactionDateTime ?? string.Empty);
                cmd.Parameters.AddWithValue("@credit", entity.CreditChange.ToString("G"));
                cmd.Parameters.AddWithValue("@oldBal", entity.OldBalance.ToString("G"));
                cmd.Parameters.AddWithValue("@newBal", entity.NewBalance.ToString("G"));
                cmd.Parameters.AddWithValue("@txnType", entity.TransactionType);
                cmd.Parameters.AddWithValue("@detail", entity.Detail ?? string.Empty);
                cmd.Parameters.AddWithValue("@charId", entity.CharacterId.HasValue ? (object)entity.CharacterId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@sysObjId", entity.SystemObjectId.HasValue ? (object)entity.SystemObjectId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@sysId", entity.SystemId.HasValue ? (object)entity.SystemId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@manual", entity.IsManualEntry ? 1 : 0);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteBankingTransactionAsync(string characterUUID, string entityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM BankingTransactions WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                cmd.Parameters.AddWithValue("@uuid", entityUUID);
                cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Faction Permission Entities
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionCapability>> GetFactionCapabilitiesAsync(string factionUUID)
        {
            var results = new List<FactionCapability>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM FactionCapabilities WHERE FactionUUID = @fid";
                cmd.Parameters.AddWithValue("@fid", factionUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new FactionCapability
                        {
                            UUID = reader["UUID"] as string ?? string.Empty,
                            FactionUUID = reader["FactionUUID"] as string ?? string.Empty,
                            Name = reader["Name"] as string ?? string.Empty,
                            Description = reader["Description"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<FactionCapability>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertFactionCapabilityAsync(FactionCapability capability)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO FactionCapabilities (UUID, FactionUUID, Name, Description)
                                   VALUES (@uuid, @fid, @name, @desc)";
                cmd.Parameters.AddWithValue("@uuid", capability.UUID);
                cmd.Parameters.AddWithValue("@fid", capability.FactionUUID);
                cmd.Parameters.AddWithValue("@name", capability.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@desc", capability.Description ?? string.Empty);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteFactionCapabilityAsync(string factionUUID, string capabilityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM FactionCapabilities WHERE UUID = @uuid AND FactionUUID = @fid";
                cmd.Parameters.AddWithValue("@uuid", capabilityUUID);
                cmd.Parameters.AddWithValue("@fid", factionUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionClearanceLevel>> GetFactionClearanceLevelsAsync(string factionUUID)
        {
            var results = new List<FactionClearanceLevel>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM FactionClearanceLevels WHERE FactionUUID = @fid";
                cmd.Parameters.AddWithValue("@fid", factionUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new FactionClearanceLevel
                        {
                            UUID = reader["UUID"] as string ?? string.Empty,
                            FactionUUID = reader["FactionUUID"] as string ?? string.Empty,
                            Level = Convert.ToInt32(reader["Level"]),
                            Name = reader["Name"] as string ?? string.Empty,
                            Description = reader["Description"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<FactionClearanceLevel>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertFactionClearanceLevelAsync(FactionClearanceLevel level)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO FactionClearanceLevels (UUID, FactionUUID, Level, Name, Description)
                                   VALUES (@uuid, @fid, @level, @name, @desc)";
                cmd.Parameters.AddWithValue("@uuid", level.UUID);
                cmd.Parameters.AddWithValue("@fid", level.FactionUUID);
                cmd.Parameters.AddWithValue("@level", level.Level);
                cmd.Parameters.AddWithValue("@name", level.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@desc", level.Description ?? string.Empty);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteFactionClearanceLevelAsync(string factionUUID, string levelUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM FactionClearanceLevels WHERE UUID = @uuid AND FactionUUID = @fid";
                cmd.Parameters.AddWithValue("@uuid", levelUUID);
                cmd.Parameters.AddWithValue("@fid", factionUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionPermissionGroup>> GetFactionGroupsAsync(string factionUUID)
        {
            var results = new List<FactionPermissionGroup>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM FactionPermissionGroups WHERE FactionUUID = @fid";
                cmd.Parameters.AddWithValue("@fid", factionUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new FactionPermissionGroup
                        {
                            UUID = reader["UUID"] as string ?? string.Empty,
                            FactionUUID = reader["FactionUUID"] as string ?? string.Empty,
                            Name = reader["Name"] as string ?? string.Empty,
                            Description = reader["Description"] as string ?? string.Empty,
                            DefaultClearanceLevelUUID = reader["DefaultClearanceLevelUUID"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<FactionPermissionGroup>>(results);
        }

        /// <inheritdoc/>
        public Task<FactionPermissionGroup> GetFactionGroupAsync(string factionUUID, string groupUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM FactionPermissionGroups WHERE UUID = @uuid AND FactionUUID = @fid";
                cmd.Parameters.AddWithValue("@uuid", groupUUID);
                cmd.Parameters.AddWithValue("@fid", factionUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        return Task.FromResult(new FactionPermissionGroup
                        {
                            UUID = reader["UUID"] as string ?? string.Empty,
                            FactionUUID = reader["FactionUUID"] as string ?? string.Empty,
                            Name = reader["Name"] as string ?? string.Empty,
                            Description = reader["Description"] as string ?? string.Empty,
                            DefaultClearanceLevelUUID = reader["DefaultClearanceLevelUUID"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<FactionPermissionGroup>(null);
        }

        /// <inheritdoc/>
        public Task UpsertFactionGroupAsync(FactionPermissionGroup group)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO FactionPermissionGroups (UUID, FactionUUID, Name, Description, DefaultClearanceLevelUUID)
                                   VALUES (@uuid, @fid, @name, @desc, @defaultCl)";
                cmd.Parameters.AddWithValue("@uuid", group.UUID);
                cmd.Parameters.AddWithValue("@fid", group.FactionUUID);
                cmd.Parameters.AddWithValue("@name", group.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@desc", group.Description ?? string.Empty);
                cmd.Parameters.AddWithValue("@defaultCl", group.DefaultClearanceLevelUUID ?? string.Empty);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteFactionGroupAsync(string factionUUID, string groupUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM FactionPermissionGroups WHERE UUID = @uuid AND FactionUUID = @fid";
                cmd.Parameters.AddWithValue("@uuid", groupUUID);
                cmd.Parameters.AddWithValue("@fid", factionUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionGroupCapability>> GetFactionGroupCapabilitiesAsync(string groupUUID)
        {
            var results = new List<FactionGroupCapability>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM FactionGroupCapabilities WHERE GroupUUID = @gid";
                cmd.Parameters.AddWithValue("@gid", groupUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new FactionGroupCapability
                        {
                            GroupUUID = reader["GroupUUID"] as string ?? string.Empty,
                            CapabilityUUID = reader["CapabilityUUID"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<FactionGroupCapability>>(results);
        }

        /// <inheritdoc/>
        public Task AddFactionGroupCapabilityAsync(FactionGroupCapability item)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO FactionGroupCapabilities (GroupUUID, CapabilityUUID)
                                   VALUES (@gid, @cid)";
                cmd.Parameters.AddWithValue("@gid", item.GroupUUID);
                cmd.Parameters.AddWithValue("@cid", item.CapabilityUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RemoveFactionGroupCapabilityAsync(string groupUUID, string capabilityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM FactionGroupCapabilities WHERE GroupUUID = @gid AND CapabilityUUID = @cid";
                cmd.Parameters.AddWithValue("@gid", groupUUID);
                cmd.Parameters.AddWithValue("@cid", capabilityUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionGroupSharingRule>> GetFactionGroupSharingRulesAsync(string groupUUID)
        {
            var results = new List<FactionGroupSharingRule>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM FactionGroupSharingRules WHERE GroupUUID = @gid";
                cmd.Parameters.AddWithValue("@gid", groupUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new FactionGroupSharingRule
                        {
                            UUID = reader["UUID"] as string ?? string.Empty,
                            GroupUUID = reader["GroupUUID"] as string ?? string.Empty,
                            DataType = reader["DataType"] as string,
                            EntityUUID = reader["EntityUUID"] as string,
                            MinClearanceLevelUUID = reader["MinClearanceLevelUUID"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<FactionGroupSharingRule>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertFactionGroupSharingRuleAsync(FactionGroupSharingRule rule)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO FactionGroupSharingRules (UUID, GroupUUID, DataType, EntityUUID, MinClearanceLevelUUID)
                                   VALUES (@uuid, @gid, @dataType, @entityUUID, @minCl)";
                cmd.Parameters.AddWithValue("@uuid", rule.UUID);
                cmd.Parameters.AddWithValue("@gid", rule.GroupUUID);
                cmd.Parameters.AddWithValue("@dataType", (object)rule.DataType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@entityUUID", (object)rule.EntityUUID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@minCl", rule.MinClearanceLevelUUID ?? string.Empty);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteFactionGroupSharingRuleAsync(string groupUUID, string ruleUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM FactionGroupSharingRules WHERE UUID = @uuid AND GroupUUID = @gid";
                cmd.Parameters.AddWithValue("@uuid", ruleUUID);
                cmd.Parameters.AddWithValue("@gid", groupUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<FactionMemberPermissions> GetFactionMemberPermissionsAsync(string factionUUID, string characterUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM FactionMemberPermissions WHERE FactionUUID = @fid AND CharacterUUID = @cid";
                cmd.Parameters.AddWithValue("@fid", factionUUID);
                cmd.Parameters.AddWithValue("@cid", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        return Task.FromResult(new FactionMemberPermissions
                        {
                            CharacterUUID = reader["CharacterUUID"] as string ?? string.Empty,
                            FactionUUID = reader["FactionUUID"] as string ?? string.Empty,
                            GroupUUID = reader["GroupUUID"] as string,
                            ClearanceLevelUUID = reader["ClearanceLevelUUID"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<FactionMemberPermissions>(null);
        }

        /// <inheritdoc/>
        public Task UpsertFactionMemberPermissionsAsync(FactionMemberPermissions perms)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO FactionMemberPermissions (FactionUUID, CharacterUUID, GroupUUID, ClearanceLevelUUID)
                                   VALUES (@fid, @cid, @gid, @clid)";
                cmd.Parameters.AddWithValue("@fid", perms.FactionUUID);
                cmd.Parameters.AddWithValue("@cid", perms.CharacterUUID);
                cmd.Parameters.AddWithValue("@gid", (object)perms.GroupUUID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@clid", perms.ClearanceLevelUUID ?? string.Empty);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionMemberPermissions>> GetAllFactionMembersPermissionsAsync(string factionUUID)
        {
            var results = new List<FactionMemberPermissions>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM FactionMemberPermissions WHERE FactionUUID = @fid";
                cmd.Parameters.AddWithValue("@fid", factionUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new FactionMemberPermissions
                        {
                            CharacterUUID = reader["CharacterUUID"] as string ?? string.Empty,
                            FactionUUID = reader["FactionUUID"] as string ?? string.Empty,
                            GroupUUID = reader["GroupUUID"] as string,
                            ClearanceLevelUUID = reader["ClearanceLevelUUID"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<FactionMemberPermissions>>(results);
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionMemberCapability>> GetFactionMemberCapabilitiesAsync(string factionUUID, string characterUUID)
        {
            var results = new List<FactionMemberCapability>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM FactionMemberCapabilities WHERE FactionUUID = @fid AND CharacterUUID = @cid";
                cmd.Parameters.AddWithValue("@fid", factionUUID);
                cmd.Parameters.AddWithValue("@cid", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new FactionMemberCapability
                        {
                            CharacterUUID = reader["CharacterUUID"] as string ?? string.Empty,
                            FactionUUID = reader["FactionUUID"] as string ?? string.Empty,
                            CapabilityUUID = reader["CapabilityUUID"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<FactionMemberCapability>>(results);
        }

        /// <inheritdoc/>
        public Task AddFactionMemberCapabilityAsync(FactionMemberCapability item)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO FactionMemberCapabilities (FactionUUID, CharacterUUID, CapabilityUUID)
                                   VALUES (@fid, @cid, @capId)";
                cmd.Parameters.AddWithValue("@fid", item.FactionUUID);
                cmd.Parameters.AddWithValue("@cid", item.CharacterUUID);
                cmd.Parameters.AddWithValue("@capId", item.CapabilityUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RemoveFactionMemberCapabilityAsync(string factionUUID, string characterUUID, string capabilityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM FactionMemberCapabilities WHERE FactionUUID = @fid AND CharacterUUID = @cid AND CapabilityUUID = @capId";
                cmd.Parameters.AddWithValue("@fid", factionUUID);
                cmd.Parameters.AddWithValue("@cid", characterUUID);
                cmd.Parameters.AddWithValue("@capId", capabilityUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Character Permission Entities
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterCapability>> GetCharacterCapabilitiesAsync(string characterUUID)
        {
            var results = new List<CharacterCapability>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM CharacterCapabilities WHERE OwnerCharacterUUID = @cid";
                cmd.Parameters.AddWithValue("@cid", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new CharacterCapability
                        {
                            UUID = reader["UUID"] as string ?? string.Empty,
                            OwnerCharacterUUID = reader["OwnerCharacterUUID"] as string ?? string.Empty,
                            Name = reader["Name"] as string ?? string.Empty,
                            Description = reader["Description"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<CharacterCapability>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertCharacterCapabilityAsync(CharacterCapability capability)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO CharacterCapabilities (UUID, OwnerCharacterUUID, Name, Description)
                                   VALUES (@uuid, @cid, @name, @desc)";
                cmd.Parameters.AddWithValue("@uuid", capability.UUID);
                cmd.Parameters.AddWithValue("@cid", capability.OwnerCharacterUUID);
                cmd.Parameters.AddWithValue("@name", capability.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@desc", capability.Description ?? string.Empty);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteCharacterCapabilityAsync(string characterUUID, string capabilityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM CharacterCapabilities WHERE UUID = @uuid AND OwnerCharacterUUID = @cid";
                cmd.Parameters.AddWithValue("@uuid", capabilityUUID);
                cmd.Parameters.AddWithValue("@cid", characterUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterClearanceLevel>> GetCharacterClearanceLevelsAsync(string characterUUID)
        {
            var results = new List<CharacterClearanceLevel>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM CharacterClearanceLevels WHERE OwnerCharacterUUID = @cid";
                cmd.Parameters.AddWithValue("@cid", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new CharacterClearanceLevel
                        {
                            UUID = reader["UUID"] as string ?? string.Empty,
                            OwnerCharacterUUID = reader["OwnerCharacterUUID"] as string ?? string.Empty,
                            Level = Convert.ToInt32(reader["Level"]),
                            Name = reader["Name"] as string ?? string.Empty,
                            Description = reader["Description"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<CharacterClearanceLevel>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertCharacterClearanceLevelAsync(CharacterClearanceLevel level)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO CharacterClearanceLevels (UUID, OwnerCharacterUUID, Level, Name, Description)
                                   VALUES (@uuid, @cid, @level, @name, @desc)";
                cmd.Parameters.AddWithValue("@uuid", level.UUID);
                cmd.Parameters.AddWithValue("@cid", level.OwnerCharacterUUID);
                cmd.Parameters.AddWithValue("@level", level.Level);
                cmd.Parameters.AddWithValue("@name", level.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@desc", level.Description ?? string.Empty);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteCharacterClearanceLevelAsync(string characterUUID, string levelUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM CharacterClearanceLevels WHERE UUID = @uuid AND OwnerCharacterUUID = @cid";
                cmd.Parameters.AddWithValue("@uuid", levelUUID);
                cmd.Parameters.AddWithValue("@cid", characterUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterPermissionGroup>> GetCharacterGroupsAsync(string characterUUID)
        {
            var results = new List<CharacterPermissionGroup>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM CharacterPermissionGroups WHERE OwnerCharacterUUID = @cid";
                cmd.Parameters.AddWithValue("@cid", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new CharacterPermissionGroup
                        {
                            UUID = reader["UUID"] as string ?? string.Empty,
                            OwnerCharacterUUID = reader["OwnerCharacterUUID"] as string ?? string.Empty,
                            Name = reader["Name"] as string ?? string.Empty,
                            Description = reader["Description"] as string ?? string.Empty,
                            DefaultClearanceLevelUUID = reader["DefaultClearanceLevelUUID"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<CharacterPermissionGroup>>(results);
        }

        /// <inheritdoc/>
        public Task<CharacterPermissionGroup> GetCharacterGroupAsync(string characterUUID, string groupUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM CharacterPermissionGroups WHERE UUID = @uuid AND OwnerCharacterUUID = @cid";
                cmd.Parameters.AddWithValue("@uuid", groupUUID);
                cmd.Parameters.AddWithValue("@cid", characterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        return Task.FromResult(new CharacterPermissionGroup
                        {
                            UUID = reader["UUID"] as string ?? string.Empty,
                            OwnerCharacterUUID = reader["OwnerCharacterUUID"] as string ?? string.Empty,
                            Name = reader["Name"] as string ?? string.Empty,
                            Description = reader["Description"] as string ?? string.Empty,
                            DefaultClearanceLevelUUID = reader["DefaultClearanceLevelUUID"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<CharacterPermissionGroup>(null);
        }

        /// <inheritdoc/>
        public Task UpsertCharacterGroupAsync(CharacterPermissionGroup group)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO CharacterPermissionGroups (UUID, OwnerCharacterUUID, Name, Description, DefaultClearanceLevelUUID)
                                   VALUES (@uuid, @cid, @name, @desc, @defaultCl)";
                cmd.Parameters.AddWithValue("@uuid", group.UUID);
                cmd.Parameters.AddWithValue("@cid", group.OwnerCharacterUUID);
                cmd.Parameters.AddWithValue("@name", group.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@desc", group.Description ?? string.Empty);
                cmd.Parameters.AddWithValue("@defaultCl", group.DefaultClearanceLevelUUID ?? string.Empty);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteCharacterGroupAsync(string characterUUID, string groupUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM CharacterPermissionGroups WHERE UUID = @uuid AND OwnerCharacterUUID = @cid";
                cmd.Parameters.AddWithValue("@uuid", groupUUID);
                cmd.Parameters.AddWithValue("@cid", characterUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGroupCapability>> GetCharacterGroupCapabilitiesAsync(string groupUUID)
        {
            var results = new List<CharacterGroupCapability>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM CharacterGroupCapabilities WHERE GroupUUID = @gid";
                cmd.Parameters.AddWithValue("@gid", groupUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new CharacterGroupCapability
                        {
                            GroupUUID = reader["GroupUUID"] as string ?? string.Empty,
                            CapabilityUUID = reader["CapabilityUUID"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<CharacterGroupCapability>>(results);
        }

        /// <inheritdoc/>
        public Task AddCharacterGroupCapabilityAsync(CharacterGroupCapability item)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO CharacterGroupCapabilities (GroupUUID, CapabilityUUID)
                                   VALUES (@gid, @cid)";
                cmd.Parameters.AddWithValue("@gid", item.GroupUUID);
                cmd.Parameters.AddWithValue("@cid", item.CapabilityUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RemoveCharacterGroupCapabilityAsync(string groupUUID, string capabilityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM CharacterGroupCapabilities WHERE GroupUUID = @gid AND CapabilityUUID = @cid";
                cmd.Parameters.AddWithValue("@gid", groupUUID);
                cmd.Parameters.AddWithValue("@cid", capabilityUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGroupSharingRule>> GetCharacterGroupSharingRulesAsync(string groupUUID)
        {
            var results = new List<CharacterGroupSharingRule>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM CharacterGroupSharingRules WHERE GroupUUID = @gid";
                cmd.Parameters.AddWithValue("@gid", groupUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new CharacterGroupSharingRule
                        {
                            UUID = reader["UUID"] as string ?? string.Empty,
                            GroupUUID = reader["GroupUUID"] as string ?? string.Empty,
                            DataType = reader["DataType"] as string,
                            EntityUUID = reader["EntityUUID"] as string,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<CharacterGroupSharingRule>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertCharacterGroupSharingRuleAsync(CharacterGroupSharingRule rule)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO CharacterGroupSharingRules (UUID, GroupUUID, DataType, EntityUUID)
                                   VALUES (@uuid, @gid, @dataType, @entityUUID)";
                cmd.Parameters.AddWithValue("@uuid", rule.UUID);
                cmd.Parameters.AddWithValue("@gid", rule.GroupUUID);
                cmd.Parameters.AddWithValue("@dataType", (object)rule.DataType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@entityUUID", (object)rule.EntityUUID ?? DBNull.Value);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteCharacterGroupSharingRuleAsync(string groupUUID, string ruleUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM CharacterGroupSharingRules WHERE UUID = @uuid AND GroupUUID = @gid";
                cmd.Parameters.AddWithValue("@uuid", ruleUUID);
                cmd.Parameters.AddWithValue("@gid", groupUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGranteePermissions>> GetCharacterGranteesAsync(string ownerCharacterUUID)
        {
            var results = new List<CharacterGranteePermissions>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM CharacterGranteePermissions WHERE OwnerCharacterUUID = @cid";
                cmd.Parameters.AddWithValue("@cid", ownerCharacterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new CharacterGranteePermissions
                        {
                            OwnerCharacterUUID = reader["OwnerCharacterUUID"] as string ?? string.Empty,
                            GranteeType = (GranteeType)Convert.ToInt32(reader["GranteeType"]),
                            GranteeUUID = reader["GranteeUUID"] as string ?? string.Empty,
                            GroupUUID = reader["GroupUUID"] as string,
                            ClearanceLevelUUID = reader["ClearanceLevelUUID"] as string,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<CharacterGranteePermissions>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertCharacterGranteePermissionsAsync(CharacterGranteePermissions perms)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO CharacterGranteePermissions (OwnerCharacterUUID, GranteeType, GranteeUUID, GroupUUID, ClearanceLevelUUID)
                                   VALUES (@cid, @granteeType, @granteeUUID, @gid, @clid)";
                cmd.Parameters.AddWithValue("@cid", perms.OwnerCharacterUUID);
                cmd.Parameters.AddWithValue("@granteeType", (int)perms.GranteeType);
                cmd.Parameters.AddWithValue("@granteeUUID", perms.GranteeUUID);
                cmd.Parameters.AddWithValue("@gid", (object)perms.GroupUUID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@clid", (object)perms.ClearanceLevelUUID ?? DBNull.Value);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteCharacterGranteePermissionsAsync(string ownerCharacterUUID, string granteeUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM CharacterGranteePermissions WHERE OwnerCharacterUUID = @cid AND GranteeUUID = @gid";
                cmd.Parameters.AddWithValue("@cid", ownerCharacterUUID);
                cmd.Parameters.AddWithValue("@gid", granteeUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGranteeCapability>> GetCharacterGranteeCapabilitiesAsync(string ownerCharacterUUID, string granteeUUID)
        {
            var results = new List<CharacterGranteeCapability>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM CharacterGranteeCapabilities WHERE OwnerCharacterUUID = @cid AND GranteeUUID = @gid";
                cmd.Parameters.AddWithValue("@cid", ownerCharacterUUID);
                cmd.Parameters.AddWithValue("@gid", granteeUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new CharacterGranteeCapability
                        {
                            OwnerCharacterUUID = reader["OwnerCharacterUUID"] as string ?? string.Empty,
                            GranteeType = (GranteeType)Convert.ToInt32(reader["GranteeType"]),
                            GranteeUUID = reader["GranteeUUID"] as string ?? string.Empty,
                            CapabilityUUID = reader["CapabilityUUID"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<CharacterGranteeCapability>>(results);
        }

        /// <inheritdoc/>
        public Task AddCharacterGranteeCapabilityAsync(CharacterGranteeCapability item)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO CharacterGranteeCapabilities (OwnerCharacterUUID, GranteeUUID, CapabilityUUID)
                                   VALUES (@cid, @gid, @capId)";
                cmd.Parameters.AddWithValue("@cid", item.OwnerCharacterUUID);
                cmd.Parameters.AddWithValue("@gid", item.GranteeUUID);
                cmd.Parameters.AddWithValue("@capId", item.CapabilityUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RemoveCharacterGranteeCapabilityAsync(string ownerCharacterUUID, string granteeUUID, string capabilityUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM CharacterGranteeCapabilities WHERE OwnerCharacterUUID = @cid AND GranteeUUID = @gid AND CapabilityUUID = @capId";
                cmd.Parameters.AddWithValue("@cid", ownerCharacterUUID);
                cmd.Parameters.AddWithValue("@gid", granteeUUID);
                cmd.Parameters.AddWithValue("@capId", capabilityUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Intel
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelComment>> GetIntelCommentsForTargetAsync(string targetCharacterUUID)
        {
            var results = new List<IntelComment>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM IntelComments WHERE TargetCharacterUUID = @tid";
                cmd.Parameters.AddWithValue("@tid", targetCharacterUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(ReadIntelComment(reader));
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<IntelComment>>(results);
        }

        /// <inheritdoc/>
        public Task<IntelComment> GetIntelCommentAsync(string commentUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM IntelComments WHERE UUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", commentUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        return Task.FromResult(ReadIntelComment(reader));
                    }
                }
            }

            return Task.FromResult<IntelComment>(null);
        }

        /// <inheritdoc/>
        public Task UpsertIntelCommentAsync(IntelComment comment)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO IntelComments (UUID, TargetCharacterUUID, SubmitterCharacterUUID, Text, CreatedUtc)
                                   VALUES (@uuid, @target, @submitter, @text, @created)";
                cmd.Parameters.AddWithValue("@uuid", comment.UUID);
                cmd.Parameters.AddWithValue("@target", comment.TargetCharacterUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@submitter", comment.SubmitterCharacterUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@text", comment.Text ?? string.Empty);
                cmd.Parameters.AddWithValue("@created", comment.CreatedUtc.ToString("O"));
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteIntelCommentAsync(string commentUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM IntelComments WHERE UUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", commentUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForCommentAsync(string commentUUID)
        {
            var results = new List<IntelCommentFactionShare>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM IntelCommentFactionShares WHERE IntelCommentUUID = @cid";
                cmd.Parameters.AddWithValue("@cid", commentUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(ReadIntelShare(reader));
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<IntelCommentFactionShare>>(results);
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForFactionAsync(string factionUUID)
        {
            var results = new List<IntelCommentFactionShare>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM IntelCommentFactionShares WHERE FactionUUID = @fid";
                cmd.Parameters.AddWithValue("@fid", factionUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(ReadIntelShare(reader));
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<IntelCommentFactionShare>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertIntelShareAsync(IntelCommentFactionShare share)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO IntelCommentFactionShares (UUID, IntelCommentUUID, FactionUUID, ClassificationLevelUUID, ClassifiedByCharacterUUID, SharedUtc, ClassifiedUtc)
                                   VALUES (@uuid, @commentId, @fid, @clLevel, @classifiedBy, @shared, @classifiedUtc)";
                cmd.Parameters.AddWithValue("@uuid", share.UUID);
                cmd.Parameters.AddWithValue("@commentId", share.IntelCommentUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@fid", share.FactionUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@clLevel", (object)share.ClassificationLevelUUID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@classifiedBy", (object)share.ClassifiedByCharacterUUID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@shared", share.SharedUtc.ToString("O"));
                cmd.Parameters.AddWithValue("@classifiedUtc", share.ClassifiedUtc.HasValue ? (object)share.ClassifiedUtc.Value.ToString("O") : DBNull.Value);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteIntelShareAsync(string shareUUID)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM IntelCommentFactionShares WHERE UUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", shareUUID);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Audit
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<IReadOnlyList<PermissionAuditEntry>> GetPermissionAuditEntriesAsync(DateTime? startDate = null, DateTime? endDate = null, PermissionActionType? actionType = null, string actorUUID = null, string targetUUID = null)
        {
            var results = new List<PermissionAuditEntry>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                var where = new List<string>();
                if (startDate.HasValue)
                {
                    where.Add("Timestamp >= @startDate");
                    cmd.Parameters.AddWithValue("@startDate", startDate.Value.ToString("O"));
                }

                if (endDate.HasValue)
                {
                    where.Add("Timestamp <= @endDate");
                    cmd.Parameters.AddWithValue("@endDate", endDate.Value.ToString("O"));
                }

                if (actionType.HasValue)
                {
                    where.Add("ActionType = @actionType");
                    cmd.Parameters.AddWithValue("@actionType", (int)actionType.Value);
                }

                if (actorUUID != null)
                {
                    where.Add("ActorCharacterUUID = @actorUUID");
                    cmd.Parameters.AddWithValue("@actorUUID", actorUUID);
                }

                if (targetUUID != null)
                {
                    where.Add("TargetCharacterUUID = @targetUUID");
                    cmd.Parameters.AddWithValue("@targetUUID", targetUUID);
                }

                cmd.CommandText = "SELECT * FROM PermissionAuditEntries"
                    + (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : string.Empty)
                    + " ORDER BY Timestamp DESC";

                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new PermissionAuditEntry
                        {
                            UUID = reader["UUID"] as string ?? string.Empty,
                            Timestamp = DateTime.Parse(reader["Timestamp"] as string ?? DateTime.MinValue.ToString("O")),
                            ActorCharacterUUID = reader["ActorCharacterUUID"] as string ?? string.Empty,
                            TargetCharacterUUID = reader["TargetCharacterUUID"] as string ?? string.Empty,
                            ActionType = (PermissionActionType)Convert.ToInt32(reader["ActionType"]),
                            OldValue = reader["OldValue"] as string ?? string.Empty,
                            NewValue = reader["NewValue"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<PermissionAuditEntry>>(results);
        }

        /// <inheritdoc/>
        public Task AppendPermissionAuditEntryAsync(PermissionAuditEntry entry)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO PermissionAuditEntries (UUID, Timestamp, ActorCharacterUUID, TargetCharacterUUID, ActionType, OldValue, NewValue)
                                   VALUES (@uuid, @ts, @actor, @target, @action, @oldVal, @newVal)";
                cmd.Parameters.AddWithValue("@uuid", entry.UUID);
                cmd.Parameters.AddWithValue("@ts", entry.Timestamp.ToString("O"));
                cmd.Parameters.AddWithValue("@actor", entry.ActorCharacterUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@target", entry.TargetCharacterUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@action", (int)entry.ActionType);
                cmd.Parameters.AddWithValue("@oldVal", entry.OldValue ?? string.Empty);
                cmd.Parameters.AddWithValue("@newVal", entry.NewValue ?? string.Empty);
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteExpiredAuditEntriesAsync(DateTime cutoff)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM PermissionAuditEntries WHERE Timestamp < @cutoff";
                cmd.Parameters.AddWithValue("@cutoff", cutoff.ToString("O"));
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Baseline / Global Lookup Data
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <inheritdoc/>
        public Task<BaselineGameConstants> GetBaselineGameConstantsAsync()
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM BaselineGameConstants WHERE Id = 1";
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    if (reader.Read())
                    {
                        return Task.FromResult(new BaselineGameConstants
                        {
                            RefiningBaseRate = 25,
                            CommoditiesPerCycle = 10,
                            CommodityCycleSeconds = 600,
                            StructureCap = 65,
                            WorkerVolume = 50m,
                        });
                    }
                }
            }

            return Task.FromResult<BaselineGameConstants>(null);
        }

        /// <inheritdoc/>
        public Task UpsertBaselineGameConstantsAsync(BaselineGameConstants constants)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO BaselineGameConstants (Id, DataVersion, LastUpdatedUtc)
                                   VALUES (1, 1, @utc)";
                cmd.Parameters.AddWithValue("@utc", SystemClock.UtcNow.ToString("O"));
                ExecuteNonQueryLogged(cmd);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<BlueprintType>> GetAllBlueprintTypesAsync()
        {
            var results = new List<BlueprintType>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM BlueprintTypes";
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        var bt = new BlueprintType
                        {
                            Id = reader["Name"] as string ?? string.Empty,
                            Name = reader["Name"] as string ?? string.Empty,
                        };
                        results.Add(bt);
                    }
                }

                // Load properties and researchable properties
                foreach (var bt in results)
                {
                    bt.Properties = LoadBlueprintTypeProperties(conn, bt.Name);
                    bt.ResearchableProperties = LoadBlueprintTypeResearchableProperties(conn, bt.Name);
                }
            }

            return Task.FromResult<IReadOnlyList<BlueprintType>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertBlueprintTypesAsync(IReadOnlyList<BlueprintType> types)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                using (var del = conn.CreateCommand())
                {
                    del.Transaction = tx;
                    del.CommandText = "DELETE FROM BlueprintTypes";
                    ExecuteNonQueryLogged(del);
                }

                foreach (var bt in types)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = @"INSERT INTO BlueprintTypes (Name, Category, TechLevel, BaseVolume, BaseMass)
                                           VALUES (@name, @cat, @tech, @vol, @mass)";
                        cmd.Parameters.AddWithValue("@name", bt.Name ?? bt.Id ?? string.Empty);
                        cmd.Parameters.AddWithValue("@cat", string.Empty);
                        cmd.Parameters.AddWithValue("@tech", string.Empty);
                        cmd.Parameters.AddWithValue("@vol", 0.0);
                        cmd.Parameters.AddWithValue("@mass", 0.0);
                        ExecuteNonQueryLogged(cmd);
                    }

                    if (bt.Properties != null)
                    {
                        foreach (var prop in bt.Properties)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                cmd.CommandText = @"INSERT INTO BlueprintTypeProperties (BlueprintTypeName, Key, DefaultValue)
                                                   VALUES (@name, @key, @val)";
                                cmd.Parameters.AddWithValue("@name", bt.Name ?? bt.Id ?? string.Empty);
                                cmd.Parameters.AddWithValue("@key", prop ?? string.Empty);
                                cmd.Parameters.AddWithValue("@val", string.Empty);
                                ExecuteNonQueryLogged(cmd);
                            }
                        }
                    }

                    if (bt.ResearchableProperties != null)
                    {
                        foreach (var prop in bt.ResearchableProperties)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                cmd.CommandText = @"INSERT INTO BlueprintTypeResearchableProperties (BlueprintTypeName, Key, MinValue, MaxValue)
                                                   VALUES (@name, @key, @min, @max)";
                                cmd.Parameters.AddWithValue("@name", bt.Name ?? bt.Id ?? string.Empty);
                                cmd.Parameters.AddWithValue("@key", prop ?? string.Empty);
                                cmd.Parameters.AddWithValue("@min", string.Empty);
                                cmd.Parameters.AddWithValue("@max", string.Empty);
                                ExecuteNonQueryLogged(cmd);
                            }
                        }
                    }
                }

                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ShipClass>> GetAllShipClassesAsync()
        {
            var results = new List<ShipClass>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM ShipClasses";
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new ShipClass
                        {
                            Id = 0,
                            Name = reader["Name"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<ShipClass>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertShipClassesAsync(IReadOnlyList<ShipClass> classes)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                using (var del = conn.CreateCommand())
                {
                    del.Transaction = tx;
                    del.CommandText = "DELETE FROM ShipClasses";
                    ExecuteNonQueryLogged(del);
                }

                foreach (var sc in classes)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = @"INSERT INTO ShipClasses (Name, HullType, CargoCapacity, HopperCapacity, ComponentSlots, BaseHP)
                                           VALUES (@name, @hull, @cargo, @hopper, @slots, @hp)";
                        cmd.Parameters.AddWithValue("@name", sc.Name ?? string.Empty);
                        cmd.Parameters.AddWithValue("@hull", string.Empty);
                        cmd.Parameters.AddWithValue("@cargo", 0);
                        cmd.Parameters.AddWithValue("@hopper", 0);
                        cmd.Parameters.AddWithValue("@slots", 0);
                        cmd.Parameters.AddWithValue("@hp", 0);
                        ExecuteNonQueryLogged(cmd);
                    }
                }

                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<TechLevel>> GetAllTechLevelsAsync()
        {
            var results = new List<TechLevel>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM TechLevels";
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new TechLevel
                        {
                            Name = reader["Name"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<TechLevel>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertTechLevelsAsync(IReadOnlyList<TechLevel> levels)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                using (var del = conn.CreateCommand())
                {
                    del.Transaction = tx;
                    del.CommandText = "DELETE FROM TechLevels";
                    ExecuteNonQueryLogged(del);
                }

                foreach (var tl in levels)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = @"INSERT INTO TechLevels (Name, Level, Description)
                                           VALUES (@name, @level, @desc)";
                        cmd.Parameters.AddWithValue("@name", tl.Name ?? string.Empty);
                        cmd.Parameters.AddWithValue("@level", 0);
                        cmd.Parameters.AddWithValue("@desc", string.Empty);
                        ExecuteNonQueryLogged(cmd);
                    }
                }

                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<Commodity>> GetAllCommoditiesAsync()
        {
            var results = new List<Commodity>();
            using (var conn = OpenConnection())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Commodities";
                    using (var reader = ExecuteReaderLogged(cmd))
                    {
                        while (reader.Read())
                        {
                            results.Add(new Commodity
                            {
                                ID = reader["Name"] as string ?? string.Empty,
                                Name = reader["Name"] as string ?? string.Empty,
                            });
                        }
                    }
                }

                // Load construction resources for each commodity
                foreach (var c in results)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT ResourceName, Quantity FROM CommodityResources WHERE CommodityName = @name";
                        cmd.Parameters.AddWithValue("@name", c.Name);
                        using (var reader = ExecuteReaderLogged(cmd))
                        {
                            while (reader.Read())
                            {
                                c.ConstructionResources[reader["ResourceName"] as string ?? string.Empty] =
                                    Convert.ToInt32(reader["Quantity"]).ToString();
                            }
                        }
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<Commodity>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertCommoditiesAsync(IReadOnlyList<Commodity> commodities)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                using (var del = conn.CreateCommand())
                {
                    del.Transaction = tx;
                    del.CommandText = "DELETE FROM Commodities";
                    ExecuteNonQueryLogged(del);
                }

                foreach (var c in commodities)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = @"INSERT INTO Commodities (Name, Category, BaseVolume, BaseMass, BaseValue)
                                           VALUES (@name, @cat, @vol, @mass, @val)";
                        cmd.Parameters.AddWithValue("@name", c.Name ?? c.ID ?? string.Empty);
                        cmd.Parameters.AddWithValue("@cat", string.Empty);
                        cmd.Parameters.AddWithValue("@vol", 0.0);
                        cmd.Parameters.AddWithValue("@mass", 0.0);
                        cmd.Parameters.AddWithValue("@val", 0.0);
                        ExecuteNonQueryLogged(cmd);
                    }

                    if (c.ConstructionResources != null)
                    {
                        foreach (var kvp in c.ConstructionResources)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                cmd.CommandText = @"INSERT INTO CommodityResources (CommodityName, ResourceName, Quantity)
                                                   VALUES (@commName, @resName, @qty)";
                                cmd.Parameters.AddWithValue("@commName", c.Name ?? c.ID ?? string.Empty);
                                cmd.Parameters.AddWithValue("@resName", kvp.Key ?? string.Empty);
                                int qty = 0;
                                int.TryParse(kvp.Value, out qty);
                                cmd.Parameters.AddWithValue("@qty", qty);
                                ExecuteNonQueryLogged(cmd);
                            }
                        }
                    }
                }

                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<RefiningRecipe>> GetAllRefiningRecipesAsync()
        {
            var results = new List<RefiningRecipe>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM RefiningRecipes";
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new RefiningRecipe
                        {
                            InputResource = reader["InputResource"] as string ?? string.Empty,
                            InputPurity = reader["InputPurity"] as string ?? string.Empty,
                            OutputResource = reader["OutputResource"] as string ?? string.Empty,
                            ConsumeRate = Convert.ToInt32(reader["OutputQuantity"]),
                            ProduceRate = Convert.ToInt32(reader["OutputQuantity"]),
                            Tier = 0,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<RefiningRecipe>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertRefiningRecipesAsync(IReadOnlyList<RefiningRecipe> recipes)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                using (var del = conn.CreateCommand())
                {
                    del.Transaction = tx;
                    del.CommandText = "DELETE FROM RefiningRecipes";
                    ExecuteNonQueryLogged(del);
                }

                int idx = 0;
                foreach (var r in recipes)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = @"INSERT INTO RefiningRecipes (Id, InputResource, InputPurity, OutputResource, OutputPurity, OutputQuantity, ProcessingTime)
                                           VALUES (@id, @inRes, @inPur, @outRes, @outPur, @outQty, @procTime)";
                        cmd.Parameters.AddWithValue("@id", $"recipe_{idx++}");
                        cmd.Parameters.AddWithValue("@inRes", r.InputResource ?? string.Empty);
                        cmd.Parameters.AddWithValue("@inPur", r.InputPurity ?? string.Empty);
                        cmd.Parameters.AddWithValue("@outRes", r.OutputResource ?? string.Empty);
                        cmd.Parameters.AddWithValue("@outPur", string.Empty);
                        cmd.Parameters.AddWithValue("@outQty", r.ProduceRate);
                        cmd.Parameters.AddWithValue("@procTime", 0);
                        ExecuteNonQueryLogged(cmd);
                    }
                }

                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ResearchTimeEntry>> GetAllResearchTimesAsync()
        {
            var results = new List<ResearchTimeEntry>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM ResearchTimes";
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new ResearchTimeEntry
                        {
                            Evolution = Convert.ToInt32(reader["BaseTimeMinutes"]),
                            ResearchTimeSeconds = Convert.ToInt64(reader["BaseTimeMinutes"]) * 60,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<ResearchTimeEntry>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertResearchTimesAsync(IReadOnlyList<ResearchTimeEntry> entries)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                using (var del = conn.CreateCommand())
                {
                    del.Transaction = tx;
                    del.CommandText = "DELETE FROM ResearchTimes";
                    ExecuteNonQueryLogged(del);
                }

                int idx = 0;
                foreach (var e in entries)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = @"INSERT INTO ResearchTimes (Id, BlueprintType, TechLevel, PropertyKey, BaseTimeMinutes)
                                           VALUES (@id, @bpType, @tech, @propKey, @minutes)";
                        cmd.Parameters.AddWithValue("@id", $"rt_{idx++}");
                        cmd.Parameters.AddWithValue("@bpType", string.Empty);
                        cmd.Parameters.AddWithValue("@tech", string.Empty);
                        cmd.Parameters.AddWithValue("@propKey", string.Empty);
                        cmd.Parameters.AddWithValue("@minutes", (int)(e.ResearchTimeSeconds / 60));
                        ExecuteNonQueryLogged(cmd);
                    }
                }

                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<PropertyTypeDefinition>> GetAllPropertyTypeDefinitionsAsync()
        {
            var results = new List<PropertyTypeDefinition>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM PropertyTypeDefinitions";
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        results.Add(new PropertyTypeDefinition
                        {
                            ModTypeId = 0,
                            PropertyName = reader["Key"] as string ?? string.Empty,
                            FriendlyPropertyName = reader["DisplayName"] as string ?? string.Empty,
                            Unit = reader["Unit"] as string ?? string.Empty,
                        });
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<PropertyTypeDefinition>>(results);
        }

        /// <inheritdoc/>
        public Task UpsertPropertyTypeDefinitionsAsync(IReadOnlyList<PropertyTypeDefinition> definitions)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                using (var del = conn.CreateCommand())
                {
                    del.Transaction = tx;
                    del.CommandText = "DELETE FROM PropertyTypeDefinitions";
                    ExecuteNonQueryLogged(del);
                }

                foreach (var d in definitions)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = @"INSERT INTO PropertyTypeDefinitions (Key, DisplayName, Category, DataType, Unit)
                                           VALUES (@key, @display, @cat, @dataType, @unit)";
                        cmd.Parameters.AddWithValue("@key", d.PropertyName ?? string.Empty);
                        cmd.Parameters.AddWithValue("@display", d.FriendlyPropertyName ?? string.Empty);
                        cmd.Parameters.AddWithValue("@cat", string.Empty);
                        cmd.Parameters.AddWithValue("@dataType", "string");
                        cmd.Parameters.AddWithValue("@unit", d.Unit ?? string.Empty);
                        ExecuteNonQueryLogged(cmd);
                    }
                }

                tx.Commit();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<Blueprint>> GetAllGlobalBlueprintsAsync()
        {
            var results = new List<Blueprint>();
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Blueprints WHERE OwnerUUID = ''";
                using (var reader = ExecuteReaderLogged(cmd))
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
        public Task UpsertGlobalBlueprintsAsync(IReadOnlyList<Blueprint> blueprints)
        {
            using (var conn = OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                foreach (var bp in blueprints)
                {
                    UpsertBlueprintParent(conn, tx, string.Empty, bp);
                    DeleteBlueprintChildren(conn, tx, bp.UUID);
                    InsertBlueprintChildren(conn, tx, bp);
                }

                tx.Commit();
            }

            return Task.CompletedTask;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Private Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <summary>
        /// Detects whether the legacy JSON-blob EntityData table exists in the
        /// database, indicating the old server schema (pre-normalized).
        /// </summary>
        /// <param name="conn">An open SQLite connection.</param>
        /// <returns>True if the legacy EntityData table exists.</returns>
        private static bool DetectLegacySchema(SqliteConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='EntityData'";
                var result = ExecuteScalarLogged(cmd);
                return result != null;
            }
        }

        /// <summary>
        /// Applies pending schema migrations sequentially from <paramref name="fromVersion"/>
        /// up to <see cref="CurrentSchemaVersion"/>. Each migration runs inside its own
        /// transaction; on failure the transaction is rolled back and a
        /// <see cref="StorageLoadException"/> is thrown. If the rollback itself fails, a
        /// <see cref="StorageCorruptionException"/> is thrown containing both errors.
        /// </summary>
        /// <param name="conn">An open SQLite connection.</param>
        /// <param name="fromVersion">The current on-disk schema version.</param>
        /// <param name="databasePath">The database file path for error reporting.</param>
        private static void RunMigrations(SqliteConnection conn, int fromVersion, string databasePath)
        {
            for (int i = fromVersion; i < CurrentSchemaVersion; i++)
            {
                int migrationIndex = i - 1; // Migrations[0] goes from v1 â†’ v2
                if (migrationIndex < 0 || migrationIndex >= Migrations.Count)
                {
                    continue;
                }

                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        Migrations[migrationIndex](conn, tx);
                        SetSchemaVersion(conn, i + 1);
                        tx.Commit();
                        Log.Info("Migrated schema from version {0} to {1}", i, i + 1);
                    }
                    catch (Exception migrationEx)
                    {
                        Log.Error(migrationEx, "Schema migration from v{0} to v{1} failed", i, i + 1);
                        try
                        {
                            tx.Rollback();
                        }
                        catch (Exception rollbackEx)
                        {
                            throw new StorageCorruptionException("Sqlite", migrationEx, rollbackEx);
                        }

                        throw new StorageLoadException(
                            "Sqlite",
                            databasePath,
                            $"Schema migration from v{i} to v{i + 1} failed",
                            migrationEx);
                    }
                }
            }
        }

        /// <summary>
        /// Migration v1 â†’ v2: Rebuilds the BankingTransactions table to use TEXT
        /// columns for CreditChange, OldBalance, and NewBalance instead of REAL,
        /// preserving decimal precision on round-trip.
        /// </summary>
        private static void MigrateBankingTransactionsDecimalToText(SqliteConnection conn, SqliteTransaction tx)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
ALTER TABLE BankingTransactions RENAME TO BankingTransactions_old;

CREATE TABLE BankingTransactions (
    UUID TEXT PRIMARY KEY,
    OwnerUUID TEXT NOT NULL DEFAULT '',
    TransactionDateTime TEXT NOT NULL DEFAULT '',
    CreditChange TEXT NOT NULL DEFAULT '0',
    OldBalance TEXT NOT NULL DEFAULT '0',
    NewBalance TEXT NOT NULL DEFAULT '0',
    TransactionType INTEGER NOT NULL DEFAULT 0,
    Detail TEXT NOT NULL DEFAULT '',
    CharacterId INTEGER,
    SystemObjectId INTEGER,
    SystemId INTEGER,
    IsManualEntry INTEGER NOT NULL DEFAULT 0
);

INSERT INTO BankingTransactions (UUID, OwnerUUID, TransactionDateTime, CreditChange, OldBalance, NewBalance, TransactionType, Detail, CharacterId, SystemObjectId, SystemId, IsManualEntry)
SELECT UUID, OwnerUUID, TransactionDateTime, CAST(CreditChange AS TEXT), CAST(OldBalance AS TEXT), CAST(NewBalance AS TEXT), TransactionType, Detail, CharacterId, SystemObjectId, SystemId, IsManualEntry
FROM BankingTransactions_old;

DROP TABLE BankingTransactions_old;
";
                ExecuteNonQueryLogged(cmd);
            }
        }

        /// <summary>
        /// Migration v2 â†’ v3: Rebuilds the MarketTransactions table to use TEXT
        /// columns for PricePerUnit, TotalPrice, and MaxRepairPercent instead of REAL,
        /// preserving decimal precision on round-trip.
        /// </summary>
        private static void MigrateMarketTransactionsDecimalToText(SqliteConnection conn, SqliteTransaction tx)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
ALTER TABLE MarketTransactions RENAME TO MarketTransactions_old;

CREATE TABLE MarketTransactions (
    UUID TEXT PRIMARY KEY,
    OwnerUUID TEXT NOT NULL DEFAULT '',
    TransactionType TEXT NOT NULL DEFAULT 'Buy',
    ItemType TEXT NOT NULL DEFAULT 'None',
    ItemReferenceID TEXT NOT NULL DEFAULT '',
    ItemName TEXT NOT NULL DEFAULT '',
    Quantity INTEGER NOT NULL DEFAULT 0,
    PricePerUnit TEXT NOT NULL DEFAULT '0',
    TotalPrice TEXT NOT NULL DEFAULT '0',
    Counterparty TEXT NOT NULL DEFAULT '',
    CounterpartyFaction TEXT NOT NULL DEFAULT '',
    StationUUID TEXT NOT NULL DEFAULT '',
    Timestamp TEXT NOT NULL DEFAULT '',
    Notes TEXT NOT NULL DEFAULT '',
    ListingUUID TEXT NOT NULL DEFAULT '',
    CurrentHP INTEGER NOT NULL DEFAULT 0,
    MaxHP INTEGER NOT NULL DEFAULT 0,
    MaxRepairPercent TEXT NOT NULL DEFAULT '0'
);

INSERT INTO MarketTransactions (UUID, OwnerUUID, TransactionType, ItemType, ItemReferenceID, ItemName, Quantity, PricePerUnit, TotalPrice, Counterparty, CounterpartyFaction, StationUUID, Timestamp, Notes, ListingUUID, CurrentHP, MaxHP, MaxRepairPercent)
SELECT UUID, OwnerUUID, TransactionType, ItemType, ItemReferenceID, ItemName, Quantity, CAST(PricePerUnit AS TEXT), CAST(TotalPrice AS TEXT), Counterparty, CounterpartyFaction, StationUUID, Timestamp, Notes, ListingUUID, CurrentHP, MaxHP, CAST(MaxRepairPercent AS TEXT)
FROM MarketTransactions_old;

DROP TABLE MarketTransactions_old;
";
                ExecuteNonQueryLogged(cmd);
            }
        }

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
                using (var reader = ExecuteReaderLogged(cmd))
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
                using (var reader = ExecuteReaderLogged(cmd))
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
                using (var reader = ExecuteReaderLogged(cmd))
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

                        if (reader[BlueprintPropertyKeys.Mass] != DBNull.Value)
                        {
                            item.Mass = Convert.ToDecimal(reader[BlueprintPropertyKeys.Mass]);
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
                ExecuteNonQueryLogged(cmd);
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
                ExecuteNonQueryLogged(cmd);
            }

            // Delete structure items
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"DELETE FROM Items WHERE ParentType = 'ColonyStructure' AND ParentUUID IN
                    (SELECT UUID FROM ColonyStructures WHERE ColonyUUID = @uuid)";
                cmd.Parameters.AddWithValue("@uuid", colonyUUID);
                ExecuteNonQueryLogged(cmd);
            }

            // CASCADE handles ColonyStructureProperties and ColonyStructureWorkers
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM ColonyStructures WHERE ColonyUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", colonyUUID);
                ExecuteNonQueryLogged(cmd);
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

                    ExecuteNonQueryLogged(cmd);
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
                        ExecuteNonQueryLogged(propCmd);
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
                        ExecuteNonQueryLogged(wCmd);
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
                    ExecuteNonQueryLogged(cmd);
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Blueprint Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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
                using (var reader = ExecuteReaderLogged(cmd))
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
                ExecuteNonQueryLogged(cmd);
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
                ExecuteNonQueryLogged(cmd);
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM BlueprintResources WHERE BlueprintUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", blueprintUUID);
                ExecuteNonQueryLogged(cmd);
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
                        ExecuteNonQueryLogged(cmd);
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
                        ExecuteNonQueryLogged(cmd);
                    }
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Survey Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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
                using (var reader = ExecuteReaderLogged(cmd))
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
                using (var reader = ExecuteReaderLogged(cmd))
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
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void DeleteSurveyChildren(SqliteConnection conn, SqliteTransaction tx, string surveyUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM SurveyProperties WHERE SurveyUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", surveyUUID);
                ExecuteNonQueryLogged(cmd);
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM SurveyResources WHERE SurveyUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", surveyUUID);
                ExecuteNonQueryLogged(cmd);
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
                        ExecuteNonQueryLogged(cmd);
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
                        ExecuteNonQueryLogged(cmd);
                    }
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Station Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static Station ReadStationParent(SqliteDataReader reader)
        {
            var station = new Station
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                SystemName = reader["SystemName"] as string ?? string.Empty,
                SystemId = Convert.ToInt32(reader["SystemId"]),
            };

            if (reader["GameLocationId"] != DBNull.Value)
            {
                station.GameLocationId = Convert.ToInt32(reader["GameLocationId"]);
            }

            return station;
        }

        private static List<ShipComponentSlot> LoadStationComponents(SqliteConnection conn, string stationUUID)
        {
            var components = new List<ShipComponentSlot>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM StationComponents WHERE StationUUID = @sUUID ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@sUUID", stationUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        components.Add(new ShipComponentSlot
                        {
                            SlotType = reader["SlotType"] as string ?? string.Empty,
                            BlueprintUUID = reader["BlueprintUUID"] as string ?? string.Empty,
                            CurrentHP = Convert.ToInt32(reader["CurrentHP"]),
                            MaxHP = Convert.ToInt32(reader["MaxHP"]),
                        });
                    }
                }
            }

            return components;
        }

        private static void LoadStationItems(SqliteConnection conn, Station station)
        {
            station.MunitionsHold = LoadItems(conn, station.UUID, "StationMunitions");
            station.Holds = new Dictionary<string, ItemBag>();

            // Load all station hold items grouped by ParentType pattern 'StationHold:holdName'
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT DISTINCT ParentType FROM Items WHERE ParentUUID = @uuid AND ParentType LIKE 'StationHold:%'";
                cmd.Parameters.AddWithValue("@uuid", station.UUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    var holdTypes = new List<string>();
                    while (reader.Read())
                    {
                        holdTypes.Add(reader.GetString(0));
                    }

                    foreach (var holdType in holdTypes)
                    {
                        var holdName = holdType.Substring("StationHold:".Length);
                        station.Holds[holdName] = LoadItems(conn, station.UUID, holdType);
                    }
                }
            }
        }

        private static void UpsertStationParent(SqliteConnection conn, SqliteTransaction tx, string characterUUID, Station entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO Stations (UUID, Name, OwnerUUID, SystemName, SystemId, SystemObjectId, GameLocationId)
                                   VALUES (@uuid, @name, @owner, @sysName, @sysId, @sysObjId, @gameLocId)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@sysName", entity.SystemName ?? string.Empty);
                cmd.Parameters.AddWithValue("@sysId", entity.SystemId ?? 0);
                cmd.Parameters.AddWithValue("@sysObjId", 0);
                cmd.Parameters.AddWithValue("@gameLocId", entity.GameLocationId.HasValue ? (object)entity.GameLocationId.Value : DBNull.Value);
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void DeleteStationChildren(SqliteConnection conn, SqliteTransaction tx, string stationUUID)
        {
            // Delete items (polymorphic FK, no CASCADE)
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM Items WHERE ParentUUID = @uuid AND (ParentType = 'StationMunitions' OR ParentType LIKE 'StationHold:%')";
                cmd.Parameters.AddWithValue("@uuid", stationUUID);
                ExecuteNonQueryLogged(cmd);
            }

            // Delete components
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM StationComponents WHERE StationUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", stationUUID);
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void InsertStationComponents(SqliteConnection conn, SqliteTransaction tx, Station entity)
        {
            if (entity.Components == null)
            {
                return;
            }

            for (int i = 0; i < entity.Components.Count; i++)
            {
                var comp = entity.Components[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO StationComponents (StationUUID, Sequence, SlotType, BlueprintUUID, CurrentHP, MaxHP)
                                       VALUES (@sUUID, @seq, @slotType, @bpUUID, @curHp, @maxHp)";
                    cmd.Parameters.AddWithValue("@sUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", i);
                    cmd.Parameters.AddWithValue("@slotType", comp.SlotType ?? string.Empty);
                    cmd.Parameters.AddWithValue("@bpUUID", comp.BlueprintUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@curHp", comp.CurrentHP);
                    cmd.Parameters.AddWithValue("@maxHp", comp.MaxHP);
                    ExecuteNonQueryLogged(cmd);
                }
            }
        }

        private static void InsertStationItems(SqliteConnection conn, SqliteTransaction tx, Station entity)
        {
            // Insert munitions hold
            InsertItems(conn, tx, entity.UUID, "StationMunitions", entity.MunitionsHold);

            // Insert named holds
            if (entity.Holds != null)
            {
                foreach (var kvp in entity.Holds)
                {
                    InsertItems(conn, tx, entity.UUID, "StationHold:" + kvp.Key, kvp.Value);
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Asteroid Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static Asteroid ReadAsteroidParent(SqliteDataReader reader)
        {
            var asteroid = new Asteroid
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                SystemName = reader["SystemName"] as string ?? string.Empty,
                SystemObjectId = Convert.ToInt32(reader["SystemObjectId"]),
            };

            return asteroid;
        }

        private static List<AsteroidReserve> LoadAsteroidReserves(SqliteConnection conn, string asteroidUUID)
        {
            var reserves = new List<AsteroidReserve>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM AsteroidReserves WHERE AsteroidUUID = @aUUID ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@aUUID", asteroidUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        var reserve = new AsteroidReserve
                        {
                            ResourceName = reader["ResourceName"] as string ?? string.Empty,
                            Purity = reader["Purity"] as string ?? string.Empty,
                            MaxReserve = Convert.ToInt32(reader["MaxReserve"]),
                        };

                        if (reader["CurrentReserve"] != DBNull.Value)
                        {
                            reserve.CurrentReserve = Convert.ToInt32(reader["CurrentReserve"]);
                        }

                        if (reader["ResetTimestamp"] != DBNull.Value)
                        {
                            reserve.ResetTimestamp = reader["ResetTimestamp"] as string ?? string.Empty;
                        }

                        reserves.Add(reserve);
                    }
                }
            }

            return reserves;
        }

        private static void UpsertAsteroidParent(SqliteConnection conn, SqliteTransaction tx, string characterUUID, Asteroid entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO Asteroids (UUID, Name, OwnerUUID, SystemName, SystemId, SystemObjectId, GameApiAsteroidId)
                                   VALUES (@uuid, @name, @owner, @sysName, @sysId, @sysObjId, @gameApiId)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@sysName", entity.SystemName ?? string.Empty);
                cmd.Parameters.AddWithValue("@sysId", 0);
                cmd.Parameters.AddWithValue("@sysObjId", entity.SystemObjectId);
                cmd.Parameters.AddWithValue("@gameApiId", DBNull.Value);
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void DeleteAsteroidChildren(SqliteConnection conn, SqliteTransaction tx, string asteroidUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM AsteroidReserves WHERE AsteroidUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", asteroidUUID);
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void InsertAsteroidReserves(SqliteConnection conn, SqliteTransaction tx, Asteroid entity)
        {
            if (entity.Reserves == null)
            {
                return;
            }

            for (int i = 0; i < entity.Reserves.Count; i++)
            {
                var reserve = entity.Reserves[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO AsteroidReserves (AsteroidUUID, Sequence, ResourceName, Purity, MaxReserve, CurrentReserve, ResetTimestamp)
                                       VALUES (@aUUID, @seq, @resName, @purity, @maxRes, @curRes, @resetTs)";
                    cmd.Parameters.AddWithValue("@aUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", i);
                    cmd.Parameters.AddWithValue("@resName", reserve.ResourceName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@purity", reserve.Purity ?? string.Empty);
                    cmd.Parameters.AddWithValue("@maxRes", reserve.MaxReserve);
                    cmd.Parameters.AddWithValue("@curRes", reserve.CurrentReserve != 0 ? (object)reserve.CurrentReserve : DBNull.Value);
                    cmd.Parameters.AddWithValue("@resetTs", !string.IsNullOrEmpty(reserve.ResetTimestamp) ? (object)reserve.ResetTimestamp : DBNull.Value);
                    ExecuteNonQueryLogged(cmd);
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // SupplyChain Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static SupplyChain ReadSupplyChainParent(SqliteDataReader reader)
        {
            return new SupplyChain
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
            };
        }

        private static List<SupplyChainStage> LoadSupplyChainStages(SqliteConnection conn, string chainUUID)
        {
            var stages = new List<SupplyChainStage>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM SupplyChainStages WHERE SupplyChainUUID = @cUUID ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@cUUID", chainUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        stages.Add(new SupplyChainStage
                        {
                            Sequence = Convert.ToInt32(reader["Sequence"]),
                            LocationUUID = reader["ColonyUUID"] as string ?? string.Empty,
                            ResourceName = reader["OutputItemType"] as string ?? string.Empty,
                            AccumulationThreshold = Convert.ToInt32(reader["OutputQuantity"]),
                        });
                    }
                }
            }

            return stages;
        }

        private static void UpsertSupplyChainParent(SqliteConnection conn, SqliteTransaction tx, string characterUUID, SupplyChain entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO SupplyChains (UUID, Name, OwnerUUID, Description)
                                   VALUES (@uuid, @name, @owner, @desc)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@desc", string.Empty);
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void DeleteSupplyChainChildren(SqliteConnection conn, SqliteTransaction tx, string chainUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM SupplyChainStages WHERE SupplyChainUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", chainUUID);
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void InsertSupplyChainStages(SqliteConnection conn, SqliteTransaction tx, SupplyChain entity)
        {
            if (entity.Stages == null)
            {
                return;
            }

            for (int i = 0; i < entity.Stages.Count; i++)
            {
                var stage = entity.Stages[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO SupplyChainStages (SupplyChainUUID, Sequence, ColonyUUID, BlueprintUUID, OutputItemType, OutputQuantity)
                                       VALUES (@cUUID, @seq, @colUUID, @bpUUID, @outputType, @outputQty)";
                    cmd.Parameters.AddWithValue("@cUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", i);
                    cmd.Parameters.AddWithValue("@colUUID", stage.LocationUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@bpUUID", string.Empty);
                    cmd.Parameters.AddWithValue("@outputType", stage.ResourceName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@outputQty", stage.AccumulationThreshold);
                    ExecuteNonQueryLogged(cmd);
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // BuildPlan Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static BuildPlan ReadBuildPlanParent(SqliteDataReader reader)
        {
            return new BuildPlan
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
            };
        }

        private static List<BuildItem> LoadBuildItems(SqliteConnection conn, string planUUID)
        {
            var items = new List<BuildItem>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM BuildItems WHERE BuildPlanUUID = @pUUID ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@pUUID", planUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        items.Add(new BuildItem
                        {
                            UUID = Guid.NewGuid().ToString(),
                            ItemName = reader["ResourceName"] as string ?? string.Empty,
                            Quantity = Convert.ToInt32(reader["Quantity"]),
                        });
                    }
                }
            }

            return items;
        }

        private static void UpsertBuildPlanParent(SqliteConnection conn, SqliteTransaction tx, string characterUUID, BuildPlan entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO BuildPlans (UUID, Name, OwnerUUID, ColonyUUID, BlueprintUUID, Quantity, Priority)
                                   VALUES (@uuid, @name, @owner, @colony, @bp, @qty, @priority)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@colony", string.Empty);
                cmd.Parameters.AddWithValue("@bp", string.Empty);
                cmd.Parameters.AddWithValue("@qty", 0);
                cmd.Parameters.AddWithValue("@priority", 0);
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void DeleteBuildPlanChildren(SqliteConnection conn, SqliteTransaction tx, string planUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM BuildItems WHERE BuildPlanUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", planUUID);
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void InsertBuildItems(SqliteConnection conn, SqliteTransaction tx, BuildPlan entity)
        {
            if (entity.Items == null)
            {
                return;
            }

            for (int i = 0; i < entity.Items.Count; i++)
            {
                var item = entity.Items[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO BuildItems (BuildPlanUUID, Sequence, ResourceName, Quantity, Fulfilled)
                                       VALUES (@pUUID, @seq, @resName, @qty, @fulfilled)";
                    cmd.Parameters.AddWithValue("@pUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", i);
                    cmd.Parameters.AddWithValue("@resName", item.ItemName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@qty", item.Quantity);
                    cmd.Parameters.AddWithValue("@fulfilled", 0);
                    ExecuteNonQueryLogged(cmd);
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // StockProfile Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static StockProfile ReadStockProfileParent(SqliteDataReader reader)
        {
            return new StockProfile
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
            };
        }

        private static List<StockProfileEntry> LoadStockProfileEntries(SqliteConnection conn, string profileUUID)
        {
            var entries = new List<StockProfileEntry>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM StockProfileEntries WHERE StockProfileUUID = @pUUID ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@pUUID", profileUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        entries.Add(new StockProfileEntry
                        {
                            GroupID = reader["ResourceName"] as string ?? string.Empty,
                            StockPlanUUID = string.Empty,
                        });
                    }
                }
            }

            return entries;
        }

        private static void UpsertStockProfileParent(SqliteConnection conn, SqliteTransaction tx, string characterUUID, StockProfile entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO StockProfiles (UUID, Name, OwnerUUID, Description)
                                   VALUES (@uuid, @name, @owner, @desc)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@desc", string.Empty);
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void DeleteStockProfileChildren(SqliteConnection conn, SqliteTransaction tx, string profileUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM StockProfileEntries WHERE StockProfileUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", profileUUID);
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void InsertStockProfileEntries(SqliteConnection conn, SqliteTransaction tx, StockProfile entity)
        {
            if (entity.Entries == null)
            {
                return;
            }

            for (int i = 0; i < entity.Entries.Count; i++)
            {
                var entry = entity.Entries[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO StockProfileEntries (StockProfileUUID, Sequence, ResourceName, MinQuantity, MaxQuantity)
                                       VALUES (@pUUID, @seq, @resName, @minQty, @maxQty)";
                    cmd.Parameters.AddWithValue("@pUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", i);
                    cmd.Parameters.AddWithValue("@resName", entry.GroupID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@minQty", 0);
                    cmd.Parameters.AddWithValue("@maxQty", 0);
                    ExecuteNonQueryLogged(cmd);
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // StockPlan Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static StockPlan ReadStockPlanParent(SqliteDataReader reader)
        {
            return new StockPlan
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
            };
        }

        private static List<StockTarget> LoadStockTargets(SqliteConnection conn, string planUUID)
        {
            var targets = new List<StockTarget>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM StockTargets WHERE StockPlanUUID = @pUUID ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@pUUID", planUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        targets.Add(new StockTarget
                        {
                            UUID = Guid.NewGuid().ToString(),
                            ItemName = reader["ResourceName"] as string ?? string.Empty,
                            TargetQuantity = Convert.ToInt32(reader["TargetQuantity"]),
                            CriticalThreshold = Convert.ToInt32(reader["Priority"]),
                        });
                    }
                }
            }

            return targets;
        }

        private static void UpsertStockPlanParent(SqliteConnection conn, SqliteTransaction tx, string characterUUID, StockPlan entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO StockPlans (UUID, Name, OwnerUUID, ColonyUUID)
                                   VALUES (@uuid, @name, @owner, @colony)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@colony", string.Empty);
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void DeleteStockPlanChildren(SqliteConnection conn, SqliteTransaction tx, string planUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM StockTargets WHERE StockPlanUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", planUUID);
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void InsertStockTargets(SqliteConnection conn, SqliteTransaction tx, StockPlan entity)
        {
            if (entity.Targets == null)
            {
                return;
            }

            for (int i = 0; i < entity.Targets.Count; i++)
            {
                var target = entity.Targets[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO StockTargets (StockPlanUUID, Sequence, ResourceName, TargetQuantity, Priority)
                                       VALUES (@pUUID, @seq, @resName, @targetQty, @priority)";
                    cmd.Parameters.AddWithValue("@pUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", i);
                    cmd.Parameters.AddWithValue("@resName", target.ItemName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@targetQty", target.TargetQuantity);
                    cmd.Parameters.AddWithValue("@priority", target.CriticalThreshold);
                    ExecuteNonQueryLogged(cmd);
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // PricingPlan Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static PricingPlan ReadPricingPlanParent(SqliteDataReader reader)
        {
            return new PricingPlan
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                Description = reader["Description"] as string ?? string.Empty,
                FixedCostPerItem = Convert.ToDecimal(reader["FixedCostPerItem"]),
                HourlyCostRate = Convert.ToDecimal(reader["HourlyCostRate"]),
            };
        }

        private static Dictionary<string, decimal> LoadPricingPlanPrices(SqliteConnection conn, string planUUID)
        {
            var prices = new Dictionary<string, decimal>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ResourceName, Price FROM PricingPlanPrices WHERE PricingPlanUUID = @pUUID";
                cmd.Parameters.AddWithValue("@pUUID", planUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        var name = reader.GetString(0);
                        var price = Convert.ToDecimal(reader["Price"]);
                        prices[name] = price;
                    }
                }
            }

            return prices;
        }

        private static void UpsertPricingPlanParent(SqliteConnection conn, SqliteTransaction tx, string characterUUID, PricingPlan entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO PricingPlans (UUID, Name, OwnerUUID, Description, FixedCostPerItem, HourlyCostRate)
                                   VALUES (@uuid, @name, @owner, @desc, @fixed, @hourly)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@desc", entity.Description ?? string.Empty);
                cmd.Parameters.AddWithValue("@fixed", (double)entity.FixedCostPerItem);
                cmd.Parameters.AddWithValue("@hourly", (double)entity.HourlyCostRate);
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void DeletePricingPlanChildren(SqliteConnection conn, SqliteTransaction tx, string planUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM PricingPlanPrices WHERE PricingPlanUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", planUUID);
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void InsertPricingPlanPrices(SqliteConnection conn, SqliteTransaction tx, PricingPlan entity)
        {
            if (entity.ResourcePrices == null)
            {
                return;
            }

            foreach (var kvp in entity.ResourcePrices)
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = "INSERT INTO PricingPlanPrices (PricingPlanUUID, ResourceName, Price) VALUES (@pUUID, @resName, @price)";
                    cmd.Parameters.AddWithValue("@pUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@resName", kvp.Key);
                    cmd.Parameters.AddWithValue("@price", (double)kvp.Value);
                    ExecuteNonQueryLogged(cmd);
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // PlayerProfile Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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
                using (var reader = ExecuteReaderLogged(cmd))
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
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void DeletePlayerProfileChildren(SqliteConnection conn, SqliteTransaction tx, string playerUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM PlayerSkills WHERE PlayerUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", playerUUID);
                ExecuteNonQueryLogged(cmd);
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

                    ExecuteNonQueryLogged(cmd);
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // DeliveryRoute Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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
                using (var reader = ExecuteReaderLogged(cmd))
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
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void DeleteDeliveryRouteChildren(SqliteConnection conn, SqliteTransaction tx, string routeUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM DeliveryRouteStops WHERE DeliveryRouteUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", routeUUID);
                ExecuteNonQueryLogged(cmd);
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
                    ExecuteNonQueryLogged(cmd);
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // DeliveryPlan Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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
                using (var reader = ExecuteReaderLogged(cmd))
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
                using (var reader = ExecuteReaderLogged(cmd))
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
                ExecuteNonQueryLogged(cmd);
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
                ExecuteNonQueryLogged(cmd);
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM DeliveryPlanStops WHERE DeliveryPlanUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", planUUID);
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void InsertDeliveryPlanChildren(SqliteConnection conn, SqliteTransaction tx, DeliveryPlan entity)
        {
            if (entity.Stops == null)
            {
                return;
            }

            // Repair duplicate sequences before inserting â€” renumber all stops
            // sequentially to ensure uniqueness without losing any data.
            for (int i = 0; i < entity.Stops.Count; i++)
            {
                entity.Stops[i].Sequence = i + 1;
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
                    ExecuteNonQueryLogged(cmd);
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
                    ExecuteNonQueryLogged(cmd);
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Ship Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static Ship ReadShipParent(SqliteDataReader reader)
        {
            var ship = new Ship
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                TemplateUUID = reader["TemplateUUID"] as string ?? string.Empty,
                HullBlueprintUUID = reader["HullBlueprintUUID"] as string ?? string.Empty,
                LocationUUID = reader["LocationUUID"] as string ?? string.Empty,
                HullCurrentHP = Convert.ToInt32(reader["HullCurrentHP"]),
                HullMaxHP = Convert.ToInt32(reader["HullMaxHP"]),
                HullMaxRepairPercent = Convert.ToDecimal(reader["HullMaxRepairPercent"]),
            };

            var locTypeStr = reader["LocationType"] as string;
            if (Enum.TryParse<DestinationType>(locTypeStr, true, out var lt))
            {
                ship.LocationType = lt;
            }

            if (reader["GameLocationId"] != DBNull.Value)
            {
                ship.GameLocationId = Convert.ToInt32(reader["GameLocationId"]);
            }

            return ship;
        }

        private static List<ShipComponentSlot> LoadShipComponents(SqliteConnection conn, string shipUUID)
        {
            var components = new List<ShipComponentSlot>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM ShipComponents WHERE ShipUUID = @sUUID ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@sUUID", shipUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        components.Add(new ShipComponentSlot
                        {
                            SlotType = reader["SlotType"] as string ?? string.Empty,
                            BlueprintUUID = reader["BlueprintUUID"] as string ?? string.Empty,
                            CurrentHP = Convert.ToInt32(reader["CurrentHP"]),
                            MaxHP = Convert.ToInt32(reader["MaxHP"]),
                        });
                    }
                }
            }

            return components;
        }

        private static void UpsertShipParent(SqliteConnection conn, SqliteTransaction tx, string characterUUID, Ship entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO Ships (
                    UUID, Name, OwnerUUID, TemplateUUID, HullBlueprintUUID,
                    LocationType, LocationUUID, GameLocationId,
                    HullCurrentHP, HullMaxHP, HullMaxRepairPercent
                ) VALUES (
                    @uuid, @name, @owner, @template, @hullBp,
                    @locType, @locUUID, @gameLocId,
                    @hullCurHp, @hullMaxHp, @hullMaxRepair
                )";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@template", entity.TemplateUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@hullBp", entity.HullBlueprintUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@locType", entity.LocationType.ToString());
                cmd.Parameters.AddWithValue("@locUUID", entity.LocationUUID ?? string.Empty);
                cmd.Parameters.AddWithValue("@gameLocId", entity.GameLocationId.HasValue ? (object)entity.GameLocationId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@hullCurHp", entity.HullCurrentHP);
                cmd.Parameters.AddWithValue("@hullMaxHp", entity.HullMaxHP);
                cmd.Parameters.AddWithValue("@hullMaxRepair", (double)entity.HullMaxRepairPercent);
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void DeleteShipChildren(SqliteConnection conn, SqliteTransaction tx, string shipUUID)
        {
            // Delete items (polymorphic FK, no CASCADE)
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM Items WHERE ParentUUID = @uuid AND (ParentType = 'ShipCargo' OR ParentType = 'ShipHopper')";
                cmd.Parameters.AddWithValue("@uuid", shipUUID);
                ExecuteNonQueryLogged(cmd);
            }

            // Delete components (CASCADE would handle this on parent delete, but for upsert we delete explicitly)
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM ShipComponents WHERE ShipUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", shipUUID);
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void InsertShipComponents(SqliteConnection conn, SqliteTransaction tx, Ship entity)
        {
            if (entity.Components == null)
            {
                return;
            }

            for (int i = 0; i < entity.Components.Count; i++)
            {
                var comp = entity.Components[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO ShipComponents (ShipUUID, Sequence, SlotType, BlueprintUUID, CurrentHP, MaxHP)
                                       VALUES (@sUUID, @seq, @slotType, @bpUUID, @curHp, @maxHp)";
                    cmd.Parameters.AddWithValue("@sUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", i);
                    cmd.Parameters.AddWithValue("@slotType", comp.SlotType ?? string.Empty);
                    cmd.Parameters.AddWithValue("@bpUUID", comp.BlueprintUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@curHp", comp.CurrentHP);
                    cmd.Parameters.AddWithValue("@maxHp", comp.MaxHP);
                    ExecuteNonQueryLogged(cmd);
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // ShipTemplate Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static ShipTemplate ReadShipTemplateParent(SqliteDataReader reader)
        {
            return new ShipTemplate
            {
                UUID = reader["UUID"] as string,
                Name = reader["Name"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                HullBlueprintUUID = reader["HullBlueprintUUID"] as string ?? string.Empty,
            };
        }

        private static List<ShipComponentSlot> LoadShipTemplateComponents(SqliteConnection conn, string templateUUID)
        {
            var components = new List<ShipComponentSlot>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM ShipTemplateComponents WHERE ShipTemplateUUID = @tUUID ORDER BY Sequence";
                cmd.Parameters.AddWithValue("@tUUID", templateUUID);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        components.Add(new ShipComponentSlot
                        {
                            SlotType = reader["SlotType"] as string ?? string.Empty,
                            SlotIndex = Convert.ToInt32(reader["SlotIndex"]),
                            BlueprintUUID = reader["BlueprintUUID"] as string ?? string.Empty,
                            CurrentHP = Convert.ToInt32(reader["CurrentHP"]),
                            MaxHP = Convert.ToInt32(reader["MaxHP"]),
                            MaxRepairPercent = Convert.ToDecimal(reader["MaxRepairPercent"]),
                        });
                    }
                }
            }

            return components;
        }

        private static void UpsertShipTemplateParent(SqliteConnection conn, SqliteTransaction tx, string characterUUID, ShipTemplate entity)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT OR REPLACE INTO ShipTemplates (UUID, Name, OwnerUUID, HullBlueprintUUID)
                                   VALUES (@uuid, @name, @owner, @hullBp)";
                cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                cmd.Parameters.AddWithValue("@owner", characterUUID);
                cmd.Parameters.AddWithValue("@hullBp", entity.HullBlueprintUUID ?? string.Empty);
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void DeleteShipTemplateChildren(SqliteConnection conn, SqliteTransaction tx, string templateUUID)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM ShipTemplateComponents WHERE ShipTemplateUUID = @uuid";
                cmd.Parameters.AddWithValue("@uuid", templateUUID);
                ExecuteNonQueryLogged(cmd);
            }
        }

        private static void InsertShipTemplateComponents(SqliteConnection conn, SqliteTransaction tx, ShipTemplate entity)
        {
            if (entity.Components == null)
            {
                return;
            }

            for (int i = 0; i < entity.Components.Count; i++)
            {
                var comp = entity.Components[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO ShipTemplateComponents (ShipTemplateUUID, Sequence, SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent)
                                       VALUES (@tUUID, @seq, @slotType, @slotIdx, @bpUUID, @curHp, @maxHp, @maxRepair)";
                    cmd.Parameters.AddWithValue("@tUUID", entity.UUID);
                    cmd.Parameters.AddWithValue("@seq", i);
                    cmd.Parameters.AddWithValue("@slotType", comp.SlotType ?? string.Empty);
                    cmd.Parameters.AddWithValue("@slotIdx", comp.SlotIndex);
                    cmd.Parameters.AddWithValue("@bpUUID", comp.BlueprintUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@curHp", comp.CurrentHP);
                    cmd.Parameters.AddWithValue("@maxHp", comp.MaxHP);
                    cmd.Parameters.AddWithValue("@maxRepair", (double)comp.MaxRepairPercent);
                    ExecuteNonQueryLogged(cmd);
                }
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // MarketListing Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static MarketListing ReadMarketListing(SqliteDataReader reader)
        {
            var listing = new MarketListing
            {
                UUID = reader["UUID"] as string,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                StationUUID = reader["StationUUID"] as string ?? string.Empty,
                ItemReferenceID = reader["ItemReferenceID"] as string ?? string.Empty,
                ItemName = reader["ItemName"] as string ?? string.Empty,
                Quantity = Convert.ToInt32(reader["Quantity"]),
                PricePerUnit = Convert.ToDecimal(reader["PricePerUnit"]),
                CurrentHP = Convert.ToInt32(reader["CurrentHP"]),
                MaxHP = Convert.ToInt32(reader["MaxHP"]),
                MaxRepairPercent = Convert.ToDecimal(reader["MaxRepairPercent"]),
                BuyOrder = Convert.ToInt32(reader["BuyOrder"]) != 0,
                BaseItemTypeID = reader["BaseItemTypeID"] as string ?? string.Empty,
                ResourcePurity = reader["ResourcePurity"] as string ?? string.Empty,
                GameTypeCode = reader["GameTypeCode"] as string ?? string.Empty,
                GameSubTypeId = reader["GameSubTypeId"] as string ?? string.Empty,
                LocationName = reader["LocationName"] as string ?? string.Empty,
                SystemName = reader["SystemName"] as string ?? string.Empty,
                SellerName = reader["SellerName"] as string ?? string.Empty,
                SellerFactionTag = reader["SellerFactionTag"] as string ?? string.Empty,
                PrivateSale = Convert.ToInt32(reader["PrivateSale"]) != 0,
                BuyerName = reader["BuyerName"] as string ?? string.Empty,
                BuyerFactionTag = reader["BuyerFactionTag"] as string ?? string.Empty,
                IsOutbid = Convert.ToInt32(reader["IsOutbid"]) != 0,
                IsUndercut = Convert.ToInt32(reader["IsUndercut"]) != 0,
                PlacedDT = reader["PlacedDT"] as string ?? string.Empty,
                ExpiresDT = reader["ExpiresDT"] as string ?? string.Empty,
                SyncedByCharacterUUID = reader["SyncedByCharacterUUID"] as string ?? string.Empty,
                SyncTimestamp = reader["SyncTimestamp"] as string ?? string.Empty,
            };

            var itemTypeStr = reader["ItemType"] as string;
            if (Enum.TryParse<ItemType.ItemTypeEnum>(itemTypeStr, true, out var it))
            {
                listing.ItemType = it;
            }

            if (reader["MarketId"] != DBNull.Value)
            {
                listing.MarketId = Convert.ToInt64(reader["MarketId"]);
            }

            if (reader["GameTypeId"] != DBNull.Value)
            {
                listing.GameTypeId = Convert.ToInt64(reader["GameTypeId"]);
            }

            if (reader["SystemId"] != DBNull.Value)
            {
                listing.SystemId = Convert.ToInt32(reader["SystemId"]);
            }

            if (reader["GameLocationId"] != DBNull.Value)
            {
                listing.GameLocationId = Convert.ToInt32(reader["GameLocationId"]);
            }

            if (reader["AmountRemaining"] != DBNull.Value)
            {
                listing.AmountRemaining = Convert.ToInt32(reader["AmountRemaining"]);
            }

            if (reader["AmountOriginal"] != DBNull.Value)
            {
                listing.AmountOriginal = Convert.ToInt32(reader["AmountOriginal"]);
            }

            if (reader["AmountSold"] != DBNull.Value)
            {
                listing.AmountSold = Convert.ToInt32(reader["AmountSold"]);
            }

            if (reader["EscrowRemaining"] != DBNull.Value)
            {
                listing.EscrowRemaining = Convert.ToDecimal(reader["EscrowRemaining"]);
            }

            if (reader["SalesTaxEstimate"] != DBNull.Value)
            {
                listing.SalesTaxEstimate = Convert.ToDecimal(reader["SalesTaxEstimate"]);
            }

            if (reader["ValueRemaining"] != DBNull.Value)
            {
                listing.ValueRemaining = Convert.ToDecimal(reader["ValueRemaining"]);
            }

            if (reader["Evolution"] != DBNull.Value)
            {
                listing.Evolution = Convert.ToInt32(reader["Evolution"]);
            }

            if (reader["HealthPercentage"] != DBNull.Value)
            {
                listing.HealthPercentage = Convert.ToDouble(reader["HealthPercentage"]);
            }

            if (reader["CompetitorForMarketId"] != DBNull.Value)
            {
                listing.CompetitorForMarketId = Convert.ToInt64(reader["CompetitorForMarketId"]);
            }

            return listing;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // MarketTransaction Helpers
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static MarketTransaction ReadMarketTransaction(SqliteDataReader reader)
        {
            var tx = new MarketTransaction
            {
                UUID = reader["UUID"] as string,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                ItemReferenceID = reader["ItemReferenceID"] as string ?? string.Empty,
                ItemName = reader["ItemName"] as string ?? string.Empty,
                Quantity = Convert.ToInt32(reader["Quantity"]),
                PricePerUnit = decimal.Parse(reader["PricePerUnit"].ToString(), CultureInfo.InvariantCulture),
                TotalPrice = decimal.Parse(reader["TotalPrice"].ToString(), CultureInfo.InvariantCulture),
                Counterparty = reader["Counterparty"] as string ?? string.Empty,
                CounterpartyFaction = reader["CounterpartyFaction"] as string ?? string.Empty,
                StationUUID = reader["StationUUID"] as string ?? string.Empty,
                Timestamp = reader["Timestamp"] as string ?? string.Empty,
                Notes = reader["Notes"] as string ?? string.Empty,
                ListingUUID = reader["ListingUUID"] as string ?? string.Empty,
                CurrentHP = Convert.ToInt32(reader["CurrentHP"]),
                MaxHP = Convert.ToInt32(reader["MaxHP"]),
                MaxRepairPercent = decimal.Parse(reader["MaxRepairPercent"].ToString(), CultureInfo.InvariantCulture),
            };

            var txTypeStr = reader["TransactionType"] as string;
            if (Enum.TryParse<TransactionType>(txTypeStr, true, out var tt))
            {
                tx.TransactionType = tt;
            }

            var itemTypeStr = reader["ItemType"] as string;
            if (Enum.TryParse<ItemType.ItemTypeEnum>(itemTypeStr, true, out var it))
            {
                tx.ItemType = it;
            }

            return tx;
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
                using (var reader = ExecuteReaderLogged(cmd))
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
                var result = ExecuteScalarLogged(cmd);
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
                ExecuteNonQueryLogged(cmd);
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Task 6.7 Helpers â€” WarehouseOverflowRule, Mail, Banking, Intel
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private static WarehouseOverflowRule ReadWarehouseOverflowRule(SqliteDataReader reader)
        {
            return new WarehouseOverflowRule
            {
                UUID = reader["UUID"] as string,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                ColonyUUID = reader["ColonyUUID"] as string ?? string.Empty,
                ResourceName = reader["ResourceName"] as string ?? string.Empty,
                RuleType = (OverflowRuleType)Convert.ToInt32(reader["RuleType"]),
                TriggerThreshold = Convert.ToDecimal(reader["Threshold"]),
                DestinationUUID = reader["DestinationColonyUUID"] as string ?? string.Empty,
            };
        }

        private static MailMessage ReadMailMessage(SqliteDataReader reader)
        {
            return new MailMessage
            {
                UUID = (reader["OwnerUUID"] as string ?? string.Empty) + "_" + Convert.ToInt32(reader["MailId"]).ToString(),
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                MailId = Convert.ToInt32(reader["MailId"]),
                CharacterIdFrom = Convert.ToInt32(reader["CharacterIdFrom"]),
                FromName = reader["FromName"] as string ?? string.Empty,
                CharacterIdTo = Convert.ToInt32(reader["CharacterIdTo"]),
                ToName = reader["ToName"] as string ?? string.Empty,
                SentTime = reader["SentTime"] as string ?? string.Empty,
                Subject = reader["Subject"] as string ?? string.Empty,
                MailRead = Convert.ToInt32(reader["MailRead"]) != 0,
                MailType = reader["MailType"] as string,
                MailContent = reader["MailContent"] as string ?? string.Empty,
                LocalRead = Convert.ToInt32(reader["LocalRead"]) != 0,
            };
        }

        private static BankingTransaction ReadBankingTransaction(SqliteDataReader reader)
        {
            var tx = new BankingTransaction
            {
                UUID = reader["UUID"] as string ?? string.Empty,
                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                TransactionDateTime = reader["TransactionDateTime"] as string ?? string.Empty,
                CreditChange = decimal.Parse(reader["CreditChange"].ToString(), CultureInfo.InvariantCulture),
                OldBalance = decimal.Parse(reader["OldBalance"].ToString(), CultureInfo.InvariantCulture),
                NewBalance = decimal.Parse(reader["NewBalance"].ToString(), CultureInfo.InvariantCulture),
                TransactionType = Convert.ToInt32(reader["TransactionType"]),
                Detail = reader["Detail"] as string ?? string.Empty,
                IsManualEntry = Convert.ToInt32(reader["IsManualEntry"]) != 0,
            };

            if (reader["CharacterId"] != DBNull.Value)
            {
                tx.CharacterId = Convert.ToInt32(reader["CharacterId"]);
            }

            if (reader["SystemObjectId"] != DBNull.Value)
            {
                tx.SystemObjectId = Convert.ToInt32(reader["SystemObjectId"]);
            }

            if (reader["SystemId"] != DBNull.Value)
            {
                tx.SystemId = Convert.ToInt32(reader["SystemId"]);
            }

            return tx;
        }

        private static IntelComment ReadIntelComment(SqliteDataReader reader)
        {
            return new IntelComment
            {
                UUID = reader["UUID"] as string ?? string.Empty,
                TargetCharacterUUID = reader["TargetCharacterUUID"] as string ?? string.Empty,
                SubmitterCharacterUUID = reader["SubmitterCharacterUUID"] as string ?? string.Empty,
                Text = reader["Text"] as string ?? string.Empty,
                CreatedUtc = DateTime.Parse(reader["CreatedUtc"] as string ?? DateTime.MinValue.ToString("O")),
            };
        }

        private static IntelCommentFactionShare ReadIntelShare(SqliteDataReader reader)
        {
            var share = new IntelCommentFactionShare
            {
                UUID = reader["UUID"] as string ?? string.Empty,
                IntelCommentUUID = reader["IntelCommentUUID"] as string ?? string.Empty,
                FactionUUID = reader["FactionUUID"] as string ?? string.Empty,
                ClassificationLevelUUID = reader["ClassificationLevelUUID"] as string,
                ClassifiedByCharacterUUID = reader["ClassifiedByCharacterUUID"] as string,
                SharedUtc = DateTime.Parse(reader["SharedUtc"] as string ?? DateTime.MinValue.ToString("O")),
            };

            var classifiedUtcStr = reader["ClassifiedUtc"] as string;
            if (classifiedUtcStr != null)
            {
                share.ClassifiedUtc = DateTime.Parse(classifiedUtcStr);
            }

            return share;
        }

        private static string[] LoadBlueprintTypeProperties(SqliteConnection conn, string blueprintTypeName)
        {
            var props = new List<string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Key FROM BlueprintTypeProperties WHERE BlueprintTypeName = @name";
                cmd.Parameters.AddWithValue("@name", blueprintTypeName);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        props.Add(reader.GetString(0));
                    }
                }
            }

            return props.ToArray();
        }

        private static string[] LoadBlueprintTypeResearchableProperties(SqliteConnection conn, string blueprintTypeName)
        {
            var props = new List<string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Key FROM BlueprintTypeResearchableProperties WHERE BlueprintTypeName = @name";
                cmd.Parameters.AddWithValue("@name", blueprintTypeName);
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        props.Add(reader.GetString(0));
                    }
                }
            }

            return props.ToArray();
        }

        /// <summary>
        /// Migrates all data from the legacy JSON-blob EntityData table into the
        /// new normalized relational tables. Each entity is deserialized from its
        /// JSON blob and inserted via the existing Upsert methods. Individual
        /// failures are logged and skipped so migration can continue with partial
        /// data. The old EntityData table is dropped after migration completes.
        /// </summary>
        /// <param name="conn">An open SQLite connection.</param>
        private void MigrateLegacyData(SqliteConnection conn)
        {
            Log.Info("Legacy JSON-blob schema detected. Migrating to normalized schema...");

            var entities = new List<LegacyEntity>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT EntityType, EntityId, CharacterUUID, JsonData FROM EntityData";
                using (var reader = ExecuteReaderLogged(cmd))
                {
                    while (reader.Read())
                    {
                        entities.Add(new LegacyEntity
                        {
                            EntityType = reader.GetString(0),
                            EntityId = reader.GetString(1),
                            CharacterUUID = reader.IsDBNull(2) ? null : reader.GetString(2),
                            JsonData = reader.GetString(3),
                        });
                    }
                }
            }

            int migrated = 0;
            foreach (var entity in entities)
            {
                try
                {
                    MigrateSingleEntity(entity);
                    migrated++;
                }
                catch (Exception ex)
                {
                    Log.Warn(
                        ex,
                        "Failed to migrate legacy entity {0}/{1}, skipping",
                        entity.EntityType,
                        entity.EntityId);
                }
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DROP TABLE IF EXISTS EntityData";
                ExecuteNonQueryLogged(cmd);
            }

            Log.Info("Legacy migration complete. Migrated {0} of {1} entities.", migrated, entities.Count);
        }

        /// <summary>
        /// Deserializes a single legacy entity from its JSON blob and inserts it
        /// into the appropriate normalized table via the existing Upsert methods.
        /// </summary>
        /// <param name="entity">The legacy entity row data.</param>
        private void MigrateSingleEntity(LegacyEntity entity)
        {
            string charUUID = entity.CharacterUUID ?? string.Empty;
            string json = entity.JsonData;

            switch (entity.EntityType)
            {
                case "Colony":
                    var colony = JsonConvert.DeserializeObject<Colony>(json, JsonSettings.SerializerSettings);
                    if (colony != null)
                    {
                        UpsertColonyAsync(charUUID.Length > 0 ? charUUID : colony.OwnerUUID, colony).GetAwaiter().GetResult();
                    }

                    break;

                case "Blueprint":
                    var bp = JsonConvert.DeserializeObject<Blueprint>(json, JsonSettings.SerializerSettings);
                    if (bp != null)
                    {
                        UpsertBlueprintAsync(charUUID.Length > 0 ? charUUID : bp.OwnerUUID, bp).GetAwaiter().GetResult();
                    }

                    break;

                case "Survey":
                    var survey = JsonConvert.DeserializeObject<Survey>(json, JsonSettings.SerializerSettings);
                    if (survey != null)
                    {
                        UpsertSurveyAsync(charUUID.Length > 0 ? charUUID : survey.OwnerUUID, survey).GetAwaiter().GetResult();
                    }

                    break;

                case "PlayerProfile":
                    var profile = JsonConvert.DeserializeObject<PlayerProfile>(json, JsonSettings.SerializerSettings);
                    if (profile != null)
                    {
                        UpsertPlayerProfileAsync(charUUID.Length > 0 ? charUUID : profile.UUID, profile).GetAwaiter().GetResult();
                    }

                    break;

                case "DeliveryRoute":
                    var route = JsonConvert.DeserializeObject<DeliveryRoute>(json, JsonSettings.SerializerSettings);
                    if (route != null)
                    {
                        UpsertDeliveryRouteAsync(charUUID.Length > 0 ? charUUID : route.OwnerUUID, route).GetAwaiter().GetResult();
                    }

                    break;

                case "DeliveryPlan":
                    var plan = JsonConvert.DeserializeObject<DeliveryPlan>(json, JsonSettings.SerializerSettings);
                    if (plan != null)
                    {
                        UpsertDeliveryPlanAsync(charUUID.Length > 0 ? charUUID : plan.OwnerUUID, plan).GetAwaiter().GetResult();
                    }

                    break;

                case "Ship":
                    var ship = JsonConvert.DeserializeObject<Ship>(json, JsonSettings.SerializerSettings);
                    if (ship != null)
                    {
                        UpsertShipAsync(charUUID.Length > 0 ? charUUID : ship.OwnerUUID, ship).GetAwaiter().GetResult();
                    }

                    break;

                case "ShipTemplate":
                    var template = JsonConvert.DeserializeObject<ShipTemplate>(json, JsonSettings.SerializerSettings);
                    if (template != null)
                    {
                        UpsertShipTemplateAsync(charUUID.Length > 0 ? charUUID : template.OwnerUUID, template).GetAwaiter().GetResult();
                    }

                    break;

                case "MarketListing":
                    var listing = JsonConvert.DeserializeObject<MarketListing>(json, JsonSettings.SerializerSettings);
                    if (listing != null)
                    {
                        UpsertMarketListingAsync(charUUID.Length > 0 ? charUUID : listing.OwnerUUID, listing).GetAwaiter().GetResult();
                    }

                    break;

                case "MarketTransaction":
                    var mtx = JsonConvert.DeserializeObject<MarketTransaction>(json, JsonSettings.SerializerSettings);
                    if (mtx != null)
                    {
                        UpsertMarketTransactionAsync(charUUID.Length > 0 ? charUUID : mtx.OwnerUUID, mtx).GetAwaiter().GetResult();
                    }

                    break;

                case "PricingPlan":
                    var pricing = JsonConvert.DeserializeObject<PricingPlan>(json, JsonSettings.SerializerSettings);
                    if (pricing != null)
                    {
                        UpsertPricingPlanAsync(charUUID.Length > 0 ? charUUID : pricing.OwnerUUID, pricing).GetAwaiter().GetResult();
                    }

                    break;

                case "StockPlan":
                    var stockPlan = JsonConvert.DeserializeObject<StockPlan>(json, JsonSettings.SerializerSettings);
                    if (stockPlan != null)
                    {
                        UpsertStockPlanAsync(charUUID.Length > 0 ? charUUID : stockPlan.OwnerUUID, stockPlan).GetAwaiter().GetResult();
                    }

                    break;

                case "StockProfile":
                    var stockProfile = JsonConvert.DeserializeObject<StockProfile>(json, JsonSettings.SerializerSettings);
                    if (stockProfile != null)
                    {
                        UpsertStockProfileAsync(charUUID.Length > 0 ? charUUID : stockProfile.OwnerUUID, stockProfile).GetAwaiter().GetResult();
                    }

                    break;

                case "BuildPlan":
                    var buildPlan = JsonConvert.DeserializeObject<BuildPlan>(json, JsonSettings.SerializerSettings);
                    if (buildPlan != null)
                    {
                        UpsertBuildPlanAsync(charUUID.Length > 0 ? charUUID : buildPlan.OwnerUUID, buildPlan).GetAwaiter().GetResult();
                    }

                    break;

                case "SupplyChain":
                    var supplyChain = JsonConvert.DeserializeObject<SupplyChain>(json, JsonSettings.SerializerSettings);
                    if (supplyChain != null)
                    {
                        UpsertSupplyChainAsync(charUUID.Length > 0 ? charUUID : supplyChain.OwnerUUID, supplyChain).GetAwaiter().GetResult();
                    }

                    break;

                case "Asteroid":
                    var asteroid = JsonConvert.DeserializeObject<Asteroid>(json, JsonSettings.SerializerSettings);
                    if (asteroid != null)
                    {
                        UpsertAsteroidAsync(charUUID.Length > 0 ? charUUID : asteroid.OwnerUUID, asteroid).GetAwaiter().GetResult();
                    }

                    break;

                case "Station":
                    var station = JsonConvert.DeserializeObject<Station>(json, JsonSettings.SerializerSettings);
                    if (station != null)
                    {
                        UpsertStationAsync(charUUID.Length > 0 ? charUUID : station.OwnerUUID, station).GetAwaiter().GetResult();
                    }

                    break;

                case "Faction":
                    var faction = JsonConvert.DeserializeObject<Faction>(json, JsonSettings.SerializerSettings);
                    if (faction != null)
                    {
                        UpsertFactionForCharacterAsync(charUUID, faction).GetAwaiter().GetResult();
                    }

                    break;

                case "ExternalCharacter":
                    var extChar = JsonConvert.DeserializeObject<ExternalCharacter>(json, JsonSettings.SerializerSettings);
                    if (extChar != null)
                    {
                        UpsertExternalCharacterAsync(charUUID, extChar).GetAwaiter().GetResult();
                    }

                    break;

                case "WarehouseOverflowRule":
                    var overflow = JsonConvert.DeserializeObject<WarehouseOverflowRule>(json, JsonSettings.SerializerSettings);
                    if (overflow != null)
                    {
                        UpsertWarehouseOverflowRuleAsync(charUUID, overflow).GetAwaiter().GetResult();
                    }

                    break;

                case "MailMessage":
                    var mail = JsonConvert.DeserializeObject<MailMessage>(json, JsonSettings.SerializerSettings);
                    if (mail != null)
                    {
                        UpsertMailMessageAsync(charUUID, mail).GetAwaiter().GetResult();
                    }

                    break;

                case "BankingTransaction":
                    var bankTx = JsonConvert.DeserializeObject<BankingTransaction>(json, JsonSettings.SerializerSettings);
                    if (bankTx != null)
                    {
                        UpsertBankingTransactionAsync(charUUID, bankTx).GetAwaiter().GetResult();
                    }

                    break;

                default:
                    Log.Debug("Unknown legacy entity type: {0}", entity.EntityType);
                    break;
            }
        }


        // ===============================================================
        // SQL Logging Helpers
        // ===============================================================

        /// <summary>
        /// Executes a non-query command with logging of the SQL text, parameters, and duration.
        /// </summary>
        private static int ExecuteNonQueryLogged(SqliteCommand cmd, [CallerMemberName] string caller = "")
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int affected = cmd.ExecuteNonQuery();
            sw.Stop();
            if (sw.ElapsedMilliseconds > 0 || Log.IsTraceEnabled)
            {
                Log.Debug(
                    "SQL|{0}|{1}ms|rows={2}|{3}|{4}",
                    caller,
                    sw.ElapsedMilliseconds,
                    affected,
                    cmd.CommandText.Replace("\n", " ").Replace("\r", string.Empty).Substring(0, Math.Min(cmd.CommandText.Length, 200)),
                    FormatParameters(cmd.Parameters));
            }

            return affected;
        }

        /// <summary>
        /// Executes a reader command with logging of the SQL text, parameters, and duration.
        /// </summary>
        private static SqliteDataReader ExecuteReaderLogged(SqliteCommand cmd, [CallerMemberName] string caller = "")
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var reader = cmd.ExecuteReader();
            sw.Stop();
            if (sw.ElapsedMilliseconds > 0 || Log.IsTraceEnabled)
            {
                Log.Debug(
                    "SQL|{0}|{1}ms|{2}|{3}",
                    caller,
                    sw.ElapsedMilliseconds,
                    cmd.CommandText.Replace("\n", " ").Replace("\r", string.Empty).Substring(0, Math.Min(cmd.CommandText.Length, 200)),
                    FormatParameters(cmd.Parameters));
            }

            return reader;
        }

        /// <summary>
        /// Executes a scalar command with logging.
        /// </summary>
        private static object ExecuteScalarLogged(SqliteCommand cmd, [CallerMemberName] string caller = "")
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = cmd.ExecuteScalar();
            sw.Stop();
            if (sw.ElapsedMilliseconds > 0 || Log.IsTraceEnabled)
            {
                Log.Debug(
                    "SQL|{0}|{1}ms|{2}|{3}",
                    caller,
                    sw.ElapsedMilliseconds,
                    cmd.CommandText.Replace("\n", " ").Replace("\r", string.Empty).Substring(0, Math.Min(cmd.CommandText.Length, 200)),
                    FormatParameters(cmd.Parameters));
            }

            return result;
        }

        private static string FormatParameters(SqliteParameterCollection parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return string.Empty;
            }

            var sb = new System.Text.StringBuilder();
            foreach (SqliteParameter p in parameters)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                string val = p.Value == null || p.Value == DBNull.Value ? "NULL" : p.Value.ToString();
                if (val.Length > 50)
                {
                    val = val.Substring(0, 50) + "...";
                }

                sb.AppendFormat("{0}={1}", p.ParameterName, val);
            }

            return sb.ToString();
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

        /// <summary>
        /// Represents a single row from the legacy EntityData table.
        /// </summary>
        private sealed class LegacyEntity
        {
            /// <summary>Gets or sets the entity type discriminator (e.g. "Colony", "Blueprint").</summary>
            public string EntityType { get; set; }

            /// <summary>Gets or sets the entity ID (typically a UUID).</summary>
            public string EntityId { get; set; }

            /// <summary>Gets or sets the owning character UUID (may be null).</summary>
            public string CharacterUUID { get; set; }

            /// <summary>Gets or sets the serialized JSON blob.</summary>
            public string JsonData { get; set; }
        }
    }
}
