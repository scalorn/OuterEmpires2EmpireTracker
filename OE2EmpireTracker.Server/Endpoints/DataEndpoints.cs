using System.Security.Claims;
using System.Text.Json;
using OE2EmpireTracker.Server.Push;
using OE2EmpireTracker.Server.Storage;

using OE2EmpireTracker.Services;
namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// Character data CRUD, global/baseline data, sync, and export endpoints.
/// </summary>
public static class DataEndpoints
{
    public static void MapDataEndpoints(this WebApplication app)
    {
        // Character data CRUD
        var charData = app.MapGroup("/api/v1/characters/{uuid}/data")
            .RequireAuthorization("Authenticated");

        charData.MapGet("/{dataType}/{entityUuid}", GetCharacterEntity);
        charData.MapPut("/{dataType}/{entityUuid}", UpdateCharacterEntity);
        charData.MapDelete("/{dataType}/{entityUuid}", DeleteCharacterEntity);
        charData.MapGet("/{dataType}", GetCharacterDataCollection);
        charData.MapPut("/{dataType}", PutCharacterDataCollection);
        charData.MapPost("/{dataType}", CreateCharacterEntity);
        charData.MapGet("/", GetAllCharacterData);
        charData.MapPut("/", PutAllCharacterData);

        // Global/baseline data
        var global = app.MapGroup("/api/v1/global")
            .RequireAuthorization("Authenticated");

        global.MapGet("/{dataType}", GetGlobalData);
        global.MapPut("/{dataType}", PutGlobalData);

        // Sync
        app.MapGet("/api/v1/sync", GetSync)
            .RequireAuthorization("Authenticated");

        // Export
        app.MapGet("/api/v1/characters/{uuid}/export", ExportCharacterData)
            .RequireAuthorization("Authenticated");
    }

    // --- Character Data CRUD ---

    private static async Task<IResult> PutCharacterDataCollection(
        string uuid,
        string dataType,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!CanAccessCharacterData(httpContext, uuid))
        {
            return Results.Forbid();
        }

        var body = await ReadBodyAsStringAsync(httpContext);
        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        await storage.UpsertCharacterDataAsync(uuid, dataType, body);

        LogMutation(httpContext, "Updated", dataType, uuid);
        await DispatchDataEvent(httpContext, ServerEventType.Updated, dataType, uuid, uuid);

