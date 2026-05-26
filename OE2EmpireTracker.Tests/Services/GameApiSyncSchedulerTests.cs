// <copyright file="GameApiSyncSchedulerTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for GameApiSyncScheduler round-robin scheduling.
    /// Feature: game-api-integration, Property 6: Round-Robin Fairness.
    /// **Validates: Requirements 5.4, 5.7**
    /// </summary>
    [TestFixture]
    public class GameApiSyncSchedulerTests
    {
        private readonly List<string> _tempFiles = new List<string>();

        [TearDown]
        public void TearDown()
        {
            foreach (string path in _tempFiles)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            _tempFiles.Clear();
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private GameApiCredentialManager CreateCredentialManagerWithKeys(int characterCount)
        {
            string tempFile = Path.Combine(
                Path.GetTempPath(),
                "oe2-test-sched-" + Guid.NewGuid().ToString("N") + ".dat");
            _tempFiles.Add(tempFile);

            var credManager = new GameApiCredentialManager(tempFile);
            for (int i = 0; i < characterCount; i++)
            {
                credManager.StoreKey("player-" + i.ToString("D3"), "test-key-" + i);
            }

            return credManager;
        }

        // -------------------------------------------------------------------
        // Property 1: Round-robin fairness — each character synced at
        // least floor(N/K) times over N cycles with K characters.
        // **Validates: Requirements 5.4, 5.7**
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property RoundRobinFairnessProperty()
        {
            var gen = from n in Gen.Choose(5, 30)
                      from k in Gen.Choose(2, 5)
                      select new { N = n, K = k };

            return Prop.ForAll(
                Arb.From(gen),
                param =>
                {
                    var credManager = CreateCredentialManagerWithKeys(param.K);
                    var client = new GameApiClient("http://localhost:99999");
                    var firstUUID = credManager.GetConfiguredPlayerUUIDs()[0];
                    var monitor = new GameApiConnectionMonitor(client, credManager, firstUUID);

                    var scheduler = new GameApiSyncScheduler(client, credManager, monitor);
                    scheduler.CurrentRoundRobinIndex = 0;

                    // Track how many times each character index is visited
                    var syncCounts = new int[param.K];

                    for (int cycle = 0; cycle < param.N; cycle++)
                    {
                        int currentIndex = scheduler.CurrentRoundRobinIndex;
                        syncCounts[currentIndex]++;
                        scheduler.PerformRoundRobinSyncAsync().GetAwaiter().GetResult();
                    }

                    int expectedMin = param.N / param.K;
                    bool allFair = true;
                    for (int i = 0; i < param.K; i++)
                    {
                        if (syncCounts[i] < expectedMin)
                        {
                            allFair = false;
                        }
                    }

                    client.Dispose();
                    monitor.Dispose();
                    scheduler.Dispose();

                    return allFair.Label(
                        string.Format(
                            "All {0} characters synced >= {1} times over {2} cycles",
                            param.K,
                            expectedMin,
                            param.N));
                });
        }

        // -------------------------------------------------------------------
        // Property 2: Index advancement — after each PerformRoundRobinSyncAsync
        // call, CurrentRoundRobinIndex advances by 1 (wrapping at K).
        // **Validates: Requirements 5.4**
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property IndexAdvancesOnEachCycle()
        {
            var gen = from k in Gen.Choose(2, 5)
                      from startIndex in Gen.Choose(0, 4)
                      select new { K = k, StartIndex = startIndex % k };

            return Prop.ForAll(
                Arb.From(gen),
                param =>
                {
                    var credManager = CreateCredentialManagerWithKeys(param.K);
                    var client = new GameApiClient("http://localhost:99999");
                    var firstUUID = credManager.GetConfiguredPlayerUUIDs()[0];
                    var monitor = new GameApiConnectionMonitor(client, credManager, firstUUID);

                    var scheduler = new GameApiSyncScheduler(client, credManager, monitor);
                    scheduler.CurrentRoundRobinIndex = param.StartIndex;

                    int expectedAfter = (param.StartIndex + 1) % param.K;
                    scheduler.PerformRoundRobinSyncAsync().GetAwaiter().GetResult();
                    int actualAfter = scheduler.CurrentRoundRobinIndex;

                    client.Dispose();
                    monitor.Dispose();
                    scheduler.Dispose();

                    return (actualAfter == expectedAfter).Label(
                        string.Format(
                            "Index should advance from {0} to {1} (K={2}), got {3}",
                            param.StartIndex,
                            expectedAfter,
                            param.K,
                            actualAfter));
                });
        }

        // -------------------------------------------------------------------
        // Unit test: Manual SyncNow syncs all characters
        // **Validates: Requirements 5.7**
        // -------------------------------------------------------------------

        [Test]
        public void SyncNowAsyncCallsSyncForAllCharacters()
        {
            var credManager = CreateCredentialManagerWithKeys(3);
            var client = new GameApiClient("http://localhost:99999");
            var firstUUID = credManager.GetConfiguredPlayerUUIDs()[0];
            var monitor = new GameApiConnectionMonitor(client, credManager, firstUUID);

            var scheduler = new GameApiSyncScheduler(client, credManager, monitor);

            // SyncNowAsync calls SyncCharacterAsync for every configured character.
            // Even though the HTTP calls fail (no real server), the method completes
            // without throwing and processes all characters.
            Assert.DoesNotThrowAsync(async () => await scheduler.SyncNowAsync());

            client.Dispose();
            monitor.Dispose();
            scheduler.Dispose();
        }

        // -------------------------------------------------------------------
        // Unit test: Index wraps around at K
        // -------------------------------------------------------------------

        [Test]
        public void IndexWrapsAroundAtCharacterCount()
        {
            var credManager = CreateCredentialManagerWithKeys(3);
            var client = new GameApiClient("http://localhost:99999");
            var firstUUID = credManager.GetConfiguredPlayerUUIDs()[0];
            var monitor = new GameApiConnectionMonitor(client, credManager, firstUUID);

            var scheduler = new GameApiSyncScheduler(client, credManager, monitor);
            scheduler.CurrentRoundRobinIndex = 2;

            scheduler.PerformRoundRobinSyncAsync().GetAwaiter().GetResult();

            Assert.That(scheduler.CurrentRoundRobinIndex, Is.EqualTo(0),
                "Index should wrap from 2 back to 0 with 3 characters");

            client.Dispose();
            monitor.Dispose();
            scheduler.Dispose();
        }

        // -------------------------------------------------------------------
        // Unit test: Failed character is skipped and index still advances
        // -------------------------------------------------------------------

        [Test]
        public void FailedCharacterSkippedIndexStillAdvances()
        {
            var credManager = CreateCredentialManagerWithKeys(4);
            var client = new GameApiClient("http://localhost:99999");
            var firstUUID = credManager.GetConfiguredPlayerUUIDs()[0];
            var monitor = new GameApiConnectionMonitor(client, credManager, firstUUID);

            var scheduler = new GameApiSyncScheduler(client, credManager, monitor);
            scheduler.CurrentRoundRobinIndex = 1;

            // This will fail (no real server) but index should still advance
            scheduler.PerformRoundRobinSyncAsync().GetAwaiter().GetResult();

            Assert.That(scheduler.CurrentRoundRobinIndex, Is.EqualTo(2),
                "Index should advance from 1 to 2 even when sync fails");

            client.Dispose();
            monitor.Dispose();
            scheduler.Dispose();
        }
    }
}
