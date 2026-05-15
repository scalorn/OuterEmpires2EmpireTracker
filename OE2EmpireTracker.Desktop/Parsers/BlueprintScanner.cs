using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Parsers;

/// <summary>
/// Stub parser for blueprint HTML clipboard data.
/// Full implementation deferred — complex property remapping required.
/// </summary>
public sealed class BlueprintScanner
{
    private readonly ILogger<BlueprintScanner> _logger;

    public BlueprintScanner(ILogger<BlueprintScanner> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Processes HTML clipboard data and returns a Blueprint.
    /// Currently a stub — returns null until full implementation.
    /// </summary>
    public Blueprint? ProcessHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return null;
        }

        _logger.LogInformation("BlueprintScanner.ProcessHtml called (stub — not yet implemented)");
        return null;
    }
}
