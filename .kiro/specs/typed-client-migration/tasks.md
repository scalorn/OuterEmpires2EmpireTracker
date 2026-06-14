# Implementation Plan: Typed Client Migration

## Overview

Migrate all production consumers from the legacy `GameApiClient` to `IGameApiTypedClient` in a phased approach. Each phase builds on the previous one, ensuring the codebase compiles after each task. The typed client infrastructure already exists — this migration rewires consumers, adapts merge services, and finally deletes the old client and hand-written response models.

## Tasks

- [x] 1. Phase 1: Foundation (GameApiContext + TokenRefreshHandler)
  - [x] 1.1 Migrate GameApiContext to create and hold IGameApiTypedClient
    - Replace `GameApiClient Client` property with `IGameApiTypedClient TypedClient`
    - Create `GameApiTypedClient` in `Initialize()` with ServerUrl, AppId, Tps from settings
    - Configure `TokenBucketRateLimiter` TPS from `GameApiConnectionSettings.Tps`
    - Dispose the typed client in `Dispose()`
    - Remove old `SetRateLimit` call
    - _Requirements: 1.1, 1.2, 1.3, 1.5, 8.1, 8.2_
    - _Inputs: GameApiContext.cs, GameApiConnectionSettings.cs_
    - _Output: GameApiContext.cs_
    - _Verification: getDiagnostics on GameApiContext.cs_

  - [x] 1.2 Migrate TokenRefreshHandler to use IGameApiTypedClient
    - Change constructor to accept `IGameApiTypedClient` instead of `GameApiClient`
    - Remove `_apiClient.InvalidateToken()` call
    - Call `IGameApiTypedClient.ExchangeTokenAsync` in `HandleUnauthorizedAsync`
    - Extract `AccessToken` from `TokenResponseDto`
    - Catch `ApiHttpException` for refresh failures
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 14.5_
    - _Inputs: TokenRefreshHandler.cs, IGameApiTypedClient.cs_
    - _Output: TokenRefreshHandler.cs_
    - _Verification: getDiagnostics on TokenRefreshHandler.cs_


  - [x] 1.3 Remove GameApiClient property from GameApiContext and update downstream references
    - Remove `GameApiClient` field and property from GameApiContext
    - Update `GameApiConnectionMonitor` construction to pass `typedClient`
    - Update `ProductionSyncScheduler` construction to pass `typedClient`
    - Ensure GameApiContext compiles with only IGameApiTypedClient
    - _Requirements: 1.4, 8.4_
    - _Inputs: GameApiContext.cs_
    - _Output: GameApiContext.cs_
    - _Verification: getDiagnostics on GameApiContext.cs_

