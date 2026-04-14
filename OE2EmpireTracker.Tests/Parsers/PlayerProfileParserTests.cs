using System;
using System.IO;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;

namespace OE2EmpireTracker.Tests.Parsers
{
    [TestFixture]
    public class PlayerProfileParserTests
    {
        private PlayerProfileParser _parser;

        [SetUp]
        public void SetUp()
        {
            _parser = new PlayerProfileParser();
        }

        private static string LoadTestData(string filename)
        {
            string baseDir = TestContext.CurrentContext.TestDirectory;
            string path = Path.Combine(baseDir, "TestData", filename);
            return File.ReadAllText(path);
        }

        private static string ExtractFragment(string clipboardData)
        {
            return OE2EmpireTracker.Forms.Blueprint.BlueprintScanner
                .ExtractHtmlFragmentFromClipboardData(clipboardData);
        }

        private PlayerProfile ParseScalorn()
        {
            string raw = LoadTestData("PlayerProfileScalorn.html");
            string fragment = ExtractFragment(raw);
            var profile = new PlayerProfile();
            _parser.ProcessHtml(profile, fragment);
            return profile;
        }

        // -------------------------------------------------------------------
        // Integration tests against PlayerProfileScalorn.html
        // -------------------------------------------------------------------

        // Requirement 1.1: Character name
        [Test]
        public void ParseScalorn_Name_IsScalornScorpus()
        {
            var profile = ParseScalorn();
            Assert.That(profile.Name, Is.EqualTo("Scalorn Scorpus"));
        }

        // Requirement 1.2: Faction
        [Test]
        public void ParseScalorn_Faction_IsNEC()
        {
            var profile = ParseScalorn();
            Assert.That(profile.Faction, Is.EqualTo("NEC"));
        }

        // Requirement 2.1: Credits
        [Test]
        public void ParseScalorn_TotalCredits_Is11982019Point28()
        {
            var profile = ParseScalorn();
            Assert.That(profile.TotalCredits, Is.EqualTo(11982019.28m));
        }

        // Requirement 3.1, 3.2, 3.3: Public rank
        [Test]
        public void ParseScalorn_PublicRank_Level42()
        {
            var profile = ParseScalorn();
            Assert.That(profile.Public.Rank, Is.EqualTo(42));
        }

        [Test]
        public void ParseScalorn_PublicRank_Title()
        {
            var profile = ParseScalorn();
            Assert.That(profile.Public.Title, Is.EqualTo("Under Secretary (Grade 3)"));
        }

        [Test]
        public void ParseScalorn_PublicRank_CurrentXP()
        {
            var profile = ParseScalorn();
            Assert.That(profile.Public.CurrentXP, Is.EqualTo(1010379L));
        }

        [Test]
        public void ParseScalorn_PublicRank_NextXP()
        {
            var profile = ParseScalorn();
            Assert.That(profile.Public.NextXP, Is.EqualTo(3063750L));
        }

        // Requirement 3.1, 3.2, 3.3: Private rank
        [Test]
        public void ParseScalorn_PrivateRank_Level42()
        {
            var profile = ParseScalorn();
            Assert.That(profile.Private.Rank, Is.EqualTo(42));
        }

        [Test]
        public void ParseScalorn_PrivateRank_Title()
        {
            var profile = ParseScalorn();
            Assert.That(profile.Private.Title, Is.EqualTo("Chief Operations Officer (Grade 3)"));
        }

        [Test]
        public void ParseScalorn_PrivateRank_CurrentXP()
        {
            var profile = ParseScalorn();
            Assert.That(profile.Private.CurrentXP, Is.EqualTo(1299791L));
        }

        [Test]
        public void ParseScalorn_PrivateRank_NextXP()
        {
            var profile = ParseScalorn();
            Assert.That(profile.Private.NextXP, Is.EqualTo(3063750L));
        }

        // Requirement 3.1, 3.2, 3.3: Military rank
        [Test]
        public void ParseScalorn_MilitaryRank_Level5()
        {
            var profile = ParseScalorn();
            Assert.That(profile.Military.Rank, Is.EqualTo(5));
        }

        [Test]
        public void ParseScalorn_MilitaryRank_Title()
        {
            var profile = ParseScalorn();
            Assert.That(profile.Military.Title, Is.EqualTo("Spacer (Grade 1)"));
        }

