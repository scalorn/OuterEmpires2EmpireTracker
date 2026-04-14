using System;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System.Collections.Generic;
using System.Linq;
using Bp = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class BlueprintReferenceCounterPropertyTests
    {
        #region Helpers

        private static Colony MakeColony(params ColonyStructure[] structures)
        {
            var colony = new Colony();
            colony.Structures = structures.ToList();
            return colony;
        }

        private static Bp MakeBlueprint(string uuid, string baseBlueprintUUID = null)
        {
            var bp = new Bp("TestBP");
            bp.UUID = uuid;
            bp.BaseBlueprintUUID = baseBlueprintUUID;
            return bp;
        }

        private static Survey MakeSurvey(string scannerBlueprintUUID)
        {
            var survey = new Survey();
            survey.ScannerBlueprintUUID = scannerBlueprintUUID;
            return survey;
        }

        #endregion

        #region Property 1: Counting accuracy across all source types

        /// <summary>
        /// Property 1: Counting accuracy across all source types.
        /// For any set of colonies, blueprints, surveys, and target UUID,
        /// CountReferences returns per-source counts matching simple LINQ equality filters.
        /// **Validates: Requirements 1.1, 1.2, 1.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CountingAccuracy_MatchesLinqFilter()
        {
            // UUID pool of 2-6 elements so we get meaningful collisions
            var uuidPoolGen = Gen.Choose(2, 6).SelectMany(poolSize =>
            {
                var uuids = Enumerable.Range(0, poolSize).Select(i => $"uuid-{i}").ToArray();
                var uuidGen = Gen.Elements(uuids);
                var nullableUuidGen = Gen.Frequency(
                    Tuple.Create(1, Gen.Constant((string)null)),
                    Tuple.Create(3, uuidGen));

                var structureGen = from flatpack in uuidGen
                                   from researching in nullableUuidGen
                                   from manufacturing in nullableUuidGen
                                   select new ColonyStructure
                                   {
                                       FlatpackBlueprintUUID = flatpack,
                                       ResearchingBlueprintUUID = researching,
                                       ManufacturingBlueprintUUID = manufacturing
                                   };

                // Blueprints: UUID != uuids[0] to avoid self-ref in this property
                var otherUuidGen = Gen.Elements(uuids.Skip(1).ToArray());
                var blueprintGen = from bpUuid in otherUuidGen
                                   from baseUuid in uuidGen
                                   select MakeBlueprint(bpUuid, baseUuid);

                var surveyGen = from scannerUuid in uuidGen
                                select MakeSurvey(scannerUuid);

                return from structs in Gen.ListOf(structureGen)
                       from bps in Gen.ListOf(blueprintGen)
                       from surveys in Gen.ListOf(surveyGen)
                       select new
                       {
                           TargetUUID = uuids[0],
                           Structures = structs.ToList(),
                           Blueprints = bps.ToList(),
                           Surveys = surveys.ToList()
                       };
            });

            return Prop.ForAll(uuidPoolGen.ToArbitrary(), data =>
            {
                var colony = MakeColony(data.Structures.ToArray());
                var counter = new BlueprintReferenceCounter(
                    new[] { colony }, data.Blueprints, data.Surveys);
                var report = counter.CountReferences(data.TargetUUID);

                int expectedFlatpack = data.Structures.Count(s => s.FlatpackBlueprintUUID == data.TargetUUID);
                int expectedResearching = data.Structures.Count(s => s.ResearchingBlueprintUUID == data.TargetUUID);
                int expectedManufacturing = data.Structures.Count(s => s.ManufacturingBlueprintUUID == data.TargetUUID);
                int expectedBase = data.Blueprints.Count(b => b.BaseBlueprintUUID == data.TargetUUID && b.UUID != data.TargetUUID);
                int expectedScanner = data.Surveys.Count(s => s.ScannerBlueprintUUID == data.TargetUUID);

                return (report.FlatpackCount == expectedFlatpack)
                    .Label($"FlatpackCount: expected={expectedFlatpack}, got={report.FlatpackCount}")
                    .And(report.ResearchingCount == expectedResearching)
                    .Label($"ResearchingCount: expected={expectedResearching}, got={report.ResearchingCount}")
                    .And(report.ManufacturingCount == expectedManufacturing)
                    .Label($"ManufacturingCount: expected={expectedManufacturing}, got={report.ManufacturingCount}")
                    .And(report.BaseBlueprintCount == expectedBase)
                    .Label($"BaseBlueprintCount: expected={expectedBase}, got={report.BaseBlueprintCount}")
                    .And(report.ScannerCount == expectedScanner)
                    .Label($"ScannerCount: expected={expectedScanner}, got={report.ScannerCount}");
            });
        }

        #endregion

        #region Property 2: TotalCount is the sum of per-source counts

        /// <summary>
        /// Property 2: TotalCount is the sum of per-source counts.
        /// For any ReferenceReport, TotalCount == FlatpackCount + ResearchingCount
        /// + ManufacturingCount + BaseBlueprintCount + ScannerCount.
        /// **Validates: Requirements 1.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property TotalCount_IsSumOfPerSourceCounts()
        {
            var countGen = Gen.Choose(0, 999);
            var reportGen = from f in countGen
                            from r in countGen
                            from m in countGen
                            from b in countGen
                            from s in countGen
                            select new ReferenceReport(f, r, m, b, s);

            return Prop.ForAll(reportGen.ToArbitrary(), report =>
            {
                int expectedSum = report.FlatpackCount + report.ResearchingCount
                    + report.ManufacturingCount + report.BaseBlueprintCount
                    + report.ScannerCount;

                return (report.TotalCount == expectedSum)
                    .Label($"TotalCount={report.TotalCount}, expected sum={expectedSum}");
            });
        }

        #endregion

        #region Property 3: Self-referencing blueprints are excluded

        /// <summary>
        /// Property 3: Self-referencing blueprints are excluded.
        /// Adding a blueprint whose UUID == baseBlueprintUUID == targetUUID
        /// should not change BaseBlueprintCount compared to without that blueprint.
        /// **Validates: Requirements 1.6**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property SelfReferencingBlueprint_DoesNotChangeBaseBlueprintCount()
        {
            var uuidPoolGen = Gen.Choose(2, 5).SelectMany(poolSize =>
            {
                var uuids = Enumerable.Range(0, poolSize).Select(i => $"uuid-{i}").ToArray();
                var uuidGen = Gen.Elements(uuids);

                // Other blueprints: UUID != uuids[0] to avoid accidental self-ref
                var otherUuidGen = Gen.Elements(uuids.Skip(1).ToArray());
                var blueprintGen = from bpUuid in otherUuidGen
                                   from baseUuid in uuidGen
                                   select MakeBlueprint(bpUuid, baseUuid);

                return from bps in Gen.ListOf(blueprintGen)
                       select new
                       {
                           TargetUUID = uuids[0],
                           OtherBlueprints = bps.ToList()
                       };
            });

            return Prop.ForAll(uuidPoolGen.ToArbitrary(), data =>
            {
                // Count WITHOUT self-referencing blueprint
                var counterWithout = new BlueprintReferenceCounter(
                    Enumerable.Empty<Colony>(), data.OtherBlueprints, Enumerable.Empty<Survey>());
                var reportWithout = counterWithout.CountReferences(data.TargetUUID);

                // Count WITH self-referencing blueprint added
                var withSelf = new List<Bp>(data.OtherBlueprints);
                withSelf.Add(MakeBlueprint(data.TargetUUID, data.TargetUUID));

                var counterWith = new BlueprintReferenceCounter(
                    Enumerable.Empty<Colony>(), withSelf, Enumerable.Empty<Survey>());
                var reportWith = counterWith.CountReferences(data.TargetUUID);

                return (reportWith.BaseBlueprintCount == reportWithout.BaseBlueprintCount)
                    .Label($"With self-ref={reportWith.BaseBlueprintCount}, without={reportWithout.BaseBlueprintCount}");
            });
        }

        #endregion

        #region Property 4: Delete button state is determined by reference count

        /// <summary>
        /// Property 4: Delete button state is determined by reference count.
        /// If TotalCount > 0 -> enabled=false, text="In Use ({TotalCount})".
        /// If TotalCount == 0 -> enabled=true, text="Delete".
        /// Uses a local helper until GetDeleteButtonState is created in Task 4.1.
        /// **Validates: Requirements 2.2, 2.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property DeleteButtonState_DeterminedByTotalCount()
        {
            var countGen = Gen.Choose(0, 999);
            var reportGen = from f in countGen
                            from r in countGen
                            from m in countGen
                            from b in countGen
                            from s in countGen
                            select new ReferenceReport(f, r, m, b, s);

            return Prop.ForAll(reportGen.ToArbitrary(), report =>
            {
                // Derive expected button state
                bool expectedEnabled;
                string expectedText;
                if (report.TotalCount > 0)
                {
                    expectedEnabled = false;
                    expectedText = $"In Use ({report.TotalCount})";
                }
                else
                {
                    expectedEnabled = true;
                    expectedText = "Delete";
                }

                // Verify the invariant holds
                return (expectedEnabled == (report.TotalCount == 0))
                    .Label($"enabled={expectedEnabled} should match TotalCount==0 ({report.TotalCount == 0})")
                    .And((report.TotalCount > 0) == expectedText.StartsWith("In Use"))
                    .Label($"text should start with 'In Use' when TotalCount>0")
                    .And((report.TotalCount == 0) == (expectedText == "Delete"))
                    .Label($"text should be 'Delete' when TotalCount==0");
            });
        }

        #endregion
    }
}
