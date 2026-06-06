// <copyright file="GameApiConnectionSettings.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// Settings for the game API connection (stored in preferences under GameApiConnection).
    /// The OE2 public API uses OAuth2 client_credentials flow:
    /// app_id (registered app GUID) + client_id (player account) + secret (per-character) → JWT.
    /// </summary>
    public class GameApiConnectionSettings
    {
        /// <summary>
        /// Gets or sets the game API server URL (e.g. https://oe2-pub-api-dev.azure-api.net).
        /// </summary>
        public string ServerUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the registered third-party application GUID.
        /// Used in token exchange and as X-App-Id header on data requests.
        /// </summary>
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the player's account identifier (client_id).
        /// Shared across all characters on the same account.
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the polling interval in minutes for sync operations.
        /// </summary>
        public int PollingIntervalMinutes { get; set; } = 5;

        /// <summary>
        /// Gets or sets a value indicating whether the game API integration is enabled.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the target transactions per second (TPS) for the API rate governor.
        /// Valid range: 0.1 to 100.0. Default 0.5 TPS.
        /// </summary>
        public double Tps { get; set; } = 0.5;
    }
}
