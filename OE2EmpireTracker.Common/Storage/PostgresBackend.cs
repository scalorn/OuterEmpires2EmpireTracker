// -----------------------------------------------------------------------
// <copyright file="PostgresBackend.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Npgsql;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using Polly;
using Polly.Retry;

namespace OE2EmpireTracker.Common.Storage
{
    /// <summary>
    /// IStorageBackend implementation that persists all data in a PostgreSQL
    /// database using Npgsql with Polly retry policies for transient fault handling.
    /// </summary>
    internal partial class PostgresBackend : IStorageBackend
    {
        private const int CurrentSchemaVersion = 1;
        private const int MaxRetries = 3;

        private const string PlayerEntitySchemaA = @"
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
    Distance DOUBLE PRECISION NOT NULL DEFAULT 0,
    SurfaceVariation INTEGER NOT NULL DEFAULT 0,
    AtmosVariation INTEGER NOT NULL DEFAULT 0,
    HexValue TEXT NOT NULL DEFAULT '',
    SystemObjectTypeName TEXT NOT NULL DEFAULT '',
    ImagePreFix TEXT NOT NULL DEFAULT '',
    ManufacturingBlocked BOOLEAN NOT NULL DEFAULT FALSE,
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
    MiningLeftOvers DOUBLE PRECISION NOT NULL DEFAULT 0,
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
    DurabilityCurrent DOUBLE PRECISION NOT NULL DEFAULT 0,
    DurabilityMax DOUBLE PRECISION NOT NULL DEFAULT 0,
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
    Volume DOUBLE PRECISION NOT NULL DEFAULT 0,
    CurrentHP INTEGER NOT NULL DEFAULT 0,
    MaxHP INTEGER NOT NULL DEFAULT 0,
    MaxRepairPercent DOUBLE PRECISION NOT NULL DEFAULT 0,
    Mass DOUBLE PRECISION,
    GameItemId INTEGER,
    JobRef INTEGER,
    JobDeliveryLoc INTEGER,
    HealthPercentage DOUBLE PRECISION,
    LastRepairHealthPercentage DOUBLE PRECISION,
    Evolution INTEGER,
    ShipPartType TEXT NOT NULL DEFAULT '',
    JobName TEXT NOT NULL DEFAULT '',
    JobTrack TEXT NOT NULL DEFAULT ''
);

CREATE INDEX IF NOT EXISTS IX_Items_Parent ON Items (ParentUUID, ParentType);

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
    Volume DOUBLE PRECISION NOT NULL DEFAULT 0,
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

CREATE TABLE IF NOT EXISTS Surveys (
    UUID TEXT PRIMARY KEY,
    OwnerUUID TEXT NOT NULL DEFAULT '',
    ItemType TEXT NOT NULL,
    BaseItemTypeID TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL DEFAULT '',
    NickName TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT '',
    Quantity INTEGER NOT NULL DEFAULT 0,
    Volume DOUBLE PRECISION NOT NULL DEFAULT 0,
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

CREATE TABLE IF NOT EXISTS PlayerProfiles (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    Faction TEXT NOT NULL DEFAULT '',
    FactionUUID TEXT NOT NULL DEFAULT '',
    TotalCredits NUMERIC NOT NULL DEFAULT 0,
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
    IsUnlocked BOOLEAN NOT NULL DEFAULT FALSE,
    TargetLevel INTEGER NOT NULL DEFAULT 0,
    TrainingPercentageComplete INTEGER NOT NULL DEFAULT 0,
    RemainingMinutes INTEGER NOT NULL DEFAULT 0,
    Completion_StartTime TEXT,
    Completion_RepeatIntervalSeconds INTEGER,
    Completion_IsRepeating INTEGER,
    PRIMARY KEY (PlayerUUID, SkillName)
);
";

        private const string PlayerEntitySchemaB = @"
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
    FuelEstimate DOUBLE PRECISION NOT NULL DEFAULT 0,
    PRIMARY KEY (DeliveryRouteUUID, Sequence)
);

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
    HullMaxRepairPercent DOUBLE PRECISION NOT NULL DEFAULT 0
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
    MaxRepairPercent DOUBLE PRECISION NOT NULL DEFAULT 0,
    PRIMARY KEY (ShipTemplateUUID, Sequence)
);

