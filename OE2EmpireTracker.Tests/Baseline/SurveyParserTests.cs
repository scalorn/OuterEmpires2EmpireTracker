using NUnit.Framework;
using OE2EmpireTracker.Baseline;
using System.IO;
using System.Linq;

namespace OE2EmpireTracker.Tests.Baseline
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
            return OE2EmpireTracker.Forms.Blueprint.BlueprintScanner
                .ExtractHtmlFragmentFromClipboardData(clipboardData);
        }

        // -----------------------------------------------------------------------
        // ParseDescription
        // -----------------------------------------------------------------------

        [Test]
        public void ParseDescription_ExtractsDateTimeAndScannedBy()
        {
            var survey = new Survey();
            SurveyParser.ParseDescription(survey,
                "A detailed survey report taken on 27JUL24-11:44p by Scalorn Scorpus");

            Assert.AreEqual("27JUL24-11:44p", survey.DateTime);
            Assert.AreEqual("Scalorn Scorpus", survey.ScannedBy);
        }

        [Test]
        public void ParseDescription_GeneratedOn_ExtractsDateTimeAndScannedBy()
        {
            var survey = new Survey();
            SurveyParser.ParseDescription(survey,
                "Survey report generated on 19FEB26-08:41p by Scalorn Scorpus");

            Assert.AreEqual("19FEB26-08:41p", survey.DateTime);
            Assert.AreEqual("Scalorn Scorpus", survey.ScannedBy);
        }

        [Test]
        public void ParseDescription_NoMatch_DoesNotCrash()
        {
            var survey = new Survey();
            SurveyParser.ParseDescription(survey, "Some unrelated text");
            Assert.IsNull(survey.DateTime);
        }

        [Test]
        public void ParseDescription_EmptyString_DoesNotCrash()
        {
            var survey = new Survey();
            SurveyParser.ParseDescription(survey, "");
            Assert.IsNull(survey.DateTime);
        }

        // -----------------------------------------------------------------------
        // ParseTitle
        // -----------------------------------------------------------------------

        [Test]
        public void ParseTitle_FullFormat_ExtractsPlanetNameAndSurveyID()
        {
            var survey = new Survey();
            SurveyParser.ParseTitle(survey, "Zeh Vazoran II M2, Zeh Vazoran (B465873)");

            Assert.AreEqual("Zeh Vazoran II M2", survey.PlanetName);
            Assert.AreEqual("B465873", survey.SurveyID);
        }

        [Test]
        public void ParseTitle_NoParen_UsesTitleAsPlanetName()
        {
            var survey = new Survey();
            SurveyParser.ParseTitle(survey, "Some Planet");
            Assert.AreEqual("Some Planet", survey.PlanetName);
        }

        [Test]
        public void ParseTitle_Empty_DoesNotCrash()
        {
            var survey = new Survey();
            SurveyParser.ParseTitle(survey, "");
            Assert.IsNull(survey.PlanetName);
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
            Assert.AreEqual("Post-Trans Metals", r.Resource);
            Assert.AreEqual("Low", r.Purity);
            Assert.AreEqual("41", r.Amount);
        }

        [Test]
        public void ParseResource_NoPurity_UsesFullNameAsResource()
        {
            var survey = new Survey();
            SurveyParser.ParseResource(survey, "Iron Ore", "100/hour");

            var r = survey.Resources["Iron Ore"];
            Assert.AreEqual("", r.Purity);
            Assert.AreEqual("100", r.Amount);
        }

        [Test]
        public void ParseResource_UnknownTrace_IsSkipped()
        {
            var survey = new Survey();
            SurveyParser.ParseResource(survey, "(Unknown Trace Elements)", "?/hour");
            Assert.AreEqual(0, survey.Resources.Count);
        }

        [Test]
        public void ParseResource_DuplicateResourceName_LastWins()
        {
            var survey = new Survey();
            SurveyParser.ParseResource(survey, "Heavy Trans-Metals (High Purity)", "20/hour");
            SurveyParser.ParseResource(survey, "Heavy Trans-Metals (High Purity)", "38/hour");

            Assert.AreEqual(1, survey.Resources.Count);
            Assert.AreEqual("38", survey.Resources["Heavy Trans-Metals"].Amount);
        }

        [Test]
        public void ParseResource_DecimalAmount_PreservesDecimal()
        {
            var survey = new Survey();
            SurveyParser.ParseResource(survey, "Heavy Trans-Metals (High Purity)", "36.3/hour");
            Assert.AreEqual("36.3", survey.Resources["Heavy Trans-Metals"].Amount);
        }

        // -----------------------------------------------------------------------
        // NormalizePurity
        // -----------------------------------------------------------------------

        [Test]
        public void NormalizePurity_Med_ReturnsMedium()
        {
            Assert.AreEqual("Medium", SurveyParser.NormalizePurity("Med"));
        }

        [Test]
        public void NormalizePurity_High_ReturnsHigh()
        {
            Assert.AreEqual("High", SurveyParser.NormalizePurity("High"));
        }

        [Test]
        public void NormalizePurity_Low_ReturnsLow()
        {
            Assert.AreEqual("Low", SurveyParser.NormalizePurity("Low"));
        }

        [Test]
        public void NormalizePurity_Empty_ReturnsEmpty()
        {
            Assert.AreEqual("", SurveyParser.NormalizePurity(""));
        }

        // -----------------------------------------------------------------------
        // ProcessHtml — SampleHtml (inline, no clipboard headers)
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_SampleData_ExtractsDateTime()
        {
            var survey = new Survey();
            _parser.ProcessHtml(survey, SampleHtml);
            Assert.AreEqual("27JUL24-11:44p", survey.DateTime);
        }

        [Test]
        public void ProcessHtml_SampleData_ExtractsScannedBy()
        {
            var survey = new Survey();
            _parser.ProcessHtml(survey, SampleHtml);
            Assert.AreEqual("Scalorn Scorpus", survey.ScannedBy);
        }

        [Test]
        public void ProcessHtml_SampleData_ExtractsKnownResources()
        {
            var survey = new Survey();
            _parser.ProcessHtml(survey, SampleHtml);

            Assert.IsTrue(survey.Resources.ContainsKey("Post-Trans Metals"));
            Assert.IsTrue(survey.Resources.ContainsKey("Heavy Trans-Metals"));
            Assert.IsTrue(survey.Resources.ContainsKey("Lanthanides"));
        }

        [Test]
        public void ProcessHtml_SampleData_SkipsUnknownTrace()
        {
            var survey = new Survey();
            _parser.ProcessHtml(survey, SampleHtml);
            Assert.IsFalse(survey.Resources.Keys.Any(k => k.Contains("Unknown")));
        }

        [Test]
        public void ProcessHtml_SampleData_PostTransMetals_CorrectValues()
        {
            var survey = new Survey();
            _parser.ProcessHtml(survey, SampleHtml);

            var r = survey.Resources["Post-Trans Metals"];
            Assert.AreEqual("Low", r.Purity);
            Assert.AreEqual("41", r.Amount);
        }

        [Test]
        public void ProcessHtml_SampleData_Lanthanides_CorrectValues()
        {
            var survey = new Survey();
            _parser.ProcessHtml(survey, SampleHtml);

            var r = survey.Resources["Lanthanides"];
            Assert.AreEqual("High", r.Purity);
            Assert.AreEqual("5", r.Amount);
        }

        [Test]
        public void ProcessHtml_SampleData_DuplicateHeavyTransMetals_LastAmountWins()
        {
            var survey = new Survey();
            _parser.ProcessHtml(survey, SampleHtml);

            var r = survey.Resources["Heavy Trans-Metals"];
            Assert.AreEqual("High", r.Purity);
            Assert.AreEqual("38", r.Amount);
        }

        [Test]
        public void ProcessHtml_EmptyHtml_DoesNotCrash()
        {
            var survey = new Survey();
            _parser.ProcessHtml(survey, "");
            Assert.AreEqual(0, survey.Resources.Count);
        }

        [Test]
        public void ProcessHtml_MalformedHtml_DoesNotCrash()
        {
            var survey = new Survey();
            _parser.ProcessHtml(survey, "<div>not a survey</div>");
            Assert.AreEqual(0, survey.Resources.Count);
        }

        [Test]
        public void ProcessHtml_NullHtml_DoesNotCrash()
        {
            var survey = new Survey();
            Assert.DoesNotThrow(() => _parser.ProcessHtml(survey, null));
        }

        // -----------------------------------------------------------------------
        // ZehVazoranIIM2 — full integration from external file
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
            Assert.AreEqual("Zeh Vazoran II M2", ParseZehVazoran().PlanetName);
        }

        [Test]
        public void ZehVazoran_ExtractsSurveyID()
        {
            Assert.AreEqual("B465873", ParseZehVazoran().SurveyID);
        }

        [Test]
        public void ZehVazoran_ExtractsDateTime()
        {
            Assert.AreEqual("19FEB26-08:41p", ParseZehVazoran().DateTime);
        }

        [Test]
        public void ZehVazoran_ExtractsScannedBy()
        {
            Assert.AreEqual("Scalorn Scorpus", ParseZehVazoran().ScannedBy);
        }

        [Test]
        public void ZehVazoran_HasThreeResources()
        {
            Assert.AreEqual(3, ParseZehVazoran().Resources.Count);
        }

        [Test]
        public void ZehVazoran_HeavyTransMetals_CorrectValues()
        {
            var r = ParseZehVazoran().Resources["Heavy Trans-Metals"];
            Assert.AreEqual("High", r.Purity);
            Assert.AreEqual("36.3", r.Amount);
        }

        [Test]
        public void ZehVazoran_ComplexMetallics_CorrectValues()
        {
            var r = ParseZehVazoran().Resources["Complex Metallics"];
            Assert.AreEqual("High", r.Purity);
            Assert.AreEqual("23.1", r.Amount);
        }

        [Test]
        public void ZehVazoran_AlkaliOrganics_CorrectValues()
        {
            var r = ParseZehVazoran().Resources["Alkali Organics"];
            Assert.AreEqual("Low", r.Purity);
            Assert.AreEqual("3.3", r.Amount);
        }

        [Test]
        public void ZehVazoran_SkipsUnknownTrace()
        {
            Assert.IsFalse(ParseZehVazoran().Resources.Keys.Any(k => k.Contains("Unknown")));
        }

        // -----------------------------------------------------------------------
        // QuogarV2249II — full integration from external file (Med Purity)
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
            Assert.AreEqual("Quogar V2249 II", ParseQuogar().PlanetName);
        }

        [Test]
        public void Quogar_ExtractsSurveyID()
        {
            Assert.AreEqual("FC2F6CC", ParseQuogar().SurveyID);
        }

        [Test]
        public void Quogar_ExtractsDateTime()
        {
            Assert.AreEqual("19FEB26-09:39p", ParseQuogar().DateTime);
        }

        [Test]
        public void Quogar_ExtractsScannedBy()
        {
            Assert.AreEqual("Scalorn Scorpus", ParseQuogar().ScannedBy);
        }

        [Test]
        public void Quogar_HasElevenResources()
        {
            Assert.AreEqual(11, ParseQuogar().Resources.Count);
        }

        [Test]
        public void Quogar_PostTransMetals_CorrectValues()
        {
            var r = ParseQuogar().Resources["Post-Trans Metals"];
            Assert.AreEqual("High", r.Purity);
            Assert.AreEqual("112.2", r.Amount);
        }

        [Test]
        public void Quogar_Halogens_CorrectValues()
        {
            var r = ParseQuogar().Resources["Halogens"];
            Assert.AreEqual("High", r.Purity);
            Assert.AreEqual("9.9", r.Amount);
        }

        [Test]
        public void Quogar_AlkaliOrganics_MedPurity_NormalizedToMedium()
        {
            var r = ParseQuogar().Resources["Alkali Organics"];
            Assert.AreEqual("Medium", r.Purity);
            Assert.AreEqual("72.6", r.Amount);
        }

        [Test]
        public void Quogar_SkipsUnknownTrace()
        {
            Assert.IsFalse(ParseQuogar().Resources.Keys.Any(k => k.Contains("Unknown")));
        }
    }
}
