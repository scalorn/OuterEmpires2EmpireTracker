using System;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Models
{
    /// <summary>
    /// Feature: colony-import-timestamp, Property 7: CountDownTime uses UTC consistently
    ///
    /// For any CountDownTime initialized via StartRepeating or TimeRemaining setter using
    /// DateTime.UtcNow, the TimeRemaining getter (which also uses DateTime.UtcNow internally)
    /// should return a value within 1 second of the expected remaining time. The StartTime and
    /// EndTime stored on the object should have Kind == DateTimeKind.Utc (or be DateTime.MinValue).
    ///
    /// **Validates: Requirements 8.2**
    /// </summary>
    [TestFixture]
    public class CountDownTimeUtcPropertyTests
    {
        #region Generators

        /// <summary>
        /// Generates positive interval seconds (1 to 86400 = 1 day).
        /// </summary>
        private static Gen<long> IntervalSecondsGen()
        {
            return Gen.Choose(1, 86400).Select(i => (long)i);
        }

        /// <summary>
        /// Generates positive remaining seconds (1 to 86400).
        /// </summary>
        private static Gen<long> RemainingSecondsGen()
        {
            return Gen.Choose(1, 86400).Select(i => (long)i);
        }

        /// <summary>
        /// Generates a pair of (intervalSeconds, secondsUntilNextInterval) where
        /// secondsUntilNextInterval is in [1, intervalSeconds].
        /// </summary>
        private static Gen<Tuple<long, long>> IntervalAndRemainingGen()
        {
            return from interval in Gen.Choose(2, 3600)
                   from remaining in Gen.Choose(1, interval)
                   select Tuple.Create((long)interval, (long)remaining);
        }

        #endregion

        #region Property 7: CountDownTime uses UTC consistently

        /// <summary>
        /// Feature: colony-import-timestamp, Property 7: CountDownTime uses UTC consistently
        ///
        /// Sub-property 7a: StartRepeating(intervalSeconds) sets StartTime and EndTime with UTC kind,
        /// and TimeRemaining is within 1 second of the interval.
        /// **Validates: Requirements 8.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property StartRepeating_SetsUtcTimesAndCorrectRemaining()
        {
            return Prop.ForAll(IntervalSecondsGen().ToArbitrary(), intervalSeconds =>
            {
                var cdt = new CountDownTime();
                cdt.StartRepeating(intervalSeconds);

                bool startKindOk = cdt.StartTime.Kind == DateTimeKind.Utc;
                bool endKindOk = cdt.EndTime.Kind == DateTimeKind.Utc;
                long remaining = cdt.TimeRemaining;
                bool remainingOk = Math.Abs(remaining - intervalSeconds) <= 1;

                return (startKindOk && endKindOk && remainingOk)
                    .Label($"StartTime.Kind={cdt.StartTime.Kind}, EndTime.Kind={cdt.EndTime.Kind}, " +
                           $"TimeRemaining={remaining}, expected~={intervalSeconds}");
            });
        }

        /// <summary>
        /// Feature: colony-import-timestamp, Property 7: CountDownTime uses UTC consistently
        ///
        /// Sub-property 7b: StartRepeating(intervalSeconds, secondsUntilNext) sets UTC times
        /// and TimeRemaining is within 1 second of secondsUntilNext.
        /// **Validates: Requirements 8.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property StartRepeatingWithOffset_SetsUtcTimesAndCorrectRemaining()
        {
            return Prop.ForAll(IntervalAndRemainingGen().ToArbitrary(), pair =>
            {
                long intervalSeconds = pair.Item1;
                long secondsUntilNext = pair.Item2;

                var cdt = new CountDownTime();
                cdt.StartRepeating(intervalSeconds, secondsUntilNext);

                bool startKindOk = cdt.StartTime.Kind == DateTimeKind.Utc;
                bool endKindOk = cdt.EndTime.Kind == DateTimeKind.Utc;
                long remaining = cdt.TimeRemaining;
                bool remainingOk = Math.Abs(remaining - secondsUntilNext) <= 1;

                return (startKindOk && endKindOk && remainingOk)
                    .Label($"StartTime.Kind={cdt.StartTime.Kind}, EndTime.Kind={cdt.EndTime.Kind}, " +
                           $"TimeRemaining={remaining}, expected~={secondsUntilNext}, " +
                           $"interval={intervalSeconds}");
            });
        }

        /// <summary>
        /// Feature: colony-import-timestamp, Property 7: CountDownTime uses UTC consistently
        ///
        /// Sub-property 7c: TimeRemaining setter sets StartTime and EndTime with UTC kind,
        /// and the getter returns a value within 1 second of what was set.
        /// **Validates: Requirements 8.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property TimeRemainingSetter_SetsUtcTimesAndRoundTrips()
        {
            return Prop.ForAll(RemainingSecondsGen().ToArbitrary(), seconds =>
            {
                var cdt = new CountDownTime();
                cdt.TimeRemaining = seconds;

                bool startKindOk = cdt.StartTime.Kind == DateTimeKind.Utc;
                bool endKindOk = cdt.EndTime.Kind == DateTimeKind.Utc;
                long remaining = cdt.TimeRemaining;
                bool remainingOk = Math.Abs(remaining - seconds) <= 1;

                return (startKindOk && endKindOk && remainingOk)
                    .Label($"StartTime.Kind={cdt.StartTime.Kind}, EndTime.Kind={cdt.EndTime.Kind}, " +
                           $"TimeRemaining={remaining}, expected~={seconds}");
            });
        }

        /// <summary>
        /// Feature: colony-import-timestamp, Property 7: CountDownTime uses UTC consistently
        ///
        /// Sub-property 7d: Default constructor sets StartTime and EndTime to DateTime.MinValue.
        /// **Validates: Requirements 8.2**
        /// </summary>
        [Test]
        public void DefaultConstructor_TimesAreMinValue()
        {
            var cdt = new CountDownTime();
            Assert.That(cdt.StartTime, Is.EqualTo(DateTime.MinValue));
            Assert.That(cdt.EndTime, Is.EqualTo(DateTime.MinValue));
        }

        #endregion
    }
}
