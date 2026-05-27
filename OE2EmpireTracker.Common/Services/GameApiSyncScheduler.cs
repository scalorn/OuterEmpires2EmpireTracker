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
using Newtonsoft.Json;
using NLog;
using OE2EmpireTracker.Client;
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

        private readonly GameApiClient _client;
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
        /// <param name="client">The game API client used for profile requests.</param>
        /// <param name="credentialManager">The credential manager for retrieving secrets.</param>
        /// <param name="connectionMonitor">The connection monitor for detecting connect/disconnect transitions.</param>
        /// <param name="appId">The registered application GUID.</param>
        /// <param name="clientId">The player's account identifier.</param>
        public GameApiSyncScheduler(
            GameApiClient client,
            GameApiCredentialManager credentialManager,
            GameApiConnectionMonitor connectionMonitor,
            string appId,
            string clientId)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
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
        /// Overwrites: Skills, Ranks (Public/Private/Military), SkillPoints, Faction, CitizenId.
        /// Preserves: SkillGroups (local-only UI preferences).
        /// Logs each conflict with field name, old value, new value, and strategy.
        /// </summary>
        /// <param name="local">The local player profile to update.</param>
        /// <param name="remote">The API response containing authoritative game data.</param>
        /// <returns>True if any fields were changed; otherwise false.</returns>
        internal static bool MergeProfileData(PlayerProfile local, GameApiProfileResponse remote)
        {
            if (local == null || remote == null)
            {
                return false;
            }

            bool changed = false;

            // Merge Faction
            if (remote.Faction != null && remote.Faction != local.Faction)
            {
                Log.Info(
                    "Profile merge conflict: Faction '{0}' -> '{1}' (strategy: API wins)",
                    local.Faction,
                    remote.Faction);
                local.Faction = remote.Faction;
                changed = true;
            }

            // Merge CitizenId
            if (remote.CitizenId != null && remote.CitizenId != local.CitizenId)
            {
                Log.Info(
                    "Profile merge conflict: CitizenId '{0}' -> '{1}' (strategy: API wins)",
                    local.CitizenId,
                    remote.CitizenId);
                local.CitizenId = remote.CitizenId;
                changed = true;
            }

            // Merge SkillPoints
            if (remote.SkillPoints != local.SkillPoints)
            {
                Log.Info(
                    "Profile merge conflict: SkillPoints '{0}' -> '{1}' (strategy: API wins)",
                    local.SkillPoints,
                    remote.SkillPoints);
                local.SkillPoints = remote.SkillPoints;
                changed = true;
            }

            // Merge CharacterId (Req 1, 9.1)
            if (remote.CharacterId != 0 && remote.CharacterId != local.CharacterId)
            {
                Log.Info(
                    "Profile merge conflict: CharacterId '{0}' -> '{1}' (strategy: API wins)",
                    local.CharacterId,
                    remote.CharacterId);
                local.CharacterId = remote.CharacterId;
                changed = true;
            }

            // Merge FirstName (Req 2, 9.2)
            if (remote.FirstName != null && remote.FirstName != local.FirstName)
            {
                Log.Info(
                    "Profile merge conflict: FirstName '{0}' -> '{1}' (strategy: API wins)",
                    local.FirstName,
                    remote.FirstName);
                local.FirstName = remote.FirstName;
                changed = true;
            }

            // Merge LastName (Req 2, 9.2)
            if (remote.LastName != null && remote.LastName != local.LastName)
            {
                Log.Info(
                    "Profile merge conflict: LastName '{0}' -> '{1}' (strategy: API wins)",
                    local.LastName,
                    remote.LastName);
                local.LastName = remote.LastName;
                changed = true;
            }

            // Merge ActiveTimeMinutes (Req 3, 9.3)
            if (remote.ActiveTimeMinutes != 0 || local.ActiveTimeMinutes != 0)
            {
                int value = remote.ActiveTimeMinutes < 0 ? 0 : remote.ActiveTimeMinutes;
                if (value != local.ActiveTimeMinutes)
                {
                    Log.Info(
                        "Profile merge conflict: ActiveTimeMinutes '{0}' -> '{1}' (strategy: API wins, clamped)",
                        local.ActiveTimeMinutes,
                        value);
                    local.ActiveTimeMinutes = value;
                    changed = true;
                }
            }

            // Merge Ranks
            if (remote.Ranks != null)
            {
                changed |= MergeRank(local.Public, remote.Ranks.Public, "Public");
                changed |= MergeRank(local.Private, remote.Ranks.Private, "Private");
                changed |= MergeRank(local.Military, remote.Ranks.Military, "Military");
            }

            // Merge Skills (overwrite levels from API, preserve TrainingStarted and CompletionTime)
            if (remote.Skills != null)
            {
                changed |= MergeSkills(local, remote.Skills, remote.SkillInTraining);
            }

            return changed;
        }

        /// <summary>
        /// Performs a sync for a single character by exchanging a token and calling GetCharacterAsync.
        /// Deserializes the response and merges game-authoritative fields into the local profile.
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
                var tokenResult = await _client.ExchangeTokenAsync(_appId, _clientId, secret).ConfigureAwait(false);
                if (!tokenResult.Success)
                {
                    if (tokenResult.ErrorMessage != null && tokenResult.ErrorMessage.Contains("401"))
                    {
                        Log.Warn("Profile sync for character {0}: credentials are invalid (HTTP 401), stopping polling", playerUUID);
                        _connectionMonitor.TransitionTo(
                            GameApiConnectionMonitor.ConnectionState.DisconnectedInvalidKey,
                            "Credentials are invalid (HTTP 401)");
                    }
                    else
                    {
                        Log.Warn("Profile sync for character {0}: token exchange failed: {1}", playerUUID, tokenResult.ErrorMessage);
                    }

                    return false;
                }

                // Use the token to fetch character data
                var result = await _client.GetCharacterAsync(_appId, tokenResult.Token.AccessToken).ConfigureAwait(false);
                if (result.Success)
                {
                    var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiProfileResponse>>(result.Json);
                    GameApiProfileResponse remoteProfile = envelope?.Data;
                    if (remoteProfile == null)
                    {
                        Log.Warn("Profile sync for character {0}: deserialized response was null", playerUUID);
                        return false;
                    }

                    var localProfile = GetPlayerProfile(playerUUID);
                    if (localProfile != null)
                    {
                        MergeProfileData(localProfile, remoteProfile);
                    }

                    Log.Info("Successfully synced profile for character {0}", playerUUID);

                    // Colony sync runs after profile sync succeeds (Req 5.1)
                    // Wrapped in try/catch so colony failures don't affect profile result (Req 5.2)
                    try
                    {
                        await SyncColoniesAsync(playerUUID, tokenResult.Token.AccessToken).ConfigureAwait(false);
                    }
                    catch (Exception colonyEx)
                    {
                        Log.Error(colonyEx, "Colony sync failed for character {0}, profile sync result preserved", playerUUID);
                    }

                    // Asset sync runs after colony sync (Req 8.1)
                    // Wrapped in try/catch so asset failures don't affect profile/colony sync (Req 8.4)
                    try
                    {
                        await SyncAssetsAsync(playerUUID, tokenResult.Token.AccessToken).ConfigureAwait(false);
                    }
                    catch (Exception assetEx)
                    {
                        Log.Error(assetEx, "Asset sync failed for character {0}, profile/colony sync results preserved", playerUUID);
                    }

                    return true;
                }

                // Handle HTTP 401 — token expired or invalid
                if (result.Json == "401")
                {
                    Log.Warn("Profile sync for character {0}: token rejected (HTTP 401), invalidating cache", playerUUID);
                    _client.InvalidateToken(_clientId, secret);
                    _connectionMonitor.TransitionTo(
                        GameApiConnectionMonitor.ConnectionState.DisconnectedInvalidKey,
                        "Credentials are invalid (HTTP 401)");
                    return false;
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
        /// Retrieves the local PlayerProfile for the given UUID.
        /// Override point for testing. Returns null if no profile is found.
        /// </summary>
        /// <param name="playerUUID">The player UUID to look up.</param>
        /// <returns>The local PlayerProfile, or null if not found.</returns>
        internal virtual PlayerProfile GetPlayerProfile(string playerUUID)
        {
            // Default implementation returns null — wired to PlayerContext in production via GameApiContext
            return null;
        }

        /// <summary>
        /// Retrieves the mutable colony list for the given player UUID.
        /// Override point for testing. Returns null if no colonies are found.
        /// </summary>
        /// <param name="playerUUID">The player UUID to look up.</param>
        /// <returns>The mutable colony list, or null if not found.</returns>
        internal virtual List<Colony> GetPlayerColonies(string playerUUID)
        {
            // Default implementation returns null — wired to PlayerContext in production via GameApiContext
            return null;
        }

        /// <summary>
        /// Persists the current player context to disk.
        /// Override point for testing. In production, calls PlayerContext.WriteContext().
        /// </summary>
        internal virtual void WriteContext()
        {
            // Default implementation is a no-op — wired to PlayerContext in production via GameApiContext
        }

        /// <summary>
        /// Raises the ColonyDataChanged event to notify UI subscribers.
        /// Override point for testing. In production, calls PlayerContext.OnColonyDataChanged.
        /// </summary>
        internal virtual void RaiseColonyDataChanged()
        {
            // Default implementation is a no-op — wired to PlayerContext in production via GameApiContext
        }

        /// <summary>
        /// Retrieves the mutable station list for the given player UUID.
        /// Override point for testing. Returns null if no stations are found.
        /// </summary>
        /// <param name="playerUUID">The player UUID to look up.</param>
        /// <returns>The mutable station list, or null if not found.</returns>
        internal virtual List<Station> GetPlayerStations(string playerUUID)
        {
            // Default implementation returns null — wired to PlayerContext in production via GameApiContext
            return null;
        }

        /// <summary>
        /// Retrieves the mutable ship list for the given player UUID.
        /// Override point for testing. Returns null if no ships are found.
        /// </summary>
        /// <param name="playerUUID">The player UUID to look up.</param>
        /// <returns>The mutable ship list, or null if not found.</returns>
        internal virtual List<Ship> GetPlayerShips(string playerUUID)
        {
            // Default implementation returns null — wired to PlayerContext in production via GameApiContext
            return null;
        }

        /// <summary>
        /// Raises the AssetDataChanged event to notify UI subscribers.
        /// Override point for testing. In production, calls PlayerContext.OnAssetDataChanged.
        /// </summary>
        internal virtual void RaiseAssetDataChanged()
        {
            // Default implementation is a no-op — wired to PlayerContext in production via GameApiContext
        }

        /// <summary>
        /// Fetches the colony list from the game API and merges it into local data.
        /// Called after profile sync succeeds. Handles 401 (token invalid) and 403 (scope missing).
        /// Colony sync failures are logged but do not affect the profile sync result.
        /// </summary>
        /// <param name="playerUUID">The player UUID whose colonies to sync.</param>
        /// <param name="accessToken">The Bearer access token from token exchange.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        internal async Task SyncColoniesAsync(string playerUUID, string accessToken)
        {
            Log.Info("Colony sync starting for character {0}", playerUUID);

            // 1. Fetch colony list (Req 5.1)
            var listResult = await _client.GetColonyListAsync(_appId, accessToken).ConfigureAwait(false);

            if (!listResult.Success)
            {
                // Handle HTTP 401 — same as profile 401 (Req 5.6)
                if (listResult.Json == "401")
                {
                    Log.Warn("Colony sync for character {0}: token rejected (HTTP 401), invalidating", playerUUID);
                    _connectionMonitor.TransitionTo(
                        GameApiConnectionMonitor.ConnectionState.DisconnectedInvalidKey,
                        "Credentials are invalid (HTTP 401)");
                    return;
                }

                // Handle HTTP 403 — scope not granted (Req 5.5, 13.4)
                if (listResult.Json == "403")
                {
                    Log.Info("Colony sync skipped: colony.list.read scope not granted");
                    return;
                }

                Log.Warn("Colony sync failed: could not fetch colony list for character {0}", playerUUID);
                return;
            }

            // 2. Deserialize — fail-fast before mutation (Req 5.3, 15.5)
            GameApiColonyListResponse colonyListResponse;
            try
            {
                var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiColonyListResponse>>(listResult.Json);
                colonyListResponse = envelope?.Data;
                if (colonyListResponse == null)
                {
                    Log.Warn("Colony sync for character {0}: deserialized response was null", playerUUID);
                    return;
                }
            }
            catch (JsonException ex)
            {
                string truncated = listResult.Json?.Length > 500
                    ? listResult.Json.Substring(0, 500)
                    : listResult.Json;
                Log.Error(ex, "Colony sync: malformed JSON response: {0}", truncated);
                return;
            }

            // Req 5.7 — log count received
            Log.Info(
                "Received {0} colonies from game API for character {1}",
                colonyListResponse.Colonies.Count,
                playerUUID);

            // 3. Merge colony list (Req 5.3, 13.1)
            var localColonies = GetPlayerColonies(playerUUID);
            if (localColonies == null)
            {
                Log.Warn("Colony sync for character {0}: no local colony list available", playerUUID);
                return;
            }

            var mergeResult = ColonyMergeService.MergeColonyList(
                colonyListResponse.Colonies,
                localColonies,
                playerUUID);

            // 4. Fetch per-colony details — buildings + warehouse (Req 5.4, 5.5, 14.2, 14.3, 14.4)
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

                // Buildings (Req 14.2, 13.6)
                if (buildingsScopeAvailable)
                {
                    var buildingsResult = await _client.GetColonyBuildingsAsync(_appId, accessToken, apiColony.ColonyId).ConfigureAwait(false);
                    if (buildingsResult.Success)
                    {
                        try
                        {
                            var bEnvelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiColonyBuildingsResponse>>(buildingsResult.Json);
                            if (bEnvelope?.Data?.Buildings != null)
                            {
                                bool buildingsChanged = ColonyMergeService.MergeBuildings(bEnvelope.Data.Buildings, colony);
                                if (buildingsChanged && mergeResult.Updated == 0)
                                {
                                    mergeResult.Updated++;
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            Log.Error(ex, "Colony sync: malformed buildings JSON for colonyId={0}", apiColony.ColonyId);
                        }
                    }
                    else if (buildingsResult.Json == "403")
                    {
                        buildingsScopeAvailable = false;
                        Log.Info("Colony sync: colony.buildings.read scope not available, skipping buildings for all colonies");
                    }
                    else if (buildingsResult.Json != "404")
                    {
                        Log.Warn("Colony sync: buildings fetch failed for colonyId={0}", apiColony.ColonyId);
                    }
                }

                // Warehouse (Req 14.3, 13.6)
                if (warehouseScopeAvailable)
                {
                    var warehouseResult = await _client.GetColonyWarehouseAsync(_appId, accessToken, apiColony.ColonyId).ConfigureAwait(false);
                    if (warehouseResult.Success)
                    {
                        try
                        {
                            var wEnvelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiColonyWarehouseResponse>>(warehouseResult.Json);
                            if (wEnvelope?.Data?.Contents != null)
                            {
                                bool warehouseChanged = ColonyMergeService.MergeWarehouse(wEnvelope.Data.Contents, colony);
                                if (warehouseChanged && mergeResult.Updated == 0)
                                {
                                    mergeResult.Updated++;
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            Log.Error(ex, "Colony sync: malformed warehouse JSON for colonyId={0}", apiColony.ColonyId);
                        }
                    }
                    else if (warehouseResult.Json == "403")
                    {
                        warehouseScopeAvailable = false;
                        Log.Info("Colony sync: colony.warehouse.read scope not available, skipping warehouse for all colonies");
                    }
                    else if (warehouseResult.Json != "404")
                    {
                        Log.Warn("Colony sync: warehouse fetch failed for colonyId={0}", apiColony.ColonyId);
                    }
                }

                // Workers (Req 14.1, 14.2, 14.3, 14.4)
                if (workersScopeAvailable)
                {
                    var workersResult = await _client.GetColonyWorkersAsync(_appId, accessToken, apiColony.ColonyId).ConfigureAwait(false);
                    if (workersResult.Success)
                    {
                        try
                        {
                            var wkEnvelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiColonyWorkersResponse>>(workersResult.Json);
                            if (wkEnvelope?.Data != null)
                            {
                                bool workersChanged = ColonyMergeService.MergeWorkers(wkEnvelope.Data, colony);
                                if (workersChanged && mergeResult.Updated == 0)
                                {
                                    mergeResult.Updated++;
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            Log.Error(ex, "Colony sync: malformed workers JSON for colonyId={0}", apiColony.ColonyId);
                        }
                    }
                    else if (workersResult.Json == "403")
                    {
                        workersScopeAvailable = false;
                        Log.Info("Colony sync: colony.workers.read scope not available, skipping workers for all colonies");
                    }
                    else if (workersResult.Json != "404")
                    {
                        Log.Warn("Colony sync: workers fetch failed for colonyId={0}", apiColony.ColonyId);
                    }
                }
            }

            // 5. Persist and notify (Req 11.1, 11.2, 11.3, 12.1, 12.3)
            if (mergeResult.HasChanges)
            {
                WriteContext();
                RaiseColonyDataChanged();
            }

            // Req 5.7 — log merge outcome
            Log.Info(
                "Colony sync complete for character {0}: {1} created, {2} updated, {3} skipped",
                playerUUID,
                mergeResult.Created,
                mergeResult.Updated,
                mergeResult.Skipped);
        }

        /// <summary>
        /// Fetches asset locations from the game API, iterates each location with assets,
        /// retrieves cargo details, and merges into the appropriate local model (colony, station, or ship).
        /// Handles 401 (invalidate credentials + abort), 403 (log + skip), 404 (log + skip location),
        /// malformed JSON (log + skip), and circuit breaker open (log + abort).
        /// </summary>
        /// <param name="playerUUID">The player UUID whose assets to sync.</param>
        /// <param name="accessToken">The Bearer access token from token exchange.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        internal virtual async Task SyncAssetsAsync(string playerUUID, string accessToken)
        {
            Log.Info("Asset sync starting for character {0}", playerUUID);

            int locationsSynced = 0;
            int itemsProcessed = 0;
            int errorsEncountered = 0;

            // 1. Fetch asset locations list
            var listResult = await _client.GetAssetLocationsAsync(_appId, accessToken).ConfigureAwait(false);

            if (!listResult.Success)
            {
                if (listResult.Json == "401")
                {
                    Log.Warn("Asset sync for character {0}: token rejected (HTTP 401), invalidating", playerUUID);
                    _connectionMonitor.TransitionTo(
                        GameApiConnectionMonitor.ConnectionState.DisconnectedInvalidKey,
                        "Credentials are invalid (HTTP 401)");
                    return;
                }

                if (listResult.Json == "403")
                {
                    Log.Info("Asset sync skipped: assets.locations.read scope not granted");
                    return;
                }

                Log.Warn("Asset sync failed: could not fetch asset locations for character {0}", playerUUID);
                errorsEncountered++;
                Log.Info(
                    "Asset sync complete for character {0}: {1} locations synced, {2} items processed, {3} errors",
                    playerUUID,
                    locationsSynced,
                    itemsProcessed,
                    errorsEncountered);
                return;
            }

            // 2. Deserialize locations list
            GameApiAssetLocationsResponse locationsResponse;
            try
            {
                var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiAssetLocationsResponse>>(listResult.Json);
                locationsResponse = envelope?.Data;
                if (locationsResponse == null)
                {
                    Log.Warn("Asset sync for character {0}: deserialized locations response was null", playerUUID);
                    return;
                }
            }
            catch (JsonException ex)
            {
                string truncated = listResult.Json?.Length > 500
                    ? listResult.Json.Substring(0, 500)
                    : listResult.Json;
                Log.Error(ex, "Asset sync: malformed JSON in locations response: {0}", truncated);
                return;
            }

            // 3. Filter to locations with assetCount > 0
            var activeLocations = locationsResponse.Locations.Where(l => l.AssetCount > 0).ToList();
            Log.Info(
                "Asset sync: {0} total locations, {1} with assets for character {2}",
                locationsResponse.Locations.Count,
                activeLocations.Count,
                playerUUID);

            // Get local data references
            var localColonies = GetPlayerColonies(playerUUID);
            var localStations = GetPlayerStations(playerUUID);
            var localShips = GetPlayerShips(playerUUID);
            bool anyChanges = false;

            // 4. Iterate each location with assets
            foreach (var location in activeLocations)
            {
                try
                {
                    var detailResult = await _client.GetAssetLocationDetailAsync(
                        _appId,
                        accessToken,
                        location.LocationId,
                        location.LocationType).ConfigureAwait(false);

                    if (!detailResult.Success)
                    {
                        if (detailResult.Json == "401")
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

                        if (detailResult.Json == "403")
                        {
                            Log.Info(
                                "Asset sync: location {0} ({1}) returned HTTP 403, skipping",
                                location.LocationId,
                                location.LocationName);
                            errorsEncountered++;
                            continue;
                        }

                        if (detailResult.Json == "404")
                        {
                            Log.Warn(
                                "Asset sync: location {0} ({1}) returned HTTP 404, skipping",
                                location.LocationId,
                                location.LocationName);
                            errorsEncountered++;
                            continue;
                        }

                        Log.Warn(
                            "Asset sync: failed to fetch detail for location {0} ({1}), skipping",
                            location.LocationId,
                            location.LocationName);
                        errorsEncountered++;
                        continue;
                    }

                    // Deserialize detail response
                    GameApiAssetDetailResponse detailResponse;
                    try
                    {
                        var detailEnvelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiAssetDetailResponse>>(detailResult.Json);
                        detailResponse = detailEnvelope?.Data;
                        if (detailResponse == null)
                        {
                            Log.Warn(
                                "Asset sync: deserialized detail response was null for location {0}, skipping",
                                location.LocationId);
                            errorsEncountered++;
                            continue;
                        }
                    }
                    catch (JsonException ex)
                    {
                        string truncated = detailResult.Json?.Length > 500
                            ? detailResult.Json.Substring(0, 500)
                            : detailResult.Json;
                        Log.Error(
                            ex,
                            "Asset sync: malformed JSON in detail response for location {0}: {1}",
                            location.LocationId,
                            truncated);
                        errorsEncountered++;
                        continue;
                    }

                    // Route to appropriate merge method based on location type
                    bool merged = RouteAssetMerge(
                        location,
                        detailResponse.Cargo,
                        localColonies,
                        localStations,
                        localShips);

                    if (merged)
                    {
                        anyChanges = true;
                    }

                    locationsSynced++;
                    itemsProcessed += detailResponse.Cargo.Count;
                }
                catch (BrokenCircuitException)
                {
                    Log.Warn("Asset sync: circuit breaker open, aborting asset sync cycle");
                    errorsEncountered++;
                    break;
                }
            }

            // 5. Persist and notify if changes occurred
            if (anyChanges)
            {
                WriteContext();
                RaiseAssetDataChanged();
            }

            // 6. Log summary
            Log.Info(
                "Asset sync complete for character {0}: {1} locations synced, {2} items processed, {3} errors",
                playerUUID,
                locationsSynced,
                itemsProcessed,
                errorsEncountered);
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
        /// Routes asset cargo items to the appropriate merge method based on location type.
        /// For colonies, matches by ColonyId. For stations, matches by GameLocationId (creates if not found).
        /// For ships, matches by GameLocationId (creates if not found).
        /// </summary>
        /// <param name="location">The asset location entry with type and ID information.</param>
        /// <param name="cargoItems">The list of cargo items to merge.</param>
        /// <param name="localColonies">The local colony list (may be null).</param>
        /// <param name="localStations">The local station list (may be null).</param>
        /// <param name="localShips">The local ship list (may be null).</param>
        /// <returns>True if any changes were made; false otherwise.</returns>
        private static bool RouteAssetMerge(
            GameApiAssetLocationEntry location,
            List<GameApiAssetCargoItem> cargoItems,
            List<Colony> localColonies,
            List<Station> localStations,
            List<Ship> localShips)
        {
            if (string.Equals(location.LocationType, "Co", StringComparison.OrdinalIgnoreCase))
            {
                return MergeColonyLocation(location, cargoItems, localColonies);
            }

            if (string.Equals(location.LocationType, "St", StringComparison.OrdinalIgnoreCase))
            {
                return MergeStationLocation(location, cargoItems, localStations);
            }

            if (string.Equals(location.LocationType, "Sh", StringComparison.OrdinalIgnoreCase))
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
        /// <param name="location">The asset location entry.</param>
        /// <param name="cargoItems">The cargo items to merge.</param>
        /// <param name="localColonies">The local colony list.</param>
        /// <returns>True if any changes were made; false otherwise.</returns>
        private static bool MergeColonyLocation(
            GameApiAssetLocationEntry location,
            List<GameApiAssetCargoItem> cargoItems,
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
        /// <param name="location">The asset location entry.</param>
        /// <param name="cargoItems">The cargo items to merge.</param>
        /// <param name="localStations">The local station list.</param>
        /// <returns>True if any changes were made; false otherwise.</returns>
        private static bool MergeStationLocation(
            GameApiAssetLocationEntry location,
            List<GameApiAssetCargoItem> cargoItems,
            List<Station> localStations)
        {
            if (localStations == null)
            {
                Log.Debug("Asset sync: no local stations available, skipping station location {0}", location.LocationId);
                return false;
            }

            var station = localStations.FirstOrDefault(s => s.GameLocationId == location.LocationId);
            if (station == null)
            {
                station = new Station
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = location.LocationName,
                    GameLocationId = location.LocationId,
                    SystemName = location.SystemName,
                    SystemId = location.SystemId,
                };
                station.Holds[AssetMergeService.DefaultHoldName] = new ItemBag();
                localStations.Add(station);
                Log.Info(
                    "Asset sync: created new station '{0}' (locationId={1}) in system '{2}'",
                    location.LocationName,
                    location.LocationId,
                    location.SystemName);
            }

            ItemBag targetHold;
            if (!station.Holds.TryGetValue(AssetMergeService.DefaultHoldName, out targetHold))
            {
                targetHold = new ItemBag();
                station.Holds[AssetMergeService.DefaultHoldName] = targetHold;
            }

            return AssetMergeService.MergeStationAssets(cargoItems, station, targetHold);
        }

        /// <summary>
        /// Merges asset cargo items into a ship matched by GameLocationId.
        /// Creates a new ship if no match is found.
        /// </summary>
        /// <param name="location">The asset location entry.</param>
        /// <param name="cargoItems">The cargo items to merge.</param>
        /// <param name="localShips">The local ship list.</param>
        /// <returns>True if any changes were made; false otherwise.</returns>
        private static bool MergeShipLocation(
            GameApiAssetLocationEntry location,
            List<GameApiAssetCargoItem> cargoItems,
            List<Ship> localShips)
        {
            if (localShips == null)
            {
                Log.Debug("Asset sync: no local ships available, skipping ship location {0}", location.LocationId);
                return false;
            }

            var ship = localShips.FirstOrDefault(s => s.GameLocationId == location.LocationId);
            if (ship == null)
            {
                ship = new Ship
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = location.LocationName,
                    GameLocationId = location.LocationId,
                };
                localShips.Add(ship);
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
        /// <param name="colonyUUID">The UUID of the colony to find.</param>
        /// <param name="localColonies">The local colony list to search.</param>
        /// <returns>The colony if found; otherwise null.</returns>
        private static Colony FindMutableColony(string colonyUUID, List<Colony> localColonies)
        {
            return localColonies.FirstOrDefault(c =>
                string.Equals(c.UUID, colonyUUID, StringComparison.Ordinal));
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
        /// Merges a single rank from the API response into the local rank.
        /// </summary>
        /// <param name="localRank">The local rank to update.</param>
        /// <param name="remoteRank">The API rank response.</param>
        /// <param name="rankName">The rank category name for logging.</param>
        /// <returns>True if any rank fields were changed.</returns>
        private static bool MergeRank(PlayerRank localRank, GameApiRankResponse remoteRank, string rankName)
        {
            if (localRank == null || remoteRank == null)
            {
                return false;
            }

            bool changed = false;

            if (remoteRank.Level != localRank.Rank)
            {
                Log.Info(
                    "Profile merge conflict: {0}.Rank '{1}' -> '{2}' (strategy: API wins)",
                    rankName,
                    localRank.Rank,
                    remoteRank.Level);
                localRank.Rank = remoteRank.Level;
                changed = true;
            }

            if (remoteRank.LevelName != null && remoteRank.LevelName != localRank.RankName)
            {
                Log.Info(
                    "Profile merge conflict: {0}.RankName '{1}' -> '{2}' (strategy: API wins)",
                    rankName,
                    localRank.RankName,
                    remoteRank.LevelName);
                localRank.RankName = remoteRank.LevelName;
                changed = true;
            }

            if (remoteRank.XpToNextLevel != localRank.XpToNextLevel)
            {
                Log.Info(
                    "Profile merge conflict: {0}.XpToNextLevel '{1}' -> '{2}' (strategy: API wins)",
                    rankName,
                    localRank.XpToNextLevel,
                    remoteRank.XpToNextLevel);
                localRank.XpToNextLevel = remoteRank.XpToNextLevel;
                changed = true;
            }

            if (remoteRank.CurrentXp != localRank.CurrentXp)
            {
                Log.Info(
                    "Profile merge conflict: {0}.CurrentXp '{1}' -> '{2}' (strategy: API wins)",
                    rankName,
                    localRank.CurrentXp,
                    remoteRank.CurrentXp);
                localRank.CurrentXp = remoteRank.CurrentXp;
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Merges skills from the API response into the local profile.
        /// Updates skill levels, metadata, and training progress from the API
        /// while preserving local-only fields (TrainingStarted, CompletionTime).
        /// </summary>
        /// <param name="local">The local player profile.</param>
        /// <param name="remoteSkills">The skills dictionary from the API response.</param>
        /// <param name="skillInTraining">The skill currently in training, or null if none.</param>
        /// <returns>True if any skill fields were changed.</returns>
        private static bool MergeSkills(
            PlayerProfile local,
            Dictionary<string, GameApiSkillResponse> remoteSkills,
            GameApiSkillInTrainingResponse skillInTraining)
        {
            bool changed = false;

            foreach (var kvp in remoteSkills)
            {
                string skillName = kvp.Key;
                GameApiSkillResponse remoteSkill = kvp.Value;
                PlayerSkill localSkill = local.GetSkill(skillName);

                if (remoteSkill.Level != localSkill.Level)
                {
                    Log.Info(
                        "Profile merge conflict: Skills[{0}].Level '{1}' -> '{2}' (strategy: API wins)",
                        skillName,
                        localSkill.Level,
                        remoteSkill.Level);
                    localSkill.Level = remoteSkill.Level;
                    changed = true;
                }

                // Merge metadata (Req 6, 9.6)
                if (remoteSkill.SkillId != localSkill.SkillId)
                {
                    localSkill.SkillId = remoteSkill.SkillId;
                    changed = true;
                }

                string effectDesc = remoteSkill.EffectDescription ?? string.Empty;
                if (effectDesc != localSkill.EffectDescription)
                {
                    localSkill.EffectDescription = effectDesc;
                    changed = true;
                }

                if (remoteSkill.AmountPerLevel != localSkill.AmountPerLevel)
                {
                    localSkill.AmountPerLevel = remoteSkill.AmountPerLevel;
                    changed = true;
                }

                string groupName = remoteSkill.SkillGroupName ?? string.Empty;
                if (groupName != localSkill.SkillGroupName)
                {
                    localSkill.SkillGroupName = groupName;
                    changed = true;
                }

                if (remoteSkill.IsUnlocked != localSkill.IsUnlocked)
                {
                    localSkill.IsUnlocked = remoteSkill.IsUnlocked;
                    changed = true;
                }

                // Merge training progress (Req 7, 9.7, 9.8)
                bool isTraining = skillInTraining != null &&
                    string.Equals(skillInTraining.SkillName, skillName, StringComparison.OrdinalIgnoreCase);

                int targetLevel = isTraining ? skillInTraining.TargetLevel : 0;
                int pctComplete = isTraining ? skillInTraining.TrainingPercentageComplete : 0;
                int remainingMin = isTraining ? skillInTraining.RemainingMinutes : 0;

                if (targetLevel != localSkill.TargetLevel)
                {
                    localSkill.TargetLevel = targetLevel;
                    changed = true;
                }

                if (pctComplete != localSkill.TrainingPercentageComplete)
                {
                    localSkill.TrainingPercentageComplete = pctComplete;
                    changed = true;
                }

                if (remainingMin != localSkill.RemainingMinutes)
                {
                    localSkill.RemainingMinutes = remainingMin;
                    changed = true;
                }
            }

            // Reset training fields for skills NOT in remoteSkills (Req 7.3, 7.4)
            foreach (var kvp in local.Skills)
            {
                if (!remoteSkills.ContainsKey(kvp.Key))
                {
                    PlayerSkill skill = kvp.Value;
                    if (skill.TargetLevel != 0 || skill.TrainingPercentageComplete != 0 || skill.RemainingMinutes != 0)
                    {
                        skill.TargetLevel = 0;
                        skill.TrainingPercentageComplete = 0;
                        skill.RemainingMinutes = 0;
                        changed = true;
                    }
                }
            }

            return changed;
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
