// <copyright file="FactionPushClient.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NLog;
using OE2EmpireTracker.Common.Client.FactionServer;

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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Attempts to connect to the server by calling the typed client's health check.
        /// </summary>
        /// <returns>True if the server is reachable; otherwise false.</returns>
        public Task<bool> TryConnectAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the connection status and raises the <see cref="ConnectionStatusChanged"/> event.
        /// </summary>
        /// <param name="connected">Whether the client is connected.</param>
        /// <param name="message">Human-readable status message.</param>
        public void SetConnectionStatus(bool connected, string message)
        {
            throw new NotImplementedException();
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
        /// Stub — full implementation in Task 2.3.
        /// </summary>
        private void StartHeartbeat()
        {
            // Will be implemented in Task 2.3 (heartbeat, reconnection, polling)
        }

        /// <summary>
        /// Handles a WebSocket disconnection by resetting state and initiating reconnection.
        /// Stub — full implementation in Task 2.3.
        /// </summary>
        private void HandleWebSocketDisconnect()
        {
            _isWebSocketMode = false;
            OnRealtimeModeChanged(false);
            _ = Task.Run(() => ReconnectLoopAsync());
        }

        /// <summary>
        /// Attempts to reconnect the WebSocket with exponential backoff.
        /// Stub — full implementation in Task 2.3.
        /// </summary>
        /// <returns>A task representing the asynchronous reconnection loop.</returns>
        private Task ReconnectLoopAsync()
        {
            // Will be implemented in Task 2.3 (heartbeat, reconnection, polling)
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the polling fallback timer if active.
        /// Stub — full implementation in Task 2.3.
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
        /// Stops the WebSocket connection, cancelling the receive loop and closing the socket.
        /// Stub — full implementation in Task 2.4.
        /// </summary>
        private void StopWebSocket()
        {
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
