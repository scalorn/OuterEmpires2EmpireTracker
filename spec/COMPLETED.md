# OE2 Empire Tracker — Completed Work Archive

This file records the details of all completed tasks. See [GOALS.md](GOALS.md) for current status.

---

## MVVM Across All Forms
Per AMB-016/017. All forms use lightweight hand-rolled ViewModels.

| Form | ViewModel(s) |
|------|-------------|
| FormColony + ColonyStructure | ColonyViewModel, ColonyStructureViewModel |
| FormPlayerProfile | PlayerProfileViewModel |
| FormBlueprint | BlueprintViewModel |
| FormSurvey | SurveyViewModel |
| MainWindow | N/A — menu shell only |

## 1. Colony Form — Structure Management
Add/remove flatpacks, Up/Down/Delete, ColonyStatusCalculator with Actual/Ideal, IColonyStructureWorkers, RtfBuilder, SetChildIndex reorder.

## 2. Colony Form — Item Management
Add items by all ItemTypes (Resource, Commodity, WorkDetail, Survey, Blueprint, ShipPart, ShipHull, Munition, Flatpack, SpaceBuildPackage, Share). Blueprint-based types populated from BlueprintType.OutputItemType. Delete key, ItemBag.CountByType.

## 3. Colony Form — Commodity Requests
Add/edit/delete commodity requests. CellValueChanged writes back. FullRowSelect. Validated Amount column.

## 4. Player Profile Form
ListView with filter, Save/Delete/Cancel, SkillName/SkillGroupName enums, delete confirmation dialog.

## 5. LockTracking
ItemKey, LockItem/LockItems/GetLockedQuantity/GetLocksForProcess/ClearLocksForProcess, JSON serialization. 30 tests.

## 6. Data Classes
WorkerDetail, CountDownTime, PropertyBag, Blueprint.ExtendedName, Survey.ExtendedName, Item.ExtendedName.

## 7. ColonyStatusCalculator Refactoring
IColonyStructureWorkers with Actual/Ideal implementations, unallocated worker tracking, bug fixes.

## 8. Unit Test Suite (465 tests)
BlueprintScanner, Blueprint, Commodity, CountDownTime, ItemBag, Item, ItemType, LockTracking, PlayerProfile, PropertyBag, ResourceClass, ResourceGroup, ResourcePurity, Survey, WorkerDetail, ColonyStatusCalculator, SurveyParser, Resource, ColonyStructure, BlueprintType. Test data in external files under TestData/.

## 10. MVVM Pilot — FormColony and ColonyStructure
ColonyStructureViewModel (IsBuilt/IsStaged/IsOnline, worker assignment, MoveUp/Down/Delete), ColonyViewModel (structure/item/commodity management, Save, RecalculateStatus).

## 11. Survey Import
SurveyParser: processHtml extracts PlanetName, SurveyID, DateTime, ScannedBy, resources with purity normalization (Med→Medium). Import button on FormSurvey. populateFormFromViewModel for unsaved imports. NLog clipboard logging. 44 tests.

## 12. Refinery Rig Support
Flatpacks/Refinery. Normal refining (Low=1x, Med=3x, High=5x of base 25). Synthetic refining via RefiningRecipes (S1: 1250→25, S2: 500→25). Tiered processing (normal→S1→S2). Whole-unit consumption. Top-of-hour timer alignment. Depleted resources removed.

## 13. Colony Structure — Built+Online Gate
All structure types require Built+Online before showing selection/process controls.

## 14. Mining Rig Improvements
Sub-selection hidden for empty survey. rtbProgressStatus shows rate/resource/purity. Top-of-hour alignment.

## 15. Research Laboratory Support
Flatpacks/ResearchLaboratory. ResearchTimeLookup (evo 0-14). One-shot timer. Creates evolved blueprint (evo+1, NickName="NEEDS SCANNED", empty resources). Added to blueprint list and warehouse.

## 16. Manufactory Support
Flatpacks/Manufactory. CanManufacture property. ManufactureTime parsing with normalization. txtQuantity for multi-item builds. Repeating timer, one item per interval. Items stack. Done completes one cycle only.

## 17. Synthetic Refining — Whole-Unit Consumption
Per-unit threshold (50 for S1, 20 for S2). Whole-unit consumption only. Combo key with tier suffix. Always repopulates from warehouse. Tab switch refreshes structures.

## 18. Colony Form — Item Grid Editing & Validation
CellValueChanged writes quantity back. DataGridViewValidatedTextBoxColumn with NUMBER_VALIDATION. CellValidating prevents invalid values. Empty=0. Same on commodity request grid.

