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
