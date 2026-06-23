# Requirements Document

## Introduction

This spec covers Phase 2 and Phase 3 of the unified storage backend migration: integrating consumers (PlayerContext, EmpireContext) with the unified IStorageBackend interface, adding backend selection preferences, implementing a data migration service, and verifying round-trip fidelity across all backend pairs.

**Prerequisite:** The `unified-storage-backend` spec (Phase 1) is **COMPLETE**. It delivered the IStorageBackend interface, all 5 backend implementations (JsonSingleFile, JsonMultiFile, SQLite, DynamoDB, Postgres), StorageBackendFactory, StorageBackendType enum, error types (StorageLoadException, StorageWriteException, StorageCorruptionException), all entity models, and the Server project migration to Common backends — all residing in OE2EmpireTracker.Common. The Server project already uses Common's IStorageBackend and StorageBackendFactory.

## What Was Already Delivered (Phase 1)

The following items from the original spec are **COMPLETE** and do not need re-implementation:

- IStorageBackend interface with all CRUD methods for 22 per-character entity types, server-global entities, permissions, intel, audit, and baseline data
- StorageBackendType enum (JsonSingleFile, JsonMultiFile, Sqlite, DynamoDb, Postgres)
- StorageBackendFactory.CreateAsync(type, config)
- StorageBackendConfig class
- All 5 backend implementations with full CRUD
- StorageExceptions (StorageLoadException, StorageWriteException, StorageCorruptionException)
- Server/Storage/ folder removed; Server references Common backends
- Server Program.cs uses Common's IStorageBackend interface and creates backends from appsettings.json
- Server models (ServerModels.cs, PermissionModels.cs) moved to Common/Models/
- All 264 Server tests pass with Common backends
- Property tests for entity count preservation and atomic write safety
- PlayerContext.WritesBlocked static property (blocks writes, returns early)

### Known Defects in Phase 1 Deliverables (to fix as prerequisite)

- **Decimal precision loss:** SQLite backend uses REAL columns and `(double)` casts for CreditChange, OldBalance, NewBalance, PricePerUnit, TotalPrice. Postgres backend uses DOUBLE PRECISION with the same pattern. This loses precision on decimal→double→decimal round-trips. Fix: migrate REAL→TEXT (SQLite) and DOUBLE PRECISION→NUMERIC (Postgres) for all decimal-typed fields, remove `(double)` casts.
- **Missing interface method:** IStorageBackend lacks `GetAllCharacterUUIDsAsync()` needed for migration character discovery. Fix: add to interface, implement in all 5 backends.

## Glossary

- **IStorageBackend**: The unified async interface in OE2EmpireTracker.Common defining CRUD operations for all entity types
- **PlayerContext**: The singleton service in OE2EmpireTracker.Common that manages per-character player data, persistence, and change events
- **EmpireContext**: The singleton service in OE2EmpireTracker.Common that manages shared baseline game data (blueprint types, ship classes, commodities)
- **BaselineRoot**: The serialization root object containing all baseline entity arrays
- **Migration_Service**: A service that reads all entity data from any source IStorageBackend and writes it to any destination IStorageBackend
- **StorageBackendType**: The enum (JsonSingleFile, JsonMultiFile, Sqlite, DynamoDb, Postgres) in OE2EmpireTracker.Common.Interfaces
- **StorageBackendFactory**: The factory class that creates backend instances from configuration, in OE2EmpireTracker.Common.Storage
- **PreferencesStore**: The user preferences persistence service in OE2EmpireTracker.Desktop that stores application settings
- **StorageWriteException**: Exception thrown by IStorageBackend when a write operation fails
- **StorageLoadException**: Exception thrown by IStorageBackend when a read/load operation fails
- **MigrationValidationException**: Exception thrown by Migration_Service when post-migration entity count validation fails
- **WritesBlocked**: A static boolean property on PlayerContext indicating that persistence is disabled (already implemented)
- **Dirty_Entity_Tracking**: A mechanism in PlayerContext that marks entities as modified so that only changed entities are persisted on the next write
- **ServerOnly_Mode**: The mode where PlayerContext delegates persistence to a remote server via delegates instead of using local IStorageBackend

## Requirements

### Requirement 1: PlayerContext Storage Backend Integration

