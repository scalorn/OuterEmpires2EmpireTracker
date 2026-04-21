namespace OE2EmpireTracker.Forms.Survey
{
    partial class FormSurvey
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
            this.flpSurveyFilter = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSurveyFilter = new System.Windows.Forms.Label();
            this.txtSurveyFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpResource = new System.Windows.Forms.FlowLayoutPanel();
            this.lblResource = new System.Windows.Forms.Label();
            this.cmbResource = new System.Windows.Forms.ComboBox();
            this.flpTypeFilter = new System.Windows.Forms.FlowLayoutPanel();
            this.lblType = new System.Windows.Forms.Label();
            this.cmbSurveyType = new System.Windows.Forms.ComboBox();
            this.lblPurity = new System.Windows.Forms.Label();
            this.cmbPurityFilter = new System.Windows.Forms.ComboBox();
            this.lblMinAmount = new System.Windows.Forms.Label();
            this.txtMinAmount = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lvwSurveys = new System.Windows.Forms.ListView();
            this.flpSurveyData = new System.Windows.Forms.FlowLayoutPanel();
            this.flpSurveyDetails = new System.Windows.Forms.FlowLayoutPanel();
            this.flpPlanetName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblPlanetName = new System.Windows.Forms.Label();
            this.txtPlanetName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpSystemName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSystemName = new System.Windows.Forms.Label();
            this.txtSystemName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpSurveyTypeEdit = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSurveyTypeEdit = new System.Windows.Forms.Label();
            this.cmbSurveyTypeEdit = new System.Windows.Forms.ComboBox();
            this.flpSurveyID = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSurveyID = new System.Windows.Forms.Label();
            this.txtSurveyID = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpNickName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblNickName = new System.Windows.Forms.Label();
            this.txtNickName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpScannerBlueprint = new System.Windows.Forms.FlowLayoutPanel();
            this.lblScannerBlueprint = new System.Windows.Forms.Label();
            this.txtFilterScannerBlueprint = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmbScannerBlueprint = new System.Windows.Forms.ComboBox();
            this.flpScannedBy = new System.Windows.Forms.FlowLayoutPanel();
            this.lblScannedBy = new System.Windows.Forms.Label();
            this.txtScannedBy = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpScanDateTime = new System.Windows.Forms.FlowLayoutPanel();
            this.lblScanDateTime = new System.Windows.Forms.Label();
            this.txtScanDateTime = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.dtpScanDateTime = new System.Windows.Forms.DateTimePicker();
            this.flpSensorAbundance = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSensorAbundance = new System.Windows.Forms.Label();
            this.txtSensorAbundance = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpPurityModifier = new System.Windows.Forms.FlowLayoutPanel();
            this.lblPurityModifier = new System.Windows.Forms.Label();
            this.txtPurityModifier = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpScanLevel = new System.Windows.Forms.FlowLayoutPanel();
            this.lblScanLevel = new System.Windows.Forms.Label();
            this.txtScanLevel = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.dgvResources = new System.Windows.Forms.DataGridView();
            this.Resource = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.Purity = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.Amount = new OE2EmpireTracker.Controls.DataGridViewValidatedTextBoxColumn();
            this.flpCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdNew = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.cmdDelete = new System.Windows.Forms.Button();
            this.cmdImport = new System.Windows.Forms.Button();
            this.flpBase.SuspendLayout();
            this.flpSearchList.SuspendLayout();
            this.flpSurveyFilter.SuspendLayout();
            this.flpResource.SuspendLayout();
            this.flpSurveyData.SuspendLayout();
            this.flpSurveyDetails.SuspendLayout();
            this.flpPlanetName.SuspendLayout();
            this.flpSurveyID.SuspendLayout();
            this.flpNickName.SuspendLayout();
            this.flpScannerBlueprint.SuspendLayout();
            this.flpScannedBy.SuspendLayout();
            this.flpScanDateTime.SuspendLayout();
            this.flpSensorAbundance.SuspendLayout();
            this.flpPurityModifier.SuspendLayout();
            this.flpScanLevel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResources)).BeginInit();
            this.flpCommands.SuspendLayout();
            this.SuspendLayout();
            // 
            // flpBase
            // 
            this.flpBase.AutoSize = true;
            this.flpBase.Controls.Add(this.flpSearchList);
            this.flpBase.Controls.Add(this.flpSurveyData);
            this.flpBase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpBase.Location = new System.Drawing.Point(0, 0);
            this.flpBase.Name = "flpBase";
            this.flpBase.Size = new System.Drawing.Size(1074, 704);
            this.flpBase.TabIndex = 10;
            this.flpBase.WrapContents = false;
            // 
            // flpSearchList
            // 
            this.flpSearchList.Controls.Add(this.flpSurveyFilter);
            this.flpSearchList.Controls.Add(this.flpResource);
            this.flpSearchList.Controls.Add(this.flpTypeFilter);
            this.flpSearchList.Controls.Add(this.lvwSurveys);
            this.flpSearchList.Location = new System.Drawing.Point(3, 3);
            this.flpSearchList.Name = "flpSearchList";
            this.flpSearchList.Size = new System.Drawing.Size(427, 641);
            this.flpSearchList.TabIndex = 10;
            // 
            // flpSurveyFilter
            // 
            this.flpSurveyFilter.AutoSize = true;
            this.flpSurveyFilter.Controls.Add(this.lblSurveyFilter);
            this.flpSurveyFilter.Controls.Add(this.txtSurveyFilter);
            this.flpSurveyFilter.Location = new System.Drawing.Point(2, 2);
            this.flpSurveyFilter.Margin = new System.Windows.Forms.Padding(2);
            this.flpSurveyFilter.Name = "flpSurveyFilter";
            this.flpSurveyFilter.Size = new System.Drawing.Size(312, 26);
            this.flpSurveyFilter.TabIndex = 0;
            // 
            // lblSurveyFilter
            // 
            this.lblSurveyFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSurveyFilter.Location = new System.Drawing.Point(2, 4);
            this.lblSurveyFilter.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblSurveyFilter.Name = "lblSurveyFilter";
            this.lblSurveyFilter.Size = new System.Drawing.Size(100, 17);
            this.lblSurveyFilter.TabIndex = 2;
            this.lblSurveyFilter.Text = "Filter";
            this.lblSurveyFilter.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtSurveyFilter
            // 
            this.txtSurveyFilter.AllowSpaces = true;
            this.txtSurveyFilter.AutoFormat = true;
            this.txtSurveyFilter.ErrorMessage = "";
            this.txtSurveyFilter.InvalidColor = System.Drawing.Color.LightCoral;
            this.txtSurveyFilter.IsValid = true;
            this.txtSurveyFilter.Location = new System.Drawing.Point(107, 3);
            this.txtSurveyFilter.Name = "txtSurveyFilter";
            this.txtSurveyFilter.Size = new System.Drawing.Size(202, 20);
            this.txtSurveyFilter.TabIndex = 0;
            this.txtSurveyFilter.ValidationPattern = null;
            this.txtSurveyFilter.ValidColor = System.Drawing.Color.White;
            // 
            // flpResource
            // 
            this.flpResource.AutoSize = true;
            this.flpResource.Controls.Add(this.lblResource);
            this.flpResource.Controls.Add(this.cmbResource);
            this.flpResource.Location = new System.Drawing.Point(2, 32);
            this.flpResource.Margin = new System.Windows.Forms.Padding(2);
            this.flpResource.Name = "flpResource";
            this.flpResource.Size = new System.Drawing.Size(309, 25);
            this.flpResource.TabIndex = 1;
            // 
            // lblResource
            // 
            this.lblResource.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblResource.Location = new System.Drawing.Point(2, 4);
            this.lblResource.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblResource.Name = "lblResource";
            this.lblResource.Size = new System.Drawing.Size(100, 17);
            this.lblResource.TabIndex = 2;
            this.lblResource.Text = "Resource";
            this.lblResource.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbResource
            // 
            this.cmbResource.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbResource.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbResource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbResource.FormattingEnabled = true;
            this.cmbResource.Location = new System.Drawing.Point(106, 2);
            this.cmbResource.Margin = new System.Windows.Forms.Padding(2);
            this.cmbResource.Name = "cmbResource";
            this.cmbResource.Size = new System.Drawing.Size(201, 21);
            this.cmbResource.TabIndex = 1;
            //
            // flpTypeFilter
            //
            this.flpTypeFilter.AutoSize = true;
            this.flpTypeFilter.Controls.Add(this.lblType);
            this.flpTypeFilter.Controls.Add(this.cmbSurveyType);
            this.flpTypeFilter.Controls.Add(this.lblPurity);
            this.flpTypeFilter.Controls.Add(this.cmbPurityFilter);
            this.flpTypeFilter.Controls.Add(this.lblMinAmount);
            this.flpTypeFilter.Controls.Add(this.txtMinAmount);
            this.flpTypeFilter.Location = new System.Drawing.Point(2, 62);
            this.flpTypeFilter.Margin = new System.Windows.Forms.Padding(2);
            this.flpTypeFilter.Name = "flpTypeFilter";
            this.flpTypeFilter.Size = new System.Drawing.Size(310, 26);
            this.flpTypeFilter.WrapContents = false;
            // lblType
            this.lblType.AutoSize = true;
            this.lblType.Text = "Type:";
            this.lblType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblType.Name = "lblType";
            // cmbSurveyType
            this.cmbSurveyType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSurveyType.Size = new System.Drawing.Size(80, 21);
            this.cmbSurveyType.Name = "cmbSurveyType";
            // lblPurity
            this.lblPurity.AutoSize = true;
            this.lblPurity.Text = "Purity:";
            this.lblPurity.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblPurity.Name = "lblPurity";
            // cmbPurityFilter
            this.cmbPurityFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPurityFilter.Size = new System.Drawing.Size(70, 21);
            this.cmbPurityFilter.Name = "cmbPurityFilter";
            // lblMinAmount
            this.lblMinAmount.AutoSize = true;
            this.lblMinAmount.Text = "Min:";
            this.lblMinAmount.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblMinAmount.Name = "lblMinAmount";
            // txtMinAmount
            this.txtMinAmount.Size = new System.Drawing.Size(45, 20);
            this.txtMinAmount.Name = "txtMinAmount";
            // 
            // lvwSurveys
            // 
            this.lvwSurveys.FullRowSelect = true;
            this.lvwSurveys.HideSelection = false;
            this.lvwSurveys.Location = new System.Drawing.Point(3, 62);
            this.lvwSurveys.MultiSelect = false;
            this.lvwSurveys.Name = "lvwSurveys";
            this.lvwSurveys.Size = new System.Drawing.Size(412, 566);
            this.lvwSurveys.TabIndex = 6;
            this.lvwSurveys.UseCompatibleStateImageBehavior = false;
            this.lvwSurveys.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.lvwSurveys_ItemSelectionChanged);
            // 
            // flpSurveyData
            // 
            this.flpSurveyData.Controls.Add(this.flpSurveyDetails);
            this.flpSurveyData.Controls.Add(this.flpCommands);
            this.flpSurveyData.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpSurveyData.Location = new System.Drawing.Point(436, 3);
            this.flpSurveyData.Name = "flpSurveyData";
            this.flpSurveyData.Size = new System.Drawing.Size(661, 641);
            this.flpSurveyData.TabIndex = 3;
            // 
            // flpSurveyDetails
            // 
            this.flpSurveyDetails.Controls.Add(this.flpSurveyTypeEdit);
            this.flpSurveyDetails.Controls.Add(this.flpSystemName);
            this.flpSurveyDetails.Controls.Add(this.flpPlanetName);
            this.flpSurveyDetails.Controls.Add(this.flpSurveyID);
            this.flpSurveyDetails.Controls.Add(this.flpNickName);
            this.flpSurveyDetails.Controls.Add(this.flpScannerBlueprint);
            this.flpSurveyDetails.Controls.Add(this.flpScannedBy);
            this.flpSurveyDetails.Controls.Add(this.flpScanDateTime);
            this.flpSurveyDetails.Controls.Add(this.flpSensorAbundance);
            this.flpSurveyDetails.Controls.Add(this.flpPurityModifier);
            this.flpSurveyDetails.Controls.Add(this.flpScanLevel);
            this.flpSurveyDetails.Controls.Add(this.dgvResources);
            this.flpSurveyDetails.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpSurveyDetails.Location = new System.Drawing.Point(2, 2);
            this.flpSurveyDetails.Margin = new System.Windows.Forms.Padding(2);
            this.flpSurveyDetails.Name = "flpSurveyDetails";
            this.flpSurveyDetails.Size = new System.Drawing.Size(831, 569);
            this.flpSurveyDetails.TabIndex = 0;
            // 
            // flpPlanetName
            // 
            this.flpPlanetName.AutoSize = true;
            this.flpPlanetName.Controls.Add(this.lblPlanetName);
            this.flpPlanetName.Controls.Add(this.txtPlanetName);
            this.flpPlanetName.Location = new System.Drawing.Point(2, 2);
            this.flpPlanetName.Margin = new System.Windows.Forms.Padding(2);
            this.flpPlanetName.Name = "flpPlanetName";
            this.flpPlanetName.Size = new System.Drawing.Size(310, 26);
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
            this.lblPlanetName.TabIndex = 2;
            this.lblPlanetName.Text = "Planet Name";
            this.lblPlanetName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtPlanetName
            // 
            this.txtPlanetName.AllowSpaces = true;
            this.txtPlanetName.AutoFormat = true;
            this.txtPlanetName.ErrorMessage = "";
            this.txtPlanetName.InvalidColor = System.Drawing.Color.LightCoral;
            this.txtPlanetName.IsValid = true;
            this.txtPlanetName.Location = new System.Drawing.Point(107, 3);
            this.txtPlanetName.Name = "txtPlanetName";
            this.txtPlanetName.Size = new System.Drawing.Size(200, 20);
            this.txtPlanetName.TabIndex = 0;
            this.txtPlanetName.ValidationPattern = null;
            this.txtPlanetName.ValidColor = System.Drawing.Color.White;
            // 
            // flpSystemName
            // 
            this.flpSystemName.AutoSize = true;
            this.flpSystemName.Controls.Add(this.lblSystemName);
            this.flpSystemName.Controls.Add(this.txtSystemName);
            this.flpSystemName.Location = new System.Drawing.Point(2, 32);
            this.flpSystemName.Margin = new System.Windows.Forms.Padding(2);
            this.flpSystemName.Name = "flpSystemName";
            this.flpSystemName.Size = new System.Drawing.Size(310, 26);
            this.flpSystemName.TabIndex = 1;
            // 
            // lblSystemName
            // 
            this.lblSystemName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSystemName.Location = new System.Drawing.Point(2, 4);
            this.lblSystemName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblSystemName.Name = "lblSystemName";
            this.lblSystemName.Size = new System.Drawing.Size(99, 17);
            this.lblSystemName.TabIndex = 1;
            this.lblSystemName.Text = "System";
            this.lblSystemName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtSystemName
            // 
            this.txtSystemName.Location = new System.Drawing.Point(107, 3);
            this.txtSystemName.Name = "txtSystemName";
            this.txtSystemName.Size = new System.Drawing.Size(200, 20);
            this.txtSystemName.TabIndex = 0;
            // 
            // flpSurveyTypeEdit
            // 
            this.flpSurveyTypeEdit.AutoSize = true;
            this.flpSurveyTypeEdit.Controls.Add(this.lblSurveyTypeEdit);
            this.flpSurveyTypeEdit.Controls.Add(this.cmbSurveyTypeEdit);
            this.flpSurveyTypeEdit.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flpSurveyTypeEdit.Margin = new System.Windows.Forms.Padding(2);
            this.flpSurveyTypeEdit.Name = "flpSurveyTypeEdit";
            this.flpSurveyTypeEdit.Size = new System.Drawing.Size(400, 26);
            this.lblSurveyTypeEdit.AutoSize = true;
            this.lblSurveyTypeEdit.Location = new System.Drawing.Point(3, 5);
            this.lblSurveyTypeEdit.Name = "lblSurveyTypeEdit";
            this.lblSurveyTypeEdit.Size = new System.Drawing.Size(100, 13);
            this.lblSurveyTypeEdit.Text = "Survey Type:";
            this.lblSurveyTypeEdit.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.cmbSurveyTypeEdit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSurveyTypeEdit.Location = new System.Drawing.Point(107, 3);
            this.cmbSurveyTypeEdit.Name = "cmbSurveyTypeEdit";
            this.cmbSurveyTypeEdit.Size = new System.Drawing.Size(120, 21);
            // 
            // flpSurveyID
            // 
            this.flpSurveyID.AutoSize = true;
            this.flpSurveyID.Controls.Add(this.lblSurveyID);
            this.flpSurveyID.Controls.Add(this.txtSurveyID);
            this.flpSurveyID.Location = new System.Drawing.Point(2, 32);
            this.flpSurveyID.Margin = new System.Windows.Forms.Padding(2);
            this.flpSurveyID.Name = "flpSurveyID";
            this.flpSurveyID.Size = new System.Drawing.Size(310, 26);
            this.flpSurveyID.TabIndex = 2;
            this.flpSurveyID.WrapContents = false;
            // 
            // lblSurveyID
            // 
            this.lblSurveyID.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSurveyID.Location = new System.Drawing.Point(2, 4);
            this.lblSurveyID.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblSurveyID.Name = "lblSurveyID";
            this.lblSurveyID.Size = new System.Drawing.Size(100, 17);
            this.lblSurveyID.TabIndex = 2;
            this.lblSurveyID.Text = "Survey ID";
            this.lblSurveyID.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtSurveyID
            // 
            this.txtSurveyID.AllowSpaces = true;
            this.txtSurveyID.AutoFormat = true;
            this.txtSurveyID.ErrorMessage = "";
            this.txtSurveyID.InvalidColor = System.Drawing.Color.LightCoral;
            this.txtSurveyID.IsValid = true;
            this.txtSurveyID.Location = new System.Drawing.Point(107, 3);
            this.txtSurveyID.Name = "txtSurveyID";
            this.txtSurveyID.Size = new System.Drawing.Size(200, 20);
            this.txtSurveyID.TabIndex = 0;
            this.txtSurveyID.ValidationPattern = null;
            this.txtSurveyID.ValidColor = System.Drawing.Color.White;
            // 
            // flpNickName
            // 
            this.flpNickName.AutoSize = true;
            this.flpNickName.Controls.Add(this.lblNickName);
            this.flpNickName.Controls.Add(this.txtNickName);
            this.flpNickName.Location = new System.Drawing.Point(2, 62);
            this.flpNickName.Margin = new System.Windows.Forms.Padding(2);
            this.flpNickName.Name = "flpNickName";
            this.flpNickName.Size = new System.Drawing.Size(310, 26);
            this.flpNickName.TabIndex = 3;
            this.flpNickName.WrapContents = false;
            // 
            // lblNickName
            // 
            this.lblNickName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblNickName.Location = new System.Drawing.Point(2, 4);
            this.lblNickName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblNickName.Name = "lblNickName";
            this.lblNickName.Size = new System.Drawing.Size(100, 17);
            this.lblNickName.TabIndex = 2;
            this.lblNickName.Text = "Nick Name";
            this.lblNickName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtNickName
            // 
            this.txtNickName.AllowSpaces = true;
            this.txtNickName.AutoFormat = true;
            this.txtNickName.ErrorMessage = "";
            this.txtNickName.InvalidColor = System.Drawing.Color.LightCoral;
            this.txtNickName.IsValid = true;
            this.txtNickName.Location = new System.Drawing.Point(107, 3);
            this.txtNickName.Name = "txtNickName";
            this.txtNickName.Size = new System.Drawing.Size(200, 20);
            this.txtNickName.TabIndex = 0;
            this.txtNickName.ValidationPattern = null;
            this.txtNickName.ValidColor = System.Drawing.Color.White;
            // 
            // flpScannerBlueprint
            // 
            this.flpScannerBlueprint.AutoSize = true;
            this.flpScannerBlueprint.Controls.Add(this.lblScannerBlueprint);
            this.flpScannerBlueprint.Controls.Add(this.txtFilterScannerBlueprint);
            this.flpScannerBlueprint.Controls.Add(this.cmbScannerBlueprint);
            this.flpScannerBlueprint.Location = new System.Drawing.Point(2, 92);
            this.flpScannerBlueprint.Margin = new System.Windows.Forms.Padding(2);
            this.flpScannerBlueprint.Name = "flpScannerBlueprint";
            this.flpScannerBlueprint.Size = new System.Drawing.Size(415, 26);
            this.flpScannerBlueprint.TabIndex = 3;
            this.flpScannerBlueprint.WrapContents = false;
            // 
            // lblScannerBlueprint
            // 
            this.lblScannerBlueprint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblScannerBlueprint.Location = new System.Drawing.Point(2, 4);
            this.lblScannerBlueprint.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblScannerBlueprint.Name = "lblScannerBlueprint";
            this.lblScannerBlueprint.Size = new System.Drawing.Size(100, 17);
            this.lblScannerBlueprint.TabIndex = 2;
            this.lblScannerBlueprint.Text = "Scanner Blueprint";
            this.lblScannerBlueprint.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtFilterScannerBlueprint
            // 
            this.txtFilterScannerBlueprint.AllowSpaces = true;
            this.txtFilterScannerBlueprint.AutoFormat = true;
            this.txtFilterScannerBlueprint.ErrorMessage = "";
            this.txtFilterScannerBlueprint.InvalidColor = System.Drawing.Color.LightCoral;
            this.txtFilterScannerBlueprint.IsValid = true;
            this.txtFilterScannerBlueprint.Location = new System.Drawing.Point(107, 3);
            this.txtFilterScannerBlueprint.Name = "txtFilterScannerBlueprint";
            this.txtFilterScannerBlueprint.Size = new System.Drawing.Size(100, 20);
            this.txtFilterScannerBlueprint.TabIndex = 0;
            this.txtFilterScannerBlueprint.ValidationPattern = null;
            this.txtFilterScannerBlueprint.ValidColor = System.Drawing.Color.White;
            this.txtFilterScannerBlueprint.TextChanged += new System.EventHandler(this.txtFilterScannerBlueprint_TextChanged);
            // 
            // cmbScannerBlueprint
            // 
            this.cmbScannerBlueprint.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbScannerBlueprint.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbScannerBlueprint.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbScannerBlueprint.FormattingEnabled = true;
            this.cmbScannerBlueprint.Location = new System.Drawing.Point(212, 2);
            this.cmbScannerBlueprint.Margin = new System.Windows.Forms.Padding(2);
            this.cmbScannerBlueprint.Name = "cmbScannerBlueprint";
            this.cmbScannerBlueprint.Size = new System.Drawing.Size(201, 21);
            this.cmbScannerBlueprint.TabIndex = 1;
            // 
            // flpScannedBy
            // 
            this.flpScannedBy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpScannedBy.AutoSize = true;
            this.flpScannedBy.Controls.Add(this.lblScannedBy);
            this.flpScannedBy.Controls.Add(this.txtScannedBy);
            this.flpScannedBy.Location = new System.Drawing.Point(2, 122);
            this.flpScannedBy.Margin = new System.Windows.Forms.Padding(2);
            this.flpScannedBy.Name = "flpScannedBy";
            this.flpScannedBy.Size = new System.Drawing.Size(827, 24);
            this.flpScannedBy.TabIndex = 4;
            this.flpScannedBy.WrapContents = false;
            // 
            // lblScannedBy
            // 
            this.lblScannedBy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblScannedBy.Location = new System.Drawing.Point(2, 3);
            this.lblScannedBy.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblScannedBy.Name = "lblScannedBy";
            this.lblScannedBy.Size = new System.Drawing.Size(100, 17);
            this.lblScannedBy.TabIndex = 2;
            this.lblScannedBy.Text = "Scanned By";
            this.lblScannedBy.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtScannedBy
            // 
            this.txtScannedBy.AllowSpaces = true;
            this.txtScannedBy.AutoFormat = true;
            this.txtScannedBy.ErrorMessage = "";
            this.txtScannedBy.InvalidColor = System.Drawing.Color.LightCoral;
            this.txtScannedBy.IsValid = true;
            this.txtScannedBy.Location = new System.Drawing.Point(106, 2);
            this.txtScannedBy.Margin = new System.Windows.Forms.Padding(2);
            this.txtScannedBy.Name = "txtScannedBy";
            this.txtScannedBy.Size = new System.Drawing.Size(201, 20);
            this.txtScannedBy.TabIndex = 7;
            this.txtScannedBy.ValidationPattern = null;
            this.txtScannedBy.ValidColor = System.Drawing.Color.White;
            // 
            // flpScanDateTime
            // 
            this.flpScanDateTime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpScanDateTime.AutoSize = true;
            this.flpScanDateTime.Controls.Add(this.lblScanDateTime);
            this.flpScanDateTime.Controls.Add(this.txtScanDateTime);
            this.flpScanDateTime.Controls.Add(this.dtpScanDateTime);
            this.flpScanDateTime.Location = new System.Drawing.Point(2, 150);
            this.flpScanDateTime.Margin = new System.Windows.Forms.Padding(2);
            this.flpScanDateTime.Name = "flpScanDateTime";
            this.flpScanDateTime.Size = new System.Drawing.Size(827, 24);
            this.flpScanDateTime.TabIndex = 5;
            this.flpScanDateTime.WrapContents = false;
            // 
            // lblScanDateTime
            // 
            this.lblScanDateTime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblScanDateTime.Location = new System.Drawing.Point(2, 3);
            this.lblScanDateTime.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblScanDateTime.Name = "lblScanDateTime";
            this.lblScanDateTime.Size = new System.Drawing.Size(100, 17);
            this.lblScanDateTime.TabIndex = 2;
            this.lblScanDateTime.Text = "Scan DateTime";
            this.lblScanDateTime.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtScanDateTime
            // 
            this.txtScanDateTime.AllowSpaces = true;
            this.txtScanDateTime.AutoFormat = true;
            this.txtScanDateTime.ErrorMessage = "";
            this.txtScanDateTime.InvalidColor = System.Drawing.Color.LightCoral;
            this.txtScanDateTime.IsValid = true;
            this.txtScanDateTime.Location = new System.Drawing.Point(106, 2);
            this.txtScanDateTime.Margin = new System.Windows.Forms.Padding(2);
            this.txtScanDateTime.Name = "txtScanDateTime";
            this.txtScanDateTime.ReadOnly = true;
            this.txtScanDateTime.Size = new System.Drawing.Size(201, 20);
            this.txtScanDateTime.TabIndex = 7;
            this.txtScanDateTime.ValidationPattern = null;
            this.txtScanDateTime.ValidColor = System.Drawing.Color.White;
            // 
            // dtpScanDateTime
            // 
            this.dtpScanDateTime.CustomFormat = "yyyy-MM-dd HH:mm";
            this.dtpScanDateTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpScanDateTime.Location = new System.Drawing.Point(311, 2);
            this.dtpScanDateTime.Margin = new System.Windows.Forms.Padding(2);
            this.dtpScanDateTime.Name = "dtpScanDateTime";
            this.dtpScanDateTime.Size = new System.Drawing.Size(160, 20);
            // 
            // flpSensorAbundance
            // 
            this.flpSensorAbundance.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpSensorAbundance.AutoSize = true;
            this.flpSensorAbundance.Controls.Add(this.lblSensorAbundance);
            this.flpSensorAbundance.Controls.Add(this.txtSensorAbundance);
            this.flpSensorAbundance.Location = new System.Drawing.Point(2, 178);
            this.flpSensorAbundance.Margin = new System.Windows.Forms.Padding(2);
            this.flpSensorAbundance.Name = "flpSensorAbundance";
            this.flpSensorAbundance.Size = new System.Drawing.Size(827, 24);
            this.flpSensorAbundance.TabIndex = 6;
            this.flpSensorAbundance.WrapContents = false;
            // 
            // lblSensorAbundance
            // 
            this.lblSensorAbundance.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSensorAbundance.Location = new System.Drawing.Point(2, 3);
            this.lblSensorAbundance.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblSensorAbundance.Name = "lblSensorAbundance";
            this.lblSensorAbundance.Size = new System.Drawing.Size(100, 17);
            this.lblSensorAbundance.TabIndex = 2;
            this.lblSensorAbundance.Text = "Sensor Abundance Factor";
            this.lblSensorAbundance.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtSensorAbundance
            // 
            this.txtSensorAbundance.AllowSpaces = true;
            this.txtSensorAbundance.AutoFormat = true;
            this.txtSensorAbundance.ErrorMessage = "";
            this.txtSensorAbundance.InvalidColor = System.Drawing.Color.LightCoral;
            this.txtSensorAbundance.IsValid = true;
            this.txtSensorAbundance.Location = new System.Drawing.Point(106, 2);
            this.txtSensorAbundance.Margin = new System.Windows.Forms.Padding(2);
            this.txtSensorAbundance.Name = "txtSensorAbundance";
            this.txtSensorAbundance.Size = new System.Drawing.Size(201, 20);
            this.txtSensorAbundance.TabIndex = 7;
            this.txtSensorAbundance.ValidationPattern = null;
            this.txtSensorAbundance.ValidColor = System.Drawing.Color.White;
            // 
            // flpPurityModifier
            // 
            this.flpPurityModifier.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpPurityModifier.AutoSize = true;
            this.flpPurityModifier.Controls.Add(this.lblPurityModifier);
            this.flpPurityModifier.Controls.Add(this.txtPurityModifier);
            this.flpPurityModifier.Location = new System.Drawing.Point(2, 206);
            this.flpPurityModifier.Margin = new System.Windows.Forms.Padding(2);
            this.flpPurityModifier.Name = "flpPurityModifier";
            this.flpPurityModifier.Size = new System.Drawing.Size(827, 24);
            this.flpPurityModifier.TabIndex = 7;
            this.flpPurityModifier.WrapContents = false;
            // 
            // lblPurityModifier
            // 
            this.lblPurityModifier.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblPurityModifier.Location = new System.Drawing.Point(2, 3);
            this.lblPurityModifier.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblPurityModifier.Name = "lblPurityModifier";
            this.lblPurityModifier.Size = new System.Drawing.Size(100, 17);
            this.lblPurityModifier.TabIndex = 2;
            this.lblPurityModifier.Text = "Purity Modifier";
            this.lblPurityModifier.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtPurityModifier
            // 
            this.txtPurityModifier.AllowSpaces = true;
            this.txtPurityModifier.AutoFormat = true;
            this.txtPurityModifier.ErrorMessage = "";
            this.txtPurityModifier.InvalidColor = System.Drawing.Color.LightCoral;
            this.txtPurityModifier.IsValid = true;
            this.txtPurityModifier.Location = new System.Drawing.Point(106, 2);
            this.txtPurityModifier.Margin = new System.Windows.Forms.Padding(2);
            this.txtPurityModifier.Name = "txtPurityModifier";
            this.txtPurityModifier.Size = new System.Drawing.Size(201, 20);
            this.txtPurityModifier.TabIndex = 7;
            this.txtPurityModifier.ValidationPattern = null;
            this.txtPurityModifier.ValidColor = System.Drawing.Color.White;
            // 
            // flpScanLevel
            // 
            this.flpScanLevel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpScanLevel.AutoSize = true;
            this.flpScanLevel.Controls.Add(this.lblScanLevel);
            this.flpScanLevel.Controls.Add(this.txtScanLevel);
            this.flpScanLevel.Location = new System.Drawing.Point(2, 234);
            this.flpScanLevel.Margin = new System.Windows.Forms.Padding(2);
            this.flpScanLevel.Name = "flpScanLevel";
            this.flpScanLevel.Size = new System.Drawing.Size(827, 24);
            this.flpScanLevel.TabIndex = 8;
            this.flpScanLevel.WrapContents = false;
            // 
            // lblScanLevel
            // 
            this.lblScanLevel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblScanLevel.Location = new System.Drawing.Point(2, 3);
            this.lblScanLevel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblScanLevel.Name = "lblScanLevel";
            this.lblScanLevel.Size = new System.Drawing.Size(100, 17);
            this.lblScanLevel.TabIndex = 2;
            this.lblScanLevel.Text = "Scan Level";
            this.lblScanLevel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtScanLevel
            // 
            this.txtScanLevel.AllowSpaces = true;
            this.txtScanLevel.AutoFormat = true;
            this.txtScanLevel.ErrorMessage = "";
            this.txtScanLevel.InvalidColor = System.Drawing.Color.LightCoral;
            this.txtScanLevel.IsValid = true;
            this.txtScanLevel.Location = new System.Drawing.Point(106, 2);
            this.txtScanLevel.Margin = new System.Windows.Forms.Padding(2);
            this.txtScanLevel.Name = "txtScanLevel";
            this.txtScanLevel.Size = new System.Drawing.Size(201, 20);
            this.txtScanLevel.TabIndex = 7;
            this.txtScanLevel.ValidationPattern = null;
            this.txtScanLevel.ValidColor = System.Drawing.Color.White;
            // 
            // dgvResources
            // 
            this.dgvResources.AllowUserToOrderColumns = true;
            this.dgvResources.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvResources.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Resource,
            this.Purity,
            this.Amount});
            this.dgvResources.Location = new System.Drawing.Point(3, 263);
            this.dgvResources.Name = "dgvResources";
            this.dgvResources.Size = new System.Drawing.Size(825, 303);
            this.dgvResources.TabIndex = 9;
            this.dgvResources.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.dgvResources_CellValidating);
            // 
            // Resource
            // 
            this.Resource.FillWeight = 250F;
            this.Resource.HeaderText = "Resource";
            this.Resource.Name = "Resource";
            this.Resource.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.Resource.Width = 250;
            // 
            // Purity
            // 
            this.Purity.HeaderText = "Purity";
            this.Purity.Name = "Purity";
            this.Purity.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // Amount
            // 
            this.Amount.AllowSpaces = false;
            this.Amount.HeaderText = "Amount";
            this.Amount.InvalidColor = System.Drawing.Color.LightCoral;
            this.Amount.Name = "Amount";
            this.Amount.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.Amount.ValidationPattern = "^[+-]?\\d+(\\.\\d+)?$";
            this.Amount.ValidColor = System.Drawing.Color.White;
            // 
            // flpCommands
            // 
            this.flpCommands.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpCommands.AutoSize = true;
            this.flpCommands.Controls.Add(this.cmdNew);
            this.flpCommands.Controls.Add(this.btnSave);
            this.flpCommands.Controls.Add(this.cmdDelete);
            this.flpCommands.Controls.Add(this.cmdImport);
            this.flpCommands.Location = new System.Drawing.Point(2, 575);
            this.flpCommands.Margin = new System.Windows.Forms.Padding(2);
            this.flpCommands.Name = "flpCommands";
            this.flpCommands.Size = new System.Drawing.Size(831, 29);
            this.flpCommands.TabIndex = 1;
            // 
            // cmdNew
            // 
            this.cmdNew.Location = new System.Drawing.Point(3, 3);
            this.cmdNew.Name = "cmdNew";
            this.cmdNew.Size = new System.Drawing.Size(75, 23);
            this.cmdNew.TabIndex = 4;
            this.cmdNew.Text = "New";
            this.cmdNew.UseVisualStyleBackColor = true;
            this.cmdNew.Click += new System.EventHandler(this.cmdNew_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(84, 3);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // cmdDelete
            // 
            this.cmdDelete.Location = new System.Drawing.Point(165, 3);
            this.cmdDelete.Name = "cmdDelete";
            this.cmdDelete.Size = new System.Drawing.Size(75, 23);
            this.cmdDelete.TabIndex = 1;
            this.cmdDelete.Text = "Delete";
            this.cmdDelete.UseVisualStyleBackColor = true;
            this.cmdDelete.Click += new System.EventHandler(this.cmdDelete_Click);
            // 
            // cmdImport
            // 
            this.cmdImport.Location = new System.Drawing.Point(246, 3);
            this.cmdImport.Name = "cmdImport";
            this.cmdImport.Size = new System.Drawing.Size(75, 23);
            this.cmdImport.TabIndex = 3;
            this.cmdImport.Text = "Import";
            this.cmdImport.UseVisualStyleBackColor = true;
            this.cmdImport.Click += new System.EventHandler(this.cmdImport_Click);
            // 
            // FormSurvey
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1074, 704);
            this.Controls.Add(this.flpBase);
            this.Name = "FormSurvey";
            this.Text = "FormSurvey";
            this.flpBase.ResumeLayout(false);
            this.flpSearchList.ResumeLayout(false);
            this.flpSearchList.PerformLayout();
            this.flpSurveyFilter.ResumeLayout(false);
            this.flpSurveyFilter.PerformLayout();
            this.flpResource.ResumeLayout(false);
            this.flpSurveyData.ResumeLayout(false);
            this.flpSurveyData.PerformLayout();
            this.flpSurveyDetails.ResumeLayout(false);
            this.flpSurveyDetails.PerformLayout();
            this.flpPlanetName.ResumeLayout(false);
            this.flpPlanetName.PerformLayout();
            this.flpSurveyID.ResumeLayout(false);
            this.flpSurveyID.PerformLayout();
            this.flpNickName.ResumeLayout(false);
            this.flpNickName.PerformLayout();
            this.flpScannerBlueprint.ResumeLayout(false);
            this.flpScannerBlueprint.PerformLayout();
            this.flpScannedBy.ResumeLayout(false);
            this.flpScannedBy.PerformLayout();
            this.flpScanDateTime.ResumeLayout(false);
            this.flpScanDateTime.PerformLayout();
            this.flpSensorAbundance.ResumeLayout(false);
            this.flpSensorAbundance.PerformLayout();
            this.flpPurityModifier.ResumeLayout(false);
            this.flpPurityModifier.PerformLayout();
            this.flpScanLevel.ResumeLayout(false);
            this.flpScanLevel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResources)).EndInit();
            this.flpCommands.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flpBase;
        private System.Windows.Forms.FlowLayoutPanel flpSearchList;
        private System.Windows.Forms.FlowLayoutPanel flpSurveyFilter;
        private System.Windows.Forms.Label lblSurveyFilter;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtSurveyFilter;
        private System.Windows.Forms.FlowLayoutPanel flpResource;
        private System.Windows.Forms.Label lblResource;
        private System.Windows.Forms.ComboBox cmbResource;
        private System.Windows.Forms.FlowLayoutPanel flpTypeFilter;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.ComboBox cmbSurveyType;
        private System.Windows.Forms.Label lblPurity;
        private System.Windows.Forms.ComboBox cmbPurityFilter;
        private System.Windows.Forms.Label lblMinAmount;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtMinAmount;
        private System.Windows.Forms.ListView lvwSurveys;
        private System.Windows.Forms.FlowLayoutPanel flpSurveyData;
        private System.Windows.Forms.FlowLayoutPanel flpSurveyDetails;
        private System.Windows.Forms.FlowLayoutPanel flpPlanetName;
        private System.Windows.Forms.Label lblPlanetName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtPlanetName;
        private System.Windows.Forms.FlowLayoutPanel flpSystemName;
        private System.Windows.Forms.Label lblSystemName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtSystemName;
        private System.Windows.Forms.FlowLayoutPanel flpSurveyTypeEdit;
        private System.Windows.Forms.Label lblSurveyTypeEdit;
        private System.Windows.Forms.ComboBox cmbSurveyTypeEdit;
        private System.Windows.Forms.FlowLayoutPanel flpScannerBlueprint;
        private System.Windows.Forms.Label lblScannerBlueprint;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtFilterScannerBlueprint;
        private System.Windows.Forms.ComboBox cmbScannerBlueprint;
        private System.Windows.Forms.FlowLayoutPanel flpScannedBy;
        private System.Windows.Forms.Label lblScannedBy;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtScannedBy;
        private System.Windows.Forms.FlowLayoutPanel flpSensorAbundance;
        private System.Windows.Forms.Label lblSensorAbundance;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtSensorAbundance;
        private System.Windows.Forms.FlowLayoutPanel flpPurityModifier;
        private System.Windows.Forms.Label lblPurityModifier;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtPurityModifier;
        private System.Windows.Forms.FlowLayoutPanel flpScanLevel;
        private System.Windows.Forms.Label lblScanLevel;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtScanLevel;
        private System.Windows.Forms.DataGridView dgvResources;
        private System.Windows.Forms.FlowLayoutPanel flpCommands;
        private System.Windows.Forms.Button cmdNew;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button cmdDelete;
        private System.Windows.Forms.Button cmdImport;
        private System.Windows.Forms.FlowLayoutPanel flpSurveyID;
        private System.Windows.Forms.Label lblSurveyID;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtSurveyID;
        private System.Windows.Forms.FlowLayoutPanel flpScanDateTime;
        private System.Windows.Forms.Label lblScanDateTime;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtScanDateTime;
        private System.Windows.Forms.FlowLayoutPanel flpNickName;
        private System.Windows.Forms.Label lblNickName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtNickName;
        private System.Windows.Forms.DataGridViewComboBoxColumn Resource;
        private System.Windows.Forms.DataGridViewComboBoxColumn Purity;
        private OE2EmpireTracker.Controls.DataGridViewValidatedTextBoxColumn Amount;
        private System.Windows.Forms.DateTimePicker dtpScanDateTime;
    }
}