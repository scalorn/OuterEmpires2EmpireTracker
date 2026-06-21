// -----------------------------------------------------------------------
// <copyright file="StarSystemStorageTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Tests.Storage;

/// <summary>
/// Unit tests for JsonFileStorageBackend star system methods.
/// Validates: Requirements 8.1, 8.2, 8.3, 8.4.
/// </summary>
[TestFixture]
public class StarSystemStorageTests
{
    private string _dataPath = null!;
    private JsonFileStorageBackend _backend = null!;

    /// <summary>
    /// Creates a temp directory and initializes the storage backend.
    /// </summary>
    [SetUp]
    public async Task Setup()
    {
        _dataPath = Path.Combine(
            Path.GetTempPath(),
            "oe2-starsys-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_dataPath);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:DataPath"] = _dataPath,
            })
            .Build();

        var logger = NullLogger<JsonFileStorageBackend>.Instance;
        _backend = new JsonFileStorageBackend(config, logger);
        await _backend.InitializeAsync();
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
    /// GetAllStarSystemsAsync returns empty list on a fresh backend.
    /// Validates: Requirement 8.1.
    /// </summary>
    [Test]
    public async Task GetAllStarSystemsAsync_ReturnsEmptyList_Initially()
    {
        var systems = await _backend.GetAllStarSystemsAsync();

        Assert.That(systems, Is.Not.Null);
        Assert.That(systems, Is.Empty);
    }

    /// <summary>
    /// UpsertStarSystemsAsync stores systems and GetAllStarSystemsAsync retrieves them.
    /// Validates: Requirements 8.1, 8.3.
    /// </summary>
    [Test]
    public async Task UpsertStarSystemsAsync_StoresAndRetrievesCorrectly()
    {
        var systems = new List<StarSystem>
        {
            new StarSystem { Id = 1, Name = "Sol", X = 0.0m, Y = 0.0m, SpectralClass = "G2V" },
            new StarSystem { Id = 2, Name = "Alpha Centauri", X = 1.3m, Y = 0.5m, SpectralClass = "G2V" },
            new StarSystem { Id = 3, Name = "Sirius", X = -2.6m, Y = 1.1m, SpectralClass = "A1V" },
        };

        await _backend.UpsertStarSystemsAsync(systems);
        var retrieved = await _backend.GetAllStarSystemsAsync();

        Assert.That(retrieved.Count, Is.EqualTo(3));
        Assert.That(retrieved.Select(s => s.Id), Is.EquivalentTo(new[] { 1, 2, 3 }));
        Assert.That(retrieved.Select(s => s.Name), Is.EquivalentTo(new[] { "Sol", "Alpha Centauri", "Sirius" }));

        var sol = retrieved.First(s => s.Id == 1);
        Assert.That(sol.Name, Is.EqualTo("Sol"));
        Assert.That(sol.X, Is.EqualTo(0.0m));
        Assert.That(sol.Y, Is.EqualTo(0.0m));
        Assert.That(sol.SpectralClass, Is.EqualTo("G2V"));
    }


    /// <summary>
    /// UpsertStarSystemsAsync with same Id overwrites existing system (upsert semantics).
    /// Validates: Requirements 8.2, 8.4.
    /// </summary>
    [Test]
    public async Task UpsertStarSystemsAsync_WithSameId_Overwrites()
    {
        var initial = new List<StarSystem>
        {
            new StarSystem { Id = 10, Name = "Original", X = 1.0m, Y = 2.0m, SpectralClass = "K0V" },
            new StarSystem { Id = 20, Name = "Other", X = 3.0m, Y = 4.0m, SpectralClass = "M2V" },
        };

        await _backend.UpsertStarSystemsAsync(initial);

        var updated = new List<StarSystem>
        {
            new StarSystem { Id = 10, Name = "Updated", X = 5.0m, Y = 6.0m, SpectralClass = "B3V" },
            new StarSystem { Id = 20, Name = "Other", X = 3.0m, Y = 4.0m, SpectralClass = "M2V" },
        };

        await _backend.UpsertStarSystemsAsync(updated);
        var retrieved = await _backend.GetAllStarSystemsAsync();

        Assert.That(retrieved.Count, Is.EqualTo(2));

        var system10 = retrieved.First(s => s.Id == 10);
        Assert.That(system10.Name, Is.EqualTo("Updated"));
        Assert.That(system10.X, Is.EqualTo(5.0m));
        Assert.That(system10.Y, Is.EqualTo(6.0m));
        Assert.That(system10.SpectralClass, Is.EqualTo("B3V"));
    }
}
