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

        public List<string> Items { get; set; } = new List<string>();
    }

    /// <summary>
    /// The cell type used by DataGridViewFilteredComboBoxColumn.
    /// Stores per-cell item list and launches the editing control.
    /// </summary>
    public class DataGridViewFilteredComboBoxCell : DataGridViewCell
    {
        public override Type ValueType
        {
            get => typeof(string);
            set { }
        }

        public List<string> Items { get; set; }

        public override Type EditType => typeof(DataGridViewFilteredComboBoxEditingControl);

        public override object DefaultNewRowValue => string.Empty;

        public override void InitializeEditingControl(
            int rowIndex,
            object initialFormattedValue,
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

        protected override void Paint(
            Graphics graphics,
            Rectangle clipBounds,
            Rectangle cellBounds,
            int rowIndex,
            DataGridViewElementStates cellState,
            object value,
            object formattedValue,
            string errorText,
            DataGridViewCellStyle cellStyle,
            DataGridViewAdvancedBorderStyle advancedBorderStyle,
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
                    TextRenderer.DrawText(
                        graphics,
                        text,
                        cellStyle.Font,
                        textBounds,
                        cellStyle.ForeColor,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
                }
            }
        }
    }

    /// <summary>
    /// Grid editing control — extends FilteredTextComboSet with IDataGridViewEditingControl.
    /// All filtering logic lives in the base class; this adds only the grid plumbing.
    /// </summary>
    public class DataGridViewFilteredComboBoxEditingControl : FilteredTextComboSet, IDataGridViewEditingControl
    {
        private DataGridView _dataGridView;

        private bool _valueChanged;

        private int _rowIndex;

        public DataGridViewFilteredComboBoxEditingControl()
        {
            // Strip borders for inline grid editing — the cell provides the border
            BorderStyle = BorderStyle.None;
            TxtFilter.BorderStyle = BorderStyle.None;
            // Always show filter in grid mode — the grid handles focus
            IsEditing = true;
            TxtFilter.Visible = true;
        }

        public DataGridView EditingControlDataGridView
        {
            get => _dataGridView;
            set => _dataGridView = value;
        }

        public object EditingControlFormattedValue
        {
            get => CmbItems.SelectedItem?.ToString() ?? string.Empty;
            set
            {
                string val = value?.ToString() ?? string.Empty;
                int idx = CmbItems.Items.IndexOf(val);
                if (idx >= 0) CmbItems.SelectedIndex = idx;
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
            ApplyStyle(dataGridViewCellStyle.Font, dataGridViewCellStyle.ForeColor);
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
            return CmbItems.SelectedItem?.ToString() ?? string.Empty;
        }

        public void PrepareEditingControlForEdit(bool selectAll)
        {
            ResetFilter();
            TxtFilter.Focus();
        }

        protected override void OnSelectedItemChanged()
        {
            base.OnSelectedItemChanged();
            _valueChanged = true;
            _dataGridView?.NotifyCurrentCellDirty(true);
        }
    }
}
