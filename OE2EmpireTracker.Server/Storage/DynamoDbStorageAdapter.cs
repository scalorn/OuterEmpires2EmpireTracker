// -----------------------------------------------------------------------
// <copyright file="DynamoDbStorageAdapter.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using OE2EmpireTracker.Common.Storage;

namespace OE2EmpireTracker.Server.Storage;

/// <summary>
/// Thin adapter that makes <see cref="DynamoDbBackend"/> satisfy
/// the Server-side <see cref="IStorageBackend"/> interface.
/// This adapter will be removed when the Server interface is deleted (Task 3.3).
/// </summary>
public class DynamoDbStorageAdapter : DynamoDbBackend, IStorageBackend
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DynamoDbStorageAdapter"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    public DynamoDbStorageAdapter(IConfiguration configuration)
        : base(
            configuration["Storage:DynamoTableName"] ?? "OE2EmpireTracker",
            configuration["Storage:DynamoRegion"] ?? "us-east-1")
    {
    }
}
