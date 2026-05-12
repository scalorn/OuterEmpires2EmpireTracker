// <copyright file="SyncManager.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using NLog;

using OE2EmpireTracker.Services;
namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// Manages synchronization between local data and the remote faction server.
    /// Handles write-through, offline queuing, and queue flushing on reconnection.
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
        /// Performs initial sync on application startup.
        /// Attempts to connect and flush any queued offline changes.
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
            if (connected && _offlineQueue.Count > 0)
            {
                Log.Info("Connected on startup with {0} queued changes — flushing", _offlineQueue.Count);
                await FlushOfflineQueueAsync().ConfigureAwait(false);
            }
            else if (!connected)
            {
                Log.Warn("Could not connect on startup — {0} changes remain queued", _offlineQueue.Count);
            }
        }

        /// <summary>
        /// Writes data to the server (write-through). If offline, queues the change.
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
            catch (Exception ex)
            {
                Log.Warn(ex, "Write-through failed, queuing offline: {0}/{1}", characterUUID, dataType);
                QueueOfflineChange(characterUUID, dataType, json);
            }
        }

        /// <summary>
        /// Queues a change for later sync when the client is offline.
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

            // Iterate in reverse so removals don't shift indices
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
                catch (Exception ex)
                {
                    Log.Warn(ex, "Failed to flush queued change {0}/{1}, stopping flush", change.CharacterUUID, change.DataType);
                    break;
                }
            }

            _offlineQueue.Save();
            Log.Info("Flushed {0}/{1} offline changes", flushed, changes.Count);
        }
    }
}
