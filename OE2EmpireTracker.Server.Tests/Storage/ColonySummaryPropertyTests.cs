// -----------------------------------------------------------------------
// <copyright file="ColonySummaryPropertyTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using FsCheck;
using FsCheck.NUnit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Server.Tests.Storage;

/// <summary>
/// Property 5: Colony Summary Projection.
/// For any colony returned by GetColonySummariesForSystemAsync, the result
/// contains exactly {ColonyName, Size, PlanetName} and nothing else.
/// When serialized to JSON, each object has exactly 3 properties.
/// **Validates: Requirements 7.2, 7.3**
/// </summary>
[TestFixture]
public class ColonySummaryPropertyTests
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
            "oe2-colsummary-test-" + Guid.NewGuid().ToString("N")[..8]);
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
    /// Property: For any set of colonies stored in a system, the colony summaries
    /// returned by GetColonySummariesForSystemAsync contain exactly three JSON
    /// properties (colonyName, size, planetName) when serialized — no extra fields
    /// from the full Colony model leak through.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property ColonySummary_ContainsExactlyThreeFields()
    {
        return Prop.ForAll(
            ColonyScenarioArbitrary(),
            scenario =>
            {
                RunProjectionTest(scenario).GetAwaiter().GetResult();
            });
    }

    private static Arbitrary<ColonyScenario> ColonyScenarioArbitrary()
    {
        var gen = from colonyCount in Gen.Choose(1, 5)
                  from colonies in Gen.ListOf(colonyCount, ColonyGen())
                  select new ColonyScenario
                  {
                      SystemName = "TestSystem",
                      SystemId = 1,
                      CharacterUUID = Guid.NewGuid().ToString(),
                      Colonies = colonies.ToList(),
                  };
        return Arb.From(gen);
    }

    private static Gen<TestColonyData> ColonyGen()
    {
        return from name in Gen.Elements(
                   "Alpha Base", "Beta Outpost", "Gamma Station",
                   "Delta Mine", "Epsilon Hub", "Zeta Colony")
               from planetName in Gen.Elements(
                   "Earth", "Mars", "Venus", "Jupiter", "Saturn", "Neptune")
               from structureCount in Gen.Choose(0, 10)
               from structures in Gen.ListOf(
                   structureCount,
                   Gen.Elements("Mining Rig", "Refinery", "Lab", "Factory"))
               select new TestColonyData
               {
                   ColonyName = name + "-" + Guid.NewGuid().ToString("N")[..4],
                   PlanetName = planetName,
                   StructureNames = structures.ToList(),
               };
    }

    private async Task RunProjectionTest(ColonyScenario scenario)
    {
        // Reset storage for each property check to avoid accumulation
        TearDown();
        await Setup();

        // Seed a star system so the backend can resolve systemId → systemName
        var systems = new List<StarSystem>
        {
            new StarSystem
            {
                Id = scenario.SystemId,
                Name = scenario.SystemName,
            },
        };
        await _backend.UpsertStarSystemsAsync(systems);

        // Seed colonies for the character in the target system
        foreach (var colonyData in scenario.Colonies)
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = scenario.CharacterUUID,
                ColonyName = colonyData.ColonyName,
                PlanetName = colonyData.PlanetName,
                SystemName = scenario.SystemName,
                Items = new ItemBag(),
                Structures = colonyData.StructureNames
                    .Select(s => new ColonyStructure { UUID = Guid.NewGuid().ToString() })
                    .ToList(),
                Commodities = new List<CommodityRequested>(),
                LastImportDateTime = SystemClock.UtcNow.ToString("o"),
            };
            await _backend.UpsertColonyAsync(scenario.CharacterUUID, colony);
        }

        // Retrieve colony summaries
        var summaries = await _backend.GetColonySummariesForSystemAsync(scenario.SystemId);

        // Verify count matches
        Assert.That(summaries.Count, Is.EqualTo(scenario.Colonies.Count),
            "Summary count should match seeded colony count");

        // Serialize each summary to JSON and verify exactly 3 properties
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        foreach (var summary in summaries)
        {
            var json = JsonSerializer.Serialize(summary, options);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var propertyNames = new HashSet<string>();
            foreach (var prop in root.EnumerateObject())
            {
                propertyNames.Add(prop.Name);
            }

            Assert.That(propertyNames.Count, Is.EqualTo(3),
                $"ColonySummary should have exactly 3 JSON properties, " +
                $"got: [{string.Join(", ", propertyNames)}]");

            Assert.That(propertyNames, Does.Contain("colonyName"),
                "ColonySummary must contain 'colonyName'");
            Assert.That(propertyNames, Does.Contain("size"),
                "ColonySummary must contain 'size'");
            Assert.That(propertyNames, Does.Contain("planetName"),
                "ColonySummary must contain 'planetName'");
        }
    }

    // ===== Scenario Types =====

    private sealed class ColonyScenario
    {
        public string SystemName { get; set; } = string.Empty;

        public int SystemId { get; set; }

        public string CharacterUUID { get; set; } = string.Empty;

        public List<TestColonyData> Colonies { get; set; } = new();

        public override string ToString()
        {
            return $"ColonyScenario(colonies={Colonies.Count})";
        }
    }

    private sealed class TestColonyData
    {
        public string ColonyName { get; set; } = string.Empty;

        public string PlanetName { get; set; } = string.Empty;

        public List<string> StructureNames { get; set; } = new();
    }
}
