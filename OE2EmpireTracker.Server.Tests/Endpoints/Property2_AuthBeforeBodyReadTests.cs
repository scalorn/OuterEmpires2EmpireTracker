// -----------------------------------------------------------------------
// <copyright file="Property2_AuthBeforeBodyReadTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using OE2EmpireTracker.Server.Endpoints;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Property 2: Authorization Before Body Read.
/// For ALL requests where authorization fails (403), the request body stream
/// SHALL NOT have been read. This prevents resource exhaustion attacks where
/// an unauthorized client sends a large body.
/// **Validates: Req 6 Criterion 2**
/// </summary>
[TestFixture]
public class Property2_AuthBeforeBodyReadTests
{
    /// <summary>
    /// Property: For any unauthorized caller (token UUID != URL UUID and not Owner),
    /// the request body stream position remains at 0 after the endpoint returns 403.
    /// Tests with randomly generated character UUIDs and varying body sizes.
    /// </summary>
    [Test]
    public async Task AuthBeforeBodyRead_UnauthorizedCaller_BodyStreamNeverRead()
    {
        var rng = new Random(42);

        for (int iteration = 0; iteration < 50; iteration++)
        {
            // Generate distinct token and URL UUIDs
            var tokenUuid = $"token-char-{Guid.NewGuid():N}";
            var urlUuid = $"url-char-{Guid.NewGuid():N}";

            // Generate a random body size (0 bytes to 64KB)
            int bodySize = rng.Next(0, 65536);
            var bodyBytes = new byte[bodySize];
            rng.NextBytes(bodyBytes);

            var httpContext = CreateHttpContext(tokenUuid);
            var bodyStream = new TrackingMemoryStream(bodyBytes);
            httpContext.Request.Body = bodyStream;
            httpContext.Request.ContentType = "application/json";

            var result = await InvokeHandleImport(urlUuid, httpContext);

            // Verify 403 was returned
            var jsonResult = result as Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<object>;
            Assert.That(
                jsonResult,
                Is.Not.Null,
                $"Iteration {iteration}: expected JSON 403 result");
            Assert.That(
                jsonResult!.StatusCode,
                Is.EqualTo(403),
                $"Iteration {iteration}: expected 403 for mismatched UUIDs");

            // Verify body was never read
            Assert.That(
                bodyStream.Position,
                Is.EqualTo(0),
                $"Iteration {iteration}: body stream position should be 0 " +
                $"(body size={bodySize}), but was {bodyStream.Position}");
            Assert.That(
                bodyStream.ReadCount,
                Is.EqualTo(0),
                $"Iteration {iteration}: body stream should have 0 read calls " +
                $"(body size={bodySize}), but had {bodyStream.ReadCount}");
        }
    }

    /// <summary>
    /// Property: For any body content (valid JSON, invalid JSON, binary garbage),
    /// authorization failure returns 403 without touching the body.
    /// Tests that the body content itself does not influence the auth decision.
    /// </summary>
    [Test]
    public async Task AuthBeforeBodyRead_VariousBodyContents_NeverRead()
    {
        var tokenUuid = "caller-uuid-fixed";
        var urlUuid = "target-uuid-different";

        var bodies = new[]
        {
            string.Empty,
            "{}",
            """{"Colony":[]}""",
            """{"Colony":[{"UUID":"c1","PlanetName":"X","ColonyName":"Y","OwnerUUID":"target-uuid-different"}]}""",
            "not json at all",
            new string('X', 1024),
            new string('Z', 32768),
            """{"deeply":{"nested":{"object":{"with":"many","levels":true}}}}""",
            "null",
            "[]",
        };

        for (int i = 0; i < bodies.Length; i++)
        {
            var httpContext = CreateHttpContext(tokenUuid);
            var bodyBytes = Encoding.UTF8.GetBytes(bodies[i]);
            var bodyStream = new TrackingMemoryStream(bodyBytes);
            httpContext.Request.Body = bodyStream;
            httpContext.Request.ContentType = "application/json";

            var result = await InvokeHandleImport(urlUuid, httpContext);

            var jsonResult = result as Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<object>;
            Assert.That(
                jsonResult?.StatusCode,
                Is.EqualTo(403),
                $"Body variant {i}: expected 403");
            Assert.That(
                bodyStream.Position,
                Is.EqualTo(0),
                $"Body variant {i}: stream position should be 0 (size={bodyBytes.Length})");
            Assert.That(
                bodyStream.ReadCount,
                Is.EqualTo(0),
                $"Body variant {i}: stream read count should be 0");
        }
    }

