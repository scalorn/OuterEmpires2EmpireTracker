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

- [x] 1. Create Asset API Response DTOs
  - [x] 1.1 Create DTO classes (implemented via generated client: AssetCargoItem, AssetLocations, etc.)

- [x] 2. Add GameLocationId to Station and Ship Models
  - [x] 2.1 GameLocationId, SystemName, SystemId on Station
  - [x] 2.2 GameLocationId on Ship

- [x] 3. Implement TypeC Mapping and Resource Purity Extraction
  - [x] 3.1 AssetMergeService.MapAssetTypeC
  - [x] 3.2 AssetMergeService.ExtractResourcePurity

- [x] 4. Implement ProcessSingleAssetItem and Item Creation/Update
  - [x] 4.1 ProcessSingleAssetItem
  - [x] 4.2 UpdateExistingAssetItem
  - [x] 4.3 CreateAssetItem

- [x] 5. Implement MergeColonyAssets
  - [x] 5.1 MergeColonyAssets method

- [x] 6. Implement MergeStationAssets
  - [x] 6.1 MergeStationAssets method

- [x] 7. Implement MergeShipAssets
  - [x] 7.1 MergeShipAssets method

- [x] 8. Implement SyncAssetsAsync Method
  - [x] 8.1 SyncAssetsAsync on GameApiSyncScheduler
  - [x] 8.2 HTTP error handling (401/403/404/malformed)
  - [x] 8.3 Logging

- [x] 9. Wire SyncAssetsAsync into SyncCharacterAsync
  - [x] 9.1 Call in SyncCharacterAsync with try/catch isolation
  - [x] 9.2 WriteContext + event raise
  - [x] 9.3 Virtual override points

- [x] 10. TypeC Mapping and Purity Extraction Property Tests
  - [x] 10.1 TypeC totality property test
  - [x] 10.2 Purity extraction round-trip property test

- [x] 11. Colony Merge Unit Tests
  - [x] 11.1 AssetMergeServiceTests.cs

- [x] 12. Station Merge Unit Tests
  - [x] 12.1 AssetMergeServiceStationTests.cs

- [x] 13. Ship Merge Unit Tests
  - [x] 13.1 AssetMergeServiceShipTests.cs

- [x] 14. Additive Merge and Idempotency Property Tests
  - [x] 14.1 Additive-only property
  - [x] 14.2 GameItemId uniqueness property
  - [x] 14.3 Idempotent merge property

- [x] 15. SyncAssetsAsync Error Handling Tests
  - [x] 15.1 401 handling
  - [x] 15.2 403 handling
  - [x] 15.3 Circuit breaker
  - [x] 15.4 Malformed JSON
  - [x] 15.5 Zero assetCount skip

## Notes

- All tasks completed. DTOs are in the generated client rather than custom DTO files.
- Implementation verified by existing test suite passing.
