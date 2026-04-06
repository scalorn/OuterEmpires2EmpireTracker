# Implementation Plan: Main Menu Overhaul

## Overview

Overhaul the MainWindow menu system: add File lifecycle operations (New, Open, Save, Save As, Exit), auto-open last file on launch, rename Edit → Manage with alphabetical ordering, and add Help → About dialog. All changes use existing PlayerContext/EmpireContext APIs. Property tests use manual randomization with System.Random and fixed seed (no FsCheck).

## Tasks

- [x] 1. Add LastOpenedPath user setting
  - [x] 1.1 Add LastOpenedPath setting to Settings.settings and Settings.Designer.cs
    - Add a new user-scoped string setting `LastOpenedPath` with empty default value to `Properties/Settings.settings`
    - Add the corresponding property to `Properties/Settings.Designer.cs` with `UserScopedSettingAttribute` and `DefaultSettingValueAttribute("")`
    - _Requirements: 6.1, 6.2_

- [x] 2. Create FormAbout dialog
  - [x] 2.1 Create FormAbout.cs and FormAbout.Designer.cs
    - Create `Forms/FormAbout.cs` — modal form with labels for app name, version, copyright, and an OK button
    - Create `Forms/FormAbout.Designer.cs` — Designer code with Label controls and Button
    - On load, read `Assembly.GetExecutingAssembly()` attributes for `AssemblyTitle`, `AssemblyVersion`, `AssemblyCopyright`
    - OK button closes the dialog
    - Add `Compile Include` entries to `OE2EmpireTracker.csproj` for both files
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_

- [x] 3. Checkpoint
  - Ensure the project builds successfully with the new setting and FormAbout. Ask the user if questions arise.

- [x] 4. Restructure MainWindow menus in Designer
  - [x] 4.1 Add new File menu items and restructure in MainWindow.Designer.cs
    - Add `newToolStripMenuItem` (File → New) and `saveAsToolStripMenuItem` (File → Save As) field declarations and initialization
    - Add `toolStripSeparatorFileExit` separator before Exit
    - Reorder File menu DropDownItems: New, Open, Save, Save As, separator, Exit
    - Wire Click events to named handler methods: `newToolStripMenuItem_Click`, `openToolStripMenuItem_Click`, `saveToolStripMenuItem_Click`, `saveAsToolStripMenuItem_Click`, `exitToolStripMenuItem_Click`, `aboutToolStripMenuItem_Click`
    - _Requirements: 9.1_

  - [x] 4.2 Rename Edit → Manage and reorder items in MainWindow.Designer.cs
    - Change `editToolStripMenuItem.Text` to `"Manage"`
    - Change `addSurveyToolStripMenuItem.Text` to `"Manage Surveys"`
    - Change `addBlueprintToolStripMenuItem.Text` to `"Manage Blueprints"`
    - Change `addColonyToolStripMenuItem.Text` to `"Manage Colonies"`
    - Reorder Manage menu DropDownItems alphabetically: Colony Activity, Colony Daily Build, Delivery Execution, Delivery Routes, Manage Blueprints, Manage Colonies, Manage Player Profiles, Manage Surveys
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 5. Implement MainWindow file lifecycle logic
  - [x] 5.1 Add helper methods to MainWindow.cs
    - Add `_lastOpenedPath` field
    - Implement `CloseAllMdiChildren()` — iterates `MdiChildren` and calls `Close()` on each
    - Implement `UpdateTitleBar()` — sets `this.Text` based on `_lastOpenedPath` (format: `"OE2 Empire Tracker - {filename}"` or `"OE2 Empire Tracker"`)
    - Implement `SetLastOpenedPath(string path)` — updates `_lastOpenedPath`, persists to `Properties.Settings.Default.LastOpenedPath`, calls `Settings.Default.Save()`, calls `UpdateTitleBar()`
    - Implement `ReloadContextFromFile(string filePath)` — stops BackgroundProcessor, closes MDI children, calls `EmpireContext.Reset()`, sets `PlayerContext.FilePath`, calls `EmpireContext.getInstance()`, re-assigns local `context`/`playerContext` refs, creates and starts new BackgroundProcessor, repopulates player dropdown
    - Implement `PerformSaveAs()` — shows SaveFileDialog, writes context, updates last-opened path and title; returns bool
    - Implement `TryAutoOpenLastFile()` — reads `Settings.Default.LastOpenedPath`, if non-empty and file exists calls `ReloadContextFromFile`, otherwise clears setting
    - _Requirements: 1.2, 1.4, 2.2, 2.6, 4.4_

  - [x] 5.2 Implement File → New handler
    - Implement `newToolStripMenuItem_Click`: close MDI children, stop/dispose BackgroundProcessor, call `EmpireContext.Reset()`, reset `PlayerContext.FilePath` to default, call `EmpireContext.getInstance()`, re-assign refs, create/start new BackgroundProcessor, repopulate dropdown, set last-opened path to empty
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

  - [x] 5.3 Implement File → Open handler
    - Implement `openToolStripMenuItem_Click`: show OpenFileDialog filtered to `*.json`, on cancel return, try `ReloadContextFromFile`, on success call `SetLastOpenedPath`, on failure show MessageBox error and retain current data
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [x] 5.4 Implement File → Save and Save As handlers
    - Implement `saveToolStripMenuItem_Click`: if `_lastOpenedPath` non-empty, set `PlayerContext.FilePath` and call `writeContext()` in try/catch; if empty, call `PerformSaveAs()`
    - Implement `saveAsToolStripMenuItem_Click`: call `PerformSaveAs()`
    - _Requirements: 3.1, 3.2, 3.3, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_

  - [x] 5.5 Implement File → Exit and Help → About handlers
    - Implement `exitToolStripMenuItem_Click`: show confirmation MessageBox (YesNo), if Yes call `this.Close()`
    - Implement `aboutToolStripMenuItem_Click`: show `new FormAbout().ShowDialog(this)`
    - _Requirements: 5.1, 5.2, 5.3, 8.1, 8.5_

  - [x] 5.6 Wire auto-open into MainWindow constructor
    - Call `TryAutoOpenLastFile()` after `InitializeComponent()` and initial setup
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