- [x] 2. Phase 2: Merge Services
  - [x] 2.1 Migrate ProfileMergeService to accept PublicCharacter DTO
    - Change `MergeProfileData` parameter from `GameApiProfileResponse` to `PublicCharacter`
    - Update field access (`UUID` → `Uuid`, same-named fields stay)
    - Update Skills/Ranks/SkillInTraining nested object access
    - _Requirements: 7.1, 7.9_
    - _Inputs: ProfileMergeService.cs, IGameApiTypedClient.cs (for DTO shape)_
    - _Output: ProfileMergeService.cs_
    - _Verification: getDiagnostics on ProfileMergeService.cs_

  - [x] 2.2 Migrate ColonyMergeService — MergeColonyList and MergeBuildings
    - Change `MergeColonyList` parameter from `GameApiColonyListResponse` to `ColonyList`
    - Change `MergeBuildings` parameter from `GameApiColonyBuildingsResponse` to `ColonyBuildings`
    - Update internal field access to use generated DTO property names
    - _Requirements: 7.2, 7.3, 7.9_
    - _Inputs: ColonyMergeService.cs, Generated DTOs_
    - _Output: ColonyMergeService.cs_
    - _Verification: getDiagnostics on ColonyMergeService.cs_


  - [x] 2.3 Migrate ColonyMergeService — MergeWarehouse and MergeWorkers
    - Change `MergeWarehouse` parameter from `GameApiColonyWarehouseResponse` to `ColonyWarehouse`
    - Change `MergeWorkers` parameter from `GameApiColonyWorkersResponse` to `ColonyWorkers`
    - Update internal field access to use generated DTO property names
    - _Requirements: 7.4, 7.5, 7.9_
    - _Inputs: ColonyMergeService.cs, Generated DTOs_
    - _Output: ColonyMergeService.cs_
    - _Verification: getDiagnostics on ColonyMergeService.cs_

  - [x] 2.4 Migrate AssetMergeService to accept ICollection<AssetCargoItem>
    - Change `MergeColonyAssets` from `List<GameApiAssetCargoItem>` to `ICollection<AssetCargoItem>`
    - Change `MergeStationAssets` from `List<GameApiAssetCargoItem>` to `ICollection<AssetCargoItem>`
    - Change `MergeShipAssets` from `List<GameApiAssetCargoItem>` to `ICollection<AssetCargoItem>`
    - Update field access (`CargoItemId` → `Id`, same-named fields stay)
    - _Requirements: 7.6, 7.9_
    - _Inputs: AssetMergeService.cs, Generated DTOs_
    - _Output: AssetMergeService.cs_
    - _Verification: getDiagnostics on AssetMergeService.cs_

  - [x] 2.5 Migrate BlueprintLinkageService and SurveyLinkageService
    - Change `BlueprintLinkageService.ProcessItem` from `GameApiAssetCargoItem` to `AssetCargoItem`
    - Change `BuildCandidateBlueprint` and `ClassifyBlueprintType` parameters
    - Change `SurveyLinkageService.ProcessItem` from `GameApiAssetCargoItem` to `AssetCargoItem`
    - Update field access (`CargoItemId` → `Id`, properties access)
    - _Requirements: 7.7, 7.8, 7.9_
    - _Inputs: BlueprintLinkageService.cs, SurveyLinkageService.cs, Generated DTOs_
    - _Output: BlueprintLinkageService.cs, SurveyLinkageService.cs_
    - _Verification: getDiagnostics on both files_


