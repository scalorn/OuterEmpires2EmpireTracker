# Design Document: Grid Context Menus

## Overview

This feature adds right-click context menus to all DataGridView grids across the application that have associated command-bar buttons. Each context menu mirrors the existing button actions for its grid, providing an alternative in-place access path. No new functionality is introduced.

The implementation follows the established pattern from FormBlueprintV2 (cmsStatistics, cmsResources) and FormBuildPlanner (cmsBuildItems): a ContextMenuStrip is declared in the Designer file, assigned to the grid's ContextMenuStrip property, and its ToolStripMenuItem Click events are wired to the same handler methods as the corresponding command-bar buttons.

Two cross-cutting enhancements are added beyond the existing pattern:
1. **Row selection on right-click** - the CellMouseClick event selects the row under the cursor before the context menu appears, ensuring the action targets the correct row.
2. **Opening event state management** - the ContextMenuStrip.Opening event evaluates the current selection and enables/disables menu items accordingly (e.g. disabling Remove when no row is selected, disabling Move Up on the first row).

## Architecture

The feature is entirely within the UI layer (Forms). No changes to Models, Services, ViewModels, or Persistence are required.

Each form that receives a context menu follows this identical pattern. The implementation is per-form (no shared base class or helper) because:
- Each form has different grids, different actions, and different enable/disable logic.
- The existing forms (FormBlueprintV2, FormBuildPlanner) already use this direct approach.
- A shared abstraction would add complexity without meaningful reuse given the varied enable/disable conditions.
### Forms Receiving Context Menus

| Form | Grid(s) | Menu Actions |
|------|---------|--------------|
| FormColonyV2 | dgvItems | Add Item, Remove Item |
| FormColonyV2 | dgvCommodityRequests | Add Request, Remove Request |
| FormColonyV2 | dgvOverflowRules | Add Rule, Remove Rule |
| FormDeliveryRoute | dgvStops | Add Stop, Move Up, Move Down, Remove Stop |
| FormDeliveryRoute | dgvDropOff | Add Item, Remove Item |
| FormDeliveryRoute | dgvPickUp | Add Item, Remove Item |
| FormDeliveryExecution | dgvLoadList | Mark All Delivered, Mark All Undelivered |
| FormSurvey | dgvResources | Add Resource, Remove Resource |
| FormStockTargets | dgvTargets | Add Target, Remove Target |
| FormMarket | dgvListings | Record Sale, Edit Listing, Delete Listing |
| FormMarket | dgvTransactions | View Details |
| FormShipTemplate | dgvSlots | Clear Slot |
| FormShipInstance | dgvComponents | Clear Slot |
| FormShipInstance | dgvCargo | Add Item, Remove Item |
| FormStation | dgvHold | Add Item, Remove Item |
| FormStation | dgvComponents | Clear Slot |
| FormStation | dgvMunitions | Add Munition, Remove Munition |

## Components and Interfaces

### Per-Grid Components (Designer.cs)

Each context menu requires these components declared in the form's Designer.cs:

1. **ContextMenuStrip** - named `cms{GridPurpose}` (e.g. `cmsStops`, `cmsDropOff`, `cmsHold`)
2. **ToolStripMenuItem(s)** - named `tsmi{Action}{GridPurpose}` (e.g. `tsmiAddStop`, `tsmiRemoveDropOff`)
3. **Assignment** - `dgv.ContextMenuStrip = this.cms{GridPurpose};`

### Event Wiring (Form .cs constructor)

Each form wires these events in its constructor:

```csharp
// Context menu item clicks -> existing button handlers
tsmiAddStop.Click += CmdAddStop_Click;
tsmiRemoveStop.Click += CmdRemoveStop_Click;

// Row selection on right-click
dgvStops.CellMouseClick += DgvStops_CellMouseClick;

// Enable/disable on menu open
cmsStops.Opening += CmsStops_Opening;
```

### Row Selection Handler Pattern

```csharp
private void DgvStops_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
{
    if (e.Button != MouseButtons.Right) return;
    if (e.RowIndex >= 0)
    {
        dgvStops.ClearSelection();
        dgvStops.Rows[e.RowIndex].Selected = true;
        dgvStops.CurrentCell = dgvStops.Rows[e.RowIndex].Cells[0];
    }
    else
    {
        dgvStops.ClearSelection();
    }
}
```

