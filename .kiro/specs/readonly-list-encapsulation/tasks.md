# Implementation Plan: Read-Only List Encapsulation

## Overview

Large refactor of PlayerContext (20 lists) and EmpireContext (9 lists) to encapsulate public `List<T>` fields behind `IReadOnlyList<T>` properties with private backing fields. All mutations route through dedicated Add/Remove methods that maintain UUID caches inline. Callers are migrated mechanically. The compiler enforces correctness after the switch — any missed `.Add()`/`.Remove()` calls on the public property fail to compile.

Tasks are ordered so each builds on the previous and leaves the solution in a compilable state. The core infrastructure is built first, then callers are migrated by area.

## Tasks

- [x] 1. Core infrastructure — PlayerContext backing fields, properties, and mutation methods for Blueprint, Survey, Colony
  - [x] 1.1 Convert BlueprintList, SurveyList, ColonyList from public fields to private backing fields with `IReadOnlyList<T>` properties
    - `private List<Blueprint> _blueprintList;` + `public IReadOnlyList<Blueprint> BlueprintList => _blueprintList;`
    - Same pattern for Survey and Colony
    - Update all internal references within PlayerContext (Init, WriteContext, Snapshot, BindingSource setup) to use the backing field
    - _Requirements: 1.1, 1.2, 1.3, 7.1, 7.3, 8.1, 12.1, 12.2_
  - [x] 1.2 Add mutation methods for Blueprint (Pattern A: UUID cache + BindingSource + derived caches)
    - `AddBlueprint(Blueprint item)` and `RemoveBlueprint(Blueprint item)`
    - Lock on `_listLock`, inline UUID cache update, invalidate `_allBlueprintsCache` and `_blueprintTypeCountCache`
    - Call `BindingSourceBlueprint?.ResetBindings(false)` outside the lock
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 5.1, 5.2, 7.4, 13.1, 13.3_
  - [x] 1.3 Add mutation methods for Survey (Pattern B: UUID cache + BindingSource)
    - `AddSurvey(Survey item)` and `RemoveSurvey(Survey item)`
    - Lock, inline UUID cache update, `BindingSourceSurvey?.ResetBindings(false)`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 5.1, 5.2, 7.4_
  - [x] 1.4 Add mutation methods for Colony (Pattern B: UUID cache + BindingSource)
    - `AddColony(Colony item)` and `RemoveColony(Colony item)`
    - Lock, inline UUID cache update, `BindingSourceColony?.ResetBindings(false)`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 5.1, 5.2, 7.4_

