// -----------------------------------------------------------------------
// <copyright file="BaselineDecompositionPropertyTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FsCheck;
using FsCheck.NUnit;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Common.Storage;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Services;
using OE2EmpireTracker.Server.Tests.Endpoints;

namespace OE2EmpireTracker.Server.Tests.Services;

/// <summary>
/// Property-based tests for <see cref="BaselineDecompositionService"/>.
/// Validates decomposition round-trip, idempotency, and blueprint additivity.
/// </summary>
[TestFixture]
public class BaselineDecompositionPropertyTests
{
    private static readonly string[] SectionKeys =
    {
        "GameConstants",
        "ShipClass",
        "BlueprintType",
        "TechLevel",
        "Commodity",
        "RefiningRecipe",
        "ResearchTime",
    };

    private string _dataPath = null!;
    private JsonMultiFileBackend _backend = null!;
    private BaselineDecompositionService _service = null!;

    /// <summary>
    /// Creates a temp directory and initializes the storage backend before each test.
    /// </summary>
    [SetUp]
    public async Task Setup()
    {
        _dataPath = Path.Combine(
            Path.GetTempPath(),
            "oe2-prop-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_dataPath);

        _backend = new JsonMultiFileBackend(_dataPath);
        await _backend.InitializeAsync();

        _service = new BaselineDecompositionService(
            NullLogger<BaselineDecompositionService>.Instance);
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
    /// Property 1: Decomposition Round-Trip.
    /// For any valid payload with section S, decompose then read(S) produces
    /// equivalent data (non-null, valid JSON).
    /// Validates: Requirements 1.2–1.8, 11.4.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property DecompositionRoundTrip_ReadBackProducesEquivalentData()
    {
        var gen = from sectionSeed in Arb.Generate<PositiveInt>()
                  select sectionSeed.Get;

        return Prop.ForAll(
            Arb.From(gen),
            seed =>
            {
                // Reset storage for each check
                TearDown();
                Setup().GetAwaiter().GetResult();

                var sectionMask = seed;
                var selectedSections = SectionKeys
                    .Where((_, i) => (sectionMask & (1 << i)) != 0)
                    .ToList();

                if (selectedSections.Count == 0)
                {
                    selectedSections.Add(SectionKeys[0]);
                }

                var payload = BuildRandomPayload(selectedSections, 0, seed);

                // Decompose into the real file-backed storage
                _service.DecomposeAsync(payload, _backend).GetAwaiter().GetResult();

                // Read back each section and verify non-null valid JSON
                foreach (var key in selectedSections)
                {
                    var stored = _backend.GetGlobalDataAsync(key).GetAwaiter().GetResult();

                    Assert.That(
                        stored,
                        Is.Not.Null,
                        $"Section '{key}' should be stored after decomposition");

                    Assert.DoesNotThrow(
                        () => Newtonsoft.Json.Linq.JToken.Parse(stored!),
                        $"Section '{key}' should contain valid JSON after decomposition");
                }
            });
    }

    /// <summary>
    /// Property 2: Decomposition Idempotency.
    /// Decomposing the same payload twice produces identical stored state.
    /// Validates: Requirements 1.2–1.8.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property Idempotency_DecomposingSamePayloadTwice_ProducesIdenticalState()
    {
        var gen = from sectionSeed in Arb.Generate<PositiveInt>()
                  from blueprintCount in Arb.Generate<PositiveInt>()
                  select (SectionSeed: sectionSeed.Get, BpCount: blueprintCount.Get % 6);

        return Prop.ForAll(
            Arb.From(gen),
            tuple =>
            {
                var sectionMask = tuple.SectionSeed;
                var bpCount = tuple.BpCount;
                var selectedSections = SectionKeys
                    .Where((_, i) => (sectionMask & (1 << i)) != 0)
                    .ToList();

                if (selectedSections.Count == 0 && bpCount == 0)
                {
                    selectedSections.Add(SectionKeys[0]);
                }

                var payload = BuildRandomPayload(selectedSections, bpCount, tuple.SectionSeed);

                // Single decomposition for reference
                var referenceStorage = new IdempotencyStorageBackend();
                _service.DecomposeAsync(payload, referenceStorage).GetAwaiter().GetResult();

                // Double decomposition into same storage
                var doubleStorage = new IdempotencyStorageBackend();
                _service.DecomposeAsync(payload, doubleStorage).GetAwaiter().GetResult();
                _service.DecomposeAsync(payload, doubleStorage).GetAwaiter().GetResult();

                // Assert: double-decomposed storage matches single-decomposed
                Assert.That(
                    doubleStorage.GlobalData.Keys,
                    Is.EquivalentTo(referenceStorage.GlobalData.Keys),
                    "Global data keys must match after double decompose");

                foreach (var key in referenceStorage.GlobalData.Keys)
                {
                    Assert.That(
                        doubleStorage.GlobalData[key],
                        Is.EqualTo(referenceStorage.GlobalData[key]),
                        $"Global data for '{key}' must be identical after double decompose");
                }

                Assert.That(
                    doubleStorage.Blueprints.Count,
                    Is.EqualTo(referenceStorage.Blueprints.Count),
                    "Blueprint count must match after double decompose");

                foreach (var kvp in referenceStorage.Blueprints)
                {
                    Assert.That(
                        doubleStorage.Blueprints.ContainsKey(kvp.Key),
                        Is.True,
                        $"Blueprint '{kvp.Key}' must exist after double decompose");
                    Assert.That(
                        doubleStorage.Blueprints[kvp.Key].Name,
                        Is.EqualTo(kvp.Value.Name),
                        $"Blueprint '{kvp.Key}' Name must match after double decompose");
                }
            });
    }


    /// <summary>
    /// Property 3: Blueprint Additivity.
    /// Sequential uploads with overlapping blueprints produce the union of all
    /// blueprint UUIDs. Blueprints are never deleted by decomposition.
    /// Validates: Requirements 11.2.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 50)]
    public Property BlueprintAdditivity_SequentialUploads_ProduceUnion()
    {
        var genUuid = Gen.Elements(
            "bp-001", "bp-002", "bp-003", "bp-004", "bp-005",
            "bp-006", "bp-007", "bp-008", "bp-009", "bp-010");

        var genBlueprintSet =
            from count in Gen.Choose(1, 6)
            from uuids in Gen.ListOf(count, genUuid)
            select uuids.Distinct().ToList();

        var genTwoSets =
            from set1 in genBlueprintSet
            from set2 in genBlueprintSet
            select (Set1: set1, Set2: set2);

        return Prop.ForAll(
            Arb.From(genTwoSets),
            tuple =>
            {
                var (set1Uuids, set2Uuids) = tuple;

                // Reset storage for each property check
                TearDown();
                Setup().GetAwaiter().GetResult();

                // Build payload 1 with blueprints from set1
                var blueprints1 = set1Uuids
                    .Select(uuid => new Blueprint
                    {
                        UUID = uuid,
                        Name = $"BP-{uuid}-v1",
                        BluePrintType = "Weapon",
                    })
                    .ToArray();
                var payload1 = BuildPayloadWithBlueprints(blueprints1);

                // Build payload 2 with blueprints from set2
                var blueprints2 = set2Uuids
                    .Select(uuid => new Blueprint
                    {
                        UUID = uuid,
                        Name = $"BP-{uuid}-v2",
                        BluePrintType = "Defense",
                    })
                    .ToArray();
                var payload2 = BuildPayloadWithBlueprints(blueprints2);

                // Decompose both payloads sequentially
                _service.DecomposeAsync(payload1, _backend)
                    .GetAwaiter().GetResult();
                _service.DecomposeAsync(payload2, _backend)
                    .GetAwaiter().GetResult();

                // Read all global blueprints
                var stored = _backend.GetAllBlueprintsAsync(string.Empty)
                    .GetAwaiter().GetResult();
                var storedUuids = stored.Select(b => b.UUID).ToHashSet();

                // The union of both sets must be present
                var expectedUnion = set1Uuids
                    .Union(set2Uuids)
                    .ToHashSet();

                return expectedUnion.IsSubsetOf(storedUuids)
                    .Label($"Expected union {{{string.Join(", ", expectedUnion)}}} " +
                           $"to be subset of stored {{{string.Join(", ", storedUuids)}}}");
            });
    }


    // =================================================================
    // Helper Methods
    // =================================================================

    /// <summary>
    /// Builds a random valid JSON payload with the specified sections and blueprints.
    /// </summary>
    private static string BuildRandomPayload(
        List<string> sections,
        int blueprintCount,
        int seed)
    {
        var payload = new Dictionary<string, object>();

        foreach (var section in sections)
        {
            if (section == "GameConstants")
            {
                payload[section] = new { MaxLevel = seed % 50, BaseRate = 1.5 + (seed % 10) };
            }
            else
            {
                var items = Enumerable.Range(0, (seed % 5) + 1)
                    .Select(i => new { Id = i + 1, Name = $"{section}_Item{i}_{seed}" })
                    .ToArray();
                payload[section] = items;
            }
        }

        if (blueprintCount > 0)
        {
            var blueprints = Enumerable.Range(0, blueprintCount)
                .Select(i => new Blueprint
                {
                    UUID = $"bp-{seed}-{i:D3}",
                    Name = $"Blueprint_{seed}_{i}",
                    BluePrintType = "Weapon",
                    TechLevel = "TL1",
                    Evolution = i % 3,
                })
                .ToArray();
            payload["Blueprint"] = blueprints;
        }

        return JsonConvert.SerializeObject(payload);
    }

    /// <summary>
    /// Builds a JSON payload containing only a Blueprint array.
    /// </summary>
    private static string BuildPayloadWithBlueprints(params Blueprint[] blueprints)
    {
        var payload = new Dictionary<string, object>
        {
            ["Blueprint"] = blueprints,
        };

        return JsonConvert.SerializeObject(payload);
    }
}

/// <summary>
/// In-memory storage backend for idempotency testing.
/// Implements replace semantics for global data and upsert-by-UUID for blueprints.
/// Overrides base methods to capture data in memory.
/// </summary>
internal class IdempotencyStorageBackend : StubStorageBackend
{
    /// <summary>
    /// Gets the stored global data keyed by data type.
    /// </summary>
    public Dictionary<string, string> GlobalData { get; } = new();

    /// <summary>
    /// Gets the stored blueprints keyed by UUID.
    /// </summary>
    public Dictionary<string, Blueprint> Blueprints { get; } = new();

    /// <inheritdoc/>
    public override Task UpsertGlobalDataAsync(string dataType, string json)
    {
        GlobalData[dataType] = json;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task UpsertBlueprintAsync(string characterUUID, Blueprint entity)
    {
        Blueprints[entity.UUID] = entity;
        return Task.CompletedTask;
    }
}
