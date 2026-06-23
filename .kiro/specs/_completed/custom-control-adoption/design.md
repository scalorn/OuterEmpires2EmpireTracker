# Design Document: Custom Control Adoption

## Overview

Three custom controls — `FilteredTextComboSet`, `DataGridViewFilteredComboBoxColumn`, and `DataEntryGridView` — were built for the ship forms (FormShipTemplate, FormShipInstance). This spec adopts them across all remaining forms that still use the manual patterns they replace.

The goal is consistent UX (inline filtering for large dropdowns, Tab-navigation in editable grids) and reduced code duplication. Each adoption site is a mechanical replacement: swap the control type in the Designer.cs, rewire events in the code-behind, and remove the now-redundant manual filter logic.

### Key Design Decisions

1. **FilteredTextComboSet replaces TextBox+ComboBox pairs** — Anywhere a ValidatedTextBox filters a ComboBox via TextChanged → repopulate, the two controls are consolidated into a single FilteredTextComboSet. The filter TextBox and label are removed from the Designer; the combo declaration changes type. The code-behind drops the TextChanged handler and uses `SetItems()` / `SelectedItemChanged` instead.

2. **DataGridViewFilteredComboBoxColumn replaces DataGridViewComboBoxColumn** — Only where the dropdown has many items (50+ resources, blueprints). Small fixed-set combos (Purity with 5 items, StageType with 3 items) stay as standard DataGridViewComboBoxColumn.

3. **DataEntryGridView replaces DataGridView** — Only in grids with a mix of read-only and editable columns where Tab navigation matters. Read-only display grids (load lists, transaction logs, summary tables) stay as standard DataGridView.

4. **No behavioral changes** — Every adoption is a pure control swap. The data model, event flow, and business logic remain identical. The only user-visible change is the appearance of the inline filter TextBox when editing.

5. **Designer.cs hand-editing required** — WinForms Designer files must be edited manually to change control types. The Designer does not support custom control type changes through the visual editor.

## Architecture

The three custom controls form a layered hierarchy:

```
FilteredTextComboSet (UserControl)
├── TextBox (filter input, hidden until focused)
└── ComboBox (filtered item list)

DataGridViewFilteredComboBoxColumn (DataGridViewColumn)
├── DataGridViewFilteredComboBoxCell (cell type)
└── DataGridViewFilteredComboBoxEditingControl : FilteredTextComboSet
    └── Implements IDataGridViewEditingControl

DataEntryGridView : DataGridView
└── Overrides ProcessDialogKey / ProcessDataGridViewKey for Tab navigation
```

No new controls are created by this spec. The existing controls are adopted as-is.

### Adoption Pattern

Every adoption site follows the same mechanical pattern:

**Designer.cs changes:**
1. Change the control type from `System.Windows.Forms.ComboBox` to `OE2EmpireTracker.Controls.FilteredTextComboSet` (or equivalent for grid columns / DataGridView)
2. Remove the filter TextBox declaration and its label
3. Remove the filter TextBox from the parent FlowLayoutPanel's `Controls.Add()` calls
4. Remove sizing/positioning for the removed controls

**Code-behind changes:**
1. Remove the `txtFilter.TextChanged += ...` event subscription
2. Remove the `TxtFilter_TextChanged` handler method
3. Remove the manual filter/repopulate logic (the `PopulateXxxCombo` method that rebuilds the ComboBox items)
4. Replace with `cmbXxx.SetItems(itemList, currentValue)` calls
5. Replace `cmbXxx.SelectedIndexChanged` with `cmbXxx.SelectedItemChanged`
6. Replace `cmbXxx.SelectedItem` / `cmbXxx.SelectedValue` reads with `cmbXxx.SelectedItem` / `cmbXxx.SelectedFullIndex`
7. Preserve all `_isProgrammaticUpdate` guards

## Candidate Site Analysis

### FilteredTextComboSet Candidates

Each site below has a ValidatedTextBox whose TextChanged handler repopulates a ComboBox with filtered items. All are candidates for replacement with a single FilteredTextComboSet.

