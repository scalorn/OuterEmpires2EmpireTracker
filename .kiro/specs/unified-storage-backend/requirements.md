# Requirements Document

## Introduction

The OE2EmpireTracker ecosystem currently has fragmented storage implementations: the WinForms/Desktop app writes monolithic JSON files via PlayerContext/EmpireContext singletons, while the Server project has its own `IStorageBackend` interface with JSON multi-file, SQLite, DynamoDB, and Postgres implementations. This feature unifies ALL storage behind a single shared `IStorageBackend` interface in OE2EmpireTracker.Common, moving all backend implementations (JSON single-file, JSON multi-file, SQLite, DynamoDB, Postgres) into Common. All applications — WinForms Desktop, the Faction Server, and any future apps — consume the same interface and can use any backend interchangeably. The migration service allows lossless data movement between any pair of backends.

## Glossary

- **IStorageBackend**: The unified async interface in OE2EmpireTracker.Common defining fine-grained CRUD operations for all entity types (player data, baseline data, faction/server entities, permissions, intel, audit)
- **JSON_SingleFile_Backend**: An IStorageBackend implementation that persists all player data as a single monolithic JSON file (the current WinForms format) and server-global data in separate files
- **JSON_MultiFile_Backend**: An IStorageBackend implementation that persists data as separate JSON files per entity type and per character (the current Server format)
- **SQLite_Backend**: An IStorageBackend implementation that persists data in a single SQLite database file with per-entity-type tables
- **DynamoDB_Backend**: An IStorageBackend implementation that persists data in AWS DynamoDB tables
- **Postgres_Backend**: An IStorageBackend implementation that persists data in a PostgreSQL database
- **Migration_Service**: A component that reads all data from one IStorageBackend and writes it to another, preserving all entities and relationships
- **PlayerContext**: The singleton in-memory repository that holds player entity data, exposes it via IReadOnlyList properties, fires change events, and delegates persistence to IStorageBackend
- **EmpireContext**: The singleton in-memory repository for baseline/shared game data (blueprint types, ship classes, commodities, etc.)
- **PlayerRoot**: The serialization root object containing all player entity arrays (22 entity types)
- **BaselineRoot**: The serialization root object containing all baseline entity arrays
- **StorageBackendType**: An enum in Common listing all available backend implementations (JsonSingleFile, JsonMultiFile, Sqlite, DynamoDb, Postgres)

## Requirements

### Requirement 1: Unified Storage Interface in Common

**User Story:** As a developer, I want a single storage interface in OE2EmpireTracker.Common that covers all entity types (player data, baseline data, faction/server management, permissions, intel, audit), so that any application can use any backend without coupling to specific persistence.

#### Acceptance Criteria

1. THE IStorageBackend interface SHALL reside in OE2EmpireTracker.Common and define async Task-based CRUD methods for all entity types currently in the Server's IStorageBackend
2. THE IStorageBackend interface SHALL include initialization and connection validation methods (InitializeAsync, ValidateConnectionAsync)
3. THE IStorageBackend interface SHALL include CRUD methods for server-global entities: ServerFaction, ServerCharacter, ApiToken, MembershipAction, StarSystem, ColonySummary
4. THE IStorageBackend interface SHALL include CRUD methods for per-character player entities: Colony, Blueprint, Survey, PlayerProfile, DeliveryRoute, DeliveryPlan, Ship, ShipTemplate, MarketListing, MarketTransaction, PricingPlan, StockPlan, StockProfile, BuildPlan, SupplyChain, Asteroid, Station, Faction, ExternalCharacter
5. THE IStorageBackend interface SHALL include CRUD methods for sharing rules (per-character) and character preferences
6. THE IStorageBackend interface SHALL include CRUD methods for the permission system: FactionCapability, FactionClearanceLevel, FactionPermissionGroup, FactionGroupCapability, FactionGroupSharingRule, FactionMemberPermissions, FactionMemberCapability, CharacterCapability, CharacterClearanceLevel, CharacterPermissionGroup, CharacterGroupCapability, CharacterGroupSharingRule, CharacterGranteePermissions, CharacterGranteeCapability
7. THE IStorageBackend interface SHALL include CRUD methods for intel: IntelComment, IntelCommentFactionShare
8. THE IStorageBackend interface SHALL include methods for permission audit entries (append, query with filters, delete expired)
9. THE IStorageBackend interface SHALL include a method for global data storage (GetGlobalDataAsync, UpsertGlobalDataAsync) for baseline data types
10. THE IStorageBackend interface SHALL target netstandard2.0 to remain compatible with .NET Framework 4.8.1 (WinForms) and .NET 8 (Server)
11. THE IStorageBackend interface SHALL expose a GetStorageInfo method that returns the backend type name and storage location


