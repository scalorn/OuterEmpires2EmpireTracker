namespace OE2EmpireTracker.Forms
{
    partial class FormPreferences
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabThresholds = new System.Windows.Forms.TabPage();
            this.tabServer = new System.Windows.Forms.TabPage();
            this.tabGameApi = new System.Windows.Forms.TabPage();
            this.grpWarehouseThresholds = new System.Windows.Forms.GroupBox();
            this.lblOverflowHorizon = new System.Windows.Forms.Label();
            this.nudOverflowHorizon = new System.Windows.Forms.NumericUpDown();
            this.lblUnderutilizedStockpile = new System.Windows.Forms.Label();
            this.nudUnderutilizedStockpile = new System.Windows.Forms.NumericUpDown();
            this.grpGameApi = new System.Windows.Forms.GroupBox();
            this.lblGameApiUrl = new System.Windows.Forms.Label();
            this.txtGameApiUrl = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblGameApiAppId = new System.Windows.Forms.Label();
            this.txtGameApiAppId = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblGameApiClientId = new System.Windows.Forms.Label();
            this.txtGameApiClientId = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblGameApiCharacter = new System.Windows.Forms.Label();
            this.cmbGameApiCharacter = new System.Windows.Forms.ComboBox();
            this.lblGameApiSecret = new System.Windows.Forms.Label();
            this.txtGameApiSecret = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblPollingInterval = new System.Windows.Forms.Label();
            this.nudPollingInterval = new System.Windows.Forms.NumericUpDown();
            this.lblPollingMinutes = new System.Windows.Forms.Label();
            this.lblTpsLimit = new System.Windows.Forms.Label();
            this.txtTpsLimit = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblDetailRefreshHours = new System.Windows.Forms.Label();
            this.nudDetailRefreshHours = new System.Windows.Forms.NumericUpDown();
            this.chkGameApiEnabled = new System.Windows.Forms.CheckBox();
            this.btnTestGameApiConnection = new System.Windows.Forms.Button();
            this.lblTestResult = new System.Windows.Forms.Label();
            this.grpStructureCount = new System.Windows.Forms.GroupBox();
            this.lblStructureYellow = new System.Windows.Forms.Label();
            this.txtStructureYellow = new System.Windows.Forms.TextBox();
            this.lblStructureRed = new System.Windows.Forms.Label();
            this.txtStructureRed = new System.Windows.Forms.TextBox();
            this.grpWorkerRequest = new System.Windows.Forms.GroupBox();
            this.lblWorkerYellow = new System.Windows.Forms.Label();
            this.txtWorkerYellow = new System.Windows.Forms.TextBox();
            this.lblWorkerRed = new System.Windows.Forms.Label();
            this.txtWorkerRed = new System.Windows.Forms.TextBox();
            this.grpColonyImport = new System.Windows.Forms.GroupBox();
            this.lblColonyImportYellow = new System.Windows.Forms.Label();
            this.txtColonyImportYellow = new System.Windows.Forms.TextBox();
            this.lblColonyImportRed = new System.Windows.Forms.Label();
            this.txtColonyImportRed = new System.Windows.Forms.TextBox();
            this.grpBackgroundProcessing = new System.Windows.Forms.GroupBox();
            this.lblBackgroundInterval = new System.Windows.Forms.Label();
            this.txtBackgroundInterval = new System.Windows.Forms.TextBox();
            this.grpAdminReport = new System.Windows.Forms.GroupBox();
            this.lblAdminRefresh = new System.Windows.Forms.Label();
            this.txtAdminRefresh = new System.Windows.Forms.TextBox();
            this.grpCountdownDisplay = new System.Windows.Forms.GroupBox();
            this.lblCountdownRefresh = new System.Windows.Forms.Label();
            this.txtCountdownRefresh = new System.Windows.Forms.TextBox();
            this.grpServerConnection = new System.Windows.Forms.GroupBox();
            this.lblServerUrl = new System.Windows.Forms.Label();
            this.txtServerUrl = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.btnTestConnection = new System.Windows.Forms.Button();
            this.btnPushLocalToServer = new System.Windows.Forms.Button();
            this.lblThumbprint = new System.Windows.Forms.Label();
            this.txtThumbprint = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblBearerToken = new System.Windows.Forms.Label();
            this.txtBearerToken = new OE2EmpireTracker.Controls.ValidatedTextBox();
            this.lblOperatingMode = new System.Windows.Forms.Label();
            this.cmbOperatingMode = new System.Windows.Forms.ComboBox();
            this.lblConnectionStatus = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnResetDefaults = new System.Windows.Forms.Button();
            this.tabControl.SuspendLayout();
            this.tabThresholds.SuspendLayout();
            this.tabServer.SuspendLayout();
            this.tabGameApi.SuspendLayout();
            this.grpGameApi.SuspendLayout();
            this.grpWarehouseThresholds.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudPollingInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDetailRefreshHours)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudOverflowHorizon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudUnderutilizedStockpile)).BeginInit();
            this.grpStructureCount.SuspendLayout();
            this.grpWorkerRequest.SuspendLayout();
            this.grpColonyImport.SuspendLayout();
            this.grpBackgroundProcessing.SuspendLayout();
            this.grpAdminReport.SuspendLayout();
            this.grpCountdownDisplay.SuspendLayout();
            this.grpServerConnection.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabThresholds);
            this.tabControl.Controls.Add(this.tabServer);
            this.tabControl.Controls.Add(this.tabGameApi);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Top;
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(498, 550);
            this.tabControl.TabIndex = 0;
            // 
            // tabThresholds
            // 
            this.tabThresholds.Controls.Add(this.grpStructureCount);
            this.tabThresholds.Controls.Add(this.grpWorkerRequest);
            this.tabThresholds.Controls.Add(this.grpColonyImport);
            this.tabThresholds.Controls.Add(this.grpBackgroundProcessing);
            this.tabThresholds.Controls.Add(this.grpAdminReport);
            this.tabThresholds.Controls.Add(this.grpCountdownDisplay);
            this.tabThresholds.Controls.Add(this.grpWarehouseThresholds);
            this.tabThresholds.Location = new System.Drawing.Point(4, 22);
            this.tabThresholds.Name = "tabThresholds";
            this.tabThresholds.Padding = new System.Windows.Forms.Padding(3);
            this.tabThresholds.Size = new System.Drawing.Size(490, 524);
            this.tabThresholds.TabIndex = 0;
            this.tabThresholds.Text = "Thresholds";
            this.tabThresholds.UseVisualStyleBackColor = true;
            // 
            // tabServer
            // 
            this.tabServer.Controls.Add(this.grpServerConnection);
            this.tabServer.Location = new System.Drawing.Point(4, 22);
            this.tabServer.Name = "tabServer";
            this.tabServer.Padding = new System.Windows.Forms.Padding(3);
            this.tabServer.Size = new System.Drawing.Size(490, 524);
            this.tabServer.TabIndex = 1;
            this.tabServer.Text = "Server";
            this.tabServer.UseVisualStyleBackColor = true;
            // 
            // tabGameApi
            // 
            this.tabGameApi.Controls.Add(this.grpGameApi);
            this.tabGameApi.Location = new System.Drawing.Point(4, 22);
            this.tabGameApi.Name = "tabGameApi";
            this.tabGameApi.Padding = new System.Windows.Forms.Padding(3);
            this.tabGameApi.Size = new System.Drawing.Size(490, 524);
            this.tabGameApi.TabIndex = 2;
            this.tabGameApi.Text = "Game API";
            this.tabGameApi.UseVisualStyleBackColor = true;
            // 
            // grpGameApi
            // 
            this.grpGameApi.Controls.Add(this.lblGameApiUrl);
            this.grpGameApi.Controls.Add(this.txtGameApiUrl);
            this.grpGameApi.Controls.Add(this.lblGameApiAppId);
            this.grpGameApi.Controls.Add(this.txtGameApiAppId);
            this.grpGameApi.Controls.Add(this.lblGameApiClientId);
            this.grpGameApi.Controls.Add(this.txtGameApiClientId);
            this.grpGameApi.Controls.Add(this.lblGameApiCharacter);
            this.grpGameApi.Controls.Add(this.cmbGameApiCharacter);
            this.grpGameApi.Controls.Add(this.lblGameApiSecret);
            this.grpGameApi.Controls.Add(this.txtGameApiSecret);
            this.grpGameApi.Controls.Add(this.lblPollingInterval);
            this.grpGameApi.Controls.Add(this.nudPollingInterval);
            this.grpGameApi.Controls.Add(this.lblPollingMinutes);
            this.grpGameApi.Controls.Add(this.lblTpsLimit);
            this.grpGameApi.Controls.Add(this.txtTpsLimit);
            this.grpGameApi.Controls.Add(this.lblDetailRefreshHours);
            this.grpGameApi.Controls.Add(this.nudDetailRefreshHours);
            this.grpGameApi.Controls.Add(this.chkGameApiEnabled);
            this.grpGameApi.Controls.Add(this.btnTestGameApiConnection);
            this.grpGameApi.Controls.Add(this.lblTestResult);
            this.grpGameApi.Location = new System.Drawing.Point(12, 12);
            this.grpGameApi.Name = "grpGameApi";
            this.grpGameApi.Size = new System.Drawing.Size(460, 380);
            this.grpGameApi.TabIndex = 0;
            this.grpGameApi.TabStop = false;
            this.grpGameApi.Text = "Game API Connection (OAuth2)";
            // 
            // lblGameApiUrl
            // 
            this.lblGameApiUrl.AutoSize = true;
            this.lblGameApiUrl.Location = new System.Drawing.Point(15, 28);
            this.lblGameApiUrl.Name = "lblGameApiUrl";
            this.lblGameApiUrl.Size = new System.Drawing.Size(63, 13);
            this.lblGameApiUrl.Text = "Server URL:";
            // 
            // txtGameApiUrl
            // 
            this.txtGameApiUrl.Location = new System.Drawing.Point(130, 25);
            this.txtGameApiUrl.Name = "txtGameApiUrl";
            this.txtGameApiUrl.Size = new System.Drawing.Size(320, 20);
            this.txtGameApiUrl.TabIndex = 1;
            // 
            // lblGameApiAppId
            // 
            this.lblGameApiAppId.AutoSize = true;
            this.lblGameApiAppId.Location = new System.Drawing.Point(15, 63);
            this.lblGameApiAppId.Name = "lblGameApiAppId";
            this.lblGameApiAppId.Size = new System.Drawing.Size(40, 13);
            this.lblGameApiAppId.Text = "App ID:";
            // 
            // txtGameApiAppId
            // 
            this.txtGameApiAppId.Location = new System.Drawing.Point(130, 60);
            this.txtGameApiAppId.Name = "txtGameApiAppId";
            this.txtGameApiAppId.Size = new System.Drawing.Size(320, 20);
            this.txtGameApiAppId.TabIndex = 2;
            // 
            // lblGameApiClientId
            // 
            this.lblGameApiClientId.AutoSize = true;
            this.lblGameApiClientId.Location = new System.Drawing.Point(15, 98);
            this.lblGameApiClientId.Name = "lblGameApiClientId";
            this.lblGameApiClientId.Size = new System.Drawing.Size(52, 13);
            this.lblGameApiClientId.Text = "Client ID:";
            // 
            // txtGameApiClientId
            // 
            this.txtGameApiClientId.Location = new System.Drawing.Point(130, 95);
            this.txtGameApiClientId.Name = "txtGameApiClientId";
            this.txtGameApiClientId.Size = new System.Drawing.Size(320, 20);
            this.txtGameApiClientId.TabIndex = 3;
            // 
            // lblGameApiCharacter
            // 
            this.lblGameApiCharacter.AutoSize = true;
            this.lblGameApiCharacter.Location = new System.Drawing.Point(15, 133);
            this.lblGameApiCharacter.Name = "lblGameApiCharacter";
            this.lblGameApiCharacter.Size = new System.Drawing.Size(59, 13);
            this.lblGameApiCharacter.Text = "Character:";
            // 
            // cmbGameApiCharacter
            // 
            this.cmbGameApiCharacter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbGameApiCharacter.FormattingEnabled = true;
            this.cmbGameApiCharacter.Location = new System.Drawing.Point(130, 130);
            this.cmbGameApiCharacter.Name = "cmbGameApiCharacter";
            this.cmbGameApiCharacter.Size = new System.Drawing.Size(320, 21);
            this.cmbGameApiCharacter.TabIndex = 4;
            // 
            // lblGameApiSecret
            // 
            this.lblGameApiSecret.AutoSize = true;
            this.lblGameApiSecret.Location = new System.Drawing.Point(15, 168);
            this.lblGameApiSecret.Name = "lblGameApiSecret";
            this.lblGameApiSecret.Size = new System.Drawing.Size(41, 13);
            this.lblGameApiSecret.Text = "Secret:";
            // 
            // txtGameApiSecret
            // 
            this.txtGameApiSecret.Location = new System.Drawing.Point(130, 165);
            this.txtGameApiSecret.Name = "txtGameApiSecret";
            this.txtGameApiSecret.PasswordChar = '\u25CF';
            this.txtGameApiSecret.Size = new System.Drawing.Size(320, 20);
            this.txtGameApiSecret.TabIndex = 5;
            // 
            // lblPollingInterval
            // 
            this.lblPollingInterval.AutoSize = true;
            this.lblPollingInterval.Location = new System.Drawing.Point(15, 203);
            this.lblPollingInterval.Name = "lblPollingInterval";
            this.lblPollingInterval.Size = new System.Drawing.Size(83, 13);
            this.lblPollingInterval.Text = "Polling Interval:";
            // 
            // nudPollingInterval
            // 
            this.nudPollingInterval.Location = new System.Drawing.Point(130, 200);
            this.nudPollingInterval.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            this.nudPollingInterval.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudPollingInterval.Name = "nudPollingInterval";
            this.nudPollingInterval.Size = new System.Drawing.Size(60, 20);
            this.nudPollingInterval.TabIndex = 6;
            this.nudPollingInterval.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // lblPollingMinutes
            // 
            this.lblPollingMinutes.AutoSize = true;
            this.lblPollingMinutes.Location = new System.Drawing.Point(196, 203);
            this.lblPollingMinutes.Name = "lblPollingMinutes";
            this.lblPollingMinutes.Size = new System.Drawing.Size(44, 13);
            this.lblPollingMinutes.Text = "minutes";
            // 
            // lblTpsLimit
            // 
            this.lblTpsLimit.AutoSize = true;
            this.lblTpsLimit.Location = new System.Drawing.Point(15, 238);
            this.lblTpsLimit.Name = "lblTpsLimit";
            this.lblTpsLimit.Size = new System.Drawing.Size(57, 13);
            this.lblTpsLimit.Text = "TPS Limit:";
            // 
            // txtTpsLimit
            // 
            this.txtTpsLimit.Location = new System.Drawing.Point(130, 235);
            this.txtTpsLimit.Name = "txtTpsLimit";
            this.txtTpsLimit.Size = new System.Drawing.Size(75, 20);
            this.txtTpsLimit.TabIndex = 7;
            this.txtTpsLimit.ValidationPattern = "^\\d{1,3}(\\.\\d{1,3})?$";
            this.txtTpsLimit.ValidColor = System.Drawing.Color.White;
            // 
            // lblDetailRefreshHours
            // 
            this.lblDetailRefreshHours.AutoSize = true;
            this.lblDetailRefreshHours.Location = new System.Drawing.Point(15, 270);
            this.lblDetailRefreshHours.Name = "lblDetailRefreshHours";
            this.lblDetailRefreshHours.Size = new System.Drawing.Size(113, 13);
            this.lblDetailRefreshHours.Text = "Detail Refresh (hours):";
            // 
            // nudDetailRefreshHours
            // 
            this.nudDetailRefreshHours.Location = new System.Drawing.Point(130, 267);
            this.nudDetailRefreshHours.Maximum = new decimal(new int[] { 168, 0, 0, 0 });
            this.nudDetailRefreshHours.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudDetailRefreshHours.Name = "nudDetailRefreshHours";
            this.nudDetailRefreshHours.Size = new System.Drawing.Size(60, 20);
            this.nudDetailRefreshHours.TabIndex = 8;
            this.nudDetailRefreshHours.Value = new decimal(new int[] { 24, 0, 0, 0 });
            // 
            // chkGameApiEnabled
            // 
            this.chkGameApiEnabled.AutoSize = true;
            this.chkGameApiEnabled.Location = new System.Drawing.Point(18, 308);
            this.chkGameApiEnabled.Name = "chkGameApiEnabled";
            this.chkGameApiEnabled.Size = new System.Drawing.Size(65, 17);
            this.chkGameApiEnabled.TabIndex = 9;
            this.chkGameApiEnabled.Text = "Enabled";
            this.chkGameApiEnabled.UseVisualStyleBackColor = true;
            // 
            // btnTestGameApiConnection
            // 
            this.btnTestGameApiConnection.Location = new System.Drawing.Point(18, 340);
            this.btnTestGameApiConnection.Name = "btnTestGameApiConnection";
            this.btnTestGameApiConnection.Size = new System.Drawing.Size(110, 23);
            this.btnTestGameApiConnection.TabIndex = 10;
            this.btnTestGameApiConnection.Text = "Test Connection";
            this.btnTestGameApiConnection.UseVisualStyleBackColor = true;
            // 
            // lblTestResult
            // 
            this.lblTestResult.AutoSize = true;
            this.lblTestResult.Location = new System.Drawing.Point(134, 345);
            this.lblTestResult.Name = "lblTestResult";
            this.lblTestResult.Size = new System.Drawing.Size(0, 13);
            this.lblTestResult.TabIndex = 11;
            // 
            // grpStructureCount
            // 
            this.grpStructureCount.Controls.Add(this.lblStructureYellow);
            this.grpStructureCount.Controls.Add(this.txtStructureYellow);
            this.grpStructureCount.Controls.Add(this.lblStructureRed);
            this.grpStructureCount.Controls.Add(this.txtStructureRed);
            this.grpStructureCount.Location = new System.Drawing.Point(12, 12);
            this.grpStructureCount.Name = "grpStructureCount";
            this.grpStructureCount.Size = new System.Drawing.Size(460, 75);
            this.grpStructureCount.TabIndex = 0;
            this.grpStructureCount.TabStop = false;
            this.grpStructureCount.Text = "Structure Count";
            // 
            // lblStructureYellow
            // 
            this.lblStructureYellow.AutoSize = true;
            this.lblStructureYellow.Location = new System.Drawing.Point(15, 28);
            this.lblStructureYellow.Name = "lblStructureYellow";
            this.lblStructureYellow.Size = new System.Drawing.Size(93, 13);
            this.lblStructureYellow.Text = "Yellow Threshold:";
            // 
            // txtStructureYellow
            // 
            this.txtStructureYellow.Location = new System.Drawing.Point(150, 25);
            this.txtStructureYellow.Name = "txtStructureYellow";
            this.txtStructureYellow.Size = new System.Drawing.Size(80, 20);
            this.txtStructureYellow.TabIndex = 1;
            // 
            // lblStructureRed
            // 
            this.lblStructureRed.AutoSize = true;
            this.lblStructureRed.Location = new System.Drawing.Point(250, 28);
            this.lblStructureRed.Name = "lblStructureRed";
            this.lblStructureRed.Size = new System.Drawing.Size(78, 13);
            this.lblStructureRed.Text = "Red Threshold:";
            // 
            // txtStructureRed
            // 
            this.txtStructureRed.Location = new System.Drawing.Point(370, 25);
            this.txtStructureRed.Name = "txtStructureRed";
            this.txtStructureRed.Size = new System.Drawing.Size(80, 20);
            this.txtStructureRed.TabIndex = 2;
            // 
            // grpWorkerRequest
            // 
            this.grpWorkerRequest.Controls.Add(this.lblWorkerYellow);
            this.grpWorkerRequest.Controls.Add(this.txtWorkerYellow);
            this.grpWorkerRequest.Controls.Add(this.lblWorkerRed);
            this.grpWorkerRequest.Controls.Add(this.txtWorkerRed);
            this.grpWorkerRequest.Location = new System.Drawing.Point(12, 93);
            this.grpWorkerRequest.Name = "grpWorkerRequest";
            this.grpWorkerRequest.Size = new System.Drawing.Size(460, 75);
            this.grpWorkerRequest.TabIndex = 1;
            this.grpWorkerRequest.TabStop = false;
            this.grpWorkerRequest.Text = "Worker Request Due Window";
            // 
            // lblWorkerYellow
            // 
            this.lblWorkerYellow.AutoSize = true;
            this.lblWorkerYellow.Location = new System.Drawing.Point(15, 28);
            this.lblWorkerYellow.Name = "lblWorkerYellow";
            this.lblWorkerYellow.Size = new System.Drawing.Size(93, 13);
            this.lblWorkerYellow.Text = "Yellow Threshold:";
            // 
            // txtWorkerYellow
            // 
            this.txtWorkerYellow.Location = new System.Drawing.Point(150, 25);
            this.txtWorkerYellow.Name = "txtWorkerYellow";
            this.txtWorkerYellow.Size = new System.Drawing.Size(100, 20);
            this.txtWorkerYellow.TabIndex = 3;
            // 
            // lblWorkerRed
            // 
            this.lblWorkerRed.AutoSize = true;
            this.lblWorkerRed.Location = new System.Drawing.Point(270, 28);
            this.lblWorkerRed.Name = "lblWorkerRed";
            this.lblWorkerRed.Size = new System.Drawing.Size(78, 13);
            this.lblWorkerRed.Text = "Red Threshold:";
            // 
            // txtWorkerRed
            // 
            this.txtWorkerRed.Location = new System.Drawing.Point(370, 25);
            this.txtWorkerRed.Name = "txtWorkerRed";
            this.txtWorkerRed.Size = new System.Drawing.Size(80, 20);
            this.txtWorkerRed.TabIndex = 4;
            // 
            // grpColonyImport
            // 
            this.grpColonyImport.Controls.Add(this.lblColonyImportYellow);
            this.grpColonyImport.Controls.Add(this.txtColonyImportYellow);
            this.grpColonyImport.Controls.Add(this.lblColonyImportRed);
            this.grpColonyImport.Controls.Add(this.txtColonyImportRed);
            this.grpColonyImport.Location = new System.Drawing.Point(12, 174);
            this.grpColonyImport.Name = "grpColonyImport";
            this.grpColonyImport.Size = new System.Drawing.Size(460, 75);
            this.grpColonyImport.TabIndex = 2;
            this.grpColonyImport.TabStop = false;
            this.grpColonyImport.Text = "Colony Import Staleness";
            // 
            // lblColonyImportYellow
            // 
            this.lblColonyImportYellow.AutoSize = true;
            this.lblColonyImportYellow.Location = new System.Drawing.Point(15, 28);
            this.lblColonyImportYellow.Name = "lblColonyImportYellow";
            this.lblColonyImportYellow.Size = new System.Drawing.Size(93, 13);
            this.lblColonyImportYellow.Text = "Yellow Threshold:";
            // 
            // txtColonyImportYellow
            // 
            this.txtColonyImportYellow.Location = new System.Drawing.Point(150, 25);
            this.txtColonyImportYellow.Name = "txtColonyImportYellow";
            this.txtColonyImportYellow.Size = new System.Drawing.Size(100, 20);
            this.txtColonyImportYellow.TabIndex = 5;
            // 
            // lblColonyImportRed
            // 
            this.lblColonyImportRed.AutoSize = true;
            this.lblColonyImportRed.Location = new System.Drawing.Point(270, 28);
            this.lblColonyImportRed.Name = "lblColonyImportRed";
            this.lblColonyImportRed.Size = new System.Drawing.Size(78, 13);
            this.lblColonyImportRed.Text = "Red Threshold:";
            // 
            // txtColonyImportRed
            // 
            this.txtColonyImportRed.Location = new System.Drawing.Point(370, 25);
            this.txtColonyImportRed.Name = "txtColonyImportRed";
            this.txtColonyImportRed.Size = new System.Drawing.Size(80, 20);
            this.txtColonyImportRed.TabIndex = 6;
            // 
            // grpBackgroundProcessing
            // 
            this.grpBackgroundProcessing.Controls.Add(this.lblBackgroundInterval);
            this.grpBackgroundProcessing.Controls.Add(this.txtBackgroundInterval);
            this.grpBackgroundProcessing.Location = new System.Drawing.Point(12, 255);
            this.grpBackgroundProcessing.Name = "grpBackgroundProcessing";
            this.grpBackgroundProcessing.Size = new System.Drawing.Size(460, 55);
            this.grpBackgroundProcessing.TabIndex = 3;
            this.grpBackgroundProcessing.TabStop = false;
            this.grpBackgroundProcessing.Text = "Background Processing";
            // 
            // lblBackgroundInterval
            // 
            this.lblBackgroundInterval.AutoSize = true;
            this.lblBackgroundInterval.Location = new System.Drawing.Point(15, 25);
            this.lblBackgroundInterval.Name = "lblBackgroundInterval";
            this.lblBackgroundInterval.Size = new System.Drawing.Size(45, 13);
            this.lblBackgroundInterval.Text = "Interval:";
            // 
            // txtBackgroundInterval
            // 
            this.txtBackgroundInterval.Location = new System.Drawing.Point(150, 22);
            this.txtBackgroundInterval.Name = "txtBackgroundInterval";
            this.txtBackgroundInterval.Size = new System.Drawing.Size(100, 20);
            this.txtBackgroundInterval.TabIndex = 7;
            // 
            // grpAdminReport
            // 
            this.grpAdminReport.Controls.Add(this.lblAdminRefresh);
            this.grpAdminReport.Controls.Add(this.txtAdminRefresh);
            this.grpAdminReport.Location = new System.Drawing.Point(12, 316);
            this.grpAdminReport.Name = "grpAdminReport";
            this.grpAdminReport.Size = new System.Drawing.Size(460, 55);
            this.grpAdminReport.TabIndex = 4;
            this.grpAdminReport.TabStop = false;
            this.grpAdminReport.Text = "Administration Report";
            // 
            // lblAdminRefresh
            // 
            this.lblAdminRefresh.AutoSize = true;
            this.lblAdminRefresh.Location = new System.Drawing.Point(15, 25);
            this.lblAdminRefresh.Name = "lblAdminRefresh";
            this.lblAdminRefresh.Size = new System.Drawing.Size(87, 13);
            this.lblAdminRefresh.Text = "Refresh Interval:";
            // 
            // txtAdminRefresh
            // 
            this.txtAdminRefresh.Location = new System.Drawing.Point(150, 22);
            this.txtAdminRefresh.Name = "txtAdminRefresh";
            this.txtAdminRefresh.Size = new System.Drawing.Size(100, 20);
            this.txtAdminRefresh.TabIndex = 8;
            // 
            // grpCountdownDisplay
            // 
            this.grpCountdownDisplay.Controls.Add(this.lblCountdownRefresh);
            this.grpCountdownDisplay.Controls.Add(this.txtCountdownRefresh);
            this.grpCountdownDisplay.Location = new System.Drawing.Point(12, 377);
            this.grpCountdownDisplay.Name = "grpCountdownDisplay";
            this.grpCountdownDisplay.Size = new System.Drawing.Size(460, 55);
            this.grpCountdownDisplay.TabIndex = 5;
            this.grpCountdownDisplay.TabStop = false;
            this.grpCountdownDisplay.Text = "Countdown Display";
            // 
            // lblCountdownRefresh
            // 
            this.lblCountdownRefresh.AutoSize = true;
            this.lblCountdownRefresh.Location = new System.Drawing.Point(15, 25);
            this.lblCountdownRefresh.Name = "lblCountdownRefresh";
            this.lblCountdownRefresh.Size = new System.Drawing.Size(72, 13);
            this.lblCountdownRefresh.Text = "Refresh Rate:";
            // 
            // txtCountdownRefresh
            // 
            this.txtCountdownRefresh.Location = new System.Drawing.Point(150, 22);
            this.txtCountdownRefresh.Name = "txtCountdownRefresh";
            this.txtCountdownRefresh.Size = new System.Drawing.Size(100, 20);
            this.txtCountdownRefresh.TabIndex = 9;
            // 
            // grpWarehouseThresholds
            // 
            this.grpWarehouseThresholds.Controls.Add(this.lblOverflowHorizon);
            this.grpWarehouseThresholds.Controls.Add(this.nudOverflowHorizon);
            this.grpWarehouseThresholds.Controls.Add(this.lblUnderutilizedStockpile);
            this.grpWarehouseThresholds.Controls.Add(this.nudUnderutilizedStockpile);
            this.grpWarehouseThresholds.Location = new System.Drawing.Point(12, 438);
            this.grpWarehouseThresholds.Name = "grpWarehouseThresholds";
            this.grpWarehouseThresholds.Size = new System.Drawing.Size(460, 75);
            this.grpWarehouseThresholds.TabIndex = 6;
            this.grpWarehouseThresholds.TabStop = false;
            this.grpWarehouseThresholds.Text = "Warehouse Thresholds";
            // 
            // lblOverflowHorizon
            // 
            this.lblOverflowHorizon.AutoSize = true;
            this.lblOverflowHorizon.Location = new System.Drawing.Point(15, 25);
            this.lblOverflowHorizon.Name = "lblOverflowHorizon";
            this.lblOverflowHorizon.Size = new System.Drawing.Size(172, 13);
            this.lblOverflowHorizon.Text = "Overflow prediction horizon hours:";
            // 
            // nudOverflowHorizon
            // 
            this.nudOverflowHorizon.Location = new System.Drawing.Point(220, 22);
            this.nudOverflowHorizon.Maximum = new decimal(new int[] { 336, 0, 0, 0 });
            this.nudOverflowHorizon.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudOverflowHorizon.Name = "nudOverflowHorizon";
            this.nudOverflowHorizon.Size = new System.Drawing.Size(60, 20);
            this.nudOverflowHorizon.TabIndex = 10;
            this.nudOverflowHorizon.Value = new decimal(new int[] { 48, 0, 0, 0 });
            // 
            // lblUnderutilizedStockpile
            // 
            this.lblUnderutilizedStockpile.AutoSize = true;
            this.lblUnderutilizedStockpile.Location = new System.Drawing.Point(15, 50);
            this.lblUnderutilizedStockpile.Name = "lblUnderutilizedStockpile";
            this.lblUnderutilizedStockpile.Size = new System.Drawing.Size(199, 13);
            this.lblUnderutilizedStockpile.Text = "Underutilized refining stockpile hours:";
            // 
            // nudUnderutilizedStockpile
            // 
            this.nudUnderutilizedStockpile.Location = new System.Drawing.Point(220, 47);
            this.nudUnderutilizedStockpile.Maximum = new decimal(new int[] { 168, 0, 0, 0 });
            this.nudUnderutilizedStockpile.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudUnderutilizedStockpile.Name = "nudUnderutilizedStockpile";
            this.nudUnderutilizedStockpile.Size = new System.Drawing.Size(60, 20);
            this.nudUnderutilizedStockpile.TabIndex = 11;
            this.nudUnderutilizedStockpile.Value = new decimal(new int[] { 24, 0, 0, 0 });
            // 
            // grpServerConnection
            // 
            this.grpServerConnection.Controls.Add(this.lblServerUrl);
            this.grpServerConnection.Controls.Add(this.txtServerUrl);
            this.grpServerConnection.Controls.Add(this.btnTestConnection);
            this.grpServerConnection.Controls.Add(this.lblThumbprint);
            this.grpServerConnection.Controls.Add(this.txtThumbprint);
            this.grpServerConnection.Controls.Add(this.lblBearerToken);
            this.grpServerConnection.Controls.Add(this.txtBearerToken);
            this.grpServerConnection.Controls.Add(this.lblOperatingMode);
            this.grpServerConnection.Controls.Add(this.cmbOperatingMode);
            this.grpServerConnection.Controls.Add(this.lblConnectionStatus);
            this.grpServerConnection.Controls.Add(this.btnPushLocalToServer);
            this.grpServerConnection.Location = new System.Drawing.Point(12, 12);
            this.grpServerConnection.Name = "grpServerConnection";
            this.grpServerConnection.Size = new System.Drawing.Size(460, 260);
            this.grpServerConnection.TabIndex = 0;
            this.grpServerConnection.TabStop = false;
            this.grpServerConnection.Text = "Server Connection";
            // 
            // lblServerUrl
            // 
            this.lblServerUrl.AutoSize = true;
            this.lblServerUrl.Location = new System.Drawing.Point(15, 28);
            this.lblServerUrl.Name = "lblServerUrl";
            this.lblServerUrl.Size = new System.Drawing.Size(63, 13);
            this.lblServerUrl.Text = "Server URL:";
            // 
            // txtServerUrl
            // 
            this.txtServerUrl.Location = new System.Drawing.Point(130, 25);
            this.txtServerUrl.Name = "txtServerUrl";
            this.txtServerUrl.Size = new System.Drawing.Size(220, 20);
            this.txtServerUrl.TabIndex = 1;
            // 
            // btnTestConnection
            // 
            this.btnTestConnection.Location = new System.Drawing.Point(360, 23);
            this.btnTestConnection.Name = "btnTestConnection";
            this.btnTestConnection.Size = new System.Drawing.Size(90, 23);
            this.btnTestConnection.TabIndex = 2;
            this.btnTestConnection.Text = "Test Connection";
            this.btnTestConnection.UseVisualStyleBackColor = true;
            // 
            // lblThumbprint
            // 
            this.lblThumbprint.AutoSize = true;
            this.lblThumbprint.Location = new System.Drawing.Point(15, 63);
            this.lblThumbprint.Name = "lblThumbprint";
            this.lblThumbprint.Size = new System.Drawing.Size(109, 13);
            this.lblThumbprint.Text = "Certificate Thumbprint:";
            // 
            // txtThumbprint
            // 
            this.txtThumbprint.Location = new System.Drawing.Point(130, 60);
            this.txtThumbprint.Name = "txtThumbprint";
            this.txtThumbprint.Size = new System.Drawing.Size(320, 20);
            this.txtThumbprint.TabIndex = 3;
            // 
            // lblBearerToken
            // 
            this.lblBearerToken.AutoSize = true;
            this.lblBearerToken.Location = new System.Drawing.Point(15, 98);
            this.lblBearerToken.Name = "lblBearerToken";
            this.lblBearerToken.Size = new System.Drawing.Size(72, 13);
            this.lblBearerToken.Text = "Bearer Token:";
            // 
            // txtBearerToken
            // 
            this.txtBearerToken.Location = new System.Drawing.Point(130, 95);
            this.txtBearerToken.Name = "txtBearerToken";
            this.txtBearerToken.Size = new System.Drawing.Size(320, 20);
            this.txtBearerToken.TabIndex = 4;
            this.txtBearerToken.UseSystemPasswordChar = true;
            // 
            // lblOperatingMode
            // 
            this.lblOperatingMode.AutoSize = true;
            this.lblOperatingMode.Location = new System.Drawing.Point(15, 133);
            this.lblOperatingMode.Name = "lblOperatingMode";
            this.lblOperatingMode.Size = new System.Drawing.Size(88, 13);
            this.lblOperatingMode.Text = "Operating Mode:";
            // 
            // cmbOperatingMode
            // 
            this.cmbOperatingMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbOperatingMode.FormattingEnabled = true;
            this.cmbOperatingMode.Location = new System.Drawing.Point(130, 130);
            this.cmbOperatingMode.Name = "cmbOperatingMode";
            this.cmbOperatingMode.Size = new System.Drawing.Size(180, 21);
            this.cmbOperatingMode.TabIndex = 5;
            // 
            // lblConnectionStatus
            // 
            this.lblConnectionStatus.AutoSize = true;
            this.lblConnectionStatus.Location = new System.Drawing.Point(15, 175);
            this.lblConnectionStatus.Name = "lblConnectionStatus";
            this.lblConnectionStatus.Size = new System.Drawing.Size(0, 13);
            this.lblConnectionStatus.TabIndex = 6;
            // 
            // btnPushLocalToServer
            // 
            this.btnPushLocalToServer.Location = new System.Drawing.Point(15, 210);
            this.btnPushLocalToServer.Name = "btnPushLocalToServer";
            this.btnPushLocalToServer.Size = new System.Drawing.Size(160, 23);
            this.btnPushLocalToServer.TabIndex = 7;
            this.btnPushLocalToServer.Text = "Push Local Data to Server";
            this.btnPushLocalToServer.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(230, 560);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 10;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(311, 560);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 11;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnResetDefaults
            // 
            this.btnResetDefaults.Location = new System.Drawing.Point(411, 560);
            this.btnResetDefaults.Name = "btnResetDefaults";
            this.btnResetDefaults.Size = new System.Drawing.Size(75, 23);
            this.btnResetDefaults.TabIndex = 12;
            this.btnResetDefaults.Text = "Reset";
            this.btnResetDefaults.UseVisualStyleBackColor = true;
            // 
            // FormPreferences
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(498, 595);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnResetDefaults);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormPreferences";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Preferences";
            this.tabControl.ResumeLayout(false);
            this.tabThresholds.ResumeLayout(false);
            this.tabServer.ResumeLayout(false);
            this.tabGameApi.ResumeLayout(false);
            this.grpGameApi.ResumeLayout(false);
            this.grpGameApi.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudPollingInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDetailRefreshHours)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudOverflowHorizon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudUnderutilizedStockpile)).EndInit();
            this.grpStructureCount.ResumeLayout(false);
            this.grpStructureCount.PerformLayout();
            this.grpWorkerRequest.ResumeLayout(false);
            this.grpWorkerRequest.PerformLayout();
            this.grpColonyImport.ResumeLayout(false);
            this.grpColonyImport.PerformLayout();
            this.grpBackgroundProcessing.ResumeLayout(false);
            this.grpBackgroundProcessing.PerformLayout();
            this.grpAdminReport.ResumeLayout(false);
            this.grpAdminReport.PerformLayout();
            this.grpCountdownDisplay.ResumeLayout(false);
            this.grpCountdownDisplay.PerformLayout();
            this.grpWarehouseThresholds.ResumeLayout(false);
            this.grpWarehouseThresholds.PerformLayout();
            this.grpServerConnection.ResumeLayout(false);
            this.grpServerConnection.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabThresholds;
        private System.Windows.Forms.TabPage tabServer;
        private System.Windows.Forms.GroupBox grpStructureCount;
        private System.Windows.Forms.Label lblStructureYellow;
        internal System.Windows.Forms.TextBox txtStructureYellow;
        private System.Windows.Forms.Label lblStructureRed;
        internal System.Windows.Forms.TextBox txtStructureRed;
        private System.Windows.Forms.GroupBox grpWorkerRequest;
        private System.Windows.Forms.Label lblWorkerYellow;
        internal System.Windows.Forms.TextBox txtWorkerYellow;
        private System.Windows.Forms.Label lblWorkerRed;
        internal System.Windows.Forms.TextBox txtWorkerRed;
        private System.Windows.Forms.GroupBox grpColonyImport;
        private System.Windows.Forms.Label lblColonyImportYellow;
        internal System.Windows.Forms.TextBox txtColonyImportYellow;
        private System.Windows.Forms.Label lblColonyImportRed;
        internal System.Windows.Forms.TextBox txtColonyImportRed;
        private System.Windows.Forms.GroupBox grpBackgroundProcessing;
        private System.Windows.Forms.Label lblBackgroundInterval;
        internal System.Windows.Forms.TextBox txtBackgroundInterval;
        private System.Windows.Forms.GroupBox grpAdminReport;
        private System.Windows.Forms.Label lblAdminRefresh;
        internal System.Windows.Forms.TextBox txtAdminRefresh;
        private System.Windows.Forms.GroupBox grpCountdownDisplay;
        private System.Windows.Forms.Label lblCountdownRefresh;
        internal System.Windows.Forms.TextBox txtCountdownRefresh;
        private System.Windows.Forms.GroupBox grpServerConnection;
        private System.Windows.Forms.Label lblServerUrl;
        internal OE2EmpireTracker.Controls.ValidatedTextBox txtServerUrl;
        internal System.Windows.Forms.Button btnTestConnection;
        internal System.Windows.Forms.Button btnPushLocalToServer;
        private System.Windows.Forms.Label lblThumbprint;
        internal OE2EmpireTracker.Controls.ValidatedTextBox txtThumbprint;
        private System.Windows.Forms.Label lblBearerToken;
        internal OE2EmpireTracker.Controls.ValidatedTextBox txtBearerToken;
        private System.Windows.Forms.Label lblOperatingMode;
        internal System.Windows.Forms.ComboBox cmbOperatingMode;
        internal System.Windows.Forms.Label lblConnectionStatus;
        internal System.Windows.Forms.Button btnOK;
        internal System.Windows.Forms.Button btnCancel;
        internal System.Windows.Forms.Button btnResetDefaults;
        private System.Windows.Forms.TabPage tabGameApi;
        private System.Windows.Forms.GroupBox grpGameApi;
        private System.Windows.Forms.Label lblGameApiUrl;
        internal OE2EmpireTracker.Controls.ValidatedTextBox txtGameApiUrl;
        private System.Windows.Forms.Label lblGameApiAppId;
        internal OE2EmpireTracker.Controls.ValidatedTextBox txtGameApiAppId;
        private System.Windows.Forms.Label lblGameApiClientId;
        internal OE2EmpireTracker.Controls.ValidatedTextBox txtGameApiClientId;
        private System.Windows.Forms.Label lblGameApiSecret;
        internal OE2EmpireTracker.Controls.ValidatedTextBox txtGameApiSecret;
        private System.Windows.Forms.Label lblGameApiCharacter;
        internal System.Windows.Forms.ComboBox cmbGameApiCharacter;
        private System.Windows.Forms.Label lblPollingInterval;
        internal System.Windows.Forms.NumericUpDown nudPollingInterval;
        private System.Windows.Forms.Label lblPollingMinutes;
        private System.Windows.Forms.Label lblTpsLimit;
        internal OE2EmpireTracker.Controls.ValidatedTextBox txtTpsLimit;
        private System.Windows.Forms.Label lblDetailRefreshHours;
        internal System.Windows.Forms.NumericUpDown nudDetailRefreshHours;
        internal System.Windows.Forms.CheckBox chkGameApiEnabled;
        internal System.Windows.Forms.Button btnTestGameApiConnection;
        internal System.Windows.Forms.Label lblTestResult;
        private System.Windows.Forms.GroupBox grpWarehouseThresholds;
        private System.Windows.Forms.Label lblOverflowHorizon;
        internal System.Windows.Forms.NumericUpDown nudOverflowHorizon;
        private System.Windows.Forms.Label lblUnderutilizedStockpile;
        internal System.Windows.Forms.NumericUpDown nudUnderutilizedStockpile;
    }
}
