using System.Collections.Generic;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed CRUD endpoints for ShipTemplate entities.
/// </summary>
public class ShipTemplateEndpoints : TypedEndpointBase<ShipTemplate, ShipTemplateCreateRequest, ShipTemplateUpdateRequest>
{
    /// <inheritdoc/>
    protected override string EntityTypeName => "ShipTemplate";

    /// <inheritdoc/>
    protected override string RoutePrefix => "ship-templates";

    /// <inheritdoc/>
    protected override string? ValidateCreate(ShipTemplateCreateRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name is required";
        }

        return null;
    }

    /// <inheritdoc/>
    protected override string? ValidateUpdate(ShipTemplateUpdateRequest dto) => null;

    /// <inheritdoc/>
    protected override ShipTemplate ApplyCreate(ShipTemplateCreateRequest dto)
    {
        return new ShipTemplate
        {
            UUID = Guid.NewGuid().ToString(),
            Name = dto.Name,
            HullBlueprintUUID = dto.HullBlueprintUUID ?? string.Empty,
            Components = dto.Components ?? new List<ShipComponentSlot>(),
        };
    }

    /// <inheritdoc/>
    protected override ShipTemplate ApplyUpdate(ShipTemplate existing, ShipTemplateUpdateRequest dto)
    {
        if (dto.Name != null)
        {
            existing.Name = dto.Name;
        }

        if (dto.HullBlueprintUUID != null)
        {
            existing.HullBlueprintUUID = dto.HullBlueprintUUID;
        }

        if (dto.Components != null)
        {
            existing.Components = dto.Components;
        }

        return existing;
    }

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<ShipTemplate>> GetAllFromStorage(string characterUUID, IStorageBackend storage)
        => storage.GetAllShipTemplatesAsync(characterUUID);

    /// <inheritdoc/>
    protected override Task<ShipTemplate?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.GetShipTemplateAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override Task UpsertToStorage(string characterUUID, ShipTemplate entity, IStorageBackend storage)
        => storage.UpsertShipTemplateAsync(characterUUID, entity);

    /// <inheritdoc/>
    protected override Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.DeleteShipTemplateAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override string GetEntityUuid(ShipTemplate entity) => entity.UUID;
}

/// <summary>
/// Extension methods for registering ShipTemplate endpoints.
/// </summary>
public static class ShipTemplateEndpointsExtensions
{
    /// <summary>
    /// Maps all ShipTemplate CRUD endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapShipTemplateEndpoints(this WebApplication app)
    {
        var endpoints = new ShipTemplateEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/ship-templates")
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