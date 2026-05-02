# BL-108: FormBlueprintV2 — Switch to ReadOnly Data Wrappers

## Motivation

The ItemType=Survey corruption (traced to April 4, 2026) proved that mutable entity references in read-only code paths are dangerous. Something in the blueprint form's read-only consumption path mutated a Blueprint's ItemType from "Blueprint" to "Survey", corrupting 14 global blueprints. The immutable data model prevents this class of bug entirely — read-only consumers physically cannot mutate the underlying data.

## Requirements

### REQ-BL108-001: List View Population
The blueprint list view (lvwBlueprints) SHALL be populated using `ReadOnlyBlueprint` instances from `playerContext.GetReadOnlyBlueprintList()` and `empireContext.GetReadOnlyGlobalBlueprintList()`. The list view item Tag SHALL store `ReadOnlyBlueprint`, not mutable `Blueprint`.

### REQ-BL108-002: Reference Counter
`BlueprintReferenceCounter` SHALL accept `IReadOnlyList<ReadOnlyBlueprint>` (or equivalent read-only inputs) for counting references. The Refs column in the list view SHALL use the read-only reference counter.

### REQ-BL108-003: Filter Combos
The filter combos (cmbFilterType, cmbFilterClass, cmbFilterTechLevel) SHALL be populated from read-only sources. `BlueprintType`, `ShipClass`, and `TechLevel` lists from EmpireContext SHALL be accessed via read-only methods.

### REQ-BL108-004: Blueprint Selection Transition
When the user selects a blueprint in the list view, the form SHALL:
1. Read the UUID from the `ReadOnlyBlueprint` stored in the list view item Tag
2. Look up the mutable `Blueprint` by UUID via `playerContext.FindBlueprint(uuid)` or `empireContext.FindGlobalBlueprint(uuid)`
3. Create/update the `BlueprintViewModel` with the mutable reference
4. Populate the editable form fields from the ViewModel

This is the ONLY point where a mutable reference is obtained. All other code paths use read-only wrappers.

### REQ-BL108-005: Evolution Graph
The evolution chain graph SHALL use `ReadOnlyBlueprint` instances for building the chain data. `EvolutionChainService.BuildGraphData` SHALL accept read-only blueprint lists.

### REQ-BL108-006: Base Blueprint Candidates
`BlueprintViewModel.GetBaseBlueprintCandidates()` SHALL return `IReadOnlyList<ReadOnlyBlueprint>` for populating the base blueprint combo. The combo items SHALL be read-only.

### REQ-BL108-007: Import Path Isolation
The clipboard import path (BtnImport_Click) SHALL:
1. Parse the clipboard into a temporary mutable `Blueprint` (as today)
2. Route through `BlueprintImportHandler` (as today — this is a mutation path)
3. After import, refresh the list view using read-only wrappers
4. Select the imported blueprint by UUID, transitioning to mutable via REQ-BL108-004

The import path is inherently mutable and does not change.

### REQ-BL108-008: Pricing Plan Combo
The pricing plan combo SHALL be populated from `playerContext.GetReadOnlyPricingPlanList()`. The selected plan for price calculation SHALL be accessed via read-only wrapper.

### REQ-BL108-009: Statistics Grid Read Path
When populating the statistics grid for display (not editing), property values SHALL be read from the `BlueprintViewModel` (which wraps the mutable entity). The grid cells are editable — this is a mutation path and remains mutable. No change needed here.

### REQ-BL108-010: No Mutable Entity in Read-Only Paths
After migration, NO read-only code path (list population, combo population, reference counting, evolution graph, pricing display) SHALL hold a direct reference to a mutable `Blueprint`, `Colony`, `Survey`, `PlayerProfile`, or other entity. All read-only paths SHALL use `ReadOnly*` wrappers exclusively.

### REQ-BL108-011: Existing Tests Pass
All 1937 existing tests SHALL continue to pass after migration. No behavioral change to the form's user-facing functionality.

## Out of Scope

- Changing the ViewModel/edit path to use read-only wrappers (the edit path needs mutable access)
- Changing the import/scanner path (inherently mutable)
- Changing the save/delete path (inherently mutable)
- Adding new tests for the form (covered by existing tests + the readonly-data-wrappers property tests)
