using System.Collections.Generic;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed CRUD endpoints for StockProfile entities.
/// </summary>
public class StockProfileEndpoints : TypedEndpointBase<StockProfile, StockProfileCreateRequest, StockProfileUpdateRequest>
{
    /// <inheritdoc/>
    protected override string EntityTypeName => "StockProfile";

    /// <inheritdoc/>
    protected override string RoutePrefix => "stock-profiles";

    /// <inheritdoc/>
    protected override string? ValidateCreate(StockProfileCreateRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name is required";
        }

        return null;
    }

    /// <inheritdoc/>
    protected override string? ValidateUpdate(StockProfileUpdateRequest dto) => null;

    /// <inheritdoc/>
    protected override StockProfile ApplyCreate(StockProfileCreateRequest dto)
    {
        return new StockProfile
        {
            UUID = Guid.NewGuid().ToString(),
            Name = dto.Name,
            IsActive = dto.IsActive,
            Entries = dto.Entries ?? new List<StockProfileEntry>(),
        };
    }

    /// <inheritdoc/>
    protected override StockProfile ApplyUpdate(StockProfile existing, StockProfileUpdateRequest dto)
    {
        if (dto.Name != null)
        {
            existing.Name = dto.Name;
        }

        existing.IsActive = dto.IsActive;

        if (dto.Entries != null)
        {
            existing.Entries = dto.Entries;
        }

        return existing;
    }

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<StockProfile>> GetAllFromStorage(string characterUUID, IStorageBackend storage)
        => storage.GetAllStockProfilesAsync(characterUUID);

    /// <inheritdoc/>
    protected override Task<StockProfile?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.GetStockProfileAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override Task UpsertToStorage(string characterUUID, StockProfile entity, IStorageBackend storage)
        => storage.UpsertStockProfileAsync(characterUUID, entity);

    /// <inheritdoc/>
    protected override Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.DeleteStockProfileAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override string GetEntityUuid(StockProfile entity) => entity.UUID;
}

/// <summary>
/// Extension methods for registering StockProfile endpoints.
/// </summary>
public static class StockProfileEndpointsExtensions
{
    /// <summary>
    /// Maps all StockProfile CRUD endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapStockProfileEndpoints(this WebApplication app)
    {
        var endpoints = new StockProfileEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/stock-profiles")
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