// <copyright file="LegacyResponseModels.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// DTO representing the token response from the OE2 public API OAuth2 token exchange.
    /// Retained in test project for backward-compatible test code until task 10.x migrates tests.
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
    /// Retained in test project for backward-compatible test code until task 10.x migrates tests.
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
    /// DTO representing the skill currently in training from the game API.
    /// Retained in test project for backward-compatible test code until task 10.x migrates tests.
    /// </summary>
    public class GameApiSkillInTrainingResponse
    {
        /// <summary>
        /// Gets or sets the name of the skill being trained.
        /// </summary>
        [JsonProperty("skillName")]
        public string SkillName { get; set; }

        /// <summary>
        /// Gets or sets the target level for training.
        /// </summary>
        [JsonProperty("targetLevel")]
        public int TargetLevel { get; set; }

        /// <summary>
        /// Gets or sets the training percentage complete.
        /// </summary>
        [JsonProperty("trainingPercentageComplete")]
        public int TrainingPercentageComplete { get; set; }

        /// <summary>
        /// Gets or sets the remaining minutes until training completes.
        /// </summary>
        [JsonProperty("remainingMinutes")]
        public int RemainingMinutes { get; set; }
    }
}
