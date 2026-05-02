# BL-108 Design: FormBlueprintV2 ReadOnly Migration

## Current State

FormBlueprintV2 currently holds mutable `Blueprint` references everywhere:
- `lvwBlueprints` items have `Tag = Blueprint` (mutable)
- `BlueprintViewModel` wraps a mutable `Blueprint` for editing
- Filter combos use mutable `BlueprintType`, `ShipClass`, `TechLevel` from EmpireContext
- Evolution graph builds chains from mutable blueprint lists
- Reference counter receives mutable blueprint lists
- Pricing plan combo uses mutable `PricingPlan` list

Any code that touches these mutable references can accidentally mutate the data. The ItemType=Survey corruption proved this happens in practice.

## Target State

```
┌─────────────────────────────────────────────────────────┐
│ FormBlueprintV2                                         │
│                                                         │
│  List View (read-only path)                             │
│    Tag = ReadOnlyBlueprint ◄── GetReadOnlyBlueprintList │
│    Refs = BlueprintReferenceCounter(ReadOnly inputs)    │
│                                                         │
│  Filter Combos (read-only path)                         │
│    cmbFilterType ◄── ReadOnly BlueprintType list        │
│    cmbFilterClass ◄── ReadOnly ShipClass list           │
│    cmbFilterTechLevel ◄── ReadOnly TechLevel list       │
│                                                         │
│  Selection ──► UUID ──► FindBlueprint(uuid) ──► mutable │
│                                                         │
│  Edit Panel (mutable path)                              │
│    BlueprintViewModel wraps mutable Blueprint            │
│    Statistics grid reads/writes via ViewModel            │
│    Resources grid reads/writes via ViewModel             │
│    Type/Class/TechLevel combos write via ViewModel       │
│                                                         │
│  Evolution Graph (read-only path)                        │
│    Chain data ◄── ReadOnly blueprint lists               │
│                                                         │
│  Pricing (read-only path)                                │
│    Plan combo ◄── GetReadOnlyPricingPlanList             │
│    Price calc ◄── ReadOnly plan + ReadOnly blueprint     │
└─────────────────────────────────────────────────────────┘
```

## Key Design Decision: Selection Boundary

The critical boundary is the list view selection handler. Today:
```csharp
// CURRENT: list item Tag is mutable Blueprint
var bp = lvwBlueprints.SelectedItems[0].Tag as Blueprint;
viewModel.SelectBlueprint(bp);  // ViewModel now holds mutable ref
PopulateForm();
```

After migration:
```csharp
// NEW: list item Tag is ReadOnlyBlueprint
var roBp = lvwBlueprints.SelectedItems[0].Tag as ReadOnlyBlueprint;
string uuid = roBp.UUID;

// Cross the read-only → mutable boundary via UUID lookup
var bp = playerContext.FindBlueprint(uuid)
      ?? empireContext.FindGlobalBlueprint(uuid);
viewModel.SelectBlueprint(bp);  // ViewModel holds mutable ref for editing
PopulateForm();
```

This is the ONLY place where a mutable reference is obtained from a read-only one. The UUID lookup is the controlled gate.

## Migration Steps

### Step 1: List View Population
Change `RefreshBlueprintList()` / `PopulateListView()`:
- Call `playerContext.GetAllReadOnlyBlueprints()` (combined player + global, read-only)
- Store `ReadOnlyBlueprint` in list view item Tags
- Read `ExtendedName`, `BluePrintType`, `Evolution`, `Class`, `TechLevel` from the read-only wrapper

### Step 2: Selection Handler
Change `LvwBlueprints_ItemSelectionChanged`:
- Read UUID from `ReadOnlyBlueprint` Tag
- Look up mutable `Blueprint` by UUID
- Pass mutable to ViewModel

### Step 3: Reference Counter
Change `BlueprintReferenceCounter` inputs:
- Accept read-only colony/build-plan/ship lists for counting
- Or: keep mutable inputs but ensure the counter doesn't mutate them (lower priority — the counter is already read-only in behavior)

### Step 4: Filter Combos
- `cmbFilterType` items: use read-only BlueprintType list
- `cmbFilterClass` items: use read-only ShipClass list  
- `cmbFilterTechLevel` items: use read-only TechLevel list

### Step 5: Evolution Graph
- `EvolutionChainService` methods accept `IReadOnlyList<ReadOnlyBlueprint>`
- Graph data builder works with read-only wrappers

### Step 6: Pricing Plan Combo
- Populate from `playerContext.GetReadOnlyPricingPlanList()`
- Price calculator accepts `ReadOnlyPricingPlan` + `ReadOnlyBlueprint`

### Step 7: Base Blueprint Combo
- `GetBaseBlueprintCandidates()` returns `IReadOnlyList<ReadOnlyBlueprint>`
- Combo items are read-only

## What Does NOT Change

- `BlueprintViewModel` — continues to wrap mutable `Blueprint` for editing
- Statistics grid editing — reads/writes through ViewModel
- Resources grid editing — reads/writes through ViewModel
- Type/Class/TechLevel combo write-through — writes through ViewModel
- Import path — creates mutable temp Blueprint, routes through importer
- Save/Delete — persists through ViewModel
- `CmbBlueprintType.SelectedItem` for the EDIT combo — this is the mutable type combo that writes through to the ViewModel, not the filter combo

## Risk: Combo DataSource Binding

The edit-panel combos (`cmbBlueprintType`, `cmbShipClass`, `cmbTechLevel`) currently use `BindingSource` backed by mutable lists. These combos need to remain mutable because `SelectedItem` writes back to the ViewModel. The FILTER combos (cmbFilterType, cmbFilterClass, cmbFilterTechLevel) can switch to read-only since they only filter the list view.

Care must be taken not to confuse the two sets of combos during migration.
