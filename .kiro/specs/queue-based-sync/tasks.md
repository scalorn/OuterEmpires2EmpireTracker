# Implementation Plan: Queue-Based Sync

## Overview

Replace sequential background sync with queue-based parallel dispatch using GameApiRequestQueue. Implement QueueSyncService as the orchestrator, add TokenRefreshHandler for serialized 401 handling, integrate crate/survey/blueprint imports into the sync cycle, and add freshness-based skip logic with in-memory indexes for O(1) lookups.

## Tasks

- [ ] 1. Data model extensions
  - [-] 1.1 Add SystemObjectId to Survey and Asteroid models
    - Add `SystemObjectId` property (int, default 0) to `Survey.cs`
    - Add `SystemObjectId` property (int, default 0) to `Asteroid.cs`
    - Both with `[JsonProperty("SystemObjectId")]` and `[DefaultValue(0)]`
    - _Requirements: 6.1, 6.2, 6.3_
    - _Inputs: Survey.cs, Asteroid.cs_
    - _Output: Survey.cs, Asteroid.cs modified_
    - _Verification: getDiagnostics on both files_

  - [-] 1.2 Add GameApiBlueprintId and LastDetailImportUtc to Blueprint model
    - Add `GameApiBlueprintId` property (int?, nullable) to Blueprint
    - Add `LastDetailImportUtc` property (DateTime?, nullable) to Blueprint
    - Both with appropriate `[JsonProperty]` attributes
    - _Requirements: 13.1, 14.1_
    - _Inputs: Blueprint.cs (extends Item)_
    - _Output: Blueprint.cs modified_
    - _Verification: getDiagnostics_

  - [-] 1.3 Add GameApiSurveyId and LastDetailImportUtc to Survey model
    - Add `GameApiSurveyId` property (int?, nullable) to Survey
    - Add `LastDetailImportUtc` property (DateTime?, nullable) to Survey
    - Both with appropriate `[JsonProperty]` attributes
    - _Requirements: 13.2, 14.2_
    - _Inputs: Survey.cs_
    - _Output: Survey.cs modified_
    - _Verification: getDiagnostics_

  - [-] 1.4 Add DetailRefreshHours to GameApiConnectionSettings
    - Add `DetailRefreshHours` property (int, default 24) with `[DefaultValue(24)]`
    - Valid range 1–168 (documented in XML comment)
    - _Requirements: 16.1_
    - _Inputs: GameApiConnectionSettings.cs_
    - _Output: GameApiConnectionSettings.cs modified_
    - _Verification: getDiagnostics_


- [ ] 2. In-memory API ID indexes in PlayerContext
  - [~] 2.1 Add Blueprint API ID index to PlayerContext
    - Add `Dictionary<int, Blueprint> _blueprintByApiIdIndex` field
    - Add `FindBlueprintByApiId(int apiId)` public method returning Blueprint or null
    - Add `IndexBlueprintByApiId(Blueprint bp)` public method
    - Populate index during `InitBlueprints` for items with non-null `GameApiBlueprintId`
    - _Requirements: 15.1, 15.3, 15.5_
    - _Inputs: PlayerContext.cs, Blueprint.cs_
    - _Output: PlayerContext.cs modified_
    - _Verification: getDiagnostics_

  - [~] 2.2 Wire Blueprint index into add/remove operations
    - Update `AddBlueprint` to insert into `_blueprintByApiIdIndex` when API ID is non-null
    - Update `RemoveBlueprint` to remove from `_blueprintByApiIdIndex` when API ID is non-null
    - _Requirements: 15.6_
    - _Inputs: PlayerContext.cs_
    - _Output: PlayerContext.cs modified_
    - _Verification: getDiagnostics_

  - [~] 2.3 Add Survey API ID index to PlayerContext
    - Add `Dictionary<int, Survey> _surveyByApiIdIndex` field
    - Add `FindSurveyByApiId(int apiId)` public method returning Survey or null
    - Add `IndexSurveyByApiId(Survey survey)` public method
    - Populate index during `InitSurveys` for items with non-null `GameApiSurveyId`
    - _Requirements: 15.2, 15.4, 15.5_
    - _Inputs: PlayerContext.cs, Survey.cs_
    - _Output: PlayerContext.cs modified_
    - _Verification: getDiagnostics_

  - [~] 2.4 Wire Survey index into add/remove operations
    - Update `AddSurvey` to insert into `_surveyByApiIdIndex` when API ID is non-null
    - Update `RemoveSurvey` to remove from `_surveyByApiIdIndex` when API ID is non-null
    - _Requirements: 15.6_
    - _Inputs: PlayerContext.cs_
    - _Output: PlayerContext.cs modified_
    - _Verification: getDiagnostics_

  - [~] 2.5 Write property test for index consistency
    - **Property 5: Index Consistency**
    - Random Add/Remove/Index sequences; verify FindBlueprintByApiId and FindSurveyByApiId return correct results
    - **Validates: Requirements 15.1, 15.2, 15.3, 15.4, 15.5**
    - _Inputs: PlayerContext.cs_
    - _Output: PlayerContextApiIdIndexTests.cs created_
    - _Verification: vstest.console passes_


