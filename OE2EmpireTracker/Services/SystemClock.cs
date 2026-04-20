using System;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Ambient clock abstraction. Production code uses SystemClock.UtcNow instead of DateTime.UtcNow.
    /// Tests can replace UtcNowFunc with a frozen or manually-advanced clock.
    /// </summary>
    public static class SystemClock
    {
        /// <summary>
        /// Replaceable function that returns the current UTC time.
        /// Default: DateTime.UtcNow. Tests override this to freeze or control time.
        /// </summary>
        public static Func<DateTime> UtcNowFunc = () => DateTime.UtcNow;

        /// <summary>
        /// Returns the current UTC time via UtcNowFunc.
        /// </summary>
        public static DateTime UtcNow => UtcNowFunc();

        /// <summary>
        /// Resets UtcNowFunc to the real clock. Call in test teardown.
        /// </summary>
        public static void Reset() => UtcNowFunc = () => DateTime.UtcNow;
    }
}
