using System.Threading.Tasks;
using NUnit.Framework;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Tests
{
    /// <summary>
    /// Runs once before any test in the assembly.
    /// Suppresses MessageBox dialogs from MigrationRunner during tests and removes
    /// wall-clock waits from Polly's retry policies.
    /// </summary>
    [SetUpFixture]
    public class GlobalSetup
    {
        [OneTimeSetUp]
        public void AssemblyInit()
        {
            MigrationRunner.SuppressUI = true;

            // GameApiTypedClient's retry policy waits 1s, 2s then 4s between attempts, so
            // every call against a dead endpoint costs seven seconds of real sleeping. The
            // scheduler property tests drive thousands of such calls, which put the suite
            // into the hours. Polly exposes its clock for exactly this reason: neutering
            // the sleep keeps attempt counts, exception handling and circuit-breaker
            // transitions intact while removing the waiting. UtcNow is deliberately left
            // alone so break durations still measure real elapsed time.
            //
            // This mirrors OE2EmpireTracker.Services.SystemClock.EnableInstantDelay(),
            // which does the same job for the request queue's own backoff.
            Polly.Utilities.SystemClock.SleepAsync = (duration, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(0);
            };

            Polly.Utilities.SystemClock.Sleep = (duration, ct) =>
            {
                ct.ThrowIfCancellationRequested();
            };
        }

        [OneTimeTearDown]
        public void AssemblyCleanup()
        {
            Polly.Utilities.SystemClock.Reset();
        }
    }
}
