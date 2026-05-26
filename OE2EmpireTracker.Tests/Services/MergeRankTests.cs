// <copyright file="MergeRankTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Reflection;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for GameApiSyncScheduler.MergeRank with new fields.
    /// **Validates: Requirements 4.3, 5.4, 9.4, 9.5**
    /// </summary>
    [TestFixture]
    public class MergeRankTests
    {
        private static readonly MethodInfo MergeRankMethod = typeof(GameApiSyncScheduler)
            .GetMethod("MergeRank", BindingFlags.NonPublic | BindingFlags.Static);

        private static bool InvokeMergeRank(PlayerRank localRank, GameApiRankResponse remoteRank, string rankName)
        {
            return (bool)MergeRankMethod.Invoke(null, new object[] { localRank, remoteRank, rankName });
        }

        [Test]
        public void LevelName_NonNull_SetsLocalRankName()
        {
            var local = new PlayerRank { RankName = "OldRank" };
            var remote = new GameApiRankResponse { LevelName = "NewRank" };

            bool changed = InvokeMergeRank(local, remote, "Public");

            Assert.That(local.RankName, Is.EqualTo("NewRank"));
            Assert.That(changed, Is.True);
        }

        [Test]
        public void LevelName_Null_LeavesLocalRankNameUnchanged()
        {
            var local = new PlayerRank { RankName = "ExistingRank" };
            var remote = new GameApiRankResponse { LevelName = null };

            InvokeMergeRank(local, remote, "Public");

            Assert.That(local.RankName, Is.EqualTo("ExistingRank"));
        }

        [Test]
        public void XpToNextLevel_Different_SetsLocalXpToNextLevel()
        {
            var local = new PlayerRank { XpToNextLevel = 100 };
            var remote = new GameApiRankResponse { XpToNextLevel = 500 };

            bool changed = InvokeMergeRank(local, remote, "Private");

            Assert.That(local.XpToNextLevel, Is.EqualTo(500));
            Assert.That(changed, Is.True);
        }

        [Test]
        public void CurrentXp_Different_SetsLocalCurrentXp()
        {
            var local = new PlayerRank { CurrentXp = 200 };
            var remote = new GameApiRankResponse { CurrentXp = 750 };

            bool changed = InvokeMergeRank(local, remote, "Military");

            Assert.That(local.CurrentXp, Is.EqualTo(750));
            Assert.That(changed, Is.True);
        }

        [Test]
        public void AllFieldsSame_ReturnsFalse()
        {
            var local = new PlayerRank
            {
                Rank = 5,
                RankName = "Captain",
                XpToNextLevel = 1000,
                CurrentXp = 300,
            };

            var remote = new GameApiRankResponse
            {
                Level = 5,
                LevelName = "Captain",
                XpToNextLevel = 1000,
                CurrentXp = 300,
            };

            bool changed = InvokeMergeRank(local, remote, "Public");

            Assert.That(changed, Is.False);
        }

        [Test]
        public void NullLocalRank_ReturnsFalse()
        {
            var remote = new GameApiRankResponse { Level = 3, LevelName = "Ensign" };

            bool changed = InvokeMergeRank(null, remote, "Public");

            Assert.That(changed, Is.False);
        }

        [Test]
        public void NullRemoteRank_ReturnsFalse()
        {
            var local = new PlayerRank { Rank = 2, RankName = "Cadet" };

            bool changed = InvokeMergeRank(local, null, "Public");

            Assert.That(changed, Is.False);
        }
    }
}
