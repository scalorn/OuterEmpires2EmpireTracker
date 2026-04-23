using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Persistence
{
    /// <summary>
    /// Static helper that saves and restores UI state for forms.
    /// Walks the control tree to find DataGridView, TextBox, and ComboBox controls automatically.
    /// </summary>
    public static class WindowStateHelper
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Save all state for an MDI child: position, size, grids, filters, combos.
        /// </summary>
        public static void SaveState(Form form, string formTypeKey, int windowNumber)
        {
            var store = PreferencesStore.GetInstance();
            var windowState = store.GetWindowState(formTypeKey, windowNumber);

            // Save position/size
            windowState.Position = new WindowPosition
            {
                Left = form.Left,
                Top = form.Top,
                Width = form.Width,
                Height = form.Height
            };

            // Ensure FormState exists
            if (windowState.FormState == null)
            {
                windowState.FormState = new FormControlState();
            }

            // Walk control tree and save all control states
            SaveControlStates(form, windowState.FormState);

            store.Save();
        }

        /// <summary>
        /// Restore all state for an MDI child: position, size, grids, filters, combos.
        /// </summary>
        public static void RestoreState(Form form, string formTypeKey, int windowNumber)
        {
            var store = PreferencesStore.GetInstance();
            var windowState = store.GetWindowState(formTypeKey, windowNumber);

            // Restore position/size if saved
            if (windowState.Position != null)
            {
                var savedBounds = new Rectangle(
                    windowState.Position.Left,
                    windowState.Position.Top,
                    windowState.Position.Width,
                    windowState.Position.Height);

                Rectangle parentArea = form.MdiParent != null
                    ? form.MdiParent.ClientRectangle
                    : Screen.PrimaryScreen.WorkingArea;

                var validated = BoundsValidator.ValidateMdiChildBounds(savedBounds, parentArea);
                form.StartPosition = FormStartPosition.Manual;
                form.Left = validated.X;
                form.Top = validated.Y;
                form.Width = validated.Width;
                form.Height = validated.Height;
            }

            // Restore control states if saved
            if (windowState.FormState != null)
            {
                RestoreControlStates(form, windowState.FormState);
            }
        }

        /// <summary>
        /// Save MainWindow position/size only.
        /// </summary>
        public static void SaveMainWindowState(Form mainWindow)
        {
            var store = PreferencesStore.GetInstance();
            store.Preferences.MainWindow = new WindowPosition
            {
                Left = mainWindow.Left,
                Top = mainWindow.Top,
                Width = mainWindow.Width,
                Height = mainWindow.Height
            };

            store.Save();
        }

        /// <summary>
        /// Restore MainWindow position/size only.
        /// </summary>
        public static void RestoreMainWindowState(Form mainWindow)
        {
            var store = PreferencesStore.GetInstance();
            var saved = store.Preferences.MainWindow;
            if (saved == null) return;

            var savedBounds = new Rectangle(saved.Left, saved.Top, saved.Width, saved.Height);
            var validated = BoundsValidator.ValidateMainWindowBounds(savedBounds);
            mainWindow.StartPosition = FormStartPosition.Manual;
            mainWindow.Left = validated.X;
            mainWindow.Top = validated.Y;
            mainWindow.Width = validated.Width;
            mainWindow.Height = validated.Height;
        }

        internal static void SaveControlStates(Control parent, FormControlState formState)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is DataGridView grid && !string.IsNullOrEmpty(grid.Name))
                {
                    SaveGridState(grid, formState);
                }
                else if (control is ListView listView && !string.IsNullOrEmpty(listView.Name))
                {
                    SaveListViewState(listView, formState);
                }
                else if (control is TextBox textBox && !string.IsNullOrEmpty(textBox.Name))
                {
                    formState.FilterTexts[textBox.Name] = textBox.Text;
                }
                else if (control is CheckBox checkBox && !string.IsNullOrEmpty(checkBox.Name))
                {
                    formState.CheckStates[checkBox.Name] = checkBox.Checked;
                }
                else if (control is ComboBox combo && !string.IsNullOrEmpty(combo.Name))
                {
                    formState.ComboSelections[combo.Name] = new ComboState
                    {
                        SelectedValue = combo.SelectedItem?.ToString(),
                        SelectedIndex = combo.SelectedIndex
                    };
                }
                else if (control is SplitContainer splitter && !string.IsNullOrEmpty(splitter.Name))
                {
                    formState.SplitterDistances[splitter.Name] = splitter.SplitterDistance;
                }
                else if (control is TabControl tab && !string.IsNullOrEmpty(tab.Name))
                {
                    formState.TabSelectedIndices[tab.Name] = tab.SelectedIndex;
                }

                // Recurse into child controls
                if (control.HasChildren)
                {
                    SaveControlStates(control, formState);
                }
            }
        }

        private static void SaveGridState(DataGridView grid, FormControlState formState)
        {
            var gridState = new GridState();

            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (string.IsNullOrEmpty(column.Name)) continue;

                gridState.Columns[column.Name] = new GridColumnState
                {
                    Width = column.Width,
                    DisplayIndex = column.DisplayIndex
                };
            }

            if (grid.SortedColumn != null)
            {
                gridState.SortColumnName = grid.SortedColumn.Name;
                gridState.SortDirection = grid.SortOrder == SortOrder.Descending
                    ? "Descending"
                    : "Ascending";
            }

            formState.Grids[grid.Name] = gridState;
        }

        private static void SaveListViewState(ListView listView, FormControlState formState)
        {
            var state = new ListViewState();
            for (int i = 0; i < listView.Columns.Count; i++)
            {
                state.ColumnWidths[i] = listView.Columns[i].Width;
            }

            // Save sort state if using ListViewItemComparer
            if (listView.ListViewItemSorter is OE2EmpireTracker.Controls.ListViewItemComparer comparer)
            {
                state.SortColumn = comparer.Column;
                state.SortDirection = comparer.Order == SortOrder.Descending ? "Descending" : "Ascending";
            }

            // Save unchecked items for CheckBoxes ListViews
            if (listView.CheckBoxes)
            {
                foreach (ListViewItem item in listView.Items)
                {
                    if (!item.Checked && item.Tag is string tag)
                        state.UncheckedItems.Add(tag);
                }
            }

            formState.ListViews[listView.Name] = state;
        }

        internal static void RestoreControlStates(Control parent, FormControlState formState)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is DataGridView grid && !string.IsNullOrEmpty(grid.Name))
                {
                    RestoreGridState(grid, formState);
                }
                else if (control is ListView listView && !string.IsNullOrEmpty(listView.Name))
                {
                    RestoreListViewState(listView, formState);
                }
                else if (control is TextBox textBox && !string.IsNullOrEmpty(textBox.Name))
                {
                    if (formState.FilterTexts.ContainsKey(textBox.Name))
                    {
                        textBox.Text = formState.FilterTexts[textBox.Name];
                    }
                }
                else if (control is CheckBox checkBox && !string.IsNullOrEmpty(checkBox.Name))
                {
                    if (formState.CheckStates.ContainsKey(checkBox.Name))
                    {
                        checkBox.Checked = formState.CheckStates[checkBox.Name];
                    }
                }
                else if (control is ComboBox combo && !string.IsNullOrEmpty(combo.Name))
                {
                    RestoreComboState(combo, formState);
                }
                else if (control is SplitContainer splitter && !string.IsNullOrEmpty(splitter.Name))
                {
                    if (formState.SplitterDistances != null &&
                        formState.SplitterDistances.ContainsKey(splitter.Name))
                    {
                        int saved = formState.SplitterDistances[splitter.Name];
                        if (saved >= splitter.Panel1MinSize &&
                            saved <= splitter.Width - splitter.Panel2MinSize)
                        {
                            splitter.SplitterDistance = saved;
                        }
                    }
                }
                else if (control is TabControl tab && !string.IsNullOrEmpty(tab.Name))
                {
                    if (formState.TabSelectedIndices != null &&
                        formState.TabSelectedIndices.ContainsKey(tab.Name))
                    {
                        int saved = formState.TabSelectedIndices[tab.Name];
                        if (saved >= 0 && saved < tab.TabCount)
                        {
                            tab.SelectedIndex = saved;
                        }
                    }
                }

                // Recurse into child controls
                if (control.HasChildren)
                {
                    RestoreControlStates(control, formState);
                }
            }
        }

        private static void RestoreGridState(DataGridView grid, FormControlState formState)
        {
            if (!formState.Grids.ContainsKey(grid.Name)) return;

            var gridState = formState.Grids[grid.Name];

            // Restore column widths and display indices
            foreach (var entry in gridState.Columns)
            {
                string columnName = entry.Key;
                var columnState = entry.Value;

                if (grid.Columns.Contains(columnName))
                {
                    grid.Columns[columnName].Width = columnState.Width;
                    grid.Columns[columnName].DisplayIndex = columnState.DisplayIndex;
                }
                else
                {
                    Log.Debug("Skipping saved column '{0}' on grid '{1}' -- column no longer exists", columnName, grid.Name);
                }
            }

            // Restore sort state
            if (!string.IsNullOrEmpty(gridState.SortColumnName))
            {
                if (grid.Columns.Contains(gridState.SortColumnName))
                {
                    var direction = gridState.SortDirection == "Descending"
                        ? ListSortDirection.Descending
                        : ListSortDirection.Ascending;
                    grid.Sort(grid.Columns[gridState.SortColumnName], direction);
                }
                else
                {
                    Log.Debug("Skipping saved sort column '{0}' on grid '{1}' -- column no longer exists", gridState.SortColumnName, grid.Name);
                }
            }
        }

        private static void RestoreListViewState(ListView listView, FormControlState formState)
        {
            if (!formState.ListViews.ContainsKey(listView.Name)) return;

            var state = formState.ListViews[listView.Name];
            foreach (var entry in state.ColumnWidths)
            {
                if (entry.Key >= 0 && entry.Key < listView.Columns.Count)
                {
                    listView.Columns[entry.Key].Width = entry.Value;
                }
            }

            // Restore sort state
            if (state.SortColumn >= 0 && state.SortColumn < listView.Columns.Count)
            {
                var order = state.SortDirection == "Descending" ? SortOrder.Descending : SortOrder.Ascending;
                listView.ListViewItemSorter = new OE2EmpireTracker.Controls.ListViewItemComparer(state.SortColumn, order);
                listView.Sort();
            }

            // Restore unchecked items for CheckBoxes ListViews
            if (listView.CheckBoxes && state.UncheckedItems != null && state.UncheckedItems.Count > 0)
            {
                var uncheckedSet = new HashSet<string>(state.UncheckedItems, StringComparer.Ordinal);
                foreach (ListViewItem item in listView.Items)
                {
                    if (item.Tag is string tag && uncheckedSet.Contains(tag))
                        item.Checked = false;
                }
            }
        }

        private static void RestoreComboState(ComboBox combo, FormControlState formState)
        {
            if (!formState.ComboSelections.ContainsKey(combo.Name)) return;

            var comboState = formState.ComboSelections[combo.Name];

            // Try to find saved value in items first
            if (comboState.SelectedValue != null)
            {
                for (int i = 0; i < combo.Items.Count; i++)
                {
                    if (combo.Items[i].ToString() == comboState.SelectedValue)
                    {
                        combo.SelectedIndex = i;
                        return;
                    }
                }
            }

            // Fall back to saved index
            if (comboState.SelectedIndex >= 0 && comboState.SelectedIndex < combo.Items.Count)
            {
                combo.SelectedIndex = comboState.SelectedIndex;
                return;
            }

            // Neither valid -- skip, leave at default
        }
    }
}
