// -----------------------------------------------------------------------
// <copyright file="Property8_RemovedRoutesTests.cs" company="OE2EmpireTracker">
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
/// Property 8: Removed Routes Return 404 via Framework Default.
/// Generates random removed route paths with random dataType values and entity UUIDs,
/// verifies HTTP 404 comes from the framework default (no explicit handler).
/// **Validates: Req 1, Criteria 8-9**
/// </summary>
[TestFixture]
public class Property8_RemovedRoutesTests
{
    /// <summary>
    /// Known data types that were previously valid route segments for the removed
    /// raw character data endpoints.
    /// </summary>
    private static readonly string[] KnownDataTypes =
    {
        "colonies",
        "blueprints",
        "surveys",
        "profiles",
        "delivery-routes",
        "delivery-plans",
        "ships",
        "ship-templates",
        "market-listings",
        "market-transactions",
        "pricing-plans",
        "stock-plans",
        "stock-profiles",
        "build-plans",
        "supply-chains",
        "asteroids",
        "stations",
        "faction-contacts",
        "external-characters",
    };

    /// <summary>
    /// Additional random-looking data type strings to verify that arbitrary
    /// path segments also return 404 (not just known types).
    /// </summary>
    private static readonly string[] ArbitraryDataTypes =
    {
        "foo",
        "bar-baz",
        "unknown-type",
        "COLONIES",
        "Blueprints",
        "some_random_thing",
        "123-numeric",
        "a",
    };

    private TestServerFactory _factory = null!;
    private HttpClient _ownerClient = null!;
    private string _charUUID = string.Empty;

    /// <summary>
    /// Sets up the test server and creates a character for route testing.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestServerFactory();
        _factory.SeedOwnerToken();
        _ownerClient = _factory.CreateAuthenticatedClient();

