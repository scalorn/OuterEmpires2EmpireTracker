// -----------------------------------------------------------------------
// <copyright file="BuildPlanEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

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

    /// <summary>
    /// Handles POST /generate-colony-items requests.
    /// Scans a colony's structures for staged items not already tracked in the
    /// build plan, adds them as new BuildItems, and returns the count added.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="entityUuid">The build plan UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> with the number of items added or an error.</returns>
    public async Task<IResult> HandleGenerateColonyItems(
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

        if (!HasJsonContentType(ctx))
        {
            return Results.Json(new { error = "Unsupported media type" }, statusCode: 415);
        }

        GenerateColonyItemsRequest? request;
        try
        {
            request = await ctx.Request.ReadFromJsonAsync<GenerateColonyItemsRequest>();
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.ColonyUUID))
        {
            return Results.BadRequest(new { error = "colonyUUID is required" });
        }

        var plan = await GetFromStorage(uuid, entityUuid, storage);
        if (plan == null)
        {
            return Results.NotFound(new { error = "BuildPlan not found" });
        }

        var colony = await storage.GetColonyAsync(uuid, request.ColonyUUID);
        if (colony == null)
        {
            return Results.NotFound(new { error = "Colony not found" });
        }

        // Find structure UUIDs already tracked in this build plan
        var existingStructureUuids = new HashSet<string>(
            plan.Items
                .Where(i => !string.IsNullOrEmpty(i.StructureUUID))
                .Select(i => i.StructureUUID),
            StringComparer.OrdinalIgnoreCase);

        int itemsAdded = 0;

        foreach (var structure in colony.Structures)
        {
            // Skip structures already in the build plan
            if (existingStructureUuids.Contains(structure.UUID))
            {
                continue;
            }

            // Only include staged structures (not yet built)
            structure.Properties.GetBoolean(GameConstants.PropStaged, false, out bool isStaged);
            if (!isStaged)
            {
                continue;
            }

            var buildItem = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Manufactory,
                Status = BuildItemStatus.Staged,
                BlueprintUUID = structure.FlatpackBlueprintUUID ?? string.Empty,
                BuildLocationType = DestinationType.Colony,
                BuildLocationUUID = colony.UUID,
                StructureUUID = structure.UUID,
                Quantity = 1,
            };

            plan.Items.Add(buildItem);
            itemsAdded++;
        }

        if (itemsAdded > 0)
        {
            await UpsertToStorage(uuid, plan, storage);
        }

        return Results.Ok(new { itemsAdded });
    }

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
/// Request body for the generate-colony-items action.
/// </summary>
public class GenerateColonyItemsRequest
{
    /// <summary>
    /// Gets or sets the UUID of the colony to scan for staged structures.
    /// </summary>
    public string? ColonyUUID { get; set; }
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
        group.MapPost("/{entityUuid}/generate-colony-items", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleGenerateColonyItems(uuid, entityUuid, ctx, storage));
    }
}
