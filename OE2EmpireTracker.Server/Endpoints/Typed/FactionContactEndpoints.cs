// -----------------------------------------------------------------------
// <copyright file="FactionContactEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed CRUD endpoint handler for Faction entities.
/// </summary>
public class FactionContactEndpoints : TypedEndpointBase<Faction, FactionCreateRequest, FactionUpdateRequest>
{
    /// <inheritdoc/>
    protected override string EntityTypeName => "Faction";

    /// <inheritdoc/>
    protected override string RoutePrefix => "factions";

    /// <inheritdoc/>
    protected override string? ValidateCreate(FactionCreateRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name is required";
        }

        return null;
    }

    /// <inheritdoc/>
    protected override string? ValidateUpdate(FactionUpdateRequest dto) => null;

    /// <inheritdoc/>
    protected override Faction ApplyCreate(FactionCreateRequest dto)
    {
        return new Faction
        {
            UUID = Guid.NewGuid().ToString(),
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
        };
    }

    /// <inheritdoc/>
    protected override Faction ApplyUpdate(Faction existing, FactionUpdateRequest dto)
    {
        if (dto.Name != null)
        {
            existing.Name = dto.Name;
        }

        if (dto.Description != null)
        {
            existing.Description = dto.Description;
        }

        return existing;
    }

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<Faction>> GetAllFromStorage(string characterUUID, IStorageBackend storage)
        => storage.GetAllFactionsForCharacterAsync(characterUUID);

    /// <inheritdoc/>
    protected override Task<Faction?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.GetFactionForCharacterAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override Task UpsertToStorage(string characterUUID, Faction entity, IStorageBackend storage)
        => storage.UpsertFactionForCharacterAsync(characterUUID, entity);

    /// <inheritdoc/>
    protected override Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.DeleteFactionForCharacterAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override string GetEntityUuid(Faction entity) => entity.UUID;
}

/// <summary>
/// Extension methods for registering Faction contact endpoints.
/// </summary>
public static class FactionContactEndpointsExtensions
{
    /// <summary>
    /// Maps the Faction CRUD endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapFactionContactEndpoints(this WebApplication app)
    {
        var endpoints = new FactionContactEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/factions")
            .RequireAuthorization("Authenticated");

        group.MapGet("/", (string uuid, int? limit, int? offset, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleGetAll(uuid, limit, offset, ctx, storage));
        group.MapGet("/{entityUuid}", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleGetOne(uuid, entityUuid, ctx, storage));
        group.MapPost("/", (string uuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleCreate(uuid, ctx, storage));
        group.MapPut("/{entityUuid}", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleUpdate(uuid, entityUuid, ctx, storage));
        group.MapDelete("/{entityUuid}", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleDelete(uuid, entityUuid, ctx, storage));
    }
}
