using System.Collections.Concurrent;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Server.Storage;

using OE2EmpireTracker.Services;
namespace OE2EmpireTracker.Server.Middleware;

/// <summary>
/// Tracks request timestamps for sliding-window rate limiting.
/// </summary>
internal sealed class TokenBucket
{
    private readonly object _lock = new object();
    private readonly List<DateTime> _timestamps = new List<DateTime>();

    public int MaxRequestsPerMinute { get; set; } = 300;

    /// <summary>
    /// Attempts to consume a request slot. Returns true if allowed.
    /// </summary>
    public bool TryConsume()
    {
        lock (_lock)
        {
            var cutoff = SystemClock.UtcNow.AddMinutes(-1);
            _timestamps.RemoveAll(t => t < cutoff);

            if (_timestamps.Count >= MaxRequestsPerMinute)
            {
                return false;
            }

            _timestamps.Add(SystemClock.UtcNow);
            return true;
        }
    }

    /// <summary>
    /// Gets the number of seconds until the next request slot opens.
    /// </summary>
    public int GetRetryAfterSeconds()
    {
        lock (_lock)
        {
            if (_timestamps.Count == 0)
            {
                return 0;
            }

            var oldest = _timestamps[0];
            var available = oldest.AddMinutes(1) - SystemClock.UtcNow;
            return Math.Max(1, (int)Math.Ceiling(available.TotalSeconds));
        }
    }
}

/// <summary>
/// Sliding-window rate limiter middleware. Limits requests per token.
/// Owner tokens are exempt from rate limiting.
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new ConcurrentDictionary<string, TokenBucket>();

    public RateLimitMiddleware(
        RequestDelegate next,
        ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tokenId = context.User?.FindFirst("TokenId")?.Value;

        // No token = unauthenticated request, skip rate limiting
        if (string.IsNullOrEmpty(tokenId))
        {
            await _next(context);
            return;
        }

        // Owner tokens are exempt from rate limiting
        var role = context.User?.FindFirst(
            System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role == TokenRole.Owner.ToString())
        {
            await _next(context);
            return;
        }

        var bucket = _buckets.GetOrAdd(tokenId, _ => new TokenBucket());

        // Sync bucket limit from token's stored config
        await SyncBucketLimit(context, tokenId, bucket);

        if (!bucket.TryConsume())
        {
            var retryAfter = bucket.GetRetryAfterSeconds();
            _logger.LogWarning(
                "Rate limit exceeded for token {TokenId}",
                tokenId);

            context.Response.StatusCode = 429;
            context.Response.Headers.RetryAfter = retryAfter.ToString();
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                retryAfterSeconds = retryAfter,
            });
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Updates the rate limit for a specific token (called from endpoints).
    /// </summary>
    public void UpdateLimit(string tokenId, int requestsPerMinute)
    {
        var bucket = _buckets.GetOrAdd(tokenId, _ => new TokenBucket());
        bucket.MaxRequestsPerMinute = requestsPerMinute;
    }

    private static async Task SyncBucketLimit(
        HttpContext context,
        string tokenId,
        TokenBucket bucket)
    {
        var storage = context.RequestServices.GetRequiredService<IStorageBackend>();
        var tokens = await storage.GetAllTokensAsync();
        var token = tokens.FirstOrDefault(t => t.Id == tokenId);

        if (token != null)
        {
            bucket.MaxRequestsPerMinute = token.RateLimits.RequestsPerMinute;
        }
    }
}
