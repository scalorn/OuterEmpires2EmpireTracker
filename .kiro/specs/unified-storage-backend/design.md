# Technical Design: Unified Storage Backend

## Overview

This design unifies all storage behind a single shared `IStorageBackend` interface in OE2EmpireTracker.Common. All backend implementations (JSON single-file, JSON multi-file, SQLite, DynamoDB, Postgres) move into Common. All applications — WinForms Desktop, the Faction Server, and any future apps — consume the same interface and can use any backend interchangeably. A MigrationService allows lossless data movement between any pair of backends.

## Architecture

```
┌─────────────────────┐  ┌──────────────────────┐  ┌────────────────────┐
│  WinForms Desktop   │  │   Faction Server     │  │  Future Apps       │
│  (.NET Fx 4.8.1)    │  │   (.NET 8)           │  │  (.NET 8+)        │
└────────┬────────────┘  └──────────┬───────────┘  └─────────┬──────────┘
         │                          │                         │
         ▼                          ▼                         ▼
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
│  │  Backend    │  │   Backend        │  │  MigrationService        │  │
│  └─────────────┘  └──────────────────┘  └──────────────────────────┘  │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │  PlayerContext / EmpireContext (existing, refactored)              │  │
│  │  - Accepts IStorageBackend for persistence                        │  │
│  │  - In-memory cache + change events unchanged                      │  │
│  └───────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

### Desktop App Startup Flow

```
User launches app
    → PreferencesStore.Load()
    → Read StorageBackendType + StoragePath
    → StorageBackendFactory.CreateAsync(type, config)
        → backend.InitializeAsync()
    → EmpireContext.SetStorageBackend(backend)
    → EmpireContext.LoadAsync()
    → PlayerContext.SetStorageBackend(backend)
    → PlayerContext.LoadAsync(currentPlayerUUID)
    → MainWindow displays data
```

### Server Startup Flow

```
Host builds
    → Read appsettings.json Storage section
    → StorageBackendFactory.CreateAsync(type, config)
        → backend.InitializeAsync()
    → Register IStorageBackend as singleton in DI
    → Endpoints inject IStorageBackend
```

### Data Write Flow (Desktop)

```
User edits colony in form
    → ColonyService.UpdateStructure(...)
    → PlayerContext.MarkDirty(colonyUUID)
    → PlayerContext.WriteContext()
        → backend.UpsertColonyAsync(charUUID, colony)
        → (JsonSingleFile: rewrites full file)
        → (SQLite: UPDATE CharacterEntities SET Data=... WHERE ...)
    → PlayerContext.OnColonyDataChanged(colonyUUID)
```

### Backend Migration Flow

```
User chooses "Switch to SQLite" in Preferences
    → MigrationService.MigrateAsync(currentBackend, newSqliteBackend)
        → For each entity type:
            → Read from source
            → Write to destination
            → Report progress via ProgressChanged event
        → Validate counts match
    → PreferencesStore.StorageBackendType = Sqlite
    → PreferencesStore.StoragePath = newPath
    → Restart PlayerContext with new backend
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
- Interface remains async Task-based despite WinForms being synchronous — the Desktop app calls `.GetAwaiter().GetResult()` on synchronous code paths.
- CancellationToken has a default value so callers not needing cancellation can omit it.
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

Provides IStorageBackend using a single SQLite database with JSON-blob storage per entity.

- Connection string: `Data Source={path}/oe2tracker.db`
- Journal mode: WAL (set on connection open via PRAGMA)
- Schema version tracked in `_metadata` table
- Uses `Microsoft.Data.Sqlite` NuGet package (netstandard2.0 compatible)
- Multi-entity writes use transactions for atomicity

### DynamoDbBackend (Common/Storage/DynamoDbBackend.cs)

Direct port of existing Server `DynamoStorageBackend`. Uses AWSSDK.DynamoDBv2. Table structure unchanged.

### PostgresBackend (Common/Storage/PostgresBackend.cs)

Direct port of existing Server `PostgresStorageBackend`. Uses Npgsql. Implements retry logic with exponential backoff via Polly before throwing exceptions.

### MigrationService (Common/Services/MigrationService.cs)

Transfers all data from any source IStorageBackend to any destination IStorageBackend.

```csharp
namespace OE2EmpireTracker.Common.Services
{
    public class MigrationService
    {
        public event EventHandler<MigrationProgressEventArgs> ProgressChanged;

        public async Task MigrateAsync(
            IStorageBackend source,
            IStorageBackend destination,
            CancellationToken ct = default);
    }

    public class MigrationProgressEventArgs : EventArgs
    {
        public string CurrentEntityType { get; set; }
        public int EntitiesProcessed { get; set; }
        public int TotalEntityTypes { get; set; }
        public int CurrentEntityTypeIndex { get; set; }
    }
}
```

