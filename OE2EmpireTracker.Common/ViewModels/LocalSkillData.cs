using System;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.ViewModels
{
    /// <summary>
    /// Local copy of a single skill's state in the edit buffer.
    /// Disconnected from the PlayerSkill entity.
    /// </summary>
    public class LocalSkillData
    {
        public int Level { get; set; }

        public bool TrainingStarted { get; set; }

        public DateTime CompletionStartTime { get; set; }

        public DateTime CompletionEndTime { get; set; }

        /// <summary>
        /// Gets the computed seconds remaining until CompletionEndTime.
        /// </summary>
        public long TimeRemaining => Math.Max(0, (long)(CompletionEndTime - SystemClock.UtcNow).TotalSeconds);

        /// <summary>
        /// Gets the human-readable countdown string (e.g. "2d 5h 30m 10s").
        /// </summary>
        public string TimeRemainingString
        {
            get
            {
                long seconds = TimeRemaining;
                if (seconds <= 0)
                {
                    return "0s";
                }

                var ts = TimeSpan.FromSeconds(seconds);
                string result = string.Empty;
                bool started = false;

                if (ts.Days > 0)
                {
                    result = string.Format("{0}d", ts.Days);
                    started = true;
                }

                if (started || ts.Hours > 0)
                {
                    if (result.Length > 0)
                    {
                        result += " ";
                    }

                    result += string.Format("{0}h", ts.Hours);
                    started = true;
                }

                if (started || ts.Minutes > 0)
                {
                    if (result.Length > 0)
                    {
                        result += " ";
                    }

                    result += string.Format("{0}m", ts.Minutes);
                    started = true;
                }

                if (started || ts.Seconds > 0)
                {
                    if (result.Length > 0)
                    {
                        result += " ";
                    }

                    result += string.Format("{0}s", ts.Seconds);
                }

                return result;
            }
        }
    }
}