using System;
using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class BankingServiceFilterTests
    {
        private List<BankingTransaction> _testTransactions;

        [SetUp]
        public void SetUp()
        {
            _testTransactions = new List<BankingTransaction>
            {
                new BankingTransaction
                {
                    UUID = "tx-1",
                    TransactionDateTime = "2026-05-01T10:00:00Z",
                    CreditChange = 500m,
                    TransactionType = 2,
                    Detail = "Market Sale",
                },
                new BankingTransaction
                {
                    UUID = "tx-2",
                    TransactionDateTime = "2026-05-10T14:00:00Z",
                    CreditChange = -100m,
                    TransactionType = 1,
                    Detail = "Worker Wages",
                },
                new BankingTransaction
                {
                    UUID = "tx-3",
                    TransactionDateTime = "2026-05-20T08:30:00Z",
                    CreditChange = 1000m,
                    TransactionType = 2,
                    Detail = "Market Sale",
                },
                new BankingTransaction
                {
                    UUID = "tx-4",
                    TransactionDateTime = "2026-05-25T16:45:00Z",
                    CreditChange = -50m,
                    TransactionType = 4,
                    Detail = "Refueling",
                },
            };
        }

        // -----------------------------------------------------------------------
        // FilterTransactions -- Null input
        // -----------------------------------------------------------------------

        [Test]
        public void FilterTransactions_NullInput_ReturnsEmptyList()
        {
            var result = BankingService.FilterTransactions(null, null, null, null);

            Assert.That(result, Is.Empty);
        }

        // -----------------------------------------------------------------------
        // FilterTransactions -- Empty list
        // -----------------------------------------------------------------------

        [Test]
        public void FilterTransactions_EmptyList_ReturnsEmptyList()
        {
            var result = BankingService.FilterTransactions(
                new List<BankingTransaction>(), null, null, null);

            Assert.That(result, Is.Empty);
        }

        // -----------------------------------------------------------------------
        // FilterTransactions -- No filters returns all sorted descending
        // -----------------------------------------------------------------------

        [Test]
        public void FilterTransactions_NoFilters_ReturnsAllSortedDescending()
        {
            var result = BankingService.FilterTransactions(
                _testTransactions, null, null, null);

            Assert.That(result, Has.Count.EqualTo(4));
            Assert.That(result[0].UUID, Is.EqualTo("tx-4"));
            Assert.That(result[1].UUID, Is.EqualTo("tx-3"));
            Assert.That(result[2].UUID, Is.EqualTo("tx-2"));
            Assert.That(result[3].UUID, Is.EqualTo("tx-1"));
        }

        // -----------------------------------------------------------------------
        // FilterTransactions -- Filter by type only
        // -----------------------------------------------------------------------

        [Test]
        public void FilterTransactions_FilterByType_ReturnsMatchingOnly()
        {
            var result = BankingService.FilterTransactions(
                _testTransactions, 2, null, null);

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].UUID, Is.EqualTo("tx-3"));
            Assert.That(result[1].UUID, Is.EqualTo("tx-1"));
        }

        // -----------------------------------------------------------------------
        // FilterTransactions -- Filter by fromDate only
        // -----------------------------------------------------------------------

        [Test]
        public void FilterTransactions_FilterByFromDate_ReturnsOnOrAfter()
        {
            var fromDate = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);

            var result = BankingService.FilterTransactions(
                _testTransactions, null, fromDate, null);

            Assert.That(result, Has.Count.EqualTo(3));
            Assert.That(result[0].UUID, Is.EqualTo("tx-4"));
            Assert.That(result[1].UUID, Is.EqualTo("tx-3"));
            Assert.That(result[2].UUID, Is.EqualTo("tx-2"));
        }

        // -----------------------------------------------------------------------
        // FilterTransactions -- Filter by toDate only
        // -----------------------------------------------------------------------

        [Test]
        public void FilterTransactions_FilterByToDate_ReturnsOnOrBefore()
        {
            var toDate = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc);

            var result = BankingService.FilterTransactions(
                _testTransactions, null, null, toDate);

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].UUID, Is.EqualTo("tx-2"));
            Assert.That(result[1].UUID, Is.EqualTo("tx-1"));
        }

        // -----------------------------------------------------------------------
        // FilterTransactions -- Combined filters (type + date range)
        // -----------------------------------------------------------------------

        [Test]
        public void FilterTransactions_CombinedFilters_AppliesAndLogic()
        {
            var fromDate = new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc);
            var toDate = new DateTime(2026, 5, 22, 0, 0, 0, DateTimeKind.Utc);

            var result = BankingService.FilterTransactions(
                _testTransactions, 2, fromDate, toDate);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].UUID, Is.EqualTo("tx-3"));
        }

        // -----------------------------------------------------------------------
        // FilterTransactions -- Combined filters with empty result
        // -----------------------------------------------------------------------

        [Test]
        public void FilterTransactions_CombinedFiltersNoMatch_ReturnsEmpty()
        {
            var fromDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

            var result = BankingService.FilterTransactions(
                _testTransactions, 2, fromDate, null);

            Assert.That(result, Is.Empty);
        }
    }
}
