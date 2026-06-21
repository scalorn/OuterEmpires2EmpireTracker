// -----------------------------------------------------------------------
// <copyright file="ColonyEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Push;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed CRUD endpoint handler for Colony entities.
/// Includes deduplication logic for PlanetName+SystemName matching.
/// </summary>
public class ColonyEndpoints : TypedEndpointBase<Colony, ColonyCreateRequest, ColonyUpdateRequest>
{
    /// <inheritdoc/>
    protected override string EntityTypeName => "Colony";

    /// <inheritdoc/>
    protected override string RoutePrefix => "colonies";

    /// <summary>
    /// Handles POST /colonies/{colonyUuid}/structures requests.
    /// Adds a new structure to the colony. Requires flatpackBlueprintUUID in the request body.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="colonyUuid">The colony UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the updated colony or an error response.</returns>
    public async Task<IResult> HandleAddStructure(
        string uuid, string colonyUuid, HttpContext ctx, IStorageBackend storage)
    {
        if (string.IsNullOrWhiteSpace(uuid))
        {
            return Results.BadRequest(new { error = "Invalid character UUID" });
        }

        if (!CanAccessCharacterData(ctx, uuid))
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        if (string.IsNullOrWhiteSpace(colonyUuid))
        {
            return Results.BadRequest(new { error = "Invalid entity UUID" });
        }

        var colony = await GetFromStorage(uuid, colonyUuid, storage);
        if (colony == null)
        {
            return Results.NotFound(new { error = $"{EntityTypeName} not found" });
        }

        if (!HasJsonContentType(ctx))
        {
            return Results.Json(new { error = "Unsupported media type" }, statusCode: 415);
        }

        AddStructureRequest? dto;
        try
        {
            dto = await ctx.Request.ReadFromJsonAsync<AddStructureRequest>();
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        if (dto == null)
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        if (string.IsNullOrWhiteSpace(dto.FlatpackBlueprintUUID))
        {
            return Results.BadRequest(new { error = "flatpackBlueprintUUID is required" });
        }

        var structure = new ColonyStructure
        {
            UUID = Guid.NewGuid().ToString(),
            FlatpackBlueprintUUID = dto.FlatpackBlueprintUUID,
        };

        colony.Structures.Add(structure);
        await UpsertToStorage(uuid, colony, storage);

        try
        {
            LogMutation(ctx, "Updated", colonyUuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
        }

        try
        {
            await DispatchEvent(ctx, ServerEventType.Updated, colonyUuid, uuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Event system temporarily unavailable" }, statusCode: 503);
        }

        return Results.Ok(colony);
    }

    /// <summary>
    /// Handles DELETE /colonies/{colonyUuid}/structures/{structureUuid} requests.
    /// Removes a structure from the colony by its UUID.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="colonyUuid">The colony UUID from the URL path.</param>
    /// <param name="structureUuid">The structure UUID to remove.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> indicating success or an error response.</returns>
    public async Task<IResult> HandleRemoveStructure(
        string uuid, string colonyUuid, string structureUuid, HttpContext ctx, IStorageBackend storage)
    {
        if (string.IsNullOrWhiteSpace(uuid))
        {
            return Results.BadRequest(new { error = "Invalid character UUID" });
        }

        if (!CanAccessCharacterData(ctx, uuid))
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        if (string.IsNullOrWhiteSpace(colonyUuid))
        {
            return Results.BadRequest(new { error = "Invalid entity UUID" });
        }

        var colony = await GetFromStorage(uuid, colonyUuid, storage);
        if (colony == null)
        {
            return Results.NotFound(new { error = $"{EntityTypeName} not found" });
        }

        if (string.IsNullOrWhiteSpace(structureUuid))
        {
            return Results.BadRequest(new { error = "Invalid structure UUID" });
        }

        var structure = colony.Structures.FirstOrDefault(s => s.UUID == structureUuid);
        if (structure == null)
        {
            return Results.NotFound(new { error = "Structure not found" });
        }

        colony.Structures.Remove(structure);
        await UpsertToStorage(uuid, colony, storage);

        try
        {
            LogMutation(ctx, "Updated", colonyUuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
        }

        try
        {
            await DispatchEvent(ctx, ServerEventType.Updated, colonyUuid, uuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Event system temporarily unavailable" }, statusCode: 503);
        }

        return Results.NoContent();
    }

    /// <summary>
    /// Handles POST /colonies/{colonyUuid}/items requests.
    /// Adds a new item to the colony inventory.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="colonyUuid">The colony UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the updated colony or an error response.</returns>
    public async Task<IResult> HandleAddItem(
        string uuid, string colonyUuid, HttpContext ctx, IStorageBackend storage)
    {
        if (string.IsNullOrWhiteSpace(uuid))
        {
            return Results.BadRequest(new { error = "Invalid character UUID" });
        }

        if (!CanAccessCharacterData(ctx, uuid))
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        if (string.IsNullOrWhiteSpace(colonyUuid))
        {
            return Results.BadRequest(new { error = "Invalid entity UUID" });
        }

        var colony = await GetFromStorage(uuid, colonyUuid, storage);
        if (colony == null)
        {
            return Results.NotFound(new { error = $"{EntityTypeName} not found" });
        }

        if (!HasJsonContentType(ctx))
        {
            return Results.Json(new { error = "Unsupported media type" }, statusCode: 415);
        }

        Item? item;
        try
        {
            item = await ctx.Request.ReadFromJsonAsync<Item>();
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        if (item == null)
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        if (string.IsNullOrWhiteSpace(item.UUID))
        {
            item.UUID = Guid.NewGuid().ToString();
        }

        colony.Items.AddItem(item);
        await UpsertToStorage(uuid, colony, storage);

        try
        {
            LogMutation(ctx, "Updated", colonyUuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
        }

        try
        {
            await DispatchEvent(ctx, ServerEventType.Updated, colonyUuid, uuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Event system temporarily unavailable" }, statusCode: 503);
        }

        return Results.Ok(colony);
    }

    /// <summary>
    /// Handles DELETE /colonies/{colonyUuid}/items/{itemUuid} requests.
    /// Removes an item from the colony inventory by its UUID.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="colonyUuid">The colony UUID from the URL path.</param>
    /// <param name="itemUuid">The item UUID to remove.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> indicating success or an error response.</returns>
    public async Task<IResult> HandleRemoveItem(
        string uuid, string colonyUuid, string itemUuid, HttpContext ctx, IStorageBackend storage)
    {
        if (string.IsNullOrWhiteSpace(uuid))
        {
            return Results.BadRequest(new { error = "Invalid character UUID" });
        }

        if (!CanAccessCharacterData(ctx, uuid))
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        if (string.IsNullOrWhiteSpace(colonyUuid))
        {
            return Results.BadRequest(new { error = "Invalid entity UUID" });
        }

        var colony = await GetFromStorage(uuid, colonyUuid, storage);
        if (colony == null)
        {
            return Results.NotFound(new { error = $"{EntityTypeName} not found" });
        }

        if (string.IsNullOrWhiteSpace(itemUuid))
        {
            return Results.BadRequest(new { error = "Invalid item UUID" });
        }

        if (!colony.Items.ContainsKey(itemUuid))
        {
            return Results.NotFound(new { error = "Item not found" });
        }

        colony.Items.Remove(itemUuid);
        await UpsertToStorage(uuid, colony, storage);

        try
        {
            LogMutation(ctx, "Updated", colonyUuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
        }

        try
        {
            await DispatchEvent(ctx, ServerEventType.Updated, colonyUuid, uuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Event system temporarily unavailable" }, statusCode: 503);
        }

        return Results.NoContent();
    }

    /// <summary>
    /// Handles PUT /colonies/{colonyUuid}/items/{itemUuid} requests.
    /// Updates the quantity of an item in the colony inventory.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="colonyUuid">The colony UUID from the URL path.</param>
    /// <param name="itemUuid">The item UUID to update.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the updated colony or an error response.</returns>
    public async Task<IResult> HandleUpdateItem(
        string uuid, string colonyUuid, string itemUuid, HttpContext ctx, IStorageBackend storage)
    {
        if (string.IsNullOrWhiteSpace(uuid))
        {
            return Results.BadRequest(new { error = "Invalid character UUID" });
        }

        if (!CanAccessCharacterData(ctx, uuid))
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        if (string.IsNullOrWhiteSpace(colonyUuid))
        {
            return Results.BadRequest(new { error = "Invalid entity UUID" });
        }

        var colony = await GetFromStorage(uuid, colonyUuid, storage);
        if (colony == null)
        {
            return Results.NotFound(new { error = $"{EntityTypeName} not found" });
        }

        if (string.IsNullOrWhiteSpace(itemUuid))
        {
            return Results.BadRequest(new { error = "Invalid item UUID" });
        }

        if (!colony.Items.ContainsKey(itemUuid))
        {
            return Results.NotFound(new { error = "Item not found" });
        }

        if (!HasJsonContentType(ctx))
        {
            return Results.Json(new { error = "Unsupported media type" }, statusCode: 415);
        }

        UpdateItemQuantityRequest? dto;
        try
        {
            dto = await ctx.Request.ReadFromJsonAsync<UpdateItemQuantityRequest>();
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        if (dto == null)
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        if (dto.Quantity < 0)
        {
            return Results.BadRequest(new { error = "quantity must be >= 0" });
        }

        colony.Items.Items[itemUuid].Quantity = dto.Quantity;
        await UpsertToStorage(uuid, colony, storage);

        try
        {
            LogMutation(ctx, "Updated", colonyUuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
        }

        try
        {
            await DispatchEvent(ctx, ServerEventType.Updated, colonyUuid, uuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Event system temporarily unavailable" }, statusCode: 503);
        }

        return Results.Ok(colony);
    }

    /// <summary>
    /// Handles POST /colonies/{colonyUuid}/commodity-requests requests.
    /// Adds a new commodity request to the colony.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="colonyUuid">The colony UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the updated colony or an error response.</returns>
    public async Task<IResult> HandleAddCommodityRequest(
        string uuid, string colonyUuid, HttpContext ctx, IStorageBackend storage)
    {
        if (string.IsNullOrWhiteSpace(uuid))
        {
            return Results.BadRequest(new { error = "Invalid character UUID" });
        }

        if (!CanAccessCharacterData(ctx, uuid))
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        if (string.IsNullOrWhiteSpace(colonyUuid))
        {
            return Results.BadRequest(new { error = "Invalid entity UUID" });
        }

        var colony = await GetFromStorage(uuid, colonyUuid, storage);
        if (colony == null)
        {
            return Results.NotFound(new { error = $"{EntityTypeName} not found" });
        }

        if (!HasJsonContentType(ctx))
        {
            return Results.Json(new { error = "Unsupported media type" }, statusCode: 415);
        }

        AddCommodityRequestDto? dto;
        try
        {
            dto = await ctx.Request.ReadFromJsonAsync<AddCommodityRequestDto>();
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        if (dto == null)
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        if (string.IsNullOrWhiteSpace(dto.CommodityName))
        {
            return Results.BadRequest(new { error = "commodityName is required" });
        }

        var commodityRequest = new CommodityRequested
        {
            Name = dto.CommodityName,
            Requested = dto.Requested,
            NeedBy = dto.NeedBy ?? DateTime.MinValue,
        };

        colony.Commodities.Add(commodityRequest);
        await UpsertToStorage(uuid, colony, storage);

        try
        {
            LogMutation(ctx, "Updated", colonyUuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
        }

        try
        {
            await DispatchEvent(ctx, ServerEventType.Updated, colonyUuid, uuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Event system temporarily unavailable" }, statusCode: 503);
        }

        return Results.Ok(colony);
    }

    /// <summary>
    /// Handles DELETE /colonies/{colonyUuid}/commodity-requests/{commodityName} requests.
    /// Removes a commodity request from the colony by commodity name.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="colonyUuid">The colony UUID from the URL path.</param>
    /// <param name="commodityName">The commodity name to remove.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> indicating success or an error response.</returns>
    public async Task<IResult> HandleRemoveCommodityRequest(
        string uuid, string colonyUuid, string commodityName, HttpContext ctx, IStorageBackend storage)
    {
        if (string.IsNullOrWhiteSpace(uuid))
        {
            return Results.BadRequest(new { error = "Invalid character UUID" });
        }

        if (!CanAccessCharacterData(ctx, uuid))
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        if (string.IsNullOrWhiteSpace(colonyUuid))
        {
            return Results.BadRequest(new { error = "Invalid entity UUID" });
        }

        var colony = await GetFromStorage(uuid, colonyUuid, storage);
        if (colony == null)
        {
            return Results.NotFound(new { error = $"{EntityTypeName} not found" });
        }

        if (string.IsNullOrWhiteSpace(commodityName))
        {
            return Results.BadRequest(new { error = "Invalid commodity name" });
        }

        var existing = colony.Commodities.FirstOrDefault(c =>
            string.Equals(c.Name, commodityName, StringComparison.OrdinalIgnoreCase));
        if (existing == null)
        {
            return Results.NotFound(new { error = "Commodity request not found" });
        }

        colony.Commodities.Remove(existing);
        await UpsertToStorage(uuid, colony, storage);

        try
        {
            LogMutation(ctx, "Updated", colonyUuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
        }

        try
        {
            await DispatchEvent(ctx, ServerEventType.Updated, colonyUuid, uuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Event system temporarily unavailable" }, statusCode: 503);
        }

        return Results.NoContent();
    }

    /// <summary>
    /// Handles PUT /colonies/{colonyUuid}/commodity-requests/{commodityName} requests.
    /// Updates a commodity request in the colony.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="colonyUuid">The colony UUID from the URL path.</param>
    /// <param name="commodityName">The commodity name to update.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the updated colony or an error response.</returns>
    public async Task<IResult> HandleUpdateCommodityRequest(
        string uuid, string colonyUuid, string commodityName, HttpContext ctx, IStorageBackend storage)
    {
        if (string.IsNullOrWhiteSpace(uuid))
        {
            return Results.BadRequest(new { error = "Invalid character UUID" });
        }

        if (!CanAccessCharacterData(ctx, uuid))
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        if (string.IsNullOrWhiteSpace(colonyUuid))
        {
            return Results.BadRequest(new { error = "Invalid entity UUID" });
        }

        var colony = await GetFromStorage(uuid, colonyUuid, storage);
        if (colony == null)
        {
            return Results.NotFound(new { error = $"{EntityTypeName} not found" });
        }

        if (string.IsNullOrWhiteSpace(commodityName))
        {
            return Results.BadRequest(new { error = "Invalid commodity name" });
        }

        var existing = colony.Commodities.FirstOrDefault(c =>
            string.Equals(c.Name, commodityName, StringComparison.OrdinalIgnoreCase));
        if (existing == null)
        {
            return Results.NotFound(new { error = "Commodity request not found" });
        }

        if (!HasJsonContentType(ctx))
        {
            return Results.Json(new { error = "Unsupported media type" }, statusCode: 415);
        }

        UpdateCommodityRequestDto? dto;
        try
        {
            dto = await ctx.Request.ReadFromJsonAsync<UpdateCommodityRequestDto>();
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        if (dto == null)
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        if (dto.Requested.HasValue)
        {
            existing.Requested = dto.Requested.Value;
        }

        if (dto.Delivered.HasValue)
        {
            existing.Delivered = dto.Delivered.Value;
        }

        if (dto.NeedBy.HasValue)
        {
            existing.NeedBy = dto.NeedBy.Value;
        }

        if (dto.Fulfilled.HasValue)
        {
            existing.Fulfilled = dto.Fulfilled.Value;
        }

        await UpsertToStorage(uuid, colony, storage);

        try
        {
            LogMutation(ctx, "Updated", colonyUuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
        }

        try
        {
            await DispatchEvent(ctx, ServerEventType.Updated, colonyUuid, uuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Event system temporarily unavailable" }, statusCode: 503);
        }

        return Results.Ok(colony);
    }

    /// <inheritdoc/>
    protected override string? ValidateCreate(ColonyCreateRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PlanetName))
        {
            return "PlanetName is required";
        }

        if (string.IsNullOrWhiteSpace(dto.ColonyName))
        {
            return "ColonyName is required";
        }

        return null;
    }

    /// <inheritdoc/>
    protected override string? ValidateUpdate(ColonyUpdateRequest dto) => null;

    /// <inheritdoc/>
    protected override Colony ApplyCreate(ColonyCreateRequest dto)
    {
        return new Colony
        {
            UUID = Guid.NewGuid().ToString(),
            PlanetName = dto.PlanetName,
            ColonyName = dto.ColonyName,
            SystemName = dto.SystemName ?? string.Empty,
        };
    }

    /// <inheritdoc/>
    protected override Colony ApplyUpdate(Colony existing, ColonyUpdateRequest dto)
    {
        if (dto.PlanetName != null)
        {
            existing.PlanetName = dto.PlanetName;
        }

        if (dto.ColonyName != null)
        {
            existing.ColonyName = dto.ColonyName;
        }

        if (dto.SystemName != null)
        {
            existing.SystemName = dto.SystemName;
        }

        return existing;
    }

    /// <summary>
    /// Checks for an existing colony with the same PlanetName and SystemName
    /// (case-insensitive). If found, merges the new data into the existing
    /// colony and returns 200 OK with the merged entity.
    /// </summary>
    /// <param name="characterUUID">The owning character's UUID.</param>
    /// <param name="dto">The create request DTO.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>
    /// An <see cref="IResult"/> with the merged colony (200) if a duplicate exists,
    /// or null if no duplicate and creation should proceed normally.
    /// </returns>
    protected override async Task<IResult?> HandleCreateDedup(
        string characterUUID, ColonyCreateRequest dto, IStorageBackend storage)
    {
        var allColonies = await GetAllFromStorage(characterUUID, storage);

        var existing = allColonies.FirstOrDefault(c =>
            string.Equals(c.PlanetName, dto.PlanetName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.SystemName, dto.SystemName ?? string.Empty, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
            return null;
        }

        // Merge: update ColonyName if provided
        if (!string.IsNullOrWhiteSpace(dto.ColonyName))
        {
            existing.ColonyName = dto.ColonyName;
        }

        await UpsertToStorage(characterUUID, existing, storage);
        return Results.Ok(existing);
    }

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<Colony>> GetAllFromStorage(string characterUUID, IStorageBackend storage)
        => storage.GetAllColoniesAsync(characterUUID);

    /// <inheritdoc/>
    protected override Task<Colony?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.GetColonyAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override Task UpsertToStorage(string characterUUID, Colony entity, IStorageBackend storage)
        => storage.UpsertColonyAsync(characterUUID, entity);

    /// <inheritdoc/>
    protected override Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.DeleteColonyAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override string GetEntityUuid(Colony entity) => entity.UUID;
}

/// <summary>
/// Request DTO for adding a structure to a colony.
/// </summary>
public class AddStructureRequest
{
    /// <summary>
    /// Gets or sets the flatpack blueprint UUID for the structure to add.
    /// </summary>
    public string? FlatpackBlueprintUUID { get; set; }
}

/// <summary>
/// Request DTO for updating an item's quantity in a colony.
/// </summary>
public class UpdateItemQuantityRequest
{
    /// <summary>
    /// Gets or sets the new quantity for the item.
    /// </summary>
    public int Quantity { get; set; }
}

/// <summary>
/// Request DTO for adding a commodity request to a colony.
/// </summary>
public class AddCommodityRequestDto
{
    /// <summary>
    /// Gets or sets the commodity name.
    /// </summary>
    public string? CommodityName { get; set; }

    /// <summary>
    /// Gets or sets the requested quantity.
    /// </summary>
    public int Requested { get; set; }

    /// <summary>
    /// Gets or sets the need-by date.
    /// </summary>
    public DateTime? NeedBy { get; set; }
}

/// <summary>
/// Request DTO for updating a commodity request in a colony.
/// </summary>
public class UpdateCommodityRequestDto
{
    /// <summary>
    /// Gets or sets the requested quantity.
    /// </summary>
    public int? Requested { get; set; }

    /// <summary>
    /// Gets or sets the delivered quantity.
    /// </summary>
    public int? Delivered { get; set; }

    /// <summary>
    /// Gets or sets the need-by date.
    /// </summary>
    public DateTime? NeedBy { get; set; }

    /// <summary>
    /// Gets or sets whether the request is fulfilled.
    /// </summary>
    public bool? Fulfilled { get; set; }
}

/// <summary>
/// Extension methods for registering Colony endpoints.
/// </summary>
public static class ColonyEndpointsExtensions
{
    /// <summary>
    /// Maps the Colony CRUD endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapColonyEndpoints(this WebApplication app)
    {
        var endpoints = new ColonyEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/colonies")
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
        group.MapPost("/{colonyUuid}/structures", (string uuid, string colonyUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleAddStructure(uuid, colonyUuid, ctx, storage));
        group.MapDelete("/{colonyUuid}/structures/{structureUuid}", (string uuid, string colonyUuid, string structureUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleRemoveStructure(uuid, colonyUuid, structureUuid, ctx, storage));
        group.MapPost("/{colonyUuid}/items", (string uuid, string colonyUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleAddItem(uuid, colonyUuid, ctx, storage));
        group.MapDelete("/{colonyUuid}/items/{itemUuid}", (string uuid, string colonyUuid, string itemUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleRemoveItem(uuid, colonyUuid, itemUuid, ctx, storage));
        group.MapPut("/{colonyUuid}/items/{itemUuid}", (string uuid, string colonyUuid, string itemUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleUpdateItem(uuid, colonyUuid, itemUuid, ctx, storage));
        group.MapPost("/{colonyUuid}/commodity-requests", (string uuid, string colonyUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleAddCommodityRequest(uuid, colonyUuid, ctx, storage));
        group.MapDelete("/{colonyUuid}/commodity-requests/{commodityName}", (string uuid, string colonyUuid, string commodityName, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleRemoveCommodityRequest(uuid, colonyUuid, commodityName, ctx, storage));
        group.MapPut("/{colonyUuid}/commodity-requests/{commodityName}", (string uuid, string colonyUuid, string commodityName, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleUpdateCommodityRequest(uuid, colonyUuid, commodityName, ctx, storage));
    }
}
