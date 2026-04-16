# Requirements: Colony Form Rewrite

## Introduction

Rewrite FormColony and the ColonyStructure user control from scratch to produce cleaner, more performant code while preserving all existing functionality. The current form is 2041 lines (FormColony.cs) plus 1859 lines (ColonyStructure.cs) of accumulated features with major performance problems — structure controls are rebuilt on every change, layout calculations cascade unnecessarily, and tab content is only partially deferred. The rewrite uses the existing spec documents (spec/requirements/Colony.md) and current code as the authoritative source of requirements. The new form lives alongside the old one during development (parallel development strategy).

## Glossary

- **Colony_Form**: The main WinForms form for managing colonies (FormColony / FormColonyV2)
- **Colony_Structure_Control**: The UserControl that displays a single structure within a colony
- **Colony_ViewModel**: The ViewModel wrapping a Colony data object, exposing typed operations
- **Colony_Structure_ViewModel**: The ViewModel wrapping a ColonyStructure, hiding PropertyBag access
- **Colony_Status_Calculator**: The service that computes cumulative resource status across structures
- **Colony_Parser**: The service that parses game HTML clipboard data into Colony objects
- **Colony_Import_Helper**: The service that handles dedup and merge logic for colony imports
- **Build_Order_Optimizer**: The service that reorders structures for optimal build sequence
- **Colony_Bootstrap**: The service that generates foundation structures for a new colony
- **Colony_Admin_Report_Builder**: The service that generates the administration tab status report
- **Tab_Warning_Service**: The service that evaluates warning levels for tab coloring
- **Player_Context**: The singleton managing player data and persistence
- **Structure_Pool**: A reusable pool of ColonyStructure controls to avoid create/dispose overhead
- **ProgrammaticUpdateGuard**: A guard that suppresses write-through during programmatic UI updates


## Requirement 1: Colony List with Filtering

**User Story:** As a player, I want to browse and filter my colonies so I can find specific colonies quickly.

### Acceptance Criteria

1. THE Colony_Form SHALL display a ListView of all colonies belonging to the current player with columns: Planet, Name, Refs.
2. WHEN the user types in the name filter field, THE Colony_Form SHALL perform case-insensitive substring matching on PlanetName and ColonyName.
3. WHEN the user clicks a column header, THE Colony_Form SHALL sort the list by that column, toggling between ascending and descending order.
4. THE Colony_Form SHALL use FullRowSelect mode and single-selection on the colony ListView.
5. WHEN CurrentPlayerChanged fires, THE Colony_Form SHALL clear the colony list and repopulate from the new player's colonies.
6. THE Colony_Form SHALL display the title bar as "Manage Colonies - {PlayerName} : {ColonyCount}" with live counts.
7. THE Refs column SHALL show the TotalCount from ColonyReferenceCounter for each colony.

## Requirement 2: Colony CRUD

**User Story:** As a player, I want to create, edit, save, and delete colonies.

### Acceptance Criteria

1. WHEN the user clicks New, THE Colony_Form SHALL reset the form to a blank colony with no UUID and clear the ListView selection.
2. WHEN the user clicks Save, THE Colony_Form SHALL set OwnerUUID to the current player if not already set, call ColonyViewModel.Save(), refresh the colony list, and update the title bar.
3. WHEN the user clicks Delete and the colony has zero references, THE Colony_Form SHALL prompt for confirmation and remove the colony from Player_Context.
4. WHEN the user clicks Delete and the colony has references, THE Colony_Form SHALL display a message showing the route and plan reference counts and prevent deletion.
5. THE Delete button SHALL display "In Use (N)" text and be disabled when ColonyReferenceCounter.TotalCount is greater than zero.
6. WHEN the user edits PlanetName, ColonyName, or SystemName, THE Colony_Form SHALL write-through to the Colony_ViewModel immediately via TextChanged handlers.
7. THE ProgrammaticUpdateGuard SHALL suppress write-through during programmatic population of form fields.

## Requirement 3: Colony Selection and Form Population

**User Story:** As a player, I want to select a colony and see all its data populated in the form.

