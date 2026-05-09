namespace OE2EmpireTracker.Forms.ShipTemplate
{
    partial class FormShipTemplate
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
            this.components = new System.ComponentModel.Container();
            this.cmsSlots = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmiClearSlot = new System.Windows.Forms.ToolStripMenuItem();
            this.flpBase = new System.Windows.Forms.FlowLayoutPanel();
            this.flpSearchList = new System.Windows.Forms.FlowLayoutPanel();
            this.flpFilter = new System.Windows.Forms.FlowLayoutPanel();
            this.lblFilter = new System.Windows.Forms.Label();
            this.txtFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lvwTemplates = new System.Windows.Forms.ListView();
            this.flpCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdNew = new System.Windows.Forms.Button();
            this.cmdSave = new System.Windows.Forms.Button();
            this.cmdDelete = new System.Windows.Forms.Button();
            this.cmdOrderBuild = new System.Windows.Forms.Button();
            this.flpDetail = new System.Windows.Forms.FlowLayoutPanel();
            this.flpName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpHull = new System.Windows.Forms.FlowLayoutPanel();
            this.lblHull = new System.Windows.Forms.Label();
            this.cmbHull = new OE2EmpireTracker.Controls.FilteredTextComboSet();
            this.cmdSave = new System.Windows.Forms.Button();
            this.dgvSlots = new System.Windows.Forms.DataGridView();
            this.colSlotType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSlotIndex = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colComponent = new OE2EmpireTracker.Controls.DataGridViewFilteredComboBoxColumn();
            this.rtbStats = new System.Windows.Forms.RichTextBox();

            this.flpBase.SuspendLayout();
            this.flpSearchList.SuspendLayout();
            this.flpFilter.SuspendLayout();
            this.flpCommands.SuspendLayout();
            this.flpDetail.SuspendLayout();
            this.flpName.SuspendLayout();
            this.flpHull.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSlots)).BeginInit();
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
            this.flpSearchList.Controls.Add(this.flpFilter);
            this.flpSearchList.Controls.Add(this.lvwTemplates);
            this.flpSearchList.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpSearchList.Location = new System.Drawing.Point(3, 3);
            this.flpSearchList.Name = "flpSearchList";
            this.flpSearchList.Size = new System.Drawing.Size(220, 594);
            this.flpSearchList.WrapContents = false;
            //
            // flpFilter
            //
            this.flpFilter.AutoSize = true;
            this.flpFilter.Controls.Add(this.lblFilter);
            this.flpFilter.Controls.Add(this.txtFilter);
            this.flpFilter.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpFilter.Location = new System.Drawing.Point(3, 3);
            this.flpFilter.Name = "flpFilter";
            this.flpFilter.Size = new System.Drawing.Size(214, 26);
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
            this.txtFilter.Size = new System.Drawing.Size(170, 20);
            //
            // lvwTemplates
            //
            this.lvwTemplates.FullRowSelect = true;
            this.lvwTemplates.HideSelection = false;
            this.lvwTemplates.Location = new System.Drawing.Point(3, 35);
            this.lvwTemplates.MultiSelect = false;
            this.lvwTemplates.Name = "lvwTemplates";
            this.lvwTemplates.Size = new System.Drawing.Size(214, 555);
            this.lvwTemplates.UseCompatibleStateImageBehavior = false;
            this.lvwTemplates.View = System.Windows.Forms.View.Details;
            //
            // flpCommands
            //
            this.flpCommands.AutoSize = true;
            this.flpCommands.Controls.Add(this.cmdNew);
            this.flpCommands.Controls.Add(this.cmdSave);
            this.flpCommands.Controls.Add(this.cmdDelete);
            this.flpCommands.Controls.Add(this.cmdOrderBuild);
            this.flpCommands.Location = new System.Drawing.Point(3, 561);
            this.flpCommands.Name = "flpCommands";
            this.flpCommands.Size = new System.Drawing.Size(662, 29);
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
            // cmdOrderBuild
            //
            this.cmdOrderBuild.Location = new System.Drawing.Point(165, 3);
            this.cmdOrderBuild.Name = "cmdOrderBuild";
            this.cmdOrderBuild.Size = new System.Drawing.Size(85, 23);
            this.cmdOrderBuild.Text = "Order Build";
            this.cmdOrderBuild.UseVisualStyleBackColor = true;
            //
            // flpDetail
            //
            this.flpDetail.Controls.Add(this.flpName);
            this.flpDetail.Controls.Add(this.flpHull);
            this.flpDetail.Controls.Add(this.dgvSlots);
            this.flpDetail.Controls.Add(this.rtbStats);
            this.flpDetail.Controls.Add(this.flpCommands);
            this.flpDetail.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpDetail.Location = new System.Drawing.Point(229, 3);
            this.flpDetail.Name = "flpDetail";
            this.flpDetail.Size = new System.Drawing.Size(668, 594);
            this.flpDetail.WrapContents = false;
            //
            // flpName
            //
            this.flpName.AutoSize = true;
            this.flpName.Controls.Add(this.lblName);
            this.flpName.Controls.Add(this.txtName);
            this.flpName.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpName.Location = new System.Drawing.Point(3, 3);
            this.flpName.Name = "flpName";
            this.flpName.Size = new System.Drawing.Size(662, 26);
            //
            // lblName
            //
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(3, 5);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(38, 13);
            this.lblName.Text = "Name:";
            this.lblName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            //
            // txtName
            //
            this.txtName.Location = new System.Drawing.Point(47, 3);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(300, 20);
            //
            // flpHull
            //
            this.flpHull.AutoSize = true;
            this.flpHull.Controls.Add(this.lblHull);
            this.flpHull.Controls.Add(this.cmbHull);
            this.flpHull.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpHull.Location = new System.Drawing.Point(3, 35);
            this.flpHull.Name = "flpHull";
            this.flpHull.Size = new System.Drawing.Size(662, 27);
            //
            // lblHull
            //
            this.lblHull.AutoSize = true;
            this.lblHull.Location = new System.Drawing.Point(3, 5);
            this.lblHull.Name = "lblHull";
            this.lblHull.Size = new System.Drawing.Size(30, 13);
            this.lblHull.Text = "Hull:";
            this.lblHull.Anchor = System.Windows.Forms.AnchorStyles.Left;
            //
            // cmbHull
            //
            this.cmbHull.Location = new System.Drawing.Point(39, 3);
            this.cmbHull.Name = "cmbHull";
            this.cmbHull.Size = new System.Drawing.Size(350, 25);
            //
            // cmdSave
            //
            this.cmdSave.Location = new System.Drawing.Point(84, 3);
            this.cmdSave.Name = "cmdSave";
            this.cmdSave.Size = new System.Drawing.Size(75, 23);
            this.cmdSave.Text = "Save";
            this.cmdSave.UseVisualStyleBackColor = true;
            //
            // dgvSlots
            //
            this.dgvSlots.ContextMenuStrip = this.cmsSlots;
            this.dgvSlots.AllowUserToAddRows = false;
            this.dgvSlots.AllowUserToDeleteRows = false;
            this.dgvSlots.AllowUserToOrderColumns = true;
            this.dgvSlots.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSlots.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colSlotType,
            this.colSlotIndex,
            this.colComponent});
            this.dgvSlots.Location = new System.Drawing.Point(3, 68);
            this.dgvSlots.Name = "dgvSlots";
            this.dgvSlots.Size = new System.Drawing.Size(662, 300);
            this.dgvSlots.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dgvSlots.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            //
            // colSlotType
            //
            this.colSlotType.HeaderText = "Slot Type";
            this.colSlotType.Name = "colSlotType";
            this.colSlotType.ReadOnly = true;
            this.colSlotType.Width = 150;
            //
            // colSlotIndex
            //
            this.colSlotIndex.HeaderText = "#";
            this.colSlotIndex.Name = "colSlotIndex";
            this.colSlotIndex.ReadOnly = true;
            this.colSlotIndex.Width = 40;
            //
            // colComponent
            //
            this.colComponent.HeaderText = "Component";
            this.colComponent.Name = "colComponent";
            this.colComponent.Width = 350;
            //
            // rtbStats
            //
            this.rtbStats.Location = new System.Drawing.Point(3, 403);
            this.rtbStats.Name = "rtbStats";
            this.rtbStats.ReadOnly = true;
            this.rtbStats.Size = new System.Drawing.Size(662, 185);
            this.rtbStats.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.rtbStats.BackColor = System.Drawing.SystemColors.Window;
            //
            // cmsSlots
            //
            this.cmsSlots.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiClearSlot});
            this.cmsSlots.Name = "cmsSlots";
            this.cmsSlots.Size = new System.Drawing.Size(126, 26);
            //
            // tsmiClearSlot
            //
            this.tsmiClearSlot.Name = "tsmiClearSlot";
            this.tsmiClearSlot.Size = new System.Drawing.Size(125, 22);
            this.tsmiClearSlot.Text = "Clear Slot";
            //
            // FormShipTemplate
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(this.flpBase);
            this.Name = "FormShipTemplate";
            this.Text = "Ship Templates";
            this.flpBase.ResumeLayout(false);
            this.flpSearchList.ResumeLayout(false);
            this.flpSearchList.PerformLayout();
            this.flpFilter.ResumeLayout(false);
            this.flpFilter.PerformLayout();
            this.flpCommands.ResumeLayout(false);
            this.flpDetail.ResumeLayout(false);
            this.flpDetail.PerformLayout();
            this.flpName.ResumeLayout(false);
            this.flpName.PerformLayout();
            this.flpHull.ResumeLayout(false);
            this.flpHull.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSlots)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flpBase;
        private System.Windows.Forms.FlowLayoutPanel flpSearchList;
        private System.Windows.Forms.FlowLayoutPanel flpFilter;
        private System.Windows.Forms.Label lblFilter;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtFilter;
        private System.Windows.Forms.ListView lvwTemplates;
        private System.Windows.Forms.FlowLayoutPanel flpCommands;
        private System.Windows.Forms.Button cmdNew;
        private System.Windows.Forms.Button cmdDelete;
        private System.Windows.Forms.Button cmdOrderBuild;
        private System.Windows.Forms.FlowLayoutPanel flpDetail;
        private System.Windows.Forms.FlowLayoutPanel flpName;
        private System.Windows.Forms.Label lblName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtName;
        private System.Windows.Forms.FlowLayoutPanel flpHull;
        private System.Windows.Forms.Label lblHull;
        private OE2EmpireTracker.Controls.FilteredTextComboSet cmbHull;
        private System.Windows.Forms.Button cmdSave;
        private System.Windows.Forms.DataGridView dgvSlots;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSlotType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSlotIndex;
        private OE2EmpireTracker.Controls.DataGridViewFilteredComboBoxColumn colComponent;
        private System.Windows.Forms.RichTextBox rtbStats;
        private System.Windows.Forms.ContextMenuStrip cmsSlots;
        private System.Windows.Forms.ToolStripMenuItem tsmiClearSlot;
    }
}