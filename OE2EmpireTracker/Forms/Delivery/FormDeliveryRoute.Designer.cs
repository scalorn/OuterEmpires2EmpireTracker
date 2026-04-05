namespace OE2EmpireTracker.Forms.Delivery
{
    partial class FormDeliveryRoute
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
            this.flpRouteFilter = new System.Windows.Forms.FlowLayoutPanel();
            this.lblRouteFilter = new System.Windows.Forms.Label();
            this.txtRouteFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lvwRoutes = new System.Windows.Forms.ListView();
            this.flpRouteData = new System.Windows.Forms.FlowLayoutPanel();
            this.flpRouteName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblRouteName = new System.Windows.Forms.Label();
            this.txtRouteName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.dgvStops = new System.Windows.Forms.DataGridView();
            this.colSequence = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colColonyName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPlanetName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSystemName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpAddStop = new System.Windows.Forms.FlowLayoutPanel();
            this.lblAddStop = new System.Windows.Forms.Label();
            this.cmbColony = new System.Windows.Forms.ComboBox();
            this.cmdAddStop = new System.Windows.Forms.Button();
            this.cmdUp = new System.Windows.Forms.Button();
            this.cmdDown = new System.Windows.Forms.Button();
            this.cmdRemoveStop = new System.Windows.Forms.Button();
            this.chkPreventDuplicates = new System.Windows.Forms.CheckBox();
            this.flpCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdNew = new System.Windows.Forms.Button();
            this.cmdSave = new System.Windows.Forms.Button();
            this.cmdDelete = new System.Windows.Forms.Button();
            this.flpBase.SuspendLayout();
            this.flpSearchList.SuspendLayout();
            this.flpRouteFilter.SuspendLayout();
            this.flpRouteData.SuspendLayout();
            this.flpRouteName.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStops)).BeginInit();
            this.flpAddStop.SuspendLayout();
            this.flpCommands.SuspendLayout();
            this.SuspendLayout();
            // 
            // flpBase
            // 
            this.flpBase.Controls.Add(this.flpSearchList);
            this.flpBase.Controls.Add(this.flpRouteData);
            this.flpBase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpBase.Location = new System.Drawing.Point(0, 0);
            this.flpBase.Name = "flpBase";
            this.flpBase.Size = new System.Drawing.Size(900, 550);
            this.flpBase.TabIndex = 0;
            this.flpBase.WrapContents = false;
            // 
            // flpSearchList
            // 
            this.flpSearchList.Controls.Add(this.flpRouteFilter);
            this.flpSearchList.Controls.Add(this.lvwRoutes);
            this.flpSearchList.Location = new System.Drawing.Point(3, 3);
            this.flpSearchList.Name = "flpSearchList";
            this.flpSearchList.Size = new System.Drawing.Size(250, 544);
            this.flpSearchList.TabIndex = 0;
            // 
            // flpRouteFilter
            // 
            this.flpRouteFilter.Controls.Add(this.lblRouteFilter);
            this.flpRouteFilter.Controls.Add(this.txtRouteFilter);
            this.flpRouteFilter.Location = new System.Drawing.Point(2, 2);
            this.flpRouteFilter.Margin = new System.Windows.Forms.Padding(2);
            this.flpRouteFilter.Name = "flpRouteFilter";
            this.flpRouteFilter.Size = new System.Drawing.Size(246, 26);
            this.flpRouteFilter.TabIndex = 0;
            // 
            // lblRouteFilter
            // 
            this.lblRouteFilter.Location = new System.Drawing.Point(2, 4);
            this.lblRouteFilter.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblRouteFilter.Name = "lblRouteFilter";
            this.lblRouteFilter.Size = new System.Drawing.Size(40, 17);
            this.lblRouteFilter.TabIndex = 0;
            this.lblRouteFilter.Text = "Filter";
            this.lblRouteFilter.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtRouteFilter
            // 
            this.txtRouteFilter.Location = new System.Drawing.Point(47, 3);
            this.txtRouteFilter.Name = "txtRouteFilter";
            this.txtRouteFilter.Size = new System.Drawing.Size(196, 20);
            this.txtRouteFilter.TabIndex = 1;
            // 
            // lvwRoutes
            // 
            this.lvwRoutes.Location = new System.Drawing.Point(3, 33);
            this.lvwRoutes.Name = "lvwRoutes";
            this.lvwRoutes.Size = new System.Drawing.Size(244, 500);
            this.lvwRoutes.TabIndex = 1;
            this.lvwRoutes.UseCompatibleStateImageBehavior = false;
            // 
            // flpRouteData
            // 
            this.flpRouteData.Controls.Add(this.flpRouteName);
            this.flpRouteData.Controls.Add(this.dgvStops);
            this.flpRouteData.Controls.Add(this.flpAddStop);
            this.flpRouteData.Controls.Add(this.flpCommands);
            this.flpRouteData.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpRouteData.Location = new System.Drawing.Point(259, 3);
            this.flpRouteData.Name = "flpRouteData";
            this.flpRouteData.Size = new System.Drawing.Size(638, 544);
            this.flpRouteData.TabIndex = 1;
            this.flpRouteData.WrapContents = false;
            // 
            // flpRouteName
            // 
            this.flpRouteName.Controls.Add(this.lblRouteName);
            this.flpRouteName.Controls.Add(this.txtRouteName);
            this.flpRouteName.Location = new System.Drawing.Point(2, 2);
            this.flpRouteName.Margin = new System.Windows.Forms.Padding(2);
            this.flpRouteName.Name = "flpRouteName";
            this.flpRouteName.Size = new System.Drawing.Size(400, 26);
            this.flpRouteName.TabIndex = 0;
            // 
            // lblRouteName
            // 
            this.lblRouteName.Location = new System.Drawing.Point(2, 4);
            this.lblRouteName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblRouteName.Name = "lblRouteName";
            this.lblRouteName.Size = new System.Drawing.Size(80, 17);
            this.lblRouteName.TabIndex = 0;
            this.lblRouteName.Text = "Route Name";
            this.lblRouteName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtRouteName
            // 
            this.txtRouteName.Location = new System.Drawing.Point(87, 3);
            this.txtRouteName.Name = "txtRouteName";
            this.txtRouteName.Size = new System.Drawing.Size(300, 20);
            this.txtRouteName.TabIndex = 1;
            // 
            // dgvStops
            // 
            this.dgvStops.AllowUserToAddRows = false;
            this.dgvStops.AllowUserToDeleteRows = false;
            this.dgvStops.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvStops.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colSequence,
            this.colColonyName,
            this.colPlanetName,
            this.colSystemName});
            this.dgvStops.Location = new System.Drawing.Point(2, 32);
            this.dgvStops.Margin = new System.Windows.Forms.Padding(2);
            this.dgvStops.Name = "dgvStops";
            this.dgvStops.ReadOnly = true;
            this.dgvStops.RowHeadersVisible = false;
            this.dgvStops.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvStops.Size = new System.Drawing.Size(630, 400);
            this.dgvStops.TabIndex = 1;
            // 
            // colSequence
            // 
            this.colSequence.HeaderText = "#";
            this.colSequence.Name = "colSequence";
            this.colSequence.ReadOnly = true;
            this.colSequence.Width = 30;
            // 
            // colColonyName
            // 
            this.colColonyName.HeaderText = "Colony";
            this.colColonyName.Name = "colColonyName";
            this.colColonyName.ReadOnly = true;
            this.colColonyName.Width = 180;
            // 
            // colPlanetName
            // 
            this.colPlanetName.HeaderText = "Planet";
            this.colPlanetName.Name = "colPlanetName";
            this.colPlanetName.ReadOnly = true;
            this.colPlanetName.Width = 180;
            // 
            // colSystemName
            // 
            this.colSystemName.HeaderText = "System";
            this.colSystemName.Name = "colSystemName";
            this.colSystemName.ReadOnly = true;
            this.colSystemName.Width = 180;
            // 
            // flpAddStop
            // 
            this.flpAddStop.Controls.Add(this.chkPreventDuplicates);
            this.flpAddStop.Controls.Add(this.lblAddStop);
            this.flpAddStop.Controls.Add(this.cmbColony);
            this.flpAddStop.Controls.Add(this.cmdAddStop);
            this.flpAddStop.Controls.Add(this.cmdUp);
            this.flpAddStop.Controls.Add(this.cmdDown);
            this.flpAddStop.Controls.Add(this.cmdRemoveStop);
            this.flpAddStop.Location = new System.Drawing.Point(2, 436);
            this.flpAddStop.Margin = new System.Windows.Forms.Padding(2);
            this.flpAddStop.Name = "flpAddStop";
            this.flpAddStop.Size = new System.Drawing.Size(630, 30);
            this.flpAddStop.TabIndex = 2;
            // 
            // chkPreventDuplicates
            // 
            this.chkPreventDuplicates.AutoSize = true;
            this.chkPreventDuplicates.Location = new System.Drawing.Point(3, 5);
            this.chkPreventDuplicates.Name = "chkPreventDuplicates";
            this.chkPreventDuplicates.Size = new System.Drawing.Size(95, 17);
            this.chkPreventDuplicates.TabIndex = 6;
            this.chkPreventDuplicates.Text = "No Duplicates";
            this.chkPreventDuplicates.UseVisualStyleBackColor = true;
            // 
            // lblAddStop
            // 
            this.lblAddStop.Location = new System.Drawing.Point(2, 4);
            this.lblAddStop.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblAddStop.Name = "lblAddStop";
            this.lblAddStop.Size = new System.Drawing.Size(50, 17);
            this.lblAddStop.TabIndex = 0;
            this.lblAddStop.Text = "Colony";
            this.lblAddStop.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbColony
            // 
            this.cmbColony.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbColony.Location = new System.Drawing.Point(57, 3);
            this.cmbColony.Name = "cmbColony";
            this.cmbColony.Size = new System.Drawing.Size(280, 21);
            this.cmbColony.TabIndex = 1;
            // 
            // cmdAddStop
            // 
            this.cmdAddStop.Location = new System.Drawing.Point(343, 3);
            this.cmdAddStop.Name = "cmdAddStop";
            this.cmdAddStop.Size = new System.Drawing.Size(50, 23);
            this.cmdAddStop.TabIndex = 2;
            this.cmdAddStop.Text = "Add";
            this.cmdAddStop.UseVisualStyleBackColor = true;
            // 
            // cmdUp
            // 
            this.cmdUp.Location = new System.Drawing.Point(399, 3);
            this.cmdUp.Name = "cmdUp";
            this.cmdUp.Size = new System.Drawing.Size(40, 23);
            this.cmdUp.TabIndex = 3;
            this.cmdUp.Text = "Up";
            this.cmdUp.UseVisualStyleBackColor = true;
            // 
            // cmdDown
            // 
            this.cmdDown.Location = new System.Drawing.Point(445, 3);
            this.cmdDown.Name = "cmdDown";
            this.cmdDown.Size = new System.Drawing.Size(50, 23);
            this.cmdDown.TabIndex = 4;
            this.cmdDown.Text = "Down";
            this.cmdDown.UseVisualStyleBackColor = true;
            // 
            // cmdRemoveStop
            // 
            this.cmdRemoveStop.Location = new System.Drawing.Point(501, 3);
            this.cmdRemoveStop.Name = "cmdRemoveStop";
            this.cmdRemoveStop.Size = new System.Drawing.Size(60, 23);
            this.cmdRemoveStop.TabIndex = 5;
            this.cmdRemoveStop.Text = "Remove";
            this.cmdRemoveStop.UseVisualStyleBackColor = true;
            // 
            // flpCommands
            // 
            this.flpCommands.Controls.Add(this.cmdNew);
            this.flpCommands.Controls.Add(this.cmdSave);
            this.flpCommands.Controls.Add(this.cmdDelete);
            this.flpCommands.Location = new System.Drawing.Point(2, 470);
            this.flpCommands.Margin = new System.Windows.Forms.Padding(2);
            this.flpCommands.Name = "flpCommands";
            this.flpCommands.Size = new System.Drawing.Size(630, 30);
            this.flpCommands.TabIndex = 3;
            // 
            // cmdNew
            // 
            this.cmdNew.Location = new System.Drawing.Point(3, 3);
            this.cmdNew.Name = "cmdNew";
            this.cmdNew.Size = new System.Drawing.Size(60, 23);
            this.cmdNew.TabIndex = 0;
            this.cmdNew.Text = "New";
            this.cmdNew.UseVisualStyleBackColor = true;
            // 
            // cmdSave
            // 
            this.cmdSave.Location = new System.Drawing.Point(69, 3);
            this.cmdSave.Name = "cmdSave";
            this.cmdSave.Size = new System.Drawing.Size(60, 23);
            this.cmdSave.TabIndex = 1;
            this.cmdSave.Text = "Save";
            this.cmdSave.UseVisualStyleBackColor = true;
            // 
            // cmdDelete
            // 
            this.cmdDelete.Location = new System.Drawing.Point(135, 3);
            this.cmdDelete.Name = "cmdDelete";
            this.cmdDelete.Size = new System.Drawing.Size(60, 23);
            this.cmdDelete.TabIndex = 2;
            this.cmdDelete.Text = "Delete";
            this.cmdDelete.UseVisualStyleBackColor = true;
            // 
            // FormDeliveryRoute
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 550);
            this.Controls.Add(this.flpBase);
            this.Name = "FormDeliveryRoute";
            this.Text = "Delivery Routes";
            this.flpBase.ResumeLayout(false);
            this.flpSearchList.ResumeLayout(false);
            this.flpRouteFilter.ResumeLayout(false);
            this.flpRouteFilter.PerformLayout();
            this.flpRouteData.ResumeLayout(false);
            this.flpRouteName.ResumeLayout(false);
            this.flpRouteName.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStops)).EndInit();
            this.flpAddStop.ResumeLayout(false);
            this.flpCommands.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flpBase;
        private System.Windows.Forms.FlowLayoutPanel flpSearchList;
        private System.Windows.Forms.FlowLayoutPanel flpRouteFilter;
        private System.Windows.Forms.Label lblRouteFilter;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtRouteFilter;
        private System.Windows.Forms.ListView lvwRoutes;
        private System.Windows.Forms.FlowLayoutPanel flpRouteData;
        private System.Windows.Forms.FlowLayoutPanel flpRouteName;
        private System.Windows.Forms.Label lblRouteName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtRouteName;
        private System.Windows.Forms.DataGridView dgvStops;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSequence;
        private System.Windows.Forms.DataGridViewTextBoxColumn colColonyName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPlanetName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSystemName;
        private System.Windows.Forms.FlowLayoutPanel flpAddStop;
        private System.Windows.Forms.Label lblAddStop;
        private System.Windows.Forms.ComboBox cmbColony;
        private System.Windows.Forms.Button cmdAddStop;
        private System.Windows.Forms.Button cmdUp;
        private System.Windows.Forms.Button cmdDown;
        private System.Windows.Forms.Button cmdRemoveStop;
        private System.Windows.Forms.CheckBox chkPreventDuplicates;
        private System.Windows.Forms.FlowLayoutPanel flpCommands;
        private System.Windows.Forms.Button cmdNew;
        private System.Windows.Forms.Button cmdSave;
        private System.Windows.Forms.Button cmdDelete;
    }
}
