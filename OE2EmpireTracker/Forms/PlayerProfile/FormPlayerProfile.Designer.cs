namespace OE2EmpireTracker.Forms.PlayerProfile
{
    partial class FormPlayerProfile
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
            this.lblNameFilter = new System.Windows.Forms.Label();
            this.txtNameFilter = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpResource = new System.Windows.Forms.FlowLayoutPanel();
            this.lblResource = new System.Windows.Forms.Label();
            this.cmbResource = new System.Windows.Forms.ComboBox();
            this.lvwPlayerProfiles = new System.Windows.Forms.ListView();
            this.flpPlayerData = new System.Windows.Forms.FlowLayoutPanel();
            this.flpPlayerDetails = new System.Windows.Forms.FlowLayoutPanel();
            this.flpPlayerName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblPlayerName = new System.Windows.Forms.Label();
            this.txtPlayerName = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpCharacterId = new System.Windows.Forms.FlowLayoutPanel();
            this.lblCharacterId = new System.Windows.Forms.Label();
            this.lblCharacterIdValue = new System.Windows.Forms.Label();
            this.flpFirstName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblFirstName = new System.Windows.Forms.Label();
            this.lblFirstNameValue = new System.Windows.Forms.Label();
            this.flpLastName = new System.Windows.Forms.FlowLayoutPanel();
            this.lblLastName = new System.Windows.Forms.Label();
            this.lblLastNameValue = new System.Windows.Forms.Label();
            this.flpActiveTime = new System.Windows.Forms.FlowLayoutPanel();
            this.lblActiveTime = new System.Windows.Forms.Label();
            this.lblActiveTimeValue = new System.Windows.Forms.Label();
            this.flpTotalCredits = new System.Windows.Forms.FlowLayoutPanel();
            this.lblTotalCredits = new System.Windows.Forms.Label();
            this.txtTotalCredits = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpFaction = new System.Windows.Forms.FlowLayoutPanel();
            this.lblFaction = new System.Windows.Forms.Label();
            this.txtFaction = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.cmbFaction = new System.Windows.Forms.ComboBox();
            this.flpPublicRankBlock = new System.Windows.Forms.FlowLayoutPanel();
            this.flpPublicRank = new System.Windows.Forms.FlowLayoutPanel();
            this.lblPublicRank = new System.Windows.Forms.Label();
            this.txtPublicRank = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblPublicRankName = new System.Windows.Forms.Label();
            this.flpPublicRankCurXP = new System.Windows.Forms.FlowLayoutPanel();
            this.lblPublicRankCurXP = new System.Windows.Forms.Label();
            this.txtPublicRankCurXP = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpPublicRankNextXP = new System.Windows.Forms.FlowLayoutPanel();
            this.lblPublicRankNextXP = new System.Windows.Forms.Label();
            this.txtPublicRankNextXP = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpPrivateRankBlock = new System.Windows.Forms.FlowLayoutPanel();
            this.flpPrivateRank = new System.Windows.Forms.FlowLayoutPanel();
            this.lblPrivateRank = new System.Windows.Forms.Label();
            this.txtPrivateRank = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblPrivateRankName = new System.Windows.Forms.Label();
            this.flpPrivateRankCurXP = new System.Windows.Forms.FlowLayoutPanel();
            this.lblPrivateRankCurXP = new System.Windows.Forms.Label();
            this.txtPrivateRankCurXP = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpPrivateRankNextXP = new System.Windows.Forms.FlowLayoutPanel();
            this.lblPrivateRankNextXP = new System.Windows.Forms.Label();
            this.txtPrivateRankNextXP = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpMilitaryRankBlock = new System.Windows.Forms.FlowLayoutPanel();
            this.flpMilitaryRank = new System.Windows.Forms.FlowLayoutPanel();
            this.lblMilitaryRank = new System.Windows.Forms.Label();
            this.txtMilitaryRank = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblMilitaryRankName = new System.Windows.Forms.Label();
            this.flpMilitaryRankCurXP = new System.Windows.Forms.FlowLayoutPanel();
            this.lblMilitaryRankCurXP = new System.Windows.Forms.Label();
            this.txtMilitaryRankCurXP = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpMilitaryRankNextXP = new System.Windows.Forms.FlowLayoutPanel();
            this.lblMilitaryRankNextXP = new System.Windows.Forms.Label();
            this.txtMilitaryRankNextXP = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpSkillPoints = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSkillPoints = new System.Windows.Forms.Label();
            this.txtSkillPoints = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.flpColonyDirectorSkillGroup = new System.Windows.Forms.FlowLayoutPanel();
            this.flpColonyDirector = new System.Windows.Forms.FlowLayoutPanel();
            this.lblColonyDirector = new System.Windows.Forms.Label();
            this.chkColonyDirector = new System.Windows.Forms.CheckBox();
            this.pskHumanResources = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.pskForeman = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.flpColonyFounderSkillGroup = new System.Windows.Forms.FlowLayoutPanel();
            this.flpColonyFounder = new System.Windows.Forms.FlowLayoutPanel();
            this.lblColonyFounder = new System.Windows.Forms.Label();
            this.chkColonyFounder = new System.Windows.Forms.CheckBox();
            this.pskFounder = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.pskEnergyEfficiency = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.pskBuilder = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.flpColonyOperationsSkillGroup = new System.Windows.Forms.FlowLayoutPanel();
            this.flpColonyOperations = new System.Windows.Forms.FlowLayoutPanel();
            this.lblColonyOperations = new System.Windows.Forms.Label();
            this.chkColonyOperations = new System.Windows.Forms.CheckBox();
            this.pskRefiningFocus = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.pskProductionFocus = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.pskExtractionFocus = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.flpCommanderSkillGroup = new System.Windows.Forms.FlowLayoutPanel();
            this.flpCommander = new System.Windows.Forms.FlowLayoutPanel();
            this.lblCommander = new System.Windows.Forms.Label();
            this.chkCommander = new System.Windows.Forms.CheckBox();
            this.pskDamageControl = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.flpEngineerSkillGroup = new System.Windows.Forms.FlowLayoutPanel();
            this.flpEngineer = new System.Windows.Forms.FlowLayoutPanel();
            this.lblEngineer = new System.Windows.Forms.Label();
            this.chkEngineer = new System.Windows.Forms.CheckBox();
            this.pskEngineeringCapacity = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.flpEntrepeneurSkillGroup = new System.Windows.Forms.FlowLayoutPanel();
            this.flpEntrepeneur = new System.Windows.Forms.FlowLayoutPanel();
            this.lblEntrepeneur = new System.Windows.Forms.Label();
            this.chkEntrepeneur = new System.Windows.Forms.CheckBox();
            this.pskSoundAsAPound = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.pskSelfMadeMillionaire = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.pskAAAHealthcare = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.flpJobManagementSkillGroup = new System.Windows.Forms.FlowLayoutPanel();
            this.flpJobManagement = new System.Windows.Forms.FlowLayoutPanel();
            this.lblJobManagement = new System.Windows.Forms.Label();
            this.chkJobManagement = new System.Windows.Forms.CheckBox();
            this.pskJobOpportunities = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.pskContractManagement = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.flpResearcherSkillGroup = new System.Windows.Forms.FlowLayoutPanel();
            this.flpResearcher = new System.Windows.Forms.FlowLayoutPanel();
            this.lblResearcher = new System.Windows.Forms.Label();
            this.chkResearcher = new System.Windows.Forms.CheckBox();
            this.pskResearchReview = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.pskResearchMethods = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.pskResearchFocus = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.flpSurveyorSkillGroup = new System.Windows.Forms.FlowLayoutPanel();
            this.flpSurveyor = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSurveyor = new System.Windows.Forms.Label();
            this.chkSurveyor = new System.Windows.Forms.CheckBox();
            this.pskSurveyingMethods = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.pskScanningMethods = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.pskQuartermaster = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.flpTraderSkillGroup = new System.Windows.Forms.FlowLayoutPanel();
            this.flpTrader = new System.Windows.Forms.FlowLayoutPanel();
            this.lblTrader = new System.Windows.Forms.Label();
            this.chkTrader = new System.Windows.Forms.CheckBox();
            this.pskBroker = new OE2EmpireTracker.Forms.PlayerProfile.PlayerSkillBlock();
            this.flpCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdImport = new System.Windows.Forms.Button();
            this.cmdSyncApi = new System.Windows.Forms.Button();
            this.cmdNew = new System.Windows.Forms.Button();
            this.cmdSave = new System.Windows.Forms.Button();
            this.cmdDelete = new System.Windows.Forms.Button();
            this.flpBase.SuspendLayout();
            this.flpSearchList.SuspendLayout();
            this.flpBlueprintSearch.SuspendLayout();
            this.flpResource.SuspendLayout();
            this.flpPlayerData.SuspendLayout();
            this.flpPlayerDetails.SuspendLayout();
            this.flpPlayerName.SuspendLayout();
            this.flpCharacterId.SuspendLayout();
            this.flpFirstName.SuspendLayout();
            this.flpLastName.SuspendLayout();
            this.flpActiveTime.SuspendLayout();
            this.flpTotalCredits.SuspendLayout();
            this.flpFaction.SuspendLayout();
            this.flpPublicRankBlock.SuspendLayout();
            this.flpPublicRank.SuspendLayout();
            this.flpPublicRankCurXP.SuspendLayout();
            this.flpPublicRankNextXP.SuspendLayout();
            this.flpPrivateRankBlock.SuspendLayout();
            this.flpPrivateRank.SuspendLayout();
            this.flpPrivateRankCurXP.SuspendLayout();
            this.flpPrivateRankNextXP.SuspendLayout();
            this.flpMilitaryRankBlock.SuspendLayout();
            this.flpMilitaryRank.SuspendLayout();
            this.flpMilitaryRankCurXP.SuspendLayout();
            this.flpMilitaryRankNextXP.SuspendLayout();
            this.flpSkillPoints.SuspendLayout();
            this.flpColonyDirectorSkillGroup.SuspendLayout();
            this.flpColonyDirector.SuspendLayout();
            this.flpColonyFounderSkillGroup.SuspendLayout();
            this.flpColonyFounder.SuspendLayout();
            this.flpColonyOperationsSkillGroup.SuspendLayout();
            this.flpColonyOperations.SuspendLayout();
            this.flpCommanderSkillGroup.SuspendLayout();
            this.flpCommander.SuspendLayout();
            this.flpEngineerSkillGroup.SuspendLayout();
            this.flpEngineer.SuspendLayout();
            this.flpEntrepeneurSkillGroup.SuspendLayout();
            this.flpEntrepeneur.SuspendLayout();
            this.flpJobManagementSkillGroup.SuspendLayout();
            this.flpJobManagement.SuspendLayout();
            this.flpResearcherSkillGroup.SuspendLayout();
            this.flpResearcher.SuspendLayout();
            this.flpSurveyorSkillGroup.SuspendLayout();
            this.flpSurveyor.SuspendLayout();
            this.flpTraderSkillGroup.SuspendLayout();
            this.flpTrader.SuspendLayout();
            this.flpCommands.SuspendLayout();
            this.SuspendLayout();
            // 
            // flpBase
            // 
            this.flpBase.AutoSize = true;
            this.flpBase.Controls.Add(this.flpSearchList);
            this.flpBase.Controls.Add(this.flpPlayerData);
            this.flpBase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpBase.Location = new System.Drawing.Point(0, 0);
            this.flpBase.Name = "flpBase";
            this.flpBase.Size = new System.Drawing.Size(1079, 594);
            this.flpBase.TabIndex = 11;
            this.flpBase.WrapContents = false;
            // 
            // flpSearchList
            // 
            this.flpSearchList.Controls.Add(this.flpBlueprintSearch);
            this.flpSearchList.Controls.Add(this.flpResource);
            this.flpSearchList.Controls.Add(this.lvwPlayerProfiles);
            this.flpSearchList.Location = new System.Drawing.Point(3, 3);
            this.flpSearchList.Name = "flpSearchList";
            this.flpSearchList.Size = new System.Drawing.Size(427, 641);
            this.flpSearchList.TabIndex = 10;
            // 
            // flpBlueprintSearch
            // 
            this.flpBlueprintSearch.AutoSize = true;
            this.flpBlueprintSearch.Controls.Add(this.lblNameFilter);
            this.flpBlueprintSearch.Controls.Add(this.txtNameFilter);
            this.flpBlueprintSearch.Location = new System.Drawing.Point(2, 2);
            this.flpBlueprintSearch.Margin = new System.Windows.Forms.Padding(2);
            this.flpBlueprintSearch.Name = "flpBlueprintSearch";
            this.flpBlueprintSearch.Size = new System.Drawing.Size(312, 26);
            this.flpBlueprintSearch.TabIndex = 0;
            // 
            // lblNameFilter
            // 
            this.lblNameFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblNameFilter.Location = new System.Drawing.Point(2, 4);
            this.lblNameFilter.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblNameFilter.Name = "lblNameFilter";
            this.lblNameFilter.Size = new System.Drawing.Size(100, 17);
            this.lblNameFilter.TabIndex = 2;
            this.lblNameFilter.Text = "Name";
            this.lblNameFilter.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtNameFilter
            // 
            this.txtNameFilter.Location = new System.Drawing.Point(107, 3);
            this.txtNameFilter.Name = "txtNameFilter";
            this.txtNameFilter.Size = new System.Drawing.Size(202, 20);
            this.txtNameFilter.TabIndex = 0;
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
            // lvwPlayerProfiles
            // 
            this.lvwPlayerProfiles.FullRowSelect = true;
            this.lvwPlayerProfiles.HideSelection = false;
            this.lvwPlayerProfiles.Location = new System.Drawing.Point(3, 62);
            this.lvwPlayerProfiles.MultiSelect = false;
            this.lvwPlayerProfiles.Name = "lvwPlayerProfiles";
            this.lvwPlayerProfiles.Size = new System.Drawing.Size(412, 566);
            this.lvwPlayerProfiles.TabIndex = 6;
            this.lvwPlayerProfiles.UseCompatibleStateImageBehavior = false;
            // 
            // flpPlayerData
            // 
            this.flpPlayerData.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flpPlayerData.AutoScroll = true;
            this.flpPlayerData.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpPlayerData.Controls.Add(this.flpPlayerDetails);
            this.flpPlayerData.Controls.Add(this.flpCommands);
            this.flpPlayerData.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpPlayerData.Location = new System.Drawing.Point(436, 3);
            this.flpPlayerData.Name = "flpPlayerData";
            this.flpPlayerData.Size = new System.Drawing.Size(661, 596);
            this.flpPlayerData.TabIndex = 3;
            this.flpPlayerData.WrapContents = false;
            // 
            // flpPlayerDetails
            // 
            this.flpPlayerDetails.AutoScroll = true;
            this.flpPlayerDetails.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpPlayerDetails.Controls.Add(this.flpPlayerName);
            this.flpPlayerDetails.Controls.Add(this.flpCharacterId);
            this.flpPlayerDetails.Controls.Add(this.flpFirstName);
            this.flpPlayerDetails.Controls.Add(this.flpLastName);
            this.flpPlayerDetails.Controls.Add(this.flpActiveTime);
            this.flpPlayerDetails.Controls.Add(this.flpTotalCredits);
            this.flpPlayerDetails.Controls.Add(this.flpFaction);
            this.flpPlayerDetails.Controls.Add(this.flpPublicRankBlock);
            this.flpPlayerDetails.Controls.Add(this.flpPrivateRankBlock);
            this.flpPlayerDetails.Controls.Add(this.flpMilitaryRankBlock);
            this.flpPlayerDetails.Controls.Add(this.flpSkillPoints);
            this.flpPlayerDetails.Controls.Add(this.flpColonyDirectorSkillGroup);
            this.flpPlayerDetails.Controls.Add(this.flpColonyFounderSkillGroup);
            this.flpPlayerDetails.Controls.Add(this.flpColonyOperationsSkillGroup);
            this.flpPlayerDetails.Controls.Add(this.flpCommanderSkillGroup);
            this.flpPlayerDetails.Controls.Add(this.flpEngineerSkillGroup);
            this.flpPlayerDetails.Controls.Add(this.flpEntrepeneurSkillGroup);
            this.flpPlayerDetails.Controls.Add(this.flpJobManagementSkillGroup);
            this.flpPlayerDetails.Controls.Add(this.flpResearcherSkillGroup);
            this.flpPlayerDetails.Controls.Add(this.flpSurveyorSkillGroup);
            this.flpPlayerDetails.Controls.Add(this.flpTraderSkillGroup);
            this.flpPlayerDetails.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpPlayerDetails.Location = new System.Drawing.Point(2, 2);
            this.flpPlayerDetails.Margin = new System.Windows.Forms.Padding(2);
            this.flpPlayerDetails.Name = "flpPlayerDetails";
            this.flpPlayerDetails.Size = new System.Drawing.Size(613, 400);
            this.flpPlayerDetails.TabIndex = 0;
            this.flpPlayerDetails.WrapContents = false;
            // 
            // flpPlayerName
            // 
            this.flpPlayerName.AutoSize = true;
            this.flpPlayerName.Controls.Add(this.lblPlayerName);
            this.flpPlayerName.Controls.Add(this.txtPlayerName);
            this.flpPlayerName.Location = new System.Drawing.Point(2, 2);
            this.flpPlayerName.Margin = new System.Windows.Forms.Padding(2);
            this.flpPlayerName.Name = "flpPlayerName";
            this.flpPlayerName.Size = new System.Drawing.Size(310, 26);
            this.flpPlayerName.TabIndex = 0;
            // 
            // lblPlayerName
            // 
            this.lblPlayerName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblPlayerName.Location = new System.Drawing.Point(2, 4);
            this.lblPlayerName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblPlayerName.Name = "lblPlayerName";
            this.lblPlayerName.Size = new System.Drawing.Size(100, 17);
            this.lblPlayerName.TabIndex = 2;
            this.lblPlayerName.Text = "Player Name";
            this.lblPlayerName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtPlayerName
            // 
            this.txtPlayerName.Location = new System.Drawing.Point(107, 3);
            this.txtPlayerName.Name = "txtPlayerName";
            this.txtPlayerName.Size = new System.Drawing.Size(200, 20);
            this.txtPlayerName.TabIndex = 0;
            this.txtPlayerName.TextChanged += new System.EventHandler(this.TxtPlayerName_TextChanged);
            // 
            // flpCharacterId
            // 
            this.flpCharacterId.AutoSize = true;
            this.flpCharacterId.Controls.Add(this.lblCharacterId);
            this.flpCharacterId.Controls.Add(this.lblCharacterIdValue);
            this.flpCharacterId.Location = new System.Drawing.Point(2, 32);
            this.flpCharacterId.Margin = new System.Windows.Forms.Padding(2);
            this.flpCharacterId.Name = "flpCharacterId";
            this.flpCharacterId.Size = new System.Drawing.Size(310, 26);
            this.flpCharacterId.TabIndex = 20;
            // 
            // lblCharacterId
            // 
            this.lblCharacterId.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCharacterId.Location = new System.Drawing.Point(2, 4);
            this.lblCharacterId.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblCharacterId.Name = "lblCharacterId";
            this.lblCharacterId.Size = new System.Drawing.Size(100, 17);
            this.lblCharacterId.TabIndex = 0;
            this.lblCharacterId.Text = "Character ID";
            this.lblCharacterId.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblCharacterIdValue
            // 
            this.lblCharacterIdValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCharacterIdValue.Location = new System.Drawing.Point(107, 4);
            this.lblCharacterIdValue.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.lblCharacterIdValue.Name = "lblCharacterIdValue";
            this.lblCharacterIdValue.Size = new System.Drawing.Size(200, 17);
            this.lblCharacterIdValue.TabIndex = 1;
            this.lblCharacterIdValue.Text = "\u2014";
            this.lblCharacterIdValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // flpFirstName
            // 
            this.flpFirstName.AutoSize = true;
            this.flpFirstName.Controls.Add(this.lblFirstName);
            this.flpFirstName.Controls.Add(this.lblFirstNameValue);
            this.flpFirstName.Location = new System.Drawing.Point(2, 62);
            this.flpFirstName.Margin = new System.Windows.Forms.Padding(2);
            this.flpFirstName.Name = "flpFirstName";
            this.flpFirstName.Size = new System.Drawing.Size(310, 26);
            this.flpFirstName.TabIndex = 21;
            this.flpFirstName.Visible = false;
            // 
            // lblFirstName
            // 
            this.lblFirstName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFirstName.Location = new System.Drawing.Point(2, 4);
            this.lblFirstName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblFirstName.Name = "lblFirstName";
            this.lblFirstName.Size = new System.Drawing.Size(100, 17);
            this.lblFirstName.TabIndex = 0;
            this.lblFirstName.Text = "First Name";
            this.lblFirstName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblFirstNameValue
            // 
            this.lblFirstNameValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFirstNameValue.Location = new System.Drawing.Point(107, 4);
            this.lblFirstNameValue.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.lblFirstNameValue.Name = "lblFirstNameValue";
            this.lblFirstNameValue.Size = new System.Drawing.Size(200, 17);
            this.lblFirstNameValue.TabIndex = 1;
            this.lblFirstNameValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // flpLastName
            // 
            this.flpLastName.AutoSize = true;
            this.flpLastName.Controls.Add(this.lblLastName);
            this.flpLastName.Controls.Add(this.lblLastNameValue);
            this.flpLastName.Location = new System.Drawing.Point(2, 92);
            this.flpLastName.Margin = new System.Windows.Forms.Padding(2);
            this.flpLastName.Name = "flpLastName";
            this.flpLastName.Size = new System.Drawing.Size(310, 26);
            this.flpLastName.TabIndex = 22;
            this.flpLastName.Visible = false;
            // 
            // lblLastName
            // 
            this.lblLastName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLastName.Location = new System.Drawing.Point(2, 4);
            this.lblLastName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblLastName.Name = "lblLastName";
            this.lblLastName.Size = new System.Drawing.Size(100, 17);
            this.lblLastName.TabIndex = 0;
            this.lblLastName.Text = "Last Name";
            this.lblLastName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblLastNameValue
            // 
            this.lblLastNameValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLastNameValue.Location = new System.Drawing.Point(107, 4);
            this.lblLastNameValue.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.lblLastNameValue.Name = "lblLastNameValue";
            this.lblLastNameValue.Size = new System.Drawing.Size(200, 17);
            this.lblLastNameValue.TabIndex = 1;
            this.lblLastNameValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // flpActiveTime
            // 
            this.flpActiveTime.AutoSize = true;
            this.flpActiveTime.Controls.Add(this.lblActiveTime);
            this.flpActiveTime.Controls.Add(this.lblActiveTimeValue);
            this.flpActiveTime.Location = new System.Drawing.Point(2, 122);
            this.flpActiveTime.Margin = new System.Windows.Forms.Padding(2);
            this.flpActiveTime.Name = "flpActiveTime";
            this.flpActiveTime.Size = new System.Drawing.Size(310, 26);
            this.flpActiveTime.TabIndex = 23;
            // 
            // lblActiveTime
            // 
            this.lblActiveTime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblActiveTime.Location = new System.Drawing.Point(2, 4);
            this.lblActiveTime.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblActiveTime.Name = "lblActiveTime";
            this.lblActiveTime.Size = new System.Drawing.Size(100, 17);
            this.lblActiveTime.TabIndex = 0;
            this.lblActiveTime.Text = "Active Time";
            this.lblActiveTime.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblActiveTimeValue
            // 
            this.lblActiveTimeValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblActiveTimeValue.Location = new System.Drawing.Point(107, 4);
            this.lblActiveTimeValue.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.lblActiveTimeValue.Name = "lblActiveTimeValue";
            this.lblActiveTimeValue.Size = new System.Drawing.Size(200, 17);
            this.lblActiveTimeValue.TabIndex = 1;
            this.lblActiveTimeValue.Text = "\u2014";
            this.lblActiveTimeValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // flpTotalCredits
            // 
            this.flpTotalCredits.AutoSize = true;
            this.flpTotalCredits.Controls.Add(this.lblTotalCredits);
            this.flpTotalCredits.Controls.Add(this.txtTotalCredits);
            this.flpTotalCredits.Location = new System.Drawing.Point(2, 32);
            this.flpTotalCredits.Margin = new System.Windows.Forms.Padding(2);
            this.flpTotalCredits.Name = "flpTotalCredits";
            this.flpTotalCredits.Size = new System.Drawing.Size(310, 26);
            this.flpTotalCredits.TabIndex = 1;
            // 
            // lblTotalCredits
            // 
            this.lblTotalCredits.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTotalCredits.Location = new System.Drawing.Point(2, 4);
            this.lblTotalCredits.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblTotalCredits.Name = "lblTotalCredits";
            this.lblTotalCredits.Size = new System.Drawing.Size(100, 17);
            this.lblTotalCredits.TabIndex = 2;
            this.lblTotalCredits.Text = "Total Credits";
            this.lblTotalCredits.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtTotalCredits
            // 
            this.txtTotalCredits.Location = new System.Drawing.Point(107, 3);
            this.txtTotalCredits.Name = "txtTotalCredits";
            this.txtTotalCredits.Size = new System.Drawing.Size(200, 20);
            this.txtTotalCredits.TabIndex = 0;
            // 
            // flpFaction
            // 
            this.flpFaction.AutoSize = true;
            this.flpFaction.Controls.Add(this.lblFaction);
            this.flpFaction.Controls.Add(this.txtFaction);
            this.flpFaction.Controls.Add(this.cmbFaction);
            this.flpFaction.Location = new System.Drawing.Point(2, 62);
            this.flpFaction.Margin = new System.Windows.Forms.Padding(2);
            this.flpFaction.Name = "flpFaction";
            this.flpFaction.Size = new System.Drawing.Size(415, 26);
            this.flpFaction.TabIndex = 3;
            // 
            // lblFaction
            // 
            this.lblFaction.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFaction.Location = new System.Drawing.Point(2, 4);
            this.lblFaction.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblFaction.Name = "lblFaction";
            this.lblFaction.Size = new System.Drawing.Size(100, 17);
            this.lblFaction.TabIndex = 2;
            this.lblFaction.Text = "Faction";
            this.lblFaction.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtFaction
            // 
            this.txtFaction.Location = new System.Drawing.Point(107, 3);
            this.txtFaction.Name = "txtFaction";
            this.txtFaction.Size = new System.Drawing.Size(100, 20);
            this.txtFaction.TabIndex = 0;
            // 
            // cmbFaction
            // 
            this.cmbFaction.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbFaction.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbFaction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFaction.FormattingEnabled = true;
            this.cmbFaction.Location = new System.Drawing.Point(212, 2);
            this.cmbFaction.Margin = new System.Windows.Forms.Padding(2);
            this.cmbFaction.Name = "cmbFaction";
            this.cmbFaction.Size = new System.Drawing.Size(201, 21);
            this.cmbFaction.TabIndex = 1;
            // 
            // flpPublicRankBlock
            // 
            this.flpPublicRankBlock.Controls.Add(this.flpPublicRank);
            this.flpPublicRankBlock.Controls.Add(this.flpPublicRankCurXP);
            this.flpPublicRankBlock.Controls.Add(this.flpPublicRankNextXP);
            this.flpPublicRankBlock.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpPublicRankBlock.Location = new System.Drawing.Point(3, 93);
            this.flpPublicRankBlock.Name = "flpPublicRankBlock";
            this.flpPublicRankBlock.Size = new System.Drawing.Size(412, 85);
            this.flpPublicRankBlock.TabIndex = 8;
            this.flpPublicRankBlock.WrapContents = false;
            // 
            // flpPublicRank
            // 
            this.flpPublicRank.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpPublicRank.AutoSize = true;
            this.flpPublicRank.Controls.Add(this.lblPublicRank);
            this.flpPublicRank.Controls.Add(this.txtPublicRank);
            this.flpPublicRank.Controls.Add(this.lblPublicRankName);
            this.flpPublicRank.Location = new System.Drawing.Point(2, 2);
            this.flpPublicRank.Margin = new System.Windows.Forms.Padding(2);
            this.flpPublicRank.Name = "flpPublicRank";
            this.flpPublicRank.Size = new System.Drawing.Size(309, 24);
            this.flpPublicRank.TabIndex = 4;
            // 
            // lblPublicRank
            // 
            this.lblPublicRank.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblPublicRank.Location = new System.Drawing.Point(2, 3);
            this.lblPublicRank.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblPublicRank.Name = "lblPublicRank";
            this.lblPublicRank.Size = new System.Drawing.Size(100, 17);
            this.lblPublicRank.TabIndex = 2;
            this.lblPublicRank.Text = "Public Rank";
            this.lblPublicRank.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtPublicRank
            // 
            this.txtPublicRank.Location = new System.Drawing.Point(106, 2);
            this.txtPublicRank.Margin = new System.Windows.Forms.Padding(2);
            this.txtPublicRank.Name = "txtPublicRank";
            this.txtPublicRank.Size = new System.Drawing.Size(201, 20);
            this.txtPublicRank.TabIndex = 7;
            // 
            // lblPublicRankName
            // 
            this.lblPublicRankName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblPublicRankName.AutoSize = true;
            this.lblPublicRankName.Location = new System.Drawing.Point(311, 5);
            this.lblPublicRankName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblPublicRankName.Name = "lblPublicRankName";
            this.lblPublicRankName.Size = new System.Drawing.Size(0, 13);
            this.lblPublicRankName.TabIndex = 8;
            this.lblPublicRankName.Visible = false;
            // 
            // flpPublicRankCurXP
            // 
            this.flpPublicRankCurXP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpPublicRankCurXP.AutoSize = true;
            this.flpPublicRankCurXP.Controls.Add(this.lblPublicRankCurXP);
            this.flpPublicRankCurXP.Controls.Add(this.txtPublicRankCurXP);
            this.flpPublicRankCurXP.Location = new System.Drawing.Point(2, 30);
            this.flpPublicRankCurXP.Margin = new System.Windows.Forms.Padding(2);
            this.flpPublicRankCurXP.Name = "flpPublicRankCurXP";
            this.flpPublicRankCurXP.Size = new System.Drawing.Size(309, 24);
            this.flpPublicRankCurXP.TabIndex = 5;
            // 
            // lblPublicRankCurXP
            // 
            this.lblPublicRankCurXP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblPublicRankCurXP.Location = new System.Drawing.Point(2, 3);
            this.lblPublicRankCurXP.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblPublicRankCurXP.Name = "lblPublicRankCurXP";
            this.lblPublicRankCurXP.Size = new System.Drawing.Size(100, 17);
            this.lblPublicRankCurXP.TabIndex = 2;
            this.lblPublicRankCurXP.Text = "Cur XP";
            this.lblPublicRankCurXP.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtPublicRankCurXP
            // 
            this.txtPublicRankCurXP.Location = new System.Drawing.Point(106, 2);
            this.txtPublicRankCurXP.Margin = new System.Windows.Forms.Padding(2);
            this.txtPublicRankCurXP.Name = "txtPublicRankCurXP";
            this.txtPublicRankCurXP.Size = new System.Drawing.Size(201, 20);
            this.txtPublicRankCurXP.TabIndex = 7;
            // 
            // flpPublicRankNextXP
            // 
            this.flpPublicRankNextXP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpPublicRankNextXP.AutoSize = true;
            this.flpPublicRankNextXP.Controls.Add(this.lblPublicRankNextXP);
            this.flpPublicRankNextXP.Controls.Add(this.txtPublicRankNextXP);
            this.flpPublicRankNextXP.Location = new System.Drawing.Point(2, 58);
            this.flpPublicRankNextXP.Margin = new System.Windows.Forms.Padding(2);
            this.flpPublicRankNextXP.Name = "flpPublicRankNextXP";
            this.flpPublicRankNextXP.Size = new System.Drawing.Size(309, 24);
            this.flpPublicRankNextXP.TabIndex = 8;
            // 
            // lblPublicRankNextXP
            // 
            this.lblPublicRankNextXP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblPublicRankNextXP.Location = new System.Drawing.Point(2, 3);
            this.lblPublicRankNextXP.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblPublicRankNextXP.Name = "lblPublicRankNextXP";
            this.lblPublicRankNextXP.Size = new System.Drawing.Size(100, 17);
            this.lblPublicRankNextXP.TabIndex = 2;
            this.lblPublicRankNextXP.Text = "Next XP";
            this.lblPublicRankNextXP.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtPublicRankNextXP
            // 
            this.txtPublicRankNextXP.Location = new System.Drawing.Point(106, 2);
            this.txtPublicRankNextXP.Margin = new System.Windows.Forms.Padding(2);
            this.txtPublicRankNextXP.Name = "txtPublicRankNextXP";
            this.txtPublicRankNextXP.Size = new System.Drawing.Size(201, 20);
            this.txtPublicRankNextXP.TabIndex = 7;
            // 
            // flpPrivateRankBlock
            // 
            this.flpPrivateRankBlock.Controls.Add(this.flpPrivateRank);
            this.flpPrivateRankBlock.Controls.Add(this.flpPrivateRankCurXP);
            this.flpPrivateRankBlock.Controls.Add(this.flpPrivateRankNextXP);
            this.flpPrivateRankBlock.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpPrivateRankBlock.Location = new System.Drawing.Point(3, 184);
            this.flpPrivateRankBlock.Name = "flpPrivateRankBlock";
            this.flpPrivateRankBlock.Size = new System.Drawing.Size(412, 85);
            this.flpPrivateRankBlock.TabIndex = 9;
            this.flpPrivateRankBlock.WrapContents = false;
            // 
            // flpPrivateRank
            // 
            this.flpPrivateRank.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpPrivateRank.AutoSize = true;
            this.flpPrivateRank.Controls.Add(this.lblPrivateRank);
            this.flpPrivateRank.Controls.Add(this.txtPrivateRank);
            this.flpPrivateRank.Controls.Add(this.lblPrivateRankName);
            this.flpPrivateRank.Location = new System.Drawing.Point(2, 2);
            this.flpPrivateRank.Margin = new System.Windows.Forms.Padding(2);
            this.flpPrivateRank.Name = "flpPrivateRank";
            this.flpPrivateRank.Size = new System.Drawing.Size(309, 24);
            this.flpPrivateRank.TabIndex = 4;
            // 
            // lblPrivateRank
            // 
            this.lblPrivateRank.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblPrivateRank.Location = new System.Drawing.Point(2, 3);
            this.lblPrivateRank.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblPrivateRank.Name = "lblPrivateRank";
            this.lblPrivateRank.Size = new System.Drawing.Size(100, 17);
            this.lblPrivateRank.TabIndex = 2;
            this.lblPrivateRank.Text = "Private Rank";
            this.lblPrivateRank.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtPrivateRank
            // 
            this.txtPrivateRank.Location = new System.Drawing.Point(106, 2);
            this.txtPrivateRank.Margin = new System.Windows.Forms.Padding(2);
            this.txtPrivateRank.Name = "txtPrivateRank";
            this.txtPrivateRank.Size = new System.Drawing.Size(201, 20);
            this.txtPrivateRank.TabIndex = 7;
            // 
            // lblPrivateRankName
            // 
            this.lblPrivateRankName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblPrivateRankName.AutoSize = true;
            this.lblPrivateRankName.Location = new System.Drawing.Point(311, 5);
            this.lblPrivateRankName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblPrivateRankName.Name = "lblPrivateRankName";
            this.lblPrivateRankName.Size = new System.Drawing.Size(0, 13);
            this.lblPrivateRankName.TabIndex = 8;
            this.lblPrivateRankName.Visible = false;
            // 
            // flpPrivateRankCurXP
            // 
            this.flpPrivateRankCurXP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpPrivateRankCurXP.AutoSize = true;
            this.flpPrivateRankCurXP.Controls.Add(this.lblPrivateRankCurXP);
            this.flpPrivateRankCurXP.Controls.Add(this.txtPrivateRankCurXP);
            this.flpPrivateRankCurXP.Location = new System.Drawing.Point(2, 30);
            this.flpPrivateRankCurXP.Margin = new System.Windows.Forms.Padding(2);
            this.flpPrivateRankCurXP.Name = "flpPrivateRankCurXP";
            this.flpPrivateRankCurXP.Size = new System.Drawing.Size(309, 24);
            this.flpPrivateRankCurXP.TabIndex = 5;
            // 
            // lblPrivateRankCurXP
            // 
            this.lblPrivateRankCurXP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblPrivateRankCurXP.Location = new System.Drawing.Point(2, 3);
            this.lblPrivateRankCurXP.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblPrivateRankCurXP.Name = "lblPrivateRankCurXP";
            this.lblPrivateRankCurXP.Size = new System.Drawing.Size(100, 17);
            this.lblPrivateRankCurXP.TabIndex = 2;
            this.lblPrivateRankCurXP.Text = "Cur XP";
            this.lblPrivateRankCurXP.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtPrivateRankCurXP
            // 
            this.txtPrivateRankCurXP.Location = new System.Drawing.Point(106, 2);
            this.txtPrivateRankCurXP.Margin = new System.Windows.Forms.Padding(2);
            this.txtPrivateRankCurXP.Name = "txtPrivateRankCurXP";
            this.txtPrivateRankCurXP.Size = new System.Drawing.Size(201, 20);
            this.txtPrivateRankCurXP.TabIndex = 7;
            // 
            // flpPrivateRankNextXP
            // 
            this.flpPrivateRankNextXP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpPrivateRankNextXP.AutoSize = true;
            this.flpPrivateRankNextXP.Controls.Add(this.lblPrivateRankNextXP);
            this.flpPrivateRankNextXP.Controls.Add(this.txtPrivateRankNextXP);
            this.flpPrivateRankNextXP.Location = new System.Drawing.Point(2, 58);
            this.flpPrivateRankNextXP.Margin = new System.Windows.Forms.Padding(2);
            this.flpPrivateRankNextXP.Name = "flpPrivateRankNextXP";
            this.flpPrivateRankNextXP.Size = new System.Drawing.Size(309, 24);
            this.flpPrivateRankNextXP.TabIndex = 8;
            // 
            // lblPrivateRankNextXP
            // 
            this.lblPrivateRankNextXP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblPrivateRankNextXP.Location = new System.Drawing.Point(2, 3);
            this.lblPrivateRankNextXP.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblPrivateRankNextXP.Name = "lblPrivateRankNextXP";
            this.lblPrivateRankNextXP.Size = new System.Drawing.Size(100, 17);
            this.lblPrivateRankNextXP.TabIndex = 2;
            this.lblPrivateRankNextXP.Text = "Next XP";
            this.lblPrivateRankNextXP.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtPrivateRankNextXP
            // 
            this.txtPrivateRankNextXP.Location = new System.Drawing.Point(106, 2);
            this.txtPrivateRankNextXP.Margin = new System.Windows.Forms.Padding(2);
            this.txtPrivateRankNextXP.Name = "txtPrivateRankNextXP";
            this.txtPrivateRankNextXP.Size = new System.Drawing.Size(201, 20);
            this.txtPrivateRankNextXP.TabIndex = 7;
            // 
            // flpMilitaryRankBlock
            // 
            this.flpMilitaryRankBlock.Controls.Add(this.flpMilitaryRank);
            this.flpMilitaryRankBlock.Controls.Add(this.flpMilitaryRankCurXP);
            this.flpMilitaryRankBlock.Controls.Add(this.flpMilitaryRankNextXP);
            this.flpMilitaryRankBlock.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpMilitaryRankBlock.Location = new System.Drawing.Point(3, 275);
            this.flpMilitaryRankBlock.Name = "flpMilitaryRankBlock";
            this.flpMilitaryRankBlock.Size = new System.Drawing.Size(412, 85);
            this.flpMilitaryRankBlock.TabIndex = 10;
            this.flpMilitaryRankBlock.WrapContents = false;
            // 
            // flpMilitaryRank
            // 
            this.flpMilitaryRank.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpMilitaryRank.AutoSize = true;
            this.flpMilitaryRank.Controls.Add(this.lblMilitaryRank);
            this.flpMilitaryRank.Controls.Add(this.txtMilitaryRank);
            this.flpMilitaryRank.Controls.Add(this.lblMilitaryRankName);
            this.flpMilitaryRank.Location = new System.Drawing.Point(2, 2);
            this.flpMilitaryRank.Margin = new System.Windows.Forms.Padding(2);
            this.flpMilitaryRank.Name = "flpMilitaryRank";
            this.flpMilitaryRank.Size = new System.Drawing.Size(309, 24);
            this.flpMilitaryRank.TabIndex = 4;
            // 
            // lblMilitaryRank
            // 
            this.lblMilitaryRank.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblMilitaryRank.Location = new System.Drawing.Point(2, 3);
            this.lblMilitaryRank.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblMilitaryRank.Name = "lblMilitaryRank";
            this.lblMilitaryRank.Size = new System.Drawing.Size(100, 17);
            this.lblMilitaryRank.TabIndex = 2;
            this.lblMilitaryRank.Text = "Military Rank";
            this.lblMilitaryRank.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtMilitaryRank
            // 
            this.txtMilitaryRank.Location = new System.Drawing.Point(106, 2);
            this.txtMilitaryRank.Margin = new System.Windows.Forms.Padding(2);
            this.txtMilitaryRank.Name = "txtMilitaryRank";
            this.txtMilitaryRank.Size = new System.Drawing.Size(201, 20);
            this.txtMilitaryRank.TabIndex = 7;
            // 
            // lblMilitaryRankName
            // 
            this.lblMilitaryRankName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblMilitaryRankName.AutoSize = true;
            this.lblMilitaryRankName.Location = new System.Drawing.Point(311, 5);
            this.lblMilitaryRankName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblMilitaryRankName.Name = "lblMilitaryRankName";
            this.lblMilitaryRankName.Size = new System.Drawing.Size(0, 13);
            this.lblMilitaryRankName.TabIndex = 8;
            this.lblMilitaryRankName.Visible = false;
            // 
            // flpMilitaryRankCurXP
            // 
            this.flpMilitaryRankCurXP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpMilitaryRankCurXP.AutoSize = true;
            this.flpMilitaryRankCurXP.Controls.Add(this.lblMilitaryRankCurXP);
            this.flpMilitaryRankCurXP.Controls.Add(this.txtMilitaryRankCurXP);
            this.flpMilitaryRankCurXP.Location = new System.Drawing.Point(2, 30);
            this.flpMilitaryRankCurXP.Margin = new System.Windows.Forms.Padding(2);
            this.flpMilitaryRankCurXP.Name = "flpMilitaryRankCurXP";
            this.flpMilitaryRankCurXP.Size = new System.Drawing.Size(309, 24);
            this.flpMilitaryRankCurXP.TabIndex = 5;
            // 
            // lblMilitaryRankCurXP
            // 
            this.lblMilitaryRankCurXP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblMilitaryRankCurXP.Location = new System.Drawing.Point(2, 3);
            this.lblMilitaryRankCurXP.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblMilitaryRankCurXP.Name = "lblMilitaryRankCurXP";
            this.lblMilitaryRankCurXP.Size = new System.Drawing.Size(100, 17);
            this.lblMilitaryRankCurXP.TabIndex = 2;
            this.lblMilitaryRankCurXP.Text = "Cur XP";
            this.lblMilitaryRankCurXP.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtMilitaryRankCurXP
            // 
            this.txtMilitaryRankCurXP.Location = new System.Drawing.Point(106, 2);
            this.txtMilitaryRankCurXP.Margin = new System.Windows.Forms.Padding(2);
            this.txtMilitaryRankCurXP.Name = "txtMilitaryRankCurXP";
            this.txtMilitaryRankCurXP.Size = new System.Drawing.Size(201, 20);
            this.txtMilitaryRankCurXP.TabIndex = 7;
            // 
            // flpMilitaryRankNextXP
            // 
            this.flpMilitaryRankNextXP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpMilitaryRankNextXP.AutoSize = true;
            this.flpMilitaryRankNextXP.Controls.Add(this.lblMilitaryRankNextXP);
            this.flpMilitaryRankNextXP.Controls.Add(this.txtMilitaryRankNextXP);
            this.flpMilitaryRankNextXP.Location = new System.Drawing.Point(2, 58);
            this.flpMilitaryRankNextXP.Margin = new System.Windows.Forms.Padding(2);
            this.flpMilitaryRankNextXP.Name = "flpMilitaryRankNextXP";
            this.flpMilitaryRankNextXP.Size = new System.Drawing.Size(309, 24);
            this.flpMilitaryRankNextXP.TabIndex = 8;
            // 
            // lblMilitaryRankNextXP
            // 
            this.lblMilitaryRankNextXP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblMilitaryRankNextXP.Location = new System.Drawing.Point(2, 3);
            this.lblMilitaryRankNextXP.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblMilitaryRankNextXP.Name = "lblMilitaryRankNextXP";
            this.lblMilitaryRankNextXP.Size = new System.Drawing.Size(100, 17);
            this.lblMilitaryRankNextXP.TabIndex = 2;
            this.lblMilitaryRankNextXP.Text = "Next XP";
            this.lblMilitaryRankNextXP.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtMilitaryRankNextXP
            // 
            this.txtMilitaryRankNextXP.Location = new System.Drawing.Point(106, 2);
            this.txtMilitaryRankNextXP.Margin = new System.Windows.Forms.Padding(2);
            this.txtMilitaryRankNextXP.Name = "txtMilitaryRankNextXP";
            this.txtMilitaryRankNextXP.Size = new System.Drawing.Size(201, 20);
            this.txtMilitaryRankNextXP.TabIndex = 7;
            // 
            // flpSkillPoints
            // 
            this.flpSkillPoints.AutoSize = true;
            this.flpSkillPoints.Controls.Add(this.lblSkillPoints);
            this.flpSkillPoints.Controls.Add(this.txtSkillPoints);
            this.flpSkillPoints.Location = new System.Drawing.Point(2, 365);
            this.flpSkillPoints.Margin = new System.Windows.Forms.Padding(2);
            this.flpSkillPoints.Name = "flpSkillPoints";
            this.flpSkillPoints.Size = new System.Drawing.Size(310, 26);
            this.flpSkillPoints.TabIndex = 3;
            // 
            // lblSkillPoints
            // 
            this.lblSkillPoints.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSkillPoints.Location = new System.Drawing.Point(2, 4);
            this.lblSkillPoints.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblSkillPoints.Name = "lblSkillPoints";
            this.lblSkillPoints.Size = new System.Drawing.Size(100, 17);
            this.lblSkillPoints.TabIndex = 2;
            this.lblSkillPoints.Text = "Skill Points";
            this.lblSkillPoints.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtSkillPoints
            // 
            this.txtSkillPoints.Location = new System.Drawing.Point(107, 3);
            this.txtSkillPoints.Name = "txtSkillPoints";
            this.txtSkillPoints.Size = new System.Drawing.Size(200, 20);
            this.txtSkillPoints.TabIndex = 0;
            // 
            // flpColonyDirectorSkillGroup
            // 
            this.flpColonyDirectorSkillGroup.AutoSize = true;
            this.flpColonyDirectorSkillGroup.Controls.Add(this.flpColonyDirector);
            this.flpColonyDirectorSkillGroup.Controls.Add(this.pskHumanResources);
            this.flpColonyDirectorSkillGroup.Controls.Add(this.pskForeman);
            this.flpColonyDirectorSkillGroup.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpColonyDirectorSkillGroup.Location = new System.Drawing.Point(3, 396);
            this.flpColonyDirectorSkillGroup.Name = "flpColonyDirectorSkillGroup";
            this.flpColonyDirectorSkillGroup.Size = new System.Drawing.Size(430, 87);
            this.flpColonyDirectorSkillGroup.TabIndex = 11;
            this.flpColonyDirectorSkillGroup.WrapContents = false;
            // 
            // flpColonyDirector
            // 
            this.flpColonyDirector.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpColonyDirector.AutoSize = true;
            this.flpColonyDirector.Controls.Add(this.lblColonyDirector);
            this.flpColonyDirector.Controls.Add(this.chkColonyDirector);
            this.flpColonyDirector.Location = new System.Drawing.Point(2, 2);
            this.flpColonyDirector.Margin = new System.Windows.Forms.Padding(2);
            this.flpColonyDirector.Name = "flpColonyDirector";
            this.flpColonyDirector.Size = new System.Drawing.Size(426, 23);
            this.flpColonyDirector.TabIndex = 4;
            // 
            // lblColonyDirector
            // 
            this.lblColonyDirector.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblColonyDirector.Location = new System.Drawing.Point(2, 3);
            this.lblColonyDirector.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblColonyDirector.Name = "lblColonyDirector";
            this.lblColonyDirector.Size = new System.Drawing.Size(100, 17);
            this.lblColonyDirector.TabIndex = 2;
            this.lblColonyDirector.Text = "Colony Director";
            this.lblColonyDirector.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // chkColonyDirector
            // 
            this.chkColonyDirector.AutoSize = true;
            this.chkColonyDirector.Location = new System.Drawing.Point(107, 3);
            this.chkColonyDirector.Name = "chkColonyDirector";
            this.chkColonyDirector.Size = new System.Drawing.Size(72, 17);
            this.chkColonyDirector.TabIndex = 3;
            this.chkColonyDirector.Text = "Unlocked";
            this.chkColonyDirector.UseVisualStyleBackColor = true;
            this.chkColonyDirector.Click += new System.EventHandler(this.ChkColonyDirector_Click);
            // 
            // pskHumanResources
            // 
            this.pskHumanResources.AutoSize = true;
            this.pskHumanResources.CanStartTraining = false;
            this.pskHumanResources.Location = new System.Drawing.Point(3, 30);
            this.pskHumanResources.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskHumanResources.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskHumanResources.Name = "pskHumanResources";
            this.pskHumanResources.SkillData = null;
            this.pskHumanResources.Size = new System.Drawing.Size(424, 24);
            this.pskHumanResources.SkillGroupCheckbox = null;
            this.pskHumanResources.SkillName = "Human Resources";
            this.pskHumanResources.TabIndex = 9;
            // 
            // pskForeman
            // 
            this.pskForeman.AutoSize = true;
            this.pskForeman.CanStartTraining = false;
            this.pskForeman.Location = new System.Drawing.Point(3, 60);
            this.pskForeman.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskForeman.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskForeman.Name = "pskForeman";
            this.pskForeman.SkillData = null;
            this.pskForeman.Size = new System.Drawing.Size(424, 24);
            this.pskForeman.SkillGroupCheckbox = null;
            this.pskForeman.SkillName = "Foreman";
            this.pskForeman.TabIndex = 10;
            // 
            // flpColonyFounderSkillGroup
            // 
            this.flpColonyFounderSkillGroup.AutoSize = true;
            this.flpColonyFounderSkillGroup.Controls.Add(this.flpColonyFounder);
            this.flpColonyFounderSkillGroup.Controls.Add(this.pskFounder);
            this.flpColonyFounderSkillGroup.Controls.Add(this.pskEnergyEfficiency);
            this.flpColonyFounderSkillGroup.Controls.Add(this.pskBuilder);
            this.flpColonyFounderSkillGroup.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpColonyFounderSkillGroup.Location = new System.Drawing.Point(3, 489);
            this.flpColonyFounderSkillGroup.Name = "flpColonyFounderSkillGroup";
            this.flpColonyFounderSkillGroup.Size = new System.Drawing.Size(430, 117);
            this.flpColonyFounderSkillGroup.TabIndex = 12;
            this.flpColonyFounderSkillGroup.WrapContents = false;
            // 
            // flpColonyFounder
            // 
            this.flpColonyFounder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpColonyFounder.AutoSize = true;
            this.flpColonyFounder.Controls.Add(this.lblColonyFounder);
            this.flpColonyFounder.Controls.Add(this.chkColonyFounder);
            this.flpColonyFounder.Location = new System.Drawing.Point(2, 2);
            this.flpColonyFounder.Margin = new System.Windows.Forms.Padding(2);
            this.flpColonyFounder.Name = "flpColonyFounder";
            this.flpColonyFounder.Size = new System.Drawing.Size(426, 23);
            this.flpColonyFounder.TabIndex = 4;
            // 
            // lblColonyFounder
            // 
            this.lblColonyFounder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblColonyFounder.Location = new System.Drawing.Point(2, 3);
            this.lblColonyFounder.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblColonyFounder.Name = "lblColonyFounder";
            this.lblColonyFounder.Size = new System.Drawing.Size(100, 17);
            this.lblColonyFounder.TabIndex = 2;
            this.lblColonyFounder.Text = "Colony Founder";
            this.lblColonyFounder.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // chkColonyFounder
            // 
            this.chkColonyFounder.AutoSize = true;
            this.chkColonyFounder.Location = new System.Drawing.Point(107, 3);
            this.chkColonyFounder.Name = "chkColonyFounder";
            this.chkColonyFounder.Size = new System.Drawing.Size(72, 17);
            this.chkColonyFounder.TabIndex = 3;
            this.chkColonyFounder.Text = "Unlocked";
            this.chkColonyFounder.UseVisualStyleBackColor = true;
            this.chkColonyFounder.Click += new System.EventHandler(this.ChkColonyFounder_Click);
            // 
            // pskFounder
            // 
            this.pskFounder.AutoSize = true;
            this.pskFounder.CanStartTraining = false;
            this.pskFounder.Location = new System.Drawing.Point(3, 30);
            this.pskFounder.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskFounder.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskFounder.Name = "pskFounder";
            this.pskFounder.SkillData = null;
            this.pskFounder.Size = new System.Drawing.Size(424, 24);
            this.pskFounder.SkillGroupCheckbox = null;
            this.pskFounder.SkillName = "Founder";
            this.pskFounder.TabIndex = 13;
            // 
            // pskEnergyEfficiency
            // 
            this.pskEnergyEfficiency.AutoSize = true;
            this.pskEnergyEfficiency.CanStartTraining = false;
            this.pskEnergyEfficiency.Location = new System.Drawing.Point(3, 60);
            this.pskEnergyEfficiency.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskEnergyEfficiency.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskEnergyEfficiency.Name = "pskEnergyEfficiency";
            this.pskEnergyEfficiency.SkillData = null;
            this.pskEnergyEfficiency.Size = new System.Drawing.Size(424, 24);
            this.pskEnergyEfficiency.SkillGroupCheckbox = null;
            this.pskEnergyEfficiency.SkillName = "Energy Efficiency";
            this.pskEnergyEfficiency.TabIndex = 12;
            // 
            // pskBuilder
            // 
            this.pskBuilder.AutoSize = true;
            this.pskBuilder.CanStartTraining = false;
            this.pskBuilder.Location = new System.Drawing.Point(3, 90);
            this.pskBuilder.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskBuilder.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskBuilder.Name = "pskBuilder";
            this.pskBuilder.SkillData = null;
            this.pskBuilder.Size = new System.Drawing.Size(424, 24);
            this.pskBuilder.SkillGroupCheckbox = null;
            this.pskBuilder.SkillName = "Builder";
            this.pskBuilder.TabIndex = 14;
            // 
            // flpColonyOperationsSkillGroup
            // 
            this.flpColonyOperationsSkillGroup.AutoSize = true;
            this.flpColonyOperationsSkillGroup.Controls.Add(this.flpColonyOperations);
            this.flpColonyOperationsSkillGroup.Controls.Add(this.pskRefiningFocus);
            this.flpColonyOperationsSkillGroup.Controls.Add(this.pskProductionFocus);
            this.flpColonyOperationsSkillGroup.Controls.Add(this.pskExtractionFocus);
            this.flpColonyOperationsSkillGroup.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpColonyOperationsSkillGroup.Location = new System.Drawing.Point(3, 612);
            this.flpColonyOperationsSkillGroup.Name = "flpColonyOperationsSkillGroup";
            this.flpColonyOperationsSkillGroup.Size = new System.Drawing.Size(430, 117);
            this.flpColonyOperationsSkillGroup.TabIndex = 15;
            this.flpColonyOperationsSkillGroup.WrapContents = false;
            // 
            // flpColonyOperations
            // 
            this.flpColonyOperations.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpColonyOperations.AutoSize = true;
            this.flpColonyOperations.Controls.Add(this.lblColonyOperations);
            this.flpColonyOperations.Controls.Add(this.chkColonyOperations);
            this.flpColonyOperations.Location = new System.Drawing.Point(2, 2);
            this.flpColonyOperations.Margin = new System.Windows.Forms.Padding(2);
            this.flpColonyOperations.Name = "flpColonyOperations";
            this.flpColonyOperations.Size = new System.Drawing.Size(426, 23);
            this.flpColonyOperations.TabIndex = 4;
            // 
            // lblColonyOperations
            // 
            this.lblColonyOperations.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblColonyOperations.Location = new System.Drawing.Point(2, 3);
            this.lblColonyOperations.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblColonyOperations.Name = "lblColonyOperations";
            this.lblColonyOperations.Size = new System.Drawing.Size(100, 17);
            this.lblColonyOperations.TabIndex = 2;
            this.lblColonyOperations.Text = "Colony Operations";
            this.lblColonyOperations.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // chkColonyOperations
            // 
            this.chkColonyOperations.AutoSize = true;
            this.chkColonyOperations.Location = new System.Drawing.Point(107, 3);
            this.chkColonyOperations.Name = "chkColonyOperations";
            this.chkColonyOperations.Size = new System.Drawing.Size(72, 17);
            this.chkColonyOperations.TabIndex = 3;
            this.chkColonyOperations.Text = "Unlocked";
            this.chkColonyOperations.UseVisualStyleBackColor = true;
            // 
            // pskRefiningFocus
            // 
            this.pskRefiningFocus.AutoSize = true;
            this.pskRefiningFocus.CanStartTraining = false;
            this.pskRefiningFocus.Location = new System.Drawing.Point(3, 30);
            this.pskRefiningFocus.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskRefiningFocus.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskRefiningFocus.Name = "pskRefiningFocus";
            this.pskRefiningFocus.SkillData = null;
            this.pskRefiningFocus.Size = new System.Drawing.Size(424, 24);
            this.pskRefiningFocus.SkillGroupCheckbox = null;
            this.pskRefiningFocus.SkillName = "Refining Focus";
            this.pskRefiningFocus.TabIndex = 13;
            // 
            // pskProductionFocus
            // 
            this.pskProductionFocus.AutoSize = true;
            this.pskProductionFocus.CanStartTraining = false;
            this.pskProductionFocus.Location = new System.Drawing.Point(3, 60);
            this.pskProductionFocus.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskProductionFocus.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskProductionFocus.Name = "pskProductionFocus";
            this.pskProductionFocus.SkillData = null;
            this.pskProductionFocus.Size = new System.Drawing.Size(424, 24);
            this.pskProductionFocus.SkillGroupCheckbox = null;
            this.pskProductionFocus.SkillName = "Production Focus";
            this.pskProductionFocus.TabIndex = 12;
            // 
            // pskExtractionFocus
            // 
            this.pskExtractionFocus.AutoSize = true;
            this.pskExtractionFocus.CanStartTraining = false;
            this.pskExtractionFocus.Location = new System.Drawing.Point(3, 90);
            this.pskExtractionFocus.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskExtractionFocus.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskExtractionFocus.Name = "pskExtractionFocus";
            this.pskExtractionFocus.SkillData = null;
            this.pskExtractionFocus.Size = new System.Drawing.Size(424, 24);
            this.pskExtractionFocus.SkillGroupCheckbox = null;
            this.pskExtractionFocus.SkillName = "Extraction Focus";
            this.pskExtractionFocus.TabIndex = 14;
            // 
            // flpCommanderSkillGroup
            // 
            this.flpCommanderSkillGroup.AutoSize = true;
            this.flpCommanderSkillGroup.Controls.Add(this.flpCommander);
            this.flpCommanderSkillGroup.Controls.Add(this.pskDamageControl);
            this.flpCommanderSkillGroup.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpCommanderSkillGroup.Location = new System.Drawing.Point(3, 735);
            this.flpCommanderSkillGroup.Name = "flpCommanderSkillGroup";
            this.flpCommanderSkillGroup.Size = new System.Drawing.Size(430, 57);
            this.flpCommanderSkillGroup.TabIndex = 16;
            this.flpCommanderSkillGroup.WrapContents = false;
            // 
            // flpCommander
            // 
            this.flpCommander.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpCommander.AutoSize = true;
            this.flpCommander.Controls.Add(this.lblCommander);
            this.flpCommander.Controls.Add(this.chkCommander);
            this.flpCommander.Location = new System.Drawing.Point(2, 2);
            this.flpCommander.Margin = new System.Windows.Forms.Padding(2);
            this.flpCommander.Name = "flpCommander";
            this.flpCommander.Size = new System.Drawing.Size(426, 23);
            this.flpCommander.TabIndex = 4;
            // 
            // lblCommander
            // 
            this.lblCommander.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCommander.Location = new System.Drawing.Point(2, 3);
            this.lblCommander.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblCommander.Name = "lblCommander";
            this.lblCommander.Size = new System.Drawing.Size(100, 17);
            this.lblCommander.TabIndex = 2;
            this.lblCommander.Text = "Commander";
            this.lblCommander.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // chkCommander
            // 
            this.chkCommander.AutoSize = true;
            this.chkCommander.Location = new System.Drawing.Point(107, 3);
            this.chkCommander.Name = "chkCommander";
            this.chkCommander.Size = new System.Drawing.Size(72, 17);
            this.chkCommander.TabIndex = 3;
            this.chkCommander.Text = "Unlocked";
            this.chkCommander.UseVisualStyleBackColor = true;
            this.chkCommander.Click += new System.EventHandler(this.ChkCommander_Click);
            // 
            // pskDamageControl
            // 
            this.pskDamageControl.AutoSize = true;
            this.pskDamageControl.CanStartTraining = false;
            this.pskDamageControl.Location = new System.Drawing.Point(3, 30);
            this.pskDamageControl.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskDamageControl.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskDamageControl.Name = "pskDamageControl";
            this.pskDamageControl.SkillData = null;
            this.pskDamageControl.Size = new System.Drawing.Size(424, 24);
            this.pskDamageControl.SkillGroupCheckbox = null;
            this.pskDamageControl.SkillName = "Damage Control";
            this.pskDamageControl.TabIndex = 13;
            // 
            // flpEngineerSkillGroup
            // 
            this.flpEngineerSkillGroup.AutoSize = true;
            this.flpEngineerSkillGroup.Controls.Add(this.flpEngineer);
            this.flpEngineerSkillGroup.Controls.Add(this.pskEngineeringCapacity);
            this.flpEngineerSkillGroup.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpEngineerSkillGroup.Location = new System.Drawing.Point(3, 798);
            this.flpEngineerSkillGroup.Name = "flpEngineerSkillGroup";
            this.flpEngineerSkillGroup.Size = new System.Drawing.Size(430, 57);
            this.flpEngineerSkillGroup.TabIndex = 17;
            this.flpEngineerSkillGroup.WrapContents = false;
            // 
            // flpEngineer
            // 
            this.flpEngineer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpEngineer.AutoSize = true;
            this.flpEngineer.Controls.Add(this.lblEngineer);
            this.flpEngineer.Controls.Add(this.chkEngineer);
            this.flpEngineer.Location = new System.Drawing.Point(2, 2);
            this.flpEngineer.Margin = new System.Windows.Forms.Padding(2);
            this.flpEngineer.Name = "flpEngineer";
            this.flpEngineer.Size = new System.Drawing.Size(426, 23);
            this.flpEngineer.TabIndex = 4;
            // 
            // lblEngineer
            // 
            this.lblEngineer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblEngineer.Location = new System.Drawing.Point(2, 3);
            this.lblEngineer.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblEngineer.Name = "lblEngineer";
            this.lblEngineer.Size = new System.Drawing.Size(100, 17);
            this.lblEngineer.TabIndex = 2;
            this.lblEngineer.Text = "Engineer";
            this.lblEngineer.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // chkEngineer
            // 
            this.chkEngineer.AutoSize = true;
            this.chkEngineer.Location = new System.Drawing.Point(107, 3);
            this.chkEngineer.Name = "chkEngineer";
            this.chkEngineer.Size = new System.Drawing.Size(72, 17);
            this.chkEngineer.TabIndex = 3;
            this.chkEngineer.Text = "Unlocked";
            this.chkEngineer.UseVisualStyleBackColor = true;
            this.chkEngineer.Click += new System.EventHandler(this.ChkEngineer_Click);
            // 
            // pskEngineeringCapacity
            // 
            this.pskEngineeringCapacity.AutoSize = true;
            this.pskEngineeringCapacity.CanStartTraining = false;
            this.pskEngineeringCapacity.Location = new System.Drawing.Point(3, 30);
            this.pskEngineeringCapacity.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskEngineeringCapacity.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskEngineeringCapacity.Name = "pskEngineeringCapacity";
            this.pskEngineeringCapacity.SkillData = null;
            this.pskEngineeringCapacity.Size = new System.Drawing.Size(424, 24);
            this.pskEngineeringCapacity.SkillGroupCheckbox = null;
            this.pskEngineeringCapacity.SkillName = "Engineering Capacity";
            this.pskEngineeringCapacity.TabIndex = 13;
            // 
            // flpEntrepeneurSkillGroup
            // 
            this.flpEntrepeneurSkillGroup.AutoSize = true;
            this.flpEntrepeneurSkillGroup.Controls.Add(this.flpEntrepeneur);
            this.flpEntrepeneurSkillGroup.Controls.Add(this.pskSoundAsAPound);
            this.flpEntrepeneurSkillGroup.Controls.Add(this.pskSelfMadeMillionaire);
            this.flpEntrepeneurSkillGroup.Controls.Add(this.pskAAAHealthcare);
            this.flpEntrepeneurSkillGroup.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpEntrepeneurSkillGroup.Location = new System.Drawing.Point(3, 861);
            this.flpEntrepeneurSkillGroup.Name = "flpEntrepeneurSkillGroup";
            this.flpEntrepeneurSkillGroup.Size = new System.Drawing.Size(430, 117);
            this.flpEntrepeneurSkillGroup.TabIndex = 16;
            this.flpEntrepeneurSkillGroup.WrapContents = false;
            // 
            // flpEntrepeneur
            // 
            this.flpEntrepeneur.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpEntrepeneur.AutoSize = true;
            this.flpEntrepeneur.Controls.Add(this.lblEntrepeneur);
            this.flpEntrepeneur.Controls.Add(this.chkEntrepeneur);
            this.flpEntrepeneur.Location = new System.Drawing.Point(2, 2);
            this.flpEntrepeneur.Margin = new System.Windows.Forms.Padding(2);
            this.flpEntrepeneur.Name = "flpEntrepeneur";
            this.flpEntrepeneur.Size = new System.Drawing.Size(426, 23);
            this.flpEntrepeneur.TabIndex = 4;
            // 
            // lblEntrepeneur
            // 
            this.lblEntrepeneur.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblEntrepeneur.Location = new System.Drawing.Point(2, 3);
            this.lblEntrepeneur.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblEntrepeneur.Name = "lblEntrepeneur";
            this.lblEntrepeneur.Size = new System.Drawing.Size(100, 17);
            this.lblEntrepeneur.TabIndex = 2;
            this.lblEntrepeneur.Text = "Entrepeneur";
            this.lblEntrepeneur.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // chkEntrepeneur
            // 
            this.chkEntrepeneur.AutoSize = true;
            this.chkEntrepeneur.Location = new System.Drawing.Point(107, 3);
            this.chkEntrepeneur.Name = "chkEntrepeneur";
            this.chkEntrepeneur.Size = new System.Drawing.Size(72, 17);
            this.chkEntrepeneur.TabIndex = 3;
            this.chkEntrepeneur.Text = "Unlocked";
            this.chkEntrepeneur.UseVisualStyleBackColor = true;
            this.chkEntrepeneur.Click += new System.EventHandler(this.ChkEntrepeneur_Click);
            // 
            // pskSoundAsAPound
            // 
            this.pskSoundAsAPound.AutoSize = true;
            this.pskSoundAsAPound.CanStartTraining = false;
            this.pskSoundAsAPound.Location = new System.Drawing.Point(3, 30);
            this.pskSoundAsAPound.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskSoundAsAPound.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskSoundAsAPound.Name = "pskSoundAsAPound";
            this.pskSoundAsAPound.SkillData = null;
            this.pskSoundAsAPound.Size = new System.Drawing.Size(424, 24);
            this.pskSoundAsAPound.SkillGroupCheckbox = null;
            this.pskSoundAsAPound.SkillName = "Sounds As A Pound";
            this.pskSoundAsAPound.TabIndex = 13;
            // 
            // pskSelfMadeMillionaire
            // 
            this.pskSelfMadeMillionaire.AutoSize = true;
            this.pskSelfMadeMillionaire.CanStartTraining = false;
            this.pskSelfMadeMillionaire.Location = new System.Drawing.Point(3, 60);
            this.pskSelfMadeMillionaire.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskSelfMadeMillionaire.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskSelfMadeMillionaire.Name = "pskSelfMadeMillionaire";
            this.pskSelfMadeMillionaire.SkillData = null;
            this.pskSelfMadeMillionaire.Size = new System.Drawing.Size(424, 24);
            this.pskSelfMadeMillionaire.SkillGroupCheckbox = null;
            this.pskSelfMadeMillionaire.SkillName = "Self-made Millionaire";
            this.pskSelfMadeMillionaire.TabIndex = 12;
            // 
            // pskAAAHealthcare
            // 
            this.pskAAAHealthcare.AutoSize = true;
            this.pskAAAHealthcare.CanStartTraining = false;
            this.pskAAAHealthcare.Location = new System.Drawing.Point(3, 90);
            this.pskAAAHealthcare.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskAAAHealthcare.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskAAAHealthcare.Name = "pskAAAHealthcare";
            this.pskAAAHealthcare.SkillData = null;
            this.pskAAAHealthcare.Size = new System.Drawing.Size(424, 24);
            this.pskAAAHealthcare.SkillGroupCheckbox = null;
            this.pskAAAHealthcare.SkillName = "AAA Healthcare";
            this.pskAAAHealthcare.TabIndex = 14;
            // 
            // flpJobManagementSkillGroup
            // 
            this.flpJobManagementSkillGroup.AutoSize = true;
            this.flpJobManagementSkillGroup.Controls.Add(this.flpJobManagement);
            this.flpJobManagementSkillGroup.Controls.Add(this.pskJobOpportunities);
            this.flpJobManagementSkillGroup.Controls.Add(this.pskContractManagement);
            this.flpJobManagementSkillGroup.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpJobManagementSkillGroup.Location = new System.Drawing.Point(3, 984);
            this.flpJobManagementSkillGroup.Name = "flpJobManagementSkillGroup";
            this.flpJobManagementSkillGroup.Size = new System.Drawing.Size(430, 87);
            this.flpJobManagementSkillGroup.TabIndex = 16;
            this.flpJobManagementSkillGroup.WrapContents = false;
            // 
            // flpJobManagement
            // 
            this.flpJobManagement.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpJobManagement.AutoSize = true;
            this.flpJobManagement.Controls.Add(this.lblJobManagement);
            this.flpJobManagement.Controls.Add(this.chkJobManagement);
            this.flpJobManagement.Location = new System.Drawing.Point(2, 2);
            this.flpJobManagement.Margin = new System.Windows.Forms.Padding(2);
            this.flpJobManagement.Name = "flpJobManagement";
            this.flpJobManagement.Size = new System.Drawing.Size(426, 23);
            this.flpJobManagement.TabIndex = 4;
            // 
            // lblJobManagement
            // 
            this.lblJobManagement.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblJobManagement.Location = new System.Drawing.Point(2, 3);
            this.lblJobManagement.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblJobManagement.Name = "lblJobManagement";
            this.lblJobManagement.Size = new System.Drawing.Size(100, 17);
            this.lblJobManagement.TabIndex = 2;
            this.lblJobManagement.Text = "Job Management";
            this.lblJobManagement.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // chkJobManagement
            // 
            this.chkJobManagement.AutoSize = true;
            this.chkJobManagement.Location = new System.Drawing.Point(107, 3);
            this.chkJobManagement.Name = "chkJobManagement";
            this.chkJobManagement.Size = new System.Drawing.Size(72, 17);
            this.chkJobManagement.TabIndex = 3;
            this.chkJobManagement.Text = "Unlocked";
            this.chkJobManagement.UseVisualStyleBackColor = true;
            this.chkJobManagement.Click += new System.EventHandler(this.ChkJobManagement_Click);
            // 
            // pskJobOpportunities
            // 
            this.pskJobOpportunities.AutoSize = true;
            this.pskJobOpportunities.CanStartTraining = false;
            this.pskJobOpportunities.Location = new System.Drawing.Point(3, 30);
            this.pskJobOpportunities.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskJobOpportunities.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskJobOpportunities.Name = "pskJobOpportunities";
            this.pskJobOpportunities.SkillData = null;
            this.pskJobOpportunities.Size = new System.Drawing.Size(424, 24);
            this.pskJobOpportunities.SkillGroupCheckbox = null;
            this.pskJobOpportunities.SkillName = "Job Opportunities";
            this.pskJobOpportunities.TabIndex = 13;
            // 
            // pskContractManagement
            // 
            this.pskContractManagement.AutoSize = true;
            this.pskContractManagement.CanStartTraining = false;
            this.pskContractManagement.Location = new System.Drawing.Point(3, 60);
            this.pskContractManagement.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskContractManagement.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskContractManagement.Name = "pskContractManagement";
            this.pskContractManagement.SkillData = null;
            this.pskContractManagement.Size = new System.Drawing.Size(424, 24);
            this.pskContractManagement.SkillGroupCheckbox = null;
            this.pskContractManagement.SkillName = "Contract Management";
            this.pskContractManagement.TabIndex = 12;
            // 
            // flpResearcherSkillGroup
            // 
            this.flpResearcherSkillGroup.AutoSize = true;
            this.flpResearcherSkillGroup.Controls.Add(this.flpResearcher);
            this.flpResearcherSkillGroup.Controls.Add(this.pskResearchReview);
            this.flpResearcherSkillGroup.Controls.Add(this.pskResearchMethods);
            this.flpResearcherSkillGroup.Controls.Add(this.pskResearchFocus);
            this.flpResearcherSkillGroup.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpResearcherSkillGroup.Location = new System.Drawing.Point(3, 1077);
            this.flpResearcherSkillGroup.Name = "flpResearcherSkillGroup";
            this.flpResearcherSkillGroup.Size = new System.Drawing.Size(430, 117);
            this.flpResearcherSkillGroup.TabIndex = 16;
            this.flpResearcherSkillGroup.WrapContents = false;
            // 
            // flpResearcher
            // 
            this.flpResearcher.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpResearcher.AutoSize = true;
            this.flpResearcher.Controls.Add(this.lblResearcher);
            this.flpResearcher.Controls.Add(this.chkResearcher);
            this.flpResearcher.Location = new System.Drawing.Point(2, 2);
            this.flpResearcher.Margin = new System.Windows.Forms.Padding(2);
            this.flpResearcher.Name = "flpResearcher";
            this.flpResearcher.Size = new System.Drawing.Size(426, 23);
            this.flpResearcher.TabIndex = 4;
            // 
            // lblResearcher
            // 
            this.lblResearcher.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblResearcher.Location = new System.Drawing.Point(2, 3);
            this.lblResearcher.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblResearcher.Name = "lblResearcher";
            this.lblResearcher.Size = new System.Drawing.Size(100, 17);
            this.lblResearcher.TabIndex = 2;
            this.lblResearcher.Text = "Researcher";
            this.lblResearcher.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // chkResearcher
            // 
            this.chkResearcher.AutoSize = true;
            this.chkResearcher.Location = new System.Drawing.Point(107, 3);
            this.chkResearcher.Name = "chkResearcher";
            this.chkResearcher.Size = new System.Drawing.Size(72, 17);
            this.chkResearcher.TabIndex = 3;
            this.chkResearcher.Text = "Unlocked";
            this.chkResearcher.UseVisualStyleBackColor = true;
            this.chkResearcher.Click += new System.EventHandler(this.ChkResearcher_Click);
            // 
            // pskResearchReview
            // 
            this.pskResearchReview.AutoSize = true;
            this.pskResearchReview.CanStartTraining = false;
            this.pskResearchReview.Location = new System.Drawing.Point(3, 30);
            this.pskResearchReview.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskResearchReview.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskResearchReview.Name = "pskResearchReview";
            this.pskResearchReview.SkillData = null;
            this.pskResearchReview.Size = new System.Drawing.Size(424, 24);
            this.pskResearchReview.SkillGroupCheckbox = null;
            this.pskResearchReview.SkillName = "Research Review";
            this.pskResearchReview.TabIndex = 13;
            // 
            // pskResearchMethods
            // 
            this.pskResearchMethods.AutoSize = true;
            this.pskResearchMethods.CanStartTraining = false;
            this.pskResearchMethods.Location = new System.Drawing.Point(3, 60);
            this.pskResearchMethods.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskResearchMethods.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskResearchMethods.Name = "pskResearchMethods";
            this.pskResearchMethods.SkillData = null;
            this.pskResearchMethods.Size = new System.Drawing.Size(424, 24);
            this.pskResearchMethods.SkillGroupCheckbox = null;
            this.pskResearchMethods.SkillName = "Research Methods";
            this.pskResearchMethods.TabIndex = 12;
            // 
            // pskResearchFocus
            // 
            this.pskResearchFocus.AutoSize = true;
            this.pskResearchFocus.CanStartTraining = false;
            this.pskResearchFocus.Location = new System.Drawing.Point(3, 90);
            this.pskResearchFocus.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskResearchFocus.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskResearchFocus.Name = "pskResearchFocus";
            this.pskResearchFocus.SkillData = null;
            this.pskResearchFocus.Size = new System.Drawing.Size(424, 24);
            this.pskResearchFocus.SkillGroupCheckbox = null;
            this.pskResearchFocus.SkillName = "Research Focus";
            this.pskResearchFocus.TabIndex = 14;
            // 
            // flpSurveyorSkillGroup
            // 
            this.flpSurveyorSkillGroup.AutoSize = true;
            this.flpSurveyorSkillGroup.Controls.Add(this.flpSurveyor);
            this.flpSurveyorSkillGroup.Controls.Add(this.pskSurveyingMethods);
            this.flpSurveyorSkillGroup.Controls.Add(this.pskScanningMethods);
            this.flpSurveyorSkillGroup.Controls.Add(this.pskQuartermaster);
            this.flpSurveyorSkillGroup.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpSurveyorSkillGroup.Location = new System.Drawing.Point(3, 1200);
            this.flpSurveyorSkillGroup.Name = "flpSurveyorSkillGroup";
            this.flpSurveyorSkillGroup.Size = new System.Drawing.Size(430, 117);
            this.flpSurveyorSkillGroup.TabIndex = 16;
            this.flpSurveyorSkillGroup.WrapContents = false;
            // 
            // flpSurveyor
            // 
            this.flpSurveyor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpSurveyor.AutoSize = true;
            this.flpSurveyor.Controls.Add(this.lblSurveyor);
            this.flpSurveyor.Controls.Add(this.chkSurveyor);
            this.flpSurveyor.Location = new System.Drawing.Point(2, 2);
            this.flpSurveyor.Margin = new System.Windows.Forms.Padding(2);
            this.flpSurveyor.Name = "flpSurveyor";
            this.flpSurveyor.Size = new System.Drawing.Size(426, 23);
            this.flpSurveyor.TabIndex = 4;
            // 
            // lblSurveyor
            // 
            this.lblSurveyor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSurveyor.Location = new System.Drawing.Point(2, 3);
            this.lblSurveyor.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblSurveyor.Name = "lblSurveyor";
            this.lblSurveyor.Size = new System.Drawing.Size(100, 17);
            this.lblSurveyor.TabIndex = 2;
            this.lblSurveyor.Text = "Surveyor";
            this.lblSurveyor.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // chkSurveyor
            // 
            this.chkSurveyor.AutoSize = true;
            this.chkSurveyor.Location = new System.Drawing.Point(107, 3);
            this.chkSurveyor.Name = "chkSurveyor";
            this.chkSurveyor.Size = new System.Drawing.Size(72, 17);
            this.chkSurveyor.TabIndex = 3;
            this.chkSurveyor.Text = "Unlocked";
            this.chkSurveyor.UseVisualStyleBackColor = true;
            this.chkSurveyor.Click += new System.EventHandler(this.ChkSurveyor_Click);
            // 
            // pskSurveyingMethods
            // 
            this.pskSurveyingMethods.AutoSize = true;
            this.pskSurveyingMethods.CanStartTraining = false;
            this.pskSurveyingMethods.Location = new System.Drawing.Point(3, 30);
            this.pskSurveyingMethods.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskSurveyingMethods.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskSurveyingMethods.Name = "pskSurveyingMethods";
            this.pskSurveyingMethods.SkillData = null;
            this.pskSurveyingMethods.Size = new System.Drawing.Size(424, 24);
            this.pskSurveyingMethods.SkillGroupCheckbox = null;
            this.pskSurveyingMethods.SkillName = "Surveying Methods";
            this.pskSurveyingMethods.TabIndex = 13;
            // 
            // pskScanningMethods
            // 
            this.pskScanningMethods.AutoSize = true;
            this.pskScanningMethods.CanStartTraining = false;
            this.pskScanningMethods.Location = new System.Drawing.Point(3, 60);
            this.pskScanningMethods.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskScanningMethods.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskScanningMethods.Name = "pskScanningMethods";
            this.pskScanningMethods.SkillData = null;
            this.pskScanningMethods.Size = new System.Drawing.Size(424, 24);
            this.pskScanningMethods.SkillGroupCheckbox = null;
            this.pskScanningMethods.SkillName = "Scanning Methods";
            this.pskScanningMethods.TabIndex = 12;
            // 
            // pskQuartermaster
            // 
            this.pskQuartermaster.AutoSize = true;
            this.pskQuartermaster.CanStartTraining = false;
            this.pskQuartermaster.Location = new System.Drawing.Point(3, 90);
            this.pskQuartermaster.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskQuartermaster.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskQuartermaster.Name = "pskQuartermaster";
            this.pskQuartermaster.SkillData = null;
            this.pskQuartermaster.Size = new System.Drawing.Size(424, 24);
            this.pskQuartermaster.SkillGroupCheckbox = null;
            this.pskQuartermaster.SkillName = "Quartermaster";
            this.pskQuartermaster.TabIndex = 14;
            // 
            // flpTraderSkillGroup
            // 
            this.flpTraderSkillGroup.AutoSize = true;
            this.flpTraderSkillGroup.Controls.Add(this.flpTrader);
            this.flpTraderSkillGroup.Controls.Add(this.pskBroker);
            this.flpTraderSkillGroup.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpTraderSkillGroup.Location = new System.Drawing.Point(3, 1323);
            this.flpTraderSkillGroup.Name = "flpTraderSkillGroup";
            this.flpTraderSkillGroup.Size = new System.Drawing.Size(430, 57);
            this.flpTraderSkillGroup.TabIndex = 16;
            this.flpTraderSkillGroup.WrapContents = false;
            // 
            // flpTrader
            // 
            this.flpTrader.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.flpTrader.AutoSize = true;
            this.flpTrader.Controls.Add(this.lblTrader);
            this.flpTrader.Controls.Add(this.chkTrader);
            this.flpTrader.Location = new System.Drawing.Point(2, 2);
            this.flpTrader.Margin = new System.Windows.Forms.Padding(2);
            this.flpTrader.Name = "flpTrader";
            this.flpTrader.Size = new System.Drawing.Size(426, 23);
            this.flpTrader.TabIndex = 4;
            // 
            // lblTrader
            // 
            this.lblTrader.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTrader.Location = new System.Drawing.Point(2, 3);
            this.lblTrader.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblTrader.Name = "lblTrader";
            this.lblTrader.Size = new System.Drawing.Size(100, 17);
            this.lblTrader.TabIndex = 2;
            this.lblTrader.Text = "Trader";
            this.lblTrader.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // chkTrader
            // 
            this.chkTrader.AutoSize = true;
            this.chkTrader.Location = new System.Drawing.Point(107, 3);
            this.chkTrader.Name = "chkTrader";
            this.chkTrader.Size = new System.Drawing.Size(72, 17);
            this.chkTrader.TabIndex = 3;
            this.chkTrader.Text = "Unlocked";
            this.chkTrader.UseVisualStyleBackColor = true;
            this.chkTrader.Click += new System.EventHandler(this.ChkTrader_Click);
            // 
            // pskBroker
            // 
            this.pskBroker.AutoSize = true;
            this.pskBroker.CanStartTraining = false;
            this.pskBroker.Location = new System.Drawing.Point(3, 30);
            this.pskBroker.MaximumSize = new System.Drawing.Size(424, 24);
            this.pskBroker.MinimumSize = new System.Drawing.Size(424, 24);
            this.pskBroker.Name = "pskBroker";
            this.pskBroker.SkillData = null;
            this.pskBroker.Size = new System.Drawing.Size(424, 24);
            this.pskBroker.SkillGroupCheckbox = null;
            this.pskBroker.SkillName = "Broker";
            this.pskBroker.TabIndex = 13;
            // 
            // flpCommands
            // 
            this.flpCommands.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.flpCommands.AutoSize = true;
            this.flpCommands.Controls.Add(this.cmdImport);
            this.flpCommands.Controls.Add(this.cmdSyncApi);
            this.flpCommands.Controls.Add(this.cmdNew);
            this.flpCommands.Controls.Add(this.cmdSave);
            this.flpCommands.Controls.Add(this.cmdDelete);
            this.flpCommands.Location = new System.Drawing.Point(2, 406);
            this.flpCommands.Margin = new System.Windows.Forms.Padding(2);
            this.flpCommands.Name = "flpCommands";
            this.flpCommands.Size = new System.Drawing.Size(324, 29);
            this.flpCommands.TabIndex = 1;
            this.flpCommands.WrapContents = false;
            // 
            // cmdImport
            // 
            this.cmdImport.Location = new System.Drawing.Point(3, 3);
            this.cmdImport.Name = "cmdImport";
            this.cmdImport.Size = new System.Drawing.Size(75, 23);
            this.cmdImport.TabIndex = 4;
            this.cmdImport.Text = "Import";
            this.cmdImport.UseVisualStyleBackColor = true;
            this.cmdImport.Click += new System.EventHandler(this.CmdImport_Click);
            // 
            // cmdSyncApi
            // 
            this.cmdSyncApi.Location = new System.Drawing.Point(84, 3);
            this.cmdSyncApi.Name = "cmdSyncApi";
            this.cmdSyncApi.Size = new System.Drawing.Size(75, 23);
            this.cmdSyncApi.TabIndex = 5;
            this.cmdSyncApi.Text = "Sync API";
            this.cmdSyncApi.UseVisualStyleBackColor = true;
            this.cmdSyncApi.Click += new System.EventHandler(this.CmdSyncApi_Click);
            // 
            // cmdNew
            // 
            this.cmdNew.Location = new System.Drawing.Point(84, 3);
            this.cmdNew.Name = "cmdNew";
            this.cmdNew.Size = new System.Drawing.Size(75, 23);
            this.cmdNew.TabIndex = 3;
            this.cmdNew.Text = "New";
            this.cmdNew.UseVisualStyleBackColor = true;
            this.cmdNew.Click += new System.EventHandler(this.CmdNew_Click);
            // 
            // cmdSave
            // 
            this.cmdSave.Location = new System.Drawing.Point(165, 3);
            this.cmdSave.Name = "cmdSave";
            this.cmdSave.Size = new System.Drawing.Size(75, 23);
            this.cmdSave.TabIndex = 0;
            this.cmdSave.Text = "Save";
            this.cmdSave.UseVisualStyleBackColor = true;
            this.cmdSave.Click += new System.EventHandler(this.CmdSave_Click);
            // 
            // cmdDelete
            // 
            this.cmdDelete.Location = new System.Drawing.Point(246, 3);
            this.cmdDelete.Name = "cmdDelete";
            this.cmdDelete.Size = new System.Drawing.Size(75, 23);
            this.cmdDelete.TabIndex = 1;
            this.cmdDelete.Text = "Delete";
            this.cmdDelete.UseVisualStyleBackColor = true;
            this.cmdDelete.Click += new System.EventHandler(this.CmdDelete_Click);
            // 
            // FormPlayerProfile
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1079, 594);
            this.Controls.Add(this.flpBase);
            this.Text = "FormPlayerProfile";
            this.flpBase.ResumeLayout(false);
            this.flpSearchList.ResumeLayout(false);
            this.flpSearchList.PerformLayout();
            this.flpBlueprintSearch.ResumeLayout(false);
            this.flpBlueprintSearch.PerformLayout();
            this.flpResource.ResumeLayout(false);
            this.flpPlayerData.ResumeLayout(false);
            this.flpPlayerData.PerformLayout();
            this.flpPlayerDetails.ResumeLayout(false);
            this.flpPlayerDetails.PerformLayout();
            this.flpPlayerName.ResumeLayout(false);
            this.flpPlayerName.PerformLayout();
            this.flpCharacterId.ResumeLayout(false);
            this.flpCharacterId.PerformLayout();
            this.flpFirstName.ResumeLayout(false);
            this.flpFirstName.PerformLayout();
            this.flpLastName.ResumeLayout(false);
            this.flpLastName.PerformLayout();
            this.flpActiveTime.ResumeLayout(false);
            this.flpActiveTime.PerformLayout();
            this.flpTotalCredits.ResumeLayout(false);
            this.flpTotalCredits.PerformLayout();
            this.flpFaction.ResumeLayout(false);
            this.flpFaction.PerformLayout();
            this.flpPublicRankBlock.ResumeLayout(false);
            this.flpPublicRankBlock.PerformLayout();
            this.flpPublicRank.ResumeLayout(false);
            this.flpPublicRank.PerformLayout();
            this.flpPublicRankCurXP.ResumeLayout(false);
            this.flpPublicRankCurXP.PerformLayout();
            this.flpPublicRankNextXP.ResumeLayout(false);
            this.flpPublicRankNextXP.PerformLayout();
            this.flpPrivateRankBlock.ResumeLayout(false);
            this.flpPrivateRankBlock.PerformLayout();
            this.flpPrivateRank.ResumeLayout(false);
            this.flpPrivateRank.PerformLayout();
            this.flpPrivateRankCurXP.ResumeLayout(false);
            this.flpPrivateRankCurXP.PerformLayout();
            this.flpPrivateRankNextXP.ResumeLayout(false);
            this.flpPrivateRankNextXP.PerformLayout();
            this.flpMilitaryRankBlock.ResumeLayout(false);
            this.flpMilitaryRankBlock.PerformLayout();
            this.flpMilitaryRank.ResumeLayout(false);
            this.flpMilitaryRank.PerformLayout();
            this.flpMilitaryRankCurXP.ResumeLayout(false);
            this.flpMilitaryRankCurXP.PerformLayout();
            this.flpMilitaryRankNextXP.ResumeLayout(false);
            this.flpMilitaryRankNextXP.PerformLayout();
            this.flpSkillPoints.ResumeLayout(false);
            this.flpSkillPoints.PerformLayout();
            this.flpColonyDirectorSkillGroup.ResumeLayout(false);
            this.flpColonyDirectorSkillGroup.PerformLayout();
            this.flpColonyDirector.ResumeLayout(false);
            this.flpColonyDirector.PerformLayout();
            this.flpColonyFounderSkillGroup.ResumeLayout(false);
            this.flpColonyFounderSkillGroup.PerformLayout();
            this.flpColonyFounder.ResumeLayout(false);
            this.flpColonyFounder.PerformLayout();
            this.flpColonyOperationsSkillGroup.ResumeLayout(false);
            this.flpColonyOperationsSkillGroup.PerformLayout();
            this.flpColonyOperations.ResumeLayout(false);
            this.flpColonyOperations.PerformLayout();
            this.flpCommanderSkillGroup.ResumeLayout(false);
            this.flpCommanderSkillGroup.PerformLayout();
            this.flpCommander.ResumeLayout(false);
            this.flpCommander.PerformLayout();
            this.flpEngineerSkillGroup.ResumeLayout(false);
            this.flpEngineerSkillGroup.PerformLayout();
            this.flpEngineer.ResumeLayout(false);
            this.flpEngineer.PerformLayout();
            this.flpEntrepeneurSkillGroup.ResumeLayout(false);
            this.flpEntrepeneurSkillGroup.PerformLayout();
            this.flpEntrepeneur.ResumeLayout(false);
            this.flpEntrepeneur.PerformLayout();
            this.flpJobManagementSkillGroup.ResumeLayout(false);
            this.flpJobManagementSkillGroup.PerformLayout();
            this.flpJobManagement.ResumeLayout(false);
            this.flpJobManagement.PerformLayout();
            this.flpResearcherSkillGroup.ResumeLayout(false);
            this.flpResearcherSkillGroup.PerformLayout();
            this.flpResearcher.ResumeLayout(false);
            this.flpResearcher.PerformLayout();
            this.flpSurveyorSkillGroup.ResumeLayout(false);
            this.flpSurveyorSkillGroup.PerformLayout();
            this.flpSurveyor.ResumeLayout(false);
            this.flpSurveyor.PerformLayout();
            this.flpTraderSkillGroup.ResumeLayout(false);
            this.flpTraderSkillGroup.PerformLayout();
            this.flpTrader.ResumeLayout(false);
            this.flpTrader.PerformLayout();
            this.flpCommands.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flpBase;
        private System.Windows.Forms.FlowLayoutPanel flpSearchList;
        private System.Windows.Forms.FlowLayoutPanel flpBlueprintSearch;
        private System.Windows.Forms.Label lblNameFilter;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtNameFilter;
        private System.Windows.Forms.FlowLayoutPanel flpResource;
        private System.Windows.Forms.Label lblResource;
        private System.Windows.Forms.ComboBox cmbResource;
        private System.Windows.Forms.ListView lvwPlayerProfiles;
        private System.Windows.Forms.FlowLayoutPanel flpPlayerData;
        private System.Windows.Forms.FlowLayoutPanel flpPlayerDetails;
        private System.Windows.Forms.FlowLayoutPanel flpPlayerName;
        private System.Windows.Forms.Label lblPlayerName;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtPlayerName;
        private System.Windows.Forms.FlowLayoutPanel flpCharacterId;
        private System.Windows.Forms.Label lblCharacterId;
        private System.Windows.Forms.Label lblCharacterIdValue;
        private System.Windows.Forms.FlowLayoutPanel flpFirstName;
        private System.Windows.Forms.Label lblFirstName;
        private System.Windows.Forms.Label lblFirstNameValue;
        private System.Windows.Forms.FlowLayoutPanel flpLastName;
        private System.Windows.Forms.Label lblLastName;
        private System.Windows.Forms.Label lblLastNameValue;
        private System.Windows.Forms.FlowLayoutPanel flpActiveTime;
        private System.Windows.Forms.Label lblActiveTime;
        private System.Windows.Forms.Label lblActiveTimeValue;
        private System.Windows.Forms.FlowLayoutPanel flpTotalCredits;
        private System.Windows.Forms.Label lblTotalCredits;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtTotalCredits;
        private System.Windows.Forms.FlowLayoutPanel flpFaction;
        private System.Windows.Forms.Label lblFaction;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtFaction;
        private System.Windows.Forms.ComboBox cmbFaction;
        private System.Windows.Forms.FlowLayoutPanel flpPublicRank;
        private System.Windows.Forms.Label lblPublicRank;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtPublicRank;
        private System.Windows.Forms.Label lblPublicRankName;
        private System.Windows.Forms.FlowLayoutPanel flpCommands;
        private System.Windows.Forms.Button cmdImport;
        private System.Windows.Forms.Button cmdSyncApi;
        private System.Windows.Forms.Button cmdNew;
        private System.Windows.Forms.Button cmdSave;
        private System.Windows.Forms.Button cmdDelete;
        private System.Windows.Forms.FlowLayoutPanel flpPublicRankBlock;
        private System.Windows.Forms.FlowLayoutPanel flpPublicRankCurXP;
        private System.Windows.Forms.Label lblPublicRankCurXP;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtPublicRankCurXP;
        private System.Windows.Forms.FlowLayoutPanel flpPublicRankNextXP;
        private System.Windows.Forms.Label lblPublicRankNextXP;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtPublicRankNextXP;
        private System.Windows.Forms.FlowLayoutPanel flpPrivateRankBlock;
        private System.Windows.Forms.FlowLayoutPanel flpPrivateRank;
        private System.Windows.Forms.Label lblPrivateRank;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtPrivateRank;
        private System.Windows.Forms.Label lblPrivateRankName;
        private System.Windows.Forms.FlowLayoutPanel flpPrivateRankCurXP;
        private System.Windows.Forms.Label lblPrivateRankCurXP;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtPrivateRankCurXP;
        private System.Windows.Forms.FlowLayoutPanel flpPrivateRankNextXP;
        private System.Windows.Forms.Label lblPrivateRankNextXP;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtPrivateRankNextXP;
        private System.Windows.Forms.FlowLayoutPanel flpMilitaryRankBlock;
        private System.Windows.Forms.FlowLayoutPanel flpMilitaryRank;
        private System.Windows.Forms.Label lblMilitaryRank;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtMilitaryRank;
        private System.Windows.Forms.Label lblMilitaryRankName;
        private System.Windows.Forms.FlowLayoutPanel flpMilitaryRankCurXP;
        private System.Windows.Forms.Label lblMilitaryRankCurXP;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtMilitaryRankCurXP;
        private System.Windows.Forms.FlowLayoutPanel flpMilitaryRankNextXP;
        private System.Windows.Forms.Label lblMilitaryRankNextXP;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtMilitaryRankNextXP;
        private System.Windows.Forms.FlowLayoutPanel flpColonyDirectorSkillGroup;
        private System.Windows.Forms.FlowLayoutPanel flpColonyDirector;
        private System.Windows.Forms.Label lblColonyDirector;
        private System.Windows.Forms.CheckBox chkColonyDirector;
        private System.Windows.Forms.FlowLayoutPanel flpColonyFounderSkillGroup;
        private System.Windows.Forms.FlowLayoutPanel flpColonyFounder;
        private System.Windows.Forms.Label lblColonyFounder;
        private System.Windows.Forms.CheckBox chkColonyFounder;
        private PlayerSkillBlock pskHumanResources;
        private PlayerSkillBlock pskForeman;
        private PlayerSkillBlock pskFounder;
        private PlayerSkillBlock pskEnergyEfficiency;
        private PlayerSkillBlock pskBuilder;
        private System.Windows.Forms.FlowLayoutPanel flpColonyOperationsSkillGroup;
        private System.Windows.Forms.FlowLayoutPanel flpColonyOperations;
        private System.Windows.Forms.Label lblColonyOperations;
        private System.Windows.Forms.CheckBox chkColonyOperations;
        private PlayerSkillBlock pskRefiningFocus;
        private PlayerSkillBlock pskProductionFocus;
        private PlayerSkillBlock pskExtractionFocus;
        private System.Windows.Forms.FlowLayoutPanel flpCommanderSkillGroup;
        private System.Windows.Forms.FlowLayoutPanel flpCommander;
        private System.Windows.Forms.Label lblCommander;
        private System.Windows.Forms.CheckBox chkCommander;
        private PlayerSkillBlock pskDamageControl;
        private System.Windows.Forms.FlowLayoutPanel flpEngineerSkillGroup;
        private System.Windows.Forms.FlowLayoutPanel flpEngineer;
        private System.Windows.Forms.Label lblEngineer;
        private System.Windows.Forms.CheckBox chkEngineer;
        private PlayerSkillBlock pskEngineeringCapacity;
        private System.Windows.Forms.FlowLayoutPanel flpEntrepeneurSkillGroup;
        private System.Windows.Forms.FlowLayoutPanel flpEntrepeneur;
        private System.Windows.Forms.Label lblEntrepeneur;
        private System.Windows.Forms.CheckBox chkEntrepeneur;
        private PlayerSkillBlock pskSoundAsAPound;
        private PlayerSkillBlock pskSelfMadeMillionaire;
        private PlayerSkillBlock pskAAAHealthcare;
        private System.Windows.Forms.FlowLayoutPanel flpJobManagementSkillGroup;
        private System.Windows.Forms.FlowLayoutPanel flpJobManagement;
        private System.Windows.Forms.Label lblJobManagement;
        private System.Windows.Forms.CheckBox chkJobManagement;
        private PlayerSkillBlock pskJobOpportunities;
        private PlayerSkillBlock pskContractManagement;
        private System.Windows.Forms.FlowLayoutPanel flpResearcherSkillGroup;
        private System.Windows.Forms.FlowLayoutPanel flpResearcher;
        private System.Windows.Forms.Label lblResearcher;
        private System.Windows.Forms.CheckBox chkResearcher;
        private PlayerSkillBlock pskResearchReview;
        private PlayerSkillBlock pskResearchMethods;
        private PlayerSkillBlock pskResearchFocus;
        private System.Windows.Forms.FlowLayoutPanel flpSurveyorSkillGroup;
        private System.Windows.Forms.FlowLayoutPanel flpSurveyor;
        private System.Windows.Forms.Label lblSurveyor;
        private System.Windows.Forms.CheckBox chkSurveyor;
        private PlayerSkillBlock pskSurveyingMethods;
        private PlayerSkillBlock pskScanningMethods;
        private PlayerSkillBlock pskQuartermaster;
        private System.Windows.Forms.FlowLayoutPanel flpTraderSkillGroup;
        private System.Windows.Forms.FlowLayoutPanel flpTrader;
        private System.Windows.Forms.Label lblTrader;
        private System.Windows.Forms.CheckBox chkTrader;
        private PlayerSkillBlock pskBroker;
        private System.Windows.Forms.FlowLayoutPanel flpSkillPoints;
        private System.Windows.Forms.Label lblSkillPoints;
        private OE2EmpireTracker.Controls.ValidatedTextBox txtSkillPoints;
    }
}