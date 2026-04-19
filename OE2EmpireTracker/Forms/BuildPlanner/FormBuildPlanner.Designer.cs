namespace OE2EmpireTracker.Forms.BuildPlanner
{
    partial class FormBuildPlanner
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
            this.flpBase = new System.Windows.Forms.FlowLayoutPanel();
            this.flpSearchList = new System.Windows.Forms.FlowLayoutPanel();
            this.flpPlanFilter = new System.Windows.Forms.FlowLayoutPanel();
            this.lblPlanFilter = new System.Windows.Forms.Label();
            this.txtPlanFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lvwPlans = new System.Windows.Forms.ListView();
            this.flpCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdNew = new System.Windows.Forms.Button();
            this.cmdDelete = new System.Windows.Forms.Button();
            this.flpDetail = new System.Windows.Forms.FlowLayoutPanel();
            this.flpPlanName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblPlanName = new System.Windows.Forms.Label();
            this.txtPlanName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpDescription = new System.Windows.Forms.FlowLayoutPanel();
            this.lblDescription = new System.Windows.Forms.Label();
            this.txtDescription = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpIsActive = new System.Windows.Forms.FlowLayoutPanel();
            this.chkIsActive = new System.Windows.Forms.CheckBox();
            this.cmdSave = new System.Windows.Forms.Button();
            this.dgvBuildItems = new System.Windows.Forms.DataGridView();
            this.colItemName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colItemType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colQuantity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLocation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNotes = new System.Windows.Forms.DataGridViewTextBoxColumn();

            this.flpBase.SuspendLayout();
            this.flpSearchList.SuspendLayout();
            this.flpPlanFilter.SuspendLayout();
            this.flpCommands.SuspendLayout();
            this.flpDetail.SuspendLayout();
            this.flpPlanName.SuspendLayout();
            this.flpDescription.SuspendLayout();
            this.flpIsActive.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBuildItems)).BeginInit();
            this.SuspendLayout();
            // 
            // flpBase
            // 
            this.flpBase.Controls.Add(this.flpSearchList);
            this.flpBase.Controls.Add(this.flpDetail);
            this.flpBase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpBase.Location = new System.Drawing.Point(0, 0);
            this.flpBase.Name = "flpBase";
            this.flpBase.Size = new System.Drawing.Size(900, 600);
            this.flpBase.WrapContents = false;
            // 
            // flpSearchList
            // 
            this.flpSearchList.Controls.Add(this.flpPlanFilter);
            this.flpSearchList.Controls.Add(this.lvwPlans);
            this.flpSearchList.Controls.Add(this.flpCommands);
            this.flpSearchList.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpSearchList.Location = new System.Drawing.Point(3, 3);
            this.flpSearchList.Name = "flpSearchList";
            this.flpSearchList.Size = new System.Drawing.Size(220, 594);
            this.flpSearchList.WrapContents = false;
            // 
            // flpPlanFilter
            // 
            this.flpPlanFilter.AutoSize = true;
            this.flpPlanFilter.Controls.Add(this.lblPlanFilter);
            this.flpPlanFilter.Controls.Add(this.txtPlanFilter);
            this.flpPlanFilter.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpPlanFilter.Location = new System.Drawing.Point(3, 3);
            this.flpPlanFilter.Name = "flpPlanFilter";
            this.flpPlanFilter.Size = new System.Drawing.Size(214, 26);
            // 
            // lblPlanFilter
            // 
            this.lblPlanFilter.AutoSize = true;
            this.lblPlanFilter.Location = new System.Drawing.Point(3, 5);
            this.lblPlanFilter.Name = "lblPlanFilter";
            this.lblPlanFilter.Size = new System.Drawing.Size(32, 13);
            this.lblPlanFilter.Text = "Filter:";
            this.lblPlanFilter.Anchor = System.Windows.Forms.AnchorStyles.Left;
            // 
            // txtPlanFilter
            // 
            this.txtPlanFilter.Location = new System.Drawing.Point(41, 3);
            this.txtPlanFilter.Name = "txtPlanFilter";
            this.txtPlanFilter.Size = new System.Drawing.Size(170, 20);
            // 
            // lvwPlans
            // 
            this.lvwPlans.FullRowSelect = true;
            this.lvwPlans.HideSelection = false;
            this.lvwPlans.Location = new System.Drawing.Point(3, 35);
            this.lvwPlans.MultiSelect = false;
            this.lvwPlans.Name = "lvwPlans";
            this.lvwPlans.Size = new System.Drawing.Size(214, 520);
            this.lvwPlans.UseCompatibleStateImageBehavior = false;
            this.lvwPlans.View = System.Windows.Forms.View.Details;
            // 
            // flpCommands
            // 
            this.flpCommands.AutoSize = true;
            this.flpCommands.Controls.Add(this.cmdNew);
            this.flpCommands.Controls.Add(this.cmdDelete);
            this.flpCommands.Location = new System.Drawing.Point(3, 561);
            this.flpCommands.Name = "flpCommands";
            this.flpCommands.Size = new System.Drawing.Size(214, 29);
            // 
            // cmdNew
            // 
            this.cmdNew.Location = new System.Drawing.Point(3, 3);
            this.cmdNew.Name = "cmdNew";
            this.cmdNew.Size = new System.Drawing.Size(75, 23);
            this.cmdNew.Text = "New";
            this.cmdNew.UseVisualStyleBackColor = true;
            // 
            // cmdDelete
            // 
            this.cmdDelete.Location = new System.Drawing.Point(84, 3);
            this.cmdDelete.Name = "cmdDelete";
            this.cmdDelete.Size = new System.Drawing.Size(75, 23);
            this.cmdDelete.Text = "Delete";
            this.cmdDelete.UseVisualStyleBackColor = true;
            // 
            // flpDetail
            // 
            this.flpDetail.Controls.Add(this.flpPlanName);
            this.flpDetail.Controls.Add(this.flpDescription);
            this.flpDetail.Controls.Add(this.flpIsActive);
            this.flpDetail.Controls.Add(this.cmdSave);
            this.flpDetail.Controls.Add(this.dgvBuildItems);
            this.flpDetail.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpDetail.Location = new System.Drawing.Point(229, 3);
            this.flpDetail.Name = "flpDetail";
            this.flpDetail.Size = new System.Drawing.Size(668, 594);
            this.flpDetail.WrapContents = false;
            // 
            // flpPlanName
            // 
            this.flpPlanName.AutoSize = true;
            this.flpPlanName.Controls.Add(this.lblPlanName);
            this.flpPlanName.Controls.Add(this.txtPlanName);
            this.flpPlanName.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpPlanName.Location = new System.Drawing.Point(3, 3);
            this.flpPlanName.Name = "flpPlanName";
            this.flpPlanName.Size = new System.Drawing.Size(662, 26);
            // 
            // lblPlanName
            // 
            this.lblPlanName.AutoSize = true;
            this.lblPlanName.Location = new System.Drawing.Point(3, 5);
            this.lblPlanName.Name = "lblPlanName";
            this.lblPlanName.Size = new System.Drawing.Size(38, 13);
            this.lblPlanName.Text = "Name:";
            this.lblPlanName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            // 
            // txtPlanName
            // 
            this.txtPlanName.Location = new System.Drawing.Point(47, 3);
            this.txtPlanName.Name = "txtPlanName";
            this.txtPlanName.Size = new System.Drawing.Size(300, 20);
            // 
            // flpDescription
            // 
            this.flpDescription.AutoSize = true;
            this.flpDescription.Controls.Add(this.lblDescription);
            this.flpDescription.Controls.Add(this.txtDescription);
            this.flpDescription.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpDescription.Location = new System.Drawing.Point(3, 35);
            this.flpDescription.Name = "flpDescription";
            this.flpDescription.Size = new System.Drawing.Size(662, 26);
            // 
            // lblDescription
            // 
            this.lblDescription.AutoSize = true;
            this.lblDescription.Location = new System.Drawing.Point(3, 5);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(63, 13);
            this.lblDescription.Text = "Description:";
            this.lblDescription.Anchor = System.Windows.Forms.AnchorStyles.Left;
            // 
            // txtDescription
            // 
            this.txtDescription.Location = new System.Drawing.Point(72, 3);
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.Size = new System.Drawing.Size(400, 20);
            // 
            // flpIsActive
            // 
            this.flpIsActive.AutoSize = true;
            this.flpIsActive.Controls.Add(this.chkIsActive);
            this.flpIsActive.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpIsActive.Location = new System.Drawing.Point(3, 67);
            this.flpIsActive.Name = "flpIsActive";
            this.flpIsActive.Size = new System.Drawing.Size(662, 23);
            // 
            // chkIsActive
            // 
            this.chkIsActive.AutoSize = true;
            this.chkIsActive.Location = new System.Drawing.Point(3, 3);
            this.chkIsActive.Name = "chkIsActive";
            this.chkIsActive.Size = new System.Drawing.Size(56, 17);
            this.chkIsActive.Text = "Active";
            this.chkIsActive.UseVisualStyleBackColor = true;
            // 
            // cmdSave
            // 
            this.cmdSave.Location = new System.Drawing.Point(3, 96);
            this.cmdSave.Name = "cmdSave";
            this.cmdSave.Size = new System.Drawing.Size(75, 23);
            this.cmdSave.Text = "Save";
            this.cmdSave.UseVisualStyleBackColor = true;
            // 
            // dgvBuildItems
            // 
            this.dgvBuildItems.AllowUserToAddRows = false;
            this.dgvBuildItems.AllowUserToDeleteRows = false;
            this.dgvBuildItems.AllowUserToOrderColumns = true;
            this.dgvBuildItems.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvBuildItems.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colItemName,
            this.colItemType,
            this.colQuantity,
            this.colStatus,
            this.colLocation,
            this.colNotes});
            this.dgvBuildItems.Location = new System.Drawing.Point(3, 125);
            this.dgvBuildItems.Name = "dgvBuildItems";
            this.dgvBuildItems.ReadOnly = true;
            this.dgvBuildItems.Size = new System.Drawing.Size(662, 460);
            this.dgvBuildItems.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            // 
            // colItemName
            // 
            this.colItemName.HeaderText = "Item Name";
            this.colItemName.Name = "colItemName";
            this.colItemName.ReadOnly = true;
            this.colItemName.Width = 200;
            // 
            // colItemType
            // 
            this.colItemType.HeaderText = "Type";
            this.colItemType.Name = "colItemType";
            this.colItemType.ReadOnly = true;
            this.colItemType.Width = 100;
            // 
            // colQuantity
            // 
            this.colQuantity.HeaderText = "Qty";
            this.colQuantity.Name = "colQuantity";
            this.colQuantity.ReadOnly = true;
            this.colQuantity.Width = 70;
            // 
            // colStatus
            // 
            this.colStatus.HeaderText = "Status";
            this.colStatus.Name = "colStatus";
            this.colStatus.ReadOnly = true;
            this.colStatus.Width = 90;
            // 
            // colLocation
            // 
            this.colLocation.HeaderText = "Location";
            this.colLocation.Name = "colLocation";
            this.colLocation.ReadOnly = true;
            this.colLocation.Width = 150;
            // 
            // colNotes
            // 
            this.colNotes.HeaderText = "Notes";
            this.colNotes.Name = "colNotes";
            this.colNotes.ReadOnly = true;
            this.colNotes.Width = 200;
            // 
            // FormBuildPlanner
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(this.flpBase);
            this.Name = "FormBuildPlanner";
            this.Text = "Build Planner";
            this.flpBase.ResumeLayout(false);
            this.flpSearchList.ResumeLayout(false);
            this.flpSearchList.PerformLayout();
            this.flpPlanFilter.ResumeLayout(false);
            this.flpPlanFilter.PerformLayout();
            this.flpCommands.ResumeLayout(false);
            this.flpDetail.ResumeLayout(false);
            this.flpDetail.PerformLayout();
            this.flpPlanName.ResumeLayout(false);
            this.flpPlanName.PerformLayout();
            this.flpDescription.ResumeLayout(false);
            this.flpDescription.PerformLayout();
            this.flpIsActive.ResumeLayout(false);
            this.flpIsActive.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBuildItems)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flpBase;
        private System.Windows.Forms.FlowLayoutPanel flpSearchList;
        private System.Windows.Forms.FlowLayoutPanel flpPlanFilter;
        private System.Windows.Forms.Label lblPlanFilter;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtPlanFilter;
        private System.Windows.Forms.ListView lvwPlans;
        private System.Windows.Forms.FlowLayoutPanel flpCommands;
        private System.Windows.Forms.Button cmdNew;
        private System.Windows.Forms.Button cmdDelete;
        private System.Windows.Forms.FlowLayoutPanel flpDetail;
        private System.Windows.Forms.FlowLayoutPanel flpPlanName;
        private System.Windows.Forms.Label lblPlanName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtPlanName;
        private System.Windows.Forms.FlowLayoutPanel flpDescription;
        private System.Windows.Forms.Label lblDescription;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtDescription;
        private System.Windows.Forms.FlowLayoutPanel flpIsActive;
        private System.Windows.Forms.CheckBox chkIsActive;
        private System.Windows.Forms.Button cmdSave;
        private System.Windows.Forms.DataGridView dgvBuildItems;
        private System.Windows.Forms.DataGridViewTextBoxColumn colItemName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colItemType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colQuantity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLocation;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNotes;
    }
}
