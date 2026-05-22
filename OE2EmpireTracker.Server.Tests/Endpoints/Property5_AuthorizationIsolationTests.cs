// -----------------------------------------------------------------------
// <copyright file="Property5_AuthorizationIsolationTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;
using OE2EmpireTracker.Server.Tests;

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Property 5: Authorization Isolation (Hardened Endpoints).
/// For any authenticated request where the caller has no permission-system relationship
/// with the target resource (not owner, not grantee, not faction member, not shared-with,
/// not Owner role): the response status code SHALL be 403 and the response body SHALL
/// contain no entity data, UUIDs, or counts.
/// **Validates: Req 14, 15, 16, 17**
/// </summary>
[TestFixture]
public class Property5_AuthorizationIsolationTests
{
    private TestServerFactory _factory = null!;
    private HttpClient _ownerClient = null!;
    private List<(string Token, string UUID)> _characters = null!;
    private List<string> _factionUUIDs = null!;

    /// <summary>
    /// Sets up the test server and creates multiple isolated characters and factions.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestServerFactory();
        _factory.SeedOwnerToken();
        _ownerClient = _factory.CreateAuthenticatedClient();
        _characters = new List<(string Token, string UUID)>();
        _factionUUIDs = new List<string>();

        // Create 5 characters with no sharing rules between them
        for (int i = 0; i < 5; i++)
        {
            var resp = _ownerClient
                .PostAsJsonAsync("/api/v1/tokens", new { characterName = $"P5Char{i}" })
                .GetAwaiter().GetResult();
            var json = resp.Content
                .ReadFromJsonAsync<JsonElement>()
                .GetAwaiter().GetResult();
            var token = json.GetProperty("token").GetString()!;
            var uuid = json.GetProperty("characterUUID").GetString()!;
            _characters.Add((token, uuid));
        }

        // Create 3 factions (no characters are members)
        for (int i = 0; i < 3; i++)
        {
            var resp = _ownerClient
                .PostAsJsonAsync("/api/v1/factions", new { name = $"P5Faction{i}" })
                .GetAwaiter().GetResult();
            var json = resp.Content
                .ReadFromJsonAsync<JsonElement>()
                .GetAwaiter().GetResult();
            _factionUUIDs.Add(json.GetProperty("uuid").GetString()!);
        }
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
    /// Property: character endpoints return 403 with no entity data when caller
    /// has no permission relationship with the target character.
    /// Randomly picks caller/target pairs and verifies isolation on all hardened
    /// character endpoints.
    /// </summary>
    [Test]
    public async Task CharacterIsolation_NoRelationship_Returns403WithNoEntityData()
    {
        var rng = new Random(42);

        for (int iteration = 0; iteration < 20; iteration++)
        {
            // Pick a random caller
            int callerIdx = rng.Next(_characters.Count);
            var (callerToken, callerUUID) = _characters[callerIdx];

            // Pick a random target different from caller
            int targetIdx;
            do
            {
                targetIdx = rng.Next(_characters.Count);
            }
            while (targetIdx == callerIdx);

            var (_, targetUUID) = _characters[targetIdx];

            using var client = _factory.CreateAuthenticatedClient(callerToken);

            // Test all hardened character endpoints
            var endpoints = new[]
            {
                $"/api/v1/characters/{targetUUID}",
                $"/api/v1/characters/{targetUUID}/capabilities",
                $"/api/v1/characters/{targetUUID}/clearance-levels",
                $"/api/v1/characters/{targetUUID}/groups",
                $"/api/v1/characters/{targetUUID}/intel",
            };

            foreach (var endpoint in endpoints)
            {
                var response = await client.GetAsync(endpoint);

                Assert.That(
                    response.StatusCode,
                    Is.EqualTo(HttpStatusCode.Forbidden),
                    $"Iteration {iteration}: GET {endpoint} " +
                    $"(caller={callerUUID[..8]}, target={targetUUID[..8]}) " +
                    $"expected 403 but got {(int)response.StatusCode}");

                var body = await response.Content.ReadAsStringAsync();
                var json = JsonSerializer.Deserialize<JsonElement>(body);

                // Verify response contains { "error": "Access denied" }
                Assert.That(
                    json.GetProperty("error").GetString(),
                    Is.EqualTo("Access denied"),
                    $"Iteration {iteration}: GET {endpoint} " +
                    $"expected error='Access denied'");

                // Verify no entity data leaked (no UUIDs, no counts)
                AssertNoEntityDataLeaked(body, targetUUID, iteration, endpoint);
            }
        }
    }


