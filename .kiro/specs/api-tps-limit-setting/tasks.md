# Implementation Plan: API TPS Limit Setting

## Overview

Implements a configurable TPS rate limit persisted in GameApiConnectionSettings, exposed via a ValidatedTextBox on the Preferences Form, and consumed by a new GameApiRequestQueue service (token-bucket rate governor, cascading work, adaptive 429 handling). The discovery test fixture is restructured from 23 sequential tests into a single queue-driven parallel run.

## Tasks

- [ ] 1. Add Tps property to GameApiConnectionSettings
  - [x] 1.1 Add Tps property with default value 0.5
    - Add `public double Tps { get; set; } = 0.5;` to GameApiConnectionSettings
    - Verify PascalCase serialization matches existing properties (ServerUrl, PollingIntervalMinutes)
    - Verify deserialization from JSON without Tps key initializes to 0.5
    - _Requirements: 1.1, 1.2, 1.3, 1.4_
    - _Verification: Build succeeds, getDiagnostics clean_

- [x] 2. Add TPS input to Preferences Form
  - [x] 2.1 Add txtTpsLimit ValidatedTextBox and label to Game API tab
    - Add `lblTpsLimit` (Label, Text="TPS Limit:") and `txtTpsLimit` (ValidatedTextBox) to Designer.cs
    - Set ValidationPattern to `^\d{1,3}(\.\d)?$`
    - Position below polling interval controls
    - _Requirements: 2.1, 2.4, 3.4, 3.5_
    - _Verification: Build succeeds, getDiagnostics clean on Designer.cs_

  - [x] 2.2 Populate txtTpsLimit from settings and save on OK
    - In PopulateGameApiFields: `txtTpsLimit.Text = settings.Tps.ToString("F1")`
    - In SaveGameApiSettings: `settings.Tps = double.Parse(txtTpsLimit.Text)`
    - Wire OK button to persist current value
    - _Requirements: 2.2, 2.3, 2.4_
    - _Verification: Build succeeds, getDiagnostics clean_

  - [x] 2.3 Add Leave handler — clamp to [0.1, 100.0] or revert
    - On txtTpsLimit Leave: if numeric but out of range, clamp to nearest boundary
    - If non-numeric or empty, revert to last valid value
    - _Requirements: 2.5, 3.1, 3.2, 3.3_
    - _Verification: Build succeeds, getDiagnostics clean_

- [~] 3. Checkpoint — Settings and UI
  - Ensure all tests pass, ask the user if questions arise.


- [ ] 4. Create WorkItem and supporting types
  - [x] 4.1 Create WorkItem.cs with WorkItem, QueueError, QueueCompletionStatus
    - Create `OE2EmpireTracker.Common/Services/WorkItem.cs`
    - Define WorkItem (Label, ExecuteAsync delegate), QueueError, QueueCompletionStatus
    - Namespace: OE2EmpireTracker.Services
    - _Requirements: 4.5, 6.2, 6.4_
    - _Verification: Build succeeds_

