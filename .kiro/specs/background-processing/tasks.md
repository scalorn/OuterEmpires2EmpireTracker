# Implementation Plan: Background Processing

## Overview

Add a background processing thread that automatically processes colony timers every 60 seconds, with per-colony locking for safe coexistence with manual Done buttons, cross-thread UI safety via `Control.Invoke`, and a "Next Process" countdown label in MainWindow. Implementation proceeds bottom-up: domain model changes first, then the processor class, then form thread-safety, then MainWindow integration, and finally wiring + tests.

## Tasks

- [x] 1. Add ProcessingLock and HasExpiredTimers to Colony
  - [x] 1.1 Add `ProcessingLock` property and `HasExpiredTimers()` method to `Colony.cs`
    - Add `[JsonIgnore] public object ProcessingLock { get; } = new object();` property
    - Add `[JsonIgnore] public bool HasExpiredTimers()` method that checks all structures for expired `BuildCompletionTime` (TimeRemaining <= 0) or expired `ProcessCompletionTime` (IntervalsPassed > 0, or non-repeating with TimeRemaining <= 0)
    - Add `using Newtonsoft.Json;` if not already present
    - Add `<Compile Include="Baseline\Colony.cs" />` is already in .csproj — no change needed
    - _Requirements: 2.2, 6.3_

  - [x] 1.2 Write property test for HasExpiredTimers predicate correctness
    - **Property 1: HasExpiredTimers predicate correctness**
    - **Validates: Requirements 2.2**
    - Create `OE2EmpireTracker.Tests/Baseline/BackgroundProcessorTests.cs`
    - Add `<Compile Include="Baseline\BackgroundProcessorTests.cs" />` to test .csproj
    - Use NUnit `[Test]` with 100 iterations of random colony/structure/timer states via `System.Random` with fixed seed
    - Assert `HasExpiredTimers()` matches manual per-structure check

  - [x] 1.3 Write property test for all-player colony scanning
    - **Property 2: All-player colony scanning**
    - **Validates: Requirements 1.2, 2.1**
    - In `BackgroundProcessorTests.cs`, generate colonyList with mixed OwnerUUIDs
    - Verify the set of colonies with `HasExpiredTimers() == true` is identical regardless of CurrentPlayerUUID

- [x] 2. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. Create BackgroundProcessor class
  - [x] 3.1 Implement `Baseline/BackgroundProcessor.cs`
    - Create `OE2EmpireTracker/Baseline/BackgroundProcessor.cs` implementing `IDisposable`
    - Use `System.Threading.Timer` for 60-second tick interval (`TickIntervalMs = 60_000`)
    - Add `ManualResetEventSlim _stopping` for graceful shutdown
    - Add `object _cycleLock` to prevent overlapping cycles
    - Accept `PlayerContext` in constructor
    - Implement `Start()`, `Stop()` (blocks until in-progress cycle completes), `Dispose()`
    - Expose `DateTime NextProcessTime { get; }` and `bool LastCycleHadError { get; }`
    - On each tick: scan `_playerContext.colonyList` for colonies where `HasExpiredTimers()` is true
    - For each such colony: `lock(colony.ProcessingLock)` then call `colony.ProcessColony()`, then fire `_playerContext.OnColonyDataChanged(colony.UUID)`
    - After all colonies processed: call `_playerContext.writeContext()` once if any were processed
    - Wrap cycle body in try/catch: log exceptions at Error level, set `LastCycleHadError` accordingly
    - If a single colony's `ProcessColony()` throws, log and continue to next colony
    - Use NLog logger for all logging (start, stop, cycle count, errors)
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 5.1, 5.2, 5.3, 7.1, 7.2, 7.3, 7.4_

  - [x] 3.2 Add `BackgroundProcessor.cs` to the main .csproj
    - Add `<Compile Include="Baseline\BackgroundProcessor.cs" />` to `OE2EmpireTracker.csproj`

  - [x] 3.3 Write property test for exact-match processing set
    - **Property 3: Exact-match processing set**
    - **Validates: Requirements 2.3, 2.4, 3.1**
    - In `BackgroundProcessorTests.cs`, generate random colonyList, run a processing cycle, verify exactly the colonies with `HasExpiredTimers() == true` were processed

  - [x] 3.4 Write property test for event firing with correct colony UUID
    - **Property 4: Event firing with correct colony UUID**
    - **Validates: Requirements 3.3**
    - In `BackgroundProcessorTests.cs`, generate colonyList with some expired, run cycle, verify `OnColonyDataChanged` fired exactly N times with correct UUIDs

  - [x] 3.5 Write property test for conditional persistence
    - **Property 5: Conditional persistence — exactly once or zero**
    - **Validates: Requirements 5.1, 5.2, 5.3**
    - In `BackgroundProcessorTests.cs`, generate colonyList, run cycle, verify `writeContext()` called exactly once if any colonies processed, zero otherwise

  - [x] 3.6 Write property test for error state round-trip
    - **Property 6: Error state round-trip**
    - **Validates: Requirements 8.5, 8.6**
    - In `BackgroundProcessorTests.cs`, simulate cycles with exceptions, verify `LastCycleHadError` reflects the outcome of the most recent cycle

