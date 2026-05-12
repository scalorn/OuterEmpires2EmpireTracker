// <copyright file="ServerConnectionSettings.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// Settings for the remote faction server connection (stored in preferences).
    /// </summary>
    public class ServerConnectionSettings
    {
        /// <summary>
        /// Gets or sets the server URL (e.g. https://host:port).
        /// </summary>
        public string ServerUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the bearer token for API authentication.
        /// </summary>
        public string BearerToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the trusted certificate thumbprint for self-signed cert pinning.
        /// </summary>
        public string TrustedThumbprint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the operating mode for data access.
        /// </summary>
        public OperatingMode Mode { get; set; } = OperatingMode.LocalOnly;

        /// <summary>
        /// Gets or sets a value indicating whether dual-write (server + local) is enabled.
        /// </summary>
        public bool DualWriteEnabled { get; set; }
    }
}
