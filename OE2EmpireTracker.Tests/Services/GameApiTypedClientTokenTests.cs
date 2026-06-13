// <copyright file="GameApiTypedClientTokenTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OE2EmpireTracker.Common.Client;
using OE2EmpireTracker.Common.Client.Generated;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for token caching and rate-limiter bypass behavior in
    /// <see cref="GameApiTypedClient"/>.
    /// **Validates: Requirements 7.2, 7.3, 7.4**
    /// </summary>
    [TestFixture]
    public class GameApiTypedClientTokenTests
    {
        /// <summary>
        /// Sets up the clock to frozen time and instant-delay mode for each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            SystemClock.FreezeAt(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            SystemClock.EnableInstantDelay();
        }

        /// <summary>
        /// Resets the clock after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            SystemClock.Reset();
        }

        /// <summary>
        /// Verifies that calling ExchangeTokenAsync twice with the same credentials
        /// only makes one HTTP call (cache hit on the second call).
        /// Then, after advancing time past token expiry, a third call triggers
        /// a second HTTP request (cache miss / re-exchange).
        /// </summary>
        [Test]
        public async Task ExchangeTokenAsync_CacheHit_ReusesToken_ExpiredToken_TriggersReExchange()
        {
            int httpCallCount = 0;
            var handler = new MockHttpHandler(() =>
            {
                Interlocked.Increment(ref httpCallCount);
                return CreateTokenResponse("test-token-123", 3600);
            });

            using (var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://fake-server/"),
            })
            using (var client = new GameApiTypedClient(httpClient, "test-app"))
            {
                // First call: should hit the network
                var result1 = await client.ExchangeTokenAsync("app", "client1", "secret1")
                    .ConfigureAwait(false);
                Assert.That(result1.AccessToken, Is.EqualTo("test-token-123"));
                Assert.That(httpCallCount, Is.EqualTo(1), "First call should make one HTTP request");

                // Second call: same credentials, should hit cache (no new HTTP call)
                var result2 = await client.ExchangeTokenAsync("app", "client1", "secret1")
                    .ConfigureAwait(false);
                Assert.That(result2.AccessToken, Is.EqualTo("test-token-123"));
                Assert.That(httpCallCount, Is.EqualTo(1), "Second call should reuse cached token");

                // Advance time past token expiry (3600s token, advance 3601s)
                SystemClock.AdvanceBy(TimeSpan.FromSeconds(3601));

                // Third call: token expired, should trigger re-exchange
                var result3 = await client.ExchangeTokenAsync("app", "client1", "secret1")
                    .ConfigureAwait(false);
                Assert.That(result3.AccessToken, Is.EqualTo("test-token-123"));
                Assert.That(httpCallCount, Is.EqualTo(2), "Expired token should trigger re-exchange");
            }
        }

        /// <summary>
        /// Verifies that token exchange bypasses the rate limiter by opening
        /// the circuit breaker (which would block ExecuteAsync-routed calls)
        /// and confirming ExchangeTokenAsync still succeeds.
        /// If ExchangeTokenAsync went through ExecuteAsync/Polly pipeline,
        /// it would throw BrokenCircuitException.
        /// </summary>
        [Test]
        public async Task ExchangeTokenAsync_BypassesRateLimiter_SucceedsWithOpenCircuit()
        {
            int httpCallCount = 0;
            var handler = new MockHttpHandler((request) =>
            {
                Interlocked.Increment(ref httpCallCount);

                // Auth endpoint returns success
                if (request.RequestUri.AbsolutePath.Contains("auth/token"))
                {
                    return CreateTokenResponse("bypass-token", 3600);
                }

                // Data endpoints return 500 to trip the circuit breaker
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(
                        "{\"success\":false,\"returnCode\":500,\"returnString\":\"Server Error\",\"data\":null,\"errors\":[]}",
                        System.Text.Encoding.UTF8,
                        "application/json"),
                };
            });

            using (var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://fake-server/"),
            })
            using (var client = new GameApiTypedClient(httpClient, "test-app"))
            {
                // Trip the circuit breaker by making 3+ failed data-endpoint calls.
                // Each call goes through retry (3 retries) so the breaker sees many failures.
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        await client.GetCharacterAsync().ConfigureAwait(false);
                    }
                    catch (ApiHttpException)
                    {
                        // Expected: server error causes ApiHttpException
                    }
#pragma warning disable RCS1075 // Polly circuit breaker throws; test project lacks Polly reference for specific type
                    catch (Exception)
                    {
                        // Expected: BrokenCircuitException after circuit opens
                    }
#pragma warning restore RCS1075
                }

                // Circuit should now be open
                Assert.That(client.IsCircuitOpen, Is.True, "Circuit breaker should be open after consecutive 500s");

                // Token exchange should still succeed despite open circuit
                // because it bypasses the rate limiter and Polly pipeline
                var token = await client.ExchangeTokenAsync("app", "client1", "secret1")
                    .ConfigureAwait(false);
                Assert.That(token.AccessToken, Is.EqualTo("bypass-token"));
            }
        }

        /// <summary>
        /// Creates an HTTP response message with a successful token exchange payload.
        /// </summary>
        /// <param name="accessToken">The access token to include.</param>
        /// <param name="expiresIn">The token lifetime in seconds.</param>
        /// <returns>An HTTP 200 response with the serialized token envelope.</returns>
        private static HttpResponseMessage CreateTokenResponse(string accessToken, int expiresIn)
        {
            string json = "{\"success\":true,\"returnCode\":0,\"returnString\":\"\","
                + "\"data\":{\"accessToken\":\"" + accessToken + "\","
                + "\"tokenType\":\"Bearer\",\"expiresIn\":" + expiresIn + ","
                + "\"characterId\":1,\"scopes\":[\"read\"],\"subscription\":\"standard\"},"
                + "\"errors\":[]}";

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            };
        }

        /// <summary>
        /// A mock HTTP message handler that returns controlled responses
        /// and tracks call counts.
        /// </summary>
        private class MockHttpHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _requestHandler;

            /// <summary>
            /// Initializes a new instance of the <see cref="MockHttpHandler"/> class
            /// with a simple response factory (ignores the request).
            /// </summary>
            /// <param name="responseFactory">Factory that produces the response.</param>
            public MockHttpHandler(Func<HttpResponseMessage> responseFactory)
            {
                _requestHandler = _ => responseFactory();
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="MockHttpHandler"/> class
            /// with a request-aware response factory.
            /// </summary>
            /// <param name="requestHandler">Function that inspects the request and produces a response.</param>
            public MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> requestHandler)
            {
                _requestHandler = requestHandler;
            }

            /// <inheritdoc/>
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(_requestHandler(request));
            }
        }
    }
}