CREATE TABLE IF NOT EXISTS DeliveryPlans (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    OwnerUUID TEXT NOT NULL DEFAULT '',
    RouteUUID TEXT NOT NULL DEFAULT '',
    ShipUUID TEXT NOT NULL DEFAULT '',
    Completed BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS DeliveryPlanStops (
    DeliveryPlanUUID TEXT NOT NULL REFERENCES DeliveryPlans(UUID) ON DELETE CASCADE,
    Sequence INTEGER NOT NULL,
    ColonyUUID TEXT NOT NULL DEFAULT '',
    StopCompleted BOOLEAN NOT NULL DEFAULT FALSE,
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

CREATE TABLE IF NOT EXISTS MarketListings (
    UUID TEXT PRIMARY KEY,
    OwnerUUID TEXT NOT NULL DEFAULT '',
    StationUUID TEXT NOT NULL DEFAULT '',
    ItemType TEXT NOT NULL DEFAULT 'None',
    ItemReferenceID TEXT NOT NULL DEFAULT '',
    ItemName TEXT NOT NULL DEFAULT '',
    Quantity INTEGER NOT NULL DEFAULT 0,
    PricePerUnit NUMERIC NOT NULL DEFAULT 0,
    CurrentHP INTEGER NOT NULL DEFAULT 0,
    MaxHP INTEGER NOT NULL DEFAULT 0,
    MaxRepairPercent DOUBLE PRECISION NOT NULL DEFAULT 0,
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
    EscrowRemaining NUMERIC,
    SalesTaxEstimate NUMERIC,
    ValueRemaining NUMERIC,
    Evolution INTEGER,
    HealthPercentage DOUBLE PRECISION,
    SellerName TEXT NOT NULL DEFAULT '',
    SellerFactionTag TEXT NOT NULL DEFAULT '',
    PrivateSale BOOLEAN NOT NULL DEFAULT FALSE,
    BuyerName TEXT NOT NULL DEFAULT '',
    BuyerFactionTag TEXT NOT NULL DEFAULT '',
    IsOutbid BOOLEAN NOT NULL DEFAULT FALSE,
    IsUndercut BOOLEAN NOT NULL DEFAULT FALSE,
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
    PricePerUnit NUMERIC NOT NULL DEFAULT 0,
    TotalPrice NUMERIC NOT NULL DEFAULT 0,
    Counterparty TEXT NOT NULL DEFAULT '',
    CounterpartyFaction TEXT NOT NULL DEFAULT '',
    StationUUID TEXT NOT NULL DEFAULT '',
    Timestamp TEXT NOT NULL DEFAULT '',
    Notes TEXT NOT NULL DEFAULT '',
    ListingUUID TEXT NOT NULL DEFAULT '',
    CurrentHP INTEGER NOT NULL DEFAULT 0,
    MaxHP INTEGER NOT NULL DEFAULT 0,
    MaxRepairPercent DOUBLE PRECISION NOT NULL DEFAULT 0
);
";

        private const string PlayerEntitySchemaC = @"
CREATE TABLE IF NOT EXISTS PricingPlans (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    OwnerUUID TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT '',
    FixedCostPerItem NUMERIC NOT NULL DEFAULT 0,
    HourlyCostRate NUMERIC NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS PricingPlanPrices (
    PricingPlanUUID TEXT NOT NULL REFERENCES PricingPlans(UUID) ON DELETE CASCADE,
    ResourceName TEXT NOT NULL,
    Price NUMERIC NOT NULL DEFAULT 0,
    PRIMARY KEY (PricingPlanUUID, ResourceName)
);

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

CREATE TABLE IF NOT EXISTS Factions (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    OwnerUUID TEXT NOT NULL DEFAULT '',
    Tag TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS ExternalCharacters (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    OwnerUUID TEXT NOT NULL DEFAULT '',
    FactionUUID TEXT NOT NULL DEFAULT '',
    FactionName TEXT NOT NULL DEFAULT '',
    CharacterId INTEGER NOT NULL DEFAULT 0,
    Notes TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS WarehouseOverflowRules (
    UUID TEXT PRIMARY KEY,
    OwnerUUID TEXT NOT NULL DEFAULT '',
    ColonyUUID TEXT NOT NULL DEFAULT '',
    ResourceName TEXT NOT NULL DEFAULT '',
    RuleType INTEGER NOT NULL DEFAULT 0,
    Threshold INTEGER NOT NULL DEFAULT 0,
    DestinationColonyUUID TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS MailMessages (
    MailId INTEGER NOT NULL,
    OwnerUUID TEXT NOT NULL DEFAULT '',
    CharacterIdFrom INTEGER NOT NULL DEFAULT 0,
    FromName TEXT NOT NULL DEFAULT '',
    CharacterIdTo INTEGER NOT NULL DEFAULT 0,
    ToName TEXT NOT NULL DEFAULT '',
    SentTime TEXT NOT NULL DEFAULT '',
    Subject TEXT NOT NULL DEFAULT '',
    MailRead BOOLEAN NOT NULL DEFAULT FALSE,
    MailType TEXT,
    MailContent TEXT NOT NULL DEFAULT '',
    LocalRead BOOLEAN NOT NULL DEFAULT FALSE,
    PRIMARY KEY (OwnerUUID, MailId)
);

CREATE TABLE IF NOT EXISTS BankingTransactions (
    UUID TEXT PRIMARY KEY,
    OwnerUUID TEXT NOT NULL DEFAULT '',
    TransactionDateTime TEXT NOT NULL DEFAULT '',
    CreditChange NUMERIC NOT NULL DEFAULT 0,
    OldBalance NUMERIC NOT NULL DEFAULT 0,
    NewBalance NUMERIC NOT NULL DEFAULT 0,
    TransactionType INTEGER NOT NULL DEFAULT 0,
    Detail TEXT NOT NULL DEFAULT '',
    CharacterId INTEGER,
    SystemObjectId INTEGER,
    SystemId INTEGER,
    IsManualEntry BOOLEAN NOT NULL DEFAULT FALSE
);
";

        private const string ServerPermissionBaselineSchema = @"
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
    IsRevoked BOOLEAN NOT NULL DEFAULT FALSE,
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
    X DOUBLE PRECISION NOT NULL DEFAULT 0,
    Y DOUBLE PRECISION NOT NULL DEFAULT 0,
    Quadrant INTEGER NOT NULL DEFAULT 0,
    Sector INTEGER NOT NULL DEFAULT 0,
    Region INTEGER NOT NULL DEFAULT 0,
    Locality INTEGER NOT NULL DEFAULT 0,
    SpectralClass TEXT NOT NULL DEFAULT '',
    FactionId INTEGER NOT NULL DEFAULT 0,
    FactionName TEXT NOT NULL DEFAULT '',
    FactionColor TEXT NOT NULL DEFAULT '',
    HasOrbital BOOLEAN NOT NULL DEFAULT FALSE,
    HasSpaceport BOOLEAN NOT NULL DEFAULT FALSE,
    HasStarbase BOOLEAN NOT NULL DEFAULT FALSE
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
    ServerProcessing BOOLEAN NOT NULL DEFAULT FALSE
);

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
    BaseVolume DOUBLE PRECISION NOT NULL DEFAULT 0,
    BaseMass DOUBLE PRECISION NOT NULL DEFAULT 0
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
    BaseVolume DOUBLE PRECISION NOT NULL DEFAULT 0,
    BaseMass DOUBLE PRECISION NOT NULL DEFAULT 0,
    BaseValue DOUBLE PRECISION NOT NULL DEFAULT 0
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
        /// Schema DDL concatenated from tasks 7.2-7.3. ExecuteSchema runs this
        /// against a fresh database.
        /// </summary>
        private static readonly string SchemaDdl = PlayerEntitySchemaA + PlayerEntitySchemaB + PlayerEntitySchemaC + ServerPermissionBaselineSchema;

        private readonly string _connectionString;
        private readonly RetryPolicy _retryPolicy;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresBackend"/> class.
        /// </summary>
        /// <param name="config">Configuration containing the Postgres connection string.</param>
        public PostgresBackend(StorageBackendConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _connectionString = config.ConnectionString ?? string.Empty;

            _retryPolicy = Policy
                .Handle<NpgsqlException>()
                .Or<TimeoutException>()
                .WaitAndRetry(
                    MaxRetries,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        Log.Warn(
                            "Postgres operation failed (attempt {0}/{1}), retrying in {2}s: {3}",
                            retryCount,
                            MaxRetries,
                            timeSpan.TotalSeconds,
                            exception.Message);
                    });
        }

        // ═══════════════════════════════════════════════════════════
        // Lifecycle
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task InitializeAsync(CancellationToken ct = default)
        {
            _retryPolicy.Execute(() =>
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
                        Log.Info("Postgres database initialized with schema version {0}", CurrentSchemaVersion);
                    }
                    else
                    {
                        Log.Debug("Postgres database already at schema version {0}", version);
                    }
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> ValidateConnectionAsync(CancellationToken ct = default)
        {
            try
            {
                _retryPolicy.Execute(() =>
                {
                    using (var conn = OpenConnection())
                    {
                    }
                });

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Postgres connection validation failed");
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc/>
        public StorageInfo GetStorageInfo()
        {
            return new StorageInfo
            {
                BackendType = "Postgres",
                Location = _connectionString,
            };
        }

        // ═══════════════════════════════════════════════════════════
        // Server Factions
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<ServerFaction> GetFactionAsync(string uuid)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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
                            return faction;
                        }
                    }
                }

                return (ServerFaction)null;
            }));
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ServerFaction>> GetAllFactionsAsync()
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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

                return (IReadOnlyList<ServerFaction>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertFactionAsync(ServerFaction faction)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = @"INSERT INTO ServerFactions (UUID, Name, Description, Metadata_LastModifiedUtc, Metadata_ModifiedByTokenId)
                                           VALUES (@uuid, @name, @desc, @modUtc, @modBy)
                                           ON CONFLICT (UUID) DO UPDATE SET Name = EXCLUDED.Name, Description = EXCLUDED.Description, Metadata_LastModifiedUtc = EXCLUDED.Metadata_LastModifiedUtc, Metadata_ModifiedByTokenId = EXCLUDED.Metadata_ModifiedByTokenId";
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
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteFactionAsync(string uuid)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM ServerFactions WHERE UUID = @uuid";
                    cmd.Parameters.AddWithValue("@uuid", uuid);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Server Characters
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<ServerCharacter> GetCharacterAsync(string uuid)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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
                            return ReadServerCharacter(reader);
                        }
                    }
                }

                return (ServerCharacter)null;
            }));
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ServerCharacter>> GetAllCharactersAsync()
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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

                return (IReadOnlyList<ServerCharacter>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertCharacterAsync(ServerCharacter character)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO ServerCharacters (UUID, Name, FactionUUID, Metadata_LastModifiedUtc, Metadata_ModifiedByTokenId)
                                       VALUES (@uuid, @name, @factionUUID, @modUtc, @modBy)
                                       ON CONFLICT (UUID) DO UPDATE SET Name = EXCLUDED.Name, FactionUUID = EXCLUDED.FactionUUID, Metadata_LastModifiedUtc = EXCLUDED.Metadata_LastModifiedUtc, Metadata_ModifiedByTokenId = EXCLUDED.Metadata_ModifiedByTokenId";
                    cmd.Parameters.AddWithValue("@uuid", character.UUID);
                    cmd.Parameters.AddWithValue("@name", character.Name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@factionUUID", (object)character.FactionUUID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@modUtc", character.Metadata?.LastModifiedUtc.ToString("O") ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@modBy", (object)character.Metadata?.ModifiedByTokenId ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteCharacterAsync(string uuid)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM ServerCharacters WHERE UUID = @uuid";
                    cmd.Parameters.AddWithValue("@uuid", uuid);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Global Data
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<string> GetGlobalDataAsync(string dataType)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Value FROM _metadata WHERE Key = @key";
                    cmd.Parameters.AddWithValue("@key", "global_" + dataType);
                    var result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                    {
                        return (string)null;
                    }

                    return result.ToString();
                }
            }));
        }

        /// <inheritdoc/>
        public Task UpsertGlobalDataAsync(string dataType, string json)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO _metadata (Key, Value) VALUES (@key, @val)
                                       ON CONFLICT (Key) DO UPDATE SET Value = EXCLUDED.Value";
                    cmd.Parameters.AddWithValue("@key", "global_" + dataType);
                    cmd.Parameters.AddWithValue("@val", json ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Star Systems
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<StarSystem>> GetAllStarSystemsAsync()
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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

                return (IReadOnlyList<StarSystem>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertStarSystemsAsync(IReadOnlyList<StarSystem> systems)
        {
            _retryPolicy.Execute(() =>
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
                            cmd.Parameters.AddWithValue("@hasOrbital", system.HasOrbital);
                            cmd.Parameters.AddWithValue("@hasSpaceport", system.HasSpaceport);
                            cmd.Parameters.AddWithValue("@hasStarbase", system.HasStarbase);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Colony Summaries
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<ColonySummary>> GetColonySummariesForSystemAsync(int systemId)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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

                return (IReadOnlyList<ColonySummary>)results;
            }));
        }

        // ═══════════════════════════════════════════════════════════
        // API Tokens
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<ApiToken> FindTokenByHashAsync(string tokenHash)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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
                            return ReadApiToken(reader);
                        }
                    }
                }

                return (ApiToken)null;
            }));
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ApiToken>> GetAllTokensAsync()
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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

                return (IReadOnlyList<ApiToken>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertTokenAsync(ApiToken token)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO ApiTokens (Id, TokenHash, CharacterUUID, Role, FactionUUID, CreatedUtc, LastUsedUtc, IsRevoked, RateLimits_RequestsPerMinute)
                                       VALUES (@id, @hash, @charUUID, @role, @factionUUID, @created, @lastUsed, @revoked, @rpm)
                                       ON CONFLICT (Id) DO UPDATE SET TokenHash = EXCLUDED.TokenHash, CharacterUUID = EXCLUDED.CharacterUUID, Role = EXCLUDED.Role, FactionUUID = EXCLUDED.FactionUUID, CreatedUtc = EXCLUDED.CreatedUtc, LastUsedUtc = EXCLUDED.LastUsedUtc, IsRevoked = EXCLUDED.IsRevoked, RateLimits_RequestsPerMinute = EXCLUDED.RateLimits_RequestsPerMinute";
                    cmd.Parameters.AddWithValue("@id", token.Id);
                    cmd.Parameters.AddWithValue("@hash", token.TokenHash ?? string.Empty);
                    cmd.Parameters.AddWithValue("@charUUID", (object)token.CharacterUUID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@role", (int)token.Role);
                    cmd.Parameters.AddWithValue("@factionUUID", (object)token.FactionUUID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@created", token.CreatedUtc.ToString("O"));
                    cmd.Parameters.AddWithValue("@lastUsed", token.LastUsedUtc.HasValue ? (object)token.LastUsedUtc.Value.ToString("O") : DBNull.Value);
                    cmd.Parameters.AddWithValue("@revoked", token.IsRevoked);
                    cmd.Parameters.AddWithValue("@rpm", token.RateLimits?.RequestsPerMinute ?? 300);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteTokenAsync(string id)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM ApiTokens WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Membership Actions
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<MembershipAction>> GetFactionActionsAsync(string factionUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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

                return (IReadOnlyList<MembershipAction>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertMembershipActionAsync(MembershipAction action)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO MembershipActions (Id, FactionUUID, CharacterUUID, Type, CreatedUtc, ExpiresUtc)
                                       VALUES (@id, @fid, @cid, @type, @created, @expires)
                                       ON CONFLICT (Id) DO UPDATE SET FactionUUID = EXCLUDED.FactionUUID, CharacterUUID = EXCLUDED.CharacterUUID, Type = EXCLUDED.Type, CreatedUtc = EXCLUDED.CreatedUtc, ExpiresUtc = EXCLUDED.ExpiresUtc";
                    cmd.Parameters.AddWithValue("@id", action.Id);
                    cmd.Parameters.AddWithValue("@fid", action.FactionUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@cid", action.CharacterUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@type", (int)action.Type);
                    cmd.Parameters.AddWithValue("@created", action.CreatedUtc.ToString("O"));
                    cmd.Parameters.AddWithValue("@expires", action.ExpiresUtc.ToString("O"));
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteMembershipActionAsync(string id)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM MembershipActions WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteExpiredActionsAsync(DateTime cutoff)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM MembershipActions WHERE ExpiresUtc < @cutoff";
                    cmd.Parameters.AddWithValue("@cutoff", cutoff.ToString("O"));
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Sharing Rules
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<SharingRule>> GetSharingRulesForCharacterAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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

                return (IReadOnlyList<SharingRule>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertSharingRulesAsync(string characterUUID, IReadOnlyList<SharingRule> rules)
        {
            _retryPolicy.Execute(() =>
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
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Character Preferences
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<CharacterPreferences> GetCharacterPreferencesAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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
                            return new CharacterPreferences
                            {
                                CharacterUUID = reader.GetString(0),
                                ServerProcessing = reader.GetBoolean(1),
                            };
                        }
                    }
                }

                return (CharacterPreferences)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertCharacterPreferencesAsync(CharacterPreferences prefs)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO CharacterPreferences (CharacterUUID, ServerProcessing)
                                       VALUES (@cid, @processing)
                                       ON CONFLICT (CharacterUUID) DO UPDATE SET ServerProcessing = EXCLUDED.ServerProcessing";
                    cmd.Parameters.AddWithValue("@cid", prefs.CharacterUUID);
                    cmd.Parameters.AddWithValue("@processing", prefs.ServerProcessing);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Colony
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Colony>> GetAllColoniesAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<Colony>();
                using (var conn = OpenConnection())
                {
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
                    }

                    foreach (var colony in results)
                    {
                        colony.Structures = LoadColonyStructures(conn, colony.UUID);
                        colony.Items = LoadItems(conn, colony.UUID, "Colony");
                    }
                }

                return (IReadOnlyList<Colony>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<Colony> GetColonyAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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
                            return colony;
                        }
                    }
                }

                return (Colony)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertColonyAsync(string characterUUID, Colony entity)
        {
            _retryPolicy.Execute(() =>
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
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteColonyAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                {
                    using (var delItems = conn.CreateCommand())
                    {
                        delItems.CommandText = "DELETE FROM Items WHERE ParentUUID = @uuid AND ParentType = 'Colony'";
                        delItems.Parameters.AddWithValue("@uuid", entityUUID);
                        delItems.ExecuteNonQuery();
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM Colonies WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                        cmd.Parameters.AddWithValue("@uuid", entityUUID);
                        cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                        cmd.ExecuteNonQuery();
                    }
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Blueprint
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Blueprint>> GetAllBlueprintsAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<Blueprint>();
                using (var conn = OpenConnection())
                {
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
                    }

                    foreach (var bp in results)
                    {
                        bp.Properties = LoadPropertyBag(conn, "BlueprintProperties", "BlueprintUUID", bp.UUID);
                        bp.Resources = LoadBlueprintResources(conn, bp.UUID);
                    }
                }

                return (IReadOnlyList<Blueprint>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<Blueprint> GetBlueprintAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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
                            return bp;
                        }
                    }
                }

                return (Blueprint)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertBlueprintAsync(string characterUUID, Blueprint entity)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    UpsertBlueprintParent(conn, tx, characterUUID, entity);
                    DeleteBlueprintChildren(conn, tx, entity.UUID);
                    InsertBlueprintChildren(conn, tx, entity);
                    tx.Commit();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteBlueprintAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Blueprints WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Survey
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Survey>> GetAllSurveysAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<Survey>();
                using (var conn = OpenConnection())
                {
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
                    }

                    foreach (var survey in results)
                    {
                        survey.Properties = LoadSurveyProperties(conn, survey.UUID);
                        survey.Resources = LoadSurveyResources(conn, survey.UUID);
                    }
                }

                return (IReadOnlyList<Survey>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<Survey> GetSurveyAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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
                            return survey;
                        }
                    }
                }

                return (Survey)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertSurveyAsync(string characterUUID, Survey entity)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    UpsertSurveyParent(conn, tx, characterUUID, entity);
                    DeleteSurveyChildren(conn, tx, entity.UUID);
                    InsertSurveyChildren(conn, tx, entity);
                    tx.Commit();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteSurveyAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Surveys WHERE SurveyID = @surveyId AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@surveyId", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — PlayerProfile
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<PlayerProfile>> GetAllPlayerProfilesAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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

                return (IReadOnlyList<PlayerProfile>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<PlayerProfile> GetPlayerProfileAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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
                            return profile;
                        }
                    }
                }

                return (PlayerProfile)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertPlayerProfileAsync(string characterUUID, PlayerProfile entity)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    UpsertPlayerProfileParent(conn, tx, entity);
                    DeletePlayerProfileChildren(conn, tx, entity.UUID);
                    InsertPlayerSkills(conn, tx, entity);
                    tx.Commit();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeletePlayerProfileAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM PlayerProfiles WHERE UUID = @uuid";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — DeliveryRoute
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<DeliveryRoute>> GetAllDeliveryRoutesAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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

                return (IReadOnlyList<DeliveryRoute>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<DeliveryRoute> GetDeliveryRouteAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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
                            return route;
                        }
                    }
                }

                return (DeliveryRoute)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertDeliveryRouteAsync(string characterUUID, DeliveryRoute entity)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    UpsertDeliveryRouteParent(conn, tx, characterUUID, entity);
                    DeleteDeliveryRouteChildren(conn, tx, entity.UUID);
                    InsertDeliveryRouteStops(conn, tx, entity);
                    tx.Commit();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteDeliveryRouteAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM DeliveryRoutes WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — DeliveryPlan
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<DeliveryPlan>> GetAllDeliveryPlansAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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

                return (IReadOnlyList<DeliveryPlan>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<DeliveryPlan> GetDeliveryPlanAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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
                            return plan;
                        }
                    }
                }

                return (DeliveryPlan)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertDeliveryPlanAsync(string characterUUID, DeliveryPlan entity)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    UpsertDeliveryPlanParent(conn, tx, characterUUID, entity);
                    DeleteDeliveryPlanChildren(conn, tx, entity.UUID);
                    InsertDeliveryPlanChildren(conn, tx, entity);
                    tx.Commit();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteDeliveryPlanAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM DeliveryPlans WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Ship
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Ship>> GetAllShipsAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<Ship>();
                using (var conn = OpenConnection())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM Ships WHERE OwnerUUID = @ownerUUID";
                        cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                        using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<Ship>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<Ship> GetShipAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Ships WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var ship = ReadShipParent(reader);
                            ship.Components = LoadShipComponents(conn, ship.UUID);
                            ship.Cargo = LoadItems(conn, ship.UUID, "ShipCargo");
                            ship.Hopper = LoadItems(conn, ship.UUID, "ShipHopper");
                            return ship;
                        }
                    }
                }

                return (Ship)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertShipAsync(string characterUUID, Ship entity)
        {
            _retryPolicy.Execute(() =>
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
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteShipAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                {
                    using (var delItems = conn.CreateCommand())
                    {
                        delItems.CommandText = "DELETE FROM Items WHERE ParentUUID = @uuid AND (ParentType = 'ShipCargo' OR ParentType = 'ShipHopper')";
                        delItems.Parameters.AddWithValue("@uuid", entityUUID);
                        delItems.ExecuteNonQuery();
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM Ships WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                        cmd.Parameters.AddWithValue("@uuid", entityUUID);
                        cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                        cmd.ExecuteNonQuery();
                    }
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — ShipTemplate
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<ShipTemplate>> GetAllShipTemplatesAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<ShipTemplate>();
                using (var conn = OpenConnection())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM ShipTemplates WHERE OwnerUUID = @ownerUUID";
                        cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                        using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<ShipTemplate>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<ShipTemplate> GetShipTemplateAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM ShipTemplates WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var template = ReadShipTemplateParent(reader);
                            template.Components = LoadShipTemplateComponents(conn, template.UUID);
                            return template;
                        }
                    }
                }

                return (ShipTemplate)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertShipTemplateAsync(string characterUUID, ShipTemplate entity)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    UpsertShipTemplateParent(conn, tx, characterUUID, entity);
                    DeleteShipTemplateChildren(conn, tx, entity.UUID);
                    InsertShipTemplateComponents(conn, tx, entity);
                    tx.Commit();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteShipTemplateAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM ShipTemplates WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — MarketListing
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<MarketListing>> GetAllMarketListingsAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<MarketListing>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM MarketListings WHERE OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadMarketListing(reader));
                        }
                    }
                }

                return (IReadOnlyList<MarketListing>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<MarketListing> GetMarketListingAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM MarketListings WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return ReadMarketListing(reader);
                        }
                    }
                }

                return (MarketListing)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertMarketListingAsync(string characterUUID, MarketListing entity)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO MarketListings (
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
                    ) ON CONFLICT (UUID) DO UPDATE SET
                        OwnerUUID = EXCLUDED.OwnerUUID, StationUUID = EXCLUDED.StationUUID,
                        ItemType = EXCLUDED.ItemType, ItemReferenceID = EXCLUDED.ItemReferenceID,
                        ItemName = EXCLUDED.ItemName, Quantity = EXCLUDED.Quantity,
                        PricePerUnit = EXCLUDED.PricePerUnit, CurrentHP = EXCLUDED.CurrentHP,
                        MaxHP = EXCLUDED.MaxHP, MaxRepairPercent = EXCLUDED.MaxRepairPercent,
                        MarketId = EXCLUDED.MarketId, BuyOrder = EXCLUDED.BuyOrder,
                        BaseItemTypeID = EXCLUDED.BaseItemTypeID, ResourcePurity = EXCLUDED.ResourcePurity,
                        GameTypeCode = EXCLUDED.GameTypeCode, GameTypeId = EXCLUDED.GameTypeId,
                        GameSubTypeId = EXCLUDED.GameSubTypeId, LocationName = EXCLUDED.LocationName,
                        SystemId = EXCLUDED.SystemId, SystemName = EXCLUDED.SystemName,
                        GameLocationId = EXCLUDED.GameLocationId, AmountRemaining = EXCLUDED.AmountRemaining,
                        AmountOriginal = EXCLUDED.AmountOriginal, AmountSold = EXCLUDED.AmountSold,
                        EscrowRemaining = EXCLUDED.EscrowRemaining, SalesTaxEstimate = EXCLUDED.SalesTaxEstimate,
                        ValueRemaining = EXCLUDED.ValueRemaining, Evolution = EXCLUDED.Evolution,
                        HealthPercentage = EXCLUDED.HealthPercentage, SellerName = EXCLUDED.SellerName,
                        SellerFactionTag = EXCLUDED.SellerFactionTag, PrivateSale = EXCLUDED.PrivateSale,
                        BuyerName = EXCLUDED.BuyerName, BuyerFactionTag = EXCLUDED.BuyerFactionTag,
                        IsOutbid = EXCLUDED.IsOutbid, IsUndercut = EXCLUDED.IsUndercut,
                        PlacedDT = EXCLUDED.PlacedDT, ExpiresDT = EXCLUDED.ExpiresDT,
                        CompetitorForMarketId = EXCLUDED.CompetitorForMarketId,
                        SyncedByCharacterUUID = EXCLUDED.SyncedByCharacterUUID,
                        SyncTimestamp = EXCLUDED.SyncTimestamp";
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
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteMarketListingAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM MarketListings WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — MarketTransaction
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<MarketTransaction>> GetAllMarketTransactionsAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<MarketTransaction>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM MarketTransactions WHERE OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadMarketTransaction(reader));
                        }
                    }
                }

                return (IReadOnlyList<MarketTransaction>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<MarketTransaction> GetMarketTransactionAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM MarketTransactions WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return ReadMarketTransaction(reader);
                        }
                    }
                }

                return (MarketTransaction)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertMarketTransactionAsync(string characterUUID, MarketTransaction entity)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO MarketTransactions (
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
                    ) ON CONFLICT (UUID) DO UPDATE SET
                        OwnerUUID = EXCLUDED.OwnerUUID, TransactionType = EXCLUDED.TransactionType,
                        ItemType = EXCLUDED.ItemType, ItemReferenceID = EXCLUDED.ItemReferenceID,
                        ItemName = EXCLUDED.ItemName, Quantity = EXCLUDED.Quantity,
                        PricePerUnit = EXCLUDED.PricePerUnit, TotalPrice = EXCLUDED.TotalPrice,
                        Counterparty = EXCLUDED.Counterparty, CounterpartyFaction = EXCLUDED.CounterpartyFaction,
                        StationUUID = EXCLUDED.StationUUID, Timestamp = EXCLUDED.Timestamp,
                        Notes = EXCLUDED.Notes, ListingUUID = EXCLUDED.ListingUUID,
                        CurrentHP = EXCLUDED.CurrentHP, MaxHP = EXCLUDED.MaxHP,
                        MaxRepairPercent = EXCLUDED.MaxRepairPercent";
                    cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                    cmd.Parameters.AddWithValue("@owner", characterUUID);
                    cmd.Parameters.AddWithValue("@txType", entity.TransactionType.ToString());
                    cmd.Parameters.AddWithValue("@itemType", entity.ItemType.ToString());
                    cmd.Parameters.AddWithValue("@itemRefId", entity.ItemReferenceID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@itemName", entity.ItemName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@qty", entity.Quantity);
                    cmd.Parameters.AddWithValue("@price", (double)entity.PricePerUnit);
                    cmd.Parameters.AddWithValue("@total", (double)entity.TotalPrice);
                    cmd.Parameters.AddWithValue("@counterparty", entity.Counterparty ?? string.Empty);
                    cmd.Parameters.AddWithValue("@counterpartyFaction", entity.CounterpartyFaction ?? string.Empty);
                    cmd.Parameters.AddWithValue("@stationUUID", entity.StationUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@timestamp", entity.Timestamp ?? string.Empty);
                    cmd.Parameters.AddWithValue("@notes", entity.Notes ?? string.Empty);
                    cmd.Parameters.AddWithValue("@listingUUID", entity.ListingUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@curHp", entity.CurrentHP);
                    cmd.Parameters.AddWithValue("@maxHp", entity.MaxHP);
                    cmd.Parameters.AddWithValue("@maxRepair", (double)entity.MaxRepairPercent);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteMarketTransactionAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM MarketTransactions WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — PricingPlan
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<PricingPlan>> GetAllPricingPlansAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<PricingPlan>();
                using (var conn = OpenConnection())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM PricingPlans WHERE OwnerUUID = @ownerUUID";
                        cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                        using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<PricingPlan>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<PricingPlan> GetPricingPlanAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM PricingPlans WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var plan = ReadPricingPlanParent(reader);
                            plan.ResourcePrices = LoadPricingPlanPrices(conn, plan.UUID);
                            return plan;
                        }
                    }
                }

                return (PricingPlan)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertPricingPlanAsync(string characterUUID, PricingPlan entity)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    UpsertPricingPlanParent(conn, tx, characterUUID, entity);
                    DeletePricingPlanChildren(conn, tx, entity.UUID);
                    InsertPricingPlanPrices(conn, tx, entity);
                    tx.Commit();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeletePricingPlanAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM PricingPlans WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — StockPlan
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<StockPlan>> GetAllStockPlansAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<StockPlan>();
                using (var conn = OpenConnection())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM StockPlans WHERE OwnerUUID = @ownerUUID";
                        cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                        using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<StockPlan>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<StockPlan> GetStockPlanAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM StockPlans WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var plan = ReadStockPlanParent(reader);
                            plan.Targets = LoadStockTargets(conn, plan.UUID);
                            return plan;
                        }
                    }
                }

                return (StockPlan)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertStockPlanAsync(string characterUUID, StockPlan entity)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    UpsertStockPlanParent(conn, tx, characterUUID, entity);
                    DeleteStockPlanChildren(conn, tx, entity.UUID);
                    InsertStockTargets(conn, tx, entity);
                    tx.Commit();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteStockPlanAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM StockPlans WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — StockProfile
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<StockProfile>> GetAllStockProfilesAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<StockProfile>();
                using (var conn = OpenConnection())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM StockProfiles WHERE OwnerUUID = @ownerUUID";
                        cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                        using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<StockProfile>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<StockProfile> GetStockProfileAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM StockProfiles WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var profile = ReadStockProfileParent(reader);
                            profile.Entries = LoadStockProfileEntries(conn, profile.UUID);
                            return profile;
                        }
                    }
                }

                return (StockProfile)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertStockProfileAsync(string characterUUID, StockProfile entity)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    UpsertStockProfileParent(conn, tx, characterUUID, entity);
                    DeleteStockProfileChildren(conn, tx, entity.UUID);
                    InsertStockProfileEntries(conn, tx, entity);
                    tx.Commit();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteStockProfileAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM StockProfiles WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — BuildPlan
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<BuildPlan>> GetAllBuildPlansAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<BuildPlan>();
                using (var conn = OpenConnection())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM BuildPlans WHERE OwnerUUID = @ownerUUID";
                        cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                        using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<BuildPlan>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<BuildPlan> GetBuildPlanAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM BuildPlans WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var plan = ReadBuildPlanParent(reader);
                            plan.Items = LoadBuildItems(conn, plan.UUID);
                            return plan;
                        }
                    }
                }

                return (BuildPlan)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertBuildPlanAsync(string characterUUID, BuildPlan entity)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    UpsertBuildPlanParent(conn, tx, characterUUID, entity);
                    DeleteBuildPlanChildren(conn, tx, entity.UUID);
                    InsertBuildItems(conn, tx, entity);
                    tx.Commit();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteBuildPlanAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM BuildPlans WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — SupplyChain
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<SupplyChain>> GetAllSupplyChainsAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<SupplyChain>();
                using (var conn = OpenConnection())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM SupplyChains WHERE OwnerUUID = @ownerUUID";
                        cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                        using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<SupplyChain>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<SupplyChain> GetSupplyChainAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM SupplyChains WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var chain = ReadSupplyChainParent(reader);
                            chain.Stages = LoadSupplyChainStages(conn, chain.UUID);
                            return chain;
                        }
                    }
                }

                return (SupplyChain)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertSupplyChainAsync(string characterUUID, SupplyChain entity)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    UpsertSupplyChainParent(conn, tx, characterUUID, entity);
                    DeleteSupplyChainChildren(conn, tx, entity.UUID);
                    InsertSupplyChainStages(conn, tx, entity);
                    tx.Commit();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteSupplyChainAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM SupplyChains WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Asteroid
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Asteroid>> GetAllAsteroidsAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<Asteroid>();
                using (var conn = OpenConnection())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM Asteroids WHERE OwnerUUID = @ownerUUID";
                        cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                        using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<Asteroid>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<Asteroid> GetAsteroidAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Asteroids WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var asteroid = ReadAsteroidParent(reader);
                            asteroid.Reserves = LoadAsteroidReserves(conn, asteroid.UUID);
                            return asteroid;
                        }
                    }
                }

                return (Asteroid)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertAsteroidAsync(string characterUUID, Asteroid entity)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    UpsertAsteroidParent(conn, tx, characterUUID, entity);
                    DeleteAsteroidChildren(conn, tx, entity.UUID);
                    InsertAsteroidReserves(conn, tx, entity);
                    tx.Commit();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteAsteroidAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Asteroids WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Station
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Station>> GetAllStationsAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<Station>();
                using (var conn = OpenConnection())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM Stations WHERE OwnerUUID = @ownerUUID";
                        cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                        using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<Station>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<Station> GetStationAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Stations WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var station = ReadStationParent(reader);
                            station.Components = LoadStationComponents(conn, station.UUID);
                            LoadStationItems(conn, station);
                            return station;
                        }
                    }
                }

                return (Station)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertStationAsync(string characterUUID, Station entity)
        {
            _retryPolicy.Execute(() =>
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
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteStationAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                {
                    using (var delItems = conn.CreateCommand())
                    {
                        delItems.CommandText = "DELETE FROM Items WHERE ParentUUID = @uuid AND (ParentType = 'StationMunitions' OR ParentType LIKE 'StationHold:%')";
                        delItems.Parameters.AddWithValue("@uuid", entityUUID);
                        delItems.ExecuteNonQuery();
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM Stations WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                        cmd.Parameters.AddWithValue("@uuid", entityUUID);
                        cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                        cmd.ExecuteNonQuery();
                    }
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Faction (contacts)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Faction>> GetAllFactionsForCharacterAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<Faction>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Factions WHERE OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<Faction>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<Faction> GetFactionForCharacterAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Factions WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Faction
                            {
                                UUID = reader["UUID"] as string,
                                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                                Name = reader["Name"] as string ?? string.Empty,
                                Description = reader["Description"] as string ?? string.Empty,
                            };
                        }
                    }
                }

                return (Faction)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertFactionForCharacterAsync(string characterUUID, Faction entity)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Factions (UUID, Name, OwnerUUID, Tag, Description)
                                       VALUES (@uuid, @name, @owner, @tag, @desc)
                                       ON CONFLICT (UUID) DO UPDATE SET
                                       Name = EXCLUDED.Name, OwnerUUID = EXCLUDED.OwnerUUID,
                                       Tag = EXCLUDED.Tag, Description = EXCLUDED.Description";
                    cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                    cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@owner", characterUUID);
                    cmd.Parameters.AddWithValue("@tag", string.Empty);
                    cmd.Parameters.AddWithValue("@desc", entity.Description ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteFactionForCharacterAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Factions WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — ExternalCharacter
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<ExternalCharacter>> GetAllExternalCharactersAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<ExternalCharacter>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM ExternalCharacters WHERE OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<ExternalCharacter>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<ExternalCharacter> GetExternalCharacterAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM ExternalCharacters WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new ExternalCharacter
                            {
                                UUID = reader["UUID"] as string,
                                OwnerUUID = reader["OwnerUUID"] as string ?? string.Empty,
                                Name = reader["Name"] as string ?? string.Empty,
                                FactionUUID = reader["FactionUUID"] as string ?? string.Empty,
                            };
                        }
                    }
                }

                return (ExternalCharacter)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertExternalCharacterAsync(string characterUUID, ExternalCharacter entity)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO ExternalCharacters (UUID, Name, OwnerUUID, FactionUUID, FactionName, CharacterId, Notes)
                                       VALUES (@uuid, @name, @owner, @factionUUID, @factionName, @charId, @notes)
                                       ON CONFLICT (UUID) DO UPDATE SET
                                       Name = EXCLUDED.Name, OwnerUUID = EXCLUDED.OwnerUUID,
                                       FactionUUID = EXCLUDED.FactionUUID, FactionName = EXCLUDED.FactionName,
                                       CharacterId = EXCLUDED.CharacterId, Notes = EXCLUDED.Notes";
                    cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                    cmd.Parameters.AddWithValue("@name", entity.Name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@owner", characterUUID);
                    cmd.Parameters.AddWithValue("@factionUUID", entity.FactionUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@factionName", string.Empty);
                    cmd.Parameters.AddWithValue("@charId", 0);
                    cmd.Parameters.AddWithValue("@notes", string.Empty);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteExternalCharacterAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM ExternalCharacters WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — WarehouseOverflowRule
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<WarehouseOverflowRule>> GetAllWarehouseOverflowRulesAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<WarehouseOverflowRule>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM WarehouseOverflowRules WHERE OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadWarehouseOverflowRule(reader));
                        }
                    }
                }

                return (IReadOnlyList<WarehouseOverflowRule>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<WarehouseOverflowRule> GetWarehouseOverflowRuleAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM WarehouseOverflowRules WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return ReadWarehouseOverflowRule(reader);
                        }
                    }
                }

                return (WarehouseOverflowRule)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertWarehouseOverflowRuleAsync(string characterUUID, WarehouseOverflowRule entity)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO WarehouseOverflowRules (UUID, OwnerUUID, ColonyUUID, ResourceName, RuleType, Threshold, DestinationColonyUUID)
                                       VALUES (@uuid, @owner, @colony, @resource, @ruleType, @threshold, @dest)
                                       ON CONFLICT (UUID) DO UPDATE SET
                                       OwnerUUID = EXCLUDED.OwnerUUID, ColonyUUID = EXCLUDED.ColonyUUID,
                                       ResourceName = EXCLUDED.ResourceName, RuleType = EXCLUDED.RuleType,
                                       Threshold = EXCLUDED.Threshold, DestinationColonyUUID = EXCLUDED.DestinationColonyUUID";
                    cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                    cmd.Parameters.AddWithValue("@owner", characterUUID);
                    cmd.Parameters.AddWithValue("@colony", entity.ColonyUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@resource", entity.ResourceName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@ruleType", (int)entity.RuleType);
                    cmd.Parameters.AddWithValue("@threshold", (int)entity.TriggerThreshold);
                    cmd.Parameters.AddWithValue("@dest", entity.DestinationUUID ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteWarehouseOverflowRuleAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM WarehouseOverflowRules WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — MailMessage
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<MailMessage>> GetAllMailMessagesAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<MailMessage>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM MailMessages WHERE OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadMailMessage(reader));
                        }
                    }
                }

                return (IReadOnlyList<MailMessage>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<MailMessage> GetMailMessageAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM MailMessages WHERE OwnerUUID = @ownerUUID AND MailId = CAST(@mailId AS INTEGER)";
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    cmd.Parameters.AddWithValue("@mailId", entityUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return ReadMailMessage(reader);
                        }
                    }
                }

                return (MailMessage)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertMailMessageAsync(string characterUUID, MailMessage entity)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO MailMessages (MailId, OwnerUUID, CharacterIdFrom, FromName, CharacterIdTo, ToName, SentTime, Subject, MailRead, MailType, MailContent, LocalRead)
                                       VALUES (@mailId, @owner, @fromId, @fromName, @toId, @toName, @sent, @subject, @mailRead, @mailType, @content, @localRead)
                                       ON CONFLICT (MailId, OwnerUUID) DO UPDATE SET
                                       CharacterIdFrom = EXCLUDED.CharacterIdFrom, FromName = EXCLUDED.FromName,
                                       CharacterIdTo = EXCLUDED.CharacterIdTo, ToName = EXCLUDED.ToName,
                                       SentTime = EXCLUDED.SentTime, Subject = EXCLUDED.Subject,
                                       MailRead = EXCLUDED.MailRead, MailType = EXCLUDED.MailType,
                                       MailContent = EXCLUDED.MailContent, LocalRead = EXCLUDED.LocalRead";
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
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteMailMessageAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM MailMessages WHERE OwnerUUID = @ownerUUID AND MailId = CAST(@mailId AS INTEGER)";
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    cmd.Parameters.AddWithValue("@mailId", entityUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — BankingTransaction
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<BankingTransaction>> GetAllBankingTransactionsAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<BankingTransaction>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM BankingTransactions WHERE OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadBankingTransaction(reader));
                        }
                    }
                }

                return (IReadOnlyList<BankingTransaction>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<BankingTransaction> GetBankingTransactionAsync(string characterUUID, string entityUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM BankingTransactions WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return ReadBankingTransaction(reader);
                        }
                    }
                }

                return (BankingTransaction)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertBankingTransactionAsync(string characterUUID, BankingTransaction entity)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO BankingTransactions (UUID, OwnerUUID, TransactionDateTime, CreditChange, OldBalance, NewBalance, TransactionType, Detail, CharacterId, SystemObjectId, SystemId, IsManualEntry)
                                       VALUES (@uuid, @owner, @txnDate, @credit, @oldBal, @newBal, @txnType, @detail, @charId, @sysObjId, @sysId, @manual)
                                       ON CONFLICT (UUID) DO UPDATE SET
                                       OwnerUUID = EXCLUDED.OwnerUUID, TransactionDateTime = EXCLUDED.TransactionDateTime,
                                       CreditChange = EXCLUDED.CreditChange, OldBalance = EXCLUDED.OldBalance,
                                       NewBalance = EXCLUDED.NewBalance, TransactionType = EXCLUDED.TransactionType,
                                       Detail = EXCLUDED.Detail, CharacterId = EXCLUDED.CharacterId,
                                       SystemObjectId = EXCLUDED.SystemObjectId, SystemId = EXCLUDED.SystemId,
                                       IsManualEntry = EXCLUDED.IsManualEntry";
                    cmd.Parameters.AddWithValue("@uuid", entity.UUID);
                    cmd.Parameters.AddWithValue("@owner", characterUUID);
                    cmd.Parameters.AddWithValue("@txnDate", entity.TransactionDateTime ?? string.Empty);
                    cmd.Parameters.AddWithValue("@credit", (double)entity.CreditChange);
                    cmd.Parameters.AddWithValue("@oldBal", (double)entity.OldBalance);
                    cmd.Parameters.AddWithValue("@newBal", (double)entity.NewBalance);
                    cmd.Parameters.AddWithValue("@txnType", entity.TransactionType);
                    cmd.Parameters.AddWithValue("@detail", entity.Detail ?? string.Empty);
                    cmd.Parameters.AddWithValue("@charId", entity.CharacterId.HasValue ? (object)entity.CharacterId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@sysObjId", entity.SystemObjectId.HasValue ? (object)entity.SystemObjectId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@sysId", entity.SystemId.HasValue ? (object)entity.SystemId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@manual", entity.IsManualEntry ? 1 : 0);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteBankingTransactionAsync(string characterUUID, string entityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM BankingTransactions WHERE UUID = @uuid AND OwnerUUID = @ownerUUID";
                    cmd.Parameters.AddWithValue("@uuid", entityUUID);
                    cmd.Parameters.AddWithValue("@ownerUUID", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Faction Permission Entities
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionCapability>> GetFactionCapabilitiesAsync(string factionUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<FactionCapability>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM FactionCapabilities WHERE FactionUUID = @fid";
                    cmd.Parameters.AddWithValue("@fid", factionUUID);
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<FactionCapability>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertFactionCapabilityAsync(FactionCapability capability)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO FactionCapabilities (UUID, FactionUUID, Name, Description)
                                       VALUES (@uuid, @fid, @name, @desc)
                                       ON CONFLICT (UUID) DO UPDATE SET
                                       FactionUUID = EXCLUDED.FactionUUID, Name = EXCLUDED.Name, Description = EXCLUDED.Description";
                    cmd.Parameters.AddWithValue("@uuid", capability.UUID);
                    cmd.Parameters.AddWithValue("@fid", capability.FactionUUID);
                    cmd.Parameters.AddWithValue("@name", capability.Name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@desc", capability.Description ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteFactionCapabilityAsync(string factionUUID, string capabilityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM FactionCapabilities WHERE UUID = @uuid AND FactionUUID = @fid";
                    cmd.Parameters.AddWithValue("@uuid", capabilityUUID);
                    cmd.Parameters.AddWithValue("@fid", factionUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionClearanceLevel>> GetFactionClearanceLevelsAsync(string factionUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<FactionClearanceLevel>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM FactionClearanceLevels WHERE FactionUUID = @fid";
                    cmd.Parameters.AddWithValue("@fid", factionUUID);
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<FactionClearanceLevel>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertFactionClearanceLevelAsync(FactionClearanceLevel level)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO FactionClearanceLevels (UUID, FactionUUID, Level, Name, Description)
                                       VALUES (@uuid, @fid, @level, @name, @desc)
                                       ON CONFLICT (UUID) DO UPDATE SET
                                       FactionUUID = EXCLUDED.FactionUUID, Level = EXCLUDED.Level,
                                       Name = EXCLUDED.Name, Description = EXCLUDED.Description";
                    cmd.Parameters.AddWithValue("@uuid", level.UUID);
                    cmd.Parameters.AddWithValue("@fid", level.FactionUUID);
                    cmd.Parameters.AddWithValue("@level", level.Level);
                    cmd.Parameters.AddWithValue("@name", level.Name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@desc", level.Description ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteFactionClearanceLevelAsync(string factionUUID, string levelUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM FactionClearanceLevels WHERE UUID = @uuid AND FactionUUID = @fid";
                    cmd.Parameters.AddWithValue("@uuid", levelUUID);
                    cmd.Parameters.AddWithValue("@fid", factionUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionPermissionGroup>> GetFactionGroupsAsync(string factionUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<FactionPermissionGroup>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM FactionPermissionGroups WHERE FactionUUID = @fid";
                    cmd.Parameters.AddWithValue("@fid", factionUUID);
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<FactionPermissionGroup>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<FactionPermissionGroup> GetFactionGroupAsync(string factionUUID, string groupUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM FactionPermissionGroups WHERE UUID = @uuid AND FactionUUID = @fid";
                    cmd.Parameters.AddWithValue("@uuid", groupUUID);
                    cmd.Parameters.AddWithValue("@fid", factionUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new FactionPermissionGroup
                            {
                                UUID = reader["UUID"] as string ?? string.Empty,
                                FactionUUID = reader["FactionUUID"] as string ?? string.Empty,
                                Name = reader["Name"] as string ?? string.Empty,
                                Description = reader["Description"] as string ?? string.Empty,
                                DefaultClearanceLevelUUID = reader["DefaultClearanceLevelUUID"] as string ?? string.Empty,
                            };
                        }
                    }
                }

                return (FactionPermissionGroup)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertFactionGroupAsync(FactionPermissionGroup group)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO FactionPermissionGroups (UUID, FactionUUID, Name, Description, DefaultClearanceLevelUUID)
                                       VALUES (@uuid, @fid, @name, @desc, @defaultCl)
                                       ON CONFLICT (UUID) DO UPDATE SET
                                       FactionUUID = EXCLUDED.FactionUUID, Name = EXCLUDED.Name,
                                       Description = EXCLUDED.Description, DefaultClearanceLevelUUID = EXCLUDED.DefaultClearanceLevelUUID";
                    cmd.Parameters.AddWithValue("@uuid", group.UUID);
                    cmd.Parameters.AddWithValue("@fid", group.FactionUUID);
                    cmd.Parameters.AddWithValue("@name", group.Name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@desc", group.Description ?? string.Empty);
                    cmd.Parameters.AddWithValue("@defaultCl", group.DefaultClearanceLevelUUID ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteFactionGroupAsync(string factionUUID, string groupUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM FactionPermissionGroups WHERE UUID = @uuid AND FactionUUID = @fid";
                    cmd.Parameters.AddWithValue("@uuid", groupUUID);
                    cmd.Parameters.AddWithValue("@fid", factionUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionGroupCapability>> GetFactionGroupCapabilitiesAsync(string groupUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<FactionGroupCapability>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM FactionGroupCapabilities WHERE GroupUUID = @gid";
                    cmd.Parameters.AddWithValue("@gid", groupUUID);
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<FactionGroupCapability>)results;
            }));
        }

        /// <inheritdoc/>
        public Task AddFactionGroupCapabilityAsync(FactionGroupCapability item)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO FactionGroupCapabilities (GroupUUID, CapabilityUUID)
                                       VALUES (@gid, @cid)
                                       ON CONFLICT (GroupUUID, CapabilityUUID) DO NOTHING";
                    cmd.Parameters.AddWithValue("@gid", item.GroupUUID);
                    cmd.Parameters.AddWithValue("@cid", item.CapabilityUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RemoveFactionGroupCapabilityAsync(string groupUUID, string capabilityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM FactionGroupCapabilities WHERE GroupUUID = @gid AND CapabilityUUID = @cid";
                    cmd.Parameters.AddWithValue("@gid", groupUUID);
                    cmd.Parameters.AddWithValue("@cid", capabilityUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionGroupSharingRule>> GetFactionGroupSharingRulesAsync(string groupUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<FactionGroupSharingRule>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM FactionGroupSharingRules WHERE GroupUUID = @gid";
                    cmd.Parameters.AddWithValue("@gid", groupUUID);
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<FactionGroupSharingRule>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertFactionGroupSharingRuleAsync(FactionGroupSharingRule rule)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO FactionGroupSharingRules (UUID, GroupUUID, DataType, EntityUUID, MinClearanceLevelUUID)
                                       VALUES (@uuid, @gid, @dataType, @entityUUID, @minCl)
                                       ON CONFLICT (UUID) DO UPDATE SET
                                       GroupUUID = EXCLUDED.GroupUUID, DataType = EXCLUDED.DataType,
                                       EntityUUID = EXCLUDED.EntityUUID, MinClearanceLevelUUID = EXCLUDED.MinClearanceLevelUUID";
                    cmd.Parameters.AddWithValue("@uuid", rule.UUID);
                    cmd.Parameters.AddWithValue("@gid", rule.GroupUUID);
                    cmd.Parameters.AddWithValue("@dataType", (object)rule.DataType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@entityUUID", (object)rule.EntityUUID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@minCl", rule.MinClearanceLevelUUID ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteFactionGroupSharingRuleAsync(string groupUUID, string ruleUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM FactionGroupSharingRules WHERE UUID = @uuid AND GroupUUID = @gid";
                    cmd.Parameters.AddWithValue("@uuid", ruleUUID);
                    cmd.Parameters.AddWithValue("@gid", groupUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<FactionMemberPermissions> GetFactionMemberPermissionsAsync(string factionUUID, string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM FactionMemberPermissions WHERE FactionUUID = @fid AND CharacterUUID = @cid";
                    cmd.Parameters.AddWithValue("@fid", factionUUID);
                    cmd.Parameters.AddWithValue("@cid", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new FactionMemberPermissions
                            {
                                CharacterUUID = reader["CharacterUUID"] as string ?? string.Empty,
                                FactionUUID = reader["FactionUUID"] as string ?? string.Empty,
                                GroupUUID = reader["GroupUUID"] as string,
                                ClearanceLevelUUID = reader["ClearanceLevelUUID"] as string ?? string.Empty,
                            };
                        }
                    }
                }

                return (FactionMemberPermissions)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertFactionMemberPermissionsAsync(FactionMemberPermissions perms)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO FactionMemberPermissions (FactionUUID, CharacterUUID, GroupUUID, ClearanceLevelUUID)
                                       VALUES (@fid, @cid, @gid, @clid)
                                       ON CONFLICT (FactionUUID, CharacterUUID) DO UPDATE SET
                                       GroupUUID = EXCLUDED.GroupUUID, ClearanceLevelUUID = EXCLUDED.ClearanceLevelUUID";
                    cmd.Parameters.AddWithValue("@fid", perms.FactionUUID);
                    cmd.Parameters.AddWithValue("@cid", perms.CharacterUUID);
                    cmd.Parameters.AddWithValue("@gid", (object)perms.GroupUUID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@clid", perms.ClearanceLevelUUID ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionMemberPermissions>> GetAllFactionMembersPermissionsAsync(string factionUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<FactionMemberPermissions>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM FactionMemberPermissions WHERE FactionUUID = @fid";
                    cmd.Parameters.AddWithValue("@fid", factionUUID);
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<FactionMemberPermissions>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionMemberCapability>> GetFactionMemberCapabilitiesAsync(string factionUUID, string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<FactionMemberCapability>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM FactionMemberCapabilities WHERE FactionUUID = @fid AND CharacterUUID = @cid";
                    cmd.Parameters.AddWithValue("@fid", factionUUID);
                    cmd.Parameters.AddWithValue("@cid", characterUUID);
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<FactionMemberCapability>)results;
            }));
        }

        /// <inheritdoc/>
        public Task AddFactionMemberCapabilityAsync(FactionMemberCapability item)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO FactionMemberCapabilities (FactionUUID, CharacterUUID, CapabilityUUID)
                                       VALUES (@fid, @cid, @capId)
                                       ON CONFLICT (FactionUUID, CharacterUUID, CapabilityUUID) DO NOTHING";
                    cmd.Parameters.AddWithValue("@fid", item.FactionUUID);
                    cmd.Parameters.AddWithValue("@cid", item.CharacterUUID);
                    cmd.Parameters.AddWithValue("@capId", item.CapabilityUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RemoveFactionMemberCapabilityAsync(string factionUUID, string characterUUID, string capabilityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM FactionMemberCapabilities WHERE FactionUUID = @fid AND CharacterUUID = @cid AND CapabilityUUID = @capId";
                    cmd.Parameters.AddWithValue("@fid", factionUUID);
                    cmd.Parameters.AddWithValue("@cid", characterUUID);
                    cmd.Parameters.AddWithValue("@capId", capabilityUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Character Permission Entities
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterCapability>> GetCharacterCapabilitiesAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<CharacterCapability>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM CharacterCapabilities WHERE OwnerCharacterUUID = @cid";
                    cmd.Parameters.AddWithValue("@cid", characterUUID);
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<CharacterCapability>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertCharacterCapabilityAsync(CharacterCapability capability)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO CharacterCapabilities (UUID, OwnerCharacterUUID, Name, Description)
                                       VALUES (@uuid, @cid, @name, @desc)
                                       ON CONFLICT (UUID) DO UPDATE SET
                                       OwnerCharacterUUID = EXCLUDED.OwnerCharacterUUID, Name = EXCLUDED.Name, Description = EXCLUDED.Description";
                    cmd.Parameters.AddWithValue("@uuid", capability.UUID);
                    cmd.Parameters.AddWithValue("@cid", capability.OwnerCharacterUUID);
                    cmd.Parameters.AddWithValue("@name", capability.Name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@desc", capability.Description ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteCharacterCapabilityAsync(string characterUUID, string capabilityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM CharacterCapabilities WHERE UUID = @uuid AND OwnerCharacterUUID = @cid";
                    cmd.Parameters.AddWithValue("@uuid", capabilityUUID);
                    cmd.Parameters.AddWithValue("@cid", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterClearanceLevel>> GetCharacterClearanceLevelsAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<CharacterClearanceLevel>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM CharacterClearanceLevels WHERE OwnerCharacterUUID = @cid";
                    cmd.Parameters.AddWithValue("@cid", characterUUID);
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<CharacterClearanceLevel>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertCharacterClearanceLevelAsync(CharacterClearanceLevel level)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO CharacterClearanceLevels (UUID, OwnerCharacterUUID, Level, Name, Description)
                                       VALUES (@uuid, @cid, @level, @name, @desc)
                                       ON CONFLICT (UUID) DO UPDATE SET
                                       OwnerCharacterUUID = EXCLUDED.OwnerCharacterUUID, Level = EXCLUDED.Level,
                                       Name = EXCLUDED.Name, Description = EXCLUDED.Description";
                    cmd.Parameters.AddWithValue("@uuid", level.UUID);
                    cmd.Parameters.AddWithValue("@cid", level.OwnerCharacterUUID);
                    cmd.Parameters.AddWithValue("@level", level.Level);
                    cmd.Parameters.AddWithValue("@name", level.Name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@desc", level.Description ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteCharacterClearanceLevelAsync(string characterUUID, string levelUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM CharacterClearanceLevels WHERE UUID = @uuid AND OwnerCharacterUUID = @cid";
                    cmd.Parameters.AddWithValue("@uuid", levelUUID);
                    cmd.Parameters.AddWithValue("@cid", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterPermissionGroup>> GetCharacterGroupsAsync(string characterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<CharacterPermissionGroup>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM CharacterPermissionGroups WHERE OwnerCharacterUUID = @cid";
                    cmd.Parameters.AddWithValue("@cid", characterUUID);
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<CharacterPermissionGroup>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<CharacterPermissionGroup> GetCharacterGroupAsync(string characterUUID, string groupUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM CharacterPermissionGroups WHERE UUID = @uuid AND OwnerCharacterUUID = @cid";
                    cmd.Parameters.AddWithValue("@uuid", groupUUID);
                    cmd.Parameters.AddWithValue("@cid", characterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new CharacterPermissionGroup
                            {
                                UUID = reader["UUID"] as string ?? string.Empty,
                                OwnerCharacterUUID = reader["OwnerCharacterUUID"] as string ?? string.Empty,
                                Name = reader["Name"] as string ?? string.Empty,
                                Description = reader["Description"] as string ?? string.Empty,
                                DefaultClearanceLevelUUID = reader["DefaultClearanceLevelUUID"] as string ?? string.Empty,
                            };
                        }
                    }
                }

                return (CharacterPermissionGroup)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertCharacterGroupAsync(CharacterPermissionGroup group)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO CharacterPermissionGroups (UUID, OwnerCharacterUUID, Name, Description, DefaultClearanceLevelUUID)
                                       VALUES (@uuid, @cid, @name, @desc, @defaultCl)
                                       ON CONFLICT (UUID) DO UPDATE SET
                                       OwnerCharacterUUID = EXCLUDED.OwnerCharacterUUID, Name = EXCLUDED.Name,
                                       Description = EXCLUDED.Description, DefaultClearanceLevelUUID = EXCLUDED.DefaultClearanceLevelUUID";
                    cmd.Parameters.AddWithValue("@uuid", group.UUID);
                    cmd.Parameters.AddWithValue("@cid", group.OwnerCharacterUUID);
                    cmd.Parameters.AddWithValue("@name", group.Name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@desc", group.Description ?? string.Empty);
                    cmd.Parameters.AddWithValue("@defaultCl", group.DefaultClearanceLevelUUID ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteCharacterGroupAsync(string characterUUID, string groupUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM CharacterPermissionGroups WHERE UUID = @uuid AND OwnerCharacterUUID = @cid";
                    cmd.Parameters.AddWithValue("@uuid", groupUUID);
                    cmd.Parameters.AddWithValue("@cid", characterUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGroupCapability>> GetCharacterGroupCapabilitiesAsync(string groupUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<CharacterGroupCapability>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM CharacterGroupCapabilities WHERE GroupUUID = @gid";
                    cmd.Parameters.AddWithValue("@gid", groupUUID);
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<CharacterGroupCapability>)results;
            }));
        }

        /// <inheritdoc/>
        public Task AddCharacterGroupCapabilityAsync(CharacterGroupCapability item)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO CharacterGroupCapabilities (GroupUUID, CapabilityUUID)
                                       VALUES (@gid, @cid)
                                       ON CONFLICT (GroupUUID, CapabilityUUID) DO NOTHING";
                    cmd.Parameters.AddWithValue("@gid", item.GroupUUID);
                    cmd.Parameters.AddWithValue("@cid", item.CapabilityUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RemoveCharacterGroupCapabilityAsync(string groupUUID, string capabilityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM CharacterGroupCapabilities WHERE GroupUUID = @gid AND CapabilityUUID = @cid";
                    cmd.Parameters.AddWithValue("@gid", groupUUID);
                    cmd.Parameters.AddWithValue("@cid", capabilityUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGroupSharingRule>> GetCharacterGroupSharingRulesAsync(string groupUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<CharacterGroupSharingRule>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM CharacterGroupSharingRules WHERE GroupUUID = @gid";
                    cmd.Parameters.AddWithValue("@gid", groupUUID);
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<CharacterGroupSharingRule>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertCharacterGroupSharingRuleAsync(CharacterGroupSharingRule rule)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO CharacterGroupSharingRules (UUID, GroupUUID, DataType, EntityUUID)
                                       VALUES (@uuid, @gid, @dataType, @entityUUID)
                                       ON CONFLICT (UUID) DO UPDATE SET
                                       GroupUUID = EXCLUDED.GroupUUID, DataType = EXCLUDED.DataType, EntityUUID = EXCLUDED.EntityUUID";
                    cmd.Parameters.AddWithValue("@uuid", rule.UUID);
                    cmd.Parameters.AddWithValue("@gid", rule.GroupUUID);
                    cmd.Parameters.AddWithValue("@dataType", (object)rule.DataType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@entityUUID", (object)rule.EntityUUID ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteCharacterGroupSharingRuleAsync(string groupUUID, string ruleUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM CharacterGroupSharingRules WHERE UUID = @uuid AND GroupUUID = @gid";
                    cmd.Parameters.AddWithValue("@uuid", ruleUUID);
                    cmd.Parameters.AddWithValue("@gid", groupUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGranteePermissions>> GetCharacterGranteesAsync(string ownerCharacterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<CharacterGranteePermissions>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM CharacterGranteePermissions WHERE OwnerCharacterUUID = @cid";
                    cmd.Parameters.AddWithValue("@cid", ownerCharacterUUID);
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<CharacterGranteePermissions>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertCharacterGranteePermissionsAsync(CharacterGranteePermissions perms)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO CharacterGranteePermissions (OwnerCharacterUUID, GranteeType, GranteeUUID, GroupUUID, ClearanceLevelUUID)
                                       VALUES (@cid, @granteeType, @granteeUUID, @gid, @clid)
                                       ON CONFLICT (OwnerCharacterUUID, GranteeUUID) DO UPDATE SET
                                       GranteeType = EXCLUDED.GranteeType, GroupUUID = EXCLUDED.GroupUUID, ClearanceLevelUUID = EXCLUDED.ClearanceLevelUUID";
                    cmd.Parameters.AddWithValue("@cid", perms.OwnerCharacterUUID);
                    cmd.Parameters.AddWithValue("@granteeType", (int)perms.GranteeType);
                    cmd.Parameters.AddWithValue("@granteeUUID", perms.GranteeUUID);
                    cmd.Parameters.AddWithValue("@gid", (object)perms.GroupUUID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@clid", (object)perms.ClearanceLevelUUID ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteCharacterGranteePermissionsAsync(string ownerCharacterUUID, string granteeUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM CharacterGranteePermissions WHERE OwnerCharacterUUID = @cid AND GranteeUUID = @gid";
                    cmd.Parameters.AddWithValue("@cid", ownerCharacterUUID);
                    cmd.Parameters.AddWithValue("@gid", granteeUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGranteeCapability>> GetCharacterGranteeCapabilitiesAsync(string ownerCharacterUUID, string granteeUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<CharacterGranteeCapability>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM CharacterGranteeCapabilities WHERE OwnerCharacterUUID = @cid AND GranteeUUID = @gid";
                    cmd.Parameters.AddWithValue("@cid", ownerCharacterUUID);
                    cmd.Parameters.AddWithValue("@gid", granteeUUID);
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<CharacterGranteeCapability>)results;
            }));
        }

        /// <inheritdoc/>
        public Task AddCharacterGranteeCapabilityAsync(CharacterGranteeCapability item)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO CharacterGranteeCapabilities (OwnerCharacterUUID, GranteeUUID, CapabilityUUID)
                                       VALUES (@cid, @gid, @capId)
                                       ON CONFLICT (OwnerCharacterUUID, GranteeUUID, CapabilityUUID) DO NOTHING";
                    cmd.Parameters.AddWithValue("@cid", item.OwnerCharacterUUID);
                    cmd.Parameters.AddWithValue("@gid", item.GranteeUUID);
                    cmd.Parameters.AddWithValue("@capId", item.CapabilityUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RemoveCharacterGranteeCapabilityAsync(string ownerCharacterUUID, string granteeUUID, string capabilityUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM CharacterGranteeCapabilities WHERE OwnerCharacterUUID = @cid AND GranteeUUID = @gid AND CapabilityUUID = @capId";
                    cmd.Parameters.AddWithValue("@cid", ownerCharacterUUID);
                    cmd.Parameters.AddWithValue("@gid", granteeUUID);
                    cmd.Parameters.AddWithValue("@capId", capabilityUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Intel
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelComment>> GetIntelCommentsForTargetAsync(string targetCharacterUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<IntelComment>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM IntelComments WHERE TargetCharacterUUID = @tid";
                    cmd.Parameters.AddWithValue("@tid", targetCharacterUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadIntelComment(reader));
                        }
                    }
                }

                return (IReadOnlyList<IntelComment>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<IntelComment> GetIntelCommentAsync(string commentUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM IntelComments WHERE UUID = @uuid";
                    cmd.Parameters.AddWithValue("@uuid", commentUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return ReadIntelComment(reader);
                        }
                    }
                }

                return (IntelComment)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertIntelCommentAsync(IntelComment comment)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO IntelComments (UUID, TargetCharacterUUID, SubmitterCharacterUUID, Text, CreatedUtc)
                                       VALUES (@uuid, @target, @submitter, @text, @created)
                                       ON CONFLICT (UUID) DO UPDATE SET
                                       TargetCharacterUUID = EXCLUDED.TargetCharacterUUID, SubmitterCharacterUUID = EXCLUDED.SubmitterCharacterUUID,
                                       Text = EXCLUDED.Text, CreatedUtc = EXCLUDED.CreatedUtc";
                    cmd.Parameters.AddWithValue("@uuid", comment.UUID);
                    cmd.Parameters.AddWithValue("@target", comment.TargetCharacterUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@submitter", comment.SubmitterCharacterUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@text", comment.Text ?? string.Empty);
                    cmd.Parameters.AddWithValue("@created", comment.CreatedUtc.ToString("O"));
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteIntelCommentAsync(string commentUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM IntelComments WHERE UUID = @uuid";
                    cmd.Parameters.AddWithValue("@uuid", commentUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForCommentAsync(string commentUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<IntelCommentFactionShare>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM IntelCommentFactionShares WHERE IntelCommentUUID = @cid";
                    cmd.Parameters.AddWithValue("@cid", commentUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadIntelShare(reader));
                        }
                    }
                }

                return (IReadOnlyList<IntelCommentFactionShare>)results;
            }));
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForFactionAsync(string factionUUID)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<IntelCommentFactionShare>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM IntelCommentFactionShares WHERE FactionUUID = @fid";
                    cmd.Parameters.AddWithValue("@fid", factionUUID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(ReadIntelShare(reader));
                        }
                    }
                }

                return (IReadOnlyList<IntelCommentFactionShare>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertIntelShareAsync(IntelCommentFactionShare share)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO IntelCommentFactionShares (UUID, IntelCommentUUID, FactionUUID, ClassificationLevelUUID, ClassifiedByCharacterUUID, SharedUtc, ClassifiedUtc)
                                       VALUES (@uuid, @commentId, @fid, @clLevel, @classifiedBy, @shared, @classifiedUtc)
                                       ON CONFLICT (UUID) DO UPDATE SET
                                       IntelCommentUUID = EXCLUDED.IntelCommentUUID, FactionUUID = EXCLUDED.FactionUUID,
                                       ClassificationLevelUUID = EXCLUDED.ClassificationLevelUUID, ClassifiedByCharacterUUID = EXCLUDED.ClassifiedByCharacterUUID,
                                       SharedUtc = EXCLUDED.SharedUtc, ClassifiedUtc = EXCLUDED.ClassifiedUtc";
                    cmd.Parameters.AddWithValue("@uuid", share.UUID);
                    cmd.Parameters.AddWithValue("@commentId", share.IntelCommentUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@fid", share.FactionUUID ?? string.Empty);
                    cmd.Parameters.AddWithValue("@clLevel", (object)share.ClassificationLevelUUID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@classifiedBy", (object)share.ClassifiedByCharacterUUID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@shared", share.SharedUtc.ToString("O"));
                    cmd.Parameters.AddWithValue("@classifiedUtc", share.ClassifiedUtc.HasValue ? (object)share.ClassifiedUtc.Value.ToString("O") : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteIntelShareAsync(string shareUUID)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM IntelCommentFactionShares WHERE UUID = @uuid";
                    cmd.Parameters.AddWithValue("@uuid", shareUUID);
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Audit
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<PermissionAuditEntry>> GetPermissionAuditEntriesAsync(DateTime? startDate = null, DateTime? endDate = null, PermissionActionType? actionType = null, string actorUUID = null, string targetUUID = null)
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
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

                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<PermissionAuditEntry>)results;
            }));
        }

        /// <inheritdoc/>
        public Task AppendPermissionAuditEntryAsync(PermissionAuditEntry entry)
        {
            _retryPolicy.Execute(() =>
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
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteExpiredAuditEntriesAsync(DateTime cutoff)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM PermissionAuditEntries WHERE Timestamp < @cutoff";
                    cmd.Parameters.AddWithValue("@cutoff", cutoff.ToString("O"));
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Baseline / Global Lookup Data
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<BaselineGameConstants> GetBaselineGameConstantsAsync()
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM BaselineGameConstants WHERE Id = 1";
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new BaselineGameConstants
                            {
                                RefiningBaseRate = 25,
                                CommoditiesPerCycle = 10,
                                CommodityCycleSeconds = 600,
                                StructureCap = 65,
                                WorkerVolume = 50m,
                            };
                        }
                    }
                }

                return (BaselineGameConstants)null;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertBaselineGameConstantsAsync(BaselineGameConstants constants)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO BaselineGameConstants (Id, DataVersion, LastUpdatedUtc)
                                       VALUES (1, 1, @utc)
                                       ON CONFLICT (Id) DO UPDATE SET DataVersion = EXCLUDED.DataVersion, LastUpdatedUtc = EXCLUDED.LastUpdatedUtc";
                    cmd.Parameters.AddWithValue("@utc", SystemClock.UtcNow.ToString("O"));
                    cmd.ExecuteNonQuery();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<BlueprintType>> GetAllBlueprintTypesAsync()
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<BlueprintType>();
                using (var conn = OpenConnection())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM BlueprintTypes";
                        using (var reader = cmd.ExecuteReader())
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
                    }

                    foreach (var bt in results)
                    {
                        bt.Properties = LoadBlueprintTypeProperties(conn, bt.Name);
                        bt.ResearchableProperties = LoadBlueprintTypeResearchableProperties(conn, bt.Name);
                    }
                }

                return (IReadOnlyList<BlueprintType>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertBlueprintTypesAsync(IReadOnlyList<BlueprintType> types)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    using (var del = conn.CreateCommand())
                    {
                        del.Transaction = tx;
                        del.CommandText = "DELETE FROM BlueprintTypes";
                        del.ExecuteNonQuery();
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
                            cmd.ExecuteNonQuery();
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
                                    cmd.ExecuteNonQuery();
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
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    tx.Commit();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ShipClass>> GetAllShipClassesAsync()
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<ShipClass>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM ShipClasses";
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<ShipClass>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertShipClassesAsync(IReadOnlyList<ShipClass> classes)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    using (var del = conn.CreateCommand())
                    {
                        del.Transaction = tx;
                        del.CommandText = "DELETE FROM ShipClasses";
                        del.ExecuteNonQuery();
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
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<TechLevel>> GetAllTechLevelsAsync()
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<TechLevel>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM TechLevels";
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<TechLevel>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertTechLevelsAsync(IReadOnlyList<TechLevel> levels)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    using (var del = conn.CreateCommand())
                    {
                        del.Transaction = tx;
                        del.CommandText = "DELETE FROM TechLevels";
                        del.ExecuteNonQuery();
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
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<Commodity>> GetAllCommoditiesAsync()
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<Commodity>();
                using (var conn = OpenConnection())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM Commodities";
                        using (var reader = cmd.ExecuteReader())
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

                    foreach (var c in results)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT ResourceName, Quantity FROM CommodityResources WHERE CommodityName = @name";
                            cmd.Parameters.AddWithValue("@name", c.Name);
                            using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<Commodity>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertCommoditiesAsync(IReadOnlyList<Commodity> commodities)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    using (var del = conn.CreateCommand())
                    {
                        del.Transaction = tx;
                        del.CommandText = "DELETE FROM Commodities";
                        del.ExecuteNonQuery();
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
                            cmd.ExecuteNonQuery();
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
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    tx.Commit();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<RefiningRecipe>> GetAllRefiningRecipesAsync()
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<RefiningRecipe>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM RefiningRecipes";
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<RefiningRecipe>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertRefiningRecipesAsync(IReadOnlyList<RefiningRecipe> recipes)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    using (var del = conn.CreateCommand())
                    {
                        del.Transaction = tx;
                        del.CommandText = "DELETE FROM RefiningRecipes";
                        del.ExecuteNonQuery();
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
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ResearchTimeEntry>> GetAllResearchTimesAsync()
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<ResearchTimeEntry>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM ResearchTimes";
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<ResearchTimeEntry>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertResearchTimesAsync(IReadOnlyList<ResearchTimeEntry> entries)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    using (var del = conn.CreateCommand())
                    {
                        del.Transaction = tx;
                        del.CommandText = "DELETE FROM ResearchTimes";
                        del.ExecuteNonQuery();
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
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<PropertyTypeDefinition>> GetAllPropertyTypeDefinitionsAsync()
        {
            return Task.FromResult(_retryPolicy.Execute(() =>
            {
                var results = new List<PropertyTypeDefinition>();
                using (var conn = OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM PropertyTypeDefinitions";
                    using (var reader = cmd.ExecuteReader())
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

                return (IReadOnlyList<PropertyTypeDefinition>)results;
            }));
        }

        /// <inheritdoc/>
        public Task UpsertPropertyTypeDefinitionsAsync(IReadOnlyList<PropertyTypeDefinition> definitions)
        {
            _retryPolicy.Execute(() =>
            {
                using (var conn = OpenConnection())
                using (var tx = conn.BeginTransaction())
                {
                    using (var del = conn.CreateCommand())
                    {
                        del.Transaction = tx;
                        del.CommandText = "DELETE FROM PropertyTypeDefinitions";
                        del.ExecuteNonQuery();
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
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            });

            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════
        // Private Helpers
        // ═══════════════════════════════════════════════════════════

        private static int GetSchemaVersion(NpgsqlConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT \"Value\" FROM _metadata WHERE \"Key\" = 'schema_version';";
                var result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    return 0;
                }

                return int.TryParse(result.ToString(), out int v) ? v : 0;
            }
        }

        private static void SetSchemaVersion(NpgsqlConnection conn, int version)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO _metadata (\"Key\", \"Value\") VALUES ('schema_version', @v) ON CONFLICT (\"Key\") DO UPDATE SET \"Value\" = @v;";
                cmd.Parameters.AddWithValue("@v", version.ToString());
                cmd.ExecuteNonQuery();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // Private Helper Methods — Read
        // ═══════════════════════════════════════════════════════════

        private static ServerFaction ReadServerFaction(NpgsqlDataReader reader)
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

        private static List<string> ReadFactionLeaders(NpgsqlConnection conn, string factionUUID)
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

        private static ServerCharacter ReadServerCharacter(NpgsqlDataReader reader)
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

        private static ApiToken ReadApiToken(NpgsqlDataReader reader)
        {
            var token = new ApiToken
            {
                Id = reader.GetString(0),
                TokenHash = reader.GetString(1),
                Role = (TokenRole)reader.GetInt32(3),
                CreatedUtc = DateTime.Parse(reader.GetString(5)),
                IsRevoked = reader.GetBoolean(7),
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

        private static MembershipAction ReadMembershipAction(NpgsqlDataReader reader)
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

        private static SharingRule ReadSharingRule(NpgsqlDataReader reader)
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

        private static StarSystem ReadStarSystem(NpgsqlDataReader reader)
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
                HasOrbital = reader.GetBoolean(12),
                HasSpaceport = reader.GetBoolean(13),
                HasStarbase = reader.GetBoolean(14),
            };
        }

        private NpgsqlConnection OpenConnection()
        {
            var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        private void ExecuteSchema(NpgsqlConnection conn)
        {
            if (string.IsNullOrWhiteSpace(SchemaDdl))
            {
                Log.Debug("No schema DDL to execute (will be populated in tasks 7.2-7.3)");
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
