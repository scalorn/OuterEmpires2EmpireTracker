namespace OE2EmpireTracker.Forms.ColonyV2
{
    partial class FormColonyV2
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.lvwColonies = new System.Windows.Forms.ListView();
            this.txtColonyFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpColonyData = new System.Windows.Forms.FlowLayoutPanel();
            this.flpIdentity = new System.Windows.Forms.FlowLayoutPanel();
            this.flpPlanetName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblPlanetName = new System.Windows.Forms.Label();
            this.txtPlanetName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpColonyName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblColonyName = new System.Windows.Forms.Label();
            this.txtColonyName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpSystemName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSystemName = new System.Windows.Forms.Label();
            this.txtSystemName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.tabDetailedData = new System.Windows.Forms.TabControl();
            this.tabPAdministration = new System.Windows.Forms.TabPage();
            this.tabPStructures = new System.Windows.Forms.TabPage();
            this.splitStructures = new System.Windows.Forms.SplitContainer();
            this.lvwStructureTypes = new System.Windows.Forms.ListView();
            this.rtbStatusSummary = new System.Windows.Forms.RichTextBox();
            this.flpStructures = new System.Windows.Forms.FlowLayoutPanel();
            this.flpAddStructure = new System.Windows.Forms.FlowLayoutPanel();

            this.cmbFlatpacks = new OE2EmpireTracker.Controls.FilteredTextComboSet();
            this.cmdAddFlatpack = new System.Windows.Forms.Button();
            this.tabPWorkers = new System.Windows.Forms.TabPage();
            this.dgvCommodityRequests = new DataEntryGridView();
            this.colCRName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCRAmount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCRFulfilled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colCRNeedBy = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpAddCommodityRequest = new System.Windows.Forms.FlowLayoutPanel();
            this.txtCommodityRequestFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmbCommodityRequest = new System.Windows.Forms.ComboBox();
            this.txtCommodityRequestQty = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.txtCommodityRequestNeedBy = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmdAddCommodityRequest = new System.Windows.Forms.Button();
            this.tabPWarehousing = new System.Windows.Forms.TabPage();
            this.tabPOverflow = new System.Windows.Forms.TabPage();
            this.dgvOverflowRules = new System.Windows.Forms.DataGridView();
            this.colOverflowRuleType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOverflowResource = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOverflowPurity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOverflowThreshold = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOverflowCurrent = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOverflowDest = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOverflowRoute = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOverflowActive = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.flpOverflowAdd = new System.Windows.Forms.FlowLayoutPanel();
            this.lblOverflowRuleType = new System.Windows.Forms.Label();
            this.cmbOverflowRuleType = new System.Windows.Forms.ComboBox();
            this.lblOverflowResource = new System.Windows.Forms.Label();
            this.cmbOverflowResource = new OE2EmpireTracker.Controls.FilteredTextComboSet();
            this.lblOverflowPurity = new System.Windows.Forms.Label();
            this.cmbOverflowPurity = new System.Windows.Forms.ComboBox();
            this.lblOverflowThreshold = new System.Windows.Forms.Label();
            this.txtOverflowThreshold = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpOverflowAdd2 = new System.Windows.Forms.FlowLayoutPanel();
            this.lblOverflowDestType = new System.Windows.Forms.Label();
            this.cmbOverflowDestType = new System.Windows.Forms.ComboBox();
            this.lblOverflowDest = new System.Windows.Forms.Label();
            this.cmbOverflowDest = new OE2EmpireTracker.Controls.FilteredTextComboSet();
            this.lblOverflowRoute = new System.Windows.Forms.Label();
            this.cmbOverflowRoute = new OE2EmpireTracker.Controls.FilteredTextComboSet();
            this.flpOverflowButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdAddOverflowRule = new System.Windows.Forms.Button();
            this.cmdRemoveOverflowRule = new System.Windows.Forms.Button();
            this.cmsItems = new System.Windows.Forms.ContextMenuStrip();
            this.tsmiAddItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiRemoveItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsCommodityRequests = new System.Windows.Forms.ContextMenuStrip();
            this.tsmiAddCommodityRequest = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiRemoveCommodityRequest = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsOverflowRules = new System.Windows.Forms.ContextMenuStrip();
            this.tsmiAddOverflowRule = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiRemoveOverflowRule = new System.Windows.Forms.ToolStripMenuItem();
            this.dgvItems = new DataEntryGridView();
            this.colItemType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colItemName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colItemLocked = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colItemAmount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpAddItem = new System.Windows.Forms.FlowLayoutPanel();
            this.cmbItemType = new System.Windows.Forms.ComboBox();
            this.cmbItem = new OE2EmpireTracker.Controls.FilteredTextComboSet();
            this.cmbPurity = new System.Windows.Forms.ComboBox();
            this.txtQuantity = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmdAddItem = new System.Windows.Forms.Button();
            this.flpCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdNew = new System.Windows.Forms.Button();
            this.cmdSave = new System.Windows.Forms.Button();
            this.cmdDelete = new System.Windows.Forms.Button();
            this.cmdImportColony = new System.Windows.Forms.Button();
            this.cmdImportClipboard = new System.Windows.Forms.Button();
            this.rtbAdminReport = new System.Windows.Forms.RichTextBox();
            this.flpAdminCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdBootstrap = new System.Windows.Forms.Button();
            this.cmdOptimize = new System.Windows.Forms.Button();
            this.cmdGenerateBuildPlan = new System.Windows.Forms.Button();
            this.cmdSync = new System.Windows.Forms.Button();
            this.toolTipSync = new System.Windows.Forms.ToolTip();
            this.components = new System.ComponentModel.Container();
            this.timerAdminRefresh = new System.Windows.Forms.Timer(this.components);
            this.timerSyncCooldown = new System.Windows.Forms.Timer(this.components);
            this.timerSyncStatus = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.flpColonyData.SuspendLayout();
            this.flpIdentity.SuspendLayout();
            this.flpPlanetName.SuspendLayout();
            this.flpColonyName.SuspendLayout();
            this.flpSystemName.SuspendLayout();
            this.tabDetailedData.SuspendLayout();
            this.tabPAdministration.SuspendLayout();
            this.flpAdminCommands.SuspendLayout();
            this.tabPStructures.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitStructures)).BeginInit();
            this.splitStructures.Panel1.SuspendLayout();
            this.splitStructures.Panel2.SuspendLayout();
            this.splitStructures.SuspendLayout();
            this.flpAddStructure.SuspendLayout();
            this.tabPWorkers.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCommodityRequests)).BeginInit();
            this.flpAddCommodityRequest.SuspendLayout();
            this.tabPWarehousing.SuspendLayout();
            this.tabPOverflow.SuspendLayout();
            this.flpOverflowAdd.SuspendLayout();
            this.flpOverflowAdd2.SuspendLayout();
            this.flpOverflowButtons.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOverflowRules)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvItems)).BeginInit();
            this.flpAddItem.SuspendLayout();
            this.flpCommands.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitMain
            // 
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitMain.Location = new System.Drawing.Point(0, 0);
            this.splitMain.Name = "splitMain";
            this.splitMain.SplitterDistance = 480;
            this.splitMain.SplitterWidth = 8;
            this.splitMain.Panel1MinSize = 150;
            this.splitMain.BackColor = System.Drawing.SystemColors.ControlDark;
            this.splitMain.Paint += new System.Windows.Forms.PaintEventHandler(this.SplitMain_Paint);
            this.splitMain.Size = new System.Drawing.Size(1225, 500);
            this.splitMain.TabIndex = 0;
            // 
            // splitMain.Panel1 — left colony list
            // 
            this.splitMain.Panel1.Controls.Add(this.lvwColonies);
            this.splitMain.Panel1.Controls.Add(this.txtColonyFilter);
            this.splitMain.Panel1.Padding = new System.Windows.Forms.Padding(2);
            this.splitMain.Panel1.BackColor = System.Drawing.SystemColors.Control;
            this.splitMain.Panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            // 
            // splitMain.Panel2 — right colony data
            // 
            this.splitMain.Panel2.Controls.Add(this.flpColonyData);
            this.splitMain.Panel2.BackColor = System.Drawing.SystemColors.Control;
            // 
            // txtColonyFilter
            // 
            this.txtColonyFilter.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtColonyFilter.Location = new System.Drawing.Point(2, 2);
            this.txtColonyFilter.Name = "txtColonyFilter";
            this.txtColonyFilter.Size = new System.Drawing.Size(236, 20);
            this.txtColonyFilter.TabIndex = 0;
            // 
            // lvwColonies
            // 
            this.lvwColonies.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvwColonies.FullRowSelect = true;
            this.lvwColonies.HideSelection = false;
            this.lvwColonies.Location = new System.Drawing.Point(2, 22);
            this.lvwColonies.MultiSelect = false;
            this.lvwColonies.Name = "lvwColonies";
            this.lvwColonies.Size = new System.Drawing.Size(236, 476);
            this.lvwColonies.TabIndex = 1;
            this.lvwColonies.UseCompatibleStateImageBehavior = false;
            this.lvwColonies.View = System.Windows.Forms.View.Details;
            // 
            // flpColonyData
            // 
            this.flpColonyData.Controls.Add(this.flpIdentity);
            this.flpColonyData.Controls.Add(this.tabDetailedData);
            this.flpColonyData.Controls.Add(this.flpCommands);
            this.flpColonyData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpColonyData.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpColonyData.Location = new System.Drawing.Point(0, 0);
            this.flpColonyData.Name = "flpColonyData";
            this.flpColonyData.Size = new System.Drawing.Size(981, 500);
            this.flpColonyData.TabIndex = 0;
            this.flpColonyData.WrapContents = false;
            this.flpColonyData.Layout += new System.Windows.Forms.LayoutEventHandler(this.FlpColonyData_Layout);
            // 
            // flpIdentity
            // 
            this.flpIdentity.Controls.Add(this.flpPlanetName);
            this.flpIdentity.Controls.Add(this.flpColonyName);
            this.flpIdentity.Controls.Add(this.flpSystemName);
            this.flpIdentity.Controls.Add(this.cmdSync);
            this.flpIdentity.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpIdentity.Location = new System.Drawing.Point(2, 2);
            this.flpIdentity.Margin = new System.Windows.Forms.Padding(2);
            this.flpIdentity.Name = "flpIdentity";
            this.flpIdentity.Size = new System.Drawing.Size(800, 92);
            this.flpIdentity.TabIndex = 0;
            this.flpIdentity.WrapContents = false;
            // 
            // flpPlanetName
            // 
            this.flpPlanetName.Controls.Add(this.lblPlanetName);
            this.flpPlanetName.Controls.Add(this.txtPlanetName);
            this.flpPlanetName.Location = new System.Drawing.Point(2, 2);
            this.flpPlanetName.Margin = new System.Windows.Forms.Padding(2);
            this.flpPlanetName.Name = "flpPlanetName";
            this.flpPlanetName.Size = new System.Drawing.Size(364, 26);
            this.flpPlanetName.TabIndex = 0;
            this.flpPlanetName.WrapContents = false;
            // 
            // lblPlanetName
            // 
            this.lblPlanetName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblPlanetName.Location = new System.Drawing.Point(2, 4);
            this.lblPlanetName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblPlanetName.Name = "lblPlanetName";
            this.lblPlanetName.Size = new System.Drawing.Size(100, 17);
            this.lblPlanetName.TabIndex = 0;
            this.lblPlanetName.Text = "Planet Name";
            this.lblPlanetName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtPlanetName
            // 
            this.txtPlanetName.Location = new System.Drawing.Point(107, 3);
            this.txtPlanetName.Name = "txtPlanetName";
            this.txtPlanetName.Size = new System.Drawing.Size(254, 20);
            this.txtPlanetName.TabIndex = 1;
            // 
            // flpColonyName
            // 
            this.flpColonyName.Controls.Add(this.lblColonyName);
            this.flpColonyName.Controls.Add(this.txtColonyName);
            this.flpColonyName.Location = new System.Drawing.Point(2, 32);
            this.flpColonyName.Margin = new System.Windows.Forms.Padding(2);
            this.flpColonyName.Name = "flpColonyName";
            this.flpColonyName.Size = new System.Drawing.Size(364, 26);
            this.flpColonyName.TabIndex = 1;
            this.flpColonyName.WrapContents = false;
            // 
            // lblColonyName
            // 
            this.lblColonyName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblColonyName.Location = new System.Drawing.Point(2, 4);
            this.lblColonyName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblColonyName.Name = "lblColonyName";
            this.lblColonyName.Size = new System.Drawing.Size(100, 17);
            this.lblColonyName.TabIndex = 0;
            this.lblColonyName.Text = "Colony Name";
            this.lblColonyName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtColonyName
            // 
            this.txtColonyName.Location = new System.Drawing.Point(107, 3);
            this.txtColonyName.Name = "txtColonyName";
            this.txtColonyName.Size = new System.Drawing.Size(254, 20);
            this.txtColonyName.TabIndex = 1;
            // 
            // flpSystemName
            // 
            this.flpSystemName.Controls.Add(this.lblSystemName);
            this.flpSystemName.Controls.Add(this.txtSystemName);
            this.flpSystemName.Location = new System.Drawing.Point(2, 62);
            this.flpSystemName.Margin = new System.Windows.Forms.Padding(2);
            this.flpSystemName.Name = "flpSystemName";
            this.flpSystemName.Size = new System.Drawing.Size(364, 26);
            this.flpSystemName.TabIndex = 2;
            this.flpSystemName.WrapContents = false;
            // 
            // lblSystemName
            // 
            this.lblSystemName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSystemName.Location = new System.Drawing.Point(2, 4);
            this.lblSystemName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblSystemName.Name = "lblSystemName";
            this.lblSystemName.Size = new System.Drawing.Size(100, 17);
            this.lblSystemName.TabIndex = 0;
            this.lblSystemName.Text = "System";
            this.lblSystemName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtSystemName
            // 
            this.txtSystemName.Location = new System.Drawing.Point(107, 3);
            this.txtSystemName.Name = "txtSystemName";
            this.txtSystemName.Size = new System.Drawing.Size(254, 20);
            this.txtSystemName.TabIndex = 1;
            // 
            // tabDetailedData
            // 
            this.tabDetailedData.Controls.Add(this.tabPAdministration);
            this.tabDetailedData.Controls.Add(this.tabPStructures);
            this.tabDetailedData.Controls.Add(this.tabPWorkers);
            this.tabDetailedData.Controls.Add(this.tabPWarehousing);
            this.tabDetailedData.Controls.Add(this.tabPOverflow);
            this.tabDetailedData.Location = new System.Drawing.Point(2, 98);
            this.tabDetailedData.Margin = new System.Windows.Forms.Padding(2);
            this.tabDetailedData.Multiline = true;
            this.tabDetailedData.Name = "tabDetailedData";
            this.tabDetailedData.SelectedIndex = 0;
            this.tabDetailedData.Size = new System.Drawing.Size(800, 350);
            this.tabDetailedData.TabIndex = 1;
            // 
            // tabPAdministration
            // 
            this.tabPAdministration.Controls.Add(this.rtbAdminReport);
            this.tabPAdministration.Controls.Add(this.flpAdminCommands);
            this.tabPAdministration.Location = new System.Drawing.Point(4, 22);
            this.tabPAdministration.Margin = new System.Windows.Forms.Padding(2);
            this.tabPAdministration.Name = "tabPAdministration";
            this.tabPAdministration.Padding = new System.Windows.Forms.Padding(2);
            this.tabPAdministration.Size = new System.Drawing.Size(792, 324);
            this.tabPAdministration.TabIndex = 0;
            this.tabPAdministration.Text = "Administration";
            this.tabPAdministration.UseVisualStyleBackColor = true;
            // 
            // tabPStructures
            // 
            this.tabPStructures.Controls.Add(this.splitStructures);
            this.tabPStructures.Controls.Add(this.rtbStatusSummary);
            this.tabPStructures.Controls.Add(this.flpAddStructure);
            this.tabPStructures.Location = new System.Drawing.Point(4, 22);
            this.tabPStructures.Margin = new System.Windows.Forms.Padding(2);
            this.tabPStructures.Name = "tabPStructures";
            this.tabPStructures.Padding = new System.Windows.Forms.Padding(2);
            this.tabPStructures.Size = new System.Drawing.Size(792, 324);
            this.tabPStructures.TabIndex = 1;
            this.tabPStructures.Text = "Structures";
            this.tabPStructures.UseVisualStyleBackColor = true;
            this.tabPStructures.Layout += new System.Windows.Forms.LayoutEventHandler(this.TabPStructures_Layout);
            // 
            // splitStructures
            // 
            this.splitStructures.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitStructures.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitStructures.Location = new System.Drawing.Point(2, 38);
            this.splitStructures.Name = "splitStructures";
            this.splitStructures.SplitterDistance = 140;
            this.splitStructures.Size = new System.Drawing.Size(788, 254);
            this.splitStructures.TabIndex = 3;
            // 
            // splitStructures.Panel1 — structure type filter
            // 
            this.splitStructures.Panel1.Controls.Add(this.lvwStructureTypes);
            // 
            // splitStructures.Panel2 — structure controls
            // 
            this.splitStructures.Panel2.Controls.Add(this.flpStructures);
            // 
            // lvwStructureTypes
            // 
            this.lvwStructureTypes.CheckBoxes = true;
            this.lvwStructureTypes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvwStructureTypes.FullRowSelect = true;
            this.lvwStructureTypes.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.lvwStructureTypes.HideSelection = false;
            this.lvwStructureTypes.Location = new System.Drawing.Point(0, 0);
            this.lvwStructureTypes.MultiSelect = true;
            this.lvwStructureTypes.Name = "lvwStructureTypes";
            this.lvwStructureTypes.Size = new System.Drawing.Size(140, 254);
            this.lvwStructureTypes.TabIndex = 0;
            this.lvwStructureTypes.UseCompatibleStateImageBehavior = false;
            this.lvwStructureTypes.View = System.Windows.Forms.View.Details;
            this.lvwStructureTypes.Columns.Add("Type", 136);
            // 
            // rtbStatusSummary
            // 
            this.rtbStatusSummary.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbStatusSummary.Dock = System.Windows.Forms.DockStyle.Top;
            this.rtbStatusSummary.Location = new System.Drawing.Point(2, 2);
            this.rtbStatusSummary.Name = "rtbStatusSummary";
            this.rtbStatusSummary.ReadOnly = true;
            this.rtbStatusSummary.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.rtbStatusSummary.Size = new System.Drawing.Size(788, 36);
            this.rtbStatusSummary.TabIndex = 0;
            this.rtbStatusSummary.Text = "";
            this.rtbStatusSummary.WordWrap = false;
            // 
            // flpStructures
            // 
            this.flpStructures.AutoScroll = true;
            this.flpStructures.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpStructures.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpStructures.Location = new System.Drawing.Point(0, 0);
            this.flpStructures.Name = "flpStructures";
            this.flpStructures.Size = new System.Drawing.Size(644, 254);
            this.flpStructures.TabIndex = 0;
            this.flpStructures.WrapContents = false;
            // 
            // flpAddStructure
            // 
            this.flpAddStructure.Controls.Add(this.cmbFlatpacks);
            this.flpAddStructure.Controls.Add(this.cmdAddFlatpack);
            this.flpAddStructure.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flpAddStructure.Location = new System.Drawing.Point(2, 292);
            this.flpAddStructure.Name = "flpAddStructure";
            this.flpAddStructure.Size = new System.Drawing.Size(788, 30);
            this.flpAddStructure.TabIndex = 2;
            this.flpAddStructure.WrapContents = false;
            // 
            // cmbFlatpacks
            // 
            this.cmbFlatpacks.Location = new System.Drawing.Point(3, 3);
            this.cmbFlatpacks.Name = "cmbFlatpacks";
            this.cmbFlatpacks.Size = new System.Drawing.Size(450, 21);
            this.cmbFlatpacks.TabIndex = 0;
            // 
            // cmdAddFlatpack
            // 
            this.cmdAddFlatpack.Location = new System.Drawing.Point(459, 3);
            this.cmdAddFlatpack.Name = "cmdAddFlatpack";
            this.cmdAddFlatpack.Size = new System.Drawing.Size(50, 23);
            this.cmdAddFlatpack.TabIndex = 1;
            this.cmdAddFlatpack.Text = "Add";
            this.cmdAddFlatpack.UseVisualStyleBackColor = true;
            // 
            // rtbAdminReport
            // 
            this.rtbAdminReport.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbAdminReport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbAdminReport.Location = new System.Drawing.Point(2, 2);
            this.rtbAdminReport.Name = "rtbAdminReport";
            this.rtbAdminReport.ReadOnly = true;
            this.rtbAdminReport.Size = new System.Drawing.Size(788, 291);
            this.rtbAdminReport.TabIndex = 0;
            this.rtbAdminReport.Text = "";
            // 
            // flpAdminCommands
            // 
            this.flpAdminCommands.Controls.Add(this.cmdBootstrap);
            this.flpAdminCommands.Controls.Add(this.cmdOptimize);
            this.flpAdminCommands.Controls.Add(this.cmdGenerateBuildPlan);
            this.flpAdminCommands.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flpAdminCommands.Location = new System.Drawing.Point(2, 293);
            this.flpAdminCommands.Margin = new System.Windows.Forms.Padding(2);
            this.flpAdminCommands.Name = "flpAdminCommands";
            this.flpAdminCommands.Size = new System.Drawing.Size(788, 29);
            this.flpAdminCommands.TabIndex = 1;
            this.flpAdminCommands.WrapContents = false;
            // 
            // cmdBootstrap
            // 
            this.cmdBootstrap.AutoSize = true;
            this.cmdBootstrap.Location = new System.Drawing.Point(3, 3);
            this.cmdBootstrap.Name = "cmdBootstrap";
            this.cmdBootstrap.Size = new System.Drawing.Size(129, 23);
            this.cmdBootstrap.TabIndex = 0;
            this.cmdBootstrap.Text = "Bootstrap From Surveys";
            this.cmdBootstrap.UseVisualStyleBackColor = true;
            this.cmdBootstrap.Click += new System.EventHandler(this.CmdBootstrap_Click);
            // 
            // cmdOptimize
            // 
            this.cmdOptimize.AutoSize = true;
            this.cmdOptimize.Location = new System.Drawing.Point(138, 3);
            this.cmdOptimize.Name = "cmdOptimize";
            this.cmdOptimize.Size = new System.Drawing.Size(112, 23);
            this.cmdOptimize.TabIndex = 1;
            this.cmdOptimize.Text = "Optimize Build Order";
            this.cmdOptimize.UseVisualStyleBackColor = true;
            this.cmdOptimize.Click += new System.EventHandler(this.CmdOptimize_Click);
            // 
            // cmdGenerateBuildPlan
            // 
            this.cmdGenerateBuildPlan.AutoSize = true;
            this.cmdGenerateBuildPlan.Location = new System.Drawing.Point(256, 3);
            this.cmdGenerateBuildPlan.Name = "cmdGenerateBuildPlan";
            this.cmdGenerateBuildPlan.Size = new System.Drawing.Size(120, 23);
            this.cmdGenerateBuildPlan.TabIndex = 2;
            this.cmdGenerateBuildPlan.Text = "Generate Build Plan";
            this.cmdGenerateBuildPlan.UseVisualStyleBackColor = true;
            this.cmdGenerateBuildPlan.Click += new System.EventHandler(this.CmdGenerateBuildPlan_Click);
            // 
            // timerAdminRefresh
            // 
            this.timerAdminRefresh.Interval = 60000;
            // 
            // timerSyncCooldown
            // 
            this.timerSyncCooldown.Interval = 10000;
            // 
            // timerSyncStatus
            // 
            this.timerSyncStatus.Interval = 2000;
            // 
            // cmdSync
            // 
            this.cmdSync.Enabled = false;
            this.cmdSync.Location = new System.Drawing.Point(3, 3);
            this.cmdSync.Margin = new System.Windows.Forms.Padding(3);
            this.cmdSync.Name = "cmdSync";
            this.cmdSync.Size = new System.Drawing.Size(75, 25);
            this.cmdSync.TabIndex = 3;
            this.cmdSync.Text = "Sync";
            this.cmdSync.UseVisualStyleBackColor = true;
            this.toolTipSync.SetToolTip(this.cmdSync, "Sync buildings, warehouse, and workers from game API");
            // 
            // tabPWorkers
            // 
            this.tabPWorkers.Controls.Add(this.dgvCommodityRequests);
            this.tabPWorkers.Controls.Add(this.flpAddCommodityRequest);
            this.tabPWorkers.Location = new System.Drawing.Point(4, 22);
            this.tabPWorkers.Name = "tabPWorkers";
            this.tabPWorkers.Padding = new System.Windows.Forms.Padding(2);
            this.tabPWorkers.Size = new System.Drawing.Size(792, 324);
            this.tabPWorkers.TabIndex = 2;
            this.tabPWorkers.Text = "Workers";
            this.tabPWorkers.UseVisualStyleBackColor = true;
            // 
            // dgvCommodityRequests
            // 
            this.dgvCommodityRequests.AllowUserToAddRows = false;
            this.dgvCommodityRequests.AllowUserToDeleteRows = false;
            this.dgvCommodityRequests.AllowUserToOrderColumns = true;
            this.dgvCommodityRequests.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvCommodityRequests.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colCRName,
            this.colCRAmount,
            this.colCRFulfilled,
            this.colCRNeedBy});
            this.dgvCommodityRequests.ContextMenuStrip = this.cmsCommodityRequests;
            this.dgvCommodityRequests.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvCommodityRequests.Location = new System.Drawing.Point(2, 2);
            this.dgvCommodityRequests.Name = "dgvCommodityRequests";
            this.dgvCommodityRequests.RowHeadersVisible = false;
            this.dgvCommodityRequests.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvCommodityRequests.Size = new System.Drawing.Size(788, 290);
            this.dgvCommodityRequests.TabIndex = 0;
            // 
            // colCRName
            // 
            this.colCRName.HeaderText = "Name";
            this.colCRName.Name = "colCRName";
            this.colCRName.ReadOnly = true;
            this.colCRName.Width = 200;
            // 
            // colCRAmount
            // 
            this.colCRAmount.HeaderText = "Amount";
            this.colCRAmount.Name = "colCRAmount";
            this.colCRAmount.Width = 80;
            // 
            // colCRFulfilled
            // 
            this.colCRFulfilled.HeaderText = "Fulfilled";
            this.colCRFulfilled.Name = "colCRFulfilled";
            this.colCRFulfilled.Width = 60;
            // 
            // colCRNeedBy
            // 
            this.colCRNeedBy.HeaderText = "NeedBy";
            this.colCRNeedBy.Name = "colCRNeedBy";
            this.colCRNeedBy.Width = 120;
            // 
            // flpAddCommodityRequest
            // 
            this.flpAddCommodityRequest.Controls.Add(this.txtCommodityRequestFilter);
            this.flpAddCommodityRequest.Controls.Add(this.cmbCommodityRequest);
            this.flpAddCommodityRequest.Controls.Add(this.txtCommodityRequestQty);
            this.flpAddCommodityRequest.Controls.Add(this.txtCommodityRequestNeedBy);
            this.flpAddCommodityRequest.Controls.Add(this.cmdAddCommodityRequest);
            this.flpAddCommodityRequest.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flpAddCommodityRequest.Location = new System.Drawing.Point(2, 292);
            this.flpAddCommodityRequest.Name = "flpAddCommodityRequest";
            this.flpAddCommodityRequest.Size = new System.Drawing.Size(788, 30);
            this.flpAddCommodityRequest.TabIndex = 1;
            this.flpAddCommodityRequest.WrapContents = false;
            // 
            // txtCommodityRequestFilter
            // 
            this.txtCommodityRequestFilter.Location = new System.Drawing.Point(3, 3);
            this.txtCommodityRequestFilter.Name = "txtCommodityRequestFilter";
            this.txtCommodityRequestFilter.Size = new System.Drawing.Size(120, 20);
            this.txtCommodityRequestFilter.TabIndex = 0;
            // 
            // cmbCommodityRequest
            // 
            this.cmbCommodityRequest.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCommodityRequest.FormattingEnabled = true;
            this.cmbCommodityRequest.Location = new System.Drawing.Point(129, 3);
            this.cmbCommodityRequest.Name = "cmbCommodityRequest";
            this.cmbCommodityRequest.Size = new System.Drawing.Size(280, 21);
            this.cmbCommodityRequest.TabIndex = 1;
            // 
            // txtCommodityRequestQty
            // 
            this.txtCommodityRequestQty.Location = new System.Drawing.Point(415, 3);
            this.txtCommodityRequestQty.Name = "txtCommodityRequestQty";
            this.txtCommodityRequestQty.Size = new System.Drawing.Size(60, 20);
            this.txtCommodityRequestQty.TabIndex = 2;
            this.txtCommodityRequestQty.Text = "0";
            // 
            // txtCommodityRequestNeedBy
            // 
            this.txtCommodityRequestNeedBy.Location = new System.Drawing.Point(481, 3);
            this.txtCommodityRequestNeedBy.Name = "txtCommodityRequestNeedBy";
            this.txtCommodityRequestNeedBy.Size = new System.Drawing.Size(100, 20);
            this.txtCommodityRequestNeedBy.TabIndex = 3;
            // 
            // cmdAddCommodityRequest
            // 
            this.cmdAddCommodityRequest.Location = new System.Drawing.Point(587, 3);
            this.cmdAddCommodityRequest.Name = "cmdAddCommodityRequest";
            this.cmdAddCommodityRequest.Size = new System.Drawing.Size(50, 23);
            this.cmdAddCommodityRequest.TabIndex = 4;
            this.cmdAddCommodityRequest.Text = "Add";
            this.cmdAddCommodityRequest.UseVisualStyleBackColor = true;
            // 
            // tabPWarehousing
            // 
            this.tabPWarehousing.Controls.Add(this.dgvItems);
            this.tabPWarehousing.Controls.Add(this.flpAddItem);
            this.tabPWarehousing.Location = new System.Drawing.Point(4, 22);
            this.tabPWarehousing.Name = "tabPWarehousing";
            this.tabPWarehousing.Padding = new System.Windows.Forms.Padding(2);
            this.tabPWarehousing.Size = new System.Drawing.Size(792, 324);
            this.tabPWarehousing.TabIndex = 3;
            this.tabPWarehousing.Text = "Warehousing";
            this.tabPWarehousing.UseVisualStyleBackColor = true;
            //
            // tabPOverflow
            //
            this.tabPOverflow.Controls.Add(this.dgvOverflowRules);
            this.tabPOverflow.Controls.Add(this.flpOverflowAdd);
            this.tabPOverflow.Controls.Add(this.flpOverflowAdd2);
            this.tabPOverflow.Controls.Add(this.flpOverflowButtons);
            this.tabPOverflow.Location = new System.Drawing.Point(4, 22);
            this.tabPOverflow.Name = "tabPOverflow";
            this.tabPOverflow.Padding = new System.Windows.Forms.Padding(2);
            this.tabPOverflow.Size = new System.Drawing.Size(792, 324);
            this.tabPOverflow.TabIndex = 4;
            this.tabPOverflow.Text = "Overflow";
            this.tabPOverflow.UseVisualStyleBackColor = true;
            //
            // dgvOverflowRules
            //
            this.dgvOverflowRules.AllowUserToAddRows = false;
            this.dgvOverflowRules.AllowUserToDeleteRows = false;
            this.dgvOverflowRules.AllowUserToOrderColumns = true;
            this.dgvOverflowRules.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvOverflowRules.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colOverflowRuleType, this.colOverflowResource, this.colOverflowPurity, this.colOverflowThreshold, this.colOverflowCurrent,
            this.colOverflowDest, this.colOverflowRoute, this.colOverflowActive});
            this.dgvOverflowRules.ContextMenuStrip = this.cmsOverflowRules;
            this.dgvOverflowRules.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvOverflowRules.Location = new System.Drawing.Point(2, 2);
            this.dgvOverflowRules.Name = "dgvOverflowRules";
            this.dgvOverflowRules.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvOverflowRules.Size = new System.Drawing.Size(788, 200);
            this.colOverflowRuleType.HeaderText = "Rule Type";
            this.colOverflowRuleType.Name = "colOverflowRuleType";
            this.colOverflowRuleType.ReadOnly = true;
            this.colOverflowRuleType.Width = 110;
            this.colOverflowResource.HeaderText = "Resource";
            this.colOverflowResource.Name = "colOverflowResource";
            this.colOverflowResource.ReadOnly = true;
            this.colOverflowResource.Width = 120;
            this.colOverflowPurity.HeaderText = "Purity";
            this.colOverflowPurity.Name = "colOverflowPurity";
            this.colOverflowPurity.ReadOnly = true;
            this.colOverflowPurity.Width = 80;
            this.colOverflowThreshold.HeaderText = "Threshold";
            this.colOverflowThreshold.Name = "colOverflowThreshold";
            this.colOverflowThreshold.ReadOnly = true;
            this.colOverflowThreshold.Width = 80;
            this.colOverflowCurrent.HeaderText = "Current";
            this.colOverflowCurrent.Name = "colOverflowCurrent";
            this.colOverflowCurrent.ReadOnly = true;
            this.colOverflowCurrent.Width = 80;
            this.colOverflowDest.HeaderText = "Destination";
            this.colOverflowDest.Name = "colOverflowDest";
            this.colOverflowDest.ReadOnly = true;
            this.colOverflowDest.Width = 130;
            this.colOverflowRoute.HeaderText = "Route";
            this.colOverflowRoute.Name = "colOverflowRoute";
            this.colOverflowRoute.ReadOnly = true;
            this.colOverflowRoute.Width = 130;
            this.colOverflowActive.HeaderText = "Active";
            this.colOverflowActive.Name = "colOverflowActive";
            this.colOverflowActive.Width = 50;
            //
            // flpOverflowAdd
            //
            this.flpOverflowAdd.AutoSize = true;
            this.flpOverflowAdd.Controls.Add(this.lblOverflowRuleType);
            this.flpOverflowAdd.Controls.Add(this.cmbOverflowRuleType);
            this.flpOverflowAdd.Controls.Add(this.lblOverflowResource);
            this.flpOverflowAdd.Controls.Add(this.cmbOverflowResource);
            this.flpOverflowAdd.Controls.Add(this.lblOverflowPurity);
            this.flpOverflowAdd.Controls.Add(this.cmbOverflowPurity);
            this.flpOverflowAdd.Controls.Add(this.lblOverflowThreshold);
            this.flpOverflowAdd.Controls.Add(this.txtOverflowThreshold);
            this.flpOverflowAdd.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flpOverflowAdd.Name = "flpOverflowAdd";
            this.flpOverflowAdd.Size = new System.Drawing.Size(788, 30);
            this.lblOverflowRuleType.AutoSize = true;
            this.lblOverflowRuleType.Text = "Rule Type:";
            this.lblOverflowRuleType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOverflowRuleType.Name = "lblOverflowRuleType";
            this.cmbOverflowRuleType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbOverflowRuleType.Size = new System.Drawing.Size(130, 21);
            this.cmbOverflowRuleType.Name = "cmbOverflowRuleType";
            this.lblOverflowResource.AutoSize = true;
            this.lblOverflowResource.Text = "Resource:";
            this.lblOverflowResource.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOverflowResource.Name = "lblOverflowResource";
            this.cmbOverflowResource.Size = new System.Drawing.Size(200, 21);
            this.cmbOverflowResource.Name = "cmbOverflowResource";
            this.lblOverflowPurity.AutoSize = true;
            this.lblOverflowPurity.Text = "Purity:";
            this.lblOverflowPurity.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOverflowPurity.Name = "lblOverflowPurity";
            this.cmbOverflowPurity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbOverflowPurity.Size = new System.Drawing.Size(90, 21);
            this.cmbOverflowPurity.Name = "cmbOverflowPurity";
            this.lblOverflowThreshold.AutoSize = true;
            this.lblOverflowThreshold.Text = "Threshold:";
            this.lblOverflowThreshold.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOverflowThreshold.Name = "lblOverflowThreshold";
            this.txtOverflowThreshold.Size = new System.Drawing.Size(70, 20);
            this.txtOverflowThreshold.Name = "txtOverflowThreshold";
            //
            // flpOverflowAdd2
            //
            this.flpOverflowAdd2.AutoSize = true;
            this.flpOverflowAdd2.Controls.Add(this.lblOverflowDestType);
            this.flpOverflowAdd2.Controls.Add(this.cmbOverflowDestType);
            this.flpOverflowAdd2.Controls.Add(this.lblOverflowDest);
            this.flpOverflowAdd2.Controls.Add(this.cmbOverflowDest);
            this.flpOverflowAdd2.Controls.Add(this.lblOverflowRoute);
            this.flpOverflowAdd2.Controls.Add(this.cmbOverflowRoute);
            this.flpOverflowAdd2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flpOverflowAdd2.Name = "flpOverflowAdd2";
            this.flpOverflowAdd2.Size = new System.Drawing.Size(788, 30);
            this.lblOverflowDestType.AutoSize = true;
            this.lblOverflowDestType.Text = "Dest Type:";
            this.lblOverflowDestType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOverflowDestType.Name = "lblOverflowDestType";
            this.cmbOverflowDestType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbOverflowDestType.Size = new System.Drawing.Size(90, 21);
            this.cmbOverflowDestType.Name = "cmbOverflowDestType";
            this.lblOverflowDest.AutoSize = true;
            this.lblOverflowDest.Text = "Dest:";
            this.lblOverflowDest.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOverflowDest.Name = "lblOverflowDest";
            this.cmbOverflowDest.Size = new System.Drawing.Size(260, 21);
            this.cmbOverflowDest.Name = "cmbOverflowDest";
            this.lblOverflowRoute.AutoSize = true;
            this.lblOverflowRoute.Text = "Route:";
            this.lblOverflowRoute.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOverflowRoute.Name = "lblOverflowRoute";
            this.cmbOverflowRoute.Size = new System.Drawing.Size(260, 21);
            this.cmbOverflowRoute.Name = "cmbOverflowRoute";
            //
            // flpOverflowButtons
            //
            this.flpOverflowButtons.AutoSize = true;
            this.flpOverflowButtons.Controls.Add(this.cmdAddOverflowRule);
            this.flpOverflowButtons.Controls.Add(this.cmdRemoveOverflowRule);
            this.flpOverflowButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flpOverflowButtons.Name = "flpOverflowButtons";
            this.flpOverflowButtons.Size = new System.Drawing.Size(788, 29);
            this.cmdAddOverflowRule.Size = new System.Drawing.Size(75, 23);
            this.cmdAddOverflowRule.Text = "Add Rule";
            this.cmdAddOverflowRule.UseVisualStyleBackColor = true;
            this.cmdAddOverflowRule.Name = "cmdAddOverflowRule";
            this.cmdRemoveOverflowRule.Size = new System.Drawing.Size(90, 23);
            this.cmdRemoveOverflowRule.Text = "Remove Rule";
            this.cmdRemoveOverflowRule.UseVisualStyleBackColor = true;
            this.cmdRemoveOverflowRule.Name = "cmdRemoveOverflowRule";
            // cmsItems
            this.cmsItems.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.tsmiAddItem, this.tsmiRemoveItem });
            this.cmsItems.Name = "cmsItems";
            this.cmsItems.Size = new System.Drawing.Size(130, 48);
            this.tsmiAddItem.Name = "tsmiAddItem";
            this.tsmiAddItem.Size = new System.Drawing.Size(129, 22);
            this.tsmiAddItem.Text = "Add Item";
            this.tsmiRemoveItem.Name = "tsmiRemoveItem";
            this.tsmiRemoveItem.Size = new System.Drawing.Size(129, 22);
            this.tsmiRemoveItem.Text = "Remove Item";
            // cmsCommodityRequests
            this.cmsCommodityRequests.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.tsmiAddCommodityRequest, this.tsmiRemoveCommodityRequest });
            this.cmsCommodityRequests.Name = "cmsCommodityRequests";
            this.cmsCommodityRequests.Size = new System.Drawing.Size(170, 48);
            this.tsmiAddCommodityRequest.Name = "tsmiAddCommodityRequest";
            this.tsmiAddCommodityRequest.Size = new System.Drawing.Size(169, 22);
            this.tsmiAddCommodityRequest.Text = "Add Request";
            this.tsmiRemoveCommodityRequest.Name = "tsmiRemoveCommodityRequest";
            this.tsmiRemoveCommodityRequest.Size = new System.Drawing.Size(169, 22);
            this.tsmiRemoveCommodityRequest.Text = "Remove Request";
            // cmsOverflowRules
            this.cmsOverflowRules.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.tsmiAddOverflowRule, this.tsmiRemoveOverflowRule });
            this.cmsOverflowRules.Name = "cmsOverflowRules";
            this.cmsOverflowRules.Size = new System.Drawing.Size(150, 48);
            this.tsmiAddOverflowRule.Name = "tsmiAddOverflowRule";
            this.tsmiAddOverflowRule.Size = new System.Drawing.Size(149, 22);
            this.tsmiAddOverflowRule.Text = "Add Rule";
            this.tsmiRemoveOverflowRule.Name = "tsmiRemoveOverflowRule";
            this.tsmiRemoveOverflowRule.Size = new System.Drawing.Size(149, 22);
            this.tsmiRemoveOverflowRule.Text = "Remove Rule";
            // 
            // dgvItems
            // 
            this.dgvItems.AllowUserToAddRows = false;
            this.dgvItems.AllowUserToDeleteRows = false;
            this.dgvItems.AllowUserToOrderColumns = true;
            this.dgvItems.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvItems.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colItemType,
            this.colItemName,
            this.colItemLocked,
            this.colItemAmount});
            this.dgvItems.ContextMenuStrip = this.cmsItems;
            this.dgvItems.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvItems.Location = new System.Drawing.Point(2, 2);
            this.dgvItems.Name = "dgvItems";
            this.dgvItems.RowHeadersVisible = false;
            this.dgvItems.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvItems.Size = new System.Drawing.Size(788, 290);
            this.dgvItems.TabIndex = 0;
            // 
            // colItemType
            // 
            this.colItemType.HeaderText = "ItemType";
            this.colItemType.Name = "colItemType";
            this.colItemType.ReadOnly = true;
            // 
            // colItemName
            // 
            this.colItemName.HeaderText = "Item";
            this.colItemName.Name = "colItemName";
            this.colItemName.ReadOnly = true;
            this.colItemName.Width = 200;
            // 
            // colItemLocked
            // 
            this.colItemLocked.HeaderText = "Locked";
            this.colItemLocked.Name = "colItemLocked";
            this.colItemLocked.ReadOnly = true;
            this.colItemLocked.Width = 60;
            // 
            // colItemAmount
            // 
            this.colItemAmount.HeaderText = "Amount";
            this.colItemAmount.Name = "colItemAmount";
            this.colItemAmount.Width = 80;
            // 
            // flpAddItem
            // 
            this.flpAddItem.Controls.Add(this.cmbItemType);
            this.flpAddItem.Controls.Add(this.cmbItem);
            this.flpAddItem.Controls.Add(this.cmbPurity);
            this.flpAddItem.Controls.Add(this.txtQuantity);
            this.flpAddItem.Controls.Add(this.cmdAddItem);
            this.flpAddItem.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flpAddItem.Location = new System.Drawing.Point(2, 292);
            this.flpAddItem.Name = "flpAddItem";
            this.flpAddItem.Size = new System.Drawing.Size(788, 30);
            this.flpAddItem.TabIndex = 1;
            this.flpAddItem.WrapContents = false;
            // 
            // cmbItemType
            // 
            this.cmbItemType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbItemType.DisplayMember = "Name";
            this.cmbItemType.FormattingEnabled = true;
            this.cmbItemType.Location = new System.Drawing.Point(3, 3);
            this.cmbItemType.Name = "cmbItemType";
            this.cmbItemType.Size = new System.Drawing.Size(121, 21);
            this.cmbItemType.TabIndex = 0;
            // 
            // cmbItem
            // 
            this.cmbItem.Location = new System.Drawing.Point(130, 3);
            this.cmbItem.Name = "cmbItem";
            this.cmbItem.Size = new System.Drawing.Size(347, 21);
            this.cmbItem.TabIndex = 1;
            // 
            // cmbPurity
            // 
            this.cmbPurity.DisplayMember = "Name";
            this.cmbPurity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPurity.FormattingEnabled = true;
            this.cmbPurity.Location = new System.Drawing.Point(483, 3);
            this.cmbPurity.Name = "cmbPurity";
            this.cmbPurity.Size = new System.Drawing.Size(70, 21);
            this.cmbPurity.TabIndex = 2;
            this.cmbPurity.Visible = false;
            // 
            // txtQuantity
            // 
            this.txtQuantity.Location = new System.Drawing.Point(559, 3);
            this.txtQuantity.Name = "txtQuantity";
            this.txtQuantity.Size = new System.Drawing.Size(60, 20);
            this.txtQuantity.TabIndex = 3;
            this.txtQuantity.Text = "0";
            // 
            // cmdAddItem
            // 
            this.cmdAddItem.Location = new System.Drawing.Point(625, 3);
            this.cmdAddItem.Name = "cmdAddItem";
            this.cmdAddItem.Size = new System.Drawing.Size(50, 23);
            this.cmdAddItem.TabIndex = 4;
            this.cmdAddItem.Text = "Add";
            this.cmdAddItem.UseVisualStyleBackColor = true;
            // 
            // flpCommands
            // 
            this.flpCommands.Controls.Add(this.cmdNew);
            this.flpCommands.Controls.Add(this.cmdSave);
            this.flpCommands.Controls.Add(this.cmdDelete);
            this.flpCommands.Controls.Add(this.cmdImportColony);
            this.flpCommands.Controls.Add(this.cmdImportClipboard);
            this.flpCommands.Location = new System.Drawing.Point(2, 452);
            this.flpCommands.Margin = new System.Windows.Forms.Padding(2);
            this.flpCommands.Name = "flpCommands";
            this.flpCommands.Size = new System.Drawing.Size(800, 32);
            this.flpCommands.TabIndex = 2;
            // 
            // cmdNew
            // 
            this.cmdNew.Location = new System.Drawing.Point(3, 3);
            this.cmdNew.Name = "cmdNew";
            this.cmdNew.Size = new System.Drawing.Size(75, 23);
            this.cmdNew.TabIndex = 0;
            this.cmdNew.Text = "New";
            this.cmdNew.UseVisualStyleBackColor = true;
            this.cmdNew.Click += new System.EventHandler(this.CmdNew_Click);
            // 
            // cmdSave
            // 
            this.cmdSave.Location = new System.Drawing.Point(84, 3);
            this.cmdSave.Name = "cmdSave";
            this.cmdSave.Size = new System.Drawing.Size(75, 23);
            this.cmdSave.TabIndex = 1;
            this.cmdSave.Text = "Save";
            this.cmdSave.UseVisualStyleBackColor = true;
            this.cmdSave.Click += new System.EventHandler(this.CmdSave_Click);
            // 
            // cmdDelete
            // 
            this.cmdDelete.Location = new System.Drawing.Point(165, 3);
            this.cmdDelete.Name = "cmdDelete";
            this.cmdDelete.Size = new System.Drawing.Size(75, 23);
            this.cmdDelete.TabIndex = 2;
            this.cmdDelete.Text = "Delete";
            this.cmdDelete.UseVisualStyleBackColor = true;
            this.cmdDelete.Click += new System.EventHandler(this.CmdDelete_Click);
            // 
            // cmdImportColony
            // 
            this.cmdImportColony.Location = new System.Drawing.Point(246, 3);
            this.cmdImportColony.Name = "cmdImportColony";
            this.cmdImportColony.Size = new System.Drawing.Size(75, 23);
            this.cmdImportColony.TabIndex = 3;
            this.cmdImportColony.Text = "Import";
            this.cmdImportColony.UseVisualStyleBackColor = true;
            // 
            // cmdImportClipboard
            // 
            this.cmdImportClipboard.Location = new System.Drawing.Point(327, 3);
            this.cmdImportClipboard.Name = "cmdImportClipboard";
            this.cmdImportClipboard.Size = new System.Drawing.Size(95, 23);
            this.cmdImportClipboard.TabIndex = 4;
            this.cmdImportClipboard.Text = "Save Clipboard";
            this.cmdImportClipboard.UseVisualStyleBackColor = true;
            // 
            // FormColonyV2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1225, 500);
            this.Controls.Add(this.splitMain);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "FormColonyV2";
            this.Text = "Manage Colonies";
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel1.PerformLayout();
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.flpColonyData.ResumeLayout(false);
            this.flpIdentity.ResumeLayout(false);
            this.flpPlanetName.ResumeLayout(false);
            this.flpPlanetName.PerformLayout();
            this.flpColonyName.ResumeLayout(false);
            this.flpColonyName.PerformLayout();
            this.flpSystemName.ResumeLayout(false);
            this.flpSystemName.PerformLayout();
            this.tabDetailedData.ResumeLayout(false);
            this.tabPAdministration.ResumeLayout(false);
            this.flpAdminCommands.ResumeLayout(false);
            this.flpAdminCommands.PerformLayout();
            this.splitStructures.Panel1.ResumeLayout(false);
            this.splitStructures.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitStructures)).EndInit();
            this.splitStructures.ResumeLayout(false);
            this.tabPStructures.ResumeLayout(false);
            this.flpAddStructure.ResumeLayout(false);
            this.flpAddStructure.PerformLayout();
            this.tabPWorkers.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvCommodityRequests)).EndInit();
            this.flpAddCommodityRequest.ResumeLayout(false);
            this.flpAddCommodityRequest.PerformLayout();
            this.tabPWarehousing.ResumeLayout(false);
            this.tabPOverflow.ResumeLayout(false);
            this.flpOverflowAdd.ResumeLayout(false);
            this.flpOverflowAdd.PerformLayout();
            this.flpOverflowAdd2.ResumeLayout(false);
            this.flpOverflowAdd2.PerformLayout();
            this.flpOverflowButtons.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvOverflowRules)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvItems)).EndInit();
            this.flpAddItem.ResumeLayout(false);
            this.flpAddItem.PerformLayout();
            this.flpCommands.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.ListView lvwColonies;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtColonyFilter;
        private System.Windows.Forms.FlowLayoutPanel flpColonyData;
        private System.Windows.Forms.FlowLayoutPanel flpIdentity;
        private System.Windows.Forms.FlowLayoutPanel flpPlanetName;
        private System.Windows.Forms.Label lblPlanetName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtPlanetName;
        private System.Windows.Forms.FlowLayoutPanel flpColonyName;
        private System.Windows.Forms.Label lblColonyName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtColonyName;
        private System.Windows.Forms.FlowLayoutPanel flpSystemName;
        private System.Windows.Forms.Label lblSystemName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtSystemName;
        private System.Windows.Forms.TabControl tabDetailedData;
        private System.Windows.Forms.TabPage tabPAdministration;
        private System.Windows.Forms.TabPage tabPStructures;
        private System.Windows.Forms.RichTextBox rtbStatusSummary;
        private System.Windows.Forms.SplitContainer splitStructures;
        private System.Windows.Forms.ListView lvwStructureTypes;
        private System.Windows.Forms.FlowLayoutPanel flpStructures;
        private System.Windows.Forms.FlowLayoutPanel flpAddStructure;
        private OE2EmpireTracker.Controls.FilteredTextComboSet cmbFlatpacks;
        private System.Windows.Forms.Button cmdAddFlatpack;
        private System.Windows.Forms.TabPage tabPWorkers;
        private DataEntryGridView dgvCommodityRequests;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCRName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCRAmount;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colCRFulfilled;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCRNeedBy;
        private System.Windows.Forms.FlowLayoutPanel flpAddCommodityRequest;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtCommodityRequestFilter;
        private System.Windows.Forms.ComboBox cmbCommodityRequest;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtCommodityRequestQty;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtCommodityRequestNeedBy;
        private System.Windows.Forms.Button cmdAddCommodityRequest;
        private System.Windows.Forms.TabPage tabPWarehousing;
        private DataEntryGridView dgvItems;
        private System.Windows.Forms.DataGridViewTextBoxColumn colItemType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colItemName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colItemLocked;
        private System.Windows.Forms.DataGridViewTextBoxColumn colItemAmount;
        private System.Windows.Forms.FlowLayoutPanel flpAddItem;
        private System.Windows.Forms.ComboBox cmbItemType;
        private OE2EmpireTracker.Controls.FilteredTextComboSet cmbItem;
        private System.Windows.Forms.ComboBox cmbPurity;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtQuantity;
        private System.Windows.Forms.Button cmdAddItem;
        private System.Windows.Forms.FlowLayoutPanel flpCommands;
        private System.Windows.Forms.Button cmdNew;
        private System.Windows.Forms.Button cmdSave;
        private System.Windows.Forms.Button cmdDelete;
        private System.Windows.Forms.Button cmdImportColony;
        private System.Windows.Forms.Button cmdImportClipboard;
        private System.Windows.Forms.RichTextBox rtbAdminReport;
        private System.Windows.Forms.FlowLayoutPanel flpAdminCommands;
        private System.Windows.Forms.Button cmdBootstrap;
        private System.Windows.Forms.Button cmdOptimize;
        private System.Windows.Forms.Button cmdGenerateBuildPlan;
        private System.Windows.Forms.Timer timerAdminRefresh;
        private System.Windows.Forms.TabPage tabPOverflow;
        private System.Windows.Forms.DataGridView dgvOverflowRules;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOverflowRuleType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOverflowResource;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOverflowPurity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOverflowThreshold;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOverflowCurrent;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOverflowDest;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOverflowRoute;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colOverflowActive;
        private System.Windows.Forms.FlowLayoutPanel flpOverflowAdd;
        private System.Windows.Forms.Label lblOverflowRuleType;
        private System.Windows.Forms.ComboBox cmbOverflowRuleType;
        private System.Windows.Forms.Label lblOverflowResource;
        private OE2EmpireTracker.Controls.FilteredTextComboSet cmbOverflowResource;
        private System.Windows.Forms.Label lblOverflowPurity;
        private System.Windows.Forms.ComboBox cmbOverflowPurity;
        private System.Windows.Forms.Label lblOverflowThreshold;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtOverflowThreshold;
        private System.Windows.Forms.FlowLayoutPanel flpOverflowAdd2;
        private System.Windows.Forms.Label lblOverflowDestType;
        private System.Windows.Forms.ComboBox cmbOverflowDestType;
        private System.Windows.Forms.Label lblOverflowDest;
        private OE2EmpireTracker.Controls.FilteredTextComboSet cmbOverflowDest;
        private System.Windows.Forms.Label lblOverflowRoute;
        private OE2EmpireTracker.Controls.FilteredTextComboSet cmbOverflowRoute;
        private System.Windows.Forms.FlowLayoutPanel flpOverflowButtons;
        private System.Windows.Forms.Button cmdAddOverflowRule;
        private System.Windows.Forms.Button cmdRemoveOverflowRule;
        private System.Windows.Forms.ContextMenuStrip cmsItems;
        private System.Windows.Forms.ToolStripMenuItem tsmiAddItem;
        private System.Windows.Forms.ToolStripMenuItem tsmiRemoveItem;
        private System.Windows.Forms.ContextMenuStrip cmsCommodityRequests;
        private System.Windows.Forms.ToolStripMenuItem tsmiAddCommodityRequest;
        private System.Windows.Forms.ToolStripMenuItem tsmiRemoveCommodityRequest;
        private System.Windows.Forms.ContextMenuStrip cmsOverflowRules;
        private System.Windows.Forms.ToolStripMenuItem tsmiAddOverflowRule;
        private System.Windows.Forms.ToolStripMenuItem tsmiRemoveOverflowRule;
        private System.Windows.Forms.Button cmdSync;
        private System.Windows.Forms.ToolTip toolTipSync;
        private System.Windows.Forms.Timer timerSyncCooldown;
        private System.Windows.Forms.Timer timerSyncStatus;
    }
}