### Opening Handler Pattern

```csharp
private void CmsStops_Opening(object sender, System.ComponentModel.CancelEventArgs e)
{
    bool hasSelection = dgvStops.CurrentRow != null;
    tsmiRemoveStop.Enabled = hasSelection;
    tsmiMoveUpStop.Enabled = hasSelection && dgvStops.CurrentRow.Index > 0;
    tsmiMoveDownStop.Enabled = hasSelection
        && dgvStops.CurrentRow.Index < dgvStops.RowCount - 1;
}
```
### Naming Conventions

| Convention | Pattern | Example |
|-----------|---------|---------|
| ContextMenuStrip | `cms{GridPurpose}` | cmsStops, cmsDropOff, cmsHold |
| ToolStripMenuItem | `tsmi{Action}{GridPurpose}` | tsmiAddStop, tsmiRemoveDropOff |
| CellMouseClick handler | `Dgv{GridPurpose}_CellMouseClick` | DgvStops_CellMouseClick |
| Opening handler | `Cms{GridPurpose}_Opening` | CmsStops_Opening |

### Complete Component Inventory

#### FormColonyV2

| Component | Type | Name |
|-----------|------|------|
| ContextMenuStrip | dgvItems | cmsItems |
| ToolStripMenuItem | Add Item | tsmiAddItem |
| ToolStripMenuItem | Remove Item | tsmiRemoveItem |
| ContextMenuStrip | dgvCommodityRequests | cmsCommodityRequests |
| ToolStripMenuItem | Add Request | tsmiAddCommodityRequest |
| ToolStripMenuItem | Remove Request | tsmiRemoveCommodityRequest |
| ContextMenuStrip | dgvOverflowRules | cmsOverflowRules |
| ToolStripMenuItem | Add Rule | tsmiAddOverflowRule |
| ToolStripMenuItem | Remove Rule | tsmiRemoveOverflowRule |

#### FormDeliveryRoute

| Component | Type | Name |
|-----------|------|------|
| ContextMenuStrip | dgvStops | cmsStops |
| ToolStripMenuItem | Add Stop | tsmiAddStop |
| ToolStripMenuItem | Move Up | tsmiMoveUpStop |
| ToolStripMenuItem | Move Down | tsmiMoveDownStop |
| ToolStripMenuItem | Remove Stop | tsmiRemoveStop |
| ContextMenuStrip | dgvDropOff | cmsDropOff |
| ToolStripMenuItem | Add Item | tsmiAddDropOff |
| ToolStripMenuItem | Remove Item | tsmiRemoveDropOff |
| ContextMenuStrip | dgvPickUp | cmsPickUp |
| ToolStripMenuItem | Add Item | tsmiAddPickUp |
| ToolStripMenuItem | Remove Item | tsmiRemovePickUp |

#### FormDeliveryExecution

| Component | Type | Name |
|-----------|------|------|
| ContextMenuStrip | dgvLoadList | cmsLoadList |
| ToolStripMenuItem | Mark All Delivered | tsmiMarkAllDelivered |
| ToolStripMenuItem | Mark All Undelivered | tsmiMarkAllUndelivered |

#### FormSurvey

| Component | Type | Name |
|-----------|------|------|
| ContextMenuStrip | dgvResources | cmsResources |
| ToolStripMenuItem | Add Resource | tsmiAddResource |
| ToolStripMenuItem | Remove Resource | tsmiRemoveResource |

#### FormStockTargets

| Component | Type | Name |
|-----------|------|------|
| ContextMenuStrip | dgvTargets | cmsTargets |
| ToolStripMenuItem | Add Target | tsmiAddTarget |
| ToolStripMenuItem | Remove Target | tsmiRemoveTarget |

#### FormMarket

| Component | Type | Name |
|-----------|------|------|
| ContextMenuStrip | dgvListings | cmsListings |
| ToolStripMenuItem | Record Sale | tsmiRecordSale |
| ToolStripMenuItem | Edit Listing | tsmiEditListing |
| ToolStripMenuItem | Delete Listing | tsmiDeleteListing |
| ContextMenuStrip | dgvTransactions | cmsTransactions |
| ToolStripMenuItem | View Details | tsmiViewDetails |

#### FormShipTemplate