## 19. Colony Form — Blueprint-Based Item Types
ShipPart, ShipHull, Munition, Flatpack, SpaceBuildPackage, Share populated from BlueprintType.OutputItemType.

## 20. Manufactory — Multi-Item Build Improvements
Done completes one cycle, keeps timer for remaining. txtQuantity shows actual quantity on load.

## 21. Worker Locking
Clear-and-rebuild in CalculateBuilt. Assigned workers locked against structure UUID. Unallocated workers locked against colony UUID. Zero-quantity items created when needed. Delete blocked for locked items.

## 22. Manufacturing Resource Locking
Lock (remaining × resource amount) for each blueprint resource. Zero-quantity items created. Locks update as items complete.

## 23. Blueprint Property Validation
BlueprintPropertyValidation lookup (54 properties: Integer/Decimal/Boolean/Time). BlueprintScanner strips units at scan time. CellValidating on dgvStatistics with type-specific patterns. Unknown properties logged.

## 24. NLog Logging
All Debug.Print replaced with NLog. PlayerContext/EmpireContext Info-level load/save. Forms Debug-level. Commodity Warn-level. SurveyParser Error-level.

## 25. Blueprint Resource Grid Validation
DataGridViewValidatedTextBoxColumn with NUMBER_VALIDATION. CellValidating prevents invalid Amount values.

## 26. Colony Bootstrap Algorithm (REQ-COL-096)
ColonyBootstrap generates foundation structures from surveys. Selects best survey per resource by refined output rate. Adds Command Centre → ROA → Miners → Refiners. Runs optimizer after. Appends only. ExtractionFocusLevel defaults to 0.

## 27. Build Order Optimization (REQ-COL-095)
BuildOrderOptimizer separates built/support/primary structures. Walks primary in order, inserts support to fix deficits. Auto-creates support structures from player blueprints when pool is empty. Respects MaxPerColony. Priority: Power(4) > Hab(3) > Food(2) > Entertainment(1).

## 28. Colony Structure Layout Fix
ColonyStructure_Layout was comparing flpStructureDetails with itself instead of including flpStructureStatus. Fixed to include all three panels. Added 2px for FixedSingle border. flpStructureStatus set to AutoSize=true.

## 29. Code Quality Scan
Full codebase scan completed. Identified 7 categories of issues: dead code, naming inconsistencies, large methods, duplicate code, magic strings/numbers, large files, duplicated ProgramaticUpdateGuard. Findings recorded in Rec #12.

## 30. Dead Code Removal (Rec 12a)
Removed commented-out fields, assignments, structure creation, layout traces, and unused `using` statements from ColonyStructure.cs, FormColony.cs, ColonyStatusCalculator.cs, and Colony.cs.

## 31. Extract Shared ProgrammaticUpdateGuard (Rec 12b)
Created `IProgrammaticUpdateSource` interface and shared `ProgrammaticUpdateGuard` class in Controls/ProgrammaticUpdateGuard.cs. ColonyStructure and FormColony implement the interface. Removed duplicated nested classes. Fixed typo throughout.

## 32. Method Naming — camelCase → PascalCase (Rec 12d)
Renamed all camelCase methods to PascalCase across the codebase. Covers form populate/update methods, context find/init methods, control helper methods, and data accessor methods. All callers updated across 15+ files. 465 tests passing.

## 33. Deduplicate Worker Parsing (Rec 12f)
Created `WorkerTypeInfo` class and `WorkerDetail.WorkerTypes` array. Replaced 12 near-identical worker parsing blocks across ColonyStructure.UpdateData and ColonyStatusCalculator.CalculateBuilt with data-driven loops. Added `GetUnallocatedPresent`/`SetUnallocatedPresent` helpers on ColonyStructureStatus. 465 tests passing.

## 34. Split Colony.cs into Separate Files (Rec 12g)
Extracted `ColonyStructure` and `CommodityRequested` classes from Colony.cs into their own files under Baseline/. Colony.cs now contains only the Colony class. 465 tests passing.

## 35. Magic Strings/Numbers → Constants (Rec 12c)
Created `Constants/GameConstants.cs` with RefiningBaseRate (25), WorkerVolume (50), SecondsPerHour (3600), PropBuilt/PropStaged/PropOnline, StatusActual/StatusIdeal, PurityRefined. Replaced all occurrences across 9 production files. Worker type strings already handled by WorkerDetail.WorkerTypes (Rec 12f). 465 tests passing.

## 36. Large Method Extraction (Rec 12e)
Extracted `ProcessMiningRig()` from inline block in `ProcessColony` — all 4 structure types now have their own method. Collapsed 5 resource accumulation blocks in `CalculateBuilt` into compact calls using `GetBlueprintDouble()` helper. Other methods (UpdateData, PopulateForm, ProcessHtml) already reduced to reasonable sizes by prior refactoring (12a, 12f). 465 tests passing.

