# Player Profile Mockups

### FormPlayerProfile

MDI child form. Left-list / right-detail with scrollable skill groups.

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│ Player Profiles                                                         [_][□][X]│
├─────────────────────────────┬───────────────────────────────────────────────────────┤
│ Name: [________________]    │ First Name: [James____] Last Name: [Kirk_______]      │
│ Resource: [All          ▼]  │ Total Credits: [1250000__________]                    │
│                             │ Faction: [____] [The Space Pirates         ▼]         │
│ ┌─────────────────────────┐ │                                                      │
│ │▸ James Kirk             │ │ Public Rank:  [Citizen_____] XP:[1200] Next:[2000]   │
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
│                             │ [Import] [Sync API] [New] [Save] [Delete]            │
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
    - `txtFirstName`, `txtLastName`, `txtTotalCredits`, `txtFaction` + `cmbFaction`
    - Public Rank: `txtPublicRank`, `txtPublicRankCurXP`, `txtPublicRankNextXP`
    - Private Rank: `txtPrivateRank`, `txtPrivateRankCurXP`, `txtPrivateRankNextXP`
    - Military Rank: `txtMilitaryRank`, `txtMilitaryRankCurXP`, `txtMilitaryRankNextXP`
    - `txtSkillPoints` — available skill points
    - Skill groups — each is a FlowLayoutPanel with a profession CheckBox and `PlayerSkillBlock` controls:
      - `chkColonyDirector`, `chkColonyFounder`, `chkColonyOperations`
      - `chkCommander`, `chkEngineer`, `chkEntrepeneur`
      - `chkJobManagement`, `chkResearcher`, `chkSurveyor`, `chkTrader`
  - `flpCommands`: `cmdImport`, `cmdSyncApi`, `cmdNew`, `cmdSave`, `cmdDelete`

Satisfies: REQ-PLR-010 (player profile management), REQ-PLR-020 (skill tracking)
