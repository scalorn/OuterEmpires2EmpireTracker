// <copyright file="GameApiSyncSchedulerColonyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Integration tests for GameApiSyncScheduler.SyncColoniesAsync.
    /// Uses HttpListener to serve controlled responses to the real GameApiClient.
    /// Validates: Requirements 5.1-5.7, 11.1-11.3, 12.1, 12.3, 13.1, 13.4, 14.1-14.4.
    /// </summary>
    [TestFixture]
    public class GameApiSyncSchedulerColonyTests
    {
        private const string PlayerUUID = "test-player-uuid-001";
        private const string AccessToken = "test-access-token";
        private const string AppId = "test-app-id";
        private const string ClientId = "test-client-id";

        private static readonly DateTime FrozenTime = new DateTime(2025, 1, 20, 10, 0, 0, DateTimeKind.Utc);

        private readonly List<string> _tempFiles = new List<string>();

        private HttpListener _listener;
        private string _baseUrl;
        private GameApiClient _client;
        private GameApiCredentialManager _credManager;
        private GameApiConnectionMonitor _monitor;
        private TestableScheduler _scheduler;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            GameApiCredentialManager.RegisterProtectionFunctions(
                OE2EmpireTracker.Client.CredentialStore.Protect,
                OE2EmpireTracker.Client.CredentialStore.Unprotect);
        }

        [SetUp]
        public void SetUp()
        {
            SystemClock.UtcNowFunc = () => FrozenTime;

            // Find an available port for the HttpListener
            _baseUrl = "http://localhost:" + GetAvailablePort() + "/";
            _listener = new HttpListener();
            _listener.Prefixes.Add(_baseUrl);
            _listener.Start();

            _client = new GameApiClient(_baseUrl.TrimEnd('/'));

            string tempFile = Path.Combine(
                Path.GetTempPath(),
                "oe2-test-colony-sched-" + Guid.NewGuid().ToString("N") + ".dat");
            _tempFiles.Add(tempFile);
            _credManager = new GameApiCredentialManager(tempFile);
            _credManager.StoreKey(PlayerUUID, "test-secret");

            _monitor = new GameApiConnectionMonitor(
                _client, _credManager, PlayerUUID, AppId, ClientId);

            _scheduler = new TestableScheduler(_client, _credManager, _monitor, AppId, ClientId);
        }

        [TearDown]
        public void TearDown()
        {
            SystemClock.Reset();
            _scheduler?.Dispose();
            _monitor?.Dispose();
            _client?.Dispose();

            if (_listener != null && _listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
            }

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
        // Test 1: Successful colony sync — creates colony, calls WriteContext
        // and RaiseColonyDataChanged.
        // Validates: Requirements 5.1, 5.3, 5.7, 11.1, 12.1
        // -------------------------------------------------------------------

        [Test]
        public async Task SyncColonies_SuccessfulSync_CreatesColonyAndPersists()
        {
            var localColonies = new List<Colony>();
            _scheduler.ColoniesToReturn = localColonies;

            var colonyListResponse = new GameApiServiceResponse<GameApiColonyListResponse>
            {
                Success = true,
                ReturnCode = 0,
                Data = new GameApiColonyListResponse
                {
                    Colonies = new List<GameApiColonyListItem>
                    {
                        new GameApiColonyListItem
                        {
                            ColonyId = 42,
                            ColonyName = "Test Colony",
                            SystemObjectName = "Planet Alpha",
                            SystemName = "Sol System",
                            SystemId = 7,
                            ColonySize = 3,
                            RemoteAccess = 0,
                        },
                    },
                },
            };

            string json = JsonConvert.SerializeObject(colonyListResponse);
            SetupSingleResponse(HttpStatusCode.OK, json);

            await _scheduler.CallSyncColoniesAsync(PlayerUUID, AccessToken);

            Assert.That(localColonies.Count, Is.EqualTo(1));
            Assert.That(localColonies[0].ColonyName, Is.EqualTo("Test Colony"));
            Assert.That(localColonies[0].PlanetName, Is.EqualTo("Planet Alpha"));
            Assert.That(_scheduler.WriteContextCallCount, Is.EqualTo(1));
            Assert.That(_scheduler.RaiseColonyDataChangedCallCount, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Test 2: HTTP 401 handling — transitions monitor to
        // DisconnectedInvalidKey.
        // Validates: Requirements 5.6
        // -------------------------------------------------------------------

        [Test]
        public async Task SyncColonies_Http401_TransitionsToDisconnectedInvalidKey()
        {
            _scheduler.ColoniesToReturn = new List<Colony>();

            SetupSingleResponse(HttpStatusCode.Unauthorized, string.Empty);

            await _scheduler.CallSyncColoniesAsync(PlayerUUID, AccessToken);

            Assert.That(
                _monitor.CurrentState,
                Is.EqualTo(GameApiConnectionMonitor.ConnectionState.DisconnectedInvalidKey));
            Assert.That(_scheduler.WriteContextCallCount, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Test 3: HTTP 403 scope skip — logs and returns without error.
        // Validates: Requirements 5.5, 14.1
        // -------------------------------------------------------------------

        [Test]
        public async Task SyncColonies_Http403_SkipsColonySyncGracefully()
        {
            _scheduler.ColoniesToReturn = new List<Colony>();

            SetupSingleResponse(HttpStatusCode.Forbidden, string.Empty);

            await _scheduler.CallSyncColoniesAsync(PlayerUUID, AccessToken);

            Assert.That(_scheduler.WriteContextCallCount, Is.EqualTo(0));
            Assert.That(_scheduler.RaiseColonyDataChangedCallCount, Is.EqualTo(0));
            // Monitor should NOT transition to DisconnectedInvalidKey for 403
            Assert.That(
                _monitor.CurrentState,
                Is.Not.EqualTo(GameApiConnectionMonitor.ConnectionState.DisconnectedInvalidKey));
        }

        // -------------------------------------------------------------------
        // Test 4: Malformed JSON — logs error, does not mutate local data.
        // Validates: Requirements 13.1, 13.4
        // -------------------------------------------------------------------

        [Test]
        public async Task SyncColonies_MalformedJson_LogsErrorNoMutation()
        {
            var existingColony = new Colony
            {
                UUID = "existing-uuid",
                OwnerUUID = PlayerUUID,
                PlanetName = "Existing Planet",
                SystemName = "Existing System",
                ColonyName = "Existing Colony",
            };
            var localColonies = new List<Colony> { existingColony };
            _scheduler.ColoniesToReturn = localColonies;

            string malformedJson = "{ this is not valid json at all!!!";
            SetupSingleResponse(HttpStatusCode.OK, malformedJson);

            await _scheduler.CallSyncColoniesAsync(PlayerUUID, AccessToken);

            // Local data should be unchanged
            Assert.That(localColonies.Count, Is.EqualTo(1));
            Assert.That(localColonies[0].ColonyName, Is.EqualTo("Existing Colony"));
            Assert.That(_scheduler.WriteContextCallCount, Is.EqualTo(0));
            Assert.That(_scheduler.RaiseColonyDataChangedCallCount, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Test 5: Scope caching — first 403 on buildings prevents subsequent
        // building requests for remaining colonies.
        // Validates: Requirements 14.2, 14.4
        // -------------------------------------------------------------------

        [Test]
        public async Task SyncColonies_BuildingsScope403_CachesAndSkipsRemaining()
        {
            var localColonies = new List<Colony>();
            _scheduler.ColoniesToReturn = localColonies;

            var colonyListResponse = new GameApiServiceResponse<GameApiColonyListResponse>
            {
                Success = true,
                ReturnCode = 0,
                Data = new GameApiColonyListResponse
                {
                    Colonies = new List<GameApiColonyListItem>
                    {
                        new GameApiColonyListItem
                        {
                            ColonyId = 1,
                            ColonyName = "Colony One",
                            SystemObjectName = "Planet One",
                            SystemName = "System One",
                            SystemId = 1,
                            RemoteAccess = 1,
                        },
                        new GameApiColonyListItem
                        {
                            ColonyId = 2,
                            ColonyName = "Colony Two",
                            SystemObjectName = "Planet Two",
                            SystemName = "System Two",
                            SystemId = 2,
                            RemoteAccess = 1,
                        },
                    },
                },
            };

            string colonyListJson = JsonConvert.SerializeObject(colonyListResponse);

            int requestCount = 0;
            SetupMultipleResponses(ctx =>
            {
                requestCount++;
                if (requestCount == 1)
                {
                    // Colony list
                    return (HttpStatusCode.OK, colonyListJson);
                }

                // All subsequent requests return 403 (buildings and warehouse)
                return (HttpStatusCode.Forbidden, string.Empty);
            });

            await _scheduler.CallSyncColoniesAsync(PlayerUUID, AccessToken);

            // Colonies should still be created from the list data
            Assert.That(localColonies.Count, Is.EqualTo(2));
            Assert.That(_scheduler.WriteContextCallCount, Is.EqualTo(1));

            // Should have made: 1 colony list + 1 buildings (403) + 1 warehouse (403) + 1 workers (403)
            // Second colony's buildings, warehouse, and workers should be skipped due to caching
            // Total: 1 + 1 + 1 + 1 = 4 requests (not 7)
            Assert.That(requestCount, Is.EqualTo(4));
        }

        // -------------------------------------------------------------------
        // Test 6: No changes — does not call WriteContext or raise event.
        // Validates: Requirements 11.2, 12.3
        // -------------------------------------------------------------------

        [Test]
        public async Task SyncColonies_NoChanges_DoesNotPersistOrNotify()
        {
            // Set up a local colony that already matches the API data exactly
            var existingColony = new Colony
            {
                UUID = "existing-uuid-001",
                OwnerUUID = PlayerUUID,
                PlanetName = "Planet Alpha",
                SystemName = "Sol System",
                ColonyName = "Test Colony",
                ColonyId = 42,
                SystemId = 7,
                ColonySize = 3,
            };
            var localColonies = new List<Colony> { existingColony };
            _scheduler.ColoniesToReturn = localColonies;

            var colonyListResponse = new GameApiServiceResponse<GameApiColonyListResponse>
            {
                Success = true,
                ReturnCode = 0,
                Data = new GameApiColonyListResponse
                {
                    Colonies = new List<GameApiColonyListItem>
                    {
                        new GameApiColonyListItem
                        {
                            ColonyId = 42,
                            ColonyName = "Test Colony",
                            SystemObjectName = "Planet Alpha",
                            SystemName = "Sol System",
                            SystemId = 7,
                            ColonySize = 3,
                            RemoteAccess = 0,
                        },
                    },
                },
            };

            string json = JsonConvert.SerializeObject(colonyListResponse);
            SetupSingleResponse(HttpStatusCode.OK, json);

            await _scheduler.CallSyncColoniesAsync(PlayerUUID, AccessToken);

            // No changes means no persistence or notification
            Assert.That(_scheduler.WriteContextCallCount, Is.EqualTo(0));
            Assert.That(_scheduler.RaiseColonyDataChangedCallCount, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Test 7: Null local colony list — logs warning and returns.
        // Validates: Requirements 5.3
        // -------------------------------------------------------------------

        [Test]
        public async Task SyncColonies_NullLocalColonies_LogsAndReturns()
        {
            _scheduler.ColoniesToReturn = null;

            var colonyListResponse = new GameApiServiceResponse<GameApiColonyListResponse>
            {
                Success = true,
                ReturnCode = 0,
                Data = new GameApiColonyListResponse
                {
                    Colonies = new List<GameApiColonyListItem>
                    {
                        new GameApiColonyListItem
                        {
                            ColonyId = 42,
                            ColonyName = "Test Colony",
                            SystemObjectName = "Planet Alpha",
                            SystemName = "Sol System",
                        },
                    },
                },
            };

            string json = JsonConvert.SerializeObject(colonyListResponse);
            SetupSingleResponse(HttpStatusCode.OK, json);

            await _scheduler.CallSyncColoniesAsync(PlayerUUID, AccessToken);

            Assert.That(_scheduler.WriteContextCallCount, Is.EqualTo(0));
            Assert.That(_scheduler.RaiseColonyDataChangedCallCount, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private static int GetAvailablePort()
        {
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private void SetupSingleResponse(HttpStatusCode statusCode, string body)
        {
            Task.Run(async () =>
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    context.Response.StatusCode = (int)statusCode;
                    if (!string.IsNullOrEmpty(body))
                    {
                        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(body);
                        context.Response.ContentLength64 = buffer.Length;
                        context.Response.ContentType = "application/json";
                        await context.Response.OutputStream.WriteAsync(
                            buffer, 0, buffer.Length);
                    }

                    context.Response.Close();
                }
                catch (ObjectDisposedException)
                {
                    // Listener was stopped
                }
                catch (HttpListenerException)
                {
                    // Listener was stopped
                }
            });
        }

        private void SetupMultipleResponses(
            Func<HttpListenerContext, (HttpStatusCode StatusCode, string Body)> handler)
        {
            Task.Run(async () =>
            {
                try
                {
                    while (_listener.IsListening)
                    {
                        var context = await _listener.GetContextAsync();
                        var result = handler(context);
                        context.Response.StatusCode = (int)result.StatusCode;
                        if (!string.IsNullOrEmpty(result.Body))
                        {
                            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(result.Body);
                            context.Response.ContentLength64 = buffer.Length;
                            context.Response.ContentType = "application/json";
                            await context.Response.OutputStream.WriteAsync(
                                buffer, 0, buffer.Length);
                        }

                        context.Response.Close();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Listener was stopped
                }
                catch (HttpListenerException)
                {
                    // Listener was stopped
                }
            });
        }

        // -------------------------------------------------------------------
        // Testable subclass
        // -------------------------------------------------------------------

        /// <summary>
        /// Testable subclass of GameApiSyncScheduler that overrides virtual
        /// methods to track calls and provide controlled colony data.
        /// </summary>
        private sealed class TestableScheduler : GameApiSyncScheduler
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TestableScheduler"/> class.
            /// </summary>
            /// <param name="client">The game API client.</param>
            /// <param name="credentialManager">The credential manager.</param>
            /// <param name="connectionMonitor">The connection monitor.</param>
            /// <param name="appId">The application identifier.</param>
            /// <param name="clientId">The client identifier.</param>
            public TestableScheduler(
                GameApiClient client,
                GameApiCredentialManager credentialManager,
                GameApiConnectionMonitor connectionMonitor,
                string appId,
                string clientId)
                : base(client, credentialManager, connectionMonitor, appId, clientId)
            {
            }

            /// <summary>
            /// Gets or sets the colony list to return from GetPlayerColonies.
            /// </summary>
            public List<Colony> ColoniesToReturn { get; set; }

            /// <summary>
            /// Gets the number of times WriteContext was called.
            /// </summary>
            public int WriteContextCallCount { get; private set; }

            /// <summary>
            /// Gets the number of times RaiseColonyDataChanged was called.
            /// </summary>
            public int RaiseColonyDataChangedCallCount { get; private set; }

            /// <summary>
            /// Exposes SyncColoniesAsync for direct testing.
            /// </summary>
            /// <param name="playerUUID">The player UUID.</param>
            /// <param name="accessToken">The access token.</param>
            /// <returns>A task representing the async operation.</returns>
            public Task CallSyncColoniesAsync(string playerUUID, string accessToken)
            {
                return SyncColoniesAsync(playerUUID, accessToken);
            }

            /// <inheritdoc/>
            internal override List<Colony> GetPlayerColonies(string playerUUID)
            {
                return ColoniesToReturn;
            }

            /// <inheritdoc/>
            internal override void WriteContext()
            {
                WriteContextCallCount++;
            }

            /// <inheritdoc/>
            internal override void RaiseColonyDataChanged()
            {
                RaiseColonyDataChangedCallCount++;
            }
        }
    }
}