        [Test]
        public void ParseScalorn_MilitaryRank_CurrentXP()
        {
            var profile = ParseScalorn();
            Assert.That(profile.Military.CurrentXP, Is.EqualTo(2402L));
        }

        [Test]
        public void ParseScalorn_MilitaryRank_NextXP()
        {
            var profile = ParseScalorn();
            Assert.That(profile.Military.NextXP, Is.EqualTo(5400L));
        }

        // Requirement 4.1: Skill points
        [Test]
        public void ParseScalorn_SkillPoints_Is42()
        {
            var profile = ParseScalorn();
            Assert.That(profile.SkillPoints, Is.EqualTo(42));
        }

        // Requirement 5.1, 5.2, 5.3: Skill group states
        // Colony Director is locked (Disabled div present)
        [Test]
        public void ParseScalorn_SkillGroup_ColonyDirector_IsLocked()
        {
            var profile = ParseScalorn();
            Assert.That(profile.GetSkillGroup(SkillGroupName.ColonyDirector), Is.False);
        }

        // Colony Founder is unlocked
        [Test]
        public void ParseScalorn_SkillGroup_ColonyFounder_IsUnlocked()
        {
            var profile = ParseScalorn();
            Assert.That(profile.GetSkillGroup(SkillGroupName.ColonyFounder), Is.True);
        }

        // Colony Operations is unlocked
        [Test]
        public void ParseScalorn_SkillGroup_ColonyOperations_IsUnlocked()
        {
            var profile = ParseScalorn();
            Assert.That(profile.GetSkillGroup(SkillGroupName.ColonyOperations), Is.True);
        }

        // Commander is locked
        [Test]
        public void ParseScalorn_SkillGroup_Commander_IsLocked()
        {
            var profile = ParseScalorn();
            Assert.That(profile.GetSkillGroup(SkillGroupName.Commander), Is.False);
        }

        // Engineer is locked
        [Test]
        public void ParseScalorn_SkillGroup_Engineer_IsLocked()
        {
            var profile = ParseScalorn();
            Assert.That(profile.GetSkillGroup(SkillGroupName.Engineer), Is.False);
        }

        // Job Management is unlocked
        [Test]
        public void ParseScalorn_SkillGroup_JobManagement_IsUnlocked()
        {
            var profile = ParseScalorn();
            Assert.That(profile.GetSkillGroup(SkillGroupName.JobManagement), Is.True);
        }

        // Researcher is unlocked
        [Test]
        public void ParseScalorn_SkillGroup_Researcher_IsUnlocked()
        {
            var profile = ParseScalorn();
            Assert.That(profile.GetSkillGroup(SkillGroupName.Researcher), Is.True);
        }

        // Surveyor is unlocked
        [Test]
        public void ParseScalorn_SkillGroup_Surveyor_IsUnlocked()
        {
            var profile = ParseScalorn();
            Assert.That(profile.GetSkillGroup(SkillGroupName.Surveyor), Is.True);
        }

        // Requirement 6.1: Individual skill levels
        [Test]
        public void ParseScalorn_Skill_Builder_Level1()
        {
            var profile = ParseScalorn();
            Assert.That(profile.GetSkill(SkillName.Builder).Level, Is.EqualTo(1));
        }

        [Test]
        public void ParseScalorn_Skill_RefiningFocus_Level1()
        {
            var profile = ParseScalorn();
            Assert.That(profile.GetSkill(SkillName.RefiningFocus).Level, Is.EqualTo(1));
        }

        [Test]
        public void ParseScalorn_Skill_ExtractionFocus_Level1()
        {
            var profile = ParseScalorn();
            Assert.That(profile.GetSkill(SkillName.ExtractionFocus).Level, Is.EqualTo(1));
        }

        [Test]
        public void ParseScalorn_Skill_ContractManagement_Level4()
        {
            var profile = ParseScalorn();
            Assert.That(profile.GetSkill(SkillName.ContractManagement).Level, Is.EqualTo(4));
        }

        [Test]
        public void ParseScalorn_Skill_ResearchMethods_Level1()
        {
            var profile = ParseScalorn();
            Assert.That(profile.GetSkill(SkillName.ResearchMethods).Level, Is.EqualTo(1));
        }

