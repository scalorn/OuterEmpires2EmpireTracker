using System.Security.Claims;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// CRUD endpoints for faction-scoped capabilities.
/// </summary>
public static class FactionCapabilityEndpoints
{
    public static void MapFactionCapabilityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/factions/{uuid}/capabilities")
            .RequireAuthorization("Authenticated");

        group.MapPost("/", CreateCapability);
        group.MapGet("/", GetCapabilities);
        group.MapPut("/{capId}", UpdateCapability);
        group.MapDelete("/{capId}", DeleteCapability);
    }

    private static async Task<IResult> CreateCapability(
        string uuid,
        CreateCapabilityRequest request,
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

        var existing = await storage.GetFactionCapabilitiesAsync(uuid);
        if (existing.Any(c => c.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return Results.Conflict(new { error = "A capability with this name already exists" });
        }

        var capability = new FactionCapability
        {
            UUID = Guid.NewGuid().ToString(),
            FactionUUID = uuid,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
        };

        await storage.UpsertFactionCapabilityAsync(capability);

        return Results.Created($"/api/v1/factions/{uuid}/capabilities/{capability.UUID}", capability);
    }

    private static async Task<IResult> GetCapabilities(
        string uuid,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        var faction = await storage.GetFactionAsync(uuid);
        if (faction == null)
        {
            return Results.NotFound(new { error = "Faction not found" });
        }

        if (!AuthorizationHelper.IsOwner(httpContext))
        {
            var callerUUID = AuthorizationHelper.GetCallerCharacterUUID(httpContext);
            if (string.IsNullOrEmpty(callerUUID) || !await AuthorizationHelper.IsFactionMember(callerUUID, uuid, storage))
            {
                return Results.Json(new { error = "Access denied" }, statusCode: 403);
            }
        }

        var capabilities = await storage.GetFactionCapabilitiesAsync(uuid);
        return Results.Ok(capabilities);
    }

    private static async Task<IResult> UpdateCapability(
        string uuid,
        string capId,
        UpdateCapabilityRequest request,
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

        var capabilities = await storage.GetFactionCapabilitiesAsync(uuid);
        var capability = capabilities.FirstOrDefault(c => c.UUID == capId);
        if (capability == null)
        {
            return Results.NotFound(new { error = "Capability not found" });
        }

        if (request.Name != null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { error = "name cannot be empty" });
            }

            var duplicate = capabilities.Any(c =>
                c.UUID != capId &&
                c.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase));
            if (duplicate)
            {
                return Results.Conflict(new { error = "A capability with this name already exists" });
            }

            capability.Name = request.Name;
        }

        if (request.Description != null)
        {
            capability.Description = request.Description;
        }

        await storage.UpsertFactionCapabilityAsync(capability);

        return Results.Ok(capability);
    }

    private static async Task<IResult> DeleteCapability(
        string uuid,
        string capId,
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

        var capabilities = await storage.GetFactionCapabilitiesAsync(uuid);
        var capability = capabilities.FirstOrDefault(c => c.UUID == capId);
        if (capability == null)
        {
            return Results.NotFound(new { error = "Capability not found" });
        }

        // Cascade: remove from all groups
        var groups = await storage.GetFactionGroupsAsync(uuid);
        foreach (var group in groups)
        {
            var groupCaps = await storage.GetFactionGroupCapabilitiesAsync(group.UUID);
            if (groupCaps.Any(gc => gc.CapabilityUUID == capId))
            {
                await storage.RemoveFactionGroupCapabilityAsync(group.UUID, capId);
            }
        }

        // Cascade: remove from all member individual capabilities
        var members = await storage.GetAllFactionMembersPermissionsAsync(uuid);
        foreach (var member in members)
        {
            var memberCaps = await storage.GetFactionMemberCapabilitiesAsync(uuid, member.CharacterUUID);
            if (memberCaps.Any(mc => mc.CapabilityUUID == capId))
            {
                await storage.RemoveFactionMemberCapabilityAsync(uuid, member.CharacterUUID, capId);
            }
        }

        await storage.DeleteFactionCapabilityAsync(uuid, capId);

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

    /// <summary>Request body for creating a capability.</summary>
    public class CreateCapabilityRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>Request body for updating a capability.</summary>
    public class UpdateCapabilityRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}
