# Tasks: Colony Form Rewrite

Target: `Forms/ColonyV2/FormColonyV2.cs` — new form alongside the existing one.
Old form stays working throughout. Service-level perf fixes benefit both forms.

## Phase 1: Service-Level Performance Fixes (shared refactors)

- [x] 1. Add dictionary caches for FindBlueprint/FindSurvey/FindColony
  - [x] 1.1 Add `Dictionary<string, Blueprint> _blueprintCache` to PlayerContext with lazy build and O(1) lookup in FindBlueprint(); invalidate on add/remove/UUID change
    - PlayerContext.FindBlueprint() checks local cache first, then delegates to EmpireContext
    - _Requirements: 24.1_
  - [x] 1.2 Add `Dictionary<string, Blueprint>` cache to EmpireContext.FindGlobalBlueprint() with same pattern
    - _Requirements: 24.1_
  - [x] 1.3 Add `Dictionary<string, Survey>` cache to PlayerContext.FindSurvey() with invalidation
    - _Requirements: 24.1_
  - [x] 1.4 Add `Dictionary<string, Colony>` cache to PlayerContext.FindColony() with invalidation
    - _Requirements: 24.1_
  - [x] 1.5 Write unit tests for dictionary caches: verify O(1) lookup, cache invalidation on add/remove, null/empty UUID handling
    - Test FindBlueprint returns correct blueprint after cache built
    - Test cache is invalidated when BlueprintList changes
    - Test FindBlueprint falls back to EmpireContext global cache
    - _Requirements: 24.1_

- [x] 2. Add ItemBag secondary index
  - [x] 2.1 Add `Dictionary<(ItemType, string), List<Item>> _typeIndex` to ItemBag with lazy build; use in FindByType and CountByType
    - Invalidate index on AddItem and Remove
    - _Requirements: 24.5_
  - [x] 2.2 Add compound key `(ItemType, BaseItemTypeID, Purity)` lookup for FindResource
    - _Requirements: 24.5_
  - [x] 2.3 Write unit tests for ItemBag secondary index: FindByType/CountByType/FindResource return correct results after add/remove
    - _Requirements: 24.5_

- [x] 3. Cache StructureViewModels and GetAllBlueprints
  - [x] 3.1 Add `_cachedStructureVMs` to ColonyViewModel; invalidate on AddStructure/RemoveStructure/reorder
    - _Requirements: 24.2_
  - [x] 3.2 Add `_allBlueprintsCache` to PlayerContext.GetAllBlueprints(); invalidate when either blueprint list changes
    - _Requirements: 24.6_
  - [x] 3.3 Write unit tests for StructureViewModels cache invalidation and GetAllBlueprints cache invalidation
    - _Requirements: 24.2, 24.6_

- [x] 4. Incremental status deltas in ColonyStatusCalculator
  - [x] 4.1 Add `StructureStatusDelta` class to Models; add `StatusDelta` property to ColonyStructure
    - Fields: PowerProvided, PowerRequired, HabitationProvision, FoodProvision, EntertainmentProvided, WarehouseCapacity, WorkerCount, UnallocatedCount
    - _Requirements: 24.4_
  - [x] 4.2 Add blueprint cache per calculation pass in ColonyStatusCalculator.CalculateBuilt() to avoid repeated FindBlueprint calls
    - _Requirements: 24.3_
  - [x] 4.3 Implement `ComputeStructureDelta()` and `SumAllDeltas()` in ColonyStatusCalculator for full recalculation path
    - _Requirements: 24.4_
  - [x] 4.4 Implement `RecalculateStructure()` for O(1) single-structure update: subtract old delta, compute new, add new
    - _Requirements: 24.4_
  - [x] 4.5 Write unit tests for incremental deltas: verify single-structure recalculation produces same totals as full recalculation
    - _Requirements: 24.4_

- [x] 5. Checkpoint — Service-level fixes
  - Ensure all existing tests pass (ColonyStatusCalculatorTests, ItemBagTests, etc.)
  - Verify old FormColony still works unchanged with the enhanced services
  - Ask the user if questions arise.

