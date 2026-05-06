# Design Document: Filtered Combo Adoption

## Overview

This design covers enhancing the `FilteredTextComboSet` control to support parallel value lists (display string + UUID pairs), then converting nine ComboBoxes across four forms to use the enhanced control. The enhancement enables ComboBoxes that previously used `BindingSource` with `DisplayMember`/`ValueMember` to be converted without losing value retrieval capability.

The work divides into two layers:
1. **Control enhancement** - Add a `_fullValues` list, new `SetItems` overloads, and a `SelectedValue` property to `FilteredTextComboSet`.
2. **Form conversions** - Change Designer declarations from `System.Windows.Forms.ComboBox` to `OE2EmpireTracker.Controls.FilteredTextComboSet`, replace `DataSource`/`BindingSource` patterns with `SetItems` calls, and wire `SelectedItemChanged` event handlers.

## Architecture

### Component Relationships

FilteredTextComboSet is the central control enhanced with value list support:
- Fields: `_fullItems`, `_fullValues` (new, nullable), `_filteredIndexMap`
- Methods: `SetItems(items, currentValue)`, `SetItems(items, values, currentValue)` (new)
- Properties: `SelectedFullIndex`, `SelectedItem`, `SelectedValue` (new)
- Event: `SelectedItemChanged`

Dependent forms:
- **FormStation** uses: cmbStationBlueprint (value list), cmbHoldItem (plain), cmbMunItem (plain)
- **FormShipInstance** uses: cmbAddItem (plain)
- **FormSupplyChain** uses: cmbLocation (value list), cmbResource (plain), cmbRoute (value list)
- **FormBuildPlanner** uses: cmbResource (plain), cmbSurvey (value list)

### Type-Cascade Pattern (Preserved)

The type-cascade pattern is preserved unchanged. A standard ComboBox (the type selector) fires `SelectedIndexChanged`, which calls `SetItems` on the dependent FilteredTextComboSet with the new item list. The type selector is NOT converted because its item lists are small (5-6 items).

Flow:
1. User selects type in ComboBox (e.g. cmbHoldType = "Resource")
2. Form handles SelectedIndexChanged
3. Form calls PopulateXxxCombo() which builds a list and calls SetItems on the FilteredTextComboSet
4. FilteredTextComboSet clears filter, displays full new list
5. User types in filter to narrow down, selects item
6. Form reads SelectedItem or SelectedValue

## Components and Interfaces

### FilteredTextComboSet Enhancement

#### New Private Field

```csharp
private List<string> _fullValues = null;
```

When `null`, the control operates in plain-string mode (backward compatible). When non-null, it holds the parallel value list.

#### New Public Property

```csharp
/// <summary>
/// Gets the value string corresponding to the currently selected item from the
/// parallel value list, or null if nothing is selected or no value list was provided.
/// </summary>
public string SelectedValue
{
    get
    {
        if (_fullValues == null) return null;
        int fullIdx = SelectedFullIndex;
        if (fullIdx < 0 || fullIdx >= _fullValues.Count) return null;
        return _fullValues[fullIdx];
    }
}
```

#### New SetItems Overload (display + values + currentValue)

```csharp
/// <summary>
/// Sets the item list with a parallel value list and optionally pre-selects a value.
/// The currentValue is matched against the value list to pre-select the corresponding item.
/// </summary>
public void SetItems(List<string> items, List<string> values, string currentValue)
{
    _fullItems = items ?? new List<string>();
    _fullValues = values;

    if (IsEditing)
    {
        string previousValue = SelectedValue ?? CmbItems.SelectedItem?.ToString();
        string restoreValue = currentValue ?? previousValue;
        SuppressSelectionEvent = true;
        RebuildFilteredList();
        if (!string.IsNullOrEmpty(restoreValue) && _fullValues != null)
        {
            int valueIdx = _fullValues.IndexOf(restoreValue);
            if (valueIdx >= 0)
            {
                int filteredIdx = _filteredIndexMap.IndexOf(valueIdx);
                if (filteredIdx >= 0) CmbItems.SelectedIndex = filteredIdx;
            }
        }

        SuppressSelectionEvent = false;
        return;
    }

    _suppressFilterEvent = true;
    SuppressSelectionEvent = true;
    TxtFilter.Text = string.Empty;
    _suppressFilterEvent = false;
    RebuildFilteredList();
    if (!string.IsNullOrEmpty(currentValue) && _fullValues != null)
    {
        int valueIdx = _fullValues.IndexOf(currentValue);
        if (valueIdx >= 0)
        {
            int filteredIdx = _filteredIndexMap.IndexOf(valueIdx);
            if (filteredIdx >= 0) CmbItems.SelectedIndex = filteredIdx;
        }
    }

    SuppressSelectionEvent = false;
}
```

