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
    /// Marked Explicit â€” requires real game API credentials via preferences/credential store.
    /// Produces raw JSON output organized by endpoint category.
    /// Feature: game-api-discovery-tool
    /// **Validates: Requirements 2.1, 2.3, 2.5, 12.1, 12.2, 12.3, 12.4**
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
        private string colonyListJson;
        private string killMailListJson;
        private string assetLocationsJson;
        private string mailListJson;

        /// <summary>
        /// Loads credentials from the app's existing credential store and preferences.
        /// Creates the output directory tree for storing raw JSON responses.
        /// Skips all tests if credentials are not configured.
        /// </summary>
        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            this.results = new List<EndpointResult>();

            // Register DPAPI protection functions
            GameApiCredentialManager.RegisterProtectionFunctions(
                CredentialStore.Protect,
                CredentialStore.Unprotect);

            // Load settings from the app's preferences store
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

            // Load the stored secret from the credential manager
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

            // Convert SecureString to plain string for token exchange
            string secret = new System.Net.NetworkCredential(string.Empty, secureSecret).Password;

            this.client = new GameApiClient(settings.ServerUrl);

            var tokenResult = await this.client.ExchangeTokenAsync(this.appId, settings.ClientId, secret).ConfigureAwait(false);
            if (!tokenResult.Success)
            {
                Assert.Ignore("Token exchange failed: " + tokenResult.ErrorMessage);
            }

            this.accessToken = tokenResult.Token.AccessToken;

            // Create output directory tree at spec/game-api-data/
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

            TestContext.WriteLine("Authenticated as player {0}, token obtained", this.playerUUID);
            TestContext.WriteLine("Output directory: {0}", this.outputDir);
        }

        /// <summary>
        /// Writes _metadata.json with run summary and disposes the client.
        /// **Validates: Requirements 11.3**
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
                    runTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
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
        /// Pulls the character profile from GET /v1/character and saves to character/profile.json.
        /// Handles 403 gracefully by recording the endpoint as skipped.
        /// **Validates: Requirements 3.1, 11.1, 11.4, 12.4**
        /// </summary>
        [Test]
        [Order(1)]
        public async Task PullCharacterProfile()
        {
            try
            {
                var result = await this.client.GetCharacterAsync(this.appId, this.accessToken).ConfigureAwait(false);
                if (!result.Success)
                {
                    RecordSkipped("character", "/v1/character", result.Json ?? "Unknown error");
                    return;
                }

                string path = Path.Combine(this.outputDir, "character", "profile.json");
                File.WriteAllText(path, FormatJson(result.Json), Encoding.UTF8);
                RecordSuccess("character", "/v1/character");
            }
            catch (Exception)
            {
                RecordSkipped("character", "/v1/character", "Circuit breaker open â€” API unavailable");
            }
        }

        /// <summary>
        /// Pulls the character skills from GET /v1/character/skills and saves to character/skills.json.
        /// Handles 403 gracefully by recording the endpoint as skipped.
        /// **Validates: Requirements 3.2, 11.1, 11.4, 12.4**
        /// </summary>
        [Test]
        [Order(2)]
        public async Task PullCharacterSkills()
        {
            try
            {
                var result = await this.client.GetCharacterSkillsAsync(this.appId, this.accessToken).ConfigureAwait(false);
                if (!result.Success)
                {
                    RecordSkipped("character", "/v1/character/skills", result.Json ?? "Unknown error");
                    return;
                }

                string path = Path.Combine(this.outputDir, "character", "skills.json");
                File.WriteAllText(path, FormatJson(result.Json), Encoding.UTF8);
                RecordSuccess("character", "/v1/character/skills");
            }
            catch (Exception)
            {
                RecordSkipped("character", "/v1/character/skills", "Circuit breaker open â€” API unavailable");
            }
        }

        /// <summary>
        /// Pulls the colony list from GET /v1/colonies and saves to colonies/list.json.
        /// Stores the raw JSON in a field so PullColonyDetails can extract colony IDs.
        /// **Validates: Requirements 4.1, 11.1, 11.4, 12.5**
        /// </summary>
        [Test]
        [Order(3)]
        public async Task PullColonyList()
        {
            try
            {
                var result = await this.client.GetColonyListAsync(this.appId, this.accessToken).ConfigureAwait(false);
                if (!result.Success)
                {
                    RecordSkipped("colonies", "/v1/colonies", result.Json ?? "Unknown error");
                    return;
                }

                this.colonyListJson = result.Json;

                string path = Path.Combine(this.outputDir, "colonies", "list.json");
                File.WriteAllText(path, FormatJson(result.Json), Encoding.UTF8);
                RecordSuccess("colonies", "/v1/colonies");
            }
            catch (Exception)
            {
                RecordSkipped("colonies", "/v1/colonies", "Circuit breaker open â€” API unavailable");
            }
        }

        /// <summary>
        /// Iterates colonies from the list response and pulls summary, buildings, warehouse,
        /// and workers for each colony. Saves to colonies/{colonyId}/ subdirectories.
        /// Handles errors gracefully per endpoint without aborting the loop.
        /// **Validates: Requirements 4.2, 4.3, 4.4, 4.5, 11.1, 11.2, 11.4, 12.5**
        /// </summary>
        [Test]
        [Order(4)]
        public async Task PullColonyDetails()
        {
            if (string.IsNullOrEmpty(this.colonyListJson))
            {
                RecordSkipped("colonies", "/v1/colonies/{id}", "Colony list not available");
                return;
            }

            var envelope = JObject.Parse(this.colonyListJson);
            var colonies = envelope["data"]?["colonies"] as JArray;
            if (colonies == null || colonies.Count == 0)
            {
                RecordSkipped("colonies", "/v1/colonies/{id}", "No colonies found in list response");
                return;
            }

            try
            {
                foreach (var colony in colonies)
                {
                    int colonyId = colony["colonyId"]?.Value<int>() ?? 0;
                    if (colonyId == 0)
                    {
                        continue;
                    }

                    string colonyDir = Path.Combine(this.outputDir, "colonies", colonyId.ToString());
                    EnsureDirectory(colonyDir);

                    // Summary
                    try
                    {
                        var summaryResult = await this.client.GetColonySummaryAsync(this.appId, this.accessToken, colonyId).ConfigureAwait(false);
                        if (summaryResult.Success)
                        {
                            File.WriteAllText(Path.Combine(colonyDir, "summary.json"), FormatJson(summaryResult.Json), Encoding.UTF8);
                            RecordSuccess("colonies", "/v1/colonies/" + colonyId);
                        }
                        else
                        {
                            RecordSkipped("colonies", "/v1/colonies/" + colonyId, summaryResult.Json ?? "Failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        RecordSkipped("colonies", "/v1/colonies/" + colonyId, ex.Message);
                    }

                    // Buildings
                    try
                    {
                        var buildingsResult = await this.client.GetColonyBuildingsAsync(this.appId, this.accessToken, colonyId).ConfigureAwait(false);
                        if (buildingsResult.Success)
                        {
                            File.WriteAllText(Path.Combine(colonyDir, "buildings.json"), FormatJson(buildingsResult.Json), Encoding.UTF8);
                            RecordSuccess("colonies", "/v1/colonies/" + colonyId + "/buildings");
                        }
                        else
                        {
                            RecordSkipped("colonies", "/v1/colonies/" + colonyId + "/buildings", buildingsResult.Json ?? "Failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        RecordSkipped("colonies", "/v1/colonies/" + colonyId + "/buildings", ex.Message);
                    }

                    // Warehouse
                    try
                    {
                        var warehouseResult = await this.client.GetColonyWarehouseAsync(this.appId, this.accessToken, colonyId).ConfigureAwait(false);
                        if (warehouseResult.Success)
                        {
                            File.WriteAllText(Path.Combine(colonyDir, "warehouse.json"), FormatJson(warehouseResult.Json), Encoding.UTF8);
                            RecordSuccess("colonies", "/v1/colonies/" + colonyId + "/warehouse");
                        }
                        else
                        {
                            RecordSkipped("colonies", "/v1/colonies/" + colonyId + "/warehouse", warehouseResult.Json ?? "Failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        RecordSkipped("colonies", "/v1/colonies/" + colonyId + "/warehouse", ex.Message);
                    }

                    // Workers
                    try
                    {
                        var workersResult = await this.client.GetColonyWorkersAsync(this.appId, this.accessToken, colonyId).ConfigureAwait(false);
                        if (workersResult.Success)
                        {
                            File.WriteAllText(Path.Combine(colonyDir, "workers.json"), FormatJson(workersResult.Json), Encoding.UTF8);
                            RecordSuccess("colonies", "/v1/colonies/" + colonyId + "/workers");
                        }
                        else
                        {
                            RecordSkipped("colonies", "/v1/colonies/" + colonyId + "/workers", workersResult.Json ?? "Failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        RecordSkipped("colonies", "/v1/colonies/" + colonyId + "/workers", ex.Message);
                    }
                }
            }
            catch (Exception)
            {
                RecordSkipped("colonies", "/v1/colonies/{id}", "Circuit breaker open â€” API unavailable");
            }
        }

        /// <summary>
        /// Pulls the banking balance from GET /v1/banking/balance and saves to banking/balance.json.
        /// Handles 403 gracefully by recording the endpoint as skipped.
        /// **Validates: Requirements 5.1, 11.1, 11.4**
        /// </summary>
        [Test]
        [Order(5)]
        public async Task PullBankingBalance()
        {
            try
            {
                var result = await this.client.GetBankingBalanceAsync(this.appId, this.accessToken).ConfigureAwait(false);
                if (!result.Success)
                {
                    RecordSkipped("banking", "/v1/banking/balance", result.Json ?? "Unknown error");
                    return;
                }

                string path = Path.Combine(this.outputDir, "banking", "balance.json");
                File.WriteAllText(path, FormatJson(result.Json), Encoding.UTF8);
                RecordSuccess("banking", "/v1/banking/balance");
            }
            catch (Exception)
            {
                RecordSkipped("banking", "/v1/banking/balance", "Circuit breaker open â€” API unavailable");
            }
        }

        /// <summary>
        /// Pulls the banking transactions from GET /v1/banking/transactions and saves to banking/transactions.json.
        /// Handles 403 gracefully by recording the endpoint as skipped.
        /// **Validates: Requirements 5.2, 11.1, 11.4**
        /// </summary>
        [Test]
        [Order(6)]
        public async Task PullBankingTransactions()
        {
            try
            {
                var result = await this.client.GetBankingTransactionsAsync(this.appId, this.accessToken).ConfigureAwait(false);
                if (!result.Success)
                {
                    RecordSkipped("banking", "/v1/banking/transactions", result.Json ?? "Unknown error");
                    return;
                }

                string path = Path.Combine(this.outputDir, "banking", "transactions.json");
                File.WriteAllText(path, FormatJson(result.Json), Encoding.UTF8);
                RecordSuccess("banking", "/v1/banking/transactions");
            }
            catch (Exception)
            {
                RecordSkipped("banking", "/v1/banking/transactions", "Circuit breaker open â€” API unavailable");
            }
        }

        /// <summary>
        /// Pulls the asset locations list from GET /v1/assets/locations and saves to assets/locations.json.
        /// Stores the raw JSON in a field so PullAssetLocationDetails can extract location IDs.
        /// Handles 403 gracefully by recording the endpoint as skipped.
        /// **Validates: Requirements 6.1, 11.1, 11.4**
        /// </summary>
        [Test]
        [Order(7)]
        public async Task PullAssetLocations()
        {
            try
            {
                var result = await this.client.GetAssetLocationsAsync(this.appId, this.accessToken).ConfigureAwait(false);
                if (!result.Success)
                {
                    RecordSkipped("assets", "/v1/assets/locations", result.Json ?? "Unknown error");
                    return;
                }

                this.assetLocationsJson = result.Json;

                string path = Path.Combine(this.outputDir, "assets", "locations.json");
                File.WriteAllText(path, FormatJson(result.Json), Encoding.UTF8);
                RecordSuccess("assets", "/v1/assets/locations");
            }
            catch (Exception)
            {
                RecordSkipped("assets", "/v1/assets/locations", "Circuit breaker open â€” API unavailable");
            }
        }

        /// <summary>
        /// Iterates asset locations from the list response and pulls detail for each location.
        /// Saves to assets/{locationType}-{locationId}.json.
        /// Handles errors gracefully per location without aborting the loop.
        /// **Validates: Requirements 6.2, 11.1, 11.2, 11.4**
        /// </summary>
        [Test]
        [Order(8)]
        public async Task PullAssetLocationDetails()
        {
            if (string.IsNullOrEmpty(this.assetLocationsJson))
            {
                RecordSkipped("assets", "/v1/assets/locations/{id}", "Asset locations list not available");
                return;
            }

            var envelope = JObject.Parse(this.assetLocationsJson);
            var locations = envelope["data"] as JArray;
            if (locations == null || locations.Count == 0)
            {
                RecordSkipped("assets", "/v1/assets/locations/{id}", "No locations found in list response");
                return;
            }

            try
            {
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
                        var detailResult = await this.client.GetAssetLocationDetailAsync(this.appId, this.accessToken, locationId, locationType).ConfigureAwait(false);
                        if (detailResult.Success)
                        {
                            string fileName = locationType + "-" + locationId + ".json";
                            string filePath = Path.Combine(this.outputDir, "assets", fileName);
                            File.WriteAllText(filePath, FormatJson(detailResult.Json), Encoding.UTF8);
                            RecordSuccess("assets", "/v1/assets/locations/" + locationId + "?locationType=" + locationType);
                        }
                        else
                        {
                            RecordSkipped("assets", "/v1/assets/locations/" + locationId + "?locationType=" + locationType, detailResult.Json ?? "Failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        RecordSkipped("assets", "/v1/assets/locations/" + locationId + "?locationType=" + locationType, ex.Message);
                    }
                }
            }
            catch (Exception)
            {
                RecordSkipped("assets", "/v1/assets/locations/{id}", "Circuit breaker open â€” API unavailable");
            }
        }

        /// <summary>
        /// Pulls accepted jobs from GET /v1/jobs/accepted and saves to jobs/accepted.json.
        /// Handles errors gracefully by recording the endpoint as skipped.
        /// **Validates: Requirements 7.1, 11.1, 11.4**
        /// </summary>
        [Test]
        [Order(9)]
        public async Task PullAcceptedJobs()
        {
            try
            {
                var result = await this.client.GetAcceptedJobsAsync(this.appId, this.accessToken).ConfigureAwait(false);
                if (!result.Success)
                {
                    RecordSkipped("jobs", "/v1/jobs/accepted", result.Json ?? "Unknown error");
                    return;
                }

                string path = Path.Combine(this.outputDir, "jobs", "accepted.json");
                File.WriteAllText(path, FormatJson(result.Json), Encoding.UTF8);
                RecordSuccess("jobs", "/v1/jobs/accepted");
            }
            catch (Exception)
            {
                RecordSkipped("jobs", "/v1/jobs/accepted", "Circuit breaker open â€” API unavailable");
            }
        }

        /// <summary>
        /// Pulls the kill mail list from GET /v1/killmails and saves to killmails/list.json.
        /// Stores the raw JSON in a field so PullKillMailDetails can extract kill mail IDs.
        /// **Validates: Requirements 8.1, 11.1, 11.4**
        /// </summary>
        [Test]
        [Order(10)]
        public async Task PullKillMailList()
        {
            try
            {
                var result = await this.client.GetKillMailListAsync(this.appId, this.accessToken).ConfigureAwait(false);
                if (!result.Success)
                {
                    RecordSkipped("killmails", "/v1/killmails", result.Json ?? "Unknown error");
                    return;
                }

                this.killMailListJson = result.Json;

                string path = Path.Combine(this.outputDir, "killmails", "list.json");
                File.WriteAllText(path, FormatJson(result.Json), Encoding.UTF8);
                RecordSuccess("killmails", "/v1/killmails");
            }
            catch (Exception)
            {
                RecordSkipped("killmails", "/v1/killmails", "Circuit breaker open â€” API unavailable");
            }
        }

        /// <summary>
        /// Iterates up to 5 kill mails from the list response and pulls detail for each.
        /// Saves to killmails/{killMailId}.json. Handles errors gracefully per kill mail
        /// without aborting the loop.
        /// **Validates: Requirements 8.2, 11.1, 11.2, 11.4**
        /// </summary>
        [Test]
        [Order(11)]
        public async Task PullKillMailDetails()
        {
            if (string.IsNullOrEmpty(this.killMailListJson))
            {
                RecordSkipped("killmails", "/v1/killmails/{id}", "Kill mail list not available");
                return;
            }

            var envelope = JObject.Parse(this.killMailListJson);
            var killMails = envelope["data"] as JArray;
            if (killMails == null || killMails.Count == 0)
            {
                RecordSkipped("killmails", "/v1/killmails/{id}", "No kill mails found in list response");
                return;
            }

            try
            {
                foreach (var killMail in killMails.Take(5))
                {
                    int killMailId = killMail["killMailId"]?.Value<int>() ?? 0;
                    if (killMailId == 0)
                    {
                        continue;
                    }

                    try
                    {
                        var detailResult = await this.client.GetKillMailDetailAsync(this.appId, this.accessToken, killMailId).ConfigureAwait(false);
                        if (detailResult.Success)
                        {
                            string detailPath = Path.Combine(this.outputDir, "killmails", killMailId + ".json");
                            File.WriteAllText(detailPath, FormatJson(detailResult.Json), Encoding.UTF8);
                            RecordSuccess("killmails", "/v1/killmails/" + killMailId);
                        }
                        else
                        {
                            RecordSkipped("killmails", "/v1/killmails/" + killMailId, detailResult.Json ?? "Failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        RecordSkipped("killmails", "/v1/killmails/" + killMailId, ex.Message);
                    }
                }
            }
            catch (Exception)
            {
                RecordSkipped("killmails", "/v1/killmails/{id}", "Circuit breaker open â€” API unavailable");
            }
        }

        /// <summary>
        /// Pulls the mail list from GET /v1/mail and saves to mail/list.json.
        /// Stores the raw JSON in a field so PullMailDetails can extract mail IDs.
        /// **Validates: Requirements 9.1, 11.1, 11.4**
        /// </summary>
        [Test]
        [Order(12)]
        public async Task PullMailList()
        {
            try
            {
                var result = await this.client.GetMailListAsync(this.appId, this.accessToken).ConfigureAwait(false);
                if (!result.Success)
                {
                    RecordSkipped("mail", "/v1/mail", result.Json ?? "Unknown error");
                    return;
                }

                this.mailListJson = result.Json;

                string path = Path.Combine(this.outputDir, "mail", "list.json");
                File.WriteAllText(path, FormatJson(result.Json), Encoding.UTF8);
                RecordSuccess("mail", "/v1/mail");
            }
            catch (Exception)
            {
                RecordSkipped("mail", "/v1/mail", "Circuit breaker open â€” API unavailable");
            }
        }

        /// <summary>
        /// Iterates up to 5 mails from the list response and pulls detail for each.
        /// Saves to mail/{mailId}.json. Handles errors gracefully per mail without aborting the loop.
        /// **Validates: Requirements 9.2, 11.1, 11.2, 11.4**
        /// </summary>
        [Test]
        [Order(13)]
        public async Task PullMailDetails()
        {
            if (string.IsNullOrEmpty(this.mailListJson))
            {
                RecordSkipped("mail", "/v1/mail/{mailId}", "Mail list not available");
                return;
            }

            var envelope = JObject.Parse(this.mailListJson);
            var mails = envelope["data"] as JArray;
            if (mails == null || mails.Count == 0)
            {
                RecordSkipped("mail", "/v1/mail/{mailId}", "No mails found in list response");
                return;
            }

            try
            {
                var mailsToFetch = mails.Take(5);
                foreach (var mail in mailsToFetch)
                {
                    int mailId = mail["mailId"]?.Value<int>() ?? 0;
                    if (mailId == 0)
                    {
                        continue;
                    }

                    try
                    {
                        var detailResult = await this.client.GetMailDetailAsync(this.appId, this.accessToken, mailId).ConfigureAwait(false);
                        if (detailResult.Success)
                        {
                            string filePath = Path.Combine(this.outputDir, "mail", mailId + ".json");
                            File.WriteAllText(filePath, FormatJson(detailResult.Json), Encoding.UTF8);
                            RecordSuccess("mail", "/v1/mail/" + mailId);
                        }
                        else
                        {
                            RecordSkipped("mail", "/v1/mail/" + mailId, detailResult.Json ?? "Failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        RecordSkipped("mail", "/v1/mail/" + mailId, ex.Message);
                    }
                }
            }
            catch (Exception)
            {
                RecordSkipped("mail", "/v1/mail/{mailId}", "Circuit breaker open â€” API unavailable");
            }
        }

        /// <summary>
        /// Pulls the ship configuration from GET /v1/ship/configuration and saves to ship/configuration.json.
        /// Handles 403 gracefully by recording the endpoint as skipped.
        /// **Validates: Requirements 10.1, 11.1, 11.4**
        /// </summary>
        [Test]
        [Order(14)]
        public async Task PullShipConfiguration()
        {
            try
            {
                var result = await this.client.GetShipConfigurationAsync(this.appId, this.accessToken).ConfigureAwait(false);
                if (!result.Success)
                {
                    RecordSkipped("ship", "/v1/ship/configuration", result.Json ?? "Unknown error");
                    return;
                }

                string path = Path.Combine(this.outputDir, "ship", "configuration.json");
                File.WriteAllText(path, FormatJson(result.Json), Encoding.UTF8);
                RecordSuccess("ship", "/v1/ship/configuration");
            }
            catch (Exception)
            {
                RecordSkipped("ship", "/v1/ship/configuration", "Circuit breaker open â€” API unavailable");
            }
        }

        /// <summary>
        /// Pulls the ship cargo from GET /v1/ship/cargo and saves to ship/cargo.json.
        /// Handles 403 gracefully by recording the endpoint as skipped.
        /// **Validates: Requirements 10.2, 11.1, 11.4**
        /// </summary>
        [Test]
        [Order(15)]
        public async Task PullShipCargo()
        {
            try
            {
                var result = await this.client.GetShipCargoAsync(this.appId, this.accessToken).ConfigureAwait(false);
                if (!result.Success)
                {
                    RecordSkipped("ship", "/v1/ship/cargo", result.Json ?? "Unknown error");
                    return;
                }

                string path = Path.Combine(this.outputDir, "ship", "cargo.json");
                File.WriteAllText(path, FormatJson(result.Json), Encoding.UTF8);
                RecordSuccess("ship", "/v1/ship/cargo");
            }
            catch (Exception)
            {
                RecordSkipped("ship", "/v1/ship/cargo", "Circuit breaker open â€” API unavailable");
            }
        }

        /// <summary>
        /// Records a successful endpoint call.
        /// </summary>
        /// <param name="category">The endpoint category (e.g. "character", "banking").</param>
        /// <param name="endpoint">The endpoint path (e.g. "/v1/character").</param>
        private void RecordSuccess(string category, string endpoint)
        {
            this.results.Add(new EndpointResult
            {
                Category = category,
                Endpoint = endpoint,
                Success = true,
                HttpStatus = 200,
            });

            TestContext.WriteLine("[OK] {0} â€” {1}", category, endpoint);
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

            TestContext.WriteLine("[SKIP] {0} â€” {1}: {2}", category, endpoint, reason);
        }

        /// <summary>
        /// Pretty-prints JSON with indentation for human readability.
        /// Returns the original string if parsing fails.
        /// </summary>
        /// <param name="json">The raw JSON string to format.</param>
        /// <returns>The formatted JSON string.</returns>
        private static string FormatJson(string json)
        {
            try
            {
                object parsed = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                return Newtonsoft.Json.JsonConvert.SerializeObject(parsed, Newtonsoft.Json.Formatting.Indented);
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
