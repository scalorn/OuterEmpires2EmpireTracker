// <copyright file="GameApiMetricsCollector.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Singleton service that collects and aggregates API request metrics.
    /// Decoupled from the form — continues collecting regardless of whether the form is open.
    /// Thread-safe: all public methods acquire the internal lock before mutating state.
    /// </summary>
    public class GameApiMetricsCollector
    {
        /// <summary>
        /// Maximum number of request records retained in history.
        /// </summary>
        private const int MaxHistorySize = 500;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static GameApiMetricsCollector _instance;

        private readonly List<RequestRecord> _history;
        private readonly object _lock = new object();

        // TPS circular buffer (300 samples at 1-second intervals = 5 minutes)
        private readonly decimal[] _tpsSamples;

        // Cumulative counters
        private long _totalRequests;
        private long _successCount;
        private long _clientErrorCount;
        private long _serverErrorCount;
        private long _exceptionCount;
        private long _rateLimitedCount;
        private long _bytesSent;
        private long _bytesReceived;

        // Outstanding tracking
        private int _outstandingRequests;
        private int _peakOutstanding;

        // TPS sampling
        private int _tpsSampleIndex;
        private decimal _currentTps;
        private System.Threading.Timer _sampleTimer;

        // Reset tracking
        private DateTime? _resetTimestamp;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameApiMetricsCollector"/> class.
        /// </summary>
        private GameApiMetricsCollector()
        {
            this._history = new List<RequestRecord>();
            this._tpsSamples = new decimal[300];
            this._sampleTimer = new System.Threading.Timer(
                this.SampleTimerCallback,
                null,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Raised after each request completion or failure, providing the updated metrics snapshot.
        /// Raised outside the lock to prevent deadlocks.
        /// </summary>
        public event EventHandler<MetricsSnapshot> MetricsUpdated;

        /// <summary>
        /// Gets the singleton instance. Returns null if not initialized.
        /// </summary>
        public static GameApiMetricsCollector Instance => _instance;

        /// <summary>
        /// Creates and assigns the singleton instance.
        /// </summary>
        public static void Initialize()
        {
            _instance = new GameApiMetricsCollector();
            Log.Info("GameApiMetricsCollector initialized.");
        }

        /// <summary>
        /// Disposes the singleton instance and sets it to null.
        /// </summary>
        public static void Reset()
        {
            var old = _instance;
            _instance = null;

            if (old != null)
            {
                old._sampleTimer?.Dispose();
                Log.Info("GameApiMetricsCollector reset.");
            }
        }

        /// <summary>
        /// Called when a request is dispatched. Increments outstanding count and updates peak.
        /// </summary>
        /// <param name="endpoint">The relative endpoint path.</param>
        /// <param name="httpMethod">The HTTP method (GET, POST, etc.).</param>
        public void OnRequestStarted(string endpoint, string httpMethod)
        {
            lock (this._lock)
            {
                this._outstandingRequests++;

                if (this._outstandingRequests > this._peakOutstanding)
                {
                    this._peakOutstanding = this._outstandingRequests;
                }
            }
        }

        /// <summary>
        /// Called when a request receives a response. Classifies the status code, records the
        /// request, decrements outstanding count, and raises MetricsUpdated.
        /// </summary>
        /// <param name="endpoint">The relative endpoint path.</param>
        /// <param name="httpMethod">The HTTP method (GET, POST, etc.).</param>
        /// <param name="statusCode">The HTTP status code received.</param>
        /// <param name="durationMs">The request duration in milliseconds.</param>
        /// <param name="bytesSent">The number of bytes sent in the request body.</param>
        /// <param name="bytesReceived">The number of bytes received in the response body.</param>
        public void OnRequestCompleted(string endpoint, string httpMethod, int statusCode, long durationMs, long bytesSent, long bytesReceived)
        {
            MetricsSnapshot snapshot;

            lock (this._lock)
            {
                this._outstandingRequests = Math.Max(0, this._outstandingRequests - 1);
                this._totalRequests++;
                this._bytesSent += bytesSent;
                this._bytesReceived += bytesReceived;

                bool isSuccess = this.ClassifyStatusCode(statusCode);

                var record = new RequestRecord(
                    SystemClock.UtcNow,
                    endpoint,
                    httpMethod,
                    statusCode,
                    durationMs,
                    bytesSent,
                    bytesReceived,
                    isSuccess,
                    string.Empty,
                    string.Empty);

                this._history.Add(record);
                this.TrimHistory();
                this.RecalculateTps();

                snapshot = this.BuildSnapshot();
            }

            this.RaiseMetricsUpdated(snapshot);
        }

        /// <summary>
        /// Called when a request fails with an exception (timeout, network error, circuit breaker).
        /// Records the failure, decrements outstanding count, and raises MetricsUpdated.
        /// </summary>
        /// <param name="endpoint">The relative endpoint path.</param>
        /// <param name="httpMethod">The HTTP method (GET, POST, etc.).</param>
        /// <param name="failureCategory">The failure category (Timeout, NetworkError, CircuitBreakerRejection).</param>
        /// <param name="message">The exception or error message.</param>
        public void OnRequestFailed(string endpoint, string httpMethod, string failureCategory, string message)
        {
            MetricsSnapshot snapshot;

            lock (this._lock)
            {
                this._outstandingRequests = Math.Max(0, this._outstandingRequests - 1);
                this._totalRequests++;
                this._exceptionCount++;

                var record = new RequestRecord(
                    SystemClock.UtcNow,
                    endpoint,
                    httpMethod,
                    0,
                    0,
                    0,
                    0,
                    false,
                    failureCategory,
                    message);

                this._history.Add(record);
                this.TrimHistory();
                this.RecalculateTps();

                snapshot = this.BuildSnapshot();
            }

            this.RaiseMetricsUpdated(snapshot);
        }

        /// <summary>
        /// Resets all cumulative counters to zero, clears history, sets peak to current
        /// outstanding, and records the reset timestamp.
        /// </summary>
        public void ResetCounters()
        {
            MetricsSnapshot snapshot;

            lock (this._lock)
            {
                this._totalRequests = 0;
                this._successCount = 0;
                this._clientErrorCount = 0;
                this._serverErrorCount = 0;
                this._exceptionCount = 0;
                this._rateLimitedCount = 0;
                this._bytesSent = 0;
                this._bytesReceived = 0;
                this._peakOutstanding = this._outstandingRequests;
                this._history.Clear();
                this._currentTps = 0;

                for (int i = 0; i < this._tpsSamples.Length; i++)
                {
                    this._tpsSamples[i] = 0;
                }

                this._tpsSampleIndex = 0;
                this._resetTimestamp = SystemClock.UtcNow;

                snapshot = this.BuildSnapshot();
            }

            this.RaiseMetricsUpdated(snapshot);
        }

        /// <summary>
        /// Returns a snapshot of all current metrics values.
        /// </summary>
        /// <returns>An immutable <see cref="MetricsSnapshot"/> with current counter values.</returns>
        public MetricsSnapshot GetCurrentSnapshot()
        {
            lock (this._lock)
            {
                return this.BuildSnapshot();
            }
        }

        /// <summary>
        /// Returns a copy of the request history list.
        /// </summary>
        /// <returns>A read-only list of <see cref="RequestRecord"/> entries.</returns>
        public IReadOnlyList<RequestRecord> GetHistory()
        {
            lock (this._lock)
            {
                return this._history.ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Returns a copy of the 300-sample TPS array ordered oldest-to-newest.
        /// </summary>
        /// <returns>A decimal array of 300 TPS samples.</returns>
        public decimal[] GetTpsSamples()
        {
            lock (this._lock)
            {
                var result = new decimal[300];
                for (int i = 0; i < 300; i++)
                {
                    result[i] = this._tpsSamples[(this._tpsSampleIndex + i) % 300];
                }

                return result;
            }
        }

        /// <summary>
        /// Classifies the HTTP status code and increments the appropriate counter.
        /// Returns true if the status code indicates success.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to classify.</param>
        /// <returns>True if the request is classified as successful; otherwise false.</returns>
        private bool ClassifyStatusCode(int statusCode)
        {
            if (statusCode >= 200 && statusCode < 400)
            {
                this._successCount++;
                return true;
            }

            if (statusCode >= 400 && statusCode < 500)
            {
                this._clientErrorCount++;

                if (statusCode == 429)
                {
                    this._rateLimitedCount++;
                }

                return false;
            }

            if (statusCode >= 500)
            {
                this._serverErrorCount++;
                return false;
            }

            // Status codes below 200 (e.g. 1xx informational) — classify as success
            this._successCount++;
            return true;
        }

        /// <summary>
        /// Trims the history list to the maximum allowed size by removing the oldest entries.
        /// </summary>
        private void TrimHistory()
        {
            while (this._history.Count > MaxHistorySize)
            {
                this._history.RemoveAt(0);
            }
        }

        /// <summary>
        /// Timer callback that samples the current TPS value into the circular buffer every 1 second.
        /// </summary>
        /// <param name="state">Unused timer state.</param>
        private void SampleTimerCallback(object state)
        {
            lock (this._lock)
            {
                this.RecalculateTps();
                this._tpsSamples[this._tpsSampleIndex] = this._currentTps;
                this._tpsSampleIndex = (this._tpsSampleIndex + 1) % 300;
            }
        }

        /// <summary>
        /// Recalculates the current TPS value based on requests completed in the last 60 seconds.
        /// Must be called within the lock.
        /// </summary>
        private void RecalculateTps()
        {
            DateTime cutoff = SystemClock.UtcNow.AddSeconds(-60);
            int count = 0;

            for (int i = 0; i < this._history.Count; i++)
            {
                if (this._history[i].Timestamp >= cutoff)
                {
                    count++;
                }
            }

            this._currentTps = count / 60m;
        }

        /// <summary>
        /// Builds a metrics snapshot from the current state. Must be called within the lock.
        /// </summary>
        /// <returns>A new <see cref="MetricsSnapshot"/> instance.</returns>
        private MetricsSnapshot BuildSnapshot()
        {
            return new MetricsSnapshot(
                this._currentTps,
                this._totalRequests,
                this._successCount,
                this._clientErrorCount,
                this._serverErrorCount,
                this._exceptionCount,
                this._rateLimitedCount,
                this._bytesSent,
                this._bytesReceived,
                this._outstandingRequests,
                this._peakOutstanding,
                this._resetTimestamp);
        }

        /// <summary>
        /// Raises the MetricsUpdated event outside the lock. Catches and logs handler exceptions.
        /// </summary>
        /// <param name="snapshot">The metrics snapshot to pass to subscribers.</param>
        private void RaiseMetricsUpdated(MetricsSnapshot snapshot)
        {
            var handler = this.MetricsUpdated;
            if (handler == null)
            {
                return;
            }

            try
            {
                handler(this, snapshot);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception in MetricsUpdated event handler.");
            }
        }
    }
}
