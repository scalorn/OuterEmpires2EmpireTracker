// <copyright file="GameApiSyncSchedulerBankingTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FsCheck;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for GameApiSyncScheduler banking fault isolation.
    /// **Validates: Requirements 2.1, 2.2**
    /// </summary>
    [TestFixture]
    public class GameApiSyncSchedulerBankingTests
    {
        private const string PlayerUUID = "test-player-uuid-banking";
        private const string AppId = "test-app-id";
        private const string ClientId = "test-client-id";

        private readonly List<string> _tempFiles = new List<string>();

        /// <summary>
        /// Registers DPAPI protection functions once for all tests.
        /// </summary>
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            GameApiCredentialManager.RegisterProtectionFunctions(
                CredentialStore.Protect,
                CredentialStore.Unprotect);
        }

        /// <summary>
        /// Cleans up temp files after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            foreach (string path in _tempFiles)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            _tempFiles.Clear();
        }

        // -------------------------------------------------------------------
        // Property 1: Balance Persistence Round-Trip
        // **Validates: Requirements 1.4**
        // -------------------------------------------------------------------

        /// <summary>
        /// Property 1: Balance Persistence Round-Trip.
        /// For any non-null decimal value returned by ImportBalanceAsync,
        /// after SyncBankingAsync completes, SetBankingBalance SHALL have been
        /// called with that value, WriteContext SHALL have been called exactly
        /// once, and BankingDataChanged SHALL have been raised.
        /// **Validates: Requirements 1.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property BalancePersistenceRoundTrip()
        {
            return Prop.ForAll(Arb.From(BalanceDecimalGen()), balance =>
            {
                HttpListener listener = null;
                GameApiClient client = null;
                GameApiConnectionMonitor monitor = null;
                BalanceRoundTripScheduler scheduler = null;

                try
                {
                    string baseUrl = "http://localhost:" + GetAvailablePort() + "/";
                    listener = new HttpListener();
                    listener.Prefixes.Add(baseUrl);
                    listener.Start();

                    client = new GameApiClient(baseUrl.TrimEnd('/'));

                    string tempFile = Path.Combine(
                        Path.GetTempPath(),
                        "oe2-test-balance-rt-" + Guid.NewGuid().ToString("N") + ".dat");
                    _tempFiles.Add(tempFile);
                    var credManager = new GameApiCredentialManager(tempFile);
                    credManager.StoreKey(PlayerUUID, "test-secret");

                    monitor = new GameApiConnectionMonitor(
                        client, credManager, PlayerUUID, AppId, ClientId);

                    scheduler = new BalanceRoundTripScheduler(
                        client, credManager, monitor, AppId, ClientId);

                    SetupBalanceRoundTripResponses(listener, balance);

                    bool result = scheduler.CallSyncCharacterAsync(PlayerUUID)
                        .GetAwaiter().GetResult();

                    bool balanceSet = scheduler.SetBankingBalanceValue == balance;
                    bool writeContextOnce = scheduler.WriteContextCallCount == 1;
                    bool dataChangedRaised = scheduler.RaiseBankingDataChangedCallCount >= 1;

                    return balanceSet
                        .Label("SetBankingBalance called with " + balance)
                        .And(writeContextOnce)
                        .Label("WriteContext called exactly once")
                        .And(dataChangedRaised)
                        .Label("RaiseBankingDataChanged raised");
                }
                finally
                {
                    scheduler?.Dispose();
                    monitor?.Dispose();
                    client?.Dispose();

                    if (listener != null && listener.IsListening)
                    {
                        listener.Stop();
                        listener.Close();
                    }
                }
            });
        }

        // -------------------------------------------------------------------
        // Property 2: Banking Exception Fault Isolation
        // For any exception thrown by ImportTransactionsAsync or
        // ImportBalanceAsync, SyncCharacterAsync SHALL return true
        // (indicating profile sync success) and all prior sync results
        // (profile merge, colony data, asset data) remain unchanged.
        // **Validates: Requirements 2.1, 2.2**
        // -------------------------------------------------------------------

        /// <summary>
        /// Property 2: Banking Exception Fault Isolation.
        /// For any exception thrown during banking sync, SyncCharacterAsync
        /// still returns true, proving fault isolation works.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property BankingExceptionDoesNotAffectSyncCharacterResult()
        {
            var gen = from msg in Gen.Elements(
                          "Network timeout",
                          "API returned 500",
                          "Connection refused",
                          "Null reference in banking",
                          "Invalid JSON response",
                          "Service unavailable")
                      from useInvalidOp in Arb.Generate<bool>()
                      select new { Message = msg, UseInvalidOp = useInvalidOp };

            return Prop.ForAll(
                Arb.From(gen),
                param =>
                {
                    HttpListener listener = null;
                    GameApiClient client = null;
                    GameApiConnectionMonitor monitor = null;
                    FaultIsolationScheduler scheduler = null;

                    try
                    {
                        string baseUrl = "http://localhost:" + GetAvailablePort() + "/";
                        listener = new HttpListener();
                        listener.Prefixes.Add(baseUrl);
                        listener.Start();

                        client = new GameApiClient(baseUrl.TrimEnd('/'));

                        string tempFile = Path.Combine(
                            Path.GetTempPath(),
                            "oe2-test-banking-" + Guid.NewGuid().ToString("N") + ".dat");
                        _tempFiles.Add(tempFile);
                        var credManager = new GameApiCredentialManager(tempFile);
                        credManager.StoreKey(PlayerUUID, "test-secret");

                        monitor = new GameApiConnectionMonitor(
                            client, credManager, PlayerUUID, AppId, ClientId);

                        Exception exToThrow = param.UseInvalidOp
                            ? (Exception)new InvalidOperationException(param.Message)
                            : new ApplicationException(param.Message);

                        scheduler = new FaultIsolationScheduler(
                            client, credManager, monitor, AppId, ClientId, exToThrow);

                        SetupTokenAndCharacterResponses(listener);

                        bool result = scheduler.SyncCharacterAsync(PlayerUUID)
                            .GetAwaiter().GetResult();

                        return result.Label(string.Format(
                            "SyncCharacterAsync returns true when banking throws {0}",
                            param.Message));
                    }
                    finally
                    {
                        scheduler?.Dispose();
                        monitor?.Dispose();
                        client?.Dispose();
                        if (listener != null && listener.IsListening)
                        {
                            listener.Stop();
                            listener.Close();
                        }
                    }
                });
        }

        private static int GetAvailablePort()
        {
            var tcpListener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            return port;
        }

        private static void SetupTokenAndCharacterResponses(HttpListener listener)
        {
            Task.Run(async () =>
            {
                try
                {
                    while (listener.IsListening)
                    {
                        var context = await listener.GetContextAsync().ConfigureAwait(false);
                        string path = context.Request.Url.AbsolutePath;
                        string body = string.Empty;
                        int statusCode = 200;

                        if (path.Contains("/v1/auth/token"))
                        {
                            var tokenResponse = new GameApiServiceResponse<GameApiTokenResponse>
                            {
                                Success = true,
                                ReturnCode = 0,
                                Data = new GameApiTokenResponse
                                {
                                    AccessToken = "test-access-token",
                                    TokenType = "Bearer",
                                    ExpiresIn = 3600,
                                    CharacterId = 1,
                                },
                            };
                            body = JsonConvert.SerializeObject(tokenResponse);
                        }
                        else if (path.Contains("/v1/character"))
                        {
                            var charResponse = new GameApiServiceResponse<object>
                            {
                                Success = true,
                                ReturnCode = 0,
                                Data = new
                                {
                                    characterId = 1,
                                    firstName = "Test",
                                    lastName = "Player",
                                    activeTimeMinutes = 0,
                                },
                            };
                            body = JsonConvert.SerializeObject(charResponse);
                        }
                        else
                        {
                            // Colony, asset, and other endpoints return 403 to prevent blocking
                            statusCode = 403;
                        }

                        context.Response.StatusCode = statusCode;
                        if (!string.IsNullOrEmpty(body))
                        {
                            byte[] buf = System.Text.Encoding.UTF8.GetBytes(body);
                            context.Response.ContentLength64 = buf.Length;
                            context.Response.ContentType = "application/json";
                            await context.Response.OutputStream.WriteAsync(
                                buf, 0, buf.Length).ConfigureAwait(false);
                        }

                        context.Response.Close();
                    }
                }
                catch (ObjectDisposedException)
                {
                }
                catch (HttpListenerException)
                {
                }
            });
        }

        // -------------------------------------------------------------------
        // Generator for Property 1
        // -------------------------------------------------------------------

        private static Gen<decimal> BalanceDecimalGen()
        {
            return from intPart in Gen.Choose(-999999999, 999999999)
                   from fracPart in Gen.Choose(0, 99)
                   select (decimal)intPart + ((decimal)fracPart / 100m);
        }

        // -------------------------------------------------------------------
        // HTTP helper for Property 1 (balance round-trip)
        // -------------------------------------------------------------------

        private static void SetupBalanceRoundTripResponses(HttpListener listener, decimal balanceValue)
        {
            string tokenJson = JsonConvert.SerializeObject(new GameApiServiceResponse<GameApiTokenResponse>
            {
                Success = true,
                ReturnCode = 0,
                Data = new GameApiTokenResponse
                {
                    AccessToken = "test-token-" + Guid.NewGuid().ToString("N"),
                    TokenType = "Bearer",
                    ExpiresIn = 3600,
                    CharacterId = 1,
                    Scopes = new List<string> { "character.read", "banking.balance.read", "banking.transactions.read" },
                },
            });

            string characterJson = JsonConvert.SerializeObject(new GameApiServiceResponse<object>
            {
                Success = true,
                ReturnCode = 0,
                Data = new
                {
                    characterId = 1,
                    firstName = "Test",
                    lastName = "Player",
                    activeTimeMinutes = 0,
                },
            });

            string transactionsJson = JsonConvert.SerializeObject(new GameApiServiceResponse<object>
            {
                Success = true,
                ReturnCode = 0,
                Data = new List<object>(),
            });

            string balanceJson = JsonConvert.SerializeObject(new GameApiServiceResponse<object>
            {
                Success = true,
                ReturnCode = 0,
                Data = new { balance = balanceValue },
            });

            Task.Run(async () =>
            {
                try
                {
                    while (listener.IsListening)
                    {
                        var context = await listener.GetContextAsync().ConfigureAwait(false);
                        string path = context.Request.Url.AbsolutePath;
                        string body;
                        int statusCode = 200;

                        if (path.Contains("/v1/auth/token"))
                        {
                            body = tokenJson;
                        }
                        else if (path.Contains("/v1/character"))
                        {
                            body = characterJson;
                        }
                        else if (path.Contains("/v1/banking/transactions"))
                        {
                            body = transactionsJson;
                        }
                        else if (path.Contains("/v1/banking/balance"))
                        {
                            body = balanceJson;
                        }
                        else
                        {
                            statusCode = 403;
                            body = string.Empty;
                        }

                        context.Response.StatusCode = statusCode;
                        if (!string.IsNullOrEmpty(body))
                        {
                            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(body);
                            context.Response.ContentLength64 = buffer.Length;
                            context.Response.ContentType = "application/json";
                            await context.Response.OutputStream.WriteAsync(
                                buffer, 0, buffer.Length).ConfigureAwait(false);
                        }

                        context.Response.Close();
                    }
                }
                catch (ObjectDisposedException)
                {
                }
                catch (HttpListenerException)
                {
                }
            });
        }

        // -------------------------------------------------------------------
        // Testable subclass for Property 1 (balance round-trip)
        // -------------------------------------------------------------------

        /// <summary>
        /// Tracks SetBankingBalance, WriteContext, and RaiseBankingDataChanged
        /// calls for verifying the balance persistence round-trip property.
        /// </summary>
        private sealed class BalanceRoundTripScheduler : GameApiSyncScheduler
        {
            public BalanceRoundTripScheduler(
                GameApiClient client,
                GameApiCredentialManager credentialManager,
                GameApiConnectionMonitor connectionMonitor,
                string appId,
                string clientId)
                : base(client, credentialManager, connectionMonitor, appId, clientId)
            {
            }

            public decimal? SetBankingBalanceValue { get; private set; }

            public int WriteContextCallCount { get; private set; }

            public int RaiseBankingDataChangedCallCount { get; private set; }

            public Task<bool> CallSyncCharacterAsync(string playerUUID)
            {
                return SyncCharacterAsync(playerUUID);
            }

            internal override PlayerProfile GetPlayerProfile(string playerUUID)
            {
                return new PlayerProfile { UUID = playerUUID };
            }

            internal override PlayerContext GetPlayerContext()
            {
                PlayerContext.Reset();
                return PlayerContext.GetInstance();
            }

            internal override void SetBankingBalance(decimal balance)
            {
                SetBankingBalanceValue = balance;
            }

            internal override void WriteContext()
            {
                WriteContextCallCount++;
            }

            internal override void RaiseBankingDataChanged()
            {
                RaiseBankingDataChangedCallCount++;
            }

            internal override void RaiseColonyDataChanged()
            {
            }

            internal override void RaiseAssetDataChanged()
            {
            }
        }

        // -------------------------------------------------------------------
        // 6.3 Call ordering: profile -> colony -> asset -> banking
        // Validates: Requirement 4.2
        // -------------------------------------------------------------------

        /// <summary>
        /// Verifies that SyncCharacterAsync calls sync phases in order:
        /// profile (token + character), then colony, then asset, then banking.
        /// Uses HTTP endpoint tracking to detect the order of API calls.
        /// </summary>
        [Test]
        public async Task SyncCharacterAsync_CallOrder_IsProfileColonyAssetBanking()
        {
            var callOrder = new List<string>();
            HttpListener listener = null;
            GameApiClient client = null;
            GameApiConnectionMonitor monitor = null;

            try
            {
                string baseUrl = "http://localhost:" + GetAvailablePort() + "/";
                listener = new HttpListener();
                listener.Prefixes.Add(baseUrl);
                listener.Start();

                client = new GameApiClient(baseUrl.TrimEnd('/'));

                string tempFile = Path.Combine(
                    Path.GetTempPath(),
                    "oe2-test-banking-order-" + Guid.NewGuid().ToString("N") + ".dat");
                _tempFiles.Add(tempFile);
                var credManager = new GameApiCredentialManager(tempFile);
                credManager.StoreKey(PlayerUUID, "test-secret");

                monitor = new GameApiConnectionMonitor(
                    client, credManager, PlayerUUID, AppId, ClientId);

                var scheduler = new OrderingScheduler(
                    client, credManager, monitor, AppId, ClientId);

                SetupOrderingResponses(listener, callOrder);

                bool result = await scheduler.CallSyncCharacterAsync(PlayerUUID);

                Assert.That(result, Is.True, "SyncCharacterAsync should succeed");
                Assert.That(callOrder.Count, Is.GreaterThanOrEqualTo(4));

                int profileIndex = callOrder.IndexOf("profile");
                int colonyIndex = callOrder.IndexOf("colony");
                int assetIndex = callOrder.IndexOf("asset");
                int bankingIndex = callOrder.IndexOf("banking");

                Assert.That(profileIndex, Is.GreaterThanOrEqualTo(0), "Profile called");
                Assert.That(colonyIndex, Is.GreaterThan(profileIndex), "Colony after profile");
                Assert.That(assetIndex, Is.GreaterThan(colonyIndex), "Asset after colony");
                Assert.That(bankingIndex, Is.GreaterThan(assetIndex), "Banking after asset");
            }
            finally
            {
                monitor?.Dispose();
                client?.Dispose();

                if (listener != null && listener.IsListening)
                {
                    listener.Stop();
                    listener.Close();
                }
            }
        }

        private static void SetupOrderingResponses(HttpListener listener, List<string> callOrder)
        {
            Task.Run(async () =>
            {
                try
                {
                    while (listener.IsListening)
                    {
                        var context = await listener.GetContextAsync().ConfigureAwait(false);
                        string path = context.Request.Url.AbsolutePath;
                        string body = string.Empty;
                        int statusCode = 200;

                        if (path.Contains("/v1/auth/token"))
                        {
                            body = JsonConvert.SerializeObject(new GameApiServiceResponse<GameApiTokenResponse>
                            {
                                Success = true,
                                ReturnCode = 0,
                                Data = new GameApiTokenResponse
                                {
                                    AccessToken = "ordering-token-" + Guid.NewGuid().ToString("N"),
                                    TokenType = "Bearer",
                                    ExpiresIn = 3600,
                                    CharacterId = 1,
                                },
                            });
                        }
                        else if (path.Contains("/v1/character"))
                        {
                            callOrder.Add("profile");
                            body = JsonConvert.SerializeObject(new GameApiServiceResponse<object>
                            {
                                Success = true,
                                ReturnCode = 0,
                                Data = new
                                {
                                    characterId = 1,
                                    firstName = "Test",
                                    lastName = "Player",
                                    activeTimeMinutes = 0,
                                },
                            });
                        }
                        else if (path.Contains("/v1/colonies"))
                        {
                            callOrder.Add("colony");
                            statusCode = 403;
                        }
                        else if (path.Contains("/v1/assets/locations"))
                        {
                            callOrder.Add("asset");
                            statusCode = 403;
                        }
                        else if (path.Contains("/v1/banking"))
                        {
                            callOrder.Add("banking");
                            statusCode = 403;
                        }
                        else
                        {
                            statusCode = 404;
                        }

                        context.Response.StatusCode = statusCode;
                        if (!string.IsNullOrEmpty(body))
                        {
                            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(body);
                            context.Response.ContentLength64 = buffer.Length;
                            context.Response.ContentType = "application/json";
                            await context.Response.OutputStream.WriteAsync(
                                buffer, 0, buffer.Length).ConfigureAwait(false);
                        }

                        context.Response.Close();
                    }
                }
                catch (ObjectDisposedException)
                {
                }
                catch (HttpListenerException)
                {
                }
            });
        }

        // -------------------------------------------------------------------
        // Testable subclass for call ordering (6.3)
        // -------------------------------------------------------------------

        /// <summary>
        /// Testable subclass that provides minimal data so all sync phases
        /// can proceed to their HTTP calls. Used for ordering verification.
        /// </summary>
        private sealed class OrderingScheduler : GameApiSyncScheduler
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OrderingScheduler"/> class.
            /// </summary>
            /// <param name="client">The game API client.</param>
            /// <param name="credentialManager">The credential manager.</param>
            /// <param name="connectionMonitor">The connection monitor.</param>
            /// <param name="appId">The application identifier.</param>
            /// <param name="clientId">The client identifier.</param>
            public OrderingScheduler(
                GameApiClient client,
                GameApiCredentialManager credentialManager,
                GameApiConnectionMonitor connectionMonitor,
                string appId,
                string clientId)
                : base(client, credentialManager, connectionMonitor, appId, clientId)
            {
            }

            /// <summary>
            /// Exposes SyncCharacterAsync for direct testing.
            /// </summary>
            /// <param name="playerUUID">The player UUID.</param>
            /// <returns>A task representing the async operation result.</returns>
            public Task<bool> CallSyncCharacterAsync(string playerUUID)
            {
                return SyncCharacterAsync(playerUUID);
            }

            /// <inheritdoc/>
            internal override PlayerProfile GetPlayerProfile(string playerUUID)
            {
                return new PlayerProfile { UUID = playerUUID };
            }

            /// <inheritdoc/>
            internal override List<Colony> GetPlayerColonies(string playerUUID)
            {
                return new List<Colony>();
            }

            /// <inheritdoc/>
            internal override List<Station> GetPlayerStations(string playerUUID)
            {
                return new List<Station>();
            }

            /// <inheritdoc/>
            internal override List<Ship> GetPlayerShips(string playerUUID)
            {
                return new List<Ship>();
            }

            /// <inheritdoc/>
            internal override void WriteContext()
            {
            }

            /// <inheritdoc/>
            internal override void RaiseColonyDataChanged()
            {
            }

            /// <inheritdoc/>
            internal override void RaiseColonyDataChanged(string colonyUUID)
            {
            }

            /// <inheritdoc/>
            internal override void RaiseAssetDataChanged()
            {
            }

            /// <inheritdoc/>
            internal override void RaiseBankingDataChanged()
            {
            }

            /// <inheritdoc/>
            internal override PlayerContext GetPlayerContext()
            {
                PlayerContext.Reset();
                return PlayerContext.GetInstance();
            }

            /// <inheritdoc/>
            internal override void SetBankingBalance(decimal balance)
            {
            }
        }

        /// <summary>
        /// Testable subclass that throws from GetPlayerContext to simulate
        /// banking failure. Validates the try/catch in SyncCharacterAsync.
        /// </summary>
        private sealed class FaultIsolationScheduler : GameApiSyncScheduler
        {
            private readonly Exception _exceptionToThrow;

            public FaultIsolationScheduler(
                GameApiClient client,
                GameApiCredentialManager credentialManager,
                GameApiConnectionMonitor connectionMonitor,
                string appId,
                string clientId,
                Exception exceptionToThrow)
                : base(client, credentialManager, connectionMonitor, appId, clientId)
            {
                _exceptionToThrow = exceptionToThrow;
            }

            /// <inheritdoc/>
            internal override PlayerContext GetPlayerContext()
            {
                throw _exceptionToThrow;
            }

            /// <inheritdoc/>
            internal override void WriteContext()
            {
            }

            /// <inheritdoc/>
            internal override void RaiseColonyDataChanged()
            {
            }

            /// <inheritdoc/>
            internal override void RaiseAssetDataChanged()
            {
            }

            /// <inheritdoc/>
            internal override void RaiseBankingDataChanged()
            {
            }
        }
    }
}
