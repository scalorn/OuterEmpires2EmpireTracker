using OE2EmpireTracker.Common.Interfaces;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// Rate limit management endpoints (Owner only).
/// </summary>
public static class RateLimitEndpoints
{
    public static void MapRateLimitEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/tokens/{id}/limits")
            .RequireAuthorization("Owner");

        group.MapGet("/", GetLimits);
        group.MapPut("/", SetLimits);
    }

    private static async Task<IResult> GetLimits(
        string id,
        IStorageBackend storage)
    {
        var tokens = await storage.GetAllTokensAsync();
        var token = tokens.FirstOrDefault(t => t.Id == id);
        if (token == null)
        {
            return Results.NotFound(new { error = "Token not found" });
        }

        return Results.Ok(new
        {
            tokenId = id,
            requestsPerMinute = token.RateLimits.RequestsPerMinute,
        });
    }

    private static async Task<IResult> SetLimits(
        string id,
        SetLimitsRequest request,
        IStorageBackend storage)
    {
        if (request.RequestsPerMinute < 1)
        {
            return Results.BadRequest(new
            {
                error = "requestsPerMinute must be at least 1",
            });
        }

        var tokens = await storage.GetAllTokensAsync();
        var token = tokens.FirstOrDefault(t => t.Id == id);
        if (token == null)
        {
            return Results.NotFound(new { error = "Token not found" });
        }

        token.RateLimits.RequestsPerMinute = request.RequestsPerMinute;
        await storage.UpsertTokenAsync(token);

        return Results.Ok(new
        {
            tokenId = id,
            requestsPerMinute = token.RateLimits.RequestsPerMinute,
        });
    }

    /// <summary>Request body for PUT /api/v1/tokens/{id}/limits.</summary>
    public class SetLimitsRequest
    {
        public int RequestsPerMinute { get; set; }
    }
}