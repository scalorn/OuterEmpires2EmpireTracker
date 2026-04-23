using System.IO;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;

namespace OE2EmpireTracker.Tests.Parsers
{
    [TestFixture]
    public class SurveyParserTests
    {
        private SurveyParser _parser;

        // Small inline fragment for basic unit tests (no clipboard headers)
        private const string SampleHtml = @"<div class=""SmallSlideOut_FormSection""><span> </span><div class=""SmallSlideOut_Form_Row_NameOfItem_Section""><div class=""SmallSlideOut_Form_Row_Text_Bold""></div><div class=""SmallSlideOut_Form_Row_Description SmallSlideOut_Form_Row_Description_Small"">A detailed survey report taken on 27JUL24-11:44p by Scalorn Scorpus</div></div></div><div class=""SmallSlideOut_FormSection""><div class=""SmallSlideOut_Form_Row""><div class=""ScanDetailOutput""><div class=""ScanRarityTypeRow"">Common Elements Detected:</div><div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"">Post-Trans Metals (Low Purity)</div> <div class=""div_block ui_text_blue_light ScanDetailOutputResourceDetail"">41/hour</div><div class=""ScanRarityTypeRow"">Uncommon Elements Detected:</div><div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"">Heavy Trans-Metals (High Purity)</div> <div class=""div_block ui_text_blue_light ScanDetailOutputResourceDetail"">20/hour</div> <div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"">Heavy Trans-Metals (High Purity)</div> <div class=""div_block ui_text_blue_light ScanDetailOutputResourceDetail"">38/hour</div><div class=""ScanRarityTypeRow"">Rare Elements Detected:</div><div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"">Lanthanides (High Purity)</div> <div class=""div_block ui_text_blue_light ScanDetailOutputResourceDetail"">5/hour</div><div class=""ScanRarityTypeRow"">Trace:</div><div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"">(Unknown Trace Elements)</div> <div class=""div_block ui_text_blue_light"">?/hour</div></div></div></div>";

        [SetUp]
        public void SetUp()
        {
            _parser = new SurveyParser();
        }

        private static string LoadTestData(string filename)
        {
            string baseDir = TestContext.CurrentContext.TestDirectory;
            string path = Path.Combine(baseDir, "TestData", filename);
            return File.ReadAllText(path);
        }

        private static string ExtractFragment(string clipboardData)
        {
            return OE2EmpireTracker.Parsers.BlueprintScanner
                .ExtractHtmlFragmentFromClipboardData(clipboardData);
        }

        // -----------------------------------------------------------------------
        // ParseDescription
        // -----------------------------------------------------------------------

        [Test]
        public void ParseDescription_ExtractsDateTimeAndScannedBy()
        {
            var survey = new Survey();
            SurveyParser.ParseDescription(
                survey,
                "A detailed survey report taken on 27JUL24-11:44p by Scalorn Scorpus");

            Assert.That(survey.DateTime, Is.EqualTo("2024-07-27T23:44:00Z"));
            Assert.That(survey.ScannedBy, Is.EqualTo("Scalorn Scorpus"));
        }

        [Test]
        public void ParseDescription_GeneratedOn_ExtractsDateTimeAndScannedBy()
        {
            var survey = new Survey();
            SurveyParser.ParseDescription(
                survey,
                "Survey report generated on 19FEB26-08:41p by Scalorn Scorpus");

            Assert.That(survey.DateTime, Is.EqualTo("2026-02-19T20:41:00Z"));
            Assert.That(survey.ScannedBy, Is.EqualTo("Scalorn Scorpus"));
        }

        [Test]
        public void ParseDescription_NoMatch_DoesNotCrash()
        {
            var survey = new Survey();
            SurveyParser.ParseDescription(survey, "Some unrelated text");
            Assert.That(survey.DateTime, Is.Null);
        }

        [Test]
        public void ParseDescription_EmptyString_DoesNotCrash()
        {
            var survey = new Survey();
            SurveyParser.ParseDescription(survey, string.Empty);
            Assert.That(survey.DateTime, Is.Null);
        }

