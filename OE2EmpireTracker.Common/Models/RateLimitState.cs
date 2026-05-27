// <copyright file="RateLimitState.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Exposes rate limiter state from the game API client for display in the status form.
    /// </summary>
    public class RateLimitState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimitState"/> class.
        /// </summary>
        /// <param name="configuredRequestsPerMinute">The configured rate limit in requests per minute.</param>
        /// <param name="serverRequestsPerMinute">The server-communicated rate limit in requests per minute, or null if not provided.</param>
        /// <param name="isPaused">Whether the rate limiter is currently paused due to an HTTP 429 response.</param>
        /// <param name="pauseUntil">The UTC time until which the rate limiter is paused, or null if not paused.</param>
        public RateLimitState(
            int configuredRequestsPerMinute,
            int? serverRequestsPerMinute,
            bool isPaused,
            DateTime? pauseUntil)
        {
            this.ConfiguredRequestsPerMinute = configuredRequestsPerMinute;
            this.ServerRequestsPerMinute = serverRequestsPerMinute;
            this.IsPaused = isPaused;
            this.PauseUntil = pauseUntil;
        }

        /// <summary>
        /// Gets the configured rate limit in requests per minute.
        /// </summary>
        public int ConfiguredRequestsPerMinute { get; }

        /// <summary>
        /// Gets the server-communicated rate limit in requests per minute, or null if not provided.
        /// </summary>
        public int? ServerRequestsPerMinute { get; }

        /// <summary>
        /// Gets a value indicating whether the rate limiter is currently paused due to an HTTP 429 response.
        /// </summary>
        public bool IsPaused { get; }

        /// <summary>
        /// Gets the UTC time until which the rate limiter is paused, or null if not paused.
        /// </summary>
        public DateTime? PauseUntil { get; }
    }
}