| # | Form / Control | Filter TextBox | ComboBox | Item Count | Decision |
|---|---------------|---------------|----------|-----------|----------|
| 1 | ColonyStructureV2 — Survey picker | `txtSurveyFilter` | `cmbSurvey` | ~20-100 surveys | **ADOPT** — same pattern as ship hull picker |
| 2 | ColonyStructureV2 — Selection picker | `txtSelectionFilter` | `cmbSelection` | ~50-200 resources/blueprints | **ADOPT** — large item list benefits from filtering |
| 3 | FormSurvey — Scanner blueprint picker | `txtFilterScannerBlueprint` | `cmbScannerBlueprint` | ~100+ scanner blueprints | **ADOPT** — large list, exact same pattern |
| 4 | FormDeliveryRoute — Drop-off item picker | `txtDropFilter` | `cmbDropItem` | ~50-200 items | **ADOPT** — large list, filters by type then name |
| 5 | FormDeliveryRoute — Pick-up item picker | `txtPickFilter` | `cmbPickItem` | ~50-200 items | **ADOPT** — same as drop-off |
| 6 | FormColonyV2 — Flatpack picker | `txtFilterFlatpack` | `cmbFlatpacks` | ~50-100 flatpack blueprints | **ADOPT** — large list, exact same pattern |
| 7 | FormColonyV2 — Item picker | `txtItemFilter` | `cmbItem` | ~50-200 items (varies by type) | **ADOPT** — large list, filters by type then name |
| 8 | FormColonyV2 — Overflow resource picker | `txtOverflowResourceFilter` | `cmbOverflowResource` | ~50+ resources | **ADOPT** — large list |
| 9 | FormColonyV2 — Overflow destination picker | `txtOverflowDestFilter` | `cmbOverflowDest` | ~10-50 colonies/stations | **ADOPT** — moderate list, consistent UX |
| 10 | FormColonyV2 — Overflow route picker | `txtOverflowRouteFilter` | `cmbOverflowRoute` | ~5-20 routes | **ADOPT** — small list but consistent UX |
| 11 | FormBlueprintV2 — Base blueprint picker | `txtFilterBaseBlueprint` | `cmbBaseBlueprint` | ~100+ blueprints | **ADOPT** — large list, exact same pattern |
| 12 | FormStockTargets — Stock plan entry picker | `txtEntryFilter` | `cmbEntry` | ~5-20 stock plans | **ADOPT** — small list but consistent UX |

### DataGridViewFilteredComboBoxColumn Candidates

Each site below has a DataGridViewComboBoxColumn with a large item list. Candidates for replacement with DataGridViewFilteredComboBoxColumn.

| # | Form | Grid | Column | Item Count | Decision |
|---|------|------|--------|-----------|----------|
| 1 | FormSurvey | `dgvResources` | `Resource` | ~50+ resources | **ADOPT** — large list, hard to find items without filtering |
| 2 | FormSurvey | `dgvResources` | `Purity` | 5 purities | **SKIP** — too few items, filtering adds no value |
| 3 | FormBlueprintV2 | `dgvResources` | `colResource` | ~50+ resources | **ADOPT** — large list, same as survey |

### DataEntryGridView Candidates

Each site below has a DataGridView with a mix of read-only and editable columns. Candidates for replacement with DataEntryGridView for Tab navigation.

