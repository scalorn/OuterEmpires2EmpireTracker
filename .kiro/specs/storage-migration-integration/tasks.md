# Implementation Plan: Storage Migration Integration (Phase 2 & 3)

## Overview

This plan implements the integration of PlayerContext and EmpireContext with the unified IStorageBackend interface, dirty entity tracking across all 18 services, backend selection preferences, desktop startup wiring, data migration service, and round-trip fidelity property tests. Tasks are ordered so prerequisite fixes land first, then core infrastructure, then consumers, then migration and tests.

## Design Note: Generic Persistence Helper

To avoid 22× repetitive code in PersistDirtyEntities and LoadFromBackend, the design introduces a helper pattern:

```csharp
// EntityPersistenceMap: maps entity types to their backend CRUD delegates
// Avoids 200+ lines of per-type boilerplate in WriteContext/LoadFromBackend
private static readonly EntityPersistenceEntry[] EntityMap = new[]
{
    new EntityPersistenceEntry(typeof(Colony),
        load: (b, uuid) => b.GetAllColoniesAsync(uuid),
        upsert: (b, uuid, e) => b.UpsertColonyAsync(uuid, (Colony)e),
        delete: (b, uuid, id) => b.DeleteColonyAsync(uuid, id),
        findInList: (ctx, id) => ctx._colonyList.FirstOrDefault(c => c.UUID == id),
        initFromRoot: (ctx, root) => ctx.InitColonies(root)),
    // ... one entry per entity type
};
```

This pattern is created in Task 6.2 and consumed by Tasks 7.2 and 6.2. Each entry is ~5 lines; all 22 entries total ~110 lines in one focused file.

## Tasks

- [x] 1. Prerequisite: Decimal precision fix in SQLite backend
  - [x] 1.1 Fix SQLite BankingTransaction decimal columns (REAL→TEXT)
    - Modify table-rebuild logic for BankingTransactions: CreditChange, OldBalance, NewBalance columns to TEXT
    - Remove `(double)` casts in BankingTransaction read/write methods; use `value.ToString("G")` and `decimal.Parse(...)`
    - Add schema migration method that rebuilds the table with TEXT columns
    - _Satisfies: Req 7, Criterion 2_
    - _Inputs: Common/Storage/SqliteBackend.cs (search for "BankingTransaction" and "CreateTable" to find existing schema + migration pattern)_
    - _Output: Common/Storage/SqliteBackend.cs (modified)_
    - _Verification: Build succeeds; existing SQLite backend tests pass; decimal round-trip test added_

  - [x] 1.2 Fix SQLite MarketTransaction decimal columns (REAL→TEXT)
    - Modify table-rebuild logic for MarketTransactions: PricePerUnit, TotalPrice columns to TEXT
    - Remove `(double)` casts in MarketTransaction read/write methods
    - Add schema migration method for MarketTransactions table rebuild
    - _Satisfies: Req 7, Criterion 2_
    - _Inputs: Common/Storage/SqliteBackend.cs (same file, search for "MarketTransaction")_
    - _Output: Common/Storage/SqliteBackend.cs (modified)_
    - _Verification: Build succeeds; existing SQLite backend tests pass_

  - [x] 1.3 Fix Postgres BankingTransaction decimal columns (DOUBLE PRECISION→NUMERIC)
    - ALTER COLUMN CreditChange, OldBalance, NewBalance to NUMERIC
    - Remove `(double)` casts in BankingTransaction read/write methods
    - Add schema migration SQL that uses `ALTER TABLE ... ALTER COLUMN col TYPE NUMERIC USING col::NUMERIC`
    - _Satisfies: Req 7, Criterion 2_
    - _Inputs: Common/Storage/PostgresBackend.cs (search for "BankingTransaction" to find schema + read/write methods)_
    - _Output: Common/Storage/PostgresBackend.cs (modified)_
    - _Verification: Build succeeds; existing Postgres backend tests pass_

  - [x] 1.4 Fix Postgres MarketTransaction decimal columns (DOUBLE PRECISION→NUMERIC)
    - ALTER COLUMN PricePerUnit, TotalPrice to NUMERIC
    - Remove `(double)` casts in MarketTransaction read/write methods
    - _Satisfies: Req 7, Criterion 2_
    - _Inputs: Common/Storage/PostgresBackend.cs (same file, search for "MarketTransaction")_
    - _Output: Common/Storage/PostgresBackend.cs (modified)_
    - _Verification: Build succeeds; existing Postgres backend tests pass_


- [x] 2. Prerequisite: Add GetAllCharacterUUIDsAsync to IStorageBackend
  - [x] 2.1 Add GetAllCharacterUUIDsAsync to IStorageBackend interface
    - Add `Task<IReadOnlyList<string>> GetAllCharacterUUIDsAsync()` to IStorageBackend
    - _Satisfies: Req 6, Criterion 3_
    - _Inputs: Common/Interfaces/IStorageBackend.cs_
    - _Output: Common/Interfaces/IStorageBackend.cs (modified)_
    - _Verification: Build will fail until implementations added — that's expected_

  - [x] 2.2 Implement GetAllCharacterUUIDsAsync in JsonSingleFileBackend
    - Parse the single JSON file, extract distinct OwnerUUID values from all per-character entity arrays
    - _Satisfies: Req 6, Criterion 3_
    - _Inputs: Common/Storage/JsonSingleFileBackend.cs (1369 lines — search for existing GetAll pattern)_
    - _Output: Common/Storage/JsonSingleFileBackend.cs (modified)_
    - _Verification: Build succeeds; unit test for character discovery_

  - [x] 2.3 Implement GetAllCharacterUUIDsAsync in JsonMultiFileBackend
    - Enumerate character subdirectories in the data folder
    - _Satisfies: Req 6, Criterion 3_
    - _Inputs: Common/Storage/JsonMultiFileBackend.cs (2207 lines — search for directory enumeration pattern)_
    - _Output: Common/Storage/JsonMultiFileBackend.cs (modified)_
    - _Verification: Build succeeds; unit test for character discovery_

  - [x] 2.4 Implement GetAllCharacterUUIDsAsync in SqliteBackend
    - SELECT DISTINCT CharacterUUID via UNION across all 22 per-character tables
    - Note: the UNION query will be ~22 lines (one SELECT per table). Use existing table name constants.
    - _Satisfies: Req 6, Criterion 3_
    - _Inputs: Common/Storage/SqliteBackend.cs (7696 lines — search for table names to build UNION)_
    - _Output: Common/Storage/SqliteBackend.cs (modified)_
    - _Verification: Build succeeds; unit test for character discovery_

  - [x] 2.5 Implement GetAllCharacterUUIDsAsync in PostgresBackend
    - Same UNION DISTINCT query pattern as SQLite, adapted for Npgsql
    - _Satisfies: Req 6, Criterion 3_
    - _Inputs: Common/Storage/PostgresBackend.cs (5647 lines — search for table names)_
    - _Output: Common/Storage/PostgresBackend.cs (modified)_
    - _Verification: Build succeeds; unit test for character discovery_

  - [x] 2.6 Implement GetAllCharacterUUIDsAsync in DynamoDbBackend
    - Scan partition key prefix for character UUIDs (use existing scan pattern from other methods)
    - _Satisfies: Req 6, Criterion 3_
    - _Inputs: Common/Storage/DynamoDbBackend.cs (1588 lines)_
    - _Output: Common/Storage/DynamoDbBackend.cs (modified)_
    - _Verification: Build succeeds; unit test for character discovery_

  - [x] 2.7 Write unit tests for GetAllCharacterUUIDsAsync
    - Test each backend: empty returns empty list; after upserting entities for 2 characters, returns both UUIDs
    - Use JsonSingleFile and JsonMultiFile (can test locally without external services)
    - _Satisfies: Req 6, Criterion 3_
    - _Inputs: Common/Storage/JsonSingleFileBackend.cs, JsonMultiFileBackend.cs_
    - _Output: OE2EmpireTracker.Tests/Storage/GetAllCharacterUUIDsTests.cs (NEW)_
    - _Verification: Tests pass in vstest.console_