        // -----------------------------------------------------------------------
        // ParseTitle
        // -----------------------------------------------------------------------

        [Test]
        public void ParseTitle_FullFormat_ExtractsPlanetNameAndSurveyID()
        {
            var survey = new Survey();
            SurveyParser.ParseTitle(survey, "Zeh Vazoran II M2, Zeh Vazoran (B465873)");

            Assert.That(survey.PlanetName, Is.EqualTo("Zeh Vazoran II M2"));
            Assert.That(survey.SurveyID, Is.EqualTo("B465873"));
        }

        [Test]
        public void ParseTitle_NoParen_UsesTitleAsPlanetName()
        {
            var survey = new Survey();
            SurveyParser.ParseTitle(survey, "Some Planet");
            Assert.That(survey.PlanetName, Is.EqualTo("Some Planet"));
        }

        [Test]
        public void ParseTitle_Empty_DoesNotCrash()
        {
            var survey = new Survey();
            SurveyParser.ParseTitle(survey, string.Empty);
            Assert.That(survey.PlanetName, Is.Null);
        }

        // -----------------------------------------------------------------------
        // ParseResource
        // -----------------------------------------------------------------------

        [Test]
        public void ParseResource_ExtractsNamePurityAndAmount()
        {
            var survey = new Survey();
            SurveyParser.ParseResource(survey, "Post-Trans Metals (Low Purity)", "41/hour");

            var r = survey.Resources["Post-Trans Metals"];
            Assert.That(r.Resource, Is.EqualTo("Post-Trans Metals"));
            Assert.That(r.Purity, Is.EqualTo("Low"));
            Assert.That(r.Amount, Is.EqualTo("41"));
        }

        [Test]
        public void ParseResource_NoPurity_UsesFullNameAsResource()
        {
            var survey = new Survey();
            SurveyParser.ParseResource(survey, "Iron Ore", "100/hour");

            var r = survey.Resources["Iron Ore"];
            Assert.That(r.Purity, Is.EqualTo(string.Empty));
            Assert.That(r.Amount, Is.EqualTo("100"));
        }

        [Test]
        public void ParseResource_UnknownTrace_IsSkipped()
        {
            var survey = new Survey();
            SurveyParser.ParseResource(survey, "(Unknown Trace Elements)", "?/hour");
            Assert.That(survey.Resources.Count, Is.EqualTo(0));
        }

        [Test]
        public void ParseResource_DuplicateResourceName_LastWins()
        {
            var survey = new Survey();
            SurveyParser.ParseResource(survey, "Heavy Trans-Metals (High Purity)", "20/hour");
            SurveyParser.ParseResource(survey, "Heavy Trans-Metals (High Purity)", "38/hour");

            Assert.That(survey.Resources.Count, Is.EqualTo(1));
            Assert.That(survey.Resources["Heavy Trans-Metals"].Amount, Is.EqualTo("38"));
        }

        [Test]
        public void ParseResource_DecimalAmount_PreservesDecimal()
        {
            var survey = new Survey();
            SurveyParser.ParseResource(survey, "Heavy Trans-Metals (High Purity)", "36.3/hour");
            Assert.That(survey.Resources["Heavy Trans-Metals"].Amount, Is.EqualTo("36.3"));
        }

        // -----------------------------------------------------------------------
        // NormalizePurity
        // -----------------------------------------------------------------------

        [Test]
        public void NormalizePurity_Med_ReturnsMedium()
        {
            Assert.That(SurveyParser.NormalizePurity("Med"), Is.EqualTo("Medium"));
        }

        [Test]
        public void NormalizePurity_High_ReturnsHigh()
        {
            Assert.That(SurveyParser.NormalizePurity("High"), Is.EqualTo("High"));
        }

        [Test]
        public void NormalizePurity_Low_ReturnsLow()
        {
            Assert.That(SurveyParser.NormalizePurity("Low"), Is.EqualTo("Low"));
        }

        [Test]
        public void NormalizePurity_Empty_ReturnsEmpty()
        {
            Assert.That(SurveyParser.NormalizePurity(string.Empty), Is.EqualTo(string.Empty));
        }

