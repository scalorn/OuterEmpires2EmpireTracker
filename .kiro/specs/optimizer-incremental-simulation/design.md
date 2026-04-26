# Design Document: Optimizer Incremental Simulation

## Overview

This design describes the refactoring of `BuildOrderOptimizer.Optimize()` to replace O(n²) `SimulateAll` calls with incremental delta tracking using a running `ColonyStructureStatus` accumulator. The optimizer currently re-simulates the entire result list from scratch on every status query. Since `SimulateAll` is O(n) per call and is invoked O(n) times during optimization, the overall cost is O(n²).

The refactored optimizer maintains a single `ColonyStructureStatus` instance (the **running accumulator**) that is updated incrementally via `SimulateOneMore` each time a structure is appended to the result list. Look-ahead projections continue to use `SimulateOneMore` against the accumulator without modifying it, preserving the existing stateless look-ahead semantics.

This is a pure internal refactor. The public API (`Optimize`, `IsSupportStructure`, constructor) does not change. All 17 existing optimizer tests (13 with pinned exact sequences) must produce identical output.

**Satisfies: Requirements 1, 2, 5, 6**

## Architecture

The refactoring is confined entirely to `BuildOrderOptimizer.cs`. No other classes are modified.

### Current Architecture (Before)

```
Optimize loop:
  for each primary:
    status = SimulateAll(result)          // O(n) - re-walks entire list
    FixDeficits(result, pool, status)     // calls SimulateAll internally
    status = SimulateAll(result)          // O(n) again
    afterPrimary = SimulateOneMore(...)   // O(1) look-ahead
    ... more SimulateAll calls ...
    result.Add(primary)
```

Each iteration of the primary loop calls `SimulateAll` 4-8 times. `FixDeficits` and `PlaceSupportSafe` also call `SimulateAll` internally. Total: O(n) calls × O(n) per call = O(n²).

### Refactored Architecture (After)

```
Optimize loop:
  accumulator = new ColonyStructureStatus()   // zero-initialized
  bootstrap CC, Rx, Hb, Hy, En:
    result.Add(structure)
    accumulator = SimulateOneMore(accumulator, structure)  // O(1)

  for each primary:
    // Step A: FixDeficits uses accumulator directly
    FixDeficits(result, pool, targetStatus, ref accumulator)

    // Step B: Look-ahead (stateless - does NOT modify accumulator)
    afterPrimary = SimulateOneMore(accumulator, primary)
    afterFuture = SimulateOneMore(afterPrimary, hab)
    afterFuture = SimulateOneMore(afterFuture, hydro)

    // Conditional support placement updates accumulator
    PlaceSupportSafe(result, pool, deficit, ref accumulator)

    // Step C: Place primary and update accumulator
    result.Add(primary)
    accumulator = SimulateOneMore(accumulator, primary)
```

Total: O(n) calls to `SimulateOneMore` × O(1) per call = O(n).

**Satisfies: Requirements 1.1, 1.2, 1.5, 7.2, 7.3**


## Components and Interfaces

### Modified Component: BuildOrderOptimizer

**File:** `OE2EmpireTracker/Services/BuildOrderOptimizer.cs`

#### New Local Variable

```csharp
// Inside Optimize(), replaces all SimulateAll calls
ColonyStructureStatus accumulator = new ColonyStructureStatus();
```

The accumulator is a local variable within `Optimize()`, not a field. It is passed by reference to `FixDeficits` and `PlaceSupportSafe` so they can update it when they append structures.

#### Modified Method Signatures (Private Only)

| Method | Current Signature | New Signature |
|--------|------------------|---------------|
| `FixDeficits` | `(List<CS> result, List<CS> pool, CSS targetStatus, ICSW workers)` | `(List<CS> result, List<CS> pool, CSS targetStatus, ICSW workers, ref CSS accumulator)` |
| `PlaceSupportSafe` | `(List<CS> result, List<CS> pool, string deficitType, ICSW workers)` | `(List<CS> result, List<CS> pool, string deficitType, ICSW workers, ref CSS accumulator)` |

