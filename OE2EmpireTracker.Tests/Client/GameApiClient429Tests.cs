// <copyright file="GameApiClient429Tests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using NUnit.Framework;
using OE2EmpireTracker.Client;

namespace OE2EmpireTracker.Tests.Client
{
    /// <summary>
    /// Unit tests for GameApiClient HTTP 429 handling and X-RateLimit-Limit header processing.
    /// Tests the internal HandleRateLimitResponse and UpdateRateLimitFromHeaders methods directly.
    /// Feature: game-api-integration
    /// **Validates: Requirements 3.2, 3.3, 3.4**
    /// </summary>
    [TestFixture]
    public class GameApiClient429Tests
    {
        /// <summary>
        /// When HandleRateLimitResponse receives a 429 with Retry-After: 2 header,
        /// subsequent AcquireRateLimitTokenAsync calls block for at least the specified duration.
        /// Uses a short Retry-After value (2 seconds) and verifies the acquire does not
        /// complete within 100ms (proving the pause is active).
        /// </summary>
        [Test]
        public void HandleRateLimitResponse_With429AndRetryAfterHeader_PausesForSpecifiedDuration()
        {
            using (var client = new GameApiClient("http://localhost"))
            using (var response = new HttpResponseMessage((HttpStatusCode)429))
            {
                response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(2));

                client.HandleRateLimitResponse(response);

                // AcquireRateLimitTokenAsync should block because the pause is active (2 seconds)
                var acquireTask = client.AcquireRateLimitTokenAsync();
                bool completedWithin100Ms = acquireTask.Wait(100);

                Assert.That(completedWithin100Ms, Is.False, "Expected acquire to block due to 2-second Retry-After pause");
            }
        }

        /// <summary>
        /// When HandleRateLimitResponse receives a 429 without a Retry-After header,
        /// the default pause of 60 seconds is applied. Verified by checking that
        /// AcquireRateLimitTokenAsync does not complete within 100ms.
        /// </summary>
        [Test]
        public void HandleRateLimitResponse_With429WithoutRetryAfterHeader_PausesFor60Seconds()
        {
            using (var client = new GameApiClient("http://localhost"))
            using (var response = new HttpResponseMessage((HttpStatusCode)429))
            {
                // No Retry-After header set on the response
                client.HandleRateLimitResponse(response);

                // AcquireRateLimitTokenAsync should block because the default 60s pause is active
                var acquireTask = client.AcquireRateLimitTokenAsync();
                bool completedWithin100Ms = acquireTask.Wait(100);

                Assert.That(completedWithin100Ms, Is.False, "Expected acquire to block due to default 60-second pause");
            }
        }

        /// <summary>
        /// When a response contains X-RateLimit-Limit: 10 header, UpdateRateLimitFromHeaders
        /// changes the internal limit. Verified by exhausting exactly 10 tokens (all succeed)
        /// and confirming the 11th acquire blocks.
        /// </summary>
        [Test]
        public void UpdateRateLimitFromHeaders_WithXRateLimitLimitHeader_UpdatesInternalLimit()
        {
            using (var client = new GameApiClient("http://localhost"))
            using (var response = new HttpResponseMessage(HttpStatusCode.OK))
            {
                response.Headers.Add("X-RateLimit-Limit", "10");

                client.UpdateRateLimitFromHeaders(response);

                // Exhaust all 10 tokens — each should succeed immediately
                for (int i = 0; i < 10; i++)
                {
                    bool acquired = client.AcquireRateLimitTokenAsync().Wait(100);
                    Assert.That(acquired, Is.True, $"Token {i + 1} of 10 should be acquired immediately");
                }

                // The 11th acquire should block (semaphore exhausted at new limit of 10)
                var eleventhAcquire = client.AcquireRateLimitTokenAsync();
                bool completedWithin100Ms = eleventhAcquire.Wait(100);

                Assert.That(completedWithin100Ms, Is.False, "Expected 11th acquire to block after exhausting limit of 10");
            }
        }

        /// <summary>
        /// When HandleRateLimitResponse receives a non-429 response (HTTP 200),
        /// no pause is applied and AcquireRateLimitTokenAsync completes immediately.
        /// </summary>
        [Test]
        public void HandleRateLimitResponse_WithNon429Response_DoesNotTriggerPause()
        {
            using (var client = new GameApiClient("http://localhost"))
            using (var response = new HttpResponseMessage(HttpStatusCode.OK))
            {
                client.HandleRateLimitResponse(response);

                // Acquire should complete immediately — no pause was set
                bool acquired = client.AcquireRateLimitTokenAsync().Wait(100);

                Assert.That(acquired, Is.True, "Expected acquire to complete immediately for non-429 response");
            }
        }
    }
}
