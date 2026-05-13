namespace OE2EmpireTracker.Forms
{
    partial class FormSystem
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
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.dgvSystems = new System.Windows.Forms.DataGridView();
            this.colId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colGridLocation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSpectralClass = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFactionName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pnlDetail = new System.Windows.Forms.Panel();
            this.lblId = new System.Windows.Forms.Label();
            this.lblIdValue = new System.Windows.Forms.Label();
            this.lblName = new System.Windows.Forms.Label();
            this.lblNameValue = new System.Windows.Forms.Label();
            this.lblX = new System.Windows.Forms.Label();
            this.lblXValue = new System.Windows.Forms.Label();
            this.lblY = new System.Windows.Forms.Label();
            this.lblYValue = new System.Windows.Forms.Label();
            this.lblQuadrant = new System.Windows.Forms.Label();
            this.lblQuadrantValue = new System.Windows.Forms.Label();
            this.lblSector = new System.Windows.Forms.Label();
            this.lblSectorValue = new System.Windows.Forms.Label();
            this.lblRegion = new System.Windows.Forms.Label();
            this.lblRegionValue = new System.Windows.Forms.Label();
            this.lblLocality = new System.Windows.Forms.Label();
            this.lblLocalityValue = new System.Windows.Forms.Label();
            this.lblSpectralClass = new System.Windows.Forms.Label();
            this.lblSpectralClassValue = new System.Windows.Forms.Label();
            this.lblFactionName = new System.Windows.Forms.Label();
            this.txtFactionName = new System.Windows.Forms.TextBox();
            this.lblFactionColor = new System.Windows.Forms.Label();
            this.txtFactionColor = new System.Windows.Forms.TextBox();
            this.chkHasOrbital = new System.Windows.Forms.CheckBox();
            this.chkHasSpaceport = new System.Windows.Forms.CheckBox();
            this.chkHasStarbase = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnReimport = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSystems)).BeginInit();
            this.pnlDetail.SuspendLayout();
            this.SuspendLayout();
            //
            // splitContainer
            //
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 0);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.SplitterDistance = 450;
            this.splitContainer.Size = new System.Drawing.Size(900, 550);
            this.splitContainer.TabIndex = 0;
            //
            // splitContainer.Panel1
            //
            this.splitContainer.Panel1.Controls.Add(this.dgvSystems);
            this.splitContainer.Panel1.Controls.Add(this.txtSearch);
            //
            // splitContainer.Panel2
            //
            this.splitContainer.Panel2.Controls.Add(this.pnlDetail);
            //
            // txtSearch
            //
            this.txtSearch.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtSearch.Location = new System.Drawing.Point(0, 0);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(450, 23);
            this.txtSearch.TabIndex = 0;
            //
            // dgvSystems
            //
            this.dgvSystems.AllowUserToAddRows = false;
            this.dgvSystems.AllowUserToDeleteRows = false;
            this.dgvSystems.AllowUserToOrderColumns = true;
            this.dgvSystems.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSystems.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colId,
                this.colName,
                this.colGridLocation,
                this.colSpectralClass,
                this.colFactionName});
            this.dgvSystems.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvSystems.Location = new System.Drawing.Point(0, 23);
            this.dgvSystems.MultiSelect = false;
            this.dgvSystems.Name = "dgvSystems";
            this.dgvSystems.ReadOnly = true;
            this.dgvSystems.RowHeadersVisible = false;
            this.dgvSystems.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvSystems.Size = new System.Drawing.Size(450, 527);
            this.dgvSystems.TabIndex = 1;
            //
            // colId
            //
            this.colId.HeaderText = "Id";
            this.colId.Name = "colId";
            this.colId.ReadOnly = true;
            this.colId.Width = 60;
            //
            // colName
            //
            this.colName.HeaderText = "Name";
            this.colName.Name = "colName";
            this.colName.ReadOnly = true;
            this.colName.Width = 150;
            //
            // colGridLocation
            //
            this.colGridLocation.HeaderText = "Grid";
            this.colGridLocation.Name = "colGridLocation";
            this.colGridLocation.ReadOnly = true;
            this.colGridLocation.Width = 100;
            //
            // colSpectralClass
            //
            this.colSpectralClass.HeaderText = "Spectral";
            this.colSpectralClass.Name = "colSpectralClass";
            this.colSpectralClass.ReadOnly = true;
            this.colSpectralClass.Width = 60;
            //
            // colFactionName
            //
            this.colFactionName.HeaderText = "Faction";
            this.colFactionName.Name = "colFactionName";
            this.colFactionName.ReadOnly = true;
            this.colFactionName.Width = 100;
            //
            // pnlDetail
            //
            this.pnlDetail.AutoScroll = true;
            this.pnlDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlDetail.Location = new System.Drawing.Point(0, 0);
            this.pnlDetail.Name = "pnlDetail";
            this.pnlDetail.Size = new System.Drawing.Size(446, 550);
            this.pnlDetail.TabIndex = 0;
            this.pnlDetail.Controls.Add(this.btnReimport);
            this.pnlDetail.Controls.Add(this.btnSave);
            this.pnlDetail.Controls.Add(this.chkHasStarbase);
            this.pnlDetail.Controls.Add(this.chkHasSpaceport);
            this.pnlDetail.Controls.Add(this.chkHasOrbital);
            this.pnlDetail.Controls.Add(this.txtFactionColor);
            this.pnlDetail.Controls.Add(this.lblFactionColor);
            this.pnlDetail.Controls.Add(this.txtFactionName);
            this.pnlDetail.Controls.Add(this.lblFactionName);
            this.pnlDetail.Controls.Add(this.lblSpectralClassValue);
            this.pnlDetail.Controls.Add(this.lblSpectralClass);
            this.pnlDetail.Controls.Add(this.lblLocalityValue);
            this.pnlDetail.Controls.Add(this.lblLocality);
            this.pnlDetail.Controls.Add(this.lblRegionValue);
            this.pnlDetail.Controls.Add(this.lblRegion);
            this.pnlDetail.Controls.Add(this.lblSectorValue);
            this.pnlDetail.Controls.Add(this.lblSector);
            this.pnlDetail.Controls.Add(this.lblQuadrantValue);
            this.pnlDetail.Controls.Add(this.lblQuadrant);
            this.pnlDetail.Controls.Add(this.lblYValue);
            this.pnlDetail.Controls.Add(this.lblY);
            this.pnlDetail.Controls.Add(this.lblXValue);
            this.pnlDetail.Controls.Add(this.lblX);
            this.pnlDetail.Controls.Add(this.lblNameValue);
            this.pnlDetail.Controls.Add(this.lblName);
            this.pnlDetail.Controls.Add(this.lblIdValue);
            this.pnlDetail.Controls.Add(this.lblId);
            //
            // lblId
            //
            this.lblId.AutoSize = true;
            this.lblId.Location = new System.Drawing.Point(10, 10);
            this.lblId.Name = "lblId";
            this.lblId.Size = new System.Drawing.Size(20, 15);
            this.lblId.TabIndex = 0;
            this.lblId.Text = "Id:";
            //
            // lblIdValue
            //
            this.lblIdValue.AutoSize = true;
            this.lblIdValue.Location = new System.Drawing.Point(120, 10);
            this.lblIdValue.Name = "lblIdValue";
            this.lblIdValue.Size = new System.Drawing.Size(0, 15);
            this.lblIdValue.TabIndex = 1;
            //
            // lblName
            //
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(10, 35);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(42, 15);
            this.lblName.TabIndex = 2;
            this.lblName.Text = "Name:";
            //
            // lblNameValue
            //
            this.lblNameValue.AutoSize = true;
            this.lblNameValue.Location = new System.Drawing.Point(120, 35);
            this.lblNameValue.Name = "lblNameValue";
            this.lblNameValue.Size = new System.Drawing.Size(0, 15);
            this.lblNameValue.TabIndex = 3;
            //
            // lblX
            //
            this.lblX.AutoSize = true;
            this.lblX.Location = new System.Drawing.Point(10, 60);
            this.lblX.Name = "lblX";
            this.lblX.Size = new System.Drawing.Size(17, 15);
            this.lblX.TabIndex = 4;
            this.lblX.Text = "X:";
            //
            // lblXValue
            //
            this.lblXValue.AutoSize = true;
            this.lblXValue.Location = new System.Drawing.Point(120, 60);
            this.lblXValue.Name = "lblXValue";
            this.lblXValue.Size = new System.Drawing.Size(0, 15);
            this.lblXValue.TabIndex = 5;
            //
            // lblY
            //
            this.lblY.AutoSize = true;
            this.lblY.Location = new System.Drawing.Point(10, 85);
            this.lblY.Name = "lblY";
            this.lblY.Size = new System.Drawing.Size(17, 15);
            this.lblY.TabIndex = 6;
            this.lblY.Text = "Y:";
            //
            // lblYValue
            //
            this.lblYValue.AutoSize = true;
            this.lblYValue.Location = new System.Drawing.Point(120, 85);
            this.lblYValue.Name = "lblYValue";
            this.lblYValue.Size = new System.Drawing.Size(0, 15);
            this.lblYValue.TabIndex = 7;
            //
            // lblQuadrant
            //
            this.lblQuadrant.AutoSize = true;
            this.lblQuadrant.Location = new System.Drawing.Point(10, 110);
            this.lblQuadrant.Name = "lblQuadrant";
            this.lblQuadrant.Size = new System.Drawing.Size(60, 15);
            this.lblQuadrant.TabIndex = 8;
            this.lblQuadrant.Text = "Quadrant:";
            //
            // lblQuadrantValue
            //
            this.lblQuadrantValue.AutoSize = true;
            this.lblQuadrantValue.Location = new System.Drawing.Point(120, 110);
            this.lblQuadrantValue.Name = "lblQuadrantValue";
            this.lblQuadrantValue.Size = new System.Drawing.Size(0, 15);
            this.lblQuadrantValue.TabIndex = 9;
            //
            // lblSector
            //
            this.lblSector.AutoSize = true;
            this.lblSector.Location = new System.Drawing.Point(10, 135);
            this.lblSector.Name = "lblSector";
            this.lblSector.Size = new System.Drawing.Size(44, 15);
            this.lblSector.TabIndex = 10;
            this.lblSector.Text = "Sector:";
            //
            // lblSectorValue
            //
            this.lblSectorValue.AutoSize = true;
            this.lblSectorValue.Location = new System.Drawing.Point(120, 135);
            this.lblSectorValue.Name = "lblSectorValue";
            this.lblSectorValue.Size = new System.Drawing.Size(0, 15);
            this.lblSectorValue.TabIndex = 11;
            //
            // lblRegion
            //
            this.lblRegion.AutoSize = true;
            this.lblRegion.Location = new System.Drawing.Point(10, 160);
            this.lblRegion.Name = "lblRegion";
            this.lblRegion.Size = new System.Drawing.Size(47, 15);
            this.lblRegion.TabIndex = 12;
            this.lblRegion.Text = "Region:";
            //
            // lblRegionValue
            //
            this.lblRegionValue.AutoSize = true;
            this.lblRegionValue.Location = new System.Drawing.Point(120, 160);
            this.lblRegionValue.Name = "lblRegionValue";
            this.lblRegionValue.Size = new System.Drawing.Size(0, 15);
            this.lblRegionValue.TabIndex = 13;
            //
            // lblLocality
            //
            this.lblLocality.AutoSize = true;
            this.lblLocality.Location = new System.Drawing.Point(10, 185);
            this.lblLocality.Name = "lblLocality";
            this.lblLocality.Size = new System.Drawing.Size(52, 15);
            this.lblLocality.TabIndex = 14;
            this.lblLocality.Text = "Locality:";
            //
            // lblLocalityValue
            //
            this.lblLocalityValue.AutoSize = true;
            this.lblLocalityValue.Location = new System.Drawing.Point(120, 185);
            this.lblLocalityValue.Name = "lblLocalityValue";
            this.lblLocalityValue.Size = new System.Drawing.Size(0, 15);
            this.lblLocalityValue.TabIndex = 15;
            //
            // lblSpectralClass
            //
            this.lblSpectralClass.AutoSize = true;
            this.lblSpectralClass.Location = new System.Drawing.Point(10, 210);
            this.lblSpectralClass.Name = "lblSpectralClass";
            this.lblSpectralClass.Size = new System.Drawing.Size(82, 15);
            this.lblSpectralClass.TabIndex = 16;
            this.lblSpectralClass.Text = "Spectral Class:";
            //
            // lblSpectralClassValue
            //
            this.lblSpectralClassValue.AutoSize = true;
            this.lblSpectralClassValue.Location = new System.Drawing.Point(120, 210);
            this.lblSpectralClassValue.Name = "lblSpectralClassValue";
            this.lblSpectralClassValue.Size = new System.Drawing.Size(0, 15);
            this.lblSpectralClassValue.TabIndex = 17;
            //
            // lblFactionName
            //
            this.lblFactionName.AutoSize = true;
            this.lblFactionName.Location = new System.Drawing.Point(10, 245);
            this.lblFactionName.Name = "lblFactionName";
            this.lblFactionName.Size = new System.Drawing.Size(82, 15);
            this.lblFactionName.TabIndex = 18;
            this.lblFactionName.Text = "Faction Name:";
            //
            // txtFactionName
            //
            this.txtFactionName.Location = new System.Drawing.Point(120, 242);
            this.txtFactionName.Name = "txtFactionName";
            this.txtFactionName.Size = new System.Drawing.Size(200, 23);
            this.txtFactionName.TabIndex = 19;
            //
            // lblFactionColor
            //
            this.lblFactionColor.AutoSize = true;
            this.lblFactionColor.Location = new System.Drawing.Point(10, 275);
            this.lblFactionColor.Name = "lblFactionColor";
            this.lblFactionColor.Size = new System.Drawing.Size(80, 15);
            this.lblFactionColor.TabIndex = 20;
            this.lblFactionColor.Text = "Faction Color:";
            //
            // txtFactionColor
            //
            this.txtFactionColor.Location = new System.Drawing.Point(120, 272);
            this.txtFactionColor.Name = "txtFactionColor";
            this.txtFactionColor.Size = new System.Drawing.Size(200, 23);
            this.txtFactionColor.TabIndex = 21;
            //
            // chkHasOrbital
            //
            this.chkHasOrbital.AutoSize = true;
            this.chkHasOrbital.Location = new System.Drawing.Point(10, 310);
            this.chkHasOrbital.Name = "chkHasOrbital";
            this.chkHasOrbital.Size = new System.Drawing.Size(88, 19);
            this.chkHasOrbital.TabIndex = 22;
            this.chkHasOrbital.Text = "Has Orbital";
            this.chkHasOrbital.UseVisualStyleBackColor = true;
            //
            // chkHasSpaceport
            //
            this.chkHasSpaceport.AutoSize = true;
            this.chkHasSpaceport.Location = new System.Drawing.Point(10, 335);
            this.chkHasSpaceport.Name = "chkHasSpaceport";
            this.chkHasSpaceport.Size = new System.Drawing.Size(104, 19);
            this.chkHasSpaceport.TabIndex = 23;
            this.chkHasSpaceport.Text = "Has Spaceport";
            this.chkHasSpaceport.UseVisualStyleBackColor = true;
            //
            // chkHasStarbase
            //
            this.chkHasStarbase.AutoSize = true;
            this.chkHasStarbase.Location = new System.Drawing.Point(10, 360);
            this.chkHasStarbase.Name = "chkHasStarbase";
            this.chkHasStarbase.Size = new System.Drawing.Size(96, 19);
            this.chkHasStarbase.TabIndex = 24;
            this.chkHasStarbase.Text = "Has Starbase";
            this.chkHasStarbase.UseVisualStyleBackColor = true;
            //
            // btnSave
            //
            this.btnSave.Location = new System.Drawing.Point(10, 395);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(90, 27);
            this.btnSave.TabIndex = 25;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            //
            // btnReimport
            //
            this.btnReimport.Location = new System.Drawing.Point(110, 395);
            this.btnReimport.Name = "btnReimport";
            this.btnReimport.Size = new System.Drawing.Size(90, 27);
            this.btnReimport.TabIndex = 26;
            this.btnReimport.Text = "Re-import";
            this.btnReimport.UseVisualStyleBackColor = true;
            //
            // FormSystem
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 550);
            this.Controls.Add(this.splitContainer);
            this.Name = "FormSystem";
            this.Text = "Systems";
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel1.PerformLayout();
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvSystems)).EndInit();
            this.pnlDetail.ResumeLayout(false);
            this.pnlDetail.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.DataGridView dgvSystems;
        private System.Windows.Forms.DataGridViewTextBoxColumn colId;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colGridLocation;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSpectralClass;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFactionName;
        private System.Windows.Forms.Panel pnlDetail;
        private System.Windows.Forms.Label lblId;
        private System.Windows.Forms.Label lblIdValue;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label lblNameValue;
        private System.Windows.Forms.Label lblX;
        private System.Windows.Forms.Label lblXValue;
        private System.Windows.Forms.Label lblY;
        private System.Windows.Forms.Label lblYValue;
        private System.Windows.Forms.Label lblQuadrant;
        private System.Windows.Forms.Label lblQuadrantValue;
        private System.Windows.Forms.Label lblSector;
        private System.Windows.Forms.Label lblSectorValue;
        private System.Windows.Forms.Label lblRegion;
        private System.Windows.Forms.Label lblRegionValue;
        private System.Windows.Forms.Label lblLocality;
        private System.Windows.Forms.Label lblLocalityValue;
        private System.Windows.Forms.Label lblSpectralClass;
        private System.Windows.Forms.Label lblSpectralClassValue;
        private System.Windows.Forms.Label lblFactionName;
        private System.Windows.Forms.TextBox txtFactionName;
        private System.Windows.Forms.Label lblFactionColor;
        private System.Windows.Forms.TextBox txtFactionColor;
        private System.Windows.Forms.CheckBox chkHasOrbital;
        private System.Windows.Forms.CheckBox chkHasSpaceport;
        private System.Windows.Forms.CheckBox chkHasStarbase;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnReimport;
    }
}
