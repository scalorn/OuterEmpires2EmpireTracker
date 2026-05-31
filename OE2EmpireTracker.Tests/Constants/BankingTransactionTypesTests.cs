using NUnit.Framework;
using OE2EmpireTracker.Constants;

namespace OE2EmpireTracker.Tests.Constants
{
    [TestFixture]
    public class BankingTransactionTypesTests
    {
        // -----------------------------------------------------------------------
        // GetLabel -- Known codes
        // -----------------------------------------------------------------------

        [TestCase(1, "Worker Wages")]
        [TestCase(2, "Market Sale")]
        [TestCase(3, "Market Purchase")]
        [TestCase(4, "Refueling")]
        [TestCase(5, "Colony Income")]
        [TestCase(6, "Transfer Received")]
        [TestCase(7, "Transfer Sent")]
        [TestCase(8, "Job Payment")]
        [TestCase(9, "Bounty")]
        [TestCase(10, "Insurance Payout")]
        public void GetLabel_KnownCode_ReturnsCorrectLabel(int code, string expectedLabel)
        {
            string result = BankingTransactionTypes.GetLabel(code, "some detail");
            Assert.That(result, Is.EqualTo(expectedLabel));
        }

        // -----------------------------------------------------------------------
        // GetLabel -- Unknown code with Detail
        // -----------------------------------------------------------------------

        [Test]
        public void GetLabel_UnknownCodeWithDetail_ReturnsDetailAndCode()
        {
            string result = BankingTransactionTypes.GetLabel(99, "Custom Transaction");
            Assert.That(result, Is.EqualTo("Custom Transaction (99)"));
        }

        // -----------------------------------------------------------------------
        // GetLabel -- Unknown code with null Detail
        // -----------------------------------------------------------------------

        [Test]
        public void GetLabel_UnknownCodeWithNullDetail_ReturnsUnknownAndCode()
        {
            string result = BankingTransactionTypes.GetLabel(42, null);
            Assert.That(result, Is.EqualTo("Unknown (42)"));
        }

        // -----------------------------------------------------------------------
        // GetLabel -- Unknown code with empty Detail
        // -----------------------------------------------------------------------

        [Test]
        public void GetLabel_UnknownCodeWithEmptyDetail_ReturnsUnknownAndCode()
        {
            string result = BankingTransactionTypes.GetLabel(55, string.Empty);
            Assert.That(result, Is.EqualTo("Unknown (55)"));
        }
    }
}
