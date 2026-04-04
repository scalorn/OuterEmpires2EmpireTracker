# Open Issues & Recommendations

Items listed here need clarification or approval before work begins.

---

## 1. NLog — Add logging calls to the codebase
**Priority: Low**
NLog is installed and configured (NLog.config exists). No logging calls have been added to any class yet.
Recommendation: decide which areas to instrument first — ColonyStatusCalculator hot path, PlayerContext load/save, and form-level errors are the highest-value targets.
**Needs approval: yes — scope of logging to be defined.**

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

## 6. cmdDelete in FormPlayerProfile — no confirmation dialog
**Priority: Low**
Deleting a player profile is immediate with no undo. A confirmation dialog (`MessageBox.Show`) would prevent accidental deletion.
**Needs approval: yes — UX decision.**

---

## 7. Resource static data — no unit tests
**Priority: Low**
`Resource.cs` follows the same static list pattern as `Commodity`, `ResourceGroup`, etc. but has no unit tests yet.
**Needs approval: no — can proceed when prioritised.**

---

## 8. SurveyParser — no unit tests
**Priority: Low**
`SurveyParser.cs` parses survey data but has no test coverage.
**Needs approval: no — can proceed when prioritised.**
