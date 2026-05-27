// <copyright file="ProductionSyncSchedulerTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for ProductionSyncScheduler delegation correctness.
    /// Validates: Req 1, Criteria 2-5.
    /// </summary>
    [TestFixture]
    public class ProductionSyncSchedulerTests
    {
        private const string TestPlayerUUID = "player-uuid-001";
        private const string UnknownUUID = "unknown-uuid-999";
        private const string AppId = "test-app-id";
        private const string ClientId = "test-client-id";

        private readonly List<string> _tempFiles = new List<string>();

        private PlayerContext _playerContext;
        private ProductionSyncScheduler _scheduler;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            GameApiCredentialManager.RegisterProtectionFunctions(
                CredentialStore.Protect,
                CredentialStore.Unprotect);
        }

        [SetUp]
        public void SetUp()
        {
            PlayerContext.FilePath = string.Empty;

            var root = new PlayerRoot
            {
                PlayerProfile = new[]
                {
                    new PlayerProfile { UUID = TestPlayerUUID, Name = "TestPlayer" },
                },
                Colony = new[]
                {
                    new Colony { UUID = "colony-1", OwnerUUID = TestPlayerUUID, ColonyName = "Alpha" },
                    new Colony { UUID = "colony-2", OwnerUUID = TestPlayerUUID, ColonyName = "Beta" },
                    new Colony { UUID = "colony-3", OwnerUUID = "other-player", ColonyName = "Gamma" },
                },
            };

            _playerContext = new PlayerContext(root);

            string tempFile = Path.Combine(
                Path.GetTempPath(),
                "oe2-test-prodsync-" + Guid.NewGuid().ToString("N") + ".dat");
            _tempFiles.Add(tempFile);

            var credManager = new GameApiCredentialManager(tempFile);
            var client = new GameApiClient("http://localhost:9999");
            var monitor = new GameApiConnectionMonitor(
                client, credManager, TestPlayerUUID, AppId, ClientId);

            _scheduler = new ProductionSyncScheduler(
                client, credManager, monitor, AppId, ClientId, _playerContext);
        }

        [TearDown]
        public void TearDown()
        {
            _scheduler?.Dispose();

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
        // Test 1: GetPlayerProfile returns profile from PlayerContext
        // Validates: Req 1.2
        // -------------------------------------------------------------------

        [Test]
        public void GetPlayerProfile_KnownUUID_ReturnsProfileFromPlayerContext()
        {
            var result = _scheduler.GetPlayerProfile(TestPlayerUUID);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.UUID, Is.EqualTo(TestPlayerUUID));
            Assert.That(result.Name, Is.EqualTo("TestPlayer"));

            // Verify same reference as PlayerContext returns
            var expected = _playerContext.FindMutablePlayerProfile(TestPlayerUUID);
            Assert.That(result, Is.SameAs(expected));
        }

        // -------------------------------------------------------------------
        // Test 2: GetPlayerProfile returns null for unknown UUID
        // Validates: Req 1.2
        // -------------------------------------------------------------------

        [Test]
        public void GetPlayerProfile_UnknownUUID_ReturnsNull()
        {
            var result = _scheduler.GetPlayerProfile(UnknownUUID);

            Assert.That(result, Is.Null);
        }

        // -------------------------------------------------------------------
        // Test 3: GetPlayerColonies returns filtered list
        // Validates: Req 1.3
        // -------------------------------------------------------------------

        [Test]
        public void GetPlayerColonies_ReturnsOnlyColoniesForOwner()
        {
            var result = _scheduler.GetPlayerColonies(TestPlayerUUID);

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.All(c => c.OwnerUUID == TestPlayerUUID), Is.True);
            Assert.That(result.Any(c => c.ColonyName == "Alpha"), Is.True);
            Assert.That(result.Any(c => c.ColonyName == "Beta"), Is.True);
        }

        // -------------------------------------------------------------------
        // Test 4: WriteContext calls PlayerContext.WriteContext
        // Validates: Req 1.4
        // -------------------------------------------------------------------

        [Test]
        public void WriteContext_DelegatesToPlayerContext()
        {
            // WriteContext with empty FilePath is a no-op (skipped) but does not throw.
            // This verifies the delegation path executes without error.
            Assert.DoesNotThrow(() => _scheduler.WriteContext());
        }

        // -------------------------------------------------------------------
        // Test 5: RaiseColonyDataChanged calls OnColonyDataChanged("")
        // Validates: Req 1.5
        // -------------------------------------------------------------------

        [Test]
        public void RaiseColonyDataChanged_FiresColonyDataChangedEvent()
        {
            string firedColonyUUID = null;
            bool eventFired = false;
            _playerContext.ColonyDataChanged += (sender, args) =>
            {
                eventFired = true;
                firedColonyUUID = args.ColonyUUID;
            };

            _scheduler.RaiseColonyDataChanged();

            Assert.That(eventFired, Is.True);
            Assert.That(firedColonyUUID, Is.EqualTo(string.Empty));
        }
    }
}
