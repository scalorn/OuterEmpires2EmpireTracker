// -----------------------------------------------------------------------
// <copyright file="PropertyTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Endpoints.Typed;
using OE2EmpireTracker.Server.Tests;

#pragma warning disable SA1009 // Closing parenthesis should be followed by a space

namespace OE2EmpireTracker.Server.Tests.Endpoints.Typed;

/// <summary>
/// Property-based tests using randomized iteration loops.
/// Validates correctness properties from the design document.
/// </summary>
[TestFixture]
public class PropertyTests
{
    private static readonly JsonSerializerOptions StjOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>
    /// Round-trip serialization integrity for Asteroid model.
    /// Serialize to JSON with STJ config, deserialize back, verify equivalent field values.
    /// **Validates: Requirements 33.1, 33.3, 33.4**
    /// </summary>
    [Test]
    public void RoundTrip_Asteroid_PreservesFields()
    {
        var rng = new Random(42);

        for (int i = 0; i < 20; i++)
        {
            var original = CreateRandomAsteroid(rng);

            var json = JsonSerializer.Serialize(original, StjOptions);
            var deserialized = JsonSerializer.Deserialize<Asteroid>(json, StjOptions);

            Assert.That(deserialized, Is.Not.Null, $"Iteration {i}: deserialized was null");
            Assert.That(deserialized!.UUID, Is.EqualTo(original.UUID), $"Iteration {i}: UUID mismatch");
            Assert.That(deserialized.Name, Is.EqualTo(original.Name), $"Iteration {i}: Name mismatch");
            Assert.That(deserialized.SystemName, Is.EqualTo(original.SystemName), $"Iteration {i}: SystemName mismatch");
            Assert.That(deserialized.Reserves, Has.Count.EqualTo(original.Reserves.Count), $"Iteration {i}: Reserves count mismatch");

            for (int r = 0; r < original.Reserves.Count; r++)
            {
                Assert.That(deserialized.Reserves[r].ResourceName, Is.EqualTo(original.Reserves[r].ResourceName));
                Assert.That(deserialized.Reserves[r].Purity, Is.EqualTo(original.Reserves[r].Purity));
                Assert.That(deserialized.Reserves[r].MaxReserve, Is.EqualTo(original.Reserves[r].MaxReserve));
                Assert.That(deserialized.Reserves[r].CurrentReserve, Is.EqualTo(original.Reserves[r].CurrentReserve));
            }
        }
    }

    /// <summary>
    /// Round-trip serialization integrity for Ship model.
    /// **Validates: Requirements 33.1, 33.3, 33.4**
    /// </summary>
    [Test]
    public void RoundTrip_Ship_PreservesFields()
    {
        var rng = new Random(123);

        for (int i = 0; i < 20; i++)
        {
            var original = CreateRandomShip(rng);

            var json = JsonSerializer.Serialize(original, StjOptions);
            var deserialized = JsonSerializer.Deserialize<Ship>(json, StjOptions);

            Assert.That(deserialized, Is.Not.Null, $"Iteration {i}: deserialized was null");
            Assert.That(deserialized!.UUID, Is.EqualTo(original.UUID), $"Iteration {i}: UUID mismatch");
            Assert.That(deserialized.Name, Is.EqualTo(original.Name), $"Iteration {i}: Name mismatch");
            Assert.That(deserialized.OwnerUUID, Is.EqualTo(original.OwnerUUID), $"Iteration {i}: OwnerUUID mismatch");
            Assert.That(deserialized.TemplateUUID, Is.EqualTo(original.TemplateUUID), $"Iteration {i}: TemplateUUID mismatch");
            Assert.That(deserialized.LocationType, Is.EqualTo(original.LocationType), $"Iteration {i}: LocationType mismatch");
            Assert.That(deserialized.LocationUUID, Is.EqualTo(original.LocationUUID), $"Iteration {i}: LocationUUID mismatch");
            Assert.That(deserialized.HullCurrentHP, Is.EqualTo(original.HullCurrentHP), $"Iteration {i}: HullCurrentHP mismatch");
            Assert.That(deserialized.HullMaxHP, Is.EqualTo(original.HullMaxHP), $"Iteration {i}: HullMaxHP mismatch");
            Assert.That(deserialized.HullMaxRepairPercent, Is.EqualTo(original.HullMaxRepairPercent), $"Iteration {i}: HullMaxRepairPercent mismatch");
        }
    }

