using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.Market
{
    public partial class FormMarket : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        private PlayerContext playerContext;

        public FormMarket()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;

            // Listings tab
            cmdListingAdd.Click += CmdListingAdd_Click;
            cmdListingEdit.Click += CmdListingEdit_Click;
            cmdListingDelete.Click += CmdListingDelete_Click;
            cmdRecordSale.Click += CmdRecordSale_Click;

            // Transactions tab
            cmbTxType.Items.AddRange(new object[] { "All", "Buy", "Sell" });
            cmbTxType.SelectedIndex = 0;
            PopulateStationCombos();
            cmdTxApply.Click += CmdTxApply_Click;

            // Summary tab
            cmdCompute.Click += CmdCompute_Click;
            PopulatePricingPlanCombo();

            // Events
            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.MarketDataChanged += OnMarketDataChanged;

            PopulateListingsGrid();
            PopulateTransactionsGrid();
        }

        // -----------------------------------------------------------------------
        // Listings Tab (30.2)
        // -----------------------------------------------------------------------
        private void PopulateListingsGrid()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvListings.Rows.Clear();

            var listings = playerContext.GetCurrentPlayerListings();
            var refCounter = new MarketListingReferenceCounter(playerContext.MarketTransactionList);

            foreach (var listing in listings.OrderBy(l => l.ItemName))
            {
                string stationName = ResolveStationName(listing.StationUUID);
                string condition = listing.MaxHP > 0
                    ? string.Format("{0:F0}%", (listing.CurrentHP * 100.0 / listing.MaxHP))
                    : "N/A";
                int refs = refCounter.CountReferences(listing.UUID);

                int rowIdx = dgvListings.Rows.Add(
                    stationName, listing.ItemName, listing.ItemType.ToString(),
                    listing.Quantity.ToString(), listing.PricePerUnit.ToString("N2"),
                    condition, refs.ToString());
                dgvListings.Rows[rowIdx].Tag = listing;
            }

            sw.Stop();
            Log.Info("PERF PopulateListingsGrid: {0}ms items={1}", sw.ElapsedMilliseconds, listings.Count);
        }

        private void CmdListingAdd_Click(object sender, EventArgs e)
        {
            var listing = new MarketListing
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = playerContext.CurrentPlayerUUID,
                ItemName = "New Listing",
                Quantity = 1,
                PricePerUnit = 0m
            };

            playerContext.AddMarketListing(listing);
            playerContext.InvalidateMarketListingCache();
            playerContext.WriteContext();
            playerContext.OnMarketDataChanged();
            Log.Info("Added new market listing");
        }

        private void CmdListingEdit_Click(object sender, EventArgs e)
        {
            if (dgvListings.SelectedRows.Count == 0) return;
            var listing = dgvListings.SelectedRows[0].Tag as MarketListing;
            if (listing == null) return;

            using (var dlg = new FormListingEdit(listing, playerContext))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    playerContext.InvalidateMarketListingCache();
                    playerContext.WriteContext();
                    playerContext.OnMarketDataChanged();
                    Log.Info("Edited market listing '{0}'", listing.ItemName);
                }
            }
        }

        private void CmdListingDelete_Click(object sender, EventArgs e)
        {
            if (dgvListings.SelectedRows.Count == 0) return;
            var listing = dgvListings.SelectedRows[0].Tag as MarketListing;
            if (listing == null) return;

            var refCounter = new MarketListingReferenceCounter(playerContext.MarketTransactionList);
            int refs = refCounter.CountReferences(listing.UUID);
            if (refs > 0)
            {
                MessageBox.Show(
                    string.Format("Cannot delete listing \"{0}\" \u2014 it is referenced by {1} transaction(s).",
                        listing.ItemName, refs),
                    "Delete Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                string.Format("Delete listing \"{0}\"?", listing.ItemName),
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            playerContext.RemoveMarketListing(listing);
            playerContext.InvalidateMarketListingCache();
            playerContext.WriteContext();
            playerContext.OnMarketDataChanged();
            Log.Info("Deleted market listing '{0}'", listing.ItemName);
        }

        // -----------------------------------------------------------------------
        // Record Sale Dialog (30.5)
        // -----------------------------------------------------------------------
        private void CmdRecordSale_Click(object sender, EventArgs e)
        {
            if (dgvListings.SelectedRows.Count == 0) return;
            var listing = dgvListings.SelectedRows[0].Tag as MarketListing;
            if (listing == null) return;

            using (var dlg = new FormRecordSale(listing))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var tx = MarketService.RecordSale(
                        listing, dlg.SaleQuantity, dlg.SalePricePerUnit,
                        dlg.Counterparty, dlg.CounterpartyFaction, listing.StationUUID);

                    if (tx == null)
                    {
                        MessageBox.Show("Sale quantity exceeds listing availability.",
                            "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    playerContext.AddMarketTransaction(tx);
                    playerContext.WriteContext();
                    playerContext.OnMarketDataChanged();
                    Log.Info("Recorded sale: {0}x '{1}' at {2}/unit", dlg.SaleQuantity, listing.ItemName, dlg.SalePricePerUnit);
                }
            }
        }

        // -----------------------------------------------------------------------
        // Transactions Tab (30.3)
        // -----------------------------------------------------------------------
        private void PopulateTransactionsGrid()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvTransactions.Rows.Clear();

            var transactions = playerContext.GetCurrentPlayerTransactions();

            // Apply filters
            string typeFilter = cmbTxType.SelectedItem?.ToString() ?? "All";
            string itemFilter = txtTxItem.Text.Trim();
            string counterpartyFilter = txtTxCounterparty.Text.Trim();
            string factionFilter = txtTxFaction.Text.Trim();
            string stationUUID = null;
            if (cmbTxStation.SelectedItem is StationEntry stEntry && !string.IsNullOrEmpty(stEntry.UUID))
                stationUUID = stEntry.UUID;

            DateTime? fromDate = dtpTxFrom.Checked ? dtpTxFrom.Value.Date : (DateTime?)null;
            DateTime? toDate = dtpTxTo.Checked ? dtpTxTo.Value.Date.AddDays(1) : (DateTime?)null;

            foreach (var tx in transactions.OrderByDescending(t => t.Timestamp))
            {
                if (typeFilter == "Buy" && tx.TransactionType != TransactionType.Buy) continue;
                if (typeFilter == "Sell" && tx.TransactionType != TransactionType.Sell) continue;
                if (!string.IsNullOrEmpty(itemFilter) && tx.ItemName.IndexOf(itemFilter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (!string.IsNullOrEmpty(counterpartyFilter) && tx.Counterparty.IndexOf(counterpartyFilter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (!string.IsNullOrEmpty(factionFilter) && tx.CounterpartyFaction.IndexOf(factionFilter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (stationUUID != null && tx.StationUUID != stationUUID) continue;

                if (fromDate.HasValue || toDate.HasValue)
                {
                    if (DateTime.TryParse(tx.Timestamp, out DateTime txDate))
                    {
                        if (fromDate.HasValue && txDate < fromDate.Value) continue;
                        if (toDate.HasValue && txDate >= toDate.Value) continue;
                    }
                }

                string stationName = ResolveStationName(tx.StationUUID);
                string condition = tx.MaxHP > 0
                    ? string.Format("{0:F0}%", (tx.CurrentHP * 100.0 / tx.MaxHP))
                    : string.Empty;
                string dateStr = string.Empty;
                if (DateTime.TryParse(tx.Timestamp, out DateTime parsed))
                    dateStr = parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

                dgvTransactions.Rows.Add(
                    dateStr, tx.TransactionType.ToString(), tx.ItemName,
                    tx.Quantity.ToString(), tx.PricePerUnit.ToString("N2"),
                    tx.TotalPrice.ToString("N2"), tx.Counterparty,
                    tx.CounterpartyFaction, stationName, condition);
            }

            sw.Stop();
            Log.Info("PERF PopulateTransactionsGrid: {0}ms rows={1}", sw.ElapsedMilliseconds, dgvTransactions.Rows.Count);
        }

        private void CmdTxApply_Click(object sender, EventArgs e)
        {
            PopulateTransactionsGrid();
        }

        // -----------------------------------------------------------------------
        // Summary Tab (30.4)
        // -----------------------------------------------------------------------
        private void CmdCompute_Click(object sender, EventArgs e)
        {
            var sw = Stopwatch.StartNew();
            var transactions = playerContext.GetCurrentPlayerTransactions();

            DateTime? startDate = dtpSumFrom.Checked ? dtpSumFrom.Value.Date : (DateTime?)null;
            DateTime? endDate = dtpSumTo.Checked ? dtpSumTo.Value.Date.AddDays(1) : (DateTime?)null;
            string stationUUID = null;
            if (cmbSumStation.SelectedItem is StationEntry stEntry && !string.IsNullOrEmpty(stEntry.UUID))
                stationUUID = stEntry.UUID;

            var summary = MarketService.ComputeProfitLoss(transactions, startDate, endDate, stationUUIDFilter: stationUUID);

            lblTotalSales.Text = string.Format("Total Sales: {0:N2}", summary.TotalSalesRevenue);
            lblTotalPurchases.Text = string.Format("Total Purchases: {0:N2}", summary.TotalPurchaseCost);
            lblNetPL.Text = string.Format("Net P/L: {0:N2}", summary.NetProfitLoss);
            if (summary.NetProfitLoss >= 0)
                lblNetPL.ForeColor = System.Drawing.Color.DarkGreen;
            else
                lblNetPL.ForeColor = System.Drawing.Color.Red;

            dgvSummary.Rows.Clear();
            var plan = GetSelectedPricingPlan();
            decimal totalPlanValue = 0m;
            foreach (var kvp in summary.ItemBreakdown.OrderBy(k => k.Key))
            {
                var b = kvp.Value;
                string planValueStr = string.Empty;
                string marginStr = string.Empty;
                if (plan != null && b.QuantitySold > 0)
                {
                    // Try to compute plan value for this item
                    decimal unitPrice = 0m;
                    var commodity = Commodity.ResourceMapByEnum.Values
                        .FirstOrDefault(c => c.Name == b.ItemName || c.ExtendedName == b.ItemName);
                    if (commodity != null)
                    {
                        var cp = PriceCalculator.ComputeCommodityPrice(plan, commodity);
                        unitPrice = cp.Price;
                    }
                    else
                    {
                        // Try as a resource
                        string purity = PriceCalculator.DeterminePurity(b.ItemName);
                        if (PriceCalculator.TryGetResourcePrice(plan, b.ItemName, purity, out decimal rp))
                            unitPrice = rp;
                    }

                    if (unitPrice > 0)
                    {
                        decimal itemPlanValue = unitPrice * b.QuantitySold;
                        decimal margin = b.SalesRevenue - itemPlanValue;
                        totalPlanValue += itemPlanValue;
                        planValueStr = itemPlanValue.ToString("N2");
                        marginStr = margin.ToString("N2");
                    }
                }

                dgvSummary.Rows.Add(b.ItemName, b.QuantitySold.ToString(),
                    b.SalesRevenue.ToString("N2"), b.QuantityBought.ToString(),
                    b.PurchaseCost.ToString("N2"), b.NetProfitLoss.ToString("N2"),
                    planValueStr, marginStr);
            }

            if (plan != null && totalPlanValue > 0)
            {
                lblNetPL.Text += string.Format("  |  Plan Value: {0:N2}  |  Margin: {1:N2}",
                    totalPlanValue, summary.TotalSalesRevenue - totalPlanValue);
            }

            sw.Stop();
            Log.Info("PERF CmdCompute_Click: {0}ms items={1}", sw.ElapsedMilliseconds, summary.ItemBreakdown.Count);
        }

        private void PopulatePricingPlanCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbPricingPlan.DataSource = null;
            cmbPricingPlan.Items.Clear();

            var plans = playerContext.PricingPlanList
                .Where(p => p.OwnerUUID == playerContext.CurrentPlayerUUID)
                .OrderBy(p => p.Name).ToList();

            var items = new List<KeyValuePair<string, string>>();
            items.Add(new KeyValuePair<string, string>(string.Empty, "(none)"));
            foreach (var plan in plans)
                items.Add(new KeyValuePair<string, string>(plan.UUID, plan.Name));

            cmbPricingPlan.DataSource = items;
            cmbPricingPlan.DisplayMember = "Value";
            cmbPricingPlan.ValueMember = "Key";
            sw.Stop(); Log.Info("PERF PopulatePricingPlanCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private Models.PricingPlan GetSelectedPricingPlan()
        {
            string uuid = cmbPricingPlan.SelectedValue?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(uuid)) return null;
            return playerContext.PricingPlanList.FirstOrDefault(p => p.UUID == uuid);
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------
        private string ResolveStationName(string stationUUID)
        {
            if (string.IsNullOrEmpty(stationUUID)) return string.Empty;
            var station = playerContext.FindStation(stationUUID);
            return station?.Name ?? stationUUID;
        }

        private void PopulateStationCombos()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            var stations = playerContext.GetCurrentPlayerStations()
                .OrderBy(s => s.Name).ToList();

            cmbTxStation.Items.Clear();
            cmbTxStation.Items.Add(new StationEntry { Display = "(All)", UUID = string.Empty });
            cmbSumStation.Items.Clear();
            cmbSumStation.Items.Add(new StationEntry { Display = "(All)", UUID = string.Empty });

            foreach (var s in stations)
            {
                var entry = new StationEntry { Display = s.Name, UUID = s.UUID };
                cmbTxStation.Items.Add(entry);
                cmbSumStation.Items.Add(entry);
            }

            cmbTxStation.SelectedIndex = 0;
            cmbSumStation.SelectedIndex = 0;
            sw.Stop(); Log.Info("PERF PopulateStationCombos: {0}ms", sw.ElapsedMilliseconds);
        }

        // -----------------------------------------------------------------------
        // Events (30.7)
        // -----------------------------------------------------------------------
        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            { try { BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e))); } catch (ObjectDisposedException) { } return; }
            PopulateStationCombos();
            PopulatePricingPlanCombo();
            PopulateListingsGrid();
            PopulateTransactionsGrid();
        }

        private void OnMarketDataChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            { try { BeginInvoke(new Action(() => OnMarketDataChanged(sender, e))); } catch (ObjectDisposedException) { } return; }
            PopulateListingsGrid();
            PopulateTransactionsGrid();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.MarketDataChanged -= OnMarketDataChanged;
            base.OnFormClosed(e);
        }

        // -----------------------------------------------------------------------
        // StationEntry helper for combo boxes
        // -----------------------------------------------------------------------
        private class StationEntry
        {
            public string Display { get; set; }
            public string UUID { get; set; }
            public override string ToString() => Display;
        }
    }
}