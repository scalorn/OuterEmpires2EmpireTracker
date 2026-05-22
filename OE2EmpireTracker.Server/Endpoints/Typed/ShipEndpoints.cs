// -----------------------------------------------------------------------
// <copyright file="ShipEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed CRUD endpoint handler for Ship entities.
/// </summary>
public class ShipEndpoints : TypedEndpointBase<Ship, ShipCreateRequest, ShipUpdateRequest>
{
    /// <inheritdoc/>
    protected override string EntityTypeName => "Ship";

    /// <inheritdoc/>
    protected override string RoutePrefix => "ships";

    /// <inheritdoc/>
    protected override string? ValidateCreate(ShipCreateRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name is required";
        }

        return null;
    }

    /// <inheritdoc/>
    protected override string? ValidateUpdate(ShipUpdateRequest dto) => null;

    /// <inheritdoc/>
    protected override Ship ApplyCreate(ShipCreateRequest dto)
    {
        return new Ship
        {
            UUID = Guid.NewGuid().ToString(),
            Name = dto.Name,
        };
    }

    /// <inheritdoc/>
    protected override Ship ApplyUpdate(Ship existing, ShipUpdateRequest dto)
    {
        if (dto.Name != null)
        {
            existing.Name = dto.Name;
        }

        if (dto.TemplateUUID != null)
        {
            existing.TemplateUUID = dto.TemplateUUID;
        }

        if (dto.HullBlueprintUUID != null)
        {
            existing.HullBlueprintUUID = dto.HullBlueprintUUID;
        }

        if (dto.LocationUUID != null)
        {
            existing.LocationUUID = dto.LocationUUID;
        }

        existing.LocationType = dto.LocationType;
        existing.HullCurrentHP = dto.HullCurrentHP;
        existing.HullMaxHP = dto.HullMaxHP;
        existing.HullMaxRepairPercent = dto.HullMaxRepairPercent;

        if (dto.Components != null)
        {
            existing.Components = dto.Components;
        }

        if (dto.Cargo != null)
        {
            existing.Cargo = dto.Cargo;
        }

        if (dto.Hopper != null)
        {
            existing.Hopper = dto.Hopper;
        }

        return existing;
    }

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<Ship>> GetAllFromStorage(string characterUUID, IStorageBackend storage)
        => storage.GetAllShipsAsync(characterUUID);

    /// <inheritdoc/>
    protected override Task<Ship?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.GetShipAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override Task UpsertToStorage(string characterUUID, Ship entity, IStorageBackend storage)
        => storage.UpsertShipAsync(characterUUID, entity);

    /// <inheritdoc/>
    protected override Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.DeleteShipAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override string GetEntityUuid(Ship entity) => entity.UUID;
}

/// <summary>
/// Extension methods for registering Ship endpoints.
/// </summary>
public static class ShipEndpointsExtensions
{
    /// <summary>
    /// Maps the Ship CRUD endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapShipEndpoints(this WebApplication app)
    {
        var endpoints = new ShipEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/ships")
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
