# Implementation Plan

## Overview

This plan implements the Assets API integration feature, adding response DTOs, an AssetMergeService for mapping and merging cargo items, and a SyncAssetsAsync method on the GameApiSyncScheduler. Tasks are organized in waves: foundational models first, then merge logic, then scheduler integration, and finally tests.

## Task Dependency Graph

```json
{
  "waves": [
    ["task1", "task2"],
    ["task3"],
    ["task4"],
    ["task5", "task6", "task7"],
    ["task8"],
    ["task9"],
    ["task10", "task11", "task12", "task13"],
    ["task14", "task15"]
  ]
}
```

## Tasks

- [ ] 1. Create Asset API Response DTOs
  - [ ] 1.1 Create `GameApiAssetLocationsResponse`, `GameApiAssetLocationEntry`, `GameApiAssetDetailResponse`, `GameApiAssetCargoItem`, and `GameApiAssetItemProperty` DTO classes with `[JsonProperty]` attributes
  - Satisfies: Req 11, Criteria 1-4 ("strongly-typed DTO classes for the asset API responses")
  - Inputs: design.md Data Models section
  - Output: `OE2EmpireTracker.Common/Models/GameApiAssetResponse.cs`
  - Verification: `getDiagnostics` — compiles with zero errors/warnings

- [ ] 2. Add GameLocationId to Station and Ship Models
  - [ ] 2.1 Add `GameLocationId` (int?), `SystemName` (string), and `SystemId` (int?) properties to Station model with `[JsonProperty]` attributes
  - [ ] 2.2 Add `GameLocationId` (int?) property to Ship model with `[JsonProperty]` attribute
  - Satisfies: Req 12, Criteria 2-5 ("match station/ship locations by GameLocationId")
  - Inputs: `OE2EmpireTracker.Common/Models/Station.cs`, `OE2EmpireTracker.Common/Models/Ship.cs`
  - Output: Modified `Station.cs`, modified `Ship.cs`
  - Verification: `getDiagnostics` — compiles with zero errors/warnings; existing tests still pass

- [ ] 3. Implement TypeC Mapping and Resource Purity Extraction
  - [ ] 3.1 Create `AssetMergeService` static class with `MapAssetTypeC(string typeC)` method implementing the full TypeC mapping table (R, C, F, Bp, S, Sc, W, SH, A, Sh, Cr) with whitespace trimming and case-insensitive matching
  - [ ] 3.2 Implement `ExtractResourcePurity(string resourceName)` method returning `(string baseName, string purity)` tuple using regex to detect "High Purity", "Med Purity", "Low Purity" descriptors
  - Satisfies: Req 3, Criteria 1-12 ("TypeC code to ItemType mapping"); Req 10, Criteria 1-4 ("resource purity extraction")
  - Inputs: design.md TypeC Mapping Table, design.md Resource Purity Extraction section
  - Output: `OE2EmpireTracker.Common/Services/AssetMergeService.cs` (partial — mapping + purity methods only)
  - Verification: `getDiagnostics` — compiles with zero errors/warnings

- [ ] 4. Implement ProcessSingleAssetItem and Item Creation/Update
  - [ ] 4.1 Implement `ProcessSingleAssetItem(GameApiAssetCargoItem apiItem, ItemBag targetBag)` that matches by GameItemId and routes to update or create
  - [ ] 4.2 Implement `UpdateExistingAssetItem(Item local, GameApiAssetCargoItem apiItem)` that updates quantity, mass, volume, health, evolution, properties, and job fields while preserving local-only fields (NickName, Description)
  - [ ] 4.3 Implement `CreateAssetItem(GameApiAssetCargoItem apiItem, ItemType.ItemTypeEnum mappedType)` that maps all Cargo_Item fields to a new Item with generated UUID
  - Satisfies: Req 4, Criteria 1-15 ("cargo item to Item model mapping"); Req 5, Criteria 5 ("preserve local-only data")
  - Inputs: design.md Item Matching Strategy section, `OE2EmpireTracker.Common/Models/Item.cs`, `OE2EmpireTracker.Common/Models/ItemBag.cs`
  - Output: Modified `OE2EmpireTracker.Common/Services/AssetMergeService.cs`
  - Verification: `getDiagnostics` — compiles with zero errors/warnings

