// <copyright file="GameApiTokenResponse.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// DTO representing the token response from the OE2 public API OAuth2 token exchange.
    /// </summary>
    public class GameApiTokenResponse
    {
        /// <summary>
        /// Gets or sets the JWT access token.
        /// </summary>
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the token type (typically "Bearer").
        /// </summary>
        [JsonProperty("tokenType")]
        public string TokenType { get; set; }

        /// <summary>
        /// Gets or sets the token lifetime in seconds.
        /// </summary>
        [JsonProperty("expiresIn")]
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Gets or sets the character ID associated with this token.
        /// </summary>
        [JsonProperty("characterId")]
        public int CharacterId { get; set; }

        /// <summary>
        /// Gets or sets the list of granted scopes.
        /// </summary>
        [JsonProperty("scopes")]
        public List<string> Scopes { get; set; }

        /// <summary>
        /// Gets or sets the subscription tier.
        /// </summary>
        [JsonProperty("subscription")]
        public string Subscription { get; set; }
    }

    /// <summary>
    /// Generic service response envelope from the OE2 public API.
    /// All API responses are wrapped in this structure.
    /// </summary>
    /// <typeparam name="T">The type of the data payload.</typeparam>
    public class GameApiServiceResponse<T>
    {
        /// <summary>
        /// Gets or sets a value indicating whether the request was successful.
        /// </summary>
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the return code.
        /// </summary>
        [JsonProperty("returnCode")]
        public int ReturnCode { get; set; }

        /// <summary>
        /// Gets or sets the return message string.
        /// </summary>
        [JsonProperty("returnString")]
        public string ReturnString { get; set; }

        /// <summary>
        /// Gets or sets the data payload.
        /// </summary>
        [JsonProperty("data")]
        public T Data { get; set; }
    }

    /// <summary>
    /// Cached token entry with expiration tracking.
    /// </summary>
    internal class CachedToken
    {
        /// <summary>
        /// Gets or sets the token response data.
        /// </summary>
        public GameApiTokenResponse Token { get; set; }

        /// <summary>
        /// Gets or sets the UTC time when the token was obtained.
        /// </summary>
        public DateTime ObtainedAtUtc { get; set; }

        /// <summary>
        /// Gets a value indicating whether the token is expired or about to expire (60s buffer).
        /// </summary>
        public bool IsExpired
        {
            get
            {
                if (Token == null)
                {
                    return true;
                }

                DateTime expiresAt = ObtainedAtUtc.AddSeconds(Token.ExpiresIn - 60);
                return SystemClock.UtcNow >= expiresAt;
            }
        }
    }
}
