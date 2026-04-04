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

## 11. Commodity Manufacturing support in Colony Form
**Priority: Medium — Major Feature**
A new class of flatpack structures for manufacturing commodities. Requires:
- New BlueprintType(s) for commodity manufacturing flatpacks
- Data model work to define commodity recipes (input resources, output commodity, rates)
- Colony structure UI handling similar to existing Manufactory but producing Commodity items
- Integration with existing commodity request system
- Needs design discussion before implementation to determine data structure and recipe format.
**Needs approval: yes — requires data design and recipe specification.**


---

## 12. Code Quality — scan findings
**Priority: Low-Medium — Incremental cleanup**
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
- Worker type strings: `"BlueCollar"`, `"WhiteCollar"`, `"Specialist"`, `"BlueCollarDetail"`, `"WhiteCollarDetail"`, `"SpecialistDetail"`
- State strings: `"Built"`, `"Staged"`, `"Online"`, `"Actual"`, `"Ideal"`, `"Refined"`
- Numbers: `25` (refining base rate), `50` (worker volume), `3600` (seconds/hour)

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
- `UpdateData` in ColonyStructure.cs (~183 lines) — worker checkbox setup could be extracted
- `populateForm` in FormColony.cs (~140 lines) — structure control setup could be extracted
- `CalculateBuilt` per-structure overload in ColonyStatusCalculator.cs (~250 lines) — worker parsing is 3 near-identical blocks
- `ProcessColony` in Colony.cs (~200 lines) — already partially extracted per structure type
- `processHtml` in SurveyParser.cs (~200 lines)

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

**Needs approval: yes — user should pick which items to tackle and in what order.**