- [x] 4. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Add per-colony lock to ColonyStructure Done button
  - [x] 5.1 Wrap `ProcessColony()` call in `ColonyStructure.cs` cmdDone_Click with `lock(Colony.ProcessingLock)`
    - In `OE2EmpireTracker/Forms/Colony/ColonyStructure.cs`, find the `cmdDone_Click` handler where `Colony.ProcessColony()` is called
    - Wrap the `ProcessColony()` call and subsequent event/save logic in `lock(Colony.ProcessingLock) { ... }`
    - _Requirements: 6.2, 6.3_

- [x] 6. Add cross-thread safety to form event handlers
  - [x] 6.1 Add InvokeRequired/Invoke wrapping to FormColony event handlers
    - In `FormColony.cs`, wrap `OnColonyDataChanged` with `InvokeRequired` check, `Invoke` call, and `ObjectDisposedException` catch
    - _Requirements: 4.1, 4.2, 4.3_

  - [x] 6.2 Add InvokeRequired/Invoke wrapping to FormColonyActivity event handlers
    - In `FormColonyActivity.cs`, wrap `OnColonyDataChanged` with the same pattern
    - _Requirements: 4.1, 4.2, 4.3_

  - [x] 6.3 Add InvokeRequired/Invoke wrapping to FormColonyDailyBuild event handlers
    - In `FormColonyDailyBuild.cs`, wrap `OnColonyDataChanged` with the same pattern
    - _Requirements: 4.1, 4.2, 4.3_

  - [x] 6.4 Add InvokeRequired/Invoke wrapping to FormBlueprint event handlers
    - In `FormBlueprint.cs`, wrap any PlayerContext event handlers (BlueprintDataChanged, etc.) with InvokeRequired/Invoke pattern
    - _Requirements: 4.1, 4.2, 4.3_

  - [x] 6.5 Add InvokeRequired/Invoke wrapping to FormSurvey event handlers
    - In `FormSurvey.cs`, wrap any PlayerContext event handlers (SurveyDataChanged, etc.) with InvokeRequired/Invoke pattern
    - _Requirements: 4.1, 4.2, 4.3_

  - [x] 6.6 Add InvokeRequired/Invoke wrapping to FormDeliveryRoute event handlers
    - In `FormDeliveryRoute.cs`, wrap any PlayerContext event handlers (DeliveryDataChanged, etc.) with InvokeRequired/Invoke pattern
    - _Requirements: 4.1, 4.2, 4.3_

  - [x] 6.7 Add InvokeRequired/Invoke wrapping to FormDeliveryExecution event handlers
    - In `FormDeliveryExecution.cs`, wrap any PlayerContext event handlers with InvokeRequired/Invoke pattern
    - _Requirements: 4.1, 4.2, 4.3_

  - [x] 6.8 Add InvokeRequired/Invoke wrapping to FormPlayerProfile event handlers
    - In `FormPlayerProfile.cs`, wrap any PlayerContext event handlers (PlayerProfileDataChanged, etc.) with InvokeRequired/Invoke pattern
    - _Requirements: 4.1, 4.2, 4.3_

- [x] 7. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 8. Integrate BackgroundProcessor into MainWindow
  - [x] 8.1 Add toolStripNextProcess label and timerNextProcess to MainWindow.Designer.cs
    - Add `private System.Windows.Forms.ToolStripLabel toolStripNextProcess;` field declaration
    - Add `private System.Windows.Forms.Timer timerNextProcess;` field declaration
    - Instantiate both in `InitializeComponent()`
    - Configure `toolStripNextProcess`: Alignment = Right, positioned to the left of `toolStripPlayerLabel` in the menuStrip1.Items array
    - Configure `timerNextProcess`: Interval = 1000
    - _Requirements: 8.1, 8.2_

  - [x] 8.2 Wire BackgroundProcessor lifecycle and countdown display in MainWindow.cs
    - Add `private BackgroundProcessor _backgroundProcessor;` field
    - In constructor after `InitializeComponent()`: create `_backgroundProcessor = new BackgroundProcessor(playerContext)`, call `_backgroundProcessor.Start()`, start `timerNextProcess`
    - Add named method `OnTimerNextProcessTick` for `timerNextProcess.Tick`: read `_backgroundProcessor.NextProcessTime`, compute remaining seconds, update `toolStripNextProcess.Text` to `"Next Process: Xd Yh Zm Ws"` format
    - If `_backgroundProcessor.LastCycleHadError`, set `toolStripNextProcess.ForeColor = Color.Red`; otherwise reset to default
    - If processor not running, display `"Next Process: --"`
    - In `OnFormClosed`: stop `timerNextProcess`, call `_backgroundProcessor.Stop()`, call `_backgroundProcessor.Dispose()`, then unsubscribe events
    - Add `using OE2EmpireTracker.Baseline;` if not present
    - _Requirements: 1.1, 1.3, 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_

- [x] 9. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests use NUnit `[Test]` with manual randomization (no FsCheck) — 100 iterations per test with `System.Random` and fixed seed
- The .csproj uses explicit `<Compile Include>` entries — new files must be added manually to both main and test .csproj files
- All forms must implement `IProgrammaticUpdateSource` and use `ProgrammaticUpdateGuard`
- Do NOT use anonymous lambdas for event subscriptions — use named methods
- All forms MUST unsubscribe from PlayerContext events in `OnFormClosed`