## Phase 2: Form Scaffolding and Colony List

- [-] 6. Create FormColonyV2 shell with colony list
  - [x] 6.1 Create `Forms/ColonyV2/` directory with FormColonyV2.cs and FormColonyV2.Designer.cs
    - Designer layout: flpColonyList (left) with txtColonyFilter + lvwColonies; flpColonyData (right) with flpIdentity + tabDetailedData + flpCommands
    - _Requirements: 25.1, 1.1_
  - [x] 6.2 Add "Manage Colonies V2" menu item to MainWindow alongside existing
    - _Requirements: 25.3_
  - [x] 6.3 Add csproj Compile Include entries for new files
    - _Requirements: 25.1_
  - [x] 6.4 Implement colony list population with FullRowSelect, single-selection, columns: Planet, Name, Refs
    - Refs column shows ColonyReferenceCounter.TotalCount per colony
    - _Requirements: 1.1, 1.4, 1.7_
  - [x] 6.5 Implement case-insensitive substring filter on PlanetName and ColonyName
    - _Requirements: 1.2_
  - [x] 6.6 Implement column header sort with ascending/descending toggle
    - _Requirements: 1.3_
  - [x] 6.7 Implement dynamic title bar "Manage Colonies - {PlayerName} : {ColonyCount}"
    - _Requirements: 1.6_
  - [x] 6.8 Implement colony selection handler: create ColonyViewModel, populate identity fields (PlanetName, ColonyName, SystemName), mark all tabs dirty
    - Use ProgrammaticUpdateGuard during population
    - _Requirements: 3.1, 3.2, 3.5, 2.7_
  - [x] 6.9 Implement New/Save/Delete CRUD operations
    - New: reset form, clear selection
    - Save: set OwnerUUID, call ColonyViewModel.Save(), refresh list, update title
    - Delete: check ColonyReferenceCounter, prompt or prevent, show "In Use (N)" when refs > 0
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

## Phase 3: Structure Control (ColonyStructureV2)

- [ ] 7. Create ColonyStructureV2 poolable UserControl
  - [ ] 7.1 Create `Forms/ColonyV2/ColonyStructureV2.cs` and `.Designer.cs` with panel layout
    - Panels: flpHeader, rtbStatus, flpWorkers, flpSurveySelection, flpSelection, flpManufacturing, flpTimer, flpStructureCommands
    - _Requirements: 25.2, 6.1_
  - [ ] 7.2 Implement `Reset()` method for pool reuse — clear all fields, hide optional panels, detach ViewModel
    - _Requirements: 24.5 (form-level)_
  - [ ] 7.3 Implement `UpdateData(Blueprint bp)` full repaint — accepts pre-resolved blueprint, sets header, status RTF, worker checkboxes, panel visibility by blueprint type
    - Panel visibility table: MiningRig/Refinery/ResearchLab/Manufactory/CommodityFactory/Other
    - _Requirements: 6.1, 6.2, 6.3, 24.7_
  - [ ] 7.4 Implement `UpdateBackgroundColor()` lightweight path — Yellow (Staged), PaleVioletRed (Built/Offline), LightGreen (Online, missing workers), Green (Online, all workers)
    - _Requirements: 6.3, 24.9 (form-level)_
  - [ ] 7.5 Implement state checkboxes: Built, Online, Staged with mutual exclusion logic
    - Built: IsBuilt=true, IsStaged=false; Online: IsOnline=true, IsBuilt=true, IsStaged=false; Staged: IsStaged=true, IsBuilt=false, IsOnline=false
    - Fire ColonyStructureDataChanged on each change
    - _Requirements: 6.4, 6.5, 6.6, 6.7_
  - [ ] 7.6 Implement worker checkboxes (BlueCollar, WhiteCollar, Specialist) with write-through to AssignedWorkers PropertyBag
    - Toggle calls UpdateBackgroundColor() instead of full UpdateData()
    - Fire ColonyStructureDataChanged with structural=false
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  - [ ] 7.7 Implement unallocated worker checkboxes (disabled, read-only) for structures with UnassignedDetail properties
    - State determined by ColonyStatusCalculator actual status
    - _Requirements: 7.5, 7.6_
  - [ ] 7.8 Implement Up/Down/Delete structure command buttons
    - Delete also triggered by Delete key when control is focused
    - _Requirements: 4.3, 4.4, 4.5, 4.6_

