// <copyright file="ProfileMergeServiceTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for ProfileMergeService.
    /// Validates: Req 1 Criteria 1.2, 1.3, 1.4, 1.5
    /// Feature: queue-sync-completion
    /// </summary>
    [TestFixture]
    public class ProfileMergeServiceTests
    {
        // ---------------------------------------------------------------
        // Generators
        // ---------------------------------------------------------------

        private static Gen<string> NonNullStringGen()
        {
            return from len in Gen.Choose(1, 20)
                   from chars in Gen.ListOf(len, Gen.Elements(
                       'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
                       'A', 'B', 'C', 'D', 'E', 'F', '0', '1', '2', '3'))
                   select new string(chars.ToArray());
        }

        private static Gen<GameApiRankResponse> RankResponseGen()
        {
            return from level in Gen.Choose(0, 50)
                   from levelName in NonNullStringGen()
                   from xpToNext in Gen.Choose(0, 100000)
                   from currentXp in Gen.Choose(0, 100000)
                   select new GameApiRankResponse
                   {
                       Level = level,
                       LevelName = levelName,
                       XpToNextLevel = xpToNext,
                       CurrentXp = currentXp,
                   };
        }

        private static Gen<GameApiRanksResponse> RanksResponseGen()
        {
            return from pub in RankResponseGen()
                   from priv in RankResponseGen()
                   from mil in RankResponseGen()
                   select new GameApiRanksResponse
                   {
                       Public = pub,
                       Private = priv,
                       Military = mil,
                   };
        }

        private static Gen<GameApiSkillResponse> SkillResponseGen()
        {
            return from level in Gen.Choose(0, 30)
                   from skillId in Gen.Choose(1, 500)
                   from effectDesc in NonNullStringGen()
                   from amountPerLevel in Gen.Choose(0, 10)
                   from groupName in NonNullStringGen()
                   from isUnlocked in Arb.Generate<bool>()
                   select new GameApiSkillResponse
                   {
                       Level = level,
                       SkillId = skillId,
                       EffectDescription = effectDesc,
                       AmountPerLevel = amountPerLevel,
                       SkillGroupName = groupName,
                       IsUnlocked = isUnlocked,
                   };
        }

        private static readonly string[] SkillNames = new[]
        {
            "Mining", "Refining", "Engineering", "Combat", "Navigation",
        };

        private static Gen<Dictionary<string, GameApiSkillResponse>> SkillsDictGen()
        {
            return from count in Gen.Choose(1, 5)
                   from skills in Gen.ListOf(count, SkillResponseGen())
                   let dict = skills
                       .Select((s, i) => new { Key = SkillNames[i % SkillNames.Length], Value = s })
                       .GroupBy(x => x.Key)
                       .ToDictionary(g => g.Key, g => g.First().Value)
                   select dict;
        }

        private static Gen<GameApiProfileResponse> ProfileResponseGen()
        {
            return from faction in NonNullStringGen()
                   from citizenId in NonNullStringGen()
                   from skillPoints in Gen.Choose(0, 1000)
                   from characterId in Gen.Choose(1, 99999)
                   from firstName in NonNullStringGen()
                   from lastName in NonNullStringGen()
                   from activeMinutes in Gen.Choose(0, 100000)
                   from ranks in RanksResponseGen()
                   from skills in SkillsDictGen()
                   select new GameApiProfileResponse
                   {
                       Faction = faction,
                       CitizenId = citizenId,
                       SkillPoints = skillPoints,
                       CharacterId = characterId,
                       FirstName = firstName,
                       LastName = lastName,
                       ActiveTimeMinutes = activeMinutes,
                       Ranks = ranks,
                       Skills = skills,
                       SkillInTraining = null,
                   };
        }

        private static PlayerProfile CreateFreshProfile()
        {
            return new PlayerProfile
            {
                UUID = Guid.NewGuid().ToString(),
                Faction = "InitialFaction",
                CitizenId = "CIT-000",
                SkillPoints = 0,
                CharacterId = 0,
                FirstName = "Initial",
                LastName = "Profile",
                ActiveTimeMinutes = 0,
            };
        }

        // ---------------------------------------------------------------
        // Property 1: Idempotent Merge
        // Applying the same API response twice to the same local state
        // SHALL produce identical results.
        // Validates: Req 1 Criteria 1.2, 1.3, 1.4, 1.5
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 1a: After applying the same response twice, the second
        /// call returns false (no additional changes).
        /// **Validates: Requirements 1.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property IdempotentMerge_SecondCallReturnsFalse()
        {
            return Prop.ForAll(ProfileResponseGen().ToArbitrary(), (remote) =>
            {
                var local = CreateFreshProfile();

                ProfileMergeService.MergeProfileData(local, remote);
                bool secondResult = ProfileMergeService.MergeProfileData(local, remote);

                return (!secondResult)
                    .Label("Second merge returned true (expected false for idempotent merge)");
            });
        }

        /// <summary>
        /// Property 1b: After applying the same response twice, all
        /// API-wins fields have the same values as after the first merge.
        /// **Validates: Requirements 1.2, 1.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property IdempotentMerge_StateIdenticalAfterSecondMerge()
        {
            return Prop.ForAll(ProfileResponseGen().ToArbitrary(), (remote) =>
            {
                var local1 = CreateFreshProfile();
                ProfileMergeService.MergeProfileData(local1, remote);

                var local2 = CreateFreshProfile();
                ProfileMergeService.MergeProfileData(local2, remote);
                ProfileMergeService.MergeProfileData(local2, remote);

                var factionMatch = local1.Faction == local2.Faction;
                var citizenMatch = local1.CitizenId == local2.CitizenId;
                var skillPtsMatch = local1.SkillPoints == local2.SkillPoints;
                var charIdMatch = local1.CharacterId == local2.CharacterId;
                var firstNameMatch = local1.FirstName == local2.FirstName;
                var lastNameMatch = local1.LastName == local2.LastName;
                var activeMinMatch = local1.ActiveTimeMinutes == local2.ActiveTimeMinutes;

                return (factionMatch && citizenMatch && skillPtsMatch
                    && charIdMatch && firstNameMatch && lastNameMatch && activeMinMatch)
                    .Label("State diverged after second merge");
            });
        }

        // ---------------------------------------------------------------
        // Property 2: API Wins — overwrite fields
        // The merge SHALL overwrite local values with API values for all
        // game-authoritative fields.
        // Validates: Req 1 Criteria 1.3
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 2: After merge, all API-wins fields on local match the
        /// remote response values.
        /// **Validates: Requirements 1.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ApiWins_FieldsOverwriteLocal()
        {
            return Prop.ForAll(ProfileResponseGen().ToArbitrary(), (remote) =>
            {
                var local = CreateFreshProfile();
                ProfileMergeService.MergeProfileData(local, remote);

                var factionMatch = local.Faction == remote.Faction;
                var citizenMatch = local.CitizenId == remote.CitizenId;
                var skillPtsMatch = local.SkillPoints == remote.SkillPoints;
                var charIdMatch = local.CharacterId == remote.CharacterId;
                var firstNameMatch = local.FirstName == remote.FirstName;
                var lastNameMatch = local.LastName == remote.LastName;

                // ActiveTimeMinutes clamps negatives to 0
                int expectedMinutes = remote.ActiveTimeMinutes < 0
                    ? 0 : remote.ActiveTimeMinutes;
                var activeMinMatch = local.ActiveTimeMinutes == expectedMinutes;

                return (factionMatch && citizenMatch && skillPtsMatch
                    && charIdMatch && firstNameMatch && lastNameMatch && activeMinMatch)
                    .Label($"API-wins field mismatch: Faction={factionMatch}, " +
                           $"CitizenId={citizenMatch}, SkillPts={skillPtsMatch}, " +
                           $"CharId={charIdMatch}, FirstName={firstNameMatch}, " +
                           $"LastName={lastNameMatch}, ActiveMin={activeMinMatch}");
            });
        }

