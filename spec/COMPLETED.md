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

## 60. Delivery Phase 6: Execution Form Wired Up (REQ-DEL-050-057)
FormDeliveryExecution with route/plan selectors, consolidated load list (calculates pre-load needs by subtracting earlier pick-ups), scrollable stop-by-stop sections with checkboxes for each item. Auto-save on check. Plan marked as Completed when all items delivered. Execute button on route builder launches with pre-selected route/plan. Accessible from Edit menu. Complete Stop button hides delivered stops. Complete Plan and Delete Plan buttons. Extended Name column on load grid. Purity-aware grouping in load list calculation. CalculateLoadList extracted to DeliveryPlan instance method. 581 tests passing.

## 61. Delivery Feature Unit Tests (68 tests)
Added 68 new tests across 4 new test files covering all delivery data models and view models:
- DeliveryRouteTests (8): default constructor, properties, RouteStop, JSON round-trip with/without stops
- DeliveryPlanTests (22): DeliveryPlan/DeliveryPlanStop/DeliveryItem defaults, ExtendedName (with/without purity), JSON round-trip (empty, completed, with items, ItemType as string, ExtendedName not serialized, StopCompleted), CalculateLoadList (empty, drop-off only, pick-up before/after drop-off, partial pick-up, multi-stop aggregation, different purities, purity preserved, sequence ordering, mixed item types, result sorting)
- DeliveryRouteViewModelTests (16): AddStop (single, multiple), RemoveStop (valid, invalid, negative, multi-select), MoveStopUp/Down (middle, boundary), MoveStopsUp/Down (multi-select), Reset, SelectRoute (new, null), sequential renumbering
- DeliveryPlanViewModelTests (14): GetOrCreateStop (new, existing, different colonies), AddDropOffItem (basic, with purity, no purity), AddPickUpItem, AddMultipleItems, RemoveDropOffItems (single, multiple, invalid), RemovePickUpItems (single, negative), constructor validation (null plan, null context), UUID property

Also extracted `CalculateLoadList` from `FormDeliveryExecution` to `DeliveryPlan.CalculateLoadList()` instance method for testability. 581 tests passing.

## 62. Commodity Delivery Loop: Auto-Fill + Fulfillment
Two-part feature closing the loop between colony commodity requests and delivery execution:

Part A — Auto-Fill: `AutoFillCommodities` method on `DeliveryPlanViewModel` (accepts colony-finder delegate for testability). Scans each stop's colony for unfulfilled `CommodityRequested` entries (`!Fulfilled && Requested - Delivered > 0`), adds drop-off `DeliveryItem` for the shortfall. Additive, drop-off only. `FormAutoFill` modal dialog with Commodities checkbox enabled, Flatpacks/Resources/Workers disabled with "(Future)" labels. Auto-Fill button on plan tab, visible when plan selected.

Part B — Fulfillment: `DeliveryItem_CheckedChanged` extended for commodity items. On check: finds matching `CommodityRequested` on target colony by name, sets `Delivered = Requested`, `Fulfilled = true`. On uncheck: sets `Delivered = 0`, `Fulfilled = false`. Logs warning if colony or CommodityRequested not found. All-or-nothing per line item.

9 new unit tests for AutoFillCommodities (unfulfilled, partial, fulfilled skip, zero shortfall, missing colony, additive, pick-up invariant, empty commodities, multi-stop mixed). 590 tests passing.

## 63. Commodity Request Grid: Completed, NeedBy, Strikethrough, Auto-Delete
Added Completed (checkbox) and Need By (countdown format) columns to the colony form commodity request grid. Fulfilled requests shown with strikethrough + gray text. NeedBy input on add row and editable in grid using CountDownTime format (e.g. "2d 6h 30m"). Completed checkbox editable in grid — checking sets Delivered=Requested, unchecking reverses. Fulfilled requests 3+ days past NeedBy auto-deleted on colony load. ColonyDataChanged event on PlayerContext for cross-form refresh when delivery execution updates a colony. ProgrammaticUpdateGuard used throughout to prevent StackOverflowException from cascading grid events. 590 tests passing.