- [~] 3. Checkpoint - Model and index verification
  - Build solution and run all tests. Ask the user if questions arise.

- [ ] 4. TokenRefreshHandler
  - [~] 4.1 Create TokenRefreshHandler service
    - Create `OE2EmpireTracker.Common/Services/TokenRefreshHandler.cs`
    - Implement SemaphoreSlim(1,1) serialization for refresh attempts
    - Track `_currentAccessToken` and `_tokenVersion` for stale-check optimization
    - `HandleUnauthorizedAsync`: acquire lock, check version, call ExchangeTokenAsync if stale, update token
    - Expose `CurrentAccessToken` property
    - _Requirements: 7.1, 7.2, 7.3_
    - _Inputs: GameApiClient.cs, CredentialStore.cs, GameApiConnectionSettings.cs_
    - _Output: TokenRefreshHandler.cs created_
    - _Verification: getDiagnostics_

  - [~] 4.2 Write unit tests for TokenRefreshHandler
    - Test serialization: multiple concurrent 401s produce exactly 1 ExchangeTokenAsync call
    - Test version check: second caller sees refreshed token and skips exchange
    - Test failure path: exchange failure returns failure result
    - _Requirements: 7.1, 7.2, 7.3_
    - _Output: TokenRefreshHandlerTests.cs created_
    - _Verification: vstest.console passes_

  - [~] 4.3 Write property test for token refresh serialization
    - **Property 2: Token Refresh Serialization**
    - N concurrent 401 events; verify exactly 1 exchange call occurs
    - **Validates: Requirements 7.4**
    - _Output: TokenRefreshHandlerPropertyTests.cs created_
    - _Verification: vstest.console passes_


- [ ] 5. QueueSyncResult DTO
  - [-] 5.1 Create QueueSyncResult class
    - Create `OE2EmpireTracker.Common/Services/QueueSyncResult.cs`
    - Properties: `Succeeded` (int), `Failed` (int), `Elapsed` (TimeSpan), `FailedLabels` (List<string>)
    - _Requirements: 8.3, 12.3_
    - _Inputs: design.md QueueSyncResult section_
    - _Output: QueueSyncResult.cs created_
    - _Verification: getDiagnostics_

- [ ] 6. QueueSyncService core orchestration
  - [~] 6.1 Create QueueSyncService shell with concurrency guard
    - Create `OE2EmpireTracker.Common/Services/QueueSyncService.cs`
    - Constructor accepting PlayerContext, EmpireContext, GameApiClient, GameApiConnectionSettings
    - `RunSyncAsync(CancellationToken)` with `_isSyncRunning` + lock guard (returns early if already running)
    - `IsSyncRunning` property
    - _Requirements: 9.2, 9.3_
    - _Inputs: GameApiConnectionSettings.cs, PlayerContext.cs_
    - _Output: QueueSyncService.cs created_
    - _Verification: getDiagnostics_

  - [~] 6.2 Add queue construction and IsDetailFresh helper to QueueSyncService
    - Inside `RunSyncAsync`: construct GameApiRequestQueue with TPS from settings, inflight = TPS × 3, maxRetries = 3
    - Add `IsDetailFresh(DateTime? lastImportUtc)` private helper using DetailRefreshHours and SystemClock.UtcNow
    - _Requirements: 1.1, 1.2, 1.3, 1.5, 14.3, 14.4, 14.6_
    - _Inputs: QueueSyncService.cs, GameApiRequestQueue.cs, GameApiConnectionSettings.cs_
    - _Output: QueueSyncService.cs modified_
    - _Verification: getDiagnostics_

  - [~] 6.3 Write property test for concurrency guard
    - **Property 1: No Concurrent Sync Cycles**
    - Invoke RunSyncAsync concurrently from multiple threads; verify only one completes with actual work
    - **Validates: Requirements 9.3**
    - _Output: QueueSyncServicePropertyTests.cs created_
    - _Verification: vstest.console passes_

  - [~] 6.4 Write property test for freshness skip correctness
    - **Property 3: Freshness Skip Correctness**
    - Generate random DateTime?/hours combos; verify skip/enqueue decision matches spec
    - **Validates: Requirements 14.3, 14.4**
    - _Output: FreshnessSkipPropertyTests.cs created_
    - _Verification: vstest.console passes_