| Component | Type | Name |
|-----------|------|------|
| ContextMenuStrip | dgvSlots | cmsSlots |
| ToolStripMenuItem | Clear Slot | tsmiClearSlot |

#### FormShipInstance

| Component | Type | Name |
|-----------|------|------|
| ContextMenuStrip | dgvComponents | cmsComponents |
| ToolStripMenuItem | Clear Slot | tsmiClearSlotComponent |
| ContextMenuStrip | dgvCargo | cmsCargo |
| ToolStripMenuItem | Add Item | tsmiAddCargo |
| ToolStripMenuItem | Remove Item | tsmiRemoveCargo |

#### FormStation

| Component | Type | Name |
|-----------|------|------|
| ContextMenuStrip | dgvHold | cmsHold |
| ToolStripMenuItem | Add Item | tsmiAddHold |
| ToolStripMenuItem | Remove Item | tsmiRemoveHold |
| ContextMenuStrip | dgvComponents | cmsStationComponents |
| ToolStripMenuItem | Clear Slot | tsmiClearSlotStation |
| ContextMenuStrip | dgvMunitions | cmsMunitions |
| ToolStripMenuItem | Add Munition | tsmiAddMunition |
| ToolStripMenuItem | Remove Munition | tsmiRemoveMunition |

## Data Models

No data model changes are required. Context menus invoke existing handlers that already interact with the data model through established ViewModels and Services.

## Error Handling

- **No row selected**: Menu items that require a selection are disabled via the Opening event handler. The existing button handlers already have null-checks as defensive guards.
- **Grid in edit mode**: Right-clicking while a cell is in edit mode will commit the edit (default DataGridView behavior) before showing the context menu. No special handling needed.
- **Form in invalid state** (e.g. no entity loaded): The existing button handlers already check for this condition (e.g. `if (_viewModel.IsNew) return;`). The context menu items call the same handlers, so they inherit this protection.
- **Positional constraints** (Move Up on first row, Move Down on last row): Handled in the Opening event by checking `CurrentRow.Index` against bounds.
- **Right-click on empty grid area** (no rows): The CellMouseClick handler checks `e.RowIndex >= 0`. When negative (header or empty area), it clears the selection, causing the Opening handler to disable row-dependent items.

## Testing Strategy

### Why Property-Based Testing Does Not Apply

This feature is purely UI wiring - adding ContextMenuStrip controls, wiring Click events to existing handlers, and managing enable/disable state in Opening events. There are no pure functions, no data transformations, no algorithms, and no input spaces to explore. The logic is:
- Event handler delegation (menu item click -> existing button handler)
- Boolean enable/disable based on selection state

PBT is not appropriate for UI rendering and interaction features. These are best verified through example-based tests and manual testing.

### Unit Test Approach

For forms where the Opening handler has non-trivial logic (e.g. FormDeliveryRoute with Move Up/Move Down positional constraints), write focused unit tests:

- Test that Opening handler disables Move Up when first row is selected
- Test that Opening handler disables Move Down when last row is selected
- Test that Opening handler disables all row-dependent items when no row is selected
- Test that Opening handler enables all items when a middle row is selected

These tests instantiate the form, programmatically set grid state, invoke the Opening handler, and assert the Enabled property of each menu item.

### Integration Testing

Each context menu action calls the same handler as the corresponding button. Since the button handlers are already tested through existing test infrastructure, the context menu integration is verified by confirming the wiring is correct (same handler method reference).

### Manual Testing Checklist

For each form/grid combination:
1. Right-click on a data row - verify menu appears with correct items
2. Verify the clicked row becomes selected
3. Activate each menu item - verify it performs the same action as the button
4. Right-click on empty area - verify row-dependent items are disabled
5. For grids with Move Up/Down - verify positional constraints disable correctly

### Test Coverage by Requirement

| Requirement | Test Type | What is Verified |
|-------------|-----------|------------------|
| Req 1 (Infrastructure) | Build + Manual | Naming conventions, Designer declarations compile |
| Req 2-18 (Per-form menus) | Manual + Unit | Menu appears, actions work, enable/disable correct |
| Req 19 (Row selection) | Manual + Unit | Right-click selects row, empty area clears selection |
| Req 20 (Enable/disable) | Unit | Opening handler sets Enabled correctly per state |