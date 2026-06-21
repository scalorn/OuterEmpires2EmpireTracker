# Design Document

## Overview

This design covers the integration of PlayerContext and EmpireContext with the unified IStorageBackend interface (Phase 2) and the data migration service with round-trip fidelity property tests (Phase 3). The architecture maintains backward compatibility with existing WinForms code while enabling configurable backend switching.

## Architecture

```
┌───────────────────────────────────────────────────────────────────┐
│                     OE2EmpireTracker.Desktop                       │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │ Program.cs (startup)                                          │ │
│  │  1. Load UIPreferences → StorageBackendType + StoragePath     │ │
│  │  2. StorageBackendFactory.CreateAsync(type, config)           │ │
│  │  3. playerCtx.StorageBackend = backend                        │ │
│  │  4. empireCtx.StorageBackend = backend                        │ │
│  └──────────────────────────────────────────────────────────────┘ │
└───────────────────────────────────────────────────────────────────┘
         │                                          │
         ▼                                          ▼
┌────────────────────────┐            ┌──────────────────────────┐
│    PlayerContext        │            │     EmpireContext         │
│  (OE2EmpireTracker.     │            │  (OE2EmpireTracker.       │
│   Common/Services/)     │            │   Common/Services/)       │
│                         │            │                           │
│  • StorageBackend prop  │            │  • StorageBackend prop    │
│  • StorageBackendType   │            │  • StorageBackendType     │
│  • DirtyTracker         │            │  • WriteContext() ──────────┐
│  • WriteContext() ─────────┐         └──────────────────────────┘  │
│  • LoadFromBackend()    │  │                                       │
└────────────────────────┘  │                                       │
                            │                                       │
         ┌──────────────────┴───────────────────────────────────────┘
         ▼
┌─────────────────────────────────────────────────────────────────┐
│                   IStorageBackend                                 │
│  (OE2EmpireTracker.Common/Interfaces/)                           │
│                                                                   │
│  + GetAllCharacterUUIDsAsync() — NEW                             │
│  + Per-character CRUD (22 entity types)                           │
│  + Baseline data CRUD                                            │
│  + Permissions / Intel / Audit                                    │
└──────────┬──────────┬──────────┬──────────┬──────────┬──────────┘
           │          │          │          │          │
     ┌─────┴───┐ ┌───┴────┐ ┌──┴───┐ ┌───┴────┐ ┌──┴──────┐
     │JsonSingle│ │JsonMulti│ │SQLite │ │DynamoDB│ │Postgres │
     │FileBackend│ │FileBackend│ │Backend│ │Backend│ │Backend  │
     └──────────┘ └──────────┘ └───────┘ └────────┘ └─────────┘
```

## Components and Interfaces

### Modified Components
- **PlayerContext** — Singleton service; gains `StorageBackend` property, `DirtyTracker`, async bridging in `WriteContext`, `LoadFromBackend` on player switch
- **EmpireContext** — Singleton service; gains `StorageBackend` + `StorageBackendType` properties, strategy-based load/save
- **PreferencesStore** — Gains `ResolveStorageConfig()` and `ParseStorageBackendType()` methods
- **UIPreferences** — Gains storage backend configuration properties
- **BackgroundProcessor** — Refactored to route colony mutations through ColonyService
- **IStorageBackend** — Extended with `GetAllCharacterUUIDsAsync()`
- **SqliteBackend** — Decimal precision fix (REAL→TEXT)
- **PostgresBackend** — Decimal precision fix (DOUBLE PRECISION→NUMERIC)
- **18 Service classes** — Each gains MarkDirty/MarkDeleted calls after mutations

### New Components
- **DirtyTracker** — Thread-safe tracker for per-entity dirty/deleted state (Common/Services/)
- **MigrationService** — Copies all data from source to destination backend (Common/Services/)
- **MigrationProgress** — Progress reporting model (Common/Services/)
- **MigrationValidationException** — Exception for post-migration count mismatches (Common/Interfaces/)
- **FormMigrationProgress** — Simple progress dialog shown during migration (Desktop/Forms/)

### Key Interfaces
- `IStorageBackend` — Unified async CRUD interface (existing, extended)
- `IProgress<MigrationProgress>` — Standard .NET progress reporting (BCL)

## Data Models

### DirtyKey (internal struct)
| Field | Type | Description |
|-------|------|-------------|
| EntityType | Type | The .NET type of the entity (Colony, Blueprint, etc.) |
| EntityUUID | string | The UUID of the entity |

### MigrationProgress
| Field | Type | Description |
|-------|------|-------------|
| CurrentEntityType | string | Name of the entity type currently being migrated |
| EntitiesProcessed | int | Cumulative count of entities processed so far |
| Phase | string | Current phase description (Global, Character {uuid}, Baseline) |

### UIPreferences Storage Extensions
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| StorageBackendType | string | "JsonSingleFile" | Backend enum name |
| StoragePath | string | null | Path for file-based backends |
| StorageAwsRegion | string | null | AWS region for DynamoDB |
| StorageTablePrefix | string | null | Table prefix for DynamoDB |
| StorageConnectionString | string | null | Connection string for Postgres |

## High-Level Design

### Component Diagram

