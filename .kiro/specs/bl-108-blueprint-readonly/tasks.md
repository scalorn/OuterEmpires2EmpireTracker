# BL-108 Tasks

## Phase 1: Read-Only Consumer Migration

### Task 1: Add GetAllReadOnlyBlueprints to PlayerContext
- [ ] Add `GetAllReadOnlyBlueprints()` returning `IReadOnlyList<ReadOnlyBlueprint>`
- [ ] Verify with unit test

### Task 2: Migrate List View to ReadOnly
- [ ] Change list view item Tag from `Blueprint` to `ReadOnlyBlueprint`
- [ ] Update `RefreshBlueprintList()` and `PopulateListView()`
- [ ] Update all code that reads from list view Tags

### Task 3: Migrate Filter Combos, Evolution Graph, Pricing, Reference Counter
- [ ] Filter combos use read-only type/class/tech lists
- [ ] Evolution graph uses read-only blueprint lists
- [ ] Base blueprint candidates returns read-only list
- [ ] Pricing plan combo uses read-only list
- [ ] Reference counter accepts read-only inputs

## Phase 2: ViewModel as Edit Buffer

### Task 4: Create BlueprintUpdateRequest DTO
- [ ] Create `BlueprintUpdateRequest` class with all editable fields
- [ ] Create `BlueprintCreateRequest` class for new blueprints

### Task 5: Refactor BlueprintViewModel to Edit Buffer
- [ ] Replace mutable `_blueprint` reference with local field copies
- [ ] Add `LoadFrom(ReadOnlyBlueprint)` to copy fields from snapshot
- [ ] Add `BuildUpdateRequest()` to collect changes into DTO
- [ ] Add `IsDirty` tracking
- [ ] All property setters write to local state only
- [ ] Remove `SelectBlueprint(Blueprint)` — replace with `LoadFrom(ReadOnlyBlueprint)`

### Task 6: Update Form Selection Handler
- [ ] On blueprint selection: read UUID from ReadOnlyBlueprint Tag
- [ ] Look up ReadOnlyBlueprint (not mutable)
- [ ] Call `viewModel.LoadFrom(readOnlyBlueprint)`
- [ ] Populate form fields from ViewModel local state

### Task 7: Remove Write-Through from Form
- [ ] TextChanged handlers write to ViewModel local state (already the case after Task 5)
- [ ] Statistics grid CellValueChanged writes to ViewModel's local properties dict
- [ ] Resources grid CellValueChanged writes to ViewModel's local resources dict
- [ ] cmbBlueprintType/cmbShipClass/cmbTechLevel write to ViewModel local fields
- [ ] No control writes directly to a Blueprint entity

### Task 8: Wire Dirty Tracking to Save Button
- [ ] Save button enabled only when `viewModel.IsDirty`
- [ ] Save button disabled after successful save

### Task 9: Unsaved Changes Guard
- [ ] Create `PromptUnsavedChanges()` helper returning Save/Discard/Cancel
- [ ] Wire into `LvwBlueprints_ItemSelectionChanged` — prompt before switching
- [ ] Wire into `OnFormClosing` — prompt before closing, cancel close on Cancel
- [ ] Wire into `BtnNew_Click` — prompt before clearing for new
- [ ] Cancel option in selection change restores the previous list view selection

## Phase 3: BlueprintService

### Task 9: Create BlueprintService
- [ ] `Update(string uuid, BlueprintUpdateRequest)` — applies changes, persists, fires event, returns ReadOnlyBlueprint
- [ ] `Create(BlueprintCreateRequest)` — creates new, assigns UUID, persists, returns ReadOnlyBlueprint
- [ ] `Delete(string uuid)` — removes, persists, fires event
- [ ] `Import(Blueprint temp, ReadOnlyBlueprint selectedTarget)` — handles dedup/merge, persists, returns result
- [ ] `MoveToGlobal(string uuid)` / `MoveToPlayer(string uuid)` — moves between lists, persists

### Task 10: Update Form Save Handler
- [ ] Save button calls `viewModel.BuildUpdateRequest()`
- [ ] Calls `blueprintService.Update(uuid, request)`
- [ ] On success: refresh list, re-select, `viewModel.LoadFrom(result)`

### Task 11: Update Form Import Handler
- [ ] Individual import (with selection): merge parsed stats/resources into ViewModel local state, mark dirty
- [ ] Full import (no selection or market): call `blueprintService.Import(temp, target)`
- [ ] Support multi-step import: stats first, resources second, Save commits both

### Task 12: Update Form Delete Handler
- [ ] Delete calls `blueprintService.Delete(uuid)`
- [ ] On success: refresh list, clear form

### Task 13: Update Form Global Toggle
- [ ] Global checkbox save calls `blueprintService.MoveToGlobal/MoveToPlayer(uuid)`

### Task 14: Add FindMutableBlueprint Internal Methods
- [ ] `internal Blueprint FindMutableBlueprint(string uuid)` on PlayerContext
- [ ] `internal Blueprint FindMutableGlobalBlueprint(string uuid)` on EmpireContext
- [ ] Only called by BlueprintService

### Task 15: Change FindBlueprint Return Type
- [ ] `FindBlueprint()` returns `ReadOnlyBlueprint`
- [ ] `FindGlobalBlueprint()` returns `ReadOnlyBlueprint`
- [ ] Fix all compile errors across the codebase

## Phase 4: Verification

### Task 16: Full Verification
- [ ] Run all tests — zero failures
- [ ] Run audit — zero findings
- [ ] Grep for direct Blueprint property sets — only in BlueprintService, deserialization, migration
- [ ] Grep for `FindMutableBlueprint` — only in BlueprintService
- [ ] Verify form behavior: select, edit, save, import, delete, global toggle all work
- [ ] Update BL-108 status in BACKLOG.md to Done