- [ ] 8. Implement Structure_Pool and structure panel in FormColonyV2
  - [ ] 8.1 Implement Structure_Pool: AcquireStructureControl(), ReturnAllToPool() with high-water-mark growth
    - _Requirements: 24.5 (form-level), 4.8_
  - [ ] 8.2 Implement structure panel population: acquire controls from pool, call UpdateData(bp), add to flpStructures with SuspendLayout/ResumeLayout
    - Wire ColonyStructureDataChanged event on each control
    - _Requirements: 4.2, 4.7, 4.8, 24.6 (form-level)_
  - [ ] 8.3 Implement flatpack filter + combo for adding structures; Add button calls ColonyViewModel.AddStructure()
    - _Requirements: 4.1, 4.2_
  - [ ] 8.4 Implement structural change handler: rebuild layout using pool (add/reorder/delete), call RecalculateStatus
    - _Requirements: 4.7, 4.8_
  - [ ] 8.5 Implement non-structural change handler: update only affected control, call RecalculateStructure() for O(1) delta update
    - _Requirements: 4.9, 24.8 (form-level)_
  - [ ] 8.6 Implement status summary panel: Power, Habitation, Food, Entertainment, Warehouse with colored RTF
    - Red when Required > Provided, green otherwise, format "Required/Provided"
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_

- [ ] 9. Implement structure type filter
  - [ ] 9.1 Add lvwStructureTypes checkbox ListView showing all flatpack blueprint types from BaselineData.json, sorted alphabetically
    - Static list — not filtered by current colony
    - _Requirements: 5.1, 5.2_
  - [ ] 9.2 Implement check/uncheck to show/hide ColonyStructureV2 controls of matching type
    - _Requirements: 5.4, 5.5_
  - [ ] 9.3 Persist unchecked types to UIPreferences.json; restore on form load
    - _Requirements: 5.3, 5.6_

- [ ] 10. Checkpoint — Structure control and pool
  - Ensure all tests pass, verify structure controls render correctly with pool reuse
  - Ask the user if questions arise.

## Phase 4: Administration Tab

- [ ] 11. Implement Administration tab
  - [ ] 11.1 Add RichTextBox for admin report, wire ColonyAdminReportBuilder.BuildReport()
    - Refresh on colony selection, ColonyDataChanged, and timer interval (AdminRefreshIntervalSeconds)
    - _Requirements: 21.1, 21.2, 21.3_
  - [ ] 11.2 Add Bootstrap button: call ColonyBootstrap.Bootstrap(), check PlanetName, refresh structures
    - _Requirements: 20.1, 20.2, 20.3, 21.4_
  - [ ] 11.3 Add Optimize button: call BuildOrderOptimizer.Optimize(), replace structure list, refresh display
    - _Requirements: 19.1, 19.2, 19.3, 21.4_
  - [ ] 11.4 Implement admin tab background color for import staleness via TabWarningService
    - No color < 5 days, Yellow 5-6 days, Red 6+ days or never imported
    - _Requirements: 21.5, 22.3_

## Phase 5: Structures Tab — Process Controls

