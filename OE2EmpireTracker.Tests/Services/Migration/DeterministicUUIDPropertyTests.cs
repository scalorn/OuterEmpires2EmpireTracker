using System;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Tests.Services.Migration
{
    /// <summary>
    /// Property tests for DeterministicUUID (UUID v5 generation).
    /// **Validates: Requirements 1.1, 1.3, 1.4**
    /// </summary>
    [TestFixture]
    public class DeterministicUUIDPropertyTests
    {
        /// <summary>
        /// For any Dedup_Key, DeterministicUUID.Generate shall produce the same UUID on every call.
        /// **Validates: Requirements 1.1, 1.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property SameInputsProduceSameUUID()
        {
            return Prop.ForAll(DedupKeyGen().ToArbitrary(), key =>
            {
                var uuid1 = DeterministicUUID.Generate(key.Item1, key.Item2, key.Item3, key.Item4, key.Item5);
                var uuid2 = DeterministicUUID.Generate(key.Item1, key.Item2, key.Item3, key.Item4, key.Item5);

                return (uuid1 == uuid2)
                    .Label($"Expected same UUID for same inputs, got '{uuid1}' and '{uuid2}'");
            });
        }

        /// <summary>
        /// Two different Dedup_Keys shall produce different UUIDs.
        /// **Validates: Requirements 1.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property DifferentInputsProduceDifferentUUIDs()
        {
            var gen = from key1 in DedupKeyGen()
                      from key2 in DedupKeyGen()
                      where key1.Item1 != key2.Item1
                         || key1.Item2 != key2.Item2
                         || key1.Item3 != key2.Item3
                         || key1.Item4 != key2.Item4
                         || key1.Item5 != key2.Item5
                      select new { Key1 = key1, Key2 = key2 };

            return Prop.ForAll(
                gen.ToArbitrary(),
                data =>
            {
                var uuid1 = DeterministicUUID.Generate(
                    data.Key1.Item1,
                    data.Key1.Item2,
                    data.Key1.Item3,
                    data.Key1.Item4,
                    data.Key1.Item5);
                var uuid2 = DeterministicUUID.Generate(
                    data.Key2.Item1,
                    data.Key2.Item2,
                    data.Key2.Item3,
                    data.Key2.Item4,
                    data.Key2.Item5);

                return (uuid1 != uuid2)
                    .Label($"Expected different UUIDs for different inputs, both got '{uuid1}'");
            });
        }

        /// <summary>
        /// Generated UUIDs should be valid GUID strings and have version 5 bits set.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property GeneratedUUIDIsValidGuid()
        {
            return Prop.ForAll(DedupKeyGen().ToArbitrary(), key =>
            {
                var uuidStr = DeterministicUUID.Generate(key.Item1, key.Item2, key.Item3, key.Item4, key.Item5);
                Guid parsed;
                var canParse = Guid.TryParse(uuidStr, out parsed);

                return canParse.Label($"UUID '{uuidStr}' is not a valid GUID");
            });
        }

        private static Gen<string> SafeStringGen()
        {
            return Arb.Default.NonEmptyString().Generator.Select(s => s.Get);
        }

        private static Gen<int> SmallIntGen()
        {
            return Gen.Choose(0, 15);
        }

        /// <summary>
        /// Generates a tuple of dedup key fields: (name, evolution, blueprintType, cls, techLevel)
        /// </summary>
        private static Gen<Tuple<string, int, string, int, string>> DedupKeyGen()
        {
            return from name in SafeStringGen()
                   from evolution in SmallIntGen()
                   from blueprintType in SafeStringGen()
                   from cls in SmallIntGen()
                   from techLevel in SafeStringGen()
                   select Tuple.Create(name, evolution, blueprintType, cls, techLevel);
        }
    }
}
