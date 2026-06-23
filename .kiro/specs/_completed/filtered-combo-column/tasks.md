# Implementation Plan: Filtered Combo Column

## Overview

Replace the existing stub `DataGridViewFilteredComboBoxColumn` (UserControl + Designer + resx) with a proper DataGridView column/cell/editing-control triad. The new control provides inline filtered combo editing with contains-match filtering. Then integrate it into FormShipTemplate's slot grid, replacing the plain `DataGridViewComboBoxColumn`.

## Tasks

- [x] 1. Implement the DataGridViewFilteredComboBoxColumn control
  - [x] 1.1 Delete the stub files (Designer.cs, .resx) and replace the main .cs file
    - Delete `Controls/DataGridViewFilteredComboBoxColumn.Designer.cs`
    - Delete `Controls/DataGridViewFilteredComboBoxColumn.resx`
    - Remove the Designer.cs and .resx from the .csproj `<Compile>` / `<EmbeddedResource>` entries
    - Replace `Controls/DataGridViewFilteredComboBoxColumn.cs` with the new implementation containing all three classes
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 9.1, 9.2_

  - [x] 1.2 Implement `DataGridViewFilteredComboBoxColumn` class
    - Extend `DataGridViewColumn`, call `base(new DataGridViewFilteredComboBoxCell())` in constructor
    - Expose `Items` property (`List<string>`) for column-level default item list
    - Validate `CellTemplate` setter accepts only `DataGridViewFilteredComboBoxCell`
    - _Requirements: 1.1, 5.1, 9.1, 9.3_

  - [x] 1.3 Implement `DataGridViewFilteredComboBoxCell` class
    - Extend `DataGridViewCell`
    - Return `typeof(DataGridViewFilteredComboBoxEditingControl)` from `EditType`
    - Return `typeof(string)` from `ValueType`, `string.Empty` from `DefaultNewRowValue`
    - Expose `Items` property (`List<string>`) for per-cell override
    - Implement `InitializeEditingControl` — resolve effective items (cell ?? column), call `SetItems()` on editor
    - Implement `Clone()` — deep-copy `Items` list
    - Implement `Paint()` — render cell value as plain text (no dropdown chrome)
    - _Requirements: 1.2, 1.3, 1.4, 1.5, 5.2, 8.2, 9.3_

  - [x] 1.4 Implement `DataGridViewFilteredComboBoxEditingControl` class
    - Extend `UserControl`, implement `IDataGridViewEditingControl`
    - Create `txtFilter` (TextBox) and `cmbItems` (ComboBox, DropDownList style) arranged horizontally
    - Implement `SetItems(List<string>, string)` — store `_fullItems`, populate combo, select current value
    - Implement static `ApplyFilter(List<string>, string)` returning `(List<string> filtered, List<int> indexMap)`
    - Wire `txtFilter.TextChanged` to call `RebuildFilteredList()` using `ApplyFilter`, update combo, open dropdown
    - Implement all `IDataGridViewEditingControl` members: properties, `ApplyCellStyleToEditingControl`, `PrepareEditingControlForEdit`, `EditingControlWantsInputKey`, `GetEditingControlFormattedValue`
    - On combo `SelectedIndexChanged`, set `_valueChanged = true` and notify grid via `NotifyCurrentCellDirty(true)`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3, 4.4, 4.5, 5.3, 5.4, 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 8.1_

- [x] 2. Checkpoint — Verify control compiles
  - Ensure the solution builds cleanly with `getDiagnostics` or MSBuild
  - Ensure no references to the deleted Designer.cs / .resx remain in the .csproj

- [x] 3. Write property-based tests for the filtering logic
  - [x] 3.1 Write property test: contains-match filtering returns exact matches
    - **Property 1: Contains-match filtering returns exactly the matching items**
    - Test `ApplyFilter` with random string lists and random filter strings
    - Verify filtered output matches manual `IndexOf(filter, OrdinalIgnoreCase) >= 0` check
    - Verify empty filter returns full list; case-insensitive equivalence
    - **Validates: Requirements 3.1, 3.2, 3.3**

  - [x] 3.2 Write property test: index correspondence preserved after filtering
    - **Property 2: Index correspondence preserved after filtering**
    - Test `ApplyFilter` with random string lists and random filter strings
    - Verify each `filteredList[i] == fullList[indexMap[i]]` for all i
    - **Validates: Requirements 5.4, 7.3**

  - [x] 3.3 Write property test: selection sets EditingControlFormattedValue
    - **Property 3: Selection sets EditingControlFormattedValue**
    - Create editing control, call `SetItems` with random list, select random item
    - Verify `GetEditingControlFormattedValue` returns that exact string
    - Verify no selection returns `string.Empty`
    - **Validates: Requirements 4.1, 6.3**

  - [x] 3.4 Write property test: cell items override column items
    - **Property 4: Cell items override column items**
    - Generate two random string lists, set column items and cell items
    - Verify effective list is cell items when non-null, column items otherwise
    - **Validates: Requirements 5.2, 9.3**

  - [x] 3.5 Write property test: input key claiming
    - **Property 5: Input key claiming**
    - Generate random keys from expected set {A-Z, 0-9, arrows, Enter, Escape, Tab, Delete, Back}
    - Verify `EditingControlWantsInputKey` returns true for all
    - **Validates: Requirements 4.5**

  - [x] 3.6 Write property test: style propagation to child controls
    - **Property 6: Style propagation to child controls**
    - Generate random Font name/size and Color, apply via `ApplyCellStyleToEditingControl`
    - Verify both txtFilter and cmbItems have matching Font and ForeColor
    - **Validates: Requirements 6.2**

