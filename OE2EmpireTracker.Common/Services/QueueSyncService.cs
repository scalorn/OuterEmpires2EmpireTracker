// <copyright file="QueueSyncService.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Central orchestrator for queue-based background sync.
    /// Replaces sequential API calls with parallel dispatch via GameApiRequestQueue.
    /// </summary>
    public class QueueSyncService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;
        private readonly EmpireContext _empireContext;
        private readonly GameApiClient _apiClient;
        private readonly GameApiConnectionSettings _settings;
        private readonly GameApiCredentialManager _credentialManager;

        private readonly object _syncLock = new object();

        private string _currentAccessToken = string.Empty;
        private TokenRefreshHandler _tokenRefreshHandler;
        private GameApiRequestQueue _currentQueue;

        private volatile bool _isSyncRunning;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueSyncService"/> class.
        /// </summary>
        /// <param name="playerContext">The player context for data persistence.</param>
        /// <param name="empireContext">The empire context for shared game data.</param>
        /// <param name="apiClient">The game API client for HTTP communication.</param>
        /// <param name="settings">The connection settings (TPS, AppId, etc.).</param>
        /// <param name="credentialManager">The credential manager for token refresh operations.</param>
        public QueueSyncService(
            PlayerContext playerContext,
            EmpireContext empireContext,
            GameApiClient apiClient,
            GameApiConnectionSettings settings,
            GameApiCredentialManager credentialManager)
        {
            _playerContext = playerContext;
            _empireContext = empireContext;
            _apiClient = apiClient;
            _settings = settings;
            _credentialManager = credentialManager;
        }

        /// <summary>
        /// Gets a value indicating whether a sync cycle is currently running.
        /// </summary>
        public bool IsSyncRunning => _isSyncRunning;

        /// <summary>
        /// Runs a single sync cycle. Returns immediately if a cycle is already in progress.
        /// </summary>
        /// <param name="ct">Cancellation token for cooperative cancellation.</param>
        /// <returns>The sync result with success/failure counts and elapsed time.</returns>
        public async Task<QueueSyncResult> RunSyncAsync(CancellationToken ct = default)
        {
            lock (_syncLock)
            {
                if (_isSyncRunning)
                {
                    Log.Debug("Sync cycle already in progress, skipping.");
                    return new QueueSyncResult();
                }

                _isSyncRunning = true;
            }

            try
            {
                Log.Info("Queue sync cycle started.");

                // Exchange token at the start of each sync cycle (same pattern as GameApiSyncScheduler).
                string playerUUID = _playerContext.CurrentPlayerUUID;
                var secret = _credentialManager.GetKey(playerUUID);
                if (secret == null)
                {
                    Log.Warn("QueueSyncService: no credential for player {0}, aborting sync.", playerUUID);
                    return new QueueSyncResult();
                }

                string plainSecret = SecureStringToPlain(secret);
                var tokenResult = await _apiClient.ExchangeTokenAsync(
                    _settings.AppId, _settings.ClientId, plainSecret).ConfigureAwait(false);

                if (!tokenResult.Success)
                {
                    Log.Error("QueueSyncService: token exchange failed: {0}", tokenResult.ErrorMessage);
                    return new QueueSyncResult();
                }

                _currentAccessToken = tokenResult.Token.AccessToken;
                Log.Info("QueueSyncService: token exchange succeeded, starting dispatch.");

                _tokenRefreshHandler = new TokenRefreshHandler(
                    _apiClient,
                    _settings,
                    _credentialManager,
                    playerUUID,
                    _currentAccessToken);

                var metricsFilePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "sync-metrics.csv");

                var queue = new GameApiRequestQueue(
                    _settings.Tps,
                    perItemTimeout: null,
                    maxInflightMultiplier: 3,
                    maxRetries: 3,
                    metricsFilePath: metricsFilePath);

                _currentQueue = queue;

                await queue.EnqueueAsync(CreateCharacterProfileItem()).ConfigureAwait(false);
                await queue.EnqueueAsync(CreateCharacterSkillsItem()).ConfigureAwait(false);
                await queue.EnqueueAsync(CreateBankingBalanceItem()).ConfigureAwait(false);
                await queue.EnqueueAsync(CreateBankingTransactionsItem()).ConfigureAwait(false);
                await queue.EnqueueAsync(CreateAcceptedJobsItem()).ConfigureAwait(false);
                await queue.EnqueueAsync(CreateShipConfigItem()).ConfigureAwait(false);
                await queue.EnqueueAsync(CreateShipCargoItem()).ConfigureAwait(false);
                await queue.EnqueueAsync(CreateMarketListingsItem()).ConfigureAwait(false);
                await queue.EnqueueAsync(CreateMarketItemsItem()).ConfigureAwait(false);
                await queue.EnqueueAsync(CreateMarketBuyOrdersItem()).ConfigureAwait(false);
                await queue.EnqueueAsync(CreateMarketSellOrdersItem()).ConfigureAwait(false);
                await queue.EnqueueAsync(CreateColonyListItem()).ConfigureAwait(false);
                await queue.EnqueueAsync(CreateAssetLocationsItem()).ConfigureAwait(false);
                await queue.EnqueueAsync(CreateKillMailListItem()).ConfigureAwait(false);
                await queue.EnqueueAsync(CreateMailListItem()).ConfigureAwait(false);

                var sw = Stopwatch.StartNew();
                queue.Start(ct);
                await queue.DrainAsync();
                sw.Stop();

                var status = queue.CompletionStatus;
                int succeeded = status.Succeeded;
                int failed = status.Failed;
                var elapsed = sw.Elapsed;

                Log.Info(
                    "Sync complete: {0} succeeded, {1} failed, elapsed {2:F1}s",
                    succeeded,
                    failed,
                    elapsed.TotalSeconds);

                var failedLabels = new List<string>();
                foreach (var error in queue.Errors)
                {
                    Log.Warn(
                        "Failed work item '{0}': {1}",
                        error.WorkItemLabel,
                        error.Exception.Message);
                    failedLabels.Add(error.WorkItemLabel);
                }

                _playerContext.WriteContext();

                return new QueueSyncResult
                {
                    Succeeded = succeeded,
                    Failed = failed,
                    Elapsed = elapsed,
                    FailedLabels = failedLabels,
                };
            }
            finally
            {
                _isSyncRunning = false;
            }
        }

        /// <summary>
        /// Constructs a temporary Survey from the game API survey detail response fields.
        /// </summary>
        /// <param name="detail">The parsed survey detail from the API.</param>
        /// <param name="planetName">The planet name from the parent asset location context.</param>
        /// <param name="systemName">The star system name from the parent asset location context.</param>
        /// <returns>A temporary Survey populated with the API response data.</returns>
        private static Survey BuildTempSurveyFromDetail(GameApiSurveyDetail detail, string planetName, string systemName)
        {
            var temp = new Survey
            {
                PlanetName = planetName,
                SystemName = systemName,
                SurveyID = detail.EncryptedId,
                ScannedBy = detail.ScanCharacter,
                DateTime = detail.ScanDate.ToString("o"),
                SurveyType = MapObjectTypeToSurveyType(detail.ObjectType),
            };

            var resources = new Dictionary<string, SurveyResource>();
            var maxReserves = new Dictionary<string, int>();

            foreach (var res in detail.Resources)
            {
                if (string.IsNullOrEmpty(res.ResourceName))
                {
                    continue;
                }

                resources[res.ResourceName] = new SurveyResource(
                    res.ResourceName,
                    res.RarityClassification,
                    res.Abundance.ToString());

                if (res.MaxReserve.HasValue)
                {
                    maxReserves[res.ResourceName] = res.MaxReserve.Value;
                }
            }

            temp.Resources = resources;
            temp.ParsedMaxReserves = maxReserves.Count > 0 ? maxReserves : null;

            return temp;
        }

        /// <summary>
        /// Maps the game API objectType string to the local SurveyType enum.
        /// </summary>
        /// <param name="objectType">The object type string from the API (e.g. "planet", "asteroid").</param>
        /// <returns>The corresponding SurveyType enum value.</returns>
        private static SurveyType MapObjectTypeToSurveyType(string objectType)
        {
            if (string.Equals(objectType, "asteroid", StringComparison.OrdinalIgnoreCase))
            {
                return SurveyType.Asteroid;
            }

            return SurveyType.Planet;
        }

        /// <summary>
        /// Converts a game API partTypeIcon code (e.g. "A22") to the CSS sprite position
        /// string (e.g. "-24px -822px") used by BaselineData BlueprintType IconPosition.
        /// The sprite sheet uses a 38px grid with a 24px offset for column 0.
        /// Column is determined by the leading letter (A=0, B=1, C=2, ...).
        /// Row is determined by the trailing digits.
        /// Returns null if the code cannot be parsed.
        /// </summary>
        /// <param name="partTypeIcon">The icon code from the API (e.g. "A22", "B20", "C20").</param>
        /// <returns>The CSS sprite position string, or null if the code is invalid.</returns>
        private static string ConvertPartTypeIconToPosition(string partTypeIcon)
        {
            if (string.IsNullOrEmpty(partTypeIcon) || partTypeIcon.Length < 2)
            {
                return null;
            }

            char letter = char.ToUpperInvariant(partTypeIcon[0]);
            if (letter < 'A' || letter > 'Z')
            {
                return null;
            }

            string rowStr = partTypeIcon.Substring(1);
            if (!int.TryParse(rowStr, out int row))
            {
                return null;
            }

            int column = letter - 'A';
            int x = -((column * 38) + 24);
            int y = -((row * 38) - 14);

            return x + "px " + y + "px";
        }

        /// <summary>
        /// Converts a SecureString to a plain string. Zeroes the unmanaged memory after use.
        /// </summary>
        /// <param name="secureString">The secure string to convert.</param>
        /// <returns>The plain text value.</returns>
        private static string SecureStringToPlain(SecureString secureString)
        {
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

                secureString.Dispose();
            }
        }

        /// <summary>
        /// Truncates a JSON string for safe inclusion in log messages.
        /// Prevents log flooding from large API response bodies.
        /// </summary>
        /// <param name="json">The JSON string to truncate, or null.</param>
        /// <param name="maxLength">The maximum number of characters to retain (default 500).</param>
        /// <returns>The truncated string, or "(null)" if the input is null.</returns>
        private static string TruncateForLog(string json, int maxLength = 500)
        {
            if (json == null)
            {
                return "(null)";
            }

            if (json.Length <= maxLength)
            {
                return json;
            }

            return json.Substring(0, maxLength) + "...(truncated)";
        }

        /// <summary>
        /// Builds a temporary Blueprint populated with API response properties for use
        /// as the scoring template in <see cref="BlueprintService.FindBestMatch"/>.
        /// </summary>
        /// <param name="response">The API blueprint detail response.</param>
        /// <returns>A temporary Blueprint with Name, Evolution, and Properties set.</returns>
        private static Blueprint BuildScoringTemplate(GameApiBlueprintDetailResponse response)
        {
            var bpInfo = response.Blueprint;
            var temp = new Blueprint(bpInfo.Name)
            {
                Evolution = bpInfo.Evolution,
                BluePrintType = bpInfo.Type ?? string.Empty,
            };

            if (response.BlueprintProperties != null)
            {
                foreach (var prop in response.BlueprintProperties)
                {
                    string key = !string.IsNullOrEmpty(prop.FriendlyPropertyName)
                        ? prop.FriendlyPropertyName
                        : prop.PropertyName;
                    string value = string.IsNullOrEmpty(prop.Unit)
                        ? prop.PropertyValue.ToString()
                        : prop.PropertyValue + prop.Unit;
                    temp.Properties.Properties[key] = value;
                }
            }

            if (response.ResourcesRequired != null)
            {
                foreach (var res in response.ResourcesRequired)
                {
                    temp.Resources[res.ResourceName] = res.ResourceAmount.ToString();
                }
            }

            return temp;
        }

        /// <summary>
        /// Determines whether a detail import is still fresh based on the configured refresh interval.
        /// </summary>
        /// <param name="lastImportUtc">The UTC timestamp of the last detail import, or null if never imported.</param>
        /// <returns>True if the import is within the refresh threshold; false if stale or never imported.</returns>
        private bool IsDetailFresh(DateTime? lastImportUtc)
        {
            if (lastImportUtc == null)
            {
                return false;
            }

            var threshold = TimeSpan.FromHours(_settings.DetailRefreshHours);
            return (SystemClock.UtcNow - lastImportUtc.Value) < threshold;
        }

        /// <summary>
        /// Finds an existing ship by GameLocationId or name, or creates a new one.
        /// Lookup order: (1) match by GameLocationId, (2) name-based fallback matching
        /// Ship.Name to the planet name, (3) create a new ship with the GameLocationId set
        /// so future lookups succeed without creation.
        /// </summary>
        /// <param name="gameLocationId">The game location identifier for the ship.</param>
        /// <param name="planetName">The planet name used for name-based fallback and as the default ship name.</param>
        /// <returns>The matched or newly created ship.</returns>
        private Ship FindOrCreateShip(int gameLocationId, string planetName)
        {
            var ships = _playerContext.GetMutableShipsForOwner(_playerContext.CurrentPlayerUUID);

            var ship = ships.FirstOrDefault(s => s.GameLocationId == gameLocationId);
            if (ship != null)
            {
                return ship;
            }

            ship = ships.FirstOrDefault(s => string.Equals(s.Name, planetName, StringComparison.OrdinalIgnoreCase));
            if (ship != null)
            {
                ship.GameLocationId = gameLocationId;
                Log.Info("FindOrCreateShip: matched ship '{0}' by name, set GameLocationId={1}.", planetName, gameLocationId);
                return ship;
            }

            ship = new Ship
            {
                UUID = Guid.NewGuid().ToString(),
                Name = planetName ?? string.Empty,
                OwnerUUID = _playerContext.CurrentPlayerUUID,
                GameLocationId = gameLocationId,
            };

            _playerContext.AddShip(ship);
            Log.Info("FindOrCreateShip: created new ship '{0}' with GameLocationId={1}.", planetName, gameLocationId);

            return ship;
        }

        /// <summary>
        /// Checks an API result for HTTP 401 (unauthorized) and attempts a token refresh.
        /// If the refresh succeeds, updates the stored access token and throws an
        /// <see cref="InvalidOperationException"/> so that the queue retries the work item
        /// with the new token. If the refresh fails, throws an
        /// <see cref="UnauthorizedAccessException"/> which will exhaust retries and mark
        /// the work item as failed.
        /// </summary>
        /// <param name="result">The API call result tuple.</param>
        /// <param name="label">The work item label for logging.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task that completes if no 401 was detected, or throws on 401.</returns>
        private async Task ThrowIfUnauthorizedAsync(
            (bool Success, string Json) result,
            string label,
            CancellationToken ct)
        {
            if (result.Success || !string.Equals(result.Json, "401", StringComparison.Ordinal))
            {
                return;
            }

            string failedToken = _currentAccessToken;
            Log.Warn("{0}: received HTTP 401, attempting token refresh.", label);

            var refreshResult = await _tokenRefreshHandler.HandleUnauthorizedAsync(failedToken, ct)
                .ConfigureAwait(false);

            if (refreshResult.Success)
            {
                _currentAccessToken = refreshResult.NewToken;
                Log.Info("{0}: token refresh succeeded, retrying with new token.", label);
                throw new InvalidOperationException(
                    "Token refreshed successfully for '" + label + "'; retrying.");
            }

            Log.Error("{0}: token refresh failed, marking work item as failed.", label);
            throw new UnauthorizedAccessException(
                "Token refresh failed for '" + label + "'; no further retry.");
        }

        /// <summary>
        /// Checks an API result for HTTP 429 (rate limited) and notifies the queue to pause dispatch.
        /// If a 429 is detected, calls <see cref="GameApiRequestQueue.NotifyRateLimited"/> with the
        /// Retry-After value (defaulting to 60 seconds if not specified) and throws an
        /// <see cref="InvalidOperationException"/> so that the queue retries the work item
        /// after the pause period expires.
        /// </summary>
        /// <param name="result">The API call result tuple.</param>
        /// <param name="label">The work item label for logging.</param>
        private void ThrowIfRateLimited(
            (bool Success, string Json) result,
            string label)
        {
            if (result.Success || !string.Equals(result.Json, "429", StringComparison.Ordinal))
            {
                return;
            }

            const int defaultRetryAfterSeconds = 60;
            Log.Warn("{0}: received HTTP 429 (rate limited), pausing queue for {1}s.", label, defaultRetryAfterSeconds);

            _currentQueue.NotifyRateLimited(defaultRetryAfterSeconds);

            throw new InvalidOperationException(
                "Rate limited (429) for '" + label + "'; retrying after pause.");
        }

        /// <summary>
        /// Creates a work item that fetches the character profile from the game API.
        /// </summary>
        /// <returns>A work item for character profile retrieval.</returns>
        private WorkItem CreateCharacterProfileItem()
        {
            return new WorkItem
            {
                Label = "CharacterProfile",
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetCharacterAsync(
                        _settings.AppId, _currentAccessToken).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "CharacterProfile", ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "CharacterProfile");

                    if (!result.Success)
                    {
                        Log.Warn("CharacterProfile fetch failed: {0}", result.Json);
                        return Array.Empty<WorkItem>();
                    }

                    Log.Debug("CharacterProfile fetched successfully.");

                    try
                    {
                        var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiProfileResponse>>(result.Json);
                        var remoteProfile = envelope?.Data;
                        if (remoteProfile == null)
                        {
                            Log.Warn("CharacterProfile: deserialized response was null.");
                            return Array.Empty<WorkItem>();
                        }

                        var localProfile = _playerContext.FindMutablePlayerProfile(
                            _playerContext.CurrentPlayerUUID);
                        if (localProfile == null)
                        {
                            Log.Warn("CharacterProfile: no local profile found for current player.");
                            return Array.Empty<WorkItem>();
                        }

                        bool changed = ProfileMergeService.MergeProfileData(localProfile, remoteProfile);
                        if (changed)
                        {
                            _playerContext.WriteContext();
                            _playerContext.OnPlayerProfileDataChanged(_playerContext.CurrentPlayerUUID);
                            Log.Info("CharacterProfile: merge applied, profile updated.");
                        }
                        else
                        {
                            Log.Debug("CharacterProfile: merge found no changes.");
                        }
                    }
                    catch (JsonException ex)
                    {
                        Log.Error(
                            "CharacterProfile: failed to deserialize response: {0}\nBody (truncated): {1}",
                            ex.Message,
                            TruncateForLog(result.Json));
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the character skills from the game API.
        /// </summary>
        /// <returns>A work item for character skills retrieval.</returns>
        private WorkItem CreateCharacterSkillsItem()
        {
            return new WorkItem
            {
                Label = "CharacterSkills",
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetCharacterSkillsAsync(
                        _settings.AppId, _currentAccessToken).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "CharacterSkills", ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "CharacterSkills");

                    if (result.Success)
                    {
                        Log.Debug("CharacterSkills fetched successfully.");
                    }
                    else
                    {
                        Log.Warn("CharacterSkills fetch failed: {0}", result.Json);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that imports the banking balance via BankingService.
        /// </summary>
        /// <returns>A work item for banking balance import.</returns>
        private WorkItem CreateBankingBalanceItem()
        {
            return new WorkItem
            {
                Label = "BankingBalance",
                ExecuteAsync = async ct =>
                {
                    try
                    {
                        decimal? balance = await BankingService.ImportBalanceAsync(
                            _apiClient, _settings.AppId, _currentAccessToken).ConfigureAwait(false);

                        if (balance.HasValue)
                        {
                            _playerContext.BankingBalance = balance.Value;
                            _playerContext.WriteContext();
                            _playerContext.OnBankingDataChanged();
                            Log.Debug("BankingBalance imported: {0}", balance.Value);
                        }
                        else
                        {
                            Log.Warn("BankingBalance: ImportBalanceAsync returned null, skipping update.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("BankingBalance: unexpected error during import: {0}", ex.Message);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the banking transactions from the game API.
        /// </summary>
        /// <returns>A work item for banking transactions retrieval.</returns>
        private WorkItem CreateBankingTransactionsItem()
        {
            return new WorkItem
            {
                Label = "BankingTransactions",
                ExecuteAsync = async ct =>
                {
                    Log.Debug("BankingTransactions: delegating to BankingService.ImportTransactionsAsync.");
                    var importResult = await BankingService.ImportTransactionsAsync(
                        _apiClient, _settings.AppId, _currentAccessToken, _playerContext).ConfigureAwait(false);

                    if (importResult.Success)
                    {
                        Log.Debug(
                            "BankingTransactions: imported {0} new, {1} duplicates skipped, {2} pages.",
                            importResult.TransactionsImported,
                            importResult.DuplicatesSkipped,
                            importResult.PagesCompleted);
                    }
                    else
                    {
                        Log.Warn("BankingTransactions import failed: {0}", importResult.ErrorMessage);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the accepted jobs from the game API.
        /// </summary>
        /// <returns>A work item for accepted jobs retrieval.</returns>
        private WorkItem CreateAcceptedJobsItem()
        {
            return new WorkItem
            {
                Label = "AcceptedJobs",
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetAcceptedJobsAsync(
                        _settings.AppId, _currentAccessToken).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "AcceptedJobs", ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "AcceptedJobs");

                    if (result.Success)
                    {
                        Log.Debug("AcceptedJobs fetched successfully.");
                    }
                    else
                    {
                        Log.Warn("AcceptedJobs fetch failed: {0}", result.Json);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the ship configuration from the game API.
        /// </summary>
        /// <returns>A work item for ship configuration retrieval.</returns>
        private WorkItem CreateShipConfigItem()
        {
            return new WorkItem
            {
                Label = "ShipConfiguration",
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetShipConfigurationAsync(
                        _settings.AppId, _currentAccessToken).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "ShipConfiguration", ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "ShipConfiguration");

                    if (!result.Success)
                    {
                        Log.Warn("ShipConfiguration fetch failed: {0}", result.Json);
                        return Array.Empty<WorkItem>();
                    }

                    Log.Debug("ShipConfiguration fetched successfully.");

                    try
                    {
                        var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiShipConfigurationResponse>>(result.Json);
                        var config = envelope?.Data;
                        if (config == null)
                        {
                            Log.Warn("ShipConfiguration: deserialized response was null.");
                            return Array.Empty<WorkItem>();
                        }

                        var ships = _playerContext.GetMutableShipsForOwner(_playerContext.CurrentPlayerUUID);
                        var ship = ships.FirstOrDefault(s => s.GameLocationId == config.ShipId);
                        if (ship == null)
                        {
                            ship = new Ship
                            {
                                UUID = Guid.NewGuid().ToString(),
                                Name = config.Summary?.ShipName ?? string.Empty,
                                OwnerUUID = _playerContext.CurrentPlayerUUID,
                                GameLocationId = config.ShipId,
                            };
                            _playerContext.AddShip(ship);
                            Log.Info("ShipConfiguration: created new ship for GameLocationId {0}.", config.ShipId);
                        }

                        var mappedComponents = new List<ShipComponentSlot>();
                        foreach (var comp in config.Components)
                        {
                            mappedComponents.Add(new ShipComponentSlot
                            {
                                SlotType = comp.BlueprintType ?? string.Empty,
                                SlotIndex = comp.Evolution,
                                BlueprintUUID = string.Empty,
                                CurrentHP = (int)((comp.HealthPercentage ?? 100d) * 100),
                                MaxHP = 10000,
                                MaxRepairPercent = (decimal)comp.LastRepairHealthPercentage,
                            });
                        }

                        ship.Components = mappedComponents;

                        if (config.Summary != null)
                        {
                            ship.Name = config.Summary.ShipName ?? ship.Name;
                        }

                        _playerContext.WriteContext();
                        _playerContext.OnShipDataChanged(ship.UUID);
                        Log.Info("ShipConfiguration: merge applied for ship '{0}' (GameLocationId {1}).", ship.Name, config.ShipId);
                    }
                    catch (JsonException ex)
                    {
                        Log.Error(
                            "ShipConfiguration: failed to deserialize response: {0}\nBody (truncated): {1}",
                            ex.Message,
                            TruncateForLog(result.Json));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            "ShipConfiguration: merge failed: {0}",
                            ex.Message);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the ship cargo from the game API.
        /// Deserializes the response as a direct JSON array of cargo items (no envelope),
        /// merges into the active ship's cargo, and cascades detail work items for
        /// crates, blueprints, and surveys.
        /// </summary>
        /// <returns>A work item for ship cargo retrieval.</returns>
        private WorkItem CreateShipCargoItem()
        {
            return new WorkItem
            {
                Label = "ShipCargo",
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetShipCargoAsync(
                        _settings.AppId, _currentAccessToken).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "ShipCargo", ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "ShipCargo");

                    if (!result.Success)
                    {
                        Log.Warn("ShipCargo fetch failed: {0}", result.Json);
                        return Array.Empty<WorkItem>();
                    }

                    Log.Debug("ShipCargo fetched successfully.");

                    try
                    {
                        var cargoItems = JsonConvert.DeserializeObject<List<GameApiAssetCargoItem>>(result.Json);
                        if (cargoItems == null || cargoItems.Count == 0)
                        {
                            Log.Debug("ShipCargo: no cargo items in response.");
                            return Array.Empty<WorkItem>();
                        }

                        var ships = _playerContext.GetMutableShipsForOwner(
                            _playerContext.CurrentPlayerUUID);
                        var activeShip = ships.FirstOrDefault();
                        if (activeShip == null)
                        {
                            Log.Warn("ShipCargo: no active ship identified, skipping merge.");
                            return Array.Empty<WorkItem>();
                        }

                        AssetMergeService.MergeShipAssets(cargoItems, activeShip);

                        _playerContext.WriteContext();
                        _playerContext.OnShipDataChanged(activeShip.UUID);
                        Log.Info("ShipCargo: merge applied for ship '{0}'.", activeShip.Name);

                        return CascadeCargoDetailItems(cargoItems, activeShip.Name, string.Empty);
                    }
                    catch (JsonException ex)
                    {
                        Log.Error(
                            "ShipCargo: failed to deserialize response: {0}\nBody (truncated): {1}",
                            ex.Message,
                            TruncateForLog(result.Json));
                    }
                    catch (Exception ex)
                    {
                        Log.Error("ShipCargo: merge failed: {0}", ex.Message);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the market listings from the game API.
        /// </summary>
        /// <returns>A work item for market listings retrieval.</returns>
        private WorkItem CreateMarketListingsItem()
        {
            return new WorkItem
            {
                Label = "MarketListings",
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetMarketListingsAsync(
                        _settings.AppId, _currentAccessToken, "all").ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "MarketListings", ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "MarketListings");

                    if (result.Success)
                    {
                        Log.Debug("MarketListings fetched successfully.");
                    }
                    else
                    {
                        Log.Warn("MarketListings fetch failed: {0}", result.Json);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches market items from the game API.
        /// </summary>
        /// <returns>A work item for market items retrieval.</returns>
        private WorkItem CreateMarketItemsItem()
        {
            return new WorkItem
            {
                Label = "MarketItems",
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetMarketItemsAsync(
                        _settings.AppId, _currentAccessToken, "all", "all").ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "MarketItems", ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "MarketItems");

                    if (result.Success)
                    {
                        Log.Debug("MarketItems fetched successfully.");
                    }
                    else
                    {
                        Log.Warn("MarketItems fetch failed: {0}", result.Json);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the market buy orders from the game API.
        /// </summary>
        /// <returns>A work item for market buy orders retrieval.</returns>
        private WorkItem CreateMarketBuyOrdersItem()
        {
            return new WorkItem
            {
                Label = "MarketBuyOrders",
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetMarketBuyOrdersAsync(
                        _settings.AppId, _currentAccessToken).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "MarketBuyOrders", ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "MarketBuyOrders");

                    if (result.Success)
                    {
                        Log.Debug("MarketBuyOrders fetched successfully.");
                    }
                    else
                    {
                        Log.Warn("MarketBuyOrders fetch failed: {0}", result.Json);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the market sell orders from the game API.
        /// </summary>
        /// <returns>A work item for market sell orders retrieval.</returns>
        private WorkItem CreateMarketSellOrdersItem()
        {
            return new WorkItem
            {
                Label = "MarketSellOrders",
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetMarketSellOrdersAsync(
                        _settings.AppId, _currentAccessToken).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "MarketSellOrders", ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "MarketSellOrders");

                    if (result.Success)
                    {
                        Log.Debug("MarketSellOrders fetched successfully.");
                    }
                    else
                    {
                        Log.Warn("MarketSellOrders fetch failed: {0}", result.Json);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the colony list and cascades per-colony detail items.
        /// </summary>
        /// <returns>A work item for colony list retrieval with cascading.</returns>
        private WorkItem CreateColonyListItem()
        {
            return new WorkItem
            {
                Label = "ColonyList",
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetColonyListAsync(
                        _settings.AppId, _currentAccessToken).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "ColonyList", ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "ColonyList");

                    if (!result.Success)
                    {
                        Log.Warn("ColonyList fetch failed: {0}", result.Json);
                        return Array.Empty<WorkItem>();
                    }

                    Log.Debug("ColonyList fetched successfully.");

                    try
                    {
                        var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiColonyListResponse>>(result.Json);
                        var response = envelope?.Data;
                        if (response?.Colonies == null || response.Colonies.Count == 0)
                        {
                            return Array.Empty<WorkItem>();
                        }

                        var localColonies = _playerContext.GetMutableColoniesForOwner(
                            _playerContext.CurrentPlayerUUID);

                        var mergeResult = ColonyMergeService.MergeColonyList(
                            response.Colonies,
                            localColonies,
                            _playerContext.CurrentPlayerUUID);

                        var colonyIdMap = mergeResult.ColonyIdToUUIDMap;

                        if (mergeResult.HasChanges)
                        {
                            _playerContext.WriteContext();
                            _playerContext.OnColonyDataChanged(string.Empty);
                            Log.Info("ColonyList: merge applied, colonies updated.");
                        }
                        else
                        {
                            Log.Debug("ColonyList: merge found no changes.");
                        }

                        var cascaded = response.Colonies
                            .SelectMany(c => new[]
                            {
                                CreateColonySummaryItem(c.ColonyId),
                                CreateColonyBuildingsItem(c.ColonyId, colonyIdMap),
                                CreateColonyWarehouseItem(c.ColonyId, colonyIdMap),
                                CreateColonyWorkersItem(c.ColonyId, colonyIdMap),
                            })
                            .ToArray();

                        return cascaded;
                    }
                    catch (JsonException ex)
                    {
                        Log.Error(
                            "ColonyList: failed to deserialize response: {0}\nBody (truncated): {1}",
                            ex.Message,
                            TruncateForLog(result.Json));
                        return Array.Empty<WorkItem>();
                    }
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the summary for a specific colony.
        /// </summary>
        /// <param name="colonyId">The colony identifier.</param>
        /// <returns>A work item for colony summary retrieval.</returns>
        private WorkItem CreateColonySummaryItem(int colonyId)
        {
            return new WorkItem
            {
                Label = "ColonySummary:" + colonyId,
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetColonySummaryAsync(
                        _settings.AppId, _currentAccessToken, colonyId).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "ColonySummary:" + colonyId, ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "ColonySummary:" + colonyId);

                    if (result.Success)
                    {
                        Log.Debug("ColonySummary:{0} fetched successfully.", colonyId);
                    }
                    else
                    {
                        Log.Warn("ColonySummary:{0} fetch failed: {1}", colonyId, result.Json);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the buildings for a specific colony.
        /// </summary>
        /// <param name="colonyId">The colony identifier.</param>
        /// <returns>A work item for colony buildings retrieval.</returns>
        private WorkItem CreateColonyBuildingsItem(int colonyId)
        {
            return new WorkItem
            {
                Label = "ColonyBuildings:" + colonyId,
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetColonyBuildingsAsync(
                        _settings.AppId, _currentAccessToken, colonyId).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "ColonyBuildings:" + colonyId, ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "ColonyBuildings:" + colonyId);

                    if (result.Success)
                    {
                        Log.Debug("ColonyBuildings:{0} fetched successfully.", colonyId);
                    }
                    else
                    {
                        Log.Warn("ColonyBuildings:{0} fetch failed: {1}", colonyId, result.Json);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the warehouse for a specific colony.
        /// </summary>
        /// <param name="colonyId">The colony identifier.</param>
        /// <returns>A work item for colony warehouse retrieval.</returns>
        private WorkItem CreateColonyWarehouseItem(int colonyId)
        {
            return new WorkItem
            {
                Label = "ColonyWarehouse:" + colonyId,
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetColonyWarehouseAsync(
                        _settings.AppId, _currentAccessToken, colonyId).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "ColonyWarehouse:" + colonyId, ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "ColonyWarehouse:" + colonyId);

                    if (result.Success)
                    {
                        Log.Debug("ColonyWarehouse:{0} fetched successfully.", colonyId);
                    }
                    else
                    {
                        Log.Warn("ColonyWarehouse:{0} fetch failed: {1}", colonyId, result.Json);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the workers for a specific colony.
        /// </summary>
        /// <param name="colonyId">The colony identifier.</param>
        /// <returns>A work item for colony workers retrieval.</returns>
        private WorkItem CreateColonyWorkersItem(int colonyId)
        {
            return new WorkItem
            {
                Label = "ColonyWorkers:" + colonyId,
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetColonyWorkersAsync(
                        _settings.AppId, _currentAccessToken, colonyId).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "ColonyWorkers:" + colonyId, ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "ColonyWorkers:" + colonyId);

                    if (result.Success)
                    {
                        Log.Debug("ColonyWorkers:{0} fetched successfully.", colonyId);
                    }
                    else
                    {
                        Log.Warn("ColonyWorkers:{0} fetch failed: {1}", colonyId, result.Json);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the buildings for a specific colony,
        /// using the ColonyId-to-UUID map from the parent ColonyList merge.
        /// </summary>
        /// <param name="colonyId">The colony identifier.</param>
        /// <param name="colonyIdMap">The mapping of API ColonyId to local Colony UUID.</param>
        /// <returns>A work item for colony buildings retrieval.</returns>
        private WorkItem CreateColonyBuildingsItem(int colonyId, Dictionary<int, string> colonyIdMap)
        {
            return new WorkItem
            {
                Label = "ColonyBuildings:" + colonyId,
                ExecuteAsync = async ct =>
                {
                    string colonyUUID;
                    if (!colonyIdMap.TryGetValue(colonyId, out colonyUUID))
                    {
                        var localColonies = _playerContext.GetMutableColoniesForOwner(
                            _playerContext.CurrentPlayerUUID);
                        var fallback = localColonies.FirstOrDefault(c => c.ColonyId == colonyId);
                        if (fallback == null)
                        {
                            Log.Warn(
                                "ColonyBuildings:{0} — colony unresolvable from map or local data, skipping.",
                                colonyId);
                            return Array.Empty<WorkItem>();
                        }

                        colonyUUID = fallback.UUID;
                    }

                    Log.Debug("ColonyBuildings:{0} resolved target colony UUID: {1}", colonyId, colonyUUID);

                    var result = await _apiClient.GetColonyBuildingsAsync(
                        _settings.AppId, _currentAccessToken, colonyId).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "ColonyBuildings:" + colonyId, ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "ColonyBuildings:" + colonyId);

                    if (!result.Success)
                    {
                        Log.Warn("ColonyBuildings:{0} fetch failed: {1}", colonyId, result.Json);
                        return Array.Empty<WorkItem>();
                    }

                    Log.Debug("ColonyBuildings:{0} fetched successfully.", colonyId);

                    try
                    {
                        var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiColonyBuildingsResponse>>(result.Json);
                        if (envelope?.Data?.Buildings == null)
                        {
                            Log.Warn(
                                "ColonyBuildings:{0} — deserialized Data.Buildings is null, skipping merge.",
                                colonyId);
                            return Array.Empty<WorkItem>();
                        }

                        var localColonies = _playerContext.GetMutableColoniesForOwner(
                            _playerContext.CurrentPlayerUUID);
                        var targetColony = localColonies.FirstOrDefault(c => c.UUID == colonyUUID);
                        if (targetColony == null)
                        {
                            Log.Warn(
                                "ColonyBuildings:{0} — target colony UUID '{1}' not found in local data, skipping merge.",
                                colonyId,
                                colonyUUID);
                            return Array.Empty<WorkItem>();
                        }

                        ColonyMergeService.MergeBuildings(envelope.Data.Buildings, targetColony);
                        _playerContext.WriteContext();
                        _playerContext.OnColonyDataChanged(colonyUUID);
                        Log.Debug("ColonyBuildings:{0} merge completed.", colonyId);
                    }
                    catch (JsonException ex)
                    {
                        Log.Error(
                            "ColonyBuildings:{0} — failed to deserialize response: {1}\nBody (truncated): {2}",
                            colonyId,
                            ex.Message,
                            TruncateForLog(result.Json));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            "ColonyBuildings:{0} — merge error: {1}",
                            colonyId,
                            ex.Message);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the warehouse for a specific colony,
        /// using the ColonyId-to-UUID map from the parent ColonyList merge.
        /// </summary>
        /// <param name="colonyId">The colony identifier.</param>
        /// <param name="colonyIdMap">The mapping of API ColonyId to local Colony UUID.</param>
        /// <returns>A work item for colony warehouse retrieval.</returns>
        private WorkItem CreateColonyWarehouseItem(int colonyId, Dictionary<int, string> colonyIdMap)
        {
            return new WorkItem
            {
                Label = "ColonyWarehouse:" + colonyId,
                ExecuteAsync = async ct =>
                {
                    string colonyUUID;
                    if (!colonyIdMap.TryGetValue(colonyId, out colonyUUID))
                    {
                        var localColonies = _playerContext.GetMutableColoniesForOwner(
                            _playerContext.CurrentPlayerUUID);
                        var fallback = localColonies.FirstOrDefault(c => c.ColonyId == colonyId);
                        if (fallback == null)
                        {
                            Log.Warn(
                                "ColonyWarehouse:{0} — colony unresolvable from map or local data, skipping.",
                                colonyId);
                            return Array.Empty<WorkItem>();
                        }

                        colonyUUID = fallback.UUID;
                    }

                    Log.Debug("ColonyWarehouse:{0} resolved target colony UUID: {1}", colonyId, colonyUUID);

                    var result = await _apiClient.GetColonyWarehouseAsync(
                        _settings.AppId, _currentAccessToken, colonyId).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "ColonyWarehouse:" + colonyId, ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "ColonyWarehouse:" + colonyId);

                    if (!result.Success)
                    {
                        Log.Warn("ColonyWarehouse:{0} fetch failed: {1}", colonyId, result.Json);
                        return Array.Empty<WorkItem>();
                    }

                    Log.Debug("ColonyWarehouse:{0} fetched successfully.", colonyId);

                    try
                    {
                        var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiColonyWarehouseResponse>>(result.Json);
                        if (envelope?.Data?.Contents == null)
                        {
                            Log.Warn(
                                "ColonyWarehouse:{0} — deserialized Data.Contents is null, skipping merge.",
                                colonyId);
                            return Array.Empty<WorkItem>();
                        }

                        var localColonies = _playerContext.GetMutableColoniesForOwner(
                            _playerContext.CurrentPlayerUUID);
                        var targetColony = localColonies.FirstOrDefault(c => c.UUID == colonyUUID);
                        if (targetColony == null)
                        {
                            Log.Warn(
                                "ColonyWarehouse:{0} — target colony UUID '{1}' not found in local data, skipping merge.",
                                colonyId,
                                colonyUUID);
                            return Array.Empty<WorkItem>();
                        }

                        var blueprintLinkage = new BlueprintLinkageService(_playerContext, _empireContext);
                        var surveyLinkage = new SurveyLinkageService(_playerContext);
                        ColonyMergeService.MergeWarehouse(
                            envelope.Data.Contents, targetColony, blueprintLinkage, surveyLinkage);
                        _playerContext.WriteContext();
                        _playerContext.OnColonyDataChanged(colonyUUID);
                        Log.Debug("ColonyWarehouse:{0} merge completed.", colonyId);
                    }
                    catch (JsonException ex)
                    {
                        Log.Error(
                            "ColonyWarehouse:{0} — failed to deserialize response: {1}\nBody (truncated): {2}",
                            colonyId,
                            ex.Message,
                            TruncateForLog(result.Json));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            "ColonyWarehouse:{0} — merge error: {1}",
                            colonyId,
                            ex.Message);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the workers for a specific colony,
        /// using the ColonyId-to-UUID map from the parent ColonyList merge.
        /// </summary>
        /// <param name="colonyId">The colony identifier.</param>
        /// <param name="colonyIdMap">The mapping of API ColonyId to local Colony UUID.</param>
        /// <returns>A work item for colony workers retrieval.</returns>
        private WorkItem CreateColonyWorkersItem(int colonyId, Dictionary<int, string> colonyIdMap)
        {
            return new WorkItem
            {
                Label = "ColonyWorkers:" + colonyId,
                ExecuteAsync = async ct =>
                {
                    string colonyUUID;
                    if (!colonyIdMap.TryGetValue(colonyId, out colonyUUID))
                    {
                        var localColonies = _playerContext.GetMutableColoniesForOwner(
                            _playerContext.CurrentPlayerUUID);
                        var fallback = localColonies.FirstOrDefault(c => c.ColonyId == colonyId);
                        if (fallback == null)
                        {
                            Log.Warn(
                                "ColonyWorkers:{0} — colony unresolvable from map or local data, skipping.",
                                colonyId);
                            return Array.Empty<WorkItem>();
                        }

                        colonyUUID = fallback.UUID;
                    }

                    Log.Debug("ColonyWorkers:{0} resolved target colony UUID: {1}", colonyId, colonyUUID);

                    var result = await _apiClient.GetColonyWorkersAsync(
                        _settings.AppId, _currentAccessToken, colonyId).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "ColonyWorkers:" + colonyId, ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "ColonyWorkers:" + colonyId);

                    if (!result.Success)
                    {
                        Log.Warn("ColonyWorkers:{0} fetch failed: {1}", colonyId, result.Json);
                        return Array.Empty<WorkItem>();
                    }

                    Log.Debug("ColonyWorkers:{0} fetched successfully.", colonyId);

                    try
                    {
                        var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiColonyWorkersResponse>>(result.Json);
                        if (envelope?.Data == null)
                        {
                            Log.Warn("ColonyWorkers:{0} — deserialized Data was null, skipping merge.", colonyId);
                            return Array.Empty<WorkItem>();
                        }

                        var localColonies = _playerContext.GetMutableColoniesForOwner(
                            _playerContext.CurrentPlayerUUID);
                        var targetColony = localColonies.FirstOrDefault(c => c.UUID == colonyUUID);
                        if (targetColony == null)
                        {
                            Log.Warn(
                                "ColonyWorkers:{0} — target colony UUID {1} not found in local data, skipping merge.",
                                colonyId,
                                colonyUUID);
                            return Array.Empty<WorkItem>();
                        }

                        ColonyMergeService.MergeWorkers(envelope.Data, targetColony);
                        _playerContext.WriteContext();
                        _playerContext.OnColonyDataChanged(colonyUUID);
                        Log.Debug("ColonyWorkers:{0} merge completed.", colonyId);
                    }
                    catch (JsonException ex)
                    {
                        Log.Error(
                            "ColonyWorkers:{0} — failed to deserialize response: {1}\nBody (truncated): {2}",
                            colonyId,
                            ex.Message,
                            TruncateForLog(result.Json));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            "ColonyWorkers:{0} — merge failed: {1}",
                            colonyId,
                            ex.Message);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the asset locations list and cascades one detail item per location.
        /// </summary>
        /// <returns>A work item for asset locations retrieval with cascading.</returns>
        private WorkItem CreateAssetLocationsItem()
        {
            return new WorkItem
            {
                Label = "AssetLocations",
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetAssetLocationsAsync(
                        _settings.AppId, _currentAccessToken).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "AssetLocations", ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "AssetLocations");

                    if (!result.Success)
                    {
                        Log.Warn("AssetLocations fetch failed: {0}", result.Json);
                        return Array.Empty<WorkItem>();
                    }

                    Log.Debug("AssetLocations fetched successfully.");

                    var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiAssetLocationsResponse>>(result.Json);
                    var response = envelope?.Data;
                    if (response?.Locations == null || response.Locations.Count == 0)
                    {
                        return Array.Empty<WorkItem>();
                    }

                    var cascaded = response.Locations
                        .Where(loc => loc.LocationId > 0 && !string.IsNullOrEmpty(loc.LocationType))
                        .Select(loc => CreateAssetLocationDetailItem(
                            loc.LocationId,
                            loc.LocationType,
                            loc.LocationName,
                            loc.SystemName))
                        .ToArray();

                    return cascaded;
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the detail for a specific asset location.
        /// Dispatches crate, survey, and blueprint detail items based on the cargo contents.
        /// </summary>
        /// <param name="id">The asset location identifier.</param>
        /// <param name="typeC">The location type code (e.g. "Co", "St", "Sh").</param>
        /// <param name="planetName">The planet or location name.</param>
        /// <param name="systemName">The star system name.</param>
        /// <returns>A work item for asset location detail retrieval.</returns>
        private WorkItem CreateAssetLocationDetailItem(int id, string typeC, string planetName, string systemName)
        {
            return new WorkItem
            {
                Label = "AssetDetail:" + id,
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetAssetLocationDetailAsync(
                        _settings.AppId, _currentAccessToken, id, typeC).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "AssetDetail:" + id, ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "AssetDetail:" + id);

                    if (!result.Success)
                    {
                        Log.Warn("AssetDetail:{0} fetch failed: {1}", id, result.Json);
                        return Array.Empty<WorkItem>();
                    }

                    Log.Debug("AssetDetail:{0} ({1}) at {2}/{3} fetched successfully.", id, typeC, systemName, planetName);

                    var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiAssetDetailResponse>>(result.Json);
                    var response = envelope?.Data;
                    if (response?.Cargo == null || response.Cargo.Count == 0)
                    {
                        return Array.Empty<WorkItem>();
                    }

                    // Generic cargo merge dispatched by location type
                    switch (typeC)
                    {
                        case "Co":
                            var colonies = _playerContext.GetMutableColoniesForOwner(
                                _playerContext.CurrentPlayerUUID);
                            var colony = colonies.FirstOrDefault(c => c.ColonyId == id);
                            if (colony != null)
                            {
                                AssetMergeService.MergeColonyAssets(response.Cargo, colony);
                                _playerContext.WriteContext();
                                _playerContext.OnColonyDataChanged(colony.UUID);
                            }
                            else
                            {
                                Log.Warn("AssetDetail:{0}: no colony found with ColonyId={0}, skipping merge.", id);
                            }

                            break;

                        case "St":
                            var station = FindOrCreateStation(id, planetName, systemName);
                            ItemBag targetHold;
                            string playerUUID = _playerContext.CurrentPlayerUUID;
                            if (!station.Holds.TryGetValue(playerUUID, out targetHold))
                            {
                                targetHold = new ItemBag();
                                station.Holds[playerUUID] = targetHold;
                            }

                            AssetMergeService.MergeStationAssets(response.Cargo, station, targetHold);
                            _playerContext.WriteContext();
                            _playerContext.OnStationDataChanged();
                            break;

                        case "Sh":
                            var ship = FindOrCreateShip(id, planetName);
                            AssetMergeService.MergeShipAssets(response.Cargo, ship);
                            _playerContext.WriteContext();
                            _playerContext.OnShipDataChanged(ship.UUID);
                            break;

                        default:
                            Log.Warn("AssetDetail:{0}: unknown location type '{1}', skipping merge.", id, typeC);
                            return Array.Empty<WorkItem>();
                    }

                    // Cascade detail work items using shared helper
                    return CascadeCargoDetailItems(response.Cargo, planetName, systemName);
                },
            };
        }

        /// <summary>
        /// Cascades cargo entries into detail work items for crates, blueprints, and surveys.
        /// This shared helper encapsulates the cascading logic used by both AssetLocationDetail
        /// and ShipCargo, ensuring consistent freshness thresholds, factory methods, and skip
        /// conditions regardless of cargo source.
        /// </summary>
        /// <param name="cargo">The list of cargo items to cascade.</param>
        /// <param name="planetName">The planet name for survey context.</param>
        /// <param name="systemName">The star system name for survey context.</param>
        /// <returns>An array of cascaded detail work items.</returns>
        private WorkItem[] CascadeCargoDetailItems(
            List<GameApiAssetCargoItem> cargo,
            string planetName,
            string systemName)
        {
            var items = new List<WorkItem>();

            foreach (var entry in cargo)
            {
                switch (entry.TypeC)
                {
                    case "Crate":
                        items.Add(CreateCrateDetailItem(entry.CargoItemId));
                        break;

                    case "Bp":
                        var existingBp = _playerContext.FindBlueprintByApiId(entry.CargoItemId);
                        if (existingBp == null || !IsDetailFresh(existingBp.LastDetailImportUtc))
                        {
                            items.Add(CreateBlueprintDetailItem(entry.CargoItemId));
                        }
                        else
                        {
                            Log.Debug("BlueprintDetail:{0} skipped (fresh).", entry.CargoItemId);
                        }

                        break;

                    case "S":
                        var existingSurvey = _playerContext.FindSurveyByApiId(entry.CargoItemId);
                        if (existingSurvey == null || !IsDetailFresh(existingSurvey.LastDetailImportUtc))
                        {
                            items.Add(CreateSurveyDetailItem(entry.CargoItemId, planetName, systemName));
                        }
                        else
                        {
                            Log.Debug("SurveyDetail:{0} skipped (fresh).", entry.CargoItemId);
                        }

                        break;
                }
            }

            return items.ToArray();
        }

        /// <summary>
        /// Finds an existing station by GameLocationId, falls back to name-based match,
        /// or creates a new station. Sets GameLocationId on adopted/created stations so
        /// future lookups succeed without creation.
        /// </summary>
        /// <param name="gameLocationId">The game API location identifier for the station.</param>
        /// <param name="planetName">The planet/station display name from the API.</param>
        /// <param name="systemName">The star system name.</param>
        /// <returns>The matched or newly created station.</returns>
        private Station FindOrCreateStation(int gameLocationId, string planetName, string systemName)
        {
            string playerUUID = _playerContext.CurrentPlayerUUID;
            var localStations = _playerContext.GetMutableStationsForOwner(playerUUID);

            // Step 1: Match by GameLocationId
            var station = localStations.FirstOrDefault(s => s.GameLocationId == gameLocationId);
            if (station != null)
            {
                return station;
            }

            // Step 2: Name-based fallback for pre-existing manually-created stations
            station = localStations.FirstOrDefault(s =>
                (s.GameLocationId == null || s.GameLocationId == 0) &&
                string.Equals(s.Name, planetName, StringComparison.OrdinalIgnoreCase));

            if (station != null)
            {
                station.GameLocationId = gameLocationId;
                station.SystemName = systemName;
                Log.Info(
                    "FindOrCreateStation: adopted existing station '{0}' UUID={1} (set GameLocationId={2})",
                    station.Name,
                    station.UUID,
                    gameLocationId);
                return station;
            }

            // Step 3: Create new station
            station = new Station
            {
                UUID = Guid.NewGuid().ToString(),
                Name = planetName,
                GameLocationId = gameLocationId,
                SystemName = systemName,
                OwnerUUID = playerUUID,
            };

            station.Holds[playerUUID] = new ItemBag();
            _playerContext.AddStation(station);
            Log.Info(
                "FindOrCreateStation: created new station '{0}' UUID={1} (GameLocationId={2}) in system '{3}'",
                station.Name,
                station.UUID,
                gameLocationId,
                systemName);

            return station;
        }

        /// <summary>
        /// Creates a work item that fetches crate detail for blueprint extraction.
        /// </summary>
        /// <param name="crateId">The crate cargo item identifier.</param>
        /// <returns>A work item for crate detail retrieval.</returns>
        private WorkItem CreateCrateDetailItem(int crateId)
        {
            return new WorkItem
            {
                Label = "CrateDetail:" + crateId,
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetAssetCrateAsync(
                        _settings.AppId, _currentAccessToken, crateId).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "CrateDetail:" + crateId, ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "CrateDetail:" + crateId);

                    if (!result.Success)
                    {
                        Log.Warn("CrateDetail:{0} fetch failed: {1}", crateId, result.Json);
                        return Array.Empty<WorkItem>();
                    }

                    Log.Debug("CrateDetail:{0} fetched successfully, importing.", crateId);
                    CrateImporter.ImportFromJson(result.Json, _playerContext, _empireContext);

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches blueprint detail for import.
        /// </summary>
        /// <param name="blueprintId">The blueprint cargo item identifier.</param>
        /// <returns>A work item for blueprint detail retrieval.</returns>
        private WorkItem CreateBlueprintDetailItem(int blueprintId)
        {
            return new WorkItem
            {
                Label = "BlueprintDetail:" + blueprintId,
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetAssetBlueprintAsync(
                        _settings.AppId, _currentAccessToken, blueprintId).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "BlueprintDetail:" + blueprintId, ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "BlueprintDetail:" + blueprintId);

                    if (!result.Success)
                    {
                        Log.Warn("BlueprintDetail:{0} fetch failed: {1}", blueprintId, result.Json);
                        return Array.Empty<WorkItem>();
                    }

                    Log.Debug("BlueprintDetail:{0} fetched successfully, importing.", blueprintId);

                    var response = JsonConvert.DeserializeObject<GameApiBlueprintDetailResponse>(result.Json);
                    if (response?.Blueprint == null)
                    {
                        Log.Warn("BlueprintDetail:{0} response has no blueprint info.", blueprintId);
                        return Array.Empty<WorkItem>();
                    }

                    var bpInfo = response.Blueprint;

                    var entry = new JObject
                    {
                        ["name"] = bpInfo.Name,
                        ["evolution"] = bpInfo.Evolution,
                    };

                    if (!string.IsNullOrEmpty(bpInfo.PartTypeIcon))
                    {
                        entry["iconClass"] = "ui_icon_" + bpInfo.PartTypeIcon;

                        string iconPos = ConvertPartTypeIconToPosition(bpInfo.PartTypeIcon);
                        if (!string.IsNullOrEmpty(iconPos))
                        {
                            entry["iconPosition"] = iconPos;
                        }
                    }

                    var propsObj = new JObject();
                    if (response.BlueprintProperties != null)
                    {
                        foreach (var prop in response.BlueprintProperties)
                        {
                            string key = !string.IsNullOrEmpty(prop.FriendlyPropertyName)
                                ? prop.FriendlyPropertyName
                                : prop.PropertyName;
                            string value = string.IsNullOrEmpty(prop.Unit)
                                ? prop.PropertyValue.ToString()
                                : prop.PropertyValue + prop.Unit;
                            propsObj[key] = value;
                        }
                    }

                    entry["properties"] = propsObj;

                    var resObj = new JObject();
                    if (response.ResourcesRequired != null)
                    {
                        foreach (var res in response.ResourcesRequired)
                        {
                            resObj[res.ResourceName] = res.ResourceAmount.ToString();
                        }
                    }

                    entry["resources"] = resObj;

                    var jsonArray = new JArray { entry };
                    var importResult = CrateImporter.ImportFromJson(jsonArray.ToString(), _playerContext, _empireContext);

                    // Track API ID and freshness on the imported blueprint.
                    if (importResult.Entries.Count > 0 &&
                        importResult.Entries[0].Action != ImportAction.Skipped)
                    {
                        string importedName = importResult.Entries[0].Name;
                        int importedEvo = importResult.Entries[0].Evolution;

                        var blueprint = ResolveBlueprintForApiId(
                            blueprintId, importedName, importedEvo, response);

                        if (blueprint != null)
                        {
                            blueprint.GameApiBlueprintId = blueprintId;
                            blueprint.LastDetailImportUtc = SystemClock.UtcNow;
                            _playerContext.IndexBlueprintByApiId(blueprint);
                            Log.Debug(
                                "BlueprintDetail:{0} tracked API ID on UUID={1}.",
                                blueprintId,
                                blueprint.UUID);
                        }
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Resolves the local blueprint that should be assigned the given API blueprint ID.
        /// Uses a three-tier strategy: (1) existing assignment lookup, (2) filtered Name+Evo
        /// candidates excluding blueprints claimed by other API IDs, (3) property-based
        /// scoring via FindBestMatch when multiple unassigned candidates remain.
        /// Returns null if no suitable candidate is found.
        /// </summary>
        /// <param name="blueprintId">The game API blueprint ID to assign.</param>
        /// <param name="importedName">The blueprint name from the import result.</param>
        /// <param name="importedEvo">The blueprint evolution from the import result.</param>
        /// <param name="response">The full API response for building a scoring template.</param>
        /// <returns>The resolved blueprint, or null if no match.</returns>
        private Blueprint ResolveBlueprintForApiId(
            int blueprintId,
            string importedName,
            int importedEvo,
            GameApiBlueprintDetailResponse response)
        {
            // Tier 1: Check if this API ID is already assigned to a local blueprint.
            var existing = _playerContext.FindBlueprintByApiId(blueprintId);
            if (existing != null)
            {
                Log.Debug(
                    "ResolveBlueprintForApiId({0}): Tier 1 hit — already assigned to UUID={1}.",
                    blueprintId,
                    existing.UUID);
                return existing;
            }

            // Tier 2: Filter by Name+Evo, excluding blueprints claimed by OTHER API IDs.
            var candidates = _playerContext.BlueprintList
                .Where(b =>
                    string.Equals(b.Name, importedName, StringComparison.Ordinal) &&
                    b.Evolution == importedEvo &&
                    !(b.GameApiBlueprintId.HasValue && b.GameApiBlueprintId.Value != blueprintId))
                .ToList();

            Log.Debug(
                "ResolveBlueprintForApiId({0}): Tier 2 filter for '{1}' Evo{2} found {3} candidate(s).",
                blueprintId,
                importedName,
                importedEvo,
                candidates.Count);

            if (candidates.Count == 0)
            {
                Log.Warn(
                    "ResolveBlueprintForApiId({0}): no candidates remain after filtering for '{1}' Evo{2}. Skipping assignment.",
                    blueprintId,
                    importedName,
                    importedEvo);
                return null;
            }

            if (candidates.Count == 1)
            {
                Log.Debug(
                    "ResolveBlueprintForApiId({0}): single candidate UUID={1}, assigning directly.",
                    blueprintId,
                    candidates[0].UUID);
                return candidates[0];
            }

            // Tier 3: Multiple unassigned candidates — score by property similarity.
            Log.Debug(
                "ResolveBlueprintForApiId({0}): {1} candidates, using FindBestMatch for scoring.",
                blueprintId,
                candidates.Count);

            var tempBlueprint = BuildScoringTemplate(response);
            var bestMatch = BlueprintService.FindBestMatch(candidates, tempBlueprint);

            if (bestMatch != null)
            {
                Log.Debug(
                    "ResolveBlueprintForApiId({0}): Tier 3 selected UUID={1} via scoring.",
                    blueprintId,
                    bestMatch.UUID);
                return bestMatch;
            }

            // FindBestMatch returned null (no candidate scored > 0) — fall back to first candidate.
            Log.Debug(
                "ResolveBlueprintForApiId({0}): Tier 3 scoring inconclusive, using first candidate UUID={1}.",
                blueprintId,
                candidates[0].UUID);
            return candidates[0];
        }

        /// <summary>
        /// Creates a work item that fetches survey detail for import.
        /// </summary>
        /// <param name="surveyId">The survey cargo item identifier.</param>
        /// <param name="planetName">The planet name for survey context.</param>
        /// <param name="systemName">The star system name for survey context.</param>
        /// <returns>A work item for survey detail retrieval.</returns>
        private WorkItem CreateSurveyDetailItem(int surveyId, string planetName, string systemName)
        {
            return new WorkItem
            {
                Label = "SurveyDetail:" + surveyId,
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetAssetSurveyAsync(
                        _settings.AppId, _currentAccessToken, surveyId).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "SurveyDetail:" + surveyId, ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "SurveyDetail:" + surveyId);

                    if (!result.Success)
                    {
                        Log.Warn("SurveyDetail:{0} fetch failed: {1}", surveyId, result.Json);
                        return Array.Empty<WorkItem>();
                    }

                    Log.Debug("SurveyDetail:{0} fetched successfully, importing.", surveyId);

                    var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiSurveyResponse>>(result.Json);
                    var detail = envelope?.Data?.Survey;
                    if (detail == null)
                    {
                        Log.Warn("SurveyDetail:{0} response contained no survey data.", surveyId);
                        return Array.Empty<WorkItem>();
                    }

                    var tempSurvey = BuildTempSurveyFromDetail(detail, planetName, systemName);

                    var surveys = _playerContext.SurveyList;
                    var existing = SurveyImportHelper.FindByKey(surveys, planetName, detail.EncryptedId);

                    Survey survey;
                    if (existing != null)
                    {
                        SurveyImportHelper.MergeData(existing, tempSurvey);
                        survey = existing;
                        Log.Debug("SurveyDetail:{0} merged into existing survey UUID={1}.", surveyId, survey.UUID);
                    }
                    else
                    {
                        survey = SurveyImportHelper.CreateFromTemp(tempSurvey, _playerContext.CurrentPlayerUUID);
                        _playerContext.AddSurvey(survey);
                        Log.Debug("SurveyDetail:{0} created new survey UUID={1}.", surveyId, survey.UUID);
                    }

                    SurveyImportHelper.LinkOrCreateAsteroid(survey, _playerContext);

                    survey.SystemObjectId = detail.SystemObjectId;
                    if (detail.SystemObjectId > 0 && !string.IsNullOrEmpty(survey.AsteroidUUID))
                    {
                        var asteroid = _playerContext.FindAsteroid(survey.AsteroidUUID);
                        if (asteroid != null)
                        {
                            asteroid.SystemObjectId = detail.SystemObjectId;
                        }
                    }

                    survey.GameApiSurveyId = detail.Id;
                    survey.LastDetailImportUtc = SystemClock.UtcNow;
                    _playerContext.IndexSurveyByApiId(survey);

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the kill mail list and cascades one detail item per kill mail.
        /// </summary>
        /// <returns>A work item for kill mail list retrieval with cascading.</returns>
        private WorkItem CreateKillMailListItem()
        {
            return new WorkItem
            {
                Label = "KillMailList",
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetKillMailListAsync(
                        _settings.AppId, _currentAccessToken).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "KillMailList", ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "KillMailList");

                    if (!result.Success)
                    {
                        Log.Warn("KillMailList fetch failed: {0}", result.Json);
                        return Array.Empty<WorkItem>();
                    }

                    Log.Debug("KillMailList fetched successfully.");

                    var envelope = JObject.Parse(result.Json);
                    var killMails = envelope["data"]?["killMails"] as JArray;
                    if (killMails == null || killMails.Count == 0)
                    {
                        return Array.Empty<WorkItem>();
                    }

                    var cascaded = killMails
                        .Select(km => km["killMailId"]?.Value<int>() ?? 0)
                        .Where(id => id > 0)
                        .Select(id => CreateKillMailDetailItem(id))
                        .ToArray();

                    return cascaded;
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the detail for a specific kill mail.
        /// </summary>
        /// <param name="killMailId">The kill mail identifier.</param>
        /// <returns>A work item for kill mail detail retrieval.</returns>
        private WorkItem CreateKillMailDetailItem(int killMailId)
        {
            return new WorkItem
            {
                Label = "KillMailDetail:" + killMailId,
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetKillMailDetailAsync(
                        _settings.AppId, _currentAccessToken, killMailId).ConfigureAwait(false);

                    await ThrowIfUnauthorizedAsync(result, "KillMailDetail:" + killMailId, ct)
                        .ConfigureAwait(false);

                    ThrowIfRateLimited(result, "KillMailDetail:" + killMailId);

                    if (result.Success)
                    {
                        Log.Debug("KillMailDetail:{0} fetched successfully.", killMailId);
                    }
                    else
                    {
                        Log.Warn("KillMailDetail:{0} fetch failed: {1}", killMailId, result.Json);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the first page of the mail list and cascades
        /// one detail item per mail plus a next page item if the current page is non-empty.
        /// </summary>
        /// <returns>A work item for mail list retrieval with cascading.</returns>
        private WorkItem CreateMailListItem()
        {
            return new WorkItem
            {
                Label = "MailList",
                ExecuteAsync = async ct =>
                {
                    Log.Debug("MailList: delegating to MailService.SyncMailAsync.");
                    int result = await MailService.SyncMailAsync(
                        _apiClient, _settings.AppId, _currentAccessToken, _playerContext).ConfigureAwait(false);

                    if (result >= 0)
                    {
                        Log.Debug("MailList: synced {0} new messages.", result);
                    }
                    else
                    {
                        Log.Warn("MailList: sync returned error ({0}).", result);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }
    }
}
