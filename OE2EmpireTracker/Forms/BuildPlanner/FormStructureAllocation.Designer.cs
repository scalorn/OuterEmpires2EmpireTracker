namespace OE2EmpireTracker.Forms.BuildPlanner
{
    partial class FormStructureAllocation
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.flpMain = new System.Windows.Forms.FlowLayoutPanel();
            this.lblItemInfo = new System.Windows.Forms.Label();
            this.flpFilterRow = new System.Windows.Forms.FlowLayoutPanel();
            this.lblFilter = new System.Windows.Forms.Label();
            this.txtFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.chkIdleOnly = new System.Windows.Forms.CheckBox();
            this.dgvStructures = new System.Windows.Forms.DataGridView();
            this.colColony = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStructure = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblFutureNote = new System.Windows.Forms.Label();
            this.flpButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdAllocate = new System.Windows.Forms.Button();
            this.cmdCancel = new System.Windows.Forms.Button();

            this.flpMain.SuspendLayout();
            this.flpFilterRow.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStructures)).BeginInit();
            this.flpButtons.SuspendLayout();
            this.SuspendLayout();
            //
            // flpMain
            //
            this.flpMain.Controls.Add(this.lblItemInfo);
            this.flpMain.Controls.Add(this.flpFilterRow);
            this.flpMain.Controls.Add(this.dgvStructures);
            this.flpMain.Controls.Add(this.lblFutureNote);
            this.flpMain.Controls.Add(this.flpButtons);
            this.flpMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpMain.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpMain.Location = new System.Drawing.Point(0, 0);
            this.flpMain.Name = "flpMain";
            this.flpMain.Padding = new System.Windows.Forms.Padding(6);
            this.flpMain.Size = new System.Drawing.Size(520, 420);
            this.flpMain.WrapContents = false;
            //
            // lblItemInfo
            //
            this.lblItemInfo.AutoSize = true;
            this.lblItemInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblItemInfo.Location = new System.Drawing.Point(9, 6);
            this.lblItemInfo.Name = "lblItemInfo";
            this.lblItemInfo.Padding = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblItemInfo.Size = new System.Drawing.Size(100, 17);
            this.lblItemInfo.Text = "Item:";
            //
            // flpFilterRow
            //
            this.flpFilterRow.AutoSize = true;
            this.flpFilterRow.Controls.Add(this.lblFilter);
            this.flpFilterRow.Controls.Add(this.txtFilter);
            this.flpFilterRow.Controls.Add(this.chkIdleOnly);
            this.flpFilterRow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpFilterRow.Location = new System.Drawing.Point(9, 26);
            this.flpFilterRow.Name = "flpFilterRow";
            this.flpFilterRow.Size = new System.Drawing.Size(502, 26);
            //
            // lblFilter
            //
            this.lblFilter.AutoSize = true;
            this.lblFilter.Location = new System.Drawing.Point(3, 5);
            this.lblFilter.Name = "lblFilter";
            this.lblFilter.Size = new System.Drawing.Size(32, 13);
            this.lblFilter.Text = "Filter:";
            this.lblFilter.Anchor = System.Windows.Forms.AnchorStyles.Left;
            //
            // txtFilter
            //
            this.txtFilter.Location = new System.Drawing.Point(41, 3);
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.Size = new System.Drawing.Size(200, 20);
            //
            // chkIdleOnly
            //
            this.chkIdleOnly.AutoSize = true;
            this.chkIdleOnly.Checked = true;
            this.chkIdleOnly.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkIdleOnly.Location = new System.Drawing.Point(247, 3);
            this.chkIdleOnly.Name = "chkIdleOnly";
            this.chkIdleOnly.Size = new System.Drawing.Size(115, 17);
            this.chkIdleOnly.Text = "Show idle only";
            this.chkIdleOnly.UseVisualStyleBackColor = true;
            //
            // dgvStructures
            //
            this.dgvStructures.AllowUserToAddRows = false;
            this.dgvStructures.AllowUserToDeleteRows = false;
            this.dgvStructures.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvStructures.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colColony,
            this.colStructure,
            this.colType,
            this.colStatus});
            this.dgvStructures.Location = new System.Drawing.Point(9, 58);
            this.dgvStructures.MultiSelect = false;
            this.dgvStructures.Name = "dgvStructures";
            this.dgvStructures.ReadOnly = true;
            this.dgvStructures.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvStructures.Size = new System.Drawing.Size(502, 280);
            //
            // colColony
            //
            this.colColony.HeaderText = "Colony";
            this.colColony.Name = "colColony";
            this.colColony.ReadOnly = true;
            this.colColony.Width = 140;
            //
            // colStructure
            //
            this.colStructure.HeaderText = "Structure";
            this.colStructure.Name = "colStructure";
            this.colStructure.ReadOnly = true;
            this.colStructure.Width = 140;
            //
            // colType
            //
            this.colType.HeaderText = "Type";
            this.colType.Name = "colType";
            this.colType.ReadOnly = true;
            this.colType.Width = 80;
            //
            // colStatus
            //
            this.colStatus.HeaderText = "Status";
            this.colStatus.Name = "colStatus";
            this.colStatus.ReadOnly = true;
            this.colStatus.Width = 80;
            //
            // lblFutureNote
            //
            this.lblFutureNote.AutoSize = true;
            this.lblFutureNote.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblFutureNote.Location = new System.Drawing.Point(9, 344);
            this.lblFutureNote.Name = "lblFutureNote";
            this.lblFutureNote.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.lblFutureNote.Size = new System.Drawing.Size(350, 21);
            this.lblFutureNote.Text = "Ship and station build locations will be supported in a future update.";
            //
            // flpButtons
            //
            this.flpButtons.AutoSize = true;
            this.flpButtons.Controls.Add(this.cmdAllocate);
            this.flpButtons.Controls.Add(this.cmdCancel);
            this.flpButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flpButtons.Location = new System.Drawing.Point(9, 371);
            this.flpButtons.Name = "flpButtons";
            this.flpButtons.Size = new System.Drawing.Size(502, 29);
            //
            // cmdAllocate
            //
            this.cmdAllocate.Location = new System.Drawing.Point(424, 3);
            this.cmdAllocate.Name = "cmdAllocate";
            this.cmdAllocate.Size = new System.Drawing.Size(75, 23);
            this.cmdAllocate.Text = "Allocate";
            this.cmdAllocate.UseVisualStyleBackColor = true;
            //
            // cmdCancel
            //
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cmdCancel.Location = new System.Drawing.Point(343, 3);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(75, 23);
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            //
            // FormStructureAllocation
            //
            this.AcceptButton = this.cmdAllocate;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cmdCancel;
            this.ClientSize = new System.Drawing.Size(520, 420);
            this.Controls.Add(this.flpMain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormStructureAllocation";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Allocate Structure";
            this.flpMain.ResumeLayout(false);
            this.flpMain.PerformLayout();
            this.flpFilterRow.ResumeLayout(false);
            this.flpFilterRow.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStructures)).EndInit();
            this.flpButtons.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flpMain;
        private System.Windows.Forms.Label lblItemInfo;
        private System.Windows.Forms.FlowLayoutPanel flpFilterRow;
        private System.Windows.Forms.Label lblFilter;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtFilter;
        private System.Windows.Forms.CheckBox chkIdleOnly;
        private System.Windows.Forms.DataGridView dgvStructures;
        private System.Windows.Forms.DataGridViewTextBoxColumn colColony;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStructure;
        private System.Windows.Forms.DataGridViewTextBoxColumn colType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStatus;
        private System.Windows.Forms.Label lblFutureNote;
        private System.Windows.Forms.FlowLayoutPanel flpButtons;
        private System.Windows.Forms.Button cmdAllocate;
        private System.Windows.Forms.Button cmdCancel;
    }
}
