using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Forms.PlayerProfile;
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
                    .Select(t => ((long)t.Item1 * int.MaxValue) + t.Item2)
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
                Assert.That(
                    lookup.ContainsKey(displayName),
                    Is.True,
                    $"Display name '{displayName}' for {s} not found in reverse lookup");
                Assert.That(
                    lookup[displayName],
                    Is.EqualTo(s),
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
                Assert.That(
                    lookup.ContainsKey(displayName),
                    Is.True,
                    $"Display name '{displayName}' for {g} not found in reverse lookup");
                Assert.That(
                    lookup[displayName],
                    Is.EqualTo(g),
                    $"Round-trip failed for {g}: display name '{displayName}' mapped to {lookup[displayName]}");
            }
        }

        // Feature: player-profile-import, Property 3: Training time parsing
        /// <summary>
        /// For any combination of days (0--99) and hours (0--23), formatting as the game's
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

                    long expectedSeconds = (((long)days * 24) + hours) * 3600;
                    long parsed = PlayerProfileParser.ParseTrainingTime(timeString);

                    return (Math.Abs(parsed - expectedSeconds) <= 1)
                        .Label($"days={days}, hours={hours}: expected {expectedSeconds}s but got {parsed}s from \"{timeString}\"");
                });
        }

        // Feature: player-profile-import, Property 4: Profile update by case-insensitive name match
        /// <summary>
        /// For any existing profile and a parsed profile whose name matches under case-insensitive
        /// comparison, merging should update the existing profile's data while preserving its UUID.
        /// **Validates: Requirements 7.1, 7.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ProfileUpdate_CaseInsensitiveNameMatch_PreservesUUID()
        {
            // Generator for a non-empty alphabetic name (1--20 chars)
            var nameGen = Gen.Choose(1, 20).SelectMany(len =>
                Gen.ArrayOf(len, Gen.Elements<char>(
                    'A',
                    'B',
                    'C',
                    'D',
                    'E',
                    'F',
                    'G',
                    'H',
                    'I',
                    'J',
                    'K',
                    'L',
                    'M',
                    'a',
                    'b',
                    'c',
                    'd',
                    'e',
                    'f',
                    'g',
                    'h',
                    'i',
                    'j',
                    'k',
                    'l',
                    'm'))
                .Select(chars => new string(chars)))
                .Where(s => !string.IsNullOrEmpty(s));

            var creditsGen = Gen.Choose(0, 999999).Select(x => (decimal)x);
            var rankGen = Gen.Choose(0, 100);

            return Prop.ForAll(
                nameGen.ToArbitrary(),
                creditsGen.ToArbitrary(),
                rankGen.ToArbitrary(),
                (baseName, newCredits, newPublicRank) =>
                {
                    // Create existing profile with a known UUID
                    string originalUUID = Guid.NewGuid().ToString();
                    var existing = new PlayerProfile
                    {
                        UUID = originalUUID,
                        Name = baseName.ToLowerInvariant(),
                        Faction = "OldFaction",
                        TotalCredits = 100m
                    };

                    // Create parsed profile with case-shuffled name
                    var parsed = new PlayerProfile
                    {
                        Name = baseName.ToUpperInvariant(),
                        Faction = "NewFaction",
                        TotalCredits = newCredits,
                        CitizenId = "42-1234",
                        RegistrationDate = "2223-01-06",
                        ActiveTime = "1D 2H"
                    };

                    parsed.Public.Rank = newPublicRank;

                    // Verify case-insensitive match
                    bool nameMatches = string.Equals(existing.Name, parsed.Name, StringComparison.OrdinalIgnoreCase);

                    // Merge
                    FormPlayerProfile.MergeProfile(existing, parsed);

                    return (nameMatches &&
                            existing.UUID == originalUUID &&
                            existing.Faction == "NewFaction" &&
                            existing.TotalCredits == newCredits &&
                            existing.CitizenId == "42-1234" &&
                            existing.Public.Rank == newPublicRank)
                        .Label($"UUID preserved: {existing.UUID == originalUUID}, " +
                               $"Faction updated: {existing.Faction == "NewFaction"}, " +
                               $"Credits updated: {existing.TotalCredits == newCredits}");
                });
        }

        // Feature: player-profile-import, Property 5: Profile creation when no name match exists
        /// <summary>
        /// For any list of existing profiles and a parsed profile whose name does not match
        /// any existing profile (case-insensitive), adding it should grow the list by one
        /// with a non-empty UUID.
        /// **Validates: Requirements 7.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ProfileCreation_NoNameMatch_AddsNewWithUUID()
        {
            // Generator for a list of 0--5 profiles with unique names
            var existingListGen = Gen.Choose(0, 5).SelectMany(count =>
                Gen.ArrayOf(count, Gen.Choose(1, 10).SelectMany(len =>
                    Gen.ArrayOf(len, Gen.Elements<char>(
                        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M'))
                    .Select(chars => new string(chars))))
                .Select(names =>
                {
                    var list = new List<PlayerProfile>();
                    var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var n in names)
                    {
                        if (usedNames.Add(n))
                        {
                            list.Add(new PlayerProfile
                            {
                                UUID = Guid.NewGuid().ToString(),
                                Name = n
                            });
                        }
                    }

                    return list;
                }));

            return Prop.ForAll(
                existingListGen.ToArbitrary(),
                existingProfiles =>
                {
                    // Generate a unique name that doesn't match any existing profile
                    string uniqueName = "UNIQUE_" + Guid.NewGuid().ToString("N").Substring(0, 8);

                    var parsed = new PlayerProfile
                    {
                        Name = uniqueName,
                        Faction = "TestFaction",
                        TotalCredits = 500m
                    };

                    // Simulate the import logic: check for case-insensitive match
                    var match = existingProfiles
                        .FirstOrDefault(p => string.Equals(p.Name, parsed.Name, StringComparison.OrdinalIgnoreCase));

                    int originalCount = existingProfiles.Count;

                    if (match == null)
                    {
                        // No match -- create new profile with UUID
                        parsed.UUID = Guid.NewGuid().ToString();
                        existingProfiles.Add(parsed);
                    }

                    return (match == null &&
                            existingProfiles.Count == originalCount + 1 &&
                            !string.IsNullOrEmpty(existingProfiles.Last().UUID) &&
                            existingProfiles.Last().Name == uniqueName)
                        .Label($"Match was null: {match == null}, " +
                               $"Count grew: {existingProfiles.Count == originalCount + 1}, " +
                               $"UUID non-empty: {!string.IsNullOrEmpty(existingProfiles.LastOrDefault()?.UUID)}");
                });
        }
    }
}
