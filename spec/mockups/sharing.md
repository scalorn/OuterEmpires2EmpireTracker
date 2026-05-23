# Sharing Mockups

Implements: REQ from .kiro/specs/sharing-visibility-system

### FormSharing

MDI child form. DataGridView with inline editing for sharing rules.

```
+-------------------------------------------------------------+
| FormSharing                                        [_][o][X] |
+-------------------------------------------------------------+
| +-----------------------------------------------------------+|
| | DataGridView (dgvRules)                                   ||
| | +-----------+---------------------------+-----------+     ||
| | |Target Type| Target UUID               | Data Type |     ||
| | +-----------+---------------------------+-----------+     ||
| | | Faction v | abc-123-def               |Blueprints v|    ||
| | | Public  v | (auto)                    | All       v|    ||
| | +-----------+---------------------------+-----------+     ||
| +-----------------------------------------------------------+|
| +-----------------------------------------------------------+|
| | [Add Rule]  [Delete Selected]  [Save]  [Reload]           ||
| +-----------------------------------------------------------+|
+-------------------------------------------------------------+
```

#### Controls

| Control | Type | Notes |
|---------|------|-------|
| dgvRules | DataGridView | Dock=Fill, inline editing, SelectionMode=FullRowSelect |
| colTargetType | DataGridViewComboBoxColumn | Items: Faction, Character, Public |
| colTargetUUID | DataGridViewTextBoxColumn | Editable text, width 250 |
| colDataType | DataGridViewComboBoxColumn | Items: All, Colonies, Blueprints, Surveys |
| pnlButtons | Panel | Dock=Bottom, height 40 |
| btnAddRule | Button | Text="Add Rule" |
| btnDeleteSelected | Button | Text="Delete Selected" |
| btnSave | Button | Text="Save" |
| btnReload | Button | Text="Reload" |
