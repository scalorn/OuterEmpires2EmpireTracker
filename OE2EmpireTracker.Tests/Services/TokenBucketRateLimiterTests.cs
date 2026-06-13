// <copyright file="TokenBucketRateLimiterTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OE2EmpireTracker.Common.Client;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="TokenBucketRateLimiter"/> covering throughput enforcement,
    /// concurrency limiting, and PauseFor behavior.
    /// </summary>
    [TestFixture]
    public class TokenBucketRateLimiterTests
    {
        [TearDown]
        public void TearDown()
        {
            SystemClock.Reset();
        }

        // ===== 11.1: Throughput enforcement =====

        [Test]
        public async Task AcquireAsync_FirstCall_SucceedsImmediately()
        {
            var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            SystemClock.FreezeAt(baseTime);
            SystemClock.EnableInstantDelay();

            var limiter = new TokenBucketRateLimiter();

            // First acquire should succeed immediately (bucket starts with 1 token)
            await limiter.AcquireAsync(CancellationToken.None);

            // Clock should not have advanced — token was available
            Assert.That(SystemClock.UtcNow, Is.EqualTo(baseTime),
                "First acquire should not wait; bucket starts with 1 token");

            limiter.Release();
        }

        [Test]
        public async Task AcquireAsync_SecondCall_WaitsForTokenRefill()
        {
            var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            SystemClock.FreezeAt(baseTime);
            SystemClock.EnableInstantDelay();

            var limiter = new TokenBucketRateLimiter();

            // First acquire consumes the initial token
            await limiter.AcquireAsync(CancellationToken.None);
            limiter.Release();

            // Second acquire must wait for refill (0.9 TPS ≈ 1.11 seconds per token)
            await limiter.AcquireAsync(CancellationToken.None);
            limiter.Release();

            // Clock should have advanced by ~1.11 seconds for the second token
            TimeSpan elapsed = SystemClock.UtcNow - baseTime;
            Assert.That(elapsed.TotalSeconds, Is.EqualTo(1.0 / 0.9).Within(0.15),
                "Second acquire should wait ~1.11s for token refill at 0.9 TPS");
        }

        [Test]
        public async Task AcquireAsync_ThirdCall_WaitsFourSecondsTotal()
        {
            var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            SystemClock.FreezeAt(baseTime);
            SystemClock.EnableInstantDelay();

            var limiter = new TokenBucketRateLimiter();

            // Three sequential acquires: first is free, second waits ~1.11s, third waits ~1.11s more
            await limiter.AcquireAsync(CancellationToken.None);
            limiter.Release();

            await limiter.AcquireAsync(CancellationToken.None);
            limiter.Release();

            await limiter.AcquireAsync(CancellationToken.None);
            limiter.Release();

            TimeSpan elapsed = SystemClock.UtcNow - baseTime;
            Assert.That(elapsed.TotalSeconds, Is.EqualTo(2.0 / 0.9).Within(0.2),
                "Three sequential acquires should space ~2.22s total (0 + 1.11 + 1.11)");
        }

        // ===== 11.2: Concurrency limit =====

        [Test]
        public async Task AcquireAsync_ConcurrencyLimit_Only9Of12AcquireSimultaneously()
        {
            var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            SystemClock.FreezeAt(baseTime);
            SystemClock.EnableInstantDelay();

            var limiter = new TokenBucketRateLimiter();

            // Pre-fill the token bucket by advancing time so we have enough tokens
            // for concurrency testing (9 tokens needed). Advance 10s = 9 tokens at 0.9 TPS.
            SystemClock.AdvanceBy(TimeSpan.FromSeconds(10));

            int acquired = 0;
            var acquiredSignal = new TaskCompletionSource<bool>();
            var holdSignal = new TaskCompletionSource<bool>();
            var tasks = new List<Task>();

            for (int i = 0; i < 12; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await limiter.AcquireAsync(CancellationToken.None);
                    int count = Interlocked.Increment(ref acquired);
                    if (count == 9)
                    {
                        acquiredSignal.TrySetResult(true);
                    }

                    // Hold the slot until released
                    await holdSignal.Task;
                    limiter.Release();
                }));
            }

            // Wait for 9 to acquire (with a timeout to prevent hanging)
            var completed = await Task.WhenAny(acquiredSignal.Task, Task.Delay(5000));
            Assert.That(completed, Is.EqualTo(acquiredSignal.Task),
                "9 tasks should acquire within timeout");

            // Give a brief moment for any extras to sneak through
            await Task.Delay(200);

            // Verify only 9 acquired (the other 3 are waiting on semaphore)
            int currentAcquired = Volatile.Read(ref acquired);
            Assert.That(currentAcquired, Is.EqualTo(9),
                "Only 9 concurrent tasks should acquire; 3 should be waiting");

            // Release all held slots so tasks can complete
            holdSignal.SetResult(true);
            await Task.WhenAll(tasks);
        }

        [Test]
        public async Task AcquireAsync_AfterRelease_WaitingTasksCanProceed()
        {
            var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            SystemClock.FreezeAt(baseTime);
            SystemClock.EnableInstantDelay();

            var limiter = new TokenBucketRateLimiter();

            // Advance time to fill bucket for 9 tokens
            SystemClock.AdvanceBy(TimeSpan.FromSeconds(10));

            var holders = new List<Task>();
            var holdSignal = new TaskCompletionSource<bool>();

            // Acquire 9 slots
            for (int i = 0; i < 9; i++)
            {
                holders.Add(Task.Run(async () =>
                {
                    await limiter.AcquireAsync(CancellationToken.None);
                    await holdSignal.Task;
                    limiter.Release();
                }));
            }

            // Wait for all 9 to be acquired
            await Task.Delay(500);

            // Start a 10th task that will wait for a slot
            int extraAcquired = 0;
            var extraTask = Task.Run(async () =>
            {
                await limiter.AcquireAsync(CancellationToken.None);
                Interlocked.Increment(ref extraAcquired);
                limiter.Release();
            });

            // Brief wait — extra should NOT have acquired yet
            await Task.Delay(200);
            Assert.That(Volatile.Read(ref extraAcquired), Is.EqualTo(0),
                "10th task should be waiting for a concurrency slot");

            // Release all held slots
            holdSignal.SetResult(true);
            await Task.WhenAll(holders);

            // Now the extra task should proceed
            await extraTask;
            Assert.That(Volatile.Read(ref extraAcquired), Is.EqualTo(1),
                "10th task should acquire after slots are released");
        }

        // ===== 11.3: PauseFor =====

        [Test]
        public async Task PauseFor_BlocksAcquireUntilPauseExpires()
        {
            var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            SystemClock.FreezeAt(baseTime);
            SystemClock.EnableInstantDelay();

            var limiter = new TokenBucketRateLimiter();

            // Consume the initial token
            await limiter.AcquireAsync(CancellationToken.None);
            limiter.Release();

            // Pause for 10 seconds
            limiter.PauseFor(TimeSpan.FromSeconds(10));

            // Next acquire should wait for the pause to expire
            await limiter.AcquireAsync(CancellationToken.None);
            limiter.Release();

            // The clock should have advanced past the pause duration
            TimeSpan elapsed = SystemClock.UtcNow - baseTime;
            Assert.That(elapsed.TotalSeconds, Is.GreaterThanOrEqualTo(10.0),
                "AcquireAsync should block until pause expires (10s)");
        }

        [Test]
        public async Task PauseFor_CapsAt300Seconds()
        {
            var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            SystemClock.FreezeAt(baseTime);
            SystemClock.EnableInstantDelay();

            var limiter = new TokenBucketRateLimiter();

            // Consume the initial token
            await limiter.AcquireAsync(CancellationToken.None);
            limiter.Release();

            // Pause for 600 seconds (should be capped at 300)
            limiter.PauseFor(TimeSpan.FromSeconds(600));

            // Next acquire should wait for the capped duration
            await limiter.AcquireAsync(CancellationToken.None);
            limiter.Release();

            TimeSpan elapsed = SystemClock.UtcNow - baseTime;
            Assert.That(elapsed.TotalSeconds, Is.GreaterThanOrEqualTo(300.0),
                "Pause should be capped at 300 seconds");
            Assert.That(elapsed.TotalSeconds, Is.LessThan(600.0),
                "Pause should not exceed cap of 300 seconds");
        }
    }
}
