// -----------------------------------------------------------------------
// <copyright file="BlueprintEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed CRUD endpoint handler for Blueprint entities.
/// </summary>
public class BlueprintEndpoints : TypedEndpointBase<Blueprint, BlueprintCreateRequest, BlueprintUpdateRequest>
{
    /// <inheritdoc/>
    protected override string EntityTypeName => "Blueprint";

    /// <inheritdoc/>
    protected override string RoutePrefix => "blueprints";

    /// <inheritdoc/>
    protected override string? ValidateCreate(BlueprintCreateRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name is required";
        }

        if (string.IsNullOrWhiteSpace(dto.BluePrintType))
        {
            return "BluePrintType is required";
        }

        return null;
    }

    /// <inheritdoc/>
    protected override string? ValidateUpdate(BlueprintUpdateRequest dto) => null;

    /// <inheritdoc/>
    protected override Blueprint ApplyCreate(BlueprintCreateRequest dto)
    {
        var blueprint = new Blueprint(dto.Name)
        {
            UUID = Guid.NewGuid().ToString(),
            BluePrintType = dto.BluePrintType,
            Evolution = dto.Evolution,
            TechLevel = dto.TechLevel,
            Class = dto.Class,
            CopyCost = dto.CopyCost,
            BaseBlueprintUUID = dto.BaseBlueprintUUID,
            NickName = dto.NickName,
            Description = dto.Description,
        };

        if (dto.Properties != null)
        {
            foreach (var kvp in dto.Properties)
            {
                blueprint.Properties.SetProperty(kvp.Key, kvp.Value);
            }
        }

        if (dto.Resources != null)
        {
            blueprint.Resources = dto.Resources;
        }

        return blueprint;
    }

    /// <inheritdoc/>
    protected override Blueprint ApplyUpdate(Blueprint existing, BlueprintUpdateRequest dto)
    {
        if (dto.Name != null)
        {
            existing.Name = dto.Name;
        }

        if (dto.NickName != null)
        {
            existing.NickName = dto.NickName;
        }

        if (dto.Description != null)
        {
            existing.Description = dto.Description;
        }

        if (dto.BluePrintType != null)
        {
            existing.BluePrintType = dto.BluePrintType;
        }

        existing.Evolution = dto.Evolution;

        if (dto.TechLevel != null)
        {
            existing.TechLevel = dto.TechLevel;
        }

        existing.Class = dto.Class;
        existing.CopyCost = dto.CopyCost;

        if (dto.BaseBlueprintUUID != null)
        {
            existing.BaseBlueprintUUID = dto.BaseBlueprintUUID;
        }

        if (dto.Properties != null)
        {
            existing.Properties = new PropertyBag();
            foreach (var kvp in dto.Properties)
            {
                existing.Properties.SetProperty(kvp.Key, kvp.Value);
            }
        }

        if (dto.Resources != null)
        {
            existing.Resources = dto.Resources;
        }

        return existing;
    }

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<Blueprint>> GetAllFromStorage(string characterUUID, IStorageBackend storage)
        => storage.GetAllBlueprintsAsync(characterUUID);

    /// <inheritdoc/>
    protected override Task<Blueprint?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.GetBlueprintAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override Task UpsertToStorage(string characterUUID, Blueprint entity, IStorageBackend storage)
        => storage.UpsertBlueprintAsync(characterUUID, entity);

    /// <inheritdoc/>
    protected override Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.DeleteBlueprintAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override string GetEntityUuid(Blueprint entity) => entity.UUID;
}

/// <summary>
/// Extension methods for registering Blueprint endpoints.
/// </summary>
public static class BlueprintEndpointsExtensions
{
    /// <summary>
    /// Maps the Blueprint CRUD endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapBlueprintEndpoints(this WebApplication app)
    {
        var endpoints = new BlueprintEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/blueprints")
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
