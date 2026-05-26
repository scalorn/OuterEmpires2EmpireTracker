using System.Linq;
using System.Threading;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Forms.PlayerProfile;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.Forms
{
    /// <summary>
    /// Feature: profile-form-upgrade, Property 5: PlayerSkillBlock height calculation
    ///
    /// For any LocalSkillData instance, the PlayerSkillBlock height SHALL be 42 pixels
    /// when any of (EffectDescription is non-empty, AmountPerLevel > 0,
    /// TrainingPercentageComplete > 0, RemainingMinutes > 0) is true, and 24 pixels
    /// when all are false/zero/empty.
    ///
    /// **Validates: Requirements 3.1, 3.2, 3.6**
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class PlayerSkillBlockPropertyTests
    {
        [SetUp]
        public void SetUp()
        {
            PreferencesStore.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            PreferencesStore.Reset();
        }

        /// <summary>
        /// Feature: profile-form-upgrade, Property 5: PlayerSkillBlock height calculation
        ///
        /// For any LocalSkillData where at least one metadata field is non-zero/non-empty,
        /// the control height SHALL be 42 pixels.
        /// For any LocalSkillData where all metadata fields are zero/empty,
        /// the control height SHALL be 24 pixels.
        ///
        /// **Validates: Requirements 3.1, 3.2, 3.6**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Height_Is42_WhenAnyMetadataPresent_And24_WhenNone()
        {
            var gen = from effectDesc in Gen.OneOf(
                          Gen.Constant(string.Empty),
                          Gen.Elements("Increases mining speed", "Reduces fuel cost", "Boosts refining"))
                      from amountPerLevel in Gen.Choose(0, 50)
                      from trainingPct in Gen.Choose(0, 100)
                      from remainingMin in Gen.Choose(0, 5000)
                      select new LocalSkillData
                      {
                          Level = 1,
                          TrainingStarted = false,
                          EffectDescription = effectDesc,
                          AmountPerLevel = amountPerLevel,
                          TrainingPercentageComplete = trainingPct,
                          RemainingMinutes = remainingMin,
                      };

            return Prop.ForAll(gen.ToArbitrary(), skillData =>
            {
                using (var block = new PlayerSkillBlock())
                {
                    block.SkillData = skillData;

                    bool hasMetadata = !string.IsNullOrEmpty(skillData.EffectDescription)
                        || skillData.AmountPerLevel > 0
                        || skillData.TrainingPercentageComplete > 0
                        || skillData.RemainingMinutes > 0;

                    int expectedHeight = hasMetadata ? 42 : 24;

                    return (block.Height == expectedHeight)
                        .Label($"Expected height {expectedHeight} but got {block.Height} " +
                               $"(Effect='{skillData.EffectDescription}', " +
                               $"AmountPerLevel={skillData.AmountPerLevel}, " +
                               $"TrainingPct={skillData.TrainingPercentageComplete}, " +
                               $"RemainingMin={skillData.RemainingMinutes})");
                }
            });
        }
    }
}
