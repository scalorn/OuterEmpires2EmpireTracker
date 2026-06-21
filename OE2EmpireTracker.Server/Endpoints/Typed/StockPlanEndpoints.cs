using System.Collections.Generic;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed CRUD endpoints for StockPlan entities.
/// </summary>
public class StockPlanEndpoints : TypedEndpointBase<StockPlan, StockPlanCreateRequest, StockPlanUpdateRequest>
{
    /// <inheritdoc/>
    protected override string EntityTypeName => "StockPlan";

    /// <inheritdoc/>
    protected override string RoutePrefix => "stock-plans";

    /// <inheritdoc/>
    protected override string? ValidateCreate(StockPlanCreateRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name is required";
        }

        return null;
    }

    /// <inheritdoc/>
    protected override string? ValidateUpdate(StockPlanUpdateRequest dto) => null;

    /// <inheritdoc/>
    protected override StockPlan ApplyCreate(StockPlanCreateRequest dto)
    {
        return new StockPlan
        {
            UUID = Guid.NewGuid().ToString(),
            Name = dto.Name,
            IsActive = dto.IsActive,
            ReplenishmentBuildPlanUUID = dto.ReplenishmentBuildPlanUUID ?? string.Empty,
            Targets = dto.Targets ?? new List<StockTarget>(),
        };
    }

    /// <inheritdoc/>
    protected override StockPlan ApplyUpdate(StockPlan existing, StockPlanUpdateRequest dto)
    {
        if (dto.Name != null)
        {
            existing.Name = dto.Name;
        }

        existing.IsActive = dto.IsActive;

        if (dto.ReplenishmentBuildPlanUUID != null)
        {
            existing.ReplenishmentBuildPlanUUID = dto.ReplenishmentBuildPlanUUID;
        }

        if (dto.Targets != null)
        {
            existing.Targets = dto.Targets;
        }

        return existing;
    }

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<StockPlan>> GetAllFromStorage(string characterUUID, IStorageBackend storage)
        => storage.GetAllStockPlansAsync(characterUUID);

    /// <inheritdoc/>
    protected override Task<StockPlan?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.GetStockPlanAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override Task UpsertToStorage(string characterUUID, StockPlan entity, IStorageBackend storage)
        => storage.UpsertStockPlanAsync(characterUUID, entity);

    /// <inheritdoc/>
    protected override Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.DeleteStockPlanAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override string GetEntityUuid(StockPlan entity) => entity.UUID;
}

/// <summary>
/// Extension methods for registering StockPlan endpoints.
/// </summary>
public static class StockPlanEndpointsExtensions
{
    /// <summary>
    /// Maps all StockPlan CRUD endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapStockPlanEndpoints(this WebApplication app)
    {
        var endpoints = new StockPlanEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/stock-plans")
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