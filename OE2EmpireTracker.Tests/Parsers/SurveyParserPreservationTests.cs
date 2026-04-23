using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Parsers
{
    /// <summary>
    /// Preservation property tests for asteroid survey import fix.
    /// These tests capture baseline behavior on UNFIXED code and must PASS,
    /// confirming that the fix does not regress existing functionality.
    ///
    /// Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5
    /// </summary>
    [TestFixture]
    public class SurveyParserPreservationTests
    {
        private SurveyParser _parser;

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
            return BlueprintScanner.ExtractHtmlFragmentFromClipboardData(clipboardData);
        }

        // -------------------------------------------------------------------
        // Helpers for FsCheck generators
        // -------------------------------------------------------------------

        private static Gen<string> NonEmptyStringGen()
        {
            return Arb.Default.NonEmptyString().Generator.Select(s => s.Get);
        }

        private static Gen<Survey> SurveyGen()
        {
            return from uuid in NonEmptyStringGen()
                   from planetName in NonEmptyStringGen()
                   from surveyId in NonEmptyStringGen()
                   from systemName in NonEmptyStringGen()
                   from scannedBy in NonEmptyStringGen()
                   from dateTime in NonEmptyStringGen()
                   from nickName in NonEmptyStringGen()
                   from ownerUuid in NonEmptyStringGen()
                   from isAsteroid in Arb.Default.Bool().Generator
                   let survey = MakeSurvey(uuid, ownerUuid, planetName, surveyId, systemName, scannedBy, dateTime, nickName, isAsteroid)
                   select survey;
        }

        private static Survey MakeSurvey(string uuid, string ownerUuid, string planetName,
            string surveyId, string systemName, string scannedBy, string dateTime,
            string nickName, bool isAsteroid)
        {
            var survey = new Survey();
            survey.UUID = uuid;
            survey.OwnerUUID = ownerUuid;
            survey.PlanetName = planetName;
            survey.SurveyID = surveyId;
            survey.SystemName = systemName;
            survey.ScannedBy = scannedBy;
            survey.DateTime = dateTime;
            survey.NickName = nickName;
            if (isAsteroid)
            {
                survey.SurveyType = SurveyType.Asteroid;
                survey.AsteroidUUID = "ast-" + uuid;
            }

            return survey;
        }

        // ===================================================================
        // Property: Planet survey HTML -> no asteroid type, resources parsed
        // Validates: Requirements 3.1, 3.3
        // ===================================================================

        [Test]
        public void PlanetSurvey_ZehVazoran_SurveyTypeIsPlanet()
        {
            // **Validates: Requirements 3.1**
            string clipboardData = LoadTestData("ZehVazoranIIM2.html");
            string html = ExtractFragment(clipboardData);
            var survey = new Survey();
            _parser.ProcessHtml(survey, html);

            Assert.That(survey.SurveyType, Is.EqualTo(SurveyType.Planet),
                "Planet survey should remain SurveyType.Planet");
        }

        [Test]
        public void PlanetSurvey_ZehVazoran_ResourcesParsedCorrectly()
        {
            // **Validates: Requirements 3.3**
            string clipboardData = LoadTestData("ZehVazoranIIM2.html");
            string html = ExtractFragment(clipboardData);
            var survey = new Survey();
            _parser.ProcessHtml(survey, html);

            Assert.That(survey.Resources.Count, Is.EqualTo(3));
            Assert.That(survey.Resources.ContainsKey("Heavy Trans-Metals"), Is.True);
            Assert.That(survey.Resources["Heavy Trans-Metals"].Purity, Is.EqualTo("High"));
            Assert.That(survey.Resources["Heavy Trans-Metals"].Amount, Is.EqualTo("36.3"));
            Assert.That(survey.Resources.ContainsKey("Complex Metallics"), Is.True);
            Assert.That(survey.Resources["Complex Metallics"].Purity, Is.EqualTo("High"));
            Assert.That(survey.Resources.ContainsKey("Alkali Organics"), Is.True);
            Assert.That(survey.Resources["Alkali Organics"].Purity, Is.EqualTo("Low"));
        }

        [Test]
        public void PlanetSurvey_Quogar_SurveyTypeIsPlanet()
        {
            // **Validates: Requirements 3.1**
            string clipboardData = LoadTestData("QuogarV2249II.html");
            string html = ExtractFragment(clipboardData);
            var survey = new Survey();
            _parser.ProcessHtml(survey, html);

            Assert.That(survey.SurveyType, Is.EqualTo(SurveyType.Planet),
                "Planet survey should remain SurveyType.Planet");
        }

        [Test]
        public void PlanetSurvey_Quogar_ResourcesParsedCorrectly()
        {
            // **Validates: Requirements 3.3**
            string clipboardData = LoadTestData("QuogarV2249II.html");
            string html = ExtractFragment(clipboardData);
            var survey = new Survey();
            _parser.ProcessHtml(survey, html);

            Assert.That(survey.Resources.Count, Is.EqualTo(11));
            Assert.That(survey.Resources.ContainsKey("Post-Trans Metals"), Is.True);
            Assert.That(survey.Resources["Post-Trans Metals"].Purity, Is.EqualTo("High"));
            Assert.That(survey.Resources["Post-Trans Metals"].Amount, Is.EqualTo("112.2"));
            Assert.That(survey.Resources.ContainsKey("Alkali Organics"), Is.True);
            Assert.That(survey.Resources["Alkali Organics"].Purity, Is.EqualTo("Medium"));
        }

        // ===================================================================
        // Property: Asteroid survey HTML -> resource parsing unchanged
        // Validates: Requirements 3.2, 3.3
        // ===================================================================

        [Test]
        public void AsteroidSurvey_ResourceNamesParsedCorrectly()
        {
            // **Validates: Requirements 3.3**
            string clipboardData = LoadTestData("AsteroidSurveySample.html");
            string html = ExtractFragment(clipboardData);
            var survey = new Survey();
            _parser.ProcessHtml(survey, html);

            Assert.That(survey.Resources.Count, Is.EqualTo(5));
            Assert.That(survey.Resources.ContainsKey("Noble Gases"), Is.True);
            Assert.That(survey.Resources.ContainsKey("Heavy Noble Gases"), Is.True);
            Assert.That(survey.Resources.ContainsKey("Alkali Organics"), Is.True);
            Assert.That(survey.Resources.ContainsKey("Strong Alkali Organics"), Is.True);
            Assert.That(survey.Resources.ContainsKey("Superheavy Exotics"), Is.True);
        }

        [Test]
        public void AsteroidSurvey_ResourcePuritiesParsedCorrectly()
        {
            // **Validates: Requirements 3.3**
            string clipboardData = LoadTestData("AsteroidSurveySample.html");
            string html = ExtractFragment(clipboardData);
            var survey = new Survey();
            _parser.ProcessHtml(survey, html);

            Assert.That(survey.Resources["Noble Gases"].Purity, Is.EqualTo("High"));
            Assert.That(survey.Resources["Heavy Noble Gases"].Purity, Is.EqualTo("High"));
            Assert.That(survey.Resources["Alkali Organics"].Purity, Is.EqualTo("High"));
            Assert.That(survey.Resources["Strong Alkali Organics"].Purity, Is.EqualTo("High"));
            Assert.That(survey.Resources["Superheavy Exotics"].Purity, Is.EqualTo("High"));
        }

        [Test]
        public void AsteroidSurvey_ResourceAmountsParsedCorrectly()
        {
            // **Validates: Requirements 3.3**
            string clipboardData = LoadTestData("AsteroidSurveySample.html");
            string html = ExtractFragment(clipboardData);
            var survey = new Survey();
            _parser.ProcessHtml(survey, html);

            Assert.That(survey.Resources["Noble Gases"].Amount, Is.EqualTo("23.1"));
            Assert.That(survey.Resources["Heavy Noble Gases"].Amount, Is.EqualTo("23.1"));
            Assert.That(survey.Resources["Alkali Organics"].Amount, Is.EqualTo("19.8"));
            Assert.That(survey.Resources["Strong Alkali Organics"].Amount, Is.EqualTo("19.8"));
            Assert.That(survey.Resources["Superheavy Exotics"].Amount, Is.EqualTo("19.8"));
        }

        // ===================================================================
        // Property: Asteroid type detection via /cycle rate units
        // Validates: Requirements 3.2
        // ===================================================================

        [Test]
        public void AsteroidSurvey_DetectsAsteroidType()
        {
            // **Validates: Requirements 3.2**
            string clipboardData = LoadTestData("AsteroidSurveySample.html");
            string html = ExtractFragment(clipboardData);
            var survey = new Survey();
            _parser.ProcessHtml(survey, html);

            Assert.That(survey.SurveyType, Is.EqualTo(SurveyType.Asteroid),
                "Asteroid survey should be detected as SurveyType.Asteroid");
        }

        [Test]
        public void ParseResource_CycleRateUnit_SetsSurveyTypeAsteroid()
        {
            // **Validates: Requirements 3.2**
            var survey = new Survey();
            SurveyParser.ParseResource(survey, "Noble Gases (High Purity)", "23.1/cycle");

            Assert.That(survey.SurveyType, Is.EqualTo(SurveyType.Asteroid),
                "/cycle rate unit should set SurveyType to Asteroid");
            Assert.That(survey.Resources["Noble Gases"].Amount, Is.EqualTo("23.1"));
        }

        // ===================================================================
        // Property: Survey dedup merge preserves UUID, OwnerUUID, NickName
        // Validates: Requirements 3.5
        // ===================================================================

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MergeData_PreservesUUID_OwnerUUID_NickName()
        {
            // **Validates: Requirements 3.5**
            var gen = from existing in SurveyGen()
                      from source in SurveyGen()
                      select new { Existing = existing, Source = source };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var originalUuid = data.Existing.UUID;
                var originalOwnerUuid = data.Existing.OwnerUUID;
                var originalNickName = data.Existing.NickName;

                SurveyImportHelper.MergeData(data.Existing, data.Source);

                var uuidPreserved = (data.Existing.UUID == originalUuid)
                    .Label(string.Format("UUID changed from '{0}' to '{1}'", originalUuid, data.Existing.UUID));
                var ownerPreserved = (data.Existing.OwnerUUID == originalOwnerUuid)
                    .Label(string.Format("OwnerUUID changed from '{0}' to '{1}'", originalOwnerUuid, data.Existing.OwnerUUID));
                var nickNamePreserved = (data.Existing.NickName == originalNickName)
                    .Label(string.Format("NickName changed from '{0}' to '{1}'", originalNickName, data.Existing.NickName));

                return uuidPreserved.And(ownerPreserved).And(nickNamePreserved);
            });
        }

        // ===================================================================
        // Property: Planet survey with generated HTML (no MaxReserve nodes)
        // produces correct results regardless of resource content
        // Validates: Requirements 3.1, 3.3
        // ===================================================================

        private static Gen<string> ResourceNameGen()
        {
            return Gen.Elements(
                "Post-Trans Metals", "Heavy Trans-Metals", "Complex Metallics",
                "Alkali Organics", "Noble Gases", "Lanthanides", "Halogens",
                "Strong Alkali Organics", "Heavy Noble Gases", "Superheavy Exotics");
        }

        private static Gen<string> PurityGen()
        {
            return Gen.Elements("High Purity", "Med Purity", "Low Purity");
        }

        private static Gen<string> AmountGen()
        {
            return from whole in Gen.Choose(1, 200)
                   from frac in Gen.Choose(0, 9)
                   select frac > 0 ? string.Format("{0}.{1}", whole, frac) : whole.ToString();
        }

        private static string BuildPlanetSurveyHtml(string planetName, string systemName,
            string surveyId, string scannedBy, List<Tuple<string, string, string>> resources)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<div class=\"SmallSlideOut_FormSection\"><span> </span>");
            sb.Append("<div class=\"SmallSlideOut_Form_Row_NameOfItem_Section\">");
            sb.AppendFormat("<div class=\"SmallSlideOut_Form_Row_Text_Bold\">{0}, {1} ({2})</div>",
                planetName, systemName, surveyId);
            sb.Append("<div class=\"SmallSlideOut_Form_Row_Description SmallSlideOut_Form_Row_Description_Small\">");
            sb.AppendFormat("A detailed survey report taken on 01JAN25-12:00p by {0}</div>", scannedBy);
            sb.Append("</div></div>");
            sb.Append("<div class=\"SmallSlideOut_FormSection\"><div class=\"SmallSlideOut_Form_Row\">");
            sb.Append("<div class=\"ScanDetailOutput\">");
            foreach (var res in resources)
            {
                sb.AppendFormat("<div class=\"div_block ui_text_lightgrey ScanDetailOutputResourceName\">{0} ({1})</div> ", res.Item1, res.Item2);
                sb.AppendFormat("<div class=\"div_block ui_text_blue_light ScanDetailOutputResourceDetail\">{0}/hour</div>", res.Item3);
            }

            sb.Append("</div></div></div>");
            return sb.ToString();
        }

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property PlanetSurveyHtml_NeverSetsAsteroidType()
        {
            // **Validates: Requirements 3.1**
            var gen = from resourceCount in Gen.Choose(1, 6)
                      from resources in Gen.ListOf(resourceCount,
                          from name in ResourceNameGen()
                          from purity in PurityGen()
                          from amount in AmountGen()
                          select Tuple.Create(name, purity, amount))
                      from planetName in NonEmptyStringGen()
                      from systemName in NonEmptyStringGen()
                      from surveyId in NonEmptyStringGen()
                      from scannedBy in NonEmptyStringGen()
                      select BuildPlanetSurveyHtml(planetName, systemName, surveyId, scannedBy, resources.ToList());

            return Prop.ForAll(gen.ToArbitrary(), html =>
            {
                var survey = new Survey();
                _parser.ProcessHtml(survey, html);

                return (survey.SurveyType == SurveyType.Planet)
                    .Label(string.Format("Planet survey HTML should not set SurveyType to Asteroid, got {0}", survey.SurveyType));
            });
        }

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property PlanetSurveyHtml_ResourcesParsedCorrectly()
        {
            // **Validates: Requirements 3.1, 3.3**
            var gen = from name in ResourceNameGen()
                      from purity in PurityGen()
                      from amount in AmountGen()
                      select new { Name = name, Purity = purity, Amount = amount };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var survey = new Survey();
                SurveyParser.ParseResource(survey, string.Format("{0} ({1})", data.Name, data.Purity), string.Format("{0}/hour", data.Amount));

                var hasResource = survey.Resources.ContainsKey(data.Name);
                if (!hasResource)
                    return false.Label(string.Format("Resource '{0}' not found in parsed survey", data.Name));

                var r = survey.Resources[data.Name];
                var amountMatch = (r.Amount == data.Amount)
                    .Label(string.Format("Amount: expected '{0}', got '{1}'", data.Amount, r.Amount));

                return amountMatch;
            });
        }
    }
}