#### Modified Existing SetItems (backward compatible)

The existing `SetItems(List<string> items, string currentValue)` method adds one line to clear the value list:

```csharp
public void SetItems(List<string> items, string currentValue)
{
    _fullValues = null;  // Added: clear value list for plain-string mode
    _fullItems = items ?? new List<string>();
    // ... rest unchanged
}
```

### Per-Form Conversion Approach

Each form conversion follows the same pattern:

#### Designer.cs Changes

1. Replace control declaration type:
```csharp
// Before:
private System.Windows.Forms.ComboBox cmbStationBlueprint;
// After:
private OE2EmpireTracker.Controls.FilteredTextComboSet cmbStationBlueprint;
```

2. Replace instantiation:
```csharp
// Before:
this.cmbStationBlueprint = new System.Windows.Forms.ComboBox();
// After:
this.cmbStationBlueprint = new OE2EmpireTracker.Controls.FilteredTextComboSet();
```

3. Remove `DropDownStyle` assignment (FilteredTextComboSet manages its own ComboBox internally).

#### Code-Behind Changes (Value List Pattern)

For controls needing UUID values (cmbStationBlueprint, cmbLocation, cmbRoute, cmbSurvey):

```csharp
// Before (BindingSource pattern):
cmbStationBlueprint.DataSource = null;
cmbStationBlueprint.Items.Clear();
var items = new List<KeyValuePair<string, string>>();
foreach (var bp in blueprints)
    items.Add(new KeyValuePair<string, string>(bp.UUID, bp.ExtendedName));
cmbStationBlueprint.DataSource = items;
cmbStationBlueprint.DisplayMember = "Value";
cmbStationBlueprint.ValueMember = "Key";
cmbStationBlueprint.SelectedValue = _viewModel.StationBlueprintUUID;

// After (SetItems with value list):
var displayNames = new List<string>();
var valueUUIDs = new List<string>();
foreach (var bp in blueprints)
{
    displayNames.Add(bp.ExtendedName);
    valueUUIDs.Add(bp.UUID);
}
cmbStationBlueprint.SetItems(displayNames, valueUUIDs, _viewModel.StationBlueprintUUID);
```

Reading the selected value:
```csharp
// Before:
string uuid = cmbStationBlueprint.SelectedValue?.ToString() ?? string.Empty;
// After:
string uuid = cmbStationBlueprint.SelectedValue ?? string.Empty;
```

#### Code-Behind Changes (Plain String Pattern)

For controls using display names as values (cmbHoldItem, cmbMunItem, cmbAddItem, cmbResource):

```csharp
// Before (Items.Add pattern):
cmbHoldItem.Items.Clear();
foreach (var r in resources.OrderBy(r => r.Name))
    cmbHoldItem.Items.Add(r.Name);
if (cmbHoldItem.Items.Count > 0) cmbHoldItem.SelectedIndex = 0;

// After (SetItems pattern):
var names = new List<string>();
foreach (var r in resources.OrderBy(r => r.Name))
    names.Add(r.Name);
cmbHoldItem.SetItems(names, string.Empty);
```

Reading the selected item (unchanged - SelectedItem property already exists):
```csharp
string itemName = cmbHoldItem.SelectedItem ?? string.Empty;
```

#### Event Handler Changes

Replace `SelectedIndexChanged` with `SelectedItemChanged`:
```csharp
// Before:
cmbStationBlueprint.SelectedIndexChanged += CmbStationBlueprint_SelectedIndexChanged;
// After:
cmbStationBlueprint.SelectedItemChanged += CmbStationBlueprint_SelectedItemChanged;
```

