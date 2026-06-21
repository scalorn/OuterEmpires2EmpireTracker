# Implementation Plan

## Overview

This plan implements the unified IStorageBackend interface and all five backend implementations (JSON single-file, JSON multi-file, SQLite, DynamoDB, Postgres) in OE2EmpireTracker.Common. The SQLite and Postgres backends are completely rewritten from JSON-blob storage to a fully normalized relational schema with 50+ tables, typed columns, and child tables for all collections. Work is sequenced in 12 phases with fine-grained tasks respecting the ≤200 LOC / ≤5 files per task limit.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2"] },
    { "id": 2, "tasks": ["1.3"] },
    { "id": 3, "tasks": ["2.1"] },
    { "id": 4, "tasks": ["2.2"] },
    { "id": 5, "tasks": ["2.3"] },
    { "id": 6, "tasks": ["3.1", "3.2"] },
    { "id": 7, "tasks": ["3.3"] },
    { "id": 8, "tasks": ["4.1"] },
    { "id": 9, "tasks": ["4.2"] },
    { "id": 10, "tasks": ["4.3"] },
    { "id": 11, "tasks": ["4.4"] },
    { "id": 12, "tasks": ["5.1"] },
    { "id": 13, "tasks": ["5.2"] },
    { "id": 14, "tasks": ["5.3"] },
    { "id": 15, "tasks": ["5.4"] },
    { "id": 16, "tasks": ["5.5"] },
    { "id": 17, "tasks": ["5.6"] },
    { "id": 18, "tasks": ["5.7"] },
    { "id": 19, "tasks": ["5.8"] },
    { "id": 20, "tasks": ["5.9"] },
    { "id": 21, "tasks": ["5.10"] },
    { "id": 22, "tasks": ["5.11"] },
    { "id": 23, "tasks": ["6.1"] },
    { "id": 24, "tasks": ["6.2"] },
    { "id": 25, "tasks": ["6.3"] },
    { "id": 26, "tasks": ["6.4"] },
    { "id": 27, "tasks": ["6.5"] },
    { "id": 28, "tasks": ["6.6"] },
    { "id": 29, "tasks": ["6.7"] },
    { "id": 30, "tasks": ["7.1"] },
    { "id": 31, "tasks": ["7.2"] },
    { "id": 32, "tasks": ["7.3"] },
    { "id": 33, "tasks": ["7.4"] },
    { "id": 34, "tasks": ["7.5"] },
    { "id": 35, "tasks": ["8.1"] },
    { "id": 36, "tasks": ["8.2"] },
    { "id": 37, "tasks": ["9.1", "9.2"] },
    { "id": 38, "tasks": ["9.3", "9.4"] },
    { "id": 39, "tasks": ["9.5", "9.6"] },
    { "id": 40, "tasks": ["10.1", "10.2"] },
    { "id": 41, "tasks": ["10.3", "10.4"] },
    { "id": 42, "tasks": ["11.1"] },
    { "id": 43, "tasks": ["11.2", "11.3"] },
    { "id": 44, "tasks": ["11.4", "11.5"] },
    { "id": 45, "tasks": ["12.1", "12.2"] }
  ]
}
```

## Tasks

- [x] 1. Interface, Enum, and Exceptions
  - [x] 1.1 Create StorageBackendType enum and StorageExceptions in Common/Interfaces
    - Satisfies: Req 7, Criterion 1; Req 10, Criteria 1-4
    - Inputs: design.md (enum values, exception hierarchy)
    - Output: OE2EmpireTracker.Common/Interfaces/StorageBackendType.cs, OE2EmpireTracker.Common/Interfaces/StorageExceptions.cs
    - Verification: getDiagnostics — compiles cleanly
  - [x] 1.2 Create IStorageBackend interface in Common/Interfaces (lifecycle + server-global + sharing + preferences)
    - Satisfies: Req 1, Criteria 1-3, 5, 10-11
    - Inputs: Server/Storage/IStorageBackend.cs (current interface), design.md
    - Output: OE2EmpireTracker.Common/Interfaces/IStorageBackend.cs (partial — lifecycle, StorageInfo, factions, characters, tokens, membership actions, star systems, colony summaries, sharing rules, preferences)
    - Verification: getDiagnostics — compiles cleanly
  - [x] 1.3 Extend IStorageBackend with per-character entity methods, permission methods, intel, audit, and baseline methods
    - Satisfies: Req 1, Criteria 4, 6-9
    - Inputs: Server/Storage/IStorageBackend.cs, design.md
    - Output: OE2EmpireTracker.Common/Interfaces/IStorageBackend.cs (complete interface)
    - Verification: getDiagnostics — compiles cleanly

- [x] 2. Move Server Models to Common
  - [x] 2.1 Move ServerModels.cs from Server/Storage to Common/Models
    - Satisfies: Req 1, Criteria 3 (model availability in Common)
    - Inputs: Server/Storage/Models.cs
    - Output: OE2EmpireTracker.Common/Models/ServerModels.cs; Server file deleted; using statements updated in Server
    - Verification: Full solution build — zero errors, zero warnings
  - [x] 2.2 Move PermissionModels.cs from Server/Storage to Common/Models
    - Satisfies: Req 1, Criteria 6-8 (permission model availability in Common)
    - Inputs: Server/Storage/PermissionModels.cs
    - Output: OE2EmpireTracker.Common/Models/PermissionModels.cs; Server file deleted; using statements updated in Server
    - Verification: Full solution build — zero errors, zero warnings
  - [x] 2.3 Move JsonFileStorageBackend from Server to Common as JsonMultiFileBackend
    - Satisfies: Req 3, Criteria 1-9
    - Inputs: Server/Storage/JsonFileStorageBackend.cs, Common/Interfaces/IStorageBackend.cs
    - Output: OE2EmpireTracker.Common/Storage/JsonMultiFileBackend.cs; Server file deleted; Server references updated
    - Verification: Full solution build — zero errors, zero warnings

- [x] 3. Move DynamoDB Backend and Create Factory Scaffolding
  - [x] 3.1 Move DynamoStorageBackend from Server to Common as DynamoDbBackend
    - Satisfies: Req 5, Criteria 1-5
    - Inputs: Server/Storage/DynamoStorageBackend.cs, Common/Interfaces/IStorageBackend.cs
    - Output: OE2EmpireTracker.Common/Storage/DynamoDbBackend.cs; Server file deleted
    - Verification: Full solution build — zero errors, zero warnings
  - [x] 3.2 Create StorageBackendConfig and StorageBackendFactory in Common/Storage
    - Satisfies: Req 8, Criteria 1-4
    - Inputs: design.md (factory pattern, config class)
    - Output: OE2EmpireTracker.Common/Storage/StorageBackendConfig.cs, OE2EmpireTracker.Common/Storage/StorageBackendFactory.cs
    - Verification: getDiagnostics — compiles cleanly
  - [x] 3.3 Update Server Program.cs to use Common backends and factory; delete Server/Storage/IStorageBackend.cs
    - Satisfies: Req 8, Criterion 2; design.md Server Project Migration section
    - Inputs: Server/Program.cs, Server/Storage/IStorageBackend.cs (to be deleted)
    - Output: Server/Program.cs updated; Server/Storage/IStorageBackend.cs deleted; old SQLite and Postgres backend files deleted from Server
    - Verification: Full solution build + dotnet test OE2EmpireTracker.Server.Tests — all existing tests pass

- [x] 4. JsonSingleFileBackend (New Implementation)
  - [x] 4.1 Implement JsonSingleFileBackend: lifecycle, load/save, empty/malformed handling
    - Satisfies: Req 2, Criteria 1-4, 6-7; Req 11, Criteria 1, 3
    - Inputs: Common/Services/PlayerRoot.cs, Common/Services/SafeFileWriter.cs, Common/Services/JsonSettings.cs, Common/Services/SerializationSorter.cs
    - Output: OE2EmpireTracker.Common/Storage/JsonSingleFileBackend.cs (lifecycle, Initialize, ValidateConnection, GetStorageInfo, load/save plumbing)
    - Verification: getDiagnostics — compiles cleanly
  - [x] 4.2 Implement JsonSingleFileBackend: per-character entity CRUD (Colony, Blueprint, Survey, PlayerProfile, DeliveryRoute, DeliveryPlan)
    - Satisfies: Req 2, Criteria 5, 8; Req 1, Criterion 4
    - Inputs: Common/Storage/JsonSingleFileBackend.cs (from 4.1), PlayerRoot structure
    - Output: OE2EmpireTracker.Common/Storage/JsonSingleFileBackend.cs (extended — 6 entity types CRUD)
    - Verification: getDiagnostics — compiles cleanly
  - [x] 4.3 Implement JsonSingleFileBackend: per-character entity CRUD (Ship, ShipTemplate, MarketListing, MarketTransaction, PricingPlan, StockPlan, StockProfile, BuildPlan, SupplyChain, Asteroid, Station, Faction, ExternalCharacter, WarehouseOverflowRule, Mail, Banking)
    - Satisfies: Req 2, Criteria 5, 8; Req 1, Criterion 4
    - Inputs: Common/Storage/JsonSingleFileBackend.cs (from 4.2), PlayerRoot structure
    - Output: OE2EmpireTracker.Common/Storage/JsonSingleFileBackend.cs (extended — remaining entity types)
    - Verification: getDiagnostics — compiles cleanly
  - [x] 4.4 Implement JsonSingleFileBackend: baseline data, DataVersion migration, and NotSupported server-global stubs
    - Satisfies: Req 2, Criteria 2, 9-10; Req 11, Criteria 2, 6; Req 1, Criteria 3, 5-9
    - Inputs: Common/Storage/JsonSingleFileBackend.cs (from 4.3), BaselineRoot, existing migration logic in PlayerContext
    - Output: OE2EmpireTracker.Common/Storage/JsonSingleFileBackend.cs (complete)
    - Verification: getDiagnostics — compiles cleanly

- [x] 5. SQLite Backend — Full Relational Rewrite (Schema DDL)
  - [x] 5.1 Create SqliteBackend scaffolding: class, constructor, InitializeAsync, OpenConnection, WAL pragma, _metadata table
    - Satisfies: Req 4, Criteria 5-6; Req 9, Criterion 1
    - Inputs: design.md (SqliteBackend section), Common/Interfaces/IStorageBackend.cs
    - Output: OE2EmpireTracker.Common/Storage/SqliteBackend.cs (scaffolding only)
    - Verification: getDiagnostics — compiles cleanly
  - [x] 5.2 SQLite schema DDL: Colonies, ColonyStructures, ColonyStructureProperties, ColonyStructureWorkers tables
    - Satisfies: Req 4, Criteria 1-4, 10-11, 13; Req 9, Criterion 3
    - Inputs: design.md (Colony schema), Common/Storage/SqliteBackend.cs (from 5.1)
    - Output: OE2EmpireTracker.Common/Storage/SqliteBackend.cs (schema string extended)
    - Verification: getDiagnostics — compiles cleanly
  - [x] 5.3 SQLite schema DDL: Items (unified), Blueprints, BlueprintProperties, BlueprintResources tables
    - Satisfies: Req 4, Criteria 2-3, 10-12, 14
    - Inputs: design.md (Items + Blueprint schema)
    - Output: OE2EmpireTracker.Common/Storage/SqliteBackend.cs (schema string extended)
    - Verification: getDiagnostics — compiles cleanly
  - [x] 5.4 SQLite schema DDL: Surveys, SurveyProperties, SurveyResources, PlayerProfiles, PlayerSkills tables
    - Satisfies: Req 4, Criteria 2-4, 10, 14
    - Inputs: design.md (Survey + PlayerProfile schema)
    - Output: OE2EmpireTracker.Common/Storage/SqliteBackend.cs (schema string extended)
    - Verification: getDiagnostics — compiles cleanly
  - [x] 5.5 SQLite schema DDL: DeliveryRoutes, DeliveryRouteStops, Ships, ShipComponents, ShipTemplates, ShipTemplateComponents tables
    - Satisfies: Req 4, Criteria 2-3, 10, 13
    - Inputs: design.md (DeliveryRoute + Ship + ShipTemplate schema)
    - Output: OE2EmpireTracker.Common/Storage/SqliteBackend.cs (schema string extended)
    - Verification: getDiagnostics — compiles cleanly
  - [x] 5.6 SQLite schema DDL: DeliveryPlans, DeliveryPlanStops, DeliveryPlanItems, MarketListings, MarketTransactions tables
    - Satisfies: Req 4, Criteria 2-3, 10, 13
    - Inputs: design.md (DeliveryPlan + Market schema)
    - Output: OE2EmpireTracker.Common/Storage/SqliteBackend.cs (schema string extended)
    - Verification: getDiagnostics — compiles cleanly
  - [x] 5.7 SQLite schema DDL: PricingPlans, PricingPlanPrices, BuildPlans, BuildItems, StockPlans, StockTargets, StockProfiles, StockProfileEntries tables
    - Satisfies: Req 4, Criteria 2-3, 10, 13-14
    - Inputs: design.md (PricingPlan + BuildPlan + StockPlan + StockProfile schema)
    - Output: OE2EmpireTracker.Common/Storage/SqliteBackend.cs (schema string extended)
    - Verification: getDiagnostics — compiles cleanly
  - [x] 5.8 SQLite schema DDL: SupplyChains, SupplyChainStages, Asteroids, AsteroidReserves, Stations, StationComponents, Factions, ExternalCharacters, WarehouseOverflowRules, MailMessages, BankingTransactions tables
    - Satisfies: Req 4, Criteria 2-3, 10, 13
    - Inputs: design.md (remaining player entity schemas)
    - Output: OE2EmpireTracker.Common/Storage/SqliteBackend.cs (schema string extended)
    - Verification: getDiagnostics — compiles cleanly
  - [x] 5.9 SQLite schema DDL: ServerFactions, ServerFactionLeaders, ServerCharacters, ApiTokens, MembershipActions, StarSystems, SharingRules, CharacterPreferences tables
    - Satisfies: Req 4, Criteria 2, 10
    - Inputs: design.md (server-global entity schemas)
    - Output: OE2EmpireTracker.Common/Storage/SqliteBackend.cs (schema string extended)
    - Verification: getDiagnostics — compiles cleanly
  - [x] 5.10 SQLite schema DDL: All permission tables (FactionCapabilities, FactionClearanceLevels, FactionPermissionGroups, FactionGroupCapabilities, FactionGroupSharingRules, FactionMemberPermissions, FactionMemberCapabilities, CharacterCapabilities, CharacterClearanceLevels, CharacterPermissionGroups, CharacterGroupCapabilities, CharacterGroupSharingRules, CharacterGranteePermissions, CharacterGranteeCapabilities)
    - Satisfies: Req 4, Criteria 2, 10
    - Inputs: design.md (permission entity schemas)
    - Output: OE2EmpireTracker.Common/Storage/SqliteBackend.cs (schema string extended)
    - Verification: getDiagnostics — compiles cleanly
  - [x] 5.11 SQLite schema DDL: IntelComments, IntelCommentFactionShares, PermissionAuditEntries, BaselineGameConstants, BlueprintTypes, BlueprintTypeProperties, BlueprintTypeResearchableProperties, ShipClasses, TechLevels, Commodities, CommodityResources, RefiningRecipes, ResearchTimes, PropertyTypeDefinitions tables
    - Satisfies: Req 4, Criteria 2, 10; Req 1, Criterion 9
    - Inputs: design.md (intel + audit + baseline schemas)
    - Output: OE2EmpireTracker.Common/Storage/SqliteBackend.cs (schema complete)
    - Verification: getDiagnostics — compiles cleanly

- [ ] 6. SQLite Backend — CRUD Implementation
  - [x] 6.1 SQLite CRUD: Server-global entities (ServerFaction, ServerCharacter, ApiToken, MembershipAction, StarSystem, SharingRules, CharacterPreferences, ColonySummary)
    - Satisfies: Req 4, Criteria 1, 9-10; Req 1, Criterion 3
    - Inputs: Common/Storage/SqliteBackend.cs (from 5.11), IStorageBackend interface
    - Output: OE2EmpireTracker.Common/Storage/SqliteBackend.cs (extended — server-global CRUD methods)
    - Verification: getDiagnostics — compiles cleanly
  - [x] 6.2 SQLite CRUD: Colony (with Structures child table, ColonyStructureProperties, ColonyStructureWorkers, Items)
    - Satisfies: Req 4, Criteria 1, 9-14
    - Inputs: Common/Storage/SqliteBackend.cs, Colony model, ColonyStructure model
    - Output: OE2EmpireTracker.Common/Storage/SqliteBackend.cs (extended — Colony Get/GetAll/Upsert/Delete with child tables)
    - Verification: getDiagnostics — compiles cleanly
  - [x] 6.3 SQLite CRUD: Blueprint (with Properties and Resources child tables), Survey (with Properties and Resources child tables)
    - Satisfies: Req 4, Criteria 1, 9-11, 14
    - Inputs: Common/Storage/SqliteBackend.cs, Blueprint model, Survey model
    - Output: OE2EmpireTracker.Common/Storage/SqliteBackend.cs (extended — Blueprint + Survey CRUD)
    - Verification: getDiagnostics — compiles cleanly
  - [x] 6.4 SQLite CRUD: PlayerProfile (with Skills child table), DeliveryRoute (with Stops child table), DeliveryPlan (with Stops and Items child tables)
    - Satisfies: Req 4, Criteria 1, 9-10, 13
    - Inputs: Common/Storage/SqliteBackend.cs, PlayerProfile model, DeliveryRoute model, DeliveryPlan model
    - Output: OE2EmpireTracker.Common/Storage/SqliteBackend.cs (extended — PlayerProfile + DeliveryRoute + DeliveryPlan CRUD)
    - Verification: getDiagnostics — compiles cleanly
  - [-] 6.5 SQLite CRUD: Ship (with Components + Items), ShipTemplate (with Components), MarketListing, MarketTransaction
    - Satisfies: Req 4, Criteria 1, 9-10, 12-13
    - Inputs: Common/Storage/SqliteBackend.cs, Ship model, ShipTemplate model, MarketListing model
    - Output: OE2EmpireTracker.Common/Storage/SqliteBackend.cs (extended)
    - Verification: getDiagnostics — compiles cleanly
  - [~] 6.6 SQLite CRUD: PricingPlan (with Prices), BuildPlan (with Items), StockPlan (with Targets), StockProfile (with Entries), SupplyChain (with Stages), Asteroid (with Reserves), Station (with Components + Items)
    - Satisfies: Req 4, Criteria 1, 9-10, 13-14
    - Inputs: Common/Storage/SqliteBackend.cs, remaining entity models
    - Output: OE2EmpireTracker.Common/Storage/SqliteBackend.cs (extended)
    - Verification: getDiagnostics — compiles cleanly
  - [~] 6.7 SQLite CRUD: Faction, ExternalCharacter, WarehouseOverflowRule, Mail, Banking, Permission entities, Intel, Audit, Baseline entities
    - Satisfies: Req 4, Criteria 1, 9-10; Req 1, Criteria 6-9
    - Inputs: Common/Storage/SqliteBackend.cs, permission models, intel models
    - Output: OE2EmpireTracker.Common/Storage/SqliteBackend.cs (complete — all IStorageBackend methods implemented)
    - Verification: getDiagnostics — compiles cleanly

- [ ] 7. Postgres Backend — Full Relational Rewrite
  - [~] 7.1 Create PostgresBackend scaffolding: class, constructor, InitializeAsync, connection management, Polly retry policy
    - Satisfies: Req 6, Criteria 3-5; Req 9, Criterion 8
    - Inputs: design.md (PostgresBackend section), Common/Interfaces/IStorageBackend.cs
    - Output: OE2EmpireTracker.Common/Storage/PostgresBackend.cs (scaffolding + _metadata + retry)
    - Verification: getDiagnostics — compiles cleanly
  - [~] 7.2 Postgres schema DDL: All player entity tables (matching SQLite schema with PostgreSQL-native types)
    - Satisfies: Req 6, Criteria 1-2, 6-7; Req 9, Criterion 8
    - Inputs: Common/Storage/SqliteBackend.cs (schema reference), design.md (Postgres types)
    - Output: OE2EmpireTracker.Common/Storage/PostgresBackend.cs (schema DDL complete)
    - Verification: getDiagnostics — compiles cleanly
  - [~] 7.3 Postgres schema DDL: All server-global, permission, intel, audit, and baseline tables
    - Satisfies: Req 6, Criteria 1-2, 6-7
    - Inputs: SQLite schema as reference
    - Output: OE2EmpireTracker.Common/Storage/PostgresBackend.cs (full schema DDL)
    - Verification: getDiagnostics — compiles cleanly
  - [~] 7.4 Postgres CRUD: Server-global entities, Colony, Blueprint, Survey, PlayerProfile, DeliveryRoute, DeliveryPlan
    - Satisfies: Req 6, Criteria 1, 5-7
    - Inputs: Common/Storage/PostgresBackend.cs (from 7.3), SqliteBackend CRUD as reference
    - Output: OE2EmpireTracker.Common/Storage/PostgresBackend.cs (extended — first batch of CRUD)
    - Verification: getDiagnostics — compiles cleanly
  - [~] 7.5 Postgres CRUD: Ship, ShipTemplate, Market, PricingPlan, BuildPlan, StockPlan, StockProfile, SupplyChain, Asteroid, Station, Faction, ExternalCharacter, WarehouseOverflowRule, Mail, Banking, Permissions, Intel, Audit, Baseline
    - Satisfies: Req 6, Criteria 1, 5-7
    - Inputs: Common/Storage/PostgresBackend.cs (from 7.4), SqliteBackend CRUD as reference
    - Output: OE2EmpireTracker.Common/Storage/PostgresBackend.cs (complete)
    - Verification: getDiagnostics — compiles cleanly

- [ ] 8. SQLite Schema Migration and Legacy Migration
  - [~] 8.1 Implement SQLite schema versioning: _metadata read/write, migration runner, rollback on failure, StorageCorruptionException
    - Satisfies: Req 9, Criteria 1-2, 6-7
    - Inputs: Common/Storage/SqliteBackend.cs (from 6.7)
    - Output: OE2EmpireTracker.Common/Storage/SqliteBackend.cs (migration infrastructure added)
    - Verification: getDiagnostics — compiles cleanly
  - [~] 8.2 Implement SQLite legacy JSON-blob migration: detect old schema, read JSON blobs, insert into normalized tables
    - Satisfies: Req 11, Criterion 5
    - Inputs: Common/Storage/SqliteBackend.cs (from 8.1), old schema structure (JSON-blob tables)
    - Output: OE2EmpireTracker.Common/Storage/SqliteBackend.cs (legacy migration logic added)
    - Verification: getDiagnostics — compiles cleanly

- [ ] 9. Unit Tests — JsonSingleFile and JsonMultiFile Backends
  - [~] 9.1 Write unit tests for JsonSingleFileBackend: lifecycle, empty/missing file, malformed JSON error handling
    - Satisfies: Req 2, Criteria 6-7; Req 10, Criteria 1-2; Req 11, Criterion 1
    - Inputs: Common/Storage/JsonSingleFileBackend.cs
    - Output: OE2EmpireTracker.Tests/Storage/JsonSingleFileBackendLifecycleTests.cs
    - Verification: vstest.console — new tests pass
  - [~] 9.2 Write unit tests for JsonSingleFileBackend: CRUD operations and serialization byte-identical output
    - Satisfies: Req 2, Criteria 1, 3-4, 8; Req 11, Criteria 1, 3
    - Inputs: Common/Storage/JsonSingleFileBackend.cs, existing PlayerData.json
    - Output: OE2EmpireTracker.Tests/Storage/JsonSingleFileBackendCrudTests.cs
    - Verification: vstest.console — new tests pass
  - [~] 9.3 Write unit tests for JsonSingleFileBackend: DataVersion migration on load
    - Satisfies: Req 2, Criteria 9-10; Req 11, Criterion 6
    - Inputs: Common/Storage/JsonSingleFileBackend.cs
    - Output: OE2EmpireTracker.Tests/Storage/JsonSingleFileBackendMigrationTests.cs
    - Verification: vstest.console — new tests pass
  - [~] 9.4 Write unit tests for JsonMultiFileBackend: initialization, CRUD, missing file, malformed JSON, directory structure
    - Satisfies: Req 3, Criteria 1-2, 4, 6-8; Req 11, Criterion 4
    - Inputs: Common/Storage/JsonMultiFileBackend.cs
    - Output: OE2EmpireTracker.Tests/Storage/JsonMultiFileBackendTests.cs
    - Verification: vstest.console — new tests pass
  - [~] 9.5 Write unit tests for StorageBackendFactory: valid types create correct backend, invalid type throws, InitializeAsync called
    - Satisfies: Req 8, Criteria 1-4
    - Inputs: Common/Storage/StorageBackendFactory.cs
    - Output: OE2EmpireTracker.Tests/Storage/StorageBackendFactoryTests.cs
    - Verification: vstest.console — new tests pass
  - [~] 9.6 Write unit tests for StorageExceptions: constructor parameters, properties set correctly
    - Satisfies: Req 10, Criteria 1-4
    - Inputs: Common/Interfaces/StorageExceptions.cs
    - Output: OE2EmpireTracker.Tests/Storage/StorageExceptionTests.cs
    - Verification: vstest.console — new tests pass

- [ ] 10. Unit Tests — SQLite and Postgres Backends
  - [~] 10.1 Write unit tests for SqliteBackend: schema creation on new DB, WAL mode, _metadata table
    - Satisfies: Req 4, Criteria 5-6; Req 9, Criteria 1-3
    - Inputs: Common/Storage/SqliteBackend.cs
    - Output: OE2EmpireTracker.Tests/Storage/SqliteBackendSchemaTests.cs
    - Verification: vstest.console — new tests pass
  - [~] 10.2 Write unit tests for SqliteBackend: Colony CRUD with child tables (Structures, Properties, Workers, Items)
    - Satisfies: Req 4, Criteria 1-4, 9-14
    - Inputs: Common/Storage/SqliteBackend.cs
    - Output: OE2EmpireTracker.Tests/Storage/SqliteBackendColonyTests.cs
    - Verification: vstest.console — new tests pass
  - [~] 10.3 Write unit tests for SqliteBackend: Blueprint, Survey, PlayerProfile, DeliveryRoute, DeliveryPlan CRUD with child tables
    - Satisfies: Req 4, Criteria 1-4, 9-14
    - Inputs: Common/Storage/SqliteBackend.cs
    - Output: OE2EmpireTracker.Tests/Storage/SqliteBackendEntityTests.cs
    - Verification: vstest.console — new tests pass
  - [~] 10.4 Write unit tests for SqliteBackend: transaction rollback on failure, schema migration versioning, legacy JSON-blob migration, error handling
    - Satisfies: Req 4, Criteria 8-9; Req 9, Criteria 2, 6-7; Req 10, Criteria 1-3; Req 11, Criterion 5
    - Inputs: Common/Storage/SqliteBackend.cs
    - Output: OE2EmpireTracker.Tests/Storage/SqliteBackendMigrationTests.cs
    - Verification: vstest.console — new tests pass

- [ ] 11. DynamoDB Backend Unit Tests (DynamoDB Local)
  - [~] 11.1 Create DynamoDB Local test fixture: start/stop process, table creation/teardown, endpoint configuration
    - Satisfies: Req 5, Criteria 1, 4; design.md (DynamoDB Local test infrastructure)
    - Inputs: design.md (DynamoDB Local config: JDK path, JAR path, port 8111, -inMemory flag)
    - Output: OE2EmpireTracker.Tests/Storage/DynamoDbLocalFixture.cs
    - Verification: vstest.console — fixture starts DynamoDB Local and creates tables successfully
  - [~] 11.2 Write unit tests for DynamoDbBackend: InitializeAsync, ValidateConnectionAsync, GetStorageInfo, table creation
    - Satisfies: Req 5, Criteria 1-2, 4
    - Inputs: Common/Storage/DynamoDbBackend.cs, DynamoDbLocalFixture
    - Output: OE2EmpireTracker.Tests/Storage/DynamoDbBackendLifecycleTests.cs
    - Verification: vstest.console — tests pass with DynamoDB Local
  - [~] 11.3 Write unit tests for DynamoDbBackend: Server-global entity CRUD (Faction, Character, Token, MembershipAction, StarSystem)
    - Satisfies: Req 5, Criteria 1-2
    - Inputs: Common/Storage/DynamoDbBackend.cs, DynamoDbLocalFixture
    - Output: OE2EmpireTracker.Tests/Storage/DynamoDbBackendGlobalEntityTests.cs
    - Verification: vstest.console — tests pass with DynamoDB Local
  - [~] 11.4 Write unit tests for DynamoDbBackend: Per-character entity CRUD (Colony, Blueprint, Survey, PlayerProfile, DeliveryRoute, Ship)
    - Satisfies: Req 5, Criteria 1-2
    - Inputs: Common/Storage/DynamoDbBackend.cs, DynamoDbLocalFixture
    - Output: OE2EmpireTracker.Tests/Storage/DynamoDbBackendCharacterEntityTests.cs
    - Verification: vstest.console — tests pass with DynamoDB Local
  - [~] 11.5 Write unit tests for DynamoDbBackend: error handling (unreachable endpoint throws StorageLoadException/StorageWriteException)
    - Satisfies: Req 5, Criterion 5
    - Inputs: Common/Storage/DynamoDbBackend.cs
    - Output: OE2EmpireTracker.Tests/Storage/DynamoDbBackendErrorTests.cs
    - Verification: vstest.console — tests pass (uses invalid endpoint to simulate unreachable)

- [ ] 12. Property Tests
  - [~] 12.1 Write property tests: Entity Count Preservation across JsonSingleFile and SQLite backends
    - Satisfies: Correctness Property 1 (design.md)
    - Inputs: All backend implementations, FsCheck 2.16.6 patterns
    - Output: OE2EmpireTracker.Tests/Storage/StorageCountPreservationPropertyTests.cs
    - Verification: vstest.console — property tests pass
  - [~] 12.2 Write property tests: Atomic Write Safety for JsonSingleFile and SQLite backends
    - Satisfies: Correctness Property 3 (design.md); Req 10, Criteria 3-4
    - Inputs: All backend implementations, FsCheck 2.16.6 patterns
    - Output: OE2EmpireTracker.Tests/Storage/AtomicWritePropertyTests.cs
    - Verification: vstest.console — property tests pass

## Notes

- The SQLite rewrite (Phase 5-6) is the largest body of work: ~50 CREATE TABLE statements + CRUD for each with child table management
- The Postgres backend (Phase 7) mirrors SQLite schema but uses PostgreSQL-native types and Npgsql
- Tasks in the same wave can run in parallel where marked (e.g. 3.1 + 3.2, 9.1 + 9.2, etc.)
- The existing SQLite backend uses JSON blobs — the new one uses typed columns and child tables. This is a complete rewrite, not a refactor.
- All property tests use FsCheck 2.16.6 API (no 3.x patterns)
- DynamoDB tests (Phase 11) use DynamoDB Local with `-inMemory` flag on port 8111. JDK at D:\tools\jdk25.0.3_9, JAR at D:\tools\dynamodb-local\DynamoDBLocal.jar
- The DynamoDbLocalFixture manages process lifecycle (start before suite, kill after) so tests don't need an external process running
- Postgres backend tests are omitted from the mandatory test suite since they require an external Postgres instance; validation is via code review and getDiagnostics
