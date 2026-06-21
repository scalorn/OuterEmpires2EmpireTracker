using System.Security.Cryptography;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Server.Auth;

/// <summary>
/// Provides token generation, hashing, and owner-token bootstrapping.
/// </summary>
public static class TokenService
{
    /// <summary>
    /// Generates a cryptographically random 32-byte base64url-encoded token.
    /// </summary>
    public static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Returns the SHA-256 hash of the given token as a lowercase hex string.
    /// </summary>
    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Ensures an Owner token exists in storage. If not, generates one,
    /// logs it to the console (one-time display), and persists the hash.
    /// </summary>
    public static async Task EnsureOwnerTokenAsync(IStorageBackend storage, ILogger logger)
    {
        var tokens = await storage.GetAllTokensAsync();
        var ownerToken = tokens.FirstOrDefault(t => t.Role == TokenRole.Owner && !t.IsRevoked);

        if (ownerToken != null)
        {
            logger.LogInformation("Owner token already exists (ID: {Id})", ownerToken.Id);
            return;
        }

        var plaintext = GenerateToken();
        var hash = HashToken(plaintext);

        var token = new ApiToken
        {
            Id = Guid.NewGuid().ToString("N")[..12],
            TokenHash = hash,
            Role = TokenRole.Owner,
            CreatedUtc = SystemClock.UtcNow,
        };

        await storage.UpsertTokenAsync(token);

        logger.LogWarning(
            "=== OWNER TOKEN (save this — it will NOT be shown again) ===");
        logger.LogWarning("Token: {Token}", plaintext);
        logger.LogWarning(
            "==============================================================");
    }

    /// <summary>
    /// Regenerates the owner token: revokes existing, creates new, prints it, then exits.
    /// </summary>
    public static async Task RegenerateOwnerTokenAsync(IStorageBackend storage, ILogger logger)
    {
        var tokens = await storage.GetAllTokensAsync();
        foreach (var existing in tokens.Where(t => t.Role == TokenRole.Owner))
        {
            existing.IsRevoked = true;
            await storage.UpsertTokenAsync(existing);
        }

        var plaintext = GenerateToken();
        var hash = HashToken(plaintext);

        var token = new ApiToken
        {
            Id = Guid.NewGuid().ToString("N")[..12],
            TokenHash = hash,
            Role = TokenRole.Owner,
            CreatedUtc = SystemClock.UtcNow,
        };

        await storage.UpsertTokenAsync(token);

        logger.LogWarning(
            "=== NEW OWNER TOKEN (save this — it will NOT be shown again) ===");
        logger.LogWarning("Token: {Token}", plaintext);
        logger.LogWarning(
            "==================================================================");
    }
}