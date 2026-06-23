# Requirements Document

## Introduction

Three custom controls  FilteredTextComboSet, DataGridViewFilteredComboBoxColumn, and DataEntryGridView  were built and deployed in the ship forms (FormShipTemplate, FormShipInstance, FormBlueprintV2). This spec covers adopting those controls across all remaining forms that still use the old manual patterns they replace. The goal is consistent UX (inline filtering for large dropdowns, Tab-navigation in editable grids) and reduced code duplication.

## Glossary

- **FilteredTextComboSet**: A UserControl combining a filter TextBox and a ComboBox. When focused, a filter TextBox appears alongside the ComboBox. Typing filters items using case-insensitive contains-match. Replaces the manual pattern of a separate TextBox + ComboBox where the TextBox's TextChanged handler repopulates the ComboBox.
- **DataGridViewFilteredComboBoxColumn**: A DataGridView column that hosts a FilteredTextComboSet as its editing control. Replaces DataGridViewComboBoxColumn in grids where the dropdown has many items (50+) and needs inline filtering.
- **DataEntryGridView**: A DataGridView subclass that handles Tab/Shift+Tab navigation between editable cells, skipping read-only columns. Replaces standard DataGridView in grids with a mix of read-only and editable columns.
- **Old_Filter_Pattern**: The manual pattern of a separate ValidatedTextBox + ComboBox where the TextBox's TextChanged handler repopulates the ComboBox with filtered items. Each site duplicates the filtering logic.
- **Candidate_Site**: A specific location in a form where one of the three custom controls should replace the old pattern.
- **Designer_File**: The .Designer.cs file for a WinForms form, containing control declarations and initialization code. Must be hand-edited to change control types.
- **IProgrammaticUpdateSource**: Interface implemented by all forms providing BeginProgrammaticUpdate/EndProgrammaticUpdate to suppress event handlers during programmatic data changes.

## Requirements

### Requirement 1: Replace Old Filter Pattern in ColonyStructureV2  Survey Picker

**User Story:** As a user managing colony mining rigs, I want the survey picker to use the FilteredTextComboSet control, so that I get consistent inline filtering behavior matching the ship forms.

#### Acceptance Criteria

1. WHEN the ColonyStructureV2 control is loaded, THE FilteredTextComboSet SHALL replace the txtSurveyFilter TextBox and cmbSurvey ComboBox with a single cmbSurvey FilteredTextComboSet control.
2. WHEN the user types in the filter area of the cmbSurvey FilteredTextComboSet, THE FilteredTextComboSet SHALL filter the survey list using case-insensitive contains-match.
3. WHEN the user selects a survey from the filtered list, THE ColonyStructureV2 SHALL write the selection through to the data model, preserving the existing _isProgrammaticUpdate guard behavior.
4. THE Designer_File for ColonyStructureV2 SHALL declare cmbSurvey as type OE2EmpireTracker.Controls.FilteredTextComboSet and SHALL remove the txtSurveyFilter and lblSurveyFilter controls.
5. WHEN the ColonyStructureV2 populates the survey picker, THE ColonyStructureV2 SHALL use the FilteredTextComboSet.SetItems method instead of manually repopulating ComboBox items.

### Requirement 2: Replace Old Filter Pattern in ColonyStructureV2  Resource Picker

**User Story:** As a user managing colony structures, I want the resource/selection picker to use the FilteredTextComboSet control, so that I get consistent inline filtering.

#### Acceptance Criteria

1. WHEN the ColonyStructureV2 control is loaded, THE FilteredTextComboSet SHALL replace the txtSelectionFilter TextBox and cmbSelection ComboBox with a single cmbSelection FilteredTextComboSet control.
2. WHEN the user types in the filter area of the cmbSelection FilteredTextComboSet, THE FilteredTextComboSet SHALL filter the selection list using case-insensitive contains-match.
3. WHEN the user selects an item from the filtered list, THE ColonyStructureV2 SHALL write the selection through to the data model, preserving the existing _isProgrammaticUpdate guard behavior.
4. THE Designer_File for ColonyStructureV2 SHALL declare cmbSelection as type OE2EmpireTracker.Controls.FilteredTextComboSet and SHALL remove the txtSelectionFilter and lblSelectionFilter controls.

### Requirement 3: Replace Old Filter Pattern in FormSurvey  Scanner Blueprint Picker

