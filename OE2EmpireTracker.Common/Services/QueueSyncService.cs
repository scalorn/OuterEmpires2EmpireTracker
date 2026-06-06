// <copyright file="QueueSyncService.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly object _syncLock = new object();

        private string _currentAccessToken = string.Empty;

        private volatile bool _isSyncRunning;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueSyncService"/> class.
        /// </summary>
        /// <param name="playerContext">The player context for data persistence.</param>
        /// <param name="empireContext">The empire context for shared game data.</param>
        /// <param name="apiClient">The game API client for HTTP communication.</param>
        /// <param name="settings">The connection settings (TPS, AppId, etc.).</param>
        public QueueSyncService(
            PlayerContext playerContext,
            EmpireContext empireContext,
            GameApiClient apiClient,
            GameApiConnectionSettings settings)
        {
            _playerContext = playerContext;
            _empireContext = empireContext;
            _apiClient = apiClient;
            _settings = settings;
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

                var queue = new GameApiRequestQueue(
                    _settings.Tps,
                    perItemTimeout: null,
                    maxInflightMultiplier: 3,
                    maxRetries: 3);

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

                queue.Start(ct);
                await queue.DrainAsync();

                Log.Info("Queue sync cycle completed.");
                return new QueueSyncResult();
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

                    if (result.Success)
                    {
                        Log.Debug("CharacterProfile fetched successfully.");
                    }
                    else
                    {
                        Log.Warn("CharacterProfile fetch failed: {0}", result.Json);
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
        /// Creates a work item that fetches the banking balance from the game API.
        /// </summary>
        /// <returns>A work item for banking balance retrieval.</returns>
        private WorkItem CreateBankingBalanceItem()
        {
            return new WorkItem
            {
                Label = "BankingBalance",
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetBankingBalanceAsync(
                        _settings.AppId, _currentAccessToken).ConfigureAwait(false);

                    if (result.Success)
                    {
                        Log.Debug("BankingBalance fetched successfully.");
                    }
                    else
                    {
                        Log.Warn("BankingBalance fetch failed: {0}", result.Json);
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
                    var result = await _apiClient.GetBankingTransactionsAsync(
                        _settings.AppId, _currentAccessToken).ConfigureAwait(false);

                    if (result.Success)
                    {
                        Log.Debug("BankingTransactions fetched successfully.");
                    }
                    else
                    {
                        Log.Warn("BankingTransactions fetch failed: {0}", result.Json);
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

                    if (result.Success)
                    {
                        Log.Debug("ShipConfiguration fetched successfully.");
                    }
                    else
                    {
                        Log.Warn("ShipConfiguration fetch failed: {0}", result.Json);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the ship cargo from the game API.
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

                    if (result.Success)
                    {
                        Log.Debug("ShipCargo fetched successfully.");
                    }
                    else
                    {
                        Log.Warn("ShipCargo fetch failed: {0}", result.Json);
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

                    if (!result.Success)
                    {
                        Log.Warn("ColonyList fetch failed: {0}", result.Json);
                        return Array.Empty<WorkItem>();
                    }

                    Log.Debug("ColonyList fetched successfully.");

                    var response = JsonConvert.DeserializeObject<GameApiColonyListResponse>(result.Json);
                    if (response?.Colonies == null || response.Colonies.Count == 0)
                    {
                        return Array.Empty<WorkItem>();
                    }

                    var cascaded = response.Colonies
                        .SelectMany(c => new[]
                        {
                            CreateColonySummaryItem(c.ColonyId),
                            CreateColonyBuildingsItem(c.ColonyId),
                            CreateColonyWarehouseItem(c.ColonyId),
                            CreateColonyWorkersItem(c.ColonyId),
                        })
                        .ToArray();

                    return cascaded;
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

                    var cascaded = new List<WorkItem>();

                    foreach (var item in response.Cargo)
                    {
                        switch (item.TypeC)
                        {
                            case "Crate":
                                cascaded.Add(CreateCrateDetailItem(item.CargoItemId));
                                break;

                            case "Bp":
                                var existingBp = _playerContext.FindBlueprintByApiId(item.CargoItemId);
                                if (existingBp == null || !IsDetailFresh(existingBp.LastDetailImportUtc))
                                {
                                    cascaded.Add(CreateBlueprintDetailItem(item.CargoItemId));
                                }
                                else
                                {
                                    Log.Debug("BlueprintDetail:{0} skipped (fresh).", item.CargoItemId);
                                }

                                break;

                            case "S":
                                var existingSurvey = _playerContext.FindSurveyByApiId(item.CargoItemId);
                                if (existingSurvey == null || !IsDetailFresh(existingSurvey.LastDetailImportUtc))
                                {
                                    cascaded.Add(CreateSurveyDetailItem(item.CargoItemId, planetName, systemName));
                                }
                                else
                                {
                                    Log.Debug("SurveyDetail:{0} skipped (fresh).", item.CargoItemId);
                                }

                                break;
                        }
                    }

                    return cascaded.ToArray();
                },
            };
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
                        ["description"] = bpInfo.Type,
                    };

                    if (!string.IsNullOrEmpty(bpInfo.PartTypeIcon))
                    {
                        entry["iconClass"] = "ui_icon_" + bpInfo.PartTypeIcon;
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

                        var blueprint = _playerContext.BlueprintList
                            .FirstOrDefault(b =>
                                string.Equals(b.Name, importedName, StringComparison.Ordinal) &&
                                b.Evolution == importedEvo);

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
            return CreateMailListPageItem(0);
        }

        /// <summary>
        /// Creates a work item that fetches a page of the mail list at the given offset
        /// and cascades one detail item per mail plus a next page item if the page is non-empty.
        /// </summary>
        /// <param name="offset">The offset into the mail list for pagination.</param>
        /// <returns>A work item for mail list page retrieval with cascading.</returns>
        private WorkItem CreateMailListPageItem(int offset)
        {
            int pageSize = 50;
            string label = offset == 0 ? "MailList" : "MailList:page" + (offset / pageSize);

            return new WorkItem
            {
                Label = label,
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetMailListAsync(
                        _settings.AppId, _currentAccessToken, offset, pageSize).ConfigureAwait(false);

                    if (!result.Success)
                    {
                        Log.Warn("{0} fetch failed: {1}", label, result.Json);
                        return Array.Empty<WorkItem>();
                    }

                    Log.Debug("{0} fetched successfully.", label);

                    var envelope = JObject.Parse(result.Json);
                    var mails = envelope["data"]?["mail"] as JArray;
                    if (mails == null || mails.Count == 0)
                    {
                        return Array.Empty<WorkItem>();
                    }

                    var cascaded = new List<WorkItem>();

                    foreach (var mail in mails)
                    {
                        int mailId = mail["mailId"]?.Value<int>() ?? 0;
                        if (mailId > 0)
                        {
                            cascaded.Add(CreateMailDetailItem(mailId));
                        }
                    }

                    cascaded.Add(CreateMailListPageItem(offset + pageSize));

                    return cascaded.ToArray();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches the detail for a specific mail message.
        /// </summary>
        /// <param name="mailId">The mail identifier.</param>
        /// <returns>A work item for mail detail retrieval.</returns>
        private WorkItem CreateMailDetailItem(int mailId)
        {
            return new WorkItem
            {
                Label = "MailDetail:" + mailId,
                ExecuteAsync = async ct =>
                {
                    var result = await _apiClient.GetMailDetailAsync(
                        _settings.AppId, _currentAccessToken, mailId).ConfigureAwait(false);

                    if (result.Success)
                    {
                        Log.Debug("MailDetail:{0} fetched successfully.", mailId);
                    }
                    else
                    {
                        Log.Warn("MailDetail:{0} fetch failed: {1}", mailId, result.Json);
                    }

                    return Array.Empty<WorkItem>();
                },
            };
        }
    }
}
