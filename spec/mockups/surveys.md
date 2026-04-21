# Survey Mockups

### FormSurvey

MDI child form. Left-list / right-detail with rich filtering and resource grid.

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│ Surveys                                                                 [_][□][X]│
├─────────────────────────────┬───────────────────────────────────────────────────────┤
│ Filter:   [______________]  │ Survey Type: [Planet         ▼]                       │
│ Resource: [All          ▼]  │ Planet Name: [Kepler-442b________]                   │
│ Type: [All▼] Purity:[All▼]  │ System:      [Kepler-442__________]                   │
│ Min Amount: [_____]         │ Survey ID:   [SVY-00142___________]                   │
│                             │ Nickname:    [Rich Iron Site_______]                   │
│ ┌─────────────────────────┐ │ Scanner BP:  [____] [Deep Scanner Mk3       ▼]        │
│ │▸ Kepler-442b (SVY-142) │ │ Scanned By:  [Captain Kirk_________]                  │
│ │  Sol-3 (SVY-089)       │ │ Scan Date:   [2024-03-15 14:30___] [📅]               │
│ │  Proxima-b (SVY-201)   │ │ Sensor Abund:[85_____]                                │
│ │  Barnard-c (SVY-055)   │ │ Purity Mod:  [1.2____]                                │
│ │                         │ │ Scan Level:  [3______]                                │
│ │                         │ │                                                      │
│ │                         │ │ ┌──────────────────┬──────────┬──────────┐            │
│ │                         │ │ │ Resource         │ Purity   │ Amount   │            │
│ │                         │ │ ├──────────────────┼──────────┼──────────┤            │
│ │                         │ │ │ Iron             │ High     │    12000 │            │
│ │                         │ │ │ Copper           │ Medium   │     8500 │            │
│ │                         │ │ │ Titanium         │ Low      │     3200 │            │
│ │                         │ │ │ Silicon          │ High     │     9800 │            │
│ │                         │ │ └──────────────────┴──────────┴──────────┘            │
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
- Right: `flpSurveyData` (top-down, AutoScroll, WrapContents=false):
  - `flpSurveyDetails` (top-down) — survey identity fields:
    - `cmbSurveyTypeEdit` (Planet/Asteroid dropdown — label changes to "Asteroid Name" when Asteroid selected), `txtPlanetName`, `txtSystemName`, `txtSurveyID`, `txtNickName`
    - `txtFilterScannerBlueprint` + `cmbScannerBlueprint` (filtered combo for scanner blueprint)
    - `txtScannedBy` — player who performed the scan
    - `txtScanDateTime` + `dtpScanDateTime` (DateTimePicker) — scan timestamp
    - `txtSensorAbundance`, `txtPurityModifier`, `txtScanLevel`
  - `dgvResources` (DataGridView, editable):
    - `Resource` (ComboBoxColumn) — resource name
    - `Purity` (ComboBoxColumn) — purity level
    - `Amount` (ValidatedTextBoxColumn) — quantity
  - `flpCommands`: `cmdNew`, `btnSave`, `cmdDelete`, `cmdImport`

Satisfies: REQ-SRV-010 (survey management), REQ-SRV-020 (survey import)
