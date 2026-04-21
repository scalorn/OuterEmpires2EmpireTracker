# Requirements Document

## Introduction

A reusable DataGridView column control that embeds a filtered combo box (TextBox + ComboBox pair) as an inline cell editor. The filter matches any substring of the item text (contains-match), not just prefix. Items are plain strings with UUIDs tracked in a parallel list on the row Tag, following the project's established combo cell pattern. The first consumer is FormShipTemplate's component slot grid, replacing the current plain DataGridViewComboBoxCell that has no filtering.

## Glossary

- **Editing_Control**: The UserControl that implements `IDataGridViewEditingControl` and hosts the filter TextBox and ComboBox. Displayed inline in the grid cell when the user begins editing.
- **Column**: The `DataGridViewColumn` subclass (`DataGridViewFilteredComboBoxColumn`) that owns the cell template and column-level configuration.
- **Cell**: The `DataGridViewCell` subclass (`DataGridViewFilteredComboBoxCell`) that stores the per-cell value and launches the Editing_Control.
- **Filter_TextBox**: The TextBox portion of the Editing_Control where the user types a filter string.
- **Result_ComboBox**: The ComboBox portion of the Editing_Control that displays the filtered list of items.
- **Full_Item_List**: The complete, unfiltered list of plain string items available for selection in a given cell.
- **Filtered_Item_List**: The subset of Full_Item_List whose entries contain the current filter text (case-insensitive substring match).
- **UUID_List**: A parallel `List<string>` of UUIDs stored on the row's Tag, indexed to match the combo items. UUIDs are never displayed to the user.
- **SlotInfo**: The existing helper class on FormShipTemplate that stores SlotType, SlotIndex, and UUIDByIndex on each row's Tag.
- **Contains_Match**: A case-insensitive substring search using `String.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0`.

## Requirements

### Requirement 1: Column and Cell Architecture

**User Story:** As a developer, I want a DataGridViewFilteredComboBoxColumn that follows the standard DataGridView column/cell/editing-control pattern, so that I can drop it into any grid the same way I use DataGridViewComboBoxColumn or DataGridViewValidatedTextBoxColumn.

#### Acceptance Criteria

1. THE Column SHALL extend `DataGridViewColumn` and use a Cell as its `CellTemplate`.
2. THE Cell SHALL extend `DataGridViewCell` and return the Editing_Control type from its `EditType` property.
3. THE Cell SHALL return `typeof(string)` from its `ValueType` property.
4. THE Cell SHALL return `string.Empty` from its `DefaultNewRowValue` property.
5. WHEN the grid begins cell editing, THE Cell SHALL initialize the Editing_Control with the current cell value and the Full_Item_List from the Cell's `Items` collection.

### Requirement 2: Editing Control Layout

**User Story:** As a user, I want the inline editor to show a filter text box and a combo box side by side within the cell, so that I can type a filter and pick from the narrowed list without leaving the grid row.

#### Acceptance Criteria

1. THE Editing_Control SHALL be a `UserControl` that implements `IDataGridViewEditingControl`.
2. THE Editing_Control SHALL contain a Filter_TextBox and a Result_ComboBox arranged horizontally within the cell bounds.
3. WHEN the Editing_Control is displayed, THE Filter_TextBox SHALL receive keyboard focus.
4. THE Editing_Control SHALL resize the Filter_TextBox and Result_ComboBox to fill the available cell width.

### Requirement 3: Contains-Match Filtering

**User Story:** As a user, I want to type part of a blueprint name and see all items that contain that text anywhere in the name, so that I can find items without remembering the exact prefix.

#### Acceptance Criteria

1. WHEN the user types in the Filter_TextBox, THE Editing_Control SHALL rebuild the Filtered_Item_List using Contains_Match against every entry in the Full_Item_List.
2. WHEN the filter text is empty, THE Editing_Control SHALL display the Full_Item_List in the Result_ComboBox.
3. THE Editing_Control SHALL perform Contains_Match using case-insensitive comparison (`StringComparison.OrdinalIgnoreCase`).
4. WHEN the Filtered_Item_List changes, THE Editing_Control SHALL update the Result_ComboBox items and open the dropdown.

### Requirement 4: Item Selection and Value Commit

**User Story:** As a user, I want to select an item from the filtered dropdown and have the grid cell update with my choice, so that the selection is committed like any other combo column.

#### Acceptance Criteria

