using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace OE2EmpireTracker.Controls
{
    /// <summary>
    /// A DataGridView column that hosts a ValidatedTextBox as its editing control.
    /// </summary>
    public class DataGridViewValidatedTextBoxColumn : DataGridViewColumn
    {
        [Category("Validation")]
        public string ValidationPattern { get; set; }

        [Category("Validation")]
        public bool AllowSpaces { get; set; }

        [Category("Colors")]
        public Color ValidColor { get; set; } = Color.White;

        [Category("Colors")]
        public Color InvalidColor { get; set; } = Color.LightCoral;

        public DataGridViewValidatedTextBoxColumn() : base(new DataGridViewValidatedTextBoxCell())
        {
        }

        public override DataGridViewCell CellTemplate
        {
            get => base.CellTemplate;
            set
            {
                if (value != null && !(value is DataGridViewValidatedTextBoxCell))
                    throw new InvalidCastException("CellTemplate must be a DataGridViewValidatedTextBoxCell.");
                base.CellTemplate = value;
            }
        }
    }

    /// <summary>
    /// The cell type used by DataGridViewValidatedTextBoxColumn.
    /// </summary>
    public class DataGridViewValidatedTextBoxCell : DataGridViewTextBoxCell
    {
        public override Type EditType => typeof(DataGridViewValidatedTextBoxEditingControl);

        public override Type ValueType => typeof(string);

        public override object DefaultNewRowValue => string.Empty;

        public override void InitializeEditingControl(int rowIndex, object initialFormattedValue,
            DataGridViewCellStyle dataGridViewCellStyle)
        {
            base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);

            if (DataGridView.EditingControl is DataGridViewValidatedTextBoxEditingControl editor)
            {
                editor.Text = initialFormattedValue?.ToString() ?? string.Empty;

                if (OwningColumn is DataGridViewValidatedTextBoxColumn col)
                {
                    editor.ValidationPattern = col.ValidationPattern;
                    editor.AllowSpaces = col.AllowSpaces;
                    editor.ValidColor = col.ValidColor;
                    editor.InvalidColor = col.InvalidColor;
                }
            }
        }
    }

    /// <summary>
    /// The editing control -- a ValidatedTextBox that implements IDataGridViewEditingControl.
    /// </summary>
    public class DataGridViewValidatedTextBoxEditingControl : ValidatedTextBox, IDataGridViewEditingControl
    {
        private DataGridView _dataGridView;
        private bool _valueChanged;
        private int _rowIndex;

        public DataGridViewValidatedTextBoxEditingControl()
        {
            BorderStyle = BorderStyle.None;
        }

        public DataGridView EditingControlDataGridView
        {
            get => _dataGridView;
            set => _dataGridView = value;
        }

        public object EditingControlFormattedValue
        {
            get => Text;
            set => Text = value?.ToString() ?? string.Empty;
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
            Font = dataGridViewCellStyle.Font;
            // BackColor is managed by ValidatedTextBox validation state
            ForeColor = dataGridViewCellStyle.ForeColor;
        }

        public bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Home:
                case Keys.End:
                case Keys.Delete:
                case Keys.Back:
                    return true;
                default:
                    return !dataGridViewWantsInputKey;
            }
        }

        public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context)
        {
            return Text;
        }

        public void PrepareEditingControlForEdit(bool selectAll)
        {
            if (selectAll)
                SelectAll();
            else
                SelectionStart = Text.Length;
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            _valueChanged = true;
            _dataGridView?.NotifyCurrentCellDirty(true);
        }
    }
}
