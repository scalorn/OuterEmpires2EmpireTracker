using System;
using NUnit.Framework;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services.Migration
{
    /// <summary>
    /// Unit tests for Migration003_SurveyDateTimeNormalization conversion logic.
    /// Tests the migration pipeline directly (without EmpireContext/PlayerContext)
    /// by replicating the same logic inline:
    ///   1. TryParseIso -> skip if already ISO
    ///   2. TryParseGameFormat -> ToIsoString
    ///   3. DateTime.TryParse -> ToIsoString
    ///   4. Else -> DateTime.Now -> ToIsoString
    /// </summary>
    [TestFixture]
    public class Migration003Tests
    {
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

            // Unparseable or null/empty -- replace with now
            return SurveyDateTimeParser.ToIsoString(DateTime.UtcNow);
        }

        [Test]
        public void MigrateDateTime_AlreadyIso_ReturnsUnchanged()
        {
            string iso = "2024-07-27T23:44:00Z";
            string result = MigrateDateTime(iso);
            Assert.That(result, Is.EqualTo(iso));
        }

        [Test]
        public void MigrateDateTime_AlreadyIso_Midnight_ReturnsUnchanged()
        {
            string iso = "2025-01-01T00:00:00Z";
            string result = MigrateDateTime(iso);
            Assert.That(result, Is.EqualTo(iso));
        }

        [Test]
        public void MigrateDateTime_AlreadyIso_Noon_ReturnsUnchanged()
        {
            string iso = "2026-06-15T12:00:00Z";
            string result = MigrateDateTime(iso);
            Assert.That(result, Is.EqualTo(iso));
        }

        [Test]
        public void MigrateDateTime_GameFormat_ConvertsToIso()
        {
            string result = MigrateDateTime("27JUL24-11:44p");
            Assert.That(result, Is.EqualTo("2024-07-27T23:44:00Z"));
        }

        [Test]
        public void MigrateDateTime_GameFormat_Morning_ConvertsToIso()
        {
            string result = MigrateDateTime("19FEB26-08:41p");
            Assert.That(result, Is.EqualTo("2026-02-19T20:41:00Z"));
        }

        [Test]
        public void MigrateDateTime_GameFormat_Midnight_ConvertsToIso()
        {
            string result = MigrateDateTime("01JAN24-12:00a");
            Assert.That(result, Is.EqualTo("2024-01-01T00:00:00Z"));
        }

        [Test]
        public void MigrateDateTime_GameFormat_Noon_ConvertsToIso()
        {
            string result = MigrateDateTime("15JUN25-12:30p");
            Assert.That(result, Is.EqualTo("2025-06-15T12:30:00Z"));
        }

        [Test]
        public void MigrateDateTime_GarbageString_ProducesValidIso()
        {
            string result = MigrateDateTime("not-a-date");
            Assert.That(SurveyDateTimeParser.TryParseIso(result, out _), Is.True,
                $"Expected valid ISO output, got: '{result}'");
        }

        [Test]
        public void MigrateDateTime_RandomSymbols_ProducesValidIso()
        {
            string result = MigrateDateTime("abc123!@#");
            Assert.That(SurveyDateTimeParser.TryParseIso(result, out _), Is.True,
                $"Expected valid ISO output, got: '{result}'");
        }

        [Test]
        public void MigrateDateTime_InvalidDate_ProducesValidIso()
        {
            // Feb 30 is invalid -- should fall through to DateTime.Now fallback
            string result = MigrateDateTime("2024-02-30T12:00:00");
            Assert.That(SurveyDateTimeParser.TryParseIso(result, out _), Is.True,
                $"Expected valid ISO output, got: '{result}'");
        }

        [Test]
        public void MigrateDateTime_Null_ProducesValidIso()
        {
            string result = MigrateDateTime(null);
            Assert.That(SurveyDateTimeParser.TryParseIso(result, out _), Is.True,
                $"Expected valid ISO output, got: '{result}'");
        }

        [Test]
        public void MigrateDateTime_Empty_ProducesValidIso()
        {
            string result = MigrateDateTime(string.Empty);
            Assert.That(SurveyDateTimeParser.TryParseIso(result, out _), Is.True,
                $"Expected valid ISO output, got: '{result}'");
        }

        [Test]
        public void MigrateDateTime_CommonNetFormat_ProducesValidIso()
        {
            // "G" format: "07/27/2024 11:44:00 PM" -- parseable by DateTime.TryParse
            var dt = new DateTime(2024, 7, 27, 23, 44, 0);
            string formatted = dt.ToString("G", System.Globalization.CultureInfo.InvariantCulture);

            string result = MigrateDateTime(formatted);
            Assert.That(SurveyDateTimeParser.TryParseIso(result, out _), Is.True,
                $"Expected valid ISO output from common format '{formatted}', got: '{result}'");
        }
    }
}
