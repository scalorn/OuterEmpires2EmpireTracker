// -----------------------------------------------------------------------
// <copyright file="PreservedEndpointTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Integration tests verifying that preserved endpoints (export, sync, global)
/// continue to work after raw data endpoint removal.
/// Validates: Req 1 Criterion 11; Req 10 Criterion 1; Req 11, Criteria 1-3.
/// </summary>
[TestFixture]
public class PreservedEndpointTests
{
    private TestServerFactory _factory = null!;
    private HttpClient _ownerClient = null!;
    private string _charUUID = string.Empty;
    private string _charToken = string.Empty;

    /// <summary>
    /// Sets up the test server with an owner token and a character for testing.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestServerFactory();
        _factory.SeedOwnerToken();
        _ownerClient = _factory.CreateAuthenticatedClient();

        // Create a character with its own token
        var createResponse = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "PreservedTestChar" })
            .GetAwaiter().GetResult();
        var createJson = createResponse.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        _charUUID = createJson.GetProperty("characterUUID").GetString()!;
        _charToken = createJson.GetProperty("token").GetString()!;
    }

    /// <summary>
    /// Tears down the test server and disposes HTTP clients.
    /// </summary>
    [OneTimeTearDown]
    public void TearDown()
    {
        _ownerClient.Dispose();
        _factory.Dispose();
    }


    /// <summary>
    /// GET /api/v1/characters/{uuid}/export returns 200 with assembled character data.
    /// The response contains entity collection properties from typed storage.
    /// Validates: Req 10, Criterion 1.
    /// </summary>
    [Test]
    public async Task GetExport_ReturnsAssembledCharacterData()
    {
        var response = await _ownerClient.GetAsync($"/api/v1/characters/{_charUUID}/export");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Verify the response contains entity collection keys
        Assert.That(body.TryGetProperty("colony", out _), Is.True, "Missing 'colony' key");
        Assert.That(body.TryGetProperty("blueprint", out _), Is.True, "Missing 'blueprint' key");
        Assert.That(body.TryGetProperty("survey", out _), Is.True, "Missing 'survey' key");
        Assert.That(body.TryGetProperty("playerProfile", out _), Is.True, "Missing 'playerProfile' key");
        Assert.That(body.TryGetProperty("deliveryRoute", out _), Is.True, "Missing 'deliveryRoute' key");
        Assert.That(body.TryGetProperty("deliveryPlan", out _), Is.True, "Missing 'deliveryPlan' key");
        Assert.That(body.TryGetProperty("ship", out _), Is.True, "Missing 'ship' key");
        Assert.That(body.TryGetProperty("shipTemplate", out _), Is.True, "Missing 'shipTemplate' key");
        Assert.That(body.TryGetProperty("marketListing", out _), Is.True, "Missing 'marketListing' key");
        Assert.That(body.TryGetProperty("marketTransaction", out _), Is.True, "Missing 'marketTransaction' key");
        Assert.That(body.TryGetProperty("pricingPlan", out _), Is.True, "Missing 'pricingPlan' key");
        Assert.That(body.TryGetProperty("stockPlan", out _), Is.True, "Missing 'stockPlan' key");
        Assert.That(body.TryGetProperty("stockProfile", out _), Is.True, "Missing 'stockProfile' key");
        Assert.That(body.TryGetProperty("buildPlan", out _), Is.True, "Missing 'buildPlan' key");
        Assert.That(body.TryGetProperty("supplyChain", out _), Is.True, "Missing 'supplyChain' key");
        Assert.That(body.TryGetProperty("asteroid", out _), Is.True, "Missing 'asteroid' key");
        Assert.That(body.TryGetProperty("station", out _), Is.True, "Missing 'station' key");
        Assert.That(body.TryGetProperty("faction", out _), Is.True, "Missing 'faction' key");
        Assert.That(body.TryGetProperty("externalCharacter", out _), Is.True, "Missing 'externalCharacter' key");
    }

    /// <summary>
    /// GET /api/v1/sync returns 200 with factions, characters, and serverTimestamp.
    /// Validates: Req 11, Criterion 1.
    /// </summary>
    [Test]
    public async Task GetSync_ReturnsFilteredData()
    {
        var response = await _ownerClient.GetAsync("/api/v1/sync");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Verify the response has the expected structure
        Assert.That(body.TryGetProperty("factions", out var factions), Is.True, "Missing 'factions' key");
        Assert.That(body.TryGetProperty("characters", out var characters), Is.True, "Missing 'characters' key");
        Assert.That(body.TryGetProperty("serverTimestamp", out var timestamp), Is.True, "Missing 'serverTimestamp' key");

        // Verify types
        Assert.That(factions.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(characters.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(timestamp.ValueKind, Is.EqualTo(JsonValueKind.String));

        // Owner should see the character we created
        var charUUIDs = Enumerable.Range(0, characters.GetArrayLength())
            .Select(i => characters[i].GetProperty("uuid").GetString())
            .ToList();
        Assert.That(charUUIDs, Does.Contain(_charUUID));
    }


    /// <summary>
    /// GET /api/v1/global/{dataType} returns data or 404 if not seeded.
    /// Validates: Req 11, Criterion 2.
    /// </summary>
    [Test]
    public async Task GetGlobalData_Works()
    {
        // First seed some global data via PUT
        var testData = JsonSerializer.Serialize(new { items = new[] { "item1", "item2" } });
        var putContent = new StringContent(testData, Encoding.UTF8, "application/json");
        var putResponse = await _ownerClient.PutAsync("/api/v1/global/testDataType", putContent);
        Assert.That(putResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Now GET should return the seeded data
        var getResponse = await _ownerClient.GetAsync("/api/v1/global/testDataType");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.TryGetProperty("items", out var items), Is.True);
        Assert.That(items.GetArrayLength(), Is.EqualTo(2));
    }

    /// <summary>
    /// GET /api/v1/global/{dataType} returns 404 for non-existent data type.
    /// Validates: Req 11, Criterion 2.
    /// </summary>
    [Test]
    public async Task GetGlobalData_NonExistent_Returns404()
    {
        var response = await _ownerClient.GetAsync("/api/v1/global/nonExistentType");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// PUT /api/v1/global/{dataType} with Owner token returns 200.
    /// Validates: Req 11, Criterion 3.
    /// </summary>
    [Test]
    public async Task PutGlobalData_OwnerOnly_Works()
    {
        var testData = JsonSerializer.Serialize(new { version = 1, data = "test" });
        var content = new StringContent(testData, Encoding.UTF8, "application/json");

        var response = await _ownerClient.PutAsync("/api/v1/global/ownerTestType", content);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("version").GetInt32(), Is.EqualTo(1));
        Assert.That(body.GetProperty("data").GetString(), Is.EqualTo("test"));
    }

    /// <summary>
    /// PUT /api/v1/global/{dataType} with non-Owner token returns 403.
    /// Validates: Req 11, Criterion 3 (Owner only).
    /// </summary>
    [Test]
    public async Task PutGlobalData_NonOwner_Returns403()
    {
        using var charClient = _factory.CreateAuthenticatedClient(_charToken);
        var testData = JsonSerializer.Serialize(new { data = "should-fail" });
        var content = new StringContent(testData, Encoding.UTF8, "application/json");

        var response = await charClient.PutAsync("/api/v1/global/forbiddenType", content);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }
}
