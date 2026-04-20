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
- Each structure's assigned workers (BlueCollar1, WhiteCollar1, etc.) are counted in a local `ColonyWorkers` list per structure. The count is added to HabitationRequired and FoodRequired (1 per worker), and EntertainmentRequired (2 per worker) for that structure only.
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
**Resolution:** The optimized flatpack build order algorithm (implemented in `BuildOrderOptimizer`):
1. Classify all structures as Support (Power, Habitation, Food, Entertainment providers) or Primary (everything else). CC is classified as Support.
2. Bootstrap: seed with CC, Reactor, Hab Block, Hydroponics Bay, Entertainment Centre.
3. For each primary in order:
   a. Fix existing deficits (power > hab > food > ent priority).
   b. Fix deficits the primary itself would cause.
   c. Look ahead: simulate primary + hab + hydro. Fix hab/food/ent deficits from worker cascading.
   d. Final deficit check after all look-ahead placements.
   e. Place the primary.
4. Append leftover support with deficit checks.
5. When support structures cause cascading deficits (Ent needs power, Hydro needs ent), recursively place prerequisites first. Chain: Hydro -> Ent -> Reactor -> done (max depth 3).
6. Create new structures from player blueprints when pool is exhausted, respecting MaxPerColony.
7. Entertainment required is 2 per worker (hab and food are 1 per worker).

The algorithm optimizes ALL structures (built and unbuilt) because the colony importer groups them by flatpack type, not by actual build order.

**Action:** Colony.md updated with REQ-COL-095 series. BuildOrderOptimizer implemented with 2 tests.

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


---

## Post-Resolution Review (new items found during final cross-reference)

### AMB-026 — RESOLVED
**Resolution:** OutputItemType data has been populated in BaselineData.json by the user.  
**Action:** No code change needed.

---

### AMB-027 — RESOLVED
**Resolution:** `Colony.Locks` (LockTracking) was added to the Colony class and initializes correctly. It serializes via `LockTrackingJsonConverter` — an empty `LockTracking` serializes as `{}` and deserializes back to an empty instance. However, nothing currently writes to `Locks` — the worker assignment UI does not call `LockItem` or `ClearLocksForProcess` when workers are assigned/unassigned. This is expected: REQ-ARCH-062b and REQ-ARCH-062c specify the locking behavior but are listed as future work. The `Locks` field is a correctly wired placeholder waiting for the locking UI to be implemented.  
**Action:** No code change needed. Locking UI is tracked under REQ-ARCH-062b/c.

---

### AMB-028 — RESOLVED
**Resolution:** Dead code removed from `cmdSave_Click`. The local `colony` variable, `selectedColony` null check, and UUID assignment were all superseded by `colonyViewModel.Save()` which handles UUID generation and persistence.  
**Action:** FormColony.cs cleaned up.

---

### AMB-029 — RESOLVED
**Resolution:** `ColonyStructure.Statuses` contains computed values (Actual and Ideal status) that are recalculated on every change. They do not need to be persisted. `[JsonIgnore]` added to `Statuses` to stop serializing them, reducing save file size significantly.  
**Action:** Colony.cs updated. REQ-ARCH-041 already covers this (computed properties SHALL be JsonIgnore).

---

### AMB-030 — RESOLVED
**Resolution:** Bug — `EmpireContext.writeContext()` was writing to `FilePath + ".new"` instead of `FilePath`, so baseline data changes were never persisted to the actual file. Fixed to write to `FilePath` directly.  
**Action:** EmpireContext.cs fixed.

---

### AMB-031 — RESOLVED
**Resolution:** `SurveyParser.parseIt()` is test code that should not run on every application startup. The call SHALL be removed from `MainWindow`. The SurveyParser needs:
1. Real HTML fragments provided by the user for proper unit tests.
2. Once tests confirm it works, it SHALL be wired into the SurveyForm (similar to how BlueprintScanner is wired into FormBlueprint for clipboard HTML processing).

**Action:** MainWindow.cs fixed — parseIt() call removed. SurveyParser testing and SurveyForm integration tracked as future work in Recommendations.md.

---

### AMB-032 — RESOLVED
**Issue:** Colony import duplicates manually-added structures. When a user manually builds out a colony in the app, structures have `gameSequence = 0`. When the importer runs, it indexes existing structures by `gameSequence` to find merge candidates. Manually-added structures (gameSequence=0) never match parsed buildings (which have real buildingIDs), so every parsed building is added as a new entry, duplicating the manual ones.

**Resolution:**