**User Story:** As a developer, I want PlayerContext to use IStorageBackend for all persistence, so that the desktop app can switch between JSON, SQLite, and cloud storage without modifying PlayerContext logic.

#### Acceptance Criteria

1. THE PlayerContext SHALL accept an IStorageBackend instance via a public property and use the IStorageBackend for all load and save operations instead of directly reading or writing JSON files
2. WHEN WriteContext is called and no IStorageBackend is configured AND ServerOnly mode is not active, THEN THE PlayerContext SHALL log a warning and return without writing (preserving backward compatibility with test scenarios that operate without a backend)
3. WHEN CurrentPlayerUUID changes and an IStorageBackend is configured, THE PlayerContext SHALL reload all per-character entity data from the backend by calling the corresponding GetAll methods, replacing in-memory state, before firing CurrentPlayerChanged; IF the load throws StorageLoadException, THE PlayerContext SHALL revert CurrentPlayerUUID to its previous value, leave in-memory state unchanged, and propagate the exception to the caller
4. IF the IStorageBackend property is set while a CurrentPlayerUUID is already active, THE PlayerContext SHALL immediately trigger a load from the new backend (equivalent to re-setting CurrentPlayerUUID)
5. THE PlayerContext SHALL continue to expose data via IReadOnlyList properties and fire all existing change events (CurrentPlayerChanged, ColonyDataChanged, BlueprintDataChanged, SurveyDataChanged, DeliveryDataChanged, PricingDataChanged, BuildPlanDataChanged, MarketDataChanged, StationDataChanged, AsteroidDataChanged, PlayerProfileDataChanged, ShipTemplateDataChanged, ShipDataChanged, StockDataChanged, SupplyChainDataChanged, ContactDataChanged, BankingDataChanged, MailDataChanged) regardless of which backend is configured
6. WHEN WriteContext is called with an IStorageBackend configured, THE PlayerContext SHALL persist the current entity state by calling Upsert on the IStorageBackend for all dirty entity collections, matching all 22 per-character entity types
7. THE PlayerContext SHALL maintain backward compatibility with existing ServerOnly mode delegates (IsServerOnlyMode, PushToServer, ExportFromServer, IsServerConnected) such that when ServerOnly mode is active, the delegates continue to control persistence behavior as they do today
8. THE PlayerContext SHALL expose a public settable property for the IStorageBackend that can be assigned after construction, enabling existing callers to configure the backend before triggering data load
9. IF an IStorageBackend method throws StorageWriteException during WriteContext, THEN THE PlayerContext SHALL set WritesBlocked to true and propagate the exception to the caller; in-memory entity state SHALL remain unchanged (no data loss on write failure)
10. ALL calls from PlayerContext to IStorageBackend SHALL use `Task.Run(() => backend.MethodAsync()).GetAwaiter().GetResult()` to bridge async-to-sync, avoiding deadlocks by ensuring the async work runs on the thread pool rather than capturing the WinForms SynchronizationContext


### Requirement 2: Dirty Entity Tracking

**User Story:** As a developer, I want PlayerContext to only persist changed entities on each write cycle, so that incremental writes are efficient regardless of backend.

**Dependency:** Requires Requirement 1 (PlayerContext uses IStorageBackend) to be implemented first. Dirty tracking has no purpose without backend integration.

#### Acceptance Criteria

1. THE PlayerContext SHALL track which entities have been modified since the last successful persistence operation, where modification means any of: an entity property was changed via a service method, an entity was added to a collection, or an entity was removed from a collection
2. THE PlayerContext SHALL expose a `MarkDirty<T>(string entityUUID)` method that service classes call after mutating an entity, and a `MarkDeleted<T>(string entityUUID)` method that service classes call after removing an entity from a collection
3. ALL existing service classes (ColonyService, BlueprintService, SurveyService, PlayerProfileService, DeliveryRouteService, DeliveryPlanService, ShipService, ShipTemplateService, StationService, MarketListingService, PricingPlanService, BuildPlanMutationService, StockTargetMutationService, SupplyChainMutationService, AsteroidService, ContactsService, BankingService, MailService) SHALL call MarkDirty after mutations and MarkDeleted after deletions
4. BackgroundProcessor SHALL NOT directly mutate colony entities; it SHALL route all colony mutations through ColonyService methods which handle dirty marking internally
4. WHEN WriteContext is called, THE PlayerContext SHALL persist only entities marked as dirty via the IStorageBackend Upsert methods, and delete only entities in the pending-deletion set via IStorageBackend Delete methods
5. WHEN persistence completes successfully, THE PlayerContext SHALL clear all dirty flags and all pending-deletion records
6. THE PlayerContext SHALL clear dirty flags one entity at a time as each Upsert succeeds within the write loop; if a write fails partway through, entities already successfully persisted have their dirty flags cleared, and entities not yet persisted retain their dirty flags for the next write attempt
7. WHEN a PlayerRoot is first loaded from a backend, THE PlayerContext SHALL start with zero dirty flags (no entities marked dirty) until a service method calls MarkDirty or MarkDeleted
8. WHEN WriteContext is called and the active backend is JsonSingleFileBackend, THE PlayerContext SHALL write the complete PlayerRoot file regardless of dirty flags, since that backend does not support per-entity persistence; dirty flags SHALL still be cleared after successful write

