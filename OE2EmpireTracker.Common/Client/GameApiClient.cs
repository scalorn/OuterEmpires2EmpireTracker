// <copyright file="GameApiClient.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
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
                "Game API request: {0} {1} (token length: {2})",
                method,
                requestUrl,
                accessToken?.Length ?? 0);

            await AcquireRateLimitTokenAsync().ConfigureAwait(false);

            var response = await _retryPolicy.ExecuteAsync(
                () => _circuitBreakerPolicy.ExecuteAsync(() =>
                {
                    var request = new HttpRequestMessage(method, requestUrl);
                    request.Headers.Add("Authorization", "Bearer " + accessToken);
                    request.Headers.Add("X-App-Id", appId);
                    return _httpClient.SendAsync(request);
                })).ConfigureAwait(false);

            Log.Debug(
                "Game API response: {0} {1} \u2192 HTTP {2} ({3})",
                method,
                requestUrl,
                (int)response.StatusCode,
                response.ReasonPhrase);

            HandleRateLimitResponse(response);
            UpdateRateLimitFromHeaders(response);

            return response;
        }
    }
}