**User Story:** As a user editing surveys, I want the scanner blueprint picker to use the FilteredTextComboSet control, so that I can filter through many scanner blueprints consistently.

#### Acceptance Criteria

1. WHEN the FormSurvey form is loaded, THE FilteredTextComboSet SHALL replace the txtFilterScannerBlueprint TextBox and cmbScannerBlueprint ComboBox with a single cmbScannerBlueprint FilteredTextComboSet control.
2. WHEN the user types in the filter area of the cmbScannerBlueprint FilteredTextComboSet, THE FilteredTextComboSet SHALL filter the scanner blueprint list using case-insensitive contains-match.
3. WHEN the user selects a scanner blueprint from the filtered list, THE FormSurvey SHALL write the selection through to the data model, preserving the existing _isProgrammaticUpdate guard behavior.
4. THE Designer_File for FormSurvey SHALL declare cmbScannerBlueprint as type OE2EmpireTracker.Controls.FilteredTextComboSet and SHALL remove the txtFilterScannerBlueprint control.
5. THE FormSurvey SHALL remove the TxtFilterScannerBlueprint_TextChanged event handler, as filtering is handled internally by the FilteredTextComboSet.

### Requirement 4: Replace Old Filter Pattern in FormDeliveryRoute  Drop-Off Item Picker

**User Story:** As a user configuring delivery routes, I want the drop-off item picker to use the FilteredTextComboSet control, so that I can filter through items consistently.

#### Acceptance Criteria

1. WHEN the FormDeliveryRoute form is loaded, THE FilteredTextComboSet SHALL replace the txtDropFilter TextBox and cmbDropItem ComboBox with a single cmbDropItem FilteredTextComboSet control.
2. WHEN the user types in the filter area of the cmbDropItem FilteredTextComboSet, THE FilteredTextComboSet SHALL filter the drop-off item list using case-insensitive contains-match.
3. WHEN the user selects an item from the filtered list, THE FormDeliveryRoute SHALL preserve the existing _isProgrammaticUpdate guard behavior.
4. THE Designer_File for FormDeliveryRoute SHALL declare cmbDropItem as type OE2EmpireTracker.Controls.FilteredTextComboSet and SHALL remove the txtDropFilter control.

### Requirement 5: Replace Old Filter Pattern in FormDeliveryRoute  Pick-Up Item Picker

**User Story:** As a user configuring delivery routes, I want the pick-up item picker to use the FilteredTextComboSet control, so that I can filter through items consistently.

#### Acceptance Criteria

1. WHEN the FormDeliveryRoute form is loaded, THE FilteredTextComboSet SHALL replace the txtPickFilter TextBox and cmbPickItem ComboBox with a single cmbPickItem FilteredTextComboSet control.
2. WHEN the user types in the filter area of the cmbPickItem FilteredTextComboSet, THE FilteredTextComboSet SHALL filter the pick-up item list using case-insensitive contains-match.
3. WHEN the user selects an item from the filtered list, THE FormDeliveryRoute SHALL preserve the existing _isProgrammaticUpdate guard behavior.
4. THE Designer_File for FormDeliveryRoute SHALL declare cmbPickItem as type OE2EmpireTracker.Controls.FilteredTextComboSet and SHALL remove the txtPickFilter control.

### Requirement 6: Replace Old Filter Patterns in FormColonyV2  Five Pickers

**User Story:** As a user managing colonies, I want all five filtered pickers (flatpack, item, overflow resource, overflow destination, overflow route) to use the FilteredTextComboSet control, so that filtering behavior is consistent across the colony form.

#### Acceptance Criteria

1. THE Designer_File for FormColonyV2 SHALL declare cmbFlatpacks as type OE2EmpireTracker.Controls.FilteredTextComboSet and SHALL remove the txtFilterFlatpack control.
2. THE Designer_File for FormColonyV2 SHALL declare cmbItem as type OE2EmpireTracker.Controls.FilteredTextComboSet and SHALL remove the txtItemFilter control.
3. THE Designer_File for FormColonyV2 SHALL declare cmbOverflowResource as type OE2EmpireTracker.Controls.FilteredTextComboSet and SHALL remove the txtOverflowResourceFilter control.
4. THE Designer_File for FormColonyV2 SHALL declare cmbOverflowDest as type OE2EmpireTracker.Controls.FilteredTextComboSet and SHALL remove the txtOverflowDestFilter control.
5. THE Designer_File for FormColonyV2 SHALL declare cmbOverflowRoute as type OE2EmpireTracker.Controls.FilteredTextComboSet and SHALL remove the txtOverflowRouteFilter control.
6. WHEN the user types in the filter area of any of the five FilteredTextComboSet controls, THE FilteredTextComboSet SHALL filter the corresponding item list using case-insensitive contains-match.
7. WHEN the user selects an item from any of the five FilteredTextComboSet controls, THE FormColonyV2 SHALL write the selection through to the data model, preserving the existing _isProgrammaticUpdate guard behavior.
8. THE FormColonyV2 SHALL use the FilteredTextComboSet.SetItems method for all five pickers instead of manually repopulating ComboBox items.

