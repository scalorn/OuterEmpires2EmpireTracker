// <copyright file="RateLimitChangedEventArgs.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// Event arguments for rate limit changes received via WebSocket.
    /// </summary>
    public class RateLimitChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the new server-communicated requests-per-minute limit.
        /// </summary>
        public int RequestsPerMinute { get; set; }
    }
}
