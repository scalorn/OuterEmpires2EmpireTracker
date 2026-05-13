// <copyright file="RemoteFactionClient.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// HTTP client that communicates with the Remote Faction Service.
    /// Handles bearer-token auth (via SecureString) and optional self-signed certificate pinning.
    /// Implements <see cref="IDisposable"/> to securely dispose the bearer token.
    /// </summary>
    public class RemoteFactionClient : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly string _serverUrl;
        private readonly SecureString _bearerToken;
        private readonly string _trustedThumbprint;

        private HttpClient _httpClient;
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
            InitializeHttpClient();
        }

        /// <summary>
        /// Occurs when the connection status changes.
        /// </summary>
        public event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;

        /// <summary>
        /// Gets a value indicating whether the client is currently connected.
        /// </summary>
        public bool IsConnected { get; private set; }

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
        /// Disconnects from the remote server.
        /// </summary>
        public void Disconnect()
        {
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
        /// Disposes the SecureString bearer token and the underlying HttpClient.
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
                    _bearerToken?.Dispose();
                    _httpClient?.Dispose();
                }

                _disposed = true;
            }
        }

        private void InitializeHttpClient()
        {
            var handler = new HttpClientHandler();

            if (!string.IsNullOrEmpty(_trustedThumbprint))
            {
                handler.ServerCertificateCustomValidationCallback = ValidateCertificate;
            }

            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Set the Authorization header using the SecureString token.
            // The plain text is only in memory briefly during this call.
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
            bool match = string.Equals(thumbprint, _trustedThumbprint, StringComparison.OrdinalIgnoreCase);
            if (!match)
            {
                Log.Warn("Certificate thumbprint mismatch. Expected={0}, Actual={1}", _trustedThumbprint, thumbprint);
            }

            return match;
        }

        private async Task<string> GetStringAsync(string path)
        {
            try
            {
                var response = await _httpClient.GetAsync(_serverUrl + path).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "GET {0}{1} failed", _serverUrl, path);
                SetConnected(false, ex.Message);
                return null;
            }
        }

        private async Task PutStringAsync(string path, string json)
        {
            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(_serverUrl + path, content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                Log.Warn(ex, "PUT {0}{1} failed", _serverUrl, path);
                SetConnected(false, ex.Message);
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