- [ ] 3. Prerequisite: BackgroundProcessor colony mutation routing
  - [x] 3.1 Add ColonyService.ProcessColonyTick method
    - Create a new public method `ProcessColonyTick(string colonyUUID, double elapsedSeconds)` in ColonyService
    - Look up colony by UUID, call existing Colony.ProcessColony logic, then call MarkDirty<Colony>(uuid)
    - Note: ColonyService is 470 lines. Check how it accesses PlayerContext (likely via PlayerContext.GetInstance())
    - _Satisfies: Req 2, Criterion 4_
    - _Inputs: Common/Services/ColonyService.cs, Common/Models/Colony.cs_
    - _Output: Common/Services/ColonyService.cs (modified)_
    - _Verification: Build succeeds_

  - [x] 3.2 Refactor BackgroundProcessor to use ColonyService.ProcessColonyTick
    - Replace direct `colony.ProcessColony(elapsed)` calls with `ColonyService.ProcessColonyTick(colony.UUID, elapsed)`
    - BackgroundProcessor is 644 lines. Search for "ProcessColony" to find call sites.
    - _Satisfies: Req 2, Criterion 4_
    - _Inputs: Common/Services/BackgroundProcessor.cs_
    - _Output: Common/Services/BackgroundProcessor.cs (modified)_
    - _Verification: Build succeeds; existing BackgroundProcessor tests pass_


- [~] 4. Checkpoint — Prerequisites verified
  - Build full solution with zero warnings
  - Run vstest.console (full suite, 600s timeout) + trxparse.js
  - Run dotnet test OE2EmpireTracker.Server.Tests
  - Run `node .kiro/tools/audit.js`
  - Verify decimal precision fix and GetAllCharacterUUIDsAsync implementations are solid

- [ ] 5. DirtyTracker implementation
  - [x] 5.1 Create DirtyTracker class
    - Create Common/Services/DirtyTracker.cs with DirtyKey struct
    - Implement MarkDirty<T>, MarkDeleted<T>, GetDirtyUUIDs<T>, GetDeletedUUIDs<T>
    - Implement ClearDirty<T>, ClearDeleted<T>, ClearAll, HasChanges
    - Thread-safe via internal lock
    - Estimated: ~80 lines (DirtyKey struct ~20, DirtyTracker methods ~60)
    - _Satisfies: Req 2, Criteria 1-2_
    - _Inputs: Design section 2 (DirtyTracker class spec)_
    - _Output: Common/Services/DirtyTracker.cs (NEW)_
    - _Verification: Build succeeds_

  - [x] 5.2 Write unit tests for DirtyTracker
    - Test MarkDirty/MarkDeleted add entries, GetDirtyUUIDs/GetDeletedUUIDs return them
    - Test ClearDirty/ClearDeleted remove individual entries
    - Test ClearAll empties both sets
    - Test HasChanges reflects state correctly
    - Test thread safety (concurrent Mark + Clear from different threads)
    - _Satisfies: Req 2, Criteria 1-2_
    - _Inputs: Common/Services/DirtyTracker.cs_
    - _Output: OE2EmpireTracker.Tests/Services/DirtyTrackerTests.cs (NEW)_
    - _Verification: DirtyTrackerTests pass in vstest.console_


- [x] 6. PlayerContext storage backend integration — properties, map, and load
  - [x] 6.1 Add StorageBackend and DirtyTracker properties to PlayerContext
    - Add `public IStorageBackend StorageBackend { get; set; }` property
    - Add `public DirtyTracker DirtyTracker { get; }` property (initialized in constructor)
    - Add `public void MarkDirty<T>(string entityUUID)` and `MarkDeleted<T>(string entityUUID)` delegate methods
    - Ensure Reset() clears DirtyTracker and nulls StorageBackend
    - Estimated: ~25 lines added to a 4235-line file
    - _Satisfies: Req 1, Criteria 1, 8; Req 2, Criterion 2_
    - _Inputs: Common/Services/PlayerContext.cs (top of class — look at existing property declarations around line 50-80)_
    - _Output: Common/Services/PlayerContext.cs (modified)_
    - _Verification: Build succeeds; existing tests still pass (backend is null → backward compat)_

  - [x] 6.2 Create EntityPersistenceMap with load/upsert/delete delegates for all 22 entity types
    - Create Common/Services/EntityPersistenceMap.cs (NEW)
    - Define `EntityPersistenceEntry` struct with Load, Upsert, Delete, FindInList delegates
    - Populate static array with one entry per entity type mapping to IStorageBackend methods
    - This is the KEY design decision that prevents 7.2 from being 286+ lines of repetitive code
    - Each entry is ~5 lines; 22 entries = ~110 lines + struct definition (~30 lines) = ~140 lines total
    - _Satisfies: Req 1, Criterion 6 (enables incremental persistence); Req 1, Criterion 3 (enables load)_
    - _Inputs: Common/Interfaces/IStorageBackend.cs (to get exact method signatures), Common/Services/PlayerContext.cs (to see list field names and Init method signatures)_
    - _Output: Common/Services/EntityPersistenceMap.cs (NEW)_
    - _Verification: Build succeeds_

  - [x] 6.3 Implement LoadFromBackend in PlayerContext using EntityPersistenceMap
    - Add private `LoadFromBackend(string characterUUID)` method
    - Iterate EntityPersistenceMap entries, call each Load delegate via Task.Run bridging
    - Assign results into a PlayerRoot, call existing Init methods (InitColonies, InitBlueprints, etc.)
    - Clear DirtyTracker after successful load
    - On StorageLoadException: revert _currentPlayerUUID to previous value, propagate
    - Estimated: ~50 lines (loop over map + PlayerRoot construction + error handling)
    - _Satisfies: Req 1, Criteria 3, 4, 10_
    - _Inputs: Common/Services/PlayerContext.cs (existing Init methods at lines 733-1655), Common/Services/EntityPersistenceMap.cs_
    - _Output: Common/Services/PlayerContext.cs (modified)_
    - _Verification: Build succeeds_

  - [x] 6.4 Wire CurrentPlayerUUID setter to use LoadFromBackend
    - When StorageBackend is non-null, call LoadFromBackend instead of legacy file load
    - Save previous UUID; on StorageLoadException revert and propagate
    - When StorageBackend property setter is called while CurrentPlayerUUID is active, trigger reload
    - Estimated: ~20 lines of branching logic in the setter
    - _Satisfies: Req 1, Criteria 3, 4_
    - _Inputs: Common/Services/PlayerContext.cs (CurrentPlayerUUID setter at line 305-316)_
    - _Output: Common/Services/PlayerContext.cs (modified)_
    - _Verification: Build succeeds_


