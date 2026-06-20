# Requirements Document

## Introduction

This spec covers Phase 2 and Phase 3 of the unified storage backend migration: integrating consumers (PlayerContext, EmpireContext, Server) with the unified IStorageBackend interface, adding backend selection preferences, implementing a data migration service, and verifying round-trip fidelity across all backend pairs.

**Prerequisite:** The `unified-storage-backend` spec (Phase 1) must be complete before this work begins. That spec delivers the IStorageBackend interface, all 5 backend implementations (JsonSingleFile, JsonMultiFile, SQLite, DynamoDB, Postgres), StorageBackendFactory, StorageBackendType enum, error types (StorageLoadException, StorageWriteException, StorageCorruptionException), and all entity models — all residing in OE2EmpireTracker.Common.

## Glossary

- **IStorageBackend**: The unified async interface in OE2EmpireTracker.Common defining CRUD operations for all entity types (delivered by the unified-storage-backend spec)
- **PlayerContext**: The singleton service in OE2EmpireTracker.Common that manages per-character player data, persistence, and change events
- **EmpireContext**: The singleton service in OE2EmpireTracker.Common that manages shared baseline game data (blueprint types, ship classes, commodities)
- **BaselineRoot**: The serialization root object containing all baseline entity arrays
- **Migration_Service**: A service that reads all entity data from any source IStorageBackend and writes it to any destination IStorageBackend
- **StorageBackendType**: The enum (JsonSingleFile, JsonMultiFile, Sqlite, DynamoDb, Postgres) delivered by the unified-storage-backend spec
- **StorageBackendFactory**: The factory class that creates backend instances from configuration, delivered by the unified-storage-backend spec
- **PreferencesStore**: The user preferences persistence service in OE2EmpireTracker.Desktop that stores application settings
- **StorageWriteException**: Exception thrown by IStorageBackend when a write operation fails (delivered by the unified-storage-backend spec)
- **StorageLoadException**: Exception thrown by IStorageBackend when a read/load operation fails (delivered by the unified-storage-backend spec)
- **MigrationValidationException**: Exception thrown by Migration_Service when post-migration entity count validation fails
- **WritesBlocked**: A boolean property on PlayerContext indicating that persistence is disabled due to a storage failure or explicit caller action
- **Dirty_Entity_Tracking**: A mechanism in PlayerContext that marks entities as modified so that only changed entities are persisted on the next write, rather than rewriting all data
- **ServerOnly_Mode**: The mode where PlayerContext delegates persistence to a remote server via delegates instead of using local IStorageBackend

## Requirements

### Requirement 1: PlayerContext Storage Backend Integration

**User Story:** As a developer, I want PlayerContext to use IStorageBackend for all persistence, so that the desktop app can switch between JSON, SQLite, and cloud storage without modifying PlayerContext logic.

#### Acceptance Criteria

