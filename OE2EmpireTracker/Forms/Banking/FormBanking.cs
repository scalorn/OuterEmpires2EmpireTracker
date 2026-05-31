using System;
using System.Diagnostics;
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

        public FormBanking()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;

            dgvTransactions.DataError += DgvTransactions_DataError;

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
        private void RefreshBalanceDisplay()
        {
            lblBalance.Text = string.Format("Balance: {0:N2}", playerContext.BankingBalance);
        }

        private void PopulateTransactionsGrid()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvTransactions.Rows.Clear();

            var transactions = CollectionSortHelper.OrderBankingTransactions(
                playerContext.BankingTransactionList);

            foreach (var tx in transactions)
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

            sw.Stop();
            Log.Info("PERF PopulateTransactionsGrid: {0}ms rows={1}", sw.ElapsedMilliseconds, transactions.Count);
        }

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