- [x] 7. PlayerContext WriteContext backend integration
  - [x] 7.1 Modify WriteContext for backend routing and full-file path
    - Add branching at top of WriteContext: WritesBlocked check → ServerOnly check → no-backend check → backend dispatch
    - When backend is JsonSingleFileBackend: serialize full PlayerRoot (reuse existing serialization), call UpsertGlobalDataAsync via Task.Run
    - Clear all dirty flags after successful full-file write
    - When no backend and no FilePath: log warning, return (backward compat for tests)
    - When no backend but FilePath exists: preserve existing file write (legacy path)
    - Preserve ServerOnly delegate logic unchanged
    - Estimated: ~45 lines (routing + full-file path). Existing WriteContext is ~80 lines — this restructures it.
    - _Satisfies: Req 1, Criteria 2, 6, 7; Req 2, Criterion 8_
    - _Inputs: Common/Services/PlayerContext.cs (WriteContext at line 568-647)_
    - _Output: Common/Services/PlayerContext.cs (modified)_
    - _Verification: Build succeeds_

  - [x] 7.2 Implement PersistDirtyEntities using EntityPersistenceMap
    - Add private `PersistDirtyEntities(IStorageBackend backend, string charUUID)` method
    - Iterate EntityPersistenceMap: for each entry, get dirty UUIDs, find entity, call Upsert delegate via Task.Run
    - Clear dirty flag per-entity on success
    - Then iterate deleted UUIDs, call Delete delegate, clear deletion record on success
    - Uses the map so this is a LOOP, not 22× copy-paste — estimated ~40 lines
    - _Satisfies: Req 1, Criterion 6; Req 2, Criteria 4-6_
    - _Inputs: Common/Services/PlayerContext.cs, Common/Services/EntityPersistenceMap.cs, Common/Services/DirtyTracker.cs_
    - _Output: Common/Services/PlayerContext.cs (modified)_
    - _Verification: Build succeeds_

  - [x] 7.3 Add StorageWriteException handling in WriteContext
    - Wrap backend calls in try/catch(StorageWriteException)
    - On catch: set WritesBlocked = true, propagate exception
    - Already-persisted entities keep cleared dirty flags; unpersisted retain flags (natural consequence of per-entity clearing)
    - Estimated: ~10 lines (try/catch wrapper)
    - _Satisfies: Req 1, Criterion 9; Req 8, Criterion 1_
    - _Inputs: Common/Services/PlayerContext.cs_
    - _Output: Common/Services/PlayerContext.cs (modified)_
    - _Verification: Build succeeds_

  - [x] 7.4 Write unit tests for PlayerContext backend integration
    - Test: WriteContext with null backend and no FilePath logs warning, no crash
    - Test: WriteContext with null backend but FilePath writes file (legacy path preserved)
    - Test: WriteContext with JsonSingleFileBackend mock calls UpsertGlobalDataAsync
    - Test: WriteContext with non-JSON backend only persists dirty entities (mock verifies specific Upsert calls)
    - Test: StorageWriteException sets WritesBlocked = true
    - Test: LoadFromBackend populates in-memory state correctly (mock returns entities, verify lists populated)
    - Test: LoadFromBackend failure reverts CurrentPlayerUUID
    - Use mock/stub IStorageBackend (create a simple TestStorageBackend or use existing test helpers)
    - _Satisfies: Req 1, Criteria 1-10; Req 8, Criteria 1-4_
    - _Inputs: Common/Services/PlayerContext.cs, Common/Services/EntityPersistenceMap.cs_
    - _Output: OE2EmpireTracker.Tests/Services/PlayerContextBackendTests.cs (NEW)_
    - _Verification: PlayerContextBackendTests pass in vstest.console_

- [x] 8. Dirty entity tracking in service classes (batch 1: Colony, Blueprint, Survey, PlayerProfile)
  - [x] 8.1 Add MarkDirty/MarkDeleted calls to ColonyService
    - ColonyService has 13 public methods (470 lines). Add MarkDirty<Colony> after each mutation.
    - Identify mutations by looking for list Add/Remove and property assignments on colony entities
    - Also wire the new ProcessColonyTick (from task 3.1) if not already calling MarkDirty
    - _Satisfies: Req 2, Criterion 3_
    - _Inputs: Common/Services/ColonyService.cs (470 lines — read all public methods to identify mutation points)_
    - _Output: Common/Services/ColonyService.cs (modified)_
    - _Verification: Build succeeds; existing ColonyService tests pass_

  - [x] 8.2 Add MarkDirty/MarkDeleted calls to BlueprintService
    - BlueprintService has 7 public methods (654 lines). Add MarkDirty<Blueprint> after mutations.
    - Note: some methods operate on batches (e.g., import) — MarkDirty each entity individually
    - _Satisfies: Req 2, Criterion 3_
    - _Inputs: Common/Services/BlueprintService.cs (654 lines — read public methods to find mutation points)_
    - _Output: Common/Services/BlueprintService.cs (modified)_
    - _Verification: Build succeeds; existing BlueprintService tests pass_

  - [x] 8.3 Add MarkDirty/MarkDeleted calls to SurveyService
    - SurveyService has 4 public methods (160 lines). Simple — add MarkDirty<Survey> after mutations.
    - _Satisfies: Req 2, Criterion 3_
    - _Inputs: Common/Services/SurveyService.cs (160 lines)_
    - _Output: Common/Services/SurveyService.cs (modified)_
    - _Verification: Build succeeds_

  - [x] 8.4 Add MarkDirty/MarkDeleted calls to PlayerProfileService
    - PlayerProfileService has 4 public methods (210 lines). Add MarkDirty<PlayerProfile> after mutations.
    - _Satisfies: Req 2, Criterion 3_
    - _Inputs: Common/Services/PlayerProfileService.cs (210 lines)_
    - _Output: Common/Services/PlayerProfileService.cs (modified)_
    - _Verification: Build succeeds_


