// <copyright file="TokenBucketRateLimiterTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OE2EmpireTracker.Common.Client;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="TokenBucketRateLimiter"/> covering throughput enforcement
    /// and PauseFor behavior.
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
