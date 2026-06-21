// -----------------------------------------------------------------------
// <copyright file="PublicDataFilteringTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using FsCheck.NUnit;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Tests;

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Property 1: Public Data Filtering.
/// For any set of characters with sharing rules and entity data, the public data
/// endpoint for a given data type SHALL return exactly those entities owned by
/// characters who have at least one sharing rule with TargetType = Public and
/// DataType of either null or matching the requested type. No other entities
/// shall be included, and no matching entities shall be excluded.
/// **Validates: Requirements 1.1, 1.2, 1.3, 5.3**
/// Feature: sharing-visibility-system, Property 1: Public Data Filtering.
/// </summary>
[TestFixture]
public class PublicDataFilteringTests
{
    private HttpClient _ownerClient = null!;

    /// <summary>
    /// Sets up the test server factory and owner client.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        _ownerClient = SharedTestServer.Factory.CreateAuthenticatedClient();
    }

    /// <summary>
    /// Tears down the test server.
    /// </summary>
    [OneTimeTearDown]
    public void TearDown()
    {
        _ownerClient.Dispose();
    }

    /// <summary>
    /// Property: For any random set of characters with random sharing rules,
    /// the public blueprints endpoint returns exactly those blueprints owned by
    /// characters who have a Public sharing rule with DataType null or "Blueprints".
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property PublicBlueprints_ReturnsExactlyMatchingEntities()
    {
        return Prop.ForAll(
            ScenarioArbitrary(),
            scenario =>
            {
                RunFilteringScenario(scenario, "Blueprints", "/api/v1/public/blueprints")
                    .GetAwaiter().GetResult();
            });
    }

    /// <summary>
    /// Property: For any random set of characters with random sharing rules,
    /// the public surveys endpoint returns exactly those surveys owned by
    /// characters who have a Public sharing rule with DataType null or "Surveys".
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property PublicSurveys_ReturnsExactlyMatchingEntities()
    {
        return Prop.ForAll(
            ScenarioArbitrary(),
            scenario =>
            {
                RunFilteringScenario(scenario, "Surveys", "/api/v1/public/surveys")
                    .GetAwaiter().GetResult();
            });
    }

    /// <summary>
    /// Property: For any random set of characters with random sharing rules,
    /// the public colonies endpoint returns exactly those colonies owned by
    /// characters who have a Public sharing rule with DataType null or "Colonies".
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property PublicColonies_ReturnsExactlyMatchingEntities()
    {
        return Prop.ForAll(
            ScenarioArbitrary(),
            scenario =>
            {
                RunFilteringScenario(scenario, "Colonies", "/api/v1/public/colonies")
                    .GetAwaiter().GetResult();
            });
    }

    /// <summary>
    /// Property: Characters without any Public sharing rules have their data
    /// excluded from all public endpoints regardless of data type.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property CharactersWithoutPublicRules_AreExcluded()
    {
        return Prop.ForAll(
            ScenarioArbitrary(),
            scenario =>
            {
                RunExclusionScenario(scenario).GetAwaiter().GetResult();
            });
    }

    /// <summary>
    /// Property: Public rules with DataType=null include all data types.
    /// A character with a single Public rule where DataType is null should have
    /// their data appear in all three public endpoints.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property PublicRuleWithNullDataType_IncludesAllDataTypes()
    {
        return Prop.ForAll(
            NullDataTypeScenarioArbitrary(),
            scenario =>
            {
                RunNullDataTypeScenario(scenario).GetAwaiter().GetResult();
            });
    }

    /// <summary>
    /// Property: Public rules with a specific DataType only include that type.
    /// A character with a Public rule for "Blueprints" should NOT have their
    /// surveys or colonies appear in the respective public endpoints.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property PublicRuleWithSpecificDataType_OnlyIncludesThatType()
    {
        return Prop.ForAll(
            SpecificDataTypeScenarioArbitrary(),
            scenario =>
            {
                RunSpecificDataTypeScenario(scenario).GetAwaiter().GetResult();
            });
    }

    // ===== Static Generators =====

    private static Arbitrary<FilteringScenario> ScenarioArbitrary()
    {
        var gen = from charCount in Gen.Choose(1, 5)
                  from characters in Gen.ListOf(charCount, CharacterGen())
                  select new FilteringScenario { Characters = characters.ToList() };
        return Arb.From(gen);
    }

    private static Gen<TestCharacter> CharacterGen()
    {
        return from ruleCount in Gen.Choose(0, 4)
               from rules in Gen.ListOf(ruleCount, RuleGen())
               from bpCount in Gen.Choose(0, 3)
               from svCount in Gen.Choose(0, 3)
               from colCount in Gen.Choose(0, 3)
               let uuid = Guid.NewGuid().ToString()
               select new TestCharacter
               {
                   UUID = uuid,
                   Name = "Char-" + uuid[..8],
                   Rules = rules.ToList(),
                   BlueprintUUIDs = Enumerable.Range(0, bpCount)
                       .Select(_ => Guid.NewGuid().ToString()).ToList(),
                   SurveyUUIDs = Enumerable.Range(0, svCount)
                       .Select(_ => Guid.NewGuid().ToString()).ToList(),
                   ColonyUUIDs = Enumerable.Range(0, colCount)
                       .Select(_ => Guid.NewGuid().ToString()).ToList(),
               };
    }

    private static Gen<TestSharingRule> RuleGen()
    {
        return from targetType in Gen.Elements(
                   SharingTargetType.Faction,
                   SharingTargetType.Character,
                   SharingTargetType.Public)
               from dataType in Gen.Elements<string?>(
                   null, "Blueprints", "Surveys", "Colonies")
               select new TestSharingRule
               {
                   TargetType = targetType,
                   DataType = dataType,
               };
    }

    private static Arbitrary<NullDataTypeScenario> NullDataTypeScenarioArbitrary()
    {
        var gen = from bpCount in Gen.Choose(0, 3)
                  from svCount in Gen.Choose(0, 3)
                  from colCount in Gen.Choose(0, 3)
                  select new NullDataTypeScenario
                  {
                      CharacterUUID = Guid.NewGuid().ToString(),
                      BlueprintCount = bpCount,
                      SurveyCount = svCount,
                      ColonyCount = colCount,
                  };
        return Arb.From(gen);
    }

    private static Arbitrary<SpecificDataTypeScenario> SpecificDataTypeScenarioArbitrary()
    {
        var gen = from dataType in Gen.Elements("Blueprints", "Surveys", "Colonies")
                  from bpCount in Gen.Choose(1, 3)
                  from svCount in Gen.Choose(1, 3)
                  from colCount in Gen.Choose(1, 3)
                  select new SpecificDataTypeScenario
                  {
                      CharacterUUID = Guid.NewGuid().ToString(),
                      SharedDataType = dataType,
                      BlueprintCount = bpCount,
                      SurveyCount = svCount,
                      ColonyCount = colCount,
                  };
        return Arb.From(gen);
    }

    private static async Task SeedCharacterData(
        IStorageBackend storage,
        TestCharacter character)
    {
        // Create the character
        await storage.UpsertCharacterAsync(new ServerCharacter
        {
            UUID = character.UUID,
            Name = character.Name,
        });

        // Create sharing rules
        var rules = character.Rules.Select((r, i) => new SharingRule
        {
            Id = $"{character.UUID}-rule-{i}",
            OwnerCharacterUUID = character.UUID,
            TargetType = r.TargetType,
            TargetUUID = r.TargetType == SharingTargetType.Public
                ? "public" : Guid.NewGuid().ToString(),
            DataType = r.DataType,
        }).ToList();
        await storage.UpsertSharingRulesAsync(character.UUID, rules);

        // Create blueprints
        foreach (var uuid in character.BlueprintUUIDs)
        {
            var bp = new Blueprint() { UUID = uuid, Name = "BP-" + uuid[..8] };
            await storage.UpsertBlueprintAsync(character.UUID, bp);
        }

        // Create surveys
        foreach (var uuid in character.SurveyUUIDs)
        {
            var sv = new Survey() { UUID = uuid, Name = "SV-" + uuid[..8] };
            await storage.UpsertSurveyAsync(character.UUID, sv);
        }

        // Create colonies
        foreach (var uuid in character.ColonyUUIDs)
        {
            var col = new Colony { UUID = uuid, ColonyName = "COL-" + uuid[..8] };
            await storage.UpsertColonyAsync(character.UUID, col);
        }
    }

    private static async Task CleanupCharacterData(
        IStorageBackend storage,
        TestCharacter character)
    {
        foreach (var uuid in character.BlueprintUUIDs)
        {
            await storage.DeleteBlueprintAsync(character.UUID, uuid);
        }

        foreach (var uuid in character.SurveyUUIDs)
        {
            await storage.DeleteSurveyAsync(character.UUID, uuid);
        }

        foreach (var uuid in character.ColonyUUIDs)
        {
            await storage.DeleteColonyAsync(character.UUID, uuid);
        }

        await storage.UpsertSharingRulesAsync(
            character.UUID, Array.Empty<SharingRule>());
        await storage.DeleteCharacterAsync(character.UUID);
    }

    private static HashSet<string> ExtractEntityUUIDs(JsonElement itemsArray)
    {
        var uuids = new HashSet<string>();
        foreach (var item in itemsArray.EnumerateArray())
        {
            // Server uses camelCase serialization (uuid not UUID)
            if (item.TryGetProperty("uuid", out var uuidProp))
            {
                var uuid = uuidProp.GetString();
                if (uuid != null)
                {
                    uuids.Add(uuid);
                }
            }
        }

        return uuids;
    }

    // ===== Private Instance Test Helpers =====

    private async Task RunFilteringScenario(
        FilteringScenario scenario,
        string dataType,
        string endpoint)
    {
        var storage = SharedTestServer.Factory.Services.GetRequiredService<IStorageBackend>();

        // Seed characters, rules, and entities
        foreach (var character in scenario.Characters)
        {
            await SeedCharacterData(storage, character);
        }

        // Request with large pageSize to get all results
        using var client = SharedTestServer.Factory.CreateClient();
        var response = await client.GetAsync($"{endpoint}?pageSize=100");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var returnedItems = json.GetProperty("items");
        var returnedUUIDs = ExtractEntityUUIDs(returnedItems);

        // Compute expected: entities from characters with matching Public rules
        var expectedUUIDs = new HashSet<string>();
        foreach (var character in scenario.Characters)
        {
            var hasMatchingPublicRule = character.Rules.Any(r =>
                r.TargetType == SharingTargetType.Public &&
                (r.DataType == null || r.DataType == dataType));

            if (hasMatchingPublicRule)
            {
                var entityUUIDs = dataType switch
                {
                    "Blueprints" => character.BlueprintUUIDs,
                    "Surveys" => character.SurveyUUIDs,
                    "Colonies" => character.ColonyUUIDs,
                    _ => new List<string>(),
                };
                foreach (var uuid in entityUUIDs)
                {
                    expectedUUIDs.Add(uuid);
                }
            }
        }

        // Verify all expected UUIDs are present
        foreach (var expectedUUID in expectedUUIDs)
        {
            Assert.That(
                returnedUUIDs,
                Does.Contain(expectedUUID),
                $"Expected entity {expectedUUID[..8]} missing from {endpoint}");
        }

        // Verify no entities from characters WITHOUT matching Public rules
        foreach (var character in scenario.Characters)
        {
            var hasMatchingPublicRule = character.Rules.Any(r =>
                r.TargetType == SharingTargetType.Public &&
                (r.DataType == null || r.DataType == dataType));

            if (!hasMatchingPublicRule)
            {
                var entityUUIDs = dataType switch
                {
                    "Blueprints" => character.BlueprintUUIDs,
                    "Surveys" => character.SurveyUUIDs,
                    "Colonies" => character.ColonyUUIDs,
                    _ => new List<string>(),
                };
                foreach (var uuid in entityUUIDs)
                {
                    Assert.That(
                        returnedUUIDs,
                        Does.Not.Contain(uuid),
                        $"Entity {uuid[..8]} from non-public char " +
                        $"{character.UUID[..8]} should NOT appear in {endpoint}");
                }
            }
        }

        // Cleanup seeded data
        foreach (var character in scenario.Characters)
        {
            await CleanupCharacterData(storage, character);
        }
    }

    private async Task RunExclusionScenario(FilteringScenario scenario)
    {
        var storage = SharedTestServer.Factory.Services.GetRequiredService<IStorageBackend>();

        foreach (var character in scenario.Characters)
        {
            await SeedCharacterData(storage, character);
        }

        using var client = SharedTestServer.Factory.CreateClient();
        var dataTypes = new[] { "Blueprints", "Surveys", "Colonies" };
        var endpoints = new[]
        {
            "/api/v1/public/blueprints",
            "/api/v1/public/surveys",
            "/api/v1/public/colonies",
        };

        for (int i = 0; i < dataTypes.Length; i++)
        {
            var response = await client.GetAsync($"{endpoints[i]}?pageSize=100");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var returnedUUIDs = ExtractEntityUUIDs(json.GetProperty("items"));

            foreach (var character in scenario.Characters)
            {
                var hasAnyPublicRule = character.Rules.Any(r =>
                    r.TargetType == SharingTargetType.Public);

                if (!hasAnyPublicRule)
                {
                    var entityUUIDs = dataTypes[i] switch
                    {
                        "Blueprints" => character.BlueprintUUIDs,
                        "Surveys" => character.SurveyUUIDs,
                        "Colonies" => character.ColonyUUIDs,
                        _ => new List<string>(),
                    };
                    foreach (var uuid in entityUUIDs)
                    {
                        Assert.That(
                            returnedUUIDs,
                            Does.Not.Contain(uuid),
                            $"Entity {uuid[..8]} from char " +
                            $"{character.UUID[..8]} (no Public rules) " +
                            $"should NOT appear in {endpoints[i]}");
                    }
                }
            }
        }

        foreach (var character in scenario.Characters)
        {
            await CleanupCharacterData(storage, character);
        }
    }

    private async Task RunNullDataTypeScenario(NullDataTypeScenario scenario)
    {
        var storage = SharedTestServer.Factory.Services.GetRequiredService<IStorageBackend>();

        var character = new TestCharacter
        {
            UUID = scenario.CharacterUUID,
            Name = "NullDT-" + scenario.CharacterUUID[..8],
            Rules = new List<TestSharingRule>
            {
                new() { TargetType = SharingTargetType.Public, DataType = null },
            },
            BlueprintUUIDs = Enumerable.Range(0, scenario.BlueprintCount)
                .Select(_ => Guid.NewGuid().ToString()).ToList(),
            SurveyUUIDs = Enumerable.Range(0, scenario.SurveyCount)
                .Select(_ => Guid.NewGuid().ToString()).ToList(),
            ColonyUUIDs = Enumerable.Range(0, scenario.ColonyCount)
                .Select(_ => Guid.NewGuid().ToString()).ToList(),
        };

        await SeedCharacterData(storage, character);

        using var client = SharedTestServer.Factory.CreateClient();

        // All three endpoints should include this character's data
        var bpResponse = await client.GetAsync(
            "/api/v1/public/blueprints?pageSize=100");
        bpResponse.EnsureSuccessStatusCode();
        var bpJson = await bpResponse.Content
            .ReadFromJsonAsync<JsonElement>();
        var bpUUIDs = ExtractEntityUUIDs(bpJson.GetProperty("items"));

        foreach (var uuid in character.BlueprintUUIDs)
        {
            Assert.That(bpUUIDs, Does.Contain(uuid),
                $"Blueprint {uuid[..8]} should appear (null DataType rule)");
        }

        var svResponse = await client.GetAsync(
            "/api/v1/public/surveys?pageSize=100");
        svResponse.EnsureSuccessStatusCode();
        var svJson = await svResponse.Content
            .ReadFromJsonAsync<JsonElement>();
        var svUUIDs = ExtractEntityUUIDs(svJson.GetProperty("items"));

        foreach (var uuid in character.SurveyUUIDs)
        {
            Assert.That(svUUIDs, Does.Contain(uuid),
                $"Survey {uuid[..8]} should appear (null DataType rule)");
        }

        var colResponse = await client.GetAsync(
            "/api/v1/public/colonies?pageSize=100");
        colResponse.EnsureSuccessStatusCode();
        var colJson = await colResponse.Content
            .ReadFromJsonAsync<JsonElement>();
        var colUUIDs = ExtractEntityUUIDs(colJson.GetProperty("items"));

        foreach (var uuid in character.ColonyUUIDs)
        {
            Assert.That(colUUIDs, Does.Contain(uuid),
                $"Colony {uuid[..8]} should appear (null DataType rule)");
        }

        await CleanupCharacterData(storage, character);
    }

    private async Task RunSpecificDataTypeScenario(
        SpecificDataTypeScenario scenario)
    {
        var storage = SharedTestServer.Factory.Services.GetRequiredService<IStorageBackend>();

        var character = new TestCharacter
        {
            UUID = scenario.CharacterUUID,
            Name = "SpecDT-" + scenario.CharacterUUID[..8],
            Rules = new List<TestSharingRule>
            {
                new()
                {
                    TargetType = SharingTargetType.Public,
                    DataType = scenario.SharedDataType,
                },
            },
            BlueprintUUIDs = Enumerable.Range(0, scenario.BlueprintCount)
                .Select(_ => Guid.NewGuid().ToString()).ToList(),
            SurveyUUIDs = Enumerable.Range(0, scenario.SurveyCount)
                .Select(_ => Guid.NewGuid().ToString()).ToList(),
            ColonyUUIDs = Enumerable.Range(0, scenario.ColonyCount)
                .Select(_ => Guid.NewGuid().ToString()).ToList(),
        };

        await SeedCharacterData(storage, character);

        using var client = SharedTestServer.Factory.CreateClient();

        var endpointMap = new Dictionary<string, (string Url, List<string> UUIDs)>
        {
            ["Blueprints"] = (
                "/api/v1/public/blueprints?pageSize=100",
                character.BlueprintUUIDs),
            ["Surveys"] = (
                "/api/v1/public/surveys?pageSize=100",
                character.SurveyUUIDs),
            ["Colonies"] = (
                "/api/v1/public/colonies?pageSize=100",
                character.ColonyUUIDs),
        };

        foreach (var (dataType, (url, entityUUIDs)) in endpointMap)
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content
                .ReadFromJsonAsync<JsonElement>();
            var returnedUUIDs = ExtractEntityUUIDs(json.GetProperty("items"));

            if (dataType == scenario.SharedDataType)
            {
                foreach (var uuid in entityUUIDs)
                {
                    Assert.That(returnedUUIDs, Does.Contain(uuid),
                        $"{dataType} entity {uuid[..8]} should appear " +
                        $"(rule matches {scenario.SharedDataType})");
                }
            }
            else
            {
                foreach (var uuid in entityUUIDs)
                {
                    Assert.That(returnedUUIDs, Does.Not.Contain(uuid),
                        $"{dataType} entity {uuid[..8]} should NOT appear " +
                        $"(rule is for {scenario.SharedDataType} only)");
                }
            }
        }

        await CleanupCharacterData(storage, character);
    }

    // ===== Nested Types =====

    private sealed class TestCharacter
    {
        public string UUID { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public List<TestSharingRule> Rules { get; set; } = new();

        public List<string> BlueprintUUIDs { get; set; } = new();

        public List<string> SurveyUUIDs { get; set; } = new();

        public List<string> ColonyUUIDs { get; set; } = new();
    }

    private sealed class TestSharingRule
    {
        public SharingTargetType TargetType { get; set; }

        public string? DataType { get; set; }
    }

    private sealed class FilteringScenario
    {
        public List<TestCharacter> Characters { get; set; } = new();

        public override string ToString()
        {
            return $"Scenario({Characters.Count} chars, " +
                $"rules=[{string.Join(",", Characters.Select(c => c.Rules.Count))}])";
        }
    }

    private sealed class NullDataTypeScenario
    {
        public string CharacterUUID { get; set; } = string.Empty;

        public int BlueprintCount { get; set; }

        public int SurveyCount { get; set; }

        public int ColonyCount { get; set; }

        public override string ToString()
        {
            return $"NullDT(bp={BlueprintCount},sv={SurveyCount}," +
                $"col={ColonyCount})";
        }
    }

    private sealed class SpecificDataTypeScenario
    {
        public string CharacterUUID { get; set; } = string.Empty;

        public string SharedDataType { get; set; } = string.Empty;

        public int BlueprintCount { get; set; }

        public int SurveyCount { get; set; }

        public int ColonyCount { get; set; }

        public override string ToString()
        {
            return $"SpecificDT({SharedDataType},bp={BlueprintCount}," +
                $"sv={SurveyCount},col={ColonyCount})";
        }
    }
}