- [x] 5. Implement GameApiRequestQueue — core structure and dispatch loop
  - [x] 5.1 Create GameApiRequestQueue.cs with constructor, public API, and dispatch loop
    - Create `OE2EmpireTracker.Common/Services/GameApiRequestQueue.cs`
    - Public: constructor(configuredTps, perItemTimeout), EnqueueAsync, Start, DrainAsync, NotifyRateLimited
    - Properties: ConfiguredTps, EffectiveTps, CompletionStatus, Errors
    - Internal: ConcurrentQueue, dispatch loop (dequeue + fire-and-forget), work item execution with cascading
    - Thread safety: ConcurrentQueue for items, ConcurrentBag for errors
    - _Requirements: 4.1, 4.2, 4.3, 4.5_
    - _Verification: Build succeeds_

  - [x] 5.2 Implement CompletionTracker (private nested class)
    - Track pending, inflight, succeeded, failed counts with Interlocked operations
    - DrainAsync via TaskCompletionSource — completes when pending==0 && inflight==0
    - OnEnqueued, OnDispatched, OnCompleted, OnCascadedEnqueued methods
    - _Requirements: 5.3, 6.4_
    - _Verification: Build succeeds_

  - [x] 5.3 Implement TokenBucketGovernor (private nested class)
    - Token bucket: capacity = configuredTps, refill rate = effectiveTps tokens/sec
    - AcquireTokenAsync blocks via SemaphoreSlim until token available
    - Burst capacity = 1 second of accumulated tokens
    - _Requirements: 7.1, 7.2, 7.3_
    - _Verification: Build succeeds_

  - [x] 5.4 Implement thread-safe enqueue (multiple threads may enqueue concurrently)
    - EnqueueAsync must be safe for concurrent callers
    - Updates CompletionTracker atomically
    - Returns Task that completes when the work item finishes
    - _Requirements: 4.4_
    - _Verification: Build succeeds_

- [x] 6. Implement GameApiRequestQueue — cascading work
  - [x] 6.1 Implement cascading work item support
    - Work items return IReadOnlyList<WorkItem> from ExecuteAsync
    - On completion, returned items are enqueued at back of queue
    - Cascaded items subject to same rate governor as direct enqueues
    - CompletionTracker updated for cascaded items before drain check
    - _Requirements: 5.1, 5.2, 5.4_
    - _Verification: Build succeeds_


- [x] 7. Implement GameApiRequestQueue — error handling and cancellation
  - [x] 7.1 Implement error isolation (catch per-item exceptions, continue processing)
    - Catch exceptions in ExecuteWorkItemAsync, record in ConcurrentBag<QueueError>
    - Mark item as failed, continue dispatch loop
    - _Requirements: 6.1, 6.2_
    - _Verification: Build succeeds_

  - [x] 7.2 Implement cancellation and per-item timeout
    - Accept CancellationToken in Start — when cancelled, stop dequeuing new items
    - Per-item timeout via CancellationTokenSource.CreateLinkedTokenSource + CancelAfter
    - In-flight items allowed to complete up to timeout
    - _Requirements: 6.3_
    - _Verification: Build succeeds_

- [x] 8. Implement AdaptiveRateController
  - [x] 8.1 Implement 429 pause and TPS reduction
    - NotifyRateLimited: pause dispatch for retryAfter seconds (default 60s if no header)
    - After pause: reduce effective TPS to 50% of active rate
    - Re-enqueue the 429 work item at back of queue for retry
    - _Requirements: 17.1, 17.2, 17.7_
    - _Verification: Build succeeds_

  - [x] 8.2 Implement exponential backoff on consecutive 429s
    - Double pause duration on consecutive 429s (2x, 4x, 8x), cap at 5 minutes
    - Track consecutive backoff count, reset on successful processing
    - _Requirements: 17.3_
    - _Verification: Build succeeds_

  - [x] 8.3 Implement recovery ramp after successful processing
    - After 60s without 429: increase effective TPS by 10% per minute
    - Stop increasing when effective TPS reaches configured TPS
    - Expose EffectiveTps property, log each rate adjustment
    - _Requirements: 17.4, 17.5, 17.6_
    - _Verification: Build succeeds_

- [~] 9. Checkpoint — Queue service complete
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 10. Queue unit tests — basic dispatch
  - [-] 10.1 Test single item dispatch and FIFO ordering
    - Enqueue_SingleItem_Dispatches: basic enqueue → execute → drain cycle
    - Enqueue_MultipleItems_FIFO: dispatch order matches enqueue order
    - _Requirements: 4.1, 4.3_
    - _Verification: Tests pass_

  - [-] 10.2 Test cascading items and drain completeness
    - CascadingItems_AreProcessed: cascaded items enqueued and executed
    - DrainAsync_WaitsForAllCascades: drain blocks until nested cascades finish
    - _Requirements: 5.1, 5.3_
    - _Verification: Tests pass_

  - [-] 10.3 Test error isolation and cancellation
    - FailedItem_DoesNotBlockOthers: exception in one item, others complete
    - Cancellation_StopsNewDispatch: after cancel, no new items dispatched
    - Cancellation_InflightItemsTimeout: in-flight items cancelled after timeout
    - _Requirements: 6.1, 6.3_
    - _Verification: Tests pass_


