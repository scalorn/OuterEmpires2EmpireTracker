// <copyright file="CircuitBreakerState.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Exposes circuit breaker state from the Game API client for display in the status form.
    /// </summary>
    public class CircuitBreakerState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerState"/> class.
        /// </summary>
        /// <param name="state">The circuit breaker state: "Closed", "Open", or "Half-Open".</param>
        /// <param name="openedAt">The UTC time when the circuit breaker opened, or null if not open.</param>
        /// <param name="breakDuration">The configured break duration, or null if not open.</param>
        public CircuitBreakerState(string state, DateTime? openedAt, TimeSpan? breakDuration)
        {
            this.State = state ?? string.Empty;
            this.OpenedAt = openedAt;
            this.BreakDuration = breakDuration;
        }

        /// <summary>
        /// Gets the circuit breaker state: "Closed", "Open", or "Half-Open".
        /// </summary>
        public string State { get; }

        /// <summary>
        /// Gets the UTC time when the circuit breaker opened, or null if not open.
        /// </summary>
        public DateTime? OpenedAt { get; }

        /// <summary>
        /// Gets the configured break duration, or null if not open.
        /// </summary>
        public TimeSpan? BreakDuration { get; }

        /// <summary>
        /// Gets the time remaining until the circuit breaker transitions to half-open,
        /// or null if the breaker is not in the Open state or the break period has elapsed.
        /// </summary>
        public TimeSpan? TimeUntilHalfOpen
        {
            get
            {
                if (this.State != "Open" || this.OpenedAt == null || this.BreakDuration == null)
                {
                    return null;
                }

                var remaining = (this.OpenedAt.Value + this.BreakDuration.Value) - SystemClock.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : null;
            }
        }
    }
}
