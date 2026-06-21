using System.Security.Claims;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Server.Services;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// Intel comment endpoints: create, query, share, classify, delete.
/// </summary>
public static class IntelEndpoints
{
    public static void MapIntelEndpoints(this WebApplication app)
    {
        var charGroup = app.MapGroup("/api/v1/characters/{uuid}/intel")
            .RequireAuthorization("Authenticated");

        charGroup.MapPost("/", CreateComment);
        charGroup.MapGet("/", GetComments);
        charGroup.MapPost("/{commentId}/share", ShareComment);
        charGroup.MapDelete("/{commentId}/share/{factionUUID}", RevokeShare);
        charGroup.MapDelete("/{commentId}", DeleteComment);
        charGroup.MapPut("/{commentId}", RejectCommentEdit);

        var factionGroup = app.MapGroup("/api/v1/factions/{uuid}/intel")
            .RequireAuthorization("Authenticated");

        factionGroup.MapPut("/{shareId}/classify", ClassifyComment);
    }

    private static async Task<IResult> CreateComment(
        string uuid,
        CreateIntelRequest request,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        var callerCharUUID = AuthorizationHelper.GetCallerCharacterUUID(httpContext);
        if (callerCharUUID == null)
        {
            return Results.Forbid();
        }

        if (callerCharUUID != uuid && !AuthorizationHelper.IsOwner(httpContext))
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return Results.BadRequest(new { error = "text is required" });
        }

        var comment = new IntelComment
        {
            UUID = Guid.NewGuid().ToString(),
            TargetCharacterUUID = uuid,
            SubmitterCharacterUUID = callerCharUUID,
            Text = request.Text,
            CreatedUtc = SystemClock.UtcNow,
        };

        await storage.UpsertIntelCommentAsync(comment);