        // -----------------------------------------------------------------------
        // ProcessHtml -- SampleHtml (inline, no clipboard headers)
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_SampleData_ExtractsDateTime()
        {
            var survey = new Survey();
            _parser.ProcessHtml(survey, SampleHtml);
            Assert.That(survey.DateTime, Is.EqualTo("2024-07-27T23:44:00Z"));
        }

        [Test]
        public void ProcessHtml_SampleData_ExtractsScannedBy()
        {
            var survey = new Survey();
            _parser.ProcessHtml(survey, SampleHtml);
            Assert.That(survey.ScannedBy, Is.EqualTo("Scalorn Scorpus"));
        }

        [Test]
        public void ProcessHtml_SampleData_ExtractsKnownResources()
        {
            var survey = new Survey();
            _parser.ProcessHtml(survey, SampleHtml);

            Assert.That(survey.Resources.ContainsKey("Post-Trans Metals"), Is.True);
            Assert.That(survey.Resources.ContainsKey("Heavy Trans-Metals"), Is.True);
            Assert.That(survey.Resources.ContainsKey("Lanthanides"), Is.True);
        }

        [Test]
        public void ProcessHtml_SampleData_SkipsUnknownTrace()
        {
            var survey = new Survey();
            _parser.ProcessHtml(survey, SampleHtml);
            Assert.That(survey.Resources.Keys.Any(k => k.Contains("Unknown")), Is.False);
        }

        [Test]
        public void ProcessHtml_SampleData_PostTransMetals_CorrectValues()
        {
            var survey = new Survey();
            _parser.ProcessHtml(survey, SampleHtml);

            var r = survey.Resources["Post-Trans Metals"];
            Assert.That(r.Purity, Is.EqualTo("Low"));
            Assert.That(r.Amount, Is.EqualTo("41"));
        }

        [Test]
        public void ProcessHtml_SampleData_Lanthanides_CorrectValues()
        {
            var survey = new Survey();
            _parser.ProcessHtml(survey, SampleHtml);

            var r = survey.Resources["Lanthanides"];
            Assert.That(r.Purity, Is.EqualTo("High"));
            Assert.That(r.Amount, Is.EqualTo("5"));
        }

        [Test]
        public void ProcessHtml_SampleData_DuplicateHeavyTransMetals_LastAmountWins()
        {
            var survey = new Survey();
            _parser.ProcessHtml(survey, SampleHtml);

            var r = survey.Resources["Heavy Trans-Metals"];
            Assert.That(r.Purity, Is.EqualTo("High"));
            Assert.That(r.Amount, Is.EqualTo("38"));
        }

        [Test]
        public void ProcessHtml_EmptyHtml_DoesNotCrash()
        {
            var survey = new Survey();
            _parser.ProcessHtml(survey, string.Empty);
            Assert.That(survey.Resources.Count, Is.EqualTo(0));
        }

        [Test]
        public void ProcessHtml_MalformedHtml_DoesNotCrash()
        {
            var survey = new Survey();
            _parser.ProcessHtml(survey, "<div>not a survey</div>");
            Assert.That(survey.Resources.Count, Is.EqualTo(0));
        }

        [Test]
        public void ProcessHtml_NullHtml_DoesNotCrash()
        {
            var survey = new Survey();
            Assert.DoesNotThrow(() => _parser.ProcessHtml(survey, null));
        }

        // -----------------------------------------------------------------------
        // ZehVazoranIIM2 -- full integration from external file
        // -----------------------------------------------------------------------

        private Survey ParseZehVazoran()
        {
            string clipboardData = LoadTestData("ZehVazoranIIM2.html");
            string html = ExtractFragment(clipboardData);
            var survey = new Survey();
            _parser.ProcessHtml(survey, html);
            return survey;
        }

        [Test]
        public void ZehVazoran_ExtractsPlanetName()
        {
            Assert.That(ParseZehVazoran().PlanetName, Is.EqualTo("Zeh Vazoran II M2"));
        }