- [x] 9. Dirty entity tracking in service classes (batch 2: Delivery, Ship, Station, Market)
  - [x] 9.1 Add MarkDirty/MarkDeleted calls to DeliveryRouteService and DeliveryPlanService
    - DeliveryRouteService: 3 public methods (121 lines)
    - DeliveryPlanService: 13 public methods (718 lines) — this is a larger service, read carefully
    - _Satisfies: Req 2, Criterion 3_
    - _Inputs: Common/Services/DeliveryRouteService.cs, Common/Services/DeliveryPlanService.cs_
    - _Output: Both files modified_
    - _Verification: Build succeeds_

  - [x] 9.2 Add MarkDirty/MarkDeleted calls to ShipService and ShipTemplateService
    - ShipService: 4 public methods (172 lines)
    - ShipTemplateService: 3 public methods (114 lines)
    - _Satisfies: Req 2, Criterion 3_
    - _Inputs: Common/Services/ShipService.cs, Common/Services/ShipTemplateService.cs_
    - _Output: Both files modified_
    - _Verification: Build succeeds_

  - [x] 9.3 Add MarkDirty/MarkDeleted calls to StationService and MarketListingService
    - StationService: 3 public methods (150 lines)
    - MarketListingService: 4 public methods (161 lines)
    - _Satisfies: Req 2, Criterion 3_
    - _Inputs: Common/Services/StationService.cs, Common/Services/MarketListingService.cs_
    - _Output: Both files modified_
    - _Verification: Build succeeds_


- [x] 10. Dirty entity tracking in service classes (batch 3: Pricing, BuildPlan, StockTarget, SupplyChain)
  - [x] 10.1 Add MarkDirty/MarkDeleted calls to PricingPlanService and BuildPlanMutationService
    - PricingPlanService: 3 public methods (76 lines)
    - BuildPlanMutationService: 3 public methods (132 lines)
    - _Satisfies: Req 2, Criterion 3_
    - _Inputs: Common/Services/PricingPlanService.cs, Common/Services/BuildPlanMutationService.cs_
    - _Output: Both files modified_
    - _Verification: Build succeeds_

  - [x] 10.2 Add MarkDirty/MarkDeleted calls to StockTargetMutationService and SupplyChainMutationService
    - StockTargetMutationService: 6 public methods (213 lines)
    - SupplyChainMutationService: 3 public methods (126 lines)
    - _Satisfies: Req 2, Criterion 3_
    - _Inputs: Common/Services/StockTargetMutationService.cs, Common/Services/SupplyChainMutationService.cs_
    - _Output: Both files modified_
    - _Verification: Build succeeds_

- [x] 11. Dirty entity tracking in service classes (batch 4: Asteroid, Contacts, Banking, Mail)
  - [x] 11.1 Add MarkDirty/MarkDeleted calls to AsteroidService and ContactsService
    - AsteroidService: 3 public methods (99 lines)
    - ContactsService: 6 public methods (152 lines)
    - _Satisfies: Req 2, Criterion 3_
    - _Inputs: Common/Services/AsteroidService.cs, Common/Services/ContactsService.cs_
    - _Output: Both files modified_
    - _Verification: Build succeeds_

  - [x] 11.2 Add MarkDirty/MarkDeleted calls to BankingService and MailService
    - BankingService: static class (316 lines) — AddManualTransaction and ImportTransactionsAsync mutate data
    - MailService: static class (341 lines) — check which methods add/remove MailMessage entities
    - Note: static services access PlayerContext.GetInstance() — ensure MarkDirty is called on the instance
    - _Satisfies: Req 2, Criterion 3_
    - _Inputs: Common/Services/BankingService.cs, Common/Services/MailService.cs_
    - _Output: Both files modified_
    - _Verification: Build succeeds_

- [~] 12. Checkpoint — PlayerContext integration and dirty tracking verified
  - Build full solution with zero warnings
  - Run vstest.console (full suite, 600s timeout) + trxparse.js
  - Run dotnet test OE2EmpireTracker.Server.Tests
  - Run `node .kiro/tools/audit.js`
  - Confirm no regressions from service MarkDirty additions
  - Spot-check: add a temporary test that mutates via ColonyService then checks DirtyTracker.HasChanges == true


- [x] 13. EmpireContext storage backend integration
  - [x] 13.1 Add StorageBackend and StorageBackendType properties to EmpireContext
    - Add `public IStorageBackend StorageBackend { get; set; }` property
    - Add `public StorageBackendType? StorageBackendType { get; set; }` property
    - When no backend (null), preserve existing file I/O behavior unchanged
    - EmpireContext is 733 lines — much smaller than PlayerContext
    - Estimated: ~10 lines of property declarations
    - _Satisfies: Req 3, Criteria 1-2_
    - _Inputs: Common/Services/EmpireContext.cs (733 lines — look at existing properties near top)_
    - _Output: Common/Services/EmpireContext.cs (modified)_
    - _Verification: Build succeeds; existing tests pass (backend is null → legacy behavior)_

  - [x] 13.2 Implement LoadBaselineFromBackend — JSON backend path
    - Add private `LoadBaselineFromBackend()` method
    - For JSON backends (JsonSingleFile/JsonMultiFile): call GetGlobalDataAsync("BaselineRoot"), deserialize into BaselineRoot
    - If result is null/empty: set _needsSeedWrite flag (handled in 13.4)
    - Call existing InitFromBaselineRoot (or equivalent initialization logic)
    - Estimated: ~30 lines
    - _Satisfies: Req 3, Criteria 3, 9_
    - _Inputs: Common/Services/EmpireContext.cs (find existing file load logic to understand initialization pattern)_
    - _Output: Common/Services/EmpireContext.cs (modified)_
    - _Verification: Build succeeds_

  - [x] 13.3 Implement LoadBaselineFromBackend — relational backend path
    - For relational backends (Sqlite/DynamoDb/Postgres): call typed Get methods for each baseline collection
    - GetBaselineGameConstantsAsync, GetAllBlueprintTypesAsync, GetAllShipClassesAsync, GetAllTechLevelsAsync, GetAllCommoditiesAsync, GetAllRefiningRecipesAsync, GetAllResearchTimesAsync, GetAllPropertyTypeDefinitionsAsync
    - If constants is null: set _needsSeedWrite flag
    - Build BaselineRoot from loaded collections, call initialization
    - Estimated: ~40 lines (8 backend calls via Task.Run + BaselineRoot assembly)
    - _Satisfies: Req 3, Criteria 4, 9_
    - _Inputs: Common/Services/EmpireContext.cs, Common/Interfaces/IStorageBackend.cs (baseline method signatures)_
    - _Output: Common/Services/EmpireContext.cs (modified)_
    - _Verification: Build succeeds_

  - [x] 13.4 Implement baseline seeding from BaselineData.json
    - When _needsSeedWrite is true after LoadBaselineFromBackend:
      - Load BaselineData.json from disk (same path as legacy file I/O)
      - Deserialize into BaselineRoot, initialize in-memory collections
      - Call WriteContext() to persist into backend
      - Log seed completion
    - If BaselineData.json also missing: init empty BaselineRoot with default BaselineGameConstants
    - Estimated: ~25 lines
    - _Satisfies: Req 3, Criterion 5_
    - _Inputs: Common/Services/EmpireContext.cs (find FilePath and existing JSON load logic)_
    - _Output: Common/Services/EmpireContext.cs (modified)_
    - _Verification: Build succeeds_

  - [x] 13.5 Modify EmpireContext WriteContext for backend persistence
    - For JSON backends: serialize BaselineRoot to JSON (existing pattern), call UpsertGlobalDataAsync via Task.Run
    - For relational backends: call typed Upsert methods per baseline collection (8 calls via Task.Run)
    - Preserve legacy file write when no backend configured
    - On StorageLoadException during load: propagate without partial init
    - Estimated: ~40 lines (branching + 8 upsert calls for relational path)
    - _Satisfies: Req 3, Criteria 6-7, 9_
    - _Inputs: Common/Services/EmpireContext.cs (existing WriteContext logic)_
    - _Output: Common/Services/EmpireContext.cs (modified)_
    - _Verification: Build succeeds_

  - [x] 13.6 Wire EmpireContext load path to use LoadBaselineFromBackend
    - In the constructor/load-data path: when StorageBackend is non-null, call LoadBaselineFromBackend instead of file load
    - On StorageLoadException: propagate without partial initialization
    - Estimated: ~15 lines of branching
    - _Satisfies: Req 3, Criteria 7-8_
    - _Inputs: Common/Services/EmpireContext.cs (find where BaselineData.json is currently loaded)_
    - _Output: Common/Services/EmpireContext.cs (modified)_
    - _Verification: Build succeeds_

  - [x] 13.7 Write unit tests for EmpireContext backend integration
    - Test: Load from JSON backend uses GetGlobalDataAsync (mock backend)
    - Test: Load from relational backend uses typed methods (mock backend)
    - Test: Empty backend triggers seed from BaselineData.json (verify Upsert called after seed)
    - Test: WriteContext with JSON backend calls UpsertGlobalDataAsync
    - Test: WriteContext with relational backend calls typed Upsert methods
    - Test: No backend → legacy file write preserved
    - Test: StorageLoadException propagated, no partial init
    - _Satisfies: Req 3, Criteria 1-9_
    - _Inputs: Common/Services/EmpireContext.cs_
    - _Output: OE2EmpireTracker.Tests/Services/EmpireContextBackendTests.cs (NEW)_
    - _Verification: EmpireContextBackendTests pass in vstest.console_


