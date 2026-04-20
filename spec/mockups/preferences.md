# Preferences Mockups

### FormPreferences

Modal dialog (FixedDialog, CenterParent). Grouped threshold and interval settings.

```
┌──────────────────────────────────────────────────────────────┐
│ Preferences                                              [X]│
├──────────────────────────────────────────────────────────────┤
│                                                              │
│ ┌─ Structure Count ────────────────────────────────────────┐ │
│ │ Yellow Threshold: [0d 2h 0m 0s__]  Red: [0d 0h 30m 0s_] │ │
│ └──────────────────────────────────────────────────────────┘ │
│                                                              │
│ ┌─ Worker Request Due Window ──────────────────────────────┐ │
│ │ Yellow Threshold: [1d 0h 0m 0s__]  Red: [0d 4h 0m 0s__] │ │
│ └──────────────────────────────────────────────────────────┘ │
│                                                              │
│ ┌─ Colony Import Staleness ────────────────────────────────┐ │
│ │ Yellow Threshold: [1d 0h 0m 0s__]  Red: [3d 0h 0m 0s__] │ │
│ └──────────────────────────────────────────────────────────┘ │
│                                                              │
│ ┌─ Background Processing ──────────────────────────────────┐ │
│ │ Interval: [0d 0h 5m 0s____]                              │ │
│ └──────────────────────────────────────────────────────────┘ │
│                                                              │
│ ┌─ Administration Report ──────────────────────────────────┐ │
│ │ Refresh Interval: [0d 0h 1m 0s]                          │ │
│ └──────────────────────────────────────────────────────────┘ │
│                                                              │
│ ┌─ Countdown Display ──────────────────────────────────────┐ │
│ │ Refresh Rate: [0d 0h 0m 1s____]                          │ │
│ └──────────────────────────────────────────────────────────┘ │
│                                                              │
│                          [OK] [Cancel] [Reset]               │
└──────────────────────────────────────────────────────────────┘
```

Controls:
- Form: `FormBorderStyle=FixedDialog`, `MaximizeBox=false`, `MinimizeBox=false`, `StartPosition=CenterParent`
- `AcceptButton=btnOK`, `CancelButton=btnCancel`
- `grpStructureCount` (GroupBox) — structure warning thresholds:
  - `txtStructureYellow` (TextBox) — countdown format (e.g. "0d 2h 0m 0s")
  - `txtStructureRed` (TextBox) — countdown format
- `grpWorkerRequest` (GroupBox) — worker request due window:
  - `txtWorkerYellow` (TextBox) — countdown format
  - `txtWorkerRed` (TextBox) — countdown format
- `grpColonyImport` (GroupBox) — colony import staleness:
  - `txtColonyImportYellow` (TextBox) — countdown format
  - `txtColonyImportRed` (TextBox) — countdown format
- `grpBackgroundProcessing` (GroupBox):
  - `txtBackgroundInterval` (TextBox) — countdown format
- `grpAdminReport` (GroupBox):
  - `txtAdminRefresh` (TextBox) — countdown format
- `grpCountdownDisplay` (GroupBox):
  - `txtCountdownRefresh` (TextBox) — countdown format
- `btnOK` (Button), `btnCancel` (Button, DialogResult=Cancel), `btnResetDefaults` (Button)
- All time values use countdown format: `Xd Xh Xm Xs`

Satisfies: REQ-PRF-010 (user preferences), REQ-PRF-020 (threshold configuration)
