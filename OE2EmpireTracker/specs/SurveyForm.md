# Survey Form — Spec Document

> Complete specification for the Survey Form used in OE2 Empire Tracker to manage planet survey data from Outer Empires 2 scanner blueprints.

## Overview

The Survey Form is a Windows Forms application that allows users to view, create, edit, and manage survey records obtained from scanner blueprints in Outer Empires 2. Each survey captures resource discoveries, sensor readings, and scan metadata for a specific planet.

## Form Dimensions and Layout

- **Form Size:** 1074 × 704 pixels
- **Auto Size Enabled:** True
- **Auto Scale Mode:** Font
- **Maximum Size:** Unrestricted

---

## Control Layout (Left-to-Right Analysis)

### Left Panel: Search List (`flpSearchList`) - Width: ~427px

Located at position (3, 3) within the main layout flow panel. Contains three filter/search sections stacked vertically.

#### Section 1: Planet Filter (`flpBlueprintSearch`) - Height: 26px

| Control           | Type    | Position | Size      | Properties                                     |
|---------          |------   |--------  |------     |------------                                    |
| `lblPlanetFilter` | Label   | (2, 4)   | 100×17 px | Text: "Planet", RightAlign, Anchor=Left\|Right |
| `txtPlanetFilter` | TextBox | (107, 3) | 202×20 px | TabIndex=0, Location relative to flow panel    |

**Purpose:** Filter surveys by planet name.

#### Section 2: Resource Filter (`flpResource`) - Height: 25px, Y position: ~35px

| Control | Type | Position | Size | Properties |
|---------|------|----------|------|------------|
| `lblResource` | Label | (2, 4) | 100×17 px | Text: "Resource", RightAlign, Anchor=Left\|Right |
| `cmbResource` | ComboBox | (106, 2) | 201×21 px | TabIndex=1, DropDownStyle=DropDownList, AutoComplete=SuggestAppend |

**Purpose:** Filter surveys by resource name.

#### Section 3: Survey List (`lvwSurveys`) - Height: 566px, Y position: ~65px

| Control | Type | Position | Size | Properties |
|---------|------|----------|------|------------|
| `lvwSurveys` | ListView | (3, 62) | 412×566 px | TabIndex=6, View=Details, FullRowSelect=True, MultiSelect=False, HideSelection=False |

**Columns:**
1. **UUID** (Column index 0) - Primary key display
2. **PlanetName** (Column index 1, width: 100px)
3. **NickName** (Column index 2, width: 100px)
4. **DateTime** (Column index 3, width: 100px)

**Features:**
- Displays existing survey records retrieved from `playerContext.surveyList`
- Clicking a row loads survey data into the form fields
- ListView items store reference to Survey object in Tag property

---

### Right Panel: Survey Data (`flpSurveyData`) - Width: ~661px

Located at position (436, 3) within the main layout. Split into two sub-sections.

#### Top Section: Survey Details (`flpSurveyDetails`) - Height: 569px

Vertical flow direction with top-to-bottom field layout. Each field consists of a FlowLayoutPanel containing a Label and TextBox/ComboBox.

**Field Sequence (Top to Bottom):**

| Field | Label Text | Control Type | Position | Size | TabIndex |
|-------|-----------|--------------|----------|------|----------|
| Planet Name | "Planet Name" | TextBox | (107, 3) | 200×20 px | 0 |
| Survey ID | "Survey ID" | TextBox | (107, 3) | 200×20 px | 0 |
| Nick Name | "Nick Name" | TextBox | (107, 3) | 200×20 px | 0 |
| Scanner Blueprint* | "Scanner Blueprint" | ComboBox + Filter TextBox | See below | - | - |
| Scanned By | "Scanned By" | TextBox | (106, 2) | 201×20 px | 7 |
| Scan DateTime | "Scan DateTime" | TextBox | (106, 2) | 201×20 px | 7 |
| Sensor Abundance Factor | "Sensor Abundance Factor" | TextBox | (106, 2) | 201×20 px | 7 |
| Purity Modifier | "Purity Modifier" | TextBox | (106, 2) | 201×20 px | 7 |
| Scan Level | "Scan Level" | TextBox | (106, 2) | 201×20 px | 7 |

*Scanner Blueprint field has special layout (see details below).

**Resource Data Grid (`dgvResources`)** - Height: 303px