Where `CS` = `ColonyStructure`, `CSS` = `ColonyStructureStatus`, `ICSW` = `IColonyStructureWorkers`.

Both methods currently call `SimulateAll(result, workers)` to get the current status. After refactoring, they read from `accumulator` instead and update it via `SimulateOneMore` when they append a structure.

#### Removed Method

```csharp
// REMOVED - no longer called
private ColonyStructureStatus SimulateAll(List<ColonyStructure> structures, IColonyStructureWorkers workers)
```

**Satisfies: Requirements 5.1, 5.2**

#### Unchanged Methods

- `SimulateOneMore` - remains unchanged, still a pure function
- `HasDeficit` - unchanged
- `IsSupportStructure` - unchanged (public)
- `GetHighestPriorityDeficit` - unchanged
- All pool management methods - unchanged
- `Optimize(Colony)` - same signature, different internal implementation

**Satisfies: Requirements 4.2, 6.1, 6.2, 6.3, 6.4**

### Unchanged Component: ColonyStatusCalculator

**File:** `OE2EmpireTracker/Services/ColonyStatusCalculator.cs`

No changes. The optimizer uses `CalculateBuilt(structure, prevStatus, currentStatus, workers, blueprint)` indirectly through `SimulateOneMore`, which already wraps it.

### Unchanged Component: ColonyStructureStatus

**File:** `OE2EmpireTracker/Models/ColonyStructureStatus.cs`

No changes. Used as the accumulator type.


## Data Models

No new data models are introduced. The refactoring uses existing types:

### ColonyStructureStatus (Accumulator)

The running accumulator is a standard `ColonyStructureStatus` instance with these fields tracked cumulatively:

| Field | Type | Description |
|-------|------|-------------|
| `PowerProvided` | `decimal` | Total power generated by all online structures |
| `PowerRequired` | `decimal` | Total power consumed by all online structures |
| `HabitationProvision` | `decimal` | Total housing units from online structures |
| `HabitationRequired` | `decimal` | Total housing needed (1 per worker + unallocated) |
| `FoodProvision` | `decimal` | Total food production (accumulates regardless of online) |
| `FoodRequired` | `decimal` | Total food needed (1 per worker + unallocated) |
| `EntertainmentProvided` | `decimal` | Total entertainment from online structures |
| `EntertainmentRequired` | `decimal` | Total entertainment needed (2 per worker + unallocated) |
| `UnallocatedBlueCollarPresent` | `bool` | Whether an unallocated blue collar worker has been counted |
| `UnallocatedWhiteCollarPresent` | `bool` | Whether an unallocated white collar worker has been counted |
| `UnallocatedSpecialistPresent` | `bool` | Whether an unallocated specialist worker has been counted |

The accumulator is updated by calling `SimulateOneMore(accumulator, structure, blueprint, workers)` which returns a new `ColonyStructureStatus` with the structure's contribution added. The accumulator variable is then reassigned to the returned value.

### Update Semantics

`SimulateOneMore` delegates to `ColonyStatusCalculator.CalculateBuilt(structure, prevStatus, currentStatus, workers, blueprint)` which:

1. Copies all provision/required fields from `prevStatus`
2. Adds the new structure's resource contributions (power, hab, food, ent, warehouse)
3. Counts assigned workers on the new structure (via `IdealColonyStructureWorkers` which returns all-assigned)
4. Adds worker costs: +1 hab required, +1 food required, +2 ent required per worker
5. Tracks unallocated workers: each type counted once for the first structure that needs it

This is identical to what `SimulateAll` computes by iterating the entire list, because `CalculateBuilt` is designed as a fold operation: `status[n] = CalculateBuilt(structure[n], status[n-1])`.

**Satisfies: Requirements 1.2, 2.2, 2.3**


## Detailed Refactoring Plan

### Call Site Inventory

Every `SimulateAll` call in the current code and its replacement:

#### 1. Bootstrap Phase (implicit)

**Current:** No explicit SimulateAll — bootstrap just adds structures.
**After:** After each `PlaceFromPool` / `result.Add`, update accumulator:
```csharp
accumulator = SimulateOneMore(accumulator, result[result.Count - 1],
    _playerContext.FindBlueprint(result[result.Count - 1].FlatpackBlueprintUUID), idealWorkers);
```

