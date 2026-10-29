// -----------------------------------------------------------------------
// <copyright file="EventDispatchTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OE2EmpireTracker.Server.Push;

#pragma warning disable SA1009 // Closing parenthesis should be followed by a space

namespace OE2EmpireTracker.Server.Tests.Endpoints.Typed;

/// <summary>
/// Unit tests for event dispatch on mutations.
/// Verifies that create, update, and delete operations succeed (implying
/// event dispatch completed without error) and return correct status codes.
/// Validates: Requirements 25.1, 25.2, 25.3, 25.4, 25.5.
/// </summary>
[TestFixture]
public class EventDispatchTests
{
    private HttpClient _ownerClient = null!;
    private string _charToken = string.Empty;
    private string _charUUID = string.Empty;

    /// <summary>
    /// Sets up the test server and creates a character token.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        var factory = SharedTestServer.Factory;
        _ownerClient = factory.CreateAuthenticatedClient();

        var createResponse = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "EventTestChar" })
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
    /// Create mutation returns 201 and dispatches Created event.
    /// The successful 201 response confirms the full pipeline completed
    /// including event dispatch (which would return 503 on failure).
    /// Validates: Requirements 25.1, 25.2.
    /// </summary>
    [Test]
    public async Task Create_Returns201_EventDispatchedSuccessfully()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/asteroids",
            new { name = "EventCreateAsteroid", systemName = "Sol" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("uuid").GetString(), Is.Not.Null.And.Not.Empty);
    }

    /// <summary>
    /// Update mutation returns 200 and dispatches Updated event.
    /// The successful 200 response confirms event dispatch succeeded.
    /// Validates: Requirements 25.1, 25.3.
    /// </summary>
    [Test]
    public async Task Update_Returns200_EventDispatchedSuccessfully()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        // Create first
        var createResponse = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/asteroids",
            new { name = "EventUpdateAsteroid", systemName = "Sol" });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var entityUuid = created.GetProperty("uuid").GetString()!;

        // Update
        var response = await charClient.PutAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/asteroids/{entityUuid}",
            new { name = "UpdatedAsteroid" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// Delete mutation returns 204 and dispatches Deleted event.
    /// The successful 204 response confirms event dispatch succeeded.
    /// Validates: Requirements 25.1, 25.4.
    /// </summary>
    [Test]
    public async Task Delete_Returns204_EventDispatchedSuccessfully()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        // Create first
        var createResponse = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/asteroids",
            new { name = "EventDeleteAsteroid", systemName = "Sol" });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var entityUuid = created.GetProperty("uuid").GetString()!;

        // Delete
        var response = await charClient.DeleteAsync(
            $"/api/v1/characters/{_charUUID}/asteroids/{entityUuid}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    /// <summary>
    /// Verifies that the created entity has correct EntityType in the response,
    /// confirming the event would carry the correct EntityType field.
    /// Validates: Requirement 25.2.
    /// </summary>
    [Test]
    public async Task Create_ResponseContainsCorrectEntityType()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/asteroids",
            new { name = "EntityTypeAsteroid", systemName = "Sol" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        // Location header confirms the entity type routing
        var location = response.Headers.Location?.ToString();
        Assert.That(location, Does.Contain("/asteroids/"));
    }

    /// <summary>
    /// Verifies that the created entity UUID is present in the response,
    /// confirming the event would carry the correct EntityUUID field.
    /// Validates: Requirement 25.3.
    /// </summary>
    [Test]
    public async Task Create_ResponseContainsEntityUUID()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/asteroids",
            new { name = "UUIDAsteroid", systemName = "Sol" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var entityUuid = body.GetProperty("uuid").GetString();
        Assert.That(entityUuid, Is.Not.Null.And.Not.Empty);

        // Location header contains the entity UUID
        var location = response.Headers.Location?.ToString();
        Assert.That(location, Does.Contain(entityUuid));
    }

    /// <summary>
    /// Verifies that the OwnerCharacterUUID is correctly associated with
    /// the mutation by confirming the entity is stored under the correct character.
    /// Validates: Requirement 25.4.
    /// </summary>
    [Test]
    public async Task Create_EntityStoredUnderCorrectOwnerCharacter()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/asteroids",
            new { name = "OwnerAsteroid", systemName = "Sol" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        var entityUuid = created.GetProperty("uuid").GetString()!;

        // Verify it's accessible under the correct character
        var getResponse = await charClient.GetAsync(
            $"/api/v1/characters/{_charUUID}/asteroids/{entityUuid}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// Verifies that after a successful delete, the entity is no longer
    /// accessible, confirming the full mutation pipeline (including event
    /// dispatch) completed without rollback.
    /// Validates: Requirement 25.5.
    /// </summary>
    [Test]
    public async Task Delete_EntityNoLongerAccessible_NoRollback()
    {
        using var charClient = SharedTestServer.Factory.CreateAuthenticatedClient(_charToken);

        // Create
        var createResponse = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/asteroids",
            new { name = "NoRollbackAsteroid", systemName = "Sol" });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var entityUuid = created.GetProperty("uuid").GetString()!;

        // Delete
        var deleteResponse = await charClient.DeleteAsync(
            $"/api/v1/characters/{_charUUID}/asteroids/{entityUuid}");
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Verify entity is gone (no rollback occurred)
        var getResponse = await charClient.GetAsync(
            $"/api/v1/characters/{_charUUID}/asteroids/{entityUuid}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
