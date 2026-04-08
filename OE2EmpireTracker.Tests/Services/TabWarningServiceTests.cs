using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for TabWarningService.
    /// Feature: worker-tab-due-warning
    /// </summary>
    [TestFixture]
    public class TabWarningServiceTests
    {
        /// <summary>
        /// Feature: worker-tab-due-warning, Property 1: Structure warning level is determined by count thresholds
        /// **Validates: Requirements 7.1, 7.2, 8.1, 8.2, 9.1**
        ///
        /// For any non-negative integer structureCount (0–200),
        /// EvaluateStructureWarning returns Red if >= 66, Yellow if >= 60, None otherwise.
        /// </summary>
        [FsCheck.NUnit.Property]
        public Property StructureWarningLevel_IsDeterminedByCountThresholds()
        {
            return Prop.ForAll(
                Gen.Choose(0, 200).ToArbitrary(),
                count =>
                {
                    var result = TabWarningService.EvaluateStructureWarning(count);

                    TabWarningLevel expected;
                    if (count >= 66)
                        expected = TabWarningLevel.Red;
                    else if (count >= 60)
                        expected = TabWarningLevel.Yellow;
                    else
                        expected = TabWarningLevel.None;

                    return (result == expected)
                        .Label($"count={count}, expected={expected}, got={result}");
                });
        }
    }
}
