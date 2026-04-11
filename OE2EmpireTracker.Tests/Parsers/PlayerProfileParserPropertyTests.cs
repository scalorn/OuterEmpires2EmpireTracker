using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
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

        // Feature: player-profile-import, Property 2: Enum display name round-trip
        /// <summary>
        /// For all SkillName and SkillGroupName values, converting to display name via
        /// ToDisplayName() and looking up in a reverse dictionary should yield the original enum value.
        /// **Validates: Requirements 5.1, 5.4, 6.4**
        /// </summary>
        [Test]
        public void EnumDisplayName_RoundTrip_SkillName()
        {
            var lookup = new Dictionary<string, SkillName>();
            foreach (SkillName s in Enum.GetValues(typeof(SkillName)))
            {
                lookup[s.ToDisplayName()] = s;
            }

            foreach (SkillName s in Enum.GetValues(typeof(SkillName)))
            {
                string displayName = s.ToDisplayName();
                Assert.That(lookup.ContainsKey(displayName), Is.True,
                    $"Display name '{displayName}' for {s} not found in reverse lookup");
                Assert.That(lookup[displayName], Is.EqualTo(s),
                    $"Round-trip failed for {s}: display name '{displayName}' mapped to {lookup[displayName]}");
            }
        }

        // Feature: player-profile-import, Property 2: Enum display name round-trip
        /// <summary>
        /// For all SkillGroupName values, converting to display name via
        /// ToDisplayName() and looking up in a reverse dictionary should yield the original enum value.
        /// **Validates: Requirements 5.1, 5.4, 6.4**
        /// </summary>
        [Test]
        public void EnumDisplayName_RoundTrip_SkillGroupName()
        {
            var lookup = new Dictionary<string, SkillGroupName>();
            foreach (SkillGroupName g in Enum.GetValues(typeof(SkillGroupName)))
            {
                lookup[g.ToDisplayName()] = g;
            }

            foreach (SkillGroupName g in Enum.GetValues(typeof(SkillGroupName)))
            {
                string displayName = g.ToDisplayName();
                Assert.That(lookup.ContainsKey(displayName), Is.True,
                    $"Display name '{displayName}' for {g} not found in reverse lookup");
                Assert.That(lookup[displayName], Is.EqualTo(g),
                    $"Round-trip failed for {g}: display name '{displayName}' mapped to {lookup[displayName]}");
            }
        }

        // Feature: player-profile-import, Property 3: Training time parsing
        /// <summary>
        /// For any combination of days (0–99) and hours (0–23), formatting as the game's
        /// training time string and parsing should produce the correct total seconds.
        /// **Validates: Requirements 6.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property TrainingTimeParsing_RoundTrip()
        {
            var daysGen = Gen.Choose(0, 99);
            var hoursGen = Gen.Choose(0, 23);

            return Prop.ForAll(
                daysGen.ToArbitrary(),
                hoursGen.ToArbitrary(),
                (days, hours) =>
                {
                    // Format as game string
                    string timeString;
                    if (days > 0 && hours > 0)
                        timeString = $"{days} days, {hours} hours";
                    else if (days > 0)
                        timeString = $"{days} days";
                    else if (hours > 0)
                        timeString = $"{hours} hours";
                    else
                        timeString = "0 hours";

                    long expectedSeconds = ((long)days * 24 + hours) * 3600;
                    long parsed = PlayerProfileParser.ParseTrainingTime(timeString);

                    return (Math.Abs(parsed - expectedSeconds) <= 1)
                        .Label($"days={days}, hours={hours}: expected {expectedSeconds}s but got {parsed}s from \"{timeString}\"");
                });
        }
    }
}
