// <copyright file="GameApiRequestQueueTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for GameApiRequestQueue basic dispatch, cascading, error isolation, and cancellation.
    /// </summary>
    [TestFixture]
    public class GameApiRequestQueueTests
    {
        [TearDown]
        public void TearDown()
        {
            SystemClock.Reset();
        }

        // ===== Task 10.1: Single item dispatch and FIFO ordering =====

        [Test]
        public async Task Enqueue_SingleItem_Dispatches()
        {
            var executed = false;
            var queue = new GameApiRequestQueue(100.0);

            await queue.EnqueueAsync(new WorkItem
            {
                Label = "single-item",
                ExecuteAsync = ct =>
                {
                    executed = true;
                    return Task.FromResult<IReadOnlyList<WorkItem>>(Array.Empty<WorkItem>());
                },
            });

            queue.Start(CancellationToken.None);
            await queue.DrainAsync();

            Assert.That(executed, Is.True);
            Assert.That(queue.CompletionStatus.Succeeded, Is.EqualTo(1));
            Assert.That(queue.CompletionStatus.TotalProcessed, Is.EqualTo(1));
        }

        [Test]
        public async Task Enqueue_MultipleItems_FIFO()
        {
            var order = new ConcurrentQueue<int>();
            var queue = new GameApiRequestQueue(100.0);

            for (int i = 0; i < 5; i++)
            {
                int captured = i;
                await queue.EnqueueAsync(new WorkItem
                {
                    Label = $"item-{captured}",
                    ExecuteAsync = ct =>
                    {
                        order.Enqueue(captured);
                        return Task.FromResult<IReadOnlyList<WorkItem>>(Array.Empty<WorkItem>());
                    },
                });
            }

            queue.Start(CancellationToken.None);
            await queue.DrainAsync();

            var dispatched = order.ToArray();
            Assert.That(dispatched.Length, Is.EqualTo(5));
            for (int i = 0; i < 5; i++)
            {
                Assert.That(dispatched[i], Is.EqualTo(i), $"Expected item {i} at position {i}");
            }
        }

        // ===== Task 10.2: Cascading items and drain completeness =====

        [Test]
        public async Task CascadingItems_AreProcessed()
        {
            var executed = new ConcurrentBag<string>();
            var queue = new GameApiRequestQueue(100.0);

            await queue.EnqueueAsync(new WorkItem
            {
                Label = "parent",
                ExecuteAsync = ct =>
                {
                    executed.Add("parent");
                    var children = new List<WorkItem>
                    {
                        new WorkItem
                        {
                            Label = "child-1",
                            ExecuteAsync = ct2 =>
                            {
                                executed.Add("child-1");
                                return Task.FromResult<IReadOnlyList<WorkItem>>(Array.Empty<WorkItem>());
                            },
                        },
                        new WorkItem
                        {
                            Label = "child-2",
                            ExecuteAsync = ct2 =>
                            {
                                executed.Add("child-2");
                                return Task.FromResult<IReadOnlyList<WorkItem>>(Array.Empty<WorkItem>());
                            },
                        },
                    };
                    return Task.FromResult<IReadOnlyList<WorkItem>>(children);
                },
            });

            queue.Start(CancellationToken.None);
            await queue.DrainAsync();

            Assert.That(executed.Count, Is.EqualTo(3));
            Assert.That(executed, Does.Contain("parent"));
            Assert.That(executed, Does.Contain("child-1"));
            Assert.That(executed, Does.Contain("child-2"));
            Assert.That(queue.CompletionStatus.Succeeded, Is.EqualTo(3));
        }

        [Test]
        public async Task DrainAsync_WaitsForAllCascades()
        {
            var executed = new ConcurrentBag<string>();
            var queue = new GameApiRequestQueue(100.0);

            await queue.EnqueueAsync(new WorkItem
            {
                Label = "root",
                ExecuteAsync = ct =>
                {
                    executed.Add("root");
                    var child = new WorkItem
                    {
                        Label = "level-1",
                        ExecuteAsync = ct2 =>
                        {
                            executed.Add("level-1");
                            var grandchild = new WorkItem
                            {
                                Label = "level-2",
                                ExecuteAsync = ct3 =>
                                {
                                    executed.Add("level-2");
                                    return Task.FromResult<IReadOnlyList<WorkItem>>(Array.Empty<WorkItem>());
                                },
                            };
                            return Task.FromResult<IReadOnlyList<WorkItem>>(new[] { grandchild });
                        },
                    };
                    return Task.FromResult<IReadOnlyList<WorkItem>>(new[] { child });
                },
            });

            queue.Start(CancellationToken.None);
            await queue.DrainAsync();

            Assert.That(executed.Count, Is.EqualTo(3));
            Assert.That(executed, Does.Contain("root"));
            Assert.That(executed, Does.Contain("level-1"));
            Assert.That(executed, Does.Contain("level-2"));
            Assert.That(queue.CompletionStatus.TotalProcessed, Is.EqualTo(3));
        }

        // ===== Task 10.3: Error isolation and cancellation =====

        [Test]
        public async Task FailedItem_DoesNotBlockOthers()
        {
            var executed = new ConcurrentBag<string>();
            var queue = new GameApiRequestQueue(100.0);

            await queue.EnqueueAsync(new WorkItem
            {
                Label = "good-1",
                ExecuteAsync = ct =>
                {
                    executed.Add("good-1");
                    return Task.FromResult<IReadOnlyList<WorkItem>>(Array.Empty<WorkItem>());
                },
            });

            await queue.EnqueueAsync(new WorkItem
            {
                Label = "bad-item",
                ExecuteAsync = ct => throw new InvalidOperationException("Simulated failure"),
            });

            await queue.EnqueueAsync(new WorkItem
            {
                Label = "good-2",
                ExecuteAsync = ct =>
                {
                    executed.Add("good-2");
                    return Task.FromResult<IReadOnlyList<WorkItem>>(Array.Empty<WorkItem>());
                },
            });

            queue.Start(CancellationToken.None);
            await queue.DrainAsync();

            Assert.That(executed, Does.Contain("good-1"));
            Assert.That(executed, Does.Contain("good-2"));
            Assert.That(queue.CompletionStatus.Succeeded, Is.EqualTo(2));
            Assert.That(queue.CompletionStatus.Failed, Is.EqualTo(1));
            Assert.That(queue.Errors.Count, Is.EqualTo(1));
            Assert.That(queue.Errors[0].WorkItemLabel, Is.EqualTo("bad-item"));
        }

        [Test]
        public async Task Cancellation_StopsNewDispatch()
        {
            var dispatched = new ConcurrentBag<string>();
            var cts = new CancellationTokenSource();
            var queue = new GameApiRequestQueue(100.0);

            // Enqueue a first item that cancels the token when executed
            await queue.EnqueueAsync(new WorkItem
            {
                Label = "cancel-trigger",
                ExecuteAsync = ct =>
                {
                    dispatched.Add("cancel-trigger");
                    cts.Cancel();
                    return Task.FromResult<IReadOnlyList<WorkItem>>(Array.Empty<WorkItem>());
                },
            });

            // Enqueue second item — should not be dispatched after cancellation
            await queue.EnqueueAsync(new WorkItem
            {
                Label = "should-not-run",
                ExecuteAsync = ct =>
                {
                    dispatched.Add("should-not-run");
                    return Task.FromResult<IReadOnlyList<WorkItem>>(Array.Empty<WorkItem>());
                },
            });

            queue.Start(cts.Token);

            // Give the dispatch loop time to process and notice cancellation
            await Task.Delay(500);

            Assert.That(dispatched, Does.Contain("cancel-trigger"));
            Assert.That(dispatched, Does.Not.Contain("should-not-run"));
        }

        [Test]
        public async Task Cancellation_InflightItemsTimeout()
        {
            var itemStarted = new TaskCompletionSource<bool>();
            var itemCancelled = false;
            var timeout = TimeSpan.FromMilliseconds(200);
            var queue = new GameApiRequestQueue(100.0, perItemTimeout: timeout);

            await queue.EnqueueAsync(new WorkItem
            {
                Label = "slow-item",
                ExecuteAsync = async ct =>
                {
                    itemStarted.TrySetResult(true);
                    try
                    {
                        // Simulate a long-running operation that respects cancellation
                        await Task.Delay(TimeSpan.FromSeconds(10), ct);
                    }
                    catch (OperationCanceledException)
                    {
                        itemCancelled = true;
                        throw;
                    }

                    return Array.Empty<WorkItem>();
                },
            });

            queue.Start(CancellationToken.None);

            // Wait for the item to start executing
            await itemStarted.Task;

            // Wait for the timeout to fire
            await Task.Delay(500);

            Assert.That(itemCancelled, Is.True);
            Assert.That(queue.CompletionStatus.Failed, Is.EqualTo(1));
            Assert.That(queue.Errors.Count, Is.EqualTo(1));
        }
    }
}
