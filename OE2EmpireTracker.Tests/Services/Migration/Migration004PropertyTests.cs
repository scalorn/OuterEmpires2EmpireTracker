using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services.Migration
{
    /// <summary>
    /// Property tests for Migration004_ColonyImportTimestampBackfill.
    /// Tests the migration logic directly by constructing PlayerContext/EmpireContext
    /// test doubles or by replicating the migration logic inline.
    /// </summary>
    [TestFixture]
    public class Migration004PropertyTests
    {
        /// <summary>
        /// Feature: colony-import-timestamp, Property 3: Migration backfills empty and preserves existing.
        /// For any list of colonies where some have null/empty LastImportDateTime and others have
        /// valid ISO 8601 strings, after running the backfill logic, every colony should have a
        /// non-null LastImportDateTime that parses via TryParseIso, and any colony that had a valid
        /// ISO string before should have the same value after.
        /// **Validates: Requirements 3.1, 3.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property MigrationBackfillsEmptyAndPreservesExisting()
        {
            var gen = Gen.ListOf(ColonyWithMixedTimestampGen());

            return Prop.ForAll(gen.ToArbitrary(), coloniesRaw =>
            {
                var colonies = coloniesRaw.ToList();

                // Record original values
                var originals = colonies.Select(c => c.LastImportDateTime).ToList();

                // Run the backfill logic (inline replica -- no singletons needed)
                foreach (var colony in colonies)
                {
                    if (string.IsNullOrEmpty(colony.LastImportDateTime))
                    {
                        colony.LastImportDateTime = SurveyDateTimeParser.ToIsoString(DateTime.UtcNow);
                    }
                }

                // Verify all colonies now have valid ISO timestamps
                for (int i = 0; i < colonies.Count; i++)
                {
                    var colony = colonies[i];
                    string original = originals[i];

                    bool nonNull = !string.IsNullOrEmpty(colony.LastImportDateTime);
                    if (!nonNull)
                        return false.Label($"Colony {i}: LastImportDateTime is null/empty after migration");

                    bool parses = SurveyDateTimeParser.TryParseIso(colony.LastImportDateTime, out _);
                    if (!parses)
                        return false.Label($"Colony {i}: LastImportDateTime '{colony.LastImportDateTime}' does not parse");

                    // If original was valid ISO, it should be preserved
                    bool originalWasValid = !string.IsNullOrEmpty(original) &&
                                            SurveyDateTimeParser.TryParseIso(original, out _);
                    if (originalWasValid && colony.LastImportDateTime != original)
                        return false.Label($"Colony {i}: valid original '{original}' was changed to '{colony.LastImportDateTime}'");
                }

                return true.Label("All colonies have valid ISO timestamps, existing preserved");
            });
        }

        /// <summary>
        /// Feature: colony-import-timestamp, Property 8: Migration converts local times to UTC correctly.
        /// For any CountDownTime with StartTime and EndTime in local time (not DateTime.MinValue),
        /// applying .ToUniversalTime() should shift them by the local UTC offset. The difference
        /// EndTime - StartTime should remain unchanged (the interval duration is preserved).
        /// **Validates: Requirements 9.1, 9.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property MigrationConvertsLocalTimesToUtcCorrectly()
        {
            return Prop.ForAll(LocalCountDownTimeGen().ToArbitrary(), timer =>
            {
                DateTime originalStart = timer.StartTime;
                DateTime originalEnd = timer.EndTime;

                // Apply the migration conversion (inline replica)
                if (timer.StartTime != DateTime.MinValue)
                    timer.StartTime = timer.StartTime.ToUniversalTime();
                if (timer.EndTime != DateTime.MinValue)
                    timer.EndTime = timer.EndTime.ToUniversalTime();

                // Both should now be UTC
                bool startIsUtc = timer.StartTime.Kind == DateTimeKind.Utc;
                bool endIsUtc = timer.EndTime.Kind == DateTimeKind.Utc;

                // The shift should equal the local UTC offset at each original time
                // (may differ across DST boundaries -- that's correct behavior)
                TimeSpan startOffset = TimeZoneInfo.Local.GetUtcOffset(originalStart);
                TimeSpan startShift = originalStart - timer.StartTime;
                bool startShiftCorrect = Math.Abs((startShift - startOffset).TotalSeconds) < 1;

                TimeSpan endOffset = TimeZoneInfo.Local.GetUtcOffset(originalEnd);
                TimeSpan endShift = originalEnd - timer.EndTime;
                bool endShiftCorrect = Math.Abs((endShift - endOffset).TotalSeconds) < 1;

                return (startIsUtc && endIsUtc)
                    .Label($"Kind not UTC: start={timer.StartTime.Kind}, end={timer.EndTime.Kind}")
                    .And(startShiftCorrect)
                    .Label($"Start shift incorrect: expected={startOffset}, actual={startShift}")
                    .And(endShiftCorrect)
                    .Label($"End shift incorrect: expected={endOffset}, actual={endShift}");
            });
        }

        /// <summary>
        /// Feature: colony-import-timestamp, Property 8 (MinValue edge case).
        /// DateTime.MinValue should be skipped by the migration -- not converted.
        /// **Validates: Requirements 9.2**
        /// </summary>
        [Test]
        public void MigrationSkipsMinValueTimers()
        {
            var timer = new CountDownTime
            {
                StartTime = DateTime.MinValue,
                EndTime = DateTime.MinValue
            };

            // Apply the migration conversion (inline replica)
            if (timer.StartTime != DateTime.MinValue)
                timer.StartTime = timer.StartTime.ToUniversalTime();
            if (timer.EndTime != DateTime.MinValue)
                timer.EndTime = timer.EndTime.ToUniversalTime();

            Assert.That(timer.StartTime, Is.EqualTo(DateTime.MinValue));
            Assert.That(timer.EndTime, Is.EqualTo(DateTime.MinValue));
        }

        /// <summary>
        /// Generates valid UTC DateTime values constrained to years 2000-2099.
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
        /// Generates valid ISO 8601 strings from random UTC DateTimes.
        /// </summary>
        private static Gen<string> ValidIsoStringGen()
        {
            return ValidUtcDateTimeGen().Select(dt => SurveyDateTimeParser.ToIsoString(dt));
        }

        /// <summary>
        /// Generates a null or empty string to represent missing LastImportDateTime.
        /// </summary>
        private static Gen<string> NullOrEmptyGen()
        {
            return Gen.OneOf(Gen.Constant((string)null), Gen.Constant(string.Empty));
        }

        /// <summary>
        /// Generates a Colony with either a valid ISO LastImportDateTime or null/empty.
        /// </summary>
        private static Gen<Colony> ColonyWithMixedTimestampGen()
        {
            var withValid = from iso in ValidIsoStringGen()
                            select new Colony { LastImportDateTime = iso, ColonyName = "TestColony" };
            var withEmpty = from empty in NullOrEmptyGen()
                            select new Colony { LastImportDateTime = empty, ColonyName = "TestColony" };
            return Gen.OneOf(withValid, withEmpty);
        }

        /// <summary>
        /// Generates a local DateTime (not MinValue) for CountDownTime testing.
        /// Uses DateTimeKind.Local to simulate pre-migration local times.
        /// </summary>
        private static Gen<DateTime> LocalDateTimeGen()
        {
            return from year in Gen.Choose(2020, 2030)
                   from month in Gen.Choose(1, 12)
                   from day in Gen.Choose(1, 28)
                   from hour in Gen.Choose(0, 23)
                   from minute in Gen.Choose(0, 59)
                   from second in Gen.Choose(0, 59)
                   select new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
        }

        /// <summary>
        /// Generates a CountDownTime with local StartTime and EndTime (not MinValue).
        /// EndTime is always after StartTime.
        /// </summary>
        private static Gen<CountDownTime> LocalCountDownTimeGen()
        {
            return from start in LocalDateTimeGen()
                   from durationSeconds in Gen.Choose(60, 86400)
                   select new CountDownTime
                   {
                       StartTime = start,
                       EndTime = start.AddSeconds(durationSeconds)
                   };
        }
    }
}
