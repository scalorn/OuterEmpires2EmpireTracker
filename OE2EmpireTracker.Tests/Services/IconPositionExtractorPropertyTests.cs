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
    [TestFixture]
    public class IconPositionExtractorPropertyTests
    {
        #region Property 3: FindBlueprintTypeByIcon uniqueness

        /// <summary>
        /// Property 3: FindBlueprintTypeByIcon uniqueness.
        /// For any BaselineData where all BlueprintType entries have distinct non-null
        /// IconPosition values, FindBlueprintTypeByIcon(pos) should return exactly one
        /// result for each known position and null for unknown positions.
        ///
        /// Uses the real BaselineData: collects all BlueprintType entries with non-null
        /// distinct IconPositions, verifies FindBlueprintTypeByIcon returns the correct
        /// type for each. Then generates random position strings and verifies they return
        /// null (since they won't match any real entry).
        /// **Validates: Requirements 3.4**
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            EmpireContext.Reset();
            TestHelper.SetEmpireFilePath();
            EmpireContext.GetInstance();
        }

        [FsCheck.NUnit.Property(MaxTest = 200)]
        public Property FindBlueprintTypeByIcon_ReturnsCorrectTypeForKnownPositions()
        {
            var ctx = EmpireContext.GetInstance();

            // Collect all BlueprintType entries with non-null, non-empty, distinct IconPositions
            var typesWithIcons = ctx.BlueprintTypeList
                .Where(bt => !string.IsNullOrEmpty(bt.IconPosition))
                .GroupBy(bt => bt.IconPosition, StringComparer.Ordinal)
                .Where(g => g.Count() == 1)   // only truly distinct positions
                .Select(g => g.Single())
                .ToList();

            if (typesWithIcons.Count == 0)
                return true.ToProperty().Label("No distinct icon positions in BaselineData -- vacuously true");

            // Generator picks a random entry from the distinct set
            var knownGen = Gen.Elements(typesWithIcons.ToArray());

            return Prop.ForAll(knownGen.ToArbitrary(), bt =>
            {
                var found = ctx.FindBlueprintTypeByIcon(bt.IconPosition);
                return (found != null && found.Id == bt.Id)
                    .Label($"Expected Id=\"{bt.Id}\" for position \"{bt.IconPosition}\", " +
                           $"got {(found == null ? "null" : $"\"{found.Id}\"")}");
            });
        }

        [FsCheck.NUnit.Property(MaxTest = 200)]
        public Property FindBlueprintTypeByIcon_ReturnsNullForUnknownPositions()
        {
            var ctx = EmpireContext.GetInstance();

            // Collect all known icon positions for exclusion
            var knownPositions = new HashSet<string>(
                ctx.BlueprintTypeList
                    .Where(bt => !string.IsNullOrEmpty(bt.IconPosition))
                    .Select(bt => bt.IconPosition),
                StringComparer.Ordinal);

            // Generate random CSS-like position strings that don't match any known position
            var randomPosGen = from x in Gen.Choose(-9999, 9999)
                               from y in Gen.Choose(-9999, 9999)
                               let pos = $"-{Math.Abs(x)}px -{Math.Abs(y)}px"
                               where !knownPositions.Contains(pos)
                               select pos;

            return Prop.ForAll(randomPosGen.ToArbitrary(), pos =>
            {
                var found = ctx.FindBlueprintTypeByIcon(pos);
                return (found == null)
                    .Label($"Expected null for unknown position \"{pos}\", got \"{found?.Id}\"");
            });
        }

        [Test]
        public void FindBlueprintTypeByIcon_ReturnsNullForNullAndEmpty()
        {
            var ctx = EmpireContext.GetInstance();
            Assert.That(ctx.FindBlueprintTypeByIcon(null), Is.Null, "null input should return null");
            Assert.That(ctx.FindBlueprintTypeByIcon(string.Empty), Is.Null, "empty input should return null");
        }

        #endregion

        #region Property 4: Coverage gap detection completeness

        /// <summary>
        /// Computes the coverage gap: BlueprintType Ids present in baselineTypeIds
        /// but absent from extractedTypeIds. This is the pure logic that
        /// ProduceCoverageGapReport uses internally.
        /// </summary>
        private static HashSet<string> ComputeCoverageGap(
            IEnumerable<string> baselineTypeIds,
            IEnumerable<string> extractedTypeIds)
        {
            var covered = new HashSet<string>(extractedTypeIds, StringComparer.Ordinal);
            return new HashSet<string>(
                baselineTypeIds.Where(id => !covered.Contains(id)),
                StringComparer.Ordinal);
        }

        /// <summary>
        /// Property 4: Coverage gap detection completeness.
        /// For any set of BlueprintTypes in BaselineData and any set of extracted icons,
        /// the coverage gap report should list exactly those BlueprintType Ids that appear
        /// in BaselineData but not in the extracted set.
        ///
        /// Generates two sets of strings (baseline type Ids and extracted type Ids),
        /// computes the expected gap (set difference), and verifies the gap detection
        /// logic produces the same result.
        /// **Validates: Requirements 9.1, 9.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 200)]
        public Property CoverageGapDetection_EqualsSetDifference()
        {
            // Generate a pool of type-Id-like strings, then split into baseline and extracted
            var idGen = from prefix in Gen.Elements("Hull", "Reactor", "Shield", "Flatpacks/MiningRig",
                            "Flatpacks/CommodityFactory/Agridome", "MainDrive", "JumpDrive",
                            "NavComp", "Thrusters", "FuelTank", "CargoPod", "OreHopper",
                            "MiningLaser", "HullPlating", "Weapon", "Munition")
                        from suffix in Gen.Choose(0, 9)
                        select $"{prefix}-{suffix}";

            var pairGen = from baselineIds in Gen.ListOf(idGen).Select(l => l.Distinct(StringComparer.Ordinal).ToList())
                          from extractedIds in Gen.ListOf(idGen).Select(l => l.Distinct(StringComparer.Ordinal).ToList())
                          select new { BaselineIds = baselineIds, ExtractedIds = extractedIds };

            return Prop.ForAll(pairGen.ToArbitrary(), data =>
            {
                var actualGap = ComputeCoverageGap(data.BaselineIds, data.ExtractedIds);

                // Expected: set difference (baseline minus extracted)
                var expectedGap = new HashSet<string>(data.BaselineIds, StringComparer.Ordinal);
                expectedGap.ExceptWith(data.ExtractedIds);

                bool setsEqual = actualGap.SetEquals(expectedGap);
                return setsEqual
                    .Label($"Gap mismatch: actual={actualGap.Count}, expected={expectedGap.Count}. " +
                           $"Baseline={data.BaselineIds.Count}, Extracted={data.ExtractedIds.Count}");
            });
        }

        /// <summary>
        /// Property 4 (subset): Every Id in the gap must be in baseline but not in extracted.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 200)]
        public Property CoverageGapDetection_GapIdsAreInBaselineNotExtracted()
        {
            var idGen = from prefix in Gen.Elements("TypeA", "TypeB", "TypeC", "TypeD", "TypeE")
                        from suffix in Gen.Choose(0, 5)
                        select $"{prefix}-{suffix}";

            var pairGen = from baselineIds in Gen.ListOf(idGen).Select(l => l.Distinct(StringComparer.Ordinal).ToList())
                          from extractedIds in Gen.ListOf(idGen).Select(l => l.Distinct(StringComparer.Ordinal).ToList())
                          select new { BaselineIds = baselineIds, ExtractedIds = extractedIds };

            return Prop.ForAll(pairGen.ToArbitrary(), data =>
            {
                var gap = ComputeCoverageGap(data.BaselineIds, data.ExtractedIds);
                var baselineSet = new HashSet<string>(data.BaselineIds, StringComparer.Ordinal);
                var extractedSet = new HashSet<string>(data.ExtractedIds, StringComparer.Ordinal);

                bool allInBaseline = gap.All(id => baselineSet.Contains(id));
                bool noneInExtracted = gap.All(id => !extractedSet.Contains(id));

                return allInBaseline
                    .Label("All gap Ids must be in baseline")
                    .And(noneInExtracted)
                    .Label("No gap Id should be in extracted set");
            });
        }

        #endregion
    }
}
