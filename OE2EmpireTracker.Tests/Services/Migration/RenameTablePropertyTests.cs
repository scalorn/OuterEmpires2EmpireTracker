using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Tests.Services.Migration
{
    /// <summary>
    /// Property tests for rename table idempotency.
    /// **Validates: Requirements 5.6**
    /// </summary>
    [TestFixture]
    public class RenameTablePropertyTests
    {
        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
        }

        [TearDown]
        public void TearDown()
        {
            EmpireContext.Reset();
        }

        /// <summary>
        /// Applies a set of rename entries to a data model, then applies them again.
        /// The second application should produce no changes (idempotent).
        /// **Validates: Requirements 5.6**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property RenameAppliedTwiceProducesSameResult()
        {
            var gen = from renameCount in Gen.Choose(0, 3)
                      from bpCount in Gen.Choose(1, 5)
                      from seed in Arb.Default.Int32().Generator
                      select new { RenameCount = renameCount, BpCount = bpCount, Seed = seed };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                TestHelper.ResetWithCachedData();
                var ec = EmpireContext.GetInstance();
                var pc = PlayerContext.GetInstance();

                var rng = new System.Random(data.Seed);
                var entries = new List<RenameEntry>();
                var bpNames = new List<string>();

                // Create blueprints with deterministic UUIDs (use index prefix to guarantee uniqueness)
                for (int i = 0; i < data.BpCount; i++)
                {
                    string name = "BP_" + i + "_" + rng.Next(1000);
                    bpNames.Add(name);
                    var bp = new OE2EmpireTracker.Models.Blueprint(name);
                    bp.Evolution = 0;
                    bp.BluePrintType = "Hull";
                    bp.Class = 1;
                    bp.TechLevel = "LL";
                    bp.UUID = DeterministicUUID.Generate(name, 0, "Hull", 1, "LL");
                    ec.AddGlobalBlueprint(bp);
                }

                // Create rename entries that rename existing blueprints
                for (int i = 0; i < data.RenameCount && i < bpNames.Count; i++)
                {
                    string oldName = bpNames[i];
                    string newName = "Renamed_" + rng.Next(1000);
                    entries.Add(new RenameEntry(oldName, newName, 0, "Hull", 1, "LL"));
                }

                // Apply renames once using direct logic (since _entries is private)
                ApplyEntries(entries, ec, pc);

                // Snapshot state after first application
                string snapshotAfterFirst = SerializeState(ec, pc);

                // Apply renames again
                ApplyEntries(entries, ec, pc);

                // Snapshot state after second application
                string snapshotAfterSecond = SerializeState(ec, pc);

                return (snapshotAfterFirst == snapshotAfterSecond)
                    .Label("State changed after second rename application (not idempotent)");
            });
        }

        private static void ApplyEntries(List<RenameEntry> entries, EmpireContext ec, PlayerContext pc)
        {
            foreach (var entry in entries)
            {
                string oldUUID = DeterministicUUID.Generate(
                    entry.OldName,
                    entry.Evolution,
                    entry.BluePrintType,
                    entry.Class,
                    entry.TechLevel);
                string newUUID = DeterministicUUID.Generate(
                    entry.NewName,
                    entry.Evolution,
                    entry.BluePrintType,
                    entry.Class,
                    entry.TechLevel);

                var bp = ec.GlobalBlueprintList.FirstOrDefault(b => b.UUID == oldUUID);
                if (bp == null) continue;

                bp.Name = entry.NewName;
                bp.UUID = newUUID;
                RemapUUID.Remap(ec, pc, oldUUID, newUUID);
            }
        }

        private static string SerializeState(EmpireContext ec, PlayerContext pc)
        {
            var state = new
            {
                GlobalBPs = ec.GlobalBlueprintList.Select(b => new { b.UUID, b.Name, b.BaseBlueprintUUID }).ToList(),
                PlayerBPs = pc.BlueprintList.Select(b => new { b.UUID, b.Name, b.BaseBlueprintUUID }).ToList(),
                Colonies = pc.ColonyList.Select(c => new
                {
                    c.UUID,
                    Structures = c.Structures.Select(s => new
                    {
                        s.FlatpackBlueprintUUID,
                        s.ResearchingBlueprintUUID,
                        s.ManufacturingBlueprintUUID
                    }).ToList()
                }).ToList()
            };

            return JsonConvert.SerializeObject(state);
        }
    }
}
