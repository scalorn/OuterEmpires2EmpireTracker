# Implementation Plan: Colony API Sync

## Overview

Phase 1 (infrastructure + discovery) is complete. Phase 2 implements the merge logic and scheduler integration using confirmed typeC mappings from the discovery report (74 colonies, 3260 buildings, 738 warehouse items analyzed).

## Tasks

- [x] 1. Create building sub-model classes
  - [x] 1.1 Create BuildingSubModels.cs with building sub-model classes
  - [x] 1.2 Create ItemProperty.cs sub-model class
- [x] 2. Expand Colony model
  - [x] 2.1 Add new API-sourced properties to Colony.cs
  - [x] 2.2 Add migration logic to Colony.cs
- [x] 3. Expand ColonyStructure model
  - [x] 3.1 Add new API-sourced properties to ColonyStructure.cs
  - [x] 3.2 Deprecate CurrentAttitude and ContentmentIndex on ColonyStructure.cs
- [x] 4. Expand Item model
  - [x] 4.1 Add new API-sourced properties to Item.cs
- [x] 5. Build data model checkpoint
- [x] 6. Create colony list DTOs
  - [x] 6.1 Create GameApiColonyResponse.cs with colony list DTOs
- [x] 7. Create building DTOs
  - [x] 7.1 Add GameApiColonyBuildingsResponse and GameApiColonyBuilding DTOs
- [x] 8. Create warehouse DTOs
  - [x] 8.1 Add GameApiColonyWarehouseResponse and GameApiWarehouseItem DTOs
- [x] 9. Build DTOs checkpoint
- [x] 10. Add colony endpoint methods to GameApiClient
  - [x] 10.1 Add GetColonyListAsync method to GameApiClient.cs
  - [x] 10.2 Add GetColonyBuildingsAsync method to GameApiClient.cs
  - [x] 10.3 Add GetColonyWarehouseAsync method to GameApiClient.cs
- [x] 11. Build API client checkpoint
- [x] 12. Write migration tests
  - [x] 12.1 Write unit tests for Colony deserialization migration
- [x] 13. Create API discovery test fixture
  - [x] 13.1 Create GameApiColonyDiscoveryTests.cs test fixture
- [x] 14. Phase 1 complete checkpoint
- [x] 15. Create ColonyMergeService with colony list merge
  - [x] 15.1 Create ColonyMergeService.cs with MergeColonyList method
    - Create `OE2EmpireTracker.Common/Services/ColonyMergeService.cs`
    - Implement static class with MergeColonyList(apiColonies, localColonies, ownerUUID)
    - Implement ColonyMergeResult class (Created, Updated, Skipped, HasChanges, ColonyIdToUUIDMap)
    - Dedup by PlanetName + SystemName (case-insensitive, same owner)
    - Skip entries with null/empty SystemObjectName, per-colony try/catch
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 13.2, 13.3_
    - _Verification: getDiagnostics shows zero errors_
  - [x] 15.2 Implement MergeAllColonyFields helper in ColonyMergeService
    - Private method that overwrites all 14 game-authoritative fields
    - String fields: skip if API value is null/empty (preserve local)
    - Numeric fields: always overwrite (0 is valid game state)
    - Log each field conflict, set LastImportDateTime, track changes
    - _Requirements: 7.1-7.16, 10.1, 10.2, 15.3, 15.4, 15.5_
    - _Verification: getDiagnostics shows zero errors_
- [x] 16. Implement building merge
  - [x] 16.1 Implement MergeBuildings method in ColonyMergeService
    - Add MergeBuildings(apiBuildings, colony) returning bool
    - Match by BlueprintDesignName (case-insensitive)
    - Update status, all API fields, replace collections
    - Conditional: MiningSurveyResource if local empty, BuildCompletionTime if local null
    - Create new ColonyStructure for unmatched, never remove absent
    - _Requirements: 8.1-8.18_
    - _Verification: getDiagnostics shows zero errors_
- [x] 17. Implement warehouse merge
  - [x] 17.1 Implement MergeWarehouse method in ColonyMergeService
    - Add MergeWarehouse(apiItems, colony) returning bool
    - Match by ResourceName + TypeC (case-insensitive)
    - Confirmed TypeC mapping: R=Resource, Sc=Survey, W=WorkDetail, S=ShipPart, Bp=Blueprint, F=Flatpack, C=Commodity, Sh=ShipHull
    - Update all API fields, replace ItemProperties, create new for unmatched
    - Never remove absent, zero amount sets Quantity to 0
    - _Requirements: 9.1-9.14_
    - _Verification: getDiagnostics shows zero errors_