    /// <summary>
    /// Property: When authorization succeeds (token matches URL UUID),
    /// the body IS read. This is the positive control — confirms the tracking
    /// stream correctly detects reads when they happen.
    /// </summary>
    [Test]
    public async Task AuthBeforeBodyRead_AuthorizedCaller_BodyIsRead()
    {
        var charUuid = "same-char-uuid";
        var json = """{"Colony":[{"UUID":"c1","PlanetName":"X","ColonyName":"Y","OwnerUUID":"same-char-uuid"}]}""";

        var httpContext = CreateHttpContext(charUuid);
        var bodyBytes = Encoding.UTF8.GetBytes(json);
        var bodyStream = new TrackingMemoryStream(bodyBytes);
        httpContext.Request.Body = bodyStream;
        httpContext.Request.ContentType = "application/json";

        await InvokeHandleImport(charUuid, httpContext);

        // When auth passes, the body SHOULD be read
        Assert.That(
            bodyStream.ReadCount,
            Is.GreaterThan(0),
            "Authorized request should read the body (positive control)");
    }

    /// <summary>
    /// Property: Large body sizes (up to 1MB) do not cause delays or memory
    /// allocation when authorization fails. The endpoint returns 403 immediately
    /// without buffering the body.
    /// </summary>
    [Test]
    public async Task AuthBeforeBodyRead_LargeBodies_NotBuffered()
    {
        var rng = new Random(2024);
        var sizes = new[] { 0, 1, 100, 1024, 10240, 102400, 524288, 1048576 };

        foreach (var size in sizes)
        {
            var tokenUuid = $"attacker-{Guid.NewGuid():N}";
            var urlUuid = $"victim-{Guid.NewGuid():N}";

            var bodyBytes = new byte[size];
            if (size > 0)
            {
                rng.NextBytes(bodyBytes);
            }

            var httpContext = CreateHttpContext(tokenUuid);
            var bodyStream = new TrackingMemoryStream(bodyBytes);
            httpContext.Request.Body = bodyStream;
            httpContext.Request.ContentType = "application/json";

            var result = await InvokeHandleImport(urlUuid, httpContext);

            var jsonResult = result as Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<object>;
            Assert.That(
                jsonResult?.StatusCode,
                Is.EqualTo(403),
                $"Body size {size}: expected 403");
            Assert.That(
                bodyStream.Position,
                Is.EqualTo(0),
                $"Body size {size}: stream should not have been read");
            Assert.That(
                bodyStream.ReadCount,
                Is.EqualTo(0),
                $"Body size {size}: no read calls should have occurred");
        }
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

    private static async Task<IResult> InvokeHandleImport(
        string uuid,
        HttpContext httpContext)
    {
        var storage = new StubStorageBackend();

        var method = typeof(BulkImportEndpoints).GetMethod(
            "HandleImport",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.That(method, Is.Not.Null, "HandleImport method not found");

        var task = (Task<IResult>)method!.Invoke(
            null, new object[] { uuid, httpContext, storage })!;
        return await task;
    }

    /// <summary>
    /// A MemoryStream wrapper that tracks whether any read operations occurred.
    /// Used to verify that the endpoint does not read the body when auth fails.
    /// </summary>
    private sealed class TrackingMemoryStream : MemoryStream
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrackingMemoryStream"/> class.
        /// </summary>
        /// <param name="buffer">The byte array to wrap.</param>
        public TrackingMemoryStream(byte[] buffer)
            : base(buffer)
        {
        }

        /// <summary>
        /// Gets the number of read operations performed on this stream.
        /// </summary>
        public int ReadCount { get; private set; }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCount++;
            return base.Read(buffer, offset, count);
        }

        /// <inheritdoc/>
        public override int Read(Span<byte> buffer)
        {
            ReadCount++;
            return base.Read(buffer);
        }

        /// <inheritdoc/>
        public override Task<int> ReadAsync(
            byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ReadCount++;
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }

        /// <inheritdoc/>
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return base.ReadAsync(buffer, cancellationToken);
        }

        /// <inheritdoc/>
        public override int ReadByte()
        {
            ReadCount++;
            return base.ReadByte();
        }
    }
}
