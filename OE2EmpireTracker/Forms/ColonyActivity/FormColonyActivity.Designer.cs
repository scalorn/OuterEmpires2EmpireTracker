namespace OE2EmpireTracker.Forms.ColonyActivity
{
    partial class FormColonyActivity
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.flpBase = new System.Windows.Forms.FlowLayoutPanel();
            this.flpFilters = new System.Windows.Forms.FlowLayoutPanel();
            this.chkBuilding = new System.Windows.Forms.CheckBox();
            this.chkManufacturing = new System.Windows.Forms.CheckBox();
            this.chkCommodityManufacturing = new System.Windows.Forms.CheckBox();
            this.chkCommodityRequest = new System.Windows.Forms.CheckBox();
            this.chkResearch = new System.Windows.Forms.CheckBox();
            this.chkMining = new System.Windows.Forms.CheckBox();
            this.chkRefining = new System.Windows.Forms.CheckBox();
            this.chkShowInactive = new System.Windows.Forms.CheckBox();
            this.txtFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.dgvActivities = new System.Windows.Forms.DataGridView();
            this.colCountDown = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSystemName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colColonyName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colActivityType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSource = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colProcessDetails = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSecondsRemaining = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.timerRefresh = new System.Windows.Forms.Timer(this.components);
            this.flpBase.SuspendLayout();
            this.flpFilters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvActivities)).BeginInit();
            this.SuspendLayout();
            // 
            // flpBase
            // 
            this.flpBase.Controls.Add(this.flpFilters);
            this.flpBase.Controls.Add(this.dgvActivities);
            this.flpBase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpBase.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpBase.Location = new System.Drawing.Point(0, 0);
            this.flpBase.Name = "flpBase";
            this.flpBase.Size = new System.Drawing.Size(1000, 600);
            this.flpBase.TabIndex = 0;
            this.flpBase.WrapContents = false;
            // 
            // flpFilters
            // 
            this.flpFilters.AutoSize = true;
            this.flpFilters.Controls.Add(this.chkBuilding);
            this.flpFilters.Controls.Add(this.chkManufacturing);
            this.flpFilters.Controls.Add(this.chkCommodityManufacturing);
            this.flpFilters.Controls.Add(this.chkCommodityRequest);
            this.flpFilters.Controls.Add(this.chkResearch);
            this.flpFilters.Controls.Add(this.chkMining);
            this.flpFilters.Controls.Add(this.chkRefining);
            this.flpFilters.Controls.Add(this.chkShowInactive);
            this.flpFilters.Controls.Add(this.txtFilter);
            this.flpFilters.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpFilters.Location = new System.Drawing.Point(3, 3);
            this.flpFilters.Name = "flpFilters";
            this.flpFilters.Size = new System.Drawing.Size(994, 30);
            this.flpFilters.TabIndex = 0;
            // 
            // chkBuilding
            // 
            this.chkBuilding.AutoSize = true;
            this.chkBuilding.Checked = true;
            this.chkBuilding.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkBuilding.Location = new System.Drawing.Point(3, 3);
            this.chkBuilding.Name = "chkBuilding";
            this.chkBuilding.Size = new System.Drawing.Size(66, 17);
            this.chkBuilding.TabIndex = 0;
            this.chkBuilding.Text = "Building";
            this.chkBuilding.UseVisualStyleBackColor = true;
            // 
            // chkManufacturing
            // 
            this.chkManufacturing.AutoSize = true;
            this.chkManufacturing.Checked = true;
            this.chkManufacturing.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkManufacturing.Location = new System.Drawing.Point(75, 3);
            this.chkManufacturing.Name = "chkManufacturing";
            this.chkManufacturing.Size = new System.Drawing.Size(95, 17);
            this.chkManufacturing.TabIndex = 1;
            this.chkManufacturing.Text = "Manufacturing";
            this.chkManufacturing.UseVisualStyleBackColor = true;
            // 
            // chkCommodityManufacturing
            // 
            this.chkCommodityManufacturing.AutoSize = true;
            this.chkCommodityManufacturing.Checked = true;
            this.chkCommodityManufacturing.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkCommodityManufacturing.Location = new System.Drawing.Point(176, 3);
            this.chkCommodityManufacturing.Name = "chkCommodityManufacturing";
            this.chkCommodityManufacturing.Size = new System.Drawing.Size(143, 17);
            this.chkCommodityManufacturing.TabIndex = 2;
            this.chkCommodityManufacturing.Text = "Commodity Manufacturing";
            this.chkCommodityManufacturing.UseVisualStyleBackColor = true;
            // 
            // chkCommodityRequest
            // 
            this.chkCommodityRequest.AutoSize = true;
            this.chkCommodityRequest.Checked = true;
            this.chkCommodityRequest.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkCommodityRequest.Location = new System.Drawing.Point(325, 3);
            this.chkCommodityRequest.Name = "chkCommodityRequest";
            this.chkCommodityRequest.Size = new System.Drawing.Size(118, 17);
            this.chkCommodityRequest.TabIndex = 3;
            this.chkCommodityRequest.Text = "Commodity Request";
            this.chkCommodityRequest.UseVisualStyleBackColor = true;
            // 
            // chkResearch
            // 
            this.chkResearch.AutoSize = true;
            this.chkResearch.Checked = true;
            this.chkResearch.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkResearch.Location = new System.Drawing.Point(449, 3);
            this.chkResearch.Name = "chkResearch";
            this.chkResearch.Size = new System.Drawing.Size(72, 17);
            this.chkResearch.TabIndex = 4;
            this.chkResearch.Text = "Research";
            this.chkResearch.UseVisualStyleBackColor = true;
            // 
            // chkMining
            // 
            this.chkMining.AutoSize = true;
            this.chkMining.Location = new System.Drawing.Point(527, 3);
            this.chkMining.Name = "chkMining";
            this.chkMining.Size = new System.Drawing.Size(57, 17);
            this.chkMining.TabIndex = 5;
            this.chkMining.Text = "Mining";
            this.chkMining.UseVisualStyleBackColor = true;
            // 
            // chkRefining
            // 
            this.chkRefining.AutoSize = true;
            this.chkRefining.Location = new System.Drawing.Point(590, 3);
            this.chkRefining.Name = "chkRefining";
            this.chkRefining.Size = new System.Drawing.Size(64, 17);
            this.chkRefining.TabIndex = 6;
            this.chkRefining.Text = "Refining";
            this.chkRefining.UseVisualStyleBackColor = true;
            // 
            // chkShowInactive
            // 
            this.chkShowInactive.AutoSize = true;
            this.chkShowInactive.Location = new System.Drawing.Point(660, 3);
            this.chkShowInactive.Name = "chkShowInactive";
            this.chkShowInactive.Size = new System.Drawing.Size(96, 17);
            this.chkShowInactive.TabIndex = 7;
            this.chkShowInactive.Text = "Show Inactive";
            this.chkShowInactive.UseVisualStyleBackColor = true;
            // 
            // txtFilter
            // 
            this.txtFilter.Location = new System.Drawing.Point(660, 3);
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.Size = new System.Drawing.Size(200, 20);
            this.txtFilter.TabIndex = 8;
            // 
            // dgvActivities
            // 
            this.dgvActivities.AllowUserToAddRows = false;
            this.dgvActivities.AllowUserToDeleteRows = false;
            this.dgvActivities.AllowUserToOrderColumns = true;
            this.dgvActivities.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvActivities.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colCountDown,
            this.colSystemName,
            this.colColonyName,
            this.colActivityType,
            this.colSource,
            this.colProcessDetails,
            this.colSecondsRemaining});
            this.dgvActivities.Location = new System.Drawing.Point(3, 39);
            this.dgvActivities.Name = "dgvActivities";
            this.dgvActivities.ReadOnly = true;
            this.dgvActivities.RowHeadersVisible = false;
            this.dgvActivities.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvActivities.Size = new System.Drawing.Size(994, 558);
            this.dgvActivities.TabIndex = 1;
            // 
            // colCountDown
            // 
            this.colCountDown.HeaderText = "Count Down";
            this.colCountDown.Name = "colCountDown";
            this.colCountDown.ReadOnly = true;
            this.colCountDown.Width = 100;
            // 
            // colSystemName
            // 
            this.colSystemName.HeaderText = "System Name";
            this.colSystemName.Name = "colSystemName";
            this.colSystemName.ReadOnly = true;
            this.colSystemName.Width = 130;
            // 
            // colColonyName
            // 
            this.colColonyName.HeaderText = "Colony Name";
            this.colColonyName.Name = "colColonyName";
            this.colColonyName.ReadOnly = true;
            this.colColonyName.Width = 130;
            // 
            // colActivityType
            // 
            this.colActivityType.HeaderText = "Activity Type";
            this.colActivityType.Name = "colActivityType";
            this.colActivityType.ReadOnly = true;
            this.colActivityType.Width = 120;
            // 
            // colSource
            // 
            this.colSource.HeaderText = "Source";
            this.colSource.Name = "colSource";
            this.colSource.ReadOnly = true;
            this.colSource.Width = 200;
            // 
            // colProcessDetails
            // 
            this.colProcessDetails.HeaderText = "Process Details";
            this.colProcessDetails.Name = "colProcessDetails";
            this.colProcessDetails.ReadOnly = true;
            this.colProcessDetails.Width = 250;
            // 
            // colSecondsRemaining
            // 
            this.colSecondsRemaining.HeaderText = "SecondsRemaining";
            this.colSecondsRemaining.Name = "colSecondsRemaining";
            this.colSecondsRemaining.ReadOnly = true;
            this.colSecondsRemaining.Visible = false;
            // 
            // timerRefresh
            // 
            this.timerRefresh.Interval = 1000;
            // 
            // FormColonyActivity
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 600);
            this.Controls.Add(this.flpBase);
            this.Name = "FormColonyActivity";
            this.Text = "Colony Activity";
            this.flpBase.ResumeLayout(false);
            this.flpBase.PerformLayout();
            this.flpFilters.ResumeLayout(false);
            this.flpFilters.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvActivities)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flpBase;
        private System.Windows.Forms.FlowLayoutPanel flpFilters;
        private System.Windows.Forms.CheckBox chkBuilding;
        private System.Windows.Forms.CheckBox chkManufacturing;
        private System.Windows.Forms.CheckBox chkCommodityManufacturing;
        private System.Windows.Forms.CheckBox chkCommodityRequest;
        private System.Windows.Forms.CheckBox chkResearch;
        private System.Windows.Forms.CheckBox chkMining;
        private System.Windows.Forms.CheckBox chkRefining;
        private System.Windows.Forms.CheckBox chkShowInactive;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtFilter;
        private System.Windows.Forms.DataGridView dgvActivities;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCountDown;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSystemName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colColonyName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colActivityType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSource;
        private System.Windows.Forms.DataGridViewTextBoxColumn colProcessDetails;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSecondsRemaining;
        private System.Windows.Forms.Timer timerRefresh;
    }
}
