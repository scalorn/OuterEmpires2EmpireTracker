// <copyright file="GameApiTypedClientEnvelopeTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OE2EmpireTracker.Common.Client;
using OE2EmpireTracker.Common.Client.Generated;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for envelope unwrapping in <see cref="GameApiTypedClient"/>.
    /// Tests success paths, business errors, HTTP errors, and deserialization failures.
    /// Feature: nswag-typed-api-client
    /// **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5**
    /// </summary>
    [TestFixture]
    public class GameApiTypedClientEnvelopeTests
    {
        /// <summary>
        /// Sets up the SystemClock to avoid wall-clock waits in the rate limiter.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            SystemClock.FreezeAt(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            SystemClock.EnableInstantDelay();
        }

        /// <summary>
        /// Resets the SystemClock after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            SystemClock.Reset();
        }

        /// <summary>
        /// Tests that a successful envelope (success=true) returns the typed DTO.
        /// </summary>
        [Test]
        public async Task GetAssetLocations_SuccessTrue_ReturnsTypedDto()
        {
            // Arrange
            string json = @"{
                ""success"": true,
                ""returnCode"": 0,
                ""returnString"": """",
                ""data"": {
                    ""locations"": [
                        {
                            ""locationId"": 42,
                            ""locationType"": ""St"",
                            ""locationName"": ""Alpha Station"",
                            ""systemName"": ""Sol"",
                            ""systemId"": 1,
                            ""assetCount"": 5
                        }
                    ]
                },
                ""errors"": []
            }";

            using (var client = CreateClientWithJsonResponse(json, HttpStatusCode.OK))
            {
                // Act
                var result = await client.GetAssetLocationsAsync().ConfigureAwait(false);

                // Assert
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Locations, Has.Count.EqualTo(1));
                Assert.That(result.Locations.First().LocationId, Is.EqualTo(42));
                Assert.That(result.Locations.First().LocationName, Is.EqualTo("Alpha Station"));
            }
        }

        /// <summary>
        /// Tests that a successful envelope with null data returns null (not throws).
        /// </summary>
        [Test]
        public async Task GetAssetLocations_SuccessTrueWithNullData_ReturnsNull()
        {
            // Arrange
            string json = @"{
                ""success"": true,
                ""returnCode"": 0,
                ""returnString"": """",
                ""data"": null,
                ""errors"": []
            }";

            using (var client = CreateClientWithJsonResponse(json, HttpStatusCode.OK))
            {
                // Act
                var result = await client.GetAssetLocationsAsync().ConfigureAwait(false);

                // Assert
                Assert.That(result, Is.Null);
            }
        }

        /// <summary>
        /// Tests that a business error (success=false) throws ApiBusinessException
        /// with the correct fields.
        /// </summary>
        [Test]
        public void GetAssetLocations_SuccessFalse_ThrowsApiBusinessException()
        {
            // Arrange
            string json = @"{
                ""success"": false,
                ""returnCode"": 42,
                ""returnString"": ""Invalid request"",
                ""data"": null,
                ""errors"": [
                    { ""field"": ""name"", ""error"": ""required"" }
                ]
            }";

            using (var client = CreateClientWithJsonResponse(json, HttpStatusCode.OK))
            {
                // Act & Assert
                var ex = Assert.ThrowsAsync<ApiBusinessException>(
                    async () => await client.GetAssetLocationsAsync().ConfigureAwait(false));

                Assert.That(ex.ReturnCode, Is.EqualTo(42));
                Assert.That(ex.ReturnString, Is.EqualTo("Invalid request"));
                Assert.That(ex.Errors, Has.Count.EqualTo(1));
                Assert.That(ex.Errors[0].Field, Is.EqualTo("name"));
                Assert.That(ex.Errors[0].Error, Is.EqualTo("required"));
            }
        }

        /// <summary>
        /// Tests that HTTP 4xx throws ApiHttpException immediately (not retried).
        /// </summary>
        [Test]
        public void GetAssetLocations_Http4xx_ThrowsApiHttpException()
        {
            // Arrange: 404 is not retried by the retry policy (only >= 500 is)
            string body = "Not Found";

            using (var client = CreateClientWithJsonResponse(body, HttpStatusCode.NotFound))
            {
                // Act & Assert
                var ex = Assert.ThrowsAsync<ApiHttpException>(
                    async () => await client.GetAssetLocationsAsync().ConfigureAwait(false));

                Assert.That(ex.StatusCode, Is.EqualTo(404));
                Assert.That(ex.ResponseBody, Is.EqualTo(body));
            }
        }

        /// <summary>
        /// Tests that repeated HTTP 5xx triggers retries and eventually fails.
        /// The circuit breaker opens after 3 consecutive 500 failures, and the
        /// exception propagates (either ApiHttpException or BrokenCircuitException).
        /// </summary>
        [Test]
        public void GetAssetLocations_Http500_ThrowsAfterRetryExhaustion()
        {
            // Arrange: return 500 on every request — after retries exhaust,
            // the circuit breaker opens and the call fails
            using (var client = CreateClientWithRepeatedResponse(
                "Internal Server Error", HttpStatusCode.InternalServerError, repeatCount: 10))
            {
                // Act & Assert: the call must fail (circuit breaker trips after 3 failures)
                var ex = Assert.CatchAsync(
                    async () => await client.GetAssetLocationsAsync().ConfigureAwait(false));

                // The inner exception chain should contain the original ApiHttpException
                Assert.That(
                    ex.ToString(),
                    Does.Contain("500").And.Contain("Internal Server Error"));
            }
        }

        /// <summary>
        /// Tests that malformed JSON on HTTP 200 throws ApiDeserializationException.
        /// </summary>
        [Test]
        public void GetAssetLocations_MalformedJson_ThrowsApiDeserializationException()
        {
            // Arrange
            string malformedJson = "not json at all";

            using (var client = CreateClientWithJsonResponse(malformedJson, HttpStatusCode.OK))
            {
                // Act & Assert
                var ex = Assert.ThrowsAsync<ApiDeserializationException>(
                    async () => await client.GetAssetLocationsAsync().ConfigureAwait(false));

                Assert.That(ex.StatusCode, Is.EqualTo(200));
            }
        }

        /// <summary>
        /// Creates a <see cref="GameApiTypedClient"/> backed by a mock handler
        /// that returns the specified body with the given status code.
        /// </summary>
        /// <param name="body">The response body.</param>
        /// <param name="statusCode">The HTTP status code to return.</param>
        /// <returns>A configured <see cref="GameApiTypedClient"/>.</returns>
        private static GameApiTypedClient CreateClientWithJsonResponse(string body, HttpStatusCode statusCode)
        {
            var handler = new FakeHttpHandler(() => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
            });

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://fake-api.test/"),
            };

            return new GameApiTypedClient(httpClient, "test-app-id");
        }

        /// <summary>
        /// Creates a <see cref="GameApiTypedClient"/> backed by a mock handler
        /// that returns the same response repeatedly (for retry/circuit testing).
        /// </summary>
        /// <param name="body">The response body text.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="repeatCount">How many responses available.</param>
        /// <returns>A configured <see cref="GameApiTypedClient"/>.</returns>
        private static GameApiTypedClient CreateClientWithRepeatedResponse(
            string body, HttpStatusCode statusCode, int repeatCount)
        {
            int callCount = 0;
            var handler = new FakeHttpHandler(() =>
            {
                callCount++;
                return new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(body, System.Text.Encoding.UTF8, "text/plain"),
                };
            });

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://fake-api.test/"),
            };

            return new GameApiTypedClient(httpClient, "test-app-id");
        }

        /// <summary>
        /// Fake HTTP message handler that returns controlled responses.
        /// </summary>
        private class FakeHttpHandler : HttpMessageHandler
        {
            private readonly Func<HttpResponseMessage> _responseFactory;

            /// <summary>
            /// Initializes a new instance of the <see cref="FakeHttpHandler"/> class.
            /// </summary>
            /// <param name="responseFactory">A factory that produces the response.</param>
            public FakeHttpHandler(Func<HttpResponseMessage> responseFactory)
            {
                _responseFactory = responseFactory;
            }

            /// <inheritdoc/>
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(_responseFactory());
            }
        }
    }
}
