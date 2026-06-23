# Requirements Document

## Introduction

This spec covers building the unified IStorageBackend interface and all five backend implementations (JSON single-file, JSON multi-file, SQLite, DynamoDB, Postgres) in OE2EmpireTracker.Common. It also covers the StorageBackendType enum, the StorageBackendFactory, error types, SQLite schema management, and serialization compatibility. This spec does NOT cover migrating consumers (PlayerContext, EmpireContext, Server) to use the new interface, nor the MigrationService for data transfer between backends — those are separate specs.

## Glossary

- **IStorageBackend**: The unified async interface in OE2EmpireTracker.Common defining fine-grained CRUD operations for all entity types (player data, baseline data, faction/server entities, permissions, intel, audit)
- **JSON_SingleFile_Backend**: An IStorageBackend implementation that persists all player data as a single monolithic JSON file (the current WinForms format) and server-global data in separate files
- **JSON_MultiFile_Backend**: An IStorageBackend implementation that persists data as separate JSON files per entity type and per character (the current Server format)
- **SQLite_Backend**: An IStorageBackend implementation that persists data in a single SQLite database file with a fully normalized relational schema (one table per entity type, child tables for collections/dictionaries)
- **DynamoDB_Backend**: An IStorageBackend implementation that persists data in AWS DynamoDB tables
- **Postgres_Backend**: An IStorageBackend implementation that persists data in a PostgreSQL database
- **PlayerRoot**: The serialization root object containing all player entity arrays (22 entity types)
- **BaselineRoot**: The serialization root object containing all baseline entity arrays
- **StorageBackendType**: An enum in Common listing all available backend implementations (JsonSingleFile, JsonMultiFile, Sqlite, DynamoDb, Postgres)
- **StorageBackendFactory**: A factory class in Common that creates the correct IStorageBackend instance from a StorageBackendType value and configuration parameters

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
9. THE IStorageBackend interface SHALL include methods for baseline/global lookup data stored as proper entities: BlueprintType, ShipClass, TechLevel, Commodity, RefiningRecipe, ResearchTimeEntry, PropertyTypeDefinition, and BaselineGameConstants — NOT as a single opaque JSON blob
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

**User Story:** As a user, I want to store data in SQLite with proper relational tables, so that data is queryable, indexable, and efficiently updatable at the field level.

#### Acceptance Criteria

1. THE SQLite_Backend SHALL implement IStorageBackend using a single SQLite database file with a fully normalized relational schema
2. THE SQLite_Backend SHALL represent each entity type as a dedicated table with typed columns for every scalar property (string, int, decimal, DateTime, bool, enum) on the DTO
3. THE SQLite_Backend SHALL represent each collection property (List, Dictionary) on an entity as a child table with a foreign key referencing the parent entity's primary key
4. THE SQLite_Backend SHALL represent nested objects (e.g. CountDownTime within ColonyStructure) as either inline columns with a prefix or as a child table, depending on whether the nested object has its own identity
5. THE SQLite_Backend SHALL use WAL (Write-Ahead Logging) journal mode for concurrent read performance and crash safety
6. THE SQLite_Backend SHALL create all required tables and schema on first initialization when the database file does not exist
7. THE SQLite_Backend SHALL reside in OE2EmpireTracker.Common
8. IF the SQLite database file is corrupted, inaccessible, has schema mismatches, or encounters permission errors, THEN THE SQLite_Backend SHALL throw a StorageLoadException with available diagnostic details
9. THE SQLite_Backend SHALL use transactions for multi-entity write operations to ensure atomicity
10. THE SQLite_Backend SHALL use the entity UUID as the primary key for all top-level entity tables
11. THE SQLite_Backend SHALL store PropertyBag instances as child tables with columns (ParentUUID TEXT, Key TEXT, Value TEXT, PRIMARY KEY (ParentUUID, Key))
12. THE SQLite_Backend SHALL store ItemBag instances as child tables with one row per Item, including all Item scalar fields as typed columns
13. THE SQLite_Backend SHALL store List collections (e.g. Colony.Structures, DeliveryRoute.Stops) as child tables with a foreign key to the parent and a Sequence integer column preserving list order
14. THE SQLite_Backend SHALL store Dictionary collections (e.g. Survey.Resources) as child tables with the dictionary key as a column alongside the value's fields


### Requirement 5: DynamoDB Backend

**User Story:** As a server operator deploying to AWS, I want to use DynamoDB as the storage backend for serverless scalability and managed infrastructure.

#### Acceptance Criteria

1. THE DynamoDB_Backend SHALL implement IStorageBackend using AWS DynamoDB tables
2. THE DynamoDB_Backend SHALL match the current Server DynamoStorageBackend table structure and access patterns for backward compatibility
3. THE DynamoDB_Backend SHALL reside in OE2EmpireTracker.Common
4. THE DynamoDB_Backend SHALL use the AWSSDK.DynamoDBv2 package for all DynamoDB operations
5. IF DynamoDB is unreachable or returns errors, THEN THE DynamoDB_Backend SHALL throw a StorageLoadException or StorageWriteException with the operation details and inner exception

### Requirement 6: Postgres Backend

**User Story:** As a server operator preferring relational databases, I want to use PostgreSQL as the storage backend with proper relational tables for ACID transactions, full SQL query capability, and managed hosting options.

#### Acceptance Criteria

