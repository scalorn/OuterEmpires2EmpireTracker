# Requirements Document

## Introduction

This spec covers adopting the FilteredTextComboSet custom control on all remaining ComboBoxes across the application that have large item lists but currently lack inline filtering. These are ComboBoxes identified during the custom-control-adoption spec audit that did not have a paired filter TextBox to replace. Instead, FilteredTextComboSet controls are added fresh to provide inline filtering where none existed before.

A key technical challenge is that FilteredTextComboSet currently only supports plain string lists. Several target ComboBoxes use BindingSource with DisplayMember/ValueMember to bind objects where the display text differs from the underlying value (typically a UUID). This spec addresses that gap by enhancing FilteredTextComboSet to support a parallel value list alongside the display string list.

This feature consolidates backlog items BL-102, BL-103, BL-104, BL-105, and BL-106.

## Glossary

- **FilteredTextComboSet**: A UserControl combining a filter TextBox and a ComboBox. When focused, a filter TextBox appears alongside the ComboBox. Typing filters items using case-insensitive contains-match. Currently supports plain string lists only.
- **Object_Binding**: The pattern of using BindingSource with DisplayMember/ValueMember on a ComboBox to display one property (e.g. Name) while storing another (e.g. UUID) as the selected value.
- **Parallel_Value_List**: A List of value identifiers (typically UUIDs) maintained in parallel with the display string list, where index N in the value list corresponds to index N in the display list.
- **Type_Cascade_Pattern**: A UI pattern where a type-selector ComboBox (e.g. cmbHoldType) controls which items appear in a dependent item ComboBox (e.g. cmbHoldItem). The type selector remains a standard ComboBox; only the item picker is converted to FilteredTextComboSet.
- **Designer_File**: The .Designer.cs file for a WinForms form, containing control declarations and initialization code.
- **IProgrammaticUpdateSource**: Interface implemented by all forms providing BeginProgrammaticUpdate/EndProgrammaticUpdate to suppress event handlers during programmatic data changes.
- **SelectedFullIndex**: Property on FilteredTextComboSet that returns the index of the selected item in the full (unfiltered) list, accounting for filter-induced index remapping.
- **FormStation**: Form for managing space stations, including blueprint assignment, cargo hold, and munitions.
- **FormShipInstance**: Form for managing individual ship instances, including cargo management.
- **FormSupplyChain**: Form for managing supply chain stages with location, resource, and route selection.
- **FormBuildPlanner**: Form for planning colony build orders, including item selection for manufacturing, commodity, mining, refining, and research stages.

## Requirements

### Requirement 1: Enhance FilteredTextComboSet to Support Parallel Value Lists

**User Story:** As a developer, I want FilteredTextComboSet to support a parallel value list alongside the display string list, so that ComboBoxes using DisplayMember/ValueMember object binding can be converted without losing the ability to retrieve the underlying value (UUID).

#### Acceptance Criteria

1. THE FilteredTextComboSet SHALL expose a SetItems overload that accepts both a display list and a parallel value list, in addition to the existing SetItems method that takes only a display list and currentValue.
2. WHEN a parallel value list is provided, THE FilteredTextComboSet SHALL maintain the value list in sync with the display list such that SelectedValue returns the value at the same position as the selected display item.
3. THE FilteredTextComboSet SHALL expose a SelectedValue property that returns the value string corresponding to the currently selected item, or null if nothing is selected or no value list was provided.
4. WHEN the filter is applied and items are removed from the visible list, THE FilteredTextComboSet SHALL maintain correct index mapping so that SelectedValue returns the value from the original unfiltered value list at the correct position.
5. WHEN SetItems is called with only a display list (no value list), THE FilteredTextComboSet SHALL behave identically to the current implementation with no value list stored.
6. THE FilteredTextComboSet SHALL expose a SetItems overload that accepts a display list, a value list, and a currentValue string, where currentValue is matched against the value list to pre-select the corresponding item.

### Requirement 2: FormStation - cmbStationBlueprint FilteredTextComboSet Adoption

**User Story:** As a user managing stations, I want the station blueprint picker to use inline filtering, so that I can quickly find a blueprint in the 50+ item list without scrolling.

#### Acceptance Criteria

