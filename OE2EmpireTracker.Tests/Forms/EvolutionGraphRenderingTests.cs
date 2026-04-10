using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Forms
{
    [TestFixture]
    public class EvolutionGraphRenderingTests
    {
        #region Property 5: Segment dash style matches evolution gap classification

        /// <summary>
        /// // Feature: evolution-graph, Property 5: Segment dash style matches evolution gap classification
        /// Generate chains with gaps (non-consecutive evolution levels); build segments
        /// and verify solid/dashed classification matches gap detection
        /// (consecutive = solid, gap > 1 = dashed).
        /// **Validates: Requirements 5.1, 5.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property SegmentDashStyle_MatchesEvolutionGapClassification()
        {
            // Generate 2–16 distinct evolution levels from 0..15, sorted ascending.
            // This guarantees at least one segment and allows both gaps and consecutive pairs.
            var pointsGen = Gen.Choose(2, 16).SelectMany(count =>
                Gen.Shuffle(Enumerable.Range(0, 16).ToArray())
                    .Select(shuffled => shuffled.Take(count).OrderBy(x => x).ToArray())
            ).SelectMany(evLevels =>
            {
                // For each evolution level, generate a random percent value (1.0–500.0)
                var percentsGen = Gen.Sequence(
                    evLevels.Select(_ => Gen.Choose(1, 500).Select(v => (decimal)v))
                );

                return percentsGen.Select(percents =>
                {
                    var pArr = percents.ToArray();
                    var points = new List<(int Evolution, decimal Percent)>();
                    for (int i = 0; i < evLevels.Length; i++)
                    {
                        points.Add((evLevels[i], pArr[i]));
                    }
                    return points;
                });
            });

            return Prop.ForAll(pointsGen.ToArbitrary(), points =>
            {
                var segments = EvolutionChainService.ClassifySegments(points);

                // There should be exactly (points.Count - 1) segments
                var sorted = points.OrderBy(p => p.Evolution).ToList();
                bool correctCount = segments.Count == sorted.Count - 1;

                // Verify each segment's IsGap matches the evolution level difference
                bool allClassificationsCorrect = true;
                string classificationError = "";

                for (int i = 0; i < segments.Count; i++)
                {
                    var seg = segments[i];
                    int expectedEvFrom = sorted[i].Evolution;
                    int expectedEvTo = sorted[i + 1].Evolution;
                    int evDiff = expectedEvTo - expectedEvFrom;
                    bool expectedIsGap = evDiff > 1;

                    if (seg.From.Evolution != expectedEvFrom ||
                        seg.To.Evolution != expectedEvTo)
                    {
                        allClassificationsCorrect = false;
                        classificationError = $"Segment {i}: expected Ev{expectedEvFrom}→Ev{expectedEvTo}, " +
                                              $"got Ev{seg.From.Evolution}→Ev{seg.To.Evolution}";
                        break;
                    }

                    if (seg.IsGap != expectedIsGap)
                    {
                        allClassificationsCorrect = false;
                        classificationError = $"Segment {i} (Ev{seg.From.Evolution}→Ev{seg.To.Evolution}): " +
                                              $"evDiff={evDiff}, expected IsGap={expectedIsGap}, got IsGap={seg.IsGap}";
                        break;
                    }
                }

                return correctCount
                    .Label($"Segment count: expected={sorted.Count - 1}, got={segments.Count}")
                    .And(allClassificationsCorrect)
                    .Label($"Classification correct: {classificationError}");
            });
        }

        #endregion

        #region Property 6: Distinct color assignment per property

        /// <summary>
        /// // Feature: evolution-graph, Property 6: Distinct color assignment per property
        /// Generate random property name lists of size 1–16; verify all assigned colors
        /// are distinct and from the extended Wong palette.
        /// **Validates: Requirements 5.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property DistinctColorAssignment_PerProperty()
        {
            // Generate a random count of properties (1–16), then create that many unique property names.
            var propertyNamesGen = Gen.Choose(1, 16).SelectMany(count =>
            {
                // Generate 'count' unique property names by shuffling a pool and taking the first 'count'
                var pool = Enumerable.Range(0, 16).Select(i => $"Prop_{i}").ToArray();
                return Gen.Shuffle(pool).Select(shuffled => shuffled.Take(count).ToArray());
            });

            return Prop.ForAll(propertyNamesGen.ToArbitrary(), propertyNames =>
            {
                var palette = FormBlueprint.WongPalette;

                // Assign colors using the same index-based logic as RefreshEvolutionGraph
                var assignedColors = new Color[propertyNames.Length];
                for (int i = 0; i < propertyNames.Length; i++)
                {
                    assignedColors[i] = palette[i % palette.Length];
                }

                // All assigned colors must be from the Wong palette
                bool allFromPalette = assignedColors.All(c => palette.Contains(c));

                // All assigned colors must be distinct (no two properties share the same color)
                bool allDistinct = assignedColors.Distinct().Count() == assignedColors.Length;

                return allFromPalette
                    .Label($"All colors from Wong palette: {allFromPalette}")
                    .And(allDistinct)
                    .Label($"All colors distinct: expected {propertyNames.Length} distinct, got {assignedColors.Distinct().Count()}");
            });
        }

        #endregion
    }
}
