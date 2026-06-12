// <copyright file="TokenRefreshHandler.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using OE2EmpireTracker.Client;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Result of a token refresh attempt.
    /// </summary>
    public class TokenRefreshResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the token refresh succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the new access token (null if refresh failed).
        /// </summary>
        public string NewToken { get; set; }
    }

    /// <summary>
    /// Serializes token refresh attempts so that only one 401 → refresh exchange
    /// occurs at a time. Concurrent 401 responses wait for the in-progress refresh
    /// and reuse the updated token.
    /// </summary>
    public class TokenRefreshHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);
        private readonly GameApiClient _apiClient;
        private readonly GameApiConnectionSettings _settings;
        private readonly GameApiCredentialManager _credentialManager;
        private readonly string _playerUUID;

        private string _currentAccessToken;
        private int _tokenVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenRefreshHandler"/> class.
        /// </summary>
        /// <param name="apiClient">The game API client used for token exchange.</param>
        /// <param name="settings">The connection settings containing AppId and ClientId.</param>
        /// <param name="credentialManager">The credential manager for retrieving the character secret.</param>
        /// <param name="playerUUID">The player UUID whose credentials are used for refresh.</param>
        /// <param name="initialAccessToken">The initial access token to use.</param>
        public TokenRefreshHandler(
            GameApiClient apiClient,
            GameApiConnectionSettings settings,
            GameApiCredentialManager credentialManager,
            string playerUUID,
            string initialAccessToken)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _credentialManager = credentialManager ?? throw new ArgumentNullException(nameof(credentialManager));
            _playerUUID = playerUUID ?? string.Empty;
            _currentAccessToken = initialAccessToken ?? string.Empty;
        }

        /// <summary>
        /// Gets the current access token.
        /// </summary>
        public string CurrentAccessToken => _currentAccessToken;

        /// <summary>
        /// Gets the current token version (incremented on each successful refresh).
        /// </summary>
        internal int TokenVersion => _tokenVersion;

        /// <summary>
        /// Handles an HTTP 401 unauthorized response by attempting to refresh the access token.
        /// Uses a semaphore to serialize refresh attempts so only one exchange occurs at a time.
        /// If the token was already refreshed by another caller (stale-check optimization),
        /// returns success immediately with the current token.
        /// </summary>
        /// <param name="failedToken">The access token that produced the 401 response.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A <see cref="TokenRefreshResult"/> indicating success and the new token.</returns>
        public async Task<TokenRefreshResult> HandleUnauthorizedAsync(string failedToken, CancellationToken ct)
        {
            await _refreshLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                // Stale-check: if the token was already refreshed by another waiter,
                // the failed token won't match the current token anymore.
                if (!string.Equals(failedToken, _currentAccessToken, StringComparison.Ordinal))
                {
                    Log.Debug(
                        "Token already refreshed by another caller (version {0}), returning current token",
                        _tokenVersion);
                    return new TokenRefreshResult { Success = true, NewToken = _currentAccessToken };
                }

                Log.Info("Attempting token refresh (version {0})", _tokenVersion);

                string secret = GetDecryptedSecret();
                if (string.IsNullOrEmpty(secret))
                {
                    Log.Error("Cannot refresh token: no secret available for player {0}", _playerUUID);
                    return new TokenRefreshResult { Success = false, NewToken = null };
                }

                // Invalidate the GameApiClient token cache before refreshing.
                // Without this, ExchangeTokenAsync may return the same expired token
                // from its internal cache if the client-side expiry buffer hasn't elapsed yet.
                _apiClient.InvalidateToken(_settings.ClientId, secret);

                var tokenResult = await _apiClient.ExchangeTokenAsync(
                    _settings.AppId,
                    _settings.ClientId,
                    secret).ConfigureAwait(false);

                if (tokenResult.Success)
                {
                    _currentAccessToken = tokenResult.Token.AccessToken;
                    _tokenVersion++;
                    Log.Info(
                        "Token refresh succeeded (version now {0})",
                        _tokenVersion);
                    return new TokenRefreshResult { Success = true, NewToken = _currentAccessToken };
                }

                Log.Error(
                    "Token refresh failed: {0}",
                    tokenResult.ErrorMessage ?? "unknown error");
                return new TokenRefreshResult { Success = false, NewToken = null };
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        /// <summary>
        /// Retrieves and decrypts the secret for the current player from the credential manager.
        /// </summary>
        /// <returns>The plain-text secret, or null if unavailable.</returns>
        private string GetDecryptedSecret()
        {
            SecureString secureSecret = _credentialManager.GetKey(_playerUUID);
            if (secureSecret == null)
            {
                return null;
            }

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.SecureStringToGlobalAllocUnicode(secureSecret);
                return Marshal.PtrToStringUni(ptr);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                }

                secureSecret.Dispose();
            }
        }
    }
}
