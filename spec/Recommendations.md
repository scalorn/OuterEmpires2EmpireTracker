# Open Issues & Recommendations

Items listed here need clarification or approval before work begins.

---

## ~~1. NLog — Add logging calls to the codebase~~
**Status: Complete**
Added NLog loggers and replaced all `Debug.Print` calls across the codebase:
- PlayerContext: Info-level load/save with counts, file path
- EmpireContext: Info-level load with baseline data counts
- FormColony, FormBlueprint, FormSurvey: Debug-level selection/populate logging
- DataEntryGridView: Debug-level key/selection logging
- Commodity: Warn-level data validation messages
- SurveyParser: Error-level exception logging in catch blocks
- Removed all `using System.Diagnostics` imports where no longer needed

---

## ~~2. ActualColonyStructureWorkers.IsUnassignedWorkerAvailable — stub implementation~~
**Status: Complete**
Implemented using `Colony.Items.CountByType(WorkDetail, workerKey)` minus `Colony.Locks.GetLockedQuantity()`. Returns true only when unlocked workers are available. Covered by 5 unit tests in ColonyStatusCalculatorTests.

---

## ~~3. LockTracking — not yet wired into Colony~~
**Status: Complete**
`Colony.Locks` property exists and serializes. `populateItemGrid` in FormColony now shows real locked quantities via `colonyViewModel.Data.Locks.GetLockedQuantity()`.

---

## 4. CountDownTime master list / active timers view
**Priority: Medium — Major Feature**
A master sorted list of active `CountDownTime` instances with UI, data locking, and a background processing thread. The `PlayerContext.ActiveCountdowns` property already provides the computed view, but the full feature requires:
- A dedicated form/panel to display active timers sorted by time remaining
- Background thread to tick timers and trigger processing (e.g. mining cycle completion)
- Data locking to prevent concurrent modification during background processing
- Integration with Colony.ProcessColony() for automated resource generation

This is on par with the other large features (colony bootstrap, flatpack optimization, multi-player, commodity delivery) and should be planned as a full feature spec.
**Needs approval: yes — should be specced as a feature before implementation.**

---

## ~~5. ColonyStatusCalculator unit tests~~
**Status: Complete**
34 tests added covering: power/habitation/food/entertainment/warehouse accumulation, online vs offline, previous status chaining, actual vs ideal workers, unallocated worker tracking with lock integration, null blueprint safety, combined scenarios, and all IColonyStructureWorkers implementations.

---

## ~~6. cmdDelete in FormPlayerProfile — no confirmation dialog~~
**Status: Complete**
Added MessageBox.Show confirmation ("Delete profile '{name}'?") with Yes/No buttons before deleting. Also guards against deleting when no profile is selected.

---

## ~~7. Resource static data — no unit tests~~
**Status: Complete**
17 tests added: list integrity, enum coverage, no duplicates, alphabetical sort, map lookups, round-trip, ResourceGroup/ResourceClass validation, synthetic group verification.

---

## ~~8. SurveyParser — no unit tests~~
**Status: Complete**
SurveyParser refactored from debug-only code to a proper `processHtml(Survey, string)` method following the BlueprintScanner pattern. Extracts DateTime, ScannedBy, and resources (name, purity, amount) from game HTML. 17 tests covering ParseDescription, ParseResource, full HTML integration, edge cases (empty, malformed, null, unknown trace, duplicates).


---

## ~~9. Grid validation for SurveyForm~~
**Status: Complete**
- Resource column: combo box bound to Resource static data (validates by selection)
- Purity column: combo box bound to ResourcePurity static data
- Amount column: DataGridViewValidatedTextBoxColumn with DECIMAL_PATTERN
- CellValidating prevents leaving with invalid values, empty=0

---

## ~~10. Grid validation for BlueprintForm~~
**Status: Complete**
- dgvStatistics: CellValidating with type-specific patterns from BlueprintPropertyValidation (54 properties)
- dgvResources Resource column: combo box bound to Resource static data (validates by selection)
- dgvResources Amount column: DataGridViewValidatedTextBoxColumn with NUMBER_VALIDATION + CellValidating


---

