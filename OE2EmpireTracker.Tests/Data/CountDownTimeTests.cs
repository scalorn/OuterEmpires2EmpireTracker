using NUnit.Framework;
using NUnit.Framework.Legacy;
using OE2EmpireTracker.Data;
using System;

namespace OE2EmpireTracker.Tests.Data
{
    [TestFixture]
    public class CountDownTimeTests
    {
        // -----------------------------------------------------------------------
        // Constructor
        // -----------------------------------------------------------------------

        [Test]
        public void DefaultConstructor_StartTimeIsMinValue()
        {
            var cdt = new CountDownTime();
            Assert.AreEqual(DateTime.MinValue, cdt.StartTime);
        }

        [Test]
        public void DefaultConstructor_EndTimeEqualsStartTime()
        {
            var cdt = new CountDownTime();
            Assert.AreEqual(cdt.StartTime, cdt.EndTime);
        }

        // -----------------------------------------------------------------------
        // TimeRemaining getter
        // -----------------------------------------------------------------------

        [Test]
        public void TimeRemaining_FutureEndTime_ReturnsPositiveSeconds()
        {
            var cdt = new CountDownTime();
            cdt.EndTime = DateTime.Now.AddSeconds(100);
            Assert.Greater(cdt.TimeRemaining, 0);
        }

        [Test]
        public void TimeRemaining_PastEndTime_ReturnsNegativeSeconds()
        {
            var cdt = new CountDownTime();
            cdt.EndTime = DateTime.Now.AddSeconds(-100);
            Assert.Less(cdt.TimeRemaining, 0);
        }

        [Test]
        public void TimeRemaining_ApproximatelyCorrect()
        {
            var cdt = new CountDownTime();
            cdt.EndTime = DateTime.Now.AddSeconds(3600);
            // Allow 2 second tolerance for test execution time
            Assert.That(cdt.TimeRemaining, Is.InRange(3598L, 3600L));
        }

        // -----------------------------------------------------------------------
        // TimeRemaining setter
        // -----------------------------------------------------------------------

        [Test]
        public void TimeRemaining_Setter_SetsEndTimeInFuture()
        {
            var cdt = new CountDownTime();
            cdt.TimeRemaining = 3600;
            Assert.That(cdt.EndTime, Is.GreaterThan(DateTime.Now));
        }

        [Test]
        public void TimeRemaining_SetterThenGetter_RoundTrips()
        {
            var cdt = new CountDownTime();
            cdt.TimeRemaining = 500;
            Assert.That(cdt.TimeRemaining, Is.InRange(498L, 500L));
        }

        [Test]
        public void TimeRemaining_SetZero_EndTimeIsNow()
        {
            var cdt = new CountDownTime();
            cdt.TimeRemaining = 0;
            Assert.That(cdt.EndTime, Is.EqualTo(DateTime.Now).Within(TimeSpan.FromSeconds(2)));
        }

        // -----------------------------------------------------------------------
        // TimeRemainingString getter
        // -----------------------------------------------------------------------

        [Test]
        public void TimeRemainingString_IncludesDays_WhenDaysPresent()
        {
            var cdt = new CountDownTime();
            cdt.EndTime = DateTime.Now.AddDays(2).AddHours(3);
            StringAssert.Contains("d", cdt.TimeRemainingString);
        }

        [Test]
        public void TimeRemainingString_NoDaySegment_WhenLessThanOneDay()
        {
            var cdt = new CountDownTime();
            cdt.EndTime = DateTime.Now.AddHours(5);
            StringAssert.DoesNotContain("d", cdt.TimeRemainingString);
        }

        [Test]
        public void TimeRemainingString_ContainsHoursMinutesSeconds()
        {
            var cdt = new CountDownTime();
            cdt.EndTime = DateTime.Now.AddHours(1).AddMinutes(30).AddSeconds(45);
            string s = cdt.TimeRemainingString;
            StringAssert.Contains("h", s);
            StringAssert.Contains("m", s);
            StringAssert.Contains("s", s);
        }

