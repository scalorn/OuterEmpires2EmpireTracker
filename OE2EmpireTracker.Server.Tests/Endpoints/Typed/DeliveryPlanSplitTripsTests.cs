// -----------------------------------------------------------------------
// <copyright file="DeliveryPlanSplitTripsTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;

#pragma warning disable SA1009 // Closing parenthesis should be followed by a space

namespace OE2EmpireTracker.Server.Tests.Endpoints.Typed;

/// <summary>
/// Unit tests for the DeliveryPlan split-trips endpoint.
/// Validates: Requirements 9.16, 30.1, 30.2, 30.3, 30.4.
/// </summary>
[TestFixture]
public class DeliveryPlanSplitTripsTests
{
    private TestServerFactory _factory = null!;
    private HttpClient _ownerClient = null!;
    private string _charToken = string.Empty;
    private string _charUUID = string.Empty;

    /// <summary>
    /// Sets up the test server with an owner token and a character token.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestServerFactory();
        _factory.SeedOwnerToken();
        _ownerClient = _factory.CreateAuthenticatedClient();

        var createResponse = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "SplitTripsChar" })
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
    /// Creates a delivery plan with drop-off items for split testing.
    /// </summary>
    private async Task<string> CreatePlanWithItemsAsync(HttpClient client, int itemCount, int quantityPerItem)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans",
            new { name = "SplitTestPlan", routeUUID = "route-split-001" });
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var planUuid = json.GetProperty("uuid").GetString()!;

        for (int i = 0; i < itemCount; i++)
        {
            await client.PostAsJsonAsync(
                $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/drop-off",
                new
                {
                    destInfo = new { colonyUUID = "colony-split", sequence = 1 },
                    itemInfo = new
                    {
                        itemType = "Resource",
                        baseItemTypeID = $"item-{i:D3}",
                        name = $"Item{i}",
                        quantity = quantityPerItem,
                        resourcePurity = "High",
                    },
                });
        }

        return planUuid;
    }

    /// <summary>
    /// Split-trips creates multiple plans when items exceed cargo capacity.
    /// Validates: Requirements 9.16, 30.1.
    /// </summary>
    [Test]
    public async Task SplitTrips_CreatesMultiplePlans()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);

        // Create plan with 3 items of quantity 10 each (total volume = 30)
        var planUuid = await CreatePlanWithItemsAsync(client, 3, 10);

        // Split with capacity 15 — should create 2 trips (10+10 won't fit, so 10 | 10 | 10 → 10 | 10+10 won't fit → trip1=10, trip2=10, trip3=10... actually 10+10=20>15, so each item is its own trip? No: 10<=15 fits, then 10+10=20>15 so trip1=[item0], trip2=[item1], trip3=[item2])
        // Actually: item0 qty=10 fits (10<=15), item1 qty=10 → 10+10=20>15 → new trip, item2 qty=10 → 10<=15 fits... so trip1=[item0], trip2=[item1], trip3=[item2]? No: after starting new trip for item1, currentVolume=10, then item2: 10+10=20>15 → new trip. So 3 trips.
        // Let's use capacity 25 instead: item0=10 fits, item1=10 → 20<=25 fits, item2=10 → 30>25 → new trip. So 2 trips.
        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/split-trips",
            new { cargoCapacity = 25 });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var plans = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(plans.GetArrayLength(), Is.EqualTo(2));
    }

    /// <summary>
    /// Original plan remains unchanged after split-trips.
    /// Validates: Requirement 30.2.
    /// </summary>
    [Test]
    public async Task SplitTrips_OriginalPlanUnchanged()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);
        var planUuid = await CreatePlanWithItemsAsync(client, 3, 10);

        // Get original plan state
        var originalResponse = await client.GetAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}");
        var originalPlan = await originalResponse.Content.ReadFromJsonAsync<JsonElement>();
        var originalStops = originalPlan.GetProperty("stops").GetArrayLength();

        // Split
        await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/split-trips",
            new { cargoCapacity = 15 });

        // Verify original is unchanged
        var afterResponse = await client.GetAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}");
        var afterPlan = await afterResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(afterPlan.GetProperty("stops").GetArrayLength(), Is.EqualTo(originalStops));
        Assert.That(afterPlan.GetProperty("name").GetString(), Is.EqualTo("SplitTestPlan"));
    }


    /// <summary>
    /// Split-trips with missing cargoCapacity (0) returns 400.
    /// Validates: Requirement 30.3.
    /// </summary>
    [Test]
    public async Task SplitTrips_MissingCargoCapacity_Returns400()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);
        var planUuid = await CreatePlanWithItemsAsync(client, 2, 10);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/split-trips",
            new { cargoCapacity = 0 });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("error").GetString(), Does.Contain("cargoCapacity"));
    }

    /// <summary>
    /// Split-trips with negative cargoCapacity returns 400.
    /// Validates: Requirement 30.4.
    /// </summary>
    [Test]
    public async Task SplitTrips_NegativeCargoCapacity_Returns400()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);
        var planUuid = await CreatePlanWithItemsAsync(client, 2, 10);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/split-trips",
            new { cargoCapacity = -5 });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// Split-trips on non-existent plan returns 404.
    /// Validates: Requirement 9.16.
    /// </summary>
    [Test]
    public async Task SplitTrips_PlanNotFound_Returns404()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/nonexistent-uuid/split-trips",
            new { cargoCapacity = 100 });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Split-trips with capacity larger than total volume creates a single plan.
    /// Validates: Requirement 30.1.
    /// </summary>
    [Test]
    public async Task SplitTrips_LargeCapacity_CreatesSinglePlan()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);

        // 3 items × 10 qty = 30 total volume
        var planUuid = await CreatePlanWithItemsAsync(client, 3, 10);

        // Capacity 100 > 30, so everything fits in one trip
        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/split-trips",
            new { cargoCapacity = 100 });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var plans = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(plans.GetArrayLength(), Is.EqualTo(1));
    }

    /// <summary>
    /// Split-trips on a plan with no items returns empty list.
    /// Validates: Requirement 30.1.
    /// </summary>
    [Test]
    public async Task SplitTrips_NoItems_ReturnsEmptyList()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);

        // Create plan with no items
        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans",
            new { name = "EmptyPlan", routeUUID = "route-empty" });
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var planUuid = json.GetProperty("uuid").GetString()!;

        var splitResponse = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/delivery-plans/{planUuid}/split-trips",
            new { cargoCapacity = 50 });

        Assert.That(splitResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var plans = await splitResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(plans.GetArrayLength(), Is.EqualTo(0));
    }
}
