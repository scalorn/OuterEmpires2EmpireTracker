using System;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class PlayerContextBankingTests
    {
        private PlayerContext _ctx;

        [SetUp]
        public void SetUp()
        {
            PlayerContext.FilePath = string.Empty;
            _ctx = new PlayerContext(new PlayerRoot());
        }

        [Test]
        public void InitBankingTransactions_DeduplicatesByUUID()
        {
            var root = new PlayerRoot
            {
                BankingTransaction = new[]
                {
                    new BankingTransaction { UUID = "aaa", Detail = "First" },
                    new BankingTransaction { UUID = "bbb", Detail = "Second" },
                    new BankingTransaction { UUID = "aaa", Detail = "Duplicate" },
                },
            };

            _ctx.InitBankingTransactions(root);

            Assert.That(_ctx.BankingTransactionList.Count, Is.EqualTo(2));
            Assert.That(_ctx.BankingTransactionList[0].Detail, Is.EqualTo("First"));
            Assert.That(_ctx.BankingTransactionList[1].Detail, Is.EqualTo("Second"));
        }

        [Test]
        public void InitBankingTransactions_SkipsNullOrEmptyUUID()
        {
            var root = new PlayerRoot
            {
                BankingTransaction = new[]
                {
                    new BankingTransaction { UUID = string.Empty, Detail = "Empty" },
                    new BankingTransaction { UUID = null, Detail = "Null" },
                    new BankingTransaction { UUID = "valid-1", Detail = "Valid" },
                },
            };

            _ctx.InitBankingTransactions(root);

            Assert.That(_ctx.BankingTransactionList.Count, Is.EqualTo(1));
            Assert.That(_ctx.BankingTransactionList[0].UUID, Is.EqualTo("valid-1"));
        }

        [Test]
        public void AddBankingTransaction_UniqueUUID_Succeeds()
        {
            var txn = new BankingTransaction { UUID = "txn-1", Detail = "Test" };

            _ctx.AddBankingTransaction(txn);

            Assert.That(_ctx.BankingTransactionList.Count, Is.EqualTo(1));
            Assert.That(_ctx.BankingTransactionList[0].UUID, Is.EqualTo("txn-1"));
        }

        [Test]
        public void AddBankingTransaction_DuplicateUUID_ThrowsInvalidOperationException()
        {
            var txn1 = new BankingTransaction { UUID = "dup-uuid", Detail = "First" };
            var txn2 = new BankingTransaction { UUID = "dup-uuid", Detail = "Second" };

            _ctx.AddBankingTransaction(txn1);

            Assert.Throws<InvalidOperationException>(() => _ctx.AddBankingTransaction(txn2));
        }

        [Test]
        public void BankingDataChanged_FiresOnAdd()
        {
            bool fired = false;
            _ctx.BankingDataChanged += (s, e) => fired = true;

            _ctx.OnBankingDataChanged();

            Assert.That(fired, Is.True);
        }

        [Test]
        public void BalancePersistence_RoundTrip_LoadsFromPlayerRoot()
        {
            var root = new PlayerRoot
            {
                BankingBalance = 12345.67m,
                BankingTransaction = new BankingTransaction[0],
            };

            _ctx.InitBankingTransactions(root);

            Assert.That(_ctx.BankingBalance, Is.EqualTo(12345.67m));
        }

        [Test]
        public void BalancePersistence_RoundTrip_SetAndReadBack()
        {
            var root = new PlayerRoot
            {
                BankingBalance = 500.00m,
                BankingTransaction = new BankingTransaction[0],
            };

            _ctx.InitBankingTransactions(root);
            _ctx.BankingBalance = 99999.99m;

            Assert.That(_ctx.BankingBalance, Is.EqualTo(99999.99m));
        }

        [Test]
        public void BalancePersistence_DefaultsToZero_WhenPlayerRootHasNoBalance()
        {
            var root = new PlayerRoot
            {
                BankingTransaction = new BankingTransaction[0],
            };

            _ctx.InitBankingTransactions(root);

            Assert.That(_ctx.BankingBalance, Is.EqualTo(0m));
        }
    }
}
