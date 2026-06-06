// <copyright file="GameApiFullDiscoveryTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Client
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
        private string appId;
        private string accessToken;
        private string playerUUID;
        private string outputDir;
        private List<EndpointResult> results;
#pragma warning disable CS0649 // Field will be assigned by future work item tasks (15.x/17.x)
        private string assetLocationsJson;
#pragma warning restore CS0649

        /// <summary>
        /// Loads credentials from the app's existing credential store and preferences.
        /// Creates the output directory tree for storing raw JSON responses.
        /// Skips all tests if credentials are not configured.
        /// </summary>
        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            this.results = new List<EndpointResult>();

            GameApiCredentialManager.RegisterProtectionFunctions(
                CredentialStore.Protect,
                CredentialStore.Unprotect);

            var settings = PreferencesStore.GetInstance().Preferences.GameApiConnection;
            if (settings == null || !settings.Enabled)
            {
                Assert.Ignore("Game API integration is not configured or disabled in preferences.");
            }

            if (string.IsNullOrEmpty(settings.AppId) || string.IsNullOrEmpty(settings.ClientId))
            {
                Assert.Ignore("Game API AppId or ClientId not configured in preferences.");
            }

            this.appId = settings.AppId;

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

            this.client = new GameApiClient(settings.ServerUrl);

            var tokenResult = await this.client.ExchangeTokenAsync(this.appId, settings.ClientId, secret).ConfigureAwait(false);
            if (!tokenResult.Success)
            {
                Assert.Ignore("Token exchange failed: " + tokenResult.ErrorMessage);
            }

            this.accessToken = tokenResult.Token.AccessToken;

            this.outputDir = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "spec", "game-api-data"));

            EnsureDirectory(this.outputDir);
            EnsureDirectory(Path.Combine(this.outputDir, "character"));
            EnsureDirectory(Path.Combine(this.outputDir, "colonies"));
            EnsureDirectory(Path.Combine(this.outputDir, "banking"));
            EnsureDirectory(Path.Combine(this.outputDir, "assets"));
            EnsureDirectory(Path.Combine(this.outputDir, "jobs"));
            EnsureDirectory(Path.Combine(this.outputDir, "killmails"));
            EnsureDirectory(Path.Combine(this.outputDir, "mail"));
            EnsureDirectory(Path.Combine(this.outputDir, "ship"));
            EnsureDirectory(Path.Combine(this.outputDir, "market"));

            TestContext.WriteLine("Authenticated as player {0}, token obtained", this.playerUUID);
            TestContext.WriteLine("Output directory: {0}", this.outputDir);
        }

        /// <summary>
        /// Writes _metadata.json with run summary and disposes the client.
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
                    details = this.results.Select(r => new
                    {
                        category = r.Category,
                        endpoint = r.Endpoint,
                        success = r.Success,
                        httpStatus = r.HttpStatus,
                        reason = r.SkipReason,
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
            }

            this.client?.Dispose();
        }

        /// <summary>
        /// Runs all API discovery endpoints through a rate-limited queue at 10 TPS.
        /// Seed work items are enqueued, and cascading items are produced on completion.
        /// </summary>
        [Test]
        public async Task RunQueuedDiscovery()
        {
            var queue = new GameApiRequestQueue(10.0);

            // TODO: Enqueue seed work items here (tasks 15.x)

            queue.Start();
            await queue.DrainAsync().ConfigureAwait(false);

            // TODO: Write metadata (task 21.x)
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
                        string itemTypeC = item["typeC"]?.Value<string>();
                        if (string.Equals(itemTypeC, typeC, StringComparison.OrdinalIgnoreCase))
                        {
                            int id = item["cargoItemId"]?.Value<int>() ?? 0;
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
        }
    }
}
