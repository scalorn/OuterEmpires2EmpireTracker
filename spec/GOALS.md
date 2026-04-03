# OE2 Empire Tracker — Goals & Progress

## Approved Tasks

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

#### Not yet tested
- ColonyStatusCalculator
- IColonyStructureWorkers / ActualColonyStructureWorkers / IdealColonyStructureWorkers
- FormColony (UI — integration test territory)
- FormPlayerProfile (UI)
- PlayerContext / EmpireContext (requires file I/O)
- SurveyParser
- Resource static data

### 9. NLog Logging
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