## ~~11. Commodity Manufacturing support in Colony Form~~
**Status: Complete**
Commodity Factory (`Flatpacks/CommodityFactory`) implemented:
- Selection combo filtered by blueprint's `CommodityIndustry` property
- `ManufacturingCommodityName` field on ColonyStructure for commodity identification
- 10 commodities per cycle, 10-minute cycle time (constants in GameConstants)
- Quantity input = number of cycles
- Resource consumption from `Commodity.ConstructionResources` per cycle
- Resource locking for entire remaining run via `LockCommodityFactoryResources`
- Commodities stack in warehouse on completion
- Done button completes one cycle, keeps timer for remaining
- ProductionFocus skill reduction deferred until timer processing (Rec #4)


---

## ~~12. Code Quality — scan findings~~
**Status: Complete (all 7 sub-items)**
Full codebase scan identified issues in 7 categories. Lower-risk items can be done incrementally; larger refactors should be planned carefully.

### 12a. Dead code removal (Low risk)
**Status: Complete**
- Removed commented-out `//PowerProvided`, `//PowerRequired` fields and assignments in ColonyStructure.cs
- Removed commented-out structure creation, layout traces, and old code in FormColony.cs
- Removed unused `using` statements: `static AxHost`, `static VisualStyleElement.ListView` (ColonyStructure.cs), `Amazon.Runtime.Internal.Transform`, `static VisualStyleElement`, `System.Windows.Forms`, `System.Text`, `System.Threading.Tasks` (ColonyStatusCalculator.cs), `static Item`, `static VisualStyleElement.Tab`, `System.Text`, `System.Threading.Tasks` (Colony.cs), `System.Collections`, `System.Data`, `System.Reflection`, `System.Windows.Forms.VisualStyles`, `System.Xml.Linq` (FormColony.cs)

### 12b. Extract shared ProgrammaticUpdateGuard (Low risk)
**Status: Complete**
- Created `IProgrammaticUpdateSource` interface and shared `ProgrammaticUpdateGuard` class in `Controls/ProgrammaticUpdateGuard.cs`
- ColonyStructure and FormColony now implement `IProgrammaticUpdateSource` with `BeginProgrammaticUpdate()`/`EndProgrammaticUpdate()`
- Removed duplicated nested `ProgramaticUpdateGuard` classes from both files
- Fixed typo: `ProgramaticUpdateGuard` → `ProgrammaticUpdateGuard` throughout

### 12c. Magic strings/numbers → constants (Low-Medium risk)
**Status: Complete**
Created `Constants/GameConstants.cs` with:
- `RefiningBaseRate` (25), `WorkerVolume` (50), `SecondsPerHour` (3600)
- `PropBuilt`, `PropStaged`, `PropOnline` (structure property keys)
- `StatusActual`, `StatusIdeal` (status dictionary keys)
- `PurityRefined` (resource purity value)

Replaced all occurrences across 9 production files. Test files left with literal values (they test the data, not the constants).

### 12d. Method naming — camelCase → PascalCase (Medium risk)
**Status: Complete**
Renamed all camelCase methods to PascalCase across the codebase:
- Form methods: `populateForm`, `populateItemGrid`, `populateStats`, `populateListView`, `populateCommodityRequestGrid`, `populateResources`, `populateFormFromViewModel`, `clearForm` and all `populate*`/`update*` variants
- Context methods: `findBlueprint`, `findSurvey`, `findColony`, `findBlueprintType`, `findShipClass`, `findTechLevel`, `findEvolution`, `initBlueprints`, `initSurveys`, `initResources`
- Control methods: `findPreviousCell`, `findNextCell`
- Data methods: `getResources`
- Status method: `populateStatus`
- All callers updated across 15+ files. `processHtml`/`ProcessHtml` was already PascalCase from a prior session.

### 12e. Large method extraction (Medium-High risk)
**Status: Complete**
- `ProcessColony` in Colony.cs: Extracted inline mining rig processing into `ProcessMiningRig()` — now all 4 structure types have their own method
- `CalculateBuilt` (per-structure) in ColonyStatusCalculator.cs: Collapsed 5 resource accumulation blocks (Power/Habitation/Food/Entertainment/Warehouse) into compact calls using `GetBlueprintDouble()` helper
- `UpdateData` in ColonyStructure.cs: Already reduced to ~100 lines by 12f worker deduplication — no further extraction needed
- `PopulateForm` in FormColony.cs: Already reduced to ~100 lines by 12a dead code removal — no further extraction needed
- `ProcessHtml` in SurveyParser.cs: Already well-structured at ~80 lines with extracted ParseDescription/ParseTitle/ParseResource helpers

### 12f. Duplicate worker parsing (Medium risk)
**Status: Complete**
- Created `WorkerTypeInfo` class and `WorkerDetail.WorkerTypes` static array defining BlueCollar/WhiteCollar/Specialist with DetailKey, WorkerPrefix, DisplayName, UnassignedKey
- `ColonyStructure.UpdateData`: Replaced 6 near-identical blocks (3 assigned + 3 unallocated) with 2 `foreach` loops over `WorkerTypes`
- `ColonyStatusCalculator.CalculateBuilt`: Replaced 6 near-identical blocks (3 assignment parsing + 3 unassigned checks) with 1 `foreach` loop, plus refactored unallocated availability check into a loop
- Added `GetUnallocatedPresent`/`SetUnallocatedPresent` helpers on `ColonyStructureStatus` to access per-type booleans by key
- `LockAssignedWorkers` already used the shared pattern via `LockWorkerType` — no change needed

### 12g. Large file splitting (Medium-High risk)
**Status: Complete**
- Extracted `ColonyStructure` class from Colony.cs into `Baseline/ColonyStructure.cs`
- Extracted `CommodityRequested` class from Colony.cs into `Baseline/CommodityRequested.cs`
- Colony.cs now contains only the `Colony` class
- Removed unused `Newtonsoft.Json` using from Colony.cs
- ColonyStructure.cs (1400+ lines) and FormColony.cs (1100+ lines) remain large but are cohesive single-class files — no further split needed


---

## ~~13. Multi-Player Support (REQ-ARCH-070-075)~~
**Status: Complete**
All 4 phases implemented:
- Phase 1: OwnerUUID on Colony/Blueprint/Survey, CurrentPlayerUUID on PlayerContext/PlayerRoot, CurrentPlayerChanged event, auto-migration on load
- Phase 2: Player dropdown on MainWindow menu bar, restores last selected player on startup
- Phase 3: Colony/Blueprint/Survey forms filter by current player, subscribe to CurrentPlayerChanged, auto-assign OwnerUUID on save
- Phase 4: Skill multipliers in Colony.ProcessColony — ExtractionFocus (+1%/lvl mining), RefiningFocus (+2%/lvl refining), ProductionFocus/Builder/ResearchFocus time reductions deferred until timer processing is implemented (Rec #4)
- Transfer UI deferred to later phase


---

## ~~14. Global Blueprints (BaselineData.json)~~
**Status: Complete**
- Global blueprints stored in `BaselineRoot.Blueprint[]` in BaselineData.json
- `EmpireContext.globalBlueprintList` holds them in memory
- `chkGlobalBlueprint` on Blueprint Form toggles between global and player-specific
- Save moves blueprint between global list and player list as needed
- `FindBlueprint` searches player list first, then global list
- `GetFilteredBlueprints` merges global + current player blueprints
- Global blueprints have empty OwnerUUID; player blueprints have the player's UUID
- Editable by all players (may be gated behind permission in future)

### Future enhancements (recorded for later):
- Visual indicator in blueprint list to distinguish global vs player-specific
- Permission/setting to gate global blueprint editing
- Separate file for global blueprints if BaselineData.json grows too large


---

## 15. Delivery Routes & Planning (REQ-DEL-001-062)
**Priority: High — Major Feature (phased)**
See [Delivery.md](requirements/Delivery.md) for full requirements.

### Phase 1: System Name Tracking (REQ-DEL-001-005) — Complete
### Phase 2: Route Builder Data Model (REQ-DEL-010-015) — Complete
### Phase 3: Route Builder UI (REQ-DEL-020-025) — Complete
### Phase 4: Delivery Plan Data Model (REQ-DEL-030-035) — Complete

### Phase 5: Delivery Planning UI (REQ-DEL-040-045) — Complete

### Phase 6: Delivery Execution Form (REQ-DEL-050-057) — Complete
- Separate form showing stop-by-stop delivery
- Consolidated load list at top
- Per-stop drop-off/pick-up with checkboxes
- Commodity fulfillment on delivery check (REQ-DEL-054): sets CommodityRequested.Delivered = Requested, Fulfilled = true
- Commodity unfulfillment on uncheck: reverses to Delivered = 0, Fulfilled = false
- Flatpack staging (REQ-DEL-055) deferred to future phase

### Phase 7: Auto-Fill Delivery Plans (REQ-DEL-060-062) — Partial (Commodities only)
- Auto-Fill button on plan tab with request type checkboxes
- Commodities enabled: scans each stop's colony for unfulfilled CommodityRequested, adds drop-off items for shortfall
- Flatpacks, Resources for Manufacturing, Workers: disabled (Future)
- Additive behavior, drop-off only

### Phases 7-9: Future
- Auto-fill delivery plans by request type
- Ship integration (cargo capacity)
- Space station hub modeling

**Phases 1-6 complete. Phase 7+ are future work.**


---

## ~~16. Forms Steering Compliance Audit~~
**Status: Complete**
Audit of all forms against `.kiro/steering/forms.md`. All forms now implement `IProgrammaticUpdateSource`:

- FormBlueprint: guard in UpdatePropertyGrid/ClearForm/PopulateResources, guard checks in CellValidating/SelectionChanged
- FormSurvey: guard in ClearForm/PopulateFormFromViewModel, guard check in CellValidating
- FormDeliveryRoute: guard in PopulateStopsGrid/PopulatePlanGrids/ClearPlanGrids, guard check in dgvStops_SelectionChanged
- FormDeliveryExecution: guard in BuildExecution/ClearExecution
- FormPlayerProfile: added CurrentPlayerChanged subscription + handler
- MainWindow: added interface (no grids, establishes pattern)
- FormColony + ColonyStructure: already compliant
