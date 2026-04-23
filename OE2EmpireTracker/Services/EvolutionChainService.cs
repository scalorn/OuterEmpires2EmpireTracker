using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Describes a single line segment between two consecutive data points
    /// in an evolution graph series.
    /// </summary>
    public class SegmentInfo
    {
        public (int Evolution, decimal Percent) From { get; set; }
        public (int Evolution, decimal Percent) To { get; set; }

        /// <summary>
        /// True when the evolution levels differ by more than 1 (gap -> dashed line).
        /// False when consecutive (solid line).
        /// </summary>
        public bool IsGap { get; set; }
    }

    public class EvolutionGraphData
    {
        /// <summary>Property name -> list of (evolutionLevel, percentageValue) points.</summary>
        public Dictionary<string, List<(int Evolution, decimal Percent)>> Series { get; set; }
            = new Dictionary<string, List<(int Evolution, decimal Percent)>>();

        /// <summary>True when no numeric properties changed across the chain.</summary>
        public bool NoChanges { get; set; }
    }

    /// <summary>
    /// Pure-logic service for resolving blueprint evolution chains
    /// and preparing graph data. No UI dependencies.
    /// </summary>
    public static class EvolutionChainService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Walks baseBlueprintUUID links backward from the given blueprint
        /// to the Ev0 ancestor. Returns the chain sorted by Evolution ascending.
        /// Terminates on missing UUID or circular reference.
        /// </summary>
        public static List<Blueprint> ResolveChain(
            Blueprint start,
            Func<string, Blueprint> resolver)
        {
            if (start == null)
                return new List<Blueprint>();

            var chain = new List<Blueprint>();
            var visited = new HashSet<string>();

            var current = start;
            while (current != null)
            {
                if (current.UUID != null && !visited.Add(current.UUID))
                    break; // circular reference detected

                chain.Add(current);

                if (string.IsNullOrEmpty(current.BaseBlueprintUUID))
                    break; // reached Ev0 or no further link

                current = resolver(current.BaseBlueprintUUID);
            }

            chain.Sort((a, b) => a.Evolution.CompareTo(b.Evolution));
            return chain;
        }

        /// <summary>
        /// Given a chain and the BlueprintType's Properties array, filters to numeric
        /// properties that changed, normalizes to percentages, and returns graph data.
        /// </summary>
        public static EvolutionGraphData BuildGraphData(
            IReadOnlyList<Blueprint> chain,
            string[] blueprintTypeProperties)
        {
            var result = new EvolutionGraphData();

            if (chain == null || chain.Count == 0 ||
                blueprintTypeProperties == null || blueprintTypeProperties.Length == 0)
            {
                result.NoChanges = true;
                return result;
            }

            var ev0 = chain[0];

            foreach (var propName in blueprintTypeProperties)
            {
                var propType = BlueprintPropertyValidation.GetPropertyType(propName);
                if (propType != PropertyValueType.Integer &&
                    propType != PropertyValueType.Decimal &&
                    propType != PropertyValueType.Time)
                {
                    continue;
                }

                decimal ev0Value = GetNumericValue(ev0, propName, propType);
                if (ev0Value == 0.0m)
                    continue; // exclude zero Ev0 value (avoid division by zero)

                var points = new List<(int Evolution, decimal Percent)>();
                bool hasChange = false;

                foreach (var bp in chain)
                {
                    decimal val = GetNumericValue(bp, propName, propType);
                    decimal percent = (val / ev0Value) * 100.0m;
                    points.Add((bp.Evolution, percent));

                    if (val != ev0Value)
                        hasChange = true;
                }

                if (!hasChange)
                    continue; // exclude unchanged properties

                result.Series[propName] = points;
            }

            result.NoChanges = result.Series.Count == 0;
            return result;
        }

        /// <summary>
        /// Classifies segments between consecutive data points as solid (consecutive
        /// evolution levels, gap == 1) or dashed (gap > 1). Points are sorted by
        /// evolution level before classification.
        /// </summary>
        public static List<SegmentInfo> ClassifySegments(
            List<(int Evolution, decimal Percent)> points)
        {
            var segments = new List<SegmentInfo>();
            if (points == null || points.Count < 2)
                return segments;

            // Sort by evolution level ascending
            var sorted = new List<(int Evolution, decimal Percent)>(points);
            sorted.Sort((a, b) => a.Evolution.CompareTo(b.Evolution));

            for (int i = 1; i < sorted.Count; i++)
            {
                int evDiff = sorted[i].Evolution - sorted[i - 1].Evolution;
                segments.Add(new SegmentInfo
                {
                    From = sorted[i - 1],
                    To = sorted[i],
                    IsGap = evDiff > 1
                });
            }

            return segments;
        }

        private static decimal GetNumericValue(Blueprint bp, string propName, PropertyValueType propType)
        {
            if (bp.Properties == null)
                return 0.0m;

            if (propType == PropertyValueType.Time)
            {
                string strVal;
                bp.Properties.GetString(propName, string.Empty, out strVal);
                return ParseTimeToSeconds(strVal);
            }
            else
            {
                decimal val;
                bp.Properties.GetDecimal(propName, 0.0m, out val);
                return val;
            }
        }

        /// <summary>
        /// Parses time strings like "1d 2h 30m 15s" to total seconds.
        /// Handles any combination of d/h/m/s components.
        /// </summary>
        internal static decimal ParseTimeToSeconds(string timeStr)
        {
            if (string.IsNullOrWhiteSpace(timeStr))
                return 0.0m;

            decimal total = 0.0m;

            var match = Regex.Match(timeStr, @"(\d+)d");
            if (match.Success)
                total += decimal.Parse(match.Groups[1].Value) * 86400;

            match = Regex.Match(timeStr, @"(\d+)h");
            if (match.Success)
                total += decimal.Parse(match.Groups[1].Value) * 3600;

            match = Regex.Match(timeStr, @"(\d+)m");
            if (match.Success)
                total += decimal.Parse(match.Groups[1].Value) * 60;

            match = Regex.Match(timeStr, @"(\d+)s");
            if (match.Success)
                total += decimal.Parse(match.Groups[1].Value);

            return total;
        }
    }
}
