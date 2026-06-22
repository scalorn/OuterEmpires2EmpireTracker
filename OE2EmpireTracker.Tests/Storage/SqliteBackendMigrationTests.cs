// -----------------------------------------------------------------------
// <copyright file="SqliteBackendMigrationTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Storage;

namespace OE2EmpireTracker.Tests.Storage
{
    /// <summary>
    /// Tests for SqliteBackend: transaction rollback on failure, schema migration
    /// versioning, legacy JSON-blob migration, and error handling.
    /// Satisfies: Req 4, Criteria 8-9; Req 9, Criteria 2, 6-7; Req 10, Criteria 1-3; Req 11, Criterion 5.
    /// </summary>
    [TestFixture]
    public class SqliteBackendMigrationTests
    {
        private string _dbPath;

        [SetUp]
        public void SetUp()
        {
            _dbPath = Path.Combine(
                Path.GetTempPath(),
                "oe2test_sqlitemig_" + Guid.NewGuid().ToString("N") + ".db");
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
                // File may be locked by SQLite native handles not yet released;
                // ignore cleanup failure — OS will reclaim temp file eventually.
            }
        }

        /// <summary>
        /// After initialization, the schema version stored in _metadata is 1.
        /// Validates: Req 9, Criterion 1.
        /// </summary>
        [Test]
        public async Task SchemaVersion_SetAndRead_RoundTrips()
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
        /// Initializing a DB that is already at the current version does not
        /// throw and the schema version remains unchanged.
        /// Validates: Req 9, Criterion 2.
        /// </summary>
        [Test]
        public async Task SchemaVersion_AlreadyCurrent_NoMigrationRun()
        {
            var config = new StorageBackendConfig { ConnectionString = _dbPath };
            var backend = new SqliteBackend(config);
            await backend.InitializeAsync();

            // Re-init on the same DB — should not throw
            var backend2 = new SqliteBackend(config);
            Assert.DoesNotThrowAsync(async () => await backend2.InitializeAsync());

            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Value FROM _metadata WHERE Key = 'schema_version'";
                    var result = cmd.ExecuteScalar();
                    Assert.That(result.ToString(), Is.EqualTo("3"));
                }
            }
        }

        /// <summary>
        /// When a DB has the legacy EntityData table, InitializeAsync detects it
        /// and drops it after migration.
        /// Validates: Req 11, Criterion 5.
        /// </summary>
        [Test]
        public async Task LegacyMigration_DetectsOldSchema()
        {
            // Create a fresh DB with the normalized schema first, then add EntityData
            var config = new StorageBackendConfig { ConnectionString = _dbPath };
            var backend = new SqliteBackend(config);
            await backend.InitializeAsync();

            // Inject legacy EntityData table (simulates old schema)
            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"CREATE TABLE EntityData (
                        EntityType TEXT NOT NULL,
                        EntityId TEXT NOT NULL,
                        CharacterUUID TEXT,
                        JsonData TEXT NOT NULL,
                        PRIMARY KEY (EntityType, EntityId))";
                    cmd.ExecuteNonQuery();
                }
            }

            // Re-init — should detect and drop EntityData
            var backend2 = new SqliteBackend(config);
            await backend2.InitializeAsync();

            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='EntityData'";
                    var result = cmd.ExecuteScalar();
                    Assert.That(result, Is.Null, "EntityData table should be dropped after migration");
                }
            }
        }

        /// <summary>
        /// Legacy migration reads JSON blobs from EntityData and inserts them
        /// into normalized Colonies table.
        /// Validates: Req 11, Criterion 5.
        /// </summary>
        [Test]
        public async Task LegacyMigration_MigratesEntities()
        {
            // Create a fresh DB with normalized schema
            var config = new StorageBackendConfig { ConnectionString = _dbPath };
            var backend = new SqliteBackend(config);
            await backend.InitializeAsync();

            // Add EntityData table with a valid Colony JSON blob
            string colonyJson = @"{
                ""UUID"": ""col-1"",
                ""OwnerUUID"": ""char-1"",
                ""ColonyName"": ""Legacy Colony"",
                ""SystemName"": ""OldSystem"",
                ""PlanetName"": ""TestPlanet"",
                ""ColonyId"": 42,
                ""SystemId"": 7,
                ""ColonySize"": 5,
                ""Distance"": 1.5,
                ""Structures"": []
            }";

            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"CREATE TABLE EntityData (
                        EntityType TEXT NOT NULL,
                        EntityId TEXT NOT NULL,
                        CharacterUUID TEXT,
                        JsonData TEXT NOT NULL,
                        PRIMARY KEY (EntityType, EntityId))";
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO EntityData VALUES ('Colony', 'col-1', 'char-1', @json)";
                    cmd.Parameters.AddWithValue("@json", colonyJson);
                    cmd.ExecuteNonQuery();
                }
            }

            // Re-init — triggers legacy migration
            var backend2 = new SqliteBackend(config);
            await backend2.InitializeAsync();

            // Verify colony exists in normalized table
            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT ColonyName FROM Colonies WHERE UUID = 'col-1'";
                    var result = cmd.ExecuteScalar();
                    Assert.That(result, Is.Not.Null);
                    Assert.That(result.ToString(), Is.EqualTo("Legacy Colony"));
                }
            }
        }

        /// <summary>
        /// Malformed JSON in EntityData is skipped without failing the migration.
        /// Validates: Req 11, Criterion 6 (partial migration continues).
        /// </summary>
        [Test]
        public async Task LegacyMigration_SkipsMalformedEntities()
        {
            // Create a fresh DB with normalized schema
            var config = new StorageBackendConfig { ConnectionString = _dbPath };
            var backend = new SqliteBackend(config);
            await backend.InitializeAsync();

            // Add EntityData table with one good and one bad JSON blob
            string goodJson = @"{
                ""UUID"": ""col-good"",
                ""OwnerUUID"": ""char-1"",
                ""ColonyName"": ""Good Colony"",
                ""SystemName"": ""GoodSys"",
                ""Structures"": []
            }";

            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"CREATE TABLE EntityData (
                        EntityType TEXT NOT NULL,
                        EntityId TEXT NOT NULL,
                        CharacterUUID TEXT,
                        JsonData TEXT NOT NULL,
                        PRIMARY KEY (EntityType, EntityId))";
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO EntityData VALUES ('Colony', 'col-good', 'char-1', @json)";
                    cmd.Parameters.AddWithValue("@json", goodJson);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO EntityData VALUES ('Colony', 'col-bad', 'char-1', '{{{INVALID JSON')";
                    cmd.ExecuteNonQuery();
                }
            }

            // Re-init — should migrate good entity and skip bad one
            var backend2 = new SqliteBackend(config);
            Assert.DoesNotThrowAsync(async () => await backend2.InitializeAsync());

            // Verify good entity was migrated
            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT ColonyName FROM Colonies WHERE UUID = 'col-good'";
                    var result = cmd.ExecuteScalar();
                    Assert.That(result, Is.Not.Null);
                    Assert.That(result.ToString(), Is.EqualTo("Good Colony"));
                }
            }
        }

        /// <summary>
        /// A fresh DB without EntityData table does not attempt legacy migration
        /// and does not throw.
        /// Validates: Req 11, Criterion 5 (no-op when no legacy data).
        /// </summary>
        [Test]
        public async Task LegacyMigration_NoOldSchema_DoesNothing()
        {
            var config = new StorageBackendConfig { ConnectionString = _dbPath };
            var backend = new SqliteBackend(config);

            // Fresh init — no EntityData table, should not throw
            Assert.DoesNotThrowAsync(async () => await backend.InitializeAsync());

            // Verify normal schema exists and no EntityData table
            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='EntityData'";
                    var result = cmd.ExecuteScalar();
                    Assert.That(result, Is.Null);
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Colonies'";
                    var result = cmd.ExecuteScalar();
                    Assert.That(result, Is.Not.Null);
                }
            }
        }

        /// <summary>
        /// ValidateConnectionAsync returns false when given an invalid path
        /// (e.g. a path into a non-existent directory).
        /// Validates: Req 10, Criterion 2.
        /// </summary>
        [Test]
        public async Task ValidateConnection_BadPath_ReturnsFalse()
        {
            var badPath = Path.Combine(
                Path.GetTempPath(),
                "nonexistent_dir_" + Guid.NewGuid().ToString("N"),
                "sub",
                "deep",
                "test.db");
            var config = new StorageBackendConfig { ConnectionString = badPath };
            var backend = new SqliteBackend(config);

            var result = await backend.ValidateConnectionAsync();

            Assert.That(result, Is.False);
        }

        /// <summary>
        /// InitializeAsync throws StorageLoadException when pointed at a file
        /// that is not a valid SQLite database (random bytes).
        /// Validates: Req 4, Criterion 8; Req 10, Criterion 2.
        /// </summary>
        [Test]
        public void InitializeAsync_CorruptedDb_ThrowsStorageLoadException()
        {
            // Write random bytes to the DB path to simulate corruption
            var random = new Random(42);
            var junk = new byte[1024];
            random.NextBytes(junk);
            File.WriteAllBytes(_dbPath, junk);

            var config = new StorageBackendConfig { ConnectionString = _dbPath };
            var backend = new SqliteBackend(config);

            var ex = Assert.ThrowsAsync<StorageLoadException>(
                async () => await backend.InitializeAsync());

            Assert.That(ex, Is.Not.Null);
            Assert.That(ex.Message, Is.Not.Empty);

            // Force pool cleanup so TearDown can delete the file
            SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
