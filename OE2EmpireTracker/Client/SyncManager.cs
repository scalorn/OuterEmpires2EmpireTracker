// <copyright file="SyncManager.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;

using OE2EmpireTracker.Common.Client.FactionServer;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// Manages synchronization between local data and the remote faction server.
    /// Handles write-through, offline queuing, queue flushing on reconnection,
    /// and divergence detection after offline periods.
    /// </summary>
    public class SyncManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly IFactionServerTypedClient _typedClient;
        private readonly FactionPushClient _pushClient;
        private readonly OfflineQueue _offlineQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncManager"/> class.
        /// </summary>
        /// <param name="typedClient">The typed faction server client for HTTP API calls.</param>
        /// <param name="pushClient">The push client for WebSocket connection status awareness.</param>
        /// <param name="offlineQueue">The offline queue for storing changes while disconnected.</param>
        public SyncManager(IFactionServerTypedClient typedClient, FactionPushClient pushClient, OfflineQueue offlineQueue)
        {
            _typedClient = typedClient;
            _pushClient = pushClient;
            _offlineQueue = offlineQueue;
            Mode = OperatingMode.LocalOnly;
        }

        /// <summary>
        /// Occurs when a bulk import to the server fails validation (HTTP 400).
        /// </summary>
        public event EventHandler<SyncValidationFailedEventArgs> SyncValidationFailed;

        /// <summary>
        /// Gets or sets the current operating mode.
        /// </summary>
        public OperatingMode Mode { get; set; }

        /// <summary>
        /// Gets a value indicating whether the client is currently online.
        /// </summary>
        public bool IsOnline => _pushClient?.IsConnected ?? false;

        /// <summary>
        /// Gets a value indicating whether divergence was detected after reconnection.
        /// When true, the UI should prompt the user to resolve the conflict.
        /// </summary>
        public bool DivergenceDetected { get; private set; }

        /// <summary>
        /// Gets the number of local changes that were queued while offline.
        /// Used by the divergence resolution UI.
        /// </summary>
        public int QueuedChangeCount => _offlineQueue.Count;

        /// <summary>
        /// Gets a value indicating whether the server reports that background
        /// processing is active for the current character.
        /// </summary>
        public bool ServerProcessingActive { get; private set; }

        /// <summary>
        /// Gets the server sync timestamp from the last successful sync.
        /// Used for divergence detection on reconnection.
        /// </summary>
        public string LastSyncTimestamp { get; private set; }

        /// <summary>
        /// Performs initial sync on application startup.
        /// Attempts to connect, pull full sync snapshot from server, and flush any queued offline changes.
        /// If the server reports processing is active, sets <see cref="ServerProcessingActive"/>.
        /// </summary>
        /// <returns>A task representing the async operation.</returns>
        public async Task SyncOnStartupAsync()
        {
            if (Mode == OperatingMode.LocalOnly)
            {
                Log.Debug("SyncOnStartup skipped — operating in LocalOnly mode");
                return;
            }

            _offlineQueue.Load();

            bool connected = await _pushClient.TryConnectAsync().ConfigureAwait(false);
            if (!connected)
            {
                Log.Warn("Could not connect on startup — {0} changes remain queued", _offlineQueue.Count);
                return;
            }

            // Pull full sync snapshot from server
            await PullFullSyncAsync().ConfigureAwait(false);

            // Flush any queued offline changes
            if (_offlineQueue.Count > 0)
            {
                Log.Info("Connected on startup with {0} queued changes — flushing", _offlineQueue.Count);
                await FlushOfflineQueueAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Pulls the full sync snapshot from the server and checks for server-side processing state.
        /// Updates <see cref="LastSyncTimestamp"/> and <see cref="ServerProcessingActive"/>.
        /// </summary>
        /// <returns>A task representing the async operation.</returns>
        public async Task PullFullSyncAsync()
        {
            try
            {
                var response = await _typedClient.GetSyncSnapshotAsync().ConfigureAwait(false);
                if (response != null)
                {
                    LastSyncTimestamp = response.ServerTimestamp.ToString("o");
                    ServerProcessingActive = response.ProcessingActive;
                    Log.Info(
                        "Full sync pulled. Timestamp={0}, ServerProcessing={1}",
                        LastSyncTimestamp ?? "(none)",
                        ServerProcessingActive);
                }
                else
                {
                    Log.Warn("Sync snapshot returned null — server may not support sync endpoint");
                }
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Failed to pull full sync snapshot from server");
            }
        }

        /// <summary>
        /// Writes data to the server via bulk import. If offline, queues the change.
        /// Called by PlayerContext.WriteContext() when mode is ServerOnly or DualWrite.
        /// On validation failure: logs errors, queues for user review, raises SyncValidationFailed.
        /// On authorization failure: logs denial, sets connection status to Unauthorized.
        /// </summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="dataType">The data type (retained for offline queue compatibility).</param>
        /// <param name="playerRoot">The typed player data to import.</param>
        /// <returns>A task representing the async operation.</returns>
        public async Task WriteToServerAsync(string characterUUID, string dataType, PlayerRoot playerRoot)
        {
            if (Mode == OperatingMode.LocalOnly)
            {
                return;
            }

            if (!IsOnline)
            {
                QueueOfflineChange(characterUUID, dataType, playerRoot);
                return;
            }

            try
            {
                await _typedClient.BulkImportAsync(characterUUID, playerRoot).ConfigureAwait(false);
                Log.Debug("Bulk import to server succeeded: {0}", characterUUID);
            }
            catch (FactionValidationException ex)
            {
                var errors = ex.Errors
                    .Select(e => string.Format(
                        "[{0}] {1}{2}: {3}",
                        e.EntityType,
                        !string.IsNullOrEmpty(e.EntityUUID) ? e.EntityUUID + " " : string.Empty,
                        !string.IsNullOrEmpty(e.Field) ? e.Field : "(general)",
                        e.Error))
                    .ToList();

                Log.Warn("Bulk import validation failed for {0}: {1} error(s)", characterUUID, errors.Count);
                foreach (string error in errors)
                {
                    Log.Warn("  Validation error: {0}", error);
                }

                QueueOfflineChange(characterUUID, dataType + ":ValidationFailed", playerRoot);

                SyncValidationFailed?.Invoke(this, new SyncValidationFailedEventArgs
                {
                    CharacterUUID = characterUUID,
                    ErrorResponseBody = ex.Message,
                    ValidationErrors = errors,
                });
            }
            catch (FactionAuthorizationException)
            {
                Log.Error("Bulk import denied (HTTP 403) for character {0} — token unauthorized", characterUUID);
                _pushClient.SetConnectionStatus(false, "Unauthorized");
            }
            catch (FactionConnectionException ex)
            {
                Log.Warn(ex, "Bulk import failed (network) for {0}, queuing offline", characterUUID);
                QueueOfflineChange(characterUUID, dataType, playerRoot);
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Bulk import failed for {0}, queuing offline", characterUUID);
                QueueOfflineChange(characterUUID, dataType, playerRoot);
            }
        }

        /// <summary>
        /// Queues a change for later sync when the client is offline.
        /// Persists the queue to disk immediately.
        /// </summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="dataType">The data type.</param>
        /// <param name="playerRoot">The typed player data to queue.</param>
        public void QueueOfflineChange(string characterUUID, string dataType, PlayerRoot playerRoot)
        {
            string json = JsonConvert.SerializeObject(playerRoot);
            var change = new QueuedChange
            {
                CharacterUUID = characterUUID,
                DataType = dataType,
                Json = json,
                TypedPayload = playerRoot,
                QueuedUtc = SystemClock.UtcNow,
            };

            _offlineQueue.Enqueue(change);
            _offlineQueue.Save();
            Log.Info("Queued offline change: {0}/{1} (queue size: {2})", characterUUID, dataType, _offlineQueue.Count);
        }

        /// <summary>
        /// Handles reconnection after an offline period.
        /// Flushes the offline queue and detects divergence if the server data also changed.
        /// Sets <see cref="DivergenceDetected"/> if the server timestamp differs from the last known sync.
        /// </summary>
        /// <returns>A task representing the async operation.</returns>
        public async Task HandleReconnectionAsync()
        {
            if (Mode == OperatingMode.LocalOnly)
            {
                return;
            }

            bool connected = await _pushClient.TryConnectAsync().ConfigureAwait(false);
            if (!connected)
            {
                Log.Warn("Reconnection attempt failed — still offline");
                return;
            }

            Log.Info("Reconnected to server. Checking for divergence...");

            string previousTimestamp = LastSyncTimestamp;
            string currentServerTimestamp = null;

            try
            {
                var syncResponse = await _typedClient.GetSyncSnapshotAsync().ConfigureAwait(false);
                if (syncResponse != null)
                {
                    currentServerTimestamp = syncResponse.ServerTimestamp.ToString("o");
                    ServerProcessingActive = syncResponse.ProcessingActive;
                }
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Failed to get sync snapshot during reconnection");
            }

            // Detect divergence: server changed while we had queued local changes
            bool serverChanged = !string.IsNullOrEmpty(previousTimestamp)
                && !string.IsNullOrEmpty(currentServerTimestamp)
                && !string.Equals(previousTimestamp, currentServerTimestamp, StringComparison.Ordinal);

            if (serverChanged && _offlineQueue.Count > 0)
            {
                DivergenceDetected = true;
                Log.Warn(
                    "Divergence detected: server timestamp changed ({0} -> {1}) and {2} local changes queued",
                    previousTimestamp,
                    currentServerTimestamp,
                    _offlineQueue.Count);
            }
            else
            {
                DivergenceDetected = false;

                if (_offlineQueue.Count > 0)
                {
                    Log.Info("No divergence — flushing {0} queued changes", _offlineQueue.Count);
                    await FlushOfflineQueueAsync().ConfigureAwait(false);
                }

                LastSyncTimestamp = currentServerTimestamp ?? LastSyncTimestamp;
            }
        }

        /// <summary>
        /// Resolves divergence by uploading local changes to the server (user chose "Upload Local").
        /// Flushes the offline queue and clears the divergence flag.
        /// </summary>
        /// <returns>A task representing the async operation.</returns>
        public async Task ResolveUploadLocalAsync()
        {
            Log.Info("Resolving divergence: uploading local changes to server");
            await FlushOfflineQueueAsync().ConfigureAwait(false);
            DivergenceDetected = false;
            await PullFullSyncAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Resolves divergence by downloading server data (user chose "Download Server").
        /// Discards local queued changes and pulls fresh data from the server.
        /// </summary>
        /// <returns>A task representing the async operation.</returns>
        public async Task ResolveDownloadServerAsync()
        {
            Log.Info("Resolving divergence: downloading server data (discarding {0} local changes)", _offlineQueue.Count);
            _offlineQueue.Clear();
            _offlineQueue.Save();
            DivergenceDetected = false;
            await PullFullSyncAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Flushes all queued offline changes to the server via bulk import.
        /// Removes each change from the queue after successful upload.
        /// </summary>
        /// <returns>A task representing the async operation.</returns>
        public async Task FlushOfflineQueueAsync()
        {
            var changes = _offlineQueue.GetAll();
            if (changes.Count == 0)
            {
                return;
            }

            Log.Info("Flushing {0} offline changes to server", changes.Count);
            int flushed = 0;

            for (int i = changes.Count - 1; i >= 0; i--)
            {
                var change = changes[i];

                if (change.TypedPayload == null)
                {
                    Log.Warn(
                        "Skipping queued change {0}/{1} — typed payload is null (legacy entry)",
                        change.CharacterUUID,
                        change.DataType);
                    _offlineQueue.Remove(change);
                    continue;
                }

                try
                {
                    await _typedClient.BulkImportAsync(change.CharacterUUID, change.TypedPayload)
                        .ConfigureAwait(false);
                    _offlineQueue.Remove(change);
                    flushed++;
                }
                catch (FactionValidationException ex)
                {
                    var errors = ex.Errors
                        .Select(e => string.Format(
                            "[{0}] {1}{2}: {3}",
                            e.EntityType,
                            !string.IsNullOrEmpty(e.EntityUUID) ? e.EntityUUID + " " : string.Empty,
                            !string.IsNullOrEmpty(e.Field) ? e.Field : "(general)",
                            e.Error))
                        .ToList();

                    Log.Warn("Flush: validation failed for {0}: {1} error(s)", change.CharacterUUID, errors.Count);
                    _offlineQueue.Remove(change);
                    flushed++;

                    SyncValidationFailed?.Invoke(this, new SyncValidationFailedEventArgs
                    {
                        CharacterUUID = change.CharacterUUID,
                        ErrorResponseBody = ex.Message,
                        ValidationErrors = errors,
                    });
                }
                catch (FactionAuthorizationException)
                {
                    Log.Error("Flush: denied (HTTP 403) for {0} — stopping flush", change.CharacterUUID);
                    _pushClient.SetConnectionStatus(false, "Unauthorized");
                    break;
                }
                catch (FactionConnectionException ex)
                {
                    Log.Warn(ex, "Flush: network error for {0}/{1}, stopping flush", change.CharacterUUID, change.DataType);
                    break;
                }
                catch (Exception ex)
                {
                    Log.Warn(ex, "Flush: failed for {0}/{1}, stopping flush", change.CharacterUUID, change.DataType);
                    break;
                }
            }

            _offlineQueue.Save();
            Log.Info("Flushed {0}/{1} offline changes", flushed, changes.Count);
        }
    }
}
