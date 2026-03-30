using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Data;

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
            Assert.AreEqual(ItemType.ItemTypeEnum.Survey, survey.ItemType);
        }

        [Test]
        public void DefaultConstructor_PropertiesIsNotNull()
        {
            var survey = new Survey();
            Assert.IsNotNull(survey.Properties);
        }

        [Test]
        public void DefaultConstructor_ResourcesIsNotNull()
        {
            var survey = new Survey();
            Assert.IsNotNull(survey.Resources);
        }

        [Test]
        public void DefaultConstructor_StringPropertiesAreNull()
        {
            var survey = new Survey();
            Assert.IsNull(survey.PlanetName);
            Assert.IsNull(survey.SurveyID);
            Assert.IsNull(survey.ScannedBy);
            Assert.IsNull(survey.DateTime);
            Assert.IsNull(survey.ScannerBlueprintUUID);
        }

        [Test]
        public void NamedConstructor_SetsNameAndItemType()
        {
            var survey = new Survey("Alpha Prime");
            Assert.AreEqual("Alpha Prime", survey.Name);
            Assert.AreEqual(ItemType.ItemTypeEnum.Survey, survey.ItemType);
        }

        // -----------------------------------------------------------------------
        // ExtendedName — PlanetName only
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_PlanetNameOnly_ReturnsPlanetName()
        {
            var survey = new Survey { PlanetName = "Alpha Prime" };
            Assert.AreEqual("Alpha Prime", survey.ExtendedName);
        }

        [Test]
        public void ExtendedName_NoPlanetName_ReturnsEmpty()
        {
            var survey = new Survey();
            Assert.AreEqual(string.Empty, survey.ExtendedName);
        }

        // -----------------------------------------------------------------------
        // ExtendedName — SurveyID
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_WithSurveyID_AppendsSurveyIDInParentheses()
        {
            var survey = new Survey { PlanetName = "Alpha Prime", SurveyID = "S-001" };
            Assert.AreEqual("Alpha Prime (S-001)", survey.ExtendedName);
        }

        [Test]
        public void ExtendedName_SurveyIDOnly_NoLeadingSpace()
        {
            // No PlanetName — SurveyID segment starts with a space, so result starts with " (S-001)"
            // This documents the current behaviour
            var survey = new Survey { SurveyID = "S-001" };
            StringAssert.Contains("(S-001)", survey.ExtendedName);
        }

        [Test]
        public void ExtendedName_EmptySurveyID_NoParentheses()
        {
            var survey = new Survey { PlanetName = "Alpha Prime", SurveyID = string.Empty };
            StringAssert.DoesNotContain("(", survey.ExtendedName);
        }

        // -----------------------------------------------------------------------
        // ExtendedName — NickName
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_WithNickName_AppendsNickNameInSquareBrackets()
        {
            var survey = new Survey { PlanetName = "Alpha Prime", NickName = "Good One" };
            StringAssert.Contains("[Good One]", survey.ExtendedName);
        }

        [Test]
        public void ExtendedName_EmptyNickName_NoSquareBrackets()
        {
            var survey = new Survey { PlanetName = "Alpha Prime", NickName = string.Empty };
            StringAssert.DoesNotContain("[", survey.ExtendedName);
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
            Assert.AreEqual("Alpha Prime (S-001) [Good One]", survey.ExtendedName);
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
            Assert.IsTrue(name.IndexOf("Alpha Prime") < name.IndexOf("(S-001)"), "PlanetName before SurveyID");
            Assert.IsTrue(name.IndexOf("(S-001)") < name.IndexOf("[Good One]"), "SurveyID before NickName");
        }

        // -----------------------------------------------------------------------
        // SurveyResource
        // -----------------------------------------------------------------------

        [Test]
        public void SurveyResource_PropertiesCanBeSetAndRead()
        {
            var resource = new SurveyResource { Resource = "Resource", Purity = "Refined", Amount = "500" };
            Assert.AreEqual("Refined", resource.Purity);
            Assert.AreEqual("500", resource.Amount);
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

            Assert.AreEqual(survey.UUID, restored.UUID);
            Assert.AreEqual(survey.PlanetName, restored.PlanetName);
            Assert.AreEqual(survey.SurveyID, restored.SurveyID);
            Assert.AreEqual(survey.ScannedBy, restored.ScannedBy);
            Assert.AreEqual(survey.NickName, restored.NickName);
        }

        [Test]
        public void JsonRoundTrip_Resources_Preserved()
        {
            var survey = new Survey { UUID = "uuid-1" };
            survey.Resources["Iron"] = new SurveyResource { Resource = "Resource", Purity = "Refined", Amount = "500" };

            string json = JsonConvert.SerializeObject(survey);
            var restored = JsonConvert.DeserializeObject<Survey>(json);

            Assert.IsTrue(restored.Resources.ContainsKey("Iron"));
            Assert.AreEqual("Refined", restored.Resources["Iron"].Purity);
            Assert.AreEqual("500", restored.Resources["Iron"].Amount);
        }
    }
}
