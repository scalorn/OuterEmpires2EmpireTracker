// -----------------------------------------------------------------------
// <copyright file="BulkImportValidRequestTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

#pragma warning disable SA1009 // Closing parenthesis should be followed by a space

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Unit tests for the bulk import endpoint — valid request path.
/// Validates: Req 2, Criteria 4, 6, 9.
/// </summary>
[TestFixture]
public class BulkImportValidRequestTests
{
    private TestServerFactory _factory = null!;
    private HttpClient _ownerClient = null!;
    private string _charToken = string.Empty;
    private string _charUUID = string.Empty;

    /// <summary>
    /// Sets up the test server and creates a character token for import tests.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestServerFactory();
        _factory.SeedOwnerToken();
        _ownerClient = _factory.CreateAuthenticatedClient();

        var createResponse = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "BulkImportTestChar" })
            .GetAwaiter().GetResult();
        var createJson = createResponse.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        _charToken = createJson.GetProperty("token").GetString()!;
        _charUUID = createJson.GetProperty("characterUUID").GetString()!;
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
    /// A valid PlayerRoot with colonies and blueprints returns HTTP 200
    /// with correct imported counts in the response.
    /// </summary>
    [Test]
    public async Task HandleImport_ValidPlayerRoot_Returns200WithCounts()
    {
        using var charClient = _factory.CreateAuthenticatedClient(_charToken);

        var payload = new
        {
            DataVersion = 1,
            CurrentPlayerUUID = _charUUID,
            Colony = new[]
            {
                new
                {
                    UUID = "colony-001",
                    OwnerUUID = _charUUID,
                    PlanetName = "Earth",
                    ColonyName = "Alpha Base",
                },
                new
                {
                    UUID = "colony-002",
                    OwnerUUID = _charUUID,
                    PlanetName = "Mars",
                    ColonyName = "Red Outpost",
                },
            },
            Blueprint = new[]
            {
                new
                {
                    UUID = "bp-001",
                    OwnerUUID = _charUUID,
                    Name = "Laser Mk1",
                    BluePrintType = "Weapon",
                },
            },
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await charClient.PutAsync(
            $"/api/v1/characters/{_charUUID}/import", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var imported = result.GetProperty("imported");
        Assert.That(imported.GetProperty("colonies").GetInt32(), Is.EqualTo(2));
        Assert.That(imported.GetProperty("blueprints").GetInt32(), Is.EqualTo(1));
        Assert.That(result.GetProperty("total").GetInt32(), Is.EqualTo(3));
    }

    /// <summary>
    /// A PlayerRoot with all null/empty collections returns HTTP 200
    /// with total=0 and an empty imported dictionary.
    /// </summary>
    [Test]
    public async Task HandleImport_EmptyCollections_Returns200WithZeroCounts()
    {
        using var charClient = _factory.CreateAuthenticatedClient(_charToken);

        var payload = new
        {
            DataVersion = 1,
            CurrentPlayerUUID = _charUUID,
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await charClient.PutAsync(
            $"/api/v1/characters/{_charUUID}/import", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(result.GetProperty("total").GetInt32(), Is.EqualTo(0));

        var imported = result.GetProperty("imported");
        Assert.That(imported.EnumerateObject().Count(), Is.EqualTo(0));
    }

    /// <summary>
    /// A JSON body with an unrecognized top-level collection key is ignored
    /// and does not cause an error. System.Text.Json ignores unknown properties by default.
    /// </summary>
    [Test]
    public async Task HandleImport_UnrecognizedCollectionKey_Ignored()
    {
        using var charClient = _factory.CreateAuthenticatedClient(_charToken);

        var payload = new
        {
            DataVersion = 1,
            CurrentPlayerUUID = _charUUID,
            Colony = new[]
            {
                new
                {
                    UUID = "colony-unk-001",
                    OwnerUUID = _charUUID,
                    PlanetName = "Venus",
                    ColonyName = "Cloud City",
                },
            },
            UnknownStuff = new[]
            {
                new { Id = "x1", Value = "something" },
                new { Id = "x2", Value = "else" },
            },
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await charClient.PutAsync(
            $"/api/v1/characters/{_charUUID}/import", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var imported = result.GetProperty("imported");
        Assert.That(imported.GetProperty("colonies").GetInt32(), Is.EqualTo(1));
        Assert.That(result.GetProperty("total").GetInt32(), Is.EqualTo(1));
    }
}