| # | Form | Grid | Editable Columns | Read-Only Columns | Decision |
|---|------|------|-------------------|-------------------|----------|
| 1 | FormSurvey | `dgvResources` | Resource, Purity, Amount | (none) | **ADOPT** — all editable, Tab navigation improves data entry |
| 2 | FormColonyV2 | `dgvCommodityRequests` | Amount, NeedBy, Fulfilled | Name, Type | **ADOPT** — mix of RO and editable |
| 3 | FormColonyV2 | `dgvItems` | Quantity | Type, Name, Purity | **ADOPT** — mostly RO with one editable column |
| 4 | FormDeliveryRoute | `dgvDropOff` | Type, Name, Purity, Qty | (none) | **ADOPT** — all editable, Tab navigation improves data entry |
| 5 | FormDeliveryRoute | `dgvPickUp` | Type, Name, Purity, Qty | (none) | **ADOPT** — same as drop-off |
| 6 | FormBlueprintV2 | `dgvResources` | Resource, Amount | (none) | **ADOPT** — all editable, Tab navigation improves data entry |
| 7 | FormShipTemplate | `dgvSlots` | Component | SlotType, SlotIndex | **ALREADY DONE** — already uses DataEntryGridView? Check. |
| 8 | FormPricingPlan | `dgvResourcePrices` | Price | Resource, Purity | **ADOPT** — mostly RO with one editable column |
| 9 | FormStockTargets | `dgvTargets` | Qty, Location | Type, Item | **ADOPT** — mix of RO and editable |
| 10 | FormStation | `dgvHold` | (display only) | All | **SKIP** — read-only display grid |
| 11 | FormStation | `dgvComponents` | (display only) | All | **SKIP** — read-only display grid |
| 12 | FormStation | `dgvMunitions` | (display only) | All | **SKIP** — read-only display grid |
| 13 | FormShipInstance | `dgvComponents` | (display only) | All | **SKIP** — read-only display grid |
| 14 | FormShipInstance | `dgvCargo` | (display only) | All | **SKIP** — read-only display grid |
| 15 | FormDeliveryExecution | `dgvLoadList` | (display only) | All | **SKIP** — read-only display grid |
| 16 | FormMarket | `dgvListings`, `dgvTransactions`, `dgvSummary` | (display only) | All | **SKIP** — read-only display grids |
| 17 | FormSupplyChain | `dgvStages` | (display only) | All | **SKIP** — read-only display grid |
| 18 | FormColonyV2 | `dgvOverflowRules` | (display only) | All | **SKIP** — read-only display grid |
| 19 | FormStockTargets | `dgvExpandedComponents` | (display only) | All | **SKIP** — read-only display grid |
| 20 | FormStockTargets | `dgvEntries` | (display only) | All | **SKIP** — read-only display grid |

### Sites NOT Adopted (and why)

These ComboBox controls were evaluated and intentionally excluded:

| Form | Control | Reason |
|------|---------|--------|
| FormSurvey | `cmbResource`, `cmbSurveyType`, `cmbPurityFilter`, `cmbSurveyTypeEdit` | Small fixed-set filter combos (resource types, purity levels, survey types). 5-10 items — filtering adds no value. |
| FormSupplyChain | `cmbStageType`, `cmbLocationType`, `cmbPurity` | Small fixed-set combos (3-5 items each). |
| FormSupplyChain | `cmbLocation`, `cmbResource`, `cmbRoute` | These are populated dynamically but don't have a paired filter TextBox — they use a different selection pattern (type-then-item cascade). Could be adopted in a future pass but are not part of the current manual filter pattern. |
| FormStockTargets | `cmbTargetType`, `cmbScope`, `cmbReplenishmentPlan` | Small fixed-set combos or short lists. |
| FormStockTargets | `cmbTargetItem`, `cmbTargetLocation` | Populated dynamically via type cascade, no paired filter TextBox. |
| FormStation | `cmbStationType`, `cmbOwnership`, `cmbHoldType`, `cmbHoldPurity` | Small fixed-set combos (2-5 items). |
| FormStation | `cmbHoldItem`, `cmbMunItem`, `cmbStationBlueprint` | No paired filter TextBox. `cmbStationBlueprint` could benefit from filtering (large blueprint list) but uses a different population pattern (BindingSource with DisplayMember/ValueMember). Candidate for a future enhancement to FilteredTextComboSet to support object binding. |
| FormShipInstance | `cmbLocationType`, `cmbLocationUUID`, `cmbAddType`, `cmbAddPurity` | Small fixed-set combos or type-cascade combos. |
| FormShipInstance | `cmbAddItem` | No paired filter TextBox. Same future-enhancement candidate as cmbStationBlueprint. |
| FormMarket | `cmbTxType`, `cmbTxStation`, `cmbSumStation`, `cmbPricingPlan` | Small fixed-set combos or short station lists. |
| FormPlayerProfile | `cmbResource`, `cmbFaction` | Small fixed-set combos. |
| FormColonyV2 | `cmbOverflowPurity`, `cmbOverflowDestType`, `cmbItemType`, `cmbPurity` | Small fixed-set combos (3-5 items). |
| FormDeliveryRoute | `cmbDropItemType`, `cmbPickItemType`, `cmbDropPurity`, `cmbPickPurity` | Small fixed-set type/purity combos. |
| FormBlueprintV2 | `cmbFilterType`, `cmbFilterClass`, `cmbFilterTechLevel`, `cmbEvolution` | Small fixed-set filter combos in the filter bar. |
| FormColonyActivity | `txtFilter` | This is a text filter for the activity grid rows, not a TextBox+ComboBox pair. Different pattern — not a candidate. |
| FormBuildPlanner | All combos | No paired filter TextBoxes. The form uses a different pattern (cascading type selectors). Future candidate. |
| FormContacts | All combos | No paired filter TextBoxes. Small lists. |
| FormAsteroid | `txtFilter` | List filter for the asteroid ListView, not a TextBox+ComboBox pair. |

