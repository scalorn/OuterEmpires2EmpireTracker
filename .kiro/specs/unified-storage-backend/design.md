# Technical Design: Unified Storage Backend

## Overview

This design defines the unified `IStorageBackend` interface and all five backend implementations (JSON single-file, JSON multi-file, SQLite, DynamoDB, Postgres) in OE2EmpireTracker.Common. The scope is limited to building the interface, backends, factory, error types, and supporting models in Common — ready for consumers to adopt in future work. No consumer integration (PlayerContext, EmpireContext, Server startup) or MigrationService is included here.

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     OE2EmpireTracker.Common (netstandard2.0)             │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │  IStorageBackend (Interfaces/IStorageBackend.cs)                   │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                              ▲                                          │
│         ┌────────────────────┼────────────────────┐                     │
│         │                    │                    │                     │
│  ┌──────┴──────┐  ┌─────────┴────────┐  ┌───────┴──────┐             │
│  │JSON Single  │  │  JSON Multi-File  │  │   SQLite     │             │
│  │File Backend │  │  Backend          │  │   Backend    │             │
│  └─────────────┘  └──────────────────┘  └──────────────┘             │
│                                                                         │
│  ┌─────────────┐  ┌──────────────────┐  ┌──────────────────────────┐  │
│  │  DynamoDB   │  │   Postgres       │  │  StorageBackendFactory   │  │
│  │  Backend    │  │   Backend        │  │                          │  │
│  └─────────────┘  └──────────────────┘  └──────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```


### Server Startup Flow (Consumer Usage Example)

This illustrates how a consumer WOULD use the factory to instantiate a backend:

```
Host builds
    → Read appsettings.json Storage section
    → StorageBackendFactory.CreateAsync(type, config)
        → backend.InitializeAsync()
    → Register IStorageBackend as singleton in DI
    → Endpoints inject IStorageBackend
```


## Components and Interfaces

### IStorageBackend Interface (Common/Interfaces/IStorageBackend.cs)

The unified interface is an exact copy of the current Server `IStorageBackend` plus a `GetStorageInfo()` method. Moves from `OE2EmpireTracker.Server.Storage` namespace to `OE2EmpireTracker.Common.Interfaces`.

```csharp
namespace OE2EmpireTracker.Common.Interfaces
{
    public interface IStorageBackend
    {
        // Lifecycle
        Task InitializeAsync(CancellationToken ct = default);
        Task<bool> ValidateConnectionAsync(CancellationToken ct = default);
        StorageInfo GetStorageInfo();

        // All existing methods from Server IStorageBackend preserved:
        // Factions, Characters, GlobalData, StarSystems, Tokens,
        // MembershipActions, SharingRules, CharacterPreferences,
        // Faction Permission Entities, Character Permission Entities,
        // Intel, Audit, and all 22 per-character entity types
        // (see requirements Req 1 for complete method enumeration)
    }

    public class StorageInfo
    {
        public string BackendType { get; set; }
        public string Location { get; set; }
    }
}
```

Design decisions:
- Interface is async Task-based. CancellationToken has a default value so callers not needing cancellation can omit it.
- Uses `IReadOnlyList<T>` for all collection returns to enforce immutability at the contract level.


### StorageBackendType Enum (Common/Interfaces/StorageBackendType.cs)

```csharp
namespace OE2EmpireTracker.Common.Interfaces
{
    public enum StorageBackendType
    {
        JsonSingleFile,
        JsonMultiFile,
        Sqlite,
        DynamoDb,
        Postgres
    }
}
```

### StorageBackendFactory (Common/Storage/StorageBackendFactory.cs)

```csharp
namespace OE2EmpireTracker.Common.Storage
{
    public static class StorageBackendFactory
    {
        public static async Task<IStorageBackend> CreateAsync(
            StorageBackendType type,
            StorageBackendConfig config,
            CancellationToken ct = default)
        {
            IStorageBackend backend = type switch
            {
                StorageBackendType.JsonSingleFile => new JsonSingleFileBackend(config),
                StorageBackendType.JsonMultiFile => new JsonMultiFileBackend(config),
                StorageBackendType.Sqlite => new SqliteBackend(config),
                StorageBackendType.DynamoDb => new DynamoDbBackend(config),
                StorageBackendType.Postgres => new PostgresBackend(config),
                _ => throw new ArgumentException($"Unsupported backend type: {type}")
            };

            await backend.InitializeAsync(ct);
            return backend;
        }
    }