- [ ] 11. Queue unit tests — rate limiting and adaptive behavior
  - [~] 11.1 Test 429 pause and recovery
    - NotifyRateLimited_PausesDispatch: no dispatch during pause window
    - ConsecutiveBackoffs_DoubleDuration: multiple 429s increase pause duration
    - Recovery_RampsBack: after backoff, TPS increases 10%/min
    - _Requirements: 17.1, 17.3, 17.4_
    - _Verification: Tests pass_

  - [~] 11.2 Write TokenBucketGovernor unit tests
    - AcquireToken_RespectsTpsRate: tokens dispensed at configured rate
    - BurstCapacity_EqualsOneSecondOfTps: accumulated tokens cap at TPS value
    - EffectiveTpsChange_AffectsRefillRate: reducing TPS slows token generation
    - _Requirements: 7.1, 7.3_
    - _Verification: Tests pass_

  - [~] 11.3 Write AdaptiveRateController unit tests
    - OnRateLimited_SetsPause: IsPaused true for duration
    - OnRateLimited_ReducesTps: EffectiveTps = 50% of prior
    - ConsecutiveBackoffs_CapsAt5Min: pause doubles but caps at 300s
    - Recovery_StopsAtConfiguredTps: does not exceed configured value
    - _Requirements: 17.1, 17.2, 17.3_
    - _Verification: Tests pass_

- [ ] 12. Property-based tests for GameApiRequestQueue
  - [~] 12.1 Property P1: Rate Governor Throughput
    - Random TPS [0.5, 20.0], random item count [5, 50], frozen SystemClock
    - Verify dispatch count per 5s window does not exceed configuredTps * 5 + configuredTps
    - **Property 1: Rate Governor Throughput**
    - **Validates: Requirements 7.1, 7.2, 7.3**
    - _Verification: Property test passes with MaxTest=100_

  - [~] 12.2 Property P2: FIFO Ordering
    - Random item count [2, 100], verify dispatch sequence numbers monotonically increasing
    - **Property 2: FIFO Ordering**
    - **Validates: Requirements 4.3**
    - _Verification: Property test passes with MaxTest=100_

  - [~] 12.3 Property P3: Cascading Completeness
    - Random tree depth [1, 3], random branching [0, 3], atomic counter
    - Verify total processed == counter after drain
    - **Property 3: Cascading Completeness**
    - **Validates: Requirements 5.1, 5.2, 5.3**
    - _Verification: Property test passes with MaxTest=100_

  - [~] 12.4 Property P4: Error Isolation
    - Random failure positions in item list
    - Verify succeeded + failed == total
    - **Property 4: Error Isolation**
    - **Validates: Requirements 6.1, 6.2**
    - _Verification: Property test passes with MaxTest=100_

  - [~] 12.5 Property P5: 429 Backoff Correctness
    - Random retryAfter [1, 120], frozen clock, advance incrementally
    - Verify no dispatch during pause window, effective TPS ≤ 50% of prior
    - **Property 5: 429 Backoff Correctness**
    - **Validates: Requirements 17.1, 17.2, 17.3**
    - _Verification: Property test passes with MaxTest=100_

  - [~] 12.6 Property P6: Drain Completeness
    - Random delays [0, 50ms], random cascading
    - Verify inflight == 0 after drain
    - **Property 6: Drain Completeness**
    - **Validates: Requirements 5.3, 6.4**
    - _Verification: Property test passes with MaxTest=100_

