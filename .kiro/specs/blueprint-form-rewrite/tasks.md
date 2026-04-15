# Tasks: Blueprint Form Rewrite

Target: `Forms/BlueprintV2/FormBlueprintV2.cs` — new form alongside the existing one.
Old form stays working throughout. Shared refactors (BlueprintImportHandler) used by both.

## Phase 1: Extract Import Logic (shared refactor)

- [x] 1. Create BlueprintImportHandler service
  - [x] 1.1 Create `Services/BlueprintImportHandler.cs` with ClassifyImport, FindTarget, MergeAndPersist
  - [x] 1.2 Move import routing logic from FormBlueprint.cmdImport_Click into the service
  - [x] 1.3 Add comprehensive logging at each decision point
  - [x] 1.4 Write unit tests for ClassifyImport (ResourcesOnly, Full, NoName)
  - [x] 1.5 Write unit tests for FindTarget (selected match, dedup match, no match)
  - [x] 1.6 Write unit tests for MergeAndPersist (additive merge, empty props skipped)
  - [x] 1.7 Update old FormBlueprint.cmdImport_Click to delegate to BlueprintImportHandler
  - [x] 1.8 Verify all existing tests still pass and old form works unchanged

## Phase 2: New Form — Core Layout and CRUD

- [x] 2. Create FormBlueprintV2 in Forms/BlueprintV2/
  - [x] 2.1 Create `Forms/BlueprintV2/` directory with FormBlueprintV2.cs and .Designer.cs
  - [x] 2.2 Design form layout in Designer: search panel, filter panel, detail panel, tab control
  - [x] 2.3 Add "Manage Blueprints V2" menu item to MainWindow (alongside existing)
  - [x] 2.4 Add csproj Compile Include entries for new files
  - [x] 2.5 Implement blueprint list population with text + structured filters
  - [x] 2.6 Implement New/Save/Delete with write-through (no ClearProperties on save)
  - [x] 2.7 Implement identity field write-through (Name, NickName, Description, etc.)
  - [x] 2.8 Implement delete protection via BlueprintReferenceCounter
  - [x] 2.9 Implement dynamic title bar with global/player counts

## Phase 3: Statistics and Resources Grids

- [x] 3. Implement write-through grids
  - [x] 3.1 Statistics grid: CellValueChanged writes to PropertyBag, empty clears
  - [x] 3.2 Statistics grid: structure caching per BlueprintType
  - [x] 3.3 Statistics grid: cell validation via BlueprintPropertyValidation
  - [x] 3.4 Statistics grid: CheckBox and ComboBox column types
  - [x] 3.5 Resources grid: write-through to Blueprint.Resources
  - [x] 3.6 Resources grid: add/delete rows update Resources dictionary

## Phase 4: Import Integration

- [ ] 4. Wire import handlers into new form
  - [ ] 4.1 Individual import button using BlueprintImportHandler service
  - [ ] 4.2 Market import button using existing ProcessMarketHtml flow
  - [ ] 4.3 Clipboard content validation with user-friendly messages
  - [ ] 4.4 Post-import: select imported blueprint, refresh list, populate form

## Phase 5: Evolution Graph and Pricing

- [ ] 5. Add evolution graph and pricing tabs
  - [ ] 5.1 Evolution graph tab with chart, checkbox panel, no-changes label
  - [ ] 5.2 Graph refresh on selection change and BlueprintDataChanged
  - [ ] 5.3 Pricing plan combo with computed price display
  - [ ] 5.4 Pricing refresh on PricingDataChanged

## Phase 6: Window State and Events

- [ ] 6. Implement state persistence and event lifecycle
  - [ ] 6.1 WindowStateHelper save/restore for position, grids, filters, combos
  - [ ] 6.2 Subscribe to CurrentPlayerChanged, BlueprintDataChanged, PricingDataChanged
  - [ ] 6.3 Unsubscribe in OnFormClosed with IsDisposed guards
  - [ ] 6.4 ProgrammaticUpdateGuard on all programmatic UI updates

## Phase 7: Acceptance Testing and Cutover

- [ ] 7. Verify and cut over
  - [ ] 7.1 Run full test suite — all existing tests must pass
  - [ ] 7.2 Manual testing: import individual blueprint (stats + resources pages)
  - [ ] 7.3 Manual testing: import market blueprints
  - [ ] 7.4 Manual testing: evolution graph with chain resolution
  - [ ] 7.5 Manual testing: filter persistence across app restart
  - [ ] 7.6 Manual testing: verify old form still works identically
  - [ ] 7.7 Cutover: rename menu item, remove old form files (deferred until user approves)
