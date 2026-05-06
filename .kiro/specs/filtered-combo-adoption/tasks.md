# Implementation Plan: Filtered Combo Adoption

## Overview

Enhance FilteredTextComboSet with parallel value list support, then convert 9 ComboBoxes across 4 forms to use the enhanced control. The control enhancement must be completed first since all form conversions depend on it. Property-based tests validate the 5 correctness properties immediately after the enhancement.

## Tasks

- [x] 1. Enhance FilteredTextComboSet with parallel value list support
  - [x] 1.1 Add _fullValues field and SelectedValue property
    - Add private List<string> _fullValues = null field
    - Add public SelectedValue property that maps through _filteredIndexMap to return the value at the selected item's original index
    - Returns null when no value list is provided or nothing is selected
    - _Requirements: 1.2, 1.3, 1.4, 1.5_
  - [x] 1.2 Add SetItems(items, values, currentValue) overload
    - Implement the new overload that stores the parallel value list
    - Match currentValue against the value list to pre-select the corresponding display item
    - When not editing: clear filter text, rebuild filtered list, select by value
    - When editing: preserve filter text, reapply filter, restore selection by value
    - _Requirements: 1.1, 1.6, 11.2, 11.3_
  - [x] 1.3 Modify existing SetItems(items, currentValue) for backward compatibility
    - Add _fullValues = null at the start to clear any previous value list
    - Ensures plain-string mode when no value list is provided
    - _Requirements: 1.5_

- [x] 2. Property-based tests for FilteredTextComboSet value list support
  - [x] 2.1 Write property test for value list index mapping through filter
    - **Property 1: Value list index mapping through filter**
    - For any display list, parallel value list of equal length, and any filter string, selecting a filtered item returns the correct value from the original value list
    - When no value list is provided, SelectedValue is always null
    - **Validates: Requirements 1.2, 1.3, 1.4, 1.5**
  - [x] 2.2 Write property test for currentValue pre-selection via value list
    - **Property 2: currentValue pre-selection via value list**
    - For any display list, parallel value list, and currentValue that exists in the value list, SetItems selects the display item at the matching value's index
    - If currentValue does not exist, no item is selected
    - **Validates: Requirements 1.6, 2.5, 6.5, 8.5, 10.5**
  - [x] 2.3 Write property test for SetItems clearing filter when not editing
    - **Property 3: SetItems clears filter when not editing**
    - For any previous state and new item list, calling SetItems when not editing results in empty filter text and all items displayed
    - **Validates: Requirements 11.2**
  - [x] 2.4 Write property test for SetItems preserving filter when editing
    - **Property 4: SetItems preserves filter when editing**
    - For any filter text and new item list, calling SetItems while editing preserves the filter and displays only matching items
    - **Validates: Requirements 11.3**
  - [x] 2.5 Write property test for event suppression during programmatic updates
    - **Property 5: Event suppression during programmatic updates**
    - For any item list, calling SetItems while SuppressSelectionEvent is true does not fire SelectedItemChanged
    - **Validates: Requirements 12.2**

- [x] 3. Checkpoint - Control enhancement verified
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. FormStation - Convert cmbStationBlueprint to FilteredTextComboSet
  - [x] 4.1 Update FormStation Designer for cmbStationBlueprint
    - Change declaration from System.Windows.Forms.ComboBox to OE2EmpireTracker.Controls.FilteredTextComboSet
    - Change instantiation to `new OE2EmpireTracker.Controls.FilteredTextComboSet()`
    - Remove DropDownStyle assignment
    - Replace SelectedIndexChanged event wiring with SelectedItemChanged
    - _Requirements: 2.1_
  - [x] 4.2 Update FormStation code-behind for cmbStationBlueprint
    - Replace BindingSource/DataSource pattern with SetItems(displayNames, valueUUIDs, currentValue)
    - Build display list from blueprint ExtendedNames and value list from blueprint UUIDs
    - Replace SelectedValue?.ToString() reads with SelectedValue
    - Update event handler to use SelectedItemChanged with _isProgrammaticUpdate guard
    - Pre-select current blueprint UUID when loading existing station
    - Preserve PERF timing on PopulateStationBlueprintCombo
    - _Requirements: 2.2, 2.3, 2.4, 2.5, 2.6, 12.1, 12.2, 13.1_

