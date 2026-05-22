// -----------------------------------------------------------------------
// <copyright file="BlueprintEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Text.Json;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Push;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed CRUD endpoint handler for Blueprint entities.
/// </summary>
public class BlueprintEndpoints : TypedEndpointBase<Blueprint, BlueprintCreateRequest, BlueprintUpdateRequest>
{
    /// <summary>
    /// Handles POST /import requests to import a full Blueprint object.
    /// If a blueprint with the same UUID exists, merges and returns 200.
    /// If no matching blueprint exists, inserts and returns 201 with Location header.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the imported entity or an error response.</returns>
    public async Task<IResult> HandleImport(string uuid, HttpContext ctx, IStorageBackend storage)
    {
        if (string.IsNullOrWhiteSpace(uuid))
        {
            return Results.BadRequest(new { error = "Invalid character UUID" });
        }

        if (!CanAccessCharacterData(ctx, uuid))
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        if (!HasJsonContentType(ctx))
        {
            return Results.Json(new { error = "Unsupported media type" }, statusCode: 415);
        }

        Blueprint? imported;
        try
        {
            imported = await ctx.Request.ReadFromJsonAsync<Blueprint>();
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        if (imported == null)
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        if (string.IsNullOrWhiteSpace(imported.UUID))
        {
            imported.UUID = Guid.NewGuid().ToString();
        }

        var existing = await GetFromStorage(uuid, imported.UUID, storage);
        bool isNew = existing == null;

        await UpsertToStorage(uuid, imported, storage);

        string action = isNew ? "Created" : "Updated";
        try
        {
            LogMutation(ctx, action, imported.UUID);
        }
        catch (Exception)
        {
            if (isNew)
            {
                await DeleteFromStorage(uuid, imported.UUID, storage);
            }
            else
            {
                await UpsertToStorage(uuid, existing!, storage);
            }

            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
        }

        var eventType = isNew ? ServerEventType.Created : ServerEventType.Updated;
        try
        {
            await DispatchEvent(ctx, eventType, imported.UUID, uuid);
        }
        catch (Exception)
        {
            if (isNew)
            {
                await DeleteFromStorage(uuid, imported.UUID, storage);
            }
            else
            {
                await UpsertToStorage(uuid, existing!, storage);
            }

            return Results.Json(new { error = "Event system temporarily unavailable" }, statusCode: 503);
        }

        if (isNew)
        {
            var location = $"/api/v1/characters/{uuid}/{RoutePrefix}/{imported.UUID}";
            return Results.Created(location, imported);
        }

        return Results.Ok(imported);
    }

    /// <summary>
    /// Handles POST /blueprints/{entityUuid}/move-to-global requests.
    /// Sets the blueprint's OwnerUUID to empty (global scope) and returns the updated blueprint.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="entityUuid">The blueprint UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the updated blueprint or an error response.</returns>
    public async Task<IResult> HandleMoveToGlobal(
        string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
    {
        if (string.IsNullOrWhiteSpace(uuid))
        {
            return Results.BadRequest(new { error = "Invalid character UUID" });
        }

        if (!CanAccessCharacterData(ctx, uuid))
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        if (string.IsNullOrWhiteSpace(entityUuid))
        {
            return Results.BadRequest(new { error = "Invalid entity UUID" });
        }

        var blueprint = await GetFromStorage(uuid, entityUuid, storage);
        if (blueprint == null)
        {
            return Results.NotFound(new { error = $"{EntityTypeName} not found" });
        }

        blueprint.OwnerUUID = string.Empty;
        await UpsertToStorage(uuid, blueprint, storage);

        try
        {
            LogMutation(ctx, "Updated", entityUuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
        }

        try
        {
            await DispatchEvent(ctx, ServerEventType.Updated, entityUuid, uuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Event system temporarily unavailable" }, statusCode: 503);
        }

        return Results.Ok(blueprint);
    }

    /// <summary>
    /// Handles POST /blueprints/{entityUuid}/move-to-player requests.
    /// Sets the blueprint's OwnerUUID to the character UUID (player scope) and returns the updated blueprint.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="entityUuid">The blueprint UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the updated blueprint or an error response.</returns>
    public async Task<IResult> HandleMoveToPlayer(
        string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
    {
        if (string.IsNullOrWhiteSpace(uuid))
        {
            return Results.BadRequest(new { error = "Invalid character UUID" });
        }

        if (!CanAccessCharacterData(ctx, uuid))
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        if (string.IsNullOrWhiteSpace(entityUuid))
        {
            return Results.BadRequest(new { error = "Invalid entity UUID" });
        }

        var blueprint = await GetFromStorage(uuid, entityUuid, storage);
        if (blueprint == null)
        {
            return Results.NotFound(new { error = $"{EntityTypeName} not found" });
        }

        blueprint.OwnerUUID = uuid;
        await UpsertToStorage(uuid, blueprint, storage);

        try
        {
            LogMutation(ctx, "Updated", entityUuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
        }

        try
        {
            await DispatchEvent(ctx, ServerEventType.Updated, entityUuid, uuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Event system temporarily unavailable" }, statusCode: 503);
        }

        return Results.Ok(blueprint);
    }

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
        group.MapPost("/import", (string uuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleImport(uuid, ctx, storage));
        group.MapPost("/{entityUuid}/move-to-global", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleMoveToGlobal(uuid, entityUuid, ctx, storage));
        group.MapPost("/{entityUuid}/move-to-player", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleMoveToPlayer(uuid, entityUuid, ctx, storage));
    }
}