- [x] 3. Phase 3: Core Consumers — QueueSyncService
  - [x] 3.1 Migrate QueueSyncService constructor and RunSyncAsync token exchange
    - Change constructor to accept `IGameApiTypedClient` instead of `GameApiClient`
    - Update `RunSyncAsync` to call `IGameApiTypedClient.ExchangeTokenAsync`
    - Remove tuple-based token handling (`result.Success`, `result.Json`)
    - Add `HandleUnauthorizedAsync` and `HandleRateLimited` helper methods
    - Remove `ThrowIfUnauthorizedAsync` and `ThrowIfRateLimited` old helpers
    - _Requirements: 2.1, 2.2, 2.4, 2.5, 9.1, 9.2, 14.2_
    - _Inputs: QueueSyncService.cs, IGameApiTypedClient.cs, TokenRefreshHandler.cs_
    - _Output: QueueSyncService.cs_
    - _Verification: getDiagnostics on QueueSyncService.cs_

  - [x] 3.2 Migrate QueueSyncService work items — Profile, Skills, Banking
    - Migrate `CreateCharacterProfileItem` to call `GetCharacterAsync` → `PublicCharacter` DTO
    - Migrate `CreateCharacterSkillsItem` to call `GetCharacterSkillsAsync` → `CharacterSkills` DTO
    - Migrate `CreateBankingBalanceItem` to call `GetBankingBalanceAsync` → `BankingBalance` DTO
    - Migrate `CreateBankingTransactionsItem` to call `GetBankingTransactionsAsync` → `BankingTransactions` DTO
    - Remove all `JsonConvert.DeserializeObject` and envelope unwrapping
    - Add try/catch for `ApiHttpException` (401, 429) and `ApiBusinessException`
    - _Requirements: 2.3, 2.6, 9.1, 9.2, 9.4, 9.6_
    - _Inputs: QueueSyncService.cs_
    - _Output: QueueSyncService.cs_
    - _Verification: getDiagnostics on QueueSyncService.cs_


  - [x] 3.3 Migrate QueueSyncService work items — AcceptedJobs, ShipConfig, ShipCargo
    - Migrate `CreateAcceptedJobsItem` to call typed client → `AcceptedJobs` DTO
    - Migrate `CreateShipConfigItem` to call typed client → `ShipConfiguration` DTO
    - Migrate `CreateShipCargoItem` to call typed client → `ShipCargo` DTO
    - Remove all `JsonConvert.DeserializeObject` and envelope unwrapping
    - Add try/catch for `ApiHttpException` and `ApiBusinessException`
    - _Requirements: 2.3, 2.6, 9.1, 9.2, 9.4, 9.6_
    - _Inputs: QueueSyncService.cs_
    - _Output: QueueSyncService.cs_
    - _Verification: getDiagnostics on QueueSyncService.cs_

  - [x] 3.4 Migrate QueueSyncService work items — Market (Listings, Items, BuyOrders, SellOrders)
    - Migrate `CreateMarketListingsItem` to call typed client → `MarketListings` DTO
    - Migrate `CreateMarketItemsItem` to call typed client → `MarketItems` DTO
    - Migrate `CreateMarketBuyOrdersItem` to call typed client → `MarketBuyOrders` DTO
    - Migrate `CreateMarketSellOrdersItem` to call typed client → `MarketSellOrders` DTO
    - Remove all `JsonConvert.DeserializeObject` and envelope unwrapping
    - Add try/catch for `ApiHttpException` and `ApiBusinessException`
    - _Requirements: 2.3, 2.6, 9.1, 9.2, 9.4, 9.6_
    - _Inputs: QueueSyncService.cs_
    - _Output: QueueSyncService.cs_
    - _Verification: getDiagnostics on QueueSyncService.cs_

  - [x] 3.5 Migrate QueueSyncService work items — ColonyList, ColonySummary, ColonyBuildings
    - Migrate `CreateColonyListItem` to call typed client → `ColonyList` DTO, call ColonyMergeService with DTO
    - Migrate `CreateColonySummaryItem` to call typed client → `ColonySummary` DTO
    - Migrate `CreateColonyBuildingsItem` (both overloads) to call typed client → `ColonyBuildings` DTO
    - Remove all `JsonConvert.DeserializeObject` and envelope unwrapping
    - Add try/catch for `ApiHttpException` and `ApiBusinessException`
    - _Requirements: 2.3, 2.6, 9.1, 9.2, 9.4, 9.6, 13.1_
    - _Inputs: QueueSyncService.cs_
    - _Output: QueueSyncService.cs_
    - _Verification: getDiagnostics on QueueSyncService.cs_


  - [x] 3.6 Migrate QueueSyncService work items — ColonyWarehouse, ColonyWorkers
    - Migrate `CreateColonyWarehouseItem` (both overloads) to call typed client → `ColonyWarehouse` DTO
    - Migrate `CreateColonyWorkersItem` (both overloads) to call typed client → `ColonyWorkers` DTO
    - Call ColonyMergeService with generated DTOs
    - Remove all `JsonConvert.DeserializeObject` and envelope unwrapping
    - Add try/catch for `ApiHttpException` and `ApiBusinessException`
    - _Requirements: 2.3, 2.6, 9.1, 9.2, 9.4, 9.6_
    - _Inputs: QueueSyncService.cs_
    - _Output: QueueSyncService.cs_
    - _Verification: getDiagnostics on QueueSyncService.cs_

  - [x] 3.7 Migrate QueueSyncService work items — AssetLocations, AssetLocationDetail
    - Migrate `CreateAssetLocationsItem` to call typed client → `AssetLocations` DTO
    - Migrate `CreateAssetLocationDetailItem` to call typed client → `AssetLocationDetail` DTO
    - Call AssetMergeService with `ICollection<AssetCargoItem>` from DTO
    - Migrate `CascadeCargoDetailItems` to work with typed DTO collections
    - Remove all `JsonConvert.DeserializeObject` and envelope unwrapping
    - Add try/catch for `ApiHttpException` and `ApiBusinessException`
    - _Requirements: 2.3, 2.6, 9.1, 9.2, 9.4, 9.6_
    - _Inputs: QueueSyncService.cs_
    - _Output: QueueSyncService.cs_
    - _Verification: getDiagnostics on QueueSyncService.cs_

  - [x] 3.8 Migrate QueueSyncService work items — CrateDetail, BlueprintDetail, SurveyDetail
    - Migrate `CreateCrateDetailItem` to call typed client → `AssetCrateContents` DTO
    - Migrate `CreateBlueprintDetailItem` to call typed client → `AssetBlueprint` DTO
    - Migrate `CreateSurveyDetailItem` to call typed client → `AssetSurvey` DTO
    - Update `BuildScoringTemplate` to accept generated DTO instead of `GameApiBlueprintDetailResponse`
    - Update `BuildTempSurveyFromDetail` to accept generated DTO
    - Remove all `JsonConvert.DeserializeObject` and envelope unwrapping
    - _Requirements: 2.3, 2.6, 9.1, 9.2, 9.4, 9.6_
    - _Inputs: QueueSyncService.cs_
    - _Output: QueueSyncService.cs_
    - _Verification: getDiagnostics on QueueSyncService.cs_


  - [x] 3.9 Migrate QueueSyncService work items — KillMail, Mail
    - Migrate `CreateKillMailListItem` to call typed client → `KillMailList` DTO
    - Migrate `CreateKillMailDetailItem` to call typed client → `KillMail` DTO
    - Migrate `CreateMailListItem` to call typed client → `MailList` DTO
    - Remove all `JsonConvert.DeserializeObject` and raw JSON parsing
    - Add try/catch for `ApiHttpException` and `ApiBusinessException`
    - _Requirements: 2.3, 2.6, 9.1, 9.2, 9.4, 9.6_
    - _Inputs: QueueSyncService.cs_
    - _Output: QueueSyncService.cs_
    - _Verification: getDiagnostics on QueueSyncService.cs_

  - [x] 3.10 Remove QueueSyncService static JSON helpers and old imports
    - Remove or update `ResponseContainsBlueprints` (adapt to work with DTO collections)
    - Remove or update `BuildCrateImporterJson` (adapt to work with DTO)
    - Remove `TruncateForLog` if no longer used (was for logging raw JSON)
    - Remove all `using Newtonsoft.Json` and `GameApiServiceResponse` references
    - Remove `ThrowIfUnauthorizedAsync` / `ThrowIfRateLimited` if not already removed
    - _Requirements: 2.6, 9.6_
    - _Inputs: QueueSyncService.cs_
    - _Output: QueueSyncService.cs_
    - _Verification: getDiagnostics on QueueSyncService.cs_