1. THE Designer_File for FormStation SHALL declare cmbStationBlueprint as type OE2EmpireTracker.Controls.FilteredTextComboSet instead of System.Windows.Forms.ComboBox.
2. WHEN the FormStation populates the station blueprint picker, THE FormStation SHALL call FilteredTextComboSet.SetItems with a display list of blueprint ExtendedNames and a parallel value list of blueprint UUIDs.
3. WHEN the user types in the filter area of cmbStationBlueprint, THE FilteredTextComboSet SHALL filter the blueprint list using case-insensitive contains-match on the display names.
4. WHEN the user selects a blueprint from the filtered list, THE FormStation SHALL read the SelectedValue property to obtain the blueprint UUID and write it to the ViewModel.
5. WHEN the FormStation loads an existing station, THE FormStation SHALL pre-select the current blueprint by passing the station's blueprint UUID as the currentValue parameter to SetItems.
6. IF the station blueprint list is empty, THEN THE FilteredTextComboSet SHALL display an empty combo with no selection.

### Requirement 3: FormStation - cmbHoldItem FilteredTextComboSet Adoption

**User Story:** As a user managing station cargo holds, I want the hold item picker to use inline filtering, so that I can quickly find a resource or commodity in the 50+ item list.

#### Acceptance Criteria

1. THE Designer_File for FormStation SHALL declare cmbHoldItem as type OE2EmpireTracker.Controls.FilteredTextComboSet instead of System.Windows.Forms.ComboBox.
2. WHEN cmbHoldType selection changes, THE FormStation SHALL repopulate cmbHoldItem by calling FilteredTextComboSet.SetItems with the appropriate item names for the selected type.
3. WHEN the user types in the filter area of cmbHoldItem, THE FilteredTextComboSet SHALL filter the item list using case-insensitive contains-match.
4. WHEN the user selects an item from the filtered list, THE FormStation SHALL read the SelectedItem property to obtain the item name for the add-to-hold operation.
5. THE type-cascade behavior SHALL be preserved: cmbHoldType remains a standard ComboBox controlling which items appear in cmbHoldItem.

### Requirement 4: FormStation - cmbMunItem FilteredTextComboSet Adoption

**User Story:** As a user managing station munitions, I want the munition item picker to use inline filtering, so that I can quickly find a munition type.

#### Acceptance Criteria

1. THE Designer_File for FormStation SHALL declare cmbMunItem as type OE2EmpireTracker.Controls.FilteredTextComboSet instead of System.Windows.Forms.ComboBox.
2. WHEN the FormStation populates the munition item picker, THE FormStation SHALL call FilteredTextComboSet.SetItems with the munition item names.
3. WHEN the user types in the filter area of cmbMunItem, THE FilteredTextComboSet SHALL filter the munition list using case-insensitive contains-match.
4. WHEN the user selects a munition from the filtered list, THE FormStation SHALL read the SelectedItem property to obtain the munition name for the add operation.

### Requirement 5: FormShipInstance - cmbAddItem FilteredTextComboSet Adoption

**User Story:** As a user managing ship cargo, I want the add-item picker to use inline filtering, so that I can quickly find a resource or commodity in the 50+ item list.

#### Acceptance Criteria

1. THE Designer_File for FormShipInstance SHALL declare cmbAddItem as type OE2EmpireTracker.Controls.FilteredTextComboSet instead of System.Windows.Forms.ComboBox.
2. WHEN cmbAddType selection changes, THE FormShipInstance SHALL repopulate cmbAddItem by calling FilteredTextComboSet.SetItems with the appropriate item names for the selected type.
3. WHEN the user types in the filter area of cmbAddItem, THE FilteredTextComboSet SHALL filter the item list using case-insensitive contains-match.
4. WHEN the user selects an item from the filtered list, THE FormShipInstance SHALL read the SelectedItem property to obtain the item name for the add-to-cargo operation.
5. THE type-cascade behavior SHALL be preserved: cmbAddType remains a standard ComboBox controlling which items appear in cmbAddItem.

### Requirement 6: FormSupplyChain - cmbLocation FilteredTextComboSet Adoption

**User Story:** As a user configuring supply chain stages, I want the location picker to use inline filtering, so that I can quickly find a colony, station, asteroid, or ship in the list.