- [ ] 7. Seed work item factories — non-cascading (character, banking, jobs)
  - [~] 7.1 Implement character and banking seed work item factories
    - Add private factory methods to QueueSyncService for: CharacterProfile, CharacterSkills, BankingBalance, BankingTransactions, AcceptedJobs
    - Each creates a WorkItem with label + async delegate that calls GameApiClient and updates PlayerContext
    - _Requirements: 1.4, 2.1_
    - _Inputs: QueueSyncService.cs, GameApiClient.cs, PlayerContext.cs_
    - _Output: QueueSyncService.cs modified_
    - _Verification: getDiagnostics_

  - [~] 7.2 Implement ship and market seed work item factories
    - Add private factory methods to QueueSyncService for: ShipConfiguration, ShipCargo, MarketListings, MarketItems, MarketBuyOrders, MarketSellOrders
    - Each creates a WorkItem with label + async delegate that calls GameApiClient and updates PlayerContext
    - _Requirements: 1.4, 2.1_
    - _Inputs: QueueSyncService.cs, GameApiClient.cs, PlayerContext.cs_
    - _Output: QueueSyncService.cs modified_
    - _Verification: getDiagnostics_

- [ ] 8. Seed work item factories — cascading (colonies)
  - [~] 8.1 Implement ColonyList seed work item with cascading
    - Add `CreateColonyListItem` factory that cascades 4 items per colony (summary, buildings, warehouse, workers)
    - Add `CreateColonySummaryItem`, `CreateColonyBuildingsItem`, `CreateColonyWarehouseItem`, `CreateColonyWorkersItem` factories
    - _Requirements: 2.1, 2.2_
    - _Inputs: QueueSyncService.cs, GameApiClient.cs_
    - _Output: QueueSyncService.cs modified_
    - _Verification: getDiagnostics_

  - [~] 8.2 Write property test for colony cascading completeness
    - **Property 7: Cascading Completeness (colonies)**
    - Generate random colony lists; verify cascade count = 4 × colony count
    - **Validates: Requirements 2.2**
    - _Output: CascadeCompletenessPropertyTests.cs created_
    - _Verification: vstest.console passes_


- [ ] 9. Seed work item factories — cascading (asset locations)
  - [~] 9.1 Implement AssetLocations list seed work item
    - Add `CreateAssetLocationsItem` factory that calls GetAssetLocationsAsync
    - On success, enqueue one `CreateAssetLocationDetailItem` per returned location
    - _Requirements: 2.3_
    - _Inputs: QueueSyncService.cs, GameApiClient.cs_
    - _Output: QueueSyncService.cs modified_
    - _Verification: getDiagnostics_

  - [~] 9.2 Implement AssetLocationDetail dispatch with freshness check
    - Add `CreateAssetLocationDetailItem(id, typeC, planetName, systemName)` factory
    - Inspect TypeC: "Crate" → enqueue CrateDetail; "Bp" → check freshness via FindBlueprintByApiId + IsDetailFresh, enqueue BlueprintDetail if stale; "S" → check freshness via FindSurveyByApiId + IsDetailFresh, enqueue SurveyDetail if stale
    - _Requirements: 2.4, 2.5, 2.6, 14.3, 14.4, 15.7_
    - _Inputs: QueueSyncService.cs, PlayerContext.cs_
    - _Output: QueueSyncService.cs modified_
    - _Verification: getDiagnostics_

  - [~] 9.3 Write property test for asset cascading completeness
    - **Property 7b: Cascading Completeness (assets)**
    - Generate random asset location lists; verify cascade count = 1 per asset
    - **Validates: Requirements 2.3**
    - _Output: AssetCascadePropertyTests.cs created_
    - _Verification: vstest.console passes_

- [ ] 10. Seed work item factories — cascading (mail)
  - [~] 10.1 Implement KillMailList and MailList seed work items with cascading
    - Add `CreateKillMailListItem` that cascades per-kill-mail detail items
    - Add `CreateMailListItem` that cascades per-mail detail items + next page item
    - Add `CreateKillMailDetailItem`, `CreateMailDetailItem`, `CreateMailListPageItem` factories
    - _Requirements: 2.7, 2.8_
    - _Inputs: QueueSyncService.cs, GameApiClient.cs_
    - _Output: QueueSyncService.cs modified_
    - _Verification: getDiagnostics_


