using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Models
{
    /// <summary>
    /// Unit tests for ReadOnlyPlayerProfile and ReadOnlyPlayerSkill gap fill.
    /// Feature: BL-111 PlayerProfile Immutable Data Model
    /// Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 2.1, 2.2
    /// </summary>
    [TestFixture]
    public class ReadOnlyPlayerProfileGapFillTests
    {
        private PlayerProfile _profile;
        private ReadOnlyPlayerProfile _readOnly;

        [SetUp]
        public void SetUp()
        {
            _profile = new PlayerProfile
            {
                UUID = "test-uuid-001",
                Name = "Scalorn",
                Faction = "Galactic Federation",
                FactionUUID = "faction-uuid-001",
                TotalCredits = 123456.78m,
                SkillPoints = 42,
                CitizenId = "CIT-99887",
                RegistrationDate = "2024-01-15",
                ActiveTime = "5d 3h 20m",
            };

            _profile.Public.Rank = 3;
            _profile.Public.CurrentXP = 500;
            _profile.Public.NextXP = 1000;
            _profile.Public.Title = "Commander";

            _profile.Private.Rank = 2;
            _profile.Private.CurrentXP = 200;
            _profile.Private.NextXP = 400;
            _profile.Private.Title = "Merchant";

            _profile.Military.Rank = 1;
            _profile.Military.CurrentXP = 50;
            _profile.Military.NextXP = 100;
            _profile.Military.Title = "Recruit";

            var foremanSkill = _profile.GetSkill("Foreman");
            foremanSkill.Level = 3;
            foremanSkill.TrainingStarted = true;
            foremanSkill.CompletionTime.StartTime = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
            foremanSkill.CompletionTime.EndTime = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

            var brokerSkill = _profile.GetSkill("Broker");
            brokerSkill.Level = 5;
            brokerSkill.TrainingStarted = false;

            _readOnly = new ReadOnlyPlayerProfile(_profile);
        }

        // -------------------------------------------------------------------
        // Requirement 1.1: Faction property
        // -------------------------------------------------------------------

        [Test]
        public void Faction_ReturnsMutableEntityFaction()
        {
            Assert.That(_readOnly.Faction, Is.EqualTo("Galactic Federation"));
        }

        [Test]
        public void Faction_ReflectsMutation()
        {
            _profile.Faction = "Pirate Syndicate";
            Assert.That(_readOnly.Faction, Is.EqualTo("Pirate Syndicate"));
        }

        // -------------------------------------------------------------------
        // Requirement 1.2: TotalCredits property
        // -------------------------------------------------------------------

        [Test]
        public void TotalCredits_ReturnsMutableEntityTotalCredits()
        {
            Assert.That(_readOnly.TotalCredits, Is.EqualTo(123456.78m));
        }

        [Test]
        public void TotalCredits_ReflectsMutation()
        {
            _profile.TotalCredits = 999999.99m;
            Assert.That(_readOnly.TotalCredits, Is.EqualTo(999999.99m));
        }

        // -------------------------------------------------------------------
        // Requirement 1.3: SkillPoints property
        // -------------------------------------------------------------------

        [Test]
        public void SkillPoints_ReturnsMutableEntitySkillPoints()
        {
            Assert.That(_readOnly.SkillPoints, Is.EqualTo(42));
        }

        [Test]
        public void SkillPoints_ReflectsMutation()
        {
            _profile.SkillPoints = 100;
            Assert.That(_readOnly.SkillPoints, Is.EqualTo(100));
        }

        // -------------------------------------------------------------------
        // Requirement 1.4: CitizenId property
        // -------------------------------------------------------------------

        [Test]
        public void CitizenId_ReturnsMutableEntityCitizenId()
        {
            Assert.That(_readOnly.CitizenId, Is.EqualTo("CIT-99887"));
        }

        [Test]
        public void CitizenId_ReflectsMutation()
        {
            _profile.CitizenId = "CIT-00001";
            Assert.That(_readOnly.CitizenId, Is.EqualTo("CIT-00001"));
        }

        // -------------------------------------------------------------------
        // Requirement 1.5: RegistrationDate property
        // -------------------------------------------------------------------

        [Test]
        public void RegistrationDate_ReturnsMutableEntityRegistrationDate()
        {
            Assert.That(_readOnly.RegistrationDate, Is.EqualTo("2024-01-15"));
        }

        [Test]
        public void RegistrationDate_ReflectsMutation()
        {
            _profile.RegistrationDate = "2025-03-01";
            Assert.That(_readOnly.RegistrationDate, Is.EqualTo("2025-03-01"));
        }

        // -------------------------------------------------------------------
        // Requirement 1.6: ActiveTime property
        // -------------------------------------------------------------------

        [Test]
        public void ActiveTime_ReturnsMutableEntityActiveTime()
        {
            Assert.That(_readOnly.ActiveTime, Is.EqualTo("5d 3h 20m"));
        }

        [Test]
        public void ActiveTime_ReflectsMutation()
        {
            _profile.ActiveTime = "10d 0h 0m";
            Assert.That(_readOnly.ActiveTime, Is.EqualTo("10d 0h 0m"));
        }

        // -------------------------------------------------------------------
        // Requirement 1.7: Skills property returns IReadOnlyDictionary
        // -------------------------------------------------------------------

        [Test]
        public void Skills_ReturnsCorrectCount()
        {
            Assert.That(_readOnly.Skills.Count, Is.EqualTo(2));
        }

        [Test]
        public void Skills_ContainsExpectedKeys()
        {
            Assert.That(_readOnly.Skills.ContainsKey("Foreman"), Is.True);
            Assert.That(_readOnly.Skills.ContainsKey("Broker"), Is.True);
        }

        [Test]
        public void Skills_ReturnsReadOnlyPlayerSkillInstances()
        {
            var foremanSkill = _readOnly.Skills["Foreman"];
            Assert.That(foremanSkill, Is.InstanceOf<ReadOnlyPlayerSkill>());
            Assert.That(foremanSkill.Level, Is.EqualTo(3));
        }

        [Test]
        public void Skills_WrappedSkillLevelMatchesEntity()
        {
            Assert.That(_readOnly.Skills["Broker"].Level, Is.EqualTo(5));
        }

        [Test]
        public void Skills_EmptyDictionary_ReturnsEmptyNotNull()
        {
            var emptyProfile = new PlayerProfile { UUID = "empty" };
            var ro = new ReadOnlyPlayerProfile(emptyProfile);
            Assert.That(ro.Skills, Is.Not.Null);
            Assert.That(ro.Skills.Count, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Existing properties still work (UUID, Name, FactionUUID, Ranks)
        // -------------------------------------------------------------------

        [Test]
        public void UUID_ReturnsMutableEntityUUID()
        {
            Assert.That(_readOnly.UUID, Is.EqualTo("test-uuid-001"));
        }

        [Test]
        public void Name_ReturnsMutableEntityName()
        {
            Assert.That(_readOnly.Name, Is.EqualTo("Scalorn"));
        }

        [Test]
        public void FactionUUID_ReturnsMutableEntityFactionUUID()
        {
            Assert.That(_readOnly.FactionUUID, Is.EqualTo("faction-uuid-001"));
        }

        [Test]
        public void PublicRank_WrapsCorrectly()
        {
            Assert.That(_readOnly.Public.Rank, Is.EqualTo(3));
            Assert.That(_readOnly.Public.Title, Is.EqualTo("Commander"));
        }

        [Test]
        public void PrivateRank_WrapsCorrectly()
        {
            Assert.That(_readOnly.Private.Rank, Is.EqualTo(2));
            Assert.That(_readOnly.Private.Title, Is.EqualTo("Merchant"));
        }

        [Test]
        public void MilitaryRank_WrapsCorrectly()
        {
            Assert.That(_readOnly.Military.Rank, Is.EqualTo(1));
            Assert.That(_readOnly.Military.Title, Is.EqualTo("Recruit"));
        }

        // -------------------------------------------------------------------
        // Constructor null guard
        // -------------------------------------------------------------------

        [Test]
        public void Constructor_NullEntity_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ReadOnlyPlayerProfile(null));
        }
    }

    // ===================================================================
    // ReadOnlyPlayerSkill gap fill tests
    // ===================================================================

    /// <summary>
    /// Unit tests for ReadOnlyPlayerSkill gap fill properties.
    /// Feature: BL-111 PlayerProfile Immutable Data Model
    /// Validates: Requirements 2.1, 2.2
    /// </summary>
    [TestFixture]
    public class ReadOnlyPlayerSkillGapFillTests
    {
        private PlayerSkill _skill;
        private ReadOnlyPlayerSkill _readOnly;

        [SetUp]
        public void SetUp()
        {
            _skill = new PlayerSkill
            {
                Level = 4,
                TrainingStarted = true,
            };

            _skill.CompletionTime.StartTime = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
            _skill.CompletionTime.EndTime = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

            _readOnly = new ReadOnlyPlayerSkill(_skill);
        }

        // -------------------------------------------------------------------
        // Requirement 2.1: TrainingStarted property
        // -------------------------------------------------------------------

        [Test]
        public void TrainingStarted_ReturnsMutableEntityTrainingStarted()
        {
            Assert.That(_readOnly.TrainingStarted, Is.True);
        }

        [Test]
        public void TrainingStarted_ReflectsMutation()
        {
            _skill.TrainingStarted = false;
            Assert.That(_readOnly.TrainingStarted, Is.False);
        }

        [Test]
        public void TrainingStarted_DefaultIsFalse()
        {
            var defaultSkill = new PlayerSkill();
            var ro = new ReadOnlyPlayerSkill(defaultSkill);
            Assert.That(ro.TrainingStarted, Is.False);
        }

        // -------------------------------------------------------------------
        // Requirement 2.2: CompletionTime read-only scalars
        // -------------------------------------------------------------------

        [Test]
        public void CompletionStartTime_ReturnsMutableEntityStartTime()
        {
            Assert.That(
                _readOnly.CompletionStartTime,
                Is.EqualTo(new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc)));
        }

        [Test]
        public void CompletionEndTime_ReturnsMutableEntityEndTime()
        {
            Assert.That(
                _readOnly.CompletionEndTime,
                Is.EqualTo(new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc)));
        }

        [Test]
        public void CompletionTimeRemaining_MatchesEntityTimeRemaining()
        {
            Assert.That(_readOnly.CompletionTimeRemaining, Is.EqualTo(_skill.CompletionTime.TimeRemaining));
        }

        [Test]
        public void CompletionTimeRemainingString_MatchesEntityTimeRemainingString()
        {
            Assert.That(
                _readOnly.CompletionTimeRemainingString,
                Is.EqualTo(_skill.CompletionTime.TimeRemainingString));
        }

        [Test]
        public void CompletionStartTime_ReflectsMutation()
        {
            var newStart = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            _skill.CompletionTime.StartTime = newStart;
            Assert.That(_readOnly.CompletionStartTime, Is.EqualTo(newStart));
        }

        [Test]
        public void CompletionEndTime_ReflectsMutation()
        {
            var newEnd = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            _skill.CompletionTime.EndTime = newEnd;
            Assert.That(_readOnly.CompletionEndTime, Is.EqualTo(newEnd));
        }

        // -------------------------------------------------------------------
        // Existing properties still work
        // -------------------------------------------------------------------

        [Test]
        public void Level_ReturnsMutableEntityLevel()
        {
            Assert.That(_readOnly.Level, Is.EqualTo(4));
        }

        [Test]
        public void Level_ReflectsMutation()
        {
            _skill.Level = 10;
            Assert.That(_readOnly.Level, Is.EqualTo(10));
        }

        // -------------------------------------------------------------------
        // Constructor null guard
        // -------------------------------------------------------------------

        [Test]
        public void Constructor_NullEntity_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ReadOnlyPlayerSkill(null));
        }

        // -------------------------------------------------------------------
        // Default CompletionTime values
        // -------------------------------------------------------------------

        [Test]
        public void CompletionTime_DefaultSkill_StartTimeIsMinValue()
        {
            var defaultSkill = new PlayerSkill();
            var ro = new ReadOnlyPlayerSkill(defaultSkill);
            Assert.That(ro.CompletionStartTime, Is.EqualTo(DateTime.MinValue));
        }

        [Test]
        public void CompletionTime_DefaultSkill_EndTimeIsMinValue()
        {
            var defaultSkill = new PlayerSkill();
            var ro = new ReadOnlyPlayerSkill(defaultSkill);
            Assert.That(ro.CompletionEndTime, Is.EqualTo(DateTime.MinValue));
        }
    }
}