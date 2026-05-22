using System.Security.Claims;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// CRUD endpoints for faction permission groups, member assignment, sharing rules, and capability grants.
/// </summary>
public static class FactionGroupEndpoints
{
    public static void MapFactionGroupEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/factions/{uuid}/groups")
            .RequireAuthorization("Authenticated");

        group.MapPost("/", CreateGroup);
        group.MapGet("/", GetGroups);
        group.MapPut("/{groupId}", UpdateGroup);
        group.MapDelete("/{groupId}", DeleteGroup);

        // Member management
        group.MapPut("/{groupId}/members", AssignMember);
        group.MapDelete("/{groupId}/members/{characterUUID}", RemoveMember);

        // Sharing rules
        group.MapPost("/{groupId}/sharing-rules", AddSharingRule);
        group.MapDelete("/{groupId}/sharing-rules/{ruleId}", RemoveSharingRule);

        // Capability grants
        group.MapPost("/{groupId}/capabilities", AddGroupCapability);
        group.MapDelete("/{groupId}/capabilities/{capId}", RemoveGroupCapability);
    }

    private static async Task<IResult> CreateGroup(
        string uuid,
        CreateGroupRequest request,
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

        if (string.IsNullOrWhiteSpace(request.DefaultClearanceLevelUUID))
        {
            return Results.BadRequest(new { error = "defaultClearanceLevelUUID is required" });
        }

        var levels = await storage.GetFactionClearanceLevelsAsync(uuid);
        if (!levels.Any(l => l.UUID == request.DefaultClearanceLevelUUID))
        {
            return Results.BadRequest(new { error = "DefaultClearanceLevelUUID does not reference a valid clearance level" });
        }

        var permGroup = new FactionPermissionGroup
        {
            UUID = Guid.NewGuid().ToString(),
            FactionUUID = uuid,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            DefaultClearanceLevelUUID = request.DefaultClearanceLevelUUID,
        };

        await storage.UpsertFactionGroupAsync(permGroup);

        return Results.Created($"/api/v1/factions/{uuid}/groups/{permGroup.UUID}", permGroup);
    }

    private static async Task<IResult> GetGroups(
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

        var groups = await storage.GetFactionGroupsAsync(uuid);
        return Results.Ok(groups);
    }

    private static async Task<IResult> UpdateGroup(
        string uuid,
        string groupId,
        UpdateGroupRequest request,
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

        var permGroup = await storage.GetFactionGroupAsync(uuid, groupId);
        if (permGroup == null)
        {
            return Results.NotFound(new { error = "Group not found" });
        }

        if (request.Name != null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { error = "name cannot be empty" });
            }

            permGroup.Name = request.Name;
        }

        if (request.Description != null)
        {
            permGroup.Description = request.Description;
        }

        if (request.DefaultClearanceLevelUUID != null)
        {
            var levels = await storage.GetFactionClearanceLevelsAsync(uuid);
            if (!levels.Any(l => l.UUID == request.DefaultClearanceLevelUUID))
            {
                return Results.BadRequest(new { error = "DefaultClearanceLevelUUID does not reference a valid clearance level" });
            }

            permGroup.DefaultClearanceLevelUUID = request.DefaultClearanceLevelUUID;
        }

        await storage.UpsertFactionGroupAsync(permGroup);

        return Results.Ok(permGroup);
    }

    private static async Task<IResult> DeleteGroup(
        string uuid,
        string groupId,
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

        var permGroup = await storage.GetFactionGroupAsync(uuid, groupId);
        if (permGroup == null)
        {
            return Results.NotFound(new { error = "Group not found" });
        }

        // Cascade: remove group capabilities
        var groupCaps = await storage.GetFactionGroupCapabilitiesAsync(groupId);
        foreach (var gc in groupCaps)
        {
            await storage.RemoveFactionGroupCapabilityAsync(groupId, gc.CapabilityUUID);
        }

        // Cascade: remove group sharing rules
        var sharingRules = await storage.GetFactionGroupSharingRulesAsync(groupId);
        foreach (var rule in sharingRules)
        {
            await storage.DeleteFactionGroupSharingRuleAsync(groupId, rule.UUID);
        }

        // Cascade: clear GroupUUID from members in this group
        var members = await storage.GetAllFactionMembersPermissionsAsync(uuid);
        foreach (var member in members)
        {
            if (member.GroupUUID == groupId)
            {
                member.GroupUUID = null;
                await storage.UpsertFactionMemberPermissionsAsync(member);
            }
        }

        await storage.DeleteFactionGroupAsync(uuid, groupId);

        return Results.NoContent();
    }

    private static async Task<IResult> AssignMember(
        string uuid,
        string groupId,
        AssignMemberRequest request,
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

        var permGroup = await storage.GetFactionGroupAsync(uuid, groupId);
        if (permGroup == null)
        {
            return Results.NotFound(new { error = "Group not found" });
        }

        if (string.IsNullOrWhiteSpace(request.CharacterUUID))
        {
            return Results.BadRequest(new { error = "characterUUID is required" });
        }

        // Get or create member permissions record
        var memberPerms = await storage.GetFactionMemberPermissionsAsync(uuid, request.CharacterUUID);
        if (memberPerms == null)
        {
            memberPerms = new FactionMemberPermissions
            {
                CharacterUUID = request.CharacterUUID,
                FactionUUID = uuid,
                GroupUUID = groupId,
                ClearanceLevelUUID = permGroup.DefaultClearanceLevelUUID,
            };
        }
        else
        {
            // Move to new group (single-group constraint)
            memberPerms.GroupUUID = groupId;

            // Assign default clearance if none set
            if (string.IsNullOrEmpty(memberPerms.ClearanceLevelUUID))
            {
                memberPerms.ClearanceLevelUUID = permGroup.DefaultClearanceLevelUUID;
            }
        }

        await storage.UpsertFactionMemberPermissionsAsync(memberPerms);

        return Results.Ok(memberPerms);
    }

    private static async Task<IResult> RemoveMember(
        string uuid,
        string groupId,
        string characterUUID,
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

        var permGroup = await storage.GetFactionGroupAsync(uuid, groupId);
        if (permGroup == null)
        {
            return Results.NotFound(new { error = "Group not found" });
        }

        var memberPerms = await storage.GetFactionMemberPermissionsAsync(uuid, characterUUID);
        if (memberPerms == null || memberPerms.GroupUUID != groupId)
        {
            return Results.NotFound(new { error = "Member not found in this group" });
        }

        memberPerms.GroupUUID = null;
        await storage.UpsertFactionMemberPermissionsAsync(memberPerms);

        return Results.NoContent();
    }

    private static async Task<IResult> AddSharingRule(
        string uuid,
        string groupId,
        AddSharingRuleRequest request,
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

        var permGroup = await storage.GetFactionGroupAsync(uuid, groupId);
        if (permGroup == null)
        {
            return Results.NotFound(new { error = "Group not found" });
        }

        if (string.IsNullOrWhiteSpace(request.MinClearanceLevelUUID))
        {
            return Results.BadRequest(new { error = "minClearanceLevelUUID is required" });
        }

        var levels = await storage.GetFactionClearanceLevelsAsync(uuid);
        if (!levels.Any(l => l.UUID == request.MinClearanceLevelUUID))
        {
            return Results.BadRequest(new { error = "MinClearanceLevelUUID does not reference a valid clearance level" });
        }

        var rule = new FactionGroupSharingRule
        {
            UUID = Guid.NewGuid().ToString(),
            GroupUUID = groupId,
            DataType = request.DataType,
            EntityUUID = request.EntityUUID,
            MinClearanceLevelUUID = request.MinClearanceLevelUUID,
        };

        await storage.UpsertFactionGroupSharingRuleAsync(rule);

        return Results.Created(
            $"/api/v1/factions/{uuid}/groups/{groupId}/sharing-rules/{rule.UUID}",
            rule);
    }

    private static async Task<IResult> RemoveSharingRule(
        string uuid,
        string groupId,
        string ruleId,
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

        var permGroup = await storage.GetFactionGroupAsync(uuid, groupId);
        if (permGroup == null)
        {
            return Results.NotFound(new { error = "Group not found" });
        }

        var rules = await storage.GetFactionGroupSharingRulesAsync(groupId);
        var rule = rules.FirstOrDefault(r => r.UUID == ruleId);
        if (rule == null)
        {
            return Results.NotFound(new { error = "Sharing rule not found" });
        }

        await storage.DeleteFactionGroupSharingRuleAsync(groupId, ruleId);

        return Results.NoContent();
    }

    private static async Task<IResult> AddGroupCapability(
        string uuid,
        string groupId,
        AddGroupCapabilityRequest request,
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

        var permGroup = await storage.GetFactionGroupAsync(uuid, groupId);
        if (permGroup == null)
        {
            return Results.NotFound(new { error = "Group not found" });
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

        var item = new FactionGroupCapability
        {
            GroupUUID = groupId,
            CapabilityUUID = request.CapabilityUUID,
        };

        await storage.AddFactionGroupCapabilityAsync(item);

        return Results.Created(
            $"/api/v1/factions/{uuid}/groups/{groupId}/capabilities/{request.CapabilityUUID}",
            item);
    }

    private static async Task<IResult> RemoveGroupCapability(
        string uuid,
        string groupId,
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

        var permGroup = await storage.GetFactionGroupAsync(uuid, groupId);
        if (permGroup == null)
        {
            return Results.NotFound(new { error = "Group not found" });
        }

        var groupCaps = await storage.GetFactionGroupCapabilitiesAsync(groupId);
        if (!groupCaps.Any(gc => gc.CapabilityUUID == capId))
        {
            return Results.NotFound(new { error = "Capability not found in this group" });
        }

        await storage.RemoveFactionGroupCapabilityAsync(groupId, capId);

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

    /// <summary>Request body for creating a permission group.</summary>
    public class CreateGroupRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? DefaultClearanceLevelUUID { get; set; }
    }

    /// <summary>Request body for updating a permission group.</summary>
    public class UpdateGroupRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? DefaultClearanceLevelUUID { get; set; }
    }

    /// <summary>Request body for assigning a member to a group.</summary>
    public class AssignMemberRequest
    {
        public string? CharacterUUID { get; set; }
    }

    /// <summary>Request body for adding a sharing rule to a group.</summary>
    public class AddSharingRuleRequest
    {
        public string? DataType { get; set; }
        public string? EntityUUID { get; set; }
        public string? MinClearanceLevelUUID { get; set; }
    }

    /// <summary>Request body for adding a capability to a group.</summary>
    public class AddGroupCapabilityRequest
    {
        public string? CapabilityUUID { get; set; }
    }
}
