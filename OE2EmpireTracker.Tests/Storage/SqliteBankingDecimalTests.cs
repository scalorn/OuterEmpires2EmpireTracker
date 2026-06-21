// -----------------------------------------------------------------------
// <copyright file="SqliteBankingDecimalTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using OE2EmpireTracker.Common.Storage;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Storage
{
    /// <summary>
    /// Tests that BankingTransaction decimal columns (CreditChange, OldBalance,
    /// NewBalance) are stored as TEXT in SQLite and round-trip without precision loss.
    /// Satisfies: Req 7, Criterion 2.
    /// </summary>
    [TestFixture]
    public class SqliteBankingDecimalTests
    {
        private string _dbPath;
        private SqliteBackend _backend;

        [SetUp]
        public async Task SetUp()
        {
            _dbPath = Path.Combine(
                Path.GetTempPath(),
                "oe2test_bankdecimal_" + Guid.NewGuid().ToString("N") + ".db");
            var config = new StorageBackendConfig { ConnectionString = _dbPath };
            _backend = new SqliteBackend(config);
            await _backend.InitializeAsync();
        }

        [TearDown]
        public void TearDown()
        {
            SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            TryDeleteFile(_dbPath);
            TryDeleteFile(_dbPath + "-wal");
            TryDeleteFile(_dbPath + "-shm");
        }

        /// <summary>
        /// Decimal values that lose precision in IEEE 754 double round-trip correctly
        /// through SQLite TEXT storage.
        /// </summary>
        [Test]
        public async Task BankingTransaction_DecimalRoundTrip_PreservesPrecision()
        {
            var tx = new BankingTransaction
            {
                UUID = "tx-precision-1",
                OwnerUUID = "char-1",
                TransactionDateTime = "2024-06-15T10:30:00.0000000Z",
                CreditChange = 79228162514264337593543950335m,
                OldBalance = 0.1234567890123456789m,
                NewBalance = 123456789.123456789m,
                TransactionType = 1,
                Detail = "Precision test",
                IsManualEntry = true,
            };

            await _backend.UpsertBankingTransactionAsync("char-1", tx);
            var loaded = await _backend.GetBankingTransactionAsync("char-1", "tx-precision-1");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.CreditChange, Is.EqualTo(tx.CreditChange));
            Assert.That(loaded.OldBalance, Is.EqualTo(tx.OldBalance));
            Assert.That(loaded.NewBalance, Is.EqualTo(tx.NewBalance));
        }

        /// <summary>
        /// Typical banking values (e.g. 1234.56) round-trip correctly.
        /// </summary>
        [Test]
        public async Task BankingTransaction_TypicalValues_RoundTrip()
        {
            var tx = new BankingTransaction
            {
                UUID = "tx-typical-1",
                OwnerUUID = "char-2",
                TransactionDateTime = "2024-01-01T00:00:00.0000000Z",
                CreditChange = -5000.75m,
                OldBalance = 1000000.50m,
                NewBalance = 995000.25m,
                TransactionType = 2,
                Detail = "Purchase",
                CharacterId = 42,
                SystemObjectId = 100,
                SystemId = 7,
                IsManualEntry = false,
            };

            await _backend.UpsertBankingTransactionAsync("char-2", tx);
            var loaded = await _backend.GetBankingTransactionAsync("char-2", "tx-typical-1");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.CreditChange, Is.EqualTo(-5000.75m));
            Assert.That(loaded.OldBalance, Is.EqualTo(1000000.50m));
            Assert.That(loaded.NewBalance, Is.EqualTo(995000.25m));
            Assert.That(loaded.CharacterId, Is.EqualTo(42));
            Assert.That(loaded.SystemObjectId, Is.EqualTo(100));
            Assert.That(loaded.SystemId, Is.EqualTo(7));
        }

        /// <summary>
        /// Schema migration from v1 (REAL columns) to v2 (TEXT columns) preserves
        /// existing data and enables precision-safe storage.
        /// </summary>
        [Test]
        public async Task SchemaMigration_V1ToV2_PreservesExistingData()
        {
            // Create a v1 database manually with REAL columns
            var v1DbPath = Path.Combine(
                Path.GetTempPath(),
                "oe2test_bankv1_" + Guid.NewGuid().ToString("N") + ".db");

            try
            {
                using (var conn = new SqliteConnection($"Data Source={v1DbPath}"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
CREATE TABLE _metadata (Key TEXT PRIMARY KEY, Value TEXT NOT NULL);
INSERT INTO _metadata (Key, Value) VALUES ('schema_version', '1');

CREATE TABLE BankingTransactions (
    UUID TEXT PRIMARY KEY,
    OwnerUUID TEXT NOT NULL DEFAULT '',
    TransactionDateTime TEXT NOT NULL DEFAULT '',
    CreditChange REAL NOT NULL DEFAULT 0,
    OldBalance REAL NOT NULL DEFAULT 0,
    NewBalance REAL NOT NULL DEFAULT 0,
    TransactionType INTEGER NOT NULL DEFAULT 0,
    Detail TEXT NOT NULL DEFAULT '',
    CharacterId INTEGER,
    SystemObjectId INTEGER,
    SystemId INTEGER,
    IsManualEntry INTEGER NOT NULL DEFAULT 0
);

INSERT INTO BankingTransactions (UUID, OwnerUUID, TransactionDateTime, CreditChange, OldBalance, NewBalance, TransactionType, Detail, IsManualEntry)
VALUES ('tx-v1-1', 'char-1', '2024-01-01', 1234.5, 10000.0, 11234.5, 1, 'Mining', 0);
";
                        cmd.ExecuteNonQuery();
                    }

                    // Also create all other required tables so InitializeAsync won't fail
                    // The backend expects all tables already exist at v1
                }

                // Now init the backend on this v1 database — should trigger migration
                var config = new StorageBackendConfig { ConnectionString = v1DbPath };
                var backend = new SqliteBackend(config);
                await backend.InitializeAsync();

                // Verify the data survived migration
                var loaded = await backend.GetBankingTransactionAsync("char-1", "tx-v1-1");
                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded.CreditChange, Is.EqualTo(1234.5m));
                Assert.That(loaded.OldBalance, Is.EqualTo(10000.0m));
                Assert.That(loaded.NewBalance, Is.EqualTo(11234.5m));

                // Verify schema version updated to 2
                using (var conn = new SqliteConnection($"Data Source={v1DbPath}"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT Value FROM _metadata WHERE Key = 'schema_version'";
                        var version = cmd.ExecuteScalar()?.ToString();
                        Assert.That(version, Is.EqualTo("2"));
                    }
                }
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                TryDeleteFile(v1DbPath);
                TryDeleteFile(v1DbPath + "-wal");
                TryDeleteFile(v1DbPath + "-shm");
            }
        }

        /// <summary>
        /// Zero decimal values round-trip correctly (edge case for TEXT storage).
        /// </summary>
        [Test]
        public async Task BankingTransaction_ZeroValues_RoundTrip()
        {
            var tx = new BankingTransaction
            {
                UUID = "tx-zero-1",
                OwnerUUID = "char-3",
                TransactionDateTime = "2024-01-01T00:00:00.0000000Z",
                CreditChange = 0m,
                OldBalance = 0m,
                NewBalance = 0m,
                TransactionType = 0,
                Detail = "Zero test",
                IsManualEntry = false,
            };

            await _backend.UpsertBankingTransactionAsync("char-3", tx);
            var loaded = await _backend.GetBankingTransactionAsync("char-3", "tx-zero-1");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.CreditChange, Is.EqualTo(0m));
            Assert.That(loaded.OldBalance, Is.EqualTo(0m));
            Assert.That(loaded.NewBalance, Is.EqualTo(0m));
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
                // Ignore cleanup failures in temp files.
            }
        }
    }
}
