// <copyright file="GameApiSyncScheduler.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Common.Client;
using OE2EmpireTracker.Common.Client.Generated;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using Polly.CircuitBreaker;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Manages periodic Player Profile synchronization across all configured characters.
    /// Uses round-robin character selection and suspends when the connection monitor reports disconnected.
    /// Authenticates via OAuth2 token exchange (appId + clientId + secret).
    /// </summary>
    public class GameApiSyncScheduler : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly IGameApiTypedClient _typedClient;
        private readonly GameApiCredentialManager _credentialManager;
        private readonly GameApiConnectionMonitor _connectionMonitor;
        private readonly string _appId;
        private readonly string _clientId;

        private Timer _pollingTimer;
        private int _pollingIntervalMs;
        private bool _suspended;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameApiSyncScheduler"/> class.
        /// </summary>
        /// <param name="typedClient">The typed game API client used for API requests.</param>
        /// <param name="credentialManager">The credential manager for retrieving secrets.</param>
        /// <param name="connectionMonitor">The connection monitor for detecting connect/disconnect transitions.</param>
        /// <param name="appId">The registered application GUID.</param>
        /// <param name="clientId">The player's account identifier.</param>
        public GameApiSyncScheduler(
            IGameApiTypedClient typedClient,
            GameApiCredentialManager credentialManager,
            GameApiConnectionMonitor connectionMonitor,
            string appId,
            string clientId)
        {
            _typedClient = typedClient ?? throw new ArgumentNullException(nameof(typedClient));
            _credentialManager = credentialManager ?? throw new ArgumentNullException(nameof(credentialManager));
            _connectionMonitor = connectionMonitor ?? throw new ArgumentNullException(nameof(connectionMonitor));
            _appId = appId ?? string.Empty;
            _clientId = clientId ?? string.Empty;
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
                Log.Warn("SyncNowAsync called but no characters have configured secrets");
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
        /// Merges game-authoritative fields from the API response into the local player profile.
        /// Delegates to <see cref="ProfileMergeService.MergeProfileData"/> for the actual merge logic.
        /// </summary>
        /// <param name="local">The local player profile to update.</param>
        /// <param name="remote">The API response containing authoritative game data.</param>
        /// <returns>True if any fields were changed; otherwise false.</returns>
        internal static bool MergeProfileData(PlayerProfile local, PublicCharacter remote)
        {
            return ProfileMergeService.MergeProfileData(local, remote);
        }

        /// <summary>
        /// Performs a sync for a single character by exchanging a token and calling GetCharacterAsync.
        /// Merges game-authoritative fields into the local profile.
        /// Internal for test access.
        /// </summary>
        /// <param name="playerUUID">The player UUID to sync.</param>
        /// <returns>True if the sync succeeded; otherwise false.</returns>
        internal async Task<bool> SyncCharacterAsync(string playerUUID)
        {
            SecureString secureSecret = _credentialManager.GetKey(playerUUID);
            if (secureSecret == null)
            {
                Log.Warn("No secret found for character {0}, skipping sync", playerUUID);
                return false;
            }

            string secret = SecureStringToString(secureSecret);
            secureSecret.Dispose();

            try
            {
                // Exchange token first
                await _typedClient.ExchangeTokenAsync(_appId, _clientId, secret).ConfigureAwait(false);

                // Use the token to fetch character data
                var remoteProfile = await _typedClient.GetCharacterAsync().ConfigureAwait(false);
                if (remoteProfile == null)
                {
                    Log.Warn("Profile sync for character {0}: response was null", playerUUID);
                    return false;
                }

                var localProfile = GetPlayerProfile(playerUUID);
                if (localProfile != null)
                {
                    MergeProfileData(localProfile, remoteProfile);
                }

                Log.Info("Successfully synced profile for character {0}", playerUUID);

                // Colony sync runs after profile sync succeeds (Req 5.1)
                try
                {
                    await SyncColoniesAsync(playerUUID).ConfigureAwait(false);
                }
                catch (Exception colonyEx)
                {
                    Log.Error(colonyEx, "Colony sync failed for character {0}, profile sync result preserved", playerUUID);
                }

                // Asset sync runs after colony sync (Req 8.1)
                try
                {
                    await SyncAssetsAsync(playerUUID).ConfigureAwait(false);
                }
                catch (Exception assetEx)
                {
                    Log.Error(assetEx, "Asset sync failed for character {0}, profile/colony sync results preserved", playerUUID);
                }

                // Banking sync runs after asset sync (Req 4.2)
                try
                {
                    await SyncBankingAsync(playerUUID).ConfigureAwait(false);
                }
                catch (Exception bankingEx)
                {
                    Log.Error(bankingEx, "Banking sync failed for character {0}, profile/colony/asset sync results preserved", playerUUID);
                }

                return true;
            }
            catch (ApiHttpException ex) when (ex.StatusCode == 401)
            {
                Log.Warn("Profile sync for character {0}: credentials are invalid (HTTP 401), stopping polling", playerUUID);
                _connectionMonitor.TransitionTo(
                    GameApiConnectionMonitor.ConnectionState.DisconnectedInvalidKey,
                    "Credentials are invalid (HTTP 401)");
                return false;
            }
            catch (ApiHttpException ex) when (ex.StatusCode == 403)
            {
                Log.Warn("Profile sync for character {0}: scope not granted (HTTP 403)", playerUUID);
                return false;
            }
            catch (ApiHttpException ex) when (ex.StatusCode == 429)
            {
                Log.Warn("Profile sync for character {0}: rate limited (HTTP 429)", playerUUID);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error syncing profile for character {0}", playerUUID);
                return false;
            }
        }

        /// <summary>
        /// Retrieves the local PlayerProfile for the given UUID.
        /// Override point for testing.
        /// </summary>
        /// <param name="playerUUID">The player UUID to look up.</param>
        /// <returns>The local PlayerProfile, or null if not found.</returns>
        internal virtual PlayerProfile GetPlayerProfile(string playerUUID)
        {
            return null;
        }

        /// <summary>
        /// Retrieves the mutable colony list for the given player UUID.
        /// Override point for testing.
        /// </summary>
        /// <param name="playerUUID">The player UUID to look up.</param>
        /// <returns>The mutable colony list, or null if not found.</returns>
        internal virtual List<Colony> GetPlayerColonies(string playerUUID)
        {
            return null;
        }

        /// <summary>
        /// Persists the current player context to disk.
        /// Override point for testing.
        /// </summary>
        internal virtual void WriteContext()
        {
        }

        /// <summary>
        /// Raises the ColonyDataChanged event to notify UI subscribers.
        /// Override point for testing.
        /// </summary>
        internal virtual void RaiseColonyDataChanged()
        {
        }

        /// <summary>
        /// Notifies subscribers that a specific colony's data has changed.
        /// Override point for testing.
        /// </summary>
        /// <param name="colonyUUID">The UUID of the colony that changed.</param>
        internal virtual void RaiseColonyDataChanged(string colonyUUID)
        {
        }

        /// <summary>
        /// Retrieves the mutable station list for the given player UUID.
        /// Override point for testing.
        /// </summary>
        /// <param name="playerUUID">The player UUID to look up.</param>
        /// <returns>The mutable station list, or null if not found.</returns>
        internal virtual List<Station> GetPlayerStations(string playerUUID)
        {
            return null;
        }

        /// <summary>
        /// Retrieves the mutable ship list for the given player UUID.
        /// Override point for testing.
        /// </summary>
        /// <param name="playerUUID">The player UUID to look up.</param>
        /// <returns>The mutable ship list, or null if not found.</returns>
        internal virtual List<Ship> GetPlayerShips(string playerUUID)
        {
            return null;
        }

        /// <summary>
        /// Raises the AssetDataChanged event to notify UI subscribers.
        /// Override point for testing.
        /// </summary>
        internal virtual void RaiseAssetDataChanged()
        {
        }

        /// <summary>
        /// Adds a newly created station to the player's data.
        /// Override point for testing.
        /// </summary>
        /// <param name="station">The station to add.</param>
        internal virtual void AddStation(Station station)
        {
        }

        /// <summary>
        /// Adds a newly created ship to the player's data.
        /// Override point for testing.
        /// </summary>
        /// <param name="ship">The ship to add.</param>
        internal virtual void AddShip(Ship ship)
        {
        }

        /// <summary>
        /// Creates a <see cref="BlueprintLinkageService"/> for use during colony warehouse sync.
        /// Override point for testing.
        /// </summary>
        /// <returns>A blueprint linkage service, or null if linkage is not available.</returns>
        internal virtual BlueprintLinkageService CreateBlueprintLinkageService()
        {
            return null;
        }

        /// <summary>
        /// Creates a <see cref="SurveyLinkageService"/> for use during colony warehouse sync.
        /// Override point for testing.
        /// </summary>
        /// <returns>A survey linkage service, or null if linkage is not available.</returns>
        internal virtual SurveyLinkageService CreateSurveyLinkageService()
        {
            return null;
        }

        /// <summary>
        /// Raises the BankingDataChanged event to notify UI subscribers.
        /// Override point for testing.
        /// </summary>
        internal virtual void RaiseBankingDataChanged()
        {
        }

        /// <summary>
        /// Retrieves the PlayerContext for banking operations.
        /// Override point for testing.
        /// </summary>
        /// <returns>The player context, or null if not available.</returns>
        internal virtual PlayerContext GetPlayerContext()
        {
            return null;
        }

        /// <summary>
        /// Sets the banking balance on the player context.
        /// Override point for testing.
        /// </summary>
        /// <param name="balance">The new banking balance value.</param>
        internal virtual void SetBankingBalance(decimal balance)
        {
        }

        /// <summary>
        /// Fetches the colony list from the game API and merges it into local data.
        /// Called after profile sync succeeds.
        /// </summary>
        /// <param name="playerUUID">The player UUID whose colonies to sync.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        internal async Task SyncColoniesAsync(string playerUUID)
        {
            Log.Info("Colony sync starting for character {0}", playerUUID);

            ColonyList colonyListResponse;
            try
            {
                colonyListResponse = await _typedClient.GetColonyListAsync().ConfigureAwait(false);
            }
            catch (ApiHttpException ex) when (ex.StatusCode == 401)
            {
                Log.Warn("Colony sync for character {0}: token rejected (HTTP 401)", playerUUID);
                _connectionMonitor.TransitionTo(
                    GameApiConnectionMonitor.ConnectionState.DisconnectedInvalidKey,
                    "Credentials are invalid (HTTP 401)");
                return;
            }
            catch (ApiHttpException ex) when (ex.StatusCode == 403)
            {
                Log.Info("Colony sync skipped: colony.list.read scope not granted");
                return;
            }
            catch (ApiHttpException ex)
            {
                Log.Warn("Colony sync failed for character {0}: HTTP {1}", playerUUID, ex.StatusCode);
                return;
            }

            if (colonyListResponse?.Colonies == null)
            {
                Log.Warn("Colony sync for character {0}: response or colonies was null", playerUUID);
                return;
            }

            Log.Info(
                "Received {0} colonies from game API for character {1}",
                colonyListResponse.Colonies.Count,
                playerUUID);

            var localColonies = GetPlayerColonies(playerUUID);
            if (localColonies == null)
            {
                Log.Warn("Colony sync for character {0}: no local colony list available", playerUUID);
                return;
            }

            var mergeResult = ColonyMergeService.MergeColonyList(
                colonyListResponse,
                localColonies,
                playerUUID);

            // Fetch per-colony details
            bool buildingsScopeAvailable = true;
            bool warehouseScopeAvailable = true;
            bool workersScopeAvailable = true;

            foreach (var apiColony in colonyListResponse.Colonies)
            {
                if (apiColony.RemoteAccess <= 0)
                {
                    Log.Debug(
                        "Skipping detail sync for colony {0} (colonyId={1}): no Remote Operations Array",
                        apiColony.ColonyName,
                        apiColony.ColonyId);
                    continue;
                }

                if (!mergeResult.ColonyIdToUUIDMap.TryGetValue(apiColony.ColonyId, out string colonyUUID))
                {
                    continue;
                }

                var colony = FindMutableColony(colonyUUID, localColonies);
                if (colony == null)
                {
                    continue;
                }

                bool colonyChanged = false;

                // Buildings
                if (buildingsScopeAvailable)
                {
                    try
                    {
                        var buildingsResponse = await _typedClient.GetColonyBuildingsAsync(apiColony.ColonyId).ConfigureAwait(false);
                        if (buildingsResponse != null)
                        {
                            bool buildingsChanged = ColonyMergeService.MergeBuildings(buildingsResponse, colony);
                            colonyChanged |= buildingsChanged;
                            if (buildingsChanged && mergeResult.Updated == 0)
                            {
                                mergeResult.Updated++;
                            }
                        }
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 403)
                    {
                        buildingsScopeAvailable = false;
                        Log.Info("Colony sync: colony.buildings.read scope not available, skipping buildings for all colonies");
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 404)
                    {
                        Log.Debug("Colony sync: buildings not found for colonyId={0}", apiColony.ColonyId);
                    }
                    catch (ApiHttpException ex)
                    {
                        Log.Warn("Colony sync: buildings fetch failed for colonyId={0}: HTTP {1}", apiColony.ColonyId, ex.StatusCode);
                    }
                }

                // Warehouse
                if (warehouseScopeAvailable)
                {
                    try
                    {
                        var warehouseResponse = await _typedClient.GetColonyWarehouseAsync(apiColony.ColonyId).ConfigureAwait(false);
                        if (warehouseResponse != null)
                        {
                            var blueprintLinkage = CreateBlueprintLinkageService();
                            var surveyLinkage = CreateSurveyLinkageService();
                            bool warehouseChanged = ColonyMergeService.MergeWarehouse(
                                warehouseResponse, colony, blueprintLinkage, surveyLinkage);
                            colonyChanged |= warehouseChanged;
                            if (warehouseChanged && mergeResult.Updated == 0)
                            {
                                mergeResult.Updated++;
                            }
                        }
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 403)
                    {
                        warehouseScopeAvailable = false;
                        Log.Info("Colony sync: colony.warehouse.read scope not available, skipping warehouse for all colonies");
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 404)
                    {
                        Log.Debug("Colony sync: warehouse not found for colonyId={0}", apiColony.ColonyId);
                    }
                    catch (ApiHttpException ex)
                    {
                        Log.Warn("Colony sync: warehouse fetch failed for colonyId={0}: HTTP {1}", apiColony.ColonyId, ex.StatusCode);
                    }
                }

                // Workers
                if (workersScopeAvailable)
                {
                    try
                    {
                        var workersResponse = await _typedClient.GetColonyWorkersAsync(apiColony.ColonyId).ConfigureAwait(false);
                        if (workersResponse != null)
                        {
                            bool workersChanged = ColonyMergeService.MergeWorkers(workersResponse, colony);
                            colonyChanged |= workersChanged;
                            if (workersChanged && mergeResult.Updated == 0)
                            {
                                mergeResult.Updated++;
                            }
                        }
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 403)
                    {
                        workersScopeAvailable = false;
                        Log.Info("Colony sync: colony.workers.read scope not available, skipping workers for all colonies");
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 404)
                    {
                        Log.Debug("Colony sync: workers not found for colonyId={0}", apiColony.ColonyId);
                    }
                    catch (ApiHttpException ex)
                    {
                        Log.Warn("Colony sync: workers fetch failed for colonyId={0}: HTTP {1}", apiColony.ColonyId, ex.StatusCode);
                    }
                }

                if (colonyChanged)
                {
                    RaiseColonyDataChanged(colonyUUID);
                }
            }

            if (mergeResult.HasChanges)
            {
                WriteContext();
                RaiseColonyDataChanged();
            }

            Log.Info(
                "Colony sync complete for character {0}: {1} created, {2} updated, {3} skipped",
                playerUUID,
                mergeResult.Created,
                mergeResult.Updated,
                mergeResult.Skipped);
        }

        /// <summary>
        /// Fetches asset locations from the game API, iterates each location with assets,
        /// retrieves cargo details, and merges into the appropriate local model.
        /// </summary>
        /// <param name="playerUUID">The player UUID whose assets to sync.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        internal virtual async Task SyncAssetsAsync(string playerUUID)
        {
            Log.Info("Asset sync starting for character {0}", playerUUID);

            int locationsSynced = 0;
            int itemsProcessed = 0;
            int errorsEncountered = 0;

            AssetLocations locationsResponse;
            try
            {
                locationsResponse = await _typedClient.GetAssetLocationsAsync().ConfigureAwait(false);
            }
            catch (ApiHttpException ex) when (ex.StatusCode == 401)
            {
                Log.Warn("Asset sync for character {0}: token rejected (HTTP 401)", playerUUID);
                _connectionMonitor.TransitionTo(
                    GameApiConnectionMonitor.ConnectionState.DisconnectedInvalidKey,
                    "Credentials are invalid (HTTP 401)");
                return;
            }
            catch (ApiHttpException ex) when (ex.StatusCode == 403)
            {
                Log.Info("Asset sync skipped: assets.locations.read scope not granted");
                return;
            }
            catch (ApiHttpException ex)
            {
                Log.Warn("Asset sync failed for character {0}: HTTP {1}", playerUUID, ex.StatusCode);
                errorsEncountered++;
                Log.Info(
                    "Asset sync complete for character {0}: {1} locations synced, {2} items processed, {3} errors",
                    playerUUID,
                    locationsSynced,
                    itemsProcessed,
                    errorsEncountered);
                return;
            }

            if (locationsResponse?.Locations == null)
            {
                Log.Warn("Asset sync for character {0}: response or locations was null", playerUUID);
                return;
            }

            var activeLocations = locationsResponse.Locations.Where(l => l.AssetCount > 0).ToList();
            Log.Info(
                "Asset sync: {0} total locations, {1} with assets for character {2}",
                locationsResponse.Locations.Count,
                activeLocations.Count,
                playerUUID);

            var localColonies = GetPlayerColonies(playerUUID);
            var localStations = GetPlayerStations(playerUUID);
            var localShips = GetPlayerShips(playerUUID);
            bool anyChanges = false;

            foreach (var location in activeLocations)
            {
                try
                {
                    var detailResponse = await _typedClient.GetAssetLocationDetailAsync(
                        location.LocationId,
                        location.LocationType).ConfigureAwait(false);

                    if (detailResponse?.Cargo == null)
                    {
                        Log.Warn(
                            "Asset sync: detail response was null for location {0}, skipping",
                            location.LocationId);
                        errorsEncountered++;
                        continue;
                    }

                    bool merged = RouteAssetMerge(
                        location,
                        detailResponse.Cargo,
                        localColonies,
                        localStations,
                        localShips,
                        playerUUID);

                    if (merged)
                    {
                        anyChanges = true;
                    }

                    locationsSynced++;
                    itemsProcessed += detailResponse.Cargo.Count;
                }
                catch (ApiHttpException ex) when (ex.StatusCode == 401)
                {
                    Log.Warn(
                        "Asset sync for character {0}: detail request for location {1} returned HTTP 401, aborting cycle",
                        playerUUID,
                        location.LocationId);
                    _connectionMonitor.TransitionTo(
                        GameApiConnectionMonitor.ConnectionState.DisconnectedInvalidKey,
                        "Credentials are invalid (HTTP 401)");
                    return;
                }
                catch (ApiHttpException ex) when (ex.StatusCode == 403)
                {
                    Log.Info(
                        "Asset sync: location {0} ({1}) returned HTTP 403, skipping",
                        location.LocationId,
                        location.LocationName);
                    errorsEncountered++;
                }
                catch (ApiHttpException ex) when (ex.StatusCode == 404)
                {
                    Log.Warn(
                        "Asset sync: location {0} ({1}) returned HTTP 404, skipping",
                        location.LocationId,
                        location.LocationName);
                    errorsEncountered++;
                }
                catch (ApiHttpException ex)
                {
                    Log.Warn(
                        "Asset sync: failed to fetch detail for location {0} ({1}): HTTP {2}",
                        location.LocationId,
                        location.LocationName,
                        ex.StatusCode);
                    errorsEncountered++;
                }
                catch (BrokenCircuitException)
                {
                    Log.Warn("Asset sync: circuit breaker open, aborting asset sync cycle");
                    errorsEncountered++;
                    break;
                }
            }

            if (anyChanges)
            {
                WriteContext();
                RaiseAssetDataChanged();
            }

            Log.Info(
                "Asset sync complete for character {0}: {1} locations synced, {2} items processed, {3} errors",
                playerUUID,
                locationsSynced,
                itemsProcessed,
                errorsEncountered);
        }

        /// <summary>
        /// Performs a single round-robin sync cycle.
        /// </summary>
        internal async Task PerformRoundRobinSyncAsync()
        {
            IReadOnlyList<string> playerUUIDs = _credentialManager.GetConfiguredPlayerUUIDs();
            if (playerUUIDs.Count == 0)
            {
                Log.Debug("No configured characters for round-robin sync");
                return;
            }

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
        /// Parses the station name from the API locationName by stripping the system suffix.
        /// </summary>
        private static string ParseStationName(string locationName, string systemName)
        {
            if (string.IsNullOrEmpty(locationName))
            {
                return string.Empty;
            }

            string suffix = " (" + systemName + ")";
            if (!string.IsNullOrEmpty(systemName) &&
                locationName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return locationName.Substring(0, locationName.Length - suffix.Length).Trim();
            }

            return locationName;
        }

        /// <summary>
        /// Parses the ship name from the API locationName by stripping prefix and location suffix.
        /// </summary>
        private static string ParseShipName(string locationName)
        {
            if (string.IsNullOrEmpty(locationName))
            {
                return string.Empty;
            }

            string name = locationName;

            if (name.StartsWith("Ship ", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(5);
            }

            int atIdx = name.IndexOf(" at ", StringComparison.OrdinalIgnoreCase);
            if (atIdx > 0)
            {
                return name.Substring(0, atIdx).Trim();
            }

            int inSpaceIdx = name.IndexOf(" in space in ", StringComparison.OrdinalIgnoreCase);
            if (inSpaceIdx > 0)
            {
                return name.Substring(0, inSpaceIdx).Trim();
            }

            return name.Trim();
        }

        /// <summary>
        /// Syncs banking transactions and balance for a character.
        /// </summary>
        private async Task SyncBankingAsync(string playerUUID)
        {
            Log.Info("Banking sync starting for character {0}", playerUUID);

            BankingImportResult txResult = await BankingService.ImportTransactionsAsync(
                _typedClient, GetPlayerContext()).ConfigureAwait(false);

            if (!txResult.Success)
            {
                Log.Warn(
                    "Banking transaction import reported failure for character {0}: {1}",
                    playerUUID,
                    txResult.ErrorMessage);
            }
            else
            {
                Log.Info(
                    "Banking transactions imported: {0} new, {1} duplicates skipped",
                    txResult.TransactionsImported,
                    txResult.DuplicatesSkipped);
            }

            decimal? balance = await BankingService.ImportBalanceAsync(_typedClient).ConfigureAwait(false);

            if (balance.HasValue)
            {
                SetBankingBalance(balance.Value);
                WriteContext();
                RaiseBankingDataChanged();
                Log.Info("Banking balance updated to {0:N2} for character {1}", balance.Value, playerUUID);
            }
            else
            {
                Log.Warn("Banking balance import returned null for character {0}", playerUUID);
            }
        }

        /// <summary>
        /// Routes asset cargo items to the appropriate merge method based on location type.
        /// </summary>
        private bool RouteAssetMerge(
            AssetLocation location,
            ICollection<AssetCargoItem> cargoItems,
            List<Colony> localColonies,
            List<Station> localStations,
            List<Ship> localShips,
            string playerUUID)
        {
            if (string.Equals(location.LocationType, AssetTypeCodes.Colony, StringComparison.OrdinalIgnoreCase))
            {
                return MergeColonyLocation(location, cargoItems, localColonies);
            }

            if (string.Equals(location.LocationType, AssetTypeCodes.Station, StringComparison.OrdinalIgnoreCase))
            {
                return MergeStationLocation(location, cargoItems, localStations, playerUUID);
            }

            if (string.Equals(location.LocationType, AssetTypeCodes.Ship, StringComparison.OrdinalIgnoreCase))
            {
                return MergeShipLocation(location, cargoItems, localShips);
            }

            Log.Warn(
                "Asset sync: unknown location type '{0}' for location {1}, skipping",
                location.LocationType,
                location.LocationId);
            return false;
        }

        /// <summary>
        /// Merges asset cargo items into a colony matched by ColonyId.
        /// </summary>
        private bool MergeColonyLocation(
            AssetLocation location,
            ICollection<AssetCargoItem> cargoItems,
            List<Colony> localColonies)
        {
            if (localColonies == null)
            {
                Log.Debug("Asset sync: no local colonies available, skipping colony location {0}", location.LocationId);
                return false;
            }

            var colony = localColonies.FirstOrDefault(c => c.ColonyId == location.LocationId);
            if (colony == null)
            {
                Log.Debug(
                    "Asset sync: no local colony matches locationId={0} ({1}), skipping",
                    location.LocationId,
                    location.LocationName);
                return false;
            }

            return AssetMergeService.MergeColonyAssets(cargoItems, colony);
        }

        /// <summary>
        /// Merges asset cargo items into a station matched by GameLocationId.
        /// Creates a new station if no match is found.
        /// </summary>
        private bool MergeStationLocation(
            AssetLocation location,
            ICollection<AssetCargoItem> cargoItems,
            List<Station> localStations,
            string playerUUID)
        {
            if (localStations == null)
            {
                Log.Debug("Asset sync: no local stations available, skipping station location {0}", location.LocationId);
                return false;
            }

            var station = GetPlayerContext()?.FindStationByGameLocationId(location.LocationId);
            if (station == null)
            {
                string parsedName = ParseStationName(location.LocationName, location.SystemName);
                station = localStations.FirstOrDefault(s =>
                    (s.GameLocationId == null || s.GameLocationId == 0) &&
                    string.Equals(s.Name, parsedName, StringComparison.OrdinalIgnoreCase));

                if (station != null)
                {
                    station.GameLocationId = location.LocationId;
                    station.SystemName = location.SystemName;
                    station.SystemId = location.SystemId;
                    GetPlayerContext()?.IndexStationByGameLocationId(station);
                    Log.Info(
                        "Asset sync: adopted existing station '{0}' UUID={1} (set locationId={2})",
                        station.Name,
                        station.UUID,
                        location.LocationId);
                }
            }

            if (station == null)
            {
                station = new Station
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = ParseStationName(location.LocationName, location.SystemName),
                    GameLocationId = location.LocationId,
                    SystemName = location.SystemName,
                    SystemId = location.SystemId,
                };
                station.Holds[playerUUID] = new ItemBag();
                AddStation(station);
                localStations.Add(station);
                Log.Info(
                    "Asset sync: created new station '{0}' (locationId={1}) in system '{2}'",
                    location.LocationName,
                    location.LocationId,
                    location.SystemName);
            }

            ItemBag targetHold;
            if (!station.Holds.TryGetValue(playerUUID, out targetHold))
            {
                targetHold = new ItemBag();
                station.Holds[playerUUID] = targetHold;
            }

            return AssetMergeService.MergeStationAssets(cargoItems, station, targetHold);
        }

        /// <summary>
        /// Merges asset cargo items into a ship matched by GameLocationId.
        /// Creates a new ship if no match is found.
        /// </summary>
        private bool MergeShipLocation(
            AssetLocation location,
            ICollection<AssetCargoItem> cargoItems,
            List<Ship> localShips)
        {
            if (localShips == null)
            {
                Log.Debug("Asset sync: no local ships available, skipping ship location {0}", location.LocationId);
                return false;
            }

            var ship = GetPlayerContext()?.FindShipByGameLocationId(location.LocationId);
            if (ship == null)
            {
                string parsedName = ParseShipName(location.LocationName);
                ship = localShips.FirstOrDefault(s =>
                    (s.GameLocationId == null || s.GameLocationId == 0) &&
                    string.Equals(s.Name, parsedName, StringComparison.OrdinalIgnoreCase));

                if (ship != null)
                {
                    ship.GameLocationId = location.LocationId;
                    GetPlayerContext()?.IndexShipByGameLocationId(ship);
                    Log.Info(
                        "Asset sync: adopted existing ship '{0}' UUID={1} (set locationId={2})",
                        ship.Name,
                        ship.UUID,
                        location.LocationId);
                }
            }

            if (ship == null)
            {
                ship = new Ship
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = ParseShipName(location.LocationName),
                    GameLocationId = location.LocationId,
                };
                localShips.Add(ship);
                AddShip(ship);
                Log.Info(
                    "Asset sync: created new ship '{0}' (locationId={1})",
                    location.LocationName,
                    location.LocationId);
            }

            return AssetMergeService.MergeShipAssets(cargoItems, ship);
        }

        /// <summary>
        /// Finds a mutable colony by UUID in the local colony list.
        /// </summary>
        private Colony FindMutableColony(string colonyUUID, List<Colony> localColonies)
        {
            return localColonies.FirstOrDefault(c =>
                string.Equals(c.UUID, colonyUUID, StringComparison.Ordinal));
        }

        /// <summary>
        /// Converts a SecureString to a plain-text string for API calls.
        /// </summary>
        private string SecureStringToString(SecureString secureString)
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
        /// Handles connection monitor status changes.
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
