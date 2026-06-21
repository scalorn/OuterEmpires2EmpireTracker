using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Server.Auth;
using OE2EmpireTracker.Server.Storage;
using OE2EmpireTracker.Services;

#pragma warning disable SA1009 // Closing parenthesis should be followed by a space (false positive with generics)

namespace OE2EmpireTracker.Server.Tests;

/// <summary>
/// Custom WebApplicationFactory that uses a temp directory for data storage
/// and pre-seeds a known Owner token for authenticated test requests.
/// </summary>
public class TestServerFactory : WebApplicationFactory<Program>
{
    public const string KnownOwnerToken = "test-owner-token-for-integration-tests";

    private bool _seeded;

    public string DataPath { get; private set; } = string.Empty;

    public HttpClient CreateAuthenticatedClient(string token = KnownOwnerToken)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Seeds the known owner token into storage. Call after factory creates the host.
    /// </summary>
    public void SeedOwnerToken()
    {
        if (_seeded)
        {
            return;
        }

        _seeded = true;

        var storage = Services.GetRequiredService<IStorageBackend>();

        // Revoke any auto-generated owner tokens
        var existingTokens = storage.GetAllTokensAsync().GetAwaiter().GetResult();
        foreach (var t in existingTokens.Where(t => t.Role == TokenRole.Owner))
        {
            t.IsRevoked = true;
            storage.UpsertTokenAsync(t).GetAwaiter().GetResult();
        }

        // Insert our known owner token
        var hash = TokenService.HashToken(KnownOwnerToken);
        var ownerToken = new ApiToken
        {
            Id = "test-owner-id",
            TokenHash = hash,
            Role = TokenRole.Owner,
            CreatedUtc = SystemClock.UtcNow,
        };
        storage.UpsertTokenAsync(ownerToken).GetAwaiter().GetResult();
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        DataPath = Path.Combine(Path.GetTempPath(), "oe2-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(DataPath);

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:DataPath"] = DataPath,
                ["Server:ProcessingEnabled"] = "false",
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(DataPath))
        {
            try
            {
                Directory.Delete(DataPath, true);
            }
            catch
            {
                // best effort cleanup
            }
        }
    }
}

// ============================================================
// Auth Tests
// ============================================================

[TestFixture]
public class AuthTests
{
    private TestServerFactory Factory => SharedTestServer.Factory;

