// -----------------------------------------------------------------------
// <copyright file="StationEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed CRUD endpoint handler for Station entities.
/// </summary>
public class StationEndpoints : TypedEndpointBase<Station, StationCreateRequest, StationUpdateRequest>
{
    /// <inheritdoc/>
    protected override string EntityTypeName => "Station";

    /// <inheritdoc/>
    protected override string RoutePrefix => "stations";

    /// <inheritdoc/>
    protected override string? ValidateCreate(StationCreateRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name is required";
        }

        return null;
    }

    /// <inheritdoc/>
    protected override string? ValidateUpdate(StationUpdateRequest dto) => null;

    /// <inheritdoc/>
    protected override Station ApplyCreate(StationCreateRequest dto)
    {
        return new Station
        {
            UUID = Guid.NewGuid().ToString(),
            Name = dto.Name,
        };
    }

    /// <inheritdoc/>
    protected override Station ApplyUpdate(Station existing, StationUpdateRequest dto)
    {
        if (dto.Name != null)
        {
            existing.Name = dto.Name;
        }

        existing.StationType = dto.StationType;
        existing.Ownership = dto.Ownership;

        if (dto.StationBlueprintUUID != null)
        {
            existing.StationBlueprintUUID = dto.StationBlueprintUUID;
        }

        existing.HullCurrentHP = dto.HullCurrentHP;
        existing.HullMaxHP = dto.HullMaxHP;
        existing.HullMaxRepairPercent = dto.HullMaxRepairPercent;

        if (dto.Components != null)
        {
            existing.Components = dto.Components;
        }

        if (dto.Hold != null)
        {
            existing.Holds["main"] = dto.Hold;
        }

        if (dto.MunitionsHold != null)
        {
            existing.MunitionsHold = dto.MunitionsHold;
        }

        return existing;
    }

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<Station>> GetAllFromStorage(string characterUUID, IStorageBackend storage)
        => storage.GetAllStationsAsync(characterUUID);

    /// <inheritdoc/>
    protected override Task<Station?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.GetStationAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override Task UpsertToStorage(string characterUUID, Station entity, IStorageBackend storage)
        => storage.UpsertStationAsync(characterUUID, entity);

    /// <inheritdoc/>
    protected override Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.DeleteStationAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override string GetEntityUuid(Station entity) => entity.UUID;
}

/// <summary>
/// Extension methods for registering Station endpoints.
/// </summary>
public static class StationEndpointsExtensions
{
    /// <summary>
    /// Maps the Station CRUD endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapStationEndpoints(this WebApplication app)
    {
        var endpoints = new StationEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/stations")
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