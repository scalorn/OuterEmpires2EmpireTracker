using System;
using System.Threading;
using System.Threading.Tasks;
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
        /// Replaceable delay function. Production: real Task.Delay. Tests: instant advance.
        /// Signature: (milliseconds, cancellationToken) => Task.
        /// </summary>
        public static Func<int, CancellationToken, Task> DelayFunc { get; set; } = Task.Delay;

        /// <summary>
        /// Returns the current UTC time via UtcNowFunc.
        /// </summary>
        public static DateTime UtcNow => UtcNowFunc();

        /// <summary>
        /// Delays for the specified milliseconds using the replaceable DelayFunc.
        /// Production code should call this instead of Task.Delay to enable test acceleration.
        /// </summary>
        /// <param name="milliseconds">The number of milliseconds to delay.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task representing the delay.</returns>
        public static Task DelayAsync(int milliseconds, CancellationToken ct)
        {
            return DelayFunc(milliseconds, ct);
        }

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
        /// Configures instant-advance mode for tests. DelayAsync will advance the frozen clock
        /// by the delay amount and return immediately (no real wall-clock wait).
        /// Call FreezeAt first, then EnableInstantDelay.
        /// </summary>
        public static void EnableInstantDelay()
        {
            DelayFunc = (ms, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                AdvanceBy(TimeSpan.FromMilliseconds(ms));
                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// Resets UtcNowFunc to the real clock and DelayFunc to real Task.Delay. Call in test teardown.
        /// </summary>
        public static void Reset()
        {
            UtcNowFunc = () => DateTime.UtcNow;
            DelayFunc = Task.Delay;
        }
    }
}
