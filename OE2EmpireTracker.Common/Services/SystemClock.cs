using System;
using NLog;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Ambient clock abstraction. Production code uses SystemClock.UtcNow instead of DateTime.UtcNow.
    /// Tests can replace UtcNowFunc with a frozen or manually-advanced clock.
    /// </summary>
    public static class SystemClock
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Replaceable function that returns the current UTC time.
        /// Default: DateTime.UtcNow. Tests override this to freeze or control time.
        /// </summary>
        public static Func<DateTime> UtcNowFunc { get; set; } = () => DateTime.UtcNow;

        /// <summary>
        /// Returns the current UTC time via UtcNowFunc.
        /// </summary>
        public static DateTime UtcNow => UtcNowFunc();

        /// <summary>
        /// Freezes the clock at the specified UTC time. Subsequent calls to UtcNow return this value
        /// until AdvanceBy is called or the clock is reset.
        /// </summary>
        /// <param name="utcTime">The UTC time to freeze at.</param>
        public static void FreezeAt(DateTime utcTime)
        {
            var frozen = utcTime;
            UtcNowFunc = () => frozen;
        }

        /// <summary>
        /// Advances the frozen clock by the specified duration.
        /// Must be called after FreezeAt.
        /// </summary>
        /// <param name="duration">The time span to advance by.</param>
        public static void AdvanceBy(TimeSpan duration)
        {
            var current = UtcNowFunc();
            var advanced = current + duration;
            UtcNowFunc = () => advanced;
        }

        /// <summary>
        /// Resets UtcNowFunc to the real clock. Call in test teardown.
        /// </summary>
        public static void Reset() => UtcNowFunc = () => DateTime.UtcNow;
    }
}
