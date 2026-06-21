// -----------------------------------------------------------------------
// <copyright file="ColonySubResourceTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Server.Auth;

#pragma warning disable SA1009 // Closing parenthesis should be followed by a space

namespace OE2EmpireTracker.Server.Tests.Endpoints.Typed;

/// <summary>
/// Unit tests for Colony sub-resource endpoints (structures, items, commodity-requests).
/// Validates: Requirements 4.8, 4.9, 4.10, 4.11, 4.12, 4.13, 4.14, 4.15.
/// </summary>
[TestFixture]
public class ColonySubResourceTests
{
    private HttpClient _ownerClient = null!;
    private string _charToken = string.Empty;
    private string _charUUID = string.Empty;
    private string _colonyUuid = string.Empty;

    /// <summary>
    /// Sets up the test server, creates a character token, and creates a colony for sub-resource tests.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        var factory = SharedTestServer.Factory;
        _ownerClient = factory.CreateAuthenticatedClient();

        var createResponse = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "ColonySubResChar" })
            .GetAwaiter().GetResult();
        var createJson = createResponse.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        _charToken = createJson.GetProperty("token").GetString()!;
        _charUUID = createJson.GetProperty("characterUUID").GetString()!;

        // Create a colony to use for sub-resource tests
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);
        var colonyResponse = charClient
            .PostAsJsonAsync(
                $"/api/v1/characters/{_charUUID}/colonies",
                new { planetName = "SubResPlanet", colonyName = "SubResColony", systemName = "Delta" })
            .GetAwaiter().GetResult();
        var colonyJson = colonyResponse.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        _colonyUuid = colonyJson.GetProperty("uuid").GetString()!;
    }

    /// <summary>
    /// Disposes the test server and clients.
    /// </summary>
    [OneTimeTearDown]
    public void TearDown()
    {
        _ownerClient.Dispose();
    }

    // ---- Structure sub-resource tests ----

    /// <summary>
    /// Adding a structure to a colony returns 200 with the updated colony.
    /// Validates: Requirement 4.8.
    /// </summary>
    [Test]
    public async Task AddStructure_ValidRequest_Returns200()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies/{_colonyUuid}/structures",
            new { flatpackBlueprintUUID = "bp-uuid-001" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(json.GetProperty("structures").GetArrayLength(), Is.GreaterThan(0));
    }

    /// <summary>
    /// Adding a structure without flatpackBlueprintUUID returns 400.
    /// Validates: Requirement 4.8.
    /// </summary>
    [Test]
    public async Task AddStructure_MissingFlatpackUUID_Returns400()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies/{_colonyUuid}/structures",
            new { flatpackBlueprintUUID = string.Empty });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// Adding a structure to a non-existent colony returns 404.
    /// Validates: Requirement 4.8.
    /// </summary>
    [Test]
    public async Task AddStructure_MissingColony_Returns404()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies/nonexistent-uuid/structures",
            new { flatpackBlueprintUUID = "bp-uuid-002" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Removing a structure from a colony returns 204.
    /// Validates: Requirement 4.9.
    /// </summary>
    [Test]
    public async Task RemoveStructure_ValidRequest_Returns204()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        // First add a structure to get its UUID
        var addResponse = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies/{_colonyUuid}/structures",
            new { flatpackBlueprintUUID = "bp-uuid-remove" });
        var addJson = await addResponse.Content.ReadFromJsonAsync<JsonElement>();
        var structures = addJson.GetProperty("structures");
        var lastStructure = structures[structures.GetArrayLength() - 1];
        var structureUuid = lastStructure.GetProperty("uuid").GetString()!;

        // Remove it
        var response = await charClient.DeleteAsync(
            $"/api/v1/characters/{_charUUID}/colonies/{_colonyUuid}/structures/{structureUuid}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    /// <summary>
    /// Removing a structure from a non-existent colony returns 404.
    /// Validates: Requirement 4.9.
    /// </summary>
    [Test]
    public async Task RemoveStructure_MissingColony_Returns404()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.DeleteAsync(
            $"/api/v1/characters/{_charUUID}/colonies/nonexistent-uuid/structures/some-uuid");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // ---- Item sub-resource tests ----

    /// <summary>
    /// Adding an item to a colony returns 200 with the updated colony.
    /// Validates: Requirement 4.10.
    /// </summary>
    [Test]
    public async Task AddItem_ValidRequest_Returns200()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies/{_colonyUuid}/items",
            new { name = "Iron Ore", quantity = 100, itemType = 0 });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// Adding an item to a non-existent colony returns 404.
    /// Validates: Requirement 4.10.
    /// </summary>
    [Test]
    public async Task AddItem_MissingColony_Returns404()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies/nonexistent-uuid/items",
            new { name = "Iron Ore", quantity = 50, itemType = 0 });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Removing an item from a colony returns 204.
    /// Validates: Requirement 4.11.
    /// </summary>
    [Test]
    public async Task RemoveItem_ValidRequest_Returns204()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        // First add an item with a known UUID
        var response = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies/{_colonyUuid}/items",
            new { uuid = "item-to-remove", name = "Copper Ore", quantity = 10, itemType = 0 });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Remove it
        var deleteResponse = await charClient.DeleteAsync(
            $"/api/v1/characters/{_charUUID}/colonies/{_colonyUuid}/items/item-to-remove");

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    /// <summary>
    /// Updating an item quantity returns 200 with the updated colony.
    /// Validates: Requirement 4.12.
    /// </summary>
    [Test]
    public async Task UpdateItem_ValidQuantity_Returns200()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        // First add an item
        var addResponse = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies/{_colonyUuid}/items",
            new { uuid = "item-to-update", name = "Gold Ore", quantity = 5, itemType = 0 });
        Assert.That(addResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Update its quantity
        var updateResponse = await charClient.PutAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies/{_colonyUuid}/items/item-to-update",
            new { quantity = 50 });

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// Updating an item with negative quantity returns 400.
    /// Validates: Requirement 4.12.
    /// </summary>
    [Test]
    public async Task UpdateItem_NegativeQuantity_Returns400()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        // First add an item
        await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies/{_colonyUuid}/items",
            new { uuid = "item-neg-qty", name = "Silver Ore", quantity = 5, itemType = 0 });

        // Update with negative quantity
        var updateResponse = await charClient.PutAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies/{_colonyUuid}/items/item-neg-qty",
            new { quantity = -1 });

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    // ---- Commodity-request sub-resource tests ----

    /// <summary>
    /// Adding a commodity request to a colony returns 200.
    /// Validates: Requirement 4.13.
    /// </summary>
    [Test]
    public async Task AddCommodityRequest_ValidRequest_Returns200()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies/{_colonyUuid}/commodity-requests",
            new { commodityName = "Steel", requested = 100 });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// Adding a commodity request without commodityName returns 400.
    /// Validates: Requirement 4.13.
    /// </summary>
    [Test]
    public async Task AddCommodityRequest_MissingCommodityName_Returns400()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies/{_colonyUuid}/commodity-requests",
            new { commodityName = string.Empty, requested = 50 });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// Adding a commodity request to a non-existent colony returns 404.
    /// Validates: Requirement 4.13.
    /// </summary>
    [Test]
    public async Task AddCommodityRequest_MissingColony_Returns404()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies/nonexistent-uuid/commodity-requests",
            new { commodityName = "Titanium", requested = 25 });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Removing a commodity request returns 204.
    /// Validates: Requirement 4.14.
    /// </summary>
    [Test]
    public async Task RemoveCommodityRequest_ValidRequest_Returns204()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        // First add a commodity request
        await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies/{_colonyUuid}/commodity-requests",
            new { commodityName = "Aluminum", requested = 75 });

        // Remove it
        var response = await charClient.DeleteAsync(
            $"/api/v1/characters/{_charUUID}/colonies/{_colonyUuid}/commodity-requests/Aluminum");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    /// <summary>
    /// Updating a commodity request returns 200.
    /// Validates: Requirement 4.15.
    /// </summary>
    [Test]
    public async Task UpdateCommodityRequest_ValidRequest_Returns200()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        // First add a commodity request
        await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies/{_colonyUuid}/commodity-requests",
            new { commodityName = "Copper", requested = 30 });

        // Update it
        var response = await charClient.PutAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies/{_colonyUuid}/commodity-requests/Copper",
            new { requested = 60, delivered = 10, fulfilled = false });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// Removing a commodity request from a non-existent colony returns 404.
    /// Validates: Requirement 4.14.
    /// </summary>
    [Test]
    public async Task RemoveCommodityRequest_MissingColony_Returns404()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.DeleteAsync(
            $"/api/v1/characters/{_charUUID}/colonies/nonexistent-uuid/commodity-requests/Steel");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
