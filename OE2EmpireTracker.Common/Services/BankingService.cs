// <copyright file="BankingService.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using OE2EmpireTracker.Common.Client;
using OE2EmpireTracker.Common.Client.Generated;
using OE2EmpireTracker.Models;
using ApiBankingTransaction = OE2EmpireTracker.Common.Client.Generated.BankingTransaction;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Provides import, summary, and filter logic for banking transactions.
    /// </summary>
    public static class BankingService
    {
        private const int PageSize = 100;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Imports banking transactions from the game API, deduplicating against existing records.
        /// Paginates through all available pages and persists new transactions to the player context.
        /// </summary>
        /// <param name="typedClient">The typed game API client instance.</param>
        /// <param name="playerContext">The player context for persistence.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A <see cref="BankingImportResult"/> describing the outcome.</returns>
        public static async Task<BankingImportResult> ImportTransactionsAsync(
            IGameApiTypedClient typedClient,
            PlayerContext playerContext,
            CancellationToken ct = default)
        {
            var result = new BankingImportResult();

            var existingKeys = new HashSet<string>();
            foreach (var tx in playerContext.BankingTransactionList)
            {
                string key = string.Format(
                    "{0}|{1}|{2}",
                    tx.TransactionDateTime,
                    tx.CreditChange,
                    tx.Detail);
                existingKeys.Add(key);
            }

            int offset = 0;
            int currentPage = 0;

            while (true)
            {
                currentPage++;

                BankingTransactions response;
                try
                {
                    response = await typedClient.GetBankingTransactionsAsync(
                        offset, PageSize, ct).ConfigureAwait(false);
                }
                catch (ApiHttpException ex) when (ex.StatusCode == 401)
                {
                    Log.Warn("Banking import 401 at page {0}.", currentPage);
                    result.Success = false;
                    result.ErrorMessage = "Authentication failure (401) at page " + currentPage;
                    result.FailedAtPage = currentPage;
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "API error during banking transaction import at page {0}", currentPage);
                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                    result.FailedAtPage = currentPage;
                    break;
                }

                ICollection<ApiBankingTransaction> transactions = response.Transactions;
                if (transactions == null || transactions.Count == 0)
                {
                    Log.Warn("Banking transaction import: no transactions at page {0}, treating as empty", currentPage);
                    break;
                }

                int pageDuplicates = 0;
                foreach (var item in transactions)
                {
                    string transactionDT = item.TransactionDT.ToString("o", CultureInfo.InvariantCulture);
                    decimal creditChange = (decimal)item.CreditChange;
                    string detail = item.Detail ?? string.Empty;

                    string compositeKey = string.Format("{0}|{1}|{2}", transactionDT, creditChange, detail);

                    if (existingKeys.Contains(compositeKey))
                    {
                        result.DuplicatesSkipped++;
                        pageDuplicates++;
                        continue;
                    }

                    var bankingTx = new Models.BankingTransaction
                    {
                        UUID = Guid.NewGuid().ToString(),
                        OwnerUUID = playerContext.CurrentPlayerUUID,
                        TransactionDateTime = transactionDT,
                        CreditChange = creditChange,
                        OldBalance = (decimal)item.OldBalance,
                        NewBalance = (decimal)item.NewBalance,
                        TransactionType = item.TransactionType,
                        Detail = detail,
                        CharacterId = item.CharacterId,
                        SystemObjectId = item.SystemObjectId,
                        SystemId = item.SystemId,
                        IsManualEntry = false,
                    };

                    playerContext.AddBankingTransaction(bankingTx);
                    playerContext.MarkDirty<Models.BankingTransaction>(bankingTx.UUID);
                    existingKeys.Add(compositeKey);
                    result.TransactionsImported++;
                }

                result.PagesCompleted = currentPage;

                // Stop early if the entire page was duplicates — the API returns
                // transactions newest-first, so once we hit a full page of known
                // records everything beyond is also already imported.
                int newOnThisPage = transactions.Count - pageDuplicates;
                if (transactions.Count > 0 && newOnThisPage == 0)
                {
                    Log.Info(
                        "Banking import: page {0} was entirely duplicates, stopping incremental import",
                        currentPage);
                    break;
                }

                if (transactions.Count < PageSize)
                {
                    break;
                }

                offset += PageSize;
            }

            playerContext.WriteContext();
            playerContext.OnBankingDataChanged();

            Log.Info(
                "Banking import complete: {0} imported, {1} duplicates skipped, {2} pages, success={3}",
                result.TransactionsImported,
                result.DuplicatesSkipped,
                result.PagesCompleted,
                result.Success);

            return result;
        }

        /// <summary>
        /// Imports the current banking balance from the game API.
        /// </summary>
        /// <param name="typedClient">The typed game API client instance.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The balance as a decimal, or null if the import failed.</returns>
        public static async Task<decimal?> ImportBalanceAsync(
            IGameApiTypedClient typedClient,
            CancellationToken ct = default)
        {
            try
            {
                var response = await typedClient.GetBankingBalanceAsync(ct).ConfigureAwait(false);
                return (decimal)response.Balance;
            }
            catch (ApiHttpException ex) when (ex.StatusCode == 401)
            {
                Log.Error("Banking balance import: authentication failure (401)");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to import banking balance");
                return null;
            }
        }

        /// <summary>
        /// Creates a manually entered banking transaction.
        /// </summary>
        /// <param name="ownerUUID">The UUID of the player who owns this transaction.</param>
        /// <param name="transactionDateTime">The date and time of the transaction.</param>
        /// <param name="creditChange">The credit change amount (must be non-zero).</param>
        /// <param name="transactionType">The transaction type code.</param>
        /// <param name="detail">The transaction detail text.</param>
        /// <returns>A new <see cref="Models.BankingTransaction"/> with IsManualEntry set to true.</returns>
        /// <exception cref="ArgumentException">Thrown when creditChange is zero.</exception>
        public static Models.BankingTransaction CreateManualTransaction(
            string ownerUUID,
            DateTime transactionDateTime,
            decimal creditChange,
            int transactionType,
            string detail)
        {
            if (creditChange == 0m)
            {
                throw new ArgumentException("Credit change must be non-zero.", nameof(creditChange));
            }

            return new Models.BankingTransaction
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = ownerUUID,
                TransactionDateTime = transactionDateTime.ToString("o", CultureInfo.InvariantCulture),
                CreditChange = creditChange,
                OldBalance = 0m,
                NewBalance = 0m,
                TransactionType = transactionType,
                Detail = detail,
                IsManualEntry = true,
            };
        }

        /// <summary>
        /// Adds a manually-created transaction to the player context, persists, and raises BankingDataChanged.
        /// This is the single mutation path for manual transaction entry.
        /// </summary>
        /// <param name="playerContext">The player context to mutate.</param>
        /// <param name="transaction">The transaction to add.</param>
        public static void AddManualTransaction(PlayerContext playerContext, Models.BankingTransaction transaction)
        {
            if (playerContext == null)
            {
                throw new ArgumentNullException(nameof(playerContext));
            }

            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            playerContext.AddBankingTransaction(transaction);
            playerContext.MarkDirty<Models.BankingTransaction>(transaction.UUID);
            playerContext.WriteContext();
            playerContext.OnBankingDataChanged();
        }

        /// <summary>
        /// Computes income, expense, and net totals for a set of transactions.
        /// </summary>
        /// <param name="transactions">The transactions to summarize.</param>
        /// <returns>A <see cref="BankingSummary"/> with computed totals.</returns>
        public static BankingSummary ComputeSummary(IEnumerable<Models.BankingTransaction> transactions)
        {
            if (transactions == null)
            {
                return new BankingSummary();
            }

            decimal totalIncome = 0m;
            decimal totalExpenses = 0m;

            foreach (var tx in transactions)
            {
                if (tx.CreditChange > 0m)
                {
                    totalIncome += tx.CreditChange;
                }
                else if (tx.CreditChange < 0m)
                {
                    totalExpenses += Math.Abs(tx.CreditChange);
                }
            }

            return new BankingSummary
            {
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                NetChange = totalIncome - totalExpenses,
            };
        }

        /// <summary>
        /// Filters transactions by type and date range using AND logic,
        /// returning results sorted by TransactionDateTime descending.
        /// </summary>
        /// <param name="transactions">The full list of transactions to filter.</param>
        /// <param name="transactionType">Optional transaction type code to filter by.</param>
        /// <param name="fromDate">Optional inclusive start date.</param>
        /// <param name="toDate">Optional inclusive end date.</param>
        /// <returns>Filtered and sorted list of transactions.</returns>
        public static IReadOnlyList<Models.BankingTransaction> FilterTransactions(
            IReadOnlyList<Models.BankingTransaction> transactions,
            int? transactionType,
            DateTime? fromDate,
            DateTime? toDate)
        {
            if (transactions == null || transactions.Count == 0)
            {
                return Array.Empty<Models.BankingTransaction>();
            }

            IEnumerable<Models.BankingTransaction> result = transactions;

            if (transactionType.HasValue)
            {
                int typeFilter = transactionType.Value;
                result = result.Where(tx => tx.TransactionType == typeFilter);
            }

            if (fromDate.HasValue)
            {
                DateTime from = fromDate.Value;
                result = result.Where(tx => ParseTransactionDateTime(tx.TransactionDateTime) >= from);
            }

            if (toDate.HasValue)
            {
                DateTime to = toDate.Value;
                result = result.Where(tx => ParseTransactionDateTime(tx.TransactionDateTime) <= to);
            }

            return result
                .OrderByDescending(tx => ParseTransactionDateTime(tx.TransactionDateTime))
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Parses a TransactionDateTime string (ISO 8601) to a DateTime value.
        /// Returns DateTime.MinValue (epoch substitute) for null or unparseable values.
        /// </summary>
        /// <param name="dateTimeString">The ISO 8601 date/time string.</param>
        /// <returns>The parsed DateTime, or DateTime.MinValue if parsing fails.</returns>
        internal static DateTime ParseTransactionDateTime(string dateTimeString)
        {
            if (string.IsNullOrEmpty(dateTimeString))
            {
                return DateTime.MinValue;
            }

            if (DateTime.TryParse(
                dateTimeString,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTime parsed))
            {
                return parsed;
            }

            Log.Warn("Failed to parse TransactionDateTime: {0}", dateTimeString);
            return DateTime.MinValue;
        }
    }
}
