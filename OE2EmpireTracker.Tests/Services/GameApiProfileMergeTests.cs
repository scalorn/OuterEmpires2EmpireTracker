// <copyright file="GameApiProfileMergeTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for GameApiSyncScheduler.MergeProfileData.
    /// Feature: game-api-integration, Property 3: Merge Idempotency.
    /// **Validates: Requirements 6.1, 6.2**
    /// </summary>
    [TestFixture]
    public class GameApiProfileMergeTests
    {
        // -------------------------------------------------------------------
        // Generators
        // -------------------------------------------------------------------

        private static Gen<string> NonNullStringGen()
        {
            return Gen.Elements(
                "Alpha", "Beta", "Gamma", "Delta", "Epsilon",
                "Faction1", "Faction2", "CID-001", "CID-002", "CID-999");
        }

        private static Gen<int> SkillPointsGen()
        {
            return Gen.Choose(0, 10000);
        }

        private static Gen<PlayerProfile> LocalProfileGen()
        {
            return from faction in NonNullStringGen()
                   from citizenId in NonNullStringGen()
                   from skillPoints in SkillPointsGen()
                   from skillGroupCount in Gen.Choose(0, 3)
                   from groupNames in Gen.ListOf(
                       skillGroupCount,
                       Gen.Elements("Colony Director", "Commander", "Engineer", "Trader"))
                   select BuildLocalProfile(
                       faction,
                       citizenId,
                       skillPoints,
                       new List<string>(groupNames));
        }

        private static Gen<GameApiProfileResponse> RemoteProfileGen()
        {
            return from faction in NonNullStringGen()
                   from citizenId in NonNullStringGen()
                   from skillPoints in SkillPointsGen()
                   select new GameApiProfileResponse
                   {
                       Faction = faction,
                       CitizenId = citizenId,
                       SkillPoints = skillPoints,
                   };
        }

        private static PlayerProfile BuildLocalProfile(
            string faction,
            string citizenId,
            int skillPoints,
            IList<string> groupNames)
        {
            var profile = new PlayerProfile
            {
                UUID = "test-uuid",
                Name = "TestPlayer",
                Faction = faction,
                CitizenId = citizenId,
                SkillPoints = skillPoints,
            };

            foreach (string group in groupNames)
            {
                profile.SetSkillGroup(group, true);
            }

            return profile;
        }

        // -------------------------------------------------------------------
        // Property 1: API-authoritative fields are overwritten
        // **Validates: Requirements 6.1**
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ApiAuthoritativeFieldsOverwritten()
        {
            return Prop.ForAll(
                Arb.From(LocalProfileGen()),
                Arb.From(RemoteProfileGen()),
                (local, remote) =>
                {
                    GameApiSyncScheduler.MergeProfileData(local, remote);

                    return (local.Faction == remote.Faction)
                        .Label("Faction overwritten")
                        .And(local.CitizenId == remote.CitizenId)
                        .Label("CitizenId overwritten")
                        .And(local.SkillPoints == remote.SkillPoints)
                        .Label("SkillPoints overwritten");
                });
        }

        // -------------------------------------------------------------------
        // Property 2: Local-only fields preserved (SkillGroups)
        // **Validates: Requirements 6.2**
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property LocalOnlyFieldsPreserved()
        {
            return Prop.ForAll(
                Arb.From(LocalProfileGen()),
                Arb.From(RemoteProfileGen()),
                (local, remote) =>
                {
                    // Capture SkillGroups state before merge
                    var groupsBefore = new Dictionary<string, bool>();
                    string[] testGroups = { "Colony Director", "Commander", "Engineer", "Trader" };
                    foreach (string g in testGroups)
                    {
                        groupsBefore[g] = local.GetSkillGroup(g);
                    }

                    GameApiSyncScheduler.MergeProfileData(local, remote);

                    // Verify SkillGroups unchanged after merge
                    bool allPreserved = true;
                    foreach (string g in testGroups)
                    {
                        if (local.GetSkillGroup(g) != groupsBefore[g])
                        {
                            allPreserved = false;
                        }
                    }

                    return allPreserved.Label("SkillGroups preserved after merge");
                });
        }

        // -------------------------------------------------------------------
        // Property 3: Idempotent application (merge twice = same result)
        // **Validates: Requirements 6.1, 6.2**
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MergeIsIdempotent()
        {
            return Prop.ForAll(
                Arb.From(LocalProfileGen()),
                Arb.From(RemoteProfileGen()),
                (local, remote) =>
                {
                    // First merge
                    GameApiSyncScheduler.MergeProfileData(local, remote);

                    // Capture state after first merge
                    string factionAfterFirst = local.Faction;
                    string citizenIdAfterFirst = local.CitizenId;
                    int skillPointsAfterFirst = local.SkillPoints;

                    // Second merge with same remote data
                    bool changedOnSecond = GameApiSyncScheduler.MergeProfileData(local, remote);

                    return (!changedOnSecond)
                        .Label("Second merge reports no changes")
                        .And(local.Faction == factionAfterFirst)
                        .Label("Faction unchanged on second merge")
                        .And(local.CitizenId == citizenIdAfterFirst)
                        .Label("CitizenId unchanged on second merge")
                        .And(local.SkillPoints == skillPointsAfterFirst)
                        .Label("SkillPoints unchanged on second merge");
                });
        }

        // -------------------------------------------------------------------
        // Property 4: Conflict logging — returns true when fields differ
        // **Validates: Requirements 6.1**
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MergeReturnsTrueWhenFieldsDiffer()
        {
            return Prop.ForAll(
                Arb.From(NonNullStringGen().Select(f => new PlayerProfile
                {
                    UUID = "test-uuid",
                    Faction = f,
                    CitizenId = "OLD-CID",
                    SkillPoints = 100,
                })),
                Arb.From(NonNullStringGen()
                    .Where(f => f != "OLD-CID")
                    .Select(cid => new GameApiProfileResponse
                    {
                        Faction = null,
                        CitizenId = cid,
                        SkillPoints = 100,
                    })),
                (local, remote) =>
                {
                    bool changed = GameApiSyncScheduler.MergeProfileData(local, remote);
                    return changed.Label("Merge returns true when CitizenId differs");
                });
        }

        // -------------------------------------------------------------------
        // Unit test: Merge returns false when all fields already match
        // -------------------------------------------------------------------

        [Test]
        public void MergeReturnsFalseWhenNoFieldsDiffer()
        {
            var local = new PlayerProfile
            {
                UUID = "test-uuid",
                Faction = "TestFaction",
                CitizenId = "CID-123",
                SkillPoints = 500,
            };

            var remote = new GameApiProfileResponse
            {
                Faction = "TestFaction",
                CitizenId = "CID-123",
                SkillPoints = 500,
            };

            bool changed = GameApiSyncScheduler.MergeProfileData(local, remote);

            Assert.That(changed, Is.False, "Merge should return false when no fields differ");
        }

        // -------------------------------------------------------------------
        // Unit test: Null local or remote returns false
        // -------------------------------------------------------------------

        [Test]
        public void MergeReturnsfalseForNullInputs()
        {
            var local = new PlayerProfile { UUID = "test" };
            var remote = new GameApiProfileResponse { Faction = "X" };

            Assert.That(GameApiSyncScheduler.MergeProfileData(null, remote), Is.False);
            Assert.That(GameApiSyncScheduler.MergeProfileData(local, null), Is.False);
            Assert.That(GameApiSyncScheduler.MergeProfileData(null, null), Is.False);
        }
    }
}
