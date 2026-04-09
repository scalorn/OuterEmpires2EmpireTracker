using OE2EmpireTracker.Services;
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
            this.flpBlueprintType = new System.Windows.Forms.FlowLayoutPanel();
            this.txtFilterBlueprintType = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmbBlueprintType = new System.Windows.Forms.ComboBox();
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
            this.txtFilterBaseBlueprint = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmbBaseBlueprint = new System.Windows.Forms.ComboBox();
            this.flpName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpNickName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblNickName = new System.Windows.Forms.Label();
            this.txtNickName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpDescription = new System.Windows.Forms.FlowLayoutPanel();
            this.lblDescription = new System.Windows.Forms.Label();
            this.txtDescription = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpCopyCost = new System.Windows.Forms.FlowLayoutPanel();
            this.lblCopyCost = new System.Windows.Forms.Label();
            this.txtCopyCost = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.chkGlobalBlueprint = new System.Windows.Forms.CheckBox();
            this.cmdNew = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.cmdDelete = new System.Windows.Forms.Button();
            this.tabDetailedData = new System.Windows.Forms.TabControl();
            this.tabPStatistics = new System.Windows.Forms.TabPage();
            this.dgvStatistics = new DataEntryGridView();
            this.Property = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BaseValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CurrentValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabPResources = new System.Windows.Forms.TabPage();
            this.dgvResources = new System.Windows.Forms.DataGridView();
            this.Resource = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.Amount = new OE2EmpireTracker.Controls.DataGridViewValidatedTextBoxColumn();
            this.flpBase = new System.Windows.Forms.FlowLayoutPanel();
            this.flpSearchList = new System.Windows.Forms.FlowLayoutPanel();
            this.flpBlueprintSearch = new System.Windows.Forms.FlowLayoutPanel();
            this.lblBlueprintListFilter = new System.Windows.Forms.Label();
            this.txtBlueprintListFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lvwBlueprints = new System.Windows.Forms.ListView();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdImportMarket = new System.Windows.Forms.Button();
            this.cmdImport = new System.Windows.Forms.Button();
            this.flpBlueprintType.SuspendLayout();
            this.flpTechLevel.SuspendLayout();
            this.flpBaseDetails.SuspendLayout();
            this.flpClass.SuspendLayout();
            this.flpEvolution.SuspendLayout();
            this.flpBaseBlueprint.SuspendLayout();
            this.flpName.SuspendLayout();
            this.flpNickName.SuspendLayout();
            this.flpDescription.SuspendLayout();
            this.flpCopyCost.SuspendLayout();
            this.flpCommands.SuspendLayout();
            this.tabDetailedData.SuspendLayout();
            this.tabPStatistics.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStatistics)).BeginInit();
            this.tabPResources.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResources)).BeginInit();
            this.flpBase.SuspendLayout();
            this.flpSearchList.SuspendLayout();
            this.flpBlueprintSearch.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
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
            // flpBlueprintType
            // 
            this.flpBlueprintType.AutoSize = true;
            this.flpBlueprintType.Controls.Add(this.lblBlueprintType);
            this.flpBlueprintType.Controls.Add(this.txtFilterBlueprintType);
            this.flpBlueprintType.Controls.Add(this.cmbBlueprintType);
            this.flpBlueprintType.Location = new System.Drawing.Point(2, 2);
            this.flpBlueprintType.Margin = new System.Windows.Forms.Padding(2);
            this.flpBlueprintType.Name = "flpBlueprintType";
            this.flpBlueprintType.Size = new System.Drawing.Size(415, 26);
            this.flpBlueprintType.TabIndex = 0;
            // 
            // txtFilterBlueprintType
            // 
            this.txtFilterBlueprintType.Location = new System.Drawing.Point(107, 3);
            this.txtFilterBlueprintType.Name = "txtFilterBlueprintType";
            this.txtFilterBlueprintType.Size = new System.Drawing.Size(100, 20);
            this.txtFilterBlueprintType.TabIndex = 0;
            this.txtFilterBlueprintType.TextChanged += new System.EventHandler(this.txtFilterBlueprintType_TextChanged);
            this.txtFilterBlueprintType.Enter += new System.EventHandler(this.txtFilterBlueprintType_Enter);
            // 
            // cmbBlueprintType
            // 
            this.cmbBlueprintType.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbBlueprintType.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbBlueprintType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBlueprintType.FormattingEnabled = true;
            this.cmbBlueprintType.Location = new System.Drawing.Point(212, 2);
            this.cmbBlueprintType.Margin = new System.Windows.Forms.Padding(2);
            this.cmbBlueprintType.Name = "cmbBlueprintType";
            this.cmbBlueprintType.Size = new System.Drawing.Size(201, 21);
            this.cmbBlueprintType.TabIndex = 3;
            this.cmbBlueprintType.SelectedIndexChanged += new System.EventHandler(this.cmbBlueprintType_SelectedIndexChanged);
            // 
            // flpTechLevel
            // 
            this.flpTechLevel.AutoSize = true;
            this.flpTechLevel.Controls.Add(this.lblTechLevel);
            this.flpTechLevel.Controls.Add(this.cmbTechLevel);
            this.flpTechLevel.Location = new System.Drawing.Point(2, 61);
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
            this.flpBaseDetails.Controls.Add(this.flpNickName);
            this.flpBaseDetails.Controls.Add(this.flpDescription);
            this.flpBaseDetails.Controls.Add(this.flpCopyCost);
            this.flpBaseDetails.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpBaseDetails.Location = new System.Drawing.Point(2, 2);
            this.flpBaseDetails.Margin = new System.Windows.Forms.Padding(2);
            this.flpBaseDetails.Name = "flpBaseDetails";
            this.flpBaseDetails.Size = new System.Drawing.Size(419, 259);
            this.flpBaseDetails.TabIndex = 6;
            // 
            // flpClass
            // 
            this.flpClass.AutoSize = true;
            this.flpClass.Controls.Add(this.lblShipClass);
            this.flpClass.Controls.Add(this.cmbShipClass);
            this.flpClass.Location = new System.Drawing.Point(2, 32);
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
            this.flpEvolution.Location = new System.Drawing.Point(2, 90);
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
            this.flpBaseBlueprint.Controls.Add(this.txtFilterBaseBlueprint);
            this.flpBaseBlueprint.Controls.Add(this.cmbBaseBlueprint);
            this.flpBaseBlueprint.Location = new System.Drawing.Point(2, 119);
            this.flpBaseBlueprint.Margin = new System.Windows.Forms.Padding(2);
            this.flpBaseBlueprint.Name = "flpBaseBlueprint";
            this.flpBaseBlueprint.Size = new System.Drawing.Size(415, 26);
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
            // txtFilterBaseBlueprint
            // 
            this.txtFilterBaseBlueprint.Location = new System.Drawing.Point(107, 3);
            this.txtFilterBaseBlueprint.Name = "txtFilterBaseBlueprint";
            this.txtFilterBaseBlueprint.Size = new System.Drawing.Size(100, 20);
            this.txtFilterBaseBlueprint.TabIndex = 0;
            this.txtFilterBaseBlueprint.TextChanged += new System.EventHandler(this.txtFilterBaseBlueprint_TextChanged);
            // 
            // cmbBaseBlueprint
            // 
            this.cmbBaseBlueprint.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbBaseBlueprint.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbBaseBlueprint.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBaseBlueprint.FormattingEnabled = true;
            this.cmbBaseBlueprint.Location = new System.Drawing.Point(212, 2);
            this.cmbBaseBlueprint.Margin = new System.Windows.Forms.Padding(2);
            this.cmbBaseBlueprint.Name = "cmbBaseBlueprint";
            this.cmbBaseBlueprint.Size = new System.Drawing.Size(201, 21);
            this.cmbBaseBlueprint.TabIndex = 1;
            // 
            // flpName
            // 
            this.flpName.AutoSize = true;
            this.flpName.Controls.Add(this.lblName);
            this.flpName.Controls.Add(this.txtName);
            this.flpName.Location = new System.Drawing.Point(2, 149);
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
            // flpNickName
            // 
            this.flpNickName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpNickName.AutoSize = true;
            this.flpNickName.Controls.Add(this.lblNickName);
            this.flpNickName.Controls.Add(this.txtNickName);
            this.flpNickName.Location = new System.Drawing.Point(2, 177);
            this.flpNickName.Margin = new System.Windows.Forms.Padding(2);
            this.flpNickName.Name = "flpNickName";
            this.flpNickName.Size = new System.Drawing.Size(415, 24);
            this.flpNickName.TabIndex = 6;
            // 
            // lblNickName
            // 
            this.lblNickName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblNickName.Location = new System.Drawing.Point(2, 3);
            this.lblNickName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblNickName.Name = "lblNickName";
            this.lblNickName.Size = new System.Drawing.Size(100, 17);
            this.lblNickName.TabIndex = 2;
            this.lblNickName.Text = "Nick Name";
            this.lblNickName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtNickName
            // 
            this.txtNickName.Location = new System.Drawing.Point(106, 2);
            this.txtNickName.Margin = new System.Windows.Forms.Padding(2);
            this.txtNickName.Name = "txtNickName";
            this.txtNickName.Size = new System.Drawing.Size(201, 20);
            this.txtNickName.TabIndex = 7;
            // 
            // flpDescription
            // 
            this.flpDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpDescription.AutoSize = true;
            this.flpDescription.Controls.Add(this.lblDescription);
            this.flpDescription.Controls.Add(this.txtDescription);
            this.flpDescription.Location = new System.Drawing.Point(2, 205);
            this.flpDescription.Margin = new System.Windows.Forms.Padding(2);
            this.flpDescription.Name = "flpDescription";
            this.flpDescription.Size = new System.Drawing.Size(415, 24);
            this.flpDescription.TabIndex = 7;
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
            // flpCopyCost
            // 
            this.flpCopyCost.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpCopyCost.AutoSize = true;
            this.flpCopyCost.Controls.Add(this.lblCopyCost);
            this.flpCopyCost.Controls.Add(this.txtCopyCost);
            this.flpCopyCost.Location = new System.Drawing.Point(2, 233);
            this.flpCopyCost.Margin = new System.Windows.Forms.Padding(2);
            this.flpCopyCost.Name = "flpCopyCost";
            this.flpCopyCost.Size = new System.Drawing.Size(415, 24);
            this.flpCopyCost.TabIndex = 8;
            // 
            // lblCopyCost
            // 
            this.lblCopyCost.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCopyCost.Location = new System.Drawing.Point(2, 3);
            this.lblCopyCost.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblCopyCost.Name = "lblCopyCost";
            this.lblCopyCost.Size = new System.Drawing.Size(100, 17);
            this.lblCopyCost.TabIndex = 2;
            this.lblCopyCost.Text = "Copy Cost";
            this.lblCopyCost.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtCopyCost
            // 
            this.txtCopyCost.Location = new System.Drawing.Point(106, 2);
            this.txtCopyCost.Margin = new System.Windows.Forms.Padding(2);
            this.txtCopyCost.Name = "txtCopyCost";
            this.txtCopyCost.Size = new System.Drawing.Size(201, 20);
            this.txtCopyCost.TabIndex = 7;
            // 
            // flpCommands
            // 
            this.flpCommands.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpCommands.AutoSize = true;
            this.flpCommands.Controls.Add(this.chkGlobalBlueprint);
            this.flpCommands.Controls.Add(this.cmdNew);
            this.flpCommands.Controls.Add(this.btnSave);
            this.flpCommands.Controls.Add(this.cmdDelete);
            this.flpCommands.Controls.Add(this.cmdImportMarket);
            this.flpCommands.Controls.Add(this.cmdImport);
            this.flpCommands.Location = new System.Drawing.Point(2, 598);
            this.flpCommands.Margin = new System.Windows.Forms.Padding(2);
            this.flpCommands.Name = "flpCommands";
            this.flpCommands.Size = new System.Drawing.Size(834, 29);
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
            // cmdNew
            // 
            this.cmdNew.Location = new System.Drawing.Point(109, 3);
            this.cmdNew.Name = "cmdNew";
            this.cmdNew.Size = new System.Drawing.Size(75, 23);
            this.cmdNew.TabIndex = 12;
            this.cmdNew.Text = "New";
            this.cmdNew.UseVisualStyleBackColor = true;
            this.cmdNew.Click += new System.EventHandler(this.cmdNew_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(190, 3);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 9;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // cmdDelete
            // 
            this.cmdDelete.Location = new System.Drawing.Point(271, 3);
            this.cmdDelete.Name = "cmdDelete";
            this.cmdDelete.Size = new System.Drawing.Size(75, 23);
            this.cmdDelete.TabIndex = 11;
            this.cmdDelete.Text = "Delete";
            this.cmdDelete.UseVisualStyleBackColor = true;
            this.cmdDelete.Click += new System.EventHandler(this.cmdDelete_Click);
            // 
            // tabDetailedData
            // 
            this.tabDetailedData.Controls.Add(this.tabPStatistics);
            this.tabDetailedData.Controls.Add(this.tabPResources);
            this.tabDetailedData.Location = new System.Drawing.Point(2, 265);
            this.tabDetailedData.Margin = new System.Windows.Forms.Padding(2);
            this.tabDetailedData.Name = "tabDetailedData";
            this.tabDetailedData.SelectedIndex = 0;
            this.tabDetailedData.Size = new System.Drawing.Size(834, 329);
            this.tabDetailedData.TabIndex = 8;
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
            // dgvStatistics
            // 
            this.dgvStatistics.AllowUserToAddRows = false;
            this.dgvStatistics.AllowUserToDeleteRows = false;
            this.dgvStatistics.AllowUserToOrderColumns = true;
            this.dgvStatistics.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvStatistics.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Property,
            this.BaseValue,
            this.CurrentValue});
            this.dgvStatistics.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvStatistics.Location = new System.Drawing.Point(-2, 0);
            this.dgvStatistics.Name = "dgvStatistics";
            this.dgvStatistics.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.dgvStatistics_CellValidating);
            this.dgvStatistics.previousControl = null;
            this.dgvStatistics.Size = new System.Drawing.Size(828, 298);
            this.dgvStatistics.TabIndex = 1;
            this.dgvStatistics.SelectionChanged += new System.EventHandler(this.dgvStatistics_SelectionChanged);
            // 
            // Property
            // 
            this.Property.HeaderText = "Property";
            this.Property.Name = "Property";
            this.Property.ReadOnly = true;
            this.Property.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // BaseValue
            // 
            this.BaseValue.HeaderText = "BaseValue";
            this.BaseValue.Name = "BaseValue";
            this.BaseValue.ReadOnly = true;
            this.BaseValue.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // CurrentValue
            // 
            this.CurrentValue.HeaderText = "CurrentValue";
            this.CurrentValue.Name = "CurrentValue";
            this.CurrentValue.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
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
            this.dgvResources.AllowUserToOrderColumns = true;
            this.dgvResources.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvResources.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Resource,
            this.Amount});
            this.dgvResources.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvResources.Location = new System.Drawing.Point(1, 0);
            this.dgvResources.Name = "dgvResources";
            this.dgvResources.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.dgvResources_CellValidating);
            this.dgvResources.Size = new System.Drawing.Size(825, 303);
            this.dgvResources.TabIndex = 0;
            // 
            // Resource
            // 
            this.Resource.HeaderText = "Resource";
            this.Resource.Name = "Resource";
            this.Resource.Width = 150;
            // 
            // Amount
            // 
            this.Amount.HeaderText = "Amount";
            this.Amount.Name = "Amount";
            this.Amount.ValidationPattern = OE2EmpireTracker.Controls.ValidatedTextBox.NUMBER_VALIDATION;
            // 
            // flpBase
            // 
            this.flpBase.Controls.Add(this.flpSearchList);
            this.flpBase.Controls.Add(this.flowLayoutPanel1);
            this.flpBase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpBase.WrapContents = false;
            this.flpBase.Location = new System.Drawing.Point(0, 0);
            this.flpBase.Name = "flpBase";
            this.flpBase.TabIndex = 9;
            this.flpBase.Layout += new System.Windows.Forms.LayoutEventHandler(this.flpBase_Layout);
            // 
            // flpSearchList
            // 
            this.flpSearchList.Controls.Add(this.flpBlueprintSearch);
            this.flpSearchList.Controls.Add(this.lvwBlueprints);
            this.flpSearchList.Location = new System.Drawing.Point(3, 3);
            this.flpSearchList.Name = "flpSearchList";
            this.flpSearchList.TabIndex = 10;
            this.flpSearchList.Layout += new System.Windows.Forms.LayoutEventHandler(this.flpSearchList_Layout);
            // 
            // flpBlueprintSearch
            // 
            this.flpBlueprintSearch.AutoSize = true;
            this.flpBlueprintSearch.Controls.Add(this.lblBlueprintListFilter);
            this.flpBlueprintSearch.Controls.Add(this.txtBlueprintListFilter);
            this.flpBlueprintSearch.Location = new System.Drawing.Point(2, 2);
            this.flpBlueprintSearch.Margin = new System.Windows.Forms.Padding(2);
            this.flpBlueprintSearch.Name = "flpBlueprintSearch";
            this.flpBlueprintSearch.Size = new System.Drawing.Size(210, 26);
            this.flpBlueprintSearch.TabIndex = 5;
            // 
            // lblBlueprintListFilter
            // 
            this.lblBlueprintListFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblBlueprintListFilter.Location = new System.Drawing.Point(2, 4);
            this.lblBlueprintListFilter.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblBlueprintListFilter.Name = "lblBlueprintListFilter";
            this.lblBlueprintListFilter.Size = new System.Drawing.Size(100, 17);
            this.lblBlueprintListFilter.TabIndex = 2;
            this.lblBlueprintListFilter.Text = "Name";
            this.lblBlueprintListFilter.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtBlueprintListFilter
            // 
            this.txtBlueprintListFilter.Location = new System.Drawing.Point(107, 3);
            this.txtBlueprintListFilter.Name = "txtBlueprintListFilter";
            this.txtBlueprintListFilter.Size = new System.Drawing.Size(100, 20);
            this.txtBlueprintListFilter.TabIndex = 0;
            this.txtBlueprintListFilter.TextChanged += new System.EventHandler(this.txtBlueprintListFilter_TextChanged);
            // 
            // lvwBlueprints
            // 
            this.lvwBlueprints.FullRowSelect = true;
            this.lvwBlueprints.HideSelection = false;
            this.lvwBlueprints.Location = new System.Drawing.Point(3, 33);
            this.lvwBlueprints.MultiSelect = false;
            this.lvwBlueprints.Name = "lvwBlueprints";
            this.lvwBlueprints.Size = new System.Drawing.Size(412, 566);
            this.lvwBlueprints.TabIndex = 6;
            this.lvwBlueprints.UseCompatibleStateImageBehavior = false;
            this.lvwBlueprints.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.lvwBlueprints_ItemSelectionChanged);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.flpBaseDetails);
            this.flowLayoutPanel1.Controls.Add(this.tabDetailedData);
            this.flowLayoutPanel1.Controls.Add(this.flpCommands);
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(436, 3);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.TabIndex = 10;
            this.flowLayoutPanel1.Layout += new System.Windows.Forms.LayoutEventHandler(this.flowLayoutPanel1_Layout);
            // 
            // cmdImportMarket
            // 
            this.cmdImportMarket.Location = new System.Drawing.Point(352, 3);
            this.cmdImportMarket.Name = "cmdImportMarket";
            this.cmdImportMarket.Size = new System.Drawing.Size(95, 23);
            this.cmdImportMarket.TabIndex = 14;
            this.cmdImportMarket.Text = "Import Market";
            this.cmdImportMarket.UseVisualStyleBackColor = true;
            this.cmdImportMarket.Click += new System.EventHandler(this.cmdImportMarket_Click);
            // 
            // cmdImport
            // 
            this.cmdImport.Location = new System.Drawing.Point(453, 3);
            this.cmdImport.Name = "cmdImport";
            this.cmdImport.Size = new System.Drawing.Size(75, 23);
            this.cmdImport.TabIndex = 13;
            this.cmdImport.Text = "Import";
            this.cmdImport.UseVisualStyleBackColor = true;
            this.cmdImport.Click += new System.EventHandler(this.cmdImport_Click);
            // 
            // FormBlueprint
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(869, 645);
            this.Controls.Add(this.flpBase);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "FormBlueprint";
            this.Text = "Blueprint";
            this.flpBlueprintType.ResumeLayout(false);
            this.flpBlueprintType.PerformLayout();
            this.flpTechLevel.ResumeLayout(false);
            this.flpBaseDetails.ResumeLayout(false);
            this.flpBaseDetails.PerformLayout();
            this.flpClass.ResumeLayout(false);
            this.flpEvolution.ResumeLayout(false);
            this.flpBaseBlueprint.ResumeLayout(false);
            this.flpBaseBlueprint.PerformLayout();
            this.flpName.ResumeLayout(false);
            this.flpName.PerformLayout();
            this.flpNickName.ResumeLayout(false);
            this.flpNickName.PerformLayout();
            this.flpDescription.ResumeLayout(false);
            this.flpDescription.PerformLayout();
            this.flpCopyCost.ResumeLayout(false);
            this.flpCopyCost.PerformLayout();
            this.flpCommands.ResumeLayout(false);
            this.flpCommands.PerformLayout();
            this.tabDetailedData.ResumeLayout(false);
            this.tabPStatistics.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvStatistics)).EndInit();
            this.tabPResources.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvResources)).EndInit();
            this.flpBase.ResumeLayout(false);
            this.flpSearchList.ResumeLayout(false);
            this.flpSearchList.PerformLayout();
            this.flpBlueprintSearch.ResumeLayout(false);
            this.flpBlueprintSearch.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label lblBlueprintType;
        private System.Windows.Forms.FlowLayoutPanel flpBlueprintType;
        private System.Windows.Forms.FlowLayoutPanel flpTechLevel;
        private System.Windows.Forms.Label lblTechLevel;
        private System.Windows.Forms.ComboBox cmbTechLevel;
        private System.Windows.Forms.FlowLayoutPanel flpBaseDetails;
        private System.Windows.Forms.FlowLayoutPanel flpName;
        private System.Windows.Forms.Label lblName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtName;
        private System.Windows.Forms.FlowLayoutPanel flpEvolution;
        private System.Windows.Forms.Label lblEvolution;
        private System.Windows.Forms.FlowLayoutPanel flpDescription;
        private System.Windows.Forms.Label lblDescription;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtDescription;
        private System.Windows.Forms.TabControl tabDetailedData;
        private System.Windows.Forms.TabPage tabPStatistics;
        private System.Windows.Forms.TabPage tabPResources;
        private System.Windows.Forms.FlowLayoutPanel flpBaseBlueprint;
        private System.Windows.Forms.Label lblBaseBlueprint;
        private System.Windows.Forms.ComboBox cmbEvolution;
        private System.Windows.Forms.FlowLayoutPanel flpClass;
        private System.Windows.Forms.Label lblShipClass;
        private System.Windows.Forms.ComboBox cmbShipClass;
        private System.Windows.Forms.FlowLayoutPanel flpBase;
        private System.Windows.Forms.FlowLayoutPanel flpCommands;
        private System.Windows.Forms.CheckBox chkGlobalBlueprint;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.DataGridView dgvResources;
        private DataEntryGridView dgvStatistics;
        private System.Windows.Forms.DataGridViewTextBoxColumn Property;
        private System.Windows.Forms.DataGridViewTextBoxColumn BaseValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn CurrentValue;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtFilterBlueprintType;
        private System.Windows.Forms.ComboBox cmbBlueprintType;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtFilterBaseBlueprint;
        private System.Windows.Forms.ComboBox cmbBaseBlueprint;
        private System.Windows.Forms.FlowLayoutPanel flpNickName;
        private System.Windows.Forms.Label lblNickName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtNickName;
        private System.Windows.Forms.DataGridViewComboBoxColumn Resource;
        private OE2EmpireTracker.Controls.DataGridViewValidatedTextBoxColumn Amount;
        private System.Windows.Forms.FlowLayoutPanel flpSearchList;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flpBlueprintSearch;
        private System.Windows.Forms.Label lblBlueprintListFilter;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtBlueprintListFilter;
        private System.Windows.Forms.ListView lvwBlueprints;
        private System.Windows.Forms.Button cmdDelete;
        private System.Windows.Forms.FlowLayoutPanel flpCopyCost;
        private System.Windows.Forms.Label lblCopyCost;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtCopyCost;
        private System.Windows.Forms.Button cmdNew;
        private System.Windows.Forms.Button cmdImportMarket;
        private System.Windows.Forms.Button cmdImport;
    }
}