// -----------------------------------------------------------------------
// <copyright file="ColonyPlannerBuildOrderTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Server.Tests;

#pragma warning disable SA1204 // Static members should appear before non-static members

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Integration tests for the POST /api/v1/colony-planner/build-order endpoint.
/// Validates that valid requests invoke the optimizer and return the expected response
/// shape, that invalid requests return 400, and that no auth is required.
/// **Validates: Requirements 3.1, 3.2, 3.4, 3.5, 3.6, 3.7**
/// </summary>
[TestFixture]
public class ColonyPlannerBuildOrderTests
{
    private const string BuildOrderUrl = "/api/v1/colony-planner/build-order";

    /// <summary>
    /// Sets up the test server factory.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        var factory = SharedTestServer.Factory;
    }

    /// <summary>
    /// Tears down the test server.
    /// </summary>
    [OneTimeTearDown]
    public void TearDown()
    {
    }

    /// <summary>
    /// Valid request with 2+ structures returns 200 with optimizedOrder array.
    /// Validates: Requirement 3.1 — valid request invokes optimizer.
    /// </summary>
    [Test]
    public async Task ValidRequest_WithMultipleStructures_Returns200WithOptimizedOrder()
    {
        using var client = SharedTestServer.Factory.CreateClient();

        var requestBody = new
        {
            structures = new[]
            {
                new
                {
                    flatpackBlueprintUUID = Guid.NewGuid().ToString(),
                    isBuilt = false,
                    isStaged = true,
                    isOnline = false,
                    buildQueueSequence = 1,
                    assignedWorkers = new Dictionary<string, bool>(),
                },
                new
                {
                    flatpackBlueprintUUID = Guid.NewGuid().ToString(),
                    isBuilt = false,
                    isStaged = true,
                    isOnline = false,
                    buildQueueSequence = 2,
                    assignedWorkers = new Dictionary<string, bool>(),
                },
            },
        };

        var response = await client.PostAsJsonAsync(BuildOrderUrl, requestBody);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(
            json.TryGetProperty("optimizedOrder", out var optimizedOrder),
            Is.True,
            "Response must contain optimizedOrder array");
        Assert.That(
            optimizedOrder.ValueKind,
            Is.EqualTo(JsonValueKind.Array),
            "optimizedOrder must be an array");
        Assert.That(
            optimizedOrder.GetArrayLength(),
            Is.GreaterThanOrEqualTo(2),
            "optimizedOrder must contain at least the submitted structures");

        // Each entry must have flatpackBlueprintUUID and buildQueueSequence
        foreach (var entry in optimizedOrder.EnumerateArray())
        {
            Assert.That(
                entry.TryGetProperty("flatpackBlueprintUUID", out _),
                Is.True,
                "Each optimizedOrder entry must have flatpackBlueprintUUID");
            Assert.That(
                entry.TryGetProperty("buildQueueSequence", out _),
                Is.True,
                "Each optimizedOrder entry must have buildQueueSequence");
        }
    }

    /// <summary>
    /// Response contains steps array and totalTimeEstimate for backward compatibility.
    /// Validates: Requirement 3.4 — backward compat fields present.
    /// </summary>
    [Test]
    public async Task ValidRequest_ResponseContainsStepsAndTotalTimeEstimate()
    {
        using var client = SharedTestServer.Factory.CreateClient();

        var requestBody = new
        {
            structures = new[]
            {
                new
                {
                    flatpackBlueprintUUID = Guid.NewGuid().ToString(),
                    isBuilt = false,
                    isStaged = true,
                    isOnline = false,
                    buildQueueSequence = 1,
                    assignedWorkers = new Dictionary<string, bool>(),
                },
                new
                {
                    flatpackBlueprintUUID = Guid.NewGuid().ToString(),
                    isBuilt = false,
                    isStaged = true,
                    isOnline = false,
                    buildQueueSequence = 2,
                    assignedWorkers = new Dictionary<string, bool>(),
                },
            },
        };

        var response = await client.PostAsJsonAsync(BuildOrderUrl, requestBody);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.That(
            json.TryGetProperty("steps", out var steps),
            Is.True,
            "Response must contain steps array");
        Assert.That(
            steps.ValueKind,
            Is.EqualTo(JsonValueKind.Array),
            "steps must be an array");

        Assert.That(
            json.TryGetProperty("totalTimeEstimate", out var totalTime),
            Is.True,
            "Response must contain totalTimeEstimate");
        Assert.That(
            totalTime.ValueKind,
            Is.EqualTo(JsonValueKind.String),
            "totalTimeEstimate must be a string");
        Assert.That(
            totalTime.GetString(),
            Is.Not.Null.And.Not.Empty,
            "totalTimeEstimate must not be empty");
    }

    /// <summary>
    /// Missing request body returns 400 with error message.
    /// Validates: Requirement 3.5 — missing body returns 400.
    /// </summary>
    [Test]
    public async Task MissingBody_Returns400WithError()
    {
        using var client = SharedTestServer.Factory.CreateClient();

        var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(BuildOrderUrl, content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(
            json.TryGetProperty("error", out var error),
            Is.True,
            "400 response must contain error field");
        Assert.That(
            error.GetString(),
            Is.Not.Null.And.Not.Empty,
            "error message must not be empty");
    }

    /// <summary>
    /// Invalid JSON body returns 400 with error message.
    /// Validates: Requirement 3.5 — invalid JSON returns 400.
    /// </summary>
    [Test]
    public async Task InvalidJson_Returns400WithError()
    {
        using var client = SharedTestServer.Factory.CreateClient();

        var content = new StringContent(
            "{ this is not valid json }",
            Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync(BuildOrderUrl, content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(
            json.TryGetProperty("error", out var error),
            Is.True,
            "400 response must contain error field");
        Assert.That(
            error.GetString(),
            Does.Contain("Invalid JSON"),
            "error message should indicate invalid JSON");
    }

    /// <summary>
    /// Empty structures array returns 400 with error message.
    /// Validates: Requirement 3.6 — empty structures returns 400.
    /// </summary>
    [Test]
    public async Task EmptyStructures_Returns400WithError()
    {
        using var client = SharedTestServer.Factory.CreateClient();

        var requestBody = new { structures = Array.Empty<object>() };
        var response = await client.PostAsJsonAsync(BuildOrderUrl, requestBody);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(
            json.TryGetProperty("error", out var error),
            Is.True,
            "400 response must contain error field");
        Assert.That(
            error.GetString(),
            Does.Contain("structures"),
            "error message should reference structures field");
    }

    /// <summary>
    /// No auth token required — endpoint does not return 401/403 without authentication.
    /// The endpoint may return 200 or 500 (if optimizer dependencies are missing in test),
    /// but never an auth failure, proving it is publicly accessible.
    /// Validates: Requirement 3.7 — endpoint is publicly accessible.
    /// </summary>
    [Test]
    public async Task NoAuthToken_DoesNotReturn401Or403_PublicEndpoint()
    {
        // Use the shared factory but create a plain client (no auth headers)
        using var client = SharedTestServer.Factory.CreateClient();

        var requestBody = new
        {
            structures = new[]
            {
                new
                {
                    flatpackBlueprintUUID = Guid.NewGuid().ToString(),
                    isBuilt = false,
                    isStaged = true,
                    isOnline = false,
                    buildQueueSequence = 1,
                    assignedWorkers = new Dictionary<string, bool>(),
                },
                new
                {
                    flatpackBlueprintUUID = Guid.NewGuid().ToString(),
                    isBuilt = false,
                    isStaged = true,
                    isOnline = false,
                    buildQueueSequence = 2,
                    assignedWorkers = new Dictionary<string, bool>(),
                },
            },
        };

        var response = await client.PostAsJsonAsync(BuildOrderUrl, requestBody);

        // The endpoint must not require authentication — it should never return 401 or 403
        Assert.That(
            response.StatusCode,
            Is.Not.EqualTo(HttpStatusCode.Unauthorized),
            "Build-order endpoint must not require authentication");
        Assert.That(
            response.StatusCode,
            Is.Not.EqualTo(HttpStatusCode.Forbidden),
            "Build-order endpoint must not require authorization");
    }

    /// <summary>
    /// Property 7: Server validation rejects invalid requests.
    /// For any request body that is empty, contains invalid JSON, has a missing
    /// structures field, or has an empty structures array, the build-order endpoint
    /// SHALL return HTTP 400 with a JSON body containing an error field.
    /// Feature: colony-planner-reorder, Property 7: Server validation rejects invalid requests
    /// **Validates: Requirements 3.5, 3.6**
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property InvalidRequests_AlwaysReturn400WithError()
    {
        return Prop.ForAll(
            InvalidRequestBodyArbitrary(),
            body =>
            {
                RunInvalidRequestCheck(body).GetAwaiter().GetResult();
            });
    }

    // ===== Property Test Helpers =====

    private async Task RunInvalidRequestCheck(InvalidRequestBody scenario)
    {
        using var client = SharedTestServer.Factory.CreateClient();

        var content = new StringContent(
            scenario.Body,
            Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync(BuildOrderUrl, content);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest),
            $"Expected 400 for invalid body: {scenario}");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(
            json.TryGetProperty("error", out var error),
            Is.True,
            $"400 response must contain error field for body: {scenario}");
        Assert.That(
            error.GetString(),
            Is.Not.Null.And.Not.Empty,
            $"error message must not be empty for body: {scenario}");
    }

    // ===== Generators =====

    private static Arbitrary<InvalidRequestBody> InvalidRequestBodyArbitrary()
    {
        var gen = Gen.OneOf(
            EmptyBodyGen(),
            InvalidJsonGen(),
            MissingStructuresFieldGen(),
            NullStructuresGen(),
            EmptyStructuresArrayGen());
        return Arb.From(gen);
    }

    private static Gen<InvalidRequestBody> EmptyBodyGen()
    {
        return Gen.Constant(new InvalidRequestBody
        {
            Body = string.Empty,
            Category = "empty",
        });
    }

    private static Gen<InvalidRequestBody> InvalidJsonGen()
    {
        return from garble in Gen.Elements(
                   "{ not json }",
                   "{ \"structures\": [",
                   "{{{{",
                   "[[[",
                   "null null",
                   "{ \"structures\": undefined }",
                   "{ 'structures': [] }",
                   "structures: []")
               select new InvalidRequestBody
               {
                   Body = garble,
                   Category = "invalid-json",
               };
    }

    private static Gen<InvalidRequestBody> MissingStructuresFieldGen()
    {
        return from key in Gen.Elements(
                   "items", "data", "buildings", "queue", "plan")
               select new InvalidRequestBody
               {
                   Body = $"{{ \"{key}\": [{{ \"id\": \"test\" }}] }}",
                   Category = "missing-structures",
               };
    }

    private static Gen<InvalidRequestBody> NullStructuresGen()
    {
        return Gen.Constant(new InvalidRequestBody
        {
            Body = "{ \"structures\": null }",
            Category = "null-structures",
        });
    }

    private static Gen<InvalidRequestBody> EmptyStructuresArrayGen()
    {
        return Gen.Constant(new InvalidRequestBody
        {
            Body = "{ \"structures\": [] }",
            Category = "empty-structures",
        });
    }

    // ===== Scenario Types =====

    private sealed class InvalidRequestBody
    {
        public string Body { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public override string ToString()
        {
            var preview = Body.Length > 40
                ? Body[..40] + "..."
                : Body;
            return $"InvalidRequestBody(category={Category}, body=\"{preview}\")";
        }
    }
}
