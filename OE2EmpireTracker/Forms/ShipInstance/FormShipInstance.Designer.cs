namespace OE2EmpireTracker.Forms.ShipInstance
{
    partial class FormShipInstance
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
            this.flpFilter = new System.Windows.Forms.FlowLayoutPanel();
            this.lblFilter = new System.Windows.Forms.Label();
            this.txtFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lvwShips = new System.Windows.Forms.ListView();
            this.flpCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdNew = new System.Windows.Forms.Button();
            this.cmdDelete = new System.Windows.Forms.Button();
            this.cmdFromTemplate = new System.Windows.Forms.Button();
            this.flpDetail = new System.Windows.Forms.FlowLayoutPanel();
            this.flpName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpLocation = new System.Windows.Forms.FlowLayoutPanel();
            this.lblLocation = new System.Windows.Forms.Label();
            this.cmbLocationType = new System.Windows.Forms.ComboBox();
            this.cmbLocationUUID = new System.Windows.Forms.ComboBox();
            this.cmdSave = new System.Windows.Forms.Button();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabOverview = new System.Windows.Forms.TabPage();
            this.dgvComponents = new System.Windows.Forms.DataGridView();
            this.colSlotType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colComponentName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCondition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMaxRepair = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabCargo = new System.Windows.Forms.TabPage();
            this.flpCargoTop = new System.Windows.Forms.FlowLayoutPanel();
            this.rbCargoHold = new System.Windows.Forms.RadioButton();
            this.rbHopper = new System.Windows.Forms.RadioButton();
            this.dgvCargo = new System.Windows.Forms.DataGridView();
            this.colCargoType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCargoName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCargoPurity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCargoQty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpAddItem = new System.Windows.Forms.FlowLayoutPanel();
            this.lblAddType = new System.Windows.Forms.Label();
            this.cmbAddType = new System.Windows.Forms.ComboBox();
            this.lblAddItem = new System.Windows.Forms.Label();
            this.cmbAddItem = new System.Windows.Forms.ComboBox();
            this.lblAddPurity = new System.Windows.Forms.Label();
            this.cmbAddPurity = new System.Windows.Forms.ComboBox();
            this.lblAddQty = new System.Windows.Forms.Label();
            this.txtAddQty = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmdAddItem = new System.Windows.Forms.Button();
            this.cmdRemoveItem = new System.Windows.Forms.Button();

            this.flpBase.SuspendLayout();
            this.flpSearchList.SuspendLayout();
            this.flpFilter.SuspendLayout();
            this.flpCommands.SuspendLayout();
            this.flpDetail.SuspendLayout();
            this.flpName.SuspendLayout();
            this.flpLocation.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.tabOverview.SuspendLayout();
            this.tabCargo.SuspendLayout();
            this.flpCargoTop.SuspendLayout();
            this.flpAddItem.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvComponents)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCargo)).BeginInit();
            this.SuspendLayout();
            //
            // flpBase
            //
            this.flpBase.Controls.Add(this.flpSearchList);
            this.flpBase.Controls.Add(this.flpDetail);
            this.flpBase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpBase.Location = new System.Drawing.Point(0, 0);
            this.flpBase.Name = "flpBase";
            this.flpBase.Size = new System.Drawing.Size(950, 620);
            this.flpBase.WrapContents = false;
            //
            // flpSearchList
            //
            this.flpSearchList.Controls.Add(this.flpFilter);
            this.flpSearchList.Controls.Add(this.lvwShips);
            this.flpSearchList.Controls.Add(this.flpCommands);
            this.flpSearchList.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpSearchList.Location = new System.Drawing.Point(3, 3);
            this.flpSearchList.Name = "flpSearchList";
            this.flpSearchList.Size = new System.Drawing.Size(220, 614);
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
            // lvwShips
            //
            this.lvwShips.FullRowSelect = true;
            this.lvwShips.HideSelection = false;
            this.lvwShips.Location = new System.Drawing.Point(3, 35);
            this.lvwShips.MultiSelect = false;
            this.lvwShips.Name = "lvwShips";
            this.lvwShips.Size = new System.Drawing.Size(214, 520);
            this.lvwShips.UseCompatibleStateImageBehavior = false;
            this.lvwShips.View = System.Windows.Forms.View.Details;
            //
            // flpCommands
            //
            this.flpCommands.AutoSize = true;
            this.flpCommands.Controls.Add(this.cmdNew);
            this.flpCommands.Controls.Add(this.cmdDelete);
            this.flpCommands.Controls.Add(this.cmdFromTemplate);
            this.flpCommands.Location = new System.Drawing.Point(3, 561);
            this.flpCommands.Name = "flpCommands";
            this.flpCommands.Size = new System.Drawing.Size(214, 29);
            //
            // cmdNew
            //
            this.cmdNew.Location = new System.Drawing.Point(3, 3);
            this.cmdNew.Name = "cmdNew";
            this.cmdNew.Size = new System.Drawing.Size(55, 23);
            this.cmdNew.Text = "New";
            this.cmdNew.UseVisualStyleBackColor = true;
            //
            // cmdDelete
            //
            this.cmdDelete.Location = new System.Drawing.Point(64, 3);
            this.cmdDelete.Name = "cmdDelete";
            this.cmdDelete.Size = new System.Drawing.Size(55, 23);
            this.cmdDelete.Text = "Delete";
            this.cmdDelete.UseVisualStyleBackColor = true;
            //
            // cmdFromTemplate
            //
            this.cmdFromTemplate.Location = new System.Drawing.Point(125, 3);
            this.cmdFromTemplate.Name = "cmdFromTemplate";
            this.cmdFromTemplate.Size = new System.Drawing.Size(85, 23);
            this.cmdFromTemplate.Text = "From Template";
            this.cmdFromTemplate.UseVisualStyleBackColor = true;
            //
            // flpDetail
            //
            this.flpDetail.Controls.Add(this.flpName);
            this.flpDetail.Controls.Add(this.flpLocation);
            this.flpDetail.Controls.Add(this.cmdSave);
            this.flpDetail.Controls.Add(this.tabControl);
            this.flpDetail.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpDetail.Location = new System.Drawing.Point(229, 3);
            this.flpDetail.Name = "flpDetail";
            this.flpDetail.Size = new System.Drawing.Size(718, 614);
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
            this.flpName.Size = new System.Drawing.Size(712, 26);
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
            // flpLocation
            //
            this.flpLocation.AutoSize = true;
            this.flpLocation.Controls.Add(this.lblLocation);
            this.flpLocation.Controls.Add(this.cmbLocationType);
            this.flpLocation.Controls.Add(this.cmbLocationUUID);
            this.flpLocation.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpLocation.Location = new System.Drawing.Point(3, 35);
            this.flpLocation.Name = "flpLocation";
            this.flpLocation.Size = new System.Drawing.Size(712, 27);
            //
            // lblLocation
            //
            this.lblLocation.AutoSize = true;
            this.lblLocation.Location = new System.Drawing.Point(3, 5);
            this.lblLocation.Name = "lblLocation";
            this.lblLocation.Size = new System.Drawing.Size(51, 13);
            this.lblLocation.Text = "Location:";
            this.lblLocation.Anchor = System.Windows.Forms.AnchorStyles.Left;
            //
            // cmbLocationType
            //
            this.cmbLocationType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLocationType.Location = new System.Drawing.Point(60, 3);
            this.cmbLocationType.Name = "cmbLocationType";
            this.cmbLocationType.Size = new System.Drawing.Size(120, 21);
            //
            // cmbLocationUUID
            //
            this.cmbLocationUUID.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLocationUUID.Location = new System.Drawing.Point(186, 3);
            this.cmbLocationUUID.Name = "cmbLocationUUID";
            this.cmbLocationUUID.Size = new System.Drawing.Size(250, 21);
            //
            // cmdSave
            //
            this.cmdSave.Location = new System.Drawing.Point(3, 68);
            this.cmdSave.Name = "cmdSave";
            this.cmdSave.Size = new System.Drawing.Size(75, 23);
            this.cmdSave.Text = "Save";
            this.cmdSave.UseVisualStyleBackColor = true;
            //
            // tabControl
            //
            this.tabControl.Controls.Add(this.tabOverview);
            this.tabControl.Controls.Add(this.tabCargo);
            this.tabControl.Location = new System.Drawing.Point(3, 97);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(712, 510);
            //
            // tabOverview
            //
            this.tabOverview.Controls.Add(this.dgvComponents);
            this.tabOverview.Location = new System.Drawing.Point(4, 22);
            this.tabOverview.Name = "tabOverview";
            this.tabOverview.Padding = new System.Windows.Forms.Padding(3);
            this.tabOverview.Size = new System.Drawing.Size(704, 484);
            this.tabOverview.TabIndex = 0;
            this.tabOverview.Text = "Overview";
            this.tabOverview.UseVisualStyleBackColor = true;
            //
            // dgvComponents
            //
            this.dgvComponents.AllowUserToAddRows = false;
            this.dgvComponents.AllowUserToDeleteRows = false;
            this.dgvComponents.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvComponents.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colSlotType, this.colComponentName, this.colCondition, this.colMaxRepair});
            this.dgvComponents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvComponents.Location = new System.Drawing.Point(3, 3);
            this.dgvComponents.Name = "dgvComponents";
            this.dgvComponents.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvComponents.Size = new System.Drawing.Size(698, 478);
            //
            // colSlotType
            //
            this.colSlotType.HeaderText = "Slot";
            this.colSlotType.Name = "colSlotType";
            this.colSlotType.ReadOnly = true;
            this.colSlotType.Width = 120;
            //
            // colComponentName
            //
            this.colComponentName.HeaderText = "Component";
            this.colComponentName.Name = "colComponentName";
            this.colComponentName.ReadOnly = true;
            this.colComponentName.Width = 250;
            //
            // colCondition
            //
            this.colCondition.HeaderText = "Condition %";
            this.colCondition.Name = "colCondition";
            this.colCondition.Width = 90;
            //
            // colMaxRepair
            //
            this.colMaxRepair.HeaderText = "Max Repair %";
            this.colMaxRepair.Name = "colMaxRepair";
            this.colMaxRepair.Width = 90;
            //
            // tabCargo
            //
            this.tabCargo.Controls.Add(this.flpCargoTop);
            this.tabCargo.Controls.Add(this.dgvCargo);
            this.tabCargo.Controls.Add(this.flpAddItem);
            this.tabCargo.Controls.Add(this.cmdRemoveItem);
            this.tabCargo.Location = new System.Drawing.Point(4, 22);
            this.tabCargo.Name = "tabCargo";
            this.tabCargo.Padding = new System.Windows.Forms.Padding(3);
            this.tabCargo.Size = new System.Drawing.Size(704, 484);
            this.tabCargo.TabIndex = 1;
            this.tabCargo.Text = "Cargo";
            this.tabCargo.UseVisualStyleBackColor = true;
            //
            // flpCargoTop
            //
            this.flpCargoTop.AutoSize = true;
            this.flpCargoTop.Controls.Add(this.rbCargoHold);
            this.flpCargoTop.Controls.Add(this.rbHopper);
            this.flpCargoTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.flpCargoTop.Location = new System.Drawing.Point(3, 3);
            this.flpCargoTop.Name = "flpCargoTop";
            this.flpCargoTop.Size = new System.Drawing.Size(698, 26);
            //
            // rbCargoHold
            //
            this.rbCargoHold.AutoSize = true;
            this.rbCargoHold.Checked = true;
            this.rbCargoHold.Location = new System.Drawing.Point(3, 3);
            this.rbCargoHold.Name = "rbCargoHold";
            this.rbCargoHold.Size = new System.Drawing.Size(78, 17);
            this.rbCargoHold.TabStop = true;
            this.rbCargoHold.Text = "Cargo Hold";
            this.rbCargoHold.UseVisualStyleBackColor = true;
            //
            // rbHopper
            //
            this.rbHopper.AutoSize = true;
            this.rbHopper.Location = new System.Drawing.Point(87, 3);
            this.rbHopper.Name = "rbHopper";
            this.rbHopper.Size = new System.Drawing.Size(61, 17);
            this.rbHopper.Text = "Hopper";
            this.rbHopper.UseVisualStyleBackColor = true;
            //
            // dgvCargo
            //
            this.dgvCargo.AllowUserToAddRows = false;
            this.dgvCargo.AllowUserToDeleteRows = false;
            this.dgvCargo.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvCargo.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colCargoType, this.colCargoName, this.colCargoPurity, this.colCargoQty});
            this.dgvCargo.Location = new System.Drawing.Point(3, 32);
            this.dgvCargo.Name = "dgvCargo";
            this.dgvCargo.ReadOnly = true;
            this.dgvCargo.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvCargo.Size = new System.Drawing.Size(698, 350);
            //
            // colCargoType
            //
            this.colCargoType.HeaderText = "Type";
            this.colCargoType.Name = "colCargoType";
            this.colCargoType.ReadOnly = true;
            this.colCargoType.Width = 100;
            //
            // colCargoName
            //
            this.colCargoName.HeaderText = "Name";
            this.colCargoName.Name = "colCargoName";
            this.colCargoName.ReadOnly = true;
            this.colCargoName.Width = 250;
            //
            // colCargoPurity
            //
            this.colCargoPurity.HeaderText = "Purity";
            this.colCargoPurity.Name = "colCargoPurity";
            this.colCargoPurity.ReadOnly = true;
            this.colCargoPurity.Width = 80;
            //
            // colCargoQty
            //
            this.colCargoQty.HeaderText = "Quantity";
            this.colCargoQty.Name = "colCargoQty";
            this.colCargoQty.ReadOnly = true;
            this.colCargoQty.Width = 80;
            //
            // flpAddItem
            //
            this.flpAddItem.AutoSize = true;
            this.flpAddItem.Controls.Add(this.lblAddType);
            this.flpAddItem.Controls.Add(this.cmbAddType);
            this.flpAddItem.Controls.Add(this.lblAddItem);
            this.flpAddItem.Controls.Add(this.cmbAddItem);
            this.flpAddItem.Controls.Add(this.lblAddPurity);
            this.flpAddItem.Controls.Add(this.cmbAddPurity);
            this.flpAddItem.Controls.Add(this.lblAddQty);
            this.flpAddItem.Controls.Add(this.txtAddQty);
            this.flpAddItem.Controls.Add(this.cmdAddItem);
            this.flpAddItem.Location = new System.Drawing.Point(3, 388);
            this.flpAddItem.Name = "flpAddItem";
            this.flpAddItem.Size = new System.Drawing.Size(698, 30);
            //
            // lblAddType
            //
            this.lblAddType.AutoSize = true;
            this.lblAddType.Location = new System.Drawing.Point(3, 7);
            this.lblAddType.Name = "lblAddType";
            this.lblAddType.Size = new System.Drawing.Size(34, 13);
            this.lblAddType.Text = "Type:";
            this.lblAddType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            //
            // cmbAddType
            //
            this.cmbAddType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAddType.Location = new System.Drawing.Point(43, 3);
            this.cmbAddType.Name = "cmbAddType";
            this.cmbAddType.Size = new System.Drawing.Size(100, 21);
            //
            // lblAddItem
            //
            this.lblAddItem.AutoSize = true;
            this.lblAddItem.Location = new System.Drawing.Point(149, 7);
            this.lblAddItem.Name = "lblAddItem";
            this.lblAddItem.Size = new System.Drawing.Size(30, 13);
            this.lblAddItem.Text = "Item:";
            this.lblAddItem.Anchor = System.Windows.Forms.AnchorStyles.Left;
            //
            // cmbAddItem
            //
            this.cmbAddItem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAddItem.Location = new System.Drawing.Point(185, 3);
            this.cmbAddItem.Name = "cmbAddItem";
            this.cmbAddItem.Size = new System.Drawing.Size(150, 21);
            //
            // lblAddPurity
            //
            this.lblAddPurity.AutoSize = true;
            this.lblAddPurity.Location = new System.Drawing.Point(341, 7);
            this.lblAddPurity.Name = "lblAddPurity";
            this.lblAddPurity.Size = new System.Drawing.Size(37, 13);
            this.lblAddPurity.Text = "Purity:";
            this.lblAddPurity.Anchor = System.Windows.Forms.AnchorStyles.Left;
            //
            // cmbAddPurity
            //
            this.cmbAddPurity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAddPurity.Location = new System.Drawing.Point(384, 3);
            this.cmbAddPurity.Name = "cmbAddPurity";
            this.cmbAddPurity.Size = new System.Drawing.Size(80, 21);
            //
            // lblAddQty
            //
            this.lblAddQty.AutoSize = true;
            this.lblAddQty.Location = new System.Drawing.Point(470, 7);
            this.lblAddQty.Name = "lblAddQty";
            this.lblAddQty.Size = new System.Drawing.Size(26, 13);
            this.lblAddQty.Text = "Qty:";
            this.lblAddQty.Anchor = System.Windows.Forms.AnchorStyles.Left;
            //
            // txtAddQty
            //
            this.txtAddQty.Location = new System.Drawing.Point(502, 3);
            this.txtAddQty.Name = "txtAddQty";
            this.txtAddQty.Size = new System.Drawing.Size(60, 20);
            //
            // cmdAddItem
            //
            this.cmdAddItem.Location = new System.Drawing.Point(568, 3);
            this.cmdAddItem.Name = "cmdAddItem";
            this.cmdAddItem.Size = new System.Drawing.Size(50, 23);
            this.cmdAddItem.Text = "Add";
            this.cmdAddItem.UseVisualStyleBackColor = true;
            //
            // cmdRemoveItem
            //
            this.cmdRemoveItem.Location = new System.Drawing.Point(3, 424);
            this.cmdRemoveItem.Name = "cmdRemoveItem";
            this.cmdRemoveItem.Size = new System.Drawing.Size(75, 23);
            this.cmdRemoveItem.Text = "Remove";
            this.cmdRemoveItem.UseVisualStyleBackColor = true;
            //
            // FormShipInstance
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(950, 620);
            this.Controls.Add(this.flpBase);
            this.Name = "FormShipInstance";
            this.Text = "Ships";
            this.flpBase.ResumeLayout(false);
            this.flpBase.PerformLayout();
            this.flpSearchList.ResumeLayout(false);
            this.flpSearchList.PerformLayout();
            this.flpFilter.ResumeLayout(false);
            this.flpFilter.PerformLayout();
            this.flpCommands.ResumeLayout(false);
            this.flpCommands.PerformLayout();
            this.flpDetail.ResumeLayout(false);
            this.flpDetail.PerformLayout();
            this.flpName.ResumeLayout(false);
            this.flpName.PerformLayout();
            this.flpLocation.ResumeLayout(false);
            this.flpLocation.PerformLayout();
            this.flpCargoTop.ResumeLayout(false);
            this.flpCargoTop.PerformLayout();
            this.flpAddItem.ResumeLayout(false);
            this.flpAddItem.PerformLayout();
            this.tabOverview.ResumeLayout(false);
            this.tabCargo.ResumeLayout(false);
            this.tabCargo.PerformLayout();
            this.tabControl.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvComponents)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCargo)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flpBase;
        private System.Windows.Forms.FlowLayoutPanel flpSearchList;
        private System.Windows.Forms.FlowLayoutPanel flpFilter;
        private System.Windows.Forms.Label lblFilter;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtFilter;
        private System.Windows.Forms.ListView lvwShips;
        private System.Windows.Forms.FlowLayoutPanel flpCommands;
        private System.Windows.Forms.Button cmdNew;
        private System.Windows.Forms.Button cmdDelete;
        private System.Windows.Forms.Button cmdFromTemplate;
        private System.Windows.Forms.FlowLayoutPanel flpDetail;
        private System.Windows.Forms.FlowLayoutPanel flpName;
        private System.Windows.Forms.Label lblName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtName;
        private System.Windows.Forms.FlowLayoutPanel flpLocation;
        private System.Windows.Forms.Label lblLocation;
        private System.Windows.Forms.ComboBox cmbLocationType;
        private System.Windows.Forms.ComboBox cmbLocationUUID;
        private System.Windows.Forms.Button cmdSave;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabOverview;
        private System.Windows.Forms.DataGridView dgvComponents;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSlotType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colComponentName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCondition;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMaxRepair;
        private System.Windows.Forms.TabPage tabCargo;
        private System.Windows.Forms.FlowLayoutPanel flpCargoTop;
        private System.Windows.Forms.RadioButton rbCargoHold;
        private System.Windows.Forms.RadioButton rbHopper;
        private System.Windows.Forms.DataGridView dgvCargo;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCargoType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCargoName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCargoPurity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCargoQty;
        private System.Windows.Forms.FlowLayoutPanel flpAddItem;
        private System.Windows.Forms.Label lblAddType;
        private System.Windows.Forms.ComboBox cmbAddType;
        private System.Windows.Forms.Label lblAddItem;
        private System.Windows.Forms.ComboBox cmbAddItem;
        private System.Windows.Forms.Label lblAddPurity;
        private System.Windows.Forms.ComboBox cmbAddPurity;
        private System.Windows.Forms.Label lblAddQty;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtAddQty;
        private System.Windows.Forms.Button cmdAddItem;
        private System.Windows.Forms.Button cmdRemoveItem;
    }
}