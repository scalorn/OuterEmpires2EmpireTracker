# Player Profile Mockups

### FormPlayerProfile

MDI child form. Left-list / right-detail with scrollable skill groups.

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│ Player Profiles                                                         [_][□][X]│
├─────────────────────────────┬───────────────────────────────────────────────────────┤
│ Name: [________________]    │ Player Name: [Captain Kirk_______]                    │
│ Resource: [All          ▼]  │ Total Credits: [1250000__________]                    │
│                             │ Faction: [____] [The Space Pirates         ▼]         │
│ ┌─────────────────────────┐ │                                                      │
│ │▸ Captain Kirk           │ │ Public Rank:  [Citizen_____] XP:[1200] Next:[2000]   │
│ │  Alice the Builder      │ │ Private Rank: [Freelancer__] XP:[800]  Next:[1500]   │
│ │  Bob the Miner          │ │ Military Rank:[Recruit_____] XP:[100]  Next:[500]    │
│ │                         │ │ Skill Points: [42]                                   │
│ │                         │ │                                                      │
│ │                         │ │ ── Colony Director ☑ ──────────────────────────       │
│ │                         │ │   Human Resources  [3/5] ████████░░                  │
│ │                         │ │   Foreman           [5/5] ██████████                  │
│ │                         │ │                                                      │
│ │                         │ │ ── Colony Founder ☑ ───────────────────────────       │
│ │                         │ │   Founder           [2/5] ████░░░░░░                  │
│ │                         │ │   Energy Efficiency [4/5] ████████░░                  │
│ │                         │ │   Builder            [1/5] ██░░░░░░░░                  │
│ │                         │ │                                                      │
│ │                         │ │ ── Researcher ☑ ──────────────────────────────       │
│ │                         │ │   Research Review   [3/5] ████████░░                  │
│ │                         │ │   Research Methods  [2/5] ████░░░░░░                  │
│ │                         │ │   Research Focus    [5/5] ██████████                  │
│ │                         │ │   ...more skill groups...                             │
│ └─────────────────────────┘ │                                                      │
│                             │ [Import] [New] [Save] [Delete]                       │
└─────────────────────────────┴───────────────────────────────────────────────────────┘
```

Controls:
- `flpBase` (FlowLayoutPanel, left-to-right, Dock=Fill, WrapContents=false)
- Left: `flpSearchList` (427px wide):
  - `txtNameFilter` (ValidatedTextBox) — name filter
  - `cmbResource` (ComboBox, DropDownList) — filter by resource relevance
  - `lvwPlayerProfiles` (ListView, full-row select)
- Right: `flpPlayerData` (top-down, AutoScroll, WrapContents=false):
  - `flpPlayerDetails` (top-down, AutoScroll) — scrollable detail area:
    - `txtPlayerName`, `txtTotalCredits`, `txtFaction` + `cmbFaction`
    - Rank blocks (Public/Private/Military), each with rank name, current XP, next XP fields
    - `txtSkillPoints` — available skill points
    - Skill groups (ColonyDirector, ColonyFounder, ColonyOperations, Commander, Engineer, Entrepreneur, JobManagement, Researcher, Surveyor, Trader) — each is a FlowLayoutPanel containing:
      - Group header with `CheckBox` (profession unlocked) and `Label`
      - `PlayerSkillBlock` controls (custom UserControl with skill name, level, progress bar)
  - `flpCommands`: `cmdImport`, `cmdNew`, `cmdSave`, `cmdDelete`

Satisfies: REQ-PLR-010 (player profile management), REQ-PLR-020 (skill tracking)
