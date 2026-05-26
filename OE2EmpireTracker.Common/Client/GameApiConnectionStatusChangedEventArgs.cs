// <copyright file="GameApiConnectionStatusChangedEventArgs.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// Event arguments raised when the game API connection state changes.
    /// </summary>
    public class GameApiConnectionStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the previous connection state.
        /// </summary>
        public GameApiConnectionMonitor.ConnectionState OldState { get; set; }

        /// <summary>
        /// Gets or sets the new connection state.
        /// </summary>
        public GameApiConnectionMonitor.ConnectionState NewState { get; set; }

        /// <summary>
        /// Gets or sets a descriptive message about the state transition.
        /// </summary>
        public string Message { get; set; }
    }
}
