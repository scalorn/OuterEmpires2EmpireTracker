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

| Control | Type | Position | Size | Properties |
|---------|------|----------|------|------------|
| `lblPlanetFilter` | Label | (2, 4) | 100×17 px | Text: "Planet", RightAlign, Anchor=Left\|Right |
| `txtPlanetFilter` | TextBox | (107, 3) | 202×20 px | TabIndex=0, Location relative to flow panel |

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

### Initialization (`FormSurvey` Constructor)

```csharp
public FormSurvey()
{
    InitializeComponent();
    empireContext = EmpireContext.getInstance();
    playerContext = EmpireContext.PlayerContext;
    
    // Initialize scanner blueprint combo box
    cmbScannerBlueprint.DisplayMember = "ExtendedName";
    cmbScannerBlueprint.ValueMember = "UUID";
    updateScannerBlueprintList();
    cmbScannerBlueprint.SelectedIndex = -1;
    
    // Set up survey list view with columns
    lvwSurveys.View = View.Details;
    lvwSurveys.Columns.Add("UUID", 0);
    lvwSurveys.Columns.Add("PlanetName", 100);
    lvwSurveys.Columns.Add("NickName", 100);
    lvwSurveys.Columns.Add("DateTime", 100);
    populateListView(new List<OE2EmpireTracker.Baseline.Survey>(playerContext.surveyList));
    
    // Configure resource data grid with combo box columns
    ...
}
```

### Survey List Population (`populateListView`)

- Creates dictionary keyed by UUID for duplicate detection
- Updates ListView items when surveys are loaded from player context
- Removes ListView items when corresponding surveys are deleted/filtered out
- Tag property maintains reference to Survey object for editing operations

### Save Behavior (`btnSave_Click`)

```csharp
private void btnSave_Click(object sender, EventArgs e)
{
    // Create new survey or update existing one
    if (selectedSurvey != null)
    {
        // Update existing survey using current data
        ...
    }
    else
    {
        // Create new survey with generated UUID
        Guid myUuid = Guid.NewGuid();
        survey.UUID = myUuid.ToString();
        ...
    }
    
    // Populate survey with form field values
    survey.PlanetName = txtPlanetName.Text;
    survey.SurveyID = txtSurveyID.Text;
    survey.ScannedBy = txtScannedBy.Text;
    survey.DateTime = txtScanDateTime.Text;
    survey.Properties["SensorAbundance"] = txtSensorAbundance.Text;
    survey.Properties["PurityModifier"] = txtPurityModifier.Text;
    survey.Properties["ScanLevel"] = txtScanLevel.Text;
    
    // Get scanner blueprint UUID if selected
    ...
    
    // Map resources from grid to survey.Resources collection
    foreach (DataGridViewRow row in dgvResources.Rows)
    {
        string resourceName = row.Cells[0].Value as string;
        string resourcePurity = row.Cells[1].Value as string;
        string resourceAmount = row.Cells[2].Value as string;
        SurveyResource surveyResource = new SurveyResource();
        surveyResource.Purity = resourcePurity;
        surveyResource.Amount = resourceAmount;
        if (resourceName != null)
        {
            survey.Resources[resourceName] = surveyResource;
        }
    }
    
    // Save to player context and persist
    playerContext.surveyList.Add(survey); // or update existing
    playerContext.writeContext();
    
    // Clear form after saving
    clearForm();
}
```

### Form Loading Behavior (`lvwSurveys_ItemSelectionChanged`)

