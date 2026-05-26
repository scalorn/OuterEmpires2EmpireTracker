// <copyright file="GameApiSyncScheduler.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using OE2EmpireTracker.Client;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Manages periodic Player Profile synchronization across all configured characters.
    /// Uses round-robin character selection and suspends when the connection monitor reports disconnected.
    /// </summary>
    public class GameApiSyncScheduler : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly GameApiClient _client;
        private readonly GameApiCredentialManager _credentialManager;
        private readonly GameApiConnectionMonitor _connectionMonitor;

        private Timer _pollingTimer;
        private int _pollingIntervalMs;
        private bool _suspended;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameApiSyncScheduler"/> class.
        /// </summary>
        /// <param name="client">The game API client used for profile requests.</param>
        /// <param name="credentialManager">The credential manager for retrieving API keys.</param>
        /// <param name="connectionMonitor">The connection monitor for detecting connect/disconnect transitions.</param>
        public GameApiSyncScheduler(
            GameApiClient client,
            GameApiCredentialManager credentialManager,
            GameApiConnectionMonitor connectionMonitor)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _credentialManager = credentialManager ?? throw new ArgumentNullException(nameof(credentialManager));
            _connectionMonitor = connectionMonitor ?? throw new ArgumentNullException(nameof(connectionMonitor));
        }

        /// <summary>
        /// Raised when the sync status changes (started, completed, or failed).
        /// </summary>
        public event EventHandler<GameApiSyncStatusChangedEventArgs> SyncStatusChanged;

        /// <summary>
        /// Gets the UTC timestamp of the last successful sync operation.
        /// </summary>
        public DateTime? LastSyncUtc { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a sync operation is currently in progress.
        /// </summary>
        public bool IsSyncing { get; private set; }

        /// <summary>
        /// Gets or sets the current round-robin index for character selection.
        /// Internal for test access.
        /// </summary>
        internal int CurrentRoundRobinIndex { get; set; }

        /// <summary>
        /// Starts the sync scheduler with periodic polling at the specified interval.
        /// Subscribes to the connection monitor's StatusChanged event for suspend/resume.
        /// </summary>
        /// <param name="pollingIntervalMinutes">The interval between sync cycles in minutes.</param>
        public void Start(int pollingIntervalMinutes)
        {
            if (pollingIntervalMinutes < 1)
            {
                pollingIntervalMinutes = 1;
            }

            _pollingIntervalMs = pollingIntervalMinutes * 60 * 1000;
            _connectionMonitor.StatusChanged += OnConnectionStatusChanged;

            // Determine initial suspended state based on current connection status
            _suspended = _connectionMonitor.CurrentState != GameApiConnectionMonitor.ConnectionState.Connected;

            if (_suspended)
            {
                Log.Info("GameApiSyncScheduler started but suspended (connection state: {0})", _connectionMonitor.CurrentState);
                return;
            }

            Log.Info("GameApiSyncScheduler starting with {0} minute polling interval", pollingIntervalMinutes);
            _pollingTimer = new Timer(OnPollingTimerElapsed, null, _pollingIntervalMs, Timeout.Infinite);
        }

        /// <summary>
        /// Stops the sync scheduler and unsubscribes from connection monitor events.
        /// </summary>
        public void Stop()
        {
            Log.Info("GameApiSyncScheduler stopping");
            _connectionMonitor.StatusChanged -= OnConnectionStatusChanged;
            DisposeTimer();
        }

        /// <summary>
        /// Updates the polling interval without restarting the scheduler.
        /// The new interval takes effect after the current polling cycle completes.
        /// </summary>
        /// <param name="pollingIntervalMinutes">The new interval between sync cycles in minutes.</param>
        public void UpdatePollingInterval(int pollingIntervalMinutes)
        {
            if (pollingIntervalMinutes < 1)
            {
                pollingIntervalMinutes = 1;
            }

            _pollingIntervalMs = pollingIntervalMinutes * 60 * 1000;
            Log.Info("GameApiSyncScheduler polling interval updated to {0} minutes", pollingIntervalMinutes);
        }

        /// <summary>
        /// Triggers an immediate sync for all configured characters, bypassing the timer.
        /// </summary>
        /// <returns>A task representing the asynchronous sync operation.</returns>
        public async Task SyncNowAsync()
        {
            IReadOnlyList<string> playerUUIDs = _credentialManager.GetConfiguredPlayerUUIDs();
            if (playerUUIDs.Count == 0)
            {
                Log.Warn("SyncNowAsync called but no characters have configured API keys");
                return;
            }

            Log.Info("SyncNowAsync triggered for {0} character(s)", playerUUIDs.Count);
            RaiseSyncStatusChanged(true, false, null);

            bool anySuccess = false;
            string lastError = null;

            for (int i = 0; i < playerUUIDs.Count; i++)
            {
                string playerUUID = playerUUIDs[i];
                bool success = await SyncCharacterAsync(playerUUID).ConfigureAwait(false);
                if (success)
                {
                    anySuccess = true;
                }
                else
                {
                    lastError = string.Format("Sync failed for character {0}", playerUUID);
                }
            }

            if (anySuccess)
            {
                LastSyncUtc = SystemClock.UtcNow;
            }

            RaiseSyncStatusChanged(false, anySuccess, anySuccess ? null : lastError);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="GameApiSyncScheduler"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs a sync for a single character by calling GetPlayerProfileAsync.
        /// Internal for test access.
        /// </summary>
        /// <param name="playerUUID">The player UUID to sync.</param>
        /// <returns>True if the sync succeeded; otherwise false.</returns>
        internal async Task<bool> SyncCharacterAsync(string playerUUID)
        {
            SecureString secureKey = _credentialManager.GetKey(playerUUID);
            if (secureKey == null)
            {
                Log.Warn("No API key found for character {0}, skipping sync", playerUUID);
                return false;
            }

            string apiKey = SecureStringToString(secureKey);
            secureKey.Dispose();

            try
            {
                var result = await _client.GetPlayerProfileAsync(apiKey).ConfigureAwait(false);
                if (result.Success)
                {
                    Log.Info("Successfully synced profile for character {0}", playerUUID);
                    return true;
                }

                Log.Warn("Profile sync failed for character {0}", playerUUID);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error syncing profile for character {0}", playerUUID);
                return false;
            }
        }

        /// <summary>
        /// Performs a single round-robin sync cycle. Advances to the next character
        /// and attempts to sync their profile. If the character fails, it is skipped
        /// and will be retried on the next cycle.
        /// </summary>
        internal async Task PerformRoundRobinSyncAsync()
        {
            IReadOnlyList<string> playerUUIDs = _credentialManager.GetConfiguredPlayerUUIDs();
            if (playerUUIDs.Count == 0)
            {
                Log.Debug("No configured characters for round-robin sync");
                return;
            }

            // Wrap index if characters were removed
            if (CurrentRoundRobinIndex >= playerUUIDs.Count)
            {
                CurrentRoundRobinIndex = 0;
            }

            string playerUUID = playerUUIDs[CurrentRoundRobinIndex];
            Log.Debug("Round-robin sync: character index {0} ({1})", CurrentRoundRobinIndex, playerUUID);

            RaiseSyncStatusChanged(true, false, null);

            bool success = await SyncCharacterAsync(playerUUID).ConfigureAwait(false);

            if (success)
            {
                LastSyncUtc = SystemClock.UtcNow;
            }
            else
            {
                Log.Warn("Round-robin sync failed for character {0}, will retry next cycle", playerUUID);
            }

            // Advance to next character regardless of success/failure
            CurrentRoundRobinIndex = (CurrentRoundRobinIndex + 1) % playerUUIDs.Count;

            RaiseSyncStatusChanged(false, success, success ? null : string.Format("Sync failed for character {0}", playerUUID));
        }

        /// <summary>
        /// Releases unmanaged and (optionally) managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _connectionMonitor.StatusChanged -= OnConnectionStatusChanged;
                    DisposeTimer();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Converts a SecureString to a plain-text string for API calls.
        /// </summary>
        private static string SecureStringToString(SecureString secureString)
        {
            if (secureString == null)
            {
                return null;
            }

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(ptr);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                }
            }
        }

        /// <summary>
        /// Handles connection monitor status changes. Suspends polling when disconnected,
        /// resumes with an immediate sync when reconnected.
        /// </summary>
        private void OnConnectionStatusChanged(object sender, GameApiConnectionStatusChangedEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            if (e.NewState == GameApiConnectionMonitor.ConnectionState.Connected && _suspended)
            {
                Log.Info("Connection restored, resuming sync scheduler");
                _suspended = false;
                ScheduleNextPoll(0);
            }
            else if (e.NewState != GameApiConnectionMonitor.ConnectionState.Connected && !_suspended)
            {
                Log.Info("Connection lost (state: {0}), suspending sync scheduler", e.NewState);
                _suspended = true;
                DisposeTimer();
            }
        }

        /// <summary>
        /// Timer callback that performs a round-robin sync and schedules the next poll.
        /// </summary>
        private async void OnPollingTimerElapsed(object state)
        {
            if (_disposed || _suspended)
            {
                return;
            }

            await PerformRoundRobinSyncAsync().ConfigureAwait(false);
            ScheduleNextPoll(_pollingIntervalMs);
        }

        /// <summary>
        /// Schedules the next polling timer tick after the specified delay.
        /// </summary>
        /// <param name="delayMs">The delay in milliseconds before the next poll.</param>
        private void ScheduleNextPoll(int delayMs)
        {
            if (_disposed || _suspended)
            {
                return;
            }

            try
            {
                if (_pollingTimer == null)
                {
                    _pollingTimer = new Timer(OnPollingTimerElapsed, null, delayMs, Timeout.Infinite);
                }
                else
                {
                    _pollingTimer.Change(delayMs, Timeout.Infinite);
                }
            }
            catch (ObjectDisposedException)
            {
                // Timer was disposed between check and use
            }
        }

        /// <summary>
        /// Raises the SyncStatusChanged event with exception safety.
        /// </summary>
        /// <param name="isSyncing">Whether a sync is currently in progress.</param>
        /// <param name="success">Whether the last sync succeeded.</param>
        /// <param name="errorMessage">The error message if the sync failed.</param>
        private void RaiseSyncStatusChanged(bool isSyncing, bool success, string errorMessage)
        {
            IsSyncing = isSyncing;

            EventHandler<GameApiSyncStatusChangedEventArgs> handler = SyncStatusChanged;
            if (handler == null)
            {
                return;
            }

            var args = new GameApiSyncStatusChangedEventArgs
            {
                IsSyncing = isSyncing,
                Success = success,
                ErrorMessage = errorMessage,
                LastSyncUtc = LastSyncUtc,
            };

            try
            {
                handler(this, args);
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "SyncStatusChanged event handler threw an exception");
            }
        }

        /// <summary>
        /// Disposes the polling timer if it exists.
        /// </summary>
        private void DisposeTimer()
        {
            if (_pollingTimer != null)
            {
                _pollingTimer.Dispose();
                _pollingTimer = null;
            }
        }
    }
}