Handler body pattern:
```csharp
private void CmbStationBlueprint_SelectedItemChanged(object sender, EventArgs e)
{
    if (_isProgrammaticUpdate > 0) return;
    string uuid = cmbStationBlueprint.SelectedValue ?? string.Empty;
    _viewModel.StationBlueprintUUID = uuid;
    // ... rest of handler logic
}
```

### FormBuildPlanner - Eliminating _itemPickerIDs

FormBuildPlanner currently maintains a manual `_itemPickerIDs` parallel list and uses `SelectedFullIndex` to look up values. With the new overload, this is replaced:

```csharp
// Before:
_itemPickerIDs = new List<string>();
var names = new List<string>();
foreach (var bp in blueprints) { names.Add(bp.ExtendedName); _itemPickerIDs.Add(bp.UUID); }
cmbItem.SetItems(names, string.Empty);
// Reading:
int selectedIdx = cmbItem.SelectedFullIndex;
string selectedID = (selectedIdx >= 0 && selectedIdx < _itemPickerIDs.Count)
    ? _itemPickerIDs[selectedIdx] : null;

// After:
var names = new List<string>();
var ids = new List<string>();
foreach (var bp in blueprints) { names.Add(bp.ExtendedName); ids.Add(bp.UUID); }
cmbItem.SetItems(names, ids, string.Empty);
// Reading:
string selectedID = cmbItem.SelectedValue;
string selectedDisplay = cmbItem.SelectedItem;
```

The `_itemPickerIDs` field is removed entirely.

### FormBuildPlanner - cmbSurvey Conversion

```csharp
// Before (ItemEntry objects in standard ComboBox):
cmbSurvey.Items.Clear();
cmbSurvey.Items.Add(new ItemEntry { Display = "(none)", ID = string.Empty });
foreach (var s in CollectionSortHelper.OrderSurveys(surveys))
    cmbSurvey.Items.Add(new ItemEntry { Display = s.Name, ID = s.UUID });
cmbSurvey.SelectedIndex = 0;

// After (FilteredTextComboSet with value list):
var displayNames = new List<string> { "(none)" };
var valueUUIDs = new List<string> { string.Empty };
foreach (var s in CollectionSortHelper.OrderSurveys(surveys))
{
    displayNames.Add(s.Name);
    valueUUIDs.Add(s.UUID);
}
cmbSurvey.SetItems(displayNames, valueUUIDs, string.Empty);
```

### ClearForm Pattern

```csharp
// Plain string controls:
cmbHoldItem.SetItems(new List<string>(), string.Empty);

// Value list controls:
cmbStationBlueprint.SetItems(new List<string>(), new List<string>(), string.Empty);
```

## Data Models

No data model changes are required. The enhancement is purely at the control and form layers. The existing domain models (Blueprint, Station, Colony, Survey, DeliveryRoute, SupplyChain, BuildPlan) continue to store UUIDs as string fields.

### Control State Model

```
FilteredTextComboSet internal state:
  _fullItems:       ["Alpha Station", "Beta Dock", ...]
  _fullValues:      ["uuid-1",        "uuid-2",   ...]  <-- NEW (nullable)
  _filteredIndexMap: [0, 2, 5, ...]
  TxtFilter.Text:   "beta"
  CmbItems.Items:   ["Beta Dock"]
  CmbItems.SelectedIndex: 0

  SelectedFullIndex -> 1  (via _filteredIndexMap)
  SelectedItem      -> "Beta Dock"
  SelectedValue     -> "uuid-2" (via _fullValues)  <-- NEW
```

### Conversion Matrix

| Form | Control | Pattern | Value Source |
|------|---------|---------|--------------|
| FormStation | cmbStationBlueprint | Value list | Blueprint UUID |
| FormStation | cmbHoldItem | Plain string | Item name |
| FormStation | cmbMunItem | Plain string | Item name |
| FormShipInstance | cmbAddItem | Plain string | Item name |
| FormSupplyChain | cmbLocation | Value list | Location UUID |
| FormSupplyChain | cmbResource | Plain string | Resource name |
| FormSupplyChain | cmbRoute | Value list | Route UUID |
| FormBuildPlanner | cmbResource | Plain string | Resource name |
| FormBuildPlanner | cmbSurvey | Value list | Survey UUID |

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system - essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Value list index mapping through filter

