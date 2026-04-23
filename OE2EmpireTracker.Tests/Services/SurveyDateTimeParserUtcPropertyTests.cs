using System;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Feature: colony-import-timestamp, Property 6: SurveyDateTimeParser stores UTC and displays local
    ///
    /// For any valid UTC DateTime, ToIsoString should produce a string ending with Z.
    /// Parsing that string back via TryParseIso should produce a DateTime with Kind == DateTimeKind.Utc
    /// and the same value. FormatForDisplay should convert to local time before formatting, so the
    /// game-format output reflects the user's timezone.
    ///
    /// **Validates: Requirements 7.3, 7.4, 7.5, 7.6**
    /// </summary>
    [TestFixture]
    public class SurveyDateTimeParserUtcPropertyTests
    {
        /// <summary>
        /// Generates valid UTC DateTime values with minute precision (seconds=0).
        /// Constrained to years 2000-2099 to match two-digit year range.
        /// </summary>
        private static Gen<DateTime> ValidUtcDateTimeGen()
        {
            return from year in Gen.Choose(2000, 2099)
                   from month in Gen.Choose(1, 12)
                   from day in Gen.Choose(1, 28)
                   from hour in Gen.Choose(0, 23)
                   from minute in Gen.Choose(0, 59)
                   select new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
        }

        /// <summary>
        /// Feature: colony-import-timestamp, Property 6: SurveyDateTimeParser stores UTC and displays local
        ///
        /// Sub-property 6a: ToIsoString produces a string ending with Z for any UTC DateTime.
        /// **Validates: Requirements 7.3, 7.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ToIsoString_ProducesZSuffix()
        {
            return Prop.ForAll(ValidUtcDateTimeGen().ToArbitrary(), utcDt =>
            {
                string iso = SurveyDateTimeParser.ToIsoString(utcDt);

                return iso.EndsWith("Z")
                    .Label($"ToIsoString did not end with Z: '{iso}' for input {utcDt:O}");
            });
        }

        /// <summary>
        /// Feature: colony-import-timestamp, Property 6: SurveyDateTimeParser stores UTC and displays local
        ///
        /// Sub-property 6b: Round-trip through ToIsoString then TryParseIso preserves UTC kind and value.
        /// **Validates: Requirements 7.3, 7.4, 7.5**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property IsoRoundTrip_PreservesUtcKindAndValue()
        {
            return Prop.ForAll(ValidUtcDateTimeGen().ToArbitrary(), utcDt =>
            {
                string iso = SurveyDateTimeParser.ToIsoString(utcDt);
                bool parsed = SurveyDateTimeParser.TryParseIso(iso, out DateTime result);

                if (!parsed)
                    return false.Label($"TryParseIso failed for '{iso}'");

                bool kindIsUtc = result.Kind == DateTimeKind.Utc;
                bool valueMatches = result == utcDt;

                return (kindIsUtc && valueMatches)
                    .Label($"Kind={result.Kind} (expected Utc), value={result:O} (expected {utcDt:O}), iso='{iso}'");
            });
        }

        /// <summary>
        /// Feature: colony-import-timestamp, Property 6: SurveyDateTimeParser stores UTC and displays local
        ///
        /// Sub-property 6c: FormatForDisplay converts UTC to local time before formatting.
        /// The game-format output should match ToGameFormat applied to the local-time equivalent.
        /// **Validates: Requirements 7.5, 7.6**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property FormatForDisplay_ConvertsToLocalBeforeFormatting()
        {
            return Prop.ForAll(ValidUtcDateTimeGen().ToArbitrary(), utcDt =>
            {
                string iso = SurveyDateTimeParser.ToIsoString(utcDt);
                string displayResult = SurveyDateTimeParser.FormatForDisplay(iso);

                // The expected result is ToGameFormat applied to the local-time equivalent
                DateTime localDt = utcDt.ToLocalTime();
                string expectedDisplay = SurveyDateTimeParser.ToGameFormat(localDt);

                return (displayResult == expectedDisplay)
                    .Label($"FormatForDisplay='{displayResult}', expected='{expectedDisplay}', " +
                           $"utc={utcDt:O}, local={localDt:O}, iso='{iso}'");
            });
        }
    }
}
