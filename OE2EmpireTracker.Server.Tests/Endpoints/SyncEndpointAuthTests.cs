// -----------------------------------------------------------------------
// <copyright file="SyncEndpointAuthTests.cs" company="OE2EmpireTracker">
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
/// Integration tests for sync endpoint authorization hardening.
/// Validates: Req 16, Criteria 1-5.
/// </summary>
[TestFixture]
public class SyncEndpointAuthTests
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
            .PostAsJsonAsync("/api/v1/factions", new { name = "SyncTestFaction" })
            .GetAwaiter().GetResult();
        var factionJson = factionResponse.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        _factionUUID = factionJson.GetProperty("uuid").GetString()!;

        // Create member character with its own token
        var memberResponse = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "SyncMember" })
            .GetAwaiter().GetResult();
        var memberJson = memberResponse.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        var memberToken = memberJson.GetProperty("token").GetString()!;
        _memberCharUUID = memberJson.GetProperty("characterUUID").GetString()!;
        _memberClient = _factory.CreateAuthenticatedClient(memberToken);

        // Create non-member character with its own token
        var nonMemberResponse = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "SyncNonMember" })
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
                new { name = "SyncMembers", defaultClearanceLevelUUID = firstLevelUUID })
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
    /// Owner caller gets all factions and all characters in the sync response.
    /// Validates: Req 16, Criterion 3.
    /// </summary>
    [Test]
    public async Task GetSync_Owner_ReturnsAllFactionsAndCharacters()
    {
        var response = await _ownerClient.GetAsync("/api/v1/sync");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var factions = body.GetProperty("factions");
        var characters = body.GetProperty("characters");

        // Owner sees all factions (at least the one we created)
        var factionUUIDs = Enumerable.Range(0, factions.GetArrayLength())
            .Select(i => factions[i].GetProperty("uuid").GetString())
            .ToList();
        Assert.That(factionUUIDs, Does.Contain(_factionUUID));

        // Owner sees all characters (both member and non-member)
        var charUUIDs = Enumerable.Range(0, characters.GetArrayLength())
            .Select(i => characters[i].GetProperty("uuid").GetString())
            .ToList();
        Assert.That(charUUIDs, Does.Contain(_memberCharUUID));
        Assert.That(charUUIDs, Does.Contain(_nonMemberCharUUID));
    }

    /// <summary>
    /// Non-owner caller only sees factions they are a member of.
    /// The member character should see the faction; the non-member should not.
    /// Validates: Req 16, Criteria 1, 5.
    /// </summary>
    [Test]
    public async Task GetSync_NonOwner_ReturnsOnlyMemberFactions()
    {
        // Member should see the faction
        var memberResponse = await _memberClient.GetAsync("/api/v1/sync");
        Assert.That(memberResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var memberBody = await memberResponse.Content.ReadFromJsonAsync<JsonElement>();
        var memberFactions = memberBody.GetProperty("factions");
        var memberFactionUUIDs = Enumerable.Range(0, memberFactions.GetArrayLength())
            .Select(i => memberFactions[i].GetProperty("uuid").GetString())
            .ToList();
        Assert.That(memberFactionUUIDs, Does.Contain(_factionUUID));

        // Non-member should NOT see the faction
        var nonMemberResponse = await _nonMemberClient.GetAsync("/api/v1/sync");
        Assert.That(nonMemberResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var nonMemberBody = await nonMemberResponse.Content.ReadFromJsonAsync<JsonElement>();
        var nonMemberFactions = nonMemberBody.GetProperty("factions");
        var nonMemberFactionUUIDs = Enumerable.Range(0, nonMemberFactions.GetArrayLength())
            .Select(i => nonMemberFactions[i].GetProperty("uuid").GetString())
            .ToList();
        Assert.That(nonMemberFactionUUIDs, Does.Not.Contain(_factionUUID));
    }

    /// <summary>
    /// Non-owner caller only sees their own character (no sharing rules configured).
    /// The member character should see itself but not the non-member character.
    /// Validates: Req 16, Criteria 2, 5.
    /// </summary>
    [Test]
    public async Task GetSync_NonOwner_ReturnsOnlyAccessibleCharacters()
    {
        var response = await _memberClient.GetAsync("/api/v1/sync");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var characters = body.GetProperty("characters");
        var charUUIDs = Enumerable.Range(0, characters.GetArrayLength())
            .Select(i => characters[i].GetProperty("uuid").GetString())
            .ToList();

        // Member sees their own character
        Assert.That(charUUIDs, Does.Contain(_memberCharUUID));

        // Member does NOT see the non-member's character (no sharing rules)
        Assert.That(charUUIDs, Does.Not.Contain(_nonMemberCharUUID));
    }

    /// <summary>
    /// Sync response has the expected JSON structure with factions, characters, and serverTimestamp keys.
    /// Validates: Req 16, Criterion 4.
    /// </summary>
    [Test]
    public async Task GetSync_ResponseFormat_HasExpectedKeys()
    {
        var response = await _memberClient.GetAsync("/api/v1/sync");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Verify all expected keys are present
        Assert.That(body.TryGetProperty("factions", out var factions), Is.True, "Missing 'factions' key");
        Assert.That(body.TryGetProperty("characters", out var characters), Is.True, "Missing 'characters' key");
        Assert.That(body.TryGetProperty("serverTimestamp", out var timestamp), Is.True, "Missing 'serverTimestamp' key");

        // Verify types
        Assert.That(factions.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(characters.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(timestamp.ValueKind, Is.EqualTo(JsonValueKind.String));
    }
}