- [ ] 12. Implement mining rig controls in ColonyStructureV2
  - [ ] 12.1 Implement survey selection combo filtered by colony PlanetName; populate resource combo from selected survey
    - _Requirements: 9.1, 9.2, 9.3_
  - [ ] 12.2 Implement Start: create repeating CountDownTime aligned to clock-hour boundary, start countdown timer
    - _Requirements: 9.4_
  - [ ] 12.3 Implement Done: call Colony.ProcessColony() under ProcessingLock, stop timer, clear state
    - _Requirements: 9.6_
  - [ ] 12.4 Implement countdown display with configurable refresh rate; manual edit support (click to edit, leave to parse)
    - _Requirements: 9.5, 15.1, 15.2, 15.3, 15.4_
  - [ ] 12.5 Reset MiningLeftOvers to zero when MiningSurvey or MiningSurveyResource changes
    - _Requirements: 9.7_
  - [ ] 12.6 Display mining progress status (rate, resource, purity); hide controls when not built/online
    - _Requirements: 9.8, 9.9_

- [ ] 13. Implement refinery controls in ColonyStructureV2
  - [ ] 13.1 Populate resource combo with unrefined warehouse resources, actively mined resources, and eligible synthetic recipes
    - Compound key format "ResourceName|Purity" and "ResourceName|Purity|S{Tier}"
    - _Requirements: 10.1, 10.2, 10.6_
  - [ ] 13.2 Implement Start/Done with repeating CountDownTime aligned to clock-hour; display refining progress status
    - _Requirements: 10.3, 10.4, 10.5_

- [ ] 14. Implement research lab controls in ColonyStructureV2
  - [ ] 14.1 Populate blueprint combo: Evolution < 15, passes CanResearchEvolution(), "Can Research" not false
    - _Requirements: 11.1, 11.2_
  - [ ] 14.2 Implement Start: one-shot CountDownTime with ResearchTimeLookup duration, reduced by ResearchFocus skill (3%/level)
    - _Requirements: 11.3_
  - [ ] 14.3 Implement Done: call Colony.ProcessColony() to create evolved blueprint; display evolution status
    - Clear orphaned research state when blueprint UUID is null or blueprint not found
    - _Requirements: 11.4, 11.5, 11.6_

- [ ] 15. Implement manufactory controls in ColonyStructureV2
  - [ ] 15.1 Populate blueprint combo: "Can Manufacture" not false; show quantity input and Stage Resources checkbox
    - Stage Resources visible when no manufacturing running, writes to ColonyStructureViewModel.StagingResources
    - _Requirements: 12.1, 12.2, 12.7_
  - [ ] 15.2 Implement Start: repeating CountDownTime with "Manufacture Run Time", reduced by ProductionFocus skill (3%/level)
    - _Requirements: 12.3_
  - [ ] 15.3 Implement Done: call Colony.ProcessColony(), display progress "(2/5) Mining Rig Ev3", clear on completion
    - Clear orphaned state when blueprint UUID null or not found
    - _Requirements: 12.4, 12.5, 12.6, 12.8_

- [ ] 16. Implement commodity factory controls in ColonyStructureV2
  - [ ] 16.1 Populate commodity combo filtered by blueprint's "Commodity Industry"; show quantity (cycles) and Stage Resources
    - _Requirements: 13.1, 13.2_
  - [ ] 16.2 Implement Start/Done with repeating CountDownTime reduced by ProductionFocus; display cycle progress
    - Clear orphaned state when commodity name is null
    - _Requirements: 13.3, 13.4, 13.5, 13.6, 13.7_

- [ ] 17. Implement structure building controls
  - [ ] 17.1 Show Build button when Staged, not Built, and no other structure currently building
    - _Requirements: 14.1_
  - [ ] 17.2 Implement Build: set IsStaged=false, create BuildCompletionTime via BuildTimeCalculator (accounting for Builder skill)
    - _Requirements: 14.2_
  - [ ] 17.3 Implement building countdown display, "Building..." status, Done button to force complete
    - Disable Built checkbox while build timer active
    - _Requirements: 14.3, 14.4, 14.5_

- [ ] 18. Checkpoint — Process controls
  - Ensure all tests pass, verify mining/refining/research/manufacturing/commodity/build flows work
  - Ask the user if questions arise.

## Phase 6: Workers Tab (Commodity Requests)