- [x] 14. Backend selection preferences
  - [x] 14.1 Add storage backend properties to UIPreferences
    - Add StorageBackendType (string, default "JsonSingleFile")
    - Add StoragePath, StorageAwsRegion, StorageTablePrefix, StorageConnectionString
    - All with [JsonProperty] attributes and [DefaultValue] where appropriate
    - Follow existing UIPreferences pattern for attribute style (check SA1133 compliance)
    - Estimated: ~25 lines
    - _Satisfies: Req 5, Criteria 1-6_
    - _Inputs: Common/Models/UIPreferences.cs (read existing properties to match style)_
    - _Output: Common/Models/UIPreferences.cs (modified)_
    - _Verification: Build succeeds_

  - [x] 14.2 Add ResolveStorageConfig and ParseStorageBackendType to PreferencesStore
    - ParseStorageBackendType: Enum.TryParse with fallback to JsonSingleFile + warning log
    - ResolveStorageConfig: switch on type, apply default paths per backend type
    - JsonSingleFile/JsonMultiFile default: AppDomain.CurrentDomain.BaseDirectory
    - Sqlite default: Path.Combine(Environment.GetFolderPath(SpecialFolder.LocalApplicationData), "OE2EmpireTracker")
    - DynamoDb: use StorageAwsRegion + StorageTablePrefix
    - Postgres: use StorageConnectionString
    - PreferencesStore is only 117 lines — this adds ~40 lines
    - _Satisfies: Req 5, Criteria 1-6, 8_
    - _Inputs: Common/Services/PreferencesStore.cs (117 lines — read to understand existing pattern)_
    - _Output: Common/Services/PreferencesStore.cs (modified)_
    - _Verification: Build succeeds_

  - [x] 14.3 Write unit tests for PreferencesStore storage config resolution
    - Test: valid backend types parsed correctly (each enum value)
    - Test: unrecognized type falls back to JsonSingleFile with warning
    - Test: null/empty falls back to JsonSingleFile
    - Test: default paths applied when StoragePath is null/empty for each file-based type
    - Test: DynamoDB config populated from preferences (region + prefix)
    - Test: Postgres config populated from preferences (connection string)
    - _Satisfies: Req 5, Criteria 1-8_
    - _Inputs: Common/Services/PreferencesStore.cs_
    - _Output: OE2EmpireTracker.Tests/Services/PreferencesStoreBackendTests.cs (NEW)_
    - _Verification: PreferencesStoreBackendTests pass in vstest.console_


- [ ] 15. Desktop startup wiring
  - [x] 15.1 Implement InitializeStorage in Program.cs
    - Load preferences via PreferencesStore.GetInstance()
    - Call ParseStorageBackendType() and ResolveStorageConfig()
    - Call StorageBackendFactory.CreateAsync via Task.Run bridge
    - Wire StorageBackend into PlayerContext.GetInstance() and EmpireContext.GetInstance()
    - Set EmpireContext.StorageBackendType
    - Estimated: ~25 lines
    - _Satisfies: Req 5, Criterion 7_
    - _Inputs: OE2EmpireTracker/Program.cs (find existing startup sequence), Common/Services/PreferencesStore.cs_
    - _Output: OE2EmpireTracker/Program.cs (modified)_
    - _Verification: Build succeeds_

  - [-] 15.2 Add error dialog and fallback logic in Program.cs
    - Wrap InitializeStorage in try/catch
    - On exception: show MessageBox with error message describing backend type and failure
    - Offer "Yes" = fall back to JsonSingleFile with default path, "No" = exit
    - On fallback: create JsonSingleFile backend, wire into contexts
    - Estimated: ~25 lines
    - _Satisfies: Req 5, Criterion 9_
    - _Inputs: OE2EmpireTracker/Program.cs_
    - _Output: OE2EmpireTracker/Program.cs (modified)_
    - _Verification: Build succeeds_

- [~] 16. Checkpoint — EmpireContext, preferences, and startup wiring verified
  - Build full solution with zero warnings
  - Run vstest.console (full suite, 600s timeout) + trxparse.js
  - Run dotnet test OE2EmpireTracker.Server.Tests
  - Run `node .kiro/tools/audit.js`
  - Manual verification: confirm application starts correctly with default JsonSingleFile backend
  - Manual verification: set StorageBackendType to "Sqlite" in UIPreferences.json, confirm app initializes SQLite


- [ ] 17. Migration service — models and exception
  - [-] 17.1 Create MigrationProgress model and MigrationValidationException
    - Create Common/Services/MigrationProgress.cs with CurrentEntityType (string), EntitiesProcessed (int), Phase (string)
    - Add MigrationValidationException to Common/Interfaces/StorageExceptions.cs with Mismatches property (Dictionary<string, (int Expected, int Actual)>)
    - Estimated: ~40 lines total across both files
    - _Satisfies: Req 6, Criteria 5, 8_
    - _Inputs: Common/Interfaces/StorageExceptions.cs (162 lines — see existing exception patterns)_
    - _Output: Common/Services/MigrationProgress.cs (NEW), Common/Interfaces/StorageExceptions.cs (modified)_
    - _Verification: Build succeeds_

