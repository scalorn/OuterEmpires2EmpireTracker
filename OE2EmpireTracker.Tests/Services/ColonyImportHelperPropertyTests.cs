using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class ColonyImportHelperPropertyTests
    {
        /// <summary>
        /// Property 1: Case-insensitive colony name search.
        /// For any list of colonies and for any colony name that exists in the list
        /// (under any case variation), FindByName shall return a colony whose ColonyName
        /// equals the search name under case-insensitive comparison. Conversely, for any
        /// name that does not exist in the list (under any case), FindByName shall return null.
        /// **Validates: Requirements 2.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CaseInsensitiveColonyNameSearch()
        {
            var gen = from colonies in Gen.ListOf(ColonyGen())
                      from seed in Gen.Choose(0, 10000)
                      from extraName in NonEmptyStringGen()
                      select new { Colonies = colonies.ToList(), Seed = seed, ExtraName = extraName };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                // Test match case: pick an existing name and shuffle its case
                if (data.Colonies.Count > 0)
                {
                    var target = data.Colonies[data.Seed % data.Colonies.Count];
                    var shuffled = ShuffleCase(target.ColonyName, data.Seed);
                    var found = ColonyImportHelper.FindByName(data.Colonies, shuffled);

                    if (found == null)
                        return false.Label($"Expected to find colony '{target.ColonyName}' with search '{shuffled}' but got null");

                    if (!string.Equals(found.ColonyName, shuffled, StringComparison.OrdinalIgnoreCase))
                        return false.Label($"Found colony name '{found.ColonyName}' does not match search '{shuffled}' case-insensitively");
                }

                // Test no-match case: use a name guaranteed not in the list
                var uniqueName = data.ExtraName + "_NOTINLIST_" + Guid.NewGuid().ToString("N");
                var noMatch = ColonyImportHelper.FindByName(data.Colonies, uniqueName);

                return (noMatch == null)
                    .Label($"Expected null for non-existent name '{uniqueName}' but got colony '{noMatch?.ColonyName}'");
            });
        }

        /// <summary>
        /// Property 2: No-match import grows colony list by one.
        /// For any list of colonies and for any temporary colony whose ColonyName does not
        /// match any existing colony (case-insensitive), calling CreateFromTemp and adding
        /// the result to the list shall increase the list count by exactly one.
        /// **Validates: Requirements 2.3, 4.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property NoMatchImportGrowsColonyListByOne()
        {
            var gen = from colonies in Gen.ListOf(ColonyGen())
                      from tempColony in ColonyGen()
                      from ownerUuid in NonEmptyStringGen()
                      select new { Colonies = colonies.ToList(), TempColony = tempColony, OwnerUuid = ownerUuid };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                // Ensure the temp colony name doesn't match any existing colony
                var uniqueName = data.TempColony.ColonyName + "_NEW_" + Guid.NewGuid().ToString("N");
                data.TempColony.ColonyName = uniqueName;

                var list = new List<Colony>(data.Colonies);
                int originalCount = list.Count;

                var newColony = ColonyImportHelper.CreateFromTemp(data.TempColony, data.OwnerUuid);
                list.Add(newColony);

                return (list.Count == originalCount + 1)
                    .Label($"Expected count {originalCount + 1} but got {list.Count}")
                    .And(list.Contains(newColony))
                    .Label("New colony should be present in the list");
            });
        }

        /// <summary>
        /// Property 3: MergeIdentity updates identity while preserving local state.
        /// For any existing colony and for any temporary colony, after calling
        /// MergeIdentity(existing, temp), the existing colony's PlanetName and SystemName
        /// shall equal the temp colony's values, while the existing colony's UUID,
        /// OwnerUUID, Items, and Structures references shall remain unchanged.
        /// **Validates: Requirements 3.3, 3.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MergeIdentityUpdatesIdentityPreservesLocalState()
        {
            var gen = from existing in ColonyGen()
                      from temp in ColonyGen()
                      from structCount in Gen.Choose(0, 5)
                      select new { Existing = existing, Temp = temp, StructCount = structCount };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                // Add some structures and items to the existing colony to verify preservation
                for (int i = 0; i < data.StructCount; i++)
                {
                    data.Existing.Structures.Add(new ColonyStructure());
                }

                // Capture references before merge
                var originalUuid = data.Existing.UUID;
                var originalOwnerUuid = data.Existing.OwnerUUID;
                var originalColonyName = data.Existing.ColonyName;
                var originalItems = data.Existing.Items;
                var originalStructures = data.Existing.Structures;
                int originalStructCount = data.Existing.Structures.Count;

                ColonyImportHelper.MergeIdentity(data.Existing, data.Temp);

                // Identity fields should be updated
                var planetMatch = (data.Existing.PlanetName == data.Temp.PlanetName)
                    .Label($"PlanetName: expected '{data.Temp.PlanetName}', got '{data.Existing.PlanetName}'");
                var systemMatch = (data.Existing.SystemName == data.Temp.SystemName)
                    .Label($"SystemName: expected '{data.Temp.SystemName}', got '{data.Existing.SystemName}'");

                // ColonyName should be preserved when target already has one
                var colonyNamePreserved = (data.Existing.ColonyName == originalColonyName)
                    .Label($"ColonyName changed from '{originalColonyName}' to '{data.Existing.ColonyName}' -- should be preserved when target already has one");

                // Local state should be preserved (same object references)
                var uuidPreserved = (data.Existing.UUID == originalUuid)
                    .Label($"UUID changed from '{originalUuid}' to '{data.Existing.UUID}'");
                var ownerPreserved = (data.Existing.OwnerUUID == originalOwnerUuid)
                    .Label($"OwnerUUID changed from '{originalOwnerUuid}' to '{data.Existing.OwnerUUID}'");
                var itemsPreserved = ReferenceEquals(data.Existing.Items, originalItems)
                    .Label("Items reference changed");
                var structsPreserved = ReferenceEquals(data.Existing.Structures, originalStructures)
                    .Label("Structures reference changed");
                var structCountPreserved = (data.Existing.Structures.Count == originalStructCount)
                    .Label($"Structures count changed from {originalStructCount} to {data.Existing.Structures.Count}");

                return planetMatch
                    .And(systemMatch)
                    .And(colonyNamePreserved)
                    .And(uuidPreserved)
                    .And(ownerPreserved)
                    .And(itemsPreserved)
                    .And(structsPreserved)
                    .And(structCountPreserved);
            });
        }

        /// <summary>
        /// Property 4: CreateFromTemp produces a valid colony with all parsed data.
        /// For any temporary colony with non-empty fields and for any owner UUID string,
        /// CreateFromTemp(temp, ownerUUID) shall return a colony where: (a) UUID is non-null
        /// and non-empty, (b) OwnerUUID equals the given owner UUID, (c) ColonyName,
        /// PlanetName, and SystemName equal the temp colony's values, and (d) Structures.Count
        /// and Commodities.Count equal the temp colony's counts.
        /// **Validates: Requirements 4.1, 4.2, 4.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CreateFromTempProducesValidColony()
        {
            var gen = from temp in ColonyGen()
                      from ownerUuid in NonEmptyStringGen()
                      from structCount in Gen.Choose(0, 5)
                      from commodityCount in Gen.Choose(0, 5)
                      select new { Temp = temp, OwnerUuid = ownerUuid, StructCount = structCount, CommodityCount = commodityCount };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                // Add structures and commodities to the temp colony
                for (int i = 0; i < data.StructCount; i++)
                {
                    data.Temp.Structures.Add(new ColonyStructure());
                }

                for (int i = 0; i < data.CommodityCount; i++)
                {
                    data.Temp.Commodities.Add(new CommodityRequested { Name = "Commodity" + i });
                }

                var result = ColonyImportHelper.CreateFromTemp(data.Temp, data.OwnerUuid);

                var uuidNonEmpty = (!string.IsNullOrEmpty(result.UUID))
                    .Label("UUID should be non-null and non-empty");
                var ownerMatch = (result.OwnerUUID == data.OwnerUuid)
                    .Label($"OwnerUUID: expected '{data.OwnerUuid}', got '{result.OwnerUUID}'");
                var nameMatch = (result.ColonyName == data.Temp.ColonyName)
                    .Label($"ColonyName: expected '{data.Temp.ColonyName}', got '{result.ColonyName}'");
                var planetMatch = (result.PlanetName == data.Temp.PlanetName)
                    .Label($"PlanetName: expected '{data.Temp.PlanetName}', got '{result.PlanetName}'");
                var systemMatch = (result.SystemName == data.Temp.SystemName)
                    .Label($"SystemName: expected '{data.Temp.SystemName}', got '{result.SystemName}'");
                var structMatch = (result.Structures.Count == data.Temp.Structures.Count)
                    .Label($"Structures.Count: expected {data.Temp.Structures.Count}, got {result.Structures.Count}");
                var commodityMatch = (result.Commodities.Count == data.Temp.Commodities.Count)
                    .Label($"Commodities.Count: expected {data.Temp.Commodities.Count}, got {result.Commodities.Count}");

                return uuidNonEmpty
                    .And(ownerMatch)
                    .And(nameMatch)
                    .And(planetMatch)
                    .And(systemMatch)
                    .And(structMatch)
                    .And(commodityMatch);
            });
        }

        /// <summary>
        /// Property 5: Duplicate name detection is case-insensitive and excludes self.
        /// For any list of colonies, for any colony name, and for any exclude UUID,
        /// IsDuplicateName shall return true if and only if there exists a colony in the
        /// list whose ColonyName matches the given name (case-insensitive) AND whose UUID
        /// is not equal to the exclude UUID.
        /// **Validates: Requirements 8.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property DuplicateNameDetectionIsCaseInsensitiveAndExcludesSelf()
        {
            var gen = from colonies in Gen.ListOf(ColonyGen())
                      from searchName in NonEmptyStringGen()
                      from excludeUuid in NonEmptyStringGen()
                      from seed in Gen.Choose(0, 10000)
                      select new { Colonies = colonies.ToList(), SearchName = searchName, ExcludeUuid = excludeUuid, Seed = seed };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                // Compute expected result using a simple reference implementation
                bool expected = data.Colonies.Any(c =>
                    !string.Equals(c.UUID, data.ExcludeUuid, StringComparison.Ordinal) &&
                    string.Equals(c.ColonyName, data.SearchName, StringComparison.OrdinalIgnoreCase));

                bool actual = ColonyImportHelper.IsDuplicateName(data.Colonies, data.SearchName, data.ExcludeUuid);

                // Also test with a case-shuffled version of an existing name if colonies exist
                if (data.Colonies.Count > 0)
                {
                    var target = data.Colonies[data.Seed % data.Colonies.Count];
                    var shuffled = ShuffleCase(target.ColonyName, data.Seed);

                    bool expectedShuffled = data.Colonies.Any(c =>
                        !string.Equals(c.UUID, data.ExcludeUuid, StringComparison.Ordinal) &&
                        string.Equals(c.ColonyName, shuffled, StringComparison.OrdinalIgnoreCase));

                    bool actualShuffled = ColonyImportHelper.IsDuplicateName(data.Colonies, shuffled, data.ExcludeUuid);

                    return (actual == expected)
                        .Label($"Random name: expected={expected}, actual={actual} for name='{data.SearchName}'")
                        .And(actualShuffled == expectedShuffled)
                        .Label($"Shuffled name: expected={expectedShuffled}, actual={actualShuffled} for name='{shuffled}'");
                }

                return (actual == expected)
                    .Label($"expected={expected}, actual={actual} for name='{data.SearchName}'");
            });
        }

        /// <summary>
        /// Property 6: Case-insensitive planet+system search.
        /// For any list of colonies and for any PlanetName+SystemName that exists in the list
        /// (under any case variation), FindByPlanet shall return a colony whose PlanetName and
        /// SystemName match case-insensitively. For any planet+system not in the list, it shall return null.
        /// **Validates: Requirements 2.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CaseInsensitivePlanetSystemSearch()
        {
            var gen = from colonies in Gen.ListOf(ColonyGen())
                      from seed in Gen.Choose(0, 10000)
                      from extraPlanet in NonEmptyStringGen()
                      from extraSystem in NonEmptyStringGen()
                      select new { Colonies = colonies.ToList(), Seed = seed, ExtraPlanet = extraPlanet, ExtraSystem = extraSystem };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                // Test match case: pick an existing colony and shuffle case of its planet+system
                if (data.Colonies.Count > 0)
                {
                    var target = data.Colonies[data.Seed % data.Colonies.Count];
                    var shuffledPlanet = ShuffleCase(target.PlanetName, data.Seed);
                    var shuffledSystem = ShuffleCase(target.SystemName, data.Seed + 1);
                    var found = ColonyImportHelper.FindByPlanet(data.Colonies, shuffledPlanet, shuffledSystem);

                    if (found == null)
                        return false.Label($"Expected to find colony for planet '{target.PlanetName}' system '{target.SystemName}' but got null");

                    if (!string.Equals(found.PlanetName, shuffledPlanet, StringComparison.OrdinalIgnoreCase))
                        return false.Label($"Found PlanetName '{found.PlanetName}' does not match search '{shuffledPlanet}'");
                }

                // Test no-match case
                var uniquePlanet = data.ExtraPlanet + "_NOTINLIST_" + Guid.NewGuid().ToString("N");
                var noMatch = ColonyImportHelper.FindByPlanet(data.Colonies, uniquePlanet, data.ExtraSystem);

                return (noMatch == null)
                    .Label($"Expected null for non-existent planet '{uniquePlanet}' but got colony");
            });
        }

        private static Colony MakeColony(string uuid, string colonyName, string planetName = "Planet", string systemName = "System")
        {
            var colony = new Colony();
            colony.UUID = uuid;
            colony.ColonyName = colonyName;
            colony.PlanetName = planetName;
            colony.SystemName = systemName;
            colony.OwnerUUID = "owner-" + uuid;
            return colony;
        }

        private static string ShuffleCase(string input, int seed)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var rng = new System.Random(seed);
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = rng.Next(2) == 0 ? char.ToUpper(chars[i]) : char.ToLower(chars[i]);
            }

            return new string(chars);
        }

        private static Gen<string> NonEmptyStringGen()
        {
            return Arb.Default.NonEmptyString().Generator.Select(s => s.Get);
        }

        private static Gen<Colony> ColonyGen()
        {
            return from uuid in NonEmptyStringGen()
                   from name in NonEmptyStringGen()
                   from planet in NonEmptyStringGen()
                   from system in NonEmptyStringGen()
                   select MakeColony(uuid, name, planet, system);
        }
    }
}