### Acceptance Criteria

1. WHEN the user selects a colony from the ListView, THE Colony_Form SHALL create a new Colony_ViewModel for the selected colony and populate all form fields.
2. THE Colony_Form SHALL populate PlanetName, ColonyName, and SystemName text fields from the Colony_ViewModel.
3. THE Colony_Form SHALL populate structure controls in the Structures tab from the colony's Structures list.
4. THE Colony_Form SHALL call RecalculateStatus() on the Colony_ViewModel and refresh the status display.
5. THE Colony_Form SHALL defer population of non-active tabs using dirty flags (_structuresDirty, _warehouseDirty, _workersDirty) and populate them when the user switches to that tab.
6. THE Colony_Form SHALL update the Delete button state and refresh the admin report when a colony is selected.


## Requirement 4: Structure Management

**User Story:** As a player, I want to add, reorder, and remove structures in my colony.

### Acceptance Criteria

1. THE Colony_Form SHALL display a flatpack filter text field and a filtered combo box showing all Flatpack blueprints sorted alphabetically by name.
2. WHEN the user selects a flatpack and clicks Add, THE Colony_Form SHALL call ColonyViewModel.AddStructure(), create a Colony_Structure_Control, wire the ColonyStructureDataChanged event, add the control to the structure panel, recalculate status, and refresh the display.
3. WHEN the user clicks the Up button on a Colony_Structure_Control, THE Colony_Structure_ViewModel SHALL move the structure one position earlier in the colony's Structures list.
4. WHEN the user clicks the Down button on a Colony_Structure_Control, THE Colony_Structure_ViewModel SHALL move the structure one position later in the colony's Structures list.
5. WHEN the user clicks the Delete button on a Colony_Structure_Control, THE Colony_Structure_ViewModel SHALL remove the structure from the colony's Structures list.
6. WHEN the user presses the Delete key while a Colony_Structure_Control is focused, THE Colony_Structure_Control SHALL delete that structure.
7. WHEN any structure change occurs (add, reorder, delete, worker assignment, state change), THE Colony_Form SHALL call RecalculateStatus() and refresh all visible structure controls.
8. WHEN a structural change occurs (add, reorder, delete), THE Colony_Form SHALL rebuild the control layout using the Structure_Pool, returning orphaned controls to the pool instead of disposing them.
9. WHEN a non-structural change occurs (worker toggle, built/online state), THE Colony_Form SHALL update only the affected control without rebuilding all controls.

## Requirement 5: Structure Type Filter

**User Story:** As a player, I want to filter which structure types are visible so I can focus on specific structure categories.

### Acceptance Criteria

1. THE Structures tab SHALL display a checkbox ListView (lvwStructureTypes) showing all flatpack blueprint types from BaselineData.json, sorted alphabetically by name.
2. THE structure type filter list SHALL be static — showing all flatpack types, not just those in the current colony.
3. WHEN the form loads, all structure types SHALL be checked by default unless the user has previously unchecked types (restored from UIPreferences).
4. WHEN the user unchecks a structure type, THE Colony_Form SHALL hide all Colony_Structure_Controls of that type.
5. WHEN the user checks a structure type, THE Colony_Form SHALL show all Colony_Structure_Controls of that type.
6. THE unchecked structure types SHALL persist across colony switches and application restarts via UIPreferences.json.

## Requirement 6: Structure State and Display

**User Story:** As a player, I want to see and control the state of each structure (staged, built, online) with clear visual indicators.

### Acceptance Criteria

