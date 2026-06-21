// -----------------------------------------------------------------------
// <copyright file="JsonSingleFileBackendCrudTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Storage;
using OE2EmpireTracker.Models;

using BlueprintModel = OE2EmpireTracker.Models.Blueprint;
namespace OE2EmpireTracker.Tests.Storage
{
    [TestFixture]
    public class JsonSingleFileBackendCrudTests
    {
        private string _testDir;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "JsonSingleFileCrud_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }

        [Test]
        public async Task ColonyUpsertAndGet_ReturnsMatchingProperties()
        {
            var backend = await CreateBackendAsync();

            var colony = new Colony
            {
                UUID = "colony-001",
                OwnerUUID = "char-A",
                ColonyName = "Test Colony",
                PlanetName = "Earth",
                SystemName = "Sol",
                ColonySize = 5
            };

            await backend.UpsertColonyAsync("char-A", colony);

            var result = await backend.GetColonyAsync("char-A", "colony-001");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.UUID, Is.EqualTo("colony-001"));
            Assert.That(result.OwnerUUID, Is.EqualTo("char-A"));
            Assert.That(result.ColonyName, Is.EqualTo("Test Colony"));
            Assert.That(result.PlanetName, Is.EqualTo("Earth"));
            Assert.That(result.SystemName, Is.EqualTo("Sol"));
            Assert.That(result.ColonySize, Is.EqualTo(5));
        }

        [Test]
        public async Task ColonyDelete_RemovesFromGetAll()
        {
            var backend = await CreateBackendAsync();

            var colony = new Colony
            {
                UUID = "colony-del",
                OwnerUUID = "char-A",
                ColonyName = "Doomed Colony",
                SystemName = "Alpha"
            };

            await backend.UpsertColonyAsync("char-A", colony);
            await backend.DeleteColonyAsync("char-A", "colony-del");

            var all = await backend.GetAllColoniesAsync("char-A");

            Assert.That(all, Is.Empty);
        }

