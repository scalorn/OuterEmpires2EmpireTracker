<!-- Extracted from .kiro/specs/empire-systems/design.md -->
# Stock Targets Mockups

### FormStockTargets (Iteration 7)

MDI child form. Left-list / right-detail pattern with plans on the left and targets on the right. All stock targets live inside plans â€” simple targets like "20k Munitions" are single-target plans.

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚ #1 - Stock Targets                                                      [_][â–¡][X]â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚ Filter: [__________] â”‚ Plan: [Ship Stock for Faction Alpha___]  â˜‘ Active        â”‚
â”‚                      â”‚ Replenishment Plan: [Filter:___] [Restock Orders    â–¼]  â”‚
â”‚ â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚                                                         â”‚
â”‚ â”‚â–¸ Faction Alpha   â”‚ â”‚ Targets:                                                â”‚
â”‚ â”‚  Faction Beta    â”‚ â”‚ â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”â”‚
â”‚ â”‚  Base Supplies   â”‚ â”‚ â”‚ Type     â”‚ Item         â”‚ Target â”‚ Scope â”‚ Curr â”‚ Î”  â”‚â”‚
â”‚ â”‚  20k Munitions   â”‚ â”‚ â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”¤â”‚
â”‚ â”‚  Spare Reactors  â”‚ â”‚ â”‚ ShipTmpl â”‚ Keystone     â”‚     10 â”‚Empire â”‚    7 â”‚ -3 â”‚â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚ ShipTmpl â”‚ Vanguard     â”‚     10 â”‚Empire â”‚   10 â”‚  0 â”‚â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚ Commodty â”‚ Fuel Cells   â”‚    500 â”‚Stn A  â”‚  320 â”‚-180â”‚â”‚
â”‚ â”‚                  â”‚ â”‚ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”˜â”‚
â”‚ â”‚                  â”‚ â”‚                                                         â”‚
â”‚ â”‚                  â”‚ â”‚ Add Target:                                             â”‚
â”‚ â”‚                  â”‚ â”‚ Type:[ShipTemplateâ–¼] Item:[Keystone          â–¼]        â”‚
â”‚ â”‚                  â”‚ â”‚ Target Qty:[10] Critical:[3]                            â”‚
â”‚ â”‚                  â”‚ â”‚ Scope:[EmpireWideâ–¼] Location:[                â–¼]       â”‚
â”‚ â”‚                  â”‚ â”‚ [Add Target] [Remove Target]                            â”‚
â”‚ â”‚                  â”‚ â”‚                                                         â”‚
â”‚ â”‚                  â”‚ â”‚ Expanded Components (Keystone Ã— 10):                    â”‚
â”‚ â”‚                  â”‚ â”‚ â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚ Component        â”‚ Required â”‚ In Stock â”‚Shortfall â”‚   â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚ Clipper Hull Mk3 â”‚       10 â”‚        7 â”‚        3 â”‚   â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚ Reactor Mk3      â”‚       10 â”‚       12 â”‚        0 â”‚   â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚ Drive Mk3        â”‚       10 â”‚        8 â”‚        2 â”‚   â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚ Cargo Pod Mk2    â”‚       20 â”‚       15 â”‚        5 â”‚   â”‚
â”‚ â”‚                  â”‚ â”‚ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚ â”‚                  â”‚ â”‚                                                         â”‚
â”‚ â”‚                  â”‚ â”‚ [Check & Generate Orders]                               â”‚
â”‚ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚                                                         â”‚
â”‚[New Plan][Quick Add] â”‚                                                         â”‚
â”‚ [Delete]             â”‚                                                         â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚ [Save]                                                                         â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

Controls:
- Left: `flpSearchList` â†’ `txtPlanFilter` + `lvwStockPlans` (ListView) + `cmdNewPlan` / `cmdQuickAdd` / `cmdDeletePlan`
- `cmdNewPlan` creates an empty plan. `cmdQuickAdd` creates a plan with a single target in one step (prompts for item type, item, quantity, scope â€” names the plan after the item).
- Right: `txtPlanName`, `chkPlanActive` (CheckBox, write-through to StockPlan.IsActive), `cmbReplenishmentPlan` (FilteredComboBox of build plans), `dgvTargets` (DataGridView with color-coded shortfall column: green=0, yellow=below target, red=below critical), add-target panel, expanded components panel
- Inactive plans: list view shows plan name in gray italic. "Check & Generate Orders" skips inactive plans.
- `dgvTargets` columns: Type, Item, TargetQty, CriticalThreshold, Scope, Location, CurrentQty, Shortfall
- Add-target panel: `cmbTargetType`, `cmbTargetItem` (FilteredComboBox), `txtTargetQty`, `txtCriticalThreshold`, `cmbScope`, `cmbLocation`, `cmdAddTarget` / `cmdRemoveTarget`
- Expanded components panel: `dgvExpandedComponents` (read-only) â€” visible when a ShipTemplate target is selected, shows per-component breakdown
- "Check & Generate Orders" runs StockTargetService.CheckTargets, shows results, and creates build items in the designated replenishment plan



