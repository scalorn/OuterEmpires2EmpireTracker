<!-- Extracted from .kiro/specs/empire-systems/design.md -->
# Station Mockups

### FormStation (Iteration 4)

MDI child form. Left-list / right-detail pattern with tabs for hold/components/munitions.

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│ #1 - Stations                                                           [_][□][X]│
├──────────────────────┬──────────────────────────────────────────────────────────┤
│ Filter: [__________] │ Name: [Station Alpha_________]                          │
│                      │ Type: [Station     ▼]  Ownership: [Government ▼]        │
│ ┌──────────────────┐ │                                                         │
│ │▸ Station Alpha   │ │ ┌─ Hold ─┬─ Components ─┬─ Munitions ──────────────┐   │
│ │  Outpost Beta    │ │ │        │              │                          │   │
│ │  Starbase Omega  │ │ │ (current player's hold)                          │   │
│ │  My Station      │ │ │                                                  │   │
│ │                  │ │ │ ┌──────────┬──────────────────┬─────┐            │   │
│ │                  │ │ │ │ Type     │ Item             │ Qty │            │   │
│ │                  │ │ │ ├──────────┼──────────────────┼─────┤            │   │
│ │                  │ │ │ │ Resource │ Refined Titanium │ 500 │            │   │
│ │                  │ │ │ │ Commodty │ Reactor Mk3      │  12 │            │   │
│ │                  │ │ │ │ [Crate]  │ Order #42 (8)    │   1 │            │   │
│ │                  │ │ │ │ Blueprnt │ Drive Mk3        │   3 │            │   │
│ │                  │ │ │ └──────────┴──────────────────┴─────┘            │   │
│ │                  │ │ │                                                  │   │
│ │                  │ │ │ Crate Contents (Order #42):                      │   │
│ │                  │ │ │ ┌──────────┬──────────────────┬─────┐            │   │
│ │                  │ │ │ │ Type     │ Item             │ Qty │            │   │
│ │                  │ │ │ ├──────────┼──────────────────┼─────┤            │   │
│ │                  │ │ │ │ Commodty │ Reactor Mk3      │   4 │            │   │
│ │                  │ │ │ │ Commodty │ Drive Mk3        │   4 │            │   │
│ │                  │ │ │ └──────────┴──────────────────┴─────┘            │   │
│ │                  │ │ │                                                  │   │
│ │                  │ │ │ Add: Type:[Resource▼] Filter:[___] [Titanium▼]  │   │
│ │                  │ │ │      Purity:[Refined▼] Qty:[100] [Add]          │   │
│ │                  │ │ │ [New Crate] [Move to Crate] [Remove] [Del Crate]│   │
│ │                  │ │ └──────────────────────────────────────────────────┘   │
│ └──────────────────┘ │                                                         │
│ [New] [Delete]       │                                                         │
├──────────────────────┴─────────────────────────────────────────────────────────┤
│ [Save] [Delete]                                                                │
└────────────────────────────────────────────────────────────────────────────────┘
```

Components tab (player-owned stations only):

```
│ ┌─ Hold ─┬─ Components ─┬─ Munitions ──────────────────────┐   │
│ │        │              │                                  │   │
│ │ Station Blueprint: [Outpost Mk2                      ▼] │   │
│ │                                                          │   │
│ │ ┌────────────┬───────┬──────────────────────┬───────────────┬────────┬─────────┐│   │
│ │ │ Slot Type  │ Slot# │ Blueprint            │ Condition     │ MaxRep │ Actions ││   │
│ │ ├────────────┼───────┼──────────────────────┼───────────────┼────────┼─────────┤│   │
│ │ │ Hull       │   -   │ Outpost Mk2          │ 76000/80000 95%│  100% │         ││   │
│ │ │ Reactor    │   0   │ Station Reactor Mk2  │  1200/1200 100%│  100% │ [Clear] ││   │
│ │ │ Shield     │   0   │ Shield Generator Mk1 │ 11000/12000 92%│   90% │ [Clear] ││   │
│ │ │ Weapon     │   0   │ Turret Mk2           │   450/500   90%│  100% │ [Clear] ││   │
│ │ │ Weapon     │   1   │ (empty)              │               │        │ [Set]   ││   │
│ │ └────────────┴───────┴──────────────────────┴───────────────┴────────┴─────────┘│   │
│ │                                                          │   │
│ │ Install: Filter:[______] [Shield Gen Mk2          ▼]    │   │
│ │          Slot:  [Shield / 0  ▼]  [Install]              │   │
│ │                                                          │   │
│ │ Stats:                                                   │   │
│ │ ┌──────────────────────┬────────────┐                    │   │
│ │ │ Total Mass           │  45000 kg  │                    │   │
│ │ │ Power Generated      │   1200 MW  │                    │   │
│ │ │ Power Consumed       │    850 MW  │                    │   │
│ │ │ Power Balance        │  + 350 MW  │                    │   │
│ │ ├──────────────────────┼────────────┤                    │   │
│ │ │ Total Health         │  80000 HP  │                    │   │
│ │ │ Shield HP            │  12000     │                    │   │
│ │ │ Shield Regen         │     50/s   │                    │   │
│ │ │ Energy Defence       │    250     │                    │   │
│ │ │ Kinetic Defence      │    180     │                    │   │
│ │ │ Missile Defence      │    120     │                    │   │
│ │ ├──────────────────────┼────────────┤                    │   │
│ │ │ Weapons (S/M/L)      │   1/1/0    │                    │   │
│ │ └──────────────────────┴────────────┘                    │   │
│ └──────────────────────────────────────────────────────────┘   │
```

Controls:
- Left: `flpSearchList` → `txtStationFilter` + `lvwStations` (ListView) + `cmdNew` / `cmdDelete`
- Right: `flpStationData` → `txtName`, `cmbStationType`, `cmbOwnership`, `cmdSave`, `tabStationDetail` (TabControl with Hold, Components, Munitions tabs)
- Hold tab: `dgvHold` (DataGridView, editable — scoped to current player's hold), `dgvHoldCrateContents` (detail grid), add-item panel (`cmbHoldType`, `cmbHoldItem` (FilteredTextComboSet), `cmbHoldPurity`, `txtHoldQty`, `cmdHoldAdd`, `cmdHoldRemove`), crate buttons
- Components tab: `cmbStationBlueprint` (FilteredTextComboSet, parallel value list: Blueprint UUID), `dgvComponents` (columns: Slot Type, Slot#, Blueprint, Condition, MaxRepair, Actions — Condition and MaxRepair editable, hull row always first), install panel, `rtbStationStats` (read-only RichTextBox) — computed via ShipBuildService.ComputeStationStats
- Munitions tab: `dgvMunitions` (DataGridView), add panel (`cmbMunItem` (FilteredTextComboSet), `txtMunQty`, `cmdMunAdd`, `cmdMunRemove`) — visible only for armed player-owned stations

### Crate UI Pattern (All Inventory Views)

All forms that display ItemBag contents (station holds, ship cargo, colony warehouse) use the same master-detail pattern for crates:

```
┌─ Inventory ────────────────────────────────────────────────────────────────┐
│ ┌──────────┬──────────────────────┬─────┬────────────┬───────────┬────────┐│
│ │ Type     │ Item                 │ Qty │ Volume     │ Condition │ MaxRep ││
│ ├──────────┼──────────────────────┼─────┼────────────┼───────────┼────────┤│
│ │ Resource │ Refined Titanium     │ 500 │    500 m³  │           │        ││
│ │ [Crate]  │ Supply Run (12 items)│   1 │    850 m³  │           │        ││ ← selected
│ │ ShipPart │ Reactor Mk3          │   1 │     50 m³  │ 380/400 95%│  90% ││
│ │ Commodty │ Fuel Cells           │  50 │    500 m³  │           │        ││
│ └──────────┴──────────────────────┴─────┴────────────┴───────────┴────────┘│
│                                                                            │
│ Crate Contents (Supply Run):                                               │
│ ┌──────────┬──────────────────────┬─────┬────────────┬───────────┬────────┐│
│ │ Type     │ Item                 │ Qty │ Volume     │ Condition │ MaxRep ││
│ ├──────────┼──────────────────────┼─────┼────────────┼───────────┼────────┤│
│ │ Resource │ Flatpack: Mfg Bay    │   4 │    400 m³  │           │        ││
│ │ Commodty │ Fuel Cells           │   8 │    200 m³  │           │        ││
│ │ ShipPart │ Drive Mk2            │   1 │     50 m³  │ 140/180 78%│  85% ││
│ └──────────┴──────────────────────┴─────┴────────────┴───────────┴────────┘│
│                                                                            │
│ [New Crate] [Move to Crate] [Remove from Crate] [Delete Crate]            │
└────────────────────────────────────────────────────────────────────────────┘
```

Behavior:
- Main grid shows all items including crates. Crate rows display "[Crate]" prefix and an item count summary (e.g. "Supply Run (12 items)").
- When a crate row is selected, the detail grid below shows the crate's contents.
- When a non-crate row is selected, the detail grid is hidden or shows empty.
- Condition and MaxRepair columns are editable on any item. Clicking the Condition cell opens an inline editor for CurrentHP/MaxHP. Clicking MaxRepair edits the percentage. For items with no damage (all zeros), the cells are blank. The user can enter damage on any item type — the model supports it universally.
- Buttons: "New Crate" (creates empty crate), "Move to Crate" (moves selected main-grid item into the selected crate), "Remove from Crate" (moves item from crate detail back to main inventory), "Delete Crate" (moves contents back to main inventory first, then removes the crate).
- Crate rows cannot be dragged into other crate rows (no nesting).

This pattern applies to: FormStation hold grid, FormShipInstance cargo grid, Colony warehouse grid (future), and any other ItemBag display. The crate master-detail controls are implemented as a reusable UserControl (`CrateInventoryPanel`) that can be dropped into any form.

