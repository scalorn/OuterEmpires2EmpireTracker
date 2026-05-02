# BL-108 Tasks

## Phase 1: Read-Only Consumer Migration

### Task 1: Add GetAllReadOnlyBlueprints to PlayerContext
- [ ] Add `GetAllReadOnlyBlueprints()` that returns combined player + global as `IReadOnlyList<ReadOnlyBlueprint>`
- [ ] Verify with unit test

### Task 2: Migrate List View to ReadOnly
- [ ] Change `PopulateListView()` to use `ReadOnlyBlueprint` list
- [ ] Change list view item Tag from `Blueprint` to `ReadOnlyBlueprint`
- [ ] Update `RefreshBlueprintList()` to call `GetAllReadOnlyBlueprints()`
- [ ] Update all code that reads from list view Tags

### Task 3: Migrate Selection Handler
- [ ] Change `LvwBlueprints_ItemSelectionChanged` to read UUID from `ReadOnlyBlueprint`
- [ ] Look up mutable `Blueprint` by UUID
- [ ] Pass mutable to `viewModel.SelectBlueprint()`
- [ ] Verify delete button ref count still works

### Task 4: Migrate Filter Combos to ReadOnly
- [ ] Change filter combo population to use read-only type/class/tech lists
- [ ] Ensure filter logic works with read-only types

### Task 5: Migrate Evolution Graph to ReadOnly
- [ ] Update `EvolutionChainService` to accept read-only blueprint lists
- [ ] Update `RefreshEvolutionGraph()` to pass read-only lists

### Task 6: Migrate Base Blueprint Candidates to ReadOnly
- [ ] Change `GetBaseBlueprintCandidates()` to return read-only list
- [ ] Update base blueprint combo and selection handler

### Task 7: Migrate Pricing Plan Combo to ReadOnly
- [ ] Change pricing plan combo to use read-only list
- [ ] Update price calculator to accept read-only inputs

### Task 8: Migrate Reference Counter Inputs
- [ ] Update `BlueprintReferenceCounter` to accept read-only inputs

## Phase 2: Blueprint Property Immutability

### Task 9: Add InternalsVisibleTo
- [ ] Add `[assembly: InternalsVisibleTo("OE2EmpireTracker.Tests")]` to main project
- [ ] Verify tests still compile and pass

### Task 10: Blueprint Internal Setters
- [ ] Change all Blueprint property setters to `internal set`
- [ ] Includes: BluePrintType, Evolution, TechLevel, Class, CopyCost, OwnerUUID, BaseBlueprintUUID, LegacyUUID, Properties, Resources
- [ ] Verify BlueprintViewModel still compiles (same assembly)
- [ ] Verify MarketBlueprintImporter still compiles (same assembly)
- [ ] Verify BlueprintScanner still compiles (same assembly)
- [ ] Verify JSON deserialization still works (round-trip tests)
- [ ] Verify all tests pass (InternalsVisibleTo)

### Task 11: Item Base Class Internal Setters
- [ ] Change all Item property setters to `internal set`
- [ ] Includes: UUID, ItemType, Name, NickName, Description, BaseItemTypeID, Quantity, ResourcePurity, Volume, Contents, CurrentHP, MaxHP, MaxRepairPercent
- [ ] Verify all code that sets Item properties is within the main assembly
- [ ] Verify JSON deserialization still works for all Item subclasses
- [ ] Verify all tests pass

## Phase 3: Verification

### Task 12: Full Verification
- [ ] Run all tests — zero failures
- [ ] Run audit — zero findings
- [ ] Grep for direct `Blueprint` property sets in FormBlueprintV2 — should only appear in ViewModel
- [ ] Grep for `\.ItemType\s*=` outside constructors — should only appear in authorized paths
- [ ] Update BL-108 status in BACKLOG.md to Done
