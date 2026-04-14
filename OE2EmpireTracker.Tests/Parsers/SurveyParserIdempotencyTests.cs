using NUnit.Framework;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Models;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace OE2EmpireTracker.Tests.Parsers
{
    [TestFixture]
    public class SurveyParserIdempotencyTests
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
            return OE2EmpireTracker.Forms.Blueprint.BlueprintScanner
                .ExtractHtmlFragmentFromClipboardData(clipboardData);
        }

        // -------------------------------------------------------------------
        // ZehVazoran idempotency tests (Requirements 1.1, 1.2, 1.3)
        // -------------------------------------------------------------------

        [Test]
        public void ZehVazoran_ParseTwice_ResourceCountUnchanged()
        {
            string clipboardData = LoadTestData("ZehVazoranIIM2.html");
            string html = ExtractFragment(clipboardData);
            var survey = new Survey();

            _parser.ProcessHtml(survey, html);
            int countAfterFirst = survey.Resources.Count;

            _parser.ProcessHtml(survey, html);
            Assert.That(survey.Resources.Count, Is.EqualTo(countAfterFirst));
        }

        [Test]
        public void ZehVazoran_ParseThreeTimes_ResourceCountUnchanged()
        {
            string clipboardData = LoadTestData("ZehVazoranIIM2.html");
            string html = ExtractFragment(clipboardData);
            var survey = new Survey();

            _parser.ProcessHtml(survey, html);
            int countAfterFirst = survey.Resources.Count;

            _parser.ProcessHtml(survey, html);
            _parser.ProcessHtml(survey, html);
            Assert.That(survey.Resources.Count, Is.EqualTo(countAfterFirst));
        }

        [Test]
        public void ZehVazoran_ParseTwice_ResourceValuesPreserved()
        {
            string clipboardData = LoadTestData("ZehVazoranIIM2.html");
            string html = ExtractFragment(clipboardData);
            var survey = new Survey();

            _parser.ProcessHtml(survey, html);

            // Snapshot values after first parse
            var snapshot = survey.Resources.Values
                .Select(r => new { r.Resource, r.Purity, r.Amount })
                .OrderBy(r => r.Resource)
                .ToList();

            _parser.ProcessHtml(survey, html);

            // Verify values unchanged after second parse
            var afterSecond = survey.Resources.Values
                .Select(r => new { r.Resource, r.Purity, r.Amount })
                .OrderBy(r => r.Resource)
                .ToList();

            Assert.That(afterSecond.Count, Is.EqualTo(snapshot.Count));
            for (int i = 0; i < snapshot.Count; i++)
            {
                Assert.That(afterSecond[i].Resource, Is.EqualTo(snapshot[i].Resource),
                    $"Resource name mismatch at index {i}");
                Assert.That(afterSecond[i].Purity, Is.EqualTo(snapshot[i].Purity),
                    $"Purity mismatch for {snapshot[i].Resource}");
                Assert.That(afterSecond[i].Amount, Is.EqualTo(snapshot[i].Amount),
                    $"Amount mismatch for {snapshot[i].Resource}");
            }
        }

        // -------------------------------------------------------------------
        // AllSurveyFiles sweep property tests (Requirements 1.1--1.4)
        // -------------------------------------------------------------------

        private static readonly string[] SurveyFiles = new[]
        {
            "ZehVazoranIIM2.html",
            "QuogarV2249II.html"
        };

        [Test]
        public void AllSurveyFiles_ParseTwice_ResourceCountUnchanged()
        {
            // Feature: parser-idempotency-tests, Property 1: Survey resource count idempotency
            // Validates: Requirements 1.1, 1.2, 1.4
            foreach (string filename in SurveyFiles)
            {
                string clipboardData = LoadTestData(filename);
                string html = ExtractFragment(clipboardData);
                var survey = new Survey();

                _parser.ProcessHtml(survey, html);
                int countAfterFirst = survey.Resources.Count;

                Assert.That(countAfterFirst, Is.GreaterThan(0),
                    $"{filename} should produce at least one resource");

                _parser.ProcessHtml(survey, html);
                Assert.That(survey.Resources.Count, Is.EqualTo(countAfterFirst),
                    $"Resource count changed after second parse of {filename}");
            }
        }

        [Test]
        public void AllSurveyFiles_ParseTwice_ResourceValuesPreserved()
        {
            // Feature: parser-idempotency-tests, Property 2: Survey resource values stability
            // Validates: Requirements 1.3
            foreach (string filename in SurveyFiles)
            {
                string clipboardData = LoadTestData(filename);
                string html = ExtractFragment(clipboardData);
                var survey = new Survey();

                _parser.ProcessHtml(survey, html);

                // Snapshot values after first parse
                var snapshot = survey.Resources.Values
                    .Select(r => new { r.Resource, r.Purity, r.Amount })
                    .OrderBy(r => r.Resource)
                    .ToList();

                _parser.ProcessHtml(survey, html);

                // Verify values unchanged after second parse
                var afterSecond = survey.Resources.Values
                    .Select(r => new { r.Resource, r.Purity, r.Amount })
                    .OrderBy(r => r.Resource)
                    .ToList();

                Assert.That(afterSecond.Count, Is.EqualTo(snapshot.Count),
                    $"Resource count changed after second parse of {filename}");

                for (int i = 0; i < snapshot.Count; i++)
                {
                    Assert.That(afterSecond[i].Resource, Is.EqualTo(snapshot[i].Resource),
                        $"Resource name mismatch in {filename} at index {i}");
                    Assert.That(afterSecond[i].Purity, Is.EqualTo(snapshot[i].Purity),
                        $"Purity mismatch in {filename} for {snapshot[i].Resource}");
                    Assert.That(afterSecond[i].Amount, Is.EqualTo(snapshot[i].Amount),
                        $"Amount mismatch in {filename} for {snapshot[i].Resource}");
                }
            }
        }
    }
}
