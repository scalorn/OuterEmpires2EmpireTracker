using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.Banking
{
    /// <summary>
    /// Modal dialog for manually entering a banking transaction.
    /// </summary>
    public partial class FormBankingEntry : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly List<int> typeCodes = new List<int>();

        private int _isProgrammaticUpdate = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormBankingEntry"/> class.
        /// </summary>
        public FormBankingEntry()
        {
            InitializeComponent();

            dtpDateTime.Value = SystemClock.UtcNow;
            dtpDateTime.MaxDate = SystemClock.UtcNow;

            PopulateTypeDropdown();

            btnOK.Click += BtnOK_Click;

            EmpireContext.PlayerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
        }

        /// <summary>
        /// Gets the created transaction after a successful dialog result.
        /// </summary>
        public BankingTransaction CreatedTransaction { get; private set; }

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }

        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            EmpireContext.PlayerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            base.OnFormClosed(e);
        }

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            // Modal dialog — close if player changes while open
            if (IsDisposed) return;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void PopulateTypeDropdown()
        {
            var sw = Stopwatch.StartNew();

            var sortedTypes = BankingTransactionTypes.TypeLabels
                .OrderBy(kvp => kvp.Value, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var kvp in sortedTypes)
            {
                cboType.Items.Add(kvp.Value);
                typeCodes.Add(kvp.Key);
            }

            if (cboType.Items.Count > 0)
            {
                cboType.SelectedIndex = 0;
            }

            sw.Stop();
            Log.Info("PERF PopulateTypeDropdown: {0}ms", sw.ElapsedMilliseconds);
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (!ValidateFields())
            {
                return;
            }

            decimal creditChange = decimal.Parse(
                txtCreditChange.Text.Trim(),
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture);

            int selectedTypeCode = typeCodes[cboType.SelectedIndex];
            string detail = txtDetail.Text.Trim();
            DateTime transactionDateTime = dtpDateTime.Value;

            string ownerUUID = EmpireContext.PlayerContext.CurrentPlayerUUID;

            CreatedTransaction = BankingService.CreateManualTransaction(
                ownerUUID,
                transactionDateTime,
                creditChange,
                selectedTypeCode,
                detail);

            Log.Info(
                "Manual transaction created: {0:N2} type={1} detail={2}",
                creditChange,
                selectedTypeCode,
                detail);

            DialogResult = DialogResult.OK;
            Close();
        }

        private bool ValidateFields()
        {
            lblError.Text = string.Empty;

            // Validate future date
            if (dtpDateTime.Value > SystemClock.UtcNow)
            {
                lblError.Text = "Date/time cannot be in the future.";
                dtpDateTime.Focus();
                return false;
            }

            // Validate credit change
            string creditText = txtCreditChange.Text.Trim();
            if (string.IsNullOrEmpty(creditText))
            {
                lblError.Text = "Credit change amount is required.";
                txtCreditChange.Focus();
                return false;
            }

            if (!decimal.TryParse(
                creditText,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out decimal creditValue))
            {
                lblError.Text = "Credit change must be a valid decimal number.";
                txtCreditChange.Focus();
                return false;
            }

            if (creditValue == 0m)
            {
                lblError.Text = "Credit change must be non-zero.";
                txtCreditChange.Focus();
                return false;
            }

            // Validate detail
            string detail = txtDetail.Text.Trim();
            if (string.IsNullOrEmpty(detail))
            {
                lblError.Text = "Detail is required.";
                txtDetail.Focus();
                return false;
            }

            // Validate type selection
            if (cboType.SelectedIndex < 0)
            {
                lblError.Text = "Transaction type must be selected.";
                cboType.Focus();
                return false;
            }

            return true;
        }
    }
}
