<!-- Extracted from .kiro/specs/empire-systems/design.md -->
# Colony Overflow & Stock Profiles Mockups

### Warehouse Overflow Rules â€” FormColony Tab (Iteration 6)

New tab on the existing FormColony, added alongside the existing Administration, Structures, Workers, and Warehousing tabs.

```
â”‚ â”Œâ”€ Admin â”€â”¬â”€ Structures â”€â”¬â”€ Workers â”€â”¬â”€ Warehousing â”€â”¬â”€ Overflow â”€â”€â”€â”€â”€â”   â”‚
â”‚ â”‚                                                                       â”‚   â”‚
â”‚ â”‚ Rules for: Alpha Prime                                                â”‚   â”‚
â”‚ â”‚                                                                       â”‚   â”‚
â”‚ â”‚ â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”  â”‚   â”‚
â”‚ â”‚ â”‚ Resource     â”‚ Purity     â”‚ Threshold â”‚ Current  â”‚ Destination    â”‚ Active â”‚  â”‚   â”‚
â”‚ â”‚ â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”¤  â”‚   â”‚
â”‚ â”‚ â”‚ Iron         â”‚ Refined    â”‚     3000  â”‚    4200  â”‚ Station Alpha  â”‚   â˜‘   â”‚  â”‚   â”‚
â”‚ â”‚ â”‚ Copper       â”‚ Refined    â”‚     2000  â”‚    1800  â”‚ Station Alpha  â”‚   â˜‘   â”‚  â”‚   â”‚
â”‚ â”‚ â”‚ Titanium     â”‚ Refined    â”‚     5000  â”‚    5100  â”‚ Station Beta   â”‚   â˜   â”‚  â”‚   â”‚
â”‚ â”‚ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”˜  â”‚   â”‚
â”‚ â”‚                                                                       â”‚   â”‚
â”‚ â”‚ Add Rule:                                                             â”‚   â”‚
â”‚ â”‚ Resource:[Filter:___] [Iron â–¼] Purity:[Refined â–¼]                    â”‚   â”‚
â”‚ â”‚ Threshold:[3000]                                                      â”‚   â”‚
â”‚ â”‚ Dest Type:[Stationâ–¼] Dest:[Filter:___] [Station Alpha          â–¼]   â”‚   â”‚
â”‚ â”‚ Route:[Filter:___] [Alpha â†’ Station Alpha          â–¼]               â”‚   â”‚
â”‚ â”‚ [Add Rule] [Remove Rule]                                              â”‚   â”‚
â”‚ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
```

Controls:
- New `tabPOverflow` tab page on the existing `tabDetailedData` TabControl
- `dgvOverflowRules` (DataGridView) â€” columns: Resource, Purity, Threshold, Current (read-only, from warehouse), Destination, Active (CheckBox column, write-through to WarehouseOverflowRule.IsActive)
- Inactive rules: row text shown in gray. Background processor skips inactive rules.
- Current column is color-coded: green when below threshold, yellow when within 20% of threshold, red when at or above threshold
- Add-rule panel: `txtOverflowResourceFilter`, `cmbOverflowResource`, `cmbOverflowPurity`, `txtOverflowThreshold`, `cmbOverflowDestType`, `txtOverflowDestFilter`, `cmbOverflowDest`, `txtOverflowRouteFilter`, `cmbOverflowRoute` (FilteredComboBox of delivery routes), `cmdAddRule` / `cmdRemoveRule`
- Rules are per-colony (ColonyUUID set automatically from the selected colony). One rule per resource+purity per colony.
- Destination combo populates with stations or colonies based on `cmbOverflowDestType`.

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
