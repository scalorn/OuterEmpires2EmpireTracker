// <copyright file="FactionPushClient.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NLog;
using OE2EmpireTracker.Common.Client.FactionServer;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// Handles WebSocket connection, push event receiving, heartbeat timer,
    /// reconnection with exponential backoff, and polling fallback.
    /// Extracted from <see cref="RemoteFactionClient"/> to separate real-time
    /// push concerns from HTTP API communication.
    /// Implements <see cref="IDisposable"/> to securely dispose the bearer token,
    /// WebSocket, and timers.
    /// </summary>
    public class FactionPushClient : IDisposable
    {
        /// <summary>
        /// Default polling interval in seconds when WebSocket is unavailable.
        /// </summary>
        private const int DefaultPollingIntervalSeconds = 60;

        /// <summary>
        /// Heartbeat interval in milliseconds (30 seconds).
        /// </summary>
        private const int HeartbeatIntervalMs = 30000;

        /// <summary>
        /// Maximum reconnection delay in milliseconds (60 seconds).
        /// </summary>
        private const int MaxReconnectDelayMs = 60000;

        /// <summary>
        /// Maximum number of consecutive reconnection attempts before switching to polling.
        /// </summary>
        private const int MaxReconnectAttempts = 10;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly string _serverUrl;
        private readonly SecureString _bearerToken;
        private readonly string _trustedThumbprint;
        private readonly IFactionServerTypedClient _typedClient;

        private HttpClient _httpClient;
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _wsCancellation;
        private System.Threading.Timer _heartbeatTimer;
        private System.Threading.Timer _pollingTimer;
        private SemaphoreSlim _rateLimiter;
        private int _rateLimitRequestsPerMinute;
        private bool _isWebSocketMode;
        private int _reconnectAttempts;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionPushClient"/> class.
        /// </summary>
        /// <param name="serverUrl">Base URL of the remote faction server.</param>
        /// <param name="bearerToken">SecureString bearer token for API authentication.</param>
        /// <param name="trustedThumbprint">Certificate thumbprint for self-signed cert pinning (may be empty).</param>
        /// <param name="typedClient">Typed client used for health checks and polling.</param>
        public FactionPushClient(
            string serverUrl,
            SecureString bearerToken,
            string trustedThumbprint,
            IFactionServerTypedClient typedClient)
        {
            _serverUrl = (serverUrl ?? string.Empty).TrimEnd('/');
            _bearerToken = bearerToken;
            _trustedThumbprint = (trustedThumbprint ?? string.Empty).Replace(" ", string.Empty);
            _typedClient = typedClient;
            _rateLimitRequestsPerMinute = 60;
            _rateLimiter = new SemaphoreSlim(60, 60);
            InitializeHttpClient();
        }

        /// <summary>
        /// Occurs when a push event is received via WebSocket.
        /// </summary>
        public event EventHandler<PushEventArgs> EventReceived;

        /// <summary>
        /// Occurs when the connection status changes.
        /// </summary>
        public event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;

        /// <summary>
        /// Occurs when the real-time mode changes (WebSocket vs polling).
        /// </summary>
        public event EventHandler<RealtimeModeChangedEventArgs> RealtimeModeChanged;

        /// <summary>
        /// Occurs when the server communicates a new rate limit via WebSocket.
        /// </summary>
        public event EventHandler<RateLimitChangedEventArgs> RateLimitChanged;

        /// <summary>
        /// Gets a value indicating whether the client is currently connected.
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the client is receiving real-time updates via WebSocket.
        /// When false, the client is in polling mode.
        /// </summary>
        public bool IsRealtimeMode => _isWebSocketMode;

        /// <summary>
        /// Gets or sets the polling interval in seconds used when WebSocket is unavailable.
        /// </summary>
        public int PollingIntervalSeconds { get; set; } = DefaultPollingIntervalSeconds;

        /// <summary>
        /// Connects to the WebSocket endpoint and begins receiving push events.
        /// Starts the receive loop, heartbeat timer, and reconnection logic.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ConnectWebSocketAsync()
        {
            if (_disposed)
            {
                return;
            }

            StopWebSocket();

            _wsCancellation = new CancellationTokenSource();
            _webSocket = new ClientWebSocket();

            string wsUrl = BuildWebSocketUrl();
            Log.Info("Connecting WebSocket to {0}", MaskTokenInUrl(wsUrl));

            try
            {
                await _webSocket.ConnectAsync(new Uri(wsUrl), _wsCancellation.Token).ConfigureAwait(false);
                _isWebSocketMode = true;
                _reconnectAttempts = 0;
                StopPolling();
                OnRealtimeModeChanged(true);
                Log.Info("WebSocket connected successfully");

                StartHeartbeat();
                _ = Task.Run(() => ReceiveLoopAsync(_wsCancellation.Token));
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "WebSocket connection failed");
                _isWebSocketMode = false;
                OnRealtimeModeChanged(false);
                _ = Task.Run(() => ReconnectLoopAsync());
            }
        }

        /// <summary>
        /// Gracefully disconnects from the WebSocket endpoint.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task DisconnectWebSocketAsync()
        {
            StopWebSocket();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Disconnects both WebSocket and polling, setting connection status to offline.
        /// </summary>
        public void Disconnect()
        {
            StopWebSocket();
            StopPolling();
            SetConnectionStatus(false, "Disconnected");
            Log.Info("Disconnected from remote server {0}", _serverUrl);
        }

        /// <summary>
        /// Attempts to connect to the server by calling the typed client's health check.
        /// </summary>
        /// <returns>True if the server is reachable; otherwise false.</returns>
        public async Task<bool> TryConnectAsync()
        {
            try
            {
                bool healthy = await _typedClient.CheckHealthAsync().ConfigureAwait(false);
                SetConnectionStatus(healthy, healthy ? "Connected" : "Health check failed");
                return healthy;
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "TryConnectAsync failed for {0}", _serverUrl);
                SetConnectionStatus(false, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Sets the connection status and raises the <see cref="ConnectionStatusChanged"/> event.
        /// </summary>
        /// <param name="connected">Whether the client is connected.</param>
        /// <param name="message">Human-readable status message.</param>
        public void SetConnectionStatus(bool connected, string message)
        {
            if (IsConnected != connected)
            {
                IsConnected = connected;
                ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs
                {
                    IsConnected = connected,
                    Message = message,
                });
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="FactionPushClient"/>.
        /// Disposes the WebSocket, timers, HTTP client, and SecureString token.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _wsCancellation?.Cancel();
            _wsCancellation?.Dispose();
            _webSocket?.Dispose();
            _heartbeatTimer?.Dispose();
            _pollingTimer?.Dispose();
            _httpClient?.Dispose();
            _rateLimiter?.Dispose();
            _bearerToken?.Dispose();
        }

        /// <summary>
        /// Masks the bearer token in a WebSocket URL for safe logging.
        /// </summary>
        /// <param name="url">The WebSocket URL containing the token query parameter.</param>
        /// <returns>The URL with the token value replaced by asterisks.</returns>
        private static string MaskTokenInUrl(string url)
        {
            int tokenIdx = url.IndexOf("token=", StringComparison.Ordinal);
            if (tokenIdx < 0)
            {
                return url;
            }

            return url.Substring(0, tokenIdx + 6) + "***";
        }

        /// <summary>
        /// Calculates exponential backoff delay for reconnection attempts.
        /// Doubles each attempt: 1s, 2s, 4s, 8s, 16s, 32s, capped at MaxReconnectDelayMs.
        /// </summary>
        /// <param name="attempt">The reconnection attempt number (1-based).</param>
        /// <returns>The delay in milliseconds before the next reconnection attempt.</returns>
        private static int CalculateBackoffDelay(int attempt)
        {
            int delayMs = (int)Math.Pow(2, attempt - 1) * 1000;
            return Math.Min(delayMs, MaxReconnectDelayMs);
        }

        /// <summary>
        /// Initializes the HTTP client with certificate pinning and bearer token authentication.
        /// Creates a handler with custom certificate validation if a trusted thumbprint is configured,
        /// and sets the ServicePointManager callback for WebSocket connections.
        /// </summary>
        private void InitializeHttpClient()
        {
            var handler = new HttpClientHandler();

            if (!string.IsNullOrEmpty(_trustedThumbprint))
            {
                handler.ServerCertificateCustomValidationCallback = (request, certificate, chain, sslPolicyErrors) =>
                {
                    if (sslPolicyErrors == SslPolicyErrors.None)
                    {
                        return true;
                    }

                    if (certificate == null)
                    {
                        return false;
                    }

                    string thumbprint = certificate.GetCertHashString();
                    bool match = string.Equals(thumbprint, _trustedThumbprint, StringComparison.OrdinalIgnoreCase);
                    if (!match)
                    {
                        Log.Warn(
                            "HTTP certificate thumbprint mismatch. Expected={0}..., Actual={1}...",
                            _trustedThumbprint.Substring(0, Math.Min(8, _trustedThumbprint.Length)),
                            thumbprint.Substring(0, Math.Min(8, thumbprint.Length)));
                    }

                    return match;
                };

                // Also set ServicePointManager callback for WebSocket connections
                // (ClientWebSocket in .NET Framework 4.8.1 uses ServicePointManager)
                System.Net.ServicePointManager.ServerCertificateValidationCallback = ValidateWebSocketCertificate;
            }

            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromMinutes(5);

            // Set the Authorization header using the SecureString token.
            string token = CredentialStore.SecureStringToString(_bearerToken);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// Validates server certificates for WebSocket connections via ServicePointManager.
        /// Returns true if there are no SSL errors, false if the certificate is null,
        /// otherwise compares the certificate thumbprint against the trusted thumbprint.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="certificate">The X.509 certificate presented by the server.</param>
        /// <param name="chain">The X.509 certificate chain.</param>
        /// <param name="sslPolicyErrors">Any SSL policy errors detected.</param>
        /// <returns>True if the certificate is valid; otherwise false.</returns>
        private bool ValidateWebSocketCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (certificate == null)
            {
                return false;
            }

            string thumbprint = certificate.GetCertHashString();

            if (string.IsNullOrEmpty(_trustedThumbprint))
            {
                return false;
            }

            bool match = string.Equals(thumbprint, _trustedThumbprint, StringComparison.OrdinalIgnoreCase);
            if (!match)
            {
                Log.Warn(
                    "WebSocket certificate thumbprint mismatch. Expected={0}..., Actual={1}...",
                    _trustedThumbprint.Substring(0, Math.Min(8, _trustedThumbprint.Length)),
                    thumbprint.Substring(0, Math.Min(8, thumbprint.Length)));
            }

            return match;
        }

        /// <summary>
        /// Builds the WebSocket URL by replacing the HTTP scheme with WebSocket scheme
        /// and appending the bearer token as a query parameter.
        /// </summary>
        /// <returns>The fully-qualified WebSocket URL with authentication token.</returns>
        private string BuildWebSocketUrl()
        {
            string wsBase = _serverUrl;
            if (wsBase.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                wsBase = "wss://" + wsBase.Substring(8);
            }
            else if (wsBase.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                wsBase = "ws://" + wsBase.Substring(7);
            }

            string token = CredentialStore.SecureStringToString(_bearerToken);
            return wsBase + "/ws?token=" + Uri.EscapeDataString(token);
        }

        /// <summary>
        /// Receives WebSocket messages in a loop until cancellation or disconnection.
        /// Assembles multi-frame messages and dispatches complete messages for processing.
        /// </summary>
        /// <param name="ct">Cancellation token to stop the receive loop.</param>
        /// <returns>A task representing the asynchronous receive loop.</returns>
        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            var buffer = new byte[4096];
            var messageBuilder = new StringBuilder();

            try
            {
                while (!ct.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result;
                    messageBuilder.Clear();

                    do
                    {
                        result = await _webSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            ct).ConfigureAwait(false);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Log.Info("WebSocket received close frame");
                            HandleWebSocketDisconnect();
                            return;
                        }

                        messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                    while (!result.EndOfMessage);

                    string message = messageBuilder.ToString();
                    ProcessIncomingMessage(message);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Debug("WebSocket receive loop cancelled");
            }
            catch (WebSocketException ex)
            {
                Log.Warn(ex, "WebSocket error in receive loop");
                HandleWebSocketDisconnect();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in WebSocket receive loop");
                HandleWebSocketDisconnect();
            }
        }

        /// <summary>
        /// Parses an incoming WebSocket message as JSON and dispatches it
        /// to the appropriate handler based on the message type field.
        /// </summary>
        /// <param name="message">The raw JSON message string received from the WebSocket.</param>
        private void ProcessIncomingMessage(string message)
        {
            try
            {
                var json = JObject.Parse(message);
                string type = json.Value<string>("type");

                switch (type)
                {
                    case "connected":
                        HandleConnectedMessage(json);
                        break;
                    case "pong":
                        Log.Debug("WebSocket pong received");
                        break;
                    case "event":
                        HandleEventMessage(json);
                        break;
                    case "rateLimitChanged":
                        HandleRateLimitMessage(json);
                        break;
                    default:
                        Log.Debug("Unknown WebSocket message type: {0}", type);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Failed to parse WebSocket message: {0}", message);
            }
        }

        /// <summary>
        /// Handles the "connected" WebSocket message received during handshake.
        /// Extracts initial rate limits if present and raises the RateLimitChanged event.
        /// </summary>
        /// <param name="json">The parsed JSON object of the connected message.</param>
        private void HandleConnectedMessage(JObject json)
        {
            Log.Info("WebSocket connected message received from server");

            var rateLimits = json["rateLimits"];
            if (rateLimits != null)
            {
                int rpm = rateLimits.Value<int>("requestsPerMinute");
                if (rpm > 0)
                {
                    ApplyRateLimit(rpm);
                    RateLimitChanged?.Invoke(this, new RateLimitChangedEventArgs { RequestsPerMinute = rpm });
                }
            }
        }

        /// <summary>
        /// Handles the "event" WebSocket message by extracting event fields
        /// and raising the EventReceived event for downstream consumers.
        /// </summary>
        /// <param name="json">The parsed JSON object of the event message.</param>
        private void HandleEventMessage(JObject json)
        {
            var eventObj = json["event"];
            if (eventObj == null)
            {
                return;
            }

            var args = new PushEventArgs
            {
                EventType = eventObj.Value<string>("eventType"),
                EntityType = eventObj.Value<string>("entityType"),
                EntityUUID = eventObj.Value<string>("entityUUID"),
                CharacterUUID = eventObj.Value<string>("characterUUID"),
                Timestamp = eventObj.Value<string>("timestamp"),
            };

            Log.Debug(
                "Push event received: {0} {1} {2}",
                args.EventType,
                args.EntityType,
                args.EntityUUID);

            EventReceived?.Invoke(this, args);
        }

        /// <summary>
        /// Handles the "rateLimitChanged" WebSocket message by applying the new
        /// rate limit internally and raising the RateLimitChanged event for external listeners.
        /// </summary>
        /// <param name="json">The parsed JSON object of the rate limit message.</param>
        private void HandleRateLimitMessage(JObject json)
        {
            var rateLimits = json["rateLimits"];
            if (rateLimits != null)
            {
                int rpm = rateLimits.Value<int>("requestsPerMinute");
                if (rpm > 0)
                {
                    ApplyRateLimit(rpm);
                    RateLimitChanged?.Invoke(this, new RateLimitChangedEventArgs { RequestsPerMinute = rpm });
                    Log.Info("Rate limit updated via WebSocket: {0} requests/minute", rpm);
                }
            }
        }

        /// <summary>
        /// Applies a new rate limit by replacing the internal semaphore with one
        /// matching the new requests-per-minute value.
        /// </summary>
        /// <param name="requestsPerMinute">The new rate limit in requests per minute.</param>
        private void ApplyRateLimit(int requestsPerMinute)
        {
            if (requestsPerMinute == _rateLimitRequestsPerMinute)
            {
                return;
            }

            _rateLimitRequestsPerMinute = requestsPerMinute;

            var oldLimiter = _rateLimiter;
            _rateLimiter = new SemaphoreSlim(requestsPerMinute, requestsPerMinute);
            oldLimiter?.Dispose();

            Log.Info("Rate limiter configured: {0} requests/minute", requestsPerMinute);
        }

        /// <summary>
        /// Acquires a rate limit token before making an outgoing REST request.
        /// Blocks if the rate limit has been reached until a token becomes available.
        /// Releases the token after 60 seconds (sliding window approximation).
        /// </summary>
        /// <returns>A task that completes when a rate limit token is acquired.</returns>
        private async Task AcquireRateLimitTokenAsync()
        {
            await _rateLimiter.WaitAsync().ConfigureAwait(false);

            _ = Task.Run(async () =>
            {
                await Task.Delay(60000).ConfigureAwait(false);
                try
                {
                    _rateLimiter.Release();
                }
                catch (SemaphoreFullException)
                {
                    // Ignore — can happen if rate limit was reconfigured
                }
            });
        }

        /// <summary>
        /// Starts the heartbeat timer to send pings at regular intervals.
        /// Disposes any existing timer first, then creates a new one firing every
        /// <see cref="HeartbeatIntervalMs"/> milliseconds.
        /// </summary>
        private void StartHeartbeat()
        {
            StopHeartbeat();
            _heartbeatTimer = new System.Threading.Timer(
                SendHeartbeat,
                null,
                HeartbeatIntervalMs,
                HeartbeatIntervalMs);
        }

        /// <summary>
        /// Stops and disposes the heartbeat timer if it is currently running.
        /// </summary>
        private void StopHeartbeat()
        {
            if (_heartbeatTimer != null)
            {
                _heartbeatTimer.Dispose();
                _heartbeatTimer = null;
            }
        }

        /// <summary>
        /// Sends a ping message over the WebSocket to keep the connection alive.
        /// Silently returns if the WebSocket is not in the Open state.
        /// </summary>
        /// <param name="state">Timer callback state (unused).</param>
        private async void SendHeartbeat(object state)
        {
            if (_webSocket?.State != WebSocketState.Open)
            {
                return;
            }

            try
            {
                string pingJson = "{\"type\":\"ping\"}";
                byte[] pingBytes = Encoding.UTF8.GetBytes(pingJson);
                await _webSocket.SendAsync(
                    new ArraySegment<byte>(pingBytes),
                    WebSocketMessageType.Text,
                    true,
                    _wsCancellation?.Token ?? CancellationToken.None).ConfigureAwait(false);
                Log.Debug("WebSocket ping sent");
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to send WebSocket ping");
            }
        }

        /// <summary>
        /// Handles a WebSocket disconnection by stopping the heartbeat, resetting
        /// the WebSocket mode flag, raising the RealtimeModeChanged event, and
        /// initiating the reconnection loop with exponential backoff.
        /// </summary>
        private void HandleWebSocketDisconnect()
        {
            _isWebSocketMode = false;
            StopHeartbeat();
            OnRealtimeModeChanged(false);
            _ = Task.Run(() => ReconnectLoopAsync());
        }

        /// <summary>
        /// Attempts to reconnect the WebSocket using exponential backoff.
        /// Delays double each attempt (1s, 2s, 4s... capped at 60s).
        /// After <see cref="MaxReconnectAttempts"/> failures, switches to polling mode.
        /// </summary>
        /// <returns>A task representing the asynchronous reconnection loop.</returns>
        private async Task ReconnectLoopAsync()
        {
            while (!_disposed)
            {
                _reconnectAttempts++;

                if (_reconnectAttempts > MaxReconnectAttempts)
                {
                    Log.Warn(
                        "WebSocket reconnection failed after {0} attempts, switching to polling",
                        MaxReconnectAttempts);
                    StartPolling();
                    return;
                }

                int delayMs = CalculateBackoffDelay(_reconnectAttempts);
                Log.Info(
                    "WebSocket reconnecting in {0}ms (attempt {1}/{2})",
                    delayMs,
                    _reconnectAttempts,
                    MaxReconnectAttempts);

                await Task.Delay(delayMs).ConfigureAwait(false);

                if (_disposed)
                {
                    return;
                }

                try
                {
                    StopWebSocket();
                    _wsCancellation = new CancellationTokenSource();
                    _webSocket = new ClientWebSocket();

                    string wsUrl = BuildWebSocketUrl();
                    await _webSocket.ConnectAsync(
                        new Uri(wsUrl),
                        _wsCancellation.Token).ConfigureAwait(false);

                    _isWebSocketMode = true;
                    _reconnectAttempts = 0;
                    StopPolling();
                    OnRealtimeModeChanged(true);
                    Log.Info("WebSocket reconnected successfully");

                    StartHeartbeat();
                    _ = Task.Run(() => ReceiveLoopAsync(_wsCancellation.Token));
                    return;
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "WebSocket reconnection attempt {0} failed", _reconnectAttempts);
                }
            }
        }

        /// <summary>
        /// Starts the polling fallback timer at <see cref="PollingIntervalSeconds"/> intervals.
        /// Stops any existing polling timer first, then creates a new one.
        /// Sets the mode to non-WebSocket and raises the RealtimeModeChanged event.
        /// </summary>
        private void StartPolling()
        {
            StopPolling();
            int intervalMs = PollingIntervalSeconds * 1000;
            _pollingTimer = new System.Threading.Timer(
                PollServer,
                null,
                intervalMs,
                intervalMs);
            _isWebSocketMode = false;
            OnRealtimeModeChanged(false);
            Log.Info("Switched to polling mode (interval: {0}s)", PollingIntervalSeconds);
        }

        /// <summary>
        /// Stops the polling fallback timer if active.
        /// Disposes the timer and sets the reference to null.
        /// </summary>
        private void StopPolling()
        {
            if (_pollingTimer != null)
            {
                _pollingTimer.Dispose();
                _pollingTimer = null;
            }
        }

        /// <summary>
        /// Polls the server for sync data via the typed client. If data is available,
        /// raises the EventReceived event with a SyncPoll event type. Periodically
        /// attempts to re-establish the WebSocket connection when reconnect attempts
        /// have been exhausted.
        /// </summary>
        /// <param name="state">Timer callback state (unused).</param>
        private async void PollServer(object state)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                var syncResult = await _typedClient.GetSyncSnapshotAsync().ConfigureAwait(false);
                if (syncResult != null)
                {
                    EventReceived?.Invoke(this, new PushEventArgs
                    {
                        EventType = "SyncPoll",
                        EntityType = "All",
                        EntityUUID = string.Empty,
                        CharacterUUID = string.Empty,
                        Timestamp = SystemClock.UtcNow.ToString("o"),
                    });
                }

                // Try to reconnect WebSocket on each poll cycle
                if (!_isWebSocketMode && _reconnectAttempts > MaxReconnectAttempts)
                {
                    _reconnectAttempts = 0;
                    _ = Task.Run(() => ConnectWebSocketAsync());
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Polling cycle failed");
            }
        }

        /// <summary>
        /// Stops the WebSocket connection, cancelling the receive loop and closing the socket.
        /// Also stops the heartbeat timer to prevent pings on a closed connection.
        /// </summary>
        private void StopWebSocket()
        {
            StopHeartbeat();

            if (_wsCancellation != null)
            {
                _wsCancellation.Cancel();
                _wsCancellation.Dispose();
                _wsCancellation = null;
            }

            if (_webSocket != null)
            {
                try
                {
                    if (_webSocket.State == WebSocketState.Open ||
                        _webSocket.State == WebSocketState.CloseReceived)
                    {
                        _webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Client disconnecting",
                            CancellationToken.None).Wait(TimeSpan.FromSeconds(2));
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Error closing WebSocket gracefully");
                }

                _webSocket.Dispose();
                _webSocket = null;
            }
        }

        /// <summary>
        /// Raises the RealtimeModeChanged event with the specified mode.
        /// </summary>
        /// <param name="isRealtime">True if operating in real-time WebSocket mode; false for polling.</param>
        private void OnRealtimeModeChanged(bool isRealtime)
        {
            RealtimeModeChanged?.Invoke(this, new RealtimeModeChangedEventArgs
            {
                IsRealtime = isRealtime,
            });
        }
    }
}
