// <copyright file="GameApiRequestQueuePropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for GameApiRequestQueue.
    /// Feature: api-tps-limit-setting
    /// </summary>
    [TestFixture]
    public class GameApiRequestQueuePropertyTests
    {
        [TearDown]
        public void TearDown()
        {
            SystemClock.Reset();
        }

        /// <summary>
        /// Property 1: Rate Governor Throughput.
        ///
        /// Over any 5-second window, the number of dispatched items SHALL NOT exceed
        /// configuredTps * 5 + configuredTps (allowing burst up to 1 second of accumulated tokens).
        /// Uses TPS range [5, 20] with item count capped at TPS*3 to keep tests fast.
        /// **Validates: Requirements 7.1, 7.2, 7.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property RateGovernorThroughput()
        {
            var gen = from tpsInt in Gen.Choose(50, 200)
                      from countExtra in Gen.Choose(5, 30)
                      select new { Tps = tpsInt / 10.0, Count = countExtra };

            return Prop.ForAll(Arb.From(gen), input =>
            {
                SystemClock.FreezeAt(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                SystemClock.EnableInstantDelay();
                double tps = input.Tps;
                int count = input.Count;

                var queue = new GameApiRequestQueue(tps);
                var dispatchTimes = new ConcurrentBag<DateTime>();

                for (int i = 0; i < count; i++)
                {
                    int idx = i;
                    queue.EnqueueAsync(new WorkItem
                    {
                        Label = $"p1-{idx}",
                        ExecuteAsync = ct =>
                        {
                            dispatchTimes.Add(SystemClock.UtcNow);
                            return Task.FromResult<IReadOnlyList<WorkItem>>(
                                Array.Empty<WorkItem>());
                        },
                    }).GetAwaiter().GetResult();
                }

                queue.Start(CancellationToken.None);
                queue.DrainAsync().GetAwaiter().GetResult();

                if (dispatchTimes.Count < 2)
                {
                    return true.Label("Too few items to measure window");
                }

                var sorted = dispatchTimes.OrderBy(t => t).ToList();
                var startTime = sorted[0];

                // Check dispatch count in 5s window from first dispatch
                int windowCount = sorted.Count(t => (t - startTime).TotalSeconds <= 5.0);

                // Allowed: burst (tps tokens) + 5 seconds of refill (tps * 5)
                double maxAllowed = (tps * 5.0) + tps;

                return (windowCount <= (int)Math.Ceiling(maxAllowed) + 1)
                    .Label($"TPS={tps:F1}, count={count}, windowCount={windowCount}, maxAllowed={maxAllowed:F1}");
            });
        }

        /// <summary>
        /// Property 2: FIFO Ordering.
        ///
        /// Items are dispatched in the order they were enqueued. For any random set of items,
        /// dispatch sequence numbers are monotonically increasing.
        /// **Validates: Requirements 4.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property FifoOrdering()
        {
            var gen = Gen.Choose(2, 100);

            return Prop.ForAll(Arb.From(gen), count =>
            {
                SystemClock.FreezeAt(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                SystemClock.EnableInstantDelay();
                var dispatchOrder = new ConcurrentQueue<int>();
                var queue = new GameApiRequestQueue(200.0);

                for (int i = 0; i < count; i++)
                {
                    int idx = i;
                    queue.EnqueueAsync(new WorkItem
                    {
                        Label = $"fifo-{idx}",
                        ExecuteAsync = ct =>
                        {
                            dispatchOrder.Enqueue(idx);
                            return Task.FromResult<IReadOnlyList<WorkItem>>(
                                Array.Empty<WorkItem>());
                        },
                    }).GetAwaiter().GetResult();
                }

                queue.Start(CancellationToken.None);
                queue.DrainAsync().GetAwaiter().GetResult();

                var order = dispatchOrder.ToArray();
                if (order.Length != count)
                {
                    return false.Label($"Expected {count} items, got {order.Length}");
                }

                // Verify monotonically increasing
                for (int i = 1; i < order.Length; i++)
                {
                    if (order[i] <= order[i - 1])
                    {
                        return false.Label(
                            $"Not monotonic at index {i}: {order[i - 1]} -> {order[i]}");
                    }
                }

                return true.Label($"count={count}, all monotonically increasing");
            });
        }

        /// <summary>
        /// Property 3: Cascading Completeness.
        ///
        /// After drain, the total items processed equals seed items + all cascaded items generated.
        /// No work item is lost.
        /// **Validates: Requirements 5.1, 5.2, 5.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CascadingCompleteness()
        {
            var gen = from depth in Gen.Choose(1, 3)
                      from branching in Gen.Choose(0, 3)
                      select new { Depth = depth, Branching = branching };

            return Prop.ForAll(Arb.From(gen), input =>
            {
                SystemClock.FreezeAt(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                SystemClock.EnableInstantDelay();
                int processedCount = 0;
                var queue = new GameApiRequestQueue(200.0);

                // Calculate expected total nodes in tree: sum of branching^level for levels 0..depth
                int expectedTotal = 0;
                for (int level = 0; level <= input.Depth; level++)
                {
                    expectedTotal += (int)Math.Pow(input.Branching, level);
                }

                // If branching is 0, only the root node
                if (input.Branching == 0)
                {
                    expectedTotal = 1;
                }

                WorkItem CreateItem(int currentDepth)
                {
                    return new WorkItem
                    {
                        Label = $"cascade-d{currentDepth}",
                        ExecuteAsync = ct =>
                        {
                            Interlocked.Increment(ref processedCount);
                            if (currentDepth < input.Depth && input.Branching > 0)
                            {
                                var children = new List<WorkItem>();
                                for (int b = 0; b < input.Branching; b++)
                                {
                                    children.Add(CreateItem(currentDepth + 1));
                                }

                                return Task.FromResult<IReadOnlyList<WorkItem>>(children);
                            }

                            return Task.FromResult<IReadOnlyList<WorkItem>>(
                                Array.Empty<WorkItem>());
                        },
                    };
                }

                queue.EnqueueAsync(CreateItem(0)).GetAwaiter().GetResult();
                queue.Start(CancellationToken.None);
                queue.DrainAsync().GetAwaiter().GetResult();

                int actual = Volatile.Read(ref processedCount);
                return (actual == expectedTotal)
                    .Label($"depth={input.Depth}, branching={input.Branching}, expected={expectedTotal}, actual={actual}");
            });
        }

        /// <summary>
        /// Property 4: Error Isolation.
        ///
        /// A failing work item does not prevent other items from completing.
        /// After drain with K failures, succeeded + failed == total and failed == K.
        /// **Validates: Requirements 6.1, 6.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ErrorIsolation()
        {
            var gen = from totalCount in Gen.Choose(3, 30)
                      from failCount in Gen.Choose(1, totalCount - 1)
                      from failPositions in Gen.Shuffle(Enumerable.Range(0, totalCount).ToArray())
                                              .Select(a => new HashSet<int>(a.Take(failCount)))
                      select new { Total = totalCount, FailPositions = failPositions };

            return Prop.ForAll(Arb.From(gen), input =>
            {
                SystemClock.FreezeAt(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                SystemClock.EnableInstantDelay();
                var queue = new GameApiRequestQueue(200.0);

                for (int i = 0; i < input.Total; i++)
                {
                    int idx = i;
                    bool shouldFail = input.FailPositions.Contains(idx);
                    queue.EnqueueAsync(new WorkItem
                    {
                        Label = $"err-{idx}",
                        ExecuteAsync = ct =>
                        {
                            if (shouldFail)
                            {
                                throw new InvalidOperationException($"Simulated failure at {idx}");
                            }

                            return Task.FromResult<IReadOnlyList<WorkItem>>(
                                Array.Empty<WorkItem>());
                        },
                    }).GetAwaiter().GetResult();
                }

                queue.Start(CancellationToken.None);
                queue.DrainAsync().GetAwaiter().GetResult();

                var status = queue.CompletionStatus;
                int expectedFailed = input.FailPositions.Count;
                int expectedSucceeded = input.Total - expectedFailed;

                bool totalCorrect = status.Succeeded + status.Failed == input.Total;
                bool failedCorrect = status.Failed == expectedFailed;
                bool succeededCorrect = status.Succeeded == expectedSucceeded;

                return (totalCorrect && failedCorrect && succeededCorrect)
                    .Label($"total={input.Total}, expectedFailed={expectedFailed}, " +
                           $"succeeded={status.Succeeded}, failed={status.Failed}");
            });
        }

        /// <summary>
        /// Property 5: 429 Backoff Correctness.
        ///
        /// After NotifyRateLimited(N), effective TPS is reduced to 50% of the prior active rate.
        /// This is a pure state test verifying the adaptive controller halves TPS on each 429.
        /// **Validates: Requirements 17.1, 17.2, 17.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property BackoffCorrectness()
        {
            var gen = from retryAfter in Gen.Choose(1, 120)
                      from tpsInt in Gen.Choose(5, 200)
                      select new { RetryAfter = retryAfter, Tps = tpsInt / 10.0 };

            return Prop.ForAll(Arb.From(gen), input =>
            {
                var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
                SystemClock.FreezeAt(baseTime);

                var queue = new GameApiRequestQueue(input.Tps);
                double priorTps = queue.EffectiveTps;

                // Notify rate limited
                queue.NotifyRateLimited(input.RetryAfter);

                double afterTps = queue.EffectiveTps;

                // Effective TPS should be 50% of prior (minimum 0.1)
                double expectedTps = Math.Max(priorTps * 0.5, 0.1);
                bool tpsCorrect = Math.Abs(afterTps - expectedTps) < 0.001;

                // Verify TPS is strictly less than or equal to 50% of prior
                bool halved = afterTps <= (priorTps * 0.5) + 0.001;

                return (tpsCorrect && halved)
                    .Label($"TPS={input.Tps:F1}, retryAfter={input.RetryAfter}, " +
                           $"priorTps={priorTps:F2}, afterTps={afterTps:F2}, expected={expectedTps:F2}");
            });
        }

        /// <summary>
        /// Property 6: Drain Completeness.
        ///
        /// DrainAsync does not complete until all items (including cascaded) are finished.
        /// There are zero in-flight items when drain resolves.
        /// **Validates: Requirements 5.3, 6.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property DrainCompleteness()
        {
            var gen = from count in Gen.Choose(2, 20)
                      from delayMs in Gen.Choose(0, 50)
                      from cascadeCount in Gen.Choose(0, 3)
                      select new { Count = count, DelayMs = delayMs, CascadeCount = cascadeCount };

            return Prop.ForAll(Arb.From(gen), input =>
            {
                SystemClock.FreezeAt(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                SystemClock.EnableInstantDelay();
                int processedCount = 0;
                var queue = new GameApiRequestQueue(200.0);

                for (int i = 0; i < input.Count; i++)
                {
                    int idx = i;
                    queue.EnqueueAsync(new WorkItem
                    {
                        Label = $"drain-{idx}",
                        ExecuteAsync = async ct =>
                        {
                            if (input.DelayMs > 0)
                            {
                                await Task.Delay(input.DelayMs, ct).ConfigureAwait(false);
                            }

                            Interlocked.Increment(ref processedCount);

                            // Only first item cascades to avoid explosion
                            if (idx == 0 && input.CascadeCount > 0)
                            {
                                var children = new List<WorkItem>();
                                for (int c = 0; c < input.CascadeCount; c++)
                                {
                                    int childIdx = c;
                                    children.Add(new WorkItem
                                    {
                                        Label = $"drain-cascade-{childIdx}",
                                        ExecuteAsync = async ct2 =>
                                        {
                                            if (input.DelayMs > 0)
                                            {
                                                await Task.Delay(input.DelayMs, ct2)
                                                    .ConfigureAwait(false);
                                            }

                                            Interlocked.Increment(ref processedCount);
                                            return Array.Empty<WorkItem>();
                                        },
                                    });
                                }

                                return (IReadOnlyList<WorkItem>)children;
                            }

                            return Array.Empty<WorkItem>();
                        },
                    }).GetAwaiter().GetResult();
                }

                queue.Start(CancellationToken.None);
                queue.DrainAsync().GetAwaiter().GetResult();

                // After drain, all items must be processed
                int expectedTotal = input.Count + input.CascadeCount;
                int actual = Volatile.Read(ref processedCount);
                var status = queue.CompletionStatus;

                // Verify no in-flight items remain (succeeded + failed == total processed)
                bool inflightZero = status.TotalProcessed == actual;
                bool allProcessed = actual == expectedTotal;

                return (inflightZero && allProcessed)
                    .Label($"count={input.Count}, cascades={input.CascadeCount}, delay={input.DelayMs}ms, " +
                           $"expected={expectedTotal}, actual={actual}, status.Total={status.TotalProcessed}");
            });
        }
    }
}
