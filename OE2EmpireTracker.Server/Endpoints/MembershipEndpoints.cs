using System.Security.Claims;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Server.Push;
using OE2EmpireTracker.Server.Storage;

using OE2EmpireTracker.Services;
namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// Faction membership endpoints: join requests, invitations, and leave.
/// Implements mutual consent model for faction membership.
/// </summary>
public static class MembershipEndpoints
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromDays(7);

    public static void MapMembershipEndpoints(this WebApplication app)
    {
        var factionGroup = app.MapGroup("/api/v1/factions")
            .RequireAuthorization("Authenticated");

        // Join Requests (Character -> Faction)
        factionGroup.MapPost("/{uuid}/requests", CreateJoinRequest);
        factionGroup.MapGet("/{uuid}/requests", GetJoinRequests);
        factionGroup.MapPost("/{uuid}/requests/{id}/accept", AcceptJoinRequest);

        // Invitations (Faction -> Character)
        factionGroup.MapPost("/{uuid}/invitations", CreateInvitation);
        factionGroup.MapGet("/{uuid}/invitations", GetInvitations);
        factionGroup.MapPost("/{uuid}/invitations/{id}/accept", AcceptInvitation);

        // Leave Faction (on characters path)
        var charGroup = app.MapGroup("/api/v1/characters")
            .RequireAuthorization("Authenticated");

        charGroup.MapDelete("/{uuid}/faction", LeaveFaction);
    }

    // --- Join Requests ---

    private static async Task<IResult> CreateJoinRequest(
        string uuid,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        // Clean up expired actions first
        await storage.DeleteExpiredActionsAsync(SystemClock.UtcNow);

        var faction = await storage.GetFactionAsync(uuid);
        if (faction == null)
        {
            return Results.NotFound(new { error = "Faction not found" });
        }

        var callerCharUUID = httpContext.User.FindFirstValue("CharacterUUID");
        if (string.IsNullOrEmpty(callerCharUUID))
        {
            return Results.BadRequest(new { error = "Token must be associated with a character" });
        }

        // Character must not already be in a faction
        var character = await storage.GetCharacterAsync(callerCharUUID);
        if (character == null)
        {
            return Results.NotFound(new { error = "Character not found" });
        }

        if (!string.IsNullOrEmpty(character.FactionUUID))
        {
            return Results.BadRequest(new { error = "Character is already in a faction. Leave first." });
        }

        // Check for duplicate pending request
        var existingActions = await storage.GetFactionActionsAsync(uuid);
        var duplicate = existingActions.FirstOrDefault(a =>
            a.CharacterUUID == callerCharUUID &&
            a.Type == MembershipActionType.JoinRequest);
        if (duplicate != null)
        {
            return Results.Conflict(new { error = "A pending join request already exists" });
        }

        var action = new MembershipAction
        {
            Id = Guid.NewGuid().ToString(),
            FactionUUID = uuid,
            CharacterUUID = callerCharUUID,
            Type = MembershipActionType.JoinRequest,
            CreatedUtc = SystemClock.UtcNow,
            ExpiresUtc = SystemClock.UtcNow.Add(DefaultExpiry),
        };

        await storage.UpsertMembershipActionAsync(action);

        return Results.Created($"/api/v1/factions/{uuid}/requests/{action.Id}", action);
    }

    private static async Task<IResult> GetJoinRequests(
        string uuid,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        // Clean up expired actions first
        await storage.DeleteExpiredActionsAsync(SystemClock.UtcNow);

        var faction = await storage.GetFactionAsync(uuid);
        if (faction == null)
        {
            return Results.NotFound(new { error = "Faction not found" });
        }

        if (!IsOwnerOrFactionLeader(httpContext, faction))
        {
            return Results.Forbid();
        }

        var actions = await storage.GetFactionActionsAsync(uuid);
        var requests = actions.Where(a => a.Type == MembershipActionType.JoinRequest).ToList();

        return Results.Ok(requests);
    }

    private static async Task<IResult> AcceptJoinRequest(
        string uuid,
        string id,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        // Clean up expired actions first
        await storage.DeleteExpiredActionsAsync(SystemClock.UtcNow);

        var faction = await storage.GetFactionAsync(uuid);
        if (faction == null)
        {
            return Results.NotFound(new { error = "Faction not found" });
        }

        if (!IsOwnerOrFactionLeader(httpContext, faction))
        {
            return Results.Forbid();
        }

        var actions = await storage.GetFactionActionsAsync(uuid);
        var request = actions.FirstOrDefault(a =>
            a.Id == id && a.Type == MembershipActionType.JoinRequest);
        if (request == null)
        {
            return Results.NotFound(new { error = "Join request not found" });
        }

        // Set character's faction
        var character = await storage.GetCharacterAsync(request.CharacterUUID);
        if (character == null)
        {
            // Character was deleted; clean up the action
            await storage.DeleteMembershipActionAsync(id);
            return Results.NotFound(new { error = "Character no longer exists" });
        }

        character.FactionUUID = uuid;
        character.Metadata.LastModifiedUtc = SystemClock.UtcNow;
        await storage.UpsertCharacterAsync(character);

        // Delete the action
        await storage.DeleteMembershipActionAsync(id);

        LogMutation(httpContext, "AcceptedJoinRequest", "Membership", $"{uuid}/{request.CharacterUUID}");
        await DispatchMembershipEvent(httpContext, uuid, request.CharacterUUID);

        return Results.Ok(character);
    }

    // --- Invitations ---

    private static async Task<IResult> CreateInvitation(
        string uuid,
        InvitationRequest request,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        // Clean up expired actions first
        await storage.DeleteExpiredActionsAsync(SystemClock.UtcNow);

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

        // Validate character exists
        var character = await storage.GetCharacterAsync(request.CharacterUUID);
        if (character == null)
        {
            return Results.NotFound(new { error = "Character not found" });
        }

        // Character must not already be in a faction
        if (!string.IsNullOrEmpty(character.FactionUUID))
        {
            return Results.BadRequest(new { error = "Character is already in a faction" });
        }

        // Check for duplicate pending invitation
        var existingActions = await storage.GetFactionActionsAsync(uuid);
        var duplicate = existingActions.FirstOrDefault(a =>
            a.CharacterUUID == request.CharacterUUID &&
            a.Type == MembershipActionType.Invitation);
        if (duplicate != null)
        {
            return Results.Conflict(new { error = "A pending invitation already exists for this character" });
        }

        var action = new MembershipAction
        {
            Id = Guid.NewGuid().ToString(),
            FactionUUID = uuid,
            CharacterUUID = request.CharacterUUID,
            Type = MembershipActionType.Invitation,
            CreatedUtc = SystemClock.UtcNow,
            ExpiresUtc = SystemClock.UtcNow.Add(DefaultExpiry),
        };

        await storage.UpsertMembershipActionAsync(action);

        return Results.Created($"/api/v1/factions/{uuid}/invitations/{action.Id}", action);
    }

    private static async Task<IResult> GetInvitations(
        string uuid,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        // Clean up expired actions first
        await storage.DeleteExpiredActionsAsync(SystemClock.UtcNow);

        var faction = await storage.GetFactionAsync(uuid);
        if (faction == null)
        {
            return Results.NotFound(new { error = "Faction not found" });
        }

        if (!IsOwnerOrFactionLeader(httpContext, faction))
        {
            return Results.Forbid();
        }

        var actions = await storage.GetFactionActionsAsync(uuid);
        var invitations = actions.Where(a => a.Type == MembershipActionType.Invitation).ToList();

        return Results.Ok(invitations);
    }

    private static async Task<IResult> AcceptInvitation(
        string uuid,
        string id,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        // Clean up expired actions first
        await storage.DeleteExpiredActionsAsync(SystemClock.UtcNow);

        var faction = await storage.GetFactionAsync(uuid);
        if (faction == null)
        {
            return Results.NotFound(new { error = "Faction not found" });
        }

        var actions = await storage.GetFactionActionsAsync(uuid);
        var invitation = actions.FirstOrDefault(a =>
            a.Id == id && a.Type == MembershipActionType.Invitation);
        if (invitation == null)
        {
            return Results.NotFound(new { error = "Invitation not found" });
        }

        // Only the invited character (or Owner) can accept
        var callerCharUUID = httpContext.User.FindFirstValue("CharacterUUID");
        if (!IsOwner(httpContext) && callerCharUUID != invitation.CharacterUUID)
        {
            return Results.Forbid();
        }

        // Set character's faction
        var character = await storage.GetCharacterAsync(invitation.CharacterUUID);
        if (character == null)
        {
            await storage.DeleteMembershipActionAsync(id);
            return Results.NotFound(new { error = "Character no longer exists" });
        }

        character.FactionUUID = uuid;
        character.Metadata.LastModifiedUtc = SystemClock.UtcNow;
        await storage.UpsertCharacterAsync(character);

        // Delete the action
        await storage.DeleteMembershipActionAsync(id);

        LogMutation(httpContext, "AcceptedInvitation", "Membership", $"{uuid}/{invitation.CharacterUUID}");
        await DispatchMembershipEvent(httpContext, uuid, invitation.CharacterUUID);

        return Results.Ok(character);
    }

    // --- Leave Faction ---

    private static async Task<IResult> LeaveFaction(
        string uuid,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        var character = await storage.GetCharacterAsync(uuid);
        if (character == null)
        {
            return Results.NotFound(new { error = "Character not found" });
        }

        // Owner can remove anyone; Character can only leave own faction
        if (!IsOwner(httpContext))
        {
            var callerCharUUID = httpContext.User.FindFirstValue("CharacterUUID");
            if (callerCharUUID != uuid)
            {
                return Results.Forbid();
            }
        }

        if (string.IsNullOrEmpty(character.FactionUUID))
        {
            return Results.BadRequest(new { error = "Character is not in a faction" });
        }

        character.FactionUUID = null;
        character.Metadata.LastModifiedUtc = SystemClock.UtcNow;
        await storage.UpsertCharacterAsync(character);

        LogMutation(httpContext, "LeftFaction", "Membership", $"{uuid}");
        await DispatchEntityEvent(httpContext, ServerEventType.MembershipChanged, "Character", uuid);

        return Results.NoContent();
    }

    // --- Helpers ---

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

    private static void LogMutation(HttpContext httpContext, string action, string entityType, string uuid)
    {
        var tokenId = httpContext.User.FindFirstValue("TokenId") ?? "unknown";
        var remoteIp = httpContext.Connection.RemoteIpAddress;
        var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("MembershipEndpoints");

        logger.LogInformation(
            "Mutation: {Action} {EntityType}/{UUID} by token {TokenId} from {IP}",
            action,
            entityType,
            uuid,
            tokenId,
            remoteIp);
    }

    private static async Task DispatchMembershipEvent(
        HttpContext httpContext,
        string factionUuid,
        string characterUuid)
    {
        var dispatcher = httpContext.RequestServices.GetRequiredService<EventDispatcher>();
        var evt = new ServerEvent
        {
            EventType = ServerEventType.MembershipChanged,
            EntityType = "Faction",
            EntityUUID = factionUuid,
        };

        // Notify the faction members
        await dispatcher.DispatchToFaction(factionUuid, evt);

        // Notify the character who joined
        await dispatcher.DispatchEvent(new ServerEvent
        {
            EventType = ServerEventType.MembershipChanged,
            EntityType = "Character",
            EntityUUID = characterUuid,
            OwnerCharacterUUID = characterUuid,
        });
    }

    private static async Task DispatchEntityEvent(
        HttpContext httpContext,
        ServerEventType eventType,
        string entityType,
        string entityUuid)
    {
        var dispatcher = httpContext.RequestServices.GetRequiredService<EventDispatcher>();
        await dispatcher.DispatchEvent(new ServerEvent
        {
            EventType = eventType,
            EntityType = entityType,
            EntityUUID = entityUuid,
        });
    }

    // --- Request DTOs ---

    /// <summary>Request body for creating an invitation.</summary>
    public class InvitationRequest
    {
        public string? CharacterUUID { get; set; }
    }
}
