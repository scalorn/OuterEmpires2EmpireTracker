// <copyright file="DesktopSyncMigrationTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Client
{
    /// <summary>
    /// Tests verifying the desktop SyncManager migration to bulk import.
    /// Satisfies: Req 4, Criteria 1-6.
    /// </summary>
    [TestFixture]
    public class DesktopSyncMigrationTests
    {
        private const string TestCharacterUUID = "char-uuid-001";
        private const string TestServerUrl = "https://localhost:9999";

        private FakeHttpHandler _handler;
        private RemoteFactionClient _client;
        private OfflineQueue _offlineQueue;
        private SyncManager _syncManager;

        [SetUp]
        public void SetUp()
        {
            SystemClock.UtcNowFunc = () => new DateTime(2025, 7, 15, 12, 0, 0, DateTimeKind.Utc);

            _handler = new FakeHttpHandler();

            // Create client with a dummy token
            var token = new SecureString();
            foreach (char c in "test-token")
            {
                token.AppendChar(c);
            }

            _client = new RemoteFactionClient(TestServerUrl, token, string.Empty);

            // Replace the internal HttpClient with one using our fake handler
            InjectFakeHttpClient(_client, _handler);

            // Use a temp file path for the offline queue so it doesn't touch real data
            string tempPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "oe2-test-" + Guid.NewGuid().ToString("N") + ".json");
            _offlineQueue = new OfflineQueue(tempPath);

            _syncManager = new SyncManager(_client, _offlineQueue);
            _syncManager.Mode = OperatingMode.ServerOnly;
        }

        [TearDown]
        public void TearDown()
        {
            SystemClock.Reset();
            _client?.Dispose();
        }

        /// <summary>
        /// Test 1: WriteToServerAsync calls BulkImportAsync (not old upload methods).
        /// Verifies the SyncManager uses the bulk import endpoint.
        /// </summary>
        [Test]
        public async Task WriteToServerAsync_CallsBulkImportAsync()
        {
            // Arrange: set client as connected
            SetClientConnected(true);
            _handler.ResponseToReturn = CreateResponse(HttpStatusCode.OK, "{}");

            string json = "{\"Colonies\":[]}";

            // Act
            await _syncManager.WriteToServerAsync(TestCharacterUUID, "colonies", json);

            // Assert: the handler captured a PUT to /import
            Assert.That(_handler.LastRequest, Is.Not.Null, "Expected an HTTP request");
            Assert.That(_handler.LastRequest.Method, Is.EqualTo(HttpMethod.Put));
            StringAssert.Contains("/characters/" + TestCharacterUUID + "/import", _handler.LastRequestUri);
            StringAssert.DoesNotContain("/data", _handler.LastRequestUri);
        }

        /// <summary>
        /// Test 2: BulkImportAsync calls the correct URL (/characters/{uuid}/import).
        /// </summary>
        [Test]
        public async Task BulkImportAsync_CallsCorrectUrl()
        {
            // Arrange
            _handler.ResponseToReturn = CreateResponse(HttpStatusCode.OK, "{}");

            // Act
            await _client.BulkImportAsync(TestCharacterUUID, "{\"Colonies\":[]}");

            // Assert
            string expectedPath = "/api/v1/characters/" + TestCharacterUUID + "/import";
            Assert.That(_handler.LastRequestUri, Is.Not.Null);
            StringAssert.EndsWith(expectedPath, _handler.LastRequestUri);
            Assert.That(_handler.LastRequest.Method, Is.EqualTo(HttpMethod.Put));
        }

        /// <summary>
        /// Test 3: When BulkImportAsync returns 400, the change is queued with ValidationFailed marker.
        /// </summary>
        [Test]
        public async Task WriteToServerAsync_ValidationFailure_QueuesForReview()
        {
            // Arrange
            SetClientConnected(true);
            string errorBody = "{\"errors\":[{\"entityType\":\"Colony\",\"error\":\"Name required\"}]}";
            _handler.ResponseToReturn = CreateResponse(HttpStatusCode.BadRequest, errorBody);

            string json = "{\"Colonies\":[{\"UUID\":\"c1\"}]}";

            // Act
            await _syncManager.WriteToServerAsync(TestCharacterUUID, "colonies", json);

            // Assert: change queued with ValidationFailed marker
            var queued = _offlineQueue.GetAll();
            Assert.That(queued.Count, Is.EqualTo(1));
            StringAssert.Contains("ValidationFailed", queued[0].DataType);
            Assert.That(queued[0].CharacterUUID, Is.EqualTo(TestCharacterUUID));
        }

        /// <summary>
        /// Test 4: When BulkImportAsync returns 400, SyncValidationFailed event is raised.
        /// </summary>
        [Test]
        public async Task WriteToServerAsync_ValidationFailure_RaisesEvent()
        {
            // Arrange
            SetClientConnected(true);
            string errorBody = "{\"errors\":[{\"entityType\":\"Colony\",\"error\":\"Name required\"}]}";
            _handler.ResponseToReturn = CreateResponse(HttpStatusCode.BadRequest, errorBody);

            SyncValidationFailedEventArgs raisedArgs = null;
            _syncManager.SyncValidationFailed += (sender, args) => raisedArgs = args;

            string json = "{\"Colonies\":[{\"UUID\":\"c1\"}]}";

            // Act
            await _syncManager.WriteToServerAsync(TestCharacterUUID, "colonies", json);

            // Assert: event was raised with correct data
            Assert.That(raisedArgs, Is.Not.Null, "SyncValidationFailed event should be raised");
            Assert.That(raisedArgs.CharacterUUID, Is.EqualTo(TestCharacterUUID));
            Assert.That(raisedArgs.ValidationErrors, Is.Not.Null);
            Assert.That(raisedArgs.ValidationErrors.Count, Is.GreaterThan(0));
        }

        /// <summary>
        /// Test 5: FlushOfflineQueueAsync calls BulkImportAsync for each queued change.
        /// </summary>
        [Test]
        public async Task FlushOfflineQueueAsync_UsesBulkImport()
        {
            // Arrange: queue two changes
            SetClientConnected(true);
            _syncManager.QueueOfflineChange("char-1", "colonies", "{\"Colonies\":[]}");
            _syncManager.QueueOfflineChange("char-2", "blueprints", "{\"Blueprints\":[]}");

            _handler.ResponseToReturn = CreateResponse(HttpStatusCode.OK, "{}");

            // Act
            await _syncManager.FlushOfflineQueueAsync();

            // Assert: requests went to /import endpoint
            Assert.That(_handler.AllRequests.Count, Is.EqualTo(2));
            foreach (var uri in _handler.AllRequestUris)
            {
                StringAssert.Contains("/import", uri);
                StringAssert.DoesNotContain("/data", uri);
            }

            // Queue should be empty after successful flush
            Assert.That(_offlineQueue.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// Test 6: When BulkImportAsync returns 403, connection status set to Unauthorized.
        /// </summary>
        [Test]
        public async Task WriteToServerAsync_Forbidden_SetsUnauthorized()
        {
            // Arrange
            SetClientConnected(true);
            _handler.ResponseToReturn = CreateResponse(HttpStatusCode.Forbidden, "{}");

            ConnectionStatusChangedEventArgs statusArgs = null;
            _client.ConnectionStatusChanged += (sender, args) => statusArgs = args;

            string json = "{\"Colonies\":[]}";

            // Act
            await _syncManager.WriteToServerAsync(TestCharacterUUID, "colonies", json);

            // Assert: connection status changed to disconnected/unauthorized
            Assert.That(statusArgs, Is.Not.Null, "ConnectionStatusChanged should fire");
            Assert.That(statusArgs.IsConnected, Is.False);
            StringAssert.Contains("Unauthorized", statusArgs.Message);
        }

        /// <summary>
        /// Uses reflection to replace the internal HttpClient with one using our fake handler.
        /// </summary>
        private static void InjectFakeHttpClient(RemoteFactionClient client, FakeHttpHandler handler)
        {
            var httpClient = new HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var field = typeof(RemoteFactionClient).GetField(
                "_httpClient",
                BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(client, httpClient);
        }

        /// <summary>
        /// Sets the client as connected using the public SetConnectionStatus method.
        /// </summary>
        private void SetClientConnected(bool connected)
        {
            _client.SetConnectionStatus(connected, connected ? "Connected" : "Disconnected");
        }

        /// <summary>
        /// Creates an HttpResponseMessage with the given status and body.
        /// </summary>
        private static HttpResponseMessage CreateResponse(HttpStatusCode status, string body)
        {
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }

        /// <summary>
        /// Fake HTTP message handler that captures requests and returns a configured response.
        /// </summary>
        private class FakeHttpHandler : HttpMessageHandler
        {
            /// <summary>
            /// Gets or sets the response to return for all requests.
            /// </summary>
            public HttpResponseMessage ResponseToReturn { get; set; }

            /// <summary>
            /// Gets the last request that was sent.
            /// </summary>
            public HttpRequestMessage LastRequest { get; private set; }

            /// <summary>
            /// Gets the last request URI as a string.
            /// </summary>
            public string LastRequestUri { get; private set; }

            /// <summary>
            /// Gets all requests that were sent.
            /// </summary>
            public List<HttpRequestMessage> AllRequests { get; } = new List<HttpRequestMessage>();

            /// <summary>
            /// Gets all request URIs as strings.
            /// </summary>
            public List<string> AllRequestUris { get; } = new List<string>();

            /// <inheritdoc/>
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                LastRequest = request;
                LastRequestUri = request.RequestUri?.ToString();
                AllRequests.Add(request);
                AllRequestUris.Add(LastRequestUri);

                var response = ResponseToReturn ?? new HttpResponseMessage(HttpStatusCode.OK);
                return Task.FromResult(response);
            }
        }
    }
}