Migration order: GlobalData → ServerFactions → ServerCharacters → ApiTokens → MembershipActions → StarSystems → Per-character entities (22 types) → SharingRules → CharacterPreferences → Faction permissions → Character permissions → Intel → Audit.

Post-migration validation compares source vs destination entity counts. Mismatch throws `MigrationValidationException`.

### PlayerContext Refactoring

Current state: Reads/writes a single JSON file directly via `File.ReadAllText` / `SafeFileWriter.WriteAllText`.

Target state: Accepts `IStorageBackend` instance and delegates all persistence.

- `SetStorageBackend(IStorageBackend)` — configures the backend
- `LoadAsync(string characterUUID)` — loads all per-character data from backend
- `WriteContext()` — persists dirty entities to backend (incremental writes)
- `MarkDirty(string entityUUID)` — service methods mark entities before WriteContext
- Parameterless constructor preserved (creates JsonSingleFileBackend internally for backward compatibility)
- `GetInstance()` singleton pattern preserved
- `IsServerOnlyMode`, `PushToServer` delegates remain for transition period

### EmpireContext Refactoring

- `SetStorageBackend(IStorageBackend)` — configures the backend
- `LoadAsync()` — loads baseline data via `GetGlobalDataAsync("BaselineData")`
- `WriteContext()` — persists via `UpsertGlobalDataAsync("BaselineData", json)`

### PreferencesStore Integration

```csharp
[JsonProperty("storageBackendType")]
[DefaultValue(StorageBackendType.JsonSingleFile)]
public StorageBackendType StorageBackendType { get; set; } = StorageBackendType.JsonSingleFile;

[JsonProperty("storagePath")]
public string StoragePath { get; set; } = string.Empty;
```

Default StoragePath: application directory for JSON backends, `%LocalAppData%/OE2EmpireTracker` for SQLite.

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

### SQLite Schema (v1)

```sql
CREATE TABLE _metadata (Key TEXT PRIMARY KEY, Value TEXT NOT NULL);
CREATE TABLE Factions (UUID TEXT PRIMARY KEY, Data TEXT NOT NULL);
CREATE TABLE Characters (UUID TEXT PRIMARY KEY, Data TEXT NOT NULL);
CREATE TABLE Tokens (Id TEXT PRIMARY KEY, Data TEXT NOT NULL);
CREATE TABLE MembershipActions (Id TEXT PRIMARY KEY, FactionUUID TEXT, ExpiresUtc TEXT, Data TEXT NOT NULL);
CREATE TABLE GlobalData (DataType TEXT PRIMARY KEY, Data TEXT NOT NULL);
CREATE TABLE StarSystems (Id INTEGER PRIMARY KEY, Data TEXT NOT NULL);
CREATE TABLE SharingRules (CharacterUUID TEXT PRIMARY KEY, Data TEXT NOT NULL);
CREATE TABLE CharacterPreferences (CharacterUUID TEXT PRIMARY KEY, Data TEXT NOT NULL);
CREATE TABLE CharacterEntities (
    CharacterUUID TEXT NOT NULL,
    EntityType TEXT NOT NULL,
    EntityUUID TEXT NOT NULL,
    Data TEXT NOT NULL,
    PRIMARY KEY (CharacterUUID, EntityType, EntityUUID)
);
CREATE TABLE FactionPermissions (
    FactionUUID TEXT NOT NULL,
    EntityType TEXT NOT NULL,
    EntityUUID TEXT NOT NULL,
    Data TEXT NOT NULL,
    PRIMARY KEY (FactionUUID, EntityType, EntityUUID)
);
CREATE TABLE CharacterPermissions (
    CharacterUUID TEXT NOT NULL,
    EntityType TEXT NOT NULL,
    EntityUUID TEXT NOT NULL,
    Data TEXT NOT NULL,
    PRIMARY KEY (CharacterUUID, EntityType, EntityUUID)
);
CREATE TABLE IntelComments (UUID TEXT PRIMARY KEY, TargetCharacterUUID TEXT, Data TEXT NOT NULL);
CREATE TABLE IntelShares (UUID TEXT PRIMARY KEY, CommentUUID TEXT, FactionUUID TEXT, Data TEXT NOT NULL);
CREATE TABLE PermissionAuditEntries (
    Id TEXT PRIMARY KEY,
    Timestamp TEXT NOT NULL,
    ActionType TEXT,
    ActorUUID TEXT,
    TargetUUID TEXT,
    Data TEXT NOT NULL
);
```

EntityType discriminator strings: Colony, Blueprint, Survey, PlayerProfile, DeliveryRoute, DeliveryPlan, Ship, ShipTemplate, MarketListing, MarketTransaction, PricingPlan, StockPlan, StockProfile, BuildPlan, SupplyChain, Asteroid, Station, Faction, ExternalCharacter

