<!-- Extracted from .kiro/specs/empire-systems/design.md — Cascade Processing section -->
# Cascade Processing

Market transactions, stock target checks, resource availability re-evaluation, and delivery plan updates are computationally expensive when they cascade (sale → stock target → build order → resource check → delivery plan). Running these synchronously on the UI thread would block the application.

Design decision: cascade operations run as part of the existing BackgroundProcessor system. The immediate user action (recording a sale, recording a purchase) performs only the direct mutation (decrement listing, add to station hold) and sets a dirty flag. The background processor picks up dirty flags on its next tick and runs the cascade:

1. Check stock targets against current inventory → generate build items if needed
2. Re-evaluate resource availability for all active build plans → update shortfall data
3. Update delivery plans if shortfalls have changed
4. Update build item statuses based on new resource availability

Forms subscribe to data change events and refresh when the background processor completes a cascade cycle. This keeps the UI responsive while ensuring cascades complete within one background tick (default 60 seconds, configurable via Preferences).

The dirty flag approach means cascades are batched — multiple transactions recorded in quick succession result in one cascade evaluation, not one per transaction.

## Thread Safety Integration

The cascade processing integrates with the existing three-tier locking model (see `.kiro/specs/data-model-thread-safety/`):

- **PlayerContext._listLock** protects all `List<T>` collections during iteration and mutation. The BackgroundProcessor uses `SnapshotColonyList()` and equivalent snapshot methods for new lists.
- **Colony.ColonyLock** (ReaderWriterLockSlim) protects per-colony data. Cascade operations that read colony warehouse data acquire `TryEnterReadLock(1000ms)`. Operations that mutate colony data acquire `TryEnterWriteLock(5000ms)`. On timeout, the cascade skips that colony and retries next tick.
- **Collection-level _syncRoot** (ItemBag, PropertyBag, LockTracking) provides fine-grained protection within each colony's data structures.

Lock ordering is always: `_listLock` → `ColonyLock` → `_syncRoot`. Events are fired outside all locks. WriteContext is called outside ColonyLock.

New entity lists use `List<T>` instead of `BindingList<T>`. Access is protected by `_listLock` — snapshot under lock, iterate outside. Forms use `BeginInvoke` to marshal data-change events to the UI thread.

## Implementation (Dirty Flags)

The dirty flags live on PlayerContext as runtime-only fields (not persisted):

```csharp
// Runtime cascade flags — not serialized
[JsonIgnore] public bool CascadeStockTargetsDirty { get; set; } = false;
[JsonIgnore] public bool CascadeResourceCheckDirty { get; set; } = false;
```

When a form records a market transaction, updates a station hold, or modifies build plan data, it sets the appropriate flag(s). The BackgroundProcessor checks these flags on each tick:

```csharp
// In BackgroundProcessor tick:
if (playerContext.CascadeStockTargetsDirty)
{
    playerContext.CascadeStockTargetsDirty = false;
    // Run StockTargetService.CheckTargets → generate build items if needed
    // Set CascadeResourceCheckDirty if new items were created
}
if (playerContext.CascadeResourceCheckDirty)
{
    playerContext.CascadeResourceCheckDirty = false;
    // Run ResourceCheckService on all active build plans
    // Update delivery plans, item statuses
    // Fire BuildPlanDataChanged event
}
```

The flags are simple booleans — no queue, no event log. The background processor clears the flag before processing to avoid missing a flag set during processing.

## Startup Cascade

On application startup, the background processor runs a full cascade evaluation unconditionally (as if all flags were dirty). This handles:
- Dirty flags lost due to app exit before the next tick
- Manual JSON edits or data imports
- Any inconsistency from a crash or unexpected shutdown

## Build Item Status Cascade Monotonicity

For any build item, the cascade processor only advances status forward:

`Staged(0) < Delivering(1) < Ready(2) < InProgress(3) < Completed(4)`

Cascade sets `max(currentStatus, computedStatus)`. It never sets InProgress or Completed (those are always manual), and never decreases the status ordinal. Manual status transitions always win — the user has an incomplete view of the data (no game API), so the tool trusts the user's judgment.