// -----------------------------------------------------------------------
// <copyright file="BulkCoexistenceTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

#pragma warning disable SA1009 // Closing parenthesis should be followed by a space

namespace OE2EmpireTracker.Server.Tests.Endpoints.Typed;

/// <summary>
/// Integration tests verifying that typed endpoints and bulk endpoints
/// coexist without conflict — data written via one is visible via the other.
/// Validates: Requirements 24.1, 24.2, 24.3, 24.4, 24.5.
/// </summary>
[TestFixture]
public class BulkCoexistenceTests
{
    private TestServerFactory _factory = null!;
    private HttpClient _ownerClient = null!;
    private string _charToken = string.Empty;
    private string _charUUID = string.Empty;

    /// <summary>
    /// Sets up the test server and creates a character token.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestServerFactory();
        _factory.SeedOwnerToken();
        _ownerClient = _factory.CreateAuthenticatedClient();

        var createResponse = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "CoexistChar" })
            .GetAwaiter().GetResult();
        var createJson = createResponse.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        _charToken = createJson.GetProperty("token").GetString()!;
        _charUUID = createJson.GetProperty("characterUUID").GetString()!;
    }

    /// <summary>
    /// Disposes the test server and clients.
    /// </summary>
    [OneTimeTearDown]
    public void TearDown()
    {
        _ownerClient.Dispose();
        _factory.Dispose();
    }

    /// <summary>
    /// Data written via typed POST endpoint is visible via bulk GET.
    /// Validates: Requirement 24.1.
    /// </summary>
    [Test]
    public async Task TypedWrite_VisibleViaBulkGet()
    {
        using var charClient = _factory.CreateAuthenticatedClient(_charToken);

        // Write via typed endpoint
        var createResponse = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/asteroids",
            new { name = "BulkTestAsteroid", systemName = "Alpha" });
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var asteroidUuid = created.GetProperty("uuid").GetString()!;

        // Read via bulk GET
        var bulkResponse = await charClient.GetAsync(
            $"/api/v1/characters/{_charUUID}/data");
        Assert.That(bulkResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var bulkJson = await bulkResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(bulkJson);
        var root = doc.RootElement;

        // The bulk endpoint returns all data keyed by data type
        Assert.That(root.TryGetProperty("asteroids", out var asteroidsElement), Is.True);
        var asteroids = asteroidsElement.EnumerateArray().ToList();
        var match = asteroids.FirstOrDefault(a =>
            a.GetProperty("UUID").GetString() == asteroidUuid);
        Assert.That(match.ValueKind, Is.Not.EqualTo(JsonValueKind.Undefined));
    }

    /// <summary>
    /// Data written via bulk PUT is visible via typed GET.
    /// Validates: Requirement 24.2.
    /// </summary>
    [Test]
    public async Task BulkWrite_VisibleViaTypedGet()
    {
        using var charClient = _factory.CreateAuthenticatedClient(_charToken);

        // Write via bulk PUT — put an asteroid into the character's data
        var asteroidUuid = Guid.NewGuid().ToString();
        var bulkPayload = JsonSerializer.Serialize(new
        {
            asteroids = new[]
            {
                new
                {
                    UUID = asteroidUuid,
                    Name = "BulkWrittenAsteroid",
                    SystemName = "Beta",
                },
            },
        });
        var putContent = new StringContent(bulkPayload, Encoding.UTF8, "application/json");
        var putResponse = await charClient.PutAsync(
            $"/api/v1/characters/{_charUUID}/data", putContent);
        Assert.That(putResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Read via typed GET
        var typedResponse = await charClient.GetAsync(
            $"/api/v1/characters/{_charUUID}/asteroids/{asteroidUuid}");
        Assert.That(typedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var asteroid = await typedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(asteroid.GetProperty("name").GetString(), Is.EqualTo("BulkWrittenAsteroid"));
    }

    /// <summary>
    /// Both endpoints coexist without conflict — typed and bulk can be used
    /// in sequence without data corruption.
    /// Validates: Requirements 24.3, 24.4, 24.5.
    /// </summary>
    [Test]
    public async Task BothEndpoints_CoexistWithoutConflict()
    {
        using var charClient = _factory.CreateAuthenticatedClient(_charToken);

        // Create via typed endpoint
        var createResponse = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/asteroids",
            new { name = "TypedAsteroid", systemName = "Gamma" });
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var typedUuid = created.GetProperty("uuid").GetString()!;

        // Create via bulk PUT (adds a second asteroid)
        var bulkUuid = Guid.NewGuid().ToString();
        var bulkPayload = JsonSerializer.Serialize(new
        {
            asteroids = new[]
            {
                new { UUID = typedUuid, Name = "TypedAsteroid", SystemName = "Gamma" },
                new { UUID = bulkUuid, Name = "BulkAsteroid", SystemName = "Delta" },
            },
        });
        var putContent = new StringContent(bulkPayload, Encoding.UTF8, "application/json");
        var putResponse = await charClient.PutAsync(
            $"/api/v1/characters/{_charUUID}/data", putContent);
        Assert.That(putResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Both should be visible via typed GET all
        var allResponse = await charClient.GetAsync(
            $"/api/v1/characters/{_charUUID}/asteroids");
        Assert.That(allResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var allAsteroids = await allResponse.Content.ReadFromJsonAsync<JsonElement>();
        var uuids = Enumerable.Range(0, allAsteroids.GetArrayLength())
            .Select(i => allAsteroids[i].GetProperty("uuid").GetString())
            .ToList();
        Assert.That(uuids, Does.Contain(typedUuid));
        Assert.That(uuids, Does.Contain(bulkUuid));
    }
}
