// <copyright file="ErrorIsolationPropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for error isolation in GameApiRequestQueue.
    /// Feature: queue-based-sync
    /// </summary>
    [TestFixture]
    public class ErrorIsolationPropertyTests
    {
        [TearDown]
        public void TearDown()
        {
            SystemClock.Reset();
        }

        /// <summary>
        /// Property 4: Error Isolation.
        ///
        /// A failed work item (exception thrown in delegate) does not prevent other
        /// work items from completing. After Start + DrainAsync, the queue's
        /// CompletionStatus.Succeeded + CompletionStatus.Failed equals the total
        /// number of enqueued items, and non-failing items all complete successfully.
        ///
        /// Generates:
        /// - Random N (5–50) total work items
        /// - Random subset of indices that throw exceptions
        /// - Verifies succeeded + failed = N
        /// - Verifies non-failing items all ran to completion
        ///
        /// **Validates: Requirements 12.1, 12.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property FailedItems_DoNotPrevent_OtherItems_FromCompleting()
        {
            var gen = from total in Gen.Choose(5, 50)
                      from failCount in Gen.Choose(1, total - 1)
                      from failPositions in Gen.Shuffle(
                          Enumerable.Range(0, total).ToArray())
                          .Select(a => new HashSet<int>(a.Take(failCount)))
                      select new
                      {
                          Total = total,
                          FailPositions = failPositions,
                      };

            return Prop.ForAll(Arb.From(gen), input =>
            {
                SystemClock.FreezeAt(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                SystemClock.EnableInstantDelay();
                var completedIndices = new ConcurrentBag<int>();
                var queue = new GameApiRequestQueue(200.0, maxRetries: 1);

                for (int i = 0; i < input.Total; i++)
                {
                    int idx = i;
                    bool shouldFail = input.FailPositions.Contains(idx);
                    queue.EnqueueAsync(new WorkItem
                    {
                        Label = $"iso-{idx}",
                        ExecuteAsync = ct =>
                        {
                            if (shouldFail)
                            {
                                throw new InvalidOperationException(
                                    $"Injected failure at index {idx}");
                            }

                            completedIndices.Add(idx);
                            return Task.FromResult<IReadOnlyList<WorkItem>>(
                                Array.Empty<WorkItem>());
                        },
                    }).GetAwaiter().GetResult();
                }

                queue.Start(CancellationToken.None);
                queue.DrainAsync().GetAwaiter().GetResult();

                var status = queue.CompletionStatus;
                int expectedSucceeded = input.Total - input.FailPositions.Count;

                // Property: succeeded + failed = total enqueued
                bool totalCorrect = status.Succeeded + status.Failed == input.Total;

                // Property: failed count matches injected failures
                bool failedCorrect = status.Failed == input.FailPositions.Count;

                // Property: all non-failing items completed
                bool allNonFailingCompleted =
                    completedIndices.Count == expectedSucceeded;

                return (totalCorrect && failedCorrect && allNonFailingCompleted)
                    .Label($"total={input.Total}, failCount={input.FailPositions.Count}, " +
                           $"succeeded={status.Succeeded}, failed={status.Failed}, " +
                           $"completedBag={completedIndices.Count}");
            });
        }

        /// <summary>
        /// Property 4b: Error Isolation — Partial Failure Is Not Total Failure.
        ///
        /// When some items fail but others succeed, the queue completes normally
        /// (DrainAsync resolves) and reports partial failure rather than aborting.
        /// The succeeded count is strictly positive when at least one item does not fail.
        ///
        /// **Validates: Requirements 12.1, 12.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property PartialFailure_StillCompletes_WithPositiveSucceeded()
        {
            var gen = from total in Gen.Choose(5, 50)
                      from failCount in Gen.Choose(1, total - 1)
                      select new { Total = total, FailCount = failCount };

            return Prop.ForAll(Arb.From(gen), input =>
            {
                SystemClock.FreezeAt(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                SystemClock.EnableInstantDelay();
                var queue = new GameApiRequestQueue(200.0, maxRetries: 1);

                // Fail the first failCount items, succeed the rest
                for (int i = 0; i < input.Total; i++)
                {
                    int idx = i;
                    queue.EnqueueAsync(new WorkItem
                    {
                        Label = $"partial-{idx}",
                        ExecuteAsync = ct =>
                        {
                            if (idx < input.FailCount)
                            {
                                throw new InvalidOperationException(
                                    $"Injected failure {idx}");
                            }

                            return Task.FromResult<IReadOnlyList<WorkItem>>(
                                Array.Empty<WorkItem>());
                        },
                    }).GetAwaiter().GetResult();
                }

                queue.Start(CancellationToken.None);
                queue.DrainAsync().GetAwaiter().GetResult();

                var status = queue.CompletionStatus;

                // Drain completed (we got here) so queue did not abort
                bool succeededPositive = status.Succeeded > 0;
                bool totalMatches = status.Succeeded + status.Failed == input.Total;

                return (succeededPositive && totalMatches)
                    .Label($"total={input.Total}, failCount={input.FailCount}, " +
                           $"succeeded={status.Succeeded}, failed={status.Failed}");
            });
        }
    }
}