### Stock Profiles â€” FormStockTargets Tab (Iteration 7)

New tab on FormStockTargets, added alongside the existing targets view. The main form becomes tabbed: "Targets & Plans" (existing content) and "Profiles" (new).

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚ #1 - Stock Targets                                                      [_][â–¡][X]â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚ â”Œâ”€ Targets & Plans â”€â”¬â”€ Profiles â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚ â”‚                                                                          â”‚   â”‚
â”‚ â”‚ Filter: [__________]                                                     â”‚   â”‚
â”‚ â”‚                                                                          â”‚   â”‚
â”‚ â”‚ â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”  Profile: [Faction Alpha Full Stock___]  â˜‘ Active  â”‚   â”‚
â”‚ â”‚ â”‚â–¸ Faction Alpha Full  â”‚                                                 â”‚   â”‚
â”‚ â”‚ â”‚  Light Combat Ready  â”‚  Entries:                                       â”‚   â”‚
â”‚ â”‚ â”‚  Base Maintenance    â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚   â”‚
â”‚ â”‚ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜  â”‚ Group â”‚ Plan                             â”‚ â”‚   â”‚
â”‚ â”‚ [New Profile] [Delete]    â”œâ”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤ â”‚   â”‚
â”‚ â”‚                           â”‚ A     â”‚ Faction Alpha Ships              â”‚ â”‚   â”‚
â”‚ â”‚                           â”‚ A     â”‚ Faction Beta Ships               â”‚ â”‚   â”‚
â”‚ â”‚                           â”‚ B     â”‚ Base Supplies                    â”‚ â”‚   â”‚
â”‚ â”‚                           â”‚ C     â”‚ 20k Munitions                    â”‚ â”‚   â”‚
â”‚ â”‚                           â””â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚   â”‚
â”‚ â”‚                                                                          â”‚   â”‚
â”‚ â”‚  Add Entry:                                                              â”‚   â”‚
â”‚ â”‚  Group:[A___] Type:[Plan    â–¼] [Filter:___] [Faction Alpha Ships   â–¼]  â”‚   â”‚
â”‚ â”‚  [Add Entry] [Remove Entry]                                              â”‚   â”‚
â”‚ â”‚                                                                          â”‚   â”‚
â”‚ â”‚  Logic: Group A (OR): max(Faction Alpha Ships, Faction Beta Ships)       â”‚   â”‚
â”‚ â”‚         Group B (AND): + Base Supplies                                   â”‚   â”‚
â”‚ â”‚         Group C (AND): + 20k Munitions                                   â”‚   â”‚
â”‚ â”‚         Total = max(A) + sum(B) + sum(C)                                 â”‚   â”‚
â”‚ â”‚                                                                          â”‚   â”‚
â”‚ â”‚  [Save]                                                                  â”‚   â”‚
â”‚ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

Controls:
- The existing FormStockTargets content moves into a "Targets & Plans" tab. The new "Profiles" tab is added alongside it.
- Profiles tab left section: `txtProfileFilter`, `lvwProfiles` (ListView), `cmdNewProfile` / `cmdDeleteProfile`
- Profiles tab right section: `txtProfileName`, `chkProfileActive` (CheckBox, write-through to StockProfile.IsActive), `dgvEntries` (DataGridView), add-entry panel, logic summary label
- Inactive profiles: list view shows profile name in gray italic. Excluded from stock target aggregation.
- `dgvEntries` columns: GroupID (editable text), Plan name (read-only, resolved from StockPlanUUID)
- Add-entry panel: `txtGroupID`, `txtEntryFilter`, `cmbEntry` (FilteredComboBox of StockPlans), `cmdAddEntry` / `cmdRemoveEntry`
- Logic summary: read-only label auto-generated from the entries, showing the AND/OR grouping in plain language. Entries with the same GroupID are ORed (max), different GroupIDs are ANDed (summed).