## 64. Forms Steering Compliance (Rec 16)
Audited all forms against `.kiro/steering/forms.md` and applied `IProgrammaticUpdateSource` to all 6 non-compliant forms: FormBlueprint, FormSurvey, FormDeliveryRoute, FormDeliveryExecution, FormPlayerProfile, MainWindow. Added `ProgrammaticUpdateGuard` to all grid-mutating methods and `_isProgrammaticUpdate > 0` guard checks to all grid event handlers. FormPlayerProfile now subscribes to `CurrentPlayerChanged`. ColonyStructure left as-is (managed by parent FormColony). 590 tests passing.

## 65. Delivery Phase 7: Auto-Fill Flatpacks, Resources, Workers + Flatpack Staging
Extended auto-fill from commodity-only to all four request types:
- AutoFillFlatpacks: scans for unbuilt+unstaged structures (IsBuilt=false AND IsStaged=false), adds Flatpack drop-offs with blueprint ExtendedName
- AutoFillManufacturingResources: scans for StagingResources=true structures (Manufactory/CommodityFactory), calculates resource shortfalls (needed × quantity - warehouse Refined stock), adds Resource drop-offs
- AutoFillWorkers: computes ideal vs actual worker gaps via ColonyStatusCalculator, adds WorkDetail drop-offs per worker type
- StagingResources boolean property on ColonyStructure + ColonyStructureViewModel, "Stage Resources" checkbox on ColonyStructure UI for Manufactory/CommodityFactory
- Flatpack staging on delivery execution: checking flatpack delivery sets Properties["Staged"]="True" on matching colony structure, fires ColonyDataChanged
- All FormAutoFill checkboxes enabled, orchestration calls all four methods
- 22 new tests. 612 total tests passing.

## 66. ProgrammaticUpdateGuard IDisposable Fix
Critical bug: ProgrammaticUpdateGuard relied on the GC finalizer to decrement `_isProgrammaticUpdate`, but finalizers run at unpredictable times. After the first `PopulateForm` call, the counter stayed > 0 permanently, blocking all grid event handlers (CellValueChanged, SelectionChanged, CellValidating) for the rest of the form's lifetime. This caused commodity request grid edits (NeedBy, Completed checkbox) to silently fail, and plan grid population to not show items.

