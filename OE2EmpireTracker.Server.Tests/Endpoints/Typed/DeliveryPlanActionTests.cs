// -----------------------------------------------------------------------
// <copyright file="DeliveryPlanActionTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using NUnit.Framework;

#pragma warning disable SA1009 // Closing parenthesis should be followed by a space

namespace OE2EmpireTracker.Server.Tests.Endpoints.Typed;

/// <summary>
/// Unit tests for DeliveryPlan action endpoints (drop-off, pick-up, mark, ship).
/// Validates: Requirements 9.8, 9.9, 9.10, 9.11, 9.12, 9.13, 9.14, 9.15.
/// </summary>
[TestFixture]
public class DeliveryPlanActionTests
{
    private HttpClient _ownerClient = null!;
    private string _charToken = string.Empty;
    private string _charUUID = string.Empty;

    /// <summary>
    /// Sets up the test server with an owner token and a character token.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        var factory = SharedTestServer.Factory;
        _ownerClient = factory.CreateAuthenticatedClient();

        var createResponse = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "DeliveryActionChar" })
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
    }

    /// <summary>
    /// POST drop-off adds a drop-off item to the plan. Returns updated plan.
    /// Validates: Requirement 9.8.
    /// </summary>
    [Test]
    public async Task AddDropOff_ReturnsUpdatedPlan()
    {
        using var client = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);
        var planUuid = await CreateDeliveryPlanAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/drop-off",
            new
            {
                destInfo = new { colonyUUID = "colony-1", sequence = 1 },
                itemInfo = new { itemType = "Resource", baseItemTypeID = "iron-001", name = "Iron", quantity = 10, resourcePurity = "High" },
            });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var plan = await response.Content.ReadFromJsonAsync<JsonElement>();
        var stops = plan.GetProperty("stops");
        Assert.That(stops.GetArrayLength(), Is.GreaterThan(0));
    }

    /// <summary>
    /// POST pick-up adds a pick-up item to the plan. Returns updated plan.
    /// Validates: Requirement 9.9.
    /// </summary>
    [Test]
    public async Task AddPickUp_ReturnsUpdatedPlan()
    {
        using var client = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);
        var planUuid = await CreateDeliveryPlanAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/pick-up",
            new
            {
                destInfo = new { colonyUUID = "colony-2", sequence = 2 },
                itemInfo = new { itemType = "Resource", baseItemTypeID = "copper-001", name = "Copper", quantity = 5, resourcePurity = "Medium" },
            });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var plan = await response.Content.ReadFromJsonAsync<JsonElement>();
        var stops = plan.GetProperty("stops");
        Assert.That(stops.GetArrayLength(), Is.GreaterThan(0));
    }

    /// <summary>
    /// DELETE drop-off removes items by index. Returns updated plan.
    /// Validates: Requirement 9.10.
    /// </summary>
    [Test]
    public async Task RemoveDropOff_RemovesItemsByIndex()
    {
        using var client = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);
        var planUuid = await CreateDeliveryPlanAsync(client);

        // Add two drop-off items first
        await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/drop-off",
            new
            {
                destInfo = new { colonyUUID = "colony-rm", sequence = 1 },
                itemInfo = new { itemType = "Resource", baseItemTypeID = "iron-001", name = "Iron", quantity = 10, resourcePurity = "High" },
            });
        await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/drop-off",
            new
            {
                destInfo = new { colonyUUID = "colony-rm", sequence = 1 },
                itemInfo = new { itemType = "Resource", baseItemTypeID = "copper-001", name = "Copper", quantity = 5, resourcePurity = "Low" },
            });

        // Remove the first item (index 0)
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/drop-off");
        request.Content = JsonContent.Create(new
        {
            destInfo = new { colonyUUID = "colony-rm", sequence = 1 },
            indices = new[] { 0 },
        });
        var response = await client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var plan = await response.Content.ReadFromJsonAsync<JsonElement>();
        var stops = plan.GetProperty("stops");
        var stop = stops[0];
        var dropOff = stop.GetProperty("dropOff");
        Assert.That(dropOff.GetArrayLength(), Is.EqualTo(1));
    }

    /// <summary>
    /// DELETE pick-up removes items by index. Returns updated plan.
    /// Validates: Requirement 9.11.
    /// </summary>
    [Test]
    public async Task RemovePickUp_RemovesItemsByIndex()
    {
        using var client = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);
        var planUuid = await CreateDeliveryPlanAsync(client);

        // Add two pick-up items
        await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/pick-up",
            new
            {
                destInfo = new { colonyUUID = "colony-pk", sequence = 1 },
                itemInfo = new { itemType = "Resource", baseItemTypeID = "iron-001", name = "Iron", quantity = 10, resourcePurity = "High" },
            });
        await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/pick-up",
            new
            {
                destInfo = new { colonyUUID = "colony-pk", sequence = 1 },
                itemInfo = new { itemType = "Resource", baseItemTypeID = "copper-001", name = "Copper", quantity = 5, resourcePurity = "Low" },
            });

        // Remove the second item (index 1)
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/pick-up");
        request.Content = JsonContent.Create(new
        {
            destInfo = new { colonyUUID = "colony-pk", sequence = 1 },
            indices = new[] { 1 },
        });
        var response = await client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var plan = await response.Content.ReadFromJsonAsync<JsonElement>();
        var stops = plan.GetProperty("stops");
        var stop = stops[0];
        var pickUp = stop.GetProperty("pickUp");
        Assert.That(pickUp.GetArrayLength(), Is.EqualTo(1));
    }

    /// <summary>
    /// PUT mark-delivered marks a specific item as delivered.
    /// Validates: Requirement 9.12.
    /// </summary>
    [Test]
    public async Task MarkDelivered_MarksItemDelivered()
    {
        using var client = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);
        var planUuid = await CreateDeliveryPlanAsync(client);

        // Add a drop-off item at sequence 1
        await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/drop-off",
            new
            {
                destInfo = new { colonyUUID = "colony-md", sequence = 1 },
                itemInfo = new { itemType = "Resource", baseItemTypeID = "iron-001", name = "Iron", quantity = 10, resourcePurity = "High" },
            });

        var response = await client.PutAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/mark-delivered",
            new { stopSequence = 1, itemIndex = 0, listType = "dropOff", delivered = true });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var plan = await response.Content.ReadFromJsonAsync<JsonElement>();
        var stops = plan.GetProperty("stops");
        var stop = stops[0];
        var dropOff = stop.GetProperty("dropOff");
        var item = dropOff[0];
        Assert.That(item.GetProperty("delivered").GetBoolean(), Is.True);
    }

    /// <summary>
    /// PUT mark-delivered with pickUp list type marks a pick-up item.
    /// Validates: Requirement 9.12.
    /// </summary>
    [Test]
    public async Task MarkDelivered_PickUpListType_MarksPickUpItem()
    {
        using var client = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);
        var planUuid = await CreateDeliveryPlanAsync(client);

        // Add a pick-up item at sequence 2
        await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/pick-up",
            new
            {
                destInfo = new { colonyUUID = "colony-mp", sequence = 2 },
                itemInfo = new { itemType = "Resource", baseItemTypeID = "copper-001", name = "Copper", quantity = 5, resourcePurity = "Low" },
            });

        var response = await client.PutAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/mark-delivered",
            new { stopSequence = 2, itemIndex = 0, listType = "pickUp", delivered = true });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var plan = await response.Content.ReadFromJsonAsync<JsonElement>();
        var stops = plan.GetProperty("stops");
        var stop = stops[0];
        var pickUp = stop.GetProperty("pickUp");
        var item = pickUp[0];
        Assert.That(item.GetProperty("delivered").GetBoolean(), Is.True);
    }

    /// <summary>
    /// PUT mark-stop-complete marks a stop as completed.
    /// Validates: Requirement 9.13.
    /// </summary>
    [Test]
    public async Task MarkStopComplete_MarksStopCompleted()
    {
        using var client = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);
        var planUuid = await CreateDeliveryPlanAsync(client);

        // Add a drop-off item to create a stop at sequence 1
        await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/drop-off",
            new
            {
                destInfo = new { colonyUUID = "colony-sc", sequence = 1 },
                itemInfo = new { itemType = "Resource", baseItemTypeID = "iron-001", name = "Iron", quantity = 10, resourcePurity = "High" },
            });

        var response = await client.PutAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/mark-stop-complete",
            new { stopSequence = 1 });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var plan = await response.Content.ReadFromJsonAsync<JsonElement>();
        var stops = plan.GetProperty("stops");
        var stop = stops[0];
        Assert.That(stop.GetProperty("stopCompleted").GetBoolean(), Is.True);
    }

    /// <summary>
    /// PUT mark-complete marks the entire plan as completed.
    /// Validates: Requirement 9.14.
    /// </summary>
    [Test]
    public async Task MarkComplete_MarksPlanCompleted()
    {
        using var client = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);
        var planUuid = await CreateDeliveryPlanAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/mark-complete",
            new { });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var plan = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(plan.GetProperty("completed").GetBoolean(), Is.True);
    }

    /// <summary>
    /// PUT ship assigns a ship UUID to the plan.
    /// Validates: Requirement 9.15.
    /// </summary>
    [Test]
    public async Task AssignShip_SetsShipUUID()
    {
        using var client = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);
        var planUuid = await CreateDeliveryPlanAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/ship",
            new { shipUUID = "ship-abc-123" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var plan = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(plan.GetProperty("shipUUID").GetString(), Is.EqualTo("ship-abc-123"));
    }

    /// <summary>
    /// POST drop-off on non-existent plan returns 404.
    /// Validates: Requirement 9.8.
    /// </summary>
    [Test]
    public async Task AddDropOff_PlanNotFound_Returns404()
    {
        using var client = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/nonexistent-uuid/drop-off",
            new
            {
                destInfo = new { colonyUUID = "colony-1", sequence = 1 },
                itemInfo = new { itemType = "Resource", baseItemTypeID = "iron-001", name = "Iron", quantity = 10, resourcePurity = "High" },
            });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// PUT mark-delivered with invalid stop sequence returns 404.
    /// Validates: Requirement 9.12.
    /// </summary>
    [Test]
    public async Task MarkDelivered_InvalidStopSequence_Returns404()
    {
        using var client = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);
        var planUuid = await CreateDeliveryPlanAsync(client);

        // Add a stop at sequence 1
        await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/drop-off",
            new
            {
                destInfo = new { colonyUUID = "colony-x", sequence = 1 },
                itemInfo = new { itemType = "Resource", baseItemTypeID = "iron-001", name = "Iron", quantity = 10, resourcePurity = "High" },
            });

        // Try to mark-delivered on non-existent stop sequence 99
        var response = await client.PutAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/mark-delivered",
            new { stopSequence = 99, itemIndex = 0, listType = "dropOff", delivered = true });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Creates a delivery plan for testing and returns its UUID.
    /// </summary>
    private async Task<string> CreateDeliveryPlanAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans",
            new { name = "TestPlan", routeUUID = "route-001" });
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("uuid").GetString()!;
    }
}
