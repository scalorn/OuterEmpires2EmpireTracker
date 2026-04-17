# Requirements Document

## Introduction

This specification defines the thread-safety and synchronization strategy for the OE2EmpireTracker data model. The application currently has a background processing timer (`BackgroundProcessor`) that mutates colony data on a `ThreadPool` thread while the UI thread reads and writes the same data through WinForms forms. Collections such as `ItemBag`, `PropertyBag`, `LockTracking`, and `BindingList<T>` caches in `PlayerContext` have no synchronization. `ColonyStatusCalculator.CalculateBuilt()` mutates `colony.Locks` and `structure.Statuses` in-place. Multiple MDI child forms can be open simultaneously, all sharing the same `PlayerContext` singleton. This spec introduces a per-colony `ReaderWriterLockSlim` strategy, thread-safe collection wrappers, a cancellable background UI update pattern for the colony form, and coordination protocols between background processors and UI forms.

## Glossary

- **Colony**: A player-owned settlement containing structures, items, locks, and commodities. Represented by `Colony.cs`.
- **ColonyLock**: A per-colony `ReaderWriterLockSlim` instance that replaces the existing `object ProcessingLock`. Provides concurrent read access and exclusive write access to all mutable state within a single colony.
- **BackgroundProcessor**: The `Services/BackgroundProcessor.cs` timer-based service that processes colony timers on a `ThreadPool` thread.
- **ColonyStatusCalculator**: The `Services/ColonyStatusCalculator.cs` class that computes resource status and mutates `colony.Locks` and `structure.Statuses`.
- **PlayerContext**: The `Services/PlayerContext.cs` singleton that holds all player data including `BindingList<Colony>`, `BindingList<Blueprint>`, and lookup caches.
- **ItemBag**: The `Models/ItemBag.cs` dictionary-based inventory container with secondary indexes.
- **PropertyBag**: The `Models/PropertyBag.cs` key-value property store used by structures and blueprints.
- **LockTracking**: The `Models/LockTracking.cs` per-process item lock tracker used by `ColonyStatusCalculator`.
- **UI_Thread**: The single WinForms message pump thread that owns all UI controls.
- **Background_Thread**: Any thread other than the UI_Thread, including `ThreadPool` threads, `BackgroundWorker` threads, and `Timer` callback threads.
- **FormColonyV2**: The `Forms/ColonyV2/FormColonyV2.cs` MDI child form for colony management.
- **CancellationFlag**: A per-operation `CancellationTokenSource` (or volatile boolean) used to abort background work when the user switches colonies or closes the form.
- **ReadLock**: Acquiring `ColonyLock.EnterReadLock()` for concurrent read access.
- **WriteLock**: Acquiring `ColonyLock.EnterWriteLock()` for exclusive write access.
- **Snapshot**: A shallow copy of a collection taken under a ReadLock to allow iteration outside the lock.

## Requirements

### Requirement 1: Per-Colony Reader/Writer Lock

**User Story:** As a developer, I want each colony to have a `ReaderWriterLockSlim` so that multiple UI forms can read colony data concurrently while background processors get exclusive write access.

#### Acceptance Criteria

1. THE Colony SHALL expose a `ReaderWriterLockSlim` property named `ColonyLock` that replaces the existing `object ProcessingLock`.
2. THE Colony.ColonyLock SHALL be initialized in the Colony constructor and SHALL NOT be serialized to JSON.
3. WHEN BackgroundProcessor processes a colony, THE BackgroundProcessor SHALL acquire Colony.ColonyLock.EnterWriteLock before calling Colony.ProcessColony and release the lock in a finally block.
4. WHEN a UI form reads colony data for display, THE UI form SHALL acquire Colony.ColonyLock.EnterReadLock and release the lock in a finally block.
5. WHEN ColonyStatusCalculator.CalculateBuilt is called, THE caller SHALL hold Colony.ColonyLock.EnterWriteLock because CalculateBuilt mutates colony.Locks and structure.Statuses.
6. THE ColonyLock SHALL use a timeout of 5000 milliseconds for all lock acquisitions to prevent deadlocks.
7. IF a lock acquisition times out, THEN THE caller SHALL log a warning via NLog and abort the operation without corrupting data.

### Requirement 2: Thread-Safe ItemBag

**User Story:** As a developer, I want ItemBag to be safe for concurrent reads so that background processing and UI display do not corrupt the item dictionary or secondary indexes.

#### Acceptance Criteria

1. THE ItemBag SHALL use a private `object _syncRoot` for synchronizing access to the Items dictionary and secondary indexes.
2. WHEN AddItem, Remove, or Clear is called, THE ItemBag SHALL acquire a lock on _syncRoot before modifying Items and invalidating indexes.
3. WHEN FindByType, FindResource, CountByType, or ContainsKey is called, THE ItemBag SHALL acquire a lock on _syncRoot before reading Items or building indexes.
4. THE ItemBag.EnsureTypeIndex and EnsureResourceIndex methods SHALL execute within the _syncRoot lock to prevent concurrent index rebuilds.
5. THE ItemBag SHALL return defensive copies from FindByType and FindResource so callers iterate without holding the lock.

