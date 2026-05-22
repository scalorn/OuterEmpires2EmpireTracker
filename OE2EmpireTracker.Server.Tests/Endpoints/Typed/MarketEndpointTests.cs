// -----------------------------------------------------------------------
// <copyright file="MarketEndpointTests.cs" company="OE2EmpireTracker">
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
/// Unit tests for MarketListing record-sale and MarketTransaction endpoints.
/// Validates: Requirements 12.8, 13.5, 13.6, 32.1, 32.2, 32.3.
/// </summary>
[TestFixture]
public class MarketEndpointTests
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
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "MarketTestChar" })
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
    /// Record-sale on an existing listing creates a transaction and returns 201.
    /// Validates: Requirement 12.8.
    /// </summary>
    [Test]
    public async Task RecordSale_ValidRequest_Returns201WithTransaction()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);
        var listingUuid = await CreateMarketListingAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/market-listings/{listingUuid}/record-sale",
            new { quantity = 10, pricePerUnit = 5.5m });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("uuid").GetString(), Is.Not.Null.And.Not.Empty);
        Assert.That(body.GetProperty("quantity").GetInt32(), Is.EqualTo(10));
        Assert.That(body.GetProperty("pricePerUnit").GetDecimal(), Is.EqualTo(5.5m));
        Assert.That(body.GetProperty("totalPrice").GetDecimal(), Is.EqualTo(55m));
        Assert.That(body.GetProperty("itemName").GetString(), Is.EqualTo("Iron Ore"));
    }

    /// <summary>
    /// Record-sale with zero quantity returns 422.
    /// Validates: Requirement 12.8.
    /// </summary>
    [Test]
    public async Task RecordSale_ZeroQuantity_Returns422()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);
        var listingUuid = await CreateMarketListingAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/market-listings/{listingUuid}/record-sale",
            new { quantity = 0, pricePerUnit = 5.0m });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
    }

    /// <summary>
    /// Record-sale with zero price returns 422.
    /// Validates: Requirement 12.8.
    /// </summary>
    [Test]
    public async Task RecordSale_ZeroPrice_Returns422()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);
        var listingUuid = await CreateMarketListingAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/market-listings/{listingUuid}/record-sale",
            new { quantity = 5, pricePerUnit = 0m });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
    }

    /// <summary>
    /// Record-sale on a non-existent listing returns 404.
    /// Validates: Requirement 12.8.
    /// </summary>
    [Test]
    public async Task RecordSale_ListingNotFound_Returns404()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/market-listings/nonexistent-uuid/record-sale",
            new { quantity = 10, pricePerUnit = 5.0m });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Record-purchase with valid data creates a transaction and returns 201.
    /// Validates: Requirements 13.5, 32.1.
    /// </summary>
    [Test]
    public async Task RecordPurchase_ValidRequest_Returns201()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/market-transactions/record-purchase",
            new
            {
                itemName = "Copper Ore",
                quantity = 20,
                pricePerUnit = 3.0m,
                counterparty = "TraderBob",
                stationUUID = "station-002",
            });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("uuid").GetString(), Is.Not.Null.And.Not.Empty);
        Assert.That(body.GetProperty("itemName").GetString(), Is.EqualTo("Copper Ore"));
        Assert.That(body.GetProperty("quantity").GetInt32(), Is.EqualTo(20));
        Assert.That(body.GetProperty("pricePerUnit").GetDecimal(), Is.EqualTo(3.0m));
        Assert.That(body.GetProperty("totalPrice").GetDecimal(), Is.EqualTo(60m));
    }

    /// <summary>
    /// Record-purchase with missing itemName returns 400.
    /// Validates: Requirement 32.1.
    /// </summary>
    [Test]
    public async Task RecordPurchase_MissingItemName_Returns400()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/market-transactions/record-purchase",
            new { quantity = 10, pricePerUnit = 2.0m });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// Record-purchase with zero quantity returns 400.
    /// Validates: Requirement 32.1.
    /// </summary>
    [Test]
    public async Task RecordPurchase_ZeroQuantity_Returns400()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/market-transactions/record-purchase",
            new { itemName = "Iron Ore", quantity = 0, pricePerUnit = 2.0m });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// Profit-loss endpoint returns computed summary with sales and purchases.
    /// Validates: Requirements 13.6, 32.2.
    /// </summary>
    [Test]
    public async Task ProfitLoss_WithTransactions_ReturnsComputedSummary()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);

        // Create a listing and record a sale
        var listingUuid = await CreateMarketListingAsync(client);
        await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/market-listings/{listingUuid}/record-sale",
            new { quantity = 10, pricePerUnit = 8.0m });

        // Record a purchase
        await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/market-transactions/record-purchase",
            new { itemName = "Iron Ore", quantity = 5, pricePerUnit = 3.0m });

        // Get profit-loss
        var response = await client.GetAsync(
            $"/api/v1/characters/{_charUUID}/market-transactions/profit-loss");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("totalSalesRevenue").GetDecimal(), Is.EqualTo(80m));
        Assert.That(body.GetProperty("totalPurchaseCost").GetDecimal(), Is.EqualTo(15m));
        Assert.That(body.GetProperty("netProfitLoss").GetDecimal(), Is.EqualTo(65m));
    }

    /// <summary>
    /// Profit-loss with invalid startDate returns 400.
    /// Validates: Requirement 32.3.
    /// </summary>
    [Test]
    public async Task ProfitLoss_InvalidStartDate_Returns400()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);

        var response = await client.GetAsync(
            $"/api/v1/characters/{_charUUID}/market-transactions/profit-loss?startDate=not-a-date");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("error").GetString(), Is.EqualTo("Invalid date format"));
    }

    /// <summary>
    /// Profit-loss with invalid endDate returns 400.
    /// Validates: Requirement 32.3.
    /// </summary>
    [Test]
    public async Task ProfitLoss_InvalidEndDate_Returns400()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);

        var response = await client.GetAsync(
            $"/api/v1/characters/{_charUUID}/market-transactions/profit-loss?endDate=invalid");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("error").GetString(), Is.EqualTo("Invalid date format"));
    }

    /// <summary>
    /// Creates a market listing for testing and returns its UUID.
    /// </summary>
    private async Task<string> CreateMarketListingAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/market-listings",
            new { itemName = "Iron Ore", stationUUID = "station-001" });
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("uuid").GetString()!;
    }
}
