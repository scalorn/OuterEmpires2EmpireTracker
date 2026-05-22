// -----------------------------------------------------------------------
// <copyright file="BuildPlanEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed CRUD endpoint handler for BuildPlan entities.
/// </summary>
public class BuildPlanEndpoints : TypedEndpointBase<BuildPlan, BuildPlanCreateRequest, BuildPlanUpdateRequest>
{
    /// <inheritdoc/>
    protected override string EntityTypeName => "BuildPlan";

    /// <inheritdoc/>
    protected override string RoutePrefix => "build-plans";

    /// <inheritdoc/>
    protected override string? ValidateCreate(BuildPlanCreateRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name is required";
        }

        return null;
    }

    /// <inheritdoc/>
    protected override string? ValidateUpdate(BuildPlanUpdateRequest dto) => null;

    /// <inheritdoc/>
    protected override BuildPlan ApplyCreate(BuildPlanCreateRequest dto)
    {
        return new BuildPlan
        {
            UUID = Guid.NewGuid().ToString(),
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            IsActive = dto.IsActive,
            Items = dto.Items ?? new List<BuildItem>(),
        };
    }

    /// <inheritdoc/>
    protected override BuildPlan ApplyUpdate(BuildPlan existing, BuildPlanUpdateRequest dto)
    {
        if (dto.Name != null)
        {
            existing.Name = dto.Name;
        }

        if (dto.Description != null)
        {
            existing.Description = dto.Description;
        }

        existing.IsActive = dto.IsActive;

        if (dto.Items != null)
        {
            existing.Items = dto.Items;
        }

        return existing;
    }

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<BuildPlan>> GetAllFromStorage(string characterUUID, IStorageBackend storage)
        => storage.GetAllBuildPlansAsync(characterUUID);

    /// <inheritdoc/>
    protected override Task<BuildPlan?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.GetBuildPlanAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override Task UpsertToStorage(string characterUUID, BuildPlan entity, IStorageBackend storage)
        => storage.UpsertBuildPlanAsync(characterUUID, entity);

    /// <inheritdoc/>
    protected override Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.DeleteBuildPlanAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override string GetEntityUuid(BuildPlan entity) => entity.UUID;
}

/// <summary>
/// Extension methods for registering BuildPlan endpoints.
/// </summary>
public static class BuildPlanEndpointsExtensions
{
    /// <summary>
    /// Maps the BuildPlan CRUD endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapBuildPlanEndpoints(this WebApplication app)
    {
        var endpoints = new BuildPlanEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/build-plans")
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
