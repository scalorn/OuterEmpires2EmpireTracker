// -----------------------------------------------------------------------
// <copyright file="BlueprintImportMoveTests.cs" company="OE2EmpireTracker">
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
/// Unit tests for Blueprint import and move actions.
/// Validates: Requirements 5.8, 5.9, 5.10.
/// </summary>
[TestFixture]
public class BlueprintImportMoveTests
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
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "BlueprintTestChar" })
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
    /// Import with a new UUID creates a new blueprint and returns 201.
    /// Validates: Requirement 5.8.
    /// </summary>
    [Test]
    public async Task Import_NewBlueprint_Returns201()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);
        var newUuid = Guid.NewGuid().ToString();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/blueprints/import",
            new
            {
                uuid = newUuid,
                name = "Imported BP",
                bluePrintType = "Hull",
            });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("uuid").GetString(), Is.EqualTo(newUuid));
        Assert.That(body.GetProperty("name").GetString(), Is.EqualTo("Imported BP"));
    }

    /// <summary>
    /// Import with an existing UUID merges and returns 200.
    /// Validates: Requirement 5.8.
    /// </summary>
    [Test]
    public async Task Import_ExistingBlueprint_Returns200()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);
        var bpUuid = Guid.NewGuid().ToString();

        // First import creates
        await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/blueprints/import",
            new
            {
                uuid = bpUuid,
                name = "Original BP",
                bluePrintType = "Hull",
            });

        // Second import with same UUID merges
        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/blueprints/import",
            new
            {
                uuid = bpUuid,
                name = "Updated BP",
                bluePrintType = "Hull",
            });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("uuid").GetString(), Is.EqualTo(bpUuid));
        Assert.That(body.GetProperty("name").GetString(), Is.EqualTo("Updated BP"));
    }

    /// <summary>
    /// Move-to-global on an existing blueprint returns 200 with empty OwnerUUID.
    /// Validates: Requirement 5.9.
    /// </summary>
    [Test]
    public async Task MoveToGlobal_ExistingBlueprint_Returns200()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);

        // Create a blueprint first
        var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/blueprints",
            new { name = "GlobalBP", bluePrintType = "Hull" });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var bpUuid = created.GetProperty("uuid").GetString()!;

        // Move to global
        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/blueprints/{bpUuid}/move-to-global",
            new { });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("ownerUUID").GetString(), Is.EqualTo(string.Empty));
    }

    /// <summary>
    /// Move-to-global on a non-existent blueprint returns 404.
    /// Validates: Requirement 5.9.
    /// </summary>
    [Test]
    public async Task MoveToGlobal_NotFound_Returns404()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/blueprints/nonexistent-uuid/move-to-global",
            new { });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Move-to-player on an existing blueprint returns 200 with OwnerUUID set to character.
    /// Validates: Requirement 5.10.
    /// </summary>
    [Test]
    public async Task MoveToPlayer_ExistingBlueprint_Returns200()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);

        // Create a blueprint and move to global first
        var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/blueprints",
            new { name = "PlayerBP", bluePrintType = "Hull" });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var bpUuid = created.GetProperty("uuid").GetString()!;

        await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/blueprints/{bpUuid}/move-to-global",
            new { });

        // Move back to player
        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/blueprints/{bpUuid}/move-to-player",
            new { });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("ownerUUID").GetString(), Is.EqualTo(_charUUID));
    }

    /// <summary>
    /// Move-to-player on a non-existent blueprint returns 404.
    /// Validates: Requirement 5.10.
    /// </summary>
    [Test]
    public async Task MoveToPlayer_NotFound_Returns404()
    {
        using var client = _factory.CreateAuthenticatedClient(_charToken);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/blueprints/nonexistent-uuid/move-to-player",
            new { });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
