using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Constants;
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

        private QueueSyncService _queueSyncService;

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

            // Use a short initial delay (10 seconds) to allow GameApiContext and other
            // components to initialize before the first cycle fires.
            // Subsequent ticks use the configured interval.
            _timer = new Timer(OnTimerTick, null, 10000, Timeout.Infinite);
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

                // Dispatch queue-based Game API sync on a background thread (Req 9.1, 9.2, 9.3)
                TryDispatchQueueSync();

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
        /// Dispatches a queue-based Game API sync cycle on a background thread if the
        /// Game API is enabled and no sync is already running. Constructs the
        /// QueueSyncService lazily on first successful dispatch.
        /// </summary>
        private void TryDispatchQueueSync()
        {
            var settings = PreferencesStore.GetInstance().Preferences.GameApiConnection;
            if (settings == null || !settings.Enabled)
            {
                return;
            }

            var gameApiContext = GameApiContext.Instance;
            if (gameApiContext == null)
            {
                return;
            }

            if (_queueSyncService == null)
            {
                var empireContext = EmpireContext.GetInstance();
                var gridIndex = new SystemGridIndex(empireContext.SystemRepository);
                var marketDataService = new MarketDataService(_playerContext, gridIndex);

                _queueSyncService = new QueueSyncService(
                    _playerContext,
                    empireContext,
                    gameApiContext.TypedClient,
                    settings,
                    gameApiContext.CredentialManager,
                    marketDataService,
                    gridIndex);
                Log.Info("BackgroundProcessor: QueueSyncService constructed");
            }

            if (!_queueSyncService.IsSyncRunning)
            {
                _ = Task.Run(() => _queueSyncService.RunSyncAsync(CancellationToken.None));
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
        /// Checks warehouse overflow rules for all colonies. Dispatches each active
        /// rule to the appropriate evaluator based on RuleType. Returns the count of
        /// overflow detections.
        /// Delivery generation will be wired in task 39.
        /// </summary>
        private int CheckWarehouseOverflow(List<Colony> colonies)
        {
            var rules = _playerContext.WarehouseOverflowRuleList;
            if (rules == null || rules.Count == 0)
            {
                return 0;
            }

            int overflowCount = 0;
            var activeRules = rules.Where(r => r.IsActive).ToList();

            foreach (var rule in activeRules)
            {
                var colony = colonies.FirstOrDefault(c => c.UUID == rule.ColonyUUID);
                if (colony == null || colony.Items == null)
                {
                    continue;
                }

                switch (rule.RuleType)
                {
                    case OverflowRuleType.SpecificResource:
                        if (EvaluateSpecificResourceRule(rule, colony))
                        {
                            overflowCount++;
                        }

                        break;
                    case OverflowRuleType.TotalWarehouse:
                        if (EvaluateTotalWarehouseRule(rule, colony))
                        {
                            overflowCount++;
                        }

                        break;
                    default:
                        Log.Warn("Unknown RuleType {0} for rule {1}, skipping", rule.RuleType, rule.UUID);
                        break;
                }
            }

            return overflowCount;
        }

        /// <summary>
        /// Evaluates a SpecificResource overflow rule. Filters colony items by
        /// ResourceName and ResourcePurity, computes volume using per-unit volume
        /// constants, and checks against the threshold. Returns true if overflow
        /// is detected.
        /// </summary>
        private bool EvaluateSpecificResourceRule(WarehouseOverflowRule rule, Colony colony)
        {
            var items = colony.Items.FindResource(rule.ResourceName, rule.ResourcePurity);
            decimal currentVolume = items.Sum(i => i.Quantity * GameConstants.VolumeResource);

            if (currentVolume > rule.TriggerThreshold && rule.TriggerThreshold > 0m)
            {
                decimal excess = currentVolume - rule.TriggerThreshold;
                Log.Info(
                    "Overflow detected: colony={0} resource={1}({2}) volume={3} threshold={4} excess={5}",
                    colony.ColonyName,
                    rule.ResourceName,
                    rule.ResourcePurity,
                    currentVolume,
                    rule.TriggerThreshold,
                    excess);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Evaluates a TotalWarehouse overflow rule. Computes the total volume of
        /// all items in the colony warehouse and checks against the threshold.
        /// Returns true if overflow is detected.
        /// </summary>
        private bool EvaluateTotalWarehouseRule(WarehouseOverflowRule rule, Colony colony)
        {
            decimal totalVolume = ComputeWarehouseVolume(colony.Items);

            if (totalVolume > rule.TriggerThreshold && rule.TriggerThreshold > 0m)
            {
                decimal excess = totalVolume - rule.TriggerThreshold;
                Log.Info(
                    "Total warehouse overflow: colony={0} volume={1} threshold={2} excess={3}",
                    colony.ColonyName,
                    totalVolume,
                    rule.TriggerThreshold,
                    excess);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Computes the total volume of all items in an <see cref="ItemBag"/>
        /// by summing (quantity × per-unit volume) for every item.
        /// </summary>
        private decimal ComputeWarehouseVolume(ItemBag items)
        {
            decimal total = 0m;
            foreach (var kvp in items.Items)
            {
                var item = kvp.Value;
                decimal unitVolume = GetItemUnitVolume(item);
                total += item.Quantity * unitVolume;
            }

            return total;
        }

        /// <summary>
        /// Returns the per-unit volume for an item based on its <see cref="ItemType.ItemTypeEnum"/>.
        /// Resources = 1, Commodities = 10, Workers = 50, Blueprints/Surveys = 0,
        /// Manufactured/other items use the item's Volume property.
        /// </summary>
        private decimal GetItemUnitVolume(Item item)
        {
            switch (item.ItemType)
            {
                case ItemType.ItemTypeEnum.Resource:
                    return GameConstants.VolumeResource;
                case ItemType.ItemTypeEnum.Commodity:
                    return GameConstants.VolumeCommodity;
                case ItemType.ItemTypeEnum.WorkDetail:
                    return GameConstants.VolumeWorkDetail;
                case ItemType.ItemTypeEnum.Blueprint:
                    return GameConstants.VolumeBlueprint;
                case ItemType.ItemTypeEnum.Survey:
                    return GameConstants.VolumeSurvey;
                default:
                    return item.Volume;
            }
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
