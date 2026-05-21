using System.Security.Claims;

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
}
