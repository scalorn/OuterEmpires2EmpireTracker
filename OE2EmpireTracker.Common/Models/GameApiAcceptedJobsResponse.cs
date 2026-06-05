// <copyright file="GameApiAcceptedJobsResponse.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// DTO representing the accepted jobs response from the game API.
    /// </summary>
    public class GameApiAcceptedJobsResponse
    {
        /// <summary>
        /// Gets or sets the list of accepted jobs.
        /// </summary>
        [JsonProperty("jobs")]
        public List<GameApiAcceptedJob> Jobs { get; set; } = new List<GameApiAcceptedJob>();
    }

    /// <summary>
    /// DTO representing a single accepted job from the game API.
    /// </summary>
    public class GameApiAcceptedJob
    {
        /// <summary>
        /// Gets or sets the job identifier.
        /// </summary>
        [JsonProperty("jobId")]
        public int JobId { get; set; }

        /// <summary>
        /// Gets or sets the job name.
        /// </summary>
        [JsonProperty("jobName")]
        public string JobName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the job type.
        /// </summary>
        [JsonProperty("jobType")]
        public string JobType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the job track.
        /// </summary>
        [JsonProperty("track")]
        public string Track { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the job detail description.
        /// </summary>
        [JsonProperty("detail")]
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the experience points reward.
        /// </summary>
        [JsonProperty("xp")]
        public int Xp { get; set; }

        /// <summary>
        /// Gets or sets the credit reward.
        /// </summary>
        [JsonProperty("credits")]
        public double Credits { get; set; }

        /// <summary>
        /// Gets or sets the bonus amount.
        /// </summary>
        [JsonProperty("bonus")]
        public int Bonus { get; set; }

        /// <summary>
        /// Gets or sets the deadline for completing the job to receive the bonus.
        /// </summary>
        [JsonProperty("completeByBonus")]
        public DateTime? CompleteByBonus { get; set; }

        /// <summary>
        /// Gets or sets the character identifier.
        /// </summary>
        [JsonProperty("characterId")]
        public int CharacterId { get; set; }

        /// <summary>
        /// Gets or sets the character name.
        /// </summary>
        [JsonProperty("charName")]
        public string CharName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the first info identifier.
        /// </summary>
        [JsonProperty("info1")]
        public int Info1 { get; set; }

        /// <summary>
        /// Gets or sets the first system name.
        /// </summary>
        [JsonProperty("systemName1")]
        public string SystemName1 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the second info identifier.
        /// </summary>
        [JsonProperty("info2")]
        public int Info2 { get; set; }

        /// <summary>
        /// Gets or sets the second system name.
        /// </summary>
        [JsonProperty("systemName2")]
        public string SystemName2 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the location identifier where the job was issued.
        /// </summary>
        [JsonProperty("jobIssuedLocId")]
        public int JobIssuedLocId { get; set; }

        /// <summary>
        /// Gets or sets the name of the location where the job was issued.
        /// </summary>
        [JsonProperty("jobIssuedLocationName")]
        public string JobIssuedLocationName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the reputation of the location where the job was issued.
        /// </summary>
        [JsonProperty("jobIssuedLocationReputation")]
        public int JobIssuedLocationReputation { get; set; }
    }
}
