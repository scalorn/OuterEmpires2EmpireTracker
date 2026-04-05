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

- Use `ProgrammaticUpdateGuard` (from `IProgrammaticUpdateSource`) to suppress event handlers during code-driven UI updates.
- Forms and controls that implement `IProgrammaticUpdateSource` expose `BeginProgrammaticUpdate()` / `EndProgrammaticUpdate()`.

## Blueprint Lists

- Always use `playerContext.GetAllBlueprints()` to get the combined player + global blueprint list. Never iterate `blueprintList` directly for read operations.
- Only save/delete mutations should access `blueprintList` or `globalBlueprintList` directly.
