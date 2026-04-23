using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace OE2EmpireTracker.Controls
{
    /// <summary>
    /// A standalone UserControl combining a filter TextBox and a ComboBox.
    /// Type in the TextBox to filter the ComboBox items using case-insensitive
    /// contains-match. Can be used directly on a form or as the base class for
    /// DataGridViewFilteredComboBoxEditingControl (grid inline editing).
    /// </summary>
    public class FilteredTextComboSet : UserControl
    {
        private List<string> _fullItems = new List<string>();

        private List<int> _filteredIndexMap = new List<int>();

        private bool _suppressFilterEvent;

        public FilteredTextComboSet()
        {
            TxtFilter = new TextBox { Dock = DockStyle.None, BorderStyle = BorderStyle.FixedSingle };
            CmbItems = new ComboBox { Dock = DockStyle.None, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            BorderStyle = BorderStyle.FixedSingle;

            Controls.Add(TxtFilter);
            Controls.Add(CmbItems);

            TxtFilter.TextChanged += TxtFilter_TextChanged;
            CmbItems.SelectedIndexChanged += CmbItems_SelectedIndexChanged;

            TxtFilter.Enter += (s, ev) => EnterEditMode();
            CmbItems.Enter += (s, ev) => EnterEditMode();
            TxtFilter.Leave += (s, ev) => BeginInvoke(new Action(CheckLeaveEditMode));
            CmbItems.Leave += (s, ev) => BeginInvoke(new Action(CheckLeaveEditMode));

            // Start in display mode — combo full-width, filter hidden
            TxtFilter.Visible = false;
        }

        /// <summary>
        /// Fires when the user selects an item in the combo box.
        /// </summary>
        public event EventHandler SelectedItemChanged;

        /// <summary>
        /// Gets the index of the selected item in the full (unfiltered) item list,
        /// or -1 if nothing is selected.
        /// </summary>
        public int SelectedFullIndex
        {
            get
            {
                if (CmbItems.SelectedIndex < 0 || CmbItems.SelectedIndex >= _filteredIndexMap.Count)
                    return -1;
                return _filteredIndexMap[CmbItems.SelectedIndex];
            }
        }

        /// <summary>
        /// Gets the currently selected item string, or null if nothing is selected.
        /// </summary>
        public string SelectedItem => CmbItems.SelectedItem?.ToString();

        /// <summary>
        /// Gets the full (unfiltered) item list.
        /// </summary>
        public List<string> Items => _fullItems;

        /// <summary>
        /// Filters the full item list using case-insensitive contains-match.
        /// Returns the filtered items and an index map back to the full list.
        /// </summary>
        public static (List<string> filtered, List<int> indexMap) ApplyFilter(List<string> fullItems, string filter)
        {
            var filtered = new List<string>();
            var indexMap = new List<int>();
            if (fullItems == null) return (filtered, indexMap);

            for (int i = 0; i < fullItems.Count; i++)
            {
                string item = fullItems[i] ?? string.Empty;
                if (string.IsNullOrEmpty(filter) || item.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    filtered.Add(fullItems[i]);
                    indexMap.Add(i);
                }
            }

            return (filtered, indexMap);
        }

        protected TextBox TxtFilter { get; set; }

        protected ComboBox CmbItems { get; set; }

        protected bool SuppressSelectionEvent { get; set; }

        protected bool IsEditing { get; set; }

        /// <summary>
        /// Sets the item list and optionally selects a current value.
        /// </summary>
        public void SetItems(List<string> items, string currentValue)
        {
            _fullItems = items ?? new List<string>();
            _suppressFilterEvent = true;
            SuppressSelectionEvent = true;
            TxtFilter.Text = string.Empty;
            _suppressFilterEvent = false;
            RebuildFilteredList();
            if (!string.IsNullOrEmpty(currentValue))
            {
                int idx = CmbItems.Items.IndexOf(currentValue);
                if (idx >= 0) CmbItems.SelectedIndex = idx;
            }

            SuppressSelectionEvent = false;
        }

        /// <summary>
        /// Clears the filter text and shows the full item list, preserving the current selection.
        /// </summary>
        public void ResetFilter()
        {
            string currentValue = CmbItems.SelectedItem?.ToString();
            _suppressFilterEvent = true;
            SuppressSelectionEvent = true;
            TxtFilter.Text = string.Empty;
            _suppressFilterEvent = false;
            RebuildFilteredList();
            if (!string.IsNullOrEmpty(currentValue))
            {
                int idx = CmbItems.Items.IndexOf(currentValue);
                if (idx >= 0) CmbItems.SelectedIndex = idx;
            }

            SuppressSelectionEvent = false;
        }

        /// <summary>
        /// Applies a font and forecolor to both child controls.
        /// </summary>
        public void ApplyStyle(Font font, Color foreColor)
        {
            TxtFilter.Font = font;
            TxtFilter.ForeColor = foreColor;
            CmbItems.Font = font;
            CmbItems.ForeColor = foreColor;
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (IsEditing)
            {
                int filterWidth = (int)(Width * 0.35);
                TxtFilter.SetBounds(0, 0, filterWidth, Height);
                CmbItems.SetBounds(filterWidth, 0, Width - filterWidth, Height);
            }
            else
            {
                CmbItems.SetBounds(0, 0, Width, Height);
            }
        }

        protected void RebuildFilteredList()
        {
            var (filtered, indexMap) = ApplyFilter(_fullItems, TxtFilter.Text);
            _filteredIndexMap = indexMap;
            SuppressSelectionEvent = true;
            CmbItems.Items.Clear();
            foreach (var item in filtered)
                CmbItems.Items.Add(item);
            SuppressSelectionEvent = false;
            if (filtered.Count > 0 && !string.IsNullOrEmpty(TxtFilter.Text))
            {
                try
                {
                    CmbItems.DroppedDown = true;
                }
                catch
                {
                }
            }
        }

        protected virtual void OnSelectedItemChanged()
        {
            SelectedItemChanged?.Invoke(this, EventArgs.Empty);
        }

        private void EnterEditMode()
        {
            if (IsEditing) return;
            IsEditing = true;
            TxtFilter.Visible = true;
            PerformLayout();
            ResetFilter();
            TxtFilter.Focus();
        }

        private void CheckLeaveEditMode()
        {
            if (IsDisposed) return;
            if (TxtFilter.Focused || CmbItems.Focused) return;
            IsEditing = false;
            TxtFilter.Visible = false;
            PerformLayout();
        }

        private void TxtFilter_TextChanged(object sender, EventArgs e)
        {
            if (_suppressFilterEvent) return;
            RebuildFilteredList();
        }

        private void CmbItems_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SuppressSelectionEvent) return;
            OnSelectedItemChanged();
        }
    }
}
