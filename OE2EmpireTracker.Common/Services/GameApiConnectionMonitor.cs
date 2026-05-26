// <copyright file="GameApiConnectionMonitor.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using OE2EmpireTracker.Client;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Monitors game API connectivity via periodic health checks.
    /// Implements exponential backoff reconnection and raises status change events.
    /// </summary>
    public class GameApiConnectionMonitor : IDisposable
    {
        private const int InitialBackoffMs = 1000;
        private const int MaxBackoffMs = 60000;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly GameApiClient _client;
        private readonly GameApiCredentialManager _credentialManager;
        private readonly string _playerUUID;

        private Timer _pollingTimer;
        private int _pollingIntervalMs;
        private int _currentBackoffMs;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameApiConnectionMonitor"/> class.
        /// </summary>
        /// <param name="client">The game API client used for health checks.</param>
        /// <param name="credentialManager">The credential manager for retrieving API keys.</param>
        /// <param name="playerUUID">The player UUID whose API key to use for health checks.</param>
        public GameApiConnectionMonitor(
            GameApiClient client,
            GameApiCredentialManager credentialManager,
            string playerUUID)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _credentialManager = credentialManager ?? throw new ArgumentNullException(nameof(credentialManager));
            _playerUUID = playerUUID ?? throw new ArgumentNullException(nameof(playerUUID));
            _currentBackoffMs = InitialBackoffMs;
            CurrentState = ConnectionState.NotConfigured;
            StatusMessage = "Not configured";
        }

        /// <summary>
        /// Raised when the connection state changes.
        /// </summary>
        public event EventHandler<GameApiConnectionStatusChangedEventArgs> StatusChanged;

        /// <summary>
        /// Represents the possible connection states for the game API.
        /// </summary>
        public enum ConnectionState
        {
            /// <summary>No API key is configured.</summary>
            NotConfigured,

            /// <summary>The game API is reachable and responding.</summary>
            Connected,

            /// <summary>The game API is unreachable.</summary>
            Disconnected,

            /// <summary>The circuit breaker is open due to repeated failures.</summary>
            DisconnectedCircuitOpen,

            /// <summary>The API key is invalid (HTTP 401).</summary>
            DisconnectedInvalidKey,

            /// <summary>A sync operation is in progress.</summary>
            Syncing,

            /// <summary>The rate limiter is pausing requests.</summary>
            RateLimited,
        }

        /// <summary>
        /// Gets the current connection state.
        /// </summary>
        public ConnectionState CurrentState { get; private set; }

        /// <summary>
        /// Gets a human-readable status message describing the current state.
        /// </summary>
        public string StatusMessage { get; private set; }

        /// <summary>
        /// Starts the connection monitor with periodic health checks.
        /// Performs an initial health check within 10 seconds of calling Start.
        /// </summary>
        /// <param name="pollingIntervalMinutes">The interval between health checks in minutes.</param>
        public void Start(int pollingIntervalMinutes)
        {
            if (pollingIntervalMinutes < 1)
            {
                pollingIntervalMinutes = 1;
            }

            _pollingIntervalMs = pollingIntervalMinutes * 60 * 1000;

            if (!_credentialManager.HasKey(_playerUUID))
            {
                TransitionTo(ConnectionState.NotConfigured, "No API key configured");
                return;
            }

            Log.Info("GameApiConnectionMonitor starting with {0} minute polling interval", pollingIntervalMinutes);

            // Perform initial health check within 10 seconds
            _pollingTimer = new Timer(OnPollingTimerElapsed, null, TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Stops the connection monitor and disposes the polling timer.
        /// </summary>
        public void Stop()
        {
            Log.Info("GameApiConnectionMonitor stopping");
            DisposeTimer();
        }

        /// <summary>
        /// Updates the polling interval without restarting the monitor.
        /// The new interval takes effect after the current polling cycle completes.
        /// </summary>
        /// <param name="pollingIntervalMinutes">The new interval between health checks in minutes.</param>
        public void UpdatePollingInterval(int pollingIntervalMinutes)
        {
            if (pollingIntervalMinutes < 1)
            {
                pollingIntervalMinutes = 1;
            }

            _pollingIntervalMs = pollingIntervalMinutes * 60 * 1000;
            Log.Info("GameApiConnectionMonitor polling interval updated to {0} minutes", pollingIntervalMinutes);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="GameApiConnectionMonitor"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Transitions the monitor to a new state. Raises the StatusChanged event with exception safety.
        /// </summary>
        /// <param name="newState">The target connection state.</param>
        /// <param name="message">A descriptive message for the transition.</param>
        internal void TransitionTo(ConnectionState newState, string message)
        {
            ConnectionState oldState = CurrentState;
            if (oldState == newState)
            {
                return;
            }

            CurrentState = newState;
            StatusMessage = message ?? string.Empty;

            Log.Info(
                "Game API connection state: {0} → {1} ({2})",
                oldState,
                newState,
                message);

            RaiseStatusChanged(oldState, newState, message);
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
        /// Timer callback that performs a health check and schedules the next poll.
        /// </summary>
        private async void OnPollingTimerElapsed(object state)
        {
            if (_disposed)
            {
                return;
            }

            await PerformHealthCheckAsync().ConfigureAwait(false);
            ScheduleNextPoll();
        }

        /// <summary>
        /// Performs a health check against the game API and transitions state accordingly.
        /// </summary>
        private async Task PerformHealthCheckAsync()
        {
            SecureString secureKey = _credentialManager.GetKey(_playerUUID);
            if (secureKey == null)
            {
                TransitionTo(ConnectionState.NotConfigured, "No API key configured");
                return;
            }

            string apiKey = SecureStringToString(secureKey);
            secureKey.Dispose();

            try
            {
                if (_client.IsCircuitOpen)
                {
                    TransitionTo(
                        ConnectionState.DisconnectedCircuitOpen,
                        "Circuit breaker is open — too many consecutive failures");
                    return;
                }

                var result = await _client.CheckHealthAsync(apiKey).ConfigureAwait(false);

                if (result.Success)
                {
                    _currentBackoffMs = InitialBackoffMs;
                    TransitionTo(ConnectionState.Connected, result.Message);
                }
                else
                {
                    HandleHealthCheckFailure(result.Message);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error during game API health check");
                HandleHealthCheckFailure(ex.Message);
            }
        }

        /// <summary>
        /// Handles a failed health check by transitioning to the appropriate disconnected state
        /// and applying exponential backoff.
        /// </summary>
        /// <param name="failureMessage">The failure reason message.</param>
        private void HandleHealthCheckFailure(string failureMessage)
        {
            if (failureMessage != null && failureMessage.Contains("401"))
            {
                TransitionTo(ConnectionState.DisconnectedInvalidKey, "API key is invalid (HTTP 401)");
                return;
            }

            if (_client.IsCircuitOpen)
            {
                TransitionTo(
                    ConnectionState.DisconnectedCircuitOpen,
                    "Circuit breaker is open — too many consecutive failures");
            }
            else
            {
                TransitionTo(ConnectionState.Disconnected, failureMessage ?? "Health check failed");
            }

            // Apply exponential backoff
            _currentBackoffMs = Math.Min(_currentBackoffMs * 2, MaxBackoffMs);
        }

        /// <summary>
        /// Schedules the next polling timer based on current state.
        /// Uses backoff interval when disconnected, normal polling interval when connected.
        /// </summary>
        private void ScheduleNextPoll()
        {
            if (_disposed)
            {
                return;
            }

            // Don't schedule if in a terminal state
            if (CurrentState == ConnectionState.NotConfigured ||
                CurrentState == ConnectionState.DisconnectedInvalidKey)
            {
                return;
            }

            int nextIntervalMs = CurrentState == ConnectionState.Connected
                ? _pollingIntervalMs
                : _currentBackoffMs;

            try
            {
                _pollingTimer?.Change(nextIntervalMs, Timeout.Infinite);
            }
            catch (ObjectDisposedException)
            {
                // Timer was disposed between check and use
            }
        }

        /// <summary>
        /// Raises the StatusChanged event with exception safety.
        /// State transitions proceed even if the event handler throws.
        /// </summary>
        /// <param name="oldState">The previous connection state.</param>
        /// <param name="newState">The new connection state.</param>
        /// <param name="message">A descriptive message about the transition.</param>
        private void RaiseStatusChanged(ConnectionState oldState, ConnectionState newState, string message)
        {
            EventHandler<GameApiConnectionStatusChangedEventArgs> handler = StatusChanged;
            if (handler == null)
            {
                return;
            }

            var args = new GameApiConnectionStatusChangedEventArgs
            {
                OldState = oldState,
                NewState = newState,
                Message = message,
            };

            try
            {
                handler(this, args);
            }
            catch (Exception ex)
            {
                Log.Warn(
                    ex,
                    "StatusChanged event handler threw an exception during {0} → {1} transition",
                    oldState,
                    newState);
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
