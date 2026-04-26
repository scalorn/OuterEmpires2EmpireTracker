# Implementation Plan: Data Model Ordering Invariant

## Overview

Establish the architectural invariant that data model collections are unordered bags by introducing a centralized `CollectionSortHelper` static class, refactoring all ~28 inline sort call sites to use it, adding property-based and unit tests, creating a steering file for AI compliance, and adding an automated audit check to prevent regressions.

All code is C# targeting .NET Framework 4.8.1. Property-based tests use FsCheck + NUnit. The `CollectionSortHelper` is the only new code artifact — a pure static utility in `OE2EmpireTracker/Services/`.

## Tasks

- [x] 1. Create CollectionSortHelper class with all sort methods
  - [x] 1.1 Create `OE2EmpireTracker/Services/CollectionSortHelper.cs` with the full static class
    - Implement all primary sort methods: `OrderStructures`, `OrderRouteStops`, `OrderPlanStops`, `OrderSupplyChainStages`, `OrderColonies`, `OrderBlueprints`, `OrderPlayerProfiles`, `OrderSurveys`, `OrderCommodityRequests`, `OrderCommodityRequestsByNeedBy`, `OrderDeliveryItems`, `OrderBuildItems`, `OrderAsteroidReserves`, `OrderComponents`
    - Implement all top-level entity sort methods: `OrderDeliveryRoutes`, `OrderDeliveryPlans`, `OrderPricingPlans`, `OrderBuildPlans`, `OrderShipTemplates`, `OrderShips`, `OrderStations`, `OrderMarketListings`, `OrderMarketTransactions`, `OrderStockPlans`, `OrderStockProfiles`, `OrderSupplyChains`, `OrderWarehouseOverflowRules`, `OrderFactions`, `OrderExternalCharacters`, `OrderAsteroids`
    - Implement alternate sort methods: `OrderMarketTransactionsByTimestamp`, `OrderBlueprintsByEvolutionDescending`, `OrderBlueprintsByEvolution`, `OrderStructuresDescending`, `OrderActivityRowsByTimeRemaining`, `OrderCountdownsByTimeRemaining`
    - Each method accepts `IEnumerable<T>`, returns `IReadOnlyList<T>`, handles null input with `Array.Empty<T>()`
    - Use `StringComparer.OrdinalIgnoreCase` for string sort keys, null-coalesce to `string.Empty`
    - Follow the implementation pattern from the design document
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_

