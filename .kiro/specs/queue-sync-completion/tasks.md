# Implementation Plan: Queue Sync Completion

## Overview

Complete the QueueSyncService work item stubs to deserialize API responses and delegate to existing merge services. Extract ProfileMergeService from GameApiSyncScheduler. Wire ColonyId-to-UUID context passing via closures for cascaded colony detail items. Fire data-changed events after successful merges. Extract shared CascadeCargoDetailItems helper for consistent detail cascading across all cargo-processing work items.

## Tasks

- [x] 1. Extract ProfileMergeService from GameApiSyncScheduler
  - [x] 1.1 Create ProfileMergeService static class
    - Extract `MergeProfileData`, `MergeRank`, and `MergeSkills` from `GameApiSyncScheduler.cs` into new `OE2EmpireTracker.Common/Services/ProfileMergeService.cs`
    - Pure extraction — no logic changes, just move the methods
    - Update visibility: `MergeProfileData` public, `MergeRank` and `MergeSkills` private
    - Add NLog logger to ProfileMergeService
    - _Satisfies: Req 1, Criteria 1.1, 1.2, 1.3_
    - _Inputs: GameApiSyncScheduler.cs_
    - _Output: OE2EmpireTracker.Common/Services/ProfileMergeService.cs_
    - _Verification: getDiagnostics clean, solution builds_

  - [x] 1.2 Wire GameApiSyncScheduler to delegate to ProfileMergeService
    - Replace the inline `MergeProfileData`/`MergeRank`/`MergeSkills` bodies in GameApiSyncScheduler with calls to `ProfileMergeService.MergeProfileData`
    - Remove the now-redundant private methods from GameApiSyncScheduler
    - _Satisfies: Req 1, Criteria 1.1_
    - _Inputs: GameApiSyncScheduler.cs, ProfileMergeService.cs_
    - _Output: GameApiSyncScheduler.cs (modified)_
    - _Verification: getDiagnostics clean, existing GameApiSyncScheduler tests still pass_

  - [x] 1.3 Write property tests for ProfileMergeService
    - **Property 1: Idempotent Merge** — applying same response twice yields identical state
    - **Validates: Req 1 Criteria 1.2, 1.3, 1.4, 1.5**
    - Test that "API wins" fields overwrite local, local-only fields (TrainingStarted, CompletionTime) are preserved
    - Test return value is true iff at least one field changed
    - _Output: OE2EmpireTracker.Tests/Services/ProfileMergeServiceTests.cs_

- [~] 2. Checkpoint — ProfileMergeService extraction verified
  - Ensure all tests pass, ask the user if questions arise.


- [ ] 3. Complete CharacterProfile work item
  - [x] 3.1 Add deserialization and merge call to CreateCharacterProfileItem
    - Deserialize `result.Json` as `GameApiServiceResponse<GameApiProfileResponse>` (try/catch JsonException)
    - Call `ProfileMergeService.MergeProfileData` with `_playerContext.Data.Profile` and deserialized response
    - If merge returns true, call `_playerContext.WriteContext()` and raise `PlayerProfileDataChanged`
    - Truncate logged JSON to 500 chars on error
    - _Satisfies: Req 2, Criteria 2.1, 2.2, 2.3; Req 12, Criteria 12.1, 12.4_
    - _Inputs: QueueSyncService.cs, ProfileMergeService.cs_
    - _Output: QueueSyncService.cs (modified — CreateCharacterProfileItem method)_
    - _Verification: getDiagnostics clean, solution builds_

- [ ] 4. Complete BankingBalance work item
  - [x] 4.1 Add balance import logic to CreateBankingBalanceItem
    - Call `BankingService.ImportBalanceAsync` with `_apiClient`, `_settings.AppId`, and `_currentAccessToken`
    - If result is non-null, call `_playerContext.SetBankingBalance(result)` and raise `BankingDataChanged`
    - If result is null, log warning and skip
    - Wrap in try/catch for unexpected exceptions (Req 12.2)
    - _Satisfies: Req 3, Criteria 3.1, 3.2, 3.3, 3.4; Req 11, Criteria 11.2_
    - _Inputs: QueueSyncService.cs, BankingService.cs_
    - _Output: QueueSyncService.cs (modified — CreateBankingBalanceItem method)_
    - _Verification: getDiagnostics clean, solution builds_

