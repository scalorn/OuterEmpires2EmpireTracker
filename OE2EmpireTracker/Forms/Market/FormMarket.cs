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
            cmdListingAdd.Click += cmdListingAdd_Click;
            cmdListingEdit.Click += cmdListingEdit_Click;
            cmdListingDelete.Click += cmdListingDelete_Click;
            cmdRecordSale.Click += cmdRecordSale_Click;

            // Transactions tab
            cmbTxType.Items.AddRange(new object[] { "All", "Buy", "Sell" });
            cmbTxType.SelectedIndex = 0;
            PopulateStationCombos();
            cmdTxApply.Click += cmdTxApply_Click;

            // Summary tab
            cmdCompute.Click += cmdCompute_Click;

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
        private void cmdListingAdd_Click(object sender, EventArgs e)
        {
            var listing = new MarketListing
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = playerContext.CurrentPlayerUUID,
                ItemName = "New Listing",
                Quantity = 1,
                PricePerUnit = 0m
            };
            playerContext.MarketListingList.Add(listing);
            playerContext.InvalidateMarketListingCache();
            playerContext.WriteContext();
            playerContext.OnMarketDataChanged();
            Log.Info("Added new market listing");
        }

        private void cmdListingEdit_Click(object sender, EventArgs e)
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

        private void cmdListingDelete_Click(object sender, EventArgs e)
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

            playerContext.MarketListingList.Remove(listing);
            playerContext.InvalidateMarketListingCache();
            playerContext.WriteContext();
            playerContext.OnMarketDataChanged();
            Log.Info("Deleted market listing '{0}'", listing.ItemName);
        }

        // -----------------------------------------------------------------------
        // Record Sale Dialog (30.5)
        // -----------------------------------------------------------------------
        private void cmdRecordSale_Click(object sender, EventArgs e)
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

                    playerContext.MarketTransactionList.Add(tx);
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
                    : "";
                string dateStr = "";
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

        private void cmdTxApply_Click(object sender, EventArgs e)
        {
            PopulateTransactionsGrid();
        }

        // -----------------------------------------------------------------------
        // Summary Tab (30.4)
        // -----------------------------------------------------------------------
        private void cmdCompute_Click(object sender, EventArgs e)
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
            foreach (var kvp in summary.ItemBreakdown.OrderBy(k => k.Key))
            {
                var b = kvp.Value;
                dgvSummary.Rows.Add(b.ItemName, b.QuantitySold.ToString(),
                    b.SalesRevenue.ToString("N2"), b.QuantityBought.ToString(),
                    b.PurchaseCost.ToString("N2"), b.NetProfitLoss.ToString("N2"));
            }
            sw.Stop();
            Log.Info("PERF cmdCompute_Click: {0}ms items={1}", sw.ElapsedMilliseconds, summary.ItemBreakdown.Count);
        }
        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------
        private string ResolveStationName(string stationUUID)
        {
            if (string.IsNullOrEmpty(stationUUID)) return "";
            var station = playerContext.FindStation(stationUUID);
            return station?.Name ?? stationUUID;
        }

        private void PopulateStationCombos()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            var stations = playerContext.GetCurrentPlayerStations()
                .OrderBy(s => s.Name).ToList();

            cmbTxStation.Items.Clear();
            cmbTxStation.Items.Add(new StationEntry { Display = "(All)", UUID = "" });
            cmbSumStation.Items.Clear();
            cmbSumStation.Items.Add(new StationEntry { Display = "(All)", UUID = "" });

            foreach (var s in stations)
            {
                var entry = new StationEntry { Display = s.Name, UUID = s.UUID };
                cmbTxStation.Items.Add(entry);
                cmbSumStation.Items.Add(entry);
            }
            cmbTxStation.SelectedIndex = 0;
            cmbSumStation.SelectedIndex = 0;
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