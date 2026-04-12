using System;
using NUnit.Framework;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class SurveyDateTimeParserTests
    {
        #region 12a → midnight, 12p → noon

        [Test]
        public void TryParseGameFormat_12a_IsMidnight()
        {
            bool ok = SurveyDateTimeParser.TryParseGameFormat("01JAN24-12:00a", out DateTime dt);

            Assert.That(ok, Is.True);
            Assert.That(dt.Hour, Is.EqualTo(0), "12a should be midnight (hour 0)");
            Assert.That(dt.Minute, Is.EqualTo(0));
        }

        [Test]
        public void TryParseGameFormat_12p_IsNoon()
        {
            bool ok = SurveyDateTimeParser.TryParseGameFormat("01JAN24-12:00p", out DateTime dt);

            Assert.That(ok, Is.True);
            Assert.That(dt.Hour, Is.EqualTo(12), "12p should be noon (hour 12)");
            Assert.That(dt.Minute, Is.EqualTo(0));
        }

        #endregion

        #region All 12 months JAN–DEC

        [TestCase("JAN", 1)]
        [TestCase("FEB", 2)]
        [TestCase("MAR", 3)]
        [TestCase("APR", 4)]
        [TestCase("MAY", 5)]
        [TestCase("JUN", 6)]
        [TestCase("JUL", 7)]
        [TestCase("AUG", 8)]
        [TestCase("SEP", 9)]
        [TestCase("OCT", 10)]
        [TestCase("NOV", 11)]
        [TestCase("DEC", 12)]
        public void TryParseGameFormat_AllMonths_ParseToCorrectNumber(string monthAbbr, int expectedMonth)
        {
            string input = $"15{monthAbbr}24-3:30p";
            bool ok = SurveyDateTimeParser.TryParseGameFormat(input, out DateTime dt);

            Assert.That(ok, Is.True, $"Failed to parse month {monthAbbr}");
            Assert.That(dt.Month, Is.EqualTo(expectedMonth));
        }

        #endregion

        #region Known game strings → expected ISO output

        [Test]
        public void KnownGameString_27JUL24_1144p()
        {
            bool ok = SurveyDateTimeParser.TryParseGameFormat("27JUL24-11:44p", out DateTime dt);

            Assert.That(ok, Is.True);
            Assert.That(SurveyDateTimeParser.ToIsoString(dt), Is.EqualTo("2024-07-27T23:44:00"));
        }

        [Test]
        public void KnownGameString_19FEB26_0841p()
        {
            bool ok = SurveyDateTimeParser.TryParseGameFormat("19FEB26-08:41p", out DateTime dt);

            Assert.That(ok, Is.True);
            Assert.That(SurveyDateTimeParser.ToIsoString(dt), Is.EqualTo("2026-02-19T20:41:00"));
        }

        #endregion

        #region FormatForDisplay passthrough for non-ISO strings

        [TestCase("27JUL24-11:44p")]
        [TestCase("not-a-date")]
        [TestCase("hello world")]
        [TestCase("2024/07/27")]
        public void FormatForDisplay_NonIsoString_ReturnsUnchanged(string input)
        {
            string result = SurveyDateTimeParser.FormatForDisplay(input);

            Assert.That(result, Is.EqualTo(input));
        }

        #endregion

        #region TryParseGameFormat returns false for null, empty, malformed

        [Test]
        public void TryParseGameFormat_Null_ReturnsFalse()
        {
            bool ok = SurveyDateTimeParser.TryParseGameFormat(null, out _);
            Assert.That(ok, Is.False);
        }

        [Test]
        public void TryParseGameFormat_Empty_ReturnsFalse()
        {
            bool ok = SurveyDateTimeParser.TryParseGameFormat("", out _);
            Assert.That(ok, Is.False);
        }

        [TestCase("not-a-date")]
        [TestCase("2024-07-27T23:44:00")]
        [TestCase("27jul24-11:44p")]       // lowercase month
        [TestCase("27JUL24 11:44p")]       // space instead of dash
        [TestCase("27JUL24-11:44x")]       // invalid suffix
        [TestCase("27JUL24-11:44")]        // missing suffix
        public void TryParseGameFormat_Malformed_ReturnsFalse(string input)
        {
            bool ok = SurveyDateTimeParser.TryParseGameFormat(input, out _);
            Assert.That(ok, Is.False);
        }

        #endregion
    }
}
