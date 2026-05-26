// <copyright file="GameApiSkillInTrainingResponse.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// DTO representing the skill currently in training from the game API.
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
