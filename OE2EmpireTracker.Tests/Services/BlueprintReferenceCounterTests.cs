using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using Bp = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class BlueprintReferenceCounterTests
    {
        private const string TargetUUID = "bp-target-uuid";

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

        // Requirement 1.5 -- empty data returns zero counts
        [Test]
        public void CountReferences_EmptyData_ReturnsZeroCounts()
        {
            var counter = new BlueprintReferenceCounter(
                Enumerable.Empty<Colony>(),
                Enumerable.Empty<Bp>(),
                Enumerable.Empty<Survey>());

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.TotalCount, Is.EqualTo(0));
            Assert.That(report.FlatpackCount, Is.EqualTo(0));
            Assert.That(report.ResearchingCount, Is.EqualTo(0));
            Assert.That(report.ManufacturingCount, Is.EqualTo(0));
            Assert.That(report.BaseBlueprintCount, Is.EqualTo(0));
            Assert.That(report.ScannerCount, Is.EqualTo(0));
        }

        // Requirement 1.1 -- FlatpackBlueprintUUID match
        [Test]
        public void CountReferences_SingleFlatpackMatch_FlatpackCountIsOne()
        {
            var structure = new ColonyStructure { FlatpackBlueprintUUID = TargetUUID };
            var colony = MakeColony(structure);

            var counter = new BlueprintReferenceCounter(
                new[] { colony },
                Enumerable.Empty<Bp>(),
                Enumerable.Empty<Survey>());

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.FlatpackCount, Is.EqualTo(1));
            Assert.That(report.TotalCount, Is.EqualTo(1));
        }

        // Requirement 1.1 -- ResearchingBlueprintUUID match
        [Test]
        public void CountReferences_SingleResearchingMatch_ResearchingCountIsOne()
        {
            var structure = new ColonyStructure { ResearchingBlueprintUUID = TargetUUID };
            var colony = MakeColony(structure);

            var counter = new BlueprintReferenceCounter(
                new[] { colony },
                Enumerable.Empty<Bp>(),
                Enumerable.Empty<Survey>());

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.ResearchingCount, Is.EqualTo(1));
            Assert.That(report.TotalCount, Is.EqualTo(1));
        }

        // Requirement 1.1 -- ManufacturingBlueprintUUID match
        [Test]
        public void CountReferences_SingleManufacturingMatch_ManufacturingCountIsOne()
        {
            var structure = new ColonyStructure { ManufacturingBlueprintUUID = TargetUUID };
            var colony = MakeColony(structure);

            var counter = new BlueprintReferenceCounter(
                new[] { colony },
                Enumerable.Empty<Bp>(),
                Enumerable.Empty<Survey>());

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.ManufacturingCount, Is.EqualTo(1));
            Assert.That(report.TotalCount, Is.EqualTo(1));
        }

        // Requirement 1.2 -- baseBlueprintUUID match (different blueprint)
        [Test]
        public void CountReferences_SingleBaseBlueprintMatch_BaseBlueprintCountIsOne()
        {
            var otherBp = MakeBlueprint("other-uuid", baseBlueprintUUID: TargetUUID);

            var counter = new BlueprintReferenceCounter(
                Enumerable.Empty<Colony>(),
                new[] { otherBp },
                Enumerable.Empty<Survey>());

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.BaseBlueprintCount, Is.EqualTo(1));
            Assert.That(report.TotalCount, Is.EqualTo(1));
        }

        // Requirement 1.3 -- ScannerBlueprintUUID match
        [Test]
        public void CountReferences_SingleScannerMatch_ScannerCountIsOne()
        {
            var survey = MakeSurvey(TargetUUID);

            var counter = new BlueprintReferenceCounter(
                Enumerable.Empty<Colony>(),
                Enumerable.Empty<Bp>(),
                new[] { survey });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.ScannerCount, Is.EqualTo(1));
            Assert.That(report.TotalCount, Is.EqualTo(1));
        }

        // Requirements 1.1, 1.2, 1.3 -- multiple references across types
        [Test]
        public void CountReferences_MultipleReferencesAcrossTypes_CorrectTotal()
        {
            var s1 = new ColonyStructure { FlatpackBlueprintUUID = TargetUUID };
            var s2 = new ColonyStructure { ResearchingBlueprintUUID = TargetUUID };
            var s3 = new ColonyStructure { ManufacturingBlueprintUUID = TargetUUID };
            var colony = MakeColony(s1, s2, s3);

            var bp = MakeBlueprint("other-uuid", baseBlueprintUUID: TargetUUID);
            var survey = MakeSurvey(TargetUUID);

            var counter = new BlueprintReferenceCounter(
                new[] { colony },
                new[] { bp },
                new[] { survey });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.FlatpackCount, Is.EqualTo(1));
            Assert.That(report.ResearchingCount, Is.EqualTo(1));
            Assert.That(report.ManufacturingCount, Is.EqualTo(1));
            Assert.That(report.BaseBlueprintCount, Is.EqualTo(1));
            Assert.That(report.ScannerCount, Is.EqualTo(1));
            Assert.That(report.TotalCount, Is.EqualTo(5));
        }

        // Requirement 1.6 -- self-referencing blueprint excluded
        [Test]
        public void CountReferences_SelfReferencingBlueprint_ExcludedFromCount()
        {
            var selfRef = MakeBlueprint(TargetUUID, baseBlueprintUUID: TargetUUID);

            var counter = new BlueprintReferenceCounter(
                Enumerable.Empty<Colony>(),
                new[] { selfRef },
                Enumerable.Empty<Survey>());

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.BaseBlueprintCount, Is.EqualTo(0));
            Assert.That(report.TotalCount, Is.EqualTo(0));
        }

        // Requirement 1.5 -- null UUID returns empty report
        [Test]
        public void CountReferences_NullUUID_ReturnsEmptyReport()
        {
            var counter = new BlueprintReferenceCounter(
                Enumerable.Empty<Colony>(),
                Enumerable.Empty<Bp>(),
                Enumerable.Empty<Survey>());

            var report = counter.CountReferences(null);

            Assert.That(report, Is.SameAs(ReferenceReport.Empty));
        }

        // Requirement 1.5 -- empty string UUID returns empty report
        [Test]
        public void CountReferences_EmptyStringUUID_ReturnsEmptyReport()
        {
            var counter = new BlueprintReferenceCounter(
                Enumerable.Empty<Colony>(),
                Enumerable.Empty<Bp>(),
                Enumerable.Empty<Survey>());

            var report = counter.CountReferences(string.Empty);

            Assert.That(report, Is.SameAs(ReferenceReport.Empty));
        }

        // Error handling -- colony with null Structures handled gracefully
        [Test]
        public void CountReferences_ColonyWithNullStructures_HandledGracefully()
        {
            var colony = new Colony();
            colony.Structures = null;

            var counter = new BlueprintReferenceCounter(
                new[] { colony },
                Enumerable.Empty<Bp>(),
                Enumerable.Empty<Survey>());

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.TotalCount, Is.EqualTo(0));
        }

        // BuildItem.BlueprintUUID match
        [Test]
        public void CountReferences_SingleBuildItemMatch_BuildItemCountIsOne()
        {
            var buildPlan = new BuildPlan
            {
                UUID = "plan-1",
                Items = new List<BuildItem>
                {
                    new BuildItem { UUID = "bi-1", BlueprintUUID = TargetUUID }
                }
            };

            var counter = new BlueprintReferenceCounter(
                Enumerable.Empty<Colony>(),
                Enumerable.Empty<Bp>(),
                Enumerable.Empty<Survey>(),
                new[] { buildPlan });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.BuildItemCount, Is.EqualTo(1));
            Assert.That(report.TotalCount, Is.EqualTo(1));
        }

        [Test]
        public void CountReferences_MultipleBuildItemMatches_CountsAll()
        {
            var buildPlan = new BuildPlan
            {
                UUID = "plan-1",
                Items = new List<BuildItem>
                {
                    new BuildItem { UUID = "bi-1", BlueprintUUID = TargetUUID },
                    new BuildItem { UUID = "bi-2", BlueprintUUID = TargetUUID },
                    new BuildItem { UUID = "bi-3", BlueprintUUID = "other-bp" }
                }
            };

            var counter = new BlueprintReferenceCounter(
                Enumerable.Empty<Colony>(),
                Enumerable.Empty<Bp>(),
                Enumerable.Empty<Survey>(),
                new[] { buildPlan });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.BuildItemCount, Is.EqualTo(2));
            Assert.That(report.TotalCount, Is.EqualTo(2));
        }

        [Test]
        public void CountReferences_NoBuildPlans_BuildItemCountIsZero()
        {
            var counter = new BlueprintReferenceCounter(
                Enumerable.Empty<Colony>(),
                Enumerable.Empty<Bp>(),
                Enumerable.Empty<Survey>());

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.BuildItemCount, Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_BuildPlanWithNullItems_HandledGracefully()
        {
            var buildPlan = new BuildPlan { UUID = "plan-1", Items = null };

            var counter = new BlueprintReferenceCounter(
                Enumerable.Empty<Colony>(),
                Enumerable.Empty<Bp>(),
                Enumerable.Empty<Survey>(),
                new[] { buildPlan });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.BuildItemCount, Is.EqualTo(0));
        }

        // Completeness: StockPlan.Targets[].ItemReferenceID (when Blueprint)
        [Test]
        public void CountReferences_StockPlanTargetItemReferenceID_Counted()
        {
            var stockPlan = new StockPlan
            {
                UUID = "sp-1",
                Targets = new List<StockTarget>
                {
                    new StockTarget { ItemReferenceID = TargetUUID }
                }
            };

            var counter = new BlueprintReferenceCounter(
                Enumerable.Empty<Colony>(),
                Enumerable.Empty<Bp>(),
                Enumerable.Empty<Survey>(),
                stockPlans: new[] { stockPlan });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.StockTargetCount, Is.EqualTo(1));
            Assert.That(report.TotalCount, Is.GreaterThan(0));
        }

        // Completeness: MarketTransaction.ItemReferenceID (when Blueprint)
        [Test]
        public void CountReferences_MarketTransactionItemReferenceID_Counted()
        {
            var tx = new MarketTransaction { UUID = "mt-1", ItemReferenceID = TargetUUID };

            var counter = new BlueprintReferenceCounter(
                Enumerable.Empty<Colony>(),
                Enumerable.Empty<Bp>(),
                Enumerable.Empty<Survey>(),
                marketTransactions: new[] { tx });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.TotalCount, Is.GreaterThan(0));
        }
    }
}
