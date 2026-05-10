# Survey Mockups

### FormSurvey

MDI child form. Left-list / right-detail with rich filtering and resource grid.

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│ Surveys                                                                 [_][□][X]│
├─────────────────────────────┬───────────────────────────────────────────────────────┤
│ Filter:   [______________]  │ Survey Type: [Planet         ▼]                       │
│ Resource: [All          ▼]  │ System:      [Kepler-442__________]                   │
│ Type: [All▼] Purity:[All▼]  │ Planet Name: [Kepler-442b________]                   │
│ Min Amount: [_____]         │ Survey ID:   [SVY-00142___________]                   │
│                             │ Nickname:    [Rich Iron Site_______]                   │
│ ┌─────────────────────────┐ │ Scanner BP:  [Deep Scanner Mk3              ▼]        │
│ │▸ Kepler-442b (SVY-142) │ │ Scanned By:  [Captain Kirk_________]                  │
│ │  Sol-3 (SVY-089)       │ │ Scan Date:   [2024-03-15 14:30___] [📅]               │
│ │  Proxima-b (SVY-201)   │ │ Sensor Abund:[85_____]                                │
│ │  Barnard-c (SVY-055)   │ │ Purity Mod:  [1.2____]                                │
│ │                         │ │ Scan Level:  [3______]                                │
│ │                         │ │                                                      │
│ │                         │ │ ┌──────────────────┬──────────┬──────────┬─────────────┐│
│ │                         │ │ │ Resource         │ Purity   │ Amount   │ Max Reserve ││
│ │                         │ │ ├──────────────────┼──────────┼──────────┼─────────────┤│
│ │                         │ │ │ Iron             │ High     │    12000 │       7,123 ││
│ │                         │ │ │ Copper           │ Medium   │     8500 │       6,998 ││
│ │                         │ │ │ Titanium         │ Low      │     3200 │       6,420 ││
│ │                         │ │ │ Silicon          │ High     │     9800 │             ││
│ │                         │ │ └──────────────────┴──────────┴──────────┴─────────────┘│
│ │                         │ │                                                      │
│ └─────────────────────────┘ │ [New] [Save] [Delete] [Import]                       │
└─────────────────────────────┴───────────────────────────────────────────────────────┘
```

Controls:
- `flpBase` (FlowLayoutPanel, left-to-right, Dock=Fill, WrapContents=false)
- Left: `flpSearchList` (427px wide):
  - `txtSurveyFilter` (ValidatedTextBox) — text filter
  - `cmbResource` (ComboBox, DropDownList) — filter by resource type
  - `flpTypeFilter`: `cmbSurveyType` (ComboBox), `cmbPurityFilter` (ComboBox), `txtMinAmount` (ValidatedTextBox)
  - `lvwSurveys` (ListView, full-row select)
- Right: `tabSurveyContent` (TabControl wrapping survey details and distribution):
  - Tab "Survey Details" (`tabSurveyDetails`):
    - `flpSurveyData` (top-down, Dock=Fill):
      - `flpSurveyDetails` (top-down) — survey identity fields:
        - `cmbSurveyTypeEdit` (Planet/Asteroid dropdown — label changes to "Asteroid Name" when Asteroid selected), `txtSystemName`, `txtPlanetName`, `txtSurveyID`, `txtNickName`
        - `cmbScannerBlueprint` (FilteredTextComboSet) — scanner blueprint picker with inline filtering
        - `txtScannedBy` — player who performed the scan
        - `txtScanDateTime` + `dtpScanDateTime` (DateTimePicker) — scan timestamp
        - `txtSensorAbundance`, `txtPurityModifier`, `txtScanLevel`
      - `dgvResources` (DataEntryGridView, editable, Tab-navigates between editable cells):
        - `Resource` (DataGridViewFilteredComboBoxColumn) — resource name with inline filtering
        - `Purity` (ComboBoxColumn) — purity level
        - `Amount` (ValidatedTextBoxColumn) — quantity
        - `MaxReserve` (TextBoxColumn, read-only) — max reserve from linked asteroid (asteroid surveys only; empty for planet surveys)
      - `flpCommands`: `cmdNew`, `btnSave`, `cmdDelete`, `cmdImport`
  - Tab "Yield Distribution" (`tabDistribution`):
    - `pnlDistControls` (FlowLayoutPanel, Dock=Top): `cmbDistResource`, `cmbDistPurity`, `btnAddSeries`, `btnRemoveSeries`, `lblBinWidth`, `nudBinWidth`
    - `lstDistSeries` (ListBox, Dock=Left, Width=200) — series legend
    - `chartDistribution` (Chart, Dock=Fill) — Spline chart with distribution curves
    - `lblDistMessage` (Label, Dock=Fill, centered) — empty state messages (hidden when chart has data)

Satisfies: REQ-SRV-010 (survey management), REQ-SRV-020 (survey import)
