# Ambiguities

Items that need clarification before they can be implemented or tested with confidence.
Each item references the relevant requirement ID where one exists.

---

## Data Model

### AMB-001 — RESOLVED
**Resolution:** When the countdown has expired (TimeRemaining <= 0), TimeRemainingString SHALL return `"0s"`.  
**Action:** REQ-DM-052 updated. Code fixed in CountDownTime.cs.

---

### AMB-002 — RESOLVED
**Resolution:** Once the highest non-zero segment has been included, all lower segments SHALL be shown even if their value is zero. Leading zero segments (before the first non-zero segment) are still omitted. Example: `1h 0m 30s` shows `0m`; `30m 0s` shows `0s`.  
**Action:** REQ-DM-052 updated. Code fixed in CountDownTime.cs.

---

### AMB-003 — RESOLVED
**Resolution:** `StartTime`, `EndTime`, and `RepeatIntervalSeconds` SHALL be persisted to JSON so that active countdowns survive app restarts. `TimeRemaining` and `IntervalsPassed` SHALL remain `[JsonIgnore]` as they are computed from the persisted fields. The current code is already correct — no code change needed.  
**Action:** REQ-DM-054 updated to clarify which fields are serialized vs ignored.

---

### AMB-004 — RESOLVED
**Resolution:**
- `CurrentAttitude`, `ContentmentIndex`, `WageLevel`, `WageAdjustmentTime` — incomplete features, leave as-is. No action.
- `Power`, `Habitation`, `Food`, `Entertainment`, `WarehouseCapacity`, `WorkersAssigned` on `ColonyStructure` — these are dead code. The correct location for these values is `ColonyStructureStatus` (accessed via `structure.Statuses["Actual"]` and `structure.Statuses["Ideal"]`). These fields SHALL be removed from `ColonyStructure`.
- `buildQueueSequence` — see AMB-005.

**Action:** Dead fields removed from ColonyStructure. Colony.md updated.

---

### AMB-005 — RESOLVED
**Resolution:** `buildQueueSequence` is an intentional field for explicitly tracking the order in which structures are queued to be built, independent of their display order in the list. It is a placeholder for the Flatpack Building Form feature (REQ-ARCH-065). The field SHALL be retained and serialized. No code change needed now.  
**Action:** Colony.md updated with a requirement for buildQueueSequence.

---

### AMB-006 — RESOLVED
**Resolution:** `CommodityRequested` represents a colony's request for a commodity delivery. It SHALL have Name, Requested (quantity), Delivered (quantity), NeedBy (DateTime), and Fulfilled (bool) — all public and serialized. NeedBy SHALL be user-enterable in the Colony Form. Fulfilled SHALL be set when Delivered >= Requested. The Commodity Delivery Form (REQ-ARCH-064) will aggregate open (unfulfilled) requests across all colonies.  
**Action:** CommodityRequested fields made public and serialized. Colony.md and DataModel.md updated. Colony Form requirements updated to include NeedBy entry.

---

### AMB-007 — RESOLVED
**Resolution:**
- The fractional leftover accumulation is correct. Survey amounts are fractional; items are integer quantities. MiningLeftOvers accumulates the fractional remainder across cycles.
- MiningLeftOvers SHALL be reset to 0 when the mining rig's MiningSurvey or MiningSurveyResource changes (i.e. the user selects a different resource to mine).
- The Extraction Focus skill provides a 1% bonus per skill level to mined quantity. This is currently a TODO in Colony.ProcessColony().
- Implementing the extraction bonus requires multi-player support: a Colony SHALL be owned by a Player, and the owning player's Extraction Focus skill level SHALL be used to calculate the bonus. This is a larger feature tracked as a new requirement.

**Action:** Colony.cs fixed to reset MiningLeftOvers on resource change. Colony.md updated. New multi-player requirements added to Architecture.md.

---