- [ ] 5. Complete ColonyList work item with context passing
  - [x] 5.1 Add MergeColonyList call and ColonyIdToUUIDMap capture to CreateColonyListItem
    - After deserializing the colony list response, call `ColonyMergeService.MergeColonyList`
    - Capture `ColonyMergeResult.ColonyIdToUUIDMap` in a local variable
    - If merge `HasChanges`, raise `ColonyDataChanged` and call `WriteContext()`
    - Pass the map to cascaded colony detail work items via updated method signatures
    - _Satisfies: Req 4, Criteria 4.1, 4.2, 4.3, 4.4; Req 14, Criteria 14.1, 14.4_
    - _Inputs: QueueSyncService.cs, ColonyMergeService.cs_
    - _Output: QueueSyncService.cs (modified — CreateColonyListItem method)_
    - _Verification: getDiagnostics clean, solution builds_

  - [x] 5.2 Add overloads for colony detail items accepting ColonyIdToUUIDMap
    - Create `CreateColonyBuildingsItem(int colonyId, Dictionary<int, string> colonyIdMap)` overload
    - Create `CreateColonyWarehouseItem(int colonyId, Dictionary<int, string> colonyIdMap)` overload
    - Create `CreateColonyWorkersItem(int colonyId, Dictionary<int, string> colonyIdMap)` overload
    - Each resolves target Colony UUID from map, with fallback to direct ColonyId lookup (Req 14.3)
    - Log warning and skip if colony unresolvable
    - _Satisfies: Req 14, Criteria 14.1, 14.2, 14.3, 14.4; Req 4, Criteria 4.5_
    - _Inputs: QueueSyncService.cs_
    - _Output: QueueSyncService.cs (modified — new overloads)_
    - _Verification: getDiagnostics clean, solution builds_


- [ ] 6. Complete ColonyBuildings work item
  - [x] 6.1 Add deserialization and merge to CreateColonyBuildingsItem (map overload)
    - Deserialize `result.Json` as `GameApiServiceResponse<GameApiColonyBuildingsResponse>`
    - Resolve target Colony from ColonyIdToUUIDMap (with fallback)
    - Call `ColonyMergeService.MergeBuildings` with buildings list and local Colony
    - Log warning and skip if colony unresolvable or Data.Buildings is null
    - Wrap in try/catch for JsonException (Req 12.1) and merge errors (Req 12.2)
    - _Satisfies: Req 5, Criteria 5.1, 5.2, 5.3, 5.4; Req 12, Criteria 12.1, 12.2_
    - _Inputs: QueueSyncService.cs, ColonyMergeService.cs_
    - _Output: QueueSyncService.cs (modified — CreateColonyBuildingsItem overload body)_
    - _Verification: getDiagnostics clean, solution builds_

- [ ] 7. Complete ColonyWarehouse work item
  - [x] 7.1 Add deserialization and merge to CreateColonyWarehouseItem (map overload)
    - Deserialize `result.Json` as `GameApiServiceResponse<GameApiColonyWarehouseResponse>`
    - Resolve target Colony from ColonyIdToUUIDMap (with fallback)
    - Call `ColonyMergeService.MergeWarehouse` with contents, Colony, BlueprintLinkageService, SurveyLinkageService
    - Log warning and skip if colony unresolvable or Data.Contents is null
    - Wrap in try/catch for JsonException and merge errors
    - _Satisfies: Req 6, Criteria 6.1, 6.2, 6.3, 6.4; Req 12, Criteria 12.1, 12.2_
    - _Inputs: QueueSyncService.cs, ColonyMergeService.cs_
    - _Output: QueueSyncService.cs (modified — CreateColonyWarehouseItem overload body)_
    - _Verification: getDiagnostics clean, solution builds_

- [ ] 8. Complete ColonyWorkers work item
  - [x] 8.1 Add deserialization and merge to CreateColonyWorkersItem (map overload)
    - Deserialize `result.Json` as `GameApiServiceResponse<GameApiColonyWorkersResponse>`
    - Resolve target Colony from ColonyIdToUUIDMap (with fallback)
    - Call `ColonyMergeService.MergeWorkers` with response data and local Colony
    - Log warning and skip if colony unresolvable or Data is null
    - Wrap in try/catch for JsonException and merge errors
    - _Satisfies: Req 7, Criteria 7.1, 7.2, 7.3, 7.4; Req 12, Criteria 12.1, 12.2_
    - _Inputs: QueueSyncService.cs, ColonyMergeService.cs_
    - _Output: QueueSyncService.cs (modified — CreateColonyWorkersItem overload body)_
    - _Verification: getDiagnostics clean, solution builds_