- [x] 4. Checkpoint — Phase 3 Complete
  - Ensure all tests pass, ask the user if questions arise.


- [x] 5. Phase 3 continued: BankingService, MailService, GameApiConnectionMonitor
  - [x] 5.1 Migrate BankingService to use IGameApiTypedClient
    - Change `ImportTransactionsAsync` to accept `IGameApiTypedClient` instead of `GameApiClient`
    - Change `ImportBalanceAsync` to accept `IGameApiTypedClient` instead of `GameApiClient`
    - Remove explicit `accessToken` parameter (typed client manages tokens)
    - Remove all `JArray`/`JObject` parsing; use `BankingTransactions` and `BankingBalance` DTOs
    - Remove `GameApiServiceResponse` envelope unwrapping
    - Add `ApiHttpException` catch for 401 retry
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 14.3_
    - _Inputs: BankingService.cs, IGameApiTypedClient.cs_
    - _Output: BankingService.cs_
    - _Verification: getDiagnostics on BankingService.cs_

  - [x] 5.2 Migrate MailService to use IGameApiTypedClient
    - Change `SyncMailAsync` to accept `IGameApiTypedClient` instead of `GameApiClient`
    - Remove explicit `accessToken` parameter (typed client manages tokens)
    - Replace `JObject.Parse` mail list parsing with `MailList` DTO iteration
    - Replace `ParseMailFromDetail` with `MailBody` DTO field access
    - Add `ApiHttpException` catch: 401/403 → return -1, 429 → return -2
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 14.4_
    - _Inputs: MailService.cs, IGameApiTypedClient.cs_
    - _Output: MailService.cs_
    - _Verification: getDiagnostics on MailService.cs_

  - [x] 5.3 Migrate GameApiConnectionMonitor to use IGameApiTypedClient
    - Change constructor to accept `IGameApiTypedClient` instead of `GameApiClient`
    - Replace connectivity check with `IGameApiTypedClient.TestConnectionAsync`
    - Add `IGameApiTypedClient.IsCircuitOpen` check for circuit breaker state
    - Add `ApiHttpException` catch for 401 → `DisconnectedInvalidKey` transition
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 9.5_
    - _Inputs: GameApiConnectionMonitor.cs, IGameApiTypedClient.cs_
    - _Output: GameApiConnectionMonitor.cs_
    - _Verification: getDiagnostics on GameApiConnectionMonitor.cs_


