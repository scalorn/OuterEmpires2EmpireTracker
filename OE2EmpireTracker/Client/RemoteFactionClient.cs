// <copyright file="RemoteFactionClient.cs" company="OE2EmpireTracker">
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
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// HTTP and WebSocket client that communicates with the Remote Faction Service.
    /// Handles bearer-token auth (via SecureString), optional self-signed certificate pinning,
    /// WebSocket real-time push events, automatic reconnection with exponential backoff,
    /// heartbeat pings, polling fallback, and server-communicated rate limiting.
    /// Implements <see cref="IDisposable"/> to securely dispose the bearer token.
    /// </summary>
    public class RemoteFactionClient : IDisposable
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

        private const string ApiPrefix = "/api/v1";

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly string _serverUrl;
        private readonly SecureString _bearerToken;
        private readonly string _trustedThumbprint;

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
        /// Initializes a new instance of the <see cref="RemoteFactionClient"/> class.
        /// </summary>
        /// <param name="serverUrl">Base URL of the remote faction server.</param>
        /// <param name="bearerToken">SecureString bearer token for API authentication.</param>
        /// <param name="trustedThumbprint">Certificate thumbprint for self-signed cert pinning (may be empty).</param>
        public RemoteFactionClient(string serverUrl, SecureString bearerToken, string trustedThumbprint)
        {
            _serverUrl = (serverUrl ?? string.Empty).TrimEnd('/');
            _bearerToken = bearerToken;
            _trustedThumbprint = (trustedThumbprint ?? string.Empty).Replace(" ", string.Empty);
            _rateLimitRequestsPerMinute = 60;
            _rateLimiter = new SemaphoreSlim(60, 60);
            InitializeHttpClient();
        }

        /// <summary>
        /// Occurs when the connection status changes.
        /// </summary>
        public event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;

        /// <summary>
        /// Occurs when a push event is received via WebSocket.
        /// </summary>
        public event EventHandler<PushEventArgs> EventReceived;

        /// <summary>
        /// Occurs when the real-time mode changes (WebSocket vs polling).
        /// </summary>
        public event EventHandler<RealtimeModeChangedEventArgs> RealtimeModeChanged;

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
        /// Gets or sets the polling interval in seconds (used when WebSocket is unavailable).
        /// </summary>
        public int PollingIntervalSeconds { get; set; } = DefaultPollingIntervalSeconds;

        /// <summary>
        /// Attempts to connect to the remote server by calling the health endpoint.
        /// </summary>
        /// <returns>True if the server responded successfully; otherwise false.</returns>
        public async Task<bool> TryConnectAsync()
        {
            try
            {
                bool healthy = await CheckHealthAsync().ConfigureAwait(false);
                SetConnected(healthy, healthy ? "Connected" : "Health check failed");
                return healthy;
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "TryConnectAsync failed for {0}", _serverUrl);
                SetConnected(false, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Disconnects from the remote server and stops WebSocket/polling.
        /// </summary>
        public void Disconnect()
        {
            StopWebSocket();
            StopPolling();
            SetConnected(false, "Disconnected");
            Log.Info("Disconnected from remote server {0}", _serverUrl);
        }

        /// <summary>
        /// Checks the server health endpoint.
        /// </summary>
        /// <returns>True if the server is healthy; otherwise false.</returns>
        public async Task<bool> CheckHealthAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_serverUrl + "/health").ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Log.Debug(ex, "Health check failed for {0}", _serverUrl);
                return false;
            }
        }

        /// <summary>
        /// Connects the WebSocket to the server for real-time push events.
        /// Starts the receive loop, heartbeat timer, and reconnection logic.
        /// </summary>
        /// <returns>A task representing the async operation.</returns>
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
        /// Disconnects the WebSocket gracefully.
        /// </summary>
        /// <returns>A task representing the async operation.</returns>
        public async Task DisconnectWebSocketAsync()
        {
            StopWebSocket();
            await Task.CompletedTask.ConfigureAwait(false);
        }

        /// <summary>
        /// Gets all factions from the server.
        /// </summary>
        /// <returns>JSON string of factions, or null on failure.</returns>
        public async Task<string> GetFactionsAsync()
        {
            return await GetStringAsync("/factions").ConfigureAwait(false);
        }

        /// <summary>
        /// Gets all characters from the server.
        /// </summary>
        /// <returns>JSON string of characters, or null on failure.</returns>
        public async Task<string> GetCharactersAsync()
        {
            return await GetStringAsync("/characters").ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a character on the server. For bootstrapping, pass the local UUID
        /// so the server uses it instead of generating a new one.
        /// </summary>
        /// <param name="name">Character name.</param>
        /// <param name="uuid">Optional UUID to use (bootstrapping).</param>
        /// <returns>A task representing the async operation.</returns>
        public async Task CreateCharacterAsync(string name, string uuid = null)
        {
            string json;
            if (!string.IsNullOrEmpty(uuid))
            {
                json = string.Format("{{\"name\":\"{0}\",\"uuid\":\"{1}\"}}", name, uuid);
            }
            else
            {
                json = string.Format("{{\"name\":\"{0}\"}}", name);
            }

            await PostStringAsync("/characters", json).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets character data of a specific type.
        /// </summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="dataType">The data type (e.g. colonies, blueprints).</param>
        /// <returns>JSON string, or null on failure.</returns>
        public async Task<string> GetCharacterDataAsync(string characterUUID, string dataType)
        {
            string path = string.Format("/characters/{0}/data/{1}", characterUUID, dataType);
            return await GetStringAsync(path).ConfigureAwait(false);
        }

        /// <summary>
        /// Uploads character data of a specific type.
        /// </summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="dataType">The data type.</param>
        /// <param name="json">The JSON payload.</param>
        /// <returns>A task representing the async operation.</returns>
        public async Task UploadCharacterDataAsync(string characterUUID, string dataType, string json)
        {
            string path = string.Format("/characters/{0}/data/{1}", characterUUID, dataType);
            await PutStringAsync(path, json).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets all data for a character (all types combined).
        /// </summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>JSON string, or null on failure.</returns>
        public async Task<string> GetAllCharacterDataAsync(string characterUUID)
        {
            string path = string.Format("/characters/{0}/data", characterUUID);
            return await GetStringAsync(path).ConfigureAwait(false);
        }

        /// <summary>
        /// Uploads all data for a character (bulk upload).
        /// </summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <param name="json">The JSON payload containing all data types.</param>
        /// <returns>A task representing the async operation.</returns>
        public async Task UploadAllCharacterDataAsync(string characterUUID, string json)
        {
            string path = string.Format("/characters/{0}/data", characterUUID);
            await PutStringAsync(path, json).ConfigureAwait(false);
        }

        /// <summary>
        /// Uploads global/baseline data to the server.
        /// Requires Owner token.
        /// </summary>
        /// <param name="dataType">The global data type (e.g. "baseline").</param>
        /// <param name="json">The JSON payload.</param>
        /// <returns>A task representing the async operation.</returns>
        public async Task UploadGlobalDataAsync(string dataType, string json)
        {
            string path = string.Format("/global/{0}", dataType);
            await PutStringAsync(path, json).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a sync snapshot from the server (timestamps for conflict resolution).
        /// </summary>
        /// <returns>JSON string of the sync snapshot, or null on failure.</returns>
        public async Task<string> GetSyncSnapshotAsync()
        {
            return await GetStringAsync("/sync/snapshot").ConfigureAwait(false);
        }

        /// <summary>
        /// Exports all character data in a portable format.
        /// </summary>
        /// <param name="characterUUID">The character UUID.</param>
        /// <returns>JSON string of the export, or null on failure.</returns>
        public async Task<string> ExportCharacterDataAsync(string characterUUID)
        {
            string path = string.Format("/characters/{0}/export", characterUUID);
            return await GetStringAsync(path).ConfigureAwait(false);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="RemoteFactionClient"/>.
        /// Disposes the SecureString bearer token, WebSocket, timers, and the underlying HttpClient.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and (optionally) managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    StopWebSocket();
                    StopPolling();
                    _bearerToken?.Dispose();
                    _httpClient?.Dispose();
                    _rateLimiter?.Dispose();
                }

                _disposed = true;
            }
        }

        private static bool PromptTrustCertificate(string thumbprint)
        {
            bool result = false;

            if (System.Windows.Forms.Application.OpenForms.Count > 0)
            {
                var mainForm = System.Windows.Forms.Application.OpenForms[0];
                if (mainForm.InvokeRequired)
                {
                    mainForm.Invoke((Action)(() =>
                    {
                        result = ShowTrustDialog(thumbprint);
                    }));
                }
                else
                {
                    result = ShowTrustDialog(thumbprint);
                }
            }
            else
            {
                result = ShowTrustDialog(thumbprint);
            }

            return result;
        }

        private static bool ShowTrustDialog(string thumbprint)
        {
            var dialogResult = System.Windows.Forms.MessageBox.Show(
                "The server presented an untrusted certificate.\n\n" +
                "Thumbprint: " + thumbprint + "\n\n" +
                "Do you want to trust this server?",
                "Trust Server Certificate?",
                System.Windows.Forms.MessageBoxButtons.YesNo,
                System.Windows.Forms.MessageBoxIcon.Warning);

            if (dialogResult == System.Windows.Forms.DialogResult.Yes)
            {
                var store = Services.PreferencesStore.GetInstance();
                store.Preferences.ServerConnection.TrustedThumbprint = thumbprint;
                store.Save();
                return true;
            }

            return false;
        }

        private static int CalculateBackoffDelay(int attempt)
        {
            // Exponential backoff: 1s, 2s, 4s, 8s, 16s, 32s, 60s (capped)
            int delayMs = (int)Math.Pow(2, attempt - 1) * 1000;
            return Math.Min(delayMs, MaxReconnectDelayMs);
        }

        private static string MaskTokenInUrl(string url)
        {
            int tokenIdx = url.IndexOf("token=", StringComparison.Ordinal);
            if (tokenIdx < 0)
            {
                return url;
            }

            return url.Substring(0, tokenIdx + 6) + "***";
        }

        private void InitializeHttpClient()
        {
            var handler = new HttpClientHandler();

            if (!string.IsNullOrEmpty(_trustedThumbprint))
            {
                handler.ServerCertificateCustomValidationCallback = ValidateCertificate;

                // Also set ServicePointManager callback for WebSocket connections
                // (ClientWebSocket in .NET Framework 4.8.1 uses ServicePointManager)
                System.Net.ServicePointManager.ServerCertificateValidationCallback = ValidateWebSocketCertificate;
            }

            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Set the Authorization header using the SecureString token.
            string token = CredentialStore.SecureStringToString(_bearerToken);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        private bool ValidateCertificate(
            HttpRequestMessage request,
            X509Certificate2 certificate,
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
                bool trusted = PromptTrustCertificate(thumbprint);
                if (trusted)
                {
                    Log.Info("User trusted new certificate thumbprint: {0}", thumbprint);
                    return true;
                }

                Log.Warn("User rejected certificate thumbprint: {0}", thumbprint);
                return false;
            }

            bool match = string.Equals(thumbprint, _trustedThumbprint, StringComparison.OrdinalIgnoreCase);
            if (!match)
            {
                Log.Warn(
                    "Certificate thumbprint mismatch. Expected={0}..., Actual={1}...",
                    _trustedThumbprint.Substring(0, Math.Min(8, _trustedThumbprint.Length)),
                    thumbprint.Substring(0, Math.Min(8, thumbprint.Length)));
            }

            return match;
        }

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

        private string BuildWebSocketUrl()
        {
            // Replace https:// with wss:// and append /ws?token=...
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

        // -----------------------------------------------------------------------
        // WebSocket Receive Loop
        // -----------------------------------------------------------------------

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

        private void HandleConnectedMessage(JObject json)
        {
            Log.Info("WebSocket connected message received from server");

            // Parse rate limits from the handshake
            var rateLimits = json["rateLimits"];
            if (rateLimits != null)
            {
                int rpm = rateLimits.Value<int>("requestsPerMinute");
                if (rpm > 0)
                {
                    ApplyRateLimit(rpm);
                }
            }
        }

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

        private void HandleRateLimitMessage(JObject json)
        {
            var rateLimits = json["rateLimits"];
            if (rateLimits != null)
            {
                int rpm = rateLimits.Value<int>("requestsPerMinute");
                if (rpm > 0)
                {
                    ApplyRateLimit(rpm);
                    Log.Info("Rate limit updated via WebSocket: {0} requests/minute", rpm);
                }
            }
        }

        // -----------------------------------------------------------------------
        // Rate Limiting (Task 14.6)
        // -----------------------------------------------------------------------

        private void ApplyRateLimit(int requestsPerMinute)
        {
            if (requestsPerMinute == _rateLimitRequestsPerMinute)
            {
                return;
            }

            _rateLimitRequestsPerMinute = requestsPerMinute;

            // Replace the semaphore with one matching the new limit
            var oldLimiter = _rateLimiter;
            _rateLimiter = new SemaphoreSlim(requestsPerMinute, requestsPerMinute);
            oldLimiter?.Dispose();

            Log.Info("Rate limiter configured: {0} requests/minute", requestsPerMinute);
        }

        /// <summary>
        /// Acquires a rate limit token before making an outgoing REST request.
        /// Blocks if the rate limit has been reached until a token becomes available.
        /// </summary>
        /// <returns>A task that completes when a rate limit token is acquired.</returns>
        private async Task AcquireRateLimitTokenAsync()
        {
            await _rateLimiter.WaitAsync().ConfigureAwait(false);

            // Release the token after 60 seconds (sliding window approximation)
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

        // -----------------------------------------------------------------------
        // Heartbeat (Task 14.3)
        // -----------------------------------------------------------------------

        private void StartHeartbeat()
        {
            StopHeartbeat();
            _heartbeatTimer = new System.Threading.Timer(
                SendHeartbeat,
                null,
                HeartbeatIntervalMs,
                HeartbeatIntervalMs);
        }

        private void StopHeartbeat()
        {
            if (_heartbeatTimer != null)
            {
                _heartbeatTimer.Dispose();
                _heartbeatTimer = null;
            }
        }

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

        // -----------------------------------------------------------------------
        // Reconnection with Exponential Backoff (Task 14.2)
        // -----------------------------------------------------------------------

        private void HandleWebSocketDisconnect()
        {
            _isWebSocketMode = false;
            StopHeartbeat();
            OnRealtimeModeChanged(false);
            _ = Task.Run(() => ReconnectLoopAsync());
        }

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

        // -----------------------------------------------------------------------
        // Polling Fallback (Task 14.5)
        // -----------------------------------------------------------------------

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

        private void StopPolling()
        {
            if (_pollingTimer != null)
            {
                _pollingTimer.Dispose();
                _pollingTimer = null;
            }
        }

        private async void PollServer(object state)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                string syncJson = await GetSyncSnapshotAsync().ConfigureAwait(false);
                if (!string.IsNullOrEmpty(syncJson))
                {
                    // Raise a generic sync event so the UI knows data may have changed
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

        // -----------------------------------------------------------------------
        // WebSocket Lifecycle Helpers
        // -----------------------------------------------------------------------

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

        private void OnRealtimeModeChanged(bool isRealtime)
        {
            RealtimeModeChanged?.Invoke(this, new RealtimeModeChangedEventArgs
            {
                IsRealtime = isRealtime,
            });
        }

        // -----------------------------------------------------------------------
        // HTTP Helpers (with rate limiting)
        // -----------------------------------------------------------------------

        // -----------------------------------------------------------------------
        // HTTP Helpers
        // -----------------------------------------------------------------------

        private async Task<string> GetStringAsync(string path)
        {
            try
            {
                await AcquireRateLimitTokenAsync().ConfigureAwait(false);
                var response = await _httpClient.GetAsync(_serverUrl + ApiPrefix + path).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "GET {0}{1}{2} failed", _serverUrl, ApiPrefix, path);
                SetConnected(false, ex.Message);
                return null;
            }
        }

        private async Task PutStringAsync(string path, string json)
        {
            try
            {
                await AcquireRateLimitTokenAsync().ConfigureAwait(false);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(_serverUrl + ApiPrefix + path, content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "PUT {0}{1}{2} failed", _serverUrl, ApiPrefix, path);
                SetConnected(false, ex.Message);
                throw;
            }
        }

        private async Task PostStringAsync(string path, string json)
        {
            try
            {
                await AcquireRateLimitTokenAsync().ConfigureAwait(false);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_serverUrl + ApiPrefix + path, content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "POST {0}{1}{2} failed", _serverUrl, ApiPrefix, path);
                SetConnected(false, ex.Message);
                throw;
            }
        }

        private void SetConnected(bool connected, string message)
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
    }
}
