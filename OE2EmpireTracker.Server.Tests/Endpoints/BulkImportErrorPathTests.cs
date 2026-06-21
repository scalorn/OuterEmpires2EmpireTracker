// -----------------------------------------------------------------------
// <copyright file="BulkImportErrorPathTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Endpoints;
using OE2EmpireTracker.Server.Storage;
using OE2EmpireTracker.Services;

#pragma warning disable SA1009 // Closing parenthesis should be followed by a space

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Unit tests for bulk import error paths.
/// Validates: Req 2, Criteria 2-3, 7; Req 6, Criteria 1-2, 4; Req 7 Criterion 2.
/// </summary>
[TestFixture]
public class BulkImportErrorPathTests
{
    private const string CharacterUUID = "char-uuid-A";

    /// <summary>
    /// Colony with null PlanetName returns 400 with error identifying the field.
    /// </summary>
    [Test]
    public async Task HandleImport_MissingRequiredField_Returns400WithErrors()
    {
        var playerRoot = new PlayerRoot
        {
            Colony = new[]
            {
                new Colony
                {
                    UUID = "colony-1",
                    OwnerUUID = CharacterUUID,
                    PlanetName = null!,
                    ColonyName = "Test Colony",
                },
            },
        };

        var json = JsonSerializer.Serialize(playerRoot);
        var httpContext = CreateHttpContext(CharacterUUID);
        SetRequestBody(httpContext, json);

        var result = await InvokeHandleImport(CharacterUUID, httpContext);

        Assert.That(result, Is.Not.Null);
        var jsonResult = result as Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<BulkImportErrorResponse>;
        Assert.That(jsonResult, Is.Not.Null);
        Assert.That(jsonResult!.StatusCode, Is.EqualTo(400));
        Assert.That(jsonResult.Value!.Errors, Has.Count.GreaterThan(0));
        Assert.That(jsonResult.Value.Errors.Any(e => e.Field == "PlanetName"), Is.True);
    }

    /// <summary>
    /// Token has CharacterUUID "A" but URL is "B" and caller is not Owner → 403.
    /// </summary>
    [Test]
    public async Task HandleImport_WrongCharacterUUID_Returns403()
    {
        var urlUuid = "char-uuid-B";
        var tokenUuid = "char-uuid-A";

        var playerRoot = new PlayerRoot();
        var json = JsonSerializer.Serialize(playerRoot);
        var httpContext = CreateHttpContext(tokenUuid);
        SetRequestBody(httpContext, json);

        var result = await InvokeHandleImport(urlUuid, httpContext);

        Assert.That(result, Is.Not.Null);
        var statusResult = result as IStatusCodeHttpResult;
        Assert.That(statusResult, Is.Not.Null);
        Assert.That(statusResult!.StatusCode, Is.EqualTo(403));
    }

    /// <summary>
    /// When auth fails (403), the request body stream position is still 0 (body was never read).
    /// Verifies authorization is checked BEFORE reading the request body.
    /// </summary>
    [Test]
    public async Task HandleImport_AuthCheckedBeforeBodyRead()
    {
        var urlUuid = "char-uuid-B";
        var tokenUuid = "char-uuid-A";

        var json = """{"Colony":[{"UUID":"c1","PlanetName":"X","ColonyName":"Y"}]}""";
        var httpContext = CreateHttpContext(tokenUuid);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        httpContext.Request.Body = bodyStream;
        httpContext.Request.ContentType = "application/json";

        await InvokeHandleImport(urlUuid, httpContext);

        // If auth was checked before body read, stream position should be 0
        Assert.That(bodyStream.Position, Is.EqualTo(0));
    }

    /// <summary>
    /// Colony with OwnerUUID different from URL uuid returns 400 with OwnerUUID mismatch error.
    /// </summary>
    [Test]
    public async Task HandleImport_MismatchedOwnerUUID_ReturnsValidationError()
    {
        var playerRoot = new PlayerRoot
        {
            Colony = new[]
            {
                new Colony
                {
                    UUID = "colony-1",
                    OwnerUUID = "different-owner-uuid",
                    PlanetName = "Earth",
                    ColonyName = "Test Colony",
                },
            },
        };

        var json = JsonSerializer.Serialize(playerRoot);
        var httpContext = CreateHttpContext(CharacterUUID);
        SetRequestBody(httpContext, json);

        var result = await InvokeHandleImport(CharacterUUID, httpContext);

        Assert.That(result, Is.Not.Null);
        var jsonResult = result as Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<BulkImportErrorResponse>;
        Assert.That(jsonResult, Is.Not.Null);
        Assert.That(jsonResult!.StatusCode, Is.EqualTo(400));
        Assert.That(jsonResult.Value!.Errors.Any(e => e.Field == "OwnerUUID"), Is.True);
    }

    private static HttpContext CreateHttpContext(
        string? characterUUID = null,
        bool isOwner = false)
    {
        var claims = new List<Claim>();

        if (characterUUID != null)
        {
            claims.Add(new Claim("CharacterUUID", characterUUID));
        }

        if (isOwner)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Owner"));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        return new DefaultHttpContext
        {
            User = principal,
        };
    }

    private static void SetRequestBody(HttpContext httpContext, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        httpContext.Request.Body = new MemoryStream(bytes);
        httpContext.Request.ContentType = "application/json";
    }

    private static async Task<IResult> InvokeHandleImport(
        string uuid,
        HttpContext httpContext)
    {
        var storage = new StubStorageBackend();

        // Use reflection to invoke the private HandleImport method
        var method = typeof(BulkImportEndpoints).GetMethod(
            "HandleImport",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.That(method, Is.Not.Null, "HandleImport method not found");

        var task = (Task<IResult>)method!.Invoke(null, new object[] { uuid, httpContext, storage })!;
        return await task;
    }
}
