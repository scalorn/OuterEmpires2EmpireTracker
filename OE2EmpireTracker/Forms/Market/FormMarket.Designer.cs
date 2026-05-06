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

            this.tabControl.SuspendLayout();
            this.tabListings.SuspendLayout();
            this.tabTransactions.SuspendLayout();
            this.tabSummary.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvListings)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTransactions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSummary)).BeginInit();
            this.SuspendLayout();
            // tabControl
            this.tabControl.Controls.Add(this.tabListings);
            this.tabControl.Controls.Add(this.tabTransactions);
            this.tabControl.Controls.Add(this.tabSummary);
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
            this.dgvListings.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvListings.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colListStation, this.colListItemName, this.colListType, this.colListQty,
                this.colListPrice, this.colListCondition, this.colListRefs});
            this.dgvListings.Location = new System.Drawing.Point(3, 3);
            this.dgvListings.Name = "dgvListings";
            this.dgvListings.ReadOnly = true;
            this.dgvListings.ContextMenuStrip = this.cmsListings;
            this.dgvListings.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvListings.Size = new System.Drawing.Size(936, 530);

            this.colListStation.HeaderText = "Station"; this.colListStation.Name = "colListStation"; this.colListStation.ReadOnly = true; this.colListStation.Width = 120;
            this.colListItemName.HeaderText = "Item Name"; this.colListItemName.Name = "colListItemName"; this.colListItemName.ReadOnly = true; this.colListItemName.Width = 180;
            this.colListType.HeaderText = "Type"; this.colListType.Name = "colListType"; this.colListType.ReadOnly = true; this.colListType.Width = 80;
            this.colListQty.HeaderText = "Quantity"; this.colListQty.Name = "colListQty"; this.colListQty.ReadOnly = true; this.colListQty.Width = 70;
            this.colListPrice.HeaderText = "Price/Unit"; this.colListPrice.Name = "colListPrice"; this.colListPrice.ReadOnly = true; this.colListPrice.Width = 90;
            this.colListCondition.HeaderText = "Condition"; this.colListCondition.Name = "colListCondition"; this.colListCondition.ReadOnly = true; this.colListCondition.Width = 90;
            this.colListRefs.HeaderText = "Refs"; this.colListRefs.Name = "colListRefs"; this.colListRefs.ReadOnly = true; this.colListRefs.Width = 50;

            // flpListingCommands
            this.flpListingCommands.AutoSize = true;
            this.flpListingCommands.Controls.Add(this.cmdListingAdd);
            this.flpListingCommands.Controls.Add(this.cmdListingEdit);
            this.flpListingCommands.Controls.Add(this.cmdListingDelete);
            this.flpListingCommands.Controls.Add(this.cmdRecordSale);
            this.flpListingCommands.Location = new System.Drawing.Point(3, 539);
            this.flpListingCommands.Name = "flpListingCommands";
            this.flpListingCommands.Size = new System.Drawing.Size(936, 29);

            this.cmdListingAdd.Size = new System.Drawing.Size(55, 23); this.cmdListingAdd.Text = "Add"; this.cmdListingAdd.UseVisualStyleBackColor = true; this.cmdListingAdd.Name = "cmdListingAdd";
            this.cmdListingEdit.Size = new System.Drawing.Size(55, 23); this.cmdListingEdit.Text = "Edit"; this.cmdListingEdit.UseVisualStyleBackColor = true; this.cmdListingEdit.Name = "cmdListingEdit";
            this.cmdListingDelete.Size = new System.Drawing.Size(55, 23); this.cmdListingDelete.Text = "Delete"; this.cmdListingDelete.UseVisualStyleBackColor = true; this.cmdListingDelete.Name = "cmdListingDelete";
            this.cmdRecordSale.Size = new System.Drawing.Size(85, 23); this.cmdRecordSale.Text = "Record Sale"; this.cmdRecordSale.UseVisualStyleBackColor = true; this.cmdRecordSale.Name = "cmdRecordSale";
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
            this.tabTransactions.Controls.Add(this.flpTxFilters);
            this.tabTransactions.Controls.Add(this.dgvTransactions);
            this.tabTransactions.Location = new System.Drawing.Point(4, 22);
            this.tabTransactions.Name = "tabTransactions";
            this.tabTransactions.Padding = new System.Windows.Forms.Padding(3);
            this.tabTransactions.Size = new System.Drawing.Size(942, 594);
            this.tabTransactions.TabIndex = 1;
            this.tabTransactions.Text = "Transactions";
            this.tabTransactions.UseVisualStyleBackColor = true;

            // flpTxFilters
            this.flpTxFilters.AutoSize = true;
            this.flpTxFilters.Controls.Add(this.lblTxType); this.flpTxFilters.Controls.Add(this.cmbTxType);
            this.flpTxFilters.Controls.Add(this.lblTxItem); this.flpTxFilters.Controls.Add(this.txtTxItem);
            this.flpTxFilters.Controls.Add(this.lblTxCounterparty); this.flpTxFilters.Controls.Add(this.txtTxCounterparty);
            this.flpTxFilters.Controls.Add(this.lblTxFaction); this.flpTxFilters.Controls.Add(this.txtTxFaction);
            this.flpTxFilters.Controls.Add(this.lblTxStation); this.flpTxFilters.Controls.Add(this.cmbTxStation);
            this.flpTxFilters.Controls.Add(this.lblTxFrom); this.flpTxFilters.Controls.Add(this.dtpTxFrom);
            this.flpTxFilters.Controls.Add(this.lblTxTo); this.flpTxFilters.Controls.Add(this.dtpTxTo);
            this.flpTxFilters.Controls.Add(this.cmdTxApply);
            this.flpTxFilters.Location = new System.Drawing.Point(3, 3);
            this.flpTxFilters.Name = "flpTxFilters";
            this.flpTxFilters.Size = new System.Drawing.Size(936, 56);
            this.flpTxFilters.WrapContents = true;

            this.lblTxType.AutoSize = true; this.lblTxType.Text = "Type:"; this.lblTxType.Anchor = System.Windows.Forms.AnchorStyles.Left; this.lblTxType.Name = "lblTxType";
            this.cmbTxType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbTxType.Size = new System.Drawing.Size(80, 21); this.cmbTxType.Name = "cmbTxType";
            this.lblTxItem.AutoSize = true; this.lblTxItem.Text = "Item:"; this.lblTxItem.Anchor = System.Windows.Forms.AnchorStyles.Left; this.lblTxItem.Name = "lblTxItem";
            this.txtTxItem.Size = new System.Drawing.Size(100, 20); this.txtTxItem.Name = "txtTxItem";
            this.lblTxCounterparty.AutoSize = true; this.lblTxCounterparty.Text = "Counterparty:"; this.lblTxCounterparty.Anchor = System.Windows.Forms.AnchorStyles.Left; this.lblTxCounterparty.Name = "lblTxCounterparty";
            this.txtTxCounterparty.Size = new System.Drawing.Size(100, 20); this.txtTxCounterparty.Name = "txtTxCounterparty";
            this.lblTxFaction.AutoSize = true; this.lblTxFaction.Text = "Faction:"; this.lblTxFaction.Anchor = System.Windows.Forms.AnchorStyles.Left; this.lblTxFaction.Name = "lblTxFaction";
            this.txtTxFaction.Size = new System.Drawing.Size(80, 20); this.txtTxFaction.Name = "txtTxFaction";
            this.lblTxStation.AutoSize = true; this.lblTxStation.Text = "Station:"; this.lblTxStation.Anchor = System.Windows.Forms.AnchorStyles.Left; this.lblTxStation.Name = "lblTxStation";
            this.cmbTxStation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbTxStation.Size = new System.Drawing.Size(120, 21); this.cmbTxStation.Name = "cmbTxStation";
            this.lblTxFrom.AutoSize = true; this.lblTxFrom.Text = "From:"; this.lblTxFrom.Anchor = System.Windows.Forms.AnchorStyles.Left; this.lblTxFrom.Name = "lblTxFrom";
            this.dtpTxFrom.Format = System.Windows.Forms.DateTimePickerFormat.Short; this.dtpTxFrom.Size = new System.Drawing.Size(100, 20); this.dtpTxFrom.Name = "dtpTxFrom"; this.dtpTxFrom.Checked = false; this.dtpTxFrom.ShowCheckBox = true;
            this.lblTxTo.AutoSize = true; this.lblTxTo.Text = "To:"; this.lblTxTo.Anchor = System.Windows.Forms.AnchorStyles.Left; this.lblTxTo.Name = "lblTxTo";
            this.dtpTxTo.Format = System.Windows.Forms.DateTimePickerFormat.Short; this.dtpTxTo.Size = new System.Drawing.Size(100, 20); this.dtpTxTo.Name = "dtpTxTo"; this.dtpTxTo.Checked = false; this.dtpTxTo.ShowCheckBox = true;
            this.cmdTxApply.Size = new System.Drawing.Size(55, 23); this.cmdTxApply.Text = "Apply"; this.cmdTxApply.UseVisualStyleBackColor = true; this.cmdTxApply.Name = "cmdTxApply";

            // dgvTransactions
            this.dgvTransactions.AllowUserToAddRows = false;
            this.dgvTransactions.AllowUserToDeleteRows = false;
            this.dgvTransactions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTransactions.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colTxDate, this.colTxType, this.colTxItem, this.colTxQty, this.colTxPrice,
                this.colTxTotal, this.colTxCounterparty, this.colTxFaction, this.colTxStation, this.colTxCondition});
            this.dgvTransactions.Location = new System.Drawing.Point(3, 65);
            this.dgvTransactions.Name = "dgvTransactions";
            this.dgvTransactions.ReadOnly = true;
            this.dgvTransactions.ContextMenuStrip = this.cmsTransactions;
            this.dgvTransactions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvTransactions.Size = new System.Drawing.Size(936, 526);

            this.colTxDate.HeaderText = "Date"; this.colTxDate.Name = "colTxDate"; this.colTxDate.ReadOnly = true; this.colTxDate.Width = 90;
            this.colTxType.HeaderText = "Type"; this.colTxType.Name = "colTxType"; this.colTxType.ReadOnly = true; this.colTxType.Width = 50;
            this.colTxItem.HeaderText = "Item"; this.colTxItem.Name = "colTxItem"; this.colTxItem.ReadOnly = true; this.colTxItem.Width = 140;
            this.colTxQty.HeaderText = "Qty"; this.colTxQty.Name = "colTxQty"; this.colTxQty.ReadOnly = true; this.colTxQty.Width = 50;
            this.colTxPrice.HeaderText = "Price/Unit"; this.colTxPrice.Name = "colTxPrice"; this.colTxPrice.ReadOnly = true; this.colTxPrice.Width = 80;
            this.colTxTotal.HeaderText = "Total"; this.colTxTotal.Name = "colTxTotal"; this.colTxTotal.ReadOnly = true; this.colTxTotal.Width = 80;
            this.colTxCounterparty.HeaderText = "Counterparty"; this.colTxCounterparty.Name = "colTxCounterparty"; this.colTxCounterparty.ReadOnly = true; this.colTxCounterparty.Width = 100;
            this.colTxFaction.HeaderText = "Faction"; this.colTxFaction.Name = "colTxFaction"; this.colTxFaction.ReadOnly = true; this.colTxFaction.Width = 80;
            this.colTxStation.HeaderText = "Station"; this.colTxStation.Name = "colTxStation"; this.colTxStation.ReadOnly = true; this.colTxStation.Width = 100;
            this.colTxCondition.HeaderText = "Condition"; this.colTxCondition.Name = "colTxCondition"; this.colTxCondition.ReadOnly = true; this.colTxCondition.Width = 80;
            // tabSummary
            this.tabSummary.Controls.Add(this.flpSummaryFilters);
            this.tabSummary.Controls.Add(this.flpSummaryTotals);
            this.tabSummary.Controls.Add(this.dgvSummary);
            this.tabSummary.Location = new System.Drawing.Point(4, 22);
            this.tabSummary.Name = "tabSummary";
            this.tabSummary.Padding = new System.Windows.Forms.Padding(3);
            this.tabSummary.Size = new System.Drawing.Size(942, 594);
            this.tabSummary.TabIndex = 2;
            this.tabSummary.Text = "Summary";
            this.tabSummary.UseVisualStyleBackColor = true;

            // flpSummaryFilters
            this.flpSummaryFilters.AutoSize = true;
            this.flpSummaryFilters.Controls.Add(this.lblSumFrom); this.flpSummaryFilters.Controls.Add(this.dtpSumFrom);
            this.flpSummaryFilters.Controls.Add(this.lblSumTo); this.flpSummaryFilters.Controls.Add(this.dtpSumTo);
            this.flpSummaryFilters.Controls.Add(this.lblSumStation); this.flpSummaryFilters.Controls.Add(this.cmbSumStation);
            this.flpSummaryFilters.Controls.Add(this.lblPricingPlan); this.flpSummaryFilters.Controls.Add(this.cmbPricingPlan);
            this.flpSummaryFilters.Controls.Add(this.cmdCompute);
            this.flpSummaryFilters.Location = new System.Drawing.Point(3, 3);
            this.flpSummaryFilters.Name = "flpSummaryFilters";
            this.flpSummaryFilters.Size = new System.Drawing.Size(936, 29);

            this.lblSumFrom.AutoSize = true; this.lblSumFrom.Text = "From:"; this.lblSumFrom.Anchor = System.Windows.Forms.AnchorStyles.Left; this.lblSumFrom.Name = "lblSumFrom";
            this.dtpSumFrom.Format = System.Windows.Forms.DateTimePickerFormat.Short; this.dtpSumFrom.Size = new System.Drawing.Size(100, 20); this.dtpSumFrom.Name = "dtpSumFrom"; this.dtpSumFrom.Checked = false; this.dtpSumFrom.ShowCheckBox = true;
            this.lblSumTo.AutoSize = true; this.lblSumTo.Text = "To:"; this.lblSumTo.Anchor = System.Windows.Forms.AnchorStyles.Left; this.lblSumTo.Name = "lblSumTo";
            this.dtpSumTo.Format = System.Windows.Forms.DateTimePickerFormat.Short; this.dtpSumTo.Size = new System.Drawing.Size(100, 20); this.dtpSumTo.Name = "dtpSumTo"; this.dtpSumTo.Checked = false; this.dtpSumTo.ShowCheckBox = true;
            this.lblSumStation.AutoSize = true; this.lblSumStation.Text = "Station:"; this.lblSumStation.Anchor = System.Windows.Forms.AnchorStyles.Left; this.lblSumStation.Name = "lblSumStation";
            this.cmbSumStation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbSumStation.Size = new System.Drawing.Size(150, 21); this.cmbSumStation.Name = "cmbSumStation";
            this.lblPricingPlan.AutoSize = true; this.lblPricingPlan.Text = "Plan:"; this.lblPricingPlan.Anchor = System.Windows.Forms.AnchorStyles.Left; this.lblPricingPlan.Name = "lblPricingPlan";
            this.cmbPricingPlan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbPricingPlan.Size = new System.Drawing.Size(150, 21); this.cmbPricingPlan.Name = "cmbPricingPlan";
            this.cmdCompute.Size = new System.Drawing.Size(75, 23); this.cmdCompute.Text = "Compute"; this.cmdCompute.UseVisualStyleBackColor = true; this.cmdCompute.Name = "cmdCompute";

            // flpSummaryTotals
            this.flpSummaryTotals.AutoSize = true;
            this.flpSummaryTotals.Controls.Add(this.lblTotalSales);
            this.flpSummaryTotals.Controls.Add(this.lblTotalPurchases);
            this.flpSummaryTotals.Controls.Add(this.lblNetPL);
            this.flpSummaryTotals.Location = new System.Drawing.Point(3, 38);
            this.flpSummaryTotals.Name = "flpSummaryTotals";
            this.flpSummaryTotals.Size = new System.Drawing.Size(936, 20);

            this.lblTotalSales.AutoSize = true; this.lblTotalSales.Text = "Total Sales: 0"; this.lblTotalSales.Name = "lblTotalSales";
            this.lblTotalPurchases.AutoSize = true; this.lblTotalPurchases.Text = "Total Purchases: 0"; this.lblTotalPurchases.Name = "lblTotalPurchases"; this.lblTotalPurchases.Margin = new System.Windows.Forms.Padding(20, 0, 3, 0);
            this.lblNetPL.AutoSize = true; this.lblNetPL.Text = "Net P/L: 0"; this.lblNetPL.Name = "lblNetPL"; this.lblNetPL.Margin = new System.Windows.Forms.Padding(20, 0, 3, 0);

            // dgvSummary
            this.dgvSummary.AllowUserToAddRows = false;
            this.dgvSummary.AllowUserToDeleteRows = false;
            this.dgvSummary.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSummary.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colSumItem, this.colSumQtySold, this.colSumRevenue, this.colSumQtyBought, this.colSumCost, this.colSumNet, this.colSumPlanValue, this.colSumMargin});
            this.dgvSummary.Location = new System.Drawing.Point(3, 64);
            this.dgvSummary.Name = "dgvSummary";
            this.dgvSummary.ReadOnly = true;
            this.dgvSummary.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvSummary.Size = new System.Drawing.Size(936, 527);

            this.colSumItem.HeaderText = "Item"; this.colSumItem.Name = "colSumItem"; this.colSumItem.ReadOnly = true; this.colSumItem.Width = 180;
            this.colSumQtySold.HeaderText = "Qty Sold"; this.colSumQtySold.Name = "colSumQtySold"; this.colSumQtySold.ReadOnly = true; this.colSumQtySold.Width = 80;
            this.colSumRevenue.HeaderText = "Sales Revenue"; this.colSumRevenue.Name = "colSumRevenue"; this.colSumRevenue.ReadOnly = true; this.colSumRevenue.Width = 120;
            this.colSumQtyBought.HeaderText = "Qty Bought"; this.colSumQtyBought.Name = "colSumQtyBought"; this.colSumQtyBought.ReadOnly = true; this.colSumQtyBought.Width = 80;
            this.colSumCost.HeaderText = "Purchase Cost"; this.colSumCost.Name = "colSumCost"; this.colSumCost.ReadOnly = true; this.colSumCost.Width = 120;
            this.colSumNet.HeaderText = "Net"; this.colSumNet.Name = "colSumNet"; this.colSumNet.ReadOnly = true; this.colSumNet.Width = 100;
            this.colSumPlanValue.HeaderText = "Plan Value"; this.colSumPlanValue.Name = "colSumPlanValue"; this.colSumPlanValue.ReadOnly = true; this.colSumPlanValue.Width = 100;
            this.colSumMargin.HeaderText = "Margin"; this.colSumMargin.Name = "colSumMargin"; this.colSumMargin.ReadOnly = true; this.colSumMargin.Width = 100;

            // FormMarket
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(950, 620);
            this.Controls.Add(this.tabControl);
            this.Name = "FormMarket";
            this.Text = "Market";
            this.tabControl.ResumeLayout(false);
            this.tabListings.ResumeLayout(false);
            this.tabListings.PerformLayout();
            this.tabTransactions.ResumeLayout(false);
            this.tabTransactions.PerformLayout();
            this.tabSummary.ResumeLayout(false);
            this.tabSummary.PerformLayout();
            this.flpTxFilters.ResumeLayout(false);
            this.flpTxFilters.PerformLayout();
            this.flpSummaryFilters.ResumeLayout(false);
            this.flpSummaryFilters.PerformLayout();
            this.flpSummaryTotals.ResumeLayout(false);
            this.flpSummaryTotals.PerformLayout();
            this.flpListingCommands.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvListings)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTransactions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSummary)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabListings;
        private System.Windows.Forms.TabPage tabTransactions;
        private System.Windows.Forms.TabPage tabSummary;
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
        private System.Windows.Forms.ContextMenuStrip cmsListings;
        private System.Windows.Forms.ToolStripMenuItem tsmiRecordSale;
        private System.Windows.Forms.ToolStripMenuItem tsmiEditListing;
        private System.Windows.Forms.ToolStripMenuItem tsmiDeleteListing;
        private System.Windows.Forms.ContextMenuStrip cmsTransactions;
        private System.Windows.Forms.ToolStripMenuItem tsmiViewDetails;
    }
}