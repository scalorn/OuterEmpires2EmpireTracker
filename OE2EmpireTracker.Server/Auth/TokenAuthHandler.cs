using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Auth;

/// <summary>
/// Authentication handler that validates Bearer tokens against stored hashes.
/// </summary>
public class TokenAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "BearerToken";

    private readonly IStorageBackend _storage;

    public TokenAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IStorageBackend storage)
        : base(options, logger, encoder)
    {
        _storage = storage;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var plainToken = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(plainToken))
        {
            return AuthenticateResult.Fail("Empty bearer token");
        }

        var hash = TokenService.HashToken(plainToken);
        var apiToken = await _storage.FindTokenByHashAsync(hash);

        if (apiToken == null)
        {
            return AuthenticateResult.Fail("Invalid token");
        }

        if (apiToken.IsRevoked)
        {
            return AuthenticateResult.Fail("Token has been revoked");
        }

        // Update last-used timestamp (fire-and-forget)
        apiToken.LastUsedUtc = DateTime.UtcNow;
        _ = _storage.UpsertTokenAsync(apiToken);

        var claims = new List<Claim>
        {
            new Claim("TokenId", apiToken.Id),
            new Claim(ClaimTypes.Role, apiToken.Role.ToString()),
        };

        if (!string.IsNullOrEmpty(apiToken.CharacterUUID))
        {
            claims.Add(new Claim("CharacterUUID", apiToken.CharacterUUID));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}
