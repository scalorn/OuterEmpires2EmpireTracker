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
        /// Gets or sets the character ID.
        /// </summary>
        [JsonProperty("characterId")]
        public int CharacterId { get; set; }

        /// <summary>
        /// Gets or sets the character's first name.
        /// </summary>
        [JsonProperty("firstName")]
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the character's last name.
        /// </summary>
        [JsonProperty("lastName")]
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the active time in minutes.
        /// </summary>
        [JsonProperty("activeTimeMinutes")]
        public int ActiveTimeMinutes { get; set; }

        /// <summary>
        /// Gets or sets the skill currently in training.
        /// </summary>
        [JsonProperty("skillInTraining")]
        public GameApiSkillInTrainingResponse SkillInTraining { get; set; }

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

        /// <summary>
        /// Gets or sets the skill ID.
        /// </summary>
        [JsonProperty("skillId")]
        public int SkillId { get; set; }

        /// <summary>
        /// Gets or sets the effect description.
        /// </summary>
        [JsonProperty("effectDescription")]
        public string EffectDescription { get; set; }

        /// <summary>
        /// Gets or sets the amount per level.
        /// </summary>
        [JsonProperty("amountPerLevel")]
        public int AmountPerLevel { get; set; }

        /// <summary>
        /// Gets or sets the skill group name.
        /// </summary>
        [JsonProperty("skillGroupName")]
        public string SkillGroupName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the skill is unlocked.
        /// </summary>
        [JsonProperty("isUnlocked")]
        public bool IsUnlocked { get; set; }
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

        /// <summary>
        /// Gets or sets the level name (rank title from the API).
        /// </summary>
        [JsonProperty("levelName")]
        public string LevelName { get; set; }

        /// <summary>
        /// Gets or sets the XP required to reach the next level.
        /// </summary>
        [JsonProperty("xpToNextLevel")]
        public long XpToNextLevel { get; set; }

        /// <summary>
        /// Gets or sets the current XP for this rank.
        /// </summary>
        [JsonProperty("currentXp")]
        public long CurrentXp { get; set; }
    }
}
