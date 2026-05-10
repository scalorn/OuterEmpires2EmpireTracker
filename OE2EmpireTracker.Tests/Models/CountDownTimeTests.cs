using System;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Models
{
    [TestFixture]
    public class CountDownTimeTests
    {
        private static readonly DateTime FrozenNow = new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            SystemClock.UtcNowFunc = () => FrozenNow;
        }

        [TearDown]
        public void TearDown()
        {
            SystemClock.Reset();
        }

        // -----------------------------------------------------------------------
        // Constructor
        // -----------------------------------------------------------------------

        [Test]
        public void DefaultConstructor_StartTimeIsMinValue()
        {
            var cdt = new CountDownTime();
            Assert.That(cdt.StartTime, Is.EqualTo(DateTime.MinValue));
        }

        [Test]
        public void DefaultConstructor_EndTimeEqualsStartTime()
        {
            var cdt = new CountDownTime();
            Assert.That(cdt.EndTime, Is.EqualTo(cdt.StartTime));
        }

        // -----------------------------------------------------------------------
        // TimeRemaining getter
        // -----------------------------------------------------------------------

        [Test]
        public void TimeRemaining_FutureEndTime_ReturnsPositiveSeconds()
        {
            var cdt = new CountDownTime();
            cdt.EndTime = FrozenNow.AddSeconds(100);
            Assert.That(cdt.TimeRemaining, Is.EqualTo(100));
        }

        [Test]
        public void TimeRemaining_PastEndTime_ReturnsNegativeSeconds()
        {
            var cdt = new CountDownTime();
            cdt.EndTime = FrozenNow.AddSeconds(-100);
            Assert.That(cdt.TimeRemaining, Is.EqualTo(-100));
        }

        [Test]
        public void TimeRemaining_ApproximatelyCorrect()
        {
            var cdt = new CountDownTime();
            cdt.EndTime = FrozenNow.AddSeconds(3600);
            Assert.That(cdt.TimeRemaining, Is.EqualTo(3600));
        }

        // -----------------------------------------------------------------------
        // TimeRemaining setter
        // -----------------------------------------------------------------------

        [Test]
        public void TimeRemaining_Setter_SetsEndTimeInFuture()
        {
            var cdt = new CountDownTime();
            cdt.TimeRemaining = 3600;
            Assert.That(cdt.EndTime, Is.EqualTo(FrozenNow.AddSeconds(3600)));
        }

        [Test]
        public void TimeRemaining_SetterThenGetter_RoundTrips()
        {
            var cdt = new CountDownTime();
            cdt.TimeRemaining = 500;
            Assert.That(cdt.TimeRemaining, Is.EqualTo(500));
        }

        [Test]
        public void TimeRemaining_SetZero_EndTimeIsNow()
        {
            var cdt = new CountDownTime();
            cdt.TimeRemaining = 0;
            Assert.That(cdt.EndTime, Is.EqualTo(FrozenNow));
        }

        // -----------------------------------------------------------------------
        // TimeRemainingString getter
        // -----------------------------------------------------------------------

        [Test]
        public void TimeRemainingString_IncludesDays_WhenDaysPresent()
        {
            var cdt = new CountDownTime();
            cdt.EndTime = DateTime.UtcNow.AddDays(2).AddHours(3);
            Assert.That(cdt.TimeRemainingString, Does.Contain("d"));
        }

        [Test]
        public void TimeRemainingString_NoDaySegment_WhenLessThanOneDay()
        {
            var cdt = new CountDownTime();
            cdt.EndTime = DateTime.UtcNow.AddHours(5);
            Assert.That(cdt.TimeRemainingString, Does.Not.Contain("d"));
        }

        [Test]
        public void TimeRemainingString_ContainsHoursMinutesSeconds()
        {
            var cdt = new CountDownTime();
            cdt.EndTime = DateTime.UtcNow.AddHours(1).AddMinutes(30).AddSeconds(45);
            string s = cdt.TimeRemainingString;
            Assert.That(s, Does.Contain("h"));
            Assert.That(s, Does.Contain("m"));
            Assert.That(s, Does.Contain("s"));
        }

        [Test]
        public void TimeRemainingString_IntermediateZeroMinutes_IsShown()
        {
            // 1h 0m 30s -- the 0m segment must appear because hours was shown
            var cdt = new CountDownTime();
            cdt.EndTime = DateTime.UtcNow.AddHours(1).AddSeconds(30);
            string s = cdt.TimeRemainingString;
            Assert.That(s, Does.Contain("1h"));
            Assert.That(s, Does.Contain("0m"));
            Assert.That(s, Does.Contain("s"));
        }

        [Test]
        public void TimeRemainingString_LeadingZeroHours_IsOmitted()
        {
            // 0h 30m 45s -- hours should not appear since it is a leading zero
            var cdt = new CountDownTime();
            cdt.EndTime = DateTime.UtcNow.AddMinutes(30).AddSeconds(45);
            string s = cdt.TimeRemainingString;
            Assert.That(s, Does.Not.Contain("h"));
            Assert.That(s, Does.Contain("30m"));
            Assert.That(s, Does.Contain("s"));
        }

        [Test]
        public void TimeRemainingString_Expired_ReturnsZeroSeconds()
        {
            var cdt = new CountDownTime();
            cdt.EndTime = DateTime.UtcNow.AddSeconds(-10); // already expired
            Assert.That(cdt.TimeRemainingString, Is.EqualTo("0s"));
        }

        [Test]
        public void TimeRemainingString_OnlySeconds_WhenLessThanOneMinute()
        {
            var cdt = new CountDownTime();
            cdt.EndTime = DateTime.UtcNow.AddSeconds(30);
            string s = cdt.TimeRemainingString;
            Assert.That(s, Does.Contain("s"));
            Assert.That(s, Does.Not.Contain("m"));
            Assert.That(s, Does.Not.Contain("h"));
            Assert.That(s, Does.Not.Contain("d"));
        }

        // -----------------------------------------------------------------------
        // TimeRemainingString setter (parse)
        // -----------------------------------------------------------------------

        [Test]
        public void TimeRemainingString_Setter_FullFormat_ParsesCorrectly()
        {
            var cdt = new CountDownTime();
            cdt.TimeRemainingString = "1d 2h 3m 4s";
            long expected = (((((1 * 24) + 2) * 60) + 3) * 60) + 4; // 93784
            Assert.That(cdt.TimeRemaining, Is.InRange(expected - 2, expected));
        }

        [Test]
        public void TimeRemainingString_Setter_HoursOnly_ParsesCorrectly()
        {
            var cdt = new CountDownTime();
            cdt.TimeRemainingString = "5h";
            Assert.That(cdt.TimeRemaining, Is.InRange((5 * 3600) - 2, 5 * 3600));
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
            Assert.That(result, Does.Contain("2d"));
            Assert.That(result, Does.Contain("4h"));
            Assert.That(result, Does.Contain("15m"));
        }

        [Test]
        public void StartRepeating_SetsRepeatIntervalAndNextInterval()
        {
            var cdt = new CountDownTime();
            cdt.StartRepeating(60);

            Assert.That(cdt.IsRepeating, Is.True);
            Assert.That(cdt.RepeatIntervalSeconds, Is.EqualTo(60));
            Assert.That(cdt.TimeRemaining, Is.InRange(58L, 60L));
        }

        [Test]
        public void TimeRemaining_RepeatingTimer_ReturnsSecondsUntilNextIntervalBoundary()
        {
            var cdt = new CountDownTime();
            cdt.StartRepeating(60, 10);

            Assert.That(cdt.TimeRemaining, Is.InRange(8L, 10L));
            Assert.That(cdt.RepeatIntervalSeconds, Is.EqualTo(60));
        }

        [Test]
        public void IntervalsPassed_ReturnsFullIntervalsSinceStart()
        {
            var cdt = new CountDownTime();
            cdt.StartRepeating(60);
            cdt.StartTime = DateTime.UtcNow.AddSeconds(-300);

            Assert.That(cdt.IntervalsPassed, Is.EqualTo(5));
        }

        [Test]
        public void ConsumeIntervals_AdvancesStartTimeAndReducesPassedIntervals()
        {
            var cdt = new CountDownTime();
            cdt.StartRepeating(60);
            cdt.StartTime = DateTime.UtcNow.AddSeconds(-300);

            Assert.That(cdt.IntervalsPassed, Is.EqualTo(5));
            cdt.ConsumeIntervals(2);
            Assert.That(cdt.IntervalsPassed, Is.EqualTo(3));
        }

        [Test]
        public void ConsumeIntervals_DoesNotConsumeMoreThanPassed()
        {
            var cdt = new CountDownTime();
            cdt.StartRepeating(60);
            cdt.StartTime = DateTime.UtcNow.AddSeconds(-90);

            Assert.That(cdt.IntervalsPassed, Is.EqualTo(1));
            cdt.ConsumeIntervals(5);
            Assert.That(cdt.IntervalsPassed, Is.EqualTo(0));
        }
    }
}
