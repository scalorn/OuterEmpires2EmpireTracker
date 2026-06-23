// <copyright file="HttpRequestPropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Common.Client.FactionServer;

namespace OE2EmpireTracker.Tests.Client.FactionServer
{
    /// <summary>
    /// Property-based tests for HTTP request equivalence.
    /// **Validates: Requirements 8.2**
    /// </summary>
    [TestFixture]
    public class HttpRequestPropertyTests
    {
        /// <summary>
        /// Creates a FactionServerTypedClient with a capturing handler injected via reflection.
        /// </summary>
        private static FactionServerTypedClient CreateClientWithHandler(HttpMessageHandler handler)
        {
            var token = new SecureString();
            token.AppendChar('t');
            token.MakeReadOnly();

            var client = new FactionServerTypedClient("http://fake-server", token);
            var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30),
            };
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "t");

            FieldInfo httpClientField = typeof(FactionServerTypedClient).GetField(
                "_httpClient",
                BindingFlags.NonPublic | BindingFlags.Instance);
            httpClientField.SetValue(client, httpClient);

            return client;
        }

        // -----------------------------------------------------------------------
        // Property 7: HTTP Request Equivalence
        // **Validates: Requirements 8.2**
        // Per-entity GET methods produce GET requests to correct URL paths
        // Write methods produce correct HTTP method and Content-Type
        // Admin/sync endpoints produce correct paths
        // -----------------------------------------------------------------------

        /// <summary>
        /// Per-entity GET methods produce GET requests to /api/v1/characters/{uuid}/{entityType}.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property PerEntityGetMethods_ProduceCorrectGetRequests()
        {
            var entityMethods = new[]
            {
                new { Method = "GetColoniesAsync", Segment = "colonies" },
                new { Method = "GetBlueprintsAsync", Segment = "blueprints" },
                new { Method = "GetSurveysAsync", Segment = "surveys" },
                new { Method = "GetPlayerProfilesAsync", Segment = "playerProfiles" },
                new { Method = "GetDeliveryRoutesAsync", Segment = "deliveryRoutes" },
                new { Method = "GetDeliveryPlansAsync", Segment = "deliveryPlans" },
                new { Method = "GetShipsAsync", Segment = "ships" },
                new { Method = "GetShipTemplatesAsync", Segment = "shipTemplates" },
                new { Method = "GetMarketListingsAsync", Segment = "marketListings" },
                new { Method = "GetMarketTransactionsAsync", Segment = "marketTransactions" },
                new { Method = "GetPricingPlansAsync", Segment = "pricingPlans" },
                new { Method = "GetStockPlansAsync", Segment = "stockPlans" },
                new { Method = "GetStockProfilesAsync", Segment = "stockProfiles" },
                new { Method = "GetBuildPlansAsync", Segment = "buildPlans" },
                new { Method = "GetSupplyChainsAsync", Segment = "supplyChains" },
                new { Method = "GetAsteroidsAsync", Segment = "asteroids" },
                new { Method = "GetStationsAsync", Segment = "stations" },
                new { Method = "GetFactionContactsAsync", Segment = "factions" },
                new { Method = "GetExternalCharactersAsync", Segment = "externalCharacters" },
            };

            var gen = from uuid in Arb.Generate<Guid>().Select(g => g.ToString())
                      from idx in Gen.Choose(0, entityMethods.Length - 1)
                      select new { Uuid = uuid, EntityInfo = entityMethods[idx] };

            return Prop.ForAll(Arb.From(gen), data =>
            {
                HttpRequestMessage captured = null;
                var handler = new CapturingHandler(req =>
                {
                    captured = req;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("[]"),
                    };
                });

                using (var client = CreateClientWithHandler(handler))
                {
                    var method = typeof(FactionServerTypedClient).GetMethod(data.EntityInfo.Method);
                    var task = (Task)method.Invoke(client, new object[] { data.Uuid, CancellationToken.None });
                    task.GetAwaiter().GetResult();

                    string expectedPath = $"/api/v1/characters/{data.Uuid}/{data.EntityInfo.Segment}";
                    bool correctMethod = captured.Method == HttpMethod.Get;
                    bool correctPath = captured.RequestUri.AbsolutePath == expectedPath;

                    return correctMethod
                        .Label($"Expected GET but was {captured.Method}")
                        .And(correctPath)
                        .Label($"Expected path {expectedPath} but was {captured.RequestUri.AbsolutePath}");
                }
            });
        }

        /// <summary>
        /// Write methods (POST/PUT) produce correct HTTP method and application/json Content-Type.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property WriteMethods_ProduceCorrectMethodAndContentType()
        {
            var gen = Arb.Generate<Guid>().Select(g => g.ToString());

            return Prop.ForAll(Arb.From(gen), uuid =>
            {
                // Test CreateCharacterAsync (POST)
                HttpRequestMessage capturedPost = null;
                var postHandler = new CapturingHandler(req =>
                {
                    capturedPost = req;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{}"),
                    };
                });

                using (var client = CreateClientWithHandler(postHandler))
                {
                    client.CreateCharacterAsync("TestName", uuid, CancellationToken.None)
                        .GetAwaiter().GetResult();
                }

                bool postCorrectMethod = capturedPost.Method == HttpMethod.Post;
                bool postCorrectPath = capturedPost.RequestUri.AbsolutePath == "/api/v1/characters";
                string postContentType = capturedPost.Content?.Headers?.ContentType?.MediaType ?? string.Empty;
                bool postCorrectContentType = postContentType == "application/json";

                // Test UploadBaselineAsync (PUT)
                HttpRequestMessage capturedPut = null;
                var putHandler = new CapturingHandler(req =>
                {
                    capturedPut = req;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{}"),
                    };
                });

                using (var client = CreateClientWithHandler(putHandler))
                {
                    client.UploadBaselineAsync(new OE2EmpireTracker.Services.BaselineRoot(), CancellationToken.None)
                        .GetAwaiter().GetResult();
                }

                bool putCorrectMethod = capturedPut.Method == HttpMethod.Put;
                bool putCorrectPath = capturedPut.RequestUri.AbsolutePath == "/api/v1/global/baseline";
                string putContentType = capturedPut.Content?.Headers?.ContentType?.MediaType ?? string.Empty;
                bool putCorrectContentType = putContentType == "application/json";

                // Test BulkImportAsync (PUT with uuid)
                HttpRequestMessage capturedImport = null;
                var importHandler = new CapturingHandler(req =>
                {
                    capturedImport = req;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"imported\":{},\"total\":0}"),
                    };
                });

                using (var client = CreateClientWithHandler(importHandler))
                {
                    client.BulkImportAsync(uuid, new OE2EmpireTracker.Services.PlayerRoot(), CancellationToken.None)
                        .GetAwaiter().GetResult();
                }

                bool importCorrectMethod = capturedImport.Method == HttpMethod.Put;
                string expectedImportPath = $"/api/v1/characters/{uuid}/import";
                bool importCorrectPath = capturedImport.RequestUri.AbsolutePath == expectedImportPath;
                string importContentType = capturedImport.Content?.Headers?.ContentType?.MediaType ?? string.Empty;
                bool importCorrectContentType = importContentType == "application/json";

                // Test PutSharingRulesAsync (PUT with uuid)
                HttpRequestMessage capturedSharing = null;
                var sharingHandler = new CapturingHandler(req =>
                {
                    capturedSharing = req;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{}"),
                    };
                });

                using (var client = CreateClientWithHandler(sharingHandler))
                {
                    client.PutSharingRulesAsync(uuid, new SharingRuleDto[0], CancellationToken.None)
                        .GetAwaiter().GetResult();
                }

                bool sharingCorrectMethod = capturedSharing.Method == HttpMethod.Put;
                string expectedSharingPath = $"/api/v1/characters/{uuid}/sharing";
                bool sharingCorrectPath = capturedSharing.RequestUri.AbsolutePath == expectedSharingPath;
                string sharingContentType = capturedSharing.Content?.Headers?.ContentType?.MediaType ?? string.Empty;
                bool sharingCorrectContentType = sharingContentType == "application/json";

                return postCorrectMethod.Label("POST: CreateCharacter should use POST")
                    .And(postCorrectPath).Label("POST: CreateCharacter path should be /api/v1/characters")
                    .And(postCorrectContentType).Label($"POST: CreateCharacter Content-Type should be application/json but was {postContentType}")
                    .And(putCorrectMethod).Label("PUT: UploadBaseline should use PUT")
                    .And(putCorrectPath).Label("PUT: UploadBaseline path should be /api/v1/global/baseline")
                    .And(putCorrectContentType).Label($"PUT: UploadBaseline Content-Type should be application/json but was {putContentType}")
                    .And(importCorrectMethod).Label("PUT: BulkImport should use PUT")
                    .And(importCorrectPath).Label($"PUT: BulkImport path should be {expectedImportPath}")
                    .And(importCorrectContentType).Label($"PUT: BulkImport Content-Type should be application/json but was {importContentType}")
                    .And(sharingCorrectMethod).Label("PUT: PutSharingRules should use PUT")
                    .And(sharingCorrectPath).Label($"PUT: PutSharingRules path should be {expectedSharingPath}")
                    .And(sharingCorrectContentType).Label($"PUT: PutSharingRules Content-Type should be application/json but was {sharingContentType}");
            });
        }

        /// <summary>
        /// Admin and sync endpoints produce correct URL paths and HTTP methods.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property AdminAndSyncEndpoints_ProduceCorrectPaths()
        {
            var gen = Arb.Generate<Guid>().Select(g => g.ToString());

            return Prop.ForAll(Arb.From(gen), uuid =>
            {
                // Test CheckHealthAsync (GET /health)
                HttpRequestMessage capturedHealth = null;
                var healthHandler = new CapturingHandler(req =>
                {
                    capturedHealth = req;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("OK"),
                    };
                });

                using (var client = CreateClientWithHandler(healthHandler))
                {
                    client.CheckHealthAsync(CancellationToken.None).GetAwaiter().GetResult();
                }

                bool healthGet = capturedHealth.Method == HttpMethod.Get;
                bool healthPath = capturedHealth.RequestUri.AbsolutePath == "/health";

                // Test GetFactionsAsync (GET /api/v1/factions)
                HttpRequestMessage capturedFactions = null;
                var factionsHandler = new CapturingHandler(req =>
                {
                    capturedFactions = req;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("[]"),
                    };
                });

                using (var client = CreateClientWithHandler(factionsHandler))
                {
                    client.GetFactionsAsync(CancellationToken.None).GetAwaiter().GetResult();
                }

                bool factionsGet = capturedFactions.Method == HttpMethod.Get;
                bool factionsPath = capturedFactions.RequestUri.AbsolutePath == "/api/v1/factions";

                // Test GetCharactersAsync (GET /api/v1/characters)
                HttpRequestMessage capturedChars = null;
                var charsHandler = new CapturingHandler(req =>
                {
                    capturedChars = req;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("[]"),
                    };
                });

                using (var client = CreateClientWithHandler(charsHandler))
                {
                    client.GetCharactersAsync(CancellationToken.None).GetAwaiter().GetResult();
                }

                bool charsGet = capturedChars.Method == HttpMethod.Get;
                bool charsPath = capturedChars.RequestUri.AbsolutePath == "/api/v1/characters";

                // Test GetSyncSnapshotAsync (GET /api/v1/sync/snapshot)
                HttpRequestMessage capturedSync = null;
                var syncHandler = new CapturingHandler(req =>
                {
                    capturedSync = req;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"factions\":[],\"characters\":[],\"serverTimestamp\":\"2024-01-01T00:00:00Z\"}"),
                    };
                });

                using (var client = CreateClientWithHandler(syncHandler))
                {
                    client.GetSyncSnapshotAsync(CancellationToken.None).GetAwaiter().GetResult();
                }

                bool syncGet = capturedSync.Method == HttpMethod.Get;
                bool syncPath = capturedSync.RequestUri.AbsolutePath == "/api/v1/sync/snapshot";

                // Test ExportCharacterDataAsync (GET /api/v1/characters/{uuid}/export)
                HttpRequestMessage capturedExport = null;
                var exportHandler = new CapturingHandler(req =>
                {
                    capturedExport = req;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{}"),
                    };
                });

                using (var client = CreateClientWithHandler(exportHandler))
                {
                    client.ExportCharacterDataAsync(uuid, CancellationToken.None).GetAwaiter().GetResult();
                }

                bool exportGet = capturedExport.Method == HttpMethod.Get;
                string expectedExportPath = $"/api/v1/characters/{uuid}/export";
                bool exportPath = capturedExport.RequestUri.AbsolutePath == expectedExportPath;

                // Test GetSharingRulesAsync (GET /api/v1/characters/{uuid}/sharing)
                HttpRequestMessage capturedGetSharing = null;
                var getSharingHandler = new CapturingHandler(req =>
                {
                    capturedGetSharing = req;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("[]"),
                    };
                });

                using (var client = CreateClientWithHandler(getSharingHandler))
                {
                    client.GetSharingRulesAsync(uuid, CancellationToken.None).GetAwaiter().GetResult();
                }

                bool getSharingGet = capturedGetSharing.Method == HttpMethod.Get;
                string expectedSharingPath = $"/api/v1/characters/{uuid}/sharing";
                bool getSharingPath = capturedGetSharing.RequestUri.AbsolutePath == expectedSharingPath;

                return healthGet.Label("Health should use GET")
                    .And(healthPath).Label($"Health path should be /health but was {capturedHealth.RequestUri.AbsolutePath}")
                    .And(factionsGet).Label("Factions should use GET")
                    .And(factionsPath).Label($"Factions path should be /api/v1/factions but was {capturedFactions.RequestUri.AbsolutePath}")
                    .And(charsGet).Label("Characters should use GET")
                    .And(charsPath).Label($"Characters path should be /api/v1/characters but was {capturedChars.RequestUri.AbsolutePath}")
                    .And(syncGet).Label("SyncSnapshot should use GET")
                    .And(syncPath).Label($"SyncSnapshot path should be /api/v1/sync/snapshot but was {capturedSync.RequestUri.AbsolutePath}")
                    .And(exportGet).Label("ExportCharacterData should use GET")
                    .And(exportPath).Label($"ExportCharacterData path should be {expectedExportPath} but was {capturedExport.RequestUri.AbsolutePath}")
                    .And(getSharingGet).Label("GetSharingRules should use GET")
                    .And(getSharingPath).Label($"GetSharingRules path should be {expectedSharingPath} but was {capturedGetSharing.RequestUri.AbsolutePath}");
            });
        }

        /// <summary>
        /// HTTP message handler that captures the outgoing request and returns a controlled response.
        /// </summary>
        private class CapturingHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

            /// <summary>
            /// Initializes a new instance of the <see cref="CapturingHandler"/> class.
            /// </summary>
            /// <param name="handler">Function that receives the request and produces a response.</param>
            public CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            {
                _handler = handler;
            }

            /// <inheritdoc/>
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(_handler(request));
            }
        }
    }
}
