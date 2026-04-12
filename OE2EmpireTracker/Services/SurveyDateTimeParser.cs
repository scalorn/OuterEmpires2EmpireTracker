using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace OE2EmpireTracker.Services
{
    public static class SurveyDateTimeParser
    {
        public const string IsoFormat = "yyyy-MM-ddTHH:mm:ssZ";

        private static readonly Regex GameFormatRegex = new Regex(
            @"^(\d{2})([A-Z]{3})(\d{2})-(\d{1,2}):(\d{2})([ap])$",
            RegexOptions.Compiled);

        private static readonly Dictionary<string, int> MonthLookup = new Dictionary<string, int>
        {
            { "JAN", 1 }, { "FEB", 2 }, { "MAR", 3 }, { "APR", 4 },
            { "MAY", 5 }, { "JUN", 6 }, { "JUL", 7 }, { "AUG", 8 },
            { "SEP", 9 }, { "OCT", 10 }, { "NOV", 11 }, { "DEC", 12 }
        };

        private static readonly Dictionary<int, string> ReverseMonthLookup =
            MonthLookup.ToDictionary(kv => kv.Value, kv => kv.Key);

        /// <summary>
        /// Parses a game-format date/time string (e.g. "27JUL24-11:44p") into a DateTime.
        /// Returns false on any mismatch — never throws.
        /// </summary>
        public static bool TryParseGameFormat(string input, out DateTime result)
        {
            result = default;
            if (string.IsNullOrEmpty(input))
                return false;

            var match = GameFormatRegex.Match(input);
            if (!match.Success)
                return false;

            int day = int.Parse(match.Groups[1].Value);
            string monthAbbr = match.Groups[2].Value;
            int yearShort = int.Parse(match.Groups[3].Value);
            int hour = int.Parse(match.Groups[4].Value);
            int minute = int.Parse(match.Groups[5].Value);
            char ampm = match.Groups[6].Value[0];

            if (!MonthLookup.TryGetValue(monthAbbr, out int month))
                return false;

            int year = 2000 + yearShort;

            // 12-hour to 24-hour conversion
            int hour24;
            if (hour == 12 && ampm == 'a')
                hour24 = 0;   // 12a = midnight
            else if (hour == 12 && ampm == 'p')
                hour24 = 12;  // 12p = noon
            else if (ampm == 'p')
                hour24 = hour + 12;
            else
                hour24 = hour;

            try
            {
                result = new DateTime(year, month, day, hour24, minute, 0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Parses an ISO 8601 date/time string (e.g. "2024-07-27T23:44:00Z") into a UTC DateTime.
        /// Accepts both Z-suffixed and non-Z-suffixed strings for backward compatibility.
        /// </summary>
        public static bool TryParseIso(string input, out DateTime result)
        {
            result = default;
            if (string.IsNullOrEmpty(input))
                return false;

            string[] formats = { "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-ddTHH:mm:ss" };
            return DateTime.TryParseExact(input, formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out result);
        }

        /// <summary>
        /// Formats a DateTime as an ISO 8601 string (e.g. "2024-07-27T23:44:00").
        /// </summary>
        public static string ToIsoString(DateTime dt)
        {
            return dt.ToString(IsoFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formats a DateTime in the game display format (e.g. "27JUL24-11:44p").
        /// </summary>
        public static string ToGameFormat(DateTime dt)
        {
            string day = dt.Day.ToString("D2");
            string month = ReverseMonthLookup[dt.Month];
            string year = (dt.Year % 100).ToString("D2");

            int hour12;
            char suffix;
            if (dt.Hour == 0)
            {
                hour12 = 12;
                suffix = 'a';
            }
            else if (dt.Hour < 12)
            {
                hour12 = dt.Hour;
                suffix = 'a';
            }
            else if (dt.Hour == 12)
            {
                hour12 = 12;
                suffix = 'p';
            }
            else
            {
                hour12 = dt.Hour - 12;
                suffix = 'p';
            }

            string minute = dt.Minute.ToString("D2");
            return $"{day}{month}{year}-{hour12}:{minute}{suffix}";
        }

        /// <summary>
        /// Converts an ISO string to game display format. Converts UTC to local time before formatting.
        /// Returns the original string unchanged on failure.
        /// </summary>
        public static string FormatForDisplay(string isoString)
        {
            if (TryParseIso(isoString, out DateTime dt))
                return ToGameFormat(dt.ToLocalTime());
            return isoString;
        }

        /// <summary>
        /// Attempts to parse a date/time string using game format, then ISO, then DateTime.TryParse.
        /// </summary>
        public static bool TryParseAny(string input, out DateTime result)
        {
            if (TryParseGameFormat(input, out result))
                return true;
            if (TryParseIso(input, out result))
                return true;
            if (DateTime.TryParse(input, out result))
                return true;

            result = default;
            return false;
        }
    }
}
