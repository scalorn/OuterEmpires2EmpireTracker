# Implementation Plan: Crate Content Import

## Overview

Implement a new `CrateContentImporter` service that parses game API crate detail responses and populates the local `Item.Contents` bag with all item types found inside crates. The service integrates into the existing `QueueSyncService` work-item pipeline alongside the existing `CrateImporter`. Blueprints are dual-tracked: they appear in crate contents and in the master blueprint list.

## Tasks

- [ ] 1. Create CrateContentImportResult model and CrateContentImporter skeleton
  - [x] 1.1 Create CrateContentImportResult class and CrateContentImporter skeleton
    - Create `OE2EmpireTracker.Common/Services/CrateContentImporter.cs`
    - Define `CrateContentImportResult` class with TotalItems, Imported, Failed, BlueprintsLinked, NestedCrateIds, CountsByType, Errors, Success properties
    - Define `CrateContentImporter` class with constructor accepting PlayerContext, EmpireContext, AssetMergeService, BlueprintLinkageService
    - Add NLog Logger field
    - Add `Import` method signature returning `CrateContentImportResult` (empty body returning default result)
    - _Satisfies: Req 2 AC1 (parse each cargo item), Req 9 AC1 (log start with GameItemId and count)_
    - _Inputs: design.md (component interfaces section)_
    - _Output: OE2EmpireTracker.Common/Services/CrateContentImporter.cs_
    - _Verification: Solution compiles with zero warnings_


- [ ] 2. Implement core Import parsing logic
  - [x] 2.1 Implement JSON deserialization and cargo item iteration loop
    - Parse raw JSON into `GameApiAssetDetailResponse` using Newtonsoft.Json
    - Implement try-catch around deserialization for malformed JSON (return empty result with Success=false)
    - Iterate over `response.Cargo` items with per-item try-catch
    - Log informational message at start with crate GameItemId and cargo count
    - Log debug-level TypeC-to-ItemType mapping for each item
    - _Satisfies: Req 2 AC1 (parse each cargo item), Req 2 AC4 (log mapping at debug), Req 9 AC1 (log start), Req 9 AC4 (malformed JSON no exception)_
    - _Inputs: CrateContentImporter.cs, AssetMergeService.cs_
    - _Output: OE2EmpireTracker.Common/Services/CrateContentImporter.cs_
    - _Verification: Solution compiles; unit test CrateContentImporter_MalformedJson_NoException passes_

  - [x] 2.2 Implement item creation with full field preservation using AssetMergeService
    - Call `AssetMergeService.MapAssetTypeC(typeC)` for type resolution
    - Call `AssetMergeService.CreateAssetItem(apiItem, mappedType)` for Item creation
    - Handle null mapping result: assign ItemType.None, log warning with unrecognized code
    - Preserve all API fields: GameItemId, Name, Quantity, ResourcePurity, Volume, Mass, CurrentHP, MaxHP, MaxRepairPercent, HealthPercentage, LastRepairHealthPercentage, Evolution, ShipPartType, JobRef, JobDeliveryLoc, JobName, JobTrack, ItemProperties
    - Accept ShipPart items with missing damage fields (use defaults)
    - _Satisfies: Req 2 AC2 (MapAssetTypeC mapping), Req 2 AC3 (preserve all fields), Req 2 AC5 (unknown TypeC → None), Req 2 AC6 (use returned ItemType), Req 8 AC1-AC5 (all item types)_
    - _Inputs: CrateContentImporter.cs, AssetMergeService.cs_
    - _Output: OE2EmpireTracker.Common/Services/CrateContentImporter.cs_
    - _Verification: Unit test CrateContentImporter_AllItemTypes_Mapped passes_


  - [-] 2.3 Implement Contents Bag population and crate Item lookup
    - Locate target crate Item in parentBag by matching GameItemId
    - If no matching crate Item exists, create a new Item with type Crate and the GameItemId
    - Replace crate Item's Contents property with a new ItemBag containing all parsed items (clear previous contents)
    - If replacement fails after successful parsing, log error and leave Contents unchanged
    - _Satisfies: Req 3 AC1 (replace Contents with ItemBag), Req 3 AC2 (identify by GameItemId), Req 3 AC3 (create new if missing), Req 3 AC4 (clear previous contents), Req 3 AC5 (failure leaves unchanged)_
    - _Inputs: CrateContentImporter.cs, ItemBag model_
    - _Output: OE2EmpireTracker.Common/Services/CrateContentImporter.cs_
    - _Verification: Unit test CrateContentImporter_ContentsBagReplaced passes_