    public class StorageBackendConfig
    {
        public string ConnectionString { get; set; }
        public string AwsRegion { get; set; }
        public string TablePrefix { get; set; }
    }
}
```


### JsonSingleFileBackend (Common/Storage/JsonSingleFileBackend.cs)

Provides IStorageBackend over the existing monolithic PlayerData.json format.

- Loads entire PlayerRoot on `InitializeAsync` into memory
- On write (Upsert/Delete), mutates in-memory state then atomically writes the full file
- Uses `SafeFileWriter.WriteAllText` for atomic writes with .bak backup
- Uses existing `JsonSettings.SerializerSettings` and `SerializationSorter` for format compatibility
- Produces byte-identical output to current `PlayerContext.WriteContext()`
- Applies `DataVersion` migrations when loading older files
- Empty/missing file returns empty collections (no exception)
- Malformed JSON throws `StorageLoadException`
- Server-global entities (ServerFaction, ApiToken, Permissions, etc.) throw `NotSupportedException` — WinForms does not need them

```csharp
namespace OE2EmpireTracker.Common.Storage
{
    internal class JsonSingleFileBackend : IStorageBackend
    {
        private readonly string _playerDataPath;
        private readonly string _baselineDataPath;
        private PlayerRoot _playerRoot;
        private BaselineRoot _baselineRoot;
        private readonly object _writeLock = new object();
    }
}
```

### JsonMultiFileBackend (Common/Storage/JsonMultiFileBackend.cs)

Provides IStorageBackend over the existing Server multi-file directory structure.

Directory layout (backward-compatible with existing server deployments):
```
{root}/
  factions/{uuid}.json
  characters/{uuid}.json
  tokens/{id}.json
  membershipActions/{id}.json
  global/{dataType}.json
  starSystems.json
  characters/{charUUID}/
    colonies.json
    blueprints.json
    surveys.json
    ... (one file per entity type)
  sharing/{charUUID}.json
  preferences/{charUUID}.json
  permissions/
    factions/{factionUUID}/capabilities.json, clearanceLevels.json, groups/, members/
    characters/{charUUID}/capabilities.json, clearanceLevels.json, groups/, grantees/
  intel/comments/{commentUUID}.json, shares/{factionUUID}.json
  audit/entries.json
```

- Each read: deserializes the specific file
- Each write: serializes to temp file, renames atomically
- Directory structure created on `InitializeAsync`


### SqliteBackend (Common/Storage/SqliteBackend.cs)

Provides IStorageBackend using a single SQLite database with a **fully normalized relational schema**. Every DTO property becomes a typed column. Every collection becomes a child table.

- Connection string: `Data Source={path}/oe2tracker.db`
- Journal mode: WAL (set on connection open via PRAGMA)
- Schema version tracked in `_metadata` table
- Uses `Microsoft.Data.Sqlite` NuGet package (netstandard2.0 compatible)
- Multi-entity writes use transactions for atomicity
- No JSON blobs — all data is stored in typed columns

**Schema Design Principles:**
- Each top-level entity type → one parent table with UUID as PK
- Each `List<T>` property → child table with FK to parent + Sequence column for ordering
- Each `Dictionary<K,V>` property → child table with FK to parent + Key column + value columns
- Each `PropertyBag` property → child table with (ParentUUID, Key, Value) columns
- Each `ItemBag` property → child table with one row per Item (all Item scalar fields as columns)
- Nested value objects without identity (e.g. CountDownTime) → inline columns with prefix on parent table
- Recursive structures (Item.Contents → ItemBag → Items) → self-referencing table with ParentItemUUID FK

### DynamoDbBackend (Common/Storage/DynamoDbBackend.cs)

Direct port of existing Server `DynamoStorageBackend`. Uses AWSSDK.DynamoDBv2. Table structure unchanged.

### PostgresBackend (Common/Storage/PostgresBackend.cs)

Uses the same fully normalized relational schema as SqliteBackend, expressed in PostgreSQL DDL. Uses Npgsql. Implements retry logic with exponential backoff via Polly before throwing exceptions.

- Schema identical to SQLite (same table names, same columns, same child tables)
- Uses PostgreSQL-native types: UUID, NUMERIC, TIMESTAMPTZ, TEXT, BOOLEAN
- Uses foreign key constraints with CASCADE DELETE
- Same _metadata table and numbered migration approach as SQLite

## Data Models

### Server Models (Common/Models/ServerModels.cs)

Moved from `OE2EmpireTracker.Server.Storage.Models.cs`. Contains:

| Class | Purpose |
|---|---|
| `ServerFaction` | Server-side faction with leadership tracking |
| `ServerCharacter` | Server-side character with metadata |
| `ApiToken` | API token stored server-side (hash only) |
| `MembershipAction` | Pending faction membership request or invitation |
| `SharingRule` | A single sharing rule granting access to data |
| `CharacterPreferences` | Character preferences stored server-side |
| `EntityMetadata` | Metadata attached to entities for conflict resolution |
| `ColonySummary` | Public colony summary (name + size only) |
| `RateLimitConfig` | Rate limit configuration per token |

Enums: `TokenRole`, `MembershipActionType`, `SharingTargetType`


### Permission Models (Common/Models/PermissionModels.cs)

Moved from `OE2EmpireTracker.Server.Storage.PermissionModels.cs`. Contains all permission-system entity classes:

- FactionCapability, FactionClearanceLevel, FactionPermissionGroup
- FactionGroupCapability, FactionGroupSharingRule
- FactionMemberPermissions, FactionMemberCapability
- CharacterCapability, CharacterClearanceLevel, CharacterPermissionGroup
- CharacterGroupCapability, CharacterGroupSharingRule
- CharacterGranteePermissions, CharacterGranteeCapability
- IntelComment, IntelCommentFactionShare
- PermissionAuditEntry, PermissionActionType (enum)

### PlayerRoot (existing, unchanged)

The serialization root for the single-file JSON format. Contains arrays for all 22 player entity types plus metadata (DataVersion, CurrentPlayerUUID, BankingBalance).

### BaselineRoot (existing, unchanged)

The serialization root for baseline/shared game data (blueprint types, ship classes, commodities, resources, tech levels, etc.).

### SQLite/Postgres Relational Schema (v1)

Every entity type has a proper table with typed columns. Collections are child tables. Examples shown below for key entities — all other entities follow the same pattern.

```sql
-- Metadata
CREATE TABLE _metadata (Key TEXT PRIMARY KEY, Value TEXT NOT NULL);