### Requirement 7: Replace DataGridViewComboBoxColumn in FormSurvey  Resource Column

**User Story:** As a user editing survey resources, I want the Resource column in the dgvResources grid to use the DataGridViewFilteredComboBoxColumn, so that I can filter through 50+ resources inline.

#### Acceptance Criteria

1. THE Designer_File for FormSurvey SHALL declare the Resource column as type OE2EmpireTracker.Controls.DataGridViewFilteredComboBoxColumn instead of System.Windows.Forms.DataGridViewComboBoxColumn.
2. WHEN the FormSurvey populates the dgvResources grid, THE FormSurvey SHALL set the Items property on the DataGridViewFilteredComboBoxColumn with the full resource list.
3. WHEN the user edits a Resource cell, THE DataGridViewFilteredComboBoxColumn SHALL display a filter TextBox alongside the ComboBox for inline filtering.
4. THE Purity column in dgvResources SHALL remain as a standard DataGridViewComboBoxColumn because the purity list contains only 5 items.

### Requirement 8: Replace DataGridViewComboBoxColumn in FormBlueprintV2  Resource Column

**User Story:** As a user editing blueprint resources, I want the colResource column in the dgvResources grid to use the DataGridViewFilteredComboBoxColumn, so that I can filter through 50+ resources inline.

#### Acceptance Criteria

1. THE Designer_File for FormBlueprintV2 SHALL declare the colResource column as type OE2EmpireTracker.Controls.DataGridViewFilteredComboBoxColumn instead of System.Windows.Forms.DataGridViewComboBoxColumn.
2. WHEN the FormBlueprintV2 populates the dgvResources grid, THE FormBlueprintV2 SHALL set the Items property on the DataGridViewFilteredComboBoxColumn with the full resource list.
3. WHEN the user edits a colResource cell, THE DataGridViewFilteredComboBoxColumn SHALL display a filter TextBox alongside the ComboBox for inline filtering.

### Requirement 9: Replace DataGridView with DataEntryGridView in FormSurvey

**User Story:** As a user editing survey resource rows, I want Tab/Shift+Tab to navigate between editable cells (Resource, Purity, Amount) and skip read-only columns, so that data entry is efficient.

#### Acceptance Criteria

1. THE Designer_File for FormSurvey SHALL declare dgvResources as type DataEntryGridView instead of System.Windows.Forms.DataGridView.
2. WHEN the user presses Tab in an editable cell of dgvResources, THE DataEntryGridView SHALL move focus to the next editable cell, skipping read-only columns.
3. WHEN the user presses Shift+Tab in an editable cell of dgvResources, THE DataEntryGridView SHALL move focus to the previous editable cell, skipping read-only columns.
4. WHEN the user presses Tab on the last editable cell of a row, THE DataEntryGridView SHALL move focus to the first editable cell of the next row.

### Requirement 10: Replace DataGridView with DataEntryGridView in FormColonyV2

**User Story:** As a user editing colony commodity requests and items, I want Tab/Shift+Tab to navigate between editable cells and skip read-only columns in dgvCommodityRequests and dgvItems.

#### Acceptance Criteria

1. THE Designer_File for FormColonyV2 SHALL declare dgvCommodityRequests as type DataEntryGridView instead of System.Windows.Forms.DataGridView.
2. THE Designer_File for FormColonyV2 SHALL declare dgvItems as type DataEntryGridView instead of System.Windows.Forms.DataGridView.
3. WHEN the user presses Tab in an editable cell of either grid, THE DataEntryGridView SHALL move focus to the next editable cell, skipping read-only columns.
4. WHEN the user presses Shift+Tab in an editable cell of either grid, THE DataEntryGridView SHALL move focus to the previous editable cell, skipping read-only columns.

