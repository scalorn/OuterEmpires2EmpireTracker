using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.Banking
{
    public partial class FormBanking : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private int _isProgrammaticUpdate = 0;

        private PlayerContext playerContext;

        private Button _activePeriodButton;

        public FormBanking()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;

            dgvTransactions.DataError += DgvTransactions_DataError;

            PopulateTypeFilter();
            _activePeriodButton = btnAllTime;
            SetActivePeriodButton(btnAllTime);

            btn24h.Click += OnPeriodButtonClick;
            btn7d.Click += OnPeriodButtonClick;
            btn30d.Click += OnPeriodButtonClick;
            btnAllTime.Click += OnPeriodButtonClick;
            cboType.SelectedIndexChanged += OnFilterChanged;
            dtpFrom.ValueChanged += OnDateFilterChanged;
            dtpTo.ValueChanged += OnDateFilterChanged;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.BankingDataChanged += OnBankingDataChanged;

            RefreshBalanceDisplay();
            PopulateTransactionsGrid();
        }

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }

        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.BankingDataChanged -= OnBankingDataChanged;
            base.OnFormClosed(e);
        }

        // -----------------------------------------------------------------------
        // Data Display
        // -----------------------------------------------------------------------
        private static string FormatTransactionDate(string transactionDateTime)
        {
            if (string.IsNullOrEmpty(transactionDateTime))
            {
                return "(no date)";
            }

            if (DateTime.TryParse(transactionDateTime, out DateTime parsed))
            {
                return parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            }

            return "(no date)";
        }

        private void RefreshBalanceDisplay()
        {
            lblBalance.Text = string.Format("Balance: {0:N2}", playerContext.BankingBalance);
        }

        private void PopulateTransactionsGrid()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvTransactions.Rows.Clear();

            var filtered = GetFilteredTransactions();

            foreach (var tx in filtered)
            {
                string dateStr = FormatTransactionDate(tx.TransactionDateTime);
                string typeLabel = BankingTransactionTypes.GetLabel(tx.TransactionType, tx.Detail);

                dgvTransactions.Rows.Add(
                    dateStr,
                    typeLabel,
                    tx.Detail,
                    tx.CreditChange.ToString("N2"),
                    tx.OldBalance.ToString("N2"),
                    tx.NewBalance.ToString("N2"));
            }

            RefreshSummaryDisplay(filtered);

            sw.Stop();
            Log.Info("PERF PopulateTransactionsGrid: {0}ms rows={1}", sw.ElapsedMilliseconds, filtered.Count);
        }

        private void RefreshSummaryDisplay(IReadOnlyList<BankingTransaction> filteredTransactions)
        {
            var summary = BankingService.ComputeSummary(filteredTransactions);
            lblIncome.Text = string.Format("Income: {0:N2}", summary.TotalIncome);
            lblExpenses.Text = string.Format("Expenses: {0:N2}", summary.TotalExpenses);
            lblNet.Text = string.Format("Net: {0:N2}", summary.NetChange);
        }

        // -----------------------------------------------------------------------
        // Filters
        // -----------------------------------------------------------------------
        private void PopulateTypeFilter()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            cboType.Items.Clear();
            cboType.Items.Add("All");

            var sortedLabels = BankingTransactionTypes.TypeLabels.Values
                .OrderBy(label => label, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (string label in sortedLabels)
            {
                cboType.Items.Add(label);
            }

            cboType.SelectedIndex = 0;
        }

        private IReadOnlyList<BankingTransaction> GetFilteredTransactions()
        {
            int? typeFilter = GetSelectedTypeFilter();
            DateTime? fromDate = GetFromDate();
            DateTime? toDate = GetToDate();

            return BankingService.FilterTransactions(
                playerContext.BankingTransactionList,
                typeFilter,
                fromDate,
                toDate);
        }

        private int? GetSelectedTypeFilter()
        {
            if (cboType.SelectedIndex <= 0)
            {
                return null;
            }

            string selectedLabel = cboType.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedLabel))
            {
                return null;
            }

            foreach (var kvp in BankingTransactionTypes.TypeLabels)
            {
                if (kvp.Value == selectedLabel)
                {
                    return kvp.Key;
                }
            }

            return null;
        }

        private DateTime? GetFromDate()
        {
            if (_activePeriodButton == btnAllTime)
            {
                return dtpFrom.Checked ? dtpFrom.Value.Date : (DateTime?)null;
            }

            if (_activePeriodButton == btn24h)
            {
                return SystemClock.UtcNow.AddHours(-24);
            }

            if (_activePeriodButton == btn7d)
            {
                return SystemClock.UtcNow.AddDays(-7);
            }

            if (_activePeriodButton == btn30d)
            {
                return SystemClock.UtcNow.AddDays(-30);
            }

            return null;
        }

        private DateTime? GetToDate()
        {
            if (_activePeriodButton != btnAllTime)
            {
                return null;
            }

            return dtpTo.Checked ? dtpTo.Value.Date.AddDays(1).AddTicks(-1) : (DateTime?)null;
        }

        private void SetActivePeriodButton(Button active)
        {
            _activePeriodButton = active;

            btn24h.Font = new Font(btn24h.Font, FontStyle.Regular);
            btn7d.Font = new Font(btn7d.Font, FontStyle.Regular);
            btn30d.Font = new Font(btn30d.Font, FontStyle.Regular);
            btnAllTime.Font = new Font(btnAllTime.Font, FontStyle.Regular);

            active.Font = new Font(active.Font, FontStyle.Bold);
        }

        private void OnPeriodButtonClick(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;

            var button = (Button)sender;
            if (button == _activePeriodButton)
            {
                return;
            }

            using var guard = new ProgrammaticUpdateGuard(this);
            SetActivePeriodButton(button);

            if (button != btnAllTime)
            {
                dtpFrom.Checked = false;
                dtpTo.Checked = false;
            }

            guard.Release();
            PopulateTransactionsGrid();
        }

        private void OnFilterChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            PopulateTransactionsGrid();
        }

        private void OnDateFilterChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;

            if (((DateTimePicker)sender).Checked && _activePeriodButton != btnAllTime)
            {
                using var guard = new ProgrammaticUpdateGuard(this);
                SetActivePeriodButton(btnAllTime);
                guard.Release();
            }

            PopulateTransactionsGrid();
        }

        // -----------------------------------------------------------------------
        // Events
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

            RefreshBalanceDisplay();
            PopulateTransactionsGrid();
        }

        private void OnBankingDataChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => OnBankingDataChanged(sender, e)));
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }

            RefreshBalanceDisplay();
            PopulateTransactionsGrid();
        }

        private void DgvTransactions_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            Log.Warn("dgvTransactions DataError at [{0},{1}]: {2}", e.RowIndex, e.ColumnIndex, e.Exception?.Message);
            e.ThrowException = false;
        }
    }
}