-- ═══════════════════════════════════════════════════════════════════
-- COLONY (parent table)
-- ═══════════════════════════════════════════════════════════════════
CREATE TABLE Colonies (
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

-- Colony child: Structures
CREATE TABLE ColonyStructures (
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
    -- Inline nested: BuildCompletionTime
    BuildCompletion_StartTime TEXT,
    BuildCompletion_RepeatIntervalSeconds INTEGER,
    BuildCompletion_IsRepeating INTEGER,
    -- Inline nested: ProcessCompletionTime
    ProcessCompletion_StartTime TEXT,
    ProcessCompletion_RepeatIntervalSeconds INTEGER,
    ProcessCompletion_IsRepeating INTEGER
);

-- ColonyStructure child: Properties (PropertyBag)
CREATE TABLE ColonyStructureProperties (
    StructureUUID TEXT NOT NULL REFERENCES ColonyStructures(UUID) ON DELETE CASCADE,
    Key TEXT NOT NULL,
    Value TEXT NOT NULL,
    PRIMARY KEY (StructureUUID, Key)
);

-- ColonyStructure child: AssignedWorkers (PropertyBag)
CREATE TABLE ColonyStructureWorkers (
    StructureUUID TEXT NOT NULL REFERENCES ColonyStructures(UUID) ON DELETE CASCADE,
    Key TEXT NOT NULL,
    Value TEXT NOT NULL,
    PRIMARY KEY (StructureUUID, Key)
);

-- Colony child: Items (ItemBag)
CREATE TABLE ColonyItems (
    UUID TEXT PRIMARY KEY,
    ColonyUUID TEXT NOT NULL REFERENCES Colonies(UUID) ON DELETE CASCADE,
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
    JobTrack TEXT NOT NULL DEFAULT '',
    ParentItemUUID TEXT  -- self-reference for nested Contents
);

-- ═══════════════════════════════════════════════════════════════════
-- BLUEPRINT
-- ═══════════════════════════════════════════════════════════════════
CREATE TABLE Blueprints (
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

-- Blueprint child: Properties (PropertyBag)
CREATE TABLE BlueprintProperties (
    BlueprintUUID TEXT NOT NULL REFERENCES Blueprints(UUID) ON DELETE CASCADE,
    Key TEXT NOT NULL,
    Value TEXT NOT NULL,
    PRIMARY KEY (BlueprintUUID, Key)
);

-- Blueprint child: Resources (Dictionary<string, string> in DTO, stored as integer)
CREATE TABLE BlueprintResources (
    BlueprintUUID TEXT NOT NULL REFERENCES Blueprints(UUID) ON DELETE CASCADE,
    ResourceName TEXT NOT NULL,
    Amount INTEGER NOT NULL,
    PRIMARY KEY (BlueprintUUID, ResourceName)
);

-- ═══════════════════════════════════════════════════════════════════
-- SURVEY
-- ═══════════════════════════════════════════════════════════════════
CREATE TABLE Surveys (
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

-- Survey child: Properties (Dictionary<string, string>)
CREATE TABLE SurveyProperties (
    SurveyUUID TEXT NOT NULL REFERENCES Surveys(UUID) ON DELETE CASCADE,
    Key TEXT NOT NULL,
    Value TEXT NOT NULL,
    PRIMARY KEY (SurveyUUID, Key)
);

-- Survey child: Resources (Dictionary<string, SurveyResource>, Amount is numeric yield rate)
CREATE TABLE SurveyResources (
    SurveyUUID TEXT NOT NULL REFERENCES Surveys(UUID) ON DELETE CASCADE,
    ResourceKey TEXT NOT NULL,
    Resource TEXT NOT NULL DEFAULT '',
    Purity TEXT NOT NULL DEFAULT '',
    Amount INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (SurveyUUID, ResourceKey)
);

-- ═══════════════════════════════════════════════════════════════════
-- DELIVERY ROUTE
-- ═══════════════════════════════════════════════════════════════════
CREATE TABLE DeliveryRoutes (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    OwnerUUID TEXT NOT NULL DEFAULT ''
);

-- DeliveryRoute child: Stops (List<RouteStop>)
CREATE TABLE DeliveryRouteStops (
    DeliveryRouteUUID TEXT NOT NULL REFERENCES DeliveryRoutes(UUID) ON DELETE CASCADE,
    Sequence INTEGER NOT NULL,
    ColonyUUID TEXT NOT NULL DEFAULT '',
    DestinationType TEXT NOT NULL DEFAULT 'Colony',
    DestinationUUID TEXT NOT NULL DEFAULT '',
    Purpose TEXT NOT NULL DEFAULT 'Cargo',
    FuelEstimate REAL NOT NULL DEFAULT 0,
    PRIMARY KEY (DeliveryRouteUUID, Sequence)
);

-- ═══════════════════════════════════════════════════════════════════
-- SHIP
-- ═══════════════════════════════════════════════════════════════════
CREATE TABLE Ships (
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

-- Ship child: Components (List<ShipComponentSlot>)
CREATE TABLE ShipComponents (
    ShipUUID TEXT NOT NULL REFERENCES Ships(UUID) ON DELETE CASCADE,
    Sequence INTEGER NOT NULL,
    SlotType TEXT NOT NULL DEFAULT '',
    BlueprintUUID TEXT NOT NULL DEFAULT '',
    CurrentHP INTEGER NOT NULL DEFAULT 0,
    MaxHP INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (ShipUUID, Sequence)
);

-- Ship child: Cargo (ItemBag) and Hopper (ItemBag) follow same pattern as ColonyItems
CREATE TABLE ShipCargoItems (
    UUID TEXT PRIMARY KEY,
    ShipUUID TEXT NOT NULL REFERENCES Ships(UUID) ON DELETE CASCADE,
    ItemType TEXT NOT NULL,
    BaseItemTypeID TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL DEFAULT '',
    Quantity INTEGER NOT NULL DEFAULT 0,
    ResourcePurity TEXT NOT NULL DEFAULT '',
    Volume REAL NOT NULL DEFAULT 0,
    ParentItemUUID TEXT
);

CREATE TABLE ShipHopperItems (
    UUID TEXT PRIMARY KEY,
    ShipUUID TEXT NOT NULL REFERENCES Ships(UUID) ON DELETE CASCADE,
    ItemType TEXT NOT NULL,
    BaseItemTypeID TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL DEFAULT '',
    Quantity INTEGER NOT NULL DEFAULT 0,
    ResourcePurity TEXT NOT NULL DEFAULT '',
    Volume REAL NOT NULL DEFAULT 0,
    ParentItemUUID TEXT
);

-- ═══════════════════════════════════════════════════════════════════
-- SERVER-GLOBAL ENTITIES
-- ═══════════════════════════════════════════════════════════════════
CREATE TABLE ServerFactions (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT '',
    Metadata_LastModifiedUtc TEXT,
    Metadata_ModifiedByTokenId TEXT
);

-- ServerFaction child: LeaderCharacterUUIDs (List<string>)
CREATE TABLE ServerFactionLeaders (
    FactionUUID TEXT NOT NULL REFERENCES ServerFactions(UUID) ON DELETE CASCADE,
    CharacterUUID TEXT NOT NULL,
    PRIMARY KEY (FactionUUID, CharacterUUID)
);

CREATE TABLE ServerCharacters (
    UUID TEXT PRIMARY KEY,
    Name TEXT NOT NULL DEFAULT '',
    FactionUUID TEXT,
    Metadata_LastModifiedUtc TEXT,
    Metadata_ModifiedByTokenId TEXT
);

CREATE TABLE ApiTokens (
    Id TEXT PRIMARY KEY,
    TokenHash TEXT NOT NULL DEFAULT '',
    CharacterUUID TEXT,
    Role TEXT NOT NULL DEFAULT 'Character',
    FactionUUID TEXT,
    CreatedUtc TEXT NOT NULL,
    LastUsedUtc TEXT,
    IsRevoked INTEGER NOT NULL DEFAULT 0,
    RateLimits_RequestsPerMinute INTEGER NOT NULL DEFAULT 300
);

CREATE TABLE MembershipActions (
    Id TEXT PRIMARY KEY,
    FactionUUID TEXT NOT NULL DEFAULT '',
    CharacterUUID TEXT NOT NULL DEFAULT '',
    Type TEXT NOT NULL DEFAULT 'JoinRequest',
    CreatedUtc TEXT NOT NULL,
    ExpiresUtc TEXT NOT NULL
);

CREATE TABLE StarSystems (
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

CREATE TABLE SharingRules (
    Id TEXT PRIMARY KEY,
    OwnerCharacterUUID TEXT NOT NULL DEFAULT '',
    TargetUUID TEXT NOT NULL DEFAULT '',
    TargetType TEXT NOT NULL DEFAULT 'Character',
    DataType TEXT,
    EntityUUID TEXT
);

CREATE TABLE CharacterPreferences (
    CharacterUUID TEXT PRIMARY KEY,
    ServerProcessing INTEGER NOT NULL DEFAULT 0
);

-- ═══════════════════════════════════════════════════════════════════
-- PERMISSION ENTITIES
-- ═══════════════════════════════════════════════════════════════════
CREATE TABLE FactionCapabilities (
    UUID TEXT PRIMARY KEY,
    FactionUUID TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT ''
);

CREATE TABLE FactionClearanceLevels (
    UUID TEXT PRIMARY KEY,
    FactionUUID TEXT NOT NULL DEFAULT '',
    Level INTEGER NOT NULL DEFAULT 0,
    Name TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT ''
);

CREATE TABLE FactionPermissionGroups (
    UUID TEXT PRIMARY KEY,
    FactionUUID TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT '',
    DefaultClearanceLevelUUID TEXT NOT NULL DEFAULT ''
);

CREATE TABLE FactionGroupCapabilities (
    GroupUUID TEXT NOT NULL,
    CapabilityUUID TEXT NOT NULL,
    PRIMARY KEY (GroupUUID, CapabilityUUID)
);

CREATE TABLE FactionGroupSharingRules (
    UUID TEXT PRIMARY KEY,
    GroupUUID TEXT NOT NULL DEFAULT '',
    DataType TEXT,
    EntityUUID TEXT,
    MinClearanceLevelUUID TEXT NOT NULL DEFAULT ''
);

CREATE TABLE FactionMemberPermissions (
    CharacterUUID TEXT NOT NULL,
    FactionUUID TEXT NOT NULL,
    GroupUUID TEXT,
    ClearanceLevelUUID TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (CharacterUUID, FactionUUID)
);

CREATE TABLE FactionMemberCapabilities (
    CharacterUUID TEXT NOT NULL,
    FactionUUID TEXT NOT NULL,
    CapabilityUUID TEXT NOT NULL,
    PRIMARY KEY (CharacterUUID, FactionUUID, CapabilityUUID)
);

CREATE TABLE CharacterCapabilities (
    UUID TEXT PRIMARY KEY,
    OwnerCharacterUUID TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT ''
);

CREATE TABLE CharacterClearanceLevels (
    UUID TEXT PRIMARY KEY,
    OwnerCharacterUUID TEXT NOT NULL DEFAULT '',
    Level INTEGER NOT NULL DEFAULT 0,
    Name TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT ''
);

CREATE TABLE CharacterPermissionGroups (
    UUID TEXT PRIMARY KEY,
    OwnerCharacterUUID TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT '',
    DefaultClearanceLevelUUID TEXT NOT NULL DEFAULT ''
);

CREATE TABLE CharacterGroupCapabilities (
    GroupUUID TEXT NOT NULL,
    CapabilityUUID TEXT NOT NULL,
    PRIMARY KEY (GroupUUID, CapabilityUUID)
);

CREATE TABLE CharacterGroupSharingRules (
    UUID TEXT PRIMARY KEY,
    GroupUUID TEXT NOT NULL DEFAULT '',
    DataType TEXT,
    EntityUUID TEXT
);

CREATE TABLE CharacterGranteePermissions (
    OwnerCharacterUUID TEXT NOT NULL,
    GranteeType TEXT NOT NULL DEFAULT 'Character',
    GranteeUUID TEXT NOT NULL,
    GroupUUID TEXT,
    ClearanceLevelUUID TEXT,
    PRIMARY KEY (OwnerCharacterUUID, GranteeUUID)
);

CREATE TABLE CharacterGranteeCapabilities (
    OwnerCharacterUUID TEXT NOT NULL,
    GranteeType TEXT NOT NULL DEFAULT 'Character',
    GranteeUUID TEXT NOT NULL,
    CapabilityUUID TEXT NOT NULL,
    PRIMARY KEY (OwnerCharacterUUID, GranteeUUID, CapabilityUUID)
);

-- ═══════════════════════════════════════════════════════════════════
-- INTEL AND AUDIT
-- ═══════════════════════════════════════════════════════════════════
CREATE TABLE IntelComments (
    UUID TEXT PRIMARY KEY,
    TargetCharacterUUID TEXT NOT NULL DEFAULT '',
    SubmitterCharacterUUID TEXT NOT NULL DEFAULT '',
    Text TEXT NOT NULL DEFAULT '',
    CreatedUtc TEXT NOT NULL
);

CREATE TABLE IntelCommentFactionShares (
    UUID TEXT PRIMARY KEY,
    IntelCommentUUID TEXT NOT NULL REFERENCES IntelComments(UUID) ON DELETE CASCADE,
    FactionUUID TEXT NOT NULL DEFAULT '',
    ClassificationLevelUUID TEXT,
    ClassifiedByCharacterUUID TEXT,
    SharedUtc TEXT NOT NULL,
    ClassifiedUtc TEXT
);

CREATE TABLE PermissionAuditEntries (
    UUID TEXT PRIMARY KEY,
    Timestamp TEXT NOT NULL,
    ActorCharacterUUID TEXT NOT NULL DEFAULT '',
    TargetCharacterUUID TEXT NOT NULL DEFAULT '',
    ActionType TEXT NOT NULL DEFAULT 'CapabilityGranted',
    OldValue TEXT NOT NULL DEFAULT '',
    NewValue TEXT NOT NULL DEFAULT ''
);

-- ═══════════════════════════════════════════════════════════════════
-- BASELINE / GLOBAL LOOKUP TABLES (no JSON blobs)
-- ═══════════════════════════════════════════════════════════════════
CREATE TABLE BaselineGameConstants (
    Key TEXT PRIMARY KEY,  -- singleton row with Key='default'
    RefiningBaseRate INTEGER NOT NULL DEFAULT 25,
    CommoditiesPerCycle INTEGER NOT NULL DEFAULT 10,
    CommodityCycleSeconds INTEGER NOT NULL DEFAULT 600,
    StructureCap INTEGER NOT NULL DEFAULT 65,
    WorkerVolume REAL NOT NULL DEFAULT 50
);

CREATE TABLE BlueprintTypes (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Universal INTEGER NOT NULL DEFAULT 0,
    IconPosition TEXT,
    OutputItemType TEXT NOT NULL DEFAULT ''
);

-- BlueprintType child: Properties (string array)
CREATE TABLE BlueprintTypeProperties (
    BlueprintTypeId TEXT NOT NULL REFERENCES BlueprintTypes(Id) ON DELETE CASCADE,
    PropertyName TEXT NOT NULL,
    Sequence INTEGER NOT NULL,
    PRIMARY KEY (BlueprintTypeId, Sequence)
);

-- BlueprintType child: ResearchableProperties (string array)
CREATE TABLE BlueprintTypeResearchableProperties (
    BlueprintTypeId TEXT NOT NULL REFERENCES BlueprintTypes(Id) ON DELETE CASCADE,
    PropertyName TEXT NOT NULL,
    Sequence INTEGER NOT NULL,
    PRIMARY KEY (BlueprintTypeId, Sequence)
);

CREATE TABLE ShipClasses (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL
);

CREATE TABLE TechLevels (
    Name TEXT PRIMARY KEY
);

CREATE TABLE Commodities (
    ID TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    CommodityIndustry TEXT NOT NULL,
    CommodityGroup TEXT NOT NULL
);

-- Commodity child: ConstructionResources (Dictionary<string, string> in DTO, stored as integer)
CREATE TABLE CommodityResources (
    CommodityID TEXT NOT NULL REFERENCES Commodities(ID) ON DELETE CASCADE,
    ResourceName TEXT NOT NULL,
    Amount INTEGER NOT NULL,
    PRIMARY KEY (CommodityID, ResourceName)
);

CREATE TABLE RefiningRecipes (
    InputResource TEXT NOT NULL,
    InputPurity TEXT NOT NULL,
    OutputResource TEXT NOT NULL,
    ConsumeRate INTEGER NOT NULL,
    ProduceRate INTEGER NOT NULL,
    Tier INTEGER NOT NULL,
    PRIMARY KEY (InputResource, InputPurity)
);

CREATE TABLE ResearchTimes (
    Evolution INTEGER PRIMARY KEY,
    ResearchTimeSeconds INTEGER NOT NULL
);

CREATE TABLE PropertyTypeDefinitions (
    Name TEXT PRIMARY KEY,
    DataType TEXT,
    Category TEXT
);

-- Global blueprints: same Blueprints table, distinguished by OwnerUUID being empty/null
-- (character-specific blueprints have OwnerUUID set to the character UUID)

-- ═══════════════════════════════════════════════════════════════════
-- PERMISSION ENTITIES (one table per type)
-- ═══════════════════════════════════════════════════════════════════
CREATE TABLE FactionCapabilities (UUID TEXT PRIMARY KEY, FactionUUID TEXT NOT NULL, ...);
CREATE TABLE FactionClearanceLevels (UUID TEXT PRIMARY KEY, FactionUUID TEXT NOT NULL, ...);
CREATE TABLE FactionPermissionGroups (UUID TEXT PRIMARY KEY, FactionUUID TEXT NOT NULL, ...);
-- ... (same pattern for all 14 permission entity types)

-- ═══════════════════════════════════════════════════════════════════
-- INTEL AND AUDIT
-- ═══════════════════════════════════════════════════════════════════
CREATE TABLE IntelComments (UUID TEXT PRIMARY KEY, TargetCharacterUUID TEXT, AuthorUUID TEXT, Content TEXT, CreatedUtc TEXT);
CREATE TABLE IntelCommentFactionShares (UUID TEXT PRIMARY KEY, CommentUUID TEXT REFERENCES IntelComments(UUID) ON DELETE CASCADE, FactionUUID TEXT);
CREATE TABLE PermissionAuditEntries (Id TEXT PRIMARY KEY, Timestamp TEXT NOT NULL, ActionType TEXT, ActorUUID TEXT, TargetUUID TEXT, Details TEXT);
```

**Design notes:**
- **No JSON blobs anywhere in SQL backends.** All data is stored in typed columns.
- CountDownTime is inlined as prefixed columns on the parent table (e.g. `BuildCompletion_StartTime`) rather than a separate table, because it has no independent identity.
- ItemBag items use a `ParentItemUUID` self-reference for recursive nesting (crate contents).
- All child tables use CASCADE DELETE so removing a parent automatically cleans up children.
- Global blueprints share the same Blueprints table as character-specific ones, distinguished by OwnerUUID being empty/null for global entries.
- BaselineGameConstants uses a singleton-row pattern (single row with Key='default').

## Error Handling

### Exception Hierarchy

```csharp
namespace OE2EmpireTracker.Common.Interfaces
{
    public class StorageLoadException : Exception
    {
        public string BackendType { get; }
        public string Location { get; }
    }

    public class StorageWriteException : Exception
    {
        public string BackendType { get; }
        public string Operation { get; }
    }

    public class StorageCorruptionException : Exception
    {
        public string BackendType { get; }
    }
}
```

### Error Handling by Backend Type

| Backend | Read Failure | Write Failure | Recovery Strategy |
|---|---|---|---|
| JsonSingleFile | StorageLoadException (malformed JSON, missing file returns empty) | StorageWriteException (I/O error) | Atomic write: original file untouched |
| JsonMultiFile | StorageLoadException per file | StorageWriteException per file | Atomic rename: temp file strategy |
| SQLite | StorageLoadException (corrupt DB, schema mismatch) | StorageWriteException (transaction rolled back) | Transaction rollback preserves prior state |
| DynamoDB | StorageLoadException (unreachable) | StorageWriteException (throttled/error) | Item-level atomicity |
| Postgres | StorageLoadException (unreachable, after retries) | StorageWriteException (after retries, transaction rolled back) | Polly retry + transaction rollback |

## Testing Strategy

### Unit Tests (per backend)

Each backend implementation gets a full test suite validating:
- Initialize creates expected storage structure
- CRUD operations for all entity types
- Empty/missing data returns empty collections
- Malformed data throws appropriate exceptions
- Atomic write safety (simulate failures)

### Integration Tests

- Server integration tests pass against Common backends (namespace change only)
- Existing server test suite verifies backward compatibility of moved backends


### Property Tests (FsCheck 2.16.6)

Property tests validate universal invariants across all backends using generated entity data.

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Entity Count Preservation

*For any* set of N entities written to any backend via Upsert operations, reading all entities of that type SHALL return exactly N entities with the same UUIDs.

**Validates: Requirements 2.1, 2.8, 3.1, 4.1, 4.2, 5.1, 6.1**

### Property 2: Concurrent Read Safety

*For any* backend and any set of stored entities, multiple concurrent read operations SHALL return consistent results (no partial reads, no corruption). Writes are serialized by the caller.

**Validates: Requirements 4.4, 4.8**

### Property 3: Atomic Write Safety

*For any* backend, if a write operation fails (exception thrown), the previous state SHALL be preserved. JSON backends use temp-file strategy, SQLite and Postgres use transaction rollback, DynamoDB item writes are individually atomic.

**Validates: Requirements 2.3, 3.4, 4.8, 10.3, 10.4**


## NuGet Dependencies (New for Common)

| Package | Version | Purpose |
|---|---|---|
| Microsoft.Data.Sqlite | 8.0.x | SQLite backend (netstandard2.0 compatible) |
| AWSSDK.DynamoDBv2 | 3.7.x | DynamoDB backend |
| Npgsql | 8.0.x | Postgres backend |

All backends ship in Common (Option A). Size increase is acceptable. No conditional compilation needed.

## Server Project Migration

What moves to Common:
- `Storage/IStorageBackend.cs` → `Common/Interfaces/IStorageBackend.cs`
- `Storage/Models.cs` → `Common/Models/ServerModels.cs`
- `Storage/PermissionModels.cs` → `Common/Models/PermissionModels.cs`
- `Storage/JsonFileStorageBackend.cs` → `Common/Storage/JsonMultiFileBackend.cs`
- `Storage/SqliteStorageBackend.cs` → `Common/Storage/SqliteBackend.cs`
- `Storage/DynamoStorageBackend.cs` → `Common/Storage/DynamoDbBackend.cs`
- `Storage/PostgresStorageBackend.cs` → `Common/Storage/PostgresBackend.cs`

Server project after migration: references Common, uses StorageBackendFactory in Program.cs, local Storage/ folder deleted entirely.

## File Layout (After Implementation)

```
OE2EmpireTracker.Common/
  Interfaces/
    IStorageBackend.cs
    StorageBackendType.cs
    StorageExceptions.cs
  Storage/
    JsonSingleFileBackend.cs
    JsonMultiFileBackend.cs
    SqliteBackend.cs
    DynamoDbBackend.cs
    PostgresBackend.cs
    StorageBackendFactory.cs
    StorageBackendConfig.cs
  Models/
    ServerModels.cs
    PermissionModels.cs
```


## Design Decisions

| Decision | Rationale |
|---|---|
| Fully normalized relational schema in SQLite/Postgres | Enables field-level queries, indexing, partial updates, and proper SQL operations. No JSON blobs anywhere in SQL backends. |
| Global blueprints in same Blueprints table | Distinguished by OwnerUUID being empty/null. Avoids duplicate schema for the same entity shape. |
| No ORM | netstandard2.0 constraint eliminates EF Core. Raw ADO.NET via Microsoft.Data.Sqlite is simpler and gives full control over schema. |
| Full-file rewrite for JsonSingleFile | Inherent limitation of the format. Per-entity Upsert still rewrites the file but the interface is uniform. |
| Server-global entities throw on JsonSingleFile | Desktop does not need them. Keeps single-file format unchanged. |
| All backends in Common (not separate assemblies) | Simpler build, single dependency graph. Size is acceptable. |
| Polly retry only for Postgres | Network backends benefit from retry. File-based and in-process SQLite do not have transient network failures. |
| CountDownTime inlined as prefixed columns | Value object with no independent identity. Separate table adds JOIN overhead for no benefit. |
| ItemBag uses ParentItemUUID self-reference | Handles recursive crate-contents nesting without infinite child tables. |
| Child tables use CASCADE DELETE | Simplifies parent deletion — no manual cleanup of child rows needed. |
| Numbered schema migrations | Each DTO field addition becomes an ALTER TABLE + default value. Predictable, auditable, reversible. |

## Implementation Phases

### Phase 1: Interface and Backends (No Behavior Change)

1. Create Common/Interfaces/IStorageBackend.cs, StorageBackendType.cs, StorageExceptions.cs
2. Move Server/Storage/Models.cs and PermissionModels.cs to Common/Models/
3. Move all four Server backend implementations to Common/Storage/
4. Create JsonSingleFileBackend (new)
5. Create StorageBackendFactory and StorageBackendConfig
6. Update Server to reference Common backends (namespace changes)
7. Delete Server local Storage/IStorageBackend.cs
8. All existing Server tests pass
