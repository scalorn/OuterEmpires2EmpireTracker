# OE2 Empire Tracker — Goals & Progress

## Specification Documents

- [Requirements README](requirements/README.md) — index of all requirement files
- [Data Model Requirements](requirements/DataModel.md)
- [Colony Requirements](requirements/Colony.md)
- [Player Profile Requirements](requirements/PlayerProfile.md)
- [Survey Requirements](requirements/Survey.md)
- [Architecture Requirements](requirements/Architecture.md)
- [Recommendations](../spec/Recommendations.md) — open issues needing approval

---

## Approved Tasks

### PRIORITY: Complete MVVM Across All Forms (before any new feature work)
**Status: Complete**

Per AMB-016 and AMB-017, MVVM must be applied to all forms before new features are added.

| Form | Status | ViewModel(s) needed |
|------|--------|---------------------|
| FormColony + ColonyStructure | Complete | ColonyViewModel, ColonyStructureViewModel |
| FormPlayerProfile | Complete | PlayerProfileViewModel |
| FormBlueprint | Complete | BlueprintViewModel |
| FormSurvey | Complete | SurveyViewModel |
| MainWindow | N/A — no data access | (none needed) |

Each ViewModel must:
- Wrap all PropertyBag access with typed properties
- Wrap all data list manipulation with methods
- Have no WinForms dependencies
- Be covered by unit tests

---

### 1. Colony Form — Structure Management
**Status: Complete**

- Add/remove flatpack structures to a colony
- Up/Down/Delete buttons on each ColonyStructure control
- ColonyStatusCalculator calculates Actual and Ideal resource status
- IColonyStructureWorkers interface abstracts worker state for actual vs ideal calculation
- RtfBuilder for single-pass RTF rendering of status displays
- Structure reorder uses SetChildIndex (no dispose/recreate)
- Delete key removes selected structure from colony and backing list

### 2. Colony Form — Item Management
**Status: Complete**

- Add items to colony warehouse by ItemType (Resource, Commodity, WorkDetail, Survey, Blueprint)
- Resource: purity selection via cmbPurity
- Commodity: filtered combo with ExtendedName display
- WorkDetail: BlueCollarDetail, WhiteCollarDetail, SpecialistDetail
- Survey: filtered by ExtendedName/PlanetName, UUID as BaseItemTypeID
- Blueprint: filtered by ExtendedName, UUID as BaseItemTypeID
- Item.ExtendedName enhanced for all item types including Survey and Blueprint lookups via PlayerContext
- Delete key removes selected item from colony and backing list
- ItemBag.CountByType for quantity aggregation across stacks

### 3. Colony Form — Commodity Requests
**Status: Complete**

- cmbCommodityRequest populated with filtered Commodities
- cmdAddCommodityRequest creates CommodityRequested and adds to Colony.Commodities
- dgvCommodityRequests populated from Colony.Commodities on load and add
- CellValueChanged writes edits back to backing CommodityRequested object
- Delete key removes selected row from colony and backing list
- FullRowSelect mode enabled on dgvCommodityRequests

### 4. Player Profile Form
**Status: Complete**

- ListView with Name/Faction columns, filtered by txtNameFilter
- Selection change populates form via PopulateForm
- Save/Delete/Cancel implemented
- SkillName and SkillGroupName enums replace hardcoded strings
- Skill group checkboxes restored from profile on selection
- Duplicate event subscription bug fixed
- ListView re-selects saved profile after save

### 5. LockTracking
**Status: Complete**

- ItemKey struct (ItemType + BaseItemTypeID) as dictionary key
- LockItem, LockItems, GetLockedQuantity, GetLocksForProcess, ClearLocksForProcess
- JSON serialization via LockTrackingJsonConverter
- Unit tests: 30 tests covering all API and JSON round-trips

### 6. Data Classes
**Status: Complete**

- WorkerDetail: BlueCollarDetail, WhiteCollarDetail, SpecialistDetail
- CountDownTime: TimeRemaining getter/setter, TimeRemainingString getter/setter
- PropertyBag: setProperty (string/double/bool), getDouble/getLong/getBoolean/getString, Remove, Clear, JSON
- Blueprint.ExtendedName: C{Class} Ev({Evolution}) Name (TechLevel) [NickName]
- Survey.ExtendedName: PlanetName (SurveyID) [NickName]
- Item.ExtendedName: branches for Resource, Commodity, Survey, Blueprint

