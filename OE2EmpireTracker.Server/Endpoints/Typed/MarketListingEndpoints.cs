// -----------------------------------------------------------------------
// <copyright file="MarketListingEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Text.Json;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Push;

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

    /// <summary>
    /// Handles POST /market-listings/{entityUuid}/record-sale requests.
    /// Creates a MarketTransaction from the listing and returns 201.
    /// Returns 422 if required fields (quantity, pricePerUnit) are missing.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="entityUuid">The market listing UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the created transaction or an error response.</returns>
    public async Task<IResult> HandleRecordSale(
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

        var listing = await GetFromStorage(uuid, entityUuid, storage);
        if (listing == null)
        {
            return Results.NotFound(new { error = $"{EntityTypeName} not found" });
        }

        if (!HasJsonContentType(ctx))
        {
            return Results.Json(new { error = "Unsupported media type" }, statusCode: 415);
        }

        RecordSaleRequest? dto;
        try
        {
            dto = await ctx.Request.ReadFromJsonAsync<RecordSaleRequest>();
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        if (dto == null)
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        if (dto.Quantity <= 0)
        {
            return Results.Json(new { error = "quantity is required and must be positive" }, statusCode: 422);
        }

        if (dto.PricePerUnit <= 0)
        {
            return Results.Json(new { error = "pricePerUnit is required and must be positive" }, statusCode: 422);
        }

        var transaction = new MarketTransaction
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerUUID = uuid,
            TransactionType = TransactionType.Sell,
            ItemType = listing.ItemType,
            ItemReferenceID = listing.ItemReferenceID,
            ItemName = listing.ItemName,
            Quantity = dto.Quantity,
            PricePerUnit = dto.PricePerUnit,
            TotalPrice = dto.Quantity * dto.PricePerUnit,
            Counterparty = dto.Counterparty ?? string.Empty,
            CounterpartyFaction = dto.CounterpartyFaction ?? string.Empty,
            StationUUID = dto.StationUUID ?? listing.StationUUID,
            Timestamp = OE2EmpireTracker.Services.SystemClock.UtcNow.ToString("o"),
            ListingUUID = entityUuid,
        };

        await storage.UpsertMarketTransactionAsync(uuid, transaction);

        try
        {
            LogMutation(ctx, "Created", transaction.UUID);
        }
        catch (Exception)
        {
            await storage.DeleteMarketTransactionAsync(uuid, transaction.UUID);
            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
        }

        try
        {
            await DispatchEvent(ctx, ServerEventType.Created, transaction.UUID, uuid);
        }
        catch (Exception)
        {
            await storage.DeleteMarketTransactionAsync(uuid, transaction.UUID);
            return Results.Json(new { error = "Event system temporarily unavailable" }, statusCode: 503);
        }

        var location = $"/api/v1/characters/{uuid}/market-transactions/{transaction.UUID}";
        return Results.Created(location, transaction);
    }

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
/// Request DTO for recording a sale against a market listing.
/// </summary>
public class RecordSaleRequest
{
    /// <summary>
    /// Gets or sets the quantity sold.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the price per unit.
    /// </summary>
    public decimal PricePerUnit { get; set; }

    /// <summary>
    /// Gets or sets the counterparty name (optional).
    /// </summary>
    public string? Counterparty { get; set; }

    /// <summary>
    /// Gets or sets the counterparty faction (optional).
    /// </summary>
    public string? CounterpartyFaction { get; set; }

    /// <summary>
    /// Gets or sets the station UUID where the sale occurred (optional, defaults to listing's station).
    /// </summary>
    public string? StationUUID { get; set; }
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
        group.MapPost("/{entityUuid}/record-sale", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleRecordSale(uuid, entityUuid, ctx, storage));
    }
}