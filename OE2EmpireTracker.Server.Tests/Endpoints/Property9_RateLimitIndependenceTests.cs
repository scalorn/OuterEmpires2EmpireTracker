// -----------------------------------------------------------------------
// <copyright file="Property9_RateLimitIndependenceTests.cs" company="OE2EmpireTracker">
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
/// Property 9: Rate Limit Independence.
/// The bulk import endpoint's rate limit SHALL be per-token and independent.
/// Token A exceeding its rate limit SHALL NOT affect Token B's ability to call the endpoint.
/// **Validates: Req 2 Criterion 10**
/// </summary>
[TestFixture]
public class Property9_RateLimitIndependenceTests
{
    private HttpClient _ownerClient = null!;
    private List<(string Token, string TokenId, string CharUUID)> _characters = null!;

    private TestServerFactory Factory => SharedTestServer.Factory;

    /// <summary>
    /// Sets up the test server and creates multiple character tokens for rate limit testing.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        var factory = SharedTestServer.Factory;
        _ownerClient = factory.CreateAuthenticatedClient();
        _characters = new List<(string Token, string TokenId, string CharUUID)>();

        // Create 4 characters for pairwise rate limit independence testing
        for (int i = 0; i < 4; i++)
        {
            var resp = _ownerClient
                .PostAsJsonAsync("/api/v1/tokens", new { characterName = $"P9Char{i}" })
                .GetAwaiter().GetResult();
            var json = resp.Content
                .ReadFromJsonAsync<JsonElement>()
                .GetAwaiter().GetResult();
            var token = json.GetProperty("token").GetString()!;
            var tokenId = json.GetProperty("tokenId").GetString()!;
            var charUUID = json.GetProperty("characterUUID").GetString()!;
            _characters.Add((token, tokenId, charUUID));
        }
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
    /// Property: Token A exceeding its rate limit does not affect Token B.
    /// Sets Token A's rate limit to a very low value (2 requests/minute),
    /// exhausts Token A's limit until 429 is returned, then verifies Token B
    /// can still make requests successfully.
    /// Repeats with random caller/target pairs.
    /// </summary>
    [Test]
    public async Task RateLimitExceeded_TokenA_DoesNotAffect_TokenB()
    {
        var rng = new Random(9999);

        for (int iteration = 0; iteration < 6; iteration++)
        {
            // Pick two different characters
            int idxA = rng.Next(_characters.Count);
            int idxB;
            do
            {
                idxB = rng.Next(_characters.Count);
            }
            while (idxB == idxA);

            var (tokenA, tokenIdA, charUuidA) = _characters[idxA];
            var (tokenB, tokenIdB, charUuidB) = _characters[idxB];

            // Set Token A's rate limit to 2 requests/minute (very low)
            var setLimitResp = await _ownerClient.PutAsJsonAsync(
                $"/api/v1/tokens/{tokenIdA}/limits",
                new { requestsPerMinute = 2 });
            Assert.That(
                setLimitResp.StatusCode,
                Is.EqualTo(HttpStatusCode.OK),
                $"Iteration {iteration}: Failed to set rate limit for Token A");

            // Ensure Token B has a normal rate limit (300/min)
            var setLimitBResp = await _ownerClient.PutAsJsonAsync(
                $"/api/v1/tokens/{tokenIdB}/limits",
                new { requestsPerMinute = 300 });
            Assert.That(
                setLimitBResp.StatusCode,
                Is.EqualTo(HttpStatusCode.OK),
                $"Iteration {iteration}: Failed to set rate limit for Token B");

            // Exhaust Token A's rate limit by making requests until 429
            using var clientA = Factory.CreateAuthenticatedClient(tokenA);
            bool tokenAHit429 = false;

            for (int req = 0; req < 10; req++)
            {
                var resp = await clientA.GetAsync("/api/v1/factions");
                if (resp.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    tokenAHit429 = true;
                    break;
                }
            }

            Assert.That(
                tokenAHit429,
                Is.True,
                $"Iteration {iteration}: Token A never hit 429 " +
                $"(rate limit may not have been applied)");

            // Now verify Token B can still make requests successfully
            using var clientB = Factory.CreateAuthenticatedClient(tokenB);
            var responseB = await clientB.GetAsync("/api/v1/factions");

            Assert.That(
                responseB.StatusCode,
                Is.EqualTo(HttpStatusCode.OK),
                $"Iteration {iteration}: Token B got {(int)responseB.StatusCode} " +
                $"after Token A was rate-limited. " +
                $"Rate limiting is NOT independent per-token!");
        }
    }

