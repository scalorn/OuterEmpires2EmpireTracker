<!-- Extracted from .kiro/specs/empire-systems/design.md -->
# Ship Mockups

### FormShipTemplate (Iteration 3)

MDI child form. Left-list / right-detail pattern.

```
+---------------------------------------------------------------------------------+
| #1 - Ship Templates                                                     [_][X] |
+----------------------+----------------------------------------------------------+
| Filter: [__________] | Name: [Keystone______________]                          |
|                      | Hull: [Clipper Hull Mk3                            v]   |
| +------------------+ |       (on focus: [Filter:____] [Clipper Hull Mk3  v])   |
| |> Keystone        | |                                                         |
| |  Vanguard        | | +------------+-------+------------------------------+   |
| |  Apollo          | | | Slot Type  | Slot# | Component                    |   |
| |  Mining Barge    | | +------------+-------+------------------------------+   |
| |                  | | | Reactor    |   0   | Reactor Mk3                  |   |
| |                  | | | Drive      |   0   | Drive Mk3                    |   |
| |                  | | | Cargo Pod  |   0   | Cargo Pod Mk2                |   |
| |                  | | | Cargo Pod  |   1   | Cargo Pod Mk2                |   |
| |                  | | | Fuel Tank  |   0   | Fuel Tank Mk2                |   |
| |                  | | | Weapon     |   0   | Laser Cannon Mk2             |   |
| |                  | | | Weapon     |   1   | (empty)                      |   |
| |                  | | +------------+-------+------------------------------+   |
| |                  | |                                                         |
| |                  | | Stats:                                                  |
| |                  | | Mass: 18500  |  Power: 850/620 (Balance: +230)          |
| |                  | | Cargo: 2400  |  Fuel: 800  |  Hopper: 0                |
| |                  | | Health: 15000  |  Shield: 5000 (Regen: 25)             |
| |                  | | Defence -- Energy: 120  Kinetic: 85  Missile: 60       |
| |                  | | Accel: 4.2  |  Rotation: 3.8  |  Jump: 12             |
| |                  | | Mining Yield: 0  |  Scan Level: 0                      |
| |                  | |                                                         |
| |                  | | [New] [Save] [Delete] [Order Build]                     |
| +------------------+ |                                                         |
+----------------------+----------------------------------------------------------+
```

Controls:
- Left: `flpSearchList` -> `txtFilter` (ValidatedTextBox) + `lvwTemplates` (ListView)
- Right: `flpDetail` -> `txtName` (ValidatedTextBox), `cmbHull` (FilteredTextComboSet), `dgvSlots` (DataGridView), `rtbStats` (RichTextBox), `flpCommands` (New/Save/Delete/Order Build)
- `dgvSlots` columns: SlotType (text, read-only), SlotIndex (text, read-only), Component (`DataGridViewFilteredComboBoxColumn` -- inline filter TextBox + ComboBox on cell edit)
- Component column uses `DataGridViewFilteredComboBoxCell` with per-cell item lists. UUID resolution via parallel `SlotInfo.UUIDByIndex` on row Tag.
- Hull combo uses `FilteredTextComboSet` with parallel `_hullUUIDs` list for UUID resolution. Shows full-width combo when unfocused, splits into filter + combo on focus.
- Stats panel: `rtbStats` (RichTextBox) -- computed from hull + components via ShipBuildService.ComputeStats. Shows all stat groups in compact multi-line format.
- Buttons at bottom of detail panel in `flpCommands`: `cmdNew`, `cmdSave`, `cmdDelete`, `cmdOrderBuild`
- "Order Build" opens a dialog to select/create a build plan and specify assembly location

### FormShipInstance (Iteration 3 — Ship Form Overhaul)

MDI child form. Left-list / right-detail pattern with tabs for stats/components and cargo/holds. Layout aligned with FormShipTemplate.

