using NUnit.Framework;
using NUnit.Framework.Legacy;
using OE2EmpireTracker.Data;

namespace OE2EmpireTracker.Tests.Data
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
            Assert.AreEqual(string.Empty, _profile.UUID);
            Assert.AreEqual(string.Empty, _profile.Name);
            Assert.AreEqual(string.Empty, _profile.Faction);
        }

        [Test]
        public void DefaultConstructor_NumericDefaultsAreZero()
        {
            Assert.AreEqual(0m, _profile.TotalCredits);
            Assert.AreEqual(0, _profile.SkillPoints);
        }

        [Test]
        public void DefaultConstructor_RankObjectsAreInitialised()
        {
            Assert.IsNotNull(_profile.Public);
            Assert.IsNotNull(_profile.Private);
            Assert.IsNotNull(_profile.Military);
        }

        [Test]
        public void DefaultConstructor_SkillsDictionaryIsEmpty()
        {
            Assert.AreEqual(0, _profile.Skills.Count);
        }

        // -----------------------------------------------------------------------
        // GetSkill — string overload
        // -----------------------------------------------------------------------

        [Test]
        public void GetSkill_UnknownSkill_CreatesAndReturnsNewSkill()
        {
            var skill = _profile.GetSkill("Foreman");
            Assert.IsNotNull(skill);
        }

        [Test]
        public void GetSkill_UnknownSkill_AddedToSkillsDictionary()
        {
            _profile.GetSkill("Foreman");
            Assert.IsTrue(_profile.Skills.ContainsKey("Foreman"));
        }

        [Test]
        public void GetSkill_CalledTwice_ReturnsSameInstance()
        {
            var first = _profile.GetSkill("Foreman");
            var second = _profile.GetSkill("Foreman");
            Assert.AreSame(first, second);
        }

        [Test]
        public void GetSkill_ExistingSkill_ReturnsExistingInstance()
        {
            var skill = new PlayerSkill { Level = 5 };
            _profile.Skills["Broker"] = skill;
            var result = _profile.GetSkill("Broker");
            Assert.AreEqual(5, result.Level);
        }

        // -----------------------------------------------------------------------
        // GetSkill — enum overload
        // -----------------------------------------------------------------------

        [Test]
        public void GetSkill_EnumOverload_UsesDisplayName()
        {
            var skill = _profile.GetSkill(SkillName.Foreman);
            Assert.IsTrue(_profile.Skills.ContainsKey("Foreman"));
            Assert.IsNotNull(skill);
        }

        [Test]
        public void GetSkill_EnumOverload_SameKeyAsStringOverload()
        {
            var byEnum = _profile.GetSkill(SkillName.Broker);
            var byString = _profile.GetSkill("Broker");
            Assert.AreSame(byEnum, byString);
        }

        // -----------------------------------------------------------------------
        // GetSkillGroup / SetSkillGroup — string overloads
        // -----------------------------------------------------------------------

        [Test]
        public void GetSkillGroup_UnknownGroup_ReturnsFalse()
        {
            Assert.IsFalse(_profile.GetSkillGroup("Commander"));
        }

        [Test]
        public void SetSkillGroup_ThenGet_ReturnsSetValue()
        {
            _profile.SetSkillGroup("Commander", true);
            Assert.IsTrue(_profile.GetSkillGroup("Commander"));
        }

        [Test]
        public void SetSkillGroup_SetFalse_ReturnsFalse()
        {
            _profile.SetSkillGroup("Commander", true);
            _profile.SetSkillGroup("Commander", false);
            Assert.IsFalse(_profile.GetSkillGroup("Commander"));
        }

        [Test]
        public void GetSkillGroup_DifferentGroups_TrackedIndependently()
        {
            _profile.SetSkillGroup("Commander", true);
            _profile.SetSkillGroup("Engineer", false);
            Assert.IsTrue(_profile.GetSkillGroup("Commander"));
            Assert.IsFalse(_profile.GetSkillGroup("Engineer"));
        }

        // -----------------------------------------------------------------------
        // GetSkillGroup / SetSkillGroup — enum overloads
        // -----------------------------------------------------------------------

        [Test]
        public void SetSkillGroup_EnumOverload_UsesDisplayName()
        {
            _profile.SetSkillGroup(SkillGroupName.Commander, true);
            Assert.IsTrue(_profile.GetSkillGroup("Commander"));
        }

        [Test]
        public void GetSkillGroup_EnumOverload_ReadsValueSetByStringOverload()
        {
            _profile.SetSkillGroup("Trader", true);
            Assert.IsTrue(_profile.GetSkillGroup(SkillGroupName.Trader));
        }

        [Test]
        public void SetAndGet_EnumOverload_RoundTrips()
        {
            _profile.SetSkillGroup(SkillGroupName.Researcher, true);
            Assert.IsTrue(_profile.GetSkillGroup(SkillGroupName.Researcher));
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
            Assert.AreEqual(0, rank.Rank);
            Assert.AreEqual(0L, rank.CurrentXP);
            Assert.AreEqual(0L, rank.NextXP);
        }

        [Test]
        public void Properties_CanBeSetAndRead()
        {
            var rank = new PlayerRank { Rank = 5, CurrentXP = 1000, NextXP = 2000 };
            Assert.AreEqual(5, rank.Rank);
            Assert.AreEqual(1000L, rank.CurrentXP);
            Assert.AreEqual(2000L, rank.NextXP);
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
            Assert.AreEqual(0, new PlayerSkill().Level);
        }

        [Test]
        public void DefaultConstructor_TrainingStartedIsFalse()
        {
            Assert.IsFalse(new PlayerSkill().TrainingStarted);
        }

        [Test]
        public void DefaultConstructor_CompletionTimeIsNotNull()
        {
            Assert.IsNotNull(new PlayerSkill().CompletionTime);
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
            Assert.AreEqual("Human Resources", SkillName.HumanResources.ToDisplayName());
            Assert.AreEqual("Sounds As A Pound", SkillName.SoundsAsAPound.ToDisplayName());
            Assert.AreEqual("AAA Healthcare", SkillName.AAAHealthcare.ToDisplayName());
        }

        [Test]
        public void SkillGroupName_ToDisplayName_ReturnsDescriptionAttribute()
        {
            Assert.AreEqual("Colony Director", SkillGroupName.ColonyDirector.ToDisplayName());
            Assert.AreEqual("Job Management", SkillGroupName.JobManagement.ToDisplayName());
            Assert.AreEqual("Entrepeneur", SkillGroupName.Entrepeneur.ToDisplayName());
        }

        [Test]
        public void SkillName_AllValues_HaveNonEmptyDisplayName()
        {
            foreach (SkillName s in System.Enum.GetValues(typeof(SkillName)))
                Assert.IsFalse(string.IsNullOrEmpty(s.ToDisplayName()),
                    $"SkillName.{s} has empty display name");
        }

        [Test]
        public void SkillGroupName_AllValues_HaveNonEmptyDisplayName()
        {
            foreach (SkillGroupName g in System.Enum.GetValues(typeof(SkillGroupName)))
                Assert.IsFalse(string.IsNullOrEmpty(g.ToDisplayName()),
                    $"SkillGroupName.{g} has empty display name");
        }
    }
}
