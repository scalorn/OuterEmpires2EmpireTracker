using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for TabWarningService with configurable thresholds.
    /// Feature: preferences-form
    /// </summary>
    [TestFixture]
    public class TabWarningServicePreferencesTests
    {
        [SetUp]
        public void SetUp()
        {
            PreferencesStore.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            PreferencesStore.Reset();
        }

        /// <summary>
        /// Feature: preferences-form, Property 3: Structure warning respects configured thresholds
        ///
        /// For any non-negative integer structureCount and any configured ThresholdPreferences
        /// where StructureCountYellow &lt; StructureCountRed and both are positive,
        /// EvaluateStructureWarning(structureCount) shall return Red when structureCount >= StructureCountRed,
        /// Yellow when structureCount >= StructureCountYellow, and None otherwise.
        ///
        /// **Validates: Requirements 4.1, 4.2, 4.7**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property StructureWarningRespectsThresholds()
        {
            // Generate valid yellow < red threshold pairs (both positive)
            var genYellow = Gen.Choose(1, 100);
            var genThresholds = genYellow.SelectMany(
                yellow => Gen.Choose(yellow + 1, yellow + 100),
                (yellow, red) => new { Yellow = yellow, Red = red });
            var genCount = Gen.Choose(0, 200);

            return Prop.ForAll(
                genThresholds.ToArbitrary(),
                genCount.ToArbitrary(),
                (thresholds, count) =>
                {
                    var store = PreferencesStore.GetInstance();
                    store.Preferences.Thresholds.StructureCountYellow = thresholds.Yellow;
                    store.Preferences.Thresholds.StructureCountRed = thresholds.Red;

                    var result = TabWarningService.EvaluateStructureWarning(count);

                    TabWarningLevel expected;
                    if (count >= thresholds.Red)
                        expected = TabWarningLevel.Red;
                    else if (count >= thresholds.Yellow)
                        expected = TabWarningLevel.Yellow;
                    else
                        expected = TabWarningLevel.None;

                    return (result == expected)
                        .Label($"yellow={thresholds.Yellow}, red={thresholds.Red}, count={count}, expected={expected}, got={result}");
                });
        }

        /// <summary>
        /// Feature: preferences-form, Property 4: Worker warning respects configured thresholds
        ///
        /// For any configured ThresholdPreferences where WorkerRequestYellowSeconds > WorkerRequestRedSeconds
        /// and both are positive, and for any unfulfilled commodity request with a valid NeedBy date,
        /// EvaluateWorkerWarning shall return Red when the due window is at or below the configured red threshold,
        /// Yellow when at or below the configured yellow threshold, and None otherwise.
        ///
        /// **Validates: Requirements 4.3, 4.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property WorkerWarningRespectsThresholds()
        {
            // Generate valid threshold pairs: yellow > red, both positive (in seconds)
            // Use ranges that produce meaningful time windows (1 hour to 10 days)
            var genRedSeconds = Gen.Choose(3600, 432000).Select(i => (long)i);
            var genThresholds = genRedSeconds.SelectMany(
                red => Gen.Choose((int)red + 3600, (int)red + 432000).Select(i => (long)i),
                (red, yellow) => new { YellowSeconds = yellow, RedSeconds = red });

            // Generate a due window offset in seconds relative to "now"
            // Range from -86400 (1 day overdue) to 864000 (10 days ahead)
            var genOffsetSeconds = Gen.Choose(-86400, 864000).Select(i => (long)i);

            var now = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);

            return Prop.ForAll(
                genThresholds.ToArbitrary(),
                genOffsetSeconds.ToArbitrary(),
                (thresholds, offsetSeconds) =>
                {
                    var store = PreferencesStore.GetInstance();
                    store.Preferences.Thresholds.WorkerRequestYellowSeconds = thresholds.YellowSeconds;
                    store.Preferences.Thresholds.WorkerRequestRedSeconds = thresholds.RedSeconds;

                    var needBy = now.AddSeconds(offsetSeconds);
                    var commodities = new List<CommodityRequested>
                    {
                        new CommodityRequested
                        {
                            Name = "TestItem",
                            Requested = 1,
                            Delivered = 0,
                            NeedBy = needBy,
                            Fulfilled = false
                        }
                    };

                    var result = TabWarningService.EvaluateWorkerWarning(commodities, now);

                    // dueWindow = NeedBy - now = offsetSeconds (as a TimeSpan)
                    var dueWindow = TimeSpan.FromSeconds(offsetSeconds);
                    var redWindow = TimeSpan.FromSeconds(thresholds.RedSeconds);
                    var yellowWindow = TimeSpan.FromSeconds(thresholds.YellowSeconds);

                    TabWarningLevel expected;
                    if (dueWindow <= redWindow)
                        expected = TabWarningLevel.Red;
                    else if (dueWindow <= yellowWindow)
                        expected = TabWarningLevel.Yellow;
                    else
                        expected = TabWarningLevel.None;

                    return (result == expected)
                        .Label($"yellowSec={thresholds.YellowSeconds}, redSec={thresholds.RedSeconds}, " +
                               $"offsetSec={offsetSeconds}, expected={expected}, got={result}");
                });
        }

        /// <summary>
        /// Feature: preferences-form, Property 5: Colony import staleness warning respects configured thresholds
        ///
        /// For any configured ThresholdPreferences where ColonyImportStalenessYellowSeconds &lt; ColonyImportStalenessRedSeconds
        /// and both are positive, and for any valid lastImportDateTime string,
        /// EvaluateColonyImportStalenessWarning shall return Red when elapsed time is at or above the configured
        /// red threshold, Yellow when at or above the configured yellow threshold, and None otherwise.
        ///
        /// **Validates: Requirements 4.5, 4.6**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ColonyImportWarningRespectsThresholds()
        {
            // Generate valid threshold pairs: yellow < red, both positive (in seconds)
            // Use ranges from 1 hour to 15 days
            var genYellowSeconds = Gen.Choose(3600, 864000).Select(i => (long)i);
            var genThresholds = genYellowSeconds.SelectMany(
                yellow => Gen.Choose((int)yellow + 3600, (int)yellow + 864000).Select(i => (long)i),
                (yellow, red) => new { YellowSeconds = yellow, RedSeconds = red });

            // Generate elapsed seconds from 0 to 20 days
            var genElapsedSeconds = Gen.Choose(0, 1728000).Select(i => (long)i);

            var now = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

            return Prop.ForAll(
                genThresholds.ToArbitrary(),
                genElapsedSeconds.ToArbitrary(),
                (thresholds, elapsedSeconds) =>
                {
                    var store = PreferencesStore.GetInstance();
                    store.Preferences.Thresholds.ColonyImportStalenessYellowSeconds = thresholds.YellowSeconds;
                    store.Preferences.Thresholds.ColonyImportStalenessRedSeconds = thresholds.RedSeconds;

                    var importTime = now.AddSeconds(-elapsedSeconds);
                    var isoString = SurveyDateTimeParser.ToIsoString(importTime);

                    var result = TabWarningService.EvaluateColonyImportStalenessWarning(isoString, now);

                    TabWarningLevel expected;
                    if (elapsedSeconds >= thresholds.RedSeconds)
                        expected = TabWarningLevel.Red;
                    else if (elapsedSeconds >= thresholds.YellowSeconds)
                        expected = TabWarningLevel.Yellow;
                    else
                        expected = TabWarningLevel.None;

                    return (result == expected)
                        .Label($"yellowSec={thresholds.YellowSeconds}, redSec={thresholds.RedSeconds}, " +
                               $"elapsedSec={elapsedSeconds}, iso={isoString}, expected={expected}, got={result}");
                });
        }
    }
}