- [ ] 18. Migration service — core implementation
  - [~] 18.1 Create MigrationService class with MigrateAsync skeleton and character discovery
    - Create Common/Services/MigrationService.cs
    - Implement MigrateAsync signature (source, destination, progress, characterUUIDs, ct)
    - Character discovery: if characterUUIDs is null, call source.GetAllCharacterUUIDsAsync()
    - Set up progress tracking (cumulative counter)
    - Estimated: ~40 lines (class shell + discovery + progress infrastructure)
    - _Satisfies: Req 6, Criteria 1, 3, 10_
    - _Inputs: Common/Interfaces/IStorageBackend.cs, Common/Services/MigrationProgress.cs_
    - _Output: Common/Services/MigrationService.cs (NEW)_
    - _Verification: Build succeeds_

  - [~] 18.2 Implement server-global data migration in MigrationService
    - Migrate Factions: source.GetAllFactionsAsync() → destination.UpsertFactionAsync() per entity
    - Migrate ExternalCharacters, StarSystems, Tokens, MembershipActions (same pattern)
    - Report progress per entity type
    - Estimated: ~60 lines (5 entity types × ~12 lines each for get-all + loop + upsert + progress)
    - _Satisfies: Req 6, Criteria 2, 4-5_
    - _Inputs: Common/Services/MigrationService.cs, Common/Interfaces/IStorageBackend.cs (find global entity methods)_
    - _Output: Common/Services/MigrationService.cs (modified)_
    - _Verification: Build succeeds_

  - [~] 18.3 Implement baseline data migration in MigrationService
    - Migrate GameConstants, BlueprintTypes, ShipClasses, TechLevels, Commodities, RefiningRecipes, ResearchTimes, PropertyTypeDefinitions
    - Each: get from source → upsert to destination → report progress
    - Estimated: ~50 lines (8 baseline types, simpler pattern since they're single-entity or small collections)
    - _Satisfies: Req 6, Criteria 2, 4-5_
    - _Inputs: Common/Services/MigrationService.cs_
    - _Output: Common/Services/MigrationService.cs (modified)_
    - _Verification: Build succeeds_

  - [~] 18.4 Implement per-character entity migration — first 11 entity types
    - For each character UUID: migrate Colonies, Blueprints, Surveys, PlayerProfiles, DeliveryRoutes, DeliveryPlans, PricingPlans, BuildPlans, ShipTemplates, Ships, Stations
    - Pattern per type: source.GetAllXxxAsync(charUUID) → loop → destination.UpsertXxxAsync(charUUID, entity)
    - Report progress per type per character
    - Estimated: ~80 lines (11 types × ~7 lines each)
    - _Satisfies: Req 6, Criteria 2, 4-5_
    - _Inputs: Common/Services/MigrationService.cs, Common/Interfaces/IStorageBackend.cs_
    - _Output: Common/Services/MigrationService.cs (modified)_
    - _Verification: Build succeeds_

  - [~] 18.5 Implement per-character entity migration — remaining 11 entity types
    - Continue: MarketListings, MarketTransactions, StockPlans, StockProfiles, SupplyChains, WarehouseOverflowRules, Asteroids, BankingTransactions, MailMessages, SharingRules, CharacterPreferences
    - Same pattern as 18.4
    - Estimated: ~80 lines (11 types × ~7 lines each)
    - _Satisfies: Req 6, Criteria 2, 4-5_
    - _Inputs: Common/Services/MigrationService.cs_
    - _Output: Common/Services/MigrationService.cs (modified)_
    - _Verification: Build succeeds_

  - [~] 18.6 Implement permissions/intel/audit migration
    - Migrate FactionCapabilities, ClearanceLevels, Groups, GroupMembers, IntelComments, IntelShares, AuditEntries
    - Same get-all → loop → upsert pattern
    - Estimated: ~50 lines (7 types × ~7 lines each)
    - _Satisfies: Req 6, Criterion 2_
    - _Inputs: Common/Services/MigrationService.cs, Common/Interfaces/IStorageBackend.cs (permission/intel methods)_
    - _Output: Common/Services/MigrationService.cs (modified)_
    - _Verification: Build succeeds_

  - [~] 18.7 Implement count validation in MigrationService
    - Add private ValidateCountsAsync method
    - For each entity type: get count from source (GetAll + .Count), get count from destination, compare
    - On mismatch: collect into Mismatches dictionary, throw MigrationValidationException
    - Include error message with entity type being processed when failure occurred + count of entities migrated
    - Estimated: ~50 lines
    - _Satisfies: Req 6, Criteria 6-8_
    - _Inputs: Common/Services/MigrationService.cs, Common/Interfaces/StorageExceptions.cs_
    - _Output: Common/Services/MigrationService.cs (modified)_
    - _Verification: Build succeeds_

  - [~] 18.8 Write unit tests for MigrationService
    - Test: empty source → empty destination, no errors, validation passes
    - Test: single character migration transfers all entity types (use InMemory or JsonSingleFile backend)
    - Test: count validation passes on correct migration
    - Test: artificially cause count mismatch → throws MigrationValidationException with correct Mismatches
    - Test: progress callback invoked at least once per entity type
    - Test: selective migration (explicit UUIDs) only migrates specified characters
    - Test: source read failure throws StorageLoadException with entity type in message
    - _Satisfies: Req 6, Criteria 1-10_
    - _Inputs: Common/Services/MigrationService.cs_
    - _Output: OE2EmpireTracker.Tests/Services/MigrationServiceTests.cs (NEW)_
    - _Verification: MigrationServiceTests pass in vstest.console_


- [ ] 19. Desktop migration workflow wiring
  - [~] 19.1 Implement migration detection and offer in Program.cs
    - Add DetectPreviousBackendData(): check for PlayerData.json in exe dir, data/ subfolder, .db file
    - If configured backend type differs from what's detected AND new backend is empty:
      - Show MessageBox: "Migrate existing data to new backend?" [Migrate] [Start Fresh] [Cancel]
      - On "Migrate": create source backend (detected type), run MigrationService.MigrateAsync
      - On "Start Fresh": continue with empty backend
      - On "Cancel": Environment.Exit(0)
    - Estimated: ~50 lines
    - _Satisfies: Req 6, Criterion 9; Req 5, Criterion 7_
    - _Inputs: OE2EmpireTracker/Program.cs (existing startup), Common/Services/MigrationService.cs_
    - _Output: OE2EmpireTracker/Program.cs (modified)_
    - _Verification: Build succeeds_

  - [~] 19.2 Create FormMigrationProgress dialog
    - Simple WinForms dialog (Designer + code-behind)
    - Controls: lblPhase (Label), lblEntityType (Label), lblCount (Label), progressBar (ProgressBar — marquee mode)
    - Public UpdateProgress(MigrationProgress progress) method to update labels from UI thread
    - No cancel button (migration is fast for local backends)
    - Estimated: ~40 lines code-behind + ~60 lines Designer
    - _Satisfies: Req 6, Criterion 5_
    - _Inputs: Design document FormMigrationProgress section_
    - _Output: OE2EmpireTracker/Forms/FormMigrationProgress.cs (NEW), FormMigrationProgress.Designer.cs (NEW), FormMigrationProgress.resx (NEW)_
    - _Verification: Build succeeds_

- [~] 20. Checkpoint — Migration service and wiring verified
  - Build full solution with zero warnings
  - Run vstest.console (full suite, 600s timeout) + trxparse.js
  - Run dotnet test OE2EmpireTracker.Server.Tests
  - Run `node .kiro/tools/audit.js`
  - Manual verification: change backend type in UIPreferences.json → app offers migration on startup


- [ ] 21. Round-trip fidelity property tests — generator infrastructure
  - [~] 21.1 Create GenHelpers utility class for FsCheck entity generators
    - Create OE2EmpireTracker.Tests/Services/GenHelpers.cs (NEW)
    - Provide reusable generator combinators: GenUUID, GenOptionalString, GenDecimal, GenDateTime, GenOptionalList<T>, GenNullable<T>
    - Use FsCheck 2.16.6 LINQ query syntax (from x in Gen.Choose(...) select ...)
    - These reduce per-entity generator code from ~50 lines to ~15-20 lines
    - Estimated: ~80 lines
    - _Satisfies: Req 7, Criteria 4-6 (enables proper edge case generation)_
    - _Inputs: None (new file, references FsCheck 2.16.6 Gen API)_
    - _Output: OE2EmpireTracker.Tests/Services/GenHelpers.cs (NEW)_
    - _Verification: Build succeeds_

  - [~] 21.2 Create FsCheck generators for Colony and Blueprint entities
    - Colony: UUID, Name, OwnerUUID, ColonyStructures (list of nested objects), resources, timers
    - Blueprint: UUID, Name, OwnerUUID, Type, all property fields, BuildItems list
    - Both have complex nested structures — use GenHelpers for common fields
    - Estimated: ~80 lines (two complex entity generators)
    - _Satisfies: Req 7, Criteria 1, 4-6_
    - _Inputs: Common/Models/Colony.cs, Common/Models/Blueprint.cs (read all properties), GenHelpers.cs_
    - _Output: OE2EmpireTracker.Tests/Services/EntityGenerators.cs (NEW)_
    - _Verification: Build succeeds; can generate 10 instances without exception_

  - [~] 21.3 Create FsCheck generators for Survey, PlayerProfile, DeliveryRoute, DeliveryPlan
    - Survey: UUID, OwnerUUID, ResourceReserves (nested list), SurveyType enum
    - PlayerProfile: UUID, Name, Skills dictionary, Rank
    - DeliveryRoute: UUID, Stops list (nested objects with StationUUID, items)
    - DeliveryPlan: UUID, RouteUUID, Items, schedule fields
    - Estimated: ~100 lines (four entities, moderate complexity)
    - _Satisfies: Req 7, Criteria 1, 4-6_
    - _Inputs: Corresponding model files in Common/Models/, GenHelpers.cs_
    - _Output: OE2EmpireTracker.Tests/Services/EntityGenerators.cs (appended)_
    - _Verification: Build succeeds_

  - [~] 21.4 Create FsCheck generators for Ship, ShipTemplate, Station, MarketListing, MarketTransaction
    - MarketTransaction has decimal fields (PricePerUnit, TotalPrice) — use GenDecimal for full precision range
    - Station has nested SystemUUID references
    - Estimated: ~100 lines (five entities)
    - _Satisfies: Req 7, Criteria 1-2, 4-6_
    - _Inputs: Corresponding model files in Common/Models/, GenHelpers.cs_
    - _Output: OE2EmpireTracker.Tests/Services/EntityGenerators.cs (appended)_
    - _Verification: Build succeeds_

  - [~] 21.5 Create FsCheck generators for PricingPlan, BuildPlan, StockPlan, StockProfile, SupplyChain, WarehouseOverflowRule
    - Six relatively simple entities (fewer nested objects than Colony/Blueprint)
    - Estimated: ~90 lines
    - _Satisfies: Req 7, Criteria 1, 4-6_
    - _Inputs: Corresponding model files in Common/Models/, GenHelpers.cs_
    - _Output: OE2EmpireTracker.Tests/Services/EntityGenerators.cs (appended)_
    - _Verification: Build succeeds_

  - [~] 21.6 Create FsCheck generators for Faction, ExternalCharacter, Asteroid, BankingTransaction, MailMessage, and baseline types
    - BankingTransaction has decimal fields — critical for precision testing
    - Baseline types: BlueprintType, ShipClass, TechLevel, Commodity, RefiningRecipe, ResearchTime, PropertyTypeDefinition
    - Estimated: ~120 lines (12 entities, all relatively simple)
    - _Satisfies: Req 7, Criteria 1, 4-7_
    - _Inputs: Corresponding model files, GenHelpers.cs_
    - _Output: OE2EmpireTracker.Tests/Services/EntityGenerators.cs (appended)_
    - _Verification: Build succeeds_


- [ ] 22. Round-trip fidelity property tests — test infrastructure
  - [~] 22.1 Create MigrationRoundTripPropertyTests fixture with round-trip helper
    - Create OE2EmpireTracker.Tests/Services/MigrationRoundTripPropertyTests.cs
    - Implement RoundTripPreservesEquality<T> helper method:
      1. Create temp source backend, write entity
      2. Create temp destination backend
      3. Migrate source → destination via MigrationService
      4. Migrate destination → source2 (fresh source) via MigrationService
      5. Serialize original and round-tripped with JsonSettings.SerializerSettings + SerializationSorter
      6. Assert JSON strings are identical
    - Backend pair setup/teardown helpers (temp directories, cleanup)
    - Estimated: ~80 lines (helper + setup/teardown infrastructure)
    - _Satisfies: Req 7, Criteria 1, 8_
    - _Inputs: Common/Services/MigrationService.cs, EntityGenerators.cs, Common/Services/SerializationSorter.cs_
    - _Output: OE2EmpireTracker.Tests/Services/MigrationRoundTripPropertyTests.cs (NEW)_
    - _Verification: Build succeeds_

  - [~] 22.2 Write property tests for JsonSingleFile↔Sqlite round-trip (first 11 entity types)
    - [FsCheck.NUnit.Property(MaxTest = 100)] for: Colony, Blueprint, Survey, PlayerProfile, DeliveryRoute, DeliveryPlan, PricingPlan, BuildPlan, ShipTemplate, Ship, Station
    - Each test: generate entity, run round-trip helper, assert equality
    - Estimated: ~60 lines (11 short test methods, each ~5 lines)
    - _Satisfies: Req 7, Criteria 1-8_
    - _Inputs: EntityGenerators.cs, MigrationRoundTripPropertyTests.cs_
    - _Output: MigrationRoundTripPropertyTests.cs (appended)_
    - _Verification: Round-trip tests pass for JsonSingleFile↔Sqlite pair (first 11 types)_

  - [~] 22.3 Write property tests for JsonSingleFile↔Sqlite round-trip (remaining 11 entity types)
    - [FsCheck.NUnit.Property(MaxTest = 100)] for: MarketListing, MarketTransaction, StockPlan, StockProfile, SupplyChain, WarehouseOverflowRule, Asteroid, BankingTransaction, MailMessage, Faction, ExternalCharacter
    - Special focus on MarketTransaction and BankingTransaction decimal precision
    - Estimated: ~60 lines
    - _Satisfies: Req 7, Criteria 1-8_
    - _Inputs: EntityGenerators.cs, MigrationRoundTripPropertyTests.cs_
    - _Output: MigrationRoundTripPropertyTests.cs (appended)_
    - _Verification: Round-trip tests pass for JsonSingleFile↔Sqlite pair (all 22 types)_

  - [~] 22.4 Write property tests for JsonSingleFile↔JsonMultiFile round-trip (all entity types)
    - [FsCheck.NUnit.Property(MaxTest = 100)] for all 22 entity types
    - Can use a parameterized helper since both are JSON — differences are in file layout, not serialization
    - Estimated: ~60 lines (reuse same test pattern with different backend pair factory)
    - _Satisfies: Req 7, Criteria 1-8_
    - _Inputs: EntityGenerators.cs, MigrationRoundTripPropertyTests.cs_
    - _Output: MigrationRoundTripPropertyTests.cs (appended) OR separate fixture class_
    - _Verification: Round-trip tests pass for JsonSingleFile↔JsonMultiFile pair_

  - [~] 22.5 Write property tests for Sqlite↔Postgres round-trip (all entity types)
    - [FsCheck.NUnit.Property(MaxTest = 100)] for all 22 entity types
    - This pair specifically validates the REAL→TEXT and DOUBLE PRECISION→NUMERIC fixes
    - Special focus: decimal values with many decimal places, negative decimals, zero
    - Note: requires Postgres to be available in test environment (may need skip attribute if unavailable)
    - Estimated: ~60 lines
    - _Satisfies: Req 7, Criteria 1-8_
    - _Inputs: EntityGenerators.cs, MigrationRoundTripPropertyTests.cs_
    - _Output: MigrationRoundTripPropertyTests.cs (appended) OR separate fixture class_
    - _Verification: Round-trip tests pass for Sqlite↔Postgres pair_


- [ ] 23. Dirty tracking property tests
  - [~] 23.1 Write property tests for DirtyTracker correctness
    - **Property 1: Write Idempotency** — after WriteContext with no mutations, DirtyTracker.HasChanges == false AND no backend calls made
    - **Property 2: Dirty Flag Completeness** — for a random sequence of service mutations, every mutated entity appears in DirtyTracker. Use reflection to call random service methods, then check tracker.
    - **Property 3: Load-Write Round Trip** — LoadFromBackend then immediate WriteContext → zero dirty entities → zero backend Upsert calls
    - Estimated: ~100 lines
    - _Satisfies: Req 2, Criteria 4-7_
    - _Inputs: Common/Services/DirtyTracker.cs, Common/Services/PlayerContext.cs, all 18 service classes_
    - _Output: OE2EmpireTracker.Tests/Services/DirtyTrackingPropertyTests.cs (NEW)_
    - _Verification: DirtyTrackingPropertyTests pass in vstest.console_

  - [~] 23.2 Write property test for WritesBlocked monotonicity
    - **Property 4: WritesBlocked Monotonicity** — configure mock backend to throw StorageWriteException after N successful writes. After exception: WritesBlocked == true, subsequent WriteContext calls produce zero backend calls, no exceptions thrown.
    - **Property 5: WritesBlocked Reset** — after setting WritesBlocked = false, next WriteContext resumes normal behavior
    - Estimated: ~60 lines
    - _Satisfies: Req 8, Criteria 1-4_
    - _Inputs: Common/Services/PlayerContext.cs_
    - _Output: OE2EmpireTracker.Tests/Services/DirtyTrackingPropertyTests.cs (appended)_
    - _Verification: DirtyTrackingPropertyTests pass in vstest.console_

- [~] 24. Final checkpoint — Full integration verified
  - Build full solution with zero warnings
  - Run vstest.console (full suite, 600s timeout) + trxparse.js — ALL tests pass
  - Run dotnet test OE2EmpireTracker.Server.Tests — ALL tests pass
  - Run npx vitest run from OE2EmpireTracker.Web/ — ALL tests pass (if frontend exists)
  - Run `node .kiro/tools/audit.js` — zero findings
  - Verify all 8 requirements' acceptance criteria are covered by at least one passing test
  - Traceability check: every acceptance criterion maps to at least one task

## Notes

- Each task includes LOC estimates based on actual file sizes measured in the codebase
- The EntityPersistenceMap (task 6.2) is the critical design decision that keeps tasks 6.3 and 7.2 under 200 LOC
- GetAllCharacterUUIDsAsync is split into one task per backend (2.2-2.6) because backend files are 1500-7700 lines each
- MarkDirty service tasks (8-11) are batched by complexity: large services like BlueprintService (654 lines) and DeliveryPlanService (718 lines) get their own subtask; small services are paired
- Property test entity generators are split into groups of ~4-6 entities to stay under 120 lines per task, with GenHelpers reducing boilerplate
- Round-trip property tests are split to max ~11 entity types per task per backend pair

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.3", "2.1"] },
    { "id": 1, "tasks": ["1.2", "1.4", "2.2", "2.3", "2.4", "2.5", "2.6"] },
    { "id": 2, "tasks": ["2.7", "3.1", "5.1"] },
    { "id": 3, "tasks": ["3.2", "5.2", "6.1"] },
    { "id": 4, "tasks": ["6.2"] },
    { "id": 5, "tasks": ["6.3", "6.4", "14.1"] },
    { "id": 6, "tasks": ["7.1", "7.2", "14.2"] },
    { "id": 7, "tasks": ["7.3", "7.4", "14.3"] },
    { "id": 8, "tasks": ["8.1", "8.2", "8.3", "8.4"] },
    { "id": 9, "tasks": ["9.1", "9.2", "9.3"] },
    { "id": 10, "tasks": ["10.1", "10.2"] },
    { "id": 11, "tasks": ["11.1", "11.2"] },
    { "id": 12, "tasks": ["13.1"] },
    { "id": 13, "tasks": ["13.2", "13.3"] },
    { "id": 14, "tasks": ["13.4", "13.5"] },
    { "id": 15, "tasks": ["13.6", "13.7"] },
    { "id": 16, "tasks": ["15.1"] },
    { "id": 17, "tasks": ["15.2", "17.1"] },
    { "id": 18, "tasks": ["18.1"] },
    { "id": 19, "tasks": ["18.2", "18.3"] },
    { "id": 20, "tasks": ["18.4", "18.5"] },
    { "id": 21, "tasks": ["18.6", "18.7"] },
    { "id": 22, "tasks": ["18.8", "19.1"] },
    { "id": 23, "tasks": ["19.2"] },
    { "id": 24, "tasks": ["21.1", "21.2"] },
    { "id": 25, "tasks": ["21.3", "21.4"] },
    { "id": 26, "tasks": ["21.5", "21.6"] },
    { "id": 27, "tasks": ["22.1"] },
    { "id": 28, "tasks": ["22.2", "22.3"] },
    { "id": 29, "tasks": ["22.4", "22.5"] },
    { "id": 30, "tasks": ["23.1", "23.2"] }
  ]
}
```
