# Systems Mockups

### FormSystem

MDI child form. Split layout with system list (left) and detail panel (right). Search box above grid, editable faction fields and facility checkboxes in detail.

```
+---------------------------------------------------------------------------------+
| Systems                                                          [_][box][X]    |
+---------------------------------------------------------------------------------+
| +--- System List (left) --------+  +--- Detail Panel (right) ---------------+ |
| | [Search_____________________] |  |                                         | |
| |                               |  | Id:            42                        | |
| | Id  | Name       | Grid | Sp  |  | Name:          Sol Alpha                | |
| |-----|------------|------|-----|  | X:             125                       | |
| |  42 | Sol Alpha  | 5,3  | G2  |  | Y:             -47                      | |
| |  87 | Arcturus B | 2,7  | K5  |  | Quadrant:      Alpha                    | |
| | 103 | Vega Prime | 8,1  | A0  |  | Sector:        Core                     | |
| | 215 | Wolf 359   | 4,4  | M6  |  | Region:        Inner Rim               | |
| |     |            |      |     |  | Locality:      Central Hub              | |
| |     |            |      |     |  | Spectral Class: G2                      | |
| |     |            |      |     |  |                                         | |
| |     |            |      |     |  | Faction Name: [Terran Federation   ]    | |
| |     |            |      |     |  | Faction Color:[#3366CC             ]    | |
| |     |            |      |     |  |                                         | |
| |     |            |      |     |  | [x] Has Orbital                         | |
| |     |            |      |     |  | [x] Has Spaceport                       | |
| |     |            |      |     |  | [ ] Has Starbase                        | |
| |     |            |      |     |  |                                         | |
| |     |            |      |     |  | [Save] [Re-import]                      | |
| +-------------------------------+  +-----------------------------------------+ |
+---------------------------------------------------------------------------------+
```

### Control List — FormSystem

| Control | Type | Purpose |
|---------|------|---------|
| splitContainer | SplitContainer (Dock=Fill) | Divides list and detail panels |
| txtSearch | TextBox (Dock=Top) | Filter systems by name/faction |
| dgvSystems | DataGridView (Dock=Fill, ReadOnly) | System list grid |
| colId | DataGridViewTextBoxColumn | System numeric ID |
| colName | DataGridViewTextBoxColumn | System name |
| colGridLocation | DataGridViewTextBoxColumn | Grid coordinates |
| colSpectralClass | DataGridViewTextBoxColumn | Spectral classification |
| colFactionName | DataGridViewTextBoxColumn | Controlling faction |
| pnlDetail | Panel (Dock=Fill, AutoScroll) | Detail fields container |
| lblId | Label | "Id:" caption |
| lblIdValue | Label | System ID value |
| lblName | Label | "Name:" caption |
| lblNameValue | Label | System name value |
| lblX | Label | "X:" caption |
| lblXValue | Label | X coordinate value |
| lblY | Label | "Y:" caption |
| lblYValue | Label | Y coordinate value |
| lblQuadrant | Label | "Quadrant:" caption |
| lblQuadrantValue | Label | Quadrant value |
| lblSector | Label | "Sector:" caption |
| lblSectorValue | Label | Sector value |
| lblRegion | Label | "Region:" caption |
| lblRegionValue | Label | Region value |
| lblLocality | Label | "Locality:" caption |
| lblLocalityValue | Label | Locality value |
| lblSpectralClass | Label | "Spectral Class:" caption |
| lblSpectralClassValue | Label | Spectral class value |
| lblFactionName | Label | "Faction Name:" caption |
| txtFactionName | TextBox | Editable faction name |
| lblFactionColor | Label | "Faction Color:" caption |
| txtFactionColor | TextBox | Editable faction color (hex) |
| chkHasOrbital | CheckBox | Has Orbital facility toggle |
| chkHasSpaceport | CheckBox | Has Spaceport facility toggle |
| chkHasStarbase | CheckBox | Has Starbase facility toggle |
| btnSave | Button | Save edits to system data |
| btnReimport | Button | Re-import system from game data |
