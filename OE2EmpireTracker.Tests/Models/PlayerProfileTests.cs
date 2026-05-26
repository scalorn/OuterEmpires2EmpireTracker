using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Models
{
    [TestFixture]
    public class PlayerProfileTests
    {
        private PlayerProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _profile = new PlayerProfile();
        }

        // -----------------------------------------------------------------------
        // Default values
        // -----------------------------------------------------------------------

        [Test]
        public void DefaultConstructor_StringPropertiesAreEmpty()
        {
            Assert.That(_profile.UUID, Is.EqualTo(string.Empty));
            Assert.That(_profile.Name, Is.EqualTo(string.Empty));
            Assert.That(_profile.Faction, Is.EqualTo(string.Empty));
        }

        [Test]
        public void DefaultConstructor_NumericDefaultsAreZero()
        {
            Assert.That(_profile.TotalCredits, Is.EqualTo(0m));
            Assert.That(_profile.SkillPoints, Is.EqualTo(0));
        }

        [Test]
        public void DefaultConstructor_RankObjectsAreInitialised()
        {
            Assert.That(_profile.Public, Is.Not.Null);
            Assert.That(_profile.Private, Is.Not.Null);
            Assert.That(_profile.Military, Is.Not.Null);
        }

        [Test]
        public void DefaultConstructor_SkillsDictionaryIsEmpty()
        {
            Assert.That(_profile.Skills.Count, Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // GetSkill -- string overload
        // -----------------------------------------------------------------------

        [Test]
        public void GetSkill_UnknownSkill_CreatesAndReturnsNewSkill()
        {
            var skill = _profile.GetSkill("Foreman");
            Assert.That(skill, Is.Not.Null);
        }

        [Test]
        public void GetSkill_UnknownSkill_AddedToSkillsDictionary()
        {
            _profile.GetSkill("Foreman");
            Assert.That(_profile.Skills.ContainsKey("Foreman"), Is.True);
        }

        [Test]
        public void GetSkill_CalledTwice_ReturnsSameInstance()
        {
            var first = _profile.GetSkill("Foreman");
            var second = _profile.GetSkill("Foreman");
            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void GetSkill_ExistingSkill_ReturnsExistingInstance()
        {
            var skill = new PlayerSkill { Level = 5 };
            _profile.Skills["Broker"] = skill;
            var result = _profile.GetSkill("Broker");
            Assert.That(result.Level, Is.EqualTo(5));
        }

        // -----------------------------------------------------------------------
        // GetSkill -- enum overload
        // -----------------------------------------------------------------------

        [Test]
        public void GetSkill_EnumOverload_UsesDisplayName()
        {
            var skill = _profile.GetSkill(SkillName.Foreman);
            Assert.That(_profile.Skills.ContainsKey("Foreman"), Is.True);
            Assert.That(skill, Is.Not.Null);
        }

        [Test]
        public void GetSkill_EnumOverload_SameKeyAsStringOverload()
        {
            var byEnum = _profile.GetSkill(SkillName.Broker);
            var byString = _profile.GetSkill("Broker");
            Assert.That(byString, Is.SameAs(byEnum));
        }

        // -----------------------------------------------------------------------
        // GetSkillGroup / SetSkillGroup -- string overloads
        // -----------------------------------------------------------------------

        [Test]
        public void GetSkillGroup_UnknownGroup_ReturnsFalse()
        {
            Assert.That(_profile.GetSkillGroup("Commander"), Is.False);
        }

        [Test]
        public void SetSkillGroup_ThenGet_ReturnsSetValue()
        {
            _profile.SetSkillGroup("Commander", true);
            Assert.That(_profile.GetSkillGroup("Commander"), Is.True);
        }

        [Test]
        public void SetSkillGroup_SetFalse_ReturnsFalse()
        {
            _profile.SetSkillGroup("Commander", true);
            _profile.SetSkillGroup("Commander", false);
            Assert.That(_profile.GetSkillGroup("Commander"), Is.False);
        }

        [Test]
        public void GetSkillGroup_DifferentGroups_TrackedIndependently()
        {
            _profile.SetSkillGroup("Commander", true);
            _profile.SetSkillGroup("Engineer", false);
            Assert.That(_profile.GetSkillGroup("Commander"), Is.True);
            Assert.That(_profile.GetSkillGroup("Engineer"), Is.False);
        }

        // -----------------------------------------------------------------------
        // GetSkillGroup / SetSkillGroup -- enum overloads
        // -----------------------------------------------------------------------

        [Test]
        public void SetSkillGroup_EnumOverload_UsesDisplayName()
        {
            _profile.SetSkillGroup(SkillGroupName.Commander, true);
            Assert.That(_profile.GetSkillGroup("Commander"), Is.True);
        }

        [Test]
        public void GetSkillGroup_EnumOverload_ReadsValueSetByStringOverload()
        {
            _profile.SetSkillGroup("Trader", true);
            Assert.That(_profile.GetSkillGroup(SkillGroupName.Trader), Is.True);
        }

        [Test]
        public void SetAndGet_EnumOverload_RoundTrips()
        {
            _profile.SetSkillGroup(SkillGroupName.Researcher, true);
            Assert.That(_profile.GetSkillGroup(SkillGroupName.Researcher), Is.True);
        }
    }

    // -----------------------------------------------------------------------
    // PlayerRank
    // -----------------------------------------------------------------------

    [TestFixture]
    public class PlayerRankTests
    {
        [Test]
        public void DefaultConstructor_AllZero()
        {
            var rank = new PlayerRank();
            Assert.That(rank.Rank, Is.EqualTo(0));
            Assert.That(rank.CurrentXp, Is.EqualTo(0L));
            Assert.That(rank.XpToNextLevel, Is.EqualTo(0L));
        }

        [Test]
        public void Properties_CanBeSetAndRead()
        {
            var rank = new PlayerRank { Rank = 5, CurrentXp = 1000, XpToNextLevel = 2000 };
            Assert.That(rank.Rank, Is.EqualTo(5));
            Assert.That(rank.CurrentXp, Is.EqualTo(1000L));
            Assert.That(rank.XpToNextLevel, Is.EqualTo(2000L));
        }
    }

    // -----------------------------------------------------------------------
    // PlayerSkill
    // -----------------------------------------------------------------------

    [TestFixture]
    public class PlayerSkillTests
    {
        [Test]
        public void DefaultConstructor_LevelIsZero()
        {
            Assert.That(new PlayerSkill().Level, Is.EqualTo(0));
        }

        [Test]
        public void DefaultConstructor_TrainingStartedIsFalse()
        {
            Assert.That(new PlayerSkill().TrainingStarted, Is.False);
        }

        [Test]
        public void DefaultConstructor_CompletionTimeIsNotNull()
        {
            Assert.That(new PlayerSkill().CompletionTime, Is.Not.Null);
        }
    }

    // -----------------------------------------------------------------------
    // SkillName / SkillGroupName ToDisplayName extensions
    // -----------------------------------------------------------------------

    [TestFixture]
    public class SkillNameExtensionsTests
    {
        [Test]
        public void SkillName_ToDisplayName_ReturnsDescriptionAttribute()
        {
            Assert.That(SkillName.HumanResources.ToDisplayName(), Is.EqualTo("Human Resources"));
            Assert.That(SkillName.SoundsAsAPound.ToDisplayName(), Is.EqualTo("Sounds As A Pound"));
            Assert.That(SkillName.AAAHealthcare.ToDisplayName(), Is.EqualTo("AAA Healthcare"));
        }

        [Test]
        public void SkillGroupName_ToDisplayName_ReturnsDescriptionAttribute()
        {
            Assert.That(SkillGroupName.ColonyDirector.ToDisplayName(), Is.EqualTo("Colony Director"));
            Assert.That(SkillGroupName.JobManagement.ToDisplayName(), Is.EqualTo("Job Management"));
            Assert.That(SkillGroupName.Entrepeneur.ToDisplayName(), Is.EqualTo("Entrepeneur"));
        }

        [Test]
        public void SkillName_AllValues_HaveNonEmptyDisplayName()
        {
            foreach (SkillName s in System.Enum.GetValues(typeof(SkillName)))
            {
                Assert.That(
                    string.IsNullOrEmpty(s.ToDisplayName()),
                    Is.False,
                    $"SkillName.{s} has empty display name");
            }
        }

        [Test]
        public void SkillGroupName_AllValues_HaveNonEmptyDisplayName()
        {
            foreach (SkillGroupName g in System.Enum.GetValues(typeof(SkillGroupName)))
            {
                Assert.That(
                    string.IsNullOrEmpty(g.ToDisplayName()),
                    Is.False,
                    $"SkillGroupName.{g} has empty display name");
            }
        }
    }
}
