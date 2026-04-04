using NUnit.Framework;
using OE2EmpireTracker.Baseline;
using System.Linq;

namespace OE2EmpireTracker.Tests.Baseline
{
    [TestFixture]
    public class SurveyParserTests
    {
        private SurveyParser _parser;

        // Real HTML fragment from the game's survey clipboard data
        private const string SampleHtml = @"<div class=""SmallSlideOut_FormSection""><span> </span><div class=""SmallSlideOut_Form_Row_NameOfItem_Section""><div class=""SmallSlideOut_Form_Row_Text_Bold""></div><div class=""SmallSlideOut_Form_Row_Description SmallSlideOut_Form_Row_Description_Small"">A detailed survey report taken on 27JUL24-11:44p by Scalorn Scorpus</div></div></div><div class=""SmallSlideOut_FormSection""><div class=""SmallSlideOut_Form_Row""><div class=""ScanDetailOutput""><div class=""ScanRarityTypeRow"">Common Elements Detected:</div><div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"">Post-Trans Metals (Low Purity)</div> <div class=""div_block ui_text_blue_light ScanDetailOutputResourceDetail"">41/hour</div><div class=""ScanRarityTypeRow"">Uncommon Elements Detected:</div><div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"">Heavy Trans-Metals (High Purity)</div> <div class=""div_block ui_text_blue_light ScanDetailOutputResourceDetail"">20/hour</div> <div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"">Heavy Trans-Metals (High Purity)</div> <div class=""div_block ui_text_blue_light ScanDetailOutputResourceDetail"">38/hour</div><div class=""ScanRarityTypeRow"">Rare Elements Detected:</div><div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"">Lanthanides (High Purity)</div> <div class=""div_block ui_text_blue_light ScanDetailOutputResourceDetail"">5/hour</div><div class=""ScanRarityTypeRow"">Trace:</div><div class=""div_block ui_text_lightgrey ScanDetailOutputResourceName"">(Unknown Trace Elements)</div> <div class=""div_block ui_text_blue_light"">?/hour</div></div></div></div>";

        [SetUp]
        public void SetUp()
        {
            _parser = new SurveyParser();
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
        public void ParseDescription_NoMatch_DoesNotCrash()
        {
            var survey = new Survey();
            SurveyParser.ParseDescription(survey, "Some unrelated text");

            Assert.IsNull(survey.DateTime);
            Assert.IsNull(survey.ScannedBy);
        }

        [Test]
        public void ParseDescription_EmptyString_DoesNotCrash()
        {
            var survey = new Survey();
            SurveyParser.ParseDescription(survey, "");

            Assert.IsNull(survey.DateTime);
            Assert.IsNull(survey.ScannedBy);
        }

        // -----------------------------------------------------------------------
        // ParseResource
        // -----------------------------------------------------------------------

        [Test]
        public void ParseResource_ExtractsNamePurityAndAmount()
        {
            var survey = new Survey();
            SurveyParser.ParseResource(survey, "Post-Trans Metals (Low Purity)", "41/hour");

            Assert.IsTrue(survey.Resources.ContainsKey("Post-Trans Metals"));
            var r = survey.Resources["Post-Trans Metals"];
            Assert.AreEqual("Post-Trans Metals", r.Resource);
            Assert.AreEqual("Low Purity", r.Purity);
            Assert.AreEqual("41", r.Amount);
        }

        [Test]
        public void ParseResource_NoPurity_UsesFullNameAsResource()
        {
            var survey = new Survey();
            SurveyParser.ParseResource(survey, "Iron Ore", "100/hour");

            Assert.IsTrue(survey.Resources.ContainsKey("Iron Ore"));
            var r = survey.Resources["Iron Ore"];
            Assert.AreEqual("Iron Ore", r.Resource);
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

            // Dictionary key is resource name, so last write wins
            Assert.AreEqual(1, survey.Resources.Count);
            Assert.AreEqual("38", survey.Resources["Heavy Trans-Metals"].Amount);
        }

        // -----------------------------------------------------------------------
        // processHtml — full integration
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_SampleData_ExtractsDateTime()
        {
            var survey = new Survey();
            _parser.processHtml(survey, SampleHtml);

            Assert.AreEqual("27JUL24-11:44p", survey.DateTime);
        }

        [Test]
        public void ProcessHtml_SampleData_ExtractsScannedBy()
        {
            var survey = new Survey();
            _parser.processHtml(survey, SampleHtml);

            Assert.AreEqual("Scalorn Scorpus", survey.ScannedBy);
        }

        [Test]
        public void ProcessHtml_SampleData_ExtractsKnownResources()
        {
            var survey = new Survey();
            _parser.processHtml(survey, SampleHtml);

            // Should have 3 known resources (Unknown Trace skipped)
            // Post-Trans Metals, Heavy Trans-Metals (last wins for duplicate), Lanthanides
            Assert.IsTrue(survey.Resources.ContainsKey("Post-Trans Metals"));
            Assert.IsTrue(survey.Resources.ContainsKey("Heavy Trans-Metals"));
            Assert.IsTrue(survey.Resources.ContainsKey("Lanthanides"));
        }

        [Test]
        public void ProcessHtml_SampleData_SkipsUnknownTrace()
        {
            var survey = new Survey();
            _parser.processHtml(survey, SampleHtml);

            // Should not contain unknown trace elements
            Assert.IsFalse(survey.Resources.Keys.Any(k => k.Contains("Unknown")));
        }

        [Test]
        public void ProcessHtml_SampleData_PostTransMetals_CorrectValues()
        {
            var survey = new Survey();
            _parser.processHtml(survey, SampleHtml);

            var r = survey.Resources["Post-Trans Metals"];
            Assert.AreEqual("Low Purity", r.Purity);
            Assert.AreEqual("41", r.Amount);
        }

        [Test]
        public void ProcessHtml_SampleData_Lanthanides_CorrectValues()
        {
            var survey = new Survey();
            _parser.processHtml(survey, SampleHtml);

            var r = survey.Resources["Lanthanides"];
            Assert.AreEqual("High Purity", r.Purity);
            Assert.AreEqual("5", r.Amount);
        }

        [Test]
        public void ProcessHtml_SampleData_DuplicateHeavyTransMetals_LastAmountWins()
        {
            var survey = new Survey();
            _parser.processHtml(survey, SampleHtml);

            // Two Heavy Trans-Metals entries: 20/hour and 38/hour — last wins
            var r = survey.Resources["Heavy Trans-Metals"];
            Assert.AreEqual("High Purity", r.Purity);
            Assert.AreEqual("38", r.Amount);
        }

        [Test]
        public void ProcessHtml_EmptyHtml_DoesNotCrash()
        {
            var survey = new Survey();
            _parser.processHtml(survey, "");

            Assert.AreEqual(0, survey.Resources.Count);
        }

        [Test]
        public void ProcessHtml_MalformedHtml_DoesNotCrash()
        {
            var survey = new Survey();
            _parser.processHtml(survey, "<div>not a survey</div>");

            Assert.AreEqual(0, survey.Resources.Count);
        }

        [Test]
        public void ProcessHtml_NullHtml_DoesNotCrash()
        {
            var survey = new Survey();
            Assert.DoesNotThrow(() => _parser.processHtml(survey, null));
        }
    }
}