**Satisfies: Requirement 1.4**

#### 2. Primary Loop - Step A: Pre-primary deficit check

**Current:**
```csharp
ColonyStructureStatus status = SimulateAll(result, idealWorkers);
FixDeficits(result, supportPool, status, idealWorkers);
// ...
status = SimulateAll(result, idealWorkers);
```

**After:**
```csharp
// accumulator is already current
FixDeficits(result, supportPool, accumulator, idealWorkers, ref accumulator);
// accumulator is updated by FixDeficits if it added structures
```

**Satisfies: Requirements 1.5, 3.1**

#### 3. Primary Loop - Step B: Look-ahead

**Current:**
```csharp
ColonyStructureStatus afterPrimary = SimulateOneMore(status, primary, primaryBp, idealWorkers);
// ... fix deficits from primary ...
// ... look further ahead with hab + hydro ...
ColonyStructureStatus currentEnd = SimulateAll(result, idealWorkers);
if (afterFutureSupport.HabitationRequired > currentEnd.HabitationProvision) { ... }
currentEnd = SimulateAll(result, idealWorkers);
if (afterFutureSupport.FoodRequired > currentEnd.FoodProvision) { ... }
currentEnd = SimulateAll(result, idealWorkers);
if (afterFutureSupport.EntertainmentRequired > currentEnd.EntertainmentProvided) { ... }
```

**After:**
```csharp
ColonyStructureStatus afterPrimary = SimulateOneMore(accumulator, primary, primaryBp, idealWorkers);
// ... fix deficits from primary using accumulator ...
// ... look further ahead with hab + hydro ...
// Use accumulator directly instead of SimulateAll:
if (afterFutureSupport.HabitationRequired > accumulator.HabitationProvision) { ... }
if (afterFutureSupport.FoodRequired > accumulator.FoodProvision) { ... }
if (afterFutureSupport.EntertainmentRequired > accumulator.EntertainmentProvided) { ... }
```

Look-ahead calls to `SimulateOneMore` are chained on projected status variables (`afterPrimary`, `afterFutureSupport`), never on the accumulator. The accumulator is only read for comparison.

**Satisfies: Requirements 4.1, 4.3**

#### 4. Primary Loop - Step B: Final deficit check

**Current:**
```csharp
status = SimulateAll(result, idealWorkers);
afterPrimary = SimulateOneMore(status, primary, primaryBp, idealWorkers);
if (HasDeficit(afterPrimary)) { FixDeficits(...); }
```

**After:**
```csharp
afterPrimary = SimulateOneMore(accumulator, primary, primaryBp, idealWorkers);
if (HasDeficit(afterPrimary)) { FixDeficits(result, supportPool, afterPrimary, idealWorkers, ref accumulator); }
```

#### 5. Primary Loop - Step C: Place primary

**Current:**
```csharp
result.Add(primary);
```

**After:**
```csharp
result.Add(primary);
accumulator = SimulateOneMore(accumulator, primary, primaryBp, idealWorkers);
```

#### 6. FixDeficits - Internal loop

**Current:**
```csharp
for (int safety = 0; safety < 50; safety++)
{
    ColonyStructureStatus currentEnd = SimulateAll(result, workers);
    // ... check deficits against currentEnd ...
    PlaceSupportSafe(result, pool, needed, workers);
}
```

**After:**
```csharp
for (int safety = 0; safety < 50; safety++)
{
    // Use accumulator directly instead of SimulateAll
    string needed = null;
    if (targetStatus.PowerRequired > accumulator.PowerProvided) needed = ...;
    // ... etc ...
    if (needed == null) needed = GetHighestPriorityDeficit(accumulator);
    if (needed == null) break;
    PlaceSupportSafe(result, pool, needed, workers, ref accumulator);
}
```

**Satisfies: Requirements 3.1, 3.4**

#### 7. PlaceSupportSafe - Before/after comparison

