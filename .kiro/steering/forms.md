---
inclusion: auto
---

# WinForms UI Patterns

Rules and patterns learned from building forms in this project. Follow these when creating or modifying any WinForms form.

## Layout & Resize

- Each form and its supporting components (Designer.cs, .resx) MUST be in its own directory under `Forms/`. Do NOT group multiple forms in a shared directory. Example: `Forms/DeliveryRoute/`, `Forms/DeliveryExecution/`, not `Forms/Delivery/`.
- All forms with a left list / right detail pattern MUST have layout event handlers (`flpBase.Layout`, `flpSearchList.Layout`, etc.) that resize panels and grids when the form resizes.
- DataGridView controls inside FlowLayoutPanels do NOT auto-resize. They need explicit sizing in layout handlers.
- FlowLayoutPanels with `AutoSize = true` will override explicit sizing from layout handlers. Set `AutoSize = false` on panels that need programmatic sizing.
- Tab pages need their own layout handlers — the parent TabControl resizing does not cascade to child content.
- Use `Dock = DockStyle.Bottom` for command/button rows at the bottom of tab pages, and `Dock = DockStyle.Fill` for the main content above them.

## DataGridView

- Always call `CancelEdit()` or detach `CellValidating` handlers before `Rows.Clear()`. If a cell is in edit mode with validation, `Rows.Clear()` throws `InvalidOperationException`.
- When a grid has `CellValidating` handlers that set `e.Cancel = true`, `CancelEdit()` alone is not enough. Detach the handler, call `EndEdit()`, then reattach.
- Apply this pattern in save handlers that iterate grid rows, and in any method that clears/repopulates a grid.
- Use `Rows.Add()` return value for the new row index. Do NOT use `RowCount - 2` unless `AllowUserToAddRows = true`.
- For grids with `DataGridViewCheckBoxColumn`: wire `CurrentCellDirtyStateChanged` to call `CommitEdit(DataGridViewDataErrorContexts.Commit)` so that `CellValueChanged` fires immediately on checkbox click instead of waiting for the user to leave the row.

## ComboBox / Dropdown Binding

- Always set `DataSource = null` before changing `DisplayMember` or `ValueMember` on a ComboBox. Changing members while a data source is bound to a different type throws `ArgumentException`.
- When repopulating a dropdown (filter change, checkbox toggle, save), preserve the current `SelectedValue`, rebuild the data source, then restore the selection if the item is still in the list.
- Filtered dropdowns should have a text filter box that repopulates the combo on `TextChanged`.

## ValidatedTextBox

- Use `ValidatedTextBox` instead of `TextBox` for all text inputs. It's a drop-in replacement with no behavioral change when no `ValidationPattern` is set.
- For external validation (e.g. duplicate name check), use `SetError(message)` / `ClearError()` in a `TextChanged` handler. These set `_hasExternalError` which prevents `ValidateInput()` from overriding the error state.
- `OnLostFocus` traps focus when `_hasExternalError` is true — the user cannot tab away from an invalid field.

## Selection Preservation

- When rebuilding a list (ListView, ComboBox, DataGridView) after save/delete/filter, always preserve and restore the current selection.
- Pattern: capture the selected UUID/value before rebuild, repopulate, then set `SelectedValue` back if the item still exists.

## Player Change Handling

- All forms that show player-specific data MUST subscribe to `playerContext.CurrentPlayerChanged` and refresh their data.
- On player change: clear selections, reset ViewModels, repopulate lists.

## Programmatic Update Guard

- ALL forms MUST implement `IProgrammaticUpdateSource` with `BeginProgrammaticUpdate()` / `EndProgrammaticUpdate()` and a `private int _isProgrammaticUpdate = 0;` field.
- ALWAYS use `using var guard = new ProgrammaticUpdateGuard(this);` (C# 8 using declaration) to suppress event handlers during code-driven UI updates. The guard auto-increments on creation and auto-decrements when disposed at scope exit.
- Do NOT create a guard without `using` — the guard relies on deterministic disposal (IDisposable), not the finalizer.
- Any method that programmatically modifies grid contents (Rows.Clear, Rows.Add, setting cell values, setting CurrentCell) MUST create a `using var guard = new ProgrammaticUpdateGuard(this);` at the top.
- All grid event handlers (CellValueChanged, SelectionChanged, CellValidating) MUST check `if (_isProgrammaticUpdate > 0) return;` as their first line.
- Do NOT use ad-hoc boolean flags for re-entrancy protection — always use the shared ProgrammaticUpdateGuard pattern.
- Setting `CurrentCell` triggers `SelectionChanged`. Setting cell values triggers `CellValueChanged`. `Rows.Clear()` triggers `SelectionChanged`. All of these cascade and cause StackOverflowException without the guard.

## Blueprint Lists

- Always use `playerContext.GetAllBlueprints()` to get the combined player + global blueprint list. Never iterate `blueprintList` directly for read operations.
- Only save/delete mutations should access `blueprintList` or `globalBlueprintList` directly.

## Scrollable Content Panels

- For forms with variable-height content (e.g. dynamically generated checkboxes, labels, buttons), use a FlowLayoutPanel with `AutoScroll = true` and `WrapContents = false`.
- The scrollable panel needs explicit sizing via a layout handler — `AutoScroll` only works when the panel has a fixed size and its children overflow it.
- Child controls inside the scrollable panel (grids, nested FlowLayoutPanels) need their widths set in the parent's layout handler since they won't auto-resize.

## Designer.cs Column Declarations

- When adding columns to a DataGridView in Designer.cs, ensure three things are present:
  1. The `new` instantiation in the control creation block at the top of `InitializeComponent()`
  2. The column configuration block (HeaderText, Name, Width, ReadOnly) — do NOT put a second `new` here
  3. The field declaration at the bottom of the class
- A duplicate `new` in the configuration block creates a second instance that overwrites the one already added to the grid's Columns collection, resulting in blank headers and default widths.

## Display Names & ExtendedName

- Data model classes that need a display name with context (e.g. purity for resources) should have a `[JsonIgnore] ExtendedName` property on the data class itself.
- Do NOT duplicate display name logic in form code — use the data model's property.
- Example: `DeliveryItem.ExtendedName` returns `"Name (Purity)"` for resources, plain `Name` otherwise.

## Dropdown Change Suppression

- When programmatically rebuilding a ComboBox data source, detach `SelectedIndexChanged` before the rebuild and reattach after to prevent cascading events.
- Use a `_lastSelectedUUID` field to detect actual selection changes vs. spurious events from data source rebinding.

## Data Model Write-Through

- ALL editable UI controls (TextBox, ValidatedTextBox, CheckBox, ComboBox) that map to a data model property MUST write back to the data model immediately on change (TextChanged, CheckedChanged, SelectedIndexChanged).
- Do NOT defer data model writes to a Save button — the in-memory data model must always reflect the current UI state. Background processing threads and cross-form code paths depend on the data model being current.
- The Save button's role is to call `writeContext()` to persist to disk, NOT to transfer UI values to the data model.
- When a control has a default display value (e.g. "1" for quantity), that default must also be written to the data model when the control becomes visible or the default is applied.
- Use the ProgrammaticUpdateGuard check (`if (_isProgrammaticUpdate > 0) return;`) in the handler to avoid writing back during programmatic population.
