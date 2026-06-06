# Preferences Mockups

### FormPreferences

Modal dialog (FixedDialog, CenterParent). TabControl with Thresholds and Server tabs.

```
┌──────────────────────────────────────────────────────────────┐
│ Preferences                                              [X] │
├──────────────────────────────────────────────────────────────┤
│ [Thresholds] [Server]                                        │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│ ┌─ Structure Count ────────────────────────────────────────┐ │
│ │ Yellow Threshold: [60_________]  Red: [66_____________]  │ │
│ └──────────────────────────────────────────────────────────┘ │
│                                                              │
│ ┌─ Worker Request Due Window ──────────────────────────────┐ │
│ │ Yellow Threshold: [2d 0h 0m 0s]  Red: [1d 0h 0m 0s___]  │ │
│ └──────────────────────────────────────────────────────────┘ │
│                                                              │
│ ┌─ Colony Import Staleness ────────────────────────────────┐ │
│ │ Yellow Threshold: [5d 0h 0m 0s]  Red: [6d 0h 0m 0s___]  │ │
│ └──────────────────────────────────────────────────────────┘ │
│                                                              │
│ ┌─ Background Processing ──────────────────────────────────┐ │
│ │ Interval: [1m 0s__________]                              │ │
│ └──────────────────────────────────────────────────────────┘ │
│                                                              │
│ ┌─ Administration Report ──────────────────────────────────┐ │
│ │ Refresh Interval: [1m 0s______]                          │ │
│ └──────────────────────────────────────────────────────────┘ │
│                                                              │
│ ┌─ Countdown Display ──────────────────────────────────────┐ │
│ │ Refresh Rate: [1s____________]                           │ │
│ └──────────────────────────────────────────────────────────┘ │
│                                                              │
│                          [OK] [Cancel] [Reset]               │
└──────────────────────────────────────────────────────────────┘
```

Server tab:

```
┌──────────────────────────────────────────────────────────────┐
│ Preferences                                              [X] │
├──────────────────────────────────────────────────────────────┤
│ [Thresholds] [Server]                                        │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│ ┌─ Server Connection ──────────────────────────────────────┐ │
│ │ Server URL:            [https://host:5443] [Test Conn.]  │ │
│ │ Certificate Thumbprint:[A1B2C3D4...___________________]  │ │
│ │ Bearer Token:          [●●●●●●●●●●●●●●●●●●●●●●●●●●●●]  │ │
│ │ Operating Mode:        [Local Only________▼]             │ │
│ │                                                          │ │
│ │ Connection successful.                                   │ │
│ │ [Push Local Data to Server]                              │ │
│ └──────────────────────────────────────────────────────────┘ │
│                                                              │
│                          [OK] [Cancel] [Reset]               │
└──────────────────────────────────────────────────────────────┘
```

Controls:
- Form: `FormBorderStyle=FixedDialog`, `MaximizeBox=false`, `MinimizeBox=false`, `StartPosition=CenterParent`
- `AcceptButton=btnOK`, `CancelButton=btnCancel`
- `tabControl` (TabControl) — three tabs: Thresholds, Server, Game API
- `tabThresholds` (TabPage) — threshold settings
- `tabServer` (TabPage) — server connection settings
- `tabGameApi` (TabPage) — game API OAuth2 connection settings
- `grpStructureCount` (GroupBox) — structure warning thresholds:
  - `txtStructureYellow` (TextBox) — integer count
  - `txtStructureRed` (TextBox) — integer count
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
- `grpServerConnection` (GroupBox) — server connection:
  - `txtServerUrl` (ValidatedTextBox) — server URL (e.g. https://host:5443)
  - `btnTestConnection` (Button) — tests connection to server /health endpoint
  - `txtThumbprint` (ValidatedTextBox) — certificate thumbprint for pinning
  - `txtBearerToken` (ValidatedTextBox) — bearer token (masked with UseSystemPasswordChar)
  - `cmbOperatingMode` (ComboBox, DropDownList) — Local Only / Server Only / Server + Local
  - `lblConnectionStatus` (Label) — shows test connection result
  - `btnPushLocalToServer` (Button) — reads local PlayerData.json and BaselineData.json from disk and uploads to server (bootstrapping)
- `grpGameApi` (GroupBox) — Game API OAuth2 connection:
  - `txtGameApiUrl` (ValidatedTextBox) — game API server URL
  - `txtGameApiAppId` (ValidatedTextBox) — registered application GUID
  - `txtGameApiClientId` (ValidatedTextBox) — player account identifier
  - `cmbGameApiCharacter` (ComboBox, DropDownList) — selects which character to enter/view the secret for
  - `txtGameApiSecret` (ValidatedTextBox, PasswordChar='●') — per-character secret
  - `nudPollingInterval` (NumericUpDown) — polling interval in minutes (1-60)
  - `chkGameApiEnabled` (CheckBox) — enable/disable game API integration
  - `lblTpsLimit` (Label) — "TPS Limit:" label
  - `txtTpsLimit` (ValidatedTextBox) — TPS rate limit value (0.1-100.0)
  - `btnTestGameApiConnection` (Button) — tests connection via token exchange
  - `lblTestResult` (Label) — shows test connection result
- `btnOK` (Button), `btnCancel` (Button, DialogResult=Cancel), `btnResetDefaults` (Button)
- All time values use countdown format: `Xd Xh Xm Xs`

Satisfies: REQ-PRF-010 (user preferences), REQ-PRF-020 (threshold configuration), Req 11 (server URL/thumbprint config), Req 16 (operating mode)
