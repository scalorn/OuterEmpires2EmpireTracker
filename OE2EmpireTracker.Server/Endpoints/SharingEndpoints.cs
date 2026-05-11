using System.Security.Claims;
using System.Text.Json;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// Sharing controls and shared data access endpoints.
/// </summary>
public static class SharingEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static void MapSharingEndpoints(this WebApplication app)
    {
        // Sharing config (per character)
        var sharing = app.MapGroup("/api/v1/characters/{uuid}/sharing")
            .RequireAuthorization("Authenticated");

        sharing.MapGet("/", GetSharingConfig);
        sharing.MapPut("/", UpdateSharingConfig);

        // Faction shared data
        app.MapGet("/api/v1/factions/{uuid}/shared/{dataType}", GetFactionSharedData)
            .RequireAuthorization("Authenticated");

        // Data shared with me
        app.MapGet("/api/v1/characters/{uuid}/shared-with-me/{dataType}", GetSharedWithMe)
            .RequireAuthorization("Authenticated");
    }

    private static async Task<IResult> GetSharingConfig(
        string uuid,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!CanAccessCharacterData(httpContext, uuid))
        {
            return Results.Forbid();
        }

        var rules = await storage.GetSharingRulesForCharacterAsync(uuid);
        return Results.Ok(rules);
    }

    private static async Task<IResult> UpdateSharingConfig(
        string uuid,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!CanAccessCharacterData(httpContext, uuid))
        {
            return Results.Forbid();
        }

        var body = await ReadBodyAsStringAsync(httpContext);
        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        List<SharingRule>? rules;
        try
        {
            rules = JsonSerializer.Deserialize<List<SharingRule>>(body, JsonOptions);
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid JSON body. Expected array of SharingRule." });
        }

        if (rules == null)
        {
            return Results.BadRequest(new { error = "Body must be a non-null array" });
        }

        // Ensure all rules have the correct owner
        foreach (var rule in rules)
        {
            rule.OwnerCharacterUUID = uuid;
            if (string.IsNullOrWhiteSpace(rule.Id))
            {
                rule.Id = Guid.NewGuid().ToString();
            }
        }

        await storage.UpsertSharingRulesAsync(uuid, rules);

        return Results.Ok(rules);
    }

    private static async Task<IResult> GetFactionSharedData(
        string uuid,
        string dataType,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        // Caller must be in the faction
        var callerCharUUID = httpContext.User.FindFirstValue("CharacterUUID");
        if (string.IsNullOrEmpty(callerCharUUID) && !IsOwner(httpContext))
        {
            return Results.Forbid();
        }

        if (!IsOwner(httpContext))
        {
            var callerChar = await storage.GetCharacterAsync(callerCharUUID!);
            if (callerChar == null || callerChar.FactionUUID != uuid)
            {
                return Results.Forbid();
            }
        }

        // Find all characters in this faction
        var allCharacters = await storage.GetAllCharactersAsync();
        var factionMembers = allCharacters
            .Where(c => c.FactionUUID == uuid)
            .ToList();

        // Aggregate data from members who share this dataType with the faction
        var aggregated = new List<JsonElement>();
        foreach (var member in factionMembers)
        {
            var rules = await storage.GetSharingRulesForCharacterAsync(member.UUID);
            var hasShared = rules.Any(r =>
                r.TargetType == SharingTargetType.Faction &&
                r.TargetUUID == uuid &&
                (r.DataType == null || r.DataType == dataType));

            if (!hasShared)
            {
                continue;
            }

            var json = await storage.GetCharacterDataAsync(member.UUID, dataType);
            if (json == null)
            {
                continue;
            }

            try
            {
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        aggregated.Add(item.Clone());
                    }
                }
            }
            catch (JsonException)
            {
                // Skip malformed data
            }
        }

        return Results.Ok(aggregated);
    }

    private static async Task<IResult> GetSharedWithMe(
        string uuid,
        string dataType,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        // Caller must be the target character (or Owner)
        if (!IsOwner(httpContext))
        {
            var callerCharUUID = httpContext.User.FindFirstValue("CharacterUUID");
            if (callerCharUUID != uuid)
            {
                return Results.Forbid();
            }
        }

        // Find all sharing rules that target this character
        var allCharacters = await storage.GetAllCharactersAsync();
        var aggregated = new List<JsonElement>();

        foreach (var character in allCharacters)
        {
            if (character.UUID == uuid)
            {
                continue;
            }

            var rules = await storage.GetSharingRulesForCharacterAsync(character.UUID);
            var hasShared = rules.Any(r =>
                r.TargetType == SharingTargetType.Character &&
                r.TargetUUID == uuid &&
                (r.DataType == null || r.DataType == dataType));

            if (!hasShared)
            {
                continue;
            }

            var json = await storage.GetCharacterDataAsync(character.UUID, dataType);
            if (json == null)
            {
                continue;
            }

            try
            {
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        aggregated.Add(item.Clone());
                    }
                }
            }
            catch (JsonException)
            {
                // Skip malformed data
            }
        }

        return Results.Ok(aggregated);
    }

    // --- Helpers ---

    private static bool IsOwner(HttpContext httpContext)
    {
        return httpContext.User.IsInRole(TokenRole.Owner.ToString());
    }

    private static bool CanAccessCharacterData(HttpContext httpContext, string characterUuid)
    {
        if (IsOwner(httpContext))
        {
            return true;
        }

        var callerCharUUID = httpContext.User.FindFirstValue("CharacterUUID");
        return callerCharUUID == characterUuid;
    }

    private static async Task<string> ReadBodyAsStringAsync(HttpContext httpContext)
    {
        using var reader = new StreamReader(httpContext.Request.Body);
        return await reader.ReadToEndAsync();
    }
}
