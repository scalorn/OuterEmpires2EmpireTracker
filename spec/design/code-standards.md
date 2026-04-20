<!-- Extracted from .kiro/specs/empire-systems/design.md — Code Standards section -->
# Code Standards

## Service Implementation Checklist

Every new service must:

1. Have a `private static readonly Logger Log = LogManager.GetCurrentClassLogger();`
2. Log entry with key parameters at Debug level
3. Log result summary at Info level
4. Log validation failures at Warn level
5. Log exceptions at Error level with the exception object
6. Include PERF timing for any computation that iterates collections or does multi-step processing
7. Never acquire locks internally — the caller is responsible for lock acquisition
8. Accept data via constructor parameters or method arguments (dependency injection), not by reaching into singletons
9. Return results rather than mutating shared state directly
10. Use `SystemClock.UtcNow` instead of `DateTime.UtcNow` for all time-dependent logic (enables test clock injection)

## Form Implementation Checklist

Every new form must:

1. Have a `private static readonly Logger Log = LogManager.GetCurrentClassLogger();`
2. Implement `IProgrammaticUpdateSource` with `ProgrammaticUpdateGuard`
3. Log selection changes, save operations, and delete operations at Debug/Info level
4. Include PERF timing on `PopulateForm`, `PopulateList`, and any grid rebuild method
5. Subscribe to relevant PlayerContext data-change events with named methods
6. Unsubscribe from all events in `OnFormClosed`
7. Use `BeginInvoke` for cross-thread UI updates with disposed-form protection
8. Use `CancellationTokenSource` for background computations
9. Follow the reference counting pattern (Refs column, disabled Delete button) where applicable
10. Preserve selection state across list rebuilds

## Logging Requirements

### Logic Logging

- **Service entry/exit**: Log.Debug on entry, Log.Info on completion with result summary.
- **Data mutations**: Log.Info when creating, updating, or deleting entities.
- **Validation failures**: Log.Warn when validation rejects input.
- **Reference resolution**: Log.Debug when resolving UUIDs. Log.Warn when reference cannot be resolved.
- **Cascade triggers**: Log.Info when setting dirty flags and when background processor picks them up.
- **Background processing**: Log.Info at start/end of each cascade cycle with counts.
- **Error paths**: Log.Error with exception for all catch blocks.

### Performance Logging

```csharp
var sw = System.Diagnostics.Stopwatch.StartNew();
// ... phase 1 ...
long t1 = sw.ElapsedMilliseconds;
// ... phase 2 ...
sw.Stop();
Log.Info("MethodName PERF: total={0}ms phase1={1}ms phase2={2}ms",
    sw.ElapsedMilliseconds, t1, sw.ElapsedMilliseconds - t1);
```

Conditional for high-frequency operations:
```csharp
if (sw.ElapsedMilliseconds > 5)
    Log.Debug("UpdateData PERF: {0} total={1}ms", itemName, sw.ElapsedMilliseconds);
```

## Thread Safety Requirements

Lock ordering: `_listLock` → entity-level lock → `_syncRoot`. Never reversed.

- **New entity lists**: `lock(_listLock)` → snapshot → release → iterate snapshot.
- **Station/Ship holds**: Use ItemBag thread-safe API. Never hold `_listLock` while iterating ItemBag.
- **Background cascade**: Snapshot lists at start. Use `TryEnterReadLock(1000ms)` / `TryEnterWriteLock(5000ms)`. Fire events outside all locks.
- **Form threading**: Named event handlers, `BeginInvoke` with `ObjectDisposedException` catch, `CancellationTokenSource` for background work, unsubscribe in `OnFormClosed`.

## Code Quality Standards

### XML Documentation
- All public classes, methods, properties on services and models SHALL have `/// <summary>` comments.
- Enum values SHALL have comments when meaning isn't obvious.

### Null Safety
- Public service methods SHALL validate parameters and throw `ArgumentNullException`.
- String parameters SHALL use `string.IsNullOrEmpty()` checks.

### Magic String/Number Elimination
- Game constants in `Constants/GameConstants.cs`.
- Slot type strings in `Constants/SlotTypes.cs`.
- Enum dictionary keys use `Description` attribute pattern.

### Method Size and Complexity
- No method SHALL exceed 80 lines.
- Nested conditionals max 3 levels deep.
- LINQ chains max 3 operations before named intermediate variables.

### Naming Conventions
- Services: `{Domain}Service` (static, stateless)
- Reference counters: `{Entity}ReferenceCounter` with `{Entity}ReferenceReport`
- ViewModels: `{Entity}ViewModel`
- Forms: `Form{Feature}`
- Constants: `{Domain}Constants`
- Boolean properties: prefix with `Is`, `Has`, `Can`

### Defensive Coding
- All `foreach` over PlayerContext collections SHALL iterate snapshots.
- All UUID lookups SHALL handle "not found" gracefully.
- All `decimal` division SHALL check for zero divisor.
- Grid cell reads SHALL use `?.ToString() ?? ""` pattern.

### Test Coverage
- Every service method: happy path, empty input, null input, edge cases.
- Every model: JSON round-trip tests.
- Reference counters: zero refs, single ref, multiple refs, null UUID.
- ViewModels: property round-trip, computed properties, validation.

## Persistence Evolution Readiness

- **Data access**: Services use `Func<string, T>` finders (swappable to DB queries).
- **UUID references**: All cross-entity refs use UUID strings (map to DB keys).
- **Separable historical data**: Transactions and completed plans are separate arrays.
- **Query patterns**: Transaction filters define minimum DB query interface.
- **Interface extraction**: Future `IDataRepository` from PlayerContext methods.
- **Shared faction DB**: Services take parameters (no singletons), deterministic UUIDs for dedup, snapshot fields on historical records.


<!-- Extracted from .kiro/specs/empire-systems/design.md, lines 3721-3736 — Error Handling -->
## Error Handling

| Scenario | Handling |
|---|---|
| Empty/whitespace plan name | Validation rejects save, shows message |
| Quantity < 1 | Validation rejects save, shows message |
| Blueprint missing "Manufacture Run Time" | Calculator shows "time unknown", no computation |
| Target duration unparseable | Calculator does nothing, no error |
| Colony warehouse missing for allocated item | Shortfall shows all resources as needed |
| Structure no longer exists (deleted colony) | Allocation cleared, item reverts to Staged |
| Ship class exceeds station type limit | Assembly location rejected with message |
| Listing quantity goes negative on sale | Clamped to 0, warning logged |
| Stock target references deleted colony/station | Target flagged as invalid, skipped during check |
| PlayerData missing new arrays | Init methods create empty lists, no error |
| RouteStop has ColonyUUID but no DestinationUUID | Migration copies ColonyUUID → DestinationUUID |

<!-- Extracted from .kiro/specs/empire-systems/design.md, lines 3876-3892 — Testing Strategy -->
## Testing Strategy

### Property-Based Tests (FsCheck)

One test per correctness property (Properties 1-12 above), minimum 100 iterations each. Custom generators for BuildPlan, BuildItem, ShipTemplate, Station, MarketListing, StockPlan.

### Unit Tests

- BuildPlanService validation edge cases
- ResourceCheckService with known blueprints and warehouse contents
- QueueCalculator with known manufacturing times
- ShipBuildService assembly validation matrix (all class × station type combinations)
- MarketService sale recording and listing decrement
- StockTargetService shortfall computation with mixed scopes
- Migration005 on existing PlayerData with RouteStops
- DeliveryGenerationService with known shortfalls and routes
