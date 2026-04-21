# Implementation Plan: Ship Form Overhaul

## Overview

Overhaul `FormShipInstance` to align with `FormShipTemplate` by relocating buttons, adding a hull combo, upgrading the component grid to use `DataGridViewFilteredComboBoxColumn`, adding slot index and condition/max-repair columns, and removing the swap component button. All changes are confined to `FormShipInstance.Designer.cs` and `FormShipInstance.cs`, reusing existing controls and patterns from `FormShipTemplate`.

## Tasks

- [x] 1. Designer.cs — Restructure controls and columns
  - [x] 1.1 Add hull row controls (`flpHull`, `lblHull`, `cmbHull` FilteredTextComboSet) and declare the `_hullUUIDs` field
    - Add `flpHull` FlowLayoutPanel, `lblHull` Label, and `cmbHull` FilteredTextComboSet declarations
    - Insert `flpHull` into `flpDetail.Controls` between `flpName` and `flpLocation`
    - Add `private List<string> _hullUUIDs` field in code-behind
    - _Requirements: 2.1_

  - [x] 1.2 Replace `colComponentName` with `colComponent` (DataGridViewFilteredComboBoxColumn) and add `colSlotIndex`
    - Remove `colComponentName` (DataGridViewTextBoxColumn) declaration and all references
    - Add `colSlotIndex` (DataGridViewTextBoxColumn, read-only, header "#", width 40)
    - Add `colComponent` (DataGridViewFilteredComboBoxColumn, header "Component", width 300)
    - Update `dgvComponents.Columns.AddRange` to: `colSlotType`, `colSlotIndex`, `colComponent`, `colCondition`, `colMaxRepair`
    - Set `dgvComponents.SelectionMode = CellSelect` and `dgvComponents.EditMode = EditOnEnter`
    - _Requirements: 4.1, 4.2, 6.1, 6.2, 8.2_

  - [x] 1.3 Relocate `flpCommands` from `flpSearchList` to `flpDetail` and move `cmdSave` into `flpCommands`
    - Remove `flpCommands` from `flpSearchList.Controls`
    - Remove standalone `cmdSave` from `flpDetail.Controls`
    - Add `cmdSave` into `flpCommands.Controls` (order: cmdNew, cmdSave, cmdDelete, cmdFromTemplate)
    - Add `flpCommands` as last child of `flpDetail.Controls` (after tabControl)
    - `flpDetail` child order: flpName, flpHull, flpLocation, tabControl, flpCommands
    - `flpSearchList` child order: flpFilter, lvwShips (only)
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

  - [x] 1.4 Remove `cmdSwapComponent` from Overview tab
    - Remove `cmdSwapComponent` declaration and remove from `tabOverview.Controls`
    - _Requirements: 7.1_

- [x] 2. Checkpoint — Verify Designer changes compile
  - Ensure all diagnostics pass after Designer.cs restructuring, ask the user if questions arise.

- [x] 3. Code-behind — Hull combo and new-ship logic
  - [x] 3.1 Add `SlotInfo` and `SlotDefinition` inner classes
    - Copy `SlotInfo` (SlotType, SlotIndex, UUIDByIndex) and `SlotDefinition` (SlotType, MaxCount, BlueprintTypes) from `FormShipTemplate`
    - _Requirements: 4.4 (SlotInfo for UUID resolution)_

  - [x] 3.2 Implement `PopulateHullCombo` and `SelectHullInCombo`
    - `PopulateHullCombo`: query all blueprints where `BluePrintType == "Hull"`, populate `cmbHull` display names and `_hullUUIDs` parallel list
    - `SelectHullInCombo(string hullUUID)`: find index in `_hullUUIDs`, set `cmbHull` selection
    - Call `PopulateHullCombo()` from constructor and `OnCurrentPlayerChanged`
    - Call `SelectHullInCombo` from `PopulateForm`
    - _Requirements: 2.2, 2.5_

  - [x] 3.3 Implement `cmbHull_SelectedItemChanged` event handler
    - Read `cmbHull.SelectedFullIndex`, resolve UUID from `_hullUUIDs`
    - Set `_selectedShip.HullBlueprintUUID`, clear components
    - Call `PopulateOverviewGrid()` + `RefreshStats()`
    - Subscribe in constructor
    - _Requirements: 2.3, 2.4_

  - [x] 3.4 Rewrite `cmdNew_Click` for blank ship creation
    - Create new `Ship` with generated UUID, `Name = "New Ship"`, current player owner
    - Add to player context, persist, select in list, populate form
    - No longer route through `cmdFromTemplate_Click`
    - Retain `cmdFromTemplate` as separate action
    - _Requirements: 3.1, 3.2, 3.3, 3.4_

  - [x] 3.5 Update `ClearForm` and `SetDetailEnabled` for hull combo
    - `ClearForm`: clear hull combo selection
    - `SetDetailEnabled`: enable/disable `cmbHull`
    - _Requirements: 2.6_

