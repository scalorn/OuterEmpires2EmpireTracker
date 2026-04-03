# Open Issues & Recommendations

Items listed here need clarification or approval before work begins.

---

## 1. NLog — Add logging calls to the codebase
**Priority: Low**
NLog is installed and configured (NLog.config exists). No logging calls have been added to any class yet.
Recommendation: decide which areas to instrument first — ColonyStatusCalculator hot path, PlayerContext load/save, and form-level errors are the highest-value targets.
**Needs approval: yes — scope of logging to be defined.**

---

## 2. ActualColonyStructureWorkers.IsUnassignedWorkerAvailable — stub implementation
**Priority: Medium**
The method currently returns `true` unconditionally with a TODO comment:
> "Need to look into the colony's warehouse and verify there is a worker available with the given workerKey"

This means unallocated worker counts are always treated as available regardless of actual colony inventory.
Recommendation: implement using `Colony.Items.CountByType(WorkDetail, workerKey)` minus any locked quantity from `LockTracking`.
**Needs approval: yes — locking model for workers needs to be confirmed.**

---

## 3. LockTracking — not yet wired into Colony
**Priority: Medium**
`LockTracking` is implemented and tested but not yet added to the `Colony` class or serialized. The `dgvItems` "Locked Amount" column is hardcoded to "0".
Recommendation: add `LockTracking Locks { get; set; }` to `Colony`, wire `populateItemGrid` to show real locked quantities, and implement lock/unlock UI.
**Needs approval: yes — UI design for locking workflow needed.**

---

## 4. CountDownTime master list / active timers view
**Priority: Low**
A master sorted list of active `CountDownTime` instances was discussed. The recommended approach is a computed/transient view (Option 4) that scans all owners at runtime rather than persisting a separate list.
Recommendation: implement as a method on `PlayerContext` or `EmpireContext` that yields all active `CountDownTime` instances sorted by `TimeRemaining`.
**Needs approval: yes — where this view should live and how it should be surfaced in the UI.**

---

## 5. ColonyStatusCalculator unit tests
**Priority: Medium**
`ColonyStatusCalculator` has no unit tests. It is the most complex calculation class in the project and has had multiple bugs fixed (entertainment/warehouse accumulator seeds, unallocated worker tracking).
Recommendation: add tests using mock `IColonyStructureWorkers` implementations to verify resource accumulation, worker counting, and the actual vs ideal distinction without requiring a full `EmpireContext`.
**Needs approval: no — can proceed when prioritised.**

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
