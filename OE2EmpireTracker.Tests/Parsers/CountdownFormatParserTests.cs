using System;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Parsers
{
    [TestFixture]
    public class CountdownFormatParserTests
    {
        [Test]
        public void TryParse_FullFormat_ReturnsCorrectSeconds()
        {
            // "5d 0h 0m 0s" -> 5 * 86400 = 432000
            bool result = CountdownFormatParser.TryParse("5d 0h 0m 0s", out long totalSeconds);
            Assert.That(result, Is.True);
            Assert.That(totalSeconds, Is.EqualTo(432000L));
        }

        [Test]
        public void TryParse_HoursAndMinutes_ReturnsCorrectSeconds()
        {
            // "1h 30m" -> 3600 + 1800 = 5400
            bool result = CountdownFormatParser.TryParse("1h 30m", out long totalSeconds);
            Assert.That(result, Is.True);
            Assert.That(totalSeconds, Is.EqualTo(5400L));
        }

        [Test]
        public void TryParse_SecondsOnly_ReturnsCorrectSeconds()
        {
            // "60s" -> 60
            bool result = CountdownFormatParser.TryParse("60s", out long totalSeconds);
            Assert.That(result, Is.True);
            Assert.That(totalSeconds, Is.EqualTo(60L));
        }

        [Test]
        public void TryParse_ZeroSeconds_ReturnsZero()
        {
            // "0s" -> 0
            bool result = CountdownFormatParser.TryParse("0s", out long totalSeconds);
            Assert.That(result, Is.True);
            Assert.That(totalSeconds, Is.EqualTo(0L));
        }

        [Test]
        public void TryParse_EmptyString_ReturnsFalse()
        {
            bool result = CountdownFormatParser.TryParse(string.Empty, out long totalSeconds);
            Assert.That(result, Is.False);
            Assert.That(totalSeconds, Is.EqualTo(0L));
        }

        [Test]
        public void TryParse_InvalidText_ReturnsFalse()
        {
            bool result = CountdownFormatParser.TryParse("abc", out long totalSeconds);
            Assert.That(result, Is.False);
            Assert.That(totalSeconds, Is.EqualTo(0L));
        }

        [Test]
        public void TryParse_DuplicateUnit_ReturnsFalse()
        {
            // "5d 3d" has duplicate 'd' unit
            bool result = CountdownFormatParser.TryParse("5d 3d", out long totalSeconds);
            Assert.That(result, Is.False);
            Assert.That(totalSeconds, Is.EqualTo(0L));
        }

        [Test]
        public void TryParse_NegativeNumber_ReturnsFalse()
        {
            bool result = CountdownFormatParser.TryParse("-5d", out long totalSeconds);
            Assert.That(result, Is.False);
            Assert.That(totalSeconds, Is.EqualTo(0L));
        }

        [Test]
        public void TryParse_Null_ReturnsFalse()
        {
            bool result = CountdownFormatParser.TryParse(null, out long totalSeconds);
            Assert.That(result, Is.False);
            Assert.That(totalSeconds, Is.EqualTo(0L));
        }

        [Test]
        public void TryParse_WhitespaceOnly_ReturnsFalse()
        {
            bool result = CountdownFormatParser.TryParse("   ", out long totalSeconds);
            Assert.That(result, Is.False);
            Assert.That(totalSeconds, Is.EqualTo(0L));
        }

        #region Property Tests

        /// <summary>
        /// Feature: preferences-form, Property 1: Countdown format round-trip
        ///
        /// For any non-negative long value s (where s >= 0), parsing the output of
        /// ActivityRow.FormatSeconds(s) with CountdownFormatParser.TryParse shall
        /// produce the original value s.
        ///
        /// **Validates: Requirements 8.1, 8.2, 8.3, 8.5**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CountdownFormatRoundTrip()
        {
            var gen = Gen.Choose(0, 10_000_000).Select(i => (long)i);

            return Prop.ForAll(gen.ToArbitrary(), s =>
            {
                string formatted = ActivityRow.FormatSeconds(s);
                bool parsed = CountdownFormatParser.TryParse(formatted, out long result);

                return (parsed && result == s)
                    .Label($"s={s}, formatted=\"{formatted}\", parsed={parsed}, result={result}");
            });
        }

        /// <summary>
        /// Feature: preferences-form, Property 2: Countdown format rejects invalid input
        ///
        /// For any string that does not conform to the countdown format pattern
        /// (contains no valid Xd, Yh, Zm, or Ws tokens, or contains negative numbers,
        /// non-numeric characters in token positions, duplicate unit suffixes, or is
        /// empty/whitespace), CountdownFormatParser.TryParse shall return false.
        ///
        /// **Validates: Requirements 8.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CountdownFormatRejectsInvalid()
        {
            var invalidGen = Gen.OneOf(
                // Empty and whitespace strings
                Gen.Elements(string.Empty, " ", "  ", "\t", "\n", "   \t  "),

                // Strings with unrecognized suffixes
                Gen.Choose(0, 999).SelectMany(n =>
                    Gen.Elements('x', 'y', 'z', 'q', 'p', 'a', 'b', 'c', 'e', 'f')
                       .Select(suffix => n + suffix.ToString())),

                // Strings with duplicate units (e.g. "5d 3d", "2h 7h")
                Gen.Choose(0, 100).SelectMany(a =>
                    Gen.Choose(0, 100).SelectMany(b =>
                        Gen.Elements('d', 'h', 'm', 's')
                           .Select(unit => a + unit.ToString() + " " + b + unit.ToString()))),

                // Strings with negative numbers
                Gen.Choose(1, 999).SelectMany(n =>
                    Gen.Elements('d', 'h', 'm', 's')
                       .Select(unit => "-" + n + unit.ToString())),

                // Pure text with no numbers
                Gen.Elements("abc", "hello", "days", "hours", "dhms", "test", "foo bar"),

                // Single characters that aren't valid tokens
                Gen.Elements("d", "h", "m", "s", "5", "0", "x"),

                // Tokens with non-numeric prefixes
                Gen.Elements("abcd", "xxh", "??m", "!!s", "1.5d", "2.0h", "3, 5m")
           );

            return Prop.ForAll(invalidGen.ToArbitrary(), input =>
            {
                bool parsed = CountdownFormatParser.TryParse(input, out long totalSeconds);

                return (!parsed)
                    .Label($"input=\"{input}\" should be rejected but TryParse returned true with totalSeconds={totalSeconds}");
            });
        }

        #endregion
    }
}
