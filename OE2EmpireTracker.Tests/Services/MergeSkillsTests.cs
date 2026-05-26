// <copyright file="MergeSkillsTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for GameApiSyncScheduler.MergeSkills (private static).
    /// Validates: Requirements 6.2, 6.3, 7.2, 7.3, 7.4, 9.6, 9.7, 9.8.
    /// </summary>
    [TestFixture]
    public class MergeSkillsTests
    {
        private static readonly MethodInfo MergeSkillsMethod = typeof(GameApiSyncScheduler)
            .GetMethod("MergeSkills", BindingFlags.NonPublic | BindingFlags.Static);

        /// <summary>
        /// Invokes the private static MergeSkills method via reflection.
        /// </summary>
        private static bool InvokeMergeSkills(
            PlayerProfile local,
            Dictionary<string, GameApiSkillResponse> remoteSkills,
            GameApiSkillInTrainingResponse skillInTraining)
        {
            return (bool)MergeSkillsMethod.Invoke(
                null,
                new object[] { local, remoteSkills, skillInTraining });
        }

        // -------------------------------------------------------------------
        // Test 1: Metadata fields merge correctly from remote
        // Validates: Requirements 6.2, 9.6
        // -------------------------------------------------------------------

        [Test]
        public void MetadataFieldsMergeFromRemote()
        {
            var local = new PlayerProfile();
            local.GetSkill("Mining");

            var remoteSkills = new Dictionary<string, GameApiSkillResponse>
            {
                ["Mining"] = new GameApiSkillResponse
                {
                    SkillId = 42,
                    EffectDescription = "Increases mining yield",
                    AmountPerLevel = 5,
                    SkillGroupName = "Colony Operations",
                    IsUnlocked = true,
                    Level = 3,
                },
            };

            bool changed = InvokeMergeSkills(local, remoteSkills, null);

            PlayerSkill skill = local.Skills["Mining"];
            Assert.That(changed, Is.True);
            Assert.That(skill.SkillId, Is.EqualTo(42));
            Assert.That(skill.EffectDescription, Is.EqualTo("Increases mining yield"));
            Assert.That(skill.AmountPerLevel, Is.EqualTo(5));
            Assert.That(skill.SkillGroupName, Is.EqualTo("Colony Operations"));
            Assert.That(skill.IsUnlocked, Is.True);
        }

        // -------------------------------------------------------------------
        // Test 2: Null EffectDescription becomes empty string
        // Validates: Requirements 6.3
        // -------------------------------------------------------------------

        [Test]
        public void NullEffectDescriptionBecomesEmptyString()
        {
            var local = new PlayerProfile();
            local.GetSkill("Refining");

            var remoteSkills = new Dictionary<string, GameApiSkillResponse>
            {
                ["Refining"] = new GameApiSkillResponse
                {
                    EffectDescription = null,
                    SkillGroupName = "Production",
                    Level = 1,
                },
            };

            InvokeMergeSkills(local, remoteSkills, null);

            Assert.That(local.Skills["Refining"].EffectDescription, Is.EqualTo(string.Empty));
        }

        // -------------------------------------------------------------------
        // Test 3: Null SkillGroupName becomes empty string
        // Validates: Requirements 6.3
        // -------------------------------------------------------------------

        [Test]
        public void NullSkillGroupNameBecomesEmptyString()
        {
            var local = new PlayerProfile();
            local.GetSkill("Trading");

            var remoteSkills = new Dictionary<string, GameApiSkillResponse>
            {
                ["Trading"] = new GameApiSkillResponse
                {
                    EffectDescription = "Boosts trade",
                    SkillGroupName = null,
                    Level = 2,
                },
            };

            InvokeMergeSkills(local, remoteSkills, null);

            Assert.That(local.Skills["Trading"].SkillGroupName, Is.EqualTo(string.Empty));
        }

        // -------------------------------------------------------------------
        // Test 4: Training progress sets on matching skill
        // Validates: Requirements 7.2, 9.7
        // -------------------------------------------------------------------

        [Test]
        public void TrainingProgressSetsOnMatchingSkill()
        {
            var local = new PlayerProfile();
            local.GetSkill("Engineering");

            var remoteSkills = new Dictionary<string, GameApiSkillResponse>
            {
                ["Engineering"] = new GameApiSkillResponse
                {
                    Level = 4,
                },
            };

            var skillInTraining = new GameApiSkillInTrainingResponse
            {
                SkillName = "Engineering",
                TargetLevel = 5,
                TrainingPercentageComplete = 67,
                RemainingMinutes = 120,
            };

            bool changed = InvokeMergeSkills(local, remoteSkills, skillInTraining);

            PlayerSkill skill = local.Skills["Engineering"];
            Assert.That(changed, Is.True);
            Assert.That(skill.TargetLevel, Is.EqualTo(5));
            Assert.That(skill.TrainingPercentageComplete, Is.EqualTo(67));
            Assert.That(skill.RemainingMinutes, Is.EqualTo(120));
        }

        // -------------------------------------------------------------------
        // Test 5: Non-matching skills get training fields reset to 0
        // Validates: Requirements 7.3, 9.8
        // -------------------------------------------------------------------

        [Test]
        public void NonMatchingSkillsGetTrainingFieldsReset()
        {
            var local = new PlayerProfile();
            PlayerSkill miningSkill = local.GetSkill("Mining");
            miningSkill.TargetLevel = 3;
            miningSkill.TrainingPercentageComplete = 50;
            miningSkill.RemainingMinutes = 60;

            local.GetSkill("Refining");

            var remoteSkills = new Dictionary<string, GameApiSkillResponse>
            {
                ["Mining"] = new GameApiSkillResponse { Level = 2 },
                ["Refining"] = new GameApiSkillResponse { Level = 1 },
            };

            var skillInTraining = new GameApiSkillInTrainingResponse
            {
                SkillName = "Refining",
                TargetLevel = 2,
                TrainingPercentageComplete = 25,
                RemainingMinutes = 90,
            };

            InvokeMergeSkills(local, remoteSkills, skillInTraining);

            // Mining is NOT the skill in training, so its fields should be reset
            Assert.That(local.Skills["Mining"].TargetLevel, Is.EqualTo(0));
            Assert.That(local.Skills["Mining"].TrainingPercentageComplete, Is.EqualTo(0));
            Assert.That(local.Skills["Mining"].RemainingMinutes, Is.EqualTo(0));

            // Refining IS the skill in training, so it should have the values
            Assert.That(local.Skills["Refining"].TargetLevel, Is.EqualTo(2));
            Assert.That(local.Skills["Refining"].TrainingPercentageComplete, Is.EqualTo(25));
            Assert.That(local.Skills["Refining"].RemainingMinutes, Is.EqualTo(90));
        }

        // -------------------------------------------------------------------
        // Test 6: Null skillInTraining resets all training fields
        // Validates: Requirements 7.4, 9.8
        // -------------------------------------------------------------------

        [Test]
        public void NullSkillInTrainingResetsAllTrainingFields()
        {
            var local = new PlayerProfile();
            PlayerSkill skill1 = local.GetSkill("Mining");
            skill1.TargetLevel = 4;
            skill1.TrainingPercentageComplete = 80;
            skill1.RemainingMinutes = 30;

            PlayerSkill skill2 = local.GetSkill("Refining");
            skill2.TargetLevel = 2;
            skill2.TrainingPercentageComplete = 10;
            skill2.RemainingMinutes = 200;

            var remoteSkills = new Dictionary<string, GameApiSkillResponse>
            {
                ["Mining"] = new GameApiSkillResponse { Level = 3 },
                ["Refining"] = new GameApiSkillResponse { Level = 1 },
            };

            bool changed = InvokeMergeSkills(local, remoteSkills, null);

            Assert.That(changed, Is.True);
            Assert.That(local.Skills["Mining"].TargetLevel, Is.EqualTo(0));
            Assert.That(local.Skills["Mining"].TrainingPercentageComplete, Is.EqualTo(0));
            Assert.That(local.Skills["Mining"].RemainingMinutes, Is.EqualTo(0));
            Assert.That(local.Skills["Refining"].TargetLevel, Is.EqualTo(0));
            Assert.That(local.Skills["Refining"].TrainingPercentageComplete, Is.EqualTo(0));
            Assert.That(local.Skills["Refining"].RemainingMinutes, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Test 7: Skills not in remoteSkills get training fields reset
        // Validates: Requirements 7.3, 7.4
        // -------------------------------------------------------------------

        [Test]
        public void SkillsNotInRemoteSkillsGetTrainingFieldsReset()
        {
            var local = new PlayerProfile();
            PlayerSkill localOnly = local.GetSkill("Surveying");
            localOnly.TargetLevel = 5;
            localOnly.TrainingPercentageComplete = 99;
            localOnly.RemainingMinutes = 1;

            local.GetSkill("Mining");

            // Remote only contains Mining, not Surveying
            var remoteSkills = new Dictionary<string, GameApiSkillResponse>
            {
                ["Mining"] = new GameApiSkillResponse { Level = 2 },
            };

            var skillInTraining = new GameApiSkillInTrainingResponse
            {
                SkillName = "Mining",
                TargetLevel = 3,
                TrainingPercentageComplete = 40,
                RemainingMinutes = 55,
            };

            InvokeMergeSkills(local, remoteSkills, skillInTraining);

            // Surveying is not in remoteSkills, so training fields should be reset
            Assert.That(local.Skills["Surveying"].TargetLevel, Is.EqualTo(0));
            Assert.That(local.Skills["Surveying"].TrainingPercentageComplete, Is.EqualTo(0));
            Assert.That(local.Skills["Surveying"].RemainingMinutes, Is.EqualTo(0));
        }
    }
}
