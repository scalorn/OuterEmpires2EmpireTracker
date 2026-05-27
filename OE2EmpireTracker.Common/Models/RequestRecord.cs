// <copyright file="RequestRecord.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Immutable data class representing one completed or failed API request.
    /// Stores timing, size, status, and outcome data for metrics collection.
    /// </summary>
    public class RequestRecord
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestRecord"/> class.
        /// </summary>
        /// <param name="timestamp">The UTC time when the request was initiated.</param>
        /// <param name="endpoint">The relative endpoint path of the request.</param>
        /// <param name="httpMethod">The HTTP method used (GET, POST, etc.).</param>
        /// <param name="statusCode">The HTTP status code received (0 if request failed before response).</param>
        /// <param name="durationMs">The request duration in milliseconds.</param>
        /// <param name="bytesSent">The number of bytes sent in the request body.</param>
        /// <param name="bytesReceived">The number of bytes received in the response body.</param>
        /// <param name="isSuccess">Whether the request was classified as successful.</param>
        /// <param name="failureCategory">The failure category (Timeout, NetworkError, CircuitBreakerRejection) or empty if successful.</param>
        /// <param name="errorMessage">The error message if the request failed, or empty if successful.</param>
        public RequestRecord(
            DateTime timestamp,
            string endpoint,
            string httpMethod,
            int statusCode,
            long durationMs,
            long bytesSent,
            long bytesReceived,
            bool isSuccess,
            string failureCategory,
            string errorMessage)
        {
            this.Timestamp = timestamp;
            this.Endpoint = endpoint ?? string.Empty;
            this.HttpMethod = httpMethod ?? string.Empty;
            this.StatusCode = statusCode;
            this.DurationMs = durationMs;
            this.BytesSent = bytesSent;
            this.BytesReceived = bytesReceived;
            this.IsSuccess = isSuccess;
            this.FailureCategory = failureCategory ?? string.Empty;
            this.ErrorMessage = errorMessage ?? string.Empty;
        }

        /// <summary>
        /// Gets the UTC time when the request was initiated.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets the relative endpoint path of the request.
        /// </summary>
        public string Endpoint { get; }

        /// <summary>
        /// Gets the HTTP method used (GET, POST, etc.).
        /// </summary>
        public string HttpMethod { get; }

        /// <summary>
        /// Gets the HTTP status code received (0 if request failed before response).
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Gets the request duration in milliseconds.
        /// </summary>
        public long DurationMs { get; }

        /// <summary>
        /// Gets the number of bytes sent in the request body.
        /// </summary>
        public long BytesSent { get; }

        /// <summary>
        /// Gets the number of bytes received in the response body.
        /// </summary>
        public long BytesReceived { get; }

        /// <summary>
        /// Gets a value indicating whether the request was classified as successful.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets the failure category (Timeout, NetworkError, CircuitBreakerRejection) or empty if successful.
        /// </summary>
        public string FailureCategory { get; }

        /// <summary>
        /// Gets the error message if the request failed, or empty if successful.
        /// </summary>
        public string ErrorMessage { get; }
    }
}