1. THE PlayerContext SHALL accept an IStorageBackend instance via a constructor parameter and use the IStorageBackend for all load and save operations instead of directly reading or writing JSON files
2. IF a caller invokes WriteContext, LoadFromServerAsync, or any entity Add/Remove/Update method on a PlayerContext instance that has no IStorageBackend configured, THEN THE PlayerContext SHALL throw InvalidOperationException
3. WHEN PlayerContext is initialized with a backend and a CurrentPlayerUUID is set, THE PlayerContext SHALL load all per-character entity data for that player by calling the corresponding GetAll methods on the IStorageBackend before firing CurrentPlayerChanged
4. THE PlayerContext SHALL continue to expose data via IReadOnlyList properties and fire all existing change events (CurrentPlayerChanged, ColonyDataChanged, BlueprintDataChanged, SurveyDataChanged, DeliveryDataChanged, PricingDataChanged, BuildPlanDataChanged, MarketDataChanged, StationDataChanged, AsteroidDataChanged, PlayerProfileDataChanged, ShipTemplateDataChanged, ShipDataChanged, StockDataChanged, SupplyChainDataChanged, ContactDataChanged, BankingDataChanged, MailDataChanged) regardless of which backend is configured
5. WHEN WriteContext is called, THE PlayerContext SHALL persist the full current entity state by calling Upsert on the IStorageBackend for all entity collections currently held in memory, matching the same set of entity types serialized today (PlayerProfile, Blueprint, Survey, Colony, DeliveryRoute, DeliveryPlan, PricingPlan, BuildPlan, ShipTemplate, Ship, Station, MarketListing, MarketTransaction, StockPlan, StockProfile, SupplyChain, WarehouseOverflowRule, Faction, ExternalCharacter, Asteroid, BankingTransaction, MailMessage)
6. THE PlayerContext SHALL maintain backward compatibility with existing ServerOnly mode delegates (IsServerOnlyMode, PushToServer, ExportFromServer, IsServerConnected) such that when an IStorageBackend is NOT configured, the delegates continue to control persistence behavior as they do today
7. THE PlayerContext SHALL provide a parameterless constructor that does not configure a backend and SHALL expose a public method or property to assign an IStorageBackend after construction, enabling existing callers to configure the backend before triggering data load
8. IF an IStorageBackend method throws during WriteContext, THEN THE PlayerContext SHALL NOT suppress the exception and SHALL leave the in-memory entity state unchanged (no data loss on write failure)

### Requirement 2: Dirty Entity Tracking

**User Story:** As a developer, I want PlayerContext to only persist changed entities on each write cycle, so that incremental writes are efficient regardless of backend.

#### Acceptance Criteria

1. THE PlayerContext SHALL track which entities have been modified since the last successful persistence operation, where modification means any of: an entity property was changed via a service method, an entity was added to a collection, or an entity was replaced in a collection
2. WHEN WriteContext is called, THE PlayerContext SHALL persist only entities marked as dirty via the IStorageBackend Upsert methods
3. WHEN WriteContext is called, THE PlayerContext SHALL delete only entities that were removed from in-memory collections since the last successful persistence operation, via the IStorageBackend Delete methods
4. WHEN persistence completes successfully, THE PlayerContext SHALL clear all dirty flags and all pending-deletion records
5. IF persistence fails partway through, THEN THE PlayerContext SHALL retain dirty flags for entities that were not successfully persisted, and SHALL retain pending-deletion records for entities whose deletion was not confirmed
6. WHEN a PlayerRoot is first loaded from a backend, THE PlayerContext SHALL start with zero dirty flags (no entities marked dirty) until a service method modifies, adds, or removes an entity
7. WHEN WriteContext is called and the active backend is JSON_SingleFile_Backend, THE PlayerContext SHALL write the complete PlayerRoot file regardless of dirty flags, since that backend does not support per-entity persistence

### Requirement 3: EmpireContext Storage Backend Integration

**User Story:** As a developer, I want EmpireContext to use IStorageBackend for baseline data, so that shared game data can be stored in any backend alongside player data.

#### Acceptance Criteria

1. THE EmpireContext SHALL accept an IStorageBackend instance via constructor parameter and use it for loading and saving BaselineRoot data; WHEN no IStorageBackend is provided (null), THE EmpireContext SHALL fall back to direct file I/O using the configured FilePath (preserving current behavior)
2. WHEN EmpireContext loads baseline data from an IStorageBackend, THE EmpireContext SHALL call GetGlobalDataAsync with dataType "BaselineRoot" and deserialize the returned JSON string into a BaselineRoot object using the existing JsonSerializerSettings
3. IF GetGlobalDataAsync returns null for dataType "BaselineRoot", THEN THE EmpireContext SHALL initialize with an empty BaselineRoot containing default BaselineGameConstants and empty entity arrays
4. WHEN EmpireContext saves baseline data via WriteContext and an IStorageBackend is configured, THE EmpireContext SHALL serialize the BaselineRoot using the existing JsonSerializerSettings (including SerializationSorter ordering) and call UpsertGlobalDataAsync with dataType "BaselineRoot" and the serialized JSON string
5. IF the IStorageBackend throws a StorageLoadException during load, THEN THE EmpireContext SHALL propagate the exception to the caller without partial initialization
6. THE EmpireContext SHALL continue to expose data via existing IReadOnlyList properties (BlueprintTypeList, ShipClassList, TechLevelList, CommodityList, GlobalBlueprintList, ResourceList, PropertyTypeRegistry) and support all existing mutation methods regardless of which backend is configured

