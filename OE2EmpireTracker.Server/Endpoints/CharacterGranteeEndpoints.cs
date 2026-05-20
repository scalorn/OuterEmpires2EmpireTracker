using System.Security.Claims;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// Endpoints for individual grantee capability grants on character-scoped permissions.
/// </summary>
public static class CharacterGranteeEndpoints
{
    public static void MapCharacterGranteeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/characters/{uuid}/grantees/{granteeUUID}/capabilities")
            .RequireAuthorization("Authenticated");

        group.MapPut("/", GrantCapability);
        group.MapDelete("/{capId}", RevokeCapability);
    }

    private static async Task<IResult> GrantCapability(
        string uuid,
        string granteeUUID,
        GrantCapabilityRequest request,
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

        if (string.IsNullOrWhiteSpace(request.CapabilityUUID))
        {
            return Results.BadRequest(new { error = "capabilityUUID is required" });
        }

        var capabilities = await storage.GetCharacterCapabilitiesAsync(uuid);
        if (!capabilities.Any(c => c.UUID == request.CapabilityUUID))
        {
            return Results.BadRequest(new { error = "CapabilityUUID does not reference a valid character capability" });
        }

        // Ensure grantee permissions record exists
        var grantees = await storage.GetCharacterGranteesAsync(uuid);
        var granteePerms = grantees.FirstOrDefault(g => g.GranteeUUID == granteeUUID);
        if (granteePerms == null)
        {
            return Results.NotFound(new { error = "Grantee not found" });
        }

        var item = new CharacterGranteeCapability
        {
            OwnerCharacterUUID = uuid,
            GranteeType = granteePerms.GranteeType,
            GranteeUUID = granteeUUID,
            CapabilityUUID = request.CapabilityUUID,
        };

        await storage.AddCharacterGranteeCapabilityAsync(item);

        return Results.Ok(item);
    }

    private static async Task<IResult> RevokeCapability(
        string uuid,
        string granteeUUID,
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

        var grantees = await storage.GetCharacterGranteesAsync(uuid);
        var granteePerms = grantees.FirstOrDefault(g => g.GranteeUUID == granteeUUID);
        if (granteePerms == null)
        {
            return Results.NotFound(new { error = "Grantee not found" });
        }

        var granteeCaps = await storage.GetCharacterGranteeCapabilitiesAsync(uuid, granteeUUID);
        if (!granteeCaps.Any(gc => gc.CapabilityUUID == capId))
        {
            return Results.NotFound(new { error = "Capability not found for this grantee" });
        }

        await storage.RemoveCharacterGranteeCapabilityAsync(uuid, granteeUUID, capId);

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

    /// <summary>Request body for granting a capability to a grantee.</summary>
    public class GrantCapabilityRequest
    {
        public string? CapabilityUUID { get; set; }
    }
}
