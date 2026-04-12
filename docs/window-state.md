# Window State

OE2 Empire Tracker remembers the position, size, and control state of every window you open. This means your workspace layout is preserved between sessions — forms reopen exactly where you left them.

## What Is Saved

### Main Window

The main window saves and restores:

- **Position** — Left and top coordinates on screen.
- **Size** — Width and height.

### MDI Child Forms

Each child form (Colonies, Blueprints, Surveys, etc.) saves and restores:

- **Position and size** — Where the form is located within the MDI container and how large it is.
- **Grid column widths** — DataGridView column sizes are preserved.
- **Grid column order** — If you reorder columns by dragging, the order is saved.
- **Grid sort state** — The sorted column and direction are restored.
- **Text filter values** — Search/filter text boxes retain their content.
- **Checkbox states** — Filter checkboxes remember their checked/unchecked state.
- **Combo box selections** — Dropdown selections are preserved where possible.

### Multiple Instances

You can open multiple instances of the same form type. Each instance gets a unique window number (shown as `#1`, `#2`, etc. in the title bar). Window state is saved per instance number, so your first Colony form and second Colony form can have different positions and settings.

When you close a window and open a new one of the same type, the app reuses the lowest available number. For example, if you have Colony `#1`, `#2`, and `#3` open and close `#2`, the next Colony window you open will be `#2` again — and it will restore the saved state (position, size, filters) from the previous `#2`.

## How It Works

Window state is stored in a preferences file managed by the `PreferencesStore` service. The `WindowStateHelper` class handles saving and restoring:

- **On form close** — The form's state is captured and written to the preferences store.
- **On form open** — If saved state exists for that form type and window number, it is restored.

### Bounds Validation

When restoring a window position, the application validates that the saved bounds are still visible on the current screen configuration. This handles cases where:

- A monitor has been disconnected since the last session.
- Screen resolution has changed.
- The saved position would place the window off-screen.

If the saved position is invalid, the form opens at a default position within the visible area.

## MDI Layout Options

The main window provides standard MDI layout commands under the **Window** menu:

| Menu Item | Description |
|-----------|-------------|
| Cascade | Arranges all child windows in a cascading pattern |
| Tile Horizontal | Tiles child windows horizontally (stacked) |
| Tile Vertical | Tiles child windows side by side |

These are useful when you have many forms open and want to quickly reorganize your workspace.

## Session Restore

When you close the application, it remembers which forms were open and their window numbers. The next time you launch, those forms are automatically reopened with their saved positions, sizes, and control states. For example, if you had Colony `#1` and Colony `#3` open, both will reappear exactly as you left them.

This works whether you exit via File → Exit or by closing the main window directly.

## File Management

The main window also remembers the last opened file path. When you launch the application, it automatically tries to reopen the last file you were working with. If the file no longer exists, the setting is cleared and you start with the default data.

Use the **File** menu to manage your data files:

| Menu Item | Description |
|-----------|-------------|
| New | Create a fresh empty data set |
| Open | Load data from a JSON file |
| Save | Save to the current file (or Save As if no file is set) |
| Save As | Save to a new file location |
| Exit | Close the application (with confirmation) |

## Related Topics

- [Getting Started](getting-started.md) — Overview of the application workspace
- [Background Processing](background-processing.md) — The processor lifecycle is tied to the main window
