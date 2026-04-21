# Requirements Document

## Introduction

Overhaul FormShipInstance to align its layout, controls, and interaction patterns with FormShipTemplate. The current ship form has buttons in the left search panel, no hull picker for manual ship creation, and a plain text component column. This spec brings the ship form in line with the template form by relocating buttons to the detail panel, adding a hull FilteredTextComboSet for manual creation, and upgrading the component grid to use DataGridViewFilteredComboBoxColumn with Condition and MaxRepair columns.

## Glossary

- **Ship_Form**: The `FormShipInstance` form that manages ship instances (OE2EmpireTracker/Forms/ShipInstance/).
- **Template_Form**: The `FormShipTemplate` form used as the reference layout (OE2EmpireTracker/Forms/ShipTemplate/).
- **Detail_Panel**: The right-side `flpDetail` FlowLayoutPanel that displays ship name, location, hull, and the tab control.
- **Command_Panel**: The `flpCommands` FlowLayoutPanel containing the action buttons (New, Save, Delete, Create From Template).
- **Search_Panel**: The left-side `flpSearchList` FlowLayoutPanel containing the filter text box and ship list.
- **Hull_Combo**: A `FilteredTextComboSet` control for selecting a hull blueprint, with inline text filtering and a parallel UUID list.
- **Component_Grid**: The `dgvComponents` DataGridView on the Overview tab that displays installed components.
- **Component_Column**: The `colComponent` column in the Component_Grid that uses `DataGridViewFilteredComboBoxColumn` for inline filtered component selection.
- **Condition_Column**: The `colCondition` editable column showing the component's current HP value.
- **MaxRepair_Column**: The `colMaxRepair` editable column showing the component's maximum repairable percentage.
- **Stats_Box**: The `rtbStats` RichTextBox that displays computed ship statistics.
- **Slot_Info**: A per-row Tag object storing SlotType, SlotIndex, and a parallel UUID list for resolving component selections.

## Requirements

### Requirement 1: Relocate Buttons to Detail Panel

**User Story:** As a user, I want the New, Save, Delete, and Create From Template buttons below the stats box in the detail panel, so that the ship form layout matches the template form.

#### Acceptance Criteria

1. THE Ship_Form SHALL display the Command_Panel inside the Detail_Panel, positioned after the Stats_Box (below the tab control).
2. THE Ship_Form SHALL remove the Command_Panel from the Search_Panel.
3. THE Command_Panel SHALL contain buttons in the order: New, Save, Delete, Create From Template.
4. THE Search_Panel SHALL contain only the filter text box and the ship list view.

### Requirement 2: Add Hull FilteredTextComboSet

**User Story:** As a user, I want a hull picker combo below the ship name field, so that I can manually select a hull when creating a ship without going through the template picker.

#### Acceptance Criteria

1. THE Ship_Form SHALL display a Hull_Combo (`FilteredTextComboSet`) below the name field and above the location row, matching the Template_Form's hull combo placement.
2. THE Hull_Combo SHALL be populated with all hull blueprint `ExtendedName` strings, with a parallel UUID list for resolution.
3. WHEN the user selects a hull in the Hull_Combo, THE Ship_Form SHALL update the selected ship's `HullBlueprintUUID` to the corresponding UUID.
4. WHEN a hull is selected, THE Ship_Form SHALL rebuild the Component_Grid slot rows based on the hull's slot definitions and refresh the Stats_Box.
5. WHEN a ship is selected from the list, THE Ship_Form SHALL set the Hull_Combo to display the ship's current hull blueprint name.
6. WHEN no ship is selected, THE Hull_Combo SHALL be disabled.

### Requirement 3: Manual Ship Creation via New Button

**User Story:** As a user, I want the New button to create a blank ship that I can configure manually by picking a hull, so that I am not forced to use a template.

#### Acceptance Criteria

1. WHEN the user clicks New, THE Ship_Form SHALL create a new Ship with a generated UUID, a default name of "New Ship", and the current player as owner.
2. WHEN the user clicks New, THE Ship_Form SHALL add the new ship to the player context, persist the data, select the new ship in the list, and populate the form for editing.
3. THE Ship_Form SHALL allow the user to select a hull via the Hull_Combo after creating a new ship manually.
4. THE Ship_Form SHALL retain the Create From Template button as a separate action that opens the template picker dialog.

