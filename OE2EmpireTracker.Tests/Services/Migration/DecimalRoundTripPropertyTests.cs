using System;
using FsCheck;
using FsCheck.NUnit;
using Newtonsoft.Json;
using NUnit.Framework;

namespace OE2EmpireTracker.Tests.Services.Migration
{
    /// <summary>
    /// Property tests for decimal JSON round-trip fidelity.
    /// **Validates: Requirements 13.6, 13.9**
    /// </summary>
    [TestFixture]
    public class DecimalRoundTripPropertyTests
    {
        /// <summary>
        /// Generates decimal values representative of game data: volumes, rates,
        /// percentages, and power/habitation values. Range covers 0 to 999999.99
        /// with up to 4 decimal places.
        /// </summary>
        private static Gen<decimal> GameDecimalGen()
        {
            // Generate an integer mantissa and a scale (0--4 decimal places)
            return from mantissa in Gen.Choose(-99999999, 99999999)
                   from scale in Gen.Choose(0, 4)
                   let divisor = (decimal)Math.Pow(10, scale)
                   select mantissa / divisor;
        }

        /// <summary>
        /// For any decimal value serialized to JSON with Newtonsoft.Json and
        /// deserialized back, the value shall be exactly equal (no floating-point drift).
        /// **Validates: Requirements 13.6, 13.9**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property DecimalRoundTrip_ExactEquality()
        {
            return Prop.ForAll(GameDecimalGen().ToArbitrary(), original =>
            {
                var wrapper = new DecimalWrapper { Value = original };
                string json = JsonConvert.SerializeObject(wrapper);
                var restored = JsonConvert.DeserializeObject<DecimalWrapper>(json);

                return (original == restored.Value)
                    .Label($"Round-trip failed: original={original}, restored={restored.Value}, json={json}");
            });
        }

        private class DecimalWrapper
        {
            public decimal Value { get; set; }
        }
    }
}
