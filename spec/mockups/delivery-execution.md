# Delivery Execution Mockups

### FormDeliveryExecution

MDI child form. Step-by-step execution of a delivery plan with load summary and stop-by-stop checklist.

```
┌──────────────────────────────────────────────────────────────────────────────────────┐
│ Delivery Execution                                                       [_][□][X]│
├──────────────┬───────────────────────────────────────────────────────────────────────┤
│ Route        │  Load Before Departure                                              │
│ [__________] │  Volume: 450 / 800 m³   Mass: 1200 / 2000 kg                       │
│ [Main Run ▼] │  [Split Trips]                                                      │
│              │  ┌──────────┬──────────────┬──────────────────────┬──────┐           │
│ Plan         │  │ Type     │ Name         │ Extended Name        │ Qty  │           │
│ [__________] │  ├──────────┼──────────────┼──────────────────────┼──────┤           │
│ [Run #4   ▼] │  │ Resource │ Ref Titanium │ Refined Titanium HP  │  200 │           │
│              │  │ Commodity│ Fuel Cells   │ Fuel Cells           │   50 │           │
│ Ship         │  │ Blueprint│ Reactor Mk3  │ Reactor Mk3 Evo2     │   10 │           │
│ [Hauler1  ▼] │  └──────────┴──────────────┴──────────────────────┴──────┘           │
│ Cap: 800 m³  │                                                                     │
│              │  ── Stop 1: Alpha Prime (Colony) ──────────────────────────          │
│ [Complete]   │  Drop Off:                                                          │
│ [Delete]     │  ☑ Refined Titanium HP ×200                                         │
│              │  ☑ Reactor Mk3 Evo2 ×10                                             │
│              │  Pick Up:                                                            │
│              │  ☐ Fuel Cells ×50                                                    │
│              │                                                                     │
│              │  ── Stop 2: Beta Colony (Colony) ──────────────────────────          │
│              │  Drop Off:                                                          │
│              │  ☐ Fuel Cells ×25                                                    │
│              │  Pick Up:                                                            │
│              │  (none)                                                              │
└──────────────┴───────────────────────────────────────────────────────────────────────┘
```

Controls:
- `flpBase` (FlowLayoutPanel, left-to-right, Dock=Fill, WrapContents=false)
- `flpSelectors` (FlowLayoutPanel, top-down, 220px wide):
  - `lblRoute` (Label, bold), `txtRouteFilter` (ValidatedTextBox), `cmbRoute` (ComboBox)
  - `lblPlan` (Label, bold), `txtPlanFilter` (ValidatedTextBox), `cmbPlan` (ComboBox)
  - `lblShip` (Label, bold), `cmbShip` (ComboBox), `lblShipCapacity` (Label, dynamic text)
  - `cmdCompletePlan` (Button), `cmdDeletePlan` (Button)
- `pnlExecution` (FlowLayoutPanel, top-down, AutoScroll, WrapContents=false):
  - `lblLoadListHeader` (Label, bold "Load Before Departure")
  - `lblCargoVolume` (Label), `lblCargoMass` (Label) — cargo summary
  - `cmdSplitTrips` (Button, visible when cargo exceeds ship capacity)
  - `dgvLoadList` (DataGridView, read-only) — columns: Type, Name, ExtendedName, Qty
  - `flpStops` (FlowLayoutPanel, top-down, auto-size) — dynamically populated per stop
    - Each stop: header label, drop-off checkboxes, pick-up checkboxes

Satisfies: REQ-DEL-030 (delivery execution), REQ-DEL-040 (trip splitting)
