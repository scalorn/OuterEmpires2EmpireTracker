// <copyright file="OfflineQueuePropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Linq;
using FsCheck;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Client
{
    /// <summary>
    /// Property-based tests for OfflineQueue PlayerRoot round-trip serialization.
    /// Feature: faction-typed-client-migration
    /// </summary>
    [TestFixture]
    public class OfflineQueuePropertyTests
    {
        private string _tempDir;

        /// <summary>
        /// Creates a temporary directory for queue file persistence.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "OQ_PropTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        /// <summary>
        /// Cleans up the temporary directory after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        /// <summary>
        /// Property 1: OfflineQueue PlayerRoot Round-Trip.
        /// For any valid PlayerRoot, serializing to QueuedChange.Json, saving via OfflineQueue,
        /// then loading produces a QueuedChange with TypedPayload matching the original.
        /// **Validates: Requirements 5.2, 5.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property PlayerRoot_RoundTrips_ThroughOfflineQueue()
        {
            var profileGen =
                from uuid in Gen.Elements("prof-1", "prof-2", "prof-3", "prof-4")
                from name in Gen.Elements("Alice", "Bob", "Charlie", "Diana", "Eve")
                from faction in Gen.Elements("Federation", "Empire", "Syndicate")
                select new PlayerProfile
                {
                    UUID = uuid,
                    Name = name,
                    Faction = faction,
                };

            var colonyGen =
                from uuid in Gen.Elements("col-1", "col-2", "col-3", "col-4")
                from planet in Gen.Elements("Terra", "Mars", "Ceres", "Titan")
                from colonyName in Gen.Elements("Alpha Base", "Mining Hub", "Outpost 7")
                select new Colony
                {
                    UUID = uuid,
                    PlanetName = planet,
                    ColonyName = colonyName,
                };

            var playerRootGen =
                from version in Gen.Choose(1, 10)
                from playerUuid in Gen.Elements("player-1", "player-2", "player-3")
                from profileCount in Gen.Choose(0, 3)
                from profiles in Gen.ListOf(profileCount, profileGen)
                from colonyCount in Gen.Choose(0, 3)
                from colonies in Gen.ListOf(colonyCount, colonyGen)
                select new PlayerRoot
                {
                    DataVersion = version,
                    CurrentPlayerUUID = playerUuid,
                    PlayerProfile = profiles.ToArray(),
                    Colony = colonies.ToArray(),
                };

            return Prop.ForAll(Arb.From(playerRootGen), original =>
            {
                string queueFilePath = Path.Combine(_tempDir, "queue.json");

                // Serialize PlayerRoot to JSON (simulates what SyncManager does)
                string json = JsonConvert.SerializeObject(original);

                var change = new QueuedChange
                {
                    CharacterUUID = "test-char-uuid",
                    DataType = "playerRoot",
                    Json = json,
                    QueuedUtc = SystemClock.UtcNow,
                };

                // Save via OfflineQueue
                var saveQueue = new OfflineQueue(queueFilePath);
                saveQueue.Enqueue(change);
                saveQueue.Save();

                // Load via OfflineQueue (triggers typed payload deserialization)
                var loadQueue = new OfflineQueue(queueFilePath);
                loadQueue.Load();

                var loaded = loadQueue.GetAll();
                Assert.That(loaded.Count, Is.EqualTo(1), "Loaded queue should have exactly 1 entry");

                var loadedChange = loaded[0];
                Assert.That(loadedChange.TypedPayload, Is.Not.Null, "TypedPayload should be populated after Load");

                // Verify key fields match
                var roundTripped = loadedChange.TypedPayload;
                Assert.That(roundTripped.DataVersion, Is.EqualTo(original.DataVersion));
                Assert.That(roundTripped.CurrentPlayerUUID, Is.EqualTo(original.CurrentPlayerUUID));
                Assert.That(roundTripped.PlayerProfile.Length, Is.EqualTo(original.PlayerProfile.Length));
                Assert.That(roundTripped.Colony.Length, Is.EqualTo(original.Colony.Length));

                // Verify profile names round-trip
                for (int i = 0; i < original.PlayerProfile.Length; i++)
                {
                    Assert.That(roundTripped.PlayerProfile[i].Name, Is.EqualTo(original.PlayerProfile[i].Name));
                    Assert.That(roundTripped.PlayerProfile[i].UUID, Is.EqualTo(original.PlayerProfile[i].UUID));
                    Assert.That(roundTripped.PlayerProfile[i].Faction, Is.EqualTo(original.PlayerProfile[i].Faction));
                }

                // Verify colony names round-trip
                for (int i = 0; i < original.Colony.Length; i++)
                {
                    Assert.That(roundTripped.Colony[i].UUID, Is.EqualTo(original.Colony[i].UUID));
                    Assert.That(roundTripped.Colony[i].PlanetName, Is.EqualTo(original.Colony[i].PlanetName));
                    Assert.That(roundTripped.Colony[i].ColonyName, Is.EqualTo(original.Colony[i].ColonyName));
                }

                // Clean up for next iteration
                File.Delete(queueFilePath);
            });
        }
    }
}
