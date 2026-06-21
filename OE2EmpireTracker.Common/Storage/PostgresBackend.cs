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
    internal class PostgresBackend : IStorageBackend
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
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ServerFaction>> GetAllFactionsAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertFactionAsync(ServerFaction faction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteFactionAsync(string uuid)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Server Characters
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<ServerCharacter> GetCharacterAsync(string uuid)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ServerCharacter>> GetAllCharactersAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertCharacterAsync(ServerCharacter character)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteCharacterAsync(string uuid)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Global Data
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<string> GetGlobalDataAsync(string dataType)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertGlobalDataAsync(string dataType, string json)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Star Systems
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<StarSystem>> GetAllStarSystemsAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertStarSystemsAsync(IReadOnlyList<StarSystem> systems)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Colony Summaries
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<ColonySummary>> GetColonySummariesForSystemAsync(int systemId)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // API Tokens
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<ApiToken> FindTokenByHashAsync(string tokenHash)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ApiToken>> GetAllTokensAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertTokenAsync(ApiToken token)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteTokenAsync(string id)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Membership Actions
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<MembershipAction>> GetFactionActionsAsync(string factionUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertMembershipActionAsync(MembershipAction action)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteMembershipActionAsync(string id)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteExpiredActionsAsync(DateTime cutoff)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Sharing Rules
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<SharingRule>> GetSharingRulesForCharacterAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertSharingRulesAsync(string characterUUID, IReadOnlyList<SharingRule> rules)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Character Preferences
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<CharacterPreferences> GetCharacterPreferencesAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertCharacterPreferencesAsync(CharacterPreferences prefs)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Colony
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Colony>> GetAllColoniesAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Colony> GetColonyAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertColonyAsync(string characterUUID, Colony entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteColonyAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Blueprint
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Blueprint>> GetAllBlueprintsAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Blueprint> GetBlueprintAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertBlueprintAsync(string characterUUID, Blueprint entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteBlueprintAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Survey
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Survey>> GetAllSurveysAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Survey> GetSurveyAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertSurveyAsync(string characterUUID, Survey entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteSurveyAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — PlayerProfile
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<PlayerProfile>> GetAllPlayerProfilesAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<PlayerProfile> GetPlayerProfileAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertPlayerProfileAsync(string characterUUID, PlayerProfile entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeletePlayerProfileAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — DeliveryRoute
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<DeliveryRoute>> GetAllDeliveryRoutesAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<DeliveryRoute> GetDeliveryRouteAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertDeliveryRouteAsync(string characterUUID, DeliveryRoute entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteDeliveryRouteAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — DeliveryPlan
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<DeliveryPlan>> GetAllDeliveryPlansAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<DeliveryPlan> GetDeliveryPlanAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertDeliveryPlanAsync(string characterUUID, DeliveryPlan entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteDeliveryPlanAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Ship
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Ship>> GetAllShipsAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Ship> GetShipAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertShipAsync(string characterUUID, Ship entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteShipAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — ShipTemplate
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<ShipTemplate>> GetAllShipTemplatesAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<ShipTemplate> GetShipTemplateAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertShipTemplateAsync(string characterUUID, ShipTemplate entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteShipTemplateAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — MarketListing
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<MarketListing>> GetAllMarketListingsAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<MarketListing> GetMarketListingAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertMarketListingAsync(string characterUUID, MarketListing entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteMarketListingAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — MarketTransaction
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<MarketTransaction>> GetAllMarketTransactionsAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<MarketTransaction> GetMarketTransactionAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertMarketTransactionAsync(string characterUUID, MarketTransaction entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteMarketTransactionAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — PricingPlan
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<PricingPlan>> GetAllPricingPlansAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<PricingPlan> GetPricingPlanAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertPricingPlanAsync(string characterUUID, PricingPlan entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeletePricingPlanAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — StockPlan
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<StockPlan>> GetAllStockPlansAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<StockPlan> GetStockPlanAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertStockPlanAsync(string characterUUID, StockPlan entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteStockPlanAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — StockProfile
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<StockProfile>> GetAllStockProfilesAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<StockProfile> GetStockProfileAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertStockProfileAsync(string characterUUID, StockProfile entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteStockProfileAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — BuildPlan
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<BuildPlan>> GetAllBuildPlansAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<BuildPlan> GetBuildPlanAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertBuildPlanAsync(string characterUUID, BuildPlan entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteBuildPlanAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — SupplyChain
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<SupplyChain>> GetAllSupplyChainsAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<SupplyChain> GetSupplyChainAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertSupplyChainAsync(string characterUUID, SupplyChain entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteSupplyChainAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Asteroid
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Asteroid>> GetAllAsteroidsAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Asteroid> GetAsteroidAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertAsteroidAsync(string characterUUID, Asteroid entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteAsteroidAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Station
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Station>> GetAllStationsAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Station> GetStationAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertStationAsync(string characterUUID, Station entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteStationAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — Faction (contacts)
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<Faction>> GetAllFactionsForCharacterAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Faction> GetFactionForCharacterAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertFactionForCharacterAsync(string characterUUID, Faction entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteFactionForCharacterAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — ExternalCharacter
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<ExternalCharacter>> GetAllExternalCharactersAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<ExternalCharacter> GetExternalCharacterAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertExternalCharacterAsync(string characterUUID, ExternalCharacter entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteExternalCharacterAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — WarehouseOverflowRule
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<WarehouseOverflowRule>> GetAllWarehouseOverflowRulesAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<WarehouseOverflowRule> GetWarehouseOverflowRuleAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertWarehouseOverflowRuleAsync(string characterUUID, WarehouseOverflowRule entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteWarehouseOverflowRuleAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — MailMessage
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<MailMessage>> GetAllMailMessagesAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<MailMessage> GetMailMessageAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertMailMessageAsync(string characterUUID, MailMessage entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteMailMessageAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Per-Character Entity CRUD — BankingTransaction
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<BankingTransaction>> GetAllBankingTransactionsAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<BankingTransaction> GetBankingTransactionAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertBankingTransactionAsync(string characterUUID, BankingTransaction entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteBankingTransactionAsync(string characterUUID, string entityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Faction Permission Entities
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionCapability>> GetFactionCapabilitiesAsync(string factionUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertFactionCapabilityAsync(FactionCapability capability)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteFactionCapabilityAsync(string factionUUID, string capabilityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionClearanceLevel>> GetFactionClearanceLevelsAsync(string factionUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertFactionClearanceLevelAsync(FactionClearanceLevel level)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteFactionClearanceLevelAsync(string factionUUID, string levelUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionPermissionGroup>> GetFactionGroupsAsync(string factionUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<FactionPermissionGroup> GetFactionGroupAsync(string factionUUID, string groupUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertFactionGroupAsync(FactionPermissionGroup group)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteFactionGroupAsync(string factionUUID, string groupUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionGroupCapability>> GetFactionGroupCapabilitiesAsync(string groupUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task AddFactionGroupCapabilityAsync(FactionGroupCapability item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task RemoveFactionGroupCapabilityAsync(string groupUUID, string capabilityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionGroupSharingRule>> GetFactionGroupSharingRulesAsync(string groupUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertFactionGroupSharingRuleAsync(FactionGroupSharingRule rule)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteFactionGroupSharingRuleAsync(string groupUUID, string ruleUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<FactionMemberPermissions> GetFactionMemberPermissionsAsync(string factionUUID, string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertFactionMemberPermissionsAsync(FactionMemberPermissions perms)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionMemberPermissions>> GetAllFactionMembersPermissionsAsync(string factionUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<FactionMemberCapability>> GetFactionMemberCapabilitiesAsync(string factionUUID, string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task AddFactionMemberCapabilityAsync(FactionMemberCapability item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task RemoveFactionMemberCapabilityAsync(string factionUUID, string characterUUID, string capabilityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Character Permission Entities
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterCapability>> GetCharacterCapabilitiesAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertCharacterCapabilityAsync(CharacterCapability capability)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteCharacterCapabilityAsync(string characterUUID, string capabilityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterClearanceLevel>> GetCharacterClearanceLevelsAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertCharacterClearanceLevelAsync(CharacterClearanceLevel level)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteCharacterClearanceLevelAsync(string characterUUID, string levelUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterPermissionGroup>> GetCharacterGroupsAsync(string characterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<CharacterPermissionGroup> GetCharacterGroupAsync(string characterUUID, string groupUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertCharacterGroupAsync(CharacterPermissionGroup group)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteCharacterGroupAsync(string characterUUID, string groupUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGroupCapability>> GetCharacterGroupCapabilitiesAsync(string groupUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task AddCharacterGroupCapabilityAsync(CharacterGroupCapability item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task RemoveCharacterGroupCapabilityAsync(string groupUUID, string capabilityUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGroupSharingRule>> GetCharacterGroupSharingRulesAsync(string groupUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertCharacterGroupSharingRuleAsync(CharacterGroupSharingRule rule)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteCharacterGroupSharingRuleAsync(string groupUUID, string ruleUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGranteePermissions>> GetCharacterGranteesAsync(string ownerCharacterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertCharacterGranteePermissionsAsync(CharacterGranteePermissions perms)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteCharacterGranteePermissionsAsync(string ownerCharacterUUID, string granteeUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<CharacterGranteeCapability>> GetCharacterGranteeCapabilitiesAsync(string ownerCharacterUUID, string granteeUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task AddCharacterGranteeCapabilityAsync(CharacterGranteeCapability item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task RemoveCharacterGranteeCapabilityAsync(string ownerCharacterUUID, string granteeUUID, string capabilityUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Intel
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelComment>> GetIntelCommentsForTargetAsync(string targetCharacterUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IntelComment> GetIntelCommentAsync(string commentUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertIntelCommentAsync(IntelComment comment)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteIntelCommentAsync(string commentUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForCommentAsync(string commentUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<IntelCommentFactionShare>> GetIntelSharesForFactionAsync(string factionUUID)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertIntelShareAsync(IntelCommentFactionShare share)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteIntelShareAsync(string shareUUID)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Audit
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<IReadOnlyList<PermissionAuditEntry>> GetPermissionAuditEntriesAsync(DateTime? startDate = null, DateTime? endDate = null, PermissionActionType? actionType = null, string actorUUID = null, string targetUUID = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task AppendPermissionAuditEntryAsync(PermissionAuditEntry entry)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteExpiredAuditEntriesAsync(DateTime cutoff)
        {
            throw new NotImplementedException();
        }

        // ═══════════════════════════════════════════════════════════
        // Baseline / Global Lookup Data
        // ═══════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public Task<BaselineGameConstants> GetBaselineGameConstantsAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertBaselineGameConstantsAsync(BaselineGameConstants constants)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<BlueprintType>> GetAllBlueprintTypesAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertBlueprintTypesAsync(IReadOnlyList<BlueprintType> types)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ShipClass>> GetAllShipClassesAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertShipClassesAsync(IReadOnlyList<ShipClass> classes)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<TechLevel>> GetAllTechLevelsAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertTechLevelsAsync(IReadOnlyList<TechLevel> levels)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<Commodity>> GetAllCommoditiesAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertCommoditiesAsync(IReadOnlyList<Commodity> commodities)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<RefiningRecipe>> GetAllRefiningRecipesAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertRefiningRecipesAsync(IReadOnlyList<RefiningRecipe> recipes)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<ResearchTimeEntry>> GetAllResearchTimesAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertResearchTimesAsync(IReadOnlyList<ResearchTimeEntry> entries)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<PropertyTypeDefinition>> GetAllPropertyTypeDefinitionsAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpsertPropertyTypeDefinitionsAsync(IReadOnlyList<PropertyTypeDefinition> definitions)
        {
            throw new NotImplementedException();
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
