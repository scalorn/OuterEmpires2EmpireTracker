// <copyright file="GameApiProfileResponse.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// DTO representing the player profile response from the game API.
    /// </summary>
    public class GameApiProfileResponse
    {
        /// <summary>
        /// Gets or sets the player UUID.
        /// </summary>
        [JsonProperty("uuid")]
        public string UUID { get; set; }

        /// <summary>
        /// Gets or sets the player name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the player faction.
        /// </summary>
        [JsonProperty("faction")]
        public string Faction { get; set; }

        /// <summary>
        /// Gets or sets the total skill points.
        /// </summary>
        [JsonProperty("skillPoints")]
        public int SkillPoints { get; set; }

        /// <summary>
        /// Gets or sets the citizen ID.
        /// </summary>
        [JsonProperty("citizenId")]
        public string CitizenId { get; set; }

        /// <summary>
        /// Gets or sets the registration date string.
        /// </summary>
        [JsonProperty("registrationDate")]
        public string RegistrationDate { get; set; }

        /// <summary>
        /// Gets or sets the skills dictionary keyed by skill name.
        /// </summary>
        [JsonProperty("skills")]
        public Dictionary<string, GameApiSkillResponse> Skills { get; set; }

        /// <summary>
        /// Gets or sets the player ranks.
        /// </summary>
        [JsonProperty("ranks")]
        public GameApiRanksResponse Ranks { get; set; }
    }

    /// <summary>
    /// DTO representing a single skill in the game API response.
    /// </summary>
    public class GameApiSkillResponse
    {
        /// <summary>
        /// Gets or sets the skill level.
        /// </summary>
        [JsonProperty("level")]
        public int Level { get; set; }

        /// <summary>
        /// Gets or sets the skill experience points.
        /// </summary>
        [JsonProperty("experience")]
        public long Experience { get; set; }
    }

    /// <summary>
    /// DTO representing the ranks section of the game API response.
    /// </summary>
    public class GameApiRanksResponse
    {
        /// <summary>
        /// Gets or sets the public rank.
        /// </summary>
        [JsonProperty("public")]
        public GameApiRankResponse Public { get; set; }

        /// <summary>
        /// Gets or sets the private rank.
        /// </summary>
        [JsonProperty("private")]
        public GameApiRankResponse Private { get; set; }

        /// <summary>
        /// Gets or sets the military rank.
        /// </summary>
        [JsonProperty("military")]
        public GameApiRankResponse Military { get; set; }
    }

    /// <summary>
    /// DTO representing a single rank in the game API response.
    /// </summary>
    public class GameApiRankResponse
    {
        /// <summary>
        /// Gets or sets the rank name/title.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the rank level.
        /// </summary>
        [JsonProperty("level")]
        public int Level { get; set; }
    }
}