### Requirement 3: Thread-Safe PropertyBag

**User Story:** As a developer, I want PropertyBag reads and writes to be atomic so that background processing and UI reads do not see partially-updated property dictionaries.

#### Acceptance Criteria

1. THE PropertyBag SHALL use a private `object _syncRoot` for synchronizing access to the Properties dictionary.
2. WHEN setProperty or Remove is called, THE PropertyBag SHALL acquire a lock on _syncRoot before modifying Properties.
3. WHEN getDecimal, getLong, getBoolean, getString, ContainsKey, or Count is called, THE PropertyBag SHALL acquire a lock on _syncRoot before reading Properties.

### Requirement 4: Thread-Safe LockTracking

**User Story:** As a developer, I want LockTracking to be safe for concurrent access so that ColonyStatusCalculator and UI forms do not corrupt the lock dictionary.

#### Acceptance Criteria

1. THE LockTracking SHALL use a private `object _syncRoot` for synchronizing access to the _locks dictionary.
2. WHEN LockItem, LockItems, or ClearLocksForProcess is called, THE LockTracking SHALL acquire a lock on _syncRoot before modifying _locks.
3. WHEN GetLockedQuantity or GetLocksForProcess is called, THE LockTracking SHALL acquire a lock on _syncRoot before reading _locks.
4. THE LockTracking.GetLocksForProcess SHALL return a read-only copy so callers iterate without holding the lock.

### Requirement 5: PlayerContext Collection Safety

**User Story:** As a developer, I want PlayerContext list access to be safe so that background timer callbacks and UI form operations do not corrupt BindingList collections or lookup caches.

#### Acceptance Criteria

1. THE PlayerContext SHALL use a private `object _listLock` for synchronizing access to ColonyList, BlueprintList, SurveyList, and their associated lookup caches.
2. WHEN BackgroundProcessor snapshots the colony list for iteration, THE BackgroundProcessor SHALL acquire a lock on PlayerContext._listLock and copy the list within the lock.
3. WHEN PlayerContext.FindBlueprint, FindSurvey, or FindColony rebuilds a lookup cache, THE PlayerContext SHALL acquire a lock on _listLock to prevent concurrent cache rebuilds.
4. WHEN PlayerContext.WriteContext serializes data, THE PlayerContext SHALL acquire a lock on _listLock to snapshot all lists before serialization.
5. THE PlayerContext.InvalidateBlueprintCache, InvalidateSurveyCache, and InvalidateColonyCache methods SHALL acquire a lock on _listLock before setting cache references to null.

### Requirement 6: Background Colony Form Updates with Cancellation

**User Story:** As a developer, I want the colony form to show basic colony identity instantly when the user switches colonies, then run expensive calculations on a background thread with cancellation support, so the UI stays responsive.

#### Acceptance Criteria

1. WHEN the user selects a colony in FormColonyV2, THE FormColonyV2 SHALL immediately display PlanetName, ColonyName, and SystemName on the UI_Thread without waiting for status calculation.
2. WHEN the user selects a colony in FormColonyV2, THE FormColonyV2 SHALL cancel any in-progress background calculation for the previously selected colony.
3. WHEN the user selects a colony, THE FormColonyV2 SHALL queue RecalculateStatus, RTF generation, and combo population onto a Background_Thread using ThreadPool.QueueUserWorkItem.
4. WHILE a background calculation is running, THE FormColonyV2 SHALL display a visual indicator (e.g. "Calculating..." text or disabled status panel) on the structures tab.
5. WHEN the background calculation completes, THE FormColonyV2 SHALL marshal results back to the UI_Thread via Control.BeginInvoke.
6. IF the CancellationFlag is set before BeginInvoke executes, THEN THE FormColonyV2 SHALL discard the stale results and not update the UI.
7. IF the form is closed while a background calculation is running, THEN THE FormColonyV2 SHALL set the CancellationFlag in OnFormClosed and the background thread SHALL check the flag before calling BeginInvoke.
8. THE background calculation thread SHALL acquire Colony.ColonyLock.EnterReadLock (or EnterWriteLock for CalculateBuilt) before accessing colony data.

### Requirement 7: BackgroundProcessor and UI Coordination

**User Story:** As a developer, I want BackgroundProcessor colony processing to coordinate with UI forms so that forms see consistent data and UI updates are not lost.

#### Acceptance Criteria

1. WHEN BackgroundProcessor completes processing a colony, THE BackgroundProcessor SHALL fire PlayerContext.OnColonyDataChanged outside the Colony.ColonyLock to avoid deadlocks with UI event handlers.
2. WHEN a UI form receives a ColonyDataChanged event from a Background_Thread, THE UI form SHALL use Control.BeginInvoke to marshal the refresh onto the UI_Thread.
3. IF a UI form is disposed when ColonyDataChanged fires, THEN THE UI form event handler SHALL catch ObjectDisposedException from BeginInvoke and return silently.
4. THE BackgroundProcessor SHALL NOT hold Colony.ColonyLock while calling PlayerContext.WriteContext to avoid lock ordering violations with PlayerContext._listLock.

