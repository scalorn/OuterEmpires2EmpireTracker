using System.Security.Claims;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// CRUD endpoints for faction-scoped clearance levels.
/// </summary>
public static class FactionClearanceLevelEndpoints
{
    public static void MapFactionClearanceLevelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/factions/{uuid}/clearance-levels")
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
        var faction = await storage.GetFactionAsync(uuid);
        if (faction == null)
        {
            return Results.NotFound(new { error = "Faction not found" });
        }

        if (!IsOwnerOrFactionLeader(httpContext, faction))
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "name is required" });
        }

        var level = new FactionClearanceLevel
        {
            UUID = Guid.NewGuid().ToString(),
            FactionUUID = uuid,
            Level = request.Level,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
        };

        await storage.UpsertFactionClearanceLevelAsync(level);

        return Results.Created($"/api/v1/factions/{uuid}/clearance-levels/{level.UUID}", level);
    }

    private static async Task<IResult> GetClearanceLevels(
        string uuid,
        IStorageBackend storage)
    {
        var faction = await storage.GetFactionAsync(uuid);
        if (faction == null)
        {
            return Results.NotFound(new { error = "Faction not found" });
        }

        var levels = await storage.GetFactionClearanceLevelsAsync(uuid);
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
        var faction = await storage.GetFactionAsync(uuid);
        if (faction == null)
        {
            return Results.NotFound(new { error = "Faction not found" });
        }

        if (!IsOwnerOrFactionLeader(httpContext, faction))
        {
            return Results.Forbid();
        }

        var levels = await storage.GetFactionClearanceLevelsAsync(uuid);
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

        await storage.UpsertFactionClearanceLevelAsync(level);

        return Results.Ok(level);
    }

    private static async Task<IResult> DeleteClearanceLevel(
        string uuid,
        string levelId,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        var faction = await storage.GetFactionAsync(uuid);
        if (faction == null)
        {
            return Results.NotFound(new { error = "Faction not found" });
        }

        if (!IsOwnerOrFactionLeader(httpContext, faction))
        {
            return Results.Forbid();
        }

        var levels = await storage.GetFactionClearanceLevelsAsync(uuid);
        var level = levels.FirstOrDefault(l => l.UUID == levelId);
        if (level == null)
        {
            return Results.NotFound(new { error = "Clearance level not found" });
        }

        await storage.DeleteFactionClearanceLevelAsync(uuid, levelId);

        return Results.NoContent();
    }

    private static bool IsOwner(HttpContext httpContext)
    {
        return httpContext.User.IsInRole(TokenRole.Owner.ToString());
    }

    private static bool IsOwnerOrFactionLeader(HttpContext httpContext, ServerFaction faction)
    {
        if (IsOwner(httpContext))
        {
            return true;
        }

        var callerCharUUID = httpContext.User.FindFirstValue("CharacterUUID");
        return callerCharUUID != null && faction.LeaderCharacterUUIDs.Contains(callerCharUUID);
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