        [Test]
        public void ParseScalorn_Skill_SurveyingMethods_Level1()
        {
            var profile = ParseScalorn();
            Assert.That(profile.GetSkill(SkillName.SurveyingMethods).Level, Is.EqualTo(1));
        }

        [Test]
        public void ParseScalorn_Skill_ScanningMethods_Level1()
        {
            var profile = ParseScalorn();
            Assert.That(profile.GetSkill(SkillName.ScanningMethods).Level, Is.EqualTo(1));
        }

        [Test]
        public void ParseScalorn_Skill_Quartermaster_Level1()
        {
            var profile = ParseScalorn();
            Assert.That(profile.GetSkill(SkillName.Quartermaster).Level, Is.EqualTo(1));
        }

        [Test]
        public void ParseScalorn_Skill_HumanResources_Level0()
        {
            var profile = ParseScalorn();
            Assert.That(profile.GetSkill(SkillName.HumanResources).Level, Is.EqualTo(0));
        }

        // Requirement 6.2: Training status
        [Test]
        public void ParseScalorn_Skill_ContractManagement_TrainingStarted()
        {
            var profile = ParseScalorn();
            Assert.That(profile.GetSkill(SkillName.ContractManagement).TrainingStarted, Is.True);
        }

        [Test]
        public void ParseScalorn_Skill_Builder_NotTraining()
        {
            var profile = ParseScalorn();
            Assert.That(profile.GetSkill(SkillName.Builder).TrainingStarted, Is.False);
        }

        // Requirement 6.3: Training time
        [Test]
        public void ParseScalorn_Skill_ContractManagement_TrainingTime()
        {
            var profile = ParseScalorn();
            var skill = profile.GetSkill(SkillName.ContractManagement);
            // 22 days, 9 hours = (22*24 + 9) * 3600 = 537 * 3600 = 1933200 seconds
            // Allow +/-5s tolerance because TimeRemaining is computed from DateTime.UtcNow
            Assert.That(skill.CompletionTime.TimeRemaining,
                Is.InRange(1933200L - 5, 1933200L));
        }

        // Requirement 9.1: CitizenId
        [Test]
        public void ParseScalorn_CitizenId()
        {
            var profile = ParseScalorn();
            Assert.That(profile.CitizenId, Is.EqualTo("43 - 4944 - 3a32 - 3339"));
        }

        // Requirement 9.3: ActiveTime
        [Test]
        public void ParseScalorn_ActiveTime()
        {
            var profile = ParseScalorn();
            Assert.That(profile.ActiveTime, Is.EqualTo("1Mn 2W 2D 6H 47m"));
        }

        // Requirement 9.2: RegistrationDate
        [Test]
        public void ParseScalorn_RegistrationDate()
        {
            var profile = ParseScalorn();
            Assert.That(profile.RegistrationDate, Is.EqualTo("2223-01-06-16:20"));
        }

        // -------------------------------------------------------------------
        // Edge case tests (Task 7.2)
        // -------------------------------------------------------------------

        // Requirement 1.3: Empty HTML string -- parser doesn't crash
        [Test]
        public void ProcessHtml_EmptyString_DoesNotCrash()
        {
            var profile = new PlayerProfile { Name = "Original", Faction = "ORG" };
            _parser.ProcessHtml(profile, string.Empty);
            Assert.That(profile.Name, Is.EqualTo("Original"));
            Assert.That(profile.Faction, Is.EqualTo("ORG"));
        }

        [Test]
        public void ProcessHtml_NullString_DoesNotCrash()
        {
            var profile = new PlayerProfile { Name = "Original", Faction = "ORG" };
            _parser.ProcessHtml(profile, null);
            Assert.That(profile.Name, Is.EqualTo("Original"));
        }

        // Requirement 1.3: Missing ui_character_detail -- name and faction unchanged
        [Test]
        public void ProcessHtml_MissingCharacterDetail_NameUnchanged()
        {
            var profile = new PlayerProfile { Name = "Original", Faction = "ORG" };
            string html = "<div id='some_other_element'>content</div>";
            _parser.ProcessHtml(profile, html);
            Assert.That(profile.Name, Is.EqualTo("Original"));
            Assert.That(profile.Faction, Is.EqualTo("ORG"));
        }