    /// <summary>
    /// Property: faction sub-resource endpoints return 403 with no entity data
    /// when caller is not a member of the faction.
    /// Randomly picks caller/faction pairs and verifies isolation on all hardened
    /// faction sub-resource endpoints.
    /// </summary>
    [Test]
    public async Task FactionIsolation_NonMember_Returns403WithNoEntityData()
    {
        var rng = new Random(99);

        for (int iteration = 0; iteration < 20; iteration++)
        {
            // Pick a random caller (none are faction members)
            int callerIdx = rng.Next(_characters.Count);
            var (callerToken, callerUUID) = _characters[callerIdx];

            // Pick a random faction
            int factionIdx = rng.Next(_factionUUIDs.Count);
            var factionUUID = _factionUUIDs[factionIdx];

            using var client = _factory.CreateAuthenticatedClient(callerToken);

            // Test all hardened faction sub-resource endpoints
            var endpoints = new[]
            {
                $"/api/v1/factions/{factionUUID}/capabilities",
                $"/api/v1/factions/{factionUUID}/clearance-levels",
                $"/api/v1/factions/{factionUUID}/groups",
                $"/api/v1/factions/{factionUUID}/members",
            };

            foreach (var endpoint in endpoints)
            {
                var response = await client.GetAsync(endpoint);

                Assert.That(
                    response.StatusCode,
                    Is.EqualTo(HttpStatusCode.Forbidden),
                    $"Iteration {iteration}: GET {endpoint} " +
                    $"(caller={callerUUID[..8]}, faction={factionUUID[..8]}) " +
                    $"expected 403 but got {(int)response.StatusCode}");

                var body = await response.Content.ReadAsStringAsync();
                var json = JsonSerializer.Deserialize<JsonElement>(body);

                // Verify response contains { "error": "Access denied" }
                Assert.That(
                    json.GetProperty("error").GetString(),
                    Is.EqualTo("Access denied"),
                    $"Iteration {iteration}: GET {endpoint} " +
                    $"expected error='Access denied'");

                // Verify no entity data leaked
                AssertNoEntityDataLeaked(body, factionUUID, iteration, endpoint);
            }
        }
    }


