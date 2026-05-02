using System;
using System.Collections.Generic;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Tests.Services.Migration
{
    /// <summary>
    /// Property tests for migration version gating.
    /// **Validates: Requirements 3.1, 3.2, 3.3**
    /// </summary>
    [TestFixture]
    public class MigrationVersionGatingPropertyTests
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
        /// For any DataVersion V, after MigrationRunner.Run, both DataVersions
        /// should equal CurrentVersion. If V was already at CurrentVersion,
        /// no migrations should have run (data unchanged except version stamp).
        /// **Validates: Requirements 3.1, 3.2, 3.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property AfterMigrationRunVersionEqualsCurrentVersion()
        {
            var gen = from baselineVersion in Gen.Choose(0, MigrationRunner.CurrentVersion + 1)
                      from playerVersion in Gen.Choose(0, MigrationRunner.CurrentVersion + 1)
                      select new { BaselineVersion = baselineVersion, PlayerVersion = playerVersion };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                TestHelper.ResetWithCachedData();
                var ec = EmpireContext.GetInstance();
                var pc = PlayerContext.GetInstance();

                // Override DataVersion to test value
                ec.DataVersion = data.BaselineVersion;
                pc.DataVersion = data.PlayerVersion;

                MigrationRunner.Run(ec, pc);

                bool baselineCorrect = ec.DataVersion == MigrationRunner.CurrentVersion;
                bool playerCorrect = pc.DataVersion == MigrationRunner.CurrentVersion;

                return (baselineCorrect && playerCorrect)
                    .Label($"Expected DataVersion={MigrationRunner.CurrentVersion}, " +
                           $"got baseline={ec.DataVersion}, player={pc.DataVersion}");
            });
        }

        /// <summary>
        /// When both DataVersions are already at CurrentVersion, running
        /// MigrationRunner should not modify any blueprint UUIDs (no migrations run).
        /// **Validates: Requirements 3.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property AlreadyAtCurrentVersionSkipsMigrations()
        {
            return Prop.ForAll(Arb.Default.Int32().Generator.Where(i => i >= 0).ToArbitrary(), _ =>
            {
                TestHelper.ResetWithCachedData();
                var ec = EmpireContext.GetInstance();
                var pc = PlayerContext.GetInstance();

                // Set both to current version
                ec.DataVersion = MigrationRunner.CurrentVersion;
                pc.DataVersion = MigrationRunner.CurrentVersion;

                // Snapshot UUIDs before
                var uuidsBefore = new List<string>();
                foreach (var bp in ec.GlobalBlueprintList)
                    uuidsBefore.Add(bp.UUID);

                MigrationRunner.Run(ec, pc);

                // Snapshot UUIDs after
                var uuidsAfter = new List<string>();
                foreach (var bp in ec.GlobalBlueprintList)
                    uuidsAfter.Add(bp.UUID);

                bool unchanged = uuidsBefore.Count == uuidsAfter.Count;
                if (unchanged)
                {
                    for (int i = 0; i < uuidsBefore.Count; i++)
                    {
                        if (uuidsBefore[i] != uuidsAfter[i])
                        {
                            unchanged = false;
                            break;
                        }
                    }
                }

                return unchanged
                    .Label("Blueprint UUIDs changed even though DataVersion was already current");
            });
        }
    }
}