1. WHEN the user selects an item in the Result_ComboBox, THE Editing_Control SHALL set `EditingControlFormattedValue` to the selected string and notify the grid that the value has changed.
2. WHEN the user presses Enter after selecting an item, THE Editing_Control SHALL commit the selection and end cell editing.
3. WHEN the user presses Escape, THE Editing_Control SHALL cancel editing without changing the cell value.
4. WHEN the user presses Tab, THE Editing_Control SHALL commit the current selection and move focus to the next cell.
5. THE Editing_Control SHALL claim input keys for alphanumeric characters, arrow keys, Enter, Escape, and Tab via `EditingControlWantsInputKey`.

### Requirement 5: Plain String Items with Parallel UUID Tracking

**User Story:** As a developer, I want the column to work with plain string items and a parallel UUID list on the row Tag, so that it follows the project's established combo cell pattern and never exposes UUIDs to the user.

#### Acceptance Criteria

1. THE Column SHALL expose an `Items` property of type `List<string>` for setting the Full_Item_List at the column level.
2. THE Cell SHALL expose an `Items` property of type `List<string>` that can override the column-level list for per-row item sets.
3. THE Editing_Control SHALL display only plain strings from the Full_Item_List and SHALL NOT display UUIDs.
4. WHEN the Filtered_Item_List is rebuilt, THE Editing_Control SHALL maintain index correspondence with the Full_Item_List so that the consuming form can resolve the selected item's index back to the UUID_List on the row Tag.

### Requirement 6: IDataGridViewEditingControl Contract

**User Story:** As a developer, I want the Editing_Control to fully implement IDataGridViewEditingControl, so that it integrates correctly with DataGridView editing lifecycle.

#### Acceptance Criteria

1. THE Editing_Control SHALL implement `EditingControlDataGridView`, `EditingControlRowIndex`, `EditingControlValueChanged`, and `EditingControlFormattedValue` properties.
2. THE Editing_Control SHALL implement `ApplyCellStyleToEditingControl` by applying the cell style's Font and ForeColor to both the Filter_TextBox and Result_ComboBox.
3. THE Editing_Control SHALL implement `GetEditingControlFormattedValue` by returning the currently selected string from the Result_ComboBox, or `string.Empty` if nothing is selected.
4. THE Editing_Control SHALL implement `PrepareEditingControlForEdit` by clearing the Filter_TextBox, showing the Full_Item_List, and setting focus to the Filter_TextBox.
5. THE Editing_Control SHALL return `false` from `RepositionEditingControlOnValueChange`.
6. THE Editing_Control SHALL return `Cursors.IBeam` from `EditingPanelCursor`.

### Requirement 7: FormShipTemplate Integration

**User Story:** As a user, I want the ship template component slot grid to use the filtered combo column, so that I can quickly find blueprints in long lists by typing part of the name.

#### Acceptance Criteria

1. WHEN FormShipTemplate populates the slot grid, THE form SHALL set each Cell's `Items` to the list of eligible blueprint `ExtendedName` strings for that slot type and hull class.
2. THE form SHALL continue to store the UUID_List on the row Tag via the existing SlotInfo class.
3. WHEN the user selects a component via the filtered combo, THE form SHALL resolve the selected display name back to a UUID using the index position in the UUID_List, following the existing `dgvSlots_CellValueChanged` pattern.
4. THE form SHALL include an "(empty)" entry as the first item in each Cell's item list, representing no component selected.

### Requirement 8: Error Handling

**User Story:** As a developer, I want the control to handle edge cases gracefully, so that invalid states do not crash the grid or show raw UUIDs.

#### Acceptance Criteria

1. IF the Full_Item_List is null or empty, THEN THE Editing_Control SHALL display an empty Result_ComboBox and allow the user to cancel editing.
2. IF the cell value does not match any item in the Full_Item_List, THEN THE Cell SHALL display the cell value as-is without throwing an exception.
3. IF a DataError occurs on the column, THEN THE grid's DataError handler SHALL log the error and set `ThrowException` to false, following the project's standard DataError pattern.

### Requirement 9: Reusability Across Grids

**User Story:** As a developer, I want the column control to be generic and reusable, so that I can use it in other grids (blueprint resources, survey resources, colony structures) without modification.

#### Acceptance Criteria

1. THE Column SHALL NOT depend on any specific model type, form, or domain concept — it SHALL operate solely on plain string item lists.
2. THE Column SHALL be declared in the `OE2EmpireTracker.Controls` namespace alongside the other reusable controls.
3. THE Column SHALL support both column-level and per-cell item lists, so that grids with uniform items and grids with per-row items are both supported.
