# Requirements Document

## Introduction

Overhaul the MainWindow menu system in OE2EmpireTracker. The File menu gains standard document-lifecycle operations (New, Open, Save, Save As, Exit) with dirty-state tracking and last-opened-file memory. The Edit menu is renamed to Manage with updated item labels sorted alphabetically. A Help → About dialog is added.

## Glossary

- **Application**: The OE2EmpireTracker WinForms desktop application
- **MainWindow**: The main MDI parent form (`MainWindow.cs`) hosting the menu strip
- **PlayerContext**: The singleton (`PlayerContext.getInstance()`) that holds all in-memory player data and persists to JSON
- **Player_File**: A JSON file on disk containing serialized player data (the format written by `PlayerContext.writeContext()`)
- **File_Menu**: The top-level "File" menu in the MainWindow menu strip
- **Manage_Menu**: The top-level menu (currently named "Edit") that provides access to feature management forms
- **Open_File_Dialog**: A standard `OpenFileDialog` for selecting a Player_File to load
- **Save_File_Dialog**: A standard `SaveFileDialog` for choosing a destination path and filename
- **Confirmation_Dialog**: A standard `MessageBox` prompting the user with Yes/No options
- **About_Dialog**: A modal dialog displaying application name, version, and copyright
- **Last_Opened_Path**: The file path of the most recently opened or saved Player_File, persisted in user settings between sessions

## Requirements

### Requirement 1: File → New

**User Story:** As a player, I want to start a fresh player file from the File menu, so that I can begin tracking a new empire without leftover data.

#### Acceptance Criteria

1. WHEN the user selects File → New, THE Application SHALL clear all in-memory player data in PlayerContext (profiles, colonies, blueprints, surveys, delivery routes, delivery plans) and reset the current player selection.
2. WHEN the user selects File → New, THE Application SHALL close all open MDI child forms before clearing data.
3. WHEN File → New completes, THE Application SHALL set the Last_Opened_Path to empty so that subsequent Save operations behave as Save As.
4. WHEN File → New completes, THE Application SHALL update the MainWindow title to indicate no file is loaded.

### Requirement 2: File → Open

**User Story:** As a player, I want to open an existing player file from disk, so that I can load previously saved empire data.

#### Acceptance Criteria

1. WHEN the user selects File → Open, THE Application SHALL display an Open_File_Dialog filtered to JSON files (`*.json`).
2. WHEN the user selects a valid file in the Open_File_Dialog, THE Application SHALL close all open MDI child forms, load the selected Player_File into PlayerContext, and update the UI.
3. WHEN the user selects a valid file in the Open_File_Dialog, THE Application SHALL store the selected path as the Last_Opened_Path.
4. WHEN the user cancels the Open_File_Dialog, THE Application SHALL take no action and return to the current state.
5. IF the selected file cannot be read or parsed, THEN THE Application SHALL display an error message and retain the current data.
6. WHEN a file is successfully opened, THE Application SHALL update the MainWindow title bar to include the loaded file name.

### Requirement 3: File → Save

**User Story:** As a player, I want to save my current data back to the file I opened, so that I can persist changes without choosing a location each time.

#### Acceptance Criteria

1. WHEN the user selects File → Save and a Last_Opened_Path exists, THE Application SHALL write the current PlayerContext data to that path using the existing persistence mechanism.
2. WHEN the user selects File → Save and no Last_Opened_Path exists (e.g. after File → New), THE Application SHALL behave identically to File → Save As.
3. IF a write error occurs during Save, THEN THE Application SHALL display an error message to the user.

### Requirement 4: File → Save As

**User Story:** As a player, I want to choose where to save my player data, so that I can create backups or manage multiple save files.

#### Acceptance Criteria

1. WHEN the user selects File → Save As, THE Application SHALL display a Save_File_Dialog filtered to JSON files (`*.json`) with a default extension of `.json`.
2. WHEN the user confirms a path in the Save_File_Dialog, THE Application SHALL write the current PlayerContext data to the chosen path.
3. WHEN the user confirms a path in the Save_File_Dialog, THE Application SHALL update the Last_Opened_Path to the chosen path.
4. WHEN the user confirms a path in the Save_File_Dialog, THE Application SHALL update the MainWindow title bar to include the new file name.
5. WHEN the user cancels the Save_File_Dialog, THE Application SHALL take no action.
6. IF a write error occurs during Save As, THEN THE Application SHALL display an error message to the user.

### Requirement 5: File → Exit

**User Story:** As a player, I want to be prompted before exiting, so that I do not accidentally lose unsaved work.

#### Acceptance Criteria

1. WHEN the user selects File → Exit, THE Application SHALL display a Confirmation_Dialog asking "Are you sure you want to exit?".
2. WHEN the user confirms the Confirmation_Dialog, THE Application SHALL close the MainWindow and terminate the application.
3. WHEN the user cancels the Confirmation_Dialog, THE Application SHALL remain open with no changes.

### Requirement 6: Auto-Open Last File on Launch

**User Story:** As a player, I want the application to automatically load my last-used player file on startup, so that I can resume where I left off.

#### Acceptance Criteria

1. WHEN the Application starts and a Last_Opened_Path is stored in user settings, THE Application SHALL attempt to load the Player_File at that path.
2. WHEN the stored Last_Opened_Path points to a valid file, THE Application SHALL load it into PlayerContext and update the MainWindow title bar.
3. IF the stored Last_Opened_Path points to a file that does not exist or cannot be parsed, THEN THE Application SHALL clear the Last_Opened_Path, log a warning, and start with empty data.
4. WHEN no Last_Opened_Path is stored, THE Application SHALL start with the default behavior (loading from the default `PlayerData.json` path).

### Requirement 7: Rename Edit Menu to Manage

**User Story:** As a player, I want the Edit menu renamed to Manage with updated item labels, so that the menu accurately describes its purpose.

#### Acceptance Criteria

1. THE Application SHALL display the top-level menu item currently labeled "Edit" as "Manage".
2. THE Application SHALL display the menu item currently labeled "Add Survey" as "Manage Surveys".
3. THE Application SHALL display the menu item currently labeled "Add Colony" as "Manage Colonies".
4. THE Application SHALL display the menu item currently labeled "Add Blueprint" as "Manage Blueprints".
5. THE Application SHALL sort all items in the Manage_Menu alphabetically by their displayed label.

### Requirement 8: Help → About Dialog

**User Story:** As a player, I want to see application information from the Help menu, so that I can identify the version I am running.

#### Acceptance Criteria

1. WHEN the user selects Help → About, THE Application SHALL display a modal About_Dialog.
2. THE About_Dialog SHALL display the application name ("OE2 Empire Tracker").
3. THE About_Dialog SHALL display the application version read from the assembly metadata.
4. THE About_Dialog SHALL display the copyright notice read from the assembly metadata.
5. WHEN the user closes the About_Dialog, THE Application SHALL return focus to the MainWindow.

### Requirement 9: File Menu Item Ordering

**User Story:** As a player, I want the File menu items in a standard order, so that the menu follows familiar desktop application conventions.

#### Acceptance Criteria

1. THE File_Menu SHALL display items in the following order: New, Open, Save, Save As, a separator, Exit.
