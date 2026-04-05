using NUnit.Framework;
using OE2EmpireTracker.Baseline;
using System;

namespace OE2EmpireTracker.Tests.Baseline
{
    [TestFixture]
    public class BuildTimeCalculatorTests
    {
        // Feature: colony-daily-build, Property 1: Build time calculation
        // **Validates: Requirements 5.1, 5.2**

        [Test]
        public void Property1_BuildTimeCalculation_AllSkillLevels()
        {
            for (int level = 0; level <= 50; level++)
            {
                long expected = Math.Max(1, (long)(86400.0 * (1.0 - level * 0.02)));
                long actual = BuildTimeCalculator.Calculate(level);
                Assert.AreEqual(expected, actual,
                    $"BuildTimeCalculator.Calculate({level}) should return {expected} but got {actual}");
            }
        }

        [Test]
        public void Calculate_Level0_Returns86400()
        {
            Assert.AreEqual(86400, BuildTimeCalculator.Calculate(0));
        }

        [Test]
        public void Calculate_Level50_Returns1()
        {
            Assert.AreEqual(1, BuildTimeCalculator.Calculate(50));
        }

        [Test]
        public void Calculate_Level25_Returns43200()
        {
            Assert.AreEqual(43200, BuildTimeCalculator.Calculate(25));
        }

        [Test]
        public void Calculate_ResultAlwaysAtLeast1()
        {
            for (int level = 0; level <= 100; level++)
            {
                long result = BuildTimeCalculator.Calculate(level);
                Assert.GreaterOrEqual(result, 1,
                    $"Calculate({level}) returned {result}, expected >= 1");
            }
        }
    }
}
