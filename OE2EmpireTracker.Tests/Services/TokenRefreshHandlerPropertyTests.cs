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
using OE2EmpireTracker.Common.Client.Generated;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Tests.Helpers;

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
        /// <remarks>
        /// This previously derived from the <c>GameApiClient</c> shim and overrode its
        /// public tuple-returning <c>ExchangeTokenAsync</c>. That never intercepted
        /// anything: <see cref="TokenRefreshHandler"/> calls the
        /// <see cref="IGameApiTypedClient"/> member, which the shim implements
        /// *explicitly*, and an explicit interface implementation cannot be overridden by
        /// a derived class. So every call fell through to the real typed client and tried
        /// to reach `http://localhost`. The test could not have passed, and nobody knew
        /// because a missing Polly reference made the fixture fail to load first.
        ///
        /// Deriving from <see cref="OfflineGameApiClientStub"/> and overriding the
        /// interface member through a normal virtual gets the interception the original
        /// was reaching for.
        /// </remarks>
        private class FakeGameApiClient : OfflineGameApiClientStub
        {
            private int _exchangeCallCount;

            /// <summary>
            /// Gets the number of times ExchangeTokenAsync was called.
            /// </summary>
            public int ExchangeCallCount => _exchangeCallCount;

            /// <summary>
            /// Gets or sets the token value to return on success.
            /// </summary>
            public string FakeNewToken { get; set; } = "refreshed-token-abc";

            /// <inheritdoc/>
            public override async Task<TokenResponseDto> ExchangeTokenAsync(
                string appId,
                string clientId,
                string secret,
                CancellationToken ct = default)
            {
                Interlocked.Increment(ref _exchangeCallCount);

                // Simulate network latency to amplify the concurrency window
                await Task.Delay(20, ct).ConfigureAwait(false);

                return new TokenResponseDto
                {
                    AccessToken = this.FakeNewToken,
                    ExpiresIn = 3600,
                };
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