- [ ] 3. Implement blueprint dual-tracking and nested crate support
  - [x] 3.1 Implement blueprint dual-tracking via BlueprintLinkageService
    - For cargo items with TypeC indicating Blueprint, call `BlueprintLinkageService.ProcessItem(apiItem, localItem, ownerUUID)`
    - Set BaseItemTypeID on the content Item to the matched/created blueprint's UUID
    - Track BlueprintsLinked count in the result
    - After all items processed, fire BlueprintDataChanged event if any blueprints were linked
    - _Satisfies: Req 4 AC1 (add to Contents AND upsert master list), Req 4 AC2 (use existing dedup logic), Req 4 AC3 (set BaseItemTypeID), Req 4 AC4 (fire BlueprintDataChanged)_
    - _Inputs: CrateContentImporter.cs, BlueprintLinkageService.cs_
    - _Output: OE2EmpireTracker.Common/Services/CrateContentImporter.cs_
    - _Verification: Unit test CrateContentImporter_Blueprint_DualTracked passes_


  - [~] 3.2 Implement nested crate detection and cycle prevention
    - When a cargo item has TypeC equal to Crate, create an Item of type Crate in Contents
    - Add the nested crate's GameItemId to NestedCrateIds in the result (for cascade work items)
    - Maintain visitedCrateIds HashSet parameter — skip any crate whose GameItemId is already visited
    - Log warning when cycle is detected with the repeated GameItemId
    - If nested crate processing fails, log warning, skip, continue with remaining items
    - _Satisfies: Req 5 AC1 (create crate Item in Contents), Req 5 AC2 (return nested IDs for cascading), Req 5 AC3 (no depth limit), Req 5 AC4 (cycle detection via visited set), Req 5 AC5 (skip on failure)_
    - _Inputs: CrateContentImporter.cs_
    - _Output: OE2EmpireTracker.Common/Services/CrateContentImporter.cs_
    - _Verification: Unit test CrateContentImporter_NestedCrate_CycleDetected passes_