### Requirement 3: EmpireContext Storage Backend Integration

**User Story:** As a developer, I want EmpireContext to use IStorageBackend for baseline data, so that shared game data can be stored in any backend alongside player data.

#### Acceptance Criteria

1. THE EmpireContext SHALL accept an IStorageBackend instance via a public property and use it for loading and saving baseline data; WHEN no IStorageBackend is provided (null), THE EmpireContext SHALL fall back to direct file I/O using the configured FilePath (preserving current behavior)
2. THE EmpireContext SHALL also accept a StorageBackendType value alongside the IStorageBackend instance so it knows which load/save strategy to use without runtime type checks
3. WHEN StorageBackendType is JsonSingleFile or JsonMultiFile, THE EmpireContext SHALL call GetGlobalDataAsync with dataType "BaselineRoot" and deserialize the returned JSON string into a BaselineRoot object
4. WHEN StorageBackendType is Sqlite, DynamoDb, or Postgres, THE EmpireContext SHALL use the typed baseline methods (GetBaselineGameConstantsAsync, GetAllBlueprintTypesAsync, GetAllShipClassesAsync, GetAllTechLevelsAsync, GetAllCommoditiesAsync, GetAllRefiningRecipesAsync, GetAllResearchTimesAsync, GetAllPropertyTypeDefinitionsAsync) to load each baseline collection independently
5. IF baseline load methods return null or empty results (backend has no baseline data), THEN THE EmpireContext SHALL seed the backend by loading baseline data from the BaselineData.json file on disk, initializing all in-memory collections, and immediately persisting the loaded data into the backend via WriteContext; IF BaselineData.json also does not exist, THEN THE EmpireContext SHALL initialize with an empty BaselineRoot containing default BaselineGameConstants and empty entity arrays
6. WHEN EmpireContext saves baseline data via WriteContext and an IStorageBackend is configured, THE EmpireContext SHALL persist using the appropriate method for the configured StorageBackendType: UpsertGlobalDataAsync for JSON backends, typed Upsert methods for relational backends
7. IF the IStorageBackend throws a StorageLoadException during load, THEN THE EmpireContext SHALL propagate the exception to the caller without partial initialization
8. THE EmpireContext SHALL continue to expose data via existing IReadOnlyList properties (BlueprintTypeList, ShipClassList, TechLevelList, CommodityList, GlobalBlueprintList, ResourceList, PropertyTypeRegistry) and support all existing mutation methods regardless of which backend is configured
9. ALL calls from EmpireContext to IStorageBackend SHALL use the same async-to-sync bridging strategy as PlayerContext (Req 1 Criterion 10)

### ~~Requirement 4: Server Migration to Common Backends~~ — COMPLETE

**Status: Delivered by unified-storage-backend spec.** All criteria satisfied:
- Server/Storage/ folder removed; all local backend implementations deleted
- Server references OE2EmpireTracker.Common; uses Common IStorageBackend and StorageBackendFactory
- Server Program.cs reads Storage section from appsettings.json and creates backends
- "JsonFile" mapped to JsonMultiFileBackend for backward compatibility
- Invalid/missing backend config fails startup
- All 264 Server tests pass
- Entity models moved to Common/Models/

No further work needed for this requirement.

### Requirement 5: Backend Selection Preferences

