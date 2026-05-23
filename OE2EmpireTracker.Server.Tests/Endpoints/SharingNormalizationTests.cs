// -----------------------------------------------------------------------
// <copyright file="SharingNormalizationTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FsCheck;
using FsCheck.NUnit;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Property 4: Server Normalizes Submitted Rules.
/// For any set of sharing rules submitted via PUT, regardless of the
/// client-provided OwnerCharacterUUID and Id values, the server SHALL
/// overwrite OwnerCharacterUUID with the authenticated character's UUID
/// on every rule, and SHALL assign a non-empty GUID to any rule with
/// an empty or missing Id.
/// **Validates: Requirements 7.3, 7.4**
/// Feature: sharing-visibility-system, Property 4: Server Normalizes Submitted Rules.
/// </summary>
[TestFixture]
public class SharingNormalizationTests
{
    private TestServerFactory _factory = null!;
    private HttpClient _ownerClient = null!;
    private string _characterUUID = null!;

    /// <summary>
    /// Sets up the test server factory and creates a character for testing.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestServerFactory();
        _factory.SeedOwnerToken();
        _ownerClient = _factory.CreateAuthenticatedClient();

        // Create a character to own the sharing rules
        var response = _ownerClient
            .PostAsJsonAsync("/api/v1/characters", new { name = "NormTestChar" })
            .GetAwaiter().GetResult();
        var json = response.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        _characterUUID = json.GetProperty("uuid").GetString()!;
    }

    /// <summary>
    /// Tears down the test server.
    /// </summary>
    [OneTimeTearDown]
    public void TearDown()
    {
        _ownerClient.Dispose();
        _factory.Dispose();
    }

    /// <summary>
    /// Property: For any rules submitted with random OwnerCharacterUUID values,
    /// the server overwrites them with the authenticated character's UUID.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property OwnerUUID_IsOverwrittenWithAuthenticatedCharacter()
    {
        return Prop.ForAll(
            NormalizationScenarioArbitrary(),
            scenario =>
            {
                RunOwnerOverwriteScenario(scenario).GetAwaiter().GetResult();
            });
    }

    /// <summary>
    /// Property: For any rules submitted with empty or missing Id values,
    /// the server assigns a non-empty GUID.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property EmptyId_GetsAssignedNonEmptyGuid()
    {
        return Prop.ForAll(
            EmptyIdScenarioArbitrary(),
            scenario =>
            {
                RunEmptyIdScenario(scenario).GetAwaiter().GetResult();
            });
    }

    /// <summary>
    /// Property: For any rules submitted with an existing non-empty Id,
    /// the server preserves the existing Id value.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property ExistingId_IsPreserved()
    {
        return Prop.ForAll(
            ExistingIdScenarioArbitrary(),
            scenario =>
            {
                RunExistingIdScenario(scenario).GetAwaiter().GetResult();
            });
    }

    // ===== Static Generators =====

    private static Arbitrary<NormalizationScenario> NormalizationScenarioArbitrary()
    {
        var gen = from ruleCount in Gen.Choose(1, 5)
                  from rules in Gen.ListOf(ruleCount, RuleWithRandomOwnerGen())
                  select new NormalizationScenario
                  {
                      Rules = rules.ToList(),
                  };
        return Arb.From(gen);
    }

    private static Arbitrary<EmptyIdScenario> EmptyIdScenarioArbitrary()
    {
        var gen = from ruleCount in Gen.Choose(1, 4)
                  from rules in Gen.ListOf(ruleCount, RuleWithEmptyIdGen())
                  select new EmptyIdScenario
                  {
                      Rules = rules.ToList(),
                  };
        return Arb.From(gen);
    }

    private static Arbitrary<ExistingIdScenario> ExistingIdScenarioArbitrary()
    {
        var gen = from ruleCount in Gen.Choose(1, 4)
                  from rules in Gen.ListOf(ruleCount, RuleWithExistingIdGen())
                  select new ExistingIdScenario
                  {
                      Rules = rules.ToList(),
                  };
        return Arb.From(gen);
    }

    private static Gen<NormRuleData> RuleWithRandomOwnerGen()
    {
        return from ownerUUID in Gen.Elements(
                   "random-owner-1", "random-owner-2", "attacker-uuid",
                   string.Empty, "someone-elses-uuid", Guid.NewGuid().ToString())
               from targetType in Gen.Elements("Faction", "Character", "Public")
               from targetUUID in Gen.Elements(
                   "target-1", "target-2", "target-3")
               from dataType in Gen.Elements<string?>(
                   null, "Blueprints", "Surveys", "Colonies")
               select new NormRuleData
               {
                   OwnerCharacterUUID = ownerUUID,
                   TargetType = targetType,
                   TargetUUID = targetUUID,
                   DataType = dataType,
                   Id = Guid.NewGuid().ToString(),
               };
    }

    private static Gen<NormRuleData> RuleWithEmptyIdGen()
    {
        return from emptyId in Gen.Elements(
                   string.Empty, " ", (string?)null)
               from targetType in Gen.Elements("Faction", "Character", "Public")
               from targetUUID in Gen.Elements(
                   "target-1", "target-2", "target-3")
               from dataType in Gen.Elements<string?>(
                   null, "Blueprints", "Surveys", "Colonies")
               select new NormRuleData
               {
                   OwnerCharacterUUID = "irrelevant-owner",
                   TargetType = targetType,
                   TargetUUID = targetUUID,
                   DataType = dataType,
                   Id = emptyId,
               };
    }

    private static Gen<NormRuleData> RuleWithExistingIdGen()
    {
        return from targetType in Gen.Elements("Faction", "Character", "Public")
               from targetUUID in Gen.Elements(
                   "target-1", "target-2", "target-3")
               from dataType in Gen.Elements<string?>(
                   null, "Blueprints", "Surveys", "Colonies")
               let existingId = Guid.NewGuid().ToString()
               select new NormRuleData
               {
                   OwnerCharacterUUID = "irrelevant-owner",
                   TargetType = targetType,
                   TargetUUID = targetUUID,
                   DataType = dataType,
                   Id = existingId,
               };
    }

    private static string SerializeRulesWithOwner(List<NormRuleData> rules)
    {
        var objects = rules.Select(r => new Dictionary<string, object?>
        {
            ["id"] = r.Id,
            ["ownerCharacterUUID"] = r.OwnerCharacterUUID,
            ["targetUUID"] = r.TargetUUID,
            ["targetType"] = TargetTypeToInt(r.TargetType),
            ["dataType"] = r.DataType,
        });
        return JsonSerializer.Serialize(objects);
    }

    private static string SerializeRulesWithId(List<NormRuleData> rules)
    {
        var objects = rules.Select(r =>
        {
            var dict = new Dictionary<string, object?>
            {
                ["targetUUID"] = r.TargetUUID,
                ["targetType"] = TargetTypeToInt(r.TargetType),
                ["dataType"] = r.DataType,
            };

            if (r.Id != null)
            {
                dict["id"] = r.Id;
            }

            return dict;
        });
        return JsonSerializer.Serialize(objects);
    }

    private static int TargetTypeToInt(string targetType)
    {
        return targetType switch
        {
            "Faction" => 0,
            "Character" => 1,
            "Public" => 2,
            _ => 0,
        };
    }

    // ===== Instance Test Helpers =====

    private async Task RunOwnerOverwriteScenario(NormalizationScenario scenario)
    {
        var rulesJson = SerializeRulesWithOwner(scenario.Rules);
        var content = new StringContent(rulesJson, Encoding.UTF8, "application/json");

        var response = await _ownerClient.PutAsync(
            $"/api/v1/characters/{_characterUUID}/sharing", content);
        response.EnsureSuccessStatusCode();

        // Verify persisted rules have the authenticated character's UUID
        var storage = _factory.Services.GetRequiredService<IStorageBackend>();
        var persisted = await storage.GetSharingRulesForCharacterAsync(_characterUUID);

        foreach (var rule in persisted)
        {
            Assert.That(
                rule.OwnerCharacterUUID,
                Is.EqualTo(_characterUUID),
                $"Rule {rule.Id} should have OwnerCharacterUUID={_characterUUID}, " +
                $"but got {rule.OwnerCharacterUUID}");
        }
    }

    private async Task RunEmptyIdScenario(EmptyIdScenario scenario)
    {
        var rulesJson = SerializeRulesWithId(scenario.Rules);
        var content = new StringContent(rulesJson, Encoding.UTF8, "application/json");

        var response = await _ownerClient.PutAsync(
            $"/api/v1/characters/{_characterUUID}/sharing", content);
        response.EnsureSuccessStatusCode();

        // Verify persisted rules all have non-empty Ids
        var storage = _factory.Services.GetRequiredService<IStorageBackend>();
        var persisted = await storage.GetSharingRulesForCharacterAsync(_characterUUID);

        Assert.That(
            persisted.Count,
            Is.EqualTo(scenario.Rules.Count),
            "Persisted rule count should match submitted count");

        foreach (var rule in persisted)
        {
            Assert.That(
                string.IsNullOrWhiteSpace(rule.Id),
                Is.False,
                "Every persisted rule must have a non-empty Id");

            // Verify it's a valid GUID format
            Assert.That(
                Guid.TryParse(rule.Id, out _),
                Is.True,
                $"Rule Id '{rule.Id}' should be a valid GUID");
        }
    }

    private async Task RunExistingIdScenario(ExistingIdScenario scenario)
    {
        var rulesJson = SerializeRulesWithId(scenario.Rules);
        var content = new StringContent(rulesJson, Encoding.UTF8, "application/json");

        var response = await _ownerClient.PutAsync(
            $"/api/v1/characters/{_characterUUID}/sharing", content);
        response.EnsureSuccessStatusCode();

        // Verify persisted rules preserve the submitted Ids
        var storage = _factory.Services.GetRequiredService<IStorageBackend>();
        var persisted = await storage.GetSharingRulesForCharacterAsync(_characterUUID);

        var submittedIds = scenario.Rules
            .Select(r => r.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet();

        var persistedIds = persisted.Select(r => r.Id).ToHashSet();

        foreach (var expectedId in submittedIds)
        {
            Assert.That(
                persistedIds,
                Does.Contain(expectedId),
                $"Existing Id '{expectedId}' should be preserved");
        }
    }

    // ===== Nested Types =====

    private sealed class NormRuleData
    {
        public string? Id { get; set; }

        public string OwnerCharacterUUID { get; set; } = string.Empty;

        public string TargetType { get; set; } = string.Empty;

        public string TargetUUID { get; set; } = string.Empty;

        public string? DataType { get; set; }

        public override string ToString()
        {
            return $"NormRule(owner={OwnerCharacterUUID[..Math.Min(8, OwnerCharacterUUID.Length)]}," +
                $"id={Id ?? "null"})";
        }
    }

    private sealed class NormalizationScenario
    {
        public List<NormRuleData> Rules { get; set; } = new();

        public override string ToString()
        {
            return $"NormScenario({Rules.Count} rules with random owners)";
        }
    }

    private sealed class EmptyIdScenario
    {
        public List<NormRuleData> Rules { get; set; } = new();

        public override string ToString()
        {
            return $"EmptyIdScenario({Rules.Count} rules with empty/null Ids)";
        }
    }

    private sealed class ExistingIdScenario
    {
        public List<NormRuleData> Rules { get; set; } = new();

        public override string ToString()
        {
            var ids = string.Join(",", Rules.Select(r => r.Id?[..8] ?? "null"));
            return $"ExistingIdScenario({Rules.Count} rules, ids=[{ids}])";
        }
    }
}
