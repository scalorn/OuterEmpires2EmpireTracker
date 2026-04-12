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
    /// Property-based tests for TabWarningService.
    /// Feature: worker-tab-due-warning
    /// </summary>
    [TestFixture]
    public class TabWarningServiceTests
    {
        /// <summary>
        /// Feature: worker-tab-due-warning, Property 1: Structure warning level is determined by count thresholds
        /// **Validates: Requirements 7.1, 7.2, 8.1, 8.2, 9.1**
        ///
        /// For any non-negative integer structureCount (0–200),
        /// EvaluateStructureWarning returns Red if >= 66, Yellow if >= 60, None otherwise.
        /// </summary>
        [FsCheck.NUnit.Property]
        public Property StructureWarningLevel_IsDeterminedByCountThresholds()
        {
            return Prop.ForAll(
                Gen.Choose(0, 200).ToArbitrary(),
                count =>
                {
                    var result = TabWarningService.EvaluateStructureWarning(count);

                    TabWarningLevel expected;
                    if (count >= 66)
                        expected = TabWarningLevel.Red;
                    else if (count >= 60)
                        expected = TabWarningLevel.Yellow;
                    else
                        expected = TabWarningLevel.None;

                    return (result == expected)
                        .Label($"count={count}, expected={expected}, got={result}");
                });
        }

        /// <summary>
        /// Feature: worker-tab-due-warning, Property 2: Worker warning level is the most urgent unfulfilled due window
        /// **Validates: Requirements 1.1, 1.2, 2.1, 2.2, 3.1, 4.1, 6.1**
        ///
        /// For any list of CommodityRequested with random Fulfilled flags, random NeedBy dates
        /// (including DateTime.MinValue, past, near-future, far-future), and a random now,
        /// EvaluateWorkerWarning returns the warning level matching the most urgent unfulfilled due window.
        /// </summary>
        [FsCheck.NUnit.Property]
        public Property WorkerWarningLevel_IsMostUrgentUnfulfilledDueWindow()
        {
            // Generator for a single CommodityRequested relative to a given 'now'
            // We generate the offset first, then build the NeedBy from now + offset
            var genOffsetHours = Gen.OneOf(
                Gen.Constant(double.MinValue),           // sentinel for DateTime.MinValue
                Gen.Choose(-72, -1).Select(h => (double)h),   // past (overdue)
                Gen.Choose(0, 72).Select(h => (double)h),     // near-future (0-3 days)
                Gen.Choose(73, 240).Select(h => (double)h)    // far-future (3-10 days)
            );

            var genFulfilled = Arb.Generate<bool>();

            var genRequest = genOffsetHours
                .SelectMany(offsetHours => genFulfilled, (offsetHours, fulfilled) => new { offsetHours, fulfilled })
                .Select(t =>
                {
                    var needBy = t.offsetHours == double.MinValue
                        ? DateTime.MinValue
                        : new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Unspecified)
                            .AddHours(t.offsetHours);
                    return new CommodityRequested
                    {
                        Name = "Item",
                        Requested = 1,
                        Delivered = 0,
                        NeedBy = needBy,
                        Fulfilled = t.fulfilled
                    };
                });

            // We fix 'now' to a known value so offsets are meaningful
            var now = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);

            var genRequestList = Gen.ListOf(genRequest);

            return Prop.ForAll(
                genRequestList.ToArbitrary(),
                requests =>
                {
                    var commodities = requests.ToList();
                    var result = TabWarningService.EvaluateWorkerWarning(commodities, now);

                    // Compute expected: filter to unfulfilled with NeedBy != MinValue
                    var relevant = commodities
                        .Where(r => !r.Fulfilled && r.NeedBy != DateTime.MinValue)
                        .ToList();

                    TabWarningLevel expected;
                    if (relevant.Count == 0)
                    {
                        expected = TabWarningLevel.None;
                    }
                    else
                    {
                        var minDueWindow = relevant.Min(r => (r.NeedBy - now).TotalDays);

                        if (minDueWindow <= 1.0)
                            expected = TabWarningLevel.Red;
                        else if (minDueWindow <= 2.0)
                            expected = TabWarningLevel.Yellow;
                        else
                            expected = TabWarningLevel.None;
                    }

                    return (result == expected)
                        .Label($"requests={commodities.Count}, relevant={relevant.Count}, expected={expected}, got={result}");
                });
        }

        /// <summary>
        /// Feature: worker-tab-due-warning, Property 3: Fulfilled requests are excluded from worker warning evaluation
        /// **Validates: Requirements 1.3**
        ///
        /// For any list of CommodityRequested where every item has Fulfilled = true,
        /// EvaluateWorkerWarning returns None regardless of the NeedBy dates.
        /// </summary>
        [FsCheck.NUnit.Property]
        public Property FulfilledRequests_AreExcludedFromWorkerWarningEvaluation()
        {
            // Generator for NeedBy dates including overdue, near-future, and DateTime.MinValue
            var now = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);

            var genNeedBy = Gen.OneOf(
                Gen.Constant(DateTime.MinValue),                                          // no due date
                Gen.Choose(-72, -1).Select(h => now.AddHours(h)),                         // overdue
                Gen.Choose(0, 24).Select(h => now.AddHours(h)),                           // due within 1 day (red zone)
                Gen.Choose(25, 48).Select(h => now.AddHours(h)),                          // due within 2 days (yellow zone)
                Gen.Choose(49, 240).Select(h => now.AddHours(h))                          // far future
            );

            var genFulfilledRequest = genNeedBy.Select(needBy => new CommodityRequested
            {
                Name = "Item",
                Requested = 1,
                Delivered = 0,
                NeedBy = needBy,
                Fulfilled = true  // Always fulfilled
            });

            var genRequestList = Gen.ListOf(genFulfilledRequest);

            return Prop.ForAll(
                genRequestList.ToArbitrary(),
                requests =>
                {
                    var commodities = requests.ToList();
                    var result = TabWarningService.EvaluateWorkerWarning(commodities, now);

                    return (result == TabWarningLevel.None)
                        .Label($"requests={commodities.Count}, expected=None, got={result}");
                });
        }

        // ── Unit Tests: Structure Warning Edge Cases ──
        // **Validates: Requirements 7.1, 7.2, 8.1, 8.2, 9.1**

        // ── Property Test: Colony Import Staleness Warning ──

        /// <summary>
        /// Feature: colony-import-timestamp, Property 5: TabWarningService returns correct warning level for import staleness
        /// **Validates: Requirements 5.2, 5.3, 5.4, 5.5**
        ///
        /// For any valid ISO 8601 timestamp string and reference DateTime now,
        /// EvaluateColonyImportStalenessWarning returns Red when elapsed >= 6 days,
        /// Yellow when elapsed >= 5 days and &lt; 6 days, and None when elapsed &lt; 5 days.
        /// For null or empty input, it returns Red.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ImportStalenessWarning_ReturnsCorrectLevelForElapsedTime()
        {
            // Generate elapsed days as a double in [0, 15] range to cover all thresholds
            var genElapsedDays = Gen.Choose(0, 15000).Select(m => m / 1000.0); // 0.000 to 15.000 days

            // Fixed reference 'now'
            var now = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

            return Prop.ForAll(
                genElapsedDays.ToArbitrary(),
                elapsedDays =>
                {
                    var importTime = now.AddDays(-elapsedDays);
                    var isoString = SurveyDateTimeParser.ToIsoString(importTime);

                    var result = TabWarningService.EvaluateColonyImportStalenessWarning(isoString, now);

                    TabWarningLevel expected;
                    if (elapsedDays >= 6.0)
                        expected = TabWarningLevel.Red;
                    else if (elapsedDays >= 5.0)
                        expected = TabWarningLevel.Yellow;
                    else
                        expected = TabWarningLevel.None;

                    return (result == expected)
                        .Label($"elapsed={elapsedDays:F3}d, iso={isoString}, expected={expected}, got={result}");
                });
        }

        /// <summary>
        /// Feature: colony-import-timestamp, Property 5 (null/empty case)
        /// **Validates: Requirements 5.5**
        ///
        /// For null or empty LastImportDateTime, EvaluateColonyImportStalenessWarning returns Red.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ImportStalenessWarning_NullOrEmpty_ReturnsRed()
        {
            var genNullOrEmpty = Gen.OneOf(
                Gen.Constant((string)null),
                Gen.Constant(string.Empty));

            var now = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

            return Prop.ForAll(
                genNullOrEmpty.ToArbitrary(),
                input =>
                {
                    var result = TabWarningService.EvaluateColonyImportStalenessWarning(input, now);
                    return (result == TabWarningLevel.Red)
                        .Label($"input={input ?? "null"}, expected=Red, got={result}");
                });
        }

        // ── Unit Tests: Structure Warning Edge Cases ──
        // **Validates: Requirements 7.1, 7.2, 8.1, 8.2, 9.1**

        private static readonly DateTime Now = new DateTime(2025, 6, 15, 12, 0, 0);

        [Test]
        public void StructureWarning_0Structures_ReturnsNone()
        {
            Assert.That(TabWarningService.EvaluateStructureWarning(0), Is.EqualTo(TabWarningLevel.None));
        }

        [Test]
        public void StructureWarning_59Structures_ReturnsNone()
        {
            Assert.That(TabWarningService.EvaluateStructureWarning(59), Is.EqualTo(TabWarningLevel.None));
        }

        [Test]
        public void StructureWarning_60Structures_ReturnsYellow()
        {
            Assert.That(TabWarningService.EvaluateStructureWarning(60), Is.EqualTo(TabWarningLevel.Yellow));
        }

        [Test]
        public void StructureWarning_65Structures_ReturnsYellow()
        {
            Assert.That(TabWarningService.EvaluateStructureWarning(65), Is.EqualTo(TabWarningLevel.Yellow));
        }

        [Test]
        public void StructureWarning_66Structures_ReturnsRed()
        {
            Assert.That(TabWarningService.EvaluateStructureWarning(66), Is.EqualTo(TabWarningLevel.Red));
        }

        // ── Unit Tests: Worker Warning Edge Cases ──
        // **Validates: Requirements 1.1, 1.2, 1.3, 2.1, 2.2, 3.1, 4.1, 4.2, 6.1**

        [Test]
        public void WorkerWarning_NoCommodityRequests_ReturnsNone()
        {
            var result = TabWarningService.EvaluateWorkerWarning(new List<CommodityRequested>(), Now);
            Assert.That(result, Is.EqualTo(TabWarningLevel.None));
        }

        [Test]
        public void WorkerWarning_AllFulfilledEvenUrgent_ReturnsNone()
        {
            var commodities = new List<CommodityRequested>
            {
                new CommodityRequested { Name = "A", Requested = 1, Delivered = 0, NeedBy = Now.AddHours(-5), Fulfilled = true },
                new CommodityRequested { Name = "B", Requested = 1, Delivered = 0, NeedBy = Now.AddHours(6), Fulfilled = true },
                new CommodityRequested { Name = "C", Requested = 1, Delivered = 0, NeedBy = Now.AddHours(30), Fulfilled = true }
            };
            var result = TabWarningService.EvaluateWorkerWarning(commodities, Now);
            Assert.That(result, Is.EqualTo(TabWarningLevel.None));
        }

        [Test]
        public void WorkerWarning_SingleUnfulfilledDueIn3Days_ReturnsNone()
        {
            var commodities = new List<CommodityRequested>
            {
                new CommodityRequested { Name = "A", Requested = 1, Delivered = 0, NeedBy = Now.AddDays(3), Fulfilled = false }
            };
            var result = TabWarningService.EvaluateWorkerWarning(commodities, Now);
            Assert.That(result, Is.EqualTo(TabWarningLevel.None));
        }

        [Test]
        public void WorkerWarning_SingleUnfulfilledDueIn1Point5Days_ReturnsYellow()
        {
            var commodities = new List<CommodityRequested>
            {
                new CommodityRequested { Name = "A", Requested = 1, Delivered = 0, NeedBy = Now.AddDays(1.5), Fulfilled = false }
            };
            var result = TabWarningService.EvaluateWorkerWarning(commodities, Now);
            Assert.That(result, Is.EqualTo(TabWarningLevel.Yellow));
        }

        [Test]
        public void WorkerWarning_SingleUnfulfilledDueIn12Hours_ReturnsRed()
        {
            var commodities = new List<CommodityRequested>
            {
                new CommodityRequested { Name = "A", Requested = 1, Delivered = 0, NeedBy = Now.AddHours(12), Fulfilled = false }
            };
            var result = TabWarningService.EvaluateWorkerWarning(commodities, Now);
            Assert.That(result, Is.EqualTo(TabWarningLevel.Red));
        }

        [Test]
        public void WorkerWarning_SingleUnfulfilledOverdue_ReturnsRed()
        {
            var commodities = new List<CommodityRequested>
            {
                new CommodityRequested { Name = "A", Requested = 1, Delivered = 0, NeedBy = Now.AddDays(-1), Fulfilled = false }
            };
            var result = TabWarningService.EvaluateWorkerWarning(commodities, Now);
            Assert.That(result, Is.EqualTo(TabWarningLevel.Red));
        }

        [Test]
        public void WorkerWarning_NeedByMinValue_IsExcluded()
        {
            var commodities = new List<CommodityRequested>
            {
                new CommodityRequested { Name = "A", Requested = 1, Delivered = 0, NeedBy = DateTime.MinValue, Fulfilled = false }
            };
            var result = TabWarningService.EvaluateWorkerWarning(commodities, Now);
            Assert.That(result, Is.EqualTo(TabWarningLevel.None));
        }

        [Test]
        public void WorkerWarning_MixOfRedAndYellow_RedWins()
        {
            var commodities = new List<CommodityRequested>
            {
                new CommodityRequested { Name = "A", Requested = 1, Delivered = 0, NeedBy = Now.AddDays(1.5), Fulfilled = false },
                new CommodityRequested { Name = "B", Requested = 1, Delivered = 0, NeedBy = Now.AddHours(12), Fulfilled = false }
            };
            var result = TabWarningService.EvaluateWorkerWarning(commodities, Now);
            Assert.That(result, Is.EqualTo(TabWarningLevel.Red));
        }
    }
}