        [Test]
        public async Task BlueprintUpsertAndGetAll_ReturnsCorrectCount()
        {
            var backend = await CreateBackendAsync();

            var bp1 = new BlueprintModel
            {
                UUID = "bp-001",
                OwnerUUID = "char-A",
                Name = "Hull Mk1"
            };

            var bp2 = new BlueprintModel
            {
                UUID = "bp-002",
                OwnerUUID = "char-A",
                Name = "Hull Mk2"
            };

            var bp3 = new BlueprintModel
            {
                UUID = "bp-003",
                OwnerUUID = "char-A",
                Name = "Engine Mk1"
            };

            await backend.UpsertBlueprintAsync("char-A", bp1);
            await backend.UpsertBlueprintAsync("char-A", bp2);
            await backend.UpsertBlueprintAsync("char-A", bp3);

            var all = await backend.GetAllBlueprintsAsync("char-A");

            Assert.That(all.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task MultiCharacterIsolation_GetAllReturnsOnlyOwnedEntities()
        {
            var backend = await CreateBackendAsync();

            var colonyA = new Colony
            {
                UUID = "colony-A1",
                OwnerUUID = "char-A",
                ColonyName = "Alpha Colony",
                SystemName = "Sol"
            };

            var colonyB = new Colony
            {
                UUID = "colony-B1",
                OwnerUUID = "char-B",
                ColonyName = "Beta Colony",
                SystemName = "Proxima"
            };

            await backend.UpsertColonyAsync("char-A", colonyA);
            await backend.UpsertColonyAsync("char-B", colonyB);

            var allA = await backend.GetAllColoniesAsync("char-A");
            var allB = await backend.GetAllColoniesAsync("char-B");

            Assert.That(allA.Count, Is.EqualTo(1));
            Assert.That(allA[0].UUID, Is.EqualTo("colony-A1"));
            Assert.That(allB.Count, Is.EqualTo(1));
            Assert.That(allB[0].UUID, Is.EqualTo("colony-B1"));
        }

        [Test]
        public async Task SerializationFormat_ProducesIndentedJsonWithCorrectSettings()
        {
            var backend = await CreateBackendAsync();

            var colony = new Colony
            {
                UUID = "colony-fmt",
                OwnerUUID = "char-A",
                ColonyName = "Format Test",
                SystemName = "TestSys"
            };

            await backend.UpsertColonyAsync("char-A", colony);

            string filePath = Path.Combine(_testDir, "PlayerData.json");
            string rawJson = File.ReadAllText(filePath);

            // Verify indented formatting (multi-line)
            Assert.That(rawJson, Does.Contain("\n"));

            // Verify it's valid JSON
            var parsed = JObject.Parse(rawJson);
            Assert.That(parsed, Is.Not.Null);

            // Verify NullValueHandling.Ignore — null properties should not appear
            // Colony.LegacyUUID is null by default and should be absent
            var colonyArray = parsed["Colony"] as JArray;
            Assert.That(colonyArray, Is.Not.Null);
            Assert.That(colonyArray.Count, Is.EqualTo(1));
            var colonyToken = colonyArray[0] as JObject;
            Assert.That(colonyToken, Is.Not.Null);
            Assert.That(colonyToken.ContainsKey("LegacyUUID"), Is.False,
                "Null properties should be omitted (NullValueHandling.Ignore)");

            // Verify DefaultValueHandling.Ignore — zero-value properties should be absent
            // Colony.ColonyId defaults to 0 and has [DefaultValue(0)]
            Assert.That(colonyToken.ContainsKey("colonyId"), Is.False,
                "Default-value properties should be omitted (DefaultValueHandling.Ignore)");
        }

        [Test]
        public async Task AtomicWrite_CreatesBackupFileAfterSecondWrite()
        {
            var backend = await CreateBackendAsync();

            var colony1 = new Colony
            {
                UUID = "colony-v1",
                OwnerUUID = "char-A",
                ColonyName = "Version 1",
                SystemName = "Sys"
            };

            await backend.UpsertColonyAsync("char-A", colony1);

            // Second write triggers .bak creation (SafeFileWriter creates backup on overwrite)
            var colony2 = new Colony
            {
                UUID = "colony-v2",
                OwnerUUID = "char-A",
                ColonyName = "Version 2",
                SystemName = "Sys2"
            };

            await backend.UpsertColonyAsync("char-A", colony2);

            string bakPath = Path.Combine(_testDir, "PlayerData.json.bak");
            Assert.That(File.Exists(bakPath), Is.True, "SafeFileWriter should create .bak on overwrite");
        }

        [Test]
        public async Task RoundTripPersistence_NewBackendInstanceLoadsData()
        {
            // First instance: upsert data
            var backend1 = await CreateBackendAsync();

            var colony = new Colony
            {
                UUID = "colony-rt",
                OwnerUUID = "char-A",
                ColonyName = "Persisted Colony",
                PlanetName = "Mars",
                SystemName = "Sol",
                ColonySize = 3
            };

            await backend1.UpsertColonyAsync("char-A", colony);

            // Second instance: load from same directory
            var config2 = new StorageBackendConfig { ConnectionString = _testDir };
            var backend2 = new JsonSingleFileBackend(config2);
            await backend2.InitializeAsync();

            var loaded = await backend2.GetColonyAsync("char-A", "colony-rt");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.UUID, Is.EqualTo("colony-rt"));
            Assert.That(loaded.ColonyName, Is.EqualTo("Persisted Colony"));
            Assert.That(loaded.PlanetName, Is.EqualTo("Mars"));
            Assert.That(loaded.SystemName, Is.EqualTo("Sol"));
            Assert.That(loaded.ColonySize, Is.EqualTo(3));
        }

        private async Task<JsonSingleFileBackend> CreateBackendAsync()
        {
            var config = new StorageBackendConfig { ConnectionString = _testDir };
            var backend = new JsonSingleFileBackend(config);
            await backend.InitializeAsync();
            return backend;
        }
    }
}