- [~] 11. Checkpoint - Core orchestration verification
  - Build solution and run all tests. Ask the user if questions arise.

- [ ] 12. Crate import integration
  - [~] 12.1 Implement CrateDetail work item factory
    - Add `CreateCrateDetailItem(int crateId)` factory to QueueSyncService
    - Calls `GameApiClient.GetAssetCrateAsync`, passes JSON to `CrateImporter.ImportFromJson`
    - Provides PlayerContext and EmpireContext to CrateImporter
    - _Requirements: 3.1, 3.2, 3.3_
    - _Inputs: QueueSyncService.cs, CrateImporter.cs, GameApiClient.cs_
    - _Output: QueueSyncService.cs modified_
    - _Verification: getDiagnostics_

- [ ] 13. Survey import integration
  - [~] 13.1 Implement SurveyDetail work item — parse and find/merge/create
    - Add `CreateSurveyDetailItem(int surveyId, string planetName, string systemName)` factory to QueueSyncService
    - Parse GameApiSurveyResponse, construct temp Survey from response fields
    - Use SurveyImportHelper.FindByKey → MergeData (if match) or CreateFromTemp (if new)
    - Call SurveyImportHelper.LinkOrCreateAsteroid for asteroid surveys
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_
    - _Inputs: QueueSyncService.cs, SurveyImportHelper.cs, GameApiClient.cs_
    - _Output: QueueSyncService.cs modified_
    - _Verification: getDiagnostics_

  - [~] 13.2 Wire SystemObjectId propagation and index update in SurveyDetail
    - After survey import: set survey.SystemObjectId from response (if > 0, propagate to linked asteroid)
    - Set survey.GameApiSurveyId = response.Survey.Id
    - Set survey.LastDetailImportUtc = SystemClock.UtcNow
    - Call PlayerContext.IndexSurveyByApiId(survey)
    - _Requirements: 4.6, 6.4, 13.4, 14.5_
    - _Inputs: QueueSyncService.cs, PlayerContext.cs_
    - _Output: QueueSyncService.cs modified_
    - _Verification: getDiagnostics_

  - [~] 13.3 Write property test for SystemObjectId propagation
    - **Property 8: SystemObjectId Propagation**
    - Import surveys with various SystemObjectId values; verify propagation to linked asteroids
    - **Validates: Requirements 6.4**
    - _Output: SystemObjectIdPropagationPropertyTests.cs created_
    - _Verification: vstest.console passes_


- [ ] 14. Blueprint import integration
  - [~] 14.1 Implement BlueprintDetail work item — parse response and construct crate JSON
    - Add `CreateBlueprintDetailItem(int blueprintId)` factory to QueueSyncService
    - Parse GameApiBlueprintDetailResponse, construct CrateImporter-compatible JSON object
    - Map `partTypeIcon` → `_IconClass` with "ui_icon_" prefix (skip if empty/null)
    - Pass to CrateImporter.ImportFromJson
    - _Requirements: 5.1, 5.2, 5.3, 11.1, 11.2, 11.3_
    - _Inputs: QueueSyncService.cs, CrateImporter.cs, GameApiClient.cs_
    - _Output: QueueSyncService.cs modified_
    - _Verification: getDiagnostics_

  - [~] 14.2 Wire API ID tracking and freshness timestamp for BlueprintDetail
    - After CrateImporter import: set GameApiBlueprintId on imported blueprint
    - Set LastDetailImportUtc = SystemClock.UtcNow
    - Call PlayerContext.IndexBlueprintByApiId(blueprint)
    - _Requirements: 5.4, 13.3, 14.5_
    - _Inputs: QueueSyncService.cs, PlayerContext.cs_
    - _Output: QueueSyncService.cs modified_
    - _Verification: getDiagnostics_

- [ ] 15. 401 handling in work item delegates
  - [~] 15.1 Wire TokenRefreshHandler into QueueSyncService
    - Construct TokenRefreshHandler in QueueSyncService constructor
    - Add 401 detection wrapper in work item delegates: call HandleUnauthorizedAsync, re-enqueue on success, mark failed on failure
    - _Requirements: 7.1, 7.2, 7.3_
    - _Inputs: QueueSyncService.cs, TokenRefreshHandler.cs_
    - _Output: QueueSyncService.cs modified_
    - _Verification: getDiagnostics_

- [ ] 16. 429 handling in work item delegates
  - [~] 16.1 Wire 429 rate limit handling in work item delegates
    - Add 429 detection in work item delegates: extract Retry-After header, call queue.NotifyRateLimited
    - _Requirements: 10.1, 10.2, 10.3, 10.4_
    - _Inputs: QueueSyncService.cs, GameApiRequestQueue.cs_
    - _Output: QueueSyncService.cs modified_
    - _Verification: getDiagnostics_