### Requirement 4: Server Migration to Common Backends

**User Story:** As a server operator, I want the Server project to use the unified backends from Common, so that backend implementations are shared and maintained in one place.

#### Acceptance Criteria

1. THE Server project SHALL remove its local IStorageBackend interface and all local backend implementation classes (JsonFileStorageBackend, SqliteStorageBackend, DynamoStorageBackend, PostgresStorageBackend) from the Storage folder
2. THE Server project SHALL add a project reference to OE2EmpireTracker.Common and use the Common IStorageBackend interface and backend implementations for all storage operations, updating namespace imports from OE2EmpireTracker.Server.Storage to the Common namespace throughout all consuming files
3. THE Server project SHALL register the IStorageBackend in its dependency injection container by reading the Storage section from appsettings.json, mapping the configured backend string to a StorageBackendType value, and calling StorageBackendFactory to obtain the instance
4. IF the appsettings.json Storage:Backend value is "JsonFile", THEN THE Server project SHALL map it to StorageBackendType.JsonMultiFile for backward compatibility with existing deployments
5. IF the appsettings.json Storage section is missing or the Backend value does not map to a known StorageBackendType, THEN THE Server project SHALL fail startup immediately with an error message indicating the invalid or missing backend configuration
6. THE Server project SHALL continue to pass all existing integration tests in OE2EmpireTracker.Server.Tests after migrating to Common backends, with no changes to test assertions or expected behavior
7. THE Server project entity model classes (Models.cs, PermissionModels.cs) SHALL move to OE2EmpireTracker.Common/Models, and all Server files referencing those types SHALL update their using directives accordingly

### Requirement 5: Backend Selection Preferences

**User Story:** As a desktop user, I want to choose my storage backend from preferences, so that I can switch between JSON, SQLite, and cloud storage based on my needs.

#### Acceptance Criteria

1. THE PreferencesStore SHALL include a StorageBackendType setting that defaults to JsonSingleFile for backward compatibility
2. THE PreferencesStore SHALL include a StoragePath setting for file-based backends (JsonSingleFile, JsonMultiFile, Sqlite)
3. WHEN StorageBackendType is JsonSingleFile or JsonMultiFile and StoragePath is not explicitly set, THE PreferencesStore SHALL default StoragePath to the directory containing the application executable
4. WHEN StorageBackendType is Sqlite and StoragePath is not explicitly set, THE PreferencesStore SHALL default StoragePath to %LocalAppData%/OE2EmpireTracker
5. WHEN StorageBackendType is DynamoDb, THE PreferencesStore SHALL accept AWS region (non-empty string) and table prefix (non-empty string) configuration
6. WHEN StorageBackendType is Postgres, THE PreferencesStore SHALL accept a connection string (non-empty string, maximum 1024 characters)
7. WHEN the desktop application starts, THE application SHALL use StorageBackendFactory to create the IStorageBackend instance corresponding to the configured StorageBackendType and StoragePath
8. IF the StorageBackendType setting contains an unrecognized value or is missing, THEN THE PreferencesStore SHALL fall back to JsonSingleFile and log a warning indicating the invalid value that was encountered

### Requirement 6: Data Migration Service

**User Story:** As a user switching storage backends, I want a migration service that copies all my data from one backend to another, so that I can change backends without losing data.

#### Acceptance Criteria

