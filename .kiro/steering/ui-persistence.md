# UI State Persistence Rules

All forms and controls must participate in the window state persistence system (UIPreferences.json in %LOCALAPPDATA%\OE2EmpireTracker\).

## New MDI Child Forms
- Must be assigned a per-form-type window number on open (via MainWindow)
- Title bar format: "#N - <Title>"
- Position and size must be saved on close and restored on open
- Bounds validation: not off-screen, 100px margin from right/bottom, min 320x200

## New DataGridViews
- Set AllowUserToOrderColumns = true
- Enable sorting on all columns
- Column widths, display order, and sort state must be persisted

## New Filter TextBoxes and ComboBoxes
- Current text/selection must be saved on form close and restored on form open

## Pattern
- Use the Preferences_Store API to save/restore state
- Key by form type name + window number + control name