### 7. ColonyStatusCalculator Refactoring
**Status: Complete**

- IColonyStructureWorkers interface with ActualColonyStructureWorkers and IdealColonyStructureWorkers
- SetWorkerAssigned and GetStructureState proxied through interface
- CalculateBuilt() and CalculateIdeal() use injected worker source
- Unallocated worker tracking (UnassignedBlueCollarDetail etc.)
- Bug fix: builtEntertainmentRequired and builtWarehouseRequired seeded from wrong fields
- Blueprint lookup cached per structure (single findBlueprint call)

### 8. Unit Test Suite
**Status: In Progress — see below**

#### Completed test files
- BlueprintScannerTests — processHtml, ExtractHtmlFragmentFromClipboardData
- BlueprintTests — constructors, ExtendedName all branches, JSON
- CommodityTests — static list, ExtendedName, lookup dictionaries
- CountDownTimeTests — constructor, TimeRemaining getter/setter, TimeRemainingString getter/setter
- ItemBagTests — AddItem, CountByType, Remove, Clear, LockTracking integration
- ItemTests — constructors, ExtendedName all branches
- ItemTypeTests — static list, MapByEnum, MapByString, round-trip
- LockTrackingTests — ItemKey, LockItem, LockItems, GetLockedQuantity, GetLocksForProcess, ClearLocksForProcess, JSON
- PlayerProfileTests — defaults, GetSkill, SetSkillGroup, enum overloads
- PropertyBagTests — all getters/setters, Remove, Clear, JSON
- ResourceClassTests — static list, ClassMapByEnum, ClassMapByString, round-trip
- ResourceGroupTests — static list, Synthetic flag, maps, round-trip
- ResourcePurityTests — static list, Refined flag, maps, round-trip
- SurveyTests — constructors, ExtendedName, SurveyResource, JSON
- WorkerDetailTests — static list, maps, round-trip, property bag key contract
- ColonyStatusCalculatorTests — power/habitation/food/entertainment/warehouse accumulation, online vs offline, chaining, actual vs ideal workers, unallocated worker tracking with lock integration, null blueprint, combined scenarios, IColonyStructureWorkers implementations
- SurveyParserTests — ParseDescription, ParseResource, full HTML integration with real game data, edge cases (empty, malformed, null, unknown trace, duplicates)
- ResourceTests — static list, enum coverage, no duplicates, alphabetical sort, map lookups, round-trip, ResourceGroup/ResourceClass validation, synthetic group

#### Not yet tested
- FormColony (UI — integration test territory)
- FormPlayerProfile (UI)
- PlayerContext / EmpireContext (requires file I/O)

### 10. MVVM Pilot — FormColony and ColonyStructure
**Status: Complete**

**Goal:** Decouple UI from data by introducing lightweight ViewModels. FormColony and ColonyStructure are the pilot. No library dependencies — plain C# classes with methods the form calls from event handlers.

**Breakdown:**

#### Task 10.1 — ColonyStructureViewModel
Wraps `Baseline.ColonyStructure` (the data class) to hide `PropertyBag` access:
- `IsBuilt`, `IsStaged`, `IsOnline` — typed bool properties over `Properties.getBoolean/setProperty`
- `GetWorkerAssigned(key)` / `SetWorkerAssigned(key, bool)` — over `AssignedWorkers`
- `MiningSurvey`, `MiningSurveyResource`, `MiningLeftOvers`, `ProcessCompletionTime` — typed pass-through properties
- `MoveUp(colony)`, `MoveDown(colony)`, `Delete(colony)` — structure list manipulation
- `BlueprintType` — resolved from `FlatpackBlueprintUUID` via `PlayerContext`