- [x] 4. Write unit tests for structural and edge-case behavior
  - [x] 4.1 Write unit tests for column/cell/editing-control structure
    - `Column_CellTemplate_IsFilteredComboBoxCell` (Req 1.1)
    - `Cell_EditType_IsEditingControl` (Req 1.2)
    - `Cell_ValueType_IsString` (Req 1.3)
    - `Cell_DefaultNewRowValue_IsEmptyString` (Req 1.4)
    - `EditingControl_ImplementsIDataGridViewEditingControl` (Req 2.1)
    - `EditingControl_ContainsTextBoxAndComboBox` (Req 2.2)
    - `EditingControl_RepositionOnValueChange_ReturnsFalse` (Req 6.5)
    - `EditingControl_EditingPanelCursor_IsIBeam` (Req 6.6)
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 6.5, 6.6_

  - [x] 4.2 Write unit tests for edge cases
    - `EditingControl_EmptyItems_ShowsEmptyCombo` (Req 8.1)
    - `EditingControl_NullItems_ShowsEmptyCombo` (Req 8.1)
    - `EditingControl_NoSelection_ReturnsEmptyString` (Req 6.3)
    - `PrepareEditingControlForEdit_ClearsFilterAndShowsFullList` (Req 6.4)
    - _Requirements: 6.3, 6.4, 8.1_

- [x] 5. Checkpoint — Verify control tests pass
  - Build the solution and run tests via vstest.console
  - Ensure all property tests and unit tests pass

- [x] 6. Integrate into FormShipTemplate
  - [x] 6.1 Update FormShipTemplate.Designer.cs — change colComponent type
    - Change `colComponent` declaration from `DataGridViewComboBoxColumn` to `DataGridViewFilteredComboBoxColumn`
    - Change the `new` in `InitializeComponent` from `DataGridViewComboBoxColumn` to `DataGridViewFilteredComboBoxColumn`
    - Add `using OE2EmpireTracker.Controls;` if not already present
    - _Requirements: 7.1_

  - [x] 6.2 Update FormShipTemplate.cs — change PopulateSlotGrid to use filtered combo cells
    - Change the cast from `DataGridViewComboBoxCell` to `DataGridViewFilteredComboBoxCell`
    - Set `cell.Items` instead of `comboCell.Items.Add(...)` — build the full string list and assign it
    - Keep the "(empty)" entry as the first item
    - Keep the `SlotInfo` / `UUIDByIndex` pattern on `row.Tag` unchanged
    - _Requirements: 7.1, 7.2, 7.4_

  - [x] 6.3 Update FormShipTemplate.cs — change dgvSlots_CellValueChanged for UUID resolution
    - Change the cast from `DataGridViewComboBoxCell` to `DataGridViewFilteredComboBoxCell`
    - Resolve selected index via `cell.Items.IndexOf(cell.Value)` instead of `comboCell.Items.IndexOf(comboCell.Value)`
    - Keep the existing UUID lookup via `SlotInfo.UUIDByIndex[selectedIdx]` unchanged
    - _Requirements: 7.3_

- [x] 7. Final checkpoint — Verify full integration
  - Build the solution and run all tests via vstest.console
  - Ensure all tests pass, ask the user if questions arise

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- The control file (`DataGridViewFilteredComboBoxColumn.cs`) contains all three classes (Column, Cell, EditingControl) in a single file — no Designer needed
- The static `ApplyFilter` method enables property tests 1 and 2 to run without a hosted DataGridView
- Property tests use FsCheck + NUnit (`[FsCheck.NUnit.Property]` attribute)
- Unit tests use NUnit (`[Test]` attribute)
- Test files: `OE2EmpireTracker.Tests/Controls/FilteredComboColumnPropertyTests.cs` and `FilteredComboColumnTests.cs`
- Build with MSBuild, test with vstest.console (not dotnet test)
