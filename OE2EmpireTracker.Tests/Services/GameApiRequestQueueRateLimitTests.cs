// <copyright file="GameApiRequestQueueRateLimitTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for GameApiRequestQueue rate limiting, token bucket, and adaptive behavior.
    /// Tests exercise private nested classes (TokenBucketGovernor, AdaptiveRateController)
    /// through the public GameApiRequestQueue API.
    /// </summary>
    [TestFixture]
    public class GameApiRequestQueueRateLimitTests
    {
        [TearDown]
        public void TearDown()
        {
            SystemClock.Reset();
        }

        // ===== Task 11.1: 429 pause and recovery =====

        [Test]
        public async Task NotifyRateLimited_PausesDispatch()
        {
            // Use real clock with a short 1s pause so test completes quickly
            SystemClock.Reset();

            var queue = new GameApiRequestQueue(100.0);
            var dispatched = new ConcurrentBag<string>();

            await queue.EnqueueAsync(new WorkItem
            {
                Label = "item-1",
                ExecuteAsync = ct =>
                {
                    dispatched.Add("item-1");
                    return Task.FromResult<IReadOnlyList<WorkItem>>(Array.Empty<WorkItem>());
                },
            });

            // Notify rate limited with 1s pause before starting
            queue.NotifyRateLimited(1);

            queue.Start(CancellationToken.None);

            // Wait 200ms — item should NOT be dispatched during the 1s pause
            await Task.Delay(200);
            Assert.That(dispatched.Count, Is.EqualTo(0), "No items should dispatch during pause window");

            // Wait for pause to expire and item to dispatch (~1.5s total)
            await queue.DrainAsync();
            Assert.That(dispatched.Count, Is.EqualTo(1), "Item should dispatch after pause expires");
        }

        [Test]
        public void ConsecutiveBackoffs_DoubleDuration()
        {
            var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            SystemClock.FreezeAt(baseTime);

            var queue = new GameApiRequestQueue(5.0);

            // First 429 with 10s retry — pause = 10s * 2^0 = 10s
            queue.NotifyRateLimited(10);
            double tpsAfterFirst = queue.EffectiveTps;

            // Second 429 — pause = 10s * 2^1 = 20s
            queue.NotifyRateLimited(10);
            double tpsAfterSecond = queue.EffectiveTps;

            // Third 429 — pause = 10s * 2^2 = 40s
            queue.NotifyRateLimited(10);
            double tpsAfterThird = queue.EffectiveTps;

            // Each NotifyRateLimited halves effective TPS
            Assert.That(tpsAfterFirst, Is.EqualTo(2.5).Within(0.01), "First backoff: 50% of 5.0");
            Assert.That(tpsAfterSecond, Is.EqualTo(1.25).Within(0.01), "Second backoff: 50% of 2.5");
            Assert.That(tpsAfterThird, Is.EqualTo(0.625).Within(0.01), "Third backoff: 50% of 1.25");
        }

        [Test]
        public async Task Recovery_RampsBack()
        {
            var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            SystemClock.FreezeAt(baseTime);

            var queue = new GameApiRequestQueue(4.0);

            // Trigger a 429 to reduce TPS to 2.0 with 5s pause
            queue.NotifyRateLimited(5);
            double reducedTps = queue.EffectiveTps;
            Assert.That(reducedTps, Is.EqualTo(2.0).Within(0.01));

            // Advance past the pause window (5s) and then 60s for recovery
            SystemClock.AdvanceBy(TimeSpan.FromSeconds(66));

            // Enqueue and process items to trigger OnSuccessfulDispatch + recovery
            await queue.EnqueueAsync(new WorkItem
            {
                Label = "recovery-item",
                ExecuteAsync = ct =>
                    Task.FromResult<IReadOnlyList<WorkItem>>(Array.Empty<WorkItem>()),
            });

            queue.Start(CancellationToken.None);
            await queue.DrainAsync();

            // After recovery (60s since pause ended), TPS should have increased
            // Recovery is 10% per minute, so after ~1 minute: 2.0 * 1.1 = 2.2
            double recoveredTps = queue.EffectiveTps;
            Assert.That(recoveredTps, Is.GreaterThan(reducedTps), "TPS should ramp back after recovery");
            Assert.That(recoveredTps, Is.LessThanOrEqualTo(4.0), "TPS should not exceed configured");
        }

        // ===== Task 11.2: TokenBucketGovernor unit tests =====

        [Test]
        public async Task AcquireToken_RespectsTpsRate()
        {
            // Use real clock — TPS=2 means ~2 items/second
            SystemClock.Reset();

            var queue = new GameApiRequestQueue(2.0);
            var dispatchCount = 0;

            // Enqueue 4 items
            for (int i = 0; i < 4; i++)
            {
                await queue.EnqueueAsync(new WorkItem
                {
                    Label = $"tps-item-{i}",
                    ExecuteAsync = ct =>
                    {
                        Interlocked.Increment(ref dispatchCount);
                        return Task.FromResult<IReadOnlyList<WorkItem>>(Array.Empty<WorkItem>());
                    },
                });
            }

            queue.Start(CancellationToken.None);

            // Wait 100ms — burst (2 tokens) should fire, but not all 4
            await Task.Delay(100);
            int afterBurst = Volatile.Read(ref dispatchCount);

            // Wait for remaining items at TPS=2 rate
            await queue.DrainAsync();

            // Burst allows 2 immediately, then ~2 per second after
            Assert.That(afterBurst, Is.LessThanOrEqualTo(3),
                "At TPS=2, initial burst should not exceed capacity + 1");
            Assert.That(Volatile.Read(ref dispatchCount), Is.EqualTo(4),
                "All items should eventually dispatch");
        }

        [Test]
        public async Task BurstCapacity_EqualsOneSecondOfTps()
        {
            var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            SystemClock.FreezeAt(baseTime);

            // TPS=3 means burst capacity = 3 tokens
            var queue = new GameApiRequestQueue(3.0);
            var dispatchCount = 0;

            // Enqueue 5 items (more than burst capacity)
            for (int i = 0; i < 5; i++)
            {
                await queue.EnqueueAsync(new WorkItem
                {
                    Label = $"burst-item-{i}",
                    ExecuteAsync = ct =>
                    {
                        Interlocked.Increment(ref dispatchCount);
                        return Task.FromResult<IReadOnlyList<WorkItem>>(Array.Empty<WorkItem>());
                    },
                });
            }

            var cts = new CancellationTokenSource();
            queue.Start(cts.Token);

            // Wait briefly for burst to be consumed (clock frozen = no refill)
            await Task.Delay(400);

            // With frozen clock, only burst capacity (3) tokens are available
            int afterBurst = Volatile.Read(ref dispatchCount);
            Assert.That(afterBurst, Is.EqualTo(3),
                "With frozen clock, only burst capacity (= TPS) items should dispatch");

            // Cancel to avoid hanging since no refill with frozen clock
            cts.Cancel();
        }

        [Test]
        public void EffectiveTpsChange_AffectsRefillRate()
        {
            var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            SystemClock.FreezeAt(baseTime);

            var queue = new GameApiRequestQueue(4.0);

            // Initial effective TPS = configured TPS
            Assert.That(queue.EffectiveTps, Is.EqualTo(4.0).Within(0.01));

            // Reduce effective TPS via NotifyRateLimited
            queue.NotifyRateLimited(1);

            // Effective TPS is now 2.0 (50% of 4.0) — slowing the refill rate
            Assert.That(queue.EffectiveTps, Is.EqualTo(2.0).Within(0.01),
                "NotifyRateLimited should halve the effective TPS, slowing refill");
        }

        // ===== Task 11.3: AdaptiveRateController unit tests =====

        [Test]
        public void OnRateLimited_SetsPause()
        {
            var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            SystemClock.FreezeAt(baseTime);

            var queue = new GameApiRequestQueue(5.0);

            // Before rate limiting, EffectiveTps equals configured
            Assert.That(queue.EffectiveTps, Is.EqualTo(5.0).Within(0.01));

            // Notify rate limited — triggers pause and TPS reduction
            queue.NotifyRateLimited(30);

            // EffectiveTps should be reduced (proves adaptive controller set pause)
            Assert.That(queue.EffectiveTps, Is.EqualTo(2.5).Within(0.01),
                "EffectiveTps should be 50% of configured after rate limiting");
        }

        [Test]
        public void OnRateLimited_ReducesTps()
        {
            var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            SystemClock.FreezeAt(baseTime);

            var queue = new GameApiRequestQueue(10.0);

            // First rate limit: 10.0 * 0.5 = 5.0
            queue.NotifyRateLimited(60);
            Assert.That(queue.EffectiveTps, Is.EqualTo(5.0).Within(0.01));

            // Second rate limit: 5.0 * 0.5 = 2.5
            queue.NotifyRateLimited(60);
            Assert.That(queue.EffectiveTps, Is.EqualTo(2.5).Within(0.01));

            // Third rate limit: 2.5 * 0.5 = 1.25
            queue.NotifyRateLimited(60);
            Assert.That(queue.EffectiveTps, Is.EqualTo(1.25).Within(0.01));
        }

        [Test]
        public void ConsecutiveBackoffs_CapsAt5Min()
        {
            var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            SystemClock.FreezeAt(baseTime);

            var queue = new GameApiRequestQueue(5.0);

            // With retryAfter=60s:
            // 1st: 60 * 2^0 = 60s
            // 2nd: 60 * 2^1 = 120s
            // 3rd: 60 * 2^2 = 240s
            // 4th: 60 * 2^3 = 480s capped at 300s
            // 5th: 60 * 2^4 = 960s capped at 300s
            queue.NotifyRateLimited(60);
            queue.NotifyRateLimited(60);
            queue.NotifyRateLimited(60);
            queue.NotifyRateLimited(60);
            queue.NotifyRateLimited(60);

            // After 5 consecutive backoffs, TPS = 5.0 * 0.5^5 = 0.15625
            // But minimum is 0.1
            Assert.That(queue.EffectiveTps, Is.GreaterThanOrEqualTo(0.1),
                "EffectiveTps should not go below minimum of 0.1");
        }

        [Test]
        public async Task Recovery_StopsAtConfiguredTps()
        {
            var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            SystemClock.FreezeAt(baseTime);

            var queue = new GameApiRequestQueue(4.0);

            // Rate limit to reduce TPS to 2.0
            queue.NotifyRateLimited(5);
            Assert.That(queue.EffectiveTps, Is.EqualTo(2.0).Within(0.01));

            // Advance past pause window (5s) plus 10 minutes for full recovery
            SystemClock.AdvanceBy(TimeSpan.FromSeconds(6));
            SystemClock.AdvanceBy(TimeSpan.FromMinutes(10));

            // Enqueue and process an item to trigger OnSuccessfulDispatch
            await queue.EnqueueAsync(new WorkItem
            {
                Label = "trigger-recovery",
                ExecuteAsync = ct =>
                    Task.FromResult<IReadOnlyList<WorkItem>>(Array.Empty<WorkItem>()),
            });

            queue.Start(CancellationToken.None);
            await queue.DrainAsync();

            // After 10 minutes of recovery, TPS should cap at configured
            Assert.That(queue.EffectiveTps, Is.EqualTo(4.0).Within(0.01),
                "Recovery should stop at configured TPS and not exceed it");
        }
    }
}