*For any* display list, parallel value list of equal length, and any filter string, selecting a filtered item SHALL return the value from the original value list at the position corresponding to the selected display item's original index. When no value list is provided, SelectedValue SHALL always be null.

**Validates: Requirements 1.2, 1.3, 1.4, 1.5**

### Property 2: currentValue pre-selection via value list

*For any* display list, parallel value list, and any currentValue string that exists in the value list, calling SetItems SHALL result in the display item at the matching value's index being selected. If currentValue does not exist in the value list, no item SHALL be selected.

**Validates: Requirements 1.6, 2.5, 6.5, 8.5, 10.5**

### Property 3: SetItems clears filter when not editing

*For any* previous control state (items + filter text) and any new item list, calling SetItems when the control is not in edit mode SHALL result in the filter text being empty and all new items being displayed in the combo.

**Validates: Requirements 11.2**

### Property 4: SetItems preserves filter when editing

*For any* filter text and any new item list, calling SetItems while the control is in edit mode SHALL preserve the current filter text and display only those new items that match the filter.

**Validates: Requirements 11.3**

### Property 5: Event suppression during programmatic updates

*For any* item list, calling SetItems while SuppressSelectionEvent is true SHALL NOT fire the SelectedItemChanged event.

**Validates: Requirements 12.2**

## Error Handling

### Empty Lists

- When `SetItems` is called with an empty display list, the combo displays no items and `SelectedValue` returns `null`.
- When `SetItems` is called with an empty value list (but non-empty display list), the control operates in plain-string mode for that call.

### Null Values

- `_fullValues` is nullable. When `null`, `SelectedValue` always returns `null` (plain-string mode).
- Individual entries in the value list may be `null` or empty string. `SelectedValue` returns whatever is at the mapped index.
- `currentValue` parameter may be `null` or empty - in that case, no pre-selection occurs.

### Mismatched List Lengths

- If the value list length differs from the display list length, `SelectedValue` performs bounds checking via `fullIdx >= _fullValues.Count` and returns `null` for out-of-range indices. This is a defensive guard; callers should always pass equal-length lists.

### Missing Items (currentValue not found)

- If `currentValue` is not found in the value list (`IndexOf` returns -1), no item is pre-selected. The combo shows the first item or no selection depending on the list content.

### Filter Produces No Results

- When the filter text matches no items, the combo displays an empty dropdown. `SelectedValue` returns `null` because `SelectedFullIndex` returns -1.

### Type-Cascade with Active Filter

- When `SetItems` is called while the user is actively filtering (IsEditing = true), the filter text is preserved and reapplied to the new list. If the previously selected value exists in the new list, it is re-selected.

## Testing Strategy

### Property-Based Tests (FsCheck + NUnit)

Property-based tests validate the five correctness properties using FsCheck with minimum 100 iterations per property. Tests target the `FilteredTextComboSet` control directly (instantiated in test code without a form).

**Library**: FsCheck.NUnit (already in use for filtered-combo-column tests)
**Location**: `OE2EmpireTracker.Tests/Controls/FilteredTextComboSetPropertyTests.cs`
**Configuration**: `[FsCheck.NUnit.Property(MaxTest = 100)]`

Each test is tagged with:
```
// Feature: filtered-combo-adoption, Property N: <property text>
```

### Unit Tests

Unit tests cover specific examples and edge cases:
- Empty list behavior
- Null value list (plain-string mode)
- Mismatched list lengths (defensive)
- Pre-selection with currentValue not in list
- Filter producing zero results
- SetItems during edit mode preserving filter

**Location**: `OE2EmpireTracker.Tests/Controls/FilteredTextComboSetTests.cs`

### Integration Verification

Form-level behavior is verified through:
- Build compilation (getDiagnostics) confirming type changes compile
- Manual testing of type-cascade flows
- Audit tool verification (mockup-controls.js, perf-check.js, control-wiring.js)

### Test Balance

- Property tests handle comprehensive input coverage for the control enhancement (Requirement 1)
- Unit tests handle specific edge cases and error conditions
- Form conversions (Requirements 2-10) are verified by compilation and audit tools
- PERF timing (Requirement 13) is verified by perf-check.js audit
- Mockup updates (Requirement 14) are verified by mockup-controls.js audit
