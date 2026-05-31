using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;

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
        /// <param name="apiClient">The game API client instance.</param>
        /// <param name="appId">The registered application GUID.</param>
        /// <param name="accessToken">The Bearer access token.</param>
        /// <param name="playerContext">The player context for persistence.</param>
        /// <returns>A <see cref="BankingImportResult"/> describing the outcome.</returns>
        public static async Task<BankingImportResult> ImportTransactionsAsync(
            GameApiClient apiClient,
            string appId,
            string accessToken,
            PlayerContext playerContext)
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

                (bool success, string json) apiResult;
                try
                {
                    apiResult = await apiClient.GetBankingTransactionsAsync(
                        appId,
                        accessToken,
                        offset,
                        PageSize).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "API error during banking transaction import at page {0}", currentPage);
                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                    result.FailedAtPage = currentPage;
                    break;
                }

                if (!apiResult.success || string.IsNullOrEmpty(apiResult.json))
                {
                    Log.Error("Banking transaction import failed at page {0}: API returned failure", currentPage);
                    result.Success = false;
                    result.ErrorMessage = "API returned failure at page " + currentPage;
                    result.FailedAtPage = currentPage;
                    break;
                }

                JArray transactions;
                try
                {
                    var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<JToken>>(apiResult.json);
                    if (envelope == null || !envelope.Success || envelope.Data == null)
                    {
                        Log.Error("Banking transaction import: envelope unsuccessful at page {0}", currentPage);
                        result.Success = false;
                        result.ErrorMessage = envelope?.ReturnString ?? "Envelope unsuccessful";
                        result.FailedAtPage = currentPage;
                        break;
                    }

                    transactions = envelope.Data.Type == JTokenType.Array
                        ? (JArray)envelope.Data
                        : envelope.Data["transactions"] as JArray;

                    if (transactions == null)
                    {
                        Log.Warn("Banking transaction import: no transactions array at page {0}, treating as empty", currentPage);
                        transactions = new JArray();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to parse banking transaction response at page {0}", currentPage);
                    result.Success = false;
                    result.ErrorMessage = "JSON parse error: " + ex.Message;
                    result.FailedAtPage = currentPage;
                    break;
                }

                foreach (var item in transactions)
                {
                    string transactionDT = item.Value<string>("transactionDT") ?? string.Empty;
                    decimal creditChange = item.Value<decimal>("creditChange");
                    string detail = item.Value<string>("detail") ?? string.Empty;

                    string compositeKey = string.Format("{0}|{1}|{2}", transactionDT, creditChange, detail);

                    if (existingKeys.Contains(compositeKey))
                    {
                        result.DuplicatesSkipped++;
                        continue;
                    }

                    var bankingTx = new BankingTransaction
                    {
                        UUID = Guid.NewGuid().ToString(),
                        OwnerUUID = playerContext.CurrentPlayerUUID,
                        TransactionDateTime = transactionDT,
                        CreditChange = creditChange,
                        OldBalance = item.Value<decimal>("oldBalance"),
                        NewBalance = item.Value<decimal>("newBalance"),
                        TransactionType = item.Value<int>("transactionType"),
                        Detail = detail,
                        CharacterId = item.Value<int?>("characterId"),
                        SystemObjectId = item.Value<int?>("systemObjectId"),
                        SystemId = item.Value<int?>("systemId"),
                        IsManualEntry = false,
                    };

                    playerContext.AddBankingTransaction(bankingTx);
                    existingKeys.Add(compositeKey);
                    result.TransactionsImported++;
                }

                result.PagesCompleted = currentPage;

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
        /// <param name="apiClient">The game API client instance.</param>
        /// <param name="appId">The registered application GUID.</param>
        /// <param name="accessToken">The Bearer access token.</param>
        /// <returns>The balance as a decimal, or null if the import failed.</returns>
        public static async Task<decimal?> ImportBalanceAsync(
            GameApiClient apiClient,
            string appId,
            string accessToken)
        {
            (bool success, string json) apiResult;
            try
            {
                apiResult = await apiClient.GetBankingBalanceAsync(appId, accessToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to import banking balance");
                return null;
            }

            if (!apiResult.success || string.IsNullOrEmpty(apiResult.json))
            {
                Log.Error("Banking balance import failed: API returned failure");
                return null;
            }

            try
            {
                var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<JToken>>(apiResult.json);
                if (envelope == null || !envelope.Success || envelope.Data == null)
                {
                    Log.Error("Banking balance import: envelope unsuccessful");
                    return null;
                }

                decimal balance = envelope.Data.Type == JTokenType.Object
                    ? envelope.Data.Value<decimal>("balance")
                    : envelope.Data.Value<decimal>();

                return balance;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to parse banking balance response");
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
        /// <returns>A new <see cref="BankingTransaction"/> with IsManualEntry set to true.</returns>
        /// <exception cref="ArgumentException">Thrown when creditChange is zero.</exception>
        public static BankingTransaction CreateManualTransaction(
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

            return new BankingTransaction
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
        /// Computes income, expense, and net totals for a set of transactions.
        /// </summary>
        /// <param name="transactions">The transactions to summarize.</param>
        /// <returns>A <see cref="BankingSummary"/> with computed totals.</returns>
        public static BankingSummary ComputeSummary(IEnumerable<BankingTransaction> transactions)
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
        public static IReadOnlyList<BankingTransaction> FilterTransactions(
            IReadOnlyList<BankingTransaction> transactions,
            int? transactionType,
            DateTime? fromDate,
            DateTime? toDate)
        {
            if (transactions == null || transactions.Count == 0)
            {
                return Array.Empty<BankingTransaction>();
            }

            IEnumerable<BankingTransaction> result = transactions;

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
