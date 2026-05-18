using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Reads markdown help files from the docs/ directory and returns raw text.
/// Full HTML rendering is deferred to a future phase.
/// </summary>
public sealed class HelpRenderer
{
    private readonly ILogger<HelpRenderer> _logger;
    private string? _docsDirectory;

    public HelpRenderer(ILogger<HelpRenderer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Reads a markdown topic file and returns its raw text content.
    /// </summary>
    /// <param name="fileName">The file name within the docs/ directory.</param>
    /// <returns>The raw markdown text, or an error message if the file cannot be read.</returns>
    public string RenderTopic(string fileName)
    {
        var docsDir = FindDocsDirectory();
        if (docsDir is null)
        {
            _logger.LogWarning("Could not locate docs/ directory");
            return $"# Error\n\nCould not locate the docs/ directory.";
        }

        var filePath = Path.Combine(docsDir, fileName);
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Help file not found: {Path}", filePath);
            return $"# Not Found\n\nHelp topic '{fileName}' was not found.";
        }

        try
        {
            return File.ReadAllText(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read help file: {Path}", filePath);
            return $"# Error\n\nFailed to read help file: {ex.Message}";
        }
    }

    /// <summary>
    /// Locates the docs/ directory relative to the application assembly location.
    /// Searches upward from the assembly directory looking for a docs/ folder
    /// that contains markdown files.
    /// </summary>
    public string? FindDocsDirectory()
    {
        if (_docsDirectory is not null)
        {
            return _docsDirectory;
        }

        // Start from the assembly location and walk up
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (assemblyDir is null)
        {
            return null;
        }

        var current = new DirectoryInfo(assemblyDir);

        // Walk up to 6 levels looking for a docs/ directory with .md files
        for (int i = 0; i < 6 && current is not null; i++)
        {
            var candidate = Path.Combine(current.FullName, "docs");
            if (Directory.Exists(candidate) && Directory.GetFiles(candidate, "*.md").Length > 0)
            {
                _docsDirectory = candidate;
                _logger.LogDebug("Found docs directory at {Path}", candidate);
                return _docsDirectory;
            }

            current = current.Parent;
        }

        _logger.LogDebug("docs/ directory not found searching from {Start}", assemblyDir);
        return null;
    }
}
