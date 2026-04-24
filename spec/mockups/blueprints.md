# Blueprint Mockups

### FormBlueprintV2

MDI child form. SplitContainer with left-list / right-detail, tabbed detail area with properties, resources, evolution graph, and price evolution.

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│ Blueprints                                                              [_][□][X]│
├────────────────────────────────┬────────────────────────────────────────────────────┤
│ [____________________]        │ Name:      [Reactor Mk3__________]                 │
│ Type:[All▼] Class:[All▼]     │ Nickname:  [Best Reactor__________]                 │
│ Tech:[All▼] Evo:[All▼] ☑≥   │ Desc:      [High output reactor___]                 │
│ [Clear Filters]               │ Copy Cost: [15000________________]                 │
│                               │ Type: [Reactor    ▼]  Class: [Capital  ▼]          │
│ ┌────┬────────────┬───┬───┬─┐│ Tech: [3          ▼]  Evo:   [2        ▼]          │
│ │Type│ Name       │TL │Evo│R││ Base BP: [____] [Reactor Mk2              ▼]        │
│ ├────┼────────────┼───┼───┼─┤│ ☑ Global Blueprint                                 │
│ │Reac│ Reactor Mk3│ 3 │ 2 │4││                                                    │
│ │Driv│ Drive Mk3  │ 3 │ 1 │2││ [New] [Save] [Delete] [Import] [Import Market]     │
│ │Hull│ Hull Clip.  │ 2 │ 3 │1││                                                    │
│ │Weap│ Laser Mk2  │ 2 │ 1 │3││ ┌─ Properties ─┬─ Resources ─┬─ Evolution Graph ─┬─ Price Evolution ─┐│
│ │Shld│ Shield Mk1 │ 1 │ 1 │0││ │                                                 ││
│ │    │            │   │   │ ││ │ ┌──────────────────┬──────────────────┐           ││
│ │    │            │   │   │ ││ │ │ Property         │ Value            │           ││
│ │    │            │   │   │ ││ │ ├──────────────────┼──────────────────┤           ││
│ │    │            │   │   │ ││ │ │ Output           │ 450              │           ││
│ │    │            │   │   │ ││ │ │ Efficiency       │ 92%              │           ││
│ │    │            │   │   │ ││ │ │ Mass             │ 120              │           ││
│ │    │            │   │   │ ││ │ │ Volume           │ 80               │           ││
│ │    │            │   │   │ ││ │ └──────────────────┴──────────────────┘           ││
│ └────┴────────────┴───┴───┴─┘│ │                                                 ││
│                               │ │ Pricing: [Standard Rates▼] Computed: 12,500 cr  ││
│                               │ └─────────────────────────────────────────────────┘│
└────────────────────────────────┴────────────────────────────────────────────────────┘
```

Controls:
- `splitMain` (SplitContainer, Dock=Fill, SplitterDistance=380)
- Left panel: `flpSearchList` (top-down, Dock=Fill):
  - `txtFilter` (ValidatedTextBox) — text filter
  - `flpFilterPanel` (top-down):
    - Row 1: `cmbFilterType` (ComboBox), `cmbFilterClass` (ComboBox)
    - Row 2: `cmbFilterTechLevel` (ComboBox), `cmbFilterEvolution` (ComboBox), `chkFilterEvoAndAbove` (CheckBox "≥")
    - `btnClearFilters` (Button)
  - `lvwBlueprints` (ListView) — columns: Type, Name, TechLevel, Evolution, NickName, Refs
- Right panel: `flpBlueprintData` (top-down):
  - `flpIdentity` — identity fields:
    - `txtName`, `txtNickName`, `txtDescription`, `txtCopyCost`
    - `cmbBlueprintType`, `cmbShipClass`, `cmbTechLevel`, `cmbEvolution`
    - `txtFilterBaseBlueprint` + `cmbBaseBlueprint` (filtered combo for base/parent blueprint)
    - `chkGlobalBlueprint` (CheckBox) — marks blueprint as shared/global
  - `flpCommands`: `btnNew`, `btnSave`, `btnDelete`, `btnImport`, `btnImportMarket`
  - `tabDetailedData` (TabControl):
    - Properties tab: `dgvStatistics` (DataEntryGridView) — Property/Value columns (editable)
    - Resources tab: `dgvResources` (DataGridView) — Resource (ComboBox)/Amount columns, `btnAddResource`/`btnDeleteResource`
    - Evolution Graph tab: `chartEvolution` (Chart) — line chart of property changes across evolutions, `pnlPropertyCheckboxes` (toggle which properties to graph), `lblNoChanges`
    - Price Evolution tab: `chartPriceEvolution` (Chart) — line chart of manufacturing cost across evolutions, `cmbPriceEvoPlan` (ComboBox — pricing plan selector), `lblPriceEvoNoPlan` (Label — shown when no plan selected)
  - `flpPricing`: `cmbPricingPlan` (ComboBox), `lblComputedPrice` (Label) — computed price from selected pricing plan

Satisfies: REQ-BPR-010 (blueprint management), REQ-BPR-020 (evolution tracking), REQ-BPR-030 (blueprint import)
