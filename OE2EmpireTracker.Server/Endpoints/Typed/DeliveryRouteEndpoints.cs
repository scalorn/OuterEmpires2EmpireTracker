// -----------------------------------------------------------------------
// <copyright file="DeliveryRouteEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed CRUD endpoint handler for DeliveryRoute entities.
/// </summary>
public class DeliveryRouteEndpoints : TypedEndpointBase<DeliveryRoute, DeliveryRouteCreateRequest, DeliveryRouteUpdateRequest>
{
    /// <inheritdoc/>
    protected override string EntityTypeName => "DeliveryRoute";

    /// <inheritdoc/>
    protected override string RoutePrefix => "delivery-routes";

    /// <inheritdoc/>
    protected override string? ValidateCreate(DeliveryRouteCreateRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name is required";
        }

        return null;
    }

    /// <inheritdoc/>
    protected override string? ValidateUpdate(DeliveryRouteUpdateRequest dto) => null;

    /// <inheritdoc/>
    protected override DeliveryRoute ApplyCreate(DeliveryRouteCreateRequest dto)
    {
        var stops = DeepCopyAndRenumberStops(dto.Stops);

        return new DeliveryRoute
        {
            UUID = Guid.NewGuid().ToString(),
            Name = dto.Name,
            Stops = stops,
        };
    }

    /// <inheritdoc/>
    protected override DeliveryRoute ApplyUpdate(DeliveryRoute existing, DeliveryRouteUpdateRequest dto)
    {
        if (dto.Name != null)
        {
            existing.Name = dto.Name;
        }

        if (dto.Stops != null)
        {
            existing.Stops = DeepCopyAndRenumberStops(dto.Stops);
        }

        return existing;
    }

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<DeliveryRoute>> GetAllFromStorage(string characterUUID, IStorageBackend storage)
        => storage.GetAllDeliveryRoutesAsync(characterUUID);

    /// <inheritdoc/>
    protected override Task<DeliveryRoute?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.GetDeliveryRouteAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override Task UpsertToStorage(string characterUUID, DeliveryRoute entity, IStorageBackend storage)
        => storage.UpsertDeliveryRouteAsync(characterUUID, entity);

    /// <inheritdoc/>
    protected override Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.DeleteDeliveryRouteAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override string GetEntityUuid(DeliveryRoute entity) => entity.UUID;

    private static List<RouteStop> DeepCopyAndRenumberStops(List<RouteStop> stops)
    {
        if (stops == null)
        {
            return new List<RouteStop>();
        }

        return stops.Select((stop, index) => new RouteStop
        {
            ColonyUUID = stop.ColonyUUID,
            Sequence = index + 1,
            DestinationType = stop.DestinationType,
            DestinationUUID = stop.DestinationUUID,
            Purpose = stop.Purpose,
            FuelEstimate = stop.FuelEstimate,
        }).ToList();
    }
}

/// <summary>
/// Extension methods for registering DeliveryRoute endpoints.
/// </summary>
public static class DeliveryRouteEndpointsExtensions
{
    /// <summary>
    /// Maps the DeliveryRoute CRUD endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapDeliveryRouteEndpoints(this WebApplication app)
    {
        var endpoints = new DeliveryRouteEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/delivery-routes")
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
