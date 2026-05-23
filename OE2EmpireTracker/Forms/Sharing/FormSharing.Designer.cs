namespace OE2EmpireTracker.Forms.Sharing
{
    partial class FormSharing
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
            this.dgvRules = new System.Windows.Forms.DataGridView();
            this.colTargetType = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.colTargetUUID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDataType = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.pnlButtons = new System.Windows.Forms.Panel();
            this.btnAddRule = new System.Windows.Forms.Button();
            this.btnDeleteSelected = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnReload = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRules)).BeginInit();
            this.pnlButtons.SuspendLayout();
            this.SuspendLayout();

            //
            // dgvRules
            //
            this.dgvRules.AllowUserToAddRows = false;
            this.dgvRules.AllowUserToDeleteRows = false;
            this.dgvRules.AllowUserToOrderColumns = true;
            this.dgvRules.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvRules.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[]
            {
                this.colTargetType,
                this.colTargetUUID,
                this.colDataType,
            });
            this.dgvRules.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvRules.Location = new System.Drawing.Point(0, 0);
            this.dgvRules.Name = "dgvRules";
            this.dgvRules.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvRules.Size = new System.Drawing.Size(684, 411);
            this.dgvRules.TabIndex = 0;
            //
            // colTargetType
            //
            this.colTargetType.HeaderText = "Target Type";
            this.colTargetType.Items.AddRange(new object[]
            {
                "Faction",
                "Character",
                "Public",
            });
            this.colTargetType.Name = "colTargetType";
            this.colTargetType.Width = 120;

            //
            // colTargetUUID
            //
            this.colTargetUUID.HeaderText = "Target UUID";
            this.colTargetUUID.Name = "colTargetUUID";
            this.colTargetUUID.Width = 250;
            //
            // colDataType
            //
            this.colDataType.HeaderText = "Data Type";
            this.colDataType.Items.AddRange(new object[]
            {
                "All",
                "Colonies",
                "Blueprints",
                "Surveys",
            });
            this.colDataType.Name = "colDataType";
            this.colDataType.Width = 120;
            //
            // pnlButtons
            //
            this.pnlButtons.Controls.Add(this.btnAddRule);
            this.pnlButtons.Controls.Add(this.btnDeleteSelected);
            this.pnlButtons.Controls.Add(this.btnSave);
            this.pnlButtons.Controls.Add(this.btnReload);
            this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlButtons.Location = new System.Drawing.Point(0, 411);
            this.pnlButtons.Name = "pnlButtons";
            this.pnlButtons.Size = new System.Drawing.Size(684, 40);
            this.pnlButtons.TabIndex = 1;

            //
            // btnAddRule
            //
            this.btnAddRule.Location = new System.Drawing.Point(8, 8);
            this.btnAddRule.Name = "btnAddRule";
            this.btnAddRule.Size = new System.Drawing.Size(80, 25);
            this.btnAddRule.TabIndex = 0;
            this.btnAddRule.Text = "Add Rule";
            this.btnAddRule.UseVisualStyleBackColor = true;
            //
            // btnDeleteSelected
            //
            this.btnDeleteSelected.Location = new System.Drawing.Point(94, 8);
            this.btnDeleteSelected.Name = "btnDeleteSelected";
            this.btnDeleteSelected.Size = new System.Drawing.Size(100, 25);
            this.btnDeleteSelected.TabIndex = 1;
            this.btnDeleteSelected.Text = "Delete Selected";
            this.btnDeleteSelected.UseVisualStyleBackColor = true;
            //
            // btnSave
            //
            this.btnSave.Location = new System.Drawing.Point(200, 8);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(80, 25);
            this.btnSave.TabIndex = 2;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            //
            // btnReload
            //
            this.btnReload.Location = new System.Drawing.Point(286, 8);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(80, 25);
            this.btnReload.TabIndex = 3;
            this.btnReload.Text = "Reload";
            this.btnReload.UseVisualStyleBackColor = true;

            //
            // FormSharing
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(684, 451);
            this.Controls.Add(this.dgvRules);
            this.Controls.Add(this.pnlButtons);
            this.Name = "FormSharing";
            this.Text = "Sharing Rules";
            ((System.ComponentModel.ISupportInitialize)(this.dgvRules)).EndInit();
            this.pnlButtons.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.DataGridView dgvRules;
        private System.Windows.Forms.DataGridViewComboBoxColumn colTargetType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTargetUUID;
        private System.Windows.Forms.DataGridViewComboBoxColumn colDataType;
        private System.Windows.Forms.Panel pnlButtons;
        private System.Windows.Forms.Button btnAddRule;
        private System.Windows.Forms.Button btnDeleteSelected;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnReload;
    }
}