## 37. Multi-Player Support (Rec 13, REQ-ARCH-070-075)
Phase 1: Added OwnerUUID to Colony, Blueprint, Survey. Added CurrentPlayerUUID and CurrentPlayerChanged event to PlayerContext. PlayerRoot persists CurrentPlayerUUID. Auto-migration assigns empty OwnerUUIDs to first player on load.
Phase 2: Player dropdown on MainWindow menu bar. Restores last selected player on startup.
Phase 3: Colony/Blueprint/Survey forms filter by current player. All forms subscribe to CurrentPlayerChanged and refresh. New items auto-assigned to current player on save.
Phase 4: ExtractionFocus (+1%/lvl) applied to mining. RefiningFocus (+2%/lvl) applied to normal and synthetic refining. ProductionFocus/Builder/ResearchFocus time reductions deferred until timer processing is implemented. 465 tests passing.

## 38. Global Blueprints in BaselineData.json (Rec 14)
Added `Blueprint[]` to `BaselineRoot` and `globalBlueprintList` to `EmpireContext`. `chkGlobalBlueprint` on Blueprint Form toggles blueprints between global (BaselineData.json) and player-specific (PlayerData.json). `FindBlueprint` searches both lists. `GetFilteredBlueprints` merges global + player blueprints. Save/delete handle both lists correctly. 465 tests passing.

## 39. Player Dropdown Refresh on Profile Save/Delete
Added `PlayerProfilesChanged` event to PlayerContext. PlayerProfileViewModel fires it after Save and Delete. MainWindow subscribes and repopulates the player dropdown, preserving current selection.

## 40. Unique Player Name Validation
Changed txtPlayerName to ValidatedTextBox on FormPlayerProfile. TextChanged handler checks for empty and duplicate names in real-time with red background. Save blocked when invalid. Fixed ValidatedTextBox: SetError/ClearError now set IsValid flag; added `_hasExternalError` flag so OnTextChanged/OnGotFocus/OnLostFocus respect externally-set errors and don't override them.

## 41. Grid CancelEdit Before Rows.Clear/Save Across All Forms
All DataGridView `Rows.Clear()` and save-handler grid iterations now detach `CellValidating` handlers, call `EndEdit()`, then reattach. Prevents `InvalidOperationException` when a cell is in edit mode with active validation. Applied to FormBlueprint (dgvStatistics, dgvResources), FormSurvey (dgvResources), FormColony (dgvItems, dgvCommodityRequests).

## 42. Replace All TextBox with ValidatedTextBox
Replaced `System.Windows.Forms.TextBox` with `ValidatedTextBox` in all 5 form Designer files. Without a ValidationPattern, ValidatedTextBox behaves identically to TextBox. Allows validation to be added to any field later without changing the control type.

## 43. PlayerProfile Form Resize Layout
Added layout handlers (flpBase_Layout, flpSearchList_Layout, flpPlayerData_Layout) so the list view, editing area, and command buttons all resize properly with the form.

## 44. Survey Form Resize Layout
Added layout handlers (flpBase_Layout, flpSearchList_Layout, flpSurveyData_Layout) with Dock=Fill on flpBase. Disabled AutoSize on flpSurveyDetails. dgvResources grid dynamically fills remaining vertical space. Command buttons stay near the bottom.

## 45. Blueprint Property Grid: ComboBox and CheckBox Cell Types
Extended `BlueprintPropertyValidation` with `ComboBox` and `CheckBox` property value types. `UpdatePropertyGrid` in FormBlueprint now swaps individual cells to `DataGridViewComboBoxCell` or `DataGridViewCheckBoxCell` based on property type. Added `GetComboBoxDataSource()` for data-driven combo sources. `CommodityIndustry` added as first ComboBox property (bound to CommodityIndustry static data). Boolean properties (`CanManufacture`, `CanResearch`, `Consumable`) changed to CheckBox type. Save handler reads bool values from checkbox cells correctly. `CommodityIndustry` added to `Flatpacks/CommodityFactory` properties in BaselineData.json. 465 tests passing.

## 46. Global Blueprints in All Colony/Structure Blueprint Lists
Fixed all blueprint selection lists (flatpack dropdown, warehouse items, research lab, manufactory, base blueprint list, scanner blueprints, bootstrap, optimizer) to include global blueprints from BaselineData.json.

## 47. Recalculate Colony Status on Item Quantity Change
dgvItems_CellValueChanged now triggers structures_ColonyStructureDataChanged to recalculate locks and unallocated worker availability when worker quantities are edited.

