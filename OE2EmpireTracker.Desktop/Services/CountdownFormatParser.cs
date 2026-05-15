using System;
using System.Text;
using System.Text.RegularExpressions;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Parses and formats countdown durations between seconds and human-readable
/// "Xd Yh Zm Ws" format. Supports round-trip: Parse(Format(x)) == x.
/// </summary>
public static class CountdownFormatParser
{
    private static readonly Regex TokenPattern = new Regex(
        @"(\d+)\s*([dhms])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Formats a duration in seconds to "Xd Yh Zm Ws" format.
    /// </summary>
    /// <param name="totalSeconds">The total seconds to format. Must be non-negative.</param>
    /// <returns>A formatted string like "2d 0h 0m 0s".</returns>
    public static string FormatSeconds(long totalSeconds)
    {
        if (totalSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalSeconds), "Value must be non-negative.");
        }

        long days = totalSeconds / 86400;
        long remainder = totalSeconds % 86400;
        long hours = remainder / 3600;
        remainder %= 3600;
        long minutes = remainder / 60;
        long seconds = remainder % 60;

        var sb = new StringBuilder();
        sb.Append(days).Append("d ");
        sb.Append(hours).Append("h ");
        sb.Append(minutes).Append("m ");
        sb.Append(seconds).Append('s');
        return sb.ToString();
    }

    /// <summary>
    /// Attempts to parse a countdown format string back to total seconds.
    /// Accepts "Xd Yh Zm Ws" with optional components, any order.
    /// Rejects duplicate units, negative values, and non-numeric tokens.
    /// </summary>
    /// <param name="input">The input string to parse.</param>
    /// <param name="totalSeconds">The parsed total seconds, or 0 if parsing fails.</param>
    /// <returns>True if parsing succeeded; false otherwise.</returns>
    public static bool TryParse(string input, out long totalSeconds)
    {
        totalSeconds = 0;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var matches = TokenPattern.Matches(input);
        if (matches.Count == 0)
        {
            return false;
        }

        bool hasD = false, hasH = false, hasM = false, hasS = false;
        long result = 0;

        foreach (Match match in matches)
        {
            if (!long.TryParse(match.Groups[1].Value, out long value))
            {
                return false;
            }

            if (value < 0)
            {
                return false;
            }

            char unit = char.ToLowerInvariant(match.Groups[2].Value[0]);

            switch (unit)
            {
                case 'd':
                    if (hasD)
                    {
                        return false;
                    }

                    hasD = true;
                    result += value * 86400;
                    break;
                case 'h':
                    if (hasH)
                    {
                        return false;
                    }

                    hasH = true;
                    result += value * 3600;
                    break;
                case 'm':
                    if (hasM)
                    {
                        return false;
                    }

                    hasM = true;
                    result += value * 60;
                    break;
                case 's':
                    if (hasS)
                    {
                        return false;
                    }

                    hasS = true;
                    result += value;
                    break;
                default:
                    return false;
            }
        }

        // Check for overflow
        if (result < 0)
        {
            return false;
        }

        totalSeconds = result;
        return true;
    }
}
