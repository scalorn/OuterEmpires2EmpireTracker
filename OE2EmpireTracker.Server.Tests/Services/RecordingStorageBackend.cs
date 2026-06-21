// -----------------------------------------------------------------------
// <copyright file="RecordingStorageBackend.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;
using OE2EmpireTracker.Server.Tests.Endpoints;
using ServerCharacter = OE2EmpireTracker.Common.Models.ServerCharacter;
using ServerFaction = OE2EmpireTracker.Common.Models.ServerFaction;

namespace OE2EmpireTracker.Server.Tests.Services;

/// <summary>
/// Recording stub that captures UpsertGlobalDataAsync and UpsertBlueprintAsync calls.
/// Inherits from <see cref="StubStorageBackend"/> and explicitly re-implements
/// the storage methods used by <see cref="OE2EmpireTracker.Server.Services.BaselineDecompositionService"/>.
/// </summary>
internal class RecordingStorageBackend : StubStorageBackend, IStorageBackend
{
    /// <summary>
    /// Gets the list of recorded UpsertGlobalDataAsync calls.
    /// </summary>
    public List<(string Key, string Json)> GlobalDataCalls { get; } = new();

    /// <summary>
    /// Gets the list of recorded UpsertBlueprintAsync calls.
    /// </summary>
    public List<(string CharUUID, Blueprint Blueprint)> BlueprintCalls { get; } = new();

    /// <inheritdoc/>
    Task IStorageBackend.UpsertGlobalDataAsync(string dataType, string json)
    {
        GlobalDataCalls.Add((dataType, json));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    Task IStorageBackend.UpsertBlueprintAsync(
        string characterUUID,
        Blueprint entity)
    {
        BlueprintCalls.Add((characterUUID, entity));
        return Task.CompletedTask;
    }
}