- [~] 13. Checkpoint — Queue tests complete
  - Ensure all tests pass, ask the user if questions arise.


- [ ] 14. Restructure discovery tests — scaffold
  - [~] 14.1 Delete old 23 ordered tests and create single [Test] method skeleton
    - Remove all existing `[Test] [Order(N)]` methods from GameApiFullDiscoveryTests
    - Create single `[Test]` method that creates GameApiRequestQueue at TPS=10.0
    - Add queue.Start() and await queue.DrainAsync() structure
    - _Requirements: 8.1, 8.3_
    - _Verification: Build succeeds, single test method compiles_

  - [~] 14.2 Add thread-safe result collection helpers
    - Replace `List<EndpointResult>` with `ConcurrentBag<EndpointResult>`
    - Add RecordSuccess and RecordSkipped helper methods (thread-safe)
    - _Requirements: 15.4_
    - _Verification: Build succeeds_

- [ ] 15. Enqueue seed work items (independent endpoints)
  - [~] 15.1 Enqueue character, banking, jobs, ship seed items
    - character/profile, character/skills, banking/balance, banking/transactions-p0
    - jobs/accepted, ship/configuration, ship/cargo
    - Each writes response to appropriate subdirectory
    - _Requirements: 8.2_
    - _Verification: Build succeeds_

  - [~] 15.2 Enqueue market seed items
    - market/listings, market/items, market/buyorders, market/sellorders
    - Each writes response to market/ subdirectory
    - _Requirements: 8.2_
    - _Verification: Build succeeds_

  - [~] 15.3 Enqueue colonies/list, assets/locations, killmails/list, mail/list-p0
    - These are seeds that will cascade to follow-up items
    - Each writes its direct response and prepares for cascading
    - _Requirements: 8.2_
    - _Verification: Build succeeds_

- [ ] 16. Implement colony cascading
  - [~] 16.1 Colony list cascades to per-colony detail items
    - On colony list completion: enqueue 4 items per colony (summary, buildings, warehouse, workers)
    - Each writes to `colonies/{colonyId}/{endpoint}.json`
    - Calls RecordSuccess/RecordSkipped independently
    - Handle zero colonies gracefully (no follow-ups)
    - _Requirements: 9.1, 9.2, 9.3_
    - _Verification: Build succeeds_

- [ ] 17. Implement asset cascading
  - [~] 17.1 Asset locations cascades to per-location detail
    - On locations completion: enqueue one item per location for detail
    - If enqueue throws, fail entire discovery run
    - _Requirements: 10.1_
    - _Verification: Build succeeds_

  - [~] 17.2 Location detail cascades to crate/survey/blueprint items
    - Inspect cargo array for typeC "Cr", "Sc", "Bp"
    - Enqueue detail fetch for ALL matching items from ALL locations
    - Each writes to `assets/{type}-{id}.json`, calls RecordSuccess/RecordSkipped
    - _Requirements: 10.2, 10.3, 10.4_
    - _Verification: Build succeeds_


- [ ] 18. Implement kill mail and mail cascading
  - [~] 18.1 Kill mail list cascades to per-mail detail
    - On kill mail list completion: enqueue one item per kill mail for detail
    - Each writes to `killmails/{killMailId}.json`, calls RecordSuccess/RecordSkipped
    - _Requirements: 11.1, 11.2_
    - _Verification: Build succeeds_

  - [~] 18.2 Mail pagination and per-mail detail cascading
    - Page 0 seed: if non-empty, enqueue next page (sequential chain)
    - Each page enqueues one detail item per mail ID on that page
    - Detail items execute concurrently with pagination and other work
    - Each detail writes to `mail/{mailId}.json`, calls RecordSuccess/RecordSkipped
    - _Requirements: 12.1, 12.2, 12.3, 12.4_
    - _Verification: Build succeeds_