- [x] 2. Write tests for CollectionSortHelper
  - [x] 2.1 Add FsCheck NuGet packages to `OE2EmpireTracker.Tests`
    - Add FsCheck 2.16.6 and FsCheck.NUnit to `packages.config`
    - Run `nuget restore` to install
    - _Requirements: 8.2_

  - [x] 2.2 Write property test: OrderStructures always sorted by BuildQueueSequence
    - Create `OE2EmpireTracker.Tests/Services/CollectionSortHelperPropertyTests.cs`
    - **Property 1: Sort helper produces correctly ordered output (structures)**
    - Generate random `List<ColonyStructure>` with random `BuildQueueSequence` values
    - Assert each element's `BuildQueueSequence` <= next element's `BuildQueueSequence`
    - Use `[Property(MaxTest = 100)]` attribute
    - **Validates: Requirements 3.1, 8.2, 8.5**

  - [x] 2.3 Write property test: OrderRouteStops always sorted by Sequence
    - **Property 1: Sort helper produces correctly ordered output (route stops)**
    - Generate random `List<RouteStop>` with random `Sequence` values
    - Assert each element's `Sequence` <= next element's `Sequence`
    - **Validates: Requirements 3.1, 8.2, 8.5**

  - [x] 2.4 Write property test: OrderColonies always sorted by composite key
    - **Property 1: Sort helper produces correctly ordered output (colonies)**
    - Generate random `List<Colony>` with random `SystemName`/`PlanetName`/`ColonyName`
    - Assert composite key ordering is correct
    - **Validates: Requirements 3.1, 8.2, 8.5**

  - [x] 2.5 Write property test: OrderComponents always sorted by SlotType then SlotIndex
    - **Property 1: Sort helper produces correctly ordered output (components)**
    - Generate random `List<ShipComponentSlot>` with random `SlotType`/`SlotIndex`
    - Assert `SlotType` ordering, then `SlotIndex` within same `SlotType`
    - **Validates: Requirements 3.1, 8.2, 8.5**

  - [x] 2.6 Write property test: SortHelper and SerializationSorter agree on RouteStop order
    - **Property 2: Sort helper and SerializationSorter agree on relative order for shared keys**
    - Generate random `RouteStop[]`, sort with both `CollectionSortHelper` and `SerializationSorter`
    - Assert identical relative order
    - **Validates: Requirements 3.3**

  - [x] 2.7 Write property test: SortHelper and SerializationSorter agree on Component order
    - **Property 2: Sort helper and SerializationSorter agree on relative order for shared keys**
    - Generate random `ShipComponentSlot[]`, sort with both sorters
    - Assert identical relative order
    - **Validates: Requirements 3.3**

  - [x] 2.8 Write property test: GetFirstStagedStructure returns lowest-sequence staged structure
    - **Property 3: GetFirstStagedStructure returns the lowest-sequence staged structure**
    - Generate random colony with shuffled structures, at least one staged
    - Assert returned structure has the lowest `BuildQueueSequence` among staged structures
    - **Validates: Requirements 4.5**

  - [x] 2.9 Write property test: CalculateLoadList is order-independent
    - **Property 4: CalculateLoadList is order-independent**
    - Generate random `DeliveryPlan` with shuffled stops
    - Assert load list result is identical regardless of stop permutation
    - **Validates: Requirements 4.6**

  - [x] 2.10 Write unit tests for CollectionSortHelper edge cases
    - Create `OE2EmpireTracker.Tests/Services/CollectionSortHelperTests.cs`
    - Test null input returns empty list
    - Test empty input returns empty list
    - Test single-element input returns single-element list
    - Test already-sorted input returns same order
    - Test reverse-sorted input returns correct order
    - Test null sort key values sort to beginning (empty string)
    - Smoke test that each sort helper method exists and compiles
    - _Requirements: 8.2, 8.3_

- [x] 3. Checkpoint - Verify CollectionSortHelper builds and tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Refactor colony structure consumers
  - [x] 4.1 Refactor `ColonyViewModel.StructureViewModels` to use `CollectionSortHelper.OrderStructures()`
    - Replace `.OrderBy(s => s.BuildQueueSequence)` with `CollectionSortHelper.OrderStructures(...)`
    - _Requirements: 4.1, 8.4_

  - [x] 4.2 Refactor `ColonyStatusCalculator.CalculateBuilt()` and `CalculateIdeal()` to use `CollectionSortHelper.OrderStructures()`
    - Replace both `.OrderBy(s => s.BuildQueueSequence)` calls
    - _Requirements: 4.2, 4.3, 8.4_

  - [x] 4.3 Refactor `BuildOrderOptimizer.Optimize()` to use `CollectionSortHelper.OrderStructures()`
    - Replace `.OrderBy(s => s.BuildQueueSequence)` call
    - _Requirements: 4.4, 8.4_

  - [x] 4.4 Refactor `ColonyBuildEligibility.GetFirstStagedStructure()` to use `CollectionSortHelper.OrderStructures()`
    - Replace `.OrderBy(s => s.BuildQueueSequence)` call
    - _Requirements: 4.5, 8.4_

  - [x] 4.5 Refactor `FormColonyV2.PopulateStructures()` to use `CollectionSortHelper.OrderStructures()`
    - Replace `.Sort(BuildQueueSequence)` call
    - _Requirements: 4.1, 8.4_

  - [x] 4.6 Refactor `ColonyInactivityCollector` to use `CollectionSortHelper.OrderStructures()` and `OrderStructuresDescending()`
    - Replace `.OrderBy(r => r.DisplaySequence)` with `CollectionSortHelper.OrderStructures(...)`
    - Replace `.OrderByDescending(r => r.DisplaySequence)` with `CollectionSortHelper.OrderStructuresDescending(...)`
    - _Requirements: 8.4_

