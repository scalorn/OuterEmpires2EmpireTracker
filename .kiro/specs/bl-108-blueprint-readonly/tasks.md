# BL-108 Tasks

## Task 1: Add GetAllReadOnlyBlueprints to PlayerContext
- [ ] Add `GetAllReadOnlyBlueprints()` method that returns combined player + global blueprints as `IReadOnlyList<ReadOnlyBlueprint>`
- [ ] Uses existing `GetAllBlueprints()` cache, wraps each in `ReadOnlyBlueprint`
- [ ] Verify with unit test

## Task 2: Migrate List View to ReadOnly
- [ ] Change `PopulateListView()` to accept/use `ReadOnlyBlueprint` list
- [ ] Change list view item Tag from `Blueprint` to `ReadOnlyBlueprint`
- [ ] Update `RefreshBlueprintList()` to call `GetAllReadOnlyBlueprints()`
- [ ] Update all code that reads from list view Tags to use `ReadOnlyBlueprint`

## Task 3: Migrate Selection Handler
- [ ] Change `LvwBlueprints_ItemSelectionChanged` to read UUID from `ReadOnlyBlueprint`
- [ ] Look up mutable `Blueprint` by UUID via `FindBlueprint` / `FindGlobalBlueprint`
- [ ] Pass mutable to `viewModel.SelectBlueprint()`
- [ ] Verify delete button ref count still works

## Task 4: Migrate Filter Combos to ReadOnly
- [ ] Change `cmbFilterType` population to use read-only BlueprintType list
- [ ] Change `cmbFilterClass` population to use read-only ShipClass list
- [ ] Change `cmbFilterTechLevel` population to use read-only TechLevel list
- [ ] Ensure filter logic works with read-only types

## Task 5: Migrate Evolution Graph to ReadOnly
- [ ] Update `EvolutionChainService` to accept `IReadOnlyList<ReadOnlyBlueprint>` or work with the existing read-only interface
- [ ] Update `RefreshEvolutionGraph()` to pass read-only blueprint lists
- [ ] Verify graph renders correctly

## Task 6: Migrate Base Blueprint Candidates to ReadOnly
- [ ] Change `GetBaseBlueprintCandidates()` to return `IReadOnlyList<ReadOnlyBlueprint>`
- [ ] Update base blueprint combo population
- [ ] Update base blueprint selection handler to use UUID lookup for mutable access

## Task 7: Migrate Pricing Plan Combo to ReadOnly
- [ ] Change pricing plan combo population to use `GetReadOnlyPricingPlanList()`
- [ ] Update price calculator to accept read-only inputs
- [ ] Verify calculated price display

## Task 8: Migrate Reference Counter Inputs
- [ ] Update `BlueprintReferenceCounter` to accept read-only inputs where possible
- [ ] Or verify it already doesn't mutate its inputs (lower priority)

## Task 9: Verify and Clean Up
- [ ] Run all 1937 tests — zero failures
- [ ] Run audit — zero findings
- [ ] Grep for direct `Blueprint` references in FormBlueprintV2 read-only paths — should only appear in ViewModel/edit/import/save paths
- [ ] Update BL-108 status in BACKLOG.md to Done
