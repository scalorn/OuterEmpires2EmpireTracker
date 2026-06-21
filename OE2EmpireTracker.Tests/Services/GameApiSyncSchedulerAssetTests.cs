// <copyright file="GameApiSyncSchedulerAssetTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Common.Client.Generated;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Integration tests for GameApiSyncScheduler.SyncAssetsAsync error handling.
    /// Uses HttpListener to serve controlled responses to the real GameApiClient.
    /// Validates: Req 1 Criteria 3-6, Req 2 Criteria 3-6, Req 8 Criteria 6;
    /// Correctness Property 7 (Error Isolation).
    /// </summary>
    [TestFixture]
    public class GameApiSyncSchedulerAssetTests
    {
        private const string PlayerUUID = "test-player-uuid-asset";
        private const string AccessToken = "test-access-token";
        private const string AppId = "test-app-id";
        private const string ClientId = "test-client-id";

        private static readonly JsonSerializerSettings CamelCaseSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };

        private readonly List<string> _tempFiles = new List<string>();

        private HttpListener _listener;
        private string _baseUrl;
        private GameApiClient _client;
        private GameApiCredentialManager _credManager;
        private GameApiConnectionMonitor _monitor;
        private TestableAssetScheduler _scheduler;

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
        /// Sets up HttpListener, client, credential manager, monitor, and scheduler.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _baseUrl = "http://localhost:" + GetAvailablePort() + "/";
            _listener = new HttpListener();
            _listener.Prefixes.Add(_baseUrl);
            _listener.Start();

            _client = new GameApiClient(_baseUrl.TrimEnd('/'));

            string tempFile = Path.Combine(
                Path.GetTempPath(),
                "oe2-test-asset-sched-" + Guid.NewGuid().ToString("N") + ".dat");
            _tempFiles.Add(tempFile);
            _credManager = new GameApiCredentialManager(tempFile);
            _credManager.StoreKey(PlayerUUID, "test-secret");

            _monitor = new GameApiConnectionMonitor(
                _client, _credManager, PlayerUUID, AppId, ClientId);

            _scheduler = new TestableAssetScheduler(
                _client, _credManager, _monitor, AppId, ClientId);
        }

        /// <summary>
        /// Tears down all test resources.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
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
        // 15.1 Test 401 handling: credentials invalidated, cycle aborted
        // Validates: Req 1 Criteria 3, Req 2 Criteria 3
        // -------------------------------------------------------------------

        /// <summary>
        /// When the asset locations endpoint returns HTTP 401, the connection
        /// monitor transitions to DisconnectedInvalidKey and the cycle aborts.
        /// </summary>
        [Test]
        public async Task SyncAssets_LocationsList401_InvalidatesCredentialsAndAborts()
        {
            SetupSingleResponse(HttpStatusCode.Unauthorized, string.Empty);

            await _scheduler.CallSyncAssetsAsync(PlayerUUID);

            Assert.That(
                _monitor.CurrentState,
                Is.EqualTo(GameApiConnectionMonitor.ConnectionState.DisconnectedInvalidKey));
            Assert.That(_scheduler.WriteContextCallCount, Is.EqualTo(0));
            Assert.That(_scheduler.RaiseAssetDataChangedCallCount, Is.EqualTo(0));
        }

        /// <summary>
        /// When a detail endpoint returns HTTP 401, the connection monitor
        /// transitions to DisconnectedInvalidKey and remaining locations are skipped.
        /// </summary>
        [Test]
        public async Task SyncAssets_Detail401_InvalidatesCredentialsAndAbortsCycle()
        {
            var locationsResponse = BuildLocationsResponse(
                new LocationDef(1, AssetTypeCodes.Colony, "Colony A", 5),
                new LocationDef(2, AssetTypeCodes.Colony, "Colony B", 3));

            string locationsJson = JsonConvert.SerializeObject(locationsResponse, CamelCaseSettings);
            int requestCount = 0;

            SetupMultipleResponses(ctx =>
            {
                requestCount++;
                if (requestCount == 1)
                {
                    return (HttpStatusCode.OK, locationsJson);
                }

                // First detail request returns 401
                return (HttpStatusCode.Unauthorized, string.Empty);
            });

            await _scheduler.CallSyncAssetsAsync(PlayerUUID);

            Assert.That(
                _monitor.CurrentState,
                Is.EqualTo(GameApiConnectionMonitor.ConnectionState.DisconnectedInvalidKey));
            Assert.That(_scheduler.WriteContextCallCount, Is.EqualTo(0));
            // Only 2 requests: locations list + first detail (401)
            Assert.That(requestCount, Is.EqualTo(2));
        }

        // -------------------------------------------------------------------
        // 15.2 Test 403 handling: logged, cycle skipped (locations) or
        // location skipped (detail)
        // Validates: Req 1 Criteria 4, Req 2 Criteria 4
        // -------------------------------------------------------------------

        /// <summary>
        /// When the asset locations endpoint returns HTTP 403, the cycle is
        /// skipped gracefully without transitioning to DisconnectedInvalidKey.
        /// </summary>
        [Test]
        public async Task SyncAssets_LocationsList403_SkipsCycleGracefully()
        {
            SetupSingleResponse(HttpStatusCode.Forbidden, string.Empty);

            await _scheduler.CallSyncAssetsAsync(PlayerUUID);

            Assert.That(
                _monitor.CurrentState,
                Is.Not.EqualTo(GameApiConnectionMonitor.ConnectionState.DisconnectedInvalidKey));
            Assert.That(_scheduler.WriteContextCallCount, Is.EqualTo(0));
            Assert.That(_scheduler.RaiseAssetDataChangedCallCount, Is.EqualTo(0));
        }

        /// <summary>
        /// When a detail endpoint returns HTTP 403, that location is skipped
        /// but other locations are still processed.
        /// </summary>
        [Test]
        public async Task SyncAssets_Detail403_SkipsLocationContinuesOthers()
        {
            _scheduler.ColoniesToReturn = new List<Colony>
            {
                new Colony { ColonyId = 2, OwnerUUID = PlayerUUID, ColonyName = "Colony B" },
            };

            var locationsResponse = BuildLocationsResponse(
                new LocationDef(1, AssetTypeCodes.Colony, "Colony A", 5),
                new LocationDef(2, AssetTypeCodes.Colony, "Colony B", 3));

            string locationsJson = JsonConvert.SerializeObject(locationsResponse, CamelCaseSettings);

            var detailResponse = new GameApiServiceResponse<AssetLocationDetail>
            {
                Success = true,
                ReturnCode = 0,
                Data = new AssetLocationDetail
                {
                    Cargo = new List<AssetCargoItem>
                    {
                        new AssetCargoItem
                        {
                            Id = 100,
                            ResourceName = "Iron",
                            TypeC = AssetTypeCodes.Resource,
                            Amount = 50,
                        },
                    },
                },
            };
            string detailJson = JsonConvert.SerializeObject(detailResponse, CamelCaseSettings);

            int requestCount = 0;
            SetupMultipleResponses(ctx =>
            {
                requestCount++;
                if (requestCount == 1)
                {
                    return (HttpStatusCode.OK, locationsJson);
                }

                if (requestCount == 2)
                {
                    // First location detail returns 403
                    return (HttpStatusCode.Forbidden, string.Empty);
                }

                // Second location detail returns success
                return (HttpStatusCode.OK, detailJson);
            });

            await _scheduler.CallSyncAssetsAsync(PlayerUUID);

            // Should have processed 3 requests: list + detail(403) + detail(OK)
            Assert.That(requestCount, Is.EqualTo(3));
            Assert.That(
                _monitor.CurrentState,
                Is.Not.EqualTo(GameApiConnectionMonitor.ConnectionState.DisconnectedInvalidKey));
        }

        // -------------------------------------------------------------------
        // 15.3 Test circuit breaker open: logged, cycle aborted
        // Validates: Req 8 Criteria 6
        // -------------------------------------------------------------------

        /// <summary>
        /// When the circuit breaker opens due to repeated transient failures,
        /// the asset sync cycle is aborted gracefully.
        /// The circuit breaker opens after 3 consecutive transient failures
        /// (500-series responses). Once open, subsequent calls fail immediately.
        /// </summary>
        [Test]
        public async Task SyncAssets_CircuitBreakerOpen_AbortsCycleGracefully()
        {
            var locationsResponse = BuildLocationsResponse(
                new LocationDef(1, AssetTypeCodes.Colony, "Colony A", 5),
                new LocationDef(2, AssetTypeCodes.Colony, "Colony B", 3),
                new LocationDef(3, AssetTypeCodes.Colony, "Colony C", 2));

            string locationsJson = JsonConvert.SerializeObject(locationsResponse, CamelCaseSettings);

            SetupMultipleResponses(ctx =>
            {
                string path = ctx.Request.Url.AbsolutePath;
                if (path.Contains("/v1/assets/locations") && !path.Contains("/v1/assets/locations/"))
                {
                    // Locations list succeeds
                    return (HttpStatusCode.OK, locationsJson);
                }

                // All detail requests return 500 (transient error)
                return (HttpStatusCode.InternalServerError, string.Empty);
            });

            await _scheduler.CallSyncAssetsAsync(PlayerUUID);

            // The scheduler should complete without throwing.
            // No data should be persisted since all details failed.
            Assert.That(_scheduler.WriteContextCallCount, Is.EqualTo(0));
            Assert.That(_scheduler.RaiseAssetDataChangedCallCount, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // 15.4 Test malformed JSON: logged, location skipped, other locations
        // still processed
        // Validates: Req 2 Criteria 5, Correctness Property 7 (Error Isolation)
        // -------------------------------------------------------------------

        /// <summary>
        /// When a detail response contains malformed JSON, that location is
        /// skipped but other locations are still processed successfully.
        /// </summary>
        [Test]
        public async Task SyncAssets_MalformedJsonDetail_SkipsLocationContinuesOthers()
        {
            _scheduler.ColoniesToReturn = new List<Colony>
            {
                new Colony { ColonyId = 2, OwnerUUID = PlayerUUID, ColonyName = "Colony B" },
            };

            var locationsResponse = BuildLocationsResponse(
                new LocationDef(1, AssetTypeCodes.Colony, "Colony A", 5),
                new LocationDef(2, AssetTypeCodes.Colony, "Colony B", 3));

            string locationsJson = JsonConvert.SerializeObject(locationsResponse, CamelCaseSettings);

            var detailResponse = new GameApiServiceResponse<AssetLocationDetail>
            {
                Success = true,
                ReturnCode = 0,
                Data = new AssetLocationDetail
                {
                    Cargo = new List<AssetCargoItem>
                    {
                        new AssetCargoItem
                        {
                            Id = 200,
                            ResourceName = "Copper",
                            TypeC = AssetTypeCodes.Resource,
                            Amount = 25,
                        },
                    },
                },
            };
            string validDetailJson = JsonConvert.SerializeObject(detailResponse, CamelCaseSettings);

            int requestCount = 0;
            SetupMultipleResponses(ctx =>
            {
                requestCount++;
                if (requestCount == 1)
                {
                    return (HttpStatusCode.OK, locationsJson);
                }

                if (requestCount == 2)
                {
                    // First location detail returns malformed JSON
                    return (HttpStatusCode.OK, "{ this is not valid json!!!");
                }

                // Second location detail returns valid response
                return (HttpStatusCode.OK, validDetailJson);
            });

            await _scheduler.CallSyncAssetsAsync(PlayerUUID);

            // Should have processed all 3 requests
            Assert.That(requestCount, Is.EqualTo(3));
            // Monitor should not be in error state
            Assert.That(
                _monitor.CurrentState,
                Is.Not.EqualTo(GameApiConnectionMonitor.ConnectionState.DisconnectedInvalidKey));
        }

        /// <summary>
        /// When the locations list response contains malformed JSON, the entire
        /// cycle is aborted without processing any locations.
        /// </summary>
        [Test]
        public async Task SyncAssets_MalformedJsonLocationsList_AbortsCycle()
        {
            SetupSingleResponse(HttpStatusCode.OK, "{ not valid json at all }}}");

            await _scheduler.CallSyncAssetsAsync(PlayerUUID);

            Assert.That(_scheduler.WriteContextCallCount, Is.EqualTo(0));
            Assert.That(_scheduler.RaiseAssetDataChangedCallCount, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // 15.5 Test zero assetCount locations are skipped (no detail API call)
        // Validates: Req 1 Criteria 5, Req 2 Criteria 6
        // -------------------------------------------------------------------

        /// <summary>
        /// Locations with assetCount == 0 are filtered out and no detail API
        /// call is made for them.
        /// </summary>
        [Test]
        public async Task SyncAssets_ZeroAssetCountLocations_SkippedNoDetailCall()
        {
            _scheduler.ColoniesToReturn = new List<Colony>
            {
                new Colony { ColonyId = 2, OwnerUUID = PlayerUUID, ColonyName = "Colony B" },
            };

            var locationsResponse = BuildLocationsResponse(
                new LocationDef(1, AssetTypeCodes.Colony, "Empty Colony", 0),
                new LocationDef(2, AssetTypeCodes.Colony, "Colony B", 3),
                new LocationDef(3, AssetTypeCodes.Station, "Empty Station", 0));

            string locationsJson = JsonConvert.SerializeObject(locationsResponse, CamelCaseSettings);

            var detailResponse = new GameApiServiceResponse<AssetLocationDetail>
            {
                Success = true,
                ReturnCode = 0,
                Data = new AssetLocationDetail
                {
                    Cargo = new List<AssetCargoItem>
                    {
                        new AssetCargoItem
                        {
                            Id = 300,
                            ResourceName = "Gold",
                            TypeC = AssetTypeCodes.Resource,
                            Amount = 10,
                        },
                    },
                },
            };
            string detailJson = JsonConvert.SerializeObject(detailResponse, CamelCaseSettings);

            int requestCount = 0;
            SetupMultipleResponses(ctx =>
            {
                requestCount++;
                if (requestCount == 1)
                {
                    return (HttpStatusCode.OK, locationsJson);
                }

                // Only one detail request should be made (for Colony B)
                return (HttpStatusCode.OK, detailJson);
            });

            await _scheduler.CallSyncAssetsAsync(PlayerUUID);

            // Only 2 requests: locations list + 1 detail (Colony B with assetCount=3)
            // The two zero-count locations should NOT trigger detail requests
            Assert.That(requestCount, Is.EqualTo(2));
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

        private static GameApiServiceResponse<AssetLocations> BuildLocationsResponse(
            params LocationDef[] locations)
        {
            var entries = new List<AssetLocation>();
            foreach (var loc in locations)
            {
                entries.Add(new AssetLocation
                {
                    LocationId = loc.Id,
                    LocationType = loc.Type,
                    LocationName = loc.Name,
                    SystemName = "Test System",
                    SystemId = 1,
                    AssetCount = loc.AssetCount,
                });
            }

            return new GameApiServiceResponse<AssetLocations>
            {
                Success = true,
                ReturnCode = 0,
                Data = new AssetLocations { Locations = entries },
            };
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
                }
                catch (HttpListenerException)
                {
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
                }
                catch (HttpListenerException)
                {
                }
            });
        }

        // -------------------------------------------------------------------
        // Helper types
        // -------------------------------------------------------------------

        /// <summary>
        /// Simple tuple for building location responses in tests.
        /// </summary>
        private struct LocationDef
        {
            public int Id;
            public string Type;
            public string Name;
            public int AssetCount;

            public LocationDef(int id, string type, string name, int assetCount)
            {
                Id = id;
                Type = type;
                Name = name;
                AssetCount = assetCount;
            }
        }

        // -------------------------------------------------------------------
        // Testable subclass
        // -------------------------------------------------------------------

        /// <summary>
        /// Testable subclass of GameApiSyncScheduler that overrides virtual
        /// methods to track calls and provide controlled data for asset tests.
        /// </summary>
        private sealed class TestableAssetScheduler : GameApiSyncScheduler
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TestableAssetScheduler"/> class.
            /// </summary>
            /// <param name="client">The game API client.</param>
            /// <param name="credentialManager">The credential manager.</param>
            /// <param name="connectionMonitor">The connection monitor.</param>
            /// <param name="appId">The application identifier.</param>
            /// <param name="clientId">The client identifier.</param>
            public TestableAssetScheduler(
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
            /// Gets or sets the station list to return from GetPlayerStations.
            /// </summary>
            public List<Station> StationsToReturn { get; set; }

            /// <summary>
            /// Gets or sets the ship list to return from GetPlayerShips.
            /// </summary>
            public List<Ship> ShipsToReturn { get; set; }

            /// <summary>
            /// Gets the number of times WriteContext was called.
            /// </summary>
            public int WriteContextCallCount { get; private set; }

            /// <summary>
            /// Gets the number of times RaiseAssetDataChanged was called.
            /// </summary>
            public int RaiseAssetDataChangedCallCount { get; private set; }

            /// <summary>
            /// Exposes SyncAssetsAsync for direct testing.
            /// </summary>
            /// <param name="playerUUID">The player UUID.</param>
            /// <param name="accessToken">The access token.</param>
            /// <returns>A task representing the async operation.</returns>
            public Task CallSyncAssetsAsync(string playerUUID)
            {
                return SyncAssetsAsync(playerUUID);
            }

            /// <inheritdoc/>
            internal override List<Colony> GetPlayerColonies(string playerUUID)
            {
                return ColoniesToReturn;
            }

            /// <inheritdoc/>
            internal override List<Station> GetPlayerStations(string playerUUID)
            {
                return StationsToReturn;
            }

            /// <inheritdoc/>
            internal override List<Ship> GetPlayerShips(string playerUUID)
            {
                return ShipsToReturn;
            }

            /// <inheritdoc/>
            internal override void WriteContext()
            {
                WriteContextCallCount++;
            }

            /// <inheritdoc/>
            internal override void RaiseAssetDataChanged()
            {
                RaiseAssetDataChangedCallCount++;
            }
        }
    }
}
