using System;
using System.Windows.Forms;

public class DataEntryGridView : System.Windows.Forms.DataGridView
{
    public DataEntryGridView()
    {
    }

    protected override bool ProcessDialogKey(Keys keyData)
    {
        Keys key = (keyData & Keys.KeyCode);

        if (key == Keys.Tab)
        {
            int col = this.CurrentCell.ColumnIndex + 1;
            for (; col < this.Columns.Count; col++)
            {
                if (!this.Columns[col].ReadOnly)
                { break; }
            }
            if (col < this.Columns.Count)
            {
                this.CurrentCell =
                this.Rows[this.CurrentCell.RowIndex].Cells[col];
            }
            else
            {
                if (this.CurrentCell.RowIndex != this.Rows.Count - 1)
                {
                    for (col = 0; col <= this.CurrentCell.ColumnIndex;
                    col++)
                    {
                        if (!this.Columns[col].ReadOnly)
                        {
                            break;
                        }
                    }
                    if (col <= this.CurrentCell.ColumnIndex)
                    {
                        this.CurrentCell =
                        this.Rows[this.CurrentCell.RowIndex + 1].Cells[col];
                    }
                }
            }
            return true;

        }
        return base.ProcessDialogKey(keyData);
    }
    protected override bool ProcessDataGridViewKey(KeyEventArgs e)
    {
        if (e.KeyData == Keys.Tab)
        {
            int col = this.CurrentCell.ColumnIndex + 1;
            for (; col < this.Columns.Count; col++)
            {
                if (!this.Columns[col].ReadOnly)
                { break; }
            }
            if (col < this.Columns.Count)
            {
                this.CurrentCell =
                this.Rows[this.CurrentCell.RowIndex].Cells[col];
            }
            else
            {
                if (this.CurrentCell.RowIndex != this.Rows.Count - 1)
                {
                    for (col = 0; col <= this.CurrentCell.ColumnIndex;
                    col++)
                    {
                        if (!this.Columns[col].ReadOnly)
                        {
                            break;
                        }
                    }
                    if (col <= this.CurrentCell.ColumnIndex)
                    {
                        this.CurrentCell =
                        this.Rows[this.CurrentCell.RowIndex + 1].Cells[col];
                    }
                }
            }
            return true;
        }
        return base.ProcessDataGridViewKey(e);
    }
}
