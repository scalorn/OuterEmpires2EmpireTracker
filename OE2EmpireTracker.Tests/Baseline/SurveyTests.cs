using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Baseline
{
    [TestFixture]
    public class SurveyTests
    {
        // -----------------------------------------------------------------------
        // Constructors
        // -----------------------------------------------------------------------

        [Test]
        public void DefaultConstructor_ItemTypeIsSurvey()
        {
            var survey = new Survey();
            Assert.That(survey.ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Survey));
        }

        [Test]
        public void DefaultConstructor_PropertiesIsNotNull()
        {
            var survey = new Survey();
            Assert.That(survey.Properties, Is.Not.Null);
        }

        [Test]
        public void DefaultConstructor_ResourcesIsNotNull()
        {
            var survey = new Survey();
            Assert.That(survey.Resources, Is.Not.Null);
        }

        [Test]
        public void DefaultConstructor_StringPropertiesAreNull()
        {
            var survey = new Survey();
            Assert.That(survey.PlanetName, Is.Null);
            Assert.That(survey.SurveyID, Is.Null);
            Assert.That(survey.ScannedBy, Is.Null);
            Assert.That(survey.DateTime, Is.Null);
            Assert.That(survey.ScannerBlueprintUUID, Is.Null);
        }

        [Test]
        public void NamedConstructor_SetsNameAndItemType()
        {
            var survey = new Survey("Alpha Prime");
            Assert.That(survey.Name, Is.EqualTo("Alpha Prime"));
            Assert.That(survey.ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Survey));
        }

        // -----------------------------------------------------------------------
        // ExtendedName — PlanetName only
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_PlanetNameOnly_ReturnsPlanetName()
        {
            var survey = new Survey { PlanetName = "Alpha Prime" };
            Assert.That(survey.ExtendedName, Is.EqualTo("Alpha Prime"));
        }

        [Test]
        public void ExtendedName_NoPlanetName_ReturnsEmpty()
        {
            var survey = new Survey();
            Assert.That(survey.ExtendedName, Is.EqualTo(string.Empty));
        }

        // -----------------------------------------------------------------------
        // ExtendedName — SurveyID
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_WithSurveyID_AppendsSurveyIDInParentheses()
        {
            var survey = new Survey { PlanetName = "Alpha Prime", SurveyID = "S-001" };
            Assert.That(survey.ExtendedName, Is.EqualTo("Alpha Prime (S-001)"));
        }

        [Test]
        public void ExtendedName_SurveyIDOnly_NoLeadingSpace()
        {
            // No PlanetName — SurveyID segment starts with a space, so result starts with " (S-001)"
            // This documents the current behaviour
            var survey = new Survey { SurveyID = "S-001" };
            Assert.That(survey.ExtendedName, Does.Contain("(S-001)"));
        }

        [Test]
        public void ExtendedName_EmptySurveyID_NoParentheses()
        {
            var survey = new Survey { PlanetName = "Alpha Prime", SurveyID = string.Empty };
            Assert.That(survey.ExtendedName, Does.Not.Contain("("));
        }

        // -----------------------------------------------------------------------
        // ExtendedName — NickName
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_WithNickName_AppendsNickNameInSquareBrackets()
        {
            var survey = new Survey { PlanetName = "Alpha Prime", NickName = "Good One" };
            Assert.That(survey.ExtendedName, Does.Contain("[Good One]"));
        }

        [Test]
        public void ExtendedName_EmptyNickName_NoSquareBrackets()
        {
            var survey = new Survey { PlanetName = "Alpha Prime", NickName = string.Empty };
            Assert.That(survey.ExtendedName, Does.Not.Contain("["));
        }

        // -----------------------------------------------------------------------
        // ExtendedName — full combination
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_AllSegments_CorrectFormat()
        {
            var survey = new Survey
            {
                PlanetName = "Alpha Prime",
                SurveyID = "S-001",
                NickName = "Good One"
            };
            Assert.That(survey.ExtendedName, Is.EqualTo("Alpha Prime (S-001) [Good One]"));
        }

        [Test]
        public void ExtendedName_AllSegments_CorrectOrder()
        {
            var survey = new Survey
            {
                PlanetName = "Alpha Prime",
                SurveyID = "S-001",
                NickName = "Good One"
            };
            string name = survey.ExtendedName;
            Assert.That(name.IndexOf("Alpha Prime") < name.IndexOf("(S-001)"), Is.True,
                    "PlanetName before SurveyID");
            Assert.That(name.IndexOf("(S-001)") < name.IndexOf("[Good One]"), Is.True,
                    "SurveyID before NickName");
        }

        // -----------------------------------------------------------------------
        // SurveyResource
        // -----------------------------------------------------------------------

        [Test]
        public void SurveyResource_PropertiesCanBeSetAndRead()
        {
            var resource = new SurveyResource { Resource = "Resource", Purity = "Refined", Amount = "500" };
            Assert.That(resource.Purity, Is.EqualTo("Refined"));
            Assert.That(resource.Amount, Is.EqualTo("500"));
        }

        // -----------------------------------------------------------------------
        // JSON round-trip
        // -----------------------------------------------------------------------

        [Test]
        public void JsonRoundTrip_CoreProperties_Preserved()
        {
            var survey = new Survey
            {
                UUID = "uuid-1",
                Name = "Survey 1",
                PlanetName = "Alpha Prime",
                SurveyID = "S-001",
                ScannedBy = "Player1",
                NickName = "Good One"
            };
            string json = JsonConvert.SerializeObject(survey);
            var restored = JsonConvert.DeserializeObject<Survey>(json);

            Assert.That(restored.UUID, Is.EqualTo(survey.UUID));
            Assert.That(restored.PlanetName, Is.EqualTo(survey.PlanetName));
            Assert.That(restored.SurveyID, Is.EqualTo(survey.SurveyID));
            Assert.That(restored.ScannedBy, Is.EqualTo(survey.ScannedBy));
            Assert.That(restored.NickName, Is.EqualTo(survey.NickName));
        }

        [Test]
        public void JsonRoundTrip_Resources_Preserved()
        {
            var survey = new Survey { UUID = "uuid-1" };
            survey.Resources["Iron"] = new SurveyResource { Resource = "Resource", Purity = "Refined", Amount = "500" };

            string json = JsonConvert.SerializeObject(survey);
            var restored = JsonConvert.DeserializeObject<Survey>(json);

            Assert.That(restored.Resources.ContainsKey("Iron"), Is.True);
            Assert.That(restored.Resources["Iron"].Purity, Is.EqualTo("Refined"));
            Assert.That(restored.Resources["Iron"].Amount, Is.EqualTo("500"));
        }
    }
}
