using System.Security.Claims;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// CRUD endpoints for character permission groups, grantee assignment, sharing rules, and capability grants.
/// </summary>
public static class CharacterGroupEndpoints
{
    public static void MapCharacterGroupEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/characters/{uuid}/groups")
            .RequireAuthorization("Authenticated");

        group.MapPost("/", CreateGroup);
        group.MapGet("/", GetGroups);
        group.MapPut("/{groupId}", UpdateGroup);
        group.MapDelete("/{groupId}", DeleteGroup);

        // Grantee management
        group.MapPut("/{groupId}/members", AssignGrantee);
        group.MapDelete("/{groupId}/members/{granteeUUID}", RemoveGrantee);

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

        if (string.IsNullOrWhiteSpace(request.DefaultClearanceLevelUUID))
        {
            return Results.BadRequest(new { error = "defaultClearanceLevelUUID is required" });
        }

        var levels = await storage.GetCharacterClearanceLevelsAsync(uuid);
        if (!levels.Any(l => l.UUID == request.DefaultClearanceLevelUUID))
        {
            return Results.BadRequest(new { error = "DefaultClearanceLevelUUID does not reference a valid clearance level" });
        }

        var permGroup = new CharacterPermissionGroup
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerCharacterUUID = uuid,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            DefaultClearanceLevelUUID = request.DefaultClearanceLevelUUID,
        };

        await storage.UpsertCharacterGroupAsync(permGroup);

        return Results.Created($"/api/v1/characters/{uuid}/groups/{permGroup.UUID}", permGroup);
    }

    private static async Task<IResult> GetGroups(
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

        var groups = await storage.GetCharacterGroupsAsync(uuid);
        return Results.Ok(groups);
    }

    private static async Task<IResult> UpdateGroup(
        string uuid,
        string groupId,
        UpdateGroupRequest request,
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

        var permGroup = await storage.GetCharacterGroupAsync(uuid, groupId);
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
            var levels = await storage.GetCharacterClearanceLevelsAsync(uuid);
            if (!levels.Any(l => l.UUID == request.DefaultClearanceLevelUUID))
            {
                return Results.BadRequest(new { error = "DefaultClearanceLevelUUID does not reference a valid clearance level" });
            }

            permGroup.DefaultClearanceLevelUUID = request.DefaultClearanceLevelUUID;
        }

        await storage.UpsertCharacterGroupAsync(permGroup);

        return Results.Ok(permGroup);
    }

    private static async Task<IResult> DeleteGroup(
        string uuid,
        string groupId,
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

        var permGroup = await storage.GetCharacterGroupAsync(uuid, groupId);
        if (permGroup == null)
        {
            return Results.NotFound(new { error = "Group not found" });
        }

        // Cascade: remove group capabilities
        var groupCaps = await storage.GetCharacterGroupCapabilitiesAsync(groupId);
        foreach (var gc in groupCaps)
        {
            await storage.RemoveCharacterGroupCapabilityAsync(groupId, gc.CapabilityUUID);
        }

        // Cascade: remove group sharing rules
        var sharingRules = await storage.GetCharacterGroupSharingRulesAsync(groupId);
        foreach (var rule in sharingRules)
        {
            await storage.DeleteCharacterGroupSharingRuleAsync(groupId, rule.UUID);
        }

        // Cascade: clear GroupUUID from grantees in this group
        var grantees = await storage.GetCharacterGranteesAsync(uuid);
        foreach (var grantee in grantees)
        {
            if (grantee.GroupUUID == groupId)
            {
                grantee.GroupUUID = null;
                await storage.UpsertCharacterGranteePermissionsAsync(grantee);
            }
        }

        await storage.DeleteCharacterGroupAsync(uuid, groupId);

        return Results.NoContent();
    }

    private static async Task<IResult> AssignGrantee(
        string uuid,
        string groupId,
        AssignGranteeRequest request,
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

        var permGroup = await storage.GetCharacterGroupAsync(uuid, groupId);
        if (permGroup == null)
        {
            return Results.NotFound(new { error = "Group not found" });
        }

        if (string.IsNullOrWhiteSpace(request.GranteeUUID))
        {
            return Results.BadRequest(new { error = "granteeUUID is required" });
        }

        // Get or create grantee permissions record
        var grantees = await storage.GetCharacterGranteesAsync(uuid);
        var granteePerms = grantees.FirstOrDefault(g => g.GranteeUUID == request.GranteeUUID);
        if (granteePerms == null)
        {
            granteePerms = new CharacterGranteePermissions
            {
                OwnerCharacterUUID = uuid,
                GranteeType = request.GranteeType,
                GranteeUUID = request.GranteeUUID,
                GroupUUID = groupId,
                ClearanceLevelUUID = permGroup.DefaultClearanceLevelUUID,
            };
        }
        else
        {
            // Move to new group (single-group constraint)
            granteePerms.GroupUUID = groupId;

            // Assign default clearance if none set
            if (string.IsNullOrEmpty(granteePerms.ClearanceLevelUUID))
            {
                granteePerms.ClearanceLevelUUID = permGroup.DefaultClearanceLevelUUID;
            }
        }

        await storage.UpsertCharacterGranteePermissionsAsync(granteePerms);

        return Results.Ok(granteePerms);
    }

    private static async Task<IResult> RemoveGrantee(
        string uuid,
        string groupId,
        string granteeUUID,
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

        var permGroup = await storage.GetCharacterGroupAsync(uuid, groupId);
        if (permGroup == null)
        {
            return Results.NotFound(new { error = "Group not found" });
        }

        var grantees = await storage.GetCharacterGranteesAsync(uuid);
        var granteePerms = grantees.FirstOrDefault(g => g.GranteeUUID == granteeUUID);
        if (granteePerms == null || granteePerms.GroupUUID != groupId)
        {
            return Results.NotFound(new { error = "Grantee not found in this group" });
        }

        granteePerms.GroupUUID = null;
        await storage.UpsertCharacterGranteePermissionsAsync(granteePerms);

        return Results.NoContent();
    }

    private static async Task<IResult> AddSharingRule(
        string uuid,
        string groupId,
        AddSharingRuleRequest request,
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

        var permGroup = await storage.GetCharacterGroupAsync(uuid, groupId);
        if (permGroup == null)
        {
            return Results.NotFound(new { error = "Group not found" });
        }

        var rule = new CharacterGroupSharingRule
        {
            UUID = Guid.NewGuid().ToString(),
            GroupUUID = groupId,
            DataType = request.DataType,
            EntityUUID = request.EntityUUID,
        };

        await storage.UpsertCharacterGroupSharingRuleAsync(rule);

        return Results.Created(
            $"/api/v1/characters/{uuid}/groups/{groupId}/sharing-rules/{rule.UUID}",
            rule);
    }

    private static async Task<IResult> RemoveSharingRule(
        string uuid,
        string groupId,
        string ruleId,
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

        var permGroup = await storage.GetCharacterGroupAsync(uuid, groupId);
        if (permGroup == null)
        {
            return Results.NotFound(new { error = "Group not found" });
        }

        var rules = await storage.GetCharacterGroupSharingRulesAsync(groupId);
        var rule = rules.FirstOrDefault(r => r.UUID == ruleId);
        if (rule == null)
        {
            return Results.NotFound(new { error = "Sharing rule not found" });
        }

        await storage.DeleteCharacterGroupSharingRuleAsync(groupId, ruleId);

        return Results.NoContent();
    }

    private static async Task<IResult> AddGroupCapability(
        string uuid,
        string groupId,
        AddGroupCapabilityRequest request,
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

        var permGroup = await storage.GetCharacterGroupAsync(uuid, groupId);
        if (permGroup == null)
        {
            return Results.NotFound(new { error = "Group not found" });
        }

        if (string.IsNullOrWhiteSpace(request.CapabilityUUID))
        {
            return Results.BadRequest(new { error = "capabilityUUID is required" });
        }

        var capabilities = await storage.GetCharacterCapabilitiesAsync(uuid);
        if (!capabilities.Any(c => c.UUID == request.CapabilityUUID))
        {
            return Results.BadRequest(new { error = "CapabilityUUID does not reference a valid character capability" });
        }

        var item = new CharacterGroupCapability
        {
            GroupUUID = groupId,
            CapabilityUUID = request.CapabilityUUID,
        };

        await storage.AddCharacterGroupCapabilityAsync(item);

        return Results.Created(
            $"/api/v1/characters/{uuid}/groups/{groupId}/capabilities/{request.CapabilityUUID}",
            item);
    }

    private static async Task<IResult> RemoveGroupCapability(
        string uuid,
        string groupId,
        string capId,
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

        var permGroup = await storage.GetCharacterGroupAsync(uuid, groupId);
        if (permGroup == null)
        {
            return Results.NotFound(new { error = "Group not found" });
        }

        var groupCaps = await storage.GetCharacterGroupCapabilitiesAsync(groupId);
        if (!groupCaps.Any(gc => gc.CapabilityUUID == capId))
        {
            return Results.NotFound(new { error = "Capability not found in this group" });
        }

        await storage.RemoveCharacterGroupCapabilityAsync(groupId, capId);

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

    /// <summary>Request body for assigning a grantee to a group.</summary>
    public class AssignGranteeRequest
    {
        public GranteeType GranteeType { get; set; }
        public string? GranteeUUID { get; set; }
    }

    /// <summary>Request body for adding a sharing rule to a group.</summary>
    public class AddSharingRuleRequest
    {
        public string? DataType { get; set; }
        public string? EntityUUID { get; set; }
    }

    /// <summary>Request body for adding a capability to a group.</summary>
    public class AddGroupCapabilityRequest
    {
        public string? CapabilityUUID { get; set; }
    }
}
