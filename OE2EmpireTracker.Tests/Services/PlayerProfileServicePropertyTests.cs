using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for PlayerProfileService.
    /// Feature: BL-111 PlayerProfile Immutable Data Model
    /// Validates: Properties 4, 5, 6, 7 from the design document.
    /// </summary>
    [TestFixture]
    public class PlayerProfileServicePropertyTests
    {
        private PlayerContext playerContext;
        private PlayerProfileService service;

        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
            playerContext = PlayerContext.GetInstance();
            service = new PlayerProfileService(playerContext);
        }

        // -----------------------------------------------------------------------
        // Shared generators
        // -----------------------------------------------------------------------

        private static Gen<string> SafeStringGen()
        {
            return Gen.OneOf(
                Gen.Constant(string.Empty),
                Arb.From<NonEmptyString>().Generator.Select(s => s.Get));
        }

        private static Gen<DateTime> DateTimeGen()
        {
            return Gen.Choose(0, int.MaxValue)
                .Select(ticks => new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(ticks));
        }

        private static Gen<SkillUpdateData> SkillUpdateDataGen()
        {
            return from level in Gen.Choose(0, 20)
                   from training in Arb.From<bool>().Generator
                   from start in DateTimeGen()
                   from end in DateTimeGen()
                   select new SkillUpdateData
                   {
                       Level = level,
                       TrainingStarted = training,
                       CompletionStartTime = start,
                       CompletionEndTime = end,
                   };
        }

        private static Gen<Dictionary<string, SkillUpdateData>> SkillsDictGen()
        {
            var skillNames = (SkillName[])Enum.GetValues(typeof(SkillName));
            return from count in Gen.Choose(0, skillNames.Length)
                   from indices in Gen.ArrayOf(count, Gen.Choose(0, skillNames.Length - 1))
                   let distinctKeys = indices.Distinct().Select(i => skillNames[i].ToDisplayName()).ToArray()
                   from skills in Gen.Sequence(distinctKeys.Select(_ => SkillUpdateDataGen()))
                   select distinctKeys.Zip(skills, (k, v) => new { k, v })
                       .ToDictionary(x => x.k, x => x.v);
        }

        private static Gen<Dictionary<string, bool>> SkillGroupsDictGen()
        {
            var groupNames = (SkillGroupName[])Enum.GetValues(typeof(SkillGroupName));
            return from flags in Gen.ArrayOf(groupNames.Length, Arb.From<bool>().Generator)
                   select groupNames.Zip(flags, (g, f) => new { Key = g.ToDisplayName(), Value = f })
                       .ToDictionary(x => x.Key, x => x.Value);
        }

        private static Gen<PlayerProfileUpdateRequest> UpdateRequestGen()
        {
            return from name in SafeStringGen()
                   from faction in SafeStringGen()
                   from credits in Gen.Choose(0, 999999).Select(i => (decimal)i)
                   from skillPoints in Gen.Choose(0, 100)
                   from citizenId in SafeStringGen()
                   from regDate in SafeStringGen()
                   from activeTime in SafeStringGen()
                   from pubRank in Gen.Choose(0, 20)
                   from pubCurXP in Gen.Choose(0, 10000).Select(i => (long)i)
                   from pubNextXP in Gen.Choose(0, 10000).Select(i => (long)i)
                   from pubTitle in SafeStringGen()
                   from privRank in Gen.Choose(0, 20)
                   from privCurXP in Gen.Choose(0, 10000).Select(i => (long)i)
                   from privNextXP in Gen.Choose(0, 10000).Select(i => (long)i)
                   from privTitle in SafeStringGen()
                   from milRank in Gen.Choose(0, 20)
                   from milCurXP in Gen.Choose(0, 10000).Select(i => (long)i)
                   from milNextXP in Gen.Choose(0, 10000).Select(i => (long)i)
                   from milTitle in SafeStringGen()
                   from skills in SkillsDictGen()
                   from groups in SkillGroupsDictGen()
                   select new PlayerProfileUpdateRequest
                   {
                       Name = name,
                       Faction = faction,
                       TotalCredits = credits,
                       SkillPoints = skillPoints,
                       CitizenId = citizenId,
                       RegistrationDate = regDate,
                       ActiveTime = activeTime,
                       PublicRank = pubRank,
                       PublicCurrentXp = pubCurXP,
                       PublicXpToNextLevel = pubNextXP,
                       PublicRankName = pubTitle,
                       PrivateRank = privRank,
                       PrivateCurrentXp = privCurXP,
                       PrivateXpToNextLevel = privNextXP,
                       PrivateRankName = privTitle,
                       MilitaryRank = milRank,
                       MilitaryCurrentXp = milCurXP,
                       MilitaryXpToNextLevel = milNextXP,
                       MilitaryRankName = milTitle,
                       Skills = skills,
                       SkillGroups = groups,
                   };
        }

        private static Gen<PlayerProfileCreateRequest> CreateRequestGen()
        {
            return from name in SafeStringGen()
                   from faction in SafeStringGen()
                   from credits in Gen.Choose(0, 999999).Select(i => (decimal)i)
                   from skillPoints in Gen.Choose(0, 100)
                   from citizenId in SafeStringGen()
                   from regDate in SafeStringGen()
                   from activeTime in SafeStringGen()
                   from pubRank in Gen.Choose(0, 20)
                   from pubCurXP in Gen.Choose(0, 10000).Select(i => (long)i)
                   from pubNextXP in Gen.Choose(0, 10000).Select(i => (long)i)
                   from pubTitle in SafeStringGen()
                   from privRank in Gen.Choose(0, 20)
                   from privCurXP in Gen.Choose(0, 10000).Select(i => (long)i)
                   from privNextXP in Gen.Choose(0, 10000).Select(i => (long)i)
                   from privTitle in SafeStringGen()
                   from milRank in Gen.Choose(0, 20)
                   from milCurXP in Gen.Choose(0, 10000).Select(i => (long)i)
                   from milNextXP in Gen.Choose(0, 10000).Select(i => (long)i)
                   from milTitle in SafeStringGen()
                   from skills in SkillsDictGen()
                   from groups in SkillGroupsDictGen()
                   select new PlayerProfileCreateRequest
                   {
                       Name = name,
                       Faction = faction,
                       TotalCredits = credits,
                       SkillPoints = skillPoints,
                       CitizenId = citizenId,
                       RegistrationDate = regDate,
                       ActiveTime = activeTime,
                       PublicRank = pubRank,
                       PublicCurrentXp = pubCurXP,
                       PublicXpToNextLevel = pubNextXP,
                       PublicRankName = pubTitle,
                       PrivateRank = privRank,
                       PrivateCurrentXp = privCurXP,
                       PrivateXpToNextLevel = privNextXP,
                       PrivateRankName = privTitle,
                       MilitaryRank = milRank,
                       MilitaryCurrentXp = milCurXP,
                       MilitaryXpToNextLevel = milNextXP,
                       MilitaryRankName = milTitle,
                       Skills = skills,
                       SkillGroups = groups,
                   };
        }

        private PlayerProfile CreateSeedProfile()
        {
            var profile = new PlayerProfile
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "SeedProfile",
            };
            playerContext.AddPlayerProfile(profile);
            return profile;
        }

        // -----------------------------------------------------------------------
        // Property 4: Service.Update round-trip
        // Feature: BL-111, Property 4: Service.Update round-trip
        // **Validates: Requirements 14.3, 14.4, 14.5, 14.6, 14.9**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Update_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(Arb.From(UpdateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                var svc = new PlayerProfileService(ctx);

                var seed = new PlayerProfile { UUID = Guid.NewGuid().ToString(), Name = "Seed" };
                ctx.AddPlayerProfile(seed);

                var result = svc.Update(seed.UUID, request);

                if (result.Name != request.Name) return false;
                if (result.Faction != request.Faction) return false;
                if (result.TotalCredits != request.TotalCredits) return false;
                if (result.SkillPoints != request.SkillPoints) return false;
                if (result.CitizenId != request.CitizenId) return false;
                if (result.RegistrationDate != request.RegistrationDate) return false;
                if (result.ActiveTime != request.ActiveTime) return false;

                if (result.Public.Rank != request.PublicRank) return false;
                if (result.Public.CurrentXp != request.PublicCurrentXp) return false;
                if (result.Public.XpToNextLevel != request.PublicXpToNextLevel) return false;
                if (result.Public.RankName != request.PublicRankName) return false;

                if (result.Private.Rank != request.PrivateRank) return false;
                if (result.Private.CurrentXp != request.PrivateCurrentXp) return false;
                if (result.Private.XpToNextLevel != request.PrivateXpToNextLevel) return false;
                if (result.Private.RankName != request.PrivateRankName) return false;

                if (result.Military.Rank != request.MilitaryRank) return false;
                if (result.Military.CurrentXp != request.MilitaryCurrentXp) return false;
                if (result.Military.XpToNextLevel != request.MilitaryXpToNextLevel) return false;
                if (result.Military.RankName != request.MilitaryRankName) return false;

                // Skills
                if (request.Skills != null)
                {
                    foreach (var kvp in request.Skills)
                    {
                        var roSkill = result.GetSkill(kvp.Key);
                        if (roSkill.Level != kvp.Value.Level) return false;
                        if (roSkill.TrainingStarted != kvp.Value.TrainingStarted) return false;
                        if (roSkill.CompletionStartTime != kvp.Value.CompletionStartTime) return false;
                        if (roSkill.CompletionEndTime != kvp.Value.CompletionEndTime) return false;
                    }
                }

                // Skill groups
                if (request.SkillGroups != null)
                {
                    foreach (var kvp in request.SkillGroups)
                    {
                        if (result.GetSkillGroup(kvp.Key) != kvp.Value) return false;
                    }
                }

                return true;
            });
        }

        // -----------------------------------------------------------------------
        // Property 5: Service.Create round-trip
        // Feature: BL-111, Property 5: Service.Create round-trip
        // **Validates: Requirements 15.1, 15.2, 15.3, 15.4, 15.5, 15.6, 15.7**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Create_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(Arb.From(CreateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                var svc = new PlayerProfileService(ctx);

                var result = svc.Create(request);

                if (string.IsNullOrEmpty(result.UUID)) return false;
                if (result.Name != request.Name) return false;
                if (result.Faction != request.Faction) return false;
                if (result.TotalCredits != request.TotalCredits) return false;
                if (result.SkillPoints != request.SkillPoints) return false;
                if (result.CitizenId != request.CitizenId) return false;
                if (result.RegistrationDate != request.RegistrationDate) return false;
                if (result.ActiveTime != request.ActiveTime) return false;

                if (result.Public.Rank != request.PublicRank) return false;
                if (result.Public.CurrentXp != request.PublicCurrentXp) return false;
                if (result.Public.XpToNextLevel != request.PublicXpToNextLevel) return false;
                if (result.Public.RankName != request.PublicRankName) return false;

                if (result.Private.Rank != request.PrivateRank) return false;
                if (result.Private.CurrentXp != request.PrivateCurrentXp) return false;
                if (result.Private.XpToNextLevel != request.PrivateXpToNextLevel) return false;
                if (result.Private.RankName != request.PrivateRankName) return false;

                if (result.Military.Rank != request.MilitaryRank) return false;
                if (result.Military.CurrentXp != request.MilitaryCurrentXp) return false;
                if (result.Military.XpToNextLevel != request.MilitaryXpToNextLevel) return false;
                if (result.Military.RankName != request.MilitaryRankName) return false;

                // Skills
                if (request.Skills != null)
                {
                    foreach (var kvp in request.Skills)
                    {
                        var roSkill = result.GetSkill(kvp.Key);
                        if (roSkill.Level != kvp.Value.Level) return false;
                        if (roSkill.TrainingStarted != kvp.Value.TrainingStarted) return false;
                        if (roSkill.CompletionStartTime != kvp.Value.CompletionStartTime) return false;
                        if (roSkill.CompletionEndTime != kvp.Value.CompletionEndTime) return false;
                    }
                }

                // Skill groups
                if (request.SkillGroups != null)
                {
                    foreach (var kvp in request.SkillGroups)
                    {
                        if (result.GetSkillGroup(kvp.Key) != kvp.Value) return false;
                    }
                }

                return true;
            });
        }

        // -----------------------------------------------------------------------
        // Property 6: Service.Delete removes profile
        // Feature: BL-111, Property 6: Service.Delete removes profile
        // **Validates: Requirements 16.1, 16.2, 16.3, 16.4, 16.5**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Delete_RemovesProfile()
        {
            return Prop.ForAll(Arb.From(CreateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                var svc = new PlayerProfileService(ctx);

                var created = svc.Create(request);
                string uuid = created.UUID;

                svc.Delete(uuid);

                var found = ctx.FindMutablePlayerProfile(uuid);
                return (found == null).Label(
                    found == null ? "OK" : "Profile still exists after Delete");
            });
        }

        // -----------------------------------------------------------------------
        // Property 7: Service.Import preserves UUID on name match
        // Feature: BL-111, Property 7: Service.Import preserves UUID on name match
        // **Validates: Requirements 17.2, 17.3**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Import_PreservesUUID_OnNameMatch()
        {
            return Prop.ForAll(Arb.From(CreateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                var svc = new PlayerProfileService(ctx);

                // Ensure the profile has a non-empty name for matching
                if (string.IsNullOrEmpty(request.Name))
                {
                    request.Name = "TestProfile";
                }

                var created = svc.Create(request);
                string originalUuid = created.UUID;

                // Build a temp profile with the same name (different case)
                var tempProfile = new PlayerProfile
                {
                    Name = request.Name.ToUpperInvariant(),
                    Faction = "ImportedFaction",
                    TotalCredits = 777m,
                    SkillPoints = 99,
                };

                var imported = svc.Import(tempProfile);

                // UUID should be preserved
                if (imported.UUID != originalUuid) return false;

                // Fields should be updated from the temp profile
                if (imported.Faction != "ImportedFaction") return false;
                if (imported.TotalCredits != 777m) return false;
                if (imported.SkillPoints != 99) return false;

                return true;
            });
        }
    }
}