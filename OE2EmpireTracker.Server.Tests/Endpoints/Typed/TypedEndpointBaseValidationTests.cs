// -----------------------------------------------------------------------
// <copyright file="TypedEndpointBaseValidationTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OE2EmpireTracker.Server.Auth;
using OE2EmpireTracker.Server.Storage;
using OE2EmpireTracker.Services;

#pragma warning disable SA1009 // Closing parenthesis should be followed by a space

namespace OE2EmpireTracker.Server.Tests.Endpoints.Typed;

/// <summary>
/// Unit tests for input validation in TypedEndpointBase.
/// Uses AsteroidEndpoints as the concrete implementation under test.
/// Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8.
/// </summary>
[TestFixture]
public class TypedEndpointBaseValidationTests
{
    private TestServerFactory _factory = null!;
    private HttpClient _ownerClient = null!;
    private string _charToken = string.Empty;
    private string _charUUID = string.Empty;

    /// <summary>
    /// Sets up the test server with a character token for validation tests.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestServerFactory();
        _factory.SeedOwnerToken();
        _ownerClient = _factory.CreateAuthenticatedClient();

        // Create a character token
        var createResponse = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "ValidationTestChar" })
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
    /// When POST body is empty, returns 400 with "Request body is required".
    /// Validates: Requirement 3.2.
    /// </summary>
    [Test]
    public async Task Create_EmptyBody_Returns400()
    {
        using var charClient = _factory.CreateAuthenticatedClient(_charToken);
        var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

        var response = await charClient.PostAsync(
            $"/api/v1/characters/{_charUUID}/asteroids", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var error = json.GetProperty("error").GetString();
        Assert.That(error, Does.Contain("body"));
    }

    /// <summary>
    /// When POST body is invalid JSON, returns 400 with "Invalid request body".
    /// Validates: Requirement 3.1.
    /// </summary>
    [Test]
    public async Task Create_InvalidJson_Returns400()
    {
        using var charClient = _factory.CreateAuthenticatedClient(_charToken);
        var content = new StringContent(
            "{ this is not valid json }", Encoding.UTF8, "application/json");

        var response = await charClient.PostAsync(
            $"/api/v1/characters/{_charUUID}/asteroids", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var error = json.GetProperty("error").GetString();
        Assert.That(error, Does.Contain("Invalid request body"));
    }

    /// <summary>
    /// When POST body is missing required field (Name for Asteroid), returns 400.
    /// Validates: Requirements 3.3, 3.4.
    /// </summary>
    [Test]
    public async Task Create_MissingRequiredField_Returns400()
    {
        using var charClient = _factory.CreateAuthenticatedClient(_charToken);

        // Send a body without the required "name" field
        var response = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/asteroids",
            new { systemName = "SomeSystem" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var error = json.GetProperty("error").GetString();
        Assert.That(error, Does.Contain("Name is required"));
    }

    /// <summary>
    /// When POST body has whitespace-only required field, returns 400.
    /// Validates: Requirement 3.4.
    /// </summary>
    [Test]
    public async Task Create_WhitespaceRequiredField_Returns400()
    {
        using var charClient = _factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/asteroids",
            new { name = "   " });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var error = json.GetProperty("error").GetString();
        Assert.That(error, Does.Contain("Name is required"));
    }

    /// <summary>
    /// When Character_UUID in URL is empty/whitespace, returns 400.
    /// Validates: Requirement 3.5.
    /// </summary>
    [Test]
    public async Task GetAll_InvalidCharacterUUID_Returns400()
    {
        // Use a space-encoded UUID to test whitespace validation
        var response = await _ownerClient.GetAsync(
            "/api/v1/characters/%20/asteroids");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var error = json.GetProperty("error").GetString();
        Assert.That(error, Does.Contain("Invalid character UUID"));
    }

    /// <summary>
    /// When Entity_UUID in URL is empty/whitespace, returns 400.
    /// Validates: Requirement 3.6.
    /// </summary>
    [Test]
    public async Task GetOne_InvalidEntityUUID_Returns400()
    {
        var response = await _ownerClient.GetAsync(
            $"/api/v1/characters/{_charUUID}/asteroids/%20");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var error = json.GetProperty("error").GetString();
        Assert.That(error, Does.Contain("Invalid entity UUID"));
    }

    /// <summary>
    /// When Content-Type is not application/json, returns 415.
    /// Validates: Requirement 3.8.
    /// </summary>
    [Test]
    public async Task Create_WrongContentType_Returns415()
    {
        using var charClient = _factory.CreateAuthenticatedClient(_charToken);
        var content = new StringContent(
            "<xml>not json</xml>", Encoding.UTF8, "text/xml");

        var response = await charClient.PostAsync(
            $"/api/v1/characters/{_charUUID}/asteroids", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnsupportedMediaType));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var error = json.GetProperty("error").GetString();
        Assert.That(error, Does.Contain("Unsupported media type"));
    }

    /// <summary>
    /// When PUT has wrong Content-Type, returns 415.
    /// Validates: Requirement 3.8.
    /// </summary>
    [Test]
    public async Task Update_WrongContentType_Returns415()
    {
        using var charClient = _factory.CreateAuthenticatedClient(_charToken);
        var content = new StringContent(
            "plain text body", Encoding.UTF8, "text/plain");

        var response = await charClient.PutAsync(
            $"/api/v1/characters/{_charUUID}/asteroids/some-uuid", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnsupportedMediaType));
    }

    /// <summary>
    /// When PUT body is invalid JSON, returns 400.
    /// Validates: Requirement 3.1.
    /// </summary>
    [Test]
    public async Task Update_InvalidJson_Returns400()
    {
        using var charClient = _factory.CreateAuthenticatedClient(_charToken);

        // First create an asteroid to have a valid entity UUID
        var createResponse = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/asteroids",
            new { name = "TestAsteroid" });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var entityUuid = created.GetProperty("uuid").GetString()!;

        // Now try to update with invalid JSON
        var content = new StringContent(
            "{ broken json !!!", Encoding.UTF8, "application/json");
        var response = await charClient.PutAsync(
            $"/api/v1/characters/{_charUUID}/asteroids/{entityUuid}", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// When request body contains extra/unknown fields, they are ignored (lenient).
    /// Validates: Requirement 3.7 (extra fields ignored, no error).
    /// </summary>
    [Test]
    public async Task Create_ExtraFields_IgnoredSuccessfully()
    {
        using var charClient = _factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/asteroids",
            new { name = "ValidAsteroid", unknownField = "should be ignored", anotherExtra = 42 });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }
}
