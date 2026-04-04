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
**Priority: Low**
A master sorted list of active `CountDownTime` instances was discussed. The recommended approach is a computed/transient view (Option 4) that scans all owners at runtime rather than persisting a separate list.
Recommendation: implement as a method on `PlayerContext` or `EmpireContext` that yields all active `CountDownTime` instances sorted by `TimeRemaining`.
**Needs approval: yes — where this view should live and how it should be surfaced in the UI.**

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
