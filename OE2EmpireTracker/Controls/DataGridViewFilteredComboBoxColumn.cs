using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OE2EmpireTracker.Controls
{
    /// <summary>
    /// A DataGridView column that hosts a filtered combo box (TextBox + ComboBox) as its editing control.
    /// Provides inline contains-match filtering for item selection.
    /// </summary>
    public class DataGridViewFilteredComboBoxColumn : DataGridViewColumn
    {
        public List<string> Items { get; set; } = new List<string>();

        public DataGridViewFilteredComboBoxColumn() : base(new DataGridViewFilteredComboBoxCell())
        {
        }

        public override DataGridViewCell CellTemplate
        {
            get => base.CellTemplate;
            set
            {
                if (value != null && !(value is DataGridViewFilteredComboBoxCell))
                    throw new InvalidCastException("CellTemplate must be a DataGridViewFilteredComboBoxCell.");
                base.CellTemplate = value;
            }
        }
    }

    /// <summary>
    /// The cell type used by DataGridViewFilteredComboBoxColumn.
    /// Stores per-cell item list and launches the editing control.
    /// </summary>
    public class DataGridViewFilteredComboBoxCell : DataGridViewCell
    {
        public List<string> Items { get; set; }

        public override Type EditType => typeof(DataGridViewFilteredComboBoxEditingControl);

        public override Type ValueType
        {
            get => typeof(string);
            set { }
        }

        public override object DefaultNewRowValue => string.Empty;

        public override void InitializeEditingControl(int rowIndex, object initialFormattedValue,
            DataGridViewCellStyle dataGridViewCellStyle)
        {
            base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);

            if (DataGridView.EditingControl is DataGridViewFilteredComboBoxEditingControl editor)
            {
                var effectiveItems = Items
                    ?? (OwningColumn as DataGridViewFilteredComboBoxColumn)?.Items
                    ?? new List<string>();
                string currentValue = initialFormattedValue?.ToString() ?? string.Empty;
                editor.SetItems(effectiveItems, currentValue);
            }
        }

        public override object Clone()
        {
            var clone = (DataGridViewFilteredComboBoxCell)base.Clone();
            if (Items != null)
                clone.Items = new List<string>(Items);
            return clone;
        }

        protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds,
            int rowIndex, DataGridViewElementStates cellState, object value, object formattedValue,
            string errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            if ((paintParts & DataGridViewPaintParts.Background) != 0)
            {
                using (var brush = new SolidBrush(cellStyle.BackColor))
                    graphics.FillRectangle(brush, cellBounds);
            }
            if ((paintParts & DataGridViewPaintParts.Border) != 0)
                PaintBorder(graphics, clipBounds, cellBounds, cellStyle, advancedBorderStyle);

            if ((paintParts & DataGridViewPaintParts.ContentForeground) != 0)
            {
                string text = formattedValue?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(text))
                {
                    var textBounds = new Rectangle(
                        cellBounds.X + cellStyle.Padding.Left + 2,
                        cellBounds.Y + cellStyle.Padding.Top,
                        cellBounds.Width - cellStyle.Padding.Horizontal - 4,
                        cellBounds.Height - cellStyle.Padding.Vertical);
                    TextRenderer.DrawText(graphics, text, cellStyle.Font, textBounds,
                        cellStyle.ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
                }
            }
        }
    }

    /// <summary>
    /// The editing control — a UserControl with TextBox + ComboBox that implements IDataGridViewEditingControl.
    /// Provides contains-match filtering of the item list.
    /// </summary>
    public class DataGridViewFilteredComboBoxEditingControl : UserControl, IDataGridViewEditingControl
    {
        private TextBox txtFilter;
        private ComboBox cmbItems;
        private List<string> _fullItems = new List<string>();
        private List<int> _filteredIndexMap = new List<int>();
        private DataGridView _dataGridView;
        private bool _valueChanged;
        private int _rowIndex;
        private bool _suppressFilterEvent;
        private bool _suppressSelectionEvent;

        public DataGridViewFilteredComboBoxEditingControl()
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

        private void RebuildFilteredList()
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
            _valueChanged = true;
            _dataGridView?.NotifyCurrentCellDirty(true);
        }

        #region IDataGridViewEditingControl

        public DataGridView EditingControlDataGridView
        {
            get => _dataGridView;
            set => _dataGridView = value;
        }

        public object EditingControlFormattedValue
        {
            get => cmbItems.SelectedItem?.ToString() ?? string.Empty;
            set
            {
                string val = value?.ToString() ?? string.Empty;
                int idx = cmbItems.Items.IndexOf(val);
                if (idx >= 0) cmbItems.SelectedIndex = idx;
            }
        }

        public int EditingControlRowIndex
        {
            get => _rowIndex;
            set => _rowIndex = value;
        }

        public bool EditingControlValueChanged
        {
            get => _valueChanged;
            set => _valueChanged = value;
        }

        public Cursor EditingPanelCursor => Cursors.IBeam;

        public bool RepositionEditingControlOnValueChange => false;

        public void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle)
        {
            txtFilter.Font = dataGridViewCellStyle.Font;
            txtFilter.ForeColor = dataGridViewCellStyle.ForeColor;
            cmbItems.Font = dataGridViewCellStyle.Font;
            cmbItems.ForeColor = dataGridViewCellStyle.ForeColor;
        }

        public bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.Enter:
                case Keys.Escape:
                case Keys.Tab:
                case Keys.Delete:
                case Keys.Back:
                    return true;
                default:
                    Keys key = keyData & Keys.KeyCode;
                    if ((key >= Keys.A && key <= Keys.Z) || (key >= Keys.D0 && key <= Keys.D9)
                        || (key >= Keys.NumPad0 && key <= Keys.NumPad9))
                        return true;
                    return !dataGridViewWantsInputKey;
            }
        }

        public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context)
        {
            return cmbItems.SelectedItem?.ToString() ?? string.Empty;
        }

        public void PrepareEditingControlForEdit(bool selectAll)
        {
            // Preserve the current selection across the filter reset
            string currentValue = cmbItems.SelectedItem?.ToString();
            _suppressFilterEvent = true;
            txtFilter.Text = string.Empty;
            _suppressFilterEvent = false;
            _suppressSelectionEvent = true;
            RebuildFilteredList();
            if (!string.IsNullOrEmpty(currentValue))
            {
                int idx = cmbItems.Items.IndexOf(currentValue);
                if (idx >= 0) cmbItems.SelectedIndex = idx;
            }
            _suppressSelectionEvent = false;
            txtFilter.Focus();
        }

        #endregion
    }
}
