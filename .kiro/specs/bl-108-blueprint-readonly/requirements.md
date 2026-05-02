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

## Phase 2: Blueprint Property Immutability

### REQ-BL108-010: Internal Setters on Blueprint
All mutable properties on `Blueprint` SHALL have `internal` setters instead of `public` setters. This includes: `BluePrintType`, `Evolution`, `TechLevel`, `Class`, `CopyCost`, `OwnerUUID`, `BaseBlueprintUUID`, `LegacyUUID`, `Properties`, `Resources`. The `Name`, `NickName`, `Description` setters (inherited from `Item`) SHALL also be restricted.

### REQ-BL108-011: Internal Setters on Item Base Class
The `Item` base class properties that Blueprint inherits (`UUID`, `ItemType`, `Name`, `NickName`, `Description`, `BaseItemTypeID`, `Quantity`, `ResourcePurity`, `Volume`, `Contents`, `CurrentHP`, `MaxHP`, `MaxRepairPercent`) SHALL have `internal` setters. This prevents any code outside the OE2EmpireTracker assembly from mutating Item/Blueprint state.

### REQ-BL108-012: InternalsVisibleTo for Test Project
The main project SHALL declare `[InternalsVisibleTo("OE2EmpireTracker.Tests")]` so tests can still construct and mutate Blueprint objects for test setup.

### REQ-BL108-013: JSON Deserialization Compatibility
Newtonsoft.Json SHALL still be able to deserialize Blueprint objects from JSON. Since Newtonsoft uses reflection and can access internal setters within the same assembly, and the deserializer runs inside the main assembly, this SHALL work without changes. Verify with existing round-trip tests.

### REQ-BL108-014: Authorized Mutation Paths
The following code paths are the ONLY authorized mutators of Blueprint state:
1. **BlueprintViewModel** — form edit path (write-through to model)
2. **MarketBlueprintImporter.UpdateExisting** — import merge
3. **MarketBlueprintImporter.MergeResourcesOnly** — resource-only import merge
4. **BlueprintScanner.ProcessHtml** — HTML parsing into temp Blueprint
5. **BlueprintImportHandler.MergeAndPersist** — import routing (sets UUID, OwnerUUID)
6. **JSON deserialization** — loading from file
7. **Migration code** — data migration paths

All of these are within the main assembly and can access `internal` setters.

### REQ-BL108-015: Compile-Time Enforcement
After migration, any attempt to set a Blueprint property from outside the main assembly (e.g. from a hypothetical plugin or external consumer) SHALL fail at compile time.

## Phase 3: Verification

### REQ-BL108-016: Existing Tests Pass
All existing tests SHALL continue to pass (via InternalsVisibleTo).

### REQ-BL108-017: Audit Clean
All audit checks SHALL pass with zero findings.

## Out of Scope

- Changing other entity types (Colony, Survey, etc.) to internal setters — those are separate BL items
- Adding a unit-of-work or change-tracking pattern — that's BL-074 Phase 3
- Database migration — future work
