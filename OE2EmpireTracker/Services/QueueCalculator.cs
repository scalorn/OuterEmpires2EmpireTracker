using System;
using System.Diagnostics;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Stateless service that computes how many manufacturing or commodity runs
    /// are needed to keep a structure busy for a target duration.
    /// </summary>
    public static class QueueCalculator
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Computes how many manufacturing runs to keep a structure busy
        /// for at least the target duration.
        /// Returns -1 if the blueprint has no "Manufacture Run Time" or
        /// the time cannot be parsed.
        /// </summary>
        /// <param name="blueprint">The blueprint to compute runs for.</param>
        /// <param name="targetDurationSeconds">Target duration in seconds.</param>
        /// <returns>
        /// Number of runs (ceiling division), or -1 if manufacturing time is unknown.
        /// </returns>
        public static int ComputeManufactoryRuns(
            Blueprint blueprint, int targetDurationSeconds)
        {
            var sw = Stopwatch.StartNew();

            if (blueprint == null)
            {
                Log.Warn("ComputeManufactoryRuns: blueprint is null");
                sw.Stop();
                Log.Debug("PERF ComputeManufactoryRuns: {0}ms", sw.ElapsedMilliseconds);
                return -1;
            }

            if (targetDurationSeconds <= 0)
            {
                Log.Warn("ComputeManufactoryRuns: targetDurationSeconds={0} is not positive",
                    targetDurationSeconds);
                sw.Stop();
                Log.Debug("PERF ComputeManufactoryRuns: {0}ms", sw.ElapsedMilliseconds);
                return 0;
            }

            string mfgTimeStr;
            if (blueprint.Properties == null ||
                !blueprint.Properties.GetString(BlueprintPropertyKeys.ManufactureRunTime, string.Empty, out mfgTimeStr) ||
                string.IsNullOrWhiteSpace(mfgTimeStr))
            {
                Log.Info("ComputeManufactoryRuns: blueprint '{0}' has no Manufacture Run Time",
                    blueprint.Name);
                sw.Stop();
                Log.Debug("PERF ComputeManufactoryRuns: {0}ms", sw.ElapsedMilliseconds);
                return -1;
            }

            decimal mfgSeconds = EvolutionChainService.ParseTimeToSeconds(mfgTimeStr);
            if (mfgSeconds <= 0m)
            {
                Log.Warn("ComputeManufactoryRuns: parsed manufacturing time is {0}s for '{1}'",
                    mfgSeconds, blueprint.Name);
                sw.Stop();
                Log.Debug("PERF ComputeManufactoryRuns: {0}ms", sw.ElapsedMilliseconds);
                return -1;
            }

            int runs = (int)Math.Ceiling((decimal)targetDurationSeconds / mfgSeconds);

            sw.Stop();
            Log.Info("PERF ComputeManufactoryRuns: {0} runs for '{1}' ({2}s target, {3}s per run) in {4}ms",
                runs, blueprint.Name, targetDurationSeconds, mfgSeconds, sw.ElapsedMilliseconds);
            return runs;
        }

        /// <summary>
        /// Computes how many commodity runs to keep a structure busy
        /// for at least the target duration.
        /// </summary>
        /// <param name="targetDurationSeconds">Target duration in seconds.</param>
        /// <returns>Number of commodity runs (ceiling division), or 0 if target is not positive.</returns>
        public static int ComputeCommodityRuns(int targetDurationSeconds)
        {
            var sw = Stopwatch.StartNew();

            if (targetDurationSeconds <= 0)
            {
                Log.Warn("ComputeCommodityRuns: targetDurationSeconds={0} is not positive",
                    targetDurationSeconds);
                sw.Stop();
                Log.Debug("PERF ComputeCommodityRuns: {0}ms", sw.ElapsedMilliseconds);
                return 0;
            }

            long cycleSeconds = GameConstants.CommodityCycleSeconds;
            if (cycleSeconds <= 0)
            {
                Log.Error("ComputeCommodityRuns: CommodityCycleSeconds is {0}", cycleSeconds);
                sw.Stop();
                Log.Debug("PERF ComputeCommodityRuns: {0}ms", sw.ElapsedMilliseconds);
                return 0;
            }

            int runs = (int)Math.Ceiling((decimal)targetDurationSeconds / cycleSeconds);

            sw.Stop();
            Log.Info("PERF ComputeCommodityRuns: {0} runs ({1}s target, {2}s per cycle) in {3}ms",
                runs, targetDurationSeconds, cycleSeconds, sw.ElapsedMilliseconds);
            return runs;
        }

        /// <summary>
        /// Returns total items produced for a given number of manufactory runs.
        /// Uses the blueprint's "Amount Manufactured" property (default 1).
        /// </summary>
        /// <param name="blueprint">The blueprint to read items-per-run from.</param>
        /// <param name="runs">Number of manufacturing runs.</param>
        /// <returns>Total items produced (runs x items per run).</returns>
        public static int ManufactoryRunsToItems(Blueprint blueprint, int runs)
        {
            if (runs <= 0)
                return 0;

            int itemsPerRun = 1;
            if (blueprint?.Properties != null)
            {
                decimal amountVal;
                if (blueprint.Properties.GetDecimal(BlueprintPropertyKeys.AmountManufactured, 0m, out amountVal)
                    && amountVal > 0m)
                {
                    itemsPerRun = (int)amountVal;
                }
            }

            int total = runs * itemsPerRun;
            Log.Debug("ManufactoryRunsToItems: {0} runs x {1} items/run = {2} total",
                runs, itemsPerRun, total);
            return total;
        }

        /// <summary>
        /// Returns total items produced for a given number of commodity runs.
        /// Each run produces <see cref="GameConstants.CommoditiesPerCycle"/> items.
        /// </summary>
        /// <param name="runs">Number of commodity runs.</param>
        /// <returns>Total items produced (runs x CommoditiesPerCycle).</returns>
        public static int CommodityRunsToItems(int runs)
        {
            if (runs <= 0)
                return 0;

            int perCycle = GameConstants.CommoditiesPerCycle;
            int total = runs * perCycle;
            Log.Debug("CommodityRunsToItems: {0} runs x {1} per cycle = {2} total",
                runs, perCycle, total);
            return total;
        }
    }
}
