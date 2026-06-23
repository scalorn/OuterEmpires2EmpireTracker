// <copyright file="FactionServerTypedClient.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OE2EmpireTracker.Common.Models;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Common.Client.FactionServer
{
    /// <summary>
    /// HTTP implementation of <see cref="IFactionServerTypedClient"/>.
    /// Uses Newtonsoft.Json for serialization, SecureString for bearer token,
    /// optional certificate pinning, and a semaphore-based rate limiter.
    /// </summary>
    public class FactionServerTypedClient : IFactionServerTypedClient
    {
        private readonly string _serverUrl;
        private readonly SecureString _bearerToken;
        private readonly string _trustedThumbprint;
        private readonly TimeSpan _timeout;
        private readonly HttpClient _httpClient;
        private SemaphoreSlim _rateLimiter;
        private int _rateLimitRequestsPerMinute;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionServerTypedClient"/> class.
        /// </summary>
        /// <param name="serverUrl">The base URL of the faction server.</param>
        /// <param name="bearerToken">The bearer token as a SecureString.</param>
        /// <param name="trustedThumbprint">Optional certificate thumbprint for pinning.</param>
        /// <param name="timeout">Optional request timeout (defaults to 5 minutes).</param>
        public FactionServerTypedClient(
            string serverUrl,
            SecureString bearerToken,
            string trustedThumbprint = null,
            TimeSpan? timeout = null)
        {
            _serverUrl = (serverUrl ?? string.Empty).TrimEnd('/');
            _bearerToken = bearerToken;
            _trustedThumbprint = trustedThumbprint;
            _timeout = timeout ?? TimeSpan.FromMinutes(5);
            _rateLimitRequestsPerMinute = 60;
            _rateLimiter = new SemaphoreSlim(60, 60);
            _httpClient = CreateHttpClient();
        }

        /// <inheritdoc/>
        public bool IsConnected { get; private set; }

        /// <inheritdoc/>
        public async Task<bool> CheckHealthAsync(CancellationToken ct = default)
        {
            try
            {
                await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
                string url = $"{_serverUrl}/health";
                var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
                IsConnected = response.IsSuccessStatusCode;
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                IsConnected = false;
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<ServerFaction[]> GetFactionsAsync(CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/factions";
            var response = await GetWithConnectionHandlingAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<ServerFaction[]>(json);
        }

        /// <inheritdoc/>
        public async Task<ServerCharacter[]> GetCharactersAsync(CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters";
            var response = await GetWithConnectionHandlingAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<ServerCharacter[]>(json);
        }

        /// <inheritdoc/>
        public async Task CreateCharacterAsync(string name, string uuid = null, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters";
            var payload = uuid != null
                ? new { name, uuid }
                : (object)new { name };
            string json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await PostWithConnectionHandlingAsync(url, content, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<SyncResponse> GetSyncSnapshotAsync(CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/sync/snapshot";
            var response = await GetWithConnectionHandlingAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<SyncResponse>(json);
        }

        /// <inheritdoc/>
        public async Task<PlayerRoot> ExportCharacterDataAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/export";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<PlayerRoot>(json);
        }

        /// <inheritdoc/>
        public async Task UploadBaselineAsync(BaselineRoot baseline, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string json = JsonConvert.SerializeObject(baseline);
            string url = $"{_serverUrl}/api/v1/global/baseline";
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(url, content, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<BulkImportResult> BulkImportAsync(string characterUUID, PlayerRoot data, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/import";
            string requestJson = JsonConvert.SerializeObject(data);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(url, content, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<BulkImportResult>(responseJson);
        }

        /// <inheritdoc/>
        public async Task<SharingRuleDto[]> GetSharingRulesAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/sharing";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<SharingRuleDto[]>(json);
        }

        /// <inheritdoc/>
        public async Task PutSharingRulesAsync(string characterUUID, SharingRuleDto[] rules, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/sharing";
            string json = JsonConvert.SerializeObject(rules);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(url, content, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<Colony[]> GetColoniesAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/colonies";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<Colony[]>(json);
        }

        /// <inheritdoc/>
        public async Task<Blueprint[]> GetBlueprintsAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/blueprints";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<Blueprint[]>(json);
        }

        /// <inheritdoc/>
        public async Task<Survey[]> GetSurveysAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/surveys";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<Survey[]>(json);
        }

        /// <inheritdoc/>
        public async Task<PlayerProfile[]> GetPlayerProfilesAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/playerProfiles";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<PlayerProfile[]>(json);
        }

        /// <inheritdoc/>
        public async Task<DeliveryRoute[]> GetDeliveryRoutesAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/deliveryRoutes";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<DeliveryRoute[]>(json);
        }

        /// <inheritdoc/>
        public async Task<DeliveryPlan[]> GetDeliveryPlansAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/deliveryPlans";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<DeliveryPlan[]>(json);
        }

        /// <inheritdoc/>
        public async Task<Ship[]> GetShipsAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/ships";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<Ship[]>(json);
        }

        /// <inheritdoc/>
        public async Task<ShipTemplate[]> GetShipTemplatesAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/shipTemplates";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<ShipTemplate[]>(json);
        }

        /// <inheritdoc/>
        public async Task<MarketListing[]> GetMarketListingsAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/marketListings";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<MarketListing[]>(json);
        }

        /// <inheritdoc/>
        public async Task<MarketTransaction[]> GetMarketTransactionsAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/marketTransactions";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<MarketTransaction[]>(json);
        }

        /// <inheritdoc/>
        public async Task<PricingPlan[]> GetPricingPlansAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/pricingPlans";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<PricingPlan[]>(json);
        }

        /// <inheritdoc/>
        public async Task<StockPlan[]> GetStockPlansAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/stockPlans";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<StockPlan[]>(json);
        }

        /// <inheritdoc/>
        public async Task<StockProfile[]> GetStockProfilesAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/stockProfiles";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<StockProfile[]>(json);
        }

        /// <inheritdoc/>
        public async Task<BuildPlan[]> GetBuildPlansAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/buildPlans";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<BuildPlan[]>(json);
        }

        /// <inheritdoc/>
        public async Task<SupplyChain[]> GetSupplyChainsAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/supplyChains";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<SupplyChain[]>(json);
        }

        /// <inheritdoc/>
        public async Task<Asteroid[]> GetAsteroidsAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/asteroids";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<Asteroid[]>(json);
        }

        /// <inheritdoc/>
        public async Task<Station[]> GetStationsAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/stations";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<Station[]>(json);
        }

        /// <inheritdoc/>
        public async Task<Faction[]> GetFactionContactsAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/factions";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<Faction[]>(json);
        }

        /// <inheritdoc/>
        public async Task<ExternalCharacter[]> GetExternalCharactersAsync(string characterUUID, CancellationToken ct = default)
        {
            await AcquireRateLimitTokenAsync(ct).ConfigureAwait(false);
            string url = $"{_serverUrl}/api/v1/characters/{characterUUID}/externalCharacters";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<ExternalCharacter[]>(json);
        }

        /// <summary>
        /// Releases all resources used by this client instance.
        /// Disposes the SecureString bearer token, the HttpClient, and the rate limiter semaphore.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _bearerToken?.Dispose();
            _httpClient?.Dispose();
            _rateLimiter?.Dispose();
        }

        /// <summary>
        /// Applies a new rate limit from any source (WebSocket, configuration, etc.).
        /// No bounds checking or validation is performed.
        /// </summary>
        /// <param name="requestsPerMinute">The new requests-per-minute limit.</param>
        public void ApplyRateLimit(int requestsPerMinute)
        {
            if (requestsPerMinute == _rateLimitRequestsPerMinute)
            {
                return;
            }

            _rateLimitRequestsPerMinute = requestsPerMinute;
            var old = _rateLimiter;
            _rateLimiter = new SemaphoreSlim(requestsPerMinute, requestsPerMinute);
            old?.Dispose();
        }

        /// <summary>
        /// Converts a <see cref="SecureString"/> to a plain-text string.
        /// </summary>
        /// <param name="secureString">The secure string to convert.</param>
        /// <returns>The plain-text representation of the secure string, or empty if null.</returns>
        private static string SecureStringToString(SecureString secureString)
        {
            if (secureString == null)
            {
                return string.Empty;
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
        /// Acquires a rate limit token, blocking until one is available.
        /// The token is automatically released after 60 seconds (sliding window approximation).
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task that completes when a token is acquired.</returns>
        private async Task AcquireRateLimitTokenAsync(CancellationToken ct)
        {
            await _rateLimiter.WaitAsync(ct).ConfigureAwait(false);

            // Release token after 60s (sliding window approximation)
            _ = Task.Run(async () =>
            {
                await Task.Delay(60000).ConfigureAwait(false);
                try
                {
                    _rateLimiter.Release();
                }
                catch (ObjectDisposedException)
                {
                }
            });
        }

        /// <summary>
        /// Executes a GET request and wraps network failures in <see cref="FactionConnectionException"/>.
        /// </summary>
        /// <param name="url">The request URL.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The HTTP response message.</returns>
        private async Task<HttpResponseMessage> GetWithConnectionHandlingAsync(string url, CancellationToken ct)
        {
            try
            {
                return await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                IsConnected = false;
                throw new FactionConnectionException("Network failure connecting to faction server", ex);
            }
        }

        /// <summary>
        /// Executes a POST request and wraps network failures in <see cref="FactionConnectionException"/>.
        /// </summary>
        /// <param name="url">The request URL.</param>
        /// <param name="content">The POST body content.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The HTTP response message.</returns>
        private async Task<HttpResponseMessage> PostWithConnectionHandlingAsync(
            string url,
            HttpContent content,
            CancellationToken ct)
        {
            try
            {
                return await _httpClient.PostAsync(url, content, ct).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                IsConnected = false;
                throw new FactionConnectionException("Network failure connecting to faction server", ex);
            }
        }

        /// <summary>
        /// Executes a PUT request and wraps network failures in <see cref="FactionConnectionException"/>.
        /// </summary>
        /// <param name="url">The request URL.</param>
        /// <param name="content">The PUT body content.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The HTTP response message.</returns>
        private async Task<HttpResponseMessage> PutWithConnectionHandlingAsync(
            string url,
            HttpContent content,
            CancellationToken ct)
        {
            try
            {
                return await _httpClient.PutAsync(url, content, ct).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                IsConnected = false;
                throw new FactionConnectionException("Network failure connecting to faction server", ex);
            }
        }

        /// <summary>
        /// Checks the HTTP response and throws an appropriate exception for error status codes.
        /// HTTP 400 attempts to parse validation errors; HTTP 403 throws authorization exception;
        /// all other errors throw the base server exception.
        /// </summary>
        /// <param name="response">The HTTP response message to check.</param>
        /// <returns>A task that completes successfully if the response indicates success.</returns>
        private async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            switch ((int)response.StatusCode)
            {
                case 400:
                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<BulkImportErrorResponse>(body);
                        if (errorResponse?.Errors == null)
                        {
                            throw new FactionServerException($"HTTP 400: {body}");
                        }

                        var errors = errorResponse.Errors.Select(e => new FactionValidationError
                        {
                            EntityType = e.EntityType,
                            EntityUUID = e.EntityUUID,
                            Field = e.Field,
                            Error = e.Error,
                        }).ToList();
                        throw new FactionValidationException(errors);
                    }
                    catch (JsonException)
                    {
                        throw new FactionServerException($"HTTP 400: {body}");
                    }

                case 403:
                    throw new FactionAuthorizationException(body);

                default:
                    throw new FactionServerException(
                        $"HTTP {(int)response.StatusCode}: {body}");
            }
        }

        /// <summary>
        /// Creates and configures the HttpClient with optional certificate pinning and bearer token authentication.
        /// </summary>
        /// <returns>A configured <see cref="HttpClient"/> instance.</returns>
        private HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler();
            if (!string.IsNullOrEmpty(_trustedThumbprint))
            {
                handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) =>
                {
                    if (cert == null)
                    {
                        return false;
                    }

                    return string.Equals(
                        cert.GetCertHashString(),
                        _trustedThumbprint,
                        StringComparison.OrdinalIgnoreCase);
                };
            }

            var client = new HttpClient(handler) { Timeout = _timeout };
            string token = SecureStringToString(_bearerToken);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        /// <summary>
        /// DTO for deserializing bulk import 400 error responses.
        /// </summary>
        private class BulkImportErrorResponse
        {
            /// <summary>
            /// Gets or sets the list of error items.
            /// </summary>
            [JsonProperty("errors")]
            public List<BulkImportErrorItem> Errors { get; set; }
        }

        /// <summary>
        /// A single error item within a bulk import error response.
        /// </summary>
        private class BulkImportErrorItem
        {
            /// <summary>
            /// Gets or sets the type of entity that failed validation.
            /// </summary>
            [JsonProperty("entityType")]
            public string EntityType { get; set; }

            /// <summary>
            /// Gets or sets the UUID of the entity that failed validation.
            /// </summary>
            [JsonProperty("entityUUID")]
            public string EntityUUID { get; set; }

            /// <summary>
            /// Gets or sets the name of the field that failed validation.
            /// </summary>
            [JsonProperty("field")]
            public string Field { get; set; }

            /// <summary>
            /// Gets or sets the validation error message.
            /// </summary>
            [JsonProperty("error")]
            public string Error { get; set; }
        }
    }
}
