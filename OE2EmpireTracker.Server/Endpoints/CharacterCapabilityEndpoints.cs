using System.Security.Claims;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Models;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// CRUD endpoints for character-scoped capabilities.
/// </summary>
public static class CharacterCapabilityEndpoints
{
    public static void MapCharacterCapabilityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/characters/{uuid}/capabilities")
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

        var existing = await storage.GetCharacterCapabilitiesAsync(uuid);
        if (existing.Any(c => c.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return Results.Conflict(new { error = "A capability with this name already exists" });
        }

        var capability = new CharacterCapability
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerCharacterUUID = uuid,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
        };

        await storage.UpsertCharacterCapabilityAsync(capability);

        return Results.Created($"/api/v1/characters/{uuid}/capabilities/{capability.UUID}", capability);
    }

    private static async Task<IResult> GetCapabilities(
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

        var capabilities = await storage.GetCharacterCapabilitiesAsync(uuid);
        return Results.Ok(capabilities);
    }

    private static async Task<IResult> UpdateCapability(
        string uuid,
        string capId,
        UpdateCapabilityRequest request,
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

        var capabilities = await storage.GetCharacterCapabilitiesAsync(uuid);
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

        await storage.UpsertCharacterCapabilityAsync(capability);

        return Results.Ok(capability);
    }

    private static async Task<IResult> DeleteCapability(
        string uuid,
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

        var capabilities = await storage.GetCharacterCapabilitiesAsync(uuid);
        var capability = capabilities.FirstOrDefault(c => c.UUID == capId);
        if (capability == null)
        {
            return Results.NotFound(new { error = "Capability not found" });
        }

        // Cascade: remove from all groups
        var groups = await storage.GetCharacterGroupsAsync(uuid);
        foreach (var group in groups)
        {
            var groupCaps = await storage.GetCharacterGroupCapabilitiesAsync(group.UUID);
            if (groupCaps.Any(gc => gc.CapabilityUUID == capId))
            {
                await storage.RemoveCharacterGroupCapabilityAsync(group.UUID, capId);
            }
        }

        // Cascade: remove from all grantee individual capabilities
        var grantees = await storage.GetCharacterGranteesAsync(uuid);
        foreach (var grantee in grantees)
        {
            var granteeCaps = await storage.GetCharacterGranteeCapabilitiesAsync(uuid, grantee.GranteeUUID);
            if (granteeCaps.Any(gc => gc.CapabilityUUID == capId))
            {
                await storage.RemoveCharacterGranteeCapabilityAsync(uuid, grantee.GranteeUUID, capId);
            }
        }

        await storage.DeleteCharacterCapabilityAsync(uuid, capId);

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