- [x] 5. FormStation - Convert cmbHoldItem and cmbMunItem to FilteredTextComboSet
  - [x] 5.1 Update FormStation Designer for cmbHoldItem and cmbMunItem
    - Change declarations from System.Windows.Forms.ComboBox to OE2EmpireTracker.Controls.FilteredTextComboSet
    - Change instantiations to `new OE2EmpireTracker.Controls.FilteredTextComboSet()`
    - Remove DropDownStyle assignments
    - Replace SelectedIndexChanged event wirings with SelectedItemChanged
    - _Requirements: 3.1, 4.1_
  - [x] 5.2 Update FormStation code-behind for cmbHoldItem
    - Replace Items.Add pattern with SetItems(names, string.Empty) in PopulateHoldItemCombo
    - Preserve type-cascade: cmbHoldType SelectedIndexChanged calls PopulateHoldItemCombo
    - Read SelectedItem property for item name
    - Update event handler with _isProgrammaticUpdate guard
    - Preserve PERF timing on PopulateHoldItemCombo
    - _Requirements: 3.2, 3.3, 3.4, 3.5, 11.1, 12.1, 13.1_
  - [x] 5.3 Update FormStation code-behind for cmbMunItem
    - Replace Items.Add pattern with SetItems(names, string.Empty) in populate method
    - Read SelectedItem property for munition name
    - Update event handler with _isProgrammaticUpdate guard
    - _Requirements: 4.2, 4.3, 4.4, 12.1_

- [x] 6. Checkpoint - FormStation conversions verified
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. FormShipInstance - Convert cmbAddItem to FilteredTextComboSet
  - [x] 7.1 Update FormShipInstance Designer for cmbAddItem
    - Change declaration from System.Windows.Forms.ComboBox to OE2EmpireTracker.Controls.FilteredTextComboSet
    - Change instantiation to `new OE2EmpireTracker.Controls.FilteredTextComboSet()`
    - Remove DropDownStyle assignment
    - Replace SelectedIndexChanged event wiring with SelectedItemChanged
    - _Requirements: 5.1_
  - [x] 7.2 Update FormShipInstance code-behind for cmbAddItem
    - Replace Items.Add pattern with SetItems(names, string.Empty) in PopulateAddItemCombo
    - Preserve type-cascade: cmbAddType SelectedIndexChanged calls PopulateAddItemCombo
    - Read SelectedItem property for item name
    - Update event handler with _isProgrammaticUpdate guard
    - Preserve PERF timing on PopulateAddItemCombo
    - _Requirements: 5.2, 5.3, 5.4, 5.5, 11.1, 12.1, 13.1_

- [x] 8. FormSupplyChain - Convert cmbLocation, cmbResource, and cmbRoute to FilteredTextComboSet
  - [x] 8.1 Update FormSupplyChain Designer for cmbLocation, cmbResource, and cmbRoute
    - Change declarations from System.Windows.Forms.ComboBox to OE2EmpireTracker.Controls.FilteredTextComboSet
    - Change instantiations to `new OE2EmpireTracker.Controls.FilteredTextComboSet()`
    - Remove DropDownStyle assignments
    - Replace SelectedIndexChanged event wirings with SelectedItemChanged
    - _Requirements: 6.1, 7.1, 8.1_
  - [x] 8.2 Update FormSupplyChain code-behind for cmbLocation
    - Replace BindingSource/DataSource pattern with SetItems(displayNames, valueUUIDs, currentValue)
    - Build display list from location names and value list from location UUIDs
    - Preserve type-cascade: cmbLocationType SelectedIndexChanged calls PopulateLocationCombo
    - Read SelectedValue for location UUID
    - Pre-select current location UUID when loading existing stage
    - Update event handler with _isProgrammaticUpdate guard
    - Preserve PERF timing on PopulateLocationCombo
    - _Requirements: 6.2, 6.3, 6.4, 6.5, 6.6, 11.1, 12.1, 13.1_
  - [x] 8.3 Update FormSupplyChain code-behind for cmbResource
    - Replace Items.Add pattern with SetItems(names, currentValue) in PopulateResourceCombo
    - Read SelectedItem for resource name
    - Pre-select current resource when loading existing stage
    - Update event handler with _isProgrammaticUpdate guard
    - Preserve PERF timing on PopulateResourceCombo
    - _Requirements: 7.2, 7.3, 7.4, 7.5, 12.1, 13.1_
  - [x] 8.4 Update FormSupplyChain code-behind for cmbRoute
    - Replace BindingSource/DataSource pattern with SetItems(displayNames, valueUUIDs, currentValue)
    - Build display list from route names and value list from route UUIDs
    - Read SelectedValue for route UUID
    - Pre-select current route UUID when loading existing stage
    - Update event handler with _isProgrammaticUpdate guard
    - Preserve PERF timing on PopulateRouteCombo
    - _Requirements: 8.2, 8.3, 8.4, 8.5, 12.1, 13.1_

