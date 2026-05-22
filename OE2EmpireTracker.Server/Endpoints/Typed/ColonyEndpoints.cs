// -----------------------------------------------------------------------
// <copyright file="ColonyEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed CRUD endpoint handler for Colony entities.
/// Includes deduplication logic for PlanetName+SystemName matching.
/// </summary>
public class ColonyEndpoints : TypedEndpointBase<Colony, ColonyCreateRequest, ColonyUpdateRequest>
{
    /// <inheritdoc/>
    protected override string EntityTypeName => "Colony";

    /// <inheritdoc/>
    protected override string RoutePrefix => "colonies";

    /// <inheritdoc/>
    protected override string? ValidateCreate(ColonyCreateRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PlanetName))
        {
            return "PlanetName is required";
        }

        if (string.IsNullOrWhiteSpace(dto.ColonyName))
        {
            return "ColonyName is required";
        }

        return null;
    }

    /// <inheritdoc/>
    protected override string? ValidateUpdate(ColonyUpdateRequest dto) => null;

    /// <inheritdoc/>
    protected override Colony ApplyCreate(ColonyCreateRequest dto)
    {
        return new Colony
        {
            UUID = Guid.NewGuid().ToString(),
            PlanetName = dto.PlanetName,
            ColonyName = dto.ColonyName,
            SystemName = dto.SystemName ?? string.Empty,
        };
    }

    /// <inheritdoc/>
    protected override Colony ApplyUpdate(Colony existing, ColonyUpdateRequest dto)
    {
        if (dto.PlanetName != null)
        {
            existing.PlanetName = dto.PlanetName;
        }

        if (dto.ColonyName != null)
        {
            existing.ColonyName = dto.ColonyName;
        }

        if (dto.SystemName != null)
        {
            existing.SystemName = dto.SystemName;
        }

        return existing;
    }

    /// <summary>
    /// Checks for an existing colony with the same PlanetName and SystemName
    /// (case-insensitive). If found, merges the new data into the existing
    /// colony and returns 200 OK with the merged entity.
    /// </summary>
    /// <param name="characterUUID">The owning character's UUID.</param>
    /// <param name="dto">The create request DTO.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>
    /// An <see cref="IResult"/> with the merged colony (200) if a duplicate exists,
    /// or null if no duplicate and creation should proceed normally.
    /// </returns>
    protected override async Task<IResult?> HandleCreateDedup(
        string characterUUID, ColonyCreateRequest dto, IStorageBackend storage)
    {
        var allColonies = await GetAllFromStorage(characterUUID, storage);

        var existing = allColonies.FirstOrDefault(c =>
            string.Equals(c.PlanetName, dto.PlanetName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.SystemName, dto.SystemName ?? string.Empty, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
            return null;
        }

        // Merge: update ColonyName if provided
        if (!string.IsNullOrWhiteSpace(dto.ColonyName))
        {
            existing.ColonyName = dto.ColonyName;
        }

        await UpsertToStorage(characterUUID, existing, storage);
        return Results.Ok(existing);
    }

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<Colony>> GetAllFromStorage(string characterUUID, IStorageBackend storage)
        => storage.GetAllColoniesAsync(characterUUID);

    /// <inheritdoc/>
    protected override Task<Colony?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.GetColonyAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override Task UpsertToStorage(string characterUUID, Colony entity, IStorageBackend storage)
        => storage.UpsertColonyAsync(characterUUID, entity);

    /// <inheritdoc/>
    protected override Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.DeleteColonyAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override string GetEntityUuid(Colony entity) => entity.UUID;
}

/// <summary>
/// Extension methods for registering Colony endpoints.
/// </summary>
public static class ColonyEndpointsExtensions
{
    /// <summary>
    /// Maps the Colony CRUD endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapColonyEndpoints(this WebApplication app)
    {
        var endpoints = new ColonyEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/colonies")
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
