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

        [TestCase(1, "Refuelling")]
        [TestCase(2, "Transport Job")]
        [TestCase(3, "Market Sale")]
        [TestCase(4, "Broker Fee")]
        [TestCase(5, "Market Purchase")]
        [TestCase(6, "Buy Order Escrow")]
        [TestCase(7, "Ship Repair")]
        [TestCase(9, "Worker Wages")]
        [TestCase(10, "Colony Payout")]
        [TestCase(12, "Transfer")]
        [TestCase(13, "Insurance")]
        [TestCase(14, "Clone")]
        [TestCase(15, "Sales Tax")]
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