### AMB-008 — RESOLVED
**Resolution:** Colony.ProcessColony() currently only handles MiningRig. The remaining processing types (Structure Building, Refining, Manufacturing, Research) are planned features not yet implemented. The processing order is defined in Overall.md and SHALL be formalized as requirements.  
**Action:** Architecture.md updated with the full processing order as future requirements.

---

### AMB-009 — RESOLVED
**Resolution:** WarehouseRequired SHALL be calculated as the sum of `item.Quantity * item.Volume` across all items in the colony's ItemBag. `Item` needs a `Volume` property (double, default 0). The calculator SHALL pass the colony's ItemBag to CalculateBuilt so it can compute the total.  
**Action:** Item.Volume added. ColonyStatusCalculator updated. REQ-COL-017 and REQ-DM updated.

---

### AMB-010 — RESOLVED
**Resolution:** An unassigned worker is a WorkDetail item sitting in the colony warehouse (ItemBag) that is unlocked (not locked by LockTracking). One unassigned worker of a given type supports all structures in the colony that need that type — it is not consumed per structure.

`IsUnassignedWorkerAvailable(workerKey)` SHALL check whether the colony warehouse contains at least 1 unlocked WorkDetail item whose BaseItemTypeID equals the full WorkerDetail ID (e.g. `"BlueCollarDetail"`, `"WhiteCollarDetail"`, `"SpecialistDetail"`).

The short-form keys `"BlueCollar"`, `"WhiteCollar"`, `"Specialist"` currently passed to `IsUnassignedWorkerAvailable` are incorrect and SHALL be changed to the full WorkerDetail IDs.

When a worker is assigned to a specific structure slot (BlueCollar1, WhiteCollar1, etc.), the corresponding WorkDetail item in the warehouse SHALL be locked via LockTracking using the structure's UUID as the process key.

**Action:** IColonyStructureWorkers.cs updated. ColonyStatusCalculator call sites updated. REQ-ARCH-062 updated. Colony.md updated.

---

### AMB-011 — Survey.ExtendedName: leading space when PlanetName is empty but SurveyID is set
**Code behavior:** If PlanetName is null/empty but SurveyID is set, ExtendedName returns `" (SurveyID)"` with a leading space.  
**Spec (REQ-DM-040/041):** Does not address this edge case.  
**Question:** Should the leading space be trimmed? Should SurveyID only be shown when PlanetName is also present?

---

### AMB-012 — Commodity.ExtendedName is [JsonIgnore] but the spec does not mention this
**Code behavior:** `Commodity.ExtendedName` has `[JsonIgnore]`.  
**Spec (REQ-ARCH-041):** Says computed properties SHALL be `[JsonIgnore]` — this is consistent.  
**Clarification needed:** Confirm that `Commodity.ExtendedName` is intentionally excluded from JSON (it is computed from Name + Industry + Group which are all serialized).

---

## Colony Status Calculation

### AMB-013 — What does "worker count" mean for Required calculations?
**Code behavior:** `HabitationRequired`, `FoodRequired`, and `EntertainmentRequired` are each incremented by `ColonyWorkers.Count + unallocatedWorkersAdded` per structure. This means a colony with 3 structures each having 2 workers would have Required = 6 per resource, not 2.  
**Spec (REQ-COL-017):** Says "worker counts SHALL contribute to HabitationRequired, FoodRequired, and EntertainmentRequired" — but does not specify whether this is per-structure or cumulative.  
**Question:** Is the current cumulative behavior correct? Or should Required be the total number of workers in the colony (not summed per structure)?

---

### AMB-014 — gameSequence is per blueprint type, not per structure
**Code behavior:** `gameSequence` is assigned as a count of how many structures of the same `BluePrintType` have been seen so far. So two Power Plants would be numbered 1 and 2, and a Habitation would also be numbered 1.  
**Question:** Is this the intended behavior? The spec (REQ-COL-033) says "sequence number" without defining what it sequences.

---