    /// <summary>
    /// Round-trip serialization integrity for PricingPlan model.
    /// **Validates: Requirements 33.1, 33.3, 33.4**
    /// </summary>
    [Test]
    public void RoundTrip_PricingPlan_PreservesFields()
    {
        var rng = new Random(777);

        for (int i = 0; i < 20; i++)
        {
            var original = CreateRandomPricingPlan(rng);

            var json = JsonSerializer.Serialize(original, StjOptions);
            var deserialized = JsonSerializer.Deserialize<PricingPlan>(json, StjOptions);

            Assert.That(deserialized, Is.Not.Null, $"Iteration {i}: deserialized was null");
            Assert.That(deserialized!.UUID, Is.EqualTo(original.UUID), $"Iteration {i}: UUID mismatch");
            Assert.That(deserialized.Name, Is.EqualTo(original.Name), $"Iteration {i}: Name mismatch");
            Assert.That(deserialized.OwnerUUID, Is.EqualTo(original.OwnerUUID), $"Iteration {i}: OwnerUUID mismatch");
            Assert.That(deserialized.Description, Is.EqualTo(original.Description), $"Iteration {i}: Description mismatch");
            Assert.That(deserialized.FixedCostPerItem, Is.EqualTo(original.FixedCostPerItem), $"Iteration {i}: FixedCostPerItem mismatch");
            Assert.That(deserialized.HourlyCostRate, Is.EqualTo(original.HourlyCostRate), $"Iteration {i}: HourlyCostRate mismatch");
            Assert.That(deserialized.ResourcePrices, Has.Count.EqualTo(original.ResourcePrices.Count), $"Iteration {i}: ResourcePrices count mismatch");

            foreach (var kvp in original.ResourcePrices)
            {
                Assert.That(deserialized.ResourcePrices.ContainsKey(kvp.Key), Is.True, $"Iteration {i}: missing key {kvp.Key}");
                Assert.That(deserialized.ResourcePrices[kvp.Key], Is.EqualTo(kvp.Value), $"Iteration {i}: value mismatch for {kvp.Key}");
            }
        }
    }

    /// <summary>
    /// Pagination correctness property: for any collection and valid limit/offset,
    /// total == collection.Count, items.length &lt;= limit, offset + items.length &lt;= total.
    /// **Validates: Requirements 29.1-29.6**
    /// </summary>
    [Test]
    public void Pagination_Invariants_HoldForRandomInputs()
    {
        var rng = new Random(999);

        for (int i = 0; i < 100; i++)
        {
            int collectionSize = rng.Next(0, 201);
            var collection = Enumerable.Range(0, collectionSize)
                .Select(x => $"item-{x}")
                .ToList();

            int limit = rng.Next(1, 501);
            int offset = rng.Next(0, collectionSize + 1);

            var result = PaginationHelper.ApplyPagination<string>(collection, limit, offset);

            var okResult = result as Ok<PaginatedResponse<string>>;
            Assert.That(okResult, Is.Not.Null, $"Iteration {i}: expected Ok<PaginatedResponse<string>> for size={collectionSize}, limit={limit}, offset={offset}");

            var response = okResult!.Value!;

            // Property 1: total == collection.Count
            Assert.That(
                response.Total,
                Is.EqualTo(collectionSize),
                $"Iteration {i}: total should equal collection count. size={collectionSize}, limit={limit}, offset={offset}");

            // Property 2: items.length <= limit (capped at MaxLimit)
            int effectiveLimit = Math.Min(limit, PaginationHelper.MaxLimit);
            Assert.That(
                response.Items.Count,
                Is.LessThanOrEqualTo(effectiveLimit),
                $"Iteration {i}: items count should be <= effective limit. size={collectionSize}, limit={limit}, offset={offset}");

            // Property 3: offset + items.length <= total
            Assert.That(
                offset + response.Items.Count,
                Is.LessThanOrEqualTo(response.Total),
                $"Iteration {i}: offset + items.length should be <= total. size={collectionSize}, limit={limit}, offset={offset}");
        }
    }