- [x] 2. Remove fallback linear scans from FindBlueprint, FindSurvey, FindColony
  - [x] 2.1 Remove the `FirstOrDefault` fallback from FindBlueprint
    - Remove the linear scan block after the cache TryGetValue
    - Keep the EmpireContext.FindGlobalBlueprint fallback (that's a different code path)
    - _Requirements: 6.1, 6.4_
  - [x] 2.2 Remove the `FirstOrDefault` fallback from FindSurvey
    - _Requirements: 6.2, 6.4_
  - [x] 2.3 Remove the `FirstOrDefault` fallback from FindColony
    - _Requirements: 6.3, 6.4_

- [x] 3. Add UUID caches and Find methods for the 10 entities that don't have them yet
  - [x] 3.1 Add cache field, Find method, and Invalidate method for PlayerProfile
    - `private Dictionary<string, PlayerProfile> _playerProfileCache;`
    - `FindPlayerProfile(string id)` with lazy-init pattern
    - `InvalidatePlayerProfileCache()`
    - _Requirements: 5.3, 5.4, 10.1, 10.3_
  - [x] 3.2 Add cache, Find, and Invalidate for DeliveryRoute, DeliveryPlan, PricingPlan
    - Same lazy-init dictionary cache pattern for each
    - _Requirements: 5.3, 5.4, 10.1, 10.3_
  - [x] 3.3 Add cache, Find, and Invalidate for MarketTransaction, StockPlan, StockProfile
    - _Requirements: 5.3, 5.4, 10.1, 10.3_
  - [x] 3.4 Add cache, Find, and Invalidate for SupplyChain, WarehouseOverflowRule, ExternalCharacter
    - _Requirements: 5.3, 5.4, 10.1, 10.3_

- [x] 4. Mutation methods for remaining 17 PlayerContext entities
  - [x] 4.1 Add mutation methods for PlayerProfile (Pattern E: UUID cache + BindingSource)
    - `AddPlayerProfile` / `RemovePlayerProfile` with lock, inline cache update, `BindingSourcePlayerProfile?.ResetBindings(false)`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 5.1, 5.2_
  - [x] 4.2 Add mutation methods for BuildPlan (Pattern D: UUID cache + derived caches)
    - `AddBuildPlan` / `RemoveBuildPlan` with lock, inline cache update, invalidate `_blueprintBuildItemIndex` and `_buildLocationBuildItemIndex`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 13.2, 13.3_
  - [x] 4.3 Add mutation methods for Station, ShipTemplate, Ship, Asteroid, Faction, MarketListing (Pattern C: UUID cache only)
    - Each gets `Add{Entity}` / `Remove{Entity}` with lock and inline cache update
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 5.1, 5.2_
  - [x] 4.4 Add mutation methods for DeliveryRoute, DeliveryPlan, PricingPlan, MarketTransaction, StockPlan, StockProfile, SupplyChain, WarehouseOverflowRule, ExternalCharacter (Pattern C: UUID cache only)
    - Each gets `Add{Entity}` / `Remove{Entity}` with lock and inline cache update using the new caches from task 3
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 5.1, 5.2_
  - [x] 4.5 Convert remaining 17 PlayerContext list fields to private backing fields with `IReadOnlyList<T>` properties
    - All 17 fields not covered in task 1: PlayerProfileList, DeliveryRouteList, DeliveryPlanList, PricingPlanList, BuildPlanList, ShipTemplateList, ShipList, StationList, MarketListingList, MarketTransactionList, StockPlanList, StockProfileList, SupplyChainList, WarehouseOverflowRuleList, FactionList, ExternalCharacterList, AsteroidList
    - Update all internal references within PlayerContext (Init, WriteContext, Snapshot, BindingSource setup)
    - _Requirements: 1.1, 1.2, 1.3, 7.1, 7.3, 8.1, 12.1, 12.2_

- [x] 5. Checkpoint — Verify PlayerContext compiles and internal tests pass
  - Ensure all tests pass, ask the user if questions arise.
  - Run `getDiagnostics` on PlayerContext.cs to verify no compile errors
  - Build the solution and run the test suite

- [x] 6. EmpireContext encapsulation
  - [x] 6.1 Convert all 9 EmpireContext list fields to private backing fields with `IReadOnlyList<T>` properties
    - GlobalBlueprintList, BlueprintTypeList, ShipClassList, TechLevelList, EvolutionList, ResourceList, ResourceGroupList, ResourcePurityList, CommodityList
    - Update all internal references (Init, WriteContext, BindingSource setup)
    - _Requirements: 2.1, 2.2, 2.3, 7.2, 7.3, 8.2_
  - [x] 6.2 Add mutation methods for GlobalBlueprint (UUID cache, no BindingSource)
    - `AddGlobalBlueprint` / `RemoveGlobalBlueprint` with inline cache update
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 5.5_
  - [x] 6.3 Add mutation methods for Commodity (name cache with `_commodityLock`)
    - `AddCommodity` / `RemoveCommodity` with `_commodityLock`, inline name cache update
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 5.5_
  - [x] 6.4 Add mutation methods for BindingSource-only lists (BlueprintType, ShipClass, TechLevel, Evolution, Resource, ResourceGroup, ResourcePurity)
    - Each gets `Add{Entity}` / `Remove{Entity}` with `ResetBindings(false)`
    - No UUID cache, no lock (single-threaded init only)
    - _Requirements: 4.1, 4.2, 4.5_

- [x] 7. Checkpoint — Verify both contexts compile cleanly
  - Ensure all tests pass, ask the user if questions arise.
  - Run `getDiagnostics` on PlayerContext.cs and EmpireContext.cs
  - Build the solution — at this point, external callers that mutate lists will fail to compile, which is expected


- [x] 8. Install FsCheck and write property-based tests
  - [x] 8.1 Install FsCheck NuGet packages
    - Add `FsCheck` and `FsCheck.NUnit` to `OE2EmpireTracker.Tests/packages.config`
    - Add assembly references to the test `.csproj`
    - _Requirements: (testing infrastructure)_
  - [x] 8.2 Write property test for Property 1: Add-then-Find round trip
    - **Property 1: Add-then-Find round trip**
    - For Blueprint, Survey, Colony, Station (representative sample of patterns A/B/C)
    - Add entity via mutation method, Find by UUID, assert same instance returned
    - **Validates: Requirements 3.5, 4.3, 5.1, 5.5, 6.1, 6.2, 6.3**
  - [x] 8.3 Write property test for Property 2: Remove-then-Find returns null
    - **Property 2: Remove-then-Find returns null**
    - Add entity, remove it, Find by UUID, assert null returned
    - **Validates: Requirements 3.6, 4.4, 5.2, 6.4**
  - [x] 8.4 Write property test for Property 4: Invalidate-then-Find rebuilds correctly
    - **Property 4: Invalidate-then-Find rebuilds correctly**
    - Add entities, call Invalidate, call Find, assert correct entity returned
    - **Validates: Requirements 10.3**
  - [x] 8.5 Write property test for Property 5: Snapshot independence
    - **Property 5: Snapshot independence**
    - Get snapshot, mutate backing list via Add/Remove, assert snapshot unchanged
    - **Validates: Requirements 12.1**
  - [x] 8.6 Write property test for Property 6: Blueprint mutation invalidates derived caches
    - **Property 6: Blueprint mutation invalidates derived caches**
    - Add blueprint, verify CountBlueprintsByType reflects it
    - **Validates: Requirements 13.1**
  - [x] 8.7 Write property test for Property 7: BuildPlan mutation invalidates build item indexes
    - **Property 7: BuildPlan mutation invalidates build item indexes**
    - Add build plan with items, verify GetBuildItemsByBlueprint returns them
    - **Validates: Requirements 13.2**

- [x] 9. Migrate callers — PlayerContext entities (forms, parsers, services)
  - [x] 9.1 Migrate Blueprint callers
    - Find all `.BlueprintList.Add(` and `.BlueprintList.Remove(` outside PlayerContext.cs
    - Replace with `AddBlueprint()` / `RemoveBlueprint()` calls
    - Covers: BlueprintScanner, forms, importers, MainWindow
    - _Requirements: 9.1, 9.2, 9.3, 9.4_
  - [x] 9.2 Migrate Survey callers
    - Replace `.SurveyList.Add(` / `.SurveyList.Remove(` with `AddSurvey()` / `RemoveSurvey()`
    - Covers: SurveyParser, survey forms
    - _Requirements: 9.1, 9.2, 9.3, 9.4_
  - [x] 9.3 Migrate Colony callers
    - Replace `.ColonyList.Add(` / `.ColonyList.Remove(` with `AddColony()` / `RemoveColony()`
    - Covers: ColonyParser, colony forms, background processor
    - _Requirements: 9.1, 9.2, 9.3, 9.4_
  - [x] 9.4 Migrate PlayerProfile callers
    - Replace `.PlayerProfileList.Add(` / `.PlayerProfileList.Remove(` with `AddPlayerProfile()` / `RemovePlayerProfile()`
    - _Requirements: 9.1, 9.2, 9.3, 9.4_
  - [x] 9.5 Migrate DeliveryRoute, DeliveryPlan, PricingPlan callers
    - Replace direct list mutations with `AddDeliveryRoute()` / `RemoveDeliveryRoute()` etc.
    - _Requirements: 9.1, 9.2, 9.3, 9.4_
  - [x] 9.6 Migrate BuildPlan, ShipTemplate, Ship callers
    - Replace direct list mutations with corresponding Add/Remove methods
    - _Requirements: 9.1, 9.2, 9.3, 9.4_
  - [x] 9.7 Migrate Station, Asteroid, Faction, ExternalCharacter callers
    - Replace direct list mutations with corresponding Add/Remove methods
    - _Requirements: 9.1, 9.2, 9.3, 9.4_
  - [x] 9.8 Migrate MarketListing, MarketTransaction, StockPlan, StockProfile, SupplyChain, WarehouseOverflowRule callers
    - Replace direct list mutations with corresponding Add/Remove methods
    - _Requirements: 9.1, 9.2, 9.3, 9.4_

- [x] 10. Migrate callers — EmpireContext entities
  - [x] 10.1 Migrate GlobalBlueprintList callers
    - Replace `.GlobalBlueprintList.Add(` / `.GlobalBlueprintList.Remove(` with `AddGlobalBlueprint()` / `RemoveGlobalBlueprint()`
    - _Requirements: 9.1, 9.2, 9.3, 9.4_
  - [x] 10.2 Migrate CommodityList callers
    - Replace `.CommodityList.Add(` with `AddCommodity()`
    - _Requirements: 9.1, 9.2, 9.3, 9.4_
  - [x] 10.3 Migrate BlueprintTypeList, ShipClassList, TechLevelList, EvolutionList, ResourceList, ResourceGroupList, ResourcePurityList callers
    - Replace direct list mutations with corresponding Add/Remove methods
    - _Requirements: 9.1, 9.2, 9.3, 9.4_

- [x] 11. Checkpoint — Verify all production code compiles after migration
  - Ensure all tests pass, ask the user if questions arise.
  - Build the solution — all `.Add()`/`.Remove()` calls on `IReadOnlyList<T>` properties should now be gone
  - Run `getDiagnostics` on any files that had compile errors

- [ ] 12. Migrate callers — test project
  - [x] 12.1 Migrate test setup code that adds entities to PlayerContext lists
    - Find all `.Add(` calls on PlayerContext list properties in `OE2EmpireTracker.Tests/`
    - Replace with corresponding `Add{Entity}()` calls
    - _Requirements: 11.1, 11.2, 11.3, 11.4_
  - [x] 12.2 Migrate test setup code that adds entities to EmpireContext lists
    - Find all `.Add(` calls on EmpireContext list properties in `OE2EmpireTracker.Tests/`
    - Replace with corresponding `Add{Entity}()` calls
    - _Requirements: 11.1, 11.2, 11.3, 11.4_
  - [x] 12.3 Migrate test cleanup code that removes entities from context lists
    - Find all `.Remove(` calls on context list properties in `OE2EmpireTracker.Tests/`
    - Replace with corresponding `Remove{Entity}()` calls
    - _Requirements: 11.1, 11.2, 11.3, 11.4_

- [ ] 13. Final verification and audit
  - [~] 13.1 Grep check — verify no remaining direct list mutations outside context classes
    - Search for `\.{ListName}\.Add\(` and `\.{ListName}\.Remove\(` across all `.cs` files
    - Only hits should be inside PlayerContext.cs and EmpireContext.cs
    - _Requirements: 9.1, 9.2, 11.1, 11.2_
  - [~] 13.2 Full build and test suite
    - Build the solution with MSBuild
    - Run all tests via vstest.console
    - Verify zero compile errors and all tests pass
    - _Requirements: 1.2, 2.2, 8.3_
  - [~] 13.3 Write unit tests for edge cases
    - Null UUID handling (add entity with null UUID, verify Find returns null)
    - Remove non-existent item (verify no exception, list unchanged)
    - BindingSource count reflects mutations (add/remove, verify count)
    - _Requirements: 3.5, 3.6, 7.4_

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- The compiler enforces the encapsulation after task 4.5 and 6.1 — any remaining `.Add()`/`.Remove()` calls on `IReadOnlyList<T>` will fail to compile, making the migration self-verifying
- Tasks 9-12 (caller migration) are the bulk of the work and are broken by entity type for manageability
