// <copyright file="OperatingMode.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// Defines the operating mode for data access (local, server, or both).
    /// </summary>
    public enum OperatingMode
    {
        /// <summary>
        /// No server connection; all reads/writes are local only.
        /// </summary>
        LocalOnly,

        /// <summary>
        /// All reads/writes go to the remote server.
        /// </summary>
        ServerOnly,

        /// <summary>
        /// Dual-write mode: server is primary, local is backup.
        /// </summary>
        ServerAndLocal,
    }
}