**Current:**
```csharp
ColonyStructureStatus beforeStatus = SimulateAll(result, workers);
ColonyStructureStatus afterStatus = SimulateOneMore(beforeStatus, support, bp, workers);
// ... check for new deficits ...
result.Add(support);
```

**After:**
```csharp
// Use accumulator as beforeStatus
ColonyStructureStatus afterStatus = SimulateOneMore(accumulator, support, bp, workers);
// ... check for new deficits using accumulator as before ...
// Recursive calls pass ref accumulator
result.Add(support);
accumulator = SimulateOneMore(accumulator, support, bp, workers);
```

**Satisfies: Requirements 3.2, 3.3**

#### 8. Leftover Support Phase

**Current:**
```csharp
foreach (var leftover in supportPool)
{
    ColonyStructureStatus status = SimulateAll(result, idealWorkers);
    // ...
    result.Add(leftover);
}
```

**After:**
```csharp
foreach (var leftover in supportPool)
{
    ColonyStructureStatus afterLeftover = SimulateOneMore(accumulator, leftover, lBp, idealWorkers);
    // ...
    result.Add(leftover);
    accumulator = SimulateOneMore(accumulator, leftover, lBp, idealWorkers);
}
```

**Satisfies: Requirement 1.6**


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Incremental accumulation equivalence

*For any* sequence of colony structures (drawn from the set of valid blueprint types: CC, Reactor, Hab, Hydro, Ent, Miner, Refinery, Manufactory, Warehouse, Research Lab, ROA), building a `ColonyStructureStatus` incrementally by folding `SimulateOneMore` over the sequence one structure at a time SHALL produce the same field values (PowerProvided, PowerRequired, HabitationProvision, HabitationRequired, FoodProvision, FoodRequired, EntertainmentProvided, EntertainmentRequired, and all three UnallocatedWorkerPresent flags) as building the status by calling `CalculateBuilt` in a loop over the entire sequence from scratch.

This is the foundational equivalence property: it proves that the incremental approach (fold one-at-a-time) produces the same result as the batch approach (iterate all). If this holds, then replacing `SimulateAll` with the running accumulator cannot change any optimizer decision.

**Validates: Requirements 1.1, 1.2, 2.1, 2.2, 2.3**

### Property 2: SimulateOneMore is pure (does not mutate input)

*For any* `ColonyStructureStatus` value and *any* colony structure with a valid blueprint, calling `SimulateOneMore(status, structure, blueprint, workers)` SHALL leave all fields of the input `status` unchanged. The returned status is a new object; the input is not modified.

This guarantees that look-ahead projections (Step B) do not corrupt the running accumulator.

**Validates: Requirements 4.1, 4.2**

### Property 3: Optimizer output equivalence

*For any* colony configuration (a list of structures composed of one CC plus a random mix of support and primary structures), the refactored optimizer SHALL produce the same output sequence (same blueprint types in the same order, same count) as the pre-refactor optimizer.

This is the end-to-end behavioral equivalence property. It subsumes all other correctness requirements: if the output is identical for all inputs, then deficit resolution, look-ahead, unallocated worker tracking, and priority ordering are all preserved.

Note: This property is tested by running the existing 17 pinned tests (which assert exact sequences and resource values) plus a property-based test that generates random colony configurations and compares outputs.

**Validates: Requirements 2.1, 2.2, 2.4, 2.5, 2.6**

### Property 4: Linear call count

*For any* colony of size n (total structures in input), the refactored optimizer SHALL call `SimulateOneMore` (or `CalculateBuilt`) at most c × n times for some constant c, where c accounts for the fixed overhead of deficit resolution per primary (bounded by the safety limit of 50 iterations and the finite set of support structure types).

This verifies the O(n) complexity claim. The constant c is determined empirically from the test suite but should be small (roughly 5-10x the number of structures).

**Validates: Requirements 7.1, 7.2, 7.3**


## Error Handling

This refactoring does not introduce new error conditions. The existing error handling is preserved:

1. **Null blueprints:** `_playerContext.FindBlueprint()` may return null. The existing null checks in `SimulateOneMore`, `FixDeficits`, and `PlaceSupportSafe` are unchanged.