        [Test]
        public void TimeRemainingString_OnlySeconds_WhenLessThanOneMinute()
        {
            var cdt = new CountDownTime();
            cdt.EndTime = DateTime.Now.AddSeconds(30);
            string s = cdt.TimeRemainingString;
            StringAssert.Contains("s", s);
            StringAssert.DoesNotContain("m", s);
            StringAssert.DoesNotContain("h", s);
            StringAssert.DoesNotContain("d", s);
        }

        // -----------------------------------------------------------------------
        // TimeRemainingString setter (parse)
        // -----------------------------------------------------------------------

        [Test]
        public void TimeRemainingString_Setter_FullFormat_ParsesCorrectly()
        {
            var cdt = new CountDownTime();
            cdt.TimeRemainingString = "1d 2h 3m 4s";
            long expected = ((1 * 24 + 2) * 60 + 3) * 60 + 4; // 93784
            Assert.That(cdt.TimeRemaining, Is.InRange(expected - 2, expected));
        }

        [Test]
        public void TimeRemainingString_Setter_HoursOnly_ParsesCorrectly()
        {
            var cdt = new CountDownTime();
            cdt.TimeRemainingString = "5h";
            Assert.That(cdt.TimeRemaining, Is.InRange(5 * 3600 - 2, 5 * 3600));
        }

        [Test]
        public void TimeRemainingString_Setter_MinutesAndSeconds_ParsesCorrectly()
        {
            var cdt = new CountDownTime();
            cdt.TimeRemainingString = "10m 30s";
            Assert.That(cdt.TimeRemaining, Is.InRange(630 - 2, 630));
        }

        [Test]
        public void TimeRemainingString_Setter_SecondsOnly_ParsesCorrectly()
        {
            var cdt = new CountDownTime();
            cdt.TimeRemainingString = "45s";
            Assert.That(cdt.TimeRemaining, Is.InRange(43, 45));
        }

        [Test]
        public void TimeRemainingString_SetterThenGetter_RoundTrips()
        {
            var cdt = new CountDownTime();
            cdt.TimeRemainingString = "2d 4h 15m 10s";
            string result = cdt.TimeRemainingString;
            StringAssert.Contains("2d", result);
            StringAssert.Contains("4h", result);
            StringAssert.Contains("15m", result);
        }

        [Test]
        public void StartRepeating_SetsRepeatIntervalAndNextInterval()
        {
            var cdt = new CountDownTime();
            cdt.StartRepeating(60);

            Assert.IsTrue(cdt.IsRepeating);
            Assert.AreEqual(60, cdt.RepeatIntervalSeconds);
            Assert.That(cdt.TimeRemaining, Is.InRange(58L, 60L));
        }

        [Test]
        public void TimeRemaining_RepeatingTimer_ReturnsSecondsUntilNextIntervalBoundary()
        {
            var cdt = new CountDownTime();
            cdt.StartRepeating(60, 10);

            Assert.That(cdt.TimeRemaining, Is.InRange(8L, 10L));
            Assert.AreEqual(60, cdt.RepeatIntervalSeconds);
        }

        [Test]
        public void IntervalsPassed_ReturnsFullIntervalsSinceStart()
        {
            var cdt = new CountDownTime();
            cdt.StartRepeating(60);
            cdt.StartTime = DateTime.Now.AddSeconds(-300);

            Assert.AreEqual(5, cdt.IntervalsPassed);
        }

        [Test]
        public void ConsumeIntervals_AdvancesStartTimeAndReducesPassedIntervals()
        {
            var cdt = new CountDownTime();
            cdt.StartRepeating(60);
            cdt.StartTime = DateTime.Now.AddSeconds(-300);

            Assert.AreEqual(5, cdt.IntervalsPassed);
            cdt.ConsumeIntervals(2);
            Assert.AreEqual(3, cdt.IntervalsPassed);
        }

        [Test]
        public void ConsumeIntervals_DoesNotConsumeMoreThanPassed()
        {
            var cdt = new CountDownTime();
            cdt.StartRepeating(60);
            cdt.StartTime = DateTime.Now.AddSeconds(-90);

            Assert.AreEqual(1, cdt.IntervalsPassed);
            cdt.ConsumeIntervals(5);
            Assert.AreEqual(0, cdt.IntervalsPassed);
        }
    }
}
