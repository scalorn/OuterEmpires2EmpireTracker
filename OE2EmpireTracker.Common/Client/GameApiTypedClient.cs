// <copyright file="GameApiTypedClient.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using OE2EmpireTracker.Common.Client.Generated;
using OE2EmpireTracker.Services;
using Polly;
using Polly.CircuitBreaker;

namespace OE2EmpireTracker.Common.Client
{
    /// <summary>
    /// Strongly-typed API client wrapper that adds resilience policies (retry, circuit breaker),
    /// rate limiting, and envelope unwrapping around the NSwag-generated client.
    /// </summary>
    public class GameApiTypedClient : IGameApiTypedClient
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly GameApiGeneratedClient _generatedClient;
        private readonly HttpClient _httpClient;
        private readonly TokenBucketRateLimiter _rateLimiter;
        private readonly IAsyncPolicy _retryPolicy;
        private readonly IAsyncPolicy _circuitBreaker;
        private readonly IAsyncPolicy _policyWrap;
        private readonly ConcurrentDictionary<string, CachedToken> _tokenCache;
        private readonly string _appId;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameApiTypedClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use for API requests.</param>
        /// <param name="appId">The application ID for header injection.</param>
        public GameApiTypedClient(HttpClient httpClient, string appId)
            : this(httpClient, appId, 0.9)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameApiTypedClient"/> class
        /// with a custom rate limiter TPS.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use for API requests.</param>
        /// <param name="appId">The application ID for header injection.</param>
        /// <param name="tps">The target transactions per second for the rate limiter.</param>
        public GameApiTypedClient(HttpClient httpClient, string appId, double tps)
        {
            _httpClient = httpClient;
            _generatedClient = new GameApiGeneratedClient(httpClient);
            _appId = appId;
            _rateLimiter = new TokenBucketRateLimiter(tps);
            _tokenCache = new ConcurrentDictionary<string, CachedToken>();

            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<ApiHttpException>(ex => ex.StatusCode >= 500)
                .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)));

            _circuitBreaker = Policy
                .Handle<HttpRequestException>()
                .Or<ApiHttpException>(ex => ex.StatusCode >= 500)
                .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30));

            _policyWrap = Policy.WrapAsync(_retryPolicy, _circuitBreaker);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameApiTypedClient"/> class
        /// with a server URL, creating its own <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="serverUrl">The base URL of the game API server.</param>
        /// <param name="appId">The application ID for header injection.</param>
        /// <param name="tps">The target transactions per second for the rate limiter.</param>
        public GameApiTypedClient(string serverUrl, string appId, double tps)
            : this(new HttpClient(new MetricsTrackingHandler()) { BaseAddress = new Uri(serverUrl) }, appId, tps)
        {
        }

        /// <inheritdoc/>
        public bool IsCircuitOpen
        {
            get
            {
                var breaker = (AsyncCircuitBreakerPolicy)_circuitBreaker;
                return breaker.CircuitState == CircuitState.Open
                    || breaker.CircuitState == CircuitState.Isolated;
            }
        }

        /// <inheritdoc/>
        public async Task<TokenResponseDto> ExchangeTokenAsync(string appId, string clientId, string secret, CancellationToken ct = default)
        {
            string key = ComputeCacheKey(clientId, secret);
            if (_tokenCache.TryGetValue(key, out var cached) && !cached.IsExpired)
            {
                _generatedClient.SetBearerToken(cached.AccessToken);
                return new TokenResponseDto { AccessToken = cached.AccessToken, ExpiresIn = cached.ExpiresIn };
            }

            // Token exchange bypasses both rate limiter and Polly policies
            var response = await _generatedClient.ExchangeTokenAsync(
                new TokenRequestDto
                {
                    AppId = appId,
                    ClientId = clientId,
                    Secret = secret,
                    GrantType = "client_credentials",
                },
                ct).ConfigureAwait(false);

            if (!response.Success)
            {
                var validationErrors = response.Errors == null
                    ? Array.Empty<ApiValidationError>()
                    : response.Errors.Select(e => new ApiValidationError { Field = e.Field, Error = e.Error }).ToArray();
                throw new ApiBusinessException(response.ReturnCode, response.ReturnString, validationErrors);
            }

            var token = response.Data;
            _tokenCache[key] = new CachedToken(token.AccessToken, token.ExpiresIn);
            _generatedClient.SetBearerToken(token.AccessToken);
            return token;
        }

        /// <inheritdoc/>
        public async Task<bool> TestConnectionAsync(string appId, string clientId, string secret, CancellationToken ct = default)
        {
            try
            {
                await this.ExchangeTokenAsync(appId, clientId, secret, ct).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "TestConnectionAsync failed.");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<AcceptedJobs> GetAcceptedJobsAsync(CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetAcceptedJobsAsync(_appId, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<AssetLocations> GetAssetLocationsAsync(CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetAssetsLocationsAsync(_appId, null, null, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<AssetLocationDetail> GetAssetLocationDetailAsync(int locationId, string locationType, CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetAssetsLocationDetailAsync(_appId, locationId, locationType, null, null, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<AssetCrateContents> GetCrateContentsAsync(int crateId, CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetAssetsCrateAsync(_appId, crateId, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<KillMailList> GetKillMailListAsync(bool? kills = null, bool? deaths = null, bool? pvp = null, int? offset = null, int? limit = null, CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetKillMailListAsync(_appId, kills, deaths, pvp, offset, limit, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<KillMail> GetKillMailDetailAsync(int killMailId, CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetKillMailDetailAsync(_appId, killMailId, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<AssetBlueprint> GetBlueprintDetailAsync(int blueprintId, CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetAssetsBlueprintAsync(_appId, blueprintId, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<AssetSurvey> GetSurveyDetailAsync(int surveyId, CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetAssetsSurveyAsync(_appId, surveyId, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<BankingBalance> GetBankingBalanceAsync(CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetBankingBalanceAsync(_appId, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<BankingTransactions> GetBankingTransactionsAsync(int? offset = null, int? limit = null, CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetBankingTransactionsAsync(_appId, offset, limit, null, null, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PublicCharacter> GetCharacterAsync(CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetCharacterAsync(_appId, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<CharacterSkills> GetCharacterSkillsAsync(CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetCharacterSkillsAsync(_appId, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ColonyList> GetColonyListAsync(CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetColonyListAsync(_appId, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ColonySummary> GetColonySummaryAsync(int colonyId, CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetColonySummaryAsync(_appId, colonyId, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ColonyBuildings> GetColonyBuildingsAsync(int colonyId, CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetColonyBuildingsAsync(_appId, colonyId, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ColonyWarehouse> GetColonyWarehouseAsync(int colonyId, CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetColonyWarehouseAsync(_appId, colonyId, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ColonyWorkers> GetColonyWorkersAsync(int colonyId, CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetColonyWorkersAsync(_appId, colonyId, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ShipConfiguration> GetShipConfigurationAsync(CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetShipConfigurationAsync(_appId, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ShipCargo> GetShipCargoAsync(CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetShipCargoAsync(_appId, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MailList> GetMailListAsync(int? offset = null, int? limit = null, string mailType = null, CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetMailListAsync(_appId, null, offset, limit, mailType, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MailBody> GetMailBodyAsync(int mailId, CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetMailBodyAsync(_appId, mailId, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MarketListings> GetMarketListingsAsync(string view, int? range = null, string search = null, CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetMarketListingsAsync(_appId, view, range, search, null, null, null, null, null, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MarketPriceStats> GetMarketPricesAsync(string type, long typeId, CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetMarketPricesAsync(_appId, type, typeId, null, null, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MarketItems> GetMarketItemsAsync(string type, string search, CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetMarketItemsAsync(_appId, type, search, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MarketBuyOrders> GetMarketBuyOrdersAsync(CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetMarketBuyOrdersAsync(_appId, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MarketSellOrders> GetMarketSellOrdersAsync(CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetMarketSellOrdersAsync(_appId, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MarketCompetitorOrders> GetMarketBuyOrderCompetitorsAsync(string marketIds, CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetMarketBuyOrderCompetitorsAsync(_appId, marketIds, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MarketCompetitorOrders> GetMarketSellOrderCompetitorsAsync(string marketIds, CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetMarketSellOrderCompetitorsAsync(_appId, marketIds, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MarketShipComponents> GetMarketShipComponentsAsync(long marketId, CancellationToken ct = default)
        {
            return await ExecuteAsync(async token =>
            {
                var response = await _generatedClient.GetMarketShipComponentsAsync(_appId, marketId, token).ConfigureAwait(false);
                return UnwrapEnvelope(response.Success, response.ReturnCode, response.ReturnString, response.Data, response.Errors);
            }, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing">True to release managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Log.Debug("GameApiTypedClient disposed.");
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Computes a stable cache key from clientId and secret using SHA256.
        /// </summary>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="secret">The client secret.</param>
        /// <returns>A hex string hash suitable for use as a dictionary key.</returns>
        private static string ComputeCacheKey(string clientId, string secret)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(clientId + ":" + secret));
                var sb = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++)
                {
                    sb.Append(bytes[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Unwraps the API envelope, returning the data payload on success
        /// or throwing <see cref="ApiBusinessException"/> on failure.
        /// </summary>
        /// <typeparam name="T">The inner data type.</typeparam>
        /// <param name="success">The envelope success flag.</param>
        /// <param name="returnCode">The envelope return code.</param>
        /// <param name="returnString">The envelope return string.</param>
        /// <param name="data">The data payload (may be null).</param>
        /// <param name="errors">The envelope validation errors collection.</param>
        /// <returns>The data payload, or null if the envelope data field was null.</returns>
        private static T UnwrapEnvelope<T>(
            bool success,
            int returnCode,
            string returnString,
            T data,
            ICollection<ValidationError> errors)
        {
            if (success)
            {
                return data;
            }

            var validationErrors = errors == null
                ? Array.Empty<ApiValidationError>()
                : errors.Select(e => new ApiValidationError { Field = e.Field, Error = e.Error }).ToArray();

            throw new ApiBusinessException(returnCode, returnString, validationErrors);
        }

        /// <summary>
        /// Executes an action through the resilience pipeline (circuit breaker check,
        /// rate limiter acquire, Polly policy wrap). Catches NSwag-generated
        /// <see cref="Generated.ApiException"/> and converts to typed exceptions.
        /// Metrics are reported by <see cref="MetricsTrackingHandler"/> at the HTTP level.
        /// </summary>
        /// <typeparam name="T">The return type of the action.</typeparam>
        /// <param name="action">The async action to execute.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The result of the action.</returns>
        private async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
        {
            if (this.IsCircuitOpen)
            {
                throw new BrokenCircuitException("The circuit breaker is open. Requests are not being dispatched.");
            }

            await _rateLimiter.AcquireAsync(ct).ConfigureAwait(false);
            try
            {
                return await _policyWrap.ExecuteAsync(
                    async (token) =>
                    {
                        try
                        {
                            return await action(token).ConfigureAwait(false);
                        }
                        catch (Generated.ApiException ex) when (ex.StatusCode == 429)
                        {
                            HandleRateLimitResponse(ex);
                            throw new ApiHttpException(ex.StatusCode, ex.Response, ex);
                        }
                        catch (Generated.ApiException ex) when (ex.InnerException is System.Text.Json.JsonException)
                        {
                            throw new ApiDeserializationException(ex.StatusCode, ex.Response, ex.InnerException);
                        }
                        catch (Generated.ApiException ex)
                        {
                            throw new ApiHttpException(ex.StatusCode, ex.Response, ex);
                        }
                    },
                    ct).ConfigureAwait(false);
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        /// <summary>
        /// Handles HTTP 429 (Too Many Requests) by parsing the Retry-After header
        /// and pausing the rate limiter for the specified duration.
        /// Defaults to 60 seconds if the header is absent or unparseable. Caps at 300 seconds.
        /// </summary>
        /// <param name="ex">The NSwag API exception containing response headers.</param>
        private void HandleRateLimitResponse(Generated.ApiException ex)
        {
            const int DefaultRetryAfterSeconds = 60;
            const int MaxRetryAfterSeconds = 300;

            int retryAfterSeconds = DefaultRetryAfterSeconds;

            if (ex.Headers != null
                && ex.Headers.TryGetValue("Retry-After", out var values))
            {
                string headerValue = null;
                foreach (var v in values)
                {
                    headerValue = v;
                    break;
                }

                if (!string.IsNullOrEmpty(headerValue) && int.TryParse(headerValue, out int parsed) && parsed > 0)
                {
                    retryAfterSeconds = parsed;
                }
            }

            if (retryAfterSeconds > MaxRetryAfterSeconds)
            {
                retryAfterSeconds = MaxRetryAfterSeconds;
            }

            Log.Warn("HTTP 429 received. Pausing rate limiter for {0}s.", retryAfterSeconds);
            _rateLimiter.PauseFor(TimeSpan.FromSeconds(retryAfterSeconds));
        }

        /// <summary>
        /// Represents a cached access token with expiry tracking.
        /// </summary>
        private sealed class CachedToken
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CachedToken"/> class.
            /// </summary>
            /// <param name="accessToken">The access token string.</param>
            /// <param name="expiresIn">The token lifetime in seconds.</param>
            public CachedToken(string accessToken, int expiresIn)
            {
                this.AccessToken = accessToken;
                this.ExpiresIn = expiresIn;
                this.ExpiresAtUtc = SystemClock.UtcNow.AddSeconds(expiresIn);
            }

            /// <summary>
            /// Gets the access token string.
            /// </summary>
            public string AccessToken { get; }

            /// <summary>
            /// Gets the token lifetime in seconds (as returned by the server).
            /// </summary>
            public int ExpiresIn { get; }

            /// <summary>
            /// Gets the UTC time at which this token expires.
            /// </summary>
            public DateTime ExpiresAtUtc { get; }

            /// <summary>
            /// Gets a value indicating whether the token has expired.
            /// </summary>
            public bool IsExpired => SystemClock.UtcNow >= this.ExpiresAtUtc;
        }
    }
}
