using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Tests.Services.Migration
{
    /// <summary>
    /// Property tests for LegacyUUID preservation.
    /// **Validates: Requirements 6.2, 6.3**
    /// </summary>
    [TestFixture]
    public class LegacyUUIDPropertyTests
    {
        [SetUp]
        public void SetUp()
        {
            EmpireContext.Reset();
            TestHelper.SetAllFilePaths();
        }

        [TearDown]
        public void TearDown()
        {
            EmpireContext.Reset();
        }

        /// <summary>
        /// For any blueprint that undergoes UUID migration, LegacyUUID shall
        /// equal the original UUID. Running migration again shall not change it.
        /// **Validates: Requirements 6.2, 6.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property LegacyUUIDEqualsOriginalAndIsPreserved()
        {
            var gen = from bpCount in Gen.Choose(1, 5)
                      from seed in Arb.Default.Int32().Generator
                      select new { BpCount = bpCount, Seed = seed };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                EmpireContext.Reset();
                TestHelper.SetAllFilePaths();
                var ec = EmpireContext.GetInstance();
                var pc = PlayerContext.GetInstance();

                // Clear existing blueprints so only test-generated ones are present
                foreach (var item in ec.GlobalBlueprintList.ToList()) ec.RemoveGlobalBlueprint(item);

                var rng = new System.Random(data.Seed);
                var originalUUIDs = new Dictionary<string, string>(); // name -> original UUID

                // Add blueprints with unique names and random (non-deterministic) UUIDs
                var usedNames = new HashSet<string>();
                for (int i = 0; i < data.BpCount; i++)
                {
                    string name;
                    do
                    {
                        name = "TestBP_" + rng.Next(10000);
                    }
                    while (!usedNames.Add(name));
                    var bp = new OE2EmpireTracker.Models.Blueprint(name);
                    bp.UUID = Guid.NewGuid().ToString();
                    bp.Evolution = 0;
                    bp.BluePrintType = "Hull";
                    bp.Class = 1;
                    bp.TechLevel = "LL";
                    bp.LegacyUUID = null;
                    originalUUIDs[name] = bp.UUID;
                    ec.AddGlobalBlueprint(bp);
                }

                // Set DataVersion to 0 so migration runs
                ec.DataVersion = 0;
                pc.DataVersion = 0;

                // Run migration
                Migration001_DeterministicUUIDs.Run(ec, pc);

                // Verify LegacyUUID equals original UUID for each migrated blueprint
                bool legacyCorrect = true;
                foreach (var kvp in originalUUIDs)
                {
                    string deterministicUUID = DeterministicUUID.Generate(kvp.Key, 0, "Hull", 1, "LL");
                    var bp = ec.GlobalBlueprintList.FirstOrDefault(b => b.UUID == deterministicUUID);
                    if (bp == null) continue;

                    if (bp.LegacyUUID != kvp.Value)
                    {
                        legacyCorrect = false;
                        break;
                    }
                }

                // Snapshot LegacyUUIDs
                var legacySnapshot = ec.GlobalBlueprintList
                    .Where(b => b.LegacyUUID != null)
                    .ToDictionary(b => b.UUID, b => b.LegacyUUID);

                // Run migration again -- LegacyUUID should not change
                Migration001_DeterministicUUIDs.Run(ec, pc);

                bool preservedAfterRerun = true;
                foreach (var kvp in legacySnapshot)
                {
                    var bp = ec.GlobalBlueprintList.FirstOrDefault(b => b.UUID == kvp.Key);
                    if (bp == null || bp.LegacyUUID != kvp.Value)
                    {
                        preservedAfterRerun = false;
                        break;
                    }
                }

                return (legacyCorrect && preservedAfterRerun)
                    .Label($"LegacyUUID incorrect: legacyCorrect={legacyCorrect}, preservedAfterRerun={preservedAfterRerun}");
            });
        }
    }
}
