using System;
using System.Collections.Generic;

namespace OE2EmpireTracker.Parsers
{
    /// <summary>
    /// Parses countdown format strings (e.g. "5d 0h 0m 0s", "2h 30m", "60s")
    /// back to total seconds. This is the inverse of ActivityRow.FormatSeconds().
    /// </summary>
    public static class CountdownFormatParser
    {
        private static readonly Dictionary<char, long> UnitMultipliers = new Dictionary<char, long>
        {
            { 'd', 86400L },
            { 'h', 3600L },
            { 'm', 60L },
            { 's', 1L }
        };

        /// <summary>
        /// Attempts to parse a countdown format string into total seconds.
        /// Accepts space-separated tokens with unit suffixes (d, h, m, s),
        /// partial formats, any order, no duplicate units.
        /// </summary>
        /// <param name="input">The countdown format string to parse.</param>
        /// <param name="totalSeconds">The parsed total seconds, or 0 on failure.</param>
        /// <returns>True if parsing succeeded; false otherwise.</returns>
        public static bool TryParse(string input, out long totalSeconds)
        {
            totalSeconds = 0;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            var tokens = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
                return false;

            var seenUnits = new HashSet<char>();
            long accumulated = 0;

            foreach (var token in tokens)
            {
                if (token.Length < 2)
                    return false;

                char unit = token[token.Length - 1];
                string numberPart = token.Substring(0, token.Length - 1);

                if (!UnitMultipliers.ContainsKey(unit))
                    return false;

                if (!long.TryParse(numberPart, out long value))
                    return false;

                if (value < 0)
                    return false;

                if (!seenUnits.Add(unit))
                    return false; // duplicate unit

                try
                {
                    checked
                    {
                        accumulated += value * UnitMultipliers[unit];
                    }
                }
                catch (OverflowException)
                {
                    return false;
                }
            }

            totalSeconds = accumulated;
            return true;
        }
    }
}