### Requirement 2: JSON Single-File Backend

**User Story:** As a desktop user, I want to continue using the current single-file JSON format, so that I can manually inspect, edit, and share my data files as before.

#### Acceptance Criteria

1. THE JSON_SingleFile_Backend SHALL implement IStorageBackend by reading/writing all player entity data from a single PlayerData.json (or Alpha3.json) file
2. THE JSON_SingleFile_Backend SHALL implement baseline operations by reading/writing BaselineData.json
3. THE JSON_SingleFile_Backend SHALL use SafeFileWriter (temp-file, atomic replace, .bak backup) for all write operations
4. THE JSON_SingleFile_Backend SHALL produce output identical to the current PlayerContext.WriteContext() serialization format, including SerializationSorter ordering and JsonSerializerSettings
5. THE JSON_SingleFile_Backend SHALL reside in OE2EmpireTracker.Common
6. WHEN a JSON file does not exist at the configured path OR exists but contains no player entities, THE JSON_SingleFile_Backend SHALL return empty collections for all entity queries
7. IF a JSON file contains malformed JSON, THEN THE JSON_SingleFile_Backend SHALL throw a StorageLoadException immediately with the file path and inner exception details, without attempting any recovery
8. THE JSON_SingleFile_Backend SHALL support per-character entity operations by maintaining the full PlayerRoot in memory and writing the complete file on each save
9. THE JSON_SingleFile_Backend SHALL apply existing data migrations (DataVersion upgrades) when loading older PlayerRoot files
10. WHEN loading a PlayerRoot with a DataVersion equal to the current version, THE JSON_SingleFile_Backend SHALL still validate migration ordering logic

### Requirement 3: JSON Multi-File Backend

**User Story:** As a server operator, I want to continue using the multi-file JSON format where each entity type has its own file per character, so that individual entity updates don't rewrite all data.

#### Acceptance Criteria

1. THE JSON_MultiFile_Backend SHALL implement IStorageBackend by persisting per-character entity data in separate JSON files organized by character UUID and entity type
2. THE JSON_MultiFile_Backend SHALL persist server-global entities (ServerFaction, ServerCharacter, ApiToken, MembershipAction, StarSystem) in dedicated JSON files in the root data directory
3. THE JSON_MultiFile_Backend SHALL persist permission-system entities in per-faction or per-character subdirectories matching the current Server layout
4. THE JSON_MultiFile_Backend SHALL use atomic write operations (temp-file then rename) for all file writes
5. THE JSON_MultiFile_Backend SHALL reside in OE2EmpireTracker.Common
6. WHEN a file does not exist for a requested entity type, THE JSON_MultiFile_Backend SHALL return an empty collection
7. IF a file contains malformed JSON, THEN THE JSON_MultiFile_Backend SHALL throw a StorageLoadException with the file path and inner exception
8. THE JSON_MultiFile_Backend SHALL create the directory structure on first initialization (InitializeAsync)
9. THE JSON_MultiFile_Backend SHALL match the current Server JsonFileStorageBackend directory layout for backward compatibility with existing server deployments


### Requirement 4: SQLite Backend

**User Story:** As a user, I want to store data in SQLite for faster incremental writes and reduced corruption risk compared to JSON files.

#### Acceptance Criteria