    [Test]
    public async Task Unauthenticated_Request_Returns_401()
    {
        var client = Factory.CreateClient();
        var response = await client.GetAsync("/api/v1/factions");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Valid_Owner_Token_Returns_200()
    {
        var client = Factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/v1/factions");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Revoked_Token_Returns_401()
    {
        var storage = Factory.Services.GetRequiredService<IStorageBackend>();
        var plainToken = "revoke-test-token";
        var hash = TokenService.HashToken(plainToken);
        var token = new ApiToken
        {
            Id = "revoke-test-id",
            TokenHash = hash,
            Role = TokenRole.Character,
            CharacterUUID = "test-char-uuid",
            CreatedUtc = SystemClock.UtcNow,
            IsRevoked = true,
        };
        await storage.UpsertTokenAsync(token);

        var client = Factory.CreateAuthenticatedClient(plainToken);
        var response = await client.GetAsync("/api/v1/factions");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}

// ============================================================
// Faction CRUD Tests
// ============================================================

[TestFixture]
public class FactionCrudTests
{
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _client = SharedTestServer.Factory.CreateAuthenticatedClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
    }

    [Test]
    [Order(1)]
    public async Task Post_Factions_Creates_Faction_Returns_201()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/factions",
            new { name = "TestFaction", description = "A test faction" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(json.GetProperty("name").GetString(), Is.EqualTo("TestFaction"));
        Assert.That(json.GetProperty("uuid").GetString(), Is.Not.Null.And.Not.Empty);
    }

    [Test]
    [Order(2)]
    public async Task Post_Factions_Duplicate_Name_Returns_409()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/factions", new { name = "TestFaction" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    [Order(3)]
    public async Task Get_Factions_Returns_List_Including_Created()
    {
        var response = await _client.GetAsync("/api/v1/factions");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var factions = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(factions.GetArrayLength(), Is.GreaterThanOrEqualTo(1));

        var names = Enumerable.Range(0, factions.GetArrayLength())
            .Select(i => factions[i].GetProperty("name").GetString())
            .ToList();
        Assert.That(names, Does.Contain("TestFaction"));
    }

    [Test]
    [Order(4)]
    public async Task Get_Faction_By_Uuid_Returns_Faction()
    {
        var listResponse = await _client.GetAsync("/api/v1/factions");
        var factions = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var uuid = factions[0].GetProperty("uuid").GetString()!;

        var response = await _client.GetAsync($"/api/v1/factions/{uuid}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var faction = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(faction.GetProperty("uuid").GetString(), Is.EqualTo(uuid));
    }

    [Test]
    [Order(5)]
    public async Task Put_Faction_Updates_Name()
    {
        var listResponse = await _client.GetAsync("/api/v1/factions");
        var factions = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var uuid = factions[0].GetProperty("uuid").GetString()!;

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/factions/{uuid}", new { name = "UpdatedFaction" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var updated = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(updated.GetProperty("name").GetString(), Is.EqualTo("UpdatedFaction"));
    }

    [Test]
    [Order(6)]
    public async Task Delete_Faction_Returns_204()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/factions", new { name = "ToDelete" });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var uuid = created.GetProperty("uuid").GetString()!;

        var response = await _client.DeleteAsync($"/api/v1/factions/{uuid}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var getResponse = await _client.GetAsync($"/api/v1/factions/{uuid}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}

// ============================================================
// Character CRUD Tests
// ============================================================

[TestFixture]
public class CharacterCrudTests
{
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _client = SharedTestServer.Factory.CreateAuthenticatedClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
    }

    [Test]
    [Order(1)]
    public async Task Post_Characters_Creates_Character_Returns_201()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/characters", new { name = "TestCharacter" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(json.GetProperty("name").GetString(), Is.EqualTo("TestCharacter"));
        Assert.That(json.GetProperty("uuid").GetString(), Is.Not.Null.And.Not.Empty);
    }

    [Test]
    [Order(2)]
    public async Task Get_Character_By_Uuid_Returns_Character()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/characters", new { name = "GetTestChar" });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var uuid = created.GetProperty("uuid").GetString()!;

        var response = await _client.GetAsync($"/api/v1/characters/{uuid}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var character = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(character.GetProperty("name").GetString(), Is.EqualTo("GetTestChar"));
    }

    [Test]
    [Order(3)]
    public async Task Put_Character_With_Invalid_FactionUUID_Returns_400()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/characters", new { name = "BadFactionChar" });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var uuid = created.GetProperty("uuid").GetString()!;

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/characters/{uuid}",
            new { factionUUID = "nonexistent-faction-uuid" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    [Order(4)]
    public async Task Delete_Character_Returns_204()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/characters", new { name = "ToDeleteChar" });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var uuid = created.GetProperty("uuid").GetString()!;

        var response = await _client.DeleteAsync($"/api/v1/characters/{uuid}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var getResponse = await _client.GetAsync($"/api/v1/characters/{uuid}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}

// ============================================================
// Token Management Tests
// ============================================================

[TestFixture]
public class TokenManagementTests
{
    private HttpClient _client = null!;

    private TestServerFactory Factory => SharedTestServer.Factory;

    [OneTimeSetUp]
    public void Setup()
    {
        _client = Factory.CreateAuthenticatedClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
    }

    [Test]
    [Order(1)]
    public async Task Post_Tokens_Creates_Character_And_Token()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/tokens", new { characterName = "TokenTestChar" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(json.GetProperty("token").GetString(), Is.Not.Null.And.Not.Empty);
        Assert.That(json.GetProperty("tokenId").GetString(), Is.Not.Null.And.Not.Empty);
        Assert.That(json.GetProperty("characterUUID").GetString(), Is.Not.Null.And.Not.Empty);
    }

    [Test]
    [Order(2)]
    public async Task Get_Tokens_Lists_Tokens()
    {
        var response = await _client.GetAsync("/api/v1/tokens");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var tokens = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(tokens.GetArrayLength(), Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    [Order(3)]
    public async Task Character_Token_Can_Read_Factions_But_Not_Create()
    {
        // Create a character token via the tokens endpoint
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/tokens", new { characterName = "LimitedChar" });
        var createJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var charToken = createJson.GetProperty("token").GetString()!;

        // Use the character token
        using var charClient = Factory.CreateAuthenticatedClient(charToken);

        // Should be able to read factions (Authenticated policy)
        var readResponse = await charClient.GetAsync("/api/v1/factions");
        Assert.That(readResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Should NOT be able to create factions (Owner only)
        var createFactionResponse = await charClient.PostAsJsonAsync(
            "/api/v1/factions", new { name = "ShouldFail" });
        Assert.That(createFactionResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }
}