### Requirement 11: Replace DataGridView with DataEntryGridView in FormDeliveryRoute

**User Story:** As a user editing delivery route items, I want Tab/Shift+Tab to navigate between editable cells and skip read-only columns in dgvDropOff and dgvPickUp.

#### Acceptance Criteria

1. THE Designer_File for FormDeliveryRoute SHALL declare dgvDropOff as type DataEntryGridView instead of System.Windows.Forms.DataGridView.
2. THE Designer_File for FormDeliveryRoute SHALL declare dgvPickUp as type DataEntryGridView instead of System.Windows.Forms.DataGridView.
3. WHEN the user presses Tab in an editable cell of either grid, THE DataEntryGridView SHALL move focus to the next editable cell, skipping read-only columns.
4. WHEN the user presses Shift+Tab in an editable cell of either grid, THE DataEntryGridView SHALL move focus to the previous editable cell, skipping read-only columns.

### Requirement 12: Replace DataGridView with DataEntryGridView in FormBlueprintV2

**User Story:** As a user editing blueprint resource rows, I want Tab/Shift+Tab to navigate between editable cells (Resource, Amount) and skip read-only columns in dgvResources.

#### Acceptance Criteria

1. THE Designer_File for FormBlueprintV2 SHALL declare dgvResources as type DataEntryGridView instead of System.Windows.Forms.DataGridView.
2. WHEN the user presses Tab in an editable cell of dgvResources, THE DataEntryGridView SHALL move focus to the next editable cell, skipping read-only columns.
3. WHEN the user presses Shift+Tab in an editable cell of dgvResources, THE DataEntryGridView SHALL move focus to the previous editable cell, skipping read-only columns.

### Requirement 13: Remove Duplicate Filter Logic from Code-Behind Files

**User Story:** As a developer, I want the manual TextChanged filter handlers removed from all converted forms, so that filtering logic lives only in the FilteredTextComboSet control and is not duplicated.

#### Acceptance Criteria

1. WHEN a TextBox+ComboBox pair is replaced by a FilteredTextComboSet, THE code-behind file SHALL remove the TextChanged event handler that previously repopulated the ComboBox.
2. WHEN a TextBox+ComboBox pair is replaced by a FilteredTextComboSet, THE code-behind file SHALL remove the manual item-filtering logic that was previously in the TextChanged handler.
3. WHEN a TextBox+ComboBox pair is replaced by a FilteredTextComboSet, THE code-behind file SHALL subscribe to the FilteredTextComboSet.SelectedItemChanged event instead of the ComboBox.SelectedIndexChanged event.
4. THE code-behind file SHALL use FilteredTextComboSet.SetItems to populate items and FilteredTextComboSet.SelectedItem or FilteredTextComboSet.SelectedFullIndex to read the selection.

### Requirement 14: Preserve IProgrammaticUpdateSource Guards

**User Story:** As a developer, I want all _isProgrammaticUpdate guards preserved during the control adoption, so that programmatic data changes do not trigger unintended event handler side effects.

#### Acceptance Criteria

1. WHEN a FilteredTextComboSet.SelectedItemChanged event handler is added, THE event handler SHALL check _isProgrammaticUpdate and return early if a programmatic update is in progress.
2. WHEN a DataGridView is replaced with DataEntryGridView, THE existing CellValueChanged and CurrentCellDirtyStateChanged handlers SHALL continue to check _isProgrammaticUpdate guards.
3. WHEN a DataGridViewComboBoxColumn is replaced with DataGridViewFilteredComboBoxColumn, THE existing DataError handler SHALL remain wired to the grid.

### Requirement 15: Update Mockups for Converted Forms

**User Story:** As a developer, I want the mockups in spec/mockups/ updated to reflect the control consolidation, so that documentation stays in sync with the code.

#### Acceptance Criteria

1. WHEN a TextBox+ComboBox pair is replaced by a FilteredTextComboSet, THE corresponding mockup SHALL remove the filter TextBox from the wireframe and control list, and SHALL show the FilteredTextComboSet as a single control.
2. WHEN a DataGridView is replaced with DataEntryGridView, THE corresponding mockup SHALL note the DataEntryGridView type in the control list.
3. WHEN a DataGridViewComboBoxColumn is replaced with DataGridViewFilteredComboBoxColumn, THE corresponding mockup SHALL note the DataGridViewFilteredComboBoxColumn type in the grid column description.