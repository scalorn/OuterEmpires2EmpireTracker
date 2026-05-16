using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NLog;
using OE2EmpireTracker.Interfaces;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    public class BackgroundProcessor : IDisposable
    {
        public const int TickIntervalMs = 60_000;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;

        private readonly IColonyProcessingContext _colonyProcessingContext;

        private readonly Func<int> _getTickIntervalMs;

        private readonly ManualResetEventSlim _stopping = new ManualResetEventSlim(false);

        private readonly object _cycleLock = new object();

        private Timer _timer;

        private bool _disposed;

        private bool _running;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundProcessor"/> class.
        /// </summary>
        /// <param name="playerContext">The player context to process.</param>
        /// <param name="getTickIntervalMs">
        /// A function that returns the desired tick interval in milliseconds.
        /// Called each tick to allow dynamic interval changes. Minimum enforced: 1000ms.
        /// If null, defaults to <see cref="TickIntervalMs"/>.
        /// </param>
        public BackgroundProcessor(PlayerContext playerContext, Func<int> getTickIntervalMs = null)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
            _getTickIntervalMs = getTickIntervalMs ?? (() => TickIntervalMs);
            _colonyProcessingContext = new ColonyProcessingContextAdapter(
                playerContext, EmpireContext.GetInstance());
        }

        /// <summary>
        /// When the next processing cycle is scheduled to run.
        /// </summary>
        public DateTime NextProcessTime { get; private set; }

        /// <summary>
        /// Whether the most recent processing cycle encountered an error.
        /// </summary>
        public bool LastCycleHadError { get; private set; }

        /// <summary>
        /// Attempts to advance a build item's status to the target status.
        /// Status can only advance (never decrease) based on ordinal value:
        /// Staged(0) -> Delivering(1) -> Ready(2) -> InProgress(3) -> Completed(4).
        /// Returns true if the status was changed.
        /// </summary>
        public static bool TryAdvanceStatus(BuildItem item, BuildItemStatus targetStatus)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if ((int)targetStatus > (int)item.Status)
            {
                item.Status = targetStatus;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Starts the background processing timer.
        /// Sets cascade dirty flags so the first cycle recomputes build plan statuses.
        /// </summary>
        public void Start()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(BackgroundProcessor));
            if (_running) return;

            // Startup cascade: mark both flags so the first tick recomputes statuses
            _playerContext.CascadeResourceCheckDirty = true;
            _playerContext.CascadeStockTargetsDirty = true;
            Log.Info("BackgroundProcessor: startup cascade flags set");

            _stopping.Reset();
            int interval = GetTickIntervalMs();
            NextProcessTime = SystemClock.UtcNow.AddMilliseconds(interval);
            _timer = new Timer(OnTimerTick, null, interval, Timeout.Infinite);
            _running = true;

            Log.Info("BackgroundProcessor started. Tick interval: {0}ms", interval);
        }

        /// <summary>
        /// Stops the background processor and blocks until any in-progress cycle completes.
        /// </summary>
        public void Stop()
        {
            if (!_running) return;

            _stopping.Set();

            if (_timer != null)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }

            // Wait for any in-progress cycle to finish
            lock (_cycleLock)
            {
                // Cycle is done once we acquire the lock
            }

            _running = false;
            Log.Info("BackgroundProcessor stopped.");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Stop();

            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }

            _stopping.Dispose();
        }

        /// <summary>
        /// Executes a single processing cycle. Exposed for testability.
        /// </summary>
        public void RunCycleOnce()
        {
            ExecuteCycle();
        }

        /// <summary>
        /// Reads the background processing interval from the injected function,
        /// enforcing a minimum of 1000ms.
        /// </summary>
        private int GetTickIntervalMs()
        {
            return Math.Max(_getTickIntervalMs(), 1000);
        }

        private void OnTimerTick(object state)
        {
            if (_stopping.IsSet) return;

            ExecuteCycle();

            if (!_stopping.IsSet && !_disposed)
            {
                int interval = GetTickIntervalMs();
                NextProcessTime = SystemClock.UtcNow.AddMilliseconds(interval);
                try
                {
                    _timer?.Change(interval, Timeout.Infinite);
                }
                catch (ObjectDisposedException)
                {
                    // Timer was disposed during shutdown
                }
            }
        }

        private void ExecuteCycle()
        {
            List<string> modifiedPlanUUIDs = null;

            if (!Monitor.TryEnter(_cycleLock))
            {
                // Another cycle is already running; skip this tick
                return;
            }

            try
            {
                bool hadError = false;
                int processedCount = 0;

                // Snapshot the colony list to avoid modification during iteration
                var colonies = _playerContext.SnapshotColonyList();

                foreach (var colony in colonies)
                {
                    if (_stopping.IsSet) break;

                    if (!colony.HasExpiredTimers()) continue;

                    try
                    {
                        if (!colony.ColonyLock.TryEnterWriteLock(Colony.WriteLockTimeoutMs))
                        {
                            Log.Warn("BackgroundProcessor: write lock timeout on colony {0}, skipping", colony.UUID);
                            continue;
                        }

                        try
                        {
                            colony.ProcessColony(_colonyProcessingContext);
                        }
                        finally
                        {
                            colony.ColonyLock.ExitWriteLock();
                        }

                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error processing colony {0} ({1})", colony.ColonyName, colony.UUID);
                        hadError = true;
                    }

                    // Fire event outside the per-colony try/catch so a UI handler
                    // exception does not mark the colony as failed or skip the count.
                    try
                    {
                        _playerContext.OnColonyDataChanged(colony.UUID);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error firing ColonyDataChanged for colony {0} ({1})", colony.ColonyName, colony.UUID);
                    }
                }

                // Process cascade dirty flags (build plan resource checks / stock targets)
                if (!_stopping.IsSet)
                {
                    try
                    {
                        modifiedPlanUUIDs = ProcessCascades();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error during cascade processing");
                        hadError = true;
                    }
                }

                // Check warehouse overflow rules (task 38.2)
                int overflowDeliveries = 0;
                if (!_stopping.IsSet)
                {
                    try
                    {
                        overflowDeliveries = CheckWarehouseOverflow(colonies);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error during warehouse overflow check");
                        hadError = true;
                    }
                }

                // Check supply chain thresholds (task 39.1)
                int supplyChainRequests = 0;
                if (!_stopping.IsSet)
                {
                    try
                    {
                        supplyChainRequests = CheckSupplyChainThresholds();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error during supply chain threshold check");
                        hadError = true;
                    }
                }

                bool cascadeModified = modifiedPlanUUIDs != null && modifiedPlanUUIDs.Count > 0;

                if (processedCount > 0 || cascadeModified || overflowDeliveries > 0 || supplyChainRequests > 0)
                {
                    try
                    {
                        _playerContext.WriteContext();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error persisting context after processing {0} colonies", processedCount);
                        hadError = true;
                    }

                    if (processedCount > 0)
                    {
                        Log.Info("BackgroundProcessor cycle complete. Processed {0} colonies.", processedCount);
                    }
                }

                LastCycleHadError = hadError;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unhandled exception in BackgroundProcessor cycle");
                LastCycleHadError = true;
            }
            finally
            {
                Monitor.Exit(_cycleLock);
            }

            // Fire BuildPlanDataChanged OUTSIDE the _cycleLock to prevent deadlocks
            // with UI event handlers that might try to acquire locks.
            if (modifiedPlanUUIDs != null)
            {
                foreach (var planUUID in modifiedPlanUUIDs)
                {
                    try
                    {
                        _playerContext.OnBuildPlanDataChanged(planUUID);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error firing BuildPlanDataChanged for plan {0}", planUUID);
                    }
                }
            }
        }

        /// <summary>
        /// Processes cascade dirty flags. Checks CascadeResourceCheckDirty and
        /// CascadeStockTargetsDirty on PlayerContext, performs the appropriate
        /// recomputation, and clears the flags.
        /// Returns a list of build plan UUIDs that were modified (for event firing).
        /// </summary>
        private List<string> ProcessCascades()
        {
            var modifiedPlanUUIDs = new List<string>();

            bool resourceCheckDirty = _playerContext.CascadeResourceCheckDirty;
            bool stockTargetsDirty = _playerContext.CascadeStockTargetsDirty;

            if (!resourceCheckDirty && !stockTargetsDirty)
                return modifiedPlanUUIDs;

            var sw = Stopwatch.StartNew();

            if (resourceCheckDirty)
            {
                _playerContext.CascadeResourceCheckDirty = false;
                Log.Info("BackgroundProcessor: processing CascadeResourceCheckDirty");

                var plans = _playerContext.SnapshotBuildPlanList();
                var activePlans = plans.Where(p => p.IsActive).ToList();

                foreach (var plan in activePlans)
                {
                    if (_stopping.IsSet) break;

                    try
                    {
                        var shortfallMap = ResourceCheckService.ComputePlanShortfalls(
                            plan,
                            uuid => _playerContext.FindColony(uuid),
                            uuid => _playerContext.FindShip(uuid),
                            uuid => _playerContext.FindStation(uuid),
                            _playerContext.CurrentPlayerUUID,
                            uuid => _playerContext.FindBlueprint(uuid));

                        bool planModified = false;

                        foreach (var item in plan.Items)
                        {
                            // Only advance items that are in Delivering status
                            if (item.Status != BuildItemStatus.Delivering)
                                continue;

                            // If no shortfalls for this item, it can advance to Ready
                            bool hasShortfalls = shortfallMap.ContainsKey(item.UUID);
                            if (!hasShortfalls)
                            {
                                if (TryAdvanceStatus(item, BuildItemStatus.Ready))
                                {
                                    planModified = true;
                                    Log.Debug(
                                        "BackgroundProcessor: item {0} in plan '{1}' advanced to Ready (shortfalls resolved)",
                                        item.UUID,
                                        plan.Name);
                                }
                            }
                        }

                        if (planModified)
                        {
                            modifiedPlanUUIDs.Add(plan.UUID);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            ex,
                            "Error processing cascade resource check for plan '{0}' ({1})",
                            plan.Name,
                            plan.UUID);
                    }
                }
            }

            // Build plan execution cascade
            if (!_stopping.IsSet)
            {
                try
                {
                    var plans = _playerContext.SnapshotBuildPlanList();
                    var activePlans = plans.Where(p => p.IsActive).ToList();
                    bool anyExecutionChanged = false;

                    foreach (var plan in activePlans)
                    {
                        if (_stopping.IsSet) break;

                        bool changed = BuildPlanExecutionService.AdvanceBuildItemStatuses(
                            plan,
                            uuid => _playerContext.FindColony(uuid),
                            uuid => _playerContext.FindBlueprint(uuid),
                            uuid => _playerContext.FindShip(uuid),
                            uuid => _playerContext.FindStation(uuid),
                            _playerContext.CurrentPlayerUUID);

                        if (changed)
                        {
                            if (!modifiedPlanUUIDs.Contains(plan.UUID))
                                modifiedPlanUUIDs.Add(plan.UUID);
                            anyExecutionChanged = true;
                        }
                    }

                    if (anyExecutionChanged)
                    {
                        _playerContext.CascadeResourceCheckDirty = true;
                        _playerContext.CascadeStockTargetsDirty = true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error during build plan execution cascade");
                }
            }

            if (stockTargetsDirty)
            {
                _playerContext.CascadeStockTargetsDirty = false;
                Log.Info("BackgroundProcessor: processing CascadeStockTargetsDirty");

                try
                {
                    var stockPlans = _playerContext.StockPlanList;
                    if (stockPlans != null && stockPlans.Count > 0)
                    {
                        string playerUUID = _playerContext.CurrentPlayerUUID ?? string.Empty;
                        var colonies = _playerContext.SnapshotColonyList();
                        var stations = _playerContext.StationList.ToList();

                        var shortfalls = StockTargetService.CheckTargets(
                            stockPlans,
                            playerUUID,
                            uuid => _playerContext.FindColony(uuid),
                            uuid => _playerContext.StationList.FirstOrDefault(s => s.UUID == uuid),
                            uuid => _playerContext.ShipTemplateList.FirstOrDefault(t => t.UUID == uuid),
                            uuid => _playerContext.FindBlueprint(uuid),
                            colonies,
                            stations);

                        if (shortfalls.Count > 0)
                        {
                            // Generate replenishment items for plans that have a replenishment build plan
                            var plansByUUID = stockPlans.Where(p => !string.IsNullOrEmpty(p.ReplenishmentBuildPlanUUID))
                                .ToDictionary(p => p.UUID, p => p);

                            foreach (var shortfall in shortfalls)
                            {
                                StockPlan stockPlan;
                                if (!plansByUUID.TryGetValue(shortfall.PlanUUID, out stockPlan)) continue;

                                var buildPlan = _playerContext.BuildPlanList
                                    .FirstOrDefault(bp => bp.UUID == stockPlan.ReplenishmentBuildPlanUUID);
                                if (buildPlan == null) continue;

                                var newItems = StockTargetService.GenerateReplenishmentItems(
                                    new List<StockShortfall> { shortfall },
                                    new[] { buildPlan });

                                if (newItems.Count > 0)
                                {
                                    buildPlan.Items.AddRange(newItems);
                                    if (!modifiedPlanUUIDs.Contains(buildPlan.UUID))
                                        modifiedPlanUUIDs.Add(buildPlan.UUID);
                                    _playerContext.CascadeResourceCheckDirty = true;
                                    Log.Info(
                                        "Stock target replenishment: added {0} items to plan '{1}'",
                                        newItems.Count,
                                        buildPlan.Name);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error during stock target cascade processing");
                }
            }

            sw.Stop();
            Log.Info(
                "PERF BackgroundProcessor.ProcessCascades: {0} plans modified in {1}ms",
                modifiedPlanUUIDs.Count,
                sw.ElapsedMilliseconds);

            return modifiedPlanUUIDs;
        }

        /// <summary>
        /// Checks warehouse overflow rules for all colonies. For each active rule
        /// where the colony's resource quantity exceeds the threshold, logs the
        /// overflow. Returns the count of overflow detections.
        /// Delivery generation will be wired in task 39.
        /// </summary>
        private int CheckWarehouseOverflow(List<Colony> colonies)
        {
            var rules = _playerContext.WarehouseOverflowRuleList;
            if (rules == null || rules.Count == 0) return 0;

            int overflowCount = 0;
            var activeRules = rules.Where(r => r.IsActive).ToList();

            foreach (var rule in activeRules)
            {
                var colony = colonies.FirstOrDefault(c => c.UUID == rule.ColonyUUID);
                if (colony == null) continue;
                if (colony.Items == null) continue;

                var items = colony.Items.FindResource(rule.ResourceName, rule.ResourcePurity);
                int currentQty = items.Sum(i => i.Quantity);

                if (currentQty > rule.TriggerThreshold && rule.TriggerThreshold > 0)
                {
                    int excess = currentQty - rule.TriggerThreshold;
                    Log.Info(
                        "Overflow detected: colony={0} resource={1}({2}) current={3} threshold={4} excess={5}",
                        colony.ColonyName,
                        rule.ResourceName,
                        rule.ResourcePurity,
                        currentQty,
                        rule.TriggerThreshold,
                        excess);
                    overflowCount++;
                }
            }

            return overflowCount;
        }

        /// <summary>
        /// Checks supply chain thresholds and logs delivery requests.
        /// Returns the count of threshold breaches detected.
        /// </summary>
        private int CheckSupplyChainThresholds()
        {
            var chains = _playerContext.SupplyChainList;
            if (chains == null || chains.Count == 0) return 0;

            string currentPlayerUUID = _playerContext.CurrentPlayerUUID ?? string.Empty;

            var requests = SupplyChainService.CheckThresholds(
                chains,
                uuid => _playerContext.ColonyList.FirstOrDefault(c => c.UUID == uuid),
                uuid => _playerContext.StationList.FirstOrDefault(s => s.UUID == uuid),
                uuid => _playerContext.ShipList.FirstOrDefault(s => s.UUID == uuid),
                currentPlayerUUID);

            if (requests.Count > 0)
            {
                Log.Info("Supply chain threshold check: {0} delivery request(s) generated", requests.Count);
            }

            return requests.Count;
        }
    }
}