- [x] 5. Refactor route stop consumers
  - [x] 5.1 Refactor `DeliveryPlanViewModel` (4 methods) to use `CollectionSortHelper.OrderRouteStops()`
    - Replace all `routeStops.OrderBy(s => s.Sequence)` calls
    - _Requirements: 4.7, 8.4_

  - [x] 5.2 Refactor `DeliveryGenerationService` (2 route stop methods) to use `CollectionSortHelper.OrderRouteStops()`
    - Replace `route.Stops.OrderBy(s => s.Sequence)` calls
    - _Requirements: 4.8, 8.4_

  - [x] 5.3 Refactor `FormColonyDailyBuild.BuildContent()` to use `CollectionSortHelper.OrderRouteStops()`
    - Replace `route.Stops.OrderBy(s => s.Sequence)` call
    - _Requirements: 4.11, 8.4_

- [x] 6. Refactor plan stop and supply chain consumers
  - [x] 6.1 Refactor `DeliveryPlan.CalculateLoadList()` to use `CollectionSortHelper.OrderPlanStops()`
    - Replace `Stops.OrderBy(s => s.Sequence)` call
    - _Requirements: 4.6, 8.4_

  - [x] 6.2 Refactor `FormDeliveryExecution` (2 locations) to use `CollectionSortHelper.OrderPlanStops()`
    - Replace `.Stops.OrderBy(s => s.Sequence)` calls
    - _Requirements: 4.10, 8.4_

  - [x] 6.3 Refactor `FormSupplyChain` (4 stage locations + 1 chain sort) to use `CollectionSortHelper`
    - Replace `.Stages.OrderBy(s => s.Sequence)` calls with `CollectionSortHelper.OrderSupplyChainStages(...)`
    - Replace `chains.OrderBy(c => c.Name)` with `CollectionSortHelper.OrderSupplyChains(...)`
    - _Requirements: 4.9, 8.4_

- [x] 7. Refactor PlayerContext init methods
  - [x] 7.1 Refactor `PlayerContext` init methods to use `CollectionSortHelper`
    - `InitPlayerProfiles()` → `CollectionSortHelper.OrderPlayerProfiles(...)`
    - `InitBlueprints()` → `CollectionSortHelper.OrderBlueprints(...)`
    - `InitSurveys()` → `CollectionSortHelper.OrderSurveys(...)`
    - `InitColonies()` → `CollectionSortHelper.OrderColonies(...)`
    - `InitDeliveryRoutes()` → `CollectionSortHelper.OrderDeliveryRoutes(...)`
    - `InitDeliveryPlans()` → `CollectionSortHelper.OrderDeliveryPlans(...)`
    - `InitPricingPlans()` → `CollectionSortHelper.OrderPricingPlans(...)`
    - _Requirements: 8.4_

- [x] 8. Refactor EmpireContext init methods
  - [x] 8.1 Refactor `EmpireContext` init methods to use `CollectionSortHelper`
    - `InitBlueprintTypes()`, `InitTechLevels()`, `InitResources()`, `InitResourceGroups()`, `InitResourcePurities()` → generic `OrderByName(...)` or appropriate helper
    - `InitGlobalBlueprints()` → `CollectionSortHelper.OrderBlueprints(...)`
    - _Requirements: 8.4_

