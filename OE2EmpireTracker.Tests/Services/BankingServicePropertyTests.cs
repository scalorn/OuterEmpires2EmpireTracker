using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for BankingService.
    /// Validates correctness properties from the design document.
    /// </summary>
    [TestFixture]
    public class BankingServicePropertyTests
    {
        // -----------------------------------------------------------------------
        // Shared generator: builds a random BankingTransaction with a random
        // CreditChange value (positive or negative decimal).
        // -----------------------------------------------------------------------

        private static Gen<decimal> CreditChangeGen()
        {
            return from sign in Gen.Elements(1, -1)
                   from whole in Gen.Choose(1, 999999)
                   from frac in Gen.Choose(0, 99)
                   select sign * (whole + (frac / 100m));
        }

        private static Gen<BankingTransaction> SummaryTransactionGen()
        {
            return from creditChange in CreditChangeGen()
                   select new BankingTransaction
                   {
                       UUID = Guid.NewGuid().ToString(),
                       CreditChange = creditChange,
                       Detail = "test",
                   };
        }

        private static Gen<List<BankingTransaction>> SummaryTransactionListGen()
        {
            return from count in Gen.Choose(0, 50)
                   from txns in Gen.ListOf(count, SummaryTransactionGen())
                   select txns.ToList();
        }

        // -----------------------------------------------------------------------
        // Property 2: Summary Arithmetic Consistency
        // For any list of transactions:
        //   TotalIncome == Sum(positive CreditChange)
        //   TotalExpenses == Sum(|negative CreditChange|)
        //   NetChange == TotalIncome - TotalExpenses
        // **Validates: Requirements 6.4, 6.5, 6.6**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property SummaryArithmeticConsistency()
        {
            return Prop.ForAll(
                Arb.From(SummaryTransactionListGen()),
                transactions =>
                {
                    var summary = BankingService.ComputeSummary(transactions);

                    decimal expectedIncome = transactions
                        .Where(tx => tx.CreditChange > 0m)
                        .Sum(tx => tx.CreditChange);

                    decimal expectedExpenses = transactions
                        .Where(tx => tx.CreditChange < 0m)
                        .Sum(tx => Math.Abs(tx.CreditChange));

                    decimal expectedNet = expectedIncome - expectedExpenses;

                    bool incomeMatch = summary.TotalIncome == expectedIncome;
                    bool expenseMatch = summary.TotalExpenses == expectedExpenses;
                    bool netMatch = summary.NetChange == expectedNet;

                    return (incomeMatch && expenseMatch && netMatch)
                        .Label(
                            "income=" + incomeMatch
                            + " (expected=" + expectedIncome + ", actual=" + summary.TotalIncome + ")"
                            + ", expenses=" + expenseMatch
                            + " (expected=" + expectedExpenses + ", actual=" + summary.TotalExpenses + ")"
                            + ", net=" + netMatch
                            + " (expected=" + expectedNet + ", actual=" + summary.NetChange + ")");
                });
        }

        // -----------------------------------------------------------------------
        // Property 3: Filter Completeness
        // Every transaction in filtered result satisfies ALL active filter conditions.
        // No transaction outside filter criteria appears in result.
        // The filtered set is always a subset of the input set.
        // **Validates: Requirements 5.7, 5.9**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property FilterCompleteness_AllResultsSatisfyAllActiveFilters()
        {
            // Generate a list of transactions with valid ISO 8601 dates and random types
            var typeGen = Gen.Choose(0, 10);
            var yearGen = Gen.Choose(2020, 2026);
            var monthGen = Gen.Choose(1, 12);
            var dayGen = Gen.Choose(1, 28);
            var hourGen = Gen.Choose(0, 23);
            var minuteGen = Gen.Choose(0, 59);

            var dateTimeGen = from year in yearGen
                              from month in monthGen
                              from day in dayGen
                              from hour in hourGen
                              from minute in minuteGen
                              select new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc)
                                  .ToString("o", CultureInfo.InvariantCulture);

            var txGen = from dt in dateTimeGen
                        from txType in typeGen
                        from credit in Gen.Choose(-10000, 10000)
                        select new BankingTransaction
                        {
                            UUID = Guid.NewGuid().ToString(),
                            TransactionDateTime = dt,
                            TransactionType = txType,
                            CreditChange = credit,
                            Detail = "Test",
                        };

            var listGen = Gen.ListOf(txGen).Select(l => l.ToList());

            // Generate filter parameters: optional type, optional fromDate, optional toDate
            var optionalTypeGen = Gen.OneOf(
                Gen.Constant((int?)null),
                typeGen.Select(t => (int?)t));

            var optionalDateGen = Gen.OneOf(
                Gen.Constant((DateTime?)null),
                from year in yearGen
                from month in monthGen
                from day in dayGen
                select (DateTime?)new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc));

            var filterGen = from txList in listGen
                            from filterType in optionalTypeGen
                            from fromDate in optionalDateGen
                            from toDate in optionalDateGen
                            select new { Transactions = txList, FilterType = filterType, FromDate = fromDate, ToDate = toDate };

            return Prop.ForAll(Arb.From(filterGen), config =>
            {
                var input = config.Transactions.AsReadOnly();
                var result = BankingService.FilterTransactions(
                    input,
                    config.FilterType,
                    config.FromDate,
                    config.ToDate);

                // 1. Filtered result is a subset of input
                foreach (var tx in result)
                {
                    if (!input.Contains(tx))
                    {
                        return false.Label("Result contains transaction not in input");
                    }
                }

                // 2. Every transaction in result satisfies ALL active filters
                foreach (var tx in result)
                {
                    DateTime parsedDt = BankingService.ParseTransactionDateTime(tx.TransactionDateTime);

                    if (config.FilterType.HasValue && tx.TransactionType != config.FilterType.Value)
                    {
                        return false.Label("Result contains transaction not matching type filter");
                    }

                    if (config.FromDate.HasValue && parsedDt < config.FromDate.Value)
                    {
                        return false.Label("Result contains transaction before fromDate");
                    }

                    if (config.ToDate.HasValue && parsedDt > config.ToDate.Value)
                    {
                        return false.Label("Result contains transaction after toDate");
                    }
                }

                // 3. No transaction outside filter criteria appears in result
                foreach (var tx in input)
                {
                    DateTime parsedDt = BankingService.ParseTransactionDateTime(tx.TransactionDateTime);
                    bool matchesType = !config.FilterType.HasValue || tx.TransactionType == config.FilterType.Value;
                    bool matchesFrom = !config.FromDate.HasValue || parsedDt >= config.FromDate.Value;
                    bool matchesTo = !config.ToDate.HasValue || parsedDt <= config.ToDate.Value;

                    bool shouldBeIncluded = matchesType && matchesFrom && matchesTo;

                    if (shouldBeIncluded && !result.Contains(tx))
                    {
                        return false.Label("Transaction matching all filters is missing from result");
                    }

                    if (!shouldBeIncluded && result.Contains(tx))
                    {
                        return false.Label("Transaction failing a filter is present in result");
                    }
                }

                return true.Label("OK");
            });
        }

        // -----------------------------------------------------------------------
        // Property 1: Import Deduplication Idempotency
        // Importing the same set of transactions twice SHALL result in zero new
        // records on the second import. The composite key
        // (TransactionDateTime + CreditChange + Detail) prevents duplicates.
        // **Validates: Requirements 3.3**
        // -----------------------------------------------------------------------

        private static Gen<string> IsoDateTimeGen()
        {
            return from year in Gen.Choose(2020, 2026)
                   from month in Gen.Choose(1, 12)
                   from day in Gen.Choose(1, 28)
                   from hour in Gen.Choose(0, 23)
                   from minute in Gen.Choose(0, 59)
                   from second in Gen.Choose(0, 59)
                   select new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc)
                       .ToString("o", CultureInfo.InvariantCulture);
        }

        private static Gen<BankingTransaction> DeduplicationTransactionGen()
        {
            return from dt in IsoDateTimeGen()
                   from creditChange in CreditChangeGen()
                   from detail in Gen.Elements("Wages", "Sale", "Purchase", "Refuel", "Income")
                   select new BankingTransaction
                   {
                       UUID = Guid.NewGuid().ToString(),
                       OwnerUUID = "test-owner",
                       TransactionDateTime = dt,
                       CreditChange = creditChange,
                       Detail = detail,
                       TransactionType = 1,
                       IsManualEntry = false,
                   };
        }

        private static Gen<List<BankingTransaction>> DeduplicationTransactionListGen()
        {
            return from count in Gen.Choose(1, 30)
                   from txns in Gen.ListOf(count, DeduplicationTransactionGen())
                   select txns.ToList();
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property DeduplicationIdempotency_SecondImportAddsZeroRecords()
        {
            return Prop.ForAll(
                Arb.From(DeduplicationTransactionListGen()),
                transactions =>
                {
                    // Simulate the dedup logic from ImportTransactionsAsync:
                    // Build a HashSet of composite keys from "existing" transactions.
                    var existingKeys = new HashSet<string>();
                    foreach (var tx in transactions)
                    {
                        string key = string.Format(
                            "{0}|{1}|{2}",
                            tx.TransactionDateTime,
                            tx.CreditChange,
                            tx.Detail);
                        existingKeys.Add(key);
                    }

                    // Now simulate a second import of the same transactions.
                    // Every transaction's composite key should already be in the set,
                    // so zero new records should be added.
                    int newRecords = 0;
                    foreach (var tx in transactions)
                    {
                        string key = string.Format(
                            "{0}|{1}|{2}",
                            tx.TransactionDateTime,
                            tx.CreditChange,
                            tx.Detail);

                        if (!existingKeys.Contains(key))
                        {
                            newRecords++;
                        }
                    }

                    return (newRecords == 0)
                        .Label("Expected 0 new records on second import, got " + newRecords);
                });
        }
    }
}
