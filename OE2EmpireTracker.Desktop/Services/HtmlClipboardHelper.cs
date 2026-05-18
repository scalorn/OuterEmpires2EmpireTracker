using System;
using System.Text;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Extracts HTML fragments from clipboard data.
/// Replaces the WinForms ClipboardHelper with a platform-independent implementation.
/// </summary>
public static class HtmlClipboardHelper
{
    private const string StartMarker = "<!--StartFragment-->";
    private const string EndMarker = "<!--EndFragment-->";

    /// <summary>
    /// Extracts the HTML fragment from clipboard data by parsing the clipboard header.
    /// Supports both marker-based (StartFragment/EndFragment comments) and byte-offset extraction.
    /// If neither format is detected, returns the raw HTML unchanged.
    /// </summary>
    public static string ExtractHtmlFragment(string clipboardData)
    {
        if (string.IsNullOrEmpty(clipboardData))
        {
            return string.Empty;
        }

        // Prefer marker-based extraction (encoding-safe)
        int startPos = clipboardData.IndexOf(StartMarker, StringComparison.Ordinal);
        if (startPos >= 0)
        {
            startPos += StartMarker.Length;
            int endPos = clipboardData.IndexOf(EndMarker, startPos, StringComparison.Ordinal);
            if (endPos >= 0)
            {
                return clipboardData.Substring(startPos, endPos - startPos);
            }
        }

        // Fallback: byte-offset extraction for Windows clipboard format
        int startFragmentIndex = clipboardData.IndexOf("StartFragment:", StringComparison.Ordinal);
        if (startFragmentIndex >= 0)
        {
            return ExtractByByteOffset(clipboardData);
        }

        // No clipboard markers found — return raw HTML
        return clipboardData;
    }

    private static string ExtractByByteOffset(string clipboardData)
    {
        int startFragmentIndex = clipboardData.IndexOf("StartFragment:", StringComparison.Ordinal);
        if (startFragmentIndex < 0)
        {
            return clipboardData;
        }

        string startValue = clipboardData.Substring(
            startFragmentIndex + "StartFragment:".Length, 10);
        if (!int.TryParse(startValue, out int startOffset) || startOffset < 0)
        {
            return clipboardData;
        }

        int endFragmentIndex = clipboardData.IndexOf("EndFragment:", StringComparison.Ordinal);
        if (endFragmentIndex < 0)
        {
            return clipboardData;
        }

        string endValue = clipboardData.Substring(
            endFragmentIndex + "EndFragment:".Length, 10);
        if (!int.TryParse(endValue, out int endOffset))
        {
            return clipboardData;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(clipboardData);
        if (startOffset > bytes.Length)
        {
            return clipboardData;
        }

        if (endOffset > bytes.Length)
        {
            endOffset = bytes.Length;
        }

        return Encoding.UTF8.GetString(bytes, startOffset, endOffset - startOffset);
    }
}
