// <copyright file="SyncManager.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Threading.Tasks;
using NLog;

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

        private readonly RemoteFactionClient _client;
        private readonly OfflineQueue _offlineQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncManager"/> class.
        /// </summary>
        /// <param name="client">The remote faction client.</param>
        /// <param name="offlineQueue">The offline queue for storing changes while disconnected.</param>
        public SyncManager(RemoteFactionClient client, OfflineQueue offlineQueue)
        {
            _client = client;
            _offlineQueue = offlineQueue;
            Mode = OperatingMode.LocalOnly;
        }

        /// <summary>
        /// Gets or sets the current operating mode.
        /// </summary>
        public OperatingMode Mode { get; set; }

        /// <summary>
        /// Gets a value indicating whether the client is currently online.
        /// </summary>
        public bool IsOnline => _client?.IsConnected ?? false;

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

            bool connected = await _client.TryConnectAsync().ConfigureAwait(false);
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
                string syncJson = await _client.GetSyncSnapshotAsync().ConfigureAwait(false);
                if (!string.IsNullOrEmpty(syncJson))
                {
                    LastSyncTimestamp = ExtractTimestamp(syncJson);
                    ServerProcessingActive = ExtractProcessingActive(syncJson);
                    Log.Info(
                        "Full sync pulled. Timestamp={0}, ServerProcessing={1}",
                        LastSyncTimestamp ?? "(none)",
                        ServerProcessingActive);
                }
                else
                {
                    Log.Warn("Sync snapshot returned empty — server may not support sync endpoint");
                }
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Failed to pull full sync snapshot from server");
            }
        }

        /// <summary>
        /// Writes data to the server (write-through). If offline, queues the change.
        /// Called by PlayerContext.WriteContext() when mode is ServerOnly or DualWrite.
        /// </summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="dataType">The data type.</param>
        /// <param name="json">The JSON payload.</param>
        /// <returns>A task representing the async operation.</returns>
        public async Task WriteToServerAsync(string characterUUID, string dataType, string json)
        {
            if (Mode == OperatingMode.LocalOnly)
            {
                return;
            }

            if (!IsOnline)
            {
                QueueOfflineChange(characterUUID, dataType, json);
                return;
            }

            try
            {
                await _client.UploadCharacterDataAsync(characterUUID, dataType, json).ConfigureAwait(false);
                Log.Debug("Write-through to server: {0}/{1}", characterUUID, dataType);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Write-through failed (network), queuing offline: {0}/{1}", characterUUID, dataType);
                QueueOfflineChange(characterUUID, dataType, json);
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Write-through failed, queuing offline: {0}/{1}", characterUUID, dataType);
                QueueOfflineChange(characterUUID, dataType, json);
            }
        }

        /// <summary>
        /// Queues a change for later sync when the client is offline.
        /// Persists the queue to disk immediately.
        /// </summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="dataType">The data type.</param>
        /// <param name="json">The JSON payload.</param>
        public void QueueOfflineChange(string characterUUID, string dataType, string json)
        {
            var change = new QueuedChange
            {
                CharacterUUID = characterUUID,
                DataType = dataType,
                Json = json,
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

            bool connected = await _client.TryConnectAsync().ConfigureAwait(false);
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
                string syncJson = await _client.GetSyncSnapshotAsync().ConfigureAwait(false);
                if (!string.IsNullOrEmpty(syncJson))
                {
                    currentServerTimestamp = ExtractTimestamp(syncJson);
                    ServerProcessingActive = ExtractProcessingActive(syncJson);
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
        /// Flushes all queued offline changes to the server.
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
                try
                {
                    await _client.UploadCharacterDataAsync(change.CharacterUUID, change.DataType, change.Json)
                        .ConfigureAwait(false);
                    _offlineQueue.Remove(change);
                    flushed++;
                }
                catch (HttpRequestException ex)
                {
                    Log.Warn(ex, "Failed to flush queued change {0}/{1} (network error), stopping flush", change.CharacterUUID, change.DataType);
                    break;
                }
                catch (Exception ex)
                {
                    Log.Warn(ex, "Failed to flush queued change {0}/{1}, stopping flush", change.CharacterUUID, change.DataType);
                    break;
                }
            }

            _offlineQueue.Save();
            Log.Info("Flushed {0}/{1} offline changes", flushed, changes.Count);
        }

        /// <summary>
        /// Extracts the timestamp field from a sync snapshot JSON response.
        /// Returns null if parsing fails.
        /// </summary>
        private static string ExtractTimestamp(string syncJson)
        {
            try
            {
                var obj = Newtonsoft.Json.Linq.JObject.Parse(syncJson);
                return obj.Value<string>("timestamp");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Extracts the processingActive field from a sync snapshot JSON response.
        /// Returns false if parsing fails or field is absent.
        /// </summary>
        private static bool ExtractProcessingActive(string syncJson)
        {
            try
            {
                var obj = Newtonsoft.Json.Linq.JObject.Parse(syncJson);
                return obj.Value<bool>("processingActive");
            }
            catch
            {
                return false;
            }
        }
    }
}
