using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Provides clipboard access for HTML paste import and text operations.
/// Uses Avalonia's built-in clipboard API which handles X11/Wayland/Windows differences.
/// </summary>
public interface IClipboardService
{
    /// <summary>Gets HTML content from the clipboard.</summary>
    Task<string?> GetHtmlAsync();

    /// <summary>Gets plain text from the clipboard.</summary>
    Task<string?> GetTextAsync();

    /// <summary>Sets plain text on the clipboard.</summary>
    Task SetTextAsync(string text);
}

/// <summary>
/// Avalonia-based clipboard service. Requires a TopLevel (window) reference.
/// </summary>
public sealed class ClipboardService : IClipboardService
{
    public async Task<string?> GetHtmlAsync()
    {
        var clipboard = GetClipboard();
        if (clipboard is null)
        {
            return null;
        }

        // Try to get clipboard data and look for HTML format
        var dataTransfer = await clipboard.TryGetDataAsync();
        if (dataTransfer is not null)
        {
            foreach (var item in dataTransfer.Items)
            {
                foreach (var format in item.Formats)
                {
                    if (format.Identifier.Contains("html", System.StringComparison.OrdinalIgnoreCase))
                    {
                        var data = await item.TryGetRawAsync(format);
                        if (data is string html)
                        {
                            return html;
                        }
                    }
                }
            }
        }

        // Fall back to text
        return await clipboard.TryGetTextAsync();
    }

    public async Task<string?> GetTextAsync()
    {
        var clipboard = GetClipboard();
        if (clipboard is null)
        {
            return null;
        }

        return await clipboard.TryGetTextAsync();
    }

    public async Task SetTextAsync(string text)
    {
        var clipboard = GetClipboard();
        if (clipboard is null)
        {
            return;
        }

        await clipboard.SetTextAsync(text);
    }

    private static IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.Clipboard;
        }

        return null;
    }
}