1. THE SQLite_Backend SHALL implement IStorageBackend using a single SQLite database file, and SHALL NOT activate any SQLite features (dedicated tables, JSON serialization, transactions) unless the full IStorageBackend interface is properly implemented
2. THE SQLite_Backend SHALL store each entity type in a dedicated table with the entity UUID as primary key
3. THE SQLite_Backend SHALL serialize individual entity objects as JSON text columns to preserve the full data model without schema-per-field mapping
4. THE SQLite_Backend SHALL use WAL (Write-Ahead Logging) journal mode for concurrent read performance and crash safety
5. THE SQLite_Backend SHALL create all required tables and schema on first initialization when the database file does not exist
6. THE SQLite_Backend SHALL reside in OE2EmpireTracker.Common
7. IF the SQLite database file is corrupted, inaccessible, has schema mismatches, or encounters permission errors, THEN THE SQLite_Backend SHALL throw a StorageLoadException with available diagnostic details (the exception MAY omit specific diagnostics if they cannot be determined, but SHALL still be thrown)
8. THE SQLite_Backend SHALL use transactions for multi-entity write operations to ensure atomicity

### Requirement 5: DynamoDB Backend

**User Story:** As a server operator deploying to AWS, I want to use DynamoDB as the storage backend for serverless scalability and managed infrastructure.

#### Acceptance Criteria

1. THE DynamoDB_Backend SHALL implement IStorageBackend using AWS DynamoDB tables
2. THE DynamoDB_Backend SHALL match the current Server DynamoStorageBackend table structure and access patterns for backward compatibility
3. THE DynamoDB_Backend SHALL reside in OE2EmpireTracker.Common
4. THE DynamoDB_Backend SHALL use the AWSSDK.DynamoDBv2 package for all DynamoDB operations
5. IF DynamoDB is unreachable or returns errors, THEN THE DynamoDB_Backend SHALL throw a StorageLoadException or StorageWriteException with the operation details and inner exception

### Requirement 6: Postgres Backend

**User Story:** As a server operator preferring relational databases, I want to use PostgreSQL as the storage backend for ACID transactions, full SQL query capability, and managed hosting options.

#### Acceptance Criteria

1. THE Postgres_Backend SHALL implement IStorageBackend using a PostgreSQL database
2. THE Postgres_Backend SHALL match the current Server PostgresStorageBackend schema and query patterns for backward compatibility
3. THE Postgres_Backend SHALL reside in OE2EmpireTracker.Common
4. THE Postgres_Backend SHALL use the Npgsql package for PostgreSQL connectivity
5. IF PostgreSQL is unreachable or returns errors, THEN THE Postgres_Backend SHALL implement retry logic (exponential backoff, configurable max retries) before throwing a StorageLoadException or StorageWriteException with operation details and inner exception


### Requirement 7: PlayerContext Integration

**User Story:** As a developer, I want PlayerContext to consume the unified IStorageBackend for all persistence, so that the in-memory cache and event system work identically regardless of which backend is active.

#### Acceptance Criteria

1. THE PlayerContext SHALL accept an IStorageBackend instance and use it for all load and save operations instead of directly reading/writing JSON files; the PlayerContext SHALL NOT load or cache data unless a backend is provided, and SHALL throw InvalidOperationException if data operations are attempted without a backend
2. THE PlayerContext SHALL load all per-character entity data at startup by calling GetAll*Async methods on the IStorageBackend and caching results in memory
3. THE PlayerContext SHALL continue to expose data via IReadOnlyList properties and fire all existing change events (ColonyDataChanged, BlueprintDataChanged, etc.) regardless of backend
4. WHEN WriteContext is called, THE PlayerContext SHALL persist data by calling the appropriate Upsert/Delete methods on the IStorageBackend
5. THE PlayerContext SHALL maintain backward compatibility with the existing ServerOnly mode delegates (IsServerOnlyMode, PushToServer)
6. THE EmpireContext SHALL accept an IStorageBackend instance and use it for loading and saving BaselineRoot data via the global data methods
7. THE PlayerContext SHALL track dirty entities and only persist changed entities rather than rewriting all data on every save (incremental writes)

### Requirement 8: Backend Selection and Configuration

**User Story:** As a user or operator, I want to configure which storage backend to use through application settings, so that each deployment can choose the backend that fits its needs.

#### Acceptance Criteria

1. THE StorageBackendType enum SHALL define values: JsonSingleFile, JsonMultiFile, Sqlite, DynamoDb, Postgres
2. THE Preferences_Store SHALL include a StorageBackendType setting defaulting to JsonSingleFile for backward compatibility with existing WinForms installations
3. WHEN the application starts, THE application SHALL instantiate the IStorageBackend implementation corresponding to the configured StorageBackendType, and SHALL fail startup immediately if instantiation fails
4. THE Preferences_Store SHALL include a StoragePath setting specifying the directory or connection string for the active backend
5. THE Preferences_Store SHALL default StoragePath to the current application directory for JSON backends and %LocalAppData%/OE2EmpireTracker for SQLite
6. FOR DynamoDB and Postgres backends, THE configuration SHALL accept connection strings or AWS region/table configuration as appropriate