1. THE Migration_Service SHALL accept any source IStorageBackend and any destination IStorageBackend
2. THE Migration_Service SHALL migrate all entity types: server-global entities, all per-character entities (22 types), baseline data, sharing rules, permission entities, intel entities, and audit entries
3. THE Migration_Service SHALL preserve every entity with no field loss during migration
4. THE Migration_Service SHALL accept an IProgress<MigrationProgress> callback and invoke it at least once per entity type, reporting the current entity type name and the cumulative count of entities processed so far
5. IF migration fails partway through, THEN THE Migration_Service SHALL throw a StorageWriteException (or StorageLoadException for read failures) whose message includes the entity type being processed when the failure occurred and the number of entities successfully migrated before failure; partial data already written to the destination SHALL remain in place (no rollback of successfully migrated entities)
6. THE Migration_Service SHALL validate entity counts after migration by comparing source counts to destination counts for each entity type
7. WHEN source and destination entity counts do not match for any entity type, THE Migration_Service SHALL throw a MigrationValidationException identifying the mismatched entity types and their expected (source) and actual (destination) counts
8. THE Migration_Service SHALL support migration between any pair of the 5 backends (JsonSingleFile, JsonMultiFile, Sqlite, DynamoDb, Postgres)

### Requirement 7: Round-Trip Fidelity

**User Story:** As a developer, I want property tests verifying that migrating data between any two backends preserves full data fidelity, so that users never lose precision or data during backend switches.

#### Acceptance Criteria

1. WHEN data is migrated from any backend A to any backend B and back to backend A (for all 20 ordered pairs of the 5 StorageBackendType values), THE migrated data SHALL be byte-for-byte equivalent when re-serialized, confirming no data transformation occurred
2. THE migration process SHALL preserve decimal precision for all numeric values (including banking balance fields) by retaining the exact decimal representation with no floating-point rounding or truncation of significant digits
3. THE migration process SHALL preserve DateTime values with tick-level precision (100-nanosecond .NET ticks, no rounding or truncation)
4. THE migration process SHALL preserve null versus empty-collection distinctions for all collection properties
5. THE migration process SHALL preserve entity UUID stability (no UUID regeneration or modification during migration)
6. THE migration process SHALL preserve full structural equality including nested objects, arrays, all property values, string values (including Unicode characters, empty strings, and whitespace-only strings), and enum values
7. THE property tests SHALL cover all entity types defined in IStorageBackend (all 22 per-character player entity types and all baseline data types) using randomly generated entity instances with a minimum of 100 test cases per backend pair
8. THE property tests SHALL use deep-equality comparison asserting that every property on the round-tripped entity equals the original, including collection ordering, nested object graphs, and null-valued optional properties

### Requirement 8: PlayerContext Error Recovery

**User Story:** As a user, I want PlayerContext to block further writes when a storage failure occurs, so that I am notified of the problem and cannot accidentally overwrite data in a corrupted state.

#### Acceptance Criteria

1. WHEN the IStorageBackend throws a StorageWriteException during a PlayerContext write operation, THE PlayerContext SHALL set WritesBlocked to true and then propagate the StorageWriteException to the caller
2. WHILE WritesBlocked is true, THE PlayerContext SHALL return immediately from all subsequent write operations without throwing exceptions and without persisting any data
3. THE PlayerContext SHALL expose WritesBlocked as a public settable boolean property with a default value of false, allowing callers to set WritesBlocked to true independently of storage failures
4. WHEN a caller sets WritesBlocked to false, THE PlayerContext SHALL resume normal write behavior on the next write operation

## Implementation Phases

### Phase 2: PlayerContext and EmpireContext Integration
- Refactor PlayerContext to accept IStorageBackend (Reqs 1, 2, 8)
- Refactor EmpireContext to use IStorageBackend for BaselineRoot (Req 3)
- Add StorageBackendType/StoragePath to PreferencesStore (Req 5)
- Desktop app startup uses StorageBackendFactory
- All existing WinForms tests pass

### Phase 3: Server Migration and Data Migration Service
- Server project removes local implementations, references Common (Req 4)
- Implement Migration_Service with progress and validation (Req 6)
- Implement round-trip fidelity property tests (Req 7)
- End-to-end testing of backend switching