**User Story:** As a desktop user, I want to choose my storage backend from preferences, so that I can switch between JSON, SQLite, and cloud storage based on my needs.

#### Acceptance Criteria

1. THE PreferencesStore SHALL include a StorageBackendType setting that defaults to JsonSingleFile for backward compatibility
2. THE PreferencesStore SHALL include a StoragePath setting for file-based backends (JsonSingleFile, JsonMultiFile, Sqlite)
3. WHEN StorageBackendType is JsonSingleFile or JsonMultiFile and StoragePath is not explicitly set, THE PreferencesStore SHALL default StoragePath to the directory containing the application executable
4. WHEN StorageBackendType is Sqlite and StoragePath is not explicitly set, THE PreferencesStore SHALL default StoragePath to %LocalAppData%/OE2EmpireTracker
5. WHEN StorageBackendType is DynamoDb, THE PreferencesStore SHALL accept AWS region (non-empty string) and table prefix (non-empty string) configuration
6. WHEN StorageBackendType is Postgres, THE PreferencesStore SHALL accept a connection string (non-empty string, maximum 1024 characters)
7. WHEN the desktop application starts, THE application SHALL use StorageBackendFactory.CreateAsync to create the IStorageBackend instance corresponding to the configured StorageBackendType and pass it to PlayerContext and EmpireContext
8. IF the StorageBackendType setting contains an unrecognized value or is missing, THEN THE PreferencesStore SHALL fall back to JsonSingleFile and log a warning indicating the invalid value that was encountered
9. IF StorageBackendFactory.CreateAsync throws during startup (e.g. SQLite file corrupt, Postgres unreachable, DynamoDB credentials invalid), THE application SHALL display an error dialog with the exception message and offer to fall back to JsonSingleFile with the default path or exit
10. Backend selection is configured via manual editing of UIPreferences.json for the initial implementation; a FormPreferences UI section for storage backend selection is deferred to a future spec


### Requirement 6: Data Migration Service

**User Story:** As a user switching storage backends, I want a migration service that copies all my data from one backend to another, so that I can change backends without losing data.

#### Acceptance Criteria

1. THE Migration_Service SHALL accept any source IStorageBackend and any destination IStorageBackend
2. THE Migration_Service SHALL migrate all entity types: server-global entities, all per-character entities (22 types), baseline data, sharing rules, permission entities, intel entities, and audit entries
3. THE IStorageBackend interface SHALL be extended with a `GetAllCharacterUUIDsAsync()` method that returns all character UUIDs that have stored player data; THE Migration_Service SHALL call this method on the source backend to discover which characters to migrate; the caller MAY also provide an explicit list of character UUIDs to migrate (for selective migration)
4. THE Migration_Service SHALL preserve every entity with no field loss during migration
5. THE Migration_Service SHALL accept an IProgress<MigrationProgress> callback and invoke it at least once per entity type, reporting the current entity type name and the cumulative count of entities processed so far
6. IF migration fails partway through, THEN THE Migration_Service SHALL throw a StorageWriteException (or StorageLoadException for read failures) whose message includes the entity type being processed when the failure occurred and the number of entities successfully migrated before failure; partial data already written to the destination SHALL remain in place (no rollback of successfully migrated entities)
7. THE Migration_Service SHALL validate entity counts after migration by comparing source counts to destination counts for each entity type
8. WHEN source and destination entity counts do not match for any entity type, THE Migration_Service SHALL throw a MigrationValidationException identifying the mismatched entity types and their expected (source) and actual (destination) counts
9. THE Migration_Service SHALL support migration between any pair of the 5 backends (JsonSingleFile, JsonMultiFile, Sqlite, DynamoDb, Postgres)
10. THE Migration_Service SHALL be async (Task-based) since it calls IStorageBackend methods directly; callers bridge to sync using the standard pattern (Req 1 Criterion 10) if needed

### Requirement 7: Round-Trip Fidelity

**User Story:** As a developer, I want property tests verifying that migrating data between any two backends preserves full data fidelity, so that users never lose precision or data during backend switches.

**Note:** The unified-storage-backend spec delivered foundational property tests (entity count preservation, atomic write safety). This requirement extends that coverage to full migration round-trip verification.

#### Acceptance Criteria

