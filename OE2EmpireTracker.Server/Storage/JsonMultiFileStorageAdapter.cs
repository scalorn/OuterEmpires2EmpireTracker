// -----------------------------------------------------------------------
// <copyright file="JsonMultiFileStorageAdapter.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using OE2EmpireTracker.Common.Storage;

namespace OE2EmpireTracker.Server.Storage;

/// <summary>
/// Thin adapter that makes <see cref="JsonMultiFileBackend"/> satisfy
/// the Server-side <see cref="IStorageBackend"/> interface.
/// This adapter will be removed when the Server interface is deleted (Task 3.3).
/// </summary>
public class JsonMultiFileStorageAdapter : JsonMultiFileBackend, IStorageBackend
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonMultiFileStorageAdapter"/> class.
    /// </summary>
    /// <param name="dataPath">Root directory for JSON file storage.</param>
    public JsonMultiFileStorageAdapter(string dataPath)
        : base(dataPath)
    {
    }
}
