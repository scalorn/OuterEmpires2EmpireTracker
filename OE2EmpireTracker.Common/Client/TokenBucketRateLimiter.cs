// <copyright file="TokenBucketRateLimiter.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Common.Client
{
    /// <summary>
    /// Token-bucket rate limiter enforcing 0.9 TPS throughput and 9 max concurrent requests.
    /// Thread-safe: uses a lock to protect token bucket state and a SemaphoreSlim for concurrency.
    /// </summary>
    internal class TokenBucketRateLimiter
    {
        /// <summary>
        /// Maximum pause duration in seconds (5 minutes).
        /// </summary>
        private const int MaxPauseSeconds = 300;

        /// <summary>
        /// Maximum number of tokens in the bucket.
        /// </summary>
        private const double MaxTokens = 1.0;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly SemaphoreSlim _concurrency;
        private readonly double _tokensPerSecond;
        private readonly object _lock = new object();

        private double _tokens;
        private DateTime _lastRefill;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenBucketRateLimiter"/> class.
        /// </summary>
        public TokenBucketRateLimiter()
        {
            _concurrency = new SemaphoreSlim(9, 9);
            _tokensPerSecond = 0.9;
            _tokens = 1.0;
            _lastRefill = SystemClock.UtcNow;
        }

        /// <summary>
        /// Acquires a rate limiter slot. Waits for a concurrency slot (max 9 inflight)
        /// and then waits for a throughput token (0.9 TPS ≈ 1 token every 1.11 seconds).
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task that completes when both concurrency and throughput are available.</returns>
        public async Task AcquireAsync(CancellationToken ct)
        {
            await _concurrency.WaitAsync(ct).ConfigureAwait(false);

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                int delayMs;
                lock (_lock)
                {
                    RefillTokens();

                    if (_tokens >= 1.0)
                    {
                        _tokens -= 1.0;
                        return;
                    }

                    double deficit = 1.0 - _tokens;
                    double waitSeconds = deficit / _tokensPerSecond;
                    delayMs = (int)Math.Ceiling(waitSeconds * 1000.0);
                }

                Log.Debug("Rate limiter: waiting {0}ms for throughput token.", delayMs);
                await SystemClock.DelayAsync(delayMs, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Releases a concurrency slot back to the rate limiter.
        /// Must be called after each successful <see cref="AcquireAsync"/> in a finally block.
        /// </summary>
        public void Release()
        {
            _concurrency.Release();
        }

        /// <summary>
        /// Pauses the rate limiter for the specified duration (called on HTTP 429).
        /// Drains all tokens and advances the next-available time forward.
        /// Duration is capped at 300 seconds.
        /// </summary>
        /// <param name="duration">The duration to pause for.</param>
        public void PauseFor(TimeSpan duration)
        {
            TimeSpan capped = duration;
            if (capped.TotalSeconds > MaxPauseSeconds)
            {
                capped = TimeSpan.FromSeconds(MaxPauseSeconds);
            }

            lock (_lock)
            {
                _tokens = 0;
                _lastRefill = SystemClock.UtcNow + capped;
                Log.Info("Rate limiter paused for {0:F1}s (capped from {1:F1}s).", capped.TotalSeconds, duration.TotalSeconds);
            }
        }

        /// <summary>
        /// Refills tokens based on elapsed time since last refill.
        /// Must be called within the lock.
        /// </summary>
        private void RefillTokens()
        {
            DateTime now = SystemClock.UtcNow;
            double elapsed = (now - _lastRefill).TotalSeconds;

            if (elapsed > 0)
            {
                _tokens += elapsed * _tokensPerSecond;
                if (_tokens > MaxTokens)
                {
                    _tokens = MaxTokens;
                }

                _lastRefill = now;
            }
        }
    }
}
