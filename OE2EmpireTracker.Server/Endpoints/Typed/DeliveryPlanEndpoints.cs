// -----------------------------------------------------------------------
// <copyright file="DeliveryPlanEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed CRUD endpoint handler for DeliveryPlan entities.
/// </summary>
public class DeliveryPlanEndpoints : TypedEndpointBase<DeliveryPlan, DeliveryPlanCreateRequest, DeliveryPlanUpdateRequest>
{
    /// <inheritdoc/>
    protected override string EntityTypeName => "DeliveryPlan";

    /// <inheritdoc/>
    protected override string RoutePrefix => "delivery-plans";

    /// <inheritdoc/>
    protected override string? ValidateCreate(DeliveryPlanCreateRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "name is required";
        }

        if (string.IsNullOrWhiteSpace(dto.RouteUUID))
        {
            return "routeUUID is required";
        }

        return null;
    }

    /// <inheritdoc/>
    protected override string? ValidateUpdate(DeliveryPlanUpdateRequest dto) => null;

    /// <inheritdoc/>
    protected override DeliveryPlan ApplyCreate(DeliveryPlanCreateRequest dto)
    {
        return new DeliveryPlan
        {
            UUID = Guid.NewGuid().ToString(),
            Name = dto.Name,
            RouteUUID = dto.RouteUUID,
        };
    }

    /// <inheritdoc/>
    protected override DeliveryPlan ApplyUpdate(DeliveryPlan existing, DeliveryPlanUpdateRequest dto)
    {
        if (dto.Name != null)
        {
            existing.Name = dto.Name;
        }

        if (dto.Stops != null)
        {
            existing.Stops = dto.Stops;
        }

        return existing;
    }

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<DeliveryPlan>> GetAllFromStorage(string characterUUID, IStorageBackend storage)
        => storage.GetAllDeliveryPlansAsync(characterUUID);

    /// <inheritdoc/>
    protected override Task<DeliveryPlan?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.GetDeliveryPlanAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override Task UpsertToStorage(string characterUUID, DeliveryPlan entity, IStorageBackend storage)
        => storage.UpsertDeliveryPlanAsync(characterUUID, entity);

    /// <inheritdoc/>
    protected override Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.DeleteDeliveryPlanAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override string GetEntityUuid(DeliveryPlan entity) => entity.UUID;
}

/// <summary>
/// Extension methods for registering DeliveryPlan endpoints.
/// </summary>
public static class DeliveryPlanEndpointsExtensions
{
    /// <summary>
    /// Maps the DeliveryPlan CRUD endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapDeliveryPlanEndpoints(this WebApplication app)
    {
        var endpoints = new DeliveryPlanEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/delivery-plans")
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
