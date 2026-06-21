// -----------------------------------------------------------------------
// <copyright file="AsteroidEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed CRUD endpoint handler for Asteroid entities.
/// </summary>
public class AsteroidEndpoints : TypedEndpointBase<Asteroid, AsteroidCreateRequest, AsteroidUpdateRequest>
{
    /// <inheritdoc/>
    protected override string EntityTypeName => "Asteroid";

    /// <inheritdoc/>
    protected override string RoutePrefix => "asteroids";

    /// <inheritdoc/>
    protected override string? ValidateCreate(AsteroidCreateRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name is required";
        }

        return null;
    }

    /// <inheritdoc/>
    protected override string? ValidateUpdate(AsteroidUpdateRequest dto) => null;

    /// <inheritdoc/>
    protected override Asteroid ApplyCreate(AsteroidCreateRequest dto)
    {
        return new Asteroid
        {
            UUID = Guid.NewGuid().ToString(),
            Name = dto.Name,
            SystemName = dto.SystemName ?? string.Empty,
            Reserves = dto.Reserves ?? new List<AsteroidReserve>(),
        };
    }

    /// <inheritdoc/>
    protected override Asteroid ApplyUpdate(Asteroid existing, AsteroidUpdateRequest dto)
    {
        if (dto.Name != null)
        {
            existing.Name = dto.Name;
        }

        if (dto.SystemName != null)
        {
            existing.SystemName = dto.SystemName;
        }

        if (dto.Reserves != null)
        {
            existing.Reserves = dto.Reserves;
        }

        return existing;
    }

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<Asteroid>> GetAllFromStorage(string characterUUID, IStorageBackend storage)
        => storage.GetAllAsteroidsAsync(characterUUID);

    /// <inheritdoc/>
    protected override Task<Asteroid?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.GetAsteroidAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override Task UpsertToStorage(string characterUUID, Asteroid entity, IStorageBackend storage)
        => storage.UpsertAsteroidAsync(characterUUID, entity);

    /// <inheritdoc/>
    protected override Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.DeleteAsteroidAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override string GetEntityUuid(Asteroid entity) => entity.UUID;
}

/// <summary>
/// Extension methods for registering Asteroid endpoints.
/// </summary>
public static class AsteroidEndpointsExtensions
{
    /// <summary>
    /// Maps the Asteroid CRUD endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapAsteroidEndpoints(this WebApplication app)
    {
        var endpoints = new AsteroidEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/asteroids")
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
