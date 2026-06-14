// <copyright file="GameApiGeneratedClient.Auth.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Net.Http;
using NLog;

namespace OE2EmpireTracker.Common.Client.Generated
{
    /// <summary>
    /// Partial class extension that injects the Authorization bearer token
    /// into every outgoing request via the NSwag PrepareRequest hook.
    /// Required because .NET Framework 4.8.1 does not propagate
    /// DefaultRequestHeaders to manually-created HttpRequestMessage objects.
    /// </summary>
    public partial class GameApiGeneratedClient
    {
        private static readonly Logger AuthLog = LogManager.GetCurrentClassLogger();

        private string _bearerToken;

        /// <summary>
        /// Sets the bearer token to inject into all subsequent requests.
        /// </summary>
        /// <param name="token">The bearer access token.</param>
        public void SetBearerToken(string token)
        {
            AuthLog.Debug("SetBearerToken called, token length={0}", token?.Length ?? 0);
            _bearerToken = token;
        }

        /// <summary>
        /// Injects the Authorization header into each outgoing request.
        /// </summary>
        partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url)
        {
            if (!string.IsNullOrEmpty(_bearerToken))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _bearerToken);
                AuthLog.Debug("PrepareRequest: injected Bearer token (len={0}) for {1}", _bearerToken.Length, url);
            }
            else
            {
                AuthLog.Warn("PrepareRequest: NO bearer token set for {0}", url);
            }
        }
    }
}
