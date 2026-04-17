# Implementation Plan: Data Model Thread Safety

## Overview

Introduce layered synchronization to the OE2EmpireTracker data model so that BackgroundProcessor, ColonyStatusCalculator, and multiple FormColonyV2 instances can safely share mutable colony data. Implementation proceeds bottom-up: collection-level locks first (no API changes), then Colony.ColonyLock (replacing ProcessingLock), then PlayerContext._listLock, then BackgroundProcessor coordination, then FormColonyV2 background calculation, and finally thread-safety tests.

## Tasks

- [x] 1. Add collection-level _syncRoot locks (ItemBag, PropertyBag, LockTracking)
  - [x] 1.1 Add _syncRoot to ItemBag and wrap all public methods
    - Add `private readonly object _syncRoot = new object();` field to `Models/ItemBag.cs`
    - Wrap `AddItem`, `Remove`, `Clear` in `lock(_syncRoot)` (write operations)
    - Wrap `FindByType`, `FindResource`, `CountByType`, `ContainsKey`, `Count` in `lock(_syncRoot)` (read operations)
    - `EnsureTypeIndex` and `EnsureResourceIndex` are called within the lock scope, no separate lock needed
    - `FindByType` and `FindResource` already return defensive copies (`new List<Item>(list)`) — verify this is preserved
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

  - [x] 1.2 Add _syncRoot to PropertyBag and wrap all public methods
    - Add `private readonly object _syncRoot = new object();` field to `Models/PropertyBag.cs`
    - Wrap `setProperty` (all overloads), `Remove`, `Clear` in `lock(_syncRoot)` (write operations)
    - Wrap `getDecimal`, `getLong`, `getBoolean`, `getString`, `ContainsKey`, `Count` in `lock(_syncRoot)` (read operations)
    - _Requirements: 3.1, 3.2, 3.3_

  - [x] 1.3 Add _syncRoot to LockTracking and wrap all public methods
    - Add `private readonly object _syncRoot = new object();` field to `Models/LockTracking.cs`
    - Wrap `LockItem`, `LockItems`, `ClearLocksForProcess` in `lock(_syncRoot)` (write operations)
    - Wrap `GetLockedQuantity`, `GetLocksForProcess` in `lock(_syncRoot)` (read operations)
    - `GetLocksForProcess` already returns `.ToList().AsReadOnly()` — verify this is preserved
    - _Requirements: 4.1, 4.2, 4.3, 4.4_

- [x] 2. Checkpoint — Verify collection-level locks compile cleanly
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. Replace Colony.ProcessingLock with Colony.ColonyLock (ReaderWriterLockSlim)
  - [x] 3.1 Add ColonyLock and timeout constants to Colony.cs
    - Add `using System.Threading;` to `Models/Colony.cs`
    - Replace `[JsonIgnore] public object ProcessingLock { get; } = new object();` with `[JsonIgnore] public ReaderWriterLockSlim ColonyLock { get; } = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);`
    - Add `public const int ReadLockTimeoutMs = 1000;` and `public const int WriteLockTimeoutMs = 5000;`
    - _Requirements: 1.1, 1.2, 1.6_

  - [x] 3.2 Update BackgroundProcessor.ExecuteCycle to use ColonyLock
    - In `Services/BackgroundProcessor.cs`, replace `lock (colony.ProcessingLock)` with `colony.ColonyLock.TryEnterWriteLock(Colony.WriteLockTimeoutMs)` / `try` / `finally { colony.ColonyLock.ExitWriteLock(); }`
    - If `TryEnterWriteLock` returns false, log a warning and `continue` to skip the colony
    - Ensure `OnColonyDataChanged` is fired OUTSIDE the ColonyLock (already the case)
    - Ensure `WriteContext` is called OUTSIDE any ColonyLock (already the case)
    - _Requirements: 1.3, 1.6, 1.7, 7.1, 7.4, 8.1, 11.2, 11.3_

  - [x] 3.3 Update ColonyStructureV2 Done button to use ColonyLock
    - In `Forms/ColonyV2/ColonyStructureV2.cs`, replace both `lock (Colony.ProcessingLock)` blocks with `Colony.ColonyLock.TryEnterWriteLock(Colony.WriteLockTimeoutMs)` / `try` / `finally { Colony.ColonyLock.ExitWriteLock(); }`
    - If lock timeout, log warning and return without processing
    - _Requirements: 1.3, 1.6, 1.7, 10.1_

  - [x] 3.4 Update ColonyStructure (V1) Done button to use ColonyLock
    - In `Forms/Colony/ColonyStructure.cs`, replace both `lock (Colony.ProcessingLock)` blocks with `Colony.ColonyLock.TryEnterWriteLock(Colony.WriteLockTimeoutMs)` / `try` / `finally { Colony.ColonyLock.ExitWriteLock(); }`
    - If lock timeout, log warning and return without processing
    - _Requirements: 1.3, 1.6, 1.7_

