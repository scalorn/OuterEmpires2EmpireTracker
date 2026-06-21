// -----------------------------------------------------------------------
// <copyright file="StorageBackendFactoryTests.cs" company="OE2EmpireTracker">
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
    /// Tests for <see cref="StorageBackendFactory"/>: valid types create
    /// correct backend, invalid type throws, InitializeAsync is called.
    /// Satisfies: Req 8, Criteria 1-4.
    /// </summary>
    [TestFixture]
    public class StorageBackendFactoryTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(
                Path.GetTempPath(),
                "oe2test_factory_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            // Clear SQLite connection pool so file handles are released.
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        /// <summary>
        /// Req 8 Criterion 1: JsonMultiFile type creates a JsonMultiFileBackend.
        /// </summary>
        [Test]
        public async Task JsonMultiFile_CreatesCorrectBackend()
        {
            // Arrange
            var config = new StorageBackendConfig
            {
                ConnectionString = _tempDir
            };

            // Act
            var backend = await StorageBackendFactory.CreateAsync(
                StorageBackendType.JsonMultiFile, config);

            // Assert
            Assert.That(backend, Is.Not.Null);
            Assert.That(backend, Is.InstanceOf<JsonMultiFileBackend>());
            Assert.That(
                backend.GetStorageInfo().BackendType,
                Is.EqualTo("JsonMultiFile"));
        }

        /// <summary>
        /// Req 8 Criterion 3: Invalid enum value throws ArgumentException
        /// without calling InitializeAsync.
        /// </summary>
        [Test]
        public void InvalidType_ThrowsArgumentException()
        {
            // Arrange
            var config = new StorageBackendConfig
            {
                ConnectionString = _tempDir
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(
                () => StorageBackendFactory.CreateAsync(
                    (StorageBackendType)999, config));

            Assert.That(
                ex.Message,
                Does.Contain("999"));
        }

        /// <summary>
        /// Req 8 Criterion 4: After CreateAsync succeeds, InitializeAsync has
        /// been called and the backend is ready for use.
        /// </summary>
        [Test]
        public async Task InitializeAsync_Called()
        {
            // Arrange
            var config = new StorageBackendConfig
            {
                ConnectionString = _tempDir
            };

            // Act
            var backend = await StorageBackendFactory.CreateAsync(
                StorageBackendType.JsonMultiFile, config);

            // Assert — ValidateConnection works (proves initialized)
            var valid = await backend.ValidateConnectionAsync();
            Assert.That(valid, Is.True);

            // Storage info is also populated (proves initialized)
            var info = backend.GetStorageInfo();
            Assert.That(info.BackendType, Is.EqualTo("JsonMultiFile"));
            Assert.That(info.Location, Is.EqualTo(_tempDir));
        }

        /// <summary>
        /// Req 8 Criterion 2: JsonSingleFile type creates a JsonSingleFileBackend.
        /// </summary>
        [Test]
        public async Task JsonSingleFile_CreatesCorrectBackend()
        {
            // Arrange — use the temp dir (constructor resolves files inside)
            var config = new StorageBackendConfig
            {
                ConnectionString = _tempDir
            };

            // Act
            var backend = await StorageBackendFactory.CreateAsync(
                StorageBackendType.JsonSingleFile, config);

            // Assert
            Assert.That(backend, Is.Not.Null);
            Assert.That(backend, Is.InstanceOf<JsonSingleFileBackend>());
            Assert.That(
                backend.GetStorageInfo().BackendType,
                Is.EqualTo("JsonSingleFile"));
        }

        /// <summary>
        /// Req 8 Criterion 2: Sqlite type creates a SqliteBackend.
        /// </summary>
        [Test]
        public async Task Sqlite_CreatesCorrectBackend()
        {
            // Arrange
            var config = new StorageBackendConfig
            {
                ConnectionString = Path.Combine(_tempDir, "test.db")
            };

            // Act
            var backend = await StorageBackendFactory.CreateAsync(
                StorageBackendType.Sqlite, config);

            // Assert
            Assert.That(backend, Is.Not.Null);
            Assert.That(backend, Is.InstanceOf<SqliteBackend>());
            Assert.That(
                backend.GetStorageInfo().BackendType,
                Is.EqualTo("Sqlite"));
        }

        /// <summary>
        /// Req 8 Criterion 2: DynamoDb type creates a DynamoDbBackend but
        /// initialization fails when connecting to an invalid endpoint.
        /// </summary>
        [Test]
        public void DynamoDb_ThrowsOnInvalidEndpoint()
        {
            // Arrange — point at a non-existent local endpoint
            var config = new StorageBackendConfig
            {
                ConnectionString = "http://localhost:59999",
                AwsRegion = "us-east-1",
                TablePrefix = "TestTable"
            };

            // Act & Assert — CreateAsync calls InitializeAsync which
            // attempts DescribeTable against the unreachable endpoint.
            Assert.CatchAsync<Exception>(
                () => StorageBackendFactory.CreateAsync(
                    StorageBackendType.DynamoDb, config));
        }

        /// <summary>
        /// Req 8 Criterion 2: Postgres type creates a PostgresBackend but
        /// initialization fails when connecting to an invalid host.
        /// </summary>
        [Test]
        public void Postgres_ThrowsOnInvalidEndpoint()
        {
            // Arrange — point at a non-existent Postgres server
            var config = new StorageBackendConfig
            {
                ConnectionString = "Host=localhost;Port=59999;Database=test;Timeout=2"
            };

            // Act & Assert — CreateAsync calls InitializeAsync which
            // attempts to open the connection and run schema setup.
            Assert.CatchAsync<Exception>(
                () => StorageBackendFactory.CreateAsync(
                    StorageBackendType.Postgres, config));
        }
    }
}
