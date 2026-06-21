// -----------------------------------------------------------------------
// <copyright file="DeliveryPlanEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Push;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed CRUD endpoint handler for DeliveryPlan entities.
/// </summary>
public class DeliveryPlanEndpoints : TypedEndpointBase<DeliveryPlan, DeliveryPlanCreateRequest, DeliveryPlanUpdateRequest>
{
    /// <inheritdoc/>
    protected override string EntityTypeName => "DeliveryPlan";

    /// <inheritdoc/>
    protected override string RoutePrefix => "delivery-plans";

    /// <summary>
    /// Handles POST requests to add a drop-off item to a delivery plan stop.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="entityUuid">The delivery plan UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the updated plan or an error response.</returns>
    public async Task<IResult> HandleAddDropOff(
        string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
    {
        return await HandleAddItem(uuid, entityUuid, ctx, storage, isDropOff: true);
    }

    /// <summary>
    /// Handles POST requests to add a pick-up item to a delivery plan stop.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="entityUuid">The delivery plan UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the updated plan or an error response.</returns>
    public async Task<IResult> HandleAddPickUp(
        string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
    {
        return await HandleAddItem(uuid, entityUuid, ctx, storage, isDropOff: false);
    }

    /// <summary>
    /// Handles DELETE requests to remove drop-off items by index from a delivery plan stop.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="entityUuid">The delivery plan UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the updated plan or an error response.</returns>
    public async Task<IResult> HandleRemoveDropOff(
        string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
    {
        return await HandleRemoveItems(uuid, entityUuid, ctx, storage, isDropOff: true);
    }

    /// <summary>
    /// Handles DELETE requests to remove pick-up items by index from a delivery plan stop.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="entityUuid">The delivery plan UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the updated plan or an error response.</returns>
    public async Task<IResult> HandleRemovePickUp(
        string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
    {
        return await HandleRemoveItems(uuid, entityUuid, ctx, storage, isDropOff: false);
    }

    /// <summary>
    /// Handles PUT requests to mark a delivery item as delivered or undelivered.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="entityUuid">The delivery plan UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the updated plan or an error response.</returns>
    public async Task<IResult> HandleMarkDelivered(
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

        var plan = await storage.GetDeliveryPlanAsync(uuid, entityUuid);
        if (plan == null)
        {
            return Results.NotFound(new { error = "DeliveryPlan not found" });
        }

        MarkDeliveredRequest? dto;
        try
        {
            dto = await ctx.Request.ReadFromJsonAsync<MarkDeliveredRequest>();
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        if (dto == null)
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        var stop = plan.Stops.FirstOrDefault(s => s.Sequence == dto.StopSequence);
        if (stop == null)
        {
            return Results.NotFound(new { error = "Stop not found" });
        }

        var list = string.Equals(dto.ListType, "pickUp", StringComparison.OrdinalIgnoreCase)
            ? stop.PickUp
            : stop.DropOff;

        if (dto.ItemIndex < 0 || dto.ItemIndex >= list.Count)
        {
            return Results.BadRequest(new { error = "Invalid item index" });
        }

        list[dto.ItemIndex].Delivered = dto.Delivered;

        await storage.UpsertDeliveryPlanAsync(uuid, plan);

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

        return Results.Ok(plan);
    }

    /// <summary>
    /// Handles PUT requests to mark a stop as complete.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="entityUuid">The delivery plan UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the updated plan or an error response.</returns>
    public async Task<IResult> HandleMarkStopComplete(
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

        var plan = await storage.GetDeliveryPlanAsync(uuid, entityUuid);
        if (plan == null)
        {
            return Results.NotFound(new { error = "DeliveryPlan not found" });
        }

        MarkStopCompleteRequest? dto;
        try
        {
            dto = await ctx.Request.ReadFromJsonAsync<MarkStopCompleteRequest>();
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        if (dto == null)
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        var stop = plan.Stops.FirstOrDefault(s => s.Sequence == dto.StopSequence);
        if (stop == null)
        {
            return Results.NotFound(new { error = "Stop not found" });
        }

        stop.StopCompleted = true;

        await storage.UpsertDeliveryPlanAsync(uuid, plan);

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

        return Results.Ok(plan);
    }

    /// <summary>
    /// Handles PUT requests to mark an entire delivery plan as complete.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="entityUuid">The delivery plan UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the updated plan or an error response.</returns>
    public async Task<IResult> HandleMarkComplete(
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

        var plan = await storage.GetDeliveryPlanAsync(uuid, entityUuid);
        if (plan == null)
        {
            return Results.NotFound(new { error = "DeliveryPlan not found" });
        }

        plan.Completed = true;

        await storage.UpsertDeliveryPlanAsync(uuid, plan);

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

        return Results.Ok(plan);
    }

    /// <summary>
    /// Handles PUT requests to assign a ship to a delivery plan.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="entityUuid">The delivery plan UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the updated plan or an error response.</returns>
    public async Task<IResult> HandleAssignShip(
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

        var plan = await storage.GetDeliveryPlanAsync(uuid, entityUuid);
        if (plan == null)
        {
            return Results.NotFound(new { error = "DeliveryPlan not found" });
        }

        AssignShipRequest? dto;
        try
        {
            dto = await ctx.Request.ReadFromJsonAsync<AssignShipRequest>();
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        if (dto == null)
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        plan.ShipUUID = dto.ShipUUID ?? string.Empty;

        await storage.UpsertDeliveryPlanAsync(uuid, plan);

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

        return Results.Ok(plan);
    }

    /// <summary>
    /// Handles POST requests to split a delivery plan into multiple trips based on cargo capacity.
    /// Creates new plans for each trip; the original plan remains unchanged.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="entityUuid">The delivery plan UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the list of new plans or an error response.</returns>
    public async Task<IResult> HandleSplitTrips(
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

        var plan = await storage.GetDeliveryPlanAsync(uuid, entityUuid);
        if (plan == null)
        {
            return Results.NotFound(new { error = "DeliveryPlan not found" });
        }

        SplitTripsRequest? dto;
        try
        {
            dto = await ctx.Request.ReadFromJsonAsync<SplitTripsRequest>();
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        if (dto == null || dto.CargoCapacity <= 0)
        {
            return Results.BadRequest(new { error = "cargoCapacity is required and must be greater than 0" });
        }

        var newPlans = SplitPlanIntoTrips(plan, dto.CargoCapacity);

        foreach (var newPlan in newPlans)
        {
            await storage.UpsertDeliveryPlanAsync(uuid, newPlan);
        }

        try
        {
            LogMutation(ctx, "Created", entityUuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
        }

        try
        {
            await DispatchEvent(ctx, ServerEventType.Created, entityUuid, uuid);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Event system temporarily unavailable" }, statusCode: 503);
        }

        return Results.Json(newPlans, statusCode: 201);
    }

    /// <inheritdoc/>
    protected override string? ValidateCreate(DeliveryPlanCreateRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "name is required";
        }

        if (string.IsNullOrWhiteSpace(dto.RouteUUID))
        {
            return "routeUUID is required";
        }

        return null;
    }

    /// <inheritdoc/>
    protected override string? ValidateUpdate(DeliveryPlanUpdateRequest dto) => null;

    /// <inheritdoc/>
    protected override DeliveryPlan ApplyCreate(DeliveryPlanCreateRequest dto)
    {
        return new DeliveryPlan
        {
            UUID = Guid.NewGuid().ToString(),
            Name = dto.Name,
            RouteUUID = dto.RouteUUID,
        };
    }

    /// <inheritdoc/>
    protected override DeliveryPlan ApplyUpdate(DeliveryPlan existing, DeliveryPlanUpdateRequest dto)
    {
        if (dto.Name != null)
        {
            existing.Name = dto.Name;
        }

        if (dto.Stops != null)
        {
            existing.Stops = dto.Stops;
        }

        return existing;
    }

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<DeliveryPlan>> GetAllFromStorage(string characterUUID, IStorageBackend storage)
        => storage.GetAllDeliveryPlansAsync(characterUUID);

    /// <inheritdoc/>
    protected override Task<DeliveryPlan?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.GetDeliveryPlanAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override Task UpsertToStorage(string characterUUID, DeliveryPlan entity, IStorageBackend storage)
        => storage.UpsertDeliveryPlanAsync(characterUUID, entity);

    /// <inheritdoc/>
    protected override Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.DeleteDeliveryPlanAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override string GetEntityUuid(DeliveryPlan entity) => entity.UUID;

    private static DeliveryPlanStop FindOrCreateStop(DeliveryPlan plan, StopDestinationInfo? destInfo)
    {
        if (destInfo == null)
        {
            destInfo = new StopDestinationInfo();
        }

        var stop = plan.Stops.FirstOrDefault(s =>
            !string.IsNullOrEmpty(destInfo.DestinationUUID) &&
            s.DestinationUUID == destInfo.DestinationUUID);

        if (stop == null)
        {
            stop = plan.Stops.FirstOrDefault(s =>
                !string.IsNullOrEmpty(destInfo.ColonyUUID) &&
                s.ColonyUUID == destInfo.ColonyUUID &&
                string.IsNullOrEmpty(s.DestinationUUID));
        }

        if (stop == null)
        {
            stop = new DeliveryPlanStop
            {
                ColonyUUID = destInfo.ColonyUUID ?? string.Empty,
                Sequence = destInfo.Sequence,
                DestinationType = destInfo.DestinationType,
                DestinationUUID = destInfo.DestinationUUID ?? string.Empty,
            };
            plan.Stops.Add(stop);
        }

        return stop;
    }

    private static DeliveryPlanStop? FindStop(DeliveryPlan plan, StopDestinationInfo? destInfo)
    {
        if (destInfo == null)
        {
            return null;
        }

        var stop = plan.Stops.FirstOrDefault(s =>
            !string.IsNullOrEmpty(destInfo.DestinationUUID) &&
            s.DestinationUUID == destInfo.DestinationUUID);

        if (stop == null)
        {
            stop = plan.Stops.FirstOrDefault(s =>
                !string.IsNullOrEmpty(destInfo.ColonyUUID) &&
                s.ColonyUUID == destInfo.ColonyUUID &&
                string.IsNullOrEmpty(s.DestinationUUID));
        }

        return stop;
    }

    private static List<DeliveryPlan> SplitPlanIntoTrips(DeliveryPlan plan, decimal cargoCapacity)
    {
        // Collect all items across all stops with their quantities
        var allItems = new List<DeliveryItem>();
        foreach (var stop in plan.Stops.OrderBy(s => s.Sequence))
        {
            foreach (var item in stop.DropOff)
            {
                allItems.Add(item);
            }
        }

        if (allItems.Count == 0)
        {
            return new List<DeliveryPlan>();
        }

        // Split items into trips based on cargo capacity (quantity-based)
        var trips = new List<List<DeliveryItem>>();
        var currentTrip = new List<DeliveryItem>();
        decimal currentVolume = 0m;

        foreach (var item in allItems)
        {
            decimal itemVolume = item.Quantity;
            if (currentVolume + itemVolume > cargoCapacity && currentTrip.Count > 0)
            {
                trips.Add(currentTrip);
                currentTrip = new List<DeliveryItem>();
                currentVolume = 0m;
            }

            currentTrip.Add(new DeliveryItem
            {
                ItemType = item.ItemType,
                BaseItemTypeID = item.BaseItemTypeID,
                Name = item.Name,
                ResourcePurity = item.ResourcePurity,
                Quantity = item.Quantity,
            });
            currentVolume += itemVolume;
        }

        if (currentTrip.Count > 0)
        {
            trips.Add(currentTrip);
        }

        // Create new plans for each trip
        var newPlans = new List<DeliveryPlan>();
        for (int i = 0; i < trips.Count; i++)
        {
            var newPlan = new DeliveryPlan
            {
                UUID = Guid.NewGuid().ToString(),
                Name = string.Format("{0} (Trip {1})", plan.Name, i + 1),
                OwnerUUID = plan.OwnerUUID,
                RouteUUID = plan.RouteUUID,
                ShipUUID = plan.ShipUUID,
            };

            // Rebuild stops structure with the trip's items
            foreach (var stop in plan.Stops.OrderBy(s => s.Sequence))
            {
                var newStop = new DeliveryPlanStop
                {
                    ColonyUUID = stop.ColonyUUID,
                    Sequence = stop.Sequence,
                    DestinationType = stop.DestinationType,
                    DestinationUUID = stop.DestinationUUID,
                };

                foreach (var dropItem in stop.DropOff)
                {
                    var tripItem = trips[i].FirstOrDefault(t =>
                        t.ItemType == dropItem.ItemType &&
                        t.BaseItemTypeID == dropItem.BaseItemTypeID &&
                        t.ResourcePurity == dropItem.ResourcePurity &&
                        t.Quantity > 0);
                    if (tripItem != null)
                    {
                        int qty = Math.Min(tripItem.Quantity, dropItem.Quantity);
                        newStop.DropOff.Add(new DeliveryItem
                        {
                            ItemType = dropItem.ItemType,
                            BaseItemTypeID = dropItem.BaseItemTypeID,
                            Name = dropItem.Name,
                            ResourcePurity = dropItem.ResourcePurity,
                            Quantity = qty,
                        });
                        tripItem.Quantity -= qty;
                    }
                }

                foreach (var pickItem in stop.PickUp)
                {
                    newStop.PickUp.Add(new DeliveryItem
                    {
                        ItemType = pickItem.ItemType,
                        BaseItemTypeID = pickItem.BaseItemTypeID,
                        Name = pickItem.Name,
                        ResourcePurity = pickItem.ResourcePurity,
                        Quantity = pickItem.Quantity,
                    });
                }

                if (newStop.DropOff.Count > 0 || newStop.PickUp.Count > 0)
                {
                    newPlan.Stops.Add(newStop);
                }
            }

            newPlans.Add(newPlan);
        }

        return newPlans;
    }

    private async Task<IResult> HandleAddItem(
        string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage, bool isDropOff)
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

        var plan = await storage.GetDeliveryPlanAsync(uuid, entityUuid);
        if (plan == null)
        {
            return Results.NotFound(new { error = "DeliveryPlan not found" });
        }

        AddItemRequest? dto;
        try
        {
            dto = await ctx.Request.ReadFromJsonAsync<AddItemRequest>();
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        if (dto == null)
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        var stop = FindOrCreateStop(plan, dto.DestInfo);
        var item = new DeliveryItem
        {
            ItemType = dto.ItemInfo?.ItemType ?? OE2EmpireTracker.Models.ItemType.ItemTypeEnum.None,
            BaseItemTypeID = dto.ItemInfo?.BaseItemTypeID ?? string.Empty,
            Name = dto.ItemInfo?.Name ?? string.Empty,
            Quantity = dto.ItemInfo?.Quantity ?? 0,
            ResourcePurity = dto.ItemInfo?.ResourcePurity ?? string.Empty,
        };

        if (isDropOff)
        {
            stop.DropOff.Add(item);
        }
        else
        {
            stop.PickUp.Add(item);
        }

        await storage.UpsertDeliveryPlanAsync(uuid, plan);

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

        return Results.Ok(plan);
    }

    private async Task<IResult> HandleRemoveItems(
        string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage, bool isDropOff)
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

        var plan = await storage.GetDeliveryPlanAsync(uuid, entityUuid);
        if (plan == null)
        {
            return Results.NotFound(new { error = "DeliveryPlan not found" });
        }

        RemoveItemsRequest? dto;
        try
        {
            dto = await ctx.Request.ReadFromJsonAsync<RemoveItemsRequest>();
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        if (dto == null)
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        var stop = FindStop(plan, dto.DestInfo);
        if (stop != null && dto.Indices != null)
        {
            var list = isDropOff ? stop.DropOff : stop.PickUp;
            foreach (int idx in dto.Indices.OrderByDescending(i => i))
            {
                if (idx >= 0 && idx < list.Count)
                {
                    list.RemoveAt(idx);
                }
            }
        }

        await storage.UpsertDeliveryPlanAsync(uuid, plan);

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

        return Results.Ok(plan);
    }
}

/// <summary>
/// Request DTO for adding a drop-off or pick-up item to a delivery plan stop.
/// </summary>
public class AddItemRequest
{
    /// <summary>Gets or sets the destination info identifying the stop.</summary>
    public StopDestinationInfo? DestInfo { get; set; }

    /// <summary>Gets or sets the item info to add.</summary>
    public DeliveryItemInfo? ItemInfo { get; set; }
}

/// <summary>
/// Request DTO for removing drop-off or pick-up items by index from a delivery plan stop.
/// </summary>
public class RemoveItemsRequest
{
    /// <summary>Gets or sets the destination info identifying the stop.</summary>
    public StopDestinationInfo? DestInfo { get; set; }

    /// <summary>Gets or sets the indices of items to remove.</summary>
    public List<int>? Indices { get; set; }
}

/// <summary>
/// Request DTO for marking a delivery item as delivered or undelivered.
/// </summary>
public class MarkDeliveredRequest
{
    /// <summary>Gets or sets the stop sequence number.</summary>
    public int StopSequence { get; set; }

    /// <summary>Gets or sets the item index within the list.</summary>
    public int ItemIndex { get; set; }

    /// <summary>Gets or sets the list type ("dropOff" or "pickUp").</summary>
    public string? ListType { get; set; }

    /// <summary>Gets or sets a value indicating whether the item is delivered.</summary>
    public bool Delivered { get; set; }
}

/// <summary>
/// Request DTO for marking a stop as complete.
/// </summary>
public class MarkStopCompleteRequest
{
    /// <summary>Gets or sets the stop sequence number.</summary>
    public int StopSequence { get; set; }
}

/// <summary>
/// Request DTO for assigning a ship to a delivery plan.
/// </summary>
public class AssignShipRequest
{
    /// <summary>Gets or sets the ship UUID to assign.</summary>
    public string? ShipUUID { get; set; }
}

/// <summary>
/// Request DTO for splitting a delivery plan into multiple trips.
/// </summary>
public class SplitTripsRequest
{
    /// <summary>Gets or sets the cargo capacity per trip.</summary>
    public decimal CargoCapacity { get; set; }
}

/// <summary>
/// Extension methods for registering DeliveryPlan endpoints.
/// </summary>
public static class DeliveryPlanEndpointsExtensions
{
    /// <summary>
    /// Maps the DeliveryPlan CRUD endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapDeliveryPlanEndpoints(this WebApplication app)
    {
        var endpoints = new DeliveryPlanEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/delivery-plans")
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
        group.MapPost("/{entityUuid}/drop-off", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleAddDropOff(uuid, entityUuid, ctx, storage));
        group.MapPost("/{entityUuid}/pick-up", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleAddPickUp(uuid, entityUuid, ctx, storage));
        group.MapDelete("/{entityUuid}/drop-off", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleRemoveDropOff(uuid, entityUuid, ctx, storage));
        group.MapDelete("/{entityUuid}/pick-up", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleRemovePickUp(uuid, entityUuid, ctx, storage));
        group.MapPut("/{entityUuid}/mark-delivered", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleMarkDelivered(uuid, entityUuid, ctx, storage));
        group.MapPut("/{entityUuid}/mark-stop-complete", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleMarkStopComplete(uuid, entityUuid, ctx, storage));
        group.MapPut("/{entityUuid}/mark-complete", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleMarkComplete(uuid, entityUuid, ctx, storage));
        group.MapPut("/{entityUuid}/ship", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleAssignShip(uuid, entityUuid, ctx, storage));
        group.MapPost("/{entityUuid}/split-trips", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleSplitTrips(uuid, entityUuid, ctx, storage));
    }
}
