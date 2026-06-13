namespace OE2EmpireTracker.Forms.GameApiStatus
{
    /// <summary>
    /// Designer-generated layout for <see cref="FormGameApiStatus"/>.
    /// </summary>
    partial class FormGameApiStatus
    {
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
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
            this.pnlSummary = new System.Windows.Forms.Panel();
            this.pnlTpsGraph = new System.Windows.Forms.Panel();
            this.pnlMiddle = new System.Windows.Forms.Panel();
            this.pnlBreakdown = new System.Windows.Forms.Panel();
            this.pnlTransfer = new System.Windows.Forms.Panel();
            this.pnlState = new System.Windows.Forms.Panel();
            this.pnlControls = new System.Windows.Forms.Panel();
            this.dgvHistory = new System.Windows.Forms.DataGridView();
            this.colTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMethod = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEndpoint = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDuration = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBytes = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblTps = new System.Windows.Forms.Label();
            this.lblTotalRequests = new System.Windows.Forms.Label();
            this.lblOutstanding = new System.Windows.Forms.Label();
            this.lblPeakOutstanding = new System.Windows.Forms.Label();
            this.lblConnectionState = new System.Windows.Forms.Label();
            this.lblLastSync = new System.Windows.Forms.Label();
            this.lblSuccess = new System.Windows.Forms.Label();
            this.lblClientErrors = new System.Windows.Forms.Label();
            this.lblServerErrors = new System.Windows.Forms.Label();
            this.lblRateLimited = new System.Windows.Forms.Label();
            this.lblExceptions = new System.Windows.Forms.Label();
            this.lblBytesSent = new System.Windows.Forms.Label();
            this.lblBytesReceived = new System.Windows.Forms.Label();
            this.lblCircuitBreaker = new System.Windows.Forms.Label();
            this.lblRateLimit = new System.Windows.Forms.Label();
            this.lblRateLimitState = new System.Windows.Forms.Label();
            this.lblResetTimestamp = new System.Windows.Forms.Label();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnCopyToClipboard = new System.Windows.Forms.Button();
            this.pnlTestApi = new System.Windows.Forms.Panel();
            this.pnlTestApiInput = new System.Windows.Forms.FlowLayoutPanel();
            this.lblEndpoint = new System.Windows.Forms.Label();
            this.cboEndpoint = new System.Windows.Forms.ComboBox();
            this.lblId = new System.Windows.Forms.Label();
            this.txtId = new System.Windows.Forms.TextBox();
            this.lblLocationType = new System.Windows.Forms.Label();
            this.cboLocationType = new System.Windows.Forms.ComboBox();
            this.btnRequest = new System.Windows.Forms.Button();
            this.btnCopyResponse = new System.Windows.Forms.Button();
            this.pnlMarketFields = new System.Windows.Forms.FlowLayoutPanel();
            this.lblView = new System.Windows.Forms.Label();
            this.txtView = new System.Windows.Forms.TextBox();
            this.lblSearch = new System.Windows.Forms.Label();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lblType = new System.Windows.Forms.Label();
            this.txtType = new System.Windows.Forms.TextBox();
            this.lblSubType = new System.Windows.Forms.Label();
            this.txtSubType = new System.Windows.Forms.TextBox();
            this.lblOrderBy = new System.Windows.Forms.Label();
            this.txtOrderBy = new System.Windows.Forms.TextBox();
            this.lblRange = new System.Windows.Forms.Label();
            this.txtRange = new System.Windows.Forms.TextBox();
            this.lblEvolution = new System.Windows.Forms.Label();
            this.txtEvolution = new System.Windows.Forms.TextBox();
            this.lblMarketIds = new System.Windows.Forms.Label();
            this.txtMarketIds = new System.Windows.Forms.TextBox();
            this.txtResponse = new System.Windows.Forms.TextBox();

            this.pnlSummary.SuspendLayout();
            this.pnlMiddle.SuspendLayout();
            this.pnlBreakdown.SuspendLayout();
            this.pnlTransfer.SuspendLayout();
            this.pnlState.SuspendLayout();
            this.pnlControls.SuspendLayout();
            this.pnlTestApi.SuspendLayout();
            this.pnlTestApiInput.SuspendLayout();
            this.pnlMarketFields.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).BeginInit();
            this.SuspendLayout();
            //
            // pnlSummary
            //
            this.pnlSummary.Controls.Add(this.lblTps);
            this.pnlSummary.Controls.Add(this.lblTotalRequests);
            this.pnlSummary.Controls.Add(this.lblOutstanding);
            this.pnlSummary.Controls.Add(this.lblPeakOutstanding);
            this.pnlSummary.Controls.Add(this.lblConnectionState);
            this.pnlSummary.Controls.Add(this.lblLastSync);
            this.pnlSummary.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlSummary.Location = new System.Drawing.Point(0, 0);
            this.pnlSummary.Name = "pnlSummary";
            this.pnlSummary.Padding = new System.Windows.Forms.Padding(4);
            this.pnlSummary.Size = new System.Drawing.Size(784, 60);
            //
            // lblTps
            //
            this.lblTps.AutoSize = true;
            this.lblTps.Location = new System.Drawing.Point(7, 8);
            this.lblTps.Name = "lblTps";
            this.lblTps.Size = new System.Drawing.Size(70, 13);
            this.lblTps.Text = "TPS: 0.00";
            //
            // lblTotalRequests
            //
            this.lblTotalRequests.AutoSize = true;
            this.lblTotalRequests.Location = new System.Drawing.Point(140, 8);
            this.lblTotalRequests.Name = "lblTotalRequests";
            this.lblTotalRequests.Size = new System.Drawing.Size(80, 13);
            this.lblTotalRequests.Text = "Total: 0";
            //
            // lblOutstanding
            //
            this.lblOutstanding.AutoSize = true;
            this.lblOutstanding.Location = new System.Drawing.Point(280, 8);
            this.lblOutstanding.Name = "lblOutstanding";
            this.lblOutstanding.Size = new System.Drawing.Size(100, 13);
            this.lblOutstanding.Text = "Outstanding: 0";
            //
            // lblPeakOutstanding
            //
            this.lblPeakOutstanding.AutoSize = true;
            this.lblPeakOutstanding.Location = new System.Drawing.Point(420, 8);
            this.lblPeakOutstanding.Name = "lblPeakOutstanding";
            this.lblPeakOutstanding.Size = new System.Drawing.Size(100, 13);
            this.lblPeakOutstanding.Text = "Peak: 0";
            //
            // lblConnectionState
            //
            this.lblConnectionState.AutoSize = true;
            this.lblConnectionState.Location = new System.Drawing.Point(7, 32);
            this.lblConnectionState.Name = "lblConnectionState";
            this.lblConnectionState.Size = new System.Drawing.Size(120, 13);
            this.lblConnectionState.Text = "Connection: Unknown";
            //
            // lblLastSync
            //
            this.lblLastSync.AutoSize = true;
            this.lblLastSync.Location = new System.Drawing.Point(280, 32);
            this.lblLastSync.Name = "lblLastSync";
            this.lblLastSync.Size = new System.Drawing.Size(100, 13);
            this.lblLastSync.Text = "Last Sync: N/A";
            //
            // pnlTpsGraph
            //
            this.pnlTpsGraph.BackColor = System.Drawing.Color.White;
            this.pnlTpsGraph.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTpsGraph.Location = new System.Drawing.Point(0, 60);
            this.pnlTpsGraph.Name = "pnlTpsGraph";
            this.pnlTpsGraph.Size = new System.Drawing.Size(784, 120);
            //
            // pnlMiddle
            //
            this.pnlMiddle.Controls.Add(this.pnlState);
            this.pnlMiddle.Controls.Add(this.pnlTransfer);
            this.pnlMiddle.Controls.Add(this.pnlBreakdown);
            this.pnlMiddle.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlMiddle.Location = new System.Drawing.Point(0, 180);
            this.pnlMiddle.Name = "pnlMiddle";
            this.pnlMiddle.Size = new System.Drawing.Size(784, 80);
            //
            // pnlBreakdown
            //
            this.pnlBreakdown.Controls.Add(this.lblSuccess);
            this.pnlBreakdown.Controls.Add(this.lblClientErrors);
            this.pnlBreakdown.Controls.Add(this.lblServerErrors);
            this.pnlBreakdown.Controls.Add(this.lblRateLimited);
            this.pnlBreakdown.Controls.Add(this.lblExceptions);
            this.pnlBreakdown.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlBreakdown.Location = new System.Drawing.Point(0, 0);
            this.pnlBreakdown.Name = "pnlBreakdown";
            this.pnlBreakdown.Padding = new System.Windows.Forms.Padding(4);
            this.pnlBreakdown.Size = new System.Drawing.Size(260, 80);
            //
            // lblSuccess
            //
            this.lblSuccess.AutoSize = true;
            this.lblSuccess.Location = new System.Drawing.Point(7, 8);
            this.lblSuccess.Name = "lblSuccess";
            this.lblSuccess.Size = new System.Drawing.Size(80, 13);
            this.lblSuccess.Text = "2xx: 0";
            //
            // lblClientErrors
            //
            this.lblClientErrors.AutoSize = true;
            this.lblClientErrors.Location = new System.Drawing.Point(7, 24);
            this.lblClientErrors.Name = "lblClientErrors";
            this.lblClientErrors.Size = new System.Drawing.Size(80, 13);
            this.lblClientErrors.Text = "4xx: 0";
            //
            // lblServerErrors
            //
            this.lblServerErrors.AutoSize = true;
            this.lblServerErrors.Location = new System.Drawing.Point(7, 40);
            this.lblServerErrors.Name = "lblServerErrors";
            this.lblServerErrors.Size = new System.Drawing.Size(80, 13);
            this.lblServerErrors.Text = "5xx: 0";
            //
            // lblRateLimited
            //
            this.lblRateLimited.AutoSize = true;
            this.lblRateLimited.Location = new System.Drawing.Point(7, 56);
            this.lblRateLimited.Name = "lblRateLimited";
            this.lblRateLimited.Size = new System.Drawing.Size(80, 13);
            this.lblRateLimited.Text = "429: 0";
            //
            // lblExceptions
            //
            this.lblExceptions.AutoSize = true;
            this.lblExceptions.Location = new System.Drawing.Point(130, 8);
            this.lblExceptions.Name = "lblExceptions";
            this.lblExceptions.Size = new System.Drawing.Size(80, 13);
            this.lblExceptions.Text = "Exceptions: 0";
            //
            // pnlTransfer
            //
            this.pnlTransfer.Controls.Add(this.lblBytesSent);
            this.pnlTransfer.Controls.Add(this.lblBytesReceived);
            this.pnlTransfer.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlTransfer.Location = new System.Drawing.Point(260, 0);
            this.pnlTransfer.Name = "pnlTransfer";
            this.pnlTransfer.Padding = new System.Windows.Forms.Padding(4);
            this.pnlTransfer.Size = new System.Drawing.Size(200, 80);
            //
            // lblBytesSent
            //
            this.lblBytesSent.AutoSize = true;
            this.lblBytesSent.Location = new System.Drawing.Point(7, 8);
            this.lblBytesSent.Name = "lblBytesSent";
            this.lblBytesSent.Size = new System.Drawing.Size(80, 13);
            this.lblBytesSent.Text = "Sent: 0 B";
            //
            // lblBytesReceived
            //
            this.lblBytesReceived.AutoSize = true;
            this.lblBytesReceived.Location = new System.Drawing.Point(7, 24);
            this.lblBytesReceived.Name = "lblBytesReceived";
            this.lblBytesReceived.Size = new System.Drawing.Size(80, 13);
            this.lblBytesReceived.Text = "Received: 0 B";
            //
            // pnlState
            //
            this.pnlState.Controls.Add(this.lblCircuitBreaker);
            this.pnlState.Controls.Add(this.lblRateLimit);
            this.pnlState.Controls.Add(this.lblRateLimitState);
            this.pnlState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlState.Location = new System.Drawing.Point(460, 0);
            this.pnlState.Name = "pnlState";
            this.pnlState.Padding = new System.Windows.Forms.Padding(4);
            this.pnlState.Size = new System.Drawing.Size(324, 80);
            //
            // lblCircuitBreaker
            //
            this.lblCircuitBreaker.AutoSize = true;
            this.lblCircuitBreaker.Location = new System.Drawing.Point(7, 8);
            this.lblCircuitBreaker.Name = "lblCircuitBreaker";
            this.lblCircuitBreaker.Size = new System.Drawing.Size(120, 13);
            this.lblCircuitBreaker.Text = "Circuit Breaker: Closed";
            //
            // lblRateLimit
            //
            this.lblRateLimit.AutoSize = true;
            this.lblRateLimit.Location = new System.Drawing.Point(7, 24);
            this.lblRateLimit.Name = "lblRateLimit";
            this.lblRateLimit.Size = new System.Drawing.Size(120, 13);
            this.lblRateLimit.Text = "Rate Limit: --/min";
            //
            // lblRateLimitState
            //
            this.lblRateLimitState.AutoSize = true;
            this.lblRateLimitState.Location = new System.Drawing.Point(7, 40);
            this.lblRateLimitState.Name = "lblRateLimitState";
            this.lblRateLimitState.Size = new System.Drawing.Size(100, 13);
            this.lblRateLimitState.Text = "Rate Limiter: Active";
            //
            // pnlControls
            //
            this.pnlControls.Controls.Add(this.btnReset);
            this.pnlControls.Controls.Add(this.btnCopyToClipboard);
            this.pnlControls.Controls.Add(this.lblResetTimestamp);
            this.pnlControls.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlControls.Location = new System.Drawing.Point(0, 260);
            this.pnlControls.Name = "pnlControls";
            this.pnlControls.Padding = new System.Windows.Forms.Padding(4);
            this.pnlControls.Size = new System.Drawing.Size(784, 35);
            //
            // btnReset
            //
            this.btnReset.Location = new System.Drawing.Point(7, 6);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(100, 23);
            this.btnReset.Text = "Reset Counters";
            this.btnReset.UseVisualStyleBackColor = true;
            //
            // btnCopyToClipboard
            //
            this.btnCopyToClipboard.Location = new System.Drawing.Point(113, 6);
            this.btnCopyToClipboard.Name = "btnCopyToClipboard";
            this.btnCopyToClipboard.Size = new System.Drawing.Size(110, 23);
            this.btnCopyToClipboard.Text = "Copy to Clipboard";
            this.btnCopyToClipboard.UseVisualStyleBackColor = true;
            //
            // lblResetTimestamp
            //
            this.lblResetTimestamp.AutoSize = true;
            this.lblResetTimestamp.Location = new System.Drawing.Point(235, 11);
            this.lblResetTimestamp.Name = "lblResetTimestamp";
            this.lblResetTimestamp.Size = new System.Drawing.Size(100, 13);
            this.lblResetTimestamp.Text = string.Empty;
            //
            // dgvHistory
            //
            this.dgvHistory.AllowUserToAddRows = false;
            this.dgvHistory.AllowUserToDeleteRows = false;
            this.dgvHistory.AllowUserToOrderColumns = true;
            this.dgvHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[]
            {
                this.colTime,
                this.colMethod,
                this.colEndpoint,
                this.colStatus,
                this.colDuration,
                this.colBytes
            });
            this.dgvHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvHistory.Location = new System.Drawing.Point(0, 295);
            this.dgvHistory.Name = "dgvHistory";
            this.dgvHistory.ReadOnly = true;
            this.dgvHistory.RowHeadersVisible = false;
            this.dgvHistory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvHistory.Size = new System.Drawing.Size(784, 266);
            //
            // colTime
            //
            this.colTime.HeaderText = "Time";
            this.colTime.Name = "colTime";
            this.colTime.ReadOnly = true;
            this.colTime.Width = 90;
            //
            // colMethod
            //
            this.colMethod.HeaderText = "Method";
            this.colMethod.Name = "colMethod";
            this.colMethod.ReadOnly = true;
            this.colMethod.Width = 55;
            //
            // colEndpoint
            //
            this.colEndpoint.HeaderText = "Endpoint";
            this.colEndpoint.Name = "colEndpoint";
            this.colEndpoint.ReadOnly = true;
            this.colEndpoint.Width = 250;
            //
            // colStatus
            //
            this.colStatus.HeaderText = "Status";
            this.colStatus.Name = "colStatus";
            this.colStatus.ReadOnly = true;
            this.colStatus.Width = 55;
            //
            // colDuration
            //
            this.colDuration.HeaderText = "Duration (ms)";
            this.colDuration.Name = "colDuration";
            this.colDuration.ReadOnly = true;
            this.colDuration.Width = 85;
            //
            // colBytes
            //
            this.colBytes.HeaderText = "Bytes";
            this.colBytes.Name = "colBytes";
            this.colBytes.ReadOnly = true;
            this.colBytes.Width = 70;
            //
            // pnlTestApi
            //
            this.pnlTestApi.Controls.Add(this.txtResponse);
            this.pnlTestApi.Controls.Add(this.pnlMarketFields);
            this.pnlTestApi.Controls.Add(this.pnlTestApiInput);
            this.pnlTestApi.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlTestApi.Location = new System.Drawing.Point(0, 361);
            this.pnlTestApi.Name = "pnlTestApi";
            this.pnlTestApi.Size = new System.Drawing.Size(784, 200);
            //
            // pnlTestApiInput
            //
            this.pnlTestApiInput.Controls.Add(this.lblEndpoint);
            this.pnlTestApiInput.Controls.Add(this.cboEndpoint);
            this.pnlTestApiInput.Controls.Add(this.lblId);
            this.pnlTestApiInput.Controls.Add(this.txtId);
            this.pnlTestApiInput.Controls.Add(this.lblLocationType);
            this.pnlTestApiInput.Controls.Add(this.cboLocationType);
            this.pnlTestApiInput.Controls.Add(this.btnRequest);
            this.pnlTestApiInput.Controls.Add(this.btnCopyResponse);
            this.pnlTestApiInput.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTestApiInput.Location = new System.Drawing.Point(0, 0);
            this.pnlTestApiInput.Name = "pnlTestApiInput";
            this.pnlTestApiInput.Padding = new System.Windows.Forms.Padding(4);
            this.pnlTestApiInput.Size = new System.Drawing.Size(784, 32);
            //
            // lblEndpoint
            //
            this.lblEndpoint.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblEndpoint.AutoSize = true;
            this.lblEndpoint.Location = new System.Drawing.Point(7, 9);
            this.lblEndpoint.Name = "lblEndpoint";
            this.lblEndpoint.Size = new System.Drawing.Size(55, 13);
            this.lblEndpoint.Text = "Endpoint:";
            //
            // cboEndpoint
            //
            this.cboEndpoint.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboEndpoint.Location = new System.Drawing.Point(68, 6);
            this.cboEndpoint.Name = "cboEndpoint";
            this.cboEndpoint.Size = new System.Drawing.Size(160, 21);
            //
            // lblId
            //
            this.lblId.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblId.AutoSize = true;
            this.lblId.Location = new System.Drawing.Point(234, 9);
            this.lblId.Name = "lblId";
            this.lblId.Size = new System.Drawing.Size(70, 13);
            this.lblId.Text = "Location ID:";
            //
            // txtId
            //
            this.txtId.Location = new System.Drawing.Point(310, 7);
            this.txtId.Name = "txtId";
            this.txtId.Size = new System.Drawing.Size(80, 20);
            //
            // lblLocationType
            //
            this.lblLocationType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblLocationType.AutoSize = true;
            this.lblLocationType.Location = new System.Drawing.Point(396, 9);
            this.lblLocationType.Name = "lblLocationType";
            this.lblLocationType.Size = new System.Drawing.Size(35, 13);
            this.lblLocationType.Text = "Type:";
            //
            // cboLocationType
            //
            this.cboLocationType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboLocationType.Items.AddRange(new object[] { "Co", "St", "Sh", "Cr" });
            this.cboLocationType.Location = new System.Drawing.Point(437, 6);
            this.cboLocationType.Name = "cboLocationType";
            this.cboLocationType.Size = new System.Drawing.Size(55, 21);
            //
            // btnRequest
            //
            this.btnRequest.Location = new System.Drawing.Point(498, 5);
            this.btnRequest.Name = "btnRequest";
            this.btnRequest.Size = new System.Drawing.Size(65, 23);
            this.btnRequest.Text = "Request";
            this.btnRequest.UseVisualStyleBackColor = true;
            //
            // btnCopyResponse
            //
            this.btnCopyResponse.Location = new System.Drawing.Point(569, 5);
            this.btnCopyResponse.Name = "btnCopyResponse";
            this.btnCopyResponse.Size = new System.Drawing.Size(100, 23);
            this.btnCopyResponse.Text = "Copy Response";
            this.btnCopyResponse.UseVisualStyleBackColor = true;
            //
            // pnlMarketFields
            //
            this.pnlMarketFields.Controls.Add(this.lblView);
            this.pnlMarketFields.Controls.Add(this.txtView);
            this.pnlMarketFields.Controls.Add(this.lblSearch);
            this.pnlMarketFields.Controls.Add(this.txtSearch);
            this.pnlMarketFields.Controls.Add(this.lblType);
            this.pnlMarketFields.Controls.Add(this.txtType);
            this.pnlMarketFields.Controls.Add(this.lblSubType);
            this.pnlMarketFields.Controls.Add(this.txtSubType);
            this.pnlMarketFields.Controls.Add(this.lblOrderBy);
            this.pnlMarketFields.Controls.Add(this.txtOrderBy);
            this.pnlMarketFields.Controls.Add(this.lblRange);
            this.pnlMarketFields.Controls.Add(this.txtRange);
            this.pnlMarketFields.Controls.Add(this.lblEvolution);
            this.pnlMarketFields.Controls.Add(this.txtEvolution);
            this.pnlMarketFields.Controls.Add(this.lblMarketIds);
            this.pnlMarketFields.Controls.Add(this.txtMarketIds);
            this.pnlMarketFields.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlMarketFields.Location = new System.Drawing.Point(0, 32);
            this.pnlMarketFields.Name = "pnlMarketFields";
            this.pnlMarketFields.Padding = new System.Windows.Forms.Padding(4);
            this.pnlMarketFields.Size = new System.Drawing.Size(784, 32);
            this.pnlMarketFields.Visible = false;
            //
            // lblView
            //
            this.lblView.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblView.AutoSize = true;
            this.lblView.Location = new System.Drawing.Point(7, 9);
            this.lblView.Name = "lblView";
            this.lblView.Size = new System.Drawing.Size(35, 13);
            this.lblView.Text = "View:";
            //
            // txtView
            //
            this.txtView.Location = new System.Drawing.Point(48, 7);
            this.txtView.Name = "txtView";
            this.txtView.Size = new System.Drawing.Size(80, 20);
            //
            // lblSearch
            //
            this.lblSearch.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSearch.AutoSize = true;
            this.lblSearch.Location = new System.Drawing.Point(134, 9);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(44, 13);
            this.lblSearch.Text = "Search:";
            //
            // txtSearch
            //
            this.txtSearch.Location = new System.Drawing.Point(184, 7);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(80, 20);
            //
            // lblType
            //
            this.lblType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblType.AutoSize = true;
            this.lblType.Location = new System.Drawing.Point(270, 9);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(34, 13);
            this.lblType.Text = "Type:";
            //
            // txtType
            //
            this.txtType.Location = new System.Drawing.Point(310, 7);
            this.txtType.Name = "txtType";
            this.txtType.Size = new System.Drawing.Size(80, 20);
            //
            // lblSubType
            //
            this.lblSubType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSubType.AutoSize = true;
            this.lblSubType.Location = new System.Drawing.Point(396, 9);
            this.lblSubType.Name = "lblSubType";
            this.lblSubType.Size = new System.Drawing.Size(54, 13);
            this.lblSubType.Text = "Sub Type:";
            //
            // txtSubType
            //
            this.txtSubType.Location = new System.Drawing.Point(456, 7);
            this.txtSubType.Name = "txtSubType";
            this.txtSubType.Size = new System.Drawing.Size(80, 20);
            //
            // lblOrderBy
            //
            this.lblOrderBy.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOrderBy.AutoSize = true;
            this.lblOrderBy.Location = new System.Drawing.Point(542, 9);
            this.lblOrderBy.Name = "lblOrderBy";
            this.lblOrderBy.Size = new System.Drawing.Size(53, 13);
            this.lblOrderBy.Text = "Order By:";
            //
            // txtOrderBy
            //
            this.txtOrderBy.Location = new System.Drawing.Point(601, 7);
            this.txtOrderBy.Name = "txtOrderBy";
            this.txtOrderBy.Size = new System.Drawing.Size(80, 20);
            //
            // lblRange
            //
            this.lblRange.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblRange.AutoSize = true;
            this.lblRange.Location = new System.Drawing.Point(687, 9);
            this.lblRange.Name = "lblRange";
            this.lblRange.Size = new System.Drawing.Size(39, 13);
            this.lblRange.Text = "Range:";
            //
            // txtRange
            //
            this.txtRange.Location = new System.Drawing.Point(732, 7);
            this.txtRange.Name = "txtRange";
            this.txtRange.Size = new System.Drawing.Size(80, 20);
            //
            // lblEvolution
            //
            this.lblEvolution.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblEvolution.AutoSize = true;
            this.lblEvolution.Location = new System.Drawing.Point(818, 9);
            this.lblEvolution.Name = "lblEvolution";
            this.lblEvolution.Size = new System.Drawing.Size(55, 13);
            this.lblEvolution.Text = "Evolution:";
            //
            // txtEvolution
            //
            this.txtEvolution.Location = new System.Drawing.Point(879, 7);
            this.txtEvolution.Name = "txtEvolution";
            this.txtEvolution.Size = new System.Drawing.Size(80, 20);
            //
            // lblMarketIds
            //
            this.lblMarketIds.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblMarketIds.AutoSize = true;
            this.lblMarketIds.Location = new System.Drawing.Point(965, 9);
            this.lblMarketIds.Name = "lblMarketIds";
            this.lblMarketIds.Size = new System.Drawing.Size(63, 13);
            this.lblMarketIds.Text = "Market IDs:";
            //
            // txtMarketIds
            //
            this.txtMarketIds.Location = new System.Drawing.Point(1034, 7);
            this.txtMarketIds.Name = "txtMarketIds";
            this.txtMarketIds.Size = new System.Drawing.Size(150, 20);
            //
            // txtResponse
            //
            this.txtResponse.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtResponse.Location = new System.Drawing.Point(0, 64);
            this.txtResponse.Multiline = true;
            this.txtResponse.Name = "txtResponse";
            this.txtResponse.ReadOnly = true;
            this.txtResponse.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtResponse.Size = new System.Drawing.Size(784, 136);
            this.txtResponse.WordWrap = false;
            //
            // FormGameApiStatus
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.dgvHistory);
            this.Controls.Add(this.pnlTestApi);
            this.Controls.Add(this.pnlControls);
            this.Controls.Add(this.pnlMiddle);
            this.Controls.Add(this.pnlTpsGraph);
            this.Controls.Add(this.pnlSummary);
            this.MinimumSize = new System.Drawing.Size(600, 400);
            this.Name = "FormGameApiStatus";
            this.Text = "Game API Status";
            this.pnlSummary.ResumeLayout(false);
            this.pnlSummary.PerformLayout();
            this.pnlMiddle.ResumeLayout(false);
            this.pnlBreakdown.ResumeLayout(false);
            this.pnlBreakdown.PerformLayout();
            this.pnlTransfer.ResumeLayout(false);
            this.pnlTransfer.PerformLayout();
            this.pnlState.ResumeLayout(false);
            this.pnlState.PerformLayout();
            this.pnlControls.ResumeLayout(false);
            this.pnlControls.PerformLayout();
            this.pnlTestApi.ResumeLayout(false);
            this.pnlTestApi.PerformLayout();
            this.pnlTestApiInput.ResumeLayout(false);
            this.pnlTestApiInput.PerformLayout();
            this.pnlMarketFields.ResumeLayout(false);
            this.pnlMarketFields.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlSummary;
        private System.Windows.Forms.Panel pnlTpsGraph;
        private System.Windows.Forms.Panel pnlMiddle;
        private System.Windows.Forms.Panel pnlBreakdown;
        private System.Windows.Forms.Panel pnlTransfer;
        private System.Windows.Forms.Panel pnlState;
        private System.Windows.Forms.Panel pnlControls;
        private System.Windows.Forms.DataGridView dgvHistory;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMethod;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEndpoint;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDuration;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBytes;
        private System.Windows.Forms.Label lblTps;
        private System.Windows.Forms.Label lblTotalRequests;
        private System.Windows.Forms.Label lblOutstanding;
        private System.Windows.Forms.Label lblPeakOutstanding;
        private System.Windows.Forms.Label lblConnectionState;
        private System.Windows.Forms.Label lblLastSync;
        private System.Windows.Forms.Label lblSuccess;
        private System.Windows.Forms.Label lblClientErrors;
        private System.Windows.Forms.Label lblServerErrors;
        private System.Windows.Forms.Label lblRateLimited;
        private System.Windows.Forms.Label lblExceptions;
        private System.Windows.Forms.Label lblBytesSent;
        private System.Windows.Forms.Label lblBytesReceived;
        private System.Windows.Forms.Label lblCircuitBreaker;
        private System.Windows.Forms.Label lblRateLimit;
        private System.Windows.Forms.Label lblRateLimitState;
        private System.Windows.Forms.Label lblResetTimestamp;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnCopyToClipboard;
        private System.Windows.Forms.Panel pnlTestApi;
        private System.Windows.Forms.FlowLayoutPanel pnlTestApiInput;
        private System.Windows.Forms.Label lblEndpoint;
        private System.Windows.Forms.ComboBox cboEndpoint;
        private System.Windows.Forms.Label lblId;
        private System.Windows.Forms.TextBox txtId;
        private System.Windows.Forms.Label lblLocationType;
        private System.Windows.Forms.ComboBox cboLocationType;
        private System.Windows.Forms.Button btnRequest;
        private System.Windows.Forms.Button btnCopyResponse;
        private System.Windows.Forms.FlowLayoutPanel pnlMarketFields;
        private System.Windows.Forms.Label lblView;
        private System.Windows.Forms.TextBox txtView;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.TextBox txtType;
        private System.Windows.Forms.Label lblSubType;
        private System.Windows.Forms.TextBox txtSubType;
        private System.Windows.Forms.Label lblOrderBy;
        private System.Windows.Forms.TextBox txtOrderBy;
        private System.Windows.Forms.Label lblRange;
        private System.Windows.Forms.TextBox txtRange;
        private System.Windows.Forms.Label lblEvolution;
        private System.Windows.Forms.TextBox txtEvolution;
        private System.Windows.Forms.Label lblMarketIds;
        private System.Windows.Forms.TextBox txtMarketIds;
        private System.Windows.Forms.TextBox txtResponse;
    }
}