- [x] 6. Phase 4: Legacy Schedulers
  - [x] 6.1 Migrate GameApiSyncScheduler to use IGameApiTypedClient
    - Change constructor to accept `IGameApiTypedClient` instead of `GameApiClient`
    - Update `SyncCharacterAsync` to call `ExchangeTokenAsync` and `GetCharacterAsync`
    - Replace `JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiProfileResponse>>` with DTO
    - Update all colony/asset sync methods to use typed client
    - Add `ApiHttpException` catch blocks
    - _Requirements: 10.1, 10.2, 10.3_
    - _Inputs: GameApiSyncScheduler.cs, IGameApiTypedClient.cs_
    - _Output: GameApiSyncScheduler.cs_
    - _Verification: getDiagnostics on GameApiSyncScheduler.cs_

  - [x] 6.2 Migrate ProductionSyncScheduler to pass IGameApiTypedClient to base
    - Change constructor to accept `IGameApiTypedClient` instead of `GameApiClient`
    - Pass `typedClient` to base class constructor
    - _Requirements: 10.4_
    - _Inputs: ProductionSyncScheduler.cs_
    - _Output: ProductionSyncScheduler.cs_
    - _Verification: getDiagnostics on ProductionSyncScheduler.cs_

- [x] 7. Checkpoint — All consumers migrated
  - Ensure all tests pass, ask the user if questions arise.
  - Build full solution with zero errors and zero warnings.


- [ ] 8. Phase 5: Legacy Client Cleanup
  - [x] 8.1 Delete GameApiClient.cs and GameApiServiceResponse envelope class
    - Delete `OE2EmpireTracker.Common/Client/GameApiClient.cs`
    - Delete the `GameApiServiceResponse<T>` envelope class file
    - Remove the old client's `SemaphoreSlim`-based rate limiter (dies with the class)
    - Verify no compile-time references remain to `GameApiClient`
    - _Requirements: 11.1, 11.3, 11.4, 11.5, 8.4_
    - _Inputs: GameApiClient.cs, GameApiServiceResponse file_
    - _Output: Deleted files_
    - _Verification: Full solution build with zero errors_

  - [x] 8.2 Delete hand-written response models (Profile, Colony, ColonySummary, Workers)
    - Delete `GameApiProfileResponse.cs`
    - Delete `GameApiColonyResponse.cs`
    - Delete `GameApiColonySummaryResponse.cs`
    - Delete `GameApiColonyWorkersResponse.cs`
    - Verify no compile-time references remain
    - _Requirements: 11.2, 11.4, 11.5_
    - _Inputs: Models/ folder_
    - _Output: Deleted files_
    - _Verification: Full solution build with zero errors_

  - [x] 8.3 Delete hand-written response models (Asset, Blueprint, Survey, Ship)
    - Delete `GameApiAssetResponse.cs`
    - Delete `GameApiBlueprintDetailResponse.cs`
    - Delete `GameApiSurveyResponse.cs`
    - Delete `GameApiShipConfigurationResponse.cs`
    - Delete `GameApiShipCargoResponse.cs`
    - Verify no compile-time references remain
    - _Requirements: 11.2, 11.4, 11.5_
    - _Inputs: Models/ folder_
    - _Output: Deleted files_
    - _Verification: Full solution build with zero errors_


  - [x] 8.4 Delete hand-written response models (Market, AcceptedJobs, Crate, SkillInTraining, Token)
    - Delete `GameApiAcceptedJobsResponse.cs`
    - Delete `GameApiMarketListingsResponse.cs`
    - Delete `GameApiMarketItemsResponse.cs`
    - Delete `GameApiMarketPriceStatsResponse.cs`
    - Delete `GameApiMarketOrdersResponse.cs`
    - Delete `GameApiMarketCompetitorsResponse.cs`
    - Delete `GameApiMarketShipComponentsResponse.cs`
    - Delete `GameApiCrateContentsResponse.cs`
    - Delete `GameApiSkillInTrainingResponse.cs`
    - Delete `GameApiTokenResponse.cs`
    - Verify no compile-time references remain
    - _Requirements: 11.2, 11.4, 11.5_
    - _Inputs: Models/ folder_
    - _Output: Deleted files_
    - _Verification: Full solution build with zero errors_

