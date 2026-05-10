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
    /// Property-based and example-based tests for YieldDistributionService.
    /// </summary>
    [TestFixture]
    public class YieldDistributionServiceTests
    {
        private static readonly string[] ResourceNames = { "Iron", "Gold", "Silver", "Copper", "Titanium" };
        private static readonly string[] Purities = { "High", "Medium", "Low" };

        // ----------------------------------------------------------------
        //  Generators
        // ----------------------------------------------------------------

        /// <summary>
        /// Generates a list of ReadOnlySurvey with controlled resource/purity/amount values.
        /// </summary>
        private static Gen<List<ReadOnlySurvey>> SurveyListGen(int minCount, int maxCount)
        {
            return Gen.Choose(minCount, maxCount).SelectMany(count =>
                Gen.ListOf(count, SurveyGen()).Select(surveys => surveys.ToList()));
        }

        private static Gen<ReadOnlySurvey> SurveyGen()
        {
            return from surveyType in Gen.Elements(SurveyType.Planet, SurveyType.Asteroid)
                   from resourceCount in Gen.Choose(1, 4)
                   from resources in Gen.ListOf(resourceCount, SurveyResourceGen())
                   from id in Gen.Choose(1, 999999)
                   select BuildSurvey(surveyType, resources.ToList(), id);
        }

        private static Gen<SurveyResource> SurveyResourceGen()
        {
            return from resource in Gen.Elements(ResourceNames)
                   from purity in Gen.Elements(Purities)
                   from amount in Gen.Choose(1, 500)
                   select new SurveyResource(resource, purity, amount.ToString());
        }

        private static ReadOnlySurvey BuildSurvey(SurveyType type, List<SurveyResource> resources, int id)
        {
            var survey = new Survey("TestSurvey" + id)
            {
                UUID = Guid.NewGuid().ToString(),
                SurveyType = type,
                PlanetName = "TestPlanet",
                NickName = "Test",
            };

            for (int i = 0; i < resources.Count; i++)
            {
                survey.Resources[$"res_{id}_{i}"] = resources[i];
            }

            return new ReadOnlySurvey(survey);
        }

        /// <summary>
        /// Generates a valid bin width in range [1, 100].
        /// </summary>
        private static Gen<int> ValidBinWidthGen()
        {
            return Gen.Choose(1, 100);
        }

        /// <summary>
        /// Generates a bin width that may be outside valid range.
        /// </summary>
        private static Gen<int> AnyBinWidthGen()
        {
            return Gen.OneOf(
                Gen.Choose(-50, 0),
                Gen.Choose(1, 100),
                Gen.Choose(101, 500));
        }

        // ----------------------------------------------------------------
        //  Property 1: Type Filtering Correctness
        //  **Validates: Requirements 2.1**
        // ----------------------------------------------------------------

        /// <summary>
        /// Feature: survey-yield-distribution, Property 1: Type Filtering Correctness
        ///
        /// When surveys are pre-filtered by SurveyType before calling ComputeDistribution,
        /// only surveys of that type contribute to the result.
        /// **Validates: Requirements 2.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property TypeFilteringCorrectness_FilteredSurveysOnlyContainSpecifiedType()
        {
            var gen = from surveys in SurveyListGen(2, 20)
                      from targetType in Gen.Elements(SurveyType.Planet, SurveyType.Asteroid)
                      from resource in Gen.Elements(ResourceNames)
                      from purity in Gen.Elements(Purities)
                      from binWidth in ValidBinWidthGen()
                      select new { surveys, targetType, resource, purity, binWidth };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var filtered = data.surveys.Where(s => s.SurveyType == data.targetType).ToList();
                var result = YieldDistributionService.ComputeDistribution(
                    filtered, data.resource, data.purity, data.binWidth);

                // Count expected matches manually from filtered list only
                int expectedCount = 0;
                foreach (var survey in filtered)
                {
                    foreach (var kvp in survey.Resources)
                    {
                        var res = kvp.Value;
                        if (string.Equals(res.Resource, data.resource, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(res.Purity, data.purity, StringComparison.OrdinalIgnoreCase)
                            && decimal.TryParse(res.Amount, out _))
                        {
                            expectedCount++;
                        }
                    }
                }

                return (result.MatchingSurveyCount == expectedCount)
                    .Label($"Expected {expectedCount} matches but got {result.MatchingSurveyCount}");
            });
        }

        // ----------------------------------------------------------------
        //  Property 2: Resource+Purity Extraction Accuracy
        //  **Validates: Requirements 5.1**
        // ----------------------------------------------------------------

        /// <summary>
        /// Feature: survey-yield-distribution, Property 2: Resource+Purity Extraction Accuracy
        ///
        /// MatchingSurveyCount equals the number of parseable yields matching the resource+purity.
        /// **Validates: Requirements 5.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ResourcePurityExtractionAccuracy_MatchingSurveyCountEqualsExpected()
        {
            var gen = from surveys in SurveyListGen(0, 15)
                      from resource in Gen.Elements(ResourceNames)
                      from purity in Gen.Elements(Purities)
                      from binWidth in ValidBinWidthGen()
                      select new { surveys, resource, purity, binWidth };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var result = YieldDistributionService.ComputeDistribution(
                    data.surveys, data.resource, data.purity, data.binWidth);

                int expectedCount = 0;
                foreach (var survey in data.surveys)
                {
                    foreach (var kvp in survey.Resources)
                    {
                        var res = kvp.Value;
                        if (string.Equals(res.Resource, data.resource, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(res.Purity, data.purity, StringComparison.OrdinalIgnoreCase)
                            && decimal.TryParse(res.Amount, out _))
                        {
                            expectedCount++;
                        }
                    }
                }

                return (result.MatchingSurveyCount == expectedCount)
                    .Label($"Expected {expectedCount} but got {result.MatchingSurveyCount}");
            });
        }

        // ----------------------------------------------------------------
        //  Property 3: Percentage Sum Invariant
        //  **Validates: Requirements 5.3**
        // ----------------------------------------------------------------

        /// <summary>
        /// Feature: survey-yield-distribution, Property 3: Percentage Sum Invariant
        ///
        /// The sum of all bin percentages equals 100% (±0.01%) when there are sufficient data points.
        /// **Validates: Requirements 5.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property PercentageSumInvariant_SumEquals100()
        {
            var gen = from surveys in SurveyListGen(5, 20)
                      from binWidth in ValidBinWidthGen()
                      select new { surveys, binWidth };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                // Pick a resource+purity that exists in the data
                var combo = FindExistingCombo(data.surveys);
                if (combo == null)
                {
                    return true.Label("No matching combo found - vacuously true");
                }

                var result = YieldDistributionService.ComputeDistribution(
                    data.surveys, combo.ResourceName, combo.Purity, data.binWidth);

                if (result.InsufficientData)
                {
                    return true.Label("Insufficient data - vacuously true");
                }

                decimal sum = result.Points.Sum(p => p.Percentage);
                return (Math.Abs(sum - 100.0m) <= 0.01m)
                    .Label($"Sum was {sum}, expected ~100");
            });
        }

        // ----------------------------------------------------------------
        //  Property 4: Bin Midpoint Correctness
        //  **Validates: Requirements 5.4**
        // ----------------------------------------------------------------

        /// <summary>
        /// Feature: survey-yield-distribution, Property 4: Bin Midpoint Correctness
        ///
        /// Each midpoint equals lower edge + binWidth/2.
        /// **Validates: Requirements 5.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property BinMidpointCorrectness_MidpointEqualsLowerPlusHalfWidth()
        {
            var gen = from surveys in SurveyListGen(5, 20)
                      from binWidth in ValidBinWidthGen()
                      select new { surveys, binWidth };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var combo = FindExistingCombo(data.surveys);
                if (combo == null)
                {
                    return true.Label("No matching combo - vacuously true");
                }

                var result = YieldDistributionService.ComputeDistribution(
                    data.surveys, combo.ResourceName, combo.Purity, data.binWidth);

                if (result.InsufficientData || result.Points.Count == 0)
                {
                    return true.Label("Insufficient data - vacuously true");
                }

                // First midpoint should be binStart + binWidth/2
                // Subsequent midpoints should be spaced exactly binWidth apart
                decimal firstMidpoint = result.Points[0].BinMidpoint;
                decimal halfWidth = data.binWidth / 2.0m;

                bool allCorrect = true;
                for (int i = 1; i < result.Points.Count; i++)
                {
                    decimal expectedMidpoint = firstMidpoint + (i * data.binWidth);
                    if (result.Points[i].BinMidpoint != expectedMidpoint)
                    {
                        allCorrect = false;
                        break;
                    }
                }

                // Verify first midpoint is aligned: (firstMidpoint - halfWidth) should be a multiple of binWidth
                decimal firstLower = firstMidpoint - halfWidth;
                bool firstAligned = firstLower % data.binWidth == 0;

                return (allCorrect && firstAligned)
                    .Label($"Midpoints not correctly spaced or aligned. First={firstMidpoint}, binWidth={data.binWidth}");
            });
        }

        // ----------------------------------------------------------------
        //  Property 5: Bin Coverage Completeness
        //  **Validates: Requirements 5.2**
        // ----------------------------------------------------------------

        /// <summary>
        /// Feature: survey-yield-distribution, Property 5: Bin Coverage Completeness
        ///
        /// Every yield value maps to exactly one bin (no gaps, no overlaps).
        /// **Validates: Requirements 5.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property BinCoverageCompleteness_EveryYieldMapsToExactlyOneBin()
        {
            var gen = from surveys in SurveyListGen(5, 20)
                      from binWidth in ValidBinWidthGen()
                      select new { surveys, binWidth };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var combo = FindExistingCombo(data.surveys);
                if (combo == null)
                {
                    return true.Label("No matching combo - vacuously true");
                }

                var result = YieldDistributionService.ComputeDistribution(
                    data.surveys, combo.ResourceName, combo.Purity, data.binWidth);

                if (result.InsufficientData || result.Points.Count == 0)
                {
                    return true.Label("Insufficient data - vacuously true");
                }

                // Extract yields manually
                var yields = ExtractYields(data.surveys, combo.ResourceName, combo.Purity);
                decimal halfWidth = data.binWidth / 2.0m;

                // For each yield, count how many bins it falls into
                bool allInExactlyOne = true;
                foreach (decimal y in yields)
                {
                    int binCount = 0;
                    foreach (var point in result.Points)
                    {
                        decimal lower = point.BinMidpoint - halfWidth;
                        decimal upper = point.BinMidpoint + halfWidth;
                        if (y >= lower && y < upper)
                        {
                            binCount++;
                        }
                    }

                    if (binCount != 1)
                    {
                        allInExactlyOne = false;
                        break;
                    }
                }

                return allInExactlyOne
                    .Label("Not every yield mapped to exactly one bin");
            });
        }

        // ----------------------------------------------------------------
        //  Property 6: Bin Width Clamping
        //  **Validates: Requirements 6.4, 6.5**
        // ----------------------------------------------------------------

        /// <summary>
        /// Feature: survey-yield-distribution, Property 6: Bin Width Clamping
        ///
        /// Effective bin width is always in [1, 100] regardless of input.
        /// **Validates: Requirements 6.4, 6.5**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property BinWidthClamping_EffectiveWidthAlwaysInRange()
        {
            var gen = from binWidth in AnyBinWidthGen()
                      select binWidth;

            return Prop.ForAll(gen.ToArbitrary(), rawBinWidth =>
            {
                // Create surveys with known yields to verify clamping behavior
                var surveys = CreateSurveysWithYields("Iron", "High", new[] { 10m, 50m, 100m });
                var result = YieldDistributionService.ComputeDistribution(
                    surveys, "Iron", "High", rawBinWidth);

                // With 3 yields, we should get points (not insufficient data)
                if (result.InsufficientData)
                {
                    return false.Label("Should have sufficient data with 3 yields");
                }

                // Verify midpoints are spaced by the clamped bin width
                int clampedWidth = Math.Max(1, Math.Min(100, rawBinWidth));
                if (result.Points.Count >= 2)
                {
                    decimal spacing = result.Points[1].BinMidpoint - result.Points[0].BinMidpoint;
                    return (spacing == clampedWidth)
                        .Label($"Spacing {spacing} != clamped width {clampedWidth} (raw={rawBinWidth})");
                }

                return true.Label("Single bin - spacing check not applicable");
            });
        }

        // ----------------------------------------------------------------
        //  Property 7: Insufficient Data Detection
        //  **Validates: Requirements 5.5**
        // ----------------------------------------------------------------

        /// <summary>
        /// Feature: survey-yield-distribution, Property 7: Insufficient Data Detection
        ///
        /// InsufficientData is true and Points is empty when fewer than 2 yields match.
        /// **Validates: Requirements 5.5**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property InsufficientDataDetection_TrueAndEmptyWhenLessThan2Matches()
        {
            var gen = from surveys in SurveyListGen(0, 10)
                      from resource in Gen.Elements(ResourceNames)
                      from purity in Gen.Elements(Purities)
                      from binWidth in ValidBinWidthGen()
                      select new { surveys, resource, purity, binWidth };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var result = YieldDistributionService.ComputeDistribution(
                    data.surveys, data.resource, data.purity, data.binWidth);

                if (result.MatchingSurveyCount < 2)
                {
                    bool insufficientFlagCorrect = result.InsufficientData;
                    bool pointsEmpty = result.Points.Count == 0;
                    return (insufficientFlagCorrect && pointsEmpty)
                        .Label($"Count={result.MatchingSurveyCount}, InsufficientData={result.InsufficientData}, Points.Count={result.Points.Count}");
                }

                // When >= 2 matches, InsufficientData should be false
                return (!result.InsufficientData)
                    .Label($"Count={result.MatchingSurveyCount} but InsufficientData={result.InsufficientData}");
            });
        }

        // ----------------------------------------------------------------
        //  Property 8: Axis Range Coverage
        //  **Validates: Requirements 7.3**
        // ----------------------------------------------------------------

        /// <summary>
        /// Feature: survey-yield-distribution, Property 8: Axis Range Coverage
        ///
        /// All yields fall within the bin range (between first bin lower and last bin upper).
        /// **Validates: Requirements 7.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property AxisRangeCoverage_AllYieldsFallWithinBinRange()
        {
            var gen = from surveys in SurveyListGen(5, 20)
                      from binWidth in ValidBinWidthGen()
                      select new { surveys, binWidth };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var combo = FindExistingCombo(data.surveys);
                if (combo == null)
                {
                    return true.Label("No matching combo - vacuously true");
                }

                var result = YieldDistributionService.ComputeDistribution(
                    data.surveys, combo.ResourceName, combo.Purity, data.binWidth);

                if (result.InsufficientData || result.Points.Count == 0)
                {
                    return true.Label("Insufficient data - vacuously true");
                }

                var yields = ExtractYields(data.surveys, combo.ResourceName, combo.Purity);
                decimal halfWidth = data.binWidth / 2.0m;
                decimal rangeMin = result.Points.First().BinMidpoint - halfWidth;
                decimal rangeMax = result.Points.Last().BinMidpoint + halfWidth;

                bool allInRange = yields.All(y => y >= rangeMin && y < rangeMax);
                return allInRange
                    .Label($"Yield outside range [{rangeMin}, {rangeMax})");
            });
        }

        // ----------------------------------------------------------------
        //  Property 9: GetAvailableCombos Completeness
        //  **Validates: Requirements 4.1**
        // ----------------------------------------------------------------

        /// <summary>
        /// Feature: survey-yield-distribution, Property 9: GetAvailableCombos Completeness
        ///
        /// Returns the exact distinct set of resource+purity pairs found in surveys.
        /// **Validates: Requirements 4.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property GetAvailableCombosCompleteness_ReturnsExactDistinctSet()
        {
            var gen = SurveyListGen(1, 15);

            return Prop.ForAll(gen.ToArbitrary(), surveys =>
            {
                var result = YieldDistributionService.GetAvailableCombos(surveys);

                // Compute expected set manually
                var expectedSet = new HashSet<string>();
                foreach (var survey in surveys)
                {
                    foreach (var kvp in survey.Resources)
                    {
                        var res = kvp.Value;
                        if (!string.IsNullOrEmpty(res.Resource) && !string.IsNullOrEmpty(res.Purity))
                        {
                            expectedSet.Add($"{res.Resource.ToLowerInvariant()}|{res.Purity.ToLowerInvariant()}");
                        }
                    }
                }

                var actualSet = new HashSet<string>(
                    result.Select(c => $"{c.ResourceName.ToLowerInvariant()}|{c.Purity.ToLowerInvariant()}"));

                bool setsEqual = expectedSet.SetEquals(actualSet);
                return setsEqual
                    .Label($"Expected {expectedSet.Count} combos, got {actualSet.Count}");
            });
        }

        // ================================================================
        //  Example-Based Edge-Case Tests
        // ================================================================

        /// <summary>
        /// Empty survey list returns empty result with zero count.
        /// </summary>
        [Test]
        public void EmptySurveyList_ReturnsEmptyResult()
        {
            var surveys = new List<ReadOnlySurvey>();
            var result = YieldDistributionService.ComputeDistribution(surveys, "Iron", "High", 10);

            Assert.That(result.MatchingSurveyCount, Is.EqualTo(0));
            Assert.That(result.Points, Is.Empty);
            Assert.That(result.InsufficientData, Is.True);
        }

        /// <summary>
        /// Single survey returns InsufficientData (need at least 2).
        /// </summary>
        [Test]
        public void SingleSurvey_ReturnsInsufficientData()
        {
            var surveys = CreateSurveysWithYields("Iron", "High", new[] { 50m });
            var result = YieldDistributionService.ComputeDistribution(surveys, "Iron", "High", 10);

            Assert.That(result.MatchingSurveyCount, Is.EqualTo(1));
            Assert.That(result.InsufficientData, Is.True);
            Assert.That(result.Points, Is.Empty);
        }

        /// <summary>
        /// All yields identical produces single bin at 100%.
        /// </summary>
        [Test]
        public void AllYieldsIdentical_ProducesSingleBinAt100Percent()
        {
            var surveys = CreateSurveysWithYields("Gold", "Medium", new[] { 25m, 25m, 25m, 25m });
            var result = YieldDistributionService.ComputeDistribution(surveys, "Gold", "Medium", 10);

            Assert.That(result.InsufficientData, Is.False);
            Assert.That(result.Points.Count, Is.EqualTo(1));
            Assert.That(result.Points[0].Percentage, Is.EqualTo(100.0m));
        }

        /// <summary>
        /// Non-numeric Amount values are skipped gracefully (not counted).
        /// </summary>
        [Test]
        public void NonNumericAmountValues_AreSkippedGracefully()
        {
            var survey1 = new Survey("S1")
            {
                UUID = Guid.NewGuid().ToString(),
                SurveyType = SurveyType.Planet,
                PlanetName = "TestPlanet",
                NickName = "Test",
            };
            survey1.Resources["r1"] = new SurveyResource("Iron", "High", "abc");
            survey1.Resources["r2"] = new SurveyResource("Iron", "High", string.Empty);
            survey1.Resources["r3"] = new SurveyResource("Iron", "High", "N/A");

            var survey2 = new Survey("S2")
            {
                UUID = Guid.NewGuid().ToString(),
                SurveyType = SurveyType.Planet,
                PlanetName = "TestPlanet2",
                NickName = "Test2",
            };
            survey2.Resources["r1"] = new SurveyResource("Iron", "High", "50");
            survey2.Resources["r2"] = new SurveyResource("Iron", "High", "60");

            var surveys = new List<ReadOnlySurvey>
            {
                new ReadOnlySurvey(survey1),
                new ReadOnlySurvey(survey2),
            };

            var result = YieldDistributionService.ComputeDistribution(surveys, "Iron", "High", 10);

            // Only 2 numeric values should be counted (50 and 60)
            Assert.That(result.MatchingSurveyCount, Is.EqualTo(2));
            Assert.That(result.InsufficientData, Is.False);
        }

        /// <summary>
        /// Yields at bin boundaries are assigned correctly (lower-inclusive, upper-exclusive).
        /// </summary>
        [Test]
        public void YieldsAtBinBoundaries_AssignedCorrectly()
        {
            // With binWidth=10, bins are [0,10), [10,20), [20,30)
            // Yield of exactly 10 should go into [10,20), not [0,10)
            var surveys = CreateSurveysWithYields("Silver", "Low", new[] { 0m, 10m, 20m });
            var result = YieldDistributionService.ComputeDistribution(surveys, "Silver", "Low", 10);

            Assert.That(result.InsufficientData, Is.False);

            // Each yield should be in its own bin
            Assert.That(result.Points.Count, Is.EqualTo(3));

            // Each bin should have 1/3 of the total = 33.33...%
            foreach (var point in result.Points)
            {
                decimal expected = 100.0m / 3.0m;
                Assert.That(point.Percentage, Is.EqualTo(expected).Within(0.01m));
            }
        }

        // ----------------------------------------------------------------
        //  Helper Methods
        // ----------------------------------------------------------------

        /// <summary>
        /// Finds the first resource+purity combo that has at least 2 matching yields.
        /// </summary>
        private static ResourcePurityCombo FindExistingCombo(List<ReadOnlySurvey> surveys)
        {
            var comboCounts = new Dictionary<string, ResourcePurityCombo>();
            var counts = new Dictionary<string, int>();

            foreach (var survey in surveys)
            {
                foreach (var kvp in survey.Resources)
                {
                    var res = kvp.Value;
                    if (!string.IsNullOrEmpty(res.Resource) && !string.IsNullOrEmpty(res.Purity)
                        && decimal.TryParse(res.Amount, out _))
                    {
                        string key = $"{res.Resource.ToLowerInvariant()}|{res.Purity.ToLowerInvariant()}";
                        if (!comboCounts.ContainsKey(key))
                        {
                            comboCounts[key] = new ResourcePurityCombo
                            {
                                ResourceName = res.Resource,
                                Purity = res.Purity,
                            };
                            counts[key] = 0;
                        }

                        counts[key]++;
                    }
                }
            }

            var match = counts.FirstOrDefault(c => c.Value >= 2);
            if (match.Key != null)
            {
                return comboCounts[match.Key];
            }

            return null;
        }

        /// <summary>
        /// Extracts decimal yield values for a given resource+purity from surveys.
        /// </summary>
        private static List<decimal> ExtractYields(
            List<ReadOnlySurvey> surveys, string resource, string purity)
        {
            var yields = new List<decimal>();
            foreach (var survey in surveys)
            {
                foreach (var kvp in survey.Resources)
                {
                    var res = kvp.Value;
                    if (string.Equals(res.Resource, resource, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(res.Purity, purity, StringComparison.OrdinalIgnoreCase)
                        && decimal.TryParse(res.Amount, out decimal amount))
                    {
                        yields.Add(amount);
                    }
                }
            }

            return yields;
        }

        /// <summary>
        /// Creates a list of surveys each containing one resource with the specified yields.
        /// </summary>
        private static List<ReadOnlySurvey> CreateSurveysWithYields(
            string resource, string purity, decimal[] yields)
        {
            var surveys = new List<ReadOnlySurvey>();
            for (int i = 0; i < yields.Length; i++)
            {
                var survey = new Survey("Survey" + i)
                {
                    UUID = Guid.NewGuid().ToString(),
                    SurveyType = SurveyType.Planet,
                    PlanetName = "Planet" + i,
                    NickName = "Nick" + i,
                };
                survey.Resources[$"res_{i}"] = new SurveyResource(resource, purity, yields[i].ToString());
                surveys.Add(new ReadOnlySurvey(survey));
            }

            return surveys;
        }
    }
}