```csharp
private void lvwSurveys_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
{
    if (lvwSurveys.SelectedItems.Count == 1)
    {
        selectedSurvey = lvwSurveys.SelectedItems[0].SubItems[0].Tag as OE2EmpireTracker.Baseline.Survey;
        populateForm();
    }
}

private void populateForm()
{
    // Populate all form fields from selected survey data
    txtPlanetName.Text = selectedSurvey.PlanetName;
    txtSurveyID.Text = selectedSurvey.SurveyID;
    txtNickName.Text = selectedSurvey.NickName;
    txtScannedBy.Text = selectedSurvey.ScannedBy;
    txtScanDateTime.Text = selectedSurvey.DateTime;
    txtSensorAbundance.Text = selectedSurvey.Properties["SensorAbundance"];
    txtPurityModifier.Text = selectedSurvey.Properties["PurityModifier"];
    txtScanLevel.Text = selectedSurvey.Properties["ScanLevel"];
    
    // Load scanner blueprint into combo box
    cmbScannerBlueprint.SelectedItem = playerContext.findBlueprint(selectedSurvey.ScannerBlueprintUUID);
    
    // Populate resources grid with survey's resource data
    dgvResources.Rows.Clear();
    foreach (KeyValuePair<string, SurveyResource> resource in selectedSurvey.Resources)
    {
        dgvResources.Rows.Add();
        DataGridViewRow row = dgvResources.Rows[dgvResources.RowCount - 2];
        row.Cells[0].Value = resource.Key;
        row.Cells[1].Value = resource.Value.Purity;
        row.Cells[2].Value = resource.Value.Amount;
    }
}
```

### Scanner Blueprint Filtering (`updateScannerBlueprintList`)

```csharp
private void updateScannerBlueprintList()
{
    string searchText = txtFilterScannerBlueprint.Text;
    List<Blueprint> filteredList = new List<Blueprint>(playerContext.blueprintList);
    
    // Filter to SystemObjectScanner blueprint type only
    BlueprintType scanners = empireContext.findBlueprintType("SystemObjectScanner");
    filteredList = filteredList
        .Where(item => item.BluePrintType == scanners.Id)
        .ToList();
    
    // Apply text filter if specified (case-insensitive on ExtendedName)
    if (!string.IsNullOrEmpty(searchText))
    {
        filteredList = filteredList
            .Where(item => item.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();
    }
    
    // Add empty entry to allow deselecting
    filteredList.Insert(0, new Blueprint());
    
    cmbScannerBlueprint.DataSource = new BindingSource(filteredList, null);
}
```

### Clear Form Behavior (`clearForm`)

```csharp
private void clearForm()
{
    selectedSurvey = null;
    
    // Reset all text fields
    txtPlanetName.Text = "";
    txtSurveyID.Text = "";
    txtNickName.Text = "";
    txtScannedBy.Text = "";
    txtScanDateTime.Text = "";
    txtSensorAbundance.Text = "";
    txtPurityModifier.Text = "";
    txtScanLevel.Text = "";
    
    // Reset combo box and grid
    cmbScannerBlueprint.SelectedItem = null;
    dgvResources.Rows.Clear();
}
```

### Delete Behavior (`cmdDelete_Click`)

Currently implemented as placeholder (empty method). Should implement:
- Validate that an item is selected in lvwSurveys
- Get selected survey's UUID from ListViewItem.Tag
- Remove from playerContext.surveyList collection
- Call playerContext.writeContext() to persist changes
- Clear the form after deletion

### Cancel Behavior (`btnCancel_Click`)

Currently implemented as placeholder (empty method). Should implement:
- Reset form state appropriately
- Close or reset focus
- Potentially close the form or return to previous view

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
| `cmdDelete_Click(object sender, EventArgs e)` | Delete survey | Placeholder - not yet implemented |
| `btnCancel_Click(object sender, EventArgs e)` | Cancel operation | Placeholder - not yet implemented |
| `lvwSurveys_ItemSelectionChanged(...)` | Handle list selection change | Loads selected survey data into form fields |
| `txtFilterScannerBlueprint_TextChanged(...)` | Update scanner filter on text change | Re-regenerates filtered scanner blueprint list, drops down combo box |

---

## Event Handlers (From ResX/Designer)

| Control | Event Handler | Purpose |
|---------|---------------|---------|
| `lvwSurveys` | ItemSelectionChanged | Triggers form field population when item selected |
| `txtFilterScannerBlueprint` | TextChanged | Triggers scanner blueprint filtering on text change |
| `btnSave` | Click | Saves new or updated survey |
| `cmdDelete` | Click | Placeholder for delete functionality |
| `btnCancel` | Click | Placeholder for cancel functionality |

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