- [x] 4. Checkpoint — Verify hull combo and new-ship logic compile
  - Ensure all diagnostics pass, ask the user if questions arise.

- [x] 5. Code-behind — Component grid upgrade
  - [x] 5.1 Implement `GetSlotDefinitions` method
    - Copy from `FormShipTemplate` — reads hull properties via `SlotTypes.HullPropertyToSlotType`, builds list of `SlotDefinition`
    - _Requirements: 4.2 (slot enumeration for grid population)_

  - [x] 5.2 Rewrite `PopulateOverviewGrid` for filtered combo column
    - Use `GetSlotDefinitions` to enumerate hull slots
    - For each slot: create row with `SlotInfo` tag containing `UUIDByIndex`
    - Set `DataGridViewFilteredComboBoxCell.Items` per-cell with eligible blueprints + "(empty)" first entry
    - Hull row first, component cell read-only with hull ExtendedName
    - Populate `colSlotIndex` with zero-based index (empty for hull row)
    - Populate `colCondition` and `colMaxRepair` from existing component data or hull data
    - _Requirements: 4.2, 4.3, 4.7, 5.1, 5.2, 6.2, 6.3_

  - [x] 5.3 Implement `dgvComponents_CellValueChanged` for component selection
    - Handle component combo selection: resolve UUID from `SlotInfo.UUIDByIndex`
    - Update or remove component slot based on selection
    - Call `RefreshStats()`
    - _Requirements: 4.4, 4.5, 4.6, 7.2_

  - [x] 5.4 Update `dgvComponents_CellEndEdit` for Condition and MaxRepair edits
    - Handle Condition column edits: update `CurrentHP` or `HullCurrentHP` for hull row
    - Handle MaxRepair column edits: update `MaxRepairPercent` or `HullMaxRepairPercent` for hull row
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

  - [x] 5.5 Add grid event handlers: `DataError`, `CurrentCellDirtyStateChanged`, `CellClick`
    - `dgvComponents_DataError`: log error, set `ThrowException = false`
    - `dgvComponents_CurrentCellDirtyStateChanged`: commit edit immediately
    - `dgvComponents_CellClick`: begin edit when component column clicked
    - Subscribe all in constructor
    - _Requirements: 8.1, 8.2, 8.3_

- [x] 6. Checkpoint — Verify component grid upgrade compiles
  - Ensure all diagnostics pass, ask the user if questions arise.

- [x] 7. Code-behind — Layout handlers and cleanup
  - [x] 7.1 Update `flpDetail_Layout` for new hull row and relocated command panel
    - Account for `flpHull.Height` and `flpCommands.Height` in tab control sizing
    - Remove standalone `cmdSave.Height` from calculation
    - _Requirements: 9.2_

  - [x] 7.2 Update `flpSearchList_Layout` to remove command panel height
    - Remove `flpCommands.Height` from list height calculation (commands no longer in search panel)
    - _Requirements: 9.3_

  - [x] 7.3 Remove `cmdSwapComponent_Click` method and clean up constructor wiring
    - Remove `cmdSwapComponent_Click` method
    - Remove `cmdSwapComponent.Click` subscription from constructor
    - Wire `cmdSave.Click` in constructor if not already wired via flpCommands relocation
    - _Requirements: 7.1, 7.2_

- [-] 8. Final checkpoint — Full build and verify
  - Ensure all diagnostics pass and the form compiles cleanly, ask the user if questions arise.

## Notes

- All changes are confined to `FormShipInstance.Designer.cs` and `FormShipInstance.cs`
- No new models or services — reuses existing `FilteredTextComboSet`, `DataGridViewFilteredComboBoxColumn`, and parallel UUID patterns from `FormShipTemplate`
- `SlotInfo` and `SlotDefinition` inner classes are copied from `FormShipTemplate`
- Each task references specific requirement acceptance criteria (e.g., 2.1 = Requirement 2, Criterion 1)
- Checkpoints ensure incremental validation after each major area of change
