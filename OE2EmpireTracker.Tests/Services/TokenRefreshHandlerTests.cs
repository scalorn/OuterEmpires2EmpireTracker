// <copyright file="TokenRefreshHandlerTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="TokenRefreshHandler"/>.
    /// Tests serialization of concurrent 401 handling, stale-check optimization,
    /// and failure path behavior.
    /// Feature: queue-based-sync
    /// **Validates: Requirements 7.1, 7.2, 7.3**
    /// </summary>
    [TestFixture]
    public class TokenRefreshHandlerTests
    {
        private const string TestAppId = "test-app-id";
        private const string TestClientId = "test-client-id";
        private const string TestPlayerUUID = "player-uuid-123";
        private const string InitialToken = "initial-access-token";
        private const string RefreshedToken = "refreshed-access-token";
        private const string TestSecret = "test-secret-value";

        private GameApiConnectionSettings _settings;
        private string _tempSecretsDir;
        private GameApiCredentialManager _credentialManager;

        /// <summary>
        /// Sets up test fixtures including connection settings and credential manager.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _settings = new GameApiConnectionSettings
            {
                AppId = TestAppId,
                ClientId = TestClientId,
            };

            _tempSecretsDir = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "TokenRefreshHandlerTests_" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(_tempSecretsDir);

            string secretsFilePath = System.IO.Path.Combine(_tempSecretsDir, "secrets.dat");

            GameApiCredentialManager.RegisterProtectionFunctions(
                plain => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plain)),
                protectedBase64 =>
                {
                    string plain = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(protectedBase64));
                    var ss = new SecureString();
                    foreach (char c in plain)
                    {
                        ss.AppendChar(c);
                    }

                    ss.MakeReadOnly();
                    return ss;
                });

            _credentialManager = new GameApiCredentialManager(secretsFilePath);
            _credentialManager.StoreKey(TestPlayerUUID, TestSecret);
        }

        /// <summary>
        /// Cleans up temp secrets directory.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            try
            {
                if (System.IO.Directory.Exists(_tempSecretsDir))
                {
                    System.IO.Directory.Delete(_tempSecretsDir, true);
                }
            }
            catch
            {
                // Best effort cleanup
            }
        }

        /// <summary>
        /// When multiple concurrent 401 responses call HandleUnauthorizedAsync with the same
        /// failed token, only one ExchangeTokenAsync call should be made. All concurrent callers
        /// should receive the refreshed token.
        /// </summary>
        [Test]
        public async Task HandleUnauthorizedAsync_MultipleConcurrent401s_ProducesExactlyOneExchange()
        {
            int exchangeCallCount = 0;
            var handler = new CountingHttpHandler(
                () =>
                {
                    Interlocked.Increment(ref exchangeCallCount);
                    return CreateSuccessTokenResponse(RefreshedToken);
                });

            using (var apiClient = CreateClientWithHandler(handler))
            {
                var refreshHandler = new TokenRefreshHandler(
                    apiClient,
                    _settings,
                    _credentialManager,
                    TestPlayerUUID,
                    InitialToken);

                // Launch multiple concurrent 401 handlers with the same failed token
                var tasks = new List<Task<TokenRefreshResult>>();
                for (int i = 0; i < 10; i++)
                {
                    tasks.Add(refreshHandler.HandleUnauthorizedAsync(InitialToken, CancellationToken.None));
                }

                TokenRefreshResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

                // All should succeed
                foreach (var result in results)
                {
                    Assert.That(result.Success, Is.True, "All concurrent callers should receive success");
                    Assert.That(result.NewToken, Is.EqualTo(RefreshedToken), "All callers should get refreshed token");
                }

                // Only one actual exchange call should have been made
                Assert.That(exchangeCallCount, Is.EqualTo(1), "Only one ExchangeTokenAsync call should occur");
            }
        }

        /// <summary>
        /// When a second caller arrives after the token was already refreshed (different failed token
        /// from current), it should see the refreshed token immediately and skip the exchange call.
        /// </summary>
        [Test]
        public async Task HandleUnauthorizedAsync_SecondCallerSeesRefreshedToken_SkipsExchange()
        {
            int exchangeCallCount = 0;
            var handler = new CountingHttpHandler(
                () =>
                {
                    Interlocked.Increment(ref exchangeCallCount);
                    return CreateSuccessTokenResponse(RefreshedToken);
                });

            using (var apiClient = CreateClientWithHandler(handler))
            {
                var refreshHandler = new TokenRefreshHandler(
                    apiClient,
                    _settings,
                    _credentialManager,
                    TestPlayerUUID,
                    InitialToken);

                // First call triggers the actual exchange
                var result1 = await refreshHandler.HandleUnauthorizedAsync(InitialToken, CancellationToken.None)
                    .ConfigureAwait(false);
                Assert.That(result1.Success, Is.True);
                Assert.That(result1.NewToken, Is.EqualTo(RefreshedToken));
                Assert.That(exchangeCallCount, Is.EqualTo(1));

                // Second call with stale token (InitialToken) — but current is already RefreshedToken
                // so the stale-check should detect mismatch and skip exchange
                var result2 = await refreshHandler.HandleUnauthorizedAsync(InitialToken, CancellationToken.None)
                    .ConfigureAwait(false);
                Assert.That(result2.Success, Is.True);
                Assert.That(result2.NewToken, Is.EqualTo(RefreshedToken));

                // No additional exchange call should have been made
                Assert.That(exchangeCallCount, Is.EqualTo(1), "Second call should skip exchange due to stale-check");
            }
        }

        /// <summary>
        /// When the token exchange fails (API returns error), HandleUnauthorizedAsync should
        /// return a failure result with null token.
        /// </summary>
        [Test]
        public async Task HandleUnauthorizedAsync_ExchangeFailure_ReturnsFailureResult()
        {
            var handler = new CountingHttpHandler(
                () => CreateFailureTokenResponse());

            using (var apiClient = CreateClientWithHandler(handler))
            {
                var refreshHandler = new TokenRefreshHandler(
                    apiClient,
                    _settings,
                    _credentialManager,
                    TestPlayerUUID,
                    InitialToken);

                var result = await refreshHandler.HandleUnauthorizedAsync(InitialToken, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.That(result.Success, Is.False, "Should return failure when exchange fails");
                Assert.That(result.NewToken, Is.Null, "Token should be null on failure");
            }
        }

        /// <summary>
        /// When no credential is available for the player (GetKey returns null),
        /// HandleUnauthorizedAsync should return failure without attempting exchange.
        /// </summary>
        [Test]
        public async Task HandleUnauthorizedAsync_NoCredentialAvailable_ReturnsFailure()
        {
            int exchangeCallCount = 0;
            var handler = new CountingHttpHandler(
                () =>
                {
                    Interlocked.Increment(ref exchangeCallCount);
                    return CreateSuccessTokenResponse(RefreshedToken);
                });

            using (var apiClient = CreateClientWithHandler(handler))
            {
                // Use a player UUID that has no stored credential
                var refreshHandler = new TokenRefreshHandler(
                    apiClient,
                    _settings,
                    _credentialManager,
                    "unknown-player-uuid",
                    InitialToken);

                var result = await refreshHandler.HandleUnauthorizedAsync(InitialToken, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.That(result.Success, Is.False, "Should return failure when no credential is available");
                Assert.That(result.NewToken, Is.Null, "Token should be null when no credential available");
                Assert.That(exchangeCallCount, Is.EqualTo(0), "No exchange call should be made without credentials");
            }
        }

        /// <summary>
        /// After a successful token refresh, the CurrentAccessToken property should reflect
        /// the new token value.
        /// </summary>
        [Test]
        public async Task HandleUnauthorizedAsync_Success_UpdatesCurrentAccessToken()
        {
            var handler = new CountingHttpHandler(
                () => CreateSuccessTokenResponse(RefreshedToken));

            using (var apiClient = CreateClientWithHandler(handler))
            {
                var refreshHandler = new TokenRefreshHandler(
                    apiClient,
                    _settings,
                    _credentialManager,
                    TestPlayerUUID,
                    InitialToken);

                Assert.That(refreshHandler.CurrentAccessToken, Is.EqualTo(InitialToken));

                await refreshHandler.HandleUnauthorizedAsync(InitialToken, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.That(refreshHandler.CurrentAccessToken, Is.EqualTo(RefreshedToken));
            }
        }

        /// <summary>
        /// Creates a GameApiClient with a fake HTTP handler injected via reflection.
        /// </summary>
        private static GameApiClient CreateClientWithHandler(HttpMessageHandler handler)
        {
            var client = new GameApiClient("http://fake-server");
            var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30),
            };

            // Replace the private _httpClient field with our fake-backed one
            FieldInfo httpClientField = typeof(GameApiClient).GetField(
                "_httpClient",
                BindingFlags.NonPublic | BindingFlags.Instance);
            httpClientField.SetValue(client, httpClient);

            return client;
        }

        /// <summary>
        /// Creates an HTTP response body for a successful token exchange.
        /// </summary>
        private static HttpResponseMessage CreateSuccessTokenResponse(string accessToken)
        {
            var tokenResponse = new GameApiServiceResponse<GameApiTokenResponse>
            {
                Success = true,
                ReturnCode = 0,
                Data = new GameApiTokenResponse
                {
                    AccessToken = accessToken,
                    TokenType = "Bearer",
                    ExpiresIn = 3600,
                    CharacterId = 1,
                    Scopes = new List<string> { "read", "write" },
                    Subscription = "standard",
                },
            };

            string json = JsonConvert.SerializeObject(tokenResponse);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            };

            return response;
        }

        /// <summary>
        /// Creates an HTTP response body for a failed token exchange (HTTP 401).
        /// </summary>
        private static HttpResponseMessage CreateFailureTokenResponse()
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(
                    "{\"success\":false,\"returnCode\":401,\"returnString\":\"Invalid credentials\"}",
                    System.Text.Encoding.UTF8,
                    "application/json"),
            };
        }

        /// <summary>
        /// A fake HTTP handler that counts invocations and returns controlled responses.
        /// Includes a configurable delay to simulate network latency for concurrency testing.
        /// </summary>
        private class CountingHttpHandler : HttpMessageHandler
        {
            private readonly Func<HttpResponseMessage> _responseFactory;

            public CountingHttpHandler(Func<HttpResponseMessage> responseFactory)
            {
                _responseFactory = responseFactory;
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                // Small delay to make concurrency test more realistic
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                return _responseFactory();
            }
        }
    }
}