- [ ] 4. Implement persistence and completion logging
  - [~] 4.1 Add WriteContext persistence with retry on failure
    - After successful Contents Bag population, call `PlayerContext.WriteContext()` to persist
    - If WriteContext fails, log error and retain in-memory state (retry on next sync)
    - _Satisfies: Req 6 AC1 (WriteContext after population), Req 6 AC4 (retry on failure)_
    - _Inputs: CrateContentImporter.cs, PlayerContext.cs_
    - _Output: OE2EmpireTracker.Common/Services/CrateContentImporter.cs_
    - _Verification: Unit test CrateContentImporter_WriteContextFailure_RetainsInMemory passes_


  - [~] 4.2 Add completion summary logging with empty response and error resilience
    - Log completion summary with counts by ItemType and any errors encountered
    - Populate CountsByType dictionary in result for per-type breakdown
    - Handle empty responses: do not produce item-level error log entries
    - If error logging itself fails for an individual item, continue processing without interruption
    - _Satisfies: Req 9 AC2 (log summary), Req 9 AC5 (empty response no item errors), Req 9 AC6 (logging failure doesn't halt)_
    - _Inputs: CrateContentImporter.cs_
    - _Output: OE2EmpireTracker.Common/Services/CrateContentImporter.cs_
    - _Verification: Unit test CrateContentImporter_EmptyResponse_ReturnsEmptyBag passes_

- [~] 5. Checkpoint - Verify CrateContentImporter compiles and core logic is testable
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 6. Integrate CrateContentImporter into QueueSyncService
  - [~] 6.1 Invoke CrateContentImporter after API fetch, pass GameItemId, return cascade work items
    - Instantiate CrateContentImporter (or use injected instance) in QueueSyncService
    - After successful API fetch, call `CrateContentImporter.Import(json, crateGameItemId, parentBag, ownerUUID, visitedCrateIds)`
    - Pass crate GameItemId to the importer for target Item location
    - Return cascade work items for nested crates from `CrateContentImportResult.NestedCrateIds`
    - Log warning on API error response and continue processing remaining work items
    - _Satisfies: Req 7 AC1 (invoke CrateContentImporter), Req 7 AC6 (pass GameItemId), Req 1 AC1 (create detail work item), Req 1 AC2 (always fetch), Req 1 AC3 (log warning on error)_
    - _Inputs: QueueSyncService.cs, CrateContentImporter.cs_
    - _Output: OE2EmpireTracker.Common/Services/QueueSyncService.cs_
    - _Verification: Solution compiles; integration flow produces nested cascade work items_


  - [~] 6.2 Coordinate CrateImporter blueprint extraction alongside content import
    - Continue calling existing `CrateImporter.ImportFromJson()` for blueprint extraction only if response contains blueprint entries
    - If crate detail response contains no blueprint entries, skip blueprint extraction and invoke only the CrateContentImporter
    - If content population succeeds but blueprint extraction fails, retain content and log error
    - If content population fails but blueprint extraction succeeds, retain blueprint result and log error
    - _Satisfies: Req 7 AC2 (continue invoking CrateImporter), Req 7 AC3 (both execute without conflict), Req 7 AC4 (skip CrateImporter if no blueprints), Req 7 AC5 (independent success/failure)_
    - _Inputs: QueueSyncService.cs, CrateImporter.cs, CrateContentImporter.cs_
    - _Output: OE2EmpireTracker.Common/Services/QueueSyncService.cs_
    - _Verification: Solution compiles; unit test QueueSyncService_CrateDetail_SkipsBlueprintExtractionWhenNone passes_

- [ ] 7. Write unit tests for CrateContentImporter
  - [~] 7.1 Write unit tests for JSON parsing and type mapping
    - Create `OE2EmpireTracker.Tests/Services/CrateContentImporterTests.cs`
    - Test: `CrateContentImporter_MalformedJson_NoException` — malformed JSON returns empty result, Success=false
    - Test: `CrateContentImporter_EmptyResponse_ReturnsEmptyBag` — empty cargo array produces empty Contents
    - Test: `CrateContentImporter_AllItemTypes_Mapped` — one cargo item per supported TypeC code, all mapped correctly
    - Test: `CrateContentImporter_UnknownTypeC_AssignsNone` — unknown TypeC returns ItemType.None
    - Test: `CrateContentImporter_MissingDamageFields_Accepted` — ShipPart with missing damage fields is accepted
    - _Satisfies: Req 2 AC1-AC6, Req 8 AC1-AC5, Req 9 AC4-AC5_
    - _Inputs: CrateContentImporter.cs_
    - _Output: OE2EmpireTracker.Tests/Services/CrateContentImporterTests.cs_
    - _Verification: All 5 tests pass via vstest.console_


  - [~] 7.2 Write unit tests for Contents Bag population and blueprint dual-tracking
    - Test: `CrateContentImporter_ContentsBagReplaced` — Contents bag fully replaced (no merge with old contents)
    - Test: `CrateContentImporter_Blueprint_DualTracked` — blueprint items appear in Contents AND master list, BaseItemTypeID set
    - Test: `CrateContentImporter_NestedCrate_CycleDetected` — repeated crate GameItemId is skipped with warning
    - _Satisfies: Req 3 AC1-AC4, Req 4 AC1-AC3, Req 5 AC4_
    - _Inputs: CrateContentImporter.cs_
    - _Output: OE2EmpireTracker.Tests/Services/CrateContentImporterTests.cs_
    - _Verification: All 3 tests pass via vstest.console_


- [ ] 8. Write property-based tests for correctness properties
  - [~] 8.1 Write property test for round-trip equivalence (Property 1)
    - **Property 1: Round-Trip Equivalence**
    - **Validates: Requirements 10.1, 10.2**
    - Create FsCheck generator `ArbitraryCrateResponse` producing valid GameApiAssetDetailResponse JSON
    - Parse → Serialize → Deserialize cycle produces equivalent Item objects
    - Cover all supported item types including Resources with purity, ShipParts with damage fields, Items with ItemProperties
    - _Inputs: CrateContentImporter.cs_
    - _Output: OE2EmpireTracker.Tests/Services/CrateContentImporterPropertyTests.cs_
    - _Verification: Property test passes (100 iterations)_

  - [~] 8.2 Write property test for contents bag completeness (Property 2)
    - **Property 2: Contents Bag Completeness**
    - **Validates: Requirements 3.1, 3.4**
    - Generate random valid crate responses with 0-50 items
    - After import: |Contents.Items| == result.Imported; result.Imported + result.Failed == response.Cargo.Count
    - No items duplicated or lost
    - _Inputs: CrateContentImporter.cs_
    - _Output: OE2EmpireTracker.Tests/Services/CrateContentImporterPropertyTests.cs_
    - _Verification: Property test passes (100 iterations)_

  - [~] 8.3 Write property test for type mapping determinism (Property 3)
    - **Property 3: Type Mapping Determinism**
    - **Validates: Requirements 2.2, 2.6**
    - For any TypeC code, MapAssetTypeC returns the same result on repeated calls
    - Mapping is a pure function with no side effects or state dependencies
    - _Inputs: AssetMergeService.cs_
    - _Output: OE2EmpireTracker.Tests/Services/CrateContentImporterPropertyTests.cs_
    - _Verification: Property test passes (100 iterations)_


  - [~] 8.4 Write property test for cycle detection termination (Property 4)
    - **Property 4: Cycle Detection Termination**
    - **Validates: Requirements 5.2, 5.4**
    - Generate arbitrary nested crate graphs (DAGs with optional cycles, depth 1-5)
    - Import always terminates; each unique crate processed at most once
    - _Inputs: CrateContentImporter.cs_
    - _Output: OE2EmpireTracker.Tests/Services/CrateContentImporterPropertyTests.cs_
    - _Verification: Property test passes (100 iterations)_

  - [~] 8.5 Write property test for blueprint dual-presence (Property 5)
    - **Property 5: Blueprint Dual-Presence**
    - **Validates: Requirements 4.1, 4.3**
    - For every blueprint-typed item in Contents after import, a corresponding entry exists in the master blueprint list
    - _Inputs: CrateContentImporter.cs, BlueprintLinkageService.cs_
    - _Output: OE2EmpireTracker.Tests/Services/CrateContentImporterPropertyTests.cs_
    - _Verification: Property test passes (100 iterations)_

- [~] 9. Checkpoint - Verify serialization round-trip (Requirement 10)
  - Ensure all tests pass, ask the user if questions arise.
  - Verify ItemBagJSONConverter handles nested Contents correctly on serialize/deserialize
  - _Satisfies: Req 6 AC2 (serialize Contents as nested JSON), Req 6 AC3 (deserialize on load)_

- [~] 10. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements and acceptance criteria for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The CrateContentImporter reuses existing AssetMergeService and BlueprintLinkageService — no new mapping logic needed
- FsCheck 2.16.6 generators must use LINQ query syntax (no FsCheck.Fluent namespace)
- The existing CrateImporter is NOT modified — it continues to handle blueprint extraction from scraped JSON

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["2.1"] },
    { "id": 2, "tasks": ["2.2"] },
    { "id": 3, "tasks": ["2.3", "3.1"] },
    { "id": 4, "tasks": ["3.2"] },
    { "id": 5, "tasks": ["4.1"] },
    { "id": 6, "tasks": ["4.2"] },
    { "id": 7, "tasks": ["6.1"] },
    { "id": 8, "tasks": ["6.2"] },
    { "id": 9, "tasks": ["7.1", "7.2"] },
    { "id": 10, "tasks": ["8.1", "8.2", "8.3", "8.4", "8.5"] }
  ]
}
```
