// <copyright file="GameApiRequestQueue.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// General-purpose parallel task queue with token-bucket rate governing.
    /// Dispatches async work items at a configurable TPS, supports cascading work,
    /// and adapts to HTTP 429 responses with backoff/recovery.
    /// </summary>
    public class GameApiRequestQueue
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ConcurrentQueue<WorkItem> _queue = new ConcurrentQueue<WorkItem>();
        private readonly ConcurrentBag<QueueError> _errors = new ConcurrentBag<QueueError>();
        private readonly TimeSpan _perItemTimeout;
        private readonly CompletionTracker _tracker = new CompletionTracker();
        private readonly AdaptiveRateController _rateController;
        private readonly TokenBucketGovernor _governor;
        private readonly SemaphoreSlim _inflightLimiter;
        private readonly int _maxInflight;
        private readonly int _maxRetries;
        private readonly StreamWriter _metricsWriter;
        private readonly object _metricsLock = new object();

        private Task _dispatchLoopTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameApiRequestQueue"/> class.
        /// </summary>
        /// <param name="configuredTps">The target transactions per second rate.</param>
        /// <param name="perItemTimeout">Optional per-item timeout. Defaults to 30 seconds.</param>
        /// <param name="maxInflight">Max concurrent in-flight requests. Defaults to 3.</param>
        /// <param name="maxRetries">Maximum number of retry attempts on failure. Defaults to 3.</param>
        /// <param name="metricsFilePath">Optional file path for CSV metrics output. Null disables file output.</param>
        public GameApiRequestQueue(double configuredTps, TimeSpan? perItemTimeout = null, int maxInflight = 3, int maxRetries = 3, string metricsFilePath = null)
        {
            ConfiguredTps = configuredTps;
            _perItemTimeout = perItemTimeout ?? TimeSpan.FromSeconds(30);
            _rateController = new AdaptiveRateController(configuredTps);
            _governor = new TokenBucketGovernor(configuredTps, () => _rateController.EffectiveTps);
            _maxInflight = Math.Max(1, maxInflight);
            _inflightLimiter = new SemaphoreSlim(_maxInflight, _maxInflight);
            _maxRetries = maxRetries;

            if (metricsFilePath != null)
            {
                _metricsWriter = new StreamWriter(metricsFilePath, append: false, encoding: new System.Text.UTF8Encoding(false));
                _metricsWriter.WriteLine("Timestamp,Label,Status,DurationMs,Attempt,Inflight,EffectiveTps");
                _metricsWriter.Flush();
            }
        }

        /// <summary>
        /// Gets the configured TPS rate (as specified at construction time).
        /// </summary>
        public double ConfiguredTps { get; }

        /// <summary>
        /// Gets the current effective TPS (may be lower than configured during 429 backoff).
        /// </summary>
        public double EffectiveTps => _rateController.EffectiveTps;

        /// <summary>
        /// Gets the current completion status (processed, succeeded, failed counts).
        /// </summary>
        public QueueCompletionStatus CompletionStatus => new QueueCompletionStatus
        {
            TotalProcessed = _tracker.Succeeded + _tracker.Failed,
            Succeeded = _tracker.Succeeded,
            Failed = _tracker.Failed,
        };

        /// <summary>
        /// Gets the list of errors that occurred during work item execution.
        /// </summary>
        public IReadOnlyList<QueueError> Errors => _errors.ToArray();

        /// <summary>
        /// Enqueues a work item for dispatch. Thread-safe for concurrent callers.
        /// </summary>
        /// <param name="workItem">The work item to enqueue.</param>
        /// <returns>A task that completes when the item is enqueued.</returns>
        public Task EnqueueAsync(WorkItem workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            _tracker.OnEnqueued();
            _queue.Enqueue(workItem);
            Log.Debug("Enqueued work item: {0}", workItem.Label);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Starts the dispatch loop. Call after enqueuing seed items.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the dispatch loop.</param>
        public void Start(CancellationToken cancellationToken = default)
        {
            Log.Info("Starting dispatch loop at configured TPS={0:F1}, maxInflight={1}", ConfiguredTps, _maxInflight);
            _dispatchLoopTask = DispatchLoopAsync(cancellationToken);
        }

        /// <summary>
        /// Awaits completion of all enqueued items (including cascaded work).
        /// </summary>
        /// <returns>A task that completes when the queue is fully drained.</returns>
        public async Task DrainAsync()
        {
            await _tracker.WaitForDrainAsync().ConfigureAwait(false);

            if (_dispatchLoopTask != null)
            {
                await _dispatchLoopTask.ConfigureAwait(false);
            }

            CloseMetricsWriter();

            Log.Info(
                "Queue drained. Succeeded={0}, Failed={1}",
                _tracker.Succeeded,
                _tracker.Failed);
        }

        /// <summary>
        /// Notifies the queue that an HTTP 429 was received, triggering a pause and TPS reduction.
        /// </summary>
        /// <param name="retryAfterSeconds">The retry-after delay in seconds (from server header).</param>
        public void NotifyRateLimited(int retryAfterSeconds)
        {
            Log.Warn("Rate limited — pausing for {0}s", retryAfterSeconds);
            _rateController.OnRateLimited(retryAfterSeconds);
        }

        private async Task DispatchLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (_rateController.IsPaused)
                    {
                        var remaining = _rateController.GetPauseRemaining();
                        if (remaining > TimeSpan.Zero)
                        {
                            await SystemClock.DelayAsync((int)remaining.TotalMilliseconds, ct).ConfigureAwait(false);
                        }

                        continue;
                    }

                    await _governor.AcquireTokenAsync(ct).ConfigureAwait(false);

                    if (!_queue.TryDequeue(out WorkItem item))
                    {
                        if (_tracker.IsFullyDrained)
                        {
                            break;
                        }

                        await SystemClock.DelayAsync(50, ct).ConfigureAwait(false);
                        continue;
                    }

                    _tracker.OnDispatched();
                    await _inflightLimiter.WaitAsync(ct).ConfigureAwait(false);
                    Log.Debug("Dispatching '{0}' (inflight={1})", item.Label, _maxInflight - _inflightLimiter.CurrentCount);
                    _ = ExecuteWorkItemAsync(item, ct);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Info("Dispatch loop cancelled.");
            }
        }

        private async Task ExecuteWorkItemAsync(WorkItem item, CancellationToken ct)
        {
            var startTime = SystemClock.UtcNow;
            Exception lastException = null;

            try
            {
                for (int attempt = 1; attempt <= _maxRetries; attempt++)
                {
                    var attemptStart = SystemClock.UtcNow;

                    try
                    {
                        using (var itemCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                        {
                            itemCts.CancelAfter(_perItemTimeout);

                            IReadOnlyList<WorkItem> cascaded = await item.ExecuteAsync(itemCts.Token)
                                .ConfigureAwait(false);

                            _rateController.OnSuccessfulDispatch();

                            if (cascaded != null && cascaded.Count > 0)
                            {
                                _tracker.OnCascadedEnqueued(cascaded.Count);
                                foreach (var followUp in cascaded)
                                {
                                    _queue.Enqueue(followUp);
                                }

                                Log.Debug(
                                    "Work item '{0}' cascaded {1} follow-up items",
                                    item.Label,
                                    cascaded.Count);
                            }

                            _tracker.OnCompleted(success: true);
                            var durationMs = (long)(SystemClock.UtcNow - attemptStart).TotalMilliseconds;
                            int inflight = _maxInflight - _inflightLimiter.CurrentCount;
                            Log.Info("METRIC|OK|{0}|{1}ms|inflight={2}", item.Label, durationMs, inflight);
                            WriteMetricRow(item.Label, "OK", durationMs, attempt, inflight);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;

                        var retryDurationMs = (long)(SystemClock.UtcNow - attemptStart).TotalMilliseconds;
                        int retryInflight = _maxInflight - _inflightLimiter.CurrentCount;

                        if (attempt < _maxRetries)
                        {
                            Log.Warn("Retrying '{0}' (attempt {1}/{2})", item.Label, attempt, _maxRetries);
                            WriteMetricRow(item.Label, "RETRY", retryDurationMs, attempt, retryInflight);
                            await SystemClock.DelayAsync(1000, ct).ConfigureAwait(false);
                        }
                    }
                }

                _errors.Add(new QueueError
                {
                    WorkItemLabel = item.Label,
                    Exception = lastException,
                });
                _tracker.OnCompleted(success: false);
                var failDurationMs = (long)(SystemClock.UtcNow - startTime).TotalMilliseconds;
                int failInflight = _maxInflight - _inflightLimiter.CurrentCount;
                Log.Error("METRIC|FAIL|{0}|{1}ms|inflight={2}|{3}", item.Label, failDurationMs, failInflight, lastException.Message);
                WriteMetricRow(item.Label, "FAIL", failDurationMs, _maxRetries, failInflight);
            }
            finally
            {
                _inflightLimiter.Release();
            }
        }

        /// <summary>
        /// Writes a single metrics CSV row. Thread-safe via lock.
        /// </summary>
        /// <param name="label">The work item label.</param>
        /// <param name="status">OK, RETRY, or FAIL.</param>
        /// <param name="durationMs">Duration in milliseconds for this attempt.</param>
        /// <param name="attempt">Which attempt number (1-based).</param>
        /// <param name="inflight">Current number of in-flight requests.</param>
        private void WriteMetricRow(string label, string status, long durationMs, int attempt, int inflight)
        {
            if (_metricsWriter == null)
            {
                return;
            }

            string timestamp = SystemClock.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            double effectiveTps = _rateController.EffectiveTps;
            string row = string.Format(
                "{0},{1},{2},{3},{4},{5},{6:F2}",
                timestamp,
                label,
                status,
                durationMs,
                attempt,
                inflight,
                effectiveTps);

            lock (_metricsLock)
            {
                _metricsWriter.WriteLine(row);
                _metricsWriter.Flush();
            }
        }

        /// <summary>
        /// Flushes and closes the metrics CSV writer.
        /// </summary>
        private void CloseMetricsWriter()
        {
            if (_metricsWriter == null)
            {
                return;
            }

            lock (_metricsLock)
            {
                _metricsWriter.Flush();
                _metricsWriter.Close();
            }
        }

        /// <summary>
        /// Tracks pending, in-flight, succeeded, and failed counts for drain signaling.
        /// </summary>
        private class CompletionTracker
        {
            private readonly TaskCompletionSource<bool> _drainTcs =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            private int _pending;
            private int _inflight;
            private int _succeeded;
            private int _failed;

            public int Succeeded => Volatile.Read(ref _succeeded);

            public int Failed => Volatile.Read(ref _failed);

            public bool IsFullyDrained =>
                Volatile.Read(ref _pending) == 0 && Volatile.Read(ref _inflight) == 0;

            public void OnEnqueued()
            {
                Interlocked.Increment(ref _pending);
            }

            public void OnDispatched()
            {
                Interlocked.Decrement(ref _pending);
                Interlocked.Increment(ref _inflight);
            }

            public void OnCompleted(bool success)
            {
                if (success)
                {
                    Interlocked.Increment(ref _succeeded);
                }
                else
                {
                    Interlocked.Increment(ref _failed);
                }

                Interlocked.Decrement(ref _inflight);
                CheckDrain();
            }

            public void OnCascadedEnqueued(int count)
            {
                Interlocked.Add(ref _pending, count);
            }

            public Task WaitForDrainAsync()
            {
                CheckDrain();
                return _drainTcs.Task;
            }

            private void CheckDrain()
            {
                if (IsFullyDrained)
                {
                    _drainTcs.TrySetResult(true);
                }
            }
        }

        /// <summary>
        /// Token-bucket rate governor. Limits dispatch rate to the effective TPS.
        /// </summary>
        private class TokenBucketGovernor
        {
            private readonly object _lock = new object();
            private readonly double _capacity;
            private readonly Func<double> _getEffectiveTps;
            private double _tokens;
            private DateTime _lastRefill;

            public TokenBucketGovernor(double configuredTps, Func<double> getEffectiveTps)
            {
                _capacity = Math.Max(configuredTps, 1.0);
                _getEffectiveTps = getEffectiveTps;
                _tokens = 1.0;
                _lastRefill = SystemClock.UtcNow;
            }

            public async Task AcquireTokenAsync(CancellationToken ct)
            {
                while (!ct.IsCancellationRequested)
                {
                    lock (_lock)
                    {
                        Refill();
                        if (_tokens >= 1.0)
                        {
                            _tokens -= 1.0;
                            return;
                        }
                    }

                    double effectiveTps = _getEffectiveTps();
                    int delayMs = effectiveTps > 0
                        ? (int)(1000.0 / effectiveTps)
                        : 1000;
                    await SystemClock.DelayAsync(Math.Max(delayMs, 10), ct).ConfigureAwait(false);
                }

                ct.ThrowIfCancellationRequested();
            }

            private void Refill()
            {
                var now = SystemClock.UtcNow;
                var elapsed = (now - _lastRefill).TotalSeconds;
                if (elapsed <= 0)
                {
                    return;
                }

                double effectiveTps = _getEffectiveTps();
                double tokensToAdd = elapsed * effectiveTps;
                _tokens = Math.Min(_tokens + tokensToAdd, _capacity);
                _lastRefill = now;
            }
        }

        /// <summary>
        /// Manages TPS reduction on 429 responses and gradual recovery.
        /// </summary>
        private class AdaptiveRateController
        {
            private readonly object _lock = new object();
            private readonly double _configuredTps;
            private double _effectiveTps;
            private int _consecutiveBackoffs;
            private DateTime _pauseUntil;
            private DateTime _lastSuccessfulDispatch;

            public AdaptiveRateController(double configuredTps)
            {
                _configuredTps = configuredTps;
                _effectiveTps = configuredTps;
                _pauseUntil = DateTime.MinValue;
                _lastSuccessfulDispatch = SystemClock.UtcNow;
            }

            public double EffectiveTps
            {
                get
                {
                    lock (_lock)
                    {
                        return _effectiveTps;
                    }
                }
            }

            public bool IsPaused
            {
                get
                {
                    lock (_lock)
                    {
                        return SystemClock.UtcNow < _pauseUntil;
                    }
                }
            }

            public void OnRateLimited(int retryAfterSeconds)
            {
                lock (_lock)
                {
                    _consecutiveBackoffs++;
                    double multiplier = Math.Pow(2, _consecutiveBackoffs - 1);
                    double pauseSeconds = retryAfterSeconds * multiplier;
                    pauseSeconds = Math.Min(pauseSeconds, 300);

                    _pauseUntil = SystemClock.UtcNow.AddSeconds(pauseSeconds);
                    _effectiveTps = _effectiveTps * 0.5;

                    if (_effectiveTps < 0.1)
                    {
                        _effectiveTps = 0.1;
                    }
                }
            }

            public void OnSuccessfulDispatch()
            {
                lock (_lock)
                {
                    _lastSuccessfulDispatch = SystemClock.UtcNow;

                    if (_consecutiveBackoffs > 0)
                    {
                        var sinceLastBackoff = (SystemClock.UtcNow - _pauseUntil).TotalSeconds;
                        if (sinceLastBackoff >= 60)
                        {
                            double minutesSinceRecovery = sinceLastBackoff / 60.0;
                            double recoveredTps = _effectiveTps * Math.Pow(1.1, minutesSinceRecovery);
                            _effectiveTps = Math.Min(recoveredTps, _configuredTps);

                            if (_effectiveTps >= _configuredTps)
                            {
                                _consecutiveBackoffs = 0;
                            }
                        }
                    }
                }
            }

            public TimeSpan GetPauseRemaining()
            {
                lock (_lock)
                {
                    var remaining = _pauseUntil - SystemClock.UtcNow;
                    return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
                }
            }
        }
    }
}