```
+---------------------------------------------------------------------------------+
| #2 - Ships                                                              [_][X] |
+----------------------+----------------------------------------------------------+
| Filter: [__________] | Name: [Keystone______________]                          |
|                      | Hull: [Clipper Hull Mk3                            v]   |
| +------------------+ |       (on focus: [Filter:____] [Clipper Hull Mk3  v])   |
| |> Keystone        | | Location: [Colony v] [Alpha Prime          v]           |
| |  Vanguard        | |                                                         |
| |  Apollo          | | [Overview] [Cargo]                                      |
| |  Mining Barge    | | +------------+---+------------------------------+-------+
| |                  | | | Slot Type  | # | Component                    | Cond% |
| |                  | | +------------+---+------------------------------+-------+
| |                  | | | Hull       |   | Clipper Hull Mk3 (read-only) | 100   |
| |                  | | | Reactor    | 0 | Reactor Mk3                  | 100   |
| |                  | | | Drive      | 0 | Drive Mk3                    | 100   |
| |                  | | | Cargo Pod  | 0 | Cargo Pod Mk2                | 100   |
| |                  | | | Cargo Pod  | 1 | Cargo Pod Mk2                | 100   |
| |                  | | | Fuel Tank  | 0 | Fuel Tank Mk2                | 100   |
| |                  | | | Weapon     | 0 | Laser Cannon Mk2             | 100   |
| |                  | | | Weapon     | 1 | (empty)                      |       |
| |                  | | +------------+---+------------------------------+-------+
| |                  | |                                                         |
| |                  | | Stats:                                                  |
| |                  | | Mass: 18500  |  Power: 850/620 (Balance: +230)          |
| |                  | | Cargo: 2400  |  Fuel: 800  |  Hopper: 0                |
| |                  | | Health: 15000  |  Shield: 5000 (Regen: 25)             |
| |                  | | Defence -- Energy: 120  Kinetic: 85  Missile: 60       |
| |                  | | Accel: 4.2  |  Rotation: 3.8  |  Jump: 12             |
| |                  | | Mining Yield: 0  |  Scan Level: 0                      |
| |                  | |                                                         |
| |                  | | [New] [Save] [Delete] [From Template]                   |
| +------------------+ |                                                         |
+----------------------+----------------------------------------------------------+
```

Controls:
- Left: `flpSearchList` -> `txtFilter` (ValidatedTextBox) + `lvwShips` (ListView)
- Right: `flpDetail` -> `txtName` (ValidatedTextBox), `cmbHull` (FilteredTextComboSet), `cmbLocationType` (ComboBox), `cmbLocationUUID` (ComboBox), `tabControl` (TabControl with Overview and Cargo tabs), `flpCommands` (New/Save/Delete/From Template)
- `dgvComponents` columns: colSlotType (text, read-only), colSlotIndex (text, read-only, "#", width 40), colComponent (`DataGridViewFilteredComboBoxColumn`, width 300), colCondition (text, editable), colMaxRepair (text, editable)
- Component column uses `DataGridViewFilteredComboBoxCell` with per-cell item lists. UUID resolution via parallel `SlotInfo.UUIDByIndex` on row Tag. Hull row is first, component cell read-only.
- Hull combo uses `FilteredTextComboSet` with parallel `_hullUUIDs` list for UUID resolution. Shows full-width combo when unfocused, splits into filter + combo on focus.
- `dgvComponents` uses `SelectionMode = CellSelect` and `EditMode = EditOnEnter` for inline component selection.
- Stats panel: `rtbStats` (RichTextBox) -- computed from hull + components via ShipBuildService.ComputeStats.
- Buttons at bottom of detail panel in `flpCommands`: `cmdNew`, `cmdSave`, `cmdDelete`, `cmdFromTemplate`
- "New" creates a blank ship with default name "New Ship" for manual hull selection
- "From Template" opens a dialog to create a ship from an existing template
- Cargo tab: `rbCargoHold` / `rbHopper` (RadioButtons) to switch views. Hopper radio only enabled when ship has Ore Hopper components.
  - Cargo Hold view: `dgvCargo` (DataGridView -- flat item list). `dgvCrateContents` crate detail grid shown when a crate is selected.
  - Hopper view: uses the same `dgvCargo` grid with radio toggle. Hopper only accepts unrefined resources (High, Medium, Low purity). Add panel (`cmbAddType`, `cmbAddItem` (FilteredTextComboSet), `cmbAddPurity`, `txtAddQty`, `cmdAddItem`, `cmdRemoveItem`) with resource filter/combo, purity combo (restricted to High/Medium/Low), quantity, and Add button.
