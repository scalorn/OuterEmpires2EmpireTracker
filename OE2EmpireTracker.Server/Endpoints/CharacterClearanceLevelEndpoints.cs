using System.Security.Claims;
using OE2EmpireTracker.Server.Services;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// CRUD endpoints for character-scoped clearance levels.
/// </summary>
public static class CharacterClearanceLevelEndpoints
{
    public static void MapCharacterClearanceLevelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/characters/{uuid}/clearance-levels")
            .RequireAuthorization("Authenticated");

        group.MapPost("/", CreateClearanceLevel);
        group.MapGet("/", GetClearanceLevels);
        group.MapPut("/{levelId}", UpdateClearanceLevel);
        group.MapDelete("/{levelId}", DeleteClearanceLevel);
    }

    private static async Task<IResult> CreateClearanceLevel(
        string uuid,
        CreateClearanceLevelRequest request,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        var character = await storage.GetCharacterAsync(uuid);
        if (character == null)
        {
            return Results.NotFound(new { error = "Character not found" });
        }

        if (!IsOwnerOrCharacterOwner(httpContext, uuid))
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "name is required" });
        }

        var level = new CharacterClearanceLevel
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerCharacterUUID = uuid,
            Level = request.Level,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
        };

        await storage.UpsertCharacterClearanceLevelAsync(level);

        return Results.Created($"/api/v1/characters/{uuid}/clearance-levels/{level.UUID}", level);
    }

    private static async Task<IResult> GetClearanceLevels(
        string uuid,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        var character = await storage.GetCharacterAsync(uuid);
        if (character == null)
        {
            return Results.NotFound(new { error = "Character not found" });
        }

        var callerUUID = AuthorizationHelper.GetCallerCharacterUUID(httpContext);
        if (callerUUID != uuid && !AuthorizationHelper.IsOwner(httpContext))
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        var levels = await storage.GetCharacterClearanceLevelsAsync(uuid);

        // Seed defaults on first access if none exist
        if (levels.Count == 0)
        {
            await PermissionSeedingService.SeedCharacterClearanceLevelsAsync(storage, uuid);
            levels = await storage.GetCharacterClearanceLevelsAsync(uuid);
        }

        var ordered = levels.OrderBy(l => l.Level).ToList();
        return Results.Ok(ordered);
    }

    private static async Task<IResult> UpdateClearanceLevel(
        string uuid,
        string levelId,
        UpdateClearanceLevelRequest request,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        var character = await storage.GetCharacterAsync(uuid);
        if (character == null)
        {
            return Results.NotFound(new { error = "Character not found" });
        }

        if (!IsOwnerOrCharacterOwner(httpContext, uuid))
        {
            return Results.Forbid();
        }

        var levels = await storage.GetCharacterClearanceLevelsAsync(uuid);
        var level = levels.FirstOrDefault(l => l.UUID == levelId);
        if (level == null)
        {
            return Results.NotFound(new { error = "Clearance level not found" });
        }

        if (request.Level.HasValue)
        {
            level.Level = request.Level.Value;
        }

        if (request.Name != null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { error = "name cannot be empty" });
            }

            level.Name = request.Name;
        }

        if (request.Description != null)
        {
            level.Description = request.Description;
        }

        await storage.UpsertCharacterClearanceLevelAsync(level);

        return Results.Ok(level);
    }

    private static async Task<IResult> DeleteClearanceLevel(
        string uuid,
        string levelId,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        var character = await storage.GetCharacterAsync(uuid);
        if (character == null)
        {
            return Results.NotFound(new { error = "Character not found" });
        }

        if (!IsOwnerOrCharacterOwner(httpContext, uuid))
        {
            return Results.Forbid();
        }

        var levels = await storage.GetCharacterClearanceLevelsAsync(uuid);
        var level = levels.FirstOrDefault(l => l.UUID == levelId);
        if (level == null)
        {
            return Results.NotFound(new { error = "Clearance level not found" });
        }

        await storage.DeleteCharacterClearanceLevelAsync(uuid, levelId);

        return Results.NoContent();
    }

    private static bool IsOwnerOrCharacterOwner(HttpContext httpContext, string characterUUID)
    {
        if (httpContext.User.IsInRole(TokenRole.Owner.ToString()))
        {
            return true;
        }

        var callerCharUUID = httpContext.User.FindFirstValue("CharacterUUID");
        return callerCharUUID == characterUUID;
    }

    /// <summary>Request body for creating a clearance level.</summary>
    public class CreateClearanceLevelRequest
    {
        public int Level { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>Request body for updating a clearance level.</summary>
    public class UpdateClearanceLevelRequest
    {
        public int? Level { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}
