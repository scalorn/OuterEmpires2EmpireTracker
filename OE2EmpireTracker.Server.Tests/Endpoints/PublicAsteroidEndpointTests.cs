// -----------------------------------------------------------------------
// <copyright file="PublicAsteroidEndpointTests.cs" company="OE2EmpireTracker">
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
/// Integration tests for GET /api/v1/public/asteroids/{uuid}.
/// Validates correct response shape, 404 for missing asteroids,
/// private field exclusion, and anonymous access.
/// **Validates: Requirements 1.1, 1.2, 1.3, 1.4**
/// </summary>
[TestFixture]
public class PublicAsteroidEndpointTests
{
        private string _charUUID = null!;

    /// <summary>
    /// Sets up the test server and seeds an asteroid with reserves.
    /// </summary>
    [OneTimeSetUp]
    public async Task Setup()
    {
        var storage = SharedTestServer.Factory.Services.GetRequiredService<IStorageBackend>();

        _charUUID = Guid.NewGuid().ToString();
        await storage.UpsertCharacterAsync(new ServerCharacter
        {
            UUID = _charUUID,
            Name = "AsteroidTestChar",
        });

        var asteroid = new Asteroid
        {
            UUID = "ast-test-001",
            Name = "Iron Belt Alpha",
            SystemName = "TestSystem",
            Reserves = new List<AsteroidReserve>
            {
                new AsteroidReserve
                {
                    ResourceName = "Iron",
                    Purity = "High",
                    MaxReserve = 1250000,
                    CurrentReserve = 800000,
                    ResetTimestamp = "2025-01-15T12:00:00Z",
                },
                new AsteroidReserve
                {
                    ResourceName = "Copper",
                    Purity = "Medium",
                    MaxReserve = 500000,
                    CurrentReserve = 250000,
                    ResetTimestamp = "2025-01-14T08:00:00Z",
                },
            },
        };

        await storage.UpsertAsteroidAsync(_charUUID, asteroid);
    }

    /// <summary>
    /// Tears down the test server.
    /// </summary>
    [OneTimeTearDown]
    public void TearDown()
    {
    }

    /// <summary>
    /// GET /api/v1/public/asteroids/{uuid} returns 200 with correct shape
    /// including uuid, name, and reserves array with resourceName, purity, maxReserve.
    /// Validates: Requirement 1.1.
    /// </summary>
    [Test]
    public async Task GetPublicAsteroidDetail_ExistingUUID_Returns200WithCorrectShape()
    {
        using var client = SharedTestServer.Factory.CreateClient();
        var response = await client.GetAsync("/api/v1/public/asteroids/ast-test-001");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Verify top-level fields
        Assert.That(json.GetProperty("uuid").GetString(), Is.EqualTo("ast-test-001"));
        Assert.That(json.GetProperty("name").GetString(), Is.EqualTo("Iron Belt Alpha"));

        // Verify reserves array
        var reserves = json.GetProperty("reserves");
        Assert.That(reserves.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(reserves.GetArrayLength(), Is.EqualTo(2));

        // Verify first reserve entry shape
        var firstReserve = reserves[0];
        Assert.That(
            firstReserve.TryGetProperty("resourceName", out var resName),
            Is.True,
            "Reserve should have resourceName");
        Assert.That(resName.GetString(), Is.EqualTo("Iron"));

        Assert.That(
            firstReserve.TryGetProperty("purity", out var purity),
            Is.True,
            "Reserve should have purity");
        Assert.That(purity.GetString(), Is.EqualTo("High"));

        Assert.That(
            firstReserve.TryGetProperty("maxReserve", out var maxRes),
            Is.True,
            "Reserve should have maxReserve");
        Assert.That(maxRes.GetInt32(), Is.EqualTo(1250000));
    }

    /// <summary>
    /// GET /api/v1/public/asteroids/{uuid} returns 404 with error object
    /// when the UUID does not match any asteroid.
    /// Validates: Requirement 1.2.
    /// </summary>
    [Test]
    public async Task GetPublicAsteroidDetail_NonExistentUUID_Returns404()
    {
        using var client = SharedTestServer.Factory.CreateClient();
        var response = await client.GetAsync("/api/v1/public/asteroids/non-existent-uuid");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(
            json.TryGetProperty("error", out var errorProp),
            Is.True,
            "404 response should have an error field");
        Assert.That(errorProp.GetString(), Is.EqualTo("Asteroid not found"));
    }

    /// <summary>
    /// GET /api/v1/public/asteroids/{uuid} response does NOT contain
    /// CurrentReserve or ResetTimestamp fields in reserve entries.
    /// Validates: Requirement 1.3.
    /// </summary>
    [Test]
    public async Task GetPublicAsteroidDetail_ResponseExcludesPrivateFields()
    {
        using var client = SharedTestServer.Factory.CreateClient();
        var response = await client.GetAsync("/api/v1/public/asteroids/ast-test-001");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var reserves = json.GetProperty("reserves");

        foreach (var reserve in reserves.EnumerateArray())
        {
            Assert.That(
                reserve.TryGetProperty("currentReserve", out _),
                Is.False,
                "Reserve must not expose currentReserve");
            Assert.That(
                reserve.TryGetProperty("resetTimestamp", out _),
                Is.False,
                "Reserve must not expose resetTimestamp");
        }
    }

    /// <summary>
    /// GET /api/v1/public/asteroids/{uuid} is accessible without an auth token.
    /// Validates: Requirement 1.4.
    /// </summary>
    [Test]
    public async Task GetPublicAsteroidDetail_AccessibleWithoutAuthToken()
    {
        // CreateClient() does NOT add any auth headers
        using var client = SharedTestServer.Factory.CreateClient();
        var response = await client.GetAsync("/api/v1/public/asteroids/ast-test-001");

        // Should succeed without auth — not 401 or 403
        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// GET /api/v1/public/asteroids/{uuid} performs case-insensitive UUID matching.
    /// Validates: Requirement 1.1 (endpoint searches by UUID case-insensitively).
    /// </summary>
    [Test]
    public async Task GetPublicAsteroidDetail_CaseInsensitiveUUIDMatch()
    {
        using var client = SharedTestServer.Factory.CreateClient();
        var response = await client.GetAsync("/api/v1/public/asteroids/AST-TEST-001");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(json.GetProperty("uuid").GetString(), Is.EqualTo("ast-test-001"));
    }
}
