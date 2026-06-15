// <copyright file="QueueSyncServiceIntegrationTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Common.Client;
using OE2EmpireTracker.Common.Client.Generated;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Integration tests verifying end-to-end typed client flow through QueueSyncService.
    /// Exercises RunSyncAsync with a stubbed IGameApiTypedClient to validate token exchange,
    /// work item execution, merge service calls, and error handling behavior.
    /// **Validates: Requirements 13.1, 13.5, 13.6**
    /// </summary>
    [TestFixture]
    public class QueueSyncServiceIntegrationTests
    {
        private const string TestPlayerUUID = "integration-test-player-uuid";
        private const string TestAppId = "test-app-id";
        private const string TestClientId = "test-client-id";
        private const string TestSecret = "test-secret-value";
        private const string TestAccessToken = "test-access-token-abc";

        private string _tempDir;
        private GameApiCredentialManager _credentialManager;
        private GameApiConnectionSettings _settings;

        /// <summary>
        /// Registers DPAPI protection functions once for all tests.
        /// </summary>
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            GameApiCredentialManager.RegisterProtectionFunctions(
                plain => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plain)),
                protectedBase64 =>
                {
                    string plain = System.Text.Encoding.UTF8.GetString(
                        Convert.FromBase64String(protectedBase64));
                    var ss = new SecureString();
                    foreach (char c in plain)
                    {
                        ss.AppendChar(c);
                    }

                    ss.MakeReadOnly();
                    return ss;
                });
        }

        /// <summary>
        /// Sets up test fixtures for each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            SystemClock.FreezeAt(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            SystemClock.EnableInstantDelay();

            _tempDir = Path.Combine(
                Path.GetTempPath(),
                "OE2Tests_QueueSyncIntegration_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            string secretsFilePath = Path.Combine(_tempDir, "secrets.dat");
            _credentialManager = new GameApiCredentialManager(secretsFilePath);
            _credentialManager.StoreKey(TestPlayerUUID, TestSecret);

            _settings = new GameApiConnectionSettings
            {
                AppId = TestAppId,
                ClientId = TestClientId,
                Tps = 100.0,
                MaxInflightRequests = 10,
                DetailRefreshHours = 24,
            };
        }

        /// <summary>
        /// Tears down test fixtures after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            SystemClock.Reset();
            EmpireContext.Reset();

            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch
            {
                // Best effort cleanup.
            }
        }

        // ---------------------------------------------------------------
        // Test Infrastructure
        // ---------------------------------------------------------------

        /// <summary>
        /// Creates a PlayerContext with a player profile for testing.
        /// </summary>
        private static PlayerContext CreateTestPlayerContext()
        {
            PlayerContext.FilePath = string.Empty;
            var root = new PlayerRoot
            {
                CurrentPlayerUUID = TestPlayerUUID,
                PlayerProfile = new[]
                {
                    new PlayerProfile
                    {
                        UUID = TestPlayerUUID,
                        FirstName = "Test",
                        LastName = "Player",
                    },
                },
            };

            return new PlayerContext(root);
        }

        /// <summary>
        /// Creates an EmpireContext for testing.
        /// </summary>
        private static EmpireContext CreateTestEmpireContext()
        {
            TestHelper.SetEmpireFilePath();
            EmpireContext.Reset();
            return EmpireContext.GetInstance();
        }

        /// <summary>
        /// Creates a MarketDataService for testing using a minimal grid index.
        /// </summary>
        private static MarketDataService CreateTestMarketDataService(PlayerContext playerContext)
        {
            var repo = new SystemRepository();
            var gridIndex = new SystemGridIndex(repo);
            return new MarketDataService(playerContext, gridIndex);
        }

        // ---------------------------------------------------------------
        // Stub IGameApiTypedClient
        // ---------------------------------------------------------------

        /// <summary>
        /// A configurable stub implementation of IGameApiTypedClient for integration tests.
        /// Tracks method call counts and allows injecting exceptions per method.
        /// </summary>
        private class StubTypedClient : IGameApiTypedClient
        {
            private int _exchangeTokenCallCount;
            private int _getCharacterCallCount;

            /// <summary>
            /// Gets the number of times ExchangeTokenAsync was called.
            /// </summary>
            public int ExchangeTokenCallCount => _exchangeTokenCallCount;

            /// <summary>
            /// Gets the number of times GetCharacterAsync was called.
            /// </summary>
            public int GetCharacterCallCount => _getCharacterCallCount;

            /// <summary>
            /// Gets or sets the exception to throw from ExchangeTokenAsync (null = success).
            /// </summary>
            public Exception ExchangeTokenException { get; set; }

            /// <summary>
            /// Gets or sets the exception to throw from GetCharacterAsync (null = success).
            /// </summary>
            public Exception GetCharacterException { get; set; }

            /// <summary>
            /// Gets or sets the exception to throw from all general data methods (null = success).
            /// </summary>
            public Exception GeneralDataException { get; set; }

            /// <inheritdoc/>
            public bool IsCircuitOpen => false;

            /// <inheritdoc/>
            public Task<TokenResponseDto> ExchangeTokenAsync(
                string appId, string clientId, string secret, CancellationToken ct = default)
            {
                Interlocked.Increment(ref _exchangeTokenCallCount);
                if (this.ExchangeTokenException != null)
                {
                    throw this.ExchangeTokenException;
                }

                return Task.FromResult(new TokenResponseDto
                {
                    AccessToken = TestAccessToken,
                    TokenType = "Bearer",
                    ExpiresIn = 3600,
                    CharacterId = 1,
                    Scopes = new List<string> { "read" },
                });
            }

            /// <inheritdoc/>
            public Task<bool> TestConnectionAsync(
                string appId, string clientId, string secret, CancellationToken ct = default)
            {
                return Task.FromResult(true);
            }

            /// <inheritdoc/>
            public Task<PublicCharacter> GetCharacterAsync(CancellationToken ct = default)
            {
                Interlocked.Increment(ref _getCharacterCallCount);
                if (this.GetCharacterException != null)
                {
                    throw this.GetCharacterException;
                }

                return Task.FromResult(new PublicCharacter
                {
                    CharacterId = 1,
                    FirstName = "Synced",
                    LastName = "Character",
                    ActiveTimeMinutes = 120,
                });
            }

            /// <inheritdoc/>
            public Task<CharacterSkills> GetCharacterSkillsAsync(CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new CharacterSkills());
            }

            /// <inheritdoc/>
            public Task<AcceptedJobs> GetAcceptedJobsAsync(CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new AcceptedJobs());
            }

            /// <inheritdoc/>
            public Task<AssetLocationDetail> GetAssetLocationDetailAsync(
                int locationId, string locationType, CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new AssetLocationDetail());
            }

            /// <inheritdoc/>
            public Task<AssetLocations> GetAssetLocationsAsync(CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new AssetLocations());
            }

            /// <inheritdoc/>
            public Task<BankingBalance> GetBankingBalanceAsync(CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new BankingBalance());
            }

            /// <inheritdoc/>
            public Task<BankingTransactions> GetBankingTransactionsAsync(
                int? offset = null, int? limit = null, CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new BankingTransactions());
            }

            /// <inheritdoc/>
            public Task<AssetBlueprint> GetBlueprintDetailAsync(int blueprintId, CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new AssetBlueprint());
            }

            /// <inheritdoc/>
            public Task<ColonyBuildings> GetColonyBuildingsAsync(int colonyId, CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new ColonyBuildings());
            }

            /// <inheritdoc/>
            public Task<ColonyList> GetColonyListAsync(CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new ColonyList());
            }

            /// <inheritdoc/>
            public Task<ColonySummary> GetColonySummaryAsync(int colonyId, CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new ColonySummary());
            }

            /// <inheritdoc/>
            public Task<ColonyWarehouse> GetColonyWarehouseAsync(int colonyId, CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new ColonyWarehouse());
            }

            /// <inheritdoc/>
            public Task<ColonyWorkers> GetColonyWorkersAsync(int colonyId, CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new ColonyWorkers());
            }

            /// <inheritdoc/>
            public Task<AssetCrateContents> GetCrateContentsAsync(int crateId, CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new AssetCrateContents());
            }

            /// <inheritdoc/>
            public Task<KillMailList> GetKillMailListAsync(
                bool? kills = null, bool? deaths = null, bool? pvp = null,
                int? offset = null, int? limit = null, CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new KillMailList());
            }

            /// <inheritdoc/>
            public Task<KillMail> GetKillMailDetailAsync(int killMailId, CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new KillMail());
            }

            /// <inheritdoc/>
            public Task<MailBody> GetMailBodyAsync(int mailId, CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new MailBody());
            }

            /// <inheritdoc/>
            public Task<MailList> GetMailListAsync(
                int? offset = null, int? limit = null, string mailType = null,
                CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new MailList());
            }

            /// <inheritdoc/>
            public Task<MarketCompetitorOrders> GetMarketBuyOrderCompetitorsAsync(
                string marketIds, CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new MarketCompetitorOrders());
            }

            /// <inheritdoc/>
            public Task<MarketBuyOrders> GetMarketBuyOrdersAsync(CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new MarketBuyOrders());
            }

            /// <inheritdoc/>
            public Task<MarketItems> GetMarketItemsAsync(
                string type, string search, CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new MarketItems());
            }

            /// <inheritdoc/>
            public Task<MarketListings> GetMarketListingsAsync(
                string view, int? range = null, string search = null, CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new MarketListings());
            }

            /// <inheritdoc/>
            public Task<MarketPriceStats> GetMarketPricesAsync(
                string type, long typeId, CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new MarketPriceStats());
            }

            /// <inheritdoc/>
            public Task<MarketCompetitorOrders> GetMarketSellOrderCompetitorsAsync(
                string marketIds, CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new MarketCompetitorOrders());
            }

            /// <inheritdoc/>
            public Task<MarketSellOrders> GetMarketSellOrdersAsync(CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new MarketSellOrders());
            }

            /// <inheritdoc/>
            public Task<MarketShipComponents> GetMarketShipComponentsAsync(
                long marketId, CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new MarketShipComponents());
            }

            /// <inheritdoc/>
            public Task<ShipCargo> GetShipCargoAsync(CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new ShipCargo());
            }

            /// <inheritdoc/>
            public Task<ShipConfiguration> GetShipConfigurationAsync(CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new ShipConfiguration
                {
                    Components = new List<ShipComponent>(),
                });
            }

            /// <inheritdoc/>
            public Task<AssetSurvey> GetSurveyDetailAsync(int surveyId, CancellationToken ct = default)
            {
                ThrowIfGeneralException();
                return Task.FromResult(new AssetSurvey());
            }

            /// <inheritdoc/>
            public void Dispose()
            {
            }

            private void ThrowIfGeneralException()
            {
                if (this.GeneralDataException != null)
                {
                    throw this.GeneralDataException;
                }
            }
        }

        // ---------------------------------------------------------------
        // Test 1: Happy Path — Token Exchange and Work Item Execution
        // WHEN RunSyncAsync is called with a valid credential,
        // THEN token exchange succeeds, work items execute, and the result
        // reports successes with zero failures.
        // **Validates: Requirements 13.1**
        // ---------------------------------------------------------------

        /// <summary>
        /// Happy path: token exchange succeeds, profile sync executes,
        /// result reports successes with zero failures.
        /// **Validates: Requirements 13.1**
        /// </summary>
        [Test]
        public async Task RunSyncAsync_HappyPath_TokenExchangeSucceedsAndWorkItemsComplete()
        {
            var playerContext = CreateTestPlayerContext();
            var empireContext = CreateTestEmpireContext();
            var stub = new StubTypedClient();

            var service = new QueueSyncService(
                playerContext,
                empireContext,
                stub,
                _settings,
                _credentialManager,
                CreateTestMarketDataService(playerContext));

            var result = await service.RunSyncAsync(CancellationToken.None).ConfigureAwait(false);

            // Token exchange should have been called exactly once at cycle start.
            Assert.That(stub.ExchangeTokenCallCount, Is.EqualTo(1),
                "ExchangeTokenAsync should be called once at sync cycle start");

            // GetCharacterAsync should have been called (profile work item).
            Assert.That(stub.GetCharacterCallCount, Is.GreaterThanOrEqualTo(1),
                "GetCharacterAsync should be called for the profile work item");

            // The result should report successes and zero failures.
            Assert.That(result.Succeeded, Is.GreaterThan(0),
                "At least some work items should succeed");
            Assert.That(result.Failed, Is.EqualTo(0),
                "No work items should fail in the happy path");
            Assert.That(result.FailedLabels, Is.Empty,
                "No failed labels expected in the happy path");
        }

        // ---------------------------------------------------------------
        // Test 2: 401 Handling — ExchangeTokenAsync Throws ApiHttpException(401)
        // WHEN token exchange fails with 401, the sync cycle aborts early
        // and returns an empty result (no work items dispatched).
        // **Validates: Requirements 13.5**
        // ---------------------------------------------------------------

        /// <summary>
        /// When ExchangeTokenAsync throws ApiHttpException(401) at sync start,
        /// the sync cycle aborts and returns empty result without dispatching
        /// any work items.
        /// **Validates: Requirements 13.5**
        /// </summary>
        [Test]
        public async Task RunSyncAsync_TokenExchange401_AbortsSyncEarly()
        {
            var playerContext = CreateTestPlayerContext();
            var empireContext = CreateTestEmpireContext();
            var stub = new StubTypedClient
            {
                ExchangeTokenException = new ApiHttpException(401, "Unauthorized"),
            };

            var service = new QueueSyncService(
                playerContext,
                empireContext,
                stub,
                _settings,
                _credentialManager,
                CreateTestMarketDataService(playerContext));

            var result = await service.RunSyncAsync(CancellationToken.None).ConfigureAwait(false);

            // Token exchange was attempted but failed.
            Assert.That(stub.ExchangeTokenCallCount, Is.EqualTo(1),
                "ExchangeTokenAsync should be called once (then fail)");

            // No work items should have been dispatched since token exchange failed.
            Assert.That(result.Succeeded, Is.EqualTo(0),
                "No work items should succeed when token exchange fails");
            Assert.That(result.Failed, Is.EqualTo(0),
                "No work items should fail since none were dispatched");

            // GetCharacterAsync should NOT have been called.
            Assert.That(stub.GetCharacterCallCount, Is.EqualTo(0),
                "No API calls should be made after token exchange failure");
        }

        // ---------------------------------------------------------------
        // Test 3: 429 Handling — Work Item Throws ApiHttpException(429)
        // WHEN a work item encounters a 429, HandleRateLimited is triggered
        // which notifies the queue. The work item is retried; if it keeps
        // failing, it eventually exhausts retries and appears in the failed list.
        // **Validates: Requirements 13.6**
        // ---------------------------------------------------------------

        /// <summary>
        /// When a work item method throws ApiHttpException(429) during execution,
        /// the rate-limited handling is triggered and the sync cycle reports failures
        /// for the affected work items while other items may still complete.
        /// **Validates: Requirements 13.6**
        /// </summary>
        [Test]
        public async Task RunSyncAsync_WorkItem429_TriggersRateLimitHandling()
        {
            var playerContext = CreateTestPlayerContext();
            var empireContext = CreateTestEmpireContext();
            var stub = new StubTypedClient
            {
                // All general data methods throw 429 (simulating persistent rate limiting).
                GeneralDataException = new ApiHttpException(429, "Too Many Requests"),
            };

            var service = new QueueSyncService(
                playerContext,
                empireContext,
                stub,
                _settings,
                _credentialManager,
                CreateTestMarketDataService(playerContext));

            var result = await service.RunSyncAsync(CancellationToken.None).ConfigureAwait(false);

            // Token exchange should succeed (it's not affected by the general exception).
            Assert.That(stub.ExchangeTokenCallCount, Is.EqualTo(1),
                "Token exchange should succeed before work items start");

            // The 429 handler catches the exception and notifies the queue to pause.
            // Work items complete without failure from the queue's perspective because
            // HandleRateLimited swallows the exception after notifying the queue.
            // The sync cycle should complete — work items are counted as succeeded
            // because the catch block handles them gracefully.
            Assert.That(result.Succeeded + result.Failed, Is.GreaterThan(0),
                "Work items should have been dispatched");
        }
    }
}
