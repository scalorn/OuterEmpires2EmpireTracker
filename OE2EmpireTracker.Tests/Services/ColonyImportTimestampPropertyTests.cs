using System;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Feature: colony-import-timestamp, Property 2: Import operations produce valid ISO timestamps
    /// </summary>
    [TestFixture]
    public class ColonyImportTimestampPropertyTests
    {
        private static Gen<string> NonEmptyStringGen()
        {
            return Arb.Default.NonEmptyString().Generator.Select(s => s.Get);
        }

        private static Gen<Colony> TempColonyGen()
        {
            return from name in NonEmptyStringGen()
                   from planet in NonEmptyStringGen()
                   from system in NonEmptyStringGen()
                   select new Colony
                   {
                       ColonyName = name,
                       PlanetName = planet,
                       SystemName = system
                   };
        }

        /// <summary>
        /// Property 2: Import operations produce valid ISO timestamps.
        /// For any valid temp colony and owner UUID, CreateFromTemp should produce a Colony
        /// whose LastImportDateTime is a non-null string that successfully parses via
        /// SurveyDateTimeParser.TryParseIso. Likewise for MergeIdentity.
        /// **Validates: Requirements 2.1, 2.2, 2.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ImportOperationsProduceValidIsoTimestamps()
        {
            var gen = from temp in TempColonyGen()
                      from target in TempColonyGen()
                      from ownerUuid in NonEmptyStringGen()
                      select new { Temp = temp, Target = target, OwnerUuid = ownerUuid };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                // Test CreateFromTemp
                var created = ColonyImportHelper.CreateFromTemp(data.Temp, data.OwnerUuid);
                bool createdNonNull = !string.IsNullOrEmpty(created.LastImportDateTime);
                bool createdParses = SurveyDateTimeParser.TryParseIso(created.LastImportDateTime, out DateTime createdDt);

                // Test MergeIdentity
                ColonyImportHelper.MergeIdentity(data.Target, data.Temp);
                bool mergedNonNull = !string.IsNullOrEmpty(data.Target.LastImportDateTime);
                bool mergedParses = SurveyDateTimeParser.TryParseIso(data.Target.LastImportDateTime, out DateTime mergedDt);

                return createdNonNull
                    .Label("CreateFromTemp: LastImportDateTime should be non-null")
                    .And(createdParses)
                    .Label($"CreateFromTemp: LastImportDateTime '{created.LastImportDateTime}' should parse via TryParseIso")
                    .And(mergedNonNull)
                    .Label("MergeIdentity: LastImportDateTime should be non-null")
                    .And(mergedParses)
                    .Label($"MergeIdentity: LastImportDateTime '{data.Target.LastImportDateTime}' should parse via TryParseIso");
            });
        }
    }
}
