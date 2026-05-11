using System.Security.Claims;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// Character CRUD endpoints.
/// </summary>
public static class CharacterEndpoints
{
    public static void MapCharacterEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/characters")
            .RequireAuthorization("Authenticated");

        group.MapPost("/", CreateCharacter);
        group.MapGet("/", GetAllCharacters);
        group.MapGet("/{uuid}", GetCharacter);
        group.MapPut("/{uuid}", UpdateCharacter);
        group.MapDelete("/{uuid}", DeleteCharacter);
    }

    private static async Task<IResult> CreateCharacter(
        CharacterRequest request,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!IsOwner(httpContext))
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "name is required" });
        }

        var uuid = FactionEndpoints.GenerateDeterministicUUID("character", request.Name);
        var existing = await storage.GetCharacterAsync(uuid);
        if (existing != null)
        {
            return Results.Conflict(new { error = "Character with this name already exists" });
        }

        var character = new ServerCharacter
        {
            UUID = uuid,
            Name = request.Name,
            Metadata = new EntityMetadata { LastModifiedUtc = DateTime.UtcNow },
        };

        await storage.UpsertCharacterAsync(character);

        return Results.Created($"/api/v1/characters/{uuid}", character);
    }

    private static async Task<IResult> GetAllCharacters(IStorageBackend storage)
    {
        var characters = await storage.GetAllCharactersAsync();
        return Results.Ok(characters);
    }

    private static async Task<IResult> GetCharacter(string uuid, IStorageBackend storage)
    {
        var character = await storage.GetCharacterAsync(uuid);
        if (character == null)
        {
            return Results.NotFound(new { error = "Character not found" });
        }

        return Results.Ok(character);
    }

    private static async Task<IResult> UpdateCharacter(
        string uuid,
        CharacterUpdateRequest request,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        var character = await storage.GetCharacterAsync(uuid);
        if (character == null)
        {
            return Results.NotFound(new { error = "Character not found" });
        }

        // Owner can update any; Character can only update own
        if (!IsOwner(httpContext))
        {
            var callerCharUUID = httpContext.User.FindFirstValue("CharacterUUID");
            if (callerCharUUID != uuid)
            {
                return Results.Forbid();
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            character.Name = request.Name;
        }

        if (request.FactionUUID != null)
        {
            if (request.FactionUUID == string.Empty)
            {
                // Allow clearing faction assignment
                character.FactionUUID = null;
            }
            else
            {
                // Validate faction exists
                var faction = await storage.GetFactionAsync(request.FactionUUID);
                if (faction == null)
                {
                    return Results.BadRequest(new { error = "factionUUID does not exist" });
                }

                character.FactionUUID = request.FactionUUID;
            }
        }

        character.Metadata.LastModifiedUtc = DateTime.UtcNow;
        await storage.UpsertCharacterAsync(character);

        return Results.Ok(character);
    }

    private static async Task<IResult> DeleteCharacter(
        string uuid,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!IsOwner(httpContext))
        {
            return Results.Forbid();
        }

        var character = await storage.GetCharacterAsync(uuid);
        if (character == null)
        {
            return Results.NotFound(new { error = "Character not found" });
        }

        await storage.DeleteCharacterAsync(uuid);

        return Results.NoContent();
    }

    private static bool IsOwner(HttpContext httpContext)
    {
        return httpContext.User.IsInRole(TokenRole.Owner.ToString());
    }

    // --- Request DTOs ---

    /// <summary>Request body for character creation.</summary>
    public class CharacterRequest
    {
        public string? Name { get; set; }
    }

    /// <summary>Request body for character update.</summary>
    public class CharacterUpdateRequest
    {
        public string? Name { get; set; }
        public string? FactionUUID { get; set; }
    }
}
