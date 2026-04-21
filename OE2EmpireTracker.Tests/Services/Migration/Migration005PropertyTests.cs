using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;
using BpModel = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services.Migration
{
    [TestFixture]
    public class Migration005PropertyTests
    {
        private static readonly Dictionary<string, string> Remap = new Dictionary<string, string>
        {
            { "Blue Collar Detail(s)", GameConstants.PropBlueCollarDetail },
            { "Unassigned White Collar Detail(s)", GameConstants.PropUnassignedWhiteCollarDetail },
            { "Unassigned Specialist Detail(s)", GameConstants.PropUnassignedSpecialistDetail },
            { "Specialist Detail(s)", GameConstants.PropSpecialistDetail },
            { "White Collar Detail(s)", GameConstants.PropWhiteCollarDetail },
            { "Warehousing Capacity", GameConstants.PropWarehouseCapacity },
        };

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

        #region Generators

        private static Gen<string> PropertyValueGen()
        {
            return Gen.Choose(1, 9999).Select(n => n.ToString());
        }

        /// <summary>
        /// Generates a Blueprint with a random subset of old-keyed properties
        /// and some unrelated properties that should be left alone.
        /// </summary>
        private static Gen<BpModel> BlueprintWithOldKeysGen()
        {
            var oldKeys = Remap.Keys.ToList();
            return from subsetSize in Gen.Choose(0, oldKeys.Count)
                   from indices in Gen.Shuffle(Enumerable.Range(0, oldKeys.Count).ToArray())
                   from values in Gen.ListOf(oldKeys.Count, PropertyValueGen())
                   select BuildBlueprint(oldKeys, indices.Take(subsetSize).ToList(), values.ToList());
        }

        private static BpModel BuildBlueprint(
            List<string> oldKeys, List<int> chosenIndices, List<string> values)
        {
            var bp = new BpModel("TestBP");
            bp.UUID = Guid.NewGuid().ToString();

            // Add chosen old keys
            foreach (int idx in chosenIndices)
            {
                bp.Properties.Properties[oldKeys[idx]] = values[idx % values.Count];
            }

            // Add an unrelated property that must survive
            bp.Properties.Properties["Health"] = "500";

            return bp;
        }

        #endregion

        #region Property: Old keys are renamed, values preserved, unrelated keys untouched

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property OldKeysRenamedAndValuesPreserved()
        {
            return Prop.ForAll(BlueprintWithOldKeysGen().ToArbitrary(), bp =>
            {
                // Snapshot old-key values before migration
                var before = new Dictionary<string, string>();
                foreach (var pair in Remap)
                {
                    if (bp.Properties.Properties.ContainsKey(pair.Key))
                        before[pair.Key] = bp.Properties.Properties[pair.Key];
                }

                // Run the migration logic inline (same as Migration005)
                foreach (var pair in Remap)
                {
                    if (bp.Properties.Properties.TryGetValue(pair.Key, out string val))
                    {
                        bp.Properties.Properties[pair.Value] = val;
                        bp.Properties.Properties.Remove(pair.Key);
                    }
                }

                // Verify: no old keys remain
                foreach (var oldKey in Remap.Keys)
                {
                    if (bp.Properties.Properties.ContainsKey(oldKey))
                        return false.Label($"Old key '{oldKey}' still present");
                }

                // Verify: new keys have the original values
                foreach (var kvp in before)
                {
                    string newKey = Remap[kvp.Key];
                    if (!bp.Properties.Properties.TryGetValue(newKey, out string actual))
                        return false.Label($"New key '{newKey}' missing");
                    if (actual != kvp.Value)
                        return false.Label($"Value mismatch for '{newKey}': expected '{kvp.Value}', got '{actual}'");
                }

                // Verify: unrelated key untouched
                if (!bp.Properties.Properties.ContainsKey("Health") ||
                    bp.Properties.Properties["Health"] != "500")
                    return false.Label("Unrelated key 'Health' was modified");

                return true.Label("All old keys renamed, values preserved, unrelated keys untouched");
            });
        }

        #endregion

        #region Property: Blueprint with no old keys is unchanged

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property BlueprintWithNoOldKeysIsUnchanged()
        {
            var gen = from val in PropertyValueGen()
                      select new BpModel("CleanBP")
                      {
                          UUID = Guid.NewGuid().ToString(),
                          Properties = new PropertyBag()
                      };

            return Prop.ForAll(gen.Select(bp =>
            {
                bp.Properties.Properties["Health"] = "100";
                bp.Properties.Properties["Power"] = "200";
                return bp;
            }).ToArbitrary(), bp =>
            {
                int countBefore = bp.Properties.Properties.Count;
                var keysBefore = new HashSet<string>(bp.Properties.Properties.Keys);

                // Run migration logic
                foreach (var pair in Remap)
                {
                    if (bp.Properties.Properties.TryGetValue(pair.Key, out string val))
                    {
                        bp.Properties.Properties[pair.Value] = val;
                        bp.Properties.Properties.Remove(pair.Key);
                    }
                }

                bool countSame = bp.Properties.Properties.Count == countBefore;
                bool keysSame = new HashSet<string>(bp.Properties.Properties.Keys).SetEquals(keysBefore);

                return (countSame && keysSame)
                    .Label("Properties changed on a blueprint with no old keys");
            });
        }

        #endregion

        #region Integration: Full migration via MigrationRunner

        [Test]
        public void FullMigrationRenamesOldKeysInPlayerAndGlobalBlueprints()
        {
            var ec = EmpireContext.GetInstance();
            var pc = PlayerContext.GetInstance();

            // Set versions to 4 so migration 5 runs
            ec.DataVersion = 4;
            pc.DataVersion = 4;

            // Inject old-keyed properties into a player blueprint
            var playerBp = new BpModel("PlayerBP") { UUID = Guid.NewGuid().ToString() };
            playerBp.Properties.Properties["Blue Collar Detail(s)"] = "42";
            playerBp.Properties.Properties["Warehousing Capacity"] = "100";
            playerBp.Properties.Properties["Health"] = "500";
            pc.AddBlueprint(playerBp);

            // Inject old-keyed properties into a global blueprint
            var globalBp = new BpModel("GlobalBP") { UUID = Guid.NewGuid().ToString() };
            globalBp.Properties.Properties["Specialist Detail(s)"] = "7";
            globalBp.Properties.Properties["White Collar Detail(s)"] = "3";
            ec.AddGlobalBlueprint(globalBp);

            MigrationRunner.Run(ec, pc);

            // Player blueprint assertions
            Assert.That(playerBp.Properties.Properties.ContainsKey("Blue Collar Detail(s)"), Is.False);
            Assert.That(playerBp.Properties.Properties.ContainsKey("Warehousing Capacity"), Is.False);
            Assert.That(playerBp.Properties.Properties[GameConstants.PropBlueCollarDetail], Is.EqualTo("42"));
            Assert.That(playerBp.Properties.Properties[GameConstants.PropWarehouseCapacity], Is.EqualTo("100"));
            Assert.That(playerBp.Properties.Properties["Health"], Is.EqualTo("500"));

            // Global blueprint assertions
            Assert.That(globalBp.Properties.Properties.ContainsKey("Specialist Detail(s)"), Is.False);
            Assert.That(globalBp.Properties.Properties.ContainsKey("White Collar Detail(s)"), Is.False);
            Assert.That(globalBp.Properties.Properties[GameConstants.PropSpecialistDetail], Is.EqualTo("7"));
            Assert.That(globalBp.Properties.Properties[GameConstants.PropWhiteCollarDetail], Is.EqualTo("3"));

            // Version bumped
            Assert.That(ec.DataVersion, Is.EqualTo(MigrationRunner.CurrentVersion));
            Assert.That(pc.DataVersion, Is.EqualTo(MigrationRunner.CurrentVersion));
        }

        #endregion
    }
}
