// <copyright file="ErrorClassificationPropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Common.Client.FactionServer;

namespace OE2EmpireTracker.Tests.Client.FactionServer
{
    /// <summary>
    /// Property-based tests for error response classification.
    /// **Validates: Requirements 4.4, 4.6**
    /// </summary>
    [TestFixture]
    public class ErrorClassificationPropertyTests
    {
        /// <summary>
        /// Creates a FactionServerTypedClient with a mock handler injected via reflection.
        /// </summary>
        private static FactionServerTypedClient CreateClientWithHandler(HttpMessageHandler handler)
        {
            var token = new SecureString();
            token.AppendChar('t');
            token.MakeReadOnly();

            var client = new FactionServerTypedClient("http://fake-server", token);
            var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30),
            };
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "t");

            FieldInfo httpClientField = typeof(FactionServerTypedClient).GetField(
                "_httpClient",
                BindingFlags.NonPublic | BindingFlags.Instance);
            httpClientField.SetValue(client, httpClient);

            return client;
        }

        /// <summary>
        /// Generates error status codes in the non-success range (400-599) excluding 400 and 403
        /// which have special handling.
        /// </summary>
        private static Gen<int> OtherErrorStatusCodeGen()
        {
            return Gen.OneOf(
                Gen.Choose(401, 402),
                Gen.Choose(404, 499),
                Gen.Choose(500, 599));
        }

        // -----------------------------------------------------------------------
        // Property 2: Error Response Classification
        // **Validates: Requirements 4.4, 4.6**
        // HTTP 400 with valid JSON → FactionValidationException
        // HTTP 400 with invalid JSON → FactionServerException (fallback)
        // HTTP 403 → FactionAuthorizationException
        // Other error codes → FactionServerException
        // -----------------------------------------------------------------------

        /// <summary>
        /// HTTP 400 with valid error JSON throws FactionValidationException with parsed errors.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Http400_WithValidErrorJson_ThrowsFactionValidationException()
        {
            var gen = from entityType in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                      from entityUUID in Arb.Generate<Guid>().Select(g => g.ToString())
                      from field in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                      from error in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                      from errorCount in Gen.Choose(1, 5)
                      select new { entityType, entityUUID, field, error, errorCount };

            return Prop.ForAll(Arb.From(gen), data =>
            {
                var errors = Enumerable.Range(0, data.errorCount).Select(_ => new
                {
                    entityType = data.entityType,
                    entityUUID = data.entityUUID,
                    field = data.field,
                    error = data.error,
                }).ToList();

                string body = JsonConvert.SerializeObject(new { errors });

                var handler = new FakeHandler(
                    () => new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(body),
                    });

                using (var client = CreateClientWithHandler(handler))
                {
                    var ex = Assert.ThrowsAsync<FactionValidationException>(
                        () => client.GetFactionsAsync(CancellationToken.None));

                    return (ex.Errors.Count == data.errorCount)
                        .Label($"Expected {data.errorCount} errors, got {ex.Errors.Count}");
                }
            });
        }

        /// <summary>
        /// HTTP 400 with invalid JSON falls back to FactionServerException.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Http400_WithInvalidJson_ThrowsFactionServerException()
        {
            var gen = from body in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                      where !body.TrimStart().StartsWith("{")
                            && !body.TrimStart().StartsWith("[")
                      select body;

            return Prop.ForAll(Arb.From(gen), body =>
            {
                var handler = new FakeHandler(
                    () => new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(body),
                    });

                using (var client = CreateClientWithHandler(handler))
                {
                    var ex = Assert.ThrowsAsync<FactionServerException>(
                        () => client.GetFactionsAsync(CancellationToken.None));

                    return (ex.GetType() == typeof(FactionServerException))
                        .Label("Should be base FactionServerException, not a subtype")
                        .And(ex.Message.Contains("400"))
                        .Label("Message should contain status code 400");
                }
            });
        }

        /// <summary>
        /// HTTP 403 throws FactionAuthorizationException.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Http403_ThrowsFactionAuthorizationException()
        {
            var gen = Arb.Generate<NonEmptyString>().Select(s => s.Get);

            return Prop.ForAll(Arb.From(gen), body =>
            {
                var handler = new FakeHandler(
                    () => new HttpResponseMessage(HttpStatusCode.Forbidden)
                    {
                        Content = new StringContent(body),
                    });

                using (var client = CreateClientWithHandler(handler))
                {
                    var ex = Assert.ThrowsAsync<FactionAuthorizationException>(
                        () => client.GetFactionsAsync(CancellationToken.None));

                    return (ex != null)
                        .Label("Should throw FactionAuthorizationException");
                }
            });
        }

        /// <summary>
        /// Other error status codes (not 400, not 403) throw base FactionServerException.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property OtherErrorCodes_ThrowFactionServerException()
        {
            var gen = from statusCode in OtherErrorStatusCodeGen()
                      from body in Arb.Generate<NonEmptyString>().Select(s => s.Get)
                      select new { statusCode, body };

            return Prop.ForAll(Arb.From(gen), data =>
            {
                var handler = new FakeHandler(
                    () => new HttpResponseMessage((HttpStatusCode)data.statusCode)
                    {
                        Content = new StringContent(data.body),
                    });

                using (var client = CreateClientWithHandler(handler))
                {
                    var ex = Assert.ThrowsAsync<FactionServerException>(
                        () => client.GetFactionsAsync(CancellationToken.None));

                    return (ex.GetType() == typeof(FactionServerException))
                        .Label("Should be base FactionServerException, not a subtype")
                        .And(ex.Message.Contains(data.statusCode.ToString()))
                        .Label($"Message should contain status code {data.statusCode}");
                }
            });
        }

        /// <summary>
        /// Fake HTTP message handler that returns controlled responses.
        /// </summary>
        private class FakeHandler : HttpMessageHandler
        {
            private readonly Func<HttpResponseMessage> _responseFactory;

            /// <summary>
            /// Initializes a new instance of the <see cref="FakeHandler"/> class.
            /// </summary>
            /// <param name="responseFactory">Factory that produces the response.</param>
            public FakeHandler(Func<HttpResponseMessage> responseFactory)
            {
                _responseFactory = responseFactory;
            }

            /// <inheritdoc/>
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(_responseFactory());
            }
        }
    }
}
