# Non-Functional Requirements

## User Goal

The application must be fast, reliable, and resilient — responding quickly to user actions, never losing data, and handling concurrent operations (background processing vs UI) without corruption or freezes.

## Performance Targets

**REQ-NF-001** Form population (PopulateForm, PopulateListView, PopulateGrid) SHALL complete within 500ms for typical data volumes (up to 200 items in a list). PERF logging SHALL report actual timing.
**REQ-NF-002** Colony import (clipboard parse + merge) SHALL complete within 2 seconds for a single colony with up to 66 structures.
**REQ-NF-003** Background processing cycle SHALL complete within 30 seconds for up to 200 colonies. If processing exceeds 30 seconds, a warning SHALL be logged.
**REQ-NF-004** PlayerContext.WriteContext() SHALL complete within 3 seconds for typical save file sizes (up to 10MB).
**REQ-NF-005** Blueprint scanner (mass import from HTML) SHALL process up to 500 listings within 10 seconds.
**REQ-NF-006** Audit tools (node .kiro/tools/audit.js) SHALL complete within 60 seconds.

## Data Scale Assumptions

**REQ-NF-010** The application SHALL support up to 200 colonies per player without degradation.
**REQ-NF-011** The application SHALL support up to 66 structures per colony (game limit).
**REQ-NF-012** The application SHALL support up to 10000 blueprints (player + global combined) without degradation.
**REQ-NF-013** The application SHALL support up to 200 surveys without degradation.
**REQ-NF-014** The application SHALL support up to 50 delivery routes with up to 20 stops each.
**REQ-NF-015** The application SHALL support up to 500 market listings and 20000 market transactions without degradation.
**REQ-NF-016** The application SHALL support up to 20 build plans with up to 100 build items each.
**REQ-NF-017** "Without degradation" means form population remains under 500ms (REQ-NF-001) and background processing remains under 30 seconds (REQ-NF-003).

## Crash Recovery

**REQ-NF-020** Data loss SHALL NOT occur on application crash. SafeFileWriter (REQ-SFW-001 through REQ-SFW-005) ensures atomic writes with one-deep backup.
**REQ-NF-021** If the application crashes during WriteContext(), the previous save SHALL be recoverable from the .bak file.
**REQ-NF-022** If the application crashes during background processing, no partial colony state SHALL be persisted — WriteContext() is called only after all colonies are processed successfully.
**REQ-NF-023** On startup, if PlayerData.json is corrupt or unreadable, the application SHALL attempt to load PlayerData.json.bak. If both fail, it SHALL start with empty data and log an error.

## Concurrent Access

**REQ-NF-030** The background processor and UI forms SHALL NOT corrupt shared data when accessing the same colony simultaneously. Colony.ColonyLock (ReaderWriterLockSlim) enforces mutual exclusion.
**REQ-NF-031** UI forms SHALL acquire a read lock before reading colony data for display. The background processor SHALL acquire a write lock before modifying colony data.
**REQ-NF-032** If the background processor modifies a colony that a form is currently displaying, the form SHALL receive a ColonyDataChanged event and refresh its display automatically.
**REQ-NF-033** If a UI write lock times out (5000ms), the operation SHALL fail gracefully with a logged warning — not hang or crash.
**REQ-NF-034** PlayerContext list mutations (_listLock) SHALL NOT be held during long operations. Snapshot the list, release the lock, then process.

## Cross-Form Cascading on Delete

**REQ-NF-040** Deleting an entity that is referenced by other entities SHALL be blocked by the reference counter system. The Delete button SHALL be disabled with "In Use (N)" text.
**REQ-NF-041** If a referenced entity is deleted despite the guard (e.g. via direct JSON editing), forms displaying the reference SHALL show "(unknown)" for the missing entity name — never a raw UUID or crash.
**REQ-NF-042** When a colony is deleted, delivery routes referencing that colony SHALL NOT be automatically modified. The route will show "(unknown)" for the deleted colony's stop until the user edits it.
**REQ-NF-043** When a blueprint is deleted, colony structures referencing it (FlatpackBlueprintUUID, ManufacturingBlueprintUUID, ResearchingBlueprintUUID) SHALL NOT be automatically cleared. The structure will show "(unknown)" for the blueprint name.
**REQ-NF-044** When a player profile is deleted, entities owned by that player (colonies, blueprints, surveys) SHALL be reassigned to the first remaining profile. If no profiles remain, OwnerUUID SHALL be set to empty string.

## UI Responsiveness

**REQ-NF-050** Long-running operations (import, mass update, auto-fill) SHALL NOT freeze the UI. Operations exceeding 200ms SHALL use BeginInvoke for UI updates.
**REQ-NF-051** Background processor events SHALL be marshaled to the UI thread via BeginInvoke. Forms SHALL check IsDisposed before processing events.
**REQ-NF-052** Grid rebuilds during background refresh SHALL use ProgrammaticUpdateGuard to suppress cascading event handlers.

## Observability

**REQ-NF-060** All service methods that iterate collections SHALL log execution time at Info level with a "PERF" prefix (e.g. "PERF: ProcessColony 45ms").
**REQ-NF-061** All form Populate/Refresh methods SHALL log execution time at Info level with a "PERF" prefix.
**REQ-NF-062** All exceptions in event handlers SHALL be logged at Error level with full stack trace.
**REQ-NF-063** PlayerContext load/save SHALL log at Info level with entity counts (e.g. "Loaded: 12 colonies, 450 blueprints, 30 surveys").
**REQ-NF-064** Background processor SHALL log at Debug level on each tick, and at Info level when colonies are actually processed.
**REQ-NF-065** Lock acquisition failures (timeout) SHALL be logged at Warn level with the colony UUID and operation attempted.
