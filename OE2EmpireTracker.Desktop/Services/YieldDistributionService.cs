using System;
using System.Collections.Generic;
using System.Linq;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Static service that computes yield distribution data from surveys.
/// Satisfies REQ-SRV-090 through REQ-SRV-100.
/// </summary>
public static class YieldDistributionService
{
    /// <summary>
    /// Computes yield distribution for a given resource+purity across all surveys.
    /// Bins yields into a histogram and returns percentage per bin.
    /// </summary>
    public static YieldDistributionResult ComputeDistribution(
        IReadOnlyList<Survey> surveys, string resourceName, string purity, int binWidth = 10)
    {
        binWidth = Math.Clamp(binWidth, 1, 100);

        // Extract matching yields
        var yields = new List<decimal>();
        foreach (var survey in surveys)
        {
            if (survey.Resources is null)
            {
                continue;
            }

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

        if (yields.Count < 2)
        {
            return new YieldDistributionResult(new List<DistributionPoint>(), true);
        }

        // Bin the yields
        decimal minYield = yields.Min();
        decimal maxYield = yields.Max();
        decimal binStart = Math.Floor(minYield / binWidth) * binWidth;

        var bins = new Dictionary<decimal, int>();
        for (decimal b = binStart; b <= maxYield; b += binWidth)
        {
            bins[b] = 0;
        }

        foreach (var y in yields)
        {
            decimal bin = Math.Floor(y / binWidth) * binWidth;
            if (!bins.ContainsKey(bin))
            {
                bins[bin] = 0;
            }

            bins[bin]++;
        }

        // Convert to percentage points
        int total = yields.Count;
        var points = bins.OrderBy(b => b.Key)
            .Select(b => new DistributionPoint(
                (double)(b.Key + (binWidth / 2m)),
                ((double)b.Value / total) * 100.0))
            .ToList();

        return new YieldDistributionResult(points, false);
    }

    /// <summary>
    /// Gets available resource+purity combinations from all surveys.
    /// Returns distinct pairs sorted by ResourceName then Purity.
    /// </summary>
    public static List<ResourcePurityCombo> GetAvailableCombos(IReadOnlyList<Survey> surveys)
    {
        var combos = new HashSet<(string Resource, string Purity)>();
        foreach (var survey in surveys)
        {
            if (survey.Resources is null)
            {
                continue;
            }

            foreach (var kvp in survey.Resources)
            {
                var res = kvp.Value;
                if (!string.IsNullOrEmpty(res.Resource) && !string.IsNullOrEmpty(res.Purity))
                {
                    combos.Add((res.Resource, res.Purity));
                }
            }
        }

        return combos
            .OrderBy(c => c.Resource, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Purity, StringComparer.OrdinalIgnoreCase)
            .Select(c => new ResourcePurityCombo(c.Resource, c.Purity))
            .ToList();
    }

    /// <summary>A single point on the distribution curve.</summary>
    public sealed class DistributionPoint
    {
        /// <summary>Initializes a new instance of the <see cref="DistributionPoint"/> class.</summary>
        public DistributionPoint(double binMidpoint, double percentage)
        {
            BinMidpoint = binMidpoint;
            Percentage = percentage;
        }

        /// <summary>Gets the midpoint of the bin.</summary>
        public double BinMidpoint { get; }

        /// <summary>Gets the percentage of yields in this bin.</summary>
        public double Percentage { get; }
    }

    /// <summary>Result of computing a yield distribution.</summary>
    public sealed class YieldDistributionResult
    {
        /// <summary>Initializes a new instance of the <see cref="YieldDistributionResult"/> class.</summary>
        public YieldDistributionResult(List<DistributionPoint> points, bool insufficientData)
        {
            Points = points;
            InsufficientData = insufficientData;
        }

        /// <summary>Gets the distribution points.</summary>
        public List<DistributionPoint> Points { get; }

        /// <summary>Gets a value indicating whether there was insufficient data.</summary>
        public bool InsufficientData { get; }
    }

    /// <summary>Identifies a unique resource+purity combination.</summary>
    public sealed class ResourcePurityCombo
    {
        /// <summary>Initializes a new instance of the <see cref="ResourcePurityCombo"/> class.</summary>
        public ResourcePurityCombo(string resourceName, string purity)
        {
            ResourceName = resourceName;
            Purity = purity;
        }

        /// <summary>Gets the resource name.</summary>
        public string ResourceName { get; }

        /// <summary>Gets the purity level.</summary>
        public string Purity { get; }

        /// <inheritdoc/>
        public override string ToString() => $"{ResourceName} ({Purity})";
    }
}