        return Results.Ok(JsonDocument.Parse(body).RootElement);
    }

    private static async Task<IResult> GetCharacterDataCollection(
        string uuid,
        string dataType,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!CanAccessCharacterData(httpContext, uuid))
        {
            return Results.Forbid();
        }

        var json = await storage.GetCharacterDataAsync(uuid, dataType);
        if (json == null)
        {
            return Results.Ok(JsonDocument.Parse("[]").RootElement);
        }

        return Results.Content(json, "application/json");
    }

    private static async Task<IResult> PutCharacterDataCollection(
        string uuid,
        string dataType,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!CanAccessCharacterData(httpContext, uuid))
        {
            return Results.Forbid();
        }

        var body = await ReadBodyAsStringAsync(httpContext);
        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        await storage.UpsertCharacterDataAsync(uuid, dataType, body);

        LogMutation(httpContext, "Updated", dataType, uuid);
        await DispatchDataEvent(httpContext, ServerEventType.Updated, dataType, uuid, uuid);

        return Results.Ok(JsonDocument.Parse(body).RootElement);
    }

    private static async Task<IResult> CreateCharacterEntity(
        string uuid,
        string dataType,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!CanAccessCharacterData(httpContext, uuid))
        {
            return Results.Forbid();
        }

        var body = await ReadBodyAsStringAsync(httpContext);
        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        // Parse UUID from body JSON
        string? entityUuid;
        try
        {
            var doc = JsonDocument.Parse(body);
            entityUuid = doc.RootElement.TryGetProperty("UUID", out var uuidProp)
                ? uuidProp.GetString()
                : null;
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid JSON body" });
        }

        if (string.IsNullOrWhiteSpace(entityUuid))
        {
            return Results.BadRequest(new { error = "Body must contain a UUID field" });
        }

        await storage.UpsertCharacterEntityAsync(uuid, dataType, entityUuid, body);

        LogMutation(httpContext, "Created", dataType, entityUuid);
        await DispatchDataEvent(httpContext, ServerEventType.Created, dataType, entityUuid, uuid);

        return Results.Created(
            $"/api/v1/characters/{uuid}/data/{dataType}/{entityUuid}",
            JsonDocument.Parse(body).RootElement);
    }

    private static async Task<IResult> GetCharacterEntity(
        string uuid,
        string dataType,
        string entityUuid,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!CanAccessCharacterData(httpContext, uuid))
        {
            return Results.Forbid();
        }

        var json = await storage.GetCharacterEntityAsync(uuid, dataType, entityUuid);
        if (json == null)
        {
            return Results.NotFound(new { error = "Entity not found" });
        }

        return Results.Content(json, "application/json");
    }

    private static async Task<IResult> UpdateCharacterEntity(
        string uuid,
        string dataType,
        string entityUuid,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!CanAccessCharacterData(httpContext, uuid))
        {
            return Results.Forbid();
        }

        var body = await ReadBodyAsStringAsync(httpContext);
        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        // Validate JSON
        try
        {
            JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid JSON body" });
        }

        await storage.UpsertCharacterEntityAsync(uuid, dataType, entityUuid, body);

        LogMutation(httpContext, "Updated", dataType, entityUuid);
        await DispatchDataEvent(httpContext, ServerEventType.Updated, dataType, entityUuid, uuid);

        return Results.Ok(JsonDocument.Parse(body).RootElement);
    }

    private static async Task<IResult> DeleteCharacterEntity(
        string uuid,
        string dataType,
        string entityUuid,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!CanAccessCharacterData(httpContext, uuid))
        {
            return Results.Forbid();
        }

        await storage.DeleteCharacterEntityAsync(uuid, dataType, entityUuid);

        LogMutation(httpContext, "Deleted", dataType, entityUuid);
        await DispatchDataEvent(httpContext, ServerEventType.Deleted, dataType, entityUuid, uuid);

        return Results.NoContent();
    }

    private static async Task<IResult> GetAllCharacterData(
        string uuid,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!CanAccessCharacterData(httpContext, uuid))
        {
            return Results.Forbid();
        }

        var json = await storage.GetAllCharacterDataAsync(uuid);
        if (json == null)
        {
            return Results.Ok(JsonDocument.Parse("{}").RootElement);
        }

        return Results.Content(json, "application/json");
    }

    private static async Task<IResult> PutAllCharacterData(
        string uuid,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!CanAccessCharacterData(httpContext, uuid))
        {
            return Results.Forbid();
        }

        var body = await ReadBodyAsStringAsync(httpContext);
        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        // Validate JSON
        try
        {
            JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid JSON body" });
        }

        await storage.PutAllCharacterDataAsync(uuid, body);

        LogMutation(httpContext, "BulkUpdated", "AllData", uuid);
        await DispatchDataEvent(httpContext, ServerEventType.Updated, "AllData", uuid, uuid);

        return Results.Ok(JsonDocument.Parse(body).RootElement);
    }

    // --- Global/Baseline Data ---

    private static async Task<IResult> GetGlobalData(
        string dataType,
        IStorageBackend storage)
    {
        var json = await storage.GetGlobalDataAsync(dataType);
        if (json == null)
        {
            return Results.NotFound(new { error = "Global data type not found" });
        }

        return Results.Content(json, "application/json");
    }

    private static async Task<IResult> PutGlobalData(
        string dataType,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!IsOwner(httpContext))
        {
            return Results.Forbid();
        }

        var body = await ReadBodyAsStringAsync(httpContext);
        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        // Validate JSON
        try
        {
            JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid JSON body" });
        }

        await storage.UpsertGlobalDataAsync(dataType, body);

        LogMutation(httpContext, "Updated", $"Global/{dataType}", dataType);

        return Results.Ok(JsonDocument.Parse(body).RootElement);
    }

    // --- Sync ---

    private static async Task<IResult> GetSync(IStorageBackend storage)
    {
        var factions = await storage.GetAllFactionsAsync();
        var characters = await storage.GetAllCharactersAsync();

        var result = new
        {
            factions,
            characters,
            serverTimestamp = SystemClock.UtcNow,
        };

        return Results.Ok(result);
    }

    // --- Export ---

    private static async Task<IResult> ExportCharacterData(
        string uuid,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!CanAccessCharacterData(httpContext, uuid))
        {
            return Results.Forbid();
        }

        var json = await storage.GetAllCharacterDataAsync(uuid);
        if (json == null)
        {
            return Results.Ok(JsonDocument.Parse("{}").RootElement);
        }

        return Results.Content(json, "application/json");
    }

    // --- Helpers ---

    private static bool IsOwner(HttpContext httpContext)
    {
        return httpContext.User.IsInRole(TokenRole.Owner.ToString());
    }

    private static bool CanAccessCharacterData(HttpContext httpContext, string characterUuid)
    {
        if (IsOwner(httpContext))
        {
            return true;
        }

        var callerCharUUID = httpContext.User.FindFirstValue("CharacterUUID");
        return callerCharUUID == characterUuid;
    }

    private static async Task<string> ReadBodyAsStringAsync(HttpContext httpContext)
    {
        using var reader = new StreamReader(httpContext.Request.Body);
        return await reader.ReadToEndAsync();
    }

    private static void LogMutation(HttpContext httpContext, string action, string entityType, string uuid)
    {
        var tokenId = httpContext.User.FindFirstValue("TokenId") ?? "unknown";
        var remoteIp = httpContext.Connection.RemoteIpAddress;
        var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DataEndpoints");

        logger.LogInformation(
            "Mutation: {Action} {EntityType}/{UUID} by token {TokenId} from {IP}",
            action,
            entityType,
            uuid,
            tokenId,
            remoteIp);
    }

    private static async Task DispatchDataEvent(
        HttpContext httpContext,
        ServerEventType eventType,
        string dataType,
        string entityUuid,
        string ownerCharacterUuid)
    {
        var dispatcher = httpContext.RequestServices.GetRequiredService<EventDispatcher>();
        await dispatcher.DispatchEvent(new ServerEvent
        {
            EventType = eventType,
            EntityType = dataType,
            EntityUUID = entityUuid,
            OwnerCharacterUUID = ownerCharacterUuid,
        });
    }
}
