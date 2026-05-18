using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Background processor that advances colony timers on a configurable interval.
/// Fires on the UI thread via DispatcherTimer.
/// Implements REQ-BP-001 through REQ-BP-044.
/// </summary>
public sealed class BackgroundProcessor : IDisposable
{
    private readonly DataService _dataService;
    private readonly ColonyProcessingContext _processingContext;
    private readonly ILogger<BackgroundProcessor> _logger;
    private readonly object _cycleLock = new object();

    private DispatcherTimer? _timer;
    private bool _disposed;

    public BackgroundProcessor(
        DataService dataService,
        ColonyProcessingContext processingContext,
        ILogger<BackgroundProcessor> logger)
    {
        _dataService = dataService;
        _processingContext = processingContext;
        _logger = logger;
    }

    /// <summary>Gets whether the processor is currently running.</summary>
    public bool IsRunning => _timer?.IsEnabled ?? false;

    /// <summary>Gets whether the last processing cycle had an error.</summary>
    public bool LastCycleHadError { get; private set; }

    /// <summary>Gets the time of the next scheduled processing tick.</summary>
    public DateTime NextProcessTime { get; private set; }

    /// <summary>
    /// Starts the background processing timer.
    /// Calling Start on an already-running processor is a no-op (REQ-BP-020).
    /// </summary>
    public void Start(int intervalSeconds = 60)
    {
        if (IsRunning)
        {
            return;
        }

        int safeInterval = Math.Max(1, intervalSeconds);
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(safeInterval),
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
        NextProcessTime = OE2EmpireTracker.Services.SystemClock.UtcNow.AddSeconds(safeInterval);
        _logger.LogInformation("BackgroundProcessor started with {Interval}s interval", safeInterval);
    }

    /// <summary>
    /// Stops the background processing timer (REQ-BP-021).
    /// </summary>
    public void Stop()
    {
        if (_timer is not null)
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }

