// <copyright file="GameApiTypedClientResilienceTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OE2EmpireTracker.Common.Client;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for circuit breaker and retry resilience policies in
    /// <see cref="GameApiTypedClient"/>.
    /// **Validates: Requirements 6.1, 6.2, 6.6, 6.7**
    /// </summary>
    [TestFixture]
    public class GameApiTypedClientResilienceTests
    {
        /// <summary>
        /// Sets up the clock to frozen time and instant-delay mode for each test.
        /// This avoids real wall-clock waits from the rate limiter.
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
        /// 12.1 - Verifies the circuit breaker opens after consecutive 500 errors.
        ///
        /// Policy structure: retry (outer) wraps circuit breaker (inner).
        /// Retry fires 3 retries (4 total attempts). Circuit breaker opens after 3 failures.
        /// After the first GetAssetLocationsAsync call completes (with exception),
        /// IsCircuitOpen should be true. A second call should throw
        /// BrokenCircuitException immediately because ExecuteAsync checks
        /// IsCircuitOpen before dispatching.
        /// </summary>
        [Test]
        public async Task CircuitBreaker_OpensAfterConsecutive500s_ThrowsWithoutDispatch()
        {
            int httpCallCount = 0;
            var handler = new SequenceHandler(() =>
            {
                Interlocked.Increment(ref httpCallCount);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(
                        "Internal Server Error",
                        System.Text.Encoding.UTF8,
                        "text/plain"),
                };
            });

            using (var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://fake-server/"),
            })
            using (var client = new GameApiTypedClient(httpClient, "test-app"))
            {
                // First call: retry will try 4 times (1 + 3 retries).
                // The circuit breaker sees 3 failures then opens.
                // The 4th attempt hits the open breaker.
                // The overall call should throw (ApiHttpException or
                // BrokenCircuitException bubbles up through the retry).
                Exception caughtException = null;
                try
                {
                    await client.GetAssetLocationsAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }

                Assert.That(
                    caughtException,
                    Is.Not.Null,
                    "First call should throw after all retries exhausted");
                Assert.That(
                    client.IsCircuitOpen,
                    Is.True,
                    "Circuit breaker should be open after consecutive 500s");

                // Record the call count after first attempt
                int callsAfterFirstAttempt = httpCallCount;

                // Second call: ExecuteAsync checks IsCircuitOpen first and throws
                // BrokenCircuitException without making any HTTP call.
                Exception secondException = null;
                try
                {
                    await client.GetAssetLocationsAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    secondException = ex;
                }

                Assert.That(
                    secondException,
                    Is.Not.Null,
                    "Second call should throw immediately");
                Assert.That(
                    secondException.GetType().Name,
                    Is.EqualTo("BrokenCircuitException"),
                    "Should throw BrokenCircuitException when circuit is open");

                // Verify no additional HTTP calls were made (blocked before dispatch)
                Assert.That(
                    httpCallCount,
                    Is.EqualTo(callsAfterFirstAttempt),
                    "No HTTP calls should occur when circuit is open");
            }
        }

        /// <summary>
        /// 12.2 - Verifies retry succeeds on the third attempt.
        ///
        /// Mock handler returns 500, 500, then 200 with a valid JSON envelope.
        /// Polly retries twice (after the first 500, and after the second 500),
        /// then the third attempt succeeds with 200.
        /// The method should return the expected data without throwing.
        /// </summary>
        [Test]
        public async Task Retry_SucceedsOnThirdAttempt_AfterTwo500s()
        {
            int httpCallCount = 0;
            var handler = new SequenceHandler(() =>
            {
                int callNumber = Interlocked.Increment(ref httpCallCount);
                if (callNumber <= 2)
                {
                    // First two calls return 500
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent(
                            "Internal Server Error",
                            System.Text.Encoding.UTF8,
                            "text/plain"),
                    };
                }

                // Third call returns 200 with valid asset locations envelope
                string json = "{\"success\":true,\"returnCode\":0,"
                    + "\"returnString\":\"\","
                    + "\"data\":{\"locations\":["
                    + "{\"locationId\":42,"
                    + "\"locationType\":\"station\","
                    + "\"locationName\":\"Alpha Station\","
                    + "\"itemCount\":5}]},\"errors\":[]}";

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        json,
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
                var result = await client.GetAssetLocationsAsync()
                    .ConfigureAwait(false);

                // Verify the response was returned successfully
                Assert.That(result, Is.Not.Null,
                    "Result should not be null after retry succeeds");
                Assert.That(result.Locations, Is.Not.Null,
                    "Locations should be populated");
                Assert.That(result.Locations.Count, Is.EqualTo(1),
                    "Should have one location");

                var firstLocation = result.Locations.First();
                Assert.That(firstLocation.LocationId, Is.EqualTo(42));
                Assert.That(
                    firstLocation.LocationName,
                    Is.EqualTo("Alpha Station"));

                // Verify exactly 3 HTTP calls were made
                // (1 original + 2 retries)
                Assert.That(httpCallCount, Is.EqualTo(3),
                    "Should make exactly 3 HTTP calls (2 failures + 1 success)");
            }
        }

        /// <summary>
        /// A mock HTTP message handler that invokes a factory function for each
        /// request. Supports sequential response patterns via closure state.
        /// </summary>
        private class SequenceHandler : HttpMessageHandler
        {
            private readonly Func<HttpResponseMessage> _responseFactory;

            /// <summary>
            /// Initializes a new instance of the <see cref="SequenceHandler"/>
            /// class.
            /// </summary>
            /// <param name="responseFactory">Factory producing responses.</param>
            public SequenceHandler(Func<HttpResponseMessage> responseFactory)
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
