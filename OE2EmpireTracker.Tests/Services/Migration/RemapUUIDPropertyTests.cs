using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Property tests for RemapUUID completeness.
    /// **Validates: Requirements 4.1, 4.2, 4.3, 4.4, 4.5, 4.6**
    /// </summary>
    [TestFixture]
    public class RemapUUIDPropertyTests
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
        /// Collects all UUID-bearing fields from the data model into a flat list.
        /// </summary>
        private static List<string> CollectAllUUIDs(EmpireContext ec, PlayerContext pc)
        {
            var uuids = new List<string>();

            foreach (var bp in ec.GlobalBlueprintList)
            {
                if (bp.UUID != null) uuids.Add(bp.UUID);
                if (bp.BaseBlueprintUUID != null) uuids.Add(bp.BaseBlueprintUUID);
            }

            foreach (var bp in pc.BlueprintList)
            {
                if (bp.UUID != null) uuids.Add(bp.UUID);
                if (bp.BaseBlueprintUUID != null) uuids.Add(bp.BaseBlueprintUUID);
            }

            foreach (var colony in pc.ColonyList)
            {
                foreach (var s in colony.Structures)
                {
                    if (s.FlatpackBlueprintUUID != null) uuids.Add(s.FlatpackBlueprintUUID);
                    if (s.ResearchingBlueprintUUID != null) uuids.Add(s.ResearchingBlueprintUUID);
                    if (s.ManufacturingBlueprintUUID != null) uuids.Add(s.ManufacturingBlueprintUUID);
                }
            }

            return uuids;
        }

        /// <summary>
        /// For any data model state and any (oldUUID, newUUID) pair, after RemapUUID.Remap,
        /// no reference to oldUUID shall remain in any Blueprint.UUID, Blueprint.BaseBlueprintUUID,
        /// or ColonyStructure UUID fields.
        /// **Validates: Requirements 4.1, 4.2, 4.3, 4.4, 4.5, 4.6**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property AfterRemapNoOldUUIDRemains()
        {
            // Generate: a target UUID to plant, a replacement UUID, and counts for data objects
            var gen = from oldUuid in Arb.Default.NonEmptyString().Generator.Select(s => "old-" + s.Get)
                      from newUuid in Arb.Default.NonEmptyString().Generator.Select(s => "new-" + s.Get)
                      from globalCount in Gen.Choose(0, 5)
                      from playerCount in Gen.Choose(0, 5)
                      from colonyCount in Gen.Choose(0, 3)
                      from structsPerColony in Gen.Choose(0, 4)
                      from plantMask in Gen.Choose(0, 127) // 7 bits: which fields get the old UUID
                      select new
                      {
                          OldUuid = oldUuid,
                          NewUuid = newUuid,
                          GlobalCount = globalCount,
                          PlayerCount = playerCount,
                          ColonyCount = colonyCount,
                          StructsPerColony = structsPerColony,
                          PlantMask = plantMask
                      };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                // Load real contexts from test data
                EmpireContext.Reset();
                TestHelper.SetAllFilePaths();
                var ec = EmpireContext.GetInstance();
                var pc = PlayerContext.GetInstance();

                // Add synthetic global blueprints
                int bitIndex = 0;
                for (int i = 0; i < data.GlobalCount; i++)
                {
                    var bp = new OE2EmpireTracker.Models.Blueprint($"Global_{i}");
                    bp.UUID = ((data.PlantMask >> (bitIndex++ % 7)) & 1) == 1
                        ? data.OldUuid : Guid.NewGuid().ToString();
                    bp.BaseBlueprintUUID = ((data.PlantMask >> (bitIndex++ % 7)) & 1) == 1
                        ? data.OldUuid : null;
                    ec.AddGlobalBlueprint(bp);
                }

                // Add synthetic player blueprints
                for (int i = 0; i < data.PlayerCount; i++)
                {
                    var bp = new OE2EmpireTracker.Models.Blueprint($"Player_{i}");
                    bp.UUID = ((data.PlantMask >> (bitIndex++ % 7)) & 1) == 1
                        ? data.OldUuid : Guid.NewGuid().ToString();
                    bp.BaseBlueprintUUID = ((data.PlantMask >> (bitIndex++ % 7)) & 1) == 1
                        ? data.OldUuid : null;
                    pc.AddBlueprint(bp);
                }

                // Add synthetic colonies with structures
                for (int c = 0; c < data.ColonyCount; c++)
                {
                    var colony = new Colony();
                    colony.UUID = Guid.NewGuid().ToString();
                    colony.PlanetName = $"Planet_{c}";
                    colony.ColonyName = $"Colony_{c}";

                    for (int s = 0; s < data.StructsPerColony; s++)
                    {
                        var cs = new ColonyStructure();
                        cs.UUID = Guid.NewGuid().ToString();
                        cs.FlatpackBlueprintUUID = ((data.PlantMask >> (bitIndex++ % 7)) & 1) == 1
                            ? data.OldUuid : Guid.NewGuid().ToString();
                        cs.ResearchingBlueprintUUID = ((data.PlantMask >> (bitIndex++ % 7)) & 1) == 1
                            ? data.OldUuid : null;
                        cs.ManufacturingBlueprintUUID = ((data.PlantMask >> (bitIndex++ % 7)) & 1) == 1
                            ? data.OldUuid : null;
                        colony.Structures.Add(cs);
                    }

                    pc.AddColony(colony);
                }

                // Act
                RemapUUID.Remap(ec, pc, data.OldUuid, data.NewUuid);

                // Assert: no old UUID remains in any tracked field
                var remaining = CollectAllUUIDs(ec, pc);
                var oldRemains = remaining.Any(u => u == data.OldUuid);

                return (!oldRemains)
                    .Label($"Old UUID '{data.OldUuid}' still found after Remap. " +
                           $"Remaining count: {remaining.Count(u => u == data.OldUuid)}");
            });
        }
    }
}
