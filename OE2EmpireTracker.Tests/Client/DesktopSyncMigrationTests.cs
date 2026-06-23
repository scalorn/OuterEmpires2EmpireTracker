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
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Common.Client.FactionServer;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Client
{
    /// <summary>
    /// Tests verifying the desktop SyncManager migration to typed client and push client.
    /// Satisfies: Req 8, Criteria 1-4.
    /// </summary>
    [TestFixture]
    public class DesktopSyncMigrationTests
    {
        private const string TestCharacterUUID = "char-uuid-001";
        private const string TestServerUrl = "https://localhost:9999";

        private FakeHttpHandler _handler;
        private FactionServerTypedClient _typedClient;
        private FactionPushClient _pushClient;
        private OfflineQueue _offlineQueue;
        private SyncManager _syncManager;

        [SetUp]
        public void SetUp()
        {
            SystemClock.UtcNowFunc = () => new DateTime(2025, 7, 15, 12, 0, 0, DateTimeKind.Utc);

            _handler = new FakeHttpHandler();

            // Create typed client with a dummy token
            var typedToken = new SecureString();
            foreach (char c in "test-token")
            {
                typedToken.AppendChar(c);
            }

            _typedClient = new FactionServerTypedClient(TestServerUrl, typedToken, string.Empty);
            InjectFakeHttpClientIntoTypedClient(_typedClient, _handler);

            // Create push client with a separate token copy
            var pushToken = new SecureString();
            foreach (char c in "test-token")
            {
                pushToken.AppendChar(c);
            }

            _pushClient = new FactionPushClient(TestServerUrl, pushToken, string.Empty, _typedClient);

            // Use a temp file path for the offline queue
            string tempPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "oe2-test-" + Guid.NewGuid().ToString("N") + ".json");
            _offlineQueue = new OfflineQueue(tempPath);

            _syncManager = new SyncManager(_typedClient, _pushClient, _offlineQueue);
            _syncManager.Mode = OperatingMode.ServerOnly;
        }

        [TearDown]
        public void TearDown()
        {
            SystemClock.Reset();
            _typedClient?.Dispose();
            _pushClient?.Dispose();
        }

        /// <summary>
        /// Test 1: WriteToServerAsync queues change when offline (pushClient not connected).
        /// </summary>
        [Test]
        public async Task WriteToServerAsync_WhenOffline_QueuesChange()
        {
            // Arrange: pushClient IsConnected is false by default
            var playerRoot = new PlayerRoot();

            // Act
            await _syncManager.WriteToServerAsync(TestCharacterUUID, "colonies", playerRoot);

            // Assert: change queued because IsOnline is false
            var queued = _offlineQueue.GetAll();
            Assert.That(queued.Count, Is.EqualTo(1));
            Assert.That(queued[0].CharacterUUID, Is.EqualTo(TestCharacterUUID));
        }

        /// <summary>
        /// Test 2: WriteToServerAsync succeeds when online and server returns 200.
        /// Verifies no exception raised and nothing queued offline.
        /// </summary>
        [Test]
        public async Task WriteToServerAsync_WhenOnline_BulkImportSucceeds()
        {
            // Arrange: set pushClient to connected
            SetPushClientOnline(_pushClient);
            var successBody = JsonConvert.SerializeObject(new { imported = new { colonies = 1 }, total = 1 });
            _handler.ResponseToReturn = CreateResponse(HttpStatusCode.OK, successBody);

            var playerRoot = new PlayerRoot();

            // Act
            await _syncManager.WriteToServerAsync(TestCharacterUUID, "colonies", playerRoot);

            // Assert: no offline queuing — successful write-through
            var queued = _offlineQueue.GetAll();
            Assert.That(queued.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// Test 3: WriteToServerAsync raises SyncValidationFailed on HTTP 400 with validation errors.
        /// </summary>
        [Test]
        public async Task WriteToServerAsync_ValidationFailure_RaisesSyncValidationFailed()
        {
            // Arrange: set pushClient to connected
            SetPushClientOnline(_pushClient);
            var errorBody = JsonConvert.SerializeObject(new
            {
                errors = new[]
                {
                    new { entityType = "Colony", entityUUID = "col-001", field = "Name", error = "Name is required" },
                },
            });
            _handler.ResponseToReturn = CreateResponse(HttpStatusCode.BadRequest, errorBody);

            SyncValidationFailedEventArgs raisedArgs = null;
            _syncManager.SyncValidationFailed += (sender, args) => raisedArgs = args;

            var playerRoot = new PlayerRoot();

            // Act
            await _syncManager.WriteToServerAsync(TestCharacterUUID, "colonies", playerRoot);

            // Assert: SyncValidationFailed event was raised with error details
            Assert.That(raisedArgs, Is.Not.Null);
            Assert.That(raisedArgs.CharacterUUID, Is.EqualTo(TestCharacterUUID));
            Assert.That(raisedArgs.ValidationErrors, Is.Not.Null);
            Assert.That(raisedArgs.ValidationErrors.Count, Is.EqualTo(1));
            StringAssert.Contains("Colony", raisedArgs.ValidationErrors[0]);
            StringAssert.Contains("Name is required", raisedArgs.ValidationErrors[0]);
        }

        /// <summary>
        /// Test 4: WriteToServerAsync sets unauthorized on HTTP 403.
        /// </summary>
        [Test]
        public async Task WriteToServerAsync_AuthorizationFailure_SetsUnauthorized()
        {
            // Arrange: set pushClient to connected
            SetPushClientOnline(_pushClient);
            _handler.ResponseToReturn = CreateResponse(HttpStatusCode.Forbidden, "Access denied");

            var playerRoot = new PlayerRoot();

            // Act
            await _syncManager.WriteToServerAsync(TestCharacterUUID, "colonies", playerRoot);

            // Assert: pushClient should have IsConnected set to false
            Assert.That(_pushClient.IsConnected, Is.False);
        }

        /// <summary>
        /// Test 5: WriteToServerAsync queues offline on connection failure.
        /// When the HTTP handler throws, the change goes to the offline queue.
        /// </summary>
        [Test]
        public async Task WriteToServerAsync_ConnectionFailure_QueuesOffline()
        {
            // Arrange: set pushClient to connected
            SetPushClientOnline(_pushClient);
            _handler.ExceptionToThrow = new HttpRequestException("Connection refused");

            var playerRoot = new PlayerRoot();

            // Act
            await _syncManager.WriteToServerAsync(TestCharacterUUID, "colonies", playerRoot);

            // Assert: change queued offline due to connection failure
            var queued = _offlineQueue.GetAll();
            Assert.That(queued.Count, Is.EqualTo(1));
            Assert.That(queued[0].CharacterUUID, Is.EqualTo(TestCharacterUUID));
        }

        /// <summary>
        /// Test 6: FlushOfflineQueueAsync removes items from queue on success.
        /// </summary>
        [Test]
        public async Task FlushOfflineQueueAsync_SuccessfulFlush_RemovesFromQueue()
        {
            // Arrange: queue a change while offline
            var playerRoot = new PlayerRoot();
            _syncManager.QueueOfflineChange(TestCharacterUUID, "colonies", playerRoot);
            Assert.That(_offlineQueue.Count, Is.EqualTo(1));

            // Now set online and configure success response
            SetPushClientOnline(_pushClient);
            var successBody = JsonConvert.SerializeObject(new { imported = new { colonies = 1 }, total = 1 });
            _handler.ResponseToReturn = CreateResponse(HttpStatusCode.OK, successBody);

            // Act
            await _syncManager.FlushOfflineQueueAsync();

            // Assert: queue is empty after successful flush
            Assert.That(_offlineQueue.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// Test 7: QueueOfflineChange stores both JSON and typed payload.
        /// </summary>
        [Test]
        public void QueueOfflineChange_StoresTypedPayload()
        {
            // Arrange
            var playerRoot = new PlayerRoot();

            // Act
            _syncManager.QueueOfflineChange(TestCharacterUUID, "colonies", playerRoot);

            // Assert
            var queued = _offlineQueue.GetAll();
            Assert.That(queued.Count, Is.EqualTo(1));
            Assert.That(queued[0].CharacterUUID, Is.EqualTo(TestCharacterUUID));
            Assert.That(queued[0].TypedPayload, Is.Not.Null);
            Assert.That(queued[0].Json, Is.Not.Null.And.Not.Empty);
        }

        /// <summary>
        /// Test 8: SyncValidationFailed event can be subscribed.
        /// </summary>
        [Test]
        public void SyncValidationFailed_EventCanBeSubscribed()
        {
            // Arrange
            SyncValidationFailedEventArgs raisedArgs = null;
            _syncManager.SyncValidationFailed += (sender, args) => raisedArgs = args;

            // Assert: just verifying the event subscription compiles and works
            Assert.That(raisedArgs, Is.Null);
        }

        /// <summary>
        /// Sets the push client's IsConnected property to true via reflection.
        /// FactionPushClient.IsConnected has a public getter but is set internally
        /// via SetConnectionStatus, which we call directly.
        /// </summary>
        private static void SetPushClientOnline(FactionPushClient pushClient)
        {
            pushClient.SetConnectionStatus(true, "Test: online");
        }

        /// <summary>
        /// Uses reflection to replace the internal HttpClient in the typed client.
        /// </summary>
        private static void InjectFakeHttpClientIntoTypedClient(FactionServerTypedClient client, FakeHttpHandler handler)
        {
            var httpClient = new HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var field = typeof(FactionServerTypedClient).GetField(
                "_httpClient",
                BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(client, httpClient);
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
        /// Supports throwing exceptions to simulate connection failures.
        /// </summary>
        private class FakeHttpHandler : HttpMessageHandler
        {
            /// <summary>
            /// Gets or sets the response to return for all requests.
            /// </summary>
            public HttpResponseMessage ResponseToReturn { get; set; }

            /// <summary>
            /// Gets or sets an exception to throw instead of returning a response.
            /// </summary>
            public Exception ExceptionToThrow { get; set; }

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

                if (ExceptionToThrow != null)
                {
                    throw ExceptionToThrow;
                }

                var response = ResponseToReturn ?? new HttpResponseMessage(HttpStatusCode.OK);
                return Task.FromResult(response);
            }
        }
    }
}
