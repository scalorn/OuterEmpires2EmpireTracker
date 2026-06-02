using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
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

            btnGroupHourly.CheckedChanged += OnGroupingChanged;
            btnGroupDaily.CheckedChanged += OnGroupingChanged;
            chkNetChange.CheckedChanged += OnCashFlowSeriesToggled;
            chkCumulative.CheckedChanged += OnCashFlowSeriesToggled;
            txtDetailFilter.TextChanged += OnDetailFilterChanged;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.BankingDataChanged += OnBankingDataChanged;

            btnAddTransaction.Click += BtnAddTransaction_Click;

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

        private static Dictionary<DateTime, List<BankingTransaction>> GroupTransactionsByTimeBucket(
            IReadOnlyList<BankingTransaction> transactions,
            bool groupByHour)
        {
            var buckets = new Dictionary<DateTime, List<BankingTransaction>>();

            foreach (var tx in transactions)
            {
                DateTime dt;
                if (string.IsNullOrEmpty(tx.TransactionDateTime) ||
                    !DateTime.TryParse(tx.TransactionDateTime, out dt))
                {
                    dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                }

                DateTime bucketKey = groupByHour
                    ? new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, dt.Kind)
                    : dt.Date;

                if (!buckets.ContainsKey(bucketKey))
                {
                    buckets[bucketKey] = new List<BankingTransaction>();
                }

                buckets[bucketKey].Add(tx);
            }

            return buckets;
        }

        private static string FormatBucketLabel(DateTime bucket, bool groupByHour)
        {
            if (groupByHour)
            {
                return bucket.ToLocalTime().ToString("MM-dd HH:mm");
            }

            return bucket.ToLocalTime().ToString("yyyy-MM-dd");
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
            RefreshCharts(filtered);

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
        // Charts
        // -----------------------------------------------------------------------
        private void RefreshCharts(IReadOnlyList<BankingTransaction> filteredTransactions)
        {
            var sw = Stopwatch.StartNew();

            bool groupByHour = btnGroupHourly.Checked;
            RefreshCashFlowChart(filteredTransactions, groupByHour);
            RefreshIncomeExpensesChart(filteredTransactions, groupByHour);
            RefreshTypeBreakdownChart(filteredTransactions);

            sw.Stop();
            Log.Info("PERF RefreshCharts: {0}ms rows={1}", sw.ElapsedMilliseconds, filteredTransactions.Count);
        }

        private void RefreshCashFlowChart(IReadOnlyList<BankingTransaction> transactions, bool groupByHour)
        {
            chartCashFlow.Series.Clear();

            if (transactions.Count == 0)
            {
                return;
            }

            var buckets = GroupTransactionsByTimeBucket(transactions, groupByHour);

            var netSeries = new Series("Net Change")
            {
                ChartType = SeriesChartType.Line,
                Color = ChartColors.WongPalette[0],
                BorderWidth = 2,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 4,
            };

            var cumulativeSeries = new Series("Cumulative")
            {
                ChartType = SeriesChartType.Line,
                Color = ChartColors.WongPalette[4],
                BorderWidth = 2,
                BorderDashStyle = ChartDashStyle.Dash,
            };

            decimal runningTotal = 0m;
            foreach (var bucket in buckets.OrderBy(b => b.Key))
            {
                decimal netChange = bucket.Value.Sum(t => t.CreditChange);
                runningTotal += netChange;

                string label = FormatBucketLabel(bucket.Key, groupByHour);
                netSeries.Points.AddXY(label, (double)netChange);
                cumulativeSeries.Points.AddXY(label, (double)runningTotal);
            }

            chartCashFlow.Series.Add(netSeries);
            chartCashFlow.Series.Add(cumulativeSeries);

            // Apply visibility from checkboxes
            netSeries.Enabled = chkNetChange.Checked;
            cumulativeSeries.Enabled = chkCumulative.Checked;
        }

        private void RefreshIncomeExpensesChart(IReadOnlyList<BankingTransaction> transactions, bool groupByHour)
        {
            chartIncomeExpenses.Series.Clear();

            if (transactions.Count == 0)
            {
                return;
            }

            var buckets = GroupTransactionsByTimeBucket(transactions, groupByHour);

            var incomeSeries = new Series("Income")
            {
                ChartType = SeriesChartType.Column,
                Color = Color.FromArgb(0, 158, 115),
            };

            var expenseSeries = new Series("Expenses")
            {
                ChartType = SeriesChartType.Column,
                Color = Color.FromArgb(213, 94, 0),
            };

            foreach (var bucket in buckets.OrderBy(b => b.Key))
            {
                decimal income = bucket.Value
                    .Where(t => t.CreditChange > 0)
                    .Sum(t => t.CreditChange);
                decimal expenses = bucket.Value
                    .Where(t => t.CreditChange < 0)
                    .Sum(t => Math.Abs(t.CreditChange));

                string label = FormatBucketLabel(bucket.Key, groupByHour);
                incomeSeries.Points.AddXY(label, (double)income);
                expenseSeries.Points.AddXY(label, (double)expenses);
            }

            chartIncomeExpenses.Series.Add(incomeSeries);
            chartIncomeExpenses.Series.Add(expenseSeries);
        }

        private void RefreshTypeBreakdownChart(IReadOnlyList<BankingTransaction> transactions)
        {
            chartTypeBreakdown.Series.Clear();

            if (transactions.Count == 0)
            {
                return;
            }

            var typeGroups = transactions
                .GroupBy(t => t.TransactionType)
                .Select(g => new
                {
                    TypeCode = g.Key,
                    Label = BankingTransactionTypes.GetLabel(g.Key, string.Empty),
                    Total = g.Sum(t => Math.Abs(t.CreditChange)),
                })
                .Where(g => g.Total > 0)
                .OrderByDescending(g => g.Total)
                .ToList();

            var pieSeries = new Series("TypeBreakdown")
            {
                ChartType = SeriesChartType.Pie,
            };

            pieSeries["PieLabelStyle"] = "Outside";

            int colorIndex = 0;
            foreach (var group in typeGroups)
            {
                int pointIndex = pieSeries.Points.AddXY(group.Label, (double)group.Total);
                var point = pieSeries.Points[pointIndex];
                point.Color = ChartColors.WongPalette[colorIndex % ChartColors.WongPalette.Length];
                point.Label = string.Format("{0}\n{1:P1}", group.Label, 0.0);
                colorIndex++;
            }

            // Calculate percentages
            decimal grandTotal = typeGroups.Sum(g => g.Total);
            if (grandTotal > 0)
            {
                for (int i = 0; i < typeGroups.Count; i++)
                {
                    decimal pct = typeGroups[i].Total / grandTotal;
                    pieSeries.Points[i].Label = string.Format(
                        "{0}\n{1:P1}",
                        typeGroups[i].Label,
                        pct);
                }
            }

            chartTypeBreakdown.Series.Add(pieSeries);
        }

        private void OnGroupingChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0)
            {
                return;
            }

            var filtered = GetFilteredTransactions();
            RefreshCharts(filtered);
        }

        private void OnCashFlowSeriesToggled(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0)
            {
                return;
            }

            foreach (var series in chartCashFlow.Series)
            {
                if (series.Name == "Net Change")
                {
                    series.Enabled = chkNetChange.Checked;
                }
                else if (series.Name == "Cumulative")
                {
                    series.Enabled = chkCumulative.Checked;
                }
            }
        }

        private void OnDetailFilterChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0)
            {
                return;
            }

            PopulateTransactionsGrid();
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

            var filtered = BankingService.FilterTransactions(
                playerContext.BankingTransactionList,
                typeFilter,
                fromDate,
                toDate);

            string detailSearch = txtDetailFilter.Text.Trim();
            if (!string.IsNullOrEmpty(detailSearch))
            {
                filtered = filtered
                    .Where(tx => tx.Detail != null && tx.Detail.IndexOf(detailSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList()
                    .AsReadOnly();
            }

            return filtered;
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
        // Actions
        // -----------------------------------------------------------------------
        private void BtnAddTransaction_Click(object sender, EventArgs e)
        {
            using (var dialog = new FormBankingEntry())
            {
                if (dialog.ShowDialog(this) == DialogResult.OK && dialog.CreatedTransaction != null)
                {
                    playerContext.AddBankingTransaction(dialog.CreatedTransaction);
                    playerContext.WriteContext();
                    playerContext.OnBankingDataChanged();
                    Log.Info("Manual transaction added: {0}", dialog.CreatedTransaction.UUID);
                }
            }
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
