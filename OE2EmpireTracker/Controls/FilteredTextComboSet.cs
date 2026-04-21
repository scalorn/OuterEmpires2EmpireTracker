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
        protected TextBox txtFilter;
        protected ComboBox cmbItems;
        private List<string> _fullItems = new List<string>();
        private List<int> _filteredIndexMap = new List<int>();
        private bool _suppressFilterEvent;
        protected bool _suppressSelectionEvent;

        /// <summary>
        /// Fires when the user selects an item in the combo box.
        /// </summary>
        public event EventHandler SelectedItemChanged;

        /// <summary>
        /// Gets the currently selected item string, or null if nothing is selected.
        /// </summary>
        public string SelectedItem => cmbItems.SelectedItem?.ToString();

        /// <summary>
        /// Gets the index of the selected item in the full (unfiltered) item list,
        /// or -1 if nothing is selected.
        /// </summary>
        public int SelectedFullIndex
        {
            get
            {
                if (cmbItems.SelectedIndex < 0 || cmbItems.SelectedIndex >= _filteredIndexMap.Count)
                    return -1;
                return _filteredIndexMap[cmbItems.SelectedIndex];
            }
        }

        /// <summary>
        /// Gets the full (unfiltered) item list.
        /// </summary>
        public List<string> Items => _fullItems;

        public FilteredTextComboSet()
        {
            txtFilter = new TextBox { Dock = DockStyle.None, BorderStyle = BorderStyle.None };
            cmbItems = new ComboBox { Dock = DockStyle.None, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };

            Controls.Add(txtFilter);
            Controls.Add(cmbItems);

            txtFilter.TextChanged += TxtFilter_TextChanged;
            cmbItems.SelectedIndexChanged += CmbItems_SelectedIndexChanged;
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            int filterWidth = (int)(Width * 0.35);
            txtFilter.SetBounds(0, 0, filterWidth, Height);
            cmbItems.SetBounds(filterWidth, 0, Width - filterWidth, Height);
        }

        /// <summary>
        /// Sets the item list and optionally selects a current value.
        /// </summary>
        public void SetItems(List<string> items, string currentValue)
        {
            _fullItems = items ?? new List<string>();
            _suppressFilterEvent = true;
            _suppressSelectionEvent = true;
            txtFilter.Text = string.Empty;
            _suppressFilterEvent = false;
            RebuildFilteredList();
            if (!string.IsNullOrEmpty(currentValue))
            {
                int idx = cmbItems.Items.IndexOf(currentValue);
                if (idx >= 0) cmbItems.SelectedIndex = idx;
            }
            _suppressSelectionEvent = false;
        }

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

        protected void RebuildFilteredList()
        {
            var (filtered, indexMap) = ApplyFilter(_fullItems, txtFilter.Text);
            _filteredIndexMap = indexMap;
            cmbItems.Items.Clear();
            foreach (var item in filtered)
                cmbItems.Items.Add(item);
            if (filtered.Count > 0 && !string.IsNullOrEmpty(txtFilter.Text))
            {
                try { cmbItems.DroppedDown = true; } catch { }
            }
        }

        private void TxtFilter_TextChanged(object sender, EventArgs e)
        {
            if (_suppressFilterEvent) return;
            RebuildFilteredList();
        }

        private void CmbItems_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressSelectionEvent) return;
            OnSelectedItemChanged();
        }

        protected virtual void OnSelectedItemChanged()
        {
            SelectedItemChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Clears the filter text and shows the full item list, preserving the current selection.
        /// </summary>
        public void ResetFilter()
        {
            string currentValue = cmbItems.SelectedItem?.ToString();
            _suppressFilterEvent = true;
            _suppressSelectionEvent = true;
            txtFilter.Text = string.Empty;
            _suppressFilterEvent = false;
            RebuildFilteredList();
            if (!string.IsNullOrEmpty(currentValue))
            {
                int idx = cmbItems.Items.IndexOf(currentValue);
                if (idx >= 0) cmbItems.SelectedIndex = idx;
            }
            _suppressSelectionEvent = false;
        }

        /// <summary>
        /// Applies a font and forecolor to both child controls.
        /// </summary>
        public void ApplyStyle(Font font, Color foreColor)
        {
            txtFilter.Font = font;
            txtFilter.ForeColor = foreColor;
            cmbItems.Font = font;
            cmbItems.ForeColor = foreColor;
        }
    }
}
