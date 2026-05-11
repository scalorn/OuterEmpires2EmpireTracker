using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// Faction CRUD and leadership management endpoints.
/// </summary>
public static class FactionEndpoints
{
    /// <summary>
    /// Generates a deterministic UUID from a namespace and value using SHA-256.
    /// </summary>
    public static string GenerateDeterministicUUID(string namespaceName, string value)
    {
        var input = $"{namespaceName}:{value.ToLowerInvariant()}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var guidBytes = hash[..16];
        return new Guid(guidBytes).ToString();
    }

    public static void MapFactionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/factions")
            .RequireAuthorization("Authenticated");

        group.MapPost("/", CreateFaction);
        group.MapGet("/", GetAllFactions);
        group.MapGet("/{uuid}", GetFaction);
        group.MapPut("/{uuid}", UpdateFaction);
        group.MapDelete("/{uuid}", DeleteFaction);

        // Leadership sub-endpoints
        group.MapGet("/{uuid}/leaders", GetLeaders);
        group.MapPut("/{uuid}/leaders", AddLeader);
        group.MapDelete("/{uuid}/leaders/{charUUID}", RemoveLeader);
    }

    private static async Task<IResult> CreateFaction(
        FactionRequest request,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!IsOwner(httpContext))
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "name is required" });
        }

        var uuid = GenerateDeterministicUUID("faction", request.Name);
        var existing = await storage.GetFactionAsync(uuid);
        if (existing != null)
        {
            return Results.Conflict(new { error = "Faction with this name already exists" });
        }

        var faction = new ServerFaction
        {
            UUID = uuid,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            Metadata = new EntityMetadata { LastModifiedUtc = DateTime.UtcNow },
        };

        await storage.UpsertFactionAsync(faction);

        return Results.Created($"/api/v1/factions/{uuid}", faction);
    }

    private static async Task<IResult> GetAllFactions(IStorageBackend storage)
    {
        var factions = await storage.GetAllFactionsAsync();
        return Results.Ok(factions);
    }

    private static async Task<IResult> GetFaction(string uuid, IStorageBackend storage)
    {
        var faction = await storage.GetFactionAsync(uuid);
        if (faction == null)
        {
            return Results.NotFound(new { error = "Faction not found" });
        }

        return Results.Ok(faction);
    }

    private static async Task<IResult> UpdateFaction(
        string uuid,
        FactionRequest request,
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

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            faction.Name = request.Name;
        }

        if (request.Description != null)
        {
            faction.Description = request.Description;
        }

        faction.Metadata.LastModifiedUtc = DateTime.UtcNow;
        await storage.UpsertFactionAsync(faction);

        return Results.Ok(faction);
    }

    private static async Task<IResult> DeleteFaction(
        string uuid,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!IsOwner(httpContext))
        {
            return Results.Forbid();
        }

        var faction = await storage.GetFactionAsync(uuid);
        if (faction == null)
        {
            return Results.NotFound(new { error = "Faction not found" });
        }

        // Clear FactionUUID on linked characters
        var characters = await storage.GetAllCharactersAsync();
        foreach (var character in characters.Where(c => c.FactionUUID == uuid))
        {
            character.FactionUUID = null;
            character.Metadata.LastModifiedUtc = DateTime.UtcNow;
            await storage.UpsertCharacterAsync(character);
        }

        await storage.DeleteFactionAsync(uuid);

        return Results.NoContent();
    }

    private static async Task<IResult> GetLeaders(string uuid, IStorageBackend storage)
    {
        var faction = await storage.GetFactionAsync(uuid);
        if (faction == null)
        {
            return Results.NotFound(new { error = "Faction not found" });
        }

        return Results.Ok(faction.LeaderCharacterUUIDs);
    }

    private static async Task<IResult> AddLeader(
        string uuid,
        LeaderRequest request,
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

        if (string.IsNullOrWhiteSpace(request.CharacterUUID))
        {
            return Results.BadRequest(new { error = "characterUUID is required" });
        }

        if (!faction.LeaderCharacterUUIDs.Contains(request.CharacterUUID))
        {
            faction.LeaderCharacterUUIDs.Add(request.CharacterUUID);
            faction.Metadata.LastModifiedUtc = DateTime.UtcNow;
            await storage.UpsertFactionAsync(faction);
        }

        return Results.Ok(faction.LeaderCharacterUUIDs);
    }

    private static async Task<IResult> RemoveLeader(
        string uuid,
        string charUUID,
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

        // Can't remove self
        var callerCharUUID = httpContext.User.FindFirstValue("CharacterUUID");
        if (callerCharUUID == charUUID && !IsOwner(httpContext))
        {
            return Results.BadRequest(new { error = "Cannot remove yourself as leader" });
        }

        faction.LeaderCharacterUUIDs.Remove(charUUID);
        faction.Metadata.LastModifiedUtc = DateTime.UtcNow;
        await storage.UpsertFactionAsync(faction);

        return Results.Ok(faction.LeaderCharacterUUIDs);
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

    /// <summary>Request body for faction create/update.</summary>
    public class FactionRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>Request body for adding a leader.</summary>
    public class LeaderRequest
    {
        public string? CharacterUUID { get; set; }
    }
}
