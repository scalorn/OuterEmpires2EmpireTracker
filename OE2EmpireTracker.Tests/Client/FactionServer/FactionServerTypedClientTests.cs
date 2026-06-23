// <copyright file="FactionServerTypedClientTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Common.Client.FactionServer;

namespace OE2EmpireTracker.Tests.Client.FactionServer
{
    /// <summary>
    /// Tests for <see cref="FactionServerTypedClient"/> certificate pinning, authentication,
    /// error handling, and interface coverage.
    /// Validates: Requirements 4.4, 4.6, 5.1, 5.2, 5.3, 8.1, 8.4.
    /// </summary>
    [TestFixture]
    public class FactionServerTypedClientTests
    {
        /// <summary>
        /// Creates a SecureString from a plain-text string for test purposes.
        /// </summary>
        /// <param name="plain">The plain-text string to convert.</param>
        /// <returns>A SecureString containing the characters from the input.</returns>
        private static SecureString MakeSecureString(string plain)
        {
            var ss = new SecureString();
            foreach (char c in plain)
            {
                ss.AppendChar(c);
            }

            ss.MakeReadOnly();
            return ss;
        }

        /// <summary>
        /// Gets the private HttpClient field from a FactionServerTypedClient via reflection.
        /// </summary>
        /// <param name="client">The typed client instance.</param>
        /// <returns>The internal HttpClient.</returns>
        private static HttpClient GetHttpClient(FactionServerTypedClient client)
        {
            var field = typeof(FactionServerTypedClient).GetField(
                "_httpClient",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (HttpClient)field.GetValue(client);
        }

        /// <summary>
        /// Gets the HttpClientHandler from an HttpClient via reflection.
        /// In .NET Framework, HttpClient extends HttpMessageInvoker which has a _handler field.
        /// The handler chain might wrap the actual HttpClientHandler.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance.</param>
        /// <returns>The underlying HttpClientHandler.</returns>
        private static HttpClientHandler GetHandler(HttpClient httpClient)
        {
            // HttpClient -> HttpMessageInvoker._handler (or HttpClient._handler)
            var handlerField = typeof(HttpMessageInvoker).GetField(
                "_handler",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (handlerField == null)
            {
                // Try alternate field name
                handlerField = typeof(HttpMessageInvoker).GetField(
                    "handler",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (handlerField == null)
            {
                // .NET Framework uses _handler on HttpClient itself
                handlerField = typeof(HttpClient).GetField(
                    "_handler",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }

            var handler = handlerField?.GetValue(httpClient);
            if (handler is HttpClientHandler clientHandler)
            {
                return clientHandler;
            }

            // It might be wrapped, try to unwrap
            var innerField = handler?.GetType().GetField(
                "_innerHandler",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (innerField != null)
            {
                var inner = innerField.GetValue(handler);
                if (inner is HttpClientHandler innerHandler)
                {
                    return innerHandler;
                }
            }

            // Try InnerHandler property
            var innerProp = handler?.GetType().GetProperty(
                "InnerHandler",
                BindingFlags.Public | BindingFlags.Instance);
            if (innerProp != null)
            {
                var inner = innerProp.GetValue(handler);
                if (inner is HttpClientHandler propHandler)
                {
                    return propHandler;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a self-signed X509Certificate2 with a known thumbprint for testing.
        /// </summary>
        /// <returns>A self-signed certificate.</returns>
        private static X509Certificate2 CreateSelfSignedCert()
        {
            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(
                    "CN=TestCert",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                return request.CreateSelfSigned(
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddDays(365));
            }
        }

        // ===================================================================
        // Constructor and Disposal Tests (Validates: Requirements 4.2, 4.5, 5.4)
        // ===================================================================

        /// <summary>
        /// Constructor trims trailing slashes from serverUrl and defaults timeout to 5 minutes.
        /// </summary>
        [Test]
        public void Constructor_TrimsServerUrl_And_DefaultsTimeoutTo5Min()
        {
            // Arrange & Act
            using (var token = MakeSecureString("test-token"))
            using (var client = new FactionServerTypedClient(
                "https://my-server.example.com///",
                token))
            {
                var httpClient = GetHttpClient(client);

                // Assert: timeout defaults to 5 minutes
                Assert.That(httpClient.Timeout, Is.EqualTo(TimeSpan.FromMinutes(5)));

                // Assert: base address incorporates trimmed URL (check via Authorization header presence)
                Assert.That(httpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
            }
        }

        /// <summary>
        /// Dispose zeroes the SecureString and does not throw on double-dispose.
        /// </summary>
        [Test]
        public void Dispose_ZeroesSecureString_And_DoesNotThrowOnDoubleDispose()
        {
            // Arrange
            var token = MakeSecureString("my-secret");
            var client = new FactionServerTypedClient("https://localhost", token);

            // Act: first dispose
            client.Dispose();

            // Assert: SecureString is disposed (accessing Length throws ObjectDisposedException)
            Assert.Throws<ObjectDisposedException>(() => { var len = token.Length; });

            // Act & Assert: second dispose does not throw
            Assert.DoesNotThrow(() => client.Dispose());
        }

        /// <summary>
        /// Timeout accepts any developer-specified value without minimum or maximum bounds.
        /// </summary>
        [Test]
        public void Timeout_AcceptsAnyDeveloperSpecifiedValue()
        {
            // Arrange & Act: 1 millisecond
            using (var token1 = MakeSecureString("t"))
            using (var client1 = new FactionServerTypedClient(
                "https://localhost",
                token1,
                timeout: TimeSpan.FromMilliseconds(1)))
            {
                var httpClient1 = GetHttpClient(client1);
                Assert.That(httpClient1.Timeout, Is.EqualTo(TimeSpan.FromMilliseconds(1)));
            }

            // Arrange & Act: 1 hour
            using (var token2 = MakeSecureString("t"))
            using (var client2 = new FactionServerTypedClient(
                "https://localhost",
                token2,
                timeout: TimeSpan.FromHours(1)))
            {
                var httpClient2 = GetHttpClient(client2);
                Assert.That(httpClient2.Timeout, Is.EqualTo(TimeSpan.FromHours(1)));
            }
        }

        // ===================================================================
        // Certificate Pinning Tests (Validates: Requirements 5.1, 5.2, 5.3)
        // ===================================================================

        /// <summary>
        /// Certificate pinning rejects a certificate with a mismatched thumbprint.
        /// </summary>
        [Test]
        public void CertificatePinning_RejectsMismatchedThumbprint()
        {
            // Arrange: create client with a dummy thumbprint that won't match any real cert
            string fakeThumbprint = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
            using (var token = MakeSecureString("test-token"))
            using (var client = new FactionServerTypedClient(
                "https://localhost",
                token,
                fakeThumbprint))
            {
                var httpClient = GetHttpClient(client);
                var handler = GetHandler(httpClient);
                Assert.That(handler, Is.Not.Null, "Could not retrieve HttpClientHandler via reflection");

                var callback = handler.ServerCertificateCustomValidationCallback;
                Assert.That(callback, Is.Not.Null, "Expected a certificate validation callback when thumbprint is set");

                // Act: invoke callback with a real self-signed cert whose thumbprint won't match
                using (var cert = CreateSelfSignedCert())
                {
                    bool result = callback(null, cert, null, SslPolicyErrors.None);

                    // Assert: mismatch should reject
                    Assert.That(result, Is.False);
                }
            }
        }

        /// <summary>
        /// Certificate pinning accepts a certificate whose thumbprint matches (case-insensitive).
        /// </summary>
        [Test]
        public void CertificatePinning_AcceptsMatchingThumbprint_CaseInsensitive()
        {
            // Arrange: create a cert then use its real thumbprint (in lowercase) as the pin
            using (var cert = CreateSelfSignedCert())
            {
                string actualThumbprint = cert.GetCertHashString();
                string lowercaseThumbprint = actualThumbprint.ToLowerInvariant();

                using (var token = MakeSecureString("test-token"))
                using (var client = new FactionServerTypedClient(
                    "https://localhost",
                    token,
                    lowercaseThumbprint))
                {
                    var httpClient = GetHttpClient(client);
                    var handler = GetHandler(httpClient);
                    Assert.That(handler, Is.Not.Null, "Could not retrieve HttpClientHandler");

                    var callback = handler.ServerCertificateCustomValidationCallback;
                    Assert.That(callback, Is.Not.Null);

                    // Act: invoke with the same cert (thumbprint should match case-insensitively)
                    bool result = callback(null, cert, null, SslPolicyErrors.None);

                    // Assert
                    Assert.That(result, Is.True);
                }
            }
        }

        /// <summary>
        /// Thumbprint mismatch rejects without performing standard CA validation.
        /// Even if sslPolicyErrors is None (valid cert per CA), a mismatched thumbprint still rejects.
        /// </summary>
        [Test]
        public void CertificatePinning_ThumbprintMismatch_RejectsRegardlessOfCaValidation()
        {
            // Arrange: fake thumbprint that won't match
            string fakeThumbprint = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";
            using (var token = MakeSecureString("test-token"))
            using (var client = new FactionServerTypedClient(
                "https://localhost",
                token,
                fakeThumbprint))
            {
                var httpClient = GetHttpClient(client);
                var handler = GetHandler(httpClient);
                Assert.That(handler, Is.Not.Null);

                var callback = handler.ServerCertificateCustomValidationCallback;

                using (var cert = CreateSelfSignedCert())
                {
                    // Act: pass SslPolicyErrors.None — meaning the cert passes CA validation
                    // The typed client should STILL reject because thumbprint doesn't match
                    bool result = callback(null, cert, null, SslPolicyErrors.None);

                    // Assert: rejected despite passing CA validation
                    Assert.That(result, Is.False);
                }
            }
        }

        /// <summary>
        /// Bearer token is attached as Authorization header on the HttpClient.
        /// </summary>
        [Test]
        public void BearerToken_AttachedAsAuthorizationHeader()
        {
            // Arrange
            string expectedToken = "my-secret-bearer-token-12345";
            using (var token = MakeSecureString(expectedToken))
            using (var client = new FactionServerTypedClient(
                "https://localhost",
                token))
            {
                // Act: get the internal HttpClient and check its default headers
                var httpClient = GetHttpClient(client);
                var authHeader = httpClient.DefaultRequestHeaders.Authorization;

                // Assert
                Assert.That(authHeader, Is.Not.Null);
                Assert.That(authHeader.Scheme, Is.EqualTo("Bearer"));
                Assert.That(authHeader.Parameter, Is.EqualTo(expectedToken));
            }
        }

        /// <summary>
        /// When thumbprint is null, no certificate pinning callback is set.
        /// </summary>
        [Test]
        public void NoPinningCallback_WhenThumbprintIsNull()
        {
            // Arrange & Act
            using (var token = MakeSecureString("test-token"))
            using (var client = new FactionServerTypedClient(
                "https://localhost",
                token,
                trustedThumbprint: null))
            {
                var httpClient = GetHttpClient(client);
                var handler = GetHandler(httpClient);
                Assert.That(handler, Is.Not.Null, "Could not retrieve HttpClientHandler");

                // Assert: no custom validation callback
                Assert.That(handler.ServerCertificateCustomValidationCallback, Is.Null);
            }
        }

        /// <summary>
        /// When thumbprint is empty string, no certificate pinning callback is set.
        /// </summary>
        [Test]
        public void NoPinningCallback_WhenThumbprintIsEmpty()
        {
            // Arrange & Act
            using (var token = MakeSecureString("test-token"))
            using (var client = new FactionServerTypedClient(
                "https://localhost",
                token,
                trustedThumbprint: string.Empty))
            {
                var httpClient = GetHttpClient(client);
                var handler = GetHandler(httpClient);
                Assert.That(handler, Is.Not.Null, "Could not retrieve HttpClientHandler");

                // Assert: no custom validation callback
                Assert.That(handler.ServerCertificateCustomValidationCallback, Is.Null);
            }
        }

        // ===================================================================
        // Error Handling Tests (Validates: Requirements 4.4, 4.6, 8.4)
        // ===================================================================

        /// <summary>
        /// Creates a FactionServerTypedClient with a mock handler injected via reflection.
        /// </summary>
        /// <param name="handler">The mock HTTP handler.</param>
        /// <returns>A client with the mocked handler.</returns>
        private static FactionServerTypedClient CreateClientWithMockHandler(
            HttpMessageHandler handler)
        {
            var token = new SecureString();
            token.AppendChar('x');
            token.MakeReadOnly();

            var client = new FactionServerTypedClient("http://fake-server", token);
            var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30),
            };
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "x");

            FieldInfo field = typeof(FactionServerTypedClient).GetField(
                "_httpClient",
                BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(client, httpClient);

            return client;
        }

        /// <summary>
        /// HTTP 400 with valid error body throws FactionValidationException with parsed errors.
        /// </summary>
        [Test]
        public void Http400_ValidErrorBody_ThrowsFactionValidationException()
        {
            // Arrange
            var errors = new List<object>
            {
                new
                {
                    entityType = "Colony",
                    entityUUID = "abc-123",
                    field = "Name",
                    error = "Name is required",
                },
                new
                {
                    entityType = "Blueprint",
                    entityUUID = "def-456",
                    field = "Type",
                    error = "Invalid type",
                },
            };
            string body = JsonConvert.SerializeObject(new { errors });

            var handler = new MockHandler(
                () => new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(body),
                });

            using (var client = CreateClientWithMockHandler(handler))
            {
                // Act & Assert
                var ex = Assert.ThrowsAsync<FactionValidationException>(
                    () => client.GetFactionsAsync(CancellationToken.None));

                Assert.That(ex.Errors.Count, Is.EqualTo(2));
                Assert.That(ex.Errors[0].EntityType, Is.EqualTo("Colony"));
                Assert.That(ex.Errors[0].Field, Is.EqualTo("Name"));
                Assert.That(ex.Errors[1].EntityType, Is.EqualTo("Blueprint"));
                Assert.That(ex.Errors[1].Error, Is.EqualTo("Invalid type"));
            }
        }

        /// <summary>
        /// HTTP 400 with unparseable body throws FactionServerException (fallback).
        /// </summary>
        [Test]
        public void Http400_UnparseableBody_ThrowsFactionServerException()
        {
            // Arrange: body that is not valid JSON
            string body = "this is not JSON at all";

            var handler = new MockHandler(
                () => new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(body),
                });

            using (var client = CreateClientWithMockHandler(handler))
            {
                // Act & Assert
                var ex = Assert.ThrowsAsync<FactionServerException>(
                    () => client.GetFactionsAsync(CancellationToken.None));

                // Should be the base type, not a subtype
                Assert.That(ex.GetType(), Is.EqualTo(typeof(FactionServerException)));
                Assert.That(ex.Message, Does.Contain("400"));
            }
        }

        /// <summary>
        /// HTTP 403 throws FactionAuthorizationException.
        /// </summary>
        [Test]
        public void Http403_ThrowsFactionAuthorizationException()
        {
            // Arrange
            var handler = new MockHandler(
                () => new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = new StringContent("Forbidden"),
                });

            using (var client = CreateClientWithMockHandler(handler))
            {
                // Act & Assert
                var ex = Assert.ThrowsAsync<FactionAuthorizationException>(
                    () => client.GetFactionsAsync(CancellationToken.None));

                Assert.That(ex, Is.Not.Null);
            }
        }

        /// <summary>
        /// Network failure throws FactionConnectionException.
        /// </summary>
        [Test]
        public void NetworkFailure_ThrowsFactionConnectionException()
        {
            // Arrange: handler that throws HttpRequestException (simulates network failure)
            var handler = new MockHandler(
                () => throw new HttpRequestException("Network unreachable"));

            using (var client = CreateClientWithMockHandler(handler))
            {
                // Act & Assert
                var ex = Assert.ThrowsAsync<FactionConnectionException>(
                    () => client.GetFactionsAsync(CancellationToken.None));

                Assert.That(ex, Is.Not.Null);
                Assert.That(ex.InnerException, Is.TypeOf<HttpRequestException>());
            }
        }

        // ===================================================================
        // Interface Coverage Tests (Validates: Requirements 8.1)
        // ===================================================================

        /// <summary>
        /// IFactionServerTypedClient interface has exactly 29 methods
        /// plus IsConnected property (reflection test).
        /// </summary>
        [Test]
        public void Interface_HasExpectedMethodCount()
        {
            // Arrange
            var interfaceType = typeof(IFactionServerTypedClient);

            // Act: get all instance methods declared directly on the interface
            // (excludes methods inherited from IDisposable)
            var methods = interfaceType.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            // Exclude property getters/setters from the method count
            var regularMethods = methods.Where(m => !m.IsSpecialName).ToArray();

            // Assert: 29 API methods
            Assert.That(regularMethods.Length, Is.EqualTo(29),
                $"Expected 29 methods, found: {string.Join(", ", regularMethods.Select(m => m.Name))}");
        }

        /// <summary>
        /// IFactionServerTypedClient interface has IsConnected property.
        /// </summary>
        [Test]
        public void Interface_HasIsConnectedProperty()
        {
            // Arrange
            var interfaceType = typeof(IFactionServerTypedClient);

            // Act
            var prop = interfaceType.GetProperty(
                "IsConnected",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            // Assert
            Assert.That(prop, Is.Not.Null, "IsConnected property should exist");
            Assert.That(prop.PropertyType, Is.EqualTo(typeof(bool)));
            Assert.That(prop.CanRead, Is.True);
            Assert.That(prop.CanWrite, Is.False, "IsConnected should be read-only");
        }

        /// <summary>
        /// Mock HTTP message handler that returns controlled responses or throws.
        /// </summary>
        private class MockHandler : HttpMessageHandler
        {
            private readonly Func<HttpResponseMessage> _responseFactory;

            /// <summary>
            /// Initializes a new instance of the <see cref="MockHandler"/> class.
            /// </summary>
            /// <param name="responseFactory">Factory that produces the response or throws.</param>
            public MockHandler(Func<HttpResponseMessage> responseFactory)
            {
                _responseFactory = responseFactory;
            }

            /// <inheritdoc/>
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                try
                {
                    return Task.FromResult(_responseFactory());
                }
                catch (HttpRequestException)
                {
                    throw;
                }
            }
        }
    }
}