| Component | Responsibility | Location |
|-----------|---------------|----------|
| PlayerContext (modified) | Accept IStorageBackend, bridge async calls, dirty tracking, error recovery | Common/Services/PlayerContext.cs |
| EmpireContext (modified) | Accept IStorageBackend, strategy-based load/save for baseline data | Common/Services/EmpireContext.cs |
| DirtyTracker | Track per-entity dirty/deleted state by type and UUID | Common/Services/DirtyTracker.cs (NEW) |
| MigrationService | Copy all entity data from source to destination backend | Common/Services/MigrationService.cs (NEW) |
| MigrationProgress | Progress reporting model | Common/Services/MigrationProgress.cs (NEW) |
| MigrationValidationException | Thrown when post-migration count validation fails | Common/Interfaces/StorageExceptions.cs (extended) |
| UIPreferences (modified) | StorageBackendType + StoragePath + DynamoDB/Postgres settings | Common/Models/UIPreferences.cs |
| PreferencesStore (modified) | Default resolution logic for backend paths | Common/Services/PreferencesStore.cs |
| Program.cs (modified) | Startup backend creation, error dialog, fallback | Desktop/Program.cs |
| IStorageBackend (modified) | Add GetAllCharacterUUIDsAsync() | Common/Interfaces/IStorageBackend.cs |
| SQLite/Postgres backends (modified) | Decimal precision fix (REAL→TEXT, DOUBLE PRECISION→NUMERIC) | Common/Storage/*.cs |

### Data Flow: WriteContext (PlayerContext)

```
WriteContext() called by service
    │
    ├── WritesBlocked? → return early
    │
    ├── No backend AND not ServerOnly? → log warning, return
    │
    ├── ServerOnly mode? → delegate to PushToServer (existing)
    │
    ├── Backend is JsonSingleFile?
    │   ├── YES: serialize full PlayerRoot → UpsertGlobalDataAsync("PlayerRoot", json)
    │   │         clear all dirty flags
    │   └── NO: iterate dirty entities per type
    │            ├── for each dirty UUID: call UpsertXxxAsync(characterUUID, entity)
    │            │   └── on success: clear dirty flag for that entity
    │            ├── for each deleted UUID: call DeleteXxxAsync(characterUUID, uuid)
    │            │   └── on success: remove from deletion set
    │            └── on StorageWriteException: set WritesBlocked=true, propagate
    │
    └── Integrity checks + logging (existing)
```

### Data Flow: Load on Player Switch

```
CurrentPlayerUUID setter
    │
    ├── No backend configured? → use existing file-based logic
    │
    ├── Save previousUUID
    │
    ├── Call backend.GetAllXxxAsync(newUUID) for each of 22 entity types
    │   (via Task.Run bridging)
    │
    ├── On success:
    │   ├── Replace in-memory lists
    │   ├── Clear all dirty flags (fresh load = clean state)
    │   ├── Fire CurrentPlayerChanged
    │   └── Rebuild caches/indexes
    │
    └── On StorageLoadException:
        ├── Revert _currentPlayerUUID to previousUUID
        └── Propagate exception
```


### Data Flow: EmpireContext Load

```
EmpireContext (backend configured)
    │
    ├── StorageBackendType == JsonSingleFile or JsonMultiFile?
    │   ├── GetGlobalDataAsync("BaselineRoot")
    │   ├── Result is null/empty? → load from BaselineData.json file, seed into backend
    │   └── Deserialize JSON → BaselineRoot → init all collections
    │
    └── StorageBackendType == Sqlite, DynamoDb, or Postgres?
        ├── GetBaselineGameConstantsAsync()
        ├── Result is null? → SEED FROM FILE (see below)
        ├── GetAllBlueprintTypesAsync()
        ├── GetAllShipClassesAsync()
        ├── GetAllTechLevelsAsync()
        ├── GetAllCommoditiesAsync()
        ├── GetAllRefiningRecipesAsync()
        ├── GetAllResearchTimesAsync()
        └── GetAllPropertyTypeDefinitionsAsync()

SEED FROM FILE (one-time bootstrap for empty relational backends):
    │
    ├── Load BaselineData.json from disk (same path as legacy file I/O)
    ├── Deserialize into BaselineRoot
    ├── Initialize all in-memory collections from BaselineRoot
    ├── Persist into backend via WriteContext (writes all baseline collections)
    └── Log.Info("Seeded baseline data into {backendType} from BaselineData.json")
```

### Data Flow: Migration Service

```
MigrationService.MigrateAsync(source, destination, progress, characterUUIDs?)
    │
    ├── Discover characters: source.GetAllCharacterUUIDsAsync()
    │   (or use provided list for selective migration)
    │
    ├── Migrate server-global data:
    │   ├── Factions, Characters, StarSystems, Tokens, MembershipActions
    │   └── Report progress per type
    │
    ├── Migrate baseline data:
    │   ├── GameConstants, BlueprintTypes, ShipClasses, TechLevels
    │   ├── Commodities, RefiningRecipes, ResearchTimes, PropertyTypes
    │   └── Report progress per type
    │
    ├── Migrate per-character data (for each character):
    │   ├── All 22 entity types (Colonies, Blueprints, Surveys, etc.)
    │   ├── SharingRules, CharacterPreferences
    │   └── Report progress per type per character
    │
    ├── Migrate permissions, intel, audit:
    │   ├── FactionCapabilities, ClearanceLevels, Groups, etc.
    │   ├── IntelComments, IntelShares
    │   └── AuditEntries
    │
    ├── Validate entity counts:
    │   ├── For each entity type: source count == destination count
    │   └── On mismatch: throw MigrationValidationException
    │
    └── Return MigrationResult (counts, duration)
```

## Low-Level Design

### 1. IStorageBackend Extension

```csharp
// Added to IStorageBackend interface
/// <summary>
/// Returns all character UUIDs that have stored player data.
/// Used by migration service for character discovery.
/// </summary>
Task<IReadOnlyList<string>> GetAllCharacterUUIDsAsync();
```

Implementation per backend:
- **JsonSingleFile**: Parse the single JSON file, extract all unique OwnerUUID values from entity arrays
- **JsonMultiFile**: Enumerate character subdirectories in the data folder
- **SQLite**: `SELECT DISTINCT CharacterUUID FROM Colonies UNION SELECT DISTINCT CharacterUUID FROM Blueprints ... `
- **DynamoDB**: Scan the partition key prefix for character UUIDs
- **Postgres**: Same UNION query as SQLite

### 2. DirtyTracker Class

```csharp
namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Tracks which entities have been modified or deleted since the last persistence.
    /// Thread-safe via internal locking.
    /// </summary>
    public class DirtyTracker
    {
        private readonly object _lock = new object();
        private readonly HashSet<DirtyKey> _dirty = new HashSet<DirtyKey>();
        private readonly HashSet<DirtyKey> _deleted = new HashSet<DirtyKey>();

        /// <summary>
        /// Marks an entity as modified. Called by service classes after mutations.
        /// </summary>
        /// <typeparam name="T">The entity type (Colony, Blueprint, etc.)</typeparam>
        /// <param name="entityUUID">The UUID of the modified entity.</param>
        public void MarkDirty<T>(string entityUUID);

        /// <summary>
        /// Marks an entity as deleted. Called by service classes after removals.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entityUUID">The UUID of the deleted entity.</param>
        public void MarkDeleted<T>(string entityUUID);

        /// <summary>
        /// Gets all dirty entity UUIDs for a given type.
        /// </summary>
        public IReadOnlyList<string> GetDirtyUUIDs<T>();

        /// <summary>
        /// Gets all deleted entity UUIDs for a given type.
        /// </summary>
        public IReadOnlyList<string> GetDeletedUUIDs<T>();

        /// <summary>
        /// Returns true if any entity of any type is dirty or deleted.
        /// </summary>
        public bool HasChanges { get; }

        /// <summary>
        /// Clears the dirty flag for a single entity after successful persistence.
        /// </summary>
        public void ClearDirty<T>(string entityUUID);

        /// <summary>
        /// Clears the deleted record for a single entity after successful deletion.
        /// </summary>
        public void ClearDeleted<T>(string entityUUID);

        /// <summary>
        /// Clears all dirty and deleted flags (used after full-file write or fresh load).
        /// </summary>
        public void ClearAll();
    }

    /// <summary>
    /// Composite key identifying a dirty/deleted entity by type + UUID.
    /// </summary>
    internal struct DirtyKey : IEquatable<DirtyKey>
    {
        public Type EntityType { get; }
        public string EntityUUID { get; }

        public DirtyKey(Type entityType, string entityUUID);
        public bool Equals(DirtyKey other);
        public override int GetHashCode();
    }
}
```


### 3. PlayerContext Modifications

```csharp
// New public properties on PlayerContext
public class PlayerContext
{
    /// <summary>
    /// The storage backend used for persistence. When null and not in ServerOnly mode,
    /// WriteContext logs a warning and returns (backward compatibility for tests).
    /// </summary>
    public IStorageBackend StorageBackend { get; set; }

    /// <summary>
    /// The dirty tracker instance. Services call MarkDirty/MarkDeleted through
    /// PlayerContext to record mutations.
    /// </summary>
    public DirtyTracker DirtyTracker { get; } = new DirtyTracker();

    /// <summary>
    /// Marks an entity as dirty (modified). Delegates to DirtyTracker.
    /// </summary>
    public void MarkDirty<T>(string entityUUID)
    {
        DirtyTracker.MarkDirty<T>(entityUUID);
    }

    /// <summary>
    /// Marks an entity as deleted. Delegates to DirtyTracker.
    /// </summary>
    public void MarkDeleted<T>(string entityUUID)
    {
        DirtyTracker.MarkDeleted<T>(entityUUID);
    }
}
```

**WriteContext modifications (pseudocode):**

```csharp
public void WriteContext()
{
    if (WritesBlocked) { Log.Warn(...); return; }

    // ServerOnly mode: existing delegate-based behavior unchanged
    if (IsServerOnlyMode?.Invoke() == true) { /* existing logic */ return; }

    // No backend configured: backward-compatible warning
    if (StorageBackend == null)
    {
        if (!string.IsNullOrEmpty(FilePath))
        {
            // Legacy file-based write (existing behavior)
            WriteLegacyJsonFile();
        }
        else
        {
            Log.Warn("WriteContext skipped -- no backend or file path");
        }
        return;
    }

    // Backend-based write
    try
    {
        string charUUID = CurrentPlayerUUID;
        var backend = StorageBackend;

        if (backend is JsonSingleFileBackend)
        {
            // Full serialization for single-file backend
            var playerRoot = BuildPlayerRoot();
            string json = JsonConvert.SerializeObject(playerRoot, JsonSettings.SerializerSettings);
            Task.Run(() => backend.UpsertGlobalDataAsync("PlayerRoot:" + charUUID, json))
                .GetAwaiter().GetResult();
            DirtyTracker.ClearAll();
        }
        else
        {
            // Incremental write: only dirty entities
            PersistDirtyEntities(backend, charUUID);
        }
    }
    catch (StorageWriteException ex)
    {
        WritesBlocked = true;
        throw;
    }
}

