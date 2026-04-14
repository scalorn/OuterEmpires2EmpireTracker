using System;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Tests.Services.Migration
{
    /// <summary>
    /// Property tests for deterministic colony UUID generation.
    /// **Validates: Requirements 2.3**
    /// </summary>
    [TestFixture]
    public class ColonyDeterministicUUIDPropertyTests
    {
        #region Generators

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

        #endregion

        #region Property 1: Deterministic colony UUID round-trip -- same inputs produce same UUID

        /// <summary>
        /// For any (ownerUUID, planetName, systemName) triple, Generate shall produce
        /// the same UUID on every call.
        /// **Validates: Requirements 2.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property SameColonyInputsProduceSameUUID()
        {
            return Prop.ForAll(ColonyKeyGen().ToArbitrary(), key =>
            {
                var uuid1 = DeterministicUUID.Generate(key.Item1, key.Item2, key.Item3);
                var uuid2 = DeterministicUUID.Generate(key.Item1, key.Item2, key.Item3);

                return (uuid1 == uuid2)
                    .Label($"Expected same UUID for same inputs, got '{uuid1}' and '{uuid2}'");
            });
        }

        #endregion

        #region Property 1 (cont): Different inputs produce different UUIDs

        /// <summary>
        /// Two different (ownerUUID, planetName, systemName) triples shall produce different UUIDs.
        /// **Validates: Requirements 2.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property DifferentColonyInputsProduceDifferentUUIDs()
        {
            var gen = from key1 in ColonyKeyGen()
                      from key2 in ColonyKeyGen()
                      where key1.Item1 != key2.Item1
                         || key1.Item2 != key2.Item2
                         || key1.Item3 != key2.Item3
                      select new { Key1 = key1, Key2 = key2 };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var uuid1 = DeterministicUUID.Generate(
                    data.Key1.Item1, data.Key1.Item2, data.Key1.Item3);
                var uuid2 = DeterministicUUID.Generate(
                    data.Key2.Item1, data.Key2.Item2, data.Key2.Item3);

                return (uuid1 != uuid2)
                    .Label($"Expected different UUIDs for different inputs, both got '{uuid1}'");
            });
        }

        #endregion

        #region UUID format validation

        /// <summary>
        /// Generated colony UUIDs should be valid GUID strings.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property GeneratedColonyUUIDIsValidGuid()
        {
            return Prop.ForAll(ColonyKeyGen().ToArbitrary(), key =>
            {
                var uuidStr = DeterministicUUID.Generate(key.Item1, key.Item2, key.Item3);
                Guid parsed;
                var canParse = Guid.TryParse(uuidStr, out parsed);

                return canParse.Label($"UUID '{uuidStr}' is not a valid GUID");
            });
        }

        #endregion
    }
}
