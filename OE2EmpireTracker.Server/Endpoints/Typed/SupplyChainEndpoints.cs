// -----------------------------------------------------------------------
// <copyright file="SupplyChainEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed CRUD endpoint handler for SupplyChain entities.
/// </summary>
public class SupplyChainEndpoints : TypedEndpointBase<SupplyChain, SupplyChainCreateRequest, SupplyChainUpdateRequest>
{
    /// <inheritdoc/>
    protected override string EntityTypeName => "SupplyChain";

    /// <inheritdoc/>
    protected override string RoutePrefix => "supply-chains";

    /// <inheritdoc/>
    protected override string? ValidateCreate(SupplyChainCreateRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name is required";
        }

        return null;
    }

    /// <inheritdoc/>
    protected override string? ValidateUpdate(SupplyChainUpdateRequest dto) => null;

    /// <inheritdoc/>
    protected override SupplyChain ApplyCreate(SupplyChainCreateRequest dto)
    {
        var stages = DeepCopyAndRenumberStages(dto.Stages);

        return new SupplyChain
        {
            UUID = Guid.NewGuid().ToString(),
            Name = dto.Name,
            IsActive = dto.IsActive,
            Stages = stages,
        };
    }

    /// <inheritdoc/>
    protected override SupplyChain ApplyUpdate(SupplyChain existing, SupplyChainUpdateRequest dto)
    {
        if (dto.Name != null)
        {
            existing.Name = dto.Name;
        }

        existing.IsActive = dto.IsActive;

        if (dto.Stages != null)
        {
            existing.Stages = DeepCopyAndRenumberStages(dto.Stages);
        }

        return existing;
    }

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<SupplyChain>> GetAllFromStorage(string characterUUID, IStorageBackend storage)
        => storage.GetAllSupplyChainsAsync(characterUUID);

    /// <inheritdoc/>
    protected override Task<SupplyChain?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.GetSupplyChainAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override Task UpsertToStorage(string characterUUID, SupplyChain entity, IStorageBackend storage)
        => storage.UpsertSupplyChainAsync(characterUUID, entity);

    /// <inheritdoc/>
    protected override Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.DeleteSupplyChainAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override string GetEntityUuid(SupplyChain entity) => entity.UUID;

    private static List<SupplyChainStage> DeepCopyAndRenumberStages(List<SupplyChainStage> stages)
    {
        if (stages == null)
        {
            return new List<SupplyChainStage>();
        }

        return stages.Select((stage, index) => new SupplyChainStage
        {
            Sequence = index + 1,
            StageType = stage.StageType,
            LocationType = stage.LocationType,
            LocationUUID = stage.LocationUUID,
            ResourceName = stage.ResourceName,
            ResourcePurity = stage.ResourcePurity,
            AccumulationThreshold = stage.AccumulationThreshold,
            ProductionRatePerHour = stage.ProductionRatePerHour,
            DeliveryRouteUUID = stage.DeliveryRouteUUID,
        }).ToList();
    }
}

/// <summary>
/// Extension methods for registering SupplyChain endpoints.
/// </summary>
public static class SupplyChainEndpointsExtensions
{
    /// <summary>
    /// Maps the SupplyChain CRUD endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapSupplyChainEndpoints(this WebApplication app)
    {
        var endpoints = new SupplyChainEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/supply-chains")
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