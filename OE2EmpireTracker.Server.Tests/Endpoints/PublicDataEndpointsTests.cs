// -----------------------------------------------------------------------
// <copyright file="PublicDataEndpointsTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;
using OE2EmpireTracker.Server.Tests;

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Unit tests for the enhanced public blueprints endpoint and public data visibility.
/// Validates that global blueprints (characterUUID="") are included, that global and
/// character-shared blueprints are combined in a single result set, and that non-public
/// data types return 404 via the public global endpoint.
/// **Validates: Requirements 4.1, 4.3, 12.2, 12.5**
/// </summary>
[TestFixture]
public class PublicDataEndpointsTests
{
    private TestServerFactory _factory = null!;

    /// <summary>
    /// Sets up the test server factory and seeds the owner token.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestServerFactory();
        _factory.SeedOwnerToken();
    }

    /// <summary>
    /// Tears down the test server.
    /// </summary>
    [OneTimeTearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that GetPublicBlueprints includes global blueprints stored with
    /// characterUUID="" (empty string indicating global ownership).
    /// Validates: Requirement 4.1 — public blueprints endpoint returns global blueprints.
    /// </summary>
    [Test]
    public async Task GetPublicBlueprints_IncludesGlobalBlueprints()
    {
        var storage = _factory.Services.GetRequiredService<IStorageBackend>();

        // Seed global blueprints (characterUUID="")
        var globalBp1 = new Blueprint { UUID = "global-bp-001", Name = "Global Laser Mk1" };
        var globalBp2 = new Blueprint { UUID = "global-bp-002", Name = "Global Shield Mk2" };
        await storage.UpsertBlueprintAsync(string.Empty, globalBp1);
        await storage.UpsertBlueprintAsync(string.Empty, globalBp2);

        try
        {
            using var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/v1/public/blueprints?pageSize=100");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var items = json.GetProperty("items");
            var returnedUUIDs = new HashSet<string>();
            foreach (var item in items.EnumerateArray())
            {
                if (item.TryGetProperty("uuid", out var uuidProp))
                {
                    var uuid = uuidProp.GetString();
                    if (uuid != null)
                    {
                        returnedUUIDs.Add(uuid);
                    }
                }
            }

            Assert.That(returnedUUIDs, Does.Contain("global-bp-001"),
                "Global blueprint 1 should be included in public blueprints");
            Assert.That(returnedUUIDs, Does.Contain("global-bp-002"),
                "Global blueprint 2 should be included in public blueprints");
        }
        finally
        {
            await storage.DeleteBlueprintAsync(string.Empty, "global-bp-001");
            await storage.DeleteBlueprintAsync(string.Empty, "global-bp-002");
        }
    }

    /// <summary>
    /// Verifies that GetPublicBlueprints combines global blueprints (characterUUID="")
    /// and character-shared blueprints into a single result set with correct total count.
    /// Validates: Requirement 4.3 — combined result set of global + shared blueprints.
    /// </summary>
    [Test]
    public async Task GetPublicBlueprints_CombinesGlobalAndSharedBlueprints()
    {
        var storage = _factory.Services.GetRequiredService<IStorageBackend>();

        // Seed global blueprints
        var globalBp = new Blueprint { UUID = "combined-global-bp", Name = "Global Engine" };
        await storage.UpsertBlueprintAsync(string.Empty, globalBp);

        // Seed a character with a Public sharing rule for Blueprints
        var charUUID = "combined-test-char-uuid";
        await storage.UpsertCharacterAsync(new ServerCharacter
        {
            UUID = charUUID,
            Name = "CombinedTestChar",
        });
        await storage.UpsertSharingRulesAsync(charUUID, new List<SharingRule>
        {
            new SharingRule
            {
                Id = "combined-rule-1",
                OwnerCharacterUUID = charUUID,
                TargetType = SharingTargetType.Public,
                TargetUUID = "public",
                DataType = "Blueprints",
            },
        });

        // Seed character blueprints
        var charBp = new Blueprint { UUID = "combined-char-bp", Name = "Shared Weapon" };
        await storage.UpsertBlueprintAsync(charUUID, charBp);

        try
        {
            using var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/v1/public/blueprints?pageSize=100");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var items = json.GetProperty("items");
            var totalCount = json.GetProperty("totalCount").GetInt32();

            var returnedUUIDs = new HashSet<string>();
            foreach (var item in items.EnumerateArray())
            {
                if (item.TryGetProperty("uuid", out var uuidProp))
                {
                    var uuid = uuidProp.GetString();
                    if (uuid != null)
                    {
                        returnedUUIDs.Add(uuid);
                    }
                }
            }

            // Both global and shared blueprints should be in the result
            Assert.That(returnedUUIDs, Does.Contain("combined-global-bp"),
                "Global blueprint should be in combined result set");
            Assert.That(returnedUUIDs, Does.Contain("combined-char-bp"),
                "Character-shared blueprint should be in combined result set");

            // Total count should include both global and shared
            Assert.That(totalCount, Is.GreaterThanOrEqualTo(2),
                "Total count should include both global and shared blueprints");
        }
        finally
        {
            await storage.DeleteBlueprintAsync(string.Empty, "combined-global-bp");
            await storage.DeleteBlueprintAsync(charUUID, "combined-char-bp");
            await storage.UpsertSharingRulesAsync(charUUID, Array.Empty<SharingRule>());
            await storage.DeleteCharacterAsync(charUUID);
        }
    }

    /// <summary>
    /// Verifies that requesting a non-public data type via the public global endpoint
    /// returns HTTP 404, preventing information leakage about private data existence.
    /// Validates: Requirement 12.5 — non-public data type returns 404.
    /// </summary>
    [Test]
    public async Task GetPublicGlobalData_NonPublicDataType_Returns404()
    {
        using var client = _factory.CreateClient();

        // Request a data type that doesn't exist in global storage
        var response = await client.GetAsync("/api/v1/public/global/PlayerProfiles");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
            "Non-public data type 'PlayerProfiles' should return 404");

        // Request another non-public data type
        var response2 = await client.GetAsync("/api/v1/public/global/DeliveryRoutes");
        Assert.That(response2.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
            "Non-public data type 'DeliveryRoutes' should return 404");

        // Request a completely unknown data type
        var response3 = await client.GetAsync("/api/v1/public/global/NonExistentType");
        Assert.That(response3.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
            "Unknown data type should return 404");
    }

    /// <summary>
    /// GET /api/v1/public/systems returns all stored star systems.
    /// Validates: Requirement 5.1.
    /// </summary>
    [Test]
    public async Task GetPublicSystems_WithSystemsStored_ReturnsAllSystems()
    {
        var storage = _factory.Services.GetRequiredService<IStorageBackend>();

        var systems = new List<StarSystem>
        {
            new StarSystem
            {
                Id = 501,
                Name = "Alpha Centauri",
                X = 10.5m,
                Y = 20.3m,
                Quadrant = 1,
                Sector = 2,
                Region = 3,
                Locality = 4,
                SpectralClass = "G2V",
            },
            new StarSystem
            {
                Id = 502,
                Name = "Sirius",
                X = 30.1m,
                Y = 40.7m,
                Quadrant = 2,
                Sector = 1,
                Region = 1,
                Locality = 1,
                SpectralClass = "A1V",
            },
        };

        await storage.UpsertStarSystemsAsync(systems);

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/public/systems");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(json.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(json.GetArrayLength(), Is.GreaterThanOrEqualTo(2));

        var names = new List<string>();
        foreach (var item in json.EnumerateArray())
        {
            if (item.TryGetProperty("name", out var nameProp))
            {
                var name = nameProp.GetString();
                if (name != null)
                {
                    names.Add(name);
                }
            }
        }

        Assert.That(names, Does.Contain("Alpha Centauri"));
        Assert.That(names, Does.Contain("Sirius"));
    }

    /// <summary>
    /// GET /api/v1/public/systems returns an empty array when no systems are stored.
    /// Validates: Requirement 5.4.
    /// </summary>
    [Test]
    public async Task GetPublicSystems_WithNoSystems_ReturnsEmptyArray()
    {
        using var freshFactory = new TestServerFactory();
        freshFactory.SeedOwnerToken();

        using var client = freshFactory.CreateClient();
        var response = await client.GetAsync("/api/v1/public/systems");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(json.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(json.GetArrayLength(), Is.EqualTo(0));
    }

    /// <summary>
    /// GET /api/v1/public/systems/{systemId}/colonies returns only ColonyName, Size,
    /// and PlanetName fields — never internal details like structures or resources.
    /// Validates: Requirements 7.1, 7.2.
    /// </summary>
    [Test]
    public async Task GetPublicColonySummaries_ReturnsOnlyNameSizePlanet()
    {
        var storage = _factory.Services.GetRequiredService<IStorageBackend>();

        // Ensure a system exists
        var systems = new List<StarSystem>
        {
            new StarSystem { Id = 700, Name = "ColonyTestSystem" },
        };
        await storage.UpsertStarSystemsAsync(systems);

        // Create a character with a colony in that system
        var charUUID = Guid.NewGuid().ToString();
        await storage.UpsertCharacterAsync(new ServerCharacter
        {
            UUID = charUUID,
            Name = "ColonyTestChar",
        });

        var colony = new Colony
        {
            UUID = Guid.NewGuid().ToString(),
            ColonyName = "My Colony",
            PlanetName = "ColonyTestSystem Planet 1",
            SystemName = "ColonyTestSystem",
        };
        await storage.UpsertColonyAsync(charUUID, colony);

        try
        {
            using var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/v1/public/systems/700/colonies");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.That(json.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(json.GetArrayLength(), Is.GreaterThanOrEqualTo(1));

            var firstColony = json[0];

            // Should have only the public summary fields
            Assert.That(
                firstColony.TryGetProperty("colonyName", out var nameEl),
                Is.True,
                "Colony summary should have colonyName");
            Assert.That(nameEl.GetString(), Is.EqualTo("My Colony"));

            Assert.That(
                firstColony.TryGetProperty("size", out _),
                Is.True,
                "Colony summary should have size");

            Assert.That(
                firstColony.TryGetProperty("planetName", out var planetEl),
                Is.True,
                "Colony summary should have planetName");
            Assert.That(planetEl.GetString(), Is.EqualTo("ColonyTestSystem Planet 1"));

            // Should NOT have internal colony fields
            Assert.That(
                firstColony.TryGetProperty("structures", out _),
                Is.False,
                "Colony summary must not expose structures");
            Assert.That(
                firstColony.TryGetProperty("resources", out _),
                Is.False,
                "Colony summary must not expose resources");
            Assert.That(
                firstColony.TryGetProperty("inventory", out _),
                Is.False,
                "Colony summary must not expose inventory");
            Assert.That(
                firstColony.TryGetProperty("uuid", out _),
                Is.False,
                "Colony summary must not expose UUID");
        }
        finally
        {
            await storage.DeleteColonyAsync(charUUID, colony.UUID);
            await storage.DeleteCharacterAsync(charUUID);
        }
    }

    /// <summary>
    /// GET /api/v1/public/systems/{systemId}/planets returns an empty array
    /// when no planets exist for the specified system.
    /// Validates: Requirement 6.3.
    /// </summary>
    [Test]
    public async Task GetPublicPlanets_WithNoPlanets_ReturnsEmptyArray()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/public/systems/9999/planets");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(json.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(json.GetArrayLength(), Is.EqualTo(0));
    }

    /// <summary>
    /// GET /api/v1/public/systems/{systemId}/asteroids returns an empty array
    /// when no asteroids exist for the specified system.
    /// Validates: Requirement 6.4.
    /// </summary>
    [Test]
    public async Task GetPublicAsteroids_WithNoAsteroids_ReturnsEmptyArray()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/public/systems/9999/asteroids");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(json.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(json.GetArrayLength(), Is.EqualTo(0));
    }
}