    /// <summary>
    /// Property: combined isolation test with randomized endpoint selection.
    /// For each iteration, randomly selects either a character or faction endpoint
    /// and verifies the 403 isolation property holds.
    /// </summary>
    [Test]
    public async Task CombinedIsolation_RandomEndpointSelection_AllReturn403()
    {
        var rng = new Random(2024);

        for (int iteration = 0; iteration < 30; iteration++)
        {
            // Pick a random caller
            int callerIdx = rng.Next(_characters.Count);
            var (callerToken, callerUUID) = _characters[callerIdx];

            using var client = _factory.CreateAuthenticatedClient(callerToken);

            // Randomly choose between character and faction endpoints
            bool testCharacter = rng.Next(2) == 0;

            if (testCharacter)
            {
                // Pick a random target character different from caller
                int targetIdx;
                do
                {
                    targetIdx = rng.Next(_characters.Count);
                }
                while (targetIdx == callerIdx);

                var (_, targetUUID) = _characters[targetIdx];

                // Pick a random character endpoint
                var charEndpoints = new[]
                {
                    $"/api/v1/characters/{targetUUID}",
                    $"/api/v1/characters/{targetUUID}/capabilities",
                    $"/api/v1/characters/{targetUUID}/clearance-levels",
                    $"/api/v1/characters/{targetUUID}/groups",
                    $"/api/v1/characters/{targetUUID}/intel",
                };
                var endpoint = charEndpoints[rng.Next(charEndpoints.Length)];

                var response = await client.GetAsync(endpoint);

                Assert.That(
                    response.StatusCode,
                    Is.EqualTo(HttpStatusCode.Forbidden),
                    $"Iteration {iteration}: GET {endpoint} " +
                    $"(caller={callerUUID[..8]}, target={targetUUID[..8]}) " +
                    $"expected 403");

                var body = await response.Content.ReadAsStringAsync();
                var json = JsonSerializer.Deserialize<JsonElement>(body);
                Assert.That(
                    json.GetProperty("error").GetString(),
                    Is.EqualTo("Access denied"));
                AssertNoEntityDataLeaked(body, targetUUID, iteration, endpoint);
            }
            else
            {
                // Pick a random faction
                var factionUUID = _factionUUIDs[rng.Next(_factionUUIDs.Count)];

                // Pick a random faction sub-resource endpoint
                var facEndpoints = new[]
                {
                    $"/api/v1/factions/{factionUUID}/capabilities",
                    $"/api/v1/factions/{factionUUID}/clearance-levels",
                    $"/api/v1/factions/{factionUUID}/groups",
                    $"/api/v1/factions/{factionUUID}/members",
                };
                var endpoint = facEndpoints[rng.Next(facEndpoints.Length)];

                var response = await client.GetAsync(endpoint);

                Assert.That(
                    response.StatusCode,
                    Is.EqualTo(HttpStatusCode.Forbidden),
                    $"Iteration {iteration}: GET {endpoint} " +
                    $"(caller={callerUUID[..8]}, faction={factionUUID[..8]}) " +
                    $"expected 403");

                var body = await response.Content.ReadAsStringAsync();
                var json = JsonSerializer.Deserialize<JsonElement>(body);
                Assert.That(
                    json.GetProperty("error").GetString(),
                    Is.EqualTo("Access denied"));
                AssertNoEntityDataLeaked(body, factionUUID, iteration, endpoint);
            }
        }
    }


    /// <summary>
    /// Asserts that the response body contains no entity data beyond the error message.
    /// Checks that the target UUID does not appear in the response (except in the
    /// generic error structure), and that no array counts or entity collections are present.
    /// </summary>
    /// <param name="responseBody">The raw response body string.</param>
    /// <param name="targetUUID">The target resource UUID that should not be leaked.</param>
    /// <param name="iteration">The test iteration number for diagnostics.</param>
    /// <param name="endpoint">The endpoint path for diagnostics.</param>
    private static void AssertNoEntityDataLeaked(
        string responseBody,
        string targetUUID,
        int iteration,
        string endpoint)
    {
        // The response should only contain { "error": "Access denied" }
        // It should NOT contain the target UUID
        Assert.That(
            responseBody,
            Does.Not.Contain(targetUUID),
            $"Iteration {iteration}: GET {endpoint} " +
            $"response leaked target UUID {targetUUID[..8]}...");

        // It should NOT contain array-like structures (entity collections)
        Assert.That(
            responseBody,
            Does.Not.Contain("["),
            $"Iteration {iteration}: GET {endpoint} " +
            $"response contains array data (potential entity leak)");

        // It should NOT contain "count" or "items" keys (entity data patterns)
        Assert.That(
            responseBody.ToLowerInvariant(),
            Does.Not.Contain("\"count\""),
            $"Iteration {iteration}: GET {endpoint} " +
            $"response contains 'count' (potential data leak)");

        Assert.That(
            responseBody.ToLowerInvariant(),
            Does.Not.Contain("\"items\""),
            $"Iteration {iteration}: GET {endpoint} " +
            $"response contains 'items' (potential data leak)");
    }
}
