# Implementation Plan: Window State Persistence

## Overview

Implement UI state persistence for OE2EmpireTracker so that window positions, sizes, grid layouts, filter text, and combo selections survive application restarts. The implementation follows a bottom-up approach: data models first, then the persistence store, then the bounds validator, then the helper class, then MainWindow integration, and finally MDI child form integration.

## Tasks

- [x] 1. Create data model classes
  - [x] 1.1 Create `OE2EmpireTracker/Baseline/UIPreferences.cs` with all data model classes: `UIPreferences`, `WindowPosition`, `WindowState`, `FormControlState`, `ComboState`, `GridState`, `GridColumnState`
    - All classes are plain POCOs with Newtonsoft.Json-compatible properties
    - `UIPreferences.Forms` is `Dictionary<string, Dictionary<string, WindowState>>`
    - Add `Compile Include` entry to `OE2EmpireTracker.csproj`
    - _Requirements: 11.3, 13.1_

  - [x] 1.2 Write unit tests for UIPreferences data model round-trip serialization
    - Create `OE2EmpireTracker.Tests/Baseline/UIPreferencesTests.cs`
    - Test JSON serialize → deserialize produces equivalent structure
    - Test that unknown keys in JSON are ignored on deserialization
    - Add `Compile Include` entry to `OE2EmpireTracker.Tests.csproj`
    - _Requirements: 13.1, 13.3_

- [x] 2. Implement BoundsValidator
  - [x] 2.1 Create `OE2EmpireTracker/Baseline/BoundsValidator.cs` with static methods `ValidateMainWindowBounds` and `ValidateMdiChildBounds`
    - Constants: `MinWidth = 320`, `MinHeight = 200`, `ScreenEdgeMargin = 100`
    - `ValidateMainWindowBounds(Rectangle savedBounds)` checks against `Screen.AllScreens` working areas, clamps size, shifts position if too close to edges, resets to primary monitor default if entirely off-screen
    - `ValidateMdiChildBounds(Rectangle savedBounds, Rectangle parentClientArea)` checks against parent client area, clamps size, shifts position if too close to edges, resets to default cascade position if entirely outside
    - Add `Compile Include` entry to `OE2EmpireTracker.csproj`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 5.1, 5.2, 5.3, 5.4_

  - [x] 2.2 Write unit tests for BoundsValidator
    - Create `OE2EmpireTracker.Tests/Baseline/BoundsValidatorTests.cs`
    - Test: window entirely off-screen resets to default
    - Test: right edge within margin shifts left
    - Test: bottom edge within margin shifts up
    - Test: width/height below minimum gets clamped to 320x200
    - Test: valid bounds pass through unchanged
    - Test MDI child variant against parent client area
    - Add `Compile Include` entry to `OE2EmpireTracker.Tests.csproj`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 5.1, 5.2, 5.3, 5.4_

- [x] 3. Checkpoint - Verify data models and validator compile
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Implement PreferencesStore
  - [x] 4.1 Create `OE2EmpireTracker/Baseline/PreferencesStore.cs` as a singleton
    - Follow existing singleton pattern (`getInstance()`, `Reset()`)
    - File path: `%LOCALAPPDATA%\OE2EmpireTracker\UIPreferences.json`
    - `Load()`: read and deserialize on construction; handle missing file (start empty), malformed JSON (log error, start empty), I/O errors (log, start empty)
    - `Save()`: serialize with `Formatting.Indented`, write via `SafeFileWriter.WriteAllText`, catch and log I/O errors without crashing
    - `GetWindowState(string formTypeKey, int windowNumber)`: return existing or create new `WindowState` entry
    - Create directory on first save if it doesn't exist
    - Add `Compile Include` entry to `OE2EmpireTracker.csproj`
    - _Requirements: 11.1, 11.2, 11.4, 11.5, 11.6, 12.3, 13.2_

  - [x] 4.2 Write unit tests for PreferencesStore
    - Create `OE2EmpireTracker.Tests/Baseline/PreferencesStoreTests.cs`
    - Test: `GetWindowState` creates new entry when none exists
    - Test: `GetWindowState` returns existing entry
    - Test: preserves other form entries when saving new ones
    - Test: handles malformed JSON gracefully (starts empty)
    - Test: handles missing file gracefully (starts empty)
    - Add `Compile Include` entry to `OE2EmpireTracker.Tests.csproj`
    - _Requirements: 11.5, 13.2, 13.3_

- [x] 5. Implement WindowStateHelper
  - [x] 5.1 Create `OE2EmpireTracker/Baseline/WindowStateHelper.cs` as a static class
    - `SaveState(Form form, string formTypeKey, int windowNumber)`: save position/size, walk control tree to find all `DataGridView` (save column widths, display indices, sort state), `TextBox` (save text), and `ComboBox` (save selected value/index). Call `PreferencesStore.getInstance().Save()`.
    - `RestoreState(Form form, string formTypeKey, int windowNumber)`: restore position/size via `BoundsValidator.ValidateMdiChildBounds`, restore grid column widths/display indices/sort, restore TextBox text, restore ComboBox selection (by value first, then by index, skip if neither valid)
    - `SaveMainWindowState(Form mainWindow)`: save position/size, call `PreferencesStore.getInstance().Save()`
    - `RestoreMainWindowState(Form mainWindow)`: restore position/size via `BoundsValidator.ValidateMainWindowBounds`
    - For grids: skip columns that no longer exist (log debug), skip sort column that no longer exists (log debug)
    - Add `Compile Include` entry to `OE2EmpireTracker.csproj`
    - _Requirements: 4.1, 4.2, 4.3, 6.1, 6.2, 6.3, 7.1, 7.2, 7.3, 8.1, 8.2, 8.3, 9.2, 9.3, 9.4, 10.2, 10.3, 10.4, 12.1, 14.1_