private void PersistDirtyEntities(IStorageBackend backend, string charUUID)
{
    // For each of 22 entity types:
    // 1. Get dirty UUIDs from DirtyTracker
    // 2. Find entity in in-memory list
    // 3. Call Upsert on backend (via Task.Run bridge)
    // 4. On success: ClearDirty for that entity
    // Then process deletions similarly
    // Example for Colony:
    foreach (string uuid in DirtyTracker.GetDirtyUUIDs<Colony>())
    {
        Colony entity = FindMutableColony(uuid);
        if (entity != null)
        {
            Task.Run(() => backend.UpsertColonyAsync(charUUID, entity))
                .GetAwaiter().GetResult();
            DirtyTracker.ClearDirty<Colony>(uuid);
        }
    }
    foreach (string uuid in DirtyTracker.GetDeletedUUIDs<Colony>())
    {
        Task.Run(() => backend.DeleteColonyAsync(charUUID, uuid))
            .GetAwaiter().GetResult();
        DirtyTracker.ClearDeleted<Colony>(uuid);
    }
    // ... repeat for all 22 types
}
```

**LoadFromBackend (new method for player switch):**

```csharp
private void LoadFromBackend(string characterUUID)
{
    var backend = StorageBackend;
    // Bridge async to sync via Task.Run to avoid SynchronizationContext deadlocks
    var colonies = Task.Run(() => backend.GetAllColoniesAsync(characterUUID))
        .GetAwaiter().GetResult();
    var blueprints = Task.Run(() => backend.GetAllBlueprintsAsync(characterUUID))
        .GetAwaiter().GetResult();
    // ... all 22 types ...

    // Build a PlayerRoot from loaded data and reinitialize
    var playerRoot = new PlayerRoot
    {
        Colony = colonies.ToArray(),
        Blueprint = blueprints.ToArray(),
        // ...
    };

    lock (_listLock)
    {
        InitColonies(playerRoot);
        InitBlueprints(playerRoot);
        // ... all Init methods ...
    }

    DirtyTracker.ClearAll();
}
```

### 4. EmpireContext Modifications

```csharp
public class EmpireContext
{
    /// <summary>
    /// The storage backend used for baseline data persistence.
    /// When null, falls back to direct file I/O using FilePath.
    /// </summary>
    public IStorageBackend StorageBackend { get; set; }