### Requirement 4: Upgrade Component Grid to Filtered Combo Column

**User Story:** As a user, I want the component column in the ship's overview grid to use the same filtered combo box as the template form, so that I can search and pick components inline.

#### Acceptance Criteria

1. THE Component_Grid SHALL use a `DataGridViewFilteredComboBoxColumn` for the component column (`colComponent`), replacing the current read-only text column.
2. THE Component_Grid SHALL include a Slot Type column (read-only), a Slot Index column (read-only), the Component_Column (editable filtered combo), the Condition_Column (editable), and the MaxRepair_Column (editable).
3. WHEN the Component_Grid is populated, THE Ship_Form SHALL set each component cell's item list to the eligible blueprint `ExtendedName` strings for that slot type and hull class, plus an "(empty)" entry as the first item.
4. WHEN the user selects a component via the filtered combo, THE Ship_Form SHALL resolve the selected display name to a UUID using the parallel UUID list on the row's Slot_Info Tag.
5. WHEN the user selects a component, THE Ship_Form SHALL update the ship's component slot and refresh the Stats_Box.
6. WHEN the user selects "(empty)", THE Ship_Form SHALL remove the component from that slot.
7. THE Component_Grid SHALL include a hull row as the first row with the hull name displayed in the Component_Column (read-only, not editable via the combo).

### Requirement 5: Condition and MaxRepair Columns

**User Story:** As a user, I want to edit the Condition and MaxRepair values for each component directly in the grid, so that I can track component wear on my ships.

#### Acceptance Criteria

1. THE Condition_Column SHALL display the component's `CurrentHP` value and be editable for all rows including the hull row.
2. THE MaxRepair_Column SHALL display the component's `MaxRepairPercent` value and be editable for all rows including the hull row.
3. WHEN the user edits a Condition_Column cell, THE Ship_Form SHALL update the corresponding component's `CurrentHP` (or `HullCurrentHP` for the hull row).
4. WHEN the user edits a MaxRepair_Column cell, THE Ship_Form SHALL update the corresponding component's `MaxRepairPercent` (or `HullMaxRepairPercent` for the hull row).

### Requirement 6: Slot Index Column

**User Story:** As a user, I want to see the slot index for each component row, so that I can distinguish between multiple slots of the same type.

#### Acceptance Criteria

1. THE Component_Grid SHALL include a Slot Index column (`colSlotIndex`) between the Slot Type column and the Component_Column.
2. THE Slot Index column SHALL be read-only and display the zero-based slot index for each component row.
3. THE hull row SHALL display an empty string in the Slot Index column.

### Requirement 7: Remove Swap Component Button

**User Story:** As a user, I want to select components directly in the grid via the filtered combo, so that the separate Swap Component button is no longer needed.

#### Acceptance Criteria

1. THE Ship_Form SHALL remove the `cmdSwapComponent` button from the Overview tab.
2. THE Ship_Form SHALL allow component selection exclusively through the Component_Column's inline filtered combo.

### Requirement 8: Grid Data Error Handling

**User Story:** As a developer, I want the component grid to handle data errors gracefully, so that invalid combo states do not crash the form.

#### Acceptance Criteria

1. THE Component_Grid SHALL have a `DataError` event handler that logs the error and sets `ThrowException` to false.
2. THE Component_Grid SHALL use `EditMode` of `EditOnEnter` so that clicking a component cell immediately opens the filtered combo editor.
3. THE Component_Grid SHALL handle `CurrentCellDirtyStateChanged` by committing the edit, matching the Template_Form's pattern.

### Requirement 9: Layout Consistency with Template Form

**User Story:** As a user, I want the ship form's detail panel layout to follow the same top-to-bottom flow as the template form, so that both forms feel consistent.

#### Acceptance Criteria

1. THE Detail_Panel SHALL arrange controls in top-down order: Name row, Hull row, Location row, Tab Control, Command_Panel.
2. THE Detail_Panel layout handler SHALL size the Tab Control to fill available vertical space after accounting for the Name row, Hull row, Location row, and Command_Panel heights.
3. THE Search_Panel layout handler SHALL size the ship list to fill available vertical space after accounting for the filter row height only (no command panel).
