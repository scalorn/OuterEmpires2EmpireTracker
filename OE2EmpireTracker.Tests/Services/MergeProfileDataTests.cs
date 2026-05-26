// <copyright file="MergeProfileDataTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for MergeProfileData new fields: CharacterId, FirstName, LastName, ActiveTimeMinutes.
    /// **Validates: Requirements 1.2, 1.3, 2.2, 2.3, 3.2, 3.3, 3.4, 9.1, 9.2, 9.3**
    /// </summary>
    [TestFixture]
    public class MergeProfileDataTests
    {
        // -------------------------------------------------------------------
        // CharacterId merge (Req 1.2, 1.3, 9.1)
        // -------------------------------------------------------------------

        [Test]
        public void CharacterId_RemoteNonZero_SetsLocal()
        {
            var local = new PlayerProfile { UUID = "test", CharacterId = 0 };
            var remote = new GameApiProfileResponse { CharacterId = 42 };

            bool changed = GameApiSyncScheduler.MergeProfileData(local, remote);

            Assert.That(local.CharacterId, Is.EqualTo(42));
            Assert.That(changed, Is.True);
        }

        [Test]
        public void CharacterId_RemoteZero_LeavesLocalUnchanged()
        {
            var local = new PlayerProfile { UUID = "test", CharacterId = 99 };
            var remote = new GameApiProfileResponse { CharacterId = 0 };

            bool changed = GameApiSyncScheduler.MergeProfileData(local, remote);

            Assert.That(local.CharacterId, Is.EqualTo(99));
        }

        [Test]
        public void CharacterId_RemoteSameAsLocal_NoChange()
        {
            var local = new PlayerProfile { UUID = "test", CharacterId = 7 };
            var remote = new GameApiProfileResponse { CharacterId = 7 };

            bool changed = GameApiSyncScheduler.MergeProfileData(local, remote);

            Assert.That(local.CharacterId, Is.EqualTo(7));
            Assert.That(changed, Is.False);
        }

        // -------------------------------------------------------------------
        // FirstName merge (Req 2.2, 2.3, 9.2)
        // -------------------------------------------------------------------

        [Test]
        public void FirstName_RemoteNonNull_SetsLocal()
        {
            var local = new PlayerProfile { UUID = "test", FirstName = string.Empty };
            var remote = new GameApiProfileResponse { FirstName = "Alice" };

            bool changed = GameApiSyncScheduler.MergeProfileData(local, remote);

            Assert.That(local.FirstName, Is.EqualTo("Alice"));
            Assert.That(changed, Is.True);
        }

        [Test]
        public void FirstName_RemoteNull_LeavesLocalUnchanged()
        {
            var local = new PlayerProfile { UUID = "test", FirstName = "Bob" };
            var remote = new GameApiProfileResponse { FirstName = null };

            bool changed = GameApiSyncScheduler.MergeProfileData(local, remote);

            Assert.That(local.FirstName, Is.EqualTo("Bob"));
        }

        [Test]
        public void FirstName_RemoteSameAsLocal_NoChange()
        {
            var local = new PlayerProfile { UUID = "test", FirstName = "Charlie" };
            var remote = new GameApiProfileResponse { FirstName = "Charlie" };

            bool changed = GameApiSyncScheduler.MergeProfileData(local, remote);

            Assert.That(local.FirstName, Is.EqualTo("Charlie"));
            Assert.That(changed, Is.False);
        }

        // -------------------------------------------------------------------
        // LastName merge (Req 2.2, 2.3, 9.2)
        // -------------------------------------------------------------------

        [Test]
        public void LastName_RemoteNonNull_SetsLocal()
        {
            var local = new PlayerProfile { UUID = "test", LastName = string.Empty };
            var remote = new GameApiProfileResponse { LastName = "Smith" };

            bool changed = GameApiSyncScheduler.MergeProfileData(local, remote);

            Assert.That(local.LastName, Is.EqualTo("Smith"));
            Assert.That(changed, Is.True);
        }

        [Test]
        public void LastName_RemoteNull_LeavesLocalUnchanged()
        {
            var local = new PlayerProfile { UUID = "test", LastName = "Jones" };
            var remote = new GameApiProfileResponse { LastName = null };

            bool changed = GameApiSyncScheduler.MergeProfileData(local, remote);

            Assert.That(local.LastName, Is.EqualTo("Jones"));
        }

        [Test]
        public void LastName_RemoteSameAsLocal_NoChange()
        {
            var local = new PlayerProfile { UUID = "test", LastName = "Williams" };
            var remote = new GameApiProfileResponse { LastName = "Williams" };

            bool changed = GameApiSyncScheduler.MergeProfileData(local, remote);

            Assert.That(local.LastName, Is.EqualTo("Williams"));
            Assert.That(changed, Is.False);
        }

        // -------------------------------------------------------------------
        // ActiveTimeMinutes merge (Req 3.2, 3.3, 3.4, 9.3)
        // -------------------------------------------------------------------

        [Test]
        public void ActiveTimeMinutes_RemotePositive_SetsLocal()
        {
            var local = new PlayerProfile { UUID = "test", ActiveTimeMinutes = 0 };
            var remote = new GameApiProfileResponse { ActiveTimeMinutes = 120 };

            bool changed = GameApiSyncScheduler.MergeProfileData(local, remote);

            Assert.That(local.ActiveTimeMinutes, Is.EqualTo(120));
            Assert.That(changed, Is.True);
        }

        [Test]
        public void ActiveTimeMinutes_RemoteNegative_ClampsToZero()
        {
            var local = new PlayerProfile { UUID = "test", ActiveTimeMinutes = 50 };
            var remote = new GameApiProfileResponse { ActiveTimeMinutes = -10 };

            bool changed = GameApiSyncScheduler.MergeProfileData(local, remote);

            Assert.That(local.ActiveTimeMinutes, Is.EqualTo(0));
            Assert.That(changed, Is.True);
        }

        [Test]
        public void ActiveTimeMinutes_RemoteZeroWithLocalZero_NoChange()
        {
            var local = new PlayerProfile { UUID = "test", ActiveTimeMinutes = 0 };
            var remote = new GameApiProfileResponse { ActiveTimeMinutes = 0 };

            bool changed = GameApiSyncScheduler.MergeProfileData(local, remote);

            Assert.That(local.ActiveTimeMinutes, Is.EqualTo(0));
            Assert.That(changed, Is.False);
        }

        [Test]
        public void ActiveTimeMinutes_RemoteZeroWithLocalNonZero_SetsToZero()
        {
            var local = new PlayerProfile { UUID = "test", ActiveTimeMinutes = 300 };
            var remote = new GameApiProfileResponse { ActiveTimeMinutes = 0 };

            bool changed = GameApiSyncScheduler.MergeProfileData(local, remote);

            Assert.That(local.ActiveTimeMinutes, Is.EqualTo(0));
            Assert.That(changed, Is.True);
        }
    }
}
