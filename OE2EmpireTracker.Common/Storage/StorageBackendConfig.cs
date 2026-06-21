// -----------------------------------------------------------------------
// <copyright file="StorageBackendConfig.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OE2EmpireTracker.Common.Storage
{
    /// <summary>
    /// Configuration parameters for creating a storage backend instance.
    /// </summary>
    public class StorageBackendConfig
    {
        /// <summary>Gets or sets the connection string (file path for JSON/SQLite, connection string for Postgres).</summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>Gets or sets the AWS region (DynamoDB only).</summary>
        public string AwsRegion { get; set; } = string.Empty;

        /// <summary>Gets or sets the table name prefix (DynamoDB only).</summary>
        public string TablePrefix { get; set; } = string.Empty;
    }
}