    /// <summary>
    /// Authorization isolation property: for any two distinct Character_UUIDs,
    /// token A (non-Owner) cannot access token B's data (403, no entity data leaked).
    /// **Validates: Requirements 2.3, 2.4, 2.6, 2.7**
    /// </summary>
    [Test]
    public async Task AuthorizationIsolation_CrossCharacterAccess_Returns403WithNoData()
    {
        using var factory = new TestServerFactory();
        factory.SeedOwnerToken();
        using var ownerClient = factory.CreateAuthenticatedClient();

        // Create two character tokens
        var responseA = await ownerClient.PostAsJsonAsync(
            "/api/v1/tokens",
            new { characterName = "PropTestCharA" });
        var jsonA = await responseA.Content.ReadFromJsonAsync<JsonElement>();
        var tokenA = jsonA.GetProperty("token").GetString()!;
        var uuidA = jsonA.GetProperty("characterUUID").GetString()!;

        var responseB = await ownerClient.PostAsJsonAsync(
            "/api/v1/tokens",
            new { characterName = "PropTestCharB" });
        var jsonB = await responseB.Content.ReadFromJsonAsync<JsonElement>();
        var tokenB = jsonB.GetProperty("token").GetString()!;
        var uuidB = jsonB.GetProperty("characterUUID").GetString()!;

        // Have token A create an asteroid
        using var clientA = factory.CreateAuthenticatedClient(tokenA);
        var createResponse = await clientA.PostAsJsonAsync(
            $"/api/v1/characters/{uuidA}/asteroids",
            new { name = "IsolationTestAsteroid", systemName = "TestSystem" });
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var createdJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var asteroidUuid = createdJson.GetProperty("uuid").GetString()!;

        // Token B tries to access token A's data across multiple endpoint types
        using var clientB = factory.CreateAuthenticatedClient(tokenB);

        var endpoints = new[]
        {
            ("GET", $"/api/v1/characters/{uuidA}/asteroids"),
            ("GET", $"/api/v1/characters/{uuidA}/asteroids/{asteroidUuid}"),
            ("GET", $"/api/v1/characters/{uuidA}/colonies"),
            ("GET", $"/api/v1/characters/{uuidA}/blueprints"),
        };

        foreach (var (method, url) in endpoints)
        {
            var resp = await clientB.GetAsync(url);
            Assert.That(
                resp.StatusCode,
                Is.EqualTo(HttpStatusCode.Forbidden),
                $"Expected 403 for {method} {url}");

            var body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // Only "error" field with generic message — no entity data leaked
            Assert.That(root.GetProperty("error").GetString(), Is.EqualTo("Access denied"), $"Error message mismatch for {url}");
            Assert.That(root.TryGetProperty("items", out _), Is.False, $"Leaked 'items' in 403 for {url}");
            Assert.That(root.TryGetProperty("uuid", out _), Is.False, $"Leaked 'uuid' in 403 for {url}");
            Assert.That(root.TryGetProperty("name", out _), Is.False, $"Leaked 'name' in 403 for {url}");
            Assert.That(root.TryGetProperty("data", out _), Is.False, $"Leaked 'data' in 403 for {url}");
        }

        // Token B tries POST to token A's asteroids
        var postResp = await clientB.PostAsJsonAsync(
            $"/api/v1/characters/{uuidA}/asteroids",
            new { name = "ShouldNotCreate", systemName = "Blocked" });
        Assert.That(postResp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        // Token B tries PUT on token A's asteroid
        var putResp = await clientB.PutAsJsonAsync(
            $"/api/v1/characters/{uuidA}/asteroids/{asteroidUuid}",
            new { name = "ShouldNotUpdate" });
        Assert.That(putResp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        // Token B tries DELETE on token A's asteroid
        var deleteResp = await clientB.DeleteAsync(
            $"/api/v1/characters/{uuidA}/asteroids/{asteroidUuid}");
        Assert.That(deleteResp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        // Verify token A's asteroid is still intact (not deleted or modified)
        var verifyResp = await clientA.GetAsync(
            $"/api/v1/characters/{uuidA}/asteroids/{asteroidUuid}");
        Assert.That(verifyResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var verifyJson = await verifyResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(verifyJson.GetProperty("name").GetString(), Is.EqualTo("IsolationTestAsteroid"));
    }

    private static Asteroid CreateRandomAsteroid(Random rng)
    {
        var reserveCount = rng.Next(0, 5);
        var reserves = new List<AsteroidReserve>();
        var resources = new[] { "Alkali Metals", "Iron", "Copper", "Titanium", "Carbon", "Silicon" };
        var purities = new[] { "High", "Medium", "Low", "Refined" };

        for (int r = 0; r < reserveCount; r++)
        {
            reserves.Add(new AsteroidReserve
            {
                ResourceName = resources[rng.Next(resources.Length)],
                Purity = purities[rng.Next(purities.Length)],
                MaxReserve = rng.Next(100, 10000),
                CurrentReserve = rng.Next(0, 10000),
                ResetTimestamp = rng.Next(2) == 0 ? string.Empty : "2024-01-15T10:30:00Z",
            });
        }

        return new Asteroid
        {
            UUID = Guid.NewGuid().ToString(),
            Name = $"Asteroid-{rng.Next(1000)}",
            SystemName = $"System-{rng.Next(100)}",
            Reserves = reserves,
        };
    }

    private static Ship CreateRandomShip(Random rng)
    {
        var locationTypes = (DestinationType[])Enum.GetValues(typeof(DestinationType));

        return new Ship
        {
            UUID = Guid.NewGuid().ToString(),
            Name = $"Ship-{rng.Next(1000)}",
            OwnerUUID = Guid.NewGuid().ToString(),
            TemplateUUID = Guid.NewGuid().ToString(),
            HullBlueprintUUID = Guid.NewGuid().ToString(),
            LocationType = locationTypes[rng.Next(locationTypes.Length)],
            LocationUUID = Guid.NewGuid().ToString(),
            HullCurrentHP = rng.Next(0, 5000),
            HullMaxHP = rng.Next(1000, 10000),
            HullMaxRepairPercent = (decimal)rng.Next(0, 100) / 100m,
        };
    }

    private static PricingPlan CreateRandomPricingPlan(Random rng)
    {
        var resourcePrices = new Dictionary<string, decimal>();
        var resourceNames = new[] { "Alkali Metals", "Iron", "Copper", "Titanium", "Carbon" };
        var purities = new[] { "High", "Medium", "Low", "Refined" };

        int priceCount = rng.Next(0, 8);
        for (int p = 0; p < priceCount; p++)
        {
            var key = $"{resourceNames[rng.Next(resourceNames.Length)]}|{purities[rng.Next(purities.Length)]}";
            if (!resourcePrices.ContainsKey(key))
            {
                resourcePrices[key] = (decimal)rng.Next(1, 10000) / 100m;
            }
        }

        return new PricingPlan
        {
            UUID = Guid.NewGuid().ToString(),
            Name = $"Plan-{rng.Next(1000)}",
            OwnerUUID = Guid.NewGuid().ToString(),
            Description = rng.Next(2) == 0 ? string.Empty : $"Description-{rng.Next(100)}",
            FixedCostPerItem = (decimal)rng.Next(0, 500) / 100m,
            HourlyCostRate = (decimal)rng.Next(0, 1000) / 100m,
            ResourcePrices = resourcePrices,
        };
    }
}
