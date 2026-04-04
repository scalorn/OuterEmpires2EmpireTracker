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

## 9. Grid validation for SurveyForm
**Priority: Medium**
Currently no format validation on SurveyForm dgvResources grid cell edits:
- Resource column: name must exist in Resource static data (use combo box or CellValidating)
- Amount column: must be a decimal number (use DataGridViewValidatedTextBoxColumn with DECIMAL_VALIDATION)
**Needs approval: no — can proceed when prioritised.**

---

## 10. Grid validation for BlueprintForm
**Priority: Medium**
Currently no format validation on BlueprintForm grid cell edits:
- dgvStatistics: format varies by property name (time values like ManufactureTime, numbers, percentages)
- dgvResources: Resource name must exist in Resource static data, quantity must be an integer
- Consider using DataGridViewValidatedTextBoxColumn with property-specific validation patterns
**Needs approval: no — can proceed when prioritised.**


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
