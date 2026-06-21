// -----------------------------------------------------------------------
// <copyright file="JsonSingleFileBackendMigrationTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using OE2EmpireTracker.Common.Storage;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Tests.Storage
{
    /// <summary>
    /// Tests for JsonSingleFileBackend DataVersion migration handling during load.
    /// Satisfies: Req 2 Criteria 9-10, Req 11 Criterion 6.
    /// </summary>
    [TestFixture]
    public class JsonSingleFileBackendMigrationTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(
                Path.GetTempPath(),
                "oe2test_migration_" + Guid.NewGuid().ToString("N"));
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
        /// Req 2 Criterion 9, Req 11 Criterion 6: Loading a file with a DataVersion
        /// lower than current does not throw — migration is best-effort and graceful.
        /// </summary>
        [Test]
        public async Task OlderDataVersion_LoadsSuccessfully()
        {
            // Arrange — write JSON with DataVersion = 1 (older than current 8)
            string json = @"{
  ""DataVersion"": 1,
  ""CurrentPlayerUUID"": ""player-1"",
  ""Colony"": [],
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

            // Act & Assert — should not throw; graceful handling per Req 11 Criterion 6
            Assert.DoesNotThrowAsync(async () => await backend.InitializeAsync());

            // Verify data still loads correctly despite older version
            var colonies = await backend.GetAllColoniesAsync("player-1");
            Assert.That(colonies, Is.Empty);
        }

        /// <summary>
        /// Req 2 Criterion 10: Loading a file with DataVersion equal to current
        /// loads without any migration concern or exception.
        /// </summary>
        [Test]
        public async Task CurrentDataVersion_LoadsWithoutMigration()
        {
            // Arrange — write JSON with DataVersion = MigrationRunner.CurrentVersion (8)
            string json = @"{
  ""DataVersion"": " + MigrationRunner.CurrentVersion + @",
  ""CurrentPlayerUUID"": ""player-1"",
  ""Colony"": [
    { ""UUID"": ""col-1"", ""OwnerUUID"": ""player-1"", ""ColonyName"": ""MyColony"" }
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

            // Assert — data loaded correctly with no migration needed
            var colonies = await backend.GetAllColoniesAsync("player-1");
            Assert.That(colonies, Has.Count.EqualTo(1));
            Assert.That(colonies[0].UUID, Is.EqualTo("col-1"));
            Assert.That(colonies[0].ColonyName, Is.EqualTo("MyColony"));
        }

        /// <summary>
        /// Req 2 Criterion 9, Req 11 Criterion 6: A file with no DataVersion field
        /// defaults to version 0 (per PlayerRoot constructor) and loads gracefully.
        /// </summary>
        [Test]
        public async Task MissingDataVersion_DefaultsToZero()
        {
            // Arrange — write JSON WITHOUT the DataVersion field
            string json = @"{
  ""CurrentPlayerUUID"": ""player-1"",
  ""Colony"": [
    { ""UUID"": ""col-2"", ""OwnerUUID"": ""player-1"", ""ColonyName"": ""OldColony"" }
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

            // Act — should not throw; treated as version 0, migration logged
            Assert.DoesNotThrowAsync(async () => await backend.InitializeAsync());

            // Assert — data still loads correctly
            var colonies = await backend.GetAllColoniesAsync("player-1");
            Assert.That(colonies, Has.Count.EqualTo(1));
            Assert.That(colonies[0].UUID, Is.EqualTo("col-2"));
            Assert.That(colonies[0].ColonyName, Is.EqualTo("OldColony"));
        }
    }
}
