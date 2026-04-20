namespace OE2EmpireTracker.Forms.StockTargets
{
    partial class FormStockTargets
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
            this.lvwPlans = new System.Windows.Forms.ListView();
            this.flpCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdNew = new System.Windows.Forms.Button();
            this.cmdDelete = new System.Windows.Forms.Button();
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabTargets = new System.Windows.Forms.TabPage();
            this.tabProfiles = new System.Windows.Forms.TabPage();
            this.flpDetail = new System.Windows.Forms.FlowLayoutPanel();
            this.flpNameRow = new System.Windows.Forms.FlowLayoutPanel();
            this.lblName = new System.Windows.Forms.Label();
            this.txtPlanName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.chkActive = new System.Windows.Forms.CheckBox();
            this.flpReplenishment = new System.Windows.Forms.FlowLayoutPanel();
            this.lblReplenishment = new System.Windows.Forms.Label();
            this.cmbReplenishmentPlan = new System.Windows.Forms.ComboBox();
            this.cmdSave = new System.Windows.Forms.Button();
            this.lblTargets = new System.Windows.Forms.Label();
            this.dgvTargets = new System.Windows.Forms.DataGridView();
            this.colTargetType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTargetItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTargetQty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCritical = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colScope = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLocation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCurrentQty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colShortfall = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpTargetAdd = new System.Windows.Forms.FlowLayoutPanel();
            this.lblTargetType = new System.Windows.Forms.Label();
            this.cmbTargetType = new System.Windows.Forms.ComboBox();
            this.lblTargetItem = new System.Windows.Forms.Label();
            this.cmbTargetItem = new System.Windows.Forms.ComboBox();
            this.lblTargetQty = new System.Windows.Forms.Label();
            this.txtTargetQty = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblCriticalThreshold = new System.Windows.Forms.Label();
            this.txtCriticalThreshold = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpTargetAdd2 = new System.Windows.Forms.FlowLayoutPanel();
            this.lblScope = new System.Windows.Forms.Label();
            this.cmbScope = new System.Windows.Forms.ComboBox();
            this.lblTargetLocation = new System.Windows.Forms.Label();
            this.cmbTargetLocation = new System.Windows.Forms.ComboBox();
            this.flpTargetButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdAddTarget = new System.Windows.Forms.Button();
            this.cmdRemoveTarget = new System.Windows.Forms.Button();
            this.cmdCheckGenerate = new System.Windows.Forms.Button();
            // Profiles tab controls
            this.flpProfilesTab = new System.Windows.Forms.FlowLayoutPanel();
            this.flpProfileList = new System.Windows.Forms.FlowLayoutPanel();
            this.lblProfileFilter = new System.Windows.Forms.Label();
            this.txtProfileFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lvwProfiles = new System.Windows.Forms.ListView();
            this.flpProfileCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdNewProfile = new System.Windows.Forms.Button();
            this.cmdDeleteProfile = new System.Windows.Forms.Button();
            this.flpProfileDetail = new System.Windows.Forms.FlowLayoutPanel();
            this.flpProfileNameRow = new System.Windows.Forms.FlowLayoutPanel();
            this.lblProfileName = new System.Windows.Forms.Label();
            this.txtProfileName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.chkProfileActive = new System.Windows.Forms.CheckBox();
            this.cmdSaveProfile = new System.Windows.Forms.Button();
            this.lblEntries = new System.Windows.Forms.Label();
            this.dgvEntries = new System.Windows.Forms.DataGridView();
            this.colEntryGroup = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEntryPlan = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpEntryAdd = new System.Windows.Forms.FlowLayoutPanel();
            this.lblGroupID = new System.Windows.Forms.Label();
            this.txtGroupID = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblEntry = new System.Windows.Forms.Label();
            this.txtEntryFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmbEntry = new System.Windows.Forms.ComboBox();
            this.flpEntryButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdAddEntry = new System.Windows.Forms.Button();
            this.cmdRemoveEntry = new System.Windows.Forms.Button();
            this.lblLogicSummary = new System.Windows.Forms.Label();

            this.flpBase.SuspendLayout();
            this.flpSearchList.SuspendLayout();
            this.flpFilter.SuspendLayout();
            this.flpCommands.SuspendLayout();
            this.tabMain.SuspendLayout();
            this.tabTargets.SuspendLayout();
            this.tabProfiles.SuspendLayout();
            this.flpDetail.SuspendLayout();
            this.flpNameRow.SuspendLayout();
            this.flpReplenishment.SuspendLayout();
            this.flpTargetAdd.SuspendLayout();
            this.flpTargetAdd2.SuspendLayout();
            this.flpTargetButtons.SuspendLayout();
            this.flpProfilesTab.SuspendLayout();
            this.flpProfileList.SuspendLayout();
            this.flpProfileCommands.SuspendLayout();
            this.flpProfileDetail.SuspendLayout();
            this.flpProfileNameRow.SuspendLayout();
            this.flpEntryAdd.SuspendLayout();
            this.flpEntryButtons.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTargets)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvEntries)).BeginInit();
            this.SuspendLayout();
            // flpBase
            this.flpBase.Controls.Add(this.flpSearchList);
            this.flpBase.Controls.Add(this.tabMain);
            this.flpBase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpBase.Location = new System.Drawing.Point(0, 0);
            this.flpBase.Name = "flpBase";
            this.flpBase.Size = new System.Drawing.Size(1050, 700);
            this.flpBase.WrapContents = false;
            // flpSearchList
            this.flpSearchList.Controls.Add(this.flpFilter);
            this.flpSearchList.Controls.Add(this.lvwPlans);
            this.flpSearchList.Controls.Add(this.flpCommands);
            this.flpSearchList.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpSearchList.Location = new System.Drawing.Point(3, 3);
            this.flpSearchList.Name = "flpSearchList";
            this.flpSearchList.Size = new System.Drawing.Size(220, 694);
            this.flpSearchList.WrapContents = false;
            // flpFilter
            this.flpFilter.AutoSize = true;
            this.flpFilter.Controls.Add(this.lblFilter);
            this.flpFilter.Controls.Add(this.txtFilter);
            this.flpFilter.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpFilter.Location = new System.Drawing.Point(3, 3);
            this.flpFilter.Name = "flpFilter";
            this.flpFilter.Size = new System.Drawing.Size(214, 26);
            this.lblFilter.AutoSize = true;
            this.lblFilter.Text = "Filter:";
            this.lblFilter.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblFilter.Name = "lblFilter";
            this.txtFilter.Location = new System.Drawing.Point(41, 3);
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.Size = new System.Drawing.Size(170, 20);
            // lvwPlans
            this.lvwPlans.FullRowSelect = true;
            this.lvwPlans.HideSelection = false;
            this.lvwPlans.Location = new System.Drawing.Point(3, 35);
            this.lvwPlans.MultiSelect = false;
            this.lvwPlans.Name = "lvwPlans";
            this.lvwPlans.Size = new System.Drawing.Size(214, 600);
            this.lvwPlans.UseCompatibleStateImageBehavior = false;
            this.lvwPlans.View = System.Windows.Forms.View.Details;
            // flpCommands
            this.flpCommands.AutoSize = true;
            this.flpCommands.Controls.Add(this.cmdNew);
            this.flpCommands.Controls.Add(this.cmdDelete);
            this.flpCommands.Location = new System.Drawing.Point(3, 641);
            this.flpCommands.Name = "flpCommands";
            this.flpCommands.Size = new System.Drawing.Size(214, 29);
            this.cmdNew.Location = new System.Drawing.Point(3, 3);
            this.cmdNew.Name = "cmdNew";
            this.cmdNew.Size = new System.Drawing.Size(75, 23);
            this.cmdNew.Text = "New Plan";
            this.cmdNew.UseVisualStyleBackColor = true;
            this.cmdDelete.Location = new System.Drawing.Point(84, 3);
            this.cmdDelete.Name = "cmdDelete";
            this.cmdDelete.Size = new System.Drawing.Size(55, 23);
            this.cmdDelete.Text = "Delete";
            this.cmdDelete.UseVisualStyleBackColor = true;
            // tabMain
            this.tabMain.Controls.Add(this.tabTargets);
            this.tabMain.Controls.Add(this.tabProfiles);
            this.tabMain.Location = new System.Drawing.Point(229, 3);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(818, 694);
            // tabTargets
            this.tabTargets.Controls.Add(this.flpDetail);
            this.tabTargets.Location = new System.Drawing.Point(4, 22);
            this.tabTargets.Name = "tabTargets";
            this.tabTargets.Padding = new System.Windows.Forms.Padding(3);
            this.tabTargets.Size = new System.Drawing.Size(810, 668);
            this.tabTargets.Text = "Targets && Plans";
            // tabProfiles
            this.tabProfiles.Controls.Add(this.flpProfilesTab);
            this.tabProfiles.Location = new System.Drawing.Point(4, 22);
            this.tabProfiles.Name = "tabProfiles";
            this.tabProfiles.Padding = new System.Windows.Forms.Padding(3);
            this.tabProfiles.Size = new System.Drawing.Size(810, 668);
            this.tabProfiles.Text = "Profiles";
            // flpDetail (existing targets content, now inside tabTargets)
            this.flpDetail.Controls.Add(this.flpNameRow);
            this.flpDetail.Controls.Add(this.flpReplenishment);
            this.flpDetail.Controls.Add(this.cmdSave);
            this.flpDetail.Controls.Add(this.lblTargets);
            this.flpDetail.Controls.Add(this.dgvTargets);
            this.flpDetail.Controls.Add(this.flpTargetAdd);
            this.flpDetail.Controls.Add(this.flpTargetAdd2);
            this.flpDetail.Controls.Add(this.flpTargetButtons);
            this.flpDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpDetail.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpDetail.Name = "flpDetail";
            this.flpDetail.Size = new System.Drawing.Size(804, 662);
            this.flpDetail.WrapContents = false;
            this.flpDetail.AutoScroll = true;
            // flpNameRow
            this.flpNameRow.AutoSize = true;
            this.flpNameRow.Controls.Add(this.lblName);
            this.flpNameRow.Controls.Add(this.txtPlanName);
            this.flpNameRow.Controls.Add(this.chkActive);
            this.flpNameRow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpNameRow.Name = "flpNameRow";
            this.flpNameRow.Size = new System.Drawing.Size(798, 26);
            this.lblName.AutoSize = true;
            this.lblName.Text = "Name:";
            this.lblName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblName.Name = "lblName";
            this.txtPlanName.Location = new System.Drawing.Point(47, 3);
            this.txtPlanName.Name = "txtPlanName";
            this.txtPlanName.Size = new System.Drawing.Size(300, 20);
            this.chkActive.AutoSize = true;
            this.chkActive.Checked = true;
            this.chkActive.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkActive.Name = "chkActive";
            this.chkActive.Text = "Active";
            this.chkActive.UseVisualStyleBackColor = true;
            // flpReplenishment
            this.flpReplenishment.AutoSize = true;
            this.flpReplenishment.Controls.Add(this.lblReplenishment);
            this.flpReplenishment.Controls.Add(this.cmbReplenishmentPlan);
            this.flpReplenishment.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpReplenishment.Name = "flpReplenishment";
            this.flpReplenishment.Size = new System.Drawing.Size(798, 26);
            this.lblReplenishment.AutoSize = true;
            this.lblReplenishment.Text = "Replenishment Plan:";
            this.lblReplenishment.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblReplenishment.Name = "lblReplenishment";
            this.cmbReplenishmentPlan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbReplenishmentPlan.Size = new System.Drawing.Size(300, 21);
            this.cmbReplenishmentPlan.Name = "cmbReplenishmentPlan";
            // cmdSave
            this.cmdSave.Name = "cmdSave";
            this.cmdSave.Size = new System.Drawing.Size(75, 23);
            this.cmdSave.Text = "Save";
            this.cmdSave.UseVisualStyleBackColor = true;
            // lblTargets
            this.lblTargets.AutoSize = true;
            this.lblTargets.Name = "lblTargets";
            this.lblTargets.Text = "Targets:";
            this.lblTargets.Font = new System.Drawing.Font(this.lblTargets.Font, System.Drawing.FontStyle.Bold);
            // dgvTargets
            this.dgvTargets.AllowUserToAddRows = false;
            this.dgvTargets.AllowUserToDeleteRows = false;
            this.dgvTargets.AllowUserToOrderColumns = true;
            this.dgvTargets.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTargets.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colTargetType, this.colTargetItem, this.colTargetQty, this.colCritical,
            this.colScope, this.colLocation, this.colCurrentQty, this.colShortfall});
            this.dgvTargets.Name = "dgvTargets";
            this.dgvTargets.ReadOnly = true;
            this.dgvTargets.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvTargets.Size = new System.Drawing.Size(798, 200);
            this.colTargetType.HeaderText = "Type"; this.colTargetType.Name = "colTargetType"; this.colTargetType.ReadOnly = true; this.colTargetType.Width = 80;
            this.colTargetItem.HeaderText = "Item"; this.colTargetItem.Name = "colTargetItem"; this.colTargetItem.ReadOnly = true; this.colTargetItem.Width = 150;
            this.colTargetQty.HeaderText = "Target"; this.colTargetQty.Name = "colTargetQty"; this.colTargetQty.ReadOnly = true; this.colTargetQty.Width = 60;
            this.colCritical.HeaderText = "Critical"; this.colCritical.Name = "colCritical"; this.colCritical.ReadOnly = true; this.colCritical.Width = 60;
            this.colScope.HeaderText = "Scope"; this.colScope.Name = "colScope"; this.colScope.ReadOnly = true; this.colScope.Width = 80;
            this.colLocation.HeaderText = "Location"; this.colLocation.Name = "colLocation"; this.colLocation.ReadOnly = true; this.colLocation.Width = 120;
            this.colCurrentQty.HeaderText = "Current"; this.colCurrentQty.Name = "colCurrentQty"; this.colCurrentQty.ReadOnly = true; this.colCurrentQty.Width = 60;
            this.colShortfall.HeaderText = "Shortfall"; this.colShortfall.Name = "colShortfall"; this.colShortfall.ReadOnly = true; this.colShortfall.Width = 70;
            // flpTargetAdd
            this.flpTargetAdd.AutoSize = true;
            this.flpTargetAdd.Controls.Add(this.lblTargetType);
            this.flpTargetAdd.Controls.Add(this.cmbTargetType);
            this.flpTargetAdd.Controls.Add(this.lblTargetItem);
            this.flpTargetAdd.Controls.Add(this.cmbTargetItem);
            this.flpTargetAdd.Controls.Add(this.lblTargetQty);
            this.flpTargetAdd.Controls.Add(this.txtTargetQty);
            this.flpTargetAdd.Controls.Add(this.lblCriticalThreshold);
            this.flpTargetAdd.Controls.Add(this.txtCriticalThreshold);
            this.flpTargetAdd.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpTargetAdd.Name = "flpTargetAdd";
            this.flpTargetAdd.Size = new System.Drawing.Size(798, 30);
            this.lblTargetType.AutoSize = true; this.lblTargetType.Text = "Type:"; this.lblTargetType.Anchor = System.Windows.Forms.AnchorStyles.Left; this.lblTargetType.Name = "lblTargetType";
            this.cmbTargetType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbTargetType.Size = new System.Drawing.Size(110, 21); this.cmbTargetType.Name = "cmbTargetType";
            this.lblTargetItem.AutoSize = true; this.lblTargetItem.Text = "Item:"; this.lblTargetItem.Anchor = System.Windows.Forms.AnchorStyles.Left; this.lblTargetItem.Name = "lblTargetItem";
            this.cmbTargetItem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbTargetItem.Size = new System.Drawing.Size(200, 21); this.cmbTargetItem.Name = "cmbTargetItem";
            this.lblTargetQty.AutoSize = true; this.lblTargetQty.Text = "Qty:"; this.lblTargetQty.Anchor = System.Windows.Forms.AnchorStyles.Left; this.lblTargetQty.Name = "lblTargetQty";
            this.txtTargetQty.Size = new System.Drawing.Size(60, 20); this.txtTargetQty.Name = "txtTargetQty";
            this.lblCriticalThreshold.AutoSize = true; this.lblCriticalThreshold.Text = "Critical:"; this.lblCriticalThreshold.Anchor = System.Windows.Forms.AnchorStyles.Left; this.lblCriticalThreshold.Name = "lblCriticalThreshold";
            this.txtCriticalThreshold.Size = new System.Drawing.Size(60, 20); this.txtCriticalThreshold.Name = "txtCriticalThreshold";
            // flpTargetAdd2
            this.flpTargetAdd2.AutoSize = true;
            this.flpTargetAdd2.Controls.Add(this.lblScope);
            this.flpTargetAdd2.Controls.Add(this.cmbScope);
            this.flpTargetAdd2.Controls.Add(this.lblTargetLocation);
            this.flpTargetAdd2.Controls.Add(this.cmbTargetLocation);
            this.flpTargetAdd2.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpTargetAdd2.Name = "flpTargetAdd2";
            this.flpTargetAdd2.Size = new System.Drawing.Size(798, 30);
            this.lblScope.AutoSize = true; this.lblScope.Text = "Scope:"; this.lblScope.Anchor = System.Windows.Forms.AnchorStyles.Left; this.lblScope.Name = "lblScope";
            this.cmbScope.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbScope.Size = new System.Drawing.Size(100, 21); this.cmbScope.Name = "cmbScope";
            this.lblTargetLocation.AutoSize = true; this.lblTargetLocation.Text = "Location:"; this.lblTargetLocation.Anchor = System.Windows.Forms.AnchorStyles.Left; this.lblTargetLocation.Name = "lblTargetLocation";
            this.cmbTargetLocation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbTargetLocation.Size = new System.Drawing.Size(200, 21); this.cmbTargetLocation.Name = "cmbTargetLocation";
            // flpTargetButtons
            this.flpTargetButtons.AutoSize = true;
            this.flpTargetButtons.Controls.Add(this.cmdAddTarget);
            this.flpTargetButtons.Controls.Add(this.cmdRemoveTarget);
            this.flpTargetButtons.Controls.Add(this.cmdCheckGenerate);
            this.flpTargetButtons.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpTargetButtons.Name = "flpTargetButtons";
            this.flpTargetButtons.Size = new System.Drawing.Size(798, 29);
            this.cmdAddTarget.Size = new System.Drawing.Size(80, 23); this.cmdAddTarget.Text = "Add Target"; this.cmdAddTarget.UseVisualStyleBackColor = true; this.cmdAddTarget.Name = "cmdAddTarget";
            this.cmdRemoveTarget.Size = new System.Drawing.Size(95, 23); this.cmdRemoveTarget.Text = "Remove Target"; this.cmdRemoveTarget.UseVisualStyleBackColor = true; this.cmdRemoveTarget.Name = "cmdRemoveTarget";
            this.cmdCheckGenerate.Size = new System.Drawing.Size(150, 23); this.cmdCheckGenerate.Text = "Check && Generate Orders"; this.cmdCheckGenerate.UseVisualStyleBackColor = true; this.cmdCheckGenerate.Name = "cmdCheckGenerate";
            // === Profiles Tab ===
            // flpProfilesTab
            this.flpProfilesTab.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpProfilesTab.Controls.Add(this.flpProfileList);
            this.flpProfilesTab.Controls.Add(this.flpProfileDetail);
            this.flpProfilesTab.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpProfilesTab.Name = "flpProfilesTab";
            this.flpProfilesTab.WrapContents = false;
            // flpProfileList
            this.flpProfileList.Controls.Add(this.lblProfileFilter);
            this.flpProfileList.Controls.Add(this.txtProfileFilter);
            this.flpProfileList.Controls.Add(this.lvwProfiles);
            this.flpProfileList.Controls.Add(this.flpProfileCommands);
            this.flpProfileList.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpProfileList.Name = "flpProfileList";
            this.flpProfileList.Size = new System.Drawing.Size(220, 640);
            this.flpProfileList.WrapContents = false;
            this.lblProfileFilter.AutoSize = true;
            this.lblProfileFilter.Text = "Filter:";
            this.lblProfileFilter.Name = "lblProfileFilter";
            this.txtProfileFilter.Name = "txtProfileFilter";
            this.txtProfileFilter.Size = new System.Drawing.Size(214, 20);
            // lvwProfiles
            this.lvwProfiles.FullRowSelect = true;
            this.lvwProfiles.HideSelection = false;
            this.lvwProfiles.MultiSelect = false;
            this.lvwProfiles.Name = "lvwProfiles";
            this.lvwProfiles.Size = new System.Drawing.Size(214, 540);
            this.lvwProfiles.UseCompatibleStateImageBehavior = false;
            this.lvwProfiles.View = System.Windows.Forms.View.Details;
            // flpProfileCommands
            this.flpProfileCommands.AutoSize = true;
            this.flpProfileCommands.Controls.Add(this.cmdNewProfile);
            this.flpProfileCommands.Controls.Add(this.cmdDeleteProfile);
            this.flpProfileCommands.Name = "flpProfileCommands";
            this.cmdNewProfile.Size = new System.Drawing.Size(80, 23); this.cmdNewProfile.Text = "New Profile"; this.cmdNewProfile.UseVisualStyleBackColor = true; this.cmdNewProfile.Name = "cmdNewProfile";
            this.cmdDeleteProfile.Size = new System.Drawing.Size(55, 23); this.cmdDeleteProfile.Text = "Delete"; this.cmdDeleteProfile.UseVisualStyleBackColor = true; this.cmdDeleteProfile.Name = "cmdDeleteProfile";
            // flpProfileDetail
            this.flpProfileDetail.Controls.Add(this.flpProfileNameRow);
            this.flpProfileDetail.Controls.Add(this.cmdSaveProfile);
            this.flpProfileDetail.Controls.Add(this.lblEntries);
            this.flpProfileDetail.Controls.Add(this.dgvEntries);
            this.flpProfileDetail.Controls.Add(this.flpEntryAdd);
            this.flpProfileDetail.Controls.Add(this.flpEntryButtons);
            this.flpProfileDetail.Controls.Add(this.lblLogicSummary);
            this.flpProfileDetail.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpProfileDetail.Name = "flpProfileDetail";
            this.flpProfileDetail.Size = new System.Drawing.Size(570, 640);
            this.flpProfileDetail.WrapContents = false;
            this.flpProfileDetail.AutoScroll = true;
            // flpProfileNameRow
            this.flpProfileNameRow.AutoSize = true;
            this.flpProfileNameRow.Controls.Add(this.lblProfileName);
            this.flpProfileNameRow.Controls.Add(this.txtProfileName);
            this.flpProfileNameRow.Controls.Add(this.chkProfileActive);
            this.flpProfileNameRow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpProfileNameRow.Name = "flpProfileNameRow";
            this.flpProfileNameRow.Size = new System.Drawing.Size(564, 26);
            this.lblProfileName.AutoSize = true; this.lblProfileName.Text = "Profile:"; this.lblProfileName.Anchor = System.Windows.Forms.AnchorStyles.Left; this.lblProfileName.Name = "lblProfileName";
            this.txtProfileName.Size = new System.Drawing.Size(250, 20); this.txtProfileName.Name = "txtProfileName";
            this.chkProfileActive.AutoSize = true; this.chkProfileActive.Checked = true; this.chkProfileActive.CheckState = System.Windows.Forms.CheckState.Checked; this.chkProfileActive.Name = "chkProfileActive"; this.chkProfileActive.Text = "Active"; this.chkProfileActive.UseVisualStyleBackColor = true;
            // cmdSaveProfile
            this.cmdSaveProfile.Size = new System.Drawing.Size(75, 23); this.cmdSaveProfile.Text = "Save"; this.cmdSaveProfile.UseVisualStyleBackColor = true; this.cmdSaveProfile.Name = "cmdSaveProfile";
            // lblEntries
            this.lblEntries.AutoSize = true; this.lblEntries.Name = "lblEntries"; this.lblEntries.Text = "Entries:"; this.lblEntries.Font = new System.Drawing.Font(this.lblEntries.Font, System.Drawing.FontStyle.Bold);
            // dgvEntries
            this.dgvEntries.AllowUserToAddRows = false;
            this.dgvEntries.AllowUserToDeleteRows = false;
            this.dgvEntries.AllowUserToOrderColumns = true;
            this.dgvEntries.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvEntries.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { this.colEntryGroup, this.colEntryPlan });
            this.dgvEntries.Name = "dgvEntries";
            this.dgvEntries.ReadOnly = true;
            this.dgvEntries.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvEntries.Size = new System.Drawing.Size(564, 200);
            this.colEntryGroup.HeaderText = "Group"; this.colEntryGroup.Name = "colEntryGroup"; this.colEntryGroup.ReadOnly = true; this.colEntryGroup.Width = 60;
            this.colEntryPlan.HeaderText = "Plan"; this.colEntryPlan.Name = "colEntryPlan"; this.colEntryPlan.ReadOnly = true; this.colEntryPlan.Width = 300;
            // flpEntryAdd
            this.flpEntryAdd.AutoSize = true;
            this.flpEntryAdd.Controls.Add(this.lblGroupID);
            this.flpEntryAdd.Controls.Add(this.txtGroupID);
            this.flpEntryAdd.Controls.Add(this.lblEntry);
            this.flpEntryAdd.Controls.Add(this.txtEntryFilter);
            this.flpEntryAdd.Controls.Add(this.cmbEntry);
            this.flpEntryAdd.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpEntryAdd.Name = "flpEntryAdd";
            this.flpEntryAdd.Size = new System.Drawing.Size(564, 30);
            this.lblGroupID.AutoSize = true; this.lblGroupID.Text = "Group:"; this.lblGroupID.Anchor = System.Windows.Forms.AnchorStyles.Left; this.lblGroupID.Name = "lblGroupID";
            this.txtGroupID.Size = new System.Drawing.Size(40, 20); this.txtGroupID.Name = "txtGroupID"; this.txtGroupID.Text = "A";
            this.lblEntry.AutoSize = true; this.lblEntry.Text = "Plan:"; this.lblEntry.Anchor = System.Windows.Forms.AnchorStyles.Left; this.lblEntry.Name = "lblEntry";
            this.txtEntryFilter.Size = new System.Drawing.Size(100, 20); this.txtEntryFilter.Name = "txtEntryFilter";
            this.cmbEntry.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbEntry.Size = new System.Drawing.Size(250, 21); this.cmbEntry.Name = "cmbEntry";
            // flpEntryButtons
            this.flpEntryButtons.AutoSize = true;
            this.flpEntryButtons.Controls.Add(this.cmdAddEntry);
            this.flpEntryButtons.Controls.Add(this.cmdRemoveEntry);
            this.flpEntryButtons.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpEntryButtons.Name = "flpEntryButtons";
            this.cmdAddEntry.Size = new System.Drawing.Size(75, 23); this.cmdAddEntry.Text = "Add Entry"; this.cmdAddEntry.UseVisualStyleBackColor = true; this.cmdAddEntry.Name = "cmdAddEntry";
            this.cmdRemoveEntry.Size = new System.Drawing.Size(95, 23); this.cmdRemoveEntry.Text = "Remove Entry"; this.cmdRemoveEntry.UseVisualStyleBackColor = true; this.cmdRemoveEntry.Name = "cmdRemoveEntry";
            // lblLogicSummary
            this.lblLogicSummary.AutoSize = true; this.lblLogicSummary.Name = "lblLogicSummary"; this.lblLogicSummary.Text = ""; this.lblLogicSummary.MaximumSize = new System.Drawing.Size(560, 0);
            // FormStockTargets
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1050, 700);
            this.Controls.Add(this.flpBase);
            this.Name = "FormStockTargets";
            this.Text = "Stock Targets";
            this.flpBase.ResumeLayout(false);
            this.flpSearchList.ResumeLayout(false);
            this.flpFilter.ResumeLayout(false);
            this.flpCommands.ResumeLayout(false);
            this.tabMain.ResumeLayout(false);
            this.tabTargets.ResumeLayout(false);
            this.tabProfiles.ResumeLayout(false);
            this.flpDetail.ResumeLayout(false);
            this.flpNameRow.ResumeLayout(false);
            this.flpReplenishment.ResumeLayout(false);
            this.flpTargetAdd.ResumeLayout(false);
            this.flpTargetAdd2.ResumeLayout(false);
            this.flpTargetButtons.ResumeLayout(false);
            this.flpProfilesTab.ResumeLayout(false);
            this.flpProfileList.ResumeLayout(false);
            this.flpProfileCommands.ResumeLayout(false);
            this.flpProfileDetail.ResumeLayout(false);
            this.flpProfileNameRow.ResumeLayout(false);
            this.flpEntryAdd.ResumeLayout(false);
            this.flpEntryButtons.ResumeLayout(false);
            this.flpFilter.PerformLayout();
            this.flpNameRow.PerformLayout();
            this.flpReplenishment.PerformLayout();
            this.flpCommands.PerformLayout();
            this.flpTargetAdd.PerformLayout();
            this.flpTargetAdd2.PerformLayout();
            this.flpTargetButtons.PerformLayout();
            this.flpProfileCommands.PerformLayout();
            this.flpProfileNameRow.PerformLayout();
            this.flpEntryAdd.PerformLayout();
            this.flpEntryButtons.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTargets)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvEntries)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flpBase;
        private System.Windows.Forms.FlowLayoutPanel flpSearchList;
        private System.Windows.Forms.FlowLayoutPanel flpFilter;
        private System.Windows.Forms.Label lblFilter;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtFilter;
        private System.Windows.Forms.ListView lvwPlans;
        private System.Windows.Forms.FlowLayoutPanel flpCommands;
        private System.Windows.Forms.Button cmdNew;
        private System.Windows.Forms.Button cmdDelete;
        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabTargets;
        private System.Windows.Forms.TabPage tabProfiles;
        private System.Windows.Forms.FlowLayoutPanel flpDetail;
        private System.Windows.Forms.FlowLayoutPanel flpNameRow;
        private System.Windows.Forms.Label lblName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtPlanName;
        private System.Windows.Forms.CheckBox chkActive;
        private System.Windows.Forms.FlowLayoutPanel flpReplenishment;
        private System.Windows.Forms.Label lblReplenishment;
        private System.Windows.Forms.ComboBox cmbReplenishmentPlan;
        private System.Windows.Forms.Button cmdSave;
        private System.Windows.Forms.Label lblTargets;
        private System.Windows.Forms.DataGridView dgvTargets;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTargetType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTargetItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTargetQty;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCritical;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScope;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLocation;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCurrentQty;
        private System.Windows.Forms.DataGridViewTextBoxColumn colShortfall;
        private System.Windows.Forms.FlowLayoutPanel flpTargetAdd;
        private System.Windows.Forms.Label lblTargetType;
        private System.Windows.Forms.ComboBox cmbTargetType;
        private System.Windows.Forms.Label lblTargetItem;
        private System.Windows.Forms.ComboBox cmbTargetItem;
        private System.Windows.Forms.Label lblTargetQty;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtTargetQty;
        private System.Windows.Forms.Label lblCriticalThreshold;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtCriticalThreshold;
        private System.Windows.Forms.FlowLayoutPanel flpTargetAdd2;
        private System.Windows.Forms.Label lblScope;
        private System.Windows.Forms.ComboBox cmbScope;
        private System.Windows.Forms.Label lblTargetLocation;
        private System.Windows.Forms.ComboBox cmbTargetLocation;
        private System.Windows.Forms.FlowLayoutPanel flpTargetButtons;
        private System.Windows.Forms.Button cmdAddTarget;
        private System.Windows.Forms.Button cmdRemoveTarget;
        private System.Windows.Forms.Button cmdCheckGenerate;
        // Profiles tab
        private System.Windows.Forms.FlowLayoutPanel flpProfilesTab;
        private System.Windows.Forms.FlowLayoutPanel flpProfileList;
        private System.Windows.Forms.Label lblProfileFilter;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtProfileFilter;
        private System.Windows.Forms.ListView lvwProfiles;
        private System.Windows.Forms.FlowLayoutPanel flpProfileCommands;
        private System.Windows.Forms.Button cmdNewProfile;
        private System.Windows.Forms.Button cmdDeleteProfile;
        private System.Windows.Forms.FlowLayoutPanel flpProfileDetail;
        private System.Windows.Forms.FlowLayoutPanel flpProfileNameRow;
        private System.Windows.Forms.Label lblProfileName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtProfileName;
        private System.Windows.Forms.CheckBox chkProfileActive;
        private System.Windows.Forms.Button cmdSaveProfile;
        private System.Windows.Forms.Label lblEntries;
        private System.Windows.Forms.DataGridView dgvEntries;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEntryGroup;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEntryPlan;
        private System.Windows.Forms.FlowLayoutPanel flpEntryAdd;
        private System.Windows.Forms.Label lblGroupID;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtGroupID;
        private System.Windows.Forms.Label lblEntry;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtEntryFilter;
        private System.Windows.Forms.ComboBox cmbEntry;
        private System.Windows.Forms.FlowLayoutPanel flpEntryButtons;
        private System.Windows.Forms.Button cmdAddEntry;
        private System.Windows.Forms.Button cmdRemoveEntry;
        private System.Windows.Forms.Label lblLogicSummary;
    }
}
