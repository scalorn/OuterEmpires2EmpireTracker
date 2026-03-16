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
            this.flpBase = new System.Windows.Forms.FlowLayoutPanel();
            this.flpSearchList = new System.Windows.Forms.FlowLayoutPanel();
            this.flpBlueprintSearch = new System.Windows.Forms.FlowLayoutPanel();
            this.lblBlueprintListFilter = new System.Windows.Forms.Label();
            this.txtBlueprintListFilter = new System.Windows.Forms.TextBox();
            this.lvwColonies = new System.Windows.Forms.ListView();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.flpBaseDetails = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel6 = new System.Windows.Forms.FlowLayoutPanel();
            this.label3 = new System.Windows.Forms.Label();
            this.txtPlanetName = new System.Windows.Forms.TextBox();
            this.flpBlueprintType = new System.Windows.Forms.FlowLayoutPanel();
            this.lblBlueprintType = new System.Windows.Forms.Label();
            this.txtColonyName = new System.Windows.Forms.TextBox();
            this.tabDetailedData = new System.Windows.Forms.TabControl();
            this.tabPAdministration = new System.Windows.Forms.TabPage();
            this.tabPStructures = new System.Windows.Forms.TabPage();
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.listView1 = new System.Windows.Forms.ListView();
            this.flowLayoutPanel4 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel5 = new System.Windows.Forms.FlowLayoutPanel();
            this.rtbStatus = new System.Windows.Forms.RichTextBox();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.txtFilterFlatpack = new System.Windows.Forms.TextBox();
            this.cmbFlatpacks = new System.Windows.Forms.ComboBox();
            this.cmdAddFlatpack = new System.Windows.Forms.Button();
            this.flpColonyStructure = new System.Windows.Forms.FlowLayoutPanel();
            this.colonyStructure1 = new OE2EmpireTracker.Forms.Colony.ColonyStructure();
            this.tabPWorkers = new System.Windows.Forms.TabPage();
            this.tabPWarehousing = new System.Windows.Forms.TabPage();
            this.flpCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.chkGlobalBlueprint = new System.Windows.Forms.CheckBox();
            this.cmdSave = new System.Windows.Forms.Button();
            this.cmdDelete = new System.Windows.Forms.Button();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.Property = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BaseValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CurrentValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpBase.SuspendLayout();
            this.flpSearchList.SuspendLayout();
            this.flpBlueprintSearch.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.flpBaseDetails.SuspendLayout();
            this.flowLayoutPanel6.SuspendLayout();
            this.flpBlueprintType.SuspendLayout();
            this.tabDetailedData.SuspendLayout();
            this.tabPStructures.SuspendLayout();
            this.flowLayoutPanel3.SuspendLayout();
            this.flowLayoutPanel4.SuspendLayout();
            this.flowLayoutPanel5.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.flpColonyStructure.SuspendLayout();
            this.flpCommands.SuspendLayout();
            this.SuspendLayout();
            // 
            // flpBase
            // 
            this.flpBase.AutoSize = true;
            this.flpBase.Controls.Add(this.flpSearchList);
            this.flpBase.Controls.Add(this.flowLayoutPanel1);
            this.flpBase.Location = new System.Drawing.Point(0, 0);
            this.flpBase.Name = "flpBase";
            this.flpBase.Size = new System.Drawing.Size(1339, 708);
            this.flpBase.TabIndex = 10;
            // 
            // flpSearchList
            // 
            this.flpSearchList.Controls.Add(this.flpBlueprintSearch);
            this.flpSearchList.Controls.Add(this.lvwColonies);
            this.flpSearchList.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpSearchList.Location = new System.Drawing.Point(3, 3);
            this.flpSearchList.Name = "flpSearchList";
            this.flpSearchList.Size = new System.Drawing.Size(266, 641);
            this.flpSearchList.TabIndex = 10;
            this.flpSearchList.WrapContents = false;
            // 
            // flpBlueprintSearch
            // 
            this.flpBlueprintSearch.AutoSize = true;
            this.flpBlueprintSearch.Controls.Add(this.lblBlueprintListFilter);
            this.flpBlueprintSearch.Controls.Add(this.txtBlueprintListFilter);
            this.flpBlueprintSearch.Location = new System.Drawing.Point(2, 2);
            this.flpBlueprintSearch.Margin = new System.Windows.Forms.Padding(2);
            this.flpBlueprintSearch.Name = "flpBlueprintSearch";
            this.flpBlueprintSearch.Size = new System.Drawing.Size(164, 26);
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
            this.lvwColonies.Size = new System.Drawing.Size(255, 566);
            this.lvwColonies.TabIndex = 6;
            this.lvwColonies.UseCompatibleStateImageBehavior = false;
            this.lvwColonies.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.lvwColonies_ItemSelectionChanged);
            this.lvwColonies.SelectedIndexChanged += new System.EventHandler(this.lvwColonies_SelectedIndexChanged);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.flpBaseDetails);
            this.flowLayoutPanel1.Controls.Add(this.tabDetailedData);
            this.flowLayoutPanel1.Controls.Add(this.flpCommands);
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(275, 3);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(900, 641);
            this.flowLayoutPanel1.TabIndex = 10;
            // 
            // flpBaseDetails
            // 
            this.flpBaseDetails.AutoSize = true;
            this.flpBaseDetails.Controls.Add(this.flowLayoutPanel6);
            this.flpBaseDetails.Controls.Add(this.flpBlueprintType);
            this.flpBaseDetails.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpBaseDetails.Location = new System.Drawing.Point(2, 2);
            this.flpBaseDetails.Margin = new System.Windows.Forms.Padding(2);
            this.flpBaseDetails.Name = "flpBaseDetails";
            this.flpBaseDetails.Size = new System.Drawing.Size(368, 60);
            this.flpBaseDetails.TabIndex = 6;
            // 
            // flowLayoutPanel6
            // 
            this.flowLayoutPanel6.AutoSize = true;
            this.flowLayoutPanel6.Controls.Add(this.label3);
            this.flowLayoutPanel6.Controls.Add(this.txtPlanetName);
            this.flowLayoutPanel6.Location = new System.Drawing.Point(2, 2);
            this.flowLayoutPanel6.Margin = new System.Windows.Forms.Padding(2);
            this.flowLayoutPanel6.Name = "flowLayoutPanel6";
            this.flowLayoutPanel6.Size = new System.Drawing.Size(364, 26);
            this.flowLayoutPanel6.TabIndex = 1;
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
            // flpBlueprintType
            // 
            this.flpBlueprintType.AutoSize = true;
            this.flpBlueprintType.Controls.Add(this.lblBlueprintType);
            this.flpBlueprintType.Controls.Add(this.txtColonyName);
            this.flpBlueprintType.Location = new System.Drawing.Point(2, 32);
            this.flpBlueprintType.Margin = new System.Windows.Forms.Padding(2);
            this.flpBlueprintType.Name = "flpBlueprintType";
            this.flpBlueprintType.Size = new System.Drawing.Size(364, 26);
            this.flpBlueprintType.TabIndex = 0;
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
            this.tabDetailedData.Size = new System.Drawing.Size(900, 500);
            this.tabDetailedData.TabIndex = 8;
            // 
            // tabPAdministration
            // 
            this.tabPAdministration.Location = new System.Drawing.Point(4, 22);
            this.tabPAdministration.Margin = new System.Windows.Forms.Padding(2);
            this.tabPAdministration.Name = "tabPAdministration";
            this.tabPAdministration.Padding = new System.Windows.Forms.Padding(2);
            this.tabPAdministration.Size = new System.Drawing.Size(892, 474);
            this.tabPAdministration.TabIndex = 0;
            this.tabPAdministration.Text = "Administration";
            this.tabPAdministration.UseVisualStyleBackColor = true;
            // 
            // tabPStructures
            // 
            this.tabPStructures.Controls.Add(this.flowLayoutPanel3);
            this.tabPStructures.Location = new System.Drawing.Point(4, 22);
            this.tabPStructures.Margin = new System.Windows.Forms.Padding(2);
            this.tabPStructures.Name = "tabPStructures";
            this.tabPStructures.Padding = new System.Windows.Forms.Padding(2);
            this.tabPStructures.Size = new System.Drawing.Size(892, 474);
            this.tabPStructures.TabIndex = 1;
            this.tabPStructures.Text = "Structures";
            this.tabPStructures.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel3
            // 
            this.flowLayoutPanel3.Controls.Add(this.listView1);
            this.flowLayoutPanel3.Controls.Add(this.flowLayoutPanel4);
            this.flowLayoutPanel3.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.flowLayoutPanel3.Size = new System.Drawing.Size(887, 470);
            this.flowLayoutPanel3.TabIndex = 3;
            this.flowLayoutPanel3.WrapContents = false;
            // 
            // listView1
            // 
            this.listView1.FullRowSelect = true;
            this.listView1.HideSelection = false;
            this.listView1.Location = new System.Drawing.Point(3, 3);
            this.listView1.MultiSelect = false;
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(130, 450);
            this.listView1.TabIndex = 7;
            this.listView1.UseCompatibleStateImageBehavior = false;
            // 
            // flowLayoutPanel4
            // 
            this.flowLayoutPanel4.AutoSize = true;
            this.flowLayoutPanel4.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel4.Controls.Add(this.flowLayoutPanel5);
            this.flowLayoutPanel4.Controls.Add(this.flowLayoutPanel2);
            this.flowLayoutPanel4.Controls.Add(this.flpColonyStructure);
            this.flowLayoutPanel4.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel4.Location = new System.Drawing.Point(139, 3);
            this.flowLayoutPanel4.Name = "flowLayoutPanel4";
            this.flowLayoutPanel4.Size = new System.Drawing.Size(742, 521);
            this.flowLayoutPanel4.TabIndex = 10;
            this.flowLayoutPanel4.WrapContents = false;
            // 
            // flowLayoutPanel5
            // 
            this.flowLayoutPanel5.AutoSize = true;
            this.flowLayoutPanel5.Controls.Add(this.rtbStatus);
            this.flowLayoutPanel5.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanel5.Name = "flowLayoutPanel5";
            this.flowLayoutPanel5.Size = new System.Drawing.Size(586, 24);
            this.flowLayoutPanel5.TabIndex = 10;
            // 
            // rtbStatus
            // 
            this.rtbStatus.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbStatus.Location = new System.Drawing.Point(3, 3);
            this.rtbStatus.Name = "rtbStatus";
            this.rtbStatus.ReadOnly = true;
            this.rtbStatus.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.rtbStatus.Size = new System.Drawing.Size(580, 18);
            this.rtbStatus.TabIndex = 3;
            this.rtbStatus.Text = "[red]6[/red]/4";
            this.rtbStatus.WordWrap = false;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.AutoSize = true;
            this.flowLayoutPanel2.Controls.Add(this.txtFilterFlatpack);
            this.flowLayoutPanel2.Controls.Add(this.cmbFlatpacks);
            this.flowLayoutPanel2.Controls.Add(this.cmdAddFlatpack);
            this.flowLayoutPanel2.Location = new System.Drawing.Point(3, 33);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(585, 29);
            this.flowLayoutPanel2.TabIndex = 9;
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
            this.flpColonyStructure.AutoScrollMinSize = new System.Drawing.Size(554, 295);
            this.flpColonyStructure.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpColonyStructure.Controls.Add(this.colonyStructure1);
            this.flpColonyStructure.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpColonyStructure.Location = new System.Drawing.Point(3, 68);
            this.flpColonyStructure.Name = "flpColonyStructure";
            this.flpColonyStructure.Size = new System.Drawing.Size(736, 450);
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
            this.tabPWorkers.Size = new System.Drawing.Size(892, 474);
            this.tabPWorkers.TabIndex = 2;
            this.tabPWorkers.Text = "Workers";
            this.tabPWorkers.UseVisualStyleBackColor = true;
            // 
            // tabPWarehousing
            // 
            this.tabPWarehousing.Location = new System.Drawing.Point(4, 22);
            this.tabPWarehousing.Name = "tabPWarehousing";
            this.tabPWarehousing.Size = new System.Drawing.Size(892, 474);
            this.tabPWarehousing.TabIndex = 3;
            this.tabPWarehousing.Text = "Warehousing";
            this.tabPWarehousing.UseVisualStyleBackColor = true;
            // 
            // flpCommands
            // 
            this.flpCommands.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpCommands.AutoSize = true;
            this.flpCommands.Controls.Add(this.chkGlobalBlueprint);
            this.flpCommands.Controls.Add(this.cmdSave);
            this.flpCommands.Controls.Add(this.cmdDelete);
            this.flpCommands.Controls.Add(this.cmdCancel);
            this.flpCommands.Location = new System.Drawing.Point(2, 570);
            this.flpCommands.Margin = new System.Windows.Forms.Padding(2);
            this.flpCommands.Name = "flpCommands";
            this.flpCommands.Size = new System.Drawing.Size(900, 29);
            this.flpCommands.TabIndex = 9;
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
            // FormColony
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1225, 708);
            this.Controls.Add(this.flpBase);
            this.Name = "FormColony";
            this.Text = "ColonyForm";
            this.flpBase.ResumeLayout(false);
            this.flpSearchList.ResumeLayout(false);
            this.flpSearchList.PerformLayout();
            this.flpBlueprintSearch.ResumeLayout(false);
            this.flpBlueprintSearch.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.flpBaseDetails.ResumeLayout(false);
            this.flpBaseDetails.PerformLayout();
            this.flowLayoutPanel6.ResumeLayout(false);
            this.flowLayoutPanel6.PerformLayout();
            this.flpBlueprintType.ResumeLayout(false);
            this.flpBlueprintType.PerformLayout();
            this.tabDetailedData.ResumeLayout(false);
            this.tabPStructures.ResumeLayout(false);
            this.flowLayoutPanel3.ResumeLayout(false);
            this.flowLayoutPanel3.PerformLayout();
            this.flowLayoutPanel4.ResumeLayout(false);
            this.flowLayoutPanel4.PerformLayout();
            this.flowLayoutPanel5.ResumeLayout(false);
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            this.flpColonyStructure.ResumeLayout(false);
            this.flpCommands.ResumeLayout(false);
            this.flpCommands.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flpBase;
        private System.Windows.Forms.FlowLayoutPanel flpSearchList;
        private System.Windows.Forms.FlowLayoutPanel flpBlueprintSearch;
        private System.Windows.Forms.Label lblBlueprintListFilter;
        private System.Windows.Forms.TextBox txtBlueprintListFilter;
        private System.Windows.Forms.ListView lvwColonies;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flpBaseDetails;
        private System.Windows.Forms.FlowLayoutPanel flpBlueprintType;
        private System.Windows.Forms.Label lblBlueprintType;
        private System.Windows.Forms.TextBox txtColonyName;
        private System.Windows.Forms.TabControl tabDetailedData;
        private System.Windows.Forms.TabPage tabPAdministration;
        private DataEntryGridView dgvStatistics;
        private System.Windows.Forms.DataGridViewTextBoxColumn Property;
        private System.Windows.Forms.DataGridViewTextBoxColumn BaseValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn CurrentValue;
        private System.Windows.Forms.TabPage tabPStructures;
        private System.Windows.Forms.FlowLayoutPanel flpCommands;
        private System.Windows.Forms.CheckBox chkGlobalBlueprint;
        private System.Windows.Forms.Button cmdSave;
        private System.Windows.Forms.Button cmdDelete;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.TabPage tabPWorkers;
        private System.Windows.Forms.TabPage tabPWarehousing;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.FlowLayoutPanel flpColonyStructure;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel6;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtPlanetName;
        private ColonyStructure colonyStructure1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.TextBox txtFilterFlatpack;
        private System.Windows.Forms.ComboBox cmbFlatpacks;
        private System.Windows.Forms.Button cmdAddFlatpack;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel4;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel5;
        private System.Windows.Forms.RichTextBox rtbStatus;
    }
}