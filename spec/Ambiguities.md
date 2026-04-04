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

### AMB-011 — RESOLVED
**Resolution:** A Survey SHALL always have a non-empty PlanetName. A survey without a PlanetName is invalid and cannot be used. The leading-space edge case (SurveyID present but PlanetName empty) is therefore not a valid state. REQ-DM-040 updated to require PlanetName. The ExtendedName getter will still trim defensively.  
**Action:** REQ-DM-040 and REQ-SRV updated.

---

### AMB-012 — RESOLVED
**Resolution:** `ExtendedName` is a computed UI display property that concatenates all relevant identifying fields into a single human-readable string (e.g. Blueprint combines Class, Evolution, Name, TechLevel, NickName). It is intentionally `[JsonIgnore]` on all types because it is derived from other serialized fields and does not need to be stored. This is consistent with REQ-ARCH-041.  
**Action:** REQ-ARCH-041 updated to explicitly mention ExtendedName as an example.

---

## Colony Status Calculation

### AMB-013 — RESOLVED
**Resolution:** The current implementation is correct:
- Each structure's assigned workers (BlueCollar1, WhiteCollar1, etc.) are counted in a local `ColonyWorkers` list per structure. The count is added to HabitationRequired, FoodRequired, and EntertainmentRequired for that structure only.
- Unallocated workers (UnassignedBlueCollarDetail etc.) are tracked via `unallocatedBlueCollarPresent` which propagates through `prevStatus`. The first structure that needs an unallocated worker of a given type adds 1; subsequent structures that also need it see it is already present and do not add again.

**Action:** REQ-COL-017 updated to document this behavior precisely.

---

### AMB-014 — RESOLVED
**Resolution:** `gameSequence` is a per-blueprint-type counter, matching the game UI which numbers structures by type (e.g. two Power Plants are numbered 1 and 2, a Habitation is also numbered 1). This is intentional.  
**Action:** REQ-COL-033 updated to clarify the sequencing rule.

---

### AMB-015 — RESOLVED
**Resolution:** The Power label SHALL show red when PowerRequired > PowerProvided, consistent with all other resources. The required number is already colored correctly by AppendStatus — only the label color was wrong.  
**Action:** populateStatus fixed. Colony.md updated.

---

## Architecture / MVVM

### AMB-016 — RESOLVED
**Resolution:** The MVVM pattern SHALL be applied to all forms. No form SHALL directly access PropertyBag, data lists, or data object internals. All such access SHALL go through a ViewModel.  
**Action:** REQ-ARCH-010 and REQ-ARCH-014 updated. GOALS.md updated with MVVM completion as a priority task.

---

### AMB-017 — RESOLVED
**Resolution:** MVVM completion across all forms SHALL be done before any additional feature work. This is the highest priority task after the current ambiguity resolution pass.  
**Action:** GOALS.md updated with MVVM completion task and priority ordering.

---

### AMB-018 — RESOLVED
**Resolution:** The Requested quantity SHALL be specified when adding a commodity request (from the quantity field). Existing requests SHALL be editable in-place via the grid (CellValueChanged already handles this). The ViewModel's AddCommodityRequest SHALL accept the quantity so the form doesn't bypass it.  
**Action:** ColonyViewModel.AddCommodityRequest updated to accept quantity. Colony.md updated.

---

## Overall.md Items Not Yet in Formal Requirements

### AMB-019 — RESOLVED
**Resolution:** The processing order was already formalized as REQ-ARCH-080 through REQ-ARCH-083 when AMB-008 was resolved. No further action needed.

---

### AMB-020 — RESOLVED
**Resolution:** The colony bootstrap algorithm generates a foundation set of structures from surveys for a planet. See REQ-COL-096 series in Colony.md for the full specification.  
**Action:** Colony.md updated with REQ-COL-096 series.

---

### AMB-021 — RESOLVED
**Resolution:** The optimized flatpack build order algorithm is:
1. Take all planned (staged/unbuilt) flatpacks and separate them into two groups:
   - **Support structures**: Power, Habitation, Food, Entertainment providers
   - **Primary structures**: everything else (in their planned build order)
2. Walk through the primary structures in order. Before each primary structure is added to the output sequence, check whether building it (with all its workers fully staffed) would cause a deficit in Power, Habitation, Food, or Entertainment.
3. If a deficit would occur, insert the minimum required support structures from the support group ahead of the primary structure to satisfy the constraint.
4. The result is a reordered sequence where all resource constraints are satisfied after each build step.

**Action:** Colony.md updated with REQ-COL-095 series for the optimization algorithm.

---

### AMB-022 — RESOLVED
**Resolution:**
1. `BlueprintType` SHALL have an `OutputItemType` string field that records what `ItemType` is produced when a blueprint of this type is manufactured. The value SHALL be the `ItemType.ItemTypeEnum` name as a human-readable string (e.g. `"ShipHull"`, `"ShipPart"`, `"Flatpack"`). This field SHALL be populated in `BaselineData.json`.
2. `Item.ItemType` (and all `ItemType.ItemTypeEnum` fields) SHALL serialize as the enum name string, not as an integer. This requires adding `[JsonConverter(typeof(StringEnumConverter))]` to the `ItemType` property on `Item`.

**Action:** BlueprintType.cs updated. Item.cs updated with StringEnumConverter. BaselineData.json needs OutputItemType populated for each BlueprintType. DataModel.md updated.

---

### AMB-023 — RESOLVED
**Resolution:** There is no background processing currently. All processing (Colony.ProcessColony, etc.) runs on the UI thread triggered by user actions. Background processing is a future feature. When implemented, a data locking strategy will need to be designed to prevent concurrent modification between the UI and background processing. This is deferred until the feature is prioritized.  
**Action:** No code change. Noted as future work.


---

### AMB-024 — RESOLVED
**Resolution:** Item volume by type:
- Blueprint: 0
- Survey: 0
- Resource: 1
- Commodity: 10
- WorkDetail: 50
- Manufactured items (Flatpack, ShipHull, ShipPart, etc.): from blueprint's CargoVolumeSize property

Volume SHALL be set when an item is added to the warehouse.  
**Action:** REQ-DM-025 updated. FormColony.cmdAdd_Click updated to set Volume on creation.


---

### AMB-025 — RESOLVED
**Resolution:** Option C — accept data loss. The existing PlayerData.json test data will be updated manually by the user. No migration code needed.  
**Action:** No code change.
