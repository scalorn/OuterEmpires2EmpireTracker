// -----------------------------------------------------------------------
// <copyright file="MarketTransactionEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Push;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed endpoint handler for MarketTransaction entities.
/// Read-only CRUD plus delete (no standalone POST — transactions are
/// created via record-sale on MarketListing or record-purchase action).
/// </summary>
public class MarketTransactionEndpoints
{
    private const string EntityTypeName = "MarketTransaction";

    /// <summary>
    /// Handles GET requests for all market transactions belonging to a character.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="limit">Optional maximum number of items to return.</param>
    /// <param name="offset">Optional number of items to skip.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the transaction collection or an error response.</returns>
    public async Task<IResult> HandleGetAll(
        string uuid, int? limit, int? offset, HttpContext ctx, IStorageBackend storage)
    {
        if (string.IsNullOrWhiteSpace(uuid))
        {
            return Results.BadRequest(new { error = "Invalid character UUID" });
        }

        if (!CanAccessCharacterData(ctx, uuid))
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        var items = await storage.GetAllMarketTransactionsAsync(uuid);
        return PaginationHelper.ApplyPagination(items, limit, offset);
    }

    /// <summary>
    /// Handles GET requests for a single market transaction by UUID.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="entityUuid">The entity UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the transaction or an error response.</returns>
    public async Task<IResult> HandleGetOne(
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

        var entity = await storage.GetMarketTransactionAsync(uuid, entityUuid);
        if (entity == null)
        {
            return Results.NotFound(new { error = $"{EntityTypeName} not found" });
        }

        return Results.Ok(entity);
    }

    /// <summary>
    /// Handles DELETE requests to remove a market transaction.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="entityUuid">The entity UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> indicating success or an error response.</returns>
    public async Task<IResult> HandleDelete(
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

        var existing = await storage.GetMarketTransactionAsync(uuid, entityUuid);
        if (existing == null)
        {
            return Results.NotFound(new { error = $"{EntityTypeName} not found" });
        }

        await storage.DeleteMarketTransactionAsync(uuid, entityUuid);

        try
        {
            LogMutation(ctx, "Deleted", entityUuid);
        }
        catch (Exception)
        {
            // Rollback: re-upsert the deleted entity
            await storage.UpsertMarketTransactionAsync(uuid, existing);
            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
        }

        try
        {
            await DispatchEvent(ctx, ServerEventType.Deleted, entityUuid, uuid);
        }
        catch (Exception)
        {
            // Rollback: re-upsert the deleted entity
            await storage.UpsertMarketTransactionAsync(uuid, existing);
            return Results.Json(new { error = "Event system temporarily unavailable" }, statusCode: 503);
        }

        return Results.NoContent();
    }

