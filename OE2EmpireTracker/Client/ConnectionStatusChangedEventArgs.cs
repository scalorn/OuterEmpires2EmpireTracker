// <copyright file="ConnectionStatusChangedEventArgs.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// Event arguments for connection status changes.
    /// </summary>
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating whether the client is connected.
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Gets or sets a human-readable status message.
        /// </summary>
        public string Message { get; set; }
    }
}
