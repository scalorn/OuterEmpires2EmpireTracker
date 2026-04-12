using NUnit.Framework;
using OE2EmpireTracker.Parsers;

namespace OE2EmpireTracker.Tests.Parsers
{
    [TestFixture]
    public class ClipboardContentDetectorTests
    {
        [Test]
        public void Detect_ColonyHtml_ReturnsColony()
        {
            string html = "<div class='ColonyInformation_PlanetOverview_StatInformation_Label'>Planet</div>";
            Assert.That(ClipboardContentDetector.Detect(html),
                Is.EqualTo(ClipboardContentDetector.ContentType.Colony));
        }

        [Test]
        public void Detect_SurveyHtml_ReturnsSurvey()
        {
            string html = "<div class='ScanDetailOutputResourceName'>Halogen</div>";
            Assert.That(ClipboardContentDetector.Detect(html),
                Is.EqualTo(ClipboardContentDetector.ContentType.Survey));
        }

        [Test]
        public void Detect_BlueprintHtml_ReturnsBlueprint()
        {
            string html = "<div class='ShipComponentProperty'>Damage</div>";
            Assert.That(ClipboardContentDetector.Detect(html),
                Is.EqualTo(ClipboardContentDetector.ContentType.Blueprint));
        }

        [Test]
        public void Detect_BlueprintDescription_ReturnsBlueprint()
        {
            string html = "<div class='SmallSlideOut_Form_Row_Description'>A particle beamer</div>";
            Assert.That(ClipboardContentDetector.Detect(html),
                Is.EqualTo(ClipboardContentDetector.ContentType.Blueprint));
        }

        [Test]
        public void Detect_PlayerProfileHtml_ReturnsPlayerProfile()
        {
            string html = "<div id='ui_character_detail'><div class='ui_text_white'>TestPlayer</div></div>";
            Assert.That(ClipboardContentDetector.Detect(html),
                Is.EqualTo(ClipboardContentDetector.ContentType.PlayerProfile));
        }

        [Test]
        public void Detect_ProfileSkillGroup_ReturnsPlayerProfile()
        {
            string html = "<div class='Profile_Skill_Group'>Skills</div>";
            Assert.That(ClipboardContentDetector.Detect(html),
                Is.EqualTo(ClipboardContentDetector.ContentType.PlayerProfile));
        }

        [Test]
        public void Detect_MarketListing_ReturnsMarketListing()
        {
            string html = "<div class='Market_ShipComponentProperty'>Damage: 100</div>";
            Assert.That(ClipboardContentDetector.Detect(html),
                Is.EqualTo(ClipboardContentDetector.ContentType.MarketListing));
        }

        [Test]
        public void Detect_EmptyHtml_ReturnsUnknown()
        {
            Assert.That(ClipboardContentDetector.Detect(""),
                Is.EqualTo(ClipboardContentDetector.ContentType.Unknown));
        }

        [Test]
        public void Detect_NullHtml_ReturnsUnknown()
        {
            Assert.That(ClipboardContentDetector.Detect(null),
                Is.EqualTo(ClipboardContentDetector.ContentType.Unknown));
        }

        [Test]
        public void Detect_UnrelatedHtml_ReturnsUnknown()
        {
            string html = "<div class='some_random_class'>Hello</div>";
            Assert.That(ClipboardContentDetector.Detect(html),
                Is.EqualTo(ClipboardContentDetector.ContentType.Unknown));
        }

        [Test]
        public void Detect_BlueprintResourcesTab_ReturnsSurvey()
        {
            // Blueprint resources tab has ScanDetailOutputResourceName but no ShipComponentProperty
            // This is expected — the blueprint import handler allows Survey content type through
            string html = "<div class='ScanDetailOutputResourceName'>Iron</div><div class='ScanDetailOutputResourceDetail'>50/h</div>";
            Assert.That(ClipboardContentDetector.Detect(html),
                Is.EqualTo(ClipboardContentDetector.ContentType.Survey));
        }

        [Test]
        public void Detect_SurveyWithProfileChrome_ReturnsSurvey()
        {
            // Game pages may include ui_character_detail in page chrome alongside survey content
            string html = "<div id='ui_character_detail'>Player</div><div class='ScanDetailOutputResourceName'>Halogen</div>";
            Assert.That(ClipboardContentDetector.Detect(html),
                Is.EqualTo(ClipboardContentDetector.ContentType.Survey));
        }

        [Test]
        public void Detect_ColonyWithProfileChrome_ReturnsColony()
        {
            // Game pages may include ui_character_detail in page chrome alongside colony content
            string html = "<div id='ui_character_detail'>Player</div><div class='ColonyInformation_PlanetOverview'>Colony</div>";
            Assert.That(ClipboardContentDetector.Detect(html),
                Is.EqualTo(ClipboardContentDetector.ContentType.Colony));
        }

        [Test]
        public void Detect_BlueprintWithProfileChrome_ReturnsBlueprint()
        {
            // Game pages may include ui_character_detail in page chrome alongside blueprint content
            string html = "<div id='ui_character_detail'>Player</div><div class='ShipComponentProperty'>Damage</div>";
            Assert.That(ClipboardContentDetector.Detect(html),
                Is.EqualTo(ClipboardContentDetector.ContentType.Blueprint));
        }

        [Test]
        public void Detect_MarketWithProfileChrome_ReturnsMarketListing()
        {
            // Game pages may include ui_character_detail in page chrome alongside market content
            string html = "<div id='ui_character_detail'>Player</div><div class='Market_ShipComponentProperty'>Item</div>";
            Assert.That(ClipboardContentDetector.Detect(html),
                Is.EqualTo(ClipboardContentDetector.ContentType.MarketListing));
        }

        [TestCase("colony data", ClipboardContentDetector.ContentType.Colony)]
        [TestCase("survey data", ClipboardContentDetector.ContentType.Survey)]
        [TestCase("blueprint data", ClipboardContentDetector.ContentType.Blueprint)]
        [TestCase("player profile data", ClipboardContentDetector.ContentType.PlayerProfile)]
        [TestCase("market listing data", ClipboardContentDetector.ContentType.MarketListing)]
        [TestCase("unrecognized content", ClipboardContentDetector.ContentType.Unknown)]
        public void GetDescription_ReturnsExpectedText(string expected, ClipboardContentDetector.ContentType type)
        {
            Assert.That(ClipboardContentDetector.GetDescription(type), Is.EqualTo(expected));
        }
    }
}
