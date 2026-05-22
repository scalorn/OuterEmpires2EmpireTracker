using System.Security.Claims;
using System.Text.Json;

using OE2EmpireTracker.Server.Push;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Abstract base class for typed CRUD endpoints. Provides shared handler
/// implementations for GET, POST, PUT, DELETE operations with authorization,
/// validation, event dispatch, and rollback semantics.
/// </summary>
/// <typeparam name="TEntity">The domain entity type (e.g. Colony).</typeparam>
/// <typeparam name="TCreate">The create request DTO type.</typeparam>
/// <typeparam name="TUpdate">The update request DTO type.</typeparam>
public abstract class TypedEndpointBase<TEntity, TCreate, TUpdate>
    where TEntity : class
    where TCreate : class
    where TUpdate : class
{
    /// <summary>
    /// Gets the display name of the entity type (e.g. "Colony").
    /// Used in error messages and logging.
    /// </summary>
    protected abstract string EntityTypeName { get; }

    /// <summary>
    /// Gets the route prefix for this entity type (e.g. "colonies").
    /// Used for Location header construction.
    /// </summary>
    protected abstract string RoutePrefix { get; }

    /// <summary>
    /// Handles GET requests for all entities of this type belonging to a character.
    /// Validates the character UUID, checks authorization, retrieves all entities
    /// from storage, and applies pagination if requested.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="limit">Optional maximum number of items to return.</param>
    /// <param name="offset">Optional number of items to skip.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the entity collection or an error response.</returns>
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

        var items = await GetAllFromStorage(uuid, storage);
        return PaginationHelper.ApplyPagination(items, limit, offset);
    }

    /// <summary>
    /// Handles GET requests for a single entity by UUID.
    /// Validates both UUIDs, checks authorization, retrieves the entity
    /// from storage, and returns 404 if not found.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="entityUuid">The entity UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the entity or an error response.</returns>
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

        var entity = await GetFromStorage(uuid, entityUuid, storage);
        if (entity == null)
        {
            return Results.NotFound(new { error = $"{EntityTypeName} not found" });
        }

        return Results.Ok(entity);
    }

    /// <summary>
    /// Handles POST requests to create a new entity.
    /// Validates the character UUID, checks authorization, reads and validates
    /// the request body, checks for duplicates, persists the entity, logs the
    /// mutation, dispatches an event, and returns 201 with a Location header.
    /// Rolls back on logging or event dispatch failure.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the created entity or an error response.</returns>
    public async Task<IResult> HandleCreate(string uuid, HttpContext ctx, IStorageBackend storage)
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

        TCreate? dto;
        try
        {
            dto = await ctx.Request.ReadFromJsonAsync<TCreate>();
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        if (dto == null)
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        var validationError = ValidateCreate(dto);
        if (validationError != null)
        {
            return Results.BadRequest(new { error = validationError });
        }

        var dedupResult = await HandleCreateDedup(uuid, dto, storage);
        if (dedupResult != null)
        {
            return dedupResult;
        }

        var entity = ApplyCreate(dto);
        var entityUuid = GetEntityUuid(entity);

        await UpsertToStorage(uuid, entity, storage);

        try
        {
            LogMutation(ctx, "Created", entityUuid);
        }
        catch (Exception)
        {
            // Rollback: delete the newly created entity
            await DeleteFromStorage(uuid, entityUuid, storage);
            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
        }

        try
        {
            await DispatchEvent(ctx, ServerEventType.Created, entityUuid, uuid);
        }
        catch (Exception)
        {
            // Rollback: delete the newly created entity
            await DeleteFromStorage(uuid, entityUuid, storage);
            return Results.Json(new { error = "Event system temporarily unavailable" }, statusCode: 503);
        }

        var location = $"/api/v1/characters/{uuid}/{RoutePrefix}/{entityUuid}";
        return Results.Created(location, entity);
    }

    /// <summary>
    /// Handles PUT requests to update an existing entity.
    /// Validates both UUIDs, checks authorization, reads and validates
    /// the request body, retrieves the existing entity, applies the update,
    /// persists, logs, and dispatches an event. Rolls back on failure.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="entityUuid">The entity UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the updated entity or an error response.</returns>
    public async Task<IResult> HandleUpdate(
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

        if (!HasJsonContentType(ctx))
        {
            return Results.Json(new { error = "Unsupported media type" }, statusCode: 415);
        }

        TUpdate? dto;
        try
        {
            dto = await ctx.Request.ReadFromJsonAsync<TUpdate>();
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        if (dto == null)
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        var validationError = ValidateUpdate(dto);
        if (validationError != null)
        {
            return Results.BadRequest(new { error = validationError });
        }

        var existing = await GetFromStorage(uuid, entityUuid, storage);
        if (existing == null)
        {
            return Results.NotFound(new { error = $"{EntityTypeName} not found" });
        }

        var previousState = existing;
        var updated = ApplyUpdate(existing, dto);

        await UpsertToStorage(uuid, updated, storage);

        try
        {
            LogMutation(ctx, "Updated", entityUuid);
        }
        catch (Exception)
        {
            // Rollback: restore previous state
            await UpsertToStorage(uuid, previousState, storage);
            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
        }

        try
        {
            await DispatchEvent(ctx, ServerEventType.Updated, entityUuid, uuid);
        }
        catch (Exception)
        {
            // Rollback: restore previous state
            await UpsertToStorage(uuid, previousState, storage);
            return Results.Json(new { error = "Event system temporarily unavailable" }, statusCode: 503);
        }

        return Results.Ok(updated);
    }

    /// <summary>
    /// Handles DELETE requests to remove an entity.
    /// Validates both UUIDs, checks authorization, retrieves the entity
    /// (for rollback), deletes from storage, logs, and dispatches an event.
    /// Rolls back on failure by re-upserting the deleted entity.
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

        var existing = await GetFromStorage(uuid, entityUuid, storage);
        if (existing == null)
        {
            return Results.NotFound(new { error = $"{EntityTypeName} not found" });
        }

        await DeleteFromStorage(uuid, entityUuid, storage);

        try
        {
            LogMutation(ctx, "Deleted", entityUuid);
        }
        catch (Exception)
        {
            // Rollback: re-upsert the deleted entity
            await UpsertToStorage(uuid, existing, storage);
            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
        }

        try
        {
            await DispatchEvent(ctx, ServerEventType.Deleted, entityUuid, uuid);
        }
        catch (Exception)
        {
            // Rollback: re-upsert the deleted entity
            await UpsertToStorage(uuid, existing, storage);
            return Results.Json(new { error = "Event system temporarily unavailable" }, statusCode: 503);
        }

        return Results.NoContent();
    }

    /// <summary>
    /// Checks whether the current request is authorized to access the
    /// specified character's data.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="characterUuid">The target character UUID from the URL.</param>
    /// <returns>True if access is allowed; false otherwise.</returns>
    protected static bool CanAccessCharacterData(HttpContext httpContext, string characterUuid)
    {
        if (httpContext.User.IsInRole("Owner"))
        {
            return true;
        }

        var callerCharUUID = httpContext.User.FindFirstValue("CharacterUUID");
        return callerCharUUID == characterUuid;
    }

    /// <summary>
    /// Dispatches a server event to WebSocket clients.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="eventType">The type of event.</param>
    /// <param name="entityUuid">The UUID of the affected entity.</param>
    /// <param name="ownerCharacterUuid">The owning character's UUID.</param>
    protected static async Task DispatchEvent(
        HttpContext httpContext,
        ServerEventType eventType,
        string entityUuid,
        string ownerCharacterUuid)
    {
        var dispatcher = httpContext.RequestServices.GetRequiredService<EventDispatcher>();
        await dispatcher.DispatchEvent(new ServerEvent
        {
            EventType = eventType,
            EntityType = typeof(TEntity).Name,
            EntityUUID = entityUuid,
            OwnerCharacterUUID = ownerCharacterUuid,
        });
    }

    /// <summary>
    /// Retrieves all entities of this type for the specified character from storage.
    /// </summary>
    /// <param name="characterUUID">The owning character's UUID.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>A read-only list of all entities for the character.</returns>
    protected abstract Task<IReadOnlyList<TEntity>> GetAllFromStorage(string characterUUID, IStorageBackend storage);

    /// <summary>
    /// Retrieves a single entity by UUID from storage.
    /// </summary>
    /// <param name="characterUUID">The owning character's UUID.</param>
    /// <param name="entityUUID">The entity's UUID.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>The entity if found; null otherwise.</returns>
    protected abstract Task<TEntity?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage);

    /// <summary>
    /// Validates a create request DTO. Returns null if valid,
    /// or an error message string if validation fails.
    /// </summary>
    /// <param name="dto">The create request DTO to validate.</param>
    /// <returns>Null if valid; an error message if invalid.</returns>
    protected abstract string? ValidateCreate(TCreate dto);

    /// <summary>
    /// Validates an update request DTO. Returns null if valid,
    /// or an error message string if validation fails.
    /// </summary>
    /// <param name="dto">The update request DTO to validate.</param>
    /// <returns>Null if valid; an error message if invalid.</returns>
    protected abstract string? ValidateUpdate(TUpdate dto);

    /// <summary>
    /// Creates a new entity from the create DTO. Assigns a new UUID
    /// and populates all required fields.
    /// </summary>
    /// <param name="dto">The create request DTO.</param>
    /// <returns>The newly created entity instance.</returns>
    protected abstract TEntity ApplyCreate(TCreate dto);

    /// <summary>
    /// Merges an update DTO into an existing entity, returning the
    /// updated entity instance.
    /// </summary>
    /// <param name="existing">The current entity state.</param>
    /// <param name="dto">The update request DTO.</param>
    /// <returns>The updated entity instance.</returns>
    protected abstract TEntity ApplyUpdate(TEntity existing, TUpdate dto);

    /// <summary>
    /// Persists an entity to storage (insert or update).
    /// </summary>
    /// <param name="characterUUID">The owning character's UUID.</param>
    /// <param name="entity">The entity to persist.</param>
    /// <param name="storage">The storage backend.</param>
    protected abstract Task UpsertToStorage(string characterUUID, TEntity entity, IStorageBackend storage);

    /// <summary>
    /// Deletes an entity from storage.
    /// </summary>
    /// <param name="characterUUID">The owning character's UUID.</param>
    /// <param name="entityUUID">The UUID of the entity to delete.</param>
    /// <param name="storage">The storage backend.</param>
    protected abstract Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage);

    /// <summary>
    /// Extracts the UUID from an entity instance.
    /// </summary>
    /// <param name="entity">The entity to extract the UUID from.</param>
    /// <returns>The entity's UUID string.</returns>
    protected abstract string GetEntityUuid(TEntity entity);

    /// <summary>
    /// Hook for handling create deduplication. Override to check for
    /// existing entities that match the create request and return a
    /// merge result instead of creating a new entity.
    /// </summary>
    /// <param name="characterUUID">The owning character's UUID.</param>
    /// <param name="dto">The create request DTO.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>
    /// An <see cref="IResult"/> if a duplicate was found and handled (e.g. 200 with merged entity),
    /// or null if no duplicate exists and creation should proceed normally.
    /// </returns>
    protected virtual Task<IResult?> HandleCreateDedup(
        string characterUUID, TCreate dto, IStorageBackend storage)
    {
        return Task.FromResult<IResult?>(null);
    }

    /// <summary>
    /// Logs a mutation operation for audit purposes.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="action">The action type (Created, Updated, Deleted).</param>
    /// <param name="entityUuid">The UUID of the affected entity.</param>
    protected void LogMutation(HttpContext httpContext, string action, string entityUuid)
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

    /// <summary>
    /// Checks whether the request has a JSON content type.
    /// </summary>
    /// <param name="ctx">The current HTTP context.</param>
    /// <returns>True if the Content-Type is application/json; false otherwise.</returns>
    private static bool HasJsonContentType(HttpContext ctx)
    {
        var contentType = ctx.Request.ContentType;
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }

        return contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);
    }
}
