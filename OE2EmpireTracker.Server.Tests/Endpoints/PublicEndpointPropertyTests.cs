// -----------------------------------------------------------------------
// <copyright file="PublicEndpointPropertyTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net.Http.Json;
using System.Text.Json;
using FsCheck;
using FsCheck.NUnit;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;
using OE2EmpireTracker.Server.Tests;

#pragma warning disable SA1204 // Static members should appear before non-static members

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Property 4: Visibility Isolation.
/// For any response from any public endpoint, no private entity fields are present.
/// Private fields include: structures, resources, inventories, surveys, profiles,
/// routes, plans, ships, templates, listings, transactions.
/// **Validates: Requirements 12.1–12.5**
/// </summary>
[TestFixture]
[NonParallelizable]
public class PublicEndpointPropertyTests
{
    /// <summary>
    /// Private field names that must never appear in any public endpoint response.
    /// These represent internal entity data that should be filtered out.
    /// </summary>
    private static readonly HashSet<string> PrivateFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "structures",
        "resources",
        "inventories",
        "items",
        "surveys",
        "profiles",
        "routes",
        "plans",
        "ships",
        "templates",
        "listings",
        "transactions",
        "commodities",
        "locks",
        "ownerUUID",
        "lastImportDateTime",
    };

    /// <summary>
    /// Sets up the test server factory and seeds the owner token.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        var factory = SharedTestServer.Factory;
    }

    /// <summary>
    /// Tears down the test server.
    /// </summary>
    [OneTimeTearDown]
    public void TearDown()
    {
    }

    /// <summary>
    /// Property: For any randomly generated colony data seeded into the system,
    /// the public colony summary endpoint never exposes private entity fields.
    /// Only ColonyName, Size, and PlanetName are permitted.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 25)]
    public Property ColonySummaryEndpoint_NeverExposesPrivateFields()
    {
        return Prop.ForAll(
            ColonyScenarioArbitrary(),
            scenario =>
            {
                RunColonyVisibilityCheck(scenario).GetAwaiter().GetResult();
            });
    }

    /// <summary>
    /// Property: For any randomly generated blueprint data seeded into the system,
    /// the public blueprints endpoint never exposes private entity fields.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 25)]
    public Property PublicBlueprintsEndpoint_NeverExposesPrivateFields()
    {
        return Prop.ForAll(
            BlueprintScenarioArbitrary(),
            scenario =>
            {
                RunBlueprintVisibilityCheck(scenario).GetAwaiter().GetResult();
            });
    }

    /// <summary>
    /// Property: For any randomly generated system data, the public systems
    /// endpoint never exposes private entity fields.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 25)]
    public Property PublicSystemsEndpoint_NeverExposesPrivateFields()
    {
        return Prop.ForAll(
            SystemScenarioArbitrary(),
            scenario =>
            {
                RunSystemVisibilityCheck(scenario).GetAwaiter().GetResult();
            });
    }

    /// <summary>
    /// Property: For any randomly generated asteroid data, the public asteroids
    /// endpoint never exposes private entity fields.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 25)]
    public Property PublicAsteroidsEndpoint_NeverExposesPrivateFields()
    {
        return Prop.ForAll(
            AsteroidScenarioArbitrary(),
            scenario =>
            {
                RunAsteroidVisibilityCheck(scenario).GetAwaiter().GetResult();
            });
    }

    // ===== Generators =====

    private static Arbitrary<ColonyScenario> ColonyScenarioArbitrary()
    {
        var gen = from colonyCount in Gen.Choose(1, 4)
                  from colonies in Gen.ListOf(colonyCount, ColonyGen())
                  select new ColonyScenario
                  {
                      SystemId = 9000 + Math.Abs(Guid.NewGuid().GetHashCode() % 100),
                      SystemName = "VisTestSystem-" + Guid.NewGuid().ToString()[..6],
                      CharacterUUID = Guid.NewGuid().ToString(),
                      Colonies = colonies.ToList(),
                  };
        return Arb.From(gen);
    }

    private static Gen<ColonyData> ColonyGen()
    {
        return from structureCount in Gen.Choose(0, 5)
               from nameLen in Gen.Choose(3, 12)
               from nameChars in Gen.ListOf(nameLen, Gen.Elements(
                   'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'K', 'L', 'M', 'N'))
               select new ColonyData
               {
                   UUID = Guid.NewGuid().ToString(),
                   ColonyName = "Col-" + new string(nameChars.ToArray()),
                   StructureCount = structureCount,
               };
    }

    private static Arbitrary<BlueprintScenario> BlueprintScenarioArbitrary()
    {
        var gen = from bpCount in Gen.Choose(1, 5)
                  from names in Gen.ListOf(bpCount, Gen.Elements(
                      "Laser Mk1", "Shield Mk2", "Engine Mk3", "Hull Mk4", "Sensor Mk5"))
                  select new BlueprintScenario
                  {
                      Blueprints = names.Select(n => new BlueprintData
                      {
                          UUID = Guid.NewGuid().ToString(),
                          Name = n + "-" + Guid.NewGuid().ToString()[..4],
                      }).ToList(),
                  };
        return Arb.From(gen);
    }

    private static Arbitrary<SystemScenario> SystemScenarioArbitrary()
    {
        var gen = from sysCount in Gen.Choose(1, 5)
                  from xs in Gen.ListOf(sysCount, Gen.Choose(-100, 100))
                  from ys in Gen.ListOf(sysCount, Gen.Choose(-100, 100))
                  select new SystemScenario
                  {
                      Systems = xs.Zip(ys, (x, y) => new SystemData
                      {
                          Id = 8000 + Math.Abs(Guid.NewGuid().GetHashCode() % 1000),
                          Name = "Sys-" + Guid.NewGuid().ToString()[..6],
                          X = x,
                          Y = y,
                      }).ToList(),
                  };
        return Arb.From(gen);
    }

    private static Arbitrary<AsteroidScenario> AsteroidScenarioArbitrary()
    {
        var gen = from asteroidCount in Gen.Choose(1, 4)
                  from names in Gen.ListOf(asteroidCount, Gen.Elements(
                      "Rock-A", "Rock-B", "Rock-C", "Rock-D", "Rock-E"))
                  select new AsteroidScenario
                  {
                      SystemId = 7000 + Math.Abs(Guid.NewGuid().GetHashCode() % 100),
                      SystemName = "AstSys-" + Guid.NewGuid().ToString()[..6],
                      CharacterUUID = Guid.NewGuid().ToString(),
                      Asteroids = names.Select(n => new AsteroidData
                      {
                          UUID = Guid.NewGuid().ToString(),
                          Name = n + "-" + Guid.NewGuid().ToString()[..4],
                      }).ToList(),
                  };
        return Arb.From(gen);
    }

    // ===== Test Runners =====

    private async Task RunColonyVisibilityCheck(ColonyScenario scenario)
    {
        var storage = SharedTestServer.Factory.Services.GetRequiredService<IStorageBackend>();

        // Seed system
        await storage.UpsertStarSystemsAsync(new List<StarSystem>
        {
            new StarSystem { Id = scenario.SystemId, Name = scenario.SystemName },
        });

        // Seed character
        await storage.UpsertCharacterAsync(new ServerCharacter
        {
            UUID = scenario.CharacterUUID,
            Name = "VisChar-" + scenario.CharacterUUID[..8],
        });

        // Seed colonies with structures
        foreach (var colData in scenario.Colonies)
        {
            var colony = new Colony
            {
                UUID = colData.UUID,
                ColonyName = colData.ColonyName,
                PlanetName = scenario.SystemName + " Planet 1",
                SystemName = scenario.SystemName,
            };

            // Add structures to make the colony non-trivial
            for (int i = 0; i < colData.StructureCount; i++)
            {
                colony.Structures.Add(new ColonyStructure
                {
                    UUID = Guid.NewGuid().ToString(),
                });
            }

            await storage.UpsertColonyAsync(scenario.CharacterUUID, colony);
        }

        try
        {
            using var client = SharedTestServer.Factory.CreateClient();
            var response = await client.GetAsync(
                $"/api/v1/public/systems/{scenario.SystemId}/colonies");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            AssertNoPrivateFields(json, "/api/v1/public/systems/{id}/colonies");
        }
        finally
        {
            foreach (var colData in scenario.Colonies)
            {
                await storage.DeleteColonyAsync(scenario.CharacterUUID, colData.UUID);
            }

            await storage.DeleteCharacterAsync(scenario.CharacterUUID);
        }
    }

    private async Task RunBlueprintVisibilityCheck(BlueprintScenario scenario)
    {
        var storage = SharedTestServer.Factory.Services.GetRequiredService<IStorageBackend>();

        // Seed global blueprints
        foreach (var bp in scenario.Blueprints)
        {
            await storage.UpsertBlueprintAsync(string.Empty, new Blueprint
            {
                UUID = bp.UUID,
                Name = bp.Name,
            });
        }

        try
        {
            using var client = SharedTestServer.Factory.CreateClient();
            var response = await client.GetAsync(
                "/api/v1/public/blueprints?pageSize=100");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            // Check the items array within the paginated response
            if (json.TryGetProperty("items", out var items))
            {
                AssertNoPrivateFields(items, "/api/v1/public/blueprints");
            }
        }
        finally
        {
            foreach (var bp in scenario.Blueprints)
            {
                await storage.DeleteBlueprintAsync(string.Empty, bp.UUID);
            }
        }
    }

    private async Task RunSystemVisibilityCheck(SystemScenario scenario)
    {
        var storage = SharedTestServer.Factory.Services.GetRequiredService<IStorageBackend>();

        var systems = scenario.Systems.Select(s => new StarSystem
        {
            Id = s.Id,
            Name = s.Name,
            X = s.X,
            Y = s.Y,
        }).ToList();

        await storage.UpsertStarSystemsAsync(systems);

        try
        {
            using var client = SharedTestServer.Factory.CreateClient();
            var response = await client.GetAsync("/api/v1/public/systems");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            AssertNoPrivateFields(json, "/api/v1/public/systems");
        }
        finally
        {
            // Systems are global; upsert with empty list would be destructive.
            // Leave them — they don't interfere with other tests.
        }
    }

    private async Task RunAsteroidVisibilityCheck(AsteroidScenario scenario)
    {
        var storage = SharedTestServer.Factory.Services.GetRequiredService<IStorageBackend>();

        // Seed system
        await storage.UpsertStarSystemsAsync(new List<StarSystem>
        {
            new StarSystem { Id = scenario.SystemId, Name = scenario.SystemName },
        });

        // Seed character
        await storage.UpsertCharacterAsync(new ServerCharacter
        {
            UUID = scenario.CharacterUUID,
            Name = "AstChar-" + scenario.CharacterUUID[..8],
        });

        // Seed asteroids
        foreach (var ast in scenario.Asteroids)
        {
            await storage.UpsertAsteroidAsync(scenario.CharacterUUID, new Asteroid
            {
                UUID = ast.UUID,
                Name = ast.Name,
                SystemName = scenario.SystemName,
            });
        }

        try
        {
            using var client = SharedTestServer.Factory.CreateClient();
            var response = await client.GetAsync(
                $"/api/v1/public/systems/{scenario.SystemId}/asteroids");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            AssertNoPrivateFields(json, "/api/v1/public/systems/{id}/asteroids");
        }
        finally
        {
            foreach (var ast in scenario.Asteroids)
            {
                await storage.DeleteAsteroidAsync(scenario.CharacterUUID, ast.UUID);
            }

            await storage.DeleteCharacterAsync(scenario.CharacterUUID);
        }
    }

    // ===== Assertion Helpers =====

    private static void AssertNoPrivateFields(JsonElement element, string endpoint)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    Assert.That(
                        PrivateFieldNames.Contains(property.Name),
                        Is.False,
                        $"Public endpoint {endpoint} exposed private field " +
                        $"'{property.Name}' in response");

                    // Recurse into nested objects/arrays
                    AssertNoPrivateFields(property.Value, endpoint);
                }

                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    AssertNoPrivateFields(item, endpoint);
                }

                break;
        }
    }

    // ===== Scenario Types =====

    private sealed class ColonyScenario
    {
        public int SystemId { get; set; }

        public string SystemName { get; set; } = string.Empty;

        public string CharacterUUID { get; set; } = string.Empty;

        public List<ColonyData> Colonies { get; set; } = new();

        public override string ToString()
        {
            return $"ColonyScenario(sys={SystemName[..10]}, " +
                $"colonies={Colonies.Count}, " +
                $"structures=[{string.Join(",", Colonies.Select(c => c.StructureCount))}])";
        }
    }

    private sealed class ColonyData
    {
        public string UUID { get; set; } = string.Empty;

        public string ColonyName { get; set; } = string.Empty;

        public int StructureCount { get; set; }
    }

    private sealed class BlueprintScenario
    {
        public List<BlueprintData> Blueprints { get; set; } = new();

        public override string ToString()
        {
            return $"BlueprintScenario(count={Blueprints.Count})";
        }
    }

    private sealed class BlueprintData
    {
        public string UUID { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    private sealed class SystemScenario
    {
        public List<SystemData> Systems { get; set; } = new();

        public override string ToString()
        {
            return $"SystemScenario(count={Systems.Count})";
        }
    }

    private sealed class SystemData
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int X { get; set; }

        public int Y { get; set; }
    }

    private sealed class AsteroidScenario
    {
        public int SystemId { get; set; }

        public string SystemName { get; set; } = string.Empty;

        public string CharacterUUID { get; set; } = string.Empty;

        public List<AsteroidData> Asteroids { get; set; } = new();

        public override string ToString()
        {
            return $"AsteroidScenario(sys={SystemName[..10]}, " +
                $"asteroids={Asteroids.Count})";
        }
    }

    private sealed class AsteroidData
    {
        public string UUID { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }
}
