// <copyright file="GameApiFullDiscoveryTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Common.Client;
using OE2EmpireTracker.Common.Client.Generated;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Comprehensive API data discovery test fixture for all game API read endpoints.
    /// Marked Explicit - requires real game API credentials via preferences/credential store.
    /// Produces raw JSON output organized by endpoint category.
    /// Uses GameApiRequestQueue for rate-limited parallel dispatch.
    /// Feature: game-api-discovery-tool
    /// </summary>
    [TestFixture]
    [Explicit("Requires real game API credentials configured in preferences")]
    public class GameApiFullDiscoveryTests
    {
        private GameApiClient client;
        private GameApiTypedClient typedClient;
        private string appId;
        private string clientId;
        private string secret;
        private string accessToken;
        private string playerUUID;
        private string outputDir;
        private ConcurrentBag<EndpointResult> results;
        private ConcurrentBag<string> bankingTransactionPages;
        private string assetLocationsJson;
        private SemaphoreSlim tokenRefreshSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Loads credentials from the app's existing credential store and preferences.
        /// Creates the output directory tree for storing raw JSON responses.
        /// Instantiates both the legacy GameApiClient and the new GameApiTypedClient.
        /// Uses the typed client for token exchange (sub-task 13.1, 13.2).
        /// Skips all tests if credentials are not configured.
        /// </summary>
        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            this.results = new ConcurrentBag<EndpointResult>();
            this.bankingTransactionPages = new ConcurrentBag<string>();

            GameApiCredentialManager.RegisterProtectionFunctions(
                CredentialStore.Protect,
                CredentialStore.Unprotect);

            var settings = PreferencesStore.GetInstance().Preferences.GameApiConnection;
            if (settings == null)
            {
                Assert.Ignore("Game API connection settings not found in preferences.");
            }

            if (string.IsNullOrEmpty(settings.AppId) || string.IsNullOrEmpty(settings.ClientId))
            {
                Assert.Ignore("Game API AppId or ClientId not configured in preferences.");
            }

            if (string.IsNullOrEmpty(settings.ServerUrl))
            {
                Assert.Ignore("Game API ServerUrl not configured in preferences.");
            }

            this.appId = settings.AppId;
            this.clientId = settings.ClientId;

            var credManager = new GameApiCredentialManager();
            var configuredPlayers = credManager.GetConfiguredPlayerUUIDs();
            if (configuredPlayers.Count == 0)
            {
                Assert.Ignore("No characters have configured game API secrets.");
            }

            this.playerUUID = configuredPlayers[0];
            var secureSecret = credManager.GetKey(this.playerUUID);
            if (secureSecret == null)
            {
                Assert.Ignore("No secret found for player " + this.playerUUID);
            }

            string secret = new System.Net.NetworkCredential(string.Empty, secureSecret).Password;
            this.secret = secret;

            // Legacy client kept for existing work items (Tasks 14-15 will migrate)
            this.client = new GameApiClient(settings.ServerUrl);

            // New typed client for auth and future endpoint migration
            var httpClient = new HttpClient { BaseAddress = new Uri(settings.ServerUrl) };
            this.typedClient = new GameApiTypedClient(httpClient, this.appId);

            this.outputDir = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "spec", "game-api-data"));

            EnsureDirectory(this.outputDir);
            EnsureDirectory(Path.Combine(this.outputDir, "auth"));
            EnsureDirectory(Path.Combine(this.outputDir, "character"));
            EnsureDirectory(Path.Combine(this.outputDir, "colonies"));
            EnsureDirectory(Path.Combine(this.outputDir, "banking"));
            EnsureDirectory(Path.Combine(this.outputDir, "assets"));
            EnsureDirectory(Path.Combine(this.outputDir, "jobs"));
            EnsureDirectory(Path.Combine(this.outputDir, "killmails"));
            EnsureDirectory(Path.Combine(this.outputDir, "mail"));
            EnsureDirectory(Path.Combine(this.outputDir, "ship"));
            EnsureDirectory(Path.Combine(this.outputDir, "market"));

            // Use typed client for token exchange (sub-task 13.2)
            try
            {
                var tokenResponse = await this.typedClient.ExchangeTokenAsync(this.appId, this.clientId, secret).ConfigureAwait(false);
                this.accessToken = tokenResponse.AccessToken;

                // Write token exchange response to auth/ directory (sub-task 15.2)
                string tokenJson = JsonConvert.SerializeObject(tokenResponse, Formatting.Indented);
                string tokenPath = Path.Combine(this.outputDir, "auth", "token-exchange.json");
                File.WriteAllText(tokenPath, tokenJson, Encoding.UTF8);
                this.RecordSuccess("auth", "token-exchange");
            }
            catch (ApiBusinessException ex)
            {
                Assert.Ignore("Token exchange failed: " + ex.ReturnString);
            }
            catch (Exception ex)
            {
                Assert.Ignore("Token exchange failed: " + ex.Message);
            }

            TestContext.WriteLine("Authenticated as player {0}, token obtained", this.playerUUID);
            TestContext.WriteLine("Output directory: {0}", this.outputDir);

            // Initialize metrics collector to track per-request timing
            GameApiMetricsCollector.Initialize();
        }

        /// <summary>
        /// Writes _metadata.json with run summary and disposes both clients.
        /// </summary>
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (this.results != null && this.outputDir != null)
            {
                int succeeded = this.results.Count(r => r.Success);
                int skipped = this.results.Count(r => !r.Success && !string.IsNullOrEmpty(r.SkipReason));
                int failed = this.results.Count(r => !r.Success && string.IsNullOrEmpty(r.SkipReason));

                var metadata = new
                {
                    runTimestamp = SystemClock.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    characterId = this.playerUUID,
                    endpointResults = new
                    {
                        succeeded,
                        skipped,
                        failed,
                    },
                    details = this.results.OrderBy(r => r.Timestamp).Select(r => new
                    {
                        category = r.Category,
                        endpoint = r.Endpoint,
                        success = r.Success,
                        httpStatus = r.HttpStatus,
                        reason = r.SkipReason,
                        timestamp = r.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    }).ToArray(),
                };

                string metadataPath = Path.Combine(this.outputDir, "_metadata.json");
                File.WriteAllText(
                    metadataPath,
                    JsonConvert.SerializeObject(metadata, Formatting.Indented),
                    Encoding.UTF8);

                TestContext.WriteLine("=== Discovery Run Summary ===");
                TestContext.WriteLine("Succeeded: {0}", succeeded);
                TestContext.WriteLine("Skipped:   {0}", skipped);
                TestContext.WriteLine("Failed:    {0}", failed);
                TestContext.WriteLine("Metadata written to: {0}", metadataPath);

                // Write per-endpoint latency percentiles from metrics collector
                WriteLatencyPercentiles();
            }

            this.typedClient?.Dispose();
            this.client?.Dispose();
        }

        /// <summary>
        /// Computes and writes P50/P90/P99/P100 latency percentiles per endpoint from the metrics collector history.
        /// </summary>
        private static void WriteLatencyPercentiles()
        {
            if (GameApiMetricsCollector.Instance == null)
            {
                return;
            }

            var history = GameApiMetricsCollector.Instance.GetHistory();
            if (history == null || history.Count == 0)
            {
                TestContext.WriteLine("No metrics history available for percentile analysis.");
                return;
            }

            var successfulByEndpoint = history
                .Where(r => r.IsSuccess)
                .GroupBy(r => r.Endpoint)
                .OrderBy(g => g.Key);

            TestContext.WriteLine("\n=== Latency Percentiles (successful requests only) ===");
            TestContext.WriteLine("{0,-45} {1,6} {2,8} {3,8} {4,8} {5,8}", "Endpoint", "Count", "P90ms", "P99ms", "P99.9ms", "P100ms");
            TestContext.WriteLine(new string('-', 95));

            foreach (var group in successfulByEndpoint)
            {
                var durations = group.Select(r => r.DurationMs).OrderBy(d => d).ToArray();
                int count = durations.Length;
                long p90 = durations[(int)(count * 0.90)];
                long p99 = durations[Math.Min((int)(count * 0.99), count - 1)];
                long p999 = durations[Math.Min((int)(count * 0.999), count - 1)];
                long p100 = durations[count - 1];

                TestContext.WriteLine("{0,-45} {1,6} {2,8} {3,8} {4,8} {5,8}", group.Key, count, p90, p99, p999, p100);
            }

            // Also write overall stats
            var allSuccessful = history.Where(r => r.IsSuccess).Select(r => r.DurationMs).OrderBy(d => d).ToArray();
            if (allSuccessful.Length > 0)
            {
                int total = allSuccessful.Length;
                TestContext.WriteLine(new string('-', 95));
                TestContext.WriteLine(
                    "{0,-45} {1,6} {2,8} {3,8} {4,8} {5,8}",
                    "ALL ENDPOINTS",
                    total,
                    allSuccessful[(int)(total * 0.90)],
                    allSuccessful[Math.Min((int)(total * 0.99), total - 1)],
                    allSuccessful[Math.Min((int)(total * 0.999), total - 1)],
                    allSuccessful[total - 1]);
            }
        }

        /// <summary>
        /// Runs all API discovery endpoints through a rate-limited queue at 10 TPS.
        /// Seed work items are enqueued, and cascading items are produced on completion.
        /// Work items still use the legacy GameApiClient — Tasks 14-15 will migrate these.
        /// </summary>
        [Test]
        public async Task RunQueuedDiscovery()
        {
            string metricsPath = Path.Combine(this.outputDir, "_metrics.csv");
            var queue = new GameApiRequestQueue(0.80, perItemTimeout: TimeSpan.FromMinutes(5), maxInflight: 9, metricsFilePath: metricsPath);

            // Set the client's internal rate limiter high enough that it doesn't
            // independently throttle — the queue controls dispatch rate.
            this.client.SetRateLimit(60);

            // === Task 15.1: Character, banking, jobs, ship seed items ===
            await queue.EnqueueAsync(new WorkItem
            {
                Label = "character/profile",
                ExecuteAsync = async ct =>
                {
                    var result = await this.client.GetCharacterAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    if (!result.Success && result.Json == "401")
                    {
                        await this.RefreshTokenAsync().ConfigureAwait(false);
                        result = await this.client.GetCharacterAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    }

                    if (result.Success)
                    {
                        string filePath = Path.Combine(this.outputDir, "character", "profile.json");
                        File.WriteAllText(filePath, FormatJson(result.Json), Encoding.UTF8);
                        RecordSuccess("character", "profile");
                    }
                    else
                    {
                        RecordSkipped("character", "profile", result.Json ?? "Request failed");
                    }

                    return Array.Empty<WorkItem>();
                },
            }).ConfigureAwait(false);

            await queue.EnqueueAsync(new WorkItem
            {
                Label = "character/skills",
                ExecuteAsync = async ct =>
                {
                    var result = await this.client.GetCharacterSkillsAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    if (!result.Success && result.Json == "401")
                    {
                        await this.RefreshTokenAsync().ConfigureAwait(false);
                        result = await this.client.GetCharacterSkillsAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    }

                    if (result.Success)
                    {
                        string filePath = Path.Combine(this.outputDir, "character", "skills.json");
                        File.WriteAllText(filePath, FormatJson(result.Json), Encoding.UTF8);
                        RecordSuccess("character", "skills");
                    }
                    else
                    {
                        RecordSkipped("character", "skills", result.Json ?? "Request failed");
                    }

                    return Array.Empty<WorkItem>();
                },
            }).ConfigureAwait(false);

            await queue.EnqueueAsync(new WorkItem
            {
                Label = "banking/balance",
                ExecuteAsync = async ct =>
                {
                    var result = await this.client.GetBankingBalanceAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    if (!result.Success && result.Json == "401")
                    {
                        await this.RefreshTokenAsync().ConfigureAwait(false);
                        result = await this.client.GetBankingBalanceAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    }

                    if (result.Success)
                    {
                        string filePath = Path.Combine(this.outputDir, "banking", "balance.json");
                        File.WriteAllText(filePath, FormatJson(result.Json), Encoding.UTF8);
                        RecordSuccess("banking", "balance");
                    }
                    else
                    {
                        RecordSkipped("banking", "balance", result.Json ?? "Request failed");
                    }

                    return Array.Empty<WorkItem>();
                },
            }).ConfigureAwait(false);

            await queue.EnqueueAsync(this.CreateBankingTransactionsPageWorkItem(0)).ConfigureAwait(false);

            await queue.EnqueueAsync(new WorkItem
            {
                Label = "jobs/accepted",
                ExecuteAsync = async ct =>
                {
                    var result = await this.client.GetAcceptedJobsAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    if (!result.Success && result.Json == "401")
                    {
                        await this.RefreshTokenAsync().ConfigureAwait(false);
                        result = await this.client.GetAcceptedJobsAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    }

                    if (result.Success)
                    {
                        string filePath = Path.Combine(this.outputDir, "jobs", "accepted.json");
                        File.WriteAllText(filePath, FormatJson(result.Json), Encoding.UTF8);
                        RecordSuccess("jobs", "accepted");
                    }
                    else
                    {
                        RecordSkipped("jobs", "accepted", result.Json ?? "Request failed");
                    }

                    return Array.Empty<WorkItem>();
                },
            }).ConfigureAwait(false);

            await queue.EnqueueAsync(new WorkItem
            {
                Label = "ship/configuration",
                ExecuteAsync = async ct =>
                {
                    var result = await this.client.GetShipConfigurationAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    if (!result.Success && result.Json == "401")
                    {
                        await this.RefreshTokenAsync().ConfigureAwait(false);
                        result = await this.client.GetShipConfigurationAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    }

                    if (result.Success)
                    {
                        string filePath = Path.Combine(this.outputDir, "ship", "configuration.json");
                        File.WriteAllText(filePath, FormatJson(result.Json), Encoding.UTF8);
                        RecordSuccess("ship", "configuration");
                    }
                    else
                    {
                        RecordSkipped("ship", "configuration", result.Json ?? "Request failed");
                    }

                    return Array.Empty<WorkItem>();
                },
            }).ConfigureAwait(false);

            await queue.EnqueueAsync(new WorkItem
            {
                Label = "ship/cargo",
                ExecuteAsync = async ct =>
                {
                    var result = await this.client.GetShipCargoAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    if (!result.Success && result.Json == "401")
                    {
                        await this.RefreshTokenAsync().ConfigureAwait(false);
                        result = await this.client.GetShipCargoAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    }

                    if (result.Success)
                    {
                        string filePath = Path.Combine(this.outputDir, "ship", "cargo.json");
                        File.WriteAllText(filePath, FormatJson(result.Json), Encoding.UTF8);
                        RecordSuccess("ship", "cargo");
                    }
                    else
                    {
                        RecordSkipped("ship", "cargo", result.Json ?? "Request failed");
                    }

                    return Array.Empty<WorkItem>();
                },
            }).ConfigureAwait(false);

            // === Task 15.2: Market seed items ===
            await queue.EnqueueAsync(new WorkItem
            {
                Label = "market/listings",
                ExecuteAsync = async ct =>
                {
                    var result = await this.client.GetMarketListingsAsync(this.appId, this.accessToken, "all").ConfigureAwait(false);
                    if (!result.Success && result.Json == "401")
                    {
                        await this.RefreshTokenAsync().ConfigureAwait(false);
                        result = await this.client.GetMarketListingsAsync(this.appId, this.accessToken, "all").ConfigureAwait(false);
                    }

                    if (!result.Success)
                    {
                        RecordSkipped("market", "listings", result.Json ?? "Request failed");
                        return Array.Empty<WorkItem>();
                    }

                    string filePath = Path.Combine(this.outputDir, "market", "listings.json");
                    File.WriteAllText(filePath, FormatJson(result.Json), Encoding.UTF8);
                    RecordSuccess("market", "listings");

                    var envelope = JObject.Parse(result.Json);
                    var listings = envelope["data"]?["listings"] as JArray;
                    if (listings == null || listings.Count == 0)
                    {
                        return Array.Empty<WorkItem>();
                    }

                    var cascaded = new List<WorkItem>();

                    // Cascade to prices using first listing's type/typeId
                    var firstListing = listings[0];
                    string listingType = firstListing["type"]?.Value<string>();
                    long listingTypeId = firstListing["typeId"]?.Value<long>() ?? 0;

                    if (!string.IsNullOrEmpty(listingType) && listingTypeId > 0)
                    {
                        string capturedType = listingType;
                        long capturedTypeId = listingTypeId;

                        cascaded.Add(new WorkItem
                        {
                            Label = "market/prices",
                            ExecuteAsync = async ct2 =>
                            {
                                var r = await this.client.GetMarketPricesAsync(this.appId, this.accessToken, capturedType, capturedTypeId).ConfigureAwait(false);
                                if (!r.Success && r.Json == "401")
                                {
                                    await this.RefreshTokenAsync().ConfigureAwait(false);
                                    r = await this.client.GetMarketPricesAsync(this.appId, this.accessToken, capturedType, capturedTypeId).ConfigureAwait(false);
                                }

                                if (r.Success)
                                {
                                    string pricePath = Path.Combine(this.outputDir, "market", "prices.json");
                                    File.WriteAllText(pricePath, FormatJson(r.Json), Encoding.UTF8);
                                    RecordSuccess("market", "prices");
                                }
                                else
                                {
                                    RecordSkipped("market", "prices", r.Json ?? "Request failed");
                                }

                                return Array.Empty<WorkItem>();
                            },
                        });
                    }

                    // Cascade to ship components using first ship listing's marketId
                    var firstShip = listings.FirstOrDefault(l => string.Equals(l["type"]?.Value<string>(), "Ship", StringComparison.OrdinalIgnoreCase));
                    if (firstShip != null)
                    {
                        long shipMarketId = firstShip["marketId"]?.Value<long>() ?? 0;
                        if (shipMarketId > 0)
                        {
                            long capturedShipMarketId = shipMarketId;

                            cascaded.Add(new WorkItem
                            {
                                Label = "market/ship-components",
                                ExecuteAsync = async ct2 =>
                                {
                                    var r = await this.client.GetMarketShipComponentsAsync(this.appId, this.accessToken, capturedShipMarketId).ConfigureAwait(false);
                                    if (!r.Success && r.Json == "401")
                                    {
                                        await this.RefreshTokenAsync().ConfigureAwait(false);
                                        r = await this.client.GetMarketShipComponentsAsync(this.appId, this.accessToken, capturedShipMarketId).ConfigureAwait(false);
                                    }

                                    if (r.Success)
                                    {
                                        string compPath = Path.Combine(this.outputDir, "market", "ship-components.json");
                                        File.WriteAllText(compPath, FormatJson(r.Json), Encoding.UTF8);
                                        RecordSuccess("market", "ship-components");
                                    }
                                    else
                                    {
                                        RecordSkipped("market", "ship-components", r.Json ?? "Request failed");
                                    }

                                    return Array.Empty<WorkItem>();
                                },
                            });
                        }
                    }

                    return cascaded;
                },
            }).ConfigureAwait(false);

            await queue.EnqueueAsync(new WorkItem
            {
                Label = "market/items",
                ExecuteAsync = async ct =>
                {
                    var result = await this.client.GetMarketItemsAsync(this.appId, this.accessToken, "R", "Halogens").ConfigureAwait(false);
                    if (!result.Success && result.Json == "401")
                    {
                        await this.RefreshTokenAsync().ConfigureAwait(false);
                        result = await this.client.GetMarketItemsAsync(this.appId, this.accessToken, "R", "Halogens").ConfigureAwait(false);
                    }

                    if (result.Success)
                    {
                        string filePath = Path.Combine(this.outputDir, "market", "items.json");
                        File.WriteAllText(filePath, FormatJson(result.Json), Encoding.UTF8);
                        RecordSuccess("market", "items");
                    }
                    else
                    {
                        RecordSkipped("market", "items", result.Json ?? "Request failed");
                    }

                    return Array.Empty<WorkItem>();
                },
            }).ConfigureAwait(false);

            await queue.EnqueueAsync(new WorkItem
            {
                Label = "market/buy-orders",
                ExecuteAsync = async ct =>
                {
                    var result = await this.client.GetMarketBuyOrdersAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    if (!result.Success && result.Json == "401")
                    {
                        await this.RefreshTokenAsync().ConfigureAwait(false);
                        result = await this.client.GetMarketBuyOrdersAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    }

                    if (!result.Success)
                    {
                        RecordSkipped("market", "buy-orders", result.Json ?? "Request failed");
                        return Array.Empty<WorkItem>();
                    }

                    string filePath = Path.Combine(this.outputDir, "market", "buy-orders.json");
                    File.WriteAllText(filePath, FormatJson(result.Json), Encoding.UTF8);
                    RecordSuccess("market", "buy-orders");

                    var envelope = JObject.Parse(result.Json);
                    var orders = envelope["data"]?["orders"] as JArray;
                    if (orders == null || orders.Count == 0)
                    {
                        return Array.Empty<WorkItem>();
                    }

                    var marketIds = orders
                        .Select(o => o["marketId"]?.Value<long>() ?? 0)
                        .Where(id => id > 0)
                        .Take(5)
                        .ToList();

                    if (marketIds.Count == 0)
                    {
                        return Array.Empty<WorkItem>();
                    }

                    string joinedIds = string.Join(",", marketIds);

                    return new List<WorkItem>
                    {
                        new WorkItem
                        {
                            Label = "market/buy-competitors",
                            ExecuteAsync = async ct2 =>
                            {
                                var r = await this.client.GetMarketBuyCompetitorsAsync(this.appId, this.accessToken, joinedIds).ConfigureAwait(false);
                                if (!r.Success && r.Json == "401")
                                {
                                    await this.RefreshTokenAsync().ConfigureAwait(false);
                                    r = await this.client.GetMarketBuyCompetitorsAsync(this.appId, this.accessToken, joinedIds).ConfigureAwait(false);
                                }

                                if (r.Success)
                                {
                                    string compPath = Path.Combine(this.outputDir, "market", "buy-competitors.json");
                                    File.WriteAllText(compPath, FormatJson(r.Json), Encoding.UTF8);
                                    RecordSuccess("market", "buy-competitors");
                                }
                                else
                                {
                                    RecordSkipped("market", "buy-competitors", r.Json ?? "Request failed");
                                }

                                return Array.Empty<WorkItem>();
                            },
                        },
                    };
                },
            }).ConfigureAwait(false);

            await queue.EnqueueAsync(new WorkItem
            {
                Label = "market/sell-orders",
                ExecuteAsync = async ct =>
                {
                    var result = await this.client.GetMarketSellOrdersAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    if (!result.Success && result.Json == "401")
                    {
                        await this.RefreshTokenAsync().ConfigureAwait(false);
                        result = await this.client.GetMarketSellOrdersAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    }

                    if (!result.Success)
                    {
                        RecordSkipped("market", "sell-orders", result.Json ?? "Request failed");
                        return Array.Empty<WorkItem>();
                    }

                    string filePath = Path.Combine(this.outputDir, "market", "sell-orders.json");
                    File.WriteAllText(filePath, FormatJson(result.Json), Encoding.UTF8);
                    RecordSuccess("market", "sell-orders");

                    var envelope = JObject.Parse(result.Json);
                    var orders = envelope["data"]?["orders"] as JArray;
                    if (orders == null || orders.Count == 0)
                    {
                        return Array.Empty<WorkItem>();
                    }

                    var marketIds = orders
                        .Select(o => o["marketId"]?.Value<long>() ?? 0)
                        .Where(id => id > 0)
                        .Take(5)
                        .ToList();

                    if (marketIds.Count == 0)
                    {
                        return Array.Empty<WorkItem>();
                    }

                    string joinedIds = string.Join(",", marketIds);

                    return new List<WorkItem>
                    {
                        new WorkItem
                        {
                            Label = "market/sell-competitors",
                            ExecuteAsync = async ct2 =>
                            {
                                var r = await this.client.GetMarketSellCompetitorsAsync(this.appId, this.accessToken, joinedIds).ConfigureAwait(false);
                                if (!r.Success && r.Json == "401")
                                {
                                    await this.RefreshTokenAsync().ConfigureAwait(false);
                                    r = await this.client.GetMarketSellCompetitorsAsync(this.appId, this.accessToken, joinedIds).ConfigureAwait(false);
                                }

                                if (r.Success)
                                {
                                    string compPath = Path.Combine(this.outputDir, "market", "sell-competitors.json");
                                    File.WriteAllText(compPath, FormatJson(r.Json), Encoding.UTF8);
                                    RecordSuccess("market", "sell-competitors");
                                }
                                else
                                {
                                    RecordSkipped("market", "sell-competitors", r.Json ?? "Request failed");
                                }

                                return Array.Empty<WorkItem>();
                            },
                        },
                    };
                },
            }).ConfigureAwait(false);

            // === Task 15.3: Cascading seed items (colonies, assets, killmails, mail) ===
            await queue.EnqueueAsync(new WorkItem
            {
                Label = "colonies/list",
                ExecuteAsync = async ct =>
                {
                    var result = await this.client.GetColonyListAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    if (!result.Success && result.Json == "401")
                    {
                        await this.RefreshTokenAsync().ConfigureAwait(false);
                        result = await this.client.GetColonyListAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    }

                    if (!result.Success)
                    {
                        RecordSkipped("colonies", "list", result.Json ?? "Request failed");
                        return Array.Empty<WorkItem>();
                    }

                    string filePath = Path.Combine(this.outputDir, "colonies", "list.json");
                    File.WriteAllText(filePath, FormatJson(result.Json), Encoding.UTF8);
                    RecordSuccess("colonies", "list");

                    var envelope = JObject.Parse(result.Json);
                    var colonies = envelope["data"]?["colonies"] as JArray;
                    if (colonies == null || colonies.Count == 0)
                    {
                        return Array.Empty<WorkItem>();
                    }

                    var cascaded = new List<WorkItem>();
                    foreach (var colony in colonies)
                    {
                        int colonyId = colony["colonyId"]?.Value<int>() ?? 0;
                        if (colonyId == 0)
                        {
                            continue;
                        }

                        string colonyDir = Path.Combine(this.outputDir, "colonies", colonyId.ToString());
                        EnsureDirectory(colonyDir);

                        int capturedId = colonyId;

                        cascaded.Add(new WorkItem
                        {
                            Label = $"colonies/{capturedId}/summary",
                            ExecuteAsync = async ct2 =>
                            {
                                var r = await this.client.GetColonySummaryAsync(this.appId, this.accessToken, capturedId).ConfigureAwait(false);
                                if (!r.Success && r.Json == "401")
                                {
                                    await this.RefreshTokenAsync().ConfigureAwait(false);
                                    r = await this.client.GetColonySummaryAsync(this.appId, this.accessToken, capturedId).ConfigureAwait(false);
                                }

                                if (r.Success)
                                {
                                    File.WriteAllText(Path.Combine(colonyDir, "summary.json"), FormatJson(r.Json), Encoding.UTF8);
                                    RecordSuccess("colonies", $"{capturedId}/summary");
                                }
                                else
                                {
                                    RecordSkipped("colonies", $"{capturedId}/summary", r.Json ?? "Request failed");
                                }

                                return Array.Empty<WorkItem>();
                            },
                        });

                        cascaded.Add(new WorkItem
                        {
                            Label = $"colonies/{capturedId}/buildings",
                            ExecuteAsync = async ct2 =>
                            {
                                var r = await this.client.GetColonyBuildingsAsync(this.appId, this.accessToken, capturedId).ConfigureAwait(false);
                                if (!r.Success && r.Json == "401")
                                {
                                    await this.RefreshTokenAsync().ConfigureAwait(false);
                                    r = await this.client.GetColonyBuildingsAsync(this.appId, this.accessToken, capturedId).ConfigureAwait(false);
                                }

                                if (r.Success)
                                {
                                    File.WriteAllText(Path.Combine(colonyDir, "buildings.json"), FormatJson(r.Json), Encoding.UTF8);
                                    RecordSuccess("colonies", $"{capturedId}/buildings");
                                }
                                else
                                {
                                    RecordSkipped("colonies", $"{capturedId}/buildings", r.Json ?? "Request failed");
                                }

                                return Array.Empty<WorkItem>();
                            },
                        });

                        cascaded.Add(new WorkItem
                        {
                            Label = $"colonies/{capturedId}/warehouse",
                            ExecuteAsync = async ct2 =>
                            {
                                var r = await this.client.GetColonyWarehouseAsync(this.appId, this.accessToken, capturedId).ConfigureAwait(false);
                                if (!r.Success && r.Json == "401")
                                {
                                    await this.RefreshTokenAsync().ConfigureAwait(false);
                                    r = await this.client.GetColonyWarehouseAsync(this.appId, this.accessToken, capturedId).ConfigureAwait(false);
                                }

                                if (r.Success)
                                {
                                    File.WriteAllText(Path.Combine(colonyDir, "warehouse.json"), FormatJson(r.Json), Encoding.UTF8);
                                    RecordSuccess("colonies", $"{capturedId}/warehouse");
                                }
                                else
                                {
                                    RecordSkipped("colonies", $"{capturedId}/warehouse", r.Json ?? "Request failed");
                                }

                                return Array.Empty<WorkItem>();
                            },
                        });

                        cascaded.Add(new WorkItem
                        {
                            Label = $"colonies/{capturedId}/workers",
                            ExecuteAsync = async ct2 =>
                            {
                                var r = await this.client.GetColonyWorkersAsync(this.appId, this.accessToken, capturedId).ConfigureAwait(false);
                                if (!r.Success && r.Json == "401")
                                {
                                    await this.RefreshTokenAsync().ConfigureAwait(false);
                                    r = await this.client.GetColonyWorkersAsync(this.appId, this.accessToken, capturedId).ConfigureAwait(false);
                                }

                                if (r.Success)
                                {
                                    File.WriteAllText(Path.Combine(colonyDir, "workers.json"), FormatJson(r.Json), Encoding.UTF8);
                                    RecordSuccess("colonies", $"{capturedId}/workers");
                                }
                                else
                                {
                                    RecordSkipped("colonies", $"{capturedId}/workers", r.Json ?? "Request failed");
                                }

                                return Array.Empty<WorkItem>();
                            },
                        });
                    }

                    return cascaded;
                },
            }).ConfigureAwait(false);

            await queue.EnqueueAsync(new WorkItem
            {
                Label = "assets/locations",
                ExecuteAsync = async ct =>
                {
                    var result = await this.client.GetAssetLocationsAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    if (!result.Success && result.Json == "401")
                    {
                        await this.RefreshTokenAsync().ConfigureAwait(false);
                        result = await this.client.GetAssetLocationsAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    }

                    if (!result.Success)
                    {
                        RecordSkipped("assets", "locations", result.Json ?? "Request failed");
                        return Array.Empty<WorkItem>();
                    }

                    string filePath = Path.Combine(this.outputDir, "assets", "locations.json");
                    File.WriteAllText(filePath, FormatJson(result.Json), Encoding.UTF8);
                    RecordSuccess("assets", "locations");
                    this.assetLocationsJson = result.Json;

                    var envelope = JObject.Parse(result.Json);
                    var locations = envelope["data"]?["locations"] as JArray;
                    if (locations == null || locations.Count == 0)
                    {
                        return Array.Empty<WorkItem>();
                    }

                    var cascaded = new List<WorkItem>();
                    foreach (var location in locations)
                    {
                        int locationId = location["locationId"]?.Value<int>() ?? 0;
                        string locationType = location["locationType"]?.Value<string>();

                        if (locationId == 0 || string.IsNullOrEmpty(locationType))
                        {
                            continue;
                        }

                        int capturedId = locationId;
                        string capturedType = locationType;

                        cascaded.Add(new WorkItem
                        {
                            Label = $"assets/{capturedType}-{capturedId}",
                            ExecuteAsync = async ct2 =>
                            {
                                var r = await this.client.GetAssetLocationDetailAsync(this.appId, this.accessToken, capturedId, capturedType).ConfigureAwait(false);
                                if (!r.Success && r.Json == "401")
                                {
                                    await this.RefreshTokenAsync().ConfigureAwait(false);
                                    r = await this.client.GetAssetLocationDetailAsync(this.appId, this.accessToken, capturedId, capturedType).ConfigureAwait(false);
                                }

                                if (!r.Success)
                                {
                                    RecordSkipped("assets", $"{capturedType}-{capturedId}", r.Json ?? "Request failed");
                                    return Array.Empty<WorkItem>();
                                }

                                string detailPath = Path.Combine(this.outputDir, "assets", $"{capturedType}-{capturedId}.json");
                                File.WriteAllText(detailPath, FormatJson(r.Json), Encoding.UTF8);
                                RecordSuccess("assets", $"{capturedType}-{capturedId}");

                                var detail = JObject.Parse(r.Json);
                                var cargo = detail["data"]?["cargo"] as JArray;
                                if (cargo == null || cargo.Count == 0)
                                {
                                    return Array.Empty<WorkItem>();
                                }

                                var cargoItems = new List<WorkItem>();
                                foreach (var item in cargo)
                                {
                                    string itemTypeC = item["typeC"]?.Value<string>()?.Trim();
                                    int cargoItemId = item["id"]?.Value<int>() ?? 0;
                                    if (cargoItemId == 0 || string.IsNullOrEmpty(itemTypeC))
                                    {
                                        continue;
                                    }

                                    int capturedItemId = cargoItemId;

                                    if (itemTypeC == "Cr")
                                    {
                                        cargoItems.Add(new WorkItem
                                        {
                                            Label = $"assets/crate-{capturedItemId}",
                                            ExecuteAsync = async ct3 =>
                                            {
                                                var cr = await this.client.GetAssetCrateAsync(this.appId, this.accessToken, capturedItemId).ConfigureAwait(false);
                                                if (!cr.Success && cr.Json == "401")
                                                {
                                                    await this.RefreshTokenAsync().ConfigureAwait(false);
                                                    cr = await this.client.GetAssetCrateAsync(this.appId, this.accessToken, capturedItemId).ConfigureAwait(false);
                                                }

                                                if (!cr.Success)
                                                {
                                                    RecordSkipped("assets", $"crate-{capturedItemId}", cr.Json ?? "Request failed");
                                                    return Array.Empty<WorkItem>();
                                                }

                                                string cratePath = Path.Combine(this.outputDir, "assets", $"crate-{capturedItemId}.json");
                                                File.WriteAllText(cratePath, FormatJson(cr.Json), Encoding.UTF8);
                                                RecordSuccess("assets", $"crate-{capturedItemId}");

                                                // Cascade: parse crate contents for nested blueprints/surveys
                                                var crateEnvelope = JObject.Parse(cr.Json);
                                                var crateCargo = crateEnvelope["data"]?["cargo"] as JArray;
                                                if (crateCargo == null || crateCargo.Count == 0)
                                                {
                                                    return Array.Empty<WorkItem>();
                                                }

                                                var crateCascade = new List<WorkItem>();
                                                foreach (var crateItem in crateCargo)
                                                {
                                                    string crateItemTypeC = crateItem["typeC"]?.Value<string>()?.Trim();
                                                    int crateItemId = crateItem["id"]?.Value<int>() ?? 0;
                                                    if (crateItemId == 0 || string.IsNullOrEmpty(crateItemTypeC))
                                                    {
                                                        continue;
                                                    }

                                                    int capturedCrateItemId = crateItemId;

                                                    if (crateItemTypeC == "Bp")
                                                    {
                                                        crateCascade.Add(new WorkItem
                                                        {
                                                            Label = $"assets/blueprint-{capturedCrateItemId}",
                                                            ExecuteAsync = async ct4 =>
                                                            {
                                                                var bp2 = await this.client.GetAssetBlueprintAsync(this.appId, this.accessToken, capturedCrateItemId).ConfigureAwait(false);
                                                                if (!bp2.Success && bp2.Json == "401")
                                                                {
                                                                    await this.RefreshTokenAsync().ConfigureAwait(false);
                                                                    bp2 = await this.client.GetAssetBlueprintAsync(this.appId, this.accessToken, capturedCrateItemId).ConfigureAwait(false);
                                                                }

                                                                if (bp2.Success)
                                                                {
                                                                    string bpPath2 = Path.Combine(this.outputDir, "assets", $"blueprint-{capturedCrateItemId}.json");
                                                                    File.WriteAllText(bpPath2, FormatJson(bp2.Json), Encoding.UTF8);
                                                                    RecordSuccess("assets", $"blueprint-{capturedCrateItemId}");
                                                                }
                                                                else
                                                                {
                                                                    RecordSkipped("assets", $"blueprint-{capturedCrateItemId}", bp2.Json ?? "Request failed");
                                                                }

                                                                return Array.Empty<WorkItem>();
                                                            },
                                                        });
                                                    }
                                                    else if (crateItemTypeC == "Sc")
                                                    {
                                                        crateCascade.Add(new WorkItem
                                                        {
                                                            Label = $"assets/survey-{capturedCrateItemId}",
                                                            ExecuteAsync = async ct4 =>
                                                            {
                                                                var sr2 = await this.client.GetAssetSurveyAsync(this.appId, this.accessToken, capturedCrateItemId).ConfigureAwait(false);
                                                                if (!sr2.Success && sr2.Json == "401")
                                                                {
                                                                    await this.RefreshTokenAsync().ConfigureAwait(false);
                                                                    sr2 = await this.client.GetAssetSurveyAsync(this.appId, this.accessToken, capturedCrateItemId).ConfigureAwait(false);
                                                                }

                                                                if (sr2.Success)
                                                                {
                                                                    string surveyPath2 = Path.Combine(this.outputDir, "assets", $"survey-{capturedCrateItemId}.json");
                                                                    File.WriteAllText(surveyPath2, FormatJson(sr2.Json), Encoding.UTF8);
                                                                    RecordSuccess("assets", $"survey-{capturedCrateItemId}");
                                                                }
                                                                else
                                                                {
                                                                    RecordSkipped("assets", $"survey-{capturedCrateItemId}", sr2.Json ?? "Request failed");
                                                                }

                                                                return Array.Empty<WorkItem>();
                                                            },
                                                        });
                                                    }
                                                }

                                                return crateCascade;
                                            },
                                        });
                                    }
                                    else if (itemTypeC == "Sc")
                                    {
                                        cargoItems.Add(new WorkItem
                                        {
                                            Label = $"assets/survey-{capturedItemId}",
                                            ExecuteAsync = async ct3 =>
                                            {
                                                var sr = await this.client.GetAssetSurveyAsync(this.appId, this.accessToken, capturedItemId).ConfigureAwait(false);
                                                if (!sr.Success && sr.Json == "401")
                                                {
                                                    await this.RefreshTokenAsync().ConfigureAwait(false);
                                                    sr = await this.client.GetAssetSurveyAsync(this.appId, this.accessToken, capturedItemId).ConfigureAwait(false);
                                                }

                                                if (sr.Success)
                                                {
                                                    string surveyPath = Path.Combine(this.outputDir, "assets", $"survey-{capturedItemId}.json");
                                                    File.WriteAllText(surveyPath, FormatJson(sr.Json), Encoding.UTF8);
                                                    RecordSuccess("assets", $"survey-{capturedItemId}");
                                                }
                                                else
                                                {
                                                    RecordSkipped("assets", $"survey-{capturedItemId}", sr.Json ?? "Request failed");
                                                }

                                                return Array.Empty<WorkItem>();
                                            },
                                        });
                                    }
                                    else if (itemTypeC == "Bp")
                                    {
                                        cargoItems.Add(new WorkItem
                                        {
                                            Label = $"assets/blueprint-{capturedItemId}",
                                            ExecuteAsync = async ct3 =>
                                            {
                                                var bp = await this.client.GetAssetBlueprintAsync(this.appId, this.accessToken, capturedItemId).ConfigureAwait(false);
                                                if (!bp.Success && bp.Json == "401")
                                                {
                                                    await this.RefreshTokenAsync().ConfigureAwait(false);
                                                    bp = await this.client.GetAssetBlueprintAsync(this.appId, this.accessToken, capturedItemId).ConfigureAwait(false);
                                                }

                                                if (bp.Success)
                                                {
                                                    string bpPath = Path.Combine(this.outputDir, "assets", $"blueprint-{capturedItemId}.json");
                                                    File.WriteAllText(bpPath, FormatJson(bp.Json), Encoding.UTF8);
                                                    RecordSuccess("assets", $"blueprint-{capturedItemId}");
                                                }
                                                else
                                                {
                                                    RecordSkipped("assets", $"blueprint-{capturedItemId}", bp.Json ?? "Request failed");
                                                }

                                                return Array.Empty<WorkItem>();
                                            },
                                        });
                                    }
                                }

                                return cargoItems;
                            },
                        });
                    }

                    return cascaded;
                },
            }).ConfigureAwait(false);

            await queue.EnqueueAsync(new WorkItem
            {
                Label = "killmails/list",
                ExecuteAsync = async ct =>
                {
                    var result = await this.client.GetKillMailListAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    if (!result.Success && result.Json == "401")
                    {
                        await this.RefreshTokenAsync().ConfigureAwait(false);
                        result = await this.client.GetKillMailListAsync(this.appId, this.accessToken).ConfigureAwait(false);
                    }

                    if (!result.Success)
                    {
                        RecordSkipped("killmails", "list", result.Json ?? "Request failed");
                        return Array.Empty<WorkItem>();
                    }

                    string filePath = Path.Combine(this.outputDir, "killmails", "list.json");
                    File.WriteAllText(filePath, FormatJson(result.Json), Encoding.UTF8);
                    RecordSuccess("killmails", "list");

                    var envelope = JObject.Parse(result.Json);
                    var killMails = envelope["data"]?["killMails"] as JArray;
                    if (killMails == null || killMails.Count == 0)
                    {
                        return Array.Empty<WorkItem>();
                    }

                    var cascaded = new List<WorkItem>();
                    foreach (var km in killMails)
                    {
                        int killMailId = km["killMailId"]?.Value<int>() ?? 0;
                        if (killMailId == 0)
                        {
                            continue;
                        }

                        int capturedKmId = killMailId;

                        cascaded.Add(new WorkItem
                        {
                            Label = $"killmails/{capturedKmId}",
                            ExecuteAsync = async ct2 =>
                            {
                                var r = await this.client.GetKillMailDetailAsync(this.appId, this.accessToken, capturedKmId).ConfigureAwait(false);
                                if (!r.Success && r.Json == "401")
                                {
                                    await this.RefreshTokenAsync().ConfigureAwait(false);
                                    r = await this.client.GetKillMailDetailAsync(this.appId, this.accessToken, capturedKmId).ConfigureAwait(false);
                                }

                                if (r.Success)
                                {
                                    string kmPath = Path.Combine(this.outputDir, "killmails", $"{capturedKmId}.json");
                                    File.WriteAllText(kmPath, FormatJson(r.Json), Encoding.UTF8);
                                    RecordSuccess("killmails", capturedKmId.ToString());
                                }
                                else
                                {
                                    RecordSkipped("killmails", capturedKmId.ToString(), r.Json ?? "Request failed");
                                }

                                return Array.Empty<WorkItem>();
                            },
                        });
                    }

                    return cascaded;
                },
            }).ConfigureAwait(false);

            await queue.EnqueueAsync(this.CreateMailPageWorkItem(0)).ConfigureAwait(false);

            queue.Start();
            await queue.DrainAsync().ConfigureAwait(false);

            // Combine all banking transaction pages into a single file
            CombineBankingTransactionPages();

            // Collect queue errors as skipped entries so they appear in metadata
            foreach (var error in queue.Errors)
            {
                RecordSkipped("queue-error", error.WorkItemLabel, error.Exception.Message);
            }

            // Write latency percentiles to test output
            WriteLatencyPercentiles();
        }

        /// <summary>
        /// Creates a work item that fetches a page of banking transactions and cascades to the next page if non-empty.
        /// </summary>
        /// <param name="page">The zero-based page number to fetch.</param>
        /// <returns>A work item for the specified banking transactions page.</returns>
        private WorkItem CreateBankingTransactionsPageWorkItem(int page)
        {
            return new WorkItem
            {
                Label = $"banking/transactions-page-{page}",
                ExecuteAsync = async ct =>
                {
                    var result = await this.client.GetBankingTransactionsAsync(this.appId, this.accessToken, page * 50, 50).ConfigureAwait(false);
                    if (!result.Success && result.Json == "401")
                    {
                        await this.RefreshTokenAsync().ConfigureAwait(false);
                        result = await this.client.GetBankingTransactionsAsync(this.appId, this.accessToken, page * 50, 50).ConfigureAwait(false);
                    }

                    if (!result.Success)
                    {
                        RecordSkipped("banking", $"transactions-page-{page}", result.Json ?? "Request failed");
                        return Array.Empty<WorkItem>();
                    }

                    string filePath = Path.Combine(this.outputDir, "banking", $"transactions-page-{page}.json");
                    File.WriteAllText(filePath, FormatJson(result.Json), Encoding.UTF8);
                    RecordSuccess("banking", $"transactions-page-{page}");
                    this.bankingTransactionPages.Add(filePath);

                    var envelope = JObject.Parse(result.Json);
                    var transactions = envelope["data"]?["transactions"] as JArray;
                    if (transactions == null || transactions.Count == 0)
                    {
                        return Array.Empty<WorkItem>();
                    }

                    return new List<WorkItem> { this.CreateBankingTransactionsPageWorkItem(page + 1) };
                },
            };
        }

        /// <summary>
        /// Combines all fetched banking transaction pages into a single transactions-all.json file.
        /// </summary>
        private void CombineBankingTransactionPages()
        {
            var allTransactions = new JArray();

            foreach (string pagePath in this.bankingTransactionPages.OrderBy(p => p))
            {
                if (!File.Exists(pagePath))
                {
                    continue;
                }

                string json = File.ReadAllText(pagePath, Encoding.UTF8);
                var envelope = JObject.Parse(json);
                var transactions = envelope["data"]?["transactions"] as JArray;
                if (transactions != null)
                {
                    foreach (var tx in transactions)
                    {
                        allTransactions.Add(tx);
                    }
                }
            }

            var combined = new JObject
            {
                ["data"] = new JObject
                {
                    ["transactions"] = allTransactions,
                },
            };

            string allPath = Path.Combine(this.outputDir, "banking", "transactions-all.json");
            File.WriteAllText(allPath, JsonConvert.SerializeObject(combined, Formatting.Indented), Encoding.UTF8);
            TestContext.WriteLine("[COMBINED] banking/transactions-all.json ({0} transactions)", allTransactions.Count);
        }

        /// <summary>
        /// Creates a work item that fetches a page of mail and cascades to detail items and the next page.
        /// </summary>
        /// <param name="page">The zero-based page number to fetch.</param>
        /// <returns>A work item for the specified mail page.</returns>
        private WorkItem CreateMailPageWorkItem(int page)
        {
            return new WorkItem
            {
                Label = $"mail/list-p{page}",
                ExecuteAsync = async ct =>
                {
                    var result = await this.client.GetMailListAsync(this.appId, this.accessToken, page * 50, 50).ConfigureAwait(false);
                    if (!result.Success && result.Json == "401")
                    {
                        await this.RefreshTokenAsync().ConfigureAwait(false);
                        result = await this.client.GetMailListAsync(this.appId, this.accessToken, page * 50, 50).ConfigureAwait(false);
                    }

                    if (!result.Success)
                    {
                        RecordSkipped("mail", $"list-p{page}", result.Json ?? "Request failed");
                        return Array.Empty<WorkItem>();
                    }

                    string filePath = Path.Combine(this.outputDir, "mail", $"list-p{page}.json");
                    File.WriteAllText(filePath, FormatJson(result.Json), Encoding.UTF8);
                    RecordSuccess("mail", $"list-p{page}");

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
                        if (mailId == 0)
                        {
                            continue;
                        }

                        int capturedMailId = mailId;

                        cascaded.Add(new WorkItem
                        {
                            Label = $"mail/{capturedMailId}",
                            ExecuteAsync = async ct2 =>
                            {
                                var r = await this.client.GetMailDetailAsync(this.appId, this.accessToken, capturedMailId).ConfigureAwait(false);
                                if (!r.Success && r.Json == "401")
                                {
                                    await this.RefreshTokenAsync().ConfigureAwait(false);
                                    r = await this.client.GetMailDetailAsync(this.appId, this.accessToken, capturedMailId).ConfigureAwait(false);
                                }

                                if (r.Success)
                                {
                                    string mailPath = Path.Combine(this.outputDir, "mail", $"{capturedMailId}.json");
                                    File.WriteAllText(mailPath, FormatJson(r.Json), Encoding.UTF8);
                                    RecordSuccess("mail", capturedMailId.ToString());
                                }
                                else
                                {
                                    RecordSkipped("mail", capturedMailId.ToString(), r.Json ?? "Request failed");
                                }

                                return Array.Empty<WorkItem>();
                            },
                        });
                    }

                    cascaded.Add(this.CreateMailPageWorkItem(page + 1));

                    return cascaded;
                },
            };
        }

        /// <summary>
        /// Runs the typed-client asset crawl path through a rate-limited queue.
        /// Seed: GetAssetLocationsAsync → cascades to location detail per location →
        /// cascades to crate/blueprint/survey detail per asset found.
        /// The typed client's internal TokenBucketRateLimiter enforces 0.9 TPS and 9 concurrent.
        /// The queue TPS is set generous (10) so the client's own limiter governs throughput.
        /// Sub-task 14.1: cascading pattern implementation.
        /// Sub-task 14.2: 0.5 TPS and 30 concurrent limits validated by typed client internals.
        /// </summary>
        [Test]
        [Explicit("Requires real game API credentials configured in preferences")]
        public async Task RunTypedAssetDiscovery()
        {
            string metricsPath = Path.Combine(this.outputDir, "_typed-asset-metrics.csv");
            var queue = new GameApiRequestQueue(
                10.0,
                perItemTimeout: TimeSpan.FromMinutes(5),
                maxInflight: 9,
                metricsFilePath: metricsPath);

            EnsureDirectory(Path.Combine(this.outputDir, "assets", "locations"));
            EnsureDirectory(Path.Combine(this.outputDir, "assets", "crates"));
            EnsureDirectory(Path.Combine(this.outputDir, "assets", "blueprints"));
            EnsureDirectory(Path.Combine(this.outputDir, "assets", "surveys"));

            await queue.EnqueueAsync(new WorkItem
            {
                Label = "assets/locations",
                ExecuteAsync = async ct =>
                {
                    AssetLocations locations;
                    try
                    {
                        locations = await this.typedClient.GetAssetLocationsAsync(ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await this.RefreshTokenAsync().ConfigureAwait(false);
                        locations = await this.typedClient.GetAssetLocationsAsync(ct).ConfigureAwait(false);
                    }

                    string json = JsonConvert.SerializeObject(locations, Formatting.Indented);
                    string filePath = Path.Combine(this.outputDir, "assets", "locations", "all.json");
                    File.WriteAllText(filePath, json, Encoding.UTF8);
                    this.RecordSuccess("assets", "locations");

                    if (locations?.Locations == null || locations.Locations.Count == 0)
                    {
                        return Array.Empty<WorkItem>();
                    }

                    var cascaded = new List<WorkItem>();
                    foreach (var loc in locations.Locations)
                    {
                        int capturedId = loc.LocationId;
                        string capturedType = loc.LocationType;
                        string capturedName = loc.LocationName ?? capturedId.ToString();

                        cascaded.Add(this.CreateTypedLocationDetailWorkItem(capturedId, capturedType, capturedName));
                    }

                    return cascaded;
                },
            }).ConfigureAwait(false);

            queue.Start();
            await queue.DrainAsync().ConfigureAwait(false);

            foreach (var error in queue.Errors)
            {
                this.RecordSkipped("queue-error", error.WorkItemLabel, error.Exception.Message);
            }

            // Sub-task 15.1: Output per-category summary for the typed asset discovery run
            var typedResults = this.results
                .Where(r => r.Category.StartsWith("assets", StringComparison.Ordinal))
                .GroupBy(r => r.Category)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Category = g.Key,
                    Succeeded = g.Count(r => r.Success),
                    Failed = g.Count(r => !r.Success),
                });

            TestContext.WriteLine("\n=== Typed Asset Discovery Summary ===");
            foreach (var cat in typedResults)
            {
                TestContext.WriteLine("{0}: {1} succeeded, {2} failed", cat.Category, cat.Succeeded, cat.Failed);
            }

            TestContext.WriteLine("=== Typed Asset Discovery Complete ===");
        }

        /// <summary>
        /// Creates a work item that fetches location detail for a specific asset location
        /// and cascades to crate/blueprint/survey detail items based on cargo typeC codes.
        /// </summary>
        /// <param name="locationId">The location ID to fetch detail for.</param>
        /// <param name="locationType">The location type string (e.g. "Station", "Colony").</param>
        /// <param name="locationName">Human-readable location name for labeling.</param>
        /// <returns>A work item for fetching location detail.</returns>
        private WorkItem CreateTypedLocationDetailWorkItem(int locationId, string locationType, string locationName)
        {
            return new WorkItem
            {
                Label = $"assets/location-{locationId}",
                ExecuteAsync = async ct =>
                {
                    AssetLocationDetail detail;
                    try
                    {
                        detail = await this.typedClient.GetAssetLocationDetailAsync(locationId, locationType, ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await this.RefreshTokenAsync().ConfigureAwait(false);
                        detail = await this.typedClient.GetAssetLocationDetailAsync(locationId, locationType, ct).ConfigureAwait(false);
                    }

                    string json = JsonConvert.SerializeObject(detail, Formatting.Indented);
                    string filePath = Path.Combine(this.outputDir, "assets", "locations", $"{locationId}.json");
                    File.WriteAllText(filePath, json, Encoding.UTF8);
                    this.RecordSuccess("assets", $"location-{locationId}");

                    var cascaded = new List<WorkItem>();

                    if (detail?.Cargo != null)
                    {
                        foreach (var item in detail.Cargo)
                        {
                            if (item.Id <= 0 || string.IsNullOrEmpty(item.TypeC))
                            {
                                continue;
                            }

                            int capturedItemId = item.Id;
                            string typeCode = item.TypeC.Trim();

                            if (string.Equals(typeCode, "Cr", StringComparison.OrdinalIgnoreCase))
                            {
                                cascaded.Add(this.CreateTypedCrateWorkItem(capturedItemId));
                            }
                            else if (string.Equals(typeCode, "Bp", StringComparison.OrdinalIgnoreCase))
                            {
                                cascaded.Add(this.CreateTypedBlueprintWorkItem(capturedItemId));
                            }
                            else if (string.Equals(typeCode, "Sc", StringComparison.OrdinalIgnoreCase))
                            {
                                cascaded.Add(this.CreateTypedSurveyWorkItem(capturedItemId));
                            }
                        }
                    }

                    return cascaded;
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches crate contents via the typed client.
        /// </summary>
        /// <param name="crateId">The crate ID to fetch.</param>
        /// <returns>A work item for fetching crate contents.</returns>
        private WorkItem CreateTypedCrateWorkItem(int crateId)
        {
            return new WorkItem
            {
                Label = $"assets/crate-{crateId}",
                ExecuteAsync = async ct =>
                {
                    AssetCrateContents contents;
                    try
                    {
                        contents = await this.typedClient.GetCrateContentsAsync(crateId, ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await this.RefreshTokenAsync().ConfigureAwait(false);
                        contents = await this.typedClient.GetCrateContentsAsync(crateId, ct).ConfigureAwait(false);
                    }

                    string json = JsonConvert.SerializeObject(contents, Formatting.Indented);
                    string filePath = Path.Combine(this.outputDir, "assets", "crates", $"{crateId}.json");
                    File.WriteAllText(filePath, json, Encoding.UTF8);
                    this.RecordSuccess("assets", $"crate-{crateId}");

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches blueprint detail via the typed client.
        /// </summary>
        /// <param name="blueprintId">The blueprint ID to fetch.</param>
        /// <returns>A work item for fetching blueprint detail.</returns>
        private WorkItem CreateTypedBlueprintWorkItem(int blueprintId)
        {
            return new WorkItem
            {
                Label = $"assets/blueprint-{blueprintId}",
                ExecuteAsync = async ct =>
                {
                    AssetBlueprint blueprint;
                    try
                    {
                        blueprint = await this.typedClient.GetBlueprintDetailAsync(blueprintId, ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await this.RefreshTokenAsync().ConfigureAwait(false);
                        blueprint = await this.typedClient.GetBlueprintDetailAsync(blueprintId, ct).ConfigureAwait(false);
                    }

                    string json = JsonConvert.SerializeObject(blueprint, Formatting.Indented);
                    string filePath = Path.Combine(this.outputDir, "assets", "blueprints", $"{blueprintId}.json");
                    File.WriteAllText(filePath, json, Encoding.UTF8);
                    this.RecordSuccess("assets", $"blueprint-{blueprintId}");

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Creates a work item that fetches survey detail via the typed client.
        /// </summary>
        /// <param name="surveyId">The survey ID to fetch.</param>
        /// <returns>A work item for fetching survey detail.</returns>
        private WorkItem CreateTypedSurveyWorkItem(int surveyId)
        {
            return new WorkItem
            {
                Label = $"assets/survey-{surveyId}",
                ExecuteAsync = async ct =>
                {
                    AssetSurvey survey;
                    try
                    {
                        survey = await this.typedClient.GetSurveyDetailAsync(surveyId, ct).ConfigureAwait(false);
                    }
                    catch (ApiHttpException ex) when (ex.StatusCode == 401)
                    {
                        await this.RefreshTokenAsync().ConfigureAwait(false);
                        survey = await this.typedClient.GetSurveyDetailAsync(surveyId, ct).ConfigureAwait(false);
                    }

                    string json = JsonConvert.SerializeObject(survey, Formatting.Indented);
                    string filePath = Path.Combine(this.outputDir, "assets", "surveys", $"{surveyId}.json");
                    File.WriteAllText(filePath, json, Encoding.UTF8);
                    this.RecordSuccess("assets", $"survey-{surveyId}");

                    return Array.Empty<WorkItem>();
                },
            };
        }

        /// <summary>
        /// Refreshes the access token using the typed client (sub-task 13.2, 13.3).
        /// Thread-safe: only one refresh happens at a time; concurrent callers wait.
        /// </summary>
        /// <returns>A task that completes when the token has been refreshed.</returns>
        private async Task RefreshTokenAsync()
        {
            await this.tokenRefreshSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                var tokenResponse = await this.typedClient.ExchangeTokenAsync(this.appId, this.clientId, this.secret).ConfigureAwait(false);
                this.accessToken = tokenResponse.AccessToken;
                TestContext.WriteLine("[TOKEN] Refreshed access token successfully via typed client");
            }
            catch (ApiBusinessException ex)
            {
                TestContext.WriteLine("[TOKEN] Token refresh failed: {0}", ex.ReturnString);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine("[TOKEN] Token refresh failed: {0}", ex.Message);
            }
            finally
            {
                this.tokenRefreshSemaphore.Release();
            }
        }

        /// <summary>
        /// Records a successful endpoint call.
        /// </summary>
        /// <param name="category">The endpoint category.</param>
        /// <param name="endpoint">The endpoint path.</param>
        private void RecordSuccess(string category, string endpoint)
        {
            this.results.Add(new EndpointResult
            {
                Category = category,
                Endpoint = endpoint,
                Success = true,
                HttpStatus = 200,
                Timestamp = SystemClock.UtcNow,
            });

            TestContext.WriteLine("[OK] {0} - {1}", category, endpoint);
        }

        /// <summary>
        /// Records a skipped or failed endpoint call.
        /// </summary>
        /// <param name="category">The endpoint category.</param>
        /// <param name="endpoint">The endpoint path.</param>
        /// <param name="reason">The reason the endpoint was skipped or failed.</param>
        private void RecordSkipped(string category, string endpoint, string reason)
        {
            this.results.Add(new EndpointResult
            {
                Category = category,
                Endpoint = endpoint,
                Success = false,
                SkipReason = reason,
                Timestamp = SystemClock.UtcNow,
            });

            TestContext.WriteLine("[SKIP] {0} - {1}: {2}", category, endpoint, reason);
        }

        /// <summary>
        /// Pretty-prints JSON with indentation for human readability.
        /// Uses DeserializeObject + SerializeObject to avoid JToken.Parse issues
        /// with nunit3-console's bundled Newtonsoft.Json.
        /// </summary>
        /// <param name="json">The raw JSON string to format.</param>
        /// <returns>The formatted JSON string.</returns>
        private static string FormatJson(string json)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject(json);
                return JsonConvert.SerializeObject(obj, Formatting.Indented);
            }
            catch
            {
                return json;
            }
        }

        /// <summary>
        /// Creates a directory if it does not already exist.
        /// </summary>
        /// <param name="path">The directory path to ensure exists.</param>
        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Searches asset location details for the first cargo item with the specified typeC code.
        /// Iterates all locations from the asset locations list, fetching detail for each until
        /// an item with matching typeC is found.
        /// </summary>
        /// <param name="typeC">The type code to search for (e.g. "Cr", "Sc", "Bp").</param>
        /// <returns>The cargoItemId of the first matching item, or 0 if not found.</returns>
        private int FindFirstAssetIdByTypeC(string typeC)
        {
            if (string.IsNullOrEmpty(this.assetLocationsJson))
            {
                return 0;
            }

            var envelope = JObject.Parse(this.assetLocationsJson);
            var locations = envelope["data"]?["locations"] as JArray;
            if (locations == null || locations.Count == 0)
            {
                return 0;
            }

            foreach (var location in locations)
            {
                int locationId = location["locationId"]?.Value<int>() ?? 0;
                string locationType = location["locationType"]?.Value<string>();

                if (locationId == 0 || string.IsNullOrEmpty(locationType))
                {
                    continue;
                }

                try
                {
                    var detailResult = this.client.GetAssetLocationDetailAsync(
                        this.appId, this.accessToken, locationId, locationType).GetAwaiter().GetResult();

                    if (!detailResult.Success)
                    {
                        continue;
                    }

                    var detail = JObject.Parse(detailResult.Json);
                    var cargo = detail["data"]?["cargo"] as JArray;
                    if (cargo == null)
                    {
                        continue;
                    }

                    foreach (var item in cargo)
                    {
                        string itemTypeC = item["typeC"]?.Value<string>()?.Trim();
                        if (string.Equals(itemTypeC, typeC, StringComparison.OrdinalIgnoreCase))
                        {
                            int id = item["id"]?.Value<int>() ?? 0;
                            if (id > 0)
                            {
                                return id;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }

            return 0;
        }

        /// <summary>
        /// Tracks the result of a single endpoint call during the discovery run.
        /// </summary>
        private class EndpointResult
        {
            /// <summary>
            /// Gets or sets the endpoint category (e.g. "character", "banking").
            /// </summary>
            public string Category { get; set; }

            /// <summary>
            /// Gets or sets the endpoint path (e.g. "/v1/character").
            /// </summary>
            public string Endpoint { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the call succeeded.
            /// </summary>
            public bool Success { get; set; }

            /// <summary>
            /// Gets or sets the HTTP status code returned.
            /// </summary>
            public int HttpStatus { get; set; }

            /// <summary>
            /// Gets or sets the reason the endpoint was skipped or failed.
            /// </summary>
            public string SkipReason { get; set; }

            /// <summary>
            /// Gets or sets the UTC timestamp when this result was recorded.
            /// </summary>
            public DateTime Timestamp { get; set; }
        }
    }
}
