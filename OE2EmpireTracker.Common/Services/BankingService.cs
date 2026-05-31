using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Provides import, summary, and filter logic for banking transactions.
    /// </summary>
    public static class BankingService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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
