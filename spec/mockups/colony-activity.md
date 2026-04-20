# Colony Activity Mockups

### FormColonyActivity

MDI child form. Read-only aggregation showing all active timers and commodity requests across colonies.

```
┌──────────────────────────────────────────────────────────────────────────────────────┐
│ Colony Activity                                                          [_][□][X]│
├──────────────────────────────────────────────────────────────────────────────────────┤
│ ☑Building ☑Manufacturing ☑CommodityMfg ☑CommodityReq ☑Research □Mining □Refining  │
│ ☑ImportStaleness □ShowInactive  Filter:[____________________]                      │
├──────────────────────────────────────────────────────────────────────────────────────┤
│ ┌───────────┬─────────────┬─────────────┬──────────────┬──────────────┬────────────┐│
│ │ CountDown │ System      │ Colony      │ Activity     │ Source       │ Details    ││
│ ├───────────┼─────────────┼─────────────┼──────────────┼──────────────┼────────────┤│
│ │ 0d 2h 15m │ Kepler-442  │ Alpha Prime │ Building     │ MfgBay1      │ Reactor Mk3││
│ │ 0d 0h 45m │ Kepler-442  │ Alpha Prime │ Manufacturing│ MfgBay2      │ Drive Mk3  ││
│ │ 1d 3h 00m │ Sol         │ Beta Colony │ Research     │ ResearchLab1 │ Hull Mk4   ││
│ │ OVERDUE   │ Sol         │ Beta Colony │ CommodityReq │ CommFac1     │ Fuel Cells ││
│ │ 2d 0h 00m │ Proxima     │ Gamma Out.  │ CommodityMfg │ CommFac2     │ Munitions  ││
│ │ STALE 3d  │ Proxima     │ Gamma Out.  │ ImportStale  │ —            │ Last import││
│ │           │             │             │              │              │            ││
│ └───────────┴─────────────┴─────────────┴──────────────┴──────────────┴────────────┘│
└──────────────────────────────────────────────────────────────────────────────────────┘
```

Controls:
- `flpBase` (FlowLayoutPanel, top-down, Dock=Fill)
- `flpFilters` (FlowLayoutPanel, left-to-right) — activity type checkboxes:
  - `chkBuilding` (CheckBox, checked by default)
  - `chkManufacturing` (CheckBox, checked by default)
  - `chkCommodityManufacturing` (CheckBox, checked by default)
  - `chkCommodityRequest` (CheckBox, checked by default)
  - `chkResearch` (CheckBox, checked by default)
  - `chkMining` (CheckBox, unchecked by default)
  - `chkRefining` (CheckBox, unchecked by default)
  - `chkColonyImportStaleness` (CheckBox, checked by default) — shows stale colony imports
  - `chkShowInactive` (CheckBox, unchecked) — toggles idle/inactive structures
  - `txtFilter` (ValidatedTextBox) — text filter across all columns
- `dgvActivities` (DataGridView, read-only, full-row select, no row headers)
  - Columns: CountDown, SystemName, ColonyName, ActivityType, Source, ProcessDetails
  - Hidden column: `colSecondsRemaining` (used for sorting)
- `timerRefresh` (Timer, Interval=1000ms) — refreshes countdown display every second

Satisfies: REQ-COL-070 (colony activity aggregation)
