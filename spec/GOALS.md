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


**Status: Pending implementation**

- NLog.config added to project
- Package installed
- No logging calls added to code yet

---

## Recently Completed (last session)
- Survey unit tests added
- Blueprint unit tests added
- WorkerDetail unit tests added
- PropertyBag unit tests added
- ResourceClass, ResourceGroup, ResourcePurity unit tests added
- PlayerProfile, PlayerRank, PlayerSkill, SkillName extension unit tests added
- ItemType unit tests added
- Item unit tests added
- CountDownTime unit tests added
- Commodity, CommodityGroup, CommodityIndustry unit tests added
