using System;
using System.Text.RegularExpressions;
using NLog;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Represents a countdown timer that can be used as a one-shot expiration timer or as a repeating interval timer.
    /// </summary>
    public class CountDownTime
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="CountDownTime"/> class.
        /// </summary>
        public CountDownTime()
        {
            StartTime = DateTime.MinValue;
            EndTime = StartTime;
        }

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
                    return (long)(GetNextIntervalBoundary(SystemClock.UtcNow) - SystemClock.UtcNow).TotalSeconds;
                }

                return (long)(EndTime - SystemClock.UtcNow).TotalSeconds;
            }

            set
            {
                var now = SystemClock.UtcNow;
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

                    var oldStart = StartTime;
                    StartTime = now.AddSeconds(remaining - RepeatIntervalSeconds);
                    EndTime = now.AddSeconds(remaining);
                    Log.Debug(
                        "TimeRemaining.set(repeating): value={0} remaining={1} interval={2}s oldStart={3:O} newStart={4:O} end={5:O}",
                        value,
                        remaining,
                        RepeatIntervalSeconds,
                        oldStart,
                        StartTime,
                        EndTime);
                    return;
                }

                StartTime = now;
                EndTime = now.AddSeconds(value);
                Log.Debug(
                    "TimeRemaining.set(oneshot): value={0} start={1:O} end={2:O}",
                    value,
                    StartTime,
                    EndTime);
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

                var now = SystemClock.UtcNow;
                var elapsedSeconds = (now - StartTime).TotalSeconds;
                if (elapsedSeconds <= 0)
                {
                    return 0;
                }

                long intervals = (long)Math.Floor(elapsedSeconds / RepeatIntervalSeconds);
                if (intervals > 1)
                {
                    Log.Debug(
                        "IntervalsPassed: {0} intervals (elapsed={1:F1}s start={2:O} now={3:O} interval={4}s)",
                        intervals,
                        elapsedSeconds,
                        StartTime,
                        now,
                        RepeatIntervalSeconds);
                }

                return intervals;
            }
        }

        /// <summary>
        /// Gets or sets the remaining countdown time as a human-readable string
        /// in the format "Xd Yh Zm Ws".
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
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

                long totalSeconds = ((((long)days * 24) + hours) * 60 * 60) + (minutes * 60) + seconds;
                Log.Debug("TimeRemainingString.set: input='{0}' parsed={1}s", value, totalSeconds);
                TimeRemaining = totalSeconds;
            }
        }

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

            var oldStart = StartTime;
            StartTime = StartTime.AddSeconds(toConsume * RepeatIntervalSeconds);
            EndTime = GetNextIntervalBoundary(SystemClock.UtcNow);
            Log.Debug(
                "ConsumeIntervals: requested={0} passed={1} consumed={2} oldStart={3:O} newStart={4:O} end={5:O}",
                intervalCount,
                passed,
                toConsume,
                oldStart,
                StartTime,
                EndTime);
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
            StartTime = SystemClock.UtcNow;
            EndTime = StartTime.AddSeconds(intervalSeconds);
            Log.Debug(
                "StartRepeating: interval={0}s start={1:O} end={2:O}",
                intervalSeconds,
                StartTime,
                EndTime);
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

            StartTime = SystemClock.UtcNow.AddSeconds(remaining - RepeatIntervalSeconds);
            EndTime = SystemClock.UtcNow.AddSeconds(remaining);
            Log.Debug(
                "StartRepeating(offset): interval={0}s secondsUntilNext={1} remaining={2} start={3:O} end={4:O}",
                intervalSeconds,
                secondsUntilNextInterval,
                remaining,
                StartTime,
                EndTime);
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