### Requirement 9: Backend Factory

**User Story:** As a developer, I want a factory that creates the correct backend from configuration, so that startup logic is decoupled from backend implementation details.

#### Acceptance Criteria

1. THE StorageBackendFactory SHALL accept a StorageBackendType value and configuration parameters and return the corresponding IStorageBackend instance
2. THE StorageBackendFactory SHALL reside in OE2EmpireTracker.Common alongside the interfaces and implementations
3. IF an unsupported StorageBackendType value is provided, THEN THE StorageBackendFactory SHALL throw an ArgumentException immediately describing the invalid value, without calling InitializeAsync
4. THE StorageBackendFactory SHALL call InitializeAsync on the created backend before returning it to ensure the backend is ready for use; IF InitializeAsync throws, THEN the exception SHALL propagate to the caller and no IStorageBackend instance SHALL be returned


### Requirement 10: Data Migration Between Backends

**User Story:** As a user, I want to migrate my data from any storage backend to any other, so that I can switch backends without losing data.

#### Acceptance Criteria

1. THE Migration_Service SHALL accept any source IStorageBackend and any destination IStorageBackend and transfer all data between them
2. THE Migration_Service SHALL migrate all entity types: server-global entities, per-character player entities (all 22 types), baseline data, sharing rules, permission entities, intel, and audit entries
3. THE Migration_Service SHALL preserve every entity with no field loss during migration
4. THE Migration_Service SHALL report progress during migration by providing the current entity type being migrated and a count of entities processed
5. IF migration fails partway through, THEN THE Migration_Service SHALL report the entity type being processed when the failure occurred; the source backend MAY be in a partially modified state if the failure occurs after some entities have already been processed
6. THE Migration_Service SHALL validate entity counts after migration by comparing source and destination counts for each entity type
7. WHEN entity counts do not match between source and destination, THE Migration_Service SHALL throw a MigrationValidationException listing the mismatched types and counts
8. THE Migration_Service SHALL support migration between any pair of the 5 backends (JsonSingleFile ↔ JsonMultiFile ↔ Sqlite ↔ DynamoDb ↔ Postgres)

### Requirement 11: Migration Round-Trip Fidelity

**User Story:** As a developer, I want to verify that data survives round-trip migration between any two backends with zero loss, so that backend choice is purely operational and never affects data integrity.

#### Acceptance Criteria

1. FOR ALL valid data sets, migrating from any backend A to any backend B and back to A SHALL produce data equivalent to the original (round-trip property)
2. THE Migration_Service SHALL preserve decimal precision for banking balance values during round-trip migration, and SHALL fail the entire migration if any decimal value loses precision
3. THE Migration_Service SHALL preserve DateTime values with full tick-level precision during round-trip migration
4. THE Migration_Service SHALL preserve null versus empty-collection distinctions for optional collection properties during round-trip migration
5. THE Migration_Service SHALL preserve entity UUID stability (same UUID in source produces same UUID in destination)

### Requirement 12: SQLite Schema Management

**User Story:** As a developer, I want the SQLite backend to manage its own schema versioning, so that future entity additions or schema changes are applied automatically on startup.

#### Acceptance Criteria

1. THE SQLite_Backend SHALL maintain a metadata table with a schema_version integer column
2. WHEN the SQLite database is opened, THE SQLite_Backend SHALL compare the on-disk schema_version with the expected version and apply any pending migrations sequentially
3. THE SQLite_Backend SHALL define the initial schema (version 1) with tables for all entity types and the metadata table
4. WHEN a new entity type is added in a future version, THE SQLite_Backend SHALL add the corresponding table via a numbered migration without affecting existing data
5. IF a schema migration fails, THEN THE SQLite_Backend SHALL roll back the transaction and throw a StorageLoadException describing which migration step failed; this SHALL apply to failures at any point in the migration process including pre-migration validation
6. IF the transaction rollback itself fails after a schema migration failure, THEN THE SQLite_Backend SHALL throw a StorageCorruptionException with both the migration error and rollback error details


