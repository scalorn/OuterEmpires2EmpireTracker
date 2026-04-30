<!-- Extracted from .kiro/specs/empire-systems/design.md -->
# Stock Targets Mockups

### FormStockTargets (Iteration 7)

MDI child form. Left-list / right-detail pattern with plans on the left and targets on the right. All stock targets live inside plans — simple targets like ""20k Munitions"" are single-target plans.

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ #1 - Stock Targets                                                  [_][□][X]│
├──────────────────────┬───────────────────────────────────────────────────────┤
│ Filter: [__________] │ Plan: [Ship Stock for Faction Alpha___]  ☑ Active    │
│                      │ Replenishment Plan: [Filter:___] [Restock Orders    ▼]│
│ ┌──────────────────┐ │                                                       │
│ │▸ Faction Alpha   │ │ Targets:                                              │
│ │  Faction Beta    │ │ ┌──────────┬─────────────┬────────┬───────┬─────┬────┐│
│ │  Base Supplies   │ │ │ Type     │ Item        │ Target │ Scope │ Curr│ Δ ││
│ │  20k Munitions   │ │ ├──────────┼─────────────┼────────┼───────┼─────┼────┤│
│ │  Spare Reactors  │ │ │ ShipTmpl │ Keystone    │     10 │Empire │    7│ -3 ││
│ │                  │ │ │ ShipTmpl │ Vanguard    │     10 │Empire │   10│  0 ││
│ │                  │ │ │ Commodty │ Fuel Cells  │    500 │Stn A  │  320│-180││
│ │                  │ │ └──────────┴─────────────┴────────┴───────┴─────┴────┘│
│ │                  │ │                                                       │
│ │                  │ │ Add Target:                                            │
│ │                  │ │ Type:[ShipTemplate▼] Item:[Keystone          ▼]       │
│ │                  │ │ Target Qty:[10] Critical:[3]                           │
│ │                  │ │ Scope:[EmpireWide▼] Location:[                ▼]      │
│ │                  │ │ [Add Target] [Remove Target]                           │
│ │                  │ │                                                       │
│ │                  │ │ Expanded Components (Keystone × 10):                   │
│ │                  │ │ ┌──────────────────┬──────────┬──────────┬──────────┐  │
│ │                  │ │ │ Component        │ Required │ In Stock │Shortfall │  │
│ │                  │ │ │ Clipper Hull Mk3 │       10 │        7 │        3 │  │
│ │                  │ │ │ Reactor Mk3      │       10 │       12 │        0 │  │
│ │                  │ │ │ Drive Mk3        │       10 │        8 │        2 │  │
│ │                  │ │ │ Cargo Pod Mk2    │       20 │       15 │        5 │  │
│ │                  │ │ └──────────────────┴──────────┴──────────┴──────────┘  │
│ │                  │ │                                                       │
│ │                  │ │ [Check & Generate Orders]                              │
│ └──────────────────┘ │                                                       │
│[New Plan][Quick Add] │                                                       │
│ [Delete]             │                                                       │
├──────────────────────┴───────────────────────────────────────────────────────┤
│ [Save]                                                                       │
└──────────────────────────────────────────────────────────────────────────────┘
```

Controls:
- Left: `flpSearchList` → `txtPlanFilter` + `lvwStockPlans` (ListView) + `cmdNewPlan` / `cmdQuickAdd` / `cmdDeletePlan`
- `cmdNewPlan` creates an empty plan. `cmdQuickAdd` creates a plan with a single target in one step (prompts for item type, item, quantity, scope — names the plan after the item).
- Right: `txtPlanName`, `chkPlanActive` (CheckBox, write-through to StockPlan.IsActive), `cmbReplenishmentPlan` (FilteredComboBox of build plans), `dgvTargets` (DataEntryGridView with color-coded shortfall column: green=0, yellow=below target, red=below critical), add-target panel, expanded components panel
- Inactive plans: list view shows plan name in gray italic. `"Check & Generate Orders"` skips inactive plans.
- `dgvTargets` columns: Type, Item, TargetQty, CriticalThreshold, Scope, Location, CurrentQty, Shortfall
- Add-target panel: `cmbTargetType`, `cmbTargetItem` (FilteredComboBox), `txtTargetQty`, `txtCriticalThreshold`, `cmbScope`, `cmbLocation`, `cmdSave`, `cmdCheckGenerate`, `cmdAddTarget` / `cmdRemoveTarget`
- Expanded components panel: `dgvExpandedComponents` (read-only) — visible when a ShipTemplate target is selected, shows per-component breakdown
- `"Check & Generate Orders"` runs StockTargetService.CheckTargets, shows results, and creates build items in the designated replenishment plan


### Stock Profiles — FormStockTargets Tab (Iteration 7)

New tab on FormStockTargets, added alongside the existing targets view. The main form becomes tabbed: `"Targets & Plans"` (existing content) and `"Profiles"` (new).

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ #1 - Stock Targets                                                  [_][□][X]│
├──────────────────────────────────────────────────────────────────────────────┤
│ ┌─ Targets & Plans ─┬─ Profiles ┐  │
│ │                                                                        │  │
│ │ Filter: [__________]                                                   │  │
│ │                                                                        │  │
│ │ ┌──────────────────────┐  Profile: [Faction Alpha Full Stock___]  ☑ Active│  │
│ │ │▸ Faction Alpha Full  │                                               │  │
│ │ │  Light Combat Ready  │  Entries:                                     │  │
│ │ │  Base Maintenance    │  ┌───────┬──────────────────────────────────┐  │  │
│ │ └──────────────────────┘  │ Group │ Plan                            │  │  │
│ │ [New Profile] [Delete]    ├─────────────────────────────────────────┤  │  │
│ │                           │ A     │ Faction Alpha Ships              │  │  │
│ │                           │ A     │ Faction Beta Ships               │  │  │
│ │                           │ B     │ Base Supplies                    │  │  │
│ │                           │ C     │ 20k Munitions                    │  │  │
│ │                           └───────┴──────────────────────────────────┘  │  │
│ │                                                                        │  │
│ │  Add Entry:                                                            │  │
│ │  Group:[A___] Plan:[Faction Alpha Ships                          ▼]    │  │
│ │  [Add Entry] [Remove Entry]                                            │  │
│ │                                                                        │  │
│ │  Logic: Group A (OR): max(Faction Alpha Ships, Faction Beta Ships)     │  │
│ │         Group B (AND): + Base Supplies                                 │  │
│ │         Group C (AND): + 20k Munitions                                 │  │
│ │         Total = max(A) + sum(B) + sum(C)                               │  │
│ │                                                                        │  │
│ │  [Save]                                                                │  │
│ └────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────────┘
```

Controls:
- The existing FormStockTargets content moves into a `"Targets & Plans"` tab. The new `"Profiles"` tab is added alongside it.
- Profiles tab left section: `txtProfileFilter`, `lvwProfiles` (ListView), `cmdNewProfile` / `cmdDeleteProfile`
- Profiles tab right section: `txtProfileName`, `chkProfileActive`, `cmdSaveProfile` (CheckBox, write-through to StockProfile.IsActive), `dgvEntries` (DataGridView), add-entry panel, logic summary label
- Inactive profiles: list view shows profile name in gray italic. Excluded from stock target aggregation.
- `dgvEntries` columns: GroupID (editable text), Plan name (read-only, resolved from StockPlanUUID)
- Add-entry panel: `txtGroupID`, `cmbEntry` (FilteredTextComboSet of StockPlans), `cmdAddEntry` / `cmdRemoveEntry`
- Logic summary: read-only label auto-generated from the entries, showing the AND/OR grouping in plain language. Entries with the same GroupID are ORed (max), different GroupIDs are ANDed (summed).
