# BL-108 Tasks

## Phase 1: Read-Only Consumer Migration (FormBlueprintV2)

### Task 1: Add GetAllReadOnlyBlueprints to PlayerContext
- [ ] Add `GetAllReadOnlyBlueprints()` returning `IReadOnlyList<ReadOnlyBlueprint>`
- [ ] Verify with unit test

### Task 2: Migrate List View to ReadOnly
- [ ] Change list view item Tag from `Blueprint` to `ReadOnlyBlueprint`
- [ ] Update `RefreshBlueprintList()` and `PopulateListView()` to use read-only list
- [ ] Update all code that reads from list view Tags

### Task 3: Migrate Selection Handler
- [ ] Read UUID from `ReadOnlyBlueprint` Tag
- [ ] Look up mutable `Blueprint` by UUID for ViewModel
- [ ] Verify delete button ref count still works

### Task 4: Migrate Filter Combos to ReadOnly
- [ ] Filter combo population uses read-only type/class/tech lists

### Task 5: Migrate Evolution Graph to ReadOnly
- [ ] `EvolutionChainService` accepts read-only blueprint lists
- [ ] `RefreshEvolutionGraph()` passes read-only lists

### Task 6: Migrate Base Blueprint Candidates to ReadOnly
- [ ] `GetBaseBlueprintCandidates()` returns read-only list
- [ ] Base blueprint combo uses read-only items

### Task 7: Migrate Pricing Plan Combo to ReadOnly
- [ ] Pricing plan combo uses read-only list
- [ ] Price calculator accepts read-only inputs

### Task 8: Migrate Reference Counter Inputs
- [ ] `BlueprintReferenceCounter` accepts read-only inputs

## Phase 2: Controlled Mutable Access

### Task 9: Add FindMutableBlueprint Internal Methods
- [ ] Add `internal Blueprint FindMutableBlueprint(string uuid)` to PlayerContext
- [ ] Add `internal Blueprint FindMutableGlobalBlueprint(string uuid)` to EmpireContext
- [ ] Add `internal` Add/Remove methods if not already internal
- [ ] These exist alongside the current public methods initially

### Task 10: Migrate Authorized Callers to FindMutableBlueprint
- [ ] BlueprintViewModel.SelectBlueprint uses `FindMutableBlueprint`
- [ ] BlueprintViewModel.Save uses internal Add/Remove
- [ ] BlueprintImportHandler.MergeAndPersist uses `FindMutableBlueprint`
- [ ] MarketBlueprintImporter.UpdateExisting receives mutable from pipeline
- [ ] ColonyStructureV2 blueprint lookups — determine if mutable needed (likely read-only)
- [ ] BuildPlanExecutionService blueprint lookups — determine if mutable needed (likely read-only)
- [ ] Any other caller that genuinely needs mutable access

### Task 11: Change FindBlueprint Return Type to ReadOnly
- [ ] Change `PlayerContext.FindBlueprint()` return type from `Blueprint` to `ReadOnlyBlueprint`
- [ ] Change `EmpireContext.FindGlobalBlueprint()` return type from `Blueprint` to `ReadOnlyBlueprint`
- [ ] Change `PlayerContext.GetAllBlueprints()` return type to `IReadOnlyList<ReadOnlyBlueprint>`
- [ ] Fix all compile errors — each one is a decision point (mutable vs read-only)
- [ ] Verify all tests pass

## Phase 3: Verification

### Task 12: Full Verification
- [ ] Run all tests — zero failures
- [ ] Run audit — zero findings
- [ ] Grep for `FindMutableBlueprint` calls — should only be in ViewModel, Importer, Scanner
- [ ] Grep for direct `Blueprint` variable declarations in FormBlueprintV2 — should only be in ViewModel/edit paths
- [ ] Update BL-108 status in BACKLOG.md to Done