- [x] 6. Checkpoint - Verify all foundation classes compile
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Integrate MainWindow
  - [x] 7.1 Add window numbering and `OpenMdiChild<T>()` to `MainWindow.cs`
    - Add `private readonly Dictionary<string, int> _windowNumberCounters` field
    - Add `private T OpenMdiChild<T>() where T : Form, new()` method that: increments counter for `typeof(T).Name`, creates form, sets `MdiParent`, stores window number in `form.Tag`, prepends `"#N - "` to `form.Text`, calls `WindowStateHelper.RestoreState`, calls `form.Show()`
    - Replace all 8 menu click handler bodies (`addColonyToolStripMenuItem_Click`, `addBlueprintToolStripMenuItem_Click`, `addSurveyToolStripMenuItem_Click`, `managePlayerProfiles_Click`, `deliveryRoutesToolStripMenuItem_Click`, `deliveryExecutionToolStripMenuItem_Click`, `colonyDailyBuildToolStripMenuItem_Click`, `colonyActivityToolStripMenuItem_Click`) with `OpenMdiChild<FormXxx>()`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

  - [x] 7.2 Add MainWindow save/restore position
    - In `MainWindow` constructor (after `InitializeComponent`): call `WindowStateHelper.RestoreMainWindowState(this)`
    - Change `OnFormClosed` to `OnFormClosing` for the save call: call `WindowStateHelper.SaveMainWindowState(this)` before existing cleanup
    - _Requirements: 1.1, 1.2, 1.3, 2.1, 2.2, 2.3, 2.4, 12.2_

- [x] 8. Integrate MDI child forms
  - [x] 8.1 Add `WindowStateHelper.SaveState` call to `FormColony.OnFormClosed`
    - Insert `WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);` before existing cleanup in `OnFormClosed`
    - _Requirements: 4.1, 6.1, 7.1, 8.1, 9.2, 10.2, 12.1_

  - [x] 8.2 Add `WindowStateHelper.SaveState` call to `FormBlueprint.OnFormClosed`
    - Insert `WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);` before existing cleanup in `OnFormClosed`
    - _Requirements: 4.1, 6.1, 7.1, 8.1, 9.2, 10.2, 12.1_

  - [x] 8.3 Add `WindowStateHelper.SaveState` call to `FormSurvey.OnFormClosed`
    - Insert `WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);` before existing cleanup in `OnFormClosed`
    - _Requirements: 4.1, 6.1, 7.1, 8.1, 9.2, 10.2, 12.1_

  - [x] 8.4 Add `WindowStateHelper.SaveState` call to `FormPlayerProfile.OnFormClosed`
    - Insert `WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);` before existing cleanup in `OnFormClosed`
    - _Requirements: 4.1, 6.1, 7.1, 8.1, 9.2, 10.2, 12.1_

  - [x] 8.5 Add `WindowStateHelper.SaveState` call to `FormDeliveryRoute.OnFormClosed`
    - Insert `WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);` before existing cleanup in `OnFormClosed`
    - _Requirements: 4.1, 6.1, 7.1, 8.1, 9.2, 10.2, 12.1_

  - [x] 8.6 Add `WindowStateHelper.SaveState` call to `FormDeliveryExecution.OnFormClosed`
    - Insert `WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);` before existing cleanup in `OnFormClosed`
    - _Requirements: 4.1, 6.1, 7.1, 8.1, 9.2, 10.2, 12.1_

  - [x] 8.7 Add `WindowStateHelper.SaveState` call to `FormColonyDailyBuild.OnFormClosed`
    - Insert `WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);` before existing cleanup in `OnFormClosed`
    - _Requirements: 4.1, 6.1, 7.1, 8.1, 9.2, 10.2, 12.1_

  - [x] 8.8 Add `WindowStateHelper.SaveState` call to `FormColonyActivity.OnFormClosed`
    - Insert `WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);` before existing cleanup in `OnFormClosed`
    - _Requirements: 4.1, 6.1, 7.1, 8.1, 9.2, 10.2, 12.1_

- [x] 9. Enable AllowUserToOrderColumns and sorting on all DataGridViews
  - [x] 9.1 Set `AllowUserToOrderColumns = true` and enable sorting on all DataGridViews across all 8 MDI child forms
    - Walk each form's Designer.cs or constructor code to find all DataGridView controls
    - Set `AllowUserToOrderColumns = true` on each
    - Set `SortMode = DataGridViewColumnSortMode.Automatic` on all sortable columns
    - Forms: FormColony, FormBlueprint, FormSurvey, FormPlayerProfile, FormDeliveryRoute, FormDeliveryExecution, FormColonyDailyBuild, FormColonyActivity
    - _Requirements: 9.1, 10.1, 14.3_

- [x] 10. Final checkpoint - Ensure all code compiles and tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Old-style csproj requires explicit `Compile Include` entries for every new `.cs` file in both main and test projects
- Use NUnit 4.x constraint-based `Assert.That` syntax only, never classic `Assert.AreEqual`
- Do NOT use `dotnet test` — use `vstest.console` or `getDiagnostics` only
- The `WindowStateHelper.RestoreState` call for MDI children happens inside `MainWindow.OpenMdiChild<T>()`, so child forms do not need constructor changes
- Each form's `OnFormClosed` already exists with event unsubscription — the `SaveState` call is inserted as the first line before existing cleanup
