// -----------------------------------------------------------------------
// <copyright file="SharingValidationTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using FsCheck.NUnit;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Property 3: Server Rejects Invalid Sharing Rules.
/// For any sharing rule submission where TargetUUID is empty/whitespace OR
/// TargetType is not a valid enum value, the server SHALL return HTTP 400
/// and SHALL NOT persist the rules.
/// **Validates: Requirements 7.1, 7.2**
/// Feature: sharing-visibility-system, Property 3: Server Rejects Invalid Sharing Rules.
/// </summary>
[TestFixture]
public class SharingValidationTests
{
    private HttpClient _ownerClient = null!;
    private string _characterUUID = null!;

    /// <summary>
    /// Sets up the test server factory and creates a character for testing.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        var factory = SharedTestServer.Factory;
        _ownerClient = factory.CreateAuthenticatedClient();

        // Create a character to own the sharing rules
        var response = _ownerClient
            .PostAsJsonAsync("/api/v1/characters", new { name = "ValidationTestChar" })
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
    }

    /// <summary>
    /// Property: For any sharing rule with an empty or whitespace-only TargetUUID,
    /// the server returns HTTP 400 Bad Request.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property EmptyTargetUUID_Returns400()
    {
        return Prop.ForAll(
            EmptyTargetUUIDRulesArbitrary(),
            scenario =>
            {
                RunEmptyTargetUUIDScenario(scenario).GetAwaiter().GetResult();
            });
    }

    /// <summary>
    /// Property: For any sharing rule with an invalid TargetType (non-enum integer),
    /// the server returns HTTP 400 Bad Request.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property InvalidTargetType_Returns400()
    {
        return Prop.ForAll(
            InvalidTargetTypeRulesArbitrary(),
            scenario =>
            {
                RunInvalidTargetTypeScenario(scenario).GetAwaiter().GetResult();
            });
    }

    /// <summary>
    /// Property: After a 400 response for invalid rules, the server does NOT
    /// persist any of the submitted rules.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property RejectedRules_AreNotPersisted()
    {
        return Prop.ForAll(
            EmptyTargetUUIDRulesArbitrary(),
            scenario =>
            {
                RunNotPersistedScenario(scenario).GetAwaiter().GetResult();
            });
    }

    // ===== Static Generators =====

    private static Arbitrary<ValidationScenario> EmptyTargetUUIDRulesArbitrary()
    {
        var gen = from validCount in Gen.Choose(0, 3)
                  from validRules in Gen.ListOf(validCount, ValidRuleGen())
                  from invalidRule in EmptyUUIDRuleGen()
                  from insertPos in Gen.Choose(0, validCount)
                  let allRules = InsertAt(validRules.ToList(), insertPos, invalidRule)
                  select new ValidationScenario
                  {
                      Rules = allRules,
                  };
        return Arb.From(gen);
    }

    private static Arbitrary<InvalidTargetTypeScenario> InvalidTargetTypeRulesArbitrary()
    {
        var gen = from invalidValue in Gen.Elements(
                      "InvalidType", "NotAnEnum", "faction!", "UNKNOWN",
                      "123abc", "null", "undefined", "true")
                  from targetUUID in Gen.Elements(
                      "abc-123", "def-456", "ghi-789", "valid-uuid-value")
                  from dataType in Gen.Elements<string?>(
                      null, "Blueprints", "Surveys", "Colonies")
                  select new InvalidTargetTypeScenario
                  {
                      InvalidTargetTypeValue = invalidValue,
                      TargetUUID = targetUUID,
                      DataType = dataType,
                  };
        return Arb.From(gen);
    }

    private static Gen<RuleData> ValidRuleGen()
    {
        return from targetType in Gen.Elements("Faction", "Character", "Public")
               from targetUUID in Gen.Elements(
                   "valid-uuid-1", "valid-uuid-2", "valid-uuid-3")
               from dataType in Gen.Elements<string?>(
                   null, "Blueprints", "Surveys", "Colonies")
               select new RuleData
               {
                   TargetType = targetType,
                   TargetUUID = targetUUID,
                   DataType = dataType,
               };
    }

    private static Gen<RuleData> EmptyUUIDRuleGen()
    {
        return from whitespace in Gen.Elements(
                   string.Empty, " ", "  ", "\t", "   \t  ", "\n", " \r\n ")
               from targetType in Gen.Elements("Faction", "Character", "Public")
               from dataType in Gen.Elements<string?>(
                   null, "Blueprints", "Surveys", "Colonies")
               select new RuleData
               {
                   TargetType = targetType,
                   TargetUUID = whitespace,
                   DataType = dataType,
               };
    }

    private static List<RuleData> InsertAt(
        List<RuleData> list, int position, RuleData item)
    {
        var result = new List<RuleData>(list);
        var clampedPos = Math.Min(position, result.Count);
        result.Insert(clampedPos, item);
        return result;
    }

    private static string SerializeRules(List<RuleData> rules)
    {
        var objects = rules.Select(r => new Dictionary<string, object?>
        {
            ["targetUUID"] = r.TargetUUID,
            ["targetType"] = TargetTypeToInt(r.TargetType),
            ["dataType"] = r.DataType,
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

    private static string BuildInvalidTargetTypeJson(InvalidTargetTypeScenario scenario)
    {
        var dataTypePart = scenario.DataType != null
            ? $"\"dataType\":\"{scenario.DataType}\""
            : "\"dataType\":null";

        return "{" +
            $"\"targetUUID\":\"{scenario.TargetUUID}\"," +
            $"\"targetType\":\"{scenario.InvalidTargetTypeValue}\"," +
            $"{dataTypePart}" +
            "}";
    }

    // ===== Instance Test Helpers =====

    private async Task RunEmptyTargetUUIDScenario(ValidationScenario scenario)
    {
        var rulesJson = SerializeRules(scenario.Rules);
        var content = new StringContent(rulesJson, Encoding.UTF8, "application/json");

        var response = await _ownerClient.PutAsync(
            $"/api/v1/characters/{_characterUUID}/sharing", content);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest),
            $"Expected 400 for rules with empty TargetUUID, got {(int)response.StatusCode}");
    }

    private async Task RunInvalidTargetTypeScenario(InvalidTargetTypeScenario scenario)
    {
        // Build raw JSON with an invalid integer for TargetType
        var ruleJson = BuildInvalidTargetTypeJson(scenario);
        var content = new StringContent(
            $"[{ruleJson}]", Encoding.UTF8, "application/json");

        var response = await _ownerClient.PutAsync(
            $"/api/v1/characters/{_characterUUID}/sharing", content);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest),
            $"Expected 400 for invalid TargetType='{scenario.InvalidTargetTypeValue}', " +
            $"got {(int)response.StatusCode}");
    }

    private async Task RunNotPersistedScenario(ValidationScenario scenario)
    {
        // First, seed some known valid rules
        var validRules = new List<RuleData>
        {
            new()
            {
                TargetType = "Faction",
                TargetUUID = "known-valid-uuid",
                DataType = "Blueprints",
            },
        };
        var seedJson = SerializeRules(validRules);
        var seedContent = new StringContent(seedJson, Encoding.UTF8, "application/json");
        var seedResponse = await _ownerClient.PutAsync(
            $"/api/v1/characters/{_characterUUID}/sharing", seedContent);
        seedResponse.EnsureSuccessStatusCode();

        // Now submit invalid rules (should be rejected)
        var invalidJson = SerializeRules(scenario.Rules);
        var invalidContent = new StringContent(
            invalidJson, Encoding.UTF8, "application/json");
        var invalidResponse = await _ownerClient.PutAsync(
            $"/api/v1/characters/{_characterUUID}/sharing", invalidContent);

        Assert.That(
            invalidResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest),
            "Expected 400 for invalid rules");

        // Verify original rules are still intact (not overwritten)
        var storage = SharedTestServer.Factory.Services.GetRequiredService<IStorageBackend>();
        var persistedRules = await storage.GetSharingRulesForCharacterAsync(
            _characterUUID);

        Assert.That(
            persistedRules.Any(r => r.TargetUUID == "known-valid-uuid"),
            Is.True,
            "Original valid rules should still be persisted after rejected submission");
    }

    // ===== Nested Types =====

    private sealed class RuleData
    {
        public string TargetType { get; set; } = string.Empty;

        public string TargetUUID { get; set; } = string.Empty;

        public string? DataType { get; set; }

        public override string ToString()
        {
            return $"Rule(type={TargetType},uuid='{TargetUUID}',dt={DataType ?? "null"})";
        }
    }

    private sealed class ValidationScenario
    {
        public List<RuleData> Rules { get; set; } = new();

        public override string ToString()
        {
            var invalidCount = Rules.Count(r =>
                string.IsNullOrWhiteSpace(r.TargetUUID));
            return $"ValidationScenario({Rules.Count} rules, " +
                $"{invalidCount} with empty UUID)";
        }
    }

    private sealed class InvalidTargetTypeScenario
    {
        public string InvalidTargetTypeValue { get; set; } = string.Empty;

        public string TargetUUID { get; set; } = string.Empty;

        public string? DataType { get; set; }

        public override string ToString()
        {
            return $"InvalidTargetType(value={InvalidTargetTypeValue}," +
                $"uuid={TargetUUID})";
        }
    }
}
