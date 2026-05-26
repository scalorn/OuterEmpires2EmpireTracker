// <copyright file="GameApiClient.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// HTTP client for communicating with the Outer Empires 2 game API.
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
        /// Tests connectivity by calling the game API health endpoint.
        /// </summary>
        /// <param name="apiKey">The plain-text API key for authentication.</param>
        /// <returns>A tuple indicating success and a descriptive message.</returns>
        public async Task<(bool Success, string Message)> CheckHealthAsync(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                return (false, "API key is not configured");
            }

            try
            {
                var response = await ExecuteWithPoliciesAsync(
                    HttpMethod.Get,
                    _serverUrl + "/health",
                    apiKey).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Connected");
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Log.Warn("Game API health check received HTTP 401 — API key is invalid");
                    return (false, "Health check failed: HTTP 401");
                }

                return (false, string.Format("Health check failed: HTTP {0}", (int)response.StatusCode));
            }
            catch (BrokenCircuitException)
            {
                return (false, "Circuit breaker is open — too many consecutive failures");
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API health check failed for {0}", _serverUrl);
                return (false, string.Format("Connection failed: {0}", ex.Message));
            }
            catch (TaskCanceledException)
            {
                return (false, "Health check timed out");
            }
        }

        /// <summary>
        /// Retrieves the player profile JSON from the game API.
        /// </summary>
        /// <param name="apiKey">The plain-text API key for authentication.</param>
        /// <returns>A tuple indicating success and the raw JSON response body.</returns>
        public async Task<(bool Success, string Json)> GetPlayerProfileAsync(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                return (false, null);
            }

            try
            {
                var response = await ExecuteWithPoliciesAsync(
                    HttpMethod.Get,
                    _serverUrl + "/api/v1/player/profile",
                    apiKey).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return (true, json);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Log.Warn("Game API GetPlayerProfile received HTTP 401 — API key is invalid");
                    return (false, "401");
                }

                Log.Warn("Game API GetPlayerProfile failed: HTTP {0}", (int)response.StatusCode);
                return (false, null);
            }
            catch (BrokenCircuitException)
            {
                Log.Warn("Game API GetPlayerProfile blocked by open circuit breaker");
                return (false, null);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "Game API GetPlayerProfile request failed");
                return (false, null);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Game API GetPlayerProfile request timed out");
                return (false, null);
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
            if (pauseUntil > DateTime.UtcNow)
            {
                TimeSpan waitDuration = pauseUntil - DateTime.UtcNow;
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

            _rateLimitPauseUntil = DateTime.UtcNow.AddSeconds(pauseSeconds);
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
        /// </summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="requestUrl">The full request URL.</param>
        /// <param name="apiKey">The plain-text API key for the X-API-Key header.</param>
        /// <returns>The HTTP response message.</returns>
        private async Task<HttpResponseMessage> ExecuteWithPoliciesAsync(
            HttpMethod method,
            string requestUrl,
            string apiKey)
        {
            await AcquireRateLimitTokenAsync().ConfigureAwait(false);

            var response = await _retryPolicy.ExecuteAsync(
                () => _circuitBreakerPolicy.ExecuteAsync(() =>
                {
                    var request = new HttpRequestMessage(method, requestUrl);
                    request.Headers.Add("X-API-Key", apiKey);
                    return _httpClient.SendAsync(request);
                })).ConfigureAwait(false);

            HandleRateLimitResponse(response);
            UpdateRateLimitFromHeaders(response);

            return response;
        }
    }
}