- [ ] 19. Implement market and banking cascading
  - [~] 19.1 Market cascading (listings → prices/components, orders → competitors)
    - market/listings completion: enqueue prices (first listing type/typeId) + ship components (first ship marketId)
    - market/buyorders completion: enqueue buy competitors (first 5 marketIds)
    - market/sellorders completion: enqueue sell competitors (first 5 marketIds)
    - _Requirements: 13.1, 13.2, 13.3, 13.4_
    - _Verification: Build succeeds_

  - [~] 19.2 Banking transaction pagination
    - banking/transactions-p0 seed: if non-empty, enqueue next page (sequential chain)
    - After drain: combine all transaction pages into `banking/transactions-all.json`
    - _Requirements: 14.1, 14.2, 14.3_
    - _Verification: Build succeeds_

- [~] 20. Checkpoint — Cascading work items complete
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 21. Discovery tests — drain, metadata, and error reporting
  - [~] 21.1 Implement drain and metadata writing
    - Await queue.DrainAsync()
    - Write `_metadata.json` with: run timestamp, character ID, total succeeded/skipped/failed counts
    - Include per-endpoint detail (category, endpoint, success, httpStatus, reason)
    - Output directory structure unchanged (character/, colonies/, banking/, assets/, jobs/, killmails/, mail/, ship/, market/)
    - _Requirements: 15.1, 15.2, 15.5_
    - _Verification: Build succeeds_

  - [~] 21.2 Implement error collection and failure isolation
    - Collect errors from queue.Errors list, report as RecordSkipped entries with exception message
    - Failed seed items do not abort other seeds — each seed independent
    - Discovery run writes metadata and completes even if all seeds fail
    - Only queue infrastructure failure (not work item failure) aborts the run
    - _Requirements: 15.3, 15.6_
    - _Verification: Build succeeds_

- [ ] 22. Verify GameApiClient preservation
  - [~] 22.1 Verify internal rate limiter and 429 handling remain unchanged
    - Confirm SemaphoreSlim rate limiter (AcquireRateLimitTokenAsync) is untouched
    - Confirm X-RateLimit-Limit header handling unchanged
    - Confirm 429 pause mechanism unchanged
    - Queue operates above client — dual-layer rate limiting by design
    - _Requirements: 16.1, 16.2, 16.3, 16.4, 16.5_
    - _Verification: getDiagnostics clean, no modifications to GameApiClient_

- [~] 23. Final checkpoint — all integration complete
  - Ensure all tests pass, ask the user if questions arise.


## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The queue service (tasks 4-8) is independent of the discovery test restructuring (tasks 14-21)
- GameApiClient (task 22) requires no code changes — it's a verification step
- FsCheck 2.16.6 API: use `[FsCheck.NUnit.Property(MaxTest = 100)]`, LINQ generators, no FsCheck.Fluent

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "4.1"] },
    { "id": 1, "tasks": ["2.1", "5.1", "5.2", "5.3"] },
    { "id": 2, "tasks": ["2.2", "5.4", "6.1"] },
    { "id": 3, "tasks": ["2.3", "7.1", "7.2"] },
    { "id": 4, "tasks": ["8.1", "8.2"] },
    { "id": 5, "tasks": ["8.3"] },
    { "id": 6, "tasks": ["10.1", "10.2", "10.3"] },
    { "id": 7, "tasks": ["11.1", "11.2", "11.3"] },
    { "id": 8, "tasks": ["12.1", "12.2", "12.3", "12.4", "12.5", "12.6"] },
    { "id": 9, "tasks": ["14.1"] },
    { "id": 10, "tasks": ["14.2"] },
    { "id": 11, "tasks": ["15.1", "15.2", "15.3"] },
    { "id": 12, "tasks": ["16.1", "17.1"] },
    { "id": 13, "tasks": ["17.2", "18.1", "18.2"] },
    { "id": 14, "tasks": ["19.1", "19.2"] },
    { "id": 15, "tasks": ["21.1"] },
    { "id": 16, "tasks": ["21.2", "22.1"] }
  ]
}
```
