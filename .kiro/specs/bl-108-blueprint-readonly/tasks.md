# BL-108 Tasks

## Phase 1: Read-Only Consumer Migration

### Task 1: Add GetAllReadOnlyBlueprints to PlayerContext
- [x] Add `GetAllReadOnlyBlueprints()` returning `IReadOnlyList<ReadOnlyBlueprint>`
- [x] Verify with unit test

### Task 2: Migrate List View to ReadOnly
- [x] Change list view item Tag from `Blueprint` to `ReadOnlyBlueprint`
- [x] Update `RefreshBlueprintList()` and `PopulateListView()`
- [x] Update all code that reads from list view Tags

### Task 3: Migrate Filter Combos, Evolution Graph, Pricing, Reference Counter
- [x] Filter combos use read-only type/class/tech lists
- [x] Evolution graph uses read-only blueprint lists
- [x] Base blueprint candidates returns read-only list
- [x] Pricing plan combo uses read-only list
- [x] Reference counter accepts read-only inputs

## Phase 2: ViewModel as Edit Buffer

### Task 4: Create BlueprintUpdateRequest DTO
- [x] Create `BlueprintUpdateRequest` class with all editable fields
- [x] Create `BlueprintCreateRequest` class for new blueprints

### Task 5: Refactor BlueprintViewModel to Edit Buffer
- [x] Replace mutable `_blueprint` reference with local field copies
- [x] Add `LoadFrom(ReadOnlyBlueprint)` to copy fields from snapshot
- [x] Add `BuildUpdateRequest()` to collect changes into DTO
- [x] Add `IsDirty` tracking
- [x] All property setters write to local state only
- [x] Remove `SelectBlueprint(Blueprint)` -- replace with `LoadFrom(ReadOnlyBlueprint)`

### Task 6: Update Form Selection Handler
- [x] On blueprint selection: read UUID from ReadOnlyBlueprint Tag
- [x] Look up ReadOnlyBlueprint (not mutable)
- [x] Call `viewModel.LoadFrom(readOnlyBlueprint)`
- [x] Populate form fields from ViewModel local state

### Task 7: Remove Write-Through from Form
- [x] TextChanged handlers write to ViewModel local state (already the case after Task 5)
- [x] Statistics grid CellValueChanged writes to ViewModel's local properties dict
- [x] Resources grid CellValueChanged writes to ViewModel's local resources dict
- [x] cmbBlueprintType/cmbShipClass/cmbTechLevel write to ViewModel local fields
- [x] No control writes directly to a Blueprint entity

### Task 8: Wire Dirty Tracking to Save Button
- [x] Save button enabled only when `viewModel.IsDirty`
- [x] Save button disabled after successful save

### Task 9: Unsaved Changes Guard
- [x] Create `PromptUnsavedChanges()` helper returning Save/Discard/Cancel
- [x] Wire into `LvwBlueprints_ItemSelectionChanged` -- prompt before switching
- [x] Wire into `OnFormClosing` -- prompt before closing, cancel close on Cancel
- [x] Wire into `BtnNew_Click` -- prompt before clearing for new
- [x] Cancel option in selection change restores the previous list view selection

## Phase 3: BlueprintService

### Task 9: Create BlueprintService
- [x] `Update(string uuid, BlueprintUpdateRequest)` -- applies changes, persists, fires event, returns ReadOnlyBlueprint
- [x] `Create(BlueprintCreateRequest)` -- creates new, assigns UUID, persists, returns ReadOnlyBlueprint
- [x] `Delete(string uuid)` -- removes, persists, fires event
- [x] `Import(Blueprint temp, ReadOnlyBlueprint selectedTarget)` -- handles dedup/merge, persists, returns result
- [x] `MoveToGlobal(string uuid)` / `MoveToPlayer(string uuid)` -- moves between lists, persists

### Task 10: Update Form Save Handler
- [x] Save checks `viewModel.IsNew` to decide Create vs Update
- [x] Create: calls `viewModel.BuildCreateRequest()`, then `blueprintService.Create(request, isGlobal)`
- [x] Update: calls `viewModel.BuildUpdateRequest()`, then `blueprintService.Update(uuid, request)`
- [x] On success: refresh list, select by UUID, `viewModel.LoadFrom(result)`

### Task 11: Update Form New Handler
- [x] New button prompts if dirty (Save / Discard / Cancel)
- [x] Calls `viewModel.Reset()` to clear to empty state
- [x] Clears all form controls
- [x] Save button disabled until user enters data

### Task 11: Update Form Import Handler
- [x] Individual import (with selection): merge parsed stats/resources into ViewModel local state, mark dirty
- [x] Full import (no selection or market): call `blueprintService.Import(temp, target)`
- [x] Support multi-step import: stats first, resources second, Save commits both

### Task 12: Update Form Delete Handler
- [x] Delete calls `blueprintService.Delete(uuid)`
- [x] On success: refresh list, clear form

### Task 13: Update Form Global Toggle
- [x] Global checkbox save calls `blueprintService.MoveToGlobal/MoveToPlayer(uuid)`

### Task 14: Add FindMutableBlueprint Internal Methods
- [x] `internal Blueprint FindMutableBlueprint(string uuid)` on PlayerContext
- [x] `internal Blueprint FindMutableGlobalBlueprint(string uuid)` on EmpireContext
- [x] Only called by BlueprintService

### Task 15: Change FindBlueprint Return Type
- [x] `FindBlueprint()` returns `ReadOnlyBlueprint`
- [x] `FindGlobalBlueprint()` returns `ReadOnlyBlueprint`
- [x] Fix all compile errors across the codebase

## Phase 4: Verification

### Task 16: Full Verification
- [ ] Run all tests -- zero failures
- [ ] Run audit -- zero findings
- [ ] Grep for direct Blueprint property sets -- only in BlueprintService, deserialization, migration
- [ ] Grep for `FindMutableBlueprint` -- only in BlueprintService
- [ ] Verify form behavior: select, edit, save, import, delete, global toggle all work
- [ ] Update BL-108 status in BACKLOG.md to Done