- [~] 9. Checkpoint — Cleanup complete
  - Ensure all tests pass, ask the user if questions arise.
  - Full solution build: zero errors, zero warnings.
  - Verify no references to old client or hand-written models remain in codebase.


- [ ] 10. Phase 6: Test Migration
  - [~] 10.1 Update QueueSyncService property tests for typed exception handling
    - Update `QueueSyncServicePropertyTests` to verify `ApiHttpException` catch patterns
    - Replace `result.Json == "401"` test patterns with typed exception assertions
    - Update `CrateContentImporterTests` to work with typed DTOs (or remove if JSON helpers deleted)
    - _Requirements: 12.4, 12.5_
    - _Inputs: QueueSyncServicePropertyTests.cs, CrateContentImporterTests.cs_
    - _Output: Updated test files_
    - _Verification: dotnet test / vstest.console pass_

  - [~] 10.2 Update ColonyMergeService tests for generated DTOs
    - Update `ColonyMergeBuildingsPreservationTests` to use generated DTOs (`ColonyBuildings`) instead of `GameApiColonyBuilding`
    - Update any test builders/generators to produce generated DTO instances
    - _Requirements: 12.3, 12.5_
    - _Inputs: ColonyMergeBuildingsPreservationTests.cs_
    - _Output: Updated test file_
    - _Verification: vstest.console pass_

  - [~] 10.3 Update GameApiSyncScheduler tests for typed client
    - Update `GameApiSyncSchedulerBankingTests` to construct `GameApiTypedClient` (or mock `IGameApiTypedClient`) instead of `GameApiClient` against HttpListener
    - Update `GameApiConnectionMonitor` construction in tests to pass typed client
    - _Requirements: 12.1, 12.2, 12.5_
    - _Inputs: GameApiSyncSchedulerBankingTests.cs_
    - _Output: Updated test file_
    - _Verification: vstest.console pass_

  - [~] 10.4 Write integration test verifying end-to-end typed client flow
    - Create a test that exercises `QueueSyncService.RunSyncAsync` with a mocked `IGameApiTypedClient`
    - Verify token exchange, work item execution, and merge service calls
    - Verify 401/429 error handling triggers correct behavior
    - _Requirements: 13.1, 13.5, 13.6_
    - _Inputs: QueueSyncService.cs, IGameApiTypedClient.cs_
    - _Output: New test file in OE2EmpireTracker.Tests/Services/_
    - _Verification: vstest.console pass_

- [~] 11. Final Checkpoint
  - Ensure all tests pass, ask the user if questions arise.
  - Full solution build: zero errors, zero warnings.
  - Run audit: `node .kiro/tools/audit.js` reports zero findings.
  - Verify behavioral equivalence: same sync results, same state transitions.


## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation after each phase
- QueueSyncService is split across 8 sub-tasks (3.1–3.10) due to its 30+ work item methods
- The phased approach ensures the codebase compiles after each task within a phase
- Phase 5 (cleanup) is deliberately last — all consumers must be migrated before deletion
- The design uses C# exclusively — no language selection was needed
- Rate limiter consolidation (Req 8) happens implicitly: the old SemaphoreSlim dies with GameApiClient
- Token management simplification (Req 14) happens as consumers drop explicit accessToken parameters

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "2.1"] },
    { "id": 2, "tasks": ["1.3", "2.2", "2.4"] },
    { "id": 3, "tasks": ["2.3", "2.5"] },
    { "id": 4, "tasks": ["3.1", "5.3"] },
    { "id": 5, "tasks": ["3.2", "5.1", "5.2", "6.1"] },
    { "id": 6, "tasks": ["3.3", "3.4", "6.2"] },
    { "id": 7, "tasks": ["3.5", "3.6"] },
    { "id": 8, "tasks": ["3.7", "3.8"] },
    { "id": 9, "tasks": ["3.9", "3.10"] },
    { "id": 10, "tasks": ["8.1"] },
    { "id": 11, "tasks": ["8.2", "8.3"] },
    { "id": 12, "tasks": ["8.4"] },
    { "id": 13, "tasks": ["10.1", "10.2", "10.3"] },
    { "id": 14, "tasks": ["10.4"] }
  ]
}
```
