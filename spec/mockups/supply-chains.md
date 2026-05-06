<!-- Extracted from .kiro/specs/empire-systems/design.md -->
# Supply Chain Mockups

### FormSupplyChain (Iteration 6)

MDI child form. Left-list / right-detail pattern with a visual stage editor.

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚ #1 - Supply Chains                                                      [_][â–¡][X]â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚ Filter: [__________] â”‚ Name: [Iron Pipeline__________]  â˜‘ Active               â”‚
â”‚                      â”‚                                                         â”‚
â”‚ â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚ Stages:                                                 â”‚
â”‚ â”‚â–¸ Iron Pipeline   â”‚ â”‚ â”Œâ”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚ â”‚  Copper Chain    â”‚ â”‚ â”‚ Seq â”‚ Type       â”‚ Location     â”‚ Resource â”‚Threshldâ”‚ â”‚
â”‚ â”‚  Titanium Flow   â”‚ â”‚ â”œâ”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”¤ â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚   1 â”‚ Mine       â”‚ Alpha Prime  â”‚ Iron     â”‚        â”‚ â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚   2 â”‚ Mine       â”‚ Beta Colony  â”‚ Iron     â”‚        â”‚ â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚   3 â”‚ AsteroidMn â”‚ Asteroid K-7 â”‚ Iron     â”‚        â”‚ â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚   4 â”‚ PickUp     â”‚ Station Alphaâ”‚ Iron(unr)â”‚   5000 â”‚ â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚   5 â”‚ Refine     â”‚ Gamma Colony â”‚ Iron(ref)â”‚   3000 â”‚ â”‚
â”‚ â”‚                  â”‚ â”‚ â”‚   6 â”‚ Deliver    â”‚ Station Beta â”‚ Iron(ref)â”‚        â”‚ â”‚
â”‚ â”‚                  â”‚ â”‚ â””â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â”‚ â”‚                  â”‚ â”‚                                                         â”‚
â”‚ â”‚                  â”‚ â”‚ Add/Edit Stage:                                         â”‚
â”‚ â”‚                  â”‚ â”‚ Seq:[4] Type:[PickUp      â–¼]                            â”‚
â”‚ â”‚                  â”‚ â”‚ Location Type:[Stationâ–¼] Location:[Station Alpha    â–¼]  â”‚
â”‚ â”‚                  â”‚ â”‚ Resource:[Filter:___] [Iron â–¼] Purity:[Unrefined â–¼]    â”‚
â”‚ â”‚                  â”‚ â”‚ Threshold:[5000]  Rate/hr:[250]                         â”‚
â”‚ â”‚                  â”‚ â”‚ [Add Stage] [Update Stage] [Remove Stage]               â”‚
â”‚ â”‚                  â”‚ â”‚ [â–² Move Up] [â–¼ Move Down]                               â”‚
â”‚ â”‚                  â”‚ â”‚                                                         â”‚
â”‚ â”‚                  â”‚ â”‚ Flow Summary:                                           â”‚
â”‚ â”‚                  â”‚ â”‚ Mine(3 sources) â†’ PickUp@Stn Alpha(5000) â†’            â”‚
â”‚ â”‚                  â”‚ â”‚   Refine@Gamma(3000) â†’ Deliver@Stn Beta                â”‚
â”‚ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚                                                         â”‚
â”‚ [New] [Delete]       â”‚                                                         â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚ [Save] [Delete]                                                                â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

Controls:
- Left: `flpSearchList` â†’ `txtChainFilter` + `lvwSupplyChains` (ListView) + `cmdNew` / `cmdDelete`
- Right: `flpChainData` â†’ `txtChainName`, `chkChainActive`, `cmdSave` (CheckBox, write-through to SupplyChain.IsActive), `dgvStages` (DataGridView), add/edit stage panel, flow summary label
- Inactive chains: list view shows chain name in gray italic. Background processor skips inactive chains entirely.
- `dgvStages` columns: Sequence, StageType, Location, Resource (with purity), AccumulationThreshold, ProductionRatePerHour
- Add/edit panel: `txtSequence`, `cmbStageType`, `cmbLocationType`, `cmbLocation` (FilteredTextComboSet, parallel value list: Location UUID — populates with colonies/stations/asteroids based on type), `cmbResource` (FilteredTextComboSet), `cmbPurity`, `txtThreshold`, `txtRate`, `cmdAddStage` / `cmdUpdateStage` / `cmdRemoveStage`, `cmdMoveUp` / `cmdMoveDown`, `cmbRoute` (FilteredTextComboSet, parallel value list: Route UUID)
- `txtFlowSummary`: read-only label showing a condensed text representation of the pipeline stages. Auto-generated from the stages list.
- Stage type determines which fields are relevant: Mine/AsteroidMine stages have no threshold (they produce continuously). PickUp stages have a threshold (trigger delivery when accumulated). Refine stages have a threshold. Research stages track evolution progress. Deliver stages are the terminal destination. Location type can be Colony, Station, or Ship (Ship for future factory ships).

