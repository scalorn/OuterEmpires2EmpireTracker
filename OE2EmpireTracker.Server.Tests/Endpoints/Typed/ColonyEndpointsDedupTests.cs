// -----------------------------------------------------------------------
// <copyright file="ColonyEndpointsDedupTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;
using OE2EmpireTracker.Server.Auth;
using OE2EmpireTracker.Server.Storage;

#pragma warning disable SA1009 // Closing parenthesis should be followed by a space

namespace OE2EmpireTracker.Server.Tests.Endpoints.Typed;

/// <summary>
/// Unit tests for ColonyEndpoints dedup logic.
/// Validates: Requirements 31.1, 31.2, 31.3, 31.4.
/// </summary>
[TestFixture]
public class ColonyEndpointsDedupTests
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
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "ColonyDedupTestChar" })
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
    /// When creating a colony with a new PlanetName+SystemName, returns 201.
    /// Validates: Requirement 31.3.
    /// </summary>
    [Test]
    public async Task Create_NewColony_Returns201()
    {
        using var charClient = _factory.CreateAuthenticatedClient(_charToken);

        var response = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies",
            new { planetName = "UniquePlanet1", colonyName = "TestColony", systemName = "Alpha" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(json.GetProperty("planetName").GetString(), Is.EqualTo("UniquePlanet1"));
        Assert.That(json.GetProperty("colonyName").GetString(), Is.EqualTo("TestColony"));
    }

    /// <summary>
    /// When creating a colony with the same PlanetName+SystemName (exact case), returns 200 (merge).
    /// Validates: Requirements 31.1, 31.2.
    /// </summary>
    [Test]
    public async Task Create_DuplicatePlanetAndSystem_Returns200Merged()
    {
        using var charClient = _factory.CreateAuthenticatedClient(_charToken);

        // First create
        var firstResponse = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies",
            new { planetName = "DedupPlanet", colonyName = "OriginalName", systemName = "Beta" });
        Assert.That(firstResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        // Second create with same PlanetName+SystemName → merge
        var secondResponse = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies",
            new { planetName = "DedupPlanet", colonyName = "MergedName", systemName = "Beta" });

        Assert.That(secondResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await secondResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(json.GetProperty("colonyName").GetString(), Is.EqualTo("MergedName"));
    }

    /// <summary>
    /// Dedup matching is case-insensitive on PlanetName and SystemName.
    /// Validates: Requirement 31.4.
    /// </summary>
    [Test]
    public async Task Create_DuplicateCaseInsensitive_Returns200Merged()
    {
        using var charClient = _factory.CreateAuthenticatedClient(_charToken);

        // First create with lowercase
        var firstResponse = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies",
            new { planetName = "caseplanet", colonyName = "LowerCase", systemName = "gamma" });
        Assert.That(firstResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        // Second create with different casing → should still merge
        var secondResponse = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies",
            new { planetName = "CasePlanet", colonyName = "UpperCase", systemName = "Gamma" });

        Assert.That(secondResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await secondResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(json.GetProperty("colonyName").GetString(), Is.EqualTo("UpperCase"));
    }

    /// <summary>
    /// When PlanetName matches but SystemName differs, creates a new colony (no merge).
    /// Validates: Requirement 31.1 (both PlanetName AND SystemName must match).
    /// </summary>
    [Test]
    public async Task Create_SamePlanetDifferentSystem_Returns201()
    {
        using var charClient = _factory.CreateAuthenticatedClient(_charToken);

        // First create
        var firstResponse = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies",
            new { planetName = "SharedPlanet", colonyName = "Colony1", systemName = "SystemA" });
        Assert.That(firstResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        // Second create with same planet but different system → new colony
        var secondResponse = await charClient.PostAsJsonAsync(
            $"/api/v1/characters/{_charUUID}/colonies",
            new { planetName = "SharedPlanet", colonyName = "Colony2", systemName = "SystemB" });

        Assert.That(secondResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }
}