- [~] 9. Checkpoint — Colony work items verified
  - Ensure all tests pass, ask the user if questions arise.


- [ ] 10. Extract CascadeCargoDetailItems shared helper method
  - [~] 10.1 Create private CascadeCargoDetailItems method in QueueSyncService
    - Add `private WorkItem[] CascadeCargoDetailItems(List<GameApiAssetCargoItem> cargo, string planetName, string systemName)` method
    - Iterate cargo entries: create `CreateCrateDetailItem` for each Crate entry, `CreateBlueprintDetailItem` for each Blueprint entry if `!IsBlueprintFresh(entry.Id)`, `CreateSurveyDetailItem` for each Survey entry if `!IsSurveyFresh(entry.Id)`
    - Return the collected work items as an array
    - This method encapsulates the shared cascading logic used by both AssetLocationDetail and ShipCargo
    - _Satisfies: Req 15, Criteria 15.1, 15.2, 15.3, 15.4, 15.5_
    - _Inputs: QueueSyncService.cs (existing CreateCrateDetailItem, CreateBlueprintDetailItem, CreateSurveyDetailItem, IsBlueprintFresh, IsSurveyFresh methods)_
    - _Output: QueueSyncService.cs (modified — new private helper method)_
    - _Verification: getDiagnostics clean, solution builds_


- [ ] 11. Complete AssetLocationDetail work item — generic cargo merge and detail cascading
  - [~] 11.1 Add cargo merge dispatch by location type and call CascadeCargoDetailItems
    - After successful deserialization (existing code), add generic cargo merge BEFORE the cascade logic
    - Switch on `typeC`: "Co" → `AssetMergeService.MergeColonyAssets`, "St" → `MergeStationAssets`, "Sh" → `MergeShipAssets`
    - For "Co": resolve Colony by matching `id` to `Colony.ColonyId`
    - For "St": find or create Station by GameLocationId (name-based fallback, then create new)
    - For "Sh": find or create Ship by GameLocationId (name-based fallback, then create new)
    - Log warning for unknown location types, skip merge and return empty
    - AFTER the generic merge, call `CascadeCargoDetailItems(response.Cargo, planetName, systemName)` to cascade detail work items
    - Replace any existing inline foreach loop for Crate/Bp/Survey cascading with the shared helper call
    - _Satisfies: Req 8, Criteria 8.1, 8.2, 8.3, 8.4, 8.7, 8.8; Req 15, Criteria 15.1, 15.5_
    - _Inputs: QueueSyncService.cs, AssetMergeService.cs_
    - _Output: QueueSyncService.cs (modified — CreateAssetLocationDetailItem body)_
    - _Verification: getDiagnostics clean, solution builds_

  - [~] 11.2 Implement station find-or-create helper method
    - Add private `FindOrCreateStation(int gameLocationId, string planetName, string systemName)` method
    - First try match by `Station.GameLocationId`, then name-based fallback, then create new
    - Set `GameLocationId` on newly created stations so future lookups succeed
    - _Satisfies: Req 8, Criteria 8.5_
    - _Inputs: QueueSyncService.cs, PlayerContext (station list access)_
    - _Output: QueueSyncService.cs (modified — new helper method)_
    - _Verification: getDiagnostics clean, solution builds_

  - [~] 11.3 Implement ship find-or-create helper method
    - Add private `FindOrCreateShip(int gameLocationId, string planetName)` method
    - First try match by `Ship.GameLocationId`, then name-based fallback, then create new
    - Set `GameLocationId` on newly created ships so future lookups succeed
    - _Satisfies: Req 8, Criteria 8.6_
    - _Inputs: QueueSyncService.cs, PlayerContext (ship list access)_
    - _Output: QueueSyncService.cs (modified — new helper method)_
    - _Verification: getDiagnostics clean, solution builds_


- [ ] 12. Complete ShipCargo work item
  - [~] 12.1 Add deserialization, merge, and CascadeCargoDetailItems call to CreateShipCargoItem
    - Deserialize `result.Json` as array of `GameApiAssetCargoItem` objects
    - Identify the player's active ship (match current GameLocationId or first in ship list)
    - Call `AssetMergeService.MergeShipAssets` with cargo items and ship
    - AFTER the generic merge, call `CascadeCargoDetailItems(cargoItems, activeShip.PlanetName, activeShip.SystemName)` to cascade detail work items
    - Log warning if no active ship can be identified, skip merge and cascading
    - Wrap in try/catch for JsonException and merge errors
    - Raise `ShipDataChanged` after successful merge
    - _Satisfies: Req 9, Criteria 9.1, 9.2, 9.3, 9.4, 9.5, 9.6; Req 11, Criteria 11.3; Req 15, Criteria 15.1, 15.5_
    - _Inputs: QueueSyncService.cs, AssetMergeService.cs_
    - _Output: QueueSyncService.cs (modified — CreateShipCargoItem body)_
    - _Verification: getDiagnostics clean, solution builds_

