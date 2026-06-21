using System.Security.Claims;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Server.Processing;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// Admin endpoints for server status and processing control,
/// plus character preferences management.
/// </summary>
public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        // Status — any authenticated user
        app.MapGet("/api/v1/status", GetStatus)
            .RequireAuthorization("Authenticated");

        // Processing control — Owner only
        app.MapPut("/api/v1/admin/processing", SetProcessingEnabled)
            .RequireAuthorization("Owner");

        // Character preferences
        var prefsGroup = app.MapGroup("/api/v1/characters/{uuid}/preferences")
            .RequireAuthorization("Authenticated");

        prefsGroup.MapGet("/", GetPreferences);
        prefsGroup.MapPut("/", UpdatePreferences);
    }

    private static IResult GetStatus(HttpContext httpContext)
    {
        var processor = httpContext.RequestServices
            .GetRequiredService<ServerBackgroundProcessor>();

        return Results.Ok(new
        {
            processingEnabled = processor.IsEnabled,
            lastTickUtc = processor.LastTickUtc,
            lastCharactersProcessed = processor.LastCharactersProcessed,
        });
    }

    private static IResult SetProcessingEnabled(
        ProcessingToggleRequest request,
        HttpContext httpContext)
    {
        var processor = httpContext.RequestServices
            .GetRequiredService<ServerBackgroundProcessor>();

        processor.IsEnabled = request.Enabled;

        return Results.Ok(new { processingEnabled = processor.IsEnabled });
    }

    private static async Task<IResult> GetPreferences(
        string uuid,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!CanAccessCharacter(httpContext, uuid))
        {
            return Results.Forbid();
        }

        var prefs = await storage.GetCharacterPreferencesAsync(uuid);
        if (prefs == null)
        {
            prefs = new CharacterPreferences
            {
                CharacterUUID = uuid,
                ServerProcessing = false,
            };
        }

        return Results.Ok(new
        {
            characterUUID = prefs.CharacterUUID,
            serverProcessing = prefs.ServerProcessing,
        });
    }

    private static async Task<IResult> UpdatePreferences(
        string uuid,
        PreferencesUpdateRequest request,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!CanAccessCharacter(httpContext, uuid))
        {
            return Results.Forbid();
        }

        var prefs = await storage.GetCharacterPreferencesAsync(uuid)
            ?? new CharacterPreferences { CharacterUUID = uuid };

        prefs.ServerProcessing = request.ServerProcessing;
        await storage.UpsertCharacterPreferencesAsync(prefs);

        return Results.Ok(new
        {
            characterUUID = prefs.CharacterUUID,
            serverProcessing = prefs.ServerProcessing,
        });
    }

    private static bool CanAccessCharacter(HttpContext httpContext, string uuid)
    {
        // Owner can access any character
        if (httpContext.User.IsInRole(TokenRole.Owner.ToString()))
        {
            return true;
        }

        // Character can only access own preferences
        var callerUUID = httpContext.User.FindFirstValue("CharacterUUID");
        return callerUUID == uuid;
    }

    /// <summary>Request body for toggling processing.</summary>
    public class ProcessingToggleRequest
    {
        public bool Enabled { get; set; }
    }

    /// <summary>Request body for updating preferences.</summary>
    public class PreferencesUpdateRequest
    {
        public bool ServerProcessing { get; set; }
    }
}
