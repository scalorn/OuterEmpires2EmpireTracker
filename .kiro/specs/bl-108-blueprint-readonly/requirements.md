# BL-108: FormBlueprintV2 — Immutable Data Model, Mutation Through Interface Only

## Motivation

The ItemType=Survey corruption (traced to April 4, 2026) proved that public setters on entity POCOs are dangerous. Something in the blueprint form's code path mutated a Blueprint's ItemType from "Blueprint" to "Survey", corrupting 14 global blueprints. With public setters, any code anywhere can mutate any field at any time without going through a controlled path.

This item makes Blueprint properties immutable to external consumers. Only authorized mutation paths (ViewModel, Importer, Scanner) can modify Blueprint state. The form's read-only paths use ReadOnly wrappers that physically cannot mutate the data.

## Phase 1: Read-Only Consumer Migration

### REQ-BL108-001: List View Uses ReadOnly Wrappers
The blueprint list view SHALL be populated using `ReadOnlyBlueprint` instances. List view item Tags SHALL store `ReadOnlyBlueprint`, not mutable `Blueprint`.

### REQ-BL108-002: Reference Counter Uses ReadOnly Inputs
`BlueprintReferenceCounter` SHALL accept read-only inputs for counting references.

### REQ-BL108-003: Filter Combos Use ReadOnly Sources
Filter combos (cmbFilterType, cmbFilterClass, cmbFilterTechLevel) SHALL be populated from read-only sources.

### REQ-BL108-004: Selection Boundary
When the user selects a blueprint in the list view, the form SHALL read the UUID from the `ReadOnlyBlueprint` Tag, look up the mutable `Blueprint` by UUID, and pass it to the ViewModel. This is the ONLY point where a mutable reference is obtained.

### REQ-BL108-005: Evolution Graph Uses ReadOnly
The evolution chain graph SHALL use `ReadOnlyBlueprint` instances.

### REQ-BL108-006: Base Blueprint Candidates ReadOnly
`GetBaseBlueprintCandidates()` SHALL return `IReadOnlyList<ReadOnlyBlueprint>`.

### REQ-BL108-007: Import Path Unchanged
The clipboard import path remains mutable (it creates temporary Blueprint objects). After import, the list refreshes using read-only wrappers.

### REQ-BL108-008: Pricing Plan Combo ReadOnly
The pricing plan combo SHALL use read-only wrappers.

### REQ-BL108-009: No Mutable Entity in Read-Only Paths
After migration, NO read-only code path SHALL hold a direct reference to a mutable `Blueprint`.

## Phase 2: Controlled Mutable Access

### REQ-BL108-010: PlayerContext Stops Exposing Mutable Blueprints Publicly
`PlayerContext.FindBlueprint(uuid)` SHALL return `ReadOnlyBlueprint`. A new `internal` method `FindMutableBlueprint(uuid)` SHALL return the mutable `Blueprint` for authorized mutation paths only. `GetAllBlueprints()` SHALL return `IReadOnlyList<ReadOnlyBlueprint>`.

### REQ-BL108-011: EmpireContext Stops Exposing Mutable Global Blueprints Publicly
`EmpireContext.FindGlobalBlueprint(uuid)` SHALL return `ReadOnlyBlueprint`. A new `internal` method `FindMutableGlobalBlueprint(uuid)` SHALL return the mutable `Blueprint` for authorized mutation paths only.

### REQ-BL108-012: BlueprintViewModel Uses Internal Mutable Access
`BlueprintViewModel.SelectBlueprint()` SHALL use `FindMutableBlueprint()` to obtain the mutable reference for editing. The ViewModel is the ONLY form-level code that holds a mutable `Blueprint`.

### REQ-BL108-013: Importer Uses Internal Mutable Access
`MarketBlueprintImporter.UpdateExisting()` and `MergeResourcesOnly()` receive mutable `Blueprint` references through the import pipeline, which uses `FindMutableBlueprint()` internally.

### REQ-BL108-014: Scanner Creates Temporary Mutable Blueprints
`BlueprintScanner` creates new `Blueprint()` objects for parsing. These are temporary and never stored — they're passed to the importer which merges them into existing entries via the mutable access path.

### REQ-BL108-015: Compile-Time Enforcement
After migration, any code that calls `FindBlueprint()` gets a `ReadOnlyBlueprint` — it cannot set properties because the wrapper has no setters. Only code that explicitly calls the `internal` mutable accessor can mutate. Since `internal` is assembly-scoped, this limits mutation to code within the main project that deliberately opts in.

## Phase 3: Verification

### REQ-BL108-016: Existing Tests Pass
All existing tests SHALL continue to pass (via InternalsVisibleTo).

### REQ-BL108-017: Audit Clean
All audit checks SHALL pass with zero findings.

## Out of Scope

- Changing other entity types (Colony, Survey, etc.) to internal setters — those are separate BL items
- Adding a unit-of-work or change-tracking pattern — that's BL-074 Phase 3
- Database migration — future work