    /// <summary>
    /// Property: Multiple tokens can all make requests in the same time window
    /// without interfering with each other. Verifies that the sliding window
    /// is truly per-token by having all tokens make requests concurrently.
    /// </summary>
    [Test]
    public async Task MultipleTokens_SameTimeWindow_AllSucceed_Independently()
    {
        // Set all tokens to a moderate rate limit (10 requests/minute)
        foreach (var (_, tokenId, _) in _characters)
        {
            var resp = await _ownerClient.PutAsJsonAsync(
                $"/api/v1/tokens/{tokenId}/limits",
                new { requestsPerMinute = 10 });
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        // Each token makes 5 requests — all should succeed since each has
        // a 10 req/min limit and they are independent
        for (int charIdx = 0; charIdx < _characters.Count; charIdx++)
        {
            var (token, _, _) = _characters[charIdx];
            using var client = Factory.CreateAuthenticatedClient(token);

            for (int req = 0; req < 5; req++)
            {
                var resp = await client.GetAsync("/api/v1/factions");
                Assert.That(
                    resp.StatusCode,
                    Is.EqualTo(HttpStatusCode.OK),
                    $"Character {charIdx}, request {req}: " +
                    $"got {(int)resp.StatusCode} instead of 200. " +
                    $"Rate limits may be shared across tokens.");
            }
        }
    }

    /// <summary>
    /// Property: After Token A is rate-limited, Token B can still call the
    /// bulk import endpoint specifically (the endpoint referenced in Req 2 Criterion 10).
    /// Verifies independence on the import endpoint path.
    /// </summary>
    [Test]
    public async Task RateLimitExceeded_TokenA_TokenB_CanStillImport()
    {
        var rng = new Random(7777);

        // Pick two characters
        int idxA = rng.Next(_characters.Count);
        int idxB;
        do
        {
            idxB = rng.Next(_characters.Count);
        }
        while (idxB == idxA);

        var (tokenA, tokenIdA, charUuidA) = _characters[idxA];
        var (tokenB, tokenIdB, charUuidB) = _characters[idxB];

        // Set Token A's rate limit to 2 requests/minute
        await _ownerClient.PutAsJsonAsync(
            $"/api/v1/tokens/{tokenIdA}/limits",
            new { requestsPerMinute = 2 });

        // Ensure Token B has normal rate limit
        await _ownerClient.PutAsJsonAsync(
            $"/api/v1/tokens/{tokenIdB}/limits",
            new { requestsPerMinute = 300 });

        // Exhaust Token A's rate limit
        using var clientA = Factory.CreateAuthenticatedClient(tokenA);
        bool tokenAHit429 = false;

        for (int req = 0; req < 10; req++)
        {
            var resp = await clientA.GetAsync("/api/v1/factions");
            if (resp.StatusCode == HttpStatusCode.TooManyRequests)
            {
                tokenAHit429 = true;
                break;
            }
        }

        Assert.That(
            tokenAHit429,
            Is.True,
            "Token A never hit 429");

        // Token B calls the bulk import endpoint with an empty (but valid) payload
        using var clientB = Factory.CreateAuthenticatedClient(tokenB);
        var importBody = new StringContent(
            "{}",
            System.Text.Encoding.UTF8,
            "application/json");
        var importResp = await clientB.PutAsync(
            $"/api/v1/characters/{charUuidB}/import",
            importBody);

        // Should succeed (200) — empty import with no collections is valid
        Assert.That(
            importResp.StatusCode,
            Is.EqualTo(HttpStatusCode.OK),
            $"Token B got {(int)importResp.StatusCode} on import " +
            $"after Token A was rate-limited. " +
            $"Rate limiting is NOT independent per-token on import endpoint!");
    }
}