    /// <summary>
    /// The backend type, used to select load/save strategy without runtime type checks.
    /// </summary>
    public StorageBackendType? StorageBackendType { get; set; }
}
```

**WriteContext modifications (pseudocode):**

```csharp
public void WriteContext()
{
    if (PlayerContext.WritesBlocked) { Log.Warn(...); return; }

    var baselineRoot = BuildBaselineRoot();
    baselineRoot = SerializationSorter.SortBaselineRoot(baselineRoot);

    if (StorageBackend == null)
    {
        // Legacy file-based write (existing behavior)
        string json = JsonConvert.SerializeObject(baselineRoot, JsonSettings.SerializerSettings);
        SafeFileWriter.WriteAllText(FilePath, json);
        return;
    }

    var backend = StorageBackend;
    var type = StorageBackendType.Value;

    if (type == Interfaces.StorageBackendType.JsonSingleFile
        || type == Interfaces.StorageBackendType.JsonMultiFile)
    {
        string json = JsonConvert.SerializeObject(baselineRoot, JsonSettings.SerializerSettings);
        Task.Run(() => backend.UpsertGlobalDataAsync("BaselineRoot", json))
            .GetAwaiter().GetResult();
    }
    else
    {
        // Relational: upsert each baseline collection
        Task.Run(() => backend.UpsertBaselineGameConstantsAsync(GameConstants))
            .GetAwaiter().GetResult();
        Task.Run(() => backend.UpsertBlueprintTypesAsync(_blueprintTypeList))
            .GetAwaiter().GetResult();
        // ... etc for all baseline types
    }
}
```

**Baseline seeding on first use (new logic in constructor/load path):**

```csharp
private void LoadBaselineFromBackend()
{
    var backend = StorageBackend;
    var type = StorageBackendType.Value;

    BaselineRoot baselineRoot = null;

    if (type == Interfaces.StorageBackendType.JsonSingleFile
        || type == Interfaces.StorageBackendType.JsonMultiFile)
    {
        string json = Task.Run(() => backend.GetGlobalDataAsync("BaselineRoot"))
            .GetAwaiter().GetResult();
        if (!string.IsNullOrEmpty(json))
        {
            baselineRoot = JsonConvert.DeserializeObject<BaselineRoot>(json);
        }
    }
    else
    {
        // Relational backend: check if baseline data exists
        var constants = Task.Run(() => backend.GetBaselineGameConstantsAsync())
            .GetAwaiter().GetResult();

        if (constants != null)
        {
            // Backend has data — load each collection
            baselineRoot = new BaselineRoot();
            baselineRoot.GameConstants = constants;
            baselineRoot.BlueprintType = Task.Run(() => backend.GetAllBlueprintTypesAsync())
                .GetAwaiter().GetResult().ToArray();
            baselineRoot.ShipClass = Task.Run(() => backend.GetAllShipClassesAsync())
                .GetAwaiter().GetResult().ToArray();
            baselineRoot.TechLevel = Task.Run(() => backend.GetAllTechLevelsAsync())
                .GetAwaiter().GetResult().ToArray();
            baselineRoot.Commodity = Task.Run(() => backend.GetAllCommoditiesAsync())
                .GetAwaiter().GetResult().ToArray();
            baselineRoot.RefiningRecipe = Task.Run(() => backend.GetAllRefiningRecipesAsync())
                .GetAwaiter().GetResult().ToArray();
            baselineRoot.ResearchTime = Task.Run(() => backend.GetAllResearchTimesAsync())
                .GetAwaiter().GetResult().ToArray();
            baselineRoot.PropertyType = Task.Run(() => backend.GetAllPropertyTypeDefinitionsAsync())
                .GetAwaiter().GetResult().ToArray();
        }
    }

    if (baselineRoot == null)
    {
        // Backend is empty — seed from BaselineData.json on disk
        Log.Info("Backend has no baseline data — seeding from {0}", FilePath);
        if (File.Exists(FilePath))
        {
            string fileJson = File.ReadAllText(FilePath);
            baselineRoot = JsonConvert.DeserializeObject<BaselineRoot>(fileJson);
        }
        else
        {
            Log.Warn("BaselineData.json not found at {0} — starting with empty baseline", FilePath);
            baselineRoot = new BaselineRoot
            {
                GameConstants = new BaselineGameConstants()
            };
        }

        _needsSeedWrite = true; // Flag to persist into backend after init
    }

    // Initialize all in-memory collections from baselineRoot
    InitFromBaselineRoot(baselineRoot);

    // If we seeded from file, persist into the backend so next startup loads from backend
    if (_needsSeedWrite)
    {
        WriteContext();
        Log.Info("Seeded baseline data into {0} backend from BaselineData.json", type);
        _needsSeedWrite = false;
    }
}
```


### 5. UIPreferences Storage Settings

```csharp
// New properties on UIPreferences
public class UIPreferences
{
    /// <summary>
    /// The configured storage backend type. Defaults to JsonSingleFile.
    /// </summary>
    [JsonProperty("storageBackendType")]
    [DefaultValue("JsonSingleFile")]
    public string StorageBackendType { get; set; } = "JsonSingleFile";

    /// <summary>
    /// Path for file-based backends (JsonSingleFile, JsonMultiFile, Sqlite).
    /// Null/empty = use default path for the backend type.
    /// </summary>
    [JsonProperty("storagePath")]
    public string StoragePath { get; set; }

    /// <summary>
    /// AWS region for DynamoDB backend.
    /// </summary>
    [JsonProperty("storageAwsRegion")]
    public string StorageAwsRegion { get; set; }

    /// <summary>
    /// Table name prefix for DynamoDB backend.
    /// </summary>
    [JsonProperty("storageTablePrefix")]
    public string StorageTablePrefix { get; set; }

    /// <summary>
    /// Connection string for Postgres backend.
    /// </summary>
    [JsonProperty("storageConnectionString")]
    public string StorageConnectionString { get; set; }
}
```

**PreferencesStore default resolution logic:**

```csharp
public StorageBackendConfig ResolveStorageConfig()
{
    var type = ParseStorageBackendType(Preferences.StorageBackendType);
    var config = new StorageBackendConfig();

    switch (type)
    {
        case StorageBackendType.JsonSingleFile:
        case StorageBackendType.JsonMultiFile:
            config.ConnectionString = !string.IsNullOrEmpty(Preferences.StoragePath)
                ? Preferences.StoragePath
                : AppDomain.CurrentDomain.BaseDirectory;
            break;

        case StorageBackendType.Sqlite:
            config.ConnectionString = !string.IsNullOrEmpty(Preferences.StoragePath)
                ? Preferences.StoragePath
                : Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "OE2EmpireTracker");
            break;

        case StorageBackendType.DynamoDb:
            config.AwsRegion = Preferences.StorageAwsRegion;
            config.TablePrefix = Preferences.StorageTablePrefix;
            break;

        case StorageBackendType.Postgres:
            config.ConnectionString = Preferences.StorageConnectionString;
            break;
    }

    return config;
}

