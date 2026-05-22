// -----------------------------------------------------------------------
// <copyright file="FactionEndpointAuthTests.cs" company="OE2EmpireTracker">
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
/// Integration tests for faction endpoint authorization hardening.
/// Validates: Req 15, Criteria 1-9.
/// </summary>
[TestFixture]
public class FactionEndpointAuthTests
{
    private TestServerFactory _factory = null!;
    private HttpClient _ownerClient = null!;
    private HttpClient _memberClient = null!;
    private HttpClient _nonMemberClient = null!;
    private string _memberCharUUID = string.Empty;
    private string _nonMemberCharUUID = string.Empty;
    private string _factionUUID = string.Empty;

    /// <summary>
    /// Sets up the test server with a faction, a member character, and a non-member character.
    /// The member is added to the faction via the group membership endpoint.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestServerFactory();
        _factory.SeedOwnerToken();
        _ownerClient = _factory.CreateAuthenticatedClient();

        // Create a faction
        var factionResponse = _ownerClient
            .PostAsJsonAsync("/api/v1/factions", new { name = "AuthTestFaction" })
            .GetAwaiter().GetResult();
        var factionJson = factionResponse.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        _factionUUID = factionJson.GetProperty("uuid").GetString()!;

        // Create member character with its own token
        var memberResponse = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "FactionMember" })
            .GetAwaiter().GetResult();
        var memberJson = memberResponse.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        var memberToken = memberJson.GetProperty("token").GetString()!;
        _memberCharUUID = memberJson.GetProperty("characterUUID").GetString()!;
        _memberClient = _factory.CreateAuthenticatedClient(memberToken);

        // Create non-member character with its own token
        var nonMemberResponse = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "NonMember" })
            .GetAwaiter().GetResult();
        var nonMemberJson = nonMemberResponse.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        var nonMemberToken = nonMemberJson.GetProperty("token").GetString()!;
        _nonMemberCharUUID = nonMemberJson.GetProperty("characterUUID").GetString()!;
        _nonMemberClient = _factory.CreateAuthenticatedClient(nonMemberToken);

        // Get the seeded clearance levels for the faction
        var levelsResponse = _ownerClient
            .GetAsync($"/api/v1/factions/{_factionUUID}/clearance-levels")
            .GetAwaiter().GetResult();
        var levelsJson = levelsResponse.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        var firstLevelUUID = levelsJson[0].GetProperty("uuid").GetString()!;

        // Create a group in the faction
        var groupResponse = _ownerClient
            .PostAsJsonAsync(
                $"/api/v1/factions/{_factionUUID}/groups",
                new { name = "Members", defaultClearanceLevelUUID = firstLevelUUID })
            .GetAwaiter().GetResult();
        var groupJson = groupResponse.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        var groupUUID = groupJson.GetProperty("uuid").GetString()!;

        // Add the member character to the faction group
        _ownerClient
            .PutAsJsonAsync(
                $"/api/v1/factions/{_factionUUID}/groups/{groupUUID}/members",
                new { characterUUID = _memberCharUUID })
            .GetAwaiter().GetResult();
    }

    /// <summary>
    /// Tears down the test server and disposes HTTP clients.
    /// </summary>
    [OneTimeTearDown]
    public void TearDown()
    {
        _memberClient.Dispose();
        _nonMemberClient.Dispose();
        _ownerClient.Dispose();
        _factory.Dispose();
    }

    /// <summary>
    /// GET /factions returns 200 for any authenticated user.
    /// Validates: Req 15, Criterion 1.
    /// </summary>
    [Test]
    public async Task GetFactions_AnyAuthenticated_Returns200()
    {
        var response = await _nonMemberClient.GetAsync("/api/v1/factions");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var factions = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(factions.GetArrayLength(), Is.GreaterThanOrEqualTo(1));
    }

    /// <summary>
    /// GET /factions/{uuid} returns 200 for any authenticated user.
    /// Validates: Req 15, Criterion 2.
    /// </summary>
    [Test]
    public async Task GetFaction_AnyAuthenticated_Returns200()
    {
        var response = await _nonMemberClient.GetAsync(
            $"/api/v1/factions/{_factionUUID}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("uuid").GetString(), Is.EqualTo(_factionUUID));
    }

    /// <summary>
    /// GET /factions/{uuid}/leaders returns 200 for any authenticated user.
    /// Validates: Req 15, Criterion 7.
    /// </summary>
    [Test]
    public async Task GetLeaders_AnyAuthenticated_Returns200()
    {
        var response = await _nonMemberClient.GetAsync(
            $"/api/v1/factions/{_factionUUID}/leaders");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// GET /factions/{uuid}/capabilities returns 403 for a non-member.
    /// Validates: Req 15, Criterion 3.
    /// </summary>
    [Test]
    public async Task GetCapabilities_NonMember_Returns403()
    {
        var response = await _nonMemberClient.GetAsync(
            $"/api/v1/factions/{_factionUUID}/capabilities");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("error").GetString(), Is.EqualTo("Access denied"));
    }

    /// <summary>
    /// GET /factions/{uuid}/clearance-levels returns 403 for a non-member.
    /// Validates: Req 15, Criterion 4.
    /// </summary>
    [Test]
    public async Task GetClearanceLevels_NonMember_Returns403()
    {
        var response = await _nonMemberClient.GetAsync(
            $"/api/v1/factions/{_factionUUID}/clearance-levels");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("error").GetString(), Is.EqualTo("Access denied"));
    }

    /// <summary>
    /// GET /factions/{uuid}/groups returns 403 for a non-member.
    /// Validates: Req 15, Criterion 5.
    /// </summary>
    [Test]
    public async Task GetGroups_NonMember_Returns403()
    {
        var response = await _nonMemberClient.GetAsync(
            $"/api/v1/factions/{_factionUUID}/groups");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("error").GetString(), Is.EqualTo("Access denied"));
    }

    /// <summary>
    /// GET /factions/{uuid}/members returns 403 for a non-member.
    /// Validates: Req 15, Criterion 6.
    /// </summary>
    [Test]
    public async Task GetMembers_NonMember_Returns403()
    {
        var response = await _nonMemberClient.GetAsync(
            $"/api/v1/factions/{_factionUUID}/members");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(body.GetProperty("error").GetString(), Is.EqualTo("Access denied"));
    }

    /// <summary>
    /// GET /factions/{uuid}/capabilities returns 200 for the Owner role.
    /// Validates: Req 15, Criterion 3 (Owner bypass).
    /// </summary>
    [Test]
    public async Task GetCapabilities_Owner_Returns200()
    {
        var response = await _ownerClient.GetAsync(
            $"/api/v1/factions/{_factionUUID}/capabilities");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// GET /factions/{uuid}/capabilities returns 200 for a faction member.
    /// Validates: Req 15, Criterion 3 (member access).
    /// </summary>
    [Test]
    public async Task GetCapabilities_Member_Returns200()
    {
        var response = await _memberClient.GetAsync(
            $"/api/v1/factions/{_factionUUID}/capabilities");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// GET /factions/{uuid}/clearance-levels returns 200 for a faction member.
    /// Validates: Req 15, Criterion 4 (member access).
    /// </summary>
    [Test]
    public async Task GetClearanceLevels_Member_Returns200()
    {
        var response = await _memberClient.GetAsync(
            $"/api/v1/factions/{_factionUUID}/clearance-levels");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// GET /factions/{uuid}/groups returns 200 for a faction member.
    /// Validates: Req 15, Criterion 5 (member access).
    /// </summary>
    [Test]
    public async Task GetGroups_Member_Returns200()
    {
        var response = await _memberClient.GetAsync(
            $"/api/v1/factions/{_factionUUID}/groups");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// GET /factions/{uuid}/members returns 200 for a faction member.
    /// Validates: Req 15, Criterion 6 (member access).
    /// </summary>
    [Test]
    public async Task GetMembers_Member_Returns200()
    {
        var response = await _memberClient.GetAsync(
            $"/api/v1/factions/{_factionUUID}/members");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
