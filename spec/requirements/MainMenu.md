# Main Menu Requirements

## Overview

The MainWindow menu provides File lifecycle operations (New/Open/Save/Save As/Exit), a Manage menu for feature forms, and Help with About dialog.

## File Menu

**REQ-MM-001** File menu items SHALL appear in order: New, Open, Save, Save As, Preferences..., separator, Exit.  
**REQ-MM-002** File > New SHALL close all MDI children, clear all PlayerContext data, reset current player, clear Last_Opened_Path, and update the title bar.  
**REQ-MM-003** File > Open SHALL display an OpenFileDialog for JSON files. On selection, close all MDI children, load the file into PlayerContext, store the path as Last_Opened_Path, and update the title bar.  
**REQ-MM-004** File > Save SHALL write to Last_Opened_Path if set, otherwise behave as Save As.  
**REQ-MM-005** File > Save As SHALL display a SaveFileDialog for JSON files. On confirmation, write data, update Last_Opened_Path, and update the title bar.  
**REQ-MM-006** File > Exit SHALL prompt "Are you sure you want to exit?" with Yes/No. Yes closes the application; No cancels.  
**REQ-MM-007** File > Preferences... SHALL open the Preferences form as a modal dialog.

## Auto-Open on Launch

**REQ-MM-010** On startup, if Last_Opened_Path is stored and the file exists, the application SHALL load it automatically and update the title bar.  
**REQ-MM-011** If the stored path does not exist or cannot be parsed, the application SHALL clear Last_Opened_Path, log a warning, and start with empty data.  
**REQ-MM-012** If no Last_Opened_Path is stored, the application SHALL load from the default PlayerData.json path.

## Manage Menu

**REQ-MM-020** The menu currently labeled "Edit" SHALL be renamed to "Manage".  
**REQ-MM-021** Items SHALL be labeled "Manage Colonies", "Manage Blueprints", "Manage Surveys", etc.  
**REQ-MM-022** Items SHALL be sorted alphabetically.

## Help Menu

**REQ-MM-030** Help > About SHALL display a modal dialog with application name, version, and copyright from assembly metadata.  
**REQ-MM-031** Help > Contents (Ctrl+F1) SHALL open the help system. F1 SHALL open context-sensitive help for the active form.