        [Test]
        public void ZehVazoran_ExtractsSurveyID()
        {
            Assert.That(ParseZehVazoran().SurveyID, Is.EqualTo("B465873"));
        }

        [Test]
        public void ZehVazoran_ExtractsDateTime()
        {
            Assert.That(ParseZehVazoran().DateTime, Is.EqualTo("2026-02-19T20:41:00Z"));
        }

        [Test]
        public void ZehVazoran_ExtractsScannedBy()
        {
            Assert.That(ParseZehVazoran().ScannedBy, Is.EqualTo("Scalorn Scorpus"));
        }

        [Test]
        public void ZehVazoran_HasThreeResources()
        {
            Assert.That(ParseZehVazoran().Resources.Count, Is.EqualTo(3));
        }

        [Test]
        public void ZehVazoran_HeavyTransMetals_CorrectValues()
        {
            var r = ParseZehVazoran().Resources["Heavy Trans-Metals"];
            Assert.That(r.Purity, Is.EqualTo("High"));
            Assert.That(r.Amount, Is.EqualTo("36.3"));
        }

        [Test]
        public void ZehVazoran_ComplexMetallics_CorrectValues()
        {
            var r = ParseZehVazoran().Resources["Complex Metallics"];
            Assert.That(r.Purity, Is.EqualTo("High"));
            Assert.That(r.Amount, Is.EqualTo("23.1"));
        }

        [Test]
        public void ZehVazoran_AlkaliOrganics_CorrectValues()
        {
            var r = ParseZehVazoran().Resources["Alkali Organics"];
            Assert.That(r.Purity, Is.EqualTo("Low"));
            Assert.That(r.Amount, Is.EqualTo("3.3"));
        }

        [Test]
        public void ZehVazoran_SkipsUnknownTrace()
        {
            Assert.That(ParseZehVazoran().Resources.Keys.Any(k => k.Contains("Unknown")), Is.False);
        }

        // -----------------------------------------------------------------------
        // QuogarV2249II -- full integration from external file (Med Purity)
        // -----------------------------------------------------------------------

        private Survey ParseQuogar()
        {
            string clipboardData = LoadTestData("QuogarV2249II.html");
            string html = ExtractFragment(clipboardData);
            var survey = new Survey();
            _parser.ProcessHtml(survey, html);
            return survey;
        }

        [Test]
        public void Quogar_ExtractsPlanetName()
        {
            Assert.That(ParseQuogar().PlanetName, Is.EqualTo("Quogar V2249 II"));
        }

        [Test]
        public void Quogar_ExtractsSurveyID()
        {
            Assert.That(ParseQuogar().SurveyID, Is.EqualTo("FC2F6CC"));
        }

        [Test]
        public void Quogar_ExtractsDateTime()
        {
            Assert.That(ParseQuogar().DateTime, Is.EqualTo("2026-02-19T21:39:00Z"));
        }

        [Test]
        public void Quogar_ExtractsScannedBy()
        {
            Assert.That(ParseQuogar().ScannedBy, Is.EqualTo("Scalorn Scorpus"));
        }

        [Test]
        public void Quogar_HasElevenResources()
        {
            Assert.That(ParseQuogar().Resources.Count, Is.EqualTo(11));
        }

        [Test]
        public void Quogar_PostTransMetals_CorrectValues()
        {
            var r = ParseQuogar().Resources["Post-Trans Metals"];
            Assert.That(r.Purity, Is.EqualTo("High"));
            Assert.That(r.Amount, Is.EqualTo("112.2"));
        }

        [Test]
        public void Quogar_Halogens_CorrectValues()
        {
            var r = ParseQuogar().Resources["Halogens"];
            Assert.That(r.Purity, Is.EqualTo("High"));
            Assert.That(r.Amount, Is.EqualTo("9.9"));
        }

        [Test]
        public void Quogar_AlkaliOrganics_MedPurity_NormalizedToMedium()
        {
            var r = ParseQuogar().Resources["Alkali Organics"];
            Assert.That(r.Purity, Is.EqualTo("Medium"));
            Assert.That(r.Amount, Is.EqualTo("72.6"));
        }

