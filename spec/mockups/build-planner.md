<!-- Extracted from .kiro/specs/empire-systems/design.md -->
# Build Planner Mockups

### FormBuildPlanner (Iteration 1)

MDI child form. Left-list / right-detail pattern with TableLayoutPanel base.

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚ #1 - Build Planner                                                          [_][â–¡][X]â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚ Filter: [__________] â”‚ Name: [Keystone Batch 3______]  Desc: [For faction order___]â”‚
â”‚                      â”‚ â˜‘ Active  [Save] [Auto-Assign] [Generate Delivery â–¼]    â”‚
â”‚ â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚                                                             â”‚
â”‚ â”‚â–¸ Keystone Batch 3â”‚ â”‚ â”Œâ”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”â”‚
â”‚ â”‚  Munitions Run   â”‚ â”‚ â”‚ Type â”‚ Item        â”‚ Qty â”‚ Location    â”‚Structreâ”‚Status â”‚â”‚
â”‚ â”‚  Reactor Restock â”‚ â”‚ â”œâ”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”¤â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚ Mfg  â”‚ Reactor Mk3 â”‚  10 â”‚ Alpha Prime â”‚MfgBay1 â”‚ Ready â”‚â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚ Mfg  â”‚ Drive Mk3   â”‚  10 â”‚ Alpha Prime â”‚MfgBay2 â”‚Staged â”‚â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚ Mfg  â”‚ Hull Clipperâ”‚  10 â”‚ Beta Colony â”‚MfgBay1 â”‚InProg â”‚â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚ Comm â”‚ Fuel Cells  â”‚  50 â”‚ Gamma Out.  â”‚CommFac1â”‚Complt â”‚â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚ Ship â”‚ KeystoneÃ—10 â”‚  10 â”‚             â”‚        â”‚Staged â”‚â”‚
â”‚ â”‚                  â”‚ â”‚ â””â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”˜â”‚
â”‚ â”‚                  â”‚ â”‚                                                             â”‚
â”‚ â”‚                  â”‚ â”‚ Add Item:                                                   â”‚
â”‚ â”‚                  â”‚ â”‚ Type:[Manufactoryâ–¼] Item:[filter|Reactor Mk3          â–¼] â”‚
â”‚ â”‚                  â”‚ â”‚ Qty:[10] Recipient:[______] Duration:[2d 12h 0m 0s] [Queue Calc] â”‚
â”‚ â”‚                  â”‚ â”‚ [Add Item] [Delete Item]                                     â”‚
â”‚ â”‚                  â”‚ â”‚                                                             â”‚
â”‚ â”‚                  â”‚ â”‚ Resource Shortfalls (Drive Mk3):                            â”‚
â”‚ â”‚                  â”‚ â”‚ â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”        â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚ Resource         â”‚Required â”‚Available â”‚Shortfall â”‚        â”‚
â”‚ â”‚                  â”‚ â”‚ â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤        â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚ Refined Titanium â”‚    500  â”‚     320  â”‚     180  â”‚        â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚ Refined Copper   â”‚    200  â”‚     200  â”‚       0  â”‚        â”‚
â”‚ â”‚                  â”‚ â”‚ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜        â”‚
â”‚ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚                                                             â”‚
â”‚ [New] [Delete]       â”‚                                                             â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

Controls:
- `tlpBase` (TableLayoutPanel, 2 columns: 250px fixed / fill)
- Left: `flpSearchList` â†’ `txtPlanFilter` (ValidatedTextBox) + `lvwPlans` (ListView) + `cmdNew` / `cmdDelete`
- Right: `flpPlanData` â†’ `txtPlanName`, `txtDescription`, `chkActive` (CheckBox, write-through to BuildPlan.IsActive), `cmdSave`, `cmdAutoAssign`, `cmdAllocate`, `dgvBuildItems` (DataGridView), add-item panel, shortfall panel
- `lblStatusSummary` (Label) -- shows per-status item counts ("Staged: X | Delivering: Y | Ready: Z | InProgress: W | Completed: V") or "Complete (N items)" when all done. Updated on grid population and data change events.
- `cmdStartManufacturing` -- "Start Manufacturing" button, enabled when selected item is Ready with valid StructureUUID and passes CanStartManufacturing. Shares handler with `tsmiStartManufacturing` context menu item.
- `cmdStartAllReady` -- "Start All Ready" button, enabled when plan has any Ready items. Shares handler with `tsmiStartAllReady` context menu item.
- `cmsBuildItems` context menu on `dgvBuildItems` includes: Set Dependency, Clear Dependency, Start Manufacturing (`tsmiStartManufacturing`), Start All Ready (`tsmiStartAllReady`)
- `cmdGenerateDelivery` is a dropdown button (ToolStripSplitButton style) with three options:
  - "Resource Delivery (This Plan)" â€” generates delivery for the selected plan's shortfalls only
  - "Consolidated Resource Delivery..." â€” prompts to select multiple plans, generates one merged delivery plan for all resource shortfalls across selected plans
  - "Flatpack Delivery..." â€” prompts to select multiple plans, generates one delivery plan for completed flatpack items to their destination colonies