Schema migration approach:
- `InitializeAsync` reads `schema_version` from `_metadata`
- Compares to `CURRENT_SCHEMA_VERSION` constant
- Applies sequential migration functions in transactions
- On failure: rollback and throw `StorageLoadException`
- If rollback itself fails: throw `StorageCorruptionException`

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

    public class MigrationValidationException : Exception
    {
        public IReadOnlyDictionary<string, (int Source, int Destination)> Mismatches { get; }
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

### PlayerContext Error Handling

- When IStorageBackend throws `StorageWriteException`, PlayerContext sets `WritesBlocked = true` and logs the error
- PlayerContext can also set `WritesBlocked = true` independently for application-level locking
- `InvalidOperationException` thrown if data operations attempted without a configured backend

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
- WinForms tests pass with JsonSingleFileBackend producing identical output

### Property Tests (FsCheck 2.16.6)

Round-trip fidelity tests using generated entity data migrated through backend pairs.

## Correctness Properties

### Property 1: Round-Trip Fidelity

**Validates: Requirements 11.1**

For any valid PlayerRoot P and any two backends A and B: `write(A, P) -> read(A) -> write(B) -> read(B) -> write(A) -> read(A) == P`. Tested via FsCheck generators producing random PlayerRoot instances, migrating through all backend pairs, and asserting structural equality.

### Property 2: Entity Count Preservation

**Validates: Requirements 10.6**

For any set of N entities written to a backend, reading all entities of that type returns exactly N entities with the same UUIDs.

### Property 3: Concurrent Read Safety

**Validates: Requirements 14.6**

Multiple concurrent reads on any backend return consistent results (no partial reads, no corruption). Writes are serialized by the caller (PlayerContext lock).

### Property 4: Atomic Write Safety

**Validates: Requirements 14.5, 14.6**

If a write fails (exception thrown), the previous state is preserved. JSON backends use temp-file strategy, SQLite and Postgres use transaction rollback, DynamoDB item writes are individually atomic.

### Property 5: Null vs Empty Preservation

**Validates: Requirements 11.4**

If an entity collection property is null in source, it remains null after round-trip. If it is an empty array, it remains empty array (not collapsed to null).

### Property 6: Decimal Precision Preservation

**Validates: Requirements 11.2**

Banking balance values (decimal) maintain full precision through all backends. Tested with values like 123456789.123456789012345678m.

### Property 7: DateTime Tick Precision

**Validates: Requirements 11.3**

DateTime values maintain tick-level precision through all backends. JSON uses ISO 8601 with 7 fractional digits. SQLite stores as ISO 8601 text. Postgres uses timestamp with time zone.

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
    MigrationService.cs
  Models/
    ServerModels.cs
    PermissionModels.cs
  Services/
    PlayerContext.cs      (modified)
    EmpireContext.cs      (modified)
    PreferencesStore.cs   (modified)
OE2EmpireTracker.Server/
  Storage/                (DELETED)
  Program.cs              (modified)
```

## Design Decisions

| Decision | Rationale |
|---|---|
| JSON-blob storage in SQLite (not normalized tables) | Avoids schema-per-entity complexity. Entity models change frequently. No SQLite schema migration for model changes. |
| No ORM | netstandard2.0 constraint eliminates EF Core. Raw ADO.NET via Microsoft.Data.Sqlite is simpler. |
| Full-file rewrite for JsonSingleFile | Inherent limitation of the format. Per-entity Upsert still rewrites the file but the interface is uniform. |
| Server-global entities throw on JsonSingleFile | Desktop does not need them. Keeps single-file format unchanged. |
| All backends in Common (not separate assemblies) | Simpler build, single dependency graph. Size is acceptable. |
| Async interface with sync callers | PlayerContext uses .GetAwaiter().GetResult() for now. Future async refactoring possible without interface changes. |
| Polly retry only for Postgres | Network backends benefit from retry. File-based and in-process SQLite do not have transient network failures. |

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

### Phase 2: PlayerContext and EmpireContext Refactoring

1. Add SetStorageBackend/LoadAsync/MarkDirty to PlayerContext
2. Modify WriteContext to delegate to backend with dirty tracking
3. Preserve backward-compatible parameterless constructor
4. Add SetStorageBackend/LoadAsync to EmpireContext
5. Add StorageBackendType/StoragePath to PreferencesStore
6. Update Desktop startup to use StorageBackendFactory
7. All existing WinForms tests pass

### Phase 3: Migration Service and Property Tests

1. Create MigrationService with progress reporting and count validation
2. Create FsCheck property tests for round-trip fidelity
3. Create property tests for decimal precision, DateTime precision, null-vs-empty
4. End-to-end integration test: migrate through all backend pairs and assert equality
