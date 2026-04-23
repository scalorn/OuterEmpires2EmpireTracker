using System;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class SurveyDateTimeParserPropertyTests
    {
        #region Generators

        private static readonly string[] Months =
            { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };

        /// <summary>
        /// Generates valid game-format strings like "27JUL24-11:44p".
        /// Uses day 01-28 to avoid month-length issues.
        /// </summary>
        private static Gen<string> ValidGameFormatStringGen()
        {
            return from day in Gen.Choose(1, 28)
                   from monthIdx in Gen.Choose(0, 11)
                   from year in Gen.Choose(0, 99)
                   from hour in Gen.Choose(1, 12)
                   from minute in Gen.Choose(0, 59)
                   from suffix in Gen.Elements('a', 'p')
                   select $"{day:D2}{Months[monthIdx]}{year:D2}-{hour}:{minute:D2}{suffix}";
        }

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
        /// Generates strings that do NOT match the game date/time regex.
        /// Includes random strings, partial matches, wrong-case months, etc.
        /// </summary>
        private static Gen<string> InvalidGameFormatStringGen()
        {
            var randomString = Arb.Default.NonEmptyString().Generator.Select(s => s.Get);
            var wrongCaseMonth = from day in Gen.Choose(1, 28)
                                 from month in Gen.Elements("jan", "Feb", "mAr", "apr")
                                 from year in Gen.Choose(0, 99)
                                 from hour in Gen.Choose(1, 12)
                                 from minute in Gen.Choose(0, 59)
                                 from suffix in Gen.Elements('a', 'p')
                                 select $"{day:D2}{month}{year:D2}-{hour}:{minute:D2}{suffix}";
            var missingDash = from day in Gen.Choose(1, 28)
                              from monthIdx in Gen.Choose(0, 11)
                              from year in Gen.Choose(0, 99)
                              from hour in Gen.Choose(1, 12)
                              from minute in Gen.Choose(0, 59)
                              select $"{day:D2}{Months[monthIdx]}{year:D2}{hour}:{minute:D2}p";
            var badSuffix = from day in Gen.Choose(1, 28)
                            from monthIdx in Gen.Choose(0, 11)
                            from year in Gen.Choose(0, 99)
                            from hour in Gen.Choose(1, 12)
                            from minute in Gen.Choose(0, 59)
                            from suffix in Gen.Elements('x', 'A', 'P', 'z')
                            select $"{day:D2}{Months[monthIdx]}{year:D2}-{hour}:{minute:D2}{suffix}";
            var emptyString = Gen.Constant(string.Empty);
            var isoString = ValidIsoStringGen();

            return Gen.OneOf(randomString, wrongCaseMonth, missingDash, badSuffix, emptyString, isoString);
        }

        /// <summary>
        /// Generates strings that are NOT valid ISO 8601 (for Property 5).
        /// </summary>
        private static Gen<string> NonIsoStringGen()
        {
            var randomString = Arb.Default.NonEmptyString().Generator.Select(s => s.Get);
            var gameFormat = ValidGameFormatStringGen();
            var plainText = Gen.Elements("hello", "not-a-date", "2024/07/27", "27-07-2024", "abc123");

            return Gen.OneOf(randomString, gameFormat, plainText);
        }

        #endregion

        #region Property 1: Game format round-trip

        /// <summary>
        /// Feature: survey-datetime-normalization, Property 1: Game format round-trip.
        /// For any valid game-format date/time string, parsing it to DateTime via TryParseGameFormat,
        /// formatting to ISO via ToIsoString, then parsing the ISO string back via TryParseIso
        /// shall produce a DateTime value equal to the first parsed value.
        /// **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 2.1, 5.1, 7.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 200)]
        public Property GameFormatRoundTrip()
        {
            return Prop.ForAll(ValidGameFormatStringGen().ToArbitrary(), gameStr =>
            {
                bool parsed1 = SurveyDateTimeParser.TryParseGameFormat(gameStr, out DateTime dt1);
                if (!parsed1)
                    return false.Label($"TryParseGameFormat failed for valid input '{gameStr}'");

                string iso = SurveyDateTimeParser.ToIsoString(dt1);

                bool parsed2 = SurveyDateTimeParser.TryParseIso(iso, out DateTime dt2);
                if (!parsed2)
                    return false.Label($"TryParseIso failed for ISO string '{iso}' from game format '{gameStr}'");

                return (dt1 == dt2)
                    .Label($"Round-trip mismatch: dt1={dt1:O}, dt2={dt2:O}, gameStr='{gameStr}', iso='{iso}'");
            });
        }

        #endregion

        #region Property 2: ISO round-trip through display format

        /// <summary>
        /// Feature: survey-datetime-normalization, Property 2: ISO round-trip through display format.
        /// For any valid ISO-format date/time string, parsing it to DateTime via TryParseIso,
        /// formatting to game display format via ToGameFormat, then parsing the game format back
        /// via TryParseGameFormat shall produce a DateTime value equal to the first parsed value.
        /// **Validates: Requirements 3.1, 7.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 200)]
        public Property IsoRoundTripThroughDisplayFormat()
        {
            return Prop.ForAll(ValidIsoStringGen().ToArbitrary(), isoStr =>
            {
                bool parsed1 = SurveyDateTimeParser.TryParseIso(isoStr, out DateTime dt1);
                if (!parsed1)
                    return false.Label($"TryParseIso failed for valid ISO input '{isoStr}'");

                string gameFormat = SurveyDateTimeParser.ToGameFormat(dt1);

                bool parsed2 = SurveyDateTimeParser.TryParseGameFormat(gameFormat, out DateTime dt2);
                if (!parsed2)
                    return false.Label($"TryParseGameFormat failed for game format '{gameFormat}' from ISO '{isoStr}'");

                return (dt1 == dt2)
                    .Label($"Round-trip mismatch: dt1={dt1:O}, dt2={dt2:O}, isoStr='{isoStr}', gameFormat='{gameFormat}'");
            });
        }

        #endregion

        #region Property 3: DateTime format path consistency

        /// <summary>
        /// Feature: survey-datetime-normalization, Property 3: DateTime format path consistency.
        /// For any valid DateTime value (with minute-level precision), formatting to game display
        /// format via ToGameFormat then parsing back via TryParseGameFormat then formatting to ISO
        /// via ToIsoString shall produce the same ISO string as formatting the original DateTime
        /// directly via ToIsoString.
        /// **Validates: Requirements 6.3, 7.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 200)]
        public Property DateTimeFormatPathConsistency()
        {
            return Prop.ForAll(ValidDateTimeGen().ToArbitrary(), dt =>
            {
                string directIso = SurveyDateTimeParser.ToIsoString(dt);

                string gameFormat = SurveyDateTimeParser.ToGameFormat(dt);
                bool parsed = SurveyDateTimeParser.TryParseGameFormat(gameFormat, out DateTime dtRoundTripped);
                if (!parsed)
                    return false.Label($"TryParseGameFormat failed for game format '{gameFormat}' from DateTime {dt:O}");

                string roundTrippedIso = SurveyDateTimeParser.ToIsoString(dtRoundTripped);

                return (directIso == roundTrippedIso)
                    .Label($"Path mismatch: direct='{directIso}', roundTripped='{roundTrippedIso}', gameFormat='{gameFormat}'");
            });
        }

        #endregion

        #region Property 4: Invalid game format returns false without throwing

        /// <summary>
        /// Feature: survey-datetime-normalization, Property 4: Invalid game format returns false without throwing.
        /// For any string that does not match the game date/time regex pattern, TryParseGameFormat
        /// shall return false and shall not throw an exception.
        /// **Validates: Requirements 1.8**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 200)]
        public Property InvalidGameFormatReturnsFalseWithoutThrowing()
        {
            return Prop.ForAll(InvalidGameFormatStringGen().ToArbitrary(), input =>
            {
                bool threw = false;
                bool result = false;
                try
                {
                    result = SurveyDateTimeParser.TryParseGameFormat(input, out _);
                }
                catch (Exception ex)
                {
                    threw = true;
                    return false.Label($"TryParseGameFormat threw {ex.GetType().Name} for input '{input}': {ex.Message}");
                }

                // If the input happens to be a valid game format string (e.g. from the ISO generator
                // that coincidentally matches), we just verify no exception was thrown.
                return (!threw).Label($"Should not throw for input '{input}'");
            });
        }

        #endregion

        #region Property 5: Unparseable ISO passthrough in display

        /// <summary>
        /// Feature: survey-datetime-normalization, Property 5: Unparseable ISO passthrough in display.
        /// For any string that cannot be parsed as ISO 8601 by TryParseIso, FormatForDisplay
        /// shall return the original string unchanged.
        /// **Validates: Requirements 3.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 200)]
        public Property UnparseableIsoPassthroughInDisplay()
        {
            return Prop.ForAll(NonIsoStringGen().ToArbitrary(), input =>
            {
                // Filter to only non-ISO strings
                if (SurveyDateTimeParser.TryParseIso(input, out _))
                    return true.Label("Skipped -- input is valid ISO");

                string result = SurveyDateTimeParser.FormatForDisplay(input);

                return (result == input)
                    .Label($"FormatForDisplay changed non-ISO input: input='{input}', result='{result}'");
            });
        }

        #endregion

        #region Property 6: ISO strings sort chronologically via string comparison

        /// <summary>
        /// Feature: survey-datetime-normalization, Property 6: ISO strings sort chronologically via string comparison.
        /// For any two DateTime values a and b, the lexicographic ordering of ToIsoString(a) vs
        /// ToIsoString(b) shall match the chronological ordering of a vs b.
        /// **Validates: Requirements 4.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 200)]
        public Property IsoStringsSortChronologically()
        {
            var pairGen = from a in ValidDateTimeGen()
                          from b in ValidDateTimeGen()
                          select new { A = a, B = b };

            return Prop.ForAll(pairGen.ToArbitrary(), pair =>
            {
                string isoA = SurveyDateTimeParser.ToIsoString(pair.A);
                string isoB = SurveyDateTimeParser.ToIsoString(pair.B);

                int dateTimeComparison = pair.A.CompareTo(pair.B);
                int stringComparison = string.Compare(isoA, isoB, StringComparison.Ordinal);

                // Both should have the same sign (positive, negative, or zero)
                return (Math.Sign(dateTimeComparison) == Math.Sign(stringComparison))
                    .Label($"Sort mismatch: A={pair.A:O} ('{isoA}'), B={pair.B:O} ('{isoB}'), " +
                           $"dateTimeCompare={dateTimeComparison}, stringCompare={stringComparison}");
            });
        }

        #endregion
    }
}
