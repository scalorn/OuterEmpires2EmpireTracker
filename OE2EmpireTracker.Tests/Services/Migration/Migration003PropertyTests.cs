using System;
using System.Globalization;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services.Migration
{
    /// <summary>
    /// Property tests for Migration003_SurveyDateTimeNormalization conversion logic.
    /// Tests the migration's conversion pipeline directly (without EmpireContext/PlayerContext)
    /// by replicating the same logic inline:
    ///   1. TryParseIso → skip if already ISO
    ///   2. TryParseGameFormat → ToIsoString
    ///   3. DateTime.TryParse → ToIsoString
    ///   4. Else → DateTime.Now → ToIsoString
    /// </summary>
    [TestFixture]
    public class Migration003PropertyTests
    {
        #region Migration conversion logic (inline replica)

        /// <summary>
        /// Replicates the conversion logic from Migration003 without requiring singletons.
        /// </summary>
        private static string MigrateDateTime(string original)
        {
            // Already ISO?
            if (SurveyDateTimeParser.TryParseIso(original, out _))
                return original;

            // Try game format
            if (SurveyDateTimeParser.TryParseGameFormat(original, out DateTime parsed))
                return SurveyDateTimeParser.ToIsoString(parsed);

            // Try common .NET formats
            if (DateTime.TryParse(original, out DateTime fallback))
                return SurveyDateTimeParser.ToIsoString(fallback);

            // Unparseable or null/empty — replace with now
            return SurveyDateTimeParser.ToIsoString(DateTime.UtcNow);
        }

        #endregion

        #region Generators

        private static readonly string[] Months =
            { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };

        /// <summary>
        /// Generates valid DateTime values with minute precision (seconds=0).
        /// Constrained to years 2000-2099 to match two-digit year range.
        /// Produces UTC DateTimes (DateTimeKind.Utc).
        /// </summary>
        private static Gen<DateTime> ValidDateTimeGen()
        {
            return from year in Gen.Choose(2000, 2099)
                   from month in Gen.Choose(1, 12)
                   from day in Gen.Choose(1, 28)
                   from hour in Gen.Choose(0, 23)
                   from minute in Gen.Choose(0, 59)
                   select new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
        }

        /// <summary>
        /// Generates valid ISO strings from random DateTimes.
        /// </summary>
        private static Gen<string> ValidIsoStringGen()
        {
            return ValidDateTimeGen().Select(dt => SurveyDateTimeParser.ToIsoString(dt));
        }

        /// <summary>
        /// Generates strings that fail all three parse methods:
        /// TryParseIso, TryParseGameFormat, and DateTime.TryParse.
        /// Includes null, empty, and garbage strings.
        /// </summary>
        private static Gen<string> UnparseableStringGen()
        {
            var nullGen = Gen.Constant((string)null);
            var emptyGen = Gen.Constant(string.Empty);
            var garbage = Gen.Elements(
                "not-a-date", "ZZZZZ", "99ZZZ99-99:99x",
                "abc123!@#", "2024-13-45T99:99:99",
                "32JAN24-1:00p", "00XXX00-0:00z",
                "random garbage here", "!!!",
                "2024-02-30T12:00:00"); // invalid day for Feb

            return Gen.OneOf(nullGen, emptyGen, garbage);
        }

        /// <summary>
        /// Generates DateTime values formatted with standard .NET format strings
        /// ("G", "s", "u", "o") that DateTime.TryParse can handle.
        /// These are NOT valid ISO (our strict format) and NOT game format,
        /// but ARE parseable by DateTime.TryParse.
        /// </summary>
        private static Gen<string> CommonFormatDateStringGen()
        {
            return from dt in ValidDateTimeGen()
                   from fmt in Gen.Elements("G", "s", "u", "o")
                   select dt.ToString(fmt, CultureInfo.InvariantCulture);
        }

        #endregion

        #region Property 7: Migration is idempotent on ISO values

        /// <summary>
        /// Feature: survey-datetime-normalization, Property 7: Migration is idempotent on ISO values.
        /// For any valid ISO-format string, running the migration conversion logic
        /// (the same TryParseIso → skip path used in Migration003) shall leave the value unchanged.
        /// **Validates: Requirements 2.2, 5.6**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 200)]
        public Property MigrationIsIdempotentOnIsoValues()
        {
            return Prop.ForAll(ValidIsoStringGen().ToArbitrary(), isoStr =>
            {
                string result = MigrateDateTime(isoStr);

                return (result == isoStr)
                    .Label($"ISO value changed: input='{isoStr}', output='{result}'");
            });
        }

        #endregion

        #region Property 8: Migration produces valid ISO for unparseable input

        /// <summary>
        /// Feature: survey-datetime-normalization, Property 8: Migration produces valid ISO for unparseable input.
        /// For any string that fails TryParseGameFormat, TryParseIso, and DateTime.TryParse
        /// (including null and empty), the migration fallback shall produce a string that is
        /// a valid ISO-format date/time (parseable by TryParseIso).
        /// **Validates: Requirements 5.4, 5.5**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 200)]
        public Property MigrationProducesValidIsoForUnparseableInput()
        {
            return Prop.ForAll(UnparseableStringGen().ToArbitrary(), input =>
            {
                // Verify the input is truly unparseable by all three methods
                if (SurveyDateTimeParser.TryParseIso(input, out _))
                    return true.Label("Skipped — input is valid ISO");
                if (SurveyDateTimeParser.TryParseGameFormat(input, out _))
                    return true.Label("Skipped — input is valid game format");
                if (DateTime.TryParse(input, out _))
                    return true.Label("Skipped — input is parseable by DateTime.TryParse");

                string result = MigrateDateTime(input);

                bool isValidIso = SurveyDateTimeParser.TryParseIso(result, out _);

                return isValidIso
                    .Label($"Migration did not produce valid ISO: input='{input ?? "(null)"}', output='{result}'");
            });
        }

        #endregion

        #region Property 9: Migration common-format fallback produces valid ISO

        /// <summary>
        /// Feature: survey-datetime-normalization, Property 9: Migration common-format fallback produces valid ISO.
        /// For any DateTime value formatted using a standard .NET format string (e.g. "G", "s", "u", "o"),
        /// the migration's DateTime.TryParse fallback shall successfully parse it and produce a valid
        /// ISO-format string.
        /// **Validates: Requirements 5.2, 5.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 200)]
        public Property MigrationCommonFormatFallbackProducesValidIso()
        {
            return Prop.ForAll(CommonFormatDateStringGen().ToArbitrary(), formatted =>
            {
                // Skip if it happens to be valid ISO already (the "s" format is close)
                if (SurveyDateTimeParser.TryParseIso(formatted, out _))
                    return true.Label("Skipped — input is already valid ISO");

                string result = MigrateDateTime(formatted);

                bool isValidIso = SurveyDateTimeParser.TryParseIso(result, out _);

                return isValidIso
                    .Label($"Migration did not produce valid ISO from common format: input='{formatted}', output='{result}'");
            });
        }

        #endregion
    }
}
