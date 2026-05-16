using System;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    public class ActivityRow
    {
        public ActivityType Type { get; set; }

        public string SystemName { get; set; }

        public string ColonyName { get; set; }

        public string SourceName { get; set; }

        public string ProcessDetails { get; set; }

        /// <summary>
        /// Reference to the CountDownTime for structure-based activities.
        /// Null for CommodityRequest rows.
        /// </summary>
        public CountDownTime CountDown { get; set; }

        /// <summary>
        /// For CommodityRequest rows: the NeedBy DateTime.
        /// For structure rows: DateTime.MinValue (unused).
        /// </summary>
        public DateTime NeedBy { get; set; }

        /// <summary>
        /// Formats seconds as "Xd Yh Zm Ws" matching CountDownTime.TimeRemainingString format.
        /// </summary>
        public static string FormatSeconds(long seconds)
        {
            if (seconds <= 0) return "0s";
            var ts = TimeSpan.FromSeconds(seconds);
            string result = string.Empty;
            bool started = false;

            if (ts.Days > 0)
            {
                result = $"{ts.Days}d";
                started = true;
            }

            if (started || ts.Hours > 0)
            {
                if (result.Length > 0) result += " ";
                result += $"{ts.Hours}h";
                started = true;
            }

            if (started || ts.Minutes > 0)
            {
                if (result.Length > 0) result += " ";
                result += $"{ts.Minutes}m";
                started = true;
            }

            if (started || ts.Seconds > 0)
            {
                if (result.Length > 0) result += " ";
                result += $"{ts.Seconds}s";
            }

            return result;
        }

        /// <summary>
        /// Returns the current seconds remaining for sorting and display.
        /// </summary>
        public long GetSecondsRemaining()
        {
            if (CountDown != null)
            {
                // Repeating timer with elapsed intervals: show 0 until background processor runs
                if (CountDown.IsRepeating && CountDown.IntervalsPassed > 0)
                    return 0;
                return Math.Max(0, CountDown.TimeRemaining);
            }

            long seconds = (long)(NeedBy - SystemClock.UtcNow).TotalSeconds;
            return Math.Max(0, seconds);
        }

        /// <summary>
        /// Returns the formatted time remaining string in "Xd Yh Zm Ws" format.
        /// </summary>
        public string GetTimeRemainingString()
        {
            if (CountDown != null)
            {
                // Repeating timer with elapsed intervals: show 0s until background processor runs
                if (CountDown.IsRepeating && CountDown.IntervalsPassed > 0)
                    return "0s";
                return CountDown.TimeRemainingString;
            }

            long seconds = GetSecondsRemaining();
            if (seconds <= 0) return "0s";
            return FormatSeconds(seconds);
        }
    }
}