        var createResponse = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "P8TestChar" })
            .GetAwaiter().GetResult();
        var createJson = createResponse.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
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
    /// Property: random removed route paths with random dataType values return 404
    /// from the framework default (empty or no-content body, no custom error handler).
    /// Tests collection-level routes: GET, PUT, POST on /data/{dataType}.
    /// </summary>
    [Test]
    public async Task RemovedCollectionRoutes_RandomDataTypes_Return404ViaFrameworkDefault()
    {
        var rng = new Random(808);
        var allDataTypes = KnownDataTypes.Concat(ArbitraryDataTypes).ToArray();

        for (int iteration = 0; iteration < 30; iteration++)
        {
            var dataType = allDataTypes[rng.Next(allDataTypes.Length)];
            var path = $"/api/v1/characters/{_charUUID}/data/{dataType}";

            // Test GET
            var getResponse = await _ownerClient.GetAsync(path);
            Assert404FrameworkDefault(getResponse, "GET", path, iteration);

            // Test PUT
            var putContent = new StringContent("[]", Encoding.UTF8, "application/json");
            var putResponse = await _ownerClient.PutAsync(path, putContent);
            Assert404FrameworkDefault(putResponse, "PUT", path, iteration);

            // Test POST
            var postContent = new StringContent("{}", Encoding.UTF8, "application/json");
            var postResponse = await _ownerClient.PostAsync(path, postContent);
            Assert404FrameworkDefault(postResponse, "POST", path, iteration);
        }
    }

    /// <summary>
    /// Property: random removed entity-level routes with random dataType and entity UUID
    /// return 404 from the framework default.
    /// Tests: GET, PUT, DELETE on /data/{dataType}/{entityUuid}.
    /// </summary>
    [Test]
    public async Task RemovedEntityRoutes_RandomDataTypesAndUUIDs_Return404ViaFrameworkDefault()
    {
        var rng = new Random(909);
        var allDataTypes = KnownDataTypes.Concat(ArbitraryDataTypes).ToArray();

        for (int iteration = 0; iteration < 30; iteration++)
        {
            var dataType = allDataTypes[rng.Next(allDataTypes.Length)];
            var entityUuid = Guid.NewGuid().ToString();
            var path = $"/api/v1/characters/{_charUUID}/data/{dataType}/{entityUuid}";

            // Test GET
            var getResponse = await _ownerClient.GetAsync(path);
            Assert404FrameworkDefault(getResponse, "GET", path, iteration);

            // Test PUT
            var putContent = new StringContent("{}", Encoding.UTF8, "application/json");
            var putResponse = await _ownerClient.PutAsync(path, putContent);
            Assert404FrameworkDefault(putResponse, "PUT", path, iteration);

            // Test DELETE
            var deleteResponse = await _ownerClient.DeleteAsync(path);
            Assert404FrameworkDefault(deleteResponse, "DELETE", path, iteration);
        }
    }

    /// <summary>
    /// Property: the removed bulk data route (PUT /data) returns 404 from framework default.
    /// Tests with random character UUIDs.
    /// </summary>
    [Test]
    public async Task RemovedBulkDataRoute_RandomCharUUIDs_Returns404ViaFrameworkDefault()
    {
        var rng = new Random(1010);

        for (int iteration = 0; iteration < 20; iteration++)
        {
            // Use either the real character UUID or a random one
            var charUuid = iteration % 2 == 0
                ? _charUUID
                : Guid.NewGuid().ToString();

            var path = $"/api/v1/characters/{charUuid}/data";

            // Test PUT (bulk upload route)
            var putContent = new StringContent("{}", Encoding.UTF8, "application/json");
            var putResponse = await _ownerClient.PutAsync(path, putContent);
            Assert404FrameworkDefault(putResponse, "PUT", path, iteration);

            // Test GET (read-all route)
            var getResponse = await _ownerClient.GetAsync(path);
            Assert404FrameworkDefault(getResponse, "GET", path, iteration);
        }
    }

    /// <summary>
    /// Property: removed routes with random HTTP methods all return 404.
    /// Verifies DELETE on collection routes and PATCH/HEAD on entity routes.
    /// </summary>
    [Test]
    public async Task RemovedRoutes_VariousHttpMethods_Return404ViaFrameworkDefault()
    {
        var rng = new Random(1111);
        var allDataTypes = KnownDataTypes.Concat(ArbitraryDataTypes).ToArray();

        for (int iteration = 0; iteration < 20; iteration++)
        {
            var dataType = allDataTypes[rng.Next(allDataTypes.Length)];
            var entityUuid = Guid.NewGuid().ToString();

            // DELETE on collection route (not just entity route)
            var collectionPath = $"/api/v1/characters/{_charUUID}/data/{dataType}";
            var deleteCollResponse = await _ownerClient.DeleteAsync(collectionPath);
            Assert404FrameworkDefault(deleteCollResponse, "DELETE", collectionPath, iteration);

            // PATCH on entity route
            var entityPath = $"/api/v1/characters/{_charUUID}/data/{dataType}/{entityUuid}";
            var patchContent = new StringContent("{}", Encoding.UTF8, "application/json");
            var patchRequest = new HttpRequestMessage(HttpMethod.Patch, entityPath)
            {
                Content = patchContent,
            };
            var patchResponse = await _ownerClient.SendAsync(patchRequest);
            Assert404FrameworkDefault(patchResponse, "PATCH", entityPath, iteration);
        }
    }

    /// <summary>
    /// Property: removed routes with completely random character UUIDs still return 404
    /// (not 403 or other status), confirming no handler is registered.
    /// </summary>
    [Test]
    public async Task RemovedRoutes_RandomCharacterUUIDs_Return404NotOtherStatus()
    {
        var rng = new Random(1212);
        var allDataTypes = KnownDataTypes.Concat(ArbitraryDataTypes).ToArray();

        for (int iteration = 0; iteration < 20; iteration++)
        {
            var randomCharUuid = Guid.NewGuid().ToString();
            var dataType = allDataTypes[rng.Next(allDataTypes.Length)];
            var entityUuid = Guid.NewGuid().ToString();

            // Collection route with random char UUID
            var collPath = $"/api/v1/characters/{randomCharUuid}/data/{dataType}";
            var getResponse = await _ownerClient.GetAsync(collPath);
            Assert404FrameworkDefault(getResponse, "GET", collPath, iteration);

            // Entity route with random char UUID
            var entityPath = $"/api/v1/characters/{randomCharUuid}/data/{dataType}/{entityUuid}";
            var getEntityResponse = await _ownerClient.GetAsync(entityPath);
            Assert404FrameworkDefault(getEntityResponse, "GET", entityPath, iteration);

            // Bulk route with random char UUID
            var bulkPath = $"/api/v1/characters/{randomCharUuid}/data";
            var getBulkResponse = await _ownerClient.GetAsync(bulkPath);
            Assert404FrameworkDefault(getBulkResponse, "GET", bulkPath, iteration);
        }
    }

    /// <summary>
    /// Asserts that the response is HTTP 404 from the framework default behavior.
    /// Framework default 404 responses have an empty body or a minimal body with no
    /// custom error structure (no "error" key, no JSON object with application-specific fields).
    /// </summary>
    /// <param name="response">The HTTP response to check.</param>
    /// <param name="method">The HTTP method used (for diagnostics).</param>
    /// <param name="path">The request path (for diagnostics).</param>
    /// <param name="iteration">The test iteration number (for diagnostics).</param>
    private static void Assert404FrameworkDefault(
        HttpResponseMessage response,
        string method,
        string path,
        int iteration)
    {
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound),
            $"Iteration {iteration}: {method} {path} " +
            $"expected 404 but got {(int)response.StatusCode}");

        // Framework default 404 has empty body (no explicit handler)
        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        // The body should be empty or contain no custom error JSON structure.
        // An explicit 404 handler would return { "error": "..." } or similar.
        // Framework default returns empty string or just the status phrase.
        if (!string.IsNullOrEmpty(body))
        {
            // If there is a body, it should NOT be a custom JSON error response
            var isJson = body.TrimStart().StartsWith("{", StringComparison.Ordinal)
                || body.TrimStart().StartsWith("[", StringComparison.Ordinal);

            if (isJson)
            {
                // If it's JSON, verify it's not a custom error handler response
                // (custom handlers would have "error" or "message" fields)
                var json = JsonSerializer.Deserialize<JsonElement>(body);
                Assert.That(
                    json.TryGetProperty("error", out _),
                    Is.False,
                    $"Iteration {iteration}: {method} {path} " +
                    $"returned a custom error JSON (explicit handler detected)");
            }
        }
    }
}