- [x] 9. Refactor admin report, other services, and form/viewmodel sorts
  - [x] 9.1 Refactor `ColonyAdminReportBuilder` activity row sorts to use `CollectionSortHelper.OrderActivityRowsByTimeRemaining()`
    - Replace building and non-repeating `rows.OrderBy(r => r.GetSecondsRemaining())` calls
    - Mining/refining group sorts are exempt (local tuple sorts)
    - _Requirements: 8.4_

  - [x] 9.2 Refactor `ColonyBootstrap` to use `CollectionSortHelper.OrderAsteroidReserves()`
    - Replace 2x `bestResources.OrderBy(r => r.ResourceName)` calls
    - _Requirements: 8.4_

  - [x] 9.3 Refactor `DeliveryGenerationService` flatpack sort to use `CollectionSortHelper.OrderBuildItems()`
    - Replace `flatpacks.OrderBy(f => f.ItemName)` call
    - _Requirements: 8.4_

  - [x] 9.4 Refactor `EvolutionChainService` to use `CollectionSortHelper.OrderBlueprintsByEvolution()`
    - Replace 2x `.Sort(Evolution)` calls
    - _Requirements: 8.4_

  - [x] 9.5 Refactor `PlayerContext.ActiveCountdowns` to use `CollectionSortHelper.OrderCountdownsByTimeRemaining()`
    - Replace `.OrderBy(c => c.TimeRemaining)` call
    - _Requirements: 8.4_

  - [x] 9.6 Refactor `BlueprintViewModel` sorts to use `CollectionSortHelper`
    - `GetEvolutionChain()` → `CollectionSortHelper.OrderBlueprintsByEvolutionDescending(...)`
    - `GetAllOfType()` → `CollectionSortHelper.OrderBlueprints(...)`
    - _Requirements: 8.4_

  - [x] 9.7 Refactor `FormMarket` transaction sort to use `CollectionSortHelper.OrderMarketTransactionsByTimestamp()`
    - Replace `transactions.OrderByDescending(t => t.Timestamp)` call
    - _Requirements: 8.4_

  - [x] 9.8 Refactor `FormColonyV2` blueprint sorts to use `CollectionSortHelper.OrderBlueprints()`
    - Replace 2x `filteredList.Sort(ExtendedName)` calls
    - _Requirements: 8.4_

- [x] 10. Checkpoint - Verify all consumer refactoring builds and tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 11. Create steering file for AI compliance
  - [x] 11.1 Create `.kiro/steering/data-model-ordering.md`
    - Document the unordered-bag invariant statement
    - Document the three approved consumption patterns with examples
    - Include the complete Sort Key Registry table from Requirements 3.1
    - Instruct that consumers must use `CollectionSortHelper` methods (never inline `.OrderBy()`)
    - Instruct that the data model must not be modified to enforce ordering
    - Include instructions for adding new collections to the registry
    - Set inclusion to `auto` (always loaded into AI context)
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 8.8_

- [x] 12. Create inline sort audit check
  - [x] 12.1 Create `.kiro/tools/inline-sort-check.js`
    - Scan all `.cs` files in `OE2EmpireTracker/` and `OE2EmpireTracker.Tests/` (excluding `bin/`, `obj/`, `Designer.cs`)
    - Skip `CollectionSortHelper.cs` and `SerializationSorter.cs`
    - Match lines containing `.OrderBy(`, `.OrderByDescending(`, `.ThenBy(`, `.ThenByDescending(`, `.Sort(` on known model types
    - Report findings with file, line number, and matched expression
    - Exit code 0 = clean, exit code 1 = findings
    - _Requirements: 9.1, 9.2_

  - [x] 12.2 Integrate `inline-sort-check.js` into `audit.js` as check #12 "Inline Sort"
    - Add the new check to the audit runner
    - _Requirements: 9.3_

- [-] 13. Run audit to verify zero inline sort findings
  - Run `node .kiro/tools/inline-sort-check.js` and verify exit code 0
  - Run `node .kiro/tools/audit.js` and verify no new findings from the inline sort check
  - _Requirements: 9.4_

- [~] 14. Final checkpoint - Ensure all tests pass and audit is clean
  - Ensure all tests pass, ask the user if questions arise.
  - Build the solution, run all tests, run full audit
  - Verify zero inline sort findings outside allowed files

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- Exempt sorts (local tuples, UI indices, infrastructure) are documented in the design and intentionally not refactored
- The `EmpireContext` init methods may need a generic `OrderByName<T>()` helper if the types don't have dedicated sort methods — this is a design detail to resolve during implementation
