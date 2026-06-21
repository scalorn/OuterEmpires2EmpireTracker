using System.Security.Claims;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// Endpoints for faction member clearance, individual capabilities, and member listing.
/// </summary>
public static class FactionMemberEndpoints
{
    public static void MapFactionMemberEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/factions/{uuid}/members")
            .RequireAuthorization("Authenticated");

        group.MapGet("/", GetMembers);
        group.MapPut("/{charUUID}/clearance", SetClearance);
        group.MapPut("/{charUUID}/capabilities", GrantCapability);
        group.MapDelete("/{charUUID}/capabilities/{capId}", RevokeCapability);
    }

    private static async Task<IResult> GetMembers(
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

        var membersPerms = await storage.GetAllFactionMembersPermissionsAsync(uuid);
        var levels = await storage.GetFactionClearanceLevelsAsync(uuid);
        var groups = await storage.GetFactionGroupsAsync(uuid);

        var result = membersPerms.Select(mp =>
        {
            var level = levels.FirstOrDefault(l => l.UUID == mp.ClearanceLevelUUID);
            var permGroup = groups.FirstOrDefault(g => g.UUID == mp.GroupUUID);
            return new
            {
                mp.CharacterUUID,
                mp.FactionUUID,
                mp.GroupUUID,
                GroupName = permGroup?.Name,
                mp.ClearanceLevelUUID,
                ClearanceLevelName = level?.Name,
                ClearanceLevelValue = level?.Level,
            };
        }).ToList();

        return Results.Ok(result);
    }

    private static async Task<IResult> SetClearance(
        string uuid,
        string charUUID,
        SetClearanceRequest request,
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

        if (string.IsNullOrWhiteSpace(request.ClearanceLevelUUID))
        {
            return Results.BadRequest(new { error = "clearanceLevelUUID is required" });
        }

        var levels = await storage.GetFactionClearanceLevelsAsync(uuid);
        if (!levels.Any(l => l.UUID == request.ClearanceLevelUUID))
        {
            return Results.BadRequest(new { error = "ClearanceLevelUUID does not reference a valid clearance level" });
        }

        var memberPerms = await storage.GetFactionMemberPermissionsAsync(uuid, charUUID);
        if (memberPerms == null)
        {
            return Results.NotFound(new { error = "Member permissions not found" });
        }

        memberPerms.ClearanceLevelUUID = request.ClearanceLevelUUID;
        await storage.UpsertFactionMemberPermissionsAsync(memberPerms);

        return Results.Ok(memberPerms);
    }

    private static async Task<IResult> GrantCapability(
        string uuid,
        string charUUID,
        GrantCapabilityRequest request,
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

        if (string.IsNullOrWhiteSpace(request.CapabilityUUID))
        {
            return Results.BadRequest(new { error = "capabilityUUID is required" });
        }

        var capabilities = await storage.GetFactionCapabilitiesAsync(uuid);
        if (!capabilities.Any(c => c.UUID == request.CapabilityUUID))
        {
            return Results.BadRequest(new { error = "CapabilityUUID does not reference a valid faction capability" });
        }

        var memberPerms = await storage.GetFactionMemberPermissionsAsync(uuid, charUUID);
        if (memberPerms == null)
        {
            return Results.NotFound(new { error = "Member permissions not found" });
        }

        var item = new FactionMemberCapability
        {
            CharacterUUID = charUUID,
            FactionUUID = uuid,
            CapabilityUUID = request.CapabilityUUID,
        };

        await storage.AddFactionMemberCapabilityAsync(item);

        return Results.Ok(item);
    }

    private static async Task<IResult> RevokeCapability(
        string uuid,
        string charUUID,
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

        var memberPerms = await storage.GetFactionMemberPermissionsAsync(uuid, charUUID);
        if (memberPerms == null)
        {
            return Results.NotFound(new { error = "Member permissions not found" });
        }

        var memberCaps = await storage.GetFactionMemberCapabilitiesAsync(uuid, charUUID);
        if (!memberCaps.Any(mc => mc.CapabilityUUID == capId))
        {
            return Results.NotFound(new { error = "Capability not found for this member" });
        }

        await storage.RemoveFactionMemberCapabilityAsync(uuid, charUUID, capId);

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

    /// <summary>Request body for setting a member's clearance level.</summary>
    public class SetClearanceRequest
    {
        public string? ClearanceLevelUUID { get; set; }
    }

    /// <summary>Request body for granting a capability to a member.</summary>
    public class GrantCapabilityRequest
    {
        public string? CapabilityUUID { get; set; }
    }
}