- [ ] 5. Implement MergeColonyAssets
  - [ ] 5.1 Implement `MergeColonyAssets(List<GameApiAssetCargoItem> apiItems, Colony colony)` that iterates cargo items, calls ProcessSingleAssetItem on Colony.Items, and returns whether changes were made
  - Satisfies: Req 5, Criteria 1-6 ("colony asset merge"); Req 13, Criteria 2-4 ("coexistence with warehouse sync")
  - Inputs: `OE2EmpireTracker.Common/Models/Colony.cs`, design.md Colony merge section
  - Output: Modified `OE2EmpireTracker.Common/Services/AssetMergeService.cs`
  - Verification: `getDiagnostics` — compiles with zero errors/warnings

- [ ] 6. Implement MergeStationAssets
  - [ ] 6.1 Implement `MergeStationAssets(List<GameApiAssetCargoItem> apiItems, Station station, ItemBag targetHold)` that merges cargo items into the station's default hold and returns whether changes were made
  - Satisfies: Req 6, Criteria 1-6 ("station asset merge")
  - Inputs: `OE2EmpireTracker.Common/Models/Station.cs`, design.md Station merge section
  - Output: Modified `OE2EmpireTracker.Common/Services/AssetMergeService.cs`
  - Verification: `getDiagnostics` — compiles with zero errors/warnings

- [ ] 7. Implement MergeShipAssets
  - [ ] 7.1 Implement `MergeShipAssets(List<GameApiAssetCargoItem> apiItems, Ship ship)` that merges cargo items into Ship.Cargo and returns whether changes were made
  - Satisfies: Req 7, Criteria 1-6 ("ship asset merge")
  - Inputs: `OE2EmpireTracker.Common/Models/Ship.cs`, design.md Ship merge section
  - Output: Modified `OE2EmpireTracker.Common/Services/AssetMergeService.cs`
  - Verification: `getDiagnostics` — compiles with zero errors/warnings

- [ ] 8. Implement SyncAssetsAsync Method
  - [ ] 8.1 Add `SyncAssetsAsync(string playerUUID, string accessToken)` method to GameApiSyncScheduler that fetches asset locations, filters by assetCount > 0, iterates locations, deserializes detail responses, and routes to the appropriate merge method
  - [ ] 8.2 Implement HTTP error handling: 401 → invalidate credentials + abort cycle; 403 → log + skip; 404 → log + skip location; malformed JSON → log + skip location; circuit breaker open → log + abort
  - [ ] 8.3 Add logging: total locations synced, items processed, errors encountered
  - Satisfies: Req 1, Criteria 1-6 ("asset locations list retrieval"); Req 2, Criteria 1-7 ("asset location detail retrieval"); Req 8, Criteria 2, 5, 6 ("sync scheduling")
  - Inputs: `OE2EmpireTracker.Common/Services/GameApiSyncScheduler.cs`, `OE2EmpireTracker.Common/Client/GameApiClient.cs`
  - Output: Modified `OE2EmpireTracker.Common/Services/GameApiSyncScheduler.cs`
  - Verification: `getDiagnostics` — compiles with zero errors/warnings

- [ ] 9. Wire SyncAssetsAsync into SyncCharacterAsync
  - [ ] 9.1 Add `SyncAssetsAsync` call in `SyncCharacterAsync` after colony sync, wrapped in try/catch to isolate asset failures from profile/colony sync
  - [ ] 9.2 Add WriteContext + RaiseAssetDataChanged after successful asset sync with changes
  - [ ] 9.3 Add virtual override points: `GetPlayerStations`, `GetPlayerShips`, `RaiseAssetDataChanged`
  - Satisfies: Req 8, Criteria 1, 3, 4 ("sync scheduling and integration"); Req 9, Criteria 1-4 ("crate handling flows through merge")
  - Inputs: `OE2EmpireTracker.Common/Services/GameApiSyncScheduler.cs`, design.md Integration Point section
  - Output: Modified `OE2EmpireTracker.Common/Services/GameApiSyncScheduler.cs`
  - Verification: `getDiagnostics` — compiles with zero errors/warnings; full solution build passes

- [ ] 10. TypeC Mapping and Purity Extraction Property Tests
  - [ ] 10.1 Write property test: all known typeC codes map to non-None ItemTypeEnum; all unknown strings map to None
  - [ ] 10.2 Write property test: for any base name + known purity, constructing the parenthesized format and calling ExtractResourcePurity returns the original name and purity
  - Satisfies: Correctness Property 3 ("TypeC Totality"); Correctness Property 4 ("Purity Extraction Consistency")
  - Inputs: `OE2EmpireTracker.Common/Services/AssetMergeService.cs`
  - Output: `OE2EmpireTracker.Tests/Services/AssetMergeServicePropertyTests.cs`
  - Verification: `vstest.console` — all property tests pass