### Requirement 13: Server Migration to Common Backends

**User Story:** As a developer, I want the Faction Server to consume the IStorageBackend from Common instead of its local implementations, so that backend code is shared and maintained in one place.

#### Acceptance Criteria

1. THE Server project SHALL remove its local IStorageBackend interface and all local backend implementations (JsonFileStorageBackend, SqliteStorageBackend, DynamoStorageBackend, PostgresStorageBackend)
2. THE Server project SHALL reference OE2EmpireTracker.Common and use the unified IStorageBackend interface for all storage operations
3. THE Server project SHALL configure the backend via its existing appsettings.json StorageBackend configuration section, and the migration SHALL be considered complete only when the system actually uses Common backends (not just preserving the config structure)
4. THE Server project SHALL continue to pass all existing integration tests after switching to the Common backends
5. THE Server project's Models.cs and PermissionModels.cs entity classes SHALL move to OE2EmpireTracker.Common/Models so they are shared across all applications

### Requirement 14: Error Handling and Recovery

**User Story:** As a user, I want clear error messages and safe recovery when storage operations fail, so that I never lose data due to backend issues.

#### Acceptance Criteria

1. IF a write operation fails on any backend, THEN THE backend SHALL throw a StorageWriteException with the backend type, operation details, and inner exception
2. IF a read/load operation fails on any backend, THEN THE backend SHALL throw a StorageLoadException with the backend type, file path or connection info, and inner exception
3. THE PlayerContext SHALL set WritesBlocked to true and log an error when the IStorageBackend throws a StorageWriteException, preventing further write attempts until the issue is resolved; the PlayerContext SHALL also block writes for any error condition that could compromise data integrity (network issues, application-level constraints, etc.)
4. THE PlayerContext SHALL be able to set WritesBlocked to true independently of storage failures (e.g., for application-level locking)
5. FOR transactional backends (SQLite, Postgres), failed write operations SHALL roll back the transaction before throwing StorageWriteException
6. FOR file-based backends (JSON single-file, JSON multi-file), failed write operations SHALL leave existing files intact via the atomic write strategy

### Requirement 15: Serialization Compatibility

**User Story:** As a user upgrading to the new unified storage system, I want my existing data files to load without any manual conversion, so that the upgrade is seamless.

#### Acceptance Criteria

1. THE JSON_SingleFile_Backend SHALL read existing PlayerData.json and Alpha3.json files produced by the current PlayerContext.WriteContext() without modification
2. THE JSON_SingleFile_Backend SHALL read existing BaselineData.json files produced by the current EmpireContext.WriteContext() without modification
3. THE JSON_SingleFile_Backend SHALL produce byte-identical output to the current serialization (same JsonSerializerSettings, Formatting.Indented, NullValueHandling, SerializationSorter ordering)
4. THE JSON_MultiFile_Backend SHALL read existing server data directories without modification
5. THE SQLite_Backend SHALL read existing server SQLite databases without modification
6. WHEN loading older data versions, THE backend SHALL apply data migrations in the correct order to bring data to the current version; IF a migration fails partway through, THE backend SHALL allow the system to continue with the partially migrated data and log a warning

## Implementation Phases

### Phase 1: Move Interface and Backends to Common
- Move IStorageBackend interface to Common
- Move all entity model classes (Models.cs, PermissionModels.cs) to Common/Models
- Move all backend implementations to Common (JSON multi-file, SQLite, DynamoDB, Postgres)
- Create the JSON single-file backend in Common
- Create StorageBackendType enum and StorageBackendFactory
- Server project references Common backends instead of local implementations
- All existing server tests pass against Common backends

### Phase 2: PlayerContext Refactoring
- Refactor PlayerContext to accept IStorageBackend and use CRUD methods
- Refactor EmpireContext to use IStorageBackend for baseline data
- Implement dirty-entity tracking for incremental writes
- Desktop app startup uses StorageBackendFactory
- All existing WinForms tests pass

### Phase 3: Migration Service and Preferences
- Implement Migration_Service supporting all backend pairs
- Add backend selection to preferences UI
- Implement round-trip fidelity property tests
- Add migration progress reporting
- End-to-end testing of backend switching

