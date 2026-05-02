# BL-108: FormBlueprintV2 — Immutable Data Model, Mutation Through Service Only

## Motivation

The ItemType=Survey corruption proved that direct in-memory mutation of entities is dangerous. But the deeper goal (BL-074) is service-call readiness: the data model should work as if the entity lives on a remote service. You can't write-through to a remote object — you read a snapshot, edit locally, and submit changes through a service call.

This item restructures the blueprint form so that:
- All data access goes through ReadOnly wrappers (no mutable entity references anywhere in the form)
- The ViewModel is a local edit buffer disconnected from the entity
- Saves go through a BlueprintService that applies changes atomically
- The form never directly mutates a Blueprint — only the service does

## Phase 1: Read-Only Consumer Migration

### REQ-BL108-001: List View Uses ReadOnly Wrappers
The blueprint list view SHALL be populated using `ReadOnlyBlueprint` instances. List view item Tags SHALL store `ReadOnlyBlueprint`.

### REQ-BL108-002: Reference Counter Uses ReadOnly Inputs
`BlueprintReferenceCounter` SHALL accept read-only inputs.

### REQ-BL108-003: Filter Combos Use ReadOnly Sources
Filter combos SHALL be populated from read-only type/class/tech lists.

### REQ-BL108-004: Evolution Graph Uses ReadOnly
The evolution chain graph SHALL use `ReadOnlyBlueprint` instances.

### REQ-BL108-005: Base Blueprint Candidates ReadOnly
`GetBaseBlueprintCandidates()` SHALL return `IReadOnlyList<ReadOnlyBlueprint>`.

### REQ-BL108-006: Pricing Plan Combo ReadOnly
The pricing plan combo SHALL use read-only wrappers.

### REQ-BL108-007: No Mutable Entity in Read-Only Paths
After migration, NO read-only code path SHALL hold a direct reference to a mutable `Blueprint`.

## Phase 2: ViewModel as Local Edit Buffer

### REQ-BL108-010: ViewModel Copies Fields from ReadOnly
When the user selects a blueprint, the ViewModel SHALL copy field values from the `ReadOnlyBlueprint` into local properties. The ViewModel SHALL NOT hold a reference to the mutable `Blueprint` entity.

### REQ-BL108-011: Text Boxes and Grids Bind to ViewModel Local State
All editable controls (txtName, txtNickName, txtDescription, txtCopyCost, cmbBlueprintType, cmbShipClass, cmbTechLevel, cmbEvolution, dgvStatistics, dgvResources) SHALL read from and write to the ViewModel's local fields. Changes live in the ViewModel only — they do NOT propagate to the entity until Save.

### REQ-BL108-012: No Write-Through
The ViewModel SHALL NOT write changes to the Blueprint entity on every keystroke or control change. The current write-through pattern (TextChanged → viewModel.Name = txtName.Text → _blueprint.Name = value) SHALL be replaced with local-only state changes.

### REQ-BL108-013: Dirty Tracking
The ViewModel SHALL track whether any field has been modified since the last load/save. The ViewModel SHALL retain the original `ReadOnlyBlueprint` snapshot it was loaded from, enabling field-level dirty detection by comparing current local values against the original. The Save button SHALL be enabled only when the ViewModel is dirty.

### REQ-BL108-014: Unsaved Changes Prompt on Selection Change
When the user selects a different blueprint in the list view and the ViewModel is dirty, the form SHALL prompt: "Save changes to '{name}'?" with Save / Discard / Cancel options.
- **Save**: calls BlueprintService.Update, then loads the new selection
- **Discard**: discards local changes, loads the new selection
- **Cancel**: cancels the selection change, keeps the current blueprint selected

### REQ-BL108-015: Unsaved Changes Prompt on Form Close
When the user closes the blueprint form (X button or MDI close) and the ViewModel is dirty, the form SHALL prompt with the same Save / Discard / Cancel dialog.
- **Save**: saves, then closes
- **Discard**: closes without saving
- **Cancel**: cancels the close (form stays open)

### REQ-BL108-016: Unsaved Changes Prompt on Application Exit
When the application exits (MainWindow closing) and any open blueprint form has unsaved changes, the form's OnFormClosing handler SHALL trigger the same prompt. If the user cancels, the application exit SHALL be cancelled.

### REQ-BL108-017: Unsaved Changes Prompt on New Blueprint
When the user clicks New while the ViewModel is dirty, the form SHALL prompt before clearing the form for the new blueprint.

### REQ-BL108-018: Import Merges Into Local Edit State
When the user imports from clipboard while a blueprint is selected, the imported data (stats, resources, or both) SHALL be merged into the ViewModel's local state — not saved directly to the entity. The ViewModel becomes dirty. The user can import stats first, then resources, building up the data across multiple imports before clicking Save. Only Save commits the accumulated changes through the service.

## Phase 3: BlueprintService

### REQ-BL108-030: BlueprintService.Update
A new `BlueprintService` class SHALL provide an `Update(string uuid, BlueprintUpdateRequest changes)` method that:
1. Looks up the mutable Blueprint by UUID (internal access)
2. Applies the changed fields from the request to the entity
3. Persists via WriteContext
4. Fires BlueprintDataChanged event
5. Returns the updated ReadOnlyBlueprint

### REQ-BL108-031: BlueprintService.Create
`BlueprintService.Create(BlueprintCreateRequest request)` SHALL create a new Blueprint, assign a UUID (deterministic for global, random for player), add it to the appropriate list, persist, and return the ReadOnlyBlueprint.

### REQ-BL108-032: BlueprintService.Delete
`BlueprintService.Delete(string uuid)` SHALL remove the blueprint from the appropriate list, persist, and fire the change event.

### REQ-BL108-033: BlueprintService.Import
`BlueprintService.Import(Blueprint tempBlueprint, ReadOnlyBlueprint selectedTarget)` SHALL handle the clipboard import flow — dedup matching, UpdateExisting/MergeResourcesOnly, persist, and return the result.

### REQ-BL108-034: BlueprintService.MoveToGlobal / MoveToPlayer
`BlueprintService.MoveToGlobal(string uuid)` and `MoveToPlayer(string uuid)` SHALL handle the global/player toggle — moving the blueprint between lists, updating OwnerUUID, persisting both contexts.

### REQ-BL108-035: Save Flow
When the user clicks Save:
1. ViewModel collects all local field values into a `BlueprintUpdateRequest`
2. Calls `BlueprintService.Update(uuid, request)`
3. Service applies changes to the entity, persists, fires event
4. Form receives the change event, refreshes list view with new ReadOnlyBlueprint
5. ViewModel reloads from the fresh ReadOnlyBlueprint

### REQ-BL108-036: Service Is the Only Mutator
After migration, the Blueprint entity SHALL only be mutated by:
1. `BlueprintService` methods (Update, Create, Delete, Import, Move)
2. JSON deserialization (loading from file)
3. Migration code (data migration paths)

No form, ViewModel, or other consumer SHALL directly set properties on a Blueprint.

## Phase 4: Verification

### REQ-BL108-040: Existing Tests Pass
All existing tests SHALL continue to pass.

### REQ-BL108-041: Audit Clean
All audit checks SHALL pass with zero findings.

### REQ-BL108-042: No Direct Mutation Outside Service
Grep for direct Blueprint property sets — SHALL only appear in BlueprintService, deserialization, and migration code.

## Out of Scope

- Changing other entity types (Colony, Survey, etc.) to the service pattern — separate BL items
- Actual remote service calls — this establishes the local service pattern that can later be swapped for HTTP/gRPC
- Undo/redo — future enhancement on top of the edit buffer pattern
