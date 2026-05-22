// -----------------------------------------------------------------------
// <copyright file="Property3_AllOrNothingImportTests.cs" company="OE2EmpireTracker">
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
/// Property 3: All-or-Nothing Import Semantics.
/// For any bulk import request: either ALL entities pass validation and ALL are persisted,
/// or NONE are persisted. There is no partial import state.
/// **Validates: Req 2, Criteria 3-4**
/// </summary>
[TestFixture]
public class Property3_AllOrNothingImportTests
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
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "Prop3TestChar" })
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
    /// Property: when a bulk import contains ANY invalid entity, ZERO entities are persisted.
    /// Generates random payloads with a mix of valid colonies and invalid colonies
    /// (missing required PlanetName or ColonyName), then verifies nothing was stored.
    /// </summary>
    [Test]
    public async Task AllOrNothing_InvalidEntitiesPresent_NothingPersisted()
    {
        var rng = new Random(42);

        for (int iteration = 0; iteration < 20; iteration++)
        {
            // Create a fresh character for each iteration to avoid cross-contamination
            var charResponse = await _ownerClient.PostAsJsonAsync(
                "/api/v1/tokens",
                new { characterName = $"Prop3Iter{iteration}" });
            var charJson = await charResponse.Content.ReadFromJsonAsync<JsonElement>();
            var iterToken = charJson.GetProperty("token").GetString()!;
            var iterUUID = charJson.GetProperty("characterUUID").GetString()!;

            using var client = _factory.CreateAuthenticatedClient(iterToken);

            // Generate a mix of valid and invalid colonies
            int validCount = rng.Next(1, 5);
            int invalidCount = rng.Next(1, 4);
            var colonies = new List<object>();

            for (int v = 0; v < validCount; v++)
            {
                colonies.Add(new
                {
                    UUID = Guid.NewGuid().ToString(),
                    OwnerUUID = iterUUID,
                    PlanetName = $"Planet-{iteration}-{v}",
                    ColonyName = $"Colony-{iteration}-{v}",
                });
            }

            for (int inv = 0; inv < invalidCount; inv++)
            {
                // Invalid: missing PlanetName (null/empty)
                colonies.Add(new
                {
                    UUID = Guid.NewGuid().ToString(),
                    OwnerUUID = iterUUID,
                    PlanetName = string.Empty,
                    ColonyName = $"BadColony-{iteration}-{inv}",
                });
            }

            // Shuffle to randomize order
            ShuffleList(colonies, rng);

            var payload = new { DataVersion = 1, CurrentPlayerUUID = iterUUID, Colony = colonies };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Attempt import — should fail with 400
            var importResp = await client.PutAsync(
                $"/api/v1/characters/{iterUUID}/import", content);

            Assert.That(
                importResp.StatusCode,
                Is.EqualTo(HttpStatusCode.BadRequest),
                $"Iteration {iteration}: expected 400 for payload with {invalidCount} invalid entities");

            // Verify ZERO colonies were persisted
            var getResp = await client.GetAsync(
                $"/api/v1/characters/{iterUUID}/colonies");
            Assert.That(getResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var getJson = await getResp.Content.ReadFromJsonAsync<JsonElement>();
            var items = getJson.GetProperty("items");
            Assert.That(
                items.GetArrayLength(),
                Is.EqualTo(0),
                $"Iteration {iteration}: expected 0 persisted colonies but found {items.GetArrayLength()}. " +
                $"Payload had {validCount} valid + {invalidCount} invalid.");
        }
    }

    /// <summary>
    /// Property: when a bulk import contains ONLY valid entities, ALL are persisted.
    /// Generates random payloads with all-valid colonies and blueprints,
    /// then verifies the exact count was stored.
    /// </summary>
    [Test]
    public async Task AllOrNothing_AllValidEntities_AllPersisted()
    {
        var rng = new Random(99);

        for (int iteration = 0; iteration < 20; iteration++)
        {
            // Create a fresh character for each iteration
            var charResponse = await _ownerClient.PostAsJsonAsync(
                "/api/v1/tokens",
                new { characterName = $"Prop3Valid{iteration}" });
            var charJson = await charResponse.Content.ReadFromJsonAsync<JsonElement>();
            var iterToken = charJson.GetProperty("token").GetString()!;
            var iterUUID = charJson.GetProperty("characterUUID").GetString()!;

            using var client = _factory.CreateAuthenticatedClient(iterToken);

            // Generate random valid colonies
            int colonyCount = rng.Next(0, 6);
            var colonies = new List<object>();
            for (int c = 0; c < colonyCount; c++)
            {
                colonies.Add(new
                {
                    UUID = Guid.NewGuid().ToString(),
                    OwnerUUID = iterUUID,
                    PlanetName = $"ValidPlanet-{iteration}-{c}",
                    ColonyName = $"ValidColony-{iteration}-{c}",
                });
            }

            // Generate random valid asteroids
            int asteroidCount = rng.Next(0, 5);
            var asteroids = new List<object>();
            for (int a = 0; a < asteroidCount; a++)
            {
                asteroids.Add(new
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = $"Asteroid-{iteration}-{a}",
                    SystemName = $"System-{iteration}-{a}",
                });
            }

            var payload = new
            {
                DataVersion = 1,
                CurrentPlayerUUID = iterUUID,
                Colony = colonies,
                Asteroid = asteroids,
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Attempt import — should succeed with 200
            var importResp = await client.PutAsync(
                $"/api/v1/characters/{iterUUID}/import", content);

            Assert.That(
                importResp.StatusCode,
                Is.EqualTo(HttpStatusCode.OK),
                $"Iteration {iteration}: expected 200 for all-valid payload");

            var importResult = await importResp.Content.ReadFromJsonAsync<JsonElement>();
            int expectedTotal = colonyCount + asteroidCount;
            Assert.That(
                importResult.GetProperty("total").GetInt32(),
                Is.EqualTo(expectedTotal),
                $"Iteration {iteration}: total mismatch");

            // Verify colonies were persisted
            var colResp = await client.GetAsync(
                $"/api/v1/characters/{iterUUID}/colonies");
            var colJson = await colResp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.That(
                colJson.GetProperty("items").GetArrayLength(),
                Is.EqualTo(colonyCount),
                $"Iteration {iteration}: colony count mismatch");

            // Verify asteroids were persisted
            var astResp = await client.GetAsync(
                $"/api/v1/characters/{iterUUID}/asteroids");
            var astJson = await astResp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.That(
                astJson.GetProperty("items").GetArrayLength(),
                Is.EqualTo(asteroidCount),
                $"Iteration {iteration}: asteroid count mismatch");
        }
    }

    /// <summary>
    /// Property: mixed entity types with one invalid entity type causes
    /// ALL collections to be rejected (not just the invalid one).
    /// Generates payloads with valid asteroids + invalid colonies to verify
    /// that even the valid asteroids are not persisted.
    /// </summary>
    [Test]
    public async Task AllOrNothing_MixedEntityTypes_InvalidInOneRejectsAll()
    {
        var rng = new Random(2024);

        for (int iteration = 0; iteration < 15; iteration++)
        {
            // Create a fresh character for each iteration
            var charResponse = await _ownerClient.PostAsJsonAsync(
                "/api/v1/tokens",
                new { characterName = $"Prop3Mix{iteration}" });
            var charJson = await charResponse.Content.ReadFromJsonAsync<JsonElement>();
            var iterToken = charJson.GetProperty("token").GetString()!;
            var iterUUID = charJson.GetProperty("characterUUID").GetString()!;

            using var client = _factory.CreateAuthenticatedClient(iterToken);

            // Valid asteroids
            int asteroidCount = rng.Next(1, 5);
            var asteroids = new List<object>();
            for (int a = 0; a < asteroidCount; a++)
            {
                asteroids.Add(new
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = $"GoodAsteroid-{iteration}-{a}",
                    SystemName = $"System-{iteration}",
                });
            }

            // Invalid colony (missing ColonyName)
            var colonies = new List<object>
            {
                new
                {
                    UUID = Guid.NewGuid().ToString(),
                    OwnerUUID = iterUUID,
                    PlanetName = $"Planet-{iteration}",
                    ColonyName = string.Empty,
                },
            };

            var payload = new
            {
                DataVersion = 1,
                CurrentPlayerUUID = iterUUID,
                Colony = colonies,
                Asteroid = asteroids,
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Import should fail
            var importResp = await client.PutAsync(
                $"/api/v1/characters/{iterUUID}/import", content);

            Assert.That(
                importResp.StatusCode,
                Is.EqualTo(HttpStatusCode.BadRequest),
                $"Iteration {iteration}: expected 400 due to invalid colony");

            // Verify ZERO asteroids persisted (even though they were valid)
            var astResp = await client.GetAsync(
                $"/api/v1/characters/{iterUUID}/asteroids");
            var astJson = await astResp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.That(
                astJson.GetProperty("items").GetArrayLength(),
                Is.EqualTo(0),
                $"Iteration {iteration}: expected 0 asteroids persisted but found some. " +
                $"Invalid colony should have rejected the entire import.");

            // Verify ZERO colonies persisted
            var colResp = await client.GetAsync(
                $"/api/v1/characters/{iterUUID}/colonies");
            var colJson = await colResp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.That(
                colJson.GetProperty("items").GetArrayLength(),
                Is.EqualTo(0),
                $"Iteration {iteration}: expected 0 colonies persisted");
        }
    }

    private static void ShuffleList<T>(List<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
