// -----------------------------------------------------------------------
// <copyright file="EndpointRemovalTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

#pragma warning disable SA1009 // Closing parenthesis should be followed by a space

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Integration tests verifying that all removed raw data routes return HTTP 404.
/// These routes were removed as part of the unvalidated endpoint removal.
/// Validates: Req 1, Criteria 1–10.
/// </summary>
[TestFixture]
public class EndpointRemovalTests
{
    private TestServerFactory _factory = null!;
    private HttpClient _ownerClient = null!;
    private string _charUUID = string.Empty;

    /// <summary>
    /// Sets up the test server and creates a character for route testing.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestServerFactory();
        _factory.SeedOwnerToken();
        _ownerClient = _factory.CreateAuthenticatedClient();

        var createResponse = _ownerClient
            .PostAsJsonAsync("/api/v1/tokens", new { characterName = "RemovalTestChar" })
            .GetAwaiter().GetResult();
        var createJson = createResponse.Content
            .ReadFromJsonAsync<JsonElement>()
            .GetAwaiter().GetResult();
        _charUUID = createJson.GetProperty("characterUUID").GetString()!;
    }

    /// <summary>
    /// Tears down the test server.
    /// </summary>
    [OneTimeTearDown]
    public void TearDown()
    {
        _ownerClient.Dispose();
        _factory.Dispose();
    }

    /// <summary>
    /// PUT /api/v1/characters/{uuid}/data/colonies returns 404.
    /// The collection upsert route has been removed.
    /// Validates: Req 1, Criterion 2.
    /// </summary>
    [Test]
    public async Task PutDataCollection_Returns404()
    {
        var content = new StringContent("[]", Encoding.UTF8, "application/json");
        var response = await _ownerClient.PutAsync(
            $"/api/v1/characters/{_charUUID}/data/colonies", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// GET /api/v1/characters/{uuid}/data/colonies returns 404.
    /// The collection read route has been removed.
    /// Validates: Req 1, Criterion 1.
    /// </summary>
    [Test]
    public async Task GetDataCollection_Returns404()
    {
        var response = await _ownerClient.GetAsync(
            $"/api/v1/characters/{_charUUID}/data/colonies");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// POST /api/v1/characters/{uuid}/data/colonies returns 404.
    /// The entity creation route has been removed.
    /// Validates: Req 1, Criterion 3.
    /// </summary>
    [Test]
    public async Task PostDataCollection_Returns404()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _ownerClient.PostAsync(
            $"/api/v1/characters/{_charUUID}/data/colonies", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// GET /api/v1/characters/{uuid}/data/colonies/{entityUuid} returns 404.
    /// The single entity retrieval route has been removed.
    /// Validates: Req 1, Criterion 4.
    /// </summary>
    [Test]
    public async Task GetDataEntity_Returns404()
    {
        var entityUuid = Guid.NewGuid().ToString();
        var response = await _ownerClient.GetAsync(
            $"/api/v1/characters/{_charUUID}/data/colonies/{entityUuid}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// PUT /api/v1/characters/{uuid}/data/colonies/{entityUuid} returns 404.
    /// The single entity update route has been removed.
    /// Validates: Req 1, Criterion 5.
    /// </summary>
    [Test]
    public async Task PutDataEntity_Returns404()
    {
        var entityUuid = Guid.NewGuid().ToString();
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _ownerClient.PutAsync(
            $"/api/v1/characters/{_charUUID}/data/colonies/{entityUuid}", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// DELETE /api/v1/characters/{uuid}/data/colonies/{entityUuid} returns 404.
    /// The single entity deletion route has been removed.
    /// Validates: Req 1, Criterion 6.
    /// </summary>
    [Test]
    public async Task DeleteDataEntity_Returns404()
    {
        var entityUuid = Guid.NewGuid().ToString();
        var response = await _ownerClient.DeleteAsync(
            $"/api/v1/characters/{_charUUID}/data/colonies/{entityUuid}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// PUT /api/v1/characters/{uuid}/data returns 404.
    /// The bulk unvalidated data upload route has been removed.
    /// Validates: Req 1, Criterion 7.
    /// </summary>
    [Test]
    public async Task PutDataBulk_Returns404()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _ownerClient.PutAsync(
            $"/api/v1/characters/{_charUUID}/data", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// GET /api/v1/characters/{uuid}/data returns 404.
    /// The read-all character data route has been removed.
    /// Validates: Req 1, Criterion 10.
    /// </summary>
    [Test]
    public async Task GetDataReadAll_Returns404()
    {
        var response = await _ownerClient.GetAsync(
            $"/api/v1/characters/{_charUUID}/data");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
