// <copyright file="GameApiSyncScheduler.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;

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
