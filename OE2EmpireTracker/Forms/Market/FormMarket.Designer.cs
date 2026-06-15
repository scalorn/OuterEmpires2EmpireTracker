namespace OE2EmpireTracker.Forms.Market
{
    partial class FormMarket
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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabListings = new System.Windows.Forms.TabPage();
            this.tabTransactions = new System.Windows.Forms.TabPage();
            this.tabSummary = new System.Windows.Forms.TabPage();
            this.tabSavedSearches = new System.Windows.Forms.TabPage();
            this.tabMyOrders = new System.Windows.Forms.TabPage();
            this.tabPrices = new System.Windows.Forms.TabPage();
            this.tabAlerts = new System.Windows.Forms.TabPage();

            // Listings tab controls
            this.dgvListings = new System.Windows.Forms.DataGridView();
            this.colListStation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colListItemName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colListType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colListQty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colListPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colListCondition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colListRefs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpListingCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.cmdListingAdd = new System.Windows.Forms.Button();
            this.cmdListingEdit = new System.Windows.Forms.Button();
            this.cmdListingDelete = new System.Windows.Forms.Button();
            this.cmdRecordSale = new System.Windows.Forms.Button();
            this.cmsListings = new System.Windows.Forms.ContextMenuStrip();
            this.tsmiRecordSale = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiEditListing = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiDeleteListing = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsTransactions = new System.Windows.Forms.ContextMenuStrip();
            this.tsmiViewDetails = new System.Windows.Forms.ToolStripMenuItem();

            // Transactions tab controls
            this.flpTxFilters = new System.Windows.Forms.FlowLayoutPanel();
            this.lblTxType = new System.Windows.Forms.Label();
            this.cmbTxType = new System.Windows.Forms.ComboBox();
            this.lblTxItem = new System.Windows.Forms.Label();
            this.txtTxItem = new System.Windows.Forms.TextBox();
            this.lblTxCounterparty = new System.Windows.Forms.Label();
            this.txtTxCounterparty = new System.Windows.Forms.TextBox();
            this.lblTxFaction = new System.Windows.Forms.Label();
            this.txtTxFaction = new System.Windows.Forms.TextBox();
            this.lblTxStation = new System.Windows.Forms.Label();
            this.cmbTxStation = new System.Windows.Forms.ComboBox();
            this.lblTxFrom = new System.Windows.Forms.Label();
            this.dtpTxFrom = new System.Windows.Forms.DateTimePicker();
            this.lblTxTo = new System.Windows.Forms.Label();
            this.dtpTxTo = new System.Windows.Forms.DateTimePicker();
            this.cmdTxApply = new System.Windows.Forms.Button();
            this.dgvTransactions = new System.Windows.Forms.DataGridView();
            this.colTxDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTxType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTxItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTxQty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTxPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTxTotal = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTxCounterparty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTxFaction = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTxStation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTxCondition = new System.Windows.Forms.DataGridViewTextBoxColumn();

            // Summary tab controls
            this.flpSummaryFilters = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSumFrom = new System.Windows.Forms.Label();
            this.dtpSumFrom = new System.Windows.Forms.DateTimePicker();
            this.lblSumTo = new System.Windows.Forms.Label();
            this.dtpSumTo = new System.Windows.Forms.DateTimePicker();
            this.lblSumStation = new System.Windows.Forms.Label();
            this.cmbSumStation = new System.Windows.Forms.ComboBox();
            this.lblPricingPlan = new System.Windows.Forms.Label();
            this.cmbPricingPlan = new System.Windows.Forms.ComboBox();
            this.cmdCompute = new System.Windows.Forms.Button();
            this.flpSummaryTotals = new System.Windows.Forms.FlowLayoutPanel();
            this.lblTotalSales = new System.Windows.Forms.Label();
            this.lblTotalPurchases = new System.Windows.Forms.Label();
            this.lblNetPL = new System.Windows.Forms.Label();
            this.dgvSummary = new System.Windows.Forms.DataGridView();
            this.colSumItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSumQtySold = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSumRevenue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSumQtyBought = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSumCost = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSumNet = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSumPlanValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSumMargin = new System.Windows.Forms.DataGridViewTextBoxColumn();

            // Saved Searches tab controls (10.1)
            this.cmbSearchCharacter = new System.Windows.Forms.ComboBox();
            this.dgvSavedSearches = new System.Windows.Forms.DataGridView();
            this.colSearchName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSearchType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSearchEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.txtSearchName = new System.Windows.Forms.TextBox();
            this.cmbSearchType = new System.Windows.Forms.ComboBox();
            this.cmbSearchItem = new System.Windows.Forms.ComboBox();
            this.cmbSearchPurity = new System.Windows.Forms.ComboBox();
            this.cmbSearchOrderType = new System.Windows.Forms.ComboBox();
            this.chkRunOnSync = new System.Windows.Forms.CheckBox();
            this.cmdTestSearch = new System.Windows.Forms.Button();
            this.cmdSaveSearch = new System.Windows.Forms.Button();
            this.cmdDeleteSearch = new System.Windows.Forms.Button();
            this.dgvTestResults = new System.Windows.Forms.DataGridView();
            this.colTestItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTestPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTestQty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTestSeller = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTestStation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpSearchFilters = new System.Windows.Forms.FlowLayoutPanel();
            this.flpSearchCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSearchCharacter = new System.Windows.Forms.Label();
            this.lblSearchName = new System.Windows.Forms.Label();
            this.lblSearchType = new System.Windows.Forms.Label();
            this.lblSearchItem = new System.Windows.Forms.Label();
            this.lblSearchPurity = new System.Windows.Forms.Label();
            this.lblSearchOrderType = new System.Windows.Forms.Label();

            // My Orders tab controls (10.3)
            this.cmbOrderCharacter = new System.Windows.Forms.ComboBox();
            this.cmbOrderType = new System.Windows.Forms.ComboBox();
            this.txtOrderSearch = new System.Windows.Forms.TextBox();
            this.cmbOrderLocation = new System.Windows.Forms.ComboBox();
            this.dgvSellOrders = new System.Windows.Forms.DataGridView();
            this.colSellItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSellQty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSellPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSellStation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSellStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvBuyOrders = new System.Windows.Forms.DataGridView();
            this.colBuyItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBuyQty = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBuyPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBuyStation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBuyStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpOrderFilters = new System.Windows.Forms.FlowLayoutPanel();
            this.lblOrderCharacter = new System.Windows.Forms.Label();
            this.lblOrderType = new System.Windows.Forms.Label();
            this.lblOrderSearch = new System.Windows.Forms.Label();
            this.lblOrderLocation = new System.Windows.Forms.Label();
            this.lblSellOrders = new System.Windows.Forms.Label();
            this.lblBuyOrders = new System.Windows.Forms.Label();

            // Prices tab controls (10.5)
            this.cmbPriceType = new System.Windows.Forms.ComboBox();
            this.cmbPriceItem = new System.Windows.Forms.ComboBox();
            this.cmbPricePurity = new System.Windows.Forms.ComboBox();
            this.numDaysBack = new System.Windows.Forms.NumericUpDown();
            this.chkBuyOrderPrices = new System.Windows.Forms.CheckBox();
            this.cmdFetchPrices = new System.Windows.Forms.Button();
            this.dgvPriceStats = new System.Windows.Forms.DataGridView();
            this.colPriceItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPriceLow = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPriceAvg = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPriceHigh = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPriceSamples = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPriceFetched = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.cmdAutoPopulate = new System.Windows.Forms.Button();
            this.cmbAutoPopPlan = new System.Windows.Forms.ComboBox();
            this.flpPriceFilters = new System.Windows.Forms.FlowLayoutPanel();
            this.flpPriceCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.lblPriceType = new System.Windows.Forms.Label();
            this.lblPriceItem = new System.Windows.Forms.Label();
            this.lblPricePurity = new System.Windows.Forms.Label();
            this.lblDaysBack = new System.Windows.Forms.Label();
            this.lblAutoPopPlan = new System.Windows.Forms.Label();

            // Alerts tab controls (10.7)
            this.dgvAlerts = new System.Windows.Forms.DataGridView();
            this.colAlertName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAlertType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAlertItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAlertCondition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAlertEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colAlertLastTriggered = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.txtAlertName = new System.Windows.Forms.TextBox();
            this.cmbAlertType = new System.Windows.Forms.ComboBox();
            this.cmbAlertItemType = new System.Windows.Forms.ComboBox();
            this.cmbAlertItem = new System.Windows.Forms.ComboBox();
            this.cmbAlertPurity = new System.Windows.Forms.ComboBox();
            this.cmbPriceCondition = new System.Windows.Forms.ComboBox();
            this.txtPriceThreshold = new System.Windows.Forms.TextBox();
            this.cmbAlertLocation = new System.Windows.Forms.ComboBox();
            this.cmdAddAlert = new System.Windows.Forms.Button();
            this.cmdEditAlert = new System.Windows.Forms.Button();
            this.cmdDeleteAlert = new System.Windows.Forms.Button();
            this.chkAlertEnabled = new System.Windows.Forms.CheckBox();
            this.flpAlertDetail = new System.Windows.Forms.FlowLayoutPanel();
            this.flpAlertCommands = new System.Windows.Forms.FlowLayoutPanel();
            this.lblAlertName = new System.Windows.Forms.Label();
            this.lblAlertType = new System.Windows.Forms.Label();
            this.lblAlertItemType = new System.Windows.Forms.Label();
            this.lblAlertItem = new System.Windows.Forms.Label();
            this.lblAlertPurity = new System.Windows.Forms.Label();
            this.lblPriceConditionLabel = new System.Windows.Forms.Label();
            this.lblPriceThreshold = new System.Windows.Forms.Label();
            this.lblAlertLocation = new System.Windows.Forms.Label();

            // Scope status (10.12) and stale indicator (10.13)
            this.lblScopeStatus = new System.Windows.Forms.Label();
            this.lblStaleWarning = new System.Windows.Forms.Label();

            this.tabControl.SuspendLayout();
            this.tabListings.SuspendLayout();
            this.tabTransactions.SuspendLayout();
            this.tabSummary.SuspendLayout();
            this.tabSavedSearches.SuspendLayout();
            this.tabMyOrders.SuspendLayout();
            this.tabPrices.SuspendLayout();
            this.tabAlerts.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvListings)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTransactions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSummary)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSavedSearches)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTestResults)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSellOrders)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBuyOrders)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPriceStats)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAlerts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDaysBack)).BeginInit();
            this.SuspendLayout();

            // tabControl
            this.tabControl.Controls.Add(this.tabListings);
            this.tabControl.Controls.Add(this.tabTransactions);
            this.tabControl.Controls.Add(this.tabSummary);
            this.tabControl.Controls.Add(this.tabSavedSearches);
            this.tabControl.Controls.Add(this.tabMyOrders);
            this.tabControl.Controls.Add(this.tabPrices);
            this.tabControl.Controls.Add(this.tabAlerts);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(950, 620);

            // tabListings
            this.tabListings.Controls.Add(this.dgvListings);
            this.tabListings.Controls.Add(this.flpListingCommands);
            this.tabListings.Location = new System.Drawing.Point(4, 22);
            this.tabListings.Name = "tabListings";
            this.tabListings.Padding = new System.Windows.Forms.Padding(3);
            this.tabListings.Size = new System.Drawing.Size(942, 594);
            this.tabListings.TabIndex = 0;
            this.tabListings.Text = "Listings";
            this.tabListings.UseVisualStyleBackColor = true;

            // dgvListings
            this.dgvListings.AllowUserToAddRows = false;
            this.dgvListings.AllowUserToDeleteRows = false;
            this.dgvListings.AllowUserToOrderColumns = true;
            this.dgvListings.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvListings.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colListStation, this.colListItemName, this.colListType, this.colListQty,
                this.colListPrice, this.colListCondition, this.colListRefs});
            this.dgvListings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvListings.Name = "dgvListings";
            this.dgvListings.ReadOnly = true;
            this.dgvListings.ContextMenuStrip = this.cmsListings;
            this.dgvListings.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;

            this.colListStation.HeaderText = "Station";
            this.colListStation.Name = "colListStation";
            this.colListStation.ReadOnly = true;
            this.colListStation.Width = 120;
            this.colListItemName.HeaderText = "Item Name";
            this.colListItemName.Name = "colListItemName";
            this.colListItemName.ReadOnly = true;
            this.colListItemName.Width = 180;
            this.colListType.HeaderText = "Type";
            this.colListType.Name = "colListType";
            this.colListType.ReadOnly = true;
            this.colListType.Width = 80;
            this.colListQty.HeaderText = "Quantity";
            this.colListQty.Name = "colListQty";
            this.colListQty.ReadOnly = true;
            this.colListQty.Width = 70;
            this.colListPrice.HeaderText = "Price/Unit";
            this.colListPrice.Name = "colListPrice";
            this.colListPrice.ReadOnly = true;
            this.colListPrice.Width = 90;
            this.colListCondition.HeaderText = "Condition";
            this.colListCondition.Name = "colListCondition";
            this.colListCondition.ReadOnly = true;
            this.colListCondition.Width = 90;
            this.colListRefs.HeaderText = "Refs";
            this.colListRefs.Name = "colListRefs";
            this.colListRefs.ReadOnly = true;
            this.colListRefs.Width = 50;

            // flpListingCommands
            this.flpListingCommands.AutoSize = true;
            this.flpListingCommands.Controls.Add(this.cmdListingAdd);
            this.flpListingCommands.Controls.Add(this.cmdListingEdit);
            this.flpListingCommands.Controls.Add(this.cmdListingDelete);
            this.flpListingCommands.Controls.Add(this.cmdRecordSale);
            this.flpListingCommands.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flpListingCommands.Name = "flpListingCommands";
            this.flpListingCommands.Size = new System.Drawing.Size(936, 29);

            this.cmdListingAdd.Name = "cmdListingAdd";
            this.cmdListingAdd.Size = new System.Drawing.Size(55, 23);
            this.cmdListingAdd.Text = "Add";
            this.cmdListingAdd.UseVisualStyleBackColor = true;
            this.cmdListingEdit.Name = "cmdListingEdit";
            this.cmdListingEdit.Size = new System.Drawing.Size(55, 23);
            this.cmdListingEdit.Text = "Edit";
            this.cmdListingEdit.UseVisualStyleBackColor = true;
            this.cmdListingDelete.Name = "cmdListingDelete";
            this.cmdListingDelete.Size = new System.Drawing.Size(55, 23);
            this.cmdListingDelete.Text = "Delete";
            this.cmdListingDelete.UseVisualStyleBackColor = true;
            this.cmdRecordSale.Name = "cmdRecordSale";
            this.cmdRecordSale.Size = new System.Drawing.Size(85, 23);
            this.cmdRecordSale.Text = "Record Sale";
            this.cmdRecordSale.UseVisualStyleBackColor = true;

            // cmsListings
            this.cmsListings.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.tsmiRecordSale, this.tsmiEditListing, this.tsmiDeleteListing });
            this.cmsListings.Name = "cmsListings";
            this.cmsListings.Size = new System.Drawing.Size(155, 70);
            this.tsmiRecordSale.Name = "tsmiRecordSale";
            this.tsmiRecordSale.Size = new System.Drawing.Size(154, 22);
            this.tsmiRecordSale.Text = "Record Sale";
            this.tsmiEditListing.Name = "tsmiEditListing";
            this.tsmiEditListing.Size = new System.Drawing.Size(154, 22);
            this.tsmiEditListing.Text = "Edit Listing";
            this.tsmiDeleteListing.Name = "tsmiDeleteListing";
            this.tsmiDeleteListing.Size = new System.Drawing.Size(154, 22);
            this.tsmiDeleteListing.Text = "Delete Listing";

            // cmsTransactions
            this.cmsTransactions.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.tsmiViewDetails });
            this.cmsTransactions.Name = "cmsTransactions";
            this.cmsTransactions.Size = new System.Drawing.Size(135, 26);
            this.tsmiViewDetails.Name = "tsmiViewDetails";
            this.tsmiViewDetails.Size = new System.Drawing.Size(134, 22);
            this.tsmiViewDetails.Text = "View Details";

            // tabTransactions
            this.tabTransactions.Controls.Add(this.dgvTransactions);
            this.tabTransactions.Controls.Add(this.flpTxFilters);
            this.tabTransactions.Location = new System.Drawing.Point(4, 22);
            this.tabTransactions.Name = "tabTransactions";
            this.tabTransactions.Padding = new System.Windows.Forms.Padding(3);
            this.tabTransactions.Size = new System.Drawing.Size(942, 594);
            this.tabTransactions.TabIndex = 1;
            this.tabTransactions.Text = "Transactions";
            this.tabTransactions.UseVisualStyleBackColor = true;

            // flpTxFilters
            this.flpTxFilters.AutoSize = true;
            this.flpTxFilters.Controls.Add(this.lblTxType);
            this.flpTxFilters.Controls.Add(this.cmbTxType);
            this.flpTxFilters.Controls.Add(this.lblTxItem);
            this.flpTxFilters.Controls.Add(this.txtTxItem);
            this.flpTxFilters.Controls.Add(this.lblTxCounterparty);
            this.flpTxFilters.Controls.Add(this.txtTxCounterparty);
            this.flpTxFilters.Controls.Add(this.lblTxFaction);
            this.flpTxFilters.Controls.Add(this.txtTxFaction);
            this.flpTxFilters.Controls.Add(this.lblTxStation);
            this.flpTxFilters.Controls.Add(this.cmbTxStation);
            this.flpTxFilters.Controls.Add(this.lblTxFrom);
            this.flpTxFilters.Controls.Add(this.dtpTxFrom);
            this.flpTxFilters.Controls.Add(this.lblTxTo);
            this.flpTxFilters.Controls.Add(this.dtpTxTo);
            this.flpTxFilters.Controls.Add(this.cmdTxApply);
            this.flpTxFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.flpTxFilters.Name = "flpTxFilters";
            this.flpTxFilters.Size = new System.Drawing.Size(936, 56);
            this.flpTxFilters.WrapContents = true;

            this.lblTxType.AutoSize = true;
            this.lblTxType.Text = "Type:";
            this.lblTxType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblTxType.Name = "lblTxType";
            this.cmbTxType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTxType.Size = new System.Drawing.Size(80, 21);
            this.cmbTxType.Name = "cmbTxType";
            this.lblTxItem.AutoSize = true;
            this.lblTxItem.Text = "Item:";
            this.lblTxItem.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblTxItem.Name = "lblTxItem";
            this.txtTxItem.Size = new System.Drawing.Size(100, 20);
            this.txtTxItem.Name = "txtTxItem";
            this.lblTxCounterparty.AutoSize = true;
            this.lblTxCounterparty.Text = "Counterparty:";
            this.lblTxCounterparty.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblTxCounterparty.Name = "lblTxCounterparty";
            this.txtTxCounterparty.Size = new System.Drawing.Size(100, 20);
            this.txtTxCounterparty.Name = "txtTxCounterparty";
            this.lblTxFaction.AutoSize = true;
            this.lblTxFaction.Text = "Faction:";
            this.lblTxFaction.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblTxFaction.Name = "lblTxFaction";
            this.txtTxFaction.Size = new System.Drawing.Size(80, 20);
            this.txtTxFaction.Name = "txtTxFaction";
            this.lblTxStation.AutoSize = true;
            this.lblTxStation.Text = "Station:";
            this.lblTxStation.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblTxStation.Name = "lblTxStation";
            this.cmbTxStation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTxStation.Size = new System.Drawing.Size(120, 21);
            this.cmbTxStation.Name = "cmbTxStation";
            this.lblTxFrom.AutoSize = true;
            this.lblTxFrom.Text = "From:";
            this.lblTxFrom.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblTxFrom.Name = "lblTxFrom";
            this.dtpTxFrom.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpTxFrom.Size = new System.Drawing.Size(100, 20);
            this.dtpTxFrom.Name = "dtpTxFrom";
            this.dtpTxFrom.Checked = false;
            this.dtpTxFrom.ShowCheckBox = true;
            this.lblTxTo.AutoSize = true;
            this.lblTxTo.Text = "To:";
            this.lblTxTo.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblTxTo.Name = "lblTxTo";
            this.dtpTxTo.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpTxTo.Size = new System.Drawing.Size(100, 20);
            this.dtpTxTo.Name = "dtpTxTo";
            this.dtpTxTo.Checked = false;
            this.dtpTxTo.ShowCheckBox = true;
            this.cmdTxApply.Name = "cmdTxApply";
            this.cmdTxApply.Size = new System.Drawing.Size(55, 23);
            this.cmdTxApply.Text = "Apply";
            this.cmdTxApply.UseVisualStyleBackColor = true;

            // dgvTransactions
            this.dgvTransactions.AllowUserToAddRows = false;
            this.dgvTransactions.AllowUserToDeleteRows = false;
            this.dgvTransactions.AllowUserToOrderColumns = true;
            this.dgvTransactions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTransactions.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colTxDate, this.colTxType, this.colTxItem, this.colTxQty,
                this.colTxPrice, this.colTxTotal, this.colTxCounterparty,
                this.colTxFaction, this.colTxStation, this.colTxCondition});
            this.dgvTransactions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvTransactions.Name = "dgvTransactions";
            this.dgvTransactions.ReadOnly = true;
            this.dgvTransactions.ContextMenuStrip = this.cmsTransactions;
            this.dgvTransactions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;

            this.colTxDate.HeaderText = "Date";
            this.colTxDate.Name = "colTxDate";
            this.colTxDate.ReadOnly = true;
            this.colTxDate.Width = 80;
            this.colTxType.HeaderText = "Type";
            this.colTxType.Name = "colTxType";
            this.colTxType.ReadOnly = true;
            this.colTxType.Width = 60;
            this.colTxItem.HeaderText = "Item";
            this.colTxItem.Name = "colTxItem";
            this.colTxItem.ReadOnly = true;
            this.colTxItem.Width = 150;
            this.colTxQty.HeaderText = "Qty";
            this.colTxQty.Name = "colTxQty";
            this.colTxQty.ReadOnly = true;
            this.colTxQty.Width = 60;
            this.colTxPrice.HeaderText = "Price";
            this.colTxPrice.Name = "colTxPrice";
            this.colTxPrice.ReadOnly = true;
            this.colTxPrice.Width = 80;
            this.colTxTotal.HeaderText = "Total";
            this.colTxTotal.Name = "colTxTotal";
            this.colTxTotal.ReadOnly = true;
            this.colTxTotal.Width = 90;
            this.colTxCounterparty.HeaderText = "Counterparty";
            this.colTxCounterparty.Name = "colTxCounterparty";
            this.colTxCounterparty.ReadOnly = true;
            this.colTxCounterparty.Width = 100;
            this.colTxFaction.HeaderText = "Faction";
            this.colTxFaction.Name = "colTxFaction";
            this.colTxFaction.ReadOnly = true;
            this.colTxFaction.Width = 70;
            this.colTxStation.HeaderText = "Station";
            this.colTxStation.Name = "colTxStation";
            this.colTxStation.ReadOnly = true;
            this.colTxStation.Width = 100;
            this.colTxCondition.HeaderText = "Condition";
            this.colTxCondition.Name = "colTxCondition";
            this.colTxCondition.ReadOnly = true;
            this.colTxCondition.Width = 80;

            // tabSummary
            this.tabSummary.Controls.Add(this.dgvSummary);
            this.tabSummary.Controls.Add(this.flpSummaryTotals);
            this.tabSummary.Controls.Add(this.flpSummaryFilters);
            this.tabSummary.Location = new System.Drawing.Point(4, 22);
            this.tabSummary.Name = "tabSummary";
            this.tabSummary.Padding = new System.Windows.Forms.Padding(3);
            this.tabSummary.Size = new System.Drawing.Size(942, 594);
            this.tabSummary.TabIndex = 2;
            this.tabSummary.Text = "Summary";
            this.tabSummary.UseVisualStyleBackColor = true;

            // flpSummaryFilters
            this.flpSummaryFilters.AutoSize = true;
            this.flpSummaryFilters.Controls.Add(this.lblSumFrom);
            this.flpSummaryFilters.Controls.Add(this.dtpSumFrom);
            this.flpSummaryFilters.Controls.Add(this.lblSumTo);
            this.flpSummaryFilters.Controls.Add(this.dtpSumTo);
            this.flpSummaryFilters.Controls.Add(this.lblSumStation);
            this.flpSummaryFilters.Controls.Add(this.cmbSumStation);
            this.flpSummaryFilters.Controls.Add(this.lblPricingPlan);
            this.flpSummaryFilters.Controls.Add(this.cmbPricingPlan);
            this.flpSummaryFilters.Controls.Add(this.cmdCompute);
            this.flpSummaryFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.flpSummaryFilters.Name = "flpSummaryFilters";
            this.flpSummaryFilters.Size = new System.Drawing.Size(936, 30);
            this.flpSummaryFilters.WrapContents = true;

            this.lblSumFrom.AutoSize = true;
            this.lblSumFrom.Text = "From:";
            this.lblSumFrom.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSumFrom.Name = "lblSumFrom";
            this.dtpSumFrom.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpSumFrom.Size = new System.Drawing.Size(100, 20);
            this.dtpSumFrom.Name = "dtpSumFrom";
            this.dtpSumFrom.ShowCheckBox = true;
            this.dtpSumFrom.Checked = false;
            this.lblSumTo.AutoSize = true;
            this.lblSumTo.Text = "To:";
            this.lblSumTo.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSumTo.Name = "lblSumTo";
            this.dtpSumTo.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpSumTo.Size = new System.Drawing.Size(100, 20);
            this.dtpSumTo.Name = "dtpSumTo";
            this.dtpSumTo.ShowCheckBox = true;
            this.dtpSumTo.Checked = false;
            this.lblSumStation.AutoSize = true;
            this.lblSumStation.Text = "Station:";
            this.lblSumStation.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSumStation.Name = "lblSumStation";
            this.cmbSumStation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSumStation.Size = new System.Drawing.Size(120, 21);
            this.cmbSumStation.Name = "cmbSumStation";
            this.lblPricingPlan.AutoSize = true;
            this.lblPricingPlan.Text = "Pricing Plan:";
            this.lblPricingPlan.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblPricingPlan.Name = "lblPricingPlan";
            this.cmbPricingPlan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPricingPlan.Size = new System.Drawing.Size(120, 21);
            this.cmbPricingPlan.Name = "cmbPricingPlan";
            this.cmdCompute.Name = "cmdCompute";
            this.cmdCompute.Size = new System.Drawing.Size(70, 23);
            this.cmdCompute.Text = "Compute";
            this.cmdCompute.UseVisualStyleBackColor = true;

            // flpSummaryTotals
            this.flpSummaryTotals.AutoSize = true;
            this.flpSummaryTotals.Controls.Add(this.lblTotalSales);
            this.flpSummaryTotals.Controls.Add(this.lblTotalPurchases);
            this.flpSummaryTotals.Controls.Add(this.lblNetPL);
            this.flpSummaryTotals.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flpSummaryTotals.Name = "flpSummaryTotals";
            this.flpSummaryTotals.Size = new System.Drawing.Size(936, 25);

            this.lblTotalSales.AutoSize = true;
            this.lblTotalSales.Text = "Total Sales: --";
            this.lblTotalSales.Name = "lblTotalSales";
            this.lblTotalPurchases.AutoSize = true;
            this.lblTotalPurchases.Text = "Total Purchases: --";
            this.lblTotalPurchases.Name = "lblTotalPurchases";
            this.lblNetPL.AutoSize = true;
            this.lblNetPL.Text = "Net P/L: --";
            this.lblNetPL.Name = "lblNetPL";

            // dgvSummary
            this.dgvSummary.AllowUserToAddRows = false;
            this.dgvSummary.AllowUserToDeleteRows = false;
            this.dgvSummary.AllowUserToOrderColumns = true;
            this.dgvSummary.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSummary.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colSumItem, this.colSumQtySold, this.colSumRevenue,
                this.colSumQtyBought, this.colSumCost, this.colSumNet,
                this.colSumPlanValue, this.colSumMargin});
            this.dgvSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvSummary.Name = "dgvSummary";
            this.dgvSummary.ReadOnly = true;
            this.dgvSummary.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;

            this.colSumItem.HeaderText = "Item";
            this.colSumItem.Name = "colSumItem";
            this.colSumItem.ReadOnly = true;
            this.colSumItem.Width = 150;
            this.colSumQtySold.HeaderText = "Qty Sold";
            this.colSumQtySold.Name = "colSumQtySold";
            this.colSumQtySold.ReadOnly = true;
            this.colSumQtySold.Width = 70;
            this.colSumRevenue.HeaderText = "Revenue";
            this.colSumRevenue.Name = "colSumRevenue";
            this.colSumRevenue.ReadOnly = true;
            this.colSumRevenue.Width = 90;
            this.colSumQtyBought.HeaderText = "Qty Bought";
            this.colSumQtyBought.Name = "colSumQtyBought";
            this.colSumQtyBought.ReadOnly = true;
            this.colSumQtyBought.Width = 80;
            this.colSumCost.HeaderText = "Cost";
            this.colSumCost.Name = "colSumCost";
            this.colSumCost.ReadOnly = true;
            this.colSumCost.Width = 90;
            this.colSumNet.HeaderText = "Net";
            this.colSumNet.Name = "colSumNet";
            this.colSumNet.ReadOnly = true;
            this.colSumNet.Width = 90;
            this.colSumPlanValue.HeaderText = "Plan Value";
            this.colSumPlanValue.Name = "colSumPlanValue";
            this.colSumPlanValue.ReadOnly = true;
            this.colSumPlanValue.Width = 90;
            this.colSumMargin.HeaderText = "Margin";
            this.colSumMargin.Name = "colSumMargin";
            this.colSumMargin.ReadOnly = true;
            this.colSumMargin.Width = 70;

            // tabSavedSearches
            this.tabSavedSearches.Controls.Add(this.dgvTestResults);
            this.tabSavedSearches.Controls.Add(this.dgvSavedSearches);
            this.tabSavedSearches.Controls.Add(this.flpSearchCommands);
            this.tabSavedSearches.Controls.Add(this.flpSearchFilters);
            this.tabSavedSearches.Location = new System.Drawing.Point(4, 22);
            this.tabSavedSearches.Name = "tabSavedSearches";
            this.tabSavedSearches.Padding = new System.Windows.Forms.Padding(3);
            this.tabSavedSearches.Size = new System.Drawing.Size(942, 594);
            this.tabSavedSearches.TabIndex = 3;
            this.tabSavedSearches.Text = "Saved Searches";
            this.tabSavedSearches.UseVisualStyleBackColor = true;

            // flpSearchFilters
            this.flpSearchFilters.AutoSize = true;
            this.flpSearchFilters.Controls.Add(this.lblSearchCharacter);
            this.flpSearchFilters.Controls.Add(this.cmbSearchCharacter);
            this.flpSearchFilters.Controls.Add(this.lblSearchName);
            this.flpSearchFilters.Controls.Add(this.txtSearchName);
            this.flpSearchFilters.Controls.Add(this.lblSearchType);
            this.flpSearchFilters.Controls.Add(this.cmbSearchType);
            this.flpSearchFilters.Controls.Add(this.lblSearchItem);
            this.flpSearchFilters.Controls.Add(this.cmbSearchItem);
            this.flpSearchFilters.Controls.Add(this.lblSearchPurity);
            this.flpSearchFilters.Controls.Add(this.cmbSearchPurity);
            this.flpSearchFilters.Controls.Add(this.lblSearchOrderType);
            this.flpSearchFilters.Controls.Add(this.cmbSearchOrderType);
            this.flpSearchFilters.Controls.Add(this.chkRunOnSync);
            this.flpSearchFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.flpSearchFilters.Name = "flpSearchFilters";
            this.flpSearchFilters.Size = new System.Drawing.Size(936, 56);
            this.flpSearchFilters.WrapContents = true;

            this.lblSearchCharacter.AutoSize = true;
            this.lblSearchCharacter.Text = "Character:";
            this.lblSearchCharacter.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSearchCharacter.Name = "lblSearchCharacter";
            this.cmbSearchCharacter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSearchCharacter.Size = new System.Drawing.Size(130, 21);
            this.cmbSearchCharacter.Name = "cmbSearchCharacter";
            this.lblSearchName.AutoSize = true;
            this.lblSearchName.Text = "Name:";
            this.lblSearchName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSearchName.Name = "lblSearchName";
            this.txtSearchName.Size = new System.Drawing.Size(120, 20);
            this.txtSearchName.Name = "txtSearchName";
            this.lblSearchType.AutoSize = true;
            this.lblSearchType.Text = "Type:";
            this.lblSearchType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSearchType.Name = "lblSearchType";
            this.cmbSearchType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSearchType.Size = new System.Drawing.Size(100, 21);
            this.cmbSearchType.Name = "cmbSearchType";
            this.lblSearchItem.AutoSize = true;
            this.lblSearchItem.Text = "Item:";
            this.lblSearchItem.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSearchItem.Name = "lblSearchItem";
            this.cmbSearchItem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSearchItem.Size = new System.Drawing.Size(130, 21);
            this.cmbSearchItem.Name = "cmbSearchItem";
            this.lblSearchPurity.AutoSize = true;
            this.lblSearchPurity.Text = "Purity:";
            this.lblSearchPurity.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSearchPurity.Name = "lblSearchPurity";
            this.cmbSearchPurity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSearchPurity.Size = new System.Drawing.Size(80, 21);
            this.cmbSearchPurity.Name = "cmbSearchPurity";
            this.lblSearchOrderType.AutoSize = true;
            this.lblSearchOrderType.Text = "Order Type:";
            this.lblSearchOrderType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSearchOrderType.Name = "lblSearchOrderType";
            this.cmbSearchOrderType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSearchOrderType.Size = new System.Drawing.Size(80, 21);
            this.cmbSearchOrderType.Name = "cmbSearchOrderType";
            this.chkRunOnSync.AutoSize = true;
            this.chkRunOnSync.Text = "Run on Sync";
            this.chkRunOnSync.Name = "chkRunOnSync";

            // flpSearchCommands
            this.flpSearchCommands.AutoSize = true;
            this.flpSearchCommands.Controls.Add(this.cmdTestSearch);
            this.flpSearchCommands.Controls.Add(this.cmdSaveSearch);
            this.flpSearchCommands.Controls.Add(this.cmdDeleteSearch);
            this.flpSearchCommands.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flpSearchCommands.Name = "flpSearchCommands";
            this.flpSearchCommands.Size = new System.Drawing.Size(936, 29);

            this.cmdTestSearch.Name = "cmdTestSearch";
            this.cmdTestSearch.Size = new System.Drawing.Size(85, 23);
            this.cmdTestSearch.Text = "Test Search";
            this.cmdTestSearch.UseVisualStyleBackColor = true;
            this.cmdSaveSearch.Name = "cmdSaveSearch";
            this.cmdSaveSearch.Size = new System.Drawing.Size(55, 23);
            this.cmdSaveSearch.Text = "Save";
            this.cmdSaveSearch.UseVisualStyleBackColor = true;
            this.cmdDeleteSearch.Name = "cmdDeleteSearch";
            this.cmdDeleteSearch.Size = new System.Drawing.Size(55, 23);
            this.cmdDeleteSearch.Text = "Delete";
            this.cmdDeleteSearch.UseVisualStyleBackColor = true;

            // dgvSavedSearches
            this.dgvSavedSearches.AllowUserToAddRows = false;
            this.dgvSavedSearches.AllowUserToDeleteRows = false;
            this.dgvSavedSearches.AllowUserToOrderColumns = true;
            this.dgvSavedSearches.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSavedSearches.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colSearchName, this.colSearchType, this.colSearchEnabled});
            this.dgvSavedSearches.Dock = System.Windows.Forms.DockStyle.Top;
            this.dgvSavedSearches.Name = "dgvSavedSearches";
            this.dgvSavedSearches.ReadOnly = true;
            this.dgvSavedSearches.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvSavedSearches.Size = new System.Drawing.Size(936, 200);

            this.colSearchName.HeaderText = "Name";
            this.colSearchName.Name = "colSearchName";
            this.colSearchName.ReadOnly = true;
            this.colSearchName.Width = 200;
            this.colSearchType.HeaderText = "Type";
            this.colSearchType.Name = "colSearchType";
            this.colSearchType.ReadOnly = true;
            this.colSearchType.Width = 100;
            this.colSearchEnabled.HeaderText = "Enabled";
            this.colSearchEnabled.Name = "colSearchEnabled";
            this.colSearchEnabled.ReadOnly = true;
            this.colSearchEnabled.Width = 60;

            // dgvTestResults
            this.dgvTestResults.AllowUserToAddRows = false;
            this.dgvTestResults.AllowUserToDeleteRows = false;
            this.dgvTestResults.AllowUserToOrderColumns = true;
            this.dgvTestResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTestResults.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colTestItem, this.colTestPrice, this.colTestQty,
                this.colTestSeller, this.colTestStation});
            this.dgvTestResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvTestResults.Name = "dgvTestResults";
            this.dgvTestResults.ReadOnly = true;
            this.dgvTestResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;

            this.colTestItem.HeaderText = "Item";
            this.colTestItem.Name = "colTestItem";
            this.colTestItem.ReadOnly = true;
            this.colTestItem.Width = 180;
            this.colTestPrice.HeaderText = "Price";
            this.colTestPrice.Name = "colTestPrice";
            this.colTestPrice.ReadOnly = true;
            this.colTestPrice.Width = 90;
            this.colTestQty.HeaderText = "Qty";
            this.colTestQty.Name = "colTestQty";
            this.colTestQty.ReadOnly = true;
            this.colTestQty.Width = 70;
            this.colTestSeller.HeaderText = "Seller";
            this.colTestSeller.Name = "colTestSeller";
            this.colTestSeller.ReadOnly = true;
            this.colTestSeller.Width = 120;
            this.colTestStation.HeaderText = "Station";
            this.colTestStation.Name = "colTestStation";
            this.colTestStation.ReadOnly = true;
            this.colTestStation.Width = 120;

            // tabMyOrders
            this.tabMyOrders.Controls.Add(this.dgvBuyOrders);
            this.tabMyOrders.Controls.Add(this.lblBuyOrders);
            this.tabMyOrders.Controls.Add(this.dgvSellOrders);
            this.tabMyOrders.Controls.Add(this.lblSellOrders);
            this.tabMyOrders.Controls.Add(this.flpOrderFilters);
            this.tabMyOrders.Location = new System.Drawing.Point(4, 22);
            this.tabMyOrders.Name = "tabMyOrders";
            this.tabMyOrders.Padding = new System.Windows.Forms.Padding(3);
            this.tabMyOrders.Size = new System.Drawing.Size(942, 594);
            this.tabMyOrders.TabIndex = 4;
            this.tabMyOrders.Text = "My Orders";
            this.tabMyOrders.UseVisualStyleBackColor = true;

            // flpOrderFilters
            this.flpOrderFilters.AutoSize = true;
            this.flpOrderFilters.Controls.Add(this.lblOrderCharacter);
            this.flpOrderFilters.Controls.Add(this.cmbOrderCharacter);
            this.flpOrderFilters.Controls.Add(this.lblOrderType);
            this.flpOrderFilters.Controls.Add(this.cmbOrderType);
            this.flpOrderFilters.Controls.Add(this.lblOrderSearch);
            this.flpOrderFilters.Controls.Add(this.txtOrderSearch);
            this.flpOrderFilters.Controls.Add(this.lblOrderLocation);
            this.flpOrderFilters.Controls.Add(this.cmbOrderLocation);
            this.flpOrderFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.flpOrderFilters.Name = "flpOrderFilters";
            this.flpOrderFilters.Size = new System.Drawing.Size(936, 30);
            this.flpOrderFilters.WrapContents = true;

            this.lblOrderCharacter.AutoSize = true;
            this.lblOrderCharacter.Text = "Character:";
            this.lblOrderCharacter.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOrderCharacter.Name = "lblOrderCharacter";
            this.cmbOrderCharacter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbOrderCharacter.Size = new System.Drawing.Size(130, 21);
            this.cmbOrderCharacter.Name = "cmbOrderCharacter";
            this.lblOrderType.AutoSize = true;
            this.lblOrderType.Text = "Type:";
            this.lblOrderType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOrderType.Name = "lblOrderType";
            this.cmbOrderType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbOrderType.Size = new System.Drawing.Size(100, 21);
            this.cmbOrderType.Name = "cmbOrderType";
            this.lblOrderSearch.AutoSize = true;
            this.lblOrderSearch.Text = "Search:";
            this.lblOrderSearch.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOrderSearch.Name = "lblOrderSearch";
            this.txtOrderSearch.Size = new System.Drawing.Size(120, 20);
            this.txtOrderSearch.Name = "txtOrderSearch";
            this.lblOrderLocation.AutoSize = true;
            this.lblOrderLocation.Text = "Location:";
            this.lblOrderLocation.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOrderLocation.Name = "lblOrderLocation";
            this.cmbOrderLocation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbOrderLocation.Size = new System.Drawing.Size(130, 21);
            this.cmbOrderLocation.Name = "cmbOrderLocation";

            // lblSellOrders
            this.lblSellOrders.AutoSize = true;
            this.lblSellOrders.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSellOrders.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblSellOrders.Text = "Sell Orders";
            this.lblSellOrders.Name = "lblSellOrders";

            // dgvSellOrders
            this.dgvSellOrders.AllowUserToAddRows = false;
            this.dgvSellOrders.AllowUserToDeleteRows = false;
            this.dgvSellOrders.AllowUserToOrderColumns = true;
            this.dgvSellOrders.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSellOrders.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colSellItem, this.colSellQty, this.colSellPrice,
                this.colSellStation, this.colSellStatus});
            this.dgvSellOrders.Dock = System.Windows.Forms.DockStyle.Top;
            this.dgvSellOrders.Name = "dgvSellOrders";
            this.dgvSellOrders.ReadOnly = true;
            this.dgvSellOrders.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvSellOrders.Size = new System.Drawing.Size(936, 220);

            this.colSellItem.HeaderText = "Item";
            this.colSellItem.Name = "colSellItem";
            this.colSellItem.ReadOnly = true;
            this.colSellItem.Width = 180;
            this.colSellQty.HeaderText = "Qty";
            this.colSellQty.Name = "colSellQty";
            this.colSellQty.ReadOnly = true;
            this.colSellQty.Width = 70;
            this.colSellPrice.HeaderText = "Price";
            this.colSellPrice.Name = "colSellPrice";
            this.colSellPrice.ReadOnly = true;
            this.colSellPrice.Width = 90;
            this.colSellStation.HeaderText = "Station";
            this.colSellStation.Name = "colSellStation";
            this.colSellStation.ReadOnly = true;
            this.colSellStation.Width = 120;
            this.colSellStatus.HeaderText = "Status";
            this.colSellStatus.Name = "colSellStatus";
            this.colSellStatus.ReadOnly = true;
            this.colSellStatus.Width = 80;

            // lblBuyOrders
            this.lblBuyOrders.AutoSize = true;
            this.lblBuyOrders.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblBuyOrders.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblBuyOrders.Text = "Buy Orders";
            this.lblBuyOrders.Name = "lblBuyOrders";

            // dgvBuyOrders
            this.dgvBuyOrders.AllowUserToAddRows = false;
            this.dgvBuyOrders.AllowUserToDeleteRows = false;
            this.dgvBuyOrders.AllowUserToOrderColumns = true;
            this.dgvBuyOrders.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvBuyOrders.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colBuyItem, this.colBuyQty, this.colBuyPrice,
                this.colBuyStation, this.colBuyStatus});
            this.dgvBuyOrders.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvBuyOrders.Name = "dgvBuyOrders";
            this.dgvBuyOrders.ReadOnly = true;
            this.dgvBuyOrders.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;

            this.colBuyItem.HeaderText = "Item";
            this.colBuyItem.Name = "colBuyItem";
            this.colBuyItem.ReadOnly = true;
            this.colBuyItem.Width = 180;
            this.colBuyQty.HeaderText = "Qty";
            this.colBuyQty.Name = "colBuyQty";
            this.colBuyQty.ReadOnly = true;
            this.colBuyQty.Width = 70;
            this.colBuyPrice.HeaderText = "Price";
            this.colBuyPrice.Name = "colBuyPrice";
            this.colBuyPrice.ReadOnly = true;
            this.colBuyPrice.Width = 90;
            this.colBuyStation.HeaderText = "Station";
            this.colBuyStation.Name = "colBuyStation";
            this.colBuyStation.ReadOnly = true;
            this.colBuyStation.Width = 120;
            this.colBuyStatus.HeaderText = "Status";
            this.colBuyStatus.Name = "colBuyStatus";
            this.colBuyStatus.ReadOnly = true;
            this.colBuyStatus.Width = 80;

            // tabPrices
            this.tabPrices.Controls.Add(this.dgvPriceStats);
            this.tabPrices.Controls.Add(this.flpPriceCommands);
            this.tabPrices.Controls.Add(this.flpPriceFilters);
            this.tabPrices.Location = new System.Drawing.Point(4, 22);
            this.tabPrices.Name = "tabPrices";
            this.tabPrices.Padding = new System.Windows.Forms.Padding(3);
            this.tabPrices.Size = new System.Drawing.Size(942, 594);
            this.tabPrices.TabIndex = 5;
            this.tabPrices.Text = "Prices";
            this.tabPrices.UseVisualStyleBackColor = true;

            // flpPriceFilters
            this.flpPriceFilters.AutoSize = true;
            this.flpPriceFilters.Controls.Add(this.lblPriceType);
            this.flpPriceFilters.Controls.Add(this.cmbPriceType);
            this.flpPriceFilters.Controls.Add(this.lblPriceItem);
            this.flpPriceFilters.Controls.Add(this.cmbPriceItem);
            this.flpPriceFilters.Controls.Add(this.lblPricePurity);
            this.flpPriceFilters.Controls.Add(this.cmbPricePurity);
            this.flpPriceFilters.Controls.Add(this.lblDaysBack);
            this.flpPriceFilters.Controls.Add(this.numDaysBack);
            this.flpPriceFilters.Controls.Add(this.chkBuyOrderPrices);
            this.flpPriceFilters.Controls.Add(this.cmdFetchPrices);
            this.flpPriceFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.flpPriceFilters.Name = "flpPriceFilters";
            this.flpPriceFilters.Size = new System.Drawing.Size(936, 30);
            this.flpPriceFilters.WrapContents = true;

            this.lblPriceType.AutoSize = true;
            this.lblPriceType.Text = "Type:";
            this.lblPriceType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblPriceType.Name = "lblPriceType";
            this.cmbPriceType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPriceType.Size = new System.Drawing.Size(100, 21);
            this.cmbPriceType.Name = "cmbPriceType";
            this.lblPriceItem.AutoSize = true;
            this.lblPriceItem.Text = "Item:";
            this.lblPriceItem.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblPriceItem.Name = "lblPriceItem";
            this.cmbPriceItem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPriceItem.Size = new System.Drawing.Size(130, 21);
            this.cmbPriceItem.Name = "cmbPriceItem";
            this.lblPricePurity.AutoSize = true;
            this.lblPricePurity.Text = "Purity:";
            this.lblPricePurity.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblPricePurity.Name = "lblPricePurity";
            this.cmbPricePurity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPricePurity.Size = new System.Drawing.Size(80, 21);
            this.cmbPricePurity.Name = "cmbPricePurity";
            this.lblDaysBack.AutoSize = true;
            this.lblDaysBack.Text = "Days:";
            this.lblDaysBack.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblDaysBack.Name = "lblDaysBack";
            this.numDaysBack.Minimum = 1;
            this.numDaysBack.Maximum = 365;
            this.numDaysBack.Value = 30;
            this.numDaysBack.Size = new System.Drawing.Size(60, 20);
            this.numDaysBack.Name = "numDaysBack";
            this.chkBuyOrderPrices.AutoSize = true;
            this.chkBuyOrderPrices.Text = "Buy Orders";
            this.chkBuyOrderPrices.Name = "chkBuyOrderPrices";
            this.cmdFetchPrices.Name = "cmdFetchPrices";
            this.cmdFetchPrices.Size = new System.Drawing.Size(55, 23);
            this.cmdFetchPrices.Text = "Fetch";
            this.cmdFetchPrices.UseVisualStyleBackColor = true;

            // flpPriceCommands
            this.flpPriceCommands.AutoSize = true;
            this.flpPriceCommands.Controls.Add(this.lblAutoPopPlan);
            this.flpPriceCommands.Controls.Add(this.cmbAutoPopPlan);
            this.flpPriceCommands.Controls.Add(this.cmdAutoPopulate);
            this.flpPriceCommands.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flpPriceCommands.Name = "flpPriceCommands";
            this.flpPriceCommands.Size = new System.Drawing.Size(936, 29);

            this.lblAutoPopPlan.AutoSize = true;
            this.lblAutoPopPlan.Text = "Plan:";
            this.lblAutoPopPlan.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblAutoPopPlan.Name = "lblAutoPopPlan";
            this.cmbAutoPopPlan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAutoPopPlan.Size = new System.Drawing.Size(150, 21);
            this.cmbAutoPopPlan.Name = "cmbAutoPopPlan";
            this.cmdAutoPopulate.Name = "cmdAutoPopulate";
            this.cmdAutoPopulate.Size = new System.Drawing.Size(95, 23);
            this.cmdAutoPopulate.Text = "Auto-Populate";
            this.cmdAutoPopulate.UseVisualStyleBackColor = true;

            // dgvPriceStats
            this.dgvPriceStats.AllowUserToAddRows = false;
            this.dgvPriceStats.AllowUserToDeleteRows = false;
            this.dgvPriceStats.AllowUserToOrderColumns = true;
            this.dgvPriceStats.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPriceStats.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colPriceItem, this.colPriceLow, this.colPriceAvg,
                this.colPriceHigh, this.colPriceSamples, this.colPriceFetched});
            this.dgvPriceStats.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvPriceStats.Name = "dgvPriceStats";
            this.dgvPriceStats.ReadOnly = true;
            this.dgvPriceStats.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;

            this.colPriceItem.HeaderText = "Item";
            this.colPriceItem.Name = "colPriceItem";
            this.colPriceItem.ReadOnly = true;
            this.colPriceItem.Width = 180;
            this.colPriceLow.HeaderText = "Low";
            this.colPriceLow.Name = "colPriceLow";
            this.colPriceLow.ReadOnly = true;
            this.colPriceLow.Width = 90;
            this.colPriceAvg.HeaderText = "Avg";
            this.colPriceAvg.Name = "colPriceAvg";
            this.colPriceAvg.ReadOnly = true;
            this.colPriceAvg.Width = 90;
            this.colPriceHigh.HeaderText = "High";
            this.colPriceHigh.Name = "colPriceHigh";
            this.colPriceHigh.ReadOnly = true;
            this.colPriceHigh.Width = 90;
            this.colPriceSamples.HeaderText = "Samples";
            this.colPriceSamples.Name = "colPriceSamples";
            this.colPriceSamples.ReadOnly = true;
            this.colPriceSamples.Width = 70;
            this.colPriceFetched.HeaderText = "Fetched";
            this.colPriceFetched.Name = "colPriceFetched";
            this.colPriceFetched.ReadOnly = true;
            this.colPriceFetched.Width = 100;

            // tabAlerts
            this.tabAlerts.Controls.Add(this.dgvAlerts);
            this.tabAlerts.Controls.Add(this.flpAlertCommands);
            this.tabAlerts.Controls.Add(this.flpAlertDetail);
            this.tabAlerts.Location = new System.Drawing.Point(4, 22);
            this.tabAlerts.Name = "tabAlerts";
            this.tabAlerts.Padding = new System.Windows.Forms.Padding(3);
            this.tabAlerts.Size = new System.Drawing.Size(942, 594);
            this.tabAlerts.TabIndex = 6;
            this.tabAlerts.Text = "Alerts";
            this.tabAlerts.UseVisualStyleBackColor = true;

            // flpAlertDetail
            this.flpAlertDetail.AutoSize = true;
            this.flpAlertDetail.Controls.Add(this.lblAlertName);
            this.flpAlertDetail.Controls.Add(this.txtAlertName);
            this.flpAlertDetail.Controls.Add(this.lblAlertType);
            this.flpAlertDetail.Controls.Add(this.cmbAlertType);
            this.flpAlertDetail.Controls.Add(this.lblAlertItemType);
            this.flpAlertDetail.Controls.Add(this.cmbAlertItemType);
            this.flpAlertDetail.Controls.Add(this.lblAlertItem);
            this.flpAlertDetail.Controls.Add(this.cmbAlertItem);
            this.flpAlertDetail.Controls.Add(this.lblAlertPurity);
            this.flpAlertDetail.Controls.Add(this.cmbAlertPurity);
            this.flpAlertDetail.Controls.Add(this.lblPriceConditionLabel);
            this.flpAlertDetail.Controls.Add(this.cmbPriceCondition);
            this.flpAlertDetail.Controls.Add(this.lblPriceThreshold);
            this.flpAlertDetail.Controls.Add(this.txtPriceThreshold);
            this.flpAlertDetail.Controls.Add(this.lblAlertLocation);
            this.flpAlertDetail.Controls.Add(this.cmbAlertLocation);
            this.flpAlertDetail.Controls.Add(this.chkAlertEnabled);
            this.flpAlertDetail.Dock = System.Windows.Forms.DockStyle.Top;
            this.flpAlertDetail.Name = "flpAlertDetail";
            this.flpAlertDetail.Size = new System.Drawing.Size(936, 56);
            this.flpAlertDetail.WrapContents = true;

            this.lblAlertName.AutoSize = true;
            this.lblAlertName.Text = "Name:";
            this.lblAlertName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblAlertName.Name = "lblAlertName";
            this.txtAlertName.Size = new System.Drawing.Size(120, 20);
            this.txtAlertName.Name = "txtAlertName";
            this.lblAlertType.AutoSize = true;
            this.lblAlertType.Text = "Alert Type:";
            this.lblAlertType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblAlertType.Name = "lblAlertType";
            this.cmbAlertType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAlertType.Size = new System.Drawing.Size(120, 21);
            this.cmbAlertType.Name = "cmbAlertType";
            this.lblAlertItemType.AutoSize = true;
            this.lblAlertItemType.Text = "Item Type:";
            this.lblAlertItemType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblAlertItemType.Name = "lblAlertItemType";
            this.cmbAlertItemType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAlertItemType.Size = new System.Drawing.Size(100, 21);
            this.cmbAlertItemType.Name = "cmbAlertItemType";
            this.lblAlertItem.AutoSize = true;
            this.lblAlertItem.Text = "Item:";
            this.lblAlertItem.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblAlertItem.Name = "lblAlertItem";
            this.cmbAlertItem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAlertItem.Size = new System.Drawing.Size(130, 21);
            this.cmbAlertItem.Name = "cmbAlertItem";
            this.lblAlertPurity.AutoSize = true;
            this.lblAlertPurity.Text = "Purity:";
            this.lblAlertPurity.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblAlertPurity.Name = "lblAlertPurity";
            this.cmbAlertPurity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAlertPurity.Size = new System.Drawing.Size(80, 21);
            this.cmbAlertPurity.Name = "cmbAlertPurity";
            this.lblPriceConditionLabel.AutoSize = true;
            this.lblPriceConditionLabel.Text = "Condition:";
            this.lblPriceConditionLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblPriceConditionLabel.Name = "lblPriceConditionLabel";
            this.cmbPriceCondition.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPriceCondition.Size = new System.Drawing.Size(90, 21);
            this.cmbPriceCondition.Name = "cmbPriceCondition";
            this.lblPriceThreshold.AutoSize = true;
            this.lblPriceThreshold.Text = "Threshold:";
            this.lblPriceThreshold.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblPriceThreshold.Name = "lblPriceThreshold";
            this.txtPriceThreshold.Size = new System.Drawing.Size(80, 20);
            this.txtPriceThreshold.Name = "txtPriceThreshold";
            this.lblAlertLocation.AutoSize = true;
            this.lblAlertLocation.Text = "Location:";
            this.lblAlertLocation.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblAlertLocation.Name = "lblAlertLocation";
            this.cmbAlertLocation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAlertLocation.Size = new System.Drawing.Size(130, 21);
            this.cmbAlertLocation.Name = "cmbAlertLocation";
            this.chkAlertEnabled.AutoSize = true;
            this.chkAlertEnabled.Text = "Enabled";
            this.chkAlertEnabled.Name = "chkAlertEnabled";

            // flpAlertCommands
            this.flpAlertCommands.AutoSize = true;
            this.flpAlertCommands.Controls.Add(this.cmdAddAlert);
            this.flpAlertCommands.Controls.Add(this.cmdEditAlert);
            this.flpAlertCommands.Controls.Add(this.cmdDeleteAlert);
            this.flpAlertCommands.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flpAlertCommands.Name = "flpAlertCommands";
            this.flpAlertCommands.Size = new System.Drawing.Size(936, 29);

            this.cmdAddAlert.Name = "cmdAddAlert";
            this.cmdAddAlert.Size = new System.Drawing.Size(55, 23);
            this.cmdAddAlert.Text = "Add";
            this.cmdAddAlert.UseVisualStyleBackColor = true;
            this.cmdEditAlert.Name = "cmdEditAlert";
            this.cmdEditAlert.Size = new System.Drawing.Size(55, 23);
            this.cmdEditAlert.Text = "Edit";
            this.cmdEditAlert.UseVisualStyleBackColor = true;
            this.cmdDeleteAlert.Name = "cmdDeleteAlert";
            this.cmdDeleteAlert.Size = new System.Drawing.Size(55, 23);
            this.cmdDeleteAlert.Text = "Delete";
            this.cmdDeleteAlert.UseVisualStyleBackColor = true;

            // dgvAlerts
            this.dgvAlerts.AllowUserToAddRows = false;
            this.dgvAlerts.AllowUserToDeleteRows = false;
            this.dgvAlerts.AllowUserToOrderColumns = true;
            this.dgvAlerts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvAlerts.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colAlertName, this.colAlertType, this.colAlertItem,
                this.colAlertCondition, this.colAlertEnabled, this.colAlertLastTriggered});
            this.dgvAlerts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvAlerts.Name = "dgvAlerts";
            this.dgvAlerts.ReadOnly = true;
            this.dgvAlerts.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;

            this.colAlertName.HeaderText = "Name";
            this.colAlertName.Name = "colAlertName";
            this.colAlertName.ReadOnly = true;
            this.colAlertName.Width = 150;
            this.colAlertType.HeaderText = "Type";
            this.colAlertType.Name = "colAlertType";
            this.colAlertType.ReadOnly = true;
            this.colAlertType.Width = 100;
            this.colAlertItem.HeaderText = "Item";
            this.colAlertItem.Name = "colAlertItem";
            this.colAlertItem.ReadOnly = true;
            this.colAlertItem.Width = 150;
            this.colAlertCondition.HeaderText = "Condition";
            this.colAlertCondition.Name = "colAlertCondition";
            this.colAlertCondition.ReadOnly = true;
            this.colAlertCondition.Width = 100;
            this.colAlertEnabled.HeaderText = "Enabled";
            this.colAlertEnabled.Name = "colAlertEnabled";
            this.colAlertEnabled.ReadOnly = true;
            this.colAlertEnabled.Width = 60;
            this.colAlertLastTriggered.HeaderText = "Last Triggered";
            this.colAlertLastTriggered.Name = "colAlertLastTriggered";
            this.colAlertLastTriggered.ReadOnly = true;
            this.colAlertLastTriggered.Width = 120;

            // lblScopeStatus
            this.lblScopeStatus.AutoSize = true;
            this.lblScopeStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblScopeStatus.Text = string.Empty;
            this.lblScopeStatus.Name = "lblScopeStatus";

            // lblStaleWarning
            this.lblStaleWarning.AutoSize = true;
            this.lblStaleWarning.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblStaleWarning.ForeColor = System.Drawing.Color.OrangeRed;
            this.lblStaleWarning.Text = string.Empty;
            this.lblStaleWarning.Name = "lblStaleWarning";
            this.lblStaleWarning.Visible = false;

            // FormMarket
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(950, 620);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.lblStaleWarning);
            this.Controls.Add(this.lblScopeStatus);
            this.Name = "FormMarket";
            this.Text = "Market";

            this.tabControl.ResumeLayout(false);
            this.tabListings.ResumeLayout(false);
            this.tabListings.PerformLayout();
            this.tabTransactions.ResumeLayout(false);
            this.tabTransactions.PerformLayout();
            this.tabSummary.ResumeLayout(false);
            this.tabSummary.PerformLayout();
            this.tabSavedSearches.ResumeLayout(false);
            this.tabSavedSearches.PerformLayout();
            this.tabMyOrders.ResumeLayout(false);
            this.tabMyOrders.PerformLayout();
            this.tabPrices.ResumeLayout(false);
            this.tabPrices.PerformLayout();
            this.tabAlerts.ResumeLayout(false);
            this.tabAlerts.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvListings)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTransactions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSummary)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSavedSearches)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTestResults)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSellOrders)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBuyOrders)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPriceStats)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAlerts)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDaysBack)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        // Tab control
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabListings;
        private System.Windows.Forms.TabPage tabTransactions;
        private System.Windows.Forms.TabPage tabSummary;
        private System.Windows.Forms.TabPage tabSavedSearches;
        private System.Windows.Forms.TabPage tabMyOrders;
        private System.Windows.Forms.TabPage tabPrices;
        private System.Windows.Forms.TabPage tabAlerts;

        // Listings tab
        private System.Windows.Forms.DataGridView dgvListings;
        private System.Windows.Forms.DataGridViewTextBoxColumn colListStation;
        private System.Windows.Forms.DataGridViewTextBoxColumn colListItemName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colListType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colListQty;
        private System.Windows.Forms.DataGridViewTextBoxColumn colListPrice;
        private System.Windows.Forms.DataGridViewTextBoxColumn colListCondition;
        private System.Windows.Forms.DataGridViewTextBoxColumn colListRefs;
        private System.Windows.Forms.FlowLayoutPanel flpListingCommands;
        private System.Windows.Forms.Button cmdListingAdd;
        private System.Windows.Forms.Button cmdListingEdit;
        private System.Windows.Forms.Button cmdListingDelete;
        private System.Windows.Forms.Button cmdRecordSale;
        private System.Windows.Forms.ContextMenuStrip cmsListings;
        private System.Windows.Forms.ToolStripMenuItem tsmiRecordSale;
        private System.Windows.Forms.ToolStripMenuItem tsmiEditListing;
        private System.Windows.Forms.ToolStripMenuItem tsmiDeleteListing;
        private System.Windows.Forms.ContextMenuStrip cmsTransactions;
        private System.Windows.Forms.ToolStripMenuItem tsmiViewDetails;

        // Transactions tab
        private System.Windows.Forms.FlowLayoutPanel flpTxFilters;
        private System.Windows.Forms.Label lblTxType;
        private System.Windows.Forms.ComboBox cmbTxType;
        private System.Windows.Forms.Label lblTxItem;
        private System.Windows.Forms.TextBox txtTxItem;
        private System.Windows.Forms.Label lblTxCounterparty;
        private System.Windows.Forms.TextBox txtTxCounterparty;
        private System.Windows.Forms.Label lblTxFaction;
        private System.Windows.Forms.TextBox txtTxFaction;
        private System.Windows.Forms.Label lblTxStation;
        private System.Windows.Forms.ComboBox cmbTxStation;
        private System.Windows.Forms.Label lblTxFrom;
        private System.Windows.Forms.DateTimePicker dtpTxFrom;
        private System.Windows.Forms.Label lblTxTo;
        private System.Windows.Forms.DateTimePicker dtpTxTo;
        private System.Windows.Forms.Button cmdTxApply;
        private System.Windows.Forms.DataGridView dgvTransactions;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTxDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTxType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTxItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTxQty;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTxPrice;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTxTotal;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTxCounterparty;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTxFaction;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTxStation;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTxCondition;

        // Summary tab
        private System.Windows.Forms.FlowLayoutPanel flpSummaryFilters;
        private System.Windows.Forms.Label lblSumFrom;
        private System.Windows.Forms.DateTimePicker dtpSumFrom;
        private System.Windows.Forms.Label lblSumTo;
        private System.Windows.Forms.DateTimePicker dtpSumTo;
        private System.Windows.Forms.Label lblSumStation;
        private System.Windows.Forms.ComboBox cmbSumStation;
        private System.Windows.Forms.Label lblPricingPlan;
        private System.Windows.Forms.ComboBox cmbPricingPlan;
        private System.Windows.Forms.Button cmdCompute;
        private System.Windows.Forms.FlowLayoutPanel flpSummaryTotals;
        private System.Windows.Forms.Label lblTotalSales;
        private System.Windows.Forms.Label lblTotalPurchases;
        private System.Windows.Forms.Label lblNetPL;
        private System.Windows.Forms.DataGridView dgvSummary;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSumItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSumQtySold;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSumRevenue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSumQtyBought;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSumCost;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSumNet;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSumPlanValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSumMargin;

        // Saved Searches tab
        private System.Windows.Forms.ComboBox cmbSearchCharacter;
        private System.Windows.Forms.DataGridView dgvSavedSearches;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSearchName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSearchType;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colSearchEnabled;
        private System.Windows.Forms.TextBox txtSearchName;
        private System.Windows.Forms.ComboBox cmbSearchType;
        private System.Windows.Forms.ComboBox cmbSearchItem;
        private System.Windows.Forms.ComboBox cmbSearchPurity;
        private System.Windows.Forms.ComboBox cmbSearchOrderType;
        private System.Windows.Forms.CheckBox chkRunOnSync;
        private System.Windows.Forms.Button cmdTestSearch;
        private System.Windows.Forms.Button cmdSaveSearch;
        private System.Windows.Forms.Button cmdDeleteSearch;
        private System.Windows.Forms.DataGridView dgvTestResults;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTestItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTestPrice;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTestQty;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTestSeller;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTestStation;
        private System.Windows.Forms.FlowLayoutPanel flpSearchFilters;
        private System.Windows.Forms.FlowLayoutPanel flpSearchCommands;
        private System.Windows.Forms.Label lblSearchCharacter;
        private System.Windows.Forms.Label lblSearchName;
        private System.Windows.Forms.Label lblSearchType;
        private System.Windows.Forms.Label lblSearchItem;
        private System.Windows.Forms.Label lblSearchPurity;
        private System.Windows.Forms.Label lblSearchOrderType;

        // My Orders tab
        private System.Windows.Forms.ComboBox cmbOrderCharacter;
        private System.Windows.Forms.ComboBox cmbOrderType;
        private System.Windows.Forms.TextBox txtOrderSearch;
        private System.Windows.Forms.ComboBox cmbOrderLocation;
        private System.Windows.Forms.DataGridView dgvSellOrders;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSellItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSellQty;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSellPrice;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSellStation;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSellStatus;
        private System.Windows.Forms.DataGridView dgvBuyOrders;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBuyItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBuyQty;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBuyPrice;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBuyStation;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBuyStatus;
        private System.Windows.Forms.FlowLayoutPanel flpOrderFilters;
        private System.Windows.Forms.Label lblOrderCharacter;
        private System.Windows.Forms.Label lblOrderType;
        private System.Windows.Forms.Label lblOrderSearch;
        private System.Windows.Forms.Label lblOrderLocation;
        private System.Windows.Forms.Label lblSellOrders;
        private System.Windows.Forms.Label lblBuyOrders;

        // Prices tab
        private System.Windows.Forms.ComboBox cmbPriceType;
        private System.Windows.Forms.ComboBox cmbPriceItem;
        private System.Windows.Forms.ComboBox cmbPricePurity;
        private System.Windows.Forms.NumericUpDown numDaysBack;
        private System.Windows.Forms.CheckBox chkBuyOrderPrices;
        private System.Windows.Forms.Button cmdFetchPrices;
        private System.Windows.Forms.DataGridView dgvPriceStats;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPriceItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPriceLow;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPriceAvg;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPriceHigh;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPriceSamples;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPriceFetched;
        private System.Windows.Forms.Button cmdAutoPopulate;
        private System.Windows.Forms.ComboBox cmbAutoPopPlan;
        private System.Windows.Forms.FlowLayoutPanel flpPriceFilters;
        private System.Windows.Forms.FlowLayoutPanel flpPriceCommands;
        private System.Windows.Forms.Label lblPriceType;
        private System.Windows.Forms.Label lblPriceItem;
        private System.Windows.Forms.Label lblPricePurity;
        private System.Windows.Forms.Label lblDaysBack;
        private System.Windows.Forms.Label lblAutoPopPlan;

        // Alerts tab
        private System.Windows.Forms.DataGridView dgvAlerts;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAlertName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAlertType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAlertItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAlertCondition;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colAlertEnabled;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAlertLastTriggered;
        private System.Windows.Forms.TextBox txtAlertName;
        private System.Windows.Forms.ComboBox cmbAlertType;
        private System.Windows.Forms.ComboBox cmbAlertItemType;
        private System.Windows.Forms.ComboBox cmbAlertItem;
        private System.Windows.Forms.ComboBox cmbAlertPurity;
        private System.Windows.Forms.ComboBox cmbPriceCondition;
        private System.Windows.Forms.TextBox txtPriceThreshold;
        private System.Windows.Forms.ComboBox cmbAlertLocation;
        private System.Windows.Forms.Button cmdAddAlert;
        private System.Windows.Forms.Button cmdEditAlert;
        private System.Windows.Forms.Button cmdDeleteAlert;
        private System.Windows.Forms.CheckBox chkAlertEnabled;
        private System.Windows.Forms.FlowLayoutPanel flpAlertDetail;
        private System.Windows.Forms.FlowLayoutPanel flpAlertCommands;
        private System.Windows.Forms.Label lblAlertName;
        private System.Windows.Forms.Label lblAlertType;
        private System.Windows.Forms.Label lblAlertItemType;
        private System.Windows.Forms.Label lblAlertItem;
        private System.Windows.Forms.Label lblAlertPurity;
        private System.Windows.Forms.Label lblPriceConditionLabel;
        private System.Windows.Forms.Label lblPriceThreshold;
        private System.Windows.Forms.Label lblAlertLocation;

        // Status labels
        private System.Windows.Forms.Label lblScopeStatus;
        private System.Windows.Forms.Label lblStaleWarning;
    }
}