    /// <summary>
    /// Handles POST requests to record a purchase transaction.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the created transaction or an error response.</returns>
    public async Task<IResult> HandleRecordPurchase(
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

        RecordPurchaseRequest? dto;
        try
        {
            dto = await ctx.Request.ReadFromJsonAsync<RecordPurchaseRequest>();
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        if (dto == null)
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        if (string.IsNullOrWhiteSpace(dto.ItemName))
        {
            return Results.BadRequest(new { error = "itemName is required" });
        }

        if (dto.Quantity <= 0)
        {
            return Results.BadRequest(new { error = "quantity is required" });
        }

        if (dto.PricePerUnit <= 0)
        {
            return Results.BadRequest(new { error = "pricePerUnit is required" });
        }

        var transaction = new MarketTransaction
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerUUID = uuid,
            TransactionType = TransactionType.Buy,
            ItemName = dto.ItemName,
            ItemType = dto.ItemType,
            Quantity = dto.Quantity,
            PricePerUnit = dto.PricePerUnit,
            TotalPrice = dto.Quantity * dto.PricePerUnit,
            Counterparty = dto.Counterparty ?? string.Empty,
            CounterpartyFaction = dto.CounterpartyFaction ?? string.Empty,
            StationUUID = dto.StationUUID ?? string.Empty,
            Timestamp = OE2EmpireTracker.Services.SystemClock.UtcNow.ToString("o"),
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

        return Results.Created(
            $"/api/v1/characters/{uuid}/market-transactions/{transaction.UUID}",
            transaction);
    }

    /// <summary>
    /// Handles GET requests for profit/loss summary with date filters.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="startDate">Optional start date filter (ISO format).</param>
    /// <param name="endDate">Optional end date filter (ISO format).</param>
    /// <param name="itemName">Optional item name filter.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the profit/loss summary or an error response.</returns>
    public async Task<IResult> HandleProfitLoss(
        string uuid,
        string? startDate,
        string? endDate,
        string? itemName,
        HttpContext ctx,
        IStorageBackend storage)
    {
        if (string.IsNullOrWhiteSpace(uuid))
        {
            return Results.BadRequest(new { error = "Invalid character UUID" });
        }

        if (!CanAccessCharacterData(ctx, uuid))
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        DateTime? parsedStart = null;
        DateTime? parsedEnd = null;

        if (!string.IsNullOrWhiteSpace(startDate))
        {
            if (!DateTime.TryParse(startDate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var s))
            {
                return Results.BadRequest(new { error = "Invalid date format" });
            }

            parsedStart = s;
        }

        if (!string.IsNullOrWhiteSpace(endDate))
        {
            if (!DateTime.TryParse(endDate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var e))
            {
                return Results.BadRequest(new { error = "Invalid date format" });
            }

            parsedEnd = e;
        }

        var allTransactions = await storage.GetAllMarketTransactionsAsync(uuid);

        var filtered = allTransactions.AsEnumerable();

        if (parsedStart.HasValue)
        {
            filtered = filtered.Where(t =>
                DateTime.TryParse(t.Timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var ts)
                && ts >= parsedStart.Value);
        }

        if (parsedEnd.HasValue)
        {
            filtered = filtered.Where(t =>
                DateTime.TryParse(t.Timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var ts)
                && ts <= parsedEnd.Value);
        }

        if (!string.IsNullOrWhiteSpace(itemName))
        {
            filtered = filtered.Where(t =>
                string.Equals(t.ItemName, itemName, StringComparison.OrdinalIgnoreCase));
        }

        var summary = new ProfitLossSummary();

        foreach (var t in filtered)
        {
            if (t.TransactionType == TransactionType.Sell)
            {
                summary.TotalSalesRevenue += t.TotalPrice;
            }
            else
            {
                summary.TotalPurchaseCost += t.TotalPrice;
            }

            if (!summary.ItemBreakdown.TryGetValue(t.ItemName, out var breakdown))
            {
                breakdown = new ProfitLossItemBreakdown { ItemName = t.ItemName };
                summary.ItemBreakdown[t.ItemName] = breakdown;
            }

            if (t.TransactionType == TransactionType.Sell)
            {
                breakdown.SalesRevenue += t.TotalPrice;
                breakdown.QuantitySold += t.Quantity;
            }
            else
            {
                breakdown.PurchaseCost += t.TotalPrice;
                breakdown.QuantityBought += t.Quantity;
            }

            breakdown.NetProfitLoss = breakdown.SalesRevenue - breakdown.PurchaseCost;
        }

        summary.NetProfitLoss = summary.TotalSalesRevenue - summary.TotalPurchaseCost;

        return Results.Ok(summary);
    }

    private static bool CanAccessCharacterData(HttpContext httpContext, string characterUuid)
    {
        if (httpContext.User.IsInRole("Owner"))
        {
            return true;
        }

        var callerCharUUID = httpContext.User.FindFirstValue("CharacterUUID");
        return callerCharUUID == characterUuid;
    }

    private static async Task DispatchEvent(
        HttpContext httpContext,
        ServerEventType eventType,
        string entityUuid,
        string ownerCharacterUuid)
    {
        var dispatcher = httpContext.RequestServices.GetRequiredService<EventDispatcher>();
        await dispatcher.DispatchEvent(new ServerEvent
        {
            EventType = eventType,
            EntityType = EntityTypeName,
            EntityUUID = entityUuid,
            OwnerCharacterUUID = ownerCharacterUuid,
        });
    }

    private void LogMutation(HttpContext httpContext, string action, string entityUuid)
    {
        var tokenId = httpContext.User.FindFirstValue("TokenId") ?? "unknown";
        var remoteIp = httpContext.Connection.RemoteIpAddress;
        var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger($"TypedEndpoint.{EntityTypeName}");

        logger.LogInformation(
            "Mutation: {Action} {EntityType}/{UUID} by token {TokenId} from {IP}",
            action,
            EntityTypeName,
            entityUuid,
            tokenId,
            remoteIp);
    }
}

/// <summary>
/// Request DTO for recording a purchase transaction.
/// </summary>
public class RecordPurchaseRequest
{
    /// <summary>Gets or sets the item name.</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>Gets or sets the item type.</summary>
    public ItemType.ItemTypeEnum ItemType { get; set; }

    /// <summary>Gets or sets the quantity purchased.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets the price per unit.</summary>
    public decimal PricePerUnit { get; set; }

    /// <summary>Gets or sets the counterparty name.</summary>
    public string? Counterparty { get; set; }

    /// <summary>Gets or sets the counterparty faction.</summary>
    public string? CounterpartyFaction { get; set; }

    /// <summary>Gets or sets the station UUID where the purchase occurred.</summary>
    public string? StationUUID { get; set; }
}

/// <summary>
/// Extension methods for registering MarketTransaction endpoints.
/// </summary>
public static class MarketTransactionEndpointsExtensions
{
    /// <summary>
    /// Maps the MarketTransaction endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapMarketTransactionEndpoints(this WebApplication app)
    {
        var endpoints = new MarketTransactionEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/market-transactions")
            .RequireAuthorization("Authenticated");

        group.MapGet("/", (string uuid, int? limit, int? offset, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleGetAll(uuid, limit, offset, ctx, storage));
        group.MapGet("/{entityUuid}", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleGetOne(uuid, entityUuid, ctx, storage));
        group.MapDelete("/{entityUuid}", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleDelete(uuid, entityUuid, ctx, storage));
        group.MapPost("/record-purchase", (string uuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleRecordPurchase(uuid, ctx, storage));
        group.MapGet("/profit-loss", (string uuid, string? startDate, string? endDate, string? itemName, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleProfitLoss(uuid, startDate, endDate, itemName, ctx, storage));
    }
}