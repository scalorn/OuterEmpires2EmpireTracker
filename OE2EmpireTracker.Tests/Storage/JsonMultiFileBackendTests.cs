// -----------------------------------------------------------------------
// <copyright file="JsonMultiFileBackendTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Storage;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Storage
{
    /// <summary>
    /// Tests for JsonMultiFileBackend: initialization, CRUD, missing file,
    /// malformed JSON, directory structure, connection validation, storage info.
    /// Satisfies: Req 3, Criteria 1-2, 4, 6-8; Req 11, Criterion 4.
    /// </summary>
    [TestFixture]
    public class JsonMultiFileBackendTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(
                Path.GetTempPath(),
                "oe2test_multi_" + Guid.NewGuid().ToString("N"));
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
        /// Req 3 Criterion 8: InitializeAsync creates expected directory structure.
        /// </summary>
        [Test]
        public async Task InitializeAsync_CreatesDirectoryStructure()
        {
            // Arrange
            var backend = new JsonMultiFileBackend(_tempDir);

            // Act
            await backend.InitializeAsync();

            // Assert — root, global, characters subdirectories exist
            Assert.That(Directory.Exists(_tempDir), Is.True);
            Assert.That(Directory.Exists(Path.Combine(_tempDir, "global")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(_tempDir, "characters")), Is.True);

            // Seed files created
            Assert.That(File.Exists(Path.Combine(_tempDir, "factions.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(_tempDir, "characters.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(_tempDir, "tokens.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(_tempDir, "membership-actions.json")), Is.True);
        }

        /// <summary>
        /// Req 3 Criteria 1-2: Basic CRUD — upsert then get returns data.
        /// </summary>
        [Test]
        public async Task ColonyUpsert_And_Get_ReturnsData()
        {
            // Arrange
            var backend = new JsonMultiFileBackend(_tempDir);
            await backend.InitializeAsync();

            var colony = new Colony
            {
                UUID = "col-001",
                OwnerUUID = "char-1",
                ColonyName = "TestColony",
                SystemName = "Alpha",
            };

            // Act
            await backend.UpsertColonyAsync("char-1", colony);
            var result = await backend.GetColonyAsync("char-1", "col-001");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.UUID, Is.EqualTo("col-001"));
            Assert.That(result.ColonyName, Is.EqualTo("TestColony"));
            Assert.That(result.SystemName, Is.EqualTo("Alpha"));
        }

        /// <summary>
        /// Req 3 Criterion 6: No file for entity type returns empty collection.
        /// </summary>
        [Test]
        public async Task GetAll_MissingFile_ReturnsEmptyCollection()
        {
            // Arrange — initialize creates dirs but no colonies file for char-1
            var backend = new JsonMultiFileBackend(_tempDir);
            await backend.InitializeAsync();

            // Act — query a character directory that has no colonies.json
            var result = await backend.GetAllColoniesAsync("nonexistent-char");

            // Assert
            Assert.That(result, Is.Empty);
        }

        /// <summary>
        /// Req 3 Criterion 7: Malformed JSON throws StorageLoadException.
        /// </summary>
        [Test]
        public void MalformedJson_ThrowsStorageLoadException()
        {
            // Arrange — create character directory with malformed colonies.json
            var backend = new JsonMultiFileBackend(_tempDir);
            var charDir = Path.Combine(_tempDir, "characters", "char-bad");
            Directory.CreateDirectory(charDir);
            File.WriteAllText(
                Path.Combine(charDir, "colonies.json"),
                "{ this is not valid json!!!");

            // Act & Assert
            var ex = Assert.ThrowsAsync<StorageLoadException>(
                () => backend.GetAllColoniesAsync("char-bad"));

            Assert.That(ex.BackendType, Is.EqualTo("JsonMultiFile"));
            Assert.That(ex.InnerException, Is.InstanceOf<JsonException>());
        }

        /// <summary>
        /// Req 3 Criterion 4: After upsert, entity JSON file exists at expected path.
        /// </summary>
        [Test]
        public async Task AtomicWrite_FileExists()
        {
            // Arrange
            var backend = new JsonMultiFileBackend(_tempDir);
            await backend.InitializeAsync();

            var colony = new Colony
            {
                UUID = "col-002",
                OwnerUUID = "char-2",
                ColonyName = "Outpost",
            };

            // Act
            await backend.UpsertColonyAsync("char-2", colony);

            // Assert — file exists at the expected path
            var expectedPath = Path.Combine(
                _tempDir, "characters", "char-2", "colonies.json");
            Assert.That(File.Exists(expectedPath), Is.True);

            // No temp file left over
            Assert.That(File.Exists(expectedPath + ".tmp"), Is.False);
        }

        /// <summary>
        /// Req 3: GetStorageInfo returns BackendType="JsonMultiFile" and Location=path.
        /// </summary>
        [Test]
        public void GetStorageInfo_ReturnsCorrectInfo()
        {
            // Arrange
            var backend = new JsonMultiFileBackend(_tempDir);

            // Act
            var info = backend.GetStorageInfo();

            // Assert
            Assert.That(info.BackendType, Is.EqualTo("JsonMultiFile"));
            Assert.That(info.Location, Is.EqualTo(_tempDir));
        }

        /// <summary>
        /// ValidateConnectionAsync returns true when directory exists and is writable.
        /// </summary>
        [Test]
        public async Task ValidateConnection_ExistingDir_ReturnsTrue()
        {
            // Arrange — initialize creates the directory
            var backend = new JsonMultiFileBackend(_tempDir);
            await backend.InitializeAsync();

            // Act
            var result = await backend.ValidateConnectionAsync();

            // Assert
            Assert.That(result, Is.True);
        }
    }
}
