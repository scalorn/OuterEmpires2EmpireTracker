using OE2EmpireTracker.Server.Auth;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// Token management endpoints (Owner only).
/// </summary>
public static class TokenEndpoints
{
    public static void MapTokenEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/tokens")
            .RequireAuthorization("Owner");

        group.MapPost("/", CreateToken);
        group.MapGet("/", ListTokens);
        group.MapDelete("/{id}", RevokeToken);
        group.MapPost("/{id}/regenerate", RegenerateToken);
    }

    private static async Task<IResult> CreateToken(
        CreateTokenRequest request,
        IStorageBackend storage)
    {
        if (string.IsNullOrWhiteSpace(request.CharacterName))
        {
            return Results.BadRequest(new { error = "characterName is required" });
        }

        var characterUUID = Guid.NewGuid().ToString("N")[..16];
        var character = new ServerCharacter
        {
            UUID = characterUUID,
            Name = request.CharacterName,
            Metadata = new EntityMetadata { LastModifiedUtc = DateTime.UtcNow },
        };

        await storage.UpsertCharacterAsync(character);

        var plainToken = TokenService.GenerateToken();
        var hash = TokenService.HashToken(plainToken);
        var tokenId = Guid.NewGuid().ToString("N")[..12];

        var apiToken = new ApiToken
        {
            Id = tokenId,
            TokenHash = hash,
            CharacterUUID = characterUUID,
            Role = TokenRole.Character,
            CreatedUtc = DateTime.UtcNow,
        };

        await storage.UpsertTokenAsync(apiToken);

        return Results.Ok(new
        {
            tokenId,
            token = plainToken,
            characterUUID,
        });
    }

    private static async Task<IResult> ListTokens(IStorageBackend storage)
    {
        var tokens = await storage.GetAllTokensAsync();
        var result = tokens.Select(t => new
        {
            id = t.Id,
            characterUUID = t.CharacterUUID,
            role = t.Role.ToString(),
            created = t.CreatedUtc,
            lastUsed = t.LastUsedUtc,
            isRevoked = t.IsRevoked,
        });

        return Results.Ok(result);
    }

    private static async Task<IResult> RevokeToken(string id, IStorageBackend storage)
    {
        var tokens = await storage.GetAllTokensAsync();
        var token = tokens.FirstOrDefault(t => t.Id == id);
        if (token == null)
        {
            return Results.NotFound(new { error = "Token not found" });
        }

        token.IsRevoked = true;
        await storage.UpsertTokenAsync(token);

        return Results.Ok(new { id, revoked = true });
    }

    private static async Task<IResult> RegenerateToken(string id, IStorageBackend storage)
    {
        var tokens = await storage.GetAllTokensAsync();
        var existing = tokens.FirstOrDefault(t => t.Id == id);
        if (existing == null)
        {
            return Results.NotFound(new { error = "Token not found" });
        }

        // Revoke old token
        existing.IsRevoked = true;
        await storage.UpsertTokenAsync(existing);

        // Generate new token with same character/role
        var plainToken = TokenService.GenerateToken();
        var hash = TokenService.HashToken(plainToken);
        var newTokenId = Guid.NewGuid().ToString("N")[..12];

        var newToken = new ApiToken
        {
            Id = newTokenId,
            TokenHash = hash,
            CharacterUUID = existing.CharacterUUID,
            Role = existing.Role,
            FactionUUID = existing.FactionUUID,
            CreatedUtc = DateTime.UtcNow,
        };

        await storage.UpsertTokenAsync(newToken);

        return Results.Ok(new
        {
            tokenId = newTokenId,
            token = plainToken,
        });
    }

    /// <summary>Request body for POST /api/v1/tokens.</summary>
    public class CreateTokenRequest
    {
        public string? CharacterName { get; set; }
    }
}