        [Test]
        public void Quogar_SkipsUnknownTrace()
        {
            Assert.That(ParseQuogar().Resources.Keys.Any(k => k.Contains("Unknown")), Is.False);
        }

        // -----------------------------------------------------------------------
        // AsteroidSurveySample -- full integration from external file (asteroid)
        // -----------------------------------------------------------------------

        private Survey ParseAsteroidSurvey()
        {
            string clipboardData = LoadTestData("AsteroidSurveySample.html");
            string html = ExtractFragment(clipboardData);
            var survey = new Survey();
            _parser.ProcessHtml(survey, html);
            return survey;
        }

        [Test]
        public void AsteroidSurvey_ExtractsPlanetName()
        {
            Assert.That(ParseAsteroidSurvey().PlanetName, Is.EqualTo("AST-DH-CA14"));
        }

        [Test]
        public void AsteroidSurvey_ExtractsSystemName()
        {
            Assert.That(ParseAsteroidSurvey().SystemName, Is.EqualTo("Dal Halcyion"));
        }

        [Test]
        public void AsteroidSurvey_ExtractsSurveyID()
        {
            Assert.That(ParseAsteroidSurvey().SurveyID, Is.EqualTo("B6CE913"));
        }

        [Test]
        public void AsteroidSurvey_ExtractsDateTime()
        {
            Assert.That(ParseAsteroidSurvey().DateTime, Is.EqualTo("2026-04-18T01:09:00Z"));
        }

        [Test]
        public void AsteroidSurvey_ExtractsScannedBy()
        {
            Assert.That(ParseAsteroidSurvey().ScannedBy, Is.EqualTo("Scalorn Scorpus"));
        }

        [Test]
        public void AsteroidSurvey_DetectsAsteroidType()
        {
            Assert.That(ParseAsteroidSurvey().SurveyType, Is.EqualTo(SurveyType.Asteroid));
        }

        [Test]
        public void AsteroidSurvey_HasFiveResources()
        {
            Assert.That(ParseAsteroidSurvey().Resources.Count, Is.EqualTo(5));
        }

        [Test]
        public void AsteroidSurvey_NobleGases_CorrectValues()
        {
            var r = ParseAsteroidSurvey().Resources["Noble Gases"];
            Assert.That(r.Purity, Is.EqualTo("High"));
            Assert.That(r.Amount, Is.EqualTo("23.1"));
        }

        [Test]
        public void AsteroidSurvey_HeavyNobleGases_CorrectValues()
        {
            var r = ParseAsteroidSurvey().Resources["Heavy Noble Gases"];
            Assert.That(r.Purity, Is.EqualTo("High"));
            Assert.That(r.Amount, Is.EqualTo("23.1"));
        }

        [Test]
        public void AsteroidSurvey_AlkaliOrganics_CorrectValues()
        {
            var r = ParseAsteroidSurvey().Resources["Alkali Organics"];
            Assert.That(r.Purity, Is.EqualTo("High"));
            Assert.That(r.Amount, Is.EqualTo("19.8"));
        }

        [Test]
        public void AsteroidSurvey_StrongAlkaliOrganics_CorrectValues()
        {
            var r = ParseAsteroidSurvey().Resources["Strong Alkali Organics"];
            Assert.That(r.Purity, Is.EqualTo("High"));
            Assert.That(r.Amount, Is.EqualTo("19.8"));
        }

        [Test]
        public void AsteroidSurvey_SuperheavyExotics_CorrectValues()
        {
            var r = ParseAsteroidSurvey().Resources["Superheavy Exotics"];
            Assert.That(r.Purity, Is.EqualTo("High"));
            Assert.That(r.Amount, Is.EqualTo("19.8"));
        }

        [Test]
        public void AsteroidSurvey_SkipsUnknownTrace()
        {
            Assert.That(ParseAsteroidSurvey().Resources.Keys.Any(k => k.Contains("Unknown")), Is.False);
        }
    }
}
