namespace OE2EmpireTracker.Forms.Station
{
    partial class FormStation
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
            this.lvwStations = new System.Windows.Forms.ListView();
            this.flpCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdNew = new System.Windows.Forms.Button();
            this.cmdDelete = new System.Windows.Forms.Button();
            this.flpDetail = new System.Windows.Forms.FlowLayoutPanel();
            this.flpName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpStationType = new System.Windows.Forms.FlowLayoutPanel();
            this.lblStationType = new System.Windows.Forms.Label();
            this.cmbStationType = new System.Windows.Forms.ComboBox();
            this.lblOwnership = new System.Windows.Forms.Label();
            this.cmbOwnership = new System.Windows.Forms.ComboBox();
            this.cmdSave = new System.Windows.Forms.Button();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabHold = new System.Windows.Forms.TabPage();
            this.dgvHold = new System.Windows.Forms.DataGridView();
            this.colHoldType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHoldName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHoldPurity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHoldQty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHoldCondition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHoldMaxRepair = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpHoldAdd = new System.Windows.Forms.FlowLayoutPanel();
            this.lblHoldType = new System.Windows.Forms.Label();
            this.cmbHoldType = new System.Windows.Forms.ComboBox();
            this.lblHoldItem = new System.Windows.Forms.Label();
            this.cmbHoldItem = new System.Windows.Forms.ComboBox();
            this.lblHoldPurity = new System.Windows.Forms.Label();
            this.cmbHoldPurity = new System.Windows.Forms.ComboBox();
            this.lblHoldQty = new System.Windows.Forms.Label();
            this.txtHoldQty = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmdHoldAdd = new System.Windows.Forms.Button();
            this.cmdHoldRemove = new System.Windows.Forms.Button();
            this.lblHoldCrateContents = new System.Windows.Forms.Label();
            this.dgvHoldCrateContents = new System.Windows.Forms.DataGridView();
            this.colHoldCrateType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHoldCrateName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHoldCratePurity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHoldCrateQty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabComponents = new System.Windows.Forms.TabPage();
            this.flpBlueprintRow = new System.Windows.Forms.FlowLayoutPanel();
            this.lblBlueprint = new System.Windows.Forms.Label();
            this.cmbStationBlueprint = new System.Windows.Forms.ComboBox();
            this.dgvComponents = new System.Windows.Forms.DataGridView();
            this.rtbStationStats = new System.Windows.Forms.RichTextBox();
            this.colSlotType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colComponentName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCondition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMaxRepair = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabMunitions = new System.Windows.Forms.TabPage();
            this.dgvMunitions = new System.Windows.Forms.DataGridView();
            this.colMunName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMunQty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpMunAdd = new System.Windows.Forms.FlowLayoutPanel();
            this.lblMunItem = new System.Windows.Forms.Label();
            this.cmbMunItem = new System.Windows.Forms.ComboBox();
            this.lblMunQty = new System.Windows.Forms.Label();
            this.txtMunQty = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmdMunAdd = new System.Windows.Forms.Button();
            this.cmdMunRemove = new System.Windows.Forms.Button();