private StorageBackendType ParseStorageBackendType(string value)
{
    if (Enum.TryParse<StorageBackendType>(value, ignoreCase: true, out var parsed))
        return parsed;

    Log.Warn("Unrecognized StorageBackendType '{0}', falling back to JsonSingleFile", value);
    return StorageBackendType.JsonSingleFile;
}
```

### 6. Desktop Startup Sequence

```csharp
// In Program.cs or MainWindow initialization
static void InitializeStorage()
{
    var prefs = PreferencesStore.GetInstance();
    var type = prefs.ParseStorageBackendType();
    var config = prefs.ResolveStorageConfig();

    IStorageBackend backend;
    try
    {
        backend = Task.Run(() => StorageBackendFactory.CreateAsync(type, config))
            .GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        // Show error dialog: "Storage backend failed to initialize"
        // Offer: [Fall back to JSON] [Exit]
        var result = MessageBox.Show(
            $"Failed to initialize {type} backend:\n{ex.Message}\n\n" +
            "Fall back to JSON file storage?",
            "Storage Error",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Error);

        if (result == DialogResult.Yes)
        {
            type = StorageBackendType.JsonSingleFile;
            config = new StorageBackendConfig
            {
                ConnectionString = AppDomain.CurrentDomain.BaseDirectory
            };
            backend = Task.Run(() => StorageBackendFactory.CreateAsync(type, config))
                .GetAwaiter().GetResult();
        }
        else
        {
            Environment.Exit(1);
            return;
        }
    }

    // Wire backend into contexts
    var playerCtx = PlayerContext.GetInstance();
    playerCtx.StorageBackend = backend;

    var empireCtx = EmpireContext.GetInstance();
    empireCtx.StorageBackend = backend;
    empireCtx.StorageBackendType = type;
}
```

### 7. MigrationService

```csharp
namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Progress data reported during migration.
    /// </summary>
    public class MigrationProgress
    {
        /// <summary>Gets or sets the current entity type being migrated.</summary>
        public string CurrentEntityType { get; set; }

        /// <summary>Gets or sets the cumulative count of entities processed.</summary>
        public int EntitiesProcessed { get; set; }

        /// <summary>Gets or sets the current phase description.</summary>
        public string Phase { get; set; }
    }

    /// <summary>
    /// Migrates all entity data from any source IStorageBackend to any destination.
    /// </summary>
    public class MigrationService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Migrates all data from source to destination.
        /// </summary>
        /// <param name="source">Source backend to read from.</param>
        /// <param name="destination">Destination backend to write to.</param>
        /// <param name="progress">Progress callback (invoked at least once per entity type).</param>
        /// <param name="characterUUIDs">
        /// Optional explicit list of character UUIDs to migrate.
        /// If null, discovers all characters from source via GetAllCharacterUUIDsAsync().
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        public async Task MigrateAsync(
            IStorageBackend source,
            IStorageBackend destination,
            IProgress<MigrationProgress> progress = null,
            IReadOnlyList<string> characterUUIDs = null,
            CancellationToken ct = default);

        /// <summary>
        /// Validates that entity counts match between source and destination.
        /// Throws MigrationValidationException on mismatch.
        /// </summary>
        private async Task ValidateCountsAsync(
            IStorageBackend source,
            IStorageBackend destination,
            IReadOnlyList<string> characterUUIDs,
            CancellationToken ct);
    }
}
```


**Migration algorithm (pseudocode):**

```csharp
async Task MigrateAsync(...)
{
    int totalProcessed = 0;

    // 1. Discover characters
    var chars = characterUUIDs
        ?? await source.GetAllCharacterUUIDsAsync();

    // 2. Migrate server-global entities
    var factions = await source.GetAllFactionsAsync();
    foreach (var f in factions)
        await destination.UpsertFactionAsync(f);
    totalProcessed += factions.Count;
    progress?.Report(new MigrationProgress
    {
        CurrentEntityType = "ServerFaction",
        EntitiesProcessed = totalProcessed,
        Phase = "Global"
    });
    // ... Characters, StarSystems, Tokens, MembershipActions

    // 3. Migrate baseline data
    var constants = await source.GetBaselineGameConstantsAsync();
    if (constants != null)
        await destination.UpsertBaselineGameConstantsAsync(constants);
    // ... BlueprintTypes, ShipClasses, TechLevels, Commodities, etc.

    // 4. Per-character migration
    foreach (var charUUID in chars)
    {
        ct.ThrowIfCancellationRequested();

        // Migrate all 22 per-character entity types
        var colonies = await source.GetAllColoniesAsync(charUUID);
        foreach (var c in colonies)
            await destination.UpsertColonyAsync(charUUID, c);
        totalProcessed += colonies.Count;
        progress?.Report(new MigrationProgress
        {
            CurrentEntityType = "Colony",
            EntitiesProcessed = totalProcessed,
            Phase = $"Character {charUUID}"
        });
        // ... repeat for Blueprints, Surveys, PlayerProfiles, etc.

        // Also: SharingRules, CharacterPreferences
        var sharing = await source.GetSharingRulesForCharacterAsync(charUUID);
        await destination.UpsertSharingRulesAsync(charUUID, sharing);
    }

    // 5. Migrate permissions, intel, audit
    // (FactionCapabilities, ClearanceLevels, Groups, etc.)
    // (IntelComments, IntelShares)
    // (AuditEntries)

    // 6. Validate counts
    await ValidateCountsAsync(source, destination, chars, ct);
}
```

**JsonSingleFileBackend as a migration destination:**

When migrating TO JsonSingleFileBackend, the MigrationService calls per-entity Upsert methods normally. Internally, JsonSingleFileBackend:
- Accumulates per-character entities in `_playerRoot` and rewrites `PlayerData.json` on each Upsert call
- Accumulates baseline entities in `_baselineRoot` and rewrites `BaselineData.json` on each baseline Upsert call

This means migrating 200 entities produces 200 file rewrites. This is correct but slow. Since migration is a one-time operation, this is acceptable. A future optimization could add a `BeginBatch()`/`EndBatch()` pattern to defer writes until the batch completes, but that is out of scope for this spec.

### 8. Migration Workflow (How Users Trigger Migration)

The migration is initiated programmatically by the desktop application when a user changes their storage backend in UIPreferences.json (or a future preferences UI). The workflow:

**Trigger:** User edits `UIPreferences.json` to change `storageBackendType` from one value to another (e.g. `"JsonSingleFile"` → `"Sqlite"`), then restarts the application.

**Startup detection logic in Program.cs:**

```csharp
static void InitializeStorage()
{
    var prefs = PreferencesStore.GetInstance();
    var newType = prefs.ParseStorageBackendType();
    var newConfig = prefs.ResolveStorageConfig();

    // Check if we have existing data in the old backend format
    // The "previous backend" is determined by whether data files exist at the old location
    var previousType = DetectCurrentDataBackend(prefs);

    IStorageBackend newBackend;
    try
    {
        newBackend = Task.Run(() => StorageBackendFactory.CreateAsync(newType, newConfig))
            .GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        // ... error dialog with fallback (see Section 6) ...
    }

    // If backend type changed AND the new backend is empty AND old data exists → migrate
    if (previousType != null && previousType != newType && BackendIsEmpty(newBackend))
    {
        OfferMigration(previousType.Value, newType, newConfig, newBackend);
    }

    // Wire backend into contexts
    PlayerContext.GetInstance().StorageBackend = newBackend;
    EmpireContext.GetInstance().StorageBackend = newBackend;
    EmpireContext.GetInstance().StorageBackendType = newType;
}
```

**Migration dialog flow:**

```
┌────────────────────────────────────────────────────────────────┐
│  Storage Backend Changed                                        │
│                                                                  │
│  You switched from JsonSingleFile to Sqlite.                    │
│  Would you like to migrate your existing data to the new        │
│  backend?                                                        │
│                                                                  │
│  [Migrate Data]    [Start Fresh]    [Cancel (revert to old)]    │
└────────────────────────────────────────────────────────────────┘
```

```csharp
private static void OfferMigration(
    StorageBackendType oldType,
    StorageBackendType newType,
    StorageBackendConfig newConfig,
    IStorageBackend newBackend)
{
    var result = MessageBox.Show(
        $"You switched from {oldType} to {newType}.\n\n" +
        "Would you like to migrate your existing data to the new backend?\n\n" +
        "• Yes = Copy all data from old backend to new backend\n" +
        "• No = Start fresh with empty data\n" +
        "• Cancel = Revert to previous backend",
        "Storage Backend Changed",
        MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Question);

    if (result == DialogResult.Cancel)
    {
        // Revert: don't use newBackend, fall back to old
        Environment.Exit(0);
        return;
    }

    if (result == DialogResult.Yes)
    {
        // Create old backend for reading
        var oldConfig = ResolveOldConfig(oldType);
        var oldBackend = Task.Run(() => StorageBackendFactory.CreateAsync(oldType, oldConfig))
            .GetAwaiter().GetResult();

        // Run migration with progress dialog
        var progressForm = new FormMigrationProgress();
        progressForm.Show();

        var progress = new Progress<MigrationProgress>(p =>
        {
            progressForm.UpdateProgress(p.Phase, p.CurrentEntityType, p.EntitiesProcessed);
        });

        try
        {
            var migrationService = new MigrationService();
            Task.Run(() => migrationService.MigrateAsync(oldBackend, newBackend, progress))
                .GetAwaiter().GetResult();

            progressForm.Close();
            MessageBox.Show("Migration complete!", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (MigrationValidationException ex)
        {
            progressForm.Close();
            MessageBox.Show(
                $"Migration completed but validation failed:\n{ex.Message}",
                "Migration Warning",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            progressForm.Close();
            PlayerContext.WritesBlocked = true;
            MessageBox.Show(
                $"Migration failed:\n{ex.Message}\n\n" +
                "Writes are blocked. Please fix the issue and restart.",
                "Migration Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    // DialogResult.No = start fresh, newBackend is already empty
}
```

**DetectCurrentDataBackend logic:**
- If `PlayerData.json` exists in the app directory → previous backend was `JsonSingleFile`
- If a `data/` folder with character subfolders exists → previous backend was `JsonMultiFile`
- If an SQLite `.db` file exists at the configured path → previous backend was `Sqlite`
- Otherwise → null (no previous data detected, skip migration offer)

**FormMigrationProgress** (simple progress dialog):
- Shows current phase (Global / Baseline / Character {uuid})
- Shows current entity type being migrated
- Shows cumulative entity count
- No cancel button in initial implementation (migration is fast enough for local backends)

**Programmatic migration (for tests and automation):**

```csharp
// Direct usage without UI — for integration tests or CLI tools
var source = await StorageBackendFactory.CreateAsync(
    StorageBackendType.JsonSingleFile,
    new StorageBackendConfig { ConnectionString = @"C:\data" });

var dest = await StorageBackendFactory.CreateAsync(
    StorageBackendType.Sqlite,
    new StorageBackendConfig { ConnectionString = @"C:\data\tracker.db" });

var service = new MigrationService();
await service.MigrateAsync(source, dest);
// source and dest can be any pair of the 5 backend types
```

### 9. MigrationValidationException

```csharp
// Added to StorageExceptions.cs
namespace OE2EmpireTracker.Common.Interfaces
{
    /// <summary>
    /// Thrown when post-migration entity count validation fails.
    /// </summary>
    public class MigrationValidationException : Exception
    {
        public MigrationValidationException(string message)
            : base(message) { }

        public MigrationValidationException(
            string message,
            IReadOnlyDictionary<string, (int Expected, int Actual)> mismatches)
            : base(message)
        {
            Mismatches = mismatches;
        }

        /// <summary>
        /// Entity types that had count mismatches: type name → (expected, actual).
        /// </summary>
        public IReadOnlyDictionary<string, (int Expected, int Actual)> Mismatches { get; }
    }
}
```

### 10. Decimal Precision Fix (Prerequisite)

**SQLite backend change:**
- Columns affected: `CreditChange`, `OldBalance`, `NewBalance`, `PricePerUnit`, `TotalPrice` (in BankingTransaction and MarketTransaction tables)
- Current: stored as `REAL`, read via `(double)` cast, then `(decimal)(double)value`
- Fix: store as `TEXT`, write via `value.ToString("G")`, read via `decimal.Parse(...)`
- Schema migration: `ALTER TABLE ... ADD COLUMN new_col TEXT; UPDATE ... SET new_col = CAST(old_col AS TEXT); ALTER TABLE ... DROP COLUMN old_col; ALTER TABLE ... RENAME COLUMN new_col TO ...`
- SQLite doesn't support DROP COLUMN pre-3.35.0; use the standard table-rebuild approach:
  1. `CREATE TABLE xxx_new (... TEXT columns ...)`
  2. `INSERT INTO xxx_new SELECT ... FROM xxx`
  3. `DROP TABLE xxx`
  4. `ALTER TABLE xxx_new RENAME TO xxx`

**Postgres backend change:**
- Same columns affected
- Current: `DOUBLE PRECISION`, read via `(double)` cast
- Fix: `NUMERIC` type (arbitrary precision), no cast needed
- Schema migration: `ALTER TABLE ... ALTER COLUMN col TYPE NUMERIC USING col::NUMERIC`

### 11. Service Class MarkDirty Integration

Each of the 18 service classes must call `PlayerContext.GetInstance().MarkDirty<T>(uuid)` after mutations and `MarkDeleted<T>(uuid)` after removals. Pattern:

```csharp
// Example: ColonyService
public void UpdateColonyName(string colonyUUID, string newName)
{
    var colony = PlayerContext.GetInstance().FindMutableColony(colonyUUID);
    colony.Name = newName;
    PlayerContext.GetInstance().MarkDirty<Colony>(colonyUUID);
    PlayerContext.GetInstance().OnColonyDataChanged(colonyUUID);
    PlayerContext.GetInstance().WriteContext();
}

public void DeleteColony(string colonyUUID)
{
    var colony = PlayerContext.GetInstance().FindMutableColony(colonyUUID);
    PlayerContext.GetInstance().RemoveColony(colony);
    PlayerContext.GetInstance().MarkDeleted<Colony>(colonyUUID);
    PlayerContext.GetInstance().OnColonyDataChanged(colonyUUID);
    PlayerContext.GetInstance().WriteContext();
}
```

**Services requiring MarkDirty/MarkDeleted calls (18 total):**
1. ColonyService
2. BlueprintService
3. SurveyService
4. PlayerProfileService
5. DeliveryRouteService
6. DeliveryPlanService
7. ShipService
8. ShipTemplateService
9. StationService
10. MarketListingService
11. PricingPlanService
12. BuildPlanMutationService
13. StockTargetMutationService
14. SupplyChainMutationService
15. AsteroidService
16. ContactsService
17. BankingService
18. MailService

### 12. BackgroundProcessor Colony Mutation Routing

BackgroundProcessor currently calls `Colony.ProcessColony()` directly. This must be refactored to route through ColonyService methods:

```csharp
// Before (direct mutation):
colony.ProcessColony(elapsedSeconds);

// After (via service):
ColonyService.ProcessColonyTick(colony.UUID, elapsedSeconds);
// ColonyService internally mutates + calls MarkDirty<Colony>(uuid)
```

This ensures BackgroundProcessor never bypasses dirty tracking.

### 13. Async-to-Sync Bridging Pattern

All calls from the WinForms UI thread to IStorageBackend use this pattern to avoid deadlocks with the WinForms SynchronizationContext:

```csharp
// Pattern: always run async work on the thread pool
var result = Task.Run(() => backend.SomeMethodAsync(args))
    .GetAwaiter()
    .GetResult();
```

This ensures the continuation doesn't try to marshal back to the UI thread (which would deadlock if the UI thread is blocked on `.GetResult()`).


### 14. Round-Trip Fidelity Property Tests

```csharp
// Test file: OE2EmpireTracker.Tests/Services/MigrationRoundTripPropertyTests.cs
[TestFixture]
public class MigrationRoundTripPropertyTests
{
    /// <summary>
    /// For any entity migrated A→B→A, the re-serialized JSON must be identical.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property ColonyRoundTrip_JsonSingleFile_To_Sqlite()
    {
        return Prop.ForAll(
            ArbitraryColony(),
            colony => RoundTripPreservesEquality<Colony>(
                StorageBackendType.JsonSingleFile,
                StorageBackendType.Sqlite,
                colony));
    }

    private bool RoundTripPreservesEquality<T>(
        StorageBackendType typeA,
        StorageBackendType typeB,
        T entity)
    {
        // 1. Write entity to backend A
        // 2. Migrate A → B
        // 3. Migrate B → A (new instance)
        // 4. Read entity from A
        // 5. Serialize both with SerializationSorter + JsonSettings
        // 6. Assert JSON strings are identical
        string originalJson = SerializeCanonical(entity);
        string roundTrippedJson = SerializeCanonical(roundTrippedEntity);
        return originalJson == roundTrippedJson;
    }

    private string SerializeCanonical<T>(T entity)
    {
        return JsonConvert.SerializeObject(
            entity,
            JsonSettings.SerializerSettings);
    }

    // FsCheck 2.16.6 generators for entities
    private Arbitrary<Colony> ArbitraryColony()
    {
        var gen = from uuid in Gen.Elements("char-1", "char-2")
                  from name in Arb.Generate<NonEmptyString>()
                  from structCount in Gen.Choose(0, 5)
                  // ... build colony with structures, resources, etc.
                  select new Colony { UUID = Guid.NewGuid().ToString(), ... };
        return Arb.From(gen);
    }
}
```

**Backend pairs to test (minimum):**
1. JsonSingleFile ↔ Sqlite
2. JsonSingleFile ↔ JsonMultiFile
3. Sqlite ↔ Postgres

**Entity types to cover (all 22 per-character + baseline):**
- Colony, Blueprint, Survey, PlayerProfile, DeliveryRoute, DeliveryPlan
- Ship, ShipTemplate, Station, MarketListing, MarketTransaction
- PricingPlan, BuildPlan, StockPlan, StockProfile, SupplyChain
- WarehouseOverflowRule, Faction, ExternalCharacter, Asteroid
- BankingTransaction, MailMessage
- BaselineGameConstants, BlueprintType, ShipClass, TechLevel, Commodity, RefiningRecipe, ResearchTimeEntry, PropertyTypeDefinition

**Key assertions per round-trip:**
- Decimal precision preserved (banking amounts, prices)
- DateTime preserved as ISO 8601 (tick-level)
- Null vs empty-collection distinction preserved
- UUID stability (no regeneration)
- Nested object graphs structurally equal
- Collection ordering preserved
- Enum values preserved
- Unicode strings preserved

## Correctness Properties

### Property 1: Write Idempotency
If WriteContext is called N times with no intervening mutations, only the first call produces backend writes (subsequent calls find no dirty entities).

**Validates: Requirements 2.4**

### Property 2: Dirty Flag Completeness
For every entity mutation made through a service method, the entity's dirty flag is set before WriteContext returns.

**Validates: Requirements 2.3**

### Property 3: Load-Write Round Trip
Loading entities from a backend and immediately writing them back produces zero net changes (no dirty flags set, no backend calls).

**Validates: Requirements 2.7**

### Property 4: Migration Entity Count Preservation
For any source backend containing N entities of type T, migrating to any destination backend results in exactly N entities of type T in the destination.

**Validates: Requirements 6.7**

### Property 5: Migration Round-Trip Fidelity
For any entity E migrated A→B→A, `serialize(original) == serialize(roundTripped)` using canonical JSON serialization.

**Validates: Requirements 7.1**

### Property 6: Decimal Precision Preservation
For any decimal value D stored via UpsertBankingTransactionAsync (or MarketTransaction), the value read back via GetBankingTransactionAsync equals D exactly (no floating-point rounding).

**Validates: Requirements 7.2**

### Property 7: WritesBlocked Monotonicity
Once WritesBlocked is set to true by a StorageWriteException, no further writes occur until WritesBlocked is explicitly reset to false by application code.

**Validates: Requirements 8.1**

### Property 8: Backend Fallback Safety
If StorageBackendFactory.CreateAsync throws, the application either falls back to JsonSingleFile (preserving all existing data access) or exits cleanly (no partial initialization).

**Validates: Requirements 5.9**

## Error Handling

### PlayerContext Write Failures
- StorageWriteException during WriteContext → set `WritesBlocked = true`, propagate exception
- Entities already persisted in the current write loop retain cleared dirty flags
- Entities not yet persisted retain dirty flags for retry after WritesBlocked is cleared
- No in-memory data is corrupted — write failure is a persistence-only problem

### PlayerContext Load Failures
- StorageLoadException during player switch → revert `CurrentPlayerUUID` to previous value
- In-memory state remains unchanged (previous player's data still active)
- Exception propagated to caller (form shows error dialog)

### EmpireContext Load Failures
- StorageLoadException during baseline load → propagate to caller, no partial initialization
- Application cannot start without baseline data — this is a fatal error

### Desktop Startup Failures
- StorageBackendFactory.CreateAsync throws → error dialog with fallback option
- User can choose JsonSingleFile fallback or exit
- Fallback uses default path (application directory)

### Migration Failures
- StorageLoadException reading from source → throw with entity type in message + count of entities migrated so far
- StorageWriteException writing to destination → same pattern
- Partial data remains in destination (no rollback of already-written entities)
- Post-migration count validation catches silent data loss via MigrationValidationException

### Invalid Preferences
- Unrecognized StorageBackendType string → fall back to JsonSingleFile, log warning
- Missing/empty connection strings → caught by StorageBackendFactory during CreateAsync

## Testing Strategy

### Unit Tests (DirtyTracker)
- MarkDirty adds entity to dirty set
- MarkDeleted adds entity to deleted set
- ClearDirty removes single entity
- ClearAll removes all dirty and deleted entries
- HasChanges returns true when dirty/deleted exist, false when empty
- Thread-safety under concurrent access

### Integration Tests (PlayerContext + Backend)
- WriteContext with no backend → warning, no crash (backward compat)
- WriteContext with backend → dirty entities persisted
- WriteContext with StorageWriteException → WritesBlocked set
- Player switch loads from backend, fires CurrentPlayerChanged
- Player switch failure reverts UUID
- JsonSingleFile backend → full write regardless of dirty flags

### Integration Tests (EmpireContext + Backend)
- Load from JSON backend → GetGlobalDataAsync used
- Load from relational backend → typed methods used
- WriteContext persists baseline data
- No backend → legacy file write

### Migration Service Tests
- Migrate empty source → empty destination (no errors)
- Migrate single character → all entities transferred
- Migrate multiple characters → all entities for all characters
- Count validation passes on correct migration
- Count validation throws MigrationValidationException on mismatch
- Progress callback invoked at least once per entity type
- Selective migration (explicit character list) only migrates specified characters

### Property Tests (Round-Trip Fidelity)
- FsCheck 2.16.6 generators for all 22 entity types + baseline types
- 100 test cases per backend pair (JsonSingleFile↔Sqlite, JsonSingleFile↔JsonMultiFile, Sqlite↔Postgres)
- Deep equality via canonical JSON serialization comparison
- Decimal precision asserted for banking/market fields
- DateTime tick-level precision asserted
- Null vs empty-collection distinction asserted

### Property Tests (Dirty Tracking)
- For any sequence of MarkDirty/MarkDeleted/ClearDirty/ClearAll operations, HasChanges is consistent
- WriteContext after N mutations produces exactly N backend Upsert calls (one per dirty entity)

## File Changes Summary

| File | Change Type | Satisfies |
|------|-------------|-----------|
| Common/Interfaces/IStorageBackend.cs | Add GetAllCharacterUUIDsAsync | Req 6 Crit 3 |
| Common/Interfaces/StorageExceptions.cs | Add MigrationValidationException | Req 6 Crit 8 |
| Common/Storage/SqliteBackend.cs | Decimal columns REAL→TEXT | Req 7 Crit 2 |
| Common/Storage/PostgresBackend.cs | Decimal columns DOUBLE PRECISION→NUMERIC | Req 7 Crit 2 |
| Common/Storage/JsonSingleFileBackend.cs | Implement GetAllCharacterUUIDsAsync | Req 6 Crit 3 |
| Common/Storage/JsonMultiFileBackend.cs | Implement GetAllCharacterUUIDsAsync | Req 6 Crit 3 |
| Common/Storage/DynamoDbBackend.cs | Implement GetAllCharacterUUIDsAsync | Req 6 Crit 3 |
| Common/Services/DirtyTracker.cs | NEW — dirty entity tracking | Req 2 |
| Common/Services/PlayerContext.cs | Add StorageBackend, DirtyTracker, modify WriteContext/Load | Req 1, 2, 8 |
| Common/Services/EmpireContext.cs | Add StorageBackend/Type, modify WriteContext/Load | Req 3 |
| Common/Services/MigrationService.cs | NEW — data migration service | Req 6 |
| Common/Services/MigrationProgress.cs | NEW — progress model | Req 6 Crit 5 |
| Common/Models/UIPreferences.cs | Add storage backend settings | Req 5 |
| Common/Services/PreferencesStore.cs | Add ResolveStorageConfig, ParseStorageBackendType | Req 5 |
| Desktop/Program.cs (or startup) | Backend creation + error dialog | Req 5 Crit 7, 9 |
| Common/Services/BackgroundProcessor.cs | Route colony mutations through ColonyService | Req 2 Crit 4 |
| Common/Services/ColonyService.cs | Add MarkDirty calls | Req 2 Crit 3 |
| Common/Services/BlueprintService.cs | Add MarkDirty calls | Req 2 Crit 3 |
| Common/Services/SurveyService.cs | Add MarkDirty calls | Req 2 Crit 3 |
| Common/Services/PlayerProfileService.cs | Add MarkDirty calls | Req 2 Crit 3 |
| Common/Services/DeliveryRouteService.cs | Add MarkDirty calls | Req 2 Crit 3 |
| Common/Services/DeliveryPlanService.cs | Add MarkDirty calls | Req 2 Crit 3 |
| Common/Services/ShipService.cs | Add MarkDirty calls | Req 2 Crit 3 |
| Common/Services/ShipTemplateService.cs | Add MarkDirty calls | Req 2 Crit 3 |
| Common/Services/StationService.cs | Add MarkDirty calls | Req 2 Crit 3 |
| Common/Services/MarketListingService.cs | Add MarkDirty calls | Req 2 Crit 3 |
| Common/Services/PricingPlanService.cs | Add MarkDirty calls | Req 2 Crit 3 |
| Common/Services/BuildPlanMutationService.cs | Add MarkDirty calls | Req 2 Crit 3 |
| Common/Services/StockTargetMutationService.cs | Add MarkDirty calls | Req 2 Crit 3 |
| Common/Services/SupplyChainMutationService.cs | Add MarkDirty calls | Req 2 Crit 3 |
| Common/Services/AsteroidService.cs | Add MarkDirty calls | Req 2 Crit 3 |
| Common/Services/ContactsService.cs | Add MarkDirty calls | Req 2 Crit 3 |
| Common/Services/BankingService.cs | Add MarkDirty calls | Req 2 Crit 3 |
| Common/Services/MailService.cs | Add MarkDirty calls | Req 2 Crit 3 |
| Tests/Services/DirtyTrackerTests.cs | NEW — unit tests | Req 2 |
| Tests/Services/MigrationServiceTests.cs | NEW — migration tests | Req 6 |
| Tests/Services/MigrationRoundTripPropertyTests.cs | NEW — property tests | Req 7 |
| Tests/Services/PlayerContextBackendTests.cs | NEW — integration tests | Req 1, 8 |
| Tests/Services/EmpireContextBackendTests.cs | NEW — integration tests | Req 3 |
