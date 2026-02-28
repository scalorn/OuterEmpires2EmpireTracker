using System;
using System.Diagnostics;
using System.Windows.Forms;

public class DataEntryGridView : System.Windows.Forms.DataGridView
{
    public Control previousControl { get; set; }
    private bool changingSelection = false;
    public DataEntryGridView()
    {
    }

    protected override bool ProcessDialogKey(Keys keyData)
    {
        Debug.Print("ProcessDialogKey Key = " + keyData);
        Keys key = (keyData & (Keys.KeyCode | Keys.Shift));

        if (key == Keys.Tab)
        {
            bool handled = handleForward(this.Focused);
            if (handled)
            {
                return true;
            }
        }
        if (key == (Keys.Tab | Keys.Shift))
        {
            bool handled = handleBackwards(this.Focused);
            if (handled)
            {
                return true;
            }
        }

        return base.ProcessDialogKey(keyData);
    }
    protected override bool ProcessDataGridViewKey(KeyEventArgs e)
    {
        Debug.Print("ProcessDataGridViewKey Key = " + e);
        if (e.KeyData == Keys.Tab)
        {
            bool handled = handleForward(this.Focused);
            if (handled)
            {
                return true;
            }
        }
        if (e.KeyData == (Keys.Tab | Keys.Shift))
        {
            bool handled = handleBackwards(this.Focused);
            if (handled)
            {
                return true;
            }
        }
        return base.ProcessDataGridViewKey(e);
    }
    protected override void OnSelectionChanged(EventArgs e)
    {
        Debug.Print("OnSelectionChanged Event Args " + e + " " + e.ToString());

        if (!changingSelection && this.CurrentCell != null)
        {
            int col = this.CurrentCell.ColumnIndex;
            // We are on a read only cell. move forward.
            if (col >=0 && col < this.Columns.Count && this.Columns[col].ReadOnly)
            {
                bool handled = handleForward(this.Focused);
            }
        }
    }

    private bool handleBackwards(bool enableEdit)
    {
        int col = this.CurrentCell.ColumnIndex - 1;
        col = findPreviousCell(col);
        if (col >= 0)
        {
            handleEditCell(this.CurrentCell.RowIndex, col, enableEdit);
            return true;
        }
        else
        {
            if (this.CurrentCell.RowIndex != 0)
            {
                col = findPreviousCell(this.Columns.Count - 1);
                Debug.Print("Backwards col = " + col);
                if (col >= 0)
                {
                    handleEditCell(this.CurrentCell.RowIndex - 1, col, enableEdit);
                    return true;
                }
            }
            else
            {
                Debug.Print("Need to reverse jump control! " + previousControl);
                if (previousControl != null)
                {
                    this.previousControl.Focus();
                    // This does not work.
                    //this.SelectNextControl(this, false, true, true, true);
                    return true;
                }
            }
        }
        return false;
    }

    private bool handleForward(bool enableEdit)
    {
        int col = this.CurrentCell.ColumnIndex + 1;
        col = findNextCell(col);
        if (col < this.Columns.Count)
        {
            handleEditCell(this.CurrentCell.RowIndex, col, enableEdit);
            return true;
        }
        else
        {
            if (this.CurrentCell.RowIndex != this.Rows.Count - 1)
            {
                col = findNextCell(0);
                if (col <= this.CurrentCell.ColumnIndex)
                {
                    handleEditCell(this.CurrentCell.RowIndex + 1, col, enableEdit);
                    return true;
                }
            } else
            {
                // This doesn't work.
                //this.SelectNextControl(this, true, true, true, true);
                //return true;
            }
        }
        return false;
    }

    private int findPreviousCell(int col)
    {
        for (; col >= 0;
        col--)
        {
            if (!this.Columns[col].ReadOnly)
            {
                break;
            }
        }
        return col;
    }

    private int findNextCell(int col)
    {
        for (; col < this.Columns.Count;
        col++)
        {
            if (!this.Columns[col].ReadOnly)
            {
                break;
            }
        }
        return col;
    }

    private void handleEditCell(int rowIndex, int col, bool enableEdit)
    {
        changingSelection = true;
        this.CurrentCell =
        this.Rows[rowIndex].Cells[col];
        if ( enableEdit)
        {
            this.BeginEdit(true);
        }
        changingSelection = false;
    }
}