### Requirement 8: Lock Ordering Convention

**User Story:** As a developer, I want a documented lock ordering convention so that future code changes do not introduce deadlocks.

#### Acceptance Criteria

1. THE application SHALL follow this lock acquisition order: PlayerContext._listLock first, then Colony.ColonyLock, then collection-level _syncRoot locks (ItemBag, PropertyBag, LockTracking).
2. THE application SHALL NOT acquire PlayerContext._listLock while holding any Colony.ColonyLock.
3. THE application SHALL NOT acquire Colony.ColonyLock while holding any collection-level _syncRoot lock.
4. IF a code path needs both PlayerContext._listLock and Colony.ColonyLock, THEN THE code path SHALL acquire PlayerContext._listLock first, release it, then acquire Colony.ColonyLock.

### Requirement 9: ColonyStatusCalculator Thread Safety

**User Story:** As a developer, I want ColonyStatusCalculator to be safe for concurrent use so that background recalculation and UI-triggered recalculation do not corrupt colony status data.

#### Acceptance Criteria

1. WHEN ColonyStatusCalculator.CalculateBuilt is called, THE caller SHALL hold Colony.ColonyLock.EnterWriteLock because CalculateBuilt mutates colony.Locks, structure.Statuses, and structure.StatusDelta.
2. WHEN ColonyStatusCalculator.CalculateIdeal is called, THE caller SHALL hold Colony.ColonyLock.EnterWriteLock because CalculateIdeal mutates structure.Statuses.
3. WHEN ColonyStatusCalculator.RecalculateStructure is called, THE caller SHALL hold Colony.ColonyLock.EnterWriteLock because RecalculateStructure mutates finalActualStatus, structure.StatusDelta, and colony.Locks.
4. WHEN ColonyStatusCalculator.ComputeStructureDelta is called as a read-only operation, THE caller SHALL hold at minimum Colony.ColonyLock.EnterReadLock.

### Requirement 10: Concurrent Form Access Safety

**User Story:** As a developer, I want multiple open colony forms to safely read and write the same colony data so that one form's save does not corrupt another form's display.

#### Acceptance Criteria

1. WHEN FormColonyV2 writes colony data (save, add structure, remove item), THE FormColonyV2 SHALL acquire Colony.ColonyLock.EnterWriteLock before mutating colony state.
2. WHEN FormColonyV2 reads colony data for display, THE FormColonyV2 SHALL acquire Colony.ColonyLock.EnterReadLock and take snapshots of collections before populating UI controls.
3. WHEN one FormColonyV2 instance saves colony data, THE FormColonyV2 SHALL fire PlayerContext.OnColonyDataChanged so other open forms displaying the same colony refresh their data.
4. THE FormColonyV2 SHALL release all Colony.ColonyLock locks before calling PlayerContext.WriteContext to respect the lock ordering convention.

### Requirement 11: Graceful Degradation Under Contention

**User Story:** As a developer, I want the application to degrade gracefully when lock contention occurs so that the user experience is not disrupted by background processing delays.

#### Acceptance Criteria

1. IF a UI form cannot acquire Colony.ColonyLock.EnterReadLock within 1000 milliseconds, THEN THE UI form SHALL display stale data and log a warning.
2. IF BackgroundProcessor cannot acquire Colony.ColonyLock.EnterWriteLock within 5000 milliseconds, THEN THE BackgroundProcessor SHALL skip that colony for the current cycle and log a warning.
3. THE BackgroundProcessor SHALL continue processing remaining colonies after skipping a contended colony.

### Requirement 12: Testing Strategy for Thread Safety

**User Story:** As a developer, I want automated tests that verify thread-safety invariants so that regressions are caught before they reach production.

#### Acceptance Criteria

1. THE test suite SHALL include a test that runs BackgroundProcessor.RunCycleOnce concurrently with ColonyStatusCalculator.CalculateBuilt on the same colony and verifies no exceptions are thrown.
2. THE test suite SHALL include a test that performs concurrent AddItem and FindByType operations on the same ItemBag instance and verifies no exceptions or data corruption.
3. THE test suite SHALL include a test that performs concurrent setProperty and getBoolean operations on the same PropertyBag instance and verifies no exceptions.
4. THE test suite SHALL include a test that performs concurrent LockItem and GetLockedQuantity operations on the same LockTracking instance and verifies no exceptions or data corruption.
5. THE test suite SHALL include a test that verifies CancellationFlag prevents stale results from being applied after a colony switch.
6. THE test suite SHALL include a test that verifies Colony.ColonyLock timeout behavior returns without corrupting data.
