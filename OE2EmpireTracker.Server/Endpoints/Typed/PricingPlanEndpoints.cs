// -----------------------------------------------------------------------
// <copyright file="PricingPlanEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed CRUD endpoint handler for PricingPlan entities.
/// </summary>
public class PricingPlanEndpoints : TypedEndpointBase<PricingPlan, PricingPlanCreateRequest, PricingPlanUpdateRequest>
{
    /// <inheritdoc/>
    protected override string EntityTypeName => "PricingPlan";

    /// <inheritdoc/>
    protected override string RoutePrefix => "pricing-plans";

    /// <inheritdoc/>
    protected override string? ValidateCreate(PricingPlanCreateRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name is required";
        }

        return null;
    }

    /// <inheritdoc/>
    protected override string? ValidateUpdate(PricingPlanUpdateRequest dto) => null;

    /// <inheritdoc/>
    protected override PricingPlan ApplyCreate(PricingPlanCreateRequest dto)
    {
        return new PricingPlan
        {
            UUID = Guid.NewGuid().ToString(),
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            FixedCostPerItem = dto.FixedCostPerItem,
            HourlyCostRate = dto.HourlyCostRate,
            ResourcePrices = dto.ResourcePrices ?? new Dictionary<string, decimal>(),
        };
    }

    /// <inheritdoc/>
    protected override PricingPlan ApplyUpdate(PricingPlan existing, PricingPlanUpdateRequest dto)
    {
        if (dto.Name != null)
        {
            existing.Name = dto.Name;
        }

        if (dto.Description != null)
        {
            existing.Description = dto.Description;
        }

        existing.FixedCostPerItem = dto.FixedCostPerItem;
        existing.HourlyCostRate = dto.HourlyCostRate;

        if (dto.ResourcePrices != null)
        {
            existing.ResourcePrices = dto.ResourcePrices;
        }

        return existing;
    }

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<PricingPlan>> GetAllFromStorage(string characterUUID, IStorageBackend storage)
        => storage.GetAllPricingPlansAsync(characterUUID);

    /// <inheritdoc/>
    protected override Task<PricingPlan?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.GetPricingPlanAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override Task UpsertToStorage(string characterUUID, PricingPlan entity, IStorageBackend storage)
        => storage.UpsertPricingPlanAsync(characterUUID, entity);

    /// <inheritdoc/>
    protected override Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.DeletePricingPlanAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override string GetEntityUuid(PricingPlan entity) => entity.UUID;
}

/// <summary>
/// Extension methods for registering PricingPlan endpoints.
/// </summary>
public static class PricingPlanEndpointsExtensions
{
    /// <summary>
    /// Maps the PricingPlan CRUD endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapPricingPlanEndpoints(this WebApplication app)
    {
        var endpoints = new PricingPlanEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/pricing-plans")
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