- [ ] 11. Colony Merge Unit Tests
  - [ ] 11.1 Test MergeColonyAssets: matching colony by ColonyId, skip on no match, new item creation, existing item update by GameItemId, local-only field preservation, change detection return value
  - Satisfies: Req 5, Criteria 1-6 (verified); Req 13, Criteria 2-4 (verified)
  - Inputs: `OE2EmpireTracker.Common/Services/AssetMergeService.cs`, `OE2EmpireTracker.Common/Models/Colony.cs`
  - Output: `OE2EmpireTracker.Tests/Services/AssetMergeServiceTests.cs`
  - Verification: `vstest.console` — all colony merge tests pass

- [ ] 12. Station Merge Unit Tests
  - [ ] 12.1 Test MergeStationAssets: matching station by GameLocationId, new item creation in default hold, existing item update, change detection return value
  - Satisfies: Req 6, Criteria 1-6 (verified)
  - Inputs: `OE2EmpireTracker.Common/Services/AssetMergeService.cs`, `OE2EmpireTracker.Common/Models/Station.cs`
  - Output: `OE2EmpireTracker.Tests/Services/AssetMergeServiceStationTests.cs`
  - Verification: `vstest.console` — all station merge tests pass

- [ ] 13. Ship Merge Unit Tests
  - [ ] 13.1 Test MergeShipAssets: matching ship by GameLocationId, new item creation in Ship.Cargo, existing item update, change detection return value
  - Satisfies: Req 7, Criteria 1-6 (verified)
  - Inputs: `OE2EmpireTracker.Common/Services/AssetMergeService.cs`, `OE2EmpireTracker.Common/Models/Ship.cs`
  - Output: `OE2EmpireTracker.Tests/Services/AssetMergeServiceShipTests.cs`
  - Verification: `vstest.console` — all ship merge tests pass

- [ ] 14. Additive Merge and Idempotency Property Tests
  - [ ] 14.1 Write property test: after any merge, ItemBag count >= count before merge (additive only)
  - [ ] 14.2 Write property test: after merge, no ItemBag contains two items with the same non-null GameItemId (uniqueness)
  - [ ] 14.3 Write property test: merging the same API response twice produces the same state; second call returns false (idempotent)
  - Satisfies: Correctness Property 1 ("Additive Only"); Correctness Property 2 ("GameItemId Uniqueness"); Correctness Property 5 ("Idempotent Merge")
  - Inputs: `OE2EmpireTracker.Common/Services/AssetMergeService.cs`, `OE2EmpireTracker.Common/Models/ItemBag.cs`
  - Output: Modified `OE2EmpireTracker.Tests/Services/AssetMergeServicePropertyTests.cs`
  - Verification: `vstest.console` — all property tests pass

- [ ] 15. SyncAssetsAsync Error Handling Tests
  - [ ] 15.1 Test 401 handling: credentials invalidated, cycle aborted
  - [ ] 15.2 Test 403 handling: logged, cycle skipped (locations) or location skipped (detail)
  - [ ] 15.3 Test circuit breaker open: logged, cycle aborted
  - [ ] 15.4 Test malformed JSON: logged, location skipped, other locations still processed
  - [ ] 15.5 Test zero assetCount locations are skipped (no detail API call made)
  - Satisfies: Req 1, Criteria 3-6 (verified); Req 2, Criteria 3-6 (verified); Req 8, Criteria 6 (verified); Correctness Property 7 ("Error Isolation")
  - Inputs: `OE2EmpireTracker.Common/Services/GameApiSyncScheduler.cs`
  - Output: `OE2EmpireTracker.Tests/Services/GameApiSyncSchedulerAssetTests.cs`
  - Verification: `vstest.console` — all scheduler asset tests pass

## Notes

- FsCheck 2.16.6 is used for property tests — do NOT use FsCheck 3.x APIs (no `FsCheck.Fluent`, no `Shrink.Default<T>()`)
- Use `[FsCheck.NUnit.Property(MaxTest = 100)]` attribute and LINQ query syntax for generators
- The existing `ColonyMergeService.MapTypeC` uses `ToUpperInvariant()` — the new `MapAssetTypeC` uses case-insensitive matching instead to handle mixed-case API codes
- Station and Ship model changes are additive (new nullable properties) so existing deserialization is unaffected
- The `DefaultHoldName` constant ("default") is used for station asset storage