1. **Rename `gameSequence` to `displaySequence`** — this field stores the per-type UI sequence number (e.g. Reactor Core #1, #2). The form code SHALL calculate this the same way the game UI does: per flatpack type, numbered sequentially (first Habitat is 1, second Habitat is 2, etc.).

2. **Add new `buildingID` field to ColonyStructure** — stores the game's unique building identifier from the JSON `buildingID` property. This value may be important for future features. The parser SHALL store the parsed buildingID in this new field instead of overwriting `displaySequence`/`gameSequence`.

3. **Merge detection SHALL use FlatpackBlueprintUUID + displaySequence** — when importing, match parsed buildings to existing structures by their flatpack blueprint UUID and their per-type display sequence number. The display sequence is derived from the UI name pattern `#<num> - <name>`.

4. **CommodityManufactory special case** — all commodity manufactory types (Agridome, Administration Block, etc.) are sequenced together by the game, and the sequence is not stable when new ones are built. For these, use best-guess matching: sort existing commodity manufactories by build order (list position), sort parsed ones by UI order, and match 1:1 by position within each commodity manufactory sub-type. The first Agridome in the UI matches the first Agridome the user built, the second matches the second, etc.

5. **REQ-CI-020 update** — replace the current text with: "When importing into an existing colony, structures SHALL be merged by FlatpackBlueprintUUID + per-type display sequence. The parser SHALL store the game's buildingID in a separate `buildingID` field on ColonyStructure. For CommodityManufactory types, matching SHALL use build order vs UI order within each sub-type."

6. **REQ-CI-024 (new)** — "When a structure is added manually via the colony form, the form SHALL calculate and assign a `displaySequence` matching the game UI convention: per flatpack type, numbered sequentially starting at 1."

**Action:** Requires implementation. New field `buildingID` on ColonyStructure, rename `gameSequence` to `displaySequence`, update merge logic in ColonyParser, update form code to calculate display sequence on manual add.


---

## Code Quality Review (April 2026)

### AMB-033 — RESOLVED: Double clipboard read in colony import handler
**Issue:** `FormColony.cmdImportColony_Click` reads the clipboard twice when updating an existing colony. First via `parser.ParseClipboardToTemp(empireContext)` (which reads clipboard internally), then again explicitly with `Clipboard.GetText(TextDataFormat.Html)` + `ExtractHtmlFragmentFromClipboardData` to get the HTML for `parser.ProcessHtml(existingColony, html, empireContext)`. This is wasteful and fragile — if the clipboard changes between the two reads, the merge would use different data.

**Resolution:** `ParseClipboardToTemp` now returns the extracted HTML via an `out string extractedHtml` parameter. `FormColony.cmdImportColony_Click` uses the out parameter to reuse the HTML for the merge path, eliminating the second clipboard read.

**Action:** ColonyParser.ParseClipboardToTemp updated with out parameter. FormColony.cmdImportColony_Click updated to use it.

---

### AMB-034 — RESOLVED: `processClipboard` renamed to PascalCase on BlueprintScanner and SurveyParser
**Issue:** Recommendations.md item 12d (method naming PascalCase) was marked complete, but two parser methods remained camelCase:
- `BlueprintScanner.processClipboard(Blueprint)` — called from `FormBlueprint.cmdImport_Click` fallback path
- `SurveyParser.processClipboard(Survey)` — called from `FormSurvey.cmdImport_Click`

`ColonyParser.ProcessClipboard` is already PascalCase.

**Resolution:** Both methods renamed to `ProcessClipboard` and all callers updated.

**Action:** BlueprintScanner.cs, SurveyParser.cs, FormBlueprint.cs, FormSurvey.cs updated.

---

### AMB-035 — RESOLVED: `ExtractHtmlFragmentFromClipboardData` extracted into shared utility
**Issue:** `BlueprintScanner.ExtractHtmlFragmentFromClipboardData` is a general-purpose clipboard HTML extraction utility, but it lives inside `Forms/Blueprint/BlueprintScanner.cs`. Both `ColonyParser` and `SurveyParser` reference it via the fully-qualified path `Forms.Blueprint.BlueprintScanner.ExtractHtmlFragmentFromClipboardData(...)`. Additionally, `FormBlueprint` has a redundant wrapper method `ExtractHtmlFragmentFromClipboardData` that just delegates to `BlueprintScanner`.

Test files also reference it via the long path (6 test files).

**Resolution:** Created `Parsers/ClipboardHelper.cs` as a facade that delegates to `BlueprintScanner.ExtractHtmlFragmentFromClipboardData`. Updated `ColonyParser` (2 places), `SurveyParser` (1 place), and `FormColony` to use `ClipboardHelper.ExtractHtmlFragment(...)`. Removed the redundant wrapper from `FormBlueprint.cs`. Updated `cmdImportMarket_Click` to call `BlueprintScanner.ExtractHtmlFragmentFromClipboardData` directly. Test files keep their current references.

**Action:** ClipboardHelper.cs created. ColonyParser.cs, SurveyParser.cs, FormBlueprint.cs, FormColony.cs updated. csproj updated with Compile Include.

---

### AMB-036 — RESOLVED: Commented-out `[NotMapped]` attributes and dead properties removed from Blueprint.cs
**Issue:** `Blueprint.cs` has 5 commented-out `//[NotMapped]` attributes on active properties and 2 fully commented-out properties (`ManufactureRunTime`, `MaxAllowedOnShip`). These are remnants from an Entity Framework era that no longer applies (the project uses Newtonsoft.Json for persistence). Recommendations.md 12a (dead code removal) was marked complete but these remain.

**Resolution:** All `//[NotMapped]` comment lines and the two dead property definitions removed.

**Action:** Blueprint.cs cleaned up.

---

### AMB-037 — RESOLVED: Commented-out debug code removed from BlueprintScanner.ProcessClipboard
**Issue:** `BlueprintScanner.ProcessClipboard` contained two commented-out string replacement lines that were debug artifacts:
```csharp
//output = $@"@""{output.Replace("\n", "\"\n")}""";
//output = $@"@""{output.Replace("\r", "\"\r")}""";
```
Also, `FormBlueprint.btnImport_Click` (a different, unused import handler) had commented-out lines referencing `rtbCopyTarget` and `ProcessHTML`.

**Resolution:** Commented-out debug lines removed from BlueprintScanner. Dead `btnImport_Click` handler removed (see AMB-039).

**Action:** BlueprintScanner.cs cleaned up.

---

### AMB-038 — RESOLVED: Survey import has no dedup logic
**Issue:** `FormSurvey.cmdImport_Click` is a 3-line method that calls `parser.processClipboard(viewModel.Data)` directly — no clipboard guard, no temporary object, no dedup, no error handling. Colony and Blueprint imports both now have dedup logic; Survey is the outlier.

Surveys are identified by PlanetName + SurveyID. Importing the same survey twice into the selected survey object works (overwrites), but importing a survey for a different planet into the wrong selected survey silently corrupts data.

**Recommendation:** Add survey import dedup following the same pattern as colony-import-dedupe: parse into temp, search by PlanetName+SurveyID, merge or create. Add a clipboard HTML guard and error handling. Tracked as BL-038 in BACKLOG.md.

---

### AMB-039 — RESOLVED: `FormBlueprint.btnImport_Click` dead code removed
**Issue:** `FormBlueprint` has two import click handlers: `cmdImport_Click` (the real one, wired to the Import button) and `btnImport_Click` (line ~490, reads clipboard but does nothing useful — the processing lines are commented out). This appears to be an old debug handler that was never removed.

**Resolution:** `btnImport_Click` removed entirely. It was not wired in the Designer.cs.

**Action:** FormBlueprint.cs cleaned up.

---

### AMB-040 — RESOLVED: Colony and Blueprint import dedup helpers use divergent patterns
**Issue:** Colony import uses `ColonyImportHelper` (a dedicated static helper in Services/) with `FindByName`, `MergeIdentity`, `CreateFromTemp`, `IsDuplicateName`. Blueprint import reuses `MarketBlueprintImporter` methods (`FindByDedupKey`, `UpdateExisting`, `IsGlobalRoute`) that were made `internal`.

The patterns diverge in several ways:
- Colony dedup key is a single field (ColonyName, case-insensitive). Blueprint dedup key is a 5-field composite (case-sensitive).
- Colony merge only copies identity fields (PlanetName, SystemName), then re-parses HTML into the existing colony for structure/commodity merge. Blueprint merge uses `UpdateExisting` which handles property preservation.
- Colony `CreateFromTemp` copies data into a new object. Blueprint create path mutates the temp object directly (assigns UUID, adds to list).
- Colony import has `IsDuplicateName` for manual edit validation. Blueprint import has no equivalent.

These differences are partly justified by domain differences, but the structural inconsistency makes the codebase harder to reason about.

**Resolution:** The divergence is justified by domain differences and does not warrant refactoring to a shared abstraction. Colonies match by a single field (name) because that's how the game identifies them; blueprints use a 5-field composite key because multiple blueprints share names. Colony merge re-parses full HTML to leverage existing structure/commodity merge logic in `ColonyParser.ProcessHtml`; blueprint merge selectively overwrites properties while preserving protected fields — genuinely different strategies. Colony `CreateFromTemp` copies into a new object because child collections (Structures, Commodities) need clean ownership transfer; blueprint create mutates the temp directly because Blueprint has no such child collections. Forcing these into a shared abstraction would add complexity without real benefit. No code change needed.

---

### AMB-041 — RESOLVED: `baseBlueprintUUID` is camelCase on Blueprint model
**Issue:** `Blueprint.baseBlueprintUUID` is the only camelCase public property on any model class. All other properties use PascalCase (`OwnerUUID`, `BluePrintType`, `ColonyName`, etc.). This is likely a legacy naming from an earlier version.

Renaming it would require a JSON migration strategy since it's serialized to PlayerData.json and BaselineData.json. Newtonsoft.Json serializes using the property name by default.

**Resolution:** Renamed to `BaseBlueprintUUID`. Updated all code references across 10+ files. Migrated all JSON data files (Alpha3.json, BaselineData.json main+test, PlayerData.json test) to use the new key name. No `[JsonProperty]` attribute needed since both code and data are aligned on the new name.


---

## Code Quality Review — Baseline Data Stability (April 2026)

### AMB-042 — RESOLVED: DeterministicUUID.Generate does not handle null fields
**Resolution:** Added explicit null-to-empty coalescing (`name ?? ""`, `blueprintType ?? ""`, `techLevel ?? ""`) in the Generate method.
**Action:** DeterministicUUID.cs updated.

---

### AMB-043 — RESOLVED: MigrationRunner has no error handling or logging
**Resolution:** Added try/catch around each migration phase (pre-rename, each migration, post-rename) with NLog error logging and a MessageBox error dialog. Added `MigrationFailed` static flag that blocks `writeContext()` on both EmpireContext and PlayerContext when set, preventing saves in a partially-migrated state. Added `ResetFailureState()` for tests and File→New/Open.
**Action:** MigrationRunner.cs rewritten with error handling. EmpireContext.writeContext and PlayerContext.writeContext guard on MigrationFailed.

---

### AMB-044 — RESOLVED: ColonyBootstrap.BestResourceEntry.RawAmount is still `double`
**Resolution:** Changed `RawAmount` to `decimal`, `double.TryParse` to `decimal.TryParse`, removed the `(decimal)` cast.
**Action:** ColonyBootstrap.cs updated.

---

### AMB-045 — RESOLVED: EmpireContext has mixed camelCase/PascalCase public members
**Resolution:** All public fields on EmpireContext (16 fields) and PlayerContext (12 fields) renamed to PascalCase across 41 files. Methods were already PascalCase from a prior rename.
**Action:** All references updated via find-and-replace. BL-044 in BACKLOG.md is now complete.

---

### AMB-046 — RESOLVED: PropertyBagTests comment says "double overload" but tests decimal
**Resolution:** Comment updated to "decimal overload".
**Action:** PropertyBagTests.cs updated.

---

### AMB-047 — RESOLVED: RefiningRecipe model class lives in Constants namespace
**Resolution:** Extracted `RefiningRecipe` POCO to `OE2EmpireTracker/Models/RefiningRecipe.cs` under the `Models` namespace. `RefiningRecipes` static helper stays in Constants with a `using OE2EmpireTracker.Models` import.
**Action:** RefiningRecipe.cs created in Models/. RefiningRecipes.cs updated. csproj updated.

---

### AMB-048 — RESOLVED: BaselineRoot uses public fields instead of properties
**Resolution:** Changed all public fields to auto-properties on both `BaselineRoot` and `PlayerRoot`.
**Action:** EmpireContext.cs and PlayerContext.cs updated.

---

### AMB-049 — RESOLVED: Externalized data (Commodity, RefiningRecipe, ResearchTime) uses mutable static state
**Resolution:** Documented the threading constraint on `SetCommodities()`, `SetRecipes()`, and `SetResearchTimes()` — each now has a THREADING doc comment stating BackgroundProcessor must be stopped before calling. No code change beyond documentation; the current guards (BackgroundProcessor stopped before Reset) are sufficient.
**Action:** Commodity.cs, RefiningRecipes.cs, ResearchTimeLookup.cs doc comments updated.

---

### AMB-050 — RESOLVED: Spec says Requirement 13.7 excludes MainWindow performance metrics from decimal conversion, but BuildTimeCalculator also uses double
**Resolution:** Documented on `BuildTimeCalculator` that double is intentional — it's a transient time calculation producing a long, not a persisted game data value. No code change needed.
**Action:** BuildTimeCalculator.cs doc comment updated.

---

### AMB-051 — RESOLVED: Baseline-data-stability spec COMPLETED.md entry missing
**Resolution:** Entry added to COMPLETED.md.
**Action:** spec/COMPLETED.md updated.

---

## Spec-vs-Code Audit (April 2026)

### AMB-052 — RESOLVED: REQ-BPV-010 lists property names without spaces but code uses spaced names

**Resolution:** Updated `BlueprintProperties.md` REQ-BPV-010 through REQ-BPV-050 to list property names with spaces matching the actual game data and `BlueprintPropertyValidation.cs`. Updated integer property count from 57 to 62 (added Accuracy, Amount Manufactured, Blue Collar Detail, Unassigned Blue Collar Detail, White Collar Detail). All property names now match the canonical spaced form used in the code and game data.

**Action:** `spec/requirements/BlueprintProperties.md` rewritten.

---

### AMB-053 — RESOLVED: REQ-DM-079 conflates two different key types

**Resolution:** Rewrote REQ-DM-079 to distinguish WorkerDetail IDs (no spaces, used as `BaseItemTypeID` and lock keys) from blueprint property keys (with spaces, used for worker count lookups). References `WorkerTypeInfo.DetailKey` and `WorkerTypeInfo.PropertyKey` as the canonical separation.

**Action:** `spec/requirements/DataModel.md` REQ-DM-079 updated.

---

### AMB-054 — RESOLVED: WorkerDetail.Name field uses property key string instead of display name

**Resolution:** The game's cargo hold displays worker items as "Blue Collar Detail", "White Collar Detail", "Specialist Detail" — matching the current `WorkerDetail.Name` values. These happen to be the same strings as the blueprint property keys (`GameConstants.PropBlueCollarDetail` etc.), which is intentional — the game uses the same name in both contexts. `WorkerTypeInfo.DisplayName` (e.g. "Blue Collar") is a shorter form used for UI labels like worker assignment checkboxes, not the canonical item name. No code change needed — the current values are correct.

**Action:** No code change. Documented in spec.

---

### AMB-055 — RESOLVED: REQ-COL-017b does not specify entertainment cost for unallocated workers

**Resolution:** Updated REQ-COL-017b to explicitly state that an unallocated worker adds 1 to HabitationRequired, 1 to FoodRequired, and 2 to EntertainmentRequired, consistent with REQ-COL-017's 2x entertainment rule.

**Action:** `spec/requirements/Colony.md` REQ-COL-017b updated.

---

### AMB-056 — RESOLVED: GOALS.md and spec-code-sync-review.md are stale

**Resolution:** GOALS.md updated: test count 750 -> 1371, feature count corrected, completed items removed from Open Work, dependency graph updated. spec-code-sync-review.md marked as superseded with pointer to AMB-052-057.

**Action:** `spec/GOALS.md` and `spec/spec-code-sync-review.md` updated in prior commit.

---

### AMB-057 — RESOLVED: ColonyStructure.displaySequence has [JsonProperty("gameSequence")] — migration concern

**Resolution:** Migrated to the new JSON key name `"displaySequence"`. Removed `[JsonProperty("gameSequence")]` from the public property. Added a private write-only `gameSequenceLegacy` property with `[JsonProperty("gameSequence")]` so old save files with the `"gameSequence"` key still deserialize correctly. New saves write `"displaySequence"`. Added Migration006 (no-op — data already loaded correctly, bumps DataVersion to 6 so next save writes the new key). Renamed ViewModel property `GameSequence` to `DisplaySequence`. Updated test names.

**Action:** `ColonyStructure.cs`, `ColonyStructureViewModel.cs`, `MigrationRunner.cs` updated. `Migration006_DisplaySequenceJsonKey.cs` created. 4 tests added. REQ-COL-033 updated to say `displaySequence`.

---

## Spec-vs-Code Audit Pass 3 (April 2026)

### AMB-058 — RESOLVED: Colony.ProcessColony processing order does not match REQ-COL-100 / REQ-ARCH-080

**Resolution:** Refactored `Colony.ProcessColony()` to match the spec order. The single interleaved loop was split into separate passes:
1. Structure Building (separate loop)
2. Mining
3. Refining in tier order (base → S1 → S2)
4. Manufacturing + Commodity Manufacturing
5. Research

Ready structures are collected once with their blueprints, then each pass filters by type. This ensures mined resources are available for refining, and refined resources are available for manufacturing within the same cycle.

**Action:** `Colony.cs` ProcessColony rewritten. All 1375 tests pass.

---

### AMB-059 — RESOLVED: Several completed features lack formal requirement docs in spec/requirements/

**Resolution:** Created 6 new requirement docs:
- `EvolutionGraph.md` (REQ-EVO-001 through REQ-EVO-042) — blueprint evolution chain chart
- `MainMenu.md` (REQ-MM-001 through REQ-MM-031) — file lifecycle, manage menu, help/about
- `MDIWindowMenu.md` (REQ-MDI-001 through REQ-MDI-012) — window menu, layout, numbering
- `Preferences.md` (REQ-PRF-001 through REQ-PRF-050) — configurable thresholds and intervals
- `ColonyAdminSummary.md` (REQ-CAS-001 through REQ-CAS-040) — per-colony admin tab report
- `PlayerProfileImport.md` (REQ-PPI-001 through REQ-PPI-081) — profile clipboard import

All derived from existing kiro specs and verified against implemented code. Requirements README index updated (now 22 files).

**Action:** 6 new requirement docs created. README.md updated.


## Code-Spec Audit Findings

### AMB-060 — RESOLVED: RouteStop.ColonyUUID missing backward-compat JSON attribute
**Resolution:** Added `[DefaultValue("")]` and `= string.Empty` default to ColonyUUID on both RouteStop and DeliveryPlanStop. With `DefaultValueHandling.Ignore` in JsonSettings, the deprecated field is now omitted from JSON when empty.
**Spec (data-models.md):** RouteStop.ColonyUUID should have `[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]` for backward compatibility.
**Code (DeliveryRoute.cs):** ColonyUUID is a plain property with no JSON attribute. Same for DeliveryPlanStop.ColonyUUID in DeliveryPlan.cs.
**Impact:** ColonyUUID will always serialize to JSON even when null/empty, adding noise to saved data. Not a functional bug since Migration008 copies ColonyUUID → DestinationUUID, but the old field persists unnecessarily.

### AMB-061 — RESOLVED: DeliveryPlanReferenceCounter does not exist
**Resolution:** Created `DeliveryPlanReferenceCounter.cs` that counts `BuildPlan.DeliveryPlanUUID` references to delivery plans. Added to csproj.
**Spec (reference-counting.md):** DeliveryPlanReferenceCounter should count BuildPlan.DeliveryPlanUUID references.
**Code:** No DeliveryPlanReferenceCounter.cs file exists. BuildPlan.DeliveryPlanUUID references are not tracked.
**Impact:** Delivery plans can be deleted even when referenced by build plans.

### AMB-062 — RESOLVED: StockPlanReferenceCounter does not exist
**Resolution:** Created `StockPlanReferenceCounter.cs` that counts `StockProfileEntry.StockPlanUUID` references to stock plans. Added to csproj.
**Spec (reference-counting.md):** StockPlanReferenceCounter should count StockProfileEntry.StockPlanUUID references.
**Code:** No StockPlanReferenceCounter.cs file exists. StockProfile entries referencing stock plans are not tracked.
**Impact:** Stock plans can be deleted even when referenced by stock profiles.

### AMB-063 — RESOLVED: DeliveryRouteReferenceCounter incomplete
**Resolution:** Expanded `DeliveryRouteReferenceCounter` to count `WarehouseOverflowRule.DeliveryRouteUUID` and `SupplyChainStage.DeliveryRouteUUID`. Updated report class with `OverflowRuleCount` and `SupplyChainStageCount` fields. New constructor parameters have defaults for backward compatibility.
**Spec (reference-counting.md):** DeliveryRouteReferenceCounter should count DeliveryPlan.RouteUUID, WarehouseOverflowRule.DeliveryRouteUUID, and SupplyChainStage.DeliveryRouteUUID.
**Code (DeliveryRouteReferenceCounter.cs):** Only counts DeliveryPlan.RouteUUID. Comment says "Will be expanded to include WarehouseOverflowRules and SupplyChainStages" but this was never done.
**Impact:** Delivery routes can be deleted even when referenced by overflow rules or supply chain stages.

### AMB-064 — RESOLVED: StationReferenceCounter incomplete
**Resolution:** Expanded `StationReferenceCounter` to count `MarketListing.StationUUID`, `MarketTransaction.StationUUID`, `SupplyChainStage.LocationUUID` (when Station), `StockPlan.Targets[].LocationUUID` (when Station), and `WarehouseOverflowRule.DestinationUUID` (when Station). New constructor parameters have defaults for backward compatibility.
**Spec (reference-counting.md):** StationReferenceCounter should count: RouteStops, DeliveryPlanStops, Ship.LocationUUID, MarketListing.StationUUID, MarketTransaction.StationUUID, BuildItem.AssemblyLocationUUID, SupplyChainStage.LocationUUID, StockPlan.Targets[].LocationUUID, WarehouseOverflowRule.DestinationUUID (all when Station).
**Code (StationReferenceCounter.cs):** Only counts RouteStops, DeliveryPlanStops, BuildItem.AssemblyLocationUUID, and BuildItem.BuildLocationUUID. Missing: Ship.LocationUUID, MarketListing.StationUUID, MarketTransaction.StationUUID, SupplyChainStage.LocationUUID, StockPlan.Targets[].LocationUUID, WarehouseOverflowRule.DestinationUUID.
**Impact:** Stations can be deleted even when referenced by ships, market listings, transactions, supply chains, stock targets, or overflow rules.

### AMB-065 — RESOLVED: AsteroidReferenceCounter missing SupplyChainStage references
**Resolution:** Expanded `AsteroidReferenceCounter` to count `SupplyChainStage.LocationUUID` (when Asteroid). Updated `AsteroidReferenceReport` with `SupplyChainStageCount` field. New constructor parameter has default for backward compatibility.
**Spec (reference-counting.md):** AsteroidReferenceCounter should count SupplyChainStage.LocationUUID (when Asteroid).
**Code (AsteroidReferenceCounter.cs):** Only counts Survey.AsteroidUUID, BuildItem.BuildLocationUUID (when Asteroid), and RouteStop (when Asteroid). Missing SupplyChainStage.
**Impact:** Asteroids can be deleted even when referenced by supply chain stages.

### AMB-066 — RESOLVED: ColonyReferenceCounter missing WarehouseOverflowRule.DestinationUUID
**Resolution:** Added `_overflowDestMap` to `ColonyReferenceCounter` that counts `WarehouseOverflowRule.DestinationUUID` when `DestinationType == Colony`. The destination count is summed into the overflow total in `CountReferences`.
**Spec (reference-counting.md):** ColonyReferenceCounter should count WarehouseOverflowRule.DestinationUUID (when Colony) in addition to ColonyUUID.
**Code (ColonyReferenceCounter.cs):** Only counts WarehouseOverflowRule.ColonyUUID, not DestinationUUID when DestinationType is Colony.
**Impact:** If an overflow rule's destination is a colony (not just its source), that reference is not counted.

### AMB-067 — RESOLVED: BlueprintReferenceCounter missing MarketTransaction.ItemReferenceID
**Resolution:** Added `marketTransactions` parameter to `BlueprintReferenceCounter` constructor and counting loop for `MarketTransaction.ItemReferenceID`.
**Spec (reference-counting.md):** BlueprintReferenceCounter should count MarketTransaction.ItemReferenceID (when Blueprint).
**Code (BlueprintReferenceCounter.cs):** Counts MarketListing.ItemReferenceID but not MarketTransaction.ItemReferenceID.
**Impact:** Blueprints referenced only by market transactions (not listings) can be deleted.

### AMB-068 — RESOLVED: BlueprintReferenceCounter duplicate station loop (bug)
**Resolution:** Removed the duplicate station component counting loop. Station blueprints now counted once.
**Code (BlueprintReferenceCounter.cs):** The station component counting loop appeared twice, causing station blueprint references to be double-counted. Duplicate removed.
**Impact:** Station component blueprint reference counts are inflated by 2x, which doesn't cause false negatives (deletion still blocked) but reports incorrect numbers.

### AMB-069 — RESOLVED: Migration numbering mismatch
**Resolution:** Updated `spec/design/migration.md` to document the actual migration sequence (Migration001 through Migration008) and clarify that the original Migration005_EmpireSystems was split across PlayerContext initialization and Migration008.
**Spec (migration.md):** Described Migration005_EmpireSystems for route stop migration and empty array initialization.
**Code:** Migration005 is PropertyKeyCleanup. The route stop migration is Migration008_RouteStopDestinationMigration. No single migration adds empty arrays for new entity types.
**Impact:** Spec migration numbering is outdated. The actual migration sequence diverged from the spec.

## Deep Code Audit (Round 2)

### AMB-070 — OPEN: MAGIC_NUMBER — Purity multipliers hardcoded in 3 files
**Files:** Colony.cs:312-315, ColonyActivityCollector.cs:252-254, ColonyAdminReportBuilder.cs:428-430
**Issue:** Purity output multipliers (Low=1, Medium=3, High=5) are hardcoded as magic numbers in switch statements in 3 separate files. Should be constants in GameConstants (e.g. PurityMultiplierLow=1, PurityMultiplierMedium=3, PurityMultiplierHigh=5) or a lookup method.
**Spec reference:** spec/requirements/GameMechanics.md should define these multipliers
**Impact:** If game balance changes multipliers, 3 files need updating independently.

### AMB-071 — OPEN: MAGIC_NUMBER — Purity name strings hardcoded instead of using constants
**Files:** Colony.cs, ColonyActivityCollector.cs, ColonyAdminReportBuilder.cs, SurveyParser.cs
**Issue:** Purity names ("Low", "Medium", "High", "Refined") are hardcoded as string literals in switch statements. GameConstants already has PurityRefined but not the unrefined names. Should add PurityHigh, PurityMedium, PurityLow constants.
**Spec reference:** spec/requirements/GameMechanics.md
**Impact:** Typo in a purity string would silently fail to match.

### AMB-072 — OPEN: MAGIC_NUMBER — Survey property keys hardcoded in SurveyViewModel
**File:** SurveyViewModel.cs:48-60
**Issue:** Property keys "SensorAbundance", "PurityModifier", "ScanLevel" are hardcoded strings. Should be constants (e.g. in a SurveyPropertyKeys class or GameConstants).
**Spec reference:** spec/requirements/Survey.md
**Impact:** Typo in key string would silently read/write wrong property.

### AMB-073 — RESOLVED: FormColony (V1) missing PERF logging
**Resolution:** Deleted the legacy V1 Colony form (FormColony.cs, ColonyStructure.cs, and their Designer/resx files). FormColonyV2 is the active form with proper PERF logging. The "FormColony" key in FormOpeners already mapped to FormColonyV2 for backward compatibility.
**File:** FormColony.cs
**Issue:** The legacy FormColony (V1) has no PERF timing on any method. FormColonyV2 has proper PERF logging. FormColony is still in the codebase and accessible.
**Spec reference:** spec/design/code-standards.md (Form Implementation Checklist point 4)
**Impact:** Cannot identify performance bottlenecks in the legacy form.

### AMB-074 — OPEN: MISSING_SPEC — CargoVolumeService has no spec coverage
**File:** OE2EmpireTracker/Services/CargoVolumeService.cs
**Issue:** CargoVolumeService computes cargo volume for delivery plans and ships but has no requirements in spec/requirements/ and no mention in spec/design/services.md.
**Spec reference:** Should be in spec/requirements/Ships.md (REQ-SHP-050 series) and spec/design/services.md
**Impact:** Service behavior is undocumented.

### AMB-075 — OPEN: MISSING_SPEC — BlueprintImportHandler has no spec coverage
**File:** OE2EmpireTracker/Services/BlueprintImportHandler.cs
**Issue:** BlueprintImportHandler orchestrates individual blueprint import routing but has no spec entry. The MarketBlueprintImporter is documented but this handler is not.
**Spec reference:** Should be in spec/requirements/BlueprintProperties.md or a new BlueprintImport.md
**Impact:** Import routing logic is undocumented.

### AMB-076 — OPEN: MISSING_SPEC — ClipboardContentDetector has no spec coverage
**File:** OE2EmpireTracker/Parsers/ClipboardContentDetector.cs
**Issue:** ClipboardContentDetector sniffs HTML to determine content type (colony, survey, blueprint, profile, market) but has no spec entry.
**Spec reference:** Should be in spec/requirements/Architecture.md or ColonyImport.md
**Impact:** Content detection logic and CSS class markers are undocumented.

### AMB-077 — OPEN: MISSING_SPEC — CountdownFormatParser has no spec coverage
**File:** OE2EmpireTracker/Parsers/CountdownFormatParser.cs
**Issue:** CountdownFormatParser converts "Xd Xh Xm Xs" strings to seconds for the Preferences form but has no spec entry.
**Spec reference:** Should be in spec/requirements/Preferences.md
**Impact:** Parsing rules for countdown format input are undocumented.

### AMB-078 — OPEN: MISSING_SPEC — JsonSettings has no spec coverage
**File:** OE2EmpireTracker/Services/JsonSettings.cs
**Issue:** JsonSettings configures Newtonsoft.Json serialization (DefaultValueHandling.Ignore, NullValueHandling.Ignore) but has no spec entry.
**Spec reference:** Should be in spec/requirements/Architecture.md
**Impact:** Serialization behavior is undocumented.

### AMB-079 — OPEN: MISSING_SPEC — 6 model classes have no spec coverage
**Files:** ColonyStructureStatus.cs, StructureStatusDelta.cs, ColonyWorker.cs, ResearchTimeEntry.cs, ItemProperty.cs, SubResource.cs
**Issue:** These supporting model classes exist in code but have no mention in spec/design/data-models.md or spec/requirements/DataModel.md.
**Spec reference:** Should be in spec/design/data-models.md
**Impact:** Data structures used by colony processing are undocumented.