- [ ] 13. Complete ShipConfiguration work item
  - [-] 13.1 Add deserialization and component mapping to CreateShipConfigItem
    - Deserialize `result.Json` as `GameApiServiceResponse<GameApiShipConfigurationResponse>`
    - Match or create Ship by `ShipId` / GameLocationId from response
    - Map each `GameApiShipComponent` to local ship component representation (BlueprintType, Name, Evolution, HealthPercentage, LastRepairHealthPercentage)
    - Replace ship's component list with mapped components from response
    - Update ship summary fields (fuel, cargo capacity, etc.) from `response.Summary`
    - Raise `ShipDataChanged` after successful merge
    - Wrap in try/catch for JsonException and merge errors
    - _Satisfies: Req 10, Criteria 10.1, 10.2, 10.3, 10.4, 10.5; Req 13, Criteria 13.1, 13.2, 13.3_
    - _Inputs: QueueSyncService.cs, GameApiShipConfigurationResponse.cs_
    - _Output: QueueSyncService.cs (modified — CreateShipConfigItem body)_
    - _Verification: getDiagnostics clean, solution builds_

- [~] 14. Checkpoint — Ship and asset work items verified
  - Ensure all tests pass, ask the user if questions arise.


- [ ] 15. Wire UI event raising for asset merges
  - [~] 15.1 Add event raising after asset cargo merges in CreateAssetLocationDetailItem
    - Raise `ColonyDataChanged` when location type "Co" merge succeeds
    - Raise `StationDataChanged` when location type "St" merge succeeds
    - Raise `ShipDataChanged` when location type "Sh" merge succeeds
    - Call `WriteContext()` only after successful merge (not on error)
    - Batch: raise event once per work item, not per-item within a merge
    - _Satisfies: Req 11, Criteria 11.3, 11.4; Req 12, Criteria 12.3_
    - _Inputs: QueueSyncService.cs_
    - _Output: QueueSyncService.cs (modified — event raising in CreateAssetLocationDetailItem)_
    - _Verification: getDiagnostics clean, solution builds_

  - [~] 15.2 Add event raising after colony detail merges
    - Raise `ColonyDataChanged` after successful MergeBuildings, MergeWarehouse, MergeWorkers
    - Call `WriteContext()` only after successful merge
    - _Satisfies: Req 11, Criteria 11.1, 11.4; Req 12, Criteria 12.3_
    - _Inputs: QueueSyncService.cs_
    - _Output: QueueSyncService.cs (modified — event raising in colony detail item overloads)_
    - _Verification: getDiagnostics clean, solution builds_

- [ ] 16. Error handling standardization
  - [x] 16.1 Add log-truncation helper and standardize error patterns across all work items
    - Add private helper `TruncateForLog(string json, int maxLength = 500)` to QueueSyncService
    - Ensure all work items use this helper when logging JSON on deserialization errors
    - Verify no WriteContext called after any error path
    - Verify no events raised after any error path
    - _Satisfies: Req 12, Criteria 12.1, 12.2, 12.3, 12.4_
    - _Inputs: QueueSyncService.cs_
    - _Output: QueueSyncService.cs (modified — helper + error path audit)_
    - _Verification: getDiagnostics clean, solution builds_

- [~] 17. Checkpoint — All work item completions verified
  - Ensure all tests pass, ask the user if questions arise.