- Multi-plan selection uses a checklist dialog showing all active build plans. The user checks which plans to include.
- Inactive plans: list view shows plan name in gray italic. Detail panel is read-only (all controls disabled except the Active checkbox). Shortfall panel hidden.
- `dgvBuildItems` columns: Type, Item, Qty (editable), Location, Structure, Status, Recipient, Notes
- Add-item panel: `cmbItemType`, `cmbItem` (FilteredTextComboSet), `cmbResource` (FilteredTextComboSet), `cmbPurity`, `cmbSurvey` (FilteredTextComboSet, parallel value list: Survey UUID), `txtQuantity`, `txtRecipient`, `lblTargetDuration`, `txtTargetDuration`, `cmdQueueCalc` (next to duration), `cmdAddItem`, `cmdDeleteItem`
- Shortfall panel: `dgvShortfalls` (read-only DataGridView) â€” visible when a build item is selected

Wiring:
- Subscribes to CurrentPlayerChanged, ColonyDataChanged, BuildPlanDataChanged.
- Fires BuildPlanDataChanged after saves.
- Structure allocation uses a modal dialog (see below).

#### Structure Allocation Dialog

Modal dialog opened from the build items grid when the user clicks the Location/Structure cell.

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚ Allocate Structure                                [X]   â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚ Item: Reactor Mk3 (10 runs)                             â”‚
â”‚                                                         â”‚
â”‚ Filter: [__________]  [âœ“] Idle structures only          â”‚
â”‚                                                         â”‚
â”‚ â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚ â”‚ Location          â”‚ Structure    â”‚ Type   â”‚ Status  â”‚ â”‚
â”‚ â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤ â”‚
â”‚ â”‚ Alpha Prime       â”‚ Mfg Bay 1   â”‚ Mfg    â”‚ Idle    â”‚ â”‚
â”‚ â”‚ Alpha Prime       â”‚ Mfg Bay 2   â”‚ Mfg    â”‚ Busy    â”‚ â”‚
â”‚ â”‚ Beta Colony       â”‚ Mfg Bay 1   â”‚ Mfg    â”‚ Idle    â”‚ â”‚
â”‚ â”‚ Gamma Outpost     â”‚ Mfg Bay 1   â”‚ Mfg    â”‚ Idle    â”‚ â”‚
â”‚ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â”‚                                                         â”‚
â”‚                              [Allocate] [Cancel]        â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

Controls:
- `txtStructureFilter` (ValidatedTextBox), `chkIdleOnly` (CheckBox)
- `dgvStructures` (DataGridView, read-only) â€” columns: Location, Structure, Type, Status
- Currently only shows colony structures. When factory ships/stations are supported, the grid will also include ship and station manufacturing, refining, and research slots.
- `cmdAllocate`, `cmdCancel`

### FormStructureAllocation

`FormStructureAllocation` is the modal dialog for allocating build items to colony structures. It displays a filterable grid of available structures and lets the user select one for the build item's manufacturing/research/commodity location.



<!-- Extracted from .kiro/specs/empire-systems/design.md, lines 2824-2843 — Colony Administration Tab Build Plan Integration -->
### Colony Administration Tab — Build Plan Integration (Iteration 1)

The existing Administration tab on FormColonyV2 gains a "Generate Build Plan" button alongside the existing Bootstrap and Optimize buttons.

```
│ ┌─ Admin ─┬─ Structures ─┬─ Workers ─┬─ Warehousing ─┬─ Overflow ─────┐   │
│ │                                                                       │   │
│ │ [Bootstrap Colony] [Optimize Build Order] [Generate Build Plan]       │   │
│ │                                                                       │   │
│ │ ┌─────────────────────────────────────────────────────────────────┐   │   │
│ │ │ (admin report — existing)                                       │   │   │
│ │ └─────────────────────────────────────────────────────────────────┘   │   │
│ └───────────────────────────────────────────────────────────────────────┘   │
```

Controls:
- `cmdGenerateBuildPlan` — enabled when the colony has at least one unstaged, unbuilt structure. Disabled otherwise.
- On click: prompts user to create a new build plan or select an existing one (modal dialog with plan picker). Calls `BuildPlanService.GenerateColonyBuildItems()`. Shows confirmation with count of items added.
- The button is disabled when no colony is selected.