1. THE Postgres_Backend SHALL implement IStorageBackend using a PostgreSQL database with a fully normalized relational schema matching the SQLite_Backend table structure (same table names, same columns, same child table relationships)
2. THE Postgres_Backend SHALL use PostgreSQL-native types where appropriate (UUID for primary keys, NUMERIC for decimals, TIMESTAMPTZ for DateTimes, TEXT for strings, BOOLEAN for bools)
3. THE Postgres_Backend SHALL reside in OE2EmpireTracker.Common
4. THE Postgres_Backend SHALL use the Npgsql package for PostgreSQL connectivity
5. IF PostgreSQL is unreachable or returns errors, THEN THE Postgres_Backend SHALL implement retry logic (exponential backoff, configurable max retries) before throwing a StorageLoadException or StorageWriteException with operation details and inner exception
6. THE Postgres_Backend SHALL use the same child table pattern as SQLite for collections, dictionaries, and nested objects
7. THE Postgres_Backend SHALL use foreign key constraints with CASCADE DELETE so that deleting a parent entity removes all child rows


### Requirement 7: Backend Selection Enum

**User Story:** As a developer, I want a well-defined enum of backend types, so that configuration and factory logic can reference backends by value rather than magic strings.

#### Acceptance Criteria

1. THE StorageBackendType enum SHALL define values: JsonSingleFile, JsonMultiFile, Sqlite, DynamoDb, Postgres
2. WHEN a consuming application starts, THE application SHALL instantiate the IStorageBackend implementation corresponding to the configured StorageBackendType, and SHALL fail startup immediately if instantiation fails

### Requirement 8: Backend Factory

**User Story:** As a developer, I want a factory that creates the correct backend from configuration, so that startup logic is decoupled from backend implementation details.

#### Acceptance Criteria

1. THE StorageBackendFactory SHALL accept a StorageBackendType value and configuration parameters and return the corresponding IStorageBackend instance
2. THE StorageBackendFactory SHALL reside in OE2EmpireTracker.Common alongside the interfaces and implementations
3. IF an unsupported StorageBackendType value is provided, THEN THE StorageBackendFactory SHALL throw an ArgumentException immediately describing the invalid value, without calling InitializeAsync
4. THE StorageBackendFactory SHALL call InitializeAsync on the created backend before returning it to ensure the backend is ready for use; IF InitializeAsync throws, THEN the exception SHALL propagate to the caller and no IStorageBackend instance SHALL be returned


### Requirement 9: Relational Schema Management

**User Story:** As a developer, I want the SQLite and Postgres backends to manage their own schema versioning, so that future DTO field additions or schema changes are applied automatically on startup.

#### Acceptance Criteria

1. THE SQLite_Backend SHALL maintain a _metadata table with a schema_version integer column
2. WHEN the SQLite database is opened, THE SQLite_Backend SHALL compare the on-disk schema_version with the expected version and apply any pending migrations sequentially
3. THE SQLite_Backend SHALL define the initial schema (version 1) with tables for all entity types, all child tables for collections/dictionaries/PropertyBags, and the metadata table
4. WHEN a DTO gains a new scalar field in a future version, THE schema migration SHALL add the corresponding column via ALTER TABLE with an appropriate default value
5. WHEN a DTO gains a new collection field in a future version, THE schema migration SHALL create the corresponding child table
6. IF a schema migration fails, THEN THE SQLite_Backend SHALL roll back the transaction and throw a StorageLoadException describing which migration step failed
7. IF the transaction rollback itself fails after a schema migration failure, THEN THE SQLite_Backend SHALL throw a StorageCorruptionException with both the migration error and rollback error details
8. THE Postgres_Backend SHALL use the same numbered migration approach and _metadata table as SQLite, with migrations expressed in PostgreSQL DDL

### Requirement 10: Error Handling and Recovery

**User Story:** As a user, I want clear error messages and safe recovery when storage operations fail, so that I never lose data due to backend issues.

#### Acceptance Criteria

1. IF a write operation fails on any backend, THEN THE backend SHALL throw a StorageWriteException with the backend type, operation details, and inner exception
2. IF a read/load operation fails on any backend, THEN THE backend SHALL throw a StorageLoadException with the backend type, file path or connection info, and inner exception
3. FOR transactional backends (SQLite, Postgres), failed write operations SHALL roll back the transaction before throwing StorageWriteException
4. FOR file-based backends (JSON single-file, JSON multi-file), failed write operations SHALL leave existing files intact via the atomic write strategy


### Requirement 11: Serialization Compatibility

**User Story:** As a user upgrading to the new unified storage system, I want my existing data files to load without any manual conversion, so that the upgrade is seamless.

#### Acceptance Criteria

1. THE JSON_SingleFile_Backend SHALL read existing PlayerData.json and Alpha3.json files produced by the current PlayerContext.WriteContext() without modification
2. THE JSON_SingleFile_Backend SHALL read existing BaselineData.json files produced by the current EmpireContext.WriteContext() without modification
3. THE JSON_SingleFile_Backend SHALL produce byte-identical output to the current serialization (same JsonSerializerSettings, Formatting.Indented, NullValueHandling, SerializationSorter ordering)
4. THE JSON_MultiFile_Backend SHALL read existing server data directories without modification
5. THE SQLite_Backend SHALL include a one-time migration path from the existing server JSON-blob SQLite schema to the new relational schema, executed automatically on first open of a legacy database
6. WHEN loading older data versions, THE backend SHALL apply data migrations in the correct order to bring data to the current version; IF a migration fails partway through, THE backend SHALL allow the system to continue with the partially migrated data and log a warning

## Implementation Phases

### Phase 1: Interface, Backends, and Factory in Common
- Define IStorageBackend interface in Common (netstandard2.0)
- Move all entity model classes (Models.cs, PermissionModels.cs) to Common/Models
- Move all backend implementations to Common (JSON multi-file, SQLite, DynamoDB, Postgres)
- Create the JSON single-file backend in Common
- Create StorageBackendType enum and StorageBackendFactory
- Define error types (StorageLoadException, StorageWriteException, StorageCorruptionException)
- Implement SQLite schema management with versioned migrations
- All backends pass serialization compatibility checks against existing data files
