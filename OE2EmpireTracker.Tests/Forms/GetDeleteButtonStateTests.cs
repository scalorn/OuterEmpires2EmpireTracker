using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Forms
{
    [TestFixture]
    public class GetDeleteButtonStateTests
    {
        // Requirement 2.3 -- zero-count report returns enabled with "Delete" text
        [Test]
        public void ZeroCountReport_ReturnsEnabled_Delete()
        {
            var report = ReferenceReport.Empty;

            var (enabled, text) = FormBlueprintV2.GetDeleteButtonState(report);

            Assert.That(enabled, Is.True);
            Assert.That(text, Is.EqualTo("Delete"));
        }

        // Requirement 2.2 -- positive-count report returns disabled with "In Use (N)" text
        [Test]
        public void PositiveCountReport_ReturnsDisabled_InUseWithCount()
        {
            var report = new ReferenceReport(1, 0, 2, 0, 0);

            var (enabled, text) = FormBlueprintV2.GetDeleteButtonState(report);

            Assert.That(enabled, Is.False);
            Assert.That(text, Is.EqualTo("In Use (3)"));
        }

        // Requirement 2.4 -- null report (no blueprint selected) returns disabled with "Delete" text
        [Test]
        public void NullReport_ReturnsDisabled_Delete()
        {
            var (enabled, text) = FormBlueprintV2.GetDeleteButtonState(null);

            Assert.That(enabled, Is.False);
            Assert.That(text, Is.EqualTo("Delete"));
        }
    }
}
