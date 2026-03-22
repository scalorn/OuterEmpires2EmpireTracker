namespace OE2EmpireTracker.Forms.Colony
{
    partial class FormColony
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
            this.flpSearchList = new System.Windows.Forms.FlowLayoutPanel();
            this.flpBlueprintSearch = new System.Windows.Forms.FlowLayoutPanel();
            this.lblBlueprintListFilter = new System.Windows.Forms.Label();
            this.txtBlueprintListFilter = new System.Windows.Forms.TextBox();
            this.lvwColonies = new System.Windows.Forms.ListView();
            this.Property = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BaseValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CurrentValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tlpBase = new System.Windows.Forms.TableLayoutPanel();
            this.flpColonyData = new System.Windows.Forms.FlowLayoutPanel();
            this.flpBaseDetails = new System.Windows.Forms.FlowLayoutPanel();
            this.flpPlanetName = new System.Windows.Forms.FlowLayoutPanel();
            this.label3 = new System.Windows.Forms.Label();
            this.txtPlanetName = new System.Windows.Forms.TextBox();
            this.flpColonyName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblBlueprintType = new System.Windows.Forms.Label();
            this.txtColonyName = new System.Windows.Forms.TextBox();
            this.tabDetailedData = new System.Windows.Forms.TabControl();
            this.tabPAdministration = new System.Windows.Forms.TabPage();
            this.tabPStructures = new System.Windows.Forms.TabPage();
            this.flpStructures = new System.Windows.Forms.FlowLayoutPanel();
            this.lvwStructureTypes = new System.Windows.Forms.ListView();
            this.flpStructureData = new System.Windows.Forms.FlowLayoutPanel();
            this.flpStatus = new System.Windows.Forms.FlowLayoutPanel();
            this.rtbStatus = new System.Windows.Forms.RichTextBox();
            this.flpAddBox = new System.Windows.Forms.FlowLayoutPanel();
            this.txtFilterFlatpack = new System.Windows.Forms.TextBox();
            this.cmbFlatpacks = new System.Windows.Forms.ComboBox();
            this.cmdAddFlatpack = new System.Windows.Forms.Button();
            this.flpColonyStructure = new System.Windows.Forms.FlowLayoutPanel();
            this.colonyStructure1 = new OE2EmpireTracker.Forms.Colony.ColonyStructure();
            this.tabPWorkers = new System.Windows.Forms.TabPage();
            this.tabPWarehousing = new System.Windows.Forms.TabPage();
            this.flowLayoutPanel7 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel9 = new System.Windows.Forms.FlowLayoutPanel();
            this.lblItemType = new System.Windows.Forms.Label();
            this.cmbItemType = new System.Windows.Forms.ComboBox();
            this.lblFilter = new System.Windows.Forms.Label();
            this.txtItemFilter = new System.Windows.Forms.TextBox();
            this.cmbItem = new System.Windows.Forms.ComboBox();
            this.cmdAdd = new System.Windows.Forms.Button();
            this.dgvResources = new System.Windows.Forms.DataGridView();
            this.ItemType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Item = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LockedAmount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Amount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.chkGlobalBlueprint = new System.Windows.Forms.CheckBox();
            this.cmdSave = new System.Windows.Forms.Button();
            this.cmdDelete = new System.Windows.Forms.Button();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.cmbPurity = new System.Windows.Forms.ComboBox();
            this.flpSearchList.SuspendLayout();
            this.flpBlueprintSearch.SuspendLayout();
            this.tlpBase.SuspendLayout();
            this.flpColonyData.SuspendLayout();
            this.flpBaseDetails.SuspendLayout();
            this.flpPlanetName.SuspendLayout();
            this.flpColonyName.SuspendLayout();
            this.tabDetailedData.SuspendLayout();
            this.tabPStructures.SuspendLayout();
            this.flpStructures.SuspendLayout();
            this.flpStructureData.SuspendLayout();
            this.flpStatus.SuspendLayout();
            this.flpAddBox.SuspendLayout();
            this.flpColonyStructure.SuspendLayout();
            this.tabPWarehousing.SuspendLayout();
            this.flowLayoutPanel7.SuspendLayout();
            this.flowLayoutPanel9.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResources)).BeginInit();
            this.flpCommands.SuspendLayout();
            this.SuspendLayout();
            // 
            // flpSearchList
            // 
            this.flpSearchList.Controls.Add(this.flpBlueprintSearch);
            this.flpSearchList.Controls.Add(this.lvwColonies);
            this.flpSearchList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpSearchList.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpSearchList.Location = new System.Drawing.Point(3, 3);
            this.flpSearchList.Name = "flpSearchList";
            this.flpSearchList.Size = new System.Drawing.Size(234, 303);
            this.flpSearchList.TabIndex = 10;
            this.flpSearchList.WrapContents = false;
            this.flpSearchList.Layout += new System.Windows.Forms.LayoutEventHandler(this.flpSearchList_Layout);
            this.flpSearchList.Resize += new System.EventHandler(this.flpSearchList_Resize);
            // 
            // flpBlueprintSearch
            // 
            this.flpBlueprintSearch.Controls.Add(this.lblBlueprintListFilter);
            this.flpBlueprintSearch.Controls.Add(this.txtBlueprintListFilter);
            this.flpBlueprintSearch.Dock = System.Windows.Forms.DockStyle.Left;
            this.flpBlueprintSearch.Location = new System.Drawing.Point(2, 2);
            this.flpBlueprintSearch.Margin = new System.Windows.Forms.Padding(2);
            this.flpBlueprintSearch.Name = "flpBlueprintSearch";
            this.flpBlueprintSearch.Size = new System.Drawing.Size(232, 26);
            this.flpBlueprintSearch.TabIndex = 5;
            // 
            // lblBlueprintListFilter
            // 
            this.lblBlueprintListFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblBlueprintListFilter.Location = new System.Drawing.Point(2, 4);
            this.lblBlueprintListFilter.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblBlueprintListFilter.Name = "lblBlueprintListFilter";
            this.lblBlueprintListFilter.Size = new System.Drawing.Size(54, 17);
            this.lblBlueprintListFilter.TabIndex = 2;
            this.lblBlueprintListFilter.Text = "Name";
            this.lblBlueprintListFilter.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtBlueprintListFilter
            // 
            this.txtBlueprintListFilter.Location = new System.Drawing.Point(61, 3);
            this.txtBlueprintListFilter.Name = "txtBlueprintListFilter";
            this.txtBlueprintListFilter.Size = new System.Drawing.Size(100, 20);
            this.txtBlueprintListFilter.TabIndex = 0;
            // 
            // lvwColonies
            // 
            this.lvwColonies.FullRowSelect = true;
            this.lvwColonies.HideSelection = false;
            this.lvwColonies.Location = new System.Drawing.Point(3, 33);
            this.lvwColonies.MultiSelect = false;
            this.lvwColonies.Name = "lvwColonies";
            this.lvwColonies.Size = new System.Drawing.Size(230, 200);
            this.lvwColonies.TabIndex = 6;
            this.lvwColonies.UseCompatibleStateImageBehavior = false;
            this.lvwColonies.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.lvwColonies_ItemSelectionChanged);
            this.lvwColonies.SelectedIndexChanged += new System.EventHandler(this.lvwColonies_SelectedIndexChanged);
            // 
            // Property
            // 
            this.Property.HeaderText = "Property";
            this.Property.Name = "Property";
            this.Property.ReadOnly = true;
            this.Property.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // BaseValue
            // 
            this.BaseValue.HeaderText = "BaseValue";
            this.BaseValue.Name = "BaseValue";
            this.BaseValue.ReadOnly = true;
            this.BaseValue.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // CurrentValue
            // 
            this.CurrentValue.HeaderText = "CurrentValue";
            this.CurrentValue.Name = "CurrentValue";
            this.CurrentValue.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // tlpBase
            // 
            this.tlpBase.AutoSize = true;
            this.tlpBase.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tlpBase.ColumnCount = 2;
            this.tlpBase.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 240F));
            this.tlpBase.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpBase.Controls.Add(this.flpSearchList, 0, 0);
            this.tlpBase.Controls.Add(this.flpColonyData, 1, 0);
            this.tlpBase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpBase.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.tlpBase.Location = new System.Drawing.Point(0, 0);
            this.tlpBase.Name = "tlpBase";
            this.tlpBase.RowCount = 1;
            this.tlpBase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpBase.Size = new System.Drawing.Size(1225, 309);
            this.tlpBase.TabIndex = 7;
            this.tlpBase.Layout += new System.Windows.Forms.LayoutEventHandler(this.tlpBase_Layout);
            this.tlpBase.Resize += new System.EventHandler(this.tlpBase_Resize);
            // 
            // flpColonyData
            // 
            this.flpColonyData.Controls.Add(this.flpBaseDetails);
            this.flpColonyData.Controls.Add(this.tabDetailedData);
            this.flpColonyData.Controls.Add(this.flpCommands);
            this.flpColonyData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpColonyData.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpColonyData.Location = new System.Drawing.Point(243, 3);
            this.flpColonyData.Name = "flpColonyData";
            this.flpColonyData.Size = new System.Drawing.Size(979, 303);
            this.flpColonyData.TabIndex = 10;
            this.flpColonyData.WrapContents = false;
            this.flpColonyData.Layout += new System.Windows.Forms.LayoutEventHandler(this.flpColonyData_Layout);
            // 
            // flpBaseDetails
            // 
            this.flpBaseDetails.Controls.Add(this.flpPlanetName);
            this.flpBaseDetails.Controls.Add(this.flpColonyName);
            this.flpBaseDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpBaseDetails.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpBaseDetails.Location = new System.Drawing.Point(2, 2);
            this.flpBaseDetails.Margin = new System.Windows.Forms.Padding(2);
            this.flpBaseDetails.Name = "flpBaseDetails";
            this.flpBaseDetails.Size = new System.Drawing.Size(800, 60);
            this.flpBaseDetails.TabIndex = 6;
            this.flpBaseDetails.WrapContents = false;
            // 
            // flpPlanetName
            // 
            this.flpPlanetName.Controls.Add(this.label3);
            this.flpPlanetName.Controls.Add(this.txtPlanetName);
            this.flpPlanetName.Location = new System.Drawing.Point(2, 2);
            this.flpPlanetName.Margin = new System.Windows.Forms.Padding(2);
            this.flpPlanetName.Name = "flpPlanetName";
            this.flpPlanetName.Size = new System.Drawing.Size(364, 26);
            this.flpPlanetName.TabIndex = 1;
            this.flpPlanetName.WrapContents = false;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.Location = new System.Drawing.Point(2, 4);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(100, 17);
            this.label3.TabIndex = 2;
            this.label3.Text = "Planet Name";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtPlanetName
            // 
            this.txtPlanetName.Location = new System.Drawing.Point(107, 3);
            this.txtPlanetName.Name = "txtPlanetName";
            this.txtPlanetName.Size = new System.Drawing.Size(254, 20);
            this.txtPlanetName.TabIndex = 0;
            // 
            // flpColonyName
            // 
            this.flpColonyName.Controls.Add(this.lblBlueprintType);
            this.flpColonyName.Controls.Add(this.txtColonyName);
            this.flpColonyName.Location = new System.Drawing.Point(2, 32);
            this.flpColonyName.Margin = new System.Windows.Forms.Padding(2);
            this.flpColonyName.Name = "flpColonyName";
            this.flpColonyName.Size = new System.Drawing.Size(364, 26);
            this.flpColonyName.TabIndex = 0;
            this.flpColonyName.WrapContents = false;
            // 
            // lblBlueprintType
            // 
            this.lblBlueprintType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblBlueprintType.Location = new System.Drawing.Point(2, 4);
            this.lblBlueprintType.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblBlueprintType.Name = "lblBlueprintType";
            this.lblBlueprintType.Size = new System.Drawing.Size(100, 17);
            this.lblBlueprintType.TabIndex = 2;
            this.lblBlueprintType.Text = "Colony Name";
            this.lblBlueprintType.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtColonyName
            // 
            this.txtColonyName.Location = new System.Drawing.Point(107, 3);
            this.txtColonyName.Name = "txtColonyName";
            this.txtColonyName.Size = new System.Drawing.Size(254, 20);
            this.txtColonyName.TabIndex = 0;
            // 
            // tabDetailedData
            // 
            this.tabDetailedData.Controls.Add(this.tabPAdministration);
            this.tabDetailedData.Controls.Add(this.tabPStructures);
            this.tabDetailedData.Controls.Add(this.tabPWorkers);
            this.tabDetailedData.Controls.Add(this.tabPWarehousing);
            this.tabDetailedData.Location = new System.Drawing.Point(2, 66);
            this.tabDetailedData.Margin = new System.Windows.Forms.Padding(2);
            this.tabDetailedData.Multiline = true;
            this.tabDetailedData.Name = "tabDetailedData";
            this.tabDetailedData.SelectedIndex = 0;
            this.tabDetailedData.Size = new System.Drawing.Size(800, 200);
            this.tabDetailedData.TabIndex = 8;
            // 
            // tabPAdministration
            // 
            this.tabPAdministration.Location = new System.Drawing.Point(4, 22);
            this.tabPAdministration.Margin = new System.Windows.Forms.Padding(2);
            this.tabPAdministration.Name = "tabPAdministration";
            this.tabPAdministration.Padding = new System.Windows.Forms.Padding(2);
            this.tabPAdministration.Size = new System.Drawing.Size(792, 174);
            this.tabPAdministration.TabIndex = 0;
            this.tabPAdministration.Text = "Administration";
            this.tabPAdministration.UseVisualStyleBackColor = true;
            // 
            // tabPStructures
            // 
            this.tabPStructures.Controls.Add(this.flpStructures);
            this.tabPStructures.Location = new System.Drawing.Point(4, 22);
            this.tabPStructures.Margin = new System.Windows.Forms.Padding(2);
            this.tabPStructures.Name = "tabPStructures";
            this.tabPStructures.Padding = new System.Windows.Forms.Padding(2);
            this.tabPStructures.Size = new System.Drawing.Size(792, 174);
            this.tabPStructures.TabIndex = 1;
            this.tabPStructures.Text = "Structures";
            this.tabPStructures.UseVisualStyleBackColor = true;
            this.tabPStructures.Layout += new System.Windows.Forms.LayoutEventHandler(this.tabPStructures_Layout);
            // 
            // flpStructures
            // 
            this.flpStructures.Controls.Add(this.lvwStructureTypes);
            this.flpStructures.Controls.Add(this.flpStructureData);
            this.flpStructures.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpStructures.Location = new System.Drawing.Point(2, 2);
            this.flpStructures.Name = "flpStructures";
            this.flpStructures.Size = new System.Drawing.Size(788, 170);
            this.flpStructures.TabIndex = 3;
            this.flpStructures.WrapContents = false;
            this.flpStructures.Layout += new System.Windows.Forms.LayoutEventHandler(this.flpStructures_Layout);
            // 
            // lvwStructureTypes
            // 
            this.lvwStructureTypes.FullRowSelect = true;
            this.lvwStructureTypes.HideSelection = false;
            this.lvwStructureTypes.Location = new System.Drawing.Point(3, 3);
            this.lvwStructureTypes.MultiSelect = false;
            this.lvwStructureTypes.Name = "lvwStructureTypes";
            this.lvwStructureTypes.Size = new System.Drawing.Size(130, 100);
            this.lvwStructureTypes.TabIndex = 7;
            this.lvwStructureTypes.UseCompatibleStateImageBehavior = false;
            // 
            // flpStructureData
            // 
            this.flpStructureData.AutoSize = true;
            this.flpStructureData.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpStructureData.Controls.Add(this.flpStatus);
            this.flpStructureData.Controls.Add(this.flpAddBox);
            this.flpStructureData.Controls.Add(this.flpColonyStructure);
            this.flpStructureData.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpStructureData.Location = new System.Drawing.Point(139, 3);
            this.flpStructureData.Name = "flpStructureData";
            this.flpStructureData.Size = new System.Drawing.Size(742, 193);
            this.flpStructureData.TabIndex = 10;
            this.flpStructureData.WrapContents = false;
            this.flpStructureData.Layout += new System.Windows.Forms.LayoutEventHandler(this.flpStructureData_Layout);
            // 
            // flpStatus
            // 
            this.flpStatus.AutoSize = true;
            this.flpStatus.Controls.Add(this.rtbStatus);
            this.flpStatus.Location = new System.Drawing.Point(3, 3);
            this.flpStatus.Name = "flpStatus";
            this.flpStatus.Size = new System.Drawing.Size(586, 46);
            this.flpStatus.TabIndex = 10;
            // 
            // rtbStatus
            // 
            this.rtbStatus.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbStatus.Location = new System.Drawing.Point(3, 3);
            this.rtbStatus.Name = "rtbStatus";
            this.rtbStatus.ReadOnly = true;
            this.rtbStatus.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.rtbStatus.Size = new System.Drawing.Size(580, 40);
            this.rtbStatus.TabIndex = 3;
            this.rtbStatus.Text = "[red]6[/red]/4";
            this.rtbStatus.WordWrap = false;
            // 
            // flpAddBox
            // 
            this.flpAddBox.AutoSize = true;
            this.flpAddBox.Controls.Add(this.txtFilterFlatpack);
            this.flpAddBox.Controls.Add(this.cmbFlatpacks);
            this.flpAddBox.Controls.Add(this.cmdAddFlatpack);
            this.flpAddBox.Location = new System.Drawing.Point(3, 55);
            this.flpAddBox.Name = "flpAddBox";
            this.flpAddBox.Size = new System.Drawing.Size(585, 29);
            this.flpAddBox.TabIndex = 9;
            // 
            // txtFilterFlatpack
            // 
            this.txtFilterFlatpack.Location = new System.Drawing.Point(3, 3);
            this.txtFilterFlatpack.Name = "txtFilterFlatpack";
            this.txtFilterFlatpack.Size = new System.Drawing.Size(275, 20);
            this.txtFilterFlatpack.TabIndex = 0;
            this.txtFilterFlatpack.TextChanged += new System.EventHandler(this.txtFilterFlatpack_TextChanged);
            // 
            // cmbFlatpacks
            // 
            this.cmbFlatpacks.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbFlatpacks.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbFlatpacks.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFlatpacks.FormattingEnabled = true;
            this.cmbFlatpacks.Location = new System.Drawing.Point(284, 3);
            this.cmbFlatpacks.Name = "cmbFlatpacks";
            this.cmbFlatpacks.Size = new System.Drawing.Size(217, 21);
            this.cmbFlatpacks.TabIndex = 1;
            this.cmbFlatpacks.SelectedIndexChanged += new System.EventHandler(this.cmbFlatpacks_SelectedIndexChanged);
            // 
            // cmdAddFlatpack
            // 
            this.cmdAddFlatpack.Location = new System.Drawing.Point(507, 3);
            this.cmdAddFlatpack.Name = "cmdAddFlatpack";
            this.cmdAddFlatpack.Size = new System.Drawing.Size(75, 23);
            this.cmdAddFlatpack.TabIndex = 2;
            this.cmdAddFlatpack.Text = "Add";
            this.cmdAddFlatpack.UseVisualStyleBackColor = true;
            this.cmdAddFlatpack.Click += new System.EventHandler(this.cmdAddFlatpack_Click);
            // 
            // flpColonyStructure
            // 
            this.flpColonyStructure.AutoScroll = true;
            this.flpColonyStructure.AutoScrollMinSize = new System.Drawing.Size(554, 0);
            this.flpColonyStructure.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpColonyStructure.Controls.Add(this.colonyStructure1);
            this.flpColonyStructure.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpColonyStructure.Location = new System.Drawing.Point(3, 90);
            this.flpColonyStructure.Name = "flpColonyStructure";
            this.flpColonyStructure.Size = new System.Drawing.Size(736, 100);
            this.flpColonyStructure.TabIndex = 8;
            this.flpColonyStructure.WrapContents = false;
            // 
            // colonyStructure1
            // 
            this.colonyStructure1.ColonyStructureData = null;
            this.colonyStructure1.Location = new System.Drawing.Point(3, 3);
            this.colonyStructure1.Name = "colonyStructure1";
            this.colonyStructure1.Size = new System.Drawing.Size(580, 168);
            this.colonyStructure1.TabIndex = 8;
            // 
            // tabPWorkers
            // 
            this.tabPWorkers.Location = new System.Drawing.Point(4, 22);
            this.tabPWorkers.Name = "tabPWorkers";
            this.tabPWorkers.Size = new System.Drawing.Size(792, 174);
            this.tabPWorkers.TabIndex = 2;
            this.tabPWorkers.Text = "Workers";
            this.tabPWorkers.UseVisualStyleBackColor = true;
            // 
            // tabPWarehousing
            // 
            this.tabPWarehousing.Controls.Add(this.flowLayoutPanel7);
            this.tabPWarehousing.Location = new System.Drawing.Point(4, 22);
            this.tabPWarehousing.Name = "tabPWarehousing";
            this.tabPWarehousing.Size = new System.Drawing.Size(792, 174);
            this.tabPWarehousing.TabIndex = 3;
            this.tabPWarehousing.Text = "Warehousing";
            this.tabPWarehousing.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel7
            // 
            this.flowLayoutPanel7.Controls.Add(this.flowLayoutPanel9);
            this.flowLayoutPanel7.Controls.Add(this.dgvResources);
            this.flowLayoutPanel7.Dock = System.Windows.Forms.DockStyle.Left;
            this.flowLayoutPanel7.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel7.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel7.Name = "flowLayoutPanel7";
            this.flowLayoutPanel7.Size = new System.Drawing.Size(886, 174);
            this.flowLayoutPanel7.TabIndex = 8;
            this.flowLayoutPanel7.WrapContents = false;
            // 
            // flowLayoutPanel9
            // 
            this.flowLayoutPanel9.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.flowLayoutPanel9.Controls.Add(this.lblItemType);
            this.flowLayoutPanel9.Controls.Add(this.cmbItemType);
            this.flowLayoutPanel9.Controls.Add(this.lblFilter);
            this.flowLayoutPanel9.Controls.Add(this.txtItemFilter);
            this.flowLayoutPanel9.Controls.Add(this.cmbItem);
            this.flowLayoutPanel9.Controls.Add(this.cmbPurity);
            this.flowLayoutPanel9.Controls.Add(this.cmdAdd);
            this.flowLayoutPanel9.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanel9.Name = "flowLayoutPanel9";
            this.flowLayoutPanel9.Size = new System.Drawing.Size(889, 30);
            this.flowLayoutPanel9.TabIndex = 6;
            this.flowLayoutPanel9.WrapContents = false;
            // 
            // lblItemType
            // 
            this.lblItemType.Location = new System.Drawing.Point(3, 0);
            this.lblItemType.Name = "lblItemType";
            this.lblItemType.Size = new System.Drawing.Size(60, 26);
            this.lblItemType.TabIndex = 6;
            this.lblItemType.Text = "Item Type:";
            this.lblItemType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cmbItemType
            // 
            this.cmbItemType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cmbItemType.DisplayMember = "Name";
            this.cmbItemType.FormattingEnabled = true;
            this.cmbItemType.Location = new System.Drawing.Point(69, 4);
            this.cmbItemType.Name = "cmbItemType";
            this.cmbItemType.Size = new System.Drawing.Size(121, 21);
            this.cmbItemType.TabIndex = 0;
            this.cmbItemType.SelectedIndexChanged += new System.EventHandler(this.cmbItemType_SelectedIndexChanged);
            // 
            // lblFilter
            // 
            this.lblFilter.Location = new System.Drawing.Point(196, 0);
            this.lblFilter.Name = "lblFilter";
            this.lblFilter.Size = new System.Drawing.Size(40, 24);
            this.lblFilter.TabIndex = 3;
            this.lblFilter.Text = "Filter:";
            this.lblFilter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtItemFilter
            // 
            this.txtItemFilter.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtItemFilter.Location = new System.Drawing.Point(242, 4);
            this.txtItemFilter.Name = "txtItemFilter";
            this.txtItemFilter.Size = new System.Drawing.Size(187, 20);
            this.txtItemFilter.TabIndex = 4;
            // 
            // cmbItem
            // 
            this.cmbItem.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cmbItem.FormattingEnabled = true;
            this.cmbItem.Location = new System.Drawing.Point(435, 4);
            this.cmbItem.Name = "cmbItem";
            this.cmbItem.Size = new System.Drawing.Size(121, 21);
            this.cmbItem.TabIndex = 5;
            // 
            // cmdAdd
            // 
            this.cmdAdd.Location = new System.Drawing.Point(689, 3);
            this.cmdAdd.Name = "cmdAdd";
            this.cmdAdd.Size = new System.Drawing.Size(39, 23);
            this.cmdAdd.TabIndex = 1;
            this.cmdAdd.Text = "Add";
            this.cmdAdd.UseVisualStyleBackColor = true;
            // 
            // dgvResources
            // 
            this.dgvResources.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvResources.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvResources.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ItemType,
            this.Item,
            this.LockedAmount,
            this.Amount});
            this.dgvResources.Location = new System.Drawing.Point(3, 39);
            this.dgvResources.Name = "dgvResources";
            this.dgvResources.Size = new System.Drawing.Size(889, 430);
            this.dgvResources.TabIndex = 7;
            // 
            // ItemType
            // 
            this.ItemType.HeaderText = "ItemType";
            this.ItemType.Name = "ItemType";
            // 
            // Item
            // 
            this.Item.HeaderText = "Item";
            this.Item.Name = "Item";
            this.Item.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.Item.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.Item.Width = 150;
            // 
            // LockedAmount
            // 
            this.LockedAmount.HeaderText = "Locked Amount";
            this.LockedAmount.Name = "LockedAmount";
            // 
            // Amount
            // 
            this.Amount.HeaderText = "Amount";
            this.Amount.Name = "Amount";
            // 
            // flpCommands
            // 
            this.flpCommands.Controls.Add(this.chkGlobalBlueprint);
            this.flpCommands.Controls.Add(this.cmdSave);
            this.flpCommands.Controls.Add(this.cmdDelete);
            this.flpCommands.Controls.Add(this.cmdCancel);
            this.flpCommands.Location = new System.Drawing.Point(2, 270);
            this.flpCommands.Margin = new System.Windows.Forms.Padding(2);
            this.flpCommands.Name = "flpCommands";
            this.flpCommands.Size = new System.Drawing.Size(800, 29);
            this.flpCommands.TabIndex = 9;
            this.flpCommands.WrapContents = false;
            // 
            // chkGlobalBlueprint
            // 
            this.chkGlobalBlueprint.AutoSize = true;
            this.chkGlobalBlueprint.Location = new System.Drawing.Point(3, 3);
            this.chkGlobalBlueprint.Name = "chkGlobalBlueprint";
            this.chkGlobalBlueprint.Size = new System.Drawing.Size(100, 17);
            this.chkGlobalBlueprint.TabIndex = 8;
            this.chkGlobalBlueprint.Text = "Global Blueprint";
            this.chkGlobalBlueprint.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.chkGlobalBlueprint.UseVisualStyleBackColor = true;
            // 
            // cmdSave
            // 
            this.cmdSave.Location = new System.Drawing.Point(109, 3);
            this.cmdSave.Name = "cmdSave";
            this.cmdSave.Size = new System.Drawing.Size(75, 23);
            this.cmdSave.TabIndex = 9;
            this.cmdSave.Text = "Save";
            this.cmdSave.UseVisualStyleBackColor = true;
            this.cmdSave.Click += new System.EventHandler(this.cmdSave_Click);
            // 
            // cmdDelete
            // 
            this.cmdDelete.Location = new System.Drawing.Point(190, 3);
            this.cmdDelete.Name = "cmdDelete";
            this.cmdDelete.Size = new System.Drawing.Size(75, 23);
            this.cmdDelete.TabIndex = 11;
            this.cmdDelete.Text = "Delete";
            this.cmdDelete.UseVisualStyleBackColor = true;
            // 
            // cmdCancel
            // 
            this.cmdCancel.Location = new System.Drawing.Point(271, 3);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(75, 23);
            this.cmdCancel.TabIndex = 10;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            // 
            // cmbPurity
            // 
            this.cmbPurity.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cmbPurity.FormattingEnabled = true;
            this.cmbPurity.Location = new System.Drawing.Point(562, 4);
            this.cmbPurity.Name = "cmbPurity";
            this.cmbPurity.Size = new System.Drawing.Size(121, 21);
            this.cmbPurity.TabIndex = 7;
            // 
            // FormColony
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1225, 309);
            this.Controls.Add(this.tlpBase);
            this.Name = "FormColony";
            this.Text = "ColonyForm";
            this.flpSearchList.ResumeLayout(false);
            this.flpBlueprintSearch.ResumeLayout(false);
            this.flpBlueprintSearch.PerformLayout();
            this.tlpBase.ResumeLayout(false);
            this.flpColonyData.ResumeLayout(false);
            this.flpBaseDetails.ResumeLayout(false);
            this.flpPlanetName.ResumeLayout(false);
            this.flpPlanetName.PerformLayout();
            this.flpColonyName.ResumeLayout(false);
            this.flpColonyName.PerformLayout();
            this.tabDetailedData.ResumeLayout(false);
            this.tabPStructures.ResumeLayout(false);
            this.flpStructures.ResumeLayout(false);
            this.flpStructures.PerformLayout();
            this.flpStructureData.ResumeLayout(false);
            this.flpStructureData.PerformLayout();
            this.flpStatus.ResumeLayout(false);
            this.flpAddBox.ResumeLayout(false);
            this.flpAddBox.PerformLayout();
            this.flpColonyStructure.ResumeLayout(false);
            this.tabPWarehousing.ResumeLayout(false);
            this.flowLayoutPanel7.ResumeLayout(false);
            this.flowLayoutPanel9.ResumeLayout(false);
            this.flowLayoutPanel9.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResources)).EndInit();
            this.flpCommands.ResumeLayout(false);
            this.flpCommands.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.FlowLayoutPanel flpSearchList;
        private System.Windows.Forms.FlowLayoutPanel flpBlueprintSearch;
        private System.Windows.Forms.Label lblBlueprintListFilter;
        private System.Windows.Forms.TextBox txtBlueprintListFilter;
        private System.Windows.Forms.ListView lvwColonies;
        private DataEntryGridView dgvStatistics;
        private System.Windows.Forms.DataGridViewTextBoxColumn Property;
        private System.Windows.Forms.DataGridViewTextBoxColumn BaseValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn CurrentValue;
        private System.Windows.Forms.TableLayoutPanel tlpBase;
        private System.Windows.Forms.FlowLayoutPanel flpColonyData;
        private System.Windows.Forms.FlowLayoutPanel flpBaseDetails;
        private System.Windows.Forms.FlowLayoutPanel flpPlanetName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtPlanetName;
        private System.Windows.Forms.FlowLayoutPanel flpColonyName;
        private System.Windows.Forms.Label lblBlueprintType;
        private System.Windows.Forms.TextBox txtColonyName;
        private System.Windows.Forms.TabControl tabDetailedData;
        private System.Windows.Forms.TabPage tabPAdministration;
        private System.Windows.Forms.TabPage tabPStructures;
        private System.Windows.Forms.FlowLayoutPanel flpStructures;
        private System.Windows.Forms.ListView lvwStructureTypes;
        private System.Windows.Forms.FlowLayoutPanel flpStructureData;
        private System.Windows.Forms.FlowLayoutPanel flpStatus;
        private System.Windows.Forms.RichTextBox rtbStatus;
        private System.Windows.Forms.FlowLayoutPanel flpAddBox;
        private System.Windows.Forms.TextBox txtFilterFlatpack;
        private System.Windows.Forms.ComboBox cmbFlatpacks;
        private System.Windows.Forms.Button cmdAddFlatpack;
        private System.Windows.Forms.FlowLayoutPanel flpColonyStructure;
        private ColonyStructure colonyStructure1;
        private System.Windows.Forms.TabPage tabPWorkers;
        private System.Windows.Forms.TabPage tabPWarehousing;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel7;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel9;
        private System.Windows.Forms.Label lblItemType;
        private System.Windows.Forms.ComboBox cmbItemType;
        private System.Windows.Forms.Label lblFilter;
        private System.Windows.Forms.TextBox txtItemFilter;
        private System.Windows.Forms.ComboBox cmbItem;
        private System.Windows.Forms.Button cmdAdd;
        private System.Windows.Forms.DataGridView dgvResources;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemType;
        private System.Windows.Forms.DataGridViewTextBoxColumn Item;
        private System.Windows.Forms.DataGridViewTextBoxColumn LockedAmount;
        private System.Windows.Forms.DataGridViewTextBoxColumn Amount;
        private System.Windows.Forms.FlowLayoutPanel flpCommands;
        private System.Windows.Forms.CheckBox chkGlobalBlueprint;
        private System.Windows.Forms.Button cmdSave;
        private System.Windows.Forms.Button cmdDelete;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.ComboBox cmbPurity;
    }
}