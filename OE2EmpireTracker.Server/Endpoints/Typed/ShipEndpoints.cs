// -----------------------------------------------------------------------
// <copyright file="ShipEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Models;

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

    /// <summary>
    /// Handles POST /ships/from-template requests.
    /// Creates a new Ship from an existing ShipTemplate, copying Name,
    /// HullBlueprintUUID, and Components from the template.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the created Ship or an error response.</returns>
    public async Task<IResult> HandleFromTemplate(
        string uuid, HttpContext ctx, IStorageBackend storage)
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

        FromTemplateRequest? request;
        try
        {
            request = await ctx.Request.ReadFromJsonAsync<FromTemplateRequest>();
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.TemplateUUID))
        {
            return Results.BadRequest(new { error = "templateUUID is required" });
        }

        var template = await storage.GetShipTemplateAsync(uuid, request.TemplateUUID);
        if (template == null)
        {
            return Results.NotFound(new { error = "ShipTemplate not found" });
        }

        var ship = new Ship
        {
            UUID = Guid.NewGuid().ToString(),
            Name = template.Name,
            TemplateUUID = template.UUID,
            HullBlueprintUUID = template.HullBlueprintUUID,
            Components = template.Components
                .Select(c => new ShipComponentSlot
                {
                    SlotType = c.SlotType,
                    SlotIndex = c.SlotIndex,
                    BlueprintUUID = c.BlueprintUUID,
                    CurrentHP = c.CurrentHP,
                    MaxHP = c.MaxHP,
                    MaxRepairPercent = c.MaxRepairPercent,
                })
                .ToList(),
        };

        await UpsertToStorage(uuid, ship, storage);

        var location = $"/api/v1/characters/{uuid}/{RoutePrefix}/{ship.UUID}";
        return Results.Created(location, ship);
    }

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
/// Request body for the from-template action.
/// </summary>
public class FromTemplateRequest
{
    /// <summary>
    /// Gets or sets the UUID of the ShipTemplate to create a ship from.
    /// </summary>
    public string? TemplateUUID { get; set; }
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
        group.MapPost("/from-template", (string uuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleFromTemplate(uuid, ctx, storage));
    }
}