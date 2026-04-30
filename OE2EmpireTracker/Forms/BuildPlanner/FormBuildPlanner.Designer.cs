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
            this.components = new System.ComponentModel.Container();
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
            this.colSequence = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDependsOn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblResource = new System.Windows.Forms.Label();
            this.cmbResource = new System.Windows.Forms.ComboBox();
            this.lblPurity = new System.Windows.Forms.Label();
            this.cmbPurity = new System.Windows.Forms.ComboBox();
            this.lblSurvey = new System.Windows.Forms.Label();
            this.cmbSurvey = new System.Windows.Forms.ComboBox();
            this.cmdStartManufacturing = new System.Windows.Forms.Button();
            this.cmdStartAllReady = new System.Windows.Forms.Button();
            this.tsmiStartManufacturing = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiStartAllReady = new System.Windows.Forms.ToolStripMenuItem();
            this.lblStatusSummary = new System.Windows.Forms.Label();
            this.cmsBuildItems = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmiSetDependency = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiClearDependency = new System.Windows.Forms.ToolStripMenuItem();

            this.lblShortfallHeader = new System.Windows.Forms.Label();
            this.dgvShortfalls = new System.Windows.Forms.DataGridView();
            this.colShortfallResource = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colShortfallRequired = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colShortfallAvailable = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colShortfallDeficit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblShortfallStatus = new System.Windows.Forms.Label();
            this.flpAddItem = new System.Windows.Forms.FlowLayoutPanel();
            this.lblAddItemHeader = new System.Windows.Forms.Label();
            this.flpAddItemRow1 = new System.Windows.Forms.FlowLayoutPanel();
            this.lblItemType = new System.Windows.Forms.Label();
            this.cmbItemType = new System.Windows.Forms.ComboBox();
            this.lblItemFilter = new System.Windows.Forms.Label();
            this.txtItemFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblItem = new System.Windows.Forms.Label();
            this.cmbItem = new System.Windows.Forms.ComboBox();
            this.flpAddItemRow2 = new System.Windows.Forms.FlowLayoutPanel();
            this.lblQuantity = new System.Windows.Forms.Label();
            this.txtQuantity = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblRecipient = new System.Windows.Forms.Label();
            this.txtRecipient = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblTargetDuration = new System.Windows.Forms.Label();
            this.txtTargetDuration = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpAddItemRow3 = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdAddItem = new System.Windows.Forms.Button();
            this.cmdQueueCalc = new System.Windows.Forms.Button();
            this.cmdAllocate = new System.Windows.Forms.Button();
            this.cmdGenerateDelivery = new System.Windows.Forms.Button();
            this.cmdAutoAssign = new System.Windows.Forms.Button();
            this.cmsGenerateDelivery = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmiResourceDelivery = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiConsolidatedDelivery = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiFlatpackDelivery = new System.Windows.Forms.ToolStripMenuItem();

            this.flpBase.SuspendLayout();
            this.flpSearchList.SuspendLayout();
            this.flpPlanFilter.SuspendLayout();
            this.flpCommands.SuspendLayout();
            this.flpDetail.SuspendLayout();
            this.flpPlanName.SuspendLayout();
            this.flpDescription.SuspendLayout();
            this.flpIsActive.SuspendLayout();
            this.flpAddItem.SuspendLayout();
            this.flpAddItemRow1.SuspendLayout();
            this.flpAddItemRow2.SuspendLayout();
            this.flpAddItemRow3.SuspendLayout();
            this.cmsGenerateDelivery.SuspendLayout();
            this.cmsBuildItems.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBuildItems)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvShortfalls)).BeginInit();
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
            this.flpDetail.Controls.Add(this.lblStatusSummary);
            this.flpDetail.Controls.Add(this.dgvBuildItems);
            this.flpDetail.Controls.Add(this.lblShortfallHeader);
            this.flpDetail.Controls.Add(this.lblShortfallStatus);
            this.flpDetail.Controls.Add(this.dgvShortfalls);
            this.flpDetail.Controls.Add(this.flpAddItem);
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
            // lblStatusSummary
            // 
            this.lblStatusSummary.AutoSize = true;
            this.lblStatusSummary.Location = new System.Drawing.Point(3, 0);
            this.lblStatusSummary.Name = "lblStatusSummary";
            this.lblStatusSummary.Size = new System.Drawing.Size(300, 13);
            this.lblStatusSummary.Text = "";
            this.lblStatusSummary.ForeColor = System.Drawing.SystemColors.GrayText;
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
            this.colNotes,
            this.colSequence,
            this.colDependsOn});
            this.dgvBuildItems.Location = new System.Drawing.Point(3, 125);
            this.dgvBuildItems.Name = "dgvBuildItems";
            this.dgvBuildItems.ReadOnly = true;
            this.dgvBuildItems.Size = new System.Drawing.Size(662, 460);
            this.dgvBuildItems.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvBuildItems.ContextMenuStrip = this.cmsBuildItems;
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
            // colSequence
            // 
            this.colSequence.HeaderText = "Seq";
            this.colSequence.Name = "colSequence";
            this.colSequence.ReadOnly = true;
            this.colSequence.Width = 40;
            // 
            // colDependsOn
            // 
            this.colDependsOn.HeaderText = "Depends On";
            this.colDependsOn.Name = "colDependsOn";
            this.colDependsOn.ReadOnly = true;
            this.colDependsOn.Width = 120;
            // 
            // lblShortfallHeader
            // 
            this.lblShortfallHeader.AutoSize = true;
            this.lblShortfallHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblShortfallHeader.Location = new System.Drawing.Point(3, 0);
            this.lblShortfallHeader.Name = "lblShortfallHeader";
            this.lblShortfallHeader.Size = new System.Drawing.Size(120, 13);
            this.lblShortfallHeader.Text = "Resource Shortfalls:";
            // 
            // lblShortfallStatus
            // 
            this.lblShortfallStatus.AutoSize = true;
            this.lblShortfallStatus.Location = new System.Drawing.Point(3, 0);
            this.lblShortfallStatus.Name = "lblShortfallStatus";
            this.lblShortfallStatus.Size = new System.Drawing.Size(200, 13);
            this.lblShortfallStatus.Text = "Select a build item to check resources.";
            this.lblShortfallStatus.ForeColor = System.Drawing.SystemColors.GrayText;
            // 
            // dgvShortfalls
            // 
            this.dgvShortfalls.AllowUserToAddRows = false;
            this.dgvShortfalls.AllowUserToDeleteRows = false;
            this.dgvShortfalls.AllowUserToOrderColumns = true;
            this.dgvShortfalls.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvShortfalls.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colShortfallResource,
            this.colShortfallRequired,
            this.colShortfallAvailable,
            this.colShortfallDeficit});
            this.dgvShortfalls.Location = new System.Drawing.Point(3, 0);
            this.dgvShortfalls.Name = "dgvShortfalls";
            this.dgvShortfalls.ReadOnly = true;
            this.dgvShortfalls.Size = new System.Drawing.Size(662, 120);
            this.dgvShortfalls.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvShortfalls.Visible = false;
            // 
            // colShortfallResource
            // 
            this.colShortfallResource.HeaderText = "Resource";
            this.colShortfallResource.Name = "colShortfallResource";
            this.colShortfallResource.ReadOnly = true;
            this.colShortfallResource.Width = 200;
            // 
            // colShortfallRequired
            // 
            this.colShortfallRequired.HeaderText = "Required";
            this.colShortfallRequired.Name = "colShortfallRequired";
            this.colShortfallRequired.ReadOnly = true;
            this.colShortfallRequired.Width = 100;
            // 
            // colShortfallAvailable
            // 
            this.colShortfallAvailable.HeaderText = "Available";
            this.colShortfallAvailable.Name = "colShortfallAvailable";
            this.colShortfallAvailable.ReadOnly = true;
            this.colShortfallAvailable.Width = 100;
            // 
            // colShortfallDeficit
            // 
            this.colShortfallDeficit.HeaderText = "Shortfall";
            this.colShortfallDeficit.Name = "colShortfallDeficit";
            this.colShortfallDeficit.ReadOnly = true;
            this.colShortfallDeficit.Width = 100;
            // 
            // flpAddItem
            // 
            this.flpAddItem.Controls.Add(this.lblAddItemHeader);
            this.flpAddItem.Controls.Add(this.flpAddItemRow1);
            this.flpAddItem.Controls.Add(this.flpAddItemRow2);
            this.flpAddItem.Controls.Add(this.flpAddItemRow3);
            this.flpAddItem.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpAddItem.Location = new System.Drawing.Point(3, 591);
            this.flpAddItem.Name = "flpAddItem";
            this.flpAddItem.Size = new System.Drawing.Size(662, 100);
            this.flpAddItem.WrapContents = false;
            // 
            // lblAddItemHeader
            // 
            this.lblAddItemHeader.AutoSize = true;
            this.lblAddItemHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblAddItemHeader.Location = new System.Drawing.Point(3, 0);
            this.lblAddItemHeader.Name = "lblAddItemHeader";
            this.lblAddItemHeader.Size = new System.Drawing.Size(60, 13);
            this.lblAddItemHeader.Text = "Add Item:";
            // 
            // flpAddItemRow1
            // 
            this.flpAddItemRow1.AutoSize = true;
            this.flpAddItemRow1.Controls.Add(this.lblItemType);
            this.flpAddItemRow1.Controls.Add(this.cmbItemType);
            this.flpAddItemRow1.Controls.Add(this.lblItemFilter);
            this.flpAddItemRow1.Controls.Add(this.txtItemFilter);
            this.flpAddItemRow1.Controls.Add(this.lblItem);
            this.flpAddItemRow1.Controls.Add(this.cmbItem);
            this.flpAddItemRow1.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpAddItemRow1.Location = new System.Drawing.Point(3, 16);
            this.flpAddItemRow1.Name = "flpAddItemRow1";
            this.flpAddItemRow1.Size = new System.Drawing.Size(656, 26);
            // 
            // lblItemType
            // 
            this.lblItemType.AutoSize = true;
            this.lblItemType.Location = new System.Drawing.Point(3, 5);
            this.lblItemType.Name = "lblItemType";
            this.lblItemType.Size = new System.Drawing.Size(34, 13);
            this.lblItemType.Text = "Type:";
            this.lblItemType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            // 
            // cmbItemType
            // 
            this.cmbItemType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbItemType.Location = new System.Drawing.Point(43, 3);
            this.cmbItemType.Name = "cmbItemType";
            this.cmbItemType.Size = new System.Drawing.Size(120, 21);
            // 
            // lblItemFilter
            // 
            this.lblItemFilter.AutoSize = true;
            this.lblItemFilter.Location = new System.Drawing.Point(169, 5);
            this.lblItemFilter.Name = "lblItemFilter";
            this.lblItemFilter.Size = new System.Drawing.Size(32, 13);
            this.lblItemFilter.Text = "Filter:";
            this.lblItemFilter.Anchor = System.Windows.Forms.AnchorStyles.Left;
            // 
            // txtItemFilter
            // 
            this.txtItemFilter.Location = new System.Drawing.Point(207, 3);
            this.txtItemFilter.Name = "txtItemFilter";
            this.txtItemFilter.Size = new System.Drawing.Size(120, 20);
            // 
            // lblItem
            // 
            this.lblItem.AutoSize = true;
            this.lblItem.Location = new System.Drawing.Point(333, 5);
            this.lblItem.Name = "lblItem";
            this.lblItem.Size = new System.Drawing.Size(30, 13);
            this.lblItem.Text = "Item:";
            this.lblItem.Anchor = System.Windows.Forms.AnchorStyles.Left;
            // 
            // cmbItem
            // 
            this.cmbItem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbItem.Location = new System.Drawing.Point(369, 3);
            this.cmbItem.Name = "cmbItem";
            this.cmbItem.Size = new System.Drawing.Size(280, 21);
            // 
            // flpAddItemRow2
            // 
            this.flpAddItemRow2.AutoSize = true;
            this.flpAddItemRow2.Controls.Add(this.lblQuantity);
            this.flpAddItemRow2.Controls.Add(this.txtQuantity);
            this.flpAddItemRow2.Controls.Add(this.lblRecipient);
            this.flpAddItemRow2.Controls.Add(this.txtRecipient);
            this.flpAddItemRow2.Controls.Add(this.lblTargetDuration);
            this.flpAddItemRow2.Controls.Add(this.txtTargetDuration);
            this.flpAddItemRow2.Controls.Add(this.lblResource);
            this.flpAddItemRow2.Controls.Add(this.cmbResource);
            this.flpAddItemRow2.Controls.Add(this.lblPurity);
            this.flpAddItemRow2.Controls.Add(this.cmbPurity);
            this.flpAddItemRow2.Controls.Add(this.lblSurvey);
            this.flpAddItemRow2.Controls.Add(this.cmbSurvey);
            this.flpAddItemRow2.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpAddItemRow2.Location = new System.Drawing.Point(3, 48);
            this.flpAddItemRow2.Name = "flpAddItemRow2";
            this.flpAddItemRow2.Size = new System.Drawing.Size(656, 26);
            // 
            // lblQuantity
            // 
            this.lblQuantity.AutoSize = true;
            this.lblQuantity.Location = new System.Drawing.Point(3, 5);
            this.lblQuantity.Name = "lblQuantity";
            this.lblQuantity.Size = new System.Drawing.Size(26, 13);
            this.lblQuantity.Text = "Qty:";
            this.lblQuantity.Anchor = System.Windows.Forms.AnchorStyles.Left;
            // 
            // txtQuantity
            // 
            this.txtQuantity.Location = new System.Drawing.Point(35, 3);
            this.txtQuantity.Name = "txtQuantity";
            this.txtQuantity.Size = new System.Drawing.Size(60, 20);
            this.txtQuantity.Text = "1";
            // 
            // lblRecipient
            // 
            this.lblRecipient.AutoSize = true;
            this.lblRecipient.Location = new System.Drawing.Point(101, 5);
            this.lblRecipient.Name = "lblRecipient";
            this.lblRecipient.Size = new System.Drawing.Size(55, 13);
            this.lblRecipient.Text = "Recipient:";
            this.lblRecipient.Anchor = System.Windows.Forms.AnchorStyles.Left;
            // 
            // txtRecipient
            // 
            this.txtRecipient.Location = new System.Drawing.Point(162, 3);
            this.txtRecipient.Name = "txtRecipient";
            this.txtRecipient.Size = new System.Drawing.Size(150, 20);
            // 
            // lblTargetDuration
            // 
            this.lblTargetDuration.AutoSize = true;
            this.lblTargetDuration.Text = "Duration:";
            this.lblTargetDuration.Name = "lblTargetDuration";
            this.lblTargetDuration.Anchor = System.Windows.Forms.AnchorStyles.Left;
            // 
            // txtTargetDuration
            // 
            this.txtTargetDuration.Name = "txtTargetDuration";
            this.txtTargetDuration.Size = new System.Drawing.Size(120, 20);
            this.txtTargetDuration.Text = "2d 0h 0m 0s";
            // 
            // lblResource
            // 
            this.lblResource.AutoSize = true;
            this.lblResource.Location = new System.Drawing.Point(318, 5);
            this.lblResource.Name = "lblResource";
            this.lblResource.Size = new System.Drawing.Size(56, 13);
            this.lblResource.Text = "Resource:";
            this.lblResource.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblResource.Visible = false;
            // 
            // cmbResource
            // 
            this.cmbResource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbResource.Location = new System.Drawing.Point(380, 3);
            this.cmbResource.Name = "cmbResource";
            this.cmbResource.Size = new System.Drawing.Size(180, 21);
            this.cmbResource.Visible = false;
            // 
            // lblPurity
            // 
            this.lblPurity.AutoSize = true;
            this.lblPurity.Location = new System.Drawing.Point(566, 5);
            this.lblPurity.Name = "lblPurity";
            this.lblPurity.Size = new System.Drawing.Size(36, 13);
            this.lblPurity.Text = "Purity:";
            this.lblPurity.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblPurity.Visible = false;
            // 
            // cmbPurity
            // 
            this.cmbPurity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPurity.Location = new System.Drawing.Point(608, 3);
            this.cmbPurity.Name = "cmbPurity";
            this.cmbPurity.Size = new System.Drawing.Size(100, 21);
            this.cmbPurity.Visible = false;
            // 
            // lblSurvey
            // 
            this.lblSurvey.AutoSize = true;
            this.lblSurvey.Location = new System.Drawing.Point(318, 5);
            this.lblSurvey.Name = "lblSurvey";
            this.lblSurvey.Size = new System.Drawing.Size(43, 13);
            this.lblSurvey.Text = "Survey:";
            this.lblSurvey.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSurvey.Visible = false;
            // 
            // cmbSurvey
            // 
            this.cmbSurvey.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSurvey.Location = new System.Drawing.Point(367, 3);
            this.cmbSurvey.Name = "cmbSurvey";
            this.cmbSurvey.Size = new System.Drawing.Size(200, 21);
            this.cmbSurvey.Visible = false;
            // 
            // flpAddItemRow3
            // 
            this.flpAddItemRow3.AutoSize = true;
            this.flpAddItemRow3.Controls.Add(this.cmdAddItem);
            this.flpAddItemRow3.Controls.Add(this.cmdQueueCalc);
            this.flpAddItemRow3.Controls.Add(this.cmdAllocate);
            this.flpAddItemRow3.Controls.Add(this.cmdGenerateDelivery);
            this.flpAddItemRow3.Controls.Add(this.cmdAutoAssign);
            this.flpAddItemRow3.Controls.Add(this.cmdStartManufacturing);
            this.flpAddItemRow3.Controls.Add(this.cmdStartAllReady);
            this.flpAddItemRow3.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpAddItemRow3.Location = new System.Drawing.Point(3, 80);
            this.flpAddItemRow3.Name = "flpAddItemRow3";
            this.flpAddItemRow3.Size = new System.Drawing.Size(656, 29);
            // 
            // cmdAddItem
            // 
            this.cmdAddItem.Location = new System.Drawing.Point(3, 3);
            this.cmdAddItem.Name = "cmdAddItem";
            this.cmdAddItem.Size = new System.Drawing.Size(75, 23);
            this.cmdAddItem.Text = "Add Item";
            this.cmdAddItem.UseVisualStyleBackColor = true;
            // 
            // cmdQueueCalc
            // 
            this.cmdQueueCalc.Location = new System.Drawing.Point(84, 3);
            this.cmdQueueCalc.Name = "cmdQueueCalc";
            this.cmdQueueCalc.Size = new System.Drawing.Size(85, 23);
            this.cmdQueueCalc.Text = "Queue Calc";
            this.cmdQueueCalc.UseVisualStyleBackColor = true;
            // 
            // cmdAllocate
            // 
            this.cmdAllocate.Location = new System.Drawing.Point(175, 3);
            this.cmdAllocate.Name = "cmdAllocate";
            this.cmdAllocate.Size = new System.Drawing.Size(75, 23);
            this.cmdAllocate.Text = "Allocate";
            this.cmdAllocate.UseVisualStyleBackColor = true;
            // 
            // cmdGenerateDelivery
            // 
            this.cmdGenerateDelivery.Location = new System.Drawing.Point(256, 3);
            this.cmdGenerateDelivery.Name = "cmdGenerateDelivery";
            this.cmdGenerateDelivery.Size = new System.Drawing.Size(130, 23);
            this.cmdGenerateDelivery.Text = "Generate Delivery \u25BC";
            this.cmdGenerateDelivery.UseVisualStyleBackColor = true;
            // 
            // cmdAutoAssign
            // 
            this.cmdAutoAssign.Location = new System.Drawing.Point(392, 3);
            this.cmdAutoAssign.Name = "cmdAutoAssign";
            this.cmdAutoAssign.Size = new System.Drawing.Size(90, 23);
            this.cmdAutoAssign.Text = "Auto-Assign";
            this.cmdAutoAssign.UseVisualStyleBackColor = true;
            // 
            // cmdStartManufacturing
            // 
            this.cmdStartManufacturing.Location = new System.Drawing.Point(488, 3);
            this.cmdStartManufacturing.Name = "cmdStartManufacturing";
            this.cmdStartManufacturing.Size = new System.Drawing.Size(130, 23);
            this.cmdStartManufacturing.Text = "Start Manufacturing";
            this.cmdStartManufacturing.UseVisualStyleBackColor = true;
            this.cmdStartManufacturing.Enabled = false;
            // 
            // cmdStartAllReady
            // 
            this.cmdStartAllReady.Location = new System.Drawing.Point(624, 3);
            this.cmdStartAllReady.Name = "cmdStartAllReady";
            this.cmdStartAllReady.Size = new System.Drawing.Size(110, 23);
            this.cmdStartAllReady.Text = "Start All Ready";
            this.cmdStartAllReady.UseVisualStyleBackColor = true;
            this.cmdStartAllReady.Enabled = false;
            // 
            // cmsGenerateDelivery
            // 
            this.cmsGenerateDelivery.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiResourceDelivery,
            this.tsmiConsolidatedDelivery,
            this.tsmiFlatpackDelivery});
            this.cmsGenerateDelivery.Name = "cmsGenerateDelivery";
            this.cmsGenerateDelivery.Size = new System.Drawing.Size(250, 70);
            // 
            // tsmiResourceDelivery
            // 
            this.tsmiResourceDelivery.Name = "tsmiResourceDelivery";
            this.tsmiResourceDelivery.Size = new System.Drawing.Size(249, 22);
            this.tsmiResourceDelivery.Text = "Resource Delivery (This Plan)";
            // 
            // tsmiConsolidatedDelivery
            // 
            this.tsmiConsolidatedDelivery.Name = "tsmiConsolidatedDelivery";
            this.tsmiConsolidatedDelivery.Size = new System.Drawing.Size(249, 22);
            this.tsmiConsolidatedDelivery.Text = "Consolidated Resource Delivery";
            // 
            // tsmiFlatpackDelivery
            // 
            this.tsmiFlatpackDelivery.Name = "tsmiFlatpackDelivery";
            this.tsmiFlatpackDelivery.Size = new System.Drawing.Size(249, 22);
            this.tsmiFlatpackDelivery.Text = "Flatpack Delivery";
            // 
            // cmsBuildItems
            // 
            this.cmsBuildItems.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiSetDependency,
            this.tsmiClearDependency,
            this.tsmiStartManufacturing,
            this.tsmiStartAllReady});
            this.cmsBuildItems.Name = "cmsBuildItems";
            this.cmsBuildItems.Size = new System.Drawing.Size(200, 92);
            // 
            // tsmiSetDependency
            // 
            this.tsmiSetDependency.Name = "tsmiSetDependency";
            this.tsmiSetDependency.Size = new System.Drawing.Size(199, 22);
            this.tsmiSetDependency.Text = "Set Dependency...";
            // 
            // tsmiClearDependency
            // 
            this.tsmiClearDependency.Name = "tsmiClearDependency";
            this.tsmiClearDependency.Size = new System.Drawing.Size(199, 22);
            this.tsmiClearDependency.Text = "Clear Dependency";
            // 
            // tsmiStartManufacturing
            // 
            this.tsmiStartManufacturing.Name = "tsmiStartManufacturing";
            this.tsmiStartManufacturing.Size = new System.Drawing.Size(199, 22);
            this.tsmiStartManufacturing.Text = "Start Manufacturing";
            // 
            // tsmiStartAllReady
            // 
            this.tsmiStartAllReady.Name = "tsmiStartAllReady";
            this.tsmiStartAllReady.Size = new System.Drawing.Size(199, 22);
            this.tsmiStartAllReady.Text = "Start All Ready";
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
            this.flpAddItem.ResumeLayout(false);
            this.flpAddItem.PerformLayout();
            this.flpAddItemRow1.ResumeLayout(false);
            this.flpAddItemRow1.PerformLayout();
            this.flpAddItemRow2.ResumeLayout(false);
            this.flpAddItemRow2.PerformLayout();
            this.flpAddItemRow3.ResumeLayout(false);
            this.flpAddItemRow3.PerformLayout();
            this.cmsGenerateDelivery.ResumeLayout(false);
            this.cmsBuildItems.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvBuildItems)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvShortfalls)).EndInit();
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
        private System.Windows.Forms.Label lblShortfallHeader;
        private System.Windows.Forms.Label lblShortfallStatus;
        private System.Windows.Forms.DataGridView dgvShortfalls;
        private System.Windows.Forms.DataGridViewTextBoxColumn colShortfallResource;
        private System.Windows.Forms.DataGridViewTextBoxColumn colShortfallRequired;
        private System.Windows.Forms.DataGridViewTextBoxColumn colShortfallAvailable;
        private System.Windows.Forms.DataGridViewTextBoxColumn colShortfallDeficit;
        private System.Windows.Forms.FlowLayoutPanel flpAddItem;
        private System.Windows.Forms.Label lblAddItemHeader;
        private System.Windows.Forms.FlowLayoutPanel flpAddItemRow1;
        private System.Windows.Forms.Label lblItemType;
        private System.Windows.Forms.ComboBox cmbItemType;
        private System.Windows.Forms.Label lblItemFilter;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtItemFilter;
        private System.Windows.Forms.Label lblItem;
        private System.Windows.Forms.ComboBox cmbItem;
        private System.Windows.Forms.FlowLayoutPanel flpAddItemRow2;
        private System.Windows.Forms.Label lblQuantity;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtQuantity;
        private System.Windows.Forms.Label lblRecipient;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtRecipient;
        private System.Windows.Forms.Label lblTargetDuration;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtTargetDuration;
        private System.Windows.Forms.FlowLayoutPanel flpAddItemRow3;
        private System.Windows.Forms.Button cmdAddItem;
        private System.Windows.Forms.Button cmdQueueCalc;
        private System.Windows.Forms.Button cmdAllocate;
        private System.Windows.Forms.Button cmdGenerateDelivery;
        private System.Windows.Forms.Button cmdAutoAssign;
        private System.Windows.Forms.ContextMenuStrip cmsGenerateDelivery;
        private System.Windows.Forms.ToolStripMenuItem tsmiResourceDelivery;
        private System.Windows.Forms.ToolStripMenuItem tsmiConsolidatedDelivery;
        private System.Windows.Forms.ToolStripMenuItem tsmiFlatpackDelivery;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSequence;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDependsOn;
        private System.Windows.Forms.Label lblResource;
        private System.Windows.Forms.ComboBox cmbResource;
        private System.Windows.Forms.Label lblPurity;
        private System.Windows.Forms.ComboBox cmbPurity;
        private System.Windows.Forms.Label lblSurvey;
        private System.Windows.Forms.ComboBox cmbSurvey;
        private System.Windows.Forms.ContextMenuStrip cmsBuildItems;
        private System.Windows.Forms.ToolStripMenuItem tsmiSetDependency;
        private System.Windows.Forms.ToolStripMenuItem tsmiClearDependency;
        private System.Windows.Forms.Button cmdStartManufacturing;
        private System.Windows.Forms.Button cmdStartAllReady;
        private System.Windows.Forms.ToolStripMenuItem tsmiStartManufacturing;
        private System.Windows.Forms.ToolStripMenuItem tsmiStartAllReady;
        private System.Windows.Forms.Label lblStatusSummary;
    }
}