- [x] 9. Checkpoint - FormShipInstance and FormSupplyChain conversions verified
  - Ensure all tests pass, ask the user if questions arise.

- [x] 10. FormBuildPlanner - Convert cmbResource and cmbSurvey to FilteredTextComboSet
  - [x] 10.1 Update FormBuildPlanner Designer for cmbResource and cmbSurvey
    - Change declarations from System.Windows.Forms.ComboBox to OE2EmpireTracker.Controls.FilteredTextComboSet
    - Change instantiations to `new OE2EmpireTracker.Controls.FilteredTextComboSet()`
    - Remove DropDownStyle assignments
    - Replace SelectedIndexChanged event wirings with SelectedItemChanged
    - _Requirements: 9.1, 10.1_
  - [x] 10.2 Update FormBuildPlanner code-behind for cmbResource
    - Replace Items.Add pattern with SetItems(names, string.Empty) in PopulateResourceCombo
    - Read SelectedItem for resource name
    - Update event handler with _isProgrammaticUpdate guard
    - _Requirements: 9.2, 9.3, 9.4, 12.1_
  - [x] 10.3 Update FormBuildPlanner code-behind for cmbSurvey
    - Replace ItemEntry objects pattern with SetItems(displayNames, valueUUIDs, currentValue)
    - Build display list with "(none)" sentinel and survey names
    - Build value list with empty string sentinel and survey UUIDs
    - Read SelectedValue for survey UUID
    - Pre-select current survey UUID when loading existing configuration
    - Update event handler with _isProgrammaticUpdate guard
    - _Requirements: 10.2, 10.3, 10.4, 10.5, 12.1_
  - [x] 10.4 Remove _itemPickerIDs field from FormBuildPlanner
    - Delete the _itemPickerIDs field declaration
    - Replace all _itemPickerIDs population code with SetItems(names, ids, currentValue) calls
    - Replace all _itemPickerIDs[SelectedFullIndex] reads with SelectedValue
    - Verify no remaining references to _itemPickerIDs
    - _Requirements: 9.2, 10.2_

- [x] 11. Checkpoint - FormBuildPlanner conversions verified
  - Ensure all tests pass, ask the user if questions arise.

- [x] 12. Update mockups for converted forms
  - [x] 12.1 Update FormStation mockup
    - Change cmbStationBlueprint, cmbHoldItem, cmbMunItem control types to FilteredTextComboSet
    - Note parallel value list pattern on cmbStationBlueprint
    - _Requirements: 14.1, 14.2, 14.3_
  - [x] 12.2 Update FormShipInstance mockup
    - Change cmbAddItem control type to FilteredTextComboSet
    - _Requirements: 14.1, 14.3_
  - [x] 12.3 Update FormSupplyChain mockup
    - Change cmbLocation, cmbResource, cmbRoute control types to FilteredTextComboSet
    - Note parallel value list pattern on cmbLocation and cmbRoute
    - _Requirements: 14.1, 14.2, 14.3_
  - [x] 12.4 Update FormBuildPlanner mockup
    - Change cmbResource, cmbSurvey control types to FilteredTextComboSet
    - Note parallel value list pattern on cmbSurvey
    - _Requirements: 14.1, 14.2, 14.3_

- [x] 13. Final checkpoint - All conversions and mockups verified
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with * are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation after each logical group
- Property tests validate universal correctness properties for the control enhancement
- Form conversions are verified by compilation (getDiagnostics) and audit tools
- The control enhancement (tasks 1-3) MUST be completed before any form conversion
- Type-selector ComboBoxes (cmbHoldType, cmbAddType, cmbLocationType, cmbItemType) remain as standard ComboBox controls