#### Task 10.2 — ColonyViewModel
Wraps `Baseline.Colony` to hide direct list/bag manipulation:
- `PlanetName`, `ColonyName` — typed string properties
- `AddStructure(blueprintUUID)` — creates `ColonyStructure`, adds to list, returns `ColonyStructureViewModel`
- `RecalculateStatus()` — runs `CalculateBuilt` and `CalculateIdeal`
- `AddItem(itemType, baseID, ...)` — delegates to `Colony.Items`
- `RemoveItem(uuid)` — delegates to `Colony.Items.Remove`
- `AddCommodityRequest(commodityName)` — creates and adds `CommodityRequested`
- `RemoveCommodityRequest(request)` — removes from list
- `Save(playerContext)` — delegates to `playerContext.writeContext()`
- `StructureViewModels` — `IReadOnlyList<ColonyStructureViewModel>` derived from `Colony.Structures`

#### Task 10.3 — Update ColonyStructure UserControl
- Replace all direct `ColonyStructureData.Properties.setProperty/getBoolean` calls with `ViewModel.IsBuilt` etc.
- Replace all direct `ColonyStructureData.AssignedWorkers` calls with `ViewModel.GetWorkerAssigned/SetWorkerAssigned`
- Replace `cmdUp/Down/Delete` direct list manipulation with `ViewModel.MoveUp/Down/Delete`
- `ColonyStructureData` property remains for backward compat but all logic goes through ViewModel

#### Task 10.4 — Update FormColony
- Replace direct `Colony.Structures.Add`, `Colony.Items`, `Colony.Commodities` access with `ColonyViewModel` methods
- Replace direct `colony.PlanetName/ColonyName` reads/writes with ViewModel properties
- `structures_ColonyStructureDataChanged` calls `ViewModel.RecalculateStatus()`
- `cmdSave_Click` calls `ViewModel.Save(playerContext)`

**Files to create:**
- `OE2EmpireTracker/ViewModels/ColonyStructureViewModel.cs`
- `OE2EmpireTracker/ViewModels/ColonyViewModel.cs`

**Files to modify:**
- `OE2EmpireTracker/Forms/Colony/ColonyStructure.cs`
- `OE2EmpireTracker/Forms/Colony/FormColony.cs`


**Status: Complete**

- NLog loggers added to all forms, contexts, and data classes
- All Debug.Print calls replaced with NLog (Debug/Info/Warn/Error levels)

---

### 11. Survey Import (SurveyParser + FormSurvey)
**Status: Complete**

- SurveyParser refactored from debug-only code to proper `processHtml(Survey, string)`
- Extracts PlanetName, SurveyID, DateTime, ScannedBy from HTML
- Extracts resources with name, purity (normalized: "Med Purity" → "Medium"), and decimal amounts
- Skips unknown/trace elements
- Import button added to FormSurvey, wired via `processClipboard`
- `populateFormFromViewModel` populates form without requiring UUID (works for unsaved imports)
- NLog clipboard HTML logging for test data capture
- Test data moved to external files in `TestData/` folder
- 44 tests covering 3 real game HTML fragments (SampleHtml, ZehVazoranIIM2, QuogarV2249II)

### 12. Refinery Rig Support
**Status: Complete**

- BlueprintTypes.Refinery = "Flatpacks/Refinery"
- ColonyStructure data: RefiningResource, RefiningResourcePurity fields
- Selection combo populated with unrefined resources from warehouse + actively mined resources
- Synthetic recipes shown when input resources available (e.g. "S1. Translanthanic Exotics <- Lanthanides (Refined)")
- Start button starts 1-hour repeating timer aligned to top-of-hour boundaries
- Progress status: `<consumed>:<produced> <resource> (<purity>)` with purity multipliers (Low=1x, Med=3x, High=5x of base rate 25)
- Synthetic progress: `<consumeRate>:<produceRate> <outputResource>`
- Colony.ProcessRefinery dispatches to normal or synthetic processing based on RefiningRecipes
- Depleted unrefined resources removed from warehouse (no zero-quantity items)
- Done button force-processes one interval (repeating) or expires timer (one-shot), fires ColonyStructureDataChanged
- Item grid refreshes on structure data changes

