// <copyright file="GameApiSyncStatusChangedEventArgs.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Event arguments raised when the game API sync status changes.
    /// </summary>
    public class GameApiSyncStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating whether a sync operation is currently in progress.
        /// </summary>
        public bool IsSyncing { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the last sync operation succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if the last sync operation failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp of the last successful sync.
        /// </summary>
        public DateTime? LastSyncUtc { get; set; }
    }
}
