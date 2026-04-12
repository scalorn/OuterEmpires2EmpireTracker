using NLog;
using OE2EmpireTracker.Models;
using System;
using System.Collections.Generic;
using System.Threading;

namespace OE2EmpireTracker.Services
{
    public class BackgroundProcessor : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public const int TickIntervalMs = 60_000;

        private readonly PlayerContext _playerContext;

        /// <summary>
        /// Reads the background processing interval from user preferences,
        /// enforcing a minimum of 1000ms.
        /// </summary>
        private int GetTickIntervalMs()
        {
            var intervalMs = (int)(PreferencesStore.GetInstance().Preferences.Thresholds.BackgroundProcessingIntervalSeconds * 1000);
            return Math.Max(intervalMs, 1000);
        }
        private Timer _timer;
        private readonly ManualResetEventSlim _stopping = new ManualResetEventSlim(false);
        private readonly object _cycleLock = new object();
        private bool _disposed;
        private bool _running;

        /// <summary>
        /// When the next processing cycle is scheduled to run.
        /// </summary>
        public DateTime NextProcessTime { get; private set; }

        /// <summary>
        /// Whether the most recent processing cycle encountered an error.
        /// </summary>
        public bool LastCycleHadError { get; private set; }

        public BackgroundProcessor(PlayerContext playerContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        /// <summary>
        /// Starts the background processing timer.
        /// </summary>
        public void Start()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(BackgroundProcessor));
            if (_running) return;

            _stopping.Reset();
            int interval = GetTickIntervalMs();
            NextProcessTime = DateTime.UtcNow.AddMilliseconds(interval);
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

        private void OnTimerTick(object state)
        {
            if (_stopping.IsSet) return;

            ExecuteCycle();

            if (!_stopping.IsSet && !_disposed)
            {
                int interval = GetTickIntervalMs();
                NextProcessTime = DateTime.UtcNow.AddMilliseconds(interval);
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
                var colonies = new List<Colony>(_playerContext.ColonyList);

                foreach (var colony in colonies)
                {
                    if (_stopping.IsSet) break;

                    if (!colony.HasExpiredTimers()) continue;

                    try
                    {
                        lock (colony.ProcessingLock)
                        {
                            colony.ProcessColony();
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

                if (processedCount > 0)
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

                    Log.Info("BackgroundProcessor cycle complete. Processed {0} colonies.", processedCount);
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
        }
    }
}