## Detailed Conversion Patterns

### Pattern A: FilteredTextComboSet replacing TextBox + ComboBox

**Before (Designer.cs):**
```csharp
this.txtFilterFoo = new OE2EmpireTracker.Controls.ValidatedTextBox();
this.cmbFoo = new System.Windows.Forms.ComboBox();
// ...
this.flpFoo.Controls.Add(this.lblFoo);
this.flpFoo.Controls.Add(this.txtFilterFoo);
this.flpFoo.Controls.Add(this.cmbFoo);
// ...
private OE2EmpireTracker.Controls.ValidatedTextBox txtFilterFoo;
private System.Windows.Forms.ComboBox cmbFoo;
```

**After (Designer.cs):**
```csharp
this.cmbFoo = new OE2EmpireTracker.Controls.FilteredTextComboSet();
// ...
this.flpFoo.Controls.Add(this.lblFoo);
this.flpFoo.Controls.Add(this.cmbFoo);
// ...
private OE2EmpireTracker.Controls.FilteredTextComboSet cmbFoo;
```

**Before (code-behind):**
```csharp
txtFilterFoo.TextChanged += TxtFilterFoo_TextChanged;
cmbFoo.SelectedIndexChanged += CmbFoo_SelectedIndexChanged;

private void TxtFilterFoo_TextChanged(object sender, EventArgs e)
{
    PopulateFooCombo();
    cmbFoo.DroppedDown = true;
}

private void PopulateFooCombo()
{
    string searchText = txtFilterFoo.Text;
    var filteredList = allItems.Where(i => i.Name.IndexOf(searchText, ...) >= 0).ToList();
    cmbFoo.DataSource = null;
    cmbFoo.DataSource = filteredList;
}
```

**After (code-behind):**
```csharp
cmbFoo.SelectedItemChanged += CmbFoo_SelectedItemChanged;

private void PopulateFooCombo()
{
    var items = allItems.Select(i => i.Name).ToList();
    string currentValue = /* current selection */;
    cmbFoo.SetItems(items, currentValue);
}

private void CmbFoo_SelectedItemChanged(object sender, EventArgs e)
{
    if (_isProgrammaticUpdate > 0) return;
    string selected = cmbFoo.SelectedItem;
    // ... write through to data model
}
```

### Pattern B: DataGridViewFilteredComboBoxColumn replacing DataGridViewComboBoxColumn

**Before (Designer.cs):**
```csharp
this.colResource = new System.Windows.Forms.DataGridViewComboBoxColumn();
// ...
private System.Windows.Forms.DataGridViewComboBoxColumn colResource;
```

**After (Designer.cs):**
```csharp
this.colResource = new OE2EmpireTracker.Controls.DataGridViewFilteredComboBoxColumn();
// ...
private OE2EmpireTracker.Controls.DataGridViewFilteredComboBoxColumn colResource;
```

**Code-behind change:**
```csharp
// Before: populate via DataSource or Items collection
colResource.DataSource = resourceNames;

// After: set Items property (List<string>)
colResource.Items = resourceNames;
```

### Pattern C: DataEntryGridView replacing DataGridView

