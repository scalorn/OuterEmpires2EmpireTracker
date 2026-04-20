# Colony Daily Build Mockups

### FormColonyDailyBuild

MDI child form. Route-based build initiation — select a delivery route, see all colonies on that route with staged structures ready to build.

```
┌──────────────────────────────────────────────────────────────────────────────────────┐
│ Colony Daily Build                                                       [_][□][X]│
├──────────────┬───────────────────────────────────────────────────────────────────────┤
│ Route        │  ┌─ Alpha Prime ──────────────────────────────────────────────────┐  │
│ [__________] │  │ ┌──────────────────┬──────────┬───────────┬──────────────────┐ │  │
│ [Main Run ▼] │  │ │ Structure        │ Item     │ Status    │ Countdown        │ │  │
│              │  │ ├──────────────────┼──────────┼───────────┼──────────────────┤ │  │
│              │  │ │ MfgBay1          │ Reactor  │ Staged    │ 1d 4h 30m 0s     │ │  │
│              │  │ │ MfgBay2          │ Drive Mk3│ Staged    │ 0d 8h 15m 0s     │ │  │
│              │  │ └──────────────────┴──────────┴───────────┴──────────────────┘ │  │
│              │  │ [Build All]                                                    │  │
│              │  └────────────────────────────────────────────────────────────────┘  │
│              │                                                                     │
│              │  ┌─ Beta Colony ───────────────────────────────────────────────────┐  │
│              │  │ ┌──────────────────┬──────────┬───────────┬──────────────────┐ │  │
│              │  │ │ Structure        │ Item     │ Status    │ Countdown        │ │  │
│              │  │ ├──────────────────┼──────────┼───────────┼──────────────────┤ │  │
│              │  │ │ CommFac1         │ Fuel Cell│ Staged    │ 2d 0h 0m 0s      │ │  │
│              │  │ └──────────────────┴──────────┴───────────┴──────────────────┘ │  │
│              │  │ [Build All]                                                    │  │
│              │  └────────────────────────────────────────────────────────────────┘  │
│              │                                                                     │
│              │  ┌─ Gamma Outpost ─────────────────────────────────────────────────┐  │
│              │  │ (no staged structures)                                          │  │
│              │  └────────────────────────────────────────────────────────────────┘  │
└──────────────┴───────────────────────────────────────────────────────────────────────┘
```

Controls:
- `flpBase` (FlowLayoutPanel, left-to-right, Dock=Fill, WrapContents=false)
- `flpSelectors` (FlowLayoutPanel, top-down, 220px wide) — route selection:
  - `lblRoute` (Label, bold) — "Route"
  - `txtRouteFilter` (ValidatedTextBox) — filters route combo
  - `cmbRoute` (ComboBox, DropDownList) — selects delivery route
- `pnlContent` (FlowLayoutPanel, top-down, AutoScroll, WrapContents=false) — scrollable colony sections
  - Dynamically populated with one GroupBox per colony on the selected route
  - Each colony section contains a DataGridView (Structure, Item, Status, Countdown columns) and a [Build All] button
  - Colonies with no staged structures show "(no staged structures)"

Satisfies: REQ-COL-080 (daily build workflow)
