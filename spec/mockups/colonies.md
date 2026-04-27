# Colony Structure Mockups

### ColonyStructureV2

UserControl embedded in FormColonyV2's Structures tab. One instance per colony structure, stacked vertically. Each structure card shows header, status, workers, survey/selection pickers, manufacturing controls, and reorder/delete commands.

```
+------------------------------------------------------------------------+
| **Blueprint Name #1**  [ ] Staged  [ ] Built  [ ] Online               |
|------------------------------------------------------------------------|
| Status: Mining Rig - Online - 2 workers - producing Reactives (High)   |
|------------------------------------------------------------------------|
| [x] Worker 1  [x] Worker 2  [ ] Worker 3  [ ] Worker 4                |
|------------------------------------------------------------------------|
| Survey:  [Survey Name                                             v]   |
|          (on focus: [Filter:____] [Survey Name                    v])  |
|------------------------------------------------------------------------|
| Select:  [Resource Name                                           v]   |
|          (on focus: [Filter:____] [Resource Name                  v])  |
|          [Qty] [ ] Stage Resources  [Start] [Done]                     |
|------------------------------------------------------------------------|
| Progress: Mining cycle 3/5   Completion: 02:15:30                      |
|------------------------------------------------------------------------|
| [Up] [Down] [Delete]                                                   |
+------------------------------------------------------------------------+
```

Controls:
- `flpColonyStructure` (FlowLayoutPanel, top-down, border) - outer container
- `flpHeader`: `lblName` (Label, bold), `chkStaged`, `chkBuilt`, `chkOnline` (CheckBoxes)
- `rtbStatus` (RichTextBox, read-only) - structure status summary
- `flpWorkers`: `chkWorker1`, `chkWorker2`, `chkWorker3`, `chkWorker4`, `chkWorker5`, `chkWorker6` (CheckBoxes, visible per structure type)
- `flpSurveySelection`: `lblSurveyFilter` (Label), `cmbSurvey` (FilteredTextComboSet - inline filter + combo for survey selection)
- `flpSelection`: `lblSelectionFilter` (Label), `cmbSelection` (FilteredTextComboSet - inline filter + combo for resource/blueprint selection), `txtQuantity` (ValidatedTextBox), `chkStageResources` (CheckBox), `cmdStart` (Button), `cmdDone` (Button)
- `flpManufacturing`: `rtbProgressStatus` (RichTextBox, read-only), `txtCompletionTime` (ValidatedTextBox)
- `flpStructureCommands`: `cmdUp`, `cmdDown`, `cmdDelete` (Buttons)
- `timerCountdown` (Timer) - countdown tick for manufacturing/mining progress

Notes:
- `cmbSurvey` and `cmbSelection` use FilteredTextComboSet (consolidated from former txtSurveyFilter+cmbSurvey and txtSelectionFilter+cmbSelection pairs). Shows full-width combo when unfocused, splits into filter TextBox + combo on focus.
- Worker checkboxes are shown/hidden based on structure type (mining rigs show workers, others may not).
- `flpSurveySelection` and `flpSelection` visibility depends on structure type.
- `flpManufacturing` visibility depends on whether the structure has an active timer.

Implements: REQ-COL-structure-management (colony structure UI)
Satisfies: Requirement 1 (survey picker FilteredTextComboSet), Requirement 2 (selection picker FilteredTextComboSet) from custom-control-adoption spec

### FormColonyV2

Main colony management form. Left panel: colony list with filter. Right panel: identity fields, tabbed detail area (Administration, Structures, Workers, Warehousing, Overflow), and command buttons.

Key custom controls (tasks 5-8 of custom-control-adoption spec):

**Structures tab:**
- `cmbFlatpacks` (FilteredTextComboSet) — flatpack picker for adding structures. Replaced former txtFilterFlatpack + cmbFlatpacks pair.

**Workers tab:**
- `dgvCommodityRequests` (DataEntryGridView) — commodity request grid with tab-navigation support.

**Warehousing tab:**
- `cmbItem` (FilteredTextComboSet) — item picker for adding warehouse items. Replaced former txtItemFilter + cmbItem pair. Type-cascade combo `cmbItemType` stays as standard ComboBox.
- `dgvItems` (DataEntryGridView) — warehouse items grid with tab-navigation support.

**Overflow tab:**
- `cmbOverflowResource` (FilteredTextComboSet) — resource picker for overflow rules. Replaced former txtOverflowResourceFilter + cmbOverflowResource pair.
- `cmbOverflowDest` (FilteredTextComboSet) — destination picker for overflow rules. Replaced former txtOverflowDestFilter + cmbOverflowDest pair.
- `cmbOverflowRoute` (FilteredTextComboSet) — route picker for overflow rules. Replaced former txtOverflowRouteFilter + cmbOverflowRoute pair.

Notes:
- All FilteredTextComboSet controls show full-width combo when unfocused, split into filter TextBox + combo on focus.
- Parallel UUID lists (`_flatpackBlueprints`, `_itemPickerObjects`, `_overflowDestUUIDs`, `_overflowRouteUUIDs`) maintain lookup indices for SelectedFullIndex.
- The overflow tab is also documented in `colony-overflow.md` (partial mockup covering the full overflow wireframe).

Satisfies: Requirements 6.1-6.5 (FormColonyV2 pickers), Requirement 10 (DataEntryGridView grids) from custom-control-adoption spec
