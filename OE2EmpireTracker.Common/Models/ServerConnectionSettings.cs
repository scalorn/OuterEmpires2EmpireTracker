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
        /// Gets or sets the DPAPI-protected bearer token (base64 blob).
        /// Never stores plaintext — use CredentialStore.Protect to set
        /// and CredentialStore.Unprotect to read.
        /// </summary>
        public string ProtectedBearerToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the trusted certificate thumbprint for self-signed cert pinning.
        /// </summary>
        public string TrustedThumbprint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the operating mode for data access.
        /// </summary>
        public OperatingMode Mode { get; set; } = OperatingMode.LocalOnly;
    }
}
