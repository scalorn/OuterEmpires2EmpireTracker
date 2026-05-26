// -----------------------------------------------------------------------
// <copyright file="TypedEndpointBaseAuthTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OE2EmpireTracker.Server.Auth;
using OE2EmpireTracker.Server.Storage;
using OE2EmpireTracker.Services;

#pragma warning disable SA1009 // Closing parenthesis should be followed by a space

namespace OE2EmpireTracker.Server.Tests.Endpoints.Typed;

/// <summary>
/// Unit tests for authorization enforcement in TypedEndpointBase.
/// Uses AsteroidEndpoints as the concrete implementation under test.
/// Validates: Requirements 2.2, 2.3, 2.4, 2.6.
/// </summary>
[TestFixture]
public class TypedEndpointBaseAuthTests
{
    private HttpClient _ownerClient = null!;
    private string _charToken = string.Empty;
    private string _charUUID = string.Empty;
    private string _otherCharUUID = string.Empty;

    /// <summary>
    /// Sets up the test server with an owner token and two character tokens.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        var factory = SharedTestServer.Factory;
        _ownerClient = factory.CreateAuthenticatedClient();

        // Create a character token
        var createResponse = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "AuthTestChar" })
            .GetAwaiter().GetResult();
        var createJson = createResponse.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        _charToken = createJson.GetProperty("token").GetString()!;
        _charUUID = createJson.GetProperty("characterUUID").GetString()!;

        // Create another character for cross-access tests
        var otherResponse = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "OtherAuthChar" })
            .GetAwaiter().GetResult();
        var otherJson = otherResponse.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        _otherCharUUID = otherJson.GetProperty("characterUUID").GetString()!;
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
    /// When a non-Owner token tries to access another character's data, returns 403.
    /// Validates: Requirement 2.4.
    /// </summary>
    [Test]
    public async Task GetAll_MismatchedCharacterUUID_Returns403()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.GetAsync(
            $"/api/v1/characters/{_otherCharUUID}/asteroids");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    /// <summary>
    /// When a non-Owner token tries to access another character's single entity, returns 403.
    /// Validates: Requirement 2.4.
    /// </summary>
    [Test]
    public async Task GetOne_MismatchedCharacterUUID_Returns403()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.GetAsync(
            $"/api/v1/characters/{_otherCharUUID}/asteroids/some-entity-uuid");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    /// <summary>
    /// When a non-Owner token tries to create on another character, returns 403.
    /// Validates: Requirement 2.4.
    /// </summary>
    [Test]
    public async Task Create_MismatchedCharacterUUID_Returns403()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_otherCharUUID}/asteroids",
            new { name = "ShouldNotCreate" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    /// <summary>
    /// When the token's Character_UUID matches the URL, the request passes through.
    /// Validates: Requirement 2.2.
    /// </summary>
    [Test]
    public async Task GetAll_MatchingCharacterUUID_PassesThrough()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.GetAsync(
            $"/api/v1/characters/{_charUUID}/asteroids");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// When the token has Owner role, it can access any character's data.
    /// Validates: Requirement 2.3.
    /// </summary>
    [Test]
    public async Task GetAll_OwnerRole_AccessesAnyCharacterData()
    {
        var response = await _ownerClient.GetAsync(
            $"/api/v1/characters/{_charUUID}/asteroids");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// When the token has Owner role, it can access a different character's data.
    /// Validates: Requirement 2.3.
    /// </summary>
    [Test]
    public async Task GetAll_OwnerRole_AccessesOtherCharacterData()
    {
        var response = await _ownerClient.GetAsync(
            $"/api/v1/characters/{_otherCharUUID}/asteroids");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// The 403 response body contains no entity data — only a generic error message.
    /// Validates: Requirement 2.6.
    /// </summary>
    [Test]
    public async Task Forbidden_Response_ContainsNoEntityData()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.GetAsync(
            $"/api/v1/characters/{_otherCharUUID}/asteroids");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Should only contain "error" field with generic message
        Assert.That(root.GetProperty("error").GetString(), Is.EqualTo("Access denied"));

        // Should NOT contain any entity-related fields
        Assert.That(root.TryGetProperty("items", out _), Is.False);
        Assert.That(root.TryGetProperty("uuid", out _), Is.False);
        Assert.That(root.TryGetProperty("name", out _), Is.False);
        Assert.That(root.TryGetProperty("data", out _), Is.False);
    }

    /// <summary>
    /// When a non-Owner token tries to delete on another character, returns 403.
    /// Validates: Requirement 2.4.
    /// </summary>
    [Test]
    public async Task Delete_MismatchedCharacterUUID_Returns403()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.DeleteAsync(
            $"/api/v1/characters/{_otherCharUUID}/asteroids/some-uuid");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }
}
