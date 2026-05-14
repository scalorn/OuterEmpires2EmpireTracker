// <copyright file="RealtimeModeChangedEventArgs.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// Event arguments for real-time mode changes (WebSocket vs polling).
    /// </summary>
    public class RealtimeModeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating whether the client is in real-time mode (WebSocket).
        /// When false, the client is in polling mode.
        /// </summary>
        public bool IsRealtime { get; set; }
    }
}