#### Acceptance Criteria

1. THE Designer_File for FormSupplyChain SHALL declare cmbLocation as type OE2EmpireTracker.Controls.FilteredTextComboSet instead of System.Windows.Forms.ComboBox.
2. WHEN cmbLocationType selection changes, THE FormSupplyChain SHALL repopulate cmbLocation by calling FilteredTextComboSet.SetItems with a display list of location names and a parallel value list of location UUIDs.
3. WHEN the user types in the filter area of cmbLocation, THE FilteredTextComboSet SHALL filter the location list using case-insensitive contains-match on the display names.
4. WHEN the user selects a location from the filtered list, THE FormSupplyChain SHALL read the SelectedValue property to obtain the location UUID for the stage configuration.
5. WHEN the FormSupplyChain loads an existing stage, THE FormSupplyChain SHALL pre-select the current location by passing the stage's LocationUUID as the currentValue parameter.
6. THE type-cascade behavior SHALL be preserved: cmbLocationType remains a standard ComboBox controlling which locations appear in cmbLocation.

### Requirement 7: FormSupplyChain - cmbResource FilteredTextComboSet Adoption

**User Story:** As a user configuring supply chain stages, I want the resource picker to use inline filtering, so that I can quickly find a resource in the 50+ item list.

#### Acceptance Criteria

1. THE Designer_File for FormSupplyChain SHALL declare cmbResource as type OE2EmpireTracker.Controls.FilteredTextComboSet instead of System.Windows.Forms.ComboBox.
2. WHEN the FormSupplyChain populates the resource picker, THE FormSupplyChain SHALL call FilteredTextComboSet.SetItems with the resource names.
3. WHEN the user types in the filter area of cmbResource, THE FilteredTextComboSet SHALL filter the resource list using case-insensitive contains-match.
4. WHEN the user selects a resource from the filtered list, THE FormSupplyChain SHALL read the SelectedItem property to obtain the resource name for the stage configuration.
5. WHEN the FormSupplyChain loads an existing stage, THE FormSupplyChain SHALL pre-select the current resource by matching the stage's resource name in the item list.

### Requirement 8: FormSupplyChain - cmbRoute FilteredTextComboSet Adoption

**User Story:** As a user configuring supply chain stages, I want the route picker to use inline filtering, so that I can quickly find a delivery route.

#### Acceptance Criteria

1. THE Designer_File for FormSupplyChain SHALL declare cmbRoute as type OE2EmpireTracker.Controls.FilteredTextComboSet instead of System.Windows.Forms.ComboBox.
2. WHEN the FormSupplyChain populates the route picker, THE FormSupplyChain SHALL call FilteredTextComboSet.SetItems with a display list of route names and a parallel value list of route UUIDs.
3. WHEN the user types in the filter area of cmbRoute, THE FilteredTextComboSet SHALL filter the route list using case-insensitive contains-match on the display names.
4. WHEN the user selects a route from the filtered list, THE FormSupplyChain SHALL read the SelectedValue property to obtain the route UUID for the stage configuration.
5. WHEN the FormSupplyChain loads an existing stage, THE FormSupplyChain SHALL pre-select the current route by passing the stage's route UUID as the currentValue parameter.

### Requirement 9: FormBuildPlanner - cmbResource FilteredTextComboSet Adoption

**User Story:** As a user planning builds, I want the resource picker in the mining/refining configuration to use inline filtering, so that I can quickly find a resource in the 50+ item list.

#### Acceptance Criteria

1. THE Designer_File for FormBuildPlanner SHALL declare cmbResource as type OE2EmpireTracker.Controls.FilteredTextComboSet instead of System.Windows.Forms.ComboBox.
2. WHEN the FormBuildPlanner populates the resource picker, THE FormBuildPlanner SHALL call FilteredTextComboSet.SetItems with the resource names.
3. WHEN the user types in the filter area of cmbResource, THE FilteredTextComboSet SHALL filter the resource list using case-insensitive contains-match.
4. WHEN the user selects a resource from the filtered list, THE FormBuildPlanner SHALL read the SelectedItem property to obtain the resource name.

### Requirement 10: FormBuildPlanner - cmbSurvey FilteredTextComboSet Adoption

