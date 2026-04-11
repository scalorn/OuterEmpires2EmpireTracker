using System;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Parsers;

namespace OE2EmpireTracker.Tests.Parsers
{
    [TestFixture]
    public class PlayerProfileParserPropertyTests
    {
        // Feature: player-profile-import, Property 1: Formatted number round-trip
        /// <summary>
        /// For any non-negative long, formatting it with commas (N0 format) and then
        /// parsing with ParseFormattedNumber should yield the original value.
        /// **Validates: Requirements 2.1, 3.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property FormattedNumber_RoundTrip()
        {
            var nonNegativeLongGen = Gen.Choose(0, int.MaxValue)
                .Select(x => (long)x)
                .Or(Gen.Choose(0, int.MaxValue).Two()
                    .Select(t => (long)t.Item1 * int.MaxValue + t.Item2)
                    .Where(x => x >= 0));

            return Prop.ForAll(
                nonNegativeLongGen.ToArbitrary(),
                value =>
                {
                    // Format with commas like "1,234,567"
                    string formatted = value.ToString("N0");
                    long parsed = PlayerProfileParser.ParseFormattedNumber(formatted);
                    return (parsed == value)
                        .Label($"Expected {value} but got {parsed} from \"{formatted}\"");
                });
        }
    }
}