- [ ] 19. Implement Workers tab
  - [ ] 19.1 Add DataGridView with columns: Name, Amount (editable), Fulfilled (checkbox), NeedBy (editable countdown format)
    - FullRowSelect mode; Name column click redirects to Amount column
    - _Requirements: 17.1, 17.8_
  - [ ] 19.2 Implement Add: filtered commodity combo, quantity input, optional NeedBy countdown, call ColonyViewModel.AddCommodityRequest()
    - _Requirements: 17.2_
  - [ ] 19.3 Implement in-place edits: Amount writes to CommodityRequested.Requested + WriteContext(); NeedBy parses countdown and updates request.NeedBy
    - _Requirements: 17.3, 17.5_
  - [ ] 19.4 Implement Fulfilled checkbox: set Fulfilled=true, Delivered=Requested, refresh grid with strikethrough styling
    - _Requirements: 17.4_
  - [ ] 19.5 Implement Delete key to remove commodity request via ColonyViewModel.RemoveCommodityRequest()
    - _Requirements: 17.6_
  - [ ] 19.6 Implement auto-cleanup of expired fulfilled requests (> 3 days past NeedBy)
    - _Requirements: 17.7_
  - [ ] 19.7 Implement Workers tab title with active request count "Workers : 3"; display "overdue" for past-due NeedBy
    - _Requirements: 17.9, 17.10_
  - [ ] 19.8 Implement Workers tab background color via TabWarningService.EvaluateWorkerWarning()
    - _Requirements: 22.2_

## Phase 7: Warehousing Tab

- [ ] 20. Implement Warehousing tab
  - [ ] 20.1 Add DataGridView with columns: ItemType, Item (ExtendedName), Locked, Amount (editable integer)
    - _Requirements: 16.1_
  - [ ] 20.2 Implement item type combo with dynamic secondary combo/filter per type
    - Resource: resource combo + purity combo (hide purity for synthetics)
    - Commodity: commodity combo with ExtendedName
    - WorkDetail: BlueCollarDetail/WhiteCollarDetail/SpecialistDetail
    - Survey: survey combo with ExtendedName
    - Blueprint: blueprint combo with ExtendedName
    - ShipPart/ShipHull/Munition/Flatpack/SpaceBuildPackage/Share: blueprints filtered by OutputItemType
    - Each combo has a text filter field
    - _Requirements: 16.2, 16.3, 16.4, 16.5, 16.6, 16.7, 16.8, 16.15_
  - [ ] 20.3 Implement Add: create Item with correct type, BaseItemTypeID, name, purity, quantity, volume; call ColonyViewModel.AddItem()
    - Volume by type: Resource=1, Commodity=10, WorkDetail=50, Blueprint/Survey=0, manufactured items use "Cargo Volume Size"
    - _Requirements: 16.9, 16.10_
  - [ ] 20.4 Implement Delete key: remove item via ColonyViewModel.RemoveItem(); prevent deletion if item has locked quantities with warning message
    - _Requirements: 16.11, 16.12_
  - [ ] 20.5 Implement editable Amount column with integer validation; update item.Quantity and recalculate status on edit
    - _Requirements: 16.13_
  - [ ] 20.6 Display Locked column from Colony.Locks for each item; use in-place row updates instead of clearing all rows
    - _Requirements: 16.14, 24.10 (form-level)_

## Phase 8: Import

- [ ] 21. Implement colony import from game clipboard
  - [ ] 21.1 Implement Import button: validate HTML clipboard, check player selected, detect content type via ClipboardContentDetector
    - Display informational messages for non-HTML, no player, or wrong content type
    - _Requirements: 18.1, 18.2, 18.3, 18.4_
  - [ ] 21.2 Parse clipboard to temp colony via ColonyParser.ParseClipboardToTemp(); find existing colony by planet+system via ColonyImportHelper.FindByPlanet()
    - _Requirements: 18.5, 18.6_
  - [ ] 21.3 Implement merge (existing) and create (new) paths
    - Existing: merge identity, process HTML, preserve ColonyName
    - New: create via ColonyImportHelper.CreateFromTemp() with current player UUID
    - _Requirements: 18.7, 18.8_
  - [ ] 21.4 Post-import: WriteContext(), fire ColonyDataChanged, refresh list, select imported colony, populate form
    - _Requirements: 18.9_
  - [ ] 21.5 Implement Save Clipboard button for debugging; implement error handling with logging
    - _Requirements: 18.10, 18.11_

