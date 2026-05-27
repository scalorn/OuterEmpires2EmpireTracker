// <copyright file="GameApiClient.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using OE2EmpireTracker.Services;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// HTTP client for communicating with the Outer Empires 2 public API.
    /// Uses OAuth2 client_credentials flow for authentication (token exchange).
    /// Uses Polly retry (exponential backoff) and circuit breaker policies for resilience.
    /// Includes SemaphoreSlim-based sliding window rate limiting with HTTP 429 handling.
    /// Implements <see cref="IDisposable"/> to clean up the underlying HttpClient.
    /// </summary>
    public class GameApiClient : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly HttpClient _httpClient;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreakerPolicy;
        private readonly string _serverUrl;
        private readonly Dictionary<string, CachedToken> _tokenCache;
        private readonly object _tokenCacheLock = new object();

        private SemaphoreSlim _rateLimiter;
        private int _rateLimitRequestsPerMinute = 30;
        private DateTime _rateLimitPauseUntil = DateTime.MinValue;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameApiClient"/> class.
        /// Configures Polly retry and circuit breaker policies for resilient HTTP communication.
        /// </summary>
        /// <param name="serverUrl">Base URL of the game API server.</param>
        public GameApiClient(string serverUrl)
        {
            _serverUrl = (serverUrl ?? string.Empty).TrimEnd('/');
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _rateLimiter = new SemaphoreSlim(30, 30);
            _tokenCache = new Dictionary<string, CachedToken>();

            Log.Info("GameApiClient created: serverUrl='{0}' (normalized from '{1}')", _serverUrl, serverUrl);

            _retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => IsTransientError(r.StatusCode))
                .WaitAndRetryAsync(
                    3,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)),
                    (outcome, delay, retryCount, context) =>
                    {
                        Log.Warn(
                            "Game API retry {0}/3 after {1}s (HTTP {2})",
                            retryCount,
                            delay.TotalSeconds,
                            (int)outcome.Result.StatusCode);
                    });

            _circuitBreakerPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => IsTransientError(r.StatusCode))
                .CircuitBreakerAsync(
                    3,
                    TimeSpan.FromSeconds(30),
                    (outcome, breakDuration) =>
                    {
                        Log.Warn(
                            "Game API circuit breaker opened for {0}s after 3 consecutive failures",
                            breakDuration.TotalSeconds);
                    },
                    () =>
                    {
                        Log.Info("Game API circuit breaker reset to closed");
                    },
                    () =>
                    {
                        Log.Info("Game API circuit breaker half-open, probing");
                    });
        }

        /// <summary>
        /// Gets a value indicating whether the circuit breaker is currently open.
        /// </summary>
        public bool IsCircuitOpen =>
            _circuitBreakerPolicy.CircuitState == CircuitState.Open ||
            _circuitBreakerPolicy.CircuitState == CircuitState.Isolated;

        /// <summary>
        /// Tests connectivity by performing a token exchange against the game API.
        /// Since no /health endpoint exists, a successful token exchange proves connectivity.
        /// </summary>
        /// <param name="appId">The registered application GUID.</param>
        /// <param name="clientId">The player's account identifier.</param>
        /// <param name="secret">The per-character secret.</param>
        /// <returns>A tuple indicating success and a descriptive message.</returns>
        public async Task<(bool Success, string Message)> TestConnectionAsync(
            string appId,
            string clientId,
            string secret)
        {
            if (string.IsNullOrEmpty(appId))
            {
                Log.Warn("TestConnectionAsync called with empty appId");
                return (false, "App ID is not configured");
            }

            if (string.IsNullOrEmpty(clientId))
            {
                Log.Warn("TestConnectionAsync called with empty clientId");
                return (false, "Client ID is not configured");
            }

            if (string.IsNullOrEmpty(secret))
            {
                Log.Warn("TestConnectionAsync called with empty secret");
                return (false, "Secret is not configured");
            }

            Log.Info("Game API connection test via token exchange");

            try
            {
                var tokenResult = await ExchangeTokenAsync(appId, clientId, secret).ConfigureAwait(false);
                if (tokenResult.Success)
                {
                    string scopes = tokenResult.Token.Scopes != null
                        ? string.Join(", ", tokenResult.Token.Scopes)
                        : "(none)";
                    string message = string.Format(
                        "Connected (character {0}, scopes: {1})",
                        tokenResult.Token.CharacterId,
                        scopes);
                    Log.Info("Game API connection test succeeded: {0}", message);
                    return (true, message);
                }

                Log.Warn("Game API connection test failed: {0}", tokenResult.ErrorMessage);
                return (false, tokenResult.ErrorMessage);
            }
            catch (BrokenCircuitException)
            {
                Log.Warn("Game API connection test blocked by circuit breaker");
                return (false, "Circuit breaker is open \u2014 too many consecutive failures");
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API connection test failed");
                return (false, string.Format("Connection failed: {0}", ex.Message));
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Game API connection test timed out");
                return (false, "Connection test timed out");
            }
        }

        /// <summary>
        /// Exchanges credentials for an access token via the OAuth2 client_credentials flow.
        /// POSTs to /v1/auth/token with appId, clientId, secret, and grantType.
        /// Caches the token per character (keyed by clientId+secret hash).
        /// </summary>
        /// <param name="appId">The registered application GUID.</param>
        /// <param name="clientId">The player's account identifier.</param>
        /// <param name="secret">The per-character secret.</param>
        /// <returns>A result containing the token response or an error message.</returns>
        public async Task<(bool Success, GameApiTokenResponse Token, string ErrorMessage)> ExchangeTokenAsync(
            string appId,
            string clientId,
            string secret)
        {
            string cacheKey = clientId + ":" + secret.GetHashCode().ToString();

            lock (_tokenCacheLock)
            {
                if (_tokenCache.ContainsKey(cacheKey))
                {
                    CachedToken cached = _tokenCache[cacheKey];
                    if (!cached.IsExpired)
                    {
                        Log.Debug("Using cached token for clientId={0}", clientId);
                        return (true, cached.Token, null);
                    }

                    _tokenCache.Remove(cacheKey);
                }
            }

            string tokenUrl = _serverUrl + "/v1/auth/token";
            Log.Info("Game API token exchange: POST {0}", tokenUrl);

            var requestBody = new
            {
                appId = appId,
                clientId = clientId,
                secret = secret,
                grantType = "client_credentials",
            };

            string jsonBody = JsonConvert.SerializeObject(requestBody);

            await AcquireRateLimitTokenAsync().ConfigureAwait(false);

            var response = await _retryPolicy.ExecuteAsync(
                () => _circuitBreakerPolicy.ExecuteAsync(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
                    request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    return _httpClient.SendAsync(request);
                })).ConfigureAwait(false);

            HandleRateLimitResponse(response);
            UpdateRateLimitFromHeaders(response);

            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiTokenResponse>>(responseBody);
                if (envelope != null && envelope.Success && envelope.Data != null)
                {
                    lock (_tokenCacheLock)
                    {
                        _tokenCache[cacheKey] = new CachedToken
                        {
                            Token = envelope.Data,
                            ObtainedAtUtc = SystemClock.UtcNow,
                        };
                    }

                    Log.Info(
                        "Token exchange succeeded: characterId={0}, expiresIn={1}s",
                        envelope.Data.CharacterId,
                        envelope.Data.ExpiresIn);
                    return (true, envelope.Data, null);
                }

                string errorMsg = envelope?.ReturnString ?? "Token exchange returned unsuccessful response";
                Log.Warn("Token exchange failed: {0}", errorMsg);
                return (false, null, errorMsg);
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Warn("Token exchange received HTTP 401 \u2014 credentials are invalid");
                return (false, null, "Authentication failed: invalid credentials (HTTP 401)");
            }

            string failMsg = string.Format("Token exchange failed: HTTP {0}", (int)response.StatusCode);
            Log.Warn("{0}. Body: {1}", failMsg, responseBody.Substring(0, Math.Min(responseBody.Length, 500)));
            return (false, null, failMsg);
        }

        /// <summary>
        /// Retrieves the character profile from the game API.
        /// Requires a valid access token and app ID.
        /// </summary>
        /// <param name="appId">The registered application GUID for the X-App-Id header.</param>
        /// <param name="accessToken">The Bearer access token from token exchange.</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetCharacterAsync(string appId, string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return (false, null);
            }

            try
            {
                var response = await ExecuteWithPoliciesAsync(
                    HttpMethod.Get,
                    _serverUrl + "/v1/character",
                    appId,
                    accessToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (true, json);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Log.Warn("Game API GetCharacter received HTTP 401 \u2014 token is invalid or expired");
                    return (false, "401");
                }

                Log.Warn("Game API GetCharacter failed: HTTP {0}", (int)response.StatusCode);
                return (false, null);
            }
            catch (BrokenCircuitException)
            {
                Log.Warn("Game API GetCharacter blocked by open circuit breaker");
                return (false, null);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API GetCharacter request failed");
                return (false, null);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Game API GetCharacter request timed out");
                return (false, null);
            }
        }

        /// <summary>
        /// Retrieves the character skills from the game API.
        /// Requires a valid access token and app ID.
        /// </summary>
        /// <param name="appId">The registered application GUID for the X-App-Id header.</param>
        /// <param name="accessToken">The Bearer access token from token exchange.</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetCharacterSkillsAsync(string appId, string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return (false, null);
            }

            try
            {
                var response = await ExecuteWithPoliciesAsync(
                    HttpMethod.Get,
                    _serverUrl + "/v1/character/skills",
                    appId,
                    accessToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (true, json);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Log.Warn("Game API GetCharacterSkills received HTTP 401 \u2014 token is invalid or expired");
                    return (false, "401");
                }

                Log.Warn("Game API GetCharacterSkills failed: HTTP {0}", (int)response.StatusCode);
                return (false, null);
            }
            catch (BrokenCircuitException)
            {
                Log.Warn("Game API GetCharacterSkills blocked by open circuit breaker");
                return (false, null);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API GetCharacterSkills request failed");
                return (false, null);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Game API GetCharacterSkills request timed out");
                return (false, null);
            }
        }

        /// <summary>
        /// Retrieves the colony list from the game API.
        /// Requires a valid access token and app ID.
        /// </summary>
        /// <param name="appId">The registered application GUID for the X-App-Id header.</param>
        /// <param name="accessToken">The Bearer access token from token exchange.</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetColonyListAsync(string appId, string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return (false, null);
            }

            try
            {
                await AcquireRateLimitTokenAsync().ConfigureAwait(false);

                var response = await ExecuteWithPoliciesAsync(
                    HttpMethod.Get,
                    _serverUrl + "/v1/colonies",
                    appId,
                    accessToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (true, json);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Log.Warn("Game API GetColonyList received HTTP 401 \u2014 token is invalid or expired");
                    return (false, "401");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Log.Warn("Game API GetColonyList received HTTP 403 \u2014 colony.list.read scope not granted");
                    return (false, "403");
                }

                Log.Warn("Game API GetColonyList failed: HTTP {0}", (int)response.StatusCode);
                return (false, null);
            }
            catch (BrokenCircuitException)
            {
                Log.Warn("Game API GetColonyList blocked by open circuit breaker");
                return (false, null);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API GetColonyList request failed");
                return (false, null);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Game API GetColonyList request timed out");
                return (false, null);
            }
        }

        /// <summary>
        /// Retrieves the buildings for a specific colony from the game API.
        /// </summary>
        /// <param name="appId">The application identifier for the API request header.</param>
        /// <param name="accessToken">The OAuth access token for authorization.</param>
        /// <param name="colonyId">The colony identifier to fetch buildings for.</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetColonyBuildingsAsync(string appId, string accessToken, int colonyId)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return (false, null);
            }

            try
            {
                await AcquireRateLimitTokenAsync().ConfigureAwait(false);

                var response = await ExecuteWithPoliciesAsync(
                    HttpMethod.Get,
                    _serverUrl + "/v1/colonies/" + colonyId + "/buildings",
                    appId,
                    accessToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (true, json);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Log.Warn("Game API GetColonyBuildings received HTTP 401 — token is invalid or expired");
                    return (false, "401");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Log.Warn("Game API GetColonyBuildings received HTTP 403 — colony.buildings.read scope not granted");
                    return (false, "403");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Log.Warn("Game API GetColonyBuildings received HTTP 404 — colony not found or not owned");
                    return (false, "404");
                }

                Log.Warn("Game API GetColonyBuildings failed: HTTP {0}", (int)response.StatusCode);
                return (false, null);
            }
            catch (BrokenCircuitException)
            {
                Log.Warn("Game API GetColonyBuildings blocked by open circuit breaker");
                return (false, null);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API GetColonyBuildings request failed");
                return (false, null);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Game API GetColonyBuildings request timed out");
                return (false, null);
            }
        }

        /// <summary>
        /// Retrieves the warehouse contents for a specific colony from the game API.
        /// </summary>
        /// <param name="appId">The application identifier for the API request header.</param>
        /// <param name="accessToken">The OAuth access token for authorization.</param>
        /// <param name="colonyId">The colony identifier to fetch warehouse for.</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetColonyWarehouseAsync(string appId, string accessToken, int colonyId)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return (false, null);
            }

            try
            {
                await AcquireRateLimitTokenAsync().ConfigureAwait(false);

                var response = await ExecuteWithPoliciesAsync(
                    HttpMethod.Get,
                    _serverUrl + "/v1/colonies/" + colonyId + "/warehouse",
                    appId,
                    accessToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (true, json);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Log.Warn("Game API GetColonyWarehouse received HTTP 401 — token is invalid or expired");
                    return (false, "401");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Log.Warn("Game API GetColonyWarehouse received HTTP 403 — colony.warehouse.read scope not granted");
                    return (false, "403");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Log.Warn("Game API GetColonyWarehouse received HTTP 404 — colony not found or not owned");
                    return (false, "404");
                }

                Log.Warn("Game API GetColonyWarehouse failed: HTTP {0}", (int)response.StatusCode);
                return (false, null);
            }
            catch (BrokenCircuitException)
            {
                Log.Warn("Game API GetColonyWarehouse blocked by open circuit breaker");
                return (false, null);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API GetColonyWarehouse request failed");
                return (false, null);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Game API GetColonyWarehouse request timed out");
                return (false, null);
            }
        }

        /// <summary>
        /// Retrieves the workers data for a specific colony from the game API.
        /// </summary>
        /// <param name="appId">The application identifier for the API request header.</param>
        /// <param name="accessToken">The OAuth access token for authorization.</param>
        /// <param name="colonyId">The colony identifier to fetch workers for.</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetColonyWorkersAsync(string appId, string accessToken, int colonyId)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return (false, null);
            }

            try
            {
                await AcquireRateLimitTokenAsync().ConfigureAwait(false);

                var response = await ExecuteWithPoliciesAsync(
                    HttpMethod.Get,
                    _serverUrl + "/v1/colonies/" + colonyId + "/workers",
                    appId,
                    accessToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (true, json);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Log.Warn("Game API GetColonyWorkers received HTTP 401 — token is invalid or expired");
                    return (false, "401");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Log.Warn("Game API GetColonyWorkers received HTTP 403 — colony.workers.read scope not granted");
                    return (false, "403");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Log.Warn("Game API GetColonyWorkers received HTTP 404 — colony not found or not owned");
                    return (false, "404");
                }

                Log.Warn("Game API GetColonyWorkers failed: HTTP {0}", (int)response.StatusCode);
                return (false, null);
            }
            catch (BrokenCircuitException)
            {
                Log.Warn("Game API GetColonyWorkers blocked by open circuit breaker");
                return (false, null);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API GetColonyWorkers request failed");
                return (false, null);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Game API GetColonyWorkers request timed out");
                return (false, null);
            }
        }

        /// <summary>
        /// Retrieves the banking balance from the game API.
        /// </summary>
        /// <param name="appId">The registered application GUID for the X-App-Id header.</param>
        /// <param name="accessToken">The Bearer access token from token exchange.</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetBankingBalanceAsync(string appId, string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return (false, null);
            }

            try
            {
                await AcquireRateLimitTokenAsync().ConfigureAwait(false);

                var response = await ExecuteWithPoliciesAsync(
                    HttpMethod.Get,
                    _serverUrl + "/v1/banking/balance",
                    appId,
                    accessToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (true, json);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Log.Warn("Game API GetBankingBalance received HTTP 401 — token is invalid or expired");
                    return (false, "401");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Log.Warn("Game API GetBankingBalance received HTTP 403 — banking.balance.read scope not granted");
                    return (false, "403");
                }

                Log.Warn("Game API GetBankingBalance failed: HTTP {0}", (int)response.StatusCode);
                return (false, null);
            }
            catch (BrokenCircuitException)
            {
                Log.Warn("Game API GetBankingBalance blocked by open circuit breaker");
                return (false, null);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API GetBankingBalance request failed");
                return (false, null);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Game API GetBankingBalance request timed out");
                return (false, null);
            }
        }

        /// <summary>
        /// Retrieves the banking transactions from the game API.
        /// </summary>
        /// <param name="appId">The registered application GUID for the X-App-Id header.</param>
        /// <param name="accessToken">The Bearer access token from token exchange.</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetBankingTransactionsAsync(string appId, string accessToken)
        {
            return await GetBankingTransactionsAsync(appId, accessToken, 0, 50).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves a page of banking transactions from the game API.
        /// </summary>
        /// <param name="appId">The registered application GUID for the X-App-Id header.</param>
        /// <param name="accessToken">The Bearer access token from token exchange.</param>
        /// <param name="offset">The zero-based offset for pagination.</param>
        /// <param name="limit">The maximum number of records to return per page.</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetBankingTransactionsAsync(string appId, string accessToken, int offset, int limit)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return (false, null);
            }

            try
            {
                await AcquireRateLimitTokenAsync().ConfigureAwait(false);

                var response = await ExecuteWithPoliciesAsync(
                    HttpMethod.Get,
                    _serverUrl + "/v1/banking/transactions?offset=" + offset + "&limit=" + limit,
                    appId,
                    accessToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (true, json);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Log.Warn("Game API GetBankingTransactions received HTTP 401 — token is invalid or expired");
                    return (false, "401");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Log.Warn("Game API GetBankingTransactions received HTTP 403 — banking.transactions.read scope not granted");
                    return (false, "403");
                }

                Log.Warn("Game API GetBankingTransactions failed: HTTP {0}", (int)response.StatusCode);
                return (false, null);
            }
            catch (BrokenCircuitException)
            {
                Log.Warn("Game API GetBankingTransactions blocked by open circuit breaker");
                return (false, null);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API GetBankingTransactions request failed");
                return (false, null);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Game API GetBankingTransactions request timed out");
                return (false, null);
            }
        }

        /// <summary>
        /// Retrieves the list of accepted jobs from the game API.
        /// Requires a valid access token and app ID.
        /// </summary>
        /// <param name="appId">The registered application GUID for the X-App-Id header.</param>
        /// <param name="accessToken">The Bearer access token from token exchange.</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetAcceptedJobsAsync(string appId, string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return (false, null);
            }

            try
            {
                await AcquireRateLimitTokenAsync().ConfigureAwait(false);

                var response = await ExecuteWithPoliciesAsync(
                    HttpMethod.Get,
                    _serverUrl + "/v1/jobs/accepted",
                    appId,
                    accessToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (true, json);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Log.Warn("Game API GetAcceptedJobs received HTTP 401 \u2014 token is invalid or expired");
                    return (false, "401");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Log.Warn("Game API GetAcceptedJobs received HTTP 403 \u2014 jobs.read scope not granted");
                    return (false, "403");
                }

                Log.Warn("Game API GetAcceptedJobs failed: HTTP {0}", (int)response.StatusCode);
                return (false, null);
            }
            catch (BrokenCircuitException)
            {
                Log.Warn("Game API GetAcceptedJobs blocked by open circuit breaker");
                return (false, null);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API GetAcceptedJobs request failed");
                return (false, null);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Game API GetAcceptedJobs request timed out");
                return (false, null);
            }
        }

        /// <summary>
        /// Retrieves the list of asset locations from the game API.
        /// </summary>
        /// <param name="appId">The application identifier for the API request header.</param>
        /// <param name="accessToken">The OAuth access token for authorization.</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetAssetLocationsAsync(string appId, string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return (false, null);
            }

            try
            {
                await AcquireRateLimitTokenAsync().ConfigureAwait(false);

                var response = await ExecuteWithPoliciesAsync(
                    HttpMethod.Get,
                    _serverUrl + "/v1/assets/locations",
                    appId,
                    accessToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (true, json);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Log.Warn("Game API GetAssetLocations received HTTP 401 — token is invalid or expired");
                    return (false, "401");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Log.Warn("Game API GetAssetLocations received HTTP 403 — assets.locations.read scope not granted");
                    return (false, "403");
                }

                Log.Warn("Game API GetAssetLocations failed: HTTP {0}", (int)response.StatusCode);
                return (false, null);
            }
            catch (BrokenCircuitException)
            {
                Log.Warn("Game API GetAssetLocations blocked by open circuit breaker");
                return (false, null);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API GetAssetLocations request failed");
                return (false, null);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Game API GetAssetLocations request timed out");
                return (false, null);
            }
        }

        /// <summary>
        /// Retrieves the asset details for a specific location from the game API.
        /// </summary>
        /// <param name="appId">The application identifier for the API request header.</param>
        /// <param name="accessToken">The OAuth access token for authorization.</param>
        /// <param name="locationId">The location identifier to fetch asset details for.</param>
        /// <param name="locationType">The type of location (e.g. colony, ship, station).</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetAssetLocationDetailAsync(string appId, string accessToken, int locationId, string locationType)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return (false, null);
            }

            try
            {
                await AcquireRateLimitTokenAsync().ConfigureAwait(false);

                var response = await ExecuteWithPoliciesAsync(
                    HttpMethod.Get,
                    _serverUrl + "/v1/assets/locations/" + locationId + "?locationType=" + Uri.EscapeDataString(locationType ?? string.Empty),
                    appId,
                    accessToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (true, json);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Log.Warn("Game API GetAssetLocationDetail received HTTP 401 — token is invalid or expired");
                    return (false, "401");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Log.Warn("Game API GetAssetLocationDetail received HTTP 403 — assets.locations.read scope not granted");
                    return (false, "403");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Log.Warn("Game API GetAssetLocationDetail received HTTP 404 — location not found");
                    return (false, "404");
                }

                Log.Warn("Game API GetAssetLocationDetail failed: HTTP {0}", (int)response.StatusCode);
                return (false, null);
            }
            catch (BrokenCircuitException)
            {
                Log.Warn("Game API GetAssetLocationDetail blocked by open circuit breaker");
                return (false, null);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API GetAssetLocationDetail request failed");
                return (false, null);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Game API GetAssetLocationDetail request timed out");
                return (false, null);
            }
        }

        /// <summary>
        /// Retrieves the list of kill mails from the game API.
        /// Requires a valid access token and app ID.
        /// </summary>
        /// <param name="appId">The registered application GUID for the X-App-Id header.</param>
        /// <param name="accessToken">The Bearer access token from token exchange.</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetKillMailListAsync(string appId, string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return (false, null);
            }

            try
            {
                await AcquireRateLimitTokenAsync().ConfigureAwait(false);

                var response = await ExecuteWithPoliciesAsync(
                    HttpMethod.Get,
                    _serverUrl + "/v1/killmails",
                    appId,
                    accessToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (true, json);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Log.Warn("Game API GetKillMailList received HTTP 401 — token is invalid or expired");
                    return (false, "401");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Log.Warn("Game API GetKillMailList received HTTP 403 — killmail.read scope not granted");
                    return (false, "403");
                }

                Log.Warn("Game API GetKillMailList failed: HTTP {0}", (int)response.StatusCode);
                return (false, null);
            }
            catch (BrokenCircuitException)
            {
                Log.Warn("Game API GetKillMailList blocked by open circuit breaker");
                return (false, null);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API GetKillMailList request failed");
                return (false, null);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Game API GetKillMailList request timed out");
                return (false, null);
            }
        }

        /// <summary>
        /// Retrieves the details of a specific kill mail from the game API.
        /// </summary>
        /// <param name="appId">The registered application GUID for the X-App-Id header.</param>
        /// <param name="accessToken">The Bearer access token from token exchange.</param>
        /// <param name="killMailId">The kill mail identifier to fetch details for.</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetKillMailDetailAsync(string appId, string accessToken, int killMailId)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return (false, null);
            }

            try
            {
                await AcquireRateLimitTokenAsync().ConfigureAwait(false);

                var response = await ExecuteWithPoliciesAsync(
                    HttpMethod.Get,
                    _serverUrl + "/v1/killmails/" + killMailId,
                    appId,
                    accessToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (true, json);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Log.Warn("Game API GetKillMailDetail received HTTP 401 — token is invalid or expired");
                    return (false, "401");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Log.Warn("Game API GetKillMailDetail received HTTP 403 — killmail.read scope not granted");
                    return (false, "403");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Log.Warn("Game API GetKillMailDetail received HTTP 404 — kill mail not found");
                    return (false, "404");
                }

                Log.Warn("Game API GetKillMailDetail failed: HTTP {0}", (int)response.StatusCode);
                return (false, null);
            }
            catch (BrokenCircuitException)
            {
                Log.Warn("Game API GetKillMailDetail blocked by open circuit breaker");
                return (false, null);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API GetKillMailDetail request failed");
                return (false, null);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Game API GetKillMailDetail request timed out");
                return (false, null);
            }
        }

        /// <summary>
        /// Retrieves the summary for a specific colony from the game API.
        /// </summary>
        /// <param name="appId">The application identifier for the API request header.</param>
        /// <param name="accessToken">The OAuth access token for authorization.</param>
        /// <param name="colonyId">The colony identifier to fetch the summary for.</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetColonySummaryAsync(string appId, string accessToken, int colonyId)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return (false, null);
            }

            try
            {
                await AcquireRateLimitTokenAsync().ConfigureAwait(false);

                var response = await ExecuteWithPoliciesAsync(
                    HttpMethod.Get,
                    _serverUrl + "/v1/colonies/" + colonyId,
                    appId,
                    accessToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (true, json);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Log.Warn("Game API GetColonySummary received HTTP 401 — token is invalid or expired");
                    return (false, "401");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Log.Warn("Game API GetColonySummary received HTTP 403 — colony.read scope not granted");
                    return (false, "403");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Log.Warn("Game API GetColonySummary received HTTP 404 — colony not found or not owned");
                    return (false, "404");
                }

                Log.Warn("Game API GetColonySummary failed: HTTP {0}", (int)response.StatusCode);
                return (false, null);
            }
            catch (BrokenCircuitException)
            {
                Log.Warn("Game API GetColonySummary blocked by open circuit breaker");
                return (false, null);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API GetColonySummary request failed");
                return (false, null);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Game API GetColonySummary request timed out");
                return (false, null);
            }
        }

        /// <summary>
        /// Retrieves the ship configuration from the game API.
        /// </summary>
        /// <param name="appId">The application identifier for the API request header.</param>
        /// <param name="accessToken">The OAuth access token for authorization.</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetShipConfigurationAsync(string appId, string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return (false, null);
            }

            try
            {
                await AcquireRateLimitTokenAsync().ConfigureAwait(false);

                var response = await ExecuteWithPoliciesAsync(
                    HttpMethod.Get,
                    _serverUrl + "/v1/ship/configuration",
                    appId,
                    accessToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (true, json);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Log.Warn("Game API GetShipConfiguration received HTTP 401 — token is invalid or expired");
                    return (false, "401");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Log.Warn("Game API GetShipConfiguration received HTTP 403 — ship.configuration.read scope not granted");
                    return (false, "403");
                }

                Log.Warn("Game API GetShipConfiguration failed: HTTP {0}", (int)response.StatusCode);
                return (false, null);
            }
            catch (BrokenCircuitException)
            {
                Log.Warn("Game API GetShipConfiguration blocked by open circuit breaker");
                return (false, null);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API GetShipConfiguration request failed");
                return (false, null);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Game API GetShipConfiguration request timed out");
                return (false, null);
            }
        }

        /// <summary>
        /// Retrieves the ship cargo from the game API.
        /// </summary>
        /// <param name="appId">The application identifier for the API request header.</param>
        /// <param name="accessToken">The OAuth access token for authorization.</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetShipCargoAsync(string appId, string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return (false, null);
            }

            try
            {
                await AcquireRateLimitTokenAsync().ConfigureAwait(false);

                var response = await ExecuteWithPoliciesAsync(
                    HttpMethod.Get,
                    _serverUrl + "/v1/ship/cargo",
                    appId,
                    accessToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (true, json);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Log.Warn("Game API GetShipCargo received HTTP 401 — token is invalid or expired");
                    return (false, "401");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Log.Warn("Game API GetShipCargo received HTTP 403 — ship.cargo.read scope not granted");
                    return (false, "403");
                }

                Log.Warn("Game API GetShipCargo failed: HTTP {0}", (int)response.StatusCode);
                return (false, null);
            }
            catch (BrokenCircuitException)
            {
                Log.Warn("Game API GetShipCargo blocked by open circuit breaker");
                return (false, null);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API GetShipCargo request failed");
                return (false, null);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Game API GetShipCargo request timed out");
                return (false, null);
            }
        }

        /// <summary>
        /// Retrieves the mail list from the game API.
        /// Requires a valid access token and app ID.
        /// </summary>
        /// <param name="appId">The registered application GUID for the X-App-Id header.</param>
        /// <param name="accessToken">The Bearer access token from token exchange.</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetMailListAsync(string appId, string accessToken)
        {
            return await GetMailListAsync(appId, accessToken, 0, 50).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves a page of mail from the game API.
        /// </summary>
        /// <param name="appId">The registered application GUID for the X-App-Id header.</param>
        /// <param name="accessToken">The Bearer access token from token exchange.</param>
        /// <param name="offset">The zero-based offset for pagination.</param>
        /// <param name="limit">The maximum number of records to return per page.</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetMailListAsync(string appId, string accessToken, int offset, int limit)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return (false, null);
            }

            try
            {
                await AcquireRateLimitTokenAsync().ConfigureAwait(false);

                var response = await ExecuteWithPoliciesAsync(
                    HttpMethod.Get,
                    _serverUrl + "/v1/mail?offset=" + offset + "&limit=" + limit,
                    appId,
                    accessToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (true, json);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Log.Warn("Game API GetMailList received HTTP 401 — token is invalid or expired");
                    return (false, "401");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Log.Warn("Game API GetMailList received HTTP 403 — mail.read scope not granted");
                    return (false, "403");
                }

                Log.Warn("Game API GetMailList failed: HTTP {0}", (int)response.StatusCode);
                return (false, null);
            }
            catch (BrokenCircuitException)
            {
                Log.Warn("Game API GetMailList blocked by open circuit breaker");
                return (false, null);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API GetMailList request failed");
                return (false, null);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Game API GetMailList request timed out");
                return (false, null);
            }
        }

        /// <summary>
        /// Retrieves the detail for a specific mail from the game API.
        /// Requires a valid access token and app ID.
        /// </summary>
        /// <param name="appId">The registered application GUID for the X-App-Id header.</param>
        /// <param name="accessToken">The Bearer access token from token exchange.</param>
        /// <param name="mailId">The mail identifier to fetch details for.</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetMailDetailAsync(string appId, string accessToken, int mailId)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return (false, null);
            }

            try
            {
                await AcquireRateLimitTokenAsync().ConfigureAwait(false);

                var response = await ExecuteWithPoliciesAsync(
                    HttpMethod.Get,
                    _serverUrl + "/v1/mail/" + mailId,
                    appId,
                    accessToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (true, json);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Log.Warn("Game API GetMailDetail received HTTP 401 — token is invalid or expired");
                    return (false, "401");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Log.Warn("Game API GetMailDetail received HTTP 403 — mail.read scope not granted");
                    return (false, "403");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Log.Warn("Game API GetMailDetail received HTTP 404 — mail not found");
                    return (false, "404");
                }

                Log.Warn("Game API GetMailDetail failed: HTTP {0}", (int)response.StatusCode);
                return (false, null);
            }
            catch (BrokenCircuitException)
            {
                Log.Warn("Game API GetMailDetail blocked by open circuit breaker");
                return (false, null);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API GetMailDetail request failed");
                return (false, null);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Game API GetMailDetail request timed out");
                return (false, null);
            }
        }

        /// <summary>
        /// Invalidates any cached token for the given credentials.
        /// Call this when a 401 is received to force re-authentication on next request.
        /// </summary>
        /// <param name="clientId">The player's account identifier.</param>
        /// <param name="secret">The per-character secret.</param>
        public void InvalidateToken(string clientId, string secret)
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(secret))
            {
                return;
            }

            string cacheKey = clientId + ":" + secret.GetHashCode().ToString();
            lock (_tokenCacheLock)
            {
                if (_tokenCache.Remove(cacheKey))
                {
                    Log.Info("Invalidated cached token for clientId={0}", clientId);
                }
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="GameApiClient"/>.
        /// Disposes the underlying HttpClient.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Acquires a rate limit token before making an outgoing request.
        /// If the rate limiter is paused (due to HTTP 429), waits until the pause expires.
        /// Blocks if the rate limit has been reached until a token becomes available.
        /// </summary>
        /// <returns>A task that completes when a rate limit token is acquired.</returns>
        internal async Task AcquireRateLimitTokenAsync()
        {
            DateTime pauseUntil = _rateLimitPauseUntil;
            if (pauseUntil > SystemClock.UtcNow)
            {
                TimeSpan waitDuration = pauseUntil - SystemClock.UtcNow;
                if (waitDuration > TimeSpan.Zero)
                {
                    Log.Info("Rate limiter paused, waiting {0:F1}s before next request", waitDuration.TotalSeconds);
                    await Task.Delay(waitDuration).ConfigureAwait(false);
                }
            }

            await _rateLimiter.WaitAsync().ConfigureAwait(false);

            // Release the token after the sliding window interval
            int releaseDelayMs = 60000 / _rateLimitRequestsPerMinute;
            _ = Task.Delay(releaseDelayMs).ContinueWith(_ =>
            {
                try
                {
                    _rateLimiter.Release();
                }
                catch (SemaphoreFullException)
                {
                    // Ignore — can happen if rate limit was reconfigured
                }
                catch (ObjectDisposedException)
                {
                    // Ignore — client was disposed
                }
            });
        }

        /// <summary>
        /// Handles an HTTP 429 (Too Many Requests) response by pausing all outgoing requests.
        /// If a Retry-After header is present, pauses for that duration; otherwise pauses for 60 seconds.
        /// </summary>
        /// <param name="response">The HTTP response to inspect.</param>
        internal void HandleRateLimitResponse(HttpResponseMessage response)
        {
            if (response.StatusCode != (HttpStatusCode)429)
            {
                return;
            }

            int pauseSeconds = 60;

            if (response.Headers.RetryAfter != null)
            {
                if (response.Headers.RetryAfter.Delta.HasValue)
                {
                    pauseSeconds = (int)response.Headers.RetryAfter.Delta.Value.TotalSeconds;
                }
                else if (response.Headers.RetryAfter.Date.HasValue)
                {
                    TimeSpan untilDate = response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
                    pauseSeconds = Math.Max(1, (int)untilDate.TotalSeconds);
                }
            }

            _rateLimitPauseUntil = SystemClock.UtcNow.AddSeconds(pauseSeconds);
            Log.Warn("Game API rate limited (HTTP 429), pausing requests for {0}s", pauseSeconds);
        }

        /// <summary>
        /// Reads the X-RateLimit-Limit response header and updates the internal rate limit
        /// to match the server-communicated value. Recreates the semaphore if the limit changes.
        /// </summary>
        /// <param name="response">The HTTP response to inspect.</param>
        internal void UpdateRateLimitFromHeaders(HttpResponseMessage response)
        {
            IEnumerable<string> values;
            if (!response.Headers.TryGetValues("X-RateLimit-Limit", out values))
            {
                return;
            }

            string headerValue = null;
            foreach (string v in values)
            {
                headerValue = v;
                break;
            }

            if (string.IsNullOrEmpty(headerValue))
            {
                return;
            }

            int newLimit;
            if (!int.TryParse(headerValue, out newLimit) || newLimit <= 0)
            {
                return;
            }

            if (newLimit == _rateLimitRequestsPerMinute)
            {
                return;
            }

            Log.Info(
                "Game API rate limit updated from header: {0} -> {1} requests/minute",
                _rateLimitRequestsPerMinute,
                newLimit);

            _rateLimitRequestsPerMinute = newLimit;

            var oldLimiter = _rateLimiter;
            _rateLimiter = new SemaphoreSlim(newLimit, newLimit);
            oldLimiter?.Dispose();
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
                    _httpClient?.Dispose();
                    _rateLimiter?.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Determines whether the given HTTP status code represents a transient server error
        /// eligible for retry.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to evaluate.</param>
        /// <returns>True if the status code is 500, 502, 503, or 504.</returns>
        private static bool IsTransientError(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.InternalServerError ||
                   statusCode == HttpStatusCode.BadGateway ||
                   statusCode == HttpStatusCode.ServiceUnavailable ||
                   statusCode == HttpStatusCode.GatewayTimeout;
        }

        /// <summary>
        /// Executes an HTTP request through the retry and circuit breaker policy pipeline.
        /// Acquires a rate limit token before sending and processes rate limit headers after
        /// receiving the response. Creates a fresh <see cref="HttpRequestMessage"/> for each
        /// attempt because HttpRequestMessage cannot be reused after being sent.
        /// Uses Bearer token + X-App-Id headers for authentication.
        /// </summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="requestUrl">The full request URL.</param>
        /// <param name="appId">The application GUID for the X-App-Id header.</param>
        /// <param name="accessToken">The Bearer access token.</param>
        /// <returns>The HTTP response message.</returns>
        private async Task<HttpResponseMessage> ExecuteWithPoliciesAsync(
            HttpMethod method,
            string requestUrl,
            string appId,
            string accessToken)
        {
            Log.Debug(
                "Game API request: {0} {1} | appId={2} | tokenLen={3}",
                method,
                requestUrl,
                appId,
                accessToken?.Length ?? 0);

            string relativePath = ExtractRelativePath(requestUrl);
            GameApiMetricsCollector.Instance?.OnRequestStarted(relativePath, method.Method);

            await AcquireRateLimitTokenAsync().ConfigureAwait(false);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var response = await _retryPolicy.ExecuteAsync(
                    () => _circuitBreakerPolicy.ExecuteAsync(() =>
                    {
                        var request = new HttpRequestMessage(method, requestUrl);
                        request.Headers.Add("Authorization", "Bearer " + accessToken);
                        request.Headers.Add("X-App-Id", appId);
                        return _httpClient.SendAsync(request);
                    })).ConfigureAwait(false);

                stopwatch.Stop();

                string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                long bytesReceived = responseBody != null ? Encoding.UTF8.GetByteCount(responseBody) : 0;

                Log.Debug(
                    "Game API response: {0} {1} → HTTP {2} ({3}) | {4}ms | bodyLen={5}",
                    method,
                    requestUrl,
                    (int)response.StatusCode,
                    response.ReasonPhrase,
                    stopwatch.ElapsedMilliseconds,
                    responseBody?.Length ?? 0);

                if (Log.IsDebugEnabled && !string.IsNullOrEmpty(responseBody))
                {
                    Log.Debug("Game API response body: {0}", responseBody);
                }

                GameApiMetricsCollector.Instance?.OnRequestCompleted(
                    relativePath,
                    method.Method,
                    (int)response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    0,
                    bytesReceived);

                HandleRateLimitResponse(response);
                UpdateRateLimitFromHeaders(response);

                return response;
            }
            catch (BrokenCircuitException ex)
            {
                stopwatch.Stop();
                GameApiMetricsCollector.Instance?.OnRequestFailed(
                    relativePath, method.Method, "CircuitBreakerRejection", ex.Message);
                throw;
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                GameApiMetricsCollector.Instance?.OnRequestFailed(
                    relativePath, method.Method, "NetworkError", ex.Message);
                throw;
            }
            catch (TaskCanceledException ex)
            {
                stopwatch.Stop();
                GameApiMetricsCollector.Instance?.OnRequestFailed(
                    relativePath, method.Method, "Timeout", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Extracts the relative path (AbsolutePath) from a full request URL.
        /// Returns the original string if parsing fails.
        /// </summary>
        /// <param name="requestUrl">The full request URL.</param>
        /// <returns>The relative path portion of the URL.</returns>
        private string ExtractRelativePath(string requestUrl)
        {
            if (string.IsNullOrEmpty(requestUrl))
            {
                return string.Empty;
            }

            try
            {
                var uri = new Uri(requestUrl);
                return uri.AbsolutePath;
            }
            catch (UriFormatException)
            {
                return requestUrl;
            }
        }
    }
}