| Control | Type | Position | Size | Columns |
|---------|------|----------|------|---------|
| `dgvResources` | DataGridView | (3, 263) | 825×303 px | TabIndex=9 |

**Columns (Left-to-Right):**

1. **Resource** - DataGridViewComboBoxColumn, Width: 250px
   - DisplayMember: Resource.Name
   - ValueMember: Resource.Name
   - DataSource: `empireContext.bindingSourceResource`
   - User-added column (true)

2. **Purity** - DataGridViewComboBoxColumn
   - DisplayMember: Purity.Name
   - ValueMember: Purity.Name
   - DataSource: `empireContext.bindingSourceResourcePurity`
   - User-added column (true)

3. **Amount** - DataGridViewTextBoxColumn (user-added column)

#### Bottom Section: Command Buttons (`flpCommands`) - Height: 29px, Y position: ~575px

| Control | Type | Position | Size | Properties |
|---------|------|----------|------|------------|
| `btnSave` | Button | (3, 3) | 75×23 px | TabIndex=0, Text: "Save" |
| `cmdDelete` | Button | (84, 3) | 75×23 px | TabIndex=1, Text: "Delete" |
| `btnCancel` | Button | (165, 3) | 75×23 px | TabIndex=2, Text: "Cancel" |

---

### Special Layout: Scanner Blueprint Field (`flpScannerBlueprint`)

Unique field with additional filter textbox for search capability.

**Layout:**

| Control | Type | Position | Size | Purpose |
|---------|------|----------|------|---------|
| `lblScannerBlueprint` | Label | (2, 4) | 100×17 px | Text: "Scanner Blueprint" |
| `txtFilterScannerBlueprint` | TextBox | (107, 3) | 100×20 px | Filter/search text box, TabIndex=0 |
| `cmbScannerBlueprint` | ComboBox | (212, 2) | 201×21 px | Dropdown list of scanner blueprints, TabIndex=1 |

**Behavior:**

- Filter textbox (`txtFilterScannerBlueprint`) has TextChanged event that triggers filtering
- Displays filtered SystemObjectScanner blueprints from player context
- Allows deselecting by selecting the empty entry (first item in dropdown)

---

## Form Behavior

### Initialization

When the SurveyForm is first instantiated and loaded, the following initialization steps occur automatically:

1. **Component Setup:** All form controls are created and configured through the design-time component initializer
2. **Context Acquisition:** The application acquires two context objects from the EmpireContext singleton - an instance for current empire operations and a static PlayerContext reference for player-specific data
3. **Scanner Blueprint Configuration:** The scanner blueprint dropdown is prepared with its extended name as the display text and UUID as the underlying value identifier, then populated with available blueprints filtered by type
4. **Initial Deselection:** No scanner blueprint is selected initially by setting the index to negative one
5. **Survey List Setup:** The survey list view is configured in details mode with four columns: UUID, Planet Name, Nickname, and Scan Date/Time, then populated with all surveys currently known to the player
6. **Resource Grid Configuration:** The resources data grid is prepared with three columns for resource type, purity level, and quantity

### Survey List Population

When survey records are loaded or updated from the player context, the following process occurs:

A dictionary structure is created with UUIDs as keys to track individual surveys and detect duplicates. As new surveys arrive from the player context's survey list, the ListView items are refreshed - if a matching survey already exists in the view, its content is updated; if it's new, a ListViewItem is created using data from the first row of the survey object. Items that no longer exist in the source collection are removed from the ListView to keep the display synchronized with actual data. The Tag property of each ListView item maintains a direct reference to the underlying Survey object, which enables efficient editing operations when items are selected.

### Save Operation

When the user clicks the Save button, the save operation proceeds through the following steps:

First, the system determines whether this is an update operation or a new record creation by checking if a survey has been previously selected. If updating an existing survey, the current survey object is retrieved and its data will be refreshed from form inputs. If creating a new survey, a brand new globally unique identifier is generated and assigned to ensure uniqueness across all surveys.

Next, all scalar form field values are transferred to their corresponding survey properties: the planet name from its text box, the official survey ID, nickname, operator name (who performed the scan), timestamp of the scan, sensor abundance factor, purity modifier, and scan level designation. The scanner blueprint UUID is retrieved from the selected item in the scanner blueprint dropdown if one has been chosen.

