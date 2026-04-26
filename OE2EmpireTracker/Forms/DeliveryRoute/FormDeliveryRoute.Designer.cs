namespace OE2EmpireTracker.Forms.DeliveryRoute
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
            this.colDestType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colColonyName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPlanetName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSystemName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPurpose = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFuelEstimate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpAddStop = new System.Windows.Forms.FlowLayoutPanel();
            this.lblAddStop = new System.Windows.Forms.Label();
            this.cmbColony = new System.Windows.Forms.ComboBox();
            this.cmdAddStop = new System.Windows.Forms.Button();
            this.cmdUp = new System.Windows.Forms.Button();
            this.cmdDown = new System.Windows.Forms.Button();
            this.cmdRemoveStop = new System.Windows.Forms.Button();
            this.chkPreventDuplicates = new System.Windows.Forms.CheckBox();
            this.cmbDestType = new System.Windows.Forms.ComboBox();
            this.cmbStopPurpose = new System.Windows.Forms.ComboBox();
            this.tabRouteDetail = new System.Windows.Forms.TabControl();
            this.tabStops = new System.Windows.Forms.TabPage();
            this.tabPlan = new System.Windows.Forms.TabPage();
            this.flpPlanContent = new System.Windows.Forms.FlowLayoutPanel();
            this.lblPlanStop = new System.Windows.Forms.Label();
            this.lblDropOff = new System.Windows.Forms.Label();
            this.dgvDropOff = new DataEntryGridView();
            this.colDropType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDropName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDropQty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpDropOffAdd = new System.Windows.Forms.FlowLayoutPanel();
            this.cmbDropItemType = new System.Windows.Forms.ComboBox();
            this.cmbDropItem = new OE2EmpireTracker.Controls.FilteredTextComboSet();
            this.txtDropQty = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmdAddDropOff = new System.Windows.Forms.Button();
            this.cmbDropPurity = new System.Windows.Forms.ComboBox();
            this.cmdRemoveDropOff = new System.Windows.Forms.Button();
            this.lblPickUp = new System.Windows.Forms.Label();
            this.dgvPickUp = new DataEntryGridView();
            this.colPickType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPickName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPickQty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpPickUpAdd = new System.Windows.Forms.FlowLayoutPanel();
            this.cmbPickItemType = new System.Windows.Forms.ComboBox();
            this.cmbPickItem = new OE2EmpireTracker.Controls.FilteredTextComboSet();
            this.txtPickQty = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmdAddPickUp = new System.Windows.Forms.Button();
            this.cmbPickPurity = new System.Windows.Forms.ComboBox();
            this.cmdRemovePickUp = new System.Windows.Forms.Button();
            this.flpPlanSelector = new System.Windows.Forms.FlowLayoutPanel();
            this.txtPlanFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmbPlan = new System.Windows.Forms.ComboBox();
            this.chkShowCompleted = new System.Windows.Forms.CheckBox();
            this.cmdNewPlan = new System.Windows.Forms.Button();
            this.cmdDeletePlan = new System.Windows.Forms.Button();
            this.cmdExecutePlan = new System.Windows.Forms.Button();
            this.cmdAutoFill = new System.Windows.Forms.Button();
            this.flpPlanName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblPlanNameLabel = new System.Windows.Forms.Label();
            this.txtPlanName = new OE2EmpireTracker.Controls.ValidatedTextBox();
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
            this.flpRouteData.Controls.Add(this.tabRouteDetail);
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
            this.dgvStops.AllowUserToOrderColumns = true;
            this.dgvStops.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvStops.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colSequence,
            this.colDestType,
            this.colColonyName,
            this.colPlanetName,
            this.colSystemName,
            this.colPurpose,
            this.colFuelEstimate});
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
            // colDestType
            // 
            this.colDestType.HeaderText = "Type";
            this.colDestType.Name = "colDestType";
            this.colDestType.ReadOnly = true;
            this.colDestType.Width = 60;
            // 
            // colColonyName
            // 
            this.colColonyName.HeaderText = "Destination";
            this.colColonyName.Name = "colColonyName";
            this.colColonyName.ReadOnly = true;
            this.colColonyName.Width = 160;
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
            this.colSystemName.Width = 120;
            // 
            // colPurpose
            // 
            this.colPurpose.HeaderText = "Purpose";
            this.colPurpose.Name = "colPurpose";
            this.colPurpose.ReadOnly = true;
            this.colPurpose.Width = 90;
            // 
            // colFuelEstimate
            // 
            this.colFuelEstimate.HeaderText = "Fuel Est.";
            this.colFuelEstimate.Name = "colFuelEstimate";
            this.colFuelEstimate.ReadOnly = true;
            this.colFuelEstimate.Width = 60;
            // 
            // tabRouteDetail
            // 
            this.tabRouteDetail.Controls.Add(this.tabStops);
            this.tabRouteDetail.Controls.Add(this.tabPlan);
            this.tabRouteDetail.Location = new System.Drawing.Point(2, 32);
            this.tabRouteDetail.Margin = new System.Windows.Forms.Padding(2);
            this.tabRouteDetail.Name = "tabRouteDetail";
            this.tabRouteDetail.SelectedIndex = 0;
            this.tabRouteDetail.Size = new System.Drawing.Size(630, 430);
            this.tabRouteDetail.TabIndex = 1;
            // 
            // tabStops
            // 
            this.tabStops.Controls.Add(this.dgvStops);
            this.tabStops.Controls.Add(this.flpAddStop);
            this.tabStops.Location = new System.Drawing.Point(4, 22);
            this.tabStops.Name = "tabStops";
            this.tabStops.Padding = new System.Windows.Forms.Padding(3);
            this.tabStops.Size = new System.Drawing.Size(622, 404);
            this.tabStops.TabIndex = 0;
            this.tabStops.Text = "Stops";
            this.tabStops.UseVisualStyleBackColor = true;
            // 
            // tabPlan
            // 
            this.tabPlan.Controls.Add(this.flpPlanContent);
            this.tabPlan.Location = new System.Drawing.Point(4, 22);
            this.tabPlan.Name = "tabPlan";
            this.tabPlan.Padding = new System.Windows.Forms.Padding(3);
            this.tabPlan.Size = new System.Drawing.Size(622, 404);
            this.tabPlan.TabIndex = 1;
            this.tabPlan.Text = "Plan";
            this.tabPlan.UseVisualStyleBackColor = true;
            // 
            // flpPlanContent
            // 
            this.flpPlanContent.Controls.Add(this.flpPlanSelector);
            this.flpPlanContent.Controls.Add(this.flpPlanName);
            this.flpPlanContent.Controls.Add(this.lblPlanStop);
            this.flpPlanContent.Controls.Add(this.lblDropOff);
            this.flpPlanContent.Controls.Add(this.dgvDropOff);
            this.flpPlanContent.Controls.Add(this.flpDropOffAdd);
            this.flpPlanContent.Controls.Add(this.lblPickUp);
            this.flpPlanContent.Controls.Add(this.dgvPickUp);
            this.flpPlanContent.Controls.Add(this.flpPickUpAdd);
            this.flpPlanContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpPlanContent.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpPlanContent.Name = "flpPlanContent";
            this.flpPlanContent.Size = new System.Drawing.Size(616, 398);
            this.flpPlanContent.WrapContents = false;
            // 
            // flpPlanSelector
            // 
            this.flpPlanSelector.Controls.Add(this.chkShowCompleted);
            this.flpPlanSelector.Controls.Add(this.txtPlanFilter);
            this.flpPlanSelector.Controls.Add(this.cmbPlan);
            this.flpPlanSelector.Controls.Add(this.cmdNewPlan);
            this.flpPlanSelector.Controls.Add(this.cmdDeletePlan);
            this.flpPlanSelector.Controls.Add(this.cmdExecutePlan);
            this.flpPlanSelector.Controls.Add(this.cmdAutoFill);
            this.flpPlanSelector.Location = new System.Drawing.Point(2, 2);
            this.flpPlanSelector.Margin = new System.Windows.Forms.Padding(2);
            this.flpPlanSelector.Name = "flpPlanSelector";
            this.flpPlanSelector.Size = new System.Drawing.Size(680, 28);
            // 
            // txtPlanFilter
            // 
            this.txtPlanFilter.Location = new System.Drawing.Point(3, 3);
            this.txtPlanFilter.Name = "txtPlanFilter";
            this.txtPlanFilter.Size = new System.Drawing.Size(80, 20);
            // 
            // cmbPlan
            // 
            this.cmbPlan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPlan.Location = new System.Drawing.Point(89, 3);
            this.cmbPlan.Name = "cmbPlan";
            this.cmbPlan.Size = new System.Drawing.Size(200, 21);
            // 
            // chkShowCompleted
            // 
            this.chkShowCompleted.AutoSize = true;
            this.chkShowCompleted.Location = new System.Drawing.Point(295, 5);
            this.chkShowCompleted.Name = "chkShowCompleted";
            this.chkShowCompleted.Size = new System.Drawing.Size(75, 17);
            this.chkShowCompleted.Text = "Show Completed";
            this.chkShowCompleted.UseVisualStyleBackColor = true;
            // 
            // cmdNewPlan
            // 
            this.cmdNewPlan.Location = new System.Drawing.Point(376, 3);
            this.cmdNewPlan.Name = "cmdNewPlan";
            this.cmdNewPlan.Size = new System.Drawing.Size(40, 23);
            this.cmdNewPlan.Text = "New";
            this.cmdNewPlan.UseVisualStyleBackColor = true;
            // 
            // cmdDeletePlan
            // 
            this.cmdDeletePlan.Location = new System.Drawing.Point(422, 3);
            this.cmdDeletePlan.Name = "cmdDeletePlan";
            this.cmdDeletePlan.Size = new System.Drawing.Size(50, 23);
            this.cmdDeletePlan.Text = "Delete";
            this.cmdDeletePlan.UseVisualStyleBackColor = true;
            // 
            // cmdExecutePlan
            // 
            this.cmdExecutePlan.Location = new System.Drawing.Point(478, 3);
            this.cmdExecutePlan.Name = "cmdExecutePlan";
            this.cmdExecutePlan.Size = new System.Drawing.Size(55, 23);
            this.cmdExecutePlan.Text = "Execute";
            this.cmdExecutePlan.UseVisualStyleBackColor = true;
            // 
            // cmdAutoFill
            // 
            this.cmdAutoFill.Location = new System.Drawing.Point(539, 3);
            this.cmdAutoFill.Name = "cmdAutoFill";
            this.cmdAutoFill.Size = new System.Drawing.Size(60, 23);
            this.cmdAutoFill.Text = "Auto-Fill";
            this.cmdAutoFill.UseVisualStyleBackColor = true;
            // 
            // flpPlanName
            // 
            this.flpPlanName.Controls.Add(this.lblPlanNameLabel);
            this.flpPlanName.Controls.Add(this.txtPlanName);
            this.flpPlanName.Location = new System.Drawing.Point(2, 34);
            this.flpPlanName.Margin = new System.Windows.Forms.Padding(2);
            this.flpPlanName.Name = "flpPlanName";
            this.flpPlanName.Size = new System.Drawing.Size(400, 26);
            // 
            // lblPlanNameLabel
            // 
            this.lblPlanNameLabel.Location = new System.Drawing.Point(2, 4);
            this.lblPlanNameLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblPlanNameLabel.Name = "lblPlanNameLabel";
            this.lblPlanNameLabel.Size = new System.Drawing.Size(70, 17);
            this.lblPlanNameLabel.Text = "Plan Name";
            this.lblPlanNameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtPlanName
            // 
            this.txtPlanName.Location = new System.Drawing.Point(77, 3);
            this.txtPlanName.Name = "txtPlanName";
            this.txtPlanName.Size = new System.Drawing.Size(300, 20);
            // 
            // lblPlanStop
            // 
            this.lblPlanStop.AutoSize = true;
            this.lblPlanStop.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblPlanStop.Location = new System.Drawing.Point(3, 3);
            this.lblPlanStop.Margin = new System.Windows.Forms.Padding(3);
            this.lblPlanStop.Name = "lblPlanStop";
            this.lblPlanStop.Size = new System.Drawing.Size(150, 13);
            this.lblPlanStop.Text = "(select a stop on Stops tab)";
            // 
            // lblDropOff
            // 
            this.lblDropOff.AutoSize = true;
            this.lblDropOff.Location = new System.Drawing.Point(3, 22);
            this.lblDropOff.Margin = new System.Windows.Forms.Padding(3);
            this.lblDropOff.Name = "lblDropOff";
            this.lblDropOff.Size = new System.Drawing.Size(50, 13);
            this.lblDropOff.Text = "Drop Off:";
            // 
            // dgvDropOff
            // 
            this.dgvDropOff.AllowUserToAddRows = false;
            this.dgvDropOff.AllowUserToDeleteRows = false;
            this.dgvDropOff.AllowUserToOrderColumns = true;
            this.dgvDropOff.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDropOff.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colDropType, this.colDropName, this.colDropQty});
            this.dgvDropOff.Location = new System.Drawing.Point(2, 41);
            this.dgvDropOff.Margin = new System.Windows.Forms.Padding(2);
            this.dgvDropOff.Name = "dgvDropOff";
            this.dgvDropOff.ReadOnly = true;
            this.dgvDropOff.RowHeadersVisible = false;
            this.dgvDropOff.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvDropOff.Size = new System.Drawing.Size(610, 120);
            this.dgvDropOff.TabIndex = 2;
            // 
            // colDropType
            // 
            this.colDropType.HeaderText = "Type";
            this.colDropType.Name = "colDropType";
            this.colDropType.ReadOnly = true;
            this.colDropType.Width = 100;
            // 
            // colDropName
            // 
            this.colDropName.HeaderText = "Item";
            this.colDropName.Name = "colDropName";
            this.colDropName.ReadOnly = true;
            this.colDropName.Width = 350;
            // 
            // colDropQty
            // 
            this.colDropQty.HeaderText = "Qty";
            this.colDropQty.Name = "colDropQty";
            this.colDropQty.ReadOnly = true;
            this.colDropQty.Width = 60;
            // 
            // flpDropOffAdd
            // 
            this.flpDropOffAdd.Controls.Add(this.cmbDropItemType);
            this.flpDropOffAdd.Controls.Add(this.cmbDropItem);
            this.flpDropOffAdd.Controls.Add(this.cmbDropPurity);
            this.flpDropOffAdd.Controls.Add(this.txtDropQty);
            this.flpDropOffAdd.Controls.Add(this.cmdAddDropOff);
            this.flpDropOffAdd.Controls.Add(this.cmdRemoveDropOff);
            this.flpDropOffAdd.Location = new System.Drawing.Point(2, 165);
            this.flpDropOffAdd.Margin = new System.Windows.Forms.Padding(2);
            this.flpDropOffAdd.Name = "flpDropOffAdd";
            this.flpDropOffAdd.Size = new System.Drawing.Size(610, 28);
            // 
            // cmbDropItemType
            // 
            this.cmbDropItemType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDropItemType.Location = new System.Drawing.Point(3, 3);
            this.cmbDropItemType.Name = "cmbDropItemType";
            this.cmbDropItemType.Size = new System.Drawing.Size(100, 21);
            // 
            // cmbDropItem
            // 
            this.cmbDropItem.Location = new System.Drawing.Point(109, 3);
            this.cmbDropItem.Name = "cmbDropItem";
            this.cmbDropItem.Size = new System.Drawing.Size(280, 21);
            // 
            // cmbDropPurity
            // 
            this.cmbDropPurity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDropPurity.Location = new System.Drawing.Point(401, 3);
            this.cmbDropPurity.Name = "cmbDropPurity";
            this.cmbDropPurity.Size = new System.Drawing.Size(70, 21);
            this.cmbDropPurity.Visible = false;
            // 
            // txtDropQty
            // 
            this.txtDropQty.Location = new System.Drawing.Point(395, 3);
            this.txtDropQty.Name = "txtDropQty";
            this.txtDropQty.Size = new System.Drawing.Size(50, 20);
            this.txtDropQty.Text = "1";
            // 
            // cmdAddDropOff
            // 
            this.cmdAddDropOff.Location = new System.Drawing.Point(451, 3);
            this.cmdAddDropOff.Name = "cmdAddDropOff";
            this.cmdAddDropOff.Size = new System.Drawing.Size(40, 23);
            this.cmdAddDropOff.Text = "Add";
            this.cmdAddDropOff.UseVisualStyleBackColor = true;
            // 
            // cmdRemoveDropOff
            // 
            this.cmdRemoveDropOff.Location = new System.Drawing.Point(497, 3);
            this.cmdRemoveDropOff.Name = "cmdRemoveDropOff";
            this.cmdRemoveDropOff.Size = new System.Drawing.Size(60, 23);
            this.cmdRemoveDropOff.Text = "Remove";
            this.cmdRemoveDropOff.UseVisualStyleBackColor = true;
            // 
            // lblPickUp
            // 
            this.lblPickUp.AutoSize = true;
            this.lblPickUp.Location = new System.Drawing.Point(3, 198);
            this.lblPickUp.Margin = new System.Windows.Forms.Padding(3);
            this.lblPickUp.Name = "lblPickUp";
            this.lblPickUp.Size = new System.Drawing.Size(46, 13);
            this.lblPickUp.Text = "Pick Up:";
            // 
            // dgvPickUp
            // 
            this.dgvPickUp.AllowUserToAddRows = false;
            this.dgvPickUp.AllowUserToDeleteRows = false;
            this.dgvPickUp.AllowUserToOrderColumns = true;
            this.dgvPickUp.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPickUp.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colPickType, this.colPickName, this.colPickQty});
            this.dgvPickUp.Location = new System.Drawing.Point(2, 217);
            this.dgvPickUp.Margin = new System.Windows.Forms.Padding(2);
            this.dgvPickUp.Name = "dgvPickUp";
            this.dgvPickUp.ReadOnly = true;
            this.dgvPickUp.RowHeadersVisible = false;
            this.dgvPickUp.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPickUp.Size = new System.Drawing.Size(610, 120);
            this.dgvPickUp.TabIndex = 5;
            // 
            // colPickType
            // 
            this.colPickType.HeaderText = "Type";
            this.colPickType.Name = "colPickType";
            this.colPickType.ReadOnly = true;
            this.colPickType.Width = 100;
            // 
            // colPickName
            // 
            this.colPickName.HeaderText = "Item";
            this.colPickName.Name = "colPickName";
            this.colPickName.ReadOnly = true;
            this.colPickName.Width = 350;
            // 
            // colPickQty
            // 
            this.colPickQty.HeaderText = "Qty";
            this.colPickQty.Name = "colPickQty";
            this.colPickQty.ReadOnly = true;
            this.colPickQty.Width = 60;
            // 
            // flpPickUpAdd
            // 
            this.flpPickUpAdd.Controls.Add(this.cmbPickItemType);
            this.flpPickUpAdd.Controls.Add(this.cmbPickItem);
            this.flpPickUpAdd.Controls.Add(this.cmbPickPurity);
            this.flpPickUpAdd.Controls.Add(this.txtPickQty);
            this.flpPickUpAdd.Controls.Add(this.cmdAddPickUp);
            this.flpPickUpAdd.Controls.Add(this.cmdRemovePickUp);
            this.flpPickUpAdd.Location = new System.Drawing.Point(2, 341);
            this.flpPickUpAdd.Margin = new System.Windows.Forms.Padding(2);
            this.flpPickUpAdd.Name = "flpPickUpAdd";
            this.flpPickUpAdd.Size = new System.Drawing.Size(610, 28);
            // 
            // cmbPickItemType
            // 
            this.cmbPickItemType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPickItemType.Location = new System.Drawing.Point(3, 3);
            this.cmbPickItemType.Name = "cmbPickItemType";
            this.cmbPickItemType.Size = new System.Drawing.Size(100, 21);
            // 
            // cmbPickItem
            // 
            this.cmbPickItem.Location = new System.Drawing.Point(109, 3);
            this.cmbPickItem.Name = "cmbPickItem";
            this.cmbPickItem.Size = new System.Drawing.Size(280, 21);
            // 
            // cmbPickPurity
            // 
            this.cmbPickPurity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPickPurity.Location = new System.Drawing.Point(401, 3);
            this.cmbPickPurity.Name = "cmbPickPurity";
            this.cmbPickPurity.Size = new System.Drawing.Size(70, 21);
            this.cmbPickPurity.Visible = false;
            // 
            // txtPickQty
            // 
            this.txtPickQty.Location = new System.Drawing.Point(395, 3);
            this.txtPickQty.Name = "txtPickQty";
            this.txtPickQty.Size = new System.Drawing.Size(50, 20);
            this.txtPickQty.Text = "1";
            // 
            // cmdAddPickUp
            // 
            this.cmdAddPickUp.Location = new System.Drawing.Point(451, 3);
            this.cmdAddPickUp.Name = "cmdAddPickUp";
            this.cmdAddPickUp.Size = new System.Drawing.Size(40, 23);
            this.cmdAddPickUp.Text = "Add";
            this.cmdAddPickUp.UseVisualStyleBackColor = true;
            // 
            // cmdRemovePickUp
            // 
            this.cmdRemovePickUp.Location = new System.Drawing.Point(497, 3);
            this.cmdRemovePickUp.Name = "cmdRemovePickUp";
            this.cmdRemovePickUp.Size = new System.Drawing.Size(60, 23);
            this.cmdRemovePickUp.Text = "Remove";
            this.cmdRemovePickUp.UseVisualStyleBackColor = true;
            // 
            // flpAddStop
            // 
            this.flpAddStop.Controls.Add(this.chkPreventDuplicates);
            this.flpAddStop.Controls.Add(this.cmbDestType);
            this.flpAddStop.Controls.Add(this.lblAddStop);
            this.flpAddStop.Controls.Add(this.cmbColony);
            this.flpAddStop.Controls.Add(this.cmbStopPurpose);
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
            // cmbDestType
            // 
            this.cmbDestType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDestType.Location = new System.Drawing.Point(104, 3);
            this.cmbDestType.Name = "cmbDestType";
            this.cmbDestType.Size = new System.Drawing.Size(75, 21);
            // 
            // lblAddStop
            // 
            this.lblAddStop.Location = new System.Drawing.Point(2, 4);
            this.lblAddStop.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblAddStop.Name = "lblAddStop";
            this.lblAddStop.Size = new System.Drawing.Size(50, 17);
            this.lblAddStop.TabIndex = 0;
            this.lblAddStop.Text = "Dest";
            this.lblAddStop.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbColony
            // 
            this.cmbColony.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbColony.Location = new System.Drawing.Point(57, 3);
            this.cmbColony.Name = "cmbColony";
            this.cmbColony.Size = new System.Drawing.Size(250, 21);
            this.cmbColony.TabIndex = 1;
            // 
            // cmbStopPurpose
            // 
            this.cmbStopPurpose.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbStopPurpose.Location = new System.Drawing.Point(313, 3);
            this.cmbStopPurpose.Name = "cmbStopPurpose";
            this.cmbStopPurpose.Size = new System.Drawing.Size(90, 21);
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
        private System.Windows.Forms.DataGridViewTextBoxColumn colDestType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colColonyName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPlanetName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSystemName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPurpose;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFuelEstimate;
        private System.Windows.Forms.FlowLayoutPanel flpAddStop;
        private System.Windows.Forms.Label lblAddStop;
        private System.Windows.Forms.ComboBox cmbColony;
        private System.Windows.Forms.Button cmdAddStop;
        private System.Windows.Forms.Button cmdUp;
        private System.Windows.Forms.Button cmdDown;
        private System.Windows.Forms.Button cmdRemoveStop;
        private System.Windows.Forms.CheckBox chkPreventDuplicates;
        private System.Windows.Forms.ComboBox cmbDestType;
        private System.Windows.Forms.ComboBox cmbStopPurpose;
        private System.Windows.Forms.TabControl tabRouteDetail;
        private System.Windows.Forms.TabPage tabStops;
        private System.Windows.Forms.TabPage tabPlan;
        private System.Windows.Forms.FlowLayoutPanel flpPlanContent;
        private System.Windows.Forms.Label lblPlanStop;
        private System.Windows.Forms.Label lblDropOff;
        private DataEntryGridView dgvDropOff;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDropType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDropName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDropQty;
        private System.Windows.Forms.FlowLayoutPanel flpDropOffAdd;
        private System.Windows.Forms.ComboBox cmbDropItemType;
        private OE2EmpireTracker.Controls.FilteredTextComboSet cmbDropItem;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtDropQty;
        private System.Windows.Forms.Button cmdAddDropOff;
        private System.Windows.Forms.ComboBox cmbDropPurity;
        private System.Windows.Forms.Button cmdRemoveDropOff;
        private System.Windows.Forms.Label lblPickUp;
        private DataEntryGridView dgvPickUp;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPickType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPickName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPickQty;
        private System.Windows.Forms.FlowLayoutPanel flpPickUpAdd;
        private System.Windows.Forms.ComboBox cmbPickItemType;
        private OE2EmpireTracker.Controls.FilteredTextComboSet cmbPickItem;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtPickQty;
        private System.Windows.Forms.Button cmdAddPickUp;
        private System.Windows.Forms.ComboBox cmbPickPurity;
        private System.Windows.Forms.Button cmdRemovePickUp;
        private System.Windows.Forms.FlowLayoutPanel flpPlanSelector;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtPlanFilter;
        private System.Windows.Forms.ComboBox cmbPlan;
        private System.Windows.Forms.CheckBox chkShowCompleted;
        private System.Windows.Forms.Button cmdNewPlan;
        private System.Windows.Forms.Button cmdDeletePlan;
        private System.Windows.Forms.Button cmdExecutePlan;
        private System.Windows.Forms.Button cmdAutoFill;
        private System.Windows.Forms.FlowLayoutPanel flpPlanName;
        private System.Windows.Forms.Label lblPlanNameLabel;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtPlanName;
        private System.Windows.Forms.FlowLayoutPanel flpCommands;
        private System.Windows.Forms.Button cmdNew;
        private System.Windows.Forms.Button cmdSave;
        private System.Windows.Forms.Button cmdDelete;
    }
}