1. EACH Colony_Structure_Control SHALL display the blueprint name, display sequence number, and Actual/Ideal resource status using RtfBuilder for colored text.
2. THE display sequence number SHALL be a per-blueprint-type counter (e.g., two Power Plants are #1 and #2; a Habitation is also #1).
3. THE Colony_Structure_Control background color SHALL indicate state: Yellow for Staged, PaleVioletRed for Built but Offline, LightGreen for Online with missing workers, Green for Online with all workers assigned.
4. WHEN the user checks the Built checkbox, THE Colony_Structure_ViewModel SHALL set IsBuilt=true and IsStaged=false.
5. WHEN the user checks the Online checkbox, THE Colony_Structure_ViewModel SHALL set IsOnline=true, IsBuilt=true, and IsStaged=false.
6. WHEN the user checks the Staged checkbox, THE Colony_Structure_ViewModel SHALL set IsStaged=true, IsBuilt=false, and IsOnline=false.
7. EACH state change SHALL trigger UpdateData() on the control and fire ColonyStructureDataChanged.


## Requirement 7: Worker Assignment

**User Story:** As a player, I want to assign and unassign workers to structures and see the impact on colony resources.

### Acceptance Criteria

1. EACH Colony_Structure_Control SHALL display up to three worker checkboxes based on the blueprint's worker property keys (BlueCollar, WhiteCollar, Specialist).
2. THE worker checkboxes SHALL reflect the current AssignedWorkers PropertyBag state when the control is loaded.
3. WHEN the user toggles a worker checkbox, THE Colony_Structure_ViewModel SHALL update the AssignedWorkers PropertyBag immediately and fire ColonyStructureDataChanged.
4. WHEN a worker checkbox changes, THE Colony_Structure_Control SHALL update its background color without a full UpdateData() repaint.
5. THE Colony_Structure_Control SHALL display unallocated worker checkboxes (disabled, read-only) for structures that have UnassignedBlueCollarDetail, UnassignedWhiteCollarDetail, or UnassignedSpecialistDetail properties.
6. THE unallocated worker checkbox state SHALL be determined by the Colony_Status_Calculator's actual status for that structure.

## Requirement 8: Colony Status Display

**User Story:** As a player, I want to see the overall colony resource status (power, habitation, food, entertainment, warehouse) at a glance.

### Acceptance Criteria

1. THE Structures tab SHALL display a status summary panel showing Power, Habitation, Food, Entertainment, and Warehouse status using colored RTF text.
2. EACH resource label SHALL be colored red when Required exceeds Provided (or Capacity for Warehouse), and green otherwise.
3. THE status display SHALL show the format "Required/Provided" for each resource.
4. WHEN any structure change occurs, THE Colony_Form SHALL refresh the status display from the Colony_Status_Calculator's finalActualStatus.
5. EACH Colony_Structure_Control SHALL display per-structure Actual and Ideal status lines using the same colored format.

## Requirement 9: Mining Rig Controls

**User Story:** As a player, I want to configure and operate mining rigs within my colony structures.

### Acceptance Criteria

1. WHEN a structure's blueprint type is MiningRig and the structure is built and online, THE Colony_Structure_Control SHALL display survey selection and resource selection combos.
2. THE survey combo SHALL be filtered to surveys matching the colony's PlanetName.
3. WHEN the user selects a survey, THE Colony_Structure_Control SHALL populate the resource combo with that survey's resources.
4. WHEN the user selects a resource and clicks Start, THE Colony_Structure_Control SHALL create a repeating CountDownTime aligned to the next clock-hour boundary and start the countdown timer.
5. THE countdown display SHALL update at the interval specified by PreferencesStore.Preferences.Thresholds.CountdownRefreshRateSeconds while the timer is running.
6. WHEN the user clicks Done, THE Colony_Structure_Control SHALL call Colony.ProcessColony() under the ProcessingLock, stop the timer, and clear the process state.
7. WHEN MiningSurvey or MiningSurveyResource changes, THE Colony_Structure_Control SHALL reset MiningLeftOvers to zero.
8. THE progress status SHALL display the mining rate, resource name, and purity (e.g., "5/h Post-Trans Metals (High)").
9. WHEN the structure is not built or not online, THE Colony_Structure_Control SHALL hide all mining controls.


## Requirement 10: Refinery Controls

**User Story:** As a player, I want to configure and operate refineries to process raw resources into refined materials.

### Acceptance Criteria

1. WHEN a structure's blueprint type is Refinery and the structure is built and online, THE Colony_Structure_Control SHALL display a resource selection combo.
2. THE resource selection combo SHALL show unrefined resources from the colony warehouse, resources being actively mined, and synthetic recipes whose input resources are available in sufficient quantity.
3. WHEN the user selects a resource and clicks Start, THE Colony_Structure_Control SHALL create a repeating CountDownTime aligned to the next clock-hour boundary.
4. THE progress status SHALL display the refining rate and resource info (e.g., "25:75 Post-Trans Metals (Medium)" for normal refining, or "50:10 Synthium" for synthetic recipes).
5. WHEN the user clicks Done, THE Colony_Structure_Control SHALL call Colony.ProcessColony() under the ProcessingLock.
6. THE refinery selection SHALL use a compound key format "ResourceName|Purity" for normal resources and "ResourceName|Purity|S{Tier}" for synthetic recipes.

## Requirement 11: Research Lab Controls

**User Story:** As a player, I want to configure and operate research labs to evolve blueprints.

### Acceptance Criteria

1. WHEN a structure's blueprint type is ResearchLaboratory and the structure is built and online, THE Colony_Structure_Control SHALL display a blueprint selection combo.
2. THE blueprint selection combo SHALL show blueprints that have Evolution less than 15, pass ResearchTimeLookup.CanResearchEvolution(), and have "Can Research" property not set to false.
3. WHEN the user selects a blueprint and clicks Start, THE Colony_Structure_Control SHALL create a one-shot CountDownTime with duration from ResearchTimeLookup, reduced by the owner's ResearchFocus skill level (3% per level).
4. THE progress status SHALL display the evolution transition and blueprint name (e.g., "Evo 3->4 Mining Rig").
5. WHEN the user clicks Done, THE Colony_Structure_Control SHALL call Colony.ProcessColony() which creates the evolved blueprint and adds it to the colony warehouse.
6. IF the researching blueprint UUID is null or the referenced blueprint does not exist, THEN THE Colony_Structure_Control SHALL clear the orphaned research state.

## Requirement 12: Manufactory Controls

**User Story:** As a player, I want to configure and operate manufactories to produce items from blueprints.

### Acceptance Criteria

1. WHEN a structure's blueprint type is Manufactory and the structure is built and online, THE Colony_Structure_Control SHALL display a blueprint selection combo, quantity input, and Stage Resources checkbox.
2. THE blueprint selection combo SHALL show blueprints that have "Can Manufacture" property not set to false.
3. WHEN the user selects a blueprint, enters a quantity, and clicks Start, THE Colony_Structure_Control SHALL create a repeating CountDownTime with interval from the blueprint's "Manufacture Run Time" property, reduced by the owner's ProductionFocus skill level (3% per level).
4. THE progress status SHALL display the manufacturing progress and blueprint name (e.g., "(2/5) Mining Rig Ev3").
5. WHEN the user clicks Done, THE Colony_Structure_Control SHALL call Colony.ProcessColony() which produces one item per completed interval.
6. WHEN all items are manufactured (ManufacturingCompleted >= ManufacturingQuantity), THE Colony_Structure_Control SHALL clear the ProcessCompletionTime.
7. THE Stage Resources checkbox SHALL be visible when no manufacturing is running, and SHALL write-through to ColonyStructureViewModel.StagingResources.
8. IF the manufacturing blueprint UUID is null or the referenced blueprint does not exist, THEN THE Colony_Structure_Control SHALL clear the orphaned manufacturing state.


## Requirement 13: Commodity Factory Controls

**User Story:** As a player, I want to configure and operate commodity factories to produce commodities.

### Acceptance Criteria

1. WHEN a structure's blueprint type is a commodity factory and the structure is built and online, THE Colony_Structure_Control SHALL display a commodity selection combo, quantity input (number of cycles), and Stage Resources checkbox.
2. THE commodity selection combo SHALL be filtered by the blueprint's "Commodity Industry" property.
3. WHEN the user selects a commodity, enters a cycle count, and clicks Start, THE Colony_Structure_Control SHALL create a repeating CountDownTime with the commodity cycle interval, reduced by the owner's ProductionFocus skill level (3% per level).
4. THE progress status SHALL display the cycle progress, commodity name, and per-cycle quantity (e.g., "(2/5) Electronics x10").
5. WHEN the user clicks Done, THE Colony_Structure_Control SHALL call Colony.ProcessColony() which produces commodities and consumes construction resources per cycle.
6. WHEN all cycles are complete, THE Colony_Structure_Control SHALL clear the ProcessCompletionTime.
7. IF the manufacturing commodity name is null, THEN THE Colony_Structure_Control SHALL clear the orphaned ProcessCompletionTime.

## Requirement 14: Structure Building

**User Story:** As a player, I want to build staged structures with a construction timer.

### Acceptance Criteria

1. WHEN a structure is Staged and not Built, and no other structure in the colony is currently building, THE Colony_Structure_Control SHALL display a "Build" button.
2. WHEN the user clicks Build, THE Colony_Structure_Control SHALL set IsStaged=false, create a BuildCompletionTime with duration from BuildTimeCalculator (accounting for the owner's Builder skill level), and fire ColonyStructureDataChanged.
3. WHILE a structure has an active BuildCompletionTime, THE Colony_Structure_Control SHALL display the countdown timer, "Building..." status text, and a Done button.
4. WHEN the user clicks Done on a building structure, THE Colony_Structure_Control SHALL force the BuildCompletionTime to zero, call Colony.ProcessColony() under the ProcessingLock, and refresh the control.
5. THE Colony_Structure_Control SHALL disable the Built checkbox while a build timer is active.

## Requirement 15: Timer Display and Manual Adjustment

**User Story:** As a player, I want to see countdown timers for active processes and manually adjust them when needed.

### Acceptance Criteria

1. THE Colony_Structure_Control SHALL display a countdown text field that updates at the configured refresh rate while a timer is running.
2. WHEN the user clicks into the countdown text field, THE Colony_Structure_Control SHALL stop automatic updates to allow manual editing.
3. WHEN the user leaves the countdown text field, THE Colony_Structure_Control SHALL parse the entered value and update the ProcessCompletionTime or BuildCompletionTime accordingly.
4. THE timer refresh interval SHALL be read from PreferencesStore.Preferences.Thresholds.CountdownRefreshRateSeconds (minimum 1 second).


## Requirement 16: Colony Warehouse (Items)

**User Story:** As a player, I want to manage the items stored in my colony's warehouse.

### Acceptance Criteria

1. THE Warehousing tab SHALL display a DataGridView with columns: ItemType, Item (ExtendedName), Locked, Amount.
2. THE Colony_Form SHALL support adding items of type Resource, Commodity, WorkDetail, Survey, Blueprint, ShipPart, ShipHull, Munition, Flatpack, SpaceBuildPackage, and Share.
3. WHEN the user selects item type Resource, THE Colony_Form SHALL show a resource combo and a purity combo (defaulting to Refined). THE purity combo SHALL be hidden for synthetic resources.
4. WHEN the user selects item type Commodity, THE Colony_Form SHALL show a commodity combo displaying ExtendedName.
5. WHEN the user selects item type WorkDetail, THE Colony_Form SHALL show a combo with BlueCollarDetail, WhiteCollarDetail, SpecialistDetail options.
6. WHEN the user selects item type Survey, THE Colony_Form SHALL show a survey combo displaying ExtendedName.
7. WHEN the user selects item type Blueprint, THE Colony_Form SHALL show a blueprint combo displaying ExtendedName.
8. WHEN the user selects a blueprint-based output item type (ShipPart, ShipHull, Munition, Flatpack, SpaceBuildPackage, Share), THE Colony_Form SHALL show blueprints filtered by matching OutputItemType.
9. WHEN the user clicks Add, THE Colony_Form SHALL create an Item with the correct type, BaseItemTypeID, name, purity, quantity, and volume, then add it via ColonyViewModel.AddItem().
10. THE item volume SHALL be determined by type: Resource=1, Commodity=10, WorkDetail=50, Blueprint/Survey=0, manufactured items use "Cargo Volume Size" from the blueprint.
11. WHEN the user presses Delete on a selected item row, THE Colony_Form SHALL remove the item via ColonyViewModel.RemoveItem() unless the item has locked quantities.
12. IF the item has locked quantities, THEN THE Colony_Form SHALL display a warning message and prevent deletion.
13. THE Amount column SHALL be editable with integer validation. WHEN the user edits the Amount, THE Colony_Form SHALL update item.Quantity immediately and recalculate status.
14. THE Locked column SHALL display the locked quantity from Colony.Locks for each item.
15. EACH item type combo SHALL support text filtering via a filter text field.

## Requirement 17: Commodity Requests

**User Story:** As a player, I want to manage commodity requests that tell the delivery system what my colony needs.

### Acceptance Criteria

1. THE Workers tab SHALL display a DataGridView with columns: Name, Amount (Requested), Fulfilled (checkbox), NeedBy (countdown format).
2. WHEN the user selects a commodity from the filtered combo, enters a quantity and optional NeedBy countdown, and clicks Add, THE Colony_Form SHALL call ColonyViewModel.AddCommodityRequest() with the commodity name, quantity, and parsed NeedBy DateTime.
3. THE Amount cell SHALL be editable in-place. WHEN the user edits the Amount, THE Colony_Form SHALL immediately update the CommodityRequested.Requested field and call WriteContext().
4. THE Fulfilled cell SHALL be a checkbox. WHEN the user checks Fulfilled, THE Colony_Form SHALL set request.Fulfilled=true, request.Delivered=request.Requested, and refresh the grid with strikethrough styling.
5. THE NeedBy cell SHALL be editable in countdown format (e.g., "2d 6h 30m"). WHEN the user edits NeedBy, THE Colony_Form SHALL parse the countdown and update request.NeedBy.
6. WHEN the user presses Delete on a selected commodity request row, THE Colony_Form SHALL remove the request via ColonyViewModel.RemoveCommodityRequest().
7. THE Colony_Form SHALL auto-delete fulfilled commodity requests that are more than 3 days past their NeedBy date via ColonyViewModel.CleanupExpiredCommodityRequests().
8. THE commodity request grid SHALL use FullRowSelect mode. Clicking the Name column SHALL redirect focus to the Amount column.
9. THE Workers tab title SHALL display the count of active (unfulfilled, not expired) requests (e.g., "Workers : 3").
10. THE NeedBy column SHALL display "overdue" for requests past their due date.


## Requirement 18: Colony Import from Game Clipboard

**User Story:** As a player, I want to import colony data from the game's HTML clipboard so I can keep my tracker in sync with the game.

### Acceptance Criteria

1. WHEN the user clicks Import, THE Colony_Form SHALL validate that the clipboard contains HTML content.
2. IF the clipboard does not contain HTML, THEN THE Colony_Form SHALL display an informational message.
3. IF no player is selected, THEN THE Colony_Form SHALL display an informational message.
4. THE Colony_Form SHALL validate clipboard content type via ClipboardContentDetector.Detect(). IF the content is not Colony type, THEN THE Colony_Form SHALL display a message explaining what was found versus expected.
5. THE Colony_Form SHALL parse the clipboard into a temporary Colony via ColonyParser.ParseClipboardToTemp() without mutating any existing colony.
6. THE Colony_Form SHALL attempt to find an existing colony by planet name and system name via ColonyImportHelper.FindByPlanet().
7. IF an existing colony is found, THE Colony_Form SHALL merge identity fields, process the HTML into the existing colony, and preserve the existing ColonyName (the game may truncate it).
8. IF no existing colony is found, THE Colony_Form SHALL create a new colony via ColonyImportHelper.CreateFromTemp() with the current player's UUID as owner.
9. WHEN import completes, THE Colony_Form SHALL persist via WriteContext(), fire ColonyDataChanged, refresh the colony list, select the imported colony, and populate the form.
10. THE Colony_Form SHALL provide a "Save Clipboard" button that saves the raw clipboard HTML to a user-chosen file for debugging purposes.
11. IF an error occurs during import, THEN THE Colony_Form SHALL log the error and display an error message.

## Requirement 19: Build Order Optimization

**User Story:** As a player, I want to optimize the build order of my colony's structures to satisfy resource constraints at every build step.

### Acceptance Criteria

1. WHEN the user clicks Optimize, THE Colony_Form SHALL call BuildOrderOptimizer.Optimize() with the selected colony.
2. THE Colony_Form SHALL replace the colony's structure list with the optimized order.
3. THE Colony_Form SHALL refresh the structure display after optimization.
4. THE Build_Order_Optimizer service SHALL remain unchanged — the new form delegates to the existing service.

## Requirement 20: Colony Bootstrap

**User Story:** As a player, I want to automatically generate a foundation set of structures for a new colony based on available surveys.

### Acceptance Criteria

1. WHEN the user clicks Bootstrap, THE Colony_Form SHALL call ColonyBootstrap.Bootstrap() with the selected colony.
2. IF the colony has no PlanetName set, THEN THE Colony_Form SHALL display a warning message and prevent bootstrapping.
3. THE Colony_Form SHALL refresh the structure display after bootstrapping.
4. THE Colony_Bootstrap service SHALL remain unchanged — the new form delegates to the existing service.


## Requirement 21: Administration Tab

**User Story:** As a player, I want to see a status report for my colony and know when data is stale.

### Acceptance Criteria

1. THE Administration tab SHALL display a RichTextBox containing the colony admin report generated by ColonyAdminReportBuilder.BuildReport().
2. THE admin report SHALL refresh at the interval specified by PreferencesStore.Preferences.Thresholds.AdminRefreshIntervalSeconds (minimum 1 second) via a timer.
3. THE admin report SHALL also refresh when a colony is selected and when ColonyDataChanged fires.
4. THE Administration tab SHALL display Bootstrap and Optimize buttons.
5. THE Administration tab background color SHALL indicate import staleness: no color for less than 5 days, Yellow for 5-6 days, Red for 6+ days or never imported, as determined by Tab_Warning_Service.

## Requirement 22: Tab Warning Indicators

**User Story:** As a player, I want visual indicators on tabs to alert me to issues that need attention.

### Acceptance Criteria

1. THE Structures tab background color SHALL indicate warning level as determined by Tab_Warning_Service.EvaluateStructureWarning() based on structure count.
2. THE Workers tab background color SHALL indicate warning level as determined by Tab_Warning_Service.EvaluateWorkerWarning() based on unfulfilled commodity requests.
3. THE Administration tab background color SHALL indicate import staleness as determined by Tab_Warning_Service.EvaluateColonyImportStalenessWarning().
4. THE Colony_Form SHALL use owner-draw mode on the TabControl to render tab background colors with visual styles enabled.
5. THE tab warnings SHALL update after any structure change, commodity request change, or colony selection change.

## Requirement 23: Window State and Data Events

**User Story:** As a player, I want the colony form to remember its state and respond to external data changes.

### Acceptance Criteria

1. THE Colony_Form SHALL persist position, size, grid columns, ListView state, and structure type filter selections via WindowStateHelper on form close.
2. THE Colony_Form SHALL restore persisted state via WindowStateHelper on form load.
3. THE Colony_Form SHALL subscribe to CurrentPlayerChanged and ColonyDataChanged events in the constructor.
4. THE Colony_Form SHALL unsubscribe from all events in OnFormClosed.
5. ALL event handlers SHALL check IsDisposed before accessing controls.
6. ALL event handlers SHALL use BeginInvoke for cross-thread marshaling when InvokeRequired is true.
7. WHEN ColonyDataChanged fires for the currently selected colony (and the change was not triggered by this form), THE Colony_Form SHALL recalculate status, repopulate the form, and refresh the admin report.
8. WHEN CurrentPlayerChanged fires, THE Colony_Form SHALL clear all form state, create a new blank colony, and repopulate the colony list.
9. THE ProgrammaticUpdateGuard SHALL be used on all programmatic UI updates to prevent cascading event handlers.


## Requirement 24: Performance

**User Story:** As a player, I want the colony form to be responsive even with colonies that have many structures.

### Acceptance Criteria

#### Service-Level Fixes (shared improvements)

1. PlayerContext.FindBlueprint() and EmpireContext.FindGlobalBlueprint() SHALL each maintain a Dictionary<string, Blueprint> lookup cache indexed by UUID, replacing the current O(n) linear scans via FirstOrDefault. PlayerContext.FindBlueprint() SHALL check its local cache first, then delegate to EmpireContext's cached lookup for the global fallback. Each cache SHALL be invalidated when blueprints are added, removed, or have their UUID changed (e.g., during migration or import).
2. ColonyViewModel.StructureViewModels SHALL cache the list of ColonyStructureViewModel objects instead of creating new objects on every property access. The cache SHALL be invalidated when structures are added, removed, or reordered.
3. ColonyStatusCalculator.CalculateBuilt() SHALL cache the blueprint lookup results for the duration of a single calculation pass, avoiding repeated FindBlueprint calls for the same structure across multiple methods (LockAssignedWorkers, LockManufacturingResources, etc.).
4. ColonyStatusCalculator SHALL separate per-structure resource contributions (deltas) from colony-wide totals. Each structure SHALL compute its own power/habitation/food/entertainment/warehouse delta independently. The colony total SHALL be the sum of all structure deltas. WHEN a single structure changes (worker toggle, online/offline), only that structure's delta SHALL be recomputed and the colony total updated by subtracting the old delta and adding the new one — O(1) instead of O(n). A full recalculation SHALL only be needed when structures are added, removed, or reordered.
5. ItemBag SHALL maintain a secondary index keyed by (ItemType, BaseItemTypeID) for O(1) lookups via FindByType and CountByType, replacing the current O(n) LINQ scans over all items. FindResource SHALL use a compound key of (ItemType, BaseItemTypeID, Purity). The index SHALL be updated on AddItem and Remove.
6. THE Colony_Structure_Control SHALL accept a pre-resolved Blueprint reference from the caller (passed during UpdateData) instead of calling FindBlueprint independently, eliminating redundant lookups.

#### Form-Level Optimizations

5. THE Colony_Form SHALL use a Structure_Pool to reuse Colony_Structure_Controls instead of creating and disposing them on every colony switch or structural change.
6. THE Colony_Form SHALL use SuspendLayout/ResumeLayout around bulk control additions and removals.
7. THE Colony_Form SHALL defer tab content population using dirty flags — only the active tab is populated on colony selection; other tabs are populated when the user switches to them.
8. WHEN a non-structural change occurs (worker toggle, built/online state change), THE Colony_Form SHALL update only the affected Colony_Structure_Control and recalculate status without rebuilding all controls.
9. THE Colony_Structure_Control SHALL provide a lightweight UpdateBackgroundColor() method for worker checkbox changes that avoids a full UpdateData() repaint.
10. THE Colony_Form SHALL use in-place row updates for the item grid and commodity request grid instead of clearing and rebuilding all rows.
11. THE Colony_Form SHALL minimize layout cascade by avoiding unnecessary layout event handler calls during programmatic updates.

## Requirement 25: Parallel Development Strategy

**User Story:** As a developer, I want to build the new colony form alongside the old one so both remain functional during development.

### Acceptance Criteria

1. THE new form SHALL live in Forms/ColonyV2/ as FormColonyV2.cs and FormColonyV2.Designer.cs.
2. THE new Colony_Structure_Control SHALL live in Forms/ColonyV2/ as ColonyStructureV2.cs and ColonyStructureV2.Designer.cs.
3. THE MainWindow SHALL add a "Manage Colonies V2" menu item alongside the existing "Manage Colonies" item.
4. THE old FormColony and ColonyStructure SHALL remain unchanged and fully functional.
5. ALL existing services (Colony_ViewModel, Colony_Structure_ViewModel, Colony_Status_Calculator, Colony_Parser, Colony_Import_Helper, Build_Order_Optimizer, Colony_Bootstrap, Colony_Admin_Report_Builder, Tab_Warning_Service) SHALL remain unchanged — the new form delegates to them.
6. WHEN the V2 form passes acceptance testing, the old form SHALL be removed and the V2 menu item renamed.
