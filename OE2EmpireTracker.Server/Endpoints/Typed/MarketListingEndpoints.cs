// -----------------------------------------------------------------------
// <copyright file="MarketListingEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed CRUD endpoint handler for MarketListing entities.
/// </summary>
public class MarketListingEndpoints : TypedEndpointBase<MarketListing, MarketListingCreateRequest, MarketListingUpdateRequest>
{
    /// <inheritdoc/>
    protected override string EntityTypeName => "MarketListing";

    /// <inheritdoc/>
    protected override string RoutePrefix => "market-listings";

    /// <inheritdoc/>
    protected override string? ValidateCreate(MarketListingCreateRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ItemName))
        {
            return "ItemName is required";
        }

        if (string.IsNullOrWhiteSpace(dto.StationUUID))
        {
            return "StationUUID is required";
        }

        return null;
    }

    /// <inheritdoc/>
    protected override string? ValidateUpdate(MarketListingUpdateRequest dto) => null;

    /// <inheritdoc/>
    protected override MarketListing ApplyCreate(MarketListingCreateRequest dto)
    {
        return new MarketListing
        {
            UUID = Guid.NewGuid().ToString(),
            ItemName = dto.ItemName,
            ItemType = dto.ItemType,
            ItemReferenceID = dto.ItemReferenceID,
            StationUUID = dto.StationUUID,
            Quantity = dto.Quantity,
            PricePerUnit = dto.PricePerUnit,
            CurrentHP = dto.CurrentHP,
            MaxHP = dto.MaxHP,
            MaxRepairPercent = dto.MaxRepairPercent,
        };
    }

    /// <inheritdoc/>
    protected override MarketListing ApplyUpdate(MarketListing existing, MarketListingUpdateRequest dto)
    {
        existing.ItemName = dto.ItemName;
        existing.ItemType = dto.ItemType;
        existing.ItemReferenceID = dto.ItemReferenceID;
        existing.StationUUID = dto.StationUUID;
        existing.Quantity = dto.Quantity;
        existing.PricePerUnit = dto.PricePerUnit;
        existing.CurrentHP = dto.CurrentHP;
        existing.MaxHP = dto.MaxHP;
        existing.MaxRepairPercent = dto.MaxRepairPercent;

        return existing;
    }

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<MarketListing>> GetAllFromStorage(string characterUUID, IStorageBackend storage)
        => storage.GetAllMarketListingsAsync(characterUUID);

    /// <inheritdoc/>
    protected override Task<MarketListing?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.GetMarketListingAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override Task UpsertToStorage(string characterUUID, MarketListing entity, IStorageBackend storage)
        => storage.UpsertMarketListingAsync(characterUUID, entity);

    /// <inheritdoc/>
    protected override Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.DeleteMarketListingAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override string GetEntityUuid(MarketListing entity) => entity.UUID;
}

/// <summary>
/// Extension methods for registering MarketListing endpoints.
/// </summary>
public static class MarketListingEndpointsExtensions
{
    /// <summary>
    /// Maps the MarketListing CRUD endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapMarketListingEndpoints(this WebApplication app)
    {
        var endpoints = new MarketListingEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/market-listings")
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
