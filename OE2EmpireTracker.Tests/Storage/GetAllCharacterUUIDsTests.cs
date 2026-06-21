// -----------------------------------------------------------------------
// <copyright file="GetAllCharacterUUIDsTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Storage;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Storage
{
    /// <summary>
    /// Tests for GetAllCharacterUUIDsAsync across local file-based backends.
    /// Satisfies: Req 6, Criterion 3.
    /// </summary>
    [TestFixture]
    public class GetAllCharacterUUIDsTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(
                Path.GetTempPath(),
                "oe2test_charuuids_" + Guid.NewGuid().ToString("N"));
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

        // ═══════════════════════════════════════════════════════════
        // JsonSingleFileBackend
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Empty JsonSingleFileBackend returns empty character list.
        /// </summary>
        [Test]
        public async Task JsonSingleFile_Empty_ReturnsEmptyList()
        {
            var backend = await CreateSingleFileBackendAsync();

            var result = await backend.GetAllCharacterUUIDsAsync();

            Assert.That(result, Is.Empty);
        }

        /// <summary>
        /// After upserting colonies for two characters, returns both UUIDs.
        /// </summary>
        [Test]
        public async Task JsonSingleFile_TwoCharacters_ReturnsBothUUIDs()
        {
            var backend = await CreateSingleFileBackendAsync();

            var colony1 = new Colony
            {
                UUID = "col-001",
                OwnerUUID = "char-alpha",
                ColonyName = "Alpha Base",
                SystemName = "Sol",
            };

            var colony2 = new Colony
            {
                UUID = "col-002",
                OwnerUUID = "char-beta",
                ColonyName = "Beta Outpost",
                SystemName = "Proxima",
            };

            await backend.UpsertColonyAsync("char-alpha", colony1);
            await backend.UpsertColonyAsync("char-beta", colony2);

            var result = await backend.GetAllCharacterUUIDsAsync();

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result, Does.Contain("char-alpha"));
            Assert.That(result, Does.Contain("char-beta"));
        }

        // ═══════════════════════════════════════════════════════════
        // JsonMultiFileBackend
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Empty JsonMultiFileBackend returns empty character list.
        /// </summary>
        [Test]
        public async Task JsonMultiFile_Empty_ReturnsEmptyList()
        {
            var multiDir = Path.Combine(_tempDir, "multi");
            var backend = new JsonMultiFileBackend(multiDir);
            await backend.InitializeAsync();

            var result = await backend.GetAllCharacterUUIDsAsync();

            Assert.That(result, Is.Empty);
        }

        /// <summary>
        /// After upserting colonies for two characters, returns both UUIDs.
        /// </summary>
        [Test]
        public async Task JsonMultiFile_TwoCharacters_ReturnsBothUUIDs()
        {
            var multiDir = Path.Combine(_tempDir, "multi");
            var backend = new JsonMultiFileBackend(multiDir);
            await backend.InitializeAsync();

            var colony1 = new Colony
            {
                UUID = "col-001",
                OwnerUUID = "char-alpha",
                ColonyName = "Alpha Base",
                SystemName = "Sol",
            };

            var colony2 = new Colony
            {
                UUID = "col-002",
                OwnerUUID = "char-beta",
                ColonyName = "Beta Outpost",
                SystemName = "Proxima",
            };

            await backend.UpsertColonyAsync("char-alpha", colony1);
            await backend.UpsertColonyAsync("char-beta", colony2);

            var result = await backend.GetAllCharacterUUIDsAsync();

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result, Does.Contain("char-alpha"));
            Assert.That(result, Does.Contain("char-beta"));
        }

        // ═══════════════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════════════

        private async Task<IStorageBackend> CreateSingleFileBackendAsync()
        {
            var config = new StorageBackendConfig { ConnectionString = _tempDir };
            var backend = new JsonSingleFileBackend(config);
            await backend.InitializeAsync();
            return backend;
        }
    }
}
