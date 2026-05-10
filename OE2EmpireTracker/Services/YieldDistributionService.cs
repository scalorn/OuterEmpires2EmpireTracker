using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Pure-logic service for computing yield distribution data from surveys.
    /// No UI dependencies. Follows the same static pattern as EvolutionChainService.
    /// </summary>
    public static class YieldDistributionService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Computes the yield distribution for a given resource+purity combination
        /// across the provided surveys.
        /// </summary>
        /// <param name="surveys">Filtered survey list (already filtered by type/name/etc).</param>
        /// <param name="resourceName">Resource name to match.</param>
        /// <param name="purity">Purity level to match.</param>
        /// <param name="binWidth">Width of each bin (clamped to 1-100).</param>
        /// <returns>Distribution result with points and survey count.</returns>
        public static YieldDistributionResult ComputeDistribution(
            IReadOnlyList<ReadOnlySurvey> surveys,
            string resourceName,
            string purity,
            int binWidth)
        {
            binWidth = Math.Max(1, Math.Min(100, binWidth));

            var result = new YieldDistributionResult();

            if (surveys == null || surveys.Count == 0)
            {
                return result;
            }

            // Extract yield values matching the resource+purity combo
            var yields = new List<decimal>();
            foreach (var survey in surveys)
            {
                foreach (var kvp in survey.Resources)
                {
                    var res = kvp.Value;
                    if (string.Equals(res.Resource, resourceName, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(res.Purity, purity, StringComparison.OrdinalIgnoreCase))
                    {
                        if (decimal.TryParse(res.Amount, out decimal amount))
                        {
                            yields.Add(amount);
                        }
                    }
                }
            }

            result.MatchingSurveyCount = yields.Count;

            if (yields.Count < 2)
            {
                return result;
            }

            // Determine bin range
            decimal minYield = yields.Min();
            decimal maxYield = yields.Max();

            // Align bins to bin-width boundaries
            decimal binStart = Math.Floor(minYield / binWidth) * binWidth;
            decimal binEnd = Math.Ceiling((maxYield + 1) / binWidth) * binWidth;

            // Build bins and count
            int totalYields = yields.Count;
            for (decimal edge = binStart; edge < binEnd; edge += binWidth)
            {
                decimal lower = edge;
                decimal upper = edge + binWidth;
                decimal midpoint = edge + (binWidth / 2.0m);

                int count = yields.Count(y => y >= lower && y < upper);
                decimal percentage = (count / (decimal)totalYields) * 100.0m;

                result.Points.Add(new DistributionPoint
                {
                    BinMidpoint = midpoint,
                    Percentage = percentage,
                });
            }

            return result;
        }

        /// <summary>
        /// Returns all distinct resource+purity combinations found across the given surveys.
        /// Useful for populating the series selection dropdowns.
        /// </summary>
        /// <param name="surveys">Survey list to scan for resource+purity pairs.</param>
        /// <returns>Ordered list of distinct resource+purity combinations.</returns>
        public static List<ResourcePurityCombo> GetAvailableCombos(
            IReadOnlyList<ReadOnlySurvey> surveys)
        {
            if (surveys == null || surveys.Count == 0)
            {
                return new List<ResourcePurityCombo>();
            }

            var combos = new HashSet<ResourcePurityCombo>();

            foreach (var survey in surveys)
            {
                foreach (var kvp in survey.Resources)
                {
                    var res = kvp.Value;
                    if (!string.IsNullOrEmpty(res.Resource) && !string.IsNullOrEmpty(res.Purity))
                    {
                        combos.Add(new ResourcePurityCombo
                        {
                            ResourceName = res.Resource,
                            Purity = res.Purity,
                        });
                    }
                }
            }

            return CollectionSortHelper.OrderResourcePurityCombos(combos).ToList();
        }
    }
}
