// -----------------------------------------------------------------------
// <copyright file="JsonFileStorageBackendTypedTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Tests.Storage;

/// <summary>
/// Integration tests for JsonFileStorageBackend typed methods.
/// Validates: Requirements 22.3, 22.7, 22.8.
/// </summary>
[TestFixture]
public class JsonFileStorageBackendTypedTests
{
    private const string CharUUID = "test-char-001";

    private string _dataPath = null!;
    private JsonFileStorageBackend _backend = null!;

    /// <summary>
    /// Creates a temp directory and initializes the storage backend.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _dataPath = Path.Combine(
            Path.GetTempPath(),
            "oe2-storage-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_dataPath);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:DataPath"] = _dataPath,
            })
            .Build();

        var logger = NullLogger<JsonFileStorageBackend>.Instance;
        _backend = new JsonFileStorageBackend(config, logger);
    }

    /// <summary>
    /// Cleans up the temp directory after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_dataPath))
        {
            Directory.Delete(_dataPath, true);
        }
    }

    /// <summary>
    /// Upsert then get returns equivalent entity (round-trip).
    /// Validates: Requirement 22.3.
    /// </summary>
    [Test]
    public async Task UpsertThenGet_ReturnsEquivalentEntity()
    {
        var colony = new Colony
        {
            UUID = "colony-rt-001",
            PlanetName = "Mars",
            ColonyName = "Alpha Base",
            SystemName = "Sol",
        };

        await _backend.UpsertColonyAsync(CharUUID, colony);
        var retrieved = await _backend.GetColonyAsync(CharUUID, "colony-rt-001");

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.UUID, Is.EqualTo("colony-rt-001"));
        Assert.That(retrieved.PlanetName, Is.EqualTo("Mars"));
        Assert.That(retrieved.ColonyName, Is.EqualTo("Alpha Base"));
        Assert.That(retrieved.SystemName, Is.EqualTo("Sol"));
    }

    /// <summary>
    /// GetAll returns all entities for a character.
    /// Validates: Requirement 22.7.
    /// </summary>
    [Test]
    public async Task GetAll_ReturnsAllEntitiesForCharacter()
    {
        var colony1 = new Colony
        {
            UUID = "colony-all-001",
            PlanetName = "Earth",
            ColonyName = "Home",
            SystemName = "Sol",
        };
        var colony2 = new Colony
        {
            UUID = "colony-all-002",
            PlanetName = "Venus",
            ColonyName = "Cloud City",
            SystemName = "Sol",
        };

        await _backend.UpsertColonyAsync(CharUUID, colony1);
        await _backend.UpsertColonyAsync(CharUUID, colony2);

        var all = await _backend.GetAllColoniesAsync(CharUUID);

        Assert.That(all.Count, Is.EqualTo(2));
        Assert.That(all.Select(c => c.UUID), Does.Contain("colony-all-001"));
        Assert.That(all.Select(c => c.UUID), Does.Contain("colony-all-002"));
    }

    /// <summary>
    /// Delete removes the entity from storage.
    /// Validates: Requirement 22.8.
    /// </summary>
    [Test]
    public async Task Delete_RemovesEntity()
    {
        var colony = new Colony
        {
            UUID = "colony-del-001",
            PlanetName = "Jupiter",
            ColonyName = "Gas Station",
            SystemName = "Sol",
        };

        await _backend.UpsertColonyAsync(CharUUID, colony);
        await _backend.DeleteColonyAsync(CharUUID, "colony-del-001");

        var retrieved = await _backend.GetColonyAsync(CharUUID, "colony-del-001");
        Assert.That(retrieved, Is.Null);
    }

    /// <summary>
    /// Get returns null for non-existent entity.
    /// Validates: Requirement 22.7.
    /// </summary>
    [Test]
    public async Task Get_ReturnsNull_ForNonExistentEntity()
    {
        var retrieved = await _backend.GetColonyAsync(CharUUID, "nonexistent-uuid");
        Assert.That(retrieved, Is.Null);
    }

    /// <summary>
    /// GetAll returns empty list for character with no entities.
    /// Validates: Requirement 22.7.
    /// </summary>
    [Test]
    public async Task GetAll_ReturnsEmptyList_ForCharacterWithNoEntities()
    {
        var all = await _backend.GetAllColoniesAsync("no-data-char");
        Assert.That(all, Is.Empty);
    }

    /// <summary>
    /// Upsert updates existing entity when UUID matches.
    /// Validates: Requirement 22.3.
    /// </summary>
    [Test]
    public async Task Upsert_UpdatesExistingEntity_WhenUUIDMatches()
    {
        var colony = new Colony
        {
            UUID = "colony-upd-001",
            PlanetName = "Saturn",
            ColonyName = "Ring Base",
            SystemName = "Sol",
        };

        await _backend.UpsertColonyAsync(CharUUID, colony);

        colony.ColonyName = "Updated Ring Base";
        await _backend.UpsertColonyAsync(CharUUID, colony);

        var all = await _backend.GetAllColoniesAsync(CharUUID);
        Assert.That(all.Count, Is.EqualTo(1));
        Assert.That(all[0].ColonyName, Is.EqualTo("Updated Ring Base"));
    }
}
