// -----------------------------------------------------------------------
// <copyright file="JsonSingleFileBackendLifecycleTests.cs" company="OE2EmpireTracker">
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

namespace OE2EmpireTracker.Tests.Storage
{
    /// <summary>
    /// Tests for JsonSingleFileBackend lifecycle: initialization, missing/empty
    /// file handling, malformed JSON, connection validation, and storage info.
    /// Satisfies: Req 2 Criteria 6-7, Req 10 Criteria 1-2, Req 11 Criterion 1.
    /// </summary>
    [TestFixture]
    public class JsonSingleFileBackendLifecycleTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(
                Path.GetTempPath(),
                "oe2test_" + Guid.NewGuid().ToString("N"));
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
        /// Req 2 Criterion 6: Missing file returns empty collections.
        /// </summary>
        [Test]
        public async Task InitializeAsync_MissingFile_ReturnsEmptyCollections()
        {
            // Arrange — no file created in temp dir
            var config = new StorageBackendConfig
            {
                ConnectionString = Path.Combine(_tempDir, "PlayerData.json")
            };
            var backend = new JsonSingleFileBackend(config);

            // Act
            await backend.InitializeAsync();

            // Assert — GetAll returns empty
            var colonies = await backend.GetAllColoniesAsync("test-char");
            Assert.That(colonies, Is.Empty);

            var blueprints = await backend.GetAllBlueprintsAsync("test-char");
            Assert.That(blueprints, Is.Empty);
        }

        /// <summary>
        /// Req 2 Criterion 6: Empty file (0 bytes) returns empty collections.
        /// </summary>
        [Test]
        public async Task InitializeAsync_EmptyFile_ReturnsEmptyCollections()
        {
            // Arrange — create an empty 0-byte file
            string filePath = Path.Combine(_tempDir, "PlayerData.json");
            File.WriteAllText(filePath, string.Empty);

            var config = new StorageBackendConfig
            {
                ConnectionString = filePath
            };
            var backend = new JsonSingleFileBackend(config);

            // Act
            await backend.InitializeAsync();

            // Assert
            var colonies = await backend.GetAllColoniesAsync("test-char");
            Assert.That(colonies, Is.Empty);
        }

        /// <summary>
        /// Req 11 Criterion 1: Valid JSON loads data correctly.
        /// </summary>
        [Test]
        public async Task InitializeAsync_ValidJson_LoadsDataCorrectly()
        {
            // Arrange — write a minimal valid PlayerData.json with one colony
            string json = @"{
  ""DataVersion"": 8,
  ""CurrentPlayerUUID"": ""player-1"",
  ""Colony"": [
    { ""UUID"": ""col-1"", ""OwnerUUID"": ""player-1"", ""ColonyName"": ""TestColony"" }
  ],
  ""PlayerProfile"": [],
  ""Blueprint"": [],
  ""Survey"": [],
  ""DeliveryRoute"": [],
  ""DeliveryPlan"": [],
  ""PricingPlan"": [],
  ""BuildPlan"": [],
  ""ShipTemplate"": [],
  ""Ship"": [],
  ""Station"": [],
  ""MarketListing"": [],
  ""MarketTransaction"": [],
  ""StockPlan"": [],
  ""StockProfile"": [],
  ""SupplyChain"": [],
  ""WarehouseOverflowRule"": [],
  ""Faction"": [],
  ""ExternalCharacter"": [],
  ""Asteroid"": [],
  ""bankingTransaction"": [],
  ""bankingBalance"": 0,
  ""mailMessage"": [],
  ""MarketSyncData"": {}
}";
            string filePath = Path.Combine(_tempDir, "PlayerData.json");
            File.WriteAllText(filePath, json);

            var config = new StorageBackendConfig
            {
                ConnectionString = filePath
            };
            var backend = new JsonSingleFileBackend(config);

            // Act
            await backend.InitializeAsync();

            // Assert
            var colonies = await backend.GetAllColoniesAsync("player-1");
            Assert.That(colonies, Has.Count.EqualTo(1));
            Assert.That(colonies[0].UUID, Is.EqualTo("col-1"));
            Assert.That(colonies[0].ColonyName, Is.EqualTo("TestColony"));
        }

        /// <summary>
        /// Req 2 Criterion 7: Malformed JSON throws StorageLoadException with file
        /// path and inner JsonReaderException.
        /// </summary>
        [Test]
        public void InitializeAsync_MalformedJson_ThrowsStorageLoadException()
        {
            // Arrange — write invalid JSON
            string filePath = Path.Combine(_tempDir, "PlayerData.json");
            File.WriteAllText(filePath, "{ this is not valid json!!!");

            var config = new StorageBackendConfig
            {
                ConnectionString = filePath
            };
            var backend = new JsonSingleFileBackend(config);

            // Act & Assert
            var ex = Assert.ThrowsAsync<StorageLoadException>(
                () => backend.InitializeAsync());

            Assert.That(ex.Location, Is.EqualTo(filePath));
            Assert.That(ex.BackendType, Is.EqualTo("JsonSingleFile"));
            Assert.That(ex.InnerException, Is.TypeOf<JsonReaderException>());
        }

        /// <summary>
        /// ValidateConnectionAsync returns true for a valid writable directory.
        /// </summary>
        [Test]
        public async Task ValidateConnectionAsync_ValidDirectory_ReturnsTrue()
        {
            // Arrange — point to existing temp dir
            string filePath = Path.Combine(_tempDir, "PlayerData.json");
            var config = new StorageBackendConfig
            {
                ConnectionString = filePath
            };
            var backend = new JsonSingleFileBackend(config);

            // Act
            bool result = await backend.ValidateConnectionAsync();

            // Assert
            Assert.That(result, Is.True);
        }

        /// <summary>
        /// ValidateConnectionAsync returns false for a non-existent directory.
        /// </summary>
        [Test]
        public async Task ValidateConnectionAsync_NonExistentDirectory_ReturnsFalse()
        {
            // Arrange — point to a path in a non-existent directory
            string filePath = Path.Combine(
                _tempDir,
                "nonexistent_subdir",
                "PlayerData.json");
            var config = new StorageBackendConfig
            {
                ConnectionString = filePath
            };
            var backend = new JsonSingleFileBackend(config);

            // Act
            bool result = await backend.ValidateConnectionAsync();

            // Assert
            Assert.That(result, Is.False);
        }

        /// <summary>
        /// GetStorageInfo returns correct BackendType and Location.
        /// </summary>
        [Test]
        public void GetStorageInfo_ReturnsCorrectBackendTypeAndLocation()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "PlayerData.json");
            var config = new StorageBackendConfig
            {
                ConnectionString = filePath
            };
            var backend = new JsonSingleFileBackend(config);

            // Act
            StorageInfo info = backend.GetStorageInfo();

            // Assert
            Assert.That(info.BackendType, Is.EqualTo("JsonSingleFile"));
            Assert.That(info.Location, Is.EqualTo(filePath));
        }
    }
}