- [x] 6. Update .csproj with new file entries
  - [x] 6.1 Add Compile Include entries to OE2EmpireTracker.csproj
    - Add `Compile Include` for `Forms\FormAbout.cs` (SubType Form) and `Forms\FormAbout.Designer.cs` (DependentUpon FormAbout.cs)
    - Verify all new files are included in the project
    - _Requirements: 8.1_

- [x] 7. Checkpoint
  - Ensure the project builds successfully with all menu changes. Ask the user if questions arise.

- [x] 8. Write tests
  - [x] 8.1 Write property test: New resets all state
    - **Property 1: New resets all state**
    - **Validates: Requirements 1.1, 1.3**
    - Create `OE2EmpireTracker.Tests/Forms/MainMenuOverhaulTests.cs`
    - Add `Compile Include` entry to `OE2EmpireTracker.Tests/OE2EmpireTracker.Tests.csproj`
    - Use manual randomization with `System.Random` and fixed seed, 100 iterations
    - Generate arbitrary PlayerRoot data, load into PlayerContext, execute New logic (EmpireContext.Reset + re-init), assert all lists empty and CurrentPlayerUUID empty
    - `// Feature: main-menu-overhaul, Property 1: New resets all state`

  - [x] 8.2 Write property test: Save/load round-trip
    - **Property 2: Save/load round-trip**
    - **Validates: Requirements 2.2, 3.1, 4.2**
    - Generate arbitrary PlayerRoot data, save via `writeContext()` to temp file, reload via `PlayerContext.Reset()` + `getInstance()`, assert data equivalence (same counts, same UUIDs)
    - `// Feature: main-menu-overhaul, Property 2: Save/load round-trip`

  - [x] 8.3 Write property test: Invalid file preserves state
    - **Property 3: Invalid file preserves state**
    - **Validates: Requirements 2.5**
    - Generate arbitrary current state and arbitrary non-JSON strings, attempt load, assert state unchanged
    - `// Feature: main-menu-overhaul, Property 3: Invalid file preserves state`

  - [x] 8.4 Write property test: Auto-open round-trip
    - **Property 4: Auto-open round-trip**
    - **Validates: Requirements 6.1, 6.2**
    - Generate arbitrary PlayerRoot, save to temp file, simulate auto-open logic, assert data equivalence
    - `// Feature: main-menu-overhaul, Property 4: Auto-open round-trip`

  - [x] 8.5 Write property test: Manage menu alphabetical ordering
    - **Property 5: Manage menu alphabetical ordering**
    - **Validates: Requirements 7.5**
    - Generate random permutations of menu item labels, apply sorting logic, assert alphabetical order
    - `// Feature: main-menu-overhaul, Property 5: Manage menu alphabetical ordering`

  - [x] 8.6 Write unit tests for menu structure and UI behavior
    - Test File menu item order: New, Open, Save, Save As, separator, Exit (Req 9.1)
    - Test menu label verification: "Manage", "Manage Surveys", "Manage Colonies", "Manage Blueprints" (Req 7.1–7.4)
    - Test Save with no last-opened path delegates to Save As behavior (Req 3.2)
    - Test About dialog displays correct app name, version, copyright (Req 8.2–8.4)
    - Test title bar shows filename after Open, clears after New (Req 1.4, 2.6, 4.4)
    - Test auto-open with no stored path uses default behavior (Req 6.4)
    - Test auto-open with missing file clears setting (Req 6.3)
    - _Requirements: 1.4, 2.6, 3.2, 4.4, 6.3, 6.4, 7.1–7.4, 8.2–8.4, 9.1_

- [x] 9. Final checkpoint
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Property tests use manual randomization with `System.Random` and fixed seed (100 iterations per test) — no FsCheck
- The .csproj uses old-style MSBuild with explicit `Compile Include` entries — new files must be added manually
- Do not use anonymous lambdas for event subscriptions — use named methods
- All forms must unsubscribe from PlayerContext events in OnFormClosed
- BackgroundProcessor needs Stop/Dispose/recreate on file operations (New, Open)
