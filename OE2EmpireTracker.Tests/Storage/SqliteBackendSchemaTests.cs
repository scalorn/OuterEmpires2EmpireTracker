// -----------------------------------------------------------------------
// <copyright file="SqliteBackendSchemaTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using OE2EmpireTracker.Common.Storage;

namespace OE2EmpireTracker.Tests.Storage
{
    /// <summary>
    /// Tests for SqliteBackend schema creation, WAL mode, and _metadata table.
    /// Satisfies: Req 4, Criteria 5-6; Req 9, Criteria 1-3.
    /// </summary>
    [TestFixture]
    public class SqliteBackendSchemaTests
    {
        private string _dbPath;

        [SetUp]
        public void SetUp()
        {
            _dbPath = Path.Combine(
                Path.GetTempPath(),
                "oe2test_sqlite_" + Guid.NewGuid().ToString("N") + ".db");
        }

        [TearDown]
        public void TearDown()
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }

            var walPath = _dbPath + "-wal";
            if (File.Exists(walPath))
            {
                File.Delete(walPath);
            }

            var shmPath = _dbPath + "-shm";
            if (File.Exists(shmPath))
            {
                File.Delete(shmPath);
            }
        }

        /// <summary>
        /// After init on a fresh DB, all expected tables exist in sqlite_master.
        /// </summary>
        [Test]
        public async Task InitializeAsync_FreshDb_CreatesAllTables()
        {
            var config = new StorageBackendConfig { ConnectionString = _dbPath };
            var backend = new SqliteBackend(config);
            await backend.InitializeAsync();

            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
                    var tables = new List<string>();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tables.Add(reader.GetString(0));
                        }
                    }

                    Assert.That(tables, Does.Contain("Colonies"));
                    Assert.That(tables, Does.Contain("ColonyStructures"));
                    Assert.That(tables, Does.Contain("ColonyStructureProperties"));
                    Assert.That(tables, Does.Contain("ColonyStructureWorkers"));
                    Assert.That(tables, Does.Contain("Items"));
                    Assert.That(tables, Does.Contain("Blueprints"));
                    Assert.That(tables, Does.Contain("BlueprintProperties"));
                    Assert.That(tables, Does.Contain("BlueprintResources"));
                    Assert.That(tables, Does.Contain("Surveys"));
                    Assert.That(tables, Does.Contain("PlayerProfiles"));
                    Assert.That(tables, Does.Contain("PlayerSkills"));
                    Assert.That(tables, Does.Contain("DeliveryRoutes"));
                    Assert.That(tables, Does.Contain("Ships"));
                    Assert.That(tables, Does.Contain("ShipTemplates"));
                    Assert.That(tables, Does.Contain("MarketListings"));
                    Assert.That(tables, Does.Contain("MarketTransactions"));
                    Assert.That(tables, Does.Contain("_metadata"));
                }
            }
        }

        /// <summary>
        /// After init, PRAGMA journal_mode returns 'wal'.
        /// </summary>
        [Test]
        public async Task InitializeAsync_FreshDb_SetsWalMode()
        {
            var config = new StorageBackendConfig { ConnectionString = _dbPath };
            var backend = new SqliteBackend(config);
            await backend.InitializeAsync();

            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA journal_mode;";
                    var mode = (string)cmd.ExecuteScalar();
                    Assert.That(mode, Is.EqualTo("wal").IgnoreCase);
                }
            }
        }

        /// <summary>
        /// After init, the _metadata table exists.
        /// </summary>
        [Test]
        public async Task InitializeAsync_FreshDb_CreatesMetadataTable()
        {
            var config = new StorageBackendConfig { ConnectionString = _dbPath };
            var backend = new SqliteBackend(config);
            await backend.InitializeAsync();

            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='_metadata'";
                    var result = cmd.ExecuteScalar();
                    Assert.That(result, Is.Not.Null);
                    Assert.That((string)result, Is.EqualTo("_metadata"));
                }
            }
        }

        /// <summary>
        /// After init, schema_version in _metadata equals CurrentSchemaVersion (3).
        /// </summary>
        [Test]
        public async Task InitializeAsync_FreshDb_SetsSchemaVersion()
        {
            var config = new StorageBackendConfig { ConnectionString = _dbPath };
            var backend = new SqliteBackend(config);
            await backend.InitializeAsync();

            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Value FROM _metadata WHERE Key = 'schema_version'";
                    var result = cmd.ExecuteScalar();
                    Assert.That(result, Is.Not.Null);
                    Assert.That(result.ToString(), Is.EqualTo("3"));
                }
            }
        }

        /// <summary>
        /// Initializing twice does not throw and tables still exist.
        /// </summary>
        [Test]
        public async Task InitializeAsync_ExistingDb_DoesNotRecreate()
        {
            var config = new StorageBackendConfig { ConnectionString = _dbPath };
            var backend = new SqliteBackend(config);
            await backend.InitializeAsync();

            // Second initialization should not throw
            var backend2 = new SqliteBackend(config);
            Assert.DoesNotThrowAsync(async () => await backend2.InitializeAsync());

            // Verify tables still exist
            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Colonies'";
                    var count = (long)cmd.ExecuteScalar();
                    Assert.That(count, Is.EqualTo(1));
                }
            }
        }

        /// <summary>
        /// GetStorageInfo returns BackendType "Sqlite".
        /// </summary>
        [Test]
        public void GetStorageInfo_ReturnsCorrectType()
        {
            var config = new StorageBackendConfig { ConnectionString = _dbPath };
            var backend = new SqliteBackend(config);

            var info = backend.GetStorageInfo();

            Assert.That(info.BackendType, Is.EqualTo("Sqlite"));
            Assert.That(info.Location, Is.EqualTo(_dbPath));
        }

        /// <summary>
        /// After init, ValidateConnectionAsync returns true.
        /// </summary>
        [Test]
        public async Task ValidateConnection_ReturnsTrue()
        {
            var config = new StorageBackendConfig { ConnectionString = _dbPath };
            var backend = new SqliteBackend(config);
            await backend.InitializeAsync();

            var result = await backend.ValidateConnectionAsync();

            Assert.That(result, Is.True);
        }
    }
}