- [x] 4. Checkpoint — Verify ColonyLock migration compiles and tests pass
  - Ensure all tests pass, ask the user if questions arise.


- [x] 5. Add PlayerContext._listLock and SnapshotColonyList
  - [x] 5.1 Add _listLock field and wrap cache methods
    - Add `private readonly object _listLock = new object();` field to `Services/PlayerContext.cs`
    - Wrap `FindBlueprint` cache rebuild in `lock(_listLock)` — acquire lock, check/rebuild `_blueprintCache`, lookup, release lock, then fall back to global blueprints outside the lock
    - Wrap `FindSurvey` cache rebuild in `lock(_listLock)` — same pattern
    - Wrap `FindColony` cache rebuild in `lock(_listLock)` — same pattern
    - Wrap `InvalidateBlueprintCache`, `InvalidateSurveyCache`, `InvalidateColonyCache` in `lock(_listLock)` before setting cache to null
    - _Requirements: 5.1, 5.3, 5.5_

  - [x] 5.2 Add SnapshotColonyList method to PlayerContext
    - Add `public List<Colony> SnapshotColonyList()` that acquires `lock(_listLock)` and returns `new List<Colony>(ColonyList)`
    - This is used by BackgroundProcessor to safely iterate colonies
    - _Requirements: 5.2_

  - [x] 5.3 Wrap WriteContext serialization snapshot in _listLock
    - In `PlayerContext.WriteContext()`, acquire `lock(_listLock)` to snapshot all lists into a `PlayerRoot` object
    - Release the lock before calling `JsonConvert.SerializeObject` and `SafeFileWriter.WriteAllText`
    - _Requirements: 5.4_

  - [x] 5.4 Update BackgroundProcessor to use SnapshotColonyList
    - In `Services/BackgroundProcessor.cs`, replace `new List<Colony>(_playerContext.ColonyList)` with `_playerContext.SnapshotColonyList()`
    - _Requirements: 5.2, 7.4, 8.1_

- [x] 6. Checkpoint — Verify PlayerContext._listLock compiles and tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Add background calculation with cancellation to FormColonyV2
  - [x] 7.1 Add CancellationTokenSource and generation counter fields
    - Add `private CancellationTokenSource _calcCts;` and `private int _calcGeneration = 0;` fields to `Forms/ColonyV2/FormColonyV2.cs`
    - Add `using System.Threading;` if not already present
    - _Requirements: 6.2_

  - [x] 7.2 Refactor colony selection to show identity immediately and queue background work
    - In `lvwColonies_ItemSelectionChanged`, cancel any previous `_calcCts` and create a new one
    - Increment `_calcGeneration` via `Interlocked.Increment`
    - Immediately display PlanetName, ColonyName, SystemName on the UI thread
    - Show a "Calculating..." indicator (e.g. disable status panel or show label)
    - Queue `RecalculateStatus` + `PopulateForm` onto `ThreadPool.QueueUserWorkItem`
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

  - [x] 7.3 Implement background calculation with ColonyLock and BeginInvoke
    - In the ThreadPool callback: check `cts.IsCancellationRequested` before starting
    - Acquire `colony.ColonyLock.TryEnterWriteLock(Colony.WriteLockTimeoutMs)` for `RecalculateStatus` (which calls CalculateBuilt/CalculateIdeal)
    - Release write lock in finally block
    - Check cancellation again after calculation
    - Marshal results to UI thread via `BeginInvoke`, checking generation counter and cancellation token before applying
    - Catch `ObjectDisposedException` and `InvalidOperationException` from `BeginInvoke`
    - _Requirements: 6.5, 6.6, 6.8, 9.1, 9.2_

  - [x] 7.4 Cancel background calculation on form close
    - In `OnFormClosed`, call `_calcCts?.Cancel()` before existing cleanup
    - _Requirements: 6.7_

  - [x] 7.5 Add read lock acquisition for colony data display
    - In `PopulateStructures`, `PopulateItemGrid`, `PopulateCommodityRequestGrid`, and `RefreshAdminReport`: acquire `colony.ColonyLock.TryEnterReadLock(Colony.ReadLockTimeoutMs)` to snapshot collections before populating UI controls
    - If read lock times out, log warning and display stale data
    - Release read lock in finally block before populating UI controls from the snapshot
    - _Requirements: 1.4, 10.2, 11.1_

  - [x] 7.6 Add write lock acquisition for colony mutation operations
    - In `cmdSave_Click`, `cmdAddFlatpack_Click`, `cmdDelete_Click`, and other mutation handlers: acquire `colony.ColonyLock.TryEnterWriteLock(Colony.WriteLockTimeoutMs)` before mutating colony state
    - Release write lock before calling `PlayerContext.WriteContext()` and `OnColonyDataChanged` (lock ordering)
    - Fire `OnColonyDataChanged` after save so other open forms refresh
    - _Requirements: 10.1, 10.3, 10.4, 8.1_

