// -----------------------------------------------------------------------
// <copyright file="CharacterEndpointAuthTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;
using OE2EmpireTracker.Server.Tests;

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Integration tests for character endpoint authorization hardening.
/// Validates: Req 14, Criteria 1-6.
/// </summary>
[TestFixture]
public class CharacterEndpointAuthTests
{
    private TestServerFactory _factory = null!;
    private HttpClient _ownerClient = null!;
    private HttpClient _charAClient = null!;
    private HttpClient _charBClient = null!;
    private string _charAUUID = string.Empty;
    private string _charBUUID = string.Empty;

    /// <summary>
    /// Sets up the test server with two character tokens (A and B) and an owner token.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestServerFactory();
        _factory.SeedOwnerToken();
        _ownerClient = _factory.CreateAuthenticatedClient();

        // Create character A with its own token
        var responseA = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "CharacterA" })
            .GetAwaiter().GetResult();
        var jsonA = responseA.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        var tokenA = jsonA.GetProperty("token").GetString()!;
        _charAUUID = jsonA.GetProperty("characterUUID").GetString()!;
        _charAClient = _factory.CreateAuthenticatedClient(tokenA);

        // Create character B with its own token
        var responseB = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "CharacterB" })
            .GetAwaiter().GetResult();
        var jsonB = responseB.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        var tokenB = jsonB.GetProperty("token").GetString()!;
        _charBUUID = jsonB.GetProperty("characterUUID").GetString()!;
        _charBClient = _factory.CreateAuthenticatedClient(tokenB);
    }

    /// <summary>
    /// Tears down the test server and disposes HTTP clients.
    /// </summary>
    [OneTimeTearDown]
    public void TearDown()
    {
        _charAClient.Dispose();
        _charBClient.Dispose();
        _ownerClient.Dispose();
        _factory.Dispose();
    }

    /// <summary>
    /// GET /characters returns only the caller's own character when no sharing rules exist.
    /// Character A should see only Character A in the list, not Character B.
    /// Validates: Req 14, Criterion 1.
    /// </summary>
    [Test]
    public async Task GetCharacters_NonOwner_ReturnsOnlyAccessibleCharacters()
    {
        var response = await _charAClient.GetAsync("/api/v1/characters");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var characters = await response.Content.ReadFromJsonAsync<JsonElement>();
        var uuids = Enumerable.Range(0, characters.GetArrayLength())
            .Select(i => characters[i].GetProperty("uuid").GetString())
            .ToList();

        Assert.That(uuids, Does.Contain(_charAUUID));
        Assert.That(uuids, Does.Not.Contain(_charBUUID));
    }

    /// <summary>
    /// GET /characters/{uuid} returns 403 when Character A tries to access Character B.
    /// Validates: Req 14, Criteria 2, 6.
    /// </summary>
    [Test]
    public async Task GetCharacter_Denied_Returns403()
    {
        var response = await _charAClient.GetAsync($"/api/v1/characters/{_charBUUID}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("error").GetString(), Is.EqualTo("Access denied"));
    }

    /// <summary>
    /// GET /characters/{uuid} succeeds for Owner token on any character.
    /// Validates: Req 14, Criterion 2 (Owner bypass).
    /// </summary>
    [Test]
    public async Task GetCharacter_Owner_ReturnsCharacter()
    {
        var response = await _ownerClient.GetAsync($"/api/v1/characters/{_charBUUID}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("uuid").GetString(), Is.EqualTo(_charBUUID));
    }

    /// <summary>
    /// GET /characters/{uuid}/capabilities returns 403 when a non-owner character tries to access.
    /// Validates: Req 14, Criterion 3.
    /// </summary>
    [Test]
    public async Task GetCapabilities_NonOwner_Returns403()
    {
        var response = await _charAClient.GetAsync(
            $"/api/v1/characters/{_charBUUID}/capabilities");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("error").GetString(), Is.EqualTo("Access denied"));
    }

    /// <summary>
    /// GET /characters/{uuid}/clearance-levels returns 403 when a non-owner character tries to access.
    /// Validates: Req 14, Criterion 4.
    /// </summary>
    [Test]
    public async Task GetClearanceLevels_NonOwner_Returns403()
    {
        var response = await _charAClient.GetAsync(
            $"/api/v1/characters/{_charBUUID}/clearance-levels");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("error").GetString(), Is.EqualTo("Access denied"));
    }

    /// <summary>
    /// GET /characters/{uuid}/groups returns 403 when a non-owner character tries to access.
    /// Validates: Req 14, Criterion 5.
    /// </summary>
    [Test]
    public async Task GetGroups_NonOwner_Returns403()
    {
        var response = await _charAClient.GetAsync(
            $"/api/v1/characters/{_charBUUID}/groups");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("error").GetString(), Is.EqualTo("Access denied"));
    }

    /// <summary>
    /// GET /characters/{uuid}/capabilities returns 200 when the character owner accesses their own.
    /// Validates: Req 14, Criterion 3 (owner access allowed).
    /// </summary>
    [Test]
    public async Task GetCapabilities_Owner_Returns200()
    {
        var response = await _charAClient.GetAsync(
            $"/api/v1/characters/{_charAUUID}/capabilities");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// GET /characters/{uuid}/capabilities returns 200 when the Owner role token accesses any character.
    /// Validates: Req 14, Criterion 3 (Owner role bypass).
    /// </summary>
    [Test]
    public async Task GetCapabilities_OwnerRole_Returns200()
    {
        var response = await _ownerClient.GetAsync(
            $"/api/v1/characters/{_charBUUID}/capabilities");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
