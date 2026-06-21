// -----------------------------------------------------------------------
// <copyright file="BaselineDecompositionServiceTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Services;
using OE2EmpireTracker.Server.Storage;
using OE2EmpireTracker.Server.Tests.Endpoints;

namespace OE2EmpireTracker.Server.Tests.Services;

/// <summary>
/// Unit tests for <see cref="BaselineDecompositionService"/> reference data decomposition.
/// Validates: Requirements 1.1–1.8, 1.10, 1.11.
/// </summary>
[TestFixture]
public class BaselineDecompositionServiceTests
{
    private BaselineDecompositionService _service = null!;
    private RecordingStorageBackend _storage = null!;

    /// <summary>
    /// Creates the service and recording storage backend before each test.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _service = new BaselineDecompositionService(
            NullLogger<BaselineDecompositionService>.Instance);
        _storage = new RecordingStorageBackend();
    }

    /// <summary>
    /// Valid payload with all 7 reference data sections stores all sections.
    /// Validates: Requirements 1.1–1.8.
    /// </summary>
    [Test]
    public async Task DecomposeAsync_ValidPayloadWithAllSections_StoresAll()
    {
        var payload = BuildPayload(
            "GameConstants",
            "ShipClass",
            "BlueprintType",
            "TechLevel",
            "Commodity",
            "RefiningRecipe",
            "ResearchTime");

        await _service.DecomposeAsync(payload, _storage);

        Assert.That(_storage.GlobalDataCalls.Count, Is.EqualTo(7));
        Assert.That(
            _storage.GlobalDataCalls.Select(c => c.Key).ToList(),
            Is.EquivalentTo(new[]
            {
                "GameConstants",
                "ShipClass",
                "BlueprintType",
                "TechLevel",
                "Commodity",
                "RefiningRecipe",
                "ResearchTime",
            }));
    }

    /// <summary>
    /// Payload with only some sections stores only those present.
    /// Validates: Requirement 1.10.
    /// </summary>
    [Test]
    public async Task DecomposeAsync_PayloadMissingSomeSections_OnlyPresentSectionsStored()
    {
        var payload = BuildPayload("GameConstants", "ShipClass");

        await _service.DecomposeAsync(payload, _storage);

        Assert.That(_storage.GlobalDataCalls.Count, Is.EqualTo(2));
        Assert.That(
            _storage.GlobalDataCalls.Select(c => c.Key).ToList(),
            Is.EquivalentTo(new[] { "GameConstants", "ShipClass" }));
    }

    /// <summary>
    /// Invalid JSON throws <see cref="JsonException"/>.
    /// Validates: Requirement 1.11.
    /// </summary>
    [Test]
    public void DecomposeAsync_InvalidJson_ThrowsJsonException()
    {
        var invalidJson = "{ not valid json at all !!!";

        Assert.That(
            () => _service.DecomposeAsync(invalidJson, _storage),
            Throws.InstanceOf<JsonException>());

        Assert.That(_storage.GlobalDataCalls, Is.Empty);
    }

    /// <summary>
    /// Valid JSON with empty object (no known sections) throws
    /// <see cref="InvalidOperationException"/>.
    /// Validates: Requirement 1.10 (no sections processed path).
    /// </summary>
    [Test]
    public void DecomposeAsync_EmptyObject_ThrowsInvalidOperationException()
    {
        var emptyObject = "{}";

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DecomposeAsync(emptyObject, _storage));

        Assert.That(ex!.Message, Does.Contain("No valid sections"));
        Assert.That(_storage.GlobalDataCalls, Is.Empty);
    }

    // ===== Blueprint Decomposition Tests (Task 14.2) =====
    // Validates: Requirements 1.9, 2.1, 2.3, 2.4.

    /// <summary>
    /// Payload with Blueprint array of 3 items calls UpsertBlueprintAsync 3 times
    /// with characterUUID="" and the correct blueprint objects.
    /// Validates: Requirements 1.9, 2.1.
    /// </summary>
    [Test]
    public async Task DecomposeAsync_BlueprintArrayPresent_EachBlueprintUpsertedWithEmptyOwner()
    {
        var payload = BuildPayloadWithBlueprints(
            new Blueprint { UUID = "bp-001", Name = "Laser Mk1", BluePrintType = "Weapon" },
            new Blueprint { UUID = "bp-002", Name = "Shield Mk2", BluePrintType = "Defense" },
            new Blueprint { UUID = "bp-003", Name = "Engine Mk3", BluePrintType = "Propulsion" });

        await _service.DecomposeAsync(payload, _storage);

        Assert.That(_storage.BlueprintCalls.Count, Is.EqualTo(3));
        Assert.That(
            _storage.BlueprintCalls.All(c => c.CharUUID == string.Empty),
            Is.True,
            "All blueprint upserts must use characterUUID=\"\"");
        Assert.That(
            _storage.BlueprintCalls.Select(c => c.Blueprint.UUID).ToList(),
            Is.EquivalentTo(new[] { "bp-001", "bp-002", "bp-003" }));
        Assert.That(
            _storage.BlueprintCalls.Select(c => c.Blueprint.Name).ToList(),
            Is.EquivalentTo(new[] { "Laser Mk1", "Shield Mk2", "Engine Mk3" }));
    }

    /// <summary>
    /// Payload with empty Blueprint array produces no UpsertBlueprintAsync calls
    /// and does not throw. The Blueprint section still counts as processed.
    /// Validates: Requirements 1.9, 2.1.
    /// </summary>
    [Test]
    public async Task DecomposeAsync_EmptyBlueprintArray_NoUpsertsNoError()
    {
        var payload = BuildPayloadWithBlueprints();

        await _service.DecomposeAsync(payload, _storage);

        Assert.That(_storage.BlueprintCalls, Is.Empty);
        Assert.That(_storage.GlobalDataCalls, Is.Empty);
    }

    /// <summary>
    /// Payload with duplicate UUID blueprints upserts both (no dedup at service level).
    /// Validates: Requirements 2.3, 2.4.
    /// </summary>
    [Test]
    public async Task DecomposeAsync_DuplicateUUID_BothUpserted()
    {
        var duplicateUuid = "bp-dup-001";
        var payload = BuildPayloadWithBlueprints(
            new Blueprint { UUID = duplicateUuid, Name = "Original", BluePrintType = "Weapon" },
            new Blueprint { UUID = duplicateUuid, Name = "Updated", BluePrintType = "Weapon" });

        await _service.DecomposeAsync(payload, _storage);

        Assert.That(_storage.BlueprintCalls.Count, Is.EqualTo(2));
        Assert.That(
            _storage.BlueprintCalls[0].Blueprint.UUID,
            Is.EqualTo(duplicateUuid));
        Assert.That(
            _storage.BlueprintCalls[1].Blueprint.UUID,
            Is.EqualTo(duplicateUuid));
        Assert.That(
            _storage.BlueprintCalls[0].Blueprint.Name,
            Is.EqualTo("Original"));
        Assert.That(
            _storage.BlueprintCalls[1].Blueprint.Name,
            Is.EqualTo("Updated"));
    }

    // ===== Helper Methods =====

    /// <summary>
    /// Builds a JSON payload containing the specified section keys.
    /// Each section is a minimal valid JSON value.
    /// </summary>
    private static string BuildPayload(params string[] sectionKeys)
    {
        var sections = new Dictionary<string, object>();
        foreach (var key in sectionKeys)
        {
            if (key == "GameConstants")
            {
                sections[key] = new { MaxLevel = 10, BaseRate = 1.5 };
            }
            else
            {
                sections[key] = new[]
                {
                    new { Id = 1, Name = key + "_Item1" },
                };
            }
        }

        return JsonSerializer.Serialize(sections);
    }

    /// <summary>
    /// Builds a JSON payload containing only a Blueprint array.
    /// Uses Newtonsoft serialization to match the service's deserialization.
    /// </summary>
    private static string BuildPayloadWithBlueprints(params Blueprint[] blueprints)
    {
        var payload = new Dictionary<string, object>
        {
            ["Blueprint"] = blueprints,
        };

        return Newtonsoft.Json.JsonConvert.SerializeObject(payload);
    }
}
