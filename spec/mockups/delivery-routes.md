# Delivery Route Mockups

### FormDeliveryRoute

MDI child form. Left-list / right-detail with tabbed stops and plan management.

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│ Delivery Routes                                                         [_][□][X]│
├──────────────────────┬──────────────────────────────────────────────────────────────┤
│ Filter: [__________] │ Route Name: [Main Supply Run_______]                        │
│                      │                                                             │
│ ┌──────────────────┐ │ ┌─ Stops ─┬─ Plan ──────────────────────────────────────┐   │
│ │▸ Main Supply Run │ │ │                                                       │   │
│ │  Fuel Circuit    │ │ │ ┌──┬────────┬─────────────┬────────┬────────┬────────┐│   │
│ │  Ore Haul        │ │ │ │# │ Type   │ Destination │ Planet │ System │Fuel Est││   │
│ │                  │ │ │ ├──┼────────┼─────────────┼────────┼────────┼────────┤│   │
│ │                  │ │ │ │1 │ Colony │ Alpha Prime │ Kepler │ K-442  │   12   ││   │
│ │                  │ │ │ │2 │ Colony │ Beta Colony │ Sol-3  │ Sol    │    8   ││   │
│ │                  │ │ │ │3 │ Station│ Gamma Depot │ —      │ Proxima│   15   ││   │
│ │                  │ │ │ └──┴────────┴─────────────┴────────┴────────┴────────┘│   │
│ │                  │ │ │                                                       │   │
│ │                  │ │ │ Add: Type:[Colony▼] [Alpha Prime     ▼] [Add]        │   │
│ │                  │ │ │ ☑ Prevent Duplicates  [▲] [▼] [Remove]               │   │
│ │                  │ │ └───────────────────────────────────────────────────────┘   │
│ │                  │ │                                                             │
│ └──────────────────┘ │ [New] [Save] [Delete]                                      │
└──────────────────────┴──────────────────────────────────────────────────────────────┘
```

Plan tab:

```
│ ┌─ Stops ─┬─ Plan ──────────────────────────────────────────────────────────────┐   │
│ │                                                                               │   │
│ │ ☑ShowCompleted [________] [Resupply Run #4    ▼] [New][Delete][Execute][Auto] │   │
│ │ Plan Name: [Resupply Run #4_______]                                           │   │
│ │ Stop: Alpha Prime (Colony)                                                    │   │
│ │                                                                               │   │
│ │ Drop Off:                                                                     │   │
│ │ ┌──────────┬──────────────────────┬──────┐                                    │   │
│ │ │ Type     │ Item                 │ Qty  │                                    │   │
│ │ ├──────────┼──────────────────────┼──────┤                                    │   │
│ │ │ Resource │ Refined Titanium     │  200 │                                    │   │
│ │ └──────────┴──────────────────────┴──────┘                                    │   │
│ │ [Resource▼] [Refined Titanium ▼] [Purity▼] [200] [Add] [Remove]              │   │
│ │                                                                               │   │
│ │ Pick Up:                                                                      │   │
│ │ ┌──────────┬──────────────────────┬──────┐                                    │   │
│ │ │ Type     │ Item                 │ Qty  │                                    │   │
│ │ ├──────────┼──────────────────────┼──────┤                                    │   │
│ │ │ Commodity│ Fuel Cells           │   50 │                                    │   │
│ │ └──────────┴──────────────────────┴──────┘                                    │   │
│ │ [Commodity▼] [Fuel Cells      ▼] [50] [Add] [Remove]                         │   │
│ └───────────────────────────────────────────────────────────────────────────────┘   │
```

Controls:
- `flpBase` (FlowLayoutPanel, left-to-right, Dock=Fill, WrapContents=false)
- Left: `flpSearchList` → `txtRouteFilter` (ValidatedTextBox) + `lvwRoutes` (ListView)
- Right: `flpRouteData` (top-down) → `txtRouteName` (ValidatedTextBox), `tabRouteDetail` (TabControl)
  - Stops tab: `dgvStops` (DataGridView, read-only) with columns: Sequence, DestType, ColonyName, PlanetName, SystemName, Purpose, FuelEstimate
    - `flpAddStop`: `cmbDestType`, `cmbStopPurpose`, `cmbColony` (FilteredTextComboSet — inline filter for destination selection), `cmdAddStop`, `cmdUp`/`cmdDown`, `cmdRemoveStop`, `chkPreventDuplicates`
  - Plan tab: `flpPlanContent` (top-down)
    - `flpPlanSelector`: `chkShowCompleted`, `txtPlanFilter`, `cmbPlan`, `cmdNewPlan`, `cmdDeletePlan`, `cmdExecutePlan`, `cmdAutoFill`
    - `txtPlanName`, `lblPlanStop` (bold, shows selected stop context)
    - Drop Off: `dgvDropOff` (DataEntryGridView, Type/Item/Qty), add row with `cmbDropItemType`/`cmbDropItem` (FilteredTextComboSet)/`cmbDropPurity`/`txtDropQty`/`cmdAddDropOff`/`cmdRemoveDropOff`
    - Pick Up: `dgvPickUp` (DataEntryGridView, Type/Item/Qty), add row with `cmbPickItemType`/`cmbPickItem` (FilteredTextComboSet)/`cmbPickPurity`/`txtPickQty`/`cmdAddPickUp`/`cmdRemovePickUp`
- Bottom: `flpCommands` → `cmdNew`, `cmdSave`, `cmdDelete`

Satisfies: REQ-DEL-010 (delivery route management), REQ-DEL-020 (delivery plan management)
