using System;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Tests.Services.Migration
{
    /// <summary>
    /// Property tests for deterministic default survey UUID generation.
    /// **Validates: Requirements 6.1**
    /// </summary>
    [TestFixture]
    public class DefaultSurveyDeterministicUUIDPropertyTests
    {
        /// <summary>
        /// For any (ownerUUID, planetName, systemName) triple, GenerateDefaultSurvey
        /// shall produce the same UUID on every call.
        /// **Validates: Requirements 6.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property SameInputsProduceSameDefaultSurveyUUID()
        {
            return Prop.ForAll(ColonyKeyGen().ToArbitrary(), key =>
            {
                var uuid1 = DeterministicUUID.GenerateDefaultSurvey(key.Item1, key.Item2, key.Item3);
                var uuid2 = DeterministicUUID.GenerateDefaultSurvey(key.Item1, key.Item2, key.Item3);

                return (uuid1 == uuid2)
                    .Label($"Expected same UUID for same inputs, got '{uuid1}' and '{uuid2}'");
            });
        }

        /// <summary>
        /// Two different (ownerUUID, planetName, systemName) triples shall produce different UUIDs.
        /// **Validates: Requirements 6.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property DifferentInputsProduceDifferentDefaultSurveyUUIDs()
        {
            var gen = from key1 in ColonyKeyGen()
                      from key2 in ColonyKeyGen()
                      where key1.Item1 != key2.Item1
                         || key1.Item2 != key2.Item2
                         || key1.Item3 != key2.Item3
                      select new { Key1 = key1, Key2 = key2 };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var uuid1 = DeterministicUUID.GenerateDefaultSurvey(
                    data.Key1.Item1, data.Key1.Item2, data.Key1.Item3);
                var uuid2 = DeterministicUUID.GenerateDefaultSurvey(
                    data.Key2.Item1, data.Key2.Item2, data.Key2.Item3);

                return (uuid1 != uuid2)
                    .Label($"Expected different UUIDs for different inputs, both got '{uuid1}'");
            });
        }

        /// <summary>
        /// GenerateDefaultSurvey and Generate (colony) with the same inputs shall
        /// produce different UUIDs, confirming namespace isolation.
        /// **Validates: Requirements 6.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property DefaultSurveyUUIDDiffersFromColonyUUID()
        {
            return Prop.ForAll(ColonyKeyGen().ToArbitrary(), key =>
            {
                var defaultSurveyUuid = DeterministicUUID.GenerateDefaultSurvey(
                    key.Item1, key.Item2, key.Item3);
                var colonyUuid = DeterministicUUID.Generate(
                    key.Item1, key.Item2, key.Item3);

                return (defaultSurveyUuid != colonyUuid)
                    .Label($"Default survey UUID '{defaultSurveyUuid}' should differ from colony UUID '{colonyUuid}'");
            });
        }

        /// <summary>
        /// Generated default survey UUIDs should be valid GUID strings.
        /// **Validates: Requirements 6.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property GeneratedDefaultSurveyUUIDIsValidGuid()
        {
            return Prop.ForAll(ColonyKeyGen().ToArbitrary(), key =>
            {
                var uuidStr = DeterministicUUID.GenerateDefaultSurvey(
                    key.Item1, key.Item2, key.Item3);
                Guid parsed;
                var canParse = Guid.TryParse(uuidStr, out parsed);

                return canParse.Label($"UUID '{uuidStr}' is not a valid GUID");
            });
        }

        private static Gen<string> SafeStringGen()
        {
            return Arb.Default.NonEmptyString().Generator.Select(s => s.Get);
        }

        /// <summary>
        /// Generates a tuple of colony identity fields: (ownerUUID, planetName, systemName)
        /// </summary>
        private static Gen<Tuple<string, string, string>> ColonyKeyGen()
        {
            return from ownerUUID in SafeStringGen()
                   from planetName in SafeStringGen()
                   from systemName in SafeStringGen()
                   select Tuple.Create(ownerUUID, planetName, systemName);
        }
    }
}