- [x] 8. Checkpoint — Verify FormColonyV2 background calculation compiles and tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 9. Thread-safety tests
  - [x] 9.1 Write concurrent colony processing test (Property 1)
    - **Property 1: Concurrent colony processing safety**
    - Create a colony with structures, items, and locks
    - Run `BackgroundProcessor.RunCycleOnce()` and `ColonyStatusCalculator.CalculateBuilt()` on separate threads, both acquiring `ColonyLock`
    - Assert no exceptions thrown and colony data remains internally consistent
    - **Validates: Requirements 1.3, 1.5, 9.1, 12.1**

  - [x] 9.2 Write concurrent ItemBag access test (Property 2)
    - **Property 2: ItemBag concurrent access safety**
    - Generate random items, spawn N threads doing concurrent `AddItem` and `FindByType`
    - Assert no exceptions and final item count matches expected
    - **Validates: Requirements 2.2, 2.3, 12.2**

  - [x] 9.3 Write ItemBag defensive copy test (Property 3)
    - **Property 3: ItemBag defensive copies**
    - Add items, call `FindByType`, modify the returned list, call `FindByType` again
    - Assert results are unchanged by the modification
    - **Validates: Requirements 2.5**

  - [x] 9.4 Write concurrent PropertyBag access test (Property 4)
    - **Property 4: PropertyBag concurrent access safety**
    - Generate random property names/values, spawn N threads doing concurrent `setProperty` and `getBoolean`
    - Assert no exceptions
    - **Validates: Requirements 3.2, 3.3, 12.3**

  - [x] 9.5 Write concurrent LockTracking access test (Property 5)
    - **Property 5: LockTracking concurrent access safety**
    - Generate random process UUIDs and item keys, spawn N threads doing concurrent `LockItem` and `GetLockedQuantity`
    - Assert no exceptions and quantities are non-negative
    - **Validates: Requirements 4.2, 4.3, 12.4**

  - [x] 9.6 Write LockTracking defensive copy test (Property 6)
    - **Property 6: LockTracking defensive copies**
    - Lock items for a process, get locks via `GetLocksForProcess`, verify the returned collection is read-only
    - **Validates: Requirements 4.4**

  - [x] 9.7 Write cancellation prevents stale results test (Property 7)
    - **Property 7: Cancellation prevents stale results**
    - Create a `CancellationTokenSource` and generation counter, simulate rapid colony switches by incrementing generation and cancelling token
    - Assert only the final generation's callback executes
    - **Validates: Requirements 6.2, 6.6, 12.5**

  - [x] 9.8 Write read lock timeout test (Property 8)
    - **Property 8: Read lock timeout graceful degradation**
    - Hold a write lock on a colony, attempt a read lock with 1000ms timeout from another thread
    - Assert `TryEnterReadLock` returns false and colony data is unchanged
    - **Validates: Requirements 11.1, 12.6**

  - [x] 9.9 Write write lock timeout test (Property 9)
    - **Property 9: Write lock timeout skips colony without corruption**
    - Hold a write lock on a colony, attempt another write lock with 5000ms timeout
    - Assert `TryEnterWriteLock` returns false and colony data is unchanged
    - **Validates: Requirements 11.2, 12.6**

  - [x] 9.10 Write BackgroundProcessor continues after skip test (Property 10)
    - **Property 10: BackgroundProcessor continues after contended colony**
    - Create multiple colonies, hold the write lock on one, run `BackgroundProcessor.RunCycleOnce()`
    - Assert the locked colony is skipped and others with expired timers are processed
    - **Validates: Requirements 11.3**

- [-] 10. Final checkpoint — Full test suite and manual verification
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Implementation proceeds bottom-up through the lock hierarchy: collection locks → ColonyLock → _listLock → coordination → UI
- Collection-level locks (task 1) are the safest change — no API changes, no callers affected
- ColonyLock migration (task 3) requires updating all `lock(ProcessingLock)` callers: BackgroundProcessor, ColonyStructureV2 (2 sites), ColonyStructure V1 (2 sites)
- Lock ordering convention: `_listLock` → `ColonyLock` → `_syncRoot` (never reversed)
- Events are always fired outside all locks to prevent deadlocks
- WriteContext is always called outside ColonyLock to respect lock ordering
- Property tests validate universal thread-safety invariants under concurrent stress
- ColonyStatusCalculator itself is unchanged — the caller contract is enforced by lock acquisition at call sites