## 48. Optimizer Adds Support for Built Structure Deficits
BuildOrderOptimizer now checks for deficits in the built structure baseline before processing unbuilt primary structures. Creates support structures for existing deficits.

## 49. Centralized GetAllBlueprints()
Added PlayerContext.GetAllBlueprints() that merges player + global blueprint lists. Replaced all 8+ locations that were doing manual merges or missing global blueprints. Only save/delete mutations access blueprintList directly.

## 50. Commodity Manufacturing (Rec 11, CommodityFactory)
Implemented `Flatpacks/CommodityFactory` structure type. Selection combo filtered by blueprint's CommodityIndustry property. Added `ManufacturingCommodityName` to ColonyStructure. 10 commodities per cycle, 10-minute cycle time. Resource consumption from Commodity.ConstructionResources. Resource locking for entire run via LockCommodityFactoryResources. Commodities stack in warehouse. Done completes one cycle. Added CommodityFactory constant to BlueprintTypes and CommoditiesPerCycle/CommodityCycleSeconds to GameConstants. 465 tests passing.

## 51. Unit Tests for New Features (48 tests)
Added 48 new tests across 3 new files and 1 updated file: BlueprintPropertyValidationTests (15 — ComboBox/CheckBox types, validation patterns, data sources), WorkerTypeInfoTests (8 — array structure, key mappings), ColonyProcessingTests (20 — commodity factory processing, resource locking, skill multipliers for mining and refining), ColonyStructureTests (+5 — Get/SetUnallocatedPresent). Total: 513 tests passing.

## 52. Delivery Phase 1: SystemName on Colony/Survey (REQ-DEL-001-005)
Added `SystemName` string property to Colony and Survey. SurveyParser.ParseTitle now extracts SystemName from "PlanetName, SystemName (SurveyID)" format. Colony form has a System text field between Planet and Colony name. 513 tests passing.

## 53. Delivery Phase 2: DeliveryRoute/RouteStop Data Model (REQ-DEL-010-015)
Created `DeliveryRoute` (UUID, Name, OwnerUUID, List<RouteStop>) and `RouteStop` (ColonyUUID, Sequence) classes. Added `DeliveryRoute[]` to PlayerRoot. PlayerContext maintains `deliveryRouteList` with load/save. Added `GetCurrentPlayerRoutes()` helper. 513 tests passing.

## 54. Delivery Phase 3: Route Builder Form + Wiring (REQ-DEL-020-025)
Created FormDeliveryRoute with DeliveryRouteViewModel. Route list with filter, stop grid with Up/Down/Remove, colony picker showing "PlanetName - ColonyName (SystemName)", Save/Delete/New with confirmation dialog. Player change refreshes all data. Accessible from Edit → Delivery Routes. 513 tests passing.

## 55. Cascade Delete Player Data + Orphan Cleanup on Load
CascadeDeletePlayer removes all colonies, blueprints, surveys, and routes owned by deleted player. CleanupOrphanedData on load removes data owned by non-existent players with per-item Warn-level logging.

## 56. Prevent Duplicate Stops Checkbox on Route Builder
"No Duplicates" checkbox filters colony picker to exclude colonies already in the route. Refreshes on add/remove/new/clear. Off by default.

## 57. Delivery Phase 4: DeliveryPlan Data Model (REQ-DEL-030-035)
Created `DeliveryPlan` (UUID, Name, OwnerUUID, RouteUUID, List<DeliveryPlanStop>), `DeliveryPlanStop` (ColonyUUID, Sequence, DropOff list, PickUp list), and `DeliveryItem` (ItemType, BaseItemTypeID, Name, Quantity, Delivered). Added to PlayerRoot, PlayerContext (deliveryPlanList, GetCurrentPlayerPlans), cascade delete, and orphan cleanup. 513 tests passing.

## 58. Delivery Phase 5: Planning Tab Wired Up (REQ-DEL-040-044)
Created DeliveryPlanViewModel with stop management, add/remove drop-off and pick-up items. Plan tab on route builder: select a stop on Stops tab, switch to Plan tab to see/edit drop-off and pick-up lists. Item type → item picker → quantity pattern (same as colony warehouse). Plan auto-created per route, persisted on save. 513 tests passing.

## 59. Multi-Plan Support + Plan Selector UX
Plan tab updated with multi-plan support: plan dropdown with filter and "Show Completed" checkbox, New/Delete/Execute buttons, plan name validation (red when empty), auto-select first open plan on route load, auto-create plan on first item add, preserve selection on filter/save/toggle. 513 tests passing.