### AMB-015 — ColonyStatusCalculator.populateStatus color logic for Power
**Code behavior:** Power label is always black regardless of deficit. All other resources use red/green.  
**Question:** Is this intentional? Should Power also show red when PowerRequired > PowerProvided?

---

## Architecture / MVVM

### AMB-016 — FormColony still has direct selectedColony references after MVVM refactor
**Code behavior:** `selectedColony` field still exists and is used directly in `structures_ColonyStructureDataChanged` (e.g. `selectedColony.Structures.Contains(kv.Key)`).  
**Spec (REQ-COL-091):** Says all Colony.Structures manipulation SHALL go through ColonyViewModel.  
**Question:** Should `selectedColony` be removed entirely and all access go through `colonyViewModel.Data`? Or is direct read access to the data object acceptable as long as mutations go through the ViewModel?

---

### AMB-017 — MVVM pattern not yet applied to FormPlayerProfile, FormBlueprint, FormSurvey
**Spec (REQ-ARCH-014):** Says the MVVM pattern SHALL serve as a template for other forms.  
**Question:** What is the priority order for applying MVVM to the remaining forms? Should this be done before or after other feature work?

---

### AMB-018 — ColonyViewModel.AddCommodityRequest ignores the Requested quantity
**Code behavior:** `ColonyViewModel.AddCommodityRequest(commodityName)` creates a request with `Requested=0`. The form then sets `request.Requested = qty` directly on the returned object — bypassing the ViewModel.  
**Spec (REQ-COL-091):** All Colony.Commodities manipulation SHALL go through ColonyViewModel.  
**Question:** Should `AddCommodityRequest` accept a quantity parameter, or should there be a separate `SetRequestedQuantity(request, qty)` method on the ViewModel?

---

## Overall.md Items Not Yet in Formal Requirements

### AMB-019 — Colony timed processing order not formalized
**Overall.md:** Lists 7 processing types in order (Building, Mining, Refining x2, Manufacturing, Research). Only Mining is implemented.  
**Question:** Should the processing order be added as formal requirements now, even though only Mining is implemented? This would prevent future implementations from getting the order wrong.

---

### AMB-020 — Colony auto-fill / bootstrap case not specified
**Overall.md:** "Need the ability to auto fillout a colony based on available surveys (bootstrap case)."  
**Question:** What does "auto fillout" mean exactly? Does it mean auto-select the best survey for each resource type? Auto-add the optimal set of flatpack structures? Both? This needs a design before it can become a requirement.

---

### AMB-021 — Colony flatpack ordering optimization not specified
**Overall.md:** "Need the ability to optimize flatpack ordering to meet needs ideally."  
**Question:** What is the optimization goal? Minimize power deficit? Maximize resource output? This needs a clear objective function before it can be designed.

---

### AMB-022 — Manufacturing structure: blueprint does not contain output item information
**Overall.md:** "Not sure the blueprint has the necessary information to create the resulting item."  
**Question:** Does the game's blueprint HTML include the output item type and quantity for manufacturing blueprints? If not, where does this information come from? This blocks the Manufacturing structure implementation.

---

### AMB-023 — Data locking between UI and background processing
**Overall.md:** "Need to be able to lock the colony between the form and background processing."  
**Question:** Is background processing currently running on a separate thread? If so, what is the threading model — timer-based on the UI thread, or a background Task? The locking strategy depends on this.


---

### AMB-024 — Item.Volume: where does the volume value come from?
**Context:** Item.Volume was added (REQ-DM-025) to support WarehouseRequired calculation. All existing items default to Volume=0, so WarehouseRequired will be 0 until volumes are populated.  
**Question:** Where does the volume value for an item come from?
- Is it stored in the blueprint's `CargoVolumeSize` property and should be copied to the item when it is added to the warehouse?
- Is it a static lookup table per resource/commodity type?
- Is it entered manually by the user?
- Does it vary by item type (e.g. resources have volume, work details do not)?
