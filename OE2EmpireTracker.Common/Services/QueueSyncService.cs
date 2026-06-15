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
using Newtonsoft.Json.Linq;
using NLog;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Common.Client;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using Generated = OE2EmpireTracker.Common.Client.Generated;

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
        private readonly IGameApiTypedClient _typedClient;
        private readonly GameApiConnectionSettings _settings;
        private readonly GameApiCredentialManager _credentialManager;
        private readonly MarketDataService _marketDataService;

        private readonly object _syncLock = new object();

        private volatile string _currentAccessToken = string.Empty;
        private TokenRefreshHandler _tokenRefreshHandler;
        private GameApiRequestQueue _currentQueue;

        private volatile bool _isSyncRunning;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueSyncService"/> class.
        /// </summary>
        /// <param name="playerContext">The player context for data persistence.</param>
        /// <param name="empireContext">The empire context for shared game data.</param>
        /// <param name="typedClient">The typed game API client for HTTP communication.</param>
        /// <param name="settings">The connection settings (TPS, AppId, etc.).</param>
        /// <param name="credentialManager">The credential manager for token refresh operations.</param>
        /// <param name="marketDataService">The market data service for persisting market listings.</param>
        public QueueSyncService(
            PlayerContext playerContext,
            EmpireContext empireContext,
            IGameApiTypedClient typedClient,
            GameApiConnectionSettings settings,
            GameApiCredentialManager credentialManager,
            MarketDataService marketDataService)
        {
            _playerContext = playerContext;
            _empireContext = empireContext;
            _typedClient = typedClient;
            _settings = settings;
            _credentialManager = credentialManager;
            _marketDataService = marketDataService;
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

                try
                {
                    var tokenDto = await _typedClient.ExchangeTokenAsync(
                        _settings.AppId, _settings.ClientId, plainSecret, ct).ConfigureAwait(false);

                    _currentAccessToken = tokenDto.AccessToken;
                }
                catch (ApiHttpException ex)
                {
                    Log.Error("QueueSyncService: token exchange failed: HTTP {0}", ex.StatusCode);
                    return new QueueSyncResult();
                }

                Log.Info("QueueSyncService: token exchange succeeded, starting dispatch.");

                _tokenRefreshHandler = new TokenRefreshHandler(
                    _typedClient,
                    _settings,
                    _credentialManager,
                    playerUUID,
                    _currentAccessToken);

                var metricsFilePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "sync-metrics.csv");

                // Rate limiting is enforced by the typed client's TokenBucketRateLimiter (set from settings.Tps).
                // Pass a high TPS to the queue so its TokenBucketGovernor acts as a pass-through;
                // it only controls dispatch pacing, not actual HTTP rate.
                var queue = new GameApiRequestQueue(
                    1000.0,
                    perItemTimeout: null,
                    maxInflight: _settings.MaxInflightRequests,
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
        /// Checks whether a collection of cargo items contains any blueprint entries.
        /// </summary>
        /// <param name="cargo">The cargo items from an asset crate or location detail.</param>
        /// <returns>True if the collection contains at least one item with TypeC equal to Blueprint.</returns>
        internal static bool ResponseContainsBlueprints(ICollection<Generated.AssetCargoItem> cargo)
        {
            if (cargo == null || cargo.Count == 0)
            {
                return false;
            }

            return cargo.Any(c =>
                string.Equals(c.TypeC, AssetTypeCodes.Blueprint, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Builds a CrateImporter-compatible JSON array from blueprint entries
        /// in a cargo item collection. Transforms AssetCargoItem format
        /// into the scraper-format JSON that CrateImporter.ImportFromJson expects.
        /// </summary>
        /// <param name="cargo">The cargo items from an asset crate or location detail.</param>
        /// <returns>A JSON array string for CrateImporter, or null if no blueprints found.</returns>
        internal static string BuildCrateImporterJson(ICollection<Generated.AssetCargoItem> cargo)
        {
            if (cargo == null || cargo.Count == 0)
            {
                return null;
            }

            var blueprintItems = cargo.Where(c =>
                string.Equals(c.TypeC, AssetTypeCodes.Blueprint, StringComparison.OrdinalIgnoreCase)).ToList();

            if (blueprintItems.Count == 0)
            {
                return null;
            }

            var jsonArray = new JArray();
            foreach (var item in blueprintItems)
            {
                var entry = new JObject
                {
                    ["name"] = item.ResourceName ?? string.Empty,
                    ["evolution"] = item.Evolution ?? 0,
                };

                // Build properties object from AssetCargoProperty list
                var propsObj = new JObject();
                if (item.Properties != null)
                {
                    foreach (var prop in item.Properties)
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
                entry["resources"] = new JObject();

                // Convert icon code to sprite position if available
                if (!string.IsNullOrEmpty(item.Icon))
                {
                    string iconPosition = ConvertPartTypeIconToPosition(item.Icon);
                    if (!string.IsNullOrEmpty(iconPosition))
                    {
                        entry["iconPosition"] = iconPosition;
                    }
                }

                jsonArray.Add(entry);
            }

            return jsonArray.ToString();
        }

        /// <summary>
        /// Constructs a temporary Survey from the game API survey detail response fields.
        /// </summary>
        /// <param name="detail">The parsed survey detail from the API.</param>
        /// <param name="planetName">The planet name from the parent asset location context.</param>
        /// <param name="systemName">The star system name from the parent asset location context.</param>
        /// <returns>A temporary Survey populated with the API response data.</returns>
        private static Survey BuildTempSurveyFromDetail(Generated.Survey detail, string planetName, string systemName)
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
        /// Builds a temporary Blueprint populated with API response properties for use
        /// as the scoring template in <see cref="BlueprintService.FindBestMatch"/>.
        /// </summary>
        /// <param name="response">The API blueprint detail response.</param>
        /// <returns>A temporary Blueprint with Name, Evolution, and Properties set.</returns>
        private static Blueprint BuildScoringTemplate(Generated.AssetBlueprint response)
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
            var ship = _playerContext.FindShipByGameLocationId(gameLocationId);
            if (ship != null)
            {
                return ship;
            }

            var ships = _playerContext.GetMutableShipsForOwner(_playerContext.CurrentPlayerUUID);

            ship = ships.FirstOrDefault(s => string.Equals(s.Name, planetName, StringComparison.OrdinalIgnoreCase));
            if (ship != null)
            {
                ship.GameLocationId = gameLocationId;
                _playerContext.IndexShipByGameLocationId(ship);
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
        /// Handles an HTTP 401 unauthorized response by attempting to refresh the access token
        /// via the TokenRefreshHandler. If the refresh succeeds, updates the stored access token
        /// and returns so the caller can retry. If the refresh fails, throws
        /// <see cref="UnauthorizedAccessException"/> which will exhaust retries and mark
        /// the work item as failed.
        /// </summary>
        /// <param name="label">The work item label for logging.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task that completes when token refresh succeeds (caller should retry).</returns>
        private async Task HandleUnauthorizedAsync(string label, CancellationToken ct)
        {
            Log.Warn("{0}: received HTTP 401, attempting token refresh.", label);
            var refreshResult = await _tokenRefreshHandler.HandleUnauthorizedAsync(_currentAccessToken, ct)
                .ConfigureAwait(false);

            if (refreshResult.Success)
            {
                _currentAccessToken = refreshResult.NewToken;
                Log.Info("{0}: token refresh succeeded.", label);
                return;
            }

            Log.Error("{0}: token refresh failed.", label);
            throw new UnauthorizedAccessException("Token refresh failed for '" + label + "'.");
        }

        /// <summary>
        /// Handles an HTTP 429 rate-limited response by notifying the queue to pause dispatch
        /// for a default interval and throwing <see cref="InvalidOperationException"/> so that
        /// the queue retries the work item after the pause period expires.
        /// </summary>
        /// <param name="label">The work item label for logging.</param>
        private void HandleRateLimited(string label)
        {
            const int defaultRetryAfterSeconds = 60;
            Log.Warn("{0}: received HTTP 429, pausing queue for {1}s.", label, defaultRetryAfterSeconds);
            _currentQueue.NotifyRateLimited(defaultRetryAfterSeconds);
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
                    try
                    {
                        var profile = await _typedClient.GetCharacterAsync(ct).ConfigureAwait(false);

                        var localProfile = _playerContext.FindMutablePlayerProfile(
                            _playerContext.CurrentPlayerUUID);
                        if (localProfile == null)
                        {
                            Log.Warn("CharacterProfile: no local profile found for current player.");
                            return Array.Empty<WorkItem>();
                        }

                        bool changed = ProfileMergeService.MergeProfileData(localProfile, profile);
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
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("CharacterProfile", ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("CharacterProfile");
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("CharacterProfile: business error RC={0}: {1}", ex.ReturnCode, ex.ReturnString);
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
                    try
                    {
                        var skills = await _typedClient.GetCharacterSkillsAsync(ct).ConfigureAwait(false);
                        Log.Debug("CharacterSkills fetched successfully ({0} skills).", skills.Skills?.Count ?? 0);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("CharacterSkills", ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("CharacterSkills");
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("CharacterSkills: business error RC={0}: {1}", ex.ReturnCode, ex.ReturnString);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that imports the banking balance from the game API.
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
                        var balanceDto = await _typedClient.GetBankingBalanceAsync(ct).ConfigureAwait(false);

                        _playerContext.BankingBalance = (decimal)balanceDto.Balance;
                        _playerContext.WriteContext();
                        _playerContext.OnBankingDataChanged();
                        Log.Debug("BankingBalance imported: {0}", balanceDto.Balance);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("BankingBalance", ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("BankingBalance");
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("BankingBalance: business error RC={0}: {1}", ex.ReturnCode, ex.ReturnString);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches banking transactions from the game API
        /// and imports them with deduplication into the player context.
        /// </summary>
        /// <returns>A work item for banking transactions retrieval.</returns>
        private WorkItem CreateBankingTransactionsItem()
        {
            return new WorkItem
            {
                Label = "BankingTransactions",
                ExecuteAsync = async ct =>
                {
                    try
                    {
                        var txDto = await _typedClient.GetBankingTransactionsAsync(ct: ct).ConfigureAwait(false);

                        if (txDto.Transactions == null || txDto.Transactions.Count == 0)
                        {
                            Log.Debug("BankingTransactions: no transactions returned.");
                            return Array.Empty<WorkItem>();
                        }

                        var existingKeys = new HashSet<string>();
                        foreach (var tx in _playerContext.BankingTransactionList)
                        {
                            string key = string.Format(
                                "{0}|{1}|{2}",
                                tx.TransactionDateTime,
                                tx.CreditChange,
                                tx.Detail);
                            existingKeys.Add(key);
                        }

                        int imported = 0;
                        int duplicates = 0;

                        foreach (var item in txDto.Transactions)
                        {
                            string transactionDT = item.TransactionDT.ToString("o");
                            decimal creditChange = (decimal)item.CreditChange;
                            string detail = item.Detail ?? string.Empty;

                            string compositeKey = string.Format("{0}|{1}|{2}", transactionDT, creditChange, detail);

                            if (existingKeys.Contains(compositeKey))
                            {
                                duplicates++;
                                continue;
                            }

                            var bankingTx = new Models.BankingTransaction
                            {
                                UUID = Guid.NewGuid().ToString(),
                                OwnerUUID = _playerContext.CurrentPlayerUUID,
                                TransactionDateTime = transactionDT,
                                CreditChange = creditChange,
                                OldBalance = (decimal)item.OldBalance,
                                NewBalance = (decimal)item.NewBalance,
                                TransactionType = item.TransactionType,
                                Detail = detail,
                                CharacterId = item.CharacterId,
                                SystemObjectId = item.SystemObjectId,
                                SystemId = item.SystemId,
                                IsManualEntry = false,
                            };

                            _playerContext.AddBankingTransaction(bankingTx);
                            existingKeys.Add(compositeKey);
                            imported++;
                        }

                        _playerContext.WriteContext();
                        _playerContext.OnBankingDataChanged();

                        Log.Debug(
                            "BankingTransactions: imported {0} new, {1} duplicates skipped.",
                            imported,
                            duplicates);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("BankingTransactions", ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("BankingTransactions");
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("BankingTransactions: business error RC={0}: {1}", ex.ReturnCode, ex.ReturnString);
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
                    try
                    {
                        var jobs = await _typedClient.GetAcceptedJobsAsync(ct).ConfigureAwait(false);
                        Log.Debug("AcceptedJobs fetched successfully ({0} jobs).", jobs.Jobs?.Count ?? 0);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("AcceptedJobs", ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("AcceptedJobs");
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("AcceptedJobs: business error RC={0}: {1}", ex.ReturnCode, ex.ReturnString);
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
                    try
                    {
                        var config = await _typedClient.GetShipConfigurationAsync(ct).ConfigureAwait(false);

                        var ship = _playerContext.FindShipByGameLocationId(config.ShipId);
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
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("ShipConfiguration", ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("ShipConfiguration");
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("ShipConfiguration: business error RC={0}: {1}", ex.ReturnCode, ex.ReturnString);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the ship cargo from the game API.
        /// Deserializes the response via the typed client as ShipCargo DTO,
        /// merges into the active ship's cargo, and cascades detail items for
        /// crates, blueprints, and surveys via CascadeCargoDetailItems.
        /// </summary>
        /// <returns>A work item for ship cargo retrieval.</returns>
        private WorkItem CreateShipCargoItem()
        {
            return new WorkItem
            {
                Label = "ShipCargo",
                ExecuteAsync = async ct =>
                {
                    try
                    {
                        var shipCargo = await _typedClient.GetShipCargoAsync(ct).ConfigureAwait(false);

                        var cargoItems = shipCargo.Cargo;
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

                        return CascadeCargoDetailItems(cargoItems, activeShip.Name, string.Empty, activeShip.Cargo);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("ShipCargo", ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("ShipCargo");
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("ShipCargo: business error RC={0}: {1}", ex.ReturnCode, ex.ReturnString);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the market listings from the game API
        /// and persists them via <see cref="MarketDataService"/>.
        /// </summary>
        /// <param name="typeCode">The game type code filter (e.g. "all", "R", "C").</param>
        /// <param name="range">The trade range in JAS for the search, or null for default.</param>
        /// <param name="searchText">Optional search text filter.</param>
        /// <returns>A work item for market listings retrieval and persistence.</returns>
        private WorkItem CreateMarketListingsItem(string typeCode = "all", int? range = null, string searchText = null)
        {
            return new WorkItem
            {
                Label = "MarketListings",
                ExecuteAsync = async ct =>
                {
                    try
                    {
                        var listings = await _typedClient.GetMarketListingsAsync(typeCode, range, searchText, ct).ConfigureAwait(false);

                        try
                        {
                            string characterUUID = _playerContext.CurrentPlayerUUID;
                            int systemId = 0;
                            int rangeJas = range ?? 0;
                            _marketDataService.ProcessMarketListings(listings, characterUUID, systemId, rangeJas);
                            Log.Debug("MarketListings processed: merged into dataset.");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Failed to process market listings for character {0}", _playerContext.CurrentPlayerUUID);
                        }
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("MarketListings", ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("MarketListings");
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("MarketListings: business error RC={0}: {1}", ex.ReturnCode, ex.ReturnString);
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
                    try
                    {
                        var items = await _typedClient.GetMarketItemsAsync("all", "all", ct).ConfigureAwait(false);
                        Log.Debug("MarketItems fetched successfully.");
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("MarketItems", ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("MarketItems");
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("MarketItems: business error RC={0}: {1}", ex.ReturnCode, ex.ReturnString);
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
                    try
                    {
                        var orders = await _typedClient.GetMarketBuyOrdersAsync(ct).ConfigureAwait(false);
                        Log.Debug("MarketBuyOrders fetched successfully.");
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("MarketBuyOrders", ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("MarketBuyOrders");
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("MarketBuyOrders: business error RC={0}: {1}", ex.ReturnCode, ex.ReturnString);
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
                    try
                    {
                        var orders = await _typedClient.GetMarketSellOrdersAsync(ct).ConfigureAwait(false);
                        Log.Debug("MarketSellOrders fetched successfully.");
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("MarketSellOrders", ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("MarketSellOrders");
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("MarketSellOrders: business error RC={0}: {1}", ex.ReturnCode, ex.ReturnString);
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
                    try
                    {
                        var response = await _typedClient.GetColonyListAsync(ct).ConfigureAwait(false);

                        if (response?.Colonies == null || response.Colonies.Count == 0)
                        {
                            Log.Debug("ColonyList: no colonies in response.");
                            return Array.Empty<WorkItem>();
                        }

                        Log.Debug("ColonyList fetched successfully.");

                        var localColonies = _playerContext.GetMutableColoniesForOwner(
                            _playerContext.CurrentPlayerUUID);

                        var mergeResult = ColonyMergeService.MergeColonyList(
                            response,
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
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("ColonyList", ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("ColonyList");
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("ColonyList: business error RC={0}: {1}", ex.ReturnCode, ex.ReturnString);
                    }

                    return Array.Empty<WorkItem>();
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
                    try
                    {
                        var summary = await _typedClient.GetColonySummaryAsync(colonyId, ct).ConfigureAwait(false);
                        Log.Debug("ColonySummary:{0} fetched successfully.", colonyId);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("ColonySummary:" + colonyId, ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("ColonySummary:" + colonyId);
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("ColonySummary:{0}: business error RC={1}: {2}", colonyId, ex.ReturnCode, ex.ReturnString);
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
                    try
                    {
                        var buildings = await _typedClient.GetColonyBuildingsAsync(colonyId, ct).ConfigureAwait(false);
                        Log.Debug("ColonyBuildings:{0} fetched successfully.", colonyId);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("ColonyBuildings:" + colonyId, ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("ColonyBuildings:" + colonyId);
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("ColonyBuildings:{0}: business error RC={1}: {2}", colonyId, ex.ReturnCode, ex.ReturnString);
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
                    try
                    {
                        var warehouse = await _typedClient.GetColonyWarehouseAsync(colonyId, ct)
                            .ConfigureAwait(false);
                        Log.Debug("ColonyWarehouse:{0} fetched successfully.", colonyId);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("ColonyWarehouse:" + colonyId, ct)
                            .ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("ColonyWarehouse:" + colonyId);
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error(
                            "ColonyWarehouse:{0}: business error RC={1}: {2}",
                            colonyId,
                            ex.ReturnCode,
                            ex.ReturnString);
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
                    try
                    {
                        var workers = await _typedClient.GetColonyWorkersAsync(colonyId, ct)
                            .ConfigureAwait(false);
                        Log.Debug("ColonyWorkers:{0} fetched successfully.", colonyId);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("ColonyWorkers:" + colonyId, ct)
                            .ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("ColonyWorkers:" + colonyId);
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error(
                            "ColonyWorkers:{0}: business error RC={1}: {2}",
                            colonyId,
                            ex.ReturnCode,
                            ex.ReturnString);
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

                    try
                    {
                        var response = await _typedClient.GetColonyBuildingsAsync(colonyId, ct).ConfigureAwait(false);

                        if (response?.Buildings == null)
                        {
                            Log.Warn(
                                "ColonyBuildings:{0} — response Buildings is null, skipping merge.",
                                colonyId);
                            return Array.Empty<WorkItem>();
                        }

                        Log.Debug("ColonyBuildings:{0} fetched successfully.", colonyId);

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

                        ColonyMergeService.MergeBuildings(response, targetColony);
                        _playerContext.WriteContext();
                        _playerContext.OnColonyDataChanged(colonyUUID);
                        Log.Debug("ColonyBuildings:{0} merge completed.", colonyId);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("ColonyBuildings:" + colonyId, ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("ColonyBuildings:" + colonyId);
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("ColonyBuildings:{0}: business error RC={1}: {2}", colonyId, ex.ReturnCode, ex.ReturnString);
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

                    try
                    {
                        var warehouse = await _typedClient.GetColonyWarehouseAsync(colonyId, ct)
                            .ConfigureAwait(false);

                        Log.Debug("ColonyWarehouse:{0} fetched successfully.", colonyId);

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
                            warehouse, targetColony, blueprintLinkage, surveyLinkage);
                        _playerContext.WriteContext();
                        _playerContext.OnColonyDataChanged(colonyUUID);
                        Log.Debug("ColonyWarehouse:{0} merge completed.", colonyId);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("ColonyWarehouse:" + colonyId, ct)
                            .ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("ColonyWarehouse:" + colonyId);
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error(
                            "ColonyWarehouse:{0}: business error RC={1}: {2}",
                            colonyId,
                            ex.ReturnCode,
                            ex.ReturnString);
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

                    try
                    {
                        var workers = await _typedClient.GetColonyWorkersAsync(colonyId, ct)
                            .ConfigureAwait(false);

                        Log.Debug("ColonyWorkers:{0} fetched successfully.", colonyId);

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

                        ColonyMergeService.MergeWorkers(workers, targetColony);
                        _playerContext.WriteContext();
                        _playerContext.OnColonyDataChanged(colonyUUID);
                        Log.Debug("ColonyWorkers:{0} merge completed.", colonyId);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("ColonyWorkers:" + colonyId, ct)
                            .ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("ColonyWorkers:" + colonyId);
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error(
                            "ColonyWorkers:{0}: business error RC={1}: {2}",
                            colonyId,
                            ex.ReturnCode,
                            ex.ReturnString);
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
                    try
                    {
                        var response = await _typedClient.GetAssetLocationsAsync(ct).ConfigureAwait(false);

                        if (response?.Locations == null || response.Locations.Count == 0)
                        {
                            Log.Debug("AssetLocations: no locations in response.");
                            return Array.Empty<WorkItem>();
                        }

                        Log.Debug("AssetLocations fetched successfully.");

                        var cascaded = response.Locations
                            .Where(loc => loc.LocationId > 0 && !string.IsNullOrEmpty(loc.LocationType))
                            .Select(loc => CreateAssetLocationDetailItem(
                                loc.LocationId,
                                loc.LocationType,
                                loc.LocationName,
                                loc.SystemName))
                            .ToArray();

                        return cascaded;
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("AssetLocations", ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("AssetLocations");
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("AssetLocations: business error RC={0}: {1}", ex.ReturnCode, ex.ReturnString);
                    }

                    return Array.Empty<WorkItem>();
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
                    try
                    {
                        var response = await _typedClient.GetAssetLocationDetailAsync(id, typeC, ct)
                            .ConfigureAwait(false);

                        if (response?.Cargo == null || response.Cargo.Count == 0)
                        {
                            Log.Debug("AssetDetail:{0} ({1}) at {2}/{3}: no cargo.", id, typeC, systemName, planetName);
                            return Array.Empty<WorkItem>();
                        }

                        Log.Debug("AssetDetail:{0} ({1}) at {2}/{3} fetched successfully.", id, typeC, systemName, planetName);

                        // Generic cargo merge dispatched by location type
                        ItemBag cargoBag = null;
                        switch (typeC)
                        {
                            case AssetTypeCodes.Colony:
                                var colonies = _playerContext.GetMutableColoniesForOwner(
                                    _playerContext.CurrentPlayerUUID);
                                var colony = colonies.FirstOrDefault(c => c.ColonyId == id);
                                if (colony != null)
                                {
                                    AssetMergeService.MergeColonyAssets(response.Cargo, colony);
                                    _playerContext.WriteContext();
                                    _playerContext.OnColonyDataChanged(colony.UUID);
                                    cargoBag = colony.Items;
                                }
                                else
                                {
                                    Log.Warn("AssetDetail:{0}: no colony found with ColonyId={0}, skipping merge.", id);
                                }

                                break;

                            case AssetTypeCodes.Station:
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
                                cargoBag = targetHold;
                                break;

                            case AssetTypeCodes.Ship:
                                var ship = FindOrCreateShip(id, planetName);
                                AssetMergeService.MergeShipAssets(response.Cargo, ship);
                                _playerContext.WriteContext();
                                _playerContext.OnShipDataChanged(ship.UUID);
                                cargoBag = ship.Cargo;
                                break;

                            default:
                                Log.Warn("AssetDetail:{0}: unknown location type '{1}', skipping merge.", id, typeC);
                                return Array.Empty<WorkItem>();
                        }

                        // Cascade detail work items using shared helper
                        return CascadeCargoDetailItems(response.Cargo, planetName, systemName, cargoBag);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("AssetDetail:" + id, ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("AssetDetail:" + id);
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("AssetDetail:{0}: business error RC={1}: {2}", id, ex.ReturnCode, ex.ReturnString);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Cascades cargo entries into detail work items for crates, blueprints, and surveys.
        /// This shared helper encapsulates the cascading logic used by both AssetLocationDetail
        /// and ShipCargo, ensuring consistent freshness thresholds, factory methods, and skip
        /// conditions regardless of cargo source.
        /// </summary>
        /// <param name="cargo">The collection of cargo items to cascade.</param>
        /// <param name="planetName">The planet name for survey context.</param>
        /// <param name="systemName">The star system name for survey context.</param>
        /// <param name="parentBag">The parent ItemBag containing the cargo items (for crate content import).</param>
        /// <returns>An array of cascaded detail work items.</returns>
        private WorkItem[] CascadeCargoDetailItems(
            ICollection<Generated.AssetCargoItem> cargo,
            string planetName,
            string systemName,
            ItemBag parentBag)
        {
            var items = new List<WorkItem>();
            var visitedCrateIds = new HashSet<int>();
            int blueprintsFetched = 0;
            int blueprintsSkipped = 0;
            int surveysFetched = 0;
            int surveysSkipped = 0;

            foreach (var entry in cargo)
            {
                switch (entry.TypeC?.Trim())
                {
                    case AssetTypeCodes.Crate:
                        items.Add(CreateCrateDetailItem(entry.Id, parentBag, visitedCrateIds, planetName, systemName));
                        break;

                    case AssetTypeCodes.Blueprint:
                        var existingBp = _playerContext.FindBlueprintByApiId(entry.Id);
                        if (existingBp == null || !IsDetailFresh(existingBp.LastDetailImportUtc))
                        {
                            items.Add(CreateBlueprintDetailItem(entry.Id));
                            blueprintsFetched++;
                        }
                        else
                        {
                            Log.Debug("BlueprintDetail:{0} skipped (fresh).", entry.Id);
                            blueprintsSkipped++;
                        }

                        break;

                    case AssetTypeCodes.Survey:
                        var existingSurvey = _playerContext.FindSurveyByApiId(entry.Id);
                        if (existingSurvey == null || !IsDetailFresh(existingSurvey.LastDetailImportUtc))
                        {
                            items.Add(CreateSurveyDetailItem(entry.Id, planetName, systemName));
                            surveysFetched++;
                        }
                        else
                        {
                            Log.Debug("SurveyDetail:{0} skipped (fresh).", entry.Id);
                            surveysSkipped++;
                        }

                        break;
                }
            }

            if (blueprintsFetched > 0 || blueprintsSkipped > 0)
            {
                Log.Info(
                    "CascadeCargoDetail: blueprints fetched={0} skipped={1}",
                    blueprintsFetched,
                    blueprintsSkipped);
            }

            if (surveysFetched > 0 || surveysSkipped > 0)
            {
                Log.Info(
                    "CascadeCargoDetail: surveys fetched={0} skipped={1}",
                    surveysFetched,
                    surveysSkipped);
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
            // Step 1: Match by GameLocationId via index (O(1))
            var station = _playerContext.FindStationByGameLocationId(gameLocationId);
            if (station != null)
            {
                return station;
            }

            string playerUUID = _playerContext.CurrentPlayerUUID;
            var localStations = _playerContext.GetMutableStationsForOwner(playerUUID);

            // Step 2: Name-based fallback for pre-existing manually-created stations
            station = localStations.FirstOrDefault(s =>
                (s.GameLocationId == null || s.GameLocationId == 0) &&
                string.Equals(s.Name, planetName, StringComparison.OrdinalIgnoreCase));

            if (station != null)
            {
                station.GameLocationId = gameLocationId;
                station.SystemName = systemName;
                _playerContext.IndexStationByGameLocationId(station);
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
        /// Creates a work item that fetches crate detail and invokes the CrateContentImporter
        /// to populate the crate's Contents bag with all item types found inside.
        /// Returns cascade work items for any nested crates, surveys, and blueprints discovered.
        /// </summary>
        /// <param name="crateId">The crate cargo item identifier.</param>
        /// <param name="parentBag">The ItemBag containing the crate item.</param>
        /// <param name="visitedCrateIds">Set of already-visited crate IDs for cycle detection.</param>
        /// <param name="planetName">The planet/location name for survey context.</param>
        /// <param name="systemName">The star system name for survey context.</param>
        /// <returns>A work item for crate detail retrieval and content import.</returns>
        private WorkItem CreateCrateDetailItem(
            int crateId,
            ItemBag parentBag,
            HashSet<int> visitedCrateIds,
            string planetName,
            string systemName)
        {
            return new WorkItem
            {
                Label = "CrateDetail:" + crateId,
                ExecuteAsync = async ct =>
                {
                    try
                    {
                        var crateContents = await _typedClient.GetCrateContentsAsync(crateId, ct)
                            .ConfigureAwait(false);

                        Log.Debug("CrateDetail:{0} fetched successfully, importing.", crateId);

                        // Invoke CrateContentImporter for full content population
                        string ownerUUID = _playerContext.CurrentPlayerUUID;
                        var blueprintLinkageService = new BlueprintLinkageService(_playerContext, _empireContext);
                        var importer = new CrateContentImporter(_playerContext, _empireContext, blueprintLinkageService);

                        bool contentSuccess = false;
                        CrateContentImportResult importResult = null;
                        try
                        {
                            importResult = importer.Import(crateContents, crateId, parentBag, ownerUUID, visitedCrateIds);
                            contentSuccess = importResult.Success;

                            if (!importResult.Success)
                            {
                                Log.Warn("CrateDetail:{0} content import failed with {1} error(s).", crateId, importResult.Errors.Count);
                            }
                        }
                        catch (Exception contentEx)
                        {
                            Log.Error("CrateDetail:{0} content import threw exception: {1}", crateId, contentEx.Message);
                            importResult = new CrateContentImportResult { Success = false };
                        }

                        // Return cascade work items for nested crates, surveys, and blueprints
                        var cascadeItems = new List<WorkItem>();
                        foreach (int nestedCrateId in importResult.NestedCrateIds)
                        {
                            cascadeItems.Add(CreateCrateDetailItem(nestedCrateId, parentBag, visitedCrateIds, planetName, systemName));
                        }

                        // Cascade survey and blueprint detail items from crate contents
                        int blueprintsFetched = 0;
                        int blueprintsSkipped = 0;
                        int surveysFetched = 0;
                        int surveysSkipped = 0;

                        if (crateContents?.Cargo != null)
                        {
                            foreach (var entry in crateContents.Cargo)
                            {
                                string typeC = entry.TypeC?.Trim();
                                if (string.Equals(typeC, AssetTypeCodes.Survey, StringComparison.OrdinalIgnoreCase))
                                {
                                    var existingSurvey = _playerContext.FindSurveyByApiId(entry.Id);
                                    if (existingSurvey == null || !IsDetailFresh(existingSurvey.LastDetailImportUtc))
                                    {
                                        cascadeItems.Add(CreateSurveyDetailItem(entry.Id, planetName, systemName));
                                        surveysFetched++;
                                    }
                                    else
                                    {
                                        Log.Debug("CrateDetail:{0} SurveyDetail:{1} skipped (fresh).", crateId, entry.Id);
                                        surveysSkipped++;
                                    }
                                }
                                else if (string.Equals(typeC, AssetTypeCodes.Blueprint, StringComparison.OrdinalIgnoreCase))
                                {
                                    var existingBp = _playerContext.FindBlueprintByApiId(entry.Id);
                                    if (existingBp == null || !IsDetailFresh(existingBp.LastDetailImportUtc))
                                    {
                                        cascadeItems.Add(CreateBlueprintDetailItem(entry.Id));
                                        blueprintsFetched++;
                                    }
                                    else
                                    {
                                        Log.Debug("CrateDetail:{0} BlueprintDetail:{1} skipped (fresh).", crateId, entry.Id);
                                        blueprintsSkipped++;
                                    }
                                }
                            }
                        }

                        if (blueprintsFetched > 0 || blueprintsSkipped > 0 || surveysFetched > 0 || surveysSkipped > 0)
                        {
                            Log.Info(
                                "CrateDetail:{0} cascade: blueprints fetched={1} skipped={2}, surveys fetched={3} skipped={4}",
                                crateId,
                                blueprintsFetched,
                                blueprintsSkipped,
                                surveysFetched,
                                surveysSkipped);
                        }

                        return cascadeItems;
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("CrateDetail:" + crateId, ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("CrateDetail:" + crateId);
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("CrateDetail:{0}: business error RC={1}: {2}", crateId, ex.ReturnCode, ex.ReturnString);
                    }

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
                    try
                    {
                        var response = await _typedClient.GetBlueprintDetailAsync(blueprintId, ct)
                            .ConfigureAwait(false);

                        if (response?.Blueprint == null)
                        {
                            Log.Warn("BlueprintDetail:{0} response has no blueprint info.", blueprintId);
                            return Array.Empty<WorkItem>();
                        }

                        Log.Debug("BlueprintDetail:{0} fetched successfully, importing.", blueprintId);

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

                        entry["description"] = bpInfo.Description;

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

                        // Map manufacture time — API value is in hours (e.g. 35 = 35h)
                        int mfgHours = bpInfo.ManufactureTime;
                        if (mfgHours > 0)
                        {
                            propsObj[BlueprintPropertyKeys.ManufactureRunTime] = mfgHours + "h";
                        }

                        // Map manufacture amount (only if > 1, since 1 is the default)
                        if (bpInfo.ManufactureAmount > 1)
                        {
                            propsObj[BlueprintPropertyKeys.AmountManufactured] = bpInfo.ManufactureAmount.ToString();
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
                            string matchedUUID = importResult.Entries[0].MatchedUUID;
                            Blueprint blueprint = null;

                            if (!string.IsNullOrEmpty(matchedUUID))
                            {
                                // Use the UUID directly from the import result — most reliable
                                blueprint = _playerContext.FindMutableBlueprint(matchedUUID)
                                    ?? _empireContext.FindMutableGlobalBlueprint(matchedUUID);
                            }

                            if (blueprint == null)
                            {
                                // Fallback to the multi-tier resolution strategy
                                string importedName = importResult.Entries[0].Name;
                                int importedEvo = importResult.Entries[0].Evolution;
                                blueprint = ResolveBlueprintForApiId(
                                    blueprintId, importedName, importedEvo, response);
                            }

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
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("BlueprintDetail:" + blueprintId, ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("BlueprintDetail:" + blueprintId);
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("BlueprintDetail:{0}: business error RC={1}: {2}", blueprintId, ex.ReturnCode, ex.ReturnString);
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
            Generated.AssetBlueprint response)
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
            // Search both player blueprints and global (evo 0) blueprints since CrateImporter
            // stores evo 0 blueprints in the global list (EmpireContext).
            var candidates = _playerContext.BlueprintList
                .Concat(_empireContext.GlobalBlueprintList)
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
                    try
                    {
                        var surveyResponse = await _typedClient.GetSurveyDetailAsync(surveyId, ct)
                            .ConfigureAwait(false);

                        var detail = surveyResponse?.Survey;
                        if (detail == null)
                        {
                            Log.Warn("SurveyDetail:{0} response contained no survey data.", surveyId);
                            return Array.Empty<WorkItem>();
                        }

                        Log.Debug("SurveyDetail:{0} fetched successfully, importing.", surveyId);

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

                        survey.GameApiSurveyId = surveyId;
                        survey.LastDetailImportUtc = SystemClock.UtcNow;
                        _playerContext.IndexSurveyByApiId(survey);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("SurveyDetail:" + surveyId, ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("SurveyDetail:" + surveyId);
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("SurveyDetail:{0}: business error RC={1}: {2}", surveyId, ex.ReturnCode, ex.ReturnString);
                    }

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
                    try
                    {
                        var killMailList = await _typedClient.GetKillMailListAsync(ct: ct).ConfigureAwait(false);

                        if (killMailList.KillMails == null || killMailList.KillMails.Count == 0)
                        {
                            Log.Debug("KillMailList: no kill mails returned.");
                            return Array.Empty<WorkItem>();
                        }

                        Log.Debug("KillMailList: fetched {0} kill mails.", killMailList.KillMails.Count);

                        var cascaded = killMailList.KillMails
                            .Where(km => km.KillMailId > 0)
                            .Select(km => CreateKillMailDetailItem(km.KillMailId))
                            .ToArray();

                        return cascaded;
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("KillMailList", ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("KillMailList");
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("KillMailList: business error RC={0}: {1}", ex.ReturnCode, ex.ReturnString);
                    }

                    return Array.Empty<WorkItem>();
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
                    try
                    {
                        var detail = await _typedClient.GetKillMailDetailAsync(killMailId, ct).ConfigureAwait(false);
                        Log.Debug("KillMailDetail:{0} fetched successfully.", killMailId);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await HandleUnauthorizedAsync("KillMailDetail:" + killMailId, ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 429)
                    {
                        HandleRateLimited("KillMailDetail:" + killMailId);
                    }
                    catch (ApiBusinessException ex)
                    {
                        Log.Error("KillMailDetail:{0}: business error RC={1}: {2}", killMailId, ex.ReturnCode, ex.ReturnString);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that syncs the mail list by delegating to MailService.
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
                        _typedClient, _playerContext, ct).ConfigureAwait(false);

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
