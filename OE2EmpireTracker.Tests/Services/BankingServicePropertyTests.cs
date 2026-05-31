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
    }
}
