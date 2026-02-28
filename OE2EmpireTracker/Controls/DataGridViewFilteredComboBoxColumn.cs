using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OE2EmpireTracker.Controls
{
    public partial class DataGridViewFilteredComboBoxColumn : UserControl, IDataGridViewEditingControl
    {
        public DataGridViewFilteredComboBoxColumn()
        {
            InitializeComponent();
        }

        public DataGridView EditingControlDataGridView { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public object EditingControlFormattedValue { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int EditingControlRowIndex { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool EditingControlValueChanged { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Cursor EditingPanelCursor => throw new NotImplementedException();

        public bool RepositionEditingControlOnValueChange => throw new NotImplementedException();

        public void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle)
        {
            throw new NotImplementedException();
        }

        public bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey)
        {
            throw new NotImplementedException();
        }

        public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context)
        {
            throw new NotImplementedException();
        }

        public void PrepareEditingControlForEdit(bool selectAll)
        {
            throw new NotImplementedException();
        }
    }
}
