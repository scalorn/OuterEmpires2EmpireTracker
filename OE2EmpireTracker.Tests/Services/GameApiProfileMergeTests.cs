// <copyright file="GameApiProfileMergeTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Common.Client.Generated;
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

        private static Gen<PublicCharacter> RemoteProfileGen()
        {
            return from characterId in Gen.Choose(1, 99999)
                   from firstName in NonNullStringGen()
                   from lastName in NonNullStringGen()
                   from activeMinutes in Gen.Choose(0, 100000)
                   select new PublicCharacter
                   {
                       CharacterId = characterId,
                       FirstName = firstName,
                       LastName = lastName,
                       ActiveTimeMinutes = activeMinutes,
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

                    return (local.CharacterId == remote.CharacterId)
                        .Label("CharacterId overwritten")
                        .And(local.FirstName == remote.FirstName)
                        .Label("FirstName overwritten")
                        .And(local.LastName == remote.LastName)
                        .Label("LastName overwritten");
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
                    string firstNameAfterFirst = local.FirstName;
                    string lastNameAfterFirst = local.LastName;
                    int charIdAfterFirst = local.CharacterId;

                    // Second merge with same remote data
                    bool changedOnSecond = GameApiSyncScheduler.MergeProfileData(local, remote);

                    return (!changedOnSecond)
                        .Label("Second merge reports no changes")
                        .And(local.FirstName == firstNameAfterFirst)
                        .Label("FirstName unchanged on second merge")
                        .And(local.LastName == lastNameAfterFirst)
                        .Label("LastName unchanged on second merge")
                        .And(local.CharacterId == charIdAfterFirst)
                        .Label("CharacterId unchanged on second merge");
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
                    FirstName = f,
                    LastName = "OLD-LAST",
                    CharacterId = 100,
                })),
                Arb.From(NonNullStringGen()
                    .Where(f => f != "OLD-LAST")
                    .Select(ln => new PublicCharacter
                    {
                        FirstName = null,
                        LastName = ln,
                        CharacterId = 100,
                    })),
                (local, remote) =>
                {
                    bool changed = GameApiSyncScheduler.MergeProfileData(local, remote);
                    return changed.Label("Merge returns true when LastName differs");
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
                CharacterId = 500,
                FirstName = "TestFirst",
                LastName = "TestLast",
                ActiveTimeMinutes = 1000,
            };

            var remote = new PublicCharacter
            {
                CharacterId = 500,
                FirstName = "TestFirst",
                LastName = "TestLast",
                ActiveTimeMinutes = 1000,
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
            var remote = new PublicCharacter { FirstName = "X" };

            Assert.That(GameApiSyncScheduler.MergeProfileData(null, remote), Is.False);
            Assert.That(GameApiSyncScheduler.MergeProfileData(local, null), Is.False);
            Assert.That(GameApiSyncScheduler.MergeProfileData(null, null), Is.False);
        }
    }
}