**User Story:** As a user planning builds, I want the survey picker to use inline filtering, so that I can quickly find a survey when multiple surveys exist.

#### Acceptance Criteria

1. THE Designer_File for FormBuildPlanner SHALL declare cmbSurvey as type OE2EmpireTracker.Controls.FilteredTextComboSet instead of System.Windows.Forms.ComboBox.
2. WHEN the FormBuildPlanner populates the survey picker, THE FormBuildPlanner SHALL call FilteredTextComboSet.SetItems with a display list of survey names and a parallel value list of survey UUIDs.
3. WHEN the user types in the filter area of cmbSurvey, THE FilteredTextComboSet SHALL filter the survey list using case-insensitive contains-match on the display names.
4. WHEN the user selects a survey from the filtered list, THE FormBuildPlanner SHALL read the SelectedValue property to obtain the survey UUID.
5. WHEN the FormBuildPlanner loads existing configuration, THE FormBuildPlanner SHALL pre-select the current survey by passing the survey UUID as the currentValue parameter.

### Requirement 11: Preserve Type-Cascade Behavior

**User Story:** As a user, I want the type-selector ComboBoxes to continue controlling which items appear in the dependent FilteredTextComboSet pickers, so that the cascading selection pattern works as before.

#### Acceptance Criteria

1. WHEN a type-selector ComboBox selection changes (cmbHoldType, cmbAddType, cmbLocationType, cmbItemType), THE form SHALL call SetItems on the dependent FilteredTextComboSet with the new item list appropriate for the selected type.
2. WHEN SetItems is called on a FilteredTextComboSet that is not in edit mode, THE FilteredTextComboSet SHALL clear any previous filter text and display the full new item list.
3. WHEN SetItems is called on a FilteredTextComboSet that is in edit mode, THE FilteredTextComboSet SHALL preserve the current filter text and reapply it to the new item list.
4. THE type-selector ComboBoxes SHALL remain as standard System.Windows.Forms.ComboBox controls and SHALL NOT be converted to FilteredTextComboSet, because their item lists are small (5-6 items).

### Requirement 12: Preserve IProgrammaticUpdateSource Guards

**User Story:** As a developer, I want all _isProgrammaticUpdate guards preserved during the control adoption, so that programmatic data changes do not trigger unintended event handler side effects.

#### Acceptance Criteria

1. WHEN a FilteredTextComboSet.SelectedItemChanged event handler is added, THE event handler SHALL check _isProgrammaticUpdate and return early if a programmatic update is in progress.
2. WHEN SetItems is called during a programmatic update (e.g. loading a record), THE form SHALL use ProgrammaticUpdateGuard or BeginProgrammaticUpdate/EndProgrammaticUpdate to suppress the SelectedItemChanged event.
3. THE existing write-through pattern SHALL be preserved: selecting an item in the FilteredTextComboSet writes the value to the ViewModel immediately.

### Requirement 13: PERF Timing on Populate Methods

**User Story:** As a developer, I want all populate methods that call SetItems to have PERF timing instrumentation, so that performance regressions are detectable.

#### Acceptance Criteria

1. WHEN a populate method calls FilteredTextComboSet.SetItems, THE method SHALL include Stopwatch timing and log the elapsed time using the PERF prefix pattern.
2. THE existing PERF timing on PopulateStationBlueprintCombo, PopulateHoldItemCombo, PopulateAddItemCombo, PopulateLocationCombo, PopulateResourceCombo, and PopulateRouteCombo SHALL be preserved after conversion.

### Requirement 14: Update Mockups for Converted Forms

**User Story:** As a developer, I want the mockups in spec/mockups/ updated to reflect the FilteredTextComboSet adoption, so that documentation stays in sync with the code.

#### Acceptance Criteria

1. WHEN a standard ComboBox is replaced by a FilteredTextComboSet, THE corresponding mockup SHALL update the control type notation to indicate FilteredTextComboSet.
2. WHEN a ComboBox that previously used BindingSource/DataSource is converted, THE mockup SHALL note that the control uses the parallel value list pattern.
3. THE mockup control lists SHALL reflect the new control types for all converted ComboBoxes across FormStation, FormShipInstance, FormSupplyChain, and FormBuildPlanner.