- [ ] 18. Property-based tests for work item completion
  - [~] 18.1 Write property test: Error Isolation
    - **Property 2: Error Isolation**
    - **Validates: Req 12, Criteria 12.1, 12.2**
    - Verify that injecting a JsonException in one work item does not prevent other work items from completing
    - Verify no WriteContext occurs after failed deserialization
    - _Output: OE2EmpireTracker.Tests/Services/QueueSyncServicePropertyTests.cs (modified)_

  - [~] 18.2 Write property test: No Data Loss on Error
    - **Property 3: No Data Loss on Error**
    - **Validates: Req 12, Criteria 12.3**
    - Verify local data remains in pre-call state when deserialization or merge throws
    - _Output: OE2EmpireTracker.Tests/Services/QueueSyncServicePropertyTests.cs (modified)_

  - [~] 18.3 Write property test: Context Closure Integrity
    - **Property 4: Context Closure Integrity**
    - **Validates: Req 14, Criteria 14.1, 14.4**
    - Verify all cascaded colony detail items receive the exact ColonyIdToUUIDMap from their parent ColonyList sync cycle
    - _Output: OE2EmpireTracker.Tests/Services/QueueSyncServicePropertyTests.cs (modified)_

  - [~] 18.4 Write property test: Event-After-Mutation
    - **Property 5: Event-After-Mutation**
    - **Validates: Req 11, Criteria 11.1, 11.2, 11.3, 11.4**
    - Verify data-changed events only fire after merge completes and WriteContext persists
    - _Output: OE2EmpireTracker.Tests/Services/QueueSyncServicePropertyTests.cs (modified)_

  - [~] 18.5 Write property test: Log Truncation
    - **Property 6: Log Truncation**
    - **Validates: Req 12, Criteria 12.4**
    - Verify logged JSON bodies are always ≤500 characters
    - _Output: OE2EmpireTracker.Tests/Services/QueueSyncServicePropertyTests.cs (modified)_

  - [~] 18.6 Write property test: Fallback Resolution
    - **Property 7: Fallback Resolution**
    - **Validates: Req 14, Criteria 14.2, 14.3**
    - Verify fallback to direct ColonyId lookup succeeds when map entry missing; returns empty (not throw) when unresolvable
    - _Output: OE2EmpireTracker.Tests/Services/QueueSyncServicePropertyTests.cs (modified)_

  - [~] 18.7 Write property test: Station/Ship Find-or-Create
    - **Property 8: Station/Ship Find-or-Create**
    - **Validates: Req 8, Criteria 8.5, 8.6**
    - Verify find-or-create sets GameLocationId on new entities, ensuring future lookups succeed without creation
    - _Output: OE2EmpireTracker.Tests/Services/QueueSyncServicePropertyTests.cs (modified)_

  - [~] 18.8 Write property test: Consistent Cascading
    - **Property 9: Consistent Cascading**
    - **Validates: Req 15, Criteria 15.1, 15.2, 15.3, 15.4, 15.5; Req 8, Criteria 8.7; Req 9, Criteria 9.4**
    - Verify `CascadeCargoDetailItems` produces the same work items regardless of calling context (AssetLocationDetail vs ShipCargo)
    - Verify Blueprint entries with fresh local data are skipped (no detail item cascaded)
    - Verify Survey entries with fresh local data are skipped (no detail item cascaded)
    - Verify Crate entries always produce a detail item regardless of freshness state
    - _Output: OE2EmpireTracker.Tests/Services/QueueSyncServicePropertyTests.cs (modified)_

- [~] 19. Final checkpoint — All tests pass
  - Ensure all tests pass, ask the user if questions arise.


## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- The GameApiShipConfigurationResponse DTO already exists — task 13.1 uses it directly
- ProfileMergeService does NOT exist yet — task 1.1 creates it by extraction from GameApiSyncScheduler
- Colony detail work items (tasks 6, 7, 8) use the new overloads created in task 5.2
- Error handling (task 16) is a cross-cutting concern applied after all work items are individually complete
- All work items follow the pattern: try deserialize → delegate to merge service → WriteContext → raise event → catch errors gracefully
- Task 10 extracts the shared `CascadeCargoDetailItems` helper early so both AssetLocationDetail (task 11) and ShipCargo (task 12) can call it instead of duplicating cascading logic
- `CascadeCargoDetailItems` enforces Req 15: same freshness thresholds, same factory methods, same skip conditions regardless of cargo source

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "16.1"] },
    { "id": 1, "tasks": ["1.2"] },
    { "id": 2, "tasks": ["1.3", "3.1", "4.1"] },
    { "id": 3, "tasks": ["5.1"] },
    { "id": 4, "tasks": ["5.2"] },
    { "id": 5, "tasks": ["6.1", "7.1", "8.1", "13.1"] },
    { "id": 6, "tasks": ["10.1"] },
    { "id": 7, "tasks": ["11.1", "11.2", "11.3"] },
    { "id": 8, "tasks": ["12.1"] },
    { "id": 9, "tasks": ["15.1", "15.2"] },
    { "id": 10, "tasks": ["18.1", "18.2", "18.3", "18.4", "18.5", "18.6", "18.7", "18.8"] }
  ]
}
```