        // Requirement 3.4: Missing rank sections -- ranks unchanged
        [Test]
        public void ProcessHtml_MissingRankSections_RanksUnchanged()
        {
            var profile = new PlayerProfile();
            profile.Public.Rank = 10;
            profile.Public.CurrentXP = 500;
            string html = "<div id='ui_character_detail'><div class='ui_text_white'>Test Name</div></div>";
            _parser.ProcessHtml(profile, html);
            Assert.That(profile.Public.Rank, Is.EqualTo(10));
            Assert.That(profile.Public.CurrentXP, Is.EqualTo(500));
        }

        // Requirement 2.2: Missing credit element -- TotalCredits unchanged
        [Test]
        public void ProcessHtml_MissingCreditElement_CreditsUnchanged()
        {
            var profile = new PlayerProfile { TotalCredits = 999m };
            string html = "<div id='ui_character_detail'><div class='ui_text_white'>Test</div></div>";
            _parser.ProcessHtml(profile, html);
            Assert.That(profile.TotalCredits, Is.EqualTo(999m));
        }

        // Requirement 4.2: Missing skill points element -- SkillPoints unchanged
        [Test]
        public void ProcessHtml_MissingSkillPoints_Unchanged()
        {
            var profile = new PlayerProfile { SkillPoints = 5 };
            string html = "<div>no skill points here</div>";
            _parser.ProcessHtml(profile, html);
            Assert.That(profile.SkillPoints, Is.EqualTo(5));
        }

        // Malformed numbers -- graceful fallback
        [Test]
        public void ParseFormattedNumber_EmptyString_ReturnsZero()
        {
            Assert.That(PlayerProfileParser.ParseFormattedNumber(""), Is.EqualTo(0));
        }

        [Test]
        public void ParseFormattedNumber_NullString_ReturnsZero()
        {
            Assert.That(PlayerProfileParser.ParseFormattedNumber(null), Is.EqualTo(0));
        }

        [Test]
        public void ParseFormattedNumber_NonNumeric_ReturnsZero()
        {
            Assert.That(PlayerProfileParser.ParseFormattedNumber("abc"), Is.EqualTo(0));
        }

        [Test]
        public void ParseFormattedNumber_ValidCommaFormatted_ParsesCorrectly()
        {
            Assert.That(PlayerProfileParser.ParseFormattedNumber("1,234,567"), Is.EqualTo(1234567L));
        }

        // Requirement 6.5: Unknown skill names -- logged and skipped, no crash
        [Test]
        public void ProcessHtml_UnknownSkillName_DoesNotCrash()
        {
            string html = @"<div class='Profile_Skill_Group'>
                <div class='Profile_Skill_Group_Name'>Known Group</div>
                <div class='Profile_Skill_Group_Skills_Skill'>
                    <div class='Profile_Skill_Group_Skills_Skill_Name'>Unknown Skill XYZ</div>
                </div>
            </div>";
            var profile = new PlayerProfile();
            _parser.ProcessHtml(profile, html);
            // Should not throw; unknown skill is skipped
            Assert.That(profile.Skills.Count, Is.EqualTo(0));
        }

        // NormalizeWhitespace edge cases
        [Test]
        public void NormalizeWhitespace_Null_ReturnsEmpty()
        {
            Assert.That(PlayerProfileParser.NormalizeWhitespace(null), Is.EqualTo(string.Empty));
        }

        [Test]
        public void NormalizeWhitespace_MultipleSpaces_CollapsesToSingle()
        {
            Assert.That(PlayerProfileParser.NormalizeWhitespace("  hello   world  "), Is.EqualTo("hello world"));
        }

        // ParseTrainingTime edge cases
        [Test]
        public void ParseTrainingTime_EmptyString_ReturnsZero()
        {
            Assert.That(PlayerProfileParser.ParseTrainingTime(""), Is.EqualTo(0));
        }

        [Test]
        public void ParseTrainingTime_OnlyHours_ParsesCorrectly()
        {
            // 5 hours = 5 * 3600 = 18000
            Assert.That(PlayerProfileParser.ParseTrainingTime("5 hours"), Is.EqualTo(18000L));
        }

        [Test]
        public void ParseTrainingTime_DaysAndHours_ParsesCorrectly()
        {
            // 22 days, 9 hours = (22*24 + 9) * 3600 = 1933200
            Assert.That(PlayerProfileParser.ParseTrainingTime("22 days, 9 hours"), Is.EqualTo(1933200L));
        }
    }
}
