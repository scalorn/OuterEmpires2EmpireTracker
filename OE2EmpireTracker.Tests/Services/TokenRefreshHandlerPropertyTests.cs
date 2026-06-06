// <copyright file="TokenRefreshHandlerPropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for TokenRefreshHandler serialization behavior.
    /// Feature: queue-based-sync
    /// </summary>
    [TestFixture]
    public class TokenRefreshHandlerPropertyTests
    {
        private string _tempDir;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            GameApiCredentialManager.RegisterProtectionFunctions(
                CredentialStore.Protect,
                CredentialStore.Unprotect);
        }

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(
                Path.GetTempPath(),
                "OE2Tests_TokenRefresh_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch
            {
            }
        }

        // ---------------------------------------------------------------
        // Test Infrastructure
        // ---------------------------------------------------------------

        /// <summary>
        /// A testable GameApiClient subclass that overrides ExchangeTokenAsync
        /// to count invocations and return a controlled token.
        /// </summary>
        private class FakeGameApiClient : GameApiClient
        {
            private int _exchangeCallCount;

            public FakeGameApiClient()
                : base("http://localhost")
            {
            }

            /// <summary>
            /// Gets the number of times ExchangeTokenAsync was called.
            /// </summary>
            public int ExchangeCallCount => _exchangeCallCount;

            /// <summary>
            /// Gets or sets the token value to return on success.
            /// </summary>
            public string FakeNewToken { get; set; } = "refreshed-token-abc";

            /// <inheritdoc/>
            public override async Task<(bool Success, GameApiTokenResponse Token, string ErrorMessage)> ExchangeTokenAsync(
                string appId,
                string clientId,
                string secret)
            {
                Interlocked.Increment(ref _exchangeCallCount);

                // Simulate network latency to amplify concurrency window
                await Task.Delay(20).ConfigureAwait(false);

                var token = new GameApiTokenResponse
                {
                    AccessToken = this.FakeNewToken,
                    ExpiresIn = 3600,
                };

                return (true, token, null);
            }
        }

        // ---------------------------------------------------------------
        // Property 2: Token Refresh Serialization
        // When multiple work items receive 401 simultaneously, only one
        // ExchangeTokenAsync call is made. All others wait and reuse
        // the refreshed token.
        // **Validates: Requirements 7.4**
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 2: For any N concurrent 401 events (2-50), exactly 1
        /// ExchangeTokenAsync call occurs and all N callers receive Success=true
        /// with the same NewToken value.
        /// **Validates: Requirements 7.4**
        /// </summary>
        [Test]
        public void ConcurrentUnauthorized_ExactlyOneExchangeCall()
        {
            var concurrencyGen = Gen.Choose(2, 50);

            Prop.ForAll(concurrencyGen.ToArbitrary(), (n) =>
            {
                using (var fakeClient = new FakeGameApiClient())
                {
                    string secretsPath = Path.Combine(
                        _tempDir,
                        Guid.NewGuid().ToString("N") + ".dat");
                    var credManager = new GameApiCredentialManager(secretsPath);

                    string playerUUID = Guid.NewGuid().ToString();
                    string testSecret = "test-secret-" + playerUUID.Substring(0, 8);
                    credManager.StoreKey(playerUUID, testSecret);

                    var settings = new GameApiConnectionSettings
                    {
                        AppId = "test-app",
                        ClientId = "test-client",
                    };

                    string initialToken = "expired-token-" + Guid.NewGuid().ToString("N");
                    string expectedNewToken = fakeClient.FakeNewToken;

                    var handler = new TokenRefreshHandler(
                        fakeClient,
                        settings,
                        credManager,
                        playerUUID,
                        initialToken);

                    // Launch N concurrent HandleUnauthorizedAsync calls
                    // all with the same failedToken (the initial token)
                    var tasks = Enumerable.Range(0, n)
                        .Select(_ => handler.HandleUnauthorizedAsync(initialToken, CancellationToken.None))
                        .ToArray();

                    Task.WaitAll(tasks);

                    var results = tasks.Select(t => t.Result).ToArray();

                    bool exactlyOneCall = fakeClient.ExchangeCallCount == 1;
                    bool allSucceeded = results.All(r => r.Success);
                    bool allSameToken = results.All(r => r.NewToken == expectedNewToken);

                    return (exactlyOneCall && allSucceeded && allSameToken)
                        .Label($"N={n}, ExchangeCalls={fakeClient.ExchangeCallCount}, " +
                               $"AllSuccess={allSucceeded}, AllSameToken={allSameToken}");
                }
            }).QuickCheckThrowOnFailure();
        }
    }
}
