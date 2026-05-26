// -----------------------------------------------------------------------
// <copyright file="Property6_SyncResponseFilteringTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Property 6: Sync Response Filtering.
/// For any non-Owner caller, the sync response SHALL contain only:
/// (a) factions where the caller is a member, and
/// (b) characters where CanAccessCharacterData returns true.
/// The response SHALL NOT contain any faction or character outside these sets.
/// **Validates: Req 16, Criteria 1-2, 5**
/// </summary>
[TestFixture]
public class Property6_SyncResponseFilteringTests
{
    private HttpClient _ownerClient = null!;

    private TestServerFactory Factory => SharedTestServer.Factory;

    /// <summary>
    /// Sets up the test server and seeds the owner token.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        _ownerClient = SharedTestServer.Factory.CreateAuthenticatedClient();
    }

    /// <summary>
    /// Tears down the test server.
    /// </summary>
    [OneTimeTearDown]
    public void TearDown()
    {
        _ownerClient.Dispose();
    }

    /// <summary>
    /// Property: sync response never contains factions the caller is NOT a member of.
    /// Creates multiple factions, assigns the caller to a random subset, then verifies
    /// the sync response contains exactly the member factions and no others.
    /// </summary>
    [Test]
    public async Task SyncFiltering_CallerSeesOnlyMemberFactions()
    {
        var rng = new Random(42);

        for (int iteration = 0; iteration < 5; iteration++)
        {
            // Create a caller character
            var callerResp = await _ownerClient.PostAsJsonAsync(
                "/api/v1/tokens",
                new { characterName = $"P6FacIter{iteration}" });
            var callerJson = await callerResp.Content.ReadFromJsonAsync<JsonElement>();
            var callerToken = callerJson.GetProperty("token").GetString()!;
            var callerUUID = callerJson.GetProperty("characterUUID").GetString()!;

            // Create 2-4 factions
            int factionCount = rng.Next(2, 5);
            var allFactionUUIDs = new List<string>();
            var memberFactionUUIDs = new HashSet<string>();

            for (int f = 0; f < factionCount; f++)
            {
                var facResp = await _ownerClient.PostAsJsonAsync(
                    "/api/v1/factions",
                    new { name = $"P6Fac{iteration}_{f}" });
                var facJson = await facResp.Content.ReadFromJsonAsync<JsonElement>();
                var facUUID = facJson.GetProperty("uuid").GetString()!;
                allFactionUUIDs.Add(facUUID);

                // Randomly decide if caller is a member of this faction
                bool isMember = rng.Next(2) == 1;
                if (isMember)
                {
                    await AddCharacterToFaction(callerUUID, facUUID);
                    memberFactionUUIDs.Add(facUUID);
                }
            }

            // Call sync as the caller
            using var callerClient = Factory.CreateAuthenticatedClient(callerToken);
            var syncResp = await callerClient.GetAsync("/api/v1/sync");
            Assert.That(syncResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var syncBody = await syncResp.Content.ReadFromJsonAsync<JsonElement>();
            var factions = syncBody.GetProperty("factions");
            var returnedFactionUUIDs = new HashSet<string>();
            for (int i = 0; i < factions.GetArrayLength(); i++)
            {
                returnedFactionUUIDs.Add(factions[i].GetProperty("uuid").GetString()!);
            }

            // Assert: every returned faction is one the caller is a member of
            foreach (var returnedUUID in returnedFactionUUIDs)
            {
                if (allFactionUUIDs.Contains(returnedUUID))
                {
                    Assert.That(
                        memberFactionUUIDs,
                        Does.Contain(returnedUUID),
                        $"Iteration {iteration}: sync returned faction {returnedUUID} " +
                        $"but caller is NOT a member.");
                }
            }

            // Assert: factions the caller IS a member of ARE in the response
            foreach (var memberUUID in memberFactionUUIDs)
            {
                Assert.That(
                    returnedFactionUUIDs,
                    Does.Contain(memberUUID),
                    $"Iteration {iteration}: caller is a member of faction {memberUUID} " +
                    $"but it was NOT in the sync response.");
            }

            // Assert: factions the caller is NOT a member of are NOT in the response
            foreach (var facUUID in allFactionUUIDs)
            {
                if (!memberFactionUUIDs.Contains(facUUID))
                {
                    Assert.That(
                        returnedFactionUUIDs,
                        Does.Not.Contain(facUUID),
                        $"Iteration {iteration}: caller is NOT a member of faction {facUUID} " +
                        $"but it WAS in the sync response.");
                }
            }
        }
    }

    /// <summary>
    /// Property: sync response never contains characters the caller has no access to.
    /// Creates multiple characters, grants the caller access to a random subset
    /// (via sharing rules), then verifies the sync response contains exactly
    /// the accessible characters and no others.
    /// </summary>
    [Test]
    public async Task SyncFiltering_CallerSeesOnlyAccessibleCharacters()
    {
        var rng = new Random(99);

        for (int iteration = 0; iteration < 5; iteration++)
        {
            // Create a caller character
            var callerResp = await _ownerClient.PostAsJsonAsync(
                "/api/v1/tokens",
                new { characterName = $"P6CharIter{iteration}" });
            var callerJson = await callerResp.Content.ReadFromJsonAsync<JsonElement>();
            var callerToken = callerJson.GetProperty("token").GetString()!;
            var callerUUID = callerJson.GetProperty("characterUUID").GetString()!;

            // Create 2-4 other characters
            int otherCount = rng.Next(2, 5);
            var otherCharUUIDs = new List<string>();
            var accessibleCharUUIDs = new HashSet<string> { callerUUID }; // Caller always sees self

            for (int c = 0; c < otherCount; c++)
            {
                var otherResp = await _ownerClient.PostAsJsonAsync(
                    "/api/v1/tokens",
                    new { characterName = $"P6Other{iteration}_{c}" });
                var otherJson = await otherResp.Content.ReadFromJsonAsync<JsonElement>();
                var otherToken = otherJson.GetProperty("token").GetString()!;
                var otherUUID = otherJson.GetProperty("characterUUID").GetString()!;
                otherCharUUIDs.Add(otherUUID);

                // Randomly decide if this character shares with the caller
                bool isShared = rng.Next(2) == 1;
                if (isShared)
                {
                    // Create a sharing rule from the other character targeting the caller
                    using var otherClient = Factory.CreateAuthenticatedClient(otherToken);
                    await otherClient.PutAsJsonAsync(
                        $"/api/v1/characters/{otherUUID}/sharing",
                        new[]
                        {
                            new
                            {
                                id = Guid.NewGuid().ToString(),
                                ownerCharacterUUID = otherUUID,
                                targetType = "Character",
                                targetUUID = callerUUID,
                            },
                        });
                    accessibleCharUUIDs.Add(otherUUID);
                }
            }

            // Call sync as the caller
            using var callerClient = Factory.CreateAuthenticatedClient(callerToken);
            var syncResp = await callerClient.GetAsync("/api/v1/sync");
            Assert.That(syncResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var syncBody = await syncResp.Content.ReadFromJsonAsync<JsonElement>();
            var characters = syncBody.GetProperty("characters");
            var returnedCharUUIDs = new HashSet<string>();
            for (int i = 0; i < characters.GetArrayLength(); i++)
            {
                returnedCharUUIDs.Add(characters[i].GetProperty("uuid").GetString()!);
            }

            // Assert: caller's own character is always in the response
            Assert.That(
                returnedCharUUIDs,
                Does.Contain(callerUUID),
                $"Iteration {iteration}: caller's own character not in sync response.");

            // Assert: every returned character (from our test set) is accessible
            foreach (var returnedUUID in returnedCharUUIDs)
            {
                if (otherCharUUIDs.Contains(returnedUUID))
                {
                    Assert.That(
                        accessibleCharUUIDs,
                        Does.Contain(returnedUUID),
                        $"Iteration {iteration}: sync returned character {returnedUUID} " +
                        $"but caller has NO access.");
                }
            }

            // Assert: characters the caller has access to ARE in the response
            foreach (var accessibleUUID in accessibleCharUUIDs)
            {
                Assert.That(
                    returnedCharUUIDs,
                    Does.Contain(accessibleUUID),
                    $"Iteration {iteration}: caller has access to character {accessibleUUID} " +
                    $"but it was NOT in the sync response.");
            }

            // Assert: characters the caller does NOT have access to are NOT in the response
            foreach (var otherUUID in otherCharUUIDs)
            {
                if (!accessibleCharUUIDs.Contains(otherUUID))
                {
                    Assert.That(
                        returnedCharUUIDs,
                        Does.Not.Contain(otherUUID),
                        $"Iteration {iteration}: caller has NO access to character {otherUUID} " +
                        $"but it WAS in the sync response.");
                }
            }
        }
    }

    /// <summary>
    /// Property: sync response filtering works correctly with mixed permission types.
    /// Creates a scenario with faction membership AND character sharing rules,
    /// verifying both access paths are respected simultaneously.
    /// </summary>
    [Test]
    public async Task SyncFiltering_MixedPermissions_CorrectFiltering()
    {
        var rng = new Random(2024);

        for (int iteration = 0; iteration < 5; iteration++)
        {
            // Create a caller character
            var callerResp = await _ownerClient.PostAsJsonAsync(
                "/api/v1/tokens",
                new { characterName = $"P6Mix{iteration}" });
            var callerJson = await callerResp.Content.ReadFromJsonAsync<JsonElement>();
            var callerToken = callerJson.GetProperty("token").GetString()!;
            var callerUUID = callerJson.GetProperty("characterUUID").GetString()!;

            // Create a faction and randomly add the caller
            var facResp = await _ownerClient.PostAsJsonAsync(
                "/api/v1/factions",
                new { name = $"P6MixFac{iteration}" });
            var facJson = await facResp.Content.ReadFromJsonAsync<JsonElement>();
            var facUUID = facJson.GetProperty("uuid").GetString()!;

            bool callerIsMember = rng.Next(2) == 1;
            if (callerIsMember)
            {
                await AddCharacterToFaction(callerUUID, facUUID);
            }

            // Create another faction the caller is never a member of
            var facResp2 = await _ownerClient.PostAsJsonAsync(
                "/api/v1/factions",
                new { name = $"P6MixFac{iteration}B" });
            var facJson2 = await facResp2.Content.ReadFromJsonAsync<JsonElement>();
            var facUUID2 = facJson2.GetProperty("uuid").GetString()!;

            // Create another character that may or may not share with caller
            var otherResp = await _ownerClient.PostAsJsonAsync(
                "/api/v1/tokens",
                new { characterName = $"P6MixOther{iteration}" });
            var otherJson = await otherResp.Content.ReadFromJsonAsync<JsonElement>();
            var otherToken = otherJson.GetProperty("token").GetString()!;
            var otherUUID = otherJson.GetProperty("characterUUID").GetString()!;

            bool otherShares = rng.Next(2) == 1;
            if (otherShares)
            {
                using var otherClient = Factory.CreateAuthenticatedClient(otherToken);
                await otherClient.PutAsJsonAsync(
                    $"/api/v1/characters/{otherUUID}/sharing",
                    new[]
                    {
                        new
                        {
                            id = Guid.NewGuid().ToString(),
                            ownerCharacterUUID = otherUUID,
                            targetType = "Character",
                            targetUUID = callerUUID,
                        },
                    });
            }

            // Call sync as the caller
            using var callerClient = Factory.CreateAuthenticatedClient(callerToken);
            var syncResp = await callerClient.GetAsync("/api/v1/sync");
            Assert.That(syncResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var syncBody = await syncResp.Content.ReadFromJsonAsync<JsonElement>();

            // Verify faction filtering
            var factions = syncBody.GetProperty("factions");
            var returnedFacUUIDs = new HashSet<string>();
            for (int i = 0; i < factions.GetArrayLength(); i++)
            {
                returnedFacUUIDs.Add(factions[i].GetProperty("uuid").GetString()!);
            }

            if (callerIsMember)
            {
                Assert.That(
                    returnedFacUUIDs,
                    Does.Contain(facUUID),
                    $"Iteration {iteration}: caller is member but faction not in sync.");
            }
            else
            {
                Assert.That(
                    returnedFacUUIDs,
                    Does.Not.Contain(facUUID),
                    $"Iteration {iteration}: caller is NOT member but faction IS in sync.");
            }

            // Second faction should never appear (caller is never a member)
            Assert.That(
                returnedFacUUIDs,
                Does.Not.Contain(facUUID2),
                $"Iteration {iteration}: caller is NOT member of faction2 but it IS in sync.");

            // Verify character filtering
            var characters = syncBody.GetProperty("characters");
            var returnedCharUUIDs = new HashSet<string>();
            for (int i = 0; i < characters.GetArrayLength(); i++)
            {
                returnedCharUUIDs.Add(characters[i].GetProperty("uuid").GetString()!);
            }

            // Caller always sees self
            Assert.That(
                returnedCharUUIDs,
                Does.Contain(callerUUID),
                $"Iteration {iteration}: caller's own character not in sync.");

            if (otherShares)
            {
                Assert.That(
                    returnedCharUUIDs,
                    Does.Contain(otherUUID),
                    $"Iteration {iteration}: other shared with caller but not in sync.");
            }
            else
            {
                Assert.That(
                    returnedCharUUIDs,
                    Does.Not.Contain(otherUUID),
                    $"Iteration {iteration}: other did NOT share but IS in sync.");
            }
        }
    }

    /// <summary>
    /// Adds a character to a faction by creating a group and assigning the character.
    /// </summary>
    private async Task AddCharacterToFaction(string characterUUID, string factionUUID)
    {
        // Get the seeded clearance levels for the faction
        var levelsResp = await _ownerClient.GetAsync(
            $"/api/v1/factions/{factionUUID}/clearance-levels");
        var levelsJson = await levelsResp.Content.ReadFromJsonAsync<JsonElement>();
        var firstLevelUUID = levelsJson[0].GetProperty("uuid").GetString()!;

        // Create a group in the faction
        var groupResp = await _ownerClient.PostAsJsonAsync(
            $"/api/v1/factions/{factionUUID}/groups",
            new
            {
                name = $"Group_{characterUUID[..8]}",
                defaultClearanceLevelUUID = firstLevelUUID,
            });
        var groupJson = await groupResp.Content.ReadFromJsonAsync<JsonElement>();
        var groupUUID = groupJson.GetProperty("uuid").GetString()!;

        // Add the character to the group
        await _ownerClient.PutAsJsonAsync(
            $"/api/v1/factions/{factionUUID}/groups/{groupUUID}/members",
            new { characterUUID });
    }
}
