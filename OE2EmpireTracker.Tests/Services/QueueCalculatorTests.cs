using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    using Blueprint = OE2EmpireTracker.Models.Blueprint;

    [TestFixture]
    public class QueueCalculatorTests
    {
        // --- ComputeManufactoryRuns ---

        [Test]
        public void ComputeManufactoryRuns_NullBlueprint_ReturnsMinusOne()
        {
            Assert.That(QueueCalculator.ComputeManufactoryRuns(null, 3600), Is.EqualTo(-1));
        }

        [Test]
        public void ComputeManufactoryRuns_NoMfgTime_ReturnsMinusOne()
        {
            var bp = new Blueprint("Test") { Properties = new PropertyBag() };
            Assert.That(QueueCalculator.ComputeManufactoryRuns(bp, 3600), Is.EqualTo(-1));
        }

        [Test]
        public void ComputeManufactoryRuns_ZeroTarget_ReturnsZero()
        {
            var bp = CreateBlueprintWithMfgTime("9h");
            Assert.That(QueueCalculator.ComputeManufactoryRuns(bp, 0), Is.EqualTo(0));
        }

        [Test]
        public void ComputeManufactoryRuns_NegativeTarget_ReturnsZero()
        {
            var bp = CreateBlueprintWithMfgTime("9h");
            Assert.That(QueueCalculator.ComputeManufactoryRuns(bp, -100), Is.EqualTo(0));
        }

        [Test]
        public void ComputeManufactoryRuns_ExactDivision_ReturnsExact()
        {
            // 9h = 32400s, target = 64800s (18h) -> 64800/32400 = 2 exactly
            var bp = CreateBlueprintWithMfgTime("9h");
            Assert.That(QueueCalculator.ComputeManufactoryRuns(bp, 64800), Is.EqualTo(2));
        }

        [Test]
        public void ComputeManufactoryRuns_NonExactDivision_ReturnsCeiling()
        {
            // 9h = 32400s, target = 216000s (2d 12h) -> ceiling(216000/32400) = 7
            var bp = CreateBlueprintWithMfgTime("9h");
            Assert.That(QueueCalculator.ComputeManufactoryRuns(bp, 216000), Is.EqualTo(7));
        }

        [Test]
        public void ComputeManufactoryRuns_TargetSmallerThanMfgTime_ReturnsOne()
        {
            // 9h = 32400s, target = 100s -> ceiling(100/32400) = 1
            var bp = CreateBlueprintWithMfgTime("9h");
            Assert.That(QueueCalculator.ComputeManufactoryRuns(bp, 100), Is.EqualTo(1));
        }

        [Test]
        public void ComputeManufactoryRuns_EmptyMfgTimeString_ReturnsMinusOne()
        {
            var bp = new Blueprint("Test") { Properties = new PropertyBag() };
            bp.Properties.SetProperty("Manufacture Run Time", string.Empty);
            Assert.That(QueueCalculator.ComputeManufactoryRuns(bp, 3600), Is.EqualTo(-1));
        }

        [Test]
        public void ComputeManufactoryRuns_ComplexTimeFormat_ComputesCorrectly()
        {
            // 1d 2h 30m = 86400 + 7200 + 1800 = 95400s
            // target = 190800s -> ceiling(190800/95400) = 2
            var bp = CreateBlueprintWithMfgTime("1d 2h 30m");
            Assert.That(QueueCalculator.ComputeManufactoryRuns(bp, 190800), Is.EqualTo(2));
        }

        // --- ComputeCommodityRuns ---

        [Test]
        public void ComputeCommodityRuns_ZeroTarget_ReturnsZero()
        {
            Assert.That(QueueCalculator.ComputeCommodityRuns(0), Is.EqualTo(0));
        }

        [Test]
        public void ComputeCommodityRuns_NegativeTarget_ReturnsZero()
        {
            Assert.That(QueueCalculator.ComputeCommodityRuns(-50), Is.EqualTo(0));
        }

        [Test]
        public void ComputeCommodityRuns_ExactDivision_ReturnsExact()
        {
            // Default CommodityCycleSeconds = 600, target = 3600 -> 3600/600 = 6
            Assert.That(QueueCalculator.ComputeCommodityRuns(3600), Is.EqualTo(6));
        }

        [Test]
        public void ComputeCommodityRuns_NonExactDivision_ReturnsCeiling()
        {
            // Default CommodityCycleSeconds = 600, target = 3601 -> ceiling(3601/600) = 7
            Assert.That(QueueCalculator.ComputeCommodityRuns(3601), Is.EqualTo(7));
        }

        [Test]
        public void ComputeCommodityRuns_DesignExample_Returns360()
        {
            // Design Flow 6: 216000s / 600s = 360 runs
            Assert.That(QueueCalculator.ComputeCommodityRuns(216000), Is.EqualTo(360));
        }

        // --- ManufactoryRunsToItems ---

        [Test]
        public void ManufactoryRunsToItems_NullBlueprint_DefaultsOnePerRun()
        {
            Assert.That(QueueCalculator.ManufactoryRunsToItems(null, 5), Is.EqualTo(5));
        }

        [Test]
        public void ManufactoryRunsToItems_NoAmountManufactured_DefaultsOnePerRun()
        {
            var bp = new Blueprint("Test") { Properties = new PropertyBag() };
            Assert.That(QueueCalculator.ManufactoryRunsToItems(bp, 7), Is.EqualTo(7));
        }

        [Test]
        public void ManufactoryRunsToItems_WithAmountManufactured_Multiplies()
        {
            var bp = new Blueprint("Munitions") { Properties = new PropertyBag() };
            bp.Properties.SetProperty("Amount Manufactured", 50m);
            Assert.That(QueueCalculator.ManufactoryRunsToItems(bp, 3), Is.EqualTo(150));
        }

        [Test]
        public void ManufactoryRunsToItems_ZeroRuns_ReturnsZero()
        {
            var bp = CreateBlueprintWithMfgTime("1h");
            Assert.That(QueueCalculator.ManufactoryRunsToItems(bp, 0), Is.EqualTo(0));
        }

        [Test]
        public void ManufactoryRunsToItems_NegativeRuns_ReturnsZero()
        {
            var bp = CreateBlueprintWithMfgTime("1h");
            Assert.That(QueueCalculator.ManufactoryRunsToItems(bp, -3), Is.EqualTo(0));
        }

        // --- CommodityRunsToItems ---

        [Test]
        public void CommodityRunsToItems_ZeroRuns_ReturnsZero()
        {
            Assert.That(QueueCalculator.CommodityRunsToItems(0), Is.EqualTo(0));
        }

        [Test]
        public void CommodityRunsToItems_NegativeRuns_ReturnsZero()
        {
            Assert.That(QueueCalculator.CommodityRunsToItems(-5), Is.EqualTo(0));
        }

        [Test]
        public void CommodityRunsToItems_DesignExample_Returns3600()
        {
            // Design Flow 6: 360 runs x 10 per cycle = 3600
            Assert.That(QueueCalculator.CommodityRunsToItems(360), Is.EqualTo(3600));
        }

        [Test]
        public void CommodityRunsToItems_SingleRun_ReturnsCommoditiesPerCycle()
        {
            // Default CommoditiesPerCycle = 10
            Assert.That(QueueCalculator.CommodityRunsToItems(1), Is.EqualTo(10));
        }

        // --- Helper ---

        private Blueprint CreateBlueprintWithMfgTime(string timeStr)
        {
            var bp = new Blueprint("TestBP") { Properties = new PropertyBag() };
            bp.Properties.SetProperty("Manufacture Run Time", timeStr);
            return bp;
        }
    }
}
