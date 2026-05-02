# BL-108 Design: Blueprint Immutable Data Model

## Overview

This is a two-phase migration:
1. **Phase 1**: Switch FormBlueprintV2's read-only paths to use ReadOnly wrappers
2. **Phase 2**: Make Blueprint/Item property setters `internal`, enforcing immutability at compile time

## Phase 1: Read-Only Consumer Migration

### Selection Boundary Pattern

The critical design decision: the list view selection handler is the ONLY mutable boundary.

```
List View (ReadOnlyBlueprint in Tags)
    │
    ▼ user selects
Selection Handler
    │ reads UUID from ReadOnlyBlueprint
    │ looks up mutable Blueprint by UUID
    ▼
BlueprintViewModel (wraps mutable Blueprint)
    │
    ▼ write-through
Mutable Blueprint (internal setters)
```

```csharp
// Selection handler — the controlled gate
var roBp = lvwBlueprints.SelectedItems[0].Tag as ReadOnlyBlueprint;
var bp = playerContext.FindBlueprint(roBp.UUID)
      ?? empireContext.FindGlobalBlueprint(roBp.UUID);
viewModel.SelectBlueprint(bp);
```

### What Uses ReadOnly (Phase 1)

| Component | Current | After |
|-----------|---------|-------|
| List view Tags | `Blueprint` | `ReadOnlyBlueprint` |
| Filter combos | Mutable type lists | Read-only type lists |
| Reference counter | Mutable lists | Read-only lists |
| Evolution graph | Mutable blueprint lists | Read-only blueprint lists |
| Base blueprint combo | Mutable list | Read-only list |
| Pricing plan combo | Mutable list | Read-only list |

### What Stays Mutable (Phase 1)

| Component | Reason |
|-----------|--------|
| BlueprintViewModel | Legitimate edit path |
| Edit-panel combos (cmbBlueprintType, cmbShipClass, cmbTechLevel) | Write-through to ViewModel |
| Statistics grid | Editable cells write through ViewModel |
| Resources grid | Editable cells write through ViewModel |
| Import/scanner path | Creates temp Blueprint objects |
| Save/delete path | Persists through ViewModel |

## Phase 2: Controlled Mutable Access

### The Real Enforcement

`internal set` on properties doesn't help in a single-assembly app — all code in the project can still mutate. The real enforcement is **controlling who gets a mutable reference**.

After Phase 2:
- `PlayerContext.FindBlueprint(uuid)` returns `ReadOnlyBlueprint` — callers can't mutate
- `PlayerContext.FindMutableBlueprint(uuid)` (internal) returns mutable `Blueprint` — only authorized code calls this
- `EmpireContext.FindGlobalBlueprint(uuid)` returns `ReadOnlyBlueprint`
- `EmpireContext.FindMutableGlobalBlueprint(uuid)` (internal) returns mutable `Blueprint`

### Who Gets Mutable Access

| Code Path | Access | Method |
|-----------|--------|--------|
| BlueprintViewModel.SelectBlueprint | Mutable | `FindMutableBlueprint(uuid)` |
| BlueprintViewModel.Save | Mutable | Already has reference from SelectBlueprint |
| MarketBlueprintImporter.UpdateExisting | Mutable | Receives mutable from import pipeline |
| BlueprintImportHandler.MergeAndPersist | Mutable | `FindMutableBlueprint(uuid)` or creates new |
| BlueprintScanner | Mutable | Creates `new Blueprint()` (temporary) |
| JSON deserialization | Mutable | Creates `new Blueprint()` via reflection |
| Migration code | Mutable | Direct list access (internal) |

### Who Gets ReadOnly Access

Everything else:
- List view population
- Filter combo population
- Reference counting
- Evolution graph
- Pricing calculation
- Colony form reading blueprint properties
- Build planner reading blueprint properties
- Any form that displays blueprint data without editing it

### API Changes on PlayerContext

```csharp
// PUBLIC — returns ReadOnly, safe for all consumers
public ReadOnlyBlueprint FindBlueprint(string uuid) { ... }
public IReadOnlyList<ReadOnlyBlueprint> GetAllBlueprints() { ... }

// INTERNAL — returns mutable, only for ViewModel/Importer/Scanner
internal Blueprint FindMutableBlueprint(string uuid) { ... }
internal void AddBlueprint(Blueprint bp) { ... }
internal void RemoveBlueprint(Blueprint bp) { ... }
```

### Migration Strategy

This is a breaking API change — every caller of `FindBlueprint` that expects a mutable `Blueprint` needs to be updated. The migration order:

1. Add the new `FindMutableBlueprint` internal methods alongside existing public methods
2. Migrate authorized callers (ViewModel, Importer) to use `FindMutableBlueprint`
3. Change `FindBlueprint` return type from `Blueprint` to `ReadOnlyBlueprint`
4. Fix all compile errors — each one is a code path that was getting mutable access and shouldn't be
5. Verify

### Risk: Broad Impact

Changing `FindBlueprint` return type breaks every caller. This is intentional — each compile error forces a decision: does this code need mutable access (use `FindMutableBlueprint`) or read-only access (use the `ReadOnlyBlueprint` it now gets)?

Most callers only read properties and will work fine with `ReadOnlyBlueprint` since it exposes the same getters. The few that mutate will need to switch to `FindMutableBlueprint`.

## Task Ordering

Phase 1 tasks (read-only wrappers) can be done independently of Phase 2 (internal setters). Phase 1 provides immediate protection for the blueprint form. Phase 2 provides assembly-wide enforcement.

Recommended order:
1. Phase 1 tasks (list view, selection, combos, graph, pricing)
2. Phase 2: Blueprint-specific internal setters
3. Phase 2: Item base class internal setters (broader impact)
4. Verification
