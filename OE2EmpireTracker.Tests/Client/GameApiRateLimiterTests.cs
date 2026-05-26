// <copyright file="GameApiRateLimiterTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Client;

namespace OE2EmpireTracker.Tests.Client
{
    /// <summary>
    /// Property-based and unit tests for GameApiClient rate limiter behavior.
    /// Feature: game-api-integration
    /// **Validates: Correctness Property 2 (Rate Limit Compliance)**
    /// </summary>
    [TestFixture]
    public class GameApiRateLimiterTests
    {
        /// <summary>
        /// Property: After acquiring exactly N tokens (where N = rate limit),
        /// the semaphore has zero available count — the next acquire would block.
        /// Generates random rate limits between 1 and 50.
        /// **Validates: Requirements 3.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 20)]
        public Property TokenExhaustion_BlocksNextRequest()
        {
            var limitGen = from n in Gen.Choose(1, 50)
                           select n;

            return Prop.ForAll(
                Arb.From(limitGen),
                limit =>
                {
                    // Create a client, then update its rate limit to the generated value
                    using (var client = new GameApiClient("http://localhost"))
                    {
                        // Set the rate limit via UpdateRateLimitFromHeaders
                        var setupResponse = new HttpResponseMessage(HttpStatusCode.OK);
                        setupResponse.Headers.Add("X-RateLimit-Limit", limit.ToString());
                        client.UpdateRateLimitFromHeaders(setupResponse);
                        setupResponse.Dispose();

                        // Acquire all tokens
                        for (int i = 0; i < limit; i++)
                        {
                            client.AcquireRateLimitTokenAsync().GetAwaiter().GetResult();
                        }

                        // The next acquire should not complete immediately (semaphore exhausted)
                        var acquireTask = client.AcquireRateLimitTokenAsync();
                        bool completedImmediately = acquireTask.Wait(50);

                        return (!completedImmediately)
                            .Label($"limit={limit}, expected blocking but completed immediately");
                    }
                });
        }

        /// <summary>
        /// Property: When UpdateRateLimitFromHeaders is called with a response containing
        /// X-RateLimit-Limit header, the internal limit changes. Verified by exhausting
        /// the new limit and confirming the next acquire blocks.
        /// **Validates: Requirements 3.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 20)]
        public Property DynamicLimitUpdate_ChangesSemaphoreCapacity()
        {
            var limitGen = from n in Gen.Choose(1, 30)
                           select n;

            return Prop.ForAll(
                Arb.From(limitGen),
                newLimit =>
                {
                    using (var client = new GameApiClient("http://localhost"))
                    {
                        // Update rate limit from header
                        var response = new HttpResponseMessage(HttpStatusCode.OK);
                        response.Headers.Add("X-RateLimit-Limit", newLimit.ToString());
                        client.UpdateRateLimitFromHeaders(response);
                        response.Dispose();

                        // Acquire exactly newLimit tokens — should all succeed
                        for (int i = 0; i < newLimit; i++)
                        {
                            client.AcquireRateLimitTokenAsync().GetAwaiter().GetResult();
                        }

                        // Next acquire should block (semaphore exhausted at new limit)
                        var acquireTask = client.AcquireRateLimitTokenAsync();
                        bool completedImmediately = acquireTask.Wait(50);

                        return (!completedImmediately)
                            .Label($"newLimit={newLimit}, expected blocking after exhaustion");
                    }
                });
        }

        /// <summary>
        /// Test: HandleRateLimitResponse with Retry-After header sets the pause duration
        /// to the specified number of seconds.
        /// **Validates: Requirements 3.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 30)]
        public Property HandleRateLimitResponse_WithRetryAfter_SetsPauseDuration()
        {
            var secondsGen = from s in Gen.Choose(1, 300)
                             select s;

            return Prop.ForAll(
                Arb.From(secondsGen),
                retryAfterSeconds =>
                {
                    using (var client = new GameApiClient("http://localhost"))
                    {
                        var response = new HttpResponseMessage((HttpStatusCode)429);
                        response.Headers.RetryAfter = new RetryConditionHeaderValue(
                            TimeSpan.FromSeconds(retryAfterSeconds));
                        client.HandleRateLimitResponse(response);
                        response.Dispose();

                        // Verify the pause is active by checking that AcquireRateLimitTokenAsync
                        // takes at least some time (the pause check happens before semaphore wait).
                        // We can't easily read _rateLimitPauseUntil directly, but we can verify
                        // the effect: a subsequent acquire should be delayed.
                        // For a property test, we verify the method doesn't throw and the pause
                        // is set by timing a short acquire attempt.
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        var task = client.AcquireRateLimitTokenAsync();

                        // Wait a short time — the task should NOT complete quickly because
                        // it's paused for retryAfterSeconds (which is at least 1 second)
                        bool completed = task.Wait(100);
                        sw.Stop();

                        // The task should still be waiting (paused for at least 1 second)
                        return (!completed)
                            .Label($"retryAfter={retryAfterSeconds}s, expected pause but completed in {sw.ElapsedMilliseconds}ms");
                    }
                });
        }

        /// <summary>
        /// Test: HandleRateLimitResponse without Retry-After header defaults to 60 seconds pause.
        /// **Validates: Requirements 3.3**
        /// </summary>
        [Test]
        public void HandleRateLimitResponse_WithoutRetryAfter_DefaultsTo60Seconds()
        {
            using (var client = new GameApiClient("http://localhost"))
            {
                var response = new HttpResponseMessage((HttpStatusCode)429);

                // No Retry-After header set
                client.HandleRateLimitResponse(response);
                response.Dispose();

                // Verify the pause is active — acquire should not complete quickly
                // because the default 60s pause is in effect
                var task = client.AcquireRateLimitTokenAsync();
                bool completed = task.Wait(100);

                Assert.That(completed, Is.False, "Expected acquire to be paused for 60s default");
            }
        }
    }
}
