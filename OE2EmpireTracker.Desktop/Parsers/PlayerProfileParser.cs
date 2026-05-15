using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Parsers;

/// <summary>
/// Stub parser for player profile HTML clipboard data.
/// Full implementation deferred.
/// </summary>
public sealed class PlayerProfileParser
{
    private readonly ILogger<PlayerProfileParser> _logger;

    public PlayerProfileParser(ILogger<PlayerProfileParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Processes HTML clipboard data and returns a PlayerProfile.
    /// Currently a stub — returns null until full implementation.
    /// </summary>
    public PlayerProfile? ParseHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return null;
        }

        _logger.LogInformation("PlayerProfileParser.ParseHtml called (stub — not yet implemented)");
        return null;
    }
}