2. **Safety limit in FixDeficits:** The `for (int safety = 0; safety < 50; safety++)` loop prevents infinite deficit resolution. This is unchanged.

3. **Empty pool fallback:** When `TakeFromPool` returns null, `CreateStructure` is called. If no blueprint exists for the needed provision type, `CreateStructure` returns null and the deficit remains. This is unchanged.

4. **MaxPerColony limit:** `CreateStructure` respects the `MaxPerColony` blueprint property. This is unchanged.

No new exceptions are thrown. No new failure modes are introduced. The accumulator is always in a valid state because `SimulateOneMore` returns a new `ColonyStructureStatus` with all fields computed from the previous status plus the new structure's contribution.

## Testing Strategy

### Existing Tests (Must Pass Unchanged)

All 17 existing tests in `BuildOrderOptimizerTests.cs` must pass with identical output:

- **13 pinned sequence tests** (`AssertExactSequence`): Assert the exact blueprint type AND exact resource values at every position. These are the strongest correctness guarantee — any accumulator drift would cause a mismatch.
- **4 behavioral tests**: Assert no-deficit-from-first-primary, determinism, structure preservation, and output size limit.

These tests serve as the primary regression suite. No changes to test code are needed.

### New Property-Based Tests

Property-based testing is appropriate for this feature because:
- The core claim is an equivalence between two computation strategies (batch vs incremental)
- The input space is large (arbitrary colony configurations)
- Universal properties hold across all valid inputs
- Tests are pure computation with no external dependencies

**Library:** FsCheck (via FsCheck.NUnit integration, already available in the .NET ecosystem for NUnit 4.x)

**Configuration:** Minimum 100 iterations per property test.

#### Property Test 1: Incremental accumulation equivalence

**Tag:** `Feature: optimizer-incremental-simulation, Property 1: Incremental accumulation equivalence`

Generate a random sequence of 1-30 structures (each with a randomly chosen blueprint type from the valid set). Build the cumulative status two ways:
1. **Incremental:** Fold `SimulateOneMore` one structure at a time
2. **Batch:** Call `CalculateBuilt` in a loop over the full sequence

Assert all fields of the final status are equal.

#### Property Test 2: SimulateOneMore purity

**Tag:** `Feature: optimizer-incremental-simulation, Property 2: SimulateOneMore is pure`

Generate a random `ColonyStructureStatus` (random decimal values for all provision/required fields, random booleans for unallocated flags) and a random structure. Snapshot all fields of the input status before calling `SimulateOneMore`. Assert all fields are unchanged after the call.

#### Property Test 3: Optimizer output equivalence

**Tag:** `Feature: optimizer-incremental-simulation, Property 3: Optimizer output equivalence`

Generate a random colony configuration (1 CC + 0-10 random primaries + 0-15 random support structures). Run the optimizer. Verify the output against the pinned test oracle: re-simulate the output sequence with `CalculateBuilt` and assert no deficits from the first primary onward, and that all input structures are preserved in the output.

Note: A true A/B comparison (old vs new implementation) is not feasible in a single codebase. Instead, this property verifies the invariants that the pinned tests enforce: correct sequencing, no deficits, and structure preservation.

#### Property Test 4: Linear call count

**Tag:** `Feature: optimizer-incremental-simulation, Property 4: Linear call count`

This is verified structurally rather than as a property test: after the refactor, `SimulateAll` is removed entirely. Each `SimulateOneMore` call is O(1), and the number of calls is bounded by the number of structures appended to the result list (which is at most n + support structures created, bounded by the safety limit).

A simple assertion can verify: for a colony of n input structures, the output has at most n + 50 structures (safety limit), confirming bounded growth.

### Unit Tests

- **Accumulator initialization:** Verify `new ColonyStructureStatus()` has all fields at zero/false.
- **SimulateAll removal:** Verify (via code review or grep) that `SimulateAll` is not called anywhere in the optimizer.
- **Bootstrap accumulator correctness:** The existing `Optimize_CCOnly_CreatesBootstrapSupport` pinned test covers this — it asserts exact resource values after each bootstrap structure.

### Test Execution

```
"D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug
"D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx
node .kiro/tools/trxparse.js
```
