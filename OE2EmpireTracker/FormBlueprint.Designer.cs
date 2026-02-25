using OE2EmpireTracker.Baseline;
using System;

namespace OE2EmpireTracker
{
    partial class FormBlueprint
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
            this.lblBlueprintType = new System.Windows.Forms.Label();
            this.cmbBlueprintType = new System.Windows.Forms.ComboBox();
            this.flpBlueprintType = new System.Windows.Forms.FlowLayoutPanel();
            this.flpTechLevel = new System.Windows.Forms.FlowLayoutPanel();
            this.lblTechLevel = new System.Windows.Forms.Label();
            this.cmbTechLevel = new System.Windows.Forms.ComboBox();
            this.flpBaseDetails = new System.Windows.Forms.FlowLayoutPanel();
            this.flpClass = new System.Windows.Forms.FlowLayoutPanel();
            this.lblShipClass = new System.Windows.Forms.Label();
            this.cmbShipClass = new System.Windows.Forms.ComboBox();
            this.flpEvolution = new System.Windows.Forms.FlowLayoutPanel();
            this.lblEvolution = new System.Windows.Forms.Label();
            this.cmbEvolution = new System.Windows.Forms.ComboBox();
            this.flpBaseBlueprint = new System.Windows.Forms.FlowLayoutPanel();
            this.lblBaseBlueprint = new System.Windows.Forms.Label();
            this.cmbBaseBlueprint = new System.Windows.Forms.ComboBox();
            this.flpName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.flpDescription = new System.Windows.Forms.FlowLayoutPanel();
            this.lblDescription = new System.Windows.Forms.Label();
            this.txtDescription = new System.Windows.Forms.TextBox();
            this.flpCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.chkGlobalBlueprint = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.tabDetailedData = new System.Windows.Forms.TabControl();
            this.tabPStatistics = new System.Windows.Forms.TabPage();
            this.tabPResources = new System.Windows.Forms.TabPage();
            this.dgvResources = new System.Windows.Forms.DataGridView();
            this.Column1 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpBase = new System.Windows.Forms.FlowLayoutPanel();
            this.dgvStatistics = new DataEntryGridView();
            this.Property = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BaseValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CurrentValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpBlueprintType.SuspendLayout();
            this.flpTechLevel.SuspendLayout();
            this.flpBaseDetails.SuspendLayout();
            this.flpClass.SuspendLayout();
            this.flpEvolution.SuspendLayout();
            this.flpBaseBlueprint.SuspendLayout();
            this.flpName.SuspendLayout();
            this.flpDescription.SuspendLayout();
            this.flpCommands.SuspendLayout();
            this.tabDetailedData.SuspendLayout();
            this.tabPStatistics.SuspendLayout();
            this.tabPResources.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResources)).BeginInit();
            this.flpBase.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStatistics)).BeginInit();
            this.SuspendLayout();
            // 
            // lblBlueprintType
            // 
            this.lblBlueprintType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblBlueprintType.Location = new System.Drawing.Point(2, 4);
            this.lblBlueprintType.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblBlueprintType.Name = "lblBlueprintType";
            this.lblBlueprintType.Size = new System.Drawing.Size(100, 17);
            this.lblBlueprintType.TabIndex = 2;
            this.lblBlueprintType.Text = "Blueprint Type";
            this.lblBlueprintType.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbBlueprintType
            // 
            this.cmbBlueprintType.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbBlueprintType.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbBlueprintType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBlueprintType.FormattingEnabled = true;
            this.cmbBlueprintType.Location = new System.Drawing.Point(106, 2);
            this.cmbBlueprintType.Margin = new System.Windows.Forms.Padding(2);
            this.cmbBlueprintType.Name = "cmbBlueprintType";
            this.cmbBlueprintType.Size = new System.Drawing.Size(201, 21);
            this.cmbBlueprintType.TabIndex = 1;
            this.cmbBlueprintType.SelectedIndexChanged += new System.EventHandler(this.cmbBlueprintType_SelectedIndexChanged);
            // 
            // flpBlueprintType
            // 
            this.flpBlueprintType.AutoSize = true;
            this.flpBlueprintType.Controls.Add(this.lblBlueprintType);
            this.flpBlueprintType.Controls.Add(this.cmbBlueprintType);
            this.flpBlueprintType.Location = new System.Drawing.Point(2, 2);
            this.flpBlueprintType.Margin = new System.Windows.Forms.Padding(2);
            this.flpBlueprintType.Name = "flpBlueprintType";
            this.flpBlueprintType.Size = new System.Drawing.Size(309, 25);
            this.flpBlueprintType.TabIndex = 0;
            // 
            // flpTechLevel
            // 
            this.flpTechLevel.AutoSize = true;
            this.flpTechLevel.Controls.Add(this.lblTechLevel);
            this.flpTechLevel.Controls.Add(this.cmbTechLevel);
            this.flpTechLevel.Location = new System.Drawing.Point(2, 60);
            this.flpTechLevel.Margin = new System.Windows.Forms.Padding(2);
            this.flpTechLevel.Name = "flpTechLevel";
            this.flpTechLevel.Size = new System.Drawing.Size(309, 25);
            this.flpTechLevel.TabIndex = 2;
            // 
            // lblTechLevel
            // 
            this.lblTechLevel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTechLevel.Location = new System.Drawing.Point(2, 4);
            this.lblTechLevel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblTechLevel.Name = "lblTechLevel";
            this.lblTechLevel.Size = new System.Drawing.Size(100, 17);
            this.lblTechLevel.TabIndex = 2;
            this.lblTechLevel.Text = "Tech Level";
            this.lblTechLevel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbTechLevel
            // 
            this.cmbTechLevel.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbTechLevel.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbTechLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTechLevel.FormattingEnabled = true;
            this.cmbTechLevel.Location = new System.Drawing.Point(106, 2);
            this.cmbTechLevel.Margin = new System.Windows.Forms.Padding(2);
            this.cmbTechLevel.Name = "cmbTechLevel";
            this.cmbTechLevel.Size = new System.Drawing.Size(201, 21);
            this.cmbTechLevel.TabIndex = 3;
            // 
            // flpBaseDetails
            // 
            this.flpBaseDetails.AutoSize = true;
            this.flpBaseDetails.Controls.Add(this.flpBlueprintType);
            this.flpBaseDetails.Controls.Add(this.flpClass);
            this.flpBaseDetails.Controls.Add(this.flpTechLevel);
            this.flpBaseDetails.Controls.Add(this.flpEvolution);
            this.flpBaseDetails.Controls.Add(this.flpBaseBlueprint);
            this.flpBaseDetails.Controls.Add(this.flpName);
            this.flpBaseDetails.Controls.Add(this.flpDescription);
            this.flpBaseDetails.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpBaseDetails.Location = new System.Drawing.Point(2, 2);
            this.flpBaseDetails.Margin = new System.Windows.Forms.Padding(2);
            this.flpBaseDetails.Name = "flpBaseDetails";
            this.flpBaseDetails.Size = new System.Drawing.Size(313, 201);
            this.flpBaseDetails.TabIndex = 6;
            // 
            // flpClass
            // 
            this.flpClass.AutoSize = true;
            this.flpClass.Controls.Add(this.lblShipClass);
            this.flpClass.Controls.Add(this.cmbShipClass);
            this.flpClass.Location = new System.Drawing.Point(2, 31);
            this.flpClass.Margin = new System.Windows.Forms.Padding(2);
            this.flpClass.Name = "flpClass";
            this.flpClass.Size = new System.Drawing.Size(309, 25);
            this.flpClass.TabIndex = 1;
            // 
            // lblShipClass
            // 
            this.lblShipClass.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblShipClass.Location = new System.Drawing.Point(2, 4);
            this.lblShipClass.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblShipClass.Name = "lblShipClass";
            this.lblShipClass.Size = new System.Drawing.Size(100, 17);
            this.lblShipClass.TabIndex = 2;
            this.lblShipClass.Text = "Class";
            this.lblShipClass.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbShipClass
            // 
            this.cmbShipClass.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbShipClass.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbShipClass.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbShipClass.FormattingEnabled = true;
            this.cmbShipClass.Location = new System.Drawing.Point(106, 2);
            this.cmbShipClass.Margin = new System.Windows.Forms.Padding(2);
            this.cmbShipClass.Name = "cmbShipClass";
            this.cmbShipClass.Size = new System.Drawing.Size(201, 21);
            this.cmbShipClass.TabIndex = 1;
            // 
            // flpEvolution
            // 
            this.flpEvolution.AutoSize = true;
            this.flpEvolution.Controls.Add(this.lblEvolution);
            this.flpEvolution.Controls.Add(this.cmbEvolution);
            this.flpEvolution.Location = new System.Drawing.Point(2, 89);
            this.flpEvolution.Margin = new System.Windows.Forms.Padding(2);
            this.flpEvolution.Name = "flpEvolution";
            this.flpEvolution.Size = new System.Drawing.Size(309, 25);
            this.flpEvolution.TabIndex = 3;
            // 
            // lblEvolution
            // 
            this.lblEvolution.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblEvolution.Location = new System.Drawing.Point(2, 4);
            this.lblEvolution.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblEvolution.Name = "lblEvolution";
            this.lblEvolution.Size = new System.Drawing.Size(100, 17);
            this.lblEvolution.TabIndex = 2;
            this.lblEvolution.Text = "Evolution";
            this.lblEvolution.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbEvolution
            // 
            this.cmbEvolution.FormattingEnabled = true;
            this.cmbEvolution.Location = new System.Drawing.Point(106, 2);
            this.cmbEvolution.Margin = new System.Windows.Forms.Padding(2);
            this.cmbEvolution.Name = "cmbEvolution";
            this.cmbEvolution.Size = new System.Drawing.Size(201, 21);
            this.cmbEvolution.TabIndex = 4;
            // 
            // flpBaseBlueprint
            // 
            this.flpBaseBlueprint.AutoSize = true;
            this.flpBaseBlueprint.Controls.Add(this.lblBaseBlueprint);
            this.flpBaseBlueprint.Controls.Add(this.cmbBaseBlueprint);
            this.flpBaseBlueprint.Location = new System.Drawing.Point(2, 118);
            this.flpBaseBlueprint.Margin = new System.Windows.Forms.Padding(2);
            this.flpBaseBlueprint.Name = "flpBaseBlueprint";
            this.flpBaseBlueprint.Size = new System.Drawing.Size(309, 25);
            this.flpBaseBlueprint.TabIndex = 4;
            // 
            // lblBaseBlueprint
            // 
            this.lblBaseBlueprint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblBaseBlueprint.Location = new System.Drawing.Point(2, 4);
            this.lblBaseBlueprint.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblBaseBlueprint.Name = "lblBaseBlueprint";
            this.lblBaseBlueprint.Size = new System.Drawing.Size(100, 17);
            this.lblBaseBlueprint.TabIndex = 2;
            this.lblBaseBlueprint.Text = "Base Blueprint";
            this.lblBaseBlueprint.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbBaseBlueprint
            // 
            this.cmbBaseBlueprint.FormattingEnabled = true;
            this.cmbBaseBlueprint.Location = new System.Drawing.Point(106, 2);
            this.cmbBaseBlueprint.Margin = new System.Windows.Forms.Padding(2);
            this.cmbBaseBlueprint.Name = "cmbBaseBlueprint";
            this.cmbBaseBlueprint.Size = new System.Drawing.Size(201, 21);
            this.cmbBaseBlueprint.TabIndex = 5;
            // 
            // flpName
            // 
            this.flpName.AutoSize = true;
            this.flpName.Controls.Add(this.lblName);
            this.flpName.Controls.Add(this.txtName);
            this.flpName.Location = new System.Drawing.Point(2, 147);
            this.flpName.Margin = new System.Windows.Forms.Padding(2);
            this.flpName.Name = "flpName";
            this.flpName.Size = new System.Drawing.Size(309, 24);
            this.flpName.TabIndex = 5;
            // 
            // lblName
            // 
            this.lblName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblName.Location = new System.Drawing.Point(2, 3);
            this.lblName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(100, 17);
            this.lblName.TabIndex = 2;
            this.lblName.Text = "Name";
            this.lblName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(106, 2);
            this.txtName.Margin = new System.Windows.Forms.Padding(2);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(201, 20);
            this.txtName.TabIndex = 0;
            // 
            // flpDescription
            // 
            this.flpDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpDescription.AutoSize = true;
            this.flpDescription.Controls.Add(this.lblDescription);
            this.flpDescription.Controls.Add(this.txtDescription);
            this.flpDescription.Location = new System.Drawing.Point(2, 175);
            this.flpDescription.Margin = new System.Windows.Forms.Padding(2);
            this.flpDescription.Name = "flpDescription";
            this.flpDescription.Size = new System.Drawing.Size(309, 24);
            this.flpDescription.TabIndex = 6;
            // 
            // lblDescription
            // 
            this.lblDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblDescription.Location = new System.Drawing.Point(2, 3);
            this.lblDescription.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(100, 17);
            this.lblDescription.TabIndex = 2;
            this.lblDescription.Text = "Description";
            this.lblDescription.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtDescription
            // 
            this.txtDescription.Location = new System.Drawing.Point(106, 2);
            this.txtDescription.Margin = new System.Windows.Forms.Padding(2);
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.Size = new System.Drawing.Size(201, 20);
            this.txtDescription.TabIndex = 7;
            // 
            // flpCommands
            // 
            this.flpCommands.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpCommands.AutoSize = true;
            this.flpCommands.Controls.Add(this.chkGlobalBlueprint);
            this.flpCommands.Controls.Add(this.btnSave);
            this.flpCommands.Controls.Add(this.btnCancel);
            this.flpCommands.Location = new System.Drawing.Point(2, 540);
            this.flpCommands.Margin = new System.Windows.Forms.Padding(2);
            this.flpCommands.Name = "flpCommands";
            this.flpCommands.Size = new System.Drawing.Size(834, 29);
            this.flpCommands.TabIndex = 8;
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
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(109, 3);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 9;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(190, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 10;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // tabDetailedData
            // 
            this.tabDetailedData.Controls.Add(this.tabPStatistics);
            this.tabDetailedData.Controls.Add(this.tabPResources);
            this.tabDetailedData.Location = new System.Drawing.Point(2, 207);
            this.tabDetailedData.Margin = new System.Windows.Forms.Padding(2);
            this.tabDetailedData.Name = "tabDetailedData";
            this.tabDetailedData.SelectedIndex = 0;
            this.tabDetailedData.Size = new System.Drawing.Size(834, 329);
            this.tabDetailedData.TabIndex = 7;
            // 
            // tabPStatistics
            // 
            this.tabPStatistics.Controls.Add(this.dgvStatistics);
            this.tabPStatistics.Location = new System.Drawing.Point(4, 22);
            this.tabPStatistics.Margin = new System.Windows.Forms.Padding(2);
            this.tabPStatistics.Name = "tabPStatistics";
            this.tabPStatistics.Padding = new System.Windows.Forms.Padding(2);
            this.tabPStatistics.Size = new System.Drawing.Size(826, 303);
            this.tabPStatistics.TabIndex = 0;
            this.tabPStatistics.Text = "Statistics";
            this.tabPStatistics.UseVisualStyleBackColor = true;
            // 
            // tabPResources
            // 
            this.tabPResources.Controls.Add(this.dgvResources);
            this.tabPResources.Location = new System.Drawing.Point(4, 22);
            this.tabPResources.Margin = new System.Windows.Forms.Padding(2);
            this.tabPResources.Name = "tabPResources";
            this.tabPResources.Padding = new System.Windows.Forms.Padding(2);
            this.tabPResources.Size = new System.Drawing.Size(826, 303);
            this.tabPResources.TabIndex = 1;
            this.tabPResources.Text = "Required Resources";
            this.tabPResources.UseVisualStyleBackColor = true;
            // 
            // dgvResources
            // 
            this.dgvResources.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvResources.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1,
            this.Column2});
            this.dgvResources.Location = new System.Drawing.Point(1, 0);
            this.dgvResources.Name = "dgvResources";
            this.dgvResources.Size = new System.Drawing.Size(825, 303);
            this.dgvResources.TabIndex = 0;
            // 
            // Column1
            // 
            this.Column1.HeaderText = "Resource";
            this.Column1.Name = "Column1";
            // 
            // Column2
            // 
            this.Column2.HeaderText = "Amount";
            this.Column2.Name = "Column2";
            // 
            // flpBase
            // 
            this.flpBase.AutoSize = true;
            this.flpBase.Controls.Add(this.flpBaseDetails);
            this.flpBase.Controls.Add(this.tabDetailedData);
            this.flpBase.Controls.Add(this.flpCommands);
            this.flpBase.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpBase.Location = new System.Drawing.Point(0, 0);
            this.flpBase.Name = "flpBase";
            this.flpBase.Size = new System.Drawing.Size(844, 579);
            this.flpBase.TabIndex = 9;
            // 
            // dgvStatistics
            // 
            this.dgvStatistics.AllowUserToAddRows = false;
            this.dgvStatistics.AllowUserToDeleteRows = false;
            this.dgvStatistics.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvStatistics.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Property,
            this.BaseValue,
            this.CurrentValue});
            this.dgvStatistics.Location = new System.Drawing.Point(-2, 0);
            this.dgvStatistics.Name = "dgvStatistics";
            this.dgvStatistics.Size = new System.Drawing.Size(828, 298);
            this.dgvStatistics.TabIndex = 1;
            this.dgvStatistics.SelectionChanged += new System.EventHandler(this.dgvStatistics_SelectionChanged);
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
            // FormBlueprint
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(845, 580);
            this.Controls.Add(this.flpBase);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "FormBlueprint";
            this.Text = "Blueprint";
            this.flpBlueprintType.ResumeLayout(false);
            this.flpTechLevel.ResumeLayout(false);
            this.flpBaseDetails.ResumeLayout(false);
            this.flpBaseDetails.PerformLayout();
            this.flpClass.ResumeLayout(false);
            this.flpEvolution.ResumeLayout(false);
            this.flpBaseBlueprint.ResumeLayout(false);
            this.flpName.ResumeLayout(false);
            this.flpName.PerformLayout();
            this.flpDescription.ResumeLayout(false);
            this.flpDescription.PerformLayout();
            this.flpCommands.ResumeLayout(false);
            this.flpCommands.PerformLayout();
            this.tabDetailedData.ResumeLayout(false);
            this.tabPStatistics.ResumeLayout(false);
            this.tabPResources.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvResources)).EndInit();
            this.flpBase.ResumeLayout(false);
            this.flpBase.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStatistics)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label lblBlueprintType;
        private System.Windows.Forms.ComboBox cmbBlueprintType;
        private System.Windows.Forms.FlowLayoutPanel flpBlueprintType;
        private System.Windows.Forms.FlowLayoutPanel flpTechLevel;
        private System.Windows.Forms.Label lblTechLevel;
        private System.Windows.Forms.ComboBox cmbTechLevel;
        private System.Windows.Forms.FlowLayoutPanel flpBaseDetails;
        private System.Windows.Forms.FlowLayoutPanel flpName;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.FlowLayoutPanel flpEvolution;
        private System.Windows.Forms.Label lblEvolution;
        private System.Windows.Forms.FlowLayoutPanel flpDescription;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.TextBox txtDescription;
        private System.Windows.Forms.TabControl tabDetailedData;
        private System.Windows.Forms.TabPage tabPStatistics;
        private System.Windows.Forms.TabPage tabPResources;
        private System.Windows.Forms.FlowLayoutPanel flpBaseBlueprint;
        private System.Windows.Forms.Label lblBaseBlueprint;
        private System.Windows.Forms.ComboBox cmbBaseBlueprint;
        private System.Windows.Forms.ComboBox cmbEvolution;
        private System.Windows.Forms.FlowLayoutPanel flpClass;
        private System.Windows.Forms.Label lblShipClass;
        private System.Windows.Forms.ComboBox cmbShipClass;
        private System.Windows.Forms.FlowLayoutPanel flpBase;
        private System.Windows.Forms.FlowLayoutPanel flpCommands;
        private System.Windows.Forms.CheckBox chkGlobalBlueprint;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.DataGridView dgvResources;
        private System.Windows.Forms.DataGridViewComboBoxColumn Column1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        private DataEntryGridView dgvStatistics;
        private System.Windows.Forms.DataGridViewTextBoxColumn Property;
        private System.Windows.Forms.DataGridViewTextBoxColumn BaseValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn CurrentValue;
    }
}