1. WHEN data is migrated from any backend A to any backend B and back to backend A, THE migrated data SHALL be structurally equivalent via deep-equality comparison; specifically, re-serializing the round-tripped entities with the project's standard JsonSerializerSettings and SerializationSorter SHALL produce identical JSON output to the original serialization
2. THE migration process SHALL preserve exact decimal precision for all numeric values (including banking balance, credit change, price per unit, and total price fields); the SQLite and Postgres backends SHALL store decimal values as TEXT (SQLite) or NUMERIC (Postgres) rather than REAL/DOUBLE PRECISION to avoid IEEE 754 floating-point precision loss; this is a prerequisite fix to the delivered backends before migration can guarantee fidelity
3. THE migration process SHALL preserve DateTime values as stored (ISO 8601 TEXT strings in all backends); both SQLite and Postgres backends store DateTime as TEXT with ISO 8601 format specifier "O", which preserves full tick-level precision including timezone information
4. THE migration process SHALL preserve null versus empty-collection distinctions for all collection properties
5. THE migration process SHALL preserve entity UUID stability (no UUID regeneration or modification during migration)
6. THE migration process SHALL preserve full structural equality including nested objects, arrays, all property values, string values (including Unicode characters, empty strings, and whitespace-only strings), and enum values
7. THE property tests SHALL cover all entity types defined in IStorageBackend (all 22 per-character player entity types and all baseline data types) using randomly generated entity instances with a minimum of 100 test cases per backend pair; tests SHALL cover at minimum the pairs: JsonSingleFile↔Sqlite, JsonSingleFile↔JsonMultiFile, Sqlite↔Postgres (3 pairs covering the most common migration paths)
8. THE property tests SHALL use deep-equality comparison asserting that every property on the round-tripped entity equals the original, including collection ordering, nested object graphs, and null-valued optional properties

### Requirement 8: PlayerContext Error Recovery

**User Story:** As a user, I want PlayerContext to block further writes when a storage failure occurs, so that I am notified of the problem and cannot accidentally overwrite data in a corrupted state.

**Note:** WritesBlocked is already implemented as a static bool on PlayerContext. This requirement formalizes the wiring to StorageWriteException that will be added when Requirement 1 is implemented.

#### Acceptance Criteria

1. WHEN the IStorageBackend throws a StorageWriteException during a PlayerContext write operation, THE PlayerContext SHALL set WritesBlocked to true and then propagate the StorageWriteException to the caller
2. WHILE WritesBlocked is true, THE PlayerContext SHALL return immediately from all subsequent write operations without throwing exceptions and without persisting any data (already implemented)
3. THE PlayerContext SHALL expose WritesBlocked as a public settable boolean property with a default value of false, allowing callers to set WritesBlocked to true independently of storage failures (already implemented)
4. WHEN a caller sets WritesBlocked to false, THE PlayerContext SHALL resume normal write behavior on the next write operation (already implemented)

## Implementation Phases

### Phase 2a: Prerequisite Backend Fixes
- Fix decimal precision in SQLite backend: REAL→TEXT for CreditChange, OldBalance, NewBalance, PricePerUnit, TotalPrice columns; remove `(double)` casts; add schema migration
- Fix decimal precision in Postgres backend: DOUBLE PRECISION→NUMERIC for same columns; remove `(double)` casts; add schema migration
- Add `GetAllCharacterUUIDsAsync()` to IStorageBackend and implement in all 5 backends
- Refactor BackgroundProcessor to route colony mutations through ColonyService instead of calling Colony.ProcessColony() directly

### Phase 2b: PlayerContext and EmpireContext Integration
- Refactor PlayerContext to accept IStorageBackend with async bridging (Reqs 1, 2, 8)
- Add MarkDirty/MarkDeleted to PlayerContext; update all 18 service classes (Req 2)
- Refactor EmpireContext to use IStorageBackend for BaselineRoot (Req 3)
- Add StorageBackendType/StoragePath to PreferencesStore (Req 5)
- Desktop app startup uses StorageBackendFactory.CreateAsync with error dialog fallback
- All existing WinForms tests pass (they operate without a backend — Req 1 Criterion 2)

### Phase 3: Data Migration Service and Round-Trip Tests
- Implement Migration_Service with progress, validation, and character discovery (Req 6)
- Implement round-trip fidelity property tests for key backend pairs (Req 7)
- End-to-end testing of backend switching via manual preferences editing
