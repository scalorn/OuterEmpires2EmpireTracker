using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Models
{
    [TestFixture]
    public class ThresholdPreferencesTests
    {
        #region Default Value Tests (Requirements 1.1–1.9)

        [Test]
        public void Default_StructureCountYellow_Is60()
        {
            var prefs = new ThresholdPreferences();
            Assert.That(prefs.StructureCountYellow, Is.EqualTo(60));
        }

        [Test]
        public void Default_StructureCountRed_Is66()
        {
            var prefs = new ThresholdPreferences();
            Assert.That(prefs.StructureCountRed, Is.EqualTo(66));
        }

        [Test]
        public void Default_WorkerRequestYellowSeconds_Is172800()
        {
            var prefs = new ThresholdPreferences();
            Assert.That(prefs.WorkerRequestYellowSeconds, Is.EqualTo(172800L));
        }

        [Test]
        public void Default_WorkerRequestRedSeconds_Is86400()
        {
            var prefs = new ThresholdPreferences();
            Assert.That(prefs.WorkerRequestRedSeconds, Is.EqualTo(86400L));
        }

        [Test]
        public void Default_ColonyImportStalenessYellowSeconds_Is432000()
        {
            var prefs = new ThresholdPreferences();
            Assert.That(prefs.ColonyImportStalenessYellowSeconds, Is.EqualTo(432000L));
        }

        [Test]
        public void Default_ColonyImportStalenessRedSeconds_Is518400()
        {
            var prefs = new ThresholdPreferences();
            Assert.That(prefs.ColonyImportStalenessRedSeconds, Is.EqualTo(518400L));
        }

        [Test]
        public void Default_BackgroundProcessingIntervalSeconds_Is60()
        {
            var prefs = new ThresholdPreferences();
            Assert.That(prefs.BackgroundProcessingIntervalSeconds, Is.EqualTo(60L));
        }

        [Test]
        public void Default_AdminRefreshIntervalSeconds_Is60()
        {
            var prefs = new ThresholdPreferences();
            Assert.That(prefs.AdminRefreshIntervalSeconds, Is.EqualTo(60L));
        }

        [Test]
        public void Default_CountdownRefreshRateSeconds_Is1()
        {
            var prefs = new ThresholdPreferences();
            Assert.That(prefs.CountdownRefreshRateSeconds, Is.EqualTo(1L));
        }

        #endregion

        #region Validation Tests (Requirements 7.1–7.6)

        [Test]
        public void Validate_DefaultPreferences_ReturnsTrue()
        {
            var prefs = new ThresholdPreferences();
            var result = ThresholdPreferences.Validate(prefs, out string error);
            Assert.That(result, Is.True);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void Validate_CountdownRefreshRateZero_Rejected()
        {
            var prefs = new ThresholdPreferences { CountdownRefreshRateSeconds = 0 };
            var result = ThresholdPreferences.Validate(prefs, out string error);
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Null);
        }

        [Test]
        public void Validate_StructureCountYellowEqualsRed_Rejected()
        {
            var prefs = new ThresholdPreferences
            {
                StructureCountYellow = 60,
                StructureCountRed = 60
            };
            var result = ThresholdPreferences.Validate(prefs, out string error);
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Null);
        }

        [Test]
        public void Validate_WorkerRequestYellowEqualsRed_Rejected()
        {
            var prefs = new ThresholdPreferences
            {
                WorkerRequestYellowSeconds = 86400,
                WorkerRequestRedSeconds = 86400
            };
            var result = ThresholdPreferences.Validate(prefs, out string error);
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Null);
        }

        [Test]
        public void Validate_NegativeStructureCountYellow_Rejected()
        {
            var prefs = new ThresholdPreferences { StructureCountYellow = -1 };
            var result = ThresholdPreferences.Validate(prefs, out string error);
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Null);
        }

        [Test]
        public void Validate_NegativeWorkerRequestRedSeconds_Rejected()
        {
            var prefs = new ThresholdPreferences { WorkerRequestRedSeconds = -5 };
            var result = ThresholdPreferences.Validate(prefs, out string error);
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Null);
        }

        [Test]
        public void Validate_NegativeBackgroundProcessingInterval_Rejected()
        {
            var prefs = new ThresholdPreferences { BackgroundProcessingIntervalSeconds = -10 };
            var result = ThresholdPreferences.Validate(prefs, out string error);
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Null);
        }

        [Test]
        public void Validate_NegativeAdminRefreshInterval_Rejected()
        {
            var prefs = new ThresholdPreferences { AdminRefreshIntervalSeconds = -1 };
            var result = ThresholdPreferences.Validate(prefs, out string error);
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Null);
        }

        [Test]
        public void Validate_NegativeColonyImportStalenessYellow_Rejected()
        {
            var prefs = new ThresholdPreferences { ColonyImportStalenessYellowSeconds = -100 };
            var result = ThresholdPreferences.Validate(prefs, out string error);
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Null);
        }

        #endregion

        #region Property Test: Validation Correctness (Property 6, Requirements 7.1–7.6)

        /// <summary>
        /// Generates a random ThresholdPreferences with a mix of valid and invalid values.
        /// Values are drawn from a range that includes negatives, zero, and positives to
        /// exercise all validation branches.
        /// </summary>
        private static Gen<ThresholdPreferences> ThresholdPreferencesGen()
        {
            return from structYellow in Gen.Choose(-5, 100)
                   from structRed in Gen.Choose(-5, 100)
                   from workerYellow in Gen.Choose(-100, 500000).Select(i => (long)i)
                   from workerRed in Gen.Choose(-100, 500000).Select(i => (long)i)
                   from importYellow in Gen.Choose(-100, 600000).Select(i => (long)i)
                   from importRed in Gen.Choose(-100, 600000).Select(i => (long)i)
                   from bgInterval in Gen.Choose(-10, 120).Select(i => (long)i)
                   from adminInterval in Gen.Choose(-10, 120).Select(i => (long)i)
                   from countdownRate in Gen.Choose(-2, 10).Select(i => (long)i)
                   select new ThresholdPreferences
                   {
                       StructureCountYellow = structYellow,
                       StructureCountRed = structRed,
                       WorkerRequestYellowSeconds = workerYellow,
                       WorkerRequestRedSeconds = workerRed,
                       ColonyImportStalenessYellowSeconds = importYellow,
                       ColonyImportStalenessRedSeconds = importRed,
                       BackgroundProcessingIntervalSeconds = bgInterval,
                       AdminRefreshIntervalSeconds = adminInterval,
                       CountdownRefreshRateSeconds = countdownRate
                   };
        }

        /// <summary>
        /// Computes the expected validation result by checking all rules manually:
        /// - All nine values must be positive (> 0)
        /// - StructureCountYellow &lt; StructureCountRed
        /// - WorkerRequestYellowSeconds &gt; WorkerRequestRedSeconds
        /// - ColonyImportStalenessYellowSeconds &lt; ColonyImportStalenessRedSeconds
        /// - CountdownRefreshRateSeconds &gt;= 1
        /// </summary>
        private static bool ExpectedValidation(ThresholdPreferences p)
        {
            bool allPositive =
                p.StructureCountYellow > 0 &&
                p.StructureCountRed > 0 &&
                p.WorkerRequestYellowSeconds > 0 &&
                p.WorkerRequestRedSeconds > 0 &&
                p.ColonyImportStalenessYellowSeconds > 0 &&
                p.ColonyImportStalenessRedSeconds > 0 &&
                p.BackgroundProcessingIntervalSeconds > 0 &&
                p.AdminRefreshIntervalSeconds > 0 &&
                p.CountdownRefreshRateSeconds > 0;

            bool orderingCorrect =
                p.StructureCountYellow < p.StructureCountRed &&
                p.WorkerRequestYellowSeconds > p.WorkerRequestRedSeconds &&
                p.ColonyImportStalenessYellowSeconds < p.ColonyImportStalenessRedSeconds;

            bool countdownMinimum = p.CountdownRefreshRateSeconds >= 1;

            return allPositive && orderingCorrect && countdownMinimum;
        }

        /// <summary>
        /// Feature: preferences-form, Property 6: Preferences validation accepts valid and rejects invalid
        ///
        /// For any ThresholdPreferences instance, the validation logic shall accept the configuration
        /// if and only if: all nine values are positive, StructureCountYellow &lt; StructureCountRed,
        /// WorkerRequestYellowSeconds &gt; WorkerRequestRedSeconds,
        /// ColonyImportStalenessYellowSeconds &lt; ColonyImportStalenessRedSeconds,
        /// and CountdownRefreshRateSeconds &gt;= 1.
        ///
        /// **Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5, 7.6**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property PreferencesValidationCorrectness()
        {
            return Prop.ForAll(ThresholdPreferencesGen().ToArbitrary(), prefs =>
            {
                bool expected = ExpectedValidation(prefs);
                bool actual = ThresholdPreferences.Validate(prefs, out string error);

                bool errorConsistent = actual ? (error == null) : (error != null);

                return (actual == expected && errorConsistent)
                    .Label($"expected={expected}, actual={actual}, error={error ?? "null"}, " +
                           $"SY={prefs.StructureCountYellow}, SR={prefs.StructureCountRed}, " +
                           $"WY={prefs.WorkerRequestYellowSeconds}, WR={prefs.WorkerRequestRedSeconds}, " +
                           $"IY={prefs.ColonyImportStalenessYellowSeconds}, IR={prefs.ColonyImportStalenessRedSeconds}, " +
                           $"BG={prefs.BackgroundProcessingIntervalSeconds}, AR={prefs.AdminRefreshIntervalSeconds}, " +
                           $"CR={prefs.CountdownRefreshRateSeconds}");
            });
        }

        #endregion
    }
}