- [x] 18. Build merge logic checkpoint
  - Build full solution, verify zero errors and zero warnings
- [x] 19. Write merge unit tests
  - [x] 19.1 Write unit tests for MergeColonyList
    - Create `OE2EmpireTracker.Tests/Services/ColonyMergeServiceTests.cs`
    - Test new colony creation, existing colony update, idempotency, empty list, null skip, owner isolation
    - _Requirements: 6.1-6.4, 7.1-7.16, 10.1, 10.2, 13.2, 13.3, 13.5_
    - _Verification: vstest.console runs tests and all pass_
  - [x] 19.2 Write unit tests for MergeBuildings
    - Test new building creation, existing update, collection replacement, local-only preservation
    - _Requirements: 8.1-8.18_
    - _Verification: vstest.console runs tests and all pass_
  - [x] 19.3 Write unit tests for MergeWarehouse
    - Test new item creation, existing update, TypeC mapping, zero amount, absent not removed
    - _Requirements: 9.1-9.14_
    - _Verification: vstest.console runs tests and all pass_
- [x] 20. Write merge property tests
  - [x]* 20.1 Write property test for dedup idempotency (Property 1)
    - _Validates: Requirements 6.1, 6.2, 6.3_
  - [x]* 20.2 Write property test for local-only field preservation (Property 2)
    - _Validates: Requirements 8.5_
  - [x]* 20.3 Write property test for no data loss on empty response (Property 3)
    - _Validates: Requirements 8.4, 9.4, 13.5_
  - [x]* 20.4 Write property test for owner isolation (Property 4)
    - _Validates: Requirements 6.4_
- [x] 21. Integrate colony sync into scheduler
  - [x] 21.1 Add SyncColoniesAsync method to GameApiSyncScheduler
    - Call GetColonyListAsync after profile sync, handle 401/403
    - Deserialize, invoke ColonyMergeService.MergeColonyList
    - _Requirements: 5.1, 5.2, 5.3, 5.6, 5.7, 13.1, 13.4, 14.1, 15.1, 15.2, 15.5_
    - _Verification: getDiagnostics shows zero errors_
  - [x] 21.2 Add per-colony detail fetching to SyncColoniesAsync
    - For RemoteAccess > 0: call buildings and warehouse endpoints
    - Scope caching on first 403, handle 404, invoke merge methods
    - _Requirements: 5.4, 5.5, 13.6, 14.2, 14.3, 14.4, 15.6_
    - _Verification: getDiagnostics shows zero errors_
  - [x] 21.3 Add persistence and UI notification
    - If HasChanges: WriteContext + raise ColonyDataChanged
    - Batch save after all colonies processed
    - _Requirements: 11.1, 11.2, 11.3, 12.1, 12.3_
    - _Verification: getDiagnostics shows zero errors_
- [x] 22. Build scheduler checkpoint
  - Build full solution, verify zero errors and zero warnings
- [x] 23. Write scheduler integration tests
  - [x]* 23.1 Write integration tests for SyncColoniesAsync
    - Test successful sync, 401 handling, 403 scope skip, malformed JSON, scope caching
    - _Requirements: 5.1-5.7, 11.1-11.3, 12.1, 12.3, 13.1, 13.4, 14.1-14.4_
    - _Verification: vstest.console runs tests and all pass_
- [x] 24. Final checkpoint
  - Build full solution, all tests pass, audit clean

## Notes

- Tasks marked with * are optional property-based tests
- Phase 1 (tasks 1-14) is complete
- Phase 2 (tasks 15-24) uses confirmed typeC mappings from discovery
- Confirmed TypeC codes: R=Resource, Sc=Survey, W=WorkDetail, S=ShipPart, Bp=Blueprint, F=Flatpack, C=Commodity (assumed), Sh=ShipHull (assumed)

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["15.1"] },
    { "id": 1, "tasks": ["15.2", "16.1", "17.1"] },
    { "id": 2, "tasks": ["19.1", "19.2", "19.3"] },
    { "id": 3, "tasks": ["20.1", "20.2", "20.3", "20.4"] },
    { "id": 4, "tasks": ["21.1"] },
    { "id": 5, "tasks": ["21.2"] },
    { "id": 6, "tasks": ["21.3"] },
    { "id": 7, "tasks": ["23.1"] }
  ]
}
```
