using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.Market
{
    public partial class FormMarket : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private int _isProgrammaticUpdate = 0;

        private PlayerContext playerContext;

        private MarketListingService _marketListingService;

        private MarketDataService _marketDataService;

        private int _staleThresholdMinutes = 60;

        public FormMarket()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;
            _marketListingService = new MarketListingService(playerContext);
            _marketDataService = CreateMarketDataService();

            // Listings tab
            cmdListingAdd.Click += CmdListingAdd_Click;
            cmdListingEdit.Click += CmdListingEdit_Click;
            cmdListingDelete.Click += CmdListingDelete_Click;
            cmdRecordSale.Click += CmdRecordSale_Click;

            // Context menu events (grid-context-menus spec, task 6.2)
            tsmiRecordSale.Click += CmdRecordSale_Click;
            tsmiEditListing.Click += CmdListingEdit_Click;
            tsmiDeleteListing.Click += CmdListingDelete_Click;
            tsmiViewDetails.Click += TsmiViewDetails_Click;
            dgvListings.CellMouseClick += DgvListings_CellMouseClick;
            dgvTransactions.CellMouseClick += DgvTransactions_CellMouseClick;
            cmsListings.Opening += CmsListings_Opening;
            cmsTransactions.Opening += CmsTransactions_Opening;

            // Transactions tab
            cmbTxType.Items.AddRange(new object[] { "All", "Buy", "Sell" });
            cmbTxType.SelectedIndex = 0;
            PopulateStationCombos();
            cmdTxApply.Click += CmdTxApply_Click;

            // Summary tab
            cmdCompute.Click += CmdCompute_Click;
            PopulatePricingPlanCombo();

            // Saved Searches tab (10.2)
            cmdSaveSearch.Click += CmdSaveSearch_Click;
            cmdDeleteSearch.Click += CmdDeleteSearch_Click;
            cmdTestSearch.Click += CmdTestSearch_Click;
            dgvSavedSearches.SelectionChanged += DgvSavedSearches_SelectionChanged;
            PopulateSearchCharacterCombo();
            PopulateSearchTypeCombos();
            PopulateSavedSearchesGrid();

            // My Orders tab (10.4)
            cmbOrderCharacter.SelectedIndexChanged += CmbOrderCharacter_SelectedIndexChanged;
            cmbOrderType.Items.AddRange(new object[] { "All", "Sell", "Buy" });
            cmbOrderType.SelectedIndex = 0;
            cmbOrderType.SelectedIndexChanged += CmbOrderType_SelectedIndexChanged;
            dgvSellOrders.SelectionChanged += DgvSellOrders_SelectionChanged;
            PopulateOrderCharacterCombo();
            PopulateMyOrdersGrids();

            // Prices tab (10.6)
            cmdFetchPrices.Click += CmdFetchPrices_Click;
            cmdAutoPopulate.Click += CmdAutoPopulate_Click;
            PopulatePriceTypeCombos();
            PopulatePriceStatsGrid();
            PopulateAutoPopPlanCombo();

            // Alerts tab (10.8)
            cmdAddAlert.Click += CmdAddAlert_Click;
            cmdEditAlert.Click += CmdEditAlert_Click;
            cmdDeleteAlert.Click += CmdDeleteAlert_Click;
            dgvAlerts.SelectionChanged += DgvAlerts_SelectionChanged;
            PopulateAlertTypeCombos();
            PopulateAlertsGrid();

            // DataError handlers on all grids
            dgvListings.DataError += DgvGeneric_DataError;
            dgvTransactions.DataError += DgvGeneric_DataError;
            dgvSummary.DataError += DgvGeneric_DataError;
            dgvSavedSearches.DataError += DgvGeneric_DataError;
            dgvTestResults.DataError += DgvGeneric_DataError;
            dgvSellOrders.DataError += DgvGeneric_DataError;
            dgvBuyOrders.DataError += DgvGeneric_DataError;
            dgvPriceStats.DataError += DgvGeneric_DataError;
            dgvAlerts.DataError += DgvGeneric_DataError;

            // Events
            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.MarketDataChanged += OnMarketDataChanged;

            PopulateListingsGrid();
            PopulateTransactionsGrid();
            UpdateScopeStatus();
            UpdateStaleWarning();
        }

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }

        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.MarketDataChanged -= OnMarketDataChanged;
            base.OnFormClosed(e);
        }

        // -----------------------------------------------------------------------
        // Listings Tab (30.2)
        // -----------------------------------------------------------------------
        private void PopulateListingsGrid()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvListings.Rows.Clear();

            var listings = playerContext.GetCurrentPlayerReadOnlyListings();
            var refCounter = new MarketListingReferenceCounter(playerContext.MarketTransactionList);

            foreach (var listing in CollectionSortHelper.OrderReadOnlyMarketListings(listings))
            {
                string stationName = ResolveStationName(listing.StationUUID);
                string condition = listing.MaxHP > 0
                    ? string.Format("{0:F0}%", listing.CurrentHP * 100.0 / listing.MaxHP)
                    : "N/A";
                int refs = refCounter.CountReferences(listing.UUID);

                int rowIdx = dgvListings.Rows.Add(
                    stationName,
                    listing.ItemName,
                    listing.ItemType.ToString(),
                    listing.Quantity,
                    listing.PricePerUnit,
                    condition,
                    refs);
                dgvListings.Rows[rowIdx].Tag = listing;
            }

            sw.Stop();
            Log.Info("PERF PopulateListingsGrid: {0}ms items={1}", sw.ElapsedMilliseconds, listings.Count);
        }

        private void CmdListingAdd_Click(object sender, EventArgs e)
        {
            var request = new MarketListingCreateRequest
            {
                ItemName = "New Listing",
                Quantity = 1,
                PricePerUnit = 0m,
            };

            _marketListingService.CreateListing(request);
            Log.Info("Added new market listing");
        }

        private void CmdListingEdit_Click(object sender, EventArgs e)
        {
            if (dgvListings.SelectedRows.Count == 0) return;
            var listing = dgvListings.SelectedRows[0].Tag as ReadOnlyMarketListing;
            if (listing == null) return;

            using (var dlg = new FormListingEdit(listing, playerContext))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var request = new MarketListingUpdateRequest
                    {
                        ItemName = dlg.EditedItemName,
                        ItemType = dlg.EditedItemType,
                        ItemReferenceID = dlg.EditedItemReferenceID,
                        StationUUID = dlg.EditedStationUUID,
                        Quantity = dlg.EditedQuantity,
                        PricePerUnit = dlg.EditedPricePerUnit,
                        CurrentHP = dlg.EditedCurrentHP,
                        MaxHP = dlg.EditedMaxHP,
                        MaxRepairPercent = dlg.EditedMaxRepairPercent,
                    };

                    _marketListingService.UpdateListing(listing.UUID, request);
                    Log.Info("Edited market listing '{0}'", listing.ItemName);
                }
            }
        }

        private void CmdListingDelete_Click(object sender, EventArgs e)
        {
            if (dgvListings.SelectedRows.Count == 0) return;
            var listing = dgvListings.SelectedRows[0].Tag as ReadOnlyMarketListing;
            if (listing == null) return;

            var refCounter = new MarketListingReferenceCounter(playerContext.MarketTransactionList);
            int refs = refCounter.CountReferences(listing.UUID);
            if (refs > 0)
            {
                MessageBox.Show(
                    string.Format(
                        "Cannot delete listing \"{0}\" \u2014 it is referenced by {1} transaction(s).",
                        listing.ItemName,
                        refs),
                    "Delete Blocked",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                string.Format("Delete listing \"{0}\"?", listing.ItemName),
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            _marketListingService.DeleteListing(listing.UUID);
            Log.Info("Deleted market listing '{0}'", listing.ItemName);
        }

        // -----------------------------------------------------------------------
        // Record Sale Dialog (30.5)
        // -----------------------------------------------------------------------
        private void CmdRecordSale_Click(object sender, EventArgs e)
        {
            if (dgvListings.SelectedRows.Count == 0) return;
            var listing = dgvListings.SelectedRows[0].Tag as ReadOnlyMarketListing;
            if (listing == null) return;

            using (var dlg = new FormRecordSale(listing))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var tx = _marketListingService.RecordSale(
                        listing.UUID,
                        dlg.SaleQuantity,
                        dlg.SalePricePerUnit,
                        dlg.Counterparty,
                        dlg.CounterpartyFaction,
                        listing.StationUUID);

                    if (tx == null)
                    {
                        MessageBox.Show(
                            "Sale quantity exceeds listing availability.",
                            "Validation",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

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

            var transactions = playerContext.GetCurrentPlayerReadOnlyTransactions();

            string typeFilter = cmbTxType.SelectedItem?.ToString() ?? "All";
            string itemFilter = txtTxItem.Text.Trim();
            string counterpartyFilter = txtTxCounterparty.Text.Trim();
            string factionFilter = txtTxFaction.Text.Trim();
            string stationUUID = null;
            if (cmbTxStation.SelectedItem is StationEntry stEntry && !string.IsNullOrEmpty(stEntry.UUID))
                stationUUID = stEntry.UUID;

            DateTime? fromDate = dtpTxFrom.Checked ? dtpTxFrom.Value.Date : (DateTime?)null;
            DateTime? toDate = dtpTxTo.Checked ? dtpTxTo.Value.Date.AddDays(1) : (DateTime?)null;

            foreach (var tx in CollectionSortHelper.OrderReadOnlyMarketTransactionsByTimestamp(transactions))
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
                    ? string.Format("{0:F0}%", tx.CurrentHP * 100.0 / tx.MaxHP)
                    : string.Empty;
                string dateStr = string.Empty;
                if (DateTime.TryParse(tx.Timestamp, out DateTime parsed))
                    dateStr = parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

                dgvTransactions.Rows.Add(
                    dateStr,
                    tx.TransactionType.ToString(),
                    tx.ItemName,
                    tx.Quantity.ToString(),
                    tx.PricePerUnit.ToString("N2"),
                    tx.TotalPrice.ToString("N2"),
                    tx.Counterparty,
                    tx.CounterpartyFaction,
                    stationName,
                    condition);
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
                lblNetPL.ForeColor = Color.DarkGreen;
            else
                lblNetPL.ForeColor = Color.Red;

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

                dgvSummary.Rows.Add(
                    b.ItemName,
                    b.QuantitySold.ToString(),
                    b.SalesRevenue.ToString("N2"),
                    b.QuantityBought.ToString(),
                    b.PurchaseCost.ToString("N2"),
                    b.NetProfitLoss.ToString("N2"),
                    planValueStr,
                    marginStr);
            }

            if (plan != null && totalPlanValue > 0)
            {
                lblNetPL.Text += string.Format(
                    "  |  Plan Value: {0:N2}  |  Margin: {1:N2}",
                    totalPlanValue,
                    summary.TotalSalesRevenue - totalPlanValue);
            }

            sw.Stop();
            Log.Info("PERF CmdCompute_Click: {0}ms items={1}", sw.ElapsedMilliseconds, summary.ItemBreakdown.Count);
        }

        private void PopulatePricingPlanCombo()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbPricingPlan.DataSource = null;
            cmbPricingPlan.Items.Clear();

            var plans = playerContext.PricingPlanList
                .Where(p => p.OwnerUUID == playerContext.CurrentPlayerUUID)
                .ToList();
            plans = CollectionSortHelper.OrderPricingPlans(plans).ToList();

            var items = new List<KeyValuePair<string, string>>();
            items.Add(new KeyValuePair<string, string>(string.Empty, "(none)"));
            foreach (var plan in plans)
                items.Add(new KeyValuePair<string, string>(plan.UUID, plan.Name));

            cmbPricingPlan.DataSource = items;
            cmbPricingPlan.DisplayMember = "Value";
            cmbPricingPlan.ValueMember = "Key";
            sw.Stop();
            Log.Info("PERF PopulatePricingPlanCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private Models.PricingPlan GetSelectedPricingPlan()
        {
            string uuid = cmbPricingPlan.SelectedValue?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(uuid)) return null;
            return playerContext.PricingPlanList.FirstOrDefault(p => p.UUID == uuid);
        }

        // -----------------------------------------------------------------------
        // Saved Searches Tab (10.2)
        // -----------------------------------------------------------------------
        private void PopulateSearchCharacterCombo()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbSearchCharacter.Items.Clear();

            foreach (var profile in CollectionSortHelper.OrderPlayerProfiles(playerContext.PlayerProfileList))
            {
                cmbSearchCharacter.Items.Add(new CharacterEntry { Display = profile.Name, UUID = profile.UUID });
            }

            if (cmbSearchCharacter.Items.Count > 0)
                cmbSearchCharacter.SelectedIndex = 0;
            sw.Stop();
            Log.Info("PERF PopulateSearchCharacterCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateSearchTypeCombos()
        {
            var sw = Stopwatch.StartNew();
            cmbSearchType.Items.Clear();
            cmbSearchType.Items.Add("All");
            cmbSearchType.Items.Add("Resource");
            cmbSearchType.Items.Add("Commodity");
            cmbSearchType.Items.Add("Blueprint");
            cmbSearchType.Items.Add("ShipHull");
            cmbSearchType.Items.Add("ShipPart");
            cmbSearchType.Items.Add("Flatpack");
            cmbSearchType.SelectedIndex = 0;

            cmbSearchPurity.Items.Clear();
            cmbSearchPurity.Items.Add("(All)");
            cmbSearchPurity.Items.Add(GameConstants.PurityHigh);
            cmbSearchPurity.Items.Add(GameConstants.PurityMedium);
            cmbSearchPurity.Items.Add(GameConstants.PurityLow);
            cmbSearchPurity.Items.Add(GameConstants.PurityRefined);
            cmbSearchPurity.SelectedIndex = 0;

            cmbSearchOrderType.Items.Clear();
            cmbSearchOrderType.Items.Add("All");
            cmbSearchOrderType.Items.Add("Sell Only");
            cmbSearchOrderType.Items.Add("Buy Only");
            cmbSearchOrderType.SelectedIndex = 0;
            sw.Stop();
            Log.Info("PERF PopulateSearchTypeCombos: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateSavedSearchesGrid()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvSavedSearches.Rows.Clear();

            string characterUUID = GetSelectedSearchCharacterUUID();
            if (string.IsNullOrEmpty(characterUUID)) return;

            var searches = playerContext.MarketSyncData.SavedSearches
                .Where(s => s.CharacterUUID == characterUUID);

            foreach (var search in searches)
            {
                int rowIdx = dgvSavedSearches.Rows.Add(
                    search.Name,
                    search.ItemType.ToString(),
                    search.Enabled);
                dgvSavedSearches.Rows[rowIdx].Tag = search;
            }

            sw.Stop();
            Log.Info("PERF PopulateSavedSearchesGrid: {0}ms", sw.ElapsedMilliseconds);
        }

        private void DgvSavedSearches_SelectionChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (dgvSavedSearches.SelectedRows.Count == 0) return;

            var search = dgvSavedSearches.SelectedRows[0].Tag as SavedMarketSearch;
            if (search == null) return;

            using var guard = new ProgrammaticUpdateGuard(this);
            txtSearchName.Text = search.Name;
            SelectComboItem(cmbSearchType, search.ItemType == ItemType.ItemTypeEnum.None ? "All" : search.ItemType.ToString());
            SelectComboItem(cmbSearchPurity, string.IsNullOrEmpty(search.ResourcePurity) ? "(All)" : search.ResourcePurity);
            chkRunOnSync.Checked = search.Enabled;
        }

        private void CmdSaveSearch_Click(object sender, EventArgs e)
        {
            string characterUUID = GetSelectedSearchCharacterUUID();
            if (string.IsNullOrEmpty(characterUUID)) return;

            string name = txtSearchName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a search name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SavedMarketSearch existing = null;
            if (dgvSavedSearches.SelectedRows.Count > 0)
                existing = dgvSavedSearches.SelectedRows[0].Tag as SavedMarketSearch;

            if (existing != null)
            {
                existing.Name = name;
                existing.ItemType = ParseSearchItemType();
                existing.ResourcePurity = GetSelectedSearchPurity();
                existing.BuyOrdersOnly = GetSelectedOrderTypeFilter();
                existing.Enabled = chkRunOnSync.Checked;
            }
            else
            {
                var search = new SavedMarketSearch
                {
                    UUID = Guid.NewGuid().ToString(),
                    CharacterUUID = characterUUID,
                    Name = name,
                    ItemType = ParseSearchItemType(),
                    ResourcePurity = GetSelectedSearchPurity(),
                    BuyOrdersOnly = GetSelectedOrderTypeFilter(),
                    Enabled = chkRunOnSync.Checked,
                };
                playerContext.MarketSyncData.SavedSearches.Add(search);
            }

            playerContext.WriteContext();
            PopulateSavedSearchesGrid();
            Log.Info("Saved search '{0}' for character {1}", name, characterUUID);
        }

        private void CmdDeleteSearch_Click(object sender, EventArgs e)
        {
            if (dgvSavedSearches.SelectedRows.Count == 0) return;
            var search = dgvSavedSearches.SelectedRows[0].Tag as SavedMarketSearch;
            if (search == null) return;

            var result = MessageBox.Show(
                string.Format("Delete search \"{0}\"?", search.Name),
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            playerContext.MarketSyncData.SavedSearches.Remove(search);
            playerContext.WriteContext();
            PopulateSavedSearchesGrid();
            Log.Info("Deleted search '{0}'", search.Name);
        }

        private async void CmdTestSearch_Click(object sender, EventArgs e)
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvTestResults.Rows.Clear();

            var apiContext = GameApiContext.Instance;
            if (apiContext == null || apiContext.TypedClient == null)
            {
                MessageBox.Show(
                    "Game API is not configured or not connected.",
                    "Test Search",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string characterUUID = GetSelectedSearchCharacterUUID();
            if (string.IsNullOrEmpty(characterUUID))
            {
                return;
            }

            var credManager = new GameApiCredentialManager();
            var secureSecret = credManager.GetKey(characterUUID);
            if (secureSecret == null)
            {
                MessageBox.Show(
                    "No API credentials configured for this character.",
                    "Test Search",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var settings = PreferencesStore.GetInstance().Preferences.GameApiConnection;
            string plainSecret = CredentialStore.SecureStringToString(secureSecret);
            secureSecret.Dispose();

            var typedClient = apiContext.TypedClient;

            try
            {
                await typedClient.ExchangeTokenAsync(
                    settings.AppId, settings.ClientId, plainSecret, CancellationToken.None).ConfigureAwait(true);
            }
            catch (Common.Client.ApiHttpException ex)
            {
                MessageBox.Show(
                    "Token exchange failed: HTTP " + ex.StatusCode,
                    "Test Search",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Token exchange failed: " + ex.Message,
                    "Test Search",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            string orderType = cmbSearchOrderType.SelectedItem?.ToString();
            string view;
            if (string.Equals(orderType, "Buy", StringComparison.OrdinalIgnoreCase))
            {
                view = "buy";
            }
            else if (string.Equals(orderType, "Sell", StringComparison.OrdinalIgnoreCase))
            {
                view = "sell";
            }
            else
            {
                view = "all";
            }

            int? range = (int)nudRange.Value == 0 ? null : (int?)nudRange.Value;
            string search = string.IsNullOrWhiteSpace(txtSearchName.Text) ? null : txtSearchName.Text;

            try
            {
                var listings = await typedClient.GetMarketListingsAsync(
                    view, range, search, CancellationToken.None).ConfigureAwait(true);

                if (listings?.Listings != null)
                {
                    foreach (var listing in listings.Listings)
                    {
                        dgvTestResults.Rows.Add(
                            listing.Description ?? listing.Type,
                            listing.Price.ToString("N2"),
                            listing.AmountRemaining.ToString(),
                            listing.SellerName,
                            listing.LocationName);
                    }
                }

                Log.Info("Test search completed: {0} results displayed", dgvTestResults.Rows.Count);
            }
            catch (Common.Client.ApiHttpException ex)
            {
                MessageBox.Show(
                    "Market search failed: HTTP " + ex.StatusCode,
                    "Test Search",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Market search failed: " + ex.Message,
                    "Test Search",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // -----------------------------------------------------------------------
        // My Orders Tab (10.4)
        // -----------------------------------------------------------------------
        private void PopulateOrderCharacterCombo()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbOrderCharacter.Items.Clear();

            foreach (var profile in CollectionSortHelper.OrderPlayerProfiles(playerContext.PlayerProfileList))
            {
                cmbOrderCharacter.Items.Add(new CharacterEntry { Display = profile.Name, UUID = profile.UUID });
            }

            if (cmbOrderCharacter.Items.Count > 0)
                cmbOrderCharacter.SelectedIndex = 0;
            sw.Stop();
            Log.Info("PERF PopulateOrderCharacterCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateMyOrdersGrids()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvSellOrders.Rows.Clear();
            dgvBuyOrders.Rows.Clear();

            if (_marketDataService == null) return;

            string characterUUID = GetSelectedOrderCharacterUUID();
            if (string.IsNullOrEmpty(characterUUID)) return;

            var ownOrders = _marketDataService.GetOwnOrders(characterUUID);
            var syncMeta = playerContext.MarketSyncData.SyncMetadata
                .FirstOrDefault(m => m.CharacterUUID == characterUUID);

            if (syncMeta != null && !string.IsNullOrEmpty(syncMeta.LastSyncTimestamp))
            {
                lblSellOrders.Text = string.Format("Sell Orders (synced: {0})", FormatTimestamp(syncMeta.LastSyncTimestamp));
            }
            else
            {
                lblSellOrders.Text = "Sell Orders";
            }

            foreach (var order in ownOrders)
            {
                string status = string.Empty;
                if (order.BuyOrder)
                {
                    if (order.IsOutbid) status = "OUTBID";
                    int rowIdx = dgvBuyOrders.Rows.Add(
                        order.ItemName,
                        order.AmountRemaining ?? order.Quantity,
                        order.PricePerUnit,
                        order.LocationName,
                        status);
                    dgvBuyOrders.Rows[rowIdx].Tag = order;
                    if (order.IsOutbid)
                        dgvBuyOrders.Rows[rowIdx].DefaultCellStyle.BackColor = Color.MistyRose;
                }
                else
                {
                    if (order.IsUndercut) status = "UNDERCUT";
                    int rowIdx = dgvSellOrders.Rows.Add(
                        order.ItemName,
                        order.AmountRemaining ?? order.Quantity,
                        order.PricePerUnit,
                        order.LocationName,
                        status);
                    dgvSellOrders.Rows[rowIdx].Tag = order;
                    if (order.IsUndercut)
                        dgvSellOrders.Rows[rowIdx].DefaultCellStyle.BackColor = Color.LemonChiffon;
                }
            }

            sw.Stop();
            Log.Info("PERF PopulateMyOrdersGrids: {0}ms orders={1}", sw.ElapsedMilliseconds, ownOrders.Count);
        }

        private void CmbOrderCharacter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            PopulateMyOrdersGrids();
        }

        private void CmbOrderType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            PopulateMyOrdersGrids();
        }

        private void DgvSellOrders_SelectionChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            // Show competitor detail for selected sell order
            if (dgvSellOrders.SelectedRows.Count == 0) return;
            var order = dgvSellOrders.SelectedRows[0].Tag as MarketListing;
            if (order == null || !order.MarketId.HasValue) return;

            // Find competitors for this order
            var allListings = playerContext.SnapshotMarketListingList();
            var competitors = allListings
                .Where(l => l.CompetitorForMarketId.HasValue && l.CompetitorForMarketId.Value == order.MarketId.Value)
                .ToList();

            if (competitors.Count > 0)
            {
                Log.Debug("Order '{0}' has {1} competitor(s), lowest price: {2:N2}",
                    order.ItemName, competitors.Count, competitors[0].PricePerUnit);
            }
        }

        // -----------------------------------------------------------------------
        // Prices Tab (10.6)
        // -----------------------------------------------------------------------
        private void PopulatePriceTypeCombos()
        {
            var sw = Stopwatch.StartNew();
            cmbPriceType.Items.Clear();
            cmbPriceType.Items.Add("All");
            cmbPriceType.Items.Add("Resource");
            cmbPriceType.Items.Add("Commodity");
            cmbPriceType.Items.Add("Blueprint");
            cmbPriceType.SelectedIndex = 0;

            cmbPricePurity.Items.Clear();
            cmbPricePurity.Items.Add("(All)");
            cmbPricePurity.Items.Add(GameConstants.PurityHigh);
            cmbPricePurity.Items.Add(GameConstants.PurityMedium);
            cmbPricePurity.Items.Add(GameConstants.PurityLow);
            cmbPricePurity.Items.Add(GameConstants.PurityRefined);
            cmbPricePurity.SelectedIndex = 0;
            sw.Stop();
            Log.Info("PERF PopulatePriceTypeCombos: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulatePriceStatsGrid()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvPriceStats.Rows.Clear();

            var stats = playerContext.MarketSyncData.PriceStats;
            foreach (var stat in stats)
            {
                string fetchedStr = FormatTimestamp(stat.FetchedTimestamp);
                dgvPriceStats.Rows.Add(
                    stat.ItemName,
                    stat.LowPrice?.ToString("N2") ?? "--",
                    stat.AvgPrice?.ToString("N2") ?? "--",
                    stat.HighPrice?.ToString("N2") ?? "--",
                    stat.SampleCount.ToString(),
                    fetchedStr);
            }

            sw.Stop();
            Log.Info("PERF PopulatePriceStatsGrid: {0}ms rows={1}", sw.ElapsedMilliseconds, dgvPriceStats.Rows.Count);
        }

        private void CmdFetchPrices_Click(object sender, EventArgs e)
        {
            var apiContext = GameApiContext.Instance;
            if (apiContext == null || apiContext.TypedClient == null)
            {
                MessageBox.Show(
                    "Game API is not configured or not connected.",
                    "Fetch Prices",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // The actual async API call is handled via the sync service.
            // For manual fetches, display what is already stored.
            PopulatePriceStatsGrid();
            MessageBox.Show(
                "Price stats are fetched during market sync. Displaying stored data.",
                "Fetch Prices",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void PopulateAutoPopPlanCombo()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbAutoPopPlan.DataSource = null;
            cmbAutoPopPlan.Items.Clear();

            var plans = playerContext.PricingPlanList
                .Where(p => p.OwnerUUID == playerContext.CurrentPlayerUUID)
                .ToList();
            plans = CollectionSortHelper.OrderPricingPlans(plans).ToList();

            var items = new List<KeyValuePair<string, string>>();
            items.Add(new KeyValuePair<string, string>(string.Empty, "(select plan)"));
            foreach (var plan in plans)
                items.Add(new KeyValuePair<string, string>(plan.UUID, plan.Name));

            cmbAutoPopPlan.DataSource = items;
            cmbAutoPopPlan.DisplayMember = "Value";
            cmbAutoPopPlan.ValueMember = "Key";
            sw.Stop();
            Log.Info("PERF PopulateAutoPopPlanCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        // -----------------------------------------------------------------------
        // Auto-Populate Pricing Plan (10.10)
        // -----------------------------------------------------------------------
        private void CmdAutoPopulate_Click(object sender, EventArgs e)
        {
            string planUUID = cmbAutoPopPlan.SelectedValue?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(planUUID))
            {
                MessageBox.Show("Please select a pricing plan.", "Auto-Populate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_marketDataService == null) return;

            var confirmResult = MessageBox.Show(
                "Auto-populate the selected pricing plan with average market prices?\n\nThis will overwrite existing resource prices.",
                "Confirm Auto-Populate",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirmResult != DialogResult.Yes) return;

            var result = _marketDataService.AutoPopulatePricingPlan(planUUID, playerContext.CurrentPlayerUUID);
            playerContext.WriteContext();

            string message = string.Format(
                "Auto-populate complete.\n\nUpdated: {0} resource(s)\nMissing data: {1} resource(s)",
                result.UpdatedResources.Count,
                result.MissingResources.Count);

            if (result.MissingResources.Count > 0 && result.MissingResources.Count <= 10)
            {
                message += "\n\nMissing:\n" + string.Join("\n", result.MissingResources.Select(r => "  - " + r));
            }

            MessageBox.Show(message, "Auto-Populate Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Log.Info("Auto-populated plan: updated={0} missing={1}", result.UpdatedResources.Count, result.MissingResources.Count);
        }

        // -----------------------------------------------------------------------
        // Alerts Tab (10.8)
        // -----------------------------------------------------------------------
        private void PopulateAlertTypeCombos()
        {
            var sw = Stopwatch.StartNew();
            cmbAlertType.Items.Clear();
            cmbAlertType.Items.Add("Sell Order Appears");
            cmbAlertType.Items.Add("Buy Order Appears");
            cmbAlertType.SelectedIndex = 0;

            cmbAlertItemType.Items.Clear();
            cmbAlertItemType.Items.Add("Resource");
            cmbAlertItemType.Items.Add("Commodity");
            cmbAlertItemType.Items.Add("Blueprint");
            cmbAlertItemType.Items.Add("ShipHull");
            cmbAlertItemType.Items.Add("ShipPart");
            cmbAlertItemType.Items.Add("Flatpack");
            cmbAlertItemType.SelectedIndex = 0;

            cmbAlertPurity.Items.Clear();
            cmbAlertPurity.Items.Add("(Any)");
            cmbAlertPurity.Items.Add(GameConstants.PurityHigh);
            cmbAlertPurity.Items.Add(GameConstants.PurityMedium);
            cmbAlertPurity.Items.Add(GameConstants.PurityLow);
            cmbAlertPurity.Items.Add(GameConstants.PurityRefined);
            cmbAlertPurity.SelectedIndex = 0;

            cmbPriceCondition.Items.Clear();
            cmbPriceCondition.Items.Add("Any Price");
            cmbPriceCondition.Items.Add("At or Below");
            cmbPriceCondition.Items.Add("At or Above");
            cmbPriceCondition.SelectedIndex = 0;

            cmbAlertLocation.Items.Clear();
            cmbAlertLocation.Items.Add(new StationEntry { Display = "(Any Location)", UUID = string.Empty });
            foreach (var s in CollectionSortHelper.OrderStations(playerContext.GetCurrentPlayerStations()))
            {
                cmbAlertLocation.Items.Add(new StationEntry { Display = s.Name, UUID = s.UUID });
            }

            cmbAlertLocation.SelectedIndex = 0;
            chkAlertEnabled.Checked = true;
            sw.Stop();
            Log.Info("PERF PopulateAlertTypeCombos: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateAlertsGrid()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvAlerts.Rows.Clear();

            var alerts = playerContext.MarketSyncData.Alerts;
            foreach (var alert in alerts)
            {
                string condStr = alert.PriceCondition == PriceCondition.AnyPrice
                    ? "Any"
                    : string.Format("{0} {1:N2}", alert.PriceCondition, alert.PriceThreshold ?? 0);
                string lastTriggered = FormatTimestamp(alert.LastTriggeredTimestamp);

                int rowIdx = dgvAlerts.Rows.Add(
                    alert.Name,
                    alert.AlertType.ToString(),
                    alert.BaseItemTypeID,
                    condStr,
                    alert.Enabled,
                    lastTriggered);
                dgvAlerts.Rows[rowIdx].Tag = alert;
            }

            sw.Stop();
            Log.Info("PERF PopulateAlertsGrid: {0}ms rows={1}", sw.ElapsedMilliseconds, dgvAlerts.Rows.Count);
        }

        private void DgvAlerts_SelectionChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (dgvAlerts.SelectedRows.Count == 0) return;

            var alert = dgvAlerts.SelectedRows[0].Tag as MarketAlert;
            if (alert == null) return;

            using var guard = new ProgrammaticUpdateGuard(this);
            txtAlertName.Text = alert.Name;
            SelectComboItem(cmbAlertType, alert.AlertType == MarketAlertType.SellOrderAppears ? "Sell Order Appears" : "Buy Order Appears");
            SelectComboItem(cmbAlertItemType, alert.ItemType.ToString());
            chkAlertEnabled.Checked = alert.Enabled;
            txtPriceThreshold.Text = alert.PriceThreshold?.ToString("N2") ?? string.Empty;
            SelectComboItem(cmbPriceCondition, MapPriceConditionToDisplay(alert.PriceCondition));
        }

        private void CmdAddAlert_Click(object sender, EventArgs e)
        {
            string name = txtAlertName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter an alert name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var alert = new MarketAlert
            {
                UUID = Guid.NewGuid().ToString(),
                Name = name,
                AlertType = ParseAlertType(),
                ItemType = ParseAlertItemType(),
                BaseItemTypeID = cmbAlertItem.SelectedValue?.ToString() ?? string.Empty,
                ResourcePurity = GetSelectedAlertPurity(),
                PriceCondition = ParsePriceCondition(),
                PriceThreshold = ParsePriceThreshold(),
                StationUUID = GetSelectedAlertStationUUID(),
                Enabled = chkAlertEnabled.Checked,
            };

            playerContext.MarketSyncData.Alerts.Add(alert);
            playerContext.WriteContext();
            PopulateAlertsGrid();
            Log.Info("Added alert '{0}'", name);
        }

        private void CmdEditAlert_Click(object sender, EventArgs e)
        {
            if (dgvAlerts.SelectedRows.Count == 0) return;
            var alert = dgvAlerts.SelectedRows[0].Tag as MarketAlert;
            if (alert == null) return;

            alert.Name = txtAlertName.Text.Trim();
            alert.AlertType = ParseAlertType();
            alert.ItemType = ParseAlertItemType();
            alert.BaseItemTypeID = cmbAlertItem.SelectedValue?.ToString() ?? string.Empty;
            alert.ResourcePurity = GetSelectedAlertPurity();
            alert.PriceCondition = ParsePriceCondition();
            alert.PriceThreshold = ParsePriceThreshold();
            alert.StationUUID = GetSelectedAlertStationUUID();
            alert.Enabled = chkAlertEnabled.Checked;

            playerContext.WriteContext();
            PopulateAlertsGrid();
            Log.Info("Edited alert '{0}'", alert.Name);
        }

        private void CmdDeleteAlert_Click(object sender, EventArgs e)
        {
            if (dgvAlerts.SelectedRows.Count == 0) return;
            var alert = dgvAlerts.SelectedRows[0].Tag as MarketAlert;
            if (alert == null) return;

            var result = MessageBox.Show(
                string.Format("Delete alert \"{0}\"?", alert.Name),
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            playerContext.MarketSyncData.Alerts.Remove(alert);
            playerContext.WriteContext();
            PopulateAlertsGrid();
            Log.Info("Deleted alert '{0}'", alert.Name);
        }

        // -----------------------------------------------------------------------
        // Scope Status Display (10.12)
        // -----------------------------------------------------------------------
        private void UpdateScopeStatus()
        {
            var sw = Stopwatch.StartNew();
            var apiContext = GameApiContext.Instance;
            if (apiContext == null)
            {
                lblScopeStatus.Text = "API not configured. Configure credentials in Preferences.";
                sw.Stop();
                return;
            }

            var credManager = apiContext.CredentialManager;
            var configuredUUIDs = credManager.GetConfiguredPlayerUUIDs();
            if (configuredUUIDs.Count == 0)
            {
                lblScopeStatus.Text = "No characters configured. Add API credentials in Preferences.";
                sw.Stop();
                return;
            }

            var parts = new List<string>();
            foreach (string uuid in configuredUUIDs)
            {
                var profile = playerContext.FindPlayerProfile(uuid);
                string charName = profile?.Name ?? "(unknown)";

                var metadata = playerContext.MarketSyncData.SyncMetadata
                    .FirstOrDefault(m => m.CharacterUUID == uuid);

                if (metadata != null && metadata.GrantedScopes.Count > 0)
                {
                    parts.Add(string.Format("{0}: {1} scope(s)", charName, metadata.GrantedScopes.Count));
                }
                else
                {
                    parts.Add(string.Format("{0}: no scopes", charName));
                }
            }

            lblScopeStatus.Text = string.Join(" | ", parts);
            sw.Stop();
            Log.Info("PERF UpdateScopeStatus: {0}ms", sw.ElapsedMilliseconds);
        }

        // -----------------------------------------------------------------------
        // Stale Data Indicator (10.13)
        // -----------------------------------------------------------------------
        private void UpdateStaleWarning()
        {
            var syncMetadata = playerContext.MarketSyncData.SyncMetadata;
            if (syncMetadata == null || syncMetadata.Count == 0)
            {
                lblStaleWarning.Visible = false;
                return;
            }

            // Find the most recent sync timestamp across all characters
            DateTime? mostRecent = null;
            foreach (var meta in syncMetadata)
            {
                if (DateTime.TryParse(meta.LastSyncTimestamp, out DateTime ts))
                {
                    if (!mostRecent.HasValue || ts > mostRecent.Value)
                        mostRecent = ts;
                }
            }

            if (!mostRecent.HasValue)
            {
                lblStaleWarning.Visible = false;
                return;
            }

            double minutesSinceSync = (SystemClock.UtcNow - mostRecent.Value).TotalMinutes;
            if (minutesSinceSync > _staleThresholdMinutes)
            {
                lblStaleWarning.Text = string.Format(
                    "\u26A0 Market data is stale (last sync: {0} ago). Manual tabs remain operational.",
                    FormatTimeSpan(SystemClock.UtcNow - mostRecent.Value));
                lblStaleWarning.Visible = true;
            }
            else
            {
                lblStaleWarning.Visible = false;
            }
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
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            var stations = CollectionSortHelper.OrderStations(
                playerContext.GetCurrentPlayerStations()).ToList();

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
            sw.Stop();
            Log.Info("PERF PopulateStationCombos: {0}ms", sw.ElapsedMilliseconds);
        }

        private string FormatTimestamp(string isoTimestamp)
        {
            if (string.IsNullOrEmpty(isoTimestamp)) return string.Empty;
            if (DateTime.TryParse(isoTimestamp, out DateTime dt))
                return dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            return isoTimestamp;
        }

        private string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalDays >= 1)
                return string.Format("{0:F0}d {1}h", ts.TotalDays, ts.Hours);
            if (ts.TotalHours >= 1)
                return string.Format("{0:F0}h {1}m", ts.TotalHours, ts.Minutes);
            return string.Format("{0}m", (int)ts.TotalMinutes);
        }

        private string GetSelectedSearchCharacterUUID()
        {
            if (cmbSearchCharacter.SelectedItem is CharacterEntry entry)
                return entry.UUID;
            return string.Empty;
        }

        private string GetSelectedOrderCharacterUUID()
        {
            if (cmbOrderCharacter.SelectedItem is CharacterEntry entry)
                return entry.UUID;
            return string.Empty;
        }

        private string GetSelectedSearchPurity()
        {
            string val = cmbSearchPurity.SelectedItem?.ToString() ?? string.Empty;
            return val == "(All)" ? string.Empty : val;
        }

        private string GetSelectedAlertPurity()
        {
            string val = cmbAlertPurity.SelectedItem?.ToString() ?? string.Empty;
            return val == "(Any)" ? string.Empty : val;
        }

        private string GetSelectedAlertStationUUID()
        {
            if (cmbAlertLocation.SelectedItem is StationEntry entry)
                return entry.UUID;
            return string.Empty;
        }

        private bool? GetSelectedOrderTypeFilter()
        {
            string val = cmbSearchOrderType.SelectedItem?.ToString() ?? "All";
            if (val == "Buy Only") return true;
            if (val == "Sell Only") return false;
            return null;
        }

        private ItemType.ItemTypeEnum ParseSearchItemType()
        {
            string val = cmbSearchType.SelectedItem?.ToString() ?? "All";
            switch (val)
            {
                case "Resource": return ItemType.ItemTypeEnum.Resource;
                case "Commodity": return ItemType.ItemTypeEnum.Commodity;
                case "Blueprint": return ItemType.ItemTypeEnum.Blueprint;
                case "ShipHull": return ItemType.ItemTypeEnum.ShipHull;
                case "ShipPart": return ItemType.ItemTypeEnum.ShipPart;
                case "Flatpack": return ItemType.ItemTypeEnum.Flatpack;
                default: return ItemType.ItemTypeEnum.None;
            }
        }

        private MarketAlertType ParseAlertType()
        {
            string val = cmbAlertType.SelectedItem?.ToString() ?? string.Empty;
            return val.Contains("Buy") ? MarketAlertType.BuyOrderAppears : MarketAlertType.SellOrderAppears;
        }

        private ItemType.ItemTypeEnum ParseAlertItemType()
        {
            string val = cmbAlertItemType.SelectedItem?.ToString() ?? string.Empty;
            switch (val)
            {
                case "Resource": return ItemType.ItemTypeEnum.Resource;
                case "Commodity": return ItemType.ItemTypeEnum.Commodity;
                case "Blueprint": return ItemType.ItemTypeEnum.Blueprint;
                case "ShipHull": return ItemType.ItemTypeEnum.ShipHull;
                case "ShipPart": return ItemType.ItemTypeEnum.ShipPart;
                case "Flatpack": return ItemType.ItemTypeEnum.Flatpack;
                default: return ItemType.ItemTypeEnum.None;
            }
        }

        private PriceCondition ParsePriceCondition()
        {
            string val = cmbPriceCondition.SelectedItem?.ToString() ?? string.Empty;
            if (val.Contains("Below")) return PriceCondition.AtOrBelow;
            if (val.Contains("Above")) return PriceCondition.AtOrAbove;
            return PriceCondition.AnyPrice;
        }

        private decimal? ParsePriceThreshold()
        {
            if (decimal.TryParse(txtPriceThreshold.Text.Trim(), out decimal threshold))
                return threshold;
            return null;
        }

        private string MapPriceConditionToDisplay(PriceCondition condition)
        {
            switch (condition)
            {
                case PriceCondition.AtOrBelow: return "At or Below";
                case PriceCondition.AtOrAbove: return "At or Above";
                default: return "Any Price";
            }
        }

        private void SelectComboItem(ComboBox cmb, string value)
        {
            for (int i = 0; i < cmb.Items.Count; i++)
            {
                if (cmb.Items[i].ToString() == value)
                {
                    cmb.SelectedIndex = i;
                    return;
                }
            }
        }

        private MarketDataService CreateMarketDataService()
        {
            var empireContext = EmpireContext.GetInstance();
            if (empireContext?.SystemRepository == null) return null;
            var gridIndex = new SystemGridIndex(empireContext.SystemRepository);
            return new MarketDataService(playerContext, gridIndex);
        }

        private void DgvGeneric_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            Log.Warn("DataGridView DataError at [{0},{1}]: {2}", e.RowIndex, e.ColumnIndex, e.Exception?.Message);
            e.ThrowException = false;
        }

        // -----------------------------------------------------------------------
        // Events (30.7)
        // -----------------------------------------------------------------------
        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e)));
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }

            PopulateStationCombos();
            PopulatePricingPlanCombo();
            PopulateListingsGrid();
            PopulateTransactionsGrid();
            PopulateSearchCharacterCombo();
            PopulateSavedSearchesGrid();
            PopulateOrderCharacterCombo();
            PopulateMyOrdersGrids();
            PopulatePriceStatsGrid();
            PopulateAutoPopPlanCombo();
            PopulateAlertsGrid();
            UpdateScopeStatus();
            UpdateStaleWarning();
        }

        private void OnMarketDataChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => OnMarketDataChanged(sender, e)));
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }

            PopulateListingsGrid();
            PopulateTransactionsGrid();
            PopulateSavedSearchesGrid();
            PopulateMyOrdersGrids();
            PopulatePriceStatsGrid();
            PopulateAlertsGrid();
            UpdateStaleWarning();
        }

        // -----------------------------------------------------------------------
        // Context Menu Handlers (grid-context-menus spec, task 6.2)
        // -----------------------------------------------------------------------
        private void TsmiViewDetails_Click(object sender, EventArgs e)
        {
            if (dgvTransactions.SelectedRows.Count == 0) return;
        }

        private void DgvListings_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            if (e.RowIndex >= 0)
            {
                dgvListings.ClearSelection();
                dgvListings.Rows[e.RowIndex].Selected = true;
                dgvListings.CurrentCell = dgvListings.Rows[e.RowIndex].Cells[0];
            }
            else
            {
                dgvListings.ClearSelection();
            }
        }

        private void DgvTransactions_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            if (e.RowIndex >= 0)
            {
                dgvTransactions.ClearSelection();
                dgvTransactions.Rows[e.RowIndex].Selected = true;
                dgvTransactions.CurrentCell = dgvTransactions.Rows[e.RowIndex].Cells[0];
            }
            else
            {
                dgvTransactions.ClearSelection();
            }
        }

        private void CmsListings_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool hasSelection = dgvListings.CurrentRow != null;
            tsmiRecordSale.Enabled = hasSelection;
            tsmiEditListing.Enabled = hasSelection;
            tsmiDeleteListing.Enabled = hasSelection;
        }

        private void CmsTransactions_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool hasSelection = dgvTransactions.CurrentRow != null;
            tsmiViewDetails.Enabled = hasSelection;
        }

        // -----------------------------------------------------------------------
        // Helper classes
        // -----------------------------------------------------------------------
        private class StationEntry
        {
            public string Display { get; set; }
            public string UUID { get; set; }
            public override string ToString() => Display;
        }

        private class CharacterEntry
        {
            public string Display { get; set; }
            public string UUID { get; set; }
            public override string ToString() => Display;
        }
    }
}