#### Synthetic Refining (RefiningRecipes)
- S1 tier: Lanthanides→S1.Translanthanic, Superheavy Exotics→S1.Translivermoric, Transuranic Volatiles→S1.Transuranic (consume 1250, produce 25)
- S2 tier: S1.Translanthanic→S2.Element126, S1.Translivermoric→S2.Element127, S1.Transuranic→S2.Superactinides (consume 500, produce 25)
- ProcessColony processes refineries in tier order: normal→S1→S2 (ensures S1 output available for S2)
- Partial batches produce proportionally

### 13. Colony Structure — Built+Online Gate
**Status: Complete**

- All structure types (mining rig, refinery, research lab, future types) require Built=true AND Online=true before showing selection/process controls
- Structures that are not built+online hide flpSelection, flpSubSelection, flpCompletionTime
- Applied consistently in handleMiningRigControls, handleRefineryControls, handleResearchLabControls, and the default else branch

### 14. Mining Rig Improvements
**Status: Complete**

- Sub-selection hidden when empty survey selected (fixed SelectedIndex >= 0 check)
- rtbProgressStatus shows `<Rate>/h <Resource> (<Purity>)` when process is active
- Cleared when no process running
- Timer aligned to top-of-hour boundaries

### 15. Research Laboratory Support
**Status: Complete**

- BlueprintTypes.ResearchLaboratory = "Flatpacks/ResearchLaboratory"
- ColonyStructure data: ResearchingBlueprintUUID field
- Selection combo populated with researchable blueprints (CanResearch property in PropertyBag, default true; evolution < 15)
- Start button starts one-shot timer based on ResearchTimeLookup (configurable table, evolution 0-14)
- Progress status: `Evo N->N+1 BlueprintName`
- Colony.ProcessResearchLab: creates evolved blueprint (evolution+1, copies properties, empty resources, NickName="NEEDS SCANNED")
- New blueprint added to playerContext.blueprintList and colony warehouse
- One-shot timer completion handled correctly in ProcessColony outer gate and cmdDone_Click

### 16. Manufactory Support
**Status: Complete**

- BlueprintTypes.Manufactory = "Flatpacks/Manufactory"
- ColonyStructure data: ManufacturingBlueprintUUID, ManufacturingQuantity, ManufacturingCompleted
- Selection combo populated with manufacturable blueprints (CanManufacture property in PropertyBag, default true; must have ManufactureTime)
- txtQuantity input field visible only after blueprint selection (defaults to 1)
- Start button parses ManufactureTime from blueprint properties, starts repeating timer for quantity items
- ManufactureTime normalized at scan time in BlueprintScanner ("9 hours" → "9h")
- Safety normalization also in ColonyStructure for existing data
- Progress status: `(completed/total) BlueprintExtendedName`
- Colony.ProcessManufactory: creates one item per interval, ItemType from BlueprintType.OutputItemType, Volume from CargoVolumeSize
- Built+online gate applied

### 17. Synthetic Refining — Whole-Unit Consumption
**Status: Complete**

- Synthetic recipes only appear when warehouse has enough for at least 1 output unit (50 for S1, 20 for S2)
- Processing consumes only whole units: 55 input with 1:50 rate → consume 50, produce 1, leave 5
- Selection combo key includes tier suffix for proper restore after re-render
- Combo always repopulates from current warehouse state
- Structures tab switch triggers UpdateData on all visible structure controls

### 18. Colony Form — Item Grid Editing
**Status: Complete**

- CellValueChanged handler on dgvItems writes edited quantity back to backing Item.Quantity
- Changes persist on save

---

## Recently Completed (this session)
- MVVM completed across all forms (Blueprint, Survey)
- All Recommendations (1-8) resolved
- SurveyParser implemented, tested, wired into FormSurvey
- Refinery rig feature with synthetic resource support
- Research laboratory feature
- Manufactory feature
- Colony structure built+online gate applied
- Mining rig UI improvements and top-of-hour alignment
- Synthetic refining whole-unit consumption and per-unit thresholds
- Item grid quantity editing
- NLog logging added throughout codebase
- Test data moved to external files
- Delete confirmation dialog added to FormPlayerProfile
- Resource static data unit tests added
- BlueprintScanner ManufactureTime normalization
- Tab switch refreshes structure controls
- Multiple bug fixes (timer completion, item grid refresh, zero-quantity cleanup, combo restore keys)
- 465 total tests, all passing
