using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO carrying a single skill's state for profile create/update requests.
    /// </summary>
    public class SkillUpdateData
    {
        public int Level { get; set; }

        public bool TrainingStarted { get; set; }

        public DateTime CompletionStartTime { get; set; }

        public DateTime CompletionEndTime { get; set; }
    }
}
