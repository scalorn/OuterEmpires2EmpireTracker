using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    /// <summary>
    /// Property-based tests for PlayerProfileViewModel edit buffer.
    /// Feature: BL-111 PlayerProfile Immutable Data Model
    /// Validates: Properties 1, 2, 3 from the design document.
    /// </summary>
    [TestFixture]
    public class PlayerProfileViewModelPropertyTests
    {
        private PlayerContext playerContext;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            TestHelper.ResetWithCachedData();
            playerContext = PlayerContext.GetInstance();
        }

        // -----------------------------------------------------------------------
        // Shared generator: builds a random PlayerProfile with all fields populated
        // -----------------------------------------------------------------------

        private static Gen<PlayerProfile> GenPlayerProfile()
        {
            var safeString = Gen.OneOf(
                Gen.Constant(string.Empty),
                Arb.From<NonEmptyString>().Generator.Select(s => s.Get));

            var skillNames = (SkillName[])Enum.GetValues(typeof(SkillName));
            var skillGroupNames = (SkillGroupName[])Enum.GetValues(typeof(SkillGroupName));

            var dateTimeGen = from ticks in Gen.Choose(0, int.MaxValue)
                              select new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(ticks);

            var skillGen = from level in Gen.Choose(0, 20)
                           from training in Arb.From<bool>().Generator
                           from start in dateTimeGen
                           from end in dateTimeGen
                           select new PlayerSkill
                           {
                               Level = level,
                               TrainingStarted = training,
                               CompletionTime = new CountDownTime
                               {
                                   StartTime = start,
                                   EndTime = end,
                               },
                           };

            var skillSubsetGen = from count in Gen.Choose(0, skillNames.Length)
                                 from indices in Gen.ArrayOf(count, Gen.Choose(0, skillNames.Length - 1))
                                 select indices.Distinct().Select(i => skillNames[i]).ToArray();

            return from name in safeString
                   from faction in safeString
                   from factionUuid in safeString
                   from credits in Gen.Choose(0, 999999).Select(i => (decimal)i)
                   from skillPoints in Gen.Choose(0, 100)
                   from citizenId in safeString
                   from regDate in safeString
                   from activeTime in safeString
                   from pubRank in Gen.Choose(0, 20)
                   from pubCurXP in Gen.Choose(0, 10000).Select(i => (long)i)
                   from pubNextXP in Gen.Choose(0, 10000).Select(i => (long)i)
                   from pubTitle in safeString
                   from privRank in Gen.Choose(0, 20)
                   from privCurXP in Gen.Choose(0, 10000).Select(i => (long)i)
                   from privNextXP in Gen.Choose(0, 10000).Select(i => (long)i)
                   from privTitle in safeString
                   from milRank in Gen.Choose(0, 20)
                   from milCurXP in Gen.Choose(0, 10000).Select(i => (long)i)
                   from milNextXP in Gen.Choose(0, 10000).Select(i => (long)i)
                   from milTitle in safeString
                   from selectedSkills in skillSubsetGen
                   from skills in Gen.Sequence(selectedSkills.Select(_ => skillGen))
                   from groupFlags in Gen.ArrayOf(skillGroupNames.Length, Arb.From<bool>().Generator)
                   let profile = BuildProfile(
                       name, faction, factionUuid, credits, skillPoints, citizenId, regDate, activeTime,
                       pubRank, pubCurXP, pubNextXP, pubTitle,
                       privRank, privCurXP, privNextXP, privTitle,
                       milRank, milCurXP, milNextXP, milTitle,
                       selectedSkills, skills.ToArray(), skillGroupNames, groupFlags)
                   select profile;
        }

        private static Gen<PlayerProfile> GenPlayerProfileWithIdentity()
        {
            var nameGen = from len in Gen.Choose(0, 50)
                          from chars in Gen.ArrayOf(len, Gen.Elements(
                              "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ".ToCharArray()))
                          select new string(chars);

            return from characterId in Gen.Choose(0, 999999)
                   from firstName in nameGen
                   from lastName in nameGen
                   from activeTimeMinutes in Gen.Choose(0, 5000000)
                   let profile = new PlayerProfile
                   {
                       UUID = Guid.NewGuid().ToString(),
                       Name = "TestPlayer",
                       CharacterId = characterId,
                       FirstName = firstName,
                       LastName = lastName,
                       ActiveTimeMinutes = activeTimeMinutes,
                   }
                   select profile;
        }

        private static PlayerProfile BuildProfile(
            string name, string faction, string factionUuid, decimal credits, int skillPoints,
            string citizenId, string regDate, string activeTime,
            int pubRank, long pubCurXP, long pubNextXP, string pubTitle,
            int privRank, long privCurXP, long privNextXP, string privTitle,
            int milRank, long milCurXP, long milNextXP, string milTitle,
            SkillName[] selectedSkills, PlayerSkill[] skills,
            SkillGroupName[] skillGroupNames, bool[] groupFlags)
        {
            var profile = new PlayerProfile
            {
                UUID = Guid.NewGuid().ToString(),
                Name = name,
                Faction = faction,
                FactionUUID = factionUuid,
                TotalCredits = credits,
                SkillPoints = skillPoints,
                CitizenId = citizenId,
                RegistrationDate = regDate,
                ActiveTime = activeTime,
            };

            profile.Public.Rank = pubRank;
            profile.Public.CurrentXp = pubCurXP;
            profile.Public.XpToNextLevel = pubNextXP;
            profile.Public.RankName = pubTitle;

            profile.Private.Rank = privRank;
            profile.Private.CurrentXp = privCurXP;
            profile.Private.XpToNextLevel = privNextXP;
            profile.Private.RankName = privTitle;

            profile.Military.Rank = milRank;
            profile.Military.CurrentXp = milCurXP;
            profile.Military.XpToNextLevel = milNextXP;
            profile.Military.RankName = milTitle;

            for (int i = 0; i < selectedSkills.Length && i < skills.Length; i++)
            {
                string key = selectedSkills[i].ToDisplayName();
                profile.Skills[key] = skills[i];
            }

            for (int i = 0; i < skillGroupNames.Length && i < groupFlags.Length; i++)
            {
                profile.SetSkillGroup(skillGroupNames[i], groupFlags[i]);
            }

            return profile;
        }

        // -----------------------------------------------------------------------
        // Property 1: LoadFrom round-trip preserves all fields
        // Feature: BL-111, Property 1: LoadFrom round-trip preserves all fields
        // **Validates: Requirements 1.7, 1.8, 5.1, 5.2, 5.3, 5.4**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property LoadFrom_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(Arb.From(GenPlayerProfile()), profile =>
            {
                var ro = new ReadOnlyPlayerProfile(profile);
                var vm = new PlayerProfileViewModel(playerContext);
                vm.LoadFrom(ro);

                // Scalar fields
                if (vm.Name != ro.Name) return false;
                if (vm.Faction != ro.Faction) return false;
                if (vm.TotalCredits != ro.TotalCredits) return false;
                if (vm.SkillPoints != ro.SkillPoints) return false;
                if (vm.CitizenId != ro.CitizenId) return false;
                if (vm.RegistrationDate != ro.RegistrationDate) return false;
                if (vm.ActiveTime != ro.ActiveTime) return false;
                if (vm.UUID != ro.UUID) return false;

                // Ranks
                if (!RankMatches(vm.PublicRank, ro.Public)) return false;
                if (!RankMatches(vm.PrivateRank, ro.Private)) return false;
                if (!RankMatches(vm.MilitaryRank, ro.Military)) return false;

                // Skills
                var roSkills = ro.Skills;
                foreach (var kvp in roSkills)
                {
                    var local = vm.GetSkill(kvp.Key);
                    if (local.Level != kvp.Value.Level) return false;
                    if (local.TrainingStarted != kvp.Value.TrainingStarted) return false;
                    if (local.CompletionStartTime != kvp.Value.CompletionStartTime) return false;
                    if (local.CompletionEndTime != kvp.Value.CompletionEndTime) return false;
                }

                // Skill groups
                foreach (SkillGroupName group in Enum.GetValues(typeof(SkillGroupName)))
                {
                    if (vm.GetSkillGroup(group) != ro.GetSkillGroup(group)) return false;
                }

                return true;
            });
        }

        // -----------------------------------------------------------------------
        // Property: LoadFrom round-trip preserves identity and time fields
        // Feature: profile-form-upgrade, Property 1: LoadFrom round-trip preserves identity and time fields
        // **Validates: Requirements 1.1, 2.1**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property LoadFrom_RoundTrip_PreservesIdentityAndTimeFields()
        {
            return Prop.ForAll(Arb.From(GenPlayerProfileWithIdentity()), profile =>
            {
                var ro = new ReadOnlyPlayerProfile(profile);
                var vm = new PlayerProfileViewModel(playerContext);
                vm.LoadFrom(ro);

                if (vm.CharacterId != ro.CharacterId)
                {
                    return false.Label("CharacterId mismatch: expected " + ro.CharacterId + " got " + vm.CharacterId);
                }

                if (vm.FirstName != ro.FirstName)
                {
                    return false.Label("FirstName mismatch: expected '" + ro.FirstName + "' got '" + vm.FirstName + "'");
                }

                if (vm.LastName != ro.LastName)
                {
                    return false.Label("LastName mismatch: expected '" + ro.LastName + "' got '" + vm.LastName + "'");
                }

                if (vm.ActiveTimeMinutes != ro.ActiveTimeMinutes)
                {
                    return false.Label("ActiveTimeMinutes mismatch: expected " + ro.ActiveTimeMinutes + " got " + vm.ActiveTimeMinutes);
                }

                return true.Label("All identity and time fields preserved");
            });
        }

        // -----------------------------------------------------------------------
        // Property 2: IsDirty is false immediately after LoadFrom
        // Feature: BL-111, Property 2: IsDirty is false immediately after LoadFrom
        // **Validates: Requirements 9.1, 9.6**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property IsDirty_FalseImmediatelyAfterLoadFrom()
        {
            return Prop.ForAll(Arb.From(GenPlayerProfile()), profile =>
            {
                var ro = new ReadOnlyPlayerProfile(profile);
                var vm = new PlayerProfileViewModel(playerContext);
                vm.LoadFrom(ro);
                return (!vm.IsDirty).Label(
                    vm.IsDirty ? "IsDirty was true after LoadFrom" : "OK");
            });
        }

        // -----------------------------------------------------------------------
        // Property 3: IsDirty detects any single field change
        // Feature: BL-111, Property 3: IsDirty detects any single field change
        // **Validates: Requirements 9.1, 9.2, 9.3, 9.4, 9.5, 9.7**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property IsDirty_DetectsAnySingleFieldChange()
        {
            // 7 scalar + 12 rank = 19 fixed fields, plus skill/group fields
            var fieldIndexGen = Gen.Choose(0, 20);

            return Prop.ForAll(
                Arb.From(GenPlayerProfile()),
                Arb.From(fieldIndexGen),
                (profile, fieldIndex) =>
                {
                    var ro = new ReadOnlyPlayerProfile(profile);
                    var vm = new PlayerProfileViewModel(playerContext);
                    vm.LoadFrom(ro);

                    string changedField = MutateSingleField(vm, ro, fieldIndex);

                    return vm.IsDirty.Label(
                        vm.IsDirty ? "OK" : "IsDirty was false after changing " + changedField);
                });
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static bool RankMatches(LocalRankData local, ReadOnlyPlayerRank ro)
        {
            return local.Rank == ro.Rank
                && local.CurrentXp == ro.CurrentXp
                && local.XpToNextLevel == ro.XpToNextLevel
                && local.RankName == ro.RankName;
        }

        private static string MutateSingleField(
            PlayerProfileViewModel vm, ReadOnlyPlayerProfile ro, int fieldIndex)
        {
            switch (fieldIndex % 21)
            {
                case 0:
                    vm.Name = (ro.Name ?? string.Empty) + "X";
                    return "Name";
                case 1:
                    vm.Faction = (ro.Faction ?? string.Empty) + "X";
                    return "Faction";
                case 2:
                    vm.TotalCredits = ro.TotalCredits + 1;
                    return "TotalCredits";
                case 3:
                    vm.SkillPoints = ro.SkillPoints + 1;
                    return "SkillPoints";
                case 4:
                    vm.CitizenId = (ro.CitizenId ?? string.Empty) + "X";
                    return "CitizenId";
                case 5:
                    vm.RegistrationDate = (ro.RegistrationDate ?? string.Empty) + "X";
                    return "RegistrationDate";
                case 6:
                    vm.ActiveTime = (ro.ActiveTime ?? string.Empty) + "X";
                    return "ActiveTime";
                case 7:
                    vm.PublicRank.Rank = ro.Public.Rank + 1;
                    return "PublicRank.Rank";
                case 8:
                    vm.PublicRank.CurrentXp = ro.Public.CurrentXp + 1;
                    return "PublicRank.CurrentXp";
                case 9:
                    vm.PublicRank.XpToNextLevel = ro.Public.XpToNextLevel + 1;
                    return "PublicRank.XpToNextLevel";
                case 10:
                    vm.PublicRank.RankName = (ro.Public.RankName ?? string.Empty) + "X";
                    return "PublicRank.RankName";
                case 11:
                    vm.PrivateRank.Rank = ro.Private.Rank + 1;
                    return "PrivateRank.Rank";
                case 12:
                    vm.PrivateRank.CurrentXp = ro.Private.CurrentXp + 1;
                    return "PrivateRank.CurrentXp";
                case 13:
                    vm.PrivateRank.XpToNextLevel = ro.Private.XpToNextLevel + 1;
                    return "PrivateRank.XpToNextLevel";
                case 14:
                    vm.PrivateRank.RankName = (ro.Private.RankName ?? string.Empty) + "X";
                    return "PrivateRank.RankName";
                case 15:
                    vm.MilitaryRank.Rank = ro.Military.Rank + 1;
                    return "MilitaryRank.Rank";
                case 16:
                    vm.MilitaryRank.CurrentXp = ro.Military.CurrentXp + 1;
                    return "MilitaryRank.CurrentXp";
                case 17:
                    vm.MilitaryRank.XpToNextLevel = ro.Military.XpToNextLevel + 1;
                    return "MilitaryRank.XpToNextLevel";
                case 18:
                    vm.MilitaryRank.RankName = (ro.Military.RankName ?? string.Empty) + "X";
                    return "MilitaryRank.RankName";
                case 19:
                    // Change a skill field if any skills exist
                    var skills = ro.Skills;
                    if (skills.Count > 0)
                    {
                        string firstKey = skills.Keys.First();
                        var local = vm.GetSkill(firstKey);
                        local.Level = skills[firstKey].Level + 1;
                        return "Skill.Level(" + firstKey + ")";
                    }
                    else
                    {
                        // No skills  fall back to Name
                        vm.Name = (ro.Name ?? string.Empty) + "X";
                        return "Name(fallback)";
                    }

                case 20:
                    // Flip a skill group
                    var firstGroup = ((SkillGroupName[])Enum.GetValues(typeof(SkillGroupName)))[0];
                    bool current = vm.GetSkillGroup(firstGroup);
                    vm.SetSkillGroup(firstGroup, !current);
                    return "SkillGroup(" + firstGroup + ")";

                default:
                    vm.Name = (ro.Name ?? string.Empty) + "X";
                    return "Name(default)";
            }
        }
    }
}