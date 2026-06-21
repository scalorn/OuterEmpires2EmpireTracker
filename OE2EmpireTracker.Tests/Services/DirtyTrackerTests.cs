// <copyright file="DirtyTrackerTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for DirtyTracker.
    /// Satisfies: Req 2, Criteria 1-2.
    /// </summary>
    [TestFixture]
    public class DirtyTrackerTests
    {
        private DirtyTracker tracker;

        [SetUp]
        public void SetUp()
        {
            tracker = new DirtyTracker();
        }

        [Test]
        public void MarkDirty_AddsEntry_GetDirtyUUIDs_ReturnsIt()
        {
            tracker.MarkDirty<Colony>("uuid-1");

            IReadOnlyList<string> result = tracker.GetDirtyUUIDs<Colony>();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0], Is.EqualTo("uuid-1"));
        }

        [Test]
        public void MarkDeleted_AddsEntry_GetDeletedUUIDs_ReturnsIt()
        {
            tracker.MarkDeleted<Survey>("uuid-2");

            IReadOnlyList<string> result = tracker.GetDeletedUUIDs<Survey>();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0], Is.EqualTo("uuid-2"));
        }

        [Test]
        public void ClearDirty_RemovesSpecificEntry()
        {
            tracker.MarkDirty<Colony>("uuid-1");
            tracker.MarkDirty<Colony>("uuid-2");

            tracker.ClearDirty<Colony>("uuid-1");

            IReadOnlyList<string> result = tracker.GetDirtyUUIDs<Colony>();
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0], Is.EqualTo("uuid-2"));
        }

        [Test]
        public void ClearDeleted_RemovesSpecificEntry()
        {
            tracker.MarkDeleted<Survey>("uuid-1");
            tracker.MarkDeleted<Survey>("uuid-2");

            tracker.ClearDeleted<Survey>("uuid-1");

            IReadOnlyList<string> result = tracker.GetDeletedUUIDs<Survey>();
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0], Is.EqualTo("uuid-2"));
        }

        [Test]
        public void ClearAll_EmptiesBothSets()
        {
            tracker.MarkDirty<Colony>("uuid-1");
            tracker.MarkDirty<Survey>("uuid-2");
            tracker.MarkDeleted<Colony>("uuid-3");
            tracker.MarkDeleted<Survey>("uuid-4");

            tracker.ClearAll();

            Assert.That(tracker.GetDirtyUUIDs<Colony>(), Is.Empty);
            Assert.That(tracker.GetDirtyUUIDs<Survey>(), Is.Empty);
            Assert.That(tracker.GetDeletedUUIDs<Colony>(), Is.Empty);
            Assert.That(tracker.GetDeletedUUIDs<Survey>(), Is.Empty);
        }

        [Test]
        public void HasChanges_TrueAfterMark_FalseAfterClearAll()
        {
            Assert.That(tracker.HasChanges, Is.False);

            tracker.MarkDirty<Colony>("uuid-1");
            Assert.That(tracker.HasChanges, Is.True);

            tracker.ClearAll();
            Assert.That(tracker.HasChanges, Is.False);
        }

        [Test]
        public void HasChanges_TrueAfterMarkDeleted()
        {
            tracker.MarkDeleted<Survey>("uuid-1");
            Assert.That(tracker.HasChanges, Is.True);
        }

        [Test]
        public void MultipleEntityTypes_TrackedIndependently()
        {
            tracker.MarkDirty<Colony>("colony-uuid");
            tracker.MarkDirty<Survey>("survey-uuid");
            tracker.MarkDeleted<Colony>("colony-del");
            tracker.MarkDeleted<Survey>("survey-del");

            IReadOnlyList<string> dirtyColonies = tracker.GetDirtyUUIDs<Colony>();
            IReadOnlyList<string> dirtySurveys = tracker.GetDirtyUUIDs<Survey>();
            IReadOnlyList<string> deletedColonies = tracker.GetDeletedUUIDs<Colony>();
            IReadOnlyList<string> deletedSurveys = tracker.GetDeletedUUIDs<Survey>();

            Assert.That(dirtyColonies, Has.Count.EqualTo(1));
            Assert.That(dirtyColonies[0], Is.EqualTo("colony-uuid"));
            Assert.That(dirtySurveys, Has.Count.EqualTo(1));
            Assert.That(dirtySurveys[0], Is.EqualTo("survey-uuid"));
            Assert.That(deletedColonies, Has.Count.EqualTo(1));
            Assert.That(deletedColonies[0], Is.EqualTo("colony-del"));
            Assert.That(deletedSurveys, Has.Count.EqualTo(1));
            Assert.That(deletedSurveys[0], Is.EqualTo("survey-del"));
        }

        [Test]
        public void ClearDirty_DoesNotAffectOtherTypes()
        {
            tracker.MarkDirty<Colony>("uuid-1");
            tracker.MarkDirty<Survey>("uuid-1");

            tracker.ClearDirty<Colony>("uuid-1");

            Assert.That(tracker.GetDirtyUUIDs<Colony>(), Is.Empty);
            Assert.That(tracker.GetDirtyUUIDs<Survey>(), Has.Count.EqualTo(1));
        }

        [Test]
        public void ThreadSafety_ConcurrentMarkAndClear_NoExceptionsAndConsistentState()
        {
            const int threadCount = 8;
            const int operationsPerThread = 200;
            var barrier = new Barrier(threadCount);
            var threads = new Thread[threadCount];
            var exceptions = new List<System.Exception>();
            var lockObj = new object();

            for (int i = 0; i < threadCount; i++)
            {
                int threadIndex = i;
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();
                        for (int op = 0; op < operationsPerThread; op++)
                        {
                            string uuid = $"uuid-{threadIndex}-{op}";
                            tracker.MarkDirty<Colony>(uuid);
                            tracker.MarkDeleted<Survey>(uuid);

                            if (op > 0)
                            {
                                string prevUuid = $"uuid-{threadIndex}-{op - 1}";
                                tracker.ClearDirty<Colony>(prevUuid);
                                tracker.ClearDeleted<Survey>(prevUuid);
                            }

                            if (op % 50 == 0)
                            {
                                tracker.GetDirtyUUIDs<Colony>();
                                tracker.GetDeletedUUIDs<Survey>();
                                bool hasChanges = tracker.HasChanges;
                                Assert.That(hasChanges, Is.True);
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        lock (lockObj)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
                threads[i].Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            Assert.That(exceptions, Is.Empty,
                "Concurrent operations should not throw exceptions");

            // Each thread left the last entry uncleaned
            IReadOnlyList<string> remaining = tracker.GetDirtyUUIDs<Colony>();
            Assert.That(remaining.Count, Is.EqualTo(threadCount));

            IReadOnlyList<string> deletedRemaining = tracker.GetDeletedUUIDs<Survey>();
            Assert.That(deletedRemaining.Count, Is.EqualTo(threadCount));
        }
    }
}