        return Results.Created(
            $"/api/v1/characters/{uuid}/intel/{comment.UUID}",
            comment);
    }

    private static async Task<IResult> GetComments(
        string uuid,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        var canAccess = await AuthorizationHelper.CanAccessCharacterData(httpContext, uuid, storage);
        if (!canAccess)
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        var callerCharUUID = httpContext.User.FindFirstValue("CharacterUUID");
        if (callerCharUUID == null)
        {
            return Results.Forbid();
        }

        var callerFactionUUID = httpContext.User.FindFirstValue("FactionUUID");

        // Determine caller's clearance level and classify_intel capability
        int callerClearanceLevel = 0;
        bool hasClassifyIntel = false;
        IReadOnlyList<FactionClearanceLevel>? factionLevels = null;

        if (callerFactionUUID != null)
        {
            factionLevels = await storage.GetFactionClearanceLevelsAsync(callerFactionUUID);

            var memberPerms = await storage.GetFactionMemberPermissionsAsync(
                callerFactionUUID, callerCharUUID);
            if (memberPerms != null && !string.IsNullOrEmpty(memberPerms.ClearanceLevelUUID))
            {
                var level = factionLevels.FirstOrDefault(
                    l => l.UUID == memberPerms.ClearanceLevelUUID);
                if (level != null)
                {
                    callerClearanceLevel = level.Level;
                }
            }

            // Check classify_intel capability
            var memberCaps = await storage.GetFactionMemberCapabilitiesAsync(
                callerFactionUUID, callerCharUUID);
            var factionCaps = await storage.GetFactionCapabilitiesAsync(callerFactionUUID);
            var classifyCapUUID = factionCaps
                .FirstOrDefault(c => c.Name == "classify_intel")?.UUID;

            if (classifyCapUUID != null)
            {
                hasClassifyIntel = memberCaps.Any(mc => mc.CapabilityUUID == classifyCapUUID);

                // Also check group capabilities
                if (!hasClassifyIntel && memberPerms?.GroupUUID != null)
                {
                    var groupCaps = await storage.GetFactionGroupCapabilitiesAsync(
                        memberPerms.GroupUUID);
                    hasClassifyIntel = groupCaps.Any(
                        gc => gc.CapabilityUUID == classifyCapUUID);
                }
            }
        }

        var allComments = await storage.GetIntelCommentsForTargetAsync(uuid);

        // Gather all shares for these comments
        var allShares = new List<IntelCommentFactionShare>();
        foreach (var comment in allComments)
        {
            var shares = await storage.GetIntelSharesForCommentAsync(comment.UUID);
            allShares.AddRange(shares);
        }

        var visible = IntelVisibilityService.FilterIntelForCaller(
            allComments,
            allShares,
            callerCharUUID,
            callerFactionUUID,
            callerClearanceLevel,
            hasClassifyIntel,
            factionLevels);

        // Build response with submitter names
        var characters = await storage.GetAllCharactersAsync();
        var charLookup = characters.ToDictionary(c => c.UUID, c => c.Name);

        var response = visible.Select(c => new
        {
            c.UUID,
            c.TargetCharacterUUID,
            c.SubmitterCharacterUUID,
            SubmitterName = charLookup.GetValueOrDefault(
                c.SubmitterCharacterUUID, "Unknown"),
            c.Text,
            c.CreatedUtc,
        });

        return Results.Ok(response);
    }

    private static async Task<IResult> ShareComment(
        string uuid,
        string commentId,
        ShareIntelRequest request,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        var callerCharUUID = AuthorizationHelper.GetCallerCharacterUUID(httpContext);
        if (callerCharUUID == null)
        {
            return Results.Forbid();
        }

        var comment = await storage.GetIntelCommentAsync(commentId);
        if (comment == null)
        {
            return Results.NotFound(new { error = "Comment not found" });
        }

        if (comment.SubmitterCharacterUUID != callerCharUUID && !AuthorizationHelper.IsOwner(httpContext))
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        if (string.IsNullOrWhiteSpace(request.FactionUUID))
        {
            return Results.BadRequest(new { error = "factionUUID is required" });
        }

        var faction = await storage.GetFactionAsync(request.FactionUUID);
        if (faction == null)
        {
            return Results.NotFound(new { error = "Faction not found" });
        }

        var share = new IntelCommentFactionShare
        {
            UUID = Guid.NewGuid().ToString(),
            IntelCommentUUID = commentId,
            FactionUUID = request.FactionUUID,
            ClassificationLevelUUID = null,
            ClassifiedByCharacterUUID = null,
            SharedUtc = SystemClock.UtcNow,
            ClassifiedUtc = null,
        };

        await storage.UpsertIntelShareAsync(share);

        return Results.Created(
            $"/api/v1/characters/{uuid}/intel/{commentId}/share/{share.UUID}",
            share);
    }

    private static async Task<IResult> RevokeShare(
        string uuid,
        string commentId,
        string factionUUID,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        var callerCharUUID = AuthorizationHelper.GetCallerCharacterUUID(httpContext);
        if (callerCharUUID == null)
        {
            return Results.Forbid();
        }

        var comment = await storage.GetIntelCommentAsync(commentId);
        if (comment == null)
        {
            return Results.NotFound(new { error = "Comment not found" });
        }

        if (comment.SubmitterCharacterUUID != callerCharUUID && !AuthorizationHelper.IsOwner(httpContext))
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        var shares = await storage.GetIntelSharesForCommentAsync(commentId);
        var share = shares.FirstOrDefault(s => s.FactionUUID == factionUUID);
        if (share == null)
        {
            return Results.NotFound(new { error = "Share not found" });
        }

        await storage.DeleteIntelShareAsync(share.UUID);

        return Results.NoContent();
    }

    private static async Task<IResult> ClassifyComment(
        string uuid,
        string shareId,
        ClassifyIntelRequest request,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        // Enforce caller is faction leader or Owner
        var faction = await storage.GetFactionAsync(uuid);
        if (faction == null)
        {
            return Results.NotFound(new { error = "Faction not found" });
        }

        var callerUUID = AuthorizationHelper.GetCallerCharacterUUID(httpContext);
        if (!AuthorizationHelper.IsOwner(httpContext) &&
            (callerUUID == null || !faction.LeaderCharacterUUIDs.Contains(callerUUID)))
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        // Find the share
        var factionShares = await storage.GetIntelSharesForFactionAsync(uuid);
        var share = factionShares.FirstOrDefault(s => s.UUID == shareId);
        if (share == null)
        {
            return Results.NotFound(new { error = "Share not found" });
        }

        // Validate classification level exists
        if (string.IsNullOrWhiteSpace(request.ClassificationLevelUUID))
        {
            return Results.BadRequest(
                new { error = "classificationLevelUUID is required" });
        }

        var levels = await storage.GetFactionClearanceLevelsAsync(uuid);
        var level = levels.FirstOrDefault(
            l => l.UUID == request.ClassificationLevelUUID);
        if (level == null)
        {
            return Results.NotFound(
                new { error = "Classification level not found" });
        }

        share.ClassificationLevelUUID = request.ClassificationLevelUUID;
        share.ClassifiedByCharacterUUID = callerUUID;
        share.ClassifiedUtc = SystemClock.UtcNow;

        await storage.UpsertIntelShareAsync(share);

        return Results.Ok(share);
    }

    private static async Task<IResult> DeleteComment(
        string uuid,
        string commentId,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        var callerCharUUID = AuthorizationHelper.GetCallerCharacterUUID(httpContext);
        if (callerCharUUID == null)
        {
            return Results.Forbid();
        }

        var comment = await storage.GetIntelCommentAsync(commentId);
        if (comment == null)
        {
            return Results.NotFound(new { error = "Comment not found" });
        }

        if (comment.SubmitterCharacterUUID != callerCharUUID && !AuthorizationHelper.IsOwner(httpContext))
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        // Delete all shares first
        var shares = await storage.GetIntelSharesForCommentAsync(commentId);
        foreach (var share in shares)
        {
            await storage.DeleteIntelShareAsync(share.UUID);
        }

        await storage.DeleteIntelCommentAsync(commentId);

        return Results.NoContent();
    }

    private static IResult RejectCommentEdit(
        string uuid,
        string commentId)
    {
        return Results.StatusCode(405);
    }

    /// <summary>Request body for creating an intel comment.</summary>
    public class CreateIntelRequest
    {
        public string? Text { get; set; }
    }

    /// <summary>Request body for sharing an intel comment with a faction.</summary>
    public class ShareIntelRequest
    {
        public string? FactionUUID { get; set; }
    }

    /// <summary>Request body for classifying an intel comment.</summary>
    public class ClassifyIntelRequest
    {
        public string? ClassificationLevelUUID { get; set; }
    }
}