- [ ] 17. Metrics and end-of-cycle reporting
  - [~] 17.1 Wire metrics CSV file path into GameApiRequestQueue construction
    - Pass metrics file path to GameApiRequestQueue at construction in RunSyncAsync
    - After DrainAsync: collect GetCompletionStatus counts
    - _Requirements: 8.1, 8.2_
    - _Inputs: QueueSyncService.cs, GameApiRequestQueue.cs_
    - _Output: QueueSyncService.cs modified_
    - _Verification: getDiagnostics_

  - [~] 17.2 Add completion logging, persistence, and QueueSyncResult return
    - Log summary (succeeded/failed/elapsed) using NLog
    - Log each failed item with label and exception message
    - Persist via PlayerContext.WriteContext on completion
    - Return populated QueueSyncResult
    - _Requirements: 8.3, 9.4, 12.2, 12.3_
    - _Inputs: QueueSyncService.cs, QueueSyncResult.cs_
    - _Output: QueueSyncService.cs modified_
    - _Verification: getDiagnostics_

  - [~] 17.3 Write property test for error isolation
    - **Property 4: Error Isolation**
    - Inject random failures into work item delegates; verify succeeded + failed = total enqueued
    - **Validates: Requirements 12.1, 12.3**
    - _Output: ErrorIsolationPropertyTests.cs created_
    - _Verification: vstest.console passes_

- [~] 18. Checkpoint - Full service verification
  - Build solution and run all tests. Ask the user if questions arise.

- [ ] 19. BackgroundProcessor integration
  - [~] 19.1 Invoke QueueSyncService from BackgroundProcessor
    - Modify `BackgroundProcessor.cs` to construct QueueSyncService
    - When timer fires and Game API sync is enabled and `!_queueSyncService.IsSyncRunning`: invoke RunSyncAsync on background thread
    - _Requirements: 9.1, 9.2, 9.3_
    - _Inputs: BackgroundProcessor.cs, QueueSyncService.cs, GameApiConnectionSettings.cs_
    - _Output: BackgroundProcessor.cs modified_
    - _Verification: getDiagnostics_

- [ ] 20. Preferences form integration
  - [~] 20.1 Add DetailRefreshHours control to Preferences form
    - Add NumericUpDown control labeled "Detail Refresh (hours)" on Game API tab
    - Min=1, Max=168, Default=24
    - Bind to GameApiConnectionSettings.DetailRefreshHours on load and save
    - _Requirements: 16.2, 16.3, 16.4, 16.5_
    - _Inputs: FormPreferences.cs, FormPreferences.Designer.cs, GameApiConnectionSettings.cs_
    - _Output: FormPreferences.cs modified, FormPreferences.Designer.cs modified_
    - _Verification: getDiagnostics_

- [~] 21. Final checkpoint - Full solution build and test
  - Build solution and run all tests. Ask the user if questions arise.


## Notes

- Tasks marked with `*` are optional property tests and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- The design uses the existing `GameApiRequestQueue` infrastructure — no queue implementation needed
- Existing `CrateImporter` and `SurveyImportHelper` services are reused as-is for import logic
- `SystemClock.UtcNow` must be used (not `DateTime.UtcNow`) per project conventions
- All tasks stay within sizing limits: ≤5 files modified, ≤200 new LOC, ≤3 acceptance criteria

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3", "1.4", "5.1"] },
    { "id": 1, "tasks": ["2.1", "2.3"] },
    { "id": 2, "tasks": ["2.2", "2.4"] },
    { "id": 3, "tasks": ["2.5", "4.1"] },
    { "id": 4, "tasks": ["4.2", "4.3", "6.1"] },
    { "id": 5, "tasks": ["6.2", "6.3", "6.4"] },
    { "id": 6, "tasks": ["7.1", "7.2"] },
    { "id": 7, "tasks": ["8.1", "8.2", "9.1"] },
    { "id": 8, "tasks": ["9.2", "9.3", "10.1"] },
    { "id": 9, "tasks": ["12.1"] },
    { "id": 10, "tasks": ["13.1", "14.1"] },
    { "id": 11, "tasks": ["13.2", "14.2"] },
    { "id": 12, "tasks": ["13.3", "15.1"] },
    { "id": 13, "tasks": ["16.1", "17.1"] },
    { "id": 14, "tasks": ["17.2", "17.3"] },
    { "id": 15, "tasks": ["19.1"] },
    { "id": 16, "tasks": ["20.1"] }
  ]
}
```
