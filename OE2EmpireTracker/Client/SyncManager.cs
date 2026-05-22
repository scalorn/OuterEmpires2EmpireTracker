// <copyright file="SyncManager.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
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
        /// Writes data to the server via bulk import. If offline, queues the change.
        /// Called by PlayerContext.WriteContext() when mode is ServerOnly or DualWrite.
        /// On HTTP 400: logs validation errors, queues for user review, raises SyncValidationFailed.
        /// On HTTP 403: logs denial, sets connection status to Unauthorized.
        /// </summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="dataType">The data type (retained for offline queue compatibility).</param>
        /// <param name="json">The JSON payload (PascalCase PlayerRoot).</param>
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
                var response = await _client.BulkImportAsync(characterUUID, json).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Log.Debug("Bulk import to server succeeded: {0}", characterUUID);
                    return;
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    await HandleValidationFailureAsync(characterUUID, dataType, json, response)
                        .ConfigureAwait(false);
                    return;
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    HandleForbiddenResponse(characterUUID);
                    return;
                }

                // Unexpected status — log and queue for retry
                Log.Warn(
                    "Bulk import returned unexpected status {0} for {1}, queuing offline",
                    (int)response.StatusCode,
                    characterUUID);
                QueueOfflineChange(characterUUID, dataType, json);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Bulk import failed (network) for {0}, queuing offline", characterUUID);
                QueueOfflineChange(characterUUID, dataType, json);
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Bulk import failed for {0}, queuing offline", characterUUID);
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
                try
                {
                    var response = await _client.BulkImportAsync(change.CharacterUUID, change.Json)
                        .ConfigureAwait(false);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        _offlineQueue.Remove(change);
                        flushed++;
                    }
                    else if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        await HandleValidationFailureAsync(
                            change.CharacterUUID, change.DataType, change.Json, response)
                            .ConfigureAwait(false);
                        _offlineQueue.Remove(change);
                        flushed++;
                    }
                    else if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        HandleForbiddenResponse(change.CharacterUUID);
                        break;
                    }
                    else
                    {
                        Log.Warn(
                            "Flush: unexpected status {0} for {1}/{2}, stopping flush",
                            (int)response.StatusCode,
                            change.CharacterUUID,
                            change.DataType);
                        break;
                    }
                }
                catch (HttpRequestException ex)
                {
                    Log.Warn(
                        ex,
                        "Failed to flush queued change {0}/{1} (network error), stopping flush",
                        change.CharacterUUID,
                        change.DataType);
                    break;
                }
                catch (Exception ex)
                {
                    Log.Warn(
                        ex,
                        "Failed to flush queued change {0}/{1}, stopping flush",
                        change.CharacterUUID,
                        change.DataType);
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
                var obj = JObject.Parse(syncJson);
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
                var obj = JObject.Parse(syncJson);
                return obj.Value<bool>("processingActive");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Parses validation errors from the bulk import error response body.
        /// Expected format: { "errors": [ { "entityType": "...", "error": "..." }, ... ] }.
        /// </summary>
        private static List<string> ParseValidationErrors(string responseBody)
        {
            var errors = new List<string>();

            try
            {
                var obj = JObject.Parse(responseBody);
                var errorsArray = obj["errors"] as JArray;
                if (errorsArray != null)
                {
                    foreach (var item in errorsArray)
                    {
                        string entityType = item.Value<string>("entityType") ?? "Unknown";
                        string entityUUID = item.Value<string>("entityUUID");
                        string field = item.Value<string>("field");
                        string error = item.Value<string>("error") ?? "Unknown error";

                        string message = string.Format(
                            "[{0}] {1}{2}: {3}",
                            entityType,
                            !string.IsNullOrEmpty(entityUUID) ? entityUUID + " " : string.Empty,
                            !string.IsNullOrEmpty(field) ? field : "(general)",
                            error);

                        errors.Add(message);
                    }
                }
                else
                {
                    // Fallback: try to get a single error message
                    string singleError = obj.Value<string>("error");
                    if (!string.IsNullOrEmpty(singleError))
                    {
                        errors.Add(singleError);
                    }
                    else
                    {
                        errors.Add("Validation failed (unable to parse error details)");
                    }
                }
            }
            catch (Exception)
            {
                errors.Add("Validation failed: " + (responseBody ?? "(empty response)"));
            }

            return errors;
        }

        /// <summary>
        /// Handles an HTTP 400 validation failure response from the bulk import endpoint.
        /// Logs the errors, queues the change for user review, and raises the SyncValidationFailed event.
        /// </summary>
        private async Task HandleValidationFailureAsync(
            string characterUUID,
            string dataType,
            string json,
            HttpResponseMessage response)
        {
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var errors = ParseValidationErrors(responseBody);

            Log.Warn(
                "Bulk import validation failed for {0}: {1} error(s)",
                characterUUID,
                errors.Count);

            foreach (string error in errors)
            {
                Log.Warn("  Validation error: {0}", error);
            }

            // Queue for user review with a ValidationFailed marker in the DataType
            var reviewChange = new QueuedChange
            {
                CharacterUUID = characterUUID,
                DataType = dataType + ":ValidationFailed",
                Json = json,
                QueuedUtc = SystemClock.UtcNow,
            };

            _offlineQueue.Enqueue(reviewChange);
            _offlineQueue.Save();

            SyncValidationFailed?.Invoke(this, new SyncValidationFailedEventArgs
            {
                CharacterUUID = characterUUID,
                ErrorResponseBody = responseBody,
                ValidationErrors = errors,
            });
        }

        /// <summary>
        /// Handles an HTTP 403 Forbidden response from the bulk import endpoint.
        /// Logs the denial and sets connection status to Unauthorized.
        /// </summary>
        private void HandleForbiddenResponse(string characterUUID)
        {
            Log.Error(
                "Bulk import denied (HTTP 403) for character {0} — token unauthorized",
                characterUUID);
            _client.SetConnectionStatus(false, "Unauthorized");
        }
    }
}
