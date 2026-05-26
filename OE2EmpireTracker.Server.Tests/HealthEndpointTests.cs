using System.Net;
using System.Text.Json;
using NUnit.Framework;

namespace OE2EmpireTracker.Server.Tests;

[TestFixture]
public class HealthEndpointTests
{
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _client = SharedTestServer.Factory.CreateClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
    }

    [Test]
    public async Task Health_ReturnsOkWithStatusAndVersion()
    {
        var response = await _client.GetAsync("/health");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.That(root.GetProperty("status").GetString(), Is.EqualTo("ok"));
        Assert.That(root.GetProperty("serverVersion").GetString(), Is.Not.Null.And.Not.Empty);
    }
}