            this.flpBase.SuspendLayout();
            this.flpSearchList.SuspendLayout();
            this.flpFilter.SuspendLayout();
            this.flpCommands.SuspendLayout();
            this.flpDetail.SuspendLayout();
            this.flpName.SuspendLayout();
            this.flpStationType.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.tabHold.SuspendLayout();
            this.flpHoldAdd.SuspendLayout();
            this.tabComponents.SuspendLayout();
            this.flpBlueprintRow.SuspendLayout();
            this.tabMunitions.SuspendLayout();
            this.flpMunAdd.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHoldCrateContents)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvComponents)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMunitions)).BeginInit();
            this.SuspendLayout();

            // flpBase
            this.flpBase.Controls.Add(this.flpSearchList);
            this.flpBase.Controls.Add(this.flpDetail);
            this.flpBase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpBase.Location = new System.Drawing.Point(0, 0);
            this.flpBase.Name = "flpBase";
            this.flpBase.Size = new System.Drawing.Size(950, 620);
            this.flpBase.WrapContents = false;

            // flpSearchList
            this.flpSearchList.Controls.Add(this.flpFilter);
            this.flpSearchList.Controls.Add(this.lvwStations);
            this.flpSearchList.Controls.Add(this.flpCommands);
            this.flpSearchList.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpSearchList.Location = new System.Drawing.Point(3, 3);
            this.flpSearchList.Name = "flpSearchList";
            this.flpSearchList.Size = new System.Drawing.Size(220, 614);
            this.flpSearchList.WrapContents = false;

            // flpFilter
            this.flpFilter.AutoSize = true;
            this.flpFilter.Controls.Add(this.lblFilter);
            this.flpFilter.Controls.Add(this.txtFilter);
            this.flpFilter.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpFilter.Location = new System.Drawing.Point(3, 3);
            this.flpFilter.Name = "flpFilter";
            this.flpFilter.Size = new System.Drawing.Size(214, 26);

            // lblFilter
            this.lblFilter.AutoSize = true;
            this.lblFilter.Location = new System.Drawing.Point(3, 5);
            this.lblFilter.Name = "lblFilter";
            this.lblFilter.Size = new System.Drawing.Size(32, 13);
            this.lblFilter.Text = "Filter:";
            this.lblFilter.Anchor = System.Windows.Forms.AnchorStyles.Left;

            // txtFilter
            this.txtFilter.Location = new System.Drawing.Point(41, 3);
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.Size = new System.Drawing.Size(170, 20);

            // lvwStations
            this.lvwStations.FullRowSelect = true;
            this.lvwStations.HideSelection = false;
            this.lvwStations.Location = new System.Drawing.Point(3, 35);
            this.lvwStations.MultiSelect = false;
            this.lvwStations.Name = "lvwStations";
            this.lvwStations.Size = new System.Drawing.Size(214, 520);
            this.lvwStations.UseCompatibleStateImageBehavior = false;
            this.lvwStations.View = System.Windows.Forms.View.Details;

            // flpCommands
            this.flpCommands.AutoSize = true;
            this.flpCommands.Controls.Add(this.cmdNew);
            this.flpCommands.Controls.Add(this.cmdDelete);
            this.flpCommands.Location = new System.Drawing.Point(3, 561);
            this.flpCommands.Name = "flpCommands";
            this.flpCommands.Size = new System.Drawing.Size(214, 29);

            // cmdNew
            this.cmdNew.Location = new System.Drawing.Point(3, 3);
            this.cmdNew.Name = "cmdNew";
            this.cmdNew.Size = new System.Drawing.Size(55, 23);
            this.cmdNew.Text = "New";
            this.cmdNew.UseVisualStyleBackColor = true;

            // cmdDelete
            this.cmdDelete.Location = new System.Drawing.Point(64, 3);
            this.cmdDelete.Name = "cmdDelete";
            this.cmdDelete.Size = new System.Drawing.Size(55, 23);
            this.cmdDelete.Text = "Delete";
            this.cmdDelete.UseVisualStyleBackColor = true;

            // flpDetail
            this.flpDetail.Controls.Add(this.flpName);
            this.flpDetail.Controls.Add(this.flpStationType);
            this.flpDetail.Controls.Add(this.cmdSave);
            this.flpDetail.Controls.Add(this.tabControl);
            this.flpDetail.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpDetail.Location = new System.Drawing.Point(229, 3);
            this.flpDetail.Name = "flpDetail";
            this.flpDetail.Size = new System.Drawing.Size(718, 614);
            this.flpDetail.WrapContents = false;

            // flpName
            this.flpName.AutoSize = true;
            this.flpName.Controls.Add(this.lblName);
            this.flpName.Controls.Add(this.txtName);
            this.flpName.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpName.Location = new System.Drawing.Point(3, 3);
            this.flpName.Name = "flpName";
            this.flpName.Size = new System.Drawing.Size(712, 26);

            // lblName
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(3, 5);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(38, 13);
            this.lblName.Text = "Name:";
            this.lblName.Anchor = System.Windows.Forms.AnchorStyles.Left;

            // txtName
            this.txtName.Location = new System.Drawing.Point(47, 3);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(300, 20);

            // flpStationType
            this.flpStationType.AutoSize = true;
            this.flpStationType.Controls.Add(this.lblStationType);
            this.flpStationType.Controls.Add(this.cmbStationType);
            this.flpStationType.Controls.Add(this.lblOwnership);
            this.flpStationType.Controls.Add(this.cmbOwnership);
            this.flpStationType.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpStationType.Location = new System.Drawing.Point(3, 35);
            this.flpStationType.Name = "flpStationType";
            this.flpStationType.Size = new System.Drawing.Size(712, 27);

            // lblStationType
            this.lblStationType.AutoSize = true;
            this.lblStationType.Location = new System.Drawing.Point(3, 5);
            this.lblStationType.Name = "lblStationType";
            this.lblStationType.Size = new System.Drawing.Size(34, 13);
            this.lblStationType.Text = "Type:";
            this.lblStationType.Anchor = System.Windows.Forms.AnchorStyles.Left;

            // cmbStationType
            this.cmbStationType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbStationType.Location = new System.Drawing.Point(43, 3);
            this.cmbStationType.Name = "cmbStationType";
            this.cmbStationType.Size = new System.Drawing.Size(120, 21);

            // lblOwnership
            this.lblOwnership.AutoSize = true;
            this.lblOwnership.Location = new System.Drawing.Point(169, 5);
            this.lblOwnership.Name = "lblOwnership";
            this.lblOwnership.Size = new System.Drawing.Size(62, 13);
            this.lblOwnership.Text = "Ownership:";
            this.lblOwnership.Anchor = System.Windows.Forms.AnchorStyles.Left;

            // cmbOwnership
            this.cmbOwnership.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbOwnership.Location = new System.Drawing.Point(237, 3);
            this.cmbOwnership.Name = "cmbOwnership";
            this.cmbOwnership.Size = new System.Drawing.Size(120, 21);

            // cmdSave
            this.cmdSave.Location = new System.Drawing.Point(3, 68);
            this.cmdSave.Name = "cmdSave";
            this.cmdSave.Size = new System.Drawing.Size(75, 23);
            this.cmdSave.Text = "Save";
            this.cmdSave.UseVisualStyleBackColor = true;

            // tabControl
            this.tabControl.Controls.Add(this.tabHold);
            this.tabControl.Controls.Add(this.tabComponents);
            this.tabControl.Controls.Add(this.tabMunitions);
            this.tabControl.Location = new System.Drawing.Point(3, 97);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(712, 510);

            // tabHold
            this.tabHold.Controls.Add(this.dgvHold);
            this.tabHold.Controls.Add(this.lblHoldCrateContents);
            this.tabHold.Controls.Add(this.dgvHoldCrateContents);
            this.tabHold.Controls.Add(this.flpHoldAdd);
            this.tabHold.Controls.Add(this.cmdHoldRemove);
            this.tabHold.Location = new System.Drawing.Point(4, 22);
            this.tabHold.Name = "tabHold";
            this.tabHold.Padding = new System.Windows.Forms.Padding(3);
            this.tabHold.Size = new System.Drawing.Size(704, 484);
            this.tabHold.TabIndex = 0;
            this.tabHold.Text = "Hold";
            this.tabHold.UseVisualStyleBackColor = true;

            // dgvHold
            this.dgvHold.AllowUserToAddRows = false;
            this.dgvHold.AllowUserToDeleteRows = false;
            this.dgvHold.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvHold.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colHoldType, this.colHoldName, this.colHoldPurity, this.colHoldQty, this.colHoldCondition, this.colHoldMaxRepair});
            this.dgvHold.Location = new System.Drawing.Point(3, 3);
            this.dgvHold.Name = "dgvHold";
            this.dgvHold.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvHold.Size = new System.Drawing.Size(698, 200);

            // lblHoldCrateContents
            this.lblHoldCrateContents.AutoSize = true;
            this.lblHoldCrateContents.Location = new System.Drawing.Point(3, 235);
            this.lblHoldCrateContents.Name = "lblHoldCrateContents";
            this.lblHoldCrateContents.Text = "";
            this.lblHoldCrateContents.Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold);
            // dgvHoldCrateContents
            this.dgvHoldCrateContents.AllowUserToAddRows = false;
            this.dgvHoldCrateContents.AllowUserToDeleteRows = false;
            this.dgvHoldCrateContents.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvHoldCrateContents.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colHoldCrateType, this.colHoldCrateName, this.colHoldCratePurity, this.colHoldCrateQty});
            this.dgvHoldCrateContents.Location = new System.Drawing.Point(3, 252);
            this.dgvHoldCrateContents.Name = "dgvHoldCrateContents";
            this.dgvHoldCrateContents.ReadOnly = true;
            this.dgvHoldCrateContents.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvHoldCrateContents.Size = new System.Drawing.Size(698, 120);
            this.dgvHoldCrateContents.Visible = false;
            this.colHoldCrateType.HeaderText = "Type"; this.colHoldCrateType.Name = "colHoldCrateType"; this.colHoldCrateType.ReadOnly = true; this.colHoldCrateType.Width = 100;
            this.colHoldCrateName.HeaderText = "Name"; this.colHoldCrateName.Name = "colHoldCrateName"; this.colHoldCrateName.ReadOnly = true; this.colHoldCrateName.Width = 250;
            this.colHoldCratePurity.HeaderText = "Purity"; this.colHoldCratePurity.Name = "colHoldCratePurity"; this.colHoldCratePurity.ReadOnly = true; this.colHoldCratePurity.Width = 80;
            this.colHoldCrateQty.HeaderText = "Quantity"; this.colHoldCrateQty.Name = "colHoldCrateQty"; this.colHoldCrateQty.ReadOnly = true; this.colHoldCrateQty.Width = 80;

            // colHoldType
            this.colHoldType.HeaderText = "Type";
            this.colHoldType.Name = "colHoldType";
            this.colHoldType.ReadOnly = true;
            this.colHoldType.Width = 100;

            // colHoldName
            this.colHoldName.HeaderText = "Name";
            this.colHoldName.Name = "colHoldName";
            this.colHoldName.ReadOnly = true;
            this.colHoldName.Width = 200;

            // colHoldPurity
            this.colHoldPurity.HeaderText = "Purity";
            this.colHoldPurity.Name = "colHoldPurity";
            this.colHoldPurity.ReadOnly = true;
            this.colHoldPurity.Width = 80;

            // colHoldQty
            this.colHoldQty.HeaderText = "Quantity";
            this.colHoldQty.Name = "colHoldQty";
            this.colHoldQty.ReadOnly = true;
            this.colHoldQty.Width = 70;

            // colHoldCondition
            this.colHoldCondition.HeaderText = "Condition";
            this.colHoldCondition.Name = "colHoldCondition";
            this.colHoldCondition.Width = 80;

            // colHoldMaxRepair
            this.colHoldMaxRepair.HeaderText = "Max Repair %";
            this.colHoldMaxRepair.Name = "colHoldMaxRepair";
            this.colHoldMaxRepair.Width = 80;

            // flpHoldAdd
            this.flpHoldAdd.AutoSize = true;
            this.flpHoldAdd.Controls.Add(this.lblHoldType);
            this.flpHoldAdd.Controls.Add(this.cmbHoldType);
            this.flpHoldAdd.Controls.Add(this.lblHoldItem);
            this.flpHoldAdd.Controls.Add(this.cmbHoldItem);
            this.flpHoldAdd.Controls.Add(this.lblHoldPurity);
            this.flpHoldAdd.Controls.Add(this.cmbHoldPurity);
            this.flpHoldAdd.Controls.Add(this.lblHoldQty);
            this.flpHoldAdd.Controls.Add(this.txtHoldQty);
            this.flpHoldAdd.Controls.Add(this.cmdHoldAdd);
            this.flpHoldAdd.Location = new System.Drawing.Point(3, 359);
            this.flpHoldAdd.Name = "flpHoldAdd";
            this.flpHoldAdd.Size = new System.Drawing.Size(698, 30);

            // lblHoldType
            this.lblHoldType.AutoSize = true;
            this.lblHoldType.Text = "Type:";
            this.lblHoldType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblHoldType.Name = "lblHoldType";

            // cmbHoldType
            this.cmbHoldType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbHoldType.Size = new System.Drawing.Size(100, 21);
            this.cmbHoldType.Name = "cmbHoldType";

            // lblHoldItem
            this.lblHoldItem.AutoSize = true;
            this.lblHoldItem.Text = "Item:";
            this.lblHoldItem.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblHoldItem.Name = "lblHoldItem";

            // cmbHoldItem
            this.cmbHoldItem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbHoldItem.Size = new System.Drawing.Size(150, 21);
            this.cmbHoldItem.Name = "cmbHoldItem";

            // lblHoldPurity
            this.lblHoldPurity.AutoSize = true;
            this.lblHoldPurity.Text = "Purity:";
            this.lblHoldPurity.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblHoldPurity.Name = "lblHoldPurity";

            // cmbHoldPurity
            this.cmbHoldPurity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbHoldPurity.Size = new System.Drawing.Size(80, 21);
            this.cmbHoldPurity.Name = "cmbHoldPurity";

            // lblHoldQty
            this.lblHoldQty.AutoSize = true;
            this.lblHoldQty.Text = "Qty:";
            this.lblHoldQty.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblHoldQty.Name = "lblHoldQty";

            // txtHoldQty
            this.txtHoldQty.Size = new System.Drawing.Size(60, 20);
            this.txtHoldQty.Name = "txtHoldQty";

            // cmdHoldAdd
            this.cmdHoldAdd.Size = new System.Drawing.Size(50, 23);
            this.cmdHoldAdd.Text = "Add";
            this.cmdHoldAdd.UseVisualStyleBackColor = true;
            this.cmdHoldAdd.Name = "cmdHoldAdd";

            // cmdHoldRemove
            this.cmdHoldRemove.Size = new System.Drawing.Size(75, 23);
            this.cmdHoldRemove.Text = "Remove";
            this.cmdHoldRemove.UseVisualStyleBackColor = true;
            this.cmdHoldRemove.Name = "cmdHoldRemove";

            // cmdHoldRemove
            this.cmdHoldRemove.Location = new System.Drawing.Point(3, 395);

            // tabComponents
            this.tabComponents.Controls.Add(this.flpBlueprintRow);
            this.tabComponents.Controls.Add(this.dgvComponents);
            this.tabComponents.Controls.Add(this.rtbStationStats);
            this.tabComponents.Location = new System.Drawing.Point(4, 22);
            this.tabComponents.Name = "tabComponents";
            this.tabComponents.Padding = new System.Windows.Forms.Padding(3);
            this.tabComponents.Size = new System.Drawing.Size(704, 484);
            this.tabComponents.TabIndex = 1;
            this.tabComponents.Text = "Components";
            this.tabComponents.UseVisualStyleBackColor = true;

            // flpBlueprintRow
            this.flpBlueprintRow.AutoSize = true;
            this.flpBlueprintRow.Controls.Add(this.lblBlueprint);
            this.flpBlueprintRow.Controls.Add(this.cmbStationBlueprint);
            this.flpBlueprintRow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpBlueprintRow.Location = new System.Drawing.Point(3, 3);
            this.flpBlueprintRow.Name = "flpBlueprintRow";
            this.flpBlueprintRow.Size = new System.Drawing.Size(698, 27);
            this.lblBlueprint.AutoSize = true;
            this.lblBlueprint.Text = "Station Blueprint:";
            this.lblBlueprint.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblBlueprint.Name = "lblBlueprint";
            this.cmbStationBlueprint.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbStationBlueprint.Size = new System.Drawing.Size(300, 21);
            this.cmbStationBlueprint.Name = "cmbStationBlueprint";
            // dgvComponents
            this.dgvComponents.AllowUserToAddRows = false;
            this.dgvComponents.AllowUserToDeleteRows = false;
            this.dgvComponents.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvComponents.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colSlotType, this.colComponentName, this.colCondition, this.colMaxRepair});
            this.dgvComponents.Location = new System.Drawing.Point(3, 33);
            this.dgvComponents.Name = "dgvComponents";
            this.dgvComponents.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvComponents.Size = new System.Drawing.Size(698, 250);
            // rtbStationStats
            this.rtbStationStats.Location = new System.Drawing.Point(3, 289);
            this.rtbStationStats.Name = "rtbStationStats";
            this.rtbStationStats.ReadOnly = true;
            this.rtbStationStats.Size = new System.Drawing.Size(698, 170);
            this.rtbStationStats.Font = new System.Drawing.Font("Consolas", 8.25F);

            this.colSlotType.HeaderText = "Slot";
            this.colSlotType.Name = "colSlotType";
            this.colSlotType.ReadOnly = true;
            this.colSlotType.Width = 120;

            this.colComponentName.HeaderText = "Component";
            this.colComponentName.Name = "colComponentName";
            this.colComponentName.ReadOnly = true;
            this.colComponentName.Width = 250;

            this.colCondition.HeaderText = "Condition %";
            this.colCondition.Name = "colCondition";
            this.colCondition.Width = 90;

            this.colMaxRepair.HeaderText = "Max Repair %";
            this.colMaxRepair.Name = "colMaxRepair";
            this.colMaxRepair.Width = 90;

            // tabMunitions
            this.tabMunitions.Controls.Add(this.dgvMunitions);
            this.tabMunitions.Controls.Add(this.flpMunAdd);
            this.tabMunitions.Controls.Add(this.cmdMunRemove);
            this.tabMunitions.Location = new System.Drawing.Point(4, 22);
            this.tabMunitions.Name = "tabMunitions";
            this.tabMunitions.Padding = new System.Windows.Forms.Padding(3);
            this.tabMunitions.Size = new System.Drawing.Size(704, 484);
            this.tabMunitions.TabIndex = 2;
            this.tabMunitions.Text = "Munitions";
            this.tabMunitions.UseVisualStyleBackColor = true;

            // dgvMunitions
            this.dgvMunitions.AllowUserToAddRows = false;
            this.dgvMunitions.AllowUserToDeleteRows = false;
            this.dgvMunitions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvMunitions.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colMunName, this.colMunQty});
            this.dgvMunitions.Location = new System.Drawing.Point(3, 3);
            this.dgvMunitions.Name = "dgvMunitions";
            this.dgvMunitions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvMunitions.Size = new System.Drawing.Size(698, 350);

            this.colMunName.HeaderText = "Name";
            this.colMunName.Name = "colMunName";
            this.colMunName.ReadOnly = true;
            this.colMunName.Width = 300;

            this.colMunQty.HeaderText = "Quantity";
            this.colMunQty.Name = "colMunQty";
            this.colMunQty.ReadOnly = true;
            this.colMunQty.Width = 100;

            // flpMunAdd
            this.flpMunAdd.AutoSize = true;
            this.flpMunAdd.Controls.Add(this.lblMunItem);
            this.flpMunAdd.Controls.Add(this.cmbMunItem);
            this.flpMunAdd.Controls.Add(this.lblMunQty);
            this.flpMunAdd.Controls.Add(this.txtMunQty);
            this.flpMunAdd.Controls.Add(this.cmdMunAdd);
            this.flpMunAdd.Location = new System.Drawing.Point(3, 359);
            this.flpMunAdd.Name = "flpMunAdd";
            this.flpMunAdd.Size = new System.Drawing.Size(698, 30);

            this.lblMunItem.AutoSize = true;
            this.lblMunItem.Text = "Item:";
            this.lblMunItem.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblMunItem.Name = "lblMunItem";

            this.cmbMunItem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMunItem.Size = new System.Drawing.Size(200, 21);
            this.cmbMunItem.Name = "cmbMunItem";

            this.lblMunQty.AutoSize = true;
            this.lblMunQty.Text = "Qty:";
            this.lblMunQty.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblMunQty.Name = "lblMunQty";

            this.txtMunQty.Size = new System.Drawing.Size(60, 20);
            this.txtMunQty.Name = "txtMunQty";

            this.cmdMunAdd.Size = new System.Drawing.Size(50, 23);
            this.cmdMunAdd.Text = "Add";
            this.cmdMunAdd.UseVisualStyleBackColor = true;
            this.cmdMunAdd.Name = "cmdMunAdd";

            this.cmdMunRemove.Location = new System.Drawing.Point(3, 395);
            this.cmdMunRemove.Size = new System.Drawing.Size(75, 23);
            this.cmdMunRemove.Text = "Remove";
            this.cmdMunRemove.UseVisualStyleBackColor = true;
            this.cmdMunRemove.Name = "cmdMunRemove";

            // FormStation
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(950, 620);
            this.Controls.Add(this.flpBase);
            this.Name = "FormStation";
            this.Text = "Stations";
            this.flpBase.ResumeLayout(false);
            this.flpSearchList.ResumeLayout(false);
            this.flpFilter.ResumeLayout(false);
            this.flpCommands.ResumeLayout(false);
            this.flpDetail.ResumeLayout(false);
            this.flpName.ResumeLayout(false);
            this.flpStationType.ResumeLayout(false);
            this.tabControl.ResumeLayout(false);
            this.tabHold.ResumeLayout(false);
            this.flpHoldAdd.ResumeLayout(false);
            this.tabComponents.ResumeLayout(false);
            this.flpBlueprintRow.ResumeLayout(false);
            this.flpBlueprintRow.PerformLayout();
            this.tabMunitions.ResumeLayout(false);
            this.flpMunAdd.ResumeLayout(false);
            this.flpFilter.PerformLayout();
            this.flpName.PerformLayout();
            this.flpStationType.PerformLayout();
            this.flpCommands.PerformLayout();
            this.flpHoldAdd.PerformLayout();
            this.flpMunAdd.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHoldCrateContents)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvComponents)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMunitions)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flpBase;
        private System.Windows.Forms.FlowLayoutPanel flpSearchList;
        private System.Windows.Forms.FlowLayoutPanel flpFilter;
        private System.Windows.Forms.Label lblFilter;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtFilter;
        private System.Windows.Forms.ListView lvwStations;
        private System.Windows.Forms.FlowLayoutPanel flpCommands;
        private System.Windows.Forms.Button cmdNew;
        private System.Windows.Forms.Button cmdDelete;
        private System.Windows.Forms.FlowLayoutPanel flpDetail;
        private System.Windows.Forms.FlowLayoutPanel flpName;
        private System.Windows.Forms.Label lblName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtName;
        private System.Windows.Forms.FlowLayoutPanel flpStationType;
        private System.Windows.Forms.Label lblStationType;
        private System.Windows.Forms.ComboBox cmbStationType;
        private System.Windows.Forms.Label lblOwnership;
        private System.Windows.Forms.ComboBox cmbOwnership;
        private System.Windows.Forms.Button cmdSave;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabHold;
        private System.Windows.Forms.DataGridView dgvHold;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHoldType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHoldName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHoldPurity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHoldQty;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHoldCondition;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHoldMaxRepair;
        private System.Windows.Forms.FlowLayoutPanel flpHoldAdd;
        private System.Windows.Forms.Label lblHoldType;
        private System.Windows.Forms.ComboBox cmbHoldType;
        private System.Windows.Forms.Label lblHoldItem;
        private System.Windows.Forms.ComboBox cmbHoldItem;
        private System.Windows.Forms.Label lblHoldPurity;
        private System.Windows.Forms.ComboBox cmbHoldPurity;
        private System.Windows.Forms.Label lblHoldQty;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtHoldQty;
        private System.Windows.Forms.Button cmdHoldAdd;
        private System.Windows.Forms.Button cmdHoldRemove;
        private System.Windows.Forms.Label lblHoldCrateContents;
        private System.Windows.Forms.DataGridView dgvHoldCrateContents;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHoldCrateType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHoldCrateName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHoldCratePurity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHoldCrateQty;
        private System.Windows.Forms.TabPage tabComponents;
        private System.Windows.Forms.FlowLayoutPanel flpBlueprintRow;
        private System.Windows.Forms.Label lblBlueprint;
        private System.Windows.Forms.ComboBox cmbStationBlueprint;
        private System.Windows.Forms.DataGridView dgvComponents;
        private System.Windows.Forms.RichTextBox rtbStationStats;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSlotType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colComponentName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCondition;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMaxRepair;
        private System.Windows.Forms.TabPage tabMunitions;
        private System.Windows.Forms.DataGridView dgvMunitions;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMunName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMunQty;
        private System.Windows.Forms.FlowLayoutPanel flpMunAdd;
        private System.Windows.Forms.Label lblMunItem;
        private System.Windows.Forms.ComboBox cmbMunItem;
        private System.Windows.Forms.Label lblMunQty;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtMunQty;
        private System.Windows.Forms.Button cmdMunAdd;
        private System.Windows.Forms.Button cmdMunRemove;
    }
}
