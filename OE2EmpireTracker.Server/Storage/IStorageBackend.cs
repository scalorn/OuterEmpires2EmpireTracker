// -----------------------------------------------------------------------
// <copyright file="IStorageBackend.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Server.Storage;

/// <summary>
/// Abstraction for server-side data persistence.
/// All backends implement this interface so the service is storage-agnostic.
/// This interface will be unified with the Common interface in Task 3.3.
/// </summary>
public interface IStorageBackend : OE2EmpireTracker.Common.Interfaces.IStorageBackend
{
}
