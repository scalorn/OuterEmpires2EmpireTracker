// <copyright file="GameApiConnectionSettings.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// Settings for the game API connection (stored in preferences under GameApiConnection).
    /// </summary>
    public class GameApiConnectionSettings
    {
        /// <summary>
        /// Gets or sets the game API server URL (e.g. https://api.outerempires2.com).
        /// </summary>
        public string ServerUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the polling interval in minutes for sync operations.
        /// </summary>
        public int PollingIntervalMinutes { get; set; } = 5;

        /// <summary>
        /// Gets or sets a value indicating whether the game API integration is enabled.
        /// </summary>
        public bool Enabled { get; set; } = false;
    }
}