Fix: Made ProgrammaticUpdateGuard implement IDisposable. Converted all 22 usages across 6 files to `using var guard` (C# 8 using declaration) for deterministic disposal. Added `LangVersion=latest` to .csproj. Removed 16 manual `guard.release()` calls. Also added `CurrentCellDirtyStateChanged` + `CommitEdit` for DataGridViewCheckBoxColumn immediate commit. 612 tests passing.

## 67. Incremental Execution Form Updates, Worker Delivery, Event Unsubscription
- Delivery execution form: incremental updates on checkbox change instead of full rebuild (no flicker, no scroll reset)
- Worker delivery: adds workers to colony warehouse on check, removes on uncheck, fires ColonyDataChanged
- ColonyDataChanged handler does full RecalculateStatus + PopulateForm for background processing readiness
- All 7 forms unsubscribe from PlayerContext events in OnFormClosed (prevents ObjectDisposedException)
- Anonymous lambda event subscriptions converted to named methods for proper unsubscription
- Flatpack auto-fill stacks same-blueprint flatpacks into single item with aggregated quantity
- Manufacturing quantity persists on txtQuantity TextChanged and loads from data model on form populate
- Stage Resources checkbox persists quantity when checked
- 612 tests passing.

## 68. Deferred Write-Through Conversion (Rec 17)
All editable controls across 5 forms now write to the data model immediately on change via TextChanged/SelectedIndexChanged handlers. Save buttons simplified to only call writeContext() for disk persistence. Forms: FormColony (3 fields), FormBlueprint (4 text + 5 combos), FormSurvey (9 text + 1 combo), FormPlayerProfile (11 text + 1 combo), FormDeliveryRoute (2 text). 612 tests passing.

## 69. Data Change Events for All Entity Types (Rec 18)
Events added on PlayerContext with EventArgs classes:
- `BlueprintDataChanged(BlueprintUUID)` — covers both player and global blueprints
- `SurveyDataChanged(SurveyUUID)`
- `DeliveryDataChanged` — plain EventArgs (routes/plans refresh as a set)
- `PlayerProfileDataChanged(PlayerUUID)`

All ViewModel Save/Delete methods fire the appropriate event:
- BlueprintViewModel → OnBlueprintDataChanged (player + global)
- SurveyViewModel → OnSurveyDataChanged
- DeliveryRouteViewModel → OnDeliveryDataChanged
- DeliveryPlanViewModel → OnDeliveryDataChanged
- PlayerProfileViewModel → OnPlayerProfileDataChanged
- ColonyViewModel → OnColonyDataChanged

Forms subscribed with IsDisposed safety check, unsubscribe in OnFormClosed. Global blueprint changes route through PlayerContext since all forms use GetAllBlueprints(). 617 tests passing.

## 70. Safe File Writer (Temp+Replace Persistence)
Created `SafeFileWriter.WriteAllText` utility that writes to a `.tmp` file first, then atomically swaps it into place via `File.Replace`, keeping the previous version as a `.bak` file for one-deep recovery. If the write to the temp file fails (crash, disk full), the original file is untouched. If the target doesn't exist yet (first save), falls back to `File.Move`. Both `PlayerContext.writeContext()` and `EmpireContext.writeContext()` now use `SafeFileWriter` instead of `File.WriteAllText`. Added `.json.bak` to `.gitignore`. 9 unit tests covering new file, existing file, backup creation, temp cleanup, multiple saves, empty content, and large content. 626 tests passing.

## 71. Colony Daily Build Feature
Full structure building lifecycle: staged → building (with countdown) → built.

Domain logic:
- `BuildTimeCalculator.Calculate(builderSkillLevel)`: 86400 × (1 - level × 0.02), min 1 second
- `ColonyBuildEligibility`: static methods for IsStagedStructure, IsBuildingStructure, IsEligible, GetFirstStagedStructure
- `Colony.ProcessColony()` new step 1: build completion — expired BuildCompletionTime sets Built=true, clears timer (before mining/refining/etc.)

FormColonyDailyBuild (new form):
- Route selector with filter, scrollable content panel showing eligible colonies
- Each eligible colony shows first staged structure's ExtendedName + Build button
- Build click: sets IsStaged=false, creates BuildCompletionTime with skill-adjusted duration, persists, fires ColonyDataChanged, removes colony from list
- Subscribes to CurrentPlayerChanged + ColonyDataChanged, unsubscribes in OnFormClosed
- Accessible from Edit → Colony Daily Build

ColonyStructure control changes:
- Building state: shows countdown timer + Done button, hides process controls, disables Built checkbox
- Staged state: shows Build button (when no sibling building), hides selection/manufacturing controls
- Build button visibility fix: cmdStart is inside flpManufacturingControls inside flpSelection — both parent panels must be visible with sibling controls hidden
- txtCompletionTime_Leave now writes back to BuildCompletionTime (was only writing to ProcessCompletionTime)

22 new tests (BuildTimeCalculatorTests, ColonyBuildEligibilityTests, ColonyBuildCompletionTests). 648 tests passing.

## 72. Colony Activity Form (Rec 4: CountDownTime Master List)
Read-only form aggregating all active countdown timers and unfulfilled commodity requests across all colonies for the current player.

Domain logic:
- `ColonyActivityCollector.CollectActivities(colonies, playerContext)`: scans structures for BuildCompletionTime and ProcessCompletionTime, classifies by BluePrintType (Building, Mining, Refining, Research, Manufacturing, CommodityManufacturing), collects unfulfilled CommodityRequested entries
- `ActivityType` enum: Building, Manufacturing, CommodityManufacturing, CommodityRequest, Research, Mining, Refining
- `ActivityRow` POCO: Type, SystemName, ColonyName, SourceName, ProcessDetails, CountDown ref, NeedBy DateTime, GetSecondsRemaining(), GetTimeRemainingString(), FormatSeconds()
- Source name: "#gameSequence ExtendedName" for structures, "Commodity Request" for commodities
- Process details match existing ColonyStructure PopulateProgressStatus patterns per type

FormColonyActivity:
- Multi-select activity type filter checkboxes (Mining/Refining off by default)
- Cross-column text filter (case-insensitive substring match on any visible column)
- DataGridView with columns: CountDownTime, System Name, Colony Name, Activity Type, Source, Process Details + hidden SecondsRemaining for numeric sort
- Default sort: time remaining ascending (soonest first), sortable on any column
- 1-second auto-refresh timer updates countdown display
- Subscribes to CurrentPlayerChanged + ColonyDataChanged, unsubscribes in OnFormClosed
- Accessible from Edit → Colony Activity

Bug fixes during testing:
- All 5 ColonyStructure process Start handlers (mining, refining, research, manufacturing, commodity manufacturing) now fire ColonyStructureDataChanged
- FormColony.structures_ColonyStructureDataChanged now fires PlayerContext.OnColonyDataChanged for cross-form notification
- txtCompletionTime_Leave now fires ColonyStructureDataChanged so manual timer edits propagate

14 new tests (4 property tests + 10 edge case tests). 662 tests passing.

## 73. Window State Persistence
Full UI state persistence across application restarts. Data models (UIPreferences, WindowPosition, WindowState, FormControlState, ComboState, GridState, GridColumnState), PreferencesStore singleton with JSON persistence to `%LOCALAPPDATA%\OE2EmpireTracker\UIPreferences.json`, BoundsValidator for multi-monitor support (min 320×200, 100px edge margin, off-screen reset), WindowStateHelper for automatic control tree walking (DataGridView columns/sort, TextBox text, ComboBox selection). MainWindow saves/restores position, assigns per-type window numbers (#N prefix). All 8 MDI child forms save state on close. AllowUserToOrderColumns and sorting enabled on all DataGridViews. 25 new tests (UIPreferences round-trip, BoundsValidator, PreferencesStore). 729 tests passing.

## 74. Delivery Fulfillment Unit Tests
Extracted commodity fulfillment, flatpack staging, and worker delivery logic from FormDeliveryExecution into static `DeliveryFulfillment` helper class. Three methods: `FulfillCommodity` (sets Delivered=Requested/Fulfilled=true or reverses), `StageFlatpack` (sets Staged property on matching structure), `DeliverWorkers` (adds/removes workers from colony warehouse). FormDeliveryExecution delegates to the helper. 17 new tests covering all three operations including null/edge cases. 746 tests passing.

## 75. Pre-Existing Test Fixes
Fixed 5 pre-existing test failures:
- ColonyActivityCollectorTests.Property4: adjusted manufacturing assertions to expect 1-based display (ManufacturingCompleted + 1) matching intentional UI behavior
- MainMenuOverhaulTests (4 tests): added EmpireContext.FilePath initialization pointing to actual BaselineData.json location using correct relative path from test bin directory

## 76. Specification Documentation Gap Fill
Created 9 new requirements documents for previously undocumented features: GameMechanics.md (mining, refining, synthetic recipes, manufacturing, research, build time), BackgroundProcessing.md (60s timer, error handling), ColonyImport.md (HTML parsing, merge semantics), BlueprintProperties.md (54 property validation rules, commodity industries), UIStatePersistence.md (window state summary), ColonyActivity.md (activity form, filtering), ColonyDailyBuild.md (build eligibility), DataChangeEvents.md (event types, write-through), SafeFileWriter.md (atomic writes). Updated requirements/README.md index.

## 77. Project Documentation Consolidation
Created spec/BACKLOG.md with 18 open features including dependency graph and suggested build order. Created spec/README.md linking project docs and .kiro specs. Merged old OE2EmpireTracker/specs/ folder content into spec/requirements/ (skill tree reference, colony processing order) and deleted the old folder. Updated steering files: workflow.md (build+test gate before commits), tech.md (VS 2026 path), forms.md (added missing description).

## 78. Directory Restructure
Reorganized the main project and test project from two catch-all directories (`Baseline/` and `Data/`) into four purpose-driven directories: `Models/` (33 files — POCOs, data types, enums, interfaces), `Services/` (11 files — singletons, processing logic, business rules), `Parsers/` (2 files — HTML/data import), `Persistence/` (3 files — file I/O, window state). All namespace declarations, csproj Compile Include entries, and using statements updated across the entire codebase. Test project mirrors the new structure. Executed in 6 atomic batches with build+test verification at each step. Steering docs updated. Fixed 3 pre-existing test failures caused by static FilePath leaking between test fixtures (added SetUp/TearDown to ContextFilePathTests, added TearDown to DeliveryPlanViewModelTests, added null-safety to FindGlobalBlueprint). 750 tests passing.
