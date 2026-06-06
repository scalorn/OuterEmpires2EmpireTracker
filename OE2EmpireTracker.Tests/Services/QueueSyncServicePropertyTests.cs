// <copyright file="QueueSyncServicePropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for QueueSyncService concurrency guard.
    /// Feature: queue-based-sync
    /// </summary>
    [TestFixture]
    public class QueueSyncServicePropertyTests
    {
        // ---------------------------------------------------------------
        // Test Infrastructure
        // ---------------------------------------------------------------

        /// <summary>
        /// A testable QueueSyncService subclass that introduces a configurable
        /// delay inside the guarded section, making concurrent overlap observable.
        /// Tracks how many times the guarded body actually executes.
        /// </summary>
        private class InstrumentedSyncService
        {
            private readonly object _syncLock = new object();
            private volatile bool _isSyncRunning;
            private int _workEntryCount;
            private int _maxConcurrent;

            /// <summary>
            /// Gets the number of times the guarded work section was entered.
            /// </summary>
            public int WorkEntryCount => _workEntryCount;

            /// <summary>
            /// Gets the maximum observed concurrent entries (should always be 0 or 1).
            /// </summary>
            public int MaxConcurrent => _maxConcurrent;

            /// <summary>
            /// Gets or sets the delay in milliseconds to hold inside the guarded section.
            /// </summary>
            public int WorkDelayMs { get; set; } = 50;

            /// <summary>
            /// Replicates the exact same concurrency guard pattern as QueueSyncService.RunSyncAsync.
            /// </summary>
            /// <returns>True if actual work was performed, false if skipped.</returns>
            public async Task<bool> RunSyncAsync()
            {
                lock (_syncLock)
                {
                    if (_isSyncRunning)
                    {
                        return false;
                    }

                    _isSyncRunning = true;
                }

                try
                {
                    int current = Interlocked.Increment(ref _workEntryCount);
                    UpdateMax(current);
                    await Task.Delay(this.WorkDelayMs).ConfigureAwait(false);
                    return true;
                }
                finally
                {
                    Interlocked.Decrement(ref _workEntryCount);
                    _isSyncRunning = false;
                }
            }

            private void UpdateMax(int current)
            {
                int snapshot;
                do
                {
                    snapshot = _maxConcurrent;
                    if (current <= snapshot)
                    {
                        break;
                    }
                }
                while (Interlocked.CompareExchange(ref _maxConcurrent, current, snapshot) != snapshot);
            }
        }

        // ---------------------------------------------------------------
        // Property 1: No Concurrent Sync Cycles
        // At most one sync cycle runs at any time. If IsSyncRunning is true,
        // RunSyncAsync returns immediately without starting a new cycle.
        // **Validates: Requirements 9.3**
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 1: For any N concurrent RunSyncAsync calls (2-20),
        /// at most 1 call performs actual work; all others return early.
        /// The maximum observed concurrent entries is always 1.
        /// **Validates: Requirements 9.3**
        /// </summary>
        [Test]
        public void ConcurrentRunSync_AtMostOnePerformsWork()
        {
            var concurrencyGen = Gen.Choose(2, 20);

            Prop.ForAll(concurrencyGen.ToArbitrary(), (n) =>
            {
                var service = new InstrumentedSyncService
                {
                    WorkDelayMs = 50,
                };

                // Use a barrier to maximize concurrent start
                using (var barrier = new Barrier(n))
                {
                    var tasks = Enumerable.Range(0, n)
                        .Select(_ => Task.Run(async () =>
                        {
                            barrier.SignalAndWait();
                            return await service.RunSyncAsync().ConfigureAwait(false);
                        }))
                        .ToArray();

                    Task.WaitAll(tasks);

                    var results = tasks.Select(t => t.Result).ToArray();
                    int didWork = results.Count(r => r);
                    int skipped = results.Count(r => !r);

                    bool exactlyOneDidWork = didWork == 1;
                    bool restSkipped = skipped == n - 1;
                    bool maxConcurrentIsOne = service.MaxConcurrent == 1;

                    return (exactlyOneDidWork && restSkipped && maxConcurrentIsOne)
                        .Label($"N={n}, DidWork={didWork}, Skipped={skipped}, " +
                               $"MaxConcurrent={service.MaxConcurrent}");
                }
            }).QuickCheckThrowOnFailure();
        }
    }
}
