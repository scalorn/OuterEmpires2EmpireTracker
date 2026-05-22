// -----------------------------------------------------------------------
// <copyright file="IntelEndpointAuthTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;
using OE2EmpireTracker.Server.Tests;

#pragma warning disable SA1009 // Closing parenthesis should be followed by a space

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Integration tests for intel endpoint authorization hardening.
/// Validates: Req 17, Criteria 1-6.
/// </summary>
[TestFixture]
public class IntelEndpointAuthTests
{
    private TestServerFactory _factory = null!;
    private HttpClient _ownerClient = null!;
    private HttpClient _charAClient = null!;
    private HttpClient _charBClient = null!;
    private string _charAUUID = string.Empty;
    private string _charBUUID = string.Empty;
    private string _factionUUID = string.Empty;
    private string _leaderCharUUID = string.Empty;
    private HttpClient _leaderClient = null!;

    /// <summary>
    /// Sets up the test server with two characters (A and B), a faction,
    /// and a leader character for classify tests.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestServerFactory();
        _factory.SeedOwnerToken();
        _ownerClient = _factory.CreateAuthenticatedClient();

        // Create character A with its own token
        var responseA = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "IntelCharA" })
            .GetAwaiter().GetResult();
        var jsonA = responseA.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        var tokenA = jsonA.GetProperty("token").GetString()!;
        _charAUUID = jsonA.GetProperty("characterUUID").GetString()!;
        _charAClient = _factory.CreateAuthenticatedClient(tokenA);

        // Create character B with its own token
        var responseB = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "IntelCharB" })
            .GetAwaiter().GetResult();
        var jsonB = responseB.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        var tokenB = jsonB.GetProperty("token").GetString()!;
        _charBUUID = jsonB.GetProperty("characterUUID").GetString()!;
        _charBClient = _factory.CreateAuthenticatedClient(tokenB);

        // Create a faction for classify tests
        var factionResponse = _ownerClient
            .PostAsJsonAsync("/api/v1/factions", new { name = "IntelTestFaction" })
            .GetAwaiter().GetResult();
        var factionJson = factionResponse.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        _factionUUID = factionJson.GetProperty("uuid").GetString()!;

        // Create a leader character with its own token
        var leaderResponse = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "IntelLeader" })
            .GetAwaiter().GetResult();
        var leaderJson = leaderResponse.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        var leaderToken = leaderJson.GetProperty("token").GetString()!;
        _leaderCharUUID = leaderJson.GetProperty("characterUUID").GetString()!;
        _leaderClient = _factory.CreateAuthenticatedClient(leaderToken);

        // Add the leader character as a faction leader
        _ownerClient
            .PutAsJsonAsync(
                $"/api/v1/factions/{_factionUUID}/leaders",
                new { characterUUID = _leaderCharUUID })
            .GetAwaiter().GetResult();
    }

    /// <summary>
    /// Tears down the test server and disposes HTTP clients.
    /// </summary>
    [OneTimeTearDown]
    public void TearDown()
    {
        _charAClient.Dispose();
        _charBClient.Dispose();
        _leaderClient.Dispose();
        _ownerClient.Dispose();
        _factory.Dispose();
    }

    /// <summary>
    /// POST /intel rejects when token character UUID does not match URL character UUID.
    /// Character B's token tries to create intel on Character A → 403.
    /// Validates: Req 17, Criterion 1.
    /// </summary>
    [Test]
    public async Task CreateComment_MismatchedCharacterUUID_Returns403()
    {
        var response = await _charBClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charAUUID}/intel",
            new { text = "Unauthorized comment" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("error").GetString(), Is.EqualTo("Access denied"));
    }

    /// <summary>
    /// GET /intel returns 403 when Character A tries to read Character B's intel
    /// without any sharing rules granting access.
    /// Validates: Req 17, Criterion 2.
    /// </summary>
    [Test]
    public async Task GetComments_NoAccess_Returns403()
    {
        var response = await _charAClient.GetAsync(
            $"/api/v1/characters/{_charBUUID}/intel");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("error").GetString(), Is.EqualTo("Access denied"));
    }

    /// <summary>
    /// DELETE /intel returns 403 when Character A tries to delete a comment
    /// submitted by Character B.
    /// Validates: Req 17, Criterion 3.
    /// </summary>
    [Test]
    public async Task DeleteComment_NotOwner_Returns403()
    {
        // Character B creates a comment on their own intel
        var createResponse = await _charBClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charBUUID}/intel",
            new { text = "B's comment for delete test" });
        Assert.That(
            createResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));
        var created = await createResponse.Content
            .ReadFromJsonAsync<JsonElement>();
        var commentId = created.GetProperty("uuid").GetString()!;

        // Character A tries to delete it
        var response = await _charAClient.DeleteAsync(
            $"/api/v1/characters/{_charBUUID}/intel/{commentId}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("error").GetString(), Is.EqualTo("Access denied"));
    }

    /// <summary>
    /// POST /intel/{commentId}/share returns 403 when Character A tries to share
    /// a comment submitted by Character B.
    /// Validates: Req 17, Criterion 4.
    /// </summary>
    [Test]
    public async Task ShareComment_NotOwner_Returns403()
    {
        // Character B creates a comment on their own intel
        var createResponse = await _charBClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charBUUID}/intel",
            new { text = "B's comment for share test" });
        Assert.That(
            createResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));
        var created = await createResponse.Content
            .ReadFromJsonAsync<JsonElement>();
        var commentId = created.GetProperty("uuid").GetString()!;

        // Character A tries to share it
        var response = await _charAClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charBUUID}/intel/{commentId}/share",
            new { factionUUID = _factionUUID });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("error").GetString(), Is.EqualTo("Access denied"));
    }

    /// <summary>
    /// DELETE /intel/{commentId}/share/{factionUUID} returns 403 when Character A
    /// tries to revoke a share on a comment submitted by Character B.
    /// Validates: Req 17, Criterion 5.
    /// </summary>
    [Test]
    public async Task RevokeShare_NotOwner_Returns403()
    {
        // Character B creates a comment and shares it
        var createResponse = await _charBClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charBUUID}/intel",
            new { text = "B's comment for revoke test" });
        Assert.That(
            createResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));
        var created = await createResponse.Content
            .ReadFromJsonAsync<JsonElement>();
        var commentId = created.GetProperty("uuid").GetString()!;

        // Character B shares the comment with the faction
        var shareResponse = await _charBClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charBUUID}/intel/{commentId}/share",
            new { factionUUID = _factionUUID });
        Assert.That(
            shareResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));

        // Character A tries to revoke the share
        var response = await _charAClient.DeleteAsync(
            $"/api/v1/characters/{_charBUUID}/intel/{commentId}/share/{_factionUUID}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("error").GetString(), Is.EqualTo("Access denied"));
    }

    /// <summary>
    /// PUT /factions/{uuid}/intel/{shareId}/classify returns 403 when a non-leader
    /// character tries to classify an intel comment.
    /// Validates: Req 17, Criterion 6.
    /// </summary>
    [Test]
    public async Task ClassifyComment_NotLeader_Returns403()
    {
        // Character A creates a comment and shares it with the faction
        var createResponse = await _charAClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charAUUID}/intel",
            new { text = "A's comment for classify test" });
        Assert.That(
            createResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));
        var created = await createResponse.Content
            .ReadFromJsonAsync<JsonElement>();
        var commentId = created.GetProperty("uuid").GetString()!;

        // Share the comment with the faction (as owner of the comment)
        var shareResponse = await _charAClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charAUUID}/intel/{commentId}/share",
            new { factionUUID = _factionUUID });
        Assert.That(
            shareResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));
        var shareJson = await shareResponse.Content
            .ReadFromJsonAsync<JsonElement>();
        var shareId = shareJson.GetProperty("uuid").GetString()!;

        // Get a clearance level UUID for classification
        var levelsResponse = await _ownerClient.GetAsync(
            $"/api/v1/factions/{_factionUUID}/clearance-levels");
        var levels = await levelsResponse.Content
            .ReadFromJsonAsync<JsonElement>();
        var levelUUID = levels[0].GetProperty("uuid").GetString()!;

        // Character B (non-leader) tries to classify
        var response = await _charBClient.PutAsJsonAsync(
            $"/api/v1/factions/{_factionUUID}/intel/{shareId}/classify",
            new { classificationLevelUUID = levelUUID });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("error").GetString(), Is.EqualTo("Access denied"));
    }

    /// <summary>
    /// POST /intel succeeds when the token's character UUID matches the URL character UUID.
    /// Character A can create intel on their own character → 201.
    /// Validates: Req 17, Criterion 1 (positive case).
    /// </summary>
    [Test]
    public async Task CreateComment_OwnerCharacter_Returns201()
    {
        var response = await _charAClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charAUUID}/intel",
            new { text = "A's own comment" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(
            body.GetProperty("submitterCharacterUUID").GetString(),
            Is.EqualTo(_charAUUID));
    }

    /// <summary>
    /// PUT /factions/{uuid}/intel/{shareId}/classify succeeds when the caller
    /// is a faction leader.
    /// Validates: Req 17, Criterion 6 (positive case).
    /// </summary>
    [Test]
    public async Task ClassifyComment_Leader_Returns200()
    {
        // Character A creates a comment and shares it with the faction
        var createResponse = await _charAClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charAUUID}/intel",
            new { text = "A's comment for leader classify" });
        Assert.That(
            createResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));
        var created = await createResponse.Content
            .ReadFromJsonAsync<JsonElement>();
        var commentId = created.GetProperty("uuid").GetString()!;

        // Share the comment with the faction
        var shareResponse = await _charAClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charAUUID}/intel/{commentId}/share",
            new { factionUUID = _factionUUID });
        Assert.That(
            shareResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));
        var shareJson = await shareResponse.Content
            .ReadFromJsonAsync<JsonElement>();
        var shareId = shareJson.GetProperty("uuid").GetString()!;

        // Get a clearance level UUID for classification
        var levelsResponse = await _ownerClient.GetAsync(
            $"/api/v1/factions/{_factionUUID}/clearance-levels");
        var levels = await levelsResponse.Content
            .ReadFromJsonAsync<JsonElement>();
        var levelUUID = levels[0].GetProperty("uuid").GetString()!;

        // Leader classifies the comment
        var response = await _leaderClient.PutAsJsonAsync(
            $"/api/v1/factions/{_factionUUID}/intel/{shareId}/classify",
            new { classificationLevelUUID = levelUUID });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