Then, resource data is processed by iterating through each row in the resources grid. For every row, three values are extracted: the resource name (which serves as the dictionary key), the purity level, and the quantity amount. A new SurveyResource object is instantiated for each entry, its properties set from the extracted values, and it's stored in a dictionary keyed by resource name - this allows quick lookup of resources by their names during data retrieval.

Finally, the survey object is persisted to the player context's survey list using appropriate methods to either add a new entry or update an existing one depending on operation type. The context changes are written back to the database to ensure persistence. Once saved, the form is automatically cleared for new input.

### Form Loading When Survey is Selected

When the user selects a different item in the survey list view, the following process occurs:

The currently selected survey object is retrieved from the Tag property of the ListView item that was clicked. This survey object serves as the data source for populating all form fields with matching information:

- The planet name field receives the survey's planet designation
- The survey ID text box is populated with the official identifier
- The nickname field shows any custom nickname assigned
- The operator name field displays who conducted this scan
- The timestamp field shows when the scan was performed
- The sensor abundance factor, purity modifier, and scan level fields are all populated from their respective stored property values

The scanner blueprint dropdown is configured by finding the blueprint object that matches the selected survey's blueprint UUID in the available blueprints collection, then setting this as the current selection.

The resources grid is cleared of any previous data, then repopulated by iterating through each resource entry in the survey's resources collection. For every resource, a new row is added to the grid with its name displayed in the first column, purity level in the second, and quantity amount in the third column.

### Scanner Blueprint Filter Refresh

When the scanner blueprint filter textbox content changes (such as when the user types), the following process occurs:

The current search text from the filter textbox is retrieved. A new list of blueprints is created by filtering the complete blueprint collection to only include items of the SystemObjectScanner blueprint type. If a search term exists, additional filtering is applied to match blueprints whose extended names contain that text (using case-insensitive matching). An empty blank entry is inserted at the beginning of the filtered list to allow the user to deselect their current selection. This updated filtered list is assigned as the data source for the scanner blueprint dropdown using a binding source, which causes the dropdown to update its display with the newly filtered options and drops open.

### Clear Form Operation

When clearing the form (either after successful save or other operations), the following steps occur:

The reference to any currently selected survey is cleared by setting it to null. All text input fields are reset to empty strings, removing any previously entered planet name, survey ID, nickname, operator name, scan timestamp, sensor abundance factor, purity modifier, and scan level values. The scanner blueprint dropdown's selection is cleared back to its blank first entry, and all rows in the resources grid are removed to clear the grid display.

### Delete Operation (Pending Implementation)

When implementing delete functionality for surveys, the system should validate that a survey item is selected in the survey list view. Once validated, the system retrieves the unique identifier of the selected survey from the ListView item's Tag property. The corresponding entry is then removed from the player context's survey list collection, and the changes are persisted by writing back to the database context. After deletion completes, the form is cleared to prepare for new input.

### Cancel Operation (Pending Implementation)

When implementing cancel functionality, the system should reset the form state appropriately depending on whether data has been modified, potentially close or refocus the form, and handle the cancellation event by returning control to the previous view or closing the form entirely.

---

## Tab Sequence

The tab order follows the natural reading flow through the form fields:

| Order | Control | Description |
|-------|---------|-------------|
| 1 | `txtPlanetFilter` | Planet filter text box (left panel) |
| 2 | `cmbResource` | Resource filter combo box (left panel) |
| 3 | `dgvResources` | Resources grid (multiple cells - tabs through columns) |
| 4 | `txtPlanetName` | Planet Name input field |
| 5 | `txtSurveyID` | Survey ID input field |
| 6 | `txtNickName` | Nick Name input field |
| 7 | `txtFilterScannerBlueprint` | Scanner blueprint filter text box |
| 8 | `cmbScannerBlueprint` | Scanner blueprint combo box |
| 9 | `txtScannedBy` | Scanned By input field |
| 10 | `txtScanDateTime` | Scan DateTime input field |
| 11 | `txtSensorAbundance` | Sensor Abundance Factor input field |
| 12 | `txtPurityModifier` | Purity Modifier input field |
| 13 | `txtScanLevel` | Scan Level input field |
| 14 | `btnSave` | Save button |
| 15 | `cmdDelete` | Delete button |
| 16 | `btnCancel` | Cancel button |

