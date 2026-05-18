using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Provides platform-appropriate file system paths for data, logs, and configuration.
/// </summary>
public interface IFileSystemService
{
    /// <summary>Gets the directory for application data files (PlayerData.json, BaselineData.json).</summary>
    string GetDataDirectory();

    /// <summary>Gets the directory for log files.</summary>
    string GetLogDirectory();

    /// <summary>Gets the directory for configuration/preferences.</summary>
    string GetConfigDirectory();

    /// <summary>Ensures a directory exists, creating it if necessary.</summary>
    void EnsureDirectoryExists(string path);
}

/// <summary>
/// Platform-aware file system service. Resolves paths based on OS conventions.
/// </summary>
public sealed class FileSystemService : IFileSystemService
{
    private const string AppName = "OE2EmpireTracker";

    public string GetDataDirectory()
    {
        string basePath;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // XDG_DATA_HOME or ~/.local/share
            basePath = Environment.GetEnvironmentVariable("XDG_DATA_HOME")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        }
        else
        {
            // macOS: ~/Library/Application Support
            basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support");
        }

        return Path.Combine(basePath, AppName);
    }

    public string GetLogDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var cacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache");
            return Path.Combine(cacheHome, AppName, "logs");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Logs", AppName);
        }

        // Windows: %APPDATA%/OE2EmpireTracker/logs
        return Path.Combine(GetDataDirectory(), "logs");
    }

    public string GetConfigDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
            return Path.Combine(configHome, AppName);
        }

        // Windows and macOS: same as data directory
        return GetDataDirectory();
    }

    public void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
