// <copyright file="RateLimiterPropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Common.Client.FactionServer;

namespace OE2EmpireTracker.Tests.Client.FactionServer
{
    /// <summary>
    /// Property-based tests for the rate limiter in FactionServerTypedClient.
    /// </summary>
    [TestFixture]
    public class RateLimiterPropertyTests
    {
        /// <summary>
        /// Creates a FactionServerTypedClient with a mock handler injected via reflection.
        /// The mock handler returns 200 OK instantly so that the rate limiter is the only bottleneck.
        /// </summary>
        private static FactionServerTypedClient CreateClientWithInstantHandler()
        {
            var token = new SecureString();
            token.AppendChar('t');
            token.MakeReadOnly();

            var client = new FactionServerTypedClient("http://fake-server", token);
            var handler = new InstantOkHandler();
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

        // -----------------------------------------------------------------------
        // Property 5: Rate Limiter Blocking
        // **Validates: Requirements 6.2**
        // Generate random N (1-20), apply rate limit N, issue N requests to consume
        // all tokens, then verify the N+1th request does not complete within 50ms.
        // -----------------------------------------------------------------------

        /// <summary>
        /// After exhausting N rate limit tokens, the N+1th request blocks (does not
        /// complete within 50ms), confirming the rate limiter queues requests when
        /// tokens are exhausted.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 20)]
        public Property RateLimiterBlocks_WhenAllTokensExhausted()
        {
            var gen = Gen.Choose(1, 20);

            return Prop.ForAll(Arb.From(gen), n =>
            {
                using (var client = CreateClientWithInstantHandler())
                {
                    // Set rate limit to exactly N tokens
                    client.ApplyRateLimit(n);

                    // Consume all N tokens by making N requests
                    for (int i = 0; i < n; i++)
                    {
                        client.CheckHealthAsync(CancellationToken.None)
                            .GetAwaiter().GetResult();
                    }

                    // The N+1th request should block because no tokens are available
                    // (tokens auto-release after 60s, so within 50ms it must still be blocked)
                    var blockedTask = client.CheckHealthAsync(CancellationToken.None);
                    var delayTask = Task.Delay(50);

                    var completed = Task.WhenAny(blockedTask, delayTask)
                        .GetAwaiter().GetResult();

                    return (completed == delayTask)
                        .Label($"N={n}: N+1th request should NOT complete within 50ms (rate limiter should block)");
                }
            });
        }

        // -----------------------------------------------------------------------
        // Property 6: Rate Limiter Application
        // **Validates: Requirements 6.3**
        // For all positive integers N in [1, 10000], ApplyRateLimit(N) does not throw.
        // -----------------------------------------------------------------------

        /// <summary>
        /// ApplyRateLimit does not throw for any positive integer in [1, 10000].
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ApplyRateLimit_DoesNotThrow_ForAnyPositiveValue()
        {
            var gen = Gen.Choose(1, 10000);

            return Prop.ForAll(Arb.From(gen), value =>
            {
                var token = new SecureString();
                token.AppendChar('x');
                token.MakeReadOnly();

                using (var client = new FactionServerTypedClient("http://localhost", token))
                {
                    Assert.DoesNotThrow(() => client.ApplyRateLimit(value));
                }
            });
        }

        /// <summary>
        /// Fake HTTP message handler that returns 200 OK instantly.
        /// This ensures the rate limiter is the only bottleneck in the test.
        /// </summary>
        private class InstantOkHandler : HttpMessageHandler
        {
            /// <inheritdoc/>
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("true"),
                });
            }
        }
    }
}