        _logger.LogInformation("BackgroundProcessor stopped");
    }

    /// <summary>Disposes the processor and stops the timer (REQ-BP-022).</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        // Update next process time immediately
        if (_timer is not null)
        {
            NextProcessTime = OE2EmpireTracker.Services.SystemClock.UtcNow.Add(_timer.Interval);
        }

        // Prevent concurrent processing (REQ-BP-005)
        if (!System.Threading.Monitor.TryEnter(_cycleLock))
        {
            return;
        }

        try
        {
            ProcessCycle();
            LastCycleHadError = false;
        }
        catch (Exception ex)
        {
            // REQ-BP-010: Log error, set flag, continue
            _logger.LogError(ex, "Background processing cycle failed");
            LastCycleHadError = true;
        }
        finally
        {
            System.Threading.Monitor.Exit(_cycleLock);
        }
    }

    private void ProcessCycle()
    {
        if (!_dataService.IsLoaded)
        {
            return;
        }

        // REQ-BP-043: Snapshot colony list to iterate safely
        var colonies = _dataService.GetCurrentPlayerColonies();
        if (colonies.Count == 0)
        {
            return;
        }

        // REQ-BP-002: Find colonies with expired timers
        var expiredColonies = colonies.Where(c => c.HasExpiredTimers()).ToList();
        if (expiredColonies.Count == 0)
        {
            return;
        }

        var processedUuids = new List<string>();

        foreach (var colony in expiredColonies)
        {
            try
            {
                // REQ-BP-040: Acquire write lock
                if (!colony.ColonyLock.TryEnterWriteLock(Colony.WriteLockTimeoutMs))
                {
                    // REQ-BP-041: Log warning, skip colony
                    _logger.LogWarning("Colony lock timeout for {Colony}, skipping", colony.ColonyName);
                    continue;
                }

                try
                {
                    // REQ-BP-002: Process the colony
                    colony.ProcessColony(_processingContext);
                    processedUuids.Add(colony.UUID);
                }
                finally
                {
                    // REQ-BP-042: Release lock before firing events
                    colony.ColonyLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing colony {Colony}", colony.ColonyName);
            }
        }

        // REQ-BP-003: Fire ColonyDataChanged for each processed colony
        foreach (var uuid in processedUuids)
        {
            _dataService.OnColonyDataChanged(uuid);
        }

        // REQ-BP-004: Persist changes (outside any colony lock per REQ-BP-044)
        if (processedUuids.Count > 0)
        {
            _dataService.IsDirty = true;
            _dataService.WriteContext();
            _logger.LogDebug("Processed {Count} colonies", processedUuids.Count);
        }

        // After processing colonies, check overflow rules (REQ-BP-050)
        CheckWarehouseOverflow();

        // Check supply chain thresholds (REQ-BP-060)
        CheckSupplyChainThresholds();

        // Check stock target cascade (REQ-BP-070)
        if (_dataService.CascadeStockTargetsDirty)
        {
            EvaluateStockTargets();
            _dataService.CascadeStockTargetsDirty = false;
        }
    }

    /// <summary>
    /// Evaluates all active warehouse overflow rules and logs when thresholds are exceeded.
    /// Implements REQ-BP-050 through REQ-BP-052.
    /// </summary>
    private void CheckWarehouseOverflow()
    {
        var rules = _dataService.WarehouseOverflowRules;
        foreach (var rule in rules)
        {
            if (!rule.IsActive)
            {
                continue;
            }

            var colony = _dataService.Colonies.FirstOrDefault(c => c.UUID == rule.ColonyUUID);
            if (colony?.Items is null)
            {
                continue;
            }

            var items = colony.Items.FindResource(rule.ResourceName, rule.ResourcePurity);
            int totalQty = items.Sum(i => i.Quantity);
            if (totalQty > rule.TriggerThreshold)
            {
                _logger.LogInformation(
                    "Overflow triggered: {Resource} ({Purity}) at {Colony}: {Qty} > {Threshold}",
                    rule.ResourceName,
                    rule.ResourcePurity,
                    colony.ColonyName,
                    totalQty,
                    rule.TriggerThreshold);

                // TODO: Generate delivery plan for excess (totalQty - threshold)
            }
        }
    }

    /// <summary>
    /// Evaluates all active supply chains and logs when stage accumulation exceeds thresholds.
    /// Implements REQ-BP-060 through REQ-BP-062.
    /// </summary>
    private void CheckSupplyChainThresholds()
    {
        var chains = _dataService.SupplyChains;
        foreach (var chain in chains)
        {
            if (!chain.IsActive)
            {
                continue;
            }

            if (chain.Stages is null)
            {
                continue;
            }

            foreach (var stage in chain.Stages)
            {
                if (stage.AccumulationThreshold <= 0)
                {
                    continue;
                }

                var colony = _dataService.Colonies.FirstOrDefault(c => c.UUID == stage.LocationUUID);
                if (colony?.Items is null)
                {
                    continue;
                }

                var items = colony.Items.FindResource(stage.ResourceName, stage.ResourcePurity);
                int totalQty = items.Sum(i => i.Quantity);
                if (totalQty > stage.AccumulationThreshold)
                {
                    _logger.LogInformation(
                        "Supply chain '{Chain}' stage {Seq} threshold exceeded: {Resource} ({Purity}) {Qty} > {Threshold}",
                        chain.Name,
                        stage.Sequence,
                        stage.ResourceName,
                        stage.ResourcePurity,
                        totalQty,
                        stage.AccumulationThreshold);

                    // TODO: Generate delivery plan on stage's designated route
                }
            }
        }
    }

    /// <summary>
    /// Evaluates stock targets when the dirty flag is set.
    /// Implements REQ-BP-070 through REQ-BP-072.
    /// </summary>
    private void EvaluateStockTargets()
    {
        _logger.LogInformation("Stock target evaluation triggered");

        // TODO: Expand template targets, check scoped inventory, compute shortfalls,
        // and create replenishment build items in linked build plans.
    }
}
