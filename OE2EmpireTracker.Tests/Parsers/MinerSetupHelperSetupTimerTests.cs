using System;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;

namespace OE2EmpireTracker.Tests.Parsers
{
    /// <summary>
    /// Unit tests for MinerSetupHelper.SetupTimer.
    /// Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5
    /// </summary>
    [TestFixture]
    public class MinerSetupHelperSetupTimerTests
    {
        /// <summary>
        /// Validates: Requirements 2.1, 2.3, 2.4
        /// When maxRate > 0 and no existing timer, SetupTimer creates a repeating timer
        /// aligned to the next hour boundary with interval = SecondsPerHour.
        /// </summary>
        [Test]
        public void SetupTimer_CreatesRepeatingTimer_WhenMaxRateGreaterThanZero()
        {
            // Arrange
            var structure = new ColonyStructure { UUID = "struct-1" };

            // Act
            MinerSetupHelper.SetupTimer(structure, 123.45m);

            // Assert
            Assert.That(structure.ProcessCompletionTime, Is.Not.Null);
            Assert.That(structure.ProcessCompletionTime.IsRepeating, Is.True);
            Assert.That(structure.ProcessCompletionTime.RepeatIntervalSeconds,
                Is.EqualTo(GameConstants.SecondsPerHour));
        }

        /// <summary>
        /// Validates: Requirements 2.4
        /// Timer should align to the next clock-hour boundary.
        /// TimeRemaining should be between 0 and SecondsPerHour (exclusive of SecondsPerHour).
        /// </summary>
        [Test]
        public void SetupTimer_AlignsToNextHourBoundary()
        {
            // Arrange
            var structure = new ColonyStructure { UUID = "struct-2" };

            // Act
            MinerSetupHelper.SetupTimer(structure, 50m);

            // Assert: time remaining should be between 1 and SecondsPerHour
            Assert.That(structure.ProcessCompletionTime, Is.Not.Null);
            long remaining = structure.ProcessCompletionTime.TimeRemaining;
            Assert.That(remaining, Is.GreaterThan(0));
            Assert.That(remaining, Is.LessThanOrEqualTo(GameConstants.SecondsPerHour));
        }

        /// <summary>
        /// Validates: Requirements 2.2
        /// When maxRate == 0, SetupTimer should not create a timer.
        /// </summary>
        [Test]
        public void SetupTimer_SkipsTimerCreation_WhenMaxRateIsZero()
        {
            // Arrange
            var structure = new ColonyStructure { UUID = "struct-3" };

            // Act
            MinerSetupHelper.SetupTimer(structure, 0m);

            // Assert
            Assert.That(structure.ProcessCompletionTime, Is.Null);
        }

        /// <summary>
        /// Validates: Requirements 2.2
        /// When maxRate is negative, SetupTimer should not create a timer.
        /// </summary>
        [Test]
        public void SetupTimer_SkipsTimerCreation_WhenMaxRateIsNegative()
        {
            // Arrange
            var structure = new ColonyStructure { UUID = "struct-4" };

            // Act
            MinerSetupHelper.SetupTimer(structure, -10m);

            // Assert
            Assert.That(structure.ProcessCompletionTime, Is.Null);
        }

        /// <summary>
        /// Validates: Requirements 2.5
        /// When structure already has an active repeating timer, SetupTimer preserves it.
        /// </summary>
        [Test]
        public void SetupTimer_PreservesExistingTimer_WhenActiveRepeatingTimerExists()
        {
            // Arrange: create a structure with an existing active repeating timer
            var structure = new ColonyStructure { UUID = "struct-5" };
            var existingTimer = new CountDownTime();
            existingTimer.StartRepeating(GameConstants.SecondsPerHour, 1800);
            structure.ProcessCompletionTime = existingTimer;

            DateTime originalEndTime = existingTimer.EndTime;

            // Act
            MinerSetupHelper.SetupTimer(structure, 200m);

            // Assert: timer should be the same object, not replaced
            Assert.That(structure.ProcessCompletionTime, Is.SameAs(existingTimer));
            Assert.That(structure.ProcessCompletionTime.EndTime, Is.EqualTo(originalEndTime));
        }

        /// <summary>
        /// Validates: Requirements 2.3
        /// Timer RepeatIntervalSeconds should equal GameConstants.SecondsPerHour (3600).
        /// </summary>
        [Test]
        public void SetupTimer_SetsRepeatInterval_ToSecondsPerHour()
        {
            // Arrange
            var structure = new ColonyStructure { UUID = "struct-6" };

            // Act
            MinerSetupHelper.SetupTimer(structure, 75m);

            // Assert
            Assert.That(structure.ProcessCompletionTime, Is.Not.Null);
            Assert.That(structure.ProcessCompletionTime.RepeatIntervalSeconds,
                Is.EqualTo(3600));
        }
    }
}
