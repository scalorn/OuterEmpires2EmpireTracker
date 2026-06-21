// -----------------------------------------------------------------------
// <copyright file="StorageBackendFactoryTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
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
        /// Req 8 Criterion 2: JsonSingleFile is a known type but currently
        /// throws NotImplementedException from the factory.
        /// </summary>
        [Test]
        public void JsonSingleFile_ThrowsNotImplementedException()
        {
            // Arrange
            var config = new StorageBackendConfig
            {
                ConnectionString = Path.Combine(_tempDir, "PlayerData.json")
            };

            // Act & Assert
            Assert.ThrowsAsync<NotImplementedException>(
                () => StorageBackendFactory.CreateAsync(
                    StorageBackendType.JsonSingleFile, config));
        }

        /// <summary>
        /// Req 8 Criterion 2: Sqlite is a known type but currently throws
        /// NotImplementedException from the factory.
        /// </summary>
        [Test]
        public void Sqlite_ThrowsNotImplementedException()
        {
            // Arrange
            var config = new StorageBackendConfig
            {
                ConnectionString = Path.Combine(_tempDir, "test.db")
            };

            // Act & Assert
            Assert.ThrowsAsync<NotImplementedException>(
                () => StorageBackendFactory.CreateAsync(
                    StorageBackendType.Sqlite, config));
        }

        /// <summary>
        /// Req 8 Criterion 2: DynamoDb is a known type but currently throws
        /// NotImplementedException from the factory.
        /// </summary>
        [Test]
        public void DynamoDb_ThrowsNotImplementedException()
        {
            // Arrange
            var config = new StorageBackendConfig
            {
                ConnectionString = "http://localhost:8111"
            };

            // Act & Assert
            Assert.ThrowsAsync<NotImplementedException>(
                () => StorageBackendFactory.CreateAsync(
                    StorageBackendType.DynamoDb, config));
        }

        /// <summary>
        /// Req 8 Criterion 2: Postgres is a known type but currently throws
        /// NotImplementedException from the factory.
        /// </summary>
        [Test]
        public void Postgres_ThrowsNotImplementedException()
        {
            // Arrange
            var config = new StorageBackendConfig
            {
                ConnectionString = "Host=localhost;Database=test"
            };

            // Act & Assert
            Assert.ThrowsAsync<NotImplementedException>(
                () => StorageBackendFactory.CreateAsync(
                    StorageBackendType.Postgres, config));
        }
    }
}
