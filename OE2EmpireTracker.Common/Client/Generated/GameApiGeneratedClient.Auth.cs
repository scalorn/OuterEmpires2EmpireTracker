// <copyright file="GameApiGeneratedClient.Auth.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Net.Http;
using System.Text;

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
        private string _bearerToken;

        /// <summary>
        /// Sets the bearer token to inject into all subsequent requests.
        /// </summary>
        /// <param name="token">The bearer access token.</param>
        public void SetBearerToken(string token)
        {
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
            }
        }
    }
}
