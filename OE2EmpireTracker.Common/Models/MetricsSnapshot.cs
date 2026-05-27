// <copyright file="MetricsSnapshot.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Immutable snapshot of all API metrics at a point in time.
    /// Passed with the MetricsUpdated event to provide current state to subscribers.
    /// </summary>
    public class MetricsSnapshot
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsSnapshot"/> class.
        /// </summary>
        /// <param name="currentTps">The current transactions per second rate.</param>
        /// <param name="totalRequests">The total number of requests sent.</param>
        /// <param name="successCount">The count of successful responses (HTTP 2xx and 3xx).</param>
        /// <param name="clientErrorCount">The count of client error responses (HTTP 4xx).</param>
        /// <param name="serverErrorCount">The count of server error responses (HTTP 5xx).</param>
        /// <param name="exceptionCount">The count of requests that failed with exceptions.</param>
        /// <param name="rateLimitedCount">The count of HTTP 429 rate-limited responses.</param>
        /// <param name="bytesSent">The cumulative bytes sent in request bodies.</param>
        /// <param name="bytesReceived">The cumulative bytes received in response bodies.</param>
        /// <param name="outstandingRequests">The current number of in-flight requests.</param>
        /// <param name="peakOutstanding">The peak number of in-flight requests since last reset.</param>
        /// <param name="resetTimestamp">The timestamp of the last counter reset, or null if never reset.</param>
        public MetricsSnapshot(
            decimal currentTps,
            long totalRequests,
            long successCount,
            long clientErrorCount,
            long serverErrorCount,
            long exceptionCount,
            long rateLimitedCount,
            long bytesSent,
            long bytesReceived,
            int outstandingRequests,
            int peakOutstanding,
            DateTime? resetTimestamp)
        {
            this.CurrentTps = currentTps;
            this.TotalRequests = totalRequests;
            this.SuccessCount = successCount;
            this.ClientErrorCount = clientErrorCount;
            this.ServerErrorCount = serverErrorCount;
            this.ExceptionCount = exceptionCount;
            this.RateLimitedCount = rateLimitedCount;
            this.BytesSent = bytesSent;
            this.BytesReceived = bytesReceived;
            this.OutstandingRequests = outstandingRequests;
            this.PeakOutstanding = peakOutstanding;
            this.ResetTimestamp = resetTimestamp;
        }

        /// <summary>
        /// Gets the current transactions per second rate.
        /// </summary>
        public decimal CurrentTps { get; }

        /// <summary>
        /// Gets the total number of requests sent.
        /// </summary>
        public long TotalRequests { get; }

        /// <summary>
        /// Gets the count of successful responses (HTTP 2xx and 3xx).
        /// </summary>
        public long SuccessCount { get; }

        /// <summary>
        /// Gets the count of client error responses (HTTP 4xx).
        /// </summary>
        public long ClientErrorCount { get; }

        /// <summary>
        /// Gets the count of server error responses (HTTP 5xx).
        /// </summary>
        public long ServerErrorCount { get; }

        /// <summary>
        /// Gets the count of requests that failed with exceptions.
        /// </summary>
        public long ExceptionCount { get; }

        /// <summary>
        /// Gets the count of HTTP 429 rate-limited responses.
        /// </summary>
        public long RateLimitedCount { get; }

        /// <summary>
        /// Gets the cumulative bytes sent in request bodies.
        /// </summary>
        public long BytesSent { get; }

        /// <summary>
        /// Gets the cumulative bytes received in response bodies.
        /// </summary>
        public long BytesReceived { get; }

        /// <summary>
        /// Gets the current number of in-flight requests.
        /// </summary>
        public int OutstandingRequests { get; }

        /// <summary>
        /// Gets the peak number of in-flight requests since last reset.
        /// </summary>
        public int PeakOutstanding { get; }

        /// <summary>
        /// Gets the timestamp of the last counter reset, or null if never reset.
        /// </summary>
        public DateTime? ResetTimestamp { get; }
    }
}
