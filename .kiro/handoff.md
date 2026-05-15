# Session Handoff — 2026-05-15

## Next Task

Create a branch `net8maui` and start a spec for porting the WinForms client to .NET 8 + MAUI for cross-platform support (Windows, macOS, potentially iOS/Android).

### Steps to execute:
1. `git checkout -b net8maui` from mainline
2. Create `.kiro/specs/net8-maui-port/` with requirements.md, design.md, tasks.md
3. Start with requirements — the user wants to iterate on this spec

### Key context for the spec:
- The current client is .NET Framework 4.8.1 + WinForms (Windows-only)
- `OE2EmpireTracker.Common` is already .NET Standard 2.0 — shared between client and server, no porting needed
- `OE2EmpireTracker.Server` is already .NET 8 — no porting needed
- Only the `OE2EmpireTracker` (client/tool) project needs porting
- The user wants macOS support — that's the primary driver
- MAUI was chosen over Avalonia (user's decision)

### Architecture considerations:
- Common library (Models, Services, Constants) stays as-is — already cross-platform
- ViewModels may need adaptation (WinForms BindingSource → MAUI data binding)
- Forms (40+ WinForms forms with Designer.cs files) need complete UI rewrite in XAML
- Custom controls (FilteredTextComboSet, DataEntryGridView, ValidatedTextBox, DataGridViewFilteredComboBoxColumn) need MAUI equivalents
- Parsers (HTML scraping) stay as-is — no UI dependency
- Client/ (RemoteFactionClient, SyncManager, etc.) stays as-is — pure networking
- Persistence (SafeFileWriter, WindowStateHelper) needs platform adaptation
- NLog → Microsoft.Extensions.Logging (or keep NLog with MAUI target)
- packages.config → PackageReference (required for .NET 8)

### Risk areas to document:
- MAUI DataGrid maturity (WinForms DataGridView is heavily used)
- MDI (Multiple Document Interface) — MAUI doesn't have MDI; need tabbed or navigation pattern
- RichTextBox equivalent in MAUI
- System.Windows.Forms.Timer → MAUI dispatcher timer
- Clipboard access for HTML paste import
- Window state persistence across platforms

### What was completed this session:
- Ship template pricing (full spec + implementation)
- Remote faction service client-side (Phases 11-15: preferences, sync, WebSocket, operating modes)
- Ship enhanced stats (full spec + implementation)
- Expanded permissions spec (requirements + design, not implemented)
- JAS distance calibration (scaling factor 25/16)
- Blueprint duplicate UUID prevention (Add* guards + Init* dedup on load + importer fixes)
- Audit enforcement (hook changed from askAgent to runCommand)
- Common-mistakes steering file (6 entries)
- Server publish script (win-x64 + linux-x64)
- Dependabot vulnerability fixes (Npgsql, System.Text.Json, npm packages)
- FsCheck 2.x compatibility fixes (star system tests)
- Class diagrams added to all spec/design/ docs
- Multiple spec design iterations (faction-server-expanded-permissions)

### Current branch: mainline
### All work committed and backed up to all 3 remotes.
