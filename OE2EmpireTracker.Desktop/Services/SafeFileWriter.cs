using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Writes files using a temp-then-replace strategy to prevent data loss
/// if the process crashes or is interrupted during a write.
/// Thread-safe: a lock prevents concurrent writes from colliding on the temp file.
/// </summary>
public sealed class SafeFileWriter
{
    private static readonly object WriteLock = new object();

    private readonly ILogger<SafeFileWriter> _logger;

    public SafeFileWriter(ILogger<SafeFileWriter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Writes content to a file safely. The content is first written to a
    /// temporary file, then atomically swapped into place via File.Replace.
    /// The previous version is kept as a .bak file for one-deep recovery.
    /// </summary>
    /// <param name="filePath">The target file path.</param>
    /// <param name="content">The content to write.</param>
    /// <returns>True if the write succeeded; false otherwise.</returns>
    public bool WriteAllText(string filePath, string content)
    {
        var fullPath = Path.GetFullPath(filePath);
        var tempPath = fullPath + ".tmp";
        var backupPath = fullPath + ".bak";

        lock (WriteLock)
        {
            try
            {
                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(tempPath, content);

                if (File.Exists(fullPath))
                {
                    File.Replace(tempPath, fullPath, backupPath);
                }
                else
                {
                    File.Move(tempPath, fullPath);
                }

                _logger.LogDebug("Safe write completed: {Path}", fullPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Safe write failed: {Path}", fullPath);
                return false;
            }
        }
    }
}