**Before (Designer.cs):**
```csharp
this.dgvResources = new System.Windows.Forms.DataGridView();
// ...
private System.Windows.Forms.DataGridView dgvResources;
```

**After (Designer.cs):**
```csharp
this.dgvResources = new DataEntryGridView();
// ...
private DataEntryGridView dgvResources;
```

No code-behind changes needed — DataEntryGridView is a drop-in replacement. Tab navigation works automatically.

## Data Models

No data model changes. This spec only modifies UI controls and their wiring.

## Error Handling

No new error handling. The existing DataError handlers on grids are preserved. The FilteredTextComboSet handles null/empty inputs gracefully (returns empty filtered list).

## Special Considerations

### FormDeliveryRoute Item Pickers (Requirements 4-5)

The drop-off and pick-up item pickers have a **type cascade**: `cmbDropItemType` controls which items appear in `cmbDropItem`. When the type changes, the item list is rebuilt. The FilteredTextComboSet replaces only the `txtDropFilter` + `cmbDropItem` pair — the type combo stays as a standard ComboBox. The `PopulateItemPicker` method changes from rebuilding the ComboBox DataSource to calling `cmbDropItem.SetItems(itemNames, currentValue)`.

### FormColonyV2 Item Picker (Requirement 6)

Same type-cascade pattern as delivery route: `cmbItemType` controls which items appear in `cmbItem`. The FilteredTextComboSet replaces `txtItemFilter` + `cmbItem`. The six `PopulateXxxItems` methods (Resource, Commodity, Worker, Survey, Blueprint, Output) each call `cmbItem.SetItems()` with the appropriate filtered list.

### FormColonyV2 Flatpack Picker (Requirement 6)

The flatpack picker currently uses `BindingSource` with `DisplayMember`/`ValueMember` for object binding. FilteredTextComboSet works with plain string lists. The conversion needs to:
1. Build a `List<string>` of flatpack ExtendedNames
2. Maintain a parallel `List<string>` of UUIDs (or use `SelectedFullIndex` to look up the UUID from the original list)
3. Replace the BindingSource pattern with `SetItems(names, currentName)`

### FormBlueprintV2 Base Blueprint Picker (Requirement 11 area)

Same BindingSource pattern as the flatpack picker. The conversion maintains a parallel list of Blueprint objects indexed to match the string items, using `SelectedFullIndex` to resolve the selected Blueprint.

### ColonyStructureV2 Survey and Selection Pickers (Requirements 1-2)

These are inside a UserControl (ColonyStructureV2), not a Form. The same pattern applies — the UserControl's Designer.cs and code-behind are modified identically. The UserControl does not implement IProgrammaticUpdateSource directly; it delegates to its parent form's guard via a passed-in reference.

## Correctness Properties

### Property 1: FilteredTextComboSet selection is equivalent to old pattern

*For any* item list and filter text, the item selected via FilteredTextComboSet.SelectedItem SHALL be the same item that would have been selected via the old TextBox+ComboBox pattern with the same filter text and click.

**Validates: Requirements 1-6, 11-12**

### Property 2: DataEntryGridView Tab navigation skips read-only columns

*For any* DataEntryGridView with a mix of read-only and editable columns, pressing Tab SHALL advance to the next editable cell, never stopping on a read-only cell.

**Validates: Requirements 9-12**

## Testing Strategy

### Manual Testing

Each converted form requires manual verification:
1. Open the form, verify the FilteredTextComboSet appears correctly
2. Type a filter string, verify the combo filters correctly
3. Select an item, verify it writes through to the data model
4. Save and reload, verify the selection persists
5. For grids: Tab through editable cells, verify navigation skips read-only columns

### Automated Testing

The existing FilteredTextComboSet and DataGridViewFilteredComboBoxColumn tests (in `OE2EmpireTracker.Tests/Controls/`) validate the control behavior. No new tests are needed for the adoption — the controls are already tested. The adoption is a wiring change, not a logic change.

### Build Verification

After each form conversion:
1. Build the solution — verify no compile errors
2. Run all tests — verify no regressions
3. Run audit — verify no new findings (mockup-controls check will need mockup updates)
