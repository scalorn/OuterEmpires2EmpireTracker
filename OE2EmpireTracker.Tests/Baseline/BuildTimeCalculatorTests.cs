using NUnit.Framework;
using OE2EmpireTracker.Services;
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
                Assert.That(actual, Is.EqualTo(expected),
                    $"BuildTimeCalculator.Calculate({level}) should return {expected} but got {actual}");
            }
        }

        [Test]
        public void Calculate_Level0_Returns86400()
        {
            Assert.That(BuildTimeCalculator.Calculate(0), Is.EqualTo(86400));
        }

        [Test]
        public void Calculate_Level50_Returns1()
        {
            Assert.That(BuildTimeCalculator.Calculate(50), Is.EqualTo(1));
        }

        [Test]
        public void Calculate_Level25_Returns43200()
        {
            Assert.That(BuildTimeCalculator.Calculate(25), Is.EqualTo(43200));
        }

        [Test]
        public void Calculate_ResultAlwaysAtLeast1()
        {
            for (int level = 0; level <= 100; level++)
            {
                long result = BuildTimeCalculator.Calculate(level);
                Assert.That(result, Is.GreaterThanOrEqualTo(1),
                    $"Calculate({level}) returned {result}, expected >= 1");
            }
        }
    }
}
