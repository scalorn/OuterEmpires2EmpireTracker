using System;
using System.Text.RegularExpressions;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Represents a countdown timer that can be used as a one-shot expiration timer or as a repeating interval timer.
    /// </summary>
    public class CountDownTime
    {
        /// <summary>
        /// The point in time the countdown was last started or updated.
        /// For repeating timers this is the baseline used to calculate how many intervals have elapsed.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The current target time for the countdown.
        /// For non-repeating timers this is the final expiration time.
        /// For repeating timers this is the anchor used to calculate the current interval end.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// The repeating interval length in seconds. If this value is greater than zero,
        /// the countdown functions as a repeating timer.
        /// </summary>
        public long RepeatIntervalSeconds { get; set; }

        /// <summary>
        /// Returns true when the countdown is configured to repeat.
        /// </summary>
        public bool IsRepeating => RepeatIntervalSeconds > 0;

        /// <summary>
        /// Gets or sets the number of seconds remaining until the next expiration.
        /// For repeating timers this returns the remaining seconds until the next interval boundary.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public long TimeRemaining
        {
            get
            {
                if (IsRepeating && RepeatIntervalSeconds > 0)
                {
                    return (long)(GetNextIntervalBoundary(DateTime.Now) - DateTime.Now).TotalSeconds;
                }

                return (long)(EndTime - DateTime.Now).TotalSeconds;
            }
            set
            {
                var now = DateTime.Now;
                if (IsRepeating && RepeatIntervalSeconds > 0)
                {
                    long remaining = value;
                    if (remaining < 0)
                    {
                        remaining = 0;
                    }

                    if (remaining > RepeatIntervalSeconds)
                    {
                        long modulo = remaining % RepeatIntervalSeconds;
                        remaining = modulo == 0 ? RepeatIntervalSeconds : modulo;
                    }

                    StartTime = now.AddSeconds(remaining - RepeatIntervalSeconds);
                    EndTime = now.AddSeconds(remaining);
                    return;
                }

                StartTime = now;
                EndTime = now.AddSeconds(value);
            }
        }

        /// <summary>
        /// Returns how many full repeat intervals have elapsed since the last StartTime.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public long IntervalsPassed
        {
            get
            {
                if (!IsRepeating || RepeatIntervalSeconds <= 0 || StartTime == DateTime.MinValue)
                {
                    return 0;
                }

                var elapsedSeconds = (DateTime.Now - StartTime).TotalSeconds;
                if (elapsedSeconds <= 0)
                {
                    return 0;
                }

                return (long)Math.Floor(elapsedSeconds / RepeatIntervalSeconds);
            }
        }

        /// <summary>
        /// Gets or sets the remaining countdown time as a human-readable string
        /// in the format "Xd Yh Zm Ws".
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

                var timeSpan = TimeSpan.FromSeconds(seconds);
                string timeString = string.Empty;
                bool started = false;

                if (timeSpan.Days > 0)
                {
                    timeString = $"{timeSpan.Days}d";
                    started = true;
                }

                if (started || timeSpan.Hours > 0)
                {
                    if (timeString.Length > 0) timeString += " ";
                    timeString += $"{timeSpan.Hours}h";
                    started = true;
                }

                if (started || timeSpan.Minutes > 0)
                {
                    if (timeString.Length > 0) timeString += " ";
                    timeString += $"{timeSpan.Minutes}m";
                    started = true;
                }

                if (started || timeSpan.Seconds > 0)
                {
                    if (timeString.Length > 0) timeString += " ";
                    timeString += $"{timeSpan.Seconds}s";
                }

                return timeString;
            }
            set
            {
                var match = Regex.Match(value, @"(?:(\d+)d\s*)?(?:(\d+)h\s*)?(?:(\d+)m\s*)?(?:(\d+)s)?");

                int days = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
                int hours = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
                int minutes = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
                int seconds = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;

                long totalSeconds = ((long)days * 24 + hours) * 60 * 60 + minutes * 60 + seconds;
                TimeRemaining = totalSeconds;
            }
        }

        /// <summary>
        /// Advances the countdown by the specified number of intervals.
        /// For repeating timers, this updates StartTime so that the remaining passed
        /// intervals are reduced by the consumed amount.
        /// </summary>
        /// <param name="intervalCount">The number of intervals to consume.</param>
        public void ConsumeIntervals(long intervalCount)
        {
            if (!IsRepeating || RepeatIntervalSeconds <= 0 || intervalCount <= 0 || StartTime == DateTime.MinValue)
            {
                return;
            }

            long passed = IntervalsPassed;
            long toConsume = Math.Min(intervalCount, passed);
            if (toConsume <= 0)
            {
                return;
            }

            StartTime = StartTime.AddSeconds(toConsume * RepeatIntervalSeconds);
            EndTime = GetNextIntervalBoundary(DateTime.Now);
        }

        /// <summary>
        /// Starts repeating mode using the configured interval and resets the countdown.
        /// </summary>
        /// <param name="intervalSeconds">Interval length in seconds.</param>
        public void StartRepeating(long intervalSeconds)
        {
            if (intervalSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalSeconds), "Repeat interval must be greater than zero.");
            }

            RepeatIntervalSeconds = intervalSeconds;
            StartTime = DateTime.Now;
            EndTime = StartTime.AddSeconds(intervalSeconds);
        }

        /// <summary>
        /// Starts repeating mode with a specific time remaining until the next interval.
        /// </summary>
        /// <param name="intervalSeconds">Interval length in seconds.</param>
        /// <param name="secondsUntilNextInterval">Seconds remaining until the next interval boundary.</param>
        public void StartRepeating(long intervalSeconds, long secondsUntilNextInterval)
        {
            if (intervalSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalSeconds), "Repeat interval must be greater than zero.");
            }

            RepeatIntervalSeconds = intervalSeconds;
            long remaining = secondsUntilNextInterval;
            if (remaining < 0)
            {
                remaining = 0;
            }

            if (remaining >= RepeatIntervalSeconds)
            {
                remaining %= RepeatIntervalSeconds;
            }

            StartTime = DateTime.Now.AddSeconds(remaining - RepeatIntervalSeconds);
            EndTime = DateTime.Now.AddSeconds(remaining);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CountDownTime"/> class.
        /// </summary>
        public CountDownTime()
        {
            StartTime = DateTime.MinValue;
            EndTime = StartTime;
        }

        private DateTime GetNextIntervalBoundary(DateTime now)
        {
            if (!IsRepeating || RepeatIntervalSeconds <= 0)
            {
                return EndTime;
            }

            if (StartTime == DateTime.MinValue)
            {
                StartTime = now;
            }

            var elapsedSeconds = (now - StartTime).TotalSeconds;
            if (elapsedSeconds < 0)
            {
                elapsedSeconds = 0;
            }

            long completedIntervals = (long)Math.Floor(elapsedSeconds / RepeatIntervalSeconds);
            return StartTime.AddSeconds((completedIntervals + 1) * RepeatIntervalSeconds);
        }
    }
}
