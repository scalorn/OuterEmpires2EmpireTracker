// <copyright file="BankingTransactionTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Models
{
    /// <summary>
    /// Unit tests for BankingTransaction model defaults and JSON serialization.
    /// Feature: banking-transactions
    /// </summary>
    [TestFixture]
    public class BankingTransactionTests
    {
        // ---------------------------------------------------------------
        // Default Values
        // ---------------------------------------------------------------

        [Test]
        public void DefaultConstructor_UUIDIsEmpty()
        {
            var txn = new BankingTransaction();
            Assert.That(txn.UUID, Is.EqualTo(string.Empty));
        }

        [Test]
        public void DefaultConstructor_OwnerUUIDIsEmpty()
        {
            var txn = new BankingTransaction();
            Assert.That(txn.OwnerUUID, Is.EqualTo(string.Empty));
        }

        [Test]
        public void DefaultConstructor_TransactionDateTimeIsEmpty()
        {
            var txn = new BankingTransaction();
            Assert.That(txn.TransactionDateTime, Is.EqualTo(string.Empty));
        }

        [Test]
        public void DefaultConstructor_CreditChangeIsZero()
        {
            var txn = new BankingTransaction();
            Assert.That(txn.CreditChange, Is.EqualTo(0m));
        }

        [Test]
        public void DefaultConstructor_OldBalanceIsZero()
        {
            var txn = new BankingTransaction();
            Assert.That(txn.OldBalance, Is.EqualTo(0m));
        }

        [Test]
        public void DefaultConstructor_NewBalanceIsZero()
        {
            var txn = new BankingTransaction();
            Assert.That(txn.NewBalance, Is.EqualTo(0m));
        }

        [Test]
        public void DefaultConstructor_TransactionTypeIsZero()
        {
            var txn = new BankingTransaction();
            Assert.That(txn.TransactionType, Is.EqualTo(0));
        }

        [Test]
        public void DefaultConstructor_DetailIsEmpty()
        {
            var txn = new BankingTransaction();
            Assert.That(txn.Detail, Is.EqualTo(string.Empty));
        }

        [Test]
        public void DefaultConstructor_CharacterIdIsNull()
        {
            var txn = new BankingTransaction();
            Assert.That(txn.CharacterId, Is.Null);
        }

        [Test]
        public void DefaultConstructor_SystemObjectIdIsNull()
        {
            var txn = new BankingTransaction();
            Assert.That(txn.SystemObjectId, Is.Null);
        }

        [Test]
        public void DefaultConstructor_SystemIdIsNull()
        {
            var txn = new BankingTransaction();
            Assert.That(txn.SystemId, Is.Null);
        }

        [Test]
        public void DefaultConstructor_IsManualEntryIsFalse()
        {
            var txn = new BankingTransaction();
            Assert.That(txn.IsManualEntry, Is.False);
        }

        // ---------------------------------------------------------------
        // JSON Serialization Round-Trip
        // ---------------------------------------------------------------

        [Test]
        public void JsonRoundTrip_AllFieldsPreserved()
        {
            var original = new BankingTransaction
            {
                UUID = "abc-123",
                OwnerUUID = "owner-456",
                TransactionDateTime = "2026-05-30T14:30:00Z",
                CreditChange = -500.75m,
                OldBalance = 12500.00m,
                NewBalance = 11999.25m,
                TransactionType = 2,
                Detail = "Market Sale - Iron Ore x100",
                CharacterId = 42,
                SystemObjectId = 7,
                SystemId = 3,
                IsManualEntry = true,
            };

            string json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<BankingTransaction>(json);

            Assert.That(deserialized.UUID, Is.EqualTo(original.UUID));
            Assert.That(deserialized.OwnerUUID, Is.EqualTo(original.OwnerUUID));
            Assert.That(deserialized.TransactionDateTime, Is.EqualTo(original.TransactionDateTime));
            Assert.That(deserialized.CreditChange, Is.EqualTo(original.CreditChange));
            Assert.That(deserialized.OldBalance, Is.EqualTo(original.OldBalance));
            Assert.That(deserialized.NewBalance, Is.EqualTo(original.NewBalance));
            Assert.That(deserialized.TransactionType, Is.EqualTo(original.TransactionType));
            Assert.That(deserialized.Detail, Is.EqualTo(original.Detail));
            Assert.That(deserialized.CharacterId, Is.EqualTo(original.CharacterId));
            Assert.That(deserialized.SystemObjectId, Is.EqualTo(original.SystemObjectId));
            Assert.That(deserialized.SystemId, Is.EqualTo(original.SystemId));
            Assert.That(deserialized.IsManualEntry, Is.EqualTo(original.IsManualEntry));
        }

        [Test]
        public void JsonRoundTrip_NullableFieldsOmittedWhenNull()
        {
            var original = new BankingTransaction
            {
                UUID = "def-789",
                CreditChange = 1000m,
            };

            string json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<BankingTransaction>(json);

            Assert.That(deserialized.CharacterId, Is.Null);
            Assert.That(deserialized.SystemObjectId, Is.Null);
            Assert.That(deserialized.SystemId, Is.Null);
        }

        [Test]
        public void JsonRoundTrip_DecimalPrecisionPreserved()
        {
            var original = new BankingTransaction
            {
                UUID = "precision-test",
                CreditChange = 123456789.99m,
                OldBalance = 999999999.01m,
                NewBalance = 1123456789.00m,
            };

            string json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<BankingTransaction>(json);

            Assert.That(deserialized.CreditChange, Is.EqualTo(123456789.99m));
            Assert.That(deserialized.OldBalance, Is.EqualTo(999999999.01m));
            Assert.That(deserialized.NewBalance, Is.EqualTo(1123456789.00m));
        }

        [Test]
        public void JsonDeserialization_FromApiFormat_MapsCorrectly()
        {
            string apiJson = @"{
                ""uuid"": ""api-uuid-001"",
                ""ownerUUID"": ""owner-001"",
                ""transactionDateTime"": ""2026-01-15T08:00:00Z"",
                ""creditChange"": -250.50,
                ""oldBalance"": 5000.00,
                ""newBalance"": 4749.50,
                ""transactionType"": 4,
                ""detail"": ""Refueling at Station Alpha"",
                ""characterId"": 10,
                ""systemObjectId"": null,
                ""systemId"": 5,
                ""isManualEntry"": false
            }";

            var txn = JsonConvert.DeserializeObject<BankingTransaction>(apiJson);

            Assert.That(txn.UUID, Is.EqualTo("api-uuid-001"));
            Assert.That(txn.OwnerUUID, Is.EqualTo("owner-001"));
            Assert.That(txn.TransactionDateTime, Is.EqualTo("2026-01-15T08:00:00Z"));
            Assert.That(txn.CreditChange, Is.EqualTo(-250.50m));
            Assert.That(txn.OldBalance, Is.EqualTo(5000.00m));
            Assert.That(txn.NewBalance, Is.EqualTo(4749.50m));
            Assert.That(txn.TransactionType, Is.EqualTo(4));
            Assert.That(txn.Detail, Is.EqualTo("Refueling at Station Alpha"));
            Assert.That(txn.CharacterId, Is.EqualTo(10));
            Assert.That(txn.SystemObjectId, Is.Null);
            Assert.That(txn.SystemId, Is.EqualTo(5));
            Assert.That(txn.IsManualEntry, Is.False);
        }
    }
}
