using System.Collections.Generic;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class BankingServiceSummaryTests
    {
        // -----------------------------------------------------------------------
        // ComputeSummary -- Null input
        // -----------------------------------------------------------------------

        [Test]
        public void ComputeSummary_NullInput_ReturnsAllZeros()
        {
            BankingSummary result = BankingService.ComputeSummary(null);

            Assert.That(result.TotalIncome, Is.EqualTo(0m));
            Assert.That(result.TotalExpenses, Is.EqualTo(0m));
            Assert.That(result.NetChange, Is.EqualTo(0m));
        }

        // -----------------------------------------------------------------------
        // ComputeSummary -- Empty list
        // -----------------------------------------------------------------------

        [Test]
        public void ComputeSummary_EmptyList_ReturnsAllZeros()
        {
            var transactions = new List<BankingTransaction>();

            BankingSummary result = BankingService.ComputeSummary(transactions);

            Assert.That(result.TotalIncome, Is.EqualTo(0m));
            Assert.That(result.TotalExpenses, Is.EqualTo(0m));
            Assert.That(result.NetChange, Is.EqualTo(0m));
        }

        // -----------------------------------------------------------------------
        // ComputeSummary -- Single positive transaction
        // -----------------------------------------------------------------------

        [Test]
        public void ComputeSummary_SinglePositiveTransaction_ReturnsIncomeOnly()
        {
            var transactions = new List<BankingTransaction>
            {
                new BankingTransaction { CreditChange = 500.50m },
            };

            BankingSummary result = BankingService.ComputeSummary(transactions);

            Assert.That(result.TotalIncome, Is.EqualTo(500.50m));
            Assert.That(result.TotalExpenses, Is.EqualTo(0m));
            Assert.That(result.NetChange, Is.EqualTo(500.50m));
        }

        // -----------------------------------------------------------------------
        // ComputeSummary -- Single negative transaction
        // -----------------------------------------------------------------------

        [Test]
        public void ComputeSummary_SingleNegativeTransaction_ReturnsExpensesOnly()
        {
            var transactions = new List<BankingTransaction>
            {
                new BankingTransaction { CreditChange = -200.75m },
            };

            BankingSummary result = BankingService.ComputeSummary(transactions);

            Assert.That(result.TotalIncome, Is.EqualTo(0m));
            Assert.That(result.TotalExpenses, Is.EqualTo(200.75m));
            Assert.That(result.NetChange, Is.EqualTo(-200.75m));
        }

        // -----------------------------------------------------------------------
        // ComputeSummary -- Mixed positive and negative transactions
        // -----------------------------------------------------------------------

        [Test]
        public void ComputeSummary_MixedTransactions_ComputesCorrectTotals()
        {
            var transactions = new List<BankingTransaction>
            {
                new BankingTransaction { CreditChange = 1000m },
                new BankingTransaction { CreditChange = -300m },
                new BankingTransaction { CreditChange = 250m },
                new BankingTransaction { CreditChange = -150m },
            };

            BankingSummary result = BankingService.ComputeSummary(transactions);

            Assert.That(result.TotalIncome, Is.EqualTo(1250m));
            Assert.That(result.TotalExpenses, Is.EqualTo(450m));
            Assert.That(result.NetChange, Is.EqualTo(800m));
        }

        // -----------------------------------------------------------------------
        // ComputeSummary -- All zero credit changes
        // -----------------------------------------------------------------------

        [Test]
        public void ComputeSummary_AllZeroCreditChanges_ReturnsAllZeros()
        {
            var transactions = new List<BankingTransaction>
            {
                new BankingTransaction { CreditChange = 0m },
                new BankingTransaction { CreditChange = 0m },
            };

            BankingSummary result = BankingService.ComputeSummary(transactions);

            Assert.That(result.TotalIncome, Is.EqualTo(0m));
            Assert.That(result.TotalExpenses, Is.EqualTo(0m));
            Assert.That(result.NetChange, Is.EqualTo(0m));
        }
    }
}