## Phase 9: Tab Warning Indicators and Owner-Draw TabControl

- [ ] 22. Implement tab warning system
  - [ ] 22.1 Implement owner-draw mode on TabControl for tab background colors with visual styles enabled
    - _Requirements: 22.4_
  - [ ] 22.2 Wire TabWarningService.EvaluateStructureWarning() for Structures tab, EvaluateWorkerWarning() for Workers tab, EvaluateColonyImportStalenessWarning() for Administration tab
    - Update after structure change, commodity request change, or colony selection change
    - _Requirements: 22.1, 22.2, 22.3, 22.5_

## Phase 10: Window State and Event Lifecycle

- [ ] 23. Implement state persistence and event lifecycle
  - [ ] 23.1 Implement WindowStateHelper save/restore for position, size, grid columns, ListView state, structure type filter selections
    - _Requirements: 23.1, 23.2_
  - [ ] 23.2 Subscribe to CurrentPlayerChanged and ColonyDataChanged in constructor; unsubscribe in OnFormClosed
    - _Requirements: 23.3, 23.4_
  - [ ] 23.3 Implement CurrentPlayerChanged handler: clear form state, create blank colony, repopulate colony list
    - _Requirements: 23.8_
  - [ ] 23.4 Implement ColonyDataChanged handler: check if change is for selected colony and not self-triggered; recalculate status, repopulate form, refresh admin report
    - _Requirements: 23.7_
  - [ ] 23.5 Add IsDisposed guards on all event handlers; use BeginInvoke for cross-thread marshaling when InvokeRequired
    - _Requirements: 23.5, 23.6_
  - [ ] 23.6 Wire ProgrammaticUpdateGuard on all programmatic UI updates to prevent cascading handlers
    - _Requirements: 23.9_

- [ ] 24. Checkpoint — Full form integration
  - Ensure all tests pass, verify all tabs populate correctly, event lifecycle is clean
  - Ask the user if questions arise.

## Phase 11: Acceptance Testing and Cutover

- [ ] 25. Verify and cut over
  - [ ] 25.1 Run full test suite — all existing tests must pass
  - [ ] 25.2 Manual testing: import colony from HTML clipboard (new colony + merge existing)
  - [ ] 25.3 Manual testing: structure add/reorder/delete with pool reuse visible (no flicker)
  - [ ] 25.4 Manual testing: mining rig full flow (survey → resource → start → countdown → done)
  - [ ] 25.5 Manual testing: refinery, research lab, manufactory, commodity factory flows
  - [ ] 25.6 Manual testing: warehouse add/edit/delete items across all item types
  - [ ] 25.7 Manual testing: commodity requests add/edit/fulfill/delete/overdue
  - [ ] 25.8 Manual testing: admin report refresh, bootstrap, optimize
  - [ ] 25.9 Manual testing: tab warnings update correctly (structure, worker, staleness)
  - [ ] 25.10 Manual testing: structure type filter persistence across colony switch and app restart
  - [ ] 25.11 Manual testing: window state persistence (position, grids, filters) across app restart
  - [ ] 25.12 Manual testing: verify old FormColony still works identically
  - [ ] 25.13 Cutover: rename menu item, remove old form files (deferred until user approves)

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Service-level fixes (Phase 1) benefit both old and new forms immediately
- Each phase builds on previous phases — no orphaned code
- Existing services remain unchanged: ColonyParser, ColonyImportHelper, BuildOrderOptimizer, ColonyBootstrap, ColonyAdminReportBuilder, TabWarningService, ColonyReferenceCounter
- Checkpoints at tasks 5, 10, 18, and 24 ensure incremental validation