**Note:** The Resources DataGridView allows tabbing through its columns (Resource → Purity → Amount), with each row starting a new cell sequence.

---

## Data Model Integration

### Survey Properties Mapped to Form Fields

| Survey Property | Form Control | Source/Target |
|-----------------|--------------|---------------|
| UUID | N/A (display only in list) | Generated automatically for new surveys |
| PlanetName | `txtPlanetName` | Input field |
| SurveyID | `txtSurveyID` | Input field |
| NickName | `txtNickName` | Input field |
| ScannedBy | `txtScannedBy` | Input field |
| DateTime | `txtScanDateTime` | Input field |
| ScannerBlueprintUUID | `cmbScannerBlueprint` (ValueMember) | Combobox selection |
| Properties.SensorAbundance | `txtSensorAbundance` | Input field |
| Properties.PurityModifier | `txtPurityModifier` | Input field |
| Properties.ScanLevel | `txtScanLevel` | Input field |
| Resources.* | `dgvResources` | Grid with multiple resources |

### Resource Data Structure

Each resource entry in the DataGridView represents:

- **Resource Name:** String (keyed by name in survey.Resources dictionary)
- **Purity:** SurveyResource.Purity property (from purity enum/set)
- **Amount:** SurveyResource.Amount property (string value)

---

## Key Methods Summary

| Method | Purpose | Behavior |
|--------|---------|----------|
| `FormSurvey()` | Constructor | Initializes controls, populates survey list and scanner blueprint filter |
| `updateScannerBlueprintList()` | Update scanner filter | Filters SystemObjectScanner blueprints based on search text |
| `populateListView(List<OE2EmpireTracker.Baseline.Survey> surveys)` | Populate survey list | Updates ListView with survey items, removes stale entries |
| `populateForm()` | Load selected survey data | Populates all form fields from currently selected Survey |
| `btnSave_Click(object sender, EventArgs e)` | Save survey | Creates new or updates existing survey, persists to context, clears form |
| `clearForm()` | Reset form state | Clears all inputs and selections |
| `cmdDelete_Click(object sender, EventArgs e)` | Delete survey | Pending implementation for removing selected survey from context |
| `btnCancel_Click(object sender, EventArgs e)` | Cancel operation | Pending implementation for handling user cancellation |
| `lvwSurveys_ItemSelectionChanged(...)` | Handle list selection change | Loads selected survey data into form fields |
| `txtFilterScannerBlueprint_TextChanged(...)` | Update scanner filter on text change | Re-regenerates filtered scanner blueprint list, drops down combo box |

---

## Event Handlers (From ResX/Designer)

| Control | Event Handler | Purpose |
|---------|---------------|---------|
| `lvwSurveys` | ItemSelectionChanged | Triggers form field population when item selected |
| `txtFilterScannerBlueprint` | TextChanged | Triggers scanner blueprint filtering on text change |
| `btnSave` | Click | Saves new or updated survey |
| `cmdDelete` | Click | Pending implementation for delete functionality |
| `btnCancel` | Click | Pending implementation for cancel functionality |

---

## UI Behavior Specifications

### Left Panel Filters

- **Planet Filter:** TextBox allows filtering the survey list by planet name (case-insensitive substring match)
- **Resource Filter:** ComboBox allows selecting a resource to filter surveys by that resource type

### ListView Behavior

- Single selection mode (MultiSelect=False)
- Full row selection for easier clicking
- Stores Survey object reference in Tag property of first column and item

### Scanner Blueprint Combo Box

- Dropdown style with suggested autocomplete
- Filter textbox enables real-time filtering as user types
- Empty entry at beginning allows deselecting a scanner
- DataSource is BindingSource for filtered list

---

## Context Requirements

The form requires:

1. **EmpireContext:** Provides `bindingSourceResource` and `bindingSourceResourcePurity` for resource type data
2. **PlayerContext:** Provides `surveyList`, `blueprintList`, and methods like `findBlueprint()` and `writeContext()`

All survey operations (save, delete) persist to player context and write back to the application database via `playerContext.writeContext()`.

---

## Placeholder Implementations

The following methods are implemented as empty placeholders and require implementation:

1. `cmdDelete_Click` - Delete selected survey from context
2. `btnCancel_Click` - Reset form state and handle cancellation

These methods should be implemented to complete the CRUD operations for surveys.
