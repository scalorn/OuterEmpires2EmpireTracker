// -----------------------------------------------------------------------
// <copyright file="MarketTransactionEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Security.Claims;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Push;
using OE2EmpireTracker.Server.Storage;